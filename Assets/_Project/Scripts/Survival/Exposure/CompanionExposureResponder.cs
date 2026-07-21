using Project.Companions;
using Project.Core;
using UnityEngine;

namespace Project.Survival.Exposure
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PioneerCompanionAgent))]
    public class CompanionExposureResponder : ExposureReceiver
    {
        private PioneerCompanionAgent agent;
        private CompanionHealth health;

        public float MoveSpeedMultiplier { get; private set; } = 1f;
        public float DamageTakenMultiplier { get; private set; } = 1f;
        public float CurrentExposureLevel { get; private set; }
        public ExposureDebuffSettings ActiveDebuffSettings { get; private set; } = new ExposureDebuffSettings();

        private float radiation;
        private float sulfur;
        private float volcano;
        private float thermalMagnitude;

        private float radiationCeiling01 = 1f;
        private float sulfurCeiling01 = 1f;
        private float volcanoCeiling01 = 1f;
        private float thermalCeiling01 = 1f;

        private void Awake()
        {
            agent = GetComponent<PioneerCompanionAgent>();
            health = GetComponent<CompanionHealth>();
        }

        private void Update()
        {
            if (agent == null || health == null || health.IsDead || !CanSimulate())
            {
                ResetModifiers();
                DecayExposure(Time.deltaTime * 2f);
                return;
            }

            AggregatePressures(out ExposureSample sample);
            ApplyExposureBuildup(sample, Time.deltaTime);
            ApplyExposureDamage(sample, Time.deltaTime);
            ApplyDebuffModifiers(sample.companionDebuffs);
        }

        private bool CanSimulate()
        {
            return GameSession.HasStarted && Time.timeScale > 0f;
        }

        private void AggregatePressures(out ExposureSample sample)
        {
            sample = default;

            ExposureDebuffSettings debuffs = null;
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
                ExposureMitigationService.MitigationResult mitigation = ExposureMitigationService.Evaluate(zone.Profile);

                sample.radiationPerSecond += zoneSample.radiationPerSecond * mitigation.radiationMultiplier;
                sample.thermalColdPerSecond += zoneSample.thermalColdPerSecond * mitigation.thermalColdMultiplier;
                sample.thermalHeatPerSecond += zoneSample.thermalHeatPerSecond * mitigation.thermalHeatMultiplier;
                sample.sulfurPerSecond += zoneSample.sulfurPerSecond * mitigation.sulfurMultiplier;
                sample.volcanoPerSecond += zoneSample.volcanoPerSecond * mitigation.volcanoMultiplier;

                if (zoneSample.companionDebuffs != null
                    && (zoneSample.companionDebuffs.debuffThreshold > 0f || zoneSample.companionDebuffs.moveSpeedPenalty > 0f))
                {
                    debuffs = zoneSample.companionDebuffs;
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

            sample.companionDebuffs = hasDebuffs ? debuffs : new ExposureDebuffSettings();
            radiationCeiling01 = radiationCeiling;
            thermalCeiling01 = thermalCeiling;
            sulfurCeiling01 = sulfurCeiling;
            volcanoCeiling01 = volcanoCeiling;

            // Squad-wide mitigation from the trio's data-asset buffs (debuffResistance/radiation-
            // Resistance, see CompanionGroupBuffService) plus this companion's own personal
            // radiationResistance spec — stacked so a hardened individual companion resists more
            // than an unhardened one even inside the same shared squad aura.
            float groupMultiplier = 1f - CompanionGroupBuffService.Current.HazardMitigation01;
            float selfMultiplier = 1f - Mathf.Clamp01(agent?.BoundRecord != null ? agent.BoundRecord.radiationResistance * 0.3f : 0f);
            float mitigationMultiplier = groupMultiplier * selfMultiplier;

            sample.radiationPerSecond *= mitigationMultiplier;
            sample.thermalColdPerSecond *= mitigationMultiplier;
            sample.thermalHeatPerSecond *= mitigationMultiplier;
            sample.sulfurPerSecond *= mitigationMultiplier;
            sample.volcanoPerSecond *= mitigationMultiplier;
        }

        private void ApplyExposureBuildup(ExposureSample sample, float deltaTime)
        {
            // Each channel climbs to its effectIntensity cap and holds there — it does NOT
            // auto-drain. It only goes back down via leaving the zone (DecayExposure below) or a
            // mitigation source (companion buff, and later food/inoculation) easing the pressure.
            radiation = Mathf.Clamp(radiation + sample.radiationPerSecond * deltaTime, 0f, 100f * radiationCeiling01);
            sulfur = Mathf.Clamp(sulfur + sample.sulfurPerSecond * deltaTime, 0f, 100f * sulfurCeiling01);
            volcano = Mathf.Clamp(volcano + sample.volcanoPerSecond * deltaTime, 0f, 100f * volcanoCeiling01);

            float thermalDelta = (sample.thermalHeatPerSecond - sample.thermalColdPerSecond) * deltaTime;
            thermalMagnitude = Mathf.Clamp(thermalMagnitude + Mathf.Abs(thermalDelta), 0f, 100f * thermalCeiling01);

            if (ActiveZones.Count == 0)
                DecayExposure(deltaTime * 8f);

            CurrentExposureLevel = (radiation + sulfur + volcano + thermalMagnitude) / 400f;
        }

        private void DecayExposure(float amount)
        {
            radiation = Mathf.Max(0f, radiation - amount);
            sulfur = Mathf.Max(0f, sulfur - amount);
            volcano = Mathf.Max(0f, volcano - amount);
            thermalMagnitude = Mathf.Max(0f, thermalMagnitude - amount);
            CurrentExposureLevel = (radiation + sulfur + volcano + thermalMagnitude) / 400f;
        }

        private void ApplyExposureDamage(ExposureSample sample, float deltaTime)
        {
            if (health.IsDead || CurrentExposureLevel <= 0.35f)
                return;

            float damage = CurrentExposureLevel * 0.35f * deltaTime;
            if (damage > 0f)
                health.ApplyDamage(damage);
        }

        private void ApplyDebuffModifiers(ExposureDebuffSettings debuffs)
        {
            if (debuffs == null)
            {
                ActiveDebuffSettings = new ExposureDebuffSettings();
                ResetModifiers();
                return;
            }

            ActiveDebuffSettings = debuffs;

            if (CurrentExposureLevel < debuffs.debuffThreshold)
            {
                ResetModifiers();
                return;
            }

            float t = Mathf.InverseLerp(debuffs.debuffThreshold, 1f, CurrentExposureLevel);
            MoveSpeedMultiplier = 1f - debuffs.moveSpeedPenalty * t;
            DamageTakenMultiplier = Mathf.Lerp(1f, debuffs.damageTakenMultiplier, t);
        }

        private void ResetModifiers()
        {
            MoveSpeedMultiplier = 1f;
            DamageTakenMultiplier = 1f;
            ActiveDebuffSettings = new ExposureDebuffSettings();
        }
    }
}
