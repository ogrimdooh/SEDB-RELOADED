using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System;
using Newtonsoft.Json;

namespace SEDiscordBridge.Patches
{

    public class PlayerStorage
    {

        public const string KEY_DID_JUMP = "DID_JUMP";
        public const string KEY_JUMP_COUNT = "JUMP_COUNT";

        public const string KEY_DID_REGISTERLOCATION = "DID_REGISTERLOCATION";
        public const string KEY_LASTLOCATION_ISGRAVITY = "LASTLOCATION_ISGRAVITY";
        public const string KEY_LASTLOCATION_ENTITYID = "LASTLOCATION_ENTITYID";
        public const string KEY_LOCATION_VISITED = "LOCATION_VISITED_{0}";

        public const string KEY_DID_KILL = "DID_KILL";
        public const string KEY_KILL_COUNT = "KILL_COUNT";

        public const string KEY_PROFESSION = "PROFESSION";

        [XmlElement]
        public ulong SteamId { get; set; }

        [XmlArray("Storage"), XmlArrayItem("Entry", typeof(PlayerStorageValue))]
        public List<PlayerStorageValue> Values { get; set; } = new List<PlayerStorageValue>();

        public T GetValue<T>(string key)
        {
            try
            {
                if (Values.Any(x => x.Key == key))
                    return (T)Convert.ChangeType(Values.FirstOrDefault(x => x.Key == key).Value, typeof(T));
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(GetType(), e);
            }
            return default(T);
        }

        public void SetValue<T>(string key, T value)
        {
            PlayerStorageValue entry = null;
            if (Values.Any(x => x.Key == key))
            {
                entry = Values.FirstOrDefault(x => x.Key == key);
                lock (Values)
                {
                    entry.Value = value?.ToString();
                }
            }
            else
            {
                entry = new PlayerStorageValue() { Key = key, Value = value?.ToString() };
                lock (Values)
                {
                    Values.Add(entry);
                }
            }
        }

        public PlayerStorage()
        {
            Values = new List<PlayerStorageValue>();
        }

    }

}
