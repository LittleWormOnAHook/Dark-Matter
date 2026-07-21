using Project.Survival.Exposure;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class ExposureZoneEditorUtility
    {
        public const string ProfileFolder = "Assets/_Project/Data/Exposure";
        public const string PrefabFolder = "Assets/_Project/Prefabs/Environment/Exposure";

        public static ExposureZoneProfile EnsureProfileAsset(ExposureZoneKind kind, bool overwriteExisting = false)
        {
            EnsureFolders();
            string assetName = GetProfileAssetName(kind);
            string path = $"{ProfileFolder}/{assetName}.asset";
            ExposureZoneProfile existing = AssetDatabase.LoadAssetAtPath<ExposureZoneProfile>(path);
            if (existing != null && !overwriteExisting)
                return existing;

            ExposureZoneProfile profile = ExposureZoneProfilePresets.CreatePreset(kind);
            if (existing != null)
            {
                EditorUtility.CopySerialized(profile, existing);
                Object.DestroyImmediate(profile);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            return profile;
        }

        public static GameObject CreateZoneObject(ExposureZoneProfile profile, Vector3 boxSize)
        {
            GameObject zoneObject = new GameObject(profile != null ? profile.displayName : "ExposureZone");
            BoxCollider collider = zoneObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = boxSize;

            ExposureZoneVolume volume = zoneObject.AddComponent<ExposureZoneVolume>();
            SerializedObject serializedVolume = new SerializedObject(volume);
            serializedVolume.FindProperty("profile").objectReferenceValue = profile;
            serializedVolume.ApplyModifiedPropertiesWithoutUndo();
            return zoneObject;
        }

        public static GameObject SaveZonePrefab(GameObject zoneObject, ExposureZoneProfile profile)
        {
            EnsureFolders();
            string safeName = MakeSafeName(profile != null ? profile.displayName : zoneObject.name);
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{PrefabFolder}/{safeName}.prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(zoneObject, prefabPath);
            return prefab;
        }

        public static GameObject PlacePrefabInScene(GameObject prefab, Vector3 position, Transform parent)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(position, Quaternion.identity);
            if (parent != null)
                instance.transform.SetParent(parent, true);
            return instance;
        }

        public static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
                AssetDatabase.CreateFolder("Assets/_Project", "Data");
            if (!AssetDatabase.IsValidFolder(ProfileFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Exposure");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Environment"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Environment");
            }
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs/Environment", "Exposure");
        }

        public static string GetProfileAssetName(ExposureZoneKind kind)
        {
            return kind switch
            {
                ExposureZoneKind.RadiationFlat => "Exposure_Radiation_Flat",
                ExposureZoneKind.ThermalCold => "Exposure_Cold_Basin",
                ExposureZoneKind.ThermalHeat => "Exposure_Heat_Vent",
                ExposureZoneKind.SulfurField => "Exposure_Sulfur_Field",
                ExposureZoneKind.VolcanoCaldera => "Exposure_Volcano_Caldera",
                ExposureZoneKind.MixedHazard => "Exposure_Mixed_Hazard",
                ExposureZoneKind.ShelterSafe => "Exposure_Shelter_Safe",
                _ => "Exposure_Custom"
            };
        }

        public static string MakeSafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "ExposureZone";
            return value.Replace(' ', '_');
        }
    }
}
