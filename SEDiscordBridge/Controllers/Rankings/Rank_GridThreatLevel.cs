using SEDiscordBridge.Controllers.Grids;
using SEDiscordBridge.Storage;
using SEDiscordBridge.Storage.Registry;
using System.Collections.Generic;
using System.Linq;

namespace SEDiscordBridge.Controllers.Rankings
{
    public class Rank_GridThreatLevel : BaseRankDefinition
    {

        public const string RANK_ID = "grid_threat_level";

        public override string GetId() => RANK_ID;

        public override string GetName() => "TOP 10 — SEASON GRID THREAT LEVEL";

        public override string GetDescription() => @"This ranking tracks the highest-rated registered grids detected by **D.A.W.N.** during the current jump cycle.
Threat Level reflects a grid’s estimated operational impact, including mass, systems, weapons, defenses, production capacity, and strategic value. This value is used for monitoring, balancing, Ark logistics, and PvP protection calculations.
Grid locations are not disclosed by this registry.";
        public override string GetFooter() => @"Records are updated by **D.A.W.N.** during the active season. Rankings may reset after the next Ark Jump.";

        public override string GetIcon() => ":satellite:";

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
            var info = value as MyCubeGridExtraData;
            return string.Format("{0} {1} — **{2}** `{3}` threat level", icon, name, info.Grid.DisplayName, info.ThreatLevel.ToString("F0"));
        }

        public override List<RankEntry> GetEntries()
        {
            var lista = GridObserverController.GetGridsExtraData()
                .Where(x => x.IsOwnerRegistred)
                .OrderByDescending(x => x.ThreatLevel)
                .Take(10)
                .ToList();
            return lista.Select(p => new RankEntry()
            {
                Order = lista.IndexOf(p),
                SteamId = p.OwnerSteamId,
                UserId = RegistryStorage.Instance.GetSteamUserInfo(p.OwnerSteamId).UserId,
                Value = p
            }).ToList();
        }

    }
}
