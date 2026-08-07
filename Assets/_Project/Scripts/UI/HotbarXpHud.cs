using Project.Core;
using Project.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Compact level + XP bar anchored under the hotbar with a small gap.
    /// Fill uses the same anchor-scale approach as <see cref="CharacterStatBarRow"/> /
    /// condensed vitals (Simple/Sliced sprite + anchorMax.x) — Image.Type.Filled with no
    /// sprite never draws, which is why the bar appeared stuck empty.
    /// </summary>
    [DisallowMultipleComponent]
    public class HotbarXpHud : MonoBehaviour
    {
        private const float BarHeight = 18f * HudLayoutMetrics.HudScale;
        private const float GapBelowHotbar = 6f * HudLayoutMetrics.HudScale;
        private const float LevelWidth = 78f * HudLayoutMetrics.HudScale;
        private const float FillLerpSpeed = 10f;

        private RectTransform root;
        private TextMeshProUGUI levelLabel;
        private TextMeshProUGUI xpCountLabel;
        private RectTransform xpFillRect;
        private Image xpFill;
        private PlayerProgressionManager progression;
        private bool built;
        private float displayedFill;
        private float targetFill;

        public static HotbarXpHud EnsureExists(Transform canvasRoot)
        {
            HotbarXpHud existing = Object.FindAnyObjectByType<HotbarXpHud>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.EnsureBuilt(canvasRoot);
                return existing;
            }

            GameObject host = new GameObject("HotbarXpHud", typeof(RectTransform), typeof(HotbarXpHud));
            HotbarXpHud hud = host.GetComponent<HotbarXpHud>();
            hud.EnsureBuilt(canvasRoot);
            return hud;
        }

        public void EnsureBuilt(Transform canvasRoot)
        {
            if (built)
            {
                ParentToFrontLayer(canvasRoot);
                AlignUnderHotbar();
                Refresh();
                return;
            }

            built = true;
            root = transform as RectTransform;
            ParentToFrontLayer(canvasRoot);

            // Drop stale children (detach first so Destroy-deferred objects leave this hierarchy).
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            Image background = gameObject.GetComponent<Image>();
            if (background == null)
                background = gameObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(background);
            background.color = new Color(0f, 0f, 0f, 0.35f);
            background.raycastTarget = false;

            GameObject levelObject = new GameObject("Level", typeof(RectTransform));
            levelObject.transform.SetParent(transform, false);
            RectTransform levelRect = levelObject.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0f, 0f);
            levelRect.anchorMax = new Vector2(0f, 1f);
            levelRect.pivot = new Vector2(0f, 0.5f);
            levelRect.anchoredPosition = Vector2.zero;
            levelRect.sizeDelta = new Vector2(LevelWidth, 0f);

            levelLabel = levelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(levelLabel);
            // 13 * 1.5 = 19.5 base before HUD scale.
            levelLabel.fontSize = 19.5f * HudLayoutMetrics.HudScale;
            levelLabel.fontStyle = FontStyles.Bold;
            levelLabel.alignment = TextAlignmentOptions.MidlineLeft;
            levelLabel.color = SurvivalPioneerUiPalette.Gold;
            levelLabel.raycastTarget = false;
            levelLabel.margin = new Vector4(6f, 0f, 0f, 0f);
            levelLabel.overflowMode = TextOverflowModes.Overflow;
            levelLabel.textWrappingMode = TextWrappingModes.NoWrap;

            GameObject trackObject = new GameObject("XpTrack", typeof(RectTransform), typeof(Image));
            trackObject.transform.SetParent(transform, false);
            RectTransform trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0.15f);
            trackRect.anchorMax = new Vector2(1f, 0.85f);
            trackRect.offsetMin = new Vector2(LevelWidth + 4f, 0f);
            trackRect.offsetMax = new Vector2(-6f, 0f);
            Image trackImage = trackObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(trackImage);
            trackImage.color = SurvivalPioneerUiPalette.SlateGray;
            trackImage.raycastTarget = false;

            GameObject fillObject = new GameObject("XpFill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(trackObject.transform, false);
            xpFillRect = fillObject.GetComponent<RectTransform>();
            xpFillRect.anchorMin = Vector2.zero;
            xpFillRect.anchorMax = Vector2.one;
            xpFillRect.pivot = new Vector2(0f, 0.5f);
            xpFillRect.offsetMin = Vector2.zero;
            xpFillRect.offsetMax = Vector2.zero;
            xpFill = fillObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(xpFill);
            xpFill.color = SurvivalPioneerUiPalette.Gold;
            xpFill.raycastTarget = false;
            xpFill.preserveAspect = false;

            GameObject countObject = new GameObject("XpCount", typeof(RectTransform));
            countObject.transform.SetParent(trackObject.transform, false);
            RectTransform countRect = countObject.GetComponent<RectTransform>();
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.one;
            countRect.offsetMin = Vector2.zero;
            countRect.offsetMax = Vector2.zero;
            xpCountLabel = countObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(xpCountLabel);
            xpCountLabel.fontSize = 11f * HudLayoutMetrics.HudScale;
            xpCountLabel.fontStyle = FontStyles.Bold;
            xpCountLabel.alignment = TextAlignmentOptions.Center;
            xpCountLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;
            xpCountLabel.raycastTarget = false;
            xpCountLabel.overflowMode = TextOverflowModes.Ellipsis;
            xpCountLabel.textWrappingMode = TextWrappingModes.NoWrap;

            BindProgression();
            AlignUnderHotbar();
            Refresh(snapFill: true);
        }

        private void OnEnable()
        {
            BindProgression();
            if (built)
                Refresh(snapFill: true);
        }

        private void OnDisable()
        {
            UnbindProgression();
        }

        private void OnDestroy()
        {
            UnbindProgression();
        }

        private void LateUpdate()
        {
            if (!built || xpFillRect == null)
                return;

            if (Mathf.Approximately(displayedFill, targetFill))
                return;

            displayedFill = Mathf.MoveTowards(displayedFill, targetFill, FillLerpSpeed * Time.unscaledDeltaTime);
            ApplyFillVisual(displayedFill);
        }

        private void BindProgression()
        {
            PlayerProgressionManager next = PlayerProgressionManager.EnsureExists();
            if (progression == next)
                return;

            UnbindProgression();
            progression = next;
            if (progression != null)
                progression.OnXpChanged += HandleXpChanged;
        }

        private void UnbindProgression()
        {
            if (progression != null)
                progression.OnXpChanged -= HandleXpChanged;
            progression = null;
        }

        private void HandleXpChanged()
        {
            Refresh(snapFill: false);
        }

        public void AlignUnderHotbar()
        {
            if (root == null)
                return;

            RectTransform hotbar = ResolveHotbarRect();
            float width = hotbar != null ? hotbar.sizeDelta.x : HudLayoutMetrics.Scaled(640f);
            float anchoredY = hotbar != null ? hotbar.anchoredPosition.y : HudLayoutMetrics.BottomHudInset;
            float centerX = hotbar != null ? hotbar.anchoredPosition.x : 0f;
            Vector2 anchorMin = hotbar != null ? hotbar.anchorMin : new Vector2(0.5f, 0f);
            Vector2 anchorMax = hotbar != null ? hotbar.anchorMax : new Vector2(0.5f, 0f);
            Vector2 pivot = hotbar != null ? hotbar.pivot : new Vector2(0.5f, 0f);

            // Place BELOW the hotbar (hotbar pivot is bottom, so subtract height + gap).
            root.anchorMin = anchorMin;
            root.anchorMax = anchorMax;
            root.pivot = pivot;
            root.sizeDelta = new Vector2(width, BarHeight);
            root.anchoredPosition = new Vector2(centerX, anchoredY - GapBelowHotbar - BarHeight);
        }

        public void SetVisible(bool visible)
        {
            if (!GameSession.HasStarted || MainMenuController.BlocksGameplayHud)
                visible = false;

            gameObject.SetActive(visible);
            if (visible)
            {
                ParentToFrontLayer(ResolveCanvasRoot());
                AlignUnderHotbar();
                Refresh(snapFill: true);
            }
        }

        private void ParentToFrontLayer(Transform canvasRoot)
        {
            canvasRoot ??= ResolveCanvasRoot();
            if (canvasRoot == null || root == null)
                return;

            // Stay with the hotbar on UiFrontLayer (sortingOrder 500). Leaving the XP bar on
            // MainCanvas (order 0) made it disappear behind raised HUD chrome after map/optics.
            Transform front = UiFrontLayer.Get(canvasRoot);
            if (root.parent != front)
                root.SetParent(front, false);
        }

        private static Transform ResolveCanvasRoot()
        {
            Canvas main = MainMenuController.ResolveMainCanvas();
            if (main != null)
                return main.transform;

            InventoryUI inventoryUi = Object.FindAnyObjectByType<InventoryUI>();
            if (inventoryUi != null)
            {
                Canvas canvas = inventoryUi.GetComponent<Canvas>() ?? inventoryUi.GetComponentInParent<Canvas>();
                if (canvas != null)
                    return canvas.transform;
            }

            return Object.FindAnyObjectByType<Canvas>()?.transform;
        }

        public void Refresh() => Refresh(snapFill: true);

        private void Refresh(bool snapFill)
        {
            if (levelLabel == null || xpFillRect == null)
                return;

            BindProgression();
            int level = progression != null ? progression.Level : 1;
            int xpIntoLevel = progression != null ? progression.GetXpProgressInCurrentLevel() : 0;
            int xpToNext = progression != null ? progression.GetXpRequiredForNextLevel() : 0;
            float fill = progression != null ? progression.GetXpProgressNormalized() : 0f;

            levelLabel.text = $"Lv {level}";
            if (xpCountLabel != null)
            {
                xpCountLabel.text = xpToNext > 0
                    ? $"{xpIntoLevel} / {xpToNext} XP"
                    : "MAX";
            }

            targetFill = Mathf.Clamp01(fill);
            if (snapFill)
            {
                displayedFill = targetFill;
                ApplyFillVisual(displayedFill);
            }
        }

        private void ApplyFillVisual(float normalized)
        {
            if (xpFillRect == null)
                return;

            normalized = Mathf.Clamp01(normalized);
            xpFillRect.anchorMin = Vector2.zero;
            xpFillRect.anchorMax = new Vector2(normalized, 1f);
            xpFillRect.pivot = new Vector2(0f, 0.5f);
            xpFillRect.anchoredPosition = Vector2.zero;
            xpFillRect.offsetMin = Vector2.zero;
            xpFillRect.offsetMax = Vector2.zero;
        }

        private static RectTransform ResolveHotbarRect()
        {
            InventoryUI inventoryUi = Object.FindAnyObjectByType<InventoryUI>();
            if (inventoryUi != null && inventoryUi.hotbarParent is RectTransform wired)
                return wired;

            GameObject tagged = GameObject.Find("Hotbar");
            return tagged != null ? tagged.transform as RectTransform : null;
        }
    }
}
