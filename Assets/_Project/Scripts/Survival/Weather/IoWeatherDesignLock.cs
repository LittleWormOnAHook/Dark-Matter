namespace Project.Survival.Weather
{
    /// <summary>
    /// Read-only design constants for Io weather — GDD 5.0 Appendix A2b.
    /// </summary>
    public static class IoWeatherDesignLock
    {
        public static IoWeatherBaseImpact GetBaseImpact(IoWeatherEventKind kind)
        {
            switch (kind)
            {
                case IoWeatherEventKind.SulfurStorm:
                case IoWeatherEventKind.ResonanceSupercell:
                    return IoWeatherBaseImpact.FullPause;
                case IoWeatherEventKind.IonLightningStorm:
                case IoWeatherEventKind.AshGale:
                case IoWeatherEventKind.GeyserFieldSurge:
                case IoWeatherEventKind.CalderaEruptionColumn:
                case IoWeatherEventKind.JovianRadiationPulse:
                    return IoWeatherBaseImpact.Reduced;
                default:
                    return IoWeatherBaseImpact.None;
            }
        }

        public static IoWeatherScope GetDefaultScope(IoWeatherEventKind kind)
        {
            switch (kind)
            {
                case IoWeatherEventKind.SulfurStorm:
                case IoWeatherEventKind.ResonanceSupercell:
                    return IoWeatherScope.Global;
                case IoWeatherEventKind.DustSpoutCluster:
                case IoWeatherEventKind.LavaFlowSurge:
                case IoWeatherEventKind.TremorSwarm:
                    return IoWeatherScope.Local;
                default:
                    return IoWeatherScope.Regional;
            }
        }

        public static string GetDisplayName(IoWeatherEventKind kind)
        {
            switch (kind)
            {
                case IoWeatherEventKind.SulfurStorm: return "Sulfur Storm";
                case IoWeatherEventKind.IonLightningStorm: return "Ion Lightning Storm";
                case IoWeatherEventKind.AshGale: return "Ash Gale";
                case IoWeatherEventKind.DustSpoutCluster: return "Dust Spout Cluster";
                case IoWeatherEventKind.LavaFlowSurge: return "Lava Flow Surge";
                case IoWeatherEventKind.GeyserFieldSurge: return "Geyser Field Surge";
                case IoWeatherEventKind.CalderaEruptionColumn: return "Caldera Eruption Column";
                case IoWeatherEventKind.TremorSwarm: return "Tremor Swarm";
                case IoWeatherEventKind.JovianRadiationPulse: return "Jovian Radiation Pulse";
                case IoWeatherEventKind.ResonanceSupercell: return "Resonance Supercell";
                default: return "Clear";
            }
        }
    }
}
