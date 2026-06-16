using System.Xml.Serialization;

namespace SEDiscordBridge.Patches
{
    public class PlayerStorageValue
    {

        [XmlElement]
        public string Key { get; set; }

        [XmlElement]
        public string Value { get; set; }

    }

}
