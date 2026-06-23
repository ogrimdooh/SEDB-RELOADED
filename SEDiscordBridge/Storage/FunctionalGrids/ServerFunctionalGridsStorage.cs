using SEDiscordBridge.Storage.Base;
using SEDiscordBridge.Storage.Registry;
using System;
using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.FunctionalGrids
{

    public class ServerFunctionalGridsStorage : BaseStorage
    {

        private const int CURRENT_VERSION = 1;
        private const string FILE_NAME = "SEDB.FunctionalGrids.Storage.xml";
        private const string JSON_FILE_NAME = "SEDB.FunctionalGrids.Storage.json";
        private const bool USE_JSON = true;

        private static ServerFunctionalGridsStorage _instance;
        public static ServerFunctionalGridsStorage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        private static bool Validate(ServerFunctionalGridsStorage settings)
        {
            var res = true;
            return res;
        }

        private static ServerFunctionalGridsStorage Upgrade(ServerFunctionalGridsStorage settings)
        {

            return settings;
        }

        public static ServerFunctionalGridsStorage Load()
        {
            _instance = Load(USE_JSON, FILE_NAME, JSON_FILE_NAME, CURRENT_VERSION, Validate, () => { return new ServerFunctionalGridsStorage(); }, Upgrade);
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
                Logging.Instance.LogError(typeof(ServerFunctionalGridsStorage), e);
            }
        }

        [XmlElement]
        public long LogisticRelayEntityId { get; set; }

        [XmlElement]
        public long GroundBaseEntityId { get; set; }

    }

}
