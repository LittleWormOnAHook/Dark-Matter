using System;
using System.Collections.Generic;
using Project.Companions;
using Project.Core;
using Project.Pioneers;
using Project.Survival;
using UnityEngine;

namespace Project.Survival.Exposure
{
    /// <summary>
    /// Builds a unified exposure readout from SurvivalStats, ExposureController, and active zones.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExposureStatusService : MonoBehaviour
    {
        private static ExposureStatusService instance;

        public static ExposureStatusService Instance => instance;
        public static ExposureStatusSnapshot Current => instance != null ? instance.snapshot : ExposureStatusSnapshot.Empty;

        public event Action<ExposureStatusSnapshot> OnSnapshotChanged;

        private ExposureStatusSnapshot snapshot = new ExposureStatusSnapshot();
        private SurvivalStats survivalStats;
        private ExposureController exposureController;
        private ExposureReceiver exposureReceiver;

        private readonly List<ExposureModifierTick> buffScratch = new List<ExposureModifierTick>(8);
        private readonly List<ExposureModifierTick> debuffScratch = new List<ExposureModifierTick>(8);
        private readonly List<ExposureModifierTick> companionBuffScratch = new List<ExposureModifierTick>(8);
        private readonly List<ExposureModifierTick> companionDebuffScratch = new List<ExposureModifierTick>(8);
        private readonly List<string> companionLabelScratch = new List<string>(4);
        private readonly CompanionExposureModifierSlot[] companionSlotScratch =
            new CompanionExposureModifierSlot[PioneerRosterManager.ExpeditionTrioSize];

        private int lastSnapshotHash;

        private static readonly ExposureDebuffSettings EmptyDebuffSettings = new ExposureDebuffSettings();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            EnsureCompanionSlotScratch();
            CacheComponents();
        }

        private void EnsureCompanionSlotScratch()
        {
            for (int i = 0; i < companionSlotScratch.Length; i++)
            {
                if (companionSlotScratch[i] == null)
                    companionSlotScratch[i] = new CompanionExposureModifierSlot();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void OnEnable()
        {
            CacheComponents();
            if (survivalStats != null)
                survivalStats.OnStatsChanged += HandleStatsChanged;

            RefreshSnapshot(forceNotify: true);
        }

        private void OnDisable()
        {
            if (survivalStats != null)
                survivalStats.OnStatsChanged -= HandleStatsChanged;
        }

        private void LateUpdate()
        {
            if (!CanRefresh())
                return;

            int before = lastSnapshotHash;
            RefreshSnapshot(forceNotify: false);
            if (lastSnapshotHash != before)
                OnSnapshotChanged?.Invoke(snapshot);
        }

        private void HandleStatsChanged()
        {
            RefreshSnapshot(forceNotify: true);
        }

        public void RefreshSnapshot(bool forceNotify)
        {
            CacheComponents();
            BuildSnapshot(snapshot);
            lastSnapshotHash = ComputeSnapshotHash(snapshot);

            if (forceNotify)
                OnSnapshotChanged?.Invoke(snapshot);
        }

        private bool CanRefresh()
        {
            return survivalStats != null
                && !survivalStats.IsDead
                && GameSession.HasStarted;
        }

        private void CacheComponents()
        {
            if (survivalStats == null)
                survivalStats = GetComponent<SurvivalStats>();

            if (exposureController == null)
                exposureController = GetComponent<ExposureController>();

            if (exposureReceiver == null)
                exposureReceiver = exposureController != null ? exposureController : GetComponent<ExposureReceiver>();
        }

        private void BuildSnapshot(ExposureStatusSnapshot target)
        {
            if (survivalStats == null)
            {
                ResetSnapshot(target);
                return;
            }

            float displayF = survivalStats.GetDisplayTemperatureFahrenheit();
            target.DisplayTemperatureF = displayF;
            target.ThermalStatusLabel = survivalStats.GetThermalStatusLabel();
            target.TemperatureText = ExposureTemperatureDisplay.FormatFahrenheit(displayF);
            target.TemperatureGaugeNormalized = ExposureTemperatureDisplay.FahrenheitToGaugeNormalized(displayF);

            IReadOnlyList<ExposureZoneVolume> zones = exposureReceiver != null
                ? exposureReceiver.ActiveZones
                : null;

            target.DominantHazard = ExposureHazardEvaluator.EvaluateDominant(survivalStats, zones);
            target.ActiveZoneNames = ExposureHazardEvaluator.CollectActiveZoneNames(zones);
            target.IsInShelter = target.DominantHazard.IsShelter
                || HasShelterZone(zones);
            target.CombinedExposureLevel = survivalStats.GetCombinedExposureLevel();
            BuildHazardLevels(target);

            BuildPlayerModifierTicks(target);
            BuildCompanionModifierSlots(target);
            target.PrimaryMitigationLabel = exposureController != null
                ? exposureController.PrimaryMitigationLabel
                : string.Empty;
        }

        private static int ComputeSnapshotHash(ExposureStatusSnapshot target)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Mathf.RoundToInt(target.DisplayTemperatureF * 10f);
                hash = hash * 31 + Mathf.RoundToInt(target.TemperatureGaugeNormalized * 500f);
                hash = hash * 31 + (target.ThermalStatusLabel?.GetHashCode() ?? 0);
                hash = hash * 31 + target.DominantHazard.Kind.GetHashCode();
                hash = hash * 31 + Mathf.RoundToInt(target.DominantHazard.Severity * 100f);
                hash = hash * 31 + target.IsInShelter.GetHashCode();
                hash = hash * 31 + (target.PlayerBuffTicks?.Length ?? 0);
                hash = hash * 31 + (target.PlayerDebuffTicks?.Length ?? 0);
                hash = hash * 31 + (target.ActiveZoneNames?.Length ?? 0);
                hash = hash * 31 + ComputeCompanionSlotsHash(target.ExpeditionCompanionSlots);
                hash = hash * 31 + Mathf.RoundToInt(target.ColdHazardLevel * 100f);
                hash = hash * 31 + Mathf.RoundToInt(target.HeatHazardLevel * 100f);
                hash = hash * 31 + Mathf.RoundToInt(target.RadiationHazardLevel * 100f);
                hash = hash * 31 + Mathf.RoundToInt(target.SulfurHazardLevel * 100f);
                hash = hash * 31 + Mathf.RoundToInt(target.VolcanoHazardLevel * 100f);
                return hash;
            }
        }

        private static int ComputeCompanionSlotsHash(CompanionExposureModifierSlot[] slots)
        {
            if (slots == null || slots.Length == 0)
                return 0;

            unchecked
            {
                int hash = 0;
                for (int i = 0; i < slots.Length; i++)
                {
                    CompanionExposureModifierSlot slot = slots[i];
                    if (slot == null)
                        continue;

                    hash = hash * 31 + slot.SlotIndex;
                    hash = hash * 31 + Mathf.RoundToInt(slot.ExposureLevel * 100f);
                    hash = hash * 31 + (slot.BuffTicks?.Length ?? 0);
                    hash = hash * 31 + (slot.DebuffTicks?.Length ?? 0);
                }

                return hash;
            }
        }

        private void BuildHazardLevels(ExposureStatusSnapshot target)
        {
            if (survivalStats == null)
            {
                target.ColdHazardLevel = 0f;
                target.HeatHazardLevel = 0f;
                target.RadiationHazardLevel = 0f;
                target.SulfurHazardLevel = 0f;
                target.VolcanoHazardLevel = 0f;
                target.HazardSeverityLabel = "CLEAR";
                return;
            }

            float thermalSigned = survivalStats.GetThermalNormalizedSigned();
            target.ColdHazardLevel = Mathf.Clamp01(-thermalSigned);
            target.HeatHazardLevel = Mathf.Clamp01(thermalSigned);
            target.RadiationHazardLevel = survivalStats.GetRadiationNormalized();
            target.SulfurHazardLevel = survivalStats.GetSulfurNormalized();
            target.VolcanoHazardLevel = survivalStats.GetVolcanoNormalized();
            target.HazardSeverityLabel = ResolveHazardSeverityLabel(target.CombinedExposureLevel);
        }

        private static string ResolveHazardSeverityLabel(float combinedExposureLevel)
        {
            if (combinedExposureLevel < 0.12f)
                return "CLEAR";

            if (combinedExposureLevel < 0.35f)
                return "LOW";

            if (combinedExposureLevel < 0.65f)
                return "MODERATE";

            return "EXTREME";
        }

        private void BuildPlayerModifierTicks(ExposureStatusSnapshot target)
        {
            buffScratch.Clear();
            debuffScratch.Clear();

            if (exposureController != null)
            {
                IReadOnlyList<string> labels = exposureController.ActiveMitigationLabels;
                for (int i = 0; i < labels.Count; i++)
                {
                    string label = labels[i];
                    if (string.IsNullOrWhiteSpace(label))
                        continue;

                    buffScratch.Add(ExposureModifierTick.Buff(
                        label,
                        "Expedition",
                        "+",
                        ExposureHazardPresentation.ShelterColor,
                        1f));
                }

                float exposureLevel = target.CombinedExposureLevel;
                ExposureDebuffSettings debuffs = exposureController.ActivePlayerDebuffs ?? EmptyDebuffSettings;
                string hazardSource = target.DominantHazard.IsClear
                    ? "Environment"
                    : target.DominantHazard.DisplayName;

                if (exposureLevel >= debuffs.debuffThreshold)
                {
                    float t = Mathf.InverseLerp(debuffs.debuffThreshold, 1f, exposureLevel);

                    if (debuffs.moveSpeedPenalty > 0f)
                    {
                        float penalty = debuffs.moveSpeedPenalty * t * 100f;
                        debuffScratch.Add(ExposureModifierTick.Debuff(
                            $"Move Speed −{Mathf.RoundToInt(penalty)}%",
                            hazardSource,
                            "−",
                            ExposureHazardPresentation.SulfurColor,
                            t));
                    }

                    if (debuffs.staminaRegenPenalty > 0f)
                    {
                        float penalty = debuffs.staminaRegenPenalty * t * 100f;
                        debuffScratch.Add(ExposureModifierTick.Debuff(
                            $"Stamina Regen −{Mathf.RoundToInt(penalty)}%",
                            hazardSource,
                            "−",
                            ExposureHazardPresentation.SulfurColor,
                            t));
                    }

                    if (debuffs.accuracyPenalty > 0f)
                    {
                        float penalty = debuffs.accuracyPenalty * t * 100f;
                        debuffScratch.Add(ExposureModifierTick.Debuff(
                            $"Accuracy −{Mathf.RoundToInt(penalty)}%",
                            hazardSource,
                            "−",
                            ExposureHazardPresentation.SulfurColor,
                            t));
                    }

                    if (debuffs.damageTakenMultiplier > 1f)
                    {
                        float bonus = (debuffs.damageTakenMultiplier - 1f) * t * 100f;
                        debuffScratch.Add(ExposureModifierTick.Debuff(
                            $"Damage Taken +{Mathf.RoundToInt(bonus)}%",
                            hazardSource,
                            "−",
                            ExposureHazardPresentation.VolcanoColor,
                            t));
                    }
                }
            }

            if (target.IsInShelter && !target.DominantHazard.IsClear)
            {
                buffScratch.Add(ExposureModifierTick.Buff(
                    "Shelter recovery",
                    "Shelter",
                    "+",
                    ExposureHazardPresentation.ShelterColor,
                    0.75f));
            }

            target.PlayerBuffTicks = buffScratch.ToArray();
            target.PlayerDebuffTicks = debuffScratch.ToArray();
        }

        private void BuildCompanionModifierSlots(ExposureStatusSnapshot target)
        {
            EnsureCompanionSlotScratch();

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            CompanionRosterBridge bridge = UnityEngine.Object.FindAnyObjectByType<CompanionRosterBridge>();
            IReadOnlyList<PioneerCompanionAgent> companions = bridge != null ? bridge.ActiveCompanions : null;

            for (int i = 0; i < companionSlotScratch.Length; i++)
            {
                CompanionExposureModifierSlot slot = companionSlotScratch[i];
                slot.SlotIndex = i;
                companionBuffScratch.Clear();
                companionDebuffScratch.Clear();

                SkilledPioneerRecord record = roster != null ? roster.GetExpeditionTrioRecordAtSlot(i) : null;
                if (record == null)
                {
                    slot.PioneerRecordId = string.Empty;
                    slot.DisplayName = string.Empty;
                    slot.ExposureLevel = 0f;
                    slot.BuffTicks = Array.Empty<ExposureModifierTick>();
                    slot.DebuffTicks = Array.Empty<ExposureModifierTick>();
                    continue;
                }

                slot.PioneerRecordId = record.id;
                slot.DisplayName = record.displayName;

                PioneerCompanionAgent agent = FindCompanionAgent(companions, record.id);
                CompanionExposureResponder responder = agent != null
                    ? agent.GetComponent<CompanionExposureResponder>()
                    : null;

                if (responder != null)
                {
                    slot.ExposureLevel = responder.CurrentExposureLevel;
                    BuildCompanionBuffTicks(responder, agent, companions, companionBuffScratch);
                    BuildCompanionDebuffTicks(responder, companionDebuffScratch);
                }
                else
                {
                    slot.ExposureLevel = 0f;
                }

                slot.BuffTicks = companionBuffScratch.ToArray();
                slot.DebuffTicks = companionDebuffScratch.ToArray();
            }

            target.ExpeditionCompanionSlots = companionSlotScratch;
        }

        private static PioneerCompanionAgent FindCompanionAgent(
            IReadOnlyList<PioneerCompanionAgent> companions,
            string pioneerRecordId)
        {
            if (companions == null || string.IsNullOrEmpty(pioneerRecordId))
                return null;

            for (int i = 0; i < companions.Count; i++)
            {
                PioneerCompanionAgent agent = companions[i];
                if (agent != null && agent.PioneerRecordId == pioneerRecordId)
                    return agent;
            }

            return null;
        }

        private void BuildCompanionBuffTicks(
            CompanionExposureResponder responder,
            PioneerCompanionAgent agent,
            IReadOnlyList<PioneerCompanionAgent> companions,
            List<ExposureModifierTick> buffsOut)
        {
            if (responder == null || agent == null)
                return;

            companionLabelScratch.Clear();
            ExposureMitigationService.CollectLabelsContributedByCompanion(
                responder.ActiveZones,
                agent,
                companions,
                companionLabelScratch);

            for (int i = 0; i < companionLabelScratch.Count; i++)
            {
                string label = companionLabelScratch[i];
                buffsOut.Add(ExposureModifierTick.Buff(
                    label,
                    agent.DisplayName,
                    "+",
                    ExposureHazardPresentation.ShelterColor,
                    1f));
            }
        }

        private void BuildCompanionDebuffTicks(
            CompanionExposureResponder responder,
            List<ExposureModifierTick> debuffsOut)
        {
            if (responder == null)
                return;

            float exposureLevel = responder.CurrentExposureLevel;
            ExposureDebuffSettings debuffs = responder.ActiveDebuffSettings ?? EmptyDebuffSettings;
            if (exposureLevel < debuffs.debuffThreshold)
                return;

            float t = Mathf.InverseLerp(debuffs.debuffThreshold, 1f, exposureLevel);

            if (debuffs.moveSpeedPenalty > 0f)
            {
                float penalty = debuffs.moveSpeedPenalty * t * 100f;
                debuffsOut.Add(ExposureModifierTick.Debuff(
                    $"Move −{Mathf.RoundToInt(penalty)}%",
                    "Environment",
                    "−",
                    ExposureHazardPresentation.SulfurColor,
                    t));
            }

            if (debuffs.damageTakenMultiplier > 1f)
            {
                float bonus = (debuffs.damageTakenMultiplier - 1f) * t * 100f;
                debuffsOut.Add(ExposureModifierTick.Debuff(
                    $"Vuln +{Mathf.RoundToInt(bonus)}%",
                    "Environment",
                    "−",
                    ExposureHazardPresentation.VolcanoColor,
                    t));
            }
        }

        private static bool HasShelterZone(IReadOnlyList<ExposureZoneVolume> zones)
        {
            if (zones == null)
                return false;

            for (int i = 0; i < zones.Count; i++)
            {
                ExposureZoneProfile profile = zones[i]?.Profile;
                if (profile != null && profile.zoneKind == ExposureZoneKind.ShelterSafe)
                    return true;
            }

            return false;
        }

        private static void ResetSnapshot(ExposureStatusSnapshot target)
        {
            target.DisplayTemperatureF = ExposureTemperatureDisplay.NominalFahrenheit;
            target.ThermalStatusLabel = "EVA NOMINAL";
            target.TemperatureText = "70°F";
            target.TemperatureGaugeNormalized = ExposureTemperatureDisplay.FahrenheitToGaugeNormalized(70f);
            target.DominantHazard = ExposureHazardState.Clear();
            target.ActiveZoneNames = Array.Empty<string>();
            target.IsInShelter = false;
            target.CombinedExposureLevel = 0f;
            target.PlayerBuffTicks = Array.Empty<ExposureModifierTick>();
            target.PlayerDebuffTicks = Array.Empty<ExposureModifierTick>();
            target.ExpeditionCompanionSlots = Array.Empty<CompanionExposureModifierSlot>();
            target.ColdHazardLevel = 0f;
            target.HeatHazardLevel = 0f;
            target.RadiationHazardLevel = 0f;
            target.SulfurHazardLevel = 0f;
            target.VolcanoHazardLevel = 0f;
            target.HazardSeverityLabel = "CLEAR";
            target.PrimaryMitigationLabel = string.Empty;
        }
    }
}
