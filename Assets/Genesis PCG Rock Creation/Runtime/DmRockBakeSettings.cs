using System;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    public enum DmRockAtlasSize { _2048 = 2048, _4096 = 4096, _8192 = 8192 }

    /// <summary>How the kit's base maps are combined for a baked rock.</summary>
    public enum DmRockTextureMode
    {
        [Tooltip("Texture2DArrays of the kit's materials at native resolution (default): one slice per material, the mesh keeps each material's own UVs.")]
        TextureArray = 0,
        [Tooltip("Legacy: one shared 8192 atlas (every material scaled into a tile; lower texel density).")]
        Atlas = 1,
    }

    /// <summary>Texture array slice size. Auto = from the source textures (2K or 4K, separate arrays per resolution).</summary>
    public enum DmRockArraySliceSize { Auto = 0, _2048 = 2048, _4096 = 4096 }

    /// <summary>What happens to rock geometry below the terrain on bake.</summary>
    public enum DmRockGroundCut
    {
        [Tooltip("Clip the mesh against the ground heightfield: triangles crossing the ground are split along the intersection, the part below is discarded (bottom edge follows the ground exactly).")]
        FlushClip = 0,
        [Tooltip("Legacy: drop only whole triangles that are completely below the ground by more than the margin (jagged edge under the ground).")]
        CullWholeTriangles = 1,
        [Tooltip("Keep everything below the ground.")]
        Off = 2,
    }

    /// <summary>Bake (atlas + single mesh + LODs) settings, stored on the combine recipe.</summary>
    [Serializable]
    public sealed class DmRockBakeSettings
    {
        [Tooltip("Atlas resolution for the shared BaseColor / Normal / Mask atlas of the kit's materials (project standard: 8192).")]
        public DmRockAtlasSize atlasSize = DmRockAtlasSize._8192;
        [Min(8), Tooltip("Pixels of edge dilation around every atlas tile (mip bleed protection). Scale with the atlas: 16 px at 4096 = 32 px at 8192.")]
        public int atlasPadding = 32;

        public const int StandardAtlasSize = 8192;
        public const int StandardAtlasPadding = 32;

        [Header("Textures")]
        [Tooltip("Texture Array (default): native-resolution Texture2DArrays per kit material set. Atlas: legacy 8192 atlas.")]
        public DmRockTextureMode textureMode = DmRockTextureMode.TextureArray;
        [Tooltip("Array slice size. Auto reads the source texture size of the kit's materials: 2K sources -> 2K slices, 4K sources -> a separate 4K array (no upscaling). 2K / 4K force one size (4K only upscales 2K sources).")]
        public DmRockArraySliceSize arraySliceSize = DmRockArraySliceSize.Auto;
        [Tooltip("Mask array (metallic / AO / smoothness) at half the colour slice size: saves ~3/4 of the mask VRAM; masks are low-frequency (most pack masks are 1K anyway).")]
        public bool arrayHalfResMask = true;

        [Tooltip("Generate LODs (Gaia's mesh simplifier when present, else the built-in clustering simplifier).")]
        public bool generateLods = true;
        [Tooltip("Screen-relative transition height where each LOD ends (LOD0, LOD1, ...). The last value is the cull height.")]
        public float[] lodTransitions = { 0.60f, 0.45f, 0.25f, 0.13f };
        public bool crossFade = true;
        [Range(0.1f, 1f), Tooltip("LOD0 detail kept on bake (1 = full merged mesh). Below 1 the merged mesh is simplified first and LOD1..n (and the LOD2 collider) are simplified from it, so the whole chain gets cheaper. Needs a rebake.")]
        public float lod0Quality = 0.7f;
        [Min(0), Tooltip("The farthest N LODs cast no shadows (far rocks' shadows are barely visible but each costs a shadow draw per cascade). Applied when the rock is enabled, no rebake needed. 0 = every LOD casts.")]
        public int lastLodsWithoutShadows = 1;

        [Header("Hidden geometry cull (before LODs)")]
        [Tooltip("Remove rock geometry below the ground under it (see Ground Cut). Off keeps everything.")]
        public bool cullBelowGround = true;
        [Tooltip("Flush Clip (default): split triangles along the ground surface and discard the part below. Cull Whole Triangles: legacy.")]
        public DmRockGroundCut groundCut = DmRockGroundCut.FlushClip;
        [Tooltip("Flush Clip: cut height relative to the ground (m). Slightly negative = just below the surface, so no gap shows at the contact.")]
        public float groundCutOffset = -0.02f;
        [Min(0f), Tooltip("Cull Whole Triangles: a triangle is dropped when all three vertices are more than this below the ground.")]
        public float cullBelowGroundMargin = 0.1f;
        [Tooltip("Drop triangles of a piece that are completely inside another (closed) piece of the same rock.")]
        public bool cullInterior = true;
        [Min(0f), Tooltip("A point only counts as inside when it is still inside after moving this far out along its normal (keeps seams closed).")]
        public float interiorInset = 0.02f;

        [Header("Auto bake")]
        [Tooltip("Bake rocks automatically after a Shift+drag copy is released and after paste / Ctrl+D settles.")]
        public bool bakeOnShiftDragCopyPaste = true;
        [Tooltip("When a baked rock is moved / rotated / scaled: re-snap (height), re-conform, re-mask and rebake once the transform is stable (~0.3 s) or the mouse is released. Overwrites the rock's own baked asset.")]
        public bool autoRebakeOnMove = true;
        [Tooltip("A baked rock whose own settings are edited (seed, overrides...) goes back to the preview; when on, it is baked again once the change settles.")]
        public bool rebakeAfterEdit = true;

        public static readonly DmRockBakeSettings Default = new DmRockBakeSettings();
    }
}
