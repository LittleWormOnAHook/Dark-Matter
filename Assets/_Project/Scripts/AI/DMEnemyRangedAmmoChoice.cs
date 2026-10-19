using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Enemy ranged ammo selection for definitions and per-instance overrides.
    /// Standard maps to <see cref="Project.Combat.AmmoType.Gunpowder"/>.
    /// Use Grip Default is explicit legacy only (pistol Standard / rifle Plasma); when nothing is chosen, runtime falls back to Standard.
    /// </summary>
    public enum DMEnemyRangedAmmoChoice
    {
        [Tooltip("Same as Standard (Gunpowder) at runtime. Kept for older assets and overrides that mean \"follow definition\" on prefabs.")]
        UseGripDefault = 0,

        [Tooltip("Gunpowder ammo row (default when no type is chosen).")]
        Standard = 1,

        Plasma = 2,
        Ion = 3,
        Ice = 4,
        Electricity = 5,

        [InspectorName("Resonance Stabilizer")]
        ResonanceStabilizer = 6,

        Laser = 7,
        Fire = 8,
        Explosive = 9,

        // Vendor ammo (FORGE3D Sci-Fi Effects, 2026-10-19). Resolved by ammo item name in
        // DMEnemyRangedAmmoCatalog; values start at 100 so the original choices never shift.
        [InspectorName("Vendor/Laser Impulse")]
        VendorLaserImpulse = 100,
        [InspectorName("Vendor/Plasma Gun")]
        VendorPlasmaGun = 101,
        [InspectorName("Vendor/Vulcan")]
        VendorVulcan = 102,
        [InspectorName("Vendor/Solo Gun")]
        VendorSoloGun = 103,
        [InspectorName("Vendor/Seeker Bolt")]
        VendorSeekerBolt = 104,
        [InspectorName("Vendor/Shot Gun")]
        VendorShotGun = 105,
        [InspectorName("Vendor/Sniper")]
        VendorSniper = 106,
        [InspectorName("Vendor/Railgun")]
        VendorRailgun = 107,
        [InspectorName("Vendor/Lightning Gun")]
        VendorLightningGun = 108,
        [InspectorName("Vendor/Plasma Beam Blue")]
        VendorPlasmaBeamBlue = 109,
        [InspectorName("Vendor/Plasma Beam Heavy Red")]
        VendorPlasmaBeamHeavyRed = 110,
        [InspectorName("Vendor/Flame Red")]
        VendorFlameRed = 111
    }
}
