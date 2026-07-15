using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Linq;
using VRage.Game.ModAPI;

namespace SEDiscordBridge.Patches
{

    public static class IMyCubeGridExtension
    {

        public static long GetOwnerId(this MyCubeGrid grid)
        {
            return (grid.BigOwners?.Any() ?? false) ? grid.BigOwners.First() : 0;
        }

        public static IMyFaction TryGetFaction(this MyCubeGrid grid)
        {
            return MyAPIGateway.Session.Factions.TryGetPlayerFaction(grid.GetOwnerId());
        }

    }

}