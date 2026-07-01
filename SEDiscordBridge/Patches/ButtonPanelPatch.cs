using Sandbox.Game;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SEDiscordBridge.Storage.Registry;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using VRage.Network;
using VRage.Utils;
using static SEDiscordBridge.PatchController;

namespace SEDiscordBridge.Patches
{
    [PatchingClass]
    public class MyFactionCollectionPatch
    {

        private static SEDiscordBridgePlugin Plugin;

        public MyFactionCollectionPatch(SEDiscordBridgePlugin plugin)
        {
            Plugin = plugin;
        }

        [PostFixMethod]
        [TargetMethod(Type = typeof(MyFactionCollection), Method = "DamageFactionPlayerReputation")]
        public static void Trigger(long playerIdentityId,
            long attackedIdentityId,
            MyReputationDamageType repDamageType,
            float damageAmount,
            MyFactionCollection __instance)
        {
            if (!Sync.IsServer || attackedIdentityId == 0L)
                return;
            if (MySession.Static == null || MySession.Static.Players == null)
                return;
            var foundPlayerId = MySession.Static.Players.TryGetPlayerId(playerIdentityId, out var playerId);
            var foundAttackedPlayerId = MySession.Static.Players.TryGetPlayerId(attackedIdentityId, out var attackedPlayerId);
            if (!foundPlayerId || !foundAttackedPlayerId)
            {
                Logging.Instance.LogInfo(typeof(MyFactionCollectionPatch), $"DamageFactionPlayerReputation: Could not find playerId for playerIdentityId={playerIdentityId} or attackedIdentityId={attackedIdentityId}");
                return;
            }
            if (playerId.SteamId == 0 || attackedPlayerId.SteamId == 0)
            {
                Logging.Instance.LogInfo(typeof(MyFactionCollectionPatch), $"DamageFactionPlayerReputation: Could not find SteamId for playerId={playerId} or attackedPlayerId={attackedPlayerId}");
                return;
            }
            var isPlayerRegistred = RegistryStorage.Instance.IsSteamUserRegistered(playerId.SteamId);
            var isAttackedPlayerRegistred = RegistryStorage.Instance.IsSteamUserRegistered(attackedPlayerId.SteamId);
            if (!isPlayerRegistred || !isAttackedPlayerRegistred)
            {
                Logging.Instance.LogInfo(typeof(MyFactionCollectionPatch), $"DamageFactionPlayerReputation: Player or attacked player is not registered in the registry. playerId={playerId}, attackedPlayerId={attackedPlayerId}");
                return;
            }

        }

    }
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
