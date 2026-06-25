using Sandbox.Definitions;
using Sandbox.Game;
using SEDiscordBridge.Entities.Base;
using SEDiscordBridge.Extensions;
using VRage.Game;
using VRage.Game.ModAPI;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;

namespace SEDiscordBridge.Extensions
{

    public static class InventoryUtils
    {

        private static bool CheckValue(float itemValue, float targetValue, float freeValue, float amount)
        {
            var totalVolume = itemValue * amount;
            var targetVolume = targetValue * amount;
            if (targetVolume <= totalVolume)
                return true;
            targetVolume -= totalVolume;
            return targetVolume <= freeValue;
        }

        public static bool CanBeReplace(this MyInventory destInventory, uint itemId, UniqueEntityId targetItem)
        {
            var item = destInventory.GetItemByID(itemId);
            if (item != null)
            {
                var itemDef = item.Value.Content.GetUniqueId().GetDefinition<MyPhysicalItemDefinition>();
                var targetDef = targetItem.GetDefinition<MyPhysicalItemDefinition>();
                if (itemDef != null && targetDef != null)
                {
                    var freeVolume = destInventory.MaxVolume - destInventory.CurrentVolume;
                    var freeMass = destInventory.MaxMass - destInventory.CurrentMass;
                    return CheckValue(itemDef.Volume, targetDef.Volume, (float)freeVolume, (float)item.Value.Amount) &&
                        CheckValue(itemDef.Mass, targetDef.Mass, (float)freeMass, (float)item.Value.Amount);
                }
            }
            return false;
        }

        public static bool ReplaceItem(this MyInventory destInventory, uint itemId, MyObjectBuilder_PhysicalObject builder)
        {
            if (destInventory.CanBeReplace(itemId, builder.GetUniqueId()))
            {
                var item = destInventory.GetItemByID(itemId);
                if (item != null)
                {
                    var ammount = item.Value.Amount;
                    destInventory.Remove(item, ammount);
                    destInventory.AddMaxItems(ammount, builder);
                    return true;
                }
            }
            return false;
        }

        public static float AddMaxItems(this IMyInventory destInventory, float maxNeeded, MyObjectBuilder_PhysicalObject objectBuilder, bool allowFraction = true)
        {
            var maxNeededFP = (VRage.MyFixedPoint)maxNeeded;
            return (float)destInventory.AddMaxItems(maxNeededFP, objectBuilder, allowFraction);
        }

        public static VRage.MyFixedPoint AddMaxItems(this IMyInventory destInventory, VRage.MyFixedPoint maxNeededFP, MyObjectBuilder_PhysicalObject objectBuilder, bool allowFraction = true)
        {
            var contentId = objectBuilder.GetObjectId();
            if (maxNeededFP <= 0)
            {
                return 0; //Amount to small
            }

            var maxPossible = destInventory.MaxFractionItemsAddable(maxNeededFP, contentId, allowFraction);
            if (maxPossible > 0)
            {
                destInventory.AddItems(maxPossible, objectBuilder);
                return maxPossible;
            }
            else
            {
                return 0;
            }
        }

        public static VRage.MyFixedPoint MaxFractionItemsAddable(this IMyInventory destInventory, VRage.MyFixedPoint maxNeeded, MyItemType itemType, bool allowFraction = true)
        {
            if (!allowFraction)
                maxNeeded = (int)maxNeeded;

            if (destInventory.CanItemsBeAdded(maxNeeded, itemType))
            {
                return maxNeeded;
            }

            if (!allowFraction)
                return 0;

            VRage.MyFixedPoint maxPossible = 0;
            VRage.MyFixedPoint currentStep = (VRage.MyFixedPoint)((float)maxNeeded / 2);
            VRage.MyFixedPoint currentTry = 0;
            while (currentStep > VRage.MyFixedPoint.SmallestPossibleValue)
            {
                currentTry = maxPossible + currentStep;
                if (destInventory.CanItemsBeAdded(currentTry, itemType))
                {
                    maxPossible = currentTry;
                }
                currentStep = (VRage.MyFixedPoint)((float)currentStep / 2);
            }
            return maxPossible;
        }

    }

}