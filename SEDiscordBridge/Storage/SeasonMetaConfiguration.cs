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

        [XmlElement]
        public long HoursBetweenCheckpoints { get; set; } = 24;

        [XmlElement]
        public long TotalCheckpoints { get; set; } = 16;

        [XmlArray("Entries"), XmlArrayItem("Entry", typeof(SeasonMetaEntry))]
        public List<SeasonMetaEntry> Entries { get; set; } = new List<SeasonMetaEntry>();

    }

}
