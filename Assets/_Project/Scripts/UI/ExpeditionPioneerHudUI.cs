using System.Collections.Generic;
using Project.Companions;
using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Separate bottom-HUD cluster: three circular expedition pioneer portraits with top-half health arcs,
    /// positioned to the right of the item hotbar.
    /// </summary>
    public class ExpeditionPioneerHudUI : MonoBehaviour
    {
        public const float ClusterGap = 12f * HudLayoutMetrics.HudScale;

        private const int SlotCount = PioneerRosterManager.ExpeditionTrioSize;

        private const float SlotSizeScale = 1.4f;
        private const float TitleSizeScale = 1.25f;

        private static float SlotDiameter => HudLayoutMetrics.Scaled(54f * SlotSizeScale);
        private static float SlotSpacing => HudLayoutMetrics.Scaled(8f * SlotSizeScale);
        private static float ArcPadding => HudLayoutMetrics.Scaled(5f * SlotSizeScale);
        private static float TitleHeight => HudLayoutMetrics.Scaled(20f * TitleSizeScale);

        private static Color EmptySlotColor => SurvivalPioneerUiPalette.WithAlpha(
            SurvivalPioneerUiPalette.DarkNavy,
            0.8f);

        private readonly SlotView[] slots = new SlotView[SlotCount];
        private readonly CompanionHealth[] subscribedHealth = new CompanionHealth[SlotCount];
        private readonly System.Action<float, float>[] healthHandlers = new System.Action<float, float>[SlotCount];

        private RectTransform clusterRoot;
        private Transform clusterOriginalParent;
        private int clusterOriginalSiblingIndex;
        private PioneerRosterManager roster;
        private CompanionRosterBridge rosterBridge;
        private bool uiBuilt;
        private bool subscribedToRoster;
        private bool raisedToFrontLayer;
        private int lastCompanionCount = -1;

        public bool IsBuilt => uiBuilt;

        public float GetClusterWidth() =>
            SlotCount * SlotDiameter + Mathf.Max(0, SlotCount - 1) * SlotSpacing;

        public float GetClusterHeight() =>
            TitleHeight + SlotDiameter + ArcPadding * 2f + HudLayoutMetrics.Scaled(4f);

        public void EnsureBuilt(Transform layoutParent, float anchoredY)
        {
            if (uiBuilt || layoutParent == null)
                return;

            BuildCluster(layoutParent, anchoredY);
            SubscribeToRoster();
            uiBuilt = true;
            SetGameplayVisible(false);
            Refresh();
        }

        public void SetGameplayVisible(bool visible)
        {
            if (clusterRoot != null)
                clusterRoot.gameObject.SetActive(visible);
        }

        public void AlignRightOfHotbar(float leftEdgeX, float anchoredY)
        {
            if (clusterRoot == null)
                return;

            clusterRoot.anchorMin = new Vector2(0.5f, 0f);
            clusterRoot.anchorMax = new Vector2(0.5f, 0f);
            clusterRoot.pivot = new Vector2(0f, 0f);
            clusterRoot.sizeDelta = new Vector2(GetClusterWidth(), GetClusterHeight());
            clusterRoot.anchoredPosition = new Vector2(leftEdgeX, anchoredY);
        }

        public void EnsureRaisedToFrontLayer(Transform canvasRoot)
        {
            if (clusterRoot == null || canvasRoot == null)
                return;

            if (!raisedToFrontLayer)
            {
                clusterOriginalParent = clusterRoot.parent;
                clusterOriginalSiblingIndex = clusterRoot.GetSiblingIndex();
                raisedToFrontLayer = true;
            }

            UiFrontLayer.ReparentToFront(clusterRoot, canvasRoot);
        }

        public void RestoreFromFrontLayer(float leftEdgeX, float anchoredY)
        {
            if (!raisedToFrontLayer || clusterRoot == null || clusterOriginalParent == null)
                return;

            clusterRoot.SetParent(clusterOriginalParent, true);
            clusterRoot.SetSiblingIndex(Mathf.Clamp(clusterOriginalSiblingIndex, 0, clusterOriginalParent.childCount - 1));
            raisedToFrontLayer = false;
            AlignRightOfHotbar(leftEdgeX, anchoredY);
        }

        public void Refresh()
        {
            if (!uiBuilt)
                return;

            roster ??= PioneerRosterManager.EnsureExists();
            rosterBridge ??= FindAnyObjectByType<CompanionRosterBridge>();

            UnsubscribeHealthListeners();

            for (int i = 0; i < SlotCount; i++)
            {
                SkilledPioneerRecord record = roster.GetExpeditionTrioRecordAtSlot(i);
                ApplySlot(i, record);
            }
        }

        private void Update()
        {
            if (!uiBuilt || rosterBridge == null)
                return;

            int count = rosterBridge.ActiveCompanions.Count;
            if (count == lastCompanionCount)
                return;

            lastCompanionCount = count;
            Refresh();
        }

        private void BuildCluster(Transform layoutParent, float anchoredY)
        {
            clusterRoot = new GameObject("ExpeditionPioneerHud", typeof(RectTransform)).GetComponent<RectTransform>();
            clusterRoot.SetParent(layoutParent, false);
            AlignRightOfHotbar(0f, anchoredY);
            CreateTitleLabel();

            GameObject rowObject = new GameObject("SlotsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowObject.transform.SetParent(clusterRoot, false);
            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.offsetMin = new Vector2(0f, 0f);
            rowRect.offsetMax = new Vector2(0f, -TitleHeight);

            HorizontalLayoutGroup rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = SlotSpacing;
            rowLayout.padding = new RectOffset(0, 0, (int)HudLayoutMetrics.Scaled(2f), 0);
            rowLayout.childAlignment = TextAnchor.LowerCenter;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            for (int i = 0; i < SlotCount; i++)
                slots[i] = CreateSlot(rowObject.transform, i);
        }

        private void CreateTitleLabel()
        {
            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(clusterRoot, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, HudLayoutMetrics.Scaled(-2f));
            titleRect.sizeDelta = new Vector2(GetClusterWidth(), TitleHeight);

            TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(title);
            title.text = "PIONEERS";
            title.fontSize = HudLayoutMetrics.ScaledInt(13f * TitleSizeScale);
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = SurvivalPioneerUiPalette.HotbarLabelText;
            title.raycastTarget = false;
        }

        private SlotView CreateSlot(Transform parent, int slotIndex)
        {
            float slotHeight = SlotDiameter + HudLayoutMetrics.Scaled(2f);
            GameObject slotRoot = new GameObject($"PioneerSlot_{slotIndex + 1}", typeof(RectTransform));
            slotRoot.transform.SetParent(parent, false);
            RectTransform slotRect = slotRoot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(SlotDiameter, slotHeight);

            LayoutElement layout = slotRoot.AddComponent<LayoutElement>();
            layout.minWidth = SlotDiameter;
            layout.preferredWidth = SlotDiameter;
            layout.minHeight = slotHeight;
            layout.preferredHeight = slotHeight;

            GameObject portraitFrame = new GameObject("PortraitFrame", typeof(RectTransform));
            portraitFrame.transform.SetParent(slotRoot.transform, false);
            RectTransform frameRect = portraitFrame.GetComponent<RectTransform>();
            float frameSize = SlotDiameter + ArcPadding * 2f;
            frameRect.anchorMin = new Vector2(0.5f, 1f);
            frameRect.anchorMax = new Vector2(0.5f, 1f);
            frameRect.pivot = new Vector2(0.5f, 1f);
            frameRect.anchoredPosition = new Vector2(0f, 0f);
            frameRect.sizeDelta = new Vector2(frameSize, frameSize);

            HalfCircleHealthBarGraphic healthTrack = CreateArcGraphic(
                portraitFrame.transform,
                "HealthTrack",
                SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.55f),
                1f);

            HalfCircleHealthBarGraphic healthFill = CreateArcGraphic(
                portraitFrame.transform,
                "HealthFill",
                SurvivalPioneerUiPalette.PositiveGreen,
                1f);

            GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image), typeof(Mask));
            portraitObject.transform.SetParent(portraitFrame.transform, false);
            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.5f, 0.5f);
            portraitRect.anchorMax = new Vector2(0.5f, 0.5f);
            portraitRect.pivot = new Vector2(0.5f, 0.5f);
            portraitRect.sizeDelta = new Vector2(SlotDiameter, SlotDiameter);

            Image portraitImage = portraitObject.GetComponent<Image>();
            ApplyCircleSprite(portraitImage);
            portraitImage.color = EmptySlotColor;
            portraitImage.raycastTarget = false;

            Mask mask = portraitObject.GetComponent<Mask>();
            mask.showMaskGraphic = true;

            GameObject initialsObject = new GameObject("Initials", typeof(RectTransform));
            initialsObject.transform.SetParent(portraitObject.transform, false);
            RectTransform initialsRect = initialsObject.GetComponent<RectTransform>();
            initialsRect.anchorMin = Vector2.zero;
            initialsRect.anchorMax = Vector2.one;
            initialsRect.offsetMin = Vector2.zero;
            initialsRect.offsetMax = Vector2.zero;

            TextMeshProUGUI initialsLabel = initialsObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(initialsLabel);
            initialsLabel.fontSize = HudLayoutMetrics.ScaledInt(16f * SlotSizeScale);
            initialsLabel.fontStyle = FontStyles.Bold;
            initialsLabel.alignment = TextAlignmentOptions.Center;
            initialsLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;
            initialsLabel.raycastTarget = false;

            return new SlotView
            {
                PortraitImage = portraitImage,
                InitialsLabel = initialsLabel,
                HealthTrack = healthTrack,
                HealthFill = healthFill
            };
        }

        private static HalfCircleHealthBarGraphic CreateArcGraphic(
            Transform parent,
            string name,
            Color color,
            float fill)
        {
            GameObject arcObject = new GameObject(name, typeof(RectTransform), typeof(HalfCircleHealthBarGraphic));
            arcObject.transform.SetParent(parent, false);
            RectTransform arcRect = arcObject.GetComponent<RectTransform>();
            arcRect.anchorMin = Vector2.zero;
            arcRect.anchorMax = Vector2.one;
            arcRect.offsetMin = Vector2.zero;
            arcRect.offsetMax = Vector2.zero;

            HalfCircleHealthBarGraphic graphic = arcObject.GetComponent<HalfCircleHealthBarGraphic>();
            graphic.color = color;
            graphic.Thickness = HudLayoutMetrics.Scaled(4f * SlotSizeScale);
            graphic.FillAmount = fill;
            graphic.raycastTarget = false;
            return graphic;
        }

        private void ApplySlot(int slotIndex, SkilledPioneerRecord record)
        {
            SlotView slot = slots[slotIndex];
            if (slot == null)
                return;

            if (record == null)
            {
                slot.PortraitImage.color = EmptySlotColor;
                slot.InitialsLabel.text = string.Empty;
                slot.HealthTrack.gameObject.SetActive(false);
                slot.HealthFill.gameObject.SetActive(false);
                return;
            }

            slot.PortraitImage.color = PioneerCompanionVisualProfile.GetClassTint(record);
            slot.InitialsLabel.text = BuildInitials(record.displayName);
            slot.HealthTrack.gameObject.SetActive(true);
            slot.HealthFill.gameObject.SetActive(true);

            CompanionHealth health = FindCompanionHealth(record.id);
            if (health != null)
            {
                subscribedHealth[slotIndex] = health;
                int capturedSlot = slotIndex;
                healthHandlers[slotIndex] = (current, max) => ApplyHealthFill(slots[capturedSlot], current, max);
                health.HealthChanged += healthHandlers[slotIndex];
                ApplyHealthFill(slot, health.CurrentHealth, health.MaxHealth);
                return;
            }

            bool injured = record.WorkState == PioneerWorkState.Injured;
            ApplyHealthFill(slot, injured ? 0.25f : 1f, 1f);
        }

        private static void ApplyHealthFill(SlotView slot, float current, float max)
        {
            if (slot == null || slot.HealthFill == null)
                return;

            float normalized = max > 0.01f ? current / max : 0f;
            slot.HealthFill.FillAmount = normalized;
            slot.HealthFill.color = normalized <= 0.3f
                ? SurvivalPioneerUiPalette.DangerRed
                : SurvivalPioneerUiPalette.PositiveGreen;
        }

        private CompanionHealth FindCompanionHealth(string pioneerRecordId)
        {
            if (rosterBridge == null || string.IsNullOrEmpty(pioneerRecordId))
                return null;

            IReadOnlyList<PioneerCompanionAgent> agents = rosterBridge.ActiveCompanions;
            for (int i = 0; i < agents.Count; i++)
            {
                PioneerCompanionAgent agent = agents[i];
                if (agent == null || agent.PioneerRecordId != pioneerRecordId)
                    continue;

                return agent.GetComponent<CompanionHealth>();
            }

            return null;
        }

        private void SubscribeToRoster()
        {
            if (subscribedToRoster)
                return;

            roster = PioneerRosterManager.EnsureExists();
            roster.OnTrioChanged += Refresh;
            roster.OnRosterChanged += Refresh;
            subscribedToRoster = true;
        }

        private void UnsubscribeHealthListeners()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (subscribedHealth[i] != null && healthHandlers[i] != null)
                    subscribedHealth[i].HealthChanged -= healthHandlers[i];

                subscribedHealth[i] = null;
                healthHandlers[i] = null;
            }
        }

        private static void ApplyCircleSprite(Image image)
        {
            if (image == null)
                return;

            Sprite circle = ShiftUiTheme.CircleFilled;
            if (circle != null)
            {
                image.sprite = circle;
                image.type = Image.Type.Simple;
                return;
            }

            MenuUiBuilder.ApplyUiSprite(image);
        }

        private static string BuildInitials(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return string.Empty;

            string trimmed = displayName.Trim();
            if (trimmed.Length == 1)
                return trimmed.ToUpperInvariant();

            int spaceIndex = trimmed.IndexOf(' ');
            if (spaceIndex > 0 && spaceIndex < trimmed.Length - 1)
                return $"{char.ToUpperInvariant(trimmed[0])}{char.ToUpperInvariant(trimmed[spaceIndex + 1])}";

            return trimmed.Length >= 2
                ? $"{char.ToUpperInvariant(trimmed[0])}{char.ToUpperInvariant(trimmed[1])}"
                : char.ToUpperInvariant(trimmed[0]).ToString();
        }

        private void OnDestroy()
        {
            if (subscribedToRoster && roster != null)
            {
                roster.OnTrioChanged -= Refresh;
                roster.OnRosterChanged -= Refresh;
            }
        }

        private sealed class SlotView
        {
            public Image PortraitImage;
            public TextMeshProUGUI InitialsLabel;
            public HalfCircleHealthBarGraphic HealthTrack;
            public HalfCircleHealthBarGraphic HealthFill;
        }
    }
}
