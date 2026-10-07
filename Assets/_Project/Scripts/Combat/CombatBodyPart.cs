namespace Project.Combat
{
    /// <summary>
    /// Hit zone recorded on ranged hits by the DM hitbox rig. Damage multipliers per zone are a later
    /// phase (Hit Marks plan B4 / D4); today every zone is 1.0 and nothing reads this for damage.
    /// </summary>
    public enum CombatBodyPart
    {
        None = 0,
        Head = 1,
        Torso = 2,
        Arm = 3,
        Leg = 4
    }
}
