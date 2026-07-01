using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System;
using Newtonsoft.Json;
using VRage.Game.ObjectBuilders.Definitions;

namespace SEDiscordBridge.Storage.Player
{

    public class PlayerStorage
    {

        public const float EXP_BY_KILL = 500f;
        public const float EXP_BY_NPC_KILL = 50f;

        private const string KEY_DID_JUMP = "DID_JUMP";
        private const string KEY_JUMP_COUNT = "JUMP_COUNT";
        private const string KEY_FIRST_SPAWN = "FIRST_SPAWN";

        private const string KEY_DID_REGISTERLOCATION = "DID_REGISTERLOCATION";
        private const string KEY_LASTLOCATION_ISGRAVITY = "LASTLOCATION_ISGRAVITY";
        private const string KEY_LASTLOCATION_ENTITYID = "LASTLOCATION_ENTITYID";
        private const string KEY_LOCATION_VISITED = "LOCATION_VISITED_{0}";

        private const string KEY_DID_KILL = "DID_KILL";
        private const string KEY_KILL_COUNT = "KILL_COUNT";
        private const string KEY_NPC_KILL_COUNT = "NPC_KILL_COUNT";

        private const string KEY_REPUTATION = "REPUTATION";
        private const string KEY_KARMA = "KARMA";

        private const string KEY_DID_COMPLETE_CONTRACT = "DID_COMPLETE_CONTRACT";
        private const string KEY_COMPLETE_CONTRACT_COUNT = "COMPLETE_CONTRACT_COUNT_{0}";
        private const string KEY_ALL_CONTRACTS_COUNT = "COMPLETE_CONTRACT_COUNT_ALL";

        private const string KEY_PROFESSION = "PROFESSION";
        private const string KEY_LAST_RESPAWN_GRID = "LAST_RESPAWN_GRID";

        [XmlIgnore]
        [JsonIgnore]
        public bool FirstSpawn
        {
            get
            {
                return GetValue<bool>(KEY_FIRST_SPAWN);
            }
            set
            {
                SetValue(KEY_FIRST_SPAWN, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public bool DidJump
        {
            get
            {
                return GetValue<bool>(KEY_DID_JUMP);
            }
            set
            {
                SetValue(KEY_DID_JUMP, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public int JumpCount
        {
            get
            {
                return GetValue<int>(KEY_JUMP_COUNT);
            }
            set
            {
                SetValue(KEY_JUMP_COUNT, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public bool DidRegistrationLocation
        {
            get
            {
                return GetValue<bool>(KEY_DID_REGISTERLOCATION);
            }
            set
            {
                SetValue<bool>(KEY_DID_REGISTERLOCATION, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public bool LastLocationIsGravity
        {
            get
            {
                return GetValue<bool>(KEY_LASTLOCATION_ISGRAVITY);
            }
            set
            {
                SetValue<bool>(KEY_LASTLOCATION_ISGRAVITY, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public long LastLocationEntityId
        {
            get
            {
                return GetValue<long>(KEY_LASTLOCATION_ENTITYID);
            }
            set
            {
                SetValue<long>(KEY_LASTLOCATION_ENTITYID, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public bool DidKill
        {
            get
            {
                return GetValue<bool>(KEY_DID_KILL);
            }
            set
            {
                SetValue(KEY_DID_KILL, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public int KillCount
        {
            get
            {
                return GetValue<int>(KEY_KILL_COUNT);
            }
            set
            {
                SetValue(KEY_KILL_COUNT, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public int NpcKillCount
        {
            get
            {
                return GetValue<int>(KEY_NPC_KILL_COUNT);
            }
            set
            {
                SetValue(KEY_NPC_KILL_COUNT, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public bool DidCompleteContract
        {
            get
            {
                return GetValue<bool>(KEY_DID_COMPLETE_CONTRACT);
            }
            set
            {
                SetValue(KEY_DID_COMPLETE_CONTRACT, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public string Profession
        {
            get
            {
                return GetValue<string>(KEY_PROFESSION);
            }
            set
            {
                SetValue(KEY_PROFESSION, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public long LastRespawnGrid
        {
            get
            {
                return GetValue<long>(KEY_LAST_RESPAWN_GRID);
            }
            set
            {
                SetValue(KEY_LAST_RESPAWN_GRID, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public int AllContractsCount
        {
            get
            {
                return GetValue<int>(KEY_ALL_CONTRACTS_COUNT);
            }
            set
            {
                SetValue(KEY_ALL_CONTRACTS_COUNT, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public long Reputation
        {
            get
            {
                return GetValue<long>(KEY_REPUTATION);
            }
            set
            {
                SetValue(KEY_REPUTATION, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public long Karma
        {
            get
            {
                return GetValue<long>(KEY_KARMA);
            }
            set
            {
                SetValue(KEY_KARMA, value);
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public long FinalReputation
        {
            get
            {
                return Reputation - Karma;
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public float KillExperience
        {
            get
            {
                return (KillCount * EXP_BY_KILL) + (NpcKillCount * EXP_BY_NPC_KILL);
            }
        }
        
        [XmlIgnore]
        [JsonIgnore]
        public float FinalExperience
        {
            get
            {
                return KillExperience + Reputation;
            }
        }

        [XmlElement]
        public ulong SteamId { get; set; }

        [XmlArray("Storage"), XmlArrayItem("Entry", typeof(PlayerStorageValue))]
        public List<PlayerStorageValue> Values { get; set; } = new List<PlayerStorageValue>();

        public bool GetLocationVisited(long entityId)
        {
            return GetValue<bool>(string.Format(KEY_LOCATION_VISITED, entityId));
        }

        public void SetLocationVisited(long entityId, bool visited)
        {
            SetValue(string.Format(KEY_LOCATION_VISITED, entityId), visited);
        }

        public int GetCompleteContractCount(MyContractStrategyType contractStrategy)
        {
            return GetValue<int>(string.Format(KEY_COMPLETE_CONTRACT_COUNT, contractStrategy.ToString().ToUpper()));
        }

        public void SetCompleteContractCount(MyContractStrategyType contractStrategy, int count)
        {
            SetValue(string.Format(KEY_COMPLETE_CONTRACT_COUNT, contractStrategy.ToString().ToUpper()), count);
        }

        public T GetValue<T>(string key)
        {
            try
            {
                if (Values.Any(x => x.Key == key))
                    return (T)Convert.ChangeType(Values.FirstOrDefault(x => x.Key == key).Value, typeof(T));
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(GetType(), e);
            }
            return default;
        }

        public void SetValue<T>(string key, T value)
        {
            PlayerStorageValue entry = null;
            if (Values.Any(x => x.Key == key))
            {
                entry = Values.FirstOrDefault(x => x.Key == key);
                lock (Values)
                {
                    entry.Value = value?.ToString();
                }
            }
            else
            {
                entry = new PlayerStorageValue() { Key = key, Value = value?.ToString() };
                lock (Values)
                {
                    Values.Add(entry);
                }
            }
        }

        public PlayerStorage()
        {
            Values = new List<PlayerStorageValue>();
        }

    }

}
