using System;
using UnityEngine;

namespace Project.Progression
{
    public enum SkillModifierType
    {
        MaxHealthPercent,
        MaxEnergyPercent,
        MaxStaminaPercent,
        MeleeDamageFlat,
        GatherSpeedPercent,
        CraftXpPercent
    }

    [CreateAssetMenu(menuName = "Project/Progression/Skill Definition", fileName = "NewSkill")]
    public class SkillDefinition : ScriptableObject, ILevelGatedUpgrade
    {
        public string skillId;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public int requiredPlayerLevel = 1;
        public int costPerRank = 1;
        public int maxRank = 3;
        public SkillModifierType modifierType = SkillModifierType.MaxHealthPercent;
        [Tooltip("Percent bonus per rank for percent-based modifiers. Flat bonus per rank for MeleeDamageFlat.")]
        public float bonusPercentPerRank = 5f;
        public string[] prerequisiteSkillIds;

        public string ResolvedId => string.IsNullOrEmpty(skillId) ? name : skillId;
        public int RequiredPlayerLevel => requiredPlayerLevel;
    }
}
