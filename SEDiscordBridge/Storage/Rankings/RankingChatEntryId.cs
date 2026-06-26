using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.Rankings
{
    public class RankingChatEntryId
    {

        [XmlElement]
        public string RankId { get; set; }

        [XmlElement]
        public ulong MsgId { get; set; }

    }

}
