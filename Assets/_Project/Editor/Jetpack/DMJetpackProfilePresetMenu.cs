using Project.Features.Jetpack;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Jetpack
{
    public static class DMJetpackProfilePresetMenu
    {
        [MenuItem("Tools/Dark Matter Genesis/Jetpack/Presets/Apply Smooth (Selected Player)")]
        public static void ApplySmooth() => ApplyPreset(DMJetpackProfilePresets.SmoothPath, "Smooth");

        [MenuItem("Tools/Dark Matter Genesis/Jetpack/Presets/Apply Arcade (Selected Player)")]
        public static void ApplyArcade() => ApplyPreset(DMJetpackProfilePresets.ArcadePath, "Arcade");

        [MenuItem("Tools/Dark Matter Genesis/Jetpack/Presets/Apply Starfield (Selected Player)")]
        public static void ApplyStarfield() => ApplyPreset(DMJetpackProfilePresets.StarfieldPath, "Starfield");

        [MenuItem("Tools/Dark Matter Genesis/Jetpack/Presets/Apply Smooth To Player_v7 Prefab")]
        public static void ApplySmoothToPrefab() => ApplyPresetToPrefab(DMJetpackProfilePresets.SmoothPath, "Smooth");

        [MenuItem("Tools/Dark Matter Genesis/Jetpack/Presets/Apply Arcade To Player_v7 Prefab")]
        public static void ApplyArcadeToPrefab() => ApplyPresetToPrefab(DMJetpackProfilePresets.ArcadePath, "Arcade");

        [MenuItem("Tools/Dark Matter Genesis/Jetpack/Presets/Apply Starfield To Player_v7 Prefab")]
        public static void ApplyStarfieldToPrefab() => ApplyPresetToPrefab(DMJetpackProfilePresets.StarfieldPath, "Starfield");

        private static void ApplyPreset(string assetPath, string presetLabel)
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog(
                    "Jetpack Preset",
                    "Select the player root in the Hierarchy (Player_v7).",
                    "OK");
                return;
            }

            DMJetpackProfile profile = LoadPreset(assetPath, presetLabel);
            if (profile == null)
                return;

            if (!AssignProfileOnHierarchy(selected.transform, profile))
            {
                EditorUtility.DisplayDialog(
                    "Jetpack Preset",
                    "No DMJetpackController found on the selection or its parents/children.",
                    "OK");
                return;
            }

            EditorUtility.SetDirty(selected);
            Debug.Log($"[Jetpack] Applied '{presetLabel}' preset to '{selected.name}'.");
        }

        private static void ApplyPresetToPrefab(string assetPath, string presetLabel)
        {
            DMJetpackProfile profile = LoadPreset(assetPath, presetLabel);
            if (profile == null)
                return;

            const string prefabPath = ProjectAssetPaths.PrefabsPlayers + "/Player_v7.prefab";
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                EditorUtility.DisplayDialog("Jetpack Preset", $"Could not load {prefabPath}", "OK");
                return;
            }

            AssignProfileOnHierarchy(prefabRoot.transform, profile);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Jetpack] Applied '{presetLabel}' preset to Player_v7 prefab.");
        }

        private static DMJetpackProfile LoadPreset(string assetPath, string presetLabel)
        {
            DMJetpackProfile profile = AssetDatabase.LoadAssetAtPath<DMJetpackProfile>(assetPath);
            if (profile != null)
                return profile;

            EditorUtility.DisplayDialog(
                "Jetpack Preset",
                $"Preset asset not found:\n{assetPath}\n\nReimport the Jetpack/Data/Presets folder.",
                "OK");
            return null;
        }

        private static bool AssignProfileOnHierarchy(Transform root, DMJetpackProfile profile)
        {
            bool assigned = false;

            DMJetpackController[] controllers = root.GetComponentsInChildren<DMJetpackController>(true);
            for (int i = 0; i < controllers.Length; i++)
            {
                SerializedObject so = new SerializedObject(controllers[i]);
                so.FindProperty("profile").objectReferenceValue = profile;
                so.ApplyModifiedPropertiesWithoutUndo();
                assigned = true;
            }

            DMJetpackAnimatorDriver[] drivers = root.GetComponentsInChildren<DMJetpackAnimatorDriver>(true);
            for (int i = 0; i < drivers.Length; i++)
            {
                SerializedObject so = new SerializedObject(drivers[i]);
                so.FindProperty("profile").objectReferenceValue = profile;
                so.ApplyModifiedPropertiesWithoutUndo();
                assigned = true;
            }

            DMJetpackThrusterVfx[] thrusters = root.GetComponentsInChildren<DMJetpackThrusterVfx>(true);
            for (int i = 0; i < thrusters.Length; i++)
            {
                SerializedObject so = new SerializedObject(thrusters[i]);
                so.FindProperty("profile").objectReferenceValue = profile;
                so.ApplyModifiedPropertiesWithoutUndo();
                assigned = true;
            }

            return assigned;
        }
    }
}
