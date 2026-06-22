using Newtonsoft.Json;
using SEDiscordBridge.Storage;
using System;
using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.Registry
{
    public class RegistredUserInfo
    {

        [XmlElement]
        public ulong SteamId { get; set; }

        [XmlElement]
        public ulong UserId { get; set; }

        [XmlElement]
        public string RegistryDateValue { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public DateTime? RegistryDate
        {
            get
            {
                if (DateTime.TryParseExact(RegistryDateValue, SEDBStorage.DATE_FORMAT, null, System.Globalization.DateTimeStyles.None, out var result))
                    return result;
                return null;
            }
            set
            {
                RegistryDateValue = value?.ToString(SEDBStorage.DATE_FORMAT);
            }
        }

    }

}
