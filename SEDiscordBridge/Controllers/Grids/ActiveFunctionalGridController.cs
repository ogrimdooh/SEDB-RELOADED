using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using System.Collections.Concurrent;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.Library.Utils;

namespace SEDiscordBridge.Controllers.Grids
{
    public static class ActiveFunctionalGridController
    {
        public static ConcurrentDictionary<long, BaseFunctionalGridController> Controllers = new ConcurrentDictionary<long, BaseFunctionalGridController>();

        public static void RegisterController(long gridId, BaseFunctionalGridController controller)
        {
            Controllers[gridId] = controller;
            Logging.Instance.LogInfo(typeof(ActiveFunctionalGridController), $"RegisterController called : gridId={gridId} controller={controller.GetType().Name} | End count = {Controllers.Count}");
        }

        public static long GetGridIdByContractBlockId(long blockId)
        {
            var gridId = Controllers.Where(c => c.Value.ARKGRIDCONTRACTBLOCK != null && c.Value.ARKGRIDCONTRACTBLOCK.EntityId == blockId)
                .Select(c => c.Key)
                .FirstOrDefault();
            return gridId;
        }

        public static BaseFunctionalGridController GetRandomFriendlyStation(long currentGridId)
        {
            var friendlyStations = Controllers.Where(c => c.Key != currentGridId)
                .OrderBy(x => MyRandom.Instance.NextFloat())
                .Select(x => x.Value)
                .FirstOrDefault();
            return friendlyStations;
        }

        public static bool RepairAllGridBlocks(MyCubeGrid grid)
        {
            var hadRepair = false;
            foreach (var item in grid.CubeBlocks)
            {
                if (item.BuildLevelRatio < 1f)
                {
                    item.SetIntegrity(item.MaxIntegrity, item.MaxIntegrity, VRage.Game.ModAPI.MyIntegrityChangeEnum.Repair, item.OwnerId);
                    hadRepair = true;
                }
                if (item.HasDeformation)
                {
                    item.DeformationRatio = 0f;
                    hadRepair = true;
                }
                if (hadRepair)
                {
                    item.UpdateVisual();
                    item.UpgradeBuildLevel();
                    item.ResumeDamageEffect();
                    item.CubeGrid.ResetBlockSkeleton(item, true);
                    grid.SetBlockDirty(item);
                    grid.RenderData.RemoveDecals(item.Position);
                    grid.SendIntegrityChanged(item, MyIntegrityChangeEnum.ConstructionEnd, 0L);
                    grid.OnIntegrityChanged(item, false);
                }
            }
            return hadRepair;
        }
    }
}
