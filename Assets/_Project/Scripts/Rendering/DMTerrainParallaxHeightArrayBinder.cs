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
        private static void RegisterLifecycleHooks()
        {
            Application.quitting -= DestroyAllRuntimeTextures;
            Application.quitting += DestroyAllRuntimeTextures;
            DestroyAllRuntimeTextures();
        }

        private static void DestroyAllRuntimeTextures()
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
                grayW = grayH = 0;
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
                ReleaseTrackedArray(array);
                array = new Texture2DArray(width, height, LayerCount, format, true, false)
                {
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear,
                    name = material.name + "_DMHeightMapArray_Runtime"
                };
            }

            // Round7: gray fallback in the array's own format (block-compressed when the array is DXT1/DXT5) so
            // unused layers never take a mismatched-format CopyTexture.
            Texture2D fallback = GetGraySliceForArray(array);
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

            ReleaseTrackedArray(existing);
            Texture2DArray array = new Texture2DArray(4, 4, LayerCount, TextureFormat.R8, false, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = material.name + "_DMHeightMapArray_Default"
            };

            // Round10: gray in the array's own R8 format (was RGBA32 -> CopyTexture size mismatch in player builds).
            Texture2D gray = GetGraySliceForArray(array);
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

        private static void ReleaseTrackedArray(Texture2DArray array)
        {
            if (array == null)
                return;

            RuntimeArrays.Remove(array);
            Object.Destroy(array);
        }

        private static int grayW;
        private static int grayH;
        private const TextureFormat GrayFallbackFormat = TextureFormat.RGBA32;

        private static Texture2D GetGraySlice(int width, int height)
        {
            if (grayFallback != null && grayW == width && grayH == height)
                return grayFallback;

            if (grayFallback != null)
            {
                Object.Destroy(grayFallback);
                grayFallback = null;
            }

            // Always CPU-writable: legacy height slices may use compressed formats unsuitable for SetPixels.
            grayFallback = new Texture2D(width, height, GrayFallbackFormat, false, true)
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
            return grayFallback;
        }

        // Round7: cached block-compressed (or plain) mid-gray slice matching the array (one per size/format).
        private static Texture2D grayArraySlice;

        private static Texture2D GetGraySliceForArray(Texture2DArray array)
        {
            TextureFormat fmt = array.format;
            if (fmt != TextureFormat.DXT1 && fmt != TextureFormat.DXT5)
                return GetGraySliceUncompressed(array.width, array.height, fmt);
            if (grayArraySlice != null && grayArraySlice.width == array.width && grayArraySlice.height == array.height && grayArraySlice.format == fmt)
                return grayArraySlice;
            if (grayArraySlice != null)
                Object.Destroy(grayArraySlice);

            int bw = Mathf.Max(1, (array.width + 3) / 4);
            int bh = Mathf.Max(1, (array.height + 3) / 4);
            int blockBytes = fmt == TextureFormat.DXT1 ? 8 : 16;
            byte[] data = new byte[bw * bh * blockBytes];
            // RGB565 mid-gray (r=15/31, g=31/63, b=15/31), both endpoints equal, all indices 0.
            const ushort gray565 = (15 << 11) | (31 << 5) | 15;
            for (int b = 0; b < bw * bh; b++)
            {
                int o = b * blockBytes;
                if (fmt == TextureFormat.DXT5)
                {
                    data[o] = 255; data[o + 1] = 255; // alpha endpoints, indices 0 = opaque
                    o += 8;
                }
                data[o] = (byte)(gray565 & 0xFF); data[o + 1] = (byte)(gray565 >> 8);
                data[o + 2] = (byte)(gray565 & 0xFF); data[o + 3] = (byte)(gray565 >> 8);
            }
            grayArraySlice = new Texture2D(array.width, array.height, fmt, false, true)
            {
                name = "DMHeightMap_GrayFallback_" + fmt,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            grayArraySlice.LoadRawTextureData(data);
            grayArraySlice.Apply(false, false);
            return grayArraySlice;
        }

        // Round10: matching-format gray for uncompressed arrays (R8 default array etc.); cached per size/format.
        private static readonly Dictionary<(int, int, TextureFormat), Texture2D> grayUncompressed =
            new Dictionary<(int, int, TextureFormat), Texture2D>(2);

        private static Texture2D GetGraySliceUncompressed(int width, int height, TextureFormat fmt)
        {
            if (fmt == GrayFallbackFormat)
                return GetGraySlice(width, height);
            var key = (width, height, fmt);
            if (grayUncompressed.TryGetValue(key, out Texture2D t) && t != null)
                return t;
            if (!SystemInfo.SupportsTextureFormat(fmt))
                return GetGraySlice(width, height);
            t = new Texture2D(width, height, fmt, false, true)
            {
                name = "DMHeightMap_GrayFallback_" + fmt,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            Color32[] px = new Color32[width * height];
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color32(128, 128, 128, 255);
            t.SetPixels32(px);
            t.Apply(false, true);
            grayUncompressed[key] = t;
            return t;
        }

        private static bool warnedUnconvertible;
        private static readonly Dictionary<(Texture2D, int, int, TextureFormat), Texture2D> compressedResamples =
            new Dictionary<(Texture2D, int, int, TextureFormat), Texture2D>(4);

        private static Texture2D GetCompressedResample(Texture2D source, Texture2DArray array)
        {
            TextureFormat fmt = array.format;
            if (fmt != TextureFormat.DXT1 && fmt != TextureFormat.DXT5)
                return null;
            var key = (source, array.width, array.height, fmt);
            if (compressedResamples.TryGetValue(key, out Texture2D cached) && cached != null)
                return cached;

            RenderTexture rt = RenderTexture.GetTemporary(array.width, array.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D t = new Texture2D(array.width, array.height, fmt == TextureFormat.DXT1 ? TextureFormat.RGB24 : TextureFormat.RGBA32, false, true)
            {
                name = source.name + "_DMHeightResample",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            t.ReadPixels(new Rect(0, 0, array.width, array.height), 0, 0);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            t.Compress(false);
            if (t.format != fmt)
            {
                Object.Destroy(t);
                return null;
            }
            t.Apply(false, true);
            compressedResamples[key] = t;
            return t;
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

            // Round7: formats/sizes differ. Uncompressed destination -> GPU convert straight into the slice
            // (no CPU readback, no allocation). Compressed destination cannot be a render target, so fall back
            // to the matching-format gray slice instead of a mismatched CopyTexture (the old error source).
            if (!UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsCompressedFormat(array.graphicsFormat))
            {
                if (Graphics.ConvertTexture(source, 0, array, slice))
                    return;
            }
            else
            {
                // Round9: resample + recompress mismatched slices once (cached per source/size/format) so the
                // layer keeps its real height instead of gray. DXT1/DXT5 targets only (runtime Compress support).
                Texture2D converted = GetCompressedResample(source, array);
                if (converted != null)
                {
                    Graphics.CopyTexture(converted, 0, 0, array, slice, 0);
                    return;
                }

                Texture2D gray = GetGraySliceForArray(array);
                if (gray != null && gray != source && gray.format == array.format && gray.width == array.width && gray.height == array.height)
                {
                    if (!warnedUnconvertible)
                    {
                        warnedUnconvertible = true;
                        Debug.LogWarning($"[DMTerrainParallax] height slice '{source.name}' ({source.format} {source.width}x{source.height}) does not match array ({array.format} {array.width}x{array.height}); using gray for that layer. Re-import the slice in the array's format/size to use it.");
                    }
                    Graphics.CopyTexture(gray, 0, 0, array, slice, 0);
                    return;
                }
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
            if (readable.format == array.format)
                Graphics.CopyTexture(readable, 0, 0, array, slice, 0);
            Object.Destroy(readable);
        }
    }
}
