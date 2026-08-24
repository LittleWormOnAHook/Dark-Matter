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
        public const float WalkRevealRadiusMeters = 5f;
        public const float BaseScanRevealRadiusMeters = 40f;
        public const float ScanSkillBonusPerRankMeters = 10f;
        public const int MaxScanSkillRanks = 5;
        public const float FogOverlayAlpha = 0.95f;
        public const float RevealThreshold = 0.35f;

        /// <summary>Master toggle for map FOW overlay + reveal stamps. Off until re-enabled.</summary>
        public static bool SystemEnabled { get; set; }

        public static MapFogOfWar Instance { get; private set; }

        [SerializeField] private int fogResolution = 2048;
        [SerializeField] private float walkStampIntervalMeters = 0.75f;
        [SerializeField] private float textureUploadInterval = 0.12f;

        private WorldMapProvider mapProvider;
        private byte[] revealMask;
        private Texture2D fogTexture;
        private Color32[] fogPixels;
        private bool textureDirty;
        private float nextUploadTime;
        private Vector3 lastWalkStampPosition = new Vector3(float.MaxValue, 0f, float.MaxValue);
        private Transform playerTransform;
        private bool fullyInitialized;

        public Texture2D FogTexture => fogTexture;
        public bool IsReady => fullyInitialized && fogTexture != null;
        public event Action FogUpdated;

        public static float GetScanRevealRadius()
        {
            float bonus = PlayerSkillAllocator.GetScanRangeBonusMeters();
            bonus = Mathf.Clamp(bonus, 0f, ScanSkillBonusPerRankMeters * MaxScanSkillRanks);
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
            EnsureBuffers();
            fullyInitialized = true;
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
            EnsureBuffers();
            EnsurePlayer();
            if (SystemEnabled && playerTransform != null)
                RevealCircle(playerTransform.position, WalkRevealRadiusMeters, edgeSoftnessMeters: 1.5f);
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

        private void EnsureBuffers()
        {
            int res = ResolveFogResolution();
            fogResolution = res;
            int count = res * res;

            if (revealMask == null || revealMask.Length != count)
                revealMask = new byte[count];

            if (fogPixels == null || fogPixels.Length != count)
                fogPixels = new Color32[count];

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
            int target = fogResolution;
            if (mapProvider != null && mapProvider.MapTexture != null)
                target = Mathf.Max(target, mapProvider.MapTexture.width);

            float worldSpan = mapProvider != null
                ? Mathf.Max(mapProvider.WorldBounds.size.x, mapProvider.WorldBounds.size.z)
                : WorldMapProvider.MultiTerrainWorldSizeMeters;

            if (worldSpan > 7000f)
                target = Mathf.Max(target, 2048);

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

            Bounds bounds = mapProvider.WorldBounds;
            if (bounds.size.x < 1f || bounds.size.z < 1f)
                return;

            Vector2 uv = mapProvider.WorldToMap01(worldPosition);
            float worldSpanX = bounds.size.x;
            float worldSpanZ = bounds.size.z;
            float radiusUvX = radiusMeters / worldSpanX;
            float radiusUvZ = radiusMeters / worldSpanZ;
            float softUv = Mathf.Max(0.001f, edgeSoftnessMeters / Mathf.Max(worldSpanX, worldSpanZ));

            int res = fogResolution;
            int minX = Mathf.Clamp(Mathf.FloorToInt((uv.x - radiusUvX - softUv) * res), 0, res - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt((uv.x + radiusUvX + softUv) * res), 0, res - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt((uv.y - radiusUvZ - softUv) * res), 0, res - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt((uv.y + radiusUvZ + softUv) * res), 0, res - 1);

            bool changed = false;
            for (int y = minY; y <= maxY; y++)
            {
                float v = (y + 0.5f) / res;
                for (int x = minX; x <= maxX; x++)
                {
                    float u = (x + 0.5f) / res;
                    float dx = (u - uv.x) / Mathf.Max(0.0001f, radiusUvX);
                    float dy = (v - uv.y) / Mathf.Max(0.0001f, radiusUvZ);
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

            RevealCircle(worldPosition, GetScanRevealRadius(), edgeSoftnessMeters: 5f);
            UploadTexture();
        }

        public bool IsWorldRevealed(Vector3 worldPosition, float threshold = RevealThreshold)
        {
            if (!SystemEnabled)
                return true;

            if (!fullyInitialized || revealMask == null || mapProvider == null)
                return false;

            Vector2 uv = mapProvider.WorldToMap01(worldPosition);
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
            RevealCircle(worldPosition, WalkRevealRadiusMeters, edgeSoftnessMeters: 1.5f);
        }

        private void RebuildFogPixelsFromMask()
        {
            Color gold = DarkMatterGenesisUiPalette.Gold;
            byte gr = (byte)Mathf.RoundToInt(gold.r * 255f);
            byte gg = (byte)Mathf.RoundToInt(gold.g * 255f);
            byte gb = (byte)Mathf.RoundToInt(gold.b * 255f);

            for (int i = 0; i < revealMask.Length; i++)
            {
                float revealed = revealMask[i] / 255f;
                float fogAmount = 1f - revealed;
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(fogAmount * FogOverlayAlpha * 255f), 0, 255);
                fogPixels[i] = new Color32(gr, gg, gb, a);
            }
        }

        private void UploadTexture()
        {
            if (fogTexture == null || revealMask == null)
                return;

            RebuildFogPixelsFromMask();
            fogTexture.SetPixels32(fogPixels);
            fogTexture.Apply(false, false);
            textureDirty = false;
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
    }
}
