using SEDiscordBridge.Storage.Base;
using SEDiscordBridge.Storage.FunctionalGrids;
using SEDiscordBridge.Storage.Player;
using SEDiscordBridge.Storage.Profession;
using SEDiscordBridge.Storage.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.Rankings
{

    public class RankingStorage : BaseStorage
    {

        private const int CURRENT_VERSION = 1;
        private const string FILE_NAME = "SEDB.Ranking.Storage.xml";
        private const string JSON_FILE_NAME = "SEDB.Ranking.Storage.json";
        private const bool USE_JSON = true;

        private static RankingStorage _instance;
        public static RankingStorage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        private static bool Validate(RankingStorage settings)
        {
            var res = true;
            return res;
        }

        private static RankingStorage Upgrade(RankingStorage settings)
        {

            return settings;
        }

        public static RankingStorage Load()
        {
            _instance = Load(USE_JSON, FILE_NAME, JSON_FILE_NAME, CURRENT_VERSION, Validate, () => { return new RankingStorage(); }, Upgrade);
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
                Logging.Instance.LogError(typeof(RankingStorage), e);
            }
        }

        [XmlElement]
        public bool Enabled { get; set; } = true;

        [XmlElement]
        public ulong StartMsgId { get; set; }

        [XmlArray("RankingsMsgIds"), XmlArrayItem("MsgId", typeof(RankingChatEntryId))]
        public List<RankingChatEntryId> RankingsMsgIds { get; set; } = new List<RankingChatEntryId>();

        public HashSet<ulong> GetAllMessagesIds()
        {
            var res = new HashSet<ulong>();
            if (StartMsgId != 0)
                res.Add(StartMsgId);
            res.UnionWith(RankingsMsgIds.Select(x => x.MsgId));
            return res;
        }

    }

}
