using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.SeasonMeta
{
    public class SeasonMetaChatMessagesIds
    {

        [XmlElement]
        public ulong StartMsgId { get; set; }

        [XmlElement]
        public ulong OverAllMsgId { get; set; }

        [XmlArray("CategoriesMsgIds"), XmlArrayItem("MsgId", typeof(SeasonMetaChatEntryId))]
        public List<SeasonMetaChatEntryId> CategoriesMsgIds { get; set; } = new List<SeasonMetaChatEntryId>();

        public HashSet<ulong> GetAllMessagesIds()
        {
            var res = new HashSet<ulong>();
            if (StartMsgId != 0)
                res.Add(StartMsgId);
            if (OverAllMsgId != 0)
                res.Add(OverAllMsgId);
            res.UnionWith(CategoriesMsgIds.Select(x => x.MsgId));
            return res;
        }

    }

}
