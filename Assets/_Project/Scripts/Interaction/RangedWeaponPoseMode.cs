namespace Project.Interaction
{
    /// <summary>
    /// Explicit pose states for a ranged weapon. Drives visual grip, animation
    /// context, and fire-direction policy together so they never disagree.
    /// </summary>
    public enum RangedWeaponPoseMode
    {
        /// <summary>Weapon stowed on the back (sheathedLocal* grip).</summary>
        Holstered = 0,

        /// <summary>Weapon drawn in hand but not aiming (heldLocal* grip).</summary>
        HipReady = 1,

        /// <summary>Aiming down sights (aimHeldLocal* grip when useAimHeldGrip, otherwise hip grip).</summary>
        Aiming = 2,
    }
}
