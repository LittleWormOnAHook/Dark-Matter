using Project.AI;
using Project.AI.Invector;
using Project.Audio;
using Project.Companions;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Survival;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Shared on-hit resolution (direct damage, splash/AoE, elemental status effects, impact VFX)
    /// used by both the traveling CombatProjectile and the instant hitscan beam path in
    /// CombatProjectileSpawner, so ammo behaves identically regardless of travel mode.
    /// </summary>
    public static class CombatHitResolver
    {
        /// <summary>
        /// Broadcast radius for ranged impact noise. Listeners still gate with their own hearingRange
        /// (EnemySenses / DMICreatureAiController): hear if distance &lt;= hearingRange + this radius.
        /// </summary>
        public const float DefaultImpactNoiseRadius = 10f;

        public static bool IsOwnerCollider(GameObject owner, Collider collider)
        {
            if (collider == null || owner == null)
                return false;

            return collider.transform.IsChildOf(owner.transform) || collider.gameObject == owner;
        }

        /// <summary>
        /// World impact shared by projectile + hitscan: VFX, hit SFX, and combat-impact noise so
        /// nearby enemies/creatures can hear and optionally aggro — even when the shot hits a wall.
        /// </summary>
        public static void HandleRangedWorldImpact(
            ItemData ammoItem,
            ItemData weapon,
            Vector3 hitPoint,
            Vector3 hitNormal,
            GameObject owner,
            bool playHitAudio = true,
            GameObject impactVfxOverride = null)
        {
            SpawnImpactVfx(ammoItem, weapon, hitPoint, hitNormal, impactVfxOverride);

            if (playHitAudio)
                PlayImpactHitAudio(hitPoint);

            EnemyNoiseEvents.RaiseCombatImpactNoise(hitPoint, DefaultImpactNoiseRadius, owner);
        }

        public static void PlayImpactHitAudio(Vector3 hitPoint)
        {
            GameAudioManager audio = GameAudioManager.Instance;
            if (audio == null)
                return;

            // Reuse melee/weapon hit pipeline so ranged impacts stay on the same combat SFX profile.
            audio.PlayWeaponHit(hitPoint, isCritical: false);
        }

        /// <summary>Applies direct damage to whatever the collider resolves to, plus VFX/UI feedback.</summary>
        public static void ApplyDirectHit(
            Collider collider,
            Vector3 hitPoint,
            Vector3 travelDirection,
            float damage,
            bool isCritical,
            GameObject owner)
        {
            IDamageable damageable = DamageableUtility.GetDamageable(collider);
            if (damageable == null)
                return;

            GameObject damageSource = owner != null ? owner : collider.gameObject;

            // Ranged hits never go through PioneerInvectorDamageReceiver / TryHitStagger. Stamp the
            // killing impulse before TakeDamage so EnemyDeathSequence can launch the corpse ragdoll.
            EnemyHealth enemyHealth = damageable as EnemyHealth;
            if (enemyHealth == null && collider != null)
                enemyHealth = collider.GetComponentInParent<EnemyHealth>();

            EnemyInvectorRagdollBridge ragdollBridge = null;
            if (enemyHealth != null && !enemyHealth.IsDead)
            {
                ragdollBridge = enemyHealth.GetComponent<EnemyInvectorRagdollBridge>();
                ragdollBridge?.RememberHitForDeath(
                    hitPoint,
                    travelDirection,
                    damage,
                    damageSource != null ? damageSource.transform : null);
            }

            damageable.TakeDamage(damage, damageSource, isCritical);
            NotifyEnemyProjectileHitOnAlly(damageSource, damageable, collider.transform, damage);

            if (ragdollBridge != null && enemyHealth != null && !enemyHealth.IsDead)
            {
                ragdollBridge.TryHitStaggerFromRanged(
                    hitPoint,
                    travelDirection,
                    damage,
                    isCritical,
                    damageSource != null ? damageSource.transform : null);
            }

            Vector3 normal = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector3.up;
            CombatHitVfx.SpawnBloodSplatter(hitPoint, travelDirection, normal, damage);

            // EnemyHealth/CompanionHealth already show their own floating damage number inside
            // TakeDamage (shared with the melee hit path) — showing it again here would double the
            // popup on every ranged hit. Targets that don't self-report (player SurvivalStats,
            // ResourceNode, etc.) still need us to show it.
            if (!SelfReportsDamageUi(damageable))
                CombatUiSpawner.ShowDamage(damage, hitPoint, isCritical);
        }

        private static bool SelfReportsDamageUi(IDamageable damageable)
        {
            return damageable is EnemyHealth || damageable is CompanionHealth;
        }

        /// <summary>
        /// When an enemy-owned projectile/beam hits the player or a pioneer, raise squad alert events
        /// and incoming-hit VFX. CompanionHealth already raises OnCompanionAttackedBy inside TakeDamage;
        /// the player path must be handled here because SurvivalStats does not.
        /// </summary>
        private static void NotifyEnemyProjectileHitOnAlly(
            GameObject owner,
            IDamageable damageable,
            Transform hitTransform,
            float damage)
        {
            if (owner == null || damageable == null || damage <= 0f)
                return;

            EnemyHealth attacker = owner.GetComponentInParent<EnemyHealth>();
            if (attacker == null || attacker.IsDead)
                return;

            Transform receiver = hitTransform != null ? hitTransform.root : null;

            if (damageable is SurvivalStats)
            {
                PlayerCombatEvents.RaisePlayerAttackedBy(attacker);
                if (receiver != null)
                    CombatHitVfx.SpawnIncomingEnemyHit(attacker.transform, receiver, damage);
                return;
            }

            if (damageable is CompanionHealth && receiver != null)
                CombatHitVfx.SpawnIncomingEnemyHit(attacker.transform, receiver, damage);
        }

        /// <summary>Splash/AoE damage around the impact point, falling off linearly to ammoItem.splashDamageFalloff at the edge.</summary>
        public static void ApplySplash(
            ItemData ammoItem,
            Vector3 center,
            float centerDamage,
            GameObject owner,
            Collider excludeCollider)
        {
            if (ammoItem == null || !ammoItem.HasSplashDamage)
                return;

            Collider[] hits = Physics.OverlapSphere(center, ammoItem.splashRadius, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i];
                if (hitCollider == null || hitCollider == excludeCollider || IsOwnerCollider(owner, hitCollider))
                    continue;

                IDamageable damageable = DamageableUtility.GetDamageable(hitCollider);
                if (damageable == null)
                    continue;

                Vector3 closest = GetClosestPointSafe(hitCollider, center);
                float distance = Vector3.Distance(center, closest);
                float t = Mathf.Clamp01(distance / Mathf.Max(0.01f, ammoItem.splashRadius));
                float falloffDamage = Mathf.Lerp(centerDamage, centerDamage * ammoItem.splashDamageFalloff, t);
                if (falloffDamage <= 0.01f)
                    continue;

                EnemyHealth splashEnemy = damageable as EnemyHealth;
                if (splashEnemy == null)
                    splashEnemy = hitCollider.GetComponentInParent<EnemyHealth>();
                if (splashEnemy != null && !splashEnemy.IsDead)
                {
                    Vector3 outward = closest - center;
                    if (outward.sqrMagnitude < 0.0001f)
                        outward = Vector3.up;
                    splashEnemy.GetComponent<EnemyInvectorRagdollBridge>()?.RememberHitForDeath(
                        closest,
                        outward,
                        falloffDamage,
                        owner != null ? owner.transform : null);
                }

                damageable.TakeDamage(falloffDamage, owner, false);
                if (!SelfReportsDamageUi(damageable))
                    CombatUiSpawner.ShowDamage(falloffDamage, closest, false);
                ApplyStatusEffect(ammoItem, hitCollider, owner);
            }
        }

        /// <summary>
        /// Collider.ClosestPoint only supports Box/Sphere/Capsule and convex MeshColliders.
        /// </summary>
        private static Vector3 GetClosestPointSafe(Collider collider, Vector3 point)
        {
            if (collider == null)
                return point;

            if (collider is BoxCollider
                || collider is SphereCollider
                || collider is CapsuleCollider
                || (collider is MeshCollider meshCollider && meshCollider.convex))
            {
                return collider.ClosestPoint(point);
            }

            return collider.bounds.ClosestPoint(point);
        }

        /// <summary>Applies the ammo's elemental status effect (Burning/Frozen/Shocked/etc.) to whatever the collider resolves to.</summary>
        public static void ApplyStatusEffect(ItemData ammoItem, Collider collider, GameObject owner)
        {
            if (ammoItem == null || collider == null || !ammoItem.HasStatusEffect)
                return;

            CombatStatusEffect.Apply(ammoItem, collider.gameObject, owner);
        }

        public static void SpawnImpactVfx(
            ItemData ammoItem,
            ItemData weapon,
            Vector3 point,
            Vector3 normal,
            GameObject impactVfxOverride = null)
        {
            GameObject prefab = impactVfxOverride;
            if (prefab == null)
            {
                prefab = ammoItem != null && ammoItem.impactVfxPrefab != null
                    ? ammoItem.impactVfxPrefab
                    : weapon != null ? weapon.impactVfxPrefab : null;
            }

            if (prefab == null)
                return;

            Quaternion rotation = normal.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(normal, Vector3.up)
                : Quaternion.identity;

            GameObject instance = PoolManager.Spawn(prefab, point, rotation);
            // Reactivating a pooled instance doesn't reliably re-trigger playOnAwake particle
            // systems on every Unity version, so explicitly clear+replay them on every reuse.
            CombatVfxUtility.PlayParticleSystemsRecursive(instance);
            PoolManager.ReleaseDelayed(instance, 4f);
        }

        public static void SpawnMuzzleFlash(ItemData ammoItem, ItemData weapon, Transform muzzle)
        {
            GameObject prefab = ammoItem != null && ammoItem.muzzleFlashPrefab != null
                ? ammoItem.muzzleFlashPrefab
                : weapon != null ? weapon.muzzleFlashPrefab : null;

            if (prefab == null || muzzle == null)
                return;

            GameObject instance = PoolManager.Spawn(prefab, muzzle.position, muzzle.rotation, muzzle);
            CombatVfxUtility.PlayParticleSystemsRecursive(instance);
            PoolManager.ReleaseDelayed(instance, 2f);
        }

        /// <summary>
        /// Pulse laser visual: stretches beamVfx (preferred) or tracerPrefab from muzzle to impact.
        /// Used by hitscan laser ammo — no traveling projectile is spawned.
        /// Attaches <see cref="HitscanBeamMuzzleFollow"/> so the beam stays glued to the live muzzle
        /// while the shooter moves / tracks (visual only; damage already applied at fire time).
        /// </summary>
        public static void SpawnHitscanBeamVisual(
            ItemData ammoItem,
            ItemData weapon,
            Transform muzzle,
            Vector3 origin,
            Vector3 endPoint,
            Vector3 direction,
            float range)
        {
            Vector3 delta = endPoint - origin;
            float length = delta.magnitude;
            if (length < 0.01f)
                return;

            float followRange = Mathf.Max(1f, range > 0.01f ? range : length);

            // Prefer the drawn weapon's muzzle/Laser/laserSight stack (Sci-Fi Pistol, Survival Rifle, Mining Tool, etc.).
            if (TryPulseWeaponLaserStack(muzzle, followRange, 0.35f))
                return;

            GameObject beamPrefab = ammoItem != null && ammoItem.beamVfxPrefab != null
                ? ammoItem.beamVfxPrefab
                : weapon != null ? weapon.beamVfxPrefab : null;

            GameObject tracerPrefab = CombatVfxUtility.ResolveTracerPrefab(ammoItem, weapon);

            Vector3 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : delta / length;
            Quaternion rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (beamPrefab != null)
            {
                GameObject beam = PoolManager.Spawn(beamPrefab, origin, rotation);
                ApplyBeamLine(beam, origin, endPoint);
                AttachMuzzleFollow(beam, muzzle, followRange);
                CombatVfxUtility.PlayParticleSystemsRecursive(beam);
                PoolManager.ReleaseDelayed(beam, 0.35f);
            }
            else if (tracerPrefab != null)
            {
                // Particle/trail tracers: place along the shot and scale to span muzzle→impact.
                GameObject tracer = PoolManager.Spawn(tracerPrefab, origin, rotation);
                ApplyBeamLine(tracer, origin, endPoint);
                StretchTracerAlongBeam(tracer, length);
                AttachMuzzleFollow(tracer, muzzle, followRange);
                CombatVfxUtility.PlayParticleSystemsRecursive(tracer);
                PoolManager.ReleaseDelayed(tracer, 0.45f);
            }
        }

        private static bool TryPulseWeaponLaserStack(Transform muzzle, float range, float durationSeconds)
        {
            if (muzzle == null)
                return false;

            if (!HitscanBeamMuzzleFollow.TryFindWeaponLaserStack(muzzle, out Transform laser, out Transform stackMuzzle))
                return false;

            HitscanBeamMuzzleFollow follow = laser.GetComponent<HitscanBeamMuzzleFollow>();
            if (follow == null)
                follow = laser.gameObject.AddComponent<HitscanBeamMuzzleFollow>();

            follow.enabled = true;
            follow.ConfigureWeaponLaserPulse(laser, stackMuzzle, range, durationSeconds);
            return true;
        }

        private static void AttachMuzzleFollow(GameObject root, Transform muzzle, float range)
        {
            if (root == null || muzzle == null)
                return;

            HitscanBeamMuzzleFollow follow = root.GetComponent<HitscanBeamMuzzleFollow>();
            if (follow == null)
                follow = root.AddComponent<HitscanBeamMuzzleFollow>();

            follow.enabled = true;
            follow.Configure(muzzle, range);
        }

        private static void ApplyBeamLine(GameObject root, Vector3 origin, Vector3 endPoint)
        {
            if (root == null)
                return;

            LineRenderer[] lines = root.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lines.Length; i++)
            {
                LineRenderer line = lines[i];
                if (line == null)
                    continue;

                line.useWorldSpace = true;
                line.positionCount = 2;
                line.SetPosition(0, origin);
                line.SetPosition(1, endPoint);
                line.enabled = true;
            }
        }

        private static void StretchTracerAlongBeam(GameObject root, float length)
        {
            if (root == null)
                return;

            // Particle-only tracers (ballistic bullet FX) must not be Z-stretched to beam length —
            // that explodes local emission offsets into multi-meter displacement behind the player.
            // LineRenderer beams already get exact endpoints via ApplyBeamLine.
            if (root.GetComponentInChildren<LineRenderer>(true) == null &&
                root.GetComponentInChildren<MeshRenderer>(true) == null &&
                root.GetComponentInChildren<ParticleSystem>(true) != null)
            {
                return;
            }

            Transform t = root.transform;
            Vector3 scale = t.localScale;
            if (scale.z > 0.001f && length > 0.01f)
            {
                // Many projectile visuals are ~1 unit long on Z — stretch to beam length.
                scale.z = length;
                t.localScale = scale;
            }
        }

        /// <summary>One-shot firing sound played at the muzzle position. Falls back to the weapon's own fire sound if the loaded ammo doesn't specify one — except hitscan laser ammo, which must not steal gunfire SFX.</summary>
        public static void PlayFireSound(ItemData ammoItem, ItemData weapon, Transform muzzle)
        {
            AudioClip clip = ammoItem != null ? ammoItem.fireSound : null;
            if (clip == null &&
                (ammoItem == null || !ammoItem.isHitscanBeam) &&
                weapon != null)
            {
                clip = weapon.fireSound;
            }

            if (clip == null || muzzle == null)
                return;

            AudioSource.PlayClipAtPoint(clip, muzzle.position);
        }
    }
}
