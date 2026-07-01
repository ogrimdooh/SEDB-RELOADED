namespace SEDiscordBridge.Controllers.Players
{
    public sealed class PlayerLevelInfo
    {
        public ulong SteamId { get; set; }
        public long PlayerId { get; set; }
        public int Level { get; set; }
        public float FinalExperience { get; set; }
        public float ThreatLevel { get; set; }
    }

}
