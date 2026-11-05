using System.Collections.Generic;
using UnityEngine;

namespace Project.Rendering
{
    /// <summary>
    /// 2026-11-05: DM/TerrainParallax packs eight per-layer height maps into one 2DArray shader slot (D3D11 64-texture limit).
    /// Materials still store legacy _DMHeightMap0..7 assignments; this binder copies them into _DMHeightMapArray at runtime without saving .mat files.
    /// </summary>
    public static class DMTerrainParallaxHeightArrayBinder
    {
        public const string ShaderName = "DM/TerrainParallax";
        public const string ArrayProperty = "_DMHeightMapArray";
        private const int LayerCount = 8;
        private const string LegacySlotPrefix = "_DMHeightMap";
        private static readonly List<Texture2DArray> RuntimeArrays = new List<Texture2DArray>(8);
        private static Texture2D grayFallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeTextures()
        {
            for (int i = RuntimeArrays.Count - 1; i >= 0; i--)
            {
                if (RuntimeArrays[i] != null)
                    Object.Destroy(RuntimeArrays[i]);
            }

            RuntimeArrays.Clear();
            if (grayFallback != null)
            {
                Object.Destroy(grayFallback);
                grayFallback = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BindAfterSceneLoad()
        {
            BindAllTerrains();
        }

        internal static void BindAllTerrains()
        {
            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains == null || terrains.Length == 0)
                return;

            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null)
                    continue;

                Material template = terrain.materialTemplate;
                if (template == null || template.shader == null || template.shader.name != ShaderName)
                    continue;

                TryBindMaterial(template);
            }
        }

        internal static bool TryBindMaterial(Material material)
        {
            if (material == null || material.shader == null || material.shader.name != ShaderName)
                return false;

            if (!material.HasProperty(ArrayProperty))
                return false;

            Texture2D[] slices = new Texture2D[LayerCount];
            bool anyCustom = false;
            int width = 4;
            int height = 4;
            TextureFormat format = TextureFormat.R8;

            for (int i = 0; i < LayerCount; i++)
            {
                string legacyId = LegacySlotPrefix + i;
                // Legacy slots may remain on the material after the shader drops the Properties entries.
                Texture2D slice = material.GetTexture(legacyId) as Texture2D;

                if (slice != null)
                {
                    anyCustom = true;
                    width = Mathf.Max(width, slice.width);
                    height = Mathf.Max(height, slice.height);
                    format = slice.format;
                }

                slices[i] = slice;
            }

            if (!anyCustom)
            {
                EnsureDefaultGrayArray(material);
                return true;
            }

            Texture2DArray array = material.GetTexture(ArrayProperty) as Texture2DArray;
            if (array == null || array.depth != LayerCount || array.width != width || array.height != height)
            {
                array = new Texture2DArray(width, height, LayerCount, format, true, false)
                {
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear,
                    name = material.name + "_DMHeightMapArray_Runtime"
                };
            }

            Texture2D fallback = GetGraySlice(width, height, format);
            for (int i = 0; i < LayerCount; i++)
                CopySliceIntoArray(slices[i] != null ? slices[i] : fallback, array, i);

            array.Apply(false, true);
            material.SetTexture(ArrayProperty, array);
            TrackRuntimeArray(array);
            return true;
        }

        private static void EnsureDefaultGrayArray(Material material)
        {
            Texture2DArray existing = material.GetTexture(ArrayProperty) as Texture2DArray;
            if (existing != null && existing.depth == LayerCount)
                return;

            Texture2DArray array = new Texture2DArray(4, 4, LayerCount, TextureFormat.R8, false, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = material.name + "_DMHeightMapArray_Default"
            };

            Texture2D gray = GetGraySlice(4, 4, TextureFormat.R8);
            for (int i = 0; i < LayerCount; i++)
                CopySliceIntoArray(gray, array, i);

            array.Apply(false, true);
            material.SetTexture(ArrayProperty, array);
            TrackRuntimeArray(array);
        }

        private static void TrackRuntimeArray(Texture2DArray array)
        {
            if (array == null || RuntimeArrays.Contains(array))
                return;

            RuntimeArrays.Add(array);
        }

        private static int grayW;
        private static int grayH;
        private static TextureFormat grayFormat;

        private static Texture2D GetGraySlice(int width, int height, TextureFormat format)
        {
            if (grayFallback != null && grayW == width && grayH == height && grayFormat == format)
                return grayFallback;

            grayFallback = new Texture2D(width, height, format, false, true)
            {
                name = "DMHeightMap_GrayFallback",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(0.5f, 0.5f, 0.5f, 1f);

            grayFallback.SetPixels(pixels);
            grayFallback.Apply(false, true);
            grayW = width;
            grayH = height;
            grayFormat = format;
            return grayFallback;
        }

        private static void CopySliceIntoArray(Texture2D source, Texture2DArray array, int slice)
        {
            if (source == null || array == null)
                return;

            if (source.width == array.width && source.height == array.height && source.format == array.format)
            {
                Graphics.CopyTexture(source, 0, 0, array, slice, 0);
                return;
            }

            RenderTexture rt = RenderTexture.GetTemporary(array.width, array.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D readable = new Texture2D(array.width, array.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, array.width, array.height), 0, 0);
            readable.Apply(false, false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Graphics.CopyTexture(readable, 0, 0, array, slice, 0);
            Object.Destroy(readable);
        }
    }
}
