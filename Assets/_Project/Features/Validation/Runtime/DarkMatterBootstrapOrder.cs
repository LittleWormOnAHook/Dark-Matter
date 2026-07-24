namespace Project.Features.Validation
{
    /// <summary>Locked CompanionSystems Features bootstrap sequence (TDB §7 / HLA §6.3).</summary>
    public static class DarkMatterBootstrapOrder
    {
        public const string GameState = "GameState";
        public const string WorldState = "WorldState";
        public const string Directors = "Directors";
        public const string Communications = "Communications";

        public static readonly string[] CompanionSystems =
        {
            GameState,
            WorldState,
            Directors,
            Communications
        };
    }
}
