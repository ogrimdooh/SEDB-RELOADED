using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SEDiscordBridge.Entities.Base;
using SEDiscordBridge.Patches;
using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.Player;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace SEDiscordBridge
{
    public static class ServerConsumablesController
    {

        public class ServerConsumableHandler
        {

            public UniqueEntityId Id { get; set; }
            public Func<MyCharacter, bool> OnConsume { get; set; }

        }

        public class DropSignalConsumableHandler : ServerConsumableHandler
        {

            public string PrefabName { get; set; }
            public string DisplayName { get; set; }

            public DropSignalConsumableHandler()
            {
                OnConsume = (character) =>
                {
                    Logging.Instance.LogInfo(typeof(ServerConsumablesController), $"Drop signal handler start!");
                    var prefabs = MyDefinitionManager.Static.GetPrefabDefinitions();
                    if (prefabs.Any(x => x.Key.ToLower() == PrefabName.ToLower()))
                    {
                        Logging.Instance.LogInfo(typeof(ServerConsumablesController), $"Drop signal prefab found!");
                        var prefab = prefabs.FirstOrDefault(x => x.Key.ToLower() == PrefabName.ToLower()).Value;
                        var playerId = character.GetPlayerId();
                        var pos = (character as IMyEntity).GetPosition();
                        var head = character.GetHeadMatrix(true);
                        var look = head.Forward;
                        float naturalGravityInterference;
                        Vector3 naturalGravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(pos, out naturalGravityInterference);
                        if (naturalGravityInterference > 0)
                        {
                            Logging.Instance.LogInfo(typeof(ServerConsumablesController), $"Player is in natural gravity interference!");
                            var sphere = new BoundingSphereD(pos, 5);
                            var voxels = new List<MyVoxelBase>();
                            MyGamePruningStructure.GetAllVoxelMapsInSphere(ref sphere, voxels);
                            if (voxels.Any(x => x is MyPlanet))
                            {
                                Logging.Instance.LogInfo(typeof(ServerConsumablesController), $"Found the planet in the voxels list!");
                                var planet = voxels.Where(x => x is MyPlanet).Select(x => x as MyPlanet).FirstOrDefault();
                                if (planet.GetHeightFromSurface(pos) <= 1)
                                {
                                    Logging.Instance.LogInfo(typeof(ServerConsumablesController), $"Player is near of the surface!");
                                    if (!GameWatcherController.IsOnSafeZone(pos))
                                    {
                                        Logging.Instance.LogInfo(typeof(ServerConsumablesController), $"Player is not in a safe zone!");
                                        var surface = planet.GetClosestSurfacePointGlobal(pos);
                                        var center = (planet as IMyEntity).GetPosition();
                                        var up = Vector3D.Normalize(surface - center);
                                        var spawnPos = surface + (up * 1000);
                                        spawnPos = MyEntities.FindFreePlaceCustom(spawnPos, 250) ?? spawnPos; // Try to avoid spawn inside something
                                        var gridListDummy = new List<IMyCubeGrid>();
                                        var options = SpawningOptions.UseOnlyWorldMatrix | SpawningOptions.SetAuthorship;
                                        MyAPIGateway.PrefabManager.SpawnPrefab(
                                            gridListDummy,
                                            prefab.Id.SubtypeName,
                                            spawnPos,
                                            (Vector3)look,
                                            (Vector3)up,
                                            Vector3.Zero,
                                            Vector3.Zero,
                                            prefab.Id.SubtypeName,
                                            options,
                                            playerId,
                                            true,
                                            () =>
                                            {
                                                Logging.Instance.LogInfo(typeof(ServerConsumablesController), $"Grid has spawn!");
                                                var cubeGrid = gridListDummy.OrderByDescending(x => (x as MyCubeGrid).BlocksCount).FirstOrDefault();
                                                if (cubeGrid != null)
                                                {
                                                    Logging.Instance.LogInfo(typeof(ServerConsumablesController), $"Main grid found!");
                                                    cubeGrid.IsRespawnGrid = true;
                                                    var parachutes = cubeGrid.GetFatBlocks<MyParachute>();
                                                    if (parachutes != null && parachutes.Any())
                                                    {
                                                        foreach (var parachute in parachutes)
                                                        {
                                                            parachute.AutoDeploy = true;
                                                            var pInv = parachute.GetInventory();
                                                            if (pInv != null)
                                                            {
                                                                var canvasAmount = pInv.GetItemAmount(CANVAS_ID.DefinitionId);
                                                                if (canvasAmount == 0)
                                                                {
                                                                    if (cubeGrid.GridSizeEnum == MyCubeSize.Large)
                                                                    {
                                                                        pInv.AddItems(5, GetPhysicalObjectBuilder(CANVAS_ID));
                                                                    }
                                                                    else
                                                                    {
                                                                        pInv.AddItems(1, GetPhysicalObjectBuilder(CANVAS_ID));
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    var cargos = cubeGrid.GetFatBlocks<MyCargoContainer>();
                                                    if (cargos != null && cargos.Any())
                                                    {
                                                        var mainCargo = cargos.OrderByDescending(x => (float)(x.GetInventory()?.MaxVolume)).FirstOrDefault();
                                                        if (mainCargo != null)
                                                        {
                                                            var cInv = mainCargo.GetInventory();
                                                            cInv.AddItems(5, GetPhysicalObjectBuilder(CANVAS_ID));
                                                            // TODO: Add all extra items in this position
                                                        }
                                                    }
                                                    // Cria GPS
                                                    var gps = new MyGps()
                                                    {
                                                        Name = $"Ark Drop: {cubeGrid.DisplayName}",
                                                        ShowOnHud = true,
                                                        GPSColor = new Color(0, 128, 192),
                                                        IsContainerGPS = true,
                                                        DiscardAt = null
                                                    };
                                                    gps.SetEntity(cubeGrid);
                                                    gps.UpdateHash();
                                                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                                                    {
                                                        // Cria GPS da nova grid
                                                        MySession.Static.Gpss.SendAddGpsRequest(playerId, ref gps);
                                                        // Salva ID da ultima grid de respawn, se tiver uma ja criada apaga ela
                                                        var steamId = MyAPIGateway.Players.TryGetSteamId(playerId);
                                                        var currentGridId = SEDBStorage.Instance.GetPlayerValue<long>(steamId, PlayerStorage.KEY_LAST_RESPAWN_GRID);
                                                        if (currentGridId != 0)
                                                        {
                                                            var curEntity = MyEntities.GetEntityById(currentGridId) as MyCubeGrid;
                                                            if (curEntity != null)
                                                            {
                                                                var whells = curEntity.GetConnectedGrids(GridLinkTypeEnum.Mechanical);
                                                                if (whells != null && whells.Any())
                                                                {
                                                                    foreach (var whell in whells.Where(x => x.BlocksCount == 1))
                                                                    {
                                                                        whell.Close();
                                                                    }
                                                                }
                                                                curEntity.Close();
                                                            }
                                                        }
                                                        SEDBStorage.Instance.SetPlayerValue<long>(steamId, PlayerStorage.KEY_LAST_RESPAWN_GRID, cubeGrid.EntityId);
                                                    });
                                                }
                                            }
                                        );
                                        MyPlayer.PlayerId id;
                                        if (MySession.Static.Players.TryGetPlayerId(playerId, out id))
                                        {
                                            var player = MySession.Static.Players.GetPlayerById(id);
                                            if (!string.IsNullOrWhiteSpace(player.DisplayName))
                                            {
                                                var msg = SEDiscordBridgePlugin.Static.Config.CallDropMessage.Replace("{g}", DisplayName);
                                                SEDiscordBridgePlugin.Static.DDBridge.SendStatusMessage(player.DisplayName, player.Id.SteamId, msg);
                                            }
                                        }
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    return false;
                };
            }

        }

        public const string CANVAS_SUBTYPEID = "Canvas";
        public static readonly UniqueEntityId CANVAS_ID = new UniqueEntityId(typeof(MyObjectBuilder_Component), CANVAS_SUBTYPEID);

        public const string DAWNDROPSIGNALEXPLORER_SUBTYPEID = "DAWNDropSignalExplorer";
        public static readonly UniqueEntityId DAWNDROPSIGNALEXPLORER_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), DAWNDROPSIGNALEXPLORER_SUBTYPEID);

        public const string DAWNDROPSIGNALLITE_SUBTYPEID = "DAWNDropSignalLite";
        public static readonly UniqueEntityId DAWNDROPSIGNALLITE_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), DAWNDROPSIGNALLITE_SUBTYPEID);

        public const string DAWNDROPSIGNALSURVIVAL_SUBTYPEID = "DAWNDropSignalSurvival";
        public static readonly UniqueEntityId DAWNDROPSIGNALSURVIVAL_ID = new UniqueEntityId(typeof(MyObjectBuilder_ConsumableItem), DAWNDROPSIGNALSURVIVAL_SUBTYPEID);

        public const string AK1EXPLORERROVER_SUBTYPEID = "AK1ExplorerRover";
        public const string AK2CARGOROVER_SUBTYPEID = "AK2CargoRover";
        public const string AK3DROPPOD_SUBTYPEID = "AK3DropPod";

        public static DropSignalConsumableHandler DAWNDROPSIGNALEXPLORER_HANDLER = new DropSignalConsumableHandler()
        {
            Id = DAWNDROPSIGNALEXPLORER_ID,
            PrefabName = AK1EXPLORERROVER_SUBTYPEID,
            DisplayName = "AK-1 Explorer Rover"
        };

        public static DropSignalConsumableHandler DAWNDROPSIGNALLITE_HANDLER = new DropSignalConsumableHandler()
        {
            Id = DAWNDROPSIGNALLITE_ID,
            PrefabName = AK2CARGOROVER_SUBTYPEID,
            DisplayName = "AK-2 Cargo Rover"
        };

        public static DropSignalConsumableHandler DAWNDROPSIGNALSURVIVAL_HANDLER = new DropSignalConsumableHandler()
        {
            Id = DAWNDROPSIGNALSURVIVAL_ID,
            PrefabName = AK3DROPPOD_SUBTYPEID,
            DisplayName = "AK-3 Drop Pod"
        };

        public static readonly Dictionary<UniqueEntityId, ServerConsumableHandler> CONSUMABLE_HANDLERS = new Dictionary<UniqueEntityId, ServerConsumableHandler>()
        {
            { DAWNDROPSIGNALEXPLORER_ID, DAWNDROPSIGNALEXPLORER_HANDLER },
            { DAWNDROPSIGNALLITE_ID, DAWNDROPSIGNALLITE_HANDLER },
            { DAWNDROPSIGNALSURVIVAL_ID, DAWNDROPSIGNALSURVIVAL_HANDLER }
        };

        private static ConcurrentDictionary<UniqueEntityId, MyObjectBuilder_Base> BUILDERS_CACHE = new ConcurrentDictionary<UniqueEntityId, MyObjectBuilder_Base>();

        public static T GetBuilder<T>(UniqueEntityId id, bool cache = true) where T : MyObjectBuilder_Base
        {
            if (cache && BUILDERS_CACHE.ContainsKey(id))
                return BUILDERS_CACHE[id] as T;
            var builder = MyObjectBuilderSerializer.CreateNewObject(id.DefinitionId) as T;
            BUILDERS_CACHE[id] = builder;
            return builder as T;
        }

        public static MyObjectBuilder_PhysicalObject GetPhysicalObjectBuilder(UniqueEntityId id)
        {
            return GetBuilder<MyObjectBuilder_PhysicalObject>(id);
        }

        public static void Init()
        {
            Logging.Instance.LogInfo(typeof(GameWatcherController), "Added Watcher to OnItemConsumed");
            MyPlayerCollection.OnItemConsumed += MyPlayerCollection_OnItemConsumed;
        }

        public static void Dispose()
        {
            MyPlayerCollection.OnItemConsumed -= MyPlayerCollection_OnItemConsumed;
        }

        private static void MyPlayerCollection_OnItemConsumed(MyCharacter character, MyDefinitionId consumedItem)
        {
            Logging.Instance.LogInfo(typeof(ServerConsumablesController), $"Item {consumedItem} got consumed!");
            var itemid = new UniqueEntityId(consumedItem);
            if (CONSUMABLE_HANDLERS.ContainsKey(itemid))
            {
                Logging.Instance.LogInfo(typeof(ServerConsumablesController), $"Handler found!");
                if (!CONSUMABLE_HANDLERS[itemid].OnConsume.Invoke(character))
                {
                    Logging.Instance.LogWarning(typeof(ServerConsumablesController), $"Item {consumedItem} got a false return from handler!");
                    var inv = character.GetInventory();
                    if (inv != null)
                    {
                        inv.AddItems(1, GetPhysicalObjectBuilder(new UniqueEntityId(consumedItem)));
                    }
                }
            }
        }

    }
}
