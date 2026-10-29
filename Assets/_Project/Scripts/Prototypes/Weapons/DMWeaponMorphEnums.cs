using UnityEngine;

namespace Project.Prototypes.Weapons
{
    public enum DMWeaponMorphKind
    {
        Ranged = 0,
        MeleeBlade = 1
    }

    /// <summary>Morph Gun forms. Exactly three: Pistol, Rifle, Sniper.</summary>
    public enum DMWeaponMorphGunForm
    {
        Pistol = 0,
        Rifle = 1,
        Sniper = 2
    }

    /// <summary>
    /// Morph Blade types. Metal is solid; others map to project combat elements
    /// (CombatElement / AmmoType family) plus Light as emissive energy.
    /// </summary>
    public enum DMWeaponMorphBladeType
    {
        Metal = 0,
        Light = 1,
        Plasma = 2,
        Ion = 3,
        Laser = 4,
        Cryo = 5,
        Lightning = 6 // 2026-10-28o
    }
}
