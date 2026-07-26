using Project.Core;
using Project.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Compact level + XP bar anchored under the hotbar with a small gap.
    /// </summary>
    [DisallowMultipleComponent]
    public class HotbarXpHud : MonoBehaviour
    {
        private const float BarHeight = 14f * HudLayoutMetrics.HudScale;
        private const float GapBelowHotbar = 6f * HudLayoutMetrics.HudScale;
        private const float LevelWidth = 52f * HudLayoutMetrics.HudScale;

        private RectTransform root;
        private TextMeshProUGUI levelLabel;
        private Image xpFill;
        private PlayerProgressionManager progression;
        private bool built;

        public static HotbarXpHud EnsureExists(Transform canvasRoot)
        {
            HotbarXpHud existing = Object.FindAnyObjectByType<HotbarXpHud>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.EnsureBuilt(canvasRoot);
                return existing;
            }

            GameObject host = new GameObject("HotbarXpHud", typeof(RectTransform), typeof(HotbarXpHud));
            host.transform.SetParent(canvasRoot, false);
            HotbarXpHud hud = host.GetComponent<HotbarXpHud>();
            hud.EnsureBuilt(canvasRoot);
            return hud;
        }

        public void EnsureBuilt(Transform canvasRoot)
        {
            if (built)
            {
                AlignUnderHotbar();
                Refresh();
                return;
            }

            built = true;
            root = transform as RectTransform;
            if (root.parent != canvasRoot && canvasRoot != null)
                root.SetParent(canvasRoot, false);

            Image background = gameObject.GetComponent<Image>();
            if (background == null)
                background = gameObject.AddComponent<Image>();
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
            levelLabel.fontSize = 13f * HudLayoutMetrics.HudScale;
            levelLabel.fontStyle = FontStyles.Bold;
            levelLabel.alignment = TextAlignmentOptions.MidlineLeft;
            levelLabel.color = SurvivalPioneerUiPalette.Gold;
            levelLabel.raycastTarget = false;
            levelLabel.margin = new Vector4(6f, 0f, 0f, 0f);

            GameObject trackObject = new GameObject("XpTrack", typeof(RectTransform), typeof(Image));
            trackObject.transform.SetParent(transform, false);
            RectTransform trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 0.2f);
            trackRect.anchorMax = new Vector2(1f, 0.8f);
            trackRect.offsetMin = new Vector2(LevelWidth + 4f, 0f);
            trackRect.offsetMax = new Vector2(-6f, 0f);
            Image trackImage = trackObject.GetComponent<Image>();
            trackImage.color = SurvivalPioneerUiPalette.SlateGray;
            trackImage.raycastTarget = false;

            GameObject fillObject = new GameObject("XpFill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(trackObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            xpFill = fillObject.GetComponent<Image>();
            xpFill.color = SurvivalPioneerUiPalette.Gold;
            xpFill.type = Image.Type.Filled;
            xpFill.fillMethod = Image.FillMethod.Horizontal;
            xpFill.raycastTarget = false;

            progression = PlayerProgressionManager.EnsureExists();
            if (progression != null)
                progression.OnXpChanged += Refresh;

            AlignUnderHotbar();
            Refresh();
        }

        private void OnDestroy()
        {
            if (progression != null)
                progression.OnXpChanged -= Refresh;
        }

        public void AlignUnderHotbar()
        {
            if (root == null)
                return;

            RectTransform hotbar = ResolveHotbarRect();
            float width = hotbar != null ? hotbar.sizeDelta.x : HudLayoutMetrics.Scaled(640f);
            float hotbarHeight = hotbar != null ? hotbar.sizeDelta.y : HudLayoutMetrics.Scaled(82f);
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
                AlignUnderHotbar();
                Refresh();
            }
        }

        public void Refresh()
        {
            if (levelLabel == null || xpFill == null)
                return;

            progression ??= PlayerProgressionManager.EnsureExists();
            int level = progression != null ? progression.Level : 1;
            float fill = progression != null ? progression.GetXpProgressNormalized() : 0f;
            levelLabel.text = $"Lv {level}";
            xpFill.fillAmount = Mathf.Clamp01(fill);
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
