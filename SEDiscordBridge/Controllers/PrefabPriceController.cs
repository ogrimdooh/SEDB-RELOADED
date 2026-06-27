using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Entities.Base;
using SEDiscordBridge.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRageMath;

namespace SEDiscordBridge.Controllers
{
    public static class PrefabPriceController
    {

        public class StationPrefabItem
        {

            public string PrefabName { get; set; }
            public MyPrefabDefinition Definition { get; set; }

            public bool IsLoaded { get; set; }
            public StationPrefabFlag Flags { get; set; }
            public StationPrefabCategory Category { get; set; }
            public long BlockCount { get; set; }
            public long TotalPCU { get; set; }

            public bool IsValid { get; set; }
            public float BaseValue { get; set; }
            public float AssembleTime { get; set; }

            public bool NeedRepair { get; set; }
            public long NotCompletedBlocksCount { get; set; }
            public float RepairCost { get; set; }
            public float RepairTime { get; set; }

        }

        public struct StationPrefabFilter
        {

            public StationPrefabFlag excludeFlags;
            public StationPrefabFlag requiredFlags;
            public StationPrefabCategory validCategories;

        }

        public static readonly Dictionary<KeyValuePair<StationType, StationLevel>, StationPrefabFilter> STATION_PREFAB_FILTERS = new Dictionary<KeyValuePair<StationType, StationLevel>, StationPrefabFilter>
        {
            {
                new KeyValuePair<StationType, StationLevel>(StationType.PlanetStation, StationLevel.Small),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.TinyToMedium,
                    excludeFlags = StationPrefabFlag.Reactor | StationPrefabFlag.JumpDrive
                }
            },
            {
                new KeyValuePair<StationType, StationLevel>(StationType.PlanetStation, StationLevel.Medium),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.TinyToMedium,
                    excludeFlags = StationPrefabFlag.JumpDrive
                }
            },
            {
                new KeyValuePair<StationType, StationLevel>(StationType.PlanetStation, StationLevel.Large),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.All
                }
            },
            {
                new KeyValuePair<StationType, StationLevel>(StationType.OrbitalStation, StationLevel.Small),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.TinyToMedium,
                    excludeFlags = StationPrefabFlag.Rover | StationPrefabFlag.Reactor | StationPrefabFlag.JumpDrive
                }
            },
            {
                new KeyValuePair<StationType, StationLevel>(StationType.OrbitalStation, StationLevel.Medium),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.TinyToMedium,
                    excludeFlags = StationPrefabFlag.Rover | StationPrefabFlag.JumpDrive
                }
            },
            {
                new KeyValuePair<StationType, StationLevel>(StationType.OrbitalStation, StationLevel.Large),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.All,
                    excludeFlags = StationPrefabFlag.Rover
                }
            },
            {
                new KeyValuePair<StationType, StationLevel>(StationType.AsteroidFieldStation, StationLevel.Small),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.TinyToMedium,
                    excludeFlags = StationPrefabFlag.Rover | StationPrefabFlag.Reactor | StationPrefabFlag.JumpDrive
                }
            },
            {
                new KeyValuePair<StationType, StationLevel>(StationType.AsteroidFieldStation, StationLevel.Medium),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.TinyToMedium,
                    excludeFlags = StationPrefabFlag.Rover | StationPrefabFlag.JumpDrive
                }
            },
            {
                new KeyValuePair<StationType, StationLevel>(StationType.AsteroidFieldStation, StationLevel.Large),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.All,
                    excludeFlags = StationPrefabFlag.Rover
                }
            },
            {
                new KeyValuePair<StationType, StationLevel>(StationType.DeepSpaceStation, StationLevel.Small),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.TinyToMedium,
                    excludeFlags = StationPrefabFlag.Rover | StationPrefabFlag.Reactor | StationPrefabFlag.JumpDrive
                }
            },
            {
                new KeyValuePair<StationType, StationLevel>(StationType.DeepSpaceStation, StationLevel.Medium),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.TinyToMedium,
                    excludeFlags = StationPrefabFlag.Rover | StationPrefabFlag.JumpDrive
                }
            },
            {
                new KeyValuePair<StationType, StationLevel>(StationType.DeepSpaceStation, StationLevel.Large),
                new StationPrefabFilter()
                {
                    validCategories = StationPrefabCategory.All,
                    excludeFlags = StationPrefabFlag.Rover
                }
            }
        };

        public static readonly ConcurrentDictionary<string, StationPrefabItem> LOADED_PREFABS = new ConcurrentDictionary<string, StationPrefabItem>();

        public static bool AddPrefabToShop(string prefabName, out StationPrefabItem prefabItem)
        {
            prefabItem = null;
            if (!LOADED_PREFABS.ContainsKey(prefabName))
            {
                var def = MyDefinitionManager.Static.GetPrefabDefinition(prefabName);
                if (def != null)
                {
                    prefabItem = new StationPrefabItem()
                    {
                        PrefabName = prefabName,
                        Definition = def
                    };
                    LOADED_PREFABS[prefabName] = prefabItem;
                }
                else
                {
                    Logging.Instance.LogWarning(typeof(PrefabPriceController), $"AddPrefabToShop: Prefab {prefabName} has no definition.");
                    return false;
                }
            }
            else
            {
                Logging.Instance.LogWarning(typeof(PrefabPriceController), $"AddPrefabToShop: Prefab {prefabName} already registered.");
                prefabItem = LOADED_PREFABS[prefabName];
            }
            if (!prefabItem.IsLoaded)
            {
                foreach (var grid in prefabItem.Definition.CubeGrids)
                {
                    switch (grid.GridSizeEnum)
                    {
                        case MyCubeSize.Large:
                            prefabItem.Flags |= StationPrefabFlag.LargeGrid;
                            break;
                        case MyCubeSize.Small:
                            prefabItem.Flags |= StationPrefabFlag.SmallGrid;
                            break;
                    }
                    prefabItem.BlockCount += grid.CubeBlocks.Count;
                    prefabItem.TotalPCU += grid.CubeBlocks.Sum(x => MyDefinitionManager.Static.GetCubeBlockDefinition(x.GetId())?.PCU ?? 0);
                    if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_Reactor)))
                        prefabItem.Flags |= StationPrefabFlag.Reactor;
                    if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_JumpDrive)))
                        prefabItem.Flags |= StationPrefabFlag.JumpDrive;
                    if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_MotorSuspension)))
                        prefabItem.Flags |= StationPrefabFlag.Rover;
                    if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_Thrust)))
                    {
                        if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_Thrust) && IsAtmThruster(MyDefinitionManager.Static.GetCubeBlockDefinition(x.GetId()))))
                            prefabItem.Flags |= StationPrefabFlag.AtmThruster;
                        if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_Thrust) && IsH2Thruster(MyDefinitionManager.Static.GetCubeBlockDefinition(x.GetId()))))
                            prefabItem.Flags |= StationPrefabFlag.H2Thruster;
                        if (grid.CubeBlocks.Any(x => x.TypeId == typeof(MyObjectBuilder_Thrust) && IsIonThruster(MyDefinitionManager.Static.GetCubeBlockDefinition(x.GetId()))))
                            prefabItem.Flags |= StationPrefabFlag.IonThruster;
                    }
                }
                if (prefabItem.Flags.IsFlagSet(StationPrefabFlag.SmallGrid) && !prefabItem.Flags.IsFlagSet(StationPrefabFlag.LargeGrid))
                {
                    if (prefabItem.BlockCount > 3000)
                        prefabItem.Category = StationPrefabCategory.Huge;
                    else if (prefabItem.BlockCount >= 1000 && prefabItem.BlockCount < 3000)
                        prefabItem.Category = StationPrefabCategory.Big;
                    else if (prefabItem.BlockCount >= 500 && prefabItem.BlockCount < 1000)
                        prefabItem.Category = StationPrefabCategory.Medium;
                    else if (prefabItem.BlockCount >= 250 && prefabItem.BlockCount < 500)
                        prefabItem.Category = StationPrefabCategory.Small;
                    else
                        prefabItem.Category = StationPrefabCategory.Tiny;
                }
                else
                {
                    if (prefabItem.BlockCount > 1500)
                        prefabItem.Category = StationPrefabCategory.Huge;
                    else if (prefabItem.BlockCount >= 750 && prefabItem.BlockCount < 1500)
                        prefabItem.Category = StationPrefabCategory.Big;
                    else if (prefabItem.BlockCount >= 300 && prefabItem.BlockCount < 750)
                        prefabItem.Category = StationPrefabCategory.Medium;
                    else if (prefabItem.BlockCount >= 100 && prefabItem.BlockCount < 300)
                        prefabItem.Category = StationPrefabCategory.Small;
                    else
                        prefabItem.Category = StationPrefabCategory.Tiny;
                }
                prefabItem.IsValid = prefabItem.Flags.IsFlagSet(StationPrefabFlag.Rover) ||
                    prefabItem.Flags.IsFlagSet(StationPrefabFlag.AtmThruster) ||
                    prefabItem.Flags.IsFlagSet(StationPrefabFlag.H2Thruster) ||
                    prefabItem.Flags.IsFlagSet(StationPrefabFlag.IonThruster);
                prefabItem.NotCompletedBlocksCount = 0;
                prefabItem.RepairCost = 0;
                prefabItem.RepairTime = 0;
                prefabItem.AssembleTime = 0;
                foreach (var grid in prefabItem.Definition.CubeGrids)
                {
                    foreach (var block in grid.CubeBlocks)
                    {
                        if (block.TypeId == typeof(MyObjectBuilder_BatteryBlock))
                        {
                            var blockDef = block as MyObjectBuilder_BatteryBlock;
                            if (blockDef != null)
                            {
                                if (_blockMaxStoredPower.ContainsKey(block.GetId()))
                                {
                                    blockDef.CurrentStoredPower = _blockMaxStoredPower[block.GetId()];
                                }
                                else
                                {
                                    var def = MyDefinitionManager.Static.GetCubeBlockDefinition(block.GetId()) as MyBatteryBlockDefinition;
                                    if (def != null)
                                    {
                                        _blockMaxStoredPower[block.GetId()] = def.MaxStoredPower;
                                        blockDef.CurrentStoredPower = _blockMaxStoredPower[block.GetId()];
                                    }
                                }
                            }
                        }
                        var blockCost = GetCubeBlockBaseValue(block.GetId(), out float timeToBuild);
                        if (block.BuildPercent < 1f && block.BuildPercent >= 0)
                        {
                            prefabItem.NotCompletedBlocksCount++;
                            prefabItem.RepairCost += blockCost * (1f - block.BuildPercent);
                            prefabItem.RepairTime += timeToBuild * (1f - block.BuildPercent);
                        }
                        prefabItem.BaseValue += blockCost;
                        prefabItem.AssembleTime += timeToBuild;
                    }
                }
                prefabItem.NeedRepair = prefabItem.NotCompletedBlocksCount > 0;
                prefabItem.IsLoaded = true;
                Logging.Instance.LogInfo(typeof(PrefabPriceController), $"AddPrefabToShop: {prefabName} : BASE VALUE = {prefabItem.BaseValue} : REPAIR VALUE = {prefabItem.RepairCost}");
            }
            return true;
        }

        public static bool CalcGridValue(MyCubeGrid grid, out float baseValue, out float repairValue, out float timeToRepair, out int damagedBlocks, out int deformedBlocks)
        {
            damagedBlocks = 0;
            deformedBlocks = 0;
            baseValue = 0f;
            repairValue = 0f;
            timeToRepair = 0f;
            if (grid != null)
            {
                foreach (var block in grid.CubeBlocks)
                {
                    var value = GetCubeBlockBaseValue(block.BlockDefinition.Id, out float timeToBuild);
                    baseValue += value;
                    if (block.BuildLevelRatio != 1f)
                    {
                        damagedBlocks++;
                        repairValue += value * (1f - block.BuildLevelRatio);
                        timeToRepair += timeToBuild * (1f - block.BuildLevelRatio);
                    }
                    if (block.HasDeformation)
                    {
                        deformedBlocks++;
                        repairValue += value * 0.025f;
                        timeToRepair += timeToBuild * 0.025f;
                    }
                }
                return true;
            }
            return false;
        }

        private static readonly ConcurrentDictionary<MyDefinitionId, float> _blockMaxStoredPower = new ConcurrentDictionary<MyDefinitionId, float>();
        private static readonly ConcurrentDictionary<MyDefinitionId, Vector2> _blockValues = new ConcurrentDictionary<MyDefinitionId, Vector2>();
        public static float GetCubeBlockBaseValue(MyDefinitionId blockId, out float timeToBuild)
        {
            if (_blockValues.ContainsKey(blockId))
            {
                timeToBuild = _blockValues[blockId].Y;
                return _blockValues[blockId].X;
            }
            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(blockId);
            float value = 0;
            if (def != null)
            {
                var components = def.Components.GroupBy(x => x.Definition.Id).ToDictionary(x => x.Key, x => x.Sum(y => y.Count));
                foreach (var compId in components.Keys)
                {
                    var itemCalculated = ItemPriceController.GetItemInfo(new UniqueEntityId(compId));
                    if (itemCalculated != null)
                    {
                        value += itemCalculated.BaseValue * components[compId];
                    }
                }
            }
            _blockValues[blockId] = new Vector2(Math.Max(value, 1), def.MaxIntegrity / def.IntegrityPointsPerSec);
            timeToBuild = _blockValues[blockId].Y;
            return _blockValues[blockId].X;
        }

        private static bool IsAtmThruster(MyCubeBlockDefinition blockDef)
        {
            var thrusterDef = blockDef as MyThrustDefinition;
            if (thrusterDef != null)
            {
                var useFuel = thrusterDef.FuelConverter != null && !thrusterDef.FuelConverter.FuelId.IsNull();
                return thrusterDef.NeedsAtmosphereForInfluence && !useFuel;
            }
            return false;
        }

        private static bool IsH2Thruster(MyCubeBlockDefinition blockDef)
        {
            var thrusterDef = blockDef as MyThrustDefinition;
            if (thrusterDef != null)
            {
                return thrusterDef.FuelConverter != null && !thrusterDef.FuelConverter.FuelId.IsNull();
            }
            return false;
        }

        private static bool IsIonThruster(MyCubeBlockDefinition blockDef)
        {
            var thrusterDef = blockDef as MyThrustDefinition;
            if (thrusterDef != null)
            {
                var useFuel = thrusterDef.FuelConverter != null && !thrusterDef.FuelConverter.FuelId.IsNull();
                return !thrusterDef.NeedsAtmosphereForInfluence && !useFuel;
            }
            return false;
        }

    }
}
