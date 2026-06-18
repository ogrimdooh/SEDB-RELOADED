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
using Torch.API.WebAPI;
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

`!ark registry {0}`

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

        private const string REGISTRY_ALERT_REGISTRY_COMPLETED = @":white_check_mark: D.A.W.N. Registry Update

Explorer **{0}** has been officially added to the Second Dawn Crew.

The Ark recognizes their signal. The journey continues with one more soul aboard.";

        public void AlertRegistryIsCompleted(ulong userId)
        {
            if (!string.IsNullOrWhiteSpace(Config.AlertsChannelId) && SEDBStorage.Instance.Registry.Enabled)
            {
                var user = DiscordBridge.Discord.GetUserAsync(userId).Result;
                var channel = DiscordBridge.Discord.GetChannelAsync(ulong.Parse(Config.AlertsChannelId)).Result;
                if (channel != null && user != null)
                {
                    Log.Info("Sending new start message to the channel...");
                    MsgWorker.SendToDiscord(channel, string.Format(REGISTRY_ALERT_REGISTRY_COMPLETED, user.Username), true);
                }
            }
        }

        public async Task CompleteRegistryToUser(ulong userId)
        {
            if (SEDBStorage.Instance.Registry.Enabled)
            {
                await DDBridge.SendDmMessage(userId, REGISTRY_DM_REGISTRY_COMPLETED);
            }
        }

        public async Task StartRegistryToUser(DiscordUser user, DiscordGuild guild)
        {
            Log.Info($"Start the registry to user {user.Username}");
            if (SEDBStorage.Instance.Registry.Enabled)
            {
                if (!SEDBStorage.Instance.Registry.IsUserRegistered(user.Id))
                {
                    Log.Info($"User is not registred!");
                    if (!SEDBStorage.Instance.Registry.UserHasValidToken(user.Id))
                    {
                        Log.Info($"User has no active token!");
                        var token = SEDBStorage.Instance.Registry.CreateToken(user.Id);
                        Log.Info($"Token {token} created to user {user.Username}!");
                        var msgToSend = string.Format(REGISTRY_DM_CODE_SEND, token, SEDBStorage.Instance.Registry.TokenValidInMinutes);
                        var member = await guild.GetMemberAsync(user.Id);
                        if (member != null)
                        {
                            await member.SendMessageAsync(msgToSend);
                            Log.Info($"Token send to {user.Username}!");
                        }
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
                            dMsg.CreateReactionAsync(DiscordBridge.ThumbsupEmoji).Wait();
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
                        await generalMsg.DeleteAllReactionsAsync();
                        await generalMsg.CreateReactionAsync(DiscordBridge.ThumbsupEmoji);
                    }
                }
                Log.Info("Cleaning old tokens!");
                SEDBStorage.Instance.Registry.CleanOldTokens();
            }
        }

        private const string BANK_START_MESSAGE = @":bank: **THE SECOND DAWN — ARK BANK TERMINAL**

This terminal is maintained by **D.A.W.N. — Distributed Ark Watch Network**.

The Ark Bank converts local in-game currency into **Dawn Marks** `DMK`, the official persistent financial unit of **The Second Dawn**.
Local currency belongs to the current system and may be lost when the Ark Jump occurs. **Dawn Marks are stored inside the Ark ledger and are preserved between jump cycles.**

---

:coin: **Conversion Rates**

**Deposit:** `2 In-Game Credits` → `1 DMK`
**Withdraw:** `2 DMK` → `1 In-Game Credit`

All conversions include logistical processing, ledger validation, and Ark-side reserve fees. Because of this, moving currency in and out of the Ark Bank always causes some value loss.

**Plan your transfers carefully.**

---

:inbox_tray: **Depositing Funds**

To deposit in-game currency into your Ark Bank account, enter this command inside the **Space Engineers in-game chat**:

`!ark bank deposit <AMOUNT>`

Example:

`!ark bank deposit 10000`

This will remove `10,000` in-game credits from your game account and convert them into `5,000 DMK`.

---

:outbox_tray: **Withdrawing Funds**

To withdraw Dawn Marks back into in-game currency, enter this command inside the **Space Engineers in-game chat**:

`!ark bank withdraw <AMOUNT>`

Example:

`!ark bank withdraw 5000`

This will remove `5,000 DMK` from your Ark Bank account and convert them into `2,500` in-game credits.

---

:receipt: **Balance & Ledger Report**

React to this message at any time to request a private Ark Bank report.

D.A.W.N. will send you a DM containing:

• Your current **Dawn Marks** balance
• Your latest 10 incoming and outgoing transactions
• Recent deposit and withdrawal records

Only registered members of the **Second Dawn Crew** can access Ark Bank services.
";

        private const string BANK_BALANCE_DM_MESSAGE = @":bank: **D.A.W.N. — ARK BANK REPORT**

Private financial report generated for your registered Ark account.

**Account Status:** `Active`
**Currency:** `Dawn Marks (DMK)`
**Current Balance:** `{0} DMK`

Dawn Marks are stored inside the **Second Dawn Ark Ledger** and are preserved between Ark Jump cycles.

---

:receipt: **Latest Ledger Records**

{1}
---

:information_source: **Ark Bank Rates**

Deposit Rate: `2 In-Game Credits` → `1 DMK`
Withdrawal Rate: `2 DMK` → `1 In-Game Credit`

All transfers include Ark-side ledger validation and logistical processing fees.

To deposit or withdraw funds, use the following commands inside the **Space Engineers in-game chat**:

`!ark bank deposit <AMOUNT>`
`!ark bank withdraw <AMOUNT>`

Report generated by **D.A.W.N. — Distributed Ark Watch Network**.
";

        private const string BANK_BALANCE_DEPOSIT_ENTRY_MESSAGE = @"`[{0}]` Deposit — `+{1} DMK`
Converted from `{2} In-Game Credits`";

        private const string BANK_BALANCE_WITHDRAWAL_ENTRY_MESSAGE = @"`[{0}]` Withdrawal — `-{1} DMK`
Converted into `{2} In-Game Credits`";

        private const string BANK_BALANCE_FEE_ENTRY_MESSAGE = @"`[{0}]` {1} Fee — `-{2} DMK`
{3}";

        private const string BANK_BALANCE_NO_ENTRIES_MESSAGE = @"No ledger records found.

Your account is active, but no deposits, withdrawals, fees, or Ark financial operations have been processed yet.";

        public async Task SendBalanceToUser(DiscordUser user, DiscordGuild guild)
        {
            Log.Info($"Sending balance to user {user.Username}");
            if (SEDBStorage.Instance.Bank.Enabled)
            {
                if (SEDBStorage.Instance.Registry.IsUserRegistered(user.Id))
                {
                    Log.Info($"User is registred!");

                    BankAccount acc = null;
                    if (!SEDBStorage.Instance.Bank.UserHasAccount(user.Id))
                    {
                        Log.Info($"User has no acc, starting a new one!");
                        var registry = SEDBStorage.Instance.Registry.GetUserInfo(user.Id);
                        acc = SEDBStorage.Instance.Bank.CreateBankAccount(user.Id, registry.SteamId);
                    }
                    else
                    {
                        Log.Info($"Finding the user acc!");
                        acc = SEDBStorage.Instance.Bank.GetBankAccount(user.Id);
                    }
                    var msgItens = new StringBuilder();

                    if (acc.Transactions.Any())
                    {
                        var lastOperations = acc.Transactions.OrderByDescending(x => x.OperationDate).Take(10);
                        foreach (var item in lastOperations)
                        {
                            switch (item.OperationType)
                            {
                                case BankTransactionType.Deposit:
                                    msgItens.AppendLine(string.Format(
                                        BANK_BALANCE_DEPOSIT_ENTRY_MESSAGE,
                                        item.OperationDateValue,
                                        item.Value.ToString("N2"),
                                        item.ReferenceValue.ToString("N2")));
                                    break;
                                case BankTransactionType.Withdraw:
                                    msgItens.AppendLine(string.Format(
                                        BANK_BALANCE_WITHDRAWAL_ENTRY_MESSAGE,
                                        item.OperationDateValue,
                                        item.Value.ToString("N2"),
                                        item.ReferenceValue.ToString("N2")));
                                    break;
                                case BankTransactionType.Fee:
                                    msgItens.AppendLine(string.Format(
                                        BANK_BALANCE_FEE_ENTRY_MESSAGE,
                                        item.OperationDateValue,
                                        item.Name,
                                        item.Value.ToString("N2"),
                                        item.Description));
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        msgItens.AppendLine(BANK_BALANCE_NO_ENTRIES_MESSAGE);
                    }

                    var msgToSend = string.Format(BANK_BALANCE_DM_MESSAGE, acc.Balance, msgItens.ToString());
                    var member = await guild.GetMemberAsync(user.Id);
                    if (member != null)
                    {
                        await member.SendMessageAsync(msgToSend);
                        Log.Info($"Balance send to {user.Username}!");
                    }
                }
            }
        }

        private async Task RefreshBankChannel()
        {
            Log.Info("Refreshing Bank Channel...");
            if (!string.IsNullOrWhiteSpace(Config.BankChannelId) && SEDBStorage.Instance.Bank.Enabled)
            {
                Log.Info("Bank is enabled, updating channel...");
                var channel = DiscordBridge.Discord.GetChannelAsync(ulong.Parse(Config.BankChannelId)).Result;
                if (channel != null)
                {
                    Log.Info("Bank channel found, updating messages...");
                    // Limpa mensagens antigas
                    var needNewMessages = false;
                    var messages = await channel.GetMessagesAsync(100);
                    if (messages.Any())
                    {
                        Log.Info($"Found {messages.Count} messages in the channel, checking if they match expected count and IDs...");
                        // Calcula o total de mensagens que deveriam estar no canal (mensagem geral + mensagens de categoria)
                        var expectedMessageCount = 1; // 1 para a mensagem geral
                        // Verifica se todas as mensagens com ids salvos existem, caso contrário, limpa o canal para evitar mensagens desatualizadas
                        var ids = SEDBStorage.Instance.Bank.GetAllMessagesIds();
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
                    var startMsg = BANK_START_MESSAGE.Replace("/n", "\n");
                    if (needNewMessages)
                    {
                        Log.Info("Sending new start message to the channel...");
                        MsgWorker.SendToDiscord(channel, startMsg, true, (dMsg) =>
                        {
                            SEDBStorage.Instance.Bank.StartMsgId = dMsg.Id;
                            dMsg.CreateReactionAsync(DiscordBridge.MoneybagEmoji).Wait();
                        });
                    }
                    else
                    {
                        Log.Info("Updating existing start message...");
                        // Atualiza a mensagem geral
                        var generalMsgId = SEDBStorage.Instance.Bank.StartMsgId;
                        var generalMsg = messages.FirstOrDefault(m => m.Id == generalMsgId);
                        if (generalMsg != null && generalMsg.Content.CompareTo(startMsg) != 0)
                        {
                            await generalMsg.ModifyAsync(startMsg);
                        }
                        await generalMsg.DeleteAllReactionsAsync();
                        await generalMsg.CreateReactionAsync(DiscordBridge.MoneybagEmoji);
                    }
                }

            }
        }

        private const string PROFESSION_START_MESSAGE = @":identification_card: **THE SECOND DAWN — PROFESSION REQUISITION TERMINAL**

This terminal is maintained by **D.A.W.N. — Distributed Ark Watch Network**.

All registered members of the **Second Dawn Crew** may request an official Ark profession assignment. Professions represent your operational role within the Ark Initiative and provide real field modifiers while active in the current jump cycle.

Your first profession assignment is **free**.

Changing your profession after one has already been assigned requires administrative reassignment, training record updates, and Ark system recalibration. Profession reassignment costs:

`{0} DMK`

To choose or change your profession, react to the specific profession transmission below.

D.A.W.N. will process your request, update your Ark personnel record, apply the corresponding field modifiers, and publish the reassignment notice through the Ark alert network.

Choose carefully. Your profession defines how The Second Dawn recognizes your role among the stars.

---";

        private const string PROFESSION_ENTRY_MESSAGE = @"{0} **{1}**

{2}

**Field Modifiers:**

{3}
React to this message to request assignment as a **{4}**.

---";

        private const string PROFESSION_ASSIGNMENT_DM_MESSAGE = @":identification_card: **D.A.W.N. — PROFESSION ASSIGNMENT CONFIRMED**

Your first Ark profession assignment has been processed successfully.

**Assigned Profession:** `{0}`
**Assignment Cost:** `Free`
**Current Balance:** `{1} DMK`

Your personnel record has been updated inside the **Second Dawn Crew Registry**. The corresponding field modifiers are now active and will remain assigned to your profile until a new profession reassignment is requested.

**Active Field Modifiers:**

{2}

Welcome to your new operational role, explorer.

D.A.W.N. has published the assignment notice through the Ark alert network.";

        private const string PROFESSION_REASSIGNMENT_DM_MESSAGE = @":arrows_counterclockwise: **D.A.W.N. — PROFESSION REASSIGNMENT CONFIRMED**

Your Ark profession reassignment has been processed successfully.

**Previous Profession:** `{0}`
**New Profession:** `{1}`
**Reassignment Fee:** `{2} DMK`
**Previous Balance:** `{3} DMK`
**Current Balance:** `{4} DMK`

Your personnel record has been recalibrated inside the **Second Dawn Crew Registry**. Previous field modifiers have been revoked, and your new operational modifiers are now active.

**Active Field Modifiers:**

{5}

D.A.W.N. has published the reassignment notice through the Ark alert network.";

        private const string PROFESSION_REASSIGNMENT_DENIED_DM_MESSAGE = @":warning: **D.A.W.N. — PROFESSION REASSIGNMENT DENIED**

Your profession reassignment request could not be completed.

**Requested Profession:** `{0}`
**Required Fee:** `{1} DMK`
**Current Balance:** `{2} DMK`
**Missing Balance:** `{3} DMK`

Your current profession remains unchanged.

To complete this reassignment, deposit funds into your Ark Bank account and try again.

Use the following command inside the **Space Engineers in-game chat**:

`!ark bank deposit <AMOUNT>`

D.A.W.N. will continue to preserve your current personnel assignment until a valid reassignment request is processed.";

        private const string PROFESSION_REASSIGNMENT_NOTNEEDED_DM_MESSAGE = @":information_source: **D.A.W.N. — ASSIGNMENT ALREADY ACTIVE**

Your personnel record is already assigned as `{0}`.

No changes were made and no Dawn Marks were charged.";

        private const string PROFESSION_ALERT_MESSAGE = @":identification_card: D.A.W.N. Registry Update

Explorer **{0}** has joined the **{1} Division**.

The Second Dawn recognizes their new operational role.";

        public void AlertChangeProffesionIsCompleted(string userName, string profName)
        {
            if (!string.IsNullOrWhiteSpace(Config.AlertsChannelId) && SEDBStorage.Instance.Registry.Enabled)
            {
                var channel = DiscordBridge.Discord.GetChannelAsync(ulong.Parse(Config.AlertsChannelId)).Result;
                if (channel != null)
                {
                    Log.Info("Sending prof changed to the channel...");
                    MsgWorker.SendToDiscord(channel, string.Format(PROFESSION_ALERT_MESSAGE, userName, profName), true);
                }
            }
        }

        public async Task StartProfessionAcquisition(DiscordUser user, DiscordGuild guild, string professionId)
        {
            Log.Info($"Start profession acquisition to user {user.Username}");
            if (SEDBStorage.Instance.Profession.Enabled)
            {
                if (SEDBStorage.Instance.Registry.IsUserRegistered(user.Id))
                {
                    Log.Info($"User is registred!");
                    var registry = SEDBStorage.Instance.Registry.GetUserInfo(user.Id);

                    BankAccount acc = null;
                    if (!SEDBStorage.Instance.Bank.UserHasAccount(user.Id))
                    {
                        Log.Info($"User has no acc, starting a new one!");
                        acc = SEDBStorage.Instance.Bank.CreateBankAccount(user.Id, registry.SteamId);
                    }
                    else
                    {
                        Log.Info($"Finding the user acc!");
                        acc = SEDBStorage.Instance.Bank.GetBankAccount(user.Id);
                    }

                    var curProf = SEDBStorage.Instance.GetPlayerValue<string>(registry.SteamId, PlayerStorage.KEY_PROFESSION);
                    var hasProf = !string.IsNullOrWhiteSpace(curProf);

                    if (!ProfessionStorage.PROFESSIONS.ContainsKey(professionId))
                    {
                        Log.Warn($"Profession {professionId} not found!");
                        return;
                    }
                    var profInfo = ProfessionStorage.PROFESSIONS[professionId];
                    var buffs = new StringBuilder();
                    foreach (var bKey in profInfo.Buffs)
                    {
                        if (ProfessionStorage.BUFFS.ContainsKey(bKey))
                        {
                            buffs.AppendLine(ProfessionStorage.BUFFS[bKey].EffectDescription);
                        }
                    }

                    var msgToSend = "";
                    var doAlert = false;
                    if (hasProf)
                    {
                        if (curProf == professionId)
                        {
                            msgToSend = string.Format(PROFESSION_REASSIGNMENT_NOTNEEDED_DM_MESSAGE,
                                profInfo.Name.ToUpper());
                        }
                        else
                        {
                            if (acc.Balance < SEDBStorage.Instance.Profession.ChangeCost)
                            {
                                var needValue = SEDBStorage.Instance.Profession.ChangeCost - acc.Balance;
                                msgToSend = string.Format(PROFESSION_REASSIGNMENT_DENIED_DM_MESSAGE,
                                    profInfo.Name.ToUpper(),
                                    SEDBStorage.Instance.Profession.ChangeCost.ToString("N2"),
                                    acc.Balance.ToString("N2"),
                                    needValue.ToString("N2"));
                            }
                            else
                            {
                                var curProfInfo = ProfessionStorage.PROFESSIONS[curProf];
                                var oldBalance = acc.Balance;
                                if (acc.DoFee(SEDBStorage.Instance.Profession.ChangeCost, "Profession Reassignment Fee", $"Assigned to `{profInfo.Name.ToUpper()}`"))
                                {
                                    SEDBStorage.Instance.SetPlayerValue<string>(registry.SteamId, PlayerStorage.KEY_PROFESSION, professionId);
                                    msgToSend = string.Format(PROFESSION_REASSIGNMENT_DM_MESSAGE,
                                        curProfInfo.Name.ToUpper(),
                                        profInfo.Name.ToUpper(),
                                        SEDBStorage.Instance.Profession.ChangeCost.ToString("N2"),
                                        oldBalance.ToString("N2"),
                                        acc.Balance.ToString("N2"),
                                        buffs.ToString());
                                    doAlert = true;
                                }
                                else
                                {
                                    Log.Error($"Failed to change player {user.Username} profession to {profInfo.Name}");
                                }
                            }
                        }
                    }
                    else
                    {
                        SEDBStorage.Instance.SetPlayerValue<string>(registry.SteamId, PlayerStorage.KEY_PROFESSION, professionId);
                        msgToSend = string.Format(PROFESSION_ASSIGNMENT_DM_MESSAGE,
                            profInfo.Name.ToUpper(),
                            acc.Balance.ToString("N2"),
                            buffs.ToString());
                        doAlert = true;
                    }

                    var member = await guild.GetMemberAsync(user.Id);
                    if (member != null)
                    {
                        await member.SendMessageAsync(msgToSend);
                        Log.Info($"Profession DM send to {user.Username}!");
                    }
                    if (doAlert)
                    {
                        AlertChangeProffesionIsCompleted(user.Username, profInfo.Name);
                    }
                }
            }
        }

        private async Task RefreshProfessionChannel()
        {
            Log.Info("Refreshing Profession Channel...");
            if (!string.IsNullOrWhiteSpace(Config.ProfessionChannelId) && SEDBStorage.Instance.Bank.Enabled)
            {
                Log.Info("Profession is enabled, updating channel...");
                var channel = DiscordBridge.Discord.GetChannelAsync(ulong.Parse(Config.ProfessionChannelId)).Result;
                if (channel != null)
                {
                    Log.Info("Profession channel found, updating messages...");
                    // Limpa mensagens antigas
                    var needNewMessages = false;
                    var messages = await channel.GetMessagesAsync(100);
                    if (messages.Any())
                    {
                        Log.Info($"Found {messages.Count} messages in the channel, checking if they match expected count and IDs...");
                        // Calcula o total de mensagens que deveriam estar no canal (mensagem geral + mensagens de categoria)
                        var expectedMessageCount = 1 + ProfessionStorage.PROFESSIONS.Count; // 1 para a mensagem geral + QTD de Profissões
                        // Verifica se todas as mensagens com ids salvos existem, caso contrário, limpa o canal para evitar mensagens desatualizadas
                        var ids = SEDBStorage.Instance.Profession.GetAllMessagesIds();
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
                    var startMsg = string.Format(PROFESSION_START_MESSAGE.Replace("/n", "\n"), SEDBStorage.Instance.Profession.ChangeCost.ToString("N2"));
                    if (needNewMessages)
                    {
                        Log.Info("Sending new start message to the channel...");
                        MsgWorker.SendToDiscord(channel, startMsg, true, (dMsg) =>
                        {
                            SEDBStorage.Instance.Profession.StartMsgId = dMsg.Id;
                        });
                    }
                    else
                    {
                        Log.Info("Updating existing start message...");
                        // Atualiza a mensagem geral
                        var generalMsgId = SEDBStorage.Instance.Bank.StartMsgId;
                        var generalMsg = messages.FirstOrDefault(m => m.Id == generalMsgId);
                        if (generalMsg != null && generalMsg.Content.CompareTo(startMsg) != 0)
                        {
                            await generalMsg.ModifyAsync(startMsg);
                        }
                        await generalMsg.DeleteAllReactionsAsync();
                    }

                    foreach (var key in ProfessionStorage.PROFESSIONS.Keys)
                    {
                        var profInfo = ProfessionStorage.PROFESSIONS[key];

                        var buffs = new StringBuilder();
                        foreach (var bKey in profInfo.Buffs)
                        {
                            if (ProfessionStorage.BUFFS.ContainsKey(bKey))
                            {
                                buffs.AppendLine(ProfessionStorage.BUFFS[bKey].EffectDescription);
                            }
                        }

                        var profissionMessage = string.Format(PROFESSION_ENTRY_MESSAGE,
                            profInfo.Icon,
                            profInfo.Name.ToUpper(),
                            profInfo.Description,
                            buffs.ToString(),
                            profInfo.Name);
                        if (needNewMessages)
                        {
                            Log.Info($"Sending new message for profession {profInfo.Name} to the channel...");
                            MsgWorker.SendToDiscord(channel, profissionMessage, true, (dMsg) => {
                                SEDBStorage.Instance.Profession.ProfessionsMsgIds.Add(new ProfessionChatEntryId()
                                {
                                    ProfessionId = key,
                                    MsgId = dMsg.Id
                                });
                                dMsg.CreateReactionAsync(DiscordBridge.ReceiptEmoji).Wait();
                            });
                        }
                        else
                        {
                            Log.Info($"Updating existing message for category {profInfo.Name}...");
                            var profMsgId = SEDBStorage.Instance.Profession.ProfessionsMsgIds.FirstOrDefault(m => m.ProfessionId == key)?.MsgId;
                            if (profMsgId != null)
                            {
                                var profMsg = messages.FirstOrDefault(m => m.Id == profMsgId);
                                if (profMsg != null && profMsg.Content.CompareTo(profissionMessage) != 0)
                                {
                                    await profMsg.ModifyAsync(profissionMessage);
                                }
                                await profMsg.DeleteAllReactionsAsync();
                                await profMsg.CreateReactionAsync(DiscordBridge.ReceiptEmoji);
                            }
                        }

                    }
                }

            }
        }

        private bool _discordChannelsCanBeRefreshing = false;
        private bool _seasonMetaNeedRefreshing = false;
        private bool _registryNeedRefreshing = false;
        private bool _bankNeedRefreshing = false;
        private bool _professionNeedRefreshing = false;

        public void LoadSEDB()
        {
            if (DDBridge == null)
                DDBridge = new DiscordBridge(this);

            _discordChannelsCanBeRefreshing = false;
            _seasonMetaNeedRefreshing = true;
            _registryNeedRefreshing = true;
            _bankNeedRefreshing = true;
            _professionNeedRefreshing = true;

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
                    if (_bankNeedRefreshing)
                    {
                        _bankNeedRefreshing = false;
                        MyAPIGateway.Parallel.Start(() => {
                            RefreshBankChannel().Wait();
                        });
                    }
                    if (_professionNeedRefreshing)
                    {
                        _professionNeedRefreshing = false;
                        MyAPIGateway.Parallel.Start(() => {
                            RefreshProfessionChannel().Wait();
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
            if (_discordChannelsCanBeRefreshing)
            {
                if (_seasonMetaNeedRefreshing)
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
                if (_bankNeedRefreshing)
                {
                    _bankNeedRefreshing = false;
                    MyAPIGateway.Parallel.Start(() => {
                        RefreshBankChannel().Wait();
                    });
                }
                if (_professionNeedRefreshing)
                {
                    _professionNeedRefreshing = false;
                    MyAPIGateway.Parallel.Start(() => {
                        RefreshProfessionChannel().Wait();
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
