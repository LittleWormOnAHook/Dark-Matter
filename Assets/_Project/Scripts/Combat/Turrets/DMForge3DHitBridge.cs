using System.Collections.Generic;
using FORGE3D;
using Project.AI;
using Project.Combat;
using Project.Companions;
using Project.Interaction;
using Project.Survival;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// FORGE3D impact adapter: project damage pipeline, no rigidbody torque on Kade.
    /// Vendor ApplyForceAtPosition is what tilted the player mesh after turret hits.
    /// </summary>
    public static class DMForge3DHitBridge
    {
        private const int PlayerLayer = 8;
        private static readonly Dictionary<int, float> BeamNextDamage = new Dictionary<int, float>(32);
        private static readonly Collider[] SplashBuffer = new Collider[32];
        private static int lastDirectHitFrame = -1;
        private static Collider lastDirectHitCollider;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            BeamNextDamage.Clear();
            SplashDamaged.Clear();
            lastDirectHitFrame = -1;
            lastDirectHitCollider = null;
        }

        public static bool IsPlayerBody(Collider collider, Rigidbody rigidbody)
        {
            if (collider != null)
            {
                if (collider.gameObject.layer == PlayerLayer)
                    return true;
                if (collider.GetComponentInParent<SurvivalStats>() != null)
                    return true;
            }

            if (rigidbody != null && rigidbody.GetComponentInParent<SurvivalStats>() != null)
                return true;

            return false;
        }

        public static void StripPlayerTorque(Collider collider, Rigidbody rigidbody)
        {
            Rigidbody rb = rigidbody;
            if (rb == null && collider != null)
                rb = collider.attachedRigidbody;
            if (rb == null)
                return;

            SurvivalStats stats = rb.GetComponentInParent<SurvivalStats>();
            if (stats == null)
                return;

            if (!rb.isKinematic)
                rb.angularVelocity = Vector3.zero;
            Transform root = stats.transform;
            Vector3 euler = root.eulerAngles;
            root.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }

        public static bool TryHandleImpact(
            Component shot,
            RaycastHit hit,
            Vector3 direction,
            float vendorForce,
            bool continuous = false)
        {
            return TryHandleImpact(shot, hit.collider, hit.point, direction, vendorForce, hit.rigidbody, continuous);
        }

        public static bool TryHandleImpact(
            Component shot,
            Collider collider,
            Vector3 point,
            Vector3 direction,
            float vendorForce,
            Rigidbody rigidbody = null,
            bool continuous = false)
        {
            if (collider == null && rigidbody == null)
                return false;

            if (rigidbody == null && collider != null)
                rigidbody = collider.attachedRigidbody;

            bool player = IsPlayerBody(collider, rigidbody);
            if (player)
                StripPlayerTorque(collider, rigidbody);

            DMTurretRuntime turret = DMTurretWeaponContext.Resolve(shot);
            if (turret == null || turret.IsDestroyed)
            {
                // Still swallow vendor torque on the player even if this shot has no turret stamp.
                return player;
            }

            if (!IsImpactReachable(turret, shot, collider, point))
                return true;

            TryApplyTurretDamage(turret, shot, collider, point, direction, continuous, losVerified: true);

            if (player)
                StripPlayerTorque(collider, rigidbody);

            // Turret-owned FORGE3D shots always skip vendor AddForce; DMI applies damage + faction rules.
            return true;
        }

        private static bool IsImpactReachable(
            DMTurretRuntime turret,
            Component shot,
            Collider collider,
            Vector3 point)
        {
            if (turret == null || collider == null)
                return false;

            Vector3 losOrigin = ResolveLosOrigin(turret, shot);
            Transform shotRoot = shot != null ? shot.transform : null;
            return DMTurretLos.IsImpactClear(
                losOrigin,
                point,
                collider,
                turret.transform,
                shotRoot,
                turret.gameObject);
        }

        private static Vector3 ResolveLosOrigin(DMTurretRuntime turret, Component shot)
        {
            if (shot is F3DBeam || shot is F3DLightning)
                return shot.transform.position;
            return ResolveMuzzle(turret, shot);
        }

        private static Vector3 ResolveMuzzle(DMTurretRuntime turret, Component shot)
        {
            if (shot != null)
            {
                DMTurretShotStamp stamp = shot.GetComponent<DMTurretShotStamp>();
                if (stamp != null)
                    return stamp.Origin;
            }

            Transform barrel = turret != null ? turret.Barrel : null;
            if (barrel != null)
                return barrel.position;
            if (shot != null)
                return shot.transform.position;
            return turret != null ? turret.transform.position : Vector3.zero;
        }

        public static bool TryApplyTurretDamage(
            DMTurretRuntime turret,
            Component shot,
            Collider collider,
            Vector3 point,
            Vector3 direction,
            bool continuous,
            bool losVerified = false)
        {
            if (turret == null || collider == null)
                return false;

            if (!losVerified)
            {
                Vector3 losOrigin = ResolveLosOrigin(turret, shot);
                Transform shotRoot = shot != null ? shot.transform : null;
                if (!DMTurretLos.IsImpactClear(
                        losOrigin,
                        point,
                        collider,
                        turret.transform,
                        shotRoot,
                        turret.gameObject))
                    return false;
            }

            IDamageable damageable = DamageableUtility.GetDamageable(collider);
            if (damageable == null || !turret.CanDamage(damageable))
                return false;

            if (continuous && !AllowBeamTick(turret, collider))
                return true;

            DMTurretProfile profile = DMTurretProfile.Live;
            float amount = turret.Damage;
            bool critical = false;
            GameObject source = turret.gameObject;
            DamageInfo info = DamageInfo.FromHit(amount, critical, source, point);
            info.DamageType = turret.AmmoType.ToDamageType();
            info.Element = turret.AmmoType.ToCombatElement();

            GameObject root = collider.transform.root.gameObject;
            bool applied = CombatDamageApplicator.ApplyToDamageable(damageable, root, in info);
            if (!applied)
                return false;

            lastDirectHitFrame = Time.frameCount;
            lastDirectHitCollider = collider;

            Vector3 travel = direction.sqrMagnitude > 0.0001f ? direction.normalized : turret.transform.forward;
            if (damageable is EnemyHealth enemyHealth)
            {
                DMEnemyHitMarks.HandleRangedHit(
                    enemyHealth,
                    collider,
                    point,
                    -travel,
                    travel,
                    amount,
                    null,
                    null);
            }
            else if (damageable is SurvivalStats)
            {
                DMEnemyHitQuery.ResolveMarkSurface(collider, point, travel, Vector3.zero, out _, out Vector3 normal);
                DMPlayerHitboxRig.TryHandleRangedHit(collider, point, normal, travel, amount, null, null);
            }

            float splash = turret.SplashRadius;
            if (splash > 0.05f)
                ApplyFactionSplash(turret, point, amount, splash, collider);

            RaisePlayerTurretImpactNoise(turret, point);
            return true;
        }

        private static bool AllowBeamTick(DMTurretRuntime turret, Collider collider)
        {
            int key = collider.GetEntityId().GetHashCode() ^ (turret.GetEntityId().GetHashCode() << 8);
            float now = Time.time;
            if (BeamNextDamage.TryGetValue(key, out float next) && now < next)
                return false;

            float interval = turret.BeamDamageInterval;
            BeamNextDamage[key] = now + interval;
            return true;
        }

        private static readonly HashSet<IDamageable> SplashDamaged = new HashSet<IDamageable>(16);

        private static void ApplyFactionSplash(
            DMTurretRuntime turret,
            Vector3 center,
            float centerDamage,
            float radius,
            Collider exclude)
        {
            SplashDamaged.Clear();
            int count = Physics.OverlapSphereNonAlloc(
                center,
                radius,
                SplashBuffer,
                ~0,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider candidate = SplashBuffer[i];
                SplashBuffer[i] = null;
                if (candidate == null || candidate == exclude)
                    continue;
                ApplySplashToCollider(turret, center, centerDamage, radius, candidate);
            }

            int hitboxCount = DMEnemyHitQuery.CollectHitboxesInRadiusNonAlloc(
                center,
                radius,
                turret.gameObject,
                SplashBuffer);
            for (int i = 0; i < hitboxCount; i++)
            {
                Collider candidate = SplashBuffer[i];
                SplashBuffer[i] = null;
                if (candidate == null || candidate == exclude)
                    continue;
                ApplySplashToCollider(turret, center, centerDamage, radius, candidate);
            }
        }

        private static void ApplySplashToCollider(
            DMTurretRuntime turret,
            Vector3 center,
            float centerDamage,
            float radius,
            Collider candidate)
        {
            if (candidate == null)
                return;
            if (candidate.transform.IsChildOf(turret.transform))
                return;

            IDamageable damageable = DamageableUtility.GetDamageable(candidate);
            if (damageable == null || !turret.CanDamage(damageable) || !SplashDamaged.Add(damageable))
                return;

            Vector3 closest = candidate.ClosestPoint(center);
            if (!DMTurretLos.IsImpactClear(
                    center,
                    closest,
                    candidate,
                    turret.transform,
                    null,
                    turret.gameObject))
                return;

            float t = Mathf.Clamp01(Vector3.Distance(center, closest) / Mathf.Max(0.01f, radius));
            float amount = Mathf.Lerp(centerDamage, centerDamage * 0.25f, t);
            if (amount <= 0.01f)
                return;

            DamageInfo info = DamageInfo.FromHit(amount, false, turret.gameObject, closest);
            CombatDamageApplicator.ApplyToDamageable(
                damageable,
                candidate.transform.root.gameObject,
                in info);
        }

        /// <summary>FORGE3D impact prefab splash (seeker / plasma / laser pulse / missile burst).</summary>
        public static void TryApplyVendorImpactSplash(Vector3 center, F3DFXType fxType, Collider excludeDirectHit = null)
        {
            DMTurretRuntime turret = DMTurretWeaponContext.Current;
            if (turret == null || turret.IsDestroyed || turret.Faction != DMTurretFaction.Player)
                return;

            float radius = ResolveVendorSplashRadius(fxType, turret);
            if (radius <= 0.05f)
                return;

            if (excludeDirectHit == null && Time.frameCount == lastDirectHitFrame)
                excludeDirectHit = lastDirectHitCollider;

            float amount = turret.Damage;
            if (fxType != F3DFXType.Seeker)
                amount *= 0.55f;

            ApplyFactionSplash(turret, center, amount, radius, excludeDirectHit);
            RaisePlayerTurretImpactNoise(turret, center);
        }

        public static void TryApplyMissileExplosion(F3DMissile missile, Vector3 center, Collider excludeDirectHit)
        {
            DMTurretRuntime turret = DMTurretWeaponContext.Resolve(missile);
            if (turret == null || turret.IsDestroyed || turret.Faction != DMTurretFaction.Player)
                return;

            float radius = Mathf.Max(turret.SplashRadius, 3.5f);
            ApplyFactionSplash(turret, center, turret.Damage, radius, excludeDirectHit);
            RaisePlayerTurretImpactNoise(turret, center);
        }

        private static float ResolveVendorSplashRadius(F3DFXType fxType, DMTurretRuntime turret)
        {
            switch (fxType)
            {
                case F3DFXType.Seeker:
                    return Mathf.Max(turret.SplashRadius, 3.5f);
                case F3DFXType.PlasmaGun:
                    return 2.75f;
                case F3DFXType.LaserImpulse:
                    return 2.25f;
                case F3DFXType.SoloGun:
                    return 1.35f;
                default:
                    return 0f;
            }
        }

        private static void RaisePlayerTurretImpactNoise(DMTurretRuntime turret, Vector3 point)
        {
            if (turret == null || turret.Faction != DMTurretFaction.Player)
                return;

            DM_CombatCoreProfile cp = DM_CombatCoreProfile.Live;
            float radius = cp != null ? cp.rangedImpactAlertRadius : 8f;
            EnemyNoiseEvents.RaiseCombatImpactNoise(point, radius, turret.gameObject);
        }
    }
}
