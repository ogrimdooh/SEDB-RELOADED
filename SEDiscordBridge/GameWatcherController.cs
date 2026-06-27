using Newtonsoft.Json.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Contracts;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SEDiscordBridge.Controllers;
using SEDiscordBridge.Controllers.Economics;
using SEDiscordBridge.Controllers.Grids;
using SEDiscordBridge.Patches;
using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.FunctionalGrids;
using SEDiscordBridge.Storage.Player;
using SEDiscordBridge.Storage.SeasonMeta;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Torch.Commands;
using VRage.Game;
using VRage.Game.Definitions.SessionComponents;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Components;
using VRage.Game.ObjectBuilders.Definitions.SessionComponents;
using VRage.Utils;
using VRageMath;
using static SEDiscordBridge.Patches.MyDamageInformationExtensions;
using static VRage.Dedicated.Configurator.SelectInstanceForm;

namespace SEDiscordBridge
{

    public static class GameWatcherController
    {

        public const ushort NETWORK_ID_CALLSERVERSYSTEM = 40432;
        public const ushort NETWORK_ID_CALLCLIENTSYSTEM = 40431;

        public const string BRODCAST_LCDTEXTCHANGE = "BRODCAST_LCDTEXTCHANGE";

        private static bool _initialized = false;
        public static void Init()
        {
            if (_initialized)
            {
                Dispose();
                _initialized = false;
            }
            Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to PlayerDied");
            MyVisualScriptLogicProvider.PlayerDied += MyPlayer_Die;
            Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to RespawnShipSpawned");
            MyVisualScriptLogicProvider.RespawnShipSpawned += MyEntities_RespawnShipSpawned;
            Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to ContractAccepted");
            MyVisualScriptLogicProvider.ContractAccepted += ContractAccepted;
            Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to ContractFinished");
            MyVisualScriptLogicProvider.ContractFinished += ContractFinished;
            Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to ContractFailed");
            MyVisualScriptLogicProvider.ContractFailed += ContractFailed;
            Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to ContractAbandoned");
            MyVisualScriptLogicProvider.ContractAbandoned += ContractAbandoned;
            if (MySession.Static != null)
            {
                Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to MySession OnReady");
                MySession.Static.OnReady += Static_OnReady;
                Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to MySession OnUnloading");
                MySession.OnUnloading += MySession_OnUnloading;
                Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to MySession OnSaved");
                MySession.OnSaved += MySession_OnSaved;
                if (MySession.Static.Factions != null)
                {
                    Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to FactionCreated");
                    MySession.Static.Factions.FactionCreated += Factions_FactionCreated;
                    Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to FactionStateChanged");
                    MySession.Static.Factions.FactionStateChanged += Factions_FactionStateChanged;
                }
                if (MySession.Static.Gpss != null)
                {
                    Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to GpsAdded");
                    MySession.Static.Gpss.GpsAdded += Gpss_GpsAdded;
                }
                if (MySession.Static.Players != null)
                {
                    Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to Player Connected/Disconnected");
                    MySession.Static.Players.PlayersChanged += Players_PlayersChanged;
                }
                Logging.Instance.LogInfo(typeof(GameWatcherController), $"Register Secure Message Handler: Id={NETWORK_ID_CALLSERVERSYSTEM}");
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NETWORK_ID_CALLSERVERSYSTEM, ServerUpdateMsgHandler);
            }
            Logging.Instance.LogInfo(typeof(GameWatcherController), "Do Initial Load Entities");
            DoInitialLoadEntities();
            Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to MyEntities OnEntityAdd");
            MyEntities.OnEntityAdd += Entities_OnEntityAdd;
            Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to MyEntities OnEntityRemove");
            MyEntities.OnEntityRemove += Entities_OnEntityRemove;
            ServerConsumablesController.Init();
            _initialized = true;
        }

        private static void ContractAccepted(long contractId, MyDefinitionId contractDefinitionId, long acceptingPlayerId, bool isPlayerMade, long startingBlockId, long startingFactionId, long startingStationId)
        {

        }

        private static void ContractFinished(long contractId, MyDefinitionId contractDefinitionId, long acceptingPlayerId, bool isPlayerMade, long startingBlockId, long startingFactionId, long startingStationId)
        {
            var contract = ContractSystemOverriding.GetContractById(contractId);
            if (contract != null)
            {
                MyAPIGateway.Parallel.Start(() => {
                    // Register player contract completion in storage
                    var steamId = MySession.Static.Players.TryGetSteamId(acceptingPlayerId);
                    var playerStorage = SEDBStorage.Instance.GetPlayer(steamId);
                    if (!playerStorage.DidCompleteContract)
                    {
                        playerStorage.DidCompleteContract = true;
                        MyPlayer.PlayerId id2;
                        if (MySession.Static.Players.TryGetPlayerId(acceptingPlayerId, out id2))
                        {
                            var player2 = MySession.Static.Players.GetPlayerById(id2);
                            if (player2 != null && !string.IsNullOrWhiteSpace(player2.DisplayName))
                            {
                                SEDiscordBridgePlugin.Static.DDBridge.SendStatusMessage(player2.DisplayName, player2.Id.SteamId, SEDiscordBridgePlugin.Static.Config.CompleteFirstContractMessage);
                            }
                        }
                    }
                    var definition = contract.GetDefinition();
                    var completeContractCount = playerStorage.GetCompleteContractCount(definition.StrategyType);
                    playerStorage.SetCompleteContractCount(definition.StrategyType, completeContractCount + 1);
                    playerStorage.AllContractsCount = playerStorage.AllContractsCount + 1;
                    // Register a donation when contract is a deliver one
                    if (contract is MyContractObtainAndDeliver obtainAndDeliver)
                    {
                        var deliverCondition = obtainAndDeliver.ContractCondition as MyContractConditionDeliverItems;
                        if (deliverCondition != null)
                        {
                            SeasonDonationController.DoRegisterPlayerDonation(
                                steamId, 
                                SeasonMetaDonationOrigin.AcquisitionContract, 
                                new Dictionary<MyDefinitionId, float> { 
                                    { deliverCondition.ItemType, deliverCondition.ItemAmount } 
                                }
                            );
                        }
                    }
                });
            }
            else
            {
                Logging.Instance.LogWarning(typeof(GameWatcherController), $"ContractFinished: Contract not found for Id={contractId}");
            }
        }

        private static void ContractFailed(long contractId, MyDefinitionId contractDefinitionId, long acceptingPlayerId, bool isPlayerMade, long startingBlockId, long startingFactionId, long startingStationId, bool IsAbandon)
        {

        }

        private static void ContractAbandoned(long contractId, MyDefinitionId contractDefinitionId, long acceptingPlayerId, bool isPlayerMade, long startingBlockId, long startingFactionId, long startingStationId)
        {

        }

        private static void ServerUpdateMsgHandler(ushort netId, byte[] data, ulong steamId, bool fromServer)
        {
            try
            {
                if (netId != NETWORK_ID_CALLSERVERSYSTEM)
                    return;

                Logging.Instance.LogInfo(typeof(GameWatcherController), $"Received message from server, steamId: {steamId}, data length: {data.Length}");

                var message = Encoding.Unicode.GetString(data);
                var mCommandData = MyAPIGateway.Utilities.SerializeFromXML<SEDiscordBridge.Entities.Command>(message);
                if (mCommandData.Content.Length > 0)
                {
                    Logging.Instance.LogInfo(typeof(GameWatcherController), $"Command {mCommandData.Content[0]} content length: {mCommandData.Content.Length}");

                    switch (mCommandData.Content[0])
                    {
                        // TODO
                    }
                }
                else
                {
                    Logging.Instance.LogInfo(typeof(GameWatcherController), $"Command content is empty");
                }
            }
            catch (Exception ex)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), ex);
            }
        }

        public static void SendLcdTextChange(ulong target, long lcdId, string lcdText)
        {
            var content = new List<string>
            {
                BRODCAST_LCDTEXTCHANGE,
                lcdId.ToString(),
                lcdText
            };
            var cmd = new Entities.Command(0, content.ToArray()); 
            string messageToSend = MyAPIGateway.Utilities.SerializeToXML<Entities.Command>(cmd);
            if (target != 0)
            {
                Logging.Instance.LogInfo(typeof(GameWatcherController), $"Sending command to {target} with content Length={messageToSend.Length}");
                MyAPIGateway.Multiplayer.SendMessageTo(
                    NETWORK_ID_CALLCLIENTSYSTEM,
                    Encoding.Unicode.GetBytes(messageToSend),
                    target
                );
            }
            else
            {
                Logging.Instance.LogInfo(typeof(GameWatcherController), $"Sending command to others with content Length={messageToSend.Length}");
                MyAPIGateway.Multiplayer.SendMessageToOthers(
                    NETWORK_ID_CALLCLIENTSYSTEM,
                    Encoding.Unicode.GetBytes(messageToSend)
                );
            }
        }

        private static void Players_PlayersChanged(bool connected, MyPlayer.PlayerId id)
        {
            if (connected)
                Players_PlayerConnected(id);
            else
                Players_PlayerDisconnected(id);
        }

        private static bool _inicialLoadComplete = false;
        private static void DoInitialLoadEntities()
        {
            if (!_inicialLoadComplete)
            {
                foreach (var entity in MyEntities.GetEntities())
                {
                    Entities_OnEntityAdd(entity);
                }
                _inicialLoadComplete = true;
            }
        }

        public static ConcurrentDictionary<long, MyPlanet> Planets { get; private set; } = new ConcurrentDictionary<long, MyPlanet>();
        public static ConcurrentDictionary<long, MySafeZone> SafeZones { get; private set; } = new ConcurrentDictionary<long, MySafeZone>();
        public static ConcurrentDictionary<MyPlayer.PlayerId, MyPlayer> Players { get; private set; } = new ConcurrentDictionary<MyPlayer.PlayerId, MyPlayer>();

        public static MyPlanet GetPlanetAtRange(Vector3D position)
        {
            return Planets.Values.OrderBy(x => Vector3D.Distance(position, x.PositionComp.GetPosition())).FirstOrDefault();
        }

        public static bool IsOnSafeZone(Vector3D position)
        {
            return SafeZones.Values.Any(x => x.Contains(position));
        }

        private static void Players_PlayerConnected(MyPlayer.PlayerId id)
        {
            if (!Players.ContainsKey(id))
            {
                var p = MySession.Static.Players.GetPlayerById(id);
                if (p != null && p.IsValidPlayer())
                {
                    Players[id] = p;
                }
            }
        }

        private static void Players_PlayerDisconnected(MyPlayer.PlayerId id)
        {
            if (Players.ContainsKey(id))
                Players.Remove(id);
        }

        private static void UpdatePlayerList()
        {
            Players.Clear();
            var tempPlayers = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(tempPlayers);

            foreach (var p in tempPlayers)
            {
                if (p.IsValidPlayer())
                {
                    Players[(p as MyPlayer).Id] = p as MyPlayer;
                }
            }
        }

        private static void Entities_OnEntityAdd(MyEntity entity)
        {
            try
            {
                var planet = entity as MyPlanet;
                if (planet != null)
                {
                    lock (Planets)
                    {
                        Planets[planet.EntityId] = planet;
                    }
                    return;
                }
                var safeZone = entity as MySafeZone;
                if (safeZone != null)
                {
                    lock (SafeZones)
                    {
                        SafeZones[safeZone.EntityId] = safeZone;
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), e);
            }
        }

        private static void CheckOnPlayerGravityMessages(MyPlayer.PlayerId playerId, MyCharacter character)
        {

            if (character == null) return;

            if (!SEDiscordBridgePlugin.Static.Config.DisplayGridsGravityMessages) return;

            if (!Players.ContainsKey(playerId))
                UpdatePlayerList();

            if (!Players.ContainsKey(playerId)) return;

            var player = Players[playerId];

            var playerStorage = SEDBStorage.Instance.GetPlayer(playerId.SteamId);

            var playerPos = player.GetPosition();

            if (MyGravityProviderSystem.IsPositionInNaturalGravity(playerPos))
            {
                /* Player Enters in Gravity Field */
                
                if (!playerStorage.LastLocationIsGravity)
                {

                    var action = SEDiscordBridgePlugin.Static.Config.GravityActionEnter;
                    var gridName = "";
                    var planetName = SEDiscordBridgePlugin.Static.Config.UnknowPlanetNameToUse;

                    var planet = GetPlanetAtRange(playerPos);

                    if (planet != null && playerStorage.LastLocationEntityId != planet.EntityId)
                    {

                        var didVisitLocation = playerStorage.GetLocationVisited(planet.EntityId);

                        IMyCubeBlock cockpit = null;
                        if (character != null)
                        {
                            cockpit = character.Parent as IMyCubeBlock;
                        }
                        else
                        {
                            cockpit = player.Controller?.ControlledEntity as IMyCubeBlock;                            
                        }

                        if (cockpit != null)
                        {
                            gridName = cockpit.CubeGrid?.DisplayName;
                            if (string.IsNullOrEmpty(gridName))
                                gridName = SEDiscordBridgePlugin.Static.Config.UnknowGravityGridName;
                        }

                        if (SEDiscordBridgePlugin.Static.Config.DisplayEnterGravityMessages || 
                            (!didVisitLocation && SEDiscordBridgePlugin.Static.Config.DisplayFirstEnterGravityMessages))
                        {

                            if (!didVisitLocation)
                            {
                                action = SEDiscordBridgePlugin.Static.Config.GravityActionFirstEnter;
                            }

                            planetName = planet.DisplayName;
                            if (string.IsNullOrEmpty(planetName))
                                planetName = planet.Generator?.Id.SubtypeName;
                            if (planetName.Contains("_"))
                            {
                                var nameParts = planetName.Split('_');
                                var namesToUse = nameParts.Where(x => !long.TryParse(x, out _)).ToArray();
                                planetName = string.Join(" ", nameParts);
                            }

                            var msgToUse = string.IsNullOrEmpty(gridName) ?
                                SEDiscordBridgePlugin.Static.Config.PilotNoGridGravityMessage :
                                SEDiscordBridgePlugin.Static.Config.GridGravityMessage;
                            msgToUse = msgToUse.Replace("{a}", action);
                            msgToUse = msgToUse.Replace("{g}", gridName);
                            msgToUse = msgToUse.Replace("{t}", planetName);

                            SEDiscordBridgePlugin.Static.DDBridge.SendStatusMessage(player.DisplayName, playerId.SteamId, msgToUse);

                        }

                        playerStorage.DidRegistrationLocation = true;
                        playerStorage.LastLocationIsGravity = true;
                        playerStorage.LastLocationEntityId = planet.EntityId;
                        playerStorage.SetLocationVisited(planet.EntityId, true);

                    }

                }

            }
            else
            {

                /* Player Leaves in Gravity Field */
                if (playerStorage.DidRegistrationLocation && playerStorage.LastLocationIsGravity)
                {

                    var action = SEDiscordBridgePlugin.Static.Config.GravityActionLeave;
                    var gridName = "";
                    var planetName = SEDiscordBridgePlugin.Static.Config.UnknowPlanetNameToUse;

                    if (SEDiscordBridgePlugin.Static.Config.DisplayLeaveGravityMessages)
                    {

                        var cockpit = character.Parent as IMyCubeBlock;
                        if (cockpit != null)
                        {
                            gridName = cockpit.CubeGrid?.DisplayName;
                            if (string.IsNullOrEmpty(gridName))
                                gridName = SEDiscordBridgePlugin.Static.Config.UnknowGravityGridName;
                        }

                        var planet = GetPlanetAtRange(playerPos);

                        var distanceToPlanet = Math.Abs(Vector3D.Distance(planet.PositionComp.GetPosition(), playerPos)) / 1000;

                        if (planet != null && distanceToPlanet <= SEDiscordBridgePlugin.Static.Config.MaxDistanceToDetectAPlanet)
                        {
                            planetName = planet.DisplayName;
                            if (string.IsNullOrEmpty(planetName))
                                planetName = planet.Generator?.Id.SubtypeName;
                            if (planetName.Contains("_"))
                            {
                                var nameParts = planetName.Split('_');
                                var namesToUse = nameParts.Where(x => !long.TryParse(x, out _)).ToArray();
                                planetName = string.Join(" ", nameParts);
                            }
                        }

                        var msgToUse = string.IsNullOrEmpty(gridName) ?
                            SEDiscordBridgePlugin.Static.Config.PilotNoGridGravityMessage :
                            SEDiscordBridgePlugin.Static.Config.GridGravityMessage;
                        msgToUse = msgToUse.Replace("{a}", action);
                        msgToUse = msgToUse.Replace("{g}", gridName);
                        msgToUse = msgToUse.Replace("{t}", planetName);

                        SEDiscordBridgePlugin.Static.DDBridge.SendStatusMessage(player.DisplayName, player.Id.SteamId, msgToUse);

                    }

                    playerStorage.DidRegistrationLocation = true;
                    playerStorage.LastLocationIsGravity = false;
                    playerStorage.LastLocationEntityId = 0;

                }

                if (!playerStorage.DidRegistrationLocation)
                {

                    playerStorage.DidRegistrationLocation = true;
                    playerStorage.LastLocationIsGravity = false;
                    playerStorage.LastLocationEntityId = 0;

                }

            }
        }

        private static void CheckOnPlayerList()
        {
            try
            {
                if (_inicialLoadComplete && MySession.Static != null && MySession.Static.Ready)
                {

                    if (!SEDiscordBridgePlugin.Static.Config.Enabled) return;

                    foreach (var playerId in Players.Keys)
                    {
                        CheckOnPlayerGravityMessages(playerId, Players[playerId].Character);
                    }

                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), e);
            }
        }

        private static void Entities_OnEntityRemove(MyEntity entity)
        {
            try
            {
                var planet = entity as MyPlanet;
                if (planet != null && Planets.ContainsKey(planet.EntityId))
                {
                    lock (Planets)
                    {
                        Planets.Remove(planet.EntityId);
                    }
                    return;
                }
                var safeZone = entity as MySafeZone;
                if (safeZone != null && SafeZones.ContainsKey(safeZone.EntityId))
                {
                    lock (SafeZones)
                    {
                        SafeZones.Remove(safeZone.EntityId);
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), e);
            }
        }

        private static void MySession_OnSaved(bool p1, string p2)
        {
            SEDBStorage.Save();
        }

        private static void MySession_OnUnloading()
        {
            SEDBStorage.Save();
            ArkLogisticRelayController.Dispose();
            ArkGroundBaseController.Dispose();
        }

        private static bool canRun;
        private static ParallelTasks.Task task;
        private static void Static_OnReady()
        {
            try
            {
                if (SEDiscordBridgePlugin.Static?.DDBridge != null)
                {
                    Logging.Instance.LogInfo(typeof(GameWatcherController), "MySession OnReady");
                    // Comunicate server is ready
                    SEDiscordBridgePlugin.Static.DDBridge.SendStatusMessage(default, default, SEDiscordBridgePlugin.Static.Config.Started);
                    // Update player list
                    UpdatePlayerList();
                    // Start player parallel check task
                    canRun = true;
                    task = MyAPIGateway.Parallel.StartBackground(() =>
                    {
                        Logging.Instance.LogInfo(typeof(GameWatcherController), "StartBackground [START]");
                        while (canRun)
                        {
                            CheckOnPlayerList();
                            if (MyAPIGateway.Parallel != null && SEDiscordBridgePlugin.Static?.Config != null)
                                MyAPIGateway.Parallel.Sleep(SEDiscordBridgePlugin.Static.Config.PlayerCheckStatusInterval);
                            else
                                break;
                        }
                    });
                    // Check Season Meta Storage to load initial data
                    SEDBStorage.Instance.SeasonMetaConfig.LoadInitialData();
                }
                else
                {
                    Logging.Instance.LogWarning(typeof(GameWatcherController), "DDBridge not found when Session Ready!");
                }
                FactionsController.ResetMainFactionBank();
                EconomicsConstants.Init();
                ItemPriceController.Init();
                /* Registra */
                ArkLogisticRelayController.Register();
                ArkGroundBaseController.Register();
                /* Inicializa */
                ArkLogisticRelayController.Init();
                ArkGroundBaseController.Init();
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), e);
            }
        }

        private static void Gpss_GpsAdded(long playerId, int gps)
        {
            try
            {
                if (!SEDiscordBridgePlugin.Static.Config.Enabled) return;

                if (!SEDiscordBridgePlugin.Static.Config.DisplayContainerMessages) return;

                if (MySession.Static?.Gpss != null)
                {
                    var gpsData = MySession.Static.Gpss.GetGps(playerId, gps);
                    if (gpsData != null)
                    {
                        var gpsName = gpsData.Name ?? "";
                        if (gpsData.IsContainerGPS && gpsName.ToLower().Contains("signal"))
                        {

                            bool isStrong = gpsName.Contains("strong");

                            if (SEDiscordBridgePlugin.Static.Config.DisplayOnlyStrongContainerMessages && !isStrong) return;

                            var msgToUse = isStrong ?
                                SEDiscordBridgePlugin.Static.Config.StrongContainerMessage : 
                                SEDiscordBridgePlugin.Static.Config.ContainerMessage;
                            var finalName = string.Join(" ",
                                gpsName
                                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Reverse()
                                .Skip(1)
                                .Reverse()
                                .ToArray()
                            );

                            msgToUse = msgToUse.Replace("{t}", finalName);
                            if (isStrong)
                            {
                                if (gpsData != null && gpsData.Entity != null)
                                {
                                    msgToUse = msgToUse.Replace("{c}", $"{gpsData.Coords.X}:{gpsData.Coords.Y}:{gpsData.Coords.Z}");
                                }
                                else
                                {
                                    msgToUse = msgToUse.Replace("{c}", "Lost Position");
                                }
                            }
                            else
                            {
                                msgToUse = msgToUse.Replace("{c}", "Unknow Position");
                            }
                            if (MySession.Static?.Players != null)
                            {
                                MyPlayer.PlayerId id;
                                if (MySession.Static.Players.TryGetPlayerId(playerId, out id))
                                {
                                    var player = MySession.Static.Players.GetPlayerById(id);
                                    if (player != null && !string.IsNullOrWhiteSpace(player.DisplayName))
                                    {
                                        SEDiscordBridgePlugin.Static.DDBridge.SendStatusMessage(player.DisplayName, player.Id.SteamId, msgToUse);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), e);
            }
        }

        public static void Dispose()
        {
            canRun = false;
            try
            {
                task.Wait();
            }
            catch (Exception ex)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), ex);
            }
            MyVisualScriptLogicProvider.PlayerDied -= MyPlayer_Die;
            MyVisualScriptLogicProvider.RespawnShipSpawned -= MyEntities_RespawnShipSpawned;
            MyVisualScriptLogicProvider.ContractAccepted -= ContractAccepted;
            MyVisualScriptLogicProvider.ContractFinished -= ContractFinished;
            MyVisualScriptLogicProvider.ContractFailed -= ContractFailed;
            MyVisualScriptLogicProvider.ContractAbandoned -= ContractAbandoned;
            if (MySession.Static != null)
            {
                MySession.Static.OnReady -= Static_OnReady;
                MySession.OnUnloading -= MySession_OnUnloading;
                MySession.OnSaved -= MySession_OnSaved;

                if (MySession.Static.Factions != null)
                {
                    MySession.Static.Factions.FactionCreated -= Factions_FactionCreated;
                    MySession.Static.Factions.FactionStateChanged -= Factions_FactionStateChanged;
                }
                if (MySession.Static.Gpss != null)
                {
                    MySession.Static.Gpss.GpsAdded -= Gpss_GpsAdded;
                }
                if (MySession.Static.Players != null)
                {
                    MySession.Static.Players.PlayersChanged -= Players_PlayersChanged;
                }
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NETWORK_ID_CALLSERVERSYSTEM, ServerUpdateMsgHandler);
            }
            MyEntities.OnEntityAdd -= Entities_OnEntityAdd;
            MyEntities.OnEntityRemove -= Entities_OnEntityRemove;
            ServerConsumablesController.Dispose();
        }

        private static bool IsFactionChangeValidToMsg(MyFactionStateChange action, out int msgType)
        {
            msgType = 0;
            switch (action)
            {
                case MyFactionStateChange.SendPeaceRequest:
                case MyFactionStateChange.AcceptPeace:
                case MyFactionStateChange.DeclareWar:
                case MyFactionStateChange.SendFriendRequest:
                case MyFactionStateChange.AcceptFriendRequest:
                    return true;
                case MyFactionStateChange.FactionMemberSendJoin:
                case MyFactionStateChange.FactionMemberLeave:
                    msgType = 1;
                    return true;
                case MyFactionStateChange.FactionMemberAcceptJoin:
                case MyFactionStateChange.FactionMemberKick:
                case MyFactionStateChange.FactionMemberPromote:
                case MyFactionStateChange.FactionMemberDemote:
                    msgType = 2;
                    return true;
                case MyFactionStateChange.RemoveFaction:
                    msgType = 3;
                    return true;
                case MyFactionStateChange.CancelPeaceRequest:
                case MyFactionStateChange.CancelFriendRequest:
                case MyFactionStateChange.FactionMemberCancelJoin:
                case MyFactionStateChange.FactionMemberNotPossibleJoin:
                default:
                    return false;
            }
        }

        private static string GetActionTitle(MyFactionStateChange action)
        {
            switch (action)
            {
                case MyFactionStateChange.RemoveFaction:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionRemoveFaction;
                case MyFactionStateChange.SendPeaceRequest:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionSendPeaceRequest;
                case MyFactionStateChange.CancelPeaceRequest:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionCancelPeaceRequest;
                case MyFactionStateChange.AcceptPeace:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionAcceptPeace;
                case MyFactionStateChange.DeclareWar:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionDeclareWar;
                case MyFactionStateChange.SendFriendRequest:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionSendFriendRequest;
                case MyFactionStateChange.CancelFriendRequest:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionCancelFriendRequest;
                case MyFactionStateChange.AcceptFriendRequest:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionAcceptFriendRequest;
                case MyFactionStateChange.FactionMemberSendJoin:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionFactionMemberSendJoin;
                case MyFactionStateChange.FactionMemberCancelJoin:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionFactionMemberCancelJoin;
                case MyFactionStateChange.FactionMemberAcceptJoin:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionFactionMemberAcceptJoin;
                case MyFactionStateChange.FactionMemberKick:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionFactionMemberKick;
                case MyFactionStateChange.FactionMemberPromote:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionFactionMemberPromote;
                case MyFactionStateChange.FactionMemberDemote:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionFactionMemberDemote;
                case MyFactionStateChange.FactionMemberLeave:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionFactionMemberLeave;
                case MyFactionStateChange.FactionMemberNotPossibleJoin:
                    return SEDiscordBridgePlugin.Static.Config.FactionActionFactionMemberNotPossibleJoin;
                default:
                    return "";
            }
        }

        private static void Factions_FactionStateChanged(MyFactionStateChange action, long fromFactionId, long toFactionId, long playerId, long senderId)
        {
            try
            {
                if (!SEDiscordBridgePlugin.Static.Config.Enabled) return;

                if (!SEDiscordBridgePlugin.Static.Config.DisplayFactionMessages) return;

                if (MySession.Static?.Factions == null) return;

                int msgType = 0;
                if (IsFactionChangeValidToMsg(action, out msgType))
                {
                    var msgToUse = "";
                    switch (msgType)
                    {
                        case 1:
                            msgToUse = SEDiscordBridgePlugin.Static.Config.FactionMemberActionFactionMessage;
                            break;
                        case 2:
                            msgToUse = SEDiscordBridgePlugin.Static.Config.FactionMemberActionMemberMessage;
                            break;
                        case 3:
                            msgToUse = SEDiscordBridgePlugin.Static.Config.FactionRemovedMessage;
                            break;
                        default:
                            msgToUse = SEDiscordBridgePlugin.Static.Config.FactionActionMessage;
                            break;
                    }
                    var actionTitle = GetActionTitle(action);
                    msgToUse = msgToUse.Replace("{a}", actionTitle);
                    var fromFaction = MySession.Static.Factions.TryGetFactionById(fromFactionId);
                    if (fromFaction != null)
                    {
                        if (SEDiscordBridgePlugin.Static.Config.IgnoreNpcFactionsInMessages && MySession.Static.Factions.IsNpcFaction(fromFaction.Tag)) return;

                        if (SEDiscordBridgePlugin.Static.Config.IgnoredFactionTags.Split(';').Contains(fromFaction.Tag)) return;

                        msgToUse = msgToUse.Replace("{f}", $"[{fromFaction.Tag}] {fromFaction.Name}");
                    }
                    var toFaction = MySession.Static.Factions.TryGetFactionById(toFactionId);
                    if (toFaction != null)
                    {
                        if (SEDiscordBridgePlugin.Static.Config.IgnoreNpcFactionsInMessages && MySession.Static.Factions.IsNpcFaction(toFaction.Tag)) return;

                        if (SEDiscordBridgePlugin.Static.Config.IgnoredFactionTags.Split(';').Contains(toFaction.Tag)) return;

                        msgToUse = msgToUse.Replace("{f2}", $"[{toFaction.Tag}] {toFaction.Name}");
                    }
                    if (senderId != 0)
                    {
                        var senderName = Utils.GetPlayerName(senderId);
                        if (!string.IsNullOrWhiteSpace(senderName))
                        {
                            msgToUse = msgToUse.Replace("{p2}", senderName);
                        }
                    }
                    MyPlayer.PlayerId id;
                    if (MySession.Static.Players.TryGetPlayerId(playerId, out id))
                    {
                        var player = MySession.Static.Players.GetPlayerById(id);

                        if (player == null) return;

                        if (player.IsBot && SEDiscordBridgePlugin.Static.Config.IgnoreBotInFactionMessages) return;

                        if (!string.IsNullOrWhiteSpace(player.DisplayName))
                        {
                            SEDiscordBridgePlugin.Static.DDBridge.SendStatusMessage(player.DisplayName, player.Id.SteamId, msgToUse);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), e);
            }
        }

        private static void Factions_FactionCreated(long factionId)
        {
            try
            {
                if (!SEDiscordBridgePlugin.Static.Config.Enabled) return;

                if (!SEDiscordBridgePlugin.Static.Config.DisplayFactionMessages) return;

                var faction = MySession.Static.Factions.TryGetFactionById(factionId);
                if (faction != null)
                {
                    if (SEDiscordBridgePlugin.Static.Config.IgnoreNpcFactionsInMessages && MySession.Static.Factions.IsNpcFaction(faction.Tag)) return;

                    if (SEDiscordBridgePlugin.Static.Config.IgnoredFactionTags.Split(';').Contains(faction.Tag)) return;

                    MyPlayer.PlayerId id;
                    if (MySession.Static.Players.TryGetPlayerId(faction.FounderId, out id))
                    {
                        var player = MySession.Static.Players.GetPlayerById(id);

                        if (player.IsBot && SEDiscordBridgePlugin.Static.Config.IgnoreBotInFactionMessages) return;

                        if (!string.IsNullOrWhiteSpace(player.DisplayName))
                        {
                            var msgToUse = SEDiscordBridgePlugin.Static.Config.FactionCretedMessage.Replace("{f}", $"[{faction.Tag}] {faction.Name}");
                            SEDiscordBridgePlugin.Static.DDBridge.SendStatusMessage(player.DisplayName, player.Id.SteamId, msgToUse);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), e);
            }
        }

        private static void MyEntities_RespawnShipSpawned(long shipEntityId, long playerId, string respawnShipPrefabName)
        {
            try
            {
                if (!SEDiscordBridgePlugin.Static.Config.Enabled) return;

                if (!SEDiscordBridgePlugin.Static.Config.DisplayRespawnMessages) return;

                MyPlayer.PlayerId id;
                if (MySession.Static.Players.TryGetPlayerId(playerId, out id))
                {
                    var player = MySession.Static.Players.GetPlayerById(id);
                    if (!string.IsNullOrWhiteSpace(player.DisplayName))
                    {
                        SEDiscordBridgePlugin.Static.DDBridge.SendStatusMessage(player.DisplayName, player.Id.SteamId, SEDiscordBridgePlugin.Static.Config.RespawnMessage);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), e);
            }
        }

        private static string GetDamageTypeDescription(MyDamageInformationExtensions.DamageType damageType)
        {
            switch (damageType)
            {
                case MyDamageInformationExtensions.DamageType.Creature:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseCreature;
                case MyDamageInformationExtensions.DamageType.Bullet:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseBullet;
                case MyDamageInformationExtensions.DamageType.Explosion:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseExplosion;
                case MyDamageInformationExtensions.DamageType.Radioactivity:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseRadioactivity;
                case MyDamageInformationExtensions.DamageType.Fire:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseFire;
                case MyDamageInformationExtensions.DamageType.Toxicity:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseToxicity;
                case MyDamageInformationExtensions.DamageType.Fall:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseFall;
                case MyDamageInformationExtensions.DamageType.Tool:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseTool;
                case MyDamageInformationExtensions.DamageType.Environment:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseEnvironment;
                case MyDamageInformationExtensions.DamageType.Suicide:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseSuicide;
                case MyDamageInformationExtensions.DamageType.Asphyxia:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseAsphyxia;
                case MyDamageInformationExtensions.DamageType.Other:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseOther;
                case MyDamageInformationExtensions.DamageType.None:
                default:
                    return SEDiscordBridgePlugin.Static.Config.DieCauseNone;
            }
        }

        private static void MyPlayer_Die(long playerId)
        {
            try
            {
                if (!SEDiscordBridgePlugin.Static.Config.Enabled) return;

                if (!SEDiscordBridgePlugin.Static.Config.DisplayDieMessages) return;

                MyPlayer.PlayerId id;
                if (MySession.Static.Players.TryGetPlayerId(playerId, out id))
                {
                    var player = MySession.Static.Players.GetPlayerById(id);
                    if (player != null && !string.IsNullOrWhiteSpace(player.DisplayName))
                    {

                        if (SEDiscordBridgePlugin.Static.Config.IgnoreBotDieMessages && player.IsBot) return;

                        long attackerPlayerId = 0;
                        MyDamageInformationExtensions.DamageType damageType;
                        MyDamageInformationExtensions.AttackerType attackerType = MyDamageInformationExtensions.AttackerType.None;
                        VRage.ModAPI.IMyEntity attackerEntity = null;
                        var damage = player.Character.StatComp.LastDamage;
                        if (damage.AttackerId != 0)
                            attackerEntity = damage.GetAttacker(out attackerPlayerId, out damageType, out attackerType);
                        else
                            damageType = MyDamageInformationExtensions.GetDamageType(damage.Type);
                        var isAttackerPlayer = MyAPIGateway.Players.TryGetSteamId(attackerPlayerId) != 0;
                        var msgToUse = SEDiscordBridgePlugin.Static.Config.DieMessage;
                        if (attackerPlayerId != 0 && isAttackerPlayer && attackerPlayerId != playerId)
                        {
                            MyPlayer.PlayerId id2;
                            if (MySession.Static.Players.TryGetPlayerId(attackerPlayerId, out id2))
                            {
                                var player2 = MySession.Static.Players.GetPlayerById(id2);
                                if (player2 != null && !string.IsNullOrWhiteSpace(player2.DisplayName))
                                {
                                    var playerStorage = SEDBStorage.Instance.GetPlayer(id2.SteamId);

                                    if (!playerStorage.DidKill)
                                    {
                                        SEDiscordBridgePlugin.Static.DDBridge.SendStatusMessage(player2.DisplayName, player2.Id.SteamId, SEDiscordBridgePlugin.Static.Config.FirstKillMessage);
                                        playerStorage.DidKill = true;
                                    }
                                    var killCount = playerStorage.KillCount;
                                    playerStorage.KillCount = killCount;

                                    msgToUse = SEDiscordBridgePlugin.Static.Config.MurderMessage;
                                    msgToUse = msgToUse.Replace("{p2}", player2.DisplayName);
                                }
                            }
                        }

                        if (SEDiscordBridgePlugin.DEBUG)
                        {
                            Logging.Instance.LogInfo(typeof(SEDiscordBridgePlugin), $"MyPlayer_Die: playerId={playerId} | AttackerId={damage.AttackerId} | attackerPlayerId={attackerPlayerId} | damage={damage.Type} | damageType={damageType}");
                        }

                        msgToUse = msgToUse.Replace("{c}", GetDamageTypeDescription(damageType));
                        msgToUse = msgToUse.Replace("{d}", damage.Amount.ToString("#0.0"));
                        SEDiscordBridgePlugin.Static.DDBridge.SendStatusMessage(player.DisplayName, player.Id.SteamId, msgToUse);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), e);
            }
        }

    } 
}
