namespace Project.Survival.Exposure
{
    /// <summary>
    /// Read-only exposure snapshot for HUD, journal, and future map POIs.
    /// </summary>
    public sealed class ExposureStatusSnapshot
    {
        public static readonly ExposureStatusSnapshot Empty = new ExposureStatusSnapshot();

        public float DisplayTemperatureF { get; internal set; } = ExposureTemperatureDisplay.NominalFahrenheit;
        public string ThermalStatusLabel { get; internal set; } = "EVA NOMINAL";
        public string TemperatureText { get; internal set; } = "70°F";
        public float TemperatureGaugeNormalized { get; internal set; } = ExposureTemperatureDisplay.FahrenheitToGaugeNormalized(70f);

        public ExposureHazardState DominantHazard { get; internal set; } = ExposureHazardState.Clear();
        public string[] ActiveZoneNames { get; internal set; } = System.Array.Empty<string>();
        public bool IsInShelter { get; internal set; }
        public float CombinedExposureLevel { get; internal set; }

        public ExposureModifierTick[] PlayerBuffTicks { get; internal set; } = System.Array.Empty<ExposureModifierTick>();
        public ExposureModifierTick[] PlayerDebuffTicks { get; internal set; } = System.Array.Empty<ExposureModifierTick>();

        public CompanionExposureModifierSlot[] ExpeditionCompanionSlots { get; internal set; }
            = System.Array.Empty<CompanionExposureModifierSlot>();

        public float ColdHazardLevel { get; internal set; }
        public float HeatHazardLevel { get; internal set; }
        public float RadiationHazardLevel { get; internal set; }
        public float SulfurHazardLevel { get; internal set; }
        public float VolcanoHazardLevel { get; internal set; }
        public string HazardSeverityLabel { get; internal set; } = "CLEAR";

        public string PrimaryMitigationLabel { get; internal set; } = string.Empty;
    }
}
