using Project.EditorTools;
using Project.Survival.Exposure;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public class ExposureZonePrefabCreatorWindow : EditorWindow
    {
        private ExposureZoneKind zoneKind = ExposureZoneKind.RadiationFlat;
        private ExposureZoneProfile profile;
        private Vector3 boxSize = new Vector3(24f, 8f, 24f);
        private bool createProfileAsset = true;
        private bool placeInOpenScene = true;
        private bool playAmbientLoop;

        [MenuItem(DarkMatterGenesisEditorMenus.ExposureZonePrefabCreator, false, 12)]
        public static void ShowWindow()
        {
            GetWindow<ExposureZonePrefabCreatorWindow>("Exposure Zone Creator").minSize = new Vector2(460f, 520f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Exposure Zone Prefab Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Creates an ExposureZoneProfile asset and a trigger volume prefab with optional pulse timing, " +
                "companion mitigation rules, and player/companion debuffs.",
                MessageType.Info);

            EditorGUILayout.Space(8f);
            zoneKind = (ExposureZoneKind)EditorGUILayout.EnumPopup("Zone Kind", zoneKind);
            profile = (ExposureZoneProfile)EditorGUILayout.ObjectField("Custom Profile (optional)", profile, typeof(ExposureZoneProfile), false);
            boxSize = EditorGUILayout.Vector3Field("Collider Size", boxSize);
            createProfileAsset = EditorGUILayout.Toggle("Save Profile Asset", createProfileAsset);
            placeInOpenScene = EditorGUILayout.Toggle("Place In Open Scene", placeInOpenScene);
            playAmbientLoop = EditorGUILayout.Toggle("Play Ambient Loop", playAmbientLoop);

            EditorGUILayout.Space(12f);
            if (GUILayout.Button("Create Exposure Zone", GUILayout.Height(42f)))
                CreateExposureZone();
        }

        private void CreateExposureZone()
        {
            ExposureZoneProfile resolvedProfile = profile;
            if (resolvedProfile == null)
            {
                resolvedProfile = ExposureZoneProfilePresets.CreatePreset(zoneKind);
                if (createProfileAsset)
                    resolvedProfile = SaveProfileAsset(resolvedProfile);
            }

            GameObject zoneObject = new GameObject(resolvedProfile.displayName);
            BoxCollider collider = zoneObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = boxSize;

            ExposureZoneVolume volume = zoneObject.AddComponent<ExposureZoneVolume>();
            SerializedObject serializedVolume = new SerializedObject(volume);
            serializedVolume.FindProperty("profile").objectReferenceValue = resolvedProfile;
            serializedVolume.FindProperty("playAmbientLoop").boolValue = playAmbientLoop;
            serializedVolume.ApplyModifiedPropertiesWithoutUndo();

            string prefabFolder = ProjectAssetPaths.PrefabsEnvironmentExposure;
            CraftingEditorUtility.EnsureFolder(prefabFolder);

            string safeName = MakeSafeName(resolvedProfile.displayName);
            string prefabPath = $"{prefabFolder}/{safeName}.prefab";
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
            PrefabUtility.SaveAsPrefabAsset(zoneObject, prefabPath);

            if (placeInOpenScene)
            {
                Selection.activeGameObject = zoneObject;
                EditorGUIUtility.PingObject(zoneObject);
            }
            else
            {
                DestroyImmediate(zoneObject);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Exposure Zone Created", $"Saved prefab:\n{prefabPath}", "OK");
        }

        private static ExposureZoneProfile SaveProfileAsset(ExposureZoneProfile profile)
        {
            string folder = "Assets/_Project/Data/Exposure";
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
                AssetDatabase.CreateFolder("Assets/_Project", "Data");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Exposure");

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{MakeSafeName(profile.displayName)}.asset");
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        private static string MakeSafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "ExposureZone";

            return value.Replace(' ', '_');
        }
    }
}
