using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Utils;
using VRageMath;

namespace SEDiscordBridge.Controllers
{

    public static class LootConstants
    {

        public class LootItem
        {

            public UniqueEntityId Id { get; set; }
            public Vector2 Chance { get; set; }

            public LootItem(UniqueEntityId id, Vector2 chance)
            {
                Id = id;
                Chance = chance;
            }

        }

        public class OreLootSettings
        {

            public float Chance { get; set; }
            public float MaxVolume { get; set; }
            public Vector2I MaxTypes { get; set; }

            public OreLootSettings(float chance, float maxVolume, Vector2I maxTypes)
            {
                Chance = chance;
                MaxVolume = maxVolume;
                MaxTypes = maxTypes;
            }

        }

        public static OreLootSettings CommonOre { get; set; } = new OreLootSettings(0.8f, 0.25f, new Vector2I(1, 4));
        public static OreLootSettings UnCommonOre { get; set; } = new OreLootSettings(0.4f, 0.2f, new Vector2I(1, 3));
        public static OreLootSettings RareOre { get; set; } = new OreLootSettings(0.2f, 0.15f, new Vector2I(1, 2));
        public static OreLootSettings EpicOre { get; set; } = new OreLootSettings(0.1f, 0.1f, new Vector2I(1, 1));
        public static OreLootSettings LegendaryOre { get; set; } = new OreLootSettings(0.05f, 0.05f, new Vector2I(1, 1));

        public static readonly LootItem[] COMMON_ORES = new LootItem[]
        {
            new LootItem(OreConstants.SILICON_ORE_ID, new Vector2(0, 0.16f)),
            new LootItem(OreConstants.ZINC_ORE_ID, new Vector2(0.16f, 0.32f)),
            new LootItem(OreConstants.COPPER_ORE_ID, new Vector2(0.32f, 0.48f)),
            new LootItem(OreConstants.NICKEL_ORE_ID, new Vector2(0.48f, 0.64f)),
            new LootItem(OreConstants.ALUMINUM_ORE_ID, new Vector2(0.64f, 0.8f)),
            new LootItem(OreConstants.IRON_ORE_ID, new Vector2(0.8f, 1.0f))
        };

        public static readonly LootItem[] UNCOMMON_ORES = new LootItem[]
        {
            new LootItem(OreConstants.CARBON_ORE_ID, new Vector2(0, 0.25f)),
            new LootItem(OreConstants.SULFUR_ORE_ID, new Vector2(0.25f, 0.5f)),
            new LootItem(OreConstants.POTASSIUM_ORE_ID, new Vector2(0.5f, 0.75f)),
            new LootItem(OreConstants.LEAD_ORE_ID, new Vector2(0.75f, 1.0f))
        };

        public static readonly LootItem[] RARE_ORES = new LootItem[]
        {
            new LootItem(OreConstants.COBALT_ORE_ID, new Vector2(0.0f, 0.3f)),
            new LootItem(OreConstants.LITHIUM_ORE_ID, new Vector2(0, 0.5f)),
            new LootItem(OreConstants.SILVER_ORE_ID, new Vector2(0.5f, 0.7f)),
            new LootItem(OreConstants.GOLD_ORE_ID, new Vector2(0.7f, 0.9f)),
            new LootItem(OreConstants.MAGNESIUM_ORE_ID, new Vector2(0.9f, 1.0f))
        };

        public static readonly LootItem[] EPIC_ORES = new LootItem[]
        {
            new LootItem(OreConstants.PLATINUM_ORE_ID, new Vector2(0.0f, 0.6f)),
            new LootItem(OreConstants.URANIUM_ORE_ID, new Vector2(0.6f, 0.9f)),
            new LootItem(OreConstants.BERYLLIUM_ORE_ID, new Vector2(0.9f, 1.0f))
        };

        public static readonly LootItem[] LEGENDARY_ORES = new LootItem[]
        {
            new LootItem(OreConstants.TITANIUM_ORE_ID, new Vector2(0.0f, 1.0f))
        };

        public static OreRarity[] GetRandomOresToLoot(Dictionary<OreRarity, OreLootSettings> oreInfo)
        {
            var ores = new List<OreRarity>();
            foreach (var key in oreInfo.Keys)
            {
                var chance = MyUtils.GetRandomFloat(0, 1);
                if (chance < oreInfo[key].Chance)
                {
                    ores.Add(key);
                }
            }
            return ores.ToArray();
        }

        public static LootItem[] GetLootTable(OreRarity rarity)
        {
            switch (rarity)
            {
                case OreRarity.Common:
                    return COMMON_ORES;
                case OreRarity.Uncommon:
                    return UNCOMMON_ORES;
                case OreRarity.Rare:
                    return RARE_ORES;
                case OreRarity.Epic:
                    return EPIC_ORES;
                case OreRarity.Legendary:
                    return LEGENDARY_ORES;
            }
            return new LootItem[] { };
        }

        public static Dictionary<OreRarity, OreLootSettings> GetAllOres(string faction, float threat)
        {
            return new Dictionary<OreRarity, OreLootSettings>()
            {
                { OreRarity.Common, GetOre(OreRarity.Common, faction, threat) },
                { OreRarity.Uncommon, GetOre(OreRarity.Uncommon, faction, threat) },
                { OreRarity.Rare, GetOre(OreRarity.Rare, faction, threat) },
                { OreRarity.Epic, GetOre(OreRarity.Epic, faction, threat) },
                { OreRarity.Legendary, GetOre(OreRarity.Legendary, faction, threat) }
            };
        }

        public static OreLootSettings GetOre(OreRarity oreRarity, string faction, float threat)
        {
            switch (oreRarity)
            {
                case OreRarity.Common:
                    return GetCommonOre(faction, threat);
                case OreRarity.Uncommon:
                    return GetUncommonOre(faction, threat);
                case OreRarity.Rare:
                    return GetRareOre(faction, threat);
                case OreRarity.Epic:
                    return GetEpicOre(faction, threat);
                case OreRarity.Legendary:
                    return GetLegendaryOre(faction, threat);
                default:
                    return null;
            }
        }

        public static OreLootSettings GetCommonOre(string faction, float threat)
        {
            return CommonOre;
        }

        public static OreLootSettings GetUncommonOre(string faction, float threat)
        {
            return UnCommonOre;
        }

        public static OreLootSettings GetRareOre(string faction, float threat)
        {
            return RareOre;
        }

        public static OreLootSettings GetEpicOre(string faction, float threat)
        {
            return EpicOre;
        }

        public static OreLootSettings GetLegendaryOre(string faction, float threat)
        {
            return LegendaryOre;
        }

    }

}
