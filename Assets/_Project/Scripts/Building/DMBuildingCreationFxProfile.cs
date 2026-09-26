using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Building Creation Effects (0926): the pop, bounce, VFX and optional material flash a piece plays when a hold-to-build finishes.
    /// Edited in Building Studio > Creation Effects and Genesis Studio > Building > Creation Effects.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Building/Creation Effects Profile")]
    public sealed class DMBuildingCreationFxProfile : ScriptableObject
    {
        public const string ResourcePath = "Building/DM_BuildingCreationFxProfile";
        public const string AssetPath = "Assets/_Project/Resources/Building/DM_BuildingCreationFxProfile.asset";

        [Tooltip("Master switch for the whole sequence.")]
        public bool enabled = true;

        [Header("Pop & bounce")]
        [Tooltip("How long the grow/shrink and bounce take. 0 skips the motion.")]
        [Range(0f, 3f)] public float duration = 0.3f;
        [Tooltip("Extra size at the peak. 0.1 = grows 10% then shrinks back.")]
        [Range(0f, 0.5f)] public float scalePunch = 0.1f;
        [Tooltip("How high the piece lifts at the peak before settling back on its seat (m).")]
        [Range(0f, 1f)] public float bounceHeightMeters = 0.1f;
        [Tooltip("Size over the duration (0 = normal size, 1 = full punch).")]
        public AnimationCurve scaleCurve = Hump();
        [Tooltip("Lift over the duration (0 = on the seat, 1 = full bounce height).")]
        public AnimationCurve bounceCurve = Hump();

        [Header("VFX")]
        [Tooltip("Optional particle prefab spawned during the sequence. Not parented to the piece.")]
        public GameObject vfxPrefab;
        [Tooltip("Seconds after the build finishes before the VFX spawns.")]
        [Range(0f, 3f)] public float vfxDelay = 0f;
        [Tooltip("Seconds before the spawned VFX is removed.")]
        [Range(0.1f, 10f)] public float vfxLifetime = 2f;
        [Tooltip("Where the VFX spawns on the piece.")]
        public DMBuildingFxAnchor vfxAnchor = DMBuildingFxAnchor.Base;
        [Tooltip("Scale the VFX to the piece's footprint (4 m foundation = 1x).")]
        public bool vfxScaleWithPiece = true;

        [Header("Material swap")]
        [Tooltip("Swap every renderer on the piece to the material below for a while, then restore its finish.")]
        public bool swapMaterial;
        public Material swapMaterialAsset;
        [Tooltip("Seconds after the build finishes before the swap starts.")]
        [Range(0f, 3f)] public float swapStart = 0f;
        [Tooltip("How long the swap material stays on.")]
        [Range(0f, 3f)] public float swapDuration = 0.3f;

        static DMBuildingCreationFxProfile live;

        public static DMBuildingCreationFxProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DMBuildingCreationFxProfile>(ResourcePath);
                return live;
            }
        }

        public static AnimationCurve Hump()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, Mathf.PI, Mathf.PI),
                new Keyframe(0.5f, 1f, 0f, 0f),
                new Keyframe(1f, 0f, -Mathf.PI, -Mathf.PI));
        }

        void OnValidate()
        {
            if (scaleCurve == null || scaleCurve.length == 0)
                scaleCurve = Hump();
            if (bounceCurve == null || bounceCurve.length == 0)
                bounceCurve = Hump();
        }
    }

    public enum DMBuildingFxAnchor
    {
        Base,
        Center,
        Top,
    }
}
