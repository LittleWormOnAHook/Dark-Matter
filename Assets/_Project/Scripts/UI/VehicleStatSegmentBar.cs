using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    internal enum VehicleStatIconKind
    {
        Shield,
        Hull,
        Fuel
    }

    /// <summary>
    /// Segmented-block stat row for the vehicle status HUD (Shield/Hull/Fuel), styled after the
    /// uploaded "Ship Status HUD" reference — icon badge, name + percentage header, a row of discrete
    /// filled blocks instead of a solid bar, and an optional flashing "CRITICAL" pill — but recolored
    /// to the project's existing navy/fuchsia palette instead of the reference's literal purple/pink.
    /// Also flashes once whenever the value is topped up (refuel/repair/big regen tick), separate
    /// from the continuous critical-low flash.
    /// </summary>
    internal sealed class VehicleStatSegmentBar
    {
        public const float RowHeight = 46f;

        private const int SegmentCount = 12;
        private const float LoadFlashThresholdFraction = 0.015f;
        private const float LoadFlashDuration = 0.5f;
        private const float CriticalFraction = 0.25f;
        private const float CriticalFlashCycleSeconds = 1.1f;

        private static readonly Color InactiveSegmentColor = new Color(1f, 1f, 1f, 0.08f);

        private Color accentColor;
        private readonly Image[] segmentImages = new Image[SegmentCount];
        private readonly Image iconGlyphImage;
        private readonly TextMeshProUGUI nameLabel;
        private readonly TextMeshProUGUI percentLabel;
        private readonly GameObject criticalPill;
        private readonly CanvasGroup criticalPillGroup;
        private readonly bool showCriticalPill;
        private readonly bool enableLoadFlash;
        private readonly MonoBehaviour coroutineHost;

        private float lastCurrentValue = float.NaN;
        private Coroutine loadFlashRoutine;
        private Coroutine criticalFlashRoutine;
        private bool isCritical;

        public VehicleStatSegmentBar(
            Transform parent,
            VehicleStatIconKind icon,
            string statName,
            Color accent,
            MonoBehaviour coroutineHost,
            bool showCriticalPill = false,
            bool enableLoadFlash = true)
            : this(parent, ResolveIconSprite(icon), statName, accent, coroutineHost, showCriticalPill, enableLoadFlash)
        {
        }

        /// <summary>
        /// Icon-by-sprite overload — used by hazard rows (real icon art from HazardHudIconSet) so they
        /// can share this exact row style (badge + name + percentage + segmented bar) without going
        /// through the vehicle-specific procedural icon set.
        /// </summary>
        public VehicleStatSegmentBar(
            Transform parent,
            Sprite iconSprite,
            string statName,
            Color accent,
            MonoBehaviour coroutineHost,
            bool showCriticalPill = false,
            bool enableLoadFlash = true)
        {
            accentColor = accent;
            this.coroutineHost = coroutineHost;
            this.showCriticalPill = showCriticalPill;
            this.enableLoadFlash = enableLoadFlash;

            GameObject rowObject = new GameObject($"VehicleStat_{statName}", typeof(RectTransform), typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);
            LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
            rowLayout.flexibleWidth = 1f;
            rowLayout.preferredHeight = RowHeight;
            rowLayout.minHeight = RowHeight;

            HorizontalLayoutGroup rowGroup = rowObject.AddComponent<HorizontalLayoutGroup>();
            rowGroup.spacing = 10f;
            // MiddleLeft (not UpperLeft) so the icon badge and the name/percent/bar column stay
            // vertically centered on each other regardless of their slightly different natural
            // heights (30px badge vs ~40px text column) — previously both were top-anchored, which
            // let the text/bar hang lower than the icon instead of sitting level with it.
            rowGroup.childAlignment = TextAnchor.MiddleLeft;
            // childControlWidth must be true here: the icon badge and content column both carry
            // LayoutElement width hints (badge: fixed 30, content: flexible-fill), but those hints
            // are only honored when the parent group actually controls width. With it left false,
            // both children fell back to Unity's default 100x100 RectTransform size instead of their
            // real dimensions — the badge rendered as an oversized box (glyph floating in empty
            // space) and the pushed-over content (name/bar/percent) overflowed past the row/panel
            // edge. This was masked in the wide 300px Hovercraft HUD panel but broke visibly in the
            // narrower Hazards list.
            rowGroup.childControlWidth = true;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childForceExpandHeight = false;

            iconGlyphImage = CreateIconBadge(rowObject.transform, iconSprite, accent);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            content.transform.SetParent(rowObject.transform, false);
            LayoutElement contentLayout = content.GetComponent<LayoutElement>();
            contentLayout.flexibleWidth = 1f;
            contentLayout.minHeight = 40f;

            VerticalLayoutGroup contentGroup = content.GetComponent<VerticalLayoutGroup>();
            contentGroup.spacing = 5f;
            contentGroup.childControlWidth = true;
            contentGroup.childControlHeight = true;
            contentGroup.childForceExpandWidth = true;
            contentGroup.childForceExpandHeight = false;

            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            header.transform.SetParent(content.transform, false);
            LayoutElement headerLayout = header.GetComponent<LayoutElement>();
            headerLayout.preferredHeight = 16f;
            headerLayout.minHeight = 16f;

            HorizontalLayoutGroup headerGroup = header.GetComponent<HorizontalLayoutGroup>();
            headerGroup.childAlignment = TextAnchor.MiddleLeft;
            headerGroup.childControlWidth = true;
            headerGroup.childForceExpandWidth = true;
            headerGroup.spacing = 6f;

            nameLabel = CreateLabel(header.transform, statName, 13f, TextAlignmentOptions.MidlineLeft, SurvivalPioneerUiPalette.BodyText, true);

            if (showCriticalPill)
                criticalPill = CreateCriticalPill(header.transform, out criticalPillGroup);

            percentLabel = CreateLabel(header.transform, "0%", 13f, TextAlignmentOptions.MidlineRight, SurvivalPioneerUiPalette.WarmOffWhite, false);
            LayoutElement percentLayout = percentLabel.gameObject.AddComponent<LayoutElement>();
            percentLayout.minWidth = 40f;
            percentLayout.preferredWidth = 40f;

            GameObject segmentRow = new GameObject("Segments", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            segmentRow.transform.SetParent(content.transform, false);
            LayoutElement segmentRowLayout = segmentRow.GetComponent<LayoutElement>();
            segmentRowLayout.preferredHeight = 10f;
            segmentRowLayout.minHeight = 10f;

            HorizontalLayoutGroup segmentGroup = segmentRow.GetComponent<HorizontalLayoutGroup>();
            segmentGroup.spacing = 2f;
            segmentGroup.childControlWidth = true;
            segmentGroup.childControlHeight = true;
            segmentGroup.childForceExpandWidth = true;
            segmentGroup.childForceExpandHeight = true;

            for (int i = 0; i < SegmentCount; i++)
                segmentImages[i] = CreateSegment(segmentRow.transform);
        }

        /// <summary>
        /// Re-themes this row for a new subject entirely (icon + accent + name), used by the compact
        /// hazard indicator which shows whichever hazard zone is currently dominant rather than a
        /// fixed subject like Shield/Hull/Fuel. Segment/icon colors pick up the new accent on the next
        /// SetValues call (skipped while a load-flash is mid-animation, same as a normal color update).
        /// </summary>
        public void SetIconAndAccent(Sprite icon, Color accent, string label)
        {
            accentColor = accent;
            nameLabel.text = label;

            if (iconGlyphImage != null)
            {
                iconGlyphImage.sprite = icon;
                if (loadFlashRoutine == null)
                    iconGlyphImage.color = accent;
            }
        }

        public void SetValues(float current, float max)
        {
            float percent = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            percentLabel.text = $"{Mathf.RoundToInt(percent * 100f)}%";

            if (loadFlashRoutine == null)
                ApplySegmentColors(percent);

            bool nowCritical = showCriticalPill && max > 0f && current > 0f && percent <= CriticalFraction;
            if (nowCritical != isCritical)
            {
                isCritical = nowCritical;
                UpdateCriticalState();
            }

            if (enableLoadFlash && max > 0f && !float.IsNaN(lastCurrentValue) && current - lastCurrentValue > max * LoadFlashThresholdFraction)
                TriggerLoadFlash(percent);

            lastCurrentValue = current;
        }

        public void SetUnavailable(string label)
        {
            nameLabel.text = label;
            percentLabel.text = "—";
            ApplySegmentColors(0f);
            lastCurrentValue = float.NaN;

            if (isCritical)
            {
                isCritical = false;
                UpdateCriticalState();
            }
        }

        private void ApplySegmentColors(float percent)
        {
            for (int i = 0; i < SegmentCount; i++)
            {
                if (segmentImages[i] == null)
                    continue;

                float segStart = i / (float)SegmentCount;
                float fill = Mathf.Clamp01((percent - segStart) * SegmentCount);
                segmentImages[i].color = Color.Lerp(InactiveSegmentColor, accentColor, fill);
            }
        }

        private void TriggerLoadFlash(float percent)
        {
            if (coroutineHost == null || !coroutineHost.isActiveAndEnabled)
                return;

            if (loadFlashRoutine != null)
                coroutineHost.StopCoroutine(loadFlashRoutine);

            loadFlashRoutine = coroutineHost.StartCoroutine(LoadFlashRoutine(percent));
        }

        private IEnumerator LoadFlashRoutine(float percent)
        {
            float t = 0f;
            while (t < LoadFlashDuration)
            {
                t += Time.deltaTime;
                float pop = Mathf.Sin(Mathf.Clamp01(t / LoadFlashDuration) * Mathf.PI);

                for (int i = 0; i < SegmentCount; i++)
                {
                    if (segmentImages[i] == null)
                        continue;

                    float segStart = i / (float)SegmentCount;
                    float fill = Mathf.Clamp01((percent - segStart) * SegmentCount);
                    Color baseColor = Color.Lerp(InactiveSegmentColor, accentColor, fill);
                    segmentImages[i].color = fill > 0.01f ? Color.Lerp(baseColor, Color.white, pop) : baseColor;
                }

                if (iconGlyphImage != null)
                    iconGlyphImage.color = Color.Lerp(accentColor, Color.white, pop);

                yield return null;
            }

            ApplySegmentColors(percent);

            if (iconGlyphImage != null)
                iconGlyphImage.color = accentColor;

            loadFlashRoutine = null;
        }

        private void UpdateCriticalState()
        {
            if (criticalPill == null)
                return;

            criticalPill.SetActive(isCritical);

            if (isCritical)
            {
                if (criticalFlashRoutine == null && coroutineHost != null && coroutineHost.isActiveAndEnabled)
                    criticalFlashRoutine = coroutineHost.StartCoroutine(CriticalFlashRoutine());
            }
            else
            {
                if (criticalFlashRoutine != null && coroutineHost != null)
                    coroutineHost.StopCoroutine(criticalFlashRoutine);

                criticalFlashRoutine = null;

                if (criticalPillGroup != null)
                    criticalPillGroup.alpha = 1f;
            }
        }

        private IEnumerator CriticalFlashRoutine()
        {
            while (isCritical)
            {
                float phase = (Time.unscaledTime % CriticalFlashCycleSeconds) / CriticalFlashCycleSeconds;
                float wave = (Mathf.Cos(phase * Mathf.PI * 2f) + 1f) * 0.5f;
                if (criticalPillGroup != null)
                    criticalPillGroup.alpha = Mathf.Lerp(0.35f, 1f, wave);

                yield return null;
            }

            criticalFlashRoutine = null;
        }

        private static Image CreateSegment(Transform parent)
        {
            GameObject segment = new GameObject("Segment", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            segment.transform.SetParent(parent, false);

            LayoutElement layout = segment.GetComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.minWidth = 0f;

            Image image = segment.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(image);
            image.color = InactiveSegmentColor;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateIconBadge(Transform parent, Sprite iconSprite, Color accent)
        {
            GameObject badge = new GameObject("IconBadge", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            badge.transform.SetParent(parent, false);

            LayoutElement badgeLayout = badge.GetComponent<LayoutElement>();
            badgeLayout.minWidth = 30f;
            badgeLayout.preferredWidth = 30f;
            badgeLayout.minHeight = 30f;
            badgeLayout.preferredHeight = 30f;

            Image chipImage = badge.GetComponent<Image>();
            // Deliberately skip MenuUiBuilder.ApplyUiSprite here — it applies the shared 9-sliced
            // panel-frame sprite, whose corner radius reads as basically circular at this ~30px
            // square size. Leaving the sprite unset draws a flat, sharp-cornered square instead.
            chipImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.9f);
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(badge, new Vector2(1f, -1f));

            GameObject glyphObject = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
            glyphObject.transform.SetParent(badge.transform, false);
            RectTransform glyphRect = glyphObject.GetComponent<RectTransform>();
            glyphRect.anchorMin = Vector2.zero;
            glyphRect.anchorMax = Vector2.one;
            glyphRect.offsetMin = new Vector2(6f, 6f);
            glyphRect.offsetMax = new Vector2(-6f, -6f);

            Image glyphImage = glyphObject.GetComponent<Image>();
            glyphImage.sprite = iconSprite;
            glyphImage.color = accent;
            glyphImage.preserveAspect = true;
            glyphImage.raycastTarget = false;

            return glyphImage;
        }

        private static Sprite ResolveIconSprite(VehicleStatIconKind icon)
        {
            return icon switch
            {
                VehicleStatIconKind.Shield => VehicleStatIconTexture.GetShield(),
                VehicleStatIconKind.Hull => VehicleStatIconTexture.GetHeart(),
                VehicleStatIconKind.Fuel => VehicleStatIconTexture.GetFuelDrop(),
                _ => null
            };
        }

        private static GameObject CreateCriticalPill(Transform parent, out CanvasGroup pillGroup)
        {
            GameObject pill = new GameObject("CriticalPill", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(CanvasGroup));
            pill.transform.SetParent(parent, false);

            LayoutElement pillLayout = pill.GetComponent<LayoutElement>();
            pillLayout.preferredWidth = 62f;
            pillLayout.preferredHeight = 16f;
            pillLayout.minWidth = 62f;
            pillLayout.minHeight = 16f;

            Image pillImage = pill.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(pillImage);
            pillImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DeepMagenta, 0.9f);

            pillGroup = pill.GetComponent<CanvasGroup>();

            GameObject label = new GameObject("Label", typeof(RectTransform));
            label.transform.SetParent(pill.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(labelRect);

            TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            text.text = "CRITICAL";
            text.fontSize = 9f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = SurvivalPioneerUiPalette.WarmOffWhite;
            text.raycastTarget = false;

            pill.SetActive(false);
            return pill;
        }

        private static TextMeshProUGUI CreateLabel(
            Transform parent,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color,
            bool flexible)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);

            if (flexible)
            {
                LayoutElement layout = labelObject.AddComponent<LayoutElement>();
                layout.flexibleWidth = 1f;
                layout.minWidth = 40f;
            }

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }
    }
}
