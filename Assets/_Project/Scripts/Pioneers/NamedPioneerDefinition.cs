using UnityEngine;

namespace Project.Pioneers
{
    [CreateAssetMenu(fileName = "NamedPioneer", menuName = "Dark Matter Genesis/Named Companion")]
    public class NamedPioneerDefinition : ScriptableObject
    {
        public string pioneerId;
        public string displayName;

        [Tooltip("Not every companion is a rescued Echo — Expedition companions start with the " +
            "player, Support Ship companions join later via a story/quest trigger. This drives when " +
            "PioneerRosterManager grants them and which prefabs the Companion Prefab Tool generates.")]
        public CompanionOrigin origin = CompanionOrigin.Echo;

        [Tooltip("Only meaningful when origin is Other — what kind of unique character this is " +
            "(alien, AI bot, etc.). Purely narrative/UI flavor, no gameplay effect.")]
        public NonHumanKind nonHumanKind = NonHumanKind.Alien;

        [Tooltip("What this character says when the player meets them (CompanionOrigin.Other, shown " +
            "via UniqueRecruitEntity through the existing QuestGiverDialogUI popup). Leave empty to " +
            "fall back to backstory.")]
        [TextArea(2, 4)]
        public string recruitmentPitch;

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

        [Header("Buffs")]
        [Tooltip("Passive buffs this companion grants. Shown in the Journal Colonists tab trio panel alongside live exposure buffs/debuffs, and available for gameplay systems to read.")]
        public CompanionBuffModifier[] buffs = System.Array.Empty<CompanionBuffModifier>();

        [Header("Loadout Override")]
        [Tooltip("ItemData asset name for this companion's starting weapon. Leave empty to use the class default resolved by PioneerLoadoutDefaults.")]
        public string preferredWeaponItemId;

        [Tooltip("ItemData asset name for this companion's starting tool. Leave empty to use the class default.")]
        public string preferredToolItemId;

        [Header("Expedition Behavior")]
        public bool overrideDefaultFollowMode;
        public bool overrideDefaultWorldAmbientMode;
        public PioneerBehaviorProfile behavior = new PioneerBehaviorProfile();

        public string ResolvedId => string.IsNullOrWhiteSpace(pioneerId) ? name : pioneerId;
    }
}
