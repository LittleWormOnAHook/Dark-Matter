using FORGE3D;
using Project.AI;
using Project.AI.Invector;
using Project.Combat;
using Project.Data;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>Resolves ItemData rows for turret game-ammo mode (combat pools / DMAmmoFxProfile).</summary>
    public static class DMTurretAmmoCatalog
    {
        public static bool TryResolveGameItems(
            DMTurretRuntime runtime,
            out ItemData weapon,
            out ItemData ammo)
        {
            weapon = null;
            ammo = null;
            if (runtime == null)
                return false;

            DMTurretProfile profile = DMTurretProfile.Live;
            DMTurretInstanceAuthority authority = runtime.InstanceAuthority;

            if (authority != null && authority.overrideGameAmmoItems)
            {
                weapon = authority.gameWeaponItem;
                ammo = authority.gameAmmoItem;
            }
            else if (profile != null)
            {
                weapon = profile.gameWeaponItem;
                ammo = profile.gameAmmoItem;
            }

            if (ammo == null && weapon != null)
                ammo = weapon.defaultAmmoItem;

            if (ammo == null)
                ammo = ResolveAmmoItem(runtime.EffectiveAmmoType);

            if (weapon == null && ammo != null)
                weapon = ResolveFallbackWeapon(ammo.ammoType);

            return weapon != null && ammo != null;
        }

        public static ItemData ResolveAmmoItem(AmmoType type)
        {
            DMEnemyRangedAmmoChoice choice = MapAmmoTypeToChoice(type);
            if (choice == DMEnemyRangedAmmoChoice.UseGripDefault)
                return null;
            return DMEnemyRangedAmmoCatalog.ResolveItem(choice);
        }

        private static ItemData ResolveFallbackWeapon(AmmoType type)
        {
            ItemData[] items = ItemRegistry.GetAllItems();
            for (int i = 0; i < items.Length; i++)
            {
                ItemData item = items[i];
                if (item == null || !item.IsRangedWeapon)
                    continue;
                if (item.AcceptsAmmoType(type))
                    return item;
            }

            return null;
        }

        public static int MixedChoiceCount =>
            System.Enum.GetValues(typeof(F3DFXType)).Length
            + System.Enum.GetValues(typeof(AmmoType)).Length;

        public static string GetMixedLabel(DMTurretRuntime runtime)
        {
            if (runtime == null)
                return string.Empty;
            if (runtime.EffectiveAmmoMode == DMTurretAmmoMode.Forge3DVfx)
                return "Vendor · " + VendorFxDisplayName(runtime.EffectiveVendorFxType);
            return "Game · " + runtime.EffectiveAmmoType.GetDisplayName();
        }

        public static void CycleMixed(DMTurretRuntime runtime, int delta)
        {
            if (runtime == null || delta == 0)
                return;
            int count = MixedChoiceCount;
            int next = (GetMixedIndex(runtime) + delta + count) % count;
            ApplyMixedIndex(runtime, next);
        }

        public static int GetMixedIndex(DMTurretRuntime runtime)
        {
            int vendorCount = System.Enum.GetValues(typeof(F3DFXType)).Length;
            if (runtime == null)
                return 0;
            if (runtime.EffectiveAmmoMode == DMTurretAmmoMode.Forge3DVfx)
                return (int)runtime.EffectiveVendorFxType;
            return vendorCount + (int)runtime.EffectiveAmmoType;
        }

        public static void ApplyMixedIndex(DMTurretRuntime runtime, int index)
        {
            if (runtime == null)
                return;
            int vendorCount = System.Enum.GetValues(typeof(F3DFXType)).Length;
            int gameCount = System.Enum.GetValues(typeof(AmmoType)).Length;
            int wrapped = ((index % MixedChoiceCount) + MixedChoiceCount) % MixedChoiceCount;
            if (wrapped < vendorCount)
            {
                F3DFXType fx = (F3DFXType)wrapped;
                runtime.SetAmmoMode(DMTurretAmmoMode.Forge3DVfx);
                runtime.SetVendorFxType(fx);
                runtime.SetAmmoType(AmmoTypeFromVendorFx(fx));
                return;
            }

            int gameIndex = wrapped - vendorCount;
            if (gameIndex < 0 || gameIndex >= gameCount)
                gameIndex = 0;
            runtime.SetAmmoMode(DMTurretAmmoMode.GameCombatAmmo);
            runtime.SetAmmoType((AmmoType)gameIndex);
        }

        public static string VendorFxDisplayName(F3DFXType type)
        {
            switch (type)
            {
                case F3DFXType.SoloGun: return "Solo Gun";
                case F3DFXType.ShotGun: return "Shotgun";
                case F3DFXType.RailGun: return "Rail Gun";
                case F3DFXType.PlasmaGun: return "Plasma Gun";
                case F3DFXType.PlasmaBeam: return "Plasma Beam";
                case F3DFXType.PlasmaBeamHeavy: return "Plasma Beam Heavy";
                case F3DFXType.LightningGun: return "Lightning Gun";
                case F3DFXType.FlameRed: return "Flame";
                case F3DFXType.LaserImpulse: return "Laser Impulse";
                default: return type.ToString();
            }
        }

        public static AmmoType AmmoTypeFromVendorFx(F3DFXType type)
        {
            switch (type)
            {
                case F3DFXType.Seeker: return AmmoType.Explosive;
                case F3DFXType.PlasmaGun:
                case F3DFXType.PlasmaBeam:
                case F3DFXType.PlasmaBeamHeavy: return AmmoType.Plasma;
                case F3DFXType.LightningGun: return AmmoType.Electricity;
                case F3DFXType.FlameRed: return AmmoType.Fire;
                case F3DFXType.Sniper:
                case F3DFXType.RailGun:
                case F3DFXType.LaserImpulse: return AmmoType.Laser;
                default: return AmmoType.Gunpowder;
            }
        }

        private static DMEnemyRangedAmmoChoice MapAmmoTypeToChoice(AmmoType type)
        {
            switch (type)
            {
                case AmmoType.Gunpowder: return DMEnemyRangedAmmoChoice.Standard;
                case AmmoType.Plasma: return DMEnemyRangedAmmoChoice.Plasma;
                case AmmoType.Ion: return DMEnemyRangedAmmoChoice.Ion;
                case AmmoType.Ice: return DMEnemyRangedAmmoChoice.Ice;
                case AmmoType.Electricity: return DMEnemyRangedAmmoChoice.Electricity;
                case AmmoType.ResonanceStabilizer: return DMEnemyRangedAmmoChoice.ResonanceStabilizer;
                case AmmoType.Laser: return DMEnemyRangedAmmoChoice.Laser;
                case AmmoType.Fire: return DMEnemyRangedAmmoChoice.Fire;
                case AmmoType.Explosive: return DMEnemyRangedAmmoChoice.Explosive;
                default: return DMEnemyRangedAmmoChoice.UseGripDefault;
            }
        }
    }
}
