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

        [Header("Materials")]
        public Color ghostColor = new Color(0.92f, 0.38f, 0.32f, 1f);

        [Range(0f, 1f)]
        public float ghostAlpha = 0.45f;

        public Color finishedColor = new Color(0.62f, 0.58f, 0.52f, 1f);

        public Color glassColor = new Color(0.85f, 0.92f, 0.95f, 1f);

        [Range(0f, 1f)]
        public float glassAlpha = 0.35f;

        [Header("Snap")]
        [Tooltip("Edge-to-edge spacing for 4 m footprints (foundations, walls, floors).")]
        public float largeModuleMeters = 4f;

        [Tooltip("Edge-to-edge spacing for smaller footprints.")]
        public float smallModuleMeters = 2f;

        public float yawStepDegrees = 90f;
        public float heightStepMeters = 0.25f;
        public float maxHeightOffsetMeters = 4f;
        public float edgeSnapRangeMeters = 4f;
        public float topSnapRangeMeters = 7f;

        [Tooltip("Extra degrees the camera may look up while build mode is on. Applied to the look-up limit only, then restored.")]
        public float buildLookUpDegrees = 45f;
        public float doorFrameRangeMeters = 5f;

        [Range(0f, 1f)]
        public float edgeFacingDot = 0.5f;

        [Header("Placement")]
        public float buildSeconds = 2f;
        public float destroyHoldSeconds = 2f;
        public float aimDistanceMeters = 80f;
        public float doorSeatDropMeters = 0.4f;
        public float overlapPaddingMeters = 0.08f;

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
            Color color = profile != null ? profile.ghostColor : new Color(0.92f, 0.38f, 0.32f, 1f);
            color.a = Mathf.Clamp01(profile != null ? profile.ghostAlpha : 0.45f);
            return color;
        }

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
        public static float DoorFrameRangeMeters => Positive(Live != null ? Live.doorFrameRangeMeters : 5f, 5f);
        public static float EdgeFacingDot => Mathf.Clamp(Live != null ? Live.edgeFacingDot : 0.5f, -1f, 1f);
        public static float BuildSeconds => Positive(Live != null ? Live.buildSeconds : 2f, 2f);
        public static float DestroyHoldSeconds => Positive(Live != null ? Live.destroyHoldSeconds : 2f, 2f);
        public static float AimDistanceMeters => Positive(Live != null ? Live.aimDistanceMeters : 80f, 80f);
        public static float DoorSeatDropMeters => Mathf.Max(0f, Live != null ? Live.doorSeatDropMeters : 0.4f);
        public static float OverlapPaddingMeters => Mathf.Max(0f, Live != null ? Live.overlapPaddingMeters : 0.08f);

        static float Positive(float value, float fallback)
        {
            return value > 0.001f ? value : fallback;
        }
    }
}
