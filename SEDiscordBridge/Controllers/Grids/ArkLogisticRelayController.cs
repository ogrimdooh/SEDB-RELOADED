using EmptyKeys.UserInterface.Generated.DataTemplatesContracts_Bindings;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.FunctionalGrids;
using SEDiscordBridge.Storage.SeasonMeta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace SEDiscordBridge.Controllers.Grids
{

    public class ArkLogisticRelayController : BaseFunctionalGridController
    {

        public static ArkLogisticRelayController Instance { get; private set; }

        public static void Register()
        {
            if (Instance == null)
            {
                Instance = new ArkLogisticRelayController();
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

        public readonly static InterfaceType INTERFACE_TYPE_SELECTCARGOTRANSFERSCOPE = "SELECTCARGOTRANSFERSCOPE";
        public readonly static InterfaceType INTERFACE_TYPE_CONFIRMCARGOTRANSFER = "CONFIRMCARGOTRANSFER";
        public readonly static InterfaceType INTERFACE_TYPE_NOCARGOTOTRASNFER = "NOCARGOTOTRASNFER";
        public readonly static InterfaceType INTERFACE_TYPE_CARGOTRANSFERERROR = "CARGOTRANSFERERROR";
        public readonly static InterfaceType INTERFACE_TYPE_CARGOTRANSFERCOMPLETED = "CARGOTRANSFERCOMPLETED";

        public readonly static ServiceType TERMINAL_SERVICE_TYPE_SUBMITRESOURCES = "SUBMITRESOURCES";

        protected readonly ArkTerminalService TERMINAL_SERVICE_SUBMITRESOURCES = new ArkTerminalService(
            "delivery system",
            @"Submit recovered resources to The Second 
Dawn logistics network.

Delivered cargo will be processed by D.A.W.N. 
and added to the current Ark Jump objectives.

",
            (controller, terminal) => {
                var grids = controller.GetConnectedGridsByPlayerId(terminal.LoggedPlayer);
                if (!grids.Any())
                    return INTERFACE_TYPE_NOVESSELCONNECTED;
                if (grids.Count > 1)
                {
                    terminal.SetValue("next_interface", INTERFACE_TYPE_SELECTCARGOTRANSFERSCOPE);
                    return INTERFACE_TYPE_SELECTVESSEL;
                }
                return INTERFACE_TYPE_SELECTCARGOTRANSFERSCOPE;
            }
        );

        protected override IArkTerminalBocks CreateNewTerminalBlock(string name)
        {
            return new ArkTerminalBocks<ArkLogisticRelayController>(this) { Name = name };
        }

        public override long GetTargetGridId()
        {
            return ServerFunctionalGridsStorage.Instance.LogisticRelayEntityId;
        }

        public override StationType GetStationType()
        {
            return StationType.OrbitalStation;
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

        protected override bool HasRepairService()
        {
            return true;
        }

        protected override void OnLoadServices()
        {
            AddService(TERMINAL_SERVICE_TYPE_SUBMITRESOURCES, TERMINAL_SERVICE_SUBMITRESOURCES);
        }

        protected override void OnLoadInterfaces()
        {
            AddInterface(INTERFACE_TYPE_SELECTCARGOTRANSFERSCOPE, SELECTCARGOTRASNFERSCOPE_INTERFACE);
            AddInterface(INTERFACE_TYPE_CONFIRMCARGOTRANSFER, CONFIRMCARGOTRASNFER_INTERFACE);
            AddInterface(INTERFACE_TYPE_NOCARGOTOTRASNFER, NOCARGOTOTRASNFER_INTERFACE);
            AddInterface(INTERFACE_TYPE_CARGOTRANSFERERROR, CARGOTRASNFERERROR_INTERFACE);
            AddInterface(INTERFACE_TYPE_CARGOTRANSFERCOMPLETED, CARGOTRASNFERCOMPLETED_INTERFACE);
        }

        public bool DoCalcTrasnferScope(IArkTerminalBocks tInterface, int scope)
        {
            var index = tInterface.GetValue<int>("vessel_index");
            var inventories = DoGetInventoriesToInteract(tInterface, index, scope);
            var validInventories = DoFilterInventoriesToInteract(inventories);
            if (validInventories.Any())
            {
                var itemCount = (long)validInventories.Values.Sum(x => x.Values.Sum());
                var itemMass = validInventories.Values.Sum(x => x.Sum(y => ItensConstants.GetItemMass(y.Key) * y.Value));
                tInterface.SetValue("item_mass", itemMass);
                tInterface.SetValue("item_count", itemCount);
                return true;
            }
            return false;
        }

        public Dictionary<MyInventory, Dictionary<MyDefinitionId, float>> DoFilterInventoriesToInteract(List<MyInventory> inventories)
        {
            var validInventories = new Dictionary<MyInventory, Dictionary<MyDefinitionId, float>>();
            if (inventories.Any())
            {
                var validIds = SEDBStorage.Instance.SeasonMetaConfig.GetValidItensIds().Select(x => x.ToMyDefinitionId()).ToList();
                foreach (var inventory in inventories)
                {
                    var validItens = inventory.GetItems().Where(x => validIds.Any(y => x.Content.TypeId == y.TypeId && x.Content.SubtypeId == y.SubtypeId))
                        .GroupBy(x => new MyDefinitionId(x.Content.TypeId, x.Content.SubtypeId))
                        .ToDictionary(x => x.Key, x => x.Sum(y => (float)y.Amount));
                    if (validItens.Any())
                    {
                        validInventories.Add(inventory, validItens);
                    }
                }
            }
            return validInventories;
        }

        public List<MyInventory> DoGetInventoriesToInteract(IArkTerminalBocks tInterface, int index, int scope)
        {
            List<MyInventory> inventories = new List<MyInventory>();
            var grids = GetConnectedGridsByPlayerId(tInterface.LoggedPlayer);
            if (grids.Any() && index >= 0 && index < grids.Count)
            {
                var grid = grids[index];
                switch (scope)
                {
                    case 0:
                        inventories.AddRange(grid.Inventories.Where(x => x is MyCargoContainer).Select(x => x.GetInventory()));
                        break;
                    case 1:
                        var gridTerminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                        if (gridTerminalSystem != null)
                        {
                            var gridGroup = gridTerminalSystem.GetBlockGroupWithName("DELIVERY");
                            if (gridGroup != null)
                            {
                                var cargoBlocks = new List<IMyTerminalBlock>();
                                gridGroup.GetBlocksOfType<MyCargoContainer>(cargoBlocks);
                                inventories.AddRange(cargoBlocks.Select(x => x.GetInventory() as MyInventory));
                            }
                        }
                        break;
                }
            }
            return inventories;
        }

        private static ArkTerminalInterface SELECTCARGOTRASNFERSCOPE_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

Select cargo transfer scope:

1 - Transfer all cargo containers
2 - Only container on group: DELIVERY
3 - Cancel",
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Num1, ArkTerminalAction.Num2, ArkTerminalAction.Num3 },
            OnOpen = (controller, terminal) =>
            {

            },
            OnInteract = (controller, terminal, action, playerId) =>
            {
                var scope = -1;
                switch (action)
                {
                    case ArkTerminalAction.Num1:
                        scope = 0;
                        break;
                    case ArkTerminalAction.Num2:
                        scope = 1;
                        break;
                    case ArkTerminalAction.Num3:
                        return INTERFACE_TYPE_SERVICESELECT;
                }
                if (scope >= 0)
                {
                    terminal.SetValue("transfer_scope", scope);
                    if ((controller as ArkLogisticRelayController).DoCalcTrasnferScope(terminal, scope))
                        return INTERFACE_TYPE_CONFIRMCARGOTRANSFER;
                    else
                        return INTERFACE_TYPE_NOCARGOTOTRASNFER;
                }
                return INTERFACE_TYPE_NONE;
            }
        };

        private bool DoExecuteCargoTrasnfer(IArkTerminalBocks tInterface, int index, long entityId, int scope)
        {
            var inventories = DoGetInventoriesToInteract(tInterface, index, scope);
            var validInventories = DoFilterInventoriesToInteract(inventories);
            return SeasonDonationController.DoRegisterPlayerDonation(tInterface.LoggedSteamId, SeasonMetaDonationOrigin.Player, validInventories);
        }

        protected override void OnAfterInit()
        {

        }

        private static ArkTerminalInterface CONFIRMCARGOTRASNFER_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

Confirm cargo submission to The Second Dawn:

Total mass: {item_mass} Kg
Total items: {item_count}

This operation will transfer the selected cargo 
to the Ark logistics network.

1 - Confirm
2 - Decline",
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Num1, ArkTerminalAction.Num2 },
            OnOpen = (controller, terminal) =>
            {

            },
            OnInteract = (controller, terminal, action, playerId) =>
            {
                switch (action)
                {
                    case ArkTerminalAction.Num1:
                        var scope = terminal.GetValue<int>("transfer_scope");
                        var index = terminal.GetValue<int>("vessel_index");
                        var vesselId = terminal.GetValue<long>("vessel_id");
                        if ((controller as ArkLogisticRelayController).DoExecuteCargoTrasnfer(terminal, index, vesselId, scope))
                            return INTERFACE_TYPE_CARGOTRANSFERCOMPLETED;
                        else
                            return INTERFACE_TYPE_CARGOTRANSFERERROR;
                    case ArkTerminalAction.Num2:
                                return INTERFACE_TYPE_SERVICESELECT;
                            }
                return INTERFACE_TYPE_NONE;
            }
        };

        private static ArkTerminalInterface NOCARGOTOTRASNFER_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

No valid cargo to transfer.

Add valid resources to your own ships cargo 
before starting cargo transfer.

1 - Return",
            BackgroundColor = Color.DarkRed,
            FontColor = Color.White,
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Num1 },
            OnOpen = (controller, terminal) =>
            {

            },
            OnInteract = (controller, terminal, action, playerId) =>
            {
                return INTERFACE_TYPE_SERVICESELECT;
            }
        };

        private static ArkTerminalInterface CARGOTRASNFERERROR_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

Transfer denied.

The selected vessel is no longer connected or 
is not registered under your control.

Reconnect your vessel and try again.

1 - Return",
            BackgroundColor = Color.DarkRed,
            FontColor = Color.White,
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Num1 },
            OnOpen = (controller, terminal) =>
            {

            },
            OnInteract = (controller, terminal, action, playerId) =>
            {
                return INTERFACE_TYPE_SERVICESELECT;
            }
        };

        private static ArkTerminalInterface CARGOTRASNFERCOMPLETED_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

Cargo transfer complete.

D.A.W.N. has processed the submitted resources 
and updated the Ark manifest.

Thank you for supporting the next jump.

1 - Return",
            BackgroundColor = Color.DarkGreen,
            FontColor = Color.White,
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Num1 },
            OnOpen = (controller, terminal) =>
            {

            },
            OnInteract = (controller, terminal, action, playerId) =>
            {
                return INTERFACE_TYPE_SERVICESELECT;
            }
        };

    }
}
