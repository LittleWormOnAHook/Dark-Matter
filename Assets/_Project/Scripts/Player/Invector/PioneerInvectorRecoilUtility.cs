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
            if (camera == null)
                return;

            ResolveRecoilKick(weaponItem, ammoItem, out float verticalKick, out float horizontalKick);
            if (Mathf.Abs(verticalKick) < 0.001f && Mathf.Abs(horizontalKick) < 0.001f)
                return;

            // RotateCamera: mouseY -= y * sensitivity. CameraInput matches PioneerShooterMeleeInput.
            camera.RotateCamera(horizontalKick * CameraInputScale, -verticalKick * CameraInputScale);
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

        /// <summary>
        /// Resolves vertical/horizontal kick from ItemData recoil base stats.
        /// When both recoilVertical and recoilHorizontal are ~0, falls back to grip defaults
        /// (rifle mild climb vs pistol stronger kick). Hitscan/continuous laser ammo and mining
        /// laser tools use near-zero kick so sustained beams don't jitter the camera.
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

            if (IsLowRecoilLaserAmmo(ammoItem))
            {
                // Almost zero kick for laser pistol / continuous laser cells.
                verticalKick = Random.Range(0.02f, 0.05f);
                horizontalKick = Random.Range(-0.02f, 0.02f);
                return;
            }

            bool isRifle = weaponItem != null && weaponItem.weaponGrip == WeaponGrip.TwoHanded;
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

                // Authoring stores center vertical magnitude and horizontal half-range.
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

            float fireRateThreshold = weaponItem != null && weaponItem.recoilFireRateScale > 0.01f
                ? weaponItem.recoilFireRateScale
                : 4.5f;
            float fireRateScale = 1f;
            if (weaponItem != null && weaponItem.fireRate > fireRateThreshold)
                fireRateScale = Mathf.Clamp(fireRateThreshold / weaponItem.fireRate, 0.65f, 1f);

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
            // Keep support-hand IK on during Shot Fire so the left hand stays glued to the
            // handguard while the weapon recoils (disableIkOnShot leaves the hand floating).
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
