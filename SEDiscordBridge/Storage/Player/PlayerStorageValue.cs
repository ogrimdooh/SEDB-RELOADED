using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.Player
{
    public class PlayerStorageValue
    {

        [XmlElement]
        public string Key { get; set; }

        [XmlElement]
        public string Value { get; set; }

    }

}
