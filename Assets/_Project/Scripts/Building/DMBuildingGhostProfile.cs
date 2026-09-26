using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Live building placement profile. Building Studio and Genesis Studio edit this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Building/Ghost Profile")]
    public sealed class DMBuildingGhostProfile : ScriptableObject
    {
        public const string ResourcePath = "Building/DM_BuildingGhostProfile";
        public const string AssetPath = "Assets/_Project/Resources/Building/DM_BuildingGhostProfile.asset";

        [Header("Preview — Snap & build (valid seat)")]
        [Tooltip("Optional HDRP material for the green build-ready hologram. Color and alpha still tint each frame.")]
        public Material validGhostMaterial;

        public Color ghostColor = new Color(0.18f, 0.78f, 0.36f, 1f);

        [Range(0f, 1f)]
        public float ghostAlpha = 0.45f;

        [Header("Preview — Blocked seat")]
        [Tooltip("Optional material when the seat is invalid or unaffordable.")]
        public Material blockedGhostMaterial;

        public Color blockedGhostColor = new Color(0.561f, 0.118f, 0.369f, 1f);

        [Range(0f, 1f)]
        public float blockedGhostAlpha = 0.45f;

        [Header("Built piece tints")]
        public Color finishedColor = new Color(0.62f, 0.58f, 0.52f, 1f);

        public Color glassColor = new Color(0.85f, 0.92f, 0.95f, 1f);

        [Range(0f, 1f)]
        public float glassAlpha = 0.35f;

        [Tooltip("Material for built pieces when their style has no finish. Tinted by Finished mesh. Empty = plain lit surface.")]
        public Material builtMaterial;

        [Header("Snap")]
        [Tooltip("Edge-to-edge spacing for 4 m footprints (foundations, walls, floors).")]
        public float largeModuleMeters = 4f;

        [Tooltip("Edge-to-edge spacing for smaller footprints.")]
        public float smallModuleMeters = 2f;

        [Tooltip("Alt + scroll step for the first foundation. Pieces on the building grid turn in 90 degree steps.")]
        public float yawStepDegrees = 45f;
        public float heightStepMeters = 0.25f;
        public float maxHeightOffsetMeters = 4f;
        public float edgeSnapRangeMeters = 4f;
        public float topSnapRangeMeters = 7f;

        [Tooltip("Extra degrees the camera may look up while build mode is on. Applied to the look-up limit only, then restored.")]
        public float buildLookUpDegrees = 45f;

        [Tooltip("Pull camera back in build mode: saved distance × this, plus extra meters. Keep low indoors to avoid wall clip fight.")]
        public float buildModeCameraDistanceMultiplier = 1.35f;

        public float buildModeCameraExtraMeters = 1.25f;
        public float doorFrameRangeMeters = 5f;

        [Range(0f, 1f)]
        public float edgeFacingDot = 0.5f;

        [Header("Placement")]
        public float buildSeconds = 2f;
        public float destroyHoldSeconds = 2f;
        public float aimDistanceMeters = 80f;
        public float doorSeatDropMeters = 0.4f;
        public float overlapPaddingMeters = 0.08f;

        [Header("Build crosshair")]
        [Tooltip("Centre dot shown while build mode is on, in pixels. 0 hides it.")]
        public float crosshairDotPixels = 6f;
        public Color crosshairDotColor = new Color(1f, 1f, 1f, 0.9f);
        public Color crosshairDotOutline = new Color(0f, 0f, 0f, 0.6f);

        [Header("Layers")]
        [Tooltip("Layer every placed piece lives on. Rebuild Stone Kit creates it when missing.")]
        public string buildingLayerName = "Building";

        [Tooltip("Crosshair ray that finds built pieces for snapping and destroy. The Building layer is always included. Nothing = Building + Climbable.")]
        public LayerMask builtAimLayers;

        [Tooltip("Ground ray for the first foundation. The Building layer is never included. Nothing = Default + Terrain.")]
        public LayerMask groundLayers;

        [Tooltip("What blocks a seat besides other pieces. The Building layer is always included. Nothing = Default, Climbable, Resource, Pushable and the PW object layers.")]
        public LayerMask blockerLayers;

        [Header("Door swing (E)")]
        public float doorSwingDegrees = 90f;
        public float doorSwingSeconds = 0.35f;
        public float doorInteractRangeMeters = 2.4f;

        static DMBuildingGhostProfile live;

        public static DMBuildingGhostProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DMBuildingGhostProfile>(ResourcePath);
                return live;
            }
        }

        public static Color ResolveGhostColor()
        {
            DMBuildingGhostProfile profile = Live;
            Color color = profile != null ? profile.ghostColor : new Color(0.18f, 0.78f, 0.36f, 1f);
            color.a = Mathf.Clamp01(profile != null ? profile.ghostAlpha : 0.45f);
            return color;
        }

        public static Color ResolveBlockedGhostColor()
        {
            DMBuildingGhostProfile profile = Live;
            Color color = profile != null ? profile.blockedGhostColor : new Color(0.561f, 0.118f, 0.369f, 1f);
            color.a = Mathf.Clamp01(profile != null ? profile.blockedGhostAlpha : 0.45f);
            return color;
        }

        public static Material ValidGhostMaterialTemplate => Live != null ? Live.validGhostMaterial : null;
        public static Material BlockedGhostMaterialTemplate => Live != null ? Live.blockedGhostMaterial : null;
        public static Material BuiltMaterialTemplate => Live != null ? Live.builtMaterial : null;

        public static Color ResolveFinishedColor()
        {
            DMBuildingGhostProfile profile = Live;
            Color color = profile != null ? profile.finishedColor : new Color(0.62f, 0.58f, 0.52f, 1f);
            color.a = 1f;
            return color;
        }

        public static Color ResolveGlassColor()
        {
            DMBuildingGhostProfile profile = Live;
            Color color = profile != null ? profile.glassColor : new Color(0.85f, 0.92f, 0.95f, 1f);
            color.a = Mathf.Clamp01(profile != null ? profile.glassAlpha : 0.35f);
            return color;
        }

        public static float LargeModuleMeters => Positive(Live != null ? Live.largeModuleMeters : 4f, 4f);
        public static float SmallModuleMeters => Positive(Live != null ? Live.smallModuleMeters : 2f, 2f);
        public static float YawStepDegrees => Positive(Live != null ? Live.yawStepDegrees : 90f, 90f);
        public static float HeightStepMeters => Positive(Live != null ? Live.heightStepMeters : 0.25f, 0.25f);
        public static float MaxHeightOffsetMeters => Positive(Live != null ? Live.maxHeightOffsetMeters : 4f, 4f);
        public static float EdgeSnapRangeMeters => Positive(Live != null ? Live.edgeSnapRangeMeters : 4f, 4f);
        public static float TopSnapRangeMeters => Positive(Live != null ? Live.topSnapRangeMeters : 7f, 7f);
        public static float BuildLookUpDegrees => Mathf.Clamp(Live != null ? Live.buildLookUpDegrees : 45f, 0f, 75f);
        public static float BuildModeCameraDistanceMultiplier =>
            Positive(Live != null ? Live.buildModeCameraDistanceMultiplier : 1.35f, 1.35f);
        public static float BuildModeCameraExtraMeters =>
            Mathf.Max(0f, Live != null ? Live.buildModeCameraExtraMeters : 1.25f);
        public static float DoorFrameRangeMeters => Positive(Live != null ? Live.doorFrameRangeMeters : 5f, 5f);
        public static float EdgeFacingDot => Mathf.Clamp(Live != null ? Live.edgeFacingDot : 0.5f, -1f, 1f);
        public static float BuildSeconds => Positive(Live != null ? Live.buildSeconds : 2f, 2f);
        public static float CrosshairDotPixels => Mathf.Max(0f, Live != null ? Live.crosshairDotPixels : 6f);
        public static Color CrosshairDotColor => Live != null ? Live.crosshairDotColor : new Color(1f, 1f, 1f, 0.9f);
        public static Color CrosshairDotOutline => Live != null ? Live.crosshairDotOutline : new Color(0f, 0f, 0f, 0.6f);
        public static float DestroyHoldSeconds => Positive(Live != null ? Live.destroyHoldSeconds : 2f, 2f);
        public static float AimDistanceMeters => Positive(Live != null ? Live.aimDistanceMeters : 80f, 80f);
        public static float DoorSeatDropMeters => Mathf.Max(0f, Live != null ? Live.doorSeatDropMeters : 0.4f);
        public static float OverlapPaddingMeters => Mathf.Max(0f, Live != null ? Live.overlapPaddingMeters : 0.08f);
        public static float DoorSwingDegrees => Positive(Live != null ? Live.doorSwingDegrees : 90f, 90f);
        public static float DoorSwingSeconds => Positive(Live != null ? Live.doorSwingSeconds : 0.35f, 0.35f);
        public static float DoorInteractRangeMeters => Positive(Live != null ? Live.doorInteractRangeMeters : 2.4f, 2.4f);

        public static string BuildingLayerName => Live != null && !string.IsNullOrWhiteSpace(Live.buildingLayerName) ? Live.buildingLayerName.Trim() : "Building";
        public static int BuiltAimLayersRaw => Live != null ? Live.builtAimLayers.value : 0;
        public static int GroundLayersRaw => Live != null ? Live.groundLayers.value : 0;
        public static int BlockerLayersRaw => Live != null ? Live.blockerLayers.value : 0;

        static float Positive(float value, float fallback)
        {
            return value > 0.001f ? value : fallback;
        }
    }
}
