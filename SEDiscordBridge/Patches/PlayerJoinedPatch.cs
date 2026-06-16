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
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace SEDiscordBridge.Patches
{

    [PatchingClass]
    public class PlayerJoinedPatch
    {

        private static SEDiscordBridgePlugin Plugin;

        public PlayerJoinedPatch(SEDiscordBridgePlugin plugin)
        {
            Plugin = plugin;
        }


        [PrefixMethod]
        [TargetMethod(Type = typeof(MyMultiplayerBase), Method = "RaiseClientJoined")]
        public static void PlayerConnected(ulong changedUser, string userName)
        {
            try
            {
                Task.Run(async () => await Plugin.DDBridge.SendStatusMessage(userName.Replace("", ""), changedUser, Plugin.Config.Join));
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(PlayerJoinedPatch), e);
            }
        }

    }

}
