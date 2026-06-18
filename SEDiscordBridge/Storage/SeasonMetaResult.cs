using System.Xml.Serialization;
using System.Collections.Generic;
using System;
using System.ComponentModel;

namespace SEDiscordBridge.Patches
{
    public class SeasonMetaResult
    {

        private const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";

        [XmlElement]
        public string Id { get; set; }

        [XmlElement]
        public string TargetConfiguration { get; set; }
        
        [XmlElement]
        public string LastCheckpointValue { get; set; }

        [XmlIgnore]
        public DateTime? LastCheckpoint 
        { 
            get
            {
                if (DateTime.TryParseExact(LastCheckpointValue, DATE_FORMAT, null, System.Globalization.DateTimeStyles.None, out var result))
                    return result;
                return null;
            }
            set
            {
                LastCheckpointValue = value?.ToString(DATE_FORMAT);
            }
        }

        [XmlElement]
        public string SeasonStartValue { get; set; }

        [XmlIgnore]
        public DateTime? SeasonStart
        {
            get
            {
                if (DateTime.TryParseExact(SeasonStartValue, DATE_FORMAT, null, System.Globalization.DateTimeStyles.None, out var result))
                    return result;
                return null;
            }
            set
            {
                SeasonStartValue = value?.ToString(DATE_FORMAT);
            }
        }

        [XmlArray("Entries"), XmlArrayItem("Entry", typeof(SeasonMetaEntry))]
        public List<SeasonMetaEntry> Entries { get; set; } = new List<SeasonMetaEntry>();

    }

}
