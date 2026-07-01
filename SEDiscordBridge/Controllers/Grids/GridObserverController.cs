using ParallelTasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Torch.Server;
using VRage.Game.Entity;
using VRage.Noise.Combiners;

namespace SEDiscordBridge.Controllers.Grids
{

    public static class GridObserverController
    {

        private static readonly ConcurrentDictionary<long, MyCubeGrid> GRIDS = new ConcurrentDictionary<long, MyCubeGrid>();
        private static readonly ConcurrentDictionary<long, MyCubeGridExtraData> GRID_EXTRADATA = new ConcurrentDictionary<long, MyCubeGridExtraData>();

        private static bool _initialized = false;
        public static void Init()
        {
            if (_initialized)
            {
                Dispose();
            }
            Logging.Instance.LogInfo(typeof(GridObserverController), "Do Initial Load Entities");
            DoInitialLoadEntities();
            Logging.Instance.LogInfo(typeof(GridObserverController), "Added Watcher to MyEntities OnEntityAdd");
            MyEntities.OnEntityAdd += Entities_OnEntityAdd;
            Logging.Instance.LogInfo(typeof(GridObserverController), "Added Watcher to MyEntities OnEntityRemove");
            MyEntities.OnEntityRemove += Entities_OnEntityRemove;
            _initialized = true;
        }

        public static void Dispose()
        {
            MyEntities.OnEntityAdd -= Entities_OnEntityAdd;
            MyEntities.OnEntityRemove -= Entities_OnEntityRemove;
            _initialized = false;
        }

        public static IEnumerable<MyCubeGridExtraData> GetGridsExtraData()
        {
            return GRID_EXTRADATA.Values;
        }

        public static List<long> GetPlayerGrids(long playerId)
        {
            List<long> playerGrids = new List<long>();
            foreach (var grid in GRIDS.Values)
            {
                if (grid.BigOwners.Contains(playerId) || grid.SmallOwners.Contains(playerId))
                {
                    playerGrids.Add(grid.EntityId);
                }
            }
            return playerGrids;
        }

        public static float GetPlayerThreatLevel(long playerId)
        {
            float threatLevel = 0f;
            foreach (var grid in GetPlayerGrids(playerId))
            {
                if (GRID_EXTRADATA.TryGetValue(grid, out var extraData))
                {
                    threatLevel += extraData.ThreatLevel;
                }
            }
            return threatLevel;
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

        private static void Entities_OnEntityAdd(MyEntity entity)
        {
            try
            {
                if (entity is MyCubeGrid grid)
                {
                    GRIDS[grid.EntityId] = grid;
                    GRID_EXTRADATA[grid.EntityId] = new MyCubeGridExtraData(grid);
                    return;
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GridObserverController), e);
            }
        }

        private static void Entities_OnEntityRemove(MyEntity entity)
        {
            try
            {
                if (entity is MyCubeGrid grid)
                {
                    if (GRIDS.ContainsKey(grid.EntityId))
                    {
                        GRIDS.TryRemove(grid.EntityId, out _);
                    }
                    if (GRID_EXTRADATA.ContainsKey(grid.EntityId))
                    {
                        GRID_EXTRADATA.TryRemove(grid.EntityId, out _);
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GridObserverController), e);
            }
        }

    }

}
