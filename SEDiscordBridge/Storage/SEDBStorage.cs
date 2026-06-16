using System;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace SEDiscordBridge.Patches
{

    public class SEDBStorage : BaseStorage
    {

        private const int CURRENT_VERSION = 1;
        private const string FILE_NAME = "SEDB.Storage.xml";

        public const string KEY_DID_JUMP = "KEY_DID_JUMP";
        public const string KEY_JUMP_COUNT = "KEY_JUMP_COUNT";

        public const string KEY_DID_REGISTERLOCATION = "KEY_DID_REGISTERLOCATION";
        public const string KEY_LASTLOCATION_ISGRAVITY = "KEY_LASTLOCATION_ISGRAVITY";
        public const string KEY_LASTLOCATION_ENTITYID = "KEY_LASTLOCATION_ENTITYID";
        public const string KEY_LOCATION_VISITED = "KEY_LOCATION_VISITED_{0}";

        public const string KEY_DID_KILL = "KEY_DID_KILL";
        public const string KEY_KILL_COUNT = "KEY_KILL_COUNT";
        
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
            _instance = Load(FILE_NAME, CURRENT_VERSION, Validate, () => { return new SEDBStorage(); }, Upgrade);
            return _instance;
        }

        public static void Save()
        {
            try
            {
                Save(Instance, FILE_NAME);
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(BaseStorage), e);
            }
        }

        [XmlElement]
        public SeasonMetaStorage SeasonMeta { get; set; } = new SeasonMetaStorage();

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
            GetPlayer(id).SetValue<T>(key, value);
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
