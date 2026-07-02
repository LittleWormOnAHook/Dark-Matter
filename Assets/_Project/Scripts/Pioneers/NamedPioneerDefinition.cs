using UnityEngine;

namespace Project.Pioneers
{
    [CreateAssetMenu(fileName = "NamedPioneer", menuName = "Survival Pioneer/Named Pioneer")]
    public class NamedPioneerDefinition : ScriptableObject
    {
        public string pioneerId;
        public string displayName;
        public SkilledPioneerClass pioneerClass = SkilledPioneerClass.CombatTactician;
        public int startLevel = 1;
        [Range(0f, 1f)] public float radiationResistance = 0.5f;
        [Range(0f, 1f)] public float expeditionEfficiency = 0.5f;
        [Range(0f, 1f)] public float combatSynergy = 0.5f;
        [Range(0f, 1f)] public float saturation = 0.2f;
        [TextArea(2, 4)] public string backstory;
        public string[] traitIds;
        public string[] passiveAbilityIds;
        public string[] learnedSkills;

        [Header("Expedition Behavior")]
        public bool overrideDefaultFollowMode;
        public PioneerBehaviorProfile behavior = new PioneerBehaviorProfile();

        public string ResolvedId => string.IsNullOrWhiteSpace(pioneerId) ? name : pioneerId;
    }
}
