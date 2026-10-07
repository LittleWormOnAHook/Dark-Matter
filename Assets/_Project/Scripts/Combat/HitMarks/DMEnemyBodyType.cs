namespace Project.Combat
{
    /// <summary>
    /// Material response of an enemy body to ranged hits (blood / coolant / sparks / burn style).
    /// Element rules on the ammo (Combat Plan §16) override this: Laser never bleeds, Ion never burns.
    /// </summary>
    public enum DMEnemyBodyType
    {
        Humanoid = 0,
        Android = 1,
        Robot = 2
    }

    /// <summary>Per-ammo override for enemy hit FX. Auto follows the Combat Plan §16 element rules.</summary>
    public enum DMEnemyFxRule
    {
        Auto = 0,
        Allow = 1,
        Deny = 2
    }

    /// <summary>Which decal look a burn mark slot uses.</summary>
    public enum DMHitMarkStyle
    {
        /// <summary>Humanoid bullet hit: bright red blood spot inside a circular char ring (no glow).</summary>
        BloodChar = 0,
        /// <summary>Android / Robot (and Laser on anything): the Laser burn look with a cooling glow.</summary>
        GlowBurn = 1
    }
}
