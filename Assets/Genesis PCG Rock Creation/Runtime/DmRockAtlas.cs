using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    /// <summary>
    /// Shared texture atlas for baked rock combiners (one per kit material set + atlas size + material override).
    /// Holds the BaseColor / Normal / HDRP-mask atlases, the material that uses them and the UV rect of every source material.
    /// Created by the editor baker under Generated/Atlases/&lt;key&gt;/ and reused by every rock that needs the same set.
    /// </summary>
    public sealed class DmRockAtlas : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public Material source;
            [Tooltip("Asset GUID + local id of the source material (stable cache key part).")]
            public string sourceId;
            [Tooltip("Normalized atlas rect (padding excluded). Unused for texture arrays.")]
            public Rect uvRect;
            [Tooltip("Texture arrays: array group (0 = group A, 1 = group B / 4K) and slice index.")]
            public int group;
            public int slice;
        }

        public string key;
        public int size;
        public int padding;
        public Material materialOverride;
        public List<Entry> entries = new List<Entry>();
        public Texture2D baseColor;
        public Texture2D normal;
        public Texture2D mask;

        [Header("Texture arrays (Texture Array mode)")]
        [Tooltip("True: base maps are Texture2DArrays (one slice per material at native resolution); entries hold group + slice.")]
        public bool isArray;
        [Tooltip("Slice size per array group (A, B). 0 = group unused.")]
        public int[] groupSize = new int[0];
        public int[] groupMaskSize = new int[0];
        public Texture2DArray[] baseArrays = new Texture2DArray[0];
        public Texture2DArray[] normalArrays = new Texture2DArray[0];
        public Texture2DArray[] maskArrays = new Texture2DArray[0];
        [Tooltip("Chosen slice size mode and the result, for the inspector (e.g. 'Auto: 2048 (37 materials)').")]
        public string sizeNote;
        [Tooltip("GPU memory of the arrays (MB), measured at build time.")]
        public float vramMB;
        [Tooltip("Average linear luminance of the base slices (blend shader reference).")]
        public float averageLuminance = 0.1f;
        [Tooltip("Material that samples the atlases. Swap for the blend shader later; the textures and rects stay the same.")]
        public Material material;

        public bool TryGetRect(Material source, out Rect rect)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].source == source)
                {
                    rect = entries[i].uvRect;
                    return true;
                }
            rect = default;
            return false;
        }

        public bool IsComplete => isArray
            ? material != null && entries.Count > 0 && baseArrays != null && baseArrays.Length > 0 && baseArrays[0] != null
              && normalArrays != null && normalArrays.Length == baseArrays.Length && maskArrays != null && maskArrays.Length == baseArrays.Length
            : baseColor != null && normal != null && mask != null && material != null && entries.Count > 0;

        public bool TryGetSlice(Material source, out int group, out int slice)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].source == source)
                {
                    group = entries[i].group;
                    slice = entries[i].slice;
                    return true;
                }
            group = slice = 0;
            return false;
        }
    }
}
