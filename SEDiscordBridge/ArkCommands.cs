using NLog;
using Sandbox.ModAPI;
using SEDiscordBridge.Patches;
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

                Plugin.AlertRegistryIsCompleted();

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
