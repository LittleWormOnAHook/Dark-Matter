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

        public static void ApplyPlayerShotRecoil(vShooterManager shooterManager, ItemData weaponItem)
        {
            if (shooterManager == null)
                return;

            vThirdPersonCamera camera = shooterManager.tpCamera;
            if (camera == null)
                return;

            bool isRifle = weaponItem != null && weaponItem.weaponGrip == WeaponGrip.TwoHanded;
            float fireRateScale = 1f;
            if (weaponItem != null && weaponItem.fireRate > 4.5f)
                fireRateScale = Mathf.Clamp(4.5f / weaponItem.fireRate, 0.65f, 1f);

            // verticalKick ~= equivalent mouse pixels before CameraInputScale (see CameraInputScale).
            // Rifle is stocked / two-handed — mild climb, almost no lateral drift.
            float verticalKick = isRifle
                ? Random.Range(0.45f, 0.85f)
                : Random.Range(2f, 3.5f);
            verticalKick *= fireRateScale;

            float horizontalKick = isRifle
                ? Random.Range(-0.2f, 0.2f)
                : Random.Range(-0.8f, 0.8f);
            horizontalKick *= fireRateScale;

            // RotateCamera: mouseY -= y * sensitivity. CameraInput matches PioneerShooterMeleeInput.
            camera.RotateCamera(horizontalKick * CameraInputScale, -verticalKick * CameraInputScale);
        }

        public static void ApplyWeaponRecoilTuning(vShooterWeapon weapon, ItemData weaponItem)
        {
            if (weapon == null)
                return;

            ZeroWeaponRecoil(weapon);
            ApplyWeaponAnimationRecoilTuning(weapon, weaponItem);
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
