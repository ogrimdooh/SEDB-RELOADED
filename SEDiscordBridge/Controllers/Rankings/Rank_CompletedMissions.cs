using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.Registry;
using System.Collections.Generic;
using System.Linq;

namespace SEDiscordBridge.Controllers.Rankings
{
    public class Rank_CompletedMissions : BaseRankDefinition
    {

        public const string RANK_ID = "completed_missions";

        public override string GetId() => RANK_ID;

        public override string GetName() => "TOP 10 — SEASON COMPLETED MISSIONS";

        public override string GetDescription() => @"This ranking tracks registered explorers who have completed the highest number of verified missions during the current jump cycle.
Each completed mission strengthens field operations, expands Ark influence, and helps prepare **The Second Dawn** for the next jump.";
        public override string GetFooter() => @"Records are updated by **D.A.W.N.** during the active season. Rankings may reset after the next Ark Jump.";

        public override string GetIcon() => ":receipt:";

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

        public override string GetValueFormated(string icon, string name, float value)
        {
            return string.Format("{0} {1} — `{2}` missions completed", icon, name, ((long)value).ToString());
        }

        public override List<RankEntry> GetEntries()
        {
            var lista = SEDBStorage.Instance.Players
                .Where(x => RegistryStorage.Instance.IsSteamUserRegistered(x.SteamId))
                .OrderByDescending(p => p.AllContractsCount)
                .Take(10)
                .ToList();
            return lista.Select(p => new RankEntry()
            {
                Order = lista.IndexOf(p),
                SteamId = p.SteamId,
                UserId = RegistryStorage.Instance.GetSteamUserInfo(p.SteamId).UserId,
                Value = p.AllContractsCount
            }).ToList();
        }

    }
}
