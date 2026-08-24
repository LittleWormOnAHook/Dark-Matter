#if UNITY_EDITOR
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    /// <summary>
    /// Wires NavMeshSurface + baked NavMeshData assets onto Gaia terrain chunks in the active scene.
    /// </summary>
    public static class SyncGaiaTerrainNavMeshUtility
    {
        private const string TerrainScenesRoot =
            "Assets/Gaia User Data/Sessions/DM Genesis/Terrain Scenes";

        [MenuItem("Dark Matter Genesis/World/Sync NavMesh Surfaces To Gaia Terrains")]
        public static void SyncActiveSceneTerrains()
        {
            Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int wired = 0;
            int missingBake = 0;

            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null || terrain.terrainData == null)
                    continue;

                string terrainName = terrain.name;
                if (!TryFindNavMeshAsset(terrainName, out NavMeshData navMeshData))
                {
                    missingBake++;
                    continue;
                }

                NavMeshSurface surface = terrain.GetComponent<NavMeshSurface>();
                if (surface == null)
                    surface = terrain.gameObject.AddComponent<NavMeshSurface>();

                surface.collectObjects = CollectObjects.Volume;
                surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
                surface.layerMask = ~0;
                surface.agentTypeID = 0;
                surface.center = new Vector3(0f, 2f, 0f);
                Vector3 size = terrain.terrainData.size;
                surface.size = new Vector3(size.x, Mathf.Max(size.y, 10f), size.z);
                surface.overrideTileSize = false;
                surface.overrideVoxelSize = false;
                surface.buildHeightMesh = false;
                surface.navMeshData = navMeshData;

                EditorUtility.SetDirty(surface);
                EditorUtility.SetDirty(terrain.gameObject);
                wired++;
            }

            if (wired > 0)
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log(
                $"Sync NavMesh: wired {wired} terrain chunk(s). " +
                $"{missingBake} terrain(s) have no baked NavMesh asset yet (still need bake).");
        }

        private static bool TryFindNavMeshAsset(string terrainName, out NavMeshData navMeshData)
        {
            navMeshData = null;
            if (string.IsNullOrEmpty(terrainName))
                return false;

            string folder = Path.Combine(TerrainScenesRoot, terrainName).Replace('\\', '/');
            string assetPath = $"{folder}/NavMesh-{terrainName}.asset";
            navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath);
            return navMeshData != null;
        }
    }
}
#endif
