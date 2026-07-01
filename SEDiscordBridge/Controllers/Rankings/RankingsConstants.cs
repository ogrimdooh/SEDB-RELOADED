using System.Windows.Documents;

namespace SEDiscordBridge.Controllers.Rankings
{
    public static class RankingsConstants
    {
        public static readonly BaseRankDefinition[] RANK_DEFINITIONS = new BaseRankDefinition[]
        {
            new Rank_Experience(),
            new Rank_Honor(),
            new Rank_GridThreatLevel(),
            new Rank_CompletedMissions(),
        };
    }
}
