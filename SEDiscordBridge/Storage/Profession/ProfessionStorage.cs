using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.Profession
{

    public class ArkGridStorage
    {

        [XmlElement]
        public long EntityId { get; set; }

    }

    public class ProfessionStorage
    {

        public class ProfessionBuff
        {

            public string Id { get; set; }
            public string Name { get; set; }
            public string EffectDescription { get; set; }

        }

        public const string KEY_HARD_SURVIVAL = "HARD_SURVIVAL";
        public const string KEY_RAD_AWAY = "RAD_AWAY";
        public const string KEY_HUNTER_MARK = "HUNTER_MARK";
        public const string KEY_BUTCHER = "BUTCHER";
        public const string KEY_HARD_SKIN = "HARD_SKIN";
        public const string KEY_KILL_MACHINE = "KILL_MACHINE";
        public const string KEY_TRUE_MINER = "TRUE_MINER";

        public static readonly ProfessionBuff HARD_SURVIVAL_INFO = new ProfessionBuff()
        {
            Id = KEY_HARD_SURVIVAL,
            Name = "Hard Survival",
            EffectDescription = "`-50%` water and food loss over time"
        };

        public static readonly ProfessionBuff RAD_AWAY_INFO = new ProfessionBuff()
        {
            Id = KEY_RAD_AWAY,
            Name = "Rad Away",
            EffectDescription = "`+25%` radiation resistance"
        };

        public static readonly ProfessionBuff HUNTER_MARK_INFO = new ProfessionBuff()
        {
            Id = KEY_HUNTER_MARK,
            Name = "Hunter Mark",
            EffectDescription = "`+75%` damage against animals with handheld weapons"
        };

        public static readonly ProfessionBuff BUTCHER_INFO = new ProfessionBuff()
        {
            Id = KEY_BUTCHER,
            Name = "Butcher",
            EffectDescription = "`+150%` loot from animals killed with handheld weapons"
        };

        public static readonly ProfessionBuff HARD_SKIN_INFO = new ProfessionBuff()
        {
            Id = KEY_HARD_SKIN,
            Name = "Hard Skin",
            EffectDescription = "`-25%` general damage received"
        };

        public static readonly ProfessionBuff KILL_MACHINE_INFO = new ProfessionBuff()
        {
            Id = KEY_KILL_MACHINE,
            Name = "Kill Machine",
            EffectDescription = "`+50%` damage with handheld weapons"
        };

        public static readonly ProfessionBuff TRUE_MINER_INFO = new ProfessionBuff()
        {
            Id = KEY_TRUE_MINER,
            Name = "True Miner",
            EffectDescription = "`+20%` ore collection"
        };

        [XmlIgnore]
        [JsonIgnore]
        public static readonly Dictionary<string, ProfessionBuff> BUFFS = new Dictionary<string, ProfessionBuff>()
        {
            { KEY_HARD_SURVIVAL, HARD_SURVIVAL_INFO },
            { KEY_RAD_AWAY, RAD_AWAY_INFO },
            { KEY_HUNTER_MARK, HUNTER_MARK_INFO },
            { KEY_BUTCHER, BUTCHER_INFO },
            { KEY_HARD_SKIN, HARD_SKIN_INFO },
            { KEY_KILL_MACHINE, KILL_MACHINE_INFO },
            { KEY_TRUE_MINER, TRUE_MINER_INFO }
        };

        public class ProfessionInfo
        {
            public string Id { get; set; }
            public string Icon { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> Buffs { get; set; }
        }

        public const string KEY_PATHFINDER_ID = "PATHFINDER";
        public const string KEY_OUTRIDER_ID = "OUTRIDER";
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
                KEY_HARD_SURVIVAL,
                KEY_RAD_AWAY
            }
        };

        public static readonly ProfessionInfo OUTRIDER_INFO = new ProfessionInfo()
        {
            Id = KEY_OUTRIDER_ID,
            Icon = ":bow_and_arrow:",
            Name = "Outrider",
            Description = @"Outriders are wilderness control specialists assigned to hostile fauna zones and biological threat regions.

They protect survey teams, recover organic materials, and keep remote outposts supplied when local wildlife becomes a danger to Ark operations. Outriders are not simple hunters — they are field harvesters, trackers, and survival combatants trained to turn hostile environments into usable resources.",
            Buffs = new List<string>()
            {
                KEY_HUNTER_MARK,
                KEY_BUTCHER
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
                KEY_HARD_SKIN,
                KEY_KILL_MACHINE
            }
        };

        public static readonly ProfessionInfo PROSPECTOR_INFO = new ProfessionInfo()
        {
            Id = KEY_VANGUARD_ID,
            Icon = ":pick:",
            Name = "Prospector",
            Description = @"Prospectors keep the Ark alive.

Every jump requires fuel, metal, components, replacement parts, structural reserves, and industrial stockpiles. Prospectors are certified extraction specialists trained to identify, recover, and process raw resources at maximum efficiency. Whether operating by hand or piloting a mining vessel, their work feeds every system aboard The Second Dawn.

Without Prospectors, there is no next jump.",
            Buffs = new List<string>()
            {
                KEY_TRUE_MINER
            }
        };

        [XmlIgnore]
        [JsonIgnore]
        public static readonly Dictionary<string, ProfessionInfo> PROFESSIONS = new Dictionary<string, ProfessionInfo>()
        {
            { KEY_PATHFINDER_ID, PATHFINDER_INFO },
            { KEY_OUTRIDER_ID, OUTRIDER_INFO },
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

        public HashSet<ulong> GetAllMessagesIds()
        {
            var res = new HashSet<ulong>();
            if (StartMsgId != 0)
                res.Add(StartMsgId);
            res.UnionWith(ProfessionsMsgIds.Select(x => x.MsgId));
            return res;
        }

    }

}
