using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Storage.FunctionalGrids;

namespace SEDiscordBridge.Controllers.Grids
{
    public class ArkGroundBaseController : BaseFunctionalGridController
    {

        public static ArkGroundBaseController Instance { get; private set; }

        public static void Init()
        {
            if (Instance == null)
            {
                Instance = new ArkGroundBaseController();
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

        protected override long GetTargetGridId()
        {
            return ServerFunctionalGridsStorage.Instance.GroundBaseEntityId;
        }

        protected override StationType GetStationType()
        {
            return StationType.PlanetStation;
        }

        protected override StationLevel GetStationLevel()
        {
            return StationLevel.Large;
        }

        protected override FactionType GetFactionType()
        {
            return FactionType.All;
        }

        protected override void LoadServices()
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
