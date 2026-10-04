namespace Project.Core
{
    /// <summary>
    /// TEMP COMBAT FOCUS — <see cref="Enabled"/> is false, so world / expedition load stays on.
    ///
    /// Set <see cref="Enabled"/> true only to pause Gaia tile streaming, content scenes, and the
    /// New Expedition tile-wait while iterating in Combat_Sandbox. Does not edit Player_v7 / enemy prefabs.
    /// </summary>
    public static class DmTempCombatFocus
    {
        // World / expedition streaming is on. Set true only while iterating in Combat_Sandbox.
        public const bool Enabled = false;

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
