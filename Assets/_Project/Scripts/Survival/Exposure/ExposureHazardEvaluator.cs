using System.Collections.Generic;
using Project.Survival;
using UnityEngine;

namespace Project.Survival.Exposure
{
    public static class ExposureHazardEvaluator
    {
        private const float HazardVisibilityThreshold = 0.08f;

        public static ExposureHazardState EvaluateDominant(
            SurvivalStats stats,
            IReadOnlyList<ExposureZoneVolume> activeZones)
        {
            if (stats == null)
                return ExposureHazardState.Clear();

            bool hasShelter = false;
            string shelterName = null;
            ExposureHazardState best = ExposureHazardState.Clear();
            int bestPriority = -1;
            float bestSeverity = 0f;

            if (activeZones != null)
            {
                for (int i = 0; i < activeZones.Count; i++)
                {
                    ExposureZoneVolume zone = activeZones[i];
                    if (zone?.Profile == null)
                        continue;

                    ExposureZoneKind kind = zone.Profile.zoneKind;
                    if (kind == ExposureZoneKind.ShelterSafe)
                    {
                        hasShelter = true;
                        shelterName = zone.Profile.displayName;
                        continue;
                    }

                    int priority = GetKindPriority(kind);
                    float severity = GetZoneSeverity(stats, zone.Profile);
                    if (priority > bestPriority || (priority == bestPriority && severity > bestSeverity))
                    {
                        bestPriority = priority;
                        bestSeverity = severity;
                        best = BuildHazard(kind, zone.Profile.displayName, severity);
                    }
                }
            }

            if (bestPriority >= 0 && bestSeverity >= HazardVisibilityThreshold)
                return best;

            if (hasShelter)
                return ExposureHazardState.Shelter(shelterName);

            float ambientSeverity = GetAmbientSeverity(stats);
            if (ambientSeverity >= HazardVisibilityThreshold)
            {
                ExposureZoneKind ambientKind = ResolveAmbientThermalKind(stats);
                if (ambientKind != ExposureZoneKind.Custom)
                    return BuildHazard(ambientKind, ExposureHazardPresentation.GetShortLabel(ambientKind), ambientSeverity);
            }

            return ExposureHazardState.Clear();
        }

        public static string[] CollectActiveZoneNames(IReadOnlyList<ExposureZoneVolume> activeZones)
        {
            if (activeZones == null || activeZones.Count == 0)
                return System.Array.Empty<string>();

            var names = new List<string>(activeZones.Count);
            for (int i = 0; i < activeZones.Count; i++)
            {
                ExposureZoneVolume zone = activeZones[i];
                if (zone?.Profile == null)
                    continue;

                string name = zone.Profile.displayName;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!names.Contains(name))
                    names.Add(name);
            }

            return names.ToArray();
        }

        private static ExposureHazardState BuildHazard(ExposureZoneKind kind, string displayName, float severity)
        {
            string label = string.IsNullOrWhiteSpace(displayName)
                ? ExposureHazardPresentation.GetShortLabel(kind)
                : displayName.ToUpperInvariant();

            return new ExposureHazardState
            {
                Kind = kind,
                DisplayName = label,
                Severity = Mathf.Clamp01(severity),
                DisplayColor = ExposureHazardPresentation.GetColor(kind),
                IsShelter = false,
                IsClear = false
            };
        }

        private static int GetKindPriority(ExposureZoneKind kind)
        {
            return kind switch
            {
                ExposureZoneKind.VolcanoCaldera => 70,
                ExposureZoneKind.SulfurField => 60,
                ExposureZoneKind.RadiationFlat => 50,
                ExposureZoneKind.MixedHazard => 45,
                ExposureZoneKind.ThermalHeat => 30,
                ExposureZoneKind.ThermalCold => 30,
                ExposureZoneKind.ShelterSafe => 10,
                _ => 0
            };
        }

        private static float GetZoneSeverity(SurvivalStats stats, ExposureZoneProfile profile)
        {
            if (stats == null || profile == null)
                return 0f;

            float rad = profile.radiationPerSecond > 0f ? stats.GetRadiationNormalized() : 0f;
            float sulfur = profile.sulfurPerSecond > 0f ? stats.GetSulfurNormalized() : 0f;
            float volcano = profile.volcanoPerSecond > 0f ? stats.GetVolcanoNormalized() : 0f;
            float thermal = 0f;

            if (profile.thermalColdPerSecond > 0f || profile.thermalHeatPerSecond > 0f)
                thermal = Mathf.Abs(stats.GetThermalNormalizedSigned());

            return Mathf.Max(rad, sulfur, volcano, thermal);
        }

        private static float GetAmbientSeverity(SurvivalStats stats)
        {
            return Mathf.Max(
                stats.GetRadiationNormalized(),
                stats.GetSulfurNormalized(),
                stats.GetVolcanoNormalized(),
                Mathf.Abs(stats.GetThermalNormalizedSigned()));
        }

        private static ExposureZoneKind ResolveAmbientThermalKind(SurvivalStats stats)
        {
            float rad = stats.GetRadiationNormalized();
            float sulfur = stats.GetSulfurNormalized();
            float volcano = stats.GetVolcanoNormalized();
            float thermalSigned = stats.GetThermalNormalizedSigned();

            if (volcano >= rad && volcano >= sulfur && volcano >= HazardVisibilityThreshold)
                return ExposureZoneKind.VolcanoCaldera;

            if (sulfur >= rad && sulfur >= HazardVisibilityThreshold)
                return ExposureZoneKind.SulfurField;

            if (rad >= HazardVisibilityThreshold)
                return ExposureZoneKind.RadiationFlat;

            if (Mathf.Abs(thermalSigned) >= HazardVisibilityThreshold)
                return thermalSigned < 0f ? ExposureZoneKind.ThermalCold : ExposureZoneKind.ThermalHeat;

            return ExposureZoneKind.Custom;
        }
    }
}
