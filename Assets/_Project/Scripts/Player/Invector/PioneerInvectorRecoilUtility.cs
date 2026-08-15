using Invector;
using Invector.vCamera;
using Invector.vShooter;
using Project.Data;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Pioneer player shot recoil — suppresses Invector camera + animation kick and applies subtle custom camera nudge.
    /// </summary>
    public static class PioneerInvectorRecoilUtility
    {
        /// <summary>Must match PioneerShooterMeleeInput mouse look scale so recoil feels like look input.</summary>
        public const float CameraInputScale = 0.1f;

        /// <summary>Invector Shot layer blend-tree id — 1 is the lightest pistol-style fire clip.</summary>
        public const int MildShotAnimationId = 1;

        /// <summary>Shot layer weight while hip-firing / aiming (Invector defaults to 1 = full body flinch).</summary>
        public const float ShotLayerWeight = 0.38f;

        /// <summary>Rifle shot layer — lighter than pistol; stocked two-hand weapons shouldn't flinch hard.</summary>
        public const float RifleShotLayerWeight = 0.18f;

        /// <summary>Shot layer weight when using scope view.</summary>
        public const float ScopeShotLayerWeight = 0.28f;

        /// <summary>Rifle scope shot layer — barely visible pulse so ADS stays stable.</summary>
        public const float RifleScopeShotLayerWeight = 0.12f;

        private const float ScopeWeightScalePistol = ScopeShotLayerWeight / ShotLayerWeight;
        private const float ScopeWeightScaleRifle = RifleScopeShotLayerWeight / RifleShotLayerWeight;

        /// <summary>Max stacked visual recoil before clamp (degrees).</summary>
        private const float MaxPitchRecoilOffset = 10f;
        private const float MaxYawRecoilOffset = 5f;

        /// <summary>Underdamped spring — stiffness high, damping low enough for a slight rebound past aim.</summary>
        private const float RecoilSpringStiffness = 210f;
        private const float RecoilSpringDampingPistol = 13.5f;
        private const float RecoilSpringDampingRifle = 16f;

        /// <summary>Extra downward velocity on kick so pitch dips slightly below aim before settling.</summary>
        private const float ReboundVelocityFromKick = 0.32f;
        private const float ReboundVelocityHorizontal = 0.18f;
        private const float MaxRecoilRecoveryVelocity = 14f;
        private const float MaxSpringDeltaTime = 1f / 30f;

        private static Vector2 _recoilRecoveryVelocity;

        public static bool HasActiveRecoil(vThirdPersonCamera camera) =>
            camera != null
            && (camera.offsetMouse.sqrMagnitude > 0.00001f
                || _recoilRecoveryVelocity.sqrMagnitude > 0.00001f);

        public static void ApplyShooterManagerDefaults(vShooterManager manager)
        {
            if (manager == null)
                return;

            manager.applyRecoilToCamera = false;
            manager.cameraRecoilStability = 1f;
        }

        public static void SuppressInvectorNativeRecoil(vShooterManager manager)
        {
            if (manager == null)
                return;

            ApplyShooterManagerDefaults(manager);
            ZeroWeaponRecoil(manager.CurrentWeapon);
            ZeroWeaponRecoil(manager.rWeapon);
            ZeroWeaponRecoil(manager.lWeapon);
            ApplyWeaponAnimationRecoilTuning(manager.CurrentWeapon);
            ApplyWeaponAnimationRecoilTuning(manager.rWeapon);
            ApplyWeaponAnimationRecoilTuning(manager.lWeapon);
        }

        public static void ApplyPlayerShotRecoil(vShooterManager shooterManager, ItemData weaponItem, ItemData ammoItem = null)
        {
            if (shooterManager == null)
                return;

            vThirdPersonCamera camera = shooterManager.tpCamera;
            if (camera == null || !camera.isInit)
                return;

            ResolveRecoilKick(weaponItem, ammoItem, out float verticalKick, out float horizontalKick);
            if (Mathf.Abs(verticalKick) < 0.001f && Mathf.Abs(horizontalKick) < 0.001f)
                return;

            // Temporary kick on offsetMouse — decays in TickRecoilRecovery so aim baseline (mouseX/Y) is preserved.
            Vector2 offset = camera.offsetMouse;
            offset.y -= verticalKick;
            offset.x += horizontalKick;
            offset.y = Mathf.Clamp(offset.y, -MaxPitchRecoilOffset, MaxPitchRecoilOffset);
            offset.x = Mathf.Clamp(offset.x, -MaxYawRecoilOffset, MaxYawRecoilOffset);
            camera.offsetMouse = offset;

            // Bias spring velocity so recovery overshoots slightly (dip back below aim) before settling.
            if (weaponItem == null || !weaponItem.isMiningTool)
            {
                _recoilRecoveryVelocity.y += verticalKick * ReboundVelocityFromKick;
                _recoilRecoveryVelocity.x -= horizontalKick * ReboundVelocityHorizontal;
                ClampRecoilRecoveryVelocity();
            }

            camera.CheckCameraIsRotating();
        }

        private static void ClampRecoilRecoveryVelocity()
        {
            _recoilRecoveryVelocity.x = Mathf.Clamp(
                _recoilRecoveryVelocity.x,
                -MaxRecoilRecoveryVelocity,
                MaxRecoilRecoveryVelocity);
            _recoilRecoveryVelocity.y = Mathf.Clamp(
                _recoilRecoveryVelocity.y,
                -MaxRecoilRecoveryVelocity,
                MaxRecoilRecoveryVelocity);
        }

        /// <summary>
        /// Spring recoil offset back to zero with a slight rebound past aim baseline.
        /// Call from PioneerShooterManager each frame while playing.
        /// </summary>
        public static void TickRecoilRecovery(vThirdPersonCamera camera, ItemData weaponItem, float deltaTime)
        {
            if (camera == null || deltaTime <= 0f)
                return;

            deltaTime = Mathf.Min(deltaTime, MaxSpringDeltaTime);

            Vector2 offset = camera.offsetMouse;
            if (offset.sqrMagnitude < 0.00001f && _recoilRecoveryVelocity.sqrMagnitude < 0.00001f)
            {
                if (offset != Vector2.zero)
                    camera.offsetMouse = Vector2.zero;
                _recoilRecoveryVelocity = Vector2.zero;
                return;
            }

            bool isRifle = weaponItem != null && weaponItem.weaponGrip == WeaponGrip.TwoHanded;
            float damping = isRifle ? RecoilSpringDampingRifle : RecoilSpringDampingPistol;

            Vector2 acceleration =
                -offset * RecoilSpringStiffness
                - _recoilRecoveryVelocity * damping;
            _recoilRecoveryVelocity += acceleration * deltaTime;
            offset += _recoilRecoveryVelocity * deltaTime;

            if (Mathf.Abs(offset.y) > MaxPitchRecoilOffset)
            {
                offset.y = Mathf.Sign(offset.y) * MaxPitchRecoilOffset;
                _recoilRecoveryVelocity.y *= 0.35f;
            }

            if (Mathf.Abs(offset.x) > MaxYawRecoilOffset)
            {
                offset.x = Mathf.Sign(offset.x) * MaxYawRecoilOffset;
                _recoilRecoveryVelocity.x *= 0.35f;
            }

            ClampRecoilRecoveryVelocity();
            camera.offsetMouse = offset;
        }

        /// <summary>Clears any in-flight recoil offset (e.g. cutscene / optics handoff).</summary>
        public static void ResetRecoilOffset(vThirdPersonCamera camera)
        {
            _recoilRecoveryVelocity = Vector2.zero;
            if (camera != null)
                camera.offsetMouse = Vector2.zero;
        }

        public static bool IsLowRecoilLaserAmmo(ItemData ammoItem)
        {
            return ammoItem != null && (ammoItem.isHitscanBeam || ammoItem.isContinuousLaser);
        }

        public static bool IsLowRecoilLaserShot(ItemData weaponItem, ItemData ammoItem)
        {
            if (weaponItem != null && weaponItem.isMiningTool)
                return true;

            return IsLowRecoilLaserAmmo(ammoItem);
        }

        public static bool ShouldSkipAnimationRecoil(ItemData weaponItem, ItemData ammoItem)
        {
            return ResolveShotAnimationWeight(weaponItem, ammoItem, isScopeView: false) < 0.01f;
        }

        public static float ResolveShotAnimationWeight(ItemData weaponItem, ItemData ammoItem, bool isScopeView)
        {
            bool isRifle = weaponItem != null && weaponItem.weaponGrip == WeaponGrip.TwoHanded;

            if (ammoItem != null && ammoItem.ammoRecoilProfile.HasAuthoredValues)
            {
                float weight = ammoItem.ammoRecoilProfile.GetAnimationWeight(isRifle);
                if (isScopeView && weight > 0.001f)
                {
                    float scopeScale = isRifle ? ScopeWeightScaleRifle : ScopeWeightScalePistol;
                    weight *= scopeScale;
                }

                return weight;
            }

            if (IsLowRecoilLaserShot(weaponItem, ammoItem))
                return 0f;

            if (isScopeView)
                return isRifle ? RifleScopeShotLayerWeight : ScopeShotLayerWeight;

            return isRifle ? RifleShotLayerWeight : ShotLayerWeight;
        }

        /// <summary>
        /// Resolves vertical/horizontal kick from ammo recoil profile, then weapon ItemData, then grip defaults.
        /// </summary>
        public static void ResolveRecoilKick(ItemData weaponItem, out float verticalKick, out float horizontalKick)
        {
            ResolveRecoilKick(weaponItem, null, out verticalKick, out horizontalKick);
        }

        public static void ResolveRecoilKick(ItemData weaponItem, ItemData ammoItem, out float verticalKick, out float horizontalKick)
        {
            if (weaponItem != null && weaponItem.isMiningTool)
            {
                // Continuous mining beam fires many recoil ticks/sec — keep kick tiny to avoid jitter.
                verticalKick = Random.Range(0.003f, 0.008f);
                horizontalKick = Random.Range(-0.004f, 0.004f);
                return;
            }

            bool isRifle = weaponItem != null && weaponItem.weaponGrip == WeaponGrip.TwoHanded;

            if (ammoItem != null && ammoItem.ammoRecoilProfile.HasAuthoredValues)
            {
                ammoItem.ammoRecoilProfile.GetCameraKick(isRifle, out float vertBase, out float horizBase);
                verticalKick = Random.Range(vertBase * 0.85f, vertBase * 1.15f);
                horizontalKick = Random.Range(-horizBase, horizBase);
                ApplyFireRateScale(weaponItem, ref verticalKick, ref horizontalKick);
                return;
            }

            if (IsLowRecoilLaserAmmo(ammoItem))
            {
                verticalKick = Random.Range(0.02f, 0.05f);
                horizontalKick = Random.Range(-0.02f, 0.02f);
                return;
            }

            bool useAuthored = weaponItem != null
                && (weaponItem.recoilVertical > 0.01f || weaponItem.recoilHorizontal > 0.01f);

            if (useAuthored)
            {
                float vertBase = weaponItem.recoilVertical > 0.01f
                    ? weaponItem.recoilVertical
                    : (isRifle ? 0.65f : 2.75f);
                float horizBase = weaponItem.recoilHorizontal > 0.01f
                    ? weaponItem.recoilHorizontal
                    : (isRifle ? 0.2f : 0.8f);

                verticalKick = Random.Range(vertBase * 0.85f, vertBase * 1.15f);
                horizontalKick = Random.Range(-horizBase, horizBase);
            }
            else
            {
                verticalKick = isRifle
                    ? Random.Range(0.45f, 0.85f)
                    : Random.Range(2f, 3.5f);
                horizontalKick = isRifle
                    ? Random.Range(-0.2f, 0.2f)
                    : Random.Range(-0.8f, 0.8f);
            }

            ApplyFireRateScale(weaponItem, ref verticalKick, ref horizontalKick);
        }

        private static void ApplyFireRateScale(ItemData weaponItem, ref float verticalKick, ref float horizontalKick)
        {
            float fireRateThreshold = weaponItem != null && weaponItem.recoilFireRateScale > 0.01f
                ? weaponItem.recoilFireRateScale
                : 4.5f;
            if (weaponItem == null || weaponItem.fireRate <= fireRateThreshold)
                return;

            float fireRateScale = Mathf.Clamp(fireRateThreshold / weaponItem.fireRate, 0.65f, 1f);
            verticalKick *= fireRateScale;
            horizontalKick *= fireRateScale;
        }

        public static void ApplyWeaponRecoilTuning(vShooterWeapon weapon, ItemData weaponItem)
        {
            if (weapon == null)
                return;

            ZeroWeaponRecoil(weapon);
            ApplyWeaponAnimationRecoilTuning(weapon, weaponItem);
            ApplyReloadTiming(weapon, weaponItem);
        }

        public static void ApplyReloadTiming(vShooterWeapon weapon, ItemData weaponItem)
        {
            if (weapon == null || weaponItem == null)
                return;

            if (weaponItem.reloadTimeSeconds > 0.01f)
                weapon.reloadTime = weaponItem.reloadTimeSeconds;
        }

        public static void ApplyWeaponRecoilTuning(GameObject weaponRoot, ItemData weaponItem)
        {
            if (weaponRoot == null)
                return;

            vShooterWeapon[] weapons = weaponRoot.GetComponentsInChildren<vShooterWeapon>(true);
            for (int i = 0; i < weapons.Length; i++)
                ApplyWeaponRecoilTuning(weapons[i], weaponItem);
        }

        public static void ApplyWeaponAnimationRecoilTuning(vShooterWeapon weapon)
        {
            ApplyWeaponAnimationRecoilTuning(weapon, null);
        }

        public static void ApplyWeaponAnimationRecoilTuning(vShooterWeapon weapon, ItemData weaponItem)
        {
            if (weapon == null)
                return;

            bool isRifle = weaponItem != null && weaponItem.weaponGrip == WeaponGrip.TwoHanded;
            weapon.shotID = MildShotAnimationId;
            weapon.scopeShootAnimationWeight = isRifle ? RifleScopeShotLayerWeight : ScopeShotLayerWeight;
            weapon.disableIkOnShot = false;
            weapon.cameraStability = 1f;
        }

        public static void ZeroWeaponRecoil(vShooterWeapon weapon)
        {
            if (weapon == null)
                return;

            weapon.recoilUp = 0f;
            weapon.recoilRight = 0f;
            weapon.recoilLeft = 0f;
        }
    }
}
