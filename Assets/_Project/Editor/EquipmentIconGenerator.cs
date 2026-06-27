using System.IO;
using Project.Interaction;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Renders inventory icons from mesh/model sources for equipment items.
/// </summary>
public static class EquipmentIconGenerator
{
    public struct Settings
    {
        public int Size;
        public Vector3 ModelRotation;
        public float Padding;
        public Color BackgroundColor;
        public bool TransparentBackground;

        public static Settings Default => new Settings
        {
            Size = 128,
            ModelRotation = new Vector3(0f, 90f, 0f),
            Padding = 1.15f,
            BackgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f),
            TransparentBackground = true
        };
    }

    public static Texture2D RenderPreview(GameObject source, Settings settings)
    {
        if (source == null)
            return null;

        GameObject instance = InstantiateForRender(source);
        if (instance == null)
            return null;

        try
        {
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(settings.ModelRotation));
            Bounds bounds = CalculateRendererBounds(instance);
            if (bounds.size == Vector3.zero)
                return null;

            int size = Mathf.Clamp(settings.Size, 32, 512);
            RenderTexture renderTarget = RenderTexture.GetTemporary(
                size,
                size,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);

            GameObject cameraObject = CreateRenderCamera(renderTarget, settings);
            GameObject[] lightObjects = CreateRenderLights();

            try
            {
                FrameCamera(cameraObject.GetComponent<Camera>(), bounds, settings.Padding);
                cameraObject.GetComponent<Camera>().Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTarget;

                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
                texture.Apply();

                RenderTexture.active = previous;
                return texture;
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                foreach (GameObject lightObject in lightObjects)
                    Object.DestroyImmediate(lightObject);
            }
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    public static Sprite SaveSpriteAsset(GameObject source, string assetPath, Settings settings)
    {
        Texture2D preview = RenderPreview(source, settings);
        if (preview == null)
            return null;

        try
        {
            string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder))
                EnsureFolder(folder);

            string fullPath = ToFullAssetSystemPath(assetPath);
            string fullDirectory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(fullDirectory))
                Directory.CreateDirectory(fullDirectory);

            File.WriteAllBytes(fullPath, preview.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigureSpriteImporter(assetPath, settings.Size);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
        finally
        {
            Object.DestroyImmediate(preview);
        }
    }

    private static GameObject InstantiateForRender(GameObject source)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null)
            instance = Object.Instantiate(source);

        if (instance == null)
            return null;

        instance.hideFlags = HideFlags.HideAndDontSave;
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        StripRuntimeComponents(instance);
        return instance;
    }

    private static void StripRuntimeComponents(GameObject root)
    {
        foreach (ItemPickup pickup in root.GetComponentsInChildren<ItemPickup>(true))
            Object.DestroyImmediate(pickup);

        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(collider);

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(body);
    }

    private static GameObject CreateRenderCamera(RenderTexture target, Settings settings)
    {
        GameObject cameraObject = new GameObject("EquipmentIconCamera")
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = settings.TransparentBackground
            ? new Color(0f, 0f, 0f, 0f)
            : settings.BackgroundColor;
        camera.cullingMask = ~0;
        camera.fieldOfView = 28f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100f;
        camera.targetTexture = target;
        camera.enabled = false;
        return cameraObject;
    }

    private static GameObject[] CreateRenderLights()
    {
        return new[]
        {
            CreateDirectionalLight("EquipmentIconKeyLight", new Vector3(35f, -35f, 0f), 1.15f),
            CreateDirectionalLight("EquipmentIconFillLight", new Vector3(325f, 25f, 0f), 0.55f)
        };
    }

    private static GameObject CreateDirectionalLight(string name, Vector3 euler, float intensity)
    {
        GameObject lightObject = new GameObject(name)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.color = Color.white;
        lightObject.transform.rotation = Quaternion.Euler(euler);
        return lightObject;
    }

    private static void FrameCamera(Camera camera, Bounds bounds, float padding)
    {
        Vector3 center = bounds.center;
        float radius = bounds.extents.magnitude * Mathf.Max(1f, padding);
        camera.transform.rotation = Quaternion.Euler(22f, -35f, 0f);
        camera.transform.position = center - camera.transform.forward * (radius * 2.8f);
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = radius * 12f;
    }

    private static Bounds CalculateRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    private static void ConfigureSpriteImporter(string assetPath, int pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.SaveAndReimport();
    }

    public static Sprite EnsureSpriteFromTexture(Texture2D texture, int pixelsPerUnit = 100)
    {
        if (texture == null)
            return null;

        string assetPath = AssetDatabase.GetAssetPath(texture);
        if (string.IsNullOrEmpty(assetPath))
            return null;

        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null &&
            importer.textureType == TextureImporterType.Sprite &&
            existing != null)
            return existing;

        ConfigureSpriteImporter(assetPath, pixelsPerUnit);
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    public static Sprite ImportImageFileAsSprite(
        string sourceFilePath,
        string targetAssetPath,
        int pixelsPerUnit = 100)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            return null;

        string folder = Path.GetDirectoryName(targetAssetPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folder))
            EnsureFolder(folder);

        string fullPath = ToFullAssetSystemPath(targetAssetPath);
        string fullDirectory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(fullDirectory))
            Directory.CreateDirectory(fullDirectory);

        File.Copy(sourceFilePath, fullPath, overwrite: true);
        AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate);
        ConfigureSpriteImporter(targetAssetPath, pixelsPerUnit);
        AssetDatabase.SaveAssets();

        return AssetDatabase.LoadAssetAtPath<Sprite>(targetAssetPath);
    }

    private static string ToFullAssetSystemPath(string assetPath)
    {
        assetPath = assetPath.Replace('\\', '/');
        if (assetPath.StartsWith("Assets/", System.StringComparison.Ordinal))
            return Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));

        return Path.GetFullPath(assetPath);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            return;

        folderPath = folderPath.Replace('\\', '/');
        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);

        string folderName = Path.GetFileName(folderPath);
        if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(parent))
            return;

        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
