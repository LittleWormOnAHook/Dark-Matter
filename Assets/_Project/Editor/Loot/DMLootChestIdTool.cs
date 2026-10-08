using System;
using System.Collections.Generic;
using System.Text;
using Project.Data;
using Project.Events;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.Loot
{
    /// <summary>
    /// Loot chest ids and validators (loot plan 6.3 / 13.1 #9).
    /// - Menu: assigns a GUID-style chestId to every DMItemCollection in open scenes that has none or a duplicate
    ///   (Ctrl+D copies). Marks the scene dirty; saving stays the designer's call.
    /// - On scene save: warns about empty / duplicate chestIds and non-item (Aether Credit) loot slots (D6). Never edits.
    /// </summary>
    [InitializeOnLoad]
    public static class DMLootChestIdTool
    {
        private const string MenuRoot = "Tools/Dark Matter Genesis/Loot/";

        static DMLootChestIdTool()
        {
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        [MenuItem(MenuRoot + "Assign Missing Chest Ids", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Loot_Assign_Missing_Chest_Ids)]
        public static void AssignMissingIds()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            int assigned = 0;
            foreach (DMItemCollection chest in FindChestsInOpenScenes())
            {
                SerializedObject so = new SerializedObject(chest);
                SerializedProperty id = so.FindProperty("chestId");
                if (id == null)
                    continue;

                string value = id.stringValue != null ? id.stringValue.Trim() : string.Empty;
                if (value.Length > 0 && seen.Add(value))
                    continue;

                string fresh;
                do
                    fresh = "lootchest_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                while (!seen.Add(fresh));

                Undo.RecordObject(chest, "Assign Loot Chest Id");
                id.stringValue = fresh;
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(chest.gameObject.scene);
                assigned++;
            }

            Debug.Log($"[DMLootChestIdTool] Assigned {assigned} chest id(s). Save the scene to keep them.");
        }

        [MenuItem(MenuRoot + "Validate Loot Chests", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Loot_Validate_Loot_Chests)]
        public static void ValidateOpenScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                Validate(SceneManager.GetSceneAt(i), logClean: true);
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            Validate(scene, logClean: false);
        }

        private static void Validate(Scene scene, bool logClean)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            Dictionary<string, DMItemCollection> ids = new Dictionary<string, DMItemCollection>(StringComparer.Ordinal);
            StringBuilder report = new StringBuilder();
            int issues = 0;

            foreach (DMItemCollection chest in FindChests(scene))
            {
                string id = chest.ChestId != null ? chest.ChestId.Trim() : string.Empty;
                if (id.Length == 0)
                {
                    issues++;
                    report.AppendLine($"- '{PathOf(chest.transform)}' has no chestId (its state will not save).");
                }
                else if (ids.TryGetValue(id, out DMItemCollection other))
                {
                    issues++;
                    report.AppendLine($"- Duplicate chestId '{id}': '{PathOf(chest.transform)}' and '{PathOf(other.transform)}'.");
                }
                else
                {
                    ids[id] = chest;
                }
            }

            foreach (DmEvents events in FindInScene<DmEvents>(scene))
            {
                SerializedProperty slots = new SerializedObject(events).FindProperty("lootSlots");
                if (slots == null || !slots.isArray)
                    continue;

                for (int i = 0; i < slots.arraySize; i++)
                {
                    SerializedProperty item = slots.GetArrayElementAtIndex(i).FindPropertyRelative("item");
                    ItemData data = item != null ? item.objectReferenceValue as ItemData : null;
                    if (data == null || !data.isAcInfused)
                        continue;
                    issues++;
                    report.AppendLine($"- '{PathOf(events.transform)}' loot slot {i} ({data.itemName}) carries Aether Credits; chests hold items only (D6).");
                }
            }

            if (issues > 0)
            {
                Debug.LogWarning(
                    $"[DMLootChestIdTool] {scene.name}: {issues} loot chest issue(s). " +
                    "Run Tools/Dark Matter Genesis/Loot/Assign Missing Chest Ids for id problems.\n" + report);
            }
            else if (logClean)
            {
                Debug.Log($"[DMLootChestIdTool] {scene.name}: loot chests OK.");
            }
        }

        private static IEnumerable<DMItemCollection> FindChestsInOpenScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                foreach (DMItemCollection chest in FindChests(SceneManager.GetSceneAt(i)))
                    yield return chest;
            }
        }

        private static IEnumerable<DMItemCollection> FindChests(Scene scene) => FindInScene<DMItemCollection>(scene);

        private static IEnumerable<T> FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
                yield break;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                T[] found = roots[r].GetComponentsInChildren<T>(true);
                for (int i = 0; i < found.Length; i++)
                    yield return found[i];
            }
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }
    }
}
