using NLog;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SEDiscordBridge.Controllers.Grids;
using SEDiscordBridge.Storage;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace SEDiscordBridge
{

    [Category("ark")]
    public class ArkCommands : CommandModule
    {
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public SEDiscordBridgePlugin Plugin => (SEDiscordBridgePlugin)Context.Plugin;

        [Command("grid", "Manage the ark grid")]
        [Permission(MyPromoteLevel.Admin)]
        public void Grid(string operation, string entry = null)
        {
            if (string.IsNullOrWhiteSpace(operation))
            {
                Log.Warn($"Grid command was call with null operation parameter!");
                return;
            }
            operation = operation.ToLower().Trim();
            entry = entry.ToLower().Trim();
            switch (operation)
            {
                case "set":
                    var target = Context.Player.Character?.Parent;
                    if (target != null && target is IMyCockpit cockpit)
                    {
                        var needReset = false;
                        switch (entry)
                        {
                            case "logistic_relay":
                                needReset = cockpit.CubeGrid.EntityId != SEDBStorage.Instance.FunctionalGrids.LogisticRelayEntityId;
                                SEDBStorage.Instance.FunctionalGrids.LogisticRelayEntityId = cockpit.CubeGrid.EntityId;
                                break;
                            case "ground_base":
                                needReset = cockpit.CubeGrid.EntityId != SEDBStorage.Instance.FunctionalGrids.GroundBaseEntityId;
                                SEDBStorage.Instance.FunctionalGrids.GroundBaseEntityId = cockpit.CubeGrid.EntityId;
                                break;
                        }
                        Log.Info($"Ark interactive grid for '{entry}' configure to {cockpit.CubeGrid.DisplayName} [{cockpit.CubeGrid.EntityId}]");
                        if (needReset)
                        {
                            switch (entry)
                            {
                                case "logistic_relay":
                                    ArkLogisticRelayController.Init();
                                    break;
                                case "ground_base":
                                    ArkGroundBaseController.Init();
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Log.Warn($"Grid SET command was call out of the target grid cockpit!");
                    }
                    break;
                case "reset":
                    ArkLogisticRelayController.Init();
                    ArkGroundBaseController.Init();
                    break;
            }

        }

        [Command("bank", "Manage the ark bank account (valid operatios: deposit, withdraw)")]
        [Permission(MyPromoteLevel.None)]
        public void Bank(string operation, ulong value = 0)
        {
            if (string.IsNullOrWhiteSpace(operation))
            {
                Log.Warn($"Bank command was call with null operation parameter!");
                return;
            }
            operation = operation.ToLower().Trim();

            if (Context.Player.SteamUserId == 0)
            {
                Log.Warn($"Bank command was call by a user with no SteamUserId!");
                return;
            }

            if (!SEDBStorage.Instance.Registry.FindUserBySteamId(Context.Player.SteamUserId, out ulong userId))
            {
                Log.Warn($"Bank command was call by a not registred user!");
                return;
            }

            if (value < SEDBStorage.Instance.Bank.MinOperationValue)
            {
                Log.Warn($"Bank command was call with value less than minimal!");
                return;
            }

            var acc = SEDBStorage.Instance.Bank.GetBankAccount(userId);

            if (MyBankingSystem.Static == null)
            {
                Log.Warn($"MyBankingSystem is null!");
                return;
            }

            var tv = MyBankingSystem.GetBalance(Context.Player.IdentityId);
            var balance = (ulong)(tv >= 0 ? tv : 0);

            switch (operation)
            {
                case "deposit":
                    if (balance < value)
                    {
                        Log.Warn($"Not sufficient founds to Deposit!");
                        return;
                    }
                    var finalDValue = (ulong)(value * SEDBStorage.Instance.Bank.DepositFactor);
                    if (finalDValue <= 0)
                    {
                        Log.Warn($"Final value got below 0 on Deposit action!");
                        return;
                    }
                    if (acc.DoDeposit(finalDValue, value))
                    {
                        MyBankingSystem.ChangeBalance(Context.Player.IdentityId, -(long)value);
                    }
                    else
                    {
                        Log.Warn($"Not abble to do Deposit action!");
                        return;
                    }
                    break;
                case "withdraw":
                    if (acc.Balance < value)
                    {
                        Log.Warn($"Not sufficient founds to Withdraw!");
                        return;
                    }
                    var finalWValue = (ulong)(value * SEDBStorage.Instance.Bank.WithdrawFactor);
                    if (finalWValue <= 0)
                    {
                        Log.Warn($"Final value got below 0 on Withdraw action!");
                        return;
                    }
                    if (acc.DoWithdraw(value, finalWValue))
                    {
                        MyBankingSystem.ChangeBalance(Context.Player.IdentityId, (long)finalWValue);
                    }
                    else
                    {
                        Log.Warn($"Not abble to do Withdraw action!");
                        return;
                    }
                    break;
                default:
                    Log.Warn($"Bank command was call with a invalid operation={operation}!");
                    break;
            }
        }

        [Command("registry", "Registry player on ark systems")]
        [Permission(MyPromoteLevel.None)]
        public void Registry(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Log.Warn($"Registry command was call with null token parameter!");
                return;
            }

            if (SEDBStorage.Instance.Registry.IsTokenValid(token, out ulong userId))
            {

                if (SEDBStorage.Instance.Registry.IsUserRegistered(userId))
                {
                    Log.Warn($"Registry command was call by a registred user!");
                    return;
                }

                if (Context.Player.SteamUserId == 0)
                {
                    Log.Warn($"Registry command was call by a user with no SteamUserId!");
                    return;
                }

                SEDBStorage.Instance.Registry.DoRegistryUser(userId, Context.Player.SteamUserId);
                SEDBStorage.Instance.Registry.DoUseToken(token);

                Plugin.AlertRegistryIsCompleted(userId);

                MyAPIGateway.Parallel.Start(() => {
                    Plugin.CompleteRegistryToUser(userId).Wait();
                });

            }
            else
            {
                Log.Warn($"Registry command was call with invalid token!");
                return;
            }

        }
    }
}
