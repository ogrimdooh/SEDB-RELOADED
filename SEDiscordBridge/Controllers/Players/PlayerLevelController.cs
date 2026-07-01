using Sandbox.Game.World;
using SEDiscordBridge.Controllers.Grids;
using SEDiscordBridge.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEDiscordBridge.Controllers.Players
{

    public static class PlayerLevelController
    {

        public static int CalculatePlayerLevel(double contractExperience, double totalGridThreat)
        {
            var totalScore = Math.Max(0, contractExperience + totalGridThreat);
            var level = Math.Floor(1 + 10 * Math.Log(1 + (totalScore / 1000.0)));
            return Math.Max(1, (int)level);
        }

        public static PlayerLevelInfo GetPlayerLevel(ulong steamId)
        {
            var playerStorage = SEDBStorage.Instance.GetPlayer(steamId);
            if (playerStorage == null)
                return new PlayerLevelInfo() { SteamId = steamId };
            var playerId = MySession.Static.Players.TryGetIdentityId(steamId);
            var threatLevel = GridObserverController.GetPlayerThreatLevel(playerId);
            return new PlayerLevelInfo
            {
                SteamId = steamId,
                PlayerId = playerId,
                Level = CalculatePlayerLevel(playerStorage.FinalExperience, threatLevel),
                FinalExperience = playerStorage.FinalExperience,
                ThreatLevel = threatLevel
            };
        }

    }

}
