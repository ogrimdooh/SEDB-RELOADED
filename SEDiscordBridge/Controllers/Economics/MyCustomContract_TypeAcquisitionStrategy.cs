using Sandbox.Definitions;
using Sandbox.Game.Contracts;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Entities.Base;
using System;
using System.Linq;
using VRage;
using VRage.Game.Definitions.SessionComponents;
using VRage.Game.ObjectBuilders.Components.Contracts;
using VRage.Library.Utils;
using VRageMath;

namespace SEDiscordBridge.Controllers.Economics
{
    public class MyCustomContract_TypeAcquisitionStrategy : MyCustomContract_Base
    {
        public MyCustomContract_TypeAcquisitionStrategy(MySessionComponentEconomyDefinition economyDefinition, MyContractTypeDefinition contractTypeDefinition)
            : base(economyDefinition, contractTypeDefinition)
        {
        }

        public override bool CanBeGenerated(StationType stationType, MyFaction faction)
        {
            return true;
        }

        public MyContractCreationResults GenerateContract(out MyContract contract, StationType stationType, Vector3D position, long factionId, long blockId, MyMinimalPriceCalculator calculator, MyTimeSpan now, UniqueEntityId itemType, int itemAmount, int itemPrice, float itemVolume, long rewardMoney)
        {
            var customData = new Tuple<string, object>[]
            {
                new Tuple<string, object>("ItemType", itemType),
                new Tuple<string, object>("ItemAmount", itemAmount),
                new Tuple<string, object>("ItemPrice", itemPrice),
                new Tuple<string, object>("ItemVolume", itemVolume),
                new Tuple<string, object>("RewardMoney", rewardMoney)
            };
            return GenerateContract(out contract, stationType, position, factionId, blockId, calculator, now, customData);
        }

        public override MyContractCreationResults GenerateContract(out MyContract contract, StationType stationType, Vector3D position, long factionId, long blockId, MyMinimalPriceCalculator calculator, MyTimeSpan now, params Tuple<string, object>[] customData)
        {
            MyFactionCollection factions = MySession.Static.Factions;
            contract = null;

            long num = MyEntityIdentifier.AllocateId(MyEntityIdentifier.ID_OBJECT_TYPE.CONTRACT);
            long num2 = 0L;

            UniqueEntityId itemType = customData.FirstOrDefault(x => x.Item1 == "ItemType").Item2 as UniqueEntityId;
            int itemAmount = (int)(customData.FirstOrDefault(x => x.Item1 == "ItemAmount").Item2 ?? 0);
            int itemPrice = (int)(customData.FirstOrDefault(x => x.Item1 == "ItemPrice").Item2 ?? 0);
            float itemVolume = (float)(customData.FirstOrDefault(x => x.Item1 == "ItemVolume").Item2 ?? 0);
            long rewardMoney = (long)(customData.FirstOrDefault(x => x.Item1 == "RewardMoney").Item2 ?? 0);

            int num3 = 0;
            MyObjectBuilder_ContractConditionDeliverItems contractCondition = new MyObjectBuilder_ContractConditionDeliverItems
            {
                Id = MyEntityIdentifier.AllocateId(MyEntityIdentifier.ID_OBJECT_TYPE.CONTRACT_CONDITION),
                ContractId = num,
                FactionEndId = factionId,
                StationEndId = 0L,
                BlockEndId = blockId,
                SubId = num3,
                IsFinished = false,
                TransferItems = false,
                ItemType = itemType.DefinitionId,
                ItemAmount = itemAmount,
                ItemVolume = itemVolume
            };
            num3++;
            num2 += itemPrice;
            MyObjectBuilder_ContractObtainAndDeliver myObjectBuilder_ContractObtainAndDeliver = CreateCommonOb<MyObjectBuilder_ContractObtainAndDeliver>(m_contractTypeDefinition, num, factionId, blockId, rewardMoney, stationType == StationType.DeepSpaceStation);
            myObjectBuilder_ContractObtainAndDeliver.RemainingTimeInS = null;
            myObjectBuilder_ContractObtainAndDeliver.ContractCondition = contractCondition;
            myObjectBuilder_ContractObtainAndDeliver.ContractTypeDefinitionId = m_contractTypeDefinition.Id;
            MyContractObtainAndDeliver myContractObtainAndDeliver = new MyContractObtainAndDeliver();
            myContractObtainAndDeliver.Init(myObjectBuilder_ContractObtainAndDeliver);
            contract = myContractObtainAndDeliver;
            return MyContractCreationResults.Success;
        }

        protected override int GetRewardRep(MyContractTypeDefinition def)
        {
            return def.MinimumReputation;
        }

    }

}
