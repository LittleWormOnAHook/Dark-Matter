using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Gaia;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes TLM impostor scenes for the 16 DM Genesis tiles. Does not use Gaia Pro
/// Create Impostors (that path assumed tiles lived in v1.6 and wiped it).
/// Menu: Dark Matter Genesis / World / Bake TLM Impostors
/// </summary>
public static class BakeTlmImpostors
{
    const string StoragePath = "Assets/Gaia User Data/Sessions/DM Genesis/TerrainScenes.asset";
    const string ScenesDir = "Assets/Gaia User Data/Sessions/DM Genesis/Terrain Scenes";
    const string AssetDir = ScenesDir + "/Impostors";
    const double ImpostorRangeMeters = 3500d;
    static readonly int[] LodGrids = { 128, 64, 32 };
    static readonly float[] LodScreens = { 0.12f, 0.04f, 0.008f };
    static readonly Regex GridName = new Regex(@"Terrain_(\d+)_(\d+)", RegexOptions.Compiled);
    static readonly Dictionary<Texture2D, Texture2D> ReadableLayerCache = new Dictionary<Texture2D, Texture2D>();

    [MenuItem("Dark Matter Genesis/World/Editor: 4 Terrains + Impostors", false, 41)]
    public static void EditorKeepFourTerrainsAndImpostors()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("4 Terrains + Impostors", "Exit play mode first.", "OK");
            return;
        }

        TerrainLoaderManager tlm = Object.FindFirstObjectByType<TerrainLoaderManager>();
        if (tlm == null)
        {
            EditorUtility.DisplayDialog("4 Terrains + Impostors", "No Terrain Loader Manager in the open scene.", "OK");
            return;
        }

        if (tlm.EditorKeepFourTerrainsAndImpostors())
            Debug.Log("Editor streaming: 4 terrains around Player_v7, impostors kept.");
        else
            EditorUtility.DisplayDialog("4 Terrains + Impostors", "Need Player_v7 in the open scene.", "OK");
    }

    [MenuItem("Dark Matter Genesis/World/Bake TLM Impostors", false, 40)]
    public static void Bake()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Bake TLM Impostors", "Exit play mode first.", "OK");
            return;
        }

        TerrainSceneStorage storage = AssetDatabase.LoadAssetAtPath<TerrainSceneStorage>(StoragePath);
        if (storage == null || storage.m_terrainScenes == null || storage.m_terrainScenes.Count == 0)
        {
            EditorUtility.DisplayDialog("Bake TLM Impostors", "Missing " + StoragePath, "OK");
            return;
        }

        Scene active = EditorSceneManager.GetActiveScene();
        string activePath = active.path;
        Directory.CreateDirectory(AssetDir.Replace("Assets", Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) + "Assets"));
        if (!AssetDatabase.IsValidFolder(AssetDir))
        {
            if (!AssetDatabase.IsValidFolder(ScenesDir))
            {
                EditorUtility.DisplayDialog("Bake TLM Impostors", "Missing " + ScenesDir, "OK");
                return;
            }
            AssetDatabase.CreateFolder(ScenesDir, "Impostors");
        }

        int baked = 0;
        try
        {
            for (int i = 0; i < storage.m_terrainScenes.Count; i++)
            {
                TerrainScene entry = storage.m_terrainScenes[i];
                if (entry == null || string.IsNullOrEmpty(entry.m_scenePath))
                    continue;

                string label = Path.GetFileNameWithoutExtension(entry.m_scenePath);
                EditorUtility.DisplayProgressBar("Bake TLM Impostors", label, (float)i / storage.m_terrainScenes.Count);
                if (BakeOne(entry, active))
                    baked++;
            }
        }
        finally
        {
            ClearReadableCache();
            EditorUtility.ClearProgressBar();
            if (!string.IsNullOrEmpty(activePath))
            {
                Scene restored = EditorSceneManager.GetSceneByPath(activePath);
                if (restored.IsValid() && restored.isLoaded)
                    EditorSceneManager.SetActiveScene(restored);
            }
        }

        TerrainLoaderManager tlm = Object.FindFirstObjectByType<TerrainLoaderManager>();
        if (tlm != null)
        {
            double regular = tlm.GetLoadingRange();
            if (regular <= 0)
                regular = 500;
            tlm.SetLoadingRange(regular, ImpostorRangeMeters, updateLoading: false, forceUpdate: true);
            tlm.TerrainSceneStorage = storage;
            tlm.LoadStorageData();
            EditorUtility.SetDirty(tlm);
        }

        EditorUtility.SetDirty(storage);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Bake TLM Impostors: wrote " + baked + " Impostor_x_y scenes. TLM impostor range " + ImpostorRangeMeters + "m. Do not run Gaia Create Impostors.");
        EditorUtility.DisplayDialog(
            "Bake TLM Impostors",
            "Wrote " + baked + " impostor scenes next to the tiles.\nImpostor range set to " + ImpostorRangeMeters + "m.\nKeep v1.6 open; Gaia Create Impostors was not used.",
            "OK");
    }

    static bool BakeOne(TerrainScene entry, Scene keepActive)
    {
        string terrainPath = entry.m_scenePath.Replace('\\', '/');
        if (!terrainPath.EndsWith(".unity"))
            terrainPath += ".unity";
        if (!File.Exists(terrainPath))
        {
            Debug.LogWarning("Bake TLM Impostors: missing " + terrainPath);
            return false;
        }

        Match grid = GridName.Match(Path.GetFileName(terrainPath));
        string stem = grid.Success ? "Impostor_" + grid.Groups[1].Value + "_" + grid.Groups[2].Value : "Impostor_" + Path.GetFileNameWithoutExtension(terrainPath);
        string impostorScenePath = ScenesDir + "/" + stem + ".unity";
        string meshFolder = AssetDir + "/" + stem;

        Scene terrainScene = EditorSceneManager.OpenScene(terrainPath, OpenSceneMode.Additive);
        Terrain terrain = null;
        GameObject[] roots = terrainScene.GetRootGameObjects();
        for (int r = 0; r < roots.Length && terrain == null; r++)
            terrain = roots[r].GetComponentInChildren<Terrain>(true);

        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogWarning("Bake TLM Impostors: no Terrain in " + terrainPath);
            EditorSceneManager.CloseScene(terrainScene, true);
            if (keepActive.IsValid())
                EditorSceneManager.SetActiveScene(keepActive);
            return false;
        }

        Vector3 worldPos = terrain.transform.position;
        Quaternion worldRot = terrain.transform.rotation;
        TerrainData data = terrain.terrainData;

        if (!AssetDatabase.IsValidFolder(meshFolder))
            AssetDatabase.CreateFolder(AssetDir, stem);

        Texture2D colorMap = BakeColorMap(data, 256);
        string texPath = meshFolder + "/Color.png";
        File.WriteAllBytes(texPath, colorMap.EncodeToPNG());
        Object.DestroyImmediate(colorMap);
        AssetDatabase.ImportAsset(texPath);
        TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer != null)
        {
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        Texture2D importedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        Shader lit = Shader.Find("HDRP/Lit");
        if (lit == null)
            lit = Shader.Find("Hidden/HDRP/FallbackError");
        Material mat = new Material(lit);
        mat.name = stem + "_Lit";
        if (importedTex != null)
        {
            if (mat.HasProperty("_BaseColorMap"))
                mat.SetTexture("_BaseColorMap", importedTex);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", importedTex);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", importedTex);
        }
        AssetDatabase.CreateAsset(mat, meshFolder + "/Lit.mat");

        Mesh[] lodMeshes = new Mesh[LodGrids.Length];
        for (int i = 0; i < LodGrids.Length; i++)
        {
            Mesh mesh = BuildHeightMesh(data, LodGrids[i]);
            mesh.name = stem + "_lod" + LodGrids[i];
            AssetDatabase.CreateAsset(mesh, meshFolder + "/lod" + LodGrids[i] + ".asset");
            lodMeshes[i] = mesh;
        }

        EditorSceneManager.CloseScene(terrainScene, true);
        if (keepActive.IsValid())
            EditorSceneManager.SetActiveScene(keepActive);

        Scene impostorScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject root = new GameObject(stem);
        root.transform.SetPositionAndRotation(worldPos, worldRot);
        SceneManager.MoveGameObjectToScene(root, impostorScene);

        LOD[] lods = new LOD[lodMeshes.Length];
        for (int i = 0; i < lodMeshes.Length; i++)
        {
            GameObject lodGo = new GameObject("LOD" + i + "_" + LodGrids[i]);
            lodGo.transform.SetParent(root.transform, false);
            MeshFilter filter = lodGo.AddComponent<MeshFilter>();
            filter.sharedMesh = lodMeshes[i];
            MeshRenderer renderer = lodGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            lods[i] = new LOD(LodScreens[i], new Renderer[] { renderer });
        }

        LODGroup group = root.AddComponent<LODGroup>();
        group.SetLODs(lods);
        group.RecalculateBounds();

        EditorSceneManager.SaveScene(impostorScene, impostorScenePath);
        EditorSceneManager.CloseScene(impostorScene, true);
        if (keepActive.IsValid())
            EditorSceneManager.SetActiveScene(keepActive);

        entry.m_impostorScenePath = impostorScenePath;
        AddToBuildSettings(impostorScenePath);
        return true;
    }

    static void AddToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        for (int i = 0; i < current.Length; i++)
        {
            if (current[i].path == scenePath)
                return;
        }

        EditorBuildSettingsScene[] next = new EditorBuildSettingsScene[current.Length + 1];
        for (int i = 0; i < current.Length; i++)
            next[i] = current[i];
        next[current.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = next;
    }

    static Mesh BuildHeightMesh(TerrainData data, int grid)
    {
        Vector3 size = data.size;
        int vertsX = grid + 1;
        Vector3[] verts = new Vector3[vertsX * vertsX];
        Vector2[] uvs = new Vector2[verts.Length];
        int[] tris = new int[grid * grid * 6];

        for (int z = 0; z < vertsX; z++)
        {
            float v = z / (float)grid;
            for (int x = 0; x < vertsX; x++)
            {
                float u = x / (float)grid;
                int i = z * vertsX + x;
                float h = data.GetInterpolatedHeight(u, v);
                verts[i] = new Vector3(u * size.x, h, v * size.z);
                uvs[i] = new Vector2(u, v);
            }
        }

        int t = 0;
        for (int z = 0; z < grid; z++)
        {
            for (int x = 0; x < grid; x++)
            {
                int i = z * vertsX + x;
                tris[t++] = i;
                tris[t++] = i + vertsX;
                tris[t++] = i + 1;
                tris[t++] = i + 1;
                tris[t++] = i + vertsX;
                tris[t++] = i + vertsX + 1;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }


    static Texture2D GetReadableLayerTexture(Texture2D src)
    {
        if (src == null)
            return null;
        if (src.isReadable)
            return src;

        Texture2D cached;
        if (ReadableLayerCache.TryGetValue(src, out cached) && cached != null)
            return cached;

        int size = 256;
        RenderTexture rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(src, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D copy = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        copy.wrapMode = TextureWrapMode.Clamp;
        copy.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        copy.Apply(false, false);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        ReadableLayerCache[src] = copy;
        return copy;
    }

    static void ClearReadableCache()
    {
        foreach (KeyValuePair<Texture2D, Texture2D> pair in ReadableLayerCache)
        {
            if (pair.Value != null && pair.Value != pair.Key)
                Object.DestroyImmediate(pair.Value);
        }
        ReadableLayerCache.Clear();
    }

    static Texture2D BakeColorMap(TerrainData data, int res)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        TerrainLayer[] layers = data.terrainLayers;
        int aRes = data.alphamapResolution;
        if (layers == null || layers.Length == 0 || aRes < 2)
        {
            for (int y = 0; y < res; y++)
            {
                float v = y / (float)(res - 1);
                for (int x = 0; x < res; x++)
                {
                    float u = x / (float)(res - 1);
                    float h = data.GetInterpolatedHeight(u, v) / Mathf.Max(data.size.y, 1f);
                    tex.SetPixel(x, y, Color.Lerp(new Color(0.35f, 0.28f, 0.22f), new Color(0.72f, 0.62f, 0.42f), h));
                }
            }
            tex.Apply(false, false);
            return tex;
        }

        float[,,] alpha = data.GetAlphamaps(0, 0, aRes, aRes);
        int layerCount = Mathf.Min(layers.Length, alpha.GetLength(2));
        for (int y = 0; y < res; y++)
        {
            float v = y / (float)(res - 1);
            int az = Mathf.Clamp(Mathf.RoundToInt(v * (aRes - 1)), 0, aRes - 1);
            for (int x = 0; x < res; x++)
            {
                float u = x / (float)(res - 1);
                int ax = Mathf.Clamp(Mathf.RoundToInt(u * (aRes - 1)), 0, aRes - 1);
                Color c = Color.black;
                float w = 0f;
                for (int l = 0; l < layerCount; l++)
                {
                    float a = alpha[az, ax, l];
                    if (a <= 0.001f || layers[l] == null)
                        continue;
                    Color sample = layers[l].diffuseRemapMax;
                    Texture2D layerTex = GetReadableLayerTexture(layers[l].diffuseTexture);
                    if (layerTex != null)
                        sample = layerTex.GetPixelBilinear(u, v) * layers[l].diffuseRemapMax;
                    c += sample * a;
                    w += a;
                }
                tex.SetPixel(x, y, w > 0f ? c / w : new Color(0.4f, 0.35f, 0.28f));
            }
        }
        tex.Apply(false, false);
        return tex;
    }
}
