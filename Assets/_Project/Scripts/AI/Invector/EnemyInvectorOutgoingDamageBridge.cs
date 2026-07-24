using Invector;
using Project.Data;
using Project.Player.Invector;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Scales humanoid enemy outgoing Invector damage from EnemyCombat stats and equipped ItemData.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyInvectorOutgoingDamageBridge : MonoBehaviour, IInvectorOutgoingDamageSource
    {
        private EnemyCombat _combat;
        private EnemyInvectorLoadoutBridge _loadout;

        private void Awake()
        {
            _combat = GetComponent<EnemyCombat>();
            _loadout = GetComponent<EnemyInvectorLoadoutBridge>();
        }

        public float ResolveOutgoingDamage(vDamage damage, GameObject source, out bool isCritical)
        {
            isCritical = Random.value < 0.08f;
            ItemData item = _loadout != null ? _loadout.ActiveItem : null;
            if (item != null)
            {
                if (item.IsRangedWeapon)
                {
                    // Ranged damage is dealt exclusively by the unified CombatProjectile spawned
                    // from EnemyInvectorProjectileBridge on the same onShot event (shared pipeline
                    // with player/companion fire). Invector's own hit detection must not also apply
                    // damage here or a single shot would double-hit.
                    isCritical = false;
                    return 0f;
                }

                if (item.itemType == ItemType.MeleeWeapon)
                {
                    isCritical = item.RollCriticalHit();
                    return item.RollMeleeDamage(isCritical);
                }
            }

            float baseDamage = _combat != null ? _combat.AttackDamage : (damage != null ? damage.damageValue : 0f);
            return isCritical ? baseDamage * 1.35f : baseDamage;
        }
    }
}
