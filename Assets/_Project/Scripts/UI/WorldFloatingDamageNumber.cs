using Project.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// World-space damage popup that billboards toward the camera, floats upward, and fades out.
    /// </summary>
    public class WorldFloatingDamageNumber : MonoBehaviour, IPoolable
    {
        private static readonly Color DefaultDamageColor = new Color(0.95f, 0.18f, 0.12f, 1f);
        private static GameObject templatePrefab;

        [SerializeField] private float worldScale = 0.012f;
        [SerializeField] private float floatSpeed = 0.75f;
        [SerializeField] private float lifetime = 1.4f;
        [SerializeField] private float fadeDuration = 0.45f;
        [SerializeField] private float fontSize = 28f;

        private TextMeshProUGUI label;
        private CanvasGroup canvasGroup;
        private Vector3 worldAnchor;
        private Vector3 worldDrift;
        private Camera worldCamera;
        private Color damageColor = DefaultDamageColor;
        private float elapsed;

        public static void Spawn(float damage, Vector3 worldPosition, Color? color = null)
        {
            if (damage <= 0f)
                return;

            GameObject root = PoolManager.Spawn(EnsureTemplate(), worldPosition, Quaternion.identity);
            if (root == null)
                return;

            WorldFloatingDamageNumber popup = root.GetComponent<WorldFloatingDamageNumber>();
            popup.Activate(damage, worldPosition, color ?? DefaultDamageColor);
        }

        public void OnSpawnedFromPool()
        {
        }

        public void OnReturnedToPool()
        {
            elapsed = 0f;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        private void Activate(float damage, Vector3 worldPosition, Color color)
        {
            EnsureVisualRefs();
            damageColor = color;
            worldAnchor = worldPosition;
            worldDrift = new Vector3(Random.Range(-0.08f, 0.08f), 0f, Random.Range(-0.08f, 0.08f));
            elapsed = 0f;

            label.text = Mathf.RoundToInt(damage).ToString();
            label.color = damageColor;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            transform.position = worldAnchor;
            FaceCamera();
        }

        private static GameObject EnsureTemplate()
        {
            if (templatePrefab != null)
                return templatePrefab;

            templatePrefab = new GameObject("DamagePopupTemplate", typeof(RectTransform));
            templatePrefab.SetActive(false);
            WorldFloatingDamageNumber popup = templatePrefab.AddComponent<WorldFloatingDamageNumber>();
            popup.BuildVisuals();
            Object.DontDestroyOnLoad(templatePrefab);
            return templatePrefab;
        }

        private void EnsureVisualRefs()
        {
            if (label != null && canvasGroup != null)
                return;

            canvasGroup = GetComponent<CanvasGroup>();
            label = GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null || canvasGroup == null)
                BuildVisuals();
        }

        private void BuildVisuals()
        {
            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120f, 48f);
            rect.localScale = Vector3.one * worldScale;

            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Transform existingText = transform.Find("Text");
            GameObject textObject = existingText != null
                ? existingText.gameObject
                : new GameObject("Text", typeof(RectTransform));
            if (existingText == null)
                textObject.transform.SetParent(transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            label = textObject.GetComponent<TextMeshProUGUI>();
            if (label == null)
                label = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            Outline outline = textObject.GetComponent<Outline>();
            if (outline == null)
                outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void LateUpdate()
        {
            elapsed += Time.deltaTime;
            worldAnchor += Vector3.up * floatSpeed * Time.deltaTime;

            transform.position = worldAnchor + worldDrift;
            FaceCamera();

            if (canvasGroup != null)
            {
                float fadeStart = lifetime - fadeDuration;
                canvasGroup.alpha = elapsed >= fadeStart
                    ? 1f - ((elapsed - fadeStart) / fadeDuration)
                    : 1f;
            }

            if (elapsed >= lifetime)
                PoolManager.Release(gameObject);
        }

        private void FaceCamera()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null)
                return;

            transform.rotation = Quaternion.LookRotation(transform.position - worldCamera.transform.position);
        }
    }
}
