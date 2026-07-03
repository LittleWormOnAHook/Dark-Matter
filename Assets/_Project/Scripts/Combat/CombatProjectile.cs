using Project.Data;
using Project.Interaction;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    public class CombatProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 85f;
        [SerializeField] private float maxLifetime = 3f;
        [SerializeField] private float radius = 0.08f;
        [SerializeField] private LayerMask hitLayers = ~0;

        private GameObject owner;
        private AmmoType ammoType;
        private float damage;
        private bool isCritical;
        private Vector3 velocity;
        private Vector3 previousPosition;
        private float spawnTime;
        private bool hasHit;
        private bool launched;

        public void Launch(
            GameObject ownerRoot,
            Vector3 direction,
            float damageAmount,
            AmmoType type,
            float speedOverride = 0f,
            bool critical = false)
        {
            owner = ownerRoot;
            ammoType = type;
            damage = damageAmount;
            isCritical = critical;
            speed = speedOverride > 0f ? speedOverride : speed;
            velocity = direction.sqrMagnitude > 0.0001f ? direction.normalized * speed : Vector3.forward * speed;
            previousPosition = transform.position;
            spawnTime = Time.time;
            hasHit = false;
            launched = true;
        }

        private void Update()
        {
            if (!launched || hasHit)
                return;

            if (Time.time - spawnTime > maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            previousPosition = transform.position;
            transform.position += velocity * Time.deltaTime;
            SweepForHit();
        }

        private void SweepForHit()
        {
            Vector3 delta = transform.position - previousPosition;
            float distance = delta.magnitude;
            if (distance <= 0.0001f)
                return;

            if (Physics.SphereCast(
                    previousPosition,
                    radius,
                    delta.normalized,
                    out RaycastHit hit,
                    distance,
                    hitLayers,
                    QueryTriggerInteraction.Ignore))
            {
                if (IsOwnerCollider(hit.collider))
                    return;

                ResolveHit(hit.collider, hit.point);
            }
        }

        private bool IsOwnerCollider(Collider collider)
        {
            if (collider == null || owner == null)
                return false;

            return collider.transform.IsChildOf(owner.transform) || collider.gameObject == owner;
        }

        private void ResolveHit(Collider collider, Vector3 hitPoint)
        {
            hasHit = true;

            float appliedDamage = damage;
            if (ammoType == AmmoType.ResonanceStabilizer)
            {
                EchoStabilizeReceiver echo = collider.GetComponentInParent<EchoStabilizeReceiver>();
                if (echo != null)
                    appliedDamage = Mathf.Max(1f, damage * 0.15f);
            }

            IDamageable damageable = DamageableUtility.GetDamageable(collider);
            if (damageable != null)
            {
                damageable.TakeDamage(appliedDamage, owner != null ? owner : gameObject, isCritical);
                Vector3 normal = (transform.position - previousPosition).normalized;
                CombatHitVfx.SpawnBloodSplatter(hitPoint, velocity, normal, appliedDamage);
                CombatUiSpawner.ShowDamage(appliedDamage, hitPoint, isCritical);
            }

            CombatStatusEffect.Apply(ammoType, collider.gameObject, owner);
            Destroy(gameObject);
        }
    }
}
