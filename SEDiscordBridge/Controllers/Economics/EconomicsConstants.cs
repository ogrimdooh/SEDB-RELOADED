using Newtonsoft.Json.Linq;
using Sandbox;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Contracts;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.World.Generator;
using Sandbox.ModAPI;
using SEDiscordBridge.Controllers.Types;
using SEDiscordBridge.Entities.Base;
using SEDiscordBridge.Extensions;
using SEDiscordBridge.Storage.FunctionalGrids;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VRage.Collections;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using static VRage.Dedicated.Configurator.SelectInstanceForm;

namespace SEDiscordBridge.Controllers.Economics
{

    public static class EconomicsConstants
    {

        public const string AK1EXPLORERROVER_SUBTYPEID = "AK1ExplorerRover";
        public const string AK2CARGOROVER_SUBTYPEID = "AK2CargoRover";
        public const string AK3DROPPOD_SUBTYPEID = "AK3DropPod";

        public const int DEFAULT_COLLATERAL_CONTRACT = 20;
        public const int DEFAULT_DURATION_CONTRACT = 600;

        public class ShopItemInfo
        {

            public bool PrefabOrigin { get; set; }
            public string PrefabName { get; set; }
            public Vector2 PriceMultiplier { get; set; } = Vector2.One;

        }

        public static readonly Dictionary<UniqueEntityId, ShopItemInfo> SHOP_ITEMS = new Dictionary<UniqueEntityId, ShopItemInfo>()
        {
            { 
                ItensConstants.ZONECHIP_ID, 
                new ShopItemInfo()
                {
                    PriceMultiplier = new Vector2(0.15f, 0.30f)
                } 
            },
            { 
                ItensConstants.DAWNDROPSIGNALEXPLORER_ID, 
                new ShopItemInfo() 
                { 
                    PrefabOrigin = true,
                    PrefabName = AK1EXPLORERROVER_SUBTYPEID,
                    PriceMultiplier = new Vector2(0.85f, 0.95f)
                } 
            },
            { 
                ItensConstants.DAWNDROPSIGNALLITE_ID, 
                new ShopItemInfo()
                {
                    PrefabOrigin = true,
                    PrefabName = AK2CARGOROVER_SUBTYPEID,
                    PriceMultiplier = new Vector2(0.75f, 0.85f)
                } 
            },
            { 
                ItensConstants.DAWNDROPSIGNALSURVIVAL_ID, 
                new ShopItemInfo()
                {
                    PrefabOrigin = true,
                    PrefabName = AK3DROPPOD_SUBTYPEID,
                    PriceMultiplier = new Vector2(0.65f, 0.75f)
                } 
            }
        };


        public static void Init()
        {
            if (!EconomyOverriding.IsValid)
            {
                Logging.Instance.LogWarning(typeof(EconomicsConstants), $"Economy component not found. EconomicsConstants initialization failed.");
                return;
            }

            Logging.Instance.LogInfo(typeof(EconomicsConstants), $"EconomicsConstants initialized.");
        }

        public static void LoadStoreBlock(MyStoreBlock storeBlock, MyCargoContainer storeCargo)
        {
            // Clear the store
            Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Loading store block {storeBlock.EntityId} with {SHOP_ITEMS.Count} items.");
            List<IMyStoreItem> items = new List<IMyStoreItem>();
            storeBlock.GetStoreItems(items);
            foreach (var item in items)
            {
                storeBlock.RemoveStoreItem(item);
            }
            var inv = storeCargo.GetInventory();
            // Limpa inventario antes de atualizar a loga
            inv.Clear();
            // Add item to store
            foreach (var key in SHOP_ITEMS.Keys)
            {
                var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(key.DefinitionId);
                var finalAmount = (float)(int)new Vector2(def.MinimumOfferAmount, def.MaximumOfferAmount).GetRandom();
                finalAmount = (int)inv.AddMaxItems(finalAmount, ItensConstants.GetPhysicalObjectBuilder(key));
                if (finalAmount > 0)
                {
                    var price = def.MinimalPricePerUnit;
                    if (SHOP_ITEMS[key].PrefabOrigin)
                    {
                        if (PrefabPriceController.AddPrefabToShop(SHOP_ITEMS[key].PrefabName, out PrefabPriceController.StationPrefabItem prefabInfo))
                        {
                            price = (int)(prefabInfo.BaseValue - prefabInfo.RepairCost);
                        }
                        else
                        {
                            Logging.Instance.LogWarning(typeof(EconomicsConstants), $"Failed to add prefab {SHOP_ITEMS[key].PrefabName} to store block {storeBlock.EntityId}. Using default price.");
                        }
                    }
                    price = (int)(price * ItemPriceController.STATION_SELL_VALUE_MULTIPLIER.GetRandom());
                    if (SHOP_ITEMS[key].PriceMultiplier != Vector2.One)
                    {
                        price = (int)(price * SHOP_ITEMS[key].PriceMultiplier.GetRandom());
                    }
                    var item = storeBlock.CreateStoreItem(key.DefinitionId, (int)finalAmount, price, StoreItemTypes.Offer);
                    storeBlock.InsertStoreItem(item);
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Added item {key.DefinitionId} to store block {storeBlock.EntityId} with amount {finalAmount}.");
                }
                else
                {
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Failed to add item {key.DefinitionId} to store block {storeBlock.EntityId} because the inventory is full.");
                }
            }
        }

        public static void LoadContractBlock(MyContractBlock contractBlock, StationType stationType, StationLevel stationLevel, FactionType factionType)
        {
            // Remove all contracts from the contract block that are not in the storage entry
            var activeContracts = ContractSystemOverriding.GetAllContractsByBlockId(contractBlock.EntityId);
            Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Clening contract block {contractBlock.EntityId} with {activeContracts.Length} contracts.");
            foreach (var item in activeContracts)
            {
                if (!MyVisualScriptLogicProvider.RemoveContract(item))
                {
                    Logging.Instance.LogWarning(typeof(EconomicsConstants), $"Contrato {item} do bloco {contractBlock.EntityId} não pôde ser removido.");
                }
            }
            // Add new acquisition contracts to the contract block
            MyTimeSpan now = MyTimeSpan.FromMilliseconds(MySandboxGame.TotalGamePlayTimeInMilliseconds);
            var position = contractBlock.CubeGrid.PositionComp.GetPosition();
            var ordersToAdd = ItemPriceController.GetItensToOrder(position, stationType, stationLevel, factionType);
            var aquisitionStrategy = EconomyOverriding.GetRandomContractType(stationType, MyContractStrategyType.Acquisition) as MyCustomContract_TypeAcquisitionStrategy;
            Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Adding {ordersToAdd.Count} contracts to contract block {contractBlock.EntityId}.");
            foreach (var order in ordersToAdd)
            {
                MyContract contract = null;
                if (aquisitionStrategy.GenerateContract(
                    out contract,
                    stationType,
                    position,
                    FactionsController.FACTION_2DAWN.FactionId,
                    contractBlock.EntityId,
                    EconomyOverriding.MinimalPriceCalculator,
                    now,
                    order.ItemId,
                    order.Count,
                    order.Price,
                    order.Volume,
                    order.Reward
                ) == MyContractCreationResults.Success)
                {
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Successfully generated definitions for a contract for block {contractBlock.EntityId}.");
                    ContractSystemOverriding.AddContract(contract);
                }
            }
            // Add others contracts
            var othersCount = ItemPriceController.STATION_ITENS_PROFILE[stationLevel].OtherContractsCount.GetRandomInt();
            Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Adding {othersCount} other contracts to contract block {contractBlock.EntityId}.");
            for (int i = 0; i < othersCount; i++)
            {
                var targetStrategy = EconomyOverriding.GetRandomContractType(stationType, 
                    MyContractStrategyType.Search, 
                    MyContractStrategyType.Repair,
                    MyContractStrategyType.Salvage,
                    MyContractStrategyType.GridHauling,
                    MyContractStrategyType.PvEBounty);
                if (targetStrategy == null)
                {
                    Logging.Instance.LogWarning(typeof(EconomicsConstants), $"No contract strategy found for contract block {contractBlock.EntityId}.");
                    break;
                }
                MyContract contract = null;
                if (targetStrategy.GenerateContract(
                    out contract,
                    stationType,
                    position,
                    FactionsController.FACTION_2DAWN.FactionId,
                    contractBlock.EntityId,
                    EconomyOverriding.MinimalPriceCalculator,
                    now
                ) == MyContractCreationResults.Success)
                {
                    Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Successfully generated definitions for a contract for block {contractBlock.EntityId}.");
                    ContractSystemOverriding.AddContract(contract);
                }
                else
                {
                    Logging.Instance.LogWarning(typeof(EconomicsConstants), $"Failed to generate definitions of strategy {targetStrategy.GetType().Name} for a contract for block {contractBlock.EntityId}.");
                }
            }
        }

    }

}
