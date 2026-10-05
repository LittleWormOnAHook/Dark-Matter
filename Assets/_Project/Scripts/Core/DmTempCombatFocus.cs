using Project.Player;

namespace Project.Core
{
    /// <summary>
    /// TEMP COMBAT FOCUS — boot overlay only. World / terrain streaming is gated by
    /// <see cref="DMPlayerSystemsProfile.terrainLoading"/> on the live player (World → Terrain Loading).
    /// </summary>
    public static class DmTempCombatFocus
    {
        // Set true only to skip the boot veil while iterating in Combat_Sandbox.
        public const bool Enabled = false;

        /// <summary>True when World → Terrain Loading is off on the live player systems profile.</summary>
        public static bool SkipWorldStreaming =>
            !DMPlayerSystemsProfile.IsWorldTerrainLoadingEnabled();

        /// <summary>
        /// Boot overlay is claimed at BeforeSceneLoad, before Combat_Sandbox is the active scene.
        /// Gate on <see cref="Enabled"/> so the veil never starts during this combat pass.
        /// </summary>
        public static bool SkipBootOverlay => Enabled;

        public static bool IsCombatSandboxName(string sceneName)
        {
            return sceneName == "Combat_Sandbox";
        }
    }
}
