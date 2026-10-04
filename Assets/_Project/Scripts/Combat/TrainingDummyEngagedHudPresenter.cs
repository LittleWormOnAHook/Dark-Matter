using Project.Core;
using Project.Player;
using Project.Survival;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Routes training dummy health into the same top-center UITK engaged bar as enemies.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TrainingDummy))]
    public sealed class TrainingDummyEngagedHudPresenter : MonoBehaviour
    {
        [SerializeField] private string displayName = "Training Dummy";

        private TrainingDummy dummy;
        private float lastPlayerAttackTime = -999f;
        private Transform canvasRoot;

        private void Awake()
        {
            dummy = GetComponent<TrainingDummy>();
        }

        private void Start()
        {
            if (dummy == null)
                return;

            canvasRoot = ResolveCanvasRoot();
            EngagedEnemyHealthHud.EnsureExists(canvasRoot);

            dummy.HealthChanged += OnHealthChanged;
            dummy.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (dummy != null)
            {
                dummy.HealthChanged -= OnHealthChanged;
                dummy.Died -= OnDied;
            }

            EngagedEnemyHealthHud.Instance?.ClearIf(dummy);
        }

        private void LateUpdate()
        {
            if (dummy == null || dummy.IsDead)
                return;

            if (Time.time - lastPlayerAttackTime > EngagedEnemyHealthHud.AttackLinger)
                EngagedEnemyHealthHud.Instance?.ClearIf(dummy);
        }

        public void NotifyDamagedByPlayer(GameObject source)
        {
            if (dummy == null || dummy.IsDead || !IsPlayerSource(source))
                return;

            lastPlayerAttackTime = Time.time;
            PushHudImmediate();
        }

        private void OnHealthChanged(float current, float max)
        {
            if (dummy == null || dummy.IsDead)
                return;

            EngagedEnemyHealthHud.Instance?.UpdateHealthIfBound(dummy, current, max);
        }

        private void OnDied()
        {
            EngagedEnemyHealthHud.Instance?.ClearIf(dummy);
        }

        private void PushHudImmediate()
        {
            EngagedEnemyHealthHud hud = EngagedEnemyHealthHud.EnsureExists(canvasRoot ?? ResolveCanvasRoot());
            hud.ShowFromPlayerDamage(
                dummy,
                displayName,
                dummy.CurrentHealth,
                dummy.MaxHealth);
        }

        private static bool IsPlayerSource(GameObject source)
        {
            if (source == null)
                return false;

            if (source.GetComponentInParent<SurvivalStats>() != null &&
                source.GetComponentInParent<PlayerController>() != null)
                return true;

            return source.GetComponentInParent<SurvivalStats>() != null;
        }

        private static Transform ResolveCanvasRoot()
        {
            UIManager uiManager = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            return uiManager != null ? uiManager.transform : null;
        }
    }
}
