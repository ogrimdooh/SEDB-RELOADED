using SEDiscordBridge.Entities.Base;
using VRage.Game;

namespace SEDiscordBridge.Controllers.Grids
{
    public static class ItensConstants
    {

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

    }
}
