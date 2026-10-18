using Project.Building;
using Project.Combat;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Shared turret / FORGE3D shot line-of-sight. Vendor prefabs often author
    /// <c>layerMask = 0</c> (plasma beams) so raycasts never hit Default / Building / rocks.
    /// Occluders always include those world layers; queries ignore triggers.
    /// </summary>
    public static class DMTurretLos
    {
        public const float MuzzleSkin = 0.12f;
        public const float ImpactSlack = 0.08f;
        private const int HitBufferSize = 16;
        private const int PlayerLayer = 8;

        private static readonly RaycastHit[] Hits = new RaycastHit[HitBufferSize];
        private static int cachedFrame = -1;
        private static int cachedOccluderMask;
        private static int cachedShotMask;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            cachedFrame = -1;
            cachedOccluderMask = 0;
            cachedShotMask = 0;
        }

        /// <summary>
        /// World blockers: Default, Terrain/Climbable, Building, nature rocks (PW_Object_*),
        /// Resource / Pushable / StopMove / BlockAIRayCast. Never Ignore Raycast or Triggers.
        /// </summary>
        public static int OccluderMask
        {
            get
            {
                RefreshMasks();
                return cachedOccluderMask;
            }
        }

        /// <summary>Occluders plus Player / Enemy / CompanionAI / DMHitbox so shots can confirm a body.</summary>
        public static int ShotMask
        {
            get
            {
                RefreshMasks();
                return cachedShotMask;
            }
        }

        public static int ResolveShotMask(int authoredMask)
        {
            return authoredMask | ShotMask;
        }

        public static bool HasLineOfSight(Vector3 origin, Transform target, Transform ignoreRoot)
        {
            if (target == null)
                return false;

            Vector3 aim = AimPoint(target);
            return HasLineOfSight(origin, aim, target, ignoreRoot);
        }

        public static bool HasLineOfSight(
            Vector3 origin,
            Vector3 targetPoint,
            Transform expectedRoot,
            Transform ignoreRoot)
        {
            GameObject owner = ignoreRoot != null ? ignoreRoot.gameObject : null;
            if (!TryFirstSolidHit(origin, targetPoint, ignoreRoot, ShotMask, out RaycastHit hit, null, owner))
                return expectedRoot != null;

            return IsExpectedHit(hit.collider, expectedRoot, null);
        }

        public static bool IsImpactClear(
            Vector3 muzzle,
            Vector3 hitPoint,
            Collider expected,
            Transform ignoreRoot,
            Transform shotRoot = null,
            GameObject losOwner = null)
        {
            if (expected == null)
                return false;

            Transform expectedRoot = expected.transform.root;
            if (!TryFirstSolidHit(muzzle, hitPoint, ignoreRoot, ShotMask, out RaycastHit hit, shotRoot, losOwner))
                return true;

            return IsExpectedHit(hit.collider, expectedRoot, expected);
        }

        public static bool TryWorldRaycast(
            Component shot,
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            int authoredMask,
            out RaycastHit hit)
        {
            hit = default;
            if (direction.sqrMagnitude < 0.0001f)
                return false;

            direction.Normalize();
            Transform ignoreRoot = null;
            DMTurretRuntime turret = DMTurretWeaponContext.Resolve(shot);
            if (turret != null)
                ignoreRoot = turret.transform;

            Vector3 start = origin + direction * MuzzleSkin;
            float distance = Mathf.Max(0.01f, maxDistance - MuzzleSkin);
            int mask = ResolveShotMask(authoredMask);
            int count = Physics.RaycastNonAlloc(
                start,
                direction,
                Hits,
                distance,
                mask,
                QueryTriggerInteraction.Ignore);

            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = Hits[i];
                if (candidate.collider == null)
                    continue;
                if (ShouldIgnoreCollider(candidate.collider, ignoreRoot, shot != null ? shot.transform : null))
                    continue;
                if (candidate.distance >= best)
                    continue;

                best = candidate.distance;
                hit = candidate;
                found = true;
            }

            GameObject owner = turret != null ? turret.gameObject : null;
            MergeHitboxRay(start, direction, distance, mask, owner, ref best, ref hit, ref found);
            return found;
        }

        public static Vector3 AimPoint(Transform target)
        {
            if (target == null)
                return Vector3.zero;

            if (target.TryGetComponent<Collider>(out Collider rootCollider) && rootCollider.enabled)
                return rootCollider.bounds.center;

            return target.position + Vector3.up * 1.1f;
        }

        public static void ApplyShotMask(Component spawned)
        {
            if (spawned == null)
                return;

            int mask = ShotMask;
            if (spawned is FORGE3D.F3DBeam beam)
                beam.layerMask = mask;
            else if (spawned is FORGE3D.F3DProjectile projectile)
                projectile.layerMask = mask;
            else if (spawned is FORGE3D.F3DLightning lightning)
                lightning.layerMask = mask;
            else if (spawned is FORGE3D.F3DMissile missile)
                missile.layerMask = mask;
        }

        private static bool TryFirstSolidHit(
            Vector3 origin,
            Vector3 targetPoint,
            Transform ignoreRoot,
            int mask,
            out RaycastHit hit,
            Transform shotRoot = null,
            GameObject losOwner = null)
        {
            hit = default;
            Vector3 delta = targetPoint - origin;
            float distance = delta.magnitude;
            if (distance <= MuzzleSkin)
                return false;

            Vector3 direction = delta / distance;
            Vector3 start = origin + direction * MuzzleSkin;
            float cast = Mathf.Max(0.01f, distance - MuzzleSkin + ImpactSlack);
            int count = Physics.RaycastNonAlloc(
                start,
                direction,
                Hits,
                cast,
                mask,
                QueryTriggerInteraction.Ignore);

            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit candidate = Hits[i];
                if (candidate.collider == null)
                    continue;
                if (ShouldIgnoreCollider(candidate.collider, ignoreRoot, shotRoot))
                    continue;
                if (candidate.distance >= best)
                    continue;

                best = candidate.distance;
                hit = candidate;
                found = true;
            }

            MergeHitboxRay(start, direction, cast, mask, losOwner, ref best, ref hit, ref found);
            return found;
        }

        /// <summary>
        /// Per-bone DMHitbox triggers (player + enemy rigs) are skipped by the solid raycast;
        /// merge the same query used by <see cref="RangedFireSolver"/>.
        /// </summary>
        private static bool MergeHitboxRay(
            Vector3 start,
            Vector3 direction,
            float maxDistance,
            int mask,
            GameObject owner,
            ref float bestDistance,
            ref RaycastHit bestHit,
            ref bool found)
        {
            if (!DMEnemyHitQuery.MaskWantsHitboxes(mask))
                return found;

            if (!DMEnemyHitQuery.RaycastHitboxes(start, direction, maxDistance, owner, out RaycastHit boxHit))
                return found;

            if (found && boxHit.distance >= bestDistance)
                return found;

            bestDistance = boxHit.distance;
            bestHit = boxHit;
            found = true;
            return true;
        }

        private static bool ShouldIgnoreCollider(Collider collider, Transform ignoreRoot, Transform shotRoot)
        {
            if (collider == null)
                return true;

            Transform t = collider.transform;
            if (ignoreRoot != null && (t == ignoreRoot || t.IsChildOf(ignoreRoot)))
                return true;
            if (shotRoot != null && (t == shotRoot || t.IsChildOf(shotRoot)))
                return true;
            if (DMEnemyHitQuery.IsSupersededByHitboxRig(collider))
                return true;

            return false;
        }

        private static bool IsExpectedHit(Collider hitCollider, Transform expectedRoot, Collider expected)
        {
            if (hitCollider == null)
                return false;
            if (expected != null && (hitCollider == expected || hitCollider.transform.IsChildOf(expected.transform)))
                return true;
            if (expected != null
                && hitCollider.gameObject.layer == DMEnemyHitQuery.HitboxLayer
                && expected.transform.root == hitCollider.transform.root)
                return true;
            if (expectedRoot == null)
                return false;
            return hitCollider.transform == expectedRoot || hitCollider.transform.IsChildOf(expectedRoot);
        }

        private static void RefreshMasks()
        {
            int frame = Time.frameCount;
            if (frame == cachedFrame && cachedOccluderMask != 0)
                return;

            cachedFrame = frame;
            int ignore = 1 << DMBuildingLayers.IgnoreRaycastLayer;
            int triggers = LayerBit("Triggers");
            int occluder = DMBuildingLayers.OverlapMask
                | DMBuildingLayers.GroundMask
                | DMBuildingLayers.AimMask
                | 1
                | LayerBit("Climbable")
                | LayerBit("Building")
                | LayerBit("Resource")
                | LayerBit("Pushable")
                | LayerBit("StopMove")
                | LayerBit("BlockAIRayCast")
                | LayerBit("PW_Object_Small")
                | LayerBit("PW_Object_Medium")
                | LayerBit("PW_Object_Large")
                | LayerBit("Terrain");
            cachedOccluderMask = occluder & ~ignore & ~triggers & ~(1 << PlayerLayer);

            int actors = (1 << PlayerLayer)
                | LayerBit("Enemy")
                | LayerBit("CompanionAI")
                | LayerBit("BodyPart")
                | DMEnemyHitQuery.HitboxMask;
            cachedShotMask = (cachedOccluderMask | actors) & ~ignore & ~triggers;
        }

        private static int LayerBit(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? 1 << layer : 0;
        }
    }
}
