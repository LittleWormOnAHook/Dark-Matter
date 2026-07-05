using Project.Data;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions.Abilities
{
    /// <summary>
    /// Defines a companion weapon, deployable, buff, or tool. Class restrictions and execution are configured per asset.
    /// </summary>
    [CreateAssetMenu(fileName = "companion_ability", menuName = "Survival Pioneer/Companions/Companion Ability")]
    public class CompanionAbilityData : ScriptableObject
    {
        [Header("Identity")]
        public string abilityId;
        public string displayName;
        public CompanionAbilityKind kind = CompanionAbilityKind.Weapon;

        [Header("Class Access")]
        [Tooltip("Pioneer classes allowed to equip this ability. Empty = all classes.")]
        public SkilledPioneerClass[] allowedClasses;

        [Header("Weapon Link")]
        [Tooltip("Optional ItemData when this ability represents a drawn/holstered weapon.")]
        public ItemData linkedItem;

        [Header("Timing")]
        public float cooldownSeconds = 6f;
        public float castDuration = 0.35f;

        [Header("AI Hints")]
        [Tooltip("Higher priority abilities are considered first by the companion decision layer.")]
        public int aiPriority = 50;

        public bool IsAllowedForClass(SkilledPioneerClass pioneerClass)
        {
            if (allowedClasses == null || allowedClasses.Length == 0)
                return true;

            for (int i = 0; i < allowedClasses.Length; i++)
            {
                if (allowedClasses[i] == pioneerClass)
                    return true;
            }

            return false;
        }
    }
}
