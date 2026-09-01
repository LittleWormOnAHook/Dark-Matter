using Project.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Vessel status HUD shown while the player is mounted in the hovercraft. Styled after the
    /// uploaded "Ship Status HUD" reference (vessel header, segmented Shield/Hull/Fuel bars with
    /// percentage readouts, a flashing "CRITICAL" hull pill, and a Weapon/Speed footer) but recolored
    /// to the project's existing navy/fuchsia palette. Built at runtime, no prefab — same convention
    /// as the other HUD widgets. Pinned to the bottom-right with the same edge gap the
    /// Temperature/Hazards cluster uses on the bottom-left (ScreenEdgeGap = 30) so the two panels
    /// mirror each other.
    /// </summary>
    public class HovercraftStatusHudUI : MonoBehaviour
    {
        private const float PanelWidth = 300f;
        private const float ScreenEdgeGap = 30f;

        private RectTransform panelRoot;
        private TextMeshProUGUI vesselNameLabel;
        private TextMeshProUGUI weaponValueLabel;
        private TextMeshProUGUI speedValueLabel;
        private VehicleStatSegmentBar shieldBar;
        private VehicleStatSegmentBar healthBar;
        private VehicleStatSegmentBar fuelBar;
        private bool uiBuilt;
        private bool gameplayVisible = true;
        private bool wasMountedLastFrame;

        // Cached alongside the craft reference so the per-frame Update() readout doesn't pay for a
        // GetComponent call every frame while mounted — only re-resolved when the active craft changes.
        private HovercraftController cachedCraft;
        private HovercraftHealth cachedHealth;
        private HovercraftFuelSystem cachedFuel;

        public void EnsureBuilt(Transform canvasRoot)
        {
            if (uiBuilt || canvasRoot == null)
                return;

            panelRoot = new GameObject("HovercraftStatusHud", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup))
                .GetComponent<RectTransform>();
            panelRoot.SetParent(canvasRoot, false);
            panelRoot.anchorMin = new Vector2(1f, 0f);
            panelRoot.anchorMax = new Vector2(1f, 0f);
            panelRoot.pivot = new Vector2(1f, 0f);
            panelRoot.sizeDelta = new Vector2(PanelWidth, 0f);
            panelRoot.anchoredPosition = new Vector2(-ScreenEdgeGap, ScreenEdgeGap);

            Image panelBg = panelRoot.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelBg);
            panelBg.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.9f);
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(panelRoot.gameObject, new Vector2(1.2f, -1.2f));

            VerticalLayoutGroup group = panelRoot.GetComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(14, 14, 12, 12);
            group.spacing = 10f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            ContentSizeFitter fitter = panelRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildHeader(panelRoot);
            CreateDivider(panelRoot);

            shieldBar = new VehicleStatSegmentBar(panelRoot, VehicleStatIconKind.Shield, "Shield", new Color(0.32f, 0.70f, 0.95f, 1f), this);
            healthBar = new VehicleStatSegmentBar(panelRoot, VehicleStatIconKind.Hull, "Hull", DarkMatterGenesisUiPalette.DangerRed, this, showCriticalPill: true);
            fuelBar = new VehicleStatSegmentBar(panelRoot, VehicleStatIconKind.Fuel, "Fuel", DarkMatterGenesisUiPalette.Gold, this);

            CreateDivider(panelRoot);
            BuildFooter(panelRoot);

            uiBuilt = true;
            panelRoot.gameObject.SetActive(false);
        }

        private void BuildHeader(Transform parent)
        {
            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            header.transform.SetParent(parent, false);

            LayoutElement layout = header.GetComponent<LayoutElement>();
            layout.preferredHeight = 40f;
            layout.minHeight = 40f;

            VerticalLayoutGroup group = header.GetComponent<VerticalLayoutGroup>();
            group.spacing = 1f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            CreateLabel(header.transform, "VESSEL", 10f, FontStyles.Normal, DarkMatterGenesisUiPalette.MutedText, 14f);
            vesselNameLabel = CreateLabel(header.transform, "Hovercraft", 18f, FontStyles.Bold, DarkMatterGenesisUiPalette.BodyText, 24f);
        }

        private void BuildFooter(Transform parent)
        {
            GameObject footer = new GameObject("Footer", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            footer.transform.SetParent(parent, false);

            LayoutElement footerLayout = footer.GetComponent<LayoutElement>();
            footerLayout.preferredHeight = 34f;
            footerLayout.minHeight = 34f;

            HorizontalLayoutGroup footerGroup = footer.GetComponent<HorizontalLayoutGroup>();
            footerGroup.spacing = 16f;
            footerGroup.childControlWidth = true;
            footerGroup.childControlHeight = true;
            footerGroup.childForceExpandWidth = true;
            footerGroup.childForceExpandHeight = false;

            GameObject weaponColumn = CreateFooterColumn(footer.transform, "WEAPON", out weaponValueLabel, TextAlignmentOptions.MidlineLeft);
            GameObject speedColumn = CreateFooterColumn(footer.transform, "SPEED", out speedValueLabel, TextAlignmentOptions.MidlineRight);
            speedColumn.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperRight;
            weaponColumn.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
        }

        private static GameObject CreateFooterColumn(Transform parent, string caption, out TextMeshProUGUI valueLabel, TextAlignmentOptions valueAlignment)
        {
            GameObject column = new GameObject($"Footer_{caption}", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            column.transform.SetParent(parent, false);

            LayoutElement columnLayout = column.GetComponent<LayoutElement>();
            columnLayout.flexibleWidth = 1f;

            VerticalLayoutGroup columnGroup = column.GetComponent<VerticalLayoutGroup>();
            columnGroup.spacing = 2f;
            columnGroup.childControlWidth = true;
            columnGroup.childControlHeight = true;
            columnGroup.childForceExpandWidth = true;
            columnGroup.childForceExpandHeight = false;

            TextMeshProUGUI captionLabel = CreateLabel(column.transform, caption, 9f, FontStyles.Bold, DarkMatterGenesisUiPalette.MutedText, 12f);
            captionLabel.alignment = valueAlignment == TextAlignmentOptions.MidlineRight ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;

            valueLabel = CreateLabel(column.transform, "—", 14f, FontStyles.Bold, DarkMatterGenesisUiPalette.BodyText, 18f);
            valueLabel.alignment = valueAlignment;

            return column;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, FontStyles style, Color color, float lineHeight)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);

            LayoutElement layout = labelObject.GetComponent<LayoutElement>();
            layout.preferredHeight = lineHeight;
            layout.minHeight = lineHeight;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        private static void CreateDivider(Transform parent)
        {
            GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            divider.transform.SetParent(parent, false);

            LayoutElement layout = divider.GetComponent<LayoutElement>();
            layout.preferredHeight = 1f;
            layout.minHeight = 1f;

            Image image = divider.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(image);
            image.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.32f);
            image.raycastTarget = false;
        }

        private void Update()
        {
            if (DMUiToolkitHud.IsDriving)
            {
                if (panelRoot != null && panelRoot.gameObject.activeSelf)
                    panelRoot.gameObject.SetActive(false);

                bool mountedUitk = PlayerVehicleState.IsMounted;
                if (mountedUitk != wasMountedLastFrame)
                {
                    wasMountedLastFrame = mountedUitk;
                    HandleMountStateChanged(mountedUitk);
                }

                return;
            }

            if (!uiBuilt)
                return;

            bool mountedNow = PlayerVehicleState.IsMounted;
            if (mountedNow != wasMountedLastFrame)
            {
                wasMountedLastFrame = mountedNow;
                HandleMountStateChanged(mountedNow);
            }

            bool shouldShow = gameplayVisible && mountedNow;
            if (panelRoot.gameObject.activeSelf != shouldShow)
                panelRoot.gameObject.SetActive(shouldShow);

            if (!shouldShow)
                return;

            HovercraftController craft = PlayerVehicleState.ActiveCraft;
            if (craft == null)
                return;

            var hovercraftItem = craft.Usable != null ? craft.Usable.HovercraftItem : null;
            vesselNameLabel.text = hovercraftItem != null && !string.IsNullOrEmpty(hovercraftItem.itemName)
                ? hovercraftItem.itemName
                : "Hovercraft";

            if (craft != cachedCraft)
            {
                cachedCraft = craft;
                cachedHealth = craft.GetComponent<HovercraftHealth>();
                cachedFuel = craft.GetComponent<HovercraftFuelSystem>();
            }

            if (cachedHealth != null)
            {
                shieldBar.SetValues(cachedHealth.CurrentShield, cachedHealth.MaxShield);
                healthBar.SetValues(cachedHealth.CurrentHealth, cachedHealth.MaxHealth);
            }
            else
            {
                shieldBar.SetUnavailable("Shield");
                healthBar.SetUnavailable("Hull");
            }

            if (cachedFuel != null)
                fuelBar.SetValues(cachedFuel.CurrentFuel, cachedFuel.MaxFuel);
            else
                fuelBar.SetUnavailable("Fuel");

            RefreshFooter(craft);
        }

        private void RefreshFooter(HovercraftController craft)
        {
            HovercraftProfile profile = craft.Profile;
            var weaponItem = profile != null ? profile.weaponItem : null;
            weaponValueLabel.text = weaponItem != null && !string.IsNullOrEmpty(weaponItem.itemName)
                ? weaponItem.itemName
                : "Unarmed";

            HoverPhysicsDriver physicsDriver = craft.PhysicsDriver;
            speedValueLabel.text = physicsDriver != null
                ? $"{physicsDriver.CurrentPlanarSpeed:0} m/s"
                : "—";
        }

        /// <summary>
        /// Reacts to a mount/dismount edge (polled here since PlayerVehicleState has no change event)
        /// by suppressing the pet/hotbar/tool-bar chrome while driving, and letting the normal HUD
        /// visibility system restore it on dismount.
        /// </summary>
        private void HandleMountStateChanged(bool mounted)
        {
            ToolBarUI toolbar = GetComponent<ToolBarUI>();
            if (toolbar == null)
                toolbar = FindAnyObjectByType<ToolBarUI>();

            toolbar?.SetVehicleModeHudSuppressed(mounted);
        }

        public void SetGameplayVisible(bool visible)
        {
            gameplayVisible = visible;
            if (uiBuilt && !visible)
                panelRoot.gameObject.SetActive(false);
        }
    }
}
