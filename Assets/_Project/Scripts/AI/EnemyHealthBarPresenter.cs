using Project.AI.Invector;
using Project.Combat;
using Project.Companions;
using Project.Creatures;
using Project.Core;
using Project.Player;
using Project.Survival;
using Project.UI;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Routes enemy health into the top-screen engaged HUD instead of floating world bars.
    /// Event-driven (no per-frame polling): only player outbound damage (EnemyHealth.DamagedBy,
    /// <see cref="DMEngagedEnemyHudFocusRouter"/>, lock-on) focuses the engaged bar — not incoming
    /// enemy hits on the player. The HUD then checks only the shown enemy at ~4 Hz.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyHealthBarPresenter : MonoBehaviour
    {
        [SerializeField] private bool showFloatingHealthBar = true;
        [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2f, 0f);

        private EnemyHealth health;
        private EnemyCombat combat;
        private DMICreatureBridge creatureBridge;
        private EnemyLootable lootable;
        private EnemyInvectorBootstrap invectorBootstrap;
        private Transform _canvasRoot;
        private Transform _cachedPlayerRoot;
        private System.Func<bool> _stillEngaged;
        private float lastSeenHealth = -1f;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            combat = GetComponent<EnemyCombat>();
            creatureBridge = GetComponent<DMICreatureBridge>();
            lootable = GetComponent<EnemyLootable>();
            invectorBootstrap = GetComponent<EnemyInvectorBootstrap>();
        }

        private void Start()
        {
            if (!showFloatingHealthBar || health == null)
                return;

            _canvasRoot = ResolveCanvasRoot();
            EngagedEnemyHealthHud.EnsureExists(_canvasRoot);
            _stillEngaged = IsEngagedWithPlayer;

            health.DamagedBy += OnDamagedBy;
            health.DamagedWithSource += OnDamagedWithSource;
            health.HealthChanged += OnHealthChanged;
            lastSeenHealth = health.CurrentHealth;
            health.Died += HandleDied;
            health.Respawned += HandleRespawned;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.DamagedBy -= OnDamagedBy;
                health.DamagedWithSource -= OnDamagedWithSource;
                health.HealthChanged -= OnHealthChanged;
                health.Died -= HandleDied;
                health.Respawned -= HandleRespawned;
            }

            EngagedEnemyHealthHud.Instance?.ClearIf(health);
        }

        private void OnDamagedBy(GameObject source)
        {
            if (!IsPlayerSource(source))
                return;

            PushHealthHudImmediate();
        }

        private void OnDamagedWithSource(float damage, GameObject source, bool isCritical)
        {
            if (damage <= 0f || !IsPlayerSource(source))
                return;

            PushHealthHudImmediate();
        }

        private void OnHealthChanged(float current, float max)
        {
            if (health == null)
                return;

            lastSeenHealth = current;
            EngagedEnemyHealthHud.Instance?.UpdateHealthIfBound(health, current, max);
        }

        /// <summary>Latest player damage wins the engaged HUD (melee, ranged, lock-on).</summary>
        public static void RequestEngagedHudFocus(EnemyHealth target)
        {
            if (target == null || target.IsDead)
                return;

            EnemyHealthBarPresenter presenter = target.GetComponent<EnemyHealthBarPresenter>();
            if (presenter != null)
            {
                presenter.PushHealthHudImmediate();
                return;
            }

            EngagedEnemyHealthHud hud = EngagedEnemyHealthHud.EnsureExists(ResolveCanvasRoot());
            hud.Focus(target, target.name, target.CurrentHealth, target.MaxHealth, null);
        }

        private void PushHealthHudImmediate()
        {
            if (!showFloatingHealthBar || health == null || health.IsDead)
                return;

            EngagedEnemyHealthHud hud = EngagedEnemyHealthHud.EnsureExists(_canvasRoot ?? ResolveCanvasRoot());
            hud.Focus(
                health,
                ResolveDisplayName(),
                health.CurrentHealth,
                health.MaxHealth,
                _stillEngaged);
        }

        private void HandleDied()
        {
            EngagedEnemyHealthHud.Instance?.ClearIf(health);
        }

        private void HandleRespawned()
        {
            EngagedEnemyHealthHud.Instance?.ClearIf(health);
            if (health != null)
                lastSeenHealth = health.CurrentHealth;
        }

        private bool IsEngagedWithPlayer()
        {
            if (this == null || health == null || health.IsDead)
                return false;

            return IsEngagedWithPlayerCore();
        }

        private bool IsEngagedWithPlayerCore()
        {
            if (combat != null && IsPlayerTarget(combat.CurrentTarget))
                return true;

            if (creatureBridge != null && IsPlayerTarget(creatureBridge.CurrentThreat))
                return true;

            return false;
        }

        private bool IsPlayerTarget(Transform target)
        {
            if (target == null)
                return false;

            if (_cachedPlayerRoot != null &&
                (target == _cachedPlayerRoot || target.IsChildOf(_cachedPlayerRoot)))
                return true;

            PlayerController player = target.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                _cachedPlayerRoot = player.transform;
                return true;
            }

            SurvivalStats stats = target.GetComponentInParent<SurvivalStats>();
            if (stats != null && target.GetComponentInParent<CompanionHealth>() == null)
            {
                _cachedPlayerRoot = stats.transform;
                return true;
            }

            return false;
        }

        private string ResolveDisplayName()
        {
            if (lootable != null && !string.IsNullOrWhiteSpace(lootable.DisplayName))
                return lootable.DisplayName;

            if (creatureBridge != null && creatureBridge.Definition != null &&
                !string.IsNullOrWhiteSpace(creatureBridge.Definition.displayName))
                return creatureBridge.Definition.displayName;

            if (invectorBootstrap != null && invectorBootstrap.Definition != null &&
                !string.IsNullOrWhiteSpace(invectorBootstrap.Definition.displayName))
                return invectorBootstrap.Definition.displayName;

            return gameObject.name;
        }

        private static bool IsPlayerSource(GameObject source)
        {
            return CombatPlayerSourceUtility.IsPlayerOrPioneerSource(source);
        }

        private static Transform _cachedCanvasRoot;

        private static Transform ResolveCanvasRoot()
        {
            if (_cachedCanvasRoot != null)
                return _cachedCanvasRoot;

            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
            _cachedCanvasRoot = uiManager != null ? uiManager.transform : null;
            return _cachedCanvasRoot;
        }
    }
}
