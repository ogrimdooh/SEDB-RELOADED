using System.Xml.Serialization;

namespace SEDiscordBridge.Patches
{
    public class ProfessionChatEntryId
    {

        [XmlElement]
        public string ProfessionId { get; set; }

        [XmlElement]
        public ulong MsgId { get; set; }

    }

}
