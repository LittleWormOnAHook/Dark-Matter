namespace Project.Progression
{
    public interface ILevelGatedUpgrade
    {
        int RequiredPlayerLevel { get; }

        bool CanUpgrade(PlayerProgressionManager progression) =>
            LevelUnlockUtility.CanAccess(progression, RequiredPlayerLevel);
    }
}
