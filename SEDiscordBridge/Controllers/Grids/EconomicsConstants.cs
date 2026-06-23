using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using SEDiscordBridge.Entities.Base;
using SEDiscordBridge.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game;
using VRageMath;

namespace SEDiscordBridge.Controllers.Grids
{

    public static class EconomicsConstants
    {

        public static readonly Vector2 STATION_BUY_VALUE_MULTIPLIER = new Vector2(0.65f, 0.75f);
        public static readonly Vector2 STATION_ORDER_VALUE_MULTIPLIER = new Vector2(0.75f, 0.85f);
        public static readonly Vector2 STATION_SELL_VALUE_MULTIPLIER = new Vector2(1.25f, 1.35f);

        public class BaseMaterialItem
        {

            public UniqueEntityId Id { get; set; }
            public MyPhysicalItemDefinition Definition { get; set; }
            public bool ForceMinimalPrice { get; set; }

            public bool IsLoaded { get; set; }
            public bool IsBlueprintChecked { get; set; }
            public MyBlueprintDefinitionBase RecipeBlueprint { get; set; }

            public float BaseValue { get; set; }
            public ConcurrentDictionary<string, float> Composition { get; set; } = new ConcurrentDictionary<string, float>();

            public long GetValue(bool buy, float modifier = 1f)
            {
                var finalValue = BaseValue;
                return (long)(finalValue * modifier);
            }

        }

        public enum ItemRarity
        {

            Common = 0,
            Uncommon = 1,
            Normal = 2,
            Rare = 3,
            Epic = 4,
            Legendary = 4

        }

        public class StationShopItem : BaseMaterialItem
        {

            public ItemRarity Rarity { get; set; }
            public bool CanBuy { get; set; }
            public bool CanSell { get; set; }
            public bool CanOrder { get; set; }
            public string PrefabToGetPrice { get; set; } 

            public void DoForceDefinition()
            {
                Definition.CanPlayerOrder = CanOrder;
                Definition.MinimalPricePerUnit = (int)BaseValue;
            }

        }

        [Flags]
        public enum StationPrefabFlag
        {

            None = 0,
            Rover = 1 << 1,
            IonThruster = 1 << 2,
            H2Thruster = 1 << 3,
            AtmThruster = 1 << 4,
            LargeGrid = 1 << 5,
            SmallGrid = 1 << 6,
            Reactor = 1 << 7,
            JumpDrive = 1 << 8

        }

        [Flags]
        public enum StationPrefabCategory
        {

            None = 0,
            Tiny = 1 << 1,
            Small = 1 << 2,
            Medium = 1 << 3,
            Big = 1 << 4,
            Huge = 1 << 5,

            TinyToMedium = Tiny | Small | Medium,
            TinyToBig = Tiny | Small | Medium | Big,
            All = Tiny | Small | Medium | Big | Huge

        }

        public class StationPrefabItem
        {

            public string PrefabName { get; set; }
            public MyPrefabDefinition Definition { get; set; }

            public bool IsLoaded { get; set; }
            public StationPrefabFlag Flags { get; set; }
            public StationPrefabCategory Category { get; set; }
            public long BlockCount { get; set; }
            public long TotalPCU { get; set; }

            public bool IsValid { get; set; }
            public float BaseValue { get; set; }

        }

        public struct StationItensAmountProfile
        {

            public Vector2I PrefabsCount;
            public Vector2I SellItensCount;
            public Vector2I BuyItensCount;
            public Vector2I AcquisitionContractsCount;
            public ItemRarity[] CanSellRarity;

        }

        public struct StationPrefabFilter
        {

            public StationPrefabFlag excludeFlags;
            public StationPrefabFlag requiredFlags;
            public StationPrefabCategory validCategories;

        }

        public static readonly ConcurrentDictionary<UniqueEntityId, StationShopItem> SHOP_ITENS = new ConcurrentDictionary<UniqueEntityId, StationShopItem>();
        public static readonly ConcurrentDictionary<UniqueEntityId, BaseMaterialItem> BASE_ITENS = new ConcurrentDictionary<UniqueEntityId, BaseMaterialItem>();

        public enum OreRarity
        {

            None = 0,
            Common = 1,
            Uncommon = 2,
            Rare = 3,
            Epic = 4,
            Legendary = 5

        }

        // 150 * 1.25 (6 - 1)
        public static readonly Dictionary<OreRarity, float> BASE_ORE_VALUE = new Dictionary<OreRarity, float>()
        {
            { OreRarity.None, 150f },
            { OreRarity.Common, 187.5f },
            { OreRarity.Uncommon, 234.37f },
            { OreRarity.Rare, 292.96f },
            { OreRarity.Epic, 366.21f },
            { OreRarity.Legendary, 457.76f }
        };

        public static readonly Dictionary<ItemRarity, Vector2> ITEM_RARITY_AMOUNT = new Dictionary<ItemRarity, Vector2>()
        {
            { ItemRarity.Common, new Vector2(64, 128) },
            { ItemRarity.Uncommon, new Vector2(32, 64) },
            { ItemRarity.Normal, new Vector2(16, 32) },
            { ItemRarity.Rare, new Vector2(8, 16) },
            { ItemRarity.Epic, new Vector2(4, 8) },
            { ItemRarity.Legendary, new Vector2(2, 4) },
        };

        public static readonly Vector2 ORE_INGOT_AMOUNT_MULTIPLIER = new Vector2(5, 25);
        public static readonly Vector2 GASCONTAINER_AMOUNT_MULTIPLIER = new Vector2(0.25f, 0.5f);

        public const string AK1EXPLORERROVER_SUBTYPEID = "AK1ExplorerRover";
        public const string AK2CARGOROVER_SUBTYPEID = "AK2CargoRover";
        public const string AK3DROPPOD_SUBTYPEID = "AK3DropPod";

        public static readonly string[] PREFABS_TO_SELL = new string[]
        {
            AK1EXPLORERROVER_SUBTYPEID,
            AK2CARGOROVER_SUBTYPEID,
            AK3DROPPOD_SUBTYPEID
        };

        public static readonly ConcurrentDictionary<string, StationPrefabItem> LOADED_PREFABS_TO_SELL = new ConcurrentDictionary<string, StationPrefabItem>();

        public static StationPrefabItem GetStationPrefabItem(string key)
        {
            if (LOADED_PREFABS_TO_SELL.ContainsKey(key))
                return LOADED_PREFABS_TO_SELL[key];
            return null;
        }

        public static bool AddPrefabToShop(string prefabName)
        {
            if (!LOADED_PREFABS_TO_SELL.ContainsKey(prefabName))
            {
                var def = MyDefinitionManager.Static.GetPrefabDefinition(prefabName);
                if (def != null)
                {
                    LOADED_PREFABS_TO_SELL[prefabName] = new StationPrefabItem()
                    {
                        PrefabName = prefabName,
                        Definition = def
                    };
                    return true;
                }
                else
                {
                    Logging.Instance.LogWarning(typeof(EconomicsConstants), $"AddPrefabToShop: Prefab {prefabName} has no definition.");
                }
            }
            Logging.Instance.LogWarning(typeof(EconomicsConstants), $"AddPrefabToShop: Prefab {prefabName} already registered.");
            return false;
        }

        public static bool AddItemToShop(UniqueEntityId id, ItemRarity rarity, bool canBuy, bool canSell, bool canOrder, bool forceMinimalPrice = false, string prefabPriceTarget = null)
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
                        CanBuy = canBuy,
                        CanSell = canSell,
                        CanOrder = canOrder,
                        ForceMinimalPrice = forceMinimalPrice,
                        Definition = def,
                        PrefabToGetPrice = prefabPriceTarget
                    };
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"AddItemToShop: Item {id.DefinitionId} registered.");
                    return true;
                }
                else
                {
                    Logging.Instance.LogWarning(typeof(EconomicsConstants), $"AddItemToShop: Item {id.DefinitionId} has no definition.");
                }
            }
            Logging.Instance.LogWarning(typeof(EconomicsConstants), $"AddItemToShop: Item {id.DefinitionId} already registered.");
            return false;
        }

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

        public const string Stone = "Stone";
        public const string Iron = "Iron";
        public const string Ice = "Ice";
        public const string Ice_01 = "Ice_01";
        public const string Ice_02 = "Ice_02";
        public const string Ice_03 = "Ice_03";
        public const string Snow = "Snow";
        public const string IceEuropa2 = "IceEuropa2";
        public const string AlienIce = "AlienIce";
        public const string AlienIce_03 = "AlienIce_03";
        public const string AlienSnow = "AlienSnow";
        public const string TritonIce = "TritonIce";
        public const string TritonStone = "TritonStone";

        public const string Grass = "Grass";
        public const string GrassBare = "Grass bare";
        public const string RocksGrass = "Rocks_grass";
        public const string GrassOld = "Grass_old";
        public const string GrassOldBare = "Grass_old bare";
        public const string Grass_02 = "Grass_02";
        public const string WoodsGrass = "Woods_grass";
        public const string WoodsGrassBare = "Woods_grass bare";
        public const string Soil = "Soil";
        public const string AlienGreenGrass = "AlienGreenGrass";
        public const string AlienGreenGrassBare = "AlienGreenGrass bare";
        public const string AlienRockyTerrain = "AlienRockyTerrain";
        public const string AlienRockGrass = "AlienRockGrass";
        public const string AlienRockGrassBare = "AlienRockGrass bare";
        public const string AlienOrangeGrass = "AlienOrangeGrass";
        public const string AlienOrangeGrassBare = "AlienOrangeGrass bare";
        public const string AlienYellowGrass = "AlienYellowGrass";
        public const string AlienYellowGrassBare = "AlienYellowGrass bare";
        public const string AlienSoil = "AlienSoil";
        public const string Sand_02 = "Sand_02";
        public const string MarsSoil = "MarsSoil";
        public const string AlienSand = "AlienSand";
        public const string SmallMoonRocks = "SmallMoonRocks";
        public const string MoonSoil = "MoonSoil";
        public const string CrackedSoil = "CrackedSoil";
        public const string DustyRocks = "DustyRocks";
        public const string DustyRocks2 = "DustyRocks2";
        public const string DustyRocks3 = "DustyRocks3";
        public const string PertamSand = "PertamSand";

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

        public static Dictionary<OreRarity, string[]> ORES_TYPES = new Dictionary<OreRarity, string[]>()
        {
            { OreRarity.Common, new string[] { Iron_01, Iron_02, Iron_03, Iron_04, Nickel_01, Silicon_01, Aluminum_01, Copper_01, Zinc_01, DirtySoil_01, StoneIce_01 } },
            { OreRarity.Uncommon, new string[] { Lead_01, Sulfur_01, Carbon_01, Potassium_01 } },
            { OreRarity.Rare, new string[] { Cobalt_01, Gold_01, Silver_01, Magnesium_01, Lithium_01 } },
            { OreRarity.Epic, new string[] { Platinum_01, Uraninite_01, Beryllium_01 } },
            { OreRarity.Legendary, new string[] { Titanium_01 } }
        };

        private static OreRarity GetOreRarity(string ore)
        {
            return ORES_TYPES.Where(x => x.Value.Contains(ore)).Select(x => x.Key).FirstOrDefault();
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
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"DoCheckForBaseMaterials: BASE_ITENS ADD {id}");
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
                if (!itemToCheck.Value.ForceMinimalPrice)
                {
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
                }
                itemToCheck.Value.IsBlueprintChecked = true;
            }
        }

        private static DictionaryValuesReader<MyDefinitionId, MyBlueprintDefinitionBase>? _bluePrints = null;
        private static DictionaryValuesReader<string, MyVoxelMaterialDefinition>? _voxelDefinitions = null;

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

        public static void DoCalcAllItensInfo()
        {
            try
            {
                foreach (var item in PREFABS_TO_SELL)
                {
                    AddPrefabToShop(item);
                }

                AddItemToShop(ItensConstants.CANVAS_ID, ItemRarity.Normal, true, true, true);

                AddItemToShop(ItensConstants.ZONECHIP_ID, ItemRarity.Rare, true, false, false);

                AddItemToShop(ItensConstants.DAWNDROPSIGNALEXPLORER_ID, ItemRarity.Legendary, true, false, false, false, AK1EXPLORERROVER_SUBTYPEID);
                AddItemToShop(ItensConstants.DAWNDROPSIGNALLITE_ID, ItemRarity.Epic, true, false, false, false, AK2CARGOROVER_SUBTYPEID);
                AddItemToShop(ItensConstants.DAWNDROPSIGNALSURVIVAL_ID, ItemRarity.Epic, true, false, false, false, AK3DROPPOD_SUBTYPEID);
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
                        if (!itemToCheck.Value.ForceMinimalPrice)
                        {
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
                                Logging.Instance.LogInfo(typeof(EconomicsConstants), $"DoCalcAllItensInfo: USE {itemToCheck.Value.RecipeBlueprint?.Id} TO {idToFind}");
                            }
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
                        Logging.Instance.LogInfo(typeof(EconomicsConstants), $"DoCalcAllItensInfo: CHECK BASE_ITENS {index}");
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
                    LoadPrefabsToSell();
                    var query2 = SHOP_ITENS.Where(x => !string.IsNullOrWhiteSpace(x.Value.PrefabToGetPrice));
                    if (query2.Any())
                    {
                        foreach (var x in query2)
                        {
                            var oldv = x.Value.BaseValue;
                            x.Value.BaseValue = GetStationPrefabItem(x.Value.PrefabToGetPrice)?.BaseValue ?? x.Value.BaseValue;
                            Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Value for {x.Key} change from {oldv} to {x.Value.BaseValue}!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Instance.LogError(typeof(EconomicsConstants), ex);
            }
        }

        private static void LoadPrefabsToSell()
        {
            try
            {
                // Filter not loaded
                var query = LOADED_PREFABS_TO_SELL.Where(x => !x.Value.IsLoaded);
                if (query.Any())
                {
                    while (query.Any())
                    {
                        var prefabToCheck = query.FirstOrDefault().Value;
                        foreach (var grid in prefabToCheck.Definition.CubeGrids)
                        {
                            switch (grid.GridSizeEnum)
                            {
                                case MyCubeSize.Large:
                                    prefabToCheck.Flags |= StationPrefabFlag.LargeGrid;
                                    break;
                                case MyCubeSize.Small:
                                    prefabToCheck.Flags |= StationPrefabFlag.SmallGrid;
                                    break;
                            }
                            prefabToCheck.BlockCount += grid.CubeBlocks.Count;
                            prefabToCheck.TotalPCU += grid.CubeBlocks.Sum(x => MyDefinitionManager.Static.GetCubeBlockDefinition(x.GetId())?.PCU ?? 0);
                            if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_Reactor)))
                                prefabToCheck.Flags |= StationPrefabFlag.Reactor;
                            if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_JumpDrive)))
                                prefabToCheck.Flags |= StationPrefabFlag.JumpDrive;
                            if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_MotorSuspension)))
                                prefabToCheck.Flags |= StationPrefabFlag.Rover;
                            if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_Thrust)))
                            {
                                if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_Thrust) && IsAtmThruster(MyDefinitionManager.Static.GetCubeBlockDefinition(x.GetId()))))
                                    prefabToCheck.Flags |= StationPrefabFlag.AtmThruster;
                                if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_Thrust) && IsH2Thruster(MyDefinitionManager.Static.GetCubeBlockDefinition(x.GetId()))))
                                    prefabToCheck.Flags |= StationPrefabFlag.H2Thruster;
                                if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_Thrust) && IsIonThruster(MyDefinitionManager.Static.GetCubeBlockDefinition(x.GetId()))))
                                    prefabToCheck.Flags |= StationPrefabFlag.IonThruster;
                            }
                        }
                        if (prefabToCheck.Flags.IsFlagSet(StationPrefabFlag.SmallGrid) && !prefabToCheck.Flags.IsFlagSet(StationPrefabFlag.LargeGrid))
                        {
                            if (prefabToCheck.BlockCount > 3000)
                                prefabToCheck.Category = StationPrefabCategory.Huge;
                            else if (prefabToCheck.BlockCount >= 1000 && prefabToCheck.BlockCount < 3000)
                                prefabToCheck.Category = StationPrefabCategory.Big;
                            else if (prefabToCheck.BlockCount >= 500 && prefabToCheck.BlockCount < 1000)
                                prefabToCheck.Category = StationPrefabCategory.Medium;
                            else if (prefabToCheck.BlockCount >= 250 && prefabToCheck.BlockCount < 500)
                                prefabToCheck.Category = StationPrefabCategory.Small;
                            else
                                prefabToCheck.Category = StationPrefabCategory.Tiny;
                        }
                        else
                        {
                            if (prefabToCheck.BlockCount > 1500)
                                prefabToCheck.Category = StationPrefabCategory.Huge;
                            else if (prefabToCheck.BlockCount >= 750 && prefabToCheck.BlockCount < 1500)
                                prefabToCheck.Category = StationPrefabCategory.Big;
                            else if (prefabToCheck.BlockCount >= 300 && prefabToCheck.BlockCount < 750)
                                prefabToCheck.Category = StationPrefabCategory.Medium;
                            else if (prefabToCheck.BlockCount >= 100 && prefabToCheck.BlockCount < 300)
                                prefabToCheck.Category = StationPrefabCategory.Small;
                            else
                                prefabToCheck.Category = StationPrefabCategory.Tiny;
                        }
                        prefabToCheck.IsValid = prefabToCheck.Flags.IsFlagSet(StationPrefabFlag.Rover) ||
                            prefabToCheck.Flags.IsFlagSet(StationPrefabFlag.AtmThruster) ||
                            prefabToCheck.Flags.IsFlagSet(StationPrefabFlag.H2Thruster) ||
                            prefabToCheck.Flags.IsFlagSet(StationPrefabFlag.IonThruster);
                        foreach (var grid in prefabToCheck.Definition.CubeGrids)
                        {
                            foreach (var block in grid.CubeBlocks)
                            {
                                if (block.TypeId == typeof(MyObjectBuilder_BatteryBlock))
                                {
                                    var blockDef = block as MyObjectBuilder_BatteryBlock;
                                    if (blockDef != null)
                                    {
                                        if (_blockMaxStoredPower.ContainsKey(block.GetId()))
                                        {
                                            blockDef.CurrentStoredPower = _blockMaxStoredPower[block.GetId()];
                                        }
                                        else
                                        {
                                            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(block.GetId()) as MyBatteryBlockDefinition;
                                            if (def != null)
                                            {
                                                _blockMaxStoredPower[block.GetId()] = def.MaxStoredPower;
                                                blockDef.CurrentStoredPower = _blockMaxStoredPower[block.GetId()];
                                            }
                                        }
                                    }
                                }
                                prefabToCheck.BaseValue += GetCubeBlockBaseValue(block.GetId());
                            }
                        }
                        prefabToCheck.IsLoaded = true;
                        Logging.Instance.LogInfo(typeof(EconomicsConstants), $"LoadPrefabsToSell: {prefabToCheck.PrefabName} : BASE VALUE = {prefabToCheck.BaseValue}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Instance.LogError(typeof(EconomicsConstants), ex);
            }
        }

        private static readonly ConcurrentDictionary<MyDefinitionId, float> _blockMaxStoredPower = new ConcurrentDictionary<MyDefinitionId, float>();
        private static readonly ConcurrentDictionary<MyDefinitionId, float> _blockValues = new ConcurrentDictionary<MyDefinitionId, float>();
        public static float GetCubeBlockBaseValue(MyDefinitionId blockId)
        {
            if (_blockValues.ContainsKey(blockId))
                return _blockValues[blockId];
            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(blockId);
            float value = 0;
            if (def != null)
            {
                var components = def.Components.GroupBy(x => x.Definition.Id).ToDictionary(x => x.Key, x => x.Sum(y => y.Count));
                foreach (var compId in components.Keys)
                {
                    var itemCalculated = GetItemInfo(new UniqueEntityId(compId));
                    if (itemCalculated != null)
                    {
                        value += itemCalculated.BaseValue * components[compId];
                    }
                }
            }
            _blockValues[blockId] = value;
            return Math.Max(value, 1);
        }

        private static bool IsAtmThruster(MyCubeBlockDefinition blockDef)
        {
            var thrusterDef = blockDef as MyThrustDefinition;
            if (thrusterDef != null)
            {
                var useFuel = thrusterDef.FuelConverter != null && !thrusterDef.FuelConverter.FuelId.IsNull();
                return thrusterDef.NeedsAtmosphereForInfluence && !useFuel;
            }
            return false;
        }

        private static bool IsH2Thruster(MyCubeBlockDefinition blockDef)
        {
            var thrusterDef = blockDef as MyThrustDefinition;
            if (thrusterDef != null)
            {
                return thrusterDef.FuelConverter != null && !thrusterDef.FuelConverter.FuelId.IsNull();
            }
            return false;
        }

        private static bool IsIonThruster(MyCubeBlockDefinition blockDef)
        {
            var thrusterDef = blockDef as MyThrustDefinition;
            if (thrusterDef != null)
            {
                var useFuel = thrusterDef.FuelConverter != null && !thrusterDef.FuelConverter.FuelId.IsNull();
                return !thrusterDef.NeedsAtmosphereForInfluence && !useFuel;
            }
            return false;
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

        private static readonly List<MyDefinitionId> ValueCalcLock = new List<MyDefinitionId>();
        private static List<MyDefinitionId> MappedIngots = new List<MyDefinitionId>();
        private static float GetBluePrintValue(MyBlueprintDefinitionBase bluePrint, MyPhysicalItemDefinition baseDef, ref ConcurrentDictionary<string, float> composition)
        {
            float baseValue = 0;
            Logging.Instance.LogInfo(typeof(EconomicsConstants), $"GetBluePrintValue: {baseDef.Id} : START");
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
                                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"GetBluePrintValue: {baseDef.Id} : {bluePrint.Id} get prerequisite {prerequisite.Id}");
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
                                    Logging.Instance.LogWarning(typeof(EconomicsConstants), $"GetBluePrintValue: {bluePrint.Id} had invalid prerequisite!");
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
                                baseValue = baseDef.MinimalPricePerUnit;
                            }
                        }
                    }
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"GetBluePrintValue: {baseDef.Id} : END");
                }
                finally
                {
                    ValueCalcLock.Remove(baseDef.Id);
                }
            }
            else
            {
                Logging.Instance.LogWarning(typeof(EconomicsConstants), $"GetBluePrintValue: {baseDef.Id} : Avoid stack overflow, maybe recipes got conflicted!");
            }
            Logging.Instance.LogInfo(typeof(EconomicsConstants), $"GetBluePrintValue: {baseDef.Id} : BASE VALUE = {baseValue}");
            return Math.Max(baseValue, 1);
        }

        private static BaseMaterialItem GetItemInfo(UniqueEntityId id)
        {
            BaseMaterialItem item = GetAndCalcItemInfo(id);
            if (item == null)
                item = GetAndCalcBaseItemInfo(id);
            return item;
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
                    SHOP_ITENS[id].DoForceDefinition();
                    SHOP_ITENS[id].IsLoaded = true;
                    SHOP_ITENS[id].Composition = compostion;
                }
                return SHOP_ITENS[id];
            }
            return null;
        }

        public static string GetEconomyValues()
        {
            var sb = new StringBuilder();
            var validTypes = BASE_ITENS.Keys.Select(x => x.typeId).Concat(SHOP_ITENS.Keys.Select(x => x.typeId)).Distinct().ToArray();
            foreach (var validType in validTypes)
            {
                sb.AppendLine($"Group {validType}:");
                var itens = BASE_ITENS.Where(x => x.Key.typeId == validType).Select(x => x.Value)
                    .Concat(SHOP_ITENS.Where(x => x.Key.typeId == validType).Select(x => x.Value as BaseMaterialItem))
                    .OrderBy(x => x.BaseValue).ToArray();
                foreach (var item in itens)
                {
                    sb.AppendLine($"- {item.Definition.DisplayNameText} = $ {item.BaseValue.ToString("#0.00")}:");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
        
    }
}
