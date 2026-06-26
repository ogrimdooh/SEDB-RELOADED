namespace SEDiscordBridge.Controllers.Rankings
{
    public class RankEntry
    {

        public int Order { get; set; }
        public ulong SteamId { get; set; }
        public ulong UserId { get; set; }
        public float Value { get; set; }

    }
}
