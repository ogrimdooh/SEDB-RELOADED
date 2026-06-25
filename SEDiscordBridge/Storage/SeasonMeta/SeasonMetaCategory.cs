using System.Xml.Serialization;
using System.Collections.Generic;
using VRage.Game;
using System.Linq;
using SEDiscordBridge.Patches;
using static SEDiscordBridge.Controllers.ItemPriceController;
using SEDiscordBridge.Controllers.Types;

namespace SEDiscordBridge.Storage.SeasonMeta
{
    public class SeasonMetaCategory
    {

        [XmlElement]
        public string Id { get; set; }

        [XmlElement]
        public string Name { get; set; }

        [XmlElement]
        public SeasonMetaEntryType Type { get; set; }

        [XmlElement]
        public ItemRarity Rarity { get; set; }

        [XmlArray("Items"), XmlArrayItem("Item", typeof(SeasonMetaCategoryValidItem))]
        public List<SeasonMetaCategoryValidItem> Items { get; set; } = new List<SeasonMetaCategoryValidItem>();

        public SeasonMetaCategoryValidItem GetItemById(MyDefinitionId itemId)
        {
            return Items.FirstOrDefault(x => x.Id.ToMyDefinitionId() == itemId);
        }

    }

}
