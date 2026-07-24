using Project.Features.Directors;
using Project.UI;
using UnityEngine;

namespace Project.Features.Directors.Adapters
{
    /// <summary>
    /// Maps WeatherDirector storm phases onto EnvironmentalCrisisHudMode.
    /// SYNC_MARKER: world-engine-782b-v3 — if Unity errors on line 54 SetCrisisActive, this file was NOT synced from git.
    /// </summary>
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

            // Exact signature: SetCrisisActive(bool active, string bannerMessage, bool retractHud, bool showOverlay)
            mode.SetCrisisActive(crisis, bannerMessage ?? string.Empty, true, true);
        }
    }
}
