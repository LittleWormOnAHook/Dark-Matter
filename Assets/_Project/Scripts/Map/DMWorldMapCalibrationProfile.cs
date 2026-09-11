using UnityEngine;

namespace Project.Map
{
    /// <summary>
    /// Tunable world-map alignment. Tweak in Play; DMProfilePlayModeSaver keeps changes on exit.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Map/World Map Calibration Profile", fileName = "DMWorldMapCalibrationProfile")]
    public sealed class DMWorldMapCalibrationProfile : ScriptableObject
    {
        public const string ResourcesPath = "Map/DMWorldMapCalibrationProfile";

        [Header("Runtime tuning")]
        [Tooltip("Arrow keys nudge map UV offset while playing.")]
        public bool enableRuntimeTuning = true;

        [Header("Map fog of war")]
        [Tooltip("Gold fog overlay on the world map / minimap. Off skips land-mask bake and walk stamps (much cheaper while moving).")]
        public bool enableMapFogOfWar = true;

        [Tooltip("Walk reveal hole radius (meters).")]
        [Min(0.5f)]
        public float walkRevealRadiusMeters = 5f;

        [Tooltip("Soft edge of the walk reveal hole (meters).")]
        [Min(0f)]
        public float walkRevealEdgeSoftnessMeters = 1.5f;

        [Tooltip("Meters walked between walk stamps. Higher = cheaper.")]
        [Min(0.1f)]
        public float walkStampIntervalMeters = 0.75f;

        [Tooltip("Seconds between fog texture uploads. Higher = cheaper.")]
        [Min(0.02f)]
        public float textureUploadInterval = 0.18f;

        [Tooltip("Fog texture size. 1024 matches minimap sharpness; 2048+ is costly while moving. Changing this in Play rebuilds the mask.")]
        [Range(64, 4096)]
        public int fogResolution = 1024;

        [Tooltip("Scanner sweep base radius (meters). Skill ranks add on top.")]
        [Min(1f)]
        public float scanRevealRadiusMeters = 40f;

        [Tooltip("Extra scan radius per Scan skill rank.")]
        [Min(0f)]
        public float scanSkillBonusPerRankMeters = 10f;

        [Tooltip("Max Scan skill ranks that add radius.")]
        [Range(0, 10)]
        public int maxScanSkillRanks = 5;

        [Tooltip("Soft edge of scanner reveal (meters).")]
        [Min(0f)]
        public float scanRevealEdgeSoftnessMeters = 5f;

        [Tooltip("How solid unrevealed gold fog is (0–1).")]
        [Range(0f, 1f)]
        public float fogOverlayAlpha = 0.95f;

        [Tooltip("Reveal mask value that counts as explored.")]
        [Range(0f, 1f)]
        public float revealThreshold = 0.35f;

        [Tooltip("Land-mask green cutoff. Darker than this = land (fog). Peach void stays clear.")]
        [Range(0f, 1f)]
        public float landMaskGreenThreshold = 0.82f;

        [Tooltip("Unrevealed fog tint. Defaults to palette Gold #D4A017.")]
        public Color fogColor = new Color(0.8313726f, 0.627451f, 0.09019608f, 1f);

        [Tooltip("Optional land/void mask. Empty uses DM Terrain Mask.jpg.")]
        public Texture2D terrainMask;

        [Header("Player icon")]
        [Tooltip("Extra rotation on the full-map / journal player arrow (degrees).")]
        public float mapPlayerIconBaseDegrees;

        [Tooltip("Extra rotation on the minimap / HUD / pilot-cluster player arrow (degrees). 180 = chevron tip on heading.")]
        public float minimapPlayerIconBaseDegrees = 180f;

        [Tooltip("Move the player arrow on the full / journal map without shifting terrain UVs.")]
        public Vector2 mapPlayerIconUvOffset;

        [Tooltip("Full-map / journal player arrow size (pixels).")]
        [Min(8f)]
        public float mapPlayerIconSizePixels = 18f;

        [Tooltip("Minimap / HUD player arrow size (pixels).")]
        [Min(8f)]
        public float minimapPlayerIconSizePixels = 24f;

        [Header("Display")]
        [Tooltip("Journal leftover. Affine travel ignores this. Use Minimap Flip Vertical for the HUD.")]
        public bool invertMapVertical;

        [Tooltip("Journal leftover. Affine travel ignores this. Use Minimap Flip Horizontal for the HUD.")]
        public bool invertMapHorizontal;

        [Tooltip("Extra yaw on minimap/compass (-90 = world +X reads as North).")]
        [Range(-180f, 180f)]
        public float mapDisplayNorthOffsetDegrees = -90f;

        [Tooltip("Fine-tune world→UV alignment after affine calibration.")]
        public Vector2 mapUvOffset;

        [Tooltip("Stretch map-east travel UV. >1 if the token stops short east/west.")]
        [Range(0.5f, 2f)]
        public float mapUvScaleX = 1.12f;

        [Tooltip("Stretch map-north travel UV. >1 if the token stops short north/south.")]
        [Range(0.5f, 2f)]
        public float mapUvScaleY = 1.14f;

        [Header("Full map / journal")]
        [Tooltip("Closest zoom: world meters visible across the square map viewport when scrolled all the way in.")]
        [Range(50f, 2000f)]
        public float fullMapMaxZoomVisibleMeters = 1000f;

        [Tooltip("World meters visible across the map viewport when the full map first opens.")]
        [Range(200f, 8000f)]
        public float fullMapOpenVisibleMeters = 1200f;

        [Tooltip("POI marker size on screen (pixels) — stays constant while zooming; shrinks on the map texture as you zoom in.")]
        [Range(4f, 20f)]
        public float fullMapMarkerScreenPixels = 7f;

        [Tooltip("Player arrow size on screen (pixels) on the full / journal map.")]
        [Range(4f, 24f)]
        public float fullMapPlayerScreenPixels = 10f;

        [Header("Minimap")]
        [Tooltip("Flip the minimap crop left/right. Does not change the journal.")]
        public bool minimapFlipHorizontal;

        [Tooltip("Flip the minimap crop up/down. Does not change the journal.")]
        public bool minimapFlipVertical;

        [Tooltip("Extra minimap pitch (X). Applied on top of heading-up yaw.")]
        [Range(-180f, 180f)]
        public float minimapRotateX;

        [Tooltip("Extra minimap roll (Y). Applied on top of heading-up yaw.")]
        [Range(-180f, 180f)]
        public float minimapRotateY;

        [Tooltip("Extra minimap yaw (Z) added to heading-up rotation.")]
        [Range(-180f, 180f)]
        public float minimapRotateZ;

        [Header("Grid 0,0 calibration")]
        [Tooltip("Texture UV where world grid origin (Map Zero) sits on the authored map.")]
        public Vector2 mapZeroUv01 = new Vector2(0.518f, 0.498f);

        [Tooltip("Known world XZ (grid coords) for the second calibration point.")]
        public Vector2 mapCalibrationWorldXz = new Vector2(-767f, -108f);

        [Tooltip("Texture UV for that second calibration point on the authored map.")]
        public Vector2 mapCalibrationUv01 = new Vector2(0.563f, 0.440f);

        public bool useAffineMapProjection = true;

        public void NudgeUvOffset(Vector2 delta)
        {
            mapUvOffset += delta;
        }

        public void NudgeMapZeroUv(Vector2 delta)
        {
            mapZeroUv01 = new Vector2(
                Mathf.Clamp01(mapZeroUv01.x + delta.x),
                Mathf.Clamp01(mapZeroUv01.y + delta.y));
            // Keep meters→UV scale: shift both calibration UVs together.
            mapCalibrationUv01 = new Vector2(
                Mathf.Clamp01(mapCalibrationUv01.x + delta.x),
                Mathf.Clamp01(mapCalibrationUv01.y + delta.y));
        }

        public void NudgeNorthOffset(float deltaDegrees)
        {
            mapDisplayNorthOffsetDegrees = NormalizeDegrees(mapDisplayNorthOffsetDegrees + deltaDegrees);
        }

        public void NudgePlayerIconUv(Vector2 delta)
        {
            mapPlayerIconUvOffset += delta;
        }

        public void NudgeUvScale(float deltaX, float deltaY)
        {
            mapUvScaleX = Mathf.Clamp(mapUvScaleX + deltaX, 0.5f, 2f);
            mapUvScaleY = Mathf.Clamp(mapUvScaleY + deltaY, 0.5f, 2f);
        }

        /// <summary>Max zoom multiplier so <paramref name="playableWorldSpanMeters"/> / zoom ≈ <see cref="fullMapMaxZoomVisibleMeters"/>.</summary>
        public float GetFullMapMaxZoomMultiplier(float playableWorldSpanMeters)
        {
            playableWorldSpanMeters = Mathf.Max(1f, playableWorldSpanMeters);
            float targetMeters = Mathf.Max(25f, fullMapMaxZoomVisibleMeters);
            return Mathf.Max(1f, playableWorldSpanMeters / targetMeters);
        }

        /// <summary>Opening zoom multiplier from <see cref="fullMapOpenVisibleMeters"/>.</summary>
        public float GetFullMapOpenZoomMultiplier(float playableWorldSpanMeters)
        {
            playableWorldSpanMeters = Mathf.Max(1f, playableWorldSpanMeters);
            float openMeters = Mathf.Clamp(fullMapOpenVisibleMeters, fullMapMaxZoomVisibleMeters, playableWorldSpanMeters);
            return Mathf.Max(1f, playableWorldSpanMeters / openMeters);
        }

        public float GetFullMapVisibleMeters(float playableWorldSpanMeters, float zoomMultiplier)
        {
            zoomMultiplier = Mathf.Max(1f, zoomMultiplier);
            return playableWorldSpanMeters / zoomMultiplier;
        }

        public static DMWorldMapCalibrationProfile LoadDefault()
        {
            if (WorldMapProvider.Instance != null && WorldMapProvider.Instance.CalibrationProfile != null)
                return WorldMapProvider.Instance.CalibrationProfile;

            return Resources.Load<DMWorldMapCalibrationProfile>(ResourcesPath);
        }

        public void ClampFogSettings()
        {
            walkRevealRadiusMeters = Mathf.Max(0.5f, walkRevealRadiusMeters);
            walkRevealEdgeSoftnessMeters = Mathf.Max(0f, walkRevealEdgeSoftnessMeters);
            walkStampIntervalMeters = Mathf.Max(0.1f, walkStampIntervalMeters);
            textureUploadInterval = Mathf.Max(0.02f, textureUploadInterval);
            fogResolution = Mathf.Clamp(fogResolution, 64, 4096);
            scanRevealRadiusMeters = Mathf.Max(1f, scanRevealRadiusMeters);
            scanSkillBonusPerRankMeters = Mathf.Max(0f, scanSkillBonusPerRankMeters);
            maxScanSkillRanks = Mathf.Clamp(maxScanSkillRanks, 0, 10);
            scanRevealEdgeSoftnessMeters = Mathf.Max(0f, scanRevealEdgeSoftnessMeters);
            fogOverlayAlpha = Mathf.Clamp01(fogOverlayAlpha);
            revealThreshold = Mathf.Clamp01(revealThreshold);
            landMaskGreenThreshold = Mathf.Clamp01(landMaskGreenThreshold);
            mapPlayerIconSizePixels = Mathf.Max(8f, mapPlayerIconSizePixels);
            minimapPlayerIconSizePixels = Mathf.Max(8f, minimapPlayerIconSizePixels);
            mapUvScaleX = Mathf.Clamp(mapUvScaleX, 0.5f, 2f);
            mapUvScaleY = Mathf.Clamp(mapUvScaleY, 0.5f, 2f);
            minimapRotateX = Mathf.Clamp(minimapRotateX, -180f, 180f);
            minimapRotateY = Mathf.Clamp(minimapRotateY, -180f, 180f);
            minimapRotateZ = Mathf.Clamp(minimapRotateZ, -180f, 180f);
            fullMapMaxZoomVisibleMeters = Mathf.Clamp(fullMapMaxZoomVisibleMeters, 50f, 2000f);
            fullMapOpenVisibleMeters = Mathf.Clamp(
                fullMapOpenVisibleMeters,
                fullMapMaxZoomVisibleMeters,
                8000f);
            fullMapMarkerScreenPixels = Mathf.Clamp(fullMapMarkerScreenPixels, 4f, 20f);
            fullMapPlayerScreenPixels = Mathf.Clamp(fullMapPlayerScreenPixels, 4f, 24f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampFogSettings();
            if (!Application.isPlaying)
                return;

            MapFogOfWar.ApplyFromProfile(this);
            if (WorldMapProvider.Instance != null)
            {
                WorldMapProvider.Instance.ApplyCalibrationToScene(this);
                WorldMapProvider.Instance.NotifyCalibrationChanged();
            }
        }
#endif

        private static float NormalizeDegrees(float degrees)
        {
            degrees %= 360f;
            return degrees < 0f ? degrees + 360f : degrees;
        }
    }
}
