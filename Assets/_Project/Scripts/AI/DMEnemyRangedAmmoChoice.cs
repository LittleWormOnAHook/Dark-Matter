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
        Explosive = 9
    }
}
