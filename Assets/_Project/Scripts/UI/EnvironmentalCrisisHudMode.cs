using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// GDD Phase E — sulfur storm / environmental crisis HUD retraction.
    /// Keeps essential survival meters; hides non-critical HUD chrome and shows PAUSED banner.
    /// </summary>
    public class EnvironmentalCrisisHudMode : MonoBehaviour
    {
        private static EnvironmentalCrisisHudMode instance;

        private GameObject vignetteRoot;
        private GameObject bannerRoot;
        private TextMeshProUGUI bannerLabel;
        private bool crisisActive;
        private bool built;

        public static EnvironmentalCrisisHudMode Instance => instance;

        public static bool IsCrisisActive => instance != null && instance.crisisActive;

        public static EnvironmentalCrisisHudMode EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("EnvironmentalCrisisHudMode", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            instance = host.AddComponent<EnvironmentalCrisisHudMode>();
            instance.Build(canvasRoot);
            return instance;
        }

        public void SetCrisisActive(bool active, string bannerMessage = null)
        {
            EnsureBuilt(transform.parent != null ? transform.parent : transform);
            crisisActive = active;

            if (vignetteRoot != null)
                vignetteRoot.SetActive(active);
            if (bannerRoot != null)
                bannerRoot.SetActive(active);

            if (bannerLabel != null && !string.IsNullOrWhiteSpace(bannerMessage))
                bannerLabel.text = bannerMessage;

            ApplyHudRetraction(active);

            if (active)
            {
                vignetteRoot?.transform.SetAsLastSibling();
                bannerRoot?.transform.SetAsLastSibling();
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void EnsureBuilt(Transform canvasRoot)
        {
            if (built)
                return;

            Build(canvasRoot);
        }

        private void Build(Transform canvasRoot)
        {
            if (built)
                return;

            built = true;
            transform.SetParent(canvasRoot, false);
            MenuUiBuilder.StretchRectToFill(GetComponent<RectTransform>());

            vignetteRoot = MenuUiBuilder.CreateFullScreenPanel(
                transform,
                "CrisisVignette",
                SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.38f),
                blockRaycasts: false);
            vignetteRoot.SetActive(false);

            bannerRoot = new GameObject("CrisisBanner", typeof(RectTransform), typeof(Image));
            bannerRoot.transform.SetParent(transform, false);
            RectTransform bannerRect = bannerRoot.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 1f);
            bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.anchoredPosition = new Vector2(0f, -12f);
            bannerRect.sizeDelta = new Vector2(720f, 52f);

            Image bannerBg = bannerRoot.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bannerBg);
            bannerBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.92f);
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(bannerRoot);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(bannerRoot.transform, false);
            bannerLabel = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(bannerLabel);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(bannerLabel, bold: true);
            bannerLabel.text = "SULFUR STORM — BASE OPERATIONS PAUSED";
            bannerLabel.fontSize = 22f;
            bannerLabel.fontStyle = FontStyles.Bold;
            bannerLabel.alignment = TextAlignmentOptions.Center;
            bannerLabel.color = SurvivalPioneerUiPalette.BodyText;
            bannerLabel.raycastTarget = false;

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            bannerRoot.SetActive(false);
            gameObject.SetActive(true);
        }

        private static void ApplyHudRetraction(bool crisis)
        {
            SetRootActive(FindAnyObjectByType<ToolBarUI>()?.gameObject, !crisis);
            SetRootActive(FindAnyObjectByType<ActiveQuestHudUI>()?.gameObject, !crisis);
            SetRootActive(FindAnyObjectByType<PickupAimReticleUI>()?.gameObject, !crisis);
            SetRootActive(FindAnyObjectByType<PickupProximityDotUI>()?.gameObject, !crisis);
            SetRootActive(FindAnyObjectByType<WorldInteractionDotUI>()?.gameObject, !crisis);

            UIManager uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                if (uiManager.interactionPrompt != null)
                    uiManager.interactionPrompt.gameObject.SetActive(!crisis);
                if (uiManager.piBalanceText != null)
                    uiManager.piBalanceText.gameObject.SetActive(!crisis);
            }
        }

        private static void SetRootActive(GameObject root, bool active)
        {
            if (root != null)
                root.SetActive(active);
        }
    }
}
