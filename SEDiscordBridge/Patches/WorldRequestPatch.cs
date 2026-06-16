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
using Sandbox;
using VRage.Network;
using System.Reflection;

namespace SEDiscordBridge.Patches
{

    [PatchingClass]
    public class WorldRequestPatch
    {
        private static SEDiscordBridgePlugin Plugin;

        public WorldRequestPatch(SEDiscordBridgePlugin plugin)
        {
            Plugin = plugin;
        }

        //[PrefixMethod]
        [TargetMethod(Type = typeof(MyMultiplayerServerBase), Method = "OnWorldRequest")]
        public static bool PatchGetWorld(EndpointId sender)
        {
            var bridge = Plugin.DDBridge;

            Logging.Instance.LogInfo(typeof(WorldRequestPatch), $"Patched World request received: {MyMultiplayer.Static.GetMemberName(sender.Value)}");

            if (!DiscordBridge.Ready)
            {
                var _raiseClientLeft = typeof(MyMultiplayerServerBase).GetMethod("RaiseClientLeft", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                _raiseClientLeft.Invoke(null, new object[] { MyMultiplayer.Static, sender.Value, MyChatMemberStateChangeEnum.Disconnected });
                return false;
            }
            return true;
        }
    }

}
