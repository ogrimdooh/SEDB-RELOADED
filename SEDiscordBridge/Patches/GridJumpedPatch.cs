using Microsoft.Win32;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.Player;
using System;
using VRage.Network;
using VRageMath;
using static SEDiscordBridge.PatchController;
using static VRage.Dedicated.Configurator.SelectInstanceForm;

namespace SEDiscordBridge.Patches
{

    [PatchingClass]
    public class GridJumpedPatch
    {

        private static SEDiscordBridgePlugin Plugin;

        public GridJumpedPatch(SEDiscordBridgePlugin plugin)
        {
            Plugin = plugin;
        }

        [PrefixMethod]
        [TargetMethod(Type = typeof(MyCubeGrid), Method = "OnJumpRequested")]
        public static void OnJumpRequested(Vector3D jumpTarget, long userId, float jumpDriveDelay)
        {
            try
            {
                if (SEDiscordBridgePlugin.DEBUG)
                {
                    Logging.Instance.LogInfo(typeof(SEDiscordBridgePlugin), $"OnJumpRequested: userId={userId}!");
                }

                if (!Plugin.Config.Enabled) return;

                if (!Plugin.Config.DisplayGridsJumpMessages) return;

                if (MySession.Static?.Players != null)
                {
                    MyPlayer.PlayerId id;
                    if (MySession.Static.Players.TryGetPlayerId(userId, out id))
                    {
                        var player = MySession.Static.Players.GetPlayerById(id);
                        if (player != null && !string.IsNullOrWhiteSpace(player.DisplayName))
                        {
                            var gridName = Plugin.Config.UnknowJumpGridName;
                            var cockpit = player.Controller?.ControlledEntity?.Entity as MyCockpit;
                            if (cockpit != null)
                            {
                                gridName = cockpit.CubeGrid.DisplayName;
                            }

                            var distance = Vector3D.Distance(player.GetPosition(), jumpTarget) / 1000;
                            if (distance < 0)
                            {
                                distance *= -1;
                            }

                            var playerStorage = SEDBStorage.Instance.GetPlayer(player.Id.SteamId);

                            if (Plugin.Config.DisplayOnlyFirstJumpMessage && playerStorage.DidJump) return;

                            var msgToUse = playerStorage.DidJump ? Plugin.Config.GridJumpMessage : Plugin.Config.FirstGridJumpMessage;
                            msgToUse = msgToUse.Replace("{g}", gridName);
                            msgToUse = msgToUse.Replace("{d}", distance.ToString("#0.0"));
                            Plugin.DDBridge.SendStatusMessage(player.DisplayName, player.Id.SteamId, msgToUse);

                            playerStorage.DidJump = true;
                            var jumpCount = playerStorage.JumpCount;
                            playerStorage.JumpCount = jumpCount + 1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(GameWatcherController), e);
            }
        }

    }

}
