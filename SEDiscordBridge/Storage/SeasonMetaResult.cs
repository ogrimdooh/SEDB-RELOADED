using System.Xml.Serialization;
using System.Collections.Generic;

namespace SEDiscordBridge.Patches
{
    public class SeasonMetaResult
    {

        [XmlElement]
        public string Id { get; set; }

        [XmlElement]
        public string TargetConfiguration { get; set; }

        [XmlArray("Entries"), XmlArrayItem("Entry", typeof(SeasonMetaEntry))]
        public List<SeasonMetaEntry> Entries { get; set; } = new List<SeasonMetaEntry>();

    }

}
