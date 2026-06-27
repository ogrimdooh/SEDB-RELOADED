using Sandbox.Definitions;
using Sandbox.Game.Contracts;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using SEDiscordBridge.Controllers.Grids;
using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game.Definitions.SessionComponents;
using VRage.Game.ObjectBuilders.Components.Contracts;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace SEDiscordBridge.Controllers.Economics
{
    public class MyCustomContract_TypeGridHaulingStrategy : MyCustomContract_Base
    {
        private const int POSITION_FIND_ATTEMPTS = 10;

        private const float MAX_GRID_RADIUS = 50f;

        public MyCustomContract_TypeGridHaulingStrategy(MySessionComponentEconomyDefinition economyDefinition, MyContractTypeDefinition contractTypeDefinition)
            : base(economyDefinition, contractTypeDefinition)
        {
        }

        public override bool CanBeGenerated(StationType stationType, MyFaction faction)
        {
            if (!(m_contractTypeDefinition is MyContractTypeGridHaulingDefinition myContractTypeGridHaulingDefinition))
            {
                return false;
            }

            return stationType != StationType.PlanetStation && 
                myContractTypeGridHaulingDefinition.GridPrefabs != null && 
                myContractTypeGridHaulingDefinition.GridPrefabs.Count > 0;
        }

        public override MyContractCreationResults GenerateContract(out MyContract contract, StationType stationType, Vector3D position, long factionId, long blockId, MyMinimalPriceCalculator calculator, MyTimeSpan now, params Tuple<string, object>[] customData)
        {
            MyFactionCollection factions = MySession.Static.Factions;
            contract = null;
            
            var blockGridId = ActiveFunctionalGridController.GetGridIdByContractBlockId(blockId);
            var friendlyStation = ActiveFunctionalGridController.GetRandomFriendlyStation(blockGridId);

            if (friendlyStation == null)
            {
                Logging.Instance.LogWarning(GetType(), $"GenerateContract: No friendly station found for grid {blockGridId}.");
                return MyContractCreationResults.Fail_Impossible;
            }

            double num = (friendlyStation.ARKGRID.PositionComp.GetPosition() - position).Length();

            if (!(m_contractTypeDefinition is MyContractTypeGridHaulingDefinition myContractTypeGridHaulingDefinition))
            {
                return MyContractCreationResults.Error;
            }

            float rewardMultiplier = myContractTypeGridHaulingDefinition.RiskSettings.RewardMultiplier;
            float num2 = 1f;
            string prefabName = null;
            if (myContractTypeGridHaulingDefinition.GridPrefabs != null && myContractTypeGridHaulingDefinition.GridPrefabs.Count > 0)
            {
                MyGridPrefabEntry myGridPrefabEntry = myContractTypeGridHaulingDefinition.GridPrefabs
                    .Where(x => !string.IsNullOrWhiteSpace(x.PrefabName))
                    .OrderBy(x => MyRandom.Instance.NextFloat())
                    .FirstOrDefault();
                prefabName = myGridPrefabEntry.PrefabName;
                num2 = myGridPrefabEntry.WeightMultiplier;
            }

            if (string.IsNullOrWhiteSpace(prefabName) || !PrefabPriceController.AddPrefabToShop(prefabName, out var prefabItem))
            {
                return MyContractCreationResults.Fail_Common;
            }

            if (MySession.Static.NPCBlockLimits.Pirate.PCU < prefabItem.TotalPCU)
            {
                return MyContractCreationResults.Fail_Common;
            }

            long rewardMoney = (long)((float)MyContractTypeBaseStrategy.GetHaulingMoneyReward(myContractTypeGridHaulingDefinition.MinimumMoney, num, MyContractTypeBaseStrategy.GetUraniumPrice(calculator)) * rewardMultiplier * num2) / 1000 * 1000;
            rewardMoney = (long)(rewardMoney * new Vector2(0.25f, 0.75f).GetRandom());
            MyObjectBuilder_ContractGridHauling myObjectBuilder_ContractGridHauling = CreateCommonOb<MyObjectBuilder_ContractGridHauling>(m_contractTypeDefinition, MyEntityIdentifier.AllocateId(MyEntityIdentifier.ID_OBJECT_TYPE.CONTRACT), factionId, blockId, rewardMoney, stationType == StationType.DeepSpaceStation);
            myObjectBuilder_ContractGridHauling.DeliverDistance = num;
            myObjectBuilder_ContractGridHauling.RemainingTimeInS = GetDuration(myContractTypeGridHaulingDefinition, num).Seconds;
            myObjectBuilder_ContractGridHauling.GridPosition = FindSpawnPositionNearStation(position, myContractTypeGridHaulingDefinition.SpawnDistanceFromStation);
            myObjectBuilder_ContractGridHauling.PrefabName = prefabName;
            myObjectBuilder_ContractGridHauling.WeightMultiplier = num2;
            myObjectBuilder_ContractGridHauling.IsPlanetaryDestination = friendlyStation.GetStationType() == StationType.PlanetStation;
            MyObjectBuilder_ContractConditionDeliverGrid contractCondition = new MyObjectBuilder_ContractConditionDeliverGrid
            {
                Id = MyEntityIdentifier.AllocateId(MyEntityIdentifier.ID_OBJECT_TYPE.CONTRACT_CONDITION),
                ContractId = myObjectBuilder_ContractGridHauling.Id,
                FactionEndId = factionId,
                StationEndId = 0L,
                BlockEndId = friendlyStation.ARKGRIDCONTRACTBLOCK.EntityId,
                SubId = 0,
                IsFinished = false
            };
            myObjectBuilder_ContractGridHauling.ContractCondition = contractCondition;
            myObjectBuilder_ContractGridHauling.ContractTypeDefinitionId = m_contractTypeDefinition.Id;
            MyContractGridHauling myContractGridHauling = new MyContractGridHauling();
            myContractGridHauling.Init(myObjectBuilder_ContractGridHauling);
            contract = myContractGridHauling;
            return MyContractCreationResults.Success;
        }

        private Vector3D FindSpawnPositionNearStation(Vector3D position, float spawnDistanceFromStation)
        {
            float num = MyFactionStation.SAFEZONE_SIZE + spawnDistanceFromStation;
            for (int i = 0; i < 10; i++)
            {
                Vector3D vector3D = MyUtils.GetRandomVector3Normalized();
                Vector3D vector3D2 = position + vector3D * num;
                List<MyObjectSeed> list = new List<MyObjectSeed>();
                MyProceduralWorldGenerator.Static?.OverlapAllAsteroidAndEncountersInSphere(new BoundingSphereD(vector3D2, 50.0), list);
                if (list.Count <= 0)
                {
                    return vector3D2;
                }
            }

            Vector3D vector3D3 = MyUtils.GetRandomVector3Normalized();
            return position + vector3D3 * num;
        }

        public static MyTimeSpan GetDuration(MyContractTypeGridHaulingDefinition def, double distanceInMeters)
        {
            return MyTimeSpan.FromSeconds((double)def.DurationBaseTime + (double)def.DurationTimePerMeter * distanceInMeters);
        }

        protected override int GetRewardRep(MyContractTypeDefinition def)
        {
            return def.MinimumReputation;
        }
    }

}
