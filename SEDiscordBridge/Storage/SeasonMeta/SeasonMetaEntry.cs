using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.SeasonMeta
{

    public class SeasonMetaEntry : SeasonSimpleMetaEntry
    {

        [XmlElement]
        public int Weight { get; set; }
        
    }

}
