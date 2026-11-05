using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// 2026-11-04f: which of the two resident worlds (Mars boot scene / IO v1.7) is currently shown.
    ///
    /// Mars and IO stay loaded at the same time after the first IO load, so "which scene is active"
    /// is not enough for world systems: several of them iterate ALL loaded content (notably
    /// <c>Terrain.activeTerrains</c>). With both scenes resident, the Mars terrain is still enabled
    /// while IO is showing, so anything that samples the terrain under the player finds Mars ground
    /// under IO coordinates and teleports him there.
    ///
    /// This holder is the single source of truth for scope questions. <see cref="DMSceneStack"/> owns
    /// it; everything else only reads it. It deliberately lives in Project.Core so world/player code
    /// does not have to reference Project.UI.
    /// </summary>
    public static class DMWorldScope
    {
        public enum World
        {
            Mars = 0,
            Io = 1
        }

        public const string MarsSceneName = "Dark Matter Mars-The Beginning";
        public const string IoSceneName = "Dark Matter Genesis v1.7";

        /// <summary>True once the IO scene has been loaded on top of Mars. Both worlds are then resident.</summary>
        public static bool DualWorldActive { get; private set; }

        /// <summary>The world the player is meant to see and stand in.</summary>
        public static World Current { get; private set; } = World.Mars;

        public static string CurrentSceneName => Current == World.Io ? IoSceneName : MarsSceneName;

        public static void Set(World world, bool dualWorldActive)
        {
            Current = world;
            DualWorldActive = dualWorldActive;
        }

        public static void ResetToMars()
        {
            Current = World.Mars;
            DualWorldActive = false;
        }

        /// <summary>
        /// True when the object belongs to the world the player is currently in, or when only Mars is
        /// loaded. Used to skip content that is still resident but no longer on screen.
        /// </summary>
        public static bool BelongsToCurrentWorld(Component component)
        {
            if (component == null)
                return false;
            if (!DualWorldActive)
                return true;

            string sceneName = component.gameObject.scene.name;
            if (string.IsNullOrEmpty(sceneName))
                return true; // runtime-created objects (DontDestroyOnLoad) belong to the live world

            return string.Equals(sceneName, CurrentSceneName, System.StringComparison.OrdinalIgnoreCase);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = World.Mars;
            DualWorldActive = false;
        }
    }
}