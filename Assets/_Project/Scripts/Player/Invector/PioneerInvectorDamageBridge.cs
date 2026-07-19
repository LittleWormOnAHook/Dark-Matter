using Invector;
using Invector.vShooter;
using Project.AI;
using Project.Combat;
using Project.Data;
using Project.Interaction;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Routes Invector outgoing damage through Pioneer ItemData rolls and hit feedback.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PioneerInvectorBootstrap))]
    public class PioneerInvectorDamageBridge : MonoBehaviour, IInvectorOutgoingDamageSource
    {
        private PioneerInvectorWeaponBridge _weaponBridge;

        private void Awake()
        {
            _weaponBridge = GetComponent<PioneerInvectorWeaponBridge>();
        }

        public float ResolveOutgoingDamage(vDamage damage, GameObject source, out bool isCritical)
        {
            isCritical = false;
            ItemData item = _weaponBridge != null ? _weaponBridge.ActiveEquippedItem : null;
            if (item == null)
                return damage != null ? damage.damageValue : 0f;

            if (item.IsRangedWeapon)
            {
                // Ranged damage is now dealt exclusively by the unified CombatProjectile spawned
                // from PioneerInvectorProjectileBridge on the same onShot event (shared with
                // companion/enemy fire so tracers, elemental status effects, and ammo types behave
                // identically everywhere). Invector's own hit detection must not also apply damage
                // here or a single shot would double-hit.
                isCritical = false;
                return 0f;
            }

            if (item.itemType == ItemType.MeleeWeapon)
                return item.RollMeleeDamage(isCritical);

            return damage != null ? damage.damageValue : 0f;
        }

        public static void ApplyPioneerDamageToCollider(Collider hitCollider, float damage, GameObject source, bool isCritical, Vector3? weaponHitPoint = null)
        {
            if (hitCollider == null || damage <= 0f)
                return;

            IDamageable damageable = DamageableUtility.GetDamageable(hitCollider);
            if (damageable == null)
                return;

            // Prefer the weapon hitbox's impact position; clamp it onto the target's surface.
            Vector3 hitPoint = weaponHitPoint.HasValue
                ? hitCollider.ClosestPoint(weaponHitPoint.Value)
                : hitCollider.ClosestPoint(source != null ? source.transform.position : hitCollider.bounds.center);
            Vector3 direction = source != null
                ? (hitPoint - source.transform.position).normalized
                : Vector3.forward;

            damageable.TakeDamage(damage, source, isCritical);
            CombatHitVfx.SpawnBloodSplatter(hitPoint, direction, -direction, damage);
            EnemyNoiseEvents.RaiseNoise(hitPoint, 12f, source);
        }
    }
}
