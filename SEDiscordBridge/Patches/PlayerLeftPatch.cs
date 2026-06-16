using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game;
using Sandbox.Engine;
using Sandbox.Engine.Multiplayer;
using HarmonyLib;
using SEDiscordBridge;
using Sandbox.Game.Gui;
using VRage.Utils;
using static SEDiscordBridge.PatchController;
using VRage.GameServices;
using System.Text.RegularExpressions;

namespace SEDiscordBridge.Patches
{

    [PatchingClass]
    class PlayerLeftPatch
    {

        private static SEDiscordBridgePlugin Plugin;

        public PlayerLeftPatch(SEDiscordBridgePlugin plugin)
        {
            Plugin = plugin;
        }

        [PrefixMethod]
        [TargetMethod(Type = typeof(MyDedicatedServerBase), Method = "MyDedicatedServer_ClientLeft")]
        public static void PlayerDisconnected(ulong user, MyChatMemberStateChangeEnum arg2)
        {
            try
            {
                string playerName = Utils.GetPlayerName(user);
                if (!(playerName.StartsWith("[") && playerName.EndsWith("]") && playerName.Contains("...")) &&
                    (Plugin.Config.NameUnknownUserAsServer && playerName != Plugin.Config.ServerUserName))
                {
                    var msgToUse = Plugin.Config.Leave;
                    msgToUse = msgToUse.Replace("{a}", GetActionTitle(arg2));
                    Task.Run(async () => await Plugin.DDBridge.SendStatusMessage(playerName, user, msgToUse));
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(PlayerLeftPatch), e);
            }
        }

        private static string GetActionTitle(MyChatMemberStateChangeEnum arg2)
        {
            switch (arg2)
            {
                case MyChatMemberStateChangeEnum.Disconnected:
                    return Plugin.Config.ServerDisconnectedAction;
                case MyChatMemberStateChangeEnum.Kicked:
                    return Plugin.Config.ServerKickedAction;
                case MyChatMemberStateChangeEnum.Banned:
                    return Plugin.Config.ServerBannedAction;
                case MyChatMemberStateChangeEnum.Entered:
                case MyChatMemberStateChangeEnum.Left:
                default:
                    return Plugin.Config.ServerLeftAction;
            }
        }

    }

}
