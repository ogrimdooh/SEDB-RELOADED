using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Entities.Base;
using SEDiscordBridge.Extensions;
using SEDiscordBridge.Storage.SeasonMeta;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using VRage.Collections;
using VRage.Game;
using VRage.Library.Utils;
using VRageMath;

namespace SEDiscordBridge.Controllers
{

    public static class ItemPriceController
    {

        public class BaseMaterialItem
        {

            public UniqueEntityId Id { get; set; }
            public MyPhysicalItemDefinition Definition { get; set; }

            public bool IsLoaded { get; set; }
            public bool IsBlueprintChecked { get; set; }
            public MyBlueprintDefinitionBase RecipeBlueprint { get; set; }

            public float BaseValue { get; set; }
            public ConcurrentDictionary<string, float> Composition { get; set; } = new ConcurrentDictionary<string, float>();

            public long GetValue(StationType stationType, Vector3D position, bool buy, float modifier = 1f)
            {
                var finalValue = BaseValue;
                if (Composition.Any())
                {
                    MyPlanet nearPlanet = null;
                    float distanceToPlanet = 0;
                    if (IsPlanetStation(stationType))
                    {
                        nearPlanet = GameWatcherController.GetPlanetAtRange(position);
                        if (stationType == StationType.OrbitalStation && nearPlanet != null)
                        {
                            distanceToPlanet = (float)Vector3D.Distance(position, nearPlanet.GetClosestSurfacePointGlobal(position));
                        }
                    }
                    else if (stationType == StationType.AsteroidFieldStation)
                    {
                        nearPlanet = GameWatcherController.GetPlanetAtRange(position);
                        if (nearPlanet != null)
                        {
                            distanceToPlanet = (float)Vector3D.Distance(position, nearPlanet.PositionComp.GetPosition());
                        }
                    }
                    if (nearPlanet != null)
                    {
                        var planetDef = MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions().FirstOrDefault(x => x.Id == nearPlanet.DefinitionId);
                        if (planetDef != null && planetDef.OreMappings != null && planetDef.OreMappings.Any())
                        {
                            var voxelTypes = planetDef.OreMappings.Select(x => x.Type).Distinct().ToArray();
                            var ores = voxelTypes.Select(x => MyDefinitionManager.Static.GetVoxelMaterialDefinition(x)).Where(x => x != null).Select(x => x.MinedOre.ToUpper()).ToArray();
                            var pEasy = Composition.Where(x => ores.Contains(x.Key)).Sum(x => x.Value);
                            var pHard = Composition.Values.Sum() - pEasy;
                            var eValue = (finalValue * pEasy) / (buy ? 5f : 7.5f);
                            var hValue = (finalValue * pHard) / (buy ? 1.25f : 2.5f);
                            var dValue = finalValue * (distanceToPlanet / 100000000);
                            finalValue -= eValue;
                            finalValue += hValue;
                            finalValue += dValue;
                        }
                    }
                }
                return (long)(finalValue * modifier);
            }

        }

        public class StationShopItem : BaseMaterialItem
        {

            public ItemRarity Rarity { get; set; }

        }

        public class StationItensAmountProfile
        {

            public Vector2I AcquisitionContractsCount { get; set; }
            public Vector2I OtherContractsCount { get; set; }

        }

        public class ItemForOrder
        {

            public UniqueEntityId ItemId { get; set; }
            public int Count { get; set; }
            public int Price { get; set; }
            public float Volume { get; set; }
            public float Mass { get; set; }
            public long Reward { get; set; }

        }

        public static readonly Vector2 STATION_BUY_VALUE_MULTIPLIER = new Vector2(0.65f, 0.75f);
        public static readonly Vector2 STATION_ORDER_VALUE_MULTIPLIER = new Vector2(0.75f, 1.25f);
        public static readonly Vector2 STATION_SELL_VALUE_MULTIPLIER = new Vector2(1.25f, 1.35f);

        // 150 * 1.25 (6 - 1)
        public static readonly Dictionary<OreRarity, float> BASE_ORE_VALUE = new Dictionary<OreRarity, float>()
        {
            { OreRarity.None, 7.5f },
            { OreRarity.Common, 10.875f },
            { OreRarity.Uncommon, 15.76875f },
            { OreRarity.Rare, 22.8646875f },
            { OreRarity.Epic, 33.153796875f },
            { OreRarity.Legendary, 48.07300546875f }
        };

        public static readonly float BYPRODUCT_VALUE_MULTIPLIER = 0.125f;
        public static readonly float TREE_VALUE_MULTIPLIER = 0.03125f;

        public static readonly Dictionary<UniqueEntityId, float> MinimalPricePerUnit_FailOver = new Dictionary<UniqueEntityId, float>()
        {
            /* ORES */
            { ItensConstants.WOOD_ORE_ID, BASE_ORE_VALUE[OreRarity.Common] * TREE_VALUE_MULTIPLIER },
            { ItensConstants.CHLORINE_ID, BASE_ORE_VALUE[OreRarity.Rare] * BYPRODUCT_VALUE_MULTIPLIER },
            { ItensConstants.SODIUM_ID, BASE_ORE_VALUE[OreRarity.Common] * BYPRODUCT_VALUE_MULTIPLIER },
            { ItensConstants.NITRATE_ID, BASE_ORE_VALUE[OreRarity.Uncommon] * BYPRODUCT_VALUE_MULTIPLIER },
            /* BASE FOODS */
            { ItensConstants.COFFEE_ID, 1.24f },
            { ItensConstants.FRUIT_ID, 1.24f },
            { ItensConstants.POTATOES_ID, 1.24f },
            { ItensConstants.VEGETABLES_ID, 1.189f },
            { ItensConstants.MUSHROOMS_ID, 1.164f },
            { ItensConstants.MAMMALMEATRAW_ID, 1.027f },
            { ItensConstants.INSECTMEATRAW_ID, 0.856f },
            /* NOT CONSUMABLE FOODS */
            { ItensConstants.GRAIN_ID, 1.264f },
            { ItensConstants.ALGAE_ID, 0.273f },
            /* NOT PRODUCED MEALS */
            { ItensConstants.MEALPACK_HARDTACK_ID, 17.5f },
            { ItensConstants.MEALPACK_FOODPASTE_ID, 12.5f },
            { ItensConstants.MEALPACK_CLANGCRUNCHIES_ID, 15f },
            { ItensConstants.MEALPACK_BANANABEEF_ID, 22.5f },
            { ItensConstants.MEALPACK_SYNTHLOAF_ID, 20f },
            /* NOT MAPPED AMMO MAGAZINES */
            { ItensConstants.NATO_5P56X45MM_ID, 250.0f },
            /* PROTOTECH NOT FABRICATED */
            { ItensConstants.PROTOTECHFRAME_ID, 5000.0f },
            { ItensConstants.PROTOTECHSCRAP_ID, 625.0f }
        };

        // Valina
        public const string Iron_01 = "Iron_01";
        public const string Iron_02 = "Iron_02";
        public const string Iron_03 = "Iron_03";
        public const string Iron_04 = "Iron_04";
        public const string Nickel_01 = "Nickel_01";
        public const string Silicon_01 = "Silicon_01";
        public const string Cobalt_01 = "Cobalt_01";
        public const string Gold_01 = "Gold_01";
        public const string Silver_01 = "Silver_01";
        public const string Platinum_01 = "Platinum_01";
        public const string Magnesium_01 = "Magnesium_01";
        public const string Uraninite_01 = "Uraninite_01";

        // ES Core
        public const string LavaSoil_01 = "LavaSoil_01";
        public const string StoneIce_01 = "StoneIce_01";
        public const string DirtySoil_01 = "DirtySoil_01";

        // ES Technology
        public const string Aluminum_01 = "Aluminum_01";
        public const string Copper_01 = "Copper_01";
        public const string Zinc_01 = "Zinc_01";
        public const string Lead_01 = "Lead_01";
        public const string Sulfur_01 = "Sulfur_01";
        public const string Carbon_01 = "Carbon_01";
        public const string Potassium_01 = "Potassium_01";
        public const string Lithium_01 = "Lithium_01";
        public const string Titanium_01 = "Titanium_01";
        public const string Beryllium_01 = "Beryllium_01";

        public static readonly string[] AllValidOres = new string[]
        {
            "IRON",
            "NICKEL",
            "SILICON",
            "COBALT",
            "GOLD",
            "SILVER",
            "PLATINUM",
            "MAGNESIUM",
            "URANIUM",
            "ICE",
            "STONEICE",
            "ALUMINUM",
            "COPPER",
            "LEAD",
            "SULFUR",
            "CARBON",
            "POTASSIUM",
            "LITHIUM",
            "ZINC",
            "IRIDIUM",
            "TITANIUM",
            "MERCURY",
            "BERYLLIUM",
            "TUNGSTEN",
            "PLUTONIUM",
            "TOXICICE",
            "SOIL",
            "MOONSOIL",
            "DESERTSOIL",
            "ALIENSOIL",
            "STONE"
        };

        public static Dictionary<OreRarity, string[]> ORES_TYPES = new Dictionary<OreRarity, string[]>()
        {
            { OreRarity.Common, new string[] { Iron_01, Iron_02, Iron_03, Iron_04, Nickel_01, Silicon_01, Aluminum_01, Copper_01, Zinc_01, DirtySoil_01, StoneIce_01, LavaSoil_01 } },
            { OreRarity.Uncommon, new string[] { Lead_01, Sulfur_01, Carbon_01, Potassium_01 } },
            { OreRarity.Rare, new string[] { Cobalt_01, Gold_01, Silver_01, Magnesium_01, Lithium_01 } },
            { OreRarity.Epic, new string[] { Platinum_01, Uraninite_01, Beryllium_01 } },
            { OreRarity.Legendary, new string[] { Titanium_01 } }
        };

        public static readonly Dictionary<ItemRarity, Vector2> ITEM_RARITY_AMOUNT = new Dictionary<ItemRarity, Vector2>()
        {
            { ItemRarity.Common, new Vector2(512, 1024) },
            { ItemRarity.Uncommon, new Vector2(256, 512) },
            { ItemRarity.Normal, new Vector2(128, 256) },
            { ItemRarity.Rare, new Vector2(64, 128) },
            { ItemRarity.Epic, new Vector2(32, 64) },
            { ItemRarity.Legendary, new Vector2(16, 32) }
        };

        public static readonly Vector2 GENERIC_AMOUNT_MULTIPLIER = new Vector2(4, 16);
        public static readonly Vector2 ORE_INGOT_AMOUNT_MULTIPLIER = new Vector2(64, 256);
        public static readonly Vector2 GASCONTAINER_AMOUNT_MULTIPLIER = new Vector2(0.16f, 0.64f);

        public static readonly Dictionary<StationLevel, StationItensAmountProfile> STATION_ITENS_PROFILE = new Dictionary<StationLevel, StationItensAmountProfile>()
        {
            {
                StationLevel.Small,
                new StationItensAmountProfile()
                {
                    AcquisitionContractsCount = new Vector2I(4, 8),
                    OtherContractsCount = new Vector2I(2, 4)
                }
            },
            {
                StationLevel.Medium,
                new StationItensAmountProfile()
                {
                    AcquisitionContractsCount = new Vector2I(8, 12),
                    OtherContractsCount = new Vector2I(4, 6)
                }
            },
            {
                StationLevel.Large,
                new StationItensAmountProfile()
                {
                    AcquisitionContractsCount = new Vector2I(12, 16),
                    OtherContractsCount = new Vector2I(6, 8)
                }
            }
        };

        public static readonly ConcurrentDictionary<UniqueEntityId, StationShopItem> SHOP_ITENS = new ConcurrentDictionary<UniqueEntityId, StationShopItem>();
        public static readonly ConcurrentDictionary<UniqueEntityId, BaseMaterialItem> BASE_ITENS = new ConcurrentDictionary<UniqueEntityId, BaseMaterialItem>();

        private static DictionaryValuesReader<MyDefinitionId, MyBlueprintDefinitionBase>? _bluePrints = null;
        private static DictionaryValuesReader<string, MyVoxelMaterialDefinition>? _voxelDefinitions = null;
        private static readonly List<MyDefinitionId> ValueCalcLock = new List<MyDefinitionId>();
        private static List<MyDefinitionId> MappedIngots = new List<MyDefinitionId>();

        private static DictionaryValuesReader<MyDefinitionId, MyBlueprintDefinitionBase> bluePrints
        {
            get
            {
                if (_bluePrints == null)
                    _bluePrints = MyDefinitionManager.Static.GetBlueprintDefinitions();
                return _bluePrints.Value;
            }
        }

        private static DictionaryValuesReader<string, MyVoxelMaterialDefinition> voxelDefinitions
        {
            get
            {
                if (_voxelDefinitions == null)
                    _voxelDefinitions = MyDefinitionManager.Static.GetVoxelMaterialDefinitions();
                return _voxelDefinitions.Value;
            }
        }

        public static bool IsPlanetStation(StationType stationType)
        {
            return stationType == StationType.PlanetStation || stationType == StationType.OrbitalStation;
        }

        public static bool AddItemToShop(UniqueEntityId id, ItemRarity rarity)
        {
            if (!SHOP_ITENS.ContainsKey(id))
            {
                var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(id.DefinitionId);
                if (def != null)
                {
                    SHOP_ITENS[id] = new StationShopItem()
                    {
                        Id = id,
                        Rarity = rarity,
                        Definition = def
                    };
                    Logging.Instance.LogInfo(typeof(ItemPriceController), $"AddItemToShop: Item {id.DefinitionId} registered.");
                    return true;
                }
                else
                {
                    Logging.Instance.LogWarning(typeof(ItemPriceController), $"AddItemToShop: Item {id.DefinitionId} has no definition.");
                }
            }
            Logging.Instance.LogWarning(typeof(ItemPriceController), $"AddItemToShop: Item {id.DefinitionId} already registered.");
            return false;
        }

        private static void DoCheckForBaseMaterials<T>(IEnumerable<KeyValuePair<UniqueEntityId, T>> queryItemBaseMaterial) where T : BaseMaterialItem
        {
            while (queryItemBaseMaterial.Any())
            {
                var itemToCheck = queryItemBaseMaterial.FirstOrDefault();
                var requisitesToAdd = itemToCheck.Value.RecipeBlueprint.Prerequisites.Where(y =>
                    !SHOP_ITENS.ContainsKey(new UniqueEntityId(y.Id)) &&
                    !BASE_ITENS.ContainsKey(new UniqueEntityId(y.Id))
                ).ToArray();
                foreach (var requisite in requisitesToAdd)
                {
                    var id = new UniqueEntityId(requisite.Id);
                    var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(id.DefinitionId);
                    Logging.Instance.LogInfo(typeof(ItemPriceController), $"DoCheckForBaseMaterials: BASE_ITENS ADD {id}");
                    BASE_ITENS[id] = new BaseMaterialItem()
                    {
                        Id = id,
                        Definition = def,
                        IsLoaded = def == null,
                        IsBlueprintChecked = def == null,
                        BaseValue = def == null ? 1 : 0
                    };
                }
            }
            var queryBaseItens = BASE_ITENS.Where(x => !x.Value.IsLoaded);
            var queryBaseItemBlueprint = queryBaseItens.Where(x => !x.Value.IsBlueprintChecked);
            while (queryBaseItemBlueprint.Any())
            {
                var itemToCheck = queryBaseItemBlueprint.FirstOrDefault();
                var idToFind = itemToCheck.Key.DefinitionId;
                var isBaseOre = idToFind.TypeId == typeof(MyObjectBuilder_Ore) && AllValidOres.Contains(idToFind.SubtypeName.ToUpper());
                if (!isBaseOre)
                {
                    var queryBlueprint = bluePrints.Where(x => x.Results.Any(y => y.Id == idToFind) && !x.Prerequisites.Any(y => y.Id == idToFind));
                    if (queryBlueprint.Any())
                    {
                        var bluePrintsToChouse = queryBlueprint.OrderBy(x => x.Results.Count()).ToList();
                        if (bluePrintsToChouse.Any(x => x.IsPrimary))
                            itemToCheck.Value.RecipeBlueprint = bluePrintsToChouse.Where(x => x.IsPrimary).FirstOrDefault();
                        else
                            itemToCheck.Value.RecipeBlueprint = bluePrintsToChouse.FirstOrDefault();
                    }
                }
                itemToCheck.Value.IsBlueprintChecked = true;
            }
        }

        private static void DoLoadActiveSeasonMeta()
        {
            if (SeasonMetaConfigStorage.Instance.Enabled)
            {
                var config = SeasonMetaConfigStorage.Instance.GetActiveConfiguration();
                if (config != null)
                {
                    foreach (var entry in config.Entries)
                    {
                        var category = SeasonMetaConfigStorage.Instance.GetCategoryById(entry.CategoryId);
                        if (category != null)
                        {
                            foreach (var item in category.Items)
                            {
                                AddItemToShop(new UniqueEntityId(item.Id.ToMyDefinitionId()), category.Rarity);
                            }
                        }
                    }
                }
            }
        }

        private static void DoCalcAllItensInfo()
        {
            try
            {
                // Filter not loaded
                var query = SHOP_ITENS.Where(x => !x.Value.IsLoaded);
                if (query.Any())
                {
                    // Get all blueprints
                    _bluePrints = null;
                    _voxelDefinitions = null;
                    // Find target blueprint
                    var queryItemBlueprint = query.Where(x => !x.Value.IsBlueprintChecked);
                    while (queryItemBlueprint.Any())
                    {
                        var itemToCheck = queryItemBlueprint.FirstOrDefault();
                        var idToFind = itemToCheck.Key.DefinitionId;
                        var isBaseOre = idToFind.TypeId == typeof(MyObjectBuilder_Ore) && AllValidOres.Contains(idToFind.SubtypeName.ToUpper());
                        if (!isBaseOre)
                        {
                            var queryBlueprint = bluePrints.Where(x =>
                                x.Results.Any(y => y.Id == idToFind) &&
                                !x.Prerequisites.Any(y => y.Id == idToFind
                            ));
                            if (queryBlueprint.Any())
                            {
                                if (queryBlueprint.Count() > 1)
                                {
                                    var newQuery = queryBlueprint.Where(x => !x.Id.SubtypeName.StartsWith("Position"));
                                    if (newQuery.Any())
                                        queryBlueprint = newQuery;
                                }
                                var bluePrintsToChouse = queryBlueprint.OrderBy(x => x.Results.Count()).ToList();
                                if (bluePrintsToChouse.Any(x => x.IsPrimary))
                                    itemToCheck.Value.RecipeBlueprint = bluePrintsToChouse.Where(x => x.IsPrimary).FirstOrDefault();
                                else
                                    itemToCheck.Value.RecipeBlueprint = bluePrintsToChouse.FirstOrDefault();
                            }
                            Logging.Instance.LogInfo(typeof(ItemPriceController), $"DoCalcAllItensInfo: USE {itemToCheck.Value.RecipeBlueprint?.Id} TO {idToFind}");
                        }
                        itemToCheck.Value.IsBlueprintChecked = true;
                    }
                    // Check blueprints for base materials
                    var queryItemBaseMaterial = query.Where(x =>
                        x.Value.IsBlueprintChecked &&
                        x.Value.RecipeBlueprint != null &&
                        x.Value.RecipeBlueprint.Prerequisites.Any(y =>
                            !SHOP_ITENS.ContainsKey(new UniqueEntityId(y.Id)) &&
                            !BASE_ITENS.ContainsKey(new UniqueEntityId(y.Id))
                        )
                    );
                    DoCheckForBaseMaterials(queryItemBaseMaterial);
                    // Check base materials blueprints for base materials
                    int index = 1;
                    do
                    {
                        Logging.Instance.LogInfo(typeof(ItemPriceController), $"DoCalcAllItensInfo: CHECK BASE_ITENS {index}");
                        var queryBaseItemBaseMaterial = BASE_ITENS.Where(x =>
                            !x.Value.IsLoaded &&
                            x.Value.IsBlueprintChecked &&
                            x.Value.RecipeBlueprint != null &&
                            x.Value.RecipeBlueprint.Prerequisites.Any(y =>
                                !SHOP_ITENS.ContainsKey(new UniqueEntityId(y.Id)) &&
                                !BASE_ITENS.ContainsKey(new UniqueEntityId(y.Id))
                            )
                        );
                        DoCheckForBaseMaterials(queryBaseItemBaseMaterial);
                        index++;
                    } while (BASE_ITENS.Any(x =>
                            !x.Value.IsLoaded &&
                            x.Value.IsBlueprintChecked &&
                            x.Value.RecipeBlueprint != null &&
                            x.Value.RecipeBlueprint.Prerequisites.Any(y =>
                                !SHOP_ITENS.ContainsKey(new UniqueEntityId(y.Id)) &&
                                !BASE_ITENS.ContainsKey(new UniqueEntityId(y.Id))
                            )
                        ));
                    // Load values based in the target recipe
                    var queryBaseItens = BASE_ITENS.Where(x => !x.Value.IsLoaded);
                    DoLoadByType(query, typeof(MyObjectBuilder_Ore));
                    DoLoadByType(queryBaseItens, typeof(MyObjectBuilder_Ore));
                    DoLoadByType(query, typeof(MyObjectBuilder_Ingot));
                    DoLoadByType(queryBaseItens, typeof(MyObjectBuilder_Ingot));
                    DoLoadByType(query);
                }
            }
            catch (Exception ex)
            {
                Logging.Instance.LogError(typeof(ItemPriceController), ex);
            }
        }

        private static void DoLoadByType<T>(IEnumerable<KeyValuePair<UniqueEntityId, T>> baseQuery, Type filter = null) where T : BaseMaterialItem
        {
            var query = filter != null ? baseQuery.Where(x => x.Key.DefinitionId.TypeId == filter) : baseQuery;
            while (query.Any())
            {
                var itemToCheck = query.FirstOrDefault();
                if (GetAndCalcItemInfo(itemToCheck.Key) == null)
                    GetAndCalcBaseItemInfo(itemToCheck.Key);
            }
        }

        private static OreRarity GetOreRarity(string voxelType)
        {
            foreach (var kvp in ORES_TYPES)
            {
                if (kvp.Value.Any(x => x.Equals(voxelType, StringComparison.OrdinalIgnoreCase)))
                {
                    return kvp.Key;
                }
            }
            return OreRarity.None;
        }

        private static float GetBluePrintValue(MyBlueprintDefinitionBase bluePrint, MyPhysicalItemDefinition baseDef, ref ConcurrentDictionary<string, float> composition)
        {
            float baseValue = 0;
            if (!ValueCalcLock.Contains(baseDef.Id))
            {
                try
                {
                    ValueCalcLock.Add(baseDef.Id);
                    if (bluePrint != null)
                    {
                        var resultaAmount = (float)bluePrint.Results.FirstOrDefault(x => x.Id == baseDef.Id).Amount;
                        if (MappedIngots.Contains(baseDef.Id) && bluePrint.Prerequisites.Length == 1 && bluePrint.Prerequisites.Any(x => x.Id.TypeId == typeof(MyObjectBuilder_Ore)))
                        {
                            var prerequisite = bluePrint.Prerequisites[0];
                            var targetId = new UniqueEntityId(prerequisite.Id);
                            var prerequisiteShopItem = GetAndCalcItemInfo(targetId);
                            if (prerequisiteShopItem != null)
                            {
                                baseValue = prerequisiteShopItem.BaseValue;
                                foreach (var c in prerequisiteShopItem.Composition)
                                {
                                    if (composition.ContainsKey(c.Key))
                                        composition[c.Key] += c.Value * (float)prerequisite.Amount;
                                    else
                                        composition[c.Key] = c.Value * (float)prerequisite.Amount;
                                }
                            }
                            else
                            {
                                var prerequisiteBaseItem = GetAndCalcBaseItemInfo(targetId);
                                if (prerequisiteBaseItem != null)
                                {
                                    baseValue = prerequisiteBaseItem.BaseValue;
                                    foreach (var c in prerequisiteBaseItem.Composition)
                                    {
                                        if (composition.ContainsKey(c.Key))
                                            composition[c.Key] += c.Value * (float)prerequisite.Amount;
                                        else
                                            composition[c.Key] = c.Value * (float)prerequisite.Amount;
                                    }
                                }
                            }
                            baseValue *= 1 + (bluePrint.BaseProductionTimeInSeconds / 60);
                            baseValue /= resultaAmount;
                        }
                        else
                        {
                            foreach (var prerequisite in bluePrint.Prerequisites)
                            {
                                if (!prerequisite.Id.TypeId.IsNull && !string.IsNullOrWhiteSpace(prerequisite.Id.SubtypeId.String))
                                {
                                    var targetId = new UniqueEntityId(prerequisite.Id);
                                    var prerequisiteShopItem = GetAndCalcItemInfo(targetId);
                                    if (prerequisiteShopItem != null)
                                    {
                                        baseValue += prerequisiteShopItem.BaseValue * (float)prerequisite.Amount;
                                        foreach (var c in prerequisiteShopItem.Composition)
                                        {
                                            if (composition.ContainsKey(c.Key))
                                                composition[c.Key] += c.Value * (float)prerequisite.Amount;
                                            else
                                                composition[c.Key] = c.Value * (float)prerequisite.Amount;
                                        }
                                    }
                                    else
                                    {
                                        var prerequisiteBaseItem = GetAndCalcBaseItemInfo(targetId);
                                        if (prerequisiteBaseItem != null)
                                        {
                                            baseValue += prerequisiteBaseItem.BaseValue * (float)prerequisite.Amount;
                                            foreach (var c in prerequisiteBaseItem.Composition)
                                            {
                                                if (composition.ContainsKey(c.Key))
                                                    composition[c.Key] += c.Value * (float)prerequisite.Amount;
                                                else
                                                    composition[c.Key] = c.Value * (float)prerequisite.Amount;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Logging.Instance.LogWarning(typeof(ItemPriceController), $"GetBluePrintValue: {bluePrint.Id} had invalid prerequisite!");
                                }
                            }
                            baseValue *= 1 + (bluePrint.BaseProductionTimeInSeconds / 60);
                            if (resultaAmount >= 1)
                                baseValue /= resultaAmount;
                            else if (resultaAmount < 1 && resultaAmount > 0)
                                baseValue *= 1 + ((1 - resultaAmount) / 2);
                        }
                        var totalMax = composition.Values.Sum();
                        foreach (var k in composition.Keys)
                        {
                            composition[k] = composition[k] / totalMax;
                        }
                    }
                    else
                    {
                        if (baseDef != null)
                        {
                            // Check if is a ore to use time to refine and voxel ratio to influece the value
                            if (baseDef.Id.TypeId == typeof(MyObjectBuilder_Ore) && AllValidOres.Contains(baseDef.Id.SubtypeName.ToUpper()))
                            {
                                // Get Ratio
                                OreRarity rarity = OreRarity.Common;
                                float baseRatio = 1;
                                var voxelQuery = voxelDefinitions.Where(x => x.MinedOre == baseDef.Id.SubtypeName);
                                if (voxelQuery.Any())
                                {
                                    baseRatio = voxelQuery.Sum(x => x.MinedOreRatio) / voxelQuery.Count();
                                    rarity = (OreRarity)voxelQuery.Max(x => (int)GetOreRarity(x.Id.SubtypeName));
                                }
                                // Get time to refine
                                float lostFactor = 0;
                                float baseRefineTime = 0;
                                float baseSourceAmount = 0;
                                float baseEndAmount = 0;
                                var refineQuery = bluePrints.Where(x =>
                                    x.Prerequisites.Any(y => y.Id == baseDef.Id) &&
                                    x.Prerequisites.Length == 1 &&
                                    x.Results.Length == 1 &&
                                    x.Results[0].Id.TypeId == typeof(MyObjectBuilder_Ingot) &&
                                    !x.Results[0].Id.SubtypeName.Contains("Compressed")
                                );
                                if (refineQuery.Any())
                                {
                                    var count = refineQuery.Count();
                                    baseRefineTime += refineQuery.Sum(x => x.BaseProductionTimeInSeconds) / count;
                                    baseSourceAmount = refineQuery.Sum(x => x.Prerequisites.Where(y => y.Id == baseDef.Id).Sum(y => (float)y.Amount)) / count;
                                    baseEndAmount = refineQuery.Sum(x => x.Results.Sum(y => (float)y.Amount) / x.Results.Count()) / count;
                                    foreach (var item in refineQuery)
                                    {
                                        if (!MappedIngots.Contains(item.Results[0].Id))
                                        {
                                            MappedIngots.Add(item.Results[0].Id);
                                        }
                                    }
                                }
                                else
                                {
                                    baseRefineTime = 1;
                                }
                                if (baseSourceAmount > 0)
                                {
                                    lostFactor += (baseSourceAmount - baseEndAmount) / baseSourceAmount;
                                }
                                // Calculate value
                                baseValue = BASE_ORE_VALUE[rarity];
                                baseValue /= baseRatio;
                                baseValue *= 1 - lostFactor;
                                baseValue *= baseRefineTime;
                                composition[baseDef.Id.SubtypeName.ToUpper()] = 1f;
                            }
                            else
                            {
                                var uId = new UniqueEntityId(baseDef.Id);
                                if (MinimalPricePerUnit_FailOver.ContainsKey(uId))
                                {
                                    baseValue = MinimalPricePerUnit_FailOver[uId];
                                }
                                else
                                {
                                    baseValue = baseDef.MinimalPricePerUnit;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    ValueCalcLock.Remove(baseDef.Id);
                }
            }
            else
            {
                Logging.Instance.LogWarning(typeof(ItemPriceController), $"GetBluePrintValue: {baseDef.Id} : Avoid stack overflow, maybe recipes got conflicted!");
            }
            Logging.Instance.LogInfo(typeof(ItemPriceController), $"GetBluePrintValue: {baseDef.Id} : BASE VALUE = {baseValue}");
            return Math.Max(baseValue, 1);
        }

        private static BaseMaterialItem GetAndCalcBaseItemInfo(UniqueEntityId id)
        {
            if (BASE_ITENS.ContainsKey(id))
            {
                if (!BASE_ITENS[id].IsLoaded)
                {
                    var compostion = new ConcurrentDictionary<string, float>();
                    BASE_ITENS[id].BaseValue = (long)GetBluePrintValue(BASE_ITENS[id].RecipeBlueprint, BASE_ITENS[id].Definition, ref compostion);
                    BASE_ITENS[id].IsLoaded = true;
                    BASE_ITENS[id].Composition = compostion;
                }
                return BASE_ITENS[id];
            }
            return null;
        }

        private static StationShopItem GetAndCalcItemInfo(UniqueEntityId id)
        {
            if (SHOP_ITENS.ContainsKey(id))
            {
                if (!SHOP_ITENS[id].IsLoaded)
                {
                    var compostion = new ConcurrentDictionary<string, float>();
                    SHOP_ITENS[id].BaseValue = (long)GetBluePrintValue(SHOP_ITENS[id].RecipeBlueprint, SHOP_ITENS[id].Definition, ref compostion);
                    SHOP_ITENS[id].IsLoaded = true;
                    SHOP_ITENS[id].Composition = compostion;
                }
                return SHOP_ITENS[id];
            }
            return null;
        }

        private static bool _initialized = false;
        public static void Init()
        {
            if (!_initialized)
            {
                DoLoadActiveSeasonMeta();
                DoCalcAllItensInfo();
                _initialized = true;
            }
        }

        public static BaseMaterialItem GetItemInfo(UniqueEntityId id)
        {
            BaseMaterialItem item = GetAndCalcItemInfo(id);
            if (item == null)
                item = GetAndCalcBaseItemInfo(id);
            return item;
        }

        public static float GetAmountWithMultiplier(float baseValue, MyDefinitionId id)
        {
            if (id.TypeId == typeof(MyObjectBuilder_Ore) || id.TypeId == typeof(MyObjectBuilder_Ingot))
                return baseValue * ORE_INGOT_AMOUNT_MULTIPLIER.GetRandom();
            if (id.TypeId == typeof(MyObjectBuilder_GasContainerObject) || id.TypeId == typeof(MyObjectBuilder_OxygenContainerObject))
                return baseValue * GASCONTAINER_AMOUNT_MULTIPLIER.GetRandom();
            return baseValue * GENERIC_AMOUNT_MULTIPLIER.GetRandom();
        }

        public static List<ItemForOrder> GetItensToOrder(Vector3D position, StationType stationType, StationLevel stationLevel, FactionType factionType)
        {
            var itemsToOrder = new List<ItemForOrder>();
            var count = STATION_ITENS_PROFILE[stationLevel].AcquisitionContractsCount.GetRandomInt();
            var selectedItems = SHOP_ITENS.Values.OrderBy(x => MyRandom.Instance.NextFloat()).Take(count);
            foreach (var item in selectedItems)
            {
                var randomAmount = ITEM_RARITY_AMOUNT[item.Rarity].GetRandom();
                var finalAmount = (int)GetAmountWithMultiplier(randomAmount, item.Definition.Id);
                var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(item.Id.DefinitionId);
                var finalValue = (int)item.GetValue(stationType, position, true) * finalAmount;
                itemsToOrder.Add(new ItemForOrder
                {
                    ItemId = item.Id,
                    Count = finalAmount,
                    Price = finalValue,
                    Volume = def.Volume * finalAmount,
                    Mass = def.Mass * finalAmount,
                    Reward = (long)(finalValue * STATION_ORDER_VALUE_MULTIPLIER.GetRandom())
                });
            }
            return itemsToOrder;
        }

    }
}
