using Sandbox.Definitions;
using SEDiscordBridge.Entities.Base;
using System.Collections.Concurrent;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;

namespace SEDiscordBridge.Controllers
{
    public static class ItensConstants
    {

        public const string PROTOTECHSCRAP_SUBTYPEID = "PrototechScrap";
        public static readonly UniqueEntityId PROTOTECHSCRAP_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), PROTOTECHSCRAP_SUBTYPEID);

        public const string PROTOTECHFRAME_SUBTYPEID = "PrototechFrame";
        public static readonly UniqueEntityId PROTOTECHFRAME_ID = new UniqueEntityId(typeof(MyObjectBuilder_Component), PROTOTECHFRAME_SUBTYPEID);

        public const string CANVAS_SUBTYPEID = "Canvas";
        public static readonly UniqueEntityId CANVAS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Component), CANVAS_SUBTYPEID);

        public const string ZONECHIP_SUBTYPEID = "ZoneChip";
        public static readonly UniqueEntityId ZONECHIP_ID = new UniqueEntityId(typeof(MyObjectBuilder_Component), ZONECHIP_SUBTYPEID);

        public const string DAWNDROPSIGNALEXPLORER_SUBTYPEID = "DAWNDropSignalExplorer";
        public static readonly UniqueEntityId DAWNDROPSIGNALEXPLORER_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), DAWNDROPSIGNALEXPLORER_SUBTYPEID);

        public const string DAWNDROPSIGNALLITE_SUBTYPEID = "DAWNDropSignalLite";
        public static readonly UniqueEntityId DAWNDROPSIGNALLITE_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), DAWNDROPSIGNALLITE_SUBTYPEID);

        public const string DAWNDROPSIGNALSURVIVAL_SUBTYPEID = "DAWNDropSignalSurvival";
        public static readonly UniqueEntityId DAWNDROPSIGNALSURVIVAL_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), DAWNDROPSIGNALSURVIVAL_SUBTYPEID);

        public const string DAWNDROPSIGNALMARINE_SUBTYPEID = "DAWNDropSignalMarine";
        public static readonly UniqueEntityId DAWNDROPSIGNALMARINE_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), DAWNDROPSIGNALMARINE_SUBTYPEID);

        public const string DAWNDROPSIGNALVOYAGER_SUBTYPEID = "DAWNDropSignalVoyager";
        public static readonly UniqueEntityId DAWNDROPSIGNALVOYAGER_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), DAWNDROPSIGNALVOYAGER_SUBTYPEID);

        public const string COFFEE_SUBTYPEID = "Coffee";
        public static readonly UniqueEntityId COFFEE_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), COFFEE_SUBTYPEID);

        public const string FRUIT_SUBTYPEID = "Fruit";
        public static readonly UniqueEntityId FRUIT_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), FRUIT_SUBTYPEID);

        public const string POTATOES_SUBTYPEID = "Potatoes";
        public static readonly UniqueEntityId POTATOES_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), POTATOES_SUBTYPEID);

        public const string VEGETABLES_SUBTYPEID = "Vegetables";
        public static readonly UniqueEntityId VEGETABLES_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), VEGETABLES_SUBTYPEID);

        public const string MUSHROOMS_SUBTYPEID = "Mushrooms";
        public static readonly UniqueEntityId MUSHROOMS_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), MUSHROOMS_SUBTYPEID);

        public const string MAMMALMEATRAW_SUBTYPEID = "MammalMeatRaw";
        public static readonly UniqueEntityId MAMMALMEATRAW_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), MAMMALMEATRAW_SUBTYPEID);

        public const string INSECTMEATRAW_SUBTYPEID = "InsectMeatRaw";
        public static readonly UniqueEntityId INSECTMEATRAW_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), INSECTMEATRAW_SUBTYPEID);

        public const string MEALPACK_HARDTACK_SUBTYPEID = "MealPack_Hardtack";
        public static readonly UniqueEntityId MEALPACK_HARDTACK_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), MEALPACK_HARDTACK_SUBTYPEID);

        public const string MEALPACK_FOODPASTE_SUBTYPEID = "MealPack_FoodPaste";
        public static readonly UniqueEntityId MEALPACK_FOODPASTE_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), MEALPACK_FOODPASTE_SUBTYPEID);

        public const string MEALPACK_CLANGCRUNCHIES_SUBTYPEID = "MealPack_ClangCrunchies";
        public static readonly UniqueEntityId MEALPACK_CLANGCRUNCHIES_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), MEALPACK_CLANGCRUNCHIES_SUBTYPEID);

        public const string MEALPACK_BANANABEEF_SUBTYPEID = "MealPack_BananaBeef";
        public static readonly UniqueEntityId MEALPACK_BANANABEEF_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), MEALPACK_BANANABEEF_SUBTYPEID);

        public const string MEALPACK_SYNTHLOAF_SUBTYPEID = "MealPack_SynthLoaf";
        public static readonly UniqueEntityId MEALPACK_SYNTHLOAF_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), MEALPACK_SYNTHLOAF_SUBTYPEID);

        public const string GRAIN_SUBTYPEID = "Grain";
        public static readonly UniqueEntityId GRAIN_ID = new UniqueEntityId(typeof(MyObjectBuilder_PhysicalObject), GRAIN_SUBTYPEID);

        public const string ALGAE_SUBTYPEID = "Algae";
        public static readonly UniqueEntityId ALGAE_ID = new UniqueEntityId(typeof(MyObjectBuilder_PhysicalObject), ALGAE_SUBTYPEID);

        public const string NATO_5P56X45MM_SUBTYPEID = "NATO_5p56x45mm";
        public static readonly UniqueEntityId NATO_5P56X45MM_ID = new UniqueEntityId(typeof(MyObjectBuilder_AmmoMagazine), NATO_5P56X45MM_SUBTYPEID);

        public const string WOOD_SUBTYPEID = "Wood";
        public static readonly UniqueEntityId WOOD_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), WOOD_SUBTYPEID);

        public const string CHLORINE_SUBTYPEID = "Chlorine";
        public static readonly UniqueEntityId CHLORINE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), CHLORINE_SUBTYPEID);

        public const string SODIUM_SUBTYPEID = "Sodium";
        public static readonly UniqueEntityId SODIUM_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), SODIUM_SUBTYPEID);

        public const string NITRATE_SUBTYPEID = "Nitrate";
        public static readonly UniqueEntityId NITRATE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), NITRATE_SUBTYPEID);

        private static ConcurrentDictionary<UniqueEntityId, MyObjectBuilder_Base> BUILDERS_CACHE = new ConcurrentDictionary<UniqueEntityId, MyObjectBuilder_Base>();

        public static T GetBuilder<T>(UniqueEntityId id, bool cache = true) where T : MyObjectBuilder_Base
        {
            if (cache && BUILDERS_CACHE.ContainsKey(id))
                return BUILDERS_CACHE[id] as T;
            var builder = MyObjectBuilderSerializer.CreateNewObject(id.DefinitionId) as T;
            BUILDERS_CACHE[id] = builder;
            return builder as T;
        }

        public static MyObjectBuilder_PhysicalObject GetPhysicalObjectBuilder(UniqueEntityId id)
        {
            return GetBuilder<MyObjectBuilder_PhysicalObject>(id);
        }

        private static ConcurrentDictionary<MyDefinitionId, float> _massCache = new ConcurrentDictionary<MyDefinitionId, float>();
        public static float GetItemMass(MyDefinitionId item)
        {
            if (_massCache.ContainsKey(item))
                return _massCache[item];
            var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(item);
            if (def != null)
            {
                _massCache[item] = def.Mass;
                return def.Mass;
            }
            return 0f;
        }

    }
}
