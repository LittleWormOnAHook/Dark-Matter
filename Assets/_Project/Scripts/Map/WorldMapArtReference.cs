using UnityEngine;

namespace Project.Map
{
    /// <summary>
    /// Scene reference for the authored world-map art plane (hierarchy "MAP art").
    /// Supplies the biome map texture used by minimap/full map UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldMapArtReference : MonoBehaviour
    {
        private const float UnityPlaneMeshSpanMeters = 10f;

        [SerializeField] private MeshRenderer mapRenderer;
        [SerializeField] private bool invertMapVertical = false;
        [Tooltip("Extra yaw added to minimap/compass heading ( -90 = world +X reads as North ).")]
        [SerializeField] private float mapDisplayNorthOffsetDegrees = -90f;
        [Tooltip("Fine-tune world→UV alignment after affine calibration.")]
        [SerializeField] private Vector2 mapUvOffset = Vector2.zero;
        [Tooltip("Static full-map player arrow base rotation (degrees).")]
        [SerializeField] private float mapPlayerIconBaseDegrees;
        [Header("Grid 0,0 calibration")]
        [Tooltip("Texture UV where world grid origin (Map Zero) sits on the authored map.")]
        [SerializeField] private Vector2 mapZeroUv01 = new Vector2(0.518f, 0.498f);
        [Tooltip("Known world XZ (grid coords) for the second calibration point.")]
        [SerializeField] private Vector2 mapCalibrationWorldXz = new Vector2(-767f, -108f);
        [Tooltip("Texture UV for that second calibration point on the authored map.")]
        [SerializeField] private Vector2 mapCalibrationUv01 = new Vector2(0.563f, 0.440f);
        [SerializeField] private bool useAffineMapProjection = true;

        public bool InvertMapVertical => invertMapVertical;
        public float MapDisplayNorthOffsetDegrees => mapDisplayNorthOffsetDegrees;
        public Vector2 MapUvOffset => mapUvOffset;
        public float MapPlayerIconBaseDegrees => mapPlayerIconBaseDegrees;
        public Vector2 MapZeroUv01 => mapZeroUv01;
        public Vector2 MapCalibrationWorldXz => mapCalibrationWorldXz;
        public Vector2 MapCalibrationUv01 => mapCalibrationUv01;
        public bool UseAffineMapProjection => useAffineMapProjection;

        public static WorldMapArtReference FindInScene()
        {
            WorldMapArtReference[] references = FindObjectsByType<WorldMapArtReference>(FindObjectsInactive.Include);
            return references.Length > 0 ? references[0] : null;
        }

        public bool TryGetMapTexture(out Texture2D texture)
        {
            texture = null;
            EnsureRenderer();

            if (mapRenderer == null)
                return false;

            Material material = mapRenderer.sharedMaterial;
            if (material == null)
                return false;

            texture = material.GetTexture("_BaseColorMap") as Texture2D;
            if (texture == null)
                texture = material.mainTexture as Texture2D;

            return texture != null;
        }

        /// <summary>World-space XZ bounds of the art plane mesh (for alignment checks).</summary>
        public Bounds GetArtWorldBounds()
        {
            EnsureRenderer();
            Vector3 scale = transform.lossyScale;
            Vector3 size = new Vector3(
                Mathf.Abs(scale.x) * UnityPlaneMeshSpanMeters,
                1f,
                Mathf.Abs(scale.z) * UnityPlaneMeshSpanMeters);
            return new Bounds(transform.position, size);
        }

        /// <summary>Axis-aligned world bounds (respects rotation/scale via renderer when present).</summary>
        public Bounds GetWorldBounds()
        {
            EnsureRenderer();
            if (mapRenderer != null)
                return mapRenderer.bounds;

            return GetArtWorldBounds();
        }

        private void EnsureRenderer()
        {
            if (mapRenderer == null)
                mapRenderer = GetComponent<MeshRenderer>();
        }

        public void ApplyCalibrationFromProfile(DMWorldMapCalibrationProfile profile)
        {
            if (profile == null)
                return;

            invertMapVertical = profile.invertMapVertical;
            mapDisplayNorthOffsetDegrees = profile.mapDisplayNorthOffsetDegrees;
            mapUvOffset = profile.mapUvOffset;
            mapPlayerIconBaseDegrees = profile.mapPlayerIconBaseDegrees;
            mapZeroUv01 = profile.mapZeroUv01;
            mapCalibrationWorldXz = profile.mapCalibrationWorldXz;
            mapCalibrationUv01 = profile.mapCalibrationUv01;
            useAffineMapProjection = profile.useAffineMapProjection;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureRenderer();
        }
#endif
    }
}
