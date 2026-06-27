using Sandbox.Game.Contracts;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRage.Game.ModAPI;

namespace SEDiscordBridge.Controllers.Economics
{
    public static class ContractSystemOverriding
    {

        private static MethodInfo _addContractMethod;

        private static MySessionComponentContractSystem _component;
        public static MySessionComponentContractSystem Component
        {
            get
            {
                if (_component == null)
                {
                    _component = MySession.Static.GetComponent<MySessionComponentContractSystem>();
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
                return Component != null && _addContractMethod != null;
            }
        }

        private static void Init()
        {
            _addContractMethod = Component.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.Name == "AddContract" && x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(MyContract))
                .FirstOrDefault();
            if (_addContractMethod != null)
            {
                Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Found AddContract method.");
            }
            else
            {
                Logging.Instance.LogWarning(typeof(EconomicsConstants), $"Could not find AddContract method. Contracts will not be added.");
            }
        }

        public static void AddContract(MyContract contract)
        {
            if (IsValid)
            {
                if (contract is MyContractFind myContractSearch)
                {
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Contract is a search contract with grid position {myContractSearch.GridPosition} and GPS position {myContractSearch.GpsPosition}.");
                    _addContractMethod.Invoke(Component, new object[] { contract });
                }
                else if (contract is MyContractRepair myContractRepair)
                {
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Contract is a repair contract with grid position {myContractRepair.GridPosition} and prefab name {myContractRepair.PrefabName}.");
                    _addContractMethod.Invoke(Component, new object[] { contract });
                }
                else if (contract is MyContractObtainAndDeliver myContractObtainAndDeliver)
                {
                    var condition = myContractObtainAndDeliver.ContractCondition as MyContractConditionDeliverItems;
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Contract is an acquisition contract with item type {condition.ItemType} and item amount {condition.ItemAmount}.");
                    _addContractMethod.Invoke(Component, new object[] { contract });
                }
                else if (contract is MyContractSalvage myContractSalvage)
                {
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Contract is a salvage contract with grid position {myContractSalvage.GridPosition} and prefab name {myContractSalvage.PrefabName}.");
                    _addContractMethod.Invoke(Component, new object[] { contract });
                }
                else if (contract is MyContractPvEBounty myContractPvEBounty)
                {
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Contract is a PvE bounty contract with grid position {myContractPvEBounty.TargetPosition} and prefab name {(myContractPvEBounty as IMyContractPvEBounty).TargetSpawnGroup.SubtypeName}.");
                    _addContractMethod.Invoke(Component, new object[] { contract });
                }
                else if (contract is MyContractGridHauling myContractGridHauling)
                {
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Contract is a grid hauling contract with grid position {myContractGridHauling.GridPosition} and deliver distance {myContractGridHauling.DeliverDistance}.");
                    _addContractMethod.Invoke(Component, new object[] { contract });
                }
                else
                {
                    Logging.Instance.LogWarning(typeof(EconomicsConstants), $"Contract is of unknown type {contract.GetType().Name}.");
                }
            }
        }

        public static long[] GetAllContractsByBlockId(long blockId)
        {
            if (IsValid)
            {
                return Component.GetAvailableContractsForBlock(blockId).Select(x => (x as MyContract).Id).ToArray();
            }
            return new long[0];

        }

        public static MyContract GetContractById(long contractId)
        {
            if (IsValid)
            {
                return Component.GetContractById(contractId) as MyContract;
            }
            return null;
        }

    }

}
