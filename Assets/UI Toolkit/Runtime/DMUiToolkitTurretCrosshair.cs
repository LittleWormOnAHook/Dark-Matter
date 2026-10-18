using Project.Combat.Turrets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    [DefaultExecutionOrder(-359)]
    [DisallowMultipleComponent]
    public sealed class DMUiToolkitTurretCrosshair : MonoBehaviour
    {
        private static DMUiToolkitTurretCrosshair instance;
        private UIDocument document;
        private VisualElement root;
        private VisualElement crossRoot;
        private bool shown;

        public static bool TryShow()
        {
            DMUiToolkitTurretCrosshair host = EnsureHost();
            if (host == null)
                return false;
            host.SetOpen(true);
            return true;
        }

        public static bool TryHide()
        {
            if (instance == null)
                return false;
            instance.SetOpen(false);
            return true;
        }

        private static DMUiToolkitTurretCrosshair EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.TurretCrosshairName,
                DMUiToolkitOverlayDocument.TurretCrosshairUxml,
                DMUiToolkitOverlayDocument.TurretCrosshairUss,
                DMUiToolkitOverlayDocument.TurretCrosshairSort);
            if (doc == null)
                return null;

            DMUiToolkitTurretCrosshair host = doc.GetComponent<DMUiToolkitTurretCrosshair>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitTurretCrosshair>();
            host.document = doc;
            host.root = doc.rootVisualElement;
            host.crossRoot = host.root != null ? host.root.Q<VisualElement>("turret-cross-root") : null;
            instance = host;
            host.SetOpen(false);
            return host;
        }

        private void LateUpdate()
        {
            if (!shown || crossRoot == null)
                return;

            DMTurretProfile profile = DMTurretProfile.Live;
            float bias = profile != null ? profile.PlayerReticleScreenBias : 0.06f;
            float centerY = 50f + bias * 100f;
            ApplyCrosshairCenterPercent(centerY);
        }

        private void ApplyCrosshairCenterPercent(float centerY)
        {
            ApplyElementCenterY(crossRoot.Q<VisualElement>("turret-cross-h"), centerY);
            ApplyElementCenterY(crossRoot.Q<VisualElement>("turret-cross-v"), centerY);
            ApplyElementCenterY(crossRoot.Q<VisualElement>("turret-cross-dot"), centerY);
        }

        private static void ApplyElementCenterY(VisualElement element, float centerY)
        {
            if (element == null)
                return;
            element.style.top = Length.Percent(centerY);
        }

        private void SetOpen(bool value)
        {
            shown = value;
            VisualElement tree = crossRoot != null ? crossRoot : (root != null ? root.Q<VisualElement>("turret-cross-root") ?? root : null);
            DMUiToolkitOverlayDocument.SetShown(tree, value);
        }
    }
}
