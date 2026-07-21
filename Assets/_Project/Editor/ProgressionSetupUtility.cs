#if UNITY_EDITOR
using System.IO;
using Project.EditorTools;
using Project.Progression;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Progression
{
    public static class ProgressionSetupUtility
    {
        private const string ProgressionResourcesRoot = "Assets/_Project/Resources/Progression";
        private const string SkillsFolder = ProgressionResourcesRoot + "/Skills";
        private const string CurvePath = ProgressionResourcesRoot + "/ProgressionCurve.asset";
        private const string RegistryPath = ProgressionResourcesRoot + "/SkillRegistry.asset";

        [MenuItem(SurvivalPioneerEditorMenus.Content + "Create Progression Curve", false, 40)]
        public static void CreateProgressionCurve()
        {
            EnsureFolder(ProgressionResourcesRoot);
            ProgressionCurveDefinition existing = AssetDatabase.LoadAssetAtPath<ProgressionCurveDefinition>(CurvePath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                return;
            }

            ProgressionCurveDefinition curve = ScriptableObject.CreateInstance<ProgressionCurveDefinition>();
            AssetDatabase.CreateAsset(curve, CurvePath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = curve;
        }

        [MenuItem(SurvivalPioneerEditorMenus.Content + "Create Starter Skills + Registry", false, 41)]
        public static void CreateStarterSkills()
        {
            EnsureFolder(SkillsFolder);

            SkillDefinition[] skills =
            {
                CreateSkill("skill_vital_boost", "Vital Boost", "Increase max health.", 1, SkillModifierType.MaxHealthPercent, 5f),
                CreateSkill("skill_endurance", "Endurance", "Increase max energy.", 1, SkillModifierType.MaxEnergyPercent, 5f),
                CreateSkill("skill_stamina_core", "Stamina Core", "Increase max stamina.", 2, SkillModifierType.MaxStaminaPercent, 5f),
                CreateSkill("skill_blade_training", "Blade Training", "+2 melee damage per rank.", 2, SkillModifierType.MeleeDamageFlat, 2f),
                CreateSkill("skill_gather_efficiency", "Gather Efficiency", "Gather resources faster.", 1, SkillModifierType.GatherSpeedPercent, 6f),
                CreateSkill("skill_artisan_focus", "Artisan Focus", "Earn more crafting XP.", 3, SkillModifierType.CraftXpPercent, 8f)
            };

            SkillRegistry registry = AssetDatabase.LoadAssetAtPath<SkillRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<SkillRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty array = serialized.FindProperty("skills");
            array.arraySize = skills.Length;
            for (int i = 0; i < skills.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            Selection.activeObject = registry;
        }

        private static SkillDefinition CreateSkill(
            string id,
            string displayName,
            string description,
            int requiredLevel,
            SkillModifierType modifier,
            float bonusPerRank)
        {
            string path = $"{SkillsFolder}/{id}.asset";
            SkillDefinition skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (skill == null)
            {
                skill = ScriptableObject.CreateInstance<SkillDefinition>();
                AssetDatabase.CreateAsset(skill, path);
            }

            skill.skillId = id;
            skill.displayName = displayName;
            skill.description = description;
            skill.requiredPlayerLevel = requiredLevel;
            skill.modifierType = modifier;
            skill.bonusPercentPerRank = bonusPerRank;
            skill.costPerRank = 1;
            skill.maxRank = 3;
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
