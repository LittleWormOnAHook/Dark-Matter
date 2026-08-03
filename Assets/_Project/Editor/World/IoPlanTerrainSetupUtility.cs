#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.World
{
    /// <summary>
    /// Creates a lightweight Io plan TerrainData shell only (2048 x 2048, 1000 m).
    /// Does NOT apply heightmaps (avoids editor crashes from huge SetHeights).
    /// Does NOT mutate or assign the Pioneer prototype scene Terrain.
    /// Heightmaps are disk files: Io_Plan_Heightmap_Unity.png/.raw at 2048×2048 — use Import Raw… manually.
    /// </summary>
    public static class IoPlanTerrainSetupUtility
    {
        private const string TerrainFolder = "Assets/_Project/World/Terrain";
        private const string TerrainDataPath = TerrainFolder + "/Io_Plan_Terrain.asset";
        private const float TerrainWidth = 2048f;
        private const float TerrainLength = 2048f;
        private const float TerrainHeight = 1000f;
        // Unity TerrainData heightmapResolution must be 2^n+1 when sculpting in-engine.
        private const int HeightmapResolution = 2049;
        private const int DetailResolution = 1024;
        private const int DetailResolutionPerPatch = 16;
        private const int ControlTextureResolution = 1024;
        private const int BaseMapResolution = 1024;

        [MenuItem(SurvivalPioneerEditorMenus.Scene + "Setup Io Plan Terrain Shell (2048 / 1000m)", false, 20)]
        public static void SetupIoPlanTerrainShell()
        {
            TerrainData terrainData = EnsureTerrainDataShell(out bool created);

            Selection.activeObject = terrainData;
            EditorGUIUtility.PingObject(terrainData);

            string verb = created ? "Created" : "Updated";
            Debug.Log(
                $"{verb} Io plan TerrainData SHELL at '{TerrainDataPath}': " +
                $"{TerrainWidth:0} x {TerrainLength:0}, height {TerrainHeight:0}m, " +
                $"heightmapResolution {HeightmapResolution} (flat). " +
                "Heights NOT applied — import Io_Plan_Heightmap_Unity.raw via Terrain Import Raw. " +
                "Did not modify any scene Terrain.");

            EditorUtility.DisplayDialog(
                "Io Plan Terrain Shell",
                $"{verb} separate TerrainData shell (flat — no height blast):\n\n" +
                $"World size: {TerrainWidth:0} x {TerrainLength:0}, height {TerrainHeight:0}m\n" +
                $"TerrainData.heightmapResolution: {HeightmapResolution} (2^n+1)\n\n" +
                "Disk elevation maps (2048×2048):\n" +
                "  Terrain/Io_Plan_Heightmap_Unity.png\n" +
                "  Terrain/Io_Plan_Heightmap_Unity.raw\n\n" +
                "See Terrain/Io_Plan_Heightmap_Unity_Import.txt\n" +
                "Pioneer scene Terrain was NOT modified.",
                "OK");
        }

        /// <summary>Creates/updates flat TerrainData shell only. Never assigns to scene Terrains. Never SetHeights.</summary>
        public static TerrainData EnsureTerrainDataShell(out bool created)
        {
            EnsureTerrainFolder();

            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            created = false;
            if (terrainData == null)
            {
                terrainData = new TerrainData();
                AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
                created = true;
            }

            Undo.RegisterCompleteObjectUndo(terrainData, "Setup Io Plan Terrain Shell");

            terrainData.heightmapResolution = HeightmapResolution;
            terrainData.size = new Vector3(TerrainWidth, TerrainHeight, TerrainLength);
            terrainData.SetDetailResolution(DetailResolution, DetailResolutionPerPatch);
            terrainData.alphamapResolution = ControlTextureResolution;
            terrainData.baseMapResolution = BaseMapResolution;

            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets();
            return terrainData;
        }

        private static void EnsureTerrainFolder()
        {
            if (!AssetDatabase.IsValidFolder(TerrainFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/World"))
                    AssetDatabase.CreateFolder("Assets/_Project", "World");
                AssetDatabase.CreateFolder("Assets/_Project/World", "Terrain");
            }
        }
    }
}
#endif
