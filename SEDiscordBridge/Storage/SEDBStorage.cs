using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using SEDiscordBridge.Storage.SeasonMeta;
using SEDiscordBridge.Storage.Registry;
using SEDiscordBridge.Storage.Profession;
using SEDiscordBridge.Storage.Player;
using SEDiscordBridge.Storage.Base;
using SEDiscordBridge.Storage.Bank;
using System.Runtime.InteropServices.WindowsRuntime;
using Newtonsoft.Json;
using SEDiscordBridge.Storage.FunctionalGrids;
using SEDiscordBridge.Storage.Rankings;

namespace SEDiscordBridge.Storage
{

    public class SEDBStorage : BaseStorage
    {

        public const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";

        private const int CURRENT_VERSION = 1;
        private const string FILE_NAME = "SEDB.Storage.xml";
        private const string JSON_FILE_NAME = "SEDB.Storage.json";
        private const bool USE_JSON = true;

        private static SEDBStorage _instance;
        public static SEDBStorage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        private static bool Validate(SEDBStorage settings)
        {
            var res = true;
            return res;
        }

        private static SEDBStorage Upgrade(SEDBStorage settings)
        {

            return settings;
        }

        public static SEDBStorage Load()
        {
            _instance = Load(USE_JSON, FILE_NAME, JSON_FILE_NAME, CURRENT_VERSION, Validate, () => { return new SEDBStorage(); }, Upgrade);
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
                Logging.Instance.LogError(typeof(BaseStorage), e);
            }
            ServerFunctionalGridsStorage.Save();
            ProfessionStorage.Save();
            BankStorage.Save();
            RegistryStorage.Save();
            SeasonMetaConfigStorage.Save();
            SeasonMetaResultStorage.Save();
            RankingStorage.Save();
        }

        [XmlIgnore]
        [JsonIgnore]
        public ServerFunctionalGridsStorage FunctionalGrids
        {
            get
            {
                return ServerFunctionalGridsStorage.Instance;
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public ProfessionStorage Profession
        {
            get
            {
                return ProfessionStorage.Instance;
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public RankingStorage Ranking
        {
            get
            {
                return RankingStorage.Instance;
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public BankStorage Bank 
        { 
            get
            {
                return BankStorage.Instance;
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public RegistryStorage Registry
        {
            get
            {
                return RegistryStorage.Instance;
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public SeasonMetaConfigStorage SeasonMetaConfig
        {
            get
            {
                return SeasonMetaConfigStorage.Instance;
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public SeasonMetaResultStorage SeasonMetaResult
        {
            get
            {
                return SeasonMetaResultStorage.Instance;
            }
        }

        [XmlArray("Players"), XmlArrayItem("Player", typeof(PlayerStorage))]
        public List<PlayerStorage> Players { get; set; } = new List<PlayerStorage>();

        private void CheckPlayers()
        {
            if (Players == null)
                Players = new List<PlayerStorage>();
            Players.RemoveAll(x => x == null);
        }

        public PlayerStorage GetPlayer(ulong id)
        {
            CheckPlayers();
            if (Players.Any(x => x.SteamId == id))
                return Players.FirstOrDefault(x => x.SteamId == id);
            var storage = new PlayerStorage() { SteamId = id };
            lock (Players)
            {
                Players.Add(storage);
            }
            return storage;
        }

        public T GetPlayerValue<T>(ulong id, string key)
        {
            return GetPlayer(id).GetValue<T>(key);
        }

        public void SetPlayerValue<T>(ulong id, string key, T value)
        {
            GetPlayer(id).SetValue(key, value);
        }

        public void RemoveEntity(ulong id)
        {
            if (Players.Any(x => x.SteamId == id))
                lock (Players)
                {
                    Players.RemoveAll(x => x.SteamId == id);
                }
        }

    }

}
