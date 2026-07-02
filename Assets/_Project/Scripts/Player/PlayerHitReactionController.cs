using Project.Inventory;
using Project.Survival;
using UnityEngine;

namespace Project.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SurvivalStats))]
    public class PlayerHitReactionController : MonoBehaviour
    {
        [SerializeField] private float hitReactionCooldown = 0.45f;
        [SerializeField] private float minDamageToReact = 1f;

        private SurvivalStats survivalStats;
        private EquipmentController equipment;
        private PlayerGkcAnimatorDriver animatorDriver;
        private float nextReactionTime;

        private void Awake()
        {
            survivalStats = GetComponent<SurvivalStats>();
            equipment = GetComponent<EquipmentController>();
            ResolveAnimatorDriver();
        }

        private void OnEnable()
        {
            if (survivalStats != null)
                survivalStats.OnDamaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (survivalStats != null)
                survivalStats.OnDamaged -= HandleDamaged;
        }

        private void ResolveAnimatorDriver()
        {
            if (animatorDriver != null)
                return;

            animatorDriver = GetComponentInChildren<PlayerGkcAnimatorDriver>(true);
        }

        private void HandleDamaged(float damage)
        {
            if (damage < minDamageToReact || survivalStats == null || survivalStats.IsDead)
                return;

            if (Time.time < nextReactionTime)
                return;

            ResolveAnimatorDriver();
            if (animatorDriver == null)
                return;

            bool armed = equipment != null && equipment.HasActiveMeleeWeapon();
            if (!animatorDriver.RequestHitReaction(armed))
                return;

            nextReactionTime = Time.time + hitReactionCooldown;
        }
    }
}
