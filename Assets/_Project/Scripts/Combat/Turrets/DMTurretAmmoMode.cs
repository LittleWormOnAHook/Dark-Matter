namespace Project.Combat.Turrets
{
    /// <summary>
    /// Per-turret fire presentation: FORGE3D pooled FX, or shared combat ammo (ItemData / CombatProjectileSpawner).
    /// </summary>
    public enum DMTurretAmmoMode
    {
        Forge3DVfx = 0,
        GameCombatAmmo = 1,
    }
}
