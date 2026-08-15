using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
        [SerializeField] private int mapTextureResolution = 512;
        [SerializeField] private Texture2D mapTextureOverride;
        [SerializeField] private bool buildTerrainTextureAtRuntime = true;
        [Tooltip("When a terrain exists, bake the live terrain map instead of the static FakeMap texture.")]
        [SerializeField] private bool preferTerrainGeneratedMap = true;
        [Tooltip("Render a top-down camera snapshot of the terrain (matches in-scene look). Falls back to height/splat bake.")]
        [SerializeField] private bool useCameraTerrainSnapshot = true;
        [Tooltip("Flip baked map V so terrain +Z (north) aligns with UI up.")]
        [SerializeField] private bool invertMapVertical;

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
            RefreshWorldBounds();

            if (mapTextureOverride == null && !ShouldPreferTerrainGeneratedMap())
                mapTextureOverride = LoadFakeMapTexture();

            InitializeMapTexture();
        }

        private void Start()
        {
            EnsureTerrainReference();
            RefreshWorldBounds();

            if (!UsesStaticMapTexture())
                TryStartTerrainBuild();
        }

        public void RefreshWorldBounds()
        {
            ResolveBounds();
        }

        public float GetPlayableWorldSpan()
        {
            return Mathf.Max(WorldBounds.size.x, WorldBounds.size.z);
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
            if (invertMapVertical)
                z = 1f - z;
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
            if (UsesStaticMapTexture() && TryApplyStaticMapTexture())
            {
                MapTextureReady?.Invoke();
                return;
            }

            // Prefer the authored FakeMap while terrain baking runs — never flash the tiny
            // procedural placeholder (looks like a broken/pixelated minimap in builds).
            Texture2D interim = LoadFakeMapTexture();
            if (interim != null)
            {
                MapTexture = interim;
            }
            else
            {
                fallbackTexture = CreateFallbackTexture();
                MapTexture = fallbackTexture;
            }

            IsMapTextureReady = true;
            MapTextureReady?.Invoke();

            if (TrySyncBakeTerrainPreview())
                MapTextureReady?.Invoke();

            if (TryStartTerrainBuild())
                return;
        }

        private bool TryApplyStaticMapTexture()
        {
            if (preferTerrainGeneratedMap && HasBakeableTerrain())
                return false;

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
            if (preferTerrainGeneratedMap && HasBakeableTerrain())
                return false;

            Texture2D texture = mapTextureOverride != null ? mapTextureOverride : LoadFakeMapTexture();
            return texture != null && IsDedicatedMapTexture(texture);
        }

        private bool HasBakeableTerrain()
        {
            EnsureTerrainReference();
            return terrain != null && terrain.terrainData != null;
        }

        private bool IsExternalMapTexture(Texture2D texture)
        {
            if (texture == null)
                return false;

            if (texture == mapTextureOverride || texture == LoadFakeMapTexture())
                return true;

            return false;
        }

        private bool ShouldPreferTerrainGeneratedMap()
        {
            return preferTerrainGeneratedMap && HasBakeableTerrain();
        }

        private bool TrySyncBakeTerrainPreview()
        {
            if (UsesStaticMapTexture() || !buildTerrainTextureAtRuntime)
                return false;

            if (!TryBakeActiveTerrainMap(out Texture2D texture, "SyncTerrainMapPreview"))
                return false;

            DestroyTexture(ref runtimeGeneratedTexture);
            runtimeGeneratedTexture = texture;
            MapTexture = texture;
            IsMapTextureReady = true;
            return true;
        }

        public bool TryBakeActiveTerrainMap(out Texture2D texture, string textureName = "RuntimeTerrainMap")
        {
            texture = null;
            EnsureTerrainReference();
            TerrainData data = terrain != null ? terrain.terrainData : null;
            if (data == null)
                return false;

            Texture2D cameraTexture = null;
            if (useCameraTerrainSnapshot
                && TryBakeCameraTerrainSnapshot(out cameraTexture, textureName)
                && HasUsableMapTexture(cameraTexture))
            {
                texture = cameraTexture;
                return true;
            }

            if (cameraTexture != null)
                DestroyImmediate(cameraTexture);

            Texture2D heightSplat = BakeTerrainMapTexture(data, ResolveMapTextureResolution(data), textureName);
            if (HasUsableMapTexture(heightSplat))
            {
                texture = heightSplat;
                return true;
            }

            if (heightSplat != null)
                DestroyImmediate(heightSplat);

            // Leave FakeMap / prior interim in place — do not promote muddy flat bakes.
            return false;
        }

        private static bool HasUsableMapTexture(Texture2D texture)
        {
            if (texture == null || texture.width < 2 || texture.height < 2)
                return false;

            // Tiny procedural placeholders are never display-worthy.
            if (texture.width < 32 || texture.height < 32)
                return false;

            // Authored FakeMap / Resources textures may be non-readable in player builds.
            if (!texture.isReadable)
                return texture.width >= 128 && texture.height >= 128;

            Color baseline = texture.GetPixel(0, 0);
            int samples = 0;
            int differentSamples = 0;
            float luminanceAccum = 0f;
            float luminanceSqAccum = 0f;
            int stepX = Mathf.Max(1, texture.width / 8);
            int stepY = Mathf.Max(1, texture.height / 8);

            for (int y = 0; y < texture.height; y += stepY)
            {
                for (int x = 0; x < texture.width; x += stepX)
                {
                    Color sample = texture.GetPixel(x, y);
                    samples++;
                    float lum = sample.r * 0.299f + sample.g * 0.587f + sample.b * 0.114f;
                    luminanceAccum += lum;
                    luminanceSqAccum += lum * lum;

                    float dr = sample.r - baseline.r;
                    float dg = sample.g - baseline.g;
                    float db = sample.b - baseline.b;
                    float da = sample.a - baseline.a;
                    if (dr * dr + dg * dg + db * db + da * da > 0.0004f)
                        differentSamples++;
                }
            }

            if (samples <= 0 || differentSamples <= 0)
                return false;

            // Flat prototype terrains bake to near-uniform muddy aerials that look like a
            // broken RawImage when zoomed in the circular minimap — reject those.
            float mean = luminanceAccum / samples;
            float variance = (luminanceSqAccum / samples) - (mean * mean);
            if (variance < 0.0025f)
                return false;

            return differentSamples >= Mathf.Max(3, samples / 4);
        }

        public Texture2D BakeTerrainMapTexture(TerrainData data, int resolution, string textureName = "TerrainMapSnapshot")
        {
            if (data == null || resolution <= 0)
                return null;

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
                name = textureName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                float sampleY = resolution <= 1 ? 0f : (float)y / (resolution - 1);
                if (invertMapVertical)
                    sampleY = 1f - sampleY;

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
            return texture;
        }

        private bool TryBakeCameraTerrainSnapshot(out Texture2D texture, string textureName)
        {
            texture = null;
            EnsureTerrainReference();
            if (terrain == null || terrain.terrainData == null)
                return false;

            TerrainData data = terrain.terrainData;
            int resolution = ResolveMapTextureResolution(data);
            Vector3 terrainOrigin = terrain.transform.position;
            Vector3 terrainSize = data.size;
            Bounds terrainBounds = new Bounds(terrainOrigin + terrainSize * 0.5f, terrainSize);

            GameObject cameraObject = new GameObject("TerrainMapBakeCamera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            Camera bakeCamera = cameraObject.AddComponent<Camera>();
            bakeCamera.enabled = false;
            bakeCamera.orthographic = true;
            bakeCamera.orthographicSize = Mathf.Max(terrainBounds.extents.x, terrainBounds.extents.z);
            bakeCamera.nearClipPlane = 0.3f;
            bakeCamera.farClipPlane = terrainSize.y + 500f;
            bakeCamera.clearFlags = CameraClearFlags.SolidColor;
            bakeCamera.backgroundColor = lowlandColor;
            bakeCamera.transform.position = terrainBounds.center + Vector3.up * (terrainBounds.max.y + 50f);
            bakeCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            if (!bakeCamera.TryGetComponent(out UniversalAdditionalCameraData urpCameraData))
                urpCameraData = bakeCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            urpCameraData.renderType = CameraRenderType.Base;

            int uiLayer = LayerMask.NameToLayer("UI");
            int terrainLayer = terrain.gameObject.layer;
            if (terrainLayer != 0)
                bakeCamera.cullingMask = 1 << terrainLayer;
            else
                bakeCamera.cullingMask = uiLayer >= 0 ? ~(1 << uiLayer) : ~0;

            RenderTexture renderTarget = RenderTexture.GetTemporary(
                resolution,
                resolution,
                24,
                RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = RenderTexture.active;

            try
            {
                bakeCamera.targetTexture = renderTarget;
                bakeCamera.Render();

                RenderTexture.active = renderTarget;
                texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false)
                {
                    name = textureName,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                texture.ReadPixels(new Rect(0f, 0f, resolution, resolution), 0, 0);
                texture.Apply();

                // RenderTexture reads are vertically flipped relative to world/map UV space.
                FlipTextureVertical(texture);
                if (invertMapVertical)
                    FlipTextureVertical(texture);
            }
            finally
            {
                bakeCamera.targetTexture = null;
                RenderTexture.active = previousTarget;
                RenderTexture.ReleaseTemporary(renderTarget);
                DestroyImmediate(cameraObject);
            }

            return texture != null;
        }

        private static void FlipTextureVertical(Texture2D texture)
        {
            if (texture == null)
                return;

            int width = texture.width;
            int height = texture.height;
            Color[] pixels = texture.GetPixels();
            Color[] flipped = new Color[pixels.Length];

            for (int y = 0; y < height; y++)
            {
                int srcRow = y * width;
                int dstRow = (height - 1 - y) * width;
                System.Array.Copy(pixels, srcRow, flipped, dstRow, width);
            }

            texture.SetPixels(flipped);
            texture.Apply();
        }

        public static Texture2D CreateDisplayFallback()
        {
            WorldMapProvider provider = Instance;
            if (provider != null && provider.MapTexture != null && HasUsableMapTexture(provider.MapTexture))
                return provider.MapTexture;

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
            if (useTerrainBounds && TryResolveTerrainBounds(out Bounds terrainBounds))
            {
                WorldBounds = terrainBounds;
                return;
            }

            Vector3 flatSize = new Vector3(manualWorldSize.x, 100f, manualWorldSize.y);
            WorldBounds = new Bounds(manualWorldOrigin + flatSize * 0.5f, flatSize);
        }

        private bool TryResolveTerrainBounds(out Bounds combinedBounds)
        {
            combinedBounds = default;
            Terrain[] terrains = FindObjectsByType<Terrain>();
            bool found = false;

            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain candidate = terrains[i];
                if (candidate == null || !candidate.isActiveAndEnabled || candidate.terrainData == null)
                    continue;

                Vector3 size = candidate.terrainData.size;
                Vector3 origin = candidate.transform.position;
                Bounds terrainBounds = new Bounds(origin + size * 0.5f, size);

                if (!found)
                {
                    combinedBounds = terrainBounds;
                    found = true;
                    if (terrain == null)
                        terrain = candidate;
                    continue;
                }

                combinedBounds.Encapsulate(terrainBounds.min);
                combinedBounds.Encapsulate(terrainBounds.max);
            }

            if (found && terrain == null)
                EnsureTerrainReference();

            return found;
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

            int resolution = ResolveMapTextureResolution(data);
            if (!TryBakeActiveTerrainMap(out Texture2D texture, "RuntimeTerrainMap"))
            {
                buildRoutine = null;
                yield break;
            }

            for (int y = 0; y < resolution; y++)
            {
                if ((y & 7) == 0)
                    yield return null;
            }

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

        private int ResolveMapTextureResolution(TerrainData data)
        {
            float maxDimension = data != null
                ? Mathf.Max(data.size.x, data.size.z)
                : GetPlayableWorldSpan();

            int scaled = mapTextureResolution;
            if (maxDimension > 700f)
                scaled = Mathf.Max(scaled, 512);
            if (maxDimension > 1400f)
                scaled = Mathf.Max(scaled, 768);

            return Mathf.Clamp(scaled, 128, 1024);
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
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "FallbackWorldMap",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color baseColor = new Color(0.14f, 0.18f, 0.16f, 1f);
            Color gridColor = new Color(0.2f, 0.26f, 0.22f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool grid = x == 0 || y == 0 || x == size - 1 || y == size - 1
                        || (x % 8) == 0 || (y % 8) == 0;
                    texture.SetPixel(x, y, grid ? gridColor : baseColor);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
