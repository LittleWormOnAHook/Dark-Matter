#if UNITY_EDITOR
using Project.Companions.Abilities;
using Project.Pioneers;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Companions
{
    public static class CompanionAbilityAssetCreator
    {
        private const string ClassProfileFolder = "Assets/_Project/Resources/CompanionClassProfiles";
        private const string AbilityFolder = "Assets/_Project/Resources/CompanionAbilities";

        [MenuItem("Tools/Dark Matter Genesis/Companions/Create Med Tech Ability Assets")]
        public static void CreateMedTechAbilityAssets()
        {
            EnsureFolder(ClassProfileFolder);
            EnsureFolder(AbilityFolder);

            CompanionAbilityData fieldTriage = CreateOrUpdateAbility(
                $"{AbilityFolder}/field_triage.asset",
                "field_triage",
                "Field Triage",
                CompanionAbilityKind.Tool,
                new[] { SkilledPioneerClass.MedTech },
                cooldownSeconds: 10f,
                castDuration: 0.2f,
                aiPriority: 80);

            CompanionAbilityData injuryStabilize = CreateOrUpdateAbility(
                $"{AbilityFolder}/injury_stabilize.asset",
                "injury_stabilize",
                "Injury Stabilize",
                CompanionAbilityKind.Buff,
                new[] { SkilledPioneerClass.MedTech },
                cooldownSeconds: 0f,
                castDuration: 0f,
                aiPriority: 40);

            CreateOrUpdateClassProfile(
                $"{ClassProfileFolder}/med_tech_class_profile.asset",
                SkilledPioneerClass.MedTech,
                fieldTriage.abilityId,
                injuryStabilize.abilityId);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CompanionClassProfileRegistry.ClearCache();
            CompanionAbilityRegistry.ClearCache();
            Debug.Log("[Companions] Med Tech class profile and ability assets created under Resources.");
        }

        private static CompanionAbilityData CreateOrUpdateAbility(
            string assetPath,
            string abilityId,
            string displayName,
            CompanionAbilityKind kind,
            SkilledPioneerClass[] allowedClasses,
            float cooldownSeconds,
            float castDuration,
            int aiPriority)
        {
            CompanionAbilityData ability = AssetDatabase.LoadAssetAtPath<CompanionAbilityData>(assetPath);
            if (ability == null)
            {
                ability = ScriptableObject.CreateInstance<CompanionAbilityData>();
                AssetDatabase.CreateAsset(ability, assetPath);
            }

            ability.abilityId = abilityId;
            ability.displayName = displayName;
            ability.kind = kind;
            ability.allowedClasses = allowedClasses;
            ability.cooldownSeconds = cooldownSeconds;
            ability.castDuration = castDuration;
            ability.aiPriority = aiPriority;
            EditorUtility.SetDirty(ability);
            return ability;
        }

        private static void CreateOrUpdateClassProfile(
            string assetPath,
            SkilledPioneerClass pioneerClass,
            string defaultToolAbilityId,
            string defaultBuffAbilityId)
        {
            CompanionClassProfile profile = AssetDatabase.LoadAssetAtPath<CompanionClassProfile>(assetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CompanionClassProfile>();
                AssetDatabase.CreateAsset(profile, assetPath);
            }

            profile.pioneerClass = pioneerClass;
            profile.weaponSlots = 1;
            profile.deployableSlots = 0;
            profile.buffSlots = 1;
            profile.toolSlots = 1;
            profile.allowedKinds = new[]
            {
                CompanionAbilityKind.Weapon,
                CompanionAbilityKind.Buff,
                CompanionAbilityKind.Tool
            };
            profile.defaultToolAbilityId = defaultToolAbilityId;
            profile.defaultBuffAbilityId = defaultBuffAbilityId;
            EditorUtility.SetDirty(profile);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
