using Sandbox.Game.World;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using VRage.Network;
using static SEDiscordBridge.PatchController;

namespace SEDiscordBridge.Patches
{
    [PatchingClass]
    public class ButtonPanelPatch
    {

        private static SEDiscordBridgePlugin Plugin;

        public static event Action<MyButtonPanel, long, int> OnActivateButton;

        public ButtonPanelPatch(SEDiscordBridgePlugin plugin)
        {
            Plugin = plugin;
        }

        [PostFixMethod]
        [TargetMethod(Type = typeof(MyButtonPanel), Method = "ActivateButton")]
        public static void Trigger(int index, MyButtonPanel __instance)
        {
            Logging.Instance.LogInfo(typeof(ButtonPanelPatch), $"Trigger got executed!");
            if (OnActivateButton != null)
            {
                long num = MySession.Static.Players.TryGetIdentityId(MyEventContext.Current.Sender.Value);
                OnActivateButton.Invoke(__instance, num, index);
            }
        }

    }

}
