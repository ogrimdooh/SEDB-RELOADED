using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SEDiscordBridge.Controllers.Economics;
using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Patches;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using VRage.Game;
using VRageMath;
using static SEDiscordBridge.Controllers.Grids.ArkLogisticRelayController;

namespace SEDiscordBridge.Controllers.Grids
{
    public abstract class BaseFunctionalGridController
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

        protected static Dictionary<int, ArkTerminalAction> COMPUTER_BTN_MAP = new Dictionary<int, ArkTerminalAction>()
        {
            { 0, ArkTerminalAction.Num1 },
            { 1, ArkTerminalAction.Num2 },
            { 2, ArkTerminalAction.Num3 }
        };

        protected static Dictionary<int, ArkTerminalAction> NAVIGATION_BTN_MAP = new Dictionary<int, ArkTerminalAction>()
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
            public Action<BaseFunctionalGridController, IArkTerminalBocks> OnOpen { get; set; }
            public Func<BaseFunctionalGridController, IArkTerminalBocks, ArkTerminalAction, long, InterfaceType> OnInteract { get; set; }

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

        public class ArkTerminalService
        {
            public string Name { get; set; }
            public string Text { get; set; }
            public Func<BaseFunctionalGridController, IArkTerminalBocks, InterfaceType> OnEnter { get; set; }
            public ArkTerminalService(string name, string text, Func<BaseFunctionalGridController, IArkTerminalBocks, InterfaceType> onEnter)
            {
                Name = name;
                Text = text;
                OnEnter = onEnter;
            }
        }

        public struct InterfaceType : IEquatable<string>
        {

            public string Value { get; private set; }

            public InterfaceType(string value)
            {
                Value = value;
            }

            public readonly bool Equals(string other)
            {
                return Value?.Equals(other) ?? other == null;
            }

            public static implicit operator InterfaceType(string v)
            {
                return new InterfaceType(v);
            }

            public static implicit operator string(InterfaceType v)
            {
                return v.Value;
            }

        }

        public readonly static InterfaceType INTERFACE_TYPE_NONE = "NONE";
        public readonly static InterfaceType INTERFACE_TYPE_HOME = "HOME";
        public readonly static InterfaceType INTERFACE_TYPE_SESSIONEXPIDED = "SESSIONEXPIDED";
        public readonly static InterfaceType INTERFACE_TYPE_SERVICESELECT = "SERVICESELECT";
        public readonly static InterfaceType INTERFACE_TYPE_NOVESSELCONNECTED = "NOVESSELCONNECTED";
        public readonly static InterfaceType INTERFACE_TYPE_SELECTVESSEL = "SELECTVESSEL";

        public struct ServiceType : IEquatable<string>
        {

            public string Value { get; private set; }

            public ServiceType(string value)
            {
                Value = value;
            }

            public readonly bool Equals(string other)
            {
                return Value?.Equals(other) ?? other == null;
            }

            public static implicit operator ServiceType(string v)
            {
                return new ServiceType(v);
            }

            public static implicit operator string(ServiceType v)
            {
                return v.Value;
            }

        }

        public readonly static ServiceType TERMINAL_SERVICE_TYPE_NONE = "NONE";

        protected ConcurrentDictionary<ServiceType, ArkTerminalService> VALID_SERVICES = new ConcurrentDictionary<ServiceType, ArkTerminalService>();

        protected void AddService(ServiceType type, ArkTerminalService service)
        {
            VALID_SERVICES[type] = service;
        }

        protected abstract void LoadServices();

        protected static readonly InterfaceType START_INTERFACE = INTERFACE_TYPE_HOME;
        protected static readonly InterfaceType EXPIRED_INTERFACE = INTERFACE_TYPE_SESSIONEXPIDED;

        private readonly ConcurrentDictionary<InterfaceType, ArkTerminalInterface> TERMINAL_INTERFACES = new ConcurrentDictionary<InterfaceType, ArkTerminalInterface>();

        protected void AddInterface(InterfaceType type, ArkTerminalInterface interFace)
        {
            TERMINAL_INTERFACES[type] = interFace;
        }

        protected abstract void OnLoadInterfaces();

        protected void LoadInterfaces()
        {
            AddInterface(INTERFACE_TYPE_HOME, HOME_INTERFACE);
            AddInterface(INTERFACE_TYPE_SESSIONEXPIDED, SESSIONEXPIRED_INTERFACE);
            AddInterface(INTERFACE_TYPE_SERVICESELECT, SERVICESELECT_INTERFACE);
            AddInterface(INTERFACE_TYPE_NOVESSELCONNECTED, NOVESSELCONNECTED_INTERFACE);
            AddInterface(INTERFACE_TYPE_SELECTVESSEL, VESSELCONNECTED_INTERFACE);
            OnLoadInterfaces();
        }

        protected static void DoSetService<C>(C controller, IArkTerminalBocks tInterface, int index) where C : BaseFunctionalGridController
        {
            tInterface.SetValue("index", index);
            tInterface.SetValue("position", index + 1);
            tInterface.SetValue("count", controller.VALID_SERVICES.Keys.Count);
            if (index >= 0)
            {
                var service = controller.VALID_SERVICES.Keys.ToList()[index];
                tInterface.SetValue("service", service);
                tInterface.SetValue("name", controller.VALID_SERVICES[service].Name);
                tInterface.SetValue("text", controller.VALID_SERVICES[service].Text);
            }
            else
            {
                tInterface.SetValue("name", "ERROR");
                tInterface.SetValue("text", "No service available!");
            }
        }

        protected static void DoSetVessel<C>(C controller, IArkTerminalBocks tInterface, int index) where C : BaseFunctionalGridController
        {
            tInterface.SetValue("vessel_index", index);
            tInterface.SetValue("vessel_position", index + 1);
            var grids = controller.GetConnectedGridsByPlayerId(tInterface.LoggedPlayer);
            tInterface.SetValue("vessel_count", grids.Count);
            if (grids.Any() && index >= 0 && index < grids.Count)
            {
                tInterface.SetValue("vessel_name", grids[index].DisplayName);
                tInterface.SetValue("vessel_id", grids[index].EntityId);
            }
            else
            {
                tInterface.SetValue("vessel_name", "ERROR");
            }
        }

        private static ConcurrentDictionary<MyDefinitionId, float> _massCache = new ConcurrentDictionary<MyDefinitionId, float>();
        protected static float GetItemMass(MyDefinitionId item)
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

        protected static ArkTerminalInterface HOME_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

Ark network synchronized.

Registered explorers may access
authorized Ark systems from this terminal.





Press LOGIN to establish a private 
D.A.W.N. session.",
            NeedLogin = false,
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Login },
            OnOpen = (controller, terminal) =>
            {

            },
            OnInteract = (controller, terminal, action, playerId) =>
            {
                terminal.LoggedPlayer = playerId;
                terminal.LastInteration = DateTime.Now;
                return INTERFACE_TYPE_SERVICESELECT;
            }
        };

        protected static ArkTerminalInterface SESSIONEXPIRED_INTERFACE = new ArkTerminalInterface()
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
            OnOpen = (controller, terminal) =>
            {

            },
            OnInteract = (controller, terminal, action, playerId) =>
            {
                return INTERFACE_TYPE_HOME;
            }
        };

        private static ArkTerminalInterface SERVICESELECT_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

{text}

Navigation: {position} / {count}
Press ENTER to access the {name}.",
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Prior, ArkTerminalAction.Next, ArkTerminalAction.Enter },
            OnOpen = (controller, terminal) =>
            {
                DoSetService(controller, terminal, controller.VALID_SERVICES.Count > 0 ? 0 : -1);
            },
            OnInteract = (controller, terminal, action, playerId) =>
            {
                var index = terminal.GetValue<int>("index");
                if (index >= 0)
                {
                    switch (action)
                    {
                        case ArkTerminalAction.Prior:
                            if (index > 0)
                                index--;
                            else
                                index = controller.VALID_SERVICES.Count - 1;
                            break;
                        case ArkTerminalAction.Next:
                            if (index < controller.VALID_SERVICES.Count - 1)
                                index++;
                            else
                                index = 0;
                            break;
                        case ArkTerminalAction.Enter:
                            var service = terminal.GetValue<ServiceType>("service");
                            return controller.VALID_SERVICES[service].OnEnter(controller, terminal);
                    }
                    DoSetService(controller, terminal, index);
                }
                return INTERFACE_TYPE_NONE;
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
            OnOpen = (controller, terminal) =>
            {

            },
            OnInteract = (controller, terminal, action, playerId) =>
            {
                return INTERFACE_TYPE_SERVICESELECT;
            }
        };

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
            OnOpen = (controller, terminal) =>
            {
                DoSetVessel(controller, terminal, 0);
            },
            OnInteract = (controller, terminal, action, playerId) =>
            {
                var index = terminal.GetValue<int>("vessel_index");
                var vessel_name = terminal.GetValue<string>("vessel_name");
                if (vessel_name == "ERROR")
                    return INTERFACE_TYPE_CARGOTRANSFERERROR;
                var grids = controller.GetConnectedGridsByPlayerId(terminal.LoggedPlayer);
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
                        return INTERFACE_TYPE_SELECTCARGOTRANSFERSCOPE;
                    case ArkTerminalAction.Num1:
                        return INTERFACE_TYPE_SERVICESELECT;
                }
                DoSetVessel(controller, terminal, index);
                return INTERFACE_TYPE_NONE;
            }
        };

        public interface IArkTerminalBocks
        {

            string Name { get; set; }
            MyButtonPanel Computer { get; set; }
            MyMultiTextPanelComponent ComputerPanelComponent { get; set; }
            MyTextPanelComponent ComputerComponent { get; set; }
            MyButtonPanel NavigationButtons { get; set; }
            bool Enabled { get; set; }
            bool Logged { get; }
            long LoggedPlayer { get; set; }
            ulong LoggedSteamId { get; }
            DateTime LastInteration { get; set; }
            InterfaceType CurrentInterface { get; set; }
            T GetValue<T>(string key);
            void SetValue<T>(string key, T value);
            void StartTerminal(long playerId);
            void ResetTerminal(long playerId);
            void LoadInterface(InterfaceType tInterface, long playerId);
            void ButtonClick(MyButtonPanel button, long playerId, int index);

        }

        public class ArkTerminalBocks<C> : IArkTerminalBocks where C : BaseFunctionalGridController
        {

            public string Name { get; set; }
            public MyButtonPanel Computer { get; set; }
            public MyMultiTextPanelComponent ComputerPanelComponent { get; set; }
            public MyTextPanelComponent ComputerComponent { get; set; }
            public MyButtonPanel NavigationButtons { get; set; }
            public C Controller { get; set; }

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
            public InterfaceType CurrentInterface { get; set; }

            public ArkTerminalBocks(C controller)
            {
                Controller = controller;
            }

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
                return default;
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

            public void LoadInterface(InterfaceType tInterface, long playerId)
            {
                Logging.Instance.LogInfo(GetType(), $"LoadInterface called : tInterface={tInterface} playerId={playerId}");
                MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                {
                    var doOpen = CurrentInterface != tInterface;
                    CurrentInterface = tInterface;
                    var tObj = Controller.TERMINAL_INTERFACES[CurrentInterface];
                    if (doOpen)
                    {
                        ComputerComponent.BackgroundColor = tObj.BackgroundColor;
                        ComputerComponent.FontColor = tObj.FontColor;
                        tObj.OnOpen(Controller, this);
                        if (tObj.AutoInteractAfter > 0)
                        {
                            var self = this;
                            MyAPIGateway.Parallel.Start(() => {
                                Thread.Sleep((int)tObj.AutoInteractAfter);
                                var target = tObj.OnInteract(Controller, self, ArkTerminalAction.None, playerId);
                                if (target != INTERFACE_TYPE_NONE)
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
                var tObj = Controller.TERMINAL_INTERFACES[CurrentInterface];
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
                    var target = tObj.OnInteract(Controller, this, action, playerId);
                    LastInteration = DateTime.Now;
                    if (target != INTERFACE_TYPE_NONE)
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

        protected MyCubeGrid ARKGRID;
        protected IMyGridTerminalSystem ARKGRIDTERMINALSYSTEM;

        protected MyStoreBlock ARKGRIDSTOREBLOCK;
        protected MyCargoContainer ARKGRIDSTORECARGOBLOCK;
        protected MyContractBlock ARKGRIDCONTRACTBLOCK;

        protected ConcurrentDictionary<int, IArkTerminalBocks> ARKGRIDTERMINALS = new ConcurrentDictionary<int, IArkTerminalBocks>();

        protected ConcurrentDictionary<long, int> BUTTONCLICKMAPTERMINAL = new ConcurrentDictionary<long, int>();

        protected ConcurrentDictionary<long, MyShipConnector> ARKGRIDCONNECTORS = new ConcurrentDictionary<long, MyShipConnector>();

        protected bool canRun;
        protected ParallelTasks.Task task;

        protected abstract long GetTargetGridId();
        protected abstract StationType GetStationType();
        protected abstract StationLevel GetStationLevel();
        protected abstract FactionType GetFactionType();

        protected abstract IArkTerminalBocks CreateNewTerminalBlock(string name);

        protected abstract void OnAfterInit();

        protected bool _initialized = false;
        protected void DoInit()
        {
            if (GetTargetGridId() == 0)
            {
                Logging.Instance.LogWarning(GetType(), "ArkGrid EntityId not defined!");
                return;
            }

            if (_initialized)
            {
                DoDispose();
                _initialized = false;
            }

            LoadInterfaces();
            LoadServices();

            ARKGRID = MyEntities.GetEntityById(GetTargetGridId()) as MyCubeGrid;
            if (ARKGRID == null)
            {
                Logging.Instance.LogWarning(GetType(), $"ArkGrid EntityId={GetTargetGridId()} not valid!");
                return;
            }

            if (FactionsController.ChangeGridOwnerToMainFaction(ARKGRID))
            {
                Logging.Instance.LogInfo(GetType(), $"ArkGrid EntityId={GetTargetGridId()} ownership changed to faction {FactionsController.FACTION_2DAWN.Tag}");
            }

            ARKGRIDTERMINALSYSTEM = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ARKGRID);

            var storeGroup = ARKGRIDTERMINALSYSTEM.GetBlockGroupWithName("ARK-STORE");
            if (storeGroup != null)
            {
                var storeBlocks = new List<IMyTerminalBlock>();
                storeGroup.GetBlocksOfType<MyStoreBlock>(storeBlocks);
                var cargoBlocks = new List<IMyTerminalBlock>();
                storeGroup.GetBlocksOfType<MyCargoContainer>(cargoBlocks);
                if (storeBlocks.Any() && cargoBlocks.Any())
                {
                    ARKGRIDSTOREBLOCK = storeBlocks.First() as MyStoreBlock;
                    Logging.Instance.LogInfo(GetType(), $"ArkGrid StoreBlock found and loaded: {ARKGRIDSTOREBLOCK.EntityId}");
                    ARKGRIDSTORECARGOBLOCK = cargoBlocks.First() as MyCargoContainer;
                    Logging.Instance.LogInfo(GetType(), $"ArkGrid Store CargoBlock found and loaded: {ARKGRIDSTORECARGOBLOCK.EntityId}");
                    EconomicsConstants.LoadStoreBlock(ARKGRIDSTOREBLOCK, ARKGRIDSTORECARGOBLOCK);
                }
            }

            ARKGRIDCONTRACTBLOCK = ARKGRID.GetFatBlocks<MyContractBlock>().FirstOrDefault();
            if (ARKGRIDCONTRACTBLOCK != null)
            {
                EconomicsConstants.LoadContractBlock(ARKGRIDCONTRACTBLOCK, GetStationType(), GetStationLevel(), GetFactionType());
            }

            foreach (var item in ARKGRID.GetFatBlocks<MyShipConnector>())
            {
                ARKGRIDCONNECTORS[item.EntityId] = item;
            }

            var groups = new List<IMyBlockGroup>();
            ARKGRIDTERMINALSYSTEM.GetBlockGroups(groups);

            int c = 0;
            foreach (IMyBlockGroup group in groups.Where(x => x.Name.ToString().StartsWith("ARK-PC-")))
            {
                var blocks = new List<IMyTerminalBlock>();
                group.GetBlocks(blocks);
                var terminal = CreateNewTerminalBlock(group.Name);
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

            OnAfterInit();

            _initialized = true;

            canRun = true;
            task = MyAPIGateway.Parallel.StartBackground(() =>
            {
                Logging.Instance.LogInfo(GetType(), "StartBackground [CheckTerminals START]");
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

        public List<MyCubeGrid> GetConnectedGridsByPlayerId(long playerId)
        {
            var connectedGrids = ARKGRIDCONNECTORS.Where(x => x.Value.Connected).Select(x => x.Value.Other.CubeGrid);
            if (connectedGrids.Any())
            {
                return connectedGrids.Where(x => x.BigOwners.Contains(playerId)).ToList();
            }
            return new List<MyCubeGrid>();
        }

        protected void CheckTerminals()
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

        protected void ButtonPanelPatch_OnActivateButton(MyButtonPanel block, long playerId, int index)
        {
            Logging.Instance.LogInfo(GetType(), $"ButtonPanelPatch_OnActivateButton called for block={block.EntityId} player={playerId} index={index}");
            Logging.Instance.LogInfo(GetType(), $"ButtonPanelPatch_OnActivateButton valid blocks ids={string.Join(",", BUTTONCLICKMAPTERMINAL.Keys)}");
            if (BUTTONCLICKMAPTERMINAL.ContainsKey(block.EntityId))
            {
                var target = BUTTONCLICKMAPTERMINAL[block.EntityId];
                ARKGRIDTERMINALS[target].ButtonClick(block, playerId, index);
            }
        }

        public void DoDispose()
        {
            ButtonPanelPatch.OnActivateButton -= ButtonPanelPatch_OnActivateButton;

            canRun = false;
            _initialized = false;
        }

    }
}
