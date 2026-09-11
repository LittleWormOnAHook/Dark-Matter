using System;
using Project.Core;
using Project.Player;
using Project.Progression;
using Project.UI;
using UnityEngine;

namespace Project.Map
{
    /// <summary>
    /// Gold fog-of-war over the world map. Walk reveals a soft 5m circle;
    /// scanner sweeps reveal a soft radius (40m base + skill ranks).
    /// </summary>
    [DisallowMultipleComponent]
    public class MapFogOfWar : MonoBehaviour
    {
        public const float DefaultWalkRevealRadiusMeters = 5f;
        public const float DefaultWalkRevealEdgeSoftnessMeters = 1.5f;
        public const float DefaultScanRevealRadiusMeters = 40f;
        public const float DefaultScanSkillBonusPerRankMeters = 10f;
        public const int DefaultMaxScanSkillRanks = 5;
        public const float DefaultScanRevealEdgeSoftnessMeters = 5f;
        public const float DefaultFogOverlayAlpha = 0.95f;
        public const float DefaultRevealThreshold = 0.35f;
        public const float DefaultWalkStampIntervalMeters = 1f;
        public const float DefaultTextureUploadInterval = 0.18f;
        /// <summary>1024 is sharp enough for minimap/pilot cluster; 2048 was 4× GPU upload cost.</summary>
        public const int DefaultFogResolution = 1024;
        public const float DefaultLandMaskGreenThreshold = 0.82f;

        public static float WalkRevealRadiusMeters =>
            ActiveProfile != null ? ActiveProfile.walkRevealRadiusMeters : DefaultWalkRevealRadiusMeters;

        public static float WalkRevealEdgeSoftnessMeters =>
            ActiveProfile != null ? ActiveProfile.walkRevealEdgeSoftnessMeters : DefaultWalkRevealEdgeSoftnessMeters;

        public static float BaseScanRevealRadiusMeters =>
            ActiveProfile != null ? ActiveProfile.scanRevealRadiusMeters : DefaultScanRevealRadiusMeters;

        public static float ScanSkillBonusPerRankMeters =>
            ActiveProfile != null ? ActiveProfile.scanSkillBonusPerRankMeters : DefaultScanSkillBonusPerRankMeters;

        public static int MaxScanSkillRanks =>
            ActiveProfile != null ? ActiveProfile.maxScanSkillRanks : DefaultMaxScanSkillRanks;

        public static float ScanRevealEdgeSoftnessMeters =>
            ActiveProfile != null ? ActiveProfile.scanRevealEdgeSoftnessMeters : DefaultScanRevealEdgeSoftnessMeters;

        public static float FogOverlayAlpha =>
            ActiveProfile != null ? ActiveProfile.fogOverlayAlpha : DefaultFogOverlayAlpha;

        public static float RevealThreshold =>
            ActiveProfile != null ? ActiveProfile.revealThreshold : DefaultRevealThreshold;

        /// <summary>Master toggle for map FOW overlay + reveal stamps.</summary>
        public static bool SystemEnabled { get; set; } = true;

        public const string TerrainMaskAssetPath =
            "Assets/_Project/Documentation/Design/ArtReference/WorldMap/DM Terrain Mask.jpg";

        public static MapFogOfWar Instance { get; private set; }

        private int fogResolution = DefaultFogResolution;
        private float walkStampIntervalMeters = DefaultWalkStampIntervalMeters;
        private float textureUploadInterval = DefaultTextureUploadInterval;
        private DMWorldMapCalibrationProfile profile;
        private float appliedLandMaskGreen = DefaultLandMaskGreenThreshold;
        private float appliedOverlayAlpha = DefaultFogOverlayAlpha;
        private Color appliedFogColor = DarkMatterGenesisUiPalette.Gold;
        private Texture2D appliedTerrainMask;

        private WorldMapProvider mapProvider;

        private static DMWorldMapCalibrationProfile ActiveProfile =>
            Instance != null && Instance.profile != null
                ? Instance.profile
                : DMWorldMapCalibrationProfile.LoadDefault();
        private byte[] revealMask;
        private byte[] landMask;
        private Texture2D fogTexture;
        private Texture2D terrainMaskSource;
        private Color32[] fogPixels;
        private bool textureDirty;
        private float nextUploadTime;
        private int dirtyMinX = int.MaxValue;
        private int dirtyMinY = int.MaxValue;
        private int dirtyMaxX = -1;
        private int dirtyMaxY = -1;
        private Vector3 lastWalkStampPosition = new Vector3(float.MaxValue, 0f, float.MaxValue);
        private Transform playerTransform;
        private bool fullyInitialized;

        public Texture2D FogTexture => fogTexture;
        public bool IsReady => fullyInitialized && fogTexture != null;
        public event Action FogUpdated;

        public static float GetScanRevealRadius()
        {
            float bonus = PlayerSkillAllocator.GetScanRangeBonusMeters();
            bonus = Mathf.Clamp(bonus, 0f, ScanSkillBonusPerRankMeters * Mathf.Max(0, MaxScanSkillRanks));
            return BaseScanRevealRadiusMeters + bonus;
        }

        internal static void ResetStaticState()
        {
            Instance = null;
        }

        public static MapFogOfWar EnsureExists()
        {
            if (Instance != null)
                return Instance;

            MapFogOfWar existing = FindAnyObjectByType<MapFogOfWar>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            GameObject host = new GameObject("MapFogOfWar");
            return host.AddComponent<MapFogOfWar>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            mapProvider = WorldMapProvider.Instance ?? FindAnyObjectByType<WorldMapProvider>();
            ApplyFromProfile(DMWorldMapCalibrationProfile.LoadDefault());
            EnsureBuffers();
            fullyInitialized = true;
            ApplyFromProfile(profile);
        }

        public static void ApplyEnabledFromProfile(DMWorldMapCalibrationProfile nextProfile)
        {
            ApplyFromProfile(nextProfile);
        }

        public static void ApplyFromProfile(DMWorldMapCalibrationProfile nextProfile)
        {
            if (nextProfile == null)
                return;

            nextProfile.ClampFogSettings();
            SystemEnabled = nextProfile.enableMapFogOfWar;

            MapFogOfWar instance = Instance;
            if (instance == null)
                return;

            instance.profile = nextProfile;
            instance.SyncLiveFromProfile();
            if (!instance.fullyInitialized)
                return;

            if (!SystemEnabled)
            {
                instance.textureDirty = false;
                return;
            }

            instance.ApplyVisualFromProfile(rebuildIfNeeded: true);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (fogTexture != null)
                Destroy(fogTexture);
        }

        private void LateUpdate()
        {
            if (!SystemEnabled || !GameSession.HasStarted || !fullyInitialized)
                return;

            SyncLiveFromProfile();

            if (mapProvider == null)
                mapProvider = WorldMapProvider.Instance ?? FindAnyObjectByType<WorldMapProvider>();

            EnsurePlayer();
            if (playerTransform == null || mapProvider == null)
                return;

            StampWalkReveal(playerTransform.position);

            if (textureDirty && Time.unscaledTime >= nextUploadTime)
                UploadTexture();
        }

        /// <summary>Rebind after terrain map bake/sync so FOW UVs and overlay stay valid.</summary>
        public void RebindAfterMapRefresh()
        {
            mapProvider = WorldMapProvider.Instance ?? FindAnyObjectByType<WorldMapProvider>();
            mapProvider?.RefreshWorldBounds();
            terrainMaskSource = null;
            landMask = null;
            ApplyFromProfile(DMWorldMapCalibrationProfile.LoadDefault());
            EnsureBuffers();
            EnsurePlayer();
            if (SystemEnabled && playerTransform != null)
                RevealCircle(playerTransform.position, WalkRevealRadiusMeters, WalkRevealEdgeSoftnessMeters);
            if (SystemEnabled)
                UploadTexture();
        }

        private void EnsurePlayer()
        {
            if (playerTransform != null)
                return;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                playerTransform = player.transform;
        }

        private void SyncLiveFromProfile()
        {
            DMWorldMapCalibrationProfile next = profile != null ? profile : DMWorldMapCalibrationProfile.LoadDefault();
            profile = next;
            if (next == null)
                return;

            walkStampIntervalMeters = Mathf.Max(0.1f, next.walkStampIntervalMeters);
            textureUploadInterval = Mathf.Max(0.02f, next.textureUploadInterval);
        }

        private void ApplyVisualFromProfile(bool rebuildIfNeeded)
        {
            DMWorldMapCalibrationProfile next = profile;
            if (next == null || !rebuildIfNeeded)
                return;

            int newRes = Mathf.Clamp(next.fogResolution, 64, 4096);
            bool resChanged = newRes != fogResolution || fogTexture == null || fogTexture.width != newRes;
            bool maskChanged = !Mathf.Approximately(appliedLandMaskGreen, next.landMaskGreenThreshold)
                || appliedTerrainMask != next.terrainMask;
            bool lookChanged = !Mathf.Approximately(appliedOverlayAlpha, next.fogOverlayAlpha)
                || appliedFogColor != next.fogColor;

            if (resChanged)
            {
                fogResolution = newRes;
                landMask = null;
                terrainMaskSource = null;
                EnsureBuffers();
                CaptureAppliedLook(next);
                return;
            }

            if (maskChanged)
            {
                landMask = null;
                terrainMaskSource = null;
                BakeLandMask(fogResolution, fogResolution * fogResolution);
                RebuildFogPixelsFromMask();
                textureDirty = true;
                UploadTexture();
                CaptureAppliedLook(next);
                return;
            }

            if (lookChanged)
            {
                RebuildFogPixelsFromMask();
                textureDirty = true;
                UploadTexture();
                CaptureAppliedLook(next);
            }
        }

        private void CaptureAppliedLook(DMWorldMapCalibrationProfile next)
        {
            appliedLandMaskGreen = next.landMaskGreenThreshold;
            appliedOverlayAlpha = next.fogOverlayAlpha;
            appliedFogColor = next.fogColor;
            appliedTerrainMask = next.terrainMask;
        }

        private void EnsureBuffers()
        {
            int res = ResolveFogResolution();
            fogResolution = res;
            int count = res * res;

            if (revealMask == null || revealMask.Length != count)
                revealMask = new byte[count];

            if (fogPixels == null || fogPixels.Length != count)
                fogPixels = new Color32[count];

            BakeLandMask(res, count);

            if (fogTexture == null || fogTexture.width != res)
            {
                if (fogTexture != null)
                    Destroy(fogTexture);

                fogTexture = new Texture2D(res, res, TextureFormat.RGBA32, false, true)
                {
                    name = "MapFogOfWar",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave
                };
            }

            RebuildFogPixelsFromMask();
            fogTexture.SetPixels32(fogPixels);
            fogTexture.Apply(false, false);
            textureDirty = false;
        }

        private int ResolveFogResolution()
        {
            int target = profile != null ? profile.fogResolution : fogResolution;
            return Mathf.Clamp(target, 64, 4096);
        }

        public void RevealCircle(Vector3 worldPosition, float radiusMeters, float edgeSoftnessMeters = 2f)
        {
            if (!SystemEnabled)
                return;

            if (!fullyInitialized)
                EnsureBuffers();

            if (mapProvider == null)
                mapProvider = WorldMapProvider.Instance ?? FindAnyObjectByType<WorldMapProvider>();

            if (mapProvider == null || revealMask == null)
                return;

            Vector2 uv = mapProvider.WorldToPlayerMap01(worldPosition);
            Vector2 radiusUv = mapProvider.GetCircularRevealUvRadius(radiusMeters);
            float radiusUvX = Mathf.Max(0.0001f, radiusUv.x);
            float radiusUvY = Mathf.Max(0.0001f, radiusUv.y);
            float iso = 0.5f * (radiusUvX + radiusUvY);
            radiusUvX = iso;
            radiusUvY = iso;
            float softUv = Mathf.Max(0.0001f, mapProvider.GetCircularRevealUvRadius(Mathf.Max(0f, edgeSoftnessMeters)).x);

            int res = fogResolution;
            int minX = Mathf.Clamp(Mathf.FloorToInt((uv.x - radiusUvX - softUv) * res), 0, res - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt((uv.x + radiusUvX + softUv) * res), 0, res - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt((uv.y - radiusUvY - softUv) * res), 0, res - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt((uv.y + radiusUvY + softUv) * res), 0, res - 1);

            bool changed = false;
            for (int y = minY; y <= maxY; y++)
            {
                float v = (y + 0.5f) / res;
                for (int x = minX; x <= maxX; x++)
                {
                    float u = (x + 0.5f) / res;
                    float dx = (u - uv.x) / Mathf.Max(0.0001f, radiusUvX);
                    float dy = (v - uv.y) / radiusUvY;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > 1f + softUv / Mathf.Max(0.0001f, radiusUvX))
                        continue;

                    float strength = dist <= 1f
                        ? 1f
                        : 1f - Mathf.Clamp01((dist - 1f) / Mathf.Max(0.0001f, softUv / Mathf.Max(0.0001f, radiusUvX)));

                    byte value = (byte)Mathf.Clamp(Mathf.RoundToInt(strength * 255f), 0, 255);
                    int index = y * res + x;
                    if (revealMask[index] >= value)
                        continue;

                    revealMask[index] = value;
                    WriteFogPixel(index);
                    ExpandDirtyRect(x, y);
                    changed = true;
                }
            }

            if (changed)
                textureDirty = true;
        }

        public void RevealScanAt(Vector3 worldPosition)
        {
            if (!SystemEnabled)
                return;

            RevealCircle(worldPosition, GetScanRevealRadius(), ScanRevealEdgeSoftnessMeters);
            UploadTexture();
        }

        public bool IsWorldRevealed(Vector3 worldPosition, float threshold = -1f)
        {
            if (threshold < 0f)
                threshold = RevealThreshold;

            if (!SystemEnabled)
                return true;

            if (!fullyInitialized || revealMask == null || mapProvider == null)
                return false;

            Vector2 uv = mapProvider.WorldToPlayerMap01(worldPosition);
            int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * fogResolution), 0, fogResolution - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * fogResolution), 0, fogResolution - 1);
            return revealMask[y * fogResolution + x] / 255f >= threshold;
        }

        private void StampWalkReveal(Vector3 worldPosition)
        {
            float moved = Vector3.Distance(
                new Vector3(worldPosition.x, 0f, worldPosition.z),
                new Vector3(lastWalkStampPosition.x, 0f, lastWalkStampPosition.z));

            if (moved < walkStampIntervalMeters && lastWalkStampPosition.x < float.MaxValue * 0.5f)
                return;

            lastWalkStampPosition = worldPosition;
            RevealCircle(worldPosition, WalkRevealRadiusMeters, WalkRevealEdgeSoftnessMeters);
        }

        private void BakeLandMask(int res, int count)
        {
            if (landMask != null && landMask.Length == count && terrainMaskSource != null)
                return;

            Texture2D mask = terrainMaskSource != null ? terrainMaskSource : LoadTerrainMask();
            terrainMaskSource = mask;
            landMask = new byte[count];
            if (mask == null || !mask.isReadable)
            {
                for (int i = 0; i < count; i++)
                    landMask[i] = 1;
                return;
            }

            float greenCut = profile != null ? profile.landMaskGreenThreshold : DefaultLandMaskGreenThreshold;
            for (int y = 0; y < res; y++)
            {
                float v = (y + 0.5f) / res;
                for (int x = 0; x < res; x++)
                {
                    float u = (x + 0.5f) / res;
                    Color c = mask.GetPixelBilinear(u, v);
                    landMask[y * res + x] = c.g < greenCut ? (byte)1 : (byte)0;
                }
            }
        }

        private Texture2D LoadTerrainMask()
        {
            if (profile != null && profile.terrainMask != null)
                return profile.terrainMask;

#if UNITY_EDITOR
            Texture2D editorMask = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainMaskAssetPath);
            if (editorMask != null)
                return editorMask;
#endif
            return Resources.Load<Texture2D>("WorldMap/DM Terrain Mask");
        }

        private void RebuildFogPixelsFromMask()
        {
            for (int i = 0; i < revealMask.Length; i++)
                WriteFogPixel(i);
        }

        private void WriteFogPixel(int index)
        {
            if (fogPixels == null || index < 0 || index >= fogPixels.Length)
                return;

            if (landMask != null && index < landMask.Length && landMask[index] == 0)
            {
                fogPixels[index] = new Color32(0, 0, 0, 0);
                return;
            }

            Color gold = profile != null ? profile.fogColor : DarkMatterGenesisUiPalette.Gold;
            float revealed = revealMask != null && index < revealMask.Length ? revealMask[index] / 255f : 0f;
            byte a = (byte)Mathf.Clamp(Mathf.RoundToInt((1f - revealed) * FogOverlayAlpha * 255f), 0, 255);
            fogPixels[index] = new Color32(
                (byte)Mathf.RoundToInt(gold.r * 255f),
                (byte)Mathf.RoundToInt(gold.g * 255f),
                (byte)Mathf.RoundToInt(gold.b * 255f),
                a);
        }

        private void ExpandDirtyRect(int x, int y)
        {
            if (x < dirtyMinX) dirtyMinX = x;
            if (y < dirtyMinY) dirtyMinY = y;
            if (x > dirtyMaxX) dirtyMaxX = x;
            if (y > dirtyMaxY) dirtyMaxY = y;
        }

        private void ClearDirtyRect()
        {
            dirtyMinX = int.MaxValue;
            dirtyMinY = int.MaxValue;
            dirtyMaxX = -1;
            dirtyMaxY = -1;
        }

        private void UploadTexture()
        {
            if (fogTexture == null || revealMask == null || fogPixels == null)
                return;

            int res = fogResolution;
            bool hasDirtyRect = dirtyMaxX >= dirtyMinX && dirtyMaxY >= dirtyMinY
                && dirtyMinX >= 0 && dirtyMinY >= 0
                && dirtyMaxX < res && dirtyMaxY < res;

            if (hasDirtyRect)
            {
                int width = dirtyMaxX - dirtyMinX + 1;
                int height = dirtyMaxY - dirtyMinY + 1;
                Color32[] block = new Color32[width * height];
                for (int y = 0; y < height; y++)
                {
                    int src = (dirtyMinY + y) * res + dirtyMinX;
                    System.Array.Copy(fogPixels, src, block, y * width, width);
                }

                fogTexture.SetPixels32(dirtyMinX, dirtyMinY, width, height, block);
            }
            else
            {
                RebuildFogPixelsFromMask();
                fogTexture.SetPixels32(fogPixels);
            }

            fogTexture.Apply(false, false);
            textureDirty = false;
            ClearDirtyRect();
            nextUploadTime = Time.unscaledTime + textureUploadInterval;
            FogUpdated?.Invoke();
        }

        public byte[] BuildSave()
        {
            if (revealMask == null)
                return Array.Empty<byte>();

            byte[] copy = new byte[revealMask.Length];
            Buffer.BlockCopy(revealMask, 0, copy, 0, revealMask.Length);
            return copy;
        }

        public int BuildSaveResolution() => fogResolution;

        public void ApplySave(byte[] savedMask, int savedResolution)
        {
            if (!SystemEnabled)
                return;

            EnsureBuffers();

            if (savedMask == null || savedMask.Length == 0)
                return;

            if (savedResolution == fogResolution && savedMask.Length == revealMask.Length)
            {
                Buffer.BlockCopy(savedMask, 0, revealMask, 0, revealMask.Length);
            }
            else
            {
                int srcRes = savedResolution > 0
                    ? savedResolution
                    : Mathf.RoundToInt(Mathf.Sqrt(savedMask.Length));
                srcRes = Mathf.Max(1, srcRes);

                for (int y = 0; y < fogResolution; y++)
                {
                    int sy = Mathf.Clamp(y * srcRes / fogResolution, 0, srcRes - 1);
                    for (int x = 0; x < fogResolution; x++)
                    {
                        int sx = Mathf.Clamp(x * srcRes / fogResolution, 0, srcRes - 1);
                        int srcIndex = sy * srcRes + sx;
                        if (srcIndex >= 0 && srcIndex < savedMask.Length)
                            revealMask[y * fogResolution + x] = savedMask[srcIndex];
                    }
                }
            }

            UploadTexture();
        }

        public void ClearAllFog()
        {
            EnsureBuffers();
            Array.Clear(revealMask, 0, revealMask.Length);
            UploadTexture();
        }

        /// <summary>Clears explored fog and restamps a walk hole at the player.</summary>
        public void ResetCoverage()
        {
            lastWalkStampPosition = new Vector3(float.MaxValue, 0f, float.MaxValue);
            ClearAllFog();
            if (!SystemEnabled)
                return;

            EnsurePlayer();
            if (playerTransform != null)
                RevealCircle(playerTransform.position, WalkRevealRadiusMeters, WalkRevealEdgeSoftnessMeters);
            UploadTexture();
        }
    }
}
