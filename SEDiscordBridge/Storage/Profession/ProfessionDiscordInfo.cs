using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.Profession
{
    public class ProfessionDiscordInfo
    {

        [XmlElement]
        public string ProfessionId { get; set; }

        [XmlElement]
        public ulong RoleId { get; set; }

    }

}
