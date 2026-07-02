namespace Project.Pioneers
{
    public enum PioneerKind
    {
        NamedCatalog = 0,
        RescuedEcho = 1,
        ColonistWorker = 2
    }

    public enum EchoDisposition
    {
        Neutral = 0,
        Friendly = 1,
        HostileUntilSynced = 2,
        Synced = 3
    }

    public enum PioneerWorkState
    {
        Idle = 0,
        AssignedFacility = 1,
        Injured = 2,
        Sheltered = 3
    }
}
