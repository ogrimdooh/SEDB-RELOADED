using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.SeasonMeta
{
    public class SeasonMetaChatEntryId
    {

        [XmlElement]
        public string CategoryId { get; set; }

         [XmlElement]
        public ulong MsgId { get; set; }

    }

}
