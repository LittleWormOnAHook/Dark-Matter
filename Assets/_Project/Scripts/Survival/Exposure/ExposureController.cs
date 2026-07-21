using System.Collections.Generic;
using Project.Companions;
using Project.Core;
using UnityEngine;

namespace Project.Survival.Exposure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SurvivalStats))]
    [RequireComponent(typeof(ExposureReceiver))]
    public class ExposureController : ExposureReceiver
    {
        private SurvivalStats survivalStats;

        public string ActiveMitigationLabel { get; private set; }
        public IReadOnlyList<string> ActiveMitigationLabels => activeMitigationLabels;
        public string PrimaryMitigationLabel => activeMitigationLabels.Count > 0 ? activeMitigationLabels[0] : string.Empty;
        public ExposureDebuffSettings ActivePlayerDebuffs { get; private set; } = new ExposureDebuffSettings();
        public float CurrentMoveSpeedPenalty { get; private set; }
        public float CurrentStaminaRegenPenalty { get; private set; }

        private readonly List<string> activeMitigationLabels = new List<string>(4);

        private void Awake()
        {
            survivalStats = GetComponent<SurvivalStats>();
            CleanupDuplicateReceivers();
        }

        private void CleanupDuplicateReceivers()
        {
            ExposureReceiver[] receivers = GetComponents<ExposureReceiver>();
            for (int i = 0; i < receivers.Length; i++)
            {
                if (receivers[i] != null && receivers[i] != this)
                    Destroy(receivers[i]);
            }
        }

        private void Update()
        {
            if (survivalStats == null || survivalStats.IsDead || !CanSimulate())
            {
                ClearRuntimeState();
                survivalStats?.ClearExternalExposureModifiers();
                return;
            }

            AggregateZonePressures(out ExposureSample sample, out ExposureMitigationService.MitigationResult mitigation);
            ApplyMitigationLabels(mitigation);
            ActivePlayerDebuffs = sample.playerDebuffs ?? ActivePlayerDebuffs;
            survivalStats.ApplyExternalExposure(sample, mitigation, Time.deltaTime);

            float exposureLevel = survivalStats.GetCombinedExposureLevel();
            ExposureDebuffSettings debuffs = sample.playerDebuffs ?? ActivePlayerDebuffs;
            CurrentMoveSpeedPenalty = EvaluateDebuffPenalty(debuffs, exposureLevel, d => d.moveSpeedPenalty);
            CurrentStaminaRegenPenalty = EvaluateDebuffPenalty(debuffs, exposureLevel, d => d.staminaRegenPenalty);
        }

        public string[] GetActiveZoneDisplayNames()
        {
            if (ActiveZones.Count == 0)
                return System.Array.Empty<string>();

            return ExposureHazardEvaluator.CollectActiveZoneNames(ActiveZones);
        }

        private bool CanSimulate()
        {
            return GameSession.HasStarted && Time.timeScale > 0f && survivalStats.enabled;
        }

        private void ClearRuntimeState()
        {
            ActiveMitigationLabel = string.Empty;
            activeMitigationLabels.Clear();
            ActivePlayerDebuffs = new ExposureDebuffSettings();
            CurrentMoveSpeedPenalty = 0f;
            CurrentStaminaRegenPenalty = 0f;
        }

        private void ApplyMitigationLabels(ExposureMitigationService.MitigationResult mitigation)
        {
            activeMitigationLabels.Clear();
            if (mitigation.activeLabels != null)
            {
                for (int i = 0; i < mitigation.activeLabels.Length; i++)
                {
                    string label = mitigation.activeLabels[i];
                    if (!string.IsNullOrWhiteSpace(label) && !activeMitigationLabels.Contains(label))
                        activeMitigationLabels.Add(label);
                }
            }

            ActiveMitigationLabel = activeMitigationLabels.Count > 0 ? activeMitigationLabels[0] : string.Empty;
        }

        private void AggregateZonePressures(
            out ExposureSample sample,
            out ExposureMitigationService.MitigationResult mitigation)
        {
            sample = default;
            mitigation = ExposureMitigationService.Combine(ActiveZones);

            // Squad-wide hazard mitigation contributed by the active trio's data-asset buffs
            // (CompanionBuffModifier.debuffResistance) and their radiationResistance specs — this is
            // what lets a companion's authored data file actually protect the player, on top of the
            // existing per-zone rule-based mitigation above.
            float groupHazardMultiplier = 1f - CompanionGroupBuffService.Current.HazardMitigation01;

            float radiation = 0f;
            float cold = 0f;
            float heat = 0f;
            float sulfur = 0f;
            float volcano = 0f;
            float oxygenMultiplier = 1f;
            float maxExposureHealthDrain = 0f;
            float maxThermalHealthDrain = 0f;
            float exposureRecovery = -1f;
            float thermalRecovery = -1f;
            ExposureDebuffSettings debuffs = default;
            bool hasDebuffs = false;

            bool radiationDriven = false;
            float radiationCeiling = 1f;
            bool thermalDriven = false;
            float thermalCeiling = 1f;
            bool sulfurDriven = false;
            float sulfurCeiling = 1f;
            bool volcanoDriven = false;
            float volcanoCeiling = 1f;

            for (int i = 0; i < ActiveZones.Count; i++)
            {
                ExposureZoneVolume zone = ActiveZones[i];
                if (zone == null || zone.Profile == null)
                    continue;

                ExposureSample zoneSample = zone.GetSampleForReceiver(this);
                ExposureMitigationService.MitigationResult zoneMitigation = ExposureMitigationService.Evaluate(zone.Profile);

                radiation += zoneSample.radiationPerSecond * zoneMitigation.radiationMultiplier * groupHazardMultiplier;
                cold += zoneSample.thermalColdPerSecond * zoneMitigation.thermalColdMultiplier * groupHazardMultiplier;
                heat += zoneSample.thermalHeatPerSecond * zoneMitigation.thermalHeatMultiplier * groupHazardMultiplier;
                sulfur += zoneSample.sulfurPerSecond * zoneMitigation.sulfurMultiplier * groupHazardMultiplier;
                volcano += zoneSample.volcanoPerSecond * zoneMitigation.volcanoMultiplier * groupHazardMultiplier;
                oxygenMultiplier *= zoneSample.oxygenDrainMultiplier * zoneMitigation.oxygenDrainMultiplier;

                maxExposureHealthDrain = Mathf.Max(maxExposureHealthDrain, zoneSample.healthDrainAtMaxExposure);
                maxThermalHealthDrain = Mathf.Max(maxThermalHealthDrain, zoneSample.healthDrainAtMaxThermal);

                if (zoneSample.exposureRecoveryPerSecond > 0f)
                    exposureRecovery = Mathf.Max(exposureRecovery, zoneSample.exposureRecoveryPerSecond);

                if (zoneSample.thermalRecoveryPerSecond > 0f)
                    thermalRecovery = Mathf.Max(thermalRecovery, zoneSample.thermalRecoveryPerSecond);

                if (zoneSample.playerDebuffs != null
                    && (zoneSample.playerDebuffs.debuffThreshold > 0f || zoneSample.playerDebuffs.moveSpeedPenalty > 0f))
                {
                    debuffs = zoneSample.playerDebuffs;
                    hasDebuffs = true;
                }

                if (zoneSample.radiationPerSecond > 0f)
                {
                    float c = Mathf.Clamp01(zoneSample.radiationCeiling01);
                    radiationCeiling = radiationDriven ? Mathf.Max(radiationCeiling, c) : c;
                    radiationDriven = true;
                }

                if (zoneSample.thermalColdPerSecond > 0f || zoneSample.thermalHeatPerSecond > 0f)
                {
                    float c = Mathf.Clamp01(zoneSample.thermalCeiling01);
                    thermalCeiling = thermalDriven ? Mathf.Max(thermalCeiling, c) : c;
                    thermalDriven = true;
                }

                if (zoneSample.sulfurPerSecond > 0f)
                {
                    float c = Mathf.Clamp01(zoneSample.sulfurCeiling01);
                    sulfurCeiling = sulfurDriven ? Mathf.Max(sulfurCeiling, c) : c;
                    sulfurDriven = true;
                }

                if (zoneSample.volcanoPerSecond > 0f)
                {
                    float c = Mathf.Clamp01(zoneSample.volcanoCeiling01);
                    volcanoCeiling = volcanoDriven ? Mathf.Max(volcanoCeiling, c) : c;
                    volcanoDriven = true;
                }
            }

            sample = new ExposureSample
            {
                radiationPerSecond = radiation,
                thermalColdPerSecond = cold,
                thermalHeatPerSecond = heat,
                sulfurPerSecond = sulfur,
                volcanoPerSecond = volcano,
                oxygenDrainMultiplier = oxygenMultiplier,
                healthDrainAtMaxExposure = maxExposureHealthDrain,
                healthDrainAtMaxThermal = maxThermalHealthDrain,
                exposureRecoveryPerSecond = exposureRecovery,
                thermalRecoveryPerSecond = thermalRecovery,
                playerDebuffs = hasDebuffs ? debuffs : new ExposureDebuffSettings(),
                radiationCeiling01 = radiationCeiling,
                thermalCeiling01 = thermalCeiling,
                sulfurCeiling01 = sulfurCeiling,
                volcanoCeiling01 = volcanoCeiling
            };
        }

        private static float EvaluateDebuffPenalty(
            ExposureDebuffSettings debuffs,
            float exposureLevel,
            System.Func<ExposureDebuffSettings, float> selector)
        {
            if (debuffs == null || exposureLevel < debuffs.debuffThreshold)
                return 0f;

            float t = Mathf.InverseLerp(debuffs.debuffThreshold, 1f, exposureLevel);
            return selector(debuffs) * t;
        }
    }
}
