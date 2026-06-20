using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using SEDiscordBridge.Patches;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Documents;
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
                        t = t.Replace(k, item.Value.ToString());
                }
                return t;
            }

        }

        public enum ArkTerminalInterfaceType
        {
            None = 0,
            Home = 1,
            SessionExpided = 2,
            ServiceSelect = 3
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
            public ArkTerminalService(string name, string text)
            {
                Name = name;
                Text = text;
            }
        }

        private static Dictionary<ArkTerminalServiceType, ArkTerminalService> VALID_SERVICES = new Dictionary<ArkTerminalServiceType, ArkTerminalService>()
        {
            { 
                ArkTerminalServiceType.SubmitResources, 
                new ArkTerminalService("delivery system", @"Submit recovered resources to The Second Dawn 
logistics network.

Delivered cargo will be processed by D.A.W.N. 
and added to the current Ark Jump objectives.") 
            }
        };

        private static void DoSetService(ArkTerminalBocks tInterface, ArkTerminalServiceType service)
        {
            tInterface.SetValue<ArkTerminalServiceType>("service", service);
            tInterface.SetValue<string>("name", VALID_SERVICES[service].Name);
            tInterface.SetValue<string>("text", VALID_SERVICES[service].Text);
        }

        private static ArkTerminalInterface SERVICESELECT_INTERFACE = new ArkTerminalInterface()
        {
            Text = @"ARK RESOURCE DELIVERY

{text}

Press ENTER to access the {name}.",
            ValidActions = new ArkTerminalAction[] { ArkTerminalAction.Prior, ArkTerminalAction.Next, ArkTerminalAction.Enter },
            OnOpen = (terminal) =>
            {
                terminal.SetValue<int>("index", 0);
                DoSetService(terminal, VALID_SERVICES.Keys.FirstOrDefault());
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

                        break;
                }
                terminal.SetValue<int>("index", index);
                return ArkTerminalInterfaceType.None;
            }
        };

        private static Dictionary<ArkTerminalInterfaceType, ArkTerminalInterface> TERMINAL_INTERFACES = new Dictionary<ArkTerminalInterfaceType, ArkTerminalInterface>()
        {
            { ArkTerminalInterfaceType.Home, HOME_INTERFACE },
            { ArkTerminalInterfaceType.SessionExpided, SESSIONEXPIRED_INTERFACE },
            { ArkTerminalInterfaceType.ServiceSelect, SERVICESELECT_INTERFACE }
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

        private static bool canRun;
        private static ParallelTasks.Task task;

        private static bool _initialized = false;
        public static void Init()
        {
            if (SEDBStorage.Instance.ArkGrid.EntityId == 0)
            {
                Logging.Instance.LogWarning(typeof(ArkLogisticRelayController), "ArkGrid EntityId not defined!");
                return;
            }

            if (_initialized)
            {
                Dispose();
                _initialized = false;
            }

            ARKGRID = MyEntities.GetEntityById(SEDBStorage.Instance.ArkGrid.EntityId) as MyCubeGrid;
            if (ARKGRID == null)
            {
                Logging.Instance.LogWarning(typeof(ArkLogisticRelayController), $"ArkGrid EntityId={SEDBStorage.Instance.ArkGrid.EntityId} not valid!");
                return;
            }

            ARKGRIDTERMINALSYSTEM = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(ARKGRID);

            ARKGRIDSTOREBLOCK = ARKGRID.GetFatBlocks<MyStoreBlock>().FirstOrDefault();
            ARKGRIDCONTRACTBLOCK = ARKGRID.GetFatBlocks<MyContractBlock>().FirstOrDefault();

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
