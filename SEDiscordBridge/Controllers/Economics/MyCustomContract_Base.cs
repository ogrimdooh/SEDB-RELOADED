using Sandbox;
using Sandbox.Definitions;
using Sandbox.Game.Contracts;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using SEDiscordBridge.Controllers.Types;
using System;
using VRage.Game.Definitions.SessionComponents;
using VRage.Game.ObjectBuilders.Components.Contracts;
using VRage.Library.Utils;
using VRageMath;

namespace SEDiscordBridge.Controllers.Economics
{
    public abstract class MyCustomContract_Base
    {

        public static readonly int TICKS_TO_LIVE = 1;

        protected MySessionComponentEconomyDefinition m_economyDefinition { get; private set; }

        protected MyContractTypeDefinition m_contractTypeDefinition { get; private set; }

        protected T GetContractDefinitionValue<T>(string fieldName)
        {
            try
            {
                return (T)m_contractTypeDefinition.GetType().GetField(fieldName)?.GetValue(m_contractTypeDefinition);
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(GetType(), e);
            }
            return default;
        }

        public MyCustomContract_Base(MySessionComponentEconomyDefinition economyDefinition, MyContractTypeDefinition contractTypeDefinition)
        {
            m_economyDefinition = economyDefinition;
            m_contractTypeDefinition = contractTypeDefinition;
        }

        protected T CreateCommonOb<T>(MyContractTypeDefinition def, long obId, long factionId, long blockId, long rewardMoney, bool isDeepSpaceStation) where T : MyObjectBuilder_Contract, new()
        {
            T val = new T
            {
                Id = obId,
                IsPlayerMade = false,
                State = MyContractStateEnum.Inactive,
                RewardMoney = rewardMoney
            };
            if (isDeepSpaceStation)
            {
                val.RewardMoney = (long)(val.RewardMoney * m_economyDefinition.DeepSpaceStationContractBonus);
            }

            val.RewardReputation = GetRewardRep(def);
            val.StartingDeposit = (long)(def.MinStartingDeposit + MyRandom.Instance.NextDouble() * (def.MaxStartingDeposit - def.MinStartingDeposit));
            val.FailReputationPrice = def.FailReputationPrice;
            val.StartFaction = factionId;
            val.StartStation = 0L;
            val.StartBlock = blockId;
            val.Creation = MyTimeSpan.FromMilliseconds(MySandboxGame.TotalGamePlayTimeInMilliseconds).Ticks;
            val.TicksToDiscard = TICKS_TO_LIVE;
            return val;
        }

        public abstract bool CanBeGenerated(StationType stationType, MyFaction faction);

        protected abstract int GetRewardRep(MyContractTypeDefinition def);

        public abstract MyContractCreationResults GenerateContract(out MyContract contract, StationType stationType, Vector3D position, long factionId, long blockId, MyMinimalPriceCalculator calculator, MyTimeSpan now, params Tuple<string, object>[] customData);

    }

}
