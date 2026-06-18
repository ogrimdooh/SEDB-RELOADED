using DSharpPlus.Entities;
using HarmonyLib;
using NLog;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SEDiscordBridge.Patches;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Interop;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Managers;
using Torch.Managers.ChatManager;
using Torch.Server;
using Torch.Session;
using VRage.Game;
using VRage.Game.ModAPI;
using Timer = System.Timers.Timer;

namespace SEDiscordBridge
{
    public sealed class SEDiscordBridgePlugin : TorchPluginBase, IWpfPlugin
    {
        public SEDBConfig Config => _config?.Data;

        public Persistent<SEDBConfig> _config;

        public DiscordBridge DDBridge;

        public bool IsRestart { get; set; } = false;

        public MethodInfo InjectDiscordIDMethod = null;

        private UserControl _control;
        private TorchSessionManager _sessionManager;
        private ChatManagerServer _chatmanager;
        public IChatManagerServer ChatManager => _chatmanager ?? (Torch.CurrentSession?.Managers?.GetManager<IChatManagerServer>());
        private IMultiplayerManagerBase _multibase;
        private readonly List<TorchChatMessage> _uniqueMessages = new List<TorchChatMessage>();
        private Timer _timer;
        private TorchServer torchServer;

        private readonly HashSet<ulong> _conecting = new HashSet<ulong>();

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static SEDiscordBridgePlugin Static { get; private set; }

        public static bool DEBUG
        {
            get
            {
                if (Static != null)
                    return Static.Config?.DebugMode ?? false;
                return false;
            }
        }

        /// <inheritdoc />
        public UserControl GetControl() => _control ??= new SEDBControl(this);

        public void Save() => _config?.Save();


        /// <inheritdoc />
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            torchServer = (TorchServer)torch;
            Static = this;

            var harmony = new Harmony("SEDiscordBridge");
            try
            {
                PatchController.PatchMethods();
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(GetType(), e, "PATCHING FAILED ");
            }

            //Init config
            InitConfig();

            //Set events
            DiscordBridge.OnReady += DiscordBridge_OnReady;

            //pre-load
            if (Config.Enabled) LoadSEDB();
        }

        public void InitConfig()
        {
            try
            {
                _config = Persistent<SEDBConfig>.Load(Path.Combine(StoragePath, "SEDiscordBridge.cfg"));
            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data == null)
                _config = new Persistent<SEDBConfig>(Path.Combine(StoragePath, "SEDiscordBridge.cfg"), new SEDBConfig());
        }

        private void MessageReceived(TorchChatMessage msg, ref bool consumed)
        {

            _ = Task.Run(() =>
            {
                lock (_uniqueMessages)
                {
                    if (_uniqueMessages.Any(message => Equals(message.Message,msg.Message) && Equals(message.Author,msg.Author)))
                    {
                        return Task.CompletedTask;
                    }
                    _uniqueMessages.Add(msg);
                }

                Thread.Sleep(1000);
                
                lock (_uniqueMessages)
                {
                    _uniqueMessages.ForEach(SendAsync);
                    _uniqueMessages.Clear();
                }
                return Task.CompletedTask;
            });
        }

        private async void SendAsync(TorchChatMessage msg)
        {
            try
            {
                if (!Config.Enabled) return;

                var foundMutedPlayer = false;

                if (ChatManager != null) {
                    if (msg.AuthorSteamId != null && ChatManager.MutedUsers.Contains((ulong)msg.AuthorSteamId))
                        foundMutedPlayer = true;
                }

                if (msg.AuthorSteamId != null && !foundMutedPlayer) {
                    if (DEBUG)
                        Log.Info($"Recieved messages with valid SID {msg.Author} | {msg.Message} | {msg.Target} | {msg.AuthorSteamId}");

                    switch (msg.Channel)
                    {
                        case ChatChannel.Global:
                            await DDBridge.SendChatMessage(msg.Author, msg.Message);
                            break;
                        case ChatChannel.GlobalScripted:
                            await DDBridge.SendChatMessage(msg.Author, msg.Message);
                            break;
                        case ChatChannel.Faction:
                            if (msg.AuthorSteamId.ToString().StartsWith("7")) {
                                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(msg.Target);
                                DDBridge.SendFacChatMessage(msg.Author, msg.Message, fac.Name);
                            }
                            break;
                    }
                }
                else if (Config.ServerToDiscord && msg.Channel.Equals(ChatChannel.Global) && !msg.Message.StartsWith(Config.CommandPrefix) && msg.Target.Equals(0)) {
                    if(DEBUG)
                        Log.Info($"Recieved messages with no SID {msg.Author} | {msg.Message} | {msg.Target}");

                    await DDBridge.SendChatMessage(msg.Author, msg.Message);
                }
            }
            catch (Exception e)
            {
                Log.Fatal(e);
            }
        }

        public void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            if (!Config.Enabled) return;

            switch (state)
            {
                case TorchSessionState.Loaded:
                    //load
                    LoadSEDB();
                    if (DDBridge != null) DDBridge.SendStatusMessage(null, Config.Started);
                    break;

                case TorchSessionState.Unloading:
                    if (IsRestart)
                    {
                        if (Config.Restarted.Length > 0)
                            DDBridge.SendStatusMessage(null, Config.Restarted);
                    }
                    else
                    {
                        if (Config.Stopped.Length > 0)
                            DDBridge.SendStatusMessage(null, Config.Stopped);
                    }
                    break;

                case TorchSessionState.Unloaded:
                    //unload
                    UnloadSEDB();

                    break;
                default:
                    // ignore
                    break;
            }
        }

        public void UnloadSEDB()
        {
            if (DDBridge != null)
            {
                Log.Info("Unloading Discord Bridge!");
                DDBridge.StopDiscord();
                DDBridge = null;
                Log.Info("Discord Bridge Unloaded!");
            }
            Dispose();
        }

        public void ReflectEssentials() {
            var pluginId = new Guid("cbfdd6ab-4cda-4544-a201-f73efa3d46c0");
            var pluginManager = Torch.Managers.GetManager<PluginManager>();

            if (pluginManager.Plugins.TryGetValue(pluginId, out ITorchPlugin EssentialsPlugin)) {
                try {
                    MethodInfo[] methods = null;
                    methods = EssentialsPlugin.GetType().GetMethods();
                    foreach (var meth in methods) {
                        if (meth.Name == "InsertDiscordID") {
                            InjectDiscordIDMethod = meth;
                        }
                    }
                }
                catch (Exception e) {
                    Log.Warn(e, "failure");
                }

            }
            else
                Log.Info("Essentials Plugin not found! ");
        }
        public Dictionary<ulong, string> GetRoles(ulong userID) {
            List<DiscordRole> discordRoles;
            Dictionary<ulong, string> roleData = new Dictionary<ulong, string>();
            var guilds = DiscordBridge.Discord.Guilds;
            foreach (var guildID in guilds) {
                var Guild = DiscordBridge.Discord.GetGuildAsync(guildID.Key).Result;
                discordRoles = Guild.GetMemberAsync(userID).Result.Roles.ToList();

                foreach (var role in discordRoles) {
                    roleData.Add(role.Id, role.Name);
                }
            }
            return roleData;
        }

        private const string SEASON_META_START_MESSAGE_TEMPLATE = @":satellite: **THE SECOND DAWN — JUMP STATUS TERMINAL**

This channel is maintained by **D.A.W.N. — Distributed Ark Watch Network**.
Every cycle, the Ark gathers fuel, materials, provisions, and strategic cargo required to perform the next interstellar jump. Contributions from all registered explorers and factions are processed through the Ark logistics network and reflected here.
Status reports are refreshed automatically. Outdated transmissions are purged to preserve terminal clarity.

---";

        private const string SEASON_META_OVERALL_MESSAGE_TEMPLATE = @":scales:  **JUMP PREPARATION STATUS**

Overall Progress: **{0}**
Next Weekly Checkpoint: **{1}**
Next Ark Jump: **{2}**

Current Protocol: **Resource Acquisition**
Ark Status: **{3}**
Risk Level: **{4}**

---";

        private const string SEASON_META_CATEGORY_MESSAGE_TEMPLATE = @"{0} **{1}**
Progress Weight: {2}

{3} / {4} {5} — **{6}**

**Accepted Cargo:**
{7}
---";

        private string GetCategoryEmoji(SeasonMetaEntryType type)
        {
            return type switch
            {
                SeasonMetaEntryType.RawResource => ":pick:",
                SeasonMetaEntryType.RefinedResource => ":hammer_pick:",
                SeasonMetaEntryType.AssembledComponent => ":tools:",
                SeasonMetaEntryType.Fuel => ":fuelpump:",
                SeasonMetaEntryType.SurvivalResource => ":meat_on_bone:",
                SeasonMetaEntryType.Ammo => ":gun:",
                SeasonMetaEntryType.WeaponAndTool => ":toolbox:",
                _ => ":package:"
            };
        }

        private string GetCategoryUnit(SeasonMetaEntryType type)
        {
            return type switch
            {
                SeasonMetaEntryType.RawResource => "Kg",
                SeasonMetaEntryType.RefinedResource => "Kg",
                SeasonMetaEntryType.AssembledComponent => "U",
                SeasonMetaEntryType.Fuel => "Kg",
                SeasonMetaEntryType.SurvivalResource => "U",
                SeasonMetaEntryType.Ammo => "U",
                SeasonMetaEntryType.WeaponAndTool => "U",
                _ => "U"
            };
        }

        private string GetSeasonMetaStatusString(float currentProgress)
        {
            if (currentProgress < 0.25f)
                return "Critical Supply Shortage";
            else if (currentProgress < 0.5f)
                return "Below Required Threshold";
            else if (currentProgress < 0.75f)
                return "Operational, Understocked";
            else if (currentProgress < 1.0f)
                return "Jump Window Approaching";
            else
                return "Jump Ready";
        }

        private string GetSeasonMetaSubStatusString(float currentProgress)
        {
            if (currentProgress < 0.25f)
                return "High";
            else if (currentProgress < 0.5f)
                return "Elevated";
            else if (currentProgress < 0.75f)
                return "Stable";
            else if (currentProgress < 1.0f)
                return "Low";
            else
                return "Minimal";
        }

        private string GetItemDisplayName(StorageDefinitionId id)
        {
            var definition = MyDefinitionManager.Static.GetPhysicalItemDefinition(id.ToMyDefinitionId());
            if (definition != null)
                // Retorna somente a primeira linha do nome se tiver quebra de linha, para evitar mensagens muito longas no Discord
                return definition.DisplayNameText.Split('\n')[0];
            return id.ToString();
        }

        public async Task RefreshSeasonMetaChannel()
        {
            Log.Info("Refreshing Season Meta Channel...");
            if (!string.IsNullOrWhiteSpace(Config.SeasonMetaChannelID) && SEDBStorage.Instance.SeasonMeta.Enabled)
            {
                Log.Info("Season Meta is enabled, updating channel...");
                var seasonConfig = SEDBStorage.Instance.SeasonMeta.GetActiveConfiguration();
                var seasonResult = SEDBStorage.Instance.SeasonMeta.GetActiveResult();
                if (seasonConfig != null && seasonResult != null)
                {
                    Log.Info("Season Meta configuration and result found, updating channel...");
                    try
                    {
                        var channel = DiscordBridge.Discord.GetChannelAsync(ulong.Parse(Config.SeasonMetaChannelID)).Result;
                        if (channel != null)
                        {
                            Log.Info("Season Meta channel found, updating messages...");
                            // Limpa mensagens antigas
                            var needNewMessages = false;
                            var messages = await channel.GetMessagesAsync(100);
                            if (messages.Any()) 
                            {
                                Log.Info($"Found {messages.Count} messages in the channel, checking if they match expected count and IDs...");
                                // Calcula o total de mensagens que deveriam estar no canal (mensagem geral + mensagens de categoria)
                                var expectedMessageCount = 2 + seasonConfig.Entries.Count; // 2 para a mensagem geral + 1 para cada categoria
                                // Verifica se todas as mensagens com ids salvos existem, caso contrário, limpa o canal para evitar mensagens desatualizadas
                                var ids = SEDBStorage.Instance.SeasonMeta.ChatMessagesIds.GetAllMessagesIds();
                                var msgsIds = messages.Select(m => m.Id).ToHashSet();
                                Log.Info($"Expected message count: {expectedMessageCount}, actual message count: {messages.Count}, expected IDs: {string.Join(", ", ids)}, actual IDs: {string.Join(", ", msgsIds)}");
                                if (expectedMessageCount != messages.Count || 
                                    expectedMessageCount != ids.Count || 
                                    !ids.All(id => msgsIds.Any(m => m != 0 && m == id)))
                                {
                                    Log.Warn("Message count or IDs do not match expected values, clearing channel messages...");
                                    await channel.DeleteMessagesAsync(messages);
                                    needNewMessages = true;
                                }
                                else
                                {
                                    Log.Info("All expected messages are present, will update existing messages...");
                                }
                            }
                            else
                            {
                                needNewMessages = true;
                            }

                            // Envia nova mensagem com os dados atualizados
                            var startMsg = SEASON_META_START_MESSAGE_TEMPLATE.Replace("/n", "\n");
                            if (needNewMessages) 
                            {
                                Log.Info("Sending new start message to the channel...");
                                MsgWorker.SendToDiscord(channel, startMsg, true, (dMsg) =>
                                {
                                    SEDBStorage.Instance.SeasonMeta.ChatMessagesIds.StartMsgId = dMsg.Id;
                                });
                            } 
                            else
                            {
                                Log.Info("Updating existing start message...");
                                // Atualiza a mensagem geral
                                var generalMsgId = SEDBStorage.Instance.SeasonMeta.ChatMessagesIds.StartMsgId;
                                var generalMsg = messages.FirstOrDefault(m => m.Id == generalMsgId);
                                if (generalMsg != null && generalMsg.Content.CompareTo(startMsg) != 0)
                                {
                                    await generalMsg.ModifyAsync(startMsg);
                                }
                            }
                            // Envia mensagem com o progresso geral
                            var currentProgress = SEDBStorage.Instance.SeasonMeta.GetCurrentProgress();
                            var nextCheckpoint = SEDBStorage.Instance.SeasonMeta.GetTimeToNextCheckpoint();
                            var nextSeason = SEDBStorage.Instance.SeasonMeta.GetTimeToNextSeason();
                            var overallMessage = string.Format(SEASON_META_OVERALL_MESSAGE_TEMPLATE, 
                                currentProgress.ToString("P2"), 
                                nextCheckpoint.ToString(@"d'd 'm'm 's's'"), 
                                nextSeason.ToString(@"d'd 'm'm 's's'"),
                                GetSeasonMetaStatusString(currentProgress),
                                GetSeasonMetaSubStatusString(currentProgress)
                            );
                            if (needNewMessages) 
                            {
                                Log.Info("Sending new overall progress message to the channel...");
                                MsgWorker.SendToDiscord(channel, overallMessage, true, (dMsg) =>
                                {
                                    SEDBStorage.Instance.SeasonMeta.ChatMessagesIds.OverAllMsgId = dMsg.Id;
                                });
                            } 
                            else
                            {
                                Log.Info("Updating existing overall progress message...");
                                var overallMsgId = SEDBStorage.Instance.SeasonMeta.ChatMessagesIds.OverAllMsgId;
                                var overallMsg = messages.FirstOrDefault(m => m.Id == overallMsgId);
                                if (overallMsg != null && overallMsg.Content.CompareTo(overallMessage) != 0)
                                {
                                    await overallMsg.ModifyAsync(overallMessage);
                                }
                            }
                            // Envia mensagens
                            var allProgress = SEDBStorage.Instance.SeasonMeta.GetActiveResultProgress();
                            Log.Info($"Updating category messages, total categories: {seasonConfig.Entries.Count}, progress entries: {allProgress.Count}...");
                            foreach (var item in seasonConfig.Entries)
                            {
                                var categoryInfo = SEDBStorage.Instance.SeasonMeta.GetCategoryById(item.CategoryId);
                                if (categoryInfo != null && allProgress.ContainsKey(item.CategoryId))
                                {
                                    var resultEntry = seasonResult.Entries.FirstOrDefault(e => e.CategoryId == item.CategoryId);
                                    var itemProgress = allProgress[item.CategoryId];
                                    var categoryItemsByWeight = categoryInfo.Items.GroupBy(x => x.Weight).ToDictionary(x => x.Key, x => x.ToList());
                                    var sb = new StringBuilder();
                                    foreach (var weight in categoryItemsByWeight.Keys.OrderBy(x => x))
                                    {
                                        sb.AppendLine($"- Logistic Value {weight}: " + string.Join(", ", categoryItemsByWeight[weight].Select(i => $"{GetItemDisplayName(i.Id)}")));
                                    }
                                    var categoryMessage = string.Format(SEASON_META_CATEGORY_MESSAGE_TEMPLATE,
                                        GetCategoryEmoji(categoryInfo.Type),
                                        categoryInfo.Name,
                                        itemProgress.Y,
                                        resultEntry.Amount.ToString("N0"),
                                        item.Amount.ToString("N0"),
                                        GetCategoryUnit(categoryInfo.Type),
                                        itemProgress.X.ToString("P2"),
                                        sb.ToString()
                                    );
                                    if (needNewMessages)
                                    {
                                        Log.Info($"Sending new message for category {categoryInfo.Name} to the channel...");
                                        MsgWorker.SendToDiscord(channel, categoryMessage, true, (dMsg) => {
                                            SEDBStorage.Instance.SeasonMeta.ChatMessagesIds.CategoriesMsgIds.Add(new SeasonMetaChatEntryId()
                                            {
                                                CategoryId = item.CategoryId,
                                                MsgId = dMsg.Id
                                            });
                                        });
                                    } 
                                    else
                                    {
                                        Log.Info($"Updating existing message for category {categoryInfo.Name}...");
                                        var categoryMsgId = SEDBStorage.Instance.SeasonMeta.ChatMessagesIds.CategoriesMsgIds.FirstOrDefault(m => m.CategoryId == item.CategoryId)?.MsgId;
                                        if (categoryMsgId != null)
                                        {
                                            var categoryMsg = messages.FirstOrDefault(m => m.Id == categoryMsgId);
                                            if (categoryMsg != null && categoryMsg.Content.CompareTo(categoryMessage) != 0)
                                            {
                                                await categoryMsg.ModifyAsync(categoryMessage);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.Instance.LogError(GetType(), e, "SEASON META CHANNEL UPDATE FAILED ");
                    }
                }
            }
        }

        private const string REGISTRY_START_MESSAGE = @":satellite: **THE SECOND DAWN — ARK REGISTRY**

This terminal is maintained by **D.A.W.N. — Distributed Ark Watch Network**.
All explorers seeking official access to Ark logistics, cargo storage, faction records, jump preparation systems, and seasonal transfer protocols must complete registration.

To begin, just react to this message with any emoji. D.A.W.N. will open a private transmission and guide you through the identity link procedure.

Once confirmed, your Discord identity will be linked to your in-game explorer profile and your Ark access credentials will be issued.

---";

        private const string REGISTRY_DM_CODE_SEND = @":incoming_envelope: **D.A.W.N. PRIVATE TRANSMISSION**

Explorer registration request received.

Thank you for joining the **Second Dawn Ark Initiative**. Your presence has been logged, but your identity must be verified before Ark access credentials can be issued.
To complete the link between your Discord profile and your in-game explorer record, enter the following command inside the **Space Engineers in-game chat**:

`!ark-registry:{0}`

This authorization key is personal and temporary (will expire in {1} minutes). Do not share it with other explorers.
Once the command is received from inside the server, D.A.W.N. will finalize your registration.
";

        private const string REGISTRY_DM_REGISTRY_COMPLETED = @":identification_card: **ARK REGISTRY CONFIRMED**

Identity link complete.

You are now officially registered as a member of the **Second Dawn Crew**.
Your profile has been connected to the Ark logistics network. From this point forward, your contributions, cargo transfers, faction activity, tribute status, and jump-cycle records may be processed by **D.A.W.N. — Distributed Ark Watch Network**.

Welcome aboard the **The Second Dawn**, explorer.

Report to your faction, secure your cargo, and prepare for the next jump.
";

        private const string REGISTRY_ALERT_REGISTRY_COMPLETED = @":white_check_mark: Registry confirmed. New explorer added to the Second Dawn Crew.";

        public async Task StartRegistryToUser(DiscordUser user)
        {
            if (SEDBStorage.Instance.Registry.Enabled)
            {
                if (!SEDBStorage.Instance.Registry.IsUserRegistered(user.Id))
                {
                    if (!SEDBStorage.Instance.Registry.UserHasValidToken(user.Id))
                    {
                        var token = SEDBStorage.Instance.Registry.CreateToken(user.Id);
                        var msgToSend = string.Format(REGISTRY_DM_CODE_SEND, token, SEDBStorage.Instance.Registry.TokenValidInMinutes);
                        DDBridge.Dis
                    }
                }
            }
        }

        private async Task RefreshRegistryChannel()
        {
            Log.Info("Refreshing Season Registry Channel...");
            if (!string.IsNullOrWhiteSpace(Config.RegistryChannelId) && SEDBStorage.Instance.Registry.Enabled)
            {
                Log.Info("Registry is enabled, updating channel...");

                var channel = DiscordBridge.Discord.GetChannelAsync(ulong.Parse(Config.RegistryChannelId)).Result;
                if (channel != null)
                {
                    Log.Info("Registry channel found, updating messages...");
                    // Limpa mensagens antigas
                    var needNewMessages = false;
                    var messages = await channel.GetMessagesAsync(100);
                    if (messages.Any())
                    {
                        Log.Info($"Found {messages.Count} messages in the channel, checking if they match expected count and IDs...");
                        // Calcula o total de mensagens que deveriam estar no canal (mensagem geral + mensagens de categoria)
                        var expectedMessageCount = 1; // 1 para a mensagem geral
                        // Verifica se todas as mensagens com ids salvos existem, caso contrário, limpa o canal para evitar mensagens desatualizadas
                        var ids = SEDBStorage.Instance.Registry.GetAllMessagesIds();
                        var msgsIds = messages.Select(m => m.Id).ToHashSet();
                        Log.Info($"Expected message count: {expectedMessageCount}, actual message count: {messages.Count}, expected IDs: {string.Join(", ", ids)}, actual IDs: {string.Join(", ", msgsIds)}");
                        if (expectedMessageCount != messages.Count ||
                            expectedMessageCount != ids.Count ||
                            !ids.All(id => msgsIds.Any(m => m != 0 && m == id)))
                        {
                            Log.Warn("Message count or IDs do not match expected values, clearing channel messages...");
                            await channel.DeleteMessagesAsync(messages);
                            needNewMessages = true;
                        }
                        else
                        {
                            Log.Info("All expected messages are present, will update existing messages...");
                        }
                    }
                    else
                    {
                        needNewMessages = true;
                    }

                    // Envia nova mensagem com os dados atualizados
                    var startMsg = REGISTRY_START_MESSAGE.Replace("/n", "\n");
                    if (needNewMessages)
                    {
                        Log.Info("Sending new start message to the channel...");
                        MsgWorker.SendToDiscord(channel, startMsg, true, (dMsg) =>
                        {
                            SEDBStorage.Instance.Registry.StartMsgId = dMsg.Id;
                        });
                    }
                    else
                    {
                        Log.Info("Updating existing start message...");
                        // Atualiza a mensagem geral
                        var generalMsgId = SEDBStorage.Instance.Registry.StartMsgId;
                        var generalMsg = messages.FirstOrDefault(m => m.Id == generalMsgId);
                        if (generalMsg != null && generalMsg.Content.CompareTo(startMsg) != 0)
                        {
                            await generalMsg.ModifyAsync(startMsg);
                        }
                    }
                }
            }
        }

        private bool _discordChannelsCanBeRefreshing = false;
        private bool _seasonMetaNeedRefreshing = false;
        private bool _registryNeedRefreshing = false;

        public void LoadSEDB()
        {
            if (DDBridge == null)
                DDBridge = new DiscordBridge(this);

            _discordChannelsCanBeRefreshing = false;
            _seasonMetaNeedRefreshing = true;
            _registryNeedRefreshing = true;

            if (Config.LoadRanks)
                ReflectEssentials();

            if (Config.BotToken.Length <= 0)
            {
                Log.Error("No BOT token set, plugin will not work at all! Add your bot TOKEN, save and restart torch.");
                return;
            }

            if (_sessionManager == null)
            {
                _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();

                if (_sessionManager == null)
                    Log.Warn("No session manager loaded!");
                else
                    _sessionManager.SessionStateChanged += SessionChanged;
            }

            if (Torch.CurrentSession != null)
            {

                try
                {
                    GameWatcherController.Init();
                }
                catch (Exception e)
                {
                    Logging.Instance.LogError(GetType(), e, "WATCHER FAILED ");
                }

                _discordChannelsCanBeRefreshing = true;
                if (DiscordBridge.Ready)
                {
                    if (_discordChannelsCanBeRefreshing)
                    {
                        _seasonMetaNeedRefreshing = false;
                        MyAPIGateway.Parallel.Start(() => {
                            RefreshSeasonMetaChannel().Wait();
                        });
                    }
                    if (_registryNeedRefreshing)
                    {
                        _registryNeedRefreshing = false;
                        MyAPIGateway.Parallel.Start(() => {
                            RefreshRegistryChannel().Wait();
                        });
                    }
                }

                if (_chatmanager == null)
                {
                    _chatmanager = Torch.CurrentSession.Managers.GetManager<ChatManagerServer>();
                    if (_chatmanager == null)
                        Log.Warn("No chat manager loaded!");
                    else
                        _chatmanager.MessageRecieved += MessageReceived;
                }
                InitPost();
            }
            else if (Config.PreLoad)
                InitPost();

            Log.Info("Discord Bridge loaded!");
        }

        private void DiscordBridge_OnReady()
        {
            if (_seasonMetaNeedRefreshing)
            {
                if (_discordChannelsCanBeRefreshing)
                {
                    _seasonMetaNeedRefreshing = false;
                    MyAPIGateway.Parallel.Start(() => {
                        RefreshSeasonMetaChannel().Wait();
                    });
                }
                if (_registryNeedRefreshing)
                {
                    _registryNeedRefreshing = false;
                    MyAPIGateway.Parallel.Start(() => {
                        RefreshRegistryChannel().Wait();
                    });
                }
            }
        }

        private void InitPost()
        {
            //send status
            if (Config.UseStatus)
                StartTimer();
        }

        public void StartTimer()
        {
            if (_timer != null) StopTimer();

            _timer = new Timer(Config.StatusInterval);
            _timer.Elapsed += Timer_Elapsed;
            _timer.Enabled = true;
        }

        public void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Elapsed -= Timer_Elapsed;
                _timer.Enabled = false;
                _timer.Dispose();
                _timer = null;
            }
        }

        // for counter within _timer_elapsed() 
        private int i = 0;
        private DateTime timerStart = new DateTime(0);
        private int TickRetry = 0;

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!Config.Enabled || DDBridge == null) return;

            if (DiscordBridge.Ready)
                TickRetry = 0;

            if (!DiscordBridge.Ready)
            {
                if (TickRetry == 5)
                {
                    DiscordBridge.Discord.DisconnectAsync();
                    DiscordBridge.Discord.ConnectAsync();
                }
                else
                {
                    if (TickRetry > 24)
                    {
                        DiscordBridge.Ready = false;
                        TickRetry = 0;
                        DiscordBridge.Discord.DisconnectAsync();
                        DiscordBridge.Discord.Dispose();
                        DDBridge = new DiscordBridge(this);
                        return;
                    }
                }

                TickRetry++;
            }
            else if (DiscordBridge.Discord.Ping == 0)
            {
                DiscordBridge.Ready = false;
                TickRetry = 0;
                DiscordBridge.Discord.DisconnectAsync();
                DiscordBridge.Discord.Dispose();
                DDBridge = new DiscordBridge(this);
                return;
            }

            if (Torch.CurrentSession == null || torchServer.SimulationRatio <= 0f)
                DDBridge.SendStatus(Config.StatusPre, UserStatus.DoNotDisturb);
            else
            {
                if (timerStart.Ticks == 0) timerStart = e.SignalTime;

                string status = Config.Status;
                DateTime upTime = new DateTime(e.SignalTime.Subtract(timerStart).Ticks);

                Regex regex = new Regex(@"{uptime@(.*?)}");
                if (regex.IsMatch(status))
                {
                    var match = regex.Match(status);
                    string format = match.Groups[0].ToString().Replace("{uptime@", "").Replace("}", "");
                    status = Regex.Replace(status, "{uptime@(.*?)}", upTime.ToString(format));
                }

                var playersCount = MySession.Static.Players.GetOnlinePlayers().Where(p => p.IsRealPlayer).Count();
                var maxPlayers = MySession.Static.MaxPlayers;
                var simSpeed = torchServer.SimulationRatio.ToString("0.00");

                DDBridge.SendStatus(status
                .Replace("{p}", playersCount.ToString())
                .Replace("{mp}", maxPlayers.ToString())
                .Replace("{mc}", MySession.Static.Mods.Count.ToString())
                .Replace("{ss}", simSpeed), playersCount > 0? UserStatus.Online : UserStatus.Idle);

                if (Config.SimPing)
                {
                    if (torchServer.SimulationRatio < Config.SimThresh)
                    {
                        //condition
                        if (i == DiscordBridge.MinIncrement && DiscordBridge.Locked != 1 && playersCount > 0)
                        {
                            Task.Run(() => DDBridge.SendSimMessage(Config.SimMessage));
                            i = 0;
                            DiscordBridge.Locked = 1;
                            DiscordBridge.FirstWarning = 1;
                            DiscordBridge.CooldownNeutral = 0;
                            Log.Warn("Simulation warning sent!");
                        }

                        if (DiscordBridge.FirstWarning == 1 && DiscordBridge.CooldownNeutral.ToString("00") == "60" && playersCount > 0)
                        {
                            Task.Run(() => DDBridge.SendSimMessage(Config.SimMessage));
                            Log.Warn("Simulation warning sent!");
                            DiscordBridge.CooldownNeutral = 0;
                            i = 0;

                        }

                        DiscordBridge.CooldownNeutral += (60 / DiscordBridge.Factor);
                        i++;
                    }
                    else
                    {
                        //reset counter whenever Sim speed warning threshold is not met meaning that sim speed has to stay below
                        //the set threshold for a consecutive minuete to trigger warning
                        i = 0;
                        DiscordBridge.CooldownNeutral = 0;
                    }
                }
            }
        }

        /// <inheritdoc />
        private static bool _disposed = false;
        public override void Dispose()
        {
            if (_disposed)
            {
                if (DEBUG)
                {
                    Logging.Instance.LogInfo(GetType(), "SEDB already disposed!");
                }
            }
            else
            {
                _disposed = true;
                try
                {
                    Logging.Instance.LogInfo(GetType(), "Unloading SEDB Lite!");
                    if (Static != null)
                    {
                        Static.DDBridge.SendStatusMessage(default, default, Static.Config.Stopped).Wait();
                        MsgWorker.DisconnectAfterSendAllMsgs(Static.DDBridge);
                    }
                    GameWatcherController.Dispose();
                }
                catch (Exception e)
                {
                    Logging.Instance.LogError(typeof(SEDiscordBridgePlugin), e);
                }

                if (_sessionManager != null)
                    _sessionManager.SessionStateChanged -= SessionChanged;

                _sessionManager = null;

                if (_chatmanager != null)
                    _chatmanager.MessageRecieved -= MessageReceived;

                _chatmanager = null;

                StopTimer();

            }
        }
    }
}
