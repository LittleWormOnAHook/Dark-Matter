namespace Project.Progression
{
    /// <summary>
    /// Journal Tier-0 anchors (Climbing / Jetpack / Dash) gate runtime movement systems.
    /// <see cref="Project.Player.DMPlayerSystemsProfile"/> toggles still apply on top.
    /// </summary>
    public static class DMSkillMovementSystemGates
    {
        public static bool IsClimbUnlockedBySkills() =>
            PlayerSkillAllocator.GetSkillRank(SkillDefinition.ClimbSystemSkillId) > 0;

        public static bool IsJetpackUnlockedBySkills() =>
            PlayerSkillAllocator.GetSkillRank(SkillDefinition.JetpackSystemSkillId) > 0;

        public static bool IsDashUnlockedBySkills() =>
            PlayerSkillAllocator.GetSkillRank(SkillDefinition.DashSystemSkillId) > 0;
    }
}
