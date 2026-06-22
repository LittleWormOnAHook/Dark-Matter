using Project.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// World-space health bar and floating damage numbers attached to a training dummy.
    /// </summary>
    public class DummyCombatUI : MonoBehaviour
    {
        [SerializeField] private Vector3 healthBarLocalOffset = new Vector3(0f, 0.2f, 0f);
        [SerializeField] private float healthBarWorldScale = 0.009f;
        [SerializeField] private Vector2 healthBarSize = new Vector2(105f, 14f);
        [SerializeField] private Color fillColor = new Color(0.92f, 0.18f, 0.14f, 1f);
        [SerializeField] private Color damageColor = new Color(0.95f, 0.18f, 0.12f, 1f);
        [SerializeField] private Color criticalDamageColor = new Color(1f, 0.82f, 0.12f, 1f);

        private TrainingDummy dummy;
        private Slider healthSlider;
        private TextMeshProUGUI healthText;
        private RectTransform healthBarRect;

        public void Initialize(TrainingDummy owner)
        {
            dummy = owner;
            BuildHealthBar();
            RefreshHealth(dummy.CurrentHealth, dummy.MaxHealth);
        }

        public void RefreshHealth(float current, float max)
        {
            float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (healthSlider != null)
                healthSlider.value = normalized;

            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(current)}";
        }

        public void ShowDamage(float damage, bool isCritical = false)
        {
            if (damage <= 0f || dummy == null)
                return;

            Color popupColor = isCritical ? criticalDamageColor : damageColor;
            WorldFloatingDamageNumber.Spawn(damage, dummy.DamageNumberAnchor.position, popupColor);
        }

        private void LateUpdate()
        {
            if (healthBarRect == null || dummy == null)
                return;

            healthBarRect.position = dummy.HealthBarAnchor.position + healthBarLocalOffset;
            Camera cam = Camera.main;
            if (cam != null)
                healthBarRect.rotation = Quaternion.LookRotation(healthBarRect.position - cam.transform.position);
        }

        private void BuildHealthBar()
        {
            GameObject root = new GameObject("HealthBar", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            healthBarRect = root.GetComponent<RectTransform>();
            healthBarRect.sizeDelta = healthBarSize;
            healthBarRect.localScale = Vector3.one * healthBarWorldScale;

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            healthSlider = root.AddComponent<Slider>();
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
            healthSlider.interactable = false;
            healthSlider.transition = Selectable.Transition.None;
            healthSlider.direction = Slider.Direction.LeftToRight;

            GameObject background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(root.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            backgroundImage.raycastTarget = false;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(2f, 2f);
            fillAreaRect.offsetMax = new Vector2(-2f, -2f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;

            healthSlider.fillRect = fillRect;
            healthSlider.targetGraphic = fillImage;

            GameObject textObject = new GameObject("HealthText", typeof(RectTransform));
            textObject.transform.SetParent(root.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            healthText = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(healthText);
            healthText.fontSize = 9f;
            healthText.fontStyle = FontStyles.Bold;
            healthText.alignment = TextAlignmentOptions.Center;
            healthText.color = Color.white;
            healthText.raycastTarget = false;
        }
    }
}
