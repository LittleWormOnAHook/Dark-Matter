using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BakeWorldBorderFence
{
    const float Origin = -4096f;
    const float TileSize = 2048f;
    const int TileCount = 4;
    const float Height = 0.5f;
    const float Inset = 8f;
    const float Step = 8f;
    const float PostEvery = 32f;
    const float PostWidth = 0.3f;
    const float Lift = 0.08f;
    const string MaterialPath = "Assets/Malbers Animations/Common/Materials & Textures/Interactables/Zones Dark.mat";
    const string OuterMeshPath = "Assets/_Project/Art/World/WorldBorderFence.mesh";
    const string SeamMeshPath = "Assets/_Project/Art/World/WorldTileSeamFence.mesh";
    const string OuterName = "WorldBorderFence";
    const string SeamName = "WorldTileSeamFence";
    const string HeightKey = "DMG_FenceHeight";

    [InitializeOnLoadMethod]
    static void RegisterAutoBake()
    {
        EditorApplication.playModeStateChanged -= OnPlayMode;
        EditorApplication.playModeStateChanged += OnPlayMode;
        EditorApplication.delayCall += TryBakeHeight;
    }

    static void OnPlayMode(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += TryBakeHeight;
    }

    static void TryBakeHeight()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene active = EditorSceneManager.GetActiveScene();
        if (!active.IsValid() || !active.isLoaded)
            return;
        if (string.IsNullOrEmpty(active.path) || !active.path.Contains("Dark Matter Genesis"))
            return;

        float last = SessionState.GetFloat(HeightKey, -1f);
        if (Mathf.Approximately(last, Height) && FindNamed(SeamName) != null && FindNamed(OuterName) != null)
            return;

        Bake();
        SessionState.SetFloat(HeightKey, Height);
    }

    [MenuItem("Dark Matter Genesis/World/Bake Border Fence")]
    public static void Bake()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Debug.LogError("Missing fence material at " + MaterialPath);
            return;
        }

        Dictionary<Vector2Int, TerrainData> tiles = LoadTileData();
        if (tiles.Count == 0)
        {
            Debug.LogError("No Gaia Terrain Data found under Sessions/DM Genesis/Terrain Data.");
            return;
        }

        float worldMin = Origin;
        float worldMax = Origin + TileSize * TileCount;
        float outerMin = worldMin + Inset;
        float outerMax = worldMax - Inset;

        var outer = new List<List<Vector3>>();
        var outerLoop = new List<Vector3>(2048);
        AppendEdge(outerLoop, tiles, outerMin, outerMin, outerMax, outerMin);
        AppendEdge(outerLoop, tiles, outerMax, outerMin, outerMax, outerMax);
        AppendEdge(outerLoop, tiles, outerMax, outerMax, outerMin, outerMax);
        AppendEdge(outerLoop, tiles, outerMin, outerMax, outerMin, outerMin);
        if (outerLoop.Count >= 2)
            outerLoop.Add(outerLoop[0]);
        outer.Add(outerLoop);

        var seams = new List<List<Vector3>>();
        for (int i = 1; i < TileCount; i++)
        {
            float x = Origin + i * TileSize;
            var line = new List<Vector3>();
            AppendEdge(line, tiles, x, worldMin, x, worldMax);
            seams.Add(line);
        }
        for (int j = 1; j < TileCount; j++)
        {
            float z = Origin + j * TileSize;
            var line = new List<Vector3>();
            AppendEdge(line, tiles, worldMin, z, worldMax, z);
            seams.Add(line);
        }

        PlaceFence(OuterName, OuterMeshPath, BuildMesh(outer, OuterName), material);
        PlaceFence(SeamName, SeamMeshPath, BuildMesh(seams, SeamName), material);
        Debug.Log("Fences baked: " + OuterName + " (outer, disable if you want) and " + SeamName + " (16-tile seams). Both off in play.");
    }

    static void PlaceFence(string objectName, string meshPath, Mesh mesh, Material material)
    {
        Directory.CreateDirectory("Assets/_Project/Art/World");
        mesh = SaveMeshAsset(mesh, meshPath);

        Scene scene = EditorSceneManager.GetActiveScene();
        GameObject go = FindNamed(objectName);
        if (go == null)
            go = new GameObject(objectName);

        if (go.scene != scene)
            EditorSceneManager.MoveGameObjectToScene(go, scene);

        go.name = objectName;
        go.SetActive(true);
        go.hideFlags = HideFlags.None;
        go.transform.SetParent(null, true);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        go.transform.localScale = Vector3.one;
        go.isStatic = true;

        if (go.GetComponent<WorldBorderFence>() == null)
            go.AddComponent<WorldBorderFence>();

        MeshFilter filter = go.GetComponent<MeshFilter>();
        if (filter == null)
            filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        Collider[] colliders = go.GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
            Object.DestroyImmediate(colliders[i]);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.SetDirty(go);
    }

    static Mesh SaveMeshAsset(Mesh built, string meshPath)
    {
        Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (asset == null)
        {
            AssetDatabase.CreateAsset(built, meshPath);
            AssetDatabase.SaveAssets();
            return built;
        }

        asset.Clear();
        asset.indexFormat = built.indexFormat;
        asset.vertices = built.vertices;
        asset.uv = built.uv;
        asset.triangles = built.triangles;
        asset.normals = built.normals;
        asset.bounds = built.bounds;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Object.DestroyImmediate(built);
        return asset;
    }

    static GameObject FindNamed(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t != null && t.name == objectName && t.gameObject.scene.IsValid())
                return t.gameObject;
        }
        return null;
    }

    static Dictionary<Vector2Int, TerrainData> LoadTileData()
    {
        var map = new Dictionary<Vector2Int, TerrainData>();
        string root = Path.Combine(Application.dataPath, "Gaia User Data/Sessions/DM Genesis/Terrain Data");
        if (!Directory.Exists(root))
            return map;

        string[] files = Directory.GetFiles(root, "Terrain_*.asset");
        for (int i = 0; i < files.Length; i++)
        {
            string assetPath = "Assets" + files[i].Substring(Application.dataPath.Length).Replace("\\", "/");
            string name = Path.GetFileNameWithoutExtension(files[i]);
            int tx, tz;
            if (!TryParseTile(name, out tx, out tz))
                continue;
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(assetPath);
            if (data != null)
                map[new Vector2Int(tx, tz)] = data;
        }
        return map;
    }

    static bool TryParseTile(string name, out int tx, out int tz)
    {
        tx = tz = -1;
        int first = name.IndexOf('_');
        int second = name.IndexOf('_', first + 1);
        if (first < 0 || second < 0)
            return false;
        int dash = name.IndexOf('-', second);
        string xs = name.Substring(first + 1, second - first - 1);
        string zs = dash > second ? name.Substring(second + 1, dash - second - 1) : name.Substring(second + 1);
        return int.TryParse(xs, NumberStyles.Integer, CultureInfo.InvariantCulture, out tx)
            && int.TryParse(zs, NumberStyles.Integer, CultureInfo.InvariantCulture, out tz);
    }

    static void AppendEdge(List<Vector3> loop, Dictionary<Vector2Int, TerrainData> tiles, float x0, float z0, float x1, float z1)
    {
        float dx = x1 - x0;
        float dz = z1 - z0;
        float length = Mathf.Sqrt(dx * dx + dz * dz);
        int steps = Mathf.Max(1, Mathf.RoundToInt(length / Step));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float x = x0 + dx * t;
            float z = z0 + dz * t;
            loop.Add(new Vector3(x, SampleHeight(tiles, x, z) + Lift, z));
        }
    }

    static float SampleHeight(Dictionary<Vector2Int, TerrainData> tiles, float x, float z)
    {
        int tx = Mathf.Clamp(Mathf.FloorToInt((x - Origin) / TileSize), 0, TileCount - 1);
        int tz = Mathf.Clamp(Mathf.FloorToInt((z - Origin) / TileSize), 0, TileCount - 1);
        TerrainData data;
        if (!tiles.TryGetValue(new Vector2Int(tx, tz), out data) || data == null)
            return 0f;

        float localX = x - (Origin + tx * TileSize);
        float localZ = z - (Origin + tz * TileSize);
        float nx = Mathf.Clamp01(localX / TileSize);
        float nz = Mathf.Clamp01(localZ / TileSize);
        return data.GetInterpolatedHeight(nx, nz);
    }

    static Mesh BuildMesh(List<List<Vector3>> polylines, string meshName)
    {
        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        for (int p = 0; p < polylines.Count; p++)
        {
            List<Vector3> loop = polylines[p];
            float dist = 0f;
            for (int i = 0; i < loop.Count - 1; i++)
            {
                Vector3 a = loop[i];
                Vector3 b = loop[i + 1];
                Vector3 along = b - a;
                along.y = 0f;
                float seg = along.magnitude;
                if (seg < 0.01f)
                    continue;
                along /= seg;

                AddDoubleQuad(
                    verts, uvs, tris,
                    a, b,
                    b + Vector3.up * Height,
                    a + Vector3.up * Height,
                    new Vector2(dist / Height, 0f),
                    new Vector2((dist + seg) / Height, 0f),
                    new Vector2((dist + seg) / Height, 1f),
                    new Vector2(dist / Height, 1f));

                if (Mathf.Repeat(dist, PostEvery) < Step * 0.51f)
                {
                    Vector3 inward = Vector3.Cross(Vector3.up, along);
                    if (inward.sqrMagnitude > 0.01f)
                        inward.Normalize();
                    Vector3 left = a - inward * (PostWidth * 0.5f);
                    Vector3 right = a + inward * (PostWidth * 0.5f);
                    AddDoubleQuad(
                        verts, uvs, tris,
                        left, right,
                        right + Vector3.up * Height,
                        left + Vector3.up * Height,
                        new Vector2(0f, 0f),
                        new Vector2(0.15f, 0f),
                        new Vector2(0.15f, 1f),
                        new Vector2(0f, 1f));
                }

                dist += seg;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = meshName;
        mesh.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void AddDoubleQuad(
        List<Vector3> verts, List<Vector2> uvs, List<int> tris,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d,
        Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
    {
        int i = verts.Count;
        verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
        uvs.Add(uvA); uvs.Add(uvB); uvs.Add(uvC); uvs.Add(uvD);
        tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
        tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
    }
}
