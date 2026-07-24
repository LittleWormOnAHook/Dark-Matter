namespace Project.Survival.Weather
{
    /// <summary>
    /// Canonical Io weather events — mirrors GDD 5.0 Appendix A2b (July 2026 lock).
    /// WeatherDirector schedules phases; only SulfurStorm and ResonanceSupercell
    /// trigger full base queue pause + Command Center shelter.
    /// </summary>
    public enum IoWeatherEventKind
    {
        None = 0,

        /// <summary>Global corrosive front; full base pause.</summary>
        SulfurStorm = 1,

        /// <summary>Jupiter–Io flux arcs; regional; comms degrade.</summary>
        IonLightningStorm = 2,

        /// <summary>Volcanic ash wind; may embed dust spouts.</summary>
        AshGale = 3,

        /// <summary>Local dirt/ash mini-tornadoes; standalone or inside ash gale.</summary>
        DustSpoutCluster = 4,

        /// <summary>Lava channel overtop / new flow fingers.</summary>
        LavaFlowSurge = 5,

        /// <summary>Rhythmic regional geyser venting.</summary>
        GeyserFieldSurge = 6,

        /// <summary>Ash plume + heat dome in caldera biomes.</summary>
        CalderaEruptionColumn = 7,

        /// <summary>Seismic bursts; stagger and rockfall.</summary>
        TremorSwarm = 8,

        /// <summary>Fast radiation front from Jupiter magnetosphere.</summary>
        JovianRadiationPulse = 9,

        /// <summary>Resonance-only composite; full base pause.</summary>
        ResonanceSupercell = 10
    }
}
