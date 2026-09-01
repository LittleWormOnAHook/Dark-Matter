using Project.Core;
using Project.Vehicles;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK hovercraft status HUD. Visible only while boarded. Same data as HovercraftStatusHudUI.
    /// </summary>
    [DefaultExecutionOrder(-378)]
    [DisallowMultipleComponent]
    public class DMUiToolkitHovercraft : MonoBehaviour
    {
        private const int SegmentCount = 12;

        private static readonly Color ShieldColor = new Color(0.32f, 0.70f, 0.95f, 1f);

        private static DMUiToolkitHovercraft instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement panel;
        private Label nameLabel;
        private Label shieldPct;
        private VisualElement shieldSegs;
        private Label hullPct;
        private Label hullCrit;
        private VisualElement hullSegs;
        private Label fuelPct;
        private VisualElement fuelSegs;
        private Label weaponLabel;
        private Label speedLabel;
        private bool bound;
        private bool wasMounted;
        private HovercraftController cachedCraft;
        private HovercraftHealth cachedHealth;
        private HovercraftFuelSystem cachedFuel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitHovercraft EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.HovercraftName,
                DMUiToolkitOverlayDocument.HovercraftUxml,
                DMUiToolkitOverlayDocument.HovercraftUss,
                DMUiToolkitOverlayDocument.HovercraftSort);
            if (doc == null)
                return null;

            DMUiToolkitHovercraft host = doc.GetComponent<DMUiToolkitHovercraft>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitHovercraft>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            bool wantHud = DMUiToolkitOverlayDocument.GameplayHudWanted();
            bool mounted = PlayerVehicleState.IsMounted;
            if (mounted != wasMounted)
            {
                wasMounted = mounted;
                HandleMountChanged(mounted);
            }

            bool show = wantHud && mounted;
            DMUiToolkitOverlayDocument.SetShown(root, show);
            if (panel != null)
                panel.style.opacity = show ? 1f : 0f;

            HideUgui();

            if (!show)
                return;

            HovercraftController craft = PlayerVehicleState.ActiveCraft;
            if (craft == null)
                return;

            var hovercraftItem = craft.Usable != null ? craft.Usable.HovercraftItem : null;
            if (nameLabel != null)
            {
                nameLabel.text = hovercraftItem != null && !string.IsNullOrEmpty(hovercraftItem.itemName)
                    ? hovercraftItem.itemName
                    : "Hovercraft";
            }

            if (craft != cachedCraft)
            {
                cachedCraft = craft;
                cachedHealth = craft.GetComponent<HovercraftHealth>();
                cachedFuel = craft.GetComponent<HovercraftFuelSystem>();
            }

            if (cachedHealth != null)
            {
                ApplyBar(shieldSegs, shieldPct, cachedHealth.CurrentShield, cachedHealth.MaxShield, ShieldColor, null);
                ApplyBar(hullSegs, hullPct, cachedHealth.CurrentHealth, cachedHealth.MaxHealth, DarkMatterGenesisUiPalette.DangerRed, hullCrit);
            }
            else
            {
                ApplyUnavailable(shieldSegs, shieldPct, "Shield");
                ApplyUnavailable(hullSegs, hullPct, "Hull");
                if (hullCrit != null)
                    DMUiToolkitOverlayDocument.SetShown(hullCrit, false);
            }

            if (cachedFuel != null)
                ApplyBar(fuelSegs, fuelPct, cachedFuel.CurrentFuel, cachedFuel.MaxFuel, DarkMatterGenesisUiPalette.Gold, null);
            else
                ApplyUnavailable(fuelSegs, fuelPct, "Fuel");

            HovercraftProfile profile = craft.Profile;
            var weaponItem = profile != null ? profile.weaponItem : null;
            if (weaponLabel != null)
            {
                weaponLabel.text = weaponItem != null && !string.IsNullOrEmpty(weaponItem.itemName)
                    ? weaponItem.itemName
                    : "Unarmed";
            }

            HoverPhysicsDriver physicsDriver = craft.PhysicsDriver;
            if (speedLabel != null)
            {
                speedLabel.text = physicsDriver != null
                    ? $"{physicsDriver.CurrentPlanarSpeed:0} m/s"
                    : "?";
            }
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("hovercraft-root") ?? tree;
            panel = tree.Q<VisualElement>("hovercraft-panel");
            nameLabel = tree.Q<Label>("hc-name");
            shieldPct = tree.Q<Label>("hc-shield-pct");
            shieldSegs = tree.Q<VisualElement>("hc-shield-segs");
            hullPct = tree.Q<Label>("hc-hull-pct");
            hullCrit = tree.Q<Label>("hc-hull-crit");
            hullSegs = tree.Q<VisualElement>("hc-hull-segs");
            fuelPct = tree.Q<Label>("hc-fuel-pct");
            fuelSegs = tree.Q<VisualElement>("hc-fuel-segs");
            weaponLabel = tree.Q<Label>("hc-weapon");
            speedLabel = tree.Q<Label>("hc-speed");

            DMUiToolkitOverlayDocument.PopulateSegments(shieldSegs, SegmentCount);
            DMUiToolkitOverlayDocument.PopulateSegments(hullSegs, SegmentCount);
            DMUiToolkitOverlayDocument.PopulateSegments(fuelSegs, SegmentCount);
            if (hullCrit != null)
                DMUiToolkitOverlayDocument.SetShown(hullCrit, false);

            DMUiToolkitOverlayDocument.SetShown(root, false);
            bound = root != null;
        }

        private static void ApplyBar(
            VisualElement segs,
            Label pct,
            float current,
            float max,
            Color color,
            Label critical)
        {
            float n = max > 0.01f ? Mathf.Clamp01(current / max) : 0f;
            DMUiToolkitOverlayDocument.ApplySegments(segs, n, color);
            if (pct != null)
                pct.text = $"{Mathf.RoundToInt(n * 100f)}%";

            if (critical != null)
                DMUiToolkitOverlayDocument.SetShown(critical, n > 0f && n <= 0.25f);
        }

        private static void ApplyUnavailable(VisualElement segs, Label pct, string _)
        {
            DMUiToolkitOverlayDocument.ApplySegments(segs, 0f, Color.white);
            if (pct != null)
                pct.text = "?";
        }

        private static void HandleMountChanged(bool mounted)
        {
            ToolBarUI toolbar = FindAnyObjectByType<ToolBarUI>(FindObjectsInactive.Include);
            toolbar?.SetVehicleModeHudSuppressed(mounted);
        }

        private static void HideUgui()
        {
            if (!DMUiToolkitHud.IsDriving)
                return;

            HovercraftStatusHudUI hud = FindAnyObjectByType<HovercraftStatusHudUI>(FindObjectsInactive.Include);
            if (hud == null)
                return;

            Transform panelRoot = hud.transform.Find("HovercraftStatusHud");
            if (panelRoot == null)
            {
                foreach (Transform child in hud.transform)
                {
                    if (child != null && child.name == "HovercraftStatusHud")
                    {
                        panelRoot = child;
                        break;
                    }
                }
            }

            if (panelRoot != null)
                DMUiToolkitOverlayDocument.HideGameObject(panelRoot.gameObject);

            hud.SetGameplayVisible(false);
        }
    }
}
