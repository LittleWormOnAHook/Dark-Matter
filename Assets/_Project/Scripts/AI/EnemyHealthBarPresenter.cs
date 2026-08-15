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
    /// Shows while this enemy targets the player, or while the player is attacking it.
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
        private float lastPlayerAttackTime = -999f;
        private float firstEngagedTime = -1f;
        private bool reporting;
        private bool wasEngagedLastFrame;
        private Transform _canvasRoot;

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

            health.DamagedBy += OnDamagedBy;
            health.HealthChanged += OnHealthChanged;
            health.Died += HandleDied;
            health.Respawned += HandleRespawned;
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

            EngagedEnemyHealthHud.Instance?.ClearIf(health);
        }

        private void LateUpdate()
        {
            if (!showFloatingHealthBar || health == null || health.IsDead)
            {
                if (reporting)
                {
                    EngagedEnemyHealthHud.Instance?.ClearIf(health);
                    reporting = false;
                }

                return;
            }

            bool engaged = IsEngagedWithPlayer();
            if (engaged && !wasEngagedLastFrame)
                firstEngagedTime = Time.time;

            if (!engaged && wasEngagedLastFrame)
                EngagedEnemyHealthHud.Instance?.ReleaseEngagementCandidate(health);

            wasEngagedLastFrame = engaged;

            bool playerAttacking = Time.time - lastPlayerAttackTime <= EngagedEnemyHealthHud.AttackLinger;
            bool shouldShow = engaged || playerAttacking;

            if (!shouldShow)
            {
                if (reporting)
                {
                    EngagedEnemyHealthHud.Instance?.ClearIf(health);
                    reporting = false;
                }

                return;
            }

            // While the player is actively damaging this target, HUD refresh is event-driven.
            if (playerAttacking)
            {
                reporting = true;
                return;
            }

            EngagedEnemyHealthHud hud = EngagedEnemyHealthHud.EnsureExists(_canvasRoot);
            hud.UpdateFromEngagement(
                health,
                ResolveDisplayName(),
                health.CurrentHealth,
                health.MaxHealth,
                firstEngagedTime);
            reporting = true;
        }

        private void OnDamagedBy(GameObject source)
        {
            if (!IsPlayerSource(source))
                return;

            lastPlayerAttackTime = Time.time;
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
            hud.ShowFromPlayerDamage(
                health,
                ResolveDisplayName(),
                health.CurrentHealth,
                health.MaxHealth);
            reporting = true;
        }

        private void HandleDied()
        {
            reporting = false;
            EngagedEnemyHealthHud.Instance?.ClearIf(health);
        }

        private void HandleRespawned()
        {
            lastPlayerAttackTime = -999f;
            firstEngagedTime = -1f;
            wasEngagedLastFrame = false;
            reporting = false;
            EngagedEnemyHealthHud.Instance?.ReleaseEngagementCandidate(health);
        }

        private bool IsEngagedWithPlayer()
        {
            if (combat != null && combat.CurrentTarget != null)
            {
                if (combat.CurrentTarget.GetComponentInParent<SurvivalStats>() != null &&
                    combat.CurrentTarget.GetComponentInParent<PlayerController>() != null)
                    return true;

                // SurvivalStats alone marks the player body in this project.
                if (combat.CurrentTarget.GetComponentInParent<SurvivalStats>() != null &&
                    combat.CurrentTarget.GetComponentInParent<CompanionHealth>() == null)
                    return true;
            }

            if (creatureBridge != null && creatureBridge.CurrentThreat != null)
            {
                Transform threat = creatureBridge.CurrentThreat;
                if (threat.GetComponentInParent<SurvivalStats>() != null &&
                    threat.GetComponentInParent<CompanionHealth>() == null)
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
