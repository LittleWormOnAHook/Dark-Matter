namespace Project.Features.WorldState
{
    public sealed class ThreatSnapshot
    {
        public static readonly ThreatSnapshot Empty = new ThreatSnapshot();

        public float EnvironmentThreat01 { get; }
        public bool SulfurStormActive { get; }
        public string StormPhaseLabel { get; }
        public string DominantHazardLabel { get; }

        public ThreatSnapshot(
            float environmentThreat01 = 0f,
            bool sulfurStormActive = false,
            string stormPhaseLabel = "Idle",
            string dominantHazardLabel = "CLEAR")
        {
            EnvironmentThreat01 = environmentThreat01;
            SulfurStormActive = sulfurStormActive;
            StormPhaseLabel = stormPhaseLabel ?? string.Empty;
            DominantHazardLabel = dominantHazardLabel ?? string.Empty;
        }
    }
}
