using Project.Player;
using UnityEngine;

namespace Project.Vehicles
{
    /// <summary>
    /// Static registry for the player's active mounted vehicle.
    /// </summary>
    public static class PlayerVehicleState
    {
        public static HovercraftController ActiveCraft { get; private set; }
        public static PlayerController MountedPlayer { get; private set; }

        public static bool IsMounted => ActiveCraft != null && MountedPlayer != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnDomainReload()
        {
            ActiveCraft = null;
            MountedPlayer = null;
        }

        public static void RegisterMount(HovercraftController craft, PlayerController player)
        {
            ActiveCraft = craft;
            MountedPlayer = player;
        }

        public static void ClearMount(HovercraftController craft, PlayerController player)
        {
            if (ActiveCraft != craft && ActiveCraft != null)
                return;

            if (MountedPlayer != player && MountedPlayer != null)
                return;

            ActiveCraft = null;
            MountedPlayer = null;
        }
    }
}
