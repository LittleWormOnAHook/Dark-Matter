#if UNITY_EDITOR
using System.IO;
using Project.Achievements;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Achievements
{
    public static class AchievementSetupUtility
    {
        private const string ResourcesRoot = "Assets/_Project/Resources/Achievements";
        private const string RegistryPath = ResourcesRoot + "/AchievementRegistry.asset";

        [MenuItem(SurvivalPioneerEditorMenus.Content + "Create Starter Achievements", false, 42)]
        public static void CreateStarterAchievements()
        {
            EnsureFolder(ResourcesRoot);

            AchievementDefinition[] achievements =
            {
                CreateDefinition("first_pet", "First Companion", "Adopt your first pet.", AchievementCategory.Pets,
                    AchievementTriggerType.AdoptPet, 1, null, 75, false, 10),
                CreateDefinition("trio_leader", "Trio Leader", "Assign a full three-pioneer expedition.", AchievementCategory.Pioneers,
                    AchievementTriggerType.AssignTrio, 3, null, 100, false, 20),
                CreateDefinition("artisan", "Artisan", "Craft five items.", AchievementCategory.Crafting,
                    AchievementTriggerType.CraftItem, 5, null, 80, false, 30),
                CreateDefinition("survivor", "Survivor", "Reach level 5.", AchievementCategory.General,
                    AchievementTriggerType.ReachLevel, 1, "5", 150, false, 40),
                CreateDefinition("hidden_signal", "Signal Found", "Discover a hidden resonance imprint.", AchievementCategory.Exploration,
                    AchievementTriggerType.Manual, 1, null, 120, true, 90)
            };

            AchievementRegistry registry = AssetDatabase.LoadAssetAtPath<AchievementRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<AchievementRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty array = serialized.FindProperty("achievements");
            array.arraySize = achievements.Length;
            for (int i = 0; i < achievements.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = achievements[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            Selection.activeObject = registry;
            Debug.Log("Starter achievements created at " + RegistryPath);
        }

        private static AchievementDefinition CreateDefinition(
            string id,
            string title,
            string description,
            AchievementCategory category,
            AchievementTriggerType trigger,
            int targetCount,
            string targetId,
            int xp,
            bool hidden,
            int sortOrder)
        {
            string path = $"{ResourcesRoot}/{id}.asset";
            AchievementDefinition definition = AssetDatabase.LoadAssetAtPath<AchievementDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<AchievementDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.achievementId = id;
            definition.title = title;
            definition.description = description;
            definition.category = category;
            definition.triggerType = trigger;
            definition.targetCount = targetCount;
            definition.targetId = targetId;
            definition.xpReward = xp;
            definition.hidden = hidden;
            definition.sortOrder = sortOrder;
            EditorUtility.SetDirty(definition);
            return definition;
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
