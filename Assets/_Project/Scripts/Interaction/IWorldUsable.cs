namespace Project.Interaction
{
    /// <summary>
    /// World objects the player can activate with the Use button (E).
    /// Higher priority scores win when multiple targets are available.
    /// </summary>
    public interface IWorldUsable
    {
        float GetUsePriority(WorldUseContext context);
        bool TryUse(WorldUseContext context);
    }
}
