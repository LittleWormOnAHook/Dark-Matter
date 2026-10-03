using System;
using UnityEngine;

namespace Project.World.ProcPlacement
{
    /// <summary>
    /// Shared settings for a family of procedurally placed objects.
    /// Every DmProcPlacer that points at this recipe uses the same material and rules,
    /// and only its seed decides the individual shape.
    /// </summary>
    [CreateAssetMenu(fileName = "DM_ProcRecipe", menuName = "Dark Matter Genesis/World/Proc Recipe")]
    public sealed class DmProcRecipe : ScriptableObject
    {
        public enum GeneratorKind
        {
            NoiseRock = 0,
            // KitCluster = 1  (Phase 4)
        }

        /// <summary>Raised when any recipe field changes so live placers can rebuild.</summary>
        public static event Action<DmProcRecipe> Changed;

        [Header("Generator")]
        public GeneratorKind generator = GeneratorKind.NoiseRock;
        [Tooltip("Material every copy uses. Copies roll a new shape but always keep this material.")]
        public Material material;
        [Tooltip("Adds a MeshCollider using the generated mesh (needed for snapping/overlap later).")]
        public bool addMeshCollider = true;

        [Header("Size (meters, full extents)")]
        public Vector3 sizeMin = new Vector3(1.6f, 1.1f, 1.6f);
        public Vector3 sizeMax = new Vector3(3.6f, 2.6f, 3.2f);
        [Range(0f, 1f), Tooltip("0 = each axis rolls its own size (stretched shapes), 1 = all axes share one roll.")]
        public float uniformScaleBias = 0.35f;
        [Range(0f, 45f), Tooltip("Random tilt of the stretch axes so shapes don't all line up.")]
        public float tiltJitter = 12f;

        [Header("Shape")]
        [Range(2, 6), Tooltip("Icosphere subdivisions. 4 = 5k tris, 5 = 20k tris, 6 = 82k tris.")]
        public int subdivisions = 5;
        [Min(0.01f)] public float noiseFrequency = 1.35f;
        [Range(0f, 1f)] public float noiseAmplitude = 0.32f;
        [Range(1, 8)] public int octaves = 5;
        [Min(1f)] public float lacunarity = 2.05f;
        [Range(0f, 1f)] public float gain = 0.5f;
        [Range(0f, 1f), Tooltip("Sharp ridges/cracks layered on top of the base noise.")]
        public float ridgeAmplitude = 0.22f;
        [Range(0f, 0.9f), Tooltip("How much of the underside gets flattened so the rock sits on the ground.")]
        public float bottomFlatten = 0.22f;
        [Range(0, 24), Tooltip("Horizontal rock layers (outcrop look). 0 = off.")]
        public int strataSteps = 0;
        [Range(0f, 1f)] public float strataStrength = 0.4f;

        [Header("UV")]
        [Min(0.001f), Tooltip("Texture repeats per meter.")]
        public float uvScale = 0.5f;

        [NonSerialized] public int Version;

        private void OnValidate()
        {
            sizeMax = Vector3.Max(sizeMax, sizeMin);
            Version++;
            Changed?.Invoke(this);
        }
    }
}
