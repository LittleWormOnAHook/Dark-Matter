namespace Project.Core
{
    /// <summary>
    /// TEMP COMBAT FOCUS — flip <see cref="Enabled"/> to false to restore full world/expedition load.
    ///
    /// Why: pause Gaia tile streaming, content scenes, and New Expedition tile-wait so Combat_Sandbox
    /// iteration is faster. Does not edit Player_v7 / enemy prefabs.
    ///
    /// Restore checklist:
    /// 1. Set <see cref="Enabled"/> to false (this file).
    /// 2. Copy the combat-scene player instance back onto Dark Matter Genesis v1.6.5
    ///    (do not treat this flag as a prefab change).
    /// 3. Ctrl+R. New Expedition waits for Gaia tiles + content scenes again.
    /// 4. Leave dirty PCG/shader files alone unless you explicitly want that work compiled.
    /// </summary>
    public static class DmTempCombatFocus
    {
        // TEMP COMBAT FOCUS — set false to restore terrain / expedition streaming.
        public const bool Enabled = true;

        public static bool SkipWorldStreaming => Enabled;

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
