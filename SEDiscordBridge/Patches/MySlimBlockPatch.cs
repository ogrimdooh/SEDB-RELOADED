using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using SEDiscordBridge.Controllers.Grids;
using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.Player;
using SEDiscordBridge.Storage.Profession;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;
using static SEDiscordBridge.PatchController;

namespace SEDiscordBridge.Patches
{
    [PatchingClass]
    public class MySlimBlockPatch
    {
        private static SEDiscordBridgePlugin Plugin;
        private static FieldInfo stockPileField = null;
        private static ConcurrentDictionary<Vector4, Dictionary<MyDefinitionId, int>> stockpileItems = new ConcurrentDictionary<Vector4, Dictionary<MyDefinitionId, int>>();
        public MySlimBlockPatch(SEDiscordBridgePlugin plugin)
        {
            Plugin = plugin;
            stockPileField = typeof(MySlimBlock).GetField("m_stockpile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }
        [PrefixMethod]
        [TargetMethod(Type = typeof(MySlimBlock), Method = "MoveItemsFromConstructionStockpile")]
        public static void Prefix_MoveItemsFromConstructionStockpile(MyInventoryBase toInventory, MyItemFlags flags, MySlimBlock __instance)
        {
            if (__instance == null)
            {
                Logging.Instance.LogWarning(typeof(MySlimBlockPatch), "MySlimBlock instance is null");
                return;
            }
            if (stockPileField == null)
            {
                Logging.Instance.LogWarning(typeof(MySlimBlockPatch), "Could not find m_stockpile field in MySlimBlock");
                return;
            }
            var stockpile = stockPileField.GetValue(__instance) as MyConstructionStockpile;
            if (stockpile == null)
            {
                Logging.Instance.LogWarning(typeof(MySlimBlockPatch), "Could not get stockpile from MySlimBlock");
                return;
            }
            var items = stockpile.GetItems().GroupBy(x => x.Content.GetId()).ToDictionary(x => x.Key, x => x.Sum(y => y.Amount));
            stockpileItems[new Vector4(__instance.Position, __instance.CubeGrid.EntityId)] = items;
        }
        [PostFixMethod]
        [TargetMethod(Type = typeof(MySlimBlock), Method = "MoveItemsFromConstructionStockpile")]
        public static void Post_MoveItemsFromConstructionStockpile(MyInventoryBase toInventory, MyItemFlags flags, MySlimBlock __instance, ref bool __result)
        {
            if (__result)
            {
                Logging.Instance.LogWarning(typeof(MySlimBlockPatch), "MoveItemsFromConstructionStockpile failed");
                return;
            }
            if (__instance == null)
            {
                Logging.Instance.LogWarning(typeof(MySlimBlockPatch), "MySlimBlock instance is null");
                return;
            }
            var key = new Vector4(__instance.Position, __instance.CubeGrid.EntityId);
            if (!stockpileItems.ContainsKey(key))
            {
                Logging.Instance.LogWarning(typeof(MySlimBlockPatch), "Could not find stockpile items for block at position " + __instance.Position + " in grid " + __instance.CubeGrid.EntityId);
                return;
            }
            long gridId = 0;
            long playerId = 0;
            bool isFromCharInventory = false;
            if (toInventory.Entity is MyCharacter character)
            {
                playerId = character.GetPlayerIdentityId();
                if (character.EquippedTool != null)
                {
                    if (!(character.EquippedTool is MyAngleGrinder))
                    {
                        Logging.Instance.LogWarning(typeof(MySlimBlockPatch), "Player " + playerId + " is moving items from construction stockpile to character inventory with a non-grinder tool");
                        return;
                    }
                }
                else
                {
                    Logging.Instance.LogWarning(typeof(MySlimBlockPatch), "Player " + playerId + " is moving items from construction stockpile to character inventory with no hand item");
                    return;
                }
                isFromCharInventory = true;
            }
            else if (toInventory.Entity is MyCubeBlock cubeBlock)
            {
                playerId = cubeBlock.OwnerId;
                gridId = cubeBlock.CubeGrid.EntityId;
                if (!(cubeBlock is MyShipGrinder))
                {
                    Logging.Instance.LogWarning(typeof(MySlimBlockPatch), "Player " + playerId + " is moving items from construction stockpile to block inventory with a non-grinder block");
                    return;
                }
            }
            var faction = MySession.Static.Factions.GetPlayerFaction(playerId);
            var steamId = MySession.Static.Players.TryGetSteamId(playerId);
            var ownerId = __instance.OwnerId;
            if (ownerId == 0)
                ownerId = __instance.BuiltBy;
            if (ownerId == 0)
                ownerId = __instance.CubeGrid.BigOwners.FirstOrDefault();
            if (ownerId == playerId)
            {
                Logging.Instance.LogInfo(typeof(MySlimBlockPatch), "Player " + playerId + " is moving items from construction stockpile to inventory of block they own, ignoring for profession");
                return;
            }
            PlayerStorage playerStorage = null;
            if (isFromCharInventory)
            {
                if (steamId != 0)
                {
                    playerStorage = SEDBStorage.Instance.GetPlayer(steamId);
                }
            }
            else
            {
                if (steamId != 0)
                {
                    playerStorage = SEDBStorage.Instance.GetPlayer(steamId);
                }
                else
                {
                    if (ActiveFunctionalGridController.Controllers.ContainsKey(gridId))
                    {
                        var terminal = ActiveFunctionalGridController.Controllers[gridId].GetFirstTerminal();
                        if (terminal != null)
                        {
                            steamId = terminal.LoggedSteamId;
                            if (steamId != 0)
                            {
                                playerStorage = SEDBStorage.Instance.GetPlayer(steamId);
                            }
                        }
                    }
                }
            }
            if (playerStorage != null)
            {
                if (ProfessionStorage.PROFESSIONS.ContainsKey(playerStorage.Profession))
                {
                    var profession = ProfessionStorage.PROFESSIONS[playerStorage.Profession];
                    if (profession != null)
                    {
                        profession.OnGrinding(steamId, __instance, stockpileItems[key], toInventory);
                    }
                }
            }
            else
            {
                Logging.Instance.LogWarning(typeof(MySlimBlockPatch), "Could not find player storage for playerId " + playerId + " and steamId " + steamId);
            }
        }
    }

}
