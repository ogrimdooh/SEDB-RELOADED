using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Contracts;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SEDiscordBridge.Controllers;
using SEDiscordBridge.Controllers.Grids;
using SEDiscordBridge.Extensions;
using SEDiscordBridge.Patches;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using static SEDiscordBridge.PatchController;

namespace SEDiscordBridge.Patches
{
    [PatchingClass]
    public class MyContractSalvagePatch
    {

        private static SEDiscordBridgePlugin Plugin;

        public MyContractSalvagePatch(SEDiscordBridgePlugin plugin)
        {
            Plugin = plugin;
        }

        [PostFixMethod]
        [TargetMethod(Type = typeof(MyContractSalvage), Method = "OnGridSpawned")]
        public static void OnGridSpawned(List<MyCubeGrid> spawnedGrids, MyContractSalvage __instance)
        {
            foreach (var grid in spawnedGrids)
            {
                Logging.Instance.LogInfo(typeof(MyContractSalvage), $"OnGridSpawned got executed for grid {grid.EntityId}!");
                var validTypes = new HashSet<MyObjectBuilderType>
                {
                    typeof(MyObjectBuilder_CargoContainer),
                    typeof(MyObjectBuilder_ShipConnector)
                };
                var targetBlock = grid.Inventories
                    .Where(x =>
                        validTypes.Contains(x.BlockDefinition.Id.TypeId) &&
                        x.IsFunctional &&
                        x.GetInventory().ItemCount == 0
                    )
                    .OrderByDescending(x => (float)x.GetInventory().MaxVolume)
                    .FirstOrDefault();
                if (targetBlock != null)
                {
                    var faction = grid.TryGetFaction();
                    var extraInfo = GridObserverController.GetGridExtraData(grid.EntityId);
                    var oreInfo = LootConstants.GetAllOres(faction?.Tag, extraInfo?.ThreatLevel ?? 0);
                    Logging.Instance.LogInfo(typeof(MyContractSalvage), $"OnGridSpawned: Found target block {targetBlock.EntityId} for grid {grid.EntityId} with faction {faction?.Tag} and threat level {extraInfo?.ThreatLevel}. Ore info: {string.Join(", ", oreInfo.Select(x => $"{x.Key}: Chance={x.Value.Chance}, MaxVolume={x.Value.MaxVolume}, MaxTypes={x.Value.MaxTypes}"))}");
                    var inventory = targetBlock.GetInventory();
                    var freeVolume = (float)(inventory.MaxVolume - inventory.CurrentVolume);
                    if (freeVolume > 0)
                    {
                        var targetLoot = LootConstants.GetRandomOresToLoot(oreInfo);
                        foreach (var oreType in targetLoot)
                        {
                            var totalTypes = oreInfo[oreType].MaxTypes.GetRandomInt();
                            var targetVolume = freeVolume * oreInfo[oreType].MaxVolume;
                            var slotVolume = targetVolume / totalTypes;
                            var lootTable = LootConstants.GetLootTable(oreType);
                            for (int i = 0; i < totalTypes; i++)
                            {
                                var chance = MyUtils.GetRandomFloat(0, 1);
                                var item = lootTable.FirstOrDefault(x => x.Chance.X <= chance && x.Chance.Y >= chance);
                                if (item != null)
                                {
                                    var iDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(item.Id.DefinitionId);
                                    if (iDef != null)
                                    {
                                        var itemTargetAmount = slotVolume / iDef.Volume;
                                        inventory.AddItems((MyFixedPoint)itemTargetAmount, ItensConstants.GetPhysicalObjectBuilder(item.Id));
                                        Logging.Instance.LogInfo(typeof(MyContractSalvage), $"OnGridSpawned: Added {itemTargetAmount} of {iDef.Id} to inventory of block {targetBlock.EntityId} in grid {grid.EntityId}.");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

    }
    [PatchingClass]
    public class MyContractGridHaulingPatch
    {

        private static SEDiscordBridgePlugin Plugin;

        public MyContractGridHaulingPatch(SEDiscordBridgePlugin plugin)
        {
            Plugin = plugin;
        }

        [PostFixMethod]
        [TargetMethod(Type = typeof(MyContractGridHauling), Method = "IsGridInEndStationSafeZone")]
        public static void IsGridInEndStationSafeZone(MyCubeGrid grid, MyContractGridHauling __instance, ref bool __result)
        {
            Logging.Instance.LogInfo(typeof(MyContractGridHauling), $"IsGridInEndStationSafeZone got executed!");

            if (__instance.ContractCondition.StationEndId == 0 && __instance.ContractCondition.BlockEndId != 0)
            {
                var stationId = ActiveFunctionalGridController.GetGridIdByContractBlockId(__instance.ContractCondition.BlockEndId);
                if (stationId == 0)
                {
                    Logging.Instance.LogWarning(typeof(MyContractGridHauling), $"IsGridInEndStationSafeZone: Could not find station for block {__instance.ContractCondition.BlockEndId}.");
                    __result = false;
                    return;
                }
                var station = ActiveFunctionalGridController.Controllers[stationId];

                double num = Vector3D.DistanceSquared(grid.PositionComp.GetPosition(), station.ARKGRID.PositionComp.GetPosition());
                float num2 = MyFactionStation.SAFEZONE_SIZE * MyFactionStation.SAFEZONE_SIZE;
                __result = num <= (double)num2;

            }
        }

    }

}
