using System.Xml.Serialization;

namespace SEDiscordBridge.Patches
{

    public class SeasonMetaEntry
    {

        [XmlElement]
        public string CategoryId { get; set; }

        [XmlElement]
        public long Amount { get; set; }

        [XmlElement]
        public int Weight { get; set; }
        
    }

}
