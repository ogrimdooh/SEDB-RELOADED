using Sandbox.Definitions;
using Sandbox.Game.Contracts;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Extensions;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.Definitions.SessionComponents;
using VRage.Game.ObjectBuilders.Components.Contracts;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace SEDiscordBridge.Controllers.Economics
{
    public class MyCustomContract_TypePvEBountyStrategy : MyCustomContract_Base
    {
        public MyCustomContract_TypePvEBountyStrategy(MySessionComponentEconomyDefinition economyDefinition, MyContractTypeDefinition myContractTypeDefinition)
            : base(economyDefinition, myContractTypeDefinition)
        {
        }

        public override bool CanBeGenerated(StationType stationType, MyFaction faction)
        {
            if (!(m_contractTypeDefinition is MyContractTypePvEBountyDefinition myContractTypePvEBountyDefinition) || string.IsNullOrEmpty(myContractTypePvEBountyDefinition.TargetFactionType))
            {
                return false;
            }

            if (MySession.Static.Settings.EnableBountyContracts)
            {
                return HasTargetFaction(myContractTypePvEBountyDefinition.TargetFactionType);
            }

            return false;
        }

        private static bool HasTargetFaction(string targetFactionType)
        {
            foreach (KeyValuePair<long, MyFaction> faction in MySession.Static.Factions)
            {
                if (faction.Value.IsEveryoneNpc() && faction.Value.FactionTypeString == targetFactionType)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3D? GenerateSpawnPosition(Vector3D origin, double min, double max)
        {
            double num = MyRandom.Instance.NextDouble(min, max);
            Vector3D vector3D = Vector3D.Normalize(MyUtils.GetRandomVector3());
            return MyGravityProviderSystem.FindPositionOutsideNaturalGravity(origin + vector3D * num);
        }

        public override MyContractCreationResults GenerateContract(out MyContract contract, StationType stationType, Vector3D position, long factionId, long blockId, MyMinimalPriceCalculator calculator, MyTimeSpan now, params Tuple<string, object>[] customData)
        {
            MyFactionCollection factions = MySession.Static.Factions;
            contract = null;
            if (!(m_contractTypeDefinition is MyContractTypePvEBountyDefinition myContractTypePvEBountyDefinition))
            {
                return MyContractCreationResults.Error;
            }

            if (!(factions.TryGetFactionById(factionId) is MyFaction))
            {
                return MyContractCreationResults.Error;
            }

            Vector3D? vector3D = GenerateSpawnPosition(position, myContractTypePvEBountyDefinition.MinDistance, myContractTypePvEBountyDefinition.MaxDistance);
            if (!vector3D.HasValue)
            {
                return MyContractCreationResults.Error;
            }

            MyFaction targetFaction = GetTargetFaction(myContractTypePvEBountyDefinition.TargetFactionType);
            if (targetFaction == null)
            {
                return MyContractCreationResults.Error;
            }

            SerializableDefinitionId targetPrefab = default(SerializableDefinitionId);
            float price = 0f;
            float prefabRewardMultiplier = 1f;
            if (myContractTypePvEBountyDefinition.TargetSpawnGroups != null && myContractTypePvEBountyDefinition.TargetSpawnGroups.Count > 0)
            {
                MyBountySpawnGroupEntry myBountySpawnGroupEntry = myContractTypePvEBountyDefinition.TargetSpawnGroups[MyRandom.Instance.Next(0, myContractTypePvEBountyDefinition.TargetSpawnGroups.Count)];
                targetPrefab = myBountySpawnGroupEntry.SpawnGroup;
                prefabRewardMultiplier = myBountySpawnGroupEntry.RewardMultiplier;
                MySpawnGroupDefinition spawnGroupDefinition = MyDefinitionManager.Static.GetSpawnGroupDefinition(myBountySpawnGroupEntry.SpawnGroup);
                if (spawnGroupDefinition?.Prefabs != null && spawnGroupDefinition.Prefabs.Count > 0)
                {
                    string[] array = new string[spawnGroupDefinition.Prefabs.Count];
                    for (int i = 0; i < spawnGroupDefinition.Prefabs.Count; i++)
                    {
                        array[i] = spawnGroupDefinition.Prefabs[i].SubtypeId;
                    }

                    long num = 0;
                    for (int j = 0; j < array.Length; j++)
                    {
                        if (PrefabPriceController.AddPrefabToShop(array[j], out var prefabInfo))
                        {
                            num += prefabInfo.TotalPCU;
                            price += prefabInfo.BaseValue;
                        }
                    }

                    if (MySession.Static.NPCBlockLimits.Pirate.PCU < num)
                    {
                        return MyContractCreationResults.Fail_Common;
                    }
                }
            }

            float rewardModifier = new Vector2(myContractTypePvEBountyDefinition.RewardModifierMin, myContractTypePvEBountyDefinition.RewardModifierMax).GetRandom();
            long moneyRewardForBountyContract = (long)(GetMoneyRewardForBountyContract((long)price, rewardModifier, 1f) / 10f);
            MyObjectBuilder_PvEBountyContract myObjectBuilder_PvEBountyContract = CreateCommonOb<MyObjectBuilder_PvEBountyContract>(m_contractTypeDefinition, MyEntityIdentifier.AllocateId(MyEntityIdentifier.ID_OBJECT_TYPE.CONTRACT), factionId, blockId, moneyRewardForBountyContract, stationType == StationType.DeepSpaceStation);
            myObjectBuilder_PvEBountyContract.TargetFactionId = targetFaction.FactionId;
            myObjectBuilder_PvEBountyContract.TargetPosition = vector3D.Value;
            myObjectBuilder_PvEBountyContract.TargetDistance = Vector3D.Distance(position, vector3D.Value);
            myObjectBuilder_PvEBountyContract.Targets = new List<long>();
            myObjectBuilder_PvEBountyContract.TargetPrefab = targetPrefab;
            myObjectBuilder_PvEBountyContract.RewardModifier = rewardModifier;
            myObjectBuilder_PvEBountyContract.PrefabRewardMultiplier = prefabRewardMultiplier;
            myObjectBuilder_PvEBountyContract.RemainingTimeInS = GetDurationForBountyContract(myContractTypePvEBountyDefinition).Seconds;
            myObjectBuilder_PvEBountyContract.ContractTypeDefinitionId = m_contractTypeDefinition.Id;
            MyContractPvEBounty myContractPvEBounty = new MyContractPvEBounty();
            myContractPvEBounty.Init(myObjectBuilder_PvEBountyContract);
            contract = myContractPvEBounty;
            return MyContractCreationResults.Success;
        }

        private static MyFaction GetTargetFaction(string targetFactionType)
        {
            List<MyFaction> list = new List<MyFaction>();
            foreach (KeyValuePair<long, MyFaction> faction in MySession.Static.Factions)
            {
                if (faction.Value.IsEveryoneNpc() && faction.Value.FactionTypeString == targetFactionType)
                {
                    list.Add(faction.Value);
                }
            }

            if (list.Count != 0)
            {
                return list[MyRandom.Instance.Next(list.Count)];
            }

            return null;
        }

        protected override int GetRewardRep(MyContractTypeDefinition def)
        {
            return def.MinimumReputation;
        }

        private static float GetRewardModifier(MyContractTypePvEBountyDefinition def)
        {
            float num = ((def.RewardModifierMin > 0f) ? def.RewardModifierMin : 1f);
            float num2 = ((def.RewardModifierMax > 0f) ? def.RewardModifierMax : num);
            if (num2 < num)
            {
                float num3 = num;
                num = num2;
                num2 = num3;
            }

            return num + (float)MyRandom.Instance.NextDouble() * (num2 - num);
        }

        private static long GetMoneyRewardForBountyContract(long baseReward, float rewardModifier, float prefabRewardMultiplier)
        {
            float num = ((rewardModifier > 0f) ? rewardModifier : 1f);
            float num2 = ((prefabRewardMultiplier > 0f) ? prefabRewardMultiplier : 1f);
            return (long)((float)baseReward * num * num2);
        }

        private static MyTimeSpan GetDurationForBountyContract(MyContractTypePvEBountyDefinition def)
        {
            return MyTimeSpan.FromSeconds(def.DurationMultiplier * def.DurationBaseTime);
        }
    }

}
