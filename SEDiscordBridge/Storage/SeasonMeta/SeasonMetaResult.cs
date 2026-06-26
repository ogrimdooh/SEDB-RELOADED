using System.Xml.Serialization;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Linq;
using SEDiscordBridge.Storage;

namespace SEDiscordBridge.Storage.SeasonMeta
{
    public class SeasonMetaResult
    {

        [XmlElement]
        public string Id { get; set; }

        [XmlElement]
        public string TargetConfiguration { get; set; }
        
        [XmlElement]
        public string LastCheckpointValue { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public DateTime? LastCheckpoint
        {
            get
            {
                if (DateTime.TryParseExact(LastCheckpointValue, SEDBStorage.DATE_FORMAT, null, System.Globalization.DateTimeStyles.None, out var result))
                    return result;
                return null;
            }
            set
            {
                LastCheckpointValue = value?.ToString(SEDBStorage.DATE_FORMAT);
            }
        }

        [XmlElement]
        public string LastManifestUpdateValue { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public DateTime? LastManifestUpdate
        { 
            get
            {
                if (DateTime.TryParseExact(LastManifestUpdateValue, SEDBStorage.DATE_FORMAT, null, System.Globalization.DateTimeStyles.None, out var result))
                    return result;
                return null;
            }
            set
            {
                LastManifestUpdateValue = value?.ToString(SEDBStorage.DATE_FORMAT);
            }
        }

        [XmlElement]
        public string SeasonStartValue { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public DateTime? SeasonStart
        {
            get
            {
                if (DateTime.TryParseExact(SeasonStartValue, SEDBStorage.DATE_FORMAT, null, System.Globalization.DateTimeStyles.None, out var result))
                    return result;
                return null;
            }
            set
            {
                SeasonStartValue = value?.ToString(SEDBStorage.DATE_FORMAT);
            }
        }

        [XmlArray("Entries"), XmlArrayItem("Entry", typeof(SeasonSimpleMetaEntry))]
        public List<SeasonSimpleMetaEntry> Entries { get; set; } = new List<SeasonSimpleMetaEntry>();

        [XmlArray("Donations"), XmlArrayItem("Donation", typeof(SeasonMetaEntry))]
        public List<SeasonMetaDonationEntry> Donations { get; set; } = new List<SeasonMetaDonationEntry>();

        public void AddValueToEntry(string categoryId, long amount, float weight)
        {
            var item = Entries.FirstOrDefault(x => x.CategoryId == categoryId);
            if (item != null)
            {
                item.Amount += (long)(amount * weight);
            }
        }

        public List<SeasonMetaDonationEntry> GetDonationsSiceLastManifest(params SeasonMetaDonationOrigin[] origins)
        {
            if (LastManifestUpdate.HasValue)
                return Donations.Where(x => x.OperationDate > LastManifestUpdate && origins.Contains(x.Origin)).ToList();
            return Donations.Where(x => origins.Contains(x.Origin)).ToList();
        }

    }

}
