using System;
using static SEDiscordBridge.PatchController;
using Sandbox.Game.World;
using Sandbox.Game.Entities;
using VRageMath;
using VRage.Network;
using Sandbox.Game.GameSystems;

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

                            var didJump = SEDBStorage.Instance.GetPlayerValue<bool>(player.Id.SteamId, SEDBStorage.KEY_DID_JUMP);

                            if (Plugin.Config.DisplayOnlyFirstJumpMessage && didJump) return;

                            var msgToUse = didJump ? Plugin.Config.GridJumpMessage : Plugin.Config.FirstGridJumpMessage;
                            msgToUse = msgToUse.Replace("{g}", gridName);
                            msgToUse = msgToUse.Replace("{d}", distance.ToString("#0.0"));
                            Plugin.DDBridge.SendStatusMessage(player.DisplayName, player.Id.SteamId, msgToUse);

                            SEDBStorage.Instance.SetPlayerValue<bool>(player.Id.SteamId, SEDBStorage.KEY_DID_JUMP, true);
                            var jumpCount = SEDBStorage.Instance.GetPlayerValue<int>(player.Id.SteamId, SEDBStorage.KEY_JUMP_COUNT);
                            SEDBStorage.Instance.SetPlayerValue<int>(player.Id.SteamId, SEDBStorage.KEY_JUMP_COUNT, jumpCount + 1);

                            SEDBStorage.Save();
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
