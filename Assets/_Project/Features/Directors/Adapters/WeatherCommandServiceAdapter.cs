using Project.Features.Directors;
using Project.UI;
using UnityEngine;

namespace Project.Features.Directors.Adapters
{
    /// <summary>Maps WeatherDirector storm phases onto EnvironmentalCrisisHudMode.</summary>
    public sealed class WeatherCommandServiceAdapter : IWeatherCommandService
    {
        public static StormPhase CurrentPhaseStatic { get; private set; } = StormPhase.Idle;

        public StormPhase CurrentPhase => CurrentPhaseStatic;

        public void SetStormPhase(StormPhase phase)
        {
            CurrentPhaseStatic = phase;

            bool crisis = phase == StormPhase.Active || phase == StormPhase.Warning;
            string bannerMessage = ResolveBanner(phase);

            ApplyCrisisHud(crisis, bannerMessage);

            Debug.Log("[Directors] WeatherCommand SetStormPhase=" + phase);
        }

        private static string ResolveBanner(StormPhase phase)
        {
            switch (phase)
            {
                case StormPhase.Warning:
                    return "SULFUR STORM WARNING — SEEK SHELTER";
                case StormPhase.Active:
                    return "SULFUR STORM — BASE OPERATIONS PAUSED";
                case StormPhase.Clearing:
                    return "STORM CLEARING — RESUME WITH CAUTION";
                default:
                    return string.Empty;
            }
        }

        private static void ApplyCrisisHud(bool crisis, string bannerMessage)
        {
            EnvironmentalCrisisHudMode mode = EnvironmentalCrisisHudMode.Instance;
            if (mode == null)
            {
                if (crisis)
                    Debug.LogWarning("[Directors] WeatherCommand: crisis requested but HUD mode missing.");
                return;
            }

            // Must match EnvironmentalCrisisHudMode.SetCrisisActive(bool, string, bool, bool)
            // Positional args only — avoids named-arg mismatch with local HUD variants.
            bool retractHud = true;
            bool showOverlay = true;
            mode.SetCrisisActive(crisis, bannerMessage, retractHud, showOverlay);
        }
    }
}
