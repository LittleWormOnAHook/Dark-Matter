using Invector.vCamera;
using Invector.vShooter;
using Project.Core;
using Project.Data;
using Project.Progression;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Resolves the final projectile direction and effective cone spread for a ranged shot.
    /// The HUD reticle and every muzzle→reticle shot share the same look-at aim point so
    /// over-shoulder zoom cannot slide the crosshair off the fire line.
    /// Hip fire adds cone spread only — it does not pull the shot back toward the barrel.
    /// </summary>
    public static class RangedFireSolver
    {
        public const float DefaultHipMaxDeviationDegrees = 15f;
        public const float DefaultLookAtConvergeDistance = 32f;
        /// <summary>Look-down starts applying the ground buffer (legacy viewport only).</summary>
        public const float LookDownStart = 0.08f;
        /// <summary>Steep look-down (almost at feet) uses the tightest ground band.</summary>
        public const float LookDownSteep = 0.78f;
        /// <summary>Max planar aim when just starting to look down (meters from pivot).</summary>
        public const float LookDownMaxHorizFar = 14f;
        /// <summary>Max planar aim when looking steeply down.</summary>
        public const float LookDownMaxHorizNear = 5.5f;
        /// <summary>Keep the reticle off the toes when looking down.</summary>
        public const float LookDownMinHoriz = 1f;
        /// <summary>Camera-center aim ray starts past the near clip to skip lip/parapet hits.</summary>
        public const float CameraAimRayStartSkin = 0.35f;
        /// <summary>Ignore lip hits only when closer than this (not applied to aim targets in front).</summary>
        public const float MinCameraAimHitDistance = 0.35f;
        private const float StandingColliderCacheSeconds = 0.12f;
        /// <summary>Only skip standing-surface hits under the feet — not the whole terrain collider.</summary>
        private const float StandingFootprintRadius = 1.05f;
        private const float StandingFootVerticalBand = 0.5f;
        private static Collider _cachedStandingCollider;
        private static float _cachedStandingColliderTime;
        private const int PlayerLayer = 8;

        /// <summary>ADS multiplies effective spread (matches RangedCombatHud crosshair shrink).</summary>
        public const float AdsSpreadScale = 0.75f;

        /// <summary>
        /// At accuracy 100, residual spread fraction after the accuracy curve.
        /// Formula: spread *= Lerp(1, MinSpreadFractionAtFullAccuracy, accuracy01).
        /// </summary>
        public const float MinSpreadFractionAtFullAccuracy = 0.15f;

        /// <summary>Exponential smooth on weapon aim line (shared by HUD + fire).</summary>
        private const float WeaponAimSmoothLambda = 20f;
        /// <summary>Skip weapon mesh colliders when raycasting from the barrel tip.</summary>
        public const float MuzzleRayStartSkin = 0.12f;
        /// <summary>Spawn traveling projectiles slightly past the muzzle to avoid barrel overlap.</summary>
        public const float ProjectileSpawnSkin = 0.08f;

        private static EntityId _weaponAimMuzzleId = EntityId.None;
        private static Vector3 _weaponAimOrigin;
        private static Vector3 _weaponAimForward;
        private static readonly RaycastHit[] AimRaycastBuffer = new RaycastHit[24];

        public static Vector3 ResolveDirection(
            Vector3 cameraAimDirection,
            Vector3 muzzleForward,
            bool isAiming,
            float hipMaxDeviationDegrees)
        {
            if (cameraAimDirection.sqrMagnitude < 0.0001f)
                return muzzleForward.sqrMagnitude > 0.0001f ? muzzleForward.normalized : Vector3.forward;

            // Always follow the HUD reticle. Hip fire uses extra spread, not a barrel clamp —
            // a 15° pull toward the muzzle walks shots off the crosshair when zoomed out.
            return cameraAimDirection.normalized;
        }

        /// <summary>
        /// Viewport of the shared look-at aim point (Unity bottom-left origin).
        /// Zoomed-in sits near screen center; zoomed-out stays on the player look line
        /// instead of drifting right with the over-shoulder camera.
        /// </summary>
        public static Vector2 ResolveReticleViewport(Camera cam, float maxRange = DefaultLookAtConvergeDistance, Transform weaponMuzzle = null)
        {
            if (cam == null)
                return new Vector2(0.5f, 0.5f);

            Vector3 aimPoint = ResolveReticleAimPoint(cam, maxRange, null, weaponMuzzle);
            Vector3 vp = cam.WorldToViewportPoint(aimPoint);
            if (vp.z <= 0.05f)
                return new Vector2(0.5f, 0.5f);

            return new Vector2(
                Mathf.Clamp(vp.x, 0.12f, 0.88f),
                Mathf.Clamp(vp.y, 0.12f, 0.88f));
        }

        /// <summary>
        /// World aim point for HUD crosshair and muzzle→reticle shots.
        /// Uses the gameplay camera viewport center (0.5, 0.5) so impacts match a fixed center reticle.
        /// </summary>
        public static Vector3 ResolveReticleAimPoint(
            Camera cam,
            float maxRange,
            LayerMask? optionalMask = null,
            Transform weaponMuzzle = null)
        {
            if (cam == null)
                return Vector3.zero;

            float range = Mathf.Max(1f, maxRange);
            int mask = optionalMask ?? Physics.DefaultRaycastLayers;
            mask &= ~(1 << PlayerLayer);
            GameObject owner = PlayerLocator.FindPlayerObject();

            Ray centerRay = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 dir = centerRay.direction;
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.forward;
            else
                dir.Normalize();

            // Center reticle is fixed — fire follows the camera center ray.
            float downAmount = Mathf.Max(0f, -dir.y);
            float minHit = Mathf.Lerp(MinCameraAimHitDistance, 0.12f, Mathf.InverseLerp(0.05f, 0.65f, downAmount));
            if (TryRaycastAim(
                    centerRay.origin,
                    dir,
                    range,
                    mask,
                    owner,
                    out RaycastHit hit,
                    CameraAimRayStartSkin,
                    minHit))
                return hit.point;

            return centerRay.origin + dir * (CameraAimRayStartSkin + range);
        }

        /// <summary>Raycast from the live muzzle along its forward axis.</summary>
        public static Vector3 ResolveMuzzleForwardAimPoint(
            Vector3 muzzlePosition,
            Vector3 muzzleForward,
            float maxRange,
            LayerMask? optionalMask = null)
        {
            float range = Mathf.Max(1f, maxRange);
            int mask = optionalMask ?? Physics.DefaultRaycastLayers;
            mask &= ~(1 << PlayerLayer);

            Vector3 forward = muzzleForward.sqrMagnitude > 0.0001f ? muzzleForward.normalized : Vector3.forward;
            GameObject owner = PlayerLocator.FindPlayerObject();
            if (TryRaycastAim(muzzlePosition, forward, range, mask, owner, out RaycastHit hit, MuzzleRayStartSkin))
                return hit.point;

            return muzzlePosition + forward * MuzzleRayStartSkin + forward * range;
        }

        /// <summary>
        /// Barrel forward (+Z on authored Pioneer muzzles). Invector aimReference is only used when
        /// this transform is the weapon's native muzzle — not a separate drawn-visual muzzle.
        /// </summary>
        public static Vector3 ResolveWeaponAimForward(Transform muzzle)
        {
            if (muzzle == null)
                return Vector3.forward;

            vShooterWeapon shooter = muzzle.GetComponentInParent<vShooterWeapon>();
            if (shooter != null &&
                shooter.muzzle == muzzle &&
                shooter.aimReference != null)
            {
                Vector3 toRef = shooter.aimReference.position - muzzle.position;
                if (toRef.sqrMagnitude > 0.04f)
                    return toRef.normalized;
            }

            return muzzle.forward.sqrMagnitude > 0.0001f ? muzzle.forward.normalized : Vector3.forward;
        }

        public static void ResetWeaponAimSmoothing()
        {
            _weaponAimMuzzleId = EntityId.None;
        }

        /// <summary>
        /// Raycast that skips owner hierarchy colliders (player body + drawn weapon meshes).
        /// </summary>
        public static bool TryRaycastAim(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            int mask,
            GameObject owner,
            out RaycastHit bestHit,
            float startSkin = 0f,
            float minHitDistance = 0f)
        {
            bestHit = default;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector3.forward;
            else
                direction.Normalize();

            Vector3 castOrigin = origin + direction * startSkin;
            maxDistance = Mathf.Max(0.01f, maxDistance - startSkin);

            int count = Physics.RaycastNonAlloc(
                castOrigin,
                direction,
                AimRaycastBuffer,
                maxDistance,
                mask,
                QueryTriggerInteraction.Ignore);

            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = AimRaycastBuffer[i];
                if (hit.collider == null)
                    continue;
                if (owner != null && CombatHitResolver.IsOwnerCollider(owner, hit.collider))
                    continue;
                if (ShouldSkipAimHit(hit, castOrigin, direction, owner, minHitDistance))
                    continue;

                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestHit = hit;
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// Skip hits on the surface under the player's feet only. Terrain is one collider — never
        /// reject the whole sheet or shots aimed at ground in front of the reticle will miss.
        /// </summary>
        private static bool ShouldSkipAimHit(
            RaycastHit hit,
            Vector3 rayOrigin,
            Vector3 rayDirection,
            GameObject owner,
            float minHitDistance)
        {
            if (owner != null)
            {
                Collider standing = ResolveStandingCollider(owner);
                if (standing != null
                    && hit.collider == standing
                    && IsUnderFootprintHit(hit, owner.transform.position))
                {
                    return true;
                }
            }

            // Overhead lip directly above the camera when looking up.
            if (hit.distance < minHitDistance && hit.normal.y < -0.55f)
                return true;

            return false;
        }

        private static bool IsUnderFootprintHit(RaycastHit hit, Vector3 playerPosition)
        {
            if (hit.normal.y < 0.35f)
                return false;

            Vector3 offset = hit.point - playerPosition;
            Vector3 flat = new Vector3(offset.x, 0f, offset.z);
            if (flat.sqrMagnitude > StandingFootprintRadius * StandingFootprintRadius)
                return false;

            return hit.point.y <= playerPosition.y + StandingFootVerticalBand
                && hit.point.y >= playerPosition.y - 0.4f;
        }

        private static Collider ResolveStandingCollider(GameObject owner)
        {
            if (owner == null)
                return null;

            if (Time.time - _cachedStandingColliderTime < StandingColliderCacheSeconds
                && _cachedStandingCollider != null)
            {
                return _cachedStandingCollider;
            }

            _cachedStandingCollider = null;
            Vector3 feet = owner.transform.position + Vector3.up * 0.2f;
            if (Physics.Raycast(
                    feet,
                    Vector3.down,
                    out RaycastHit groundHit,
                    2.75f,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore)
                && groundHit.collider != null
                && !CombatHitResolver.IsOwnerCollider(owner, groundHit.collider))
            {
                _cachedStandingCollider = groundHit.collider;
            }

            _cachedStandingColliderTime = Time.time;
            return _cachedStandingCollider;
        }

        /// <summary>
        /// Smoothed weapon aim point on the barrel line — dampens IK jitter for HUD and shots.
        /// </summary>
        public static Vector3 ResolveStableWeaponAimPoint(
            Transform muzzle,
            float maxRange,
            int mask)
        {
            if (muzzle == null)
                return Vector3.zero;

            float range = Mathf.Max(1f, maxRange);
            Vector3 rawOrigin = muzzle.position;
            Vector3 rawForward = ResolveWeaponAimForward(muzzle);
            EntityId id = muzzle.GetEntityId();
            float dt = Time.deltaTime;
            if (dt <= 0f)
                dt = 0.02f;

            float blend = 1f - Mathf.Exp(-WeaponAimSmoothLambda * dt);
            if (id != _weaponAimMuzzleId)
            {
                _weaponAimMuzzleId = id;
                _weaponAimOrigin = rawOrigin;
                _weaponAimForward = rawForward;
            }
            else
            {
                _weaponAimOrigin = Vector3.Lerp(_weaponAimOrigin, rawOrigin, blend);
                _weaponAimForward = Vector3.Slerp(_weaponAimForward, rawForward, blend).normalized;
            }

            GameObject owner = PlayerLocator.FindPlayerObject();
            if (TryRaycastAim(_weaponAimOrigin, _weaponAimForward, range, mask, owner, out RaycastHit hit, MuzzleRayStartSkin))
                return hit.point;

            return _weaponAimOrigin + _weaponAimForward * (MuzzleRayStartSkin + range);
        }

        private static Vector3 ResolveLookDownAimPoint(Vector3 rayOrigin, Vector3 camForward, float range, int mask)
        {
            Vector3 flat = Vector3.ProjectOnPlane(camForward, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f)
                flat = Vector3.forward;
            else
                flat.Normalize();

            float down01 = Mathf.InverseLerp(LookDownStart, LookDownSteep, -camForward.y);
            float maxHoriz = Mathf.Lerp(LookDownMaxHorizFar, LookDownMaxHorizNear, down01);
            float minHoriz = LookDownMinHoriz;

            // Cap downward pitch (legacy viewport helper — wider band than before).
            float pitch = Mathf.Asin(Mathf.Clamp(-camForward.y, 0f, 1f)) * Mathf.Rad2Deg;
            float maxPitch = Mathf.Lerp(62f, 52f, down01);
            float usePitch = Mathf.Min(pitch, maxPitch);
            Vector3 axis = Vector3.Cross(Vector3.up, flat);
            if (axis.sqrMagnitude < 0.0001f)
                axis = Vector3.right;
            else
                axis.Normalize();
            Vector3 dir = Quaternion.AngleAxis(usePitch, axis) * flat;

            Vector3 origin = rayOrigin + dir * 0.35f;
            Vector3 point = origin + dir * range;
            GameObject owner = PlayerLocator.FindPlayerObject();
            if (TryRaycastAim(origin, dir, range, mask, owner, out RaycastHit hit))
                point = hit.point;

            Vector3 horiz = Vector3.ProjectOnPlane(point - rayOrigin, Vector3.up);
            float horizDist = horiz.magnitude;
            if (horizDist >= minHoriz && horizDist <= maxHoriz)
                return point;

            Vector3 horizDir = horizDist > 0.05f ? horiz / horizDist : flat;
            Vector3 desired = rayOrigin + horizDir * Mathf.Clamp(horizDist, minHoriz, maxHoriz);
            if (TryRaycastAim(desired + Vector3.up * 2.5f, Vector3.down, 10f, mask, owner, out RaycastHit ground))
                return ground.point;

            return new Vector3(desired.x, point.y, desired.z);
        }

        public static Vector3 ResolveAimPivot(Camera cam)
        {
            vThirdPersonCamera tp = vThirdPersonCamera.instance;
            if (tp != null && tp.currentTarget != null)
            {
                Vector3 pivot = tp.currentTarget.position + tp.currentTarget.up * tp.offSetPlayerPivot;
                if (tp.currentState != null)
                    pivot += tp.currentTarget.up * tp.currentState.height;
                return pivot;
            }

            if (cam != null)
                return cam.transform.position + cam.transform.forward * 0.75f;

            return Vector3.zero;
        }

        /// <summary>
        /// Direction from muzzle to the reticle aim point (true 3D aim, including vertical).
        /// </summary>
        public static Vector3 ResolveMuzzleToReticleDirection(
            Camera cam,
            Vector3 muzzlePosition,
            float maxRange,
            out float aimDistance,
            LayerMask? optionalMask = null,
            Transform weaponMuzzle = null)
        {
            aimDistance = 0f;
            if (cam == null && weaponMuzzle == null)
                return Vector3.forward;

            // Always aim at the camera reticle world point — muzzle only supplies spawn origin.
            Vector3 aimPoint = ResolveReticleAimPoint(cam, maxRange, optionalMask);

            Vector3 toAim = aimPoint - muzzlePosition;
            aimDistance = toAim.magnitude;
            if (aimDistance < 0.0001f)
            {
                if (weaponMuzzle != null)
                    return ResolveWeaponAimForward(weaponMuzzle);

                Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
                return camForward.sqrMagnitude > 0.0001f ? camForward.normalized : Vector3.forward;
            }

            return toAim / aimDistance;
        }

        /// <summary>
        /// Builds the cone spread (degrees) passed to CombatProjectileSpawner after accuracy,
        /// skill, hip-fire, close-range, and ADS modifiers.
        /// </summary>
        public static float ResolveEffectiveSpreadDegrees(
            ItemData weapon,
            ItemData ammo,
            bool isAiming,
            float aimDistance,
            bool applyPlayerSkillBonus)
        {
            if (weapon == null)
                return 0f;

            float spread = ammo != null && ammo.projectileSpreadDegrees > 0f
                ? ammo.projectileSpreadDegrees
                : weapon.projectileSpreadDegrees;

            float accuracy = weapon.ResolveBaseAccuracy(ammo);
            if (applyPlayerSkillBonus)
                accuracy += PlayerSkillAllocator.GetWeaponAccuracyBonusPercent();

            accuracy = Mathf.Clamp(accuracy, 0f, 100f);
            float accuracy01 = accuracy * 0.01f;
            spread *= Mathf.Lerp(1f, MinSpreadFractionAtFullAccuracy, accuracy01);

            if (!isAiming)
            {
                float hipMul = weapon.hipFireSpreadMultiplier > 0.01f
                    ? weapon.hipFireSpreadMultiplier
                    : 1f;
                spread *= hipMul;
            }
            else
            {
                spread *= AdsSpreadScale;
            }

            float closeDist = weapon.closeRangeFullAccuracyDistance > 0.01f
                ? weapon.closeRangeFullAccuracyDistance
                : 12f;
            float closeScale = Mathf.Clamp01(weapon.closeRangeSpreadScale);
            float t = Mathf.Clamp01(aimDistance / closeDist);
            spread *= Mathf.Lerp(closeScale, 1f, t);

            return Mathf.Max(0f, spread);
        }
    }
}
