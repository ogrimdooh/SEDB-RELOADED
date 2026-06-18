using System.Xml.Serialization;

namespace SEDiscordBridge.Patches
{
    public class SeasonMetaChatEntryId
    {

        [XmlElement]
        public string CategoryId { get; set; }

         [XmlElement]
        public ulong MsgId { get; set; }

    }

}
