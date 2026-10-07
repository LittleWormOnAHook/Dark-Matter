using Project.AI.Invector;
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
    /// Event-driven (no per-frame polling): the player damaging this enemy (EnemyHealth.DamagedBy) or this
    /// enemy damaging the player (SurvivalStats.DamagedBySource) focuses it; starting to target the player
    /// claims the bar only when nothing else is shown. The HUD then checks only the shown enemy at ~4 Hz.
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

        private static SurvivalStats s_boundPlayerStats;

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
            health.HealthChanged += OnHealthChanged;
            health.Died += HandleDied;
            health.Respawned += HandleRespawned;
            if (combat != null)
                combat.TargetChanged += OnCombatTargetChanged;

            EnsurePlayerDamageRouting();
        }

        /// <summary>
        /// Subscribes once (static) to the player's SurvivalStats.DamagedBySource. Re-binds only if the player
        /// object was replaced. Runs at enemy Start, never per frame.
        /// </summary>
        private static void EnsurePlayerDamageRouting()
        {
            if (s_boundPlayerStats != null)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            SurvivalStats stats = player != null ? player.GetComponentInParent<SurvivalStats>() : null;
            if (stats == null && player != null)
                stats = player.GetComponentInChildren<SurvivalStats>();
            if (stats == null)
                return;

            s_boundPlayerStats = stats;
            stats.DamagedBySource += HandlePlayerDamagedBySource;
        }

        private static void HandlePlayerDamagedBySource(GameObject source)
        {
            if (source == null)
                return;

            EnemyHealthBarPresenter presenter = source.GetComponentInParent<EnemyHealthBarPresenter>();
            if (presenter != null)
                presenter.PushHealthHudImmediate();
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.DamagedBy -= OnDamagedBy;
                health.HealthChanged -= OnHealthChanged;
                health.Died -= HandleDied;
                health.Respawned -= HandleRespawned;
            }

            if (combat != null)
                combat.TargetChanged -= OnCombatTargetChanged;

            EngagedEnemyHealthHud.Instance?.ClearIf(health);
        }

        private void OnCombatTargetChanged(Transform newTarget)
        {
            if (!showFloatingHealthBar || health == null || health.IsDead || !IsPlayerTarget(newTarget))
                return;

            EnsurePlayerDamageRouting();

            EngagedEnemyHealthHud hud = EngagedEnemyHealthHud.EnsureExists(_canvasRoot ?? ResolveCanvasRoot());
            hud.TryFocusIfIdle(health, ResolveDisplayName(), health.CurrentHealth, health.MaxHealth, _stillEngaged);
        }

        private void OnDamagedBy(GameObject source)
        {
            if (!IsPlayerSource(source))
                return;

            PushHealthHudImmediate();
        }

        private void OnHealthChanged(float current, float max)
        {
            if (health == null || health.IsDead)
                return;

            EngagedEnemyHealthHud.Instance?.UpdateHealthIfBound(health, current, max);
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
            if (source == null)
                return false;

            if (source.CompareTag("Player"))
                return true;

            if (source.GetComponentInParent<PlayerController>() != null)
                return true;

            if (source.GetComponentInParent<SurvivalStats>() != null &&
                source.GetComponentInParent<CompanionHealth>() == null)
                return true;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return false;

            return source == player || source.transform.IsChildOf(player.transform);
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
