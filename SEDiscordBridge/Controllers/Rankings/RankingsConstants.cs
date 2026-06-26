using System.Windows.Documents;

namespace SEDiscordBridge.Controllers.Rankings
{
    public static class RankingsConstants
    {
        public static readonly BaseRankDefinition[] RANK_DEFINITIONS = new BaseRankDefinition[]
        {
            new Rank_CompletedMissions()
        };
    }
}
