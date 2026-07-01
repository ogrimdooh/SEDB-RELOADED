using SEDiscordBridge.Controllers.Players;
using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.Registry;
using System.Collections.Generic;
using System.Linq;

namespace SEDiscordBridge.Controllers.Rankings
{
    public class Rank_Experience : BaseRankDefinition
    {

        public const string RANK_ID = "season_experience";

        public override string GetId() => RANK_ID;

        public override string GetName() => "TOP 10 — SEASON LEVEL";

        public override string GetDescription() => @"This ranking tracks registered explorers with the highest level records during the current jump cycle.
Level reflects overall activity, field operations, completed missions, exploration, Ark contributions, and operational presence recognized by **D.A.W.N.**.";
        public override string GetFooter() => @"Records are updated by **D.A.W.N.** during the active season. Rankings may reset after the next Ark Jump.";

        public override string GetIcon() => ":balance_scale:";

        public override string GetIconForOrder(int order)
        {
            switch (order)
            {
                case 0:
                    return ":first_place:";
                case 1:
                    return ":second_place:";
                case 2:
                    return ":third_place:";
                default:
                    return string.Format("`{0}.`", (order + 1).ToString("00"));
            }
        }

        public override string GetValueFormated(string icon, string name, object value)
        {
            if (value is PlayerLevelInfo info)
                return string.Format("{0} {1} — **Level `{2}`** [`{3}` experience] [`{4}` threat level]", icon, name, 
                    info.Level.ToString(), info.FinalExperience.ToString("F0"), info.ThreatLevel.ToString("F0"));
            return "";
        }

        public override List<RankEntry> GetEntries()
        {
            var lista = SEDBStorage.Instance.Players
                .Where(x => RegistryStorage.Instance.IsSteamUserRegistered(x.SteamId))
                .Select(x => PlayerLevelController.GetPlayerLevel(x.SteamId))
                .OrderByDescending(p => p.Level)
                .Take(10)
                .ToList();
            return lista.Select(p => new RankEntry()
            {
                Order = lista.IndexOf(p),
                SteamId = p.SteamId,
                UserId = RegistryStorage.Instance.GetSteamUserInfo(p.SteamId).UserId,
                Value = p
            }).ToList();
        }

    }
}
