namespace Project.Interaction
{
    /// <summary>
    /// World object that requires holding Use (E) for a duration instead of a single press.
    /// </summary>
    public interface IHoldWorldUsable : IWorldUsable
    {
        float HoldDurationSeconds { get; }
        string HoldPromptText { get; }
        bool CanBeginHold(WorldUseContext context);
        void BeginHold(WorldUseContext context);
        /// <summary>Returns true when the hold completes successfully this frame.</summary>
        bool TickHold(WorldUseContext context, float deltaTime, out float progress01);
        void CancelHold(WorldUseContext context);
        bool IsHoldActive { get; }
    }
}
