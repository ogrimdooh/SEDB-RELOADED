using Sandbox.Definitions;
using Sandbox.Game.Contracts;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using SEDiscordBridge.Controllers.Types;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.Game.Definitions.SessionComponents;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Components.Contracts;
using VRage.Library.Utils;
using VRage.ObjectBuilder;
using VRageMath;
using static System.Net.Mime.MediaTypeNames;

namespace SEDiscordBridge.Controllers.Economics
{
    public class MyCustomContract_TypeRepairStrategy : MyCustomContract_Base
    {
        private static readonly float GRAVITY_SQUARED_EPSILON = 0.0001f;

        public double MaxGridDistance
        {
            get
            {
                return GetContractDefinitionValue<double>(nameof(MaxGridDistance));
            }
        }

        public double MinGridDistance
        {
            get
            {
                return GetContractDefinitionValue<double>(nameof(MinGridDistance));
            }
        }

        public double Duration_BaseTime
        {
            get
            {
                return GetContractDefinitionValue<double>(nameof(Duration_BaseTime));
            }
        }

        public double Duration_TimePerMeter
        {
            get
            {
                return GetContractDefinitionValue<double>(nameof(Duration_TimePerMeter));
            }
        }

        public float PriceToRewardCoeficient
        {
            get
            {
                return GetContractDefinitionValue<float>(nameof(PriceToRewardCoeficient));
            }
        }

        public float PriceSpread
        {
            get
            {
                return GetContractDefinitionValue<float>(nameof(PriceSpread));
            }
        }

        public float TimeToPriceDenominator
        {
            get
            {
                return GetContractDefinitionValue<float>(nameof(TimeToPriceDenominator));
            }
        }

        public List<string> PrefabNames
        {
            get
            {
                return GetContractDefinitionValue<List<string>>(nameof(PrefabNames));
            }
        }

        public MyCustomContract_TypeRepairStrategy(MySessionComponentEconomyDefinition economyDefinition, MyContractTypeDefinition contractTypeDefinition)
            : base(economyDefinition, contractTypeDefinition)
        {
        }

        public override bool CanBeGenerated(StationType stationType, MyFaction faction)
        {
            if (stationType == StationType.PlanetStation)
            {
                return false;
            }

            return true;
        }

        public override MyContractCreationResults GenerateContract(out MyContract contract, StationType stationType, Vector3D position, long factionId, long blockId, MyMinimalPriceCalculator calculator, MyTimeSpan now, params Tuple<string, object>[] customData)
        {
            MyFactionCollection factions = MySession.Static.Factions;
            contract = null;
            int num = 20;
            int num2 = 50;

            if (stationType == StationType.PlanetStation)
            {
                return MyContractCreationResults.Fail_Impossible;
            }

            Vector3D vector3D = Vector3D.Zero;
            BoundingSphereD boundingSphereD = new BoundingSphereD(position, MaxGridDistance);
            bool flag = false;
            int num3 = 0;
            do
            {
                Vector3D? vector3D2 = boundingSphereD.RandomToUniformPointInSphereWithInnerCutout(MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble(), MyRandom.Instance.NextDouble(), MinGridDistance);
                if (!vector3D2.HasValue)
                {
                    continue;
                }

                List<MyObjectSeed> list = new List<MyObjectSeed>();
                MyProceduralWorldGenerator.Static.OverlapAllAsteroidAndEncountersInSphere(new BoundingSphereD(vector3D2.Value, num2), list);
                if (list.Count <= 0)
                {
                    if (MyGravityProviderSystem.CalculateNaturalGravityInPoint(vector3D2.Value).LengthSquared() <= GRAVITY_SQUARED_EPSILON)
                    {
                        flag = true;
                        vector3D = vector3D2.Value;
                        break;
                    }

                    num3++;
                }
            }
            while (num3 <= num);
            if (!flag)
            {
                return MyContractCreationResults.Fail_Common;
            }

            double gridDistance = (vector3D - position).Length();

            string text = "";
            if (PrefabNames.Count > 0)
            {
                text = PrefabNames[MyRandom.Instance.Next(0, PrefabNames.Count)];
            }

            if (!PrefabPriceController.AddPrefabToShop(text, out var prefabItem))
            {
                return MyContractCreationResults.Fail_Common;
            }

            if (MySession.Static.NPCBlockLimits.Pirate.PCU < prefabItem.TotalPCU)
            {
                return MyContractCreationResults.Fail_Common;
            }

            long rewardMoney = GetMoneyRewardForRepairContract(m_contractTypeDefinition.MinimumMoney, gridDistance, (long)prefabItem.RepairCost);
            MyObjectBuilder_ContractRepair myObjectBuilder_ContractRepair = CreateCommonOb<MyObjectBuilder_ContractRepair>(m_contractTypeDefinition, MyEntityIdentifier.AllocateId(MyEntityIdentifier.ID_OBJECT_TYPE.CONTRACT), factionId, blockId, rewardMoney, stationType == StationType.DeepSpaceStation);
            myObjectBuilder_ContractRepair.GridPosition = vector3D;
            myObjectBuilder_ContractRepair.GridId = 0L;
            myObjectBuilder_ContractRepair.PrefabName = text;
            myObjectBuilder_ContractRepair.BlocksToRepair = new MySerializableList<Vector3I>();
            myObjectBuilder_ContractRepair.KeepGridAtTheEnd = false;
            myObjectBuilder_ContractRepair.UnrepairedBlockCount = 0;
            myObjectBuilder_ContractRepair.RemainingTimeInS = GetDurationForRepairContract(gridDistance, (int)prefabItem.RepairTime).Seconds;
            myObjectBuilder_ContractRepair.ContractTypeDefinitionId = m_contractTypeDefinition.Id;
            MyContractRepair myContractRepair = new MyContractRepair();
            myContractRepair.Init(myObjectBuilder_ContractRepair);
            contract = myContractRepair;
            return MyContractCreationResults.Success;
        }

        private long GetMoneyRewardForRepairContract(long baseRew, double gridDistance, long gridRepairPrice)
        {
            return (long)(baseRew * Math.Pow(1.25, Math.Log10(gridDistance / 1000))) + gridRepairPrice;
        }

        protected override int GetRewardRep(MyContractTypeDefinition def)
        {
            return def.MinimumReputation;
        }

        private MyTimeSpan GetDurationForRepairContract(double gridDistance, int repairComponentTimeInS)
        {
            return MyTimeSpan.FromSeconds(m_contractTypeDefinition.DurationMultiplier * (Duration_BaseTime + gridDistance * Duration_TimePerMeter + repairComponentTimeInS));
        }
    }

}
