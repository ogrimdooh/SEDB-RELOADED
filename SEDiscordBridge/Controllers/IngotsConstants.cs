using SEDiscordBridge.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace SEDiscordBridge.Controllers
{

    public static class IngotsConstants
    {

        public static readonly UniqueEntityId GRAVEL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.STONE_SUBTYPEID);

        public static readonly UniqueEntityId COBALT_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.COBALT_SUBTYPEID);

        public static readonly UniqueEntityId GOLD_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.GOLD_SUBTYPEID);

        public static readonly UniqueEntityId SILICON_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.SILICON_SUBTYPEID);

        public static readonly UniqueEntityId SILVER_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.SILVER_SUBTYPEID);

        public static readonly UniqueEntityId IRON_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.IRON_SUBTYPEID);

        public static readonly UniqueEntityId ALUMINUM_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.ALUMINUM_SUBTYPEID);

        public static readonly UniqueEntityId LEAD_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.LEAD_SUBTYPEID);

        public static readonly UniqueEntityId COPPER_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.COPPER_SUBTYPEID);

        public static readonly UniqueEntityId LITHIUM_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.LITHIUM_SUBTYPEID);

        public static readonly UniqueEntityId NICKEL_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.NICKEL_SUBTYPEID);

        public static readonly UniqueEntityId PLATINUM_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.PLATINUM_SUBTYPEID);

        public static readonly UniqueEntityId CARBON_POWDER_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.CARBON_SUBTYPEID);

        public static readonly UniqueEntityId URANIUM_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.URANIUM_SUBTYPEID);

        public static readonly UniqueEntityId ZINC_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.ZINC_SUBTYPEID);

        public static readonly UniqueEntityId TITANIUM_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.TITANIUM_SUBTYPEID);

        public static readonly UniqueEntityId SULFUR_POWDER_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.SULFUR_SUBTYPEID);

        public static readonly UniqueEntityId POTASSIUM_POWDER_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.POTASSIUM_SUBTYPEID);

        public static readonly UniqueEntityId BERYLLIUM_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.BERYLLIUM_SUBTYPEID);

        public static readonly UniqueEntityId MAGNESIUM_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.MAGNESIUM_SUBTYPEID);

        // Powders

        public const string CONCRETE_SUBTYPEID = "Concrete";
        public static readonly UniqueEntityId CONCRETE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), CONCRETE_SUBTYPEID);

        public const string CERAMICPOWDER_SUBTYPEID = "CeramicPowder";
        public static readonly UniqueEntityId CERAMICPOWDER_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), CERAMICPOWDER_SUBTYPEID);

        public const string COBALTCERAMICPOWDER_SUBTYPEID = "CobaltCeramicPowder";
        public static readonly UniqueEntityId COBALTCERAMICPOWDER_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), COBALTCERAMICPOWDER_SUBTYPEID);

        public const string SAND_SUBTYPEID = "Sand";
        public static readonly UniqueEntityId SAND_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), SAND_SUBTYPEID);

        public const string GUNPOWDER_SUBTYPEID = "GunPowder";
        public static readonly UniqueEntityId GUNPOWDER_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), GUNPOWDER_SUBTYPEID);

        public const string SOLIDFUEL_SUBTYPEID = "SolidFuel";
        public static readonly UniqueEntityId SOLIDFUEL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), SOLIDFUEL_SUBTYPEID);

        public const string MGKFUEL_SUBTYPEID = "MgKFuel";
        public static readonly UniqueEntityId MGKFUEL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), MGKFUEL_SUBTYPEID);
        
        public const string SALT_SUBTYPEID = "Salt";
        public static readonly UniqueEntityId SALT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), SALT_SUBTYPEID);
        
        public const string FLOUR_SUBTYPEID = "Flour";
        public static readonly UniqueEntityId FLOUR_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), FLOUR_SUBTYPEID);

        public const string COFFEEPOWDER_SUBTYPEID = "CoffeePowder";
        public static readonly UniqueEntityId COFFEEPOWDER_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), COFFEEPOWDER_SUBTYPEID);

        public static readonly UniqueEntityId CHLORINE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.CHLORINE_SUBTYPEID);

        public static readonly UniqueEntityId SODIUM_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.SODIUM_SUBTYPEID);

        public static readonly UniqueEntityId NITRATE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.NITRATE_SUBTYPEID);

        // Others  

        public const string LATEX_SUBTYPEID = "Latex";
        public static readonly UniqueEntityId LATEX_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), LATEX_SUBTYPEID);

        public const string RUBBER_SUBTYPEID = "Rubber";
        public static readonly UniqueEntityId RUBBER_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), RUBBER_SUBTYPEID);

        public static readonly UniqueEntityId FIREWOOD_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), OreConstants.WOODLOG_SUBTYPEID);

        // Alloy

        public const string ALUMINUMMG_INGOT_SUBTYPEID = "AluminumMg";
        public static readonly UniqueEntityId ALUMINUMMG_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), ALUMINUMMG_INGOT_SUBTYPEID);

        public const string STEEL_INGOT_SUBTYPEID = "Steel";
        public static readonly UniqueEntityId STEEL_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), STEEL_INGOT_SUBTYPEID);

        public const string COBALTSTEEL_INGOT_SUBTYPEID = "CobaltSteel";
        public static readonly UniqueEntityId COBALTSTEEL_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), COBALTSTEEL_INGOT_SUBTYPEID);

        public const string BRASS_SUBTYPEID = "Brass";
        public static readonly UniqueEntityId BRASS_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), BRASS_SUBTYPEID);

        public const string BERYLLIUMSTEEL_SUBTYPEID = "BerylliumSteel";
        public static readonly UniqueEntityId BERYLLIUMSTEEL_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), BERYLLIUMSTEEL_SUBTYPEID);

        public const string BERYLLIUMCOPPER_SUBTYPEID = "BerylliumCopper";
        public static readonly UniqueEntityId BERYLLIUMCOPPER_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), BERYLLIUMCOPPER_SUBTYPEID);

        public const string TITANIUMSTEEL_SUBTYPEID = "TitaniumSteel";
        public static readonly UniqueEntityId TITANIUMSTEEL_INGOT_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ingot), TITANIUMSTEEL_SUBTYPEID);

    }

}
