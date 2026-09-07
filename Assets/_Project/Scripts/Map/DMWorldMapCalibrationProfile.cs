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

        [Header("Display")]
        [Tooltip("Flip map V in world→UV math when authored north is texture bottom.")]
        public bool invertMapVertical;

        [Tooltip("Extra yaw on minimap/compass (-90 = world +X reads as North).")]
        [Range(-180f, 180f)]
        public float mapDisplayNorthOffsetDegrees = -90f;

        [Tooltip("Fine-tune world→UV alignment after affine calibration.")]
        public Vector2 mapUvOffset;

        [Tooltip("Static full-map player arrow base rotation (degrees).")]
        public float mapPlayerIconBaseDegrees;

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

        private static float NormalizeDegrees(float degrees)
        {
            degrees %= 360f;
            return degrees < 0f ? degrees + 360f : degrees;
        }
    }
}
