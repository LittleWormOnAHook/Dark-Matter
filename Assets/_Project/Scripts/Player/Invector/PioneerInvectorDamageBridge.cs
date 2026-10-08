using Invector;
using Invector.vShooter;
using Project.AI;
using Project.Combat;
using Project.Data;
using Project.Interaction;
using Project.Survival;
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
        private SurvivalStats _survival;

        private void Awake()
        {
            _weaponBridge = GetComponent<PioneerInvectorWeaponBridge>();
            _survival = GetComponent<SurvivalStats>();
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
            {
                isCritical = item.RollCriticalHit();
                TrySpendMeleeStamina(item);
                float rolled = item.RollMeleeDamage(isCritical);
                PioneerShooterMeleeInput meleeInput = GetComponent<PioneerShooterMeleeInput>();
                if (meleeInput != null && meleeInput.IsStrongMeleeDamageActive)
                {
                    DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
                    float scale = profile != null ? profile.strongMeleeDamageMultiplier : 1.8f;
                    rolled *= Mathf.Max(1f, scale);
                    // Strong B lands two hits; together they equal one strong hit.
                    if (meleeInput.StrongReleaseSlot == 1)
                        rolled *= 0.5f;
                }

                return rolled;
            }

            return damage != null ? damage.damageValue : 0f;
        }

        private void TrySpendMeleeStamina(ItemData item)
        {
            if (item == null || item.meleeStaminaCost <= 0f)
                return;

            if (_survival == null)
                _survival = GetComponent<SurvivalStats>();
            if (_survival == null)
                return;

            _survival.SetStamina(_survival.CurrentStamina - item.meleeStaminaCost);
        }

        public static ItemData ResolveEquippedWeapon(GameObject source)
        {
            if (source == null)
                return null;

            PioneerInvectorWeaponBridge bridge = source.GetComponentInParent<PioneerInvectorWeaponBridge>();
            return bridge != null ? bridge.ActiveEquippedItem : null;
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

            DamageInfo info = DamageInfo.FromHit(damage, isCritical, source, hitPoint);
            CombatDamageApplicator.ApplyToDamageable(
                damageable,
                hitCollider.transform.root.gameObject,
                in info);

            EnemyHealth enemyHealth = hitCollider.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                DMEnemyHitMarks.HandleMeleeHit(
                    enemyHealth,
                    hitCollider,
                    hitPoint,
                    direction,
                    damage,
                    ResolveEquippedWeapon(source),
                    source);
            }
            else
            {
                CombatHitVfx.SpawnBloodSplatter(hitPoint, direction, -direction, damage);
            }

            EnemyNoiseEvents.RaiseNoise(hitPoint, 12f, source);
            DMCombatCameraShake.TryPlayChargedHit(source);
        }
    }
}
