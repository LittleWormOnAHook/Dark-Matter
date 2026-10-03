#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Builds (or reuses) the Texture2DArray set for a kit's material set: BaseColor (sRGB, BC1), Normal (linear, BC5) and
    /// Mask (linear, BC7; half size by default), full mip chains, one slice per material at native resolution, so a baked
    /// rock keeps every material's own UVs (tiling baked into UV0, slice + group in UV3). Auto slice size reads each
    /// material's actual source texture size: up to 2K sources go to group A, 4K sources to a separate 4K group B (never
    /// upscaled). Per-material tint, normal strength and mask remaps are baked into the slices. Cached by
    /// hash(material ids + slice mode + mask mode) under Generated/TextureArrays/&lt;key&gt;/ and shared by every rock of
    /// the kit. Source textures are read through RenderTexture blits (import settings are never touched).
    /// </summary>
    public static class DmRockArrayBuilder
    {
        public const string ArrayRoot = "Assets/Genesis PCG Rock Creation/Generated/TextureArrays";
        public const int MaxSlice = 4096;

        public static float LastBuildSeconds { get; private set; }

        public static string MakeKey(IEnumerable<Material> materials, DmRockArraySliceSize mode, bool halfMask, Material materialOverride)
        {
            var ids = materials.Where(m => m != null).Select(DmRockAtlasBuilder.MaterialId).Distinct().OrderBy(s => s, StringComparer.Ordinal);
            string text = string.Join("|", ids) + "#array#" + (int)mode + "#" + (halfMask ? 1 : 0) + "#" + DmRockAtlasBuilder.MaterialId(materialOverride);
            return "A" + Hash128.Compute(text).ToString().Substring(0, 11);
        }

        public static DmRockAtlas Find(string key)
        {
            var a = AssetDatabase.LoadAssetAtPath<DmRockAtlas>($"{ArrayRoot}/{key}/{key}_Arrays.asset");
            return a != null && a.IsComplete ? a : null;
        }

        public static DmRockAtlas GetOrCreate(IList<Material> materials, DmRockArraySliceSize mode, bool halfMask, out bool created)
        {
            created = false;
            var mats = materials.Where(m => m != null).Distinct().OrderBy(DmRockAtlasBuilder.MaterialId, StringComparer.Ordinal).ToList();
            if (mats.Count == 0) return null;
            string key = MakeKey(mats, mode, halfMask, null);
            DmRockAtlas existing = Find(key);
            if (existing != null) return existing;
            created = true;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            DmRockAtlas built = Build(key, mats, mode, halfMask);
            LastBuildSeconds = (float)sw.Elapsed.TotalSeconds;
            return built;
        }

        /// <summary>Actual source size (largest of the base / normal map source files; importer max size ignored).</summary>
        public static int SourceSize(Material m, out int imported)
        {
            int src = 0; imported = 0;
            foreach (string p in new[] { "_BaseColorMap", "_MainTex", "_NormalMap", "_BumpMap" })
            {
                if (!m.HasProperty(p) || !(m.GetTexture(p) is Texture t)) continue;
                imported = Mathf.Max(imported, Mathf.Max(t.width, t.height));
                int w = t.width, h = t.height;
                if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)) is TextureImporter ti) ti.GetSourceTextureWidthAndHeight(out w, out h);
                src = Mathf.Max(src, Mathf.Max(w, h));
            }
            return src > 0 ? src : 1024;
        }

        private static int Pow2(int s) => Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(256, s)), 256, MaxSlice);

        /// <summary>Group (0 = A, 1 = B) per material and the slice size of each group.</summary>
        public static int[] Plan(List<Material> mats, DmRockArraySliceSize mode, out int sizeA, out int sizeB, out string note)
        {
            var grp = new int[mats.Count];
            var src = mats.Select(m => Pow2(SourceSize(m, out _))).ToArray();
            sizeB = 0;
            if (mode != DmRockArraySliceSize.Auto)
            {
                sizeA = (int)mode;
                note = $"{(int)mode} (forced, {mats.Count} materials{(src.Any(s => s < (int)mode) ? "; smaller sources upscaled" : "")}{(src.Any(s => s > (int)mode) ? "; larger sources downscaled" : "")})";
                return grp;
            }
            int a = 0, nB = 0;
            for (int i = 0; i < mats.Count; i++)
            {
                if (src[i] >= MaxSlice) { grp[i] = 1; nB++; }
                else a = Mathf.Max(a, src[i]);
            }
            if (nB == mats.Count) { for (int i = 0; i < grp.Length; i++) grp[i] = 0; sizeA = MaxSlice; note = $"Auto: {MaxSlice} ({mats.Count} materials)"; return grp; }
            sizeA = a;
            if (nB > 0) sizeB = MaxSlice;
            note = $"Auto: {sizeA} ({mats.Count - nB} materials)" + (nB > 0 ? $" + {MaxSlice} ({nB} materials)" : "");
            return grp;
        }

        // ------------------------------------------------------------------------------------------------

        private static string Signature(Material m)
        {
            Texture b = DmRockAtlasBuilder.GetTex(m, "_BaseColorMap", "_MainTex"), n = DmRockAtlasBuilder.GetTex(m, "_NormalMap", "_BumpMap"), k = DmRockAtlasBuilder.GetTex(m, "_MaskMap");
            string F(string p) => m.HasProperty(p) ? m.GetFloat(p).ToString("F3") : "-";
            Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
            return $"{(b ? b.GetEntityId().ToString() : "0")}|{(n ? n.GetEntityId().ToString() : "0")}|{(k ? k.GetEntityId().ToString() : "0")}|{c}|{F("_NormalScale")}|{F("_Metallic")}|{F("_Smoothness")}|" +
                   $"{F("_MetallicRemapMin")}|{F("_MetallicRemapMax")}|{F("_SmoothnessRemapMin")}|{F("_SmoothnessRemapMax")}|{F("_AORemapMin")}|{F("_AORemapMax")}";
        }

        private static DmRockAtlas Build(string key, List<Material> mats, DmRockArraySliceSize mode, bool halfMask)
        {
            string folder = $"{ArrayRoot}/{key}";
            DmRockAtlasBuilder.EnsureFolder(folder);
            int[] grp = Plan(mats, mode, out int sizeA, out int sizeB, out string note);
            int groups = sizeB > 0 ? 2 : 1;
            var sizes = new[] { sizeA, sizeB };
            // unique slices per group (materials with identical maps + parameters share one)
            var sliceOf = new int[mats.Count];
            var uniq = new List<int>[2] { new List<int>(), new List<int>() };
            var sigs = new Dictionary<string, int>[2] { new Dictionary<string, int>(), new Dictionary<string, int>() };
            for (int i = 0; i < mats.Count; i++)
            {
                string sig = Signature(mats[i]);
                if (!sigs[grp[i]].TryGetValue(sig, out int sl)) { sl = uniq[grp[i]].Count; uniq[grp[i]].Add(i); sigs[grp[i]][sig] = sl; }
                sliceOf[i] = sl;
            }

            var atlas = ScriptableObject.CreateInstance<DmRockAtlas>();
            atlas.key = key;
            atlas.isArray = true;
            atlas.size = sizeA;
            atlas.padding = 0;
            atlas.groupSize = new int[groups];
            atlas.groupMaskSize = new int[groups];
            atlas.baseArrays = new Texture2DArray[groups];
            atlas.normalArrays = new Texture2DArray[groups];
            atlas.maskArrays = new Texture2DArray[groups];
            double lumSum = 0; int lumN = 0; long bytes = 0;
            for (int g = 0; g < groups; g++)
            {
                int S = sizes[g], MS = halfMask ? Mathf.Max(256, S / 2) : S;
                atlas.groupSize[g] = S;
                atlas.groupMaskSize[g] = MS;
                string gn = g == 0 ? "A" : "B";
                List<int> list = uniq[g];
                var baseArr = new Texture2DArray(S, S, list.Count, TextureFormat.DXT1, true, false);
                var normArr = new Texture2DArray(S, S, list.Count, TextureFormat.BC5, true, true);
                var maskArr = new Texture2DArray(MS, MS, list.Count, TextureFormat.BC7, true, true);
                for (int k = 0; k < list.Count; k++)
                {
                    Material m = mats[list[k]];
                    EditorUtility.DisplayProgressBar("Rock texture arrays", $"{m.name} ({k + 1}/{list.Count}, {S}px)", (k + 0.5f) / list.Count);
                    Color32[] a = BasePixels(m, S, out double lum);
                    lumSum += lum; lumN++;
                    CopySlice(baseArr, k, a, S, TextureFormat.DXT1, false);
                    CopySlice(normArr, k, NormalPixels(m, S), S, TextureFormat.BC5, true);
                    CopySlice(maskArr, k, MaskPixels(m, MS), MS, TextureFormat.BC7, true);
                }
                EditorUtility.ClearProgressBar();
                foreach (Texture2DArray arr in new[] { baseArr, normArr, maskArr })
                {
                    arr.filterMode = FilterMode.Trilinear;
                    arr.wrapMode = TextureWrapMode.Repeat;
                    arr.anisoLevel = 4;
                    arr.Apply(false, false);
                }
                baseArr.name = $"{key}_Base{gn}"; normArr.name = $"{key}_Normal{gn}"; maskArr.name = $"{key}_Mask{gn}";
                atlas.baseArrays[g] = SaveArray(baseArr, $"{folder}/{baseArr.name}.asset");
                atlas.normalArrays[g] = SaveArray(normArr, $"{folder}/{normArr.name}.asset");
                atlas.maskArrays[g] = SaveArray(maskArr, $"{folder}/{maskArr.name}.asset");
                bytes += ArrayBytes(S, list.Count, 0.5) + ArrayBytes(S, list.Count, 1.0) + ArrayBytes(MS, list.Count, 1.0);
            }
            atlas.averageLuminance = lumN > 0 ? (float)(lumSum / lumN) : 0.1f;
            atlas.vramMB = bytes / (1024f * 1024f);
            atlas.sizeNote = note + (halfMask ? ", mask half size" : "") + $", {atlas.vramMB:F0} MB";
            atlas.entries = new List<DmRockAtlas.Entry>();
            for (int i = 0; i < mats.Count; i++)
                atlas.entries.Add(new DmRockAtlas.Entry { source = mats[i], sourceId = DmRockAtlasBuilder.MaterialId(mats[i]), group = grp[i], slice = sliceOf[i], uvRect = new Rect(0, 0, 1, 1) });

            Material mat = CreateMaterial(atlas);
            mat.name = key + "_Array";
            AssetDatabase.CreateAsset(mat, $"{folder}/{key}_Material.mat");
            atlas.material = mat;
            AssetDatabase.CreateAsset(atlas, $"{folder}/{key}_Arrays.asset");
            AssetDatabase.SaveAssetIfDirty(mat);
            AssetDatabase.SaveAssetIfDirty(atlas);
            Debug.Log($"Genesis PCG Rock Creation: texture arrays {key}: {atlas.sizeNote}; {mats.Count} materials -> {uniq[0].Count + uniq[1].Count} slices.");
            return atlas;
        }

        /// <summary>Compressed size of one array with full mips (bytes per texel: BC1 0.5, BC5 / BC7 1).</summary>
        public static long ArrayBytes(int size, int slices, double bpp)
        {
            double t = 0; for (int s = size; s >= 1; s /= 2) t += Math.Max(4, s) * (double)Math.Max(4, s);
            return (long)(t * bpp * slices);
        }

        private static Texture2DArray SaveArray(Texture2DArray arr, string path)
        {
            AssetDatabase.CreateAsset(arr, path);
            // keep the pixel data in the asset but do not keep a CPU copy at runtime
            using (var so = new SerializedObject(arr))
            {
                SerializedProperty r = so.FindProperty("m_IsReadable");
                if (r != null) { r.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
            }
            AssetDatabase.SaveAssetIfDirty(arr);
            return AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
        }

        private static void CopySlice(Texture2DArray arr, int slice, Color32[] px, int size, TextureFormat fmt, bool linear)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true, linear);
            try
            {
                t.SetPixels32(px);
                t.Apply(true, false);
                EditorUtility.CompressTexture(t, fmt, TextureCompressionQuality.Normal);
                for (int mip = 0; mip < t.mipmapCount && mip < arr.mipmapCount; mip++)
                    arr.SetPixelData(t.GetPixelData<byte>(mip), mip, slice);
            }
            finally { Object.DestroyImmediate(t); }
        }

        private static Color32[] BasePixels(Material m, int S, out double avgLum)
        {
            Color tint = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : (m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white);
            Color32[] a = DmRockAtlasBuilder.ReadTile(DmRockAtlasBuilder.GetTex(m, "_BaseColorMap", "_MainTex"), S, true, new Color32(255, 255, 255, 255));
            Color lin = tint.linear;
            bool tinted = tint != Color.white;
            double sum = 0; int cnt = 0;
            for (int p = 0; p < a.Length; p++)
            {
                if (tinted)
                {
                    Color c = ((Color)a[p]).linear;
                    a[p] = (Color32)new Color(c.r * lin.r, c.g * lin.g, c.b * lin.b, 1f).gamma;
                }
                a[p].a = 255;
                if ((p & 15) == 0)
                {
                    Color l = ((Color)a[p]).linear;
                    sum += 0.2126 * l.r + 0.7152 * l.g + 0.0722 * l.b; cnt++;
                }
            }
            avgLum = cnt > 0 ? sum / cnt : 0.1;
            return a;
        }

        private static Color32[] NormalPixels(Material m, int S)
        {
            Color32[] n = DmRockAtlasBuilder.ReadTile(DmRockAtlasBuilder.GetTex(m, "_NormalMap", "_BumpMap"), S, false, new Color32(128, 128, 255, 255));
            DmRockAtlasBuilder.DecodeNormals(n);
            float scale = m.HasProperty("_NormalScale") ? m.GetFloat("_NormalScale") : 1f;
            for (int i = 0; i < n.Length; i++)
            {
                float x = n[i].r / 255f * 2f - 1f, y = n[i].g / 255f * 2f - 1f;
                if (Mathf.Abs(scale - 1f) > 0.01f)
                {
                    x *= scale; y *= scale;
                    float l2 = x * x + y * y;
                    if (l2 > 1f) { float l = Mathf.Sqrt(l2); x /= l; y /= l; }
                }
                // BC5 keeps R/G only; the shader rebuilds Z (UnpackNormalMapRGorAG)
                n[i] = new Color32(ToByte(x * 0.5f + 0.5f), ToByte(y * 0.5f + 0.5f), 0, 255);
            }
            return n;
        }

        private static Color32[] MaskPixels(Material m, int MS)
        {
            float F(string p, float d) => m.HasProperty(p) ? m.GetFloat(p) : d;
            Texture mt = DmRockAtlasBuilder.GetTex(m, "_MaskMap");
            if (mt == null)
            {
                // HDRP Lit without a mask map: metallic / smoothness sliders, AO 1
                var c = new Color32(ToByte(F("_Metallic", 0f)), 255, 255, ToByte(F("_Smoothness", 0.5f)));
                var flat = new Color32[MS * MS];
                for (int i = 0; i < flat.Length; i++) flat[i] = c;
                return flat;
            }
            Color32[] k = DmRockAtlasBuilder.ReadTile(mt, MS, false, new Color32(0, 255, 255, 128));
            float m0 = F("_MetallicRemapMin", 0f), m1 = F("_MetallicRemapMax", 1f);
            float a0 = F("_AORemapMin", 0f), a1 = F("_AORemapMax", 1f);
            float s0 = F("_SmoothnessRemapMin", 0f), s1 = F("_SmoothnessRemapMax", 1f);
            for (int i = 0; i < k.Length; i++)
            {
                Color32 c = k[i];
                k[i] = new Color32(ToByte(Mathf.Lerp(m0, m1, c.r / 255f)), ToByte(Mathf.Lerp(a0, a1, c.g / 255f)), c.b, ToByte(Mathf.Lerp(s0, s1, c.a / 255f)));
            }
            return k;
        }

        private static byte ToByte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

        /// <summary>Rock Blend Lit with every layer off, sampling the arrays: the plain (pack look) material of array bakes.</summary>
        internal static Material CreateMaterial(DmRockAtlas a)
        {
            Shader sh = Shader.Find(DmRockBlendMaterials.ShaderName);
            if (sh == null) throw new InvalidOperationException("Rock texture arrays need the shader '" + DmRockBlendMaterials.ShaderName + "'.");
            var mat = new Material(sh);
            ConfigureArrayMaterial(mat, a);
            mat.SetFloat("_GRB_TopEnable", 0f);
            mat.SetFloat("_GRB_TopCoverage", 0f);
            mat.SetFloat("_GRB_BottomEnable", 0f);
            mat.SetFloat("_GRB_StrataEnable", 0f);
            mat.SetFloat("_GRB_MacroStrength", 0f);
            mat.SetFloat("_GRB_FadeEnable", 0f);
            mat.SetFloat("_GRB_TerrainAuto", 0f);
            mat.SetFloat("_GRB_TerrainPerPixel", 0f);
            mat.SetFloat("_GRB_ContactAOKeep", 1f);
            mat.SetFloat("_Smoothness", 0.5f);
            mat.SetFloat("_NormalScale", 1f);
            mat.SetFloat("_MetallicRemapMin", 0f); mat.SetFloat("_MetallicRemapMax", 1f);
            mat.SetFloat("_SmoothnessRemapMin", 0f); mat.SetFloat("_SmoothnessRemapMax", 1f);
            mat.SetFloat("_AORemapMin", 0f); mat.SetFloat("_AORemapMax", 1f);
            mat.SetColor("_BaseColor", Color.white);
            mat.enableInstancing = true;
            GenesisRockBlendGUI.Validate(mat);
            return mat;
        }

        /// <summary>Points a Rock Blend material at the arrays (group B falls back to group A when unused).</summary>
        public static void ConfigureArrayMaterial(Material mat, DmRockAtlas a)
        {
            mat.SetFloat("_GRB_UseTexArray", 1f);
            mat.SetTexture("_BaseColorMap", null);
            mat.SetTexture("_NormalMap", null);
            mat.SetTexture("_MaskMap", null);
            int b = a.baseArrays.Length > 1 ? 1 : 0;
            mat.SetTexture("_GRB_BaseArray", a.baseArrays[0]);
            mat.SetTexture("_GRB_NormalArray", a.normalArrays[0]);
            mat.SetTexture("_GRB_MaskArray", a.maskArrays[0]);
            mat.SetTexture("_GRB_BaseArrayB", a.baseArrays[b]);
            mat.SetTexture("_GRB_NormalArrayB", a.normalArrays[b]);
            mat.SetTexture("_GRB_MaskArrayB", a.maskArrays[b]);
            mat.SetFloat("_GRB_BaseLumRef", Mathf.Max(0.005f, a.averageLuminance));
            mat.DisableKeyword("_NORMALMAP");
            mat.DisableKeyword("_NORMALMAP_TANGENT_SPACE");
            mat.DisableKeyword("_MASKMAP");
        }

        /// <summary>Generated array sets (folders) no bake in the open scenes references.</summary>
        public static IEnumerable<string> Folders()
        {
            if (!AssetDatabase.IsValidFolder(ArrayRoot)) yield break;
            foreach (string f in AssetDatabase.GetSubFolders(ArrayRoot)) yield return f;
        }
    }
}
#endif
