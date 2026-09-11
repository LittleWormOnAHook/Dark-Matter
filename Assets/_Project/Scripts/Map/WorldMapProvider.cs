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
        private const string FakeMapAssetPath = "Assets/_Project/World/WorldMap/Io_Plan_BiomeMap_TopDown.png";
        private const string FakeMapLegacyAssetPath = "Assets/_Project/Textures/UI/FakeMap.png";
        private const string MinimapMapResourcePath = "UI/FakeMap";

        /// <summary>Gaia DM Genesis: 4×4 tiles × 2048 m ≈ 8.19 km playable span.</summary>
        public const float MultiTerrainWorldSizeMeters = 8192f;
        public static readonly Vector3 MultiTerrainWorldOrigin = new Vector3(-4096f, 0f, -4096f);

        public static WorldMapProvider Instance { get; private set; }

        public const string CalibrationResourcesPath = DMWorldMapCalibrationProfile.ResourcesPath;

        [SerializeField] private DMWorldMapCalibrationProfile calibrationProfile;
        [SerializeField] private Terrain terrain;
        [SerializeField] private bool useTerrainBounds = true;
        [Tooltip("When enabled, map UVs use the full Gaia 4×4 terrain span even if only nearby tiles are loaded.")]
        [SerializeField] private bool useGaiaMultiTerrainBounds = true;
        [SerializeField] private Vector2 manualWorldSize = new Vector2(MultiTerrainWorldSizeMeters, MultiTerrainWorldSizeMeters);
        [SerializeField] private Vector3 manualWorldOrigin = MultiTerrainWorldOrigin;
        [Tooltip("When set (or a hierarchy object named Map Zero exists), map UVs anchor to this XZ corner.")]
        [SerializeField] private Transform mapOriginMarker;
        [SerializeField] private bool preferMapOriginMarker = true;
        [Tooltip("When enabled, Map Zero position is the map minimum XZ corner. When disabled, it is the map center.")]
        [SerializeField] private bool mapOriginIsMinCorner = false;
        [SerializeField] private int mapTextureResolution = 512;
        [SerializeField] private Texture2D mapTextureOverride;
        [SerializeField] private bool buildTerrainTextureAtRuntime = true;
        [Tooltip("When a terrain exists, bake the live terrain map instead of the static FakeMap texture.")]
        [SerializeField] private bool preferTerrainGeneratedMap = false;
        [Tooltip("Pull map texture from hierarchy MAP art (WorldMapArtReference) when no override is assigned.")]
        [SerializeField] private bool useMapArtReference = true;
        [Tooltip("Render a top-down camera snapshot of the terrain (matches in-scene look). Falls back to height/splat bake.")]
        [SerializeField] private bool useCameraTerrainSnapshot = true;
        [Tooltip("Flip map V in world→UV math when authored north is texture bottom.")]
        [SerializeField] private bool invertMapVertical = false;
        [SerializeField] private bool invertMapHorizontal = false;
        [Tooltip("Extra yaw added to minimap/compass heading ( -90 = world +X reads as North ).")]
        [SerializeField] private float mapDisplayNorthOffsetDegrees = -90f;
        [Tooltip("Fine-tune world→UV alignment after affine calibration.")]
        [SerializeField] private Vector2 mapUvOffset = Vector2.zero;
        [Tooltip("Static full-map player arrow base rotation (degrees). Driven by the calibration profile.")]
        [SerializeField] private float mapPlayerIconBaseDegrees;
        [SerializeField] private Vector2 mapPlayerIconUvOffset;
        [Header("Grid 0,0 calibration")]
        [Tooltip("Texture UV where world grid origin (Map Zero) sits on the authored map.")]
        [SerializeField] private Vector2 mapZeroUv01 = new Vector2(0.518f, 0.498f);
        [Tooltip("Known world XZ (grid coords) for the second calibration point.")]
        [SerializeField] private Vector2 mapCalibrationWorldXz = new Vector2(-767f, -108f);
        [Tooltip("Texture UV for that second calibration point on the authored map.")]
        [SerializeField] private Vector2 mapCalibrationUv01 = new Vector2(0.563f, 0.440f);
        [SerializeField] private bool useAffineMapProjection = true;
        [SerializeField] private Texture2D minimapTextureOverride;

        [Header("Terrain Map Colors")]
        [SerializeField] private Color lowlandColor = new Color(0.12f, 0.24f, 0.14f, 1f);
        [SerializeField] private Color highlandColor = new Color(0.45f, 0.42f, 0.32f, 1f);

        public Bounds WorldBounds { get; private set; }
        public Texture2D MapTexture { get; private set; }
        public Texture2D MinimapTexture { get; private set; }
        public bool IsMapTextureReady { get; private set; }
        public bool InvertMapVertical => ActiveCalibration.invertMapVertical;
        public bool InvertMapHorizontal => ActiveCalibration.invertMapHorizontal;
        public bool MinimapFlipHorizontal => ActiveCalibration.minimapFlipHorizontal;
        public bool MinimapFlipVertical => ActiveCalibration.minimapFlipVertical;
        public Vector3 MinimapRotateEuler => new Vector3(
            ActiveCalibration.minimapRotateX,
            ActiveCalibration.minimapRotateY,
            ActiveCalibration.minimapRotateZ);
        public float MapDisplayNorthOffsetDegrees => ActiveCalibration.mapDisplayNorthOffsetDegrees;
        public Vector2 MapUvOffset => ActiveCalibration.mapUvOffset;
        public float MapPlayerIconBaseDegrees => ActiveCalibration.mapPlayerIconBaseDegrees;
        public float MinimapPlayerIconBaseDegrees => ActiveCalibration.minimapPlayerIconBaseDegrees;
        public Vector2 MapPlayerIconUvOffset => ActiveCalibration.mapPlayerIconUvOffset;
        public float MapPlayerIconSizePixels => ActiveCalibration.mapPlayerIconSizePixels;
        public float MinimapPlayerIconSizePixels => ActiveCalibration.minimapPlayerIconSizePixels;
        public Vector3 MapGridOriginWorld => ResolveMapGridOriginWorld();
        public DMWorldMapCalibrationProfile CalibrationProfile => ResolveCalibrationProfile();

        public event Action MapTextureReady;
        public event Action WorldBoundsChanged;
        public event Action CalibrationChanged;

        private struct CalibrationState
        {
            public bool invertMapVertical;
            public bool invertMapHorizontal;
            public float mapDisplayNorthOffsetDegrees;
            public Vector2 mapUvOffset;
            public float mapUvScaleX;
            public float mapUvScaleY;
            public bool minimapFlipHorizontal;
            public bool minimapFlipVertical;
            public float minimapRotateX;
            public float minimapRotateY;
            public float minimapRotateZ;
            public float mapPlayerIconBaseDegrees;
            public float minimapPlayerIconBaseDegrees;
            public Vector2 mapPlayerIconUvOffset;
            public float mapPlayerIconSizePixels;
            public float minimapPlayerIconSizePixels;
            public Vector2 mapZeroUv01;
            public Vector2 mapCalibrationWorldXz;
            public Vector2 mapCalibrationUv01;
            public bool useAffineMapProjection;
        }

        private CalibrationState ActiveCalibration => BuildCalibrationState();

        private Coroutine buildRoutine;
        private Texture2D runtimeGeneratedTexture;
        private Texture2D fallbackTexture;
        private static Texture2D cachedFakeMapTexture;
        private static Texture2D cachedMinimapTexture;
        private Transform cachedMapOriginMarker;

        internal static void ResetStaticState()
        {
            Instance = null;
            cachedFakeMapTexture = null;
            cachedMinimapTexture = null;
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
            EnsureCalibrationProfile();
            TryApplyMapArtReference();
            EnsureCalibrationRuntime();
            RefreshWorldBounds();

            if (mapTextureOverride == null && !ShouldPreferTerrainGeneratedMap())
                mapTextureOverride = LoadFakeMapTexture();

            if (minimapTextureOverride == null && !ShouldPreferTerrainGeneratedMap())
                minimapTextureOverride = LoadMinimapMapTexture();

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
            WorldBoundsChanged?.Invoke();
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

        public Vector2 WorldToMapRaw01(Vector3 worldPosition)
        {
            if (ActiveCalibration.useAffineMapProjection)
                return ProjectWorldToMapAffine(worldPosition, applyFineTuneOffset: false);

            Vector3 min = WorldBounds.min;
            Vector3 max = WorldBounds.max;
            float x = Mathf.InverseLerp(min.x, max.x, worldPosition.x);
            float z = Mathf.InverseLerp(min.z, max.z, worldPosition.z);
            if (ActiveCalibration.invertMapVertical)
                z = 1f - z;
            return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(z));
        }

        public Vector2 WorldToMap01(Vector3 worldPosition)
        {
            if (ActiveCalibration.useAffineMapProjection)
                return ProjectWorldToMapAffine(worldPosition, applyFineTuneOffset: true);

            return ApplyMapUvOffset(WorldToMapRaw01(worldPosition));
        }

        /// <summary>
        /// UV on the painted map after the player-icon calibration nudge.
        /// Use for the player token and POI dots so they sit on the same texture.
        /// </summary>
        public Vector2 WorldToPlayerMap01(Vector3 worldPosition)
        {
            return WorldToMap01(worldPosition) + MapPlayerIconUvOffset;
        }

        public Vector2 GetCircularRevealUvRadius(float radiusMeters)
        {
            Vector2 uvPerMeter = GetMapUvPerWorldMeter();
            float iso = 0.5f * (Mathf.Abs(uvPerMeter.x) + Mathf.Abs(uvPerMeter.y));
            if (iso < 0.0000001f)
            {
                float span = Mathf.Max(WorldBounds.size.x, WorldBounds.size.z);
                iso = span > 1f ? 1f / span : 0.0001f;
            }

            float radiusUv = Mathf.Max(0.00001f, radiusMeters * iso);
            float aspect = 1f;
            if (MapTexture != null && MapTexture.height > 0)
                aspect = MapTexture.width / (float)MapTexture.height;

            return new Vector2(radiusUv, radiusUv * aspect);
        }

        public Vector3 Map01ToWorld(Vector2 map01)
        {
            Vector2 raw = RemoveMapUvOffset(map01);

            if (ActiveCalibration.useAffineMapProjection)
            {
                CalibrationState calibration = ActiveCalibration;
                Vector2 uvPerMeter = GetMapUvPerWorldMeter();
                Vector3 origin = ResolveMapGridOriginWorld();
                float worldX = origin.x;
                float worldZ = origin.z;
                float axisX = 0f;
                float axisZ = 0f;
                if (!Mathf.Approximately(uvPerMeter.x, 0f))
                    axisX = (raw.x - calibration.mapZeroUv01.x) / uvPerMeter.x;
                if (!Mathf.Approximately(uvPerMeter.y, 0f))
                    axisZ = (raw.y - calibration.mapZeroUv01.y) / uvPerMeter.y;
                MapAxesToWorldDelta(axisX, axisZ, out float relX, out float relZ);
                worldX += relX;
                worldZ += relZ;
                return new Vector3(worldX, WorldBounds.center.y, worldZ);
            }

            Vector3 min = WorldBounds.min;
            Vector3 max = WorldBounds.max;
            float mapZ = raw.y;
            if (ActiveCalibration.invertMapVertical)
                mapZ = 1f - mapZ;
            return new Vector3(
                Mathf.Lerp(min.x, max.x, raw.x),
                WorldBounds.center.y,
                Mathf.Lerp(min.z, max.z, mapZ));
        }

        public Vector2 WorldToGridXz(Vector3 worldPosition)
        {
            Vector3 origin = ResolveMapGridOriginWorld();
            return new Vector2(
                worldPosition.x - origin.x,
                worldPosition.z - origin.z);
        }

        public float GetMapDisplayYaw(float facingYawDegrees)
        {
            CalibrationState calibration = ActiveCalibration;
            return NormalizeDegrees(facingYawDegrees + calibration.mapDisplayNorthOffsetDegrees);
        }

        /// <summary>
        /// Compass / minimap / player-arrow yaw. Heading is 180° from
        /// <see cref="GetMapDisplayYaw"/> so NEWS matches player facing.
        /// </summary>
        public float GetMapCompassYaw(float facingYawDegrees)
        {
            return NormalizeDegrees(GetMapDisplayYaw(facingYawDegrees) + 180f);
        }

        public Vector3 GetMinimapViewEuler(float headingYawDegrees)
        {
            Vector3 extra = MinimapRotateEuler;
            return new Vector3(extra.x, extra.y, extra.z - headingYawDegrees);
        }

        /// <summary>Map-display bearing from a world XZ delta (matches MapCompassYaw space).</summary>
        public float WorldDeltaToDisplayBearing(Vector3 worldDelta)
        {
            worldDelta.y = 0f;
            if (worldDelta.sqrMagnitude <= 0.0001f)
                return 0f;

            float worldBearing = Mathf.Atan2(worldDelta.x, worldDelta.z) * Mathf.Rad2Deg;
            return GetMapCompassYaw(worldBearing);
        }

        public static float WorldDeltaToDisplayBearing(Vector3 fromWorld, Vector3 toWorld)
        {
            WorldMapProvider provider = Instance;
            Vector3 delta = toWorld - fromWorld;
            return provider != null
                ? provider.WorldDeltaToDisplayBearing(delta)
                : NormalizeDegrees(Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg + 90f);
        }

        public void NotifyCalibrationChanged()
        {
            RefreshWorldBounds();
            CalibrationChanged?.Invoke();
            WorldBoundsChanged?.Invoke();
        }

        public void ApplyCalibrationToScene(DMWorldMapCalibrationProfile profile)
        {
            if (profile == null)
                return;

            invertMapVertical = profile.invertMapVertical;
            invertMapHorizontal = profile.invertMapHorizontal;
            mapDisplayNorthOffsetDegrees = profile.mapDisplayNorthOffsetDegrees;
            mapUvOffset = profile.mapUvOffset;
            mapPlayerIconBaseDegrees = profile.mapPlayerIconBaseDegrees;
            mapPlayerIconUvOffset = profile.mapPlayerIconUvOffset;
            mapZeroUv01 = profile.mapZeroUv01;
            mapCalibrationWorldXz = profile.mapCalibrationWorldXz;
            mapCalibrationUv01 = profile.mapCalibrationUv01;
            useAffineMapProjection = profile.useAffineMapProjection;
            calibrationProfile = profile;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private Vector2 ApplyMapUvOffset(Vector2 uv)
        {
            CalibrationState calibration = ActiveCalibration;
            return new Vector2(
                uv.x + calibration.mapUvOffset.x,
                uv.y + calibration.mapUvOffset.y);
        }

        private Vector2 RemoveMapUvOffset(Vector2 uv)
        {
            CalibrationState calibration = ActiveCalibration;
            return new Vector2(
                uv.x - calibration.mapUvOffset.x,
                uv.y - calibration.mapUvOffset.y);
        }

        private Vector3 ResolveMapGridOriginWorld()
        {
            WorldMapOriginMarker originMarker = WorldMapOriginMarker.FindInScene();
            if (originMarker != null)
                return originMarker.transform.position;

            if (mapOriginMarker != null)
                return mapOriginMarker.position;

            if (cachedMapOriginMarker != null)
                return cachedMapOriginMarker.position;

            return Vector3.zero;
        }

        private Vector2 GetMapUvPerWorldMeter()
        {
            CalibrationState calibration = ActiveCalibration;
            Vector2 deltaWorld = calibration.mapCalibrationWorldXz;
            Vector2 deltaUv = calibration.mapCalibrationUv01 - calibration.mapZeroUv01;
            float worldLen = deltaWorld.magnitude;
            float uvLen = deltaUv.magnitude;
            float iso = worldLen > 0.01f ? uvLen / worldLen : 0f;
            return new Vector2(
                iso * Mathf.Max(0.01f, calibration.mapUvScaleX),
                iso * Mathf.Max(0.01f, calibration.mapUvScaleY));
        }

        private Vector2 ProjectWorldToMapAffine(Vector3 worldPosition, bool applyFineTuneOffset)
        {
            CalibrationState calibration = ActiveCalibration;
            Vector3 origin = ResolveMapGridOriginWorld();
            Vector2 uvPerMeter = GetMapUvPerWorldMeter();
            float relX = worldPosition.x - origin.x;
            float relZ = worldPosition.z - origin.z;
            WorldDeltaToMapAxes(relX, relZ, out float mapX, out float mapZ);
            Vector2 uv = new Vector2(
                calibration.mapZeroUv01.x + mapX * uvPerMeter.x,
                calibration.mapZeroUv01.y + mapZ * uvPerMeter.y);
            return applyFineTuneOffset ? ApplyMapUvOffset(uv) : uv;
        }

        /// <summary>
        /// Compass north is world +X. Map north is +UV.y. Fixed 90° basis so
        /// walking north moves the token up, not east. Do not use heading offset.
        /// </summary>
        private static void WorldDeltaToMapAxes(float relX, float relZ, out float mapX, out float mapZ)
        {
            mapX = -relZ;
            mapZ = relX;
        }

        private static void MapAxesToWorldDelta(float mapX, float mapZ, out float relX, out float relZ)
        {
            relX = mapZ;
            relZ = -mapX;
        }

        private CalibrationState BuildCalibrationState()
        {
            DMWorldMapCalibrationProfile profile = ResolveCalibrationProfile();
            if (profile != null)
            {
                return new CalibrationState
                {
                    invertMapVertical = profile.invertMapVertical,
                    invertMapHorizontal = profile.invertMapHorizontal,
                    mapDisplayNorthOffsetDegrees = profile.mapDisplayNorthOffsetDegrees,
                    mapUvOffset = profile.mapUvOffset,
                    mapUvScaleX = profile.mapUvScaleX,
                    mapUvScaleY = profile.mapUvScaleY,
                    minimapFlipHorizontal = profile.minimapFlipHorizontal,
                    minimapFlipVertical = profile.minimapFlipVertical,
                    minimapRotateX = profile.minimapRotateX,
                    minimapRotateY = profile.minimapRotateY,
                    minimapRotateZ = profile.minimapRotateZ,
                    mapPlayerIconBaseDegrees = profile.mapPlayerIconBaseDegrees,
                    minimapPlayerIconBaseDegrees = profile.minimapPlayerIconBaseDegrees,
                    mapPlayerIconUvOffset = profile.mapPlayerIconUvOffset,
                    mapPlayerIconSizePixels = profile.mapPlayerIconSizePixels,
                    minimapPlayerIconSizePixels = profile.minimapPlayerIconSizePixels,
                    mapZeroUv01 = profile.mapZeroUv01,
                    mapCalibrationWorldXz = profile.mapCalibrationWorldXz,
                    mapCalibrationUv01 = profile.mapCalibrationUv01,
                    useAffineMapProjection = profile.useAffineMapProjection,
                };
            }

            return new CalibrationState
            {
                invertMapVertical = invertMapVertical,
                invertMapHorizontal = invertMapHorizontal,
                mapDisplayNorthOffsetDegrees = mapDisplayNorthOffsetDegrees,
                mapUvOffset = mapUvOffset,
                mapUvScaleX = 1f,
                mapUvScaleY = 1f,
                mapPlayerIconBaseDegrees = mapPlayerIconBaseDegrees,
                minimapPlayerIconBaseDegrees = 180f,
                mapPlayerIconUvOffset = mapPlayerIconUvOffset,
                mapPlayerIconSizePixels = 18f,
                minimapPlayerIconSizePixels = 24f,
                mapZeroUv01 = mapZeroUv01,
                mapCalibrationWorldXz = mapCalibrationWorldXz,
                mapCalibrationUv01 = mapCalibrationUv01,
                useAffineMapProjection = useAffineMapProjection,
            };
        }

        private DMWorldMapCalibrationProfile ResolveCalibrationProfile()
        {
            if (calibrationProfile != null)
                return calibrationProfile;

            calibrationProfile = Resources.Load<DMWorldMapCalibrationProfile>(CalibrationResourcesPath);
            return calibrationProfile;
        }

        private void EnsureCalibrationProfile()
        {
            if (calibrationProfile != null)
                return;

            calibrationProfile = Resources.Load<DMWorldMapCalibrationProfile>(CalibrationResourcesPath);
        }

        private void EnsureCalibrationRuntime()
        {
            DMWorldMapCalibrationProfile profile = ResolveCalibrationProfile();
            if (profile == null || !profile.enableRuntimeTuning)
                return;

            if (GetComponent<DMWorldMapCalibrationRuntime>() == null)
                gameObject.AddComponent<DMWorldMapCalibrationRuntime>();
        }

        private static float NormalizeDegrees(float degrees)
        {
            degrees %= 360f;
            return degrees < 0f ? degrees + 360f : degrees;
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
                MinimapTexture = minimapTextureOverride != null ? minimapTextureOverride : LoadMinimapMapTexture();
                if (MinimapTexture == null)
                    MinimapTexture = interim;
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
            MinimapTexture = minimapTextureOverride != null ? minimapTextureOverride : LoadMinimapMapTexture();
            if (MinimapTexture == null)
                MinimapTexture = texture;
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

#if UNITY_EDITOR
            cachedFakeMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FakeMapAssetPath);
            if (cachedFakeMapTexture == null)
                cachedFakeMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FakeMapLegacyAssetPath);
#endif
            if (cachedFakeMapTexture == null)
                cachedFakeMapTexture = Resources.Load<Texture2D>(FakeMapResourcePath);

            return cachedFakeMapTexture;
        }

        public static Texture2D LoadMinimapMapTexture()
        {
            if (cachedMinimapTexture != null)
                return cachedMinimapTexture;

            cachedMinimapTexture = Resources.Load<Texture2D>(MinimapMapResourcePath);
#if UNITY_EDITOR
            if (cachedMinimapTexture == null)
                cachedMinimapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Resources/UI/FakeMap.png");
            if (cachedMinimapTexture == null)
                cachedMinimapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FakeMapLegacyAssetPath);
#endif
            if (cachedMinimapTexture == null)
                cachedMinimapTexture = LoadFakeMapTexture();

            return cachedMinimapTexture;
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
            if (TryResolveOriginMarkerBounds(out Bounds originBounds))
            {
                WorldBounds = originBounds;
                return;
            }

            if (TryResolveMapArtBounds(out Bounds artBounds))
            {
                WorldBounds = artBounds;
                return;
            }

            if (useGaiaMultiTerrainBounds)
            {
                Vector3 flatSize = new Vector3(MultiTerrainWorldSizeMeters, 100f, MultiTerrainWorldSizeMeters);
                WorldBounds = new Bounds(MultiTerrainWorldOrigin + flatSize * 0.5f, flatSize);
                return;
            }

            if (useTerrainBounds && TryResolveTerrainBounds(out Bounds terrainBounds))
            {
                WorldBounds = terrainBounds;
                return;
            }

            Vector3 manualSize = new Vector3(manualWorldSize.x, 100f, manualWorldSize.y);
            WorldBounds = new Bounds(manualWorldOrigin + manualSize * 0.5f, manualSize);
        }

        private bool TryResolveOriginMarkerBounds(out Bounds bounds)
        {
            bounds = default;
            if (!preferMapOriginMarker)
                return false;

            WorldMapOriginMarker originMarker = WorldMapOriginMarker.FindInScene();
            if (originMarker != null)
            {
                bounds = originMarker.BuildWorldBounds();
                cachedMapOriginMarker = originMarker.transform;
                mapOriginMarker = originMarker.transform;
                return true;
            }

            Transform marker = mapOriginMarker != null ? mapOriginMarker : cachedMapOriginMarker;
            if (marker == null)
            {
                GameObject found = GameObject.Find(WorldMapOriginMarker.DefaultObjectName);
                if (found != null)
                    cachedMapOriginMarker = marker = found.transform;
            }

            if (marker == null)
                return false;

            WorldMapArtReference art = WorldMapArtReference.FindInScene();
            if (art != null)
            {
                Bounds artBounds = art.GetWorldBounds();
                if (artBounds.size.x > 0.01f && artBounds.size.z > 0.01f)
                {
                    Vector3 flatSize = new Vector3(artBounds.size.x, 100f, artBounds.size.z);
                    Vector3 minCorner = mapOriginIsMinCorner
                        ? new Vector3(artBounds.min.x, 0f, artBounds.min.z)
                        : artBounds.center - new Vector3(flatSize.x * 0.5f, 0f, flatSize.z * 0.5f);
                    bounds = new Bounds(minCorner + flatSize * 0.5f, flatSize);
                    return true;
                }
            }

            Vector2 worldSize = new Vector2(
                manualWorldSize.x > 0.01f ? manualWorldSize.x : MultiTerrainWorldSizeMeters,
                manualWorldSize.y > 0.01f ? manualWorldSize.y : MultiTerrainWorldSizeMeters);

            Vector3 legacyFlatSize = new Vector3(worldSize.x, 100f, worldSize.y);
            Vector3 legacyMinCorner = mapOriginIsMinCorner
                ? new Vector3(marker.position.x, 0f, marker.position.z)
                : marker.position - new Vector3(legacyFlatSize.x * 0.5f, 0f, legacyFlatSize.z * 0.5f);
            bounds = new Bounds(legacyMinCorner + legacyFlatSize * 0.5f, legacyFlatSize);
            return true;
        }

        private static bool TryResolveMapArtBounds(out Bounds bounds)
        {
            bounds = default;
            WorldMapArtReference art = WorldMapArtReference.FindInScene();
            if (art == null)
                return false;

            bounds = art.GetWorldBounds();
            return bounds.size.x > 0.01f && bounds.size.z > 0.01f;
        }

        private void TryApplyMapArtReference()
        {
            if (!useMapArtReference)
                return;

            WorldMapArtReference artReference = WorldMapArtReference.FindInScene();
            if (artReference == null)
                return;

            if (mapTextureOverride == null && artReference.TryGetMapTexture(out Texture2D artTexture))
            {
                mapTextureOverride = artTexture;
                preferTerrainGeneratedMap = false;
                buildTerrainTextureAtRuntime = false;
            }

            if (ResolveCalibrationProfile() == null)
            {
                invertMapVertical = artReference.InvertMapVertical;
                mapDisplayNorthOffsetDegrees = artReference.MapDisplayNorthOffsetDegrees;
                mapUvOffset = artReference.MapUvOffset;
                mapPlayerIconBaseDegrees = artReference.MapPlayerIconBaseDegrees;
                mapZeroUv01 = artReference.MapZeroUv01;
                mapCalibrationWorldXz = artReference.MapCalibrationWorldXz;
                mapCalibrationUv01 = artReference.MapCalibrationUv01;
                useAffineMapProjection = artReference.UseAffineMapProjection;
            }

            if (IsMapTextureReady && UsesStaticMapTexture())
            {
                TryApplyStaticMapTexture();
                MapTextureReady?.Invoke();
            }
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

            if (maxDimension > 7000f)
                scaled = Mathf.Max(scaled, 2048);

            return Mathf.Clamp(scaled, 128, 4096);
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
