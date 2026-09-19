using UnityEngine;

namespace Project.World.TerrainSplat
{
    /// <summary>
    /// Sidecar tunables for one Unity <see cref="TerrainLayer"/> (height blend, parallax, normal scale).
    /// Pair with a splat layer asset under World/Terrain/Layers; catalog rows can reference this instead of inline fields.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Dark Matter/World/Terrain Layer Render Options",
        fileName = "DMTerrainLayerRenderOptions")]
    public sealed class DMTerrainLayerRenderOptions : ScriptableObject
    {
        [Tooltip("TerrainLayer asset this sidecar configures.")]
        public TerrainLayer terrainLayer;

        [Header("Layer-to-layer blend (HDRP height blend)")]
        [Tooltip("When off, height (Mask B) is flattened so this layer blends softly without height fighting.")]
        public bool useHeightBasedBlend = true;

        [Tooltip("Remap contrast for Mask B before height blend. 1 = as authored.")]
        [Range(0.25f, 2f)]
        public float heightBlendContrast = 1f;

        [Tooltip("Bias added to remapped height (-1..1).")]
        [Range(-0.5f, 0.5f)]
        public float heightBlendBias;

        [Header("Parallax (select layers)")]
        [Tooltip("Request parallax / POM for this splat. Stock HDRP Terrain Lit ignores this until a DM terrain shader reads SplatLayerParams.")]
        public bool enableParallax;

        [Tooltip("Parallax amplitude (world-scale feel; tune in Genesis Studio).")]
        [Range(0f, 0.08f)]
        public float parallaxStrength = 0.02f;

        [Tooltip("Optional POM step count hint for custom terrain shaders.")]
        [Range(4, 32)]
        public int parallaxMaxSteps = 12;

        [Header("Surface response")]
        [Tooltip("When >= 0, overrides TerrainLayer.normalScale when applying the catalog.")]
        [Range(-1f, 2f)]
        public float normalScaleOverride = -1f;

        [Tooltip("When > 0, overrides TerrainLayer tile size (meters) for this layer.")]
        public float tileSizeWorldMeters;
    }
}
