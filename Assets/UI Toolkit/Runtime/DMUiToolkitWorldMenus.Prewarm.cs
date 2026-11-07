using System.Collections;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    // Round3: hidden pre-warm of the weapon-mode menu (UIDocument/UXML bind + one invisible layout/text pass
    // so font glyphs/atlas work happens at load, not on the first Hold-R / D-pad open).
    public partial class DMUiToolkitWorldMenus
    {
        private static readonly ProfilerMarker s_pmWeaponPrewarm = new ProfilerMarker("WeaponModeSwitch.Prewarm");
        private bool weaponPrewarmed;

        /// <summary>True when the host exists and the weapon panel has had its invisible warm pass.</summary>
        public static bool IsWeaponMenuPrewarmed => instance != null && instance.weaponPrewarmed;

        public static void PrewarmWeaponMenu()
        {
            using (s_pmWeaponPrewarm.Auto())
            {
                if (!Application.isPlaying || !DMUiToolkitHud.IsDriving)
                    return;
                DMUiToolkitWorldMenus host = EnsureHost();
                if (host == null || host.weaponPrewarmed || host.weaponHost == null || host.weaponOpen)
                    return;
                host.StartCoroutine(host.PrewarmWeaponRoutine());
            }
        }

        private IEnumerator PrewarmWeaponRoutine()
        {
            VisualElement h = weaponHost;
            if (h == null || weaponOpen)
                yield break;

            StyleFloat prevOpacity = h.style.opacity;
            PickingMode prevPicking = h.pickingMode;
            h.style.opacity = 0f;
            h.pickingMode = PickingMode.Ignore;
            using (s_pmWeaponPrewarm.Auto())
            {
                DMUiToolkitOverlayDocument.SetShown(h, true);
                RefreshWeapon();
            }

            yield return null; // one layout + text generation pass while fully transparent
            yield return null;

            using (s_pmWeaponPrewarm.Auto())
            {
                if (!weaponOpen && weaponHost == h)
                    DMUiToolkitOverlayDocument.SetShown(h, false);
                h.style.opacity = prevOpacity;
                h.pickingMode = prevPicking;
                weaponPrewarmed = true;
            }
        }
    }
}
