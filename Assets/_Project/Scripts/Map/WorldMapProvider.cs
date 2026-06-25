using System;
using System.Collections;
using Project.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Map
{
    /// <summary>
    /// Defines playable world bounds and provides a top-down map texture for UI.
    /// </summary>
    public class WorldMapProvider : MonoBehaviour
    {
        private const string FakeMapResourcePath = "UI/FakeMap";
        private const string FakeMapAssetPath = "Assets/_Project/Textures/UI/FakeMap.png";

        public static WorldMapProvider Instance { get; private set; }

        [SerializeField] private Terrain terrain;
        [SerializeField] private bool useTerrainBounds = true;
        [SerializeField] private Vector2 manualWorldSize = new Vector2(512f, 512f);
        [SerializeField] private Vector3 manualWorldOrigin = Vector3.zero;
        [SerializeField] private int mapTextureResolution = 96;
        [SerializeField] private Texture2D mapTextureOverride;
        [SerializeField] private bool buildTerrainTextureAtRuntime = true;

        [Header("Terrain Map Colors")]
        [SerializeField] private Color lowlandColor = new Color(0.12f, 0.24f, 0.14f, 1f);
        [SerializeField] private Color highlandColor = new Color(0.45f, 0.42f, 0.32f, 1f);

        public Bounds WorldBounds { get; private set; }
        public Texture2D MapTexture { get; private set; }
        public bool IsMapTextureReady { get; private set; }

        public event Action MapTextureReady;

        private Coroutine buildRoutine;
        private Texture2D runtimeGeneratedTexture;
        private Texture2D fallbackTexture;
        private static Texture2D cachedFakeMapTexture;

        internal static void ResetStaticState()
        {
            Instance = null;
            cachedFakeMapTexture = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureTerrainReference();
            ResolveBounds();

            if (mapTextureOverride == null)
                mapTextureOverride = LoadFakeMapTexture();

            if (!GameSettings.MapSystemEnabled)
            {
                enabled = false;
                return;
            }

            InitializeMapTexture();
        }

        private void Start()
        {
            if (!GameSettings.MapSystemEnabled)
                return;

            EnsureTerrainReference();
            ResolveBounds();

            if (!UsesStaticMapTexture())
                TryStartTerrainBuild();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (buildRoutine != null)
            {
                StopCoroutine(buildRoutine);
                buildRoutine = null;
            }

            DestroyTexture(ref runtimeGeneratedTexture);
            DestroyTexture(ref fallbackTexture);

            if (!IsExternalMapTexture(MapTexture))
                MapTexture = null;
        }

        public Vector2 WorldToMap01(Vector3 worldPosition)
        {
            Vector3 min = WorldBounds.min;
            Vector3 max = WorldBounds.max;
            float x = Mathf.InverseLerp(min.x, max.x, worldPosition.x);
            float z = Mathf.InverseLerp(min.z, max.z, worldPosition.z);
            return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(z));
        }

        public Vector3 Map01ToWorld(Vector2 map01)
        {
            Vector3 min = WorldBounds.min;
            Vector3 max = WorldBounds.max;
            return new Vector3(
                Mathf.Lerp(min.x, max.x, map01.x),
                WorldBounds.center.y,
                Mathf.Lerp(min.z, max.z, map01.y));
        }

        public void ApplySystemEnabled(bool enabled)
        {
            if (!enabled)
            {
                if (buildRoutine != null)
                {
                    StopCoroutine(buildRoutine);
                    buildRoutine = null;
                }

                this.enabled = false;
                return;
            }

            if (!this.enabled)
                this.enabled = true;

            EnsureTerrainReference();
            ResolveBounds();

            if (IsMapTextureReady)
            {
                MapTextureReady?.Invoke();
                return;
            }

            InitializeMapTexture();
        }

        private void InitializeMapTexture()
        {
            if (TryApplyStaticMapTexture())
            {
                MapTextureReady?.Invoke();
                return;
            }

            fallbackTexture = CreateFallbackTexture();
            MapTexture = fallbackTexture;
            IsMapTextureReady = true;
            MapTextureReady?.Invoke();

            if (TrySyncBakeTerrainPreview())
                MapTextureReady?.Invoke();

            if (TryStartTerrainBuild())
                return;
        }

        private bool TryApplyStaticMapTexture()
        {
            Texture2D texture = mapTextureOverride;
            if (texture == null || !IsDedicatedMapTexture(texture))
                texture = LoadFakeMapTexture();

            if (texture == null)
                return false;

            mapTextureOverride = texture;
            MapTexture = texture;
            IsMapTextureReady = true;
            return true;
        }

        private bool UsesStaticMapTexture()
        {
            Texture2D texture = mapTextureOverride != null ? mapTextureOverride : LoadFakeMapTexture();
            return texture != null && IsDedicatedMapTexture(texture);
        }

        private bool IsExternalMapTexture(Texture2D texture)
        {
            if (texture == null)
                return false;

            if (texture == mapTextureOverride || texture == LoadFakeMapTexture())
                return true;

            return false;
        }

        private bool TrySyncBakeTerrainPreview()
        {
            if (UsesStaticMapTexture() || !buildTerrainTextureAtRuntime)
                return false;

            EnsureTerrainReference();
            TerrainData data = terrain != null ? terrain.terrainData : null;
            if (data == null)
                return false;

            int resolution = Mathf.Clamp(Mathf.Min(mapTextureResolution, 128), 32, 128);
            float maxHeight = Mathf.Max(0.001f, data.size.y);
            int alphaWidth = data.alphamapWidth;
            int alphaHeight = data.alphamapHeight;
            int layerCount = data.alphamapLayers;
            float[,,] alphamaps = layerCount > 0
                ? data.GetAlphamaps(0, 0, alphaWidth, alphaHeight)
                : null;
            TerrainLayer[] layers = data.terrainLayers;

            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "SyncTerrainMapPreview",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                float sampleY = resolution <= 1 ? 0f : (float)y / (resolution - 1);
                for (int x = 0; x < resolution; x++)
                {
                    float sampleX = resolution <= 1 ? 0f : (float)x / (resolution - 1);
                    pixels[y * resolution + x] = SampleTerrainMapColor(
                        data,
                        alphamaps,
                        layers,
                        alphaWidth,
                        alphaHeight,
                        sampleX,
                        sampleY,
                        maxHeight);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            DestroyTexture(ref runtimeGeneratedTexture);
            runtimeGeneratedTexture = texture;
            MapTexture = texture;
            IsMapTextureReady = true;
            return true;
        }

        public static Texture2D CreateDisplayFallback()
        {
            Texture2D fakeMap = LoadFakeMapTexture();
            if (fakeMap != null)
                return fakeMap;

            return CreateFallbackTexture();
        }

        public static Texture2D LoadFakeMapTexture()
        {
            if (cachedFakeMapTexture != null)
                return cachedFakeMapTexture;

            cachedFakeMapTexture = Resources.Load<Texture2D>(FakeMapResourcePath);
#if UNITY_EDITOR
            if (cachedFakeMapTexture == null)
                cachedFakeMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FakeMapAssetPath);
#endif
            return cachedFakeMapTexture;
        }

        private bool TryStartTerrainBuild()
        {
            if (UsesStaticMapTexture() || !buildTerrainTextureAtRuntime || !isActiveAndEnabled)
                return false;

            EnsureTerrainReference();
            if (terrain == null || terrain.terrainData == null)
                return false;

            if (buildRoutine != null)
                return true;

            buildRoutine = StartCoroutine(BuildTerrainMapTextureAsync());
            return true;
        }

        private void EnsureTerrainReference()
        {
            if (terrain != null)
                return;

            terrain = GetComponent<Terrain>();
            if (terrain == null)
                terrain = FindAnyObjectByType<Terrain>();
        }

        private void ResolveBounds()
        {
            if (useTerrainBounds)
            {
                EnsureTerrainReference();

                if (terrain != null && terrain.terrainData != null)
                {
                    Vector3 size = terrain.terrainData.size;
                    Vector3 origin = terrain.transform.position;
                    WorldBounds = new Bounds(origin + size * 0.5f, size);
                    return;
                }
            }

            Vector3 flatSize = new Vector3(manualWorldSize.x, 100f, manualWorldSize.y);
            WorldBounds = new Bounds(manualWorldOrigin + flatSize * 0.5f, flatSize);
        }

        private IEnumerator BuildTerrainMapTextureAsync()
        {
            yield return null;
            yield return null;

            if (UsesStaticMapTexture())
            {
                buildRoutine = null;
                yield break;
            }

            EnsureTerrainReference();
            TerrainData data = terrain != null ? terrain.terrainData : null;
            if (data == null)
                yield break;

            int resolution = Mathf.Clamp(mapTextureResolution, 64, 512);
            float maxHeight = Mathf.Max(0.001f, data.size.y);
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "RuntimeTerrainMap",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            int alphaWidth = data.alphamapWidth;
            int alphaHeight = data.alphamapHeight;
            int layerCount = data.alphamapLayers;
            float[,,] alphamaps = layerCount > 0
                ? data.GetAlphamaps(0, 0, alphaWidth, alphaHeight)
                : null;
            TerrainLayer[] layers = data.terrainLayers;

            Color[] pixels = new Color[resolution * resolution];

            for (int y = 0; y < resolution; y++)
            {
                float sampleY = resolution <= 1 ? 0f : (float)y / (resolution - 1);
                for (int x = 0; x < resolution; x++)
                {
                    float sampleX = resolution <= 1 ? 0f : (float)x / (resolution - 1);
                    pixels[y * resolution + x] = SampleTerrainMapColor(
                        data,
                        alphamaps,
                        layers,
                        alphaWidth,
                        alphaHeight,
                        sampleX,
                        sampleY,
                        maxHeight);
                }

                if ((y & 7) == 0)
                    yield return null;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            runtimeGeneratedTexture = texture;
            MapTexture = texture;
            IsMapTextureReady = true;

            DestroyTexture(ref fallbackTexture);
            MapTextureReady?.Invoke();
            buildRoutine = null;
        }

        private Color SampleTerrainMapColor(
            TerrainData data,
            float[,,] alphamaps,
            TerrainLayer[] layers,
            int alphaWidth,
            int alphaHeight,
            float sampleX,
            float sampleY,
            float maxHeight)
        {
            float height = data.GetInterpolatedHeight(sampleX, sampleY);
            float normalizedHeight = Mathf.Clamp01(height / maxHeight);
            Color heightColor = Color.Lerp(lowlandColor, highlandColor, normalizedHeight);

            if (alphamaps == null || layers == null || layers.Length == 0 || alphaWidth <= 0 || alphaHeight <= 0)
                return heightColor;

            int alphaX = Mathf.Clamp(Mathf.FloorToInt(sampleX * (alphaWidth - 1)), 0, alphaWidth - 1);
            int alphaY = Mathf.Clamp(Mathf.FloorToInt(sampleY * (alphaHeight - 1)), 0, alphaHeight - 1);

            Color splatColor = Color.black;
            float weightSum = 0f;
            int layerLimit = Mathf.Min(layers.Length, alphamaps.GetLength(2));
            for (int layerIndex = 0; layerIndex < layerLimit; layerIndex++)
            {
                float weight = alphamaps[alphaY, alphaX, layerIndex];
                if (weight <= 0.001f)
                    continue;

                TerrainLayer layer = layers[layerIndex];
                Color layerColor = layer != null
                    ? SampleTerrainLayerColor(layer, sampleX, sampleY)
                    : Color.gray;
                splatColor += layerColor * weight;
                weightSum += weight;
            }

            if (weightSum <= 0.001f)
                return heightColor;

            splatColor /= weightSum;
            return Color.Lerp(heightColor, splatColor, 0.85f);
        }

        private static Color SampleTerrainLayerColor(TerrainLayer layer, float sampleX, float sampleY)
        {
            if (layer == null)
                return Color.gray;

            Color tint = layer.diffuseRemapMax;
            Texture2D diffuse = layer.diffuseTexture;
            if (diffuse == null || !diffuse.isReadable)
                return tint;

            int texX = Mathf.Clamp(Mathf.FloorToInt(sampleX * diffuse.width), 0, diffuse.width - 1);
            int texY = Mathf.Clamp(Mathf.FloorToInt(sampleY * diffuse.height), 0, diffuse.height - 1);
            Color sampled = diffuse.GetPixel(texX, texY);
            return Color.Lerp(tint, sampled, 0.65f);
        }

        private static bool IsDedicatedMapTexture(Texture2D texture)
        {
            if (texture == null)
                return false;

            string name = texture.name;
            return name.Contains("FakeMap", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Map", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Minimap", StringComparison.OrdinalIgnoreCase);
        }

        private static void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
                return;

            Destroy(texture);
            texture = null;
        }

        private static Texture2D CreateFallbackTexture()
        {
            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false)
            {
                name = "FallbackWorldMap",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color baseColor = new Color(0.14f, 0.18f, 0.16f, 1f);
            Color gridColor = new Color(0.2f, 0.26f, 0.22f, 1f);

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool grid = x == 0 || y == 0 || x == 7 || y == 7;
                    texture.SetPixel(x, y, grid ? gridColor : baseColor);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
