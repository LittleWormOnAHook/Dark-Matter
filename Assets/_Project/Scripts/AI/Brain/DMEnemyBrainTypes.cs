namespace Project.AI
{
    /// <summary>Behaviour template (Combat Plan §6). Data rows live on DM_EnemyBrainProfile; unknown rows fall back to Duelist.</summary>
    public enum DMEnemyArchetype
    {
        Duelist = 0,
        Rusher = 1,
        Berserker = 2,
        Flanker = 3,
        Ambusher = 4,
        Tank = 5,
        Guardian = 6,
        Shooter = 7,
        Kiter = 8,
        Swarmer = 9,
        Stalker = 10
    }

    /// <summary>Personality (Combat Plan §7). Adjusts utility weights, never overrides them.</summary>
    public enum DMEnemyPersonality
    {
        None = 0,
        Aggressive = 1,
        Cautious = 2,
        Cowardly = 3,
        Brave = 4,
        Curious = 5,
        Territorial = 6,
        Opportunistic = 7,
        Protective = 8,
        Reckless = 9,
        Calculating = 10,
        Vengeful = 11,
        Defensive = 12,
        Predatory = 13
    }

    /// <summary>Role handed out by the engagement director (Spacing plan §5.1). One Engager per target.</summary>
    public enum DMEngagementRole
    {
        None = 0,
        Engager = 1,
        Holder = 2
    }

    /// <summary>Condition layer (Combat Plan §12), separate from behaviour states.</summary>
    public enum DMEnemyCondition
    {
        Healthy = 0,
        Injured = 1,
        SeverelyInjured = 2,
        Critical = 3
    }

    /// <summary>Awareness (Combat Plan §9). Derived from the controller state; no icon yet.</summary>
    public enum DMEnemyAwareness
    {
        Unaware = 0,
        Suspicious = 1,
        Alert = 2,
        Combat = 3
    }

    /// <summary>Combat actions scored by the utility brain.</summary>
    public enum DMBrainAction
    {
        None = 0,
        Press = 1,
        Defend = 2,
        Retreat = 3,
        Hold = 4
    }

    /// <summary>One discrete holder idle action (Spacing plan §5.5).</summary>
    public enum DMHolderAction
    {
        None = 0,
        Sidestep = 1,
        Taunt = 2,
        Feint = 3
    }
}
