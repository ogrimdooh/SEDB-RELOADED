using Sandbox.Definitions;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using SEDiscordBridge.Controllers.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Definitions.SessionComponents;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Library.Utils;

namespace SEDiscordBridge.Controllers.Economics
{
    public static class EconomyOverriding
    {

        private static MySessionComponentEconomy _component;
        public static MySessionComponentEconomy Component
        {
            get
            {
                if (_component == null)
                {
                    _component = MySession.Static.GetComponent<MySessionComponentEconomy>();
                    if (_component != null)
                    {
                        Init();
                    }
                }
                return _component;
            }
        }

        public static bool IsValid
        {
            get
            {
                return Component != null;
            }
        }

        public static MySessionComponentEconomyDefinition Definition
        {
            get
            {
                if (Component != null)
                {
                    return Component.EconomyDefinition;
                }
                return null;
            }
        }

        private static MyMinimalPriceCalculator m_minimalPriceCalculator = new MyMinimalPriceCalculator();
        public static MyMinimalPriceCalculator MinimalPriceCalculator
        {
            get
            {
                return m_minimalPriceCalculator;
            }
        }

        private static Dictionary<MyDefinitionId, MyCustomContract_Base> m_contractTypeStrategies = new Dictionary<MyDefinitionId, MyCustomContract_Base>();
        private static Dictionary<MyContractStrategyType, List<MyDefinitionId>> m_contractsByStrategy = new Dictionary<MyContractStrategyType, List<MyDefinitionId>>();

        private static void Init()
        {
            foreach (KeyValuePair<MyDefinitionId, MyContractTypeDefinition> contractTypeDefinition in MyDefinitionManager.Static.GetContractTypeDefinitions())
            {
                MyContractTypeDefinition value = contractTypeDefinition.Value;
                MyDefinitionId key = contractTypeDefinition.Key;
                switch (value.StrategyType)
                {
                    case MyContractStrategyType.Acquisition:
                        m_contractTypeStrategies.Add(key, new MyCustomContract_TypeAcquisitionStrategy(Definition, value));
                        if (!m_contractsByStrategy.ContainsKey(MyContractStrategyType.Acquisition))
                        {
                            m_contractsByStrategy.Add(MyContractStrategyType.Acquisition, new List<MyDefinitionId>());
                        }
                        m_contractsByStrategy[MyContractStrategyType.Acquisition].Add(key);
                        break;
                    case MyContractStrategyType.Bounty:
                        //m_contractTypeStrategies.Add(key, new MyContractTypeBountyStrategy(economyDefinition, value));
                        break;
                    case MyContractStrategyType.GridHauling:
                        //m_contractTypeStrategies.Add(key, new MyContractTypeGridHaulingStrategy(economyDefinition, value));
                        break;
                    case MyContractStrategyType.Hauling:
                        //m_contractTypeStrategies.Add(key, new MyContractTypeHaulingStrategy(economyDefinition, value));
                        break;
                    case MyContractStrategyType.PvEBounty:
                        //m_contractTypeStrategies.Add(key, new MyContractTypePvEBountyStrategy(economyDefinition, value));
                        break;
                    case MyContractStrategyType.Repair:
                        m_contractTypeStrategies.Add(key, new MyCustomContract_TypeRepairStrategy(Definition, value));
                        if (!m_contractsByStrategy.ContainsKey(MyContractStrategyType.Repair))
                        {
                            m_contractsByStrategy.Add(MyContractStrategyType.Repair, new List<MyDefinitionId>());
                        }
                        m_contractsByStrategy[MyContractStrategyType.Repair].Add(key);
                        break;
                    case MyContractStrategyType.Salvage:
                        //m_contractTypeStrategies.Add(key, new MyContractTypeSalvageStrategy(economyDefinition, value));
                        break;
                    case MyContractStrategyType.Search:
                        m_contractTypeStrategies.Add(key, new MyCustomContract_TypeSearchStrategy(Definition, value));
                        if (!m_contractsByStrategy.ContainsKey(MyContractStrategyType.Search))
                        {
                            m_contractsByStrategy.Add(MyContractStrategyType.Search, new List<MyDefinitionId>());
                        }
                        m_contractsByStrategy[MyContractStrategyType.Search].Add(key);
                        break;
                }
            }

            Logging.Instance.LogInfo(typeof(EconomicsConstants), $"EconomicsConstants initialized.");
        }

        public static MyCustomContract_Base GetRandomContractType(StationType stationType, params MyContractStrategyType[] types)
        {
            if (IsValid)
            {
                var validIds = m_contractsByStrategy
                    .Where(x => types.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .ToList();
                return m_contractTypeStrategies
                    .Where(x => x.Value.CanBeGenerated(stationType, FactionsController.FACTION_2DAWN) && validIds.Contains(x.Key))
                    .OrderBy(x => MyRandom.Instance.NextFloat())
                    .Select(x => x.Value)
                    .FirstOrDefault();
            }
            return null;
        }

    }

}
