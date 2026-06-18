using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace SEDiscordBridge.Patches
{

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

        public bool IsTokenValid(string token, out ulong userId)
        {
            if (Guid.TryParse(token, out Guid tk))
            {
                var t = Tokens.FirstOrDefault(x => x.Token == tk && !x.Used && x.ExpireAt > DateTime.Now);
                if (t != null)
                {
                    userId = t.UserId;
                    return true;
                }
            }
            userId = 0;
            return false;
        }

        public void DoUseToken(string token)
        {
            if (Guid.TryParse(token, out Guid tk))
            {
                var t = Tokens.FirstOrDefault(x => x.Token == tk && !x.Used && x.ExpireAt > DateTime.Now);
                if (t != null)
                {
                    t.Used = true;
                }
            }
        }

        public void DoRegistryUser(ulong userId, ulong steamId)
        {
            Users.Add(new RegistredUserInfo() { 
                UserId = userId,
                SteamId = steamId,
                RegistryDate = DateTime.Now
            });
        }

        public void CleanOldTokens()
        {
            Tokens.RemoveAll(x => x.Used || x.ExpireAt < DateTime.Now);
        }

    }

}
