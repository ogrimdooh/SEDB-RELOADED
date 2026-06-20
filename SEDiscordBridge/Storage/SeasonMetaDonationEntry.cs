using Newtonsoft.Json;
using System;
using System.Xml.Serialization;

namespace SEDiscordBridge.Patches
{
    public class SeasonMetaDonationEntry
    {

        [XmlElement]
        public ulong SteamId { get; set; }

        [XmlElement]
        public long ItemCount { get; set; }

        [XmlElement]
        public float MassAmount { get; set; }

        [XmlElement]
        public string OperationDateValue { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public DateTime? OperationDate
        {
            get
            {
                if (DateTime.TryParseExact(OperationDateValue, SEDBStorage.DATE_FORMAT, null, System.Globalization.DateTimeStyles.None, out var result))
                    return result;
                return null;
            }
            set
            {
                OperationDateValue = value?.ToString(SEDBStorage.DATE_FORMAT);
            }
        }

    }

}
