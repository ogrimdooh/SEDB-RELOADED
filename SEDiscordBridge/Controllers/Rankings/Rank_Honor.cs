using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.Registry;
using System.Collections.Generic;
using System.Linq;

namespace SEDiscordBridge.Controllers.Rankings
{
    public class Rank_Honor : BaseRankDefinition
    {

        public const string RANK_ID = "season_honor";

        public override string GetId() => RANK_ID;

        public override string GetName() => "TOP 10 — SEASON HONOR";

        public override string GetDescription() => @"This ranking tracks registered explorers with the highest honor records during the current jump cycle.
Honor reflects trusted conduct, fair engagement, support for Ark operations, and behavior recognized by **D.A.W.N.** as aligned with the survival of **The Second Dawn**.";
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
            return string.Format("{0} {1} — `{2}` honor points", icon, name, ((long)value).ToString());
        }

        public override List<RankEntry> GetEntries()
        {
            var lista = SEDBStorage.Instance.Players
                .Where(x => RegistryStorage.Instance.IsSteamUserRegistered(x.SteamId))
                .OrderByDescending(p => p.FinalReputation)
                .Take(10)
                .ToList();
            return lista.Select(p => new RankEntry()
            {
                Order = lista.IndexOf(p),
                SteamId = p.SteamId,
                UserId = RegistryStorage.Instance.GetSteamUserInfo(p.SteamId).UserId,
                Value = p.FinalReputation
            }).ToList();
        }

    }
}
