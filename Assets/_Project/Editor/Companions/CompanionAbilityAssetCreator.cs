#if UNITY_EDITOR
using Project.Building;
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
                deployableSlots: 0,
                fieldTriage.abilityId,
                injuryStabilize.abilityId);

            SaveAndClearCaches("Med Tech");
        }

        [MenuItem("Tools/Dark Matter Genesis/Companions/Create Base Role Class Assets")]
        public static void CreateBaseRoleClassAssets()
        {
            EnsureFolder(ClassProfileFolder);
            EnsureFolder(AbilityFolder);

            CompanionAbilityData supplyCache = CreateOrUpdateAbility(
                $"{AbilityFolder}/supply_cache.asset",
                BaseRoleCompanionBonusService.SupplyCacheAbilityId,
                "Supply Cache",
                CompanionAbilityKind.Tool,
                new[] { SkilledPioneerClass.LogisticsOfficer },
                cooldownSeconds: 18f,
                castDuration: 0.35f,
                aiPriority: 70);

            CompanionAbilityData quartermasterRoutes = CreateOrUpdateAbility(
                $"{AbilityFolder}/quartermaster_routes.asset",
                BaseRoleCompanionBonusService.QuartermasterRoutesAbilityId,
                "Quartermaster Routes",
                CompanionAbilityKind.Buff,
                new[] { SkilledPioneerClass.LogisticsOfficer },
                cooldownSeconds: 0f,
                castDuration: 0f,
                aiPriority: 55);

            CompanionAbilityData fieldSalvage = CreateOrUpdateAbility(
                $"{AbilityFolder}/field_salvage.asset",
                BaseRoleCompanionBonusService.FieldSalvageAbilityId,
                "Field Salvage",
                CompanionAbilityKind.Tool,
                new[] { SkilledPioneerClass.SalvageEngineer },
                cooldownSeconds: 14f,
                castDuration: 0.3f,
                aiPriority: 75);

            CompanionAbilityData upkeepPatch = CreateOrUpdateAbility(
                $"{AbilityFolder}/upkeep_patch.asset",
                BaseRoleCompanionBonusService.UpkeepPatchAbilityId,
                "Upkeep Patch",
                CompanionAbilityKind.Buff,
                new[] { SkilledPioneerClass.SalvageEngineer },
                cooldownSeconds: 0f,
                castDuration: 0f,
                aiPriority: 50);

            CreateOrUpdateClassProfile(
                $"{ClassProfileFolder}/logistics_officer_class_profile.asset",
                SkilledPioneerClass.LogisticsOfficer,
                deployableSlots: 1,
                supplyCache.abilityId,
                quartermasterRoutes.abilityId);

            CreateOrUpdateClassProfile(
                $"{ClassProfileFolder}/salvage_engineer_class_profile.asset",
                SkilledPioneerClass.SalvageEngineer,
                deployableSlots: 0,
                fieldSalvage.abilityId,
                upkeepPatch.abilityId);

            SaveAndClearCaches("Logistics Officer + Salvage Engineer base role");
        }

        private static void SaveAndClearCaches(string label)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CompanionClassProfileRegistry.ClearCache();
            CompanionAbilityRegistry.ClearCache();
            Debug.Log($"[Companions] {label} class profile and ability assets created under Resources.");
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
            int deployableSlots,
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
            profile.deployableSlots = deployableSlots;
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
