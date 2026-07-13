using Newtonsoft.Json;
using Sandbox.Game;
using Sandbox.Game.Contracts;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.World;
using SEDiscordBridge.Extensions;
using SEDiscordBridge.Storage.Bank;
using SEDiscordBridge.Storage.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using SEDiscordBridge.Controllers;

namespace SEDiscordBridge.Storage.Profession
{

    public class ProfessionStorage : BaseStorage
    {

        private const int CURRENT_VERSION = 1;
        private const string FILE_NAME = "SEDB.Profession.Storage.xml";
        private const string JSON_FILE_NAME = "SEDB.Profession.Storage.json";
        private const bool USE_JSON = true;

        private static ProfessionStorage _instance;
        public static ProfessionStorage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        private static bool Validate(ProfessionStorage settings)
        {
            var res = true;
            return res;
        }

        private static ProfessionStorage Upgrade(ProfessionStorage settings)
        {

            return settings;
        }

        public static ProfessionStorage Load()
        {
            _instance = Load(USE_JSON, FILE_NAME, JSON_FILE_NAME, CURRENT_VERSION, Validate, () => { return new ProfessionStorage(); }, Upgrade);
            return _instance;
        }

        public static void Save()
        {
            try
            {
                Save(Instance, USE_JSON, FILE_NAME, JSON_FILE_NAME);
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(ProfessionStorage), e);
            }
        }

        [Flags]
        public enum BuffInterationType
        {
            None = 0,

            OnContractFinish = 1 << 1,
            OnGrinding = 1 << 2,
        }

        public class ProfessionBuff
        {

            public string Id { get; set; }
            public string Name { get; set; }
            public string EffectDescription { get; set; }
            public BuffInterationType Interation { get; set; }
            public Action<ulong, BuffInterationType, IDictionary<string, object>> OnInteract { get; set; }

        }

        public const string KEY_HARD_TO_KILL = "HARD_TO_KILL";
        public const string KEY_BETTER_REWARDS_SH = "BETTER_REWARDS_SH";
        public const string KEY_SCRAPING_TREASURE = "SCRAPING_TREASURE";
        public const string KEY_BETTER_REWARDS_RS = "BETTER_REWARDS_RS";
        public const string KEY_KILL_MACHINE = "KILL_MACHINE";
        public const string KEY_BETTER_REWARDS_B = "BETTER_REWARDS_B";
        public const string KEY_TRUE_MINER = "TRUE_MINER";
        public const string KEY_BETTER_REWARDS_A = "BETTER_REWARDS_A";

        public static readonly ProfessionBuff HARD_TO_KILL_INFO = new ProfessionBuff()
        {
            Id = KEY_HARD_TO_KILL,
            Name = "Hard to Kill",
            EffectDescription = "`-25%` damage received by player or ship"
        };

        public static readonly ProfessionBuff BETTER_REWARDS_SH_INFO = new ProfessionBuff()
        {
            Id = KEY_BETTER_REWARDS_SH,
            Name = "Better Rewards (Search & Hauling)",
            EffectDescription = "`+25-50%` Ark contract rewards for **Search** and **Hauling** contracts",
            Interation = BuffInterationType.OnContractFinish,
            OnInteract = (ulong steamId, BuffInterationType interactionType, IDictionary<string, object> data) =>
            {
                if (interactionType == BuffInterationType.OnContractFinish)
                {
                    if (data.TryGetValue("contract", out var contractObj) && contractObj is MyContract contract)
                    {
                        if (contract is MyContractFind || contract is MyContractGridHauling)
                        {
                            var extraReward = contract.RewardMoney * MyUtils.GetRandomFloat(0.25f, 0.5f); // 25-50% bonus
                            var playerId = MySession.Static.Players.TryGetIdentityId(steamId);
                            if (MyBankingSystem.ChangeBalance(playerId, (long)extraReward))
                            {
                                Logging.Instance.LogInfo(typeof(ProfessionStorage), $"Applied Better Rewards (Search & Hauling) bonus of {extraReward} to player {steamId} for contract {contract.Id}");
                            }
                        }
                    }
                }
            }
        };

        public static readonly ProfessionBuff SCRAPING_TREASURE_INFO = new ProfessionBuff()
        {
            Id = KEY_SCRAPING_TREASURE,
            Name = "Scraping Treasure",
            EffectDescription = "`+25%` chance to recover `1-5%` extra parts when dismantling blocks not owned by you",
            Interation = BuffInterationType.OnGrinding,
            OnInteract = (ulong steamId, BuffInterationType interactionType, IDictionary<string, object> data) =>
            {
                if (interactionType == BuffInterationType.OnGrinding)
                {
                    if (data.TryGetValue("block", out var blockObj) && blockObj is MySlimBlock block)
                    {
                        if (data.TryGetValue("stockPile", out var stockPileObj) && stockPileObj is Dictionary<MyDefinitionId, int> stockPile)
                        {
                            if (data.TryGetValue("toInventory", out var toInventoryObj) && toInventoryObj is MyInventory toInventory)
                            {
                                foreach (var itemId in stockPile.Keys)
                                {
                                    var chance = 0.25f; // 25% chance
                                    if (MyUtils.GetRandomFloat(0f, 1f) <= chance)
                                    {
                                        var extraPartsMultiplier = MyUtils.GetRandomFloat(0.01f, 0.05f); // Random multiplier between 1% and 5%
                                        var extraParts = Math.Max((int)(stockPile[itemId] * extraPartsMultiplier), 1);
                                        // Grant extra parts to the player
                                        var addParts = toInventory.AddMaxItems((float)extraParts, ItensConstants.GetPhysicalObjectBuilder(new Entities.Base.UniqueEntityId(itemId)), false);
                                        if (addParts == 0)
                                        {
                                            Logging.Instance.LogInfo(typeof(ProfessionStorage), $"Scraping Treasure: Could not add extra parts for item {itemId} to player {steamId}'s inventory.");
                                            break;
                                        }
                                        else
                                        {
                                            Logging.Instance.LogInfo(typeof(ProfessionStorage), $"Scraping Treasure: Granted {addParts} extra parts of item {itemId} to player {steamId}'s inventory.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        public static readonly ProfessionBuff BETTER_REWARDS_RS_INFO = new ProfessionBuff()
        {
            Id = KEY_BETTER_REWARDS_RS,
            Name = "Better Rewards (Repair & Salvage)",
            EffectDescription = "`+25-50%` Ark contract rewards for **Repair** and **Salvage** contracts",
            Interation = BuffInterationType.OnContractFinish,
            OnInteract = (ulong steamId, BuffInterationType interactionType, IDictionary<string, object> data) =>
            {
                if (interactionType == BuffInterationType.OnContractFinish)
                {
                    if (data.TryGetValue("contract", out var contractObj) && contractObj is MyContract contract)
                    {
                        if (contract is MyContractRepair || contract is MyContractSalvage)
                        {
                            var extraReward = contract.RewardMoney * MyUtils.GetRandomFloat(0.25f, 0.5f); // 25-50% bonus
                            var playerId = MySession.Static.Players.TryGetIdentityId(steamId);
                            if (MyBankingSystem.ChangeBalance(playerId, (long)extraReward))
                            {
                                Logging.Instance.LogInfo(typeof(ProfessionStorage), $"Applied Better Rewards (Repair & Salvage) bonus of {extraReward} to player {steamId} for contract {contract.Id}");
                            }
                        }
                    }
                }
            }
        };

        public static readonly ProfessionBuff KILL_MACHINE_INFO = new ProfessionBuff()
        {
            Id = KEY_KILL_MACHINE,
            Name = "Kill Machine",
            EffectDescription = "`+25%` damage with any weapons, including handheld and ship weapons"
        };

        public static readonly ProfessionBuff BETTER_REWARDS_B_INFO = new ProfessionBuff()
        {
            Id = KEY_BETTER_REWARDS_B,
            Name = "Better Rewards (Bounty)",
            EffectDescription = "`+25-50%` Ark contract rewards for **Bounty** contracts",
            Interation = BuffInterationType.OnContractFinish,
            OnInteract = (ulong steamId, BuffInterationType interactionType, IDictionary<string, object> data) =>
            {
                if (interactionType == BuffInterationType.OnContractFinish)
                {
                    if (data.TryGetValue("contract", out var contractObj) && contractObj is MyContract contract)
                    {
                        if (contract is MyContractPvEBounty)
                        {
                            var extraReward = contract.RewardMoney * MyUtils.GetRandomFloat(0.25f, 0.5f); // 25-50% bonus
                            var playerId = MySession.Static.Players.TryGetIdentityId(steamId);
                            if (MyBankingSystem.ChangeBalance(playerId, (long)extraReward))
                            {
                                Logging.Instance.LogInfo(typeof(ProfessionStorage), $"Applied Better Rewards (Bounty) bonus of {extraReward} to player {steamId} for contract {contract.Id}");
                            }
                        }
                    }
                }
            }
        };

        public static readonly ProfessionBuff TRUE_MINER_INFO = new ProfessionBuff()
        {
            Id = KEY_TRUE_MINER,
            Name = "True Miner",
            EffectDescription = "`+15%` ore collection"
        };

        public static readonly ProfessionBuff BETTER_REWARDS_A_INFO = new ProfessionBuff()
        {
            Id = KEY_BETTER_REWARDS_A,
            Name = "Better Rewards (Acquisition)",
            EffectDescription = "`+25-50%` Ark contract rewards for **Acquisition** contracts",
            Interation = BuffInterationType.OnContractFinish,
            OnInteract = (ulong steamId, BuffInterationType interactionType, IDictionary<string, object> data) =>
            {
                if (interactionType == BuffInterationType.OnContractFinish)
                {
                    if (data.TryGetValue("contract", out var contractObj) && contractObj is MyContract contract)
                    {
                        if (contract is MyContractObtainAndDeliver)
                        {
                            var extraReward = contract.RewardMoney * MyUtils.GetRandomFloat(0.25f, 0.5f); // 25-50% bonus
                            var playerId = MySession.Static.Players.TryGetIdentityId(steamId);
                            if (MyBankingSystem.ChangeBalance(playerId, (long)extraReward))
                            {
                                Logging.Instance.LogInfo(typeof(ProfessionStorage), $"Applied Better Rewards (Acquisition) bonus of {extraReward} to player {steamId} for contract {contract.Id}");
                            }
                        }
                    }
                }
            }
        };

        [XmlIgnore]
        [JsonIgnore]
        public static readonly Dictionary<string, ProfessionBuff> BUFFS = new Dictionary<string, ProfessionBuff>()
        {
            { KEY_HARD_TO_KILL, HARD_TO_KILL_INFO },
            { KEY_BETTER_REWARDS_SH, BETTER_REWARDS_SH_INFO },
            { KEY_SCRAPING_TREASURE, SCRAPING_TREASURE_INFO },
            { KEY_BETTER_REWARDS_RS, BETTER_REWARDS_RS_INFO },
            { KEY_BETTER_REWARDS_B, BETTER_REWARDS_B_INFO },
            { KEY_KILL_MACHINE, KILL_MACHINE_INFO },
            { KEY_TRUE_MINER, TRUE_MINER_INFO },
            { KEY_BETTER_REWARDS_A, BETTER_REWARDS_A_INFO }
        };

        public class ProfessionInfo
        {
            public string Id { get; set; }
            public string Icon { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> Buffs { get; set; }

            public void ApplyBuffs(ulong steamId, BuffInterationType interactionType, IDictionary<string, object> data)
            {
                foreach (var buffId in Buffs)
                {
                    if (BUFFS.TryGetValue(buffId, out var buffInfo))
                    {
                        if (buffInfo.Interation.HasFlag(interactionType) && buffInfo.OnInteract != null)
                        {
                            buffInfo.OnInteract(steamId, interactionType, data);
                        }
                    }
                }
            }

            public void OnFinishContract(ulong steamId, MyContract contract)
            {
                var data = new Dictionary<string, object>
                {
                    { "contract", contract }
                };
                ApplyBuffs(steamId, BuffInterationType.OnContractFinish, data);
            }

            public void OnGrinding(ulong steamId, MySlimBlock block, Dictionary<MyDefinitionId, int> stockPile, MyInventoryBase toInventory)
            {
                var data = new Dictionary<string, object>
                {
                    { "block", block },
                    { "stockPile", stockPile },
                    { "toInventory", toInventory }
                };
                ApplyBuffs(steamId, BuffInterationType.OnGrinding, data);
            }
        }

        public const string KEY_PATHFINDER_ID = "PATHFINDER";
        public const string KEY_SALVAGER_ID = "SALVAGER";
        public const string KEY_VANGUARD_ID = "VANGUARD";
        public const string KEY_PROSPECTOR_ID = "PROSPECTOR";

        public static readonly ProfessionInfo PATHFINDER_INFO = new ProfessionInfo()
        {
            Id = KEY_PATHFINDER_ID,
            Icon = ":compass:",
            Name = "Pathfinder",
            Description = @"Pathfinders are the first boots beyond the landing zone.

They chart unstable terrain, scout hostile biomes, cross irradiated regions, and locate safe routes for crews, convoys, and recovery teams. When the Ark enters an unknown system, Pathfinders are often the first to leave the hangar and the last to return.

They are trained for endurance, exposure survival, and long-range reconnaissance.",
            Buffs = new List<string>()
            {
                KEY_HARD_TO_KILL,
                KEY_BETTER_REWARDS_SH
            }
        };

        public static readonly ProfessionInfo SALVAGER_INFO = new ProfessionInfo()
        {
            Id = KEY_SALVAGER_ID,
            Icon = ":wrench:",
            Name = "Salvager",
            Description = @"Salvagers are the recovery hands of The Second Dawn.

They enter wreckage fields, strip abandoned structures, recover usable components, and restore damaged assets before they are lost to the void. Where others see debris, Salvagers see replacement parts, repair stock, and another chance to keep the Ark moving.

They are trained in field dismantling, emergency repair, structural recovery, and salvage logistics.",
            Buffs = new List<string>()
            {
                KEY_SCRAPING_TREASURE,
                KEY_BETTER_REWARDS_RS
            }
        };

        public static readonly ProfessionInfo VANGUARD_INFO = new ProfessionInfo()
        {
            Id = KEY_VANGUARD_ID,
            Icon = ":crossed_swords:",
            Name = "Vanguard",
            Description = @"Vanguards are the shield line of The Second Dawn.

They are deployed when negotiations fail, when pirates breach the perimeter, or when exploration teams encounter threats too dangerous for civilian crews. Their duty is simple: hold the line, protect Ark personnel, and ensure critical operations survive contact with hostile forces.

Vanguards receive combat conditioning, defensive training, and authorization for frontline engagement.",
            Buffs = new List<string>()
            {
                KEY_KILL_MACHINE,
                KEY_BETTER_REWARDS_B
            }
        };

        public static readonly ProfessionInfo PROSPECTOR_INFO = new ProfessionInfo()
        {
            Id = KEY_PROSPECTOR_ID,
            Icon = ":pick:",
            Name = "Prospector",
            Description = @"Prospectors keep the Ark alive.

Every jump requires fuel, metal, components, replacement parts, structural reserves, and industrial stockpiles. Prospectors are certified extraction specialists trained to identify, recover, and process raw resources at maximum efficiency. Whether operating by hand or piloting a mining vessel, their work feeds every system aboard The Second Dawn.

Without Prospectors, there is no next jump.",
            Buffs = new List<string>()
            {
                KEY_TRUE_MINER,
                KEY_BETTER_REWARDS_A
            }
        };

        [XmlIgnore]
        [JsonIgnore]
        public static readonly Dictionary<string, ProfessionInfo> PROFESSIONS = new Dictionary<string, ProfessionInfo>()
        {
            { KEY_PATHFINDER_ID, PATHFINDER_INFO },
            { KEY_SALVAGER_ID, SALVAGER_INFO },
            { KEY_VANGUARD_ID, VANGUARD_INFO },
            { KEY_PROSPECTOR_ID, PROSPECTOR_INFO }
        };

        [XmlElement]
        public bool Enabled { get; set; } = true;

        [XmlElement]
        public ulong StartMsgId { get; set; }

        [XmlElement]
        public ulong ChangeCost { get; set; } = 5000;

        [XmlArray("ProfessionsMsgIds"), XmlArrayItem("MsgId", typeof(ProfessionChatEntryId))]
        public List<ProfessionChatEntryId> ProfessionsMsgIds { get; set; } = new List<ProfessionChatEntryId>();

        [XmlArray("DiscordInfos"), XmlArrayItem("Info", typeof(ProfessionDiscordInfo))]
        public List<ProfessionDiscordInfo> DiscordInfos { get; set; } = new List<ProfessionDiscordInfo>();

        public HashSet<ulong> GetAllMessagesIds()
        {
            var res = new HashSet<ulong>();
            if (StartMsgId != 0)
                res.Add(StartMsgId);
            res.UnionWith(ProfessionsMsgIds.Select(x => x.MsgId));
            return res;
        }

        public HashSet<ulong> GetOthersRolesIds(string professionId)
        {
            var res = new HashSet<ulong>();
            res.UnionWith(DiscordInfos.Where(x => x.ProfessionId != professionId).Select(x => x.RoleId));
            return res;
        }

        public ulong GetRolesId(string professionId)
        {
            return DiscordInfos.Where(x => x.ProfessionId == professionId).Select(x => x.RoleId).FirstOrDefault();
        }

    }

}
