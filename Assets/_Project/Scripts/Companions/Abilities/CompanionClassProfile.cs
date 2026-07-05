using Project.Pioneers;
using UnityEngine;

namespace Project.Companions.Abilities
{
    /// <summary>
    /// Per-class loadout rules: allowed ability kinds, slot counts, and default ability ids.
    /// </summary>
    [CreateAssetMenu(fileName = "companion_class_profile", menuName = "Survival Pioneer/Companions/Class Profile")]
    public class CompanionClassProfile : ScriptableObject
    {
        public SkilledPioneerClass pioneerClass;

        [Header("Slot Limits")]
        public int weaponSlots = 1;
        public int deployableSlots = 1;
        public int buffSlots = 1;
        public int toolSlots = 1;

        [Header("Allowed Kinds")]
        public CompanionAbilityKind[] allowedKinds =
        {
            CompanionAbilityKind.Weapon,
            CompanionAbilityKind.Deployable,
            CompanionAbilityKind.Buff,
            CompanionAbilityKind.Tool
        };

        [Header("Defaults (configure later)")]
        public string defaultWeaponAbilityId;
        public string defaultDeployableAbilityId;
        public string defaultBuffAbilityId;
        public string defaultToolAbilityId;

        public bool AllowsKind(CompanionAbilityKind kind)
        {
            if (allowedKinds == null || allowedKinds.Length == 0)
                return true;

            for (int i = 0; i < allowedKinds.Length; i++)
            {
                if (allowedKinds[i] == kind)
                    return true;
            }

            return false;
        }
    }
}
