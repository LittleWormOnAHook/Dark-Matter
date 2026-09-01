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
