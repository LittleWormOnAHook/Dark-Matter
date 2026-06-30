using Project.AI;
using Project.Combat;
using Project.Companions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class FloatingTargetHealthBar : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.45f, 0f);
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, 28f);
        [SerializeField] private Color fillColor = new Color(0.92f, 0.18f, 0.14f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        private RectTransform rectTransform;
        private TrainingDummy dummyTarget;
        private EnemyHealth enemyTarget;
        private CompanionHealth companionTarget;
        private Vector3 targetBarOffset;
        private Camera worldCamera;
        private bool userVisible = true;

        public EnemyHealth EnemyTarget => enemyTarget;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            if (backgroundImage == null)
                backgroundImage = transform.Find("Background")?.GetComponent<Image>();

            if (fillImage == null)
                fillImage = transform.Find("Fill")?.GetComponent<Image>();

            if (healthText == null)
                healthText = GetComponentInChildren<TextMeshProUGUI>();

            TmpUiHelper.ApplyDefaultFont(healthText);

            if (backgroundImage != null)
                backgroundImage.color = backgroundColor;

            if (fillImage != null)
            {
                fillImage.color = fillColor;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            }
        }

        public void Bind(TrainingDummy dummy)
        {
            ClearBindings();
            dummyTarget = dummy;
            if (dummyTarget == null)
                return;

            dummyTarget.HealthChanged += HandleHealthChanged;
            HandleHealthChanged(dummyTarget.CurrentHealth, dummyTarget.MaxHealth);
        }

        public void Bind(EnemyHealth health, Vector3 worldOffset)
        {
            ClearBindings();
            enemyTarget = health;
            targetBarOffset = worldOffset;
            if (enemyTarget == null)
                return;

            enemyTarget.HealthChanged += HandleHealthChanged;
            HandleHealthChanged(enemyTarget.CurrentHealth, enemyTarget.MaxHealth);
        }

        public void Bind(CompanionHealth health, Vector3 worldOffset)
        {
            ClearBindings();
            companionTarget = health;
            targetBarOffset = worldOffset;
            if (companionTarget == null)
                return;

            companionTarget.HealthChanged += HandleHealthChanged;
            HandleHealthChanged(companionTarget.CurrentHealth, companionTarget.MaxHealth);
        }

        public void SetVisible(bool visible)
        {
            userVisible = visible;
            if (!visible)
                SetBarVisible(0f);
        }

        private void ClearBindings()
        {
            if (dummyTarget != null)
                dummyTarget.HealthChanged -= HandleHealthChanged;

            if (enemyTarget != null)
                enemyTarget.HealthChanged -= HandleHealthChanged;

            if (companionTarget != null)
                companionTarget.HealthChanged -= HandleHealthChanged;

            dummyTarget = null;
            enemyTarget = null;
            companionTarget = null;
        }

        private void OnDestroy()
        {
            ClearBindings();
        }

        private void LateUpdate()
        {
            if (rectTransform == null)
                return;

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null)
                return;

            Vector3 worldPosition;
            if (dummyTarget != null)
            {
                worldPosition = dummyTarget.HealthBarAnchor.position + worldOffset;
            }
            else if (enemyTarget != null)
            {
                if (enemyTarget.IsDead)
                {
                    SetBarVisible(0f);
                    return;
                }

                worldPosition = enemyTarget.HealthBarAnchor.position + targetBarOffset;
            }
            else if (companionTarget != null)
            {
                if (companionTarget.IsDead)
                {
                    SetBarVisible(0f);
                    return;
                }

                worldPosition = companionTarget.HealthBarAnchor.position + targetBarOffset;
            }
            else
            {
                return;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
            if (!userVisible || screenPoint.z <= 0f)
            {
                SetBarVisible(0f);
                return;
            }

            SetBarVisible(1f);
            rectTransform.position = screenPoint + (Vector3)screenOffset;
        }

        private void SetBarVisible(float alpha)
        {
            if (backgroundImage != null)
            {
                Color color = backgroundImage.color;
                color.a = backgroundColor.a * alpha;
                backgroundImage.color = color;
            }

            if (fillImage != null)
            {
                Color color = fillImage.color;
                color.a = fillColor.a * alpha;
                fillImage.color = color;
            }

            if (healthText != null)
            {
                Color color = healthText.color;
                color.a = alpha;
                healthText.color = color;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (fillImage != null)
                fillImage.fillAmount = normalized;

            if (healthText != null)
                healthText.text = $"{Mathf.CeilToInt(current)}";
        }
    }
}
