using System.Xml.Serialization;
using System.Collections.Generic;
using VRage.Game;
using System.Linq;

namespace SEDiscordBridge.Patches
{
    public class SeasonMetaCategory
    {

        [XmlElement]
        public string Id { get; set; }

        [XmlElement]
        public string Name { get; set; }

        [XmlElement]
        public SeasonMetaEntryType Type { get; set; }

        [XmlArray("Items"), XmlArrayItem("Item", typeof(SeasonMetaCategoryValidItem))]
        public List<SeasonMetaCategoryValidItem> Items { get; set; } = new List<SeasonMetaCategoryValidItem>();

        public SeasonMetaCategoryValidItem GetItemById(MyDefinitionId itemId)
        {
            return Items.FirstOrDefault(x => x.Id.ToMyDefinitionId() == itemId);
        }

    }

}
