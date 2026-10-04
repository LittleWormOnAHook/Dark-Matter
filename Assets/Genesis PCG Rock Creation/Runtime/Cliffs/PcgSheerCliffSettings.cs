using System;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    /// <summary>Which side of the path (looking along it) the cliff face looks toward.</summary>
    public enum PcgCliffFaceSide { Right = 0, Left = 1 }

    /// <summary>
    /// Sheer cliff (phase 1 of the cliff tool): one tier, 15-80 m, a near-vertical 80-90 degree face of tall joint-aligned
    /// kit pieces, a small cap lip (0.5 m max) and a coarse talus apron at 34-37 degrees with the big blocks at the toe.
    /// </summary>
    [Serializable]
    public sealed class PcgSheerCliffSettings
    {
        [Header("Source")]
        [Tooltip("Measured kit (Tall pieces build the face, Slabs the cap, Boulders / Small pieces the talus). Pieces keep their own pack materials.")]
        public DmRockKit kit;
        public int seed = 1;

        [Header("Path")]
        [Tooltip("Side the face looks toward, walking along the path from its first point.")]
        public PcgCliffFaceSide faceSide = PcgCliffFaceSide.Right;
        [Range(2f, 12f), Tooltip("Width of one wall module along the path (m).")]
        public float moduleWidth = 5f;
        [Range(1.5f, 10f), Tooltip("Thickness of the wall modules (m).")]
        public float moduleDepth = 3.5f;
        [Range(0f, 0.4f), Tooltip("Side overlap between neighbouring modules (0.1-0.2 hides the seams).")]
        public float overlap = 0.15f;
        [Range(4f, 60f), Tooltip("Turn (degrees across one module) above which a station counts as convex / concave.")]
        public float curvatureThreshold = 12f;
        [Tooltip("Sample the terrain / colliders under each station; off = use the path points' own height.")]
        public bool followGround = true;

        [Header("Profile")]
        [Range(15f, 80f), Tooltip("Face height above the ground (m).")]
        public float height = 30f;
        [Range(0f, 0.4f), Tooltip("Height variation along the path (fraction of the height).")]
        public float heightNoise = 0.15f;
        [Range(10f, 200f), Tooltip("Wavelength of the height variation (m).")]
        public float heightNoiseScale = 45f;
        [Range(1f, 3f), Tooltip("How far the wall sinks below the ground (m), so no gap shows at the foot.")]
        public float sink = 2f;
        [Range(80f, 90f), Tooltip("Face angle: 90 = vertical, lower leans the top back into the cliff.")]
        public float faceAngle = 86f;
        [Range(4f, 14f), Tooltip("Target height of one stacked wall piece (m).")]
        public float rowHeight = 8f;
        [Tooltip("A second, plainer column row behind the face so no light shows through the overlaps.")]
        public bool backfill = true;
        [Tooltip("Open paths: the first / last modules drop toward 55% height instead of ending as a full wall.")]
        public bool taperEnds = true;

        [Header("Cap")]
        public bool cap = true;
        [Range(0f, 0.5f), Tooltip("How far the cap overhangs the face (m, 0.5 max for a sheer cliff).")]
        public float lip = 0.3f;
        [Range(0.5f, 3f), Tooltip("Cap slab thickness (m).")]
        public float capThickness = 1.4f;

        [Header("Jitter")]
        [Range(0f, 15f)] public float yawJitter = 6f;
        [Range(0f, 0.35f)] public float scaleJitter = 0.15f;
        [Range(0f, 8f), Tooltip("Random extra lean per piece (degrees).")]
        public float tiltJitter = 2f;

        [Header("Talus apron")]
        public bool talus = true;
        [Range(30f, 40f), Tooltip("Repose angle of the apron (34-37 for coarse talus).")]
        public float talusAngle = 35f;
        [Range(0.1f, 0.6f), Tooltip("Apron width as a fraction of the face height.")]
        public float apronWidth = 0.3f;
        [Range(0.3f, 3f)] public float talusDensity = 1f;
        [Range(0.3f, 2f), Tooltip("Debris size near the wall (m).")]
        public float talusMinSize = 0.6f;
        [Range(1f, 6f), Tooltip("Block size at the toe (m).")]
        public float talusMaxSize = 3f;
        [Range(0.3f, 4f), Tooltip("Size grading: higher keeps the debris small longer and puts the big blocks right at the toe.")]
        public float grading = 1.6f;

        [Header("Output")]
        [Range(20f, 40f), Tooltip("Path length per chunk (one mesh, LOD group and collider each).")]
        public float chunkLength = 30f;
        [Range(1, 4)] public int lodCount = 3;
        public bool collision = true;
    }
}
