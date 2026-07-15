using SEDiscordBridge.Entities.Base;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace SEDiscordBridge.Controllers
{

    public static class OreConstants
    {

        // Natural

        public const string STONE_SUBTYPEID = "Stone";
        public static readonly UniqueEntityId STONE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), STONE_SUBTYPEID);

        public const string IRON_SUBTYPEID = "Iron";
        public static readonly UniqueEntityId IRON_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), IRON_SUBTYPEID);

        public const string ZINC_SUBTYPEID = "Zinc";
        public static readonly UniqueEntityId ZINC_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ZINC_SUBTYPEID);

        public const string COPPER_SUBTYPEID = "Copper";
        public static readonly UniqueEntityId COPPER_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), COPPER_SUBTYPEID);

        public const string NICKEL_SUBTYPEID = "Nickel";
        public static readonly UniqueEntityId NICKEL_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), NICKEL_SUBTYPEID);

        public const string ALUMINUM_SUBTYPEID = "Aluminum";
        public static readonly UniqueEntityId ALUMINUM_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ALUMINUM_SUBTYPEID);

        public const string SILICON_SUBTYPEID = "Silicon";
        public static readonly UniqueEntityId SILICON_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), SILICON_SUBTYPEID);

        public const string CARBON_SUBTYPEID = "Carbon";
        public static readonly UniqueEntityId CARBON_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), CARBON_SUBTYPEID);

        public const string SULFUR_SUBTYPEID = "Sulfur";
        public static readonly UniqueEntityId SULFUR_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), SULFUR_SUBTYPEID);

        public const string POTASSIUM_SUBTYPEID = "Potassium";
        public static readonly UniqueEntityId POTASSIUM_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), POTASSIUM_SUBTYPEID);

        public const string LEAD_SUBTYPEID = "Lead";
        public static readonly UniqueEntityId LEAD_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), LEAD_SUBTYPEID);

        public const string COBALT_SUBTYPEID = "Cobalt";
        public static readonly UniqueEntityId COBALT_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), COBALT_SUBTYPEID);

        public const string LITHIUM_SUBTYPEID = "Lithium";
        public static readonly UniqueEntityId LITHIUM_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), LITHIUM_SUBTYPEID);

        public const string SILVER_SUBTYPEID = "Silver";
        public static readonly UniqueEntityId SILVER_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), SILVER_SUBTYPEID);

        public const string GOLD_SUBTYPEID = "Gold";
        public static readonly UniqueEntityId GOLD_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), GOLD_SUBTYPEID);

        public const string PLATINUM_SUBTYPEID = "Platinum";
        public static readonly UniqueEntityId PLATINUM_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), PLATINUM_SUBTYPEID);

        public const string MAGNESIUM_SUBTYPEID = "Magnesium";
        public static readonly UniqueEntityId MAGNESIUM_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), MAGNESIUM_SUBTYPEID);

        public const string URANIUM_SUBTYPEID = "Uranium";
        public static readonly UniqueEntityId URANIUM_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), URANIUM_SUBTYPEID);

        public const string TITANIUM_SUBTYPEID = "Titanium";
        public static readonly UniqueEntityId TITANIUM_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), TITANIUM_SUBTYPEID);

        public const string BERYLLIUM_SUBTYPEID = "Beryllium";
        public static readonly UniqueEntityId BERYLLIUM_ORE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), BERYLLIUM_SUBTYPEID);

        public const string SOIL_SUBTYPEID = "Soil";
        public static readonly UniqueEntityId SOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), SOIL_SUBTYPEID);

        public const string MUD_SUBTYPEID = "Mud";
        public static readonly UniqueEntityId MUD_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), MUD_SUBTYPEID);

        public const string ALIENSOIL_SUBTYPEID = "AlienSoil";
        public static readonly UniqueEntityId ALIENSOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ALIENSOIL_SUBTYPEID);

        public const string ASTEROIDSOIL_SUBTYPEID = "AsteroidSoil";
        public static readonly UniqueEntityId ASTEROIDSOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ASTEROIDSOIL_SUBTYPEID);

        public const string DESERTSOIL_SUBTYPEID = "DesertSoil";
        public static readonly UniqueEntityId DESERTSOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), DESERTSOIL_SUBTYPEID);

        public const string VULCANICSOIL_SUBTYPEID = "VulcanicSoil";
        public static readonly UniqueEntityId VULCANICSOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), VULCANICSOIL_SUBTYPEID);

        public const string SULFURICSOIL_SUBTYPEID = "SulfuricSoil";
        public static readonly UniqueEntityId SULFURICSOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), SULFURICSOIL_SUBTYPEID);

        public const string FERROUSSOIL_SUBTYPEID = "FerrousSoil";
        public static readonly UniqueEntityId FERROUSSOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), FERROUSSOIL_SUBTYPEID);

        public const string CHROMESOIL_SUBTYPEID = "ChromeSoil";
        public static readonly UniqueEntityId CHROMESOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), CHROMESOIL_SUBTYPEID);

        public const string MILENARSOIL_SUBTYPEID = "MilenarSoil";
        public static readonly UniqueEntityId MILENARSOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), MILENARSOIL_SUBTYPEID);

        public const string MOONSOIL_SUBTYPEID = "MoonSoil";
        public static readonly UniqueEntityId MOONSOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), MOONSOIL_SUBTYPEID);

        public const string ORGANIC_SUBTYPEID = "Organic";
        public static readonly UniqueEntityId ORGANIC_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ORGANIC_SUBTYPEID);

        public const string ICE_SUBTYPEID = "Ice";
        public static readonly UniqueEntityId ICE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), ICE_SUBTYPEID);

        public const string STONEICE_SUBTYPEID = "StoneIce";
        public static readonly UniqueEntityId STONEICE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), STONEICE_SUBTYPEID);

        public const string TOXICICE_SUBTYPEID = "ToxicIce";
        public static readonly UniqueEntityId TOXICICE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), TOXICICE_SUBTYPEID);

        public const string WOODLOG_SUBTYPEID = "Wood";
        public static readonly UniqueEntityId WOODLOG_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), WOODLOG_SUBTYPEID);

        public const string SAWDUST_SUBTYPEID = "Sawdust";
        public static readonly UniqueEntityId SAWDUST_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), SAWDUST_SUBTYPEID);

        public const string LEAF_SUBTYPEID = "Leaf";
        public static readonly UniqueEntityId LEAF_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), LEAF_SUBTYPEID);

        public const string TWIG_SUBTYPEID = "Twig";
        public static readonly UniqueEntityId TWIG_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), TWIG_SUBTYPEID);

        public const string BRANCH_SUBTYPEID = "Branch";
        public static readonly UniqueEntityId BRANCH_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), BRANCH_SUBTYPEID);

        public const string CM_IRON_FE_SUBTYPEID = "IcyIron";
        public static readonly UniqueEntityId CM_IRON_FE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), CM_IRON_FE_SUBTYPEID);

        public const string CM_DENSE_IRON_FE_SUBTYPEID = "DenseIron";
        public static readonly UniqueEntityId CM_DENSE_IRON_FE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), CM_DENSE_IRON_FE_SUBTYPEID);

        public const string CHLORINE_SUBTYPEID = "Chlorine";
        public static readonly UniqueEntityId CHLORINE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), CHLORINE_SUBTYPEID);

        public const string SODIUM_SUBTYPEID = "Sodium";
        public static readonly UniqueEntityId SODIUM_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), SODIUM_SUBTYPEID);

        public const string NITRATE_SUBTYPEID = "Nitrate";
        public static readonly UniqueEntityId NITRATE_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), NITRATE_SUBTYPEID);

        // Others

        public const string MINERALOIL_SUBTYPEID = "MineralOil";
        public static readonly UniqueEntityId MINERALOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), MINERALOIL_SUBTYPEID);

        public const string VEGETALOIL_SUBTYPEID = "VegetalOil";
        public static readonly UniqueEntityId VEGETALOIL_ID = new UniqueEntityId(typeof(MyObjectBuilder_Ore), VEGETALOIL_SUBTYPEID);

    }

}
