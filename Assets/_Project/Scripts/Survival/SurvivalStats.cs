using UnityEngine;

using Project.Core;

using Project.Data;

using Project.UI;



namespace Project.Survival

{

    public class SurvivalStats : MonoBehaviour

    {

        [Header("Survival Stats")]

        public float maxHealth = 100f;

        public float maxHunger = 100f;

        public float maxThirst = 100f;

        public float maxEnergy = 100f;



        [Header("Drain Rates")]

        public float hungerDrain = 0.5f;

        public float thirstDrain = 0.8f;

        public float energyDrain = 0.3f;



        [Header("Health Drain")]

        [Tooltip("Health drained per second while hunger or thirst is at/below the threshold.")]

        public float healthDrain = 2f;

        [Tooltip("Percent (0-100) of hunger/thirst that triggers health drain. Default 25 = drain below 25%.")]

        [Range(1f, 99f)]

        public float lowStatThreshold = 25f;



        [Header("Health Regen")]

        public bool enableHealthRegen = false;

        public float healthRegenPerSecond = 1f;

        public float healthRegenDelayAfterDamage = 5f;



        public float CurrentHealth { get; private set; }

        public float CurrentHunger { get; private set; }

        public float CurrentThirst { get; private set; }

        public float CurrentEnergy { get; private set; }

        public bool IsDead { get; private set; }

        public event System.Action PlayerDied;

        public event System.Action OnStatsChanged;

        private float lastHealthReductionTime = float.NegativeInfinity;
        private bool hasAppliedSaveState;
        private bool simulationPaused;

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

            CurrentHealth = maxHealth;

            CurrentHunger = maxHunger;

            CurrentThirst = maxThirst;

            CurrentEnergy = maxEnergy;

            OnStatsChanged?.Invoke();

        }

        public void ApplySaveState(float health, float hunger, float thirst, float energy)
        {
            hasAppliedSaveState = true;
            enabled = true;
            simulationPaused = false;
            IsDead = false;
            lastHealthReductionTime = float.NegativeInfinity;
            CurrentHealth = Mathf.Clamp(health, 0f, maxHealth);
            CurrentHunger = Mathf.Clamp(hunger, 0f, maxHunger);
            CurrentThirst = Mathf.Clamp(thirst, 0f, maxThirst);
            CurrentEnergy = Mathf.Clamp(energy, 0f, maxEnergy);
            NotifyStatsChanged();
            StartCoroutine(RefreshUiAfterLoad());
        }

        public void SetSimulationPaused(bool paused)
        {
            simulationPaused = paused;
            if (!paused)
                NotifyStatsChanged();
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

        private void Update()

        {

            if (simulationPaused || !GameSession.HasStarted)

                return;



            CurrentHunger = Mathf.Clamp(CurrentHunger - Time.deltaTime * hungerDrain, 0f, maxHunger);

            CurrentThirst = Mathf.Clamp(CurrentThirst - Time.deltaTime * thirstDrain, 0f, maxThirst);

            CurrentEnergy = Mathf.Clamp(CurrentEnergy - Time.deltaTime * energyDrain, 0f, maxEnergy);



            if (!IsDead)

            {

                float previousHealth = CurrentHealth;
                float healthLossRate = 0f;

                if (IsStatCritical(CurrentHunger, maxHunger))

                    healthLossRate += healthDrain;

                if (IsStatCritical(CurrentThirst, maxThirst))

                    healthLossRate += healthDrain;



                if (healthLossRate > 0f)

                    CurrentHealth = Mathf.Max(0f, CurrentHealth - Time.deltaTime * healthLossRate);



                if (CurrentHealth < previousHealth)

                    lastHealthReductionTime = Time.time;



                ApplyHealthRegen();



                if (CurrentHealth <= 0f)

                    Die();

            }



            OnStatsChanged?.Invoke();

        }



        private bool IsStatCritical(float current, float max)

        {

            if (max <= 0f) return false;

            return (current / max) * 100f <= lowStatThreshold;

        }



        public void Consume(ItemData item)

        {

            if (item == null || IsDead) return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + item.healthRestore);
            CurrentHunger = Mathf.Min(maxHunger, CurrentHunger + item.hungerRestore);

            CurrentThirst = Mathf.Min(maxThirst, CurrentThirst + item.thirstRestore);

            CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + item.energyRestore);

            OnStatsChanged?.Invoke();

        }



        public void SetEnergy(float newEnergy)

        {

            CurrentEnergy = Mathf.Clamp(newEnergy, 0f, maxEnergy);

        }



        public void ApplyDamage(float damage)

        {

            if (damage <= 0f || IsDead)

                return;



            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

            lastHealthReductionTime = Time.time;



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

            if (IsDead) return;



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


