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

        // 0926-force-fields: every door piece is a force field. These drive its look, sound and behaviour.
        [Header("Force fields: look")]
        [Tooltip("Material on every force field door. Empty uses the default DM_ForceField material. The colour and sliders below tint whichever material is here.")]
        public Material forceFieldMaterial;
        [Tooltip("Main glow colour of the field (HDR, so values above 1 glow brighter).")]
        [ColorUsage(true, true)] public Color forceFieldColor = new Color(0.25f, 0.75f, 1.6f, 1f);
        [Tooltip("Colour of the rim, the ripples and the glancing-angle glow.")]
        [ColorUsage(true, true)] public Color forceFieldEdgeColor = new Color(0.6f, 1.2f, 2.4f, 1f);
        [Tooltip("How see-through the field is when nobody is passing. 0 = almost invisible.")]
        [Range(0f, 1f)] public float forceFieldOpacity = 0.25f;
        [Tooltip("Brightness of the rim where the field touches the frame.")]
        [Range(0f, 4f)] public float forceFieldEdgeGlow = 1.5f;
        [Tooltip("Width of that rim, as a share of the field (0.06 = 6%).")]
        [Range(0.005f, 0.5f)] public float forceFieldEdgeWidth = 0.06f;
        [Tooltip("Size of the moving energy pattern. Higher = finer pattern.")]
        [Range(0.1f, 5f)] public float forceFieldPatternScale = 1.2f;
        [Tooltip("How fast the energy pattern drifts.")]
        [Range(0f, 3f)] public float forceFieldScrollSpeed = 0.35f;

        [Header("Force fields: pass-through shimmer")]
        [Tooltip("How much brighter the whole field flashes when someone crosses it.")]
        [Range(0f, 4f)] public float forceFieldPulseBrightness = 1.5f;
        [Tooltip("How long the flash and the ripple last (s).")]
        [Range(0.05f, 3f)] public float forceFieldPulseSeconds = 0.6f;
        [Tooltip("How fast the ripple ring spreads from the crossing point (m/s).")]
        [Range(0.5f, 10f)] public float forceFieldRippleSpeed = 3f;
        [Tooltip("Thickness of the ripple ring (m).")]
        [Range(0.05f, 2f)] public float forceFieldRippleWidth = 0.35f;

        [Header("Force fields: audio")]
        [Tooltip("Sound played when someone crosses the field. Empty uses the electric crackle generated in code.")]
        public AudioClip forceFieldPassClip;
        [Range(0f, 1f)] public float forceFieldPassVolume = 0.35f;
        [Tooltip("Random pitch change per crossing (0.08 = up to 8% higher or lower).")]
        [Range(0f, 0.5f)] public float forceFieldPitchJitter = 0.08f;
        [Tooltip("Minimum time between crossing sounds for the same person (s).")]
        [Range(0f, 2f)] public float forceFieldSoundCooldown = 0.4f;
        [Tooltip("Distance where the field sounds fade to silence (m).")]
        [Range(2f, 40f)] public float forceFieldAudioMaxDistance = 14f;
        [Tooltip("Optional looping idle sound. Empty uses a hum generated in code. Only plays when the volume below is above 0.")]
        public AudioClip forceFieldIdleHumClip;
        [Range(0f, 1f)] public float forceFieldIdleHumVolume = 0f;

        [Header("Force fields: behaviour")]
        [Tooltip("Let the player and companions walk through. Off keeps every field solid. Bullets, enemies and weather are always stopped.")]
        public bool forceFieldLetFriendliesThrough = true;
        [Tooltip("How far in front of or behind the field someone must be for it to open (m).")]
        [Range(0.2f, 3f)] public float forceFieldSenseDepthMeters = 0.9f;
        [Tooltip("How long the field stays open after the last person leaves it (s).")]
        [Range(0f, 2f)] public float forceFieldCloseDelaySeconds = 0.35f;

        // 0927-ff-corners: a small block in each corner of every field, with a glowing strip through its middle.
        [Header("Force fields: corner blocks")]
        [Tooltip("Put a small block in each corner of every force field.")]
        public bool forceFieldCorners = true;
        [Tooltip("Material on the corner blocks. Empty uses DM_ForceFieldCorner in Resources/Building (Building Studio can create it), or a plain dark metal.")]
        public Material forceFieldCornerMaterial;
        [Tooltip("Width and height of each corner block (m).")]
        [Range(0.05f, 0.6f)] public float forceFieldCornerSize = 0.18f;
        [Tooltip("Thickness of each corner block, measured through the field (m).")]
        [Range(0.05f, 0.8f)] public float forceFieldCornerDepth = 0.24f;
        [Tooltip("Material for the glowing strip sandwiched in the middle of each block. Empty uses a generated glow. Its emission colour is replaced by the two colours below.")]
        public Material forceFieldStripMaterial;
        [Tooltip("Strip colour while the base has no power. The field material and its collider are off in this state.")]
        [ColorUsage(false, true)] public Color forceFieldStripUnpoweredColor = new Color(1f, 0.05f, 0.03f, 1f);
        [Tooltip("Strip colour once a fuelled generator powers the base and the field is on.")]
        [ColorUsage(false, true)] public Color forceFieldStripPoweredColor = new Color(0.1f, 1f, 0.2f, 1f);
        [Tooltip("How bright the strip glows (multiplies both colours).")]
        [Range(0f, 30f)] public float forceFieldStripGlow = 4f;
        [Tooltip("Thickness of the glowing strip (m).")]
        [Range(0.005f, 0.2f)] public float forceFieldStripThickness = 0.035f;

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
