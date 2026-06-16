using System.Xml.Serialization;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SEDiscordBridge.Patches
{
    public class PlayerStorage
    {

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
