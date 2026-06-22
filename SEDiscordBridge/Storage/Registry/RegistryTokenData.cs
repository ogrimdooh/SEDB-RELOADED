using Newtonsoft.Json;
using SEDiscordBridge.Storage;
using System;
using System.Xml.Serialization;

namespace SEDiscordBridge.Storage.Registry
{
    public class RegistryTokenData
    {

        [XmlElement]
        public string RegistryToken { get; set; }

        [XmlElement]
        public ulong UserId { get; set; }

        [XmlElement]
        public bool Used { get; set; }

        [XmlElement]
        public string ExpireAtValue { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public Guid Token
        {
            get
            {
                if (Guid.TryParse(RegistryToken, out Guid t))
                    return t;
                return Guid.Empty;
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public DateTime? ExpireAt
        {
            get
            {
                if (DateTime.TryParseExact(ExpireAtValue, SEDBStorage.DATE_FORMAT, null, System.Globalization.DateTimeStyles.None, out var result))
                    return result;
                return null;
            }
            set
            {
                ExpireAtValue = value?.ToString(SEDBStorage.DATE_FORMAT);
            }
        }

    }

}
