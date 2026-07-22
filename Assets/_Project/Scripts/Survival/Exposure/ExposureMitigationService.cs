using System.Collections.Generic;
using Project.Companions;
using Project.Pioneers;
using UnityEngine;

namespace Project.Survival.Exposure
{
    /// <summary>
    /// Evaluates expedition trio / companion abilities against zone mitigation rules.
    /// </summary>
    public static class ExposureMitigationService
    {
        public struct MitigationResult
        {
            public float radiationMultiplier;
            public float thermalColdMultiplier;
            public float thermalHeatMultiplier;
            public float sulfurMultiplier;
            public float volcanoMultiplier;
            public float oxygenDrainMultiplier;
            public string activeLabel;
            public string[] activeLabels;
        }

        public static MitigationResult Evaluate(ExposureZoneProfile profile)
        {
            MitigationResult result = MitigationResultDefaults();
            if (profile == null || profile.mitigationRules == null || profile.mitigationRules.Length == 0)
                return result;

            CompanionRosterBridge bridge = Object.FindAnyObjectByType<CompanionRosterBridge>();
            IReadOnlyList<PioneerCompanionAgent> companions =
                bridge != null ? bridge.ActiveCompanions : null;

            var labels = new List<string>(4);

            for (int i = 0; i < profile.mitigationRules.Length; i++)
            {
                ExposureMitigationRule rule = profile.mitigationRules[i];
                if (rule == null || rule.exposureReduction <= 0f)
                    continue;

                if (!RuleMatches(rule, companions))
                    continue;

                ApplyReduction(ref result, rule);
                if (!string.IsNullOrWhiteSpace(rule.activeLabel) && !labels.Contains(rule.activeLabel))
                    labels.Add(rule.activeLabel);
            }

            result.activeLabels = labels.ToArray();
            result.activeLabel = labels.Count > 0 ? labels[0] : string.Empty;
            return result;
        }

        public static MitigationResult Combine(IReadOnlyList<ExposureZoneVolume> zones)
        {
            MitigationResult combined = MitigationResultDefaults();
            if (zones == null || zones.Count == 0)
                return combined;

            var labels = new List<string>(8);

            for (int i = 0; i < zones.Count; i++)
            {
                ExposureZoneVolume zone = zones[i];
                if (zone == null || zone.Profile == null)
                    continue;

                MitigationResult zoneResult = Evaluate(zone.Profile);
                combined.radiationMultiplier = Mathf.Min(combined.radiationMultiplier, zoneResult.radiationMultiplier);
                combined.thermalColdMultiplier = Mathf.Min(combined.thermalColdMultiplier, zoneResult.thermalColdMultiplier);
                combined.thermalHeatMultiplier = Mathf.Min(combined.thermalHeatMultiplier, zoneResult.thermalHeatMultiplier);
                combined.sulfurMultiplier = Mathf.Min(combined.sulfurMultiplier, zoneResult.sulfurMultiplier);
                combined.volcanoMultiplier = Mathf.Min(combined.volcanoMultiplier, zoneResult.volcanoMultiplier);
                combined.oxygenDrainMultiplier = Mathf.Min(combined.oxygenDrainMultiplier, zoneResult.oxygenDrainMultiplier);

                if (zoneResult.activeLabels == null)
                    continue;

                for (int labelIndex = 0; labelIndex < zoneResult.activeLabels.Length; labelIndex++)
                {
                    string label = zoneResult.activeLabels[labelIndex];
                    if (!string.IsNullOrWhiteSpace(label) && !labels.Contains(label))
                        labels.Add(label);
                }
            }

            combined.activeLabels = labels.ToArray();
            combined.activeLabel = labels.Count > 0 ? labels[0] : string.Empty;
            return combined;
        }

        private static MitigationResult MitigationResultDefaults()
        {
            return new MitigationResult
            {
                radiationMultiplier = 1f,
                thermalColdMultiplier = 1f,
                thermalHeatMultiplier = 1f,
                sulfurMultiplier = 1f,
                volcanoMultiplier = 1f,
                oxygenDrainMultiplier = 1f,
                activeLabel = string.Empty,
                activeLabels = System.Array.Empty<string>()
            };
        }

        private static void ApplyReduction(ref MitigationResult result, ExposureMitigationRule rule)
        {
            float multiplier = 1f - Mathf.Clamp01(rule.exposureReduction);
            switch (rule.pressureType)
            {
                case ExposurePressureType.Radiation:
                    result.radiationMultiplier *= multiplier;
                    break;
                case ExposurePressureType.ThermalCold:
                    result.thermalColdMultiplier *= multiplier;
                    break;
                case ExposurePressureType.ThermalHeat:
                    result.thermalHeatMultiplier *= multiplier;
                    break;
                case ExposurePressureType.Sulfur:
                    result.sulfurMultiplier *= multiplier;
                    break;
                case ExposurePressureType.Volcano:
                    result.volcanoMultiplier *= multiplier;
                    break;
                case ExposurePressureType.Oxygen:
                    result.oxygenDrainMultiplier *= multiplier;
                    break;
            }
        }

        private static bool RuleMatches(ExposureMitigationRule rule, IReadOnlyList<PioneerCompanionAgent> companions)
        {
            if (companions == null || companions.Count == 0)
                return false;

            switch (rule.source)
            {
                case ExposureMitigationSource.CompanionClass:
                    return AnyCompanionClass(companions, rule.requiredClass);

                case ExposureMitigationSource.FullTrioClass:
                    return AllCompanionsClass(companions, rule.requiredClass);

                case ExposureMitigationSource.CompanionAbility:
                    return AnyCompanionHasAbility(companions, rule.requiredAbilityId);

                case ExposureMitigationSource.TrioComposition:
                    return AnyCompanionClass(companions, rule.requiredClass)
                        && AnyCompanionClass(companions, rule.secondaryClass);

                default:
                    return false;
            }
        }

        private static bool AnyCompanionClass(IReadOnlyList<PioneerCompanionAgent> companions, SkilledPioneerClass pioneerClass)
        {
            for (int i = 0; i < companions.Count; i++)
            {
                PioneerCompanionAgent companion = companions[i];
                if (companion != null && companion.PioneerClass == pioneerClass)
                    return true;
            }

            return false;
        }

        private static bool AllCompanionsClass(IReadOnlyList<PioneerCompanionAgent> companions, SkilledPioneerClass pioneerClass)
        {
            if (companions.Count < PioneerRosterManager.ExpeditionTrioSize)
                return false;

            int matches = 0;
            for (int i = 0; i < companions.Count; i++)
            {
                PioneerCompanionAgent companion = companions[i];
                if (companion != null && companion.PioneerClass == pioneerClass)
                    matches++;
            }

            return matches >= PioneerRosterManager.ExpeditionTrioSize;
        }

        private static bool AnyCompanionHasAbility(IReadOnlyList<PioneerCompanionAgent> companions, string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
                return false;

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
                return false;

            for (int i = 0; i < companions.Count; i++)
            {
                PioneerCompanionAgent companion = companions[i];
                if (companion == null)
                    continue;

                SkilledPioneerRecord record = roster.FindSkilledById(companion.PioneerRecordId);
                if (record == null)
                    continue;

                if (RecordHasAbility(record, abilityId))
                    return true;
            }

            return false;
        }

        private static bool RecordHasAbility(SkilledPioneerRecord record, string abilityId) =>
            PioneerTraitUtility.RecordHasAbility(record, abilityId);

        /// <summary>
        /// Mitigation labels this companion personally contributes while inside the given zones.
        /// </summary>
        public static void CollectLabelsContributedByCompanion(
            IReadOnlyList<ExposureZoneVolume> zones,
            PioneerCompanionAgent companion,
            IReadOnlyList<PioneerCompanionAgent> allCompanions,
            List<string> labelsOut)
        {
            labelsOut?.Clear();
            if (labelsOut == null || zones == null || companion == null)
                return;

            for (int i = 0; i < zones.Count; i++)
            {
                ExposureZoneVolume zone = zones[i];
                ExposureZoneProfile profile = zone != null ? zone.Profile : null;
                if (profile?.mitigationRules == null)
                    continue;

                for (int ruleIndex = 0; ruleIndex < profile.mitigationRules.Length; ruleIndex++)
                {
                    ExposureMitigationRule rule = profile.mitigationRules[ruleIndex];
                    if (rule == null || rule.exposureReduction <= 0f || string.IsNullOrWhiteSpace(rule.activeLabel))
                        continue;

                    if (!RuleContributedByCompanion(rule, companion, allCompanions))
                        continue;

                    if (!labelsOut.Contains(rule.activeLabel))
                        labelsOut.Add(rule.activeLabel);
                }
            }
        }

        private static bool RuleContributedByCompanion(
            ExposureMitigationRule rule,
            PioneerCompanionAgent companion,
            IReadOnlyList<PioneerCompanionAgent> allCompanions)
        {
            switch (rule.source)
            {
                case ExposureMitigationSource.CompanionClass:
                    return companion.PioneerClass == rule.requiredClass;

                case ExposureMitigationSource.FullTrioClass:
                    return AllCompanionsClass(allCompanions, rule.requiredClass);

                case ExposureMitigationSource.CompanionAbility:
                    return CompanionHasAbility(companion, rule.requiredAbilityId);

                case ExposureMitigationSource.TrioComposition:
                    return RuleMatches(rule, allCompanions)
                        && (companion.PioneerClass == rule.requiredClass
                            || companion.PioneerClass == rule.secondaryClass);

                default:
                    return false;
            }
        }

        private static bool CompanionHasAbility(PioneerCompanionAgent companion, string abilityId)
        {
            if (companion == null || string.IsNullOrWhiteSpace(abilityId))
                return false;

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
                return false;

            SkilledPioneerRecord record = roster.FindSkilledById(companion.PioneerRecordId);
            return record != null && PioneerTraitUtility.RecordHasAbility(record, abilityId);
        }
    }
}
