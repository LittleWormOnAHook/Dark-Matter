using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GenesisPCG.RockCreation
{
    /// <summary>
    /// Rule-based rock formation assembler (combiner-local space, y up, ground given by a height function).
    /// General rules for every style: one shared grain (lean + azimuth), keep-outs reserved first (passages / nooks),
    /// large to small, overlap + sink, support test for resting pieces (COM inside the 15%-shrunk contact hull, width
    /// at most ~1.2x the support, no wide piece on a tall/thin one except hoodoo caps, contact slope), size by height,
    /// debris apron at the angle of repose. Each StyleKind arranges pieces on top of these rules.
    /// </summary>
    public sealed partial class DmRockAssembler
    {
        public enum Mode { Ground, Rest, Free }

        public sealed class KeepOut
        {
            public int kind;                 // 0 passage segment, 1 nook, 2 under-mass
            public Vector3 center;           // box centre
            public Quaternion rotation = Quaternion.identity; // local x = across, y = up, z = along
            public Vector3 size;
            public bool active = true;

            public bool Contains(Vector3 p, float margin)
            {
                Vector3 l = Quaternion.Inverse(rotation) * (p - center);
                Vector3 h = size * 0.5f;
                return Mathf.Abs(l.x) < h.x + margin && Mathf.Abs(l.y) < h.y + margin && Mathf.Abs(l.z) < h.z + margin;
            }

            public Vector3 Local(Vector3 p) => Quaternion.Inverse(rotation) * (p - center);
        }

        public sealed class PassageInfo
        {
            public Vector3 a, b;              // centre line endpoints at floor level
            public float width, headroom;
            public readonly List<KeepOut> segments = new List<KeepOut>();
            public bool kept = true;
            public string purpose = "passage";
        }

        public sealed class NookInfo
        {
            public Vector3 position;          // floor centre
            public Quaternion rotation = Quaternion.identity; // forward = opening direction
            public Vector3 size;
            public int walls;
            public bool roof;
            public KeepOut box;
        }

        public sealed class Stats
        {
            public int attempts, placed, restTested, restPassed, wedged, caps;
            public int rejCom, rejWidth, rejOnThin, rejSlope, rejKeepOut, rejOverlap, rejHidden, rejNoSupport, rejBounds, rejWedge;
            public float maxWidthRatio, maxCapRatio;
            public int bigOnThinViolations;
            public int passagesRequested, passagesKept, nooksRequested, nooksKept;
            public int cutsPlanned;

            public string Summary()
            {
                return $"placed {placed}/{attempts} attempts; rest tested {restTested} passed {restPassed} (wedged {wedged}, caps {caps}); " +
                       $"rejected: com {rejCom}, width {rejWidth}, onThin {rejOnThin}, slope {rejSlope}, keepOut {rejKeepOut}, overlap {rejOverlap}, hidden {rejHidden}, noSupport {rejNoSupport}, wedge {rejWedge}, bounds {rejBounds}; " +
                       $"max width/support {maxWidthRatio:0.00}, max cap/stem {maxCapRatio:0.00}; bigOnThin violations {bigOnThinViolations}; " +
                       $"passages {passagesKept}/{passagesRequested}, nooks {nooksKept}/{nooksRequested}, cuts {cutsPlanned}";
            }
        }

        public sealed class Result
        {
            public readonly List<PcgPlacedPart> parts = new List<PcgPlacedPart>();
            public readonly List<PassageInfo> passages = new List<PassageInfo>();
            public readonly List<NookInfo> nooks = new List<NookInfo>();
            public readonly List<string> log = new List<string>();
            public Stats stats = new Stats();
            public float height, radius, leanDeg, azimuthDeg;
            public bool noConform;
            public string kind;
        }

        // Inputs
        private readonly DmRockStyle m_style;
        private readonly DmRockFeatureSettings m_feat;
        private readonly List<DmRockPieceInfo> m_pieces;
        private readonly Func<float, float, float> m_groundFn;
        private readonly Random m_rng;
        private readonly Result m_res = new Result();
        private Stats S => m_res.stats;

        // Formation
        private float m_H, m_R, m_lean, m_az;
        private Vector3 m_up, m_dip, m_strike;

        // Grid
        private float m_cell, m_half;
        private int m_n;
        private float[] m_top, m_ground;
        private int[] m_owner;
        private float[] m_tBot, m_tTop;
        private int[] m_tStamp;
        private int m_stamp;
        private readonly List<int> m_touched = new List<int>();

        private readonly List<Placed> m_placed = new List<Placed>();
        private readonly List<KeepOut> m_keepOuts = new List<KeepOut>();
        private string m_lastReject;
        private KeepOut m_lastKeepOut;
        private DmRockPieceInfo m_lastPick;

        private sealed class Placed
        {
            public int id;
            public PcgPlacedPart part;
            public DmRockPieceInfo info;
            public Bounds bounds;
            public Vector3 com, topCenter;
            public float height, topWidth, maxExtent, bottomY;
            public bool tall, cap, stem;
            public int tier;
            public Mode mode;
            public string role;
            public float widthRatio;
            public readonly List<int> topCells = new List<int>();
            public readonly List<int> supporters = new List<int>();
            public readonly List<Vector3> undo = new List<Vector3>(); // (cell, previous top, previous owner)
        }

        private sealed class Req
        {
            public DmRockPieceInfo info;
            public Vector3 scale = Vector3.one;
            public Quaternion rot = Quaternion.identity;
            public PcgPieceDeform deform = PcgPieceDeform.None;
            public Vector2 xz;
            public Mode mode = Mode.Ground;
            public float sink;          // Ground: fraction of the vertical extent pushed below the contact; Rest: embed fraction
            public float fitHeight;     // > 0: uniform rescale so the visible height matches
            public float nominal;       // > 0: size-by-height reference (max extent at ground level)
            public float freeY;
            public bool allowCap, lintel, requireGround, ignoreKeepOuts, skipOverlap;
            public float maxOverlap = 0.45f, minVisible = 0.25f;
            public string role = "";
            public int tier;
            public bool tallFlag = true;

            public Req Clone() => (Req)MemberwiseClone();
        }

        public DmRockAssembler(DmRockStyle style, DmRockFeatureSettings features, List<DmRockPieceInfo> pieces, Func<float, float, float> groundFn, int seed)
        {
            m_style = style;
            m_feat = features ?? DmRockFeatureSettings.Default;
            m_pieces = pieces ?? new List<DmRockPieceInfo>();
            m_groundFn = groundFn ?? ((x, z) => 0f);
            m_rng = new Random(seed);
        }

        /// <summary>Builds the formation. <paramref name="fixedParts"/> (captured base models) are kept as-is; only the apron is added around them.</summary>
        public Result Assemble(IList<PcgPlacedPart> fixedParts = null)
        {
            DmRockStyle st = m_style;
            m_res.kind = st.kind.ToString();
            m_H = R(st.height);
            m_R = R(st.footprintRadius);
            m_lean = R(st.leanDegrees);
            m_az = R(0f, 360f);
            float a = m_az * Mathf.Deg2Rad;
            m_dip = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            m_strike = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
            m_up = (Vector3.up * Mathf.Cos(m_lean * Mathf.Deg2Rad) + m_dip * Mathf.Sin(m_lean * Mathf.Deg2Rad)).normalized;
            m_res.height = m_H; m_res.radius = m_R; m_res.leanDeg = m_lean; m_res.azimuthDeg = m_az;

            float extent = m_R + Mathf.Max(m_H * st.apronReach, 2f) + Mathf.Max(m_H * 0.6f, 3f) + 3f;
            if (st.kind == DmRockStyle.StyleKind.ErodedYardang) extent = Mathf.Max(extent, m_H * 12f + 4f);
            InitGrid(extent);

            if (fixedParts != null && fixedParts.Count > 0)
            {
                foreach (PcgPlacedPart fp in fixedParts) AddFixed(fp);
                Apron(R(st.apronCount));
                Finish();
                return m_res;
            }
            if (m_pieces.Count == 0)
            {
                m_res.log.Add("kit has no pieces");
                return m_res;
            }

            switch (st.kind)
            {
                case DmRockStyle.StyleKind.BoulderPile: GenericFeatures(m_dip); BoulderPile(); break;
                case DmRockStyle.StyleKind.OutcropShelf: GenericFeatures(m_strike); Outcrop(); break;
                case DmRockStyle.StyleKind.Spire: GenericFeatures(m_strike); Spire(); break;
                case DmRockStyle.StyleKind.Arch: Arch(); break;
                case DmRockStyle.StyleKind.RubbleScatter: Rubble(); break;
                case DmRockStyle.StyleKind.VolcanicColumnar: Columnar(); break;
                case DmRockStyle.StyleKind.VolcanicEjecta: Ejecta(); break;
                case DmRockStyle.StyleKind.VolcanicDome: GenericFeatures(m_strike); Dome(); break;
                case DmRockStyle.StyleKind.ErodedHoodoo: GenericFeatures(m_strike, R(2f, 4f)); Hoodoo(); break;
                case DmRockStyle.StyleKind.ErodedMesa: Mesa(); break;
                case DmRockStyle.StyleKind.ErodedYardang: Yardang(); break;
                case DmRockStyle.StyleKind.SharpClean: SharpClean(); break;
                case DmRockStyle.StyleKind.AlienShards: GenericFeatures(m_strike); Shards(); break;
                case DmRockStyle.StyleKind.AlienFloating: Floating(); break;
            }

            ResolveNooks();
            if (st.kind != DmRockStyle.StyleKind.AlienFloating && st.kind != DmRockStyle.StyleKind.RubbleScatter && st.kind != DmRockStyle.StyleKind.VolcanicEjecta)
                Apron(R(st.apronCount));
            Finish();
            return m_res;
        }

        // Random helpers
        private float R() => (float)m_rng.NextDouble();
        private float R(float a, float b) => a + (b - a) * (float)m_rng.NextDouble();
        private float R(Vector2 v) => R(v.x, v.y);
        private int R(Vector2Int v) => m_rng.Next(Mathf.Min(v.x, v.y), Mathf.Max(v.x, v.y) + 1);
        private bool Chance(float p) => m_rng.NextDouble() < p;
        private Vector2 InCircle() { float t = R(0f, Mathf.PI * 2f), r = Mathf.Sqrt(R()); return new Vector2(Mathf.Cos(t) * r, Mathf.Sin(t) * r); }
        private static Vector2 XZ(Vector3 v) => new Vector2(v.x, v.z);
        private static Vector3 X0Z(Vector2 v) => new Vector3(v.x, 0f, v.y);
        private static Vector3 Dir(float deg) { float a = deg * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)); }
        private static float Az(Vector3 d) => Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;

        private Vector3 Jitter(Vector3 v, float deg)
        {
            if (deg <= 0f) return v.normalized;
            Vector3 perp = Vector3.Cross(v, Mathf.Abs(v.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            perp = Quaternion.AngleAxis(R(0f, 360f), v) * perp;
            return (Quaternion.AngleAxis(deg * Mathf.Sqrt(R()), perp) * v).normalized;
        }

        private float Gauss() { double u1 = 1.0 - m_rng.NextDouble(), u2 = m_rng.NextDouble(); return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2)); }

        // Orientation helpers (piece frame: y = long, z = short, x = mid)
        private static Quaternion FrameLong(Vector3 longDir, Vector3 shortHint)
        {
            longDir.Normalize();
            Vector3 sh = Vector3.ProjectOnPlane(shortHint, longDir);
            if (sh.sqrMagnitude < 1e-4f) sh = Vector3.ProjectOnPlane(Vector3.forward, longDir);
            if (sh.sqrMagnitude < 1e-4f) sh = Vector3.ProjectOnPlane(Vector3.right, longDir);
            return Quaternion.LookRotation(sh.normalized, longDir);
        }

        private static Quaternion FrameShort(Vector3 shortDir, Vector3 longHint)
        {
            shortDir.Normalize();
            Vector3 lh = Vector3.ProjectOnPlane(longHint, shortDir);
            if (lh.sqrMagnitude < 1e-4f) lh = Vector3.ProjectOnPlane(Vector3.forward, shortDir);
            if (lh.sqrMagnitude < 1e-4f) lh = Vector3.ProjectOnPlane(Vector3.right, shortDir);
            return Quaternion.LookRotation(shortDir, lh.normalized);
        }

        /// <summary>Standing piece: long axis on the grain, broad face toward <paramref name="face"/> (rolled by up to rollDeg).</summary>
        private Quaternion Upright(float jitter, Vector3 face, float rollDeg)
        {
            Vector3 l = Jitter(m_up, jitter);
            Vector3 f = Quaternion.AngleAxis(R(-rollDeg, rollDeg), l) * face;
            return FrameLong(l, f);
        }

        /// <summary>Lying piece: flat (short) side on the bedding plane, long axis along strike.</summary>
        private Quaternion Bedded(float jitter, float extraLooseness = 0f)
        {
            float loose = (1f - m_style.beddingAlign) * 35f + extraLooseness;
            Vector3 n = Jitter(m_up, jitter + loose);
            Vector3 l = Quaternion.AngleAxis(R(-jitter, jitter) + (Chance(0.5f) ? 0f : 180f), n) * m_strike;
            return FrameShort(n, l);
        }

        private Quaternion BeddedAlong(Vector3 longHint, float jitter, Vector3 normal)
        {
            Vector3 n = Jitter(normal, jitter);
            Vector3 l = Quaternion.AngleAxis(R(-jitter, jitter), n) * longHint;
            return FrameShort(n, l);
        }
    }
}
