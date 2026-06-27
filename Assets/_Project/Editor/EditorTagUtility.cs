using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Keeps ProjectSettings/TagManager.asset free of built-in duplicates (e.g. Player) that spam the console.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorTagUtility
    {
        private static readonly HashSet<string> BuiltInTags = new HashSet<string>
        {
            "Untagged",
            "Respawn",
            "Finish",
            "EditorOnly",
            "MainCamera",
            "Player",
            "GameController"
        };

        private static readonly string[] ProjectTags =
        {
            "Enemy",
            "Building",
            "Animal",
            "Dirt"
        };

        static EditorTagUtility()
        {
            EditorApplication.delayCall += SanitizeTagManager;
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Fix Tag Manager", false, 10)]
        public static void FixTagManagerMenu()
        {
            if (SanitizeTagManager(forceLog: true))
            {
                EditorUtility.DisplayDialog(
                    "Tag Manager",
                    "Removed duplicate or built-in tags from Tag Manager.\n\nThe Player tag remains available as a Unity built-in tag.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Tag Manager",
                    "Tag Manager is already clean.",
                    "OK");
            }
        }

        private static bool SanitizeTagManager(bool forceLog = false)
        {
            SerializedObject tagManager = GetTagManager();
            if (tagManager == null)
                return false;

            SerializedProperty tagsProperty = tagManager.FindProperty("tags");
            if (tagsProperty == null || !tagsProperty.isArray)
                return false;

            bool changed = false;
            HashSet<string> seen = new HashSet<string>();

            for (int i = tagsProperty.arraySize - 1; i >= 0; i--)
            {
                string tag = tagsProperty.GetArrayElementAtIndex(i).stringValue;
                if (string.IsNullOrWhiteSpace(tag) ||
                    tag == "Default" ||
                    BuiltInTags.Contains(tag) ||
                    !seen.Add(tag))
                {
                    tagsProperty.DeleteArrayElementAtIndex(i);
                    changed = true;
                }
            }

            for (int i = 0; i < ProjectTags.Length; i++)
            {
                string tag = ProjectTags[i];
                if (TagExists(tagsProperty, tag) || BuiltInTags.Contains(tag))
                    continue;

                tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
                tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tag;
                changed = true;
            }

            if (!changed)
                return false;

            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();

            if (forceLog)
                Debug.Log("EditorTagUtility: sanitized Tag Manager (removed built-in/duplicate tags).");

            return true;
        }

        public static bool TagExists(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            if (BuiltInTags.Contains(tag))
                return true;

            SerializedObject tagManager = GetTagManager();
            SerializedProperty tagsProperty = tagManager?.FindProperty("tags");
            return tagsProperty != null && TagExists(tagsProperty, tag);
        }

        public static void EnsureTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || TagExists(tag))
                return;

            SerializedObject tagManager = GetTagManager();
            SerializedProperty tagsProperty = tagManager?.FindProperty("tags");
            if (tagsProperty == null)
                return;

            tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
            tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private static bool TagExists(SerializedProperty tagsProperty, string tag)
        {
            for (int i = 0; i < tagsProperty.arraySize; i++)
            {
                if (tagsProperty.GetArrayElementAtIndex(i).stringValue == tag)
                    return true;
            }

            return false;
        }

        private static SerializedObject GetTagManager()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
                return null;

            return new SerializedObject(assets[0]);
        }
    }
}
