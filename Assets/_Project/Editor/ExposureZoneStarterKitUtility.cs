using Project.Companions;
using Project.Survival.Exposure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    public static class ExposureZoneStarterKitUtility
    {
        private const string ScenePath = "Assets/Pioneer v1.5.unity";
        private const string KitRootName = "ExposureStarterKit";

        private static readonly ExposureZoneKind[] StarterKinds =
        {
            ExposureZoneKind.RadiationFlat,
            ExposureZoneKind.ThermalCold,
            ExposureZoneKind.ThermalHeat,
            ExposureZoneKind.SulfurField,
            ExposureZoneKind.VolcanoCaldera,
            ExposureZoneKind.MixedHazard,
            ExposureZoneKind.ShelterSafe
        };

        [MenuItem(SurvivalPioneerEditorMenus.PlaceExposureStarterKit, false, 20)]
        public static void PlaceStarterKitInOpenScene()
        {
            PlaceStarterKit(loadPioneerIfNeeded: false);
        }

        [MenuItem(SurvivalPioneerEditorMenus.PlaceExposureStarterKitInPioneer, false, 21)]
        public static void PlaceStarterKitInPioneerScene()
        {
            PlaceStarterKit(loadPioneerIfNeeded: true);
        }

        private static void PlaceStarterKit(bool loadPioneerIfNeeded)
        {
            if (loadPioneerIfNeeded)
            {
                if (!System.IO.File.Exists(ScenePath))
                {
                    EditorUtility.DisplayDialog("Exposure Starter Kit", $"Scene not found:\n{ScenePath}", "OK");
                    return;
                }

                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Exposure Starter Kit", "No active scene.", "OK");
                return;
            }

            Vector3 anchor = FindPlayerAnchor();
            Transform kitRoot = FindOrCreateKitRoot();

            int created = 0;
            int placed = 0;
            Vector3 spacing = new Vector3(30f, 0f, 30f);
            Vector3 gridOrigin = anchor + new Vector3(35f, 0f, -45f);

            for (int i = 0; i < StarterKinds.Length; i++)
            {
                ExposureZoneKind kind = StarterKinds[i];
                ExposureZoneProfile profile = ExposureZoneEditorUtility.EnsureProfileAsset(kind);
                Vector3 boxSize = GetBoxSize(kind);
                Vector3 position = gridOrigin + new Vector3((i % 3) * spacing.x, 0f, (i / 3) * spacing.z);

                string prefabPath = $"{ExposureZoneEditorUtility.PrefabFolder}/{ExposureZoneEditorUtility.MakeSafeName(profile.displayName)}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    GameObject temp = ExposureZoneEditorUtility.CreateZoneObject(profile, boxSize);
                    prefab = ExposureZoneEditorUtility.SaveZonePrefab(temp, profile);
                    Object.DestroyImmediate(temp);
                    created++;
                }

                string instanceName = ExposureZoneEditorUtility.MakeSafeName(profile.displayName);
                Transform existing = kitRoot.Find(instanceName);
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);

                GameObject instance = ExposureZoneEditorUtility.PlacePrefabInScene(prefab, position, kitRoot);
                instance.name = instanceName;
                placed++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = kitRoot.gameObject;

            EditorUtility.DisplayDialog(
                "Exposure Starter Kit",
                $"Placed {placed} zones in '{scene.name}'.\n" +
                $"New prefabs created: {created}\n" +
                $"Root object: {KitRootName}\n" +
                $"Grid anchor near player at {anchor}.",
                "OK");
        }

        private static Vector3 GetBoxSize(ExposureZoneKind kind)
        {
            return kind switch
            {
                ExposureZoneKind.ShelterSafe => new Vector3(18f, 8f, 18f),
                ExposureZoneKind.VolcanoCaldera => new Vector3(28f, 10f, 28f),
                _ => new Vector3(22f, 8f, 22f)
            };
        }

        private static Transform FindOrCreateKitRoot()
        {
            GameObject existing = GameObject.Find(KitRootName);
            if (existing != null)
                return existing.transform;

            GameObject root = new GameObject(KitRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Exposure Starter Kit Root");
            return root.transform;
        }

        private static Vector3 FindPlayerAnchor()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                return player.transform.position;

            PioneerCompanionAgent[] companions = Object.FindObjectsByType<PioneerCompanionAgent>();
            if (companions != null && companions.Length > 0 && companions[0] != null)
                return companions[0].transform.position;

            return Vector3.zero;
        }
    }
}
