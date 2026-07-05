using Invector;
using Project.AI;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Forwards Invector vDamage hits into Pioneer EnemyHealth / IDamageable with ItemData-based rolls.
    /// </summary>
    [DisallowMultipleComponent]
    public class PioneerInvectorDamageReceiver : MonoBehaviour, vIDamageReceiver
    {
        [SerializeField] private bool applyToChildren = true;

        private readonly OnReceiveDamage _onStartReceiveDamage = new OnReceiveDamage();
        private readonly OnReceiveDamage _onReceiveDamage = new OnReceiveDamage();

        public OnReceiveDamage onStartReceiveDamage => _onStartReceiveDamage;
        public OnReceiveDamage onReceiveDamage => _onReceiveDamage;

        private void Awake()
        {
            // Invector melee only damages colliders whose tag is in vMeleeManager.hitProperties
            // .hitDamageTags ("Enemy"). Guarantee anything meant to receive damage carries it.
            if (gameObject.CompareTag("Untagged"))
                gameObject.tag = "Enemy";
        }

        public void TakeDamage(vDamage damage)
        {
            if (damage == null)
                return;

            onStartReceiveDamage.Invoke(damage);

            GameObject sender = damage.sender != null ? damage.sender.gameObject : null;
            float pioneerDamage = PioneerInvectorDamageResolver.ResolveOutgoingDamage(damage, sender, out bool critical);

            GameObject source = sender != null
                ? sender
                : (damage.receiver != null ? damage.receiver.gameObject : null);

            // Invector stamps the weapon hitbox's world position at the moment of impact.
            Vector3? hitPoint = damage.hitPosition != Vector3.zero ? damage.hitPosition : (Vector3?)null;
            ApplyToPioneerHealth(pioneerDamage, source, critical, hitPoint);

            onReceiveDamage.Invoke(damage);
        }

        private void ApplyToPioneerHealth(float damage, GameObject source, bool isCritical, Vector3? hitPoint)
        {
            if (applyToChildren)
            {
                EnemyHealth enemyHealth = GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage, source, isCritical);
                    SpawnHitVfx(damage, source, hitPoint);
                    return;
                }
            }

            Collider collider = GetComponent<Collider>();
            if (collider != null)
                PioneerInvectorDamageBridge.ApplyPioneerDamageToCollider(collider, damage, source, isCritical, hitPoint);
        }

        private void SpawnHitVfx(float damage, GameObject source, Vector3? hitPoint)
        {
            Vector3 point = hitPoint ?? transform.position;

            // Clamp the hitbox position onto this receiver's collider surface so the
            // splatter sits on the target instead of floating mid-swing.
            Collider receiverCollider = GetComponent<Collider>();
            if (receiverCollider != null && receiverCollider.enabled && !receiverCollider.isTrigger)
                point = receiverCollider.ClosestPoint(point);

            Vector3 direction = source != null
                ? (point - source.transform.position).normalized
                : transform.forward;
            CombatHitVfx.SpawnBloodSplatter(point, direction, -direction, damage);
        }
    }
}
