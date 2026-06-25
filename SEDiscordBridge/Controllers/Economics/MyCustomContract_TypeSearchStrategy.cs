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
using VRage;
using VRage.Game.Definitions.SessionComponents;
using VRage.Game.ObjectBuilders.Components.Contracts;
using VRage.Library.Utils;
using VRageMath;

namespace SEDiscordBridge.Controllers.Economics
{
    public class MyCustomContract_TypeSearchStrategy : MyCustomContract_Base
    {
        private static readonly float GRAVITY_SQUARED_EPSILON = 0.0001f;

        public MyCustomContract_TypeSearchStrategy(MySessionComponentEconomyDefinition economyDefinition, MyContractTypeDefinition contractTypeDefinition)
            : base(economyDefinition, contractTypeDefinition)
        {

        }

        public override MyContractCreationResults GenerateContract(out MyContract contract, StationType stationType, Vector3D position, long factionId, long blockId, MyMinimalPriceCalculator calculator, MyTimeSpan now, params Tuple<string, object>[] customData)
        {
            MyFactionCollection factions = MySession.Static.Factions;
            contract = null;
            int num = 20;
            int num2 = 50;

            Vector3D vector3D = Vector3D.Zero;
            Vector3D vector3D2 = Vector3D.Zero;
            double num3 = 0.0;
            double num4 = 0.0;
            double num5 = 0.0;
            MyContractTypeFindDefinition myContractTypeFindDefinition = m_contractTypeDefinition as MyContractTypeFindDefinition;
            if (stationType == StationType.PlanetStation)
            {
                BoundingSphereD boundingSphereD = new BoundingSphereD(position, myContractTypeFindDefinition.MaxGridDistance);
                bool flag = false;
                int num6 = 0;
                do
                {
                    Vector3D? vector3D3 = boundingSphereD.RandomToUniformPointInSphereWithInnerCutout(MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble(), myContractTypeFindDefinition.MinGridDistance);
                    if (vector3D3.HasValue)
                    {
                        MyPlanet closestPlanet = MyPlanets.Static.GetClosestPlanet(vector3D3.Value);
                        if (closestPlanet != null && closestPlanet.Generator.AllowContractSpawns)
                        {
                            float num7 = 10f;
                            Vector3D closestSurfacePointGlobal = closestPlanet.GetClosestSurfacePointGlobal(vector3D3.Value);
                            vector3D = closestSurfacePointGlobal + Vector3.Normalize(closestSurfacePointGlobal - closestPlanet.PositionComp.GetPosition()) * num7;
                            Vector3D? vector3D4 = new BoundingSphereD(vector3D, myContractTypeFindDefinition.MaxGpsOffset).RandomToUniformPointInSphere(MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble());
                            if (!vector3D4.HasValue)
                            {
                                return MyContractCreationResults.Fail_Common;
                            }

                            vector3D2 = closestPlanet.GetClosestSurfacePointGlobal(vector3D4.Value);
                            flag = true;
                            break;
                        }
                    }

                    num6++;
                }
                while (num6 <= num);
                if (!flag)
                {
                    return MyContractCreationResults.Fail_Common;
                }
            }
            else
            {
                BoundingSphereD boundingSphereD2 = new BoundingSphereD(position, myContractTypeFindDefinition.MaxGridDistance);
                bool flag2 = false;
                int num8 = 0;
                do
                {
                    Vector3D? vector3D5 = boundingSphereD2.RandomToUniformPointInSphereWithInnerCutout(MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble(), myContractTypeFindDefinition.MinGridDistance);
                    if (!vector3D5.HasValue)
                    {
                        continue;
                    }

                    List<MyObjectSeed> list = new List<MyObjectSeed>();
                    MyProceduralWorldGenerator.Static.OverlapAllAsteroidAndEncountersInSphere(new BoundingSphereD(vector3D5.Value, num2), list);
                    if (list.Count <= 0)
                    {
                        if (MyGravityProviderSystem.CalculateNaturalGravityInPoint(vector3D5.Value).LengthSquared() <= GRAVITY_SQUARED_EPSILON)
                        {
                            flag2 = true;
                            vector3D = vector3D5.Value;
                            break;
                        }

                        num8++;
                    }
                }
                while (num8 <= num);
                if (!flag2)
                {
                    return MyContractCreationResults.Fail_Common;
                }

                Vector3D? vector3D6 = new BoundingSphereD(vector3D, myContractTypeFindDefinition.MaxGpsOffset).RandomToUniformPointInSphere(MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble());
                if (!vector3D6.HasValue)
                {
                    return MyContractCreationResults.Fail_Common;
                }

                vector3D2 = vector3D6.Value;
            }

            num5 = myContractTypeFindDefinition.MaxGpsOffset;
            num3 = (vector3D - position).Length();
            num4 = (vector3D2 - position).Length();
            long rewardMoney = GetMoneyRewardForSearchContract(myContractTypeFindDefinition.MinimumMoney, num3, myContractTypeFindDefinition.MinGridDistance) / 1000 * 1000;
            MyObjectBuilder_ContractFind myObjectBuilder_ContractFind = CreateCommonOb<MyObjectBuilder_ContractFind>(myContractTypeFindDefinition, MyEntityIdentifier.AllocateId(MyEntityIdentifier.ID_OBJECT_TYPE.CONTRACT), factionId, blockId, rewardMoney, stationType == StationType.DeepSpaceStation);
            myObjectBuilder_ContractFind.GridPosition = vector3D;
            myObjectBuilder_ContractFind.GpsPosition = vector3D2;
            myObjectBuilder_ContractFind.GpsDistance = num4;
            myObjectBuilder_ContractFind.MaxGpsOffset = (float)myContractTypeFindDefinition.MaxGpsOffset;
            myObjectBuilder_ContractFind.GridId = 0L;
            myObjectBuilder_ContractFind.KeepGridAtTheEnd = false;
            myObjectBuilder_ContractFind.RemainingTimeInS = GetDurationForSearchContract(myContractTypeFindDefinition, num3, num5).Seconds;
            myObjectBuilder_ContractFind.ContractTypeDefinitionId = m_contractTypeDefinition.Id;
            MyContractFind myContractFind = new MyContractFind();
            myContractFind.Init(myObjectBuilder_ContractFind);
            contract = myContractFind;
            return MyContractCreationResults.Success;
        }

        private long GetMoneyRewardForSearchContract(long baseRew, double distance, double minDistance)
        {
            return (long)(baseRew + 7000.0 * Math.Pow(distance / minDistance, 2.2));
        }

        public override bool CanBeGenerated(StationType stationType, MyFaction faction)
        {
            return true;
        }

        protected override int GetRewardRep(MyContractTypeDefinition def)
        {
            return def.MinimumReputation;
        }

        private MyTimeSpan GetDurationForSearchContract(MyContractTypeFindDefinition def, double distanceInM, double searchAreaRadius, bool cutOutJumps = true)
        {
            double num = searchAreaRadius / 1000.0;
            return MyTimeSpan.FromSeconds(def.DurationMultiplier * (def.Duration_BaseTime + (distanceInM * def.Duration_TimePerMeter + 3.1415929794311523 * num * num * num * def.Duration_TimePerCubicKm)));
        }

    }

}
