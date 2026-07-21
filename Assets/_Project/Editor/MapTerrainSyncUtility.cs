#if UNITY_EDITOR
using Project.EditorTools;
using Project.Map;
using Project.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.Map
{
    /// <summary>
    /// Syncs WorldMapProvider bounds and MapUI minimap span to the active scene terrain size.
    /// </summary>
    public static class MapTerrainSyncUtility
    {
        private const float ReferenceTerrainSpan = 512f;
        private const float ReferenceMinimapSpan = 96f;
        private const string TerrainMapSnapshotPath = "Assets/_Project/Textures/UI/TerrainMapSnapshot.png";

        [MenuItem(SurvivalPioneerEditorMenus.Scene + "Sync Map To Terrain", false, 11)]
        public static void SyncActiveSceneMapToTerrain()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Map Sync", "Open a scene first.", "OK");
                return;
            }

            if (!TryGetCombinedTerrainBounds(out Bounds bounds, out Terrain primaryTerrain))
            {
                EditorUtility.DisplayDialog(
                    "Map Sync",
                    "No active Terrain with TerrainData found in the open scene.",
                    "OK");
                return;
            }

            int changes = 0;
            WorldMapProvider provider = Object.FindAnyObjectByType<WorldMapProvider>();
            if (provider == null && primaryTerrain != null)
            {
                Undo.AddComponent<WorldMapProvider>(primaryTerrain.gameObject);
                provider = primaryTerrain.GetComponent<WorldMapProvider>();
                changes++;
            }

            if (provider != null)
            {
                SerializedObject serializedProvider = new SerializedObject(provider);
                serializedProvider.FindProperty("terrain").objectReferenceValue = primaryTerrain;
                serializedProvider.FindProperty("useTerrainBounds").boolValue = true;
                serializedProvider.FindProperty("preferTerrainGeneratedMap").boolValue = true;
                serializedProvider.FindProperty("buildTerrainTextureAtRuntime").boolValue = true;
                serializedProvider.FindProperty("manualWorldSize").vector2Value = new Vector2(bounds.size.x, bounds.size.z);
                serializedProvider.FindProperty("manualWorldOrigin").vector3Value = bounds.min;
                int resolution = ResolveMapResolution(bounds.size);
                serializedProvider.FindProperty("mapTextureResolution").intValue = resolution;
                serializedProvider.ApplyModifiedPropertiesWithoutUndo();
                provider.RefreshWorldBounds();
                EditorUtility.SetDirty(provider);
                changes++;
            }

            foreach (MapUI mapUi in Object.FindObjectsByType<MapUI>(FindObjectsInactive.Include))
            {
                SerializedObject serializedMapUi = new SerializedObject(mapUi);
                serializedMapUi.FindProperty("autoScaleMinimapToTerrain").boolValue = true;
                float scaledSpan = ReferenceMinimapSpan * (Mathf.Max(bounds.size.x, bounds.size.z) / ReferenceTerrainSpan);
                serializedMapUi.FindProperty("minimapWorldSpan").floatValue = Mathf.Clamp(scaledSpan, 40f, 420f);
                serializedMapUi.ApplyModifiedPropertiesWithoutUndo();
                mapUi.SyncMinimapSpanFromWorldBounds();
                EditorUtility.SetDirty(mapUi);
                changes++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log(
                $"Map sync complete for '{scene.name}'. Terrain bounds: {bounds.size.x:0.#} x {bounds.size.z:0.#} " +
                $"(min {bounds.min.x:0.#}, {bounds.min.z:0.#}). Changes: {changes}.");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Scene + "Bake Terrain Map Snapshot", false, 12)]
        public static void BakeTerrainMapSnapshot()
        {
            if (!TryGetCombinedTerrainBounds(out Bounds bounds, out Terrain primaryTerrain))
            {
                EditorUtility.DisplayDialog(
                    "Bake Terrain Map",
                    "No active Terrain with TerrainData found in the open scene.",
                    "OK");
                return;
            }

            WorldMapProvider provider = Object.FindAnyObjectByType<WorldMapProvider>();
            if (provider == null && primaryTerrain != null)
                provider = primaryTerrain.GetComponent<WorldMapProvider>() ?? primaryTerrain.gameObject.AddComponent<WorldMapProvider>();

            if (provider == null)
            {
                EditorUtility.DisplayDialog("Bake Terrain Map", "Could not resolve a WorldMapProvider.", "OK");
                return;
            }

            SerializedObject serializedProvider = new SerializedObject(provider);
            serializedProvider.FindProperty("terrain").objectReferenceValue = primaryTerrain;
            serializedProvider.FindProperty("preferTerrainGeneratedMap").boolValue = true;
            serializedProvider.FindProperty("buildTerrainTextureAtRuntime").boolValue = true;
            int resolution = ResolveMapResolution(bounds.size);
            serializedProvider.FindProperty("mapTextureResolution").intValue = resolution;
            serializedProvider.ApplyModifiedPropertiesWithoutUndo();
            provider.RefreshWorldBounds();

            if (!provider.TryBakeActiveTerrainMap(out Texture2D bakedTexture, "TerrainMapSnapshot"))
            {
                EditorUtility.DisplayDialog("Bake Terrain Map", "Failed to bake terrain map texture.", "OK");
                return;
            }

            EnsureSnapshotFolderExists();
            byte[] pngBytes = bakedTexture.EncodeToPNG();
            Object.DestroyImmediate(bakedTexture);
            System.IO.File.WriteAllBytes(TerrainMapSnapshotPath, pngBytes);
            AssetDatabase.ImportAsset(TerrainMapSnapshotPath);

            TextureImporter importer = AssetImporter.GetAtPath(TerrainMapSnapshotPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Texture2D savedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainMapSnapshotPath);
            serializedProvider.FindProperty("mapTextureOverride").objectReferenceValue = null;
            serializedProvider.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(provider);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log(
                $"Terrain map snapshot saved to '{TerrainMapSnapshotPath}' " +
                $"({resolution}x{resolution}, bounds {bounds.size.x:0.#} x {bounds.size.z:0.#}). " +
                (savedTexture != null
                    ? "Asset imported for editor preview."
                    : "Asset import pending.") +
                " Runtime still uses live terrain bake when preferTerrainGeneratedMap is enabled.");
        }

        private static void EnsureSnapshotFolderExists()
        {
            const string folder = "Assets/_Project/Textures/UI";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Textures"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Textures");
                AssetDatabase.CreateFolder("Assets/_Project/Textures", "UI");
            }
        }

        public static bool TryGetCombinedTerrainBounds(out Bounds bounds, out Terrain primaryTerrain)
        {
            bounds = default;
            primaryTerrain = null;
            Terrain[] terrains = Object.FindObjectsByType<Terrain>();
            bool found = false;

            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null || !terrain.gameObject.activeInHierarchy || terrain.terrainData == null)
                    continue;

                Vector3 size = terrain.terrainData.size;
                Vector3 origin = terrain.transform.position;
                Bounds terrainBounds = new Bounds(origin + size * 0.5f, size);

                if (!found)
                {
                    bounds = terrainBounds;
                    primaryTerrain = terrain;
                    found = true;
                    continue;
                }

                bounds.Encapsulate(terrainBounds.min);
                bounds.Encapsulate(terrainBounds.max);
            }

            return found;
        }

        private static int ResolveMapResolution(Vector3 terrainSize)
        {
            float maxDimension = Mathf.Max(terrainSize.x, terrainSize.z);
            if (maxDimension > 1400f)
                return 512;
            if (maxDimension > 700f)
                return 384;
            return 256;
        }
    }
}
#endif
