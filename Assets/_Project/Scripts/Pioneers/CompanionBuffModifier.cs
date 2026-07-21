using System;
using UnityEngine;

namespace Project.Pioneers
{
    /// <summary>
    /// A single named passive buff a companion definition can grant. Shown in the Journal
    /// Pioneers tab trio panel alongside the live exposure-driven buffs/debuffs, and available
    /// for gameplay systems to read as they wire up to it (stat rolls, exposure resistance, etc.).
    /// </summary>
    [Serializable]
    public class CompanionBuffModifier
    {
        public string label = "Buff";

        [TextArea(1, 2)]
        public string description;

        [Header("Stat Bonuses (additive, 0 = no effect)")]
        public float radiationResistanceBonus;
        public float expeditionEfficiencyBonus;
        public float combatSynergyBonus;
        public float moveSpeedBonus;

        [Header("Exposure")]
        [Tooltip("Reduces this companion's incoming exposure debuff severity. 0 = no effect, 1 = fully immune.")]
        [Range(0f, 1f)]
        public float debuffResistance;
    }
}
