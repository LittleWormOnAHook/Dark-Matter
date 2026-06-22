using System;
using System.Collections;
using Project.Core;
using UnityEngine;

namespace Project.Map
{
    /// <summary>
    /// Defines playable world bounds and provides a top-down map texture for UI.
    /// </summary>
    public class WorldMapProvider : MonoBehaviour
    {
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

        internal static void ResetStaticState()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            ResolveBounds();

            if (!GameSettings.MapSystemEnabled)
            {
                enabled = false;
                return;
            }

            if (mapTextureOverride != null)
            {
                MapTexture = mapTextureOverride;
                IsMapTextureReady = true;
                MapTextureReady?.Invoke();
                return;
            }

            fallbackTexture = CreateFallbackTexture();
            MapTexture = fallbackTexture;
            IsMapTextureReady = true;

            if (buildTerrainTextureAtRuntime && isActiveAndEnabled)
                buildRoutine = StartCoroutine(BuildTerrainMapTextureAsync());
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

            if (MapTexture != mapTextureOverride)
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

            if (mapTextureOverride != null || IsMapTextureReady)
                return;

            fallbackTexture = CreateFallbackTexture();
            MapTexture = fallbackTexture;
            IsMapTextureReady = true;

            if (buildTerrainTextureAtRuntime && buildRoutine == null)
                buildRoutine = StartCoroutine(BuildTerrainMapTextureAsync());
        }

        private void ResolveBounds()
        {
            if (useTerrainBounds)
            {
                if (terrain == null)
                    terrain = GetComponent<Terrain>();

                if (terrain == null)
                    terrain = FindAnyObjectByType<Terrain>();

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

            if (terrain == null)
                terrain = GetComponent<Terrain>();

            if (terrain == null)
                terrain = FindAnyObjectByType<Terrain>();

            TerrainData data = terrain != null ? terrain.terrainData : null;
            if (data == null)
                yield break;

            int resolution = Mathf.Clamp(mapTextureResolution, 64, 128);
            float maxHeight = Mathf.Max(0.001f, data.size.y);
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "RuntimeTerrainMap",
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
                    float height = data.GetInterpolatedHeight(sampleX, sampleY);
                    float normalized = Mathf.Clamp01(height / maxHeight);
                    pixels[y * resolution + x] = Color.Lerp(lowlandColor, highlandColor, normalized);
                }

                yield return null;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            runtimeGeneratedTexture = texture;
            MapTexture = texture;
            IsMapTextureReady = true;

            DestroyTexture(ref fallbackTexture);
            MapTextureReady?.Invoke();
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
