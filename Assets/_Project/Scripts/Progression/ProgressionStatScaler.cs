using Project.Survival;
using UnityEngine;

namespace Project.Progression
{
    [RequireComponent(typeof(SurvivalStats))]
    public class ProgressionStatScaler : MonoBehaviour
    {
        private SurvivalStats stats;
        private PlayerProgressionManager progression;
        private float baseMaxHealth;
        private float baseMaxEnergy;
        private float baseMaxStamina;
        private float baseMaxOxygen;
        private bool basesCaptured;

        private void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            CaptureBaseMaxValues();
        }

        private void OnEnable()
        {
            progression = PlayerProgressionManager.EnsureExists();
            if (progression != null)
            {
                progression.OnLevelUp += HandleLevelUp;
                progression.OnXpChanged += HandleProgressionChanged;
            }

            ApplyLevelScaling();
        }

        private void OnDisable()
        {
            if (progression != null)
            {
                progression.OnLevelUp -= HandleLevelUp;
                progression.OnXpChanged -= HandleProgressionChanged;
            }
        }

        public void CaptureBaseMaxValues()
        {
            if (stats == null)
                return;

            baseMaxHealth = stats.maxHealth;
            baseMaxEnergy = stats.maxEnergy;
            baseMaxStamina = stats.maxStamina;
            baseMaxOxygen = stats.maxOxygen;
            basesCaptured = true;
        }

        private void HandleLevelUp(int newLevel, int levelsGained) => ApplyLevelScaling();

        private void HandleProgressionChanged() => ApplyLevelScaling();

        public void ApplyLevelScaling()
        {
            if (stats == null)
                return;

            if (!basesCaptured)
                CaptureBaseMaxValues();

            PlayerProgressionManager pm = progression ?? PlayerProgressionManager.EnsureExists();
            float levelMultiplier = pm != null ? pm.GetLevelStatMultiplier() : 1f;
            float healthSkill = 1f + PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.MaxHealthPercent) * 0.01f;
            float energySkill = 1f + PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.MaxEnergyPercent) * 0.01f;
            float staminaSkill = 1f + PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.MaxStaminaPercent) * 0.01f;

            float healthRatio = stats.maxHealth > 0.001f ? stats.CurrentHealth / stats.maxHealth : 1f;
            float energyRatio = stats.maxEnergy > 0.001f ? stats.CurrentEnergy / stats.maxEnergy : 1f;
            float staminaRatio = stats.maxStamina > 0.001f ? stats.CurrentStamina / stats.maxStamina : 1f;
            float oxygenRatio = stats.maxOxygen > 0.001f ? stats.CurrentOxygen / stats.maxOxygen : 1f;

            stats.maxHealth = baseMaxHealth * levelMultiplier * healthSkill;
            stats.maxEnergy = baseMaxEnergy * levelMultiplier * energySkill;
            stats.maxStamina = baseMaxStamina * levelMultiplier * staminaSkill;
            stats.maxOxygen = baseMaxOxygen * levelMultiplier;

            stats.ClampCurrentToMax(
                stats.maxHealth * healthRatio,
                stats.maxEnergy * energyRatio,
                stats.maxStamina * staminaRatio,
                stats.maxOxygen * oxygenRatio);
        }
    }
}
