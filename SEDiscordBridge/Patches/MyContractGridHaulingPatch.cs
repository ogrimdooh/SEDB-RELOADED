using Sandbox.Game.Contracts;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using SEDiscordBridge.Controllers.Grids;
using VRageMath;
using static SEDiscordBridge.PatchController;

namespace SEDiscordBridge.Patches
{
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
