using System;

namespace SEDiscordBridge.Controllers.Types
{
    [Flags]
    public enum FactionType
    {

        None = 0,

        Miner = 1 << 1,
        Lumber = 1 << 2,
        Shipyard = 1 << 3,
        Armory = 1 << 4,
        Trader = 1 << 5,
        Farming = 1 << 6,
        Livestock = 1 << 7,
        Market = 1 << 8,

        All = Miner | Lumber | Shipyard | Armory | Trader | Farming | Livestock | Market

    }
}
