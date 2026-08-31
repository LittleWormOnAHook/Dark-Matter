using System.Collections.Generic;
using UnityEngine;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Survival.Exposure;
using Project.UI;

namespace Project.Survival
{
    public class SurvivalStats : MonoBehaviour, IDamageable
    {
        public const float OxygenCriticalPercent = 15f;

        [Header("Survival Stats")]
        public float maxHealth = 100f;
        public float maxEnergy = 100f;
        public float maxStamina = 100f;
        public float maxOxygen = 2400f;

        [Header("Drain Rates")]
        public float energyDrain = 1.3f;
        public float oxygenDrainPerSecond = 4f;
        public float staminaRegenPerSecond = 12f;

        [Header("Health Drain")]
        [Tooltip("Health drained per second while energy is critical.")]
        public float healthDrain = 2f;

        [Tooltip("Multiplier applied to healthDrain while oxygen is depleted.")]
        public float oxygenDepletedHealthDrainMultiplier = 5f;

        [Tooltip("Percent (0-100) of energy that triggers health drain. Default 25 = drain below 25%.")]
        [Range(1f, 99f)]
        public float lowStatThreshold = 25f;

        [Header("Health Regen")]
        public bool enableHealthRegen = false;
        public float healthRegenPerSecond = 1f;
        public float healthRegenDelayAfterDamage = 5f;

        [Header("Exposure")]
        [Tooltip("Maximum thermal magnitude in each direction (cold negative, heat positive).")]
        public float maxThermalStress = 100f;

        public float maxRadiation = 100f;
        public float maxSulfur = 100f;
        public float maxVolcano = 100f;

        [Tooltip("Exposure decay per second when outside hazard zones.")]
        public float exposureRecoveryPerSecond = 8f;

        [Tooltip("Thermal drift toward neutral per second when outside hazard zones.")]
        public float thermalRecoveryPerSecond = 12f;

        public float CurrentHealth { get; private set; }
        public float CurrentEnergy { get; private set; }
        public float CurrentStamina { get; private set; }
        public float CurrentOxygen { get; private set; }
        public float CurrentThermalStress { get; private set; }
        public float CurrentRadiation { get; private set; }
        public float CurrentSulfur { get; private set; }
        public float CurrentVolcano { get; private set; }

        public bool IsDead { get; private set; }

        public event System.Action PlayerDied;
        public event System.Action PlayerRevived;
        public event System.Action OnStatsChanged;
        public event System.Action<float> OnDamaged;

        public float LastDamageTime { get; private set; } = float.NegativeInfinity;

        /// <summary>
        /// Brief window after respawn where enemies ignore the player for chase/attack.
        /// </summary>
        public bool HasEnemyCombatImmunity => Time.time < enemyCombatImmunityUntil;

        public void GrantEnemyCombatImmunity(float durationSeconds)
        {
            enemyCombatImmunityUntil = Time.time + Mathf.Max(0f, durationSeconds);
        }

        private float lastHealthReductionTime = float.NegativeInfinity;
        private float enemyCombatImmunityUntil;
        private bool hasAppliedSaveState;
        private bool simulationPaused;
        private bool isSprinting;
        private string lastDamageSource = "unknown";
        private float externalOxygenDrainMultiplier = 1f;
        private float externalExposureHealthDrain;
        private float externalThermalHealthDrain;
        private bool insideExposureZone;
        private ExposureController cachedExposureController;

        // Walking drains stats every frame; notifying UI/listeners every frame allocates TMP strings
        // and rebuilds exposure snapshots. Quantize + throttle keeps bars smooth enough without GC stalls.
        private const float StatsNotifyMinInterval = 0.1f;
        private float nextStatsNotifyTime;
        private float lastNotifiedHealth = float.NaN;
        private float lastNotifiedEnergy = float.NaN;
        private float lastNotifiedStamina = float.NaN;
        private float lastNotifiedOxygen = float.NaN;
        private float lastNotifiedThermal = float.NaN;
        private float lastNotifiedRadiation = float.NaN;
        private float lastNotifiedSulfur = float.NaN;
        private float lastNotifiedVolcano = float.NaN;

        private void OnValidate()
        {
            lowStatThreshold = Mathf.Clamp(lowStatThreshold, 1f, 99f);
        }

        private void Start()
        {
            if (!hasAppliedSaveState)
                ResetStats();
        }

        public void ResetStats()
        {
            IsDead = false;
            LastDamageTime = float.NegativeInfinity;
            enemyCombatImmunityUntil = 0f;
            lastDamageSource = "unknown";
            CurrentHealth = maxHealth;
            CurrentEnergy = maxEnergy;
            CurrentStamina = maxStamina;
            CurrentOxygen = maxOxygen;
            CurrentThermalStress = 0f;
            CurrentRadiation = 0f;
            CurrentSulfur = 0f;
            CurrentVolcano = 0f;
            ClearExternalExposureModifiers();
            NotifyStatsChanged(force: true);
        }

        /// <summary>
        /// Called by PlayerDeathHandler after a death-popup respawn.
        /// </summary>
        public void NotifyRevivedAfterRespawn(float immunitySeconds = 3f)
        {
            GrantEnemyCombatImmunity(immunitySeconds);
            PlayerRevived?.Invoke();
        }

        public void ApplySaveState(
            float health,
            float energy,
            float stamina,
            float oxygen,
            float thermalStress = 0f,
            float radiation = 0f,
            float sulfur = 0f,
            float volcano = 0f)
        {
            hasAppliedSaveState = true;
            enabled = true;
            simulationPaused = false;
            IsDead = false;
            lastHealthReductionTime = float.NegativeInfinity;
            CurrentHealth = Mathf.Clamp(health, 0f, maxHealth);
            CurrentEnergy = Mathf.Clamp(energy, 0f, maxEnergy);
            CurrentStamina = Mathf.Clamp(stamina, 0f, maxStamina);
            CurrentOxygen = Mathf.Clamp(oxygen, 0f, maxOxygen);
            CurrentThermalStress = Mathf.Clamp(thermalStress, -maxThermalStress, maxThermalStress);
            CurrentRadiation = Mathf.Clamp(radiation, 0f, maxRadiation);
            CurrentSulfur = Mathf.Clamp(sulfur, 0f, maxSulfur);
            CurrentVolcano = Mathf.Clamp(volcano, 0f, maxVolcano);
            ClearExternalExposureModifiers();
            NotifyStatsChanged();
            StartCoroutine(RefreshUiAfterLoad());
        }

        public void ApplyLegacySaveState(float health, float energy, float stamina, float oxygen)
        {
            ApplySaveState(health, energy, stamina, oxygen);
        }

        public void ClampCurrentToMax(
            float health,
            float energy,
            float stamina,
            float oxygen,
            float thermalStress = 0f,
            float radiation = 0f,
            float sulfur = 0f,
            float volcano = 0f)
        {
            CurrentHealth = Mathf.Clamp(health, 0f, maxHealth);
            CurrentEnergy = Mathf.Clamp(energy, 0f, maxEnergy);
            CurrentStamina = Mathf.Clamp(stamina, 0f, maxStamina);
            CurrentOxygen = Mathf.Clamp(oxygen, 0f, maxOxygen);
            CurrentThermalStress = Mathf.Clamp(thermalStress, -maxThermalStress, maxThermalStress);
            CurrentRadiation = Mathf.Clamp(radiation, 0f, maxRadiation);
            CurrentSulfur = Mathf.Clamp(sulfur, 0f, maxSulfur);
            CurrentVolcano = Mathf.Clamp(volcano, 0f, maxVolcano);
            NotifyStatsChanged();
        }

        public void SetSimulationPaused(bool paused)
        {
            simulationPaused = paused;
            if (!paused)
                NotifyStatsChanged();
        }

        public void SetSprinting(bool sprinting)
        {
            isSprinting = sprinting;
        }

        public void ResetForNewGame()
        {
            hasAppliedSaveState = false;
            simulationPaused = false;
            ResetStats();
        }

        private System.Collections.IEnumerator RefreshUiAfterLoad()
        {
            yield return null;
            NotifyStatsChanged();

            UIManager ui = FindAnyObjectByType<UIManager>();
            if (ui != null)
                ui.RefreshSurvivalDisplay();
        }

        private void NotifyStatsChanged(bool force = true)
        {
            if (!force)
            {
                if (Time.unscaledTime < nextStatsNotifyTime)
                    return;

                if (!HasNotifiableStatsDelta())
                    return;

                nextStatsNotifyTime = Time.unscaledTime + StatsNotifyMinInterval;
            }

            CaptureNotifiedStatsSnapshot();
            OnStatsChanged?.Invoke();
        }

        private bool HasNotifiableStatsDelta()
        {
            // Display text uses ceil/round; keep notify thresholds near UI quantization.
            return !ApproximatelyUi(CurrentHealth, lastNotifiedHealth, 0.25f)
                || !ApproximatelyUi(CurrentEnergy, lastNotifiedEnergy, 0.25f)
                || !ApproximatelyUi(CurrentStamina, lastNotifiedStamina, 0.5f)
                || !ApproximatelyUi(CurrentOxygen, lastNotifiedOxygen, 0.5f)
                || !ApproximatelyUi(CurrentThermalStress, lastNotifiedThermal, 0.25f)
                || !ApproximatelyUi(CurrentRadiation, lastNotifiedRadiation, 0.25f)
                || !ApproximatelyUi(CurrentSulfur, lastNotifiedSulfur, 0.25f)
                || !ApproximatelyUi(CurrentVolcano, lastNotifiedVolcano, 0.25f);
        }

        private void CaptureNotifiedStatsSnapshot()
        {
            lastNotifiedHealth = CurrentHealth;
            lastNotifiedEnergy = CurrentEnergy;
            lastNotifiedStamina = CurrentStamina;
            lastNotifiedOxygen = CurrentOxygen;
            lastNotifiedThermal = CurrentThermalStress;
            lastNotifiedRadiation = CurrentRadiation;
            lastNotifiedSulfur = CurrentSulfur;
            lastNotifiedVolcano = CurrentVolcano;
        }

        private static bool ApproximatelyUi(float current, float last, float threshold)
        {
            return !float.IsNaN(last) && Mathf.Abs(current - last) < threshold;
        }

        private bool CanSimulateStats()
        {
            return !simulationPaused
                && GameSession.HasStarted
                && Time.timeScale > 0f;
        }

        private void Update()
        {
            if (IsDead || !CanSimulateStats())
                return;

            CurrentEnergy = Mathf.Clamp(CurrentEnergy - Time.deltaTime * energyDrain, 0f, maxEnergy);
            CurrentOxygen = Mathf.Clamp(
                CurrentOxygen - Time.deltaTime * oxygenDrainPerSecond * externalOxygenDrainMultiplier,
                0f,
                maxOxygen);

            if (!isSprinting)
                CurrentStamina = Mathf.Clamp(
                    CurrentStamina + Time.deltaTime * staminaRegenPerSecond * GetStaminaRegenMultiplier(),
                    0f,
                    maxStamina);

            if (!insideExposureZone)
                RecoverExposure(Time.deltaTime);

            float previousHealth = CurrentHealth;
            float healthLossRate = 0f;

            if (IsStatCritical(CurrentEnergy, maxEnergy))
                healthLossRate += healthDrain;

            if (CurrentOxygen <= 0f)
                healthLossRate += healthDrain * oxygenDepletedHealthDrainMultiplier;

            healthLossRate += externalExposureHealthDrain + externalThermalHealthDrain;

            if (healthLossRate > 0f)
                CurrentHealth = Mathf.Max(0f, CurrentHealth - Time.deltaTime * healthLossRate);

            if (CurrentHealth < previousHealth)
                lastHealthReductionTime = Time.time;

            if (CurrentOxygen > 0f)
                ApplyHealthRegen();

            if (CurrentHealth <= 0f)
                Die();

            NotifyStatsChanged(force: false);
        }

        private bool IsStatCritical(float current, float max)
        {
            if (max <= 0f)
                return false;

            return (current / max) * 100f <= lowStatThreshold;
        }

        public void Consume(ItemData item)
        {
            if (item == null || IsDead)
                return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + item.healthRestore);
            CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + item.energyRestore);
            CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + item.staminaRestore);
            CurrentOxygen = Mathf.Min(maxOxygen, CurrentOxygen + item.oxygenRestore);
            NotifyStatsChanged(force: true);
        }

        public void SetStamina(float newStamina)
        {
            CurrentStamina = Mathf.Clamp(newStamina, 0f, maxStamina);
        }

        public float GetOxygenDisplayMinutes()
        {
            return Mathf.Floor(CurrentOxygen / 60f);
        }

        public float GetOxygenNormalized()
        {
            if (maxOxygen <= 0f)
                return 0f;

            return CurrentOxygen / maxOxygen;
        }

        public bool IsOxygenCritical()
        {
            return GetOxygenNormalized() * 100f <= OxygenCriticalPercent;
        }

        public float GetThermalNormalizedSigned()
        {
            if (maxThermalStress <= 0f)
                return 0f;

            return CurrentThermalStress / maxThermalStress;
        }

        public float GetThermalHudFill()
        {
            return Mathf.InverseLerp(-maxThermalStress, maxThermalStress, CurrentThermalStress);
        }

        public float GetRadiationNormalized()
        {
            return maxRadiation <= 0f ? 0f : CurrentRadiation / maxRadiation;
        }

        public float GetSulfurNormalized()
        {
            return maxSulfur <= 0f ? 0f : CurrentSulfur / maxSulfur;
        }

        public float GetVolcanoNormalized()
        {
            return maxVolcano <= 0f ? 0f : CurrentVolcano / maxVolcano;
        }

        public float GetCombinedExposureLevel()
        {
            float rad = GetRadiationNormalized();
            float sulfur = GetSulfurNormalized();
            float volcano = GetVolcanoNormalized();
            float thermal = Mathf.Abs(GetThermalNormalizedSigned());
            return (rad + sulfur + volcano + thermal) * 0.25f;
        }

        public float GetDisplayTemperatureFahrenheit()
        {
            return ExposureTemperatureDisplay.StressToFahrenheit(CurrentThermalStress, maxThermalStress);
        }

        public float GetDisplayTemperatureGaugeNormalized()
        {
            return ExposureTemperatureDisplay.FahrenheitToGaugeNormalized(GetDisplayTemperatureFahrenheit());
        }

        public string GetThermalStatusLabel()
        {
            return ExposureTemperatureDisplay.GetStatusLabel(CurrentThermalStress, maxThermalStress);
        }

        public ExposureHazardState GetDominantHazardState(ExposureReceiver receiver)
        {
            IReadOnlyList<ExposureZoneVolume> zones = receiver != null ? receiver.ActiveZones : null;
            return ExposureHazardEvaluator.EvaluateDominant(this, zones);
        }

        public void ClearExternalExposureModifiers()
        {
            externalOxygenDrainMultiplier = 1f;
            externalExposureHealthDrain = 0f;
            externalThermalHealthDrain = 0f;
            insideExposureZone = false;
        }

        public void ApplyExternalExposure(
            ExposureSample sample,
            ExposureMitigationService.MitigationResult mitigation,
            float deltaTime)
        {
            float radiationRate = sample.radiationPerSecond;
            float sulfurRate = sample.sulfurPerSecond;
            float volcanoRate = sample.volcanoPerSecond;
            float coldRate = sample.thermalColdPerSecond;
            float heatRate = sample.thermalHeatPerSecond;

            insideExposureZone = radiationRate > 0f
                || sulfurRate > 0f
                || volcanoRate > 0f
                || coldRate > 0f
                || heatRate > 0f;

            // Ceilings default to 1 (uncapped) when no active zone drives that channel, so this
            // matches prior behavior unless a zone's effectIntensity explicitly caps it lower.
            float radiationCap = Mathf.Clamp01(sample.radiationCeiling01) * maxRadiation;
            float sulfurCap = Mathf.Clamp01(sample.sulfurCeiling01) * maxSulfur;
            float volcanoCap = Mathf.Clamp01(sample.volcanoCeiling01) * maxVolcano;
            float thermalCapMagnitude = Mathf.Clamp01(sample.thermalCeiling01) * maxThermalStress;

            // Each channel climbs to its effectIntensity cap and holds there — it does NOT
            // auto-drain. The only ways it goes back down are leaving the zone (RecoverExposure
            // decay below) or a mitigation source (companion buff, and later food/inoculation)
            // reducing the incoming rate or applying a direct reduction.
            CurrentRadiation = Mathf.Clamp(CurrentRadiation + radiationRate * deltaTime, 0f, radiationCap);
            CurrentSulfur = Mathf.Clamp(CurrentSulfur + sulfurRate * deltaTime, 0f, sulfurCap);
            CurrentVolcano = Mathf.Clamp(CurrentVolcano + volcanoRate * deltaTime, 0f, volcanoCap);

            float thermalDelta = (heatRate - coldRate) * deltaTime;
            CurrentThermalStress = Mathf.Clamp(
                CurrentThermalStress + thermalDelta,
                -thermalCapMagnitude,
                thermalCapMagnitude);

            externalOxygenDrainMultiplier = sample.oxygenDrainMultiplier * mitigation.oxygenDrainMultiplier;

            float exposureLevel = GetCombinedExposureLevel();
            externalExposureHealthDrain = exposureLevel * sample.healthDrainAtMaxExposure;
            externalThermalHealthDrain = Mathf.Abs(GetThermalNormalizedSigned()) * sample.healthDrainAtMaxThermal;

            if (sample.exposureRecoveryPerSecond > 0f)
                exposureRecoveryPerSecond = sample.exposureRecoveryPerSecond;

            if (sample.thermalRecoveryPerSecond > 0f)
                thermalRecoveryPerSecond = sample.thermalRecoveryPerSecond;

            if (!insideExposureZone && (sample.exposureRecoveryPerSecond > 0f || sample.thermalRecoveryPerSecond > 0f))
                RecoverExposure(deltaTime * 2f);
        }

        private void RecoverExposure(float deltaTime)
        {
            CurrentRadiation = Mathf.Max(0f, CurrentRadiation - exposureRecoveryPerSecond * deltaTime);
            CurrentSulfur = Mathf.Max(0f, CurrentSulfur - exposureRecoveryPerSecond * deltaTime);
            CurrentVolcano = Mathf.Max(0f, CurrentVolcano - exposureRecoveryPerSecond * deltaTime);

            if (Mathf.Abs(CurrentThermalStress) <= 0.01f)
                CurrentThermalStress = 0f;
            else if (CurrentThermalStress > 0f)
                CurrentThermalStress = Mathf.Max(0f, CurrentThermalStress - thermalRecoveryPerSecond * deltaTime);
            else
                CurrentThermalStress = Mathf.Min(0f, CurrentThermalStress + thermalRecoveryPerSecond * deltaTime);
        }

        private float GetStaminaRegenMultiplier()
        {
            if (cachedExposureController == null)
                cachedExposureController = GetComponent<ExposureController>();
            if (cachedExposureController == null)
                return 1f;

            return 1f - cachedExposureController.CurrentStaminaRegenPenalty;
        }

        void IDamageable.TakeDamage(float damage, GameObject source, bool isCritical)
        {
            ApplyDamage(damage, source != null ? source.name : null);
        }

        public void ApplyDamage(float damage, string sourceName = null)
        {
            if (damage <= 0f || IsDead)
                return;

            bool fromFall = string.Equals(sourceName, "fall", System.StringComparison.OrdinalIgnoreCase);
            if (HasEnemyCombatImmunity && !fromFall)
                return;

            lastDamageSource = string.IsNullOrWhiteSpace(sourceName) ? "unknown" : sourceName;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            lastHealthReductionTime = Time.time;
            LastDamageTime = Time.time;
            OnDamaged?.Invoke(damage);

            if (CurrentHealth <= 0f)
                Die();
            else
                NotifyStatsChanged(force: true);
        }

        /// <summary>
        /// Lethal fall. Bypasses enemy combat immunity so a 20m drop still kills
        /// during post-respawn grace. Other damage sources are unchanged.
        /// </summary>
        public void KillFromFall()
        {
            if (IsDead)
                return;

            lastDamageSource = "fall";
            CurrentHealth = 0f;
            lastHealthReductionTime = Time.time;
            LastDamageTime = Time.time;
            OnDamaged?.Invoke(9999f);
            Die();
        }

        private void ApplyHealthRegen()
        {
            if (!enableHealthRegen || CurrentHealth <= 0f || CurrentHealth >= maxHealth)
                return;

            if (Time.time < lastHealthReductionTime + healthRegenDelayAfterDamage)
                return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + healthRegenPerSecond * Time.deltaTime);
        }

        private void Die()
        {
            if (IsDead)
                return;

            IsDead = true;
            CurrentHealth = 0f;
            SetSimulationPaused(true);

            Debug.Log($"Player has died! (killed by {lastDamageSource}, last hit left health at 0)");

            PlayerDied?.Invoke();

            // Menu waits for ragdoll to settle. PlayerDeathHandler owns that delay.
            // Keep an immediate fallback if this body has no death handler.
            if (GetComponent<Project.Player.PlayerDeathHandler>() == null)
            {
                UIManager ui = FindAnyObjectByType<UIManager>();
                if (ui != null)
                    ui.ShowDeathPopup();
            }
        }
    }
}
