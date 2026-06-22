using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.SeasonMeta
{
    public class SeasonSimpleMetaEntry
    {

        [XmlElement]
        public string CategoryId { get; set; }

        [XmlElement]
        public long Amount { get; set; }

    }

}
