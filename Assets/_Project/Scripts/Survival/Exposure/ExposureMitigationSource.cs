namespace Project.Survival.Exposure
{
    public enum ExposureMitigationSource
    {
        /// <summary>Any expedition companion with the required class grants partial mitigation.</summary>
        CompanionClass = 0,

        /// <summary>All three expedition slots must contain the required class.</summary>
        FullTrioClass = 1,

        /// <summary>Any companion with a passive ability or assigned skill id.</summary>
        CompanionAbility = 2,

        /// <summary>Specific trio composition (e.g. Science + Engineer present).</summary>
        TrioComposition = 3
    }
}
