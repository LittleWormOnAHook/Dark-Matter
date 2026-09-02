using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Overlay stamp + hide HUD-appended builder hosts (C# runtime hide, not USS display:none).
    /// Does not add Update (owned by DMUiToolkitHud.XpAmmoEnemy) or LateUpdate (owned by main HUD).
    /// </summary>
    public partial class DMUiToolkitHud
    {
        private static bool overlaysStamped;
        private static readonly string[] HudOverlayHostNames =
        {
            "levelup",
            "quest-dialog",
            "hovercraft",
            "hazards",
            "zone-banner"
        };

        private void Start()
        {
            StampOverlaysOnce();
            HideHudAuthoredOverlayHosts();
            DMUiToolkitHazards.EnsureHost();
        }

        internal static void StampOverlaysOnce()
        {
            if (overlaysStamped)
                return;

            overlaysStamped = true;
            Debug.Log("DMUiToolkit 0901-overlays");
        }

        internal static void HideHudAuthoredOverlayHosts()
        {
            UIDocument hud = DMUiToolkitBootstrap.Instance != null
                ? DMUiToolkitBootstrap.Instance.HudDocument
                : null;
            if (hud == null)
                return;

            VisualElement root = hud.rootVisualElement;
            if (root == null)
                return;

            for (int i = 0; i < HudOverlayHostNames.Length; i++)
            {
                VisualElement host = root.Q<VisualElement>(HudOverlayHostNames[i]);
                DMUiToolkitOverlayDocument.SetShown(host, false);
            }
        }
    }
}
