using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    /// <summary>Shape class of a kit piece, measured from its vertices (PCA extents).</summary>
    public enum DmRockPieceClass { Tall = 0, Boulder = 1, Slab = 2, Small = 3 }

    [Flags]
    public enum DmRockPieceClassMask
    {
        None = 0, Tall = 1 << 0, Boulder = 1 << 1, Slab = 1 << 2, Small = 1 << 3,
        Any = Tall | Boulder | Slab | Small,
    }

    /// <summary>
    /// Pre-measured kit piece (cached in the kit by the kit editor's Analyze, refreshed on Scan).
    /// Frame = principal axes of the piece's vertices: frame up = long axis, frame right = mid axis, frame forward = short axis.
    /// </summary>
    [Serializable]
    public sealed class DmRockPieceInfo
    {
        public GameObject prefab;
        public DmRockPieceClass pieceClass;
        [Tooltip("Rotation from the frame (x = mid, y = long, z = short) to the prefab's local space.")]
        public Quaternion frame = Quaternion.identity;
        [Tooltip("Frame origin in prefab space (centre of the frame-aligned bounds).")]
        public Vector3 center;
        [Tooltip("Full extents along mid (x), long (y) and short (z) axes, meters at scale 1.")]
        public Vector3 size = Vector3.one;
        [Tooltip("Dominant long axis expressed in prefab local space.")]
        public Vector3 longAxis = Vector3.up;
        [Tooltip("Cross-section width (mid axis) in the top / bottom 15% of the long axis, meters at scale 1.")]
        public float topWidth, bottomWidth;
        [Range(0f, 1f), Tooltip("Fraction of (welded) edges with a dihedral angle above 35 degrees. Higher = crisper fractures.")]
        public float sharpness;
        [Tooltip("Long axis points mostly up in the prefab (tall group pieces are authored standing).")]
        public bool authoredUpright;
        public int vertexCount, triangleCount;

        public float Long => size.y;
        public float Mid => size.x;
        public float Short => size.z;
        public float Aspect => size.y / Mathf.Max(1e-4f, size.x);
        public float Flatness => size.z / Mathf.Max(1e-4f, size.x);
        public DmRockPieceClassMask Mask => (DmRockPieceClassMask)(1 << (int)pieceClass);
    }

    /// <summary>Source pieces for DmRockCombiner: which prefabs may be a base and which may be stuck onto it.</summary>
    [CreateAssetMenu(fileName = "DM_RockKit", menuName = "Genesis PCG Rock Creation/Rock Kit")]
    public sealed class DmRockKit : ScriptableObject
    {
        public static event Action<DmRockKit> Changed;

        [Header("Scan rules (Inspector > Scan Folder fills the lists below)")]
        [Tooltip("Project folder with the pack's prefabs, e.g. Assets/SomeRockPack/Prefabs.")]
        public string scanFolder = "";
        public bool scanSubfolders = true;
        [Tooltip("Prefabs whose name contains this (case-insensitive) are base candidates only (e.g. 'group').")]
        public string baseNameContains = "group";
        [Min(0f), Tooltip("Single rocks at least this big (largest extent, meters) are also base candidates.")]
        public float baseMinExtent = 1.9f;

        [Header("Classification (Analyze)")]
        [Min(1f), Tooltip("Long / mid extent at or above this = Tall (vertical) piece.")]
        public float tallAspect = 1.5f;
        [Range(0.05f, 1f), Tooltip("Short / mid extent at or below this = Slab.")]
        public float slabFlatness = 0.42f;
        [Min(0f), Tooltip("Pieces whose long extent is below this (meters) are Small.")]
        public float smallExtent = 1.45f;

        [Header("Pieces")]
        [Tooltip("Prefabs that can be the main body of a combined rock.")]
        public List<GameObject> baseCandidates = new List<GameObject>();
        [Tooltip("Prefabs that can be added onto the base.")]
        public List<GameObject> addOnCandidates = new List<GameObject>();

        [Tooltip("Measured pieces (filled by Analyze / Scan). The assembler uses these for every style.")]
        public List<DmRockPieceInfo> pieces = new List<DmRockPieceInfo>();

        [NonSerialized] public int Version;

        public DmRockPieceInfo Find(GameObject prefab)
        {
            foreach (DmRockPieceInfo p in pieces)
                if (p != null && p.prefab == prefab) return p;
            return null;
        }

        private void OnValidate()
        {
            Version++;
            Changed?.Invoke(this);
        }
    }
}
