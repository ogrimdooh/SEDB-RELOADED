using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Storage.FunctionalGrids;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using VRageMath;

namespace SEDiscordBridge.Controllers.Grids
{
    public class ArkGrindingServiceController : BaseFunctionalGridController
    {

        public static ArkGrindingServiceController Instance { get; private set; }

        public readonly static ServiceType TERMINAL_SERVICE_TYPE_GRINDING = "GRINDING";

        public const long GRINDING_COST = 1000;
        public const long GRINDING_TIME = 120;

        protected readonly ArkTerminalService TERMINAL_SERVICE_GRINDING = new ArkTerminalService(
            "repair service",
            @"Use this system to request D.A.W.N. 
grinding support.

Cost: " + GRINDING_COST + @" credits
Time: " + GRINDING_TIME + @" seconds

",
            (controller, terminal) => {
                var timerBlock = (controller as ArkGrindingServiceController).ARKSTARTGRINDING;
                if (timerBlock != null)
                {
                    var balance = MyBankingSystem.GetBalance(terminal.LoggedPlayer);
                    if (GRINDING_COST > balance)
                    {
                        terminal.SetValue("balance", balance);
                        return INTERFACE_TYPE_INSUFFICIENTFUNDS;
                    }
                    if (MyBankingSystem.ChangeBalance(terminal.LoggedPlayer, -GRINDING_COST))
                    {
                        (timerBlock as IMyTriggerableBlock).Trigger();
                    }
                    return INTERFACE_TYPE_GRINDINGSTART;
                }
                return INTERFACE_TYPE_SERVICESELECT;
            }
        );

        public readonly static InterfaceType INTERFACE_TYPE_GRINDINGSTART = "GRINDINGSTART";

        private static ArkTerminalInterface GRINDINGSTART_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK TERMINAL SYSTEM

Grinding support started.

Attention!

Stay away from the machinery while 
it is operating.",
            BackgroundColor = Color.Orange,
            FontColor = Color.White,
            AutoInteractAfter = GRINDING_TIME * 1000,
            ValidActions = new ArkTerminalAction[] { },
            OnOpen = (controller, terminal) =>
            {

            },
            OnInteract = (controller, terminal, action, playerId) =>
            {
                return INTERFACE_TYPE_HOME;
            }
        };

        public static void Register()
        {
            if (Instance == null)
            {
                Instance = new ArkGrindingServiceController();
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

        public MyTimerBlock ARKSTARTGRINDING { get; protected set; }

        protected override IArkTerminalBocks CreateNewTerminalBlock(string name)
        {
            return new ArkTerminalBocks<ArkGrindingServiceController>(this) { Name = name };
        }

        public override long GetTargetGridId()
        {
            return ServerFunctionalGridsStorage.Instance.GrindingServiceEntityId;
        }

        public override StationType GetStationType()
        {
            return StationType.PlanetStation;
        }

        public override StationLevel GetStationLevel()
        {
            return StationLevel.Small;
        }

        public override FactionType GetFactionType()
        {
            return FactionType.Shipyard;
        }

        protected override Vector2 GetEconomyCycleTime()
        {
            return new Vector2(1350, 2250);
        }

        protected override bool HasRepairService()
        {
            return false;
        }

        protected override void OnLoadServices()
        {
            AddService(TERMINAL_SERVICE_TYPE_GRINDING, TERMINAL_SERVICE_GRINDING);
        }

        protected override void OnAfterInit()
        {
            var timerGrp = ARKGRIDGROUPS.FirstOrDefault(x => x.Name.ToString().StartsWith("ARK-START-GRIND"));
            if (timerGrp != null)
            {
                var timerBlocks = new List<MyTimerBlock>();
                timerGrp.GetBlocksOfType<MyTimerBlock>(timerBlocks);
                ARKSTARTGRINDING = timerBlocks.FirstOrDefault();
                if (ARKSTARTGRINDING == null)
                {
                    Logging.Instance.LogWarning(typeof(ArkGrindingServiceController), $"OnAfterInit: ARKSTARTGRINDING block not found in group {timerGrp.Name}");
                }
            }
        }

        protected override void OnLoadInterfaces()
        {
            AddInterface(INTERFACE_TYPE_GRINDINGSTART, GRINDINGSTART_INTERFACE);
        }

    }
}
