using System.Xml.Serialization;
using VRage.ObjectBuilders;

namespace SEDiscordBridge.Patches
{
    public class SeasonMetaCategoryValidItem
    {

        [XmlElement]
        public SerializableDefinitionId Id { get; set; }

        [XmlElement]
        public float Weight { get; set; }

    }

}
