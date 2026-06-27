using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SEDiscordBridge.Controllers.Economics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Entity.EntityComponents;
using VRage.Game.ModAPI;

namespace SEDiscordBridge.Controllers
{
    public static class FactionsController
    {

        public const string FACTION_2DAWN_TAG = "2DAWN";
        public const long MIN_BANK_BALANCE = long.MaxValue / 4;


        private static MyFaction _faction2Dawn;
        public static MyFaction FACTION_2DAWN
        {
            get
            {
                if (_faction2Dawn == null)
                {
                    _faction2Dawn = GetFactionInfo(FACTION_2DAWN_TAG);
                }
                return _faction2Dawn;
            }
        }

        public static MyFaction GetFactionInfo(string factionTag)
        {
            var factions = MySession.Static.Factions;
            var faction = factions.TryGetFactionByTag(factionTag);
            if (faction != null)
            {
                return faction;
            }
            return null;
        }

        public static void ResetMainFactionBank()
        {
            var factionId = FACTION_2DAWN?.FactionId ?? 0;
            var founderId = FACTION_2DAWN?.FounderId ?? 0;
            var idsToBalance = new long[] { factionId, founderId };
            foreach (var id in idsToBalance)
            {
                if (id == 0)
                    continue;
                var balance = MyBankingSystem.GetBalance(id);
                if (balance < MIN_BANK_BALANCE)
                {
                    if (balance == -1)
                    {
                        MyBankingSystem.Static.CreateAccount(id, MIN_BANK_BALANCE);
                        Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Creating bank account for ID {id} with balance {MIN_BANK_BALANCE}.");
                    } 
                    else
                    {
                        var valueToAdd = MIN_BANK_BALANCE - balance;
                        Logging.Instance.LogInfo(typeof(EconomicsConstants), $"Resetting bank balance for ID {id} from {balance} to {valueToAdd}.");
                        MyBankingSystem.ChangeBalance(id, valueToAdd);
                    }
                }
            }
        }

        public static bool ChangeGridOwnerToMainFaction(MyCubeGrid grid)
        {
            if (grid == null)
                return false;
            var mainFaction = FACTION_2DAWN;
            if (mainFaction == null)
                return false;
            var ownerId = mainFaction.FounderId;
            foreach (var item in grid.BigOwners.Concat(grid.SmallOwners).Where(x => x != ownerId).Distinct())
            {
                grid.TransferBlocksBuiltByID(item, ownerId);
            }
            grid.ChangeGridOwnership(ownerId, MyOwnershipShareModeEnum.Faction);
            var gridsToCheck = grid.GetConnectedGrids(GridLinkTypeEnum.Mechanical);
            List<MyCubeGrid> subGrids = new List<MyCubeGrid>();
            while (gridsToCheck.Any())
            {
                var gridToCheck = gridsToCheck.First();
                gridsToCheck.Remove(gridToCheck);
                if (!subGrids.Contains(gridToCheck))
                {
                    subGrids.Add(gridToCheck);
                    var othersGrids = gridToCheck.GetConnectedGrids(GridLinkTypeEnum.Mechanical);
                    foreach (var otherGrid in othersGrids)
                    {
                        if (!subGrids.Contains(otherGrid))
                        {
                            gridsToCheck.Add(otherGrid);
                        }
                    }
                }
            }
            Logging.Instance.LogInfo(typeof(FactionsController), $"Changing ownership of {subGrids.Count} subgrids to main faction.");
            foreach (var subGrid in subGrids)
            {
                subGrid.ChangeGridOwnership(ownerId, MyOwnershipShareModeEnum.Faction);
            }
            var terminalSystem = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
            if (terminalSystem != null)
            {
                var group = terminalSystem.GetBlockGroupWithName("SHARED-BLOCKS");
                if (group != null)
                {
                    var sharedBlocks = new List<IMyTerminalBlock>();
                    group.GetBlocks(sharedBlocks);
                    Logging.Instance.LogInfo(typeof(FactionsController), $"Changing {sharedBlocks.Count} shared blocks to access level to all.");
                    foreach (var block in sharedBlocks)
                    {
                        (block as MyTerminalBlock).ChangeOwner(ownerId, MyOwnershipShareModeEnum.All);
                    }
                }
            }
            return true;
        }

        public static void MakePirateFactionsEnemiesOfEveryOne()
        {
            List<MyFaction> list = new List<MyFaction>();
            foreach (KeyValuePair<long, MyFaction> faction in MySession.Static.Factions)
            {
                if (faction.Value.IsEveryoneNpc() && faction.Value.FactionTypeString == MyFactionTypes.Pirate.ToString())
                {
                    list.Add(faction.Value);
                }
            }
            if (list.Any())
            {
                foreach (var item in list)
                {
                    item.DefaultFactionRelationshipAndReputation = new System.Tuple<MyRelationsBetweenFactions, int>(MyRelationsBetweenFactions.Enemies, -1500);
                    item.StartingReputation = -1500;
                    foreach (KeyValuePair<long, MyFaction> faction in MySession.Static.Factions)
                    {
                        MySession.Static.Factions.SetReputationBetweenFactions(faction.Key, item.FactionId, -1500);
                    }
                    foreach (var player in MySession.Static.Players.GetAllPlayers())
                    {
                        var id = MySession.Static.Players.TryGetIdentityId(player.SteamId);
                        if (id != 0)
                        {
                            MySession.Static.Factions.SetReputationBetweenPlayerAndFaction(id, item.FactionId, -1500, ReputationChangeReason.Admin);
                        }
                    }
                }
            }
        }

    }

}
