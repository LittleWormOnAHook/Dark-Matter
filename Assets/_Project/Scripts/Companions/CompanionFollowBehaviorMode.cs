namespace Project.Companions
{
    /// <summary>
    /// High-level locomotion mode for companions. Expedition trio members use Follow;
    /// pre-recruitment world Echoes/recruits default to PingPong or Idle.
    /// </summary>
    public enum CompanionFollowBehaviorMode
    {
        Follow = 0,
        PingPong = 1,
        Idle = 2
    }
}
