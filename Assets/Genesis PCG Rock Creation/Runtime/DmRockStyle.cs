using System;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    /// <summary>
    /// Formation style for DmRockCombiner's rule-based assembler (see DmRockAssembler). General rules (grain, large to
    /// small, overlap/sink, support test, size by height, debris apron, keep-outs) apply to every kind; the kind picks the
    /// arrangement and the kind-specific fields below.
    /// </summary>
    [CreateAssetMenu(fileName = "DM_RockStyle", menuName = "Genesis PCG Rock Creation/Rock Style")]
    public sealed class DmRockStyle : ScriptableObject
    {
        public enum StyleKind
        {
            BoulderPile = 0, OutcropShelf = 1, Spire = 2, Arch = 3, RubbleScatter = 4,
            VolcanicColumnar = 5, VolcanicEjecta = 6, VolcanicDome = 7,
            ErodedHoodoo = 8, ErodedMesa = 9, ErodedYardang = 10,
            SharpClean = 11, AlienShards = 12, AlienFloating = 13,
        }

        public static event Action<DmRockStyle> Changed;

        public StyleKind kind = StyleKind.BoulderPile;
        [TextArea(1, 3)] public string notes = "";

        [Header("Formation size")]
        [Tooltip("Target formation height range (meters).")]
        public Vector2 height = new Vector2(2.5f, 4f);
        [Tooltip("Footprint radius range (meters).")]
        public Vector2 footprintRadius = new Vector2(2.5f, 4f);
        [Tooltip("Main pieces (hero + mid + fill, apron excluded).")]
        public Vector2Int pieceCount = new Vector2Int(5, 12);

        [Header("Tiers (fraction of the hero size)")]
        [Range(1, 4)] public int heroCount = 1;
        public Vector2 midScale = new Vector2(0.5f, 0.7f);
        public Vector2 fillScale = new Vector2(0.2f, 0.4f);
        [Range(0f, 1f), Tooltip("Share of non-hero pieces that are mids (rest = fill).")]
        public float midShare = 0.35f;
        [Tooltip("Piece classes allowed for the hero / mids / fill / debris.")]
        public DmRockPieceClassMask heroClasses = DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Tall;
        public DmRockPieceClassMask midClasses = DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Slab;
        public DmRockPieceClassMask fillClasses = DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Small;
        public DmRockPieceClassMask debrisClasses = DmRockPieceClassMask.Small | DmRockPieceClassMask.Boulder;
        [Range(0f, 1f), Tooltip("Prefer pieces with crisp fractures (kit sharpness).")]
        public float preferSharp = 0f;

        [Header("Grain (one lean + azimuth per formation)")]
        public Vector2 leanDegrees = new Vector2(0f, 15f);
        [Range(0f, 15f), Tooltip("Per-piece jitter around the shared grain.")]
        public float grainJitter = 7f;
        [Range(0f, 1f), Tooltip("How strongly lying pieces put their flat side on the bedding plane.")]
        public float beddingAlign = 0.8f;

        [Header("Contact")]
        public Vector2 overlap = new Vector2(0.1f, 0.3f);
        [Tooltip("Sink into the ground as a fraction of the piece height.")]
        public Vector2 sink = new Vector2(0.15f, 0.3f);
        [Range(0, 4), Tooltip("Tiers above the ground (0 = nothing rests on other pieces).")]
        public int maxTiers = 2;
        [Range(0.2f, 1f), Tooltip("Upper-tier pieces are at most this fraction of the piece below.")]
        public float upperMaxRatio = 0.6f;
        [Range(0.2f, 1f), Tooltip("Size by height: max scale at the top of the formation.")]
        public float sizeAtTop = 0.45f;

        [Header("Support test")]
        [Range(0f, 0.5f)] public float comShrink = 0.15f;
        [Range(0.5f, 2f)] public float maxWidthOverSupport = 1.2f;
        [Range(10f, 60f)] public float maxContactSlope = 35f;
        [Tooltip("Hoodoo only: a wider cap may sit on a tall stem.")]
        public bool allowCaps;
        public Vector2 capWidthRatio = new Vector2(1f, 1.5f);

        [Header("Debris apron")]
        public Vector2Int apronCount = new Vector2Int(4, 10);
        public Vector2 apronScale = new Vector2(0.12f, 0.3f);
        [Range(20f, 45f)] public float reposeDegrees = 35f;
        [Tooltip("Apron reach as a multiple of the formation height (talus toe).")]
        public float apronReach = 1.3f;
        [Range(0f, 1f), Tooltip("Bias toward downhill / lean side.")]
        public float downhillBias = 0.6f;
        [Range(0f, 1f)] public float rockfallChance = 0.3f;

        [Header("Passages and nooks")]
        [Range(0f, 1f)] public float passageChance = 0.05f;
        [Range(0f, 1f)] public float nookChance = 0.1f;

        [Header("Volcanic columnar")]
        public Vector2Int columnCount = new Vector2Int(10, 24);
        [Tooltip("Top step between neighbouring columns (meters).")]
        public Vector2 columnStep = new Vector2(0.2f, 1.5f);
        [Range(0f, 30f)] public float columnFan = 8f;
        [Range(0f, 0.5f)] public float missingColumnChance = 0.12f;

        [Header("Arch")]
        public Vector2 archSpan = new Vector2(2.6f, 4f);
        public Vector2 archHeight = new Vector2(3f, 4.2f);
        [Range(0.25f, 0.6f)] public float lintelThicknessRatio = 0.3f;

        [Header("Ejecta")]
        [Range(0f, 360f)] public float ejectaFanDegrees = 360f;

        [Header("Yardang")]
        public Vector2Int ridgeCount = new Vector2Int(3, 7);
        [Range(1f, 1.5f)] public float maxStretch = 1.5f;

        [Header("Sharp / clean")]
        [Range(0f, 1f)] public float planeCutChance = 0.6f;
        [Range(0f, 45f)] public float planeCutMaxAngle = 25f;
        [Range(0f, 3f)] public float alignmentDegrees = 3f;

        [Header("Alien shards")]
        public Vector2Int shardSeeds = new Vector2Int(1, 3);
        public Vector2Int shardsPerSeed = new Vector2Int(3, 6);
        public Vector2 shardLean = new Vector2(10f, 35f);
        public Vector2 shardCurve = new Vector2(5f, 20f);
        [Range(0.02f, 1f)] public float shardTipScale = 0.12f;
        [Range(0.5f, 4f)] public float shardTaperPower = 1.6f;
        [Tooltip("Long-axis stretch of shards (pieces are thinned across).")]
        public Vector2 shardStretch = new Vector2(1.3f, 1.8f);

        [Header("Floating monolith")]
        public Vector2 hover = new Vector2(3f, 8f);
        public Vector2Int satellites = new Vector2Int(3, 8);

        [Header("Look")]
        [Tooltip("Optional material for every piece (null = keep the pack's originals). DM PCG Creator / preset override wins.")]
        public Material materialOverride;

        [NonSerialized] public int Version;

        public bool IsAlien => kind == StyleKind.AlienShards || kind == StyleKind.AlienFloating;

        private void OnValidate()
        {
            pieceCount.y = Mathf.Max(pieceCount.x, pieceCount.y);
            apronCount.y = Mathf.Max(apronCount.x, apronCount.y);
            Version++;
            Changed?.Invoke(this);
        }
    }

    /// <summary>Passage / nook settings (on the combiner, copied from a preset).</summary>
    [Serializable]
    public sealed class DmRockFeatureSettings
    {
        [Tooltip("Use the style's passage / nook chances. Off = the chances below.")]
        public bool useStyleChances = true;
        [Range(0f, 1f)] public float passageChance = 0f;
        [Range(0f, 1f)] public float nookChance = 0f;
        [Tooltip("When nooks are enabled (chance > 0) and the formation is big enough, guarantee one crate nook: if no nook survived the build, one is carved against the side of one of the biggest grounded pieces (never in the core).")]
        public bool guaranteeNook = true;
        [Min(1.6f), Tooltip("Corridor clear width (meters).")]
        public float passageWidth = 1.6f;
        [Min(2.5f), Tooltip("Corridor clear headroom (meters).")]
        public float passageHeadroom = 2.5f;
        [Min(0.3f), Tooltip("Crate size the nook is made for (meters); the pocket is ~1.4x this.")]
        public float crateSize = 0.9f;
        [Tooltip("Optional prefabs spawned in the nook socket (one picked by seed).")]
        public GameObject[] nookPrefabs = new GameObject[0];
        [Range(0f, 1f)] public float nookSpawnChance = 0.5f;

        public static DmRockFeatureSettings Default => new DmRockFeatureSettings();

        public DmRockFeatureSettings Clone()
        {
            var c = (DmRockFeatureSettings)MemberwiseClone();
            c.nookPrefabs = nookPrefabs != null ? (GameObject[])nookPrefabs.Clone() : new GameObject[0];
            return c;
        }
    }
}
