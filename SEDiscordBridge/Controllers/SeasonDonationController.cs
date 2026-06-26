using Sandbox.Game;
using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.SeasonMeta;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;

namespace SEDiscordBridge.Controllers
{
    public static class SeasonDonationController
    {

        public static bool DoRegisterPlayerDonation(ulong steamId, SeasonMetaDonationOrigin origin, Dictionary<MyInventory, Dictionary<MyDefinitionId, float>> validInventories)
        {
            if (validInventories.Any())
            {
                var finalItens = new Dictionary<MyDefinitionId, float>();
                foreach (var inventory in validInventories.Keys)
                {
                    var items = validInventories[inventory];
                    foreach (var itemId in items.Keys)
                    {
                        var amount = (MyFixedPoint)items[itemId];
                        var removedAmount = (float)inventory.RemoveItemsOfType(amount, itemId);
                        if (removedAmount > 0f)
                        {
                            if (!finalItens.ContainsKey(itemId))
                            {
                                finalItens.Add(itemId, removedAmount);
                            }
                            else
                            {
                                finalItens[itemId] += removedAmount;
                            }
                        }
                    }
                }
                return DoRegisterPlayerDonation(steamId, origin, finalItens);
            }
            return false;
        }

        public static bool DoRegisterPlayerDonation(ulong steamId, SeasonMetaDonationOrigin origin, Dictionary<MyDefinitionId, float> items)
        {
            float finalAmount = 0;
            float finalMass = 0;
            var categories = new HashSet<string>();
            foreach (var itemId in items.Keys)
            {
                var removedMass = items[itemId] * ItensConstants.GetItemMass(itemId);
                finalAmount += items[itemId];
                finalMass += removedMass;
                var categoryId = SEDBStorage.Instance.SeasonMetaConfig.GetItemCategoryById(itemId);
                categories.Add(categoryId);
                var categoryInfo = SEDBStorage.Instance.SeasonMetaConfig.GetCategoryById(categoryId);
                var itemInfo = categoryInfo.GetItemById(itemId);
                SEDBStorage.Instance.SeasonMetaResult.GetActiveResult().AddValueToEntry(
                    categoryId,
                    (long)items[itemId],
                    itemInfo.Weight
                );
            }
            if (finalAmount > 0)
            {
                var donation = new SeasonMetaDonationEntry()
                {
                    SteamId = steamId,
                    ItemCount = (long)finalAmount,
                    MassAmount = finalMass,
                    OperationDate = DateTime.Now,
                    Origin = origin,
                    CategoriesIds = categories.ToList()
                };
                SEDBStorage.Instance.SeasonMetaResult.GetActiveResult().Donations.Add(donation);
                if (origin == SeasonMetaDonationOrigin.Player)
                {
                    SEDiscordBridgePlugin.Static.AlertDonationIsCompleted(donation.SteamId, donation.ItemCount, donation.MassAmount);
                }
                return true;
            }
            return false;
        }

    }

}
