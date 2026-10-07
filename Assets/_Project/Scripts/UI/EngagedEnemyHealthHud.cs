using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Single top-center enemy health readout. Event-driven: the latest event wins (the player
    /// hitting an enemy, or an enemy hitting the player, focuses that enemy). After the last event
    /// the bar lingers <see cref="AttackLinger"/> seconds, and stays while the shown enemy is still
    /// engaged with the player. Only the shown enemy is checked, at ~4 Hz (no per-enemy polling).
    /// </summary>
    [DisallowMultipleComponent]
    public class EngagedEnemyHealthHud : MonoBehaviour
    {
        private const float ScreenWidthFraction = 0.33f;
        private const float BarHeight = 8f;
        private const float NameHeight = 22f;
        private const float TopMargin = 28f;
        private const float NameGap = 4f;
        private const float AttackLingerSeconds = 3.5f;
        private const float FocusCheckInterval = 0.25f;

        private static EngagedEnemyHealthHud instance;

        private RectTransform root;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI nameLabel;
        private Image backgroundImage;
        private Image fillImage;
        private IEngagedHealthHudTarget boundHealth;
        private Func<bool> boundStillEngaged;
        private float lastFocusEventTime = -999f;
        private float nextFocusCheckTime;
        private bool built;

        public static EngagedEnemyHealthHud Instance => instance;

        public static EngagedEnemyHealthHud EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
            {
                instance.EnsureBuilt(canvasRoot);
                return instance;
            }

            EngagedEnemyHealthHud existing = UnityEngine.Object.FindAnyObjectByType<EngagedEnemyHealthHud>(FindObjectsInactive.Include);
            if (existing != null)
            {
                instance = existing;
                existing.EnsureBuilt(canvasRoot);
                return existing;
            }

            Transform parent = canvasRoot != null ? canvasRoot : ResolveDefaultCanvasRoot();
            GameObject host = new GameObject("EngagedEnemyHealthHud", typeof(RectTransform), typeof(EngagedEnemyHealthHud));
            if (parent != null)
                host.transform.SetParent(parent, false);

            instance = host.GetComponent<EngagedEnemyHealthHud>();
            instance.EnsureBuilt(parent);
            return instance;
        }

        private void Awake()
        {
            instance = this;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            UnbindHealth();
        }

        public void EnsureBuilt(Transform canvasRoot)
        {
            if (built)
            {
                if (canvasRoot != null && transform.parent != canvasRoot)
                    transform.SetParent(canvasRoot, false);
                ApplyLayout();
                return;
            }

            built = true;
            root = transform as RectTransform;
            if (canvasRoot != null)
                transform.SetParent(canvasRoot, false);

            // Keep this GameObject active — hide via CanvasGroup so callers can always re-show.
            if (!TryGetComponent(out canvasGroup))
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }

            GameObject nameObject = new GameObject("EnemyName", typeof(RectTransform));
            nameObject.transform.SetParent(transform, false);
            nameLabel = nameObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(nameLabel);
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.fontSize = 16f;
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;
            nameLabel.raycastTarget = false;
            nameLabel.text = string.Empty;
            TmpUiHelper.TryApplyOutline(nameLabel, 0.2f, Color.black);

            GameObject barObject = new GameObject("HealthBar", typeof(RectTransform));
            barObject.transform.SetParent(transform, false);

            backgroundImage = barObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(backgroundImage);
            backgroundImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 0.9f);
            backgroundImage.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
            fillObject.transform.SetParent(barObject.transform, false);
            fillImage = fillObject.AddComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(fillImage);
            fillImage.color = DarkMatterGenesisUiPalette.DeepMagenta;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.raycastTarget = false;

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            ApplyLayout();
            SetVisible(false);
        }

        /// <summary>Player damage always takes over the HUD from any other enemy (latest event wins).</summary>
        public void ShowFromPlayerDamage(
            IEngagedHealthHudTarget health,
            string displayName,
            float current,
            float max)
        {
            Focus(health, displayName, current, max, null);
        }

        /// <summary>
        /// Latest event wins: focuses this target (player hit it, or it hit the player) and restarts the linger.
        /// <paramref name="stillEngaged"/> (optional) keeps the bar up past the linger while it returns true;
        /// it is only evaluated for the shown target, at ~4 Hz.
        /// </summary>
        public void Focus(
            IEngagedHealthHudTarget health,
            string displayName,
            float current,
            float max,
            Func<bool> stillEngaged)
        {
            if (health == null || health.IsDead)
            {
                ClearIf(health);
                return;
            }

            EnsureBuilt(transform.parent);
            lastFocusEventTime = Time.time;
            nextFocusCheckTime = Time.time + FocusCheckInterval;
            boundStillEngaged = stillEngaged;
            BindAndPresent(health, displayName, current, max);
        }

        /// <summary>
        /// Weak claim (an enemy started targeting the player): only shows when no other target is focused.
        /// </summary>
        public bool TryFocusIfIdle(
            IEngagedHealthHudTarget health,
            string displayName,
            float current,
            float max,
            Func<bool> stillEngaged)
        {
            if (boundHealth != null && boundHealth != health && !boundHealth.IsDead)
                return false;

            Focus(health, displayName, current, max, stillEngaged);
            return true;
        }

        public bool IsFocused(IEngagedHealthHudTarget health)
        {
            return health != null && boundHealth == health;
        }

        private void Update()
        {
            if (boundHealth == null || Time.time < nextFocusCheckTime)
                return;

            nextFocusCheckTime = Time.time + FocusCheckInterval;
            if (boundHealth.IsDead)
            {
                Clear();
                return;
            }

            if (Time.time - lastFocusEventTime <= AttackLingerSeconds)
                return;

            bool engaged = false;
            if (boundStillEngaged != null)
            {
                try
                {
                    engaged = boundStillEngaged();
                }
                catch (MissingReferenceException)
                {
                    engaged = false;
                }
            }

            if (!engaged)
                Clear();
        }

        public void UpdateHealthIfBound(IEngagedHealthHudTarget health, float current, float max)
        {
            if (health == null || boundHealth != health)
                return;

            ApplyHealth(current, max);
        }

        public void ClearIf(IEngagedHealthHudTarget health)
        {
            if (health != null && boundHealth != health)
                return;

            Clear();
        }

        public void Clear()
        {
            UnbindHealth();
            SetVisible(false);
        }

        private void BindAndPresent(
            IEngagedHealthHudTarget health,
            string displayName,
            float current,
            float max)
        {
            if (boundHealth != health)
            {
                UnbindHealth();
                boundHealth = health;
                boundHealth.HealthChanged += HandleHealthChanged;
                boundHealth.Died += HandleBoundDied;
            }

            if (nameLabel != null)
                nameLabel.text = string.IsNullOrWhiteSpace(displayName) ? "Enemy" : displayName;

            ApplyHealth(current, max);
            SetVisible(true);
            transform.SetAsLastSibling();
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (boundHealth == null || boundHealth.IsDead)
            {
                Clear();
                return;
            }

            ApplyHealth(current, max);
        }

        private void HandleBoundDied()
        {
            Clear();
        }

        private void ApplyHealth(float current, float max)
        {
            float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (fillImage != null)
                fillImage.fillAmount = normalized;
        }

        private void UnbindHealth()
        {
            if (boundHealth == null)
                return;

            boundHealth.HealthChanged -= HandleHealthChanged;
            boundHealth.Died -= HandleBoundDied;
            boundHealth = null;
            boundStillEngaged = null;
        }

        public bool TryGetFocus(out string displayName, out float normalized)
        {
            displayName = string.Empty;
            normalized = 0f;
            if (boundHealth == null || boundHealth.IsDead)
                return false;

            displayName = nameLabel != null && !string.IsNullOrWhiteSpace(nameLabel.text)
                ? nameLabel.text.Trim()
                : "Enemy";
            float max = boundHealth.MaxHealth;
            normalized = max > 0f ? Mathf.Clamp01(boundHealth.CurrentHealth / max) : 0f;
            return true;
        }

        public void ApplyToolkitVisibility()
        {
            SetVisible(boundHealth != null && !boundHealth.IsDead);
        }

        private void SetVisible(bool visible)
        {
            if (DMUiToolkitHud.IsDriving)
                visible = false;

            // Never deactivate this host — SetActive(false) previously hid the HUD permanently
            // when Awake/EnsureBuilt raced, and left Instance pointing at a dead tree.
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (canvasGroup == null && !TryGetComponent(out canvasGroup))
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            if (nameLabel != null)
                nameLabel.enabled = visible;
            if (backgroundImage != null)
                backgroundImage.enabled = visible;
            if (fillImage != null)
                fillImage.enabled = visible;
        }

        private void ApplyLayout()
        {
            if (root == null)
                root = transform as RectTransform;

            float totalHeight = NameHeight + NameGap + BarHeight;
            const float halfWidth = ScreenWidthFraction * 0.5f;

            // Center-pivoted, ~33% of parent/canvas width via anchors (Canvas Scaler safe).
            root.anchorMin = new Vector2(0.5f - halfWidth, 1f);
            root.anchorMax = new Vector2(0.5f + halfWidth, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(0f, totalHeight);
            root.anchoredPosition = new Vector2(0f, -TopMargin);

            if (nameLabel != null)
            {
                RectTransform nameRect = nameLabel.rectTransform;
                nameRect.anchorMin = new Vector2(0f, 1f);
                nameRect.anchorMax = new Vector2(1f, 1f);
                nameRect.pivot = new Vector2(0.5f, 1f);
                nameRect.sizeDelta = new Vector2(0f, NameHeight);
                nameRect.anchoredPosition = Vector2.zero;
            }

            if (backgroundImage != null)
            {
                RectTransform barRect = backgroundImage.rectTransform;
                barRect.anchorMin = new Vector2(0f, 0f);
                barRect.anchorMax = new Vector2(1f, 0f);
                barRect.pivot = new Vector2(0.5f, 0f);
                barRect.sizeDelta = new Vector2(0f, BarHeight);
                barRect.anchoredPosition = Vector2.zero;
            }
        }

        private static Transform ResolveDefaultCanvasRoot()
        {
            UIManager uiManager = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            return uiManager != null ? uiManager.transform : null;
        }

        /// <summary>How long the HUD may linger after the player last damaged an enemy.</summary>
        public static float AttackLinger => AttackLingerSeconds;
    }
}
