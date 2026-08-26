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
/// LOD0 uses a 1024 albedo + baked normal + AO mask. Mesh grids stay 128/64/32.
/// </summary>
public static class BakeTlmImpostors
{
    const string StoragePath = "Assets/Gaia User Data/Sessions/DM Genesis/TerrainScenes.asset";
    const string ScenesDir = "Assets/Gaia User Data/Sessions/DM Genesis/Terrain Scenes";
    const string AssetDir = ScenesDir + "/Impostors";
    const double ImpostorRangeMeters = 3500d;
    const int Lod0TextureSize = 1024;
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
        Debug.Log("Bake TLM Impostors: wrote " + baked + " Impostor_x_y scenes with 1024 albedo/normal/AO. TLM impostor range " + ImpostorRangeMeters + "m.");
        EditorUtility.DisplayDialog(
            "Bake TLM Impostors",
            "Wrote " + baked + " impostor scenes.\nLOD0 textures: 1024 albedo + normal + AO.\nImpostor range " + ImpostorRangeMeters + "m.\nGaia Create Impostors was not used.",
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

        float[,] heights = SampleHeights(data, Lod0TextureSize);

        Texture2D colorMap = BakeColorMap(data, Lod0TextureSize);
        WriteTexture(meshFolder + "/Color.png", colorMap, TextureImporterType.Default, true);
        Object.DestroyImmediate(colorMap);

        Texture2D normalMap = BakeNormalMap(data, heights, Lod0TextureSize);
        WriteTexture(meshFolder + "/Normal.png", normalMap, TextureImporterType.NormalMap, false);
        Object.DestroyImmediate(normalMap);

        Texture2D aoMap = BakeAoMap(data, heights, Lod0TextureSize);
        WriteTexture(meshFolder + "/AO.png", aoMap, TextureImporterType.Default, false);
        Texture2D maskMap = BakeMaskMap(aoMap);
        WriteTexture(meshFolder + "/Mask.png", maskMap, TextureImporterType.Default, false);
        Object.DestroyImmediate(aoMap);
        Object.DestroyImmediate(maskMap);

        AssetDatabase.ImportAsset(meshFolder + "/Color.png");
        AssetDatabase.ImportAsset(meshFolder + "/Normal.png");
        AssetDatabase.ImportAsset(meshFolder + "/AO.png");
        AssetDatabase.ImportAsset(meshFolder + "/Mask.png");

        Texture2D importedColor = AssetDatabase.LoadAssetAtPath<Texture2D>(meshFolder + "/Color.png");
        Texture2D importedNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(meshFolder + "/Normal.png");
        Texture2D importedMask = AssetDatabase.LoadAssetAtPath<Texture2D>(meshFolder + "/Mask.png");

        Shader lit = Shader.Find("HDRP/Lit");
        if (lit == null)
            lit = Shader.Find("Hidden/HDRP/FallbackError");
        Material mat = new Material(lit);
        mat.name = stem + "_Lit";
        AssignTex(mat, importedColor, "_BaseColorMap", "_BaseMap", "_MainTex");
        if (importedNormal != null)
        {
            AssignTex(mat, importedNormal, "_NormalMap", "_BumpMap");
            if (mat.HasProperty("_NormalScale"))
                mat.SetFloat("_NormalScale", 1f);
            if (mat.HasProperty("_BumpScale"))
                mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (importedMask != null)
        {
            AssignTex(mat, importedMask, "_MaskMap");
            mat.EnableKeyword("_MASKMAP");
        }
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.18f);
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
        group.fadeMode = LODFadeMode.CrossFade;
        group.animateCrossFading = true;
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

    static void AssignTex(Material mat, Texture tex, params string[] names)
    {
        if (mat == null || tex == null)
            return;
        for (int i = 0; i < names.Length; i++)
        {
            if (mat.HasProperty(names[i]))
                mat.SetTexture(names[i], tex);
        }
    }

    static void WriteTexture(string assetPath, Texture2D tex, TextureImporterType type, bool sRGB)
    {
        File.WriteAllBytes(assetPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;
        importer.textureType = type;
        importer.sRGBTexture = sRGB;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = Lod0TextureSize;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
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
        Vector4[] tangents = new Vector4[verts.Length];
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
                tangents[i] = new Vector4(1f, 0f, 0f, 1f);
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
        mesh.tangents = tangents;
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

    static float[,] SampleHeights(TerrainData data, int res)
    {
        float[,] h = new float[res, res];
        int last = res - 1;
        for (int y = 0; y < res; y++)
        {
            float v = y / (float)last;
            for (int x = 0; x < res; x++)
            {
                float u = x / (float)last;
                h[y, x] = data.GetInterpolatedHeight(u, v);
            }
        }
        return h;
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

    static Texture2D BakeNormalMap(TerrainData data, float[,] heights, int res)
    {
        Vector3 size = data.size;
        float stepX = size.x / (res - 1);
        float stepZ = size.z / (res - 1);
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        int last = res - 1;
        for (int y = 0; y < res; y++)
        {
            int y0 = Mathf.Max(0, y - 1);
            int y1 = Mathf.Min(last, y + 1);
            for (int x = 0; x < res; x++)
            {
                int x0 = Mathf.Max(0, x - 1);
                int x1 = Mathf.Min(last, x + 1);
                float dx = (heights[y, x1] - heights[y, x0]) / Mathf.Max(0.0001f, (x1 - x0) * stepX);
                float dz = (heights[y1, x] - heights[y0, x]) / Mathf.Max(0.0001f, (y1 - y0) * stepZ);
                Vector3 n = new Vector3(-dx, 1f, -dz).normalized;
                // Heightfield tangent +X, bitangent +Z, normal +Y -> tangent-space normal.
                Vector3 ts = new Vector3(n.x, n.z, n.y);
                tex.SetPixel(x, y, new Color(ts.x * 0.5f + 0.5f, ts.y * 0.5f + 0.5f, ts.z * 0.5f + 0.5f, 1f));
            }
        }
        tex.Apply(false, false);
        return tex;
    }

    static Texture2D BakeAoMap(TerrainData data, float[,] heights, int res)
    {
        Vector3 size = data.size;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGB24, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        int last = res - 1;
        Vector2Int[] dirs =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };
        int steps = 8;
        float worldPerTexel = size.x / last;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float h = heights[y, x];
                float vis = 0f;
                for (int d = 0; d < dirs.Length; d++)
                {
                    float maxRise = 0f;
                    int sx = dirs[d].x;
                    int sy = dirs[d].y;
                    float diag = Mathf.Sqrt(sx * sx + sy * sy);
                    for (int s = 1; s <= steps; s++)
                    {
                        int px = Mathf.Clamp(x + sx * s, 0, last);
                        int py = Mathf.Clamp(y + sy * s, 0, last);
                        float dist = s * diag * worldPerTexel;
                        float rise = (heights[py, px] - h) / Mathf.Max(dist, 0.001f);
                        if (rise > maxRise)
                            maxRise = rise;
                    }
                    vis += 1f - Mathf.Clamp01(maxRise * 6f);
                }
                float ao = Mathf.Lerp(0.28f, 1f, vis / dirs.Length);
                tex.SetPixel(x, y, new Color(ao, ao, ao, 1f));
            }
        }
        tex.Apply(false, false);
        return tex;
    }

    static Texture2D BakeMaskMap(Texture2D ao)
    {
        int w = ao.width;
        int h = ao.height;
        Texture2D mask = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        mask.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float g = ao.GetPixel(x, y).g;
                // HDRP mask: R metallic, G AO, B detail, A smoothness
                mask.SetPixel(x, y, new Color(0f, g, 0f, 0.18f));
            }
        }
        mask.Apply(false, false);
        return mask;
    }
}
