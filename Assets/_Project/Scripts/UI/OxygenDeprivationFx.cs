using System.Collections;
using Project.Core;
using Project.Survival;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Screen flash and vignette when oxygen drops to the critical threshold.
    /// </summary>
    [DisallowMultipleComponent]
    public class OxygenDeprivationFx : MonoBehaviour
    {
        [SerializeField] private float criticalThreshold = 0.15f;
        [SerializeField] private float flashPeakAlpha = 0.22f;
        [SerializeField] private float flashDuration = 0.35f;
        [SerializeField] private int flashCount = 3;
        [SerializeField] private float vignetteAlpha = 0.45f;

        private SurvivalStats survivalStats;
        private Image flashOverlay;
        private Image vignetteOverlay;
        private Coroutine flashRoutine;
        private bool wasCritical;
        private bool overlayBuilt;

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
        }

        private void BindStats()
        {
            UnbindStats();

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                survivalStats = player.GetComponent<SurvivalStats>();

            if (survivalStats == null)
                survivalStats = FindAnyObjectByType<SurvivalStats>();

            if (survivalStats != null)
                survivalStats.OnStatsChanged += HandleStatsChanged;
        }

        private void UnbindStats()
        {
            if (survivalStats != null)
                survivalStats.OnStatsChanged -= HandleStatsChanged;
        }

        private void HandleStatsChanged()
        {
            if (survivalStats == null || !GameSession.HasStarted)
                return;

            EnsureOverlay();

            if (survivalStats.IsDead)
            {
                ClearCriticalEffects();
                wasCritical = false;
                return;
            }

            bool isCritical = survivalStats.GetOxygenNormalized() <= criticalThreshold;

            if (isCritical && !wasCritical)
                BeginCriticalEffects();
            else if (!isCritical && wasCritical)
                ClearCriticalEffects();

            UpdateVignette(isCritical);
            wasCritical = isCritical;
        }

        private void BeginCriticalEffects()
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(FlashRedRoutine());
        }

        private IEnumerator FlashRedRoutine()
        {
            for (int i = 0; i < flashCount; i++)
            {
                yield return FadeOverlay(flashOverlay, 0f, flashPeakAlpha, flashDuration * 0.4f);
                yield return FadeOverlay(flashOverlay, flashPeakAlpha, 0f, flashDuration * 0.6f);
            }

            flashRoutine = null;
        }

        private static IEnumerator FadeOverlay(Image overlay, float from, float to, float duration)
        {
            if (overlay == null)
                yield break;

            overlay.gameObject.SetActive(true);

            float elapsed = 0f;
            Color color = overlay.color;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : elapsed / duration;
                color.a = Mathf.Lerp(from, to, t);
                overlay.color = color;
                yield return null;
            }

            color.a = to;
            overlay.color = color;

            if (to <= 0f)
                overlay.gameObject.SetActive(false);
        }

        private void UpdateVignette(bool active)
        {
            if (vignetteOverlay == null)
                return;

            if (active && vignetteOverlay.sprite == null)
                vignetteOverlay.sprite = GetVignetteSprite();

            vignetteOverlay.gameObject.SetActive(active);
            if (!active)
                return;

            Color color = vignetteOverlay.color;
            color.a = vignetteAlpha;
            vignetteOverlay.color = color;
        }

        private void ClearCriticalEffects()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(false);
                Color color = flashOverlay.color;
                color.a = 0f;
                flashOverlay.color = color;
            }

            UpdateVignette(false);
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

            flashOverlay = CreateFullscreenImage(host, "OxygenFlashOverlay", new Color(0.85f, 0.05f, 0.05f, 0f));
            flashOverlay.raycastTarget = false;
            flashOverlay.gameObject.SetActive(false);

            vignetteOverlay = CreateFullscreenImage(host, "OxygenVignetteOverlay", new Color(0.55f, 0f, 0f, vignetteAlpha));
            vignetteOverlay.type = Image.Type.Simple;
            vignetteOverlay.preserveAspect = false;
            vignetteOverlay.raycastTarget = false;
            vignetteOverlay.gameObject.SetActive(false);
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

        private static Sprite vignetteSprite;

        private static Sprite GetVignetteSprite()
        {
            if (vignetteSprite != null)
                return vignetteSprite;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxDist = center.magnitude;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float alpha = Mathf.SmoothStep(0.2f, 1f, dist);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            vignetteSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            vignetteSprite.name = "OxygenVignetteSprite";
            return vignetteSprite;
        }
    }
}
