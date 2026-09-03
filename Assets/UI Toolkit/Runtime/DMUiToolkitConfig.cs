using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Runtime kill-switch for the UI Toolkit dual-run path.
    /// Default is enabled. Missing asset also counts as enabled so Play is not blocked.
    /// Disable this asset (or uncheck <see cref="enabled"/>) to keep uGUI-only.
    /// Hierarchy kill-switch: disable GameObject "UITK_Root".
    /// </summary>
    [CreateAssetMenu(
        fileName = "DMUiToolkitConfig",
        menuName = "Dark Matter Genesis/UI/Toolkit Config")]
    public class DMUiToolkitConfig : ScriptableObject
    {
        public const string ResourcePath = "DMUiToolkitConfig";
        public const string LogStamp = "DMUiToolkit 0831-hide";

        [Tooltip("When false, Loading overlay and future Toolkit screens stay on uGUI.")]
        public bool enabled = true;

        [Header("Zone Vignette")]
        [Tooltip("Overlay alpha at the outer rim of a hazard zone.")]
        [Range(0f, 1f)]
        public float zoneVignetteAlphaMin = 0.1f;

        [Tooltip("Overlay alpha at the inner / center of a hazard zone.")]
        [Range(0f, 1f)]
        public float zoneVignetteAlphaMax = 0.6f;

        [Tooltip("Peak alpha of the combat hit edge flash.")]
        [Range(0f, 1f)]
        public float damageVignetteAlpha = 0.5f;

        [Header("Pilot Cluster Prototype")]
        [Tooltip("Show the lower-right combined minimap / stats prototype. Existing HUD stays up.")]
        public bool showPilotCluster = true;

        private static DMUiToolkitConfig cached;

        public static DMUiToolkitConfig Instance
        {
            get
            {
                if (cached != null)
                    return cached;

                cached = Resources.Load<DMUiToolkitConfig>(ResourcePath);
                return cached;
            }
        }

        /// <summary>True when Toolkit visuals should run. Missing config defaults to true.</summary>
        public static bool IsEnabled
        {
            get
            {
                DMUiToolkitConfig config = Instance;
                return config == null || config.enabled;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            cached = null;
        }
    }
}
