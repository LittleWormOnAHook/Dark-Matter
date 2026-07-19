namespace Project.Combat
{
    /// <summary>
    /// Elemental combat status effects applied by ammo/projectile hits. Ticks are handled by
    /// <see cref="CombatStatusEffectController"/>; per-ammo magnitude/duration/tick-interval come
    /// from the ItemData fields under "Elemental Effect".
    /// </summary>
    public enum StatusEffectType
    {
        None = 0,
        Burning = 1,
        Frozen = 2,
        Shocked = 3,
        Corroded = 4,
        Stabilized = 5
    }

    public static class StatusEffectTypeExtensions
    {
        /// <summary>
        /// Sensible default status effect per ammo type, used when an ItemData ammo asset leaves
        /// its Elemental Effect override at None. Ammo types with no elemental identity (plain
        /// kinetic rounds, laser, ion, explosive shrapnel) default to no lingering effect.
        /// </summary>
        public static StatusEffectType DefaultStatusEffectFor(this AmmoType ammoType)
        {
            switch (ammoType)
            {
                case AmmoType.Fire:
                    return StatusEffectType.Burning;
                case AmmoType.Ice:
                    return StatusEffectType.Frozen;
                case AmmoType.Electricity:
                    return StatusEffectType.Shocked;
                case AmmoType.Plasma:
                    return StatusEffectType.Corroded;
                case AmmoType.ResonanceStabilizer:
                    return StatusEffectType.Stabilized;
                default:
                    return StatusEffectType.None;
            }
        }
    }
}
