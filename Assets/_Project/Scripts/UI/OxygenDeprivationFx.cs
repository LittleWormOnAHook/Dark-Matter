using Project.Core;
using Project.Survival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Center-screen Low Oxygen toast while oxygen is at/below the Genesis Studio Survival threshold.
    /// Flashes at the configured rate; prefers UITK when the HUD is driving.
    /// </summary>
    [DisallowMultipleComponent]
    public class OxygenDeprivationFx : MonoBehaviour
    {
        private SurvivalStats survivalStats;
        private TextMeshProUGUI warningLabel;
        private CanvasGroup warningGroup;
        private Image vignetteOverlay;
        private bool wasCritical;
        private bool overlayBuilt;
        private bool uitkDriving;

        private void Start()
        {
            if (!Application.isPlaying)
                return;

            EnsureOverlay();
            BindStats();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            if (overlayBuilt)
                BindStats();
        }

        private void OnDisable()
        {
            UnbindStats();
            // Avoid EnsureHost during scene teardown (spawns UITK_LevelUp after close).
            if (!Application.isPlaying)
                return;
            SetWarningActive(false);
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || survivalStats == null || !GameSession.HasStarted)
                return;

            if (survivalStats.IsDead || !SurvivalStats.ResolveOxygenWarningEnabled())
            {
                if (wasCritical)
                {
                    SetWarningActive(false);
                    wasCritical = false;
                }
                return;
            }

            float threshold01 = SurvivalStats.ResolveOxygenWarningPercent() * 0.01f;
            bool isCritical = survivalStats.GetOxygenNormalized() <= threshold01;
            if (isCritical != wasCritical)
            {
                SetWarningActive(isCritical);
                wasCritical = isCritical;
            }

            if (!isCritical)
                return;

            float hz = SurvivalStats.ResolveOxygenFlashPerSecond();
            // 1.5 flashes/sec => opacity pulses 1.5 full cycles per second.
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f) * hz);
            ApplyFlashAlpha(pulse);
        }

        private void BindStats()
        {
            UnbindStats();

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                survivalStats = player.GetComponent<SurvivalStats>();

            if (survivalStats == null)
                survivalStats = FindAnyObjectByType<SurvivalStats>();
        }

        private void UnbindStats()
        {
            survivalStats = null;
        }

        private void SetWarningActive(bool active)
        {
            string text = SurvivalStats.ResolveOxygenWarningText();
            uitkDriving = DMUiToolkitLevelUp.TrySetLowOxygenWarning(active, text);

            if (uitkDriving)
            {
                if (warningGroup != null)
                    warningGroup.alpha = 0f;
                if (warningLabel != null)
                    warningLabel.gameObject.SetActive(false);
                UpdateVignette(active);
                return;
            }

            EnsureOverlay();
            if (warningLabel != null)
            {
                warningLabel.text = text;
                warningLabel.gameObject.SetActive(active);
            }

            if (warningGroup != null)
                warningGroup.alpha = active ? 1f : 0f;

            UpdateVignette(active);
        }

        private void ApplyFlashAlpha(float alpha01)
        {
            if (uitkDriving)
            {
                DMUiToolkitLevelUp.SetLowOxygenFlashAlpha(alpha01);
                return;
            }

            if (warningGroup != null)
                warningGroup.alpha = alpha01;
        }

        private void UpdateVignette(bool active)
        {
            if (vignetteOverlay == null)
                return;

            vignetteOverlay.gameObject.SetActive(active);
            if (!active)
                return;

            Color color = vignetteOverlay.color;
            color.a = 0.35f;
            vignetteOverlay.color = color;
        }

        private void EnsureOverlay()
        {
            if (overlayBuilt)
                return;

            overlayBuilt = true;
            BuildOverlay();
        }

        private void BuildOverlay()
        {
            Transform host = transform;

            vignetteOverlay = CreateFullscreenImage(host, "OxygenVignetteOverlay", new Color(0.55f, 0f, 0f, 0.35f));
            vignetteOverlay.raycastTarget = false;
            vignetteOverlay.gameObject.SetActive(false);

            GameObject toast = new GameObject("LowOxygenWarning", typeof(RectTransform), typeof(CanvasGroup));
            toast.transform.SetParent(host, false);
            RectTransform rect = toast.GetComponent<RectTransform>();
            // Top-center, 150px below jetfuel bar (compass 10 + fuel 40+16 => ~66; +150 => 216 from top).
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -216f);
            rect.sizeDelta = new Vector2(520f, 56f);

            warningGroup = toast.GetComponent<CanvasGroup>();
            warningGroup.alpha = 0f;
            warningGroup.blocksRaycasts = false;
            warningGroup.interactable = false;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(toast.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            warningLabel = labelGo.GetComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(warningLabel);
            warningLabel.fontSize = 50f;
            warningLabel.fontStyle = FontStyles.Bold;
            warningLabel.alignment = TextAlignmentOptions.Center;
            warningLabel.color = new Color(0.86f, 0.16f, 0.14f, 1f);
            warningLabel.text = "Low Oxygen";
            warningLabel.raycastTarget = false;
            warningLabel.gameObject.SetActive(false);
        }

        private static Image CreateFullscreenImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }
    }
}
