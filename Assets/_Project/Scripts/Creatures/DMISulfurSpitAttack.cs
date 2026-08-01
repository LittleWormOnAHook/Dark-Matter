using Project.Combat;
using UnityEngine;

namespace Project.Creatures
{
    /// <summary>
    /// Creature ranged particle special. Spawns the assigned Prefabs/Particles VFX as a projectile.
    /// Chance is higher when the target is in the player camera frustum.
    /// </summary>
    [DisallowMultipleComponent]
    public class DMISulfurSpitAttack : MonoBehaviour
    {
        public const string DefaultPoisonSpitPrefabPath =
            "Assets/_Project/Prefabs/Particles/Poison Spit.prefab";

        [Header("Tuning")]
        [SerializeField] private bool enableAttack = true;
        [SerializeField] private float baseChance = 0.12f;
        [SerializeField] private float viewBoostedChance = 0.45f;
        [SerializeField] private float range = 14f;
        [SerializeField] private float cooldown = 6f;
        [SerializeField] private float cooldownVariation = 0f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float projectileSpeed = 18f;
        [SerializeField] private float projectileLifetime = 4f;
        [SerializeField] private float projectileRadius = 0.35f;
        [Tooltip("Forward offset from muzzle so the trigger clears the caster's head/body colliders.")]
        [SerializeField] private float spawnForwardOffset = 0.75f;
        [SerializeField] private Transform muzzle;

        [Header("VFX")]
        [Tooltip("Any particle prefab under Prefabs/Particles (Poison Spit, FireBreath, Plasma Ball, etc.).")]
        [SerializeField] private GameObject spitVfxPrefab;

        private float cooldownEndsAt;

        public bool EnableAttack => enableAttack;
        public float BaseChance => baseChance;
        public float ViewBoostedChance => viewBoostedChance;
        public float Range => range;
        public float Cooldown => cooldown;
        public float Damage => damage;
        public GameObject SpitVfxPrefab => spitVfxPrefab;
        public Transform Muzzle => muzzle != null ? muzzle : transform;

        public bool IsReady => enableAttack && spitVfxPrefab != null && Time.time >= cooldownEndsAt;

        public void ConfigureFromDefinition(DMICreatureDefinition definition)
        {
            if (definition == null)
                return;

            enableAttack = definition.enableRangedParticleAttack;
            baseChance = definition.spitBaseChance;
            viewBoostedChance = definition.spitViewBoostedChance;
            range = definition.spitRange;
            cooldown = definition.spitCooldown;
            cooldownVariation = Mathf.Clamp(definition.spitCooldownVariation, 0f, 10f);
            damage = definition.spitDamage;

            if (definition.spitVfxPrefab != null)
                spitVfxPrefab = definition.spitVfxPrefab;
        }

        public void SetSpitVfxPrefab(GameObject prefab)
        {
            spitVfxPrefab = prefab;
        }

        public void SetMuzzle(Transform muzzleTransform)
        {
            muzzle = muzzleTransform;
        }

        public bool RollSpitChance(bool targetInPlayerView)
        {
            if (!IsReady)
                return false;

            float chance = targetInPlayerView ? viewBoostedChance : baseChance;
            return Random.value <= chance;
        }

        /// <summary>
        /// Spawns the assigned ranged particle VFX as a traveling projectile toward <paramref name="target"/>.
        /// </summary>
        public bool TryFire(Transform target)
        {
            if (!IsReady || target == null)
                return false;

            Vector3 muzzlePos = Muzzle.position;
            Vector3 aimPoint = target.position + Vector3.up * 0.9f;
            Vector3 direction = aimPoint - muzzlePos;
            if (direction.sqrMagnitude < 0.0001f)
                direction = transform.forward;
            else
                direction.Normalize();

            // Clear the jaw/head capsule before the trigger exists so spawn-overlap cannot self-hit.
            Vector3 origin = muzzlePos + direction * Mathf.Max(0f, spawnForwardOffset);

            string projectileName = spitVfxPrefab != null
                ? $"{spitVfxPrefab.name}Projectile"
                : "CreatureRangedProjectile";
            GameObject projectileRoot = new GameObject(projectileName);
            projectileRoot.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
            // Keep spit off the Animal layer so it does not fight owner body colliders.
            projectileRoot.layer = 0;

            // Bind owner BEFORE any collider exists — OnTriggerEnter must never see a null source.
            DMISulfurSpitProjectile projectile = projectileRoot.AddComponent<DMISulfurSpitProjectile>();
            projectile.Configure(projectileSpeed, projectileLifetime);
            projectile.Launch(direction, damage, gameObject);

            if (spitVfxPrefab != null)
            {
                GameObject vfx = Instantiate(spitVfxPrefab, projectileRoot.transform);
                vfx.name = spitVfxPrefab.name;
                vfx.transform.localPosition = Vector3.zero;
                vfx.transform.localRotation = Quaternion.identity;
                vfx.transform.localScale = Vector3.one;
                PrepareSpitVfx(vfx);
                CombatVfxUtility.PlayParticleSystemsRecursive(vfx);
            }

            SphereCollider trigger = projectileRoot.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = projectileRadius;

            Rigidbody body = projectileRoot.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            // Re-apply ignores now that the projectile collider exists.
            projectile.IgnoreOwnerColliders();

            MarkFired();
            return true;
        }

        public void MarkFired()
        {
            float interval = Mathf.Max(0.05f, cooldown);
            float variation = Mathf.Clamp(cooldownVariation, 0f, 10f);
            cooldownEndsAt = Time.time + (variation > 0f ? interval + Random.Range(0f, variation) : interval);
        }

        private static void PrepareSpitVfx(GameObject vfx)
        {
            if (vfx == null)
                return;

            // Prefab variant may bake scene-world transforms; force projectile-local identity.
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localRotation = Quaternion.identity;

            ParticleSystem[] systems = vfx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                ParticleSystem.MainModule main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;

                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                    renderer.enabled = true;
            }
        }
    }
}
