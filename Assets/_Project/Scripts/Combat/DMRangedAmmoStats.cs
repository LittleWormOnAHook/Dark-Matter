using Project.Data;
using Project.Prototypes.Weapons;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Resolves live ranged stats from the drawn weapon plus the loaded ammo profile.
    /// Ammo wins when the field is authored (greater than zero); otherwise the weapon is used.
    /// </summary>
    public static class DMRangedAmmoStats
    {
        public const float DefaultFireRate = 4f;
        public const float DefaultBurstFireRate = 10f;
        public const float DefaultReloadSeconds = 1.8f;
        public const float DefaultRange = 45f;
        public const float DefaultProjectileSpeed = 85f;

        public static float ResolveFireRate(ItemData weapon, ItemData ammo)
        {
            if (Project.Combat.DMRanged.DMRangedMorphRuntime.TryGetActiveForm(weapon, out var dmf)) return Project.Combat.DMRanged.DM_RangedMorphSettings.Instance.FireRate(dmf); // 20261028a DM Ranged form wins
            if (ammo != null && ammo.fireRate > 0.01f)
                return ammo.fireRate;
            if (weapon != null && weapon.fireRate > 0.01f)
                return weapon.fireRate;
            return DefaultFireRate;
        }

        public static float ResolveBurstFireRate(ItemData weapon, ItemData ammo)
        {
            if (ammo != null && ammo.burstFireRate > 0.01f)
                return ammo.burstFireRate;
            if (weapon != null && weapon.burstFireRate > 0.01f)
                return weapon.burstFireRate;
            return Mathf.Max(DefaultBurstFireRate, ResolveFireRate(weapon, ammo));
        }

        public static int ResolveBurstCount(ItemData weapon, ItemData ammo)
        {
            if (DMScopeMode.IsScoped)
                return 1;

            int count = 1;
            if (ammo != null && ammo.shotsPerBurst > 1)
                count = ammo.shotsPerBurst;
            else if (weapon != null && weapon.shotsPerBurst > 1)
                count = weapon.shotsPerBurst;

            return Mathf.Clamp(count, 1, 5);
        }

        /// <summary>Seconds between Invector trigger cycles (one shot or one full burst).</summary>
        public static float ResolveShootInterval(ItemData weapon, ItemData ammo)
        {
            if (DMScopeMode.IsScoped)
            {
                bool rifle = weapon != null && DMScopeMode.IsRifle(weapon);
                return DMScopeMode.ScopedFireInterval(rifle, ammo);
            }

            float fireRate = ResolveFireRate(weapon, ammo);
            float cycle = fireRate > 0.01f ? 1f / fireRate : 1f / DefaultFireRate;
            int burst = ResolveBurstCount(weapon, ammo);
            if (burst <= 1)
                return cycle;

            float intra = 1f / ResolveBurstFireRate(weapon, ammo);
            float burstDuration = (burst - 1) * intra;
            return Mathf.Max(cycle, burstDuration + 0.01f);
        }

        /// <summary>
        /// Base seconds per scoped shot before per-ammo percent tweak, scope skills, and min interval clamp
        /// (see <see cref="ResolveScopedFireIntervalBeforeSkills"/> and <see cref="DMScopeMode.ScopedFireInterval"/>).
        /// DM_ScopeProfile rifle/pistol interval wins when &gt; 0; loaded ammo interval is fallback only (does not replace profile).
        /// </summary>
        public static float ResolveScopedFireIntervalBase(ItemData ammo, bool rifle)
        {
            DM_ScopeProfile p = DM_ScopeProfile.Live;
            float fromProfile = rifle ? p.rifleScopedFireInterval : p.pistolScopedFireInterval;
            if (fromProfile > 0.01f)
                return fromProfile;

            if (ammo != null)
            {
                float authored = rifle ? ammo.rifleScopedFireInterval : ammo.pistolScopedFireInterval;
                if (authored > 0.01f)
                    return authored;
            }

            return fromProfile;
        }

        /// <summary>
        /// ±% scoped cadence from loaded ammo. Positive = faster (shorter interval). Clamped to ±10.
        /// </summary>
        public static float ResolveScopedFireIntervalPercentModifier(ItemData ammo, bool rifle)
        {
            if (ammo == null)
                return 0f;

            float mod = rifle
                ? ammo.rifleScopedFireIntervalPercentModifier
                : ammo.pistolScopedFireIntervalPercentModifier;
            return Mathf.Clamp(mod, -10f, 10f);
        }

        /// <summary>
        /// Seconds per scoped shot after profile/ammo base and per-ammo ±% modifier, before Rifles scope skills and min interval.
        /// Order: <see cref="ResolveScopedFireIntervalBase"/> → ammo percent (<c>interval × (1 − modifier/100)</c>) → skills in <see cref="DMScopeMode.ScopedFireInterval"/>.
        /// </summary>
        public static float ResolveScopedFireIntervalBeforeSkills(ItemData ammo, bool rifle)
        {
            float baseI = ResolveScopedFireIntervalBase(ammo, rifle);
            if (baseI <= 0.01f)
                return baseI;

            float pct = ResolveScopedFireIntervalPercentModifier(ammo, rifle);
            return baseI * (1f - pct * 0.01f);
        }

        public static int ResolveMagazineSize(ItemData weapon, ItemData ammo)
        {
            if (Project.Combat.DMRanged.DMRangedMorphRuntime.TryGetActiveForm(weapon, out var dmf)) return Project.Combat.DMRanged.DM_RangedMorphSettings.Instance.Magazine(dmf, ammo); // 28j: Rifle magazine by ammo
            if (ammo != null && ammo.magazineSize > 0)
                return ammo.magazineSize;
            if (weapon != null && weapon.magazineSize > 0)
                return weapon.magazineSize;
            return 1;
        }

        public static float ResolveReloadSeconds(ItemData weapon, ItemData ammo)
        {
            if (Project.Combat.DMRanged.DMRangedMorphRuntime.TryGetActiveForm(weapon, out var dmf)) return Project.Combat.DMRanged.DM_RangedMorphSettings.Instance.Reload(dmf);
            if (ammo != null && ammo.reloadTimeSeconds > 0.01f)
                return ammo.reloadTimeSeconds;
            if (weapon != null && weapon.reloadTimeSeconds > 0.01f)
                return weapon.reloadTimeSeconds;
            return DefaultReloadSeconds;
        }

        public static float ResolveRange(ItemData weapon, ItemData ammo)
        {
            if (Project.Combat.DMRanged.DMRangedMorphRuntime.TryGetActiveForm(weapon, out var dmf)) return Project.Combat.DMRanged.DM_RangedMorphSettings.Instance.Range(dmf);
            if (ammo != null && ammo.rangedRange > 0.01f)
                return ammo.rangedRange;
            if (weapon != null
                && DMWeaponMorphRuntime.TryApplyRangedOverrides(weapon, out _, out _, out float morphRange, out _, out _, out _, out _, out _, out _)
                && morphRange > 0.01f)
                return morphRange;
            if (weapon != null && weapon.rangedRange > 0.01f)
                return weapon.rangedRange;
            return DefaultRange;
        }

        public static float ResolveProjectileSpeed(ItemData weapon, ItemData ammo)
        {
            if (ammo != null && ammo.projectileSpeed > 0.01f)
                return ammo.projectileSpeed;
            if (weapon != null && weapon.projectileSpeed > 0.01f)
                return weapon.projectileSpeed;
            return DefaultProjectileSpeed;
        }

        public static float ResolveShotDamage(ItemData weapon, ItemData ammo) => ResolveShotDamageCore(weapon, ammo) * DMRangedDamageMul(weapon);

        static float DMRangedDamageMul(ItemData weapon) => Project.Combat.DMRanged.DMRangedMorphRuntime.TryGetActiveForm(weapon, out var dmf) ? Project.Combat.DMRanged.DM_RangedMorphSettings.Instance.DamageMul(dmf) : 1f;
        static float DMRangedRecoilMul(ItemData weapon) => Project.Combat.DMRanged.DMRangedMorphRuntime.TryGetActiveForm(weapon, out var dmf) ? Project.Combat.DMRanged.DM_RangedMorphSettings.Instance.RecoilMul(dmf) : 1f;

        private static float ResolveShotDamageCore(ItemData weapon, ItemData ammo)
        {
            if (ammo != null && ammo.rangedDamage > 0.01f)
                return ammo.RollRangedDamage();
            if (weapon != null
                && DMWeaponMorphRuntime.TryApplyRangedOverrides(weapon, out float dmg, out float dmgRand, out _, out _, out _, out _, out _, out _, out _))
            {
                float min = Mathf.Max(1f, dmg);
                return dmgRand <= 0f ? min : Random.Range(min, Mathf.Max(min, dmg + dmgRand));
            }
            if (weapon != null)
                return weapon.RollRangedDamage();
            return 1f;
        }

        public static float ResolveAverageDamage(ItemData weapon, ItemData ammo) => ResolveAverageDamageCore(weapon, ammo) * DMRangedDamageMul(weapon);

        private static float ResolveAverageDamageCore(ItemData weapon, ItemData ammo)
        {
            if (ammo != null && ammo.rangedDamage > 0.01f)
                return ammo.GetAverageRangedDamage();
            if (weapon != null)
                return weapon.GetAverageRangedDamage();
            return 1f;
        }

        public static float ResolveMinDamage(ItemData weapon, ItemData ammo) => ResolveMinDamageCore(weapon, ammo) * DMRangedDamageMul(weapon);

        private static float ResolveMinDamageCore(ItemData weapon, ItemData ammo)
        {
            ItemData source = ammo != null && ammo.rangedDamage > 0.01f ? ammo : weapon;
            if (source == null)
                return 1f;
            return Mathf.Max(1f, source.rangedDamage);
        }

        public const float DefaultPistolRecoilVertical = 2.75f;
        public const float DefaultPistolRecoilHorizontal = 0.8f;
        public const float DefaultRifleRecoilVertical = 0.65f;
        public const float DefaultRifleRecoilHorizontal = 0.2f;
        public const float DefaultRecoilFireRateScale = 4.5f;

        /// <summary>
        /// Camera kick magnitudes. Rifle column on ammoRecoilProfile wins for two-hand weapons;
        /// otherwise ammo recoilVertical/Horizontal (Studio sliders), then nested profile,
        /// then the weapon, then grip defaults.
        /// </summary>
        public static void ResolveRecoilKick(ItemData weapon, ItemData ammo, bool isRifle, out float vertical, out float horizontal)
        {
            ResolveRecoilKickCore(weapon, ammo, isRifle, out vertical, out horizontal);
            float m = DMRangedRecoilMul(weapon);
            vertical *= m;
            horizontal *= m;
        }

        private static void ResolveRecoilKickCore(ItemData weapon, ItemData ammo, bool isRifle, out float vertical, out float horizontal)
        {
            if (TryResolveRifleProfileKick(ammo, isRifle, out vertical, out horizontal))
                return;

            if (TryResolveSimpleKick(ammo, isRifle, out vertical, out horizontal))
                return;

            if (TryResolveProfileKick(ammo, isRifle, out vertical, out horizontal))
                return;

            if (TryResolveSimpleKick(weapon, isRifle, out vertical, out horizontal))
                return;

            vertical = isRifle ? DefaultRifleRecoilVertical : DefaultPistolRecoilVertical;
            horizontal = isRifle ? DefaultRifleRecoilHorizontal : DefaultPistolRecoilHorizontal;
        }

        public static float ResolveRecoilFireRateScale(ItemData weapon, ItemData ammo)
        {
            if (ammo != null && ammo.recoilFireRateScale > 0.01f)
                return ammo.recoilFireRateScale;
            if (weapon != null && weapon.recoilFireRateScale > 0.01f)
                return weapon.recoilFireRateScale;
            return DefaultRecoilFireRateScale;
        }

        private static bool TryResolveRifleProfileKick(ItemData ammo, bool isRifle, out float vertical, out float horizontal)
        {
            vertical = 0f;
            horizontal = 0f;
            if (!isRifle || ammo == null)
                return false;

            float authoredVertical = ammo.ammoRecoilProfile.rifleCameraVertical;
            float authoredHorizontal = ammo.ammoRecoilProfile.rifleCameraHorizontal;
            if (authoredVertical <= 0.001f && authoredHorizontal <= 0.001f)
                return false;

            vertical = authoredVertical > 0.001f ? authoredVertical : DefaultRifleRecoilVertical;
            horizontal = authoredHorizontal > 0.001f ? authoredHorizontal : DefaultRifleRecoilHorizontal;
            return true;
        }

        private static bool TryResolveSimpleKick(ItemData item, bool isRifle, out float vertical, out float horizontal)
        {
            vertical = 0f;
            horizontal = 0f;
            if (item == null || (item.recoilVertical <= 0.01f && item.recoilHorizontal <= 0.01f))
                return false;

            float fallbackVertical = isRifle ? DefaultRifleRecoilVertical : DefaultPistolRecoilVertical;
            float fallbackHorizontal = isRifle ? DefaultRifleRecoilHorizontal : DefaultPistolRecoilHorizontal;
            vertical = item.recoilVertical > 0.01f ? item.recoilVertical : fallbackVertical;
            horizontal = item.recoilHorizontal > 0.01f ? item.recoilHorizontal : fallbackHorizontal;
            return true;
        }

        private static bool TryResolveProfileKick(ItemData ammo, bool isRifle, out float vertical, out float horizontal)
        {
            vertical = 0f;
            horizontal = 0f;
            if (ammo == null || !ammo.ammoRecoilProfile.HasAuthoredValues)
                return false;

            ammo.ammoRecoilProfile.GetCameraKick(isRifle, out vertical, out horizontal);
            return vertical > 0.001f || horizontal > 0.001f;
        }
    }
}
