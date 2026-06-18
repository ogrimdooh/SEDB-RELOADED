using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace SEDiscordBridge.Patches
{
    public class SeasonMetaCategoryValidItem
    {

        [XmlElement]
        public StorageDefinitionId Id { get; set; }

        [XmlElement]
        public float Weight { get; set; }

    }

}
