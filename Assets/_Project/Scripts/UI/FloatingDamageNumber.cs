using TMPro;
using UnityEngine;

namespace Project.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class FloatingDamageNumber : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float floatSpeed = 55f;
        [SerializeField] private float lifetime = 1.6f;
        [SerializeField] private float fadeDuration = 0.55f;
        [SerializeField] private float startScale = 0.85f;
        [SerializeField] private float peakScale = 1.15f;
        [SerializeField] private Color damageColor = new Color(0.95f, 0.18f, 0.12f, 1f);

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Camera worldCamera;
        private Vector3 worldAnchor;
        private Vector2 screenOffset;
        private float elapsed;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>();

            TmpUiHelper.ApplyDefaultFont(label);
        }

        public void Initialize(float damage, Vector3 worldPosition, Camera camera)
        {
            worldAnchor = worldPosition;
            worldCamera = camera != null ? camera : Camera.main;
            screenOffset = new Vector2(Random.Range(-18f, 18f), Random.Range(0f, 10f));
            elapsed = 0f;

            if (label != null)
            {
                label.text = Mathf.RoundToInt(damage).ToString();
                label.color = damageColor;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            transform.localScale = Vector3.one * startScale;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            worldAnchor += Vector3.up * floatSpeed * Time.deltaTime;

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null || rectTransform == null)
                return;

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldAnchor);
            if (screenPoint.z <= 0f)
            {
                if (canvasGroup != null)
                    canvasGroup.alpha = 0f;
                return;
            }

            rectTransform.position = screenPoint + (Vector3)screenOffset;

            float lifeT = Mathf.Clamp01(elapsed / lifetime);
            float scaleT = lifeT < 0.18f ? lifeT / 0.18f : 1f - ((lifeT - 0.18f) / 0.82f);
            float scale = Mathf.Lerp(startScale, peakScale, 1f - scaleT);
            transform.localScale = Vector3.one * scale;

            if (canvasGroup != null)
            {
                float fadeStart = lifetime - fadeDuration;
                canvasGroup.alpha = elapsed >= fadeStart
                    ? 1f - ((elapsed - fadeStart) / fadeDuration)
                    : 1f;
            }

            if (elapsed >= lifetime)
                Destroy(gameObject);
        }
    }
}
