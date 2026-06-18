using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace SEDiscordBridge.Patches
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

    public class RegistryStorage
    {

        [XmlElement]
        public bool Enabled { get; set; } = true;

        [XmlElement]
        public ulong StartMsgId { get; set; }

        [XmlElement]
        public int TokenValidInMinutes { get; set; } = 5;

        [XmlArray("Users"), XmlArrayItem("User", typeof(RegistredUserInfo))]
        public List<RegistredUserInfo> Users { get; set; } = new List<RegistredUserInfo>();

        [XmlArray("Tokens"), XmlArrayItem("Token", typeof(RegistryTokenData))]
        public List<RegistryTokenData> Tokens { get; set; } = new List<RegistryTokenData>();

        public HashSet<ulong> GetAllMessagesIds()
        {
            return new HashSet<ulong>() { StartMsgId };
        }

        public bool IsUserRegistered(ulong userId)
        {
            return Users.Any(x => x.UserId == userId);
        }

        public bool UserHasValidToken(ulong userId)
        {
            return Tokens.Any(x => x.UserId == userId && !x.Used && x.ExpireAt > DateTime.Now);
        }

        public string CreateToken(ulong userId)
        {
            var token = new RegistryTokenData()
            {
                UserId = userId,
                ExpireAt = DateTime.Now.AddMinutes(TokenValidInMinutes),
                Used = false,
                RegistryToken = Guid.NewGuid().ToString()
            };
            Tokens.Add(token);
            return token.RegistryToken;
        }

    }

}
