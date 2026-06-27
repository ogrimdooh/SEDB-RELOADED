using Sandbox.Definitions;
using Sandbox.Game.Contracts;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using SEDiscordBridge.Controllers.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Definitions.SessionComponents;
using VRage.Game.ObjectBuilders.Components.Contracts;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace SEDiscordBridge.Controllers.Economics
{
    public class MyCustomContract_TypeSalvageStrategy : MyCustomContract_Base
    {
        private static readonly float GRAVITY_SQUARED_EPSILON = 0.0001f;

        public MyCustomContract_TypeSalvageStrategy(MySessionComponentEconomyDefinition economyDefinition, MyContractTypeDefinition contractTypeDefinition)
            : base(economyDefinition, contractTypeDefinition)
        {
        }

        public override MyContractCreationResults GenerateContract(out MyContract contract, StationType stationType, Vector3D position, long factionId, long blockId, MyMinimalPriceCalculator calculator, MyTimeSpan now, params Tuple<string, object>[] customData)
        {
            MyFactionCollection factions = MySession.Static.Factions;
            contract = null;

            if (!(m_contractTypeDefinition is MyContractTypeSalvageDefinition myContractTypeSalvageDefinition))
            {
                return MyContractCreationResults.Error;
            }

            if (myContractTypeSalvageDefinition.SpawnGroups == null || myContractTypeSalvageDefinition.SpawnGroups.Count == 0)
            {
                return MyContractCreationResults.Fail_Common;
            }

            Vector3D vector3D = Vector3D.Zero;
            long planetEntityId = 0L;
            string prefabName = null;
            string spawnGroupSubtypeId = null;
            if (myContractTypeSalvageDefinition.MaxPlanetRange.HasValue)
            {
                List<MyPlanet> planets = MyPlanets.GetPlanets();
                if (planets == null || planets.Count == 0)
                {
                    return MyContractCreationResults.Fail_Common;
                }

                List<MyPlanet> list = new List<MyPlanet>();
                foreach (MyPlanet item in planets)
                {
                    if (Vector3D.Distance(position, item.PositionComp.GetPosition()) <= myContractTypeSalvageDefinition.MaxPlanetRange.Value)
                    {
                        list.Add(item);
                    }
                }

                if (list.Count == 0)
                {
                    return MyContractCreationResults.Fail_Common;
                }

                MyPlanet myPlanet = list[MyRandom.Instance.Next(0, list.Count)];
                List<MySpawnGroupDefinition> list2 = new List<MySpawnGroupDefinition>();
                foreach (string spawnGroup in myContractTypeSalvageDefinition.SpawnGroups)
                {
                    MyDefinitionId id = new MyDefinitionId(typeof(MyObjectBuilder_SpawnGroupDefinition), spawnGroup);
                    if (!(MyDefinitionManager.Static.GetDefinition(id) is MySpawnGroupDefinition mySpawnGroupDefinition) || mySpawnGroupDefinition.Prefabs.Count == 0)
                    {
                        continue;
                    }

                    List<string> planets2 = mySpawnGroupDefinition.PlanetaryInstallationSettings.Planets;
                    if (planets2 == null || planets2.Count == 0)
                    {
                        list2.Add(mySpawnGroupDefinition);
                        continue;
                    }

                    foreach (string item2 in planets2)
                    {
                        if (myPlanet.Name.Contains(item2))
                        {
                            list2.Add(mySpawnGroupDefinition);
                            break;
                        }
                    }
                }

                if (list2.Count == 0)
                {
                    return MyContractCreationResults.Fail_Common;
                }

                Vector3D planetPosition = myPlanet.PositionComp.GetPosition();
                bool flag = false;
                List<MySpawnGroupDefinition> list3 = new List<MySpawnGroupDefinition>();
                for (int i = 0; i < 40; i++)
                {
                    Vector3D vector3D2 = Vector3D.Normalize(MyUtils.GetRandomVector3D());
                    Vector3D surfacePosition = myPlanet.GetClosestSurfacePointGlobal(planetPosition + vector3D2 * myPlanet.AverageRadius);
                    if (myPlanet.IsSurfaceFlat(surfacePosition, 0.85f, out var _))
                    {
                        FilterByVoxelMaterial(list2, myPlanet, ref surfacePosition, list3);
                        if (list3.Count != 0)
                        {
                            vector3D = surfacePosition;
                            flag = true;
                            MySpawnGroupDefinition mySpawnGroupDefinition2 = list3[MyRandom.Instance.Next(0, list3.Count)];
                            spawnGroupSubtypeId = mySpawnGroupDefinition2.Id.SubtypeId.String;
                            prefabName = mySpawnGroupDefinition2.Prefabs[MyRandom.Instance.Next(0, mySpawnGroupDefinition2.Prefabs.Count)].SubtypeId;
                            break;
                        }
                    }
                }

                if (!flag)
                {
                    return MyContractCreationResults.Fail_Common;
                }

                planetEntityId = myPlanet.EntityId;
            }
            else
            {
                BoundingSphereD boundingSphereD = new BoundingSphereD(position, myContractTypeSalvageDefinition.MaxDistance);
                bool flag2 = false;
                int num = 0;
                do
                {
                    Vector3D? vector3D3 = boundingSphereD.RandomToUniformPointInSphereWithInnerCutout(MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble(), myContractTypeSalvageDefinition.MinDistance);
                    if (!vector3D3.HasValue)
                    {
                        num++;
                        continue;
                    }

                    List<MyObjectSeed> list4 = new List<MyObjectSeed>();
                    MyProceduralWorldGenerator.Static.OverlapAllAsteroidAndEncountersInSphere(new BoundingSphereD(vector3D3.Value, 100.0), list4);
                    if (list4.Count > 0)
                    {
                        num++;
                        continue;
                    }

                    if (MyGravityProviderSystem.CalculateNaturalGravityInPoint(vector3D3.Value).LengthSquared() <= GRAVITY_SQUARED_EPSILON)
                    {
                        flag2 = true;
                        vector3D = vector3D3.Value;
                        break;
                    }

                    num++;
                }
                while (num <= 20);
                if (!flag2)
                {
                    return MyContractCreationResults.Fail_Common;
                }

                string text = myContractTypeSalvageDefinition.SpawnGroups[MyRandom.Instance.Next(0, myContractTypeSalvageDefinition.SpawnGroups.Count)];
                spawnGroupSubtypeId = text;
                MyDefinitionId defId = new MyDefinitionId(typeof(MyObjectBuilder_SpawnGroupDefinition), text);
                prefabName = ((!MyDefinitionManager.Static.TryGetDefinition<MySpawnGroupDefinition>(defId, out var definition) || definition.Prefabs.Count <= 0) ? text : definition.Prefabs[MyRandom.Instance.Next(0, definition.Prefabs.Count)].SubtypeId);
            }

            if (!PrefabPriceController.AddPrefabToShop(prefabName, out var prefabItem))
            {
                return MyContractCreationResults.Fail_Common;
            }

            if (MySession.Static.NPCBlockLimits.Pirate.PCU < prefabItem.TotalPCU)
            {
                return MyContractCreationResults.Fail_Common;
            }

            double num2 = (vector3D - position).Length();
            long rewardMoney = GetMoneyRewardForSalvageContract(myContractTypeSalvageDefinition.MinimumMoney, num2, myContractTypeSalvageDefinition.DistanceRewardMultiplier) / 1000 * 1000;
            long num3 = MyEntityIdentifier.AllocateId(MyEntityIdentifier.ID_OBJECT_TYPE.CONTRACT);
            if (myContractTypeSalvageDefinition.SalvageItemIds == null || myContractTypeSalvageDefinition.SalvageItemIds.Count == 0)
            {
                return MyContractCreationResults.Fail_Common;
            }

            SerializableDefinitionId serializableDefinitionId = myContractTypeSalvageDefinition.SalvageItemIds[MyRandom.Instance.Next(0, myContractTypeSalvageDefinition.SalvageItemIds.Count)];
            MyPhysicalItemDefinition physicalItemDefinition = MyDefinitionManager.Static.GetPhysicalItemDefinition(serializableDefinitionId);
            if (physicalItemDefinition == null)
            {
                return MyContractCreationResults.Error;
            }

            MyObjectBuilder_ContractConditionDeliverItems contractCondition = new MyObjectBuilder_ContractConditionDeliverItems
            {
                Id = MyEntityIdentifier.AllocateId(MyEntityIdentifier.ID_OBJECT_TYPE.CONTRACT_CONDITION),
                ContractId = num3,
                FactionEndId = factionId,
                StationEndId = 0L,
                BlockEndId = blockId,
                SubId = 0,
                IsFinished = false,
                ItemType = serializableDefinitionId,
                ItemAmount = 1,
                ItemVolume = physicalItemDefinition.Volume,
                TransferItems = false
            };
            MyObjectBuilder_ContractSalvage myObjectBuilder_ContractSalvage = CreateCommonOb<MyObjectBuilder_ContractSalvage>(m_contractTypeDefinition, num3, factionId, blockId, rewardMoney, stationType == StationType.DeepSpaceStation);
            myObjectBuilder_ContractSalvage.GridPosition = vector3D;
            myObjectBuilder_ContractSalvage.PrefabName = prefabName;
            myObjectBuilder_ContractSalvage.GridEntityId = 0L;
            myObjectBuilder_ContractSalvage.GridDistance = num2;
            myObjectBuilder_ContractSalvage.PlanetEntityId = planetEntityId;
            myObjectBuilder_ContractSalvage.SpawnGroupSubtypeId = spawnGroupSubtypeId;
            myObjectBuilder_ContractSalvage.ContractCondition = contractCondition;
            myObjectBuilder_ContractSalvage.ContractTypeDefinitionId = m_contractTypeDefinition.Id;
            MyContractSalvage myContractSalvage = new MyContractSalvage();
            myContractSalvage.Init(myObjectBuilder_ContractSalvage);
            contract = myContractSalvage;
            return MyContractCreationResults.Success;
        }

        public override bool CanBeGenerated(StationType stationType, MyFaction faction)
        {
            if (!(m_contractTypeDefinition is MyContractTypeSalvageDefinition myContractTypeSalvageDefinition))
            {
                return false;
            }

            switch (stationType)
            {
                case StationType.PlanetStation:
                    return myContractTypeSalvageDefinition.MaxPlanetRange.HasValue && MyPlanets.GetPlanets().Any();
                case StationType.AsteroidFieldStation:
                case StationType.OrbitalStation:
                case StationType.DeepSpaceStation:
                    return !myContractTypeSalvageDefinition.MaxPlanetRange.HasValue;
            }

            return false;
        }

        private static void FilterByVoxelMaterial(List<MySpawnGroupDefinition> groups, MyPlanet planet, ref Vector3D surfacePosition, List<MySpawnGroupDefinition> result)
        {
            result.Clear();
            string text = planet.GetMaterialAt(ref surfacePosition)?.MaterialTypeName;
            foreach (MySpawnGroupDefinition group in groups)
            {
                List<string> voxelMaterials = group.PlanetaryInstallationSettings.VoxelMaterials;
                if (voxelMaterials == null || voxelMaterials.Count == 0)
                {
                    result.Add(group);
                }
                else if (text != null && voxelMaterials.Contains(text))
                {
                    result.Add(group);
                }
            }
        }

        private static long GetMoneyRewardForSalvageContract(long baseReward, double distance, float distanceMultiplier)
        {
            return (long)((double)baseReward + distance * (double)distanceMultiplier);
        }

        protected override int GetRewardRep(MyContractTypeDefinition def)
        {
            return def.MinimumReputation;
        }
    }

}
