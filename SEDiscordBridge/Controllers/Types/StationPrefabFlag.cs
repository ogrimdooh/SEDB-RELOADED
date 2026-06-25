using System;

namespace SEDiscordBridge.Controllers.Types
{
    [Flags]
    public enum StationPrefabFlag
    {

        None = 0,
        Rover = 1 << 1,
        IonThruster = 1 << 2,
        H2Thruster = 1 << 3,
        AtmThruster = 1 << 4,
        LargeGrid = 1 << 5,
        SmallGrid = 1 << 6,
        Reactor = 1 << 7,
        JumpDrive = 1 << 8

    }
}
