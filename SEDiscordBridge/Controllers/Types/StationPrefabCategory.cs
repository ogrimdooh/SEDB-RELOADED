using System;

namespace SEDiscordBridge.Controllers.Types
{
    [Flags]
    public enum StationPrefabCategory
    {

        None = 0,
        Tiny = 1 << 1,
        Small = 1 << 2,
        Medium = 1 << 3,
        Big = 1 << 4,
        Huge = 1 << 5,

        TinyToMedium = Tiny | Small | Medium,
        TinyToBig = Tiny | Small | Medium | Big,
        All = Tiny | Small | Medium | Big | Huge

    }
}
