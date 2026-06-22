using EmptyKeys.UserInterface.Generated.DataTemplatesContracts_Bindings;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SEDiscordBridge.Patches;
using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.SeasonMeta;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Documents;
using VRage;
using VRage.Game;
using VRageMath;
using static System.Collections.Specialized.BitVector32;
using static VRage.MyMiniDump;

namespace SEDiscordBridge
{

    public static class ArkLogisticRelayController
    {

        public enum ArkTerminalAction
        {
            None = 0,
            Login = 1,
            Prior = 2,
            Next = 3,
            Enter = 4,
            Num1 = 5,
            Num2 = 6,
            Num3 = 7
        }

        private static Dictionary<int, ArkTerminalAction> COMPUTER_BTN_MAP = new Dictionary<int, ArkTerminalAction>()
        {
            { 0, ArkTerminalAction.Num1 },
            { 1, ArkTerminalAction.Num2 },
            { 2, ArkTerminalAction.Num3 }
        };

        private static Dictionary<int, ArkTerminalAction> NAVIGATION_BTN_MAP = new Dictionary<int, ArkTerminalAction>()
        {
            { 0, ArkTerminalAction.Login },
            { 1, ArkTerminalAction.Prior },
            { 2, ArkTerminalAction.Next },
            { 3, ArkTerminalAction.Enter }
        };

        public abstract class BaseArkTerminalInterface
        {
            public Color BackgroundColor { get; set; } = Color.Black;
            public Color FontColor { get; set; } = Color.Orange;
            public float FontSize { get; set; } = 0.9f;
            public long AutoInteractAfter { get; set; } = 0;
            public bool NeedLogin { get; set; } = true;
        }

        public class ArkTerminalInterface : BaseArkTerminalInterface
        {
            public string Text { get; set; }
            public ArkTerminalAction[] ValidActions { get; set; } = new ArkTerminalAction[0];
            public Action<ArkTerminalBocks> OnOpen { get; set; }
            public Func<ArkTerminalBocks, ArkTerminalAction, long, ArkTerminalInterfaceType> OnInteract { get; set; }

            public string GetText(ConcurrentDictionary<string, object> properties)
            {
                var t = Text;
                foreach (var item in properties)
                {
                    var k = "{" + item.Key + "}";
                    if (t.Contains(k))
                        t = t.Replace(k, item.Value?.ToString() ?? "");
                }
                return t;
            }

        }

        public enum ArkTerminalInterfaceType
        {
            None = 0,
            Home = 1,
            SessionExpided = 2,
            ServiceSelect = 3,
            NoVesselConnected = 4,
            SelectVessel = 5,
            SelectCargoTransferScope = 6,
            ConfirmCargoTransfer = 7,
            NoCargoToTrasnfer = 8,
            CargoTransferError = 9,
            CargoTransferCompleted = 10
        }

        private static ArkTerminalInterface HOME_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

Ark network synchronized.

Registered explorers may access
authorized Ark systems from this terminal.





Press LOGIN to establish a private 
D.A.W.N. session.",
            NeedLogin = false,
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Login },
            OnOpen = (terminal) =>
            {

            },
            OnInteract = (terminal, action, playerId) =>
            {
                terminal.LoggedPlayer = playerId;
                terminal.LastInteration = DateTime.Now;
                return ArkTerminalInterfaceType.ServiceSelect;
            }
        };

        private static ArkTerminalInterface SESSIONEXPIRED_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

Session expired.

No terminal input received within the security 
window.





Press LOGIN to establish a new private 
D.A.W.N. session.",
            NeedLogin = false,
            AutoInteractAfter = 5000,
            BackgroundColor = Color.DarkRed,
            FontColor = Color.White,
            ValidActions = new ArkTerminalAction[] { },
            OnOpen = (terminal) =>
            {

            },
            OnInteract = (terminal, action, playerId) =>
            {
                return ArkTerminalInterfaceType.Home;
            }
        };

        public enum ArkTerminalServiceType
        {
            None = 0,
            SubmitResources = 1
        }

        public class ArkTerminalService
        {
            public string Name { get; set; }
            public string Text { get; set; }
            public Func<ArkTerminalBocks, ArkTerminalInterfaceType> OnEnter { get; set; }
            public ArkTerminalService(string name, string text, Func<ArkTerminalBocks, ArkTerminalInterfaceType> onEnter)
            {
                Name = name;
                Text = text;
                OnEnter = onEnter;
            }
        }

        private static Dictionary<ArkTerminalServiceType, ArkTerminalService> VALID_SERVICES = new Dictionary<ArkTerminalServiceType, ArkTerminalService>()
        {
            { 
                ArkTerminalServiceType.SubmitResources, 
                new ArkTerminalService(
                    "delivery system", 
                    @"Submit recovered resources to The Second 
Dawn logistics network.

Delivered cargo will be processed by D.A.W.N. 
and added to the current Ark Jump objectives.

", 
                    (terminal) => {
                        var grids = GetConnectedGridsByPlayerId(terminal.LoggedPlayer);
                        if (!grids.Any())
                            return ArkTerminalInterfaceType.NoVesselConnected;
                        if (grids.Count > 1)
                            return ArkTerminalInterfaceType.SelectVessel;
                        return ArkTerminalInterfaceType.SelectCargoTransferScope;
                    }
                ) 
            }
        };

        private static void DoSetService(ArkTerminalBocks tInterface, int index)
        {
            tInterface.SetValue<int>("index", index);
            tInterface.SetValue<int>("position", index + 1);
            tInterface.SetValue<int>("count", VALID_SERVICES.Keys.Count);
            var service = VALID_SERVICES.Keys.ToList()[index];
            tInterface.SetValue<ArkTerminalServiceType>("service", service);
            tInterface.SetValue<string>("name", VALID_SERVICES[service].Name);
            tInterface.SetValue<string>("text", VALID_SERVICES[service].Text);
        }

        private static ArkTerminalInterface SERVICESELECT_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

{text}

Navigation: {position} / {count}
Press ENTER to access the {name}.",
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Prior, ArkTerminalAction.Next, ArkTerminalAction.Enter },
            OnOpen = (terminal) =>
            {
                DoSetService(terminal, 0);
            },
            OnInteract = (terminal, action, playerId) =>
            {
                var index = terminal.GetValue<int>("index");
                switch (action)
                {
                    case ArkTerminalAction.Prior:
                        if (index > 0)
                            index--;
                        else
                            index = VALID_SERVICES.Count - 1;
                        break;
                    case ArkTerminalAction.Next:
                        if (index < VALID_SERVICES.Count - 1)
                            index++;
                        else
                            index = 0;
                        break;
                    case ArkTerminalAction.Enter:
                        var service = terminal.GetValue<ArkTerminalServiceType>("service");
                        return VALID_SERVICES[service].OnEnter(terminal);
                }
                DoSetService(terminal, index);
                return ArkTerminalInterfaceType.None;
            }
        };

        private static ArkTerminalInterface NOVESSELCONNECTED_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

No registered vessel is currently connected to 
this relay.

Dock one of your own ships to an authorized Ark 
connector before starting cargo transfer.

1 - Return",
            BackgroundColor = Color.DarkRed,
            FontColor = Color.White,
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Num1 },
            OnOpen = (terminal) =>
            {

            },
            OnInteract = (terminal, action, playerId) =>
            {
                return ArkTerminalInterfaceType.ServiceSelect;
            }
        };

        private static void DoSetVessel(ArkTerminalBocks tInterface, int index)
        {
            tInterface.SetValue<int>("vessel_index", index);
            tInterface.SetValue<int>("vessel_position", index + 1);
            var grids = GetConnectedGridsByPlayerId(tInterface.LoggedPlayer);
            tInterface.SetValue<int>("vessel_count", grids.Count);
            if (grids.Any() && index >= 0 && index < grids.Count)
            {
                tInterface.SetValue<string>("vessel_name", grids[index].DisplayName);
                tInterface.SetValue<long>("vessel_id", grids[index].EntityId);
            }
            else
            {
                tInterface.SetValue<string>("vessel_name", "ERROR");
            }
        }

        private static ArkTerminalInterface VESSELCONNECTED_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

Multiple registered vessels detected.

Select the vessel that will submit cargo to the 
Ark manifest:

{vessel_name}

1 - Cancel

Navigation: {vessel_position} / {vessel_count}
Press ENTER to confirm selection.",
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Prior, ArkTerminalAction.Next, ArkTerminalAction.Enter, ArkTerminalAction.Num1 },
            OnOpen = (terminal) =>
            {
                DoSetVessel(terminal, 0);
            },
            OnInteract = (terminal, action, playerId) =>
            {
                var index = terminal.GetValue<int>("vessel_index");
                var vessel_name = terminal.GetValue<string>("vessel_name");
                if (vessel_name == "ERROR")
                    return ArkTerminalInterfaceType.CargoTransferError;
                var grids = GetConnectedGridsByPlayerId(terminal.LoggedPlayer);
                switch (action)
                {
                    case ArkTerminalAction.Prior:
                        if (index > 0)
                            index--;
                        else
                            index = grids.Count - 1;
                        break;
                    case ArkTerminalAction.Next:
                        if (index < grids.Count - 1)
                            index++;
                        else
                            index = 0;
                        break;
                    case ArkTerminalAction.Enter:
                        return ArkTerminalInterfaceType.SelectCargoTransferScope;
                    case ArkTerminalAction.Num1:
                        return ArkTerminalInterfaceType.ServiceSelect;
                }
                DoSetVessel(terminal, index);
                return ArkTerminalInterfaceType.None;
            }
        };

        private static bool DoCalcTrasnferScope(ArkTerminalBocks tInterface, int scope)
        {
            var index = tInterface.GetValue<int>("vessel_index");
            var inventories = DoGetInventoriesToInteract(tInterface, index, scope);
            var validInventories = DoFilterInventoriesToInteract(inventories);
            if (validInventories.Any())
            {
                var itemCount = (long)validInventories.Values.Sum(x => x.Values.Sum());
                var itemMass = validInventories.Values.Sum(x => x.Sum(y => GetItemMass(y.Key) * y.Value));
                tInterface.SetValue<float>("item_mass", itemMass);
                tInterface.SetValue<long>("item_count", itemCount);
                return true;
            }
            return false;
        }

        private static ConcurrentDictionary<MyDefinitionId, float> _massCache = new ConcurrentDictionary<MyDefinitionId, float>();
        private static float GetItemMass(MyDefinitionId item)
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

        private static Dictionary<MyInventory, Dictionary<MyDefinitionId, float>> DoFilterInventoriesToInteract(List<MyInventory> inventories)
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

        private static List<MyInventory> DoGetInventoriesToInteract(ArkTerminalBocks tInterface, int index, int scope)
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
            OnOpen = (terminal) =>
            {

            },
            OnInteract = (terminal, action, playerId) =>
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
                        return ArkTerminalInterfaceType.ServiceSelect;
                }
                if (scope >= 0)
                {
                    terminal.SetValue<int>("transfer_scope", scope);
                    if (DoCalcTrasnferScope(terminal, scope))
                        return ArkTerminalInterfaceType.ConfirmCargoTransfer;
                    else
                        return ArkTerminalInterfaceType.NoCargoToTrasnfer;
                }
                return ArkTerminalInterfaceType.None;
            }
        };

        private static bool DoExecuteCargoTrasnfer(ArkTerminalBocks tInterface, int index, long entityId, int scope)
        {
            var inventories = DoGetInventoriesToInteract(tInterface, index, scope);
            var validInventories = DoFilterInventoriesToInteract(inventories);
            if (validInventories.Any())
            {
                var finalAmount = 0f;
                var finalMass = 0f;
                foreach (var inventory in validInventories.Keys)
                {
                    var items = validInventories[inventory];
                    foreach (var itemId in items.Keys)
                    {
                        var amount = (MyFixedPoint)items[itemId];
                        var removedAmount = (float) inventory.RemoveItemsOfType(amount, itemId);
                        if (removedAmount > 0)
                        {
                            var removedMass = removedAmount * GetItemMass(itemId);
                            finalAmount += removedAmount;
                            finalMass += removedMass;
                            var categoryId = SEDBStorage.Instance.SeasonMetaConfig.GetItemCategoryById(itemId);
                            var categoryInfo = SEDBStorage.Instance.SeasonMetaConfig.GetCategoryById(categoryId);
                            var itemInfo = categoryInfo.GetItemById(itemId);
                            SEDBStorage.Instance.SeasonMetaResult.GetActiveResult().AddValueToEntry(
                                categoryId, 
                                (long)removedAmount,
                                itemInfo.Weight
                            );
                        }
                    }
                }
                if (finalAmount > 0)
                {
                    var donation = new SeasonMetaDonationEntry()
                    {
                        SteamId = tInterface.LoggedSteamId,
                        ItemCount = (long)finalAmount,
                        MassAmount = finalMass,
                        OperationDate = DateTime.Now
                    };
                    SEDBStorage.Instance.SeasonMetaResult.GetActiveResult().Donations.Add(donation);
                    SEDiscordBridgePlugin.Static.AlertDonationIsCompleted(donation.SteamId, donation.ItemCount, donation.MassAmount);
                    return true;
                }
            }
            return false;
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
            OnOpen = (terminal) =>
            {

            },
            OnInteract = (terminal, action, playerId) =>
            {
                switch (action)
                {
                    case ArkTerminalAction.Num1:
                        var scope = terminal.GetValue<int>("transfer_scope");
                        var index = terminal.GetValue<int>("vessel_index");
                        var vesselId = terminal.GetValue<long>("vessel_id");
                        if (DoExecuteCargoTrasnfer(terminal, index, vesselId, scope))
                            return ArkTerminalInterfaceType.CargoTransferCompleted;
                        else
                            return ArkTerminalInterfaceType.CargoTransferError;
                    case ArkTerminalAction.Num2:
                                return ArkTerminalInterfaceType.ServiceSelect;
                            }
                return ArkTerminalInterfaceType.None;
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
            OnOpen = (terminal) =>
            {

            },
            OnInteract = (terminal, action, playerId) =>
            {
                return ArkTerminalInterfaceType.ServiceSelect;
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
            OnOpen = (terminal) =>
            {

            },
            OnInteract = (terminal, action, playerId) =>
            {
                return ArkTerminalInterfaceType.ServiceSelect;
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
            OnOpen = (terminal) =>
            {

            },
            OnInteract = (terminal, action, playerId) =>
            {
                return ArkTerminalInterfaceType.ServiceSelect;
            }
        };

        private static Dictionary<ArkTerminalInterfaceType, ArkTerminalInterface> TERMINAL_INTERFACES = new Dictionary<ArkTerminalInterfaceType, ArkTerminalInterface>()
        {
            { ArkTerminalInterfaceType.Home, HOME_INTERFACE },
            { ArkTerminalInterfaceType.SessionExpided, SESSIONEXPIRED_INTERFACE },
            { ArkTerminalInterfaceType.ServiceSelect, SERVICESELECT_INTERFACE },
            { ArkTerminalInterfaceType.NoVesselConnected, NOVESSELCONNECTED_INTERFACE },
            { ArkTerminalInterfaceType.SelectVessel, VESSELCONNECTED_INTERFACE },
            { ArkTerminalInterfaceType.SelectCargoTransferScope, SELECTCARGOTRASNFERSCOPE_INTERFACE },
            { ArkTerminalInterfaceType.ConfirmCargoTransfer, CONFIRMCARGOTRASNFER_INTERFACE },
            { ArkTerminalInterfaceType.NoCargoToTrasnfer, NOCARGOTOTRASNFER_INTERFACE },
            { ArkTerminalInterfaceType.CargoTransferError, CARGOTRASNFERERROR_INTERFACE },
            { ArkTerminalInterfaceType.CargoTransferCompleted, CARGOTRASNFERCOMPLETED_INTERFACE }
        };

        private const ArkTerminalInterfaceType START_INTERFACE = ArkTerminalInterfaceType.Home;
        private const ArkTerminalInterfaceType EXPIRED_INTERFACE = ArkTerminalInterfaceType.SessionExpided;

        public class ArkTerminalBocks
        {

            public string Name { get; set; }
            public MyButtonPanel Computer { get; set; }
            public MyMultiTextPanelComponent ComputerPanelComponent { get; set; }
            public MyTextPanelComponent ComputerComponent { get; set; }
            public MyButtonPanel NavigationButtons { get; set; }

            public bool Enabled { get; set; }
            public bool Logged
            {
                get
                {
                    return LoggedPlayer != 0 && (DateTime.Now - LastInteration).TotalSeconds < 120;
                }
            }
            public long LoggedPlayer { get; set; }
            public ulong LoggedSteamId
            {
                get
                {
                    return MySession.Static.Players.TryGetSteamId(LoggedPlayer);
                }
            }
            public DateTime LastInteration { get; set; }
            public ArkTerminalInterfaceType CurrentInterface { get; set; }

            public ConcurrentDictionary<string, object> Properties { get; set; } = new ConcurrentDictionary<string, object>();
            public T GetValue<T>(string key)
            {
                try
                {
                    if (Properties.ContainsKey(key))
                        return (T)Convert.ChangeType(Properties[key], typeof(T));
                }
                catch (Exception e)
                {
                    Logging.Instance.LogError(GetType(), e);
                }
                return default(T);
            }

            public void SetValue<T>(string key, T value)
            {
                Properties[key] = value;
            }

            public bool IsValid()
            {
                return Computer != null && NavigationButtons != null;
            }

            public void StartTerminal(long playerId)
            {
                if (IsValid())
                {
                    ComputerPanelComponent = Computer.Components.Get<MyMultiTextPanelComponent>();
                    ComputerComponent = ComputerPanelComponent.Panels.FirstOrDefault();
                    Enabled = true;
                    ResetTerminal(playerId);
                }
                else
                {
                    Logging.Instance.LogWarning(GetType(), $"Terminal {Name} is not valid!");
                }
            }

            public void ResetTerminal(long playerId)
            {
                LoadInterface(START_INTERFACE, playerId);
            }

            public void LoadInterface(ArkTerminalInterfaceType tInterface, long playerId)
            {
                Logging.Instance.LogInfo(GetType(), $"LoadInterface called : tInterface={tInterface} playerId={playerId}");
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    var doOpen = CurrentInterface != tInterface;
                    CurrentInterface = tInterface;
                    var tObj = TERMINAL_INTERFACES[CurrentInterface];
                    if (doOpen)
                    {
                        ComputerComponent.BackgroundColor = tObj.BackgroundColor;
                        ComputerComponent.FontColor = tObj.FontColor;
                        tObj.OnOpen(this);
                        if (tObj.AutoInteractAfter > 0)
                        {
                            var self = this;
                            MyAPIGateway.Parallel.Start(() => {
                                Thread.Sleep((int)tObj.AutoInteractAfter);
                                var target = tObj.OnInteract(self, ArkTerminalAction.None, playerId);
                                if (target != ArkTerminalInterfaceType.None)
                                {
                                    LoadInterface(target, playerId);
                                }
                            });
                        }
                    }
                    DoRefreshComputerText(tObj);
                });
            }

            private void DoRefreshComputerText(ArkTerminalInterface tObj)
            {
                var text = tObj.GetText(Properties);
                ComputerPanelComponent.ChangeText(0, text);
                GameWatcherController.SendLcdTextChange(0, Computer.EntityId, text);
            }

            private void DoInteractCurrentInterface(ArkTerminalAction action, long playerId)
            {
                Logging.Instance.LogInfo(GetType(), $"DoInteractCurrentInterface called : action={action} playerId={playerId}");
                var tObj = TERMINAL_INTERFACES[CurrentInterface];
                if (tObj.ValidActions.Contains(action))
                {
                    if (tObj.NeedLogin)
                    {
                        Logging.Instance.LogInfo(GetType(), $"TERMINAL_INTERFACES : Needs Login and Logged={Logged} | LoggedPlayer={LoggedPlayer}");
                        if (!Logged || LoggedPlayer != playerId)
                        {
                            return;
                        }
                    }
                    var target = tObj.OnInteract(this, action, playerId);
                    LastInteration = DateTime.Now;
                    if (target != ArkTerminalInterfaceType.None)
                    {
                        LoadInterface(target, playerId);
                    }
                    else
                    {
                        DoRefreshComputerText(tObj);
                    }
                }
                else
                {
                    Logging.Instance.LogInfo(GetType(), $"TERMINAL_INTERFACES : ValidActions did not contains action={action}");
                }
            }

            public void ButtonClick(MyButtonPanel button, long playerId, int index)
            {
                Logging.Instance.LogInfo(GetType(), $"ButtonClick called and terminal is Enabled={Enabled}");
                if (Enabled)
                {
                    var action = ArkTerminalAction.None;
                    var targetId = button.EntityId;
                    if (Computer.EntityId == targetId)
                    {
                        action = index < COMPUTER_BTN_MAP.Count ? COMPUTER_BTN_MAP[index] : ArkTerminalAction.None;
                    }
                    else if (NavigationButtons.EntityId == targetId)
                    {
                        action = index < NAVIGATION_BTN_MAP.Count ? NAVIGATION_BTN_MAP[index] : ArkTerminalAction.None;
                    }
                    DoInteractCurrentInterface(action, playerId);
                }
            }

        }

        private static MyCubeGrid ARKGRID;
        private static IMyGridTerminalSystem ARKGRIDTERMINALSYSTEM;

        private static MyStoreBlock ARKGRIDSTOREBLOCK;
        private static MyContractBlock ARKGRIDCONTRACTBLOCK;

        private static ConcurrentDictionary<int, ArkTerminalBocks> ARKGRIDTERMINALS = new ConcurrentDictionary<int, ArkTerminalBocks>();

        private static ConcurrentDictionary<long, int> BUTTONCLICKMAPTERMINAL = new ConcurrentDictionary<long, int>();

        private static ConcurrentDictionary<long, MyShipConnector> ARKGRIDCONNECTORS = new ConcurrentDictionary<long, MyShipConnector>();

        private static bool canRun;
        private static ParallelTasks.Task task;

        private static bool _initialized = false;
        public static void Init()
        {
            if (SEDBStorage.Instance.FunctionalGrids.EntityId == 0)
            {
                Logging.Instance.LogWarning(typeof(ArkLogisticRelayController), "ArkGrid EntityId not defined!");
                return;
            }

            if (_initialized)
            {
                Dispose();
                _initialized = false;
            }

            ARKGRID = MyEntities.GetEntityById(SEDBStorage.Instance.FunctionalGrids.EntityId) as MyCubeGrid;
            if (ARKGRID == null)
            {
                Logging.Instance.LogWarning(typeof(ArkLogisticRelayController), $"ArkGrid EntityId={SEDBStorage.Instance.FunctionalGrids.EntityId} not valid!");
                return;
            }

            ARKGRIDTERMINALSYSTEM = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ARKGRID);

            ARKGRIDSTOREBLOCK = ARKGRID.GetFatBlocks<MyStoreBlock>().FirstOrDefault();
            ARKGRIDCONTRACTBLOCK = ARKGRID.GetFatBlocks<MyContractBlock>().FirstOrDefault();

            foreach (var item in ARKGRID.GetFatBlocks<MyShipConnector>())
            {
                ARKGRIDCONNECTORS[item.EntityId] = item;
            }

            var groups = new List<IMyBlockGroup>();
            ARKGRIDTERMINALSYSTEM.GetBlockGroups(groups);

            int c = 0;
            foreach (IMyBlockGroup group in groups.Where(x=> x.Name.ToString().StartsWith("ARK-PC-")))
            {
                var blocks = new List<IMyTerminalBlock>();
                group.GetBlocks(blocks);
                var terminal = new ArkTerminalBocks() { Name = group.Name };
                foreach (var block in blocks)
                {
                    if (block is MyButtonPanel btPanel)
                    {
                        if (block.CustomName.StartsWith("Ark - Buttons"))
                        {
                            terminal.NavigationButtons = btPanel;
                            BUTTONCLICKMAPTERMINAL[btPanel.EntityId] = c;
                        }
                        else if (block.CustomName.StartsWith("Ark - Computer"))
                        {
                            terminal.Computer = btPanel;
                            BUTTONCLICKMAPTERMINAL[btPanel.EntityId] = c;
                        }
                    }
                }
                ARKGRIDTERMINALS[c] = terminal;
                terminal.StartTerminal(0);
                c++;
            }

            ButtonPanelPatch.OnActivateButton += ButtonPanelPatch_OnActivateButton;

            _initialized = true;

            canRun = true;
            task = MyAPIGateway.Parallel.StartBackground(() =>
            {
                Logging.Instance.LogInfo(typeof(ArkLogisticRelayController), "StartBackground [CheckTerminals START]");
                // Loop CheckTerminals
                while (canRun)
                {
                    CheckTerminals();
                    if (MyAPIGateway.Parallel != null)
                        MyAPIGateway.Parallel.Sleep(1000);
                    else
                        break;
                }
            });

        }

        private static List<MyCubeGrid> GetConnectedGridsByPlayerId(long playerId)
        {
            var connectedGrids = ARKGRIDCONNECTORS.Where(x => x.Value.Connected).Select(x => x.Value.Other.CubeGrid);
            if (connectedGrids.Any())
            {
                return connectedGrids.Where(x=>x.BigOwners.Contains(playerId)).ToList();
            }
            return new List<MyCubeGrid>();
        }

        private static void CheckTerminals()
        {
            foreach (var terminal in ARKGRIDTERMINALS)
            {
                if (terminal.Value.Enabled &&
                    !terminal.Value.Logged &&
                    terminal.Value.CurrentInterface != START_INTERFACE &&
                    terminal.Value.CurrentInterface != EXPIRED_INTERFACE)
                {
                    terminal.Value.LoadInterface(EXPIRED_INTERFACE, 0);
                }
            }
        }

        private static void ButtonPanelPatch_OnActivateButton(MyButtonPanel block, long playerId, int index)
        {
            Logging.Instance.LogInfo(typeof(ArkLogisticRelayController), $"ButtonPanelPatch_OnActivateButton called for block={block.EntityId} player={playerId} index={index}");
            Logging.Instance.LogInfo(typeof(ArkLogisticRelayController), $"ButtonPanelPatch_OnActivateButton valid blocks ids={string.Join(",", BUTTONCLICKMAPTERMINAL.Keys)}");
            if (BUTTONCLICKMAPTERMINAL.ContainsKey(block.EntityId))
            {
                var target = BUTTONCLICKMAPTERMINAL[block.EntityId];
                ARKGRIDTERMINALS[target].ButtonClick(block, playerId, index);
            }
        }

        public static void Dispose()
        {
            ButtonPanelPatch.OnActivateButton -= ButtonPanelPatch_OnActivateButton;

            canRun = false;
            _initialized = false;
        }

    }
}
