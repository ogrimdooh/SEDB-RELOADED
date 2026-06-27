using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Storage.FunctionalGrids;
using VRageMath;

namespace SEDiscordBridge.Controllers.Grids
{
    public class ArkGroundBaseController : BaseFunctionalGridController
    {

        public static ArkGroundBaseController Instance { get; private set; }

        public static void Register()
        {
            if (Instance == null)
            {
                Instance = new ArkGroundBaseController();
            }
            Instance.DoRegister();
        }

        public static void Init()
        {
            if (Instance == null)
            {
                Register();
            }
            Instance.DoInit();
        }

        public static void Dispose()
        {
            if (Instance != null)
                Instance.DoDispose();
        }

        protected override IArkTerminalBocks CreateNewTerminalBlock(string name)
        {
            return new ArkTerminalBocks<ArkGroundBaseController>(this) { Name = name };
        }

        public override long GetTargetGridId()
        {
            return ServerFunctionalGridsStorage.Instance.GroundBaseEntityId;
        }

        public override StationType GetStationType()
        {
            return StationType.PlanetStation;
        }

        public override StationLevel GetStationLevel()
        {
            return StationLevel.Large;
        }

        public override FactionType GetFactionType()
        {
            return FactionType.All;
        }

        protected override Vector2 GetEconomyCycleTime()
        {
            return new Vector2(1350, 2250);
        }

        protected override void OnLoadServices()
        {

        }

        protected override void OnAfterInit()
        {

        }

        protected override void OnLoadInterfaces()
        {

        }

    }
}
