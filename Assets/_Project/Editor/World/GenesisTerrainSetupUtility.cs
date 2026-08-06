#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.World
{
    /// <summary>
    /// Builds Genesis_Terrain.asset the same way as Io_Plan_Terrain.asset:
    /// TerrainData shell + heights from Genesis_Heightmap_Unity.raw (safe at 1025²).
    /// Does not assign or modify the Pioneer scene Terrain.
    /// </summary>
    public static class GenesisTerrainSetupUtility
    {
        private const string TerrainFolder = "Assets/_Project/World/Terrain";
        private const string TerrainDataPath = TerrainFolder + "/Genesis_Terrain.asset";
        private const string RawPath = TerrainFolder + "/Genesis_Heightmap_Unity.raw";

        private const float TerrainWidth = 1024f;
        private const float TerrainLength = 1024f;
        private const float TerrainHeight = 100f;
        private const int HeightmapResolution = 1025;
        private const int DetailResolution = 512;
        private const int DetailResolutionPerPatch = 16;
        private const int ControlTextureResolution = 512;
        private const int BaseMapResolution = 512;

        [MenuItem(SurvivalPioneerEditorMenus.Scene + "Setup Genesis Terrain (1024 / 100m + RAW heights)", false, 21)]
        public static void SetupGenesisTerrain()
        {
            if (!TryBuild(out TerrainData terrainData, out string error))
            {
                EditorUtility.DisplayDialog("Genesis Terrain", error, "OK");
                return;
            }

            Selection.activeObject = terrainData;
            EditorGUIUtility.PingObject(terrainData);

            Debug.Log(
                $"Built Genesis TerrainData at '{TerrainDataPath}': " +
                $"{TerrainWidth:0} x {TerrainLength:0}, height {TerrainHeight:0}m, " +
                $"heightmapResolution {HeightmapResolution}. Heights applied from {RawPath}. " +
                "Did not modify any scene Terrain.");

            EditorUtility.DisplayDialog(
                "Genesis Terrain",
                "Built Genesis_Terrain.asset like Io_Plan_Terrain:\n\n" +
                $"World size: {TerrainWidth:0} x {TerrainLength:0}, height {TerrainHeight:0}m\n" +
                $"Heightmap Resolution: {HeightmapResolution}\n" +
                $"Heights: {RawPath}\n\n" +
                "Assign this TerrainData to a Terrain component on an Io/Genesis scene object.\n" +
                "Pioneer scene Terrain was NOT modified.",
                "OK");
        }

        public static bool TryBuild(out TerrainData terrainData, out string error)
        {
            terrainData = null;
            error = null;

            EnsureTerrainFolder();

            string absRaw = Path.GetFullPath(Path.Combine(
                Directory.GetParent(Application.dataPath)!.FullName,
                RawPath.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(absRaw))
            {
                error = $"Missing heightmap RAW:\n{RawPath}";
                return false;
            }

            byte[] bytes = File.ReadAllBytes(absRaw);
            int expected = HeightmapResolution * HeightmapResolution * 2;
            if (bytes.Length != expected)
            {
                error =
                    $"RAW size mismatch.\nExpected {expected} bytes ({HeightmapResolution}² × 16-bit).\n" +
                    $"Got {bytes.Length} bytes.";
                return false;
            }

            float[,] heights = new float[HeightmapResolution, HeightmapResolution];
            for (int y = 0; y < HeightmapResolution; y++)
            {
                for (int x = 0; x < HeightmapResolution; x++)
                {
                    int i = (y * HeightmapResolution + x) * 2;
                    ushort sample = (ushort)(bytes[i] | (bytes[i + 1] << 8));
                    heights[y, x] = sample / 65535f;
                }
            }

            terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (terrainData == null)
            {
                terrainData = new TerrainData();
                AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
            }

            Undo.RegisterCompleteObjectUndo(terrainData, "Setup Genesis Terrain");

            terrainData.heightmapResolution = HeightmapResolution;
            terrainData.size = new Vector3(TerrainWidth, TerrainHeight, TerrainLength);
            terrainData.SetDetailResolution(DetailResolution, DetailResolutionPerPatch);
            terrainData.alphamapResolution = ControlTextureResolution;
            terrainData.baseMapResolution = BaseMapResolution;

            TerrainData ioPlan = AssetDatabase.LoadAssetAtPath<TerrainData>(
                TerrainFolder + "/Io_Plan_Terrain.asset");
            if (ioPlan != null && ioPlan.terrainLayers != null && ioPlan.terrainLayers.Length > 0)
                terrainData.terrainLayers = ioPlan.terrainLayers;

            terrainData.SetHeights(0, 0, heights);

            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void EnsureTerrainFolder()
        {
            if (AssetDatabase.IsValidFolder(TerrainFolder))
                return;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/World"))
                AssetDatabase.CreateFolder("Assets/_Project", "World");
            AssetDatabase.CreateFolder("Assets/_Project/World", "Terrain");
        }
    }
}
#endif
