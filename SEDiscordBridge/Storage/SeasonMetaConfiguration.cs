using System.Xml.Serialization;
using System.Collections.Generic;

namespace SEDiscordBridge.Patches
{

    public class SeasonMetaConfiguration
    {

        [XmlElement]
        public string Id { get; set; }

        [XmlElement]
        public string Name { get; set; }

        [XmlArray("Entries"), XmlArrayItem("Entry", typeof(SeasonMetaEntry))]
        public List<SeasonMetaEntry> Entries { get; set; } = new List<SeasonMetaEntry>();

    }

}
