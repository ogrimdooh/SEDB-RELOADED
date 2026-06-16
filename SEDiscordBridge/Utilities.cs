using DSharpPlus;
using NLog;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using Torch.API;
using VRage.Plugins;

namespace SEDiscordBridge {

    public class Utils {
        public static ITorchBase Torch { get; }
        public static bool debug = true;
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static string GetSubstringByString(string from, string until, string wholestring) {
            return wholestring.Substring((wholestring.IndexOf(from) + from.Length), (wholestring.IndexOf(until) - wholestring.IndexOf(from) - from.Length));
        }

        public static Dictionary<string, string> ParseQueryString(string queryString) {
            var nvc = HttpUtility.ParseQueryString(queryString);
            return nvc.AllKeys.ToDictionary(k => k, k => nvc[k]);
        }

        public static MyPlayer GetPlayerByNameOrId(string nameOrPlayerId)
        {
            if (!long.TryParse(nameOrPlayerId, out long id))
            {
                foreach (var identity in MySession.Static.Players.GetAllIdentities())
                {
                    if (identity.DisplayName == nameOrPlayerId)
                    {
                        id = identity.IdentityId;
                    }
                }
            }

            if (MySession.Static.Players.TryGetPlayerId(id, out MyPlayer.PlayerId playerId))
            {
                if (MySession.Static.Players.TryGetPlayerById(playerId, out MyPlayer player))
                {
                    return player;
                }
            }

            return null;
        }

        public static MethodInfo FindOverLoadMethod(MethodInfo[] methodInfo, string name, int parameterLenth) {
            MethodInfo method = null;
            foreach (var DecalredMethod in methodInfo) {
                if (debug)
                    Log.Info($"Method name: {DecalredMethod.Name}");
                if (DecalredMethod.GetParameters().Length == parameterLenth && DecalredMethod.Name == name) {
                    method = DecalredMethod;
                    break;
                }
            }
            return method;
        }


        private static string TryGetAsServerName(string defaultToUse)
        {
            if (SEDiscordBridgePlugin.Static != null &&
                SEDiscordBridgePlugin.Static.Config.NameUnknownUserAsServer &&
                !string.IsNullOrWhiteSpace(SEDiscordBridgePlugin.Static.Config.ServerUserName))
                return SEDiscordBridgePlugin.Static.Config.ServerUserName;
            return defaultToUse;
        }

        public static bool IsPlayerAdmin(ulong steamUserID)
        {
            try
            {
                if (MySession.Static != null)
                {
                    return MySession.Static.IsUserAdmin(steamUserID);
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(Utilities), e);
            }
            return false;
        }

        public static string GetPlayerName(ulong steamId)
        {
            long identityId = 0;
            try
            {
                if (MySession.Static?.Players != null)
                {
                    identityId = MySession.Static.Players.TryGetIdentityId(steamId);
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(Utilities), e);
            }
            if (identityId == 0)
                return TryGetAsServerName(steamId.ToString());
            return GetPlayerName(identityId);
        }

        public static string GetPlayerName(long identityId)
        {
            try
            {
                if (MySession.Static?.Players != null)
                {
                    MyPlayer.PlayerId id;
                    if (MySession.Static.Players.TryGetPlayerId(identityId, out id))
                    {
                        var player = MySession.Static.Players.GetPlayerById(id);
                        if (player != null && !string.IsNullOrWhiteSpace(player.DisplayName))
                        {
                            return player.DisplayName;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Instance.LogError(typeof(Utilities), e);
            }
            return TryGetAsServerName(identityId.ToString());
        }
    }
}
