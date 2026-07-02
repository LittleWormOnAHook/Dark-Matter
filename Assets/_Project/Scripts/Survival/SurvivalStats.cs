using UnityEngine;
using Project.Core;
using Project.Data;
using Project.UI;

namespace Project.Survival
{
    public class SurvivalStats : MonoBehaviour
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

        public float CurrentHealth { get; private set; }
        public float CurrentEnergy { get; private set; }
        public float CurrentStamina { get; private set; }
        public float CurrentOxygen { get; private set; }

        public bool IsDead { get; private set; }

        public event System.Action PlayerDied;
        public event System.Action OnStatsChanged;
        public event System.Action<float> OnDamaged;

        public float LastDamageTime { get; private set; } = float.NegativeInfinity;

        private float lastHealthReductionTime = float.NegativeInfinity;
        private bool hasAppliedSaveState;
        private bool simulationPaused;
        private bool isSprinting;

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
            CurrentHealth = maxHealth;
            CurrentEnergy = maxEnergy;
            CurrentStamina = maxStamina;
            CurrentOxygen = maxOxygen;
            OnStatsChanged?.Invoke();
        }

        public void ApplySaveState(float health, float energy, float stamina, float oxygen)
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
            NotifyStatsChanged();
            StartCoroutine(RefreshUiAfterLoad());
        }

        public void ClampCurrentToMax(float health, float energy, float stamina, float oxygen)
        {
            CurrentHealth = Mathf.Clamp(health, 0f, maxHealth);
            CurrentEnergy = Mathf.Clamp(energy, 0f, maxEnergy);
            CurrentStamina = Mathf.Clamp(stamina, 0f, maxStamina);
            CurrentOxygen = Mathf.Clamp(oxygen, 0f, maxOxygen);
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

        private void NotifyStatsChanged()
        {
            OnStatsChanged?.Invoke();
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
            CurrentOxygen = Mathf.Clamp(CurrentOxygen - Time.deltaTime * oxygenDrainPerSecond, 0f, maxOxygen);

            if (!isSprinting)
                CurrentStamina = Mathf.Clamp(CurrentStamina + Time.deltaTime * staminaRegenPerSecond, 0f, maxStamina);

            float previousHealth = CurrentHealth;
            float healthLossRate = 0f;

            if (IsStatCritical(CurrentEnergy, maxEnergy))
                healthLossRate += healthDrain;

            if (CurrentOxygen <= 0f)
                healthLossRate += healthDrain * oxygenDepletedHealthDrainMultiplier;

            if (healthLossRate > 0f)
                CurrentHealth = Mathf.Max(0f, CurrentHealth - Time.deltaTime * healthLossRate);

            if (CurrentHealth < previousHealth)
                lastHealthReductionTime = Time.time;

            if (CurrentOxygen > 0f)
                ApplyHealthRegen();

            if (CurrentHealth <= 0f)
                Die();

            OnStatsChanged?.Invoke();
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
            OnStatsChanged?.Invoke();
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

        public void ApplyDamage(float damage)
        {
            if (damage <= 0f || IsDead)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            lastHealthReductionTime = Time.time;
            LastDamageTime = Time.time;
            OnDamaged?.Invoke(damage);

            if (CurrentHealth <= 0f)
                Die();
            else
                OnStatsChanged?.Invoke();
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

            Debug.Log("Player has died!");

            PlayerDied?.Invoke();

            UIManager ui = FindAnyObjectByType<UIManager>();
            if (ui != null)
                ui.ShowDeathPopup();
        }
    }
}
