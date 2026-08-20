using Project.Combat;
using Project.Interaction;
using UnityEngine;

namespace Project.Creatures
{
    /// <summary>
    /// Traveling sulfur spit projectile. Carries Poison Spit particle VFX as a child.
    /// Hits player / pets / companions / non-ally creatures via IDamageable.
    /// Never damages the casting creature (owner hierarchy + Physics.IgnoreCollision).
    /// </summary>
    [DisallowMultipleComponent]
    public class DMISulfurSpitProjectile : MonoBehaviour
    {
        private static readonly Collider[] ProximityOverlapBuffer = new Collider[12];

        [SerializeField] private float speed = 18f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private GameObject source;

        private Vector3 direction;
        private float expiresAt;
        private bool consumed;
        private bool launched;
        private float hitRadius = 0.35f;

        public void Configure(float projectileSpeed, float projectileLifetime)
        {
            speed = projectileSpeed;
            lifetime = projectileLifetime;
        }

        public void ConfigureHitRadius(float radius)
        {
            hitRadius = Mathf.Max(0.05f, radius);
        }

        public void Launch(Vector3 launchDirection, float launchDamage, GameObject launchSource)
        {
            direction = launchDirection.sqrMagnitude > 0.0001f
                ? launchDirection.normalized
                : transform.forward;
            damage = launchDamage;
            source = launchSource;
            expiresAt = Time.time + lifetime;
            consumed = false;
            launched = true;
            transform.rotation = Quaternion.LookRotation(direction);

            IgnoreOwnerColliders();
        }

        /// <summary>
        /// Hard-ignore every collider under the caster so spawn-inside-mesh overlaps cannot self-hit.
        /// </summary>
        public void IgnoreOwnerColliders()
        {
            if (source == null)
                return;

            Collider[] ownColliders = GetComponentsInChildren<Collider>(true);
            Collider[] ownerColliders = source.GetComponentsInChildren<Collider>(true);
            if (ownColliders == null || ownerColliders == null)
                return;

            for (int i = 0; i < ownColliders.Length; i++)
            {
                Collider own = ownColliders[i];
                if (own == null)
                    continue;

                for (int j = 0; j < ownerColliders.Length; j++)
                {
                    Collider ownerCol = ownerColliders[j];
                    if (ownerCol == null)
                        continue;

                    Physics.IgnoreCollision(own, ownerCol, true);
                }
            }
        }

        private void Update()
        {
            if (consumed || !launched)
                return;

            if (Time.time >= expiresAt)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += direction * (speed * Time.deltaTime);
            TryProximityHit();
        }

        private void TryProximityHit()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                hitRadius,
                ProximityOverlapBuffer,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < hitCount; i++)
            {
                Collider other = ProximityOverlapBuffer[i];
                if (other == null)
                    continue;

                if (TryConsumeHit(other))
                    return;
            }
        }

        private bool TryConsumeHit(Collider other)
        {
            if (consumed || !launched || other == null)
                return false;

            if (IsOwnerHit(other))
                return false;

            DMICreatureBridge sourceBridge = source != null
                ? source.GetComponentInParent<DMICreatureBridge>()
                : null;
            if (sourceBridge != null &&
                DMICreatureTargetResolver.IsAllyCreature(sourceBridge, other.transform))
                return false;

            IDamageable damageable = DamageableUtility.GetDamageable(other);
            if (damageable == null)
            {
                MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IDamageable found)
                    {
                        damageable = found;
                        break;
                    }
                }
            }

            if (damageable == null)
                return false;

            if (IsOwnerDamageable(damageable))
                return false;

            consumed = true;
            CombatHitResolver.ApplyDirectHit(
                other,
                transform.position,
                direction,
                damage,
                false,
                source);
            Destroy(gameObject);
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            TryConsumeHit(other);
        }

        private bool IsOwnerHit(Collider other)
        {
            if (source == null || other == null)
                return false;

            return CombatHitResolver.IsOwnerCollider(source, other);
        }

        private bool IsOwnerDamageable(IDamageable damageable)
        {
            if (source == null || damageable == null)
                return false;

            MonoBehaviour behaviour = damageable as MonoBehaviour;
            if (behaviour == null)
                return false;

            Transform damageableTransform = behaviour.transform;
            if (damageableTransform == source.transform)
                return true;

            return damageableTransform.IsChildOf(source.transform) ||
                   source.transform.IsChildOf(damageableTransform);
        }
    }
}
