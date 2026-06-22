using System.Xml.Serialization;
using VRage.Game;

namespace SEDiscordBridge.Storage.Base
{
    public class StorageDefinitionId
    {

        public StorageDefinitionId() { }

        public StorageDefinitionId(MyDefinitionId id)
        {
            Type = id.TypeId.ToString();
            Subtype = id.SubtypeId.ToString();
        }

        [XmlAttribute]
        public string Type { get; set; }

        [XmlAttribute]
        public string Subtype { get; set; }

        public MyDefinitionId ToMyDefinitionId()
        {
            if (MyDefinitionId.TryParse($"{Type}/{Subtype}", out var defId))
            {
                return defId;
            }
            return default;
        }

    }

}
