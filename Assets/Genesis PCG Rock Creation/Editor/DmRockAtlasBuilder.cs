#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GenesisPCG.RockCreation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Builds (or reuses) the shared BaseColor / Normal / HDRP-mask atlas for a set of source materials.
    /// Cache key = hash(sorted material ids + atlas size + padding + material override). Source textures are read through
    /// RenderTexture blits (import settings are never touched). Each tile gets edge-clamp dilation into its padding.
    /// </summary>
    public static class DmRockAtlasBuilder
    {
        public const string AtlasRoot = "Assets/Genesis PCG Rock Creation/Generated/Atlases";
        internal const string BaseSuffix = "_BaseColor", NormalSuffix = "_Normal", MaskSuffix = "_Mask";

        public static string MaterialId(Material m)
        {
            if (m == null) return "null";
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m, out string guid, out long local))
                return guid + ":" + local;
            return "scene:" + m.GetEntityId();
        }

        public static string MakeKey(IEnumerable<Material> materials, int size, int padding, Material materialOverride)
        {
            var ids = materials.Where(m => m != null).Select(MaterialId).Distinct().OrderBy(s => s, StringComparer.Ordinal);
            string text = string.Join("|", ids) + "#" + size + "#" + padding + "#" + MaterialId(materialOverride);
            return Hash128.Compute(text).ToString().Substring(0, 12);
        }

        public static DmRockAtlas Find(string key)
        {
            string path = $"{AtlasRoot}/{key}/{key}_Atlas.asset";
            var a = AssetDatabase.LoadAssetAtPath<DmRockAtlas>(path);
            return a != null && a.IsComplete ? a : null;
        }

        /// <summary>Returns the cached atlas for this material set or builds it once.</summary>
        /// <summary>Wall time of the last atlas build (PNG writes + imports + compression), for reports.</summary>
        public static float LastBuildSeconds { get; private set; }

        public static DmRockAtlas GetOrCreate(IList<Material> materials, int size, int padding, Material materialOverride, out bool created)
        {
            created = false;
            var mats = materials.Where(m => m != null).Distinct().OrderBy(MaterialId, StringComparer.Ordinal).ToList();
            if (mats.Count == 0)
                return null;
            padding = Mathf.Max(8, padding);
            string key = MakeKey(mats, size, padding, materialOverride);
            DmRockAtlas existing = Find(key);
            if (existing != null)
                return existing;

            created = true;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            DmRockAtlas built = Build(key, mats, size, padding, materialOverride);
            LastBuildSeconds = (float)sw.Elapsed.TotalSeconds;
            return built;
        }

        // ------------------------------------------------------------------------------------------------

        private struct Tile { public int x, y, size; }

        private static DmRockAtlas Build(string key, List<Material> mats, int size, int padding, Material materialOverride)
        {
            string folder = $"{AtlasRoot}/{key}";
            EnsureFolder(folder);

            // Layout: one square tile per material, sized by its base map (common scale so texel density stays even).
            var srcSizes = mats.Select(m => { Texture t = GetTex(m, "_BaseColorMap", "_MainTex"); return t != null ? Mathf.Max(t.width, t.height) : 256; }).ToList();
            Tile[] tiles = Layout(srcSizes, size, padding);
            if (tiles == null)
                throw new InvalidOperationException($"Rock atlas: {mats.Count} materials do not fit a {size} atlas with {padding}px padding.");

            var baseC = new Color32[size * size];
            var normC = new Color32[size * size];
            var maskC = new Color32[size * size];
            Fill(baseC, new Color32(128, 128, 128, 255));
            Fill(normC, new Color32(128, 128, 255, 255));
            Fill(maskC, new Color32(0, 255, 255, 128));

            var entries = new List<DmRockAtlas.Entry>();
            for (int i = 0; i < mats.Count; i++)
            {
                Material m = mats[i];
                Tile t = tiles[i];
                Color tint = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : (m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white);

                Color32[] a = ReadTile(GetTex(m, "_BaseColorMap", "_MainTex"), t.size, true, new Color32(255, 255, 255, 255));
                if (tint != Color.white)
                {
                    Color lin = tint.linear;
                    for (int p = 0; p < a.Length; p++)
                    {
                        Color c = ((Color)a[p]).linear;
                        a[p] = (Color32)new Color(c.r * lin.r, c.g * lin.g, c.b * lin.b, 1f).gamma;
                    }
                }
                for (int p = 0; p < a.Length; p++) a[p].a = 255;

                Color32[] n = ReadTile(GetTex(m, "_NormalMap", "_BumpMap"), t.size, false, new Color32(128, 128, 255, 255));
                DecodeNormals(n);

                float metal = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
                float smooth = m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : 0.5f;
                Color32[] k = ReadTile(GetTex(m, "_MaskMap"), t.size, false,
                    new Color32((byte)Mathf.RoundToInt(metal * 255f), 255, 255, (byte)Mathf.RoundToInt(smooth * 255f)));

                Blit(baseC, size, a, t, padding);
                Blit(normC, size, n, t, padding);
                Blit(maskC, size, k, t, padding);
                entries.Add(new DmRockAtlas.Entry
                {
                    source = m,
                    sourceId = MaterialId(m),
                    uvRect = new Rect((float)t.x / size, (float)t.y / size, (float)t.size / size, (float)t.size / size),
                });
            }

            string pBase = $"{folder}/{key}{BaseSuffix}.png";
            string pNorm = $"{folder}/{key}{NormalSuffix}.png";
            string pMask = $"{folder}/{key}{MaskSuffix}.png";
            WritePng(pBase, baseC, size, false);
            WritePng(pNorm, normC, size, true);
            WritePng(pMask, maskC, size, true);
            // Only these three paths are imported; settings are then enforced directly on their TextureImporters.
            AssetDatabase.ImportAsset(pBase, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(pNorm, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(pMask, ImportAssetOptions.ForceSynchronousImport);
            ApplyImportSettings(pBase, size);
            ApplyImportSettings(pNorm, size);
            ApplyImportSettings(pMask, size);

            var texBase = AssetDatabase.LoadAssetAtPath<Texture2D>(pBase);
            var texNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(pNorm);
            var texMask = AssetDatabase.LoadAssetAtPath<Texture2D>(pMask);

            Material mat = CreateMaterial(materialOverride, texBase, texNorm, texMask);
            mat.name = key + "_Rock";
            string pMat = $"{folder}/{key}_Material.mat";
            AssetDatabase.CreateAsset(mat, pMat);

            var atlas = ScriptableObject.CreateInstance<DmRockAtlas>();
            atlas.key = key;
            atlas.size = size;
            atlas.padding = padding;
            atlas.materialOverride = materialOverride;
            atlas.entries = entries;
            atlas.baseColor = texBase;
            atlas.normal = texNorm;
            atlas.mask = texMask;
            atlas.material = mat;
            AssetDatabase.CreateAsset(atlas, $"{folder}/{key}_Atlas.asset");
            AssetDatabase.SaveAssetIfDirty(atlas);
            AssetDatabase.SaveAssetIfDirty(mat);
            return atlas;
        }

        /// <summary>
        /// Atlas import settings, set directly on the importer of that single path (reimports only if something differs).
        /// </summary>
        internal static void ApplyImportSettings(string path, int size)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
            string name = Path.GetFileNameWithoutExtension(path);
            bool normal = name.EndsWith(NormalSuffix, StringComparison.Ordinal);
            bool mask = name.EndsWith(MaskSuffix, StringComparison.Ordinal);
            bool dirty = false;
            void Set<T>(T cur, T want, Action<T> apply)
            {
                if (EqualityComparer<T>.Default.Equals(cur, want)) return;
                apply(want);
                dirty = true;
            }
            Set(ti.maxTextureSize, Mathf.Clamp(Mathf.NextPowerOfTwo(size), 32, 16384), v => ti.maxTextureSize = v);
            Set(ti.mipmapEnabled, true, v => ti.mipmapEnabled = v);
            Set(ti.wrapMode, TextureWrapMode.Clamp, v => ti.wrapMode = v);
            Set(ti.filterMode, FilterMode.Trilinear, v => ti.filterMode = v);
            Set(ti.anisoLevel, 4, v => ti.anisoLevel = v);
            Set(ti.isReadable, false, v => ti.isReadable = v);
            if (normal)
            {
                Set(ti.textureType, TextureImporterType.NormalMap, v => ti.textureType = v);
                Set(ti.textureCompression, TextureImporterCompression.CompressedHQ, v => ti.textureCompression = v);
            }
            else
            {
                Set(ti.textureType, TextureImporterType.Default, v => ti.textureType = v);
                Set(ti.sRGBTexture, !mask, v => ti.sRGBTexture = v);
                Set(ti.alphaSource, mask ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None, v => ti.alphaSource = v);
                Set(ti.alphaIsTransparency, false, v => ti.alphaIsTransparency = v);
                Set(ti.textureCompression, mask ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed, v => ti.textureCompression = v);
            }
            if (dirty) ti.SaveAndReimport();
        }

        /// <summary>HDRP/Lit (or a copy of the override material) using the atlases. Swappable for the blend shader later.</summary>
        internal static Material CreateMaterial(Material materialOverride, Texture2D baseTex, Texture2D normTex, Texture2D maskTex)
        {
            Material mat;
            if (materialOverride != null)
            {
                mat = new Material(materialOverride);
            }
            else
            {
                Shader sh = Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(sh);
            }
            SetTex(mat, baseTex, "_BaseColorMap", "_BaseMap", "_MainTex");
            SetTex(mat, normTex, "_NormalMap", "_BumpMap");
            SetTex(mat, maskTex, "_MaskMap");
            if (materialOverride == null)
            {
                SetFloat(mat, "_NormalScale", 1f);
                SetFloat(mat, "_NormalMapSpace", 0f);
                SetFloat(mat, "_MetallicRemapMin", 0f); SetFloat(mat, "_MetallicRemapMax", 0f);
                SetFloat(mat, "_SmoothnessRemapMin", 0f); SetFloat(mat, "_SmoothnessRemapMax", 1f);
                SetFloat(mat, "_AORemapMin", 0f); SetFloat(mat, "_AORemapMax", 1f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            }
            mat.EnableKeyword("_NORMALMAP");
            mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            mat.EnableKeyword("_MASKMAP");
            ResetHdrpKeywords(mat);
            mat.enableInstancing = true;
            return mat;
        }

        internal static void ResetHdrpKeywords(Material mat)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType("UnityEditor.Rendering.HighDefinition.HDShaderUtils", false);
                if (t == null) continue;
                var mi = t.GetMethod("ResetMaterialKeywords", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
                    null, new[] { typeof(Material) }, null);
                try { mi?.Invoke(null, new object[] { mat }); }
                catch (Exception e) { Debug.LogWarning("Rock atlas: HDRP keyword reset failed: " + e.Message); }
                return;
            }
        }

        // ------------------------------------------------------------------------------------------------

        private static Tile[] Layout(List<int> srcSizes, int size, int padding)
        {
            // Largest common scale whose tiles shelf-pack into the atlas.
            float lo = 0f, hi = 1f;
            Tile[] best = null;
            Tile[] full = TryPack(srcSizes, 1f, size, padding);
            if (full != null) return full;
            for (int it = 0; it < 24; it++)
            {
                float mid = (lo + hi) * 0.5f;
                Tile[] r = TryPack(srcSizes, mid, size, padding);
                if (r != null) { best = r; lo = mid; } else hi = mid;
            }
            return best;
        }

        private static Tile[] TryPack(List<int> srcSizes, float scale, int size, int padding)
        {
            var order = Enumerable.Range(0, srcSizes.Count).OrderByDescending(i => srcSizes[i]).ToList();
            var tiles = new Tile[srcSizes.Count];
            int x = 0, y = 0, shelf = 0;
            foreach (int i in order)
            {
                int t = Mathf.Max(16, Mathf.FloorToInt(srcSizes[i] * scale / 4f) * 4);
                int foot = t + padding * 2;
                if (foot > size) return null;
                if (x + foot > size) { x = 0; y += shelf; shelf = 0; }
                if (y + foot > size) return null;
                tiles[i] = new Tile { x = x + padding, y = y + padding, size = t };
                x += foot;
                shelf = Mathf.Max(shelf, foot);
            }
            return tiles;
        }

        internal static Texture GetTex(Material m, params string[] names)
        {
            foreach (string n in names)
                if (m.HasProperty(n) && m.GetTexture(n) != null)
                    return m.GetTexture(n);
            return null;
        }

        /// <summary>Reads <paramref name="src"/> scaled to t x t through RenderTextures (box-ish downsample in halving steps).</summary>
        internal static Color32[] ReadTile(Texture src, int t, bool srgb, Color32 fallback)
        {
            var result = new Color32[t * t];
            if (src == null)
            {
                Fill(result, fallback);
                return result;
            }
            RenderTexture prevActive = RenderTexture.active;
            var temps = new List<RenderTexture>();
            try
            {
                Texture cur = src;
                int w = src.width, h = src.height;
                while (w > t * 2 || h > t * 2)
                {
                    w = Mathf.Max(t, w / 2); h = Mathf.Max(t, h / 2);
                    RenderTexture rt = Temp(w, h, srgb);
                    temps.Add(rt);
                    Graphics.Blit(cur, rt);
                    cur = rt;
                }
                RenderTexture fin = Temp(t, t, srgb);
                temps.Add(fin);
                Graphics.Blit(cur, fin);
                RenderTexture.active = fin;
                var tex = new Texture2D(t, t, TextureFormat.RGBA32, false, !srgb);
                tex.ReadPixels(new Rect(0, 0, t, t), 0, 0, false);
                tex.Apply(false);
                result = tex.GetPixels32();
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = prevActive;
                foreach (RenderTexture rt in temps) RenderTexture.ReleaseTemporary(rt);
            }
            return result;
        }

        private static RenderTexture Temp(int w, int h, bool srgb)
        {
            var desc = new RenderTextureDescriptor(w, h, RenderTextureFormat.ARGB32, 0) { sRGB = srgb, useMipMap = false, msaaSamples = 1 };
            RenderTexture rt = RenderTexture.GetTemporary(desc);
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            return rt;
        }

        /// <summary>Raw normal-map texels (DXT5nm: x in A, BC5: x in R, plain RGB) -> tangent-space RGB normals.</summary>
        internal static void DecodeNormals(Color32[] px)
        {
            for (int i = 0; i < px.Length; i++)
            {
                Color32 c = px[i];
                float x = (c.r / 255f) * (c.a / 255f) * 2f - 1f;
                float y = c.g / 255f * 2f - 1f;
                float z = Mathf.Sqrt(Mathf.Max(0f, 1f - x * x - y * y));
                px[i] = new Color32(ToByte(x * 0.5f + 0.5f), ToByte(y * 0.5f + 0.5f), ToByte(z * 0.5f + 0.5f), 255);
            }
        }

        private static byte ToByte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

        /// <summary>Copies the tile and clamps its edge texels out into the padding (dilation against seams / mip bleed).</summary>
        private static void Blit(Color32[] dst, int size, Color32[] tile, Tile t, int padding)
        {
            int x0 = t.x - padding, y0 = t.y - padding, x1 = t.x + t.size + padding, y1 = t.y + t.size + padding;
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(size, y1); y++)
            {
                int sy = Mathf.Clamp(y - t.y, 0, t.size - 1);
                int row = y * size, srow = sy * t.size;
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(size, x1); x++)
                {
                    int sx = Mathf.Clamp(x - t.x, 0, t.size - 1);
                    dst[row + x] = tile[srow + sx];
                }
            }
        }

        private static void Fill(Color32[] a, Color32 c)
        {
            for (int i = 0; i < a.Length; i++) a[i] = c;
        }

        private static void WritePng(string assetPath, Color32[] px, int size, bool linear)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            tex.SetPixels32(px); // CPU only: no GPU upload of the 8K buffer before encoding
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(Path.GetFullPath(assetPath), png);
        }

        private static void SetTex(Material m, Texture t, params string[] names)
        {
            foreach (string n in names)
                if (m.HasProperty(n)) { m.SetTexture(n, t); return; }
        }

        private static void SetFloat(Material m, string n, float v)
        {
            if (m.HasProperty(n)) m.SetFloat(n, v);
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }

    /// <summary>Import settings for generated atlas textures (applied on first import, kept on reimport).</summary>
    internal sealed class DmRockAtlasTexturePostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(DmRockAtlasBuilder.AtlasRoot + "/", StringComparison.Ordinal))
                return;
            var ti = (TextureImporter)assetImporter;
            string name = Path.GetFileNameWithoutExtension(assetPath);
            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            ti.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(w, h)), 32, 16384);
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 4;
            ti.isReadable = false;
            if (name.EndsWith(DmRockAtlasBuilder.NormalSuffix, StringComparison.Ordinal))
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            else if (name.EndsWith(DmRockAtlasBuilder.MaskSuffix, StringComparison.Ordinal))
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = false;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = false;
                ti.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            else if (name.EndsWith(DmRockAtlasBuilder.BaseSuffix, StringComparison.Ordinal))
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
                ti.alphaSource = TextureImporterAlphaSource.None;
                ti.textureCompression = TextureImporterCompression.Compressed;
            }
        }
    }
}
#endif
