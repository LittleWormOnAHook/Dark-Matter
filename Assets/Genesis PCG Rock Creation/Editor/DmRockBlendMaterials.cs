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
    /// Rock Blend materials: layer sets, the template materials (Data/Materials/Blend) and the per-rock bake variants
    /// (Generated/Materials/Blend): template settings + the rock's base maps (atlas or override material) + the terrain
    /// layer under the rock.
    /// </summary>
    public static partial class DmRockBlendMaterials
    {
        public const string ShaderName = "Genesis PCG/Rock Blend Lit";
        /// <summary>Same material, forward-only passes (lets Contact AO Keep fade SSAO on the rock).</summary>
        public const string ForwardShaderName = "Genesis PCG/Rock Blend Lit (Forward Only)";
        public const string Root = "Assets/Genesis PCG Rock Creation";
        public const string TemplateFolder = Root + "/Data/Materials/Blend";
        public const string VariantFolder = Root + "/Generated/Materials/Blend";
        /// <summary>Per-rock blend materials (one per rock, named MB_&lt;rock&gt;_&lt;placement stamp&gt;): the rock's own editable blend settings.</summary>
        public const string PerRockFolder = VariantFolder + "/PerRock";
        public const string DefaultTemplateName = "M_RockBlend_Default";
        private const string SourceTag = "GRB_SourceTemplate";

        public sealed class LayerSet
        {
            public string name, albedo, normal, height, mask, ao;
            public float tiling, smoothness, normalScale;
            public Color tint;
        }

        private const string AM = "Assets/ANGRY MESH/Nature Pack - PBR Rocks/Ground Textures/";
        private const string IO = "Assets/_Project/Art/Terrain/Io_Moon_Surface/";

        /// <summary>Top layer sets from textures already in the project (missing files are simply skipped).</summary>
        public static readonly LayerSet[] LayerSets =
        {
            new LayerSet { name = "Moss", albedo = AM + "Moss_01_Albedo.tif", normal = AM + "Moss_01_NM.tif", height = AM + "Moss_01_Height.tif", mask = AM + "Moss_01_Mask.tif", ao = AM + "Moss_01_AO.tif", tiling = 0.5f, smoothness = 0.15f, normalScale = 1f, tint = new Color(0.9f, 1f, 0.85f) },
            new LayerSet { name = "Sand", albedo = AM + "Sand_01_Albedo.tif", normal = AM + "Sand_01_NM.tif", height = AM + "Sand_01_Height.tif", mask = AM + "Sand_01_Mask.tif", ao = AM + "Sand_01_AO.tif", tiling = 0.4f, smoothness = 0.2f, normalScale = 0.8f, tint = Color.white },
            new LayerSet { name = "Snow", albedo = AM + "Snow_01_Albedo.tif", normal = AM + "Snow_01_NM.tif", height = AM + "Snow_01_Height.tif", mask = AM + "Snow_01_Mask.tif", ao = AM + "Snow_01_AO.tif", tiling = 0.35f, smoothness = 0.45f, normalScale = 0.6f, tint = new Color(0.97f, 0.98f, 1f) },
            new LayerSet { name = "Sulfur (Io)", albedo = IO + "Set02_Sulfur_Plains/Io_Set02_Color_Periodic_2048.png", normal = IO + "Set01_Basalt_Sulfur_Dust/Io_Set01_Normal_Periodic_2048.png", height = null, mask = IO + "Set02_Sulfur_Plains/Io_Set02_Mask_Periodic_2048.png", tiling = 0.35f, smoothness = 0.25f, normalScale = 0.6f, tint = new Color(1f, 0.95f, 0.75f) },
            new LayerSet { name = "Dust (Io ash)", albedo = IO + "Set05_Ash_Regolith/Io_Set05_Color_Periodic_2048.png", normal = IO + "Set01_Basalt_Sulfur_Dust/Io_Set01_Normal_Periodic_2048.png", height = null, mask = IO + "Set05_Ash_Regolith/Io_Set05_Mask_Periodic_2048.png", tiling = 0.35f, smoothness = 0.1f, normalScale = 0.5f, tint = Color.white },
        };

        public static string[] LayerSetNames() => LayerSets.Select(s => s.name).ToArray();

        public static void ApplyLayerSet(Material m, string setName)
        {
            LayerSet s = LayerSets.FirstOrDefault(x => x.name == setName);
            if (m == null || s == null) return;
            m.SetTexture("_GRB_TopBaseMap", Load<Texture2D>(s.albedo));
            m.SetTexture("_GRB_TopNormalMap", Load<Texture2D>(s.normal));
            m.SetTexture("_GRB_TopHeightMap", Load<Texture2D>(s.height));
            m.SetFloat("_GRB_TopTiling", s.tiling);
            m.SetFloat("_GRB_TopSmoothness", s.smoothness);
            m.SetFloat("_GRB_TopNormalScale", s.normalScale);
            m.SetColor("_GRB_TopTint", s.tint);
            // Mask / AO slots: filled when the set has the maps, cleared otherwise (no stale maps from the previous set).
            if (m.HasProperty("_GRB_TopMaskMap"))
            {
                m.SetTexture("_GRB_TopMaskMap", Load<Texture2D>(s.mask));
                m.SetFloat("_GRB_TopMetallicMin", 0f);
                m.SetFloat("_GRB_TopMetallicMax", 0f); // deposits are dielectric (the packs' mask R is 0 anyway)
                m.SetFloat("_GRB_TopSmoothMin", 0f);
                m.SetFloat("_GRB_TopSmoothMax", 1f);
            }
            if (m.HasProperty("_GRB_TopAOMap")) m.SetTexture("_GRB_TopAOMap", Load<Texture2D>(s.ao));
            GenesisRockBlendGUI.SyncTopMaps(m);
        }

        private static T Load<T>(string path) where T : Object => string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);

        public static bool IsBlendMaterial(Material m) => m != null && m.shader != null && (m.shader.name == ShaderName || m.shader.name == ForwardShaderName);

        public static List<Material> Templates()
        {
            var list = new List<Material>();
            if (!AssetDatabase.IsValidFolder(TemplateFolder)) return list;
            foreach (string g in AssetDatabase.FindAssets("t:Material", new[] { TemplateFolder }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                if (IsBlendMaterial(m)) list.Add(m);
            }
            return list.OrderBy(m => m.name).ToList();
        }

        // ------------------------------------------------------------------------------------------------
        // Template materials

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Creates (or refreshes) the five template materials. Existing templates keep user edits unless <paramref name="reset"/>.</summary>
        [MenuItem("Tools/Genesis PCG Rock Creation/Create Rock Blend Materials")]
        public static void CreateTemplatesMenu() => Debug.Log("Genesis PCG Rock Creation: " + CreateTemplates(false));

        /// <summary>Recommended tight ground fade: band ~0.005-0.135 m above the terrain (height 0.1, softness 0.6, noise 0.35 at 6/m).</summary>
        public static void ApplyFadeDefaults(Material m)
        {
            m.SetFloat("_GRB_FadeEnable", 1f);
            m.SetFloat("_GRB_FadeHeight", 0.1f);
            m.SetFloat("_GRB_FadeSoftness", 0.6f);
            m.SetFloat("_GRB_FadeNoiseScale", 6f);
            m.SetFloat("_GRB_FadeNoiseStrength", 0.35f);
        }

        public static string CreateTemplates(bool reset)
        {
            Shader sh = Shader.Find(ShaderName);
            if (sh == null) return "shader '" + ShaderName + "' not found";
            EnsureFolder(TemplateFolder);
            var made = new List<string>();
            void Make(string name, Action<Material> setup)
            {
                string path = TemplateFolder + "/" + name + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool isNew = m == null;
                if (!isNew && !reset) { made.Add(name + " (kept)"); return; }
                if (isNew) m = new Material(sh) { name = name };
                else if (!IsBlendMaterial(m)) { m.shader = sh; } // keep a forward-only choice
                m.SetFloat("_Smoothness", 0.5f);
                setup(m);
                GenesisRockBlendGUI.Validate(m);
                if (isNew) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
                made.Add(name);
            }

            void Base(Material m, float bottomH)
            {
                m.SetFloat("_GRB_BottomEnable", 1f);
                m.SetFloat("_GRB_BottomHeight", bottomH);
                m.SetFloat("_GRB_BottomFalloff", 0.08f);
                m.SetFloat("_GRB_BottomSharpness", 3f);
                m.SetFloat("_GRB_BottomEdgeNoise", 0.05f);
                m.SetFloat("_GRB_BottomPieceScale", 0.25f);
                m.SetFloat("_GRB_BottomMaxWeight", 0.9f);
                // Ground fade (on by default; the dither needs TAA to resolve, which the game cameras use) + contact AO.
                // Measured per pixel from the terrain height: a tight band of ~0-0.13 m (FadeDefaults) reads as "in the ground".
                ApplyFadeDefaults(m);
                m.SetFloat("_GRB_FadeTerrainColor", 1f);
                m.SetFloat("_GRB_ContactAOKeep", 0.3f);
                m.SetFloat("_GRB_ContactAOWidth", 2f);
            }
            void Strata(Material m, Color a, Color b, float strength, float spacing)
            {
                m.SetFloat("_GRB_StrataEnable", 1f);
                m.SetColor("_GRB_StrataColorA", a);
                m.SetColor("_GRB_StrataColorB", b);
                m.SetFloat("_GRB_StrataStrength", strength);
                m.SetFloat("_GRB_StrataSpacing", spacing);
                m.SetFloat("_GRB_StrataThickVar", 0.6f);
                m.SetFloat("_GRB_StrataDetail", 0.7f);
                m.SetFloat("_GRB_StrataLedge", 0.6f);
                m.SetFloat("_GRB_StrataLineDarken", 0.45f);
                m.SetFloat("_GRB_StrataLineWidth", 0.035f);
            }

            Make("M_RockBlend_Default", m =>
            {
                m.SetFloat("_GRB_TopEnable", 0f);
                m.SetFloat("_GRB_TopCoverage", 0f);
                Base(m, 0.2f);
                m.SetFloat("_GRB_StrataEnable", 0f);
                m.SetFloat("_GRB_MacroStrength", 0.2f);
            });
            Make("M_RockBlend_Io", m =>
            {
                ApplyLayerSet(m, "Sulfur (Io)");
                m.SetFloat("_GRB_TopEnable", 1f);
                m.SetColor("_GRB_TopTint", new Color(1f, 0.95f, 0.78f));
                m.SetColor("_GRB_TopTint2", new Color(0.92f, 0.84f, 0.66f));
                m.SetFloat("_GRB_TopSaturation", 0.7f);
                m.SetFloat("_GRB_TopValueVar", 0.2f);
                m.SetFloat("_GRB_TopCoverage", 0.42f);
                m.SetFloat("_GRB_TopSharpness", 6f);
                m.SetFloat("_GRB_TopNoiseStrength", 0.45f);
                m.SetFloat("_GRB_TopSlopeStart", 0.4f);
                m.SetFloat("_GRB_TopSlopeEnd", 0.8f);
                m.SetFloat("_GRB_TopCreviceBias", 0.8f);
                m.SetFloat("_GRB_TopMinPieceSize", 0.2f);
                Base(m, 0.25f);
                Strata(m, new Color(0.10f, 0.09f, 0.08f), new Color(0.30f, 0.24f, 0.17f), 0.55f, 0.45f);
                m.SetFloat("_GRB_MacroStrength", 0.3f);
            });
            Make("M_RockBlend_Moss", m =>
            {
                ApplyLayerSet(m, "Moss");
                m.SetFloat("_GRB_TopEnable", 1f);
                m.SetColor("_GRB_TopTint", new Color(0.82f, 0.9f, 0.6f));
                m.SetColor("_GRB_TopTint2", new Color(0.78f, 0.7f, 0.46f));
                m.SetFloat("_GRB_TopSaturation", 0.7f);
                m.SetFloat("_GRB_TopValueVar", 0.3f);
                m.SetFloat("_GRB_TopVarScale", 0.8f);
                m.SetFloat("_GRB_TopCoverage", 0.62f);
                m.SetFloat("_GRB_TopSharpness", 7f);
                m.SetFloat("_GRB_TopSlopeStart", 0.3f);
                m.SetFloat("_GRB_TopSlopeEnd", 0.75f);
                m.SetFloat("_GRB_TopCreviceBias", 0.8f);
                m.SetFloat("_GRB_TopMinPieceSize", 0.5f);
                Base(m, 0.2f);
                m.SetFloat("_GRB_MacroStrength", 0.25f);
            });
            Make("M_RockBlend_Sand", m =>
            {
                ApplyLayerSet(m, "Sand"); // fallback when the rock is not on a terrain
                m.SetFloat("_GRB_TopEnable", 1f);
                m.SetFloat("_GRB_TopFromTerrain", 1f);
                m.SetFloat("_GRB_TopHeightContrast", 0.3f);
                m.SetFloat("_GRB_TopCoverage", 0.4f);
                m.SetFloat("_GRB_TopSharpness", 6f);
                m.SetFloat("_GRB_TopSlopeStart", 0.5f);
                m.SetFloat("_GRB_TopSlopeEnd", 0.85f);
                m.SetFloat("_GRB_TopNoiseStrength", 0.4f);
                m.SetFloat("_GRB_TopMaxWeight", 0.8f);
                m.SetFloat("_GRB_TopBrightness", 0.8f); // deposits a touch darker than the open ground: cap reads against it
                m.SetFloat("_GRB_TopMinPieceSize", 0.3f);
                Base(m, 0.25f);
                Strata(m, new Color(0.20f, 0.15f, 0.12f), new Color(0.52f, 0.41f, 0.30f), 0.8f, 0.7f);
                m.SetFloat("_GRB_StrataThickVar", 0.85f);
                m.SetFloat("_GRB_StrataNoiseAmp", 0.55f);
                m.SetFloat("_GRB_MacroStrength", 0.25f);
            });
            Make("M_RockBlend_Snow", m =>
            {
                ApplyLayerSet(m, "Snow");
                m.SetFloat("_GRB_TopEnable", 1f);
                m.SetColor("_GRB_TopTint", new Color(1f, 0.96f, 0.9f));   // warm/neutral: reads white-ish under an orange sky
                m.SetColor("_GRB_TopTint2", new Color(0.96f, 0.94f, 0.9f));
                m.SetFloat("_GRB_TopSaturation", 0.5f);
                m.SetFloat("_GRB_TopBrightness", 1.05f);
                m.SetFloat("_GRB_TopValueVar", 0.08f);
                m.SetFloat("_GRB_TopCoverage", 0.45f);
                m.SetFloat("_GRB_TopSharpness", 14f);
                m.SetFloat("_GRB_TopSlopeStart", 0.62f);
                m.SetFloat("_GRB_TopSlopeEnd", 0.9f);
                m.SetFloat("_GRB_TopCreviceBias", 1f);
                m.SetFloat("_GRB_TopHeightContrast", 0.8f);
                m.SetFloat("_GRB_TopNoiseStrength", 0.3f);
                m.SetFloat("_GRB_TopVertexWeight", 0.2f);
                m.SetFloat("_GRB_TopMinPieceSize", 0.25f);
                Base(m, 0.15f);
                m.SetFloat("_GRB_MacroStrength", 0.15f);
            });
            RefreshPreviews(null);
            AssetDatabase.SaveAssets();
            return "rock blend templates: " + string.Join(", ", made);
        }

        // ------------------------------------------------------------------------------------------------
        // Bake variants

        private static readonly Dictionary<string, Material> s_variants = new Dictionary<string, Material>();

        private static readonly string[] ArrayProps =
            { "_GRB_BaseArray", "_GRB_NormalArray", "_GRB_MaskArray", "_GRB_BaseArrayB", "_GRB_NormalArrayB", "_GRB_MaskArrayB" };

        private static readonly string[] BaseProps =
        {
            "_BaseColor", "_Metallic", "_Smoothness", "_MetallicRemapMin", "_MetallicRemapMax", "_SmoothnessRemapMin", "_SmoothnessRemapMax",
            "_AORemapMin", "_AORemapMax", "_NormalScale",
        };

        /// <summary>
        /// Bake hook: when the rock has a blend template, every baked material (atlas / override / fallback) becomes a
        /// variant of the template with that material's base maps. Null template = materials unchanged (pack path).
        /// </summary>
        public static Material[] Apply(DmRockCombiner c, Material[] materials, out string note)
        {
            note = null;
            Material template = c != null ? c.BlendMaterial : null;
            if (!IsBlendMaterial(template) || materials == null) return materials;
            TerrainLayerInfo tl = template.GetFloat("_GRB_TerrainAuto") > 0.5f && (c == null || c.MatchTerrain) ? FindTerrainLayer(c) : null;
            var outMats = new Material[materials.Length];
            for (int i = 0; i < materials.Length; i++)
                outMats[i] = materials[i] == null ? null : GetVariant(template, materials[i], tl);
            note = "blend " + template.name + (tl != null ? " + terrain layer " + tl.layer.name + " (" + tl.terrain.name + ")" : " (no terrain layer)");
            return outMats;
        }

        // ------------------------------------------------------------------------------------------------
        // Per-rock blend materials

        public static bool IsPerRock(Material m) => m != null && AssetDatabase.GetAssetPath(m).StartsWith(PerRockFolder + "/", StringComparison.Ordinal);

        private static string StampSuffix(DmRockCombiner c) => "_" + c.PlacementStamp.ToString("x");

        /// <summary>True when the rock's own blend slot holds the per-rock material made for this rock (not a copy's original).</summary>
        public static bool OwnsBlend(DmRockCombiner c)
        {
            Material m = c != null ? c.OwnBlendMaterial : null;
            return m != null && IsPerRock(m) && m.name.EndsWith(StampSuffix(c), StringComparison.Ordinal);
        }

        /// <summary>Template a per-rock blend was cloned from (or the material itself when it is not per-rock).</summary>
        public static Material SourceTemplate(Material m)
        {
            if (m == null || !IsPerRock(m)) return m;
            string n = m.GetTag(SourceTag, false, "");
            return string.IsNullOrEmpty(n) ? null : Templates().FirstOrDefault(t => t.name == n);
        }

        public static Material DefaultTemplate()
        {
            List<Material> t = Templates();
            Material d = t.FirstOrDefault(m => m.name == DefaultTemplateName);
            if (d == null) { CreateTemplates(false); d = Templates().FirstOrDefault(m => m.name == DefaultTemplateName); }
            return d;
        }

        /// <summary>
        /// What the rock needs: its own per-rock blend when terrain match is on and it has none, or when its per-rock blend
        /// belongs to another rock (copy / paste / duplicate copied the reference).
        /// </summary>
        public static bool NeedsOwnBlend(DmRockCombiner c)
        {
            if (c == null || EditorUtility.IsPersistent(c) || !c.gameObject.scene.IsValid() || c.PlacementStamp == 0) return false;
            Material own = c.OwnBlendMaterial;
            if (own != null && IsPerRock(own)) return !OwnsBlend(c);
            return c.MatchTerrain && c.BlendMaterial == null;
        }

        /// <summary>
        /// Gives the rock its own blend material (per-rock asset, never shared) and returns it. Source: the per-rock blend it
        /// was copied from, else its blend template (own slot or preset), else an override material that is itself a Rock
        /// Blend material, else the Default template. Already owned = returned unchanged.
        /// </summary>
        public static Material EnsureOwnBlend(DmRockCombiner c, string undoName = null)
        {
            if (c == null) return null;
            if (OwnsBlend(c)) return c.OwnBlendMaterial;
            Material src = c.BlendMaterial;
            if (src == null && IsBlendMaterial(c.MaterialOverride)) src = c.MaterialOverride;
            if (src == null) src = DefaultTemplate();
            if (src == null) return null;
            Material tmpl = IsPerRock(src) ? SourceTemplate(src) : src;
            EnsureFolder(PerRockFolder);

            // One file per rock: MB_<rock>_<stamp>.mat. A file already at that name that no other loaded rock uses is this
            // rock's own (same placement stamp = same rock: slot cleared by Undo / reset / a template pick) and is reused, so
            // repeated calls never pile up "<name> 1.mat" copies. Only when the name is taken by a different rock does a new
            // file get a counter, placed before the stamp (MB_<rock>_<n>_<stamp>) so the object name = file name and still
            // ends with the stamp (OwnsBlend).
            string prefix = "MB_" + Safe(c.name), suffix = StampSuffix(c);
            Material m = null;
            string baseName = null, path = null;
            for (int n = 0; n < 1000 && m == null; n++)
            {
                baseName = n == 0 ? prefix + suffix : prefix + "_" + n + suffix;
                path = PerRockFolder + "/" + baseName + ".mat";
                if (AssetDatabase.LoadMainAssetAtPath(path) == null) break; // free name: create it below
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing != null && IsBlendMaterial(existing) && !UsedByOtherRock(existing, c))
                    m = existing;
            }
            if (m != null)
            {
                Material chosen = c.OwnBlendMaterial;
                if (chosen == null && IsBlendMaterial(c.MaterialOverride)) chosen = c.MaterialOverride;
                if (chosen != null && chosen != m)
                {
                    // An explicit pick (template / other material) replaces the settings; an empty slot keeps the file's own edits.
                    Undo.RecordObject(m, string.IsNullOrEmpty(undoName) ? "Rock Blend" : undoName);
                    if (m.shader != src.shader) m.shader = src.shader;
                    m.CopyPropertiesFromMaterial(src);
                    m.shaderKeywords = src.shaderKeywords;
                    PrepareOwnBlend(m, c, src, tmpl);
                    EditorUtility.SetDirty(m);
                }
                if (m.name != baseName) { m.name = baseName; EditorUtility.SetDirty(m); }
            }
            else
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null) path = AssetDatabase.GenerateUniqueAssetPath(path); // 1000 rocks share the name: give up on tidy names
                baseName = Path.GetFileNameWithoutExtension(path);
                m = new Material(src) { name = baseName };
                PrepareOwnBlend(m, c, src, tmpl);
                AssetDatabase.CreateAsset(m, path);
            }
            if (!string.IsNullOrEmpty(undoName)) Undo.RecordObject(c, undoName);
            c.SetBlendMaterial(m);
            EditorUtility.SetDirty(c);
            return m;
        }

        private static void PrepareOwnBlend(Material m, DmRockCombiner c, Material src, Material tmpl)
        {
            // texture-array / atlas maps are set per bake; a clone of an override material keeps its own base maps
            if (m.HasProperty("_GRB_UseTexArray") && src != c.MaterialOverride) m.SetFloat("_GRB_UseTexArray", 0f);
            m.SetOverrideTag(SourceTag, tmpl != null && !IsPerRock(tmpl) ? tmpl.name : "");
            if (c.MatchTerrain && m.HasProperty("_GRB_TerrainAuto")) m.SetFloat("_GRB_TerrainAuto", 1f);
            GenesisRockBlendGUI.Validate(m); // fixed vertex ranges + fade alpha-clip state
        }

        /// <summary>True when a loaded rock other than <paramref name="self"/> has <paramref name="m"/> in its own blend slot.</summary>
        private static bool UsedByOtherRock(Material m, DmRockCombiner self)
        {
            foreach (DmRockCombiner o in DmRockBaker.AllCombiners())
                if (o != null && o != self && o.OwnBlendMaterial == m)
                    return true;
            return false;
        }

        private static readonly HashSet<DmRockCombiner> s_blendQueue = new HashSet<DmRockCombiner>();

        /// <summary>Live-rebuild hook: queue (never create assets inside OnEnable / OnValidate).</summary>
        private static void QueueBlendCheck(DmRockCombiner c)
        {
            if (Application.isPlaying || !NeedsOwnBlend(c)) return;
            s_blendQueue.Add(c); // processed by the editor update tick (delayCall registrations made during a domain reload can be lost)
        }

        private static void ProcessBlendQueue()
        {
            if (s_blendQueue.Count == 0) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return; // next tick
            var list = s_blendQueue.ToList();
            s_blendQueue.Clear();
            foreach (DmRockCombiner c in list)
            {
                if (c == null || !NeedsOwnBlend(c)) continue;
                bool baked = c.IsBaked;
                EnsureOwnBlend(c);
                if (!baked) c.RefreshLiveMaterials();
                else if (c.BakeSettings.rebakeAfterEdit) DmRockBakeScheduler.Request(c, 0.5f, false);
            }
        }

        public sealed class TerrainLayerInfo
        {
            public TerrainLayer layer;
            public Terrain terrain;
            public Vector2 uvOffset;   // (tileOffset - terrain position) / tile size
            public float tiling;       // 1 / tile size
        }

        /// <summary>Dominant terrain layer under the rock's footprint (terrains only), with the terrain's UV origin.</summary>
        /// <summary>
        /// Dominant terrain layer under the rock's footprint (5x5 samples over the renderer bounds), on the same terrain the
        /// per-pixel match binds (PcgTerrainSplat.FindTerrain: footprint contains the pivot, closest surface height wins when
        /// terrains / loaded terrain scenes overlap). Layers without a diffuse texture are skipped in favour of the next one.
        /// </summary>
        public static TerrainLayerInfo FindTerrainLayer(DmRockCombiner c)
        {
            if (c == null) return null;
            var r = c.GetComponent<Renderer>();
            Vector3 p = c.transform.position;
            Bounds b = r != null && r.bounds.size.sqrMagnitude > 1e-6f ? r.bounds : new Bounds(p, Vector3.one * 4f);
            Terrain t = PcgTerrainSplat.FindTerrain(p);
            TerrainData td = t != null ? t.terrainData : null;
            if (td == null || td.terrainLayers == null || td.terrainLayers.Length == 0 || td.alphamapLayers == 0) return null;
            Vector3 tp = t.GetPosition(); Vector3 sz = td.size;
            var weight = new float[td.alphamapLayers];
            int aw = td.alphamapWidth, ah = td.alphamapHeight;
            for (int gx = 0; gx < 5; gx++)
                for (int gz = 0; gz < 5; gz++)
                {
                    float wx = Mathf.Lerp(b.min.x, b.max.x, gx / 4f), wz = Mathf.Lerp(b.min.z, b.max.z, gz / 4f);
                    int ax = Mathf.Clamp(Mathf.RoundToInt((wx - tp.x) / sz.x * (aw - 1)), 0, aw - 1);
                    int az = Mathf.Clamp(Mathf.RoundToInt((wz - tp.z) / sz.z * (ah - 1)), 0, ah - 1);
                    float[,,] a = td.GetAlphamaps(ax, az, 1, 1);
                    for (int l = 0; l < weight.Length; l++) weight[l] += a[0, 0, l];
                }
            int best = -1;
            for (int l = 0; l < weight.Length; l++)
            {
                TerrainLayer cand = l < td.terrainLayers.Length ? td.terrainLayers[l] : null;
                if (cand == null || cand.diffuseTexture == null) continue;
                if (best < 0 || weight[l] > weight[best]) best = l;
            }
            if (best < 0) return null;
            TerrainLayer layer = td.terrainLayers[best];
            Vector2 tile = new Vector2(Mathf.Max(0.01f, layer.tileSize.x), Mathf.Max(0.01f, layer.tileSize.y));
            return new TerrainLayerInfo
            {
                layer = layer,
                terrain = t,
                tiling = 1f / tile.x,
                uvOffset = new Vector2((layer.tileOffset.x - tp.x) / tile.x, (layer.tileOffset.y - tp.z) / tile.y),
            };
        }

        private static string Safe(string s) => new string((s ?? "null").Select(ch => char.IsLetterOrDigit(ch) || ch == '-' ? ch : '_').ToArray());

        private static string TerrainKey(TerrainLayerInfo tl) => tl != null ? "__" + Safe(tl.layer.name) + "_" + Hash128.Compute($"{Frac(tl.uvOffset.x):F3},{Frac(tl.uvOffset.y):F3},{tl.tiling:F4}").ToString().Substring(0, 6) : "";

        public static Material GetVariant(Material template, Material baseMat, TerrainLayerInfo tl)
        {
            string baseKey = Safe(baseMat.name.Replace("_Material", ""));
            // Per-rock blends own their variants (one per base material, refreshed in place on every bake); shared templates
            // keep one variant per base material + terrain layer / offset (shared by the rocks on that layer).
            string path = VariantFolder + "/" + Safe(template.name) + "__" + baseKey + (IsPerRock(template) ? "" : TerrainKey(tl)) + ".mat";
            if (!s_variants.TryGetValue(path, out Material m) || m == null)
                m = AssetDatabase.LoadAssetAtPath<Material>(path);
            // A shared variant edited by hand is still what the other rocks baked with it show: leave it to them (they keep
            // the edit in their own blend on their next bake) and use the next free / unedited slot.
            for (int n = 2; m != null && !IsPerRock(template) && n < 100 && VariantEdits(m, template, false).Count > 0; n++)
            {
                path = VariantFolder + "/" + Safe(template.name) + "__" + baseKey + TerrainKey(tl) + "__" + n + ".mat";
                if (!s_variants.TryGetValue(path, out m) || m == null)
                    m = AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            bool isNew = m == null;
            if (isNew) { EnsureFolder(VariantFolder); m = new Material(template) { name = Path.GetFileNameWithoutExtension(path) }; }
            ConfigureVariant(m, template, baseMat, tl);
            if (isNew) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
            s_variants[path] = m;
            return m;
        }

        /// <summary>Template properties + the base material's maps / remaps + the terrain layer (shared by bake and preview variants).</summary>
        public static void ConfigureVariant(Material m, Material template, Material baseMat, TerrainLayerInfo tl)
        {
            if (m.shader != template.shader) m.shader = template.shader; // deferred / forward-only follows the template
            m.CopyPropertiesFromMaterial(template);
            // Base layer = the baked material's own maps (atlas UVs or the override's mesh UVs).
            foreach (string tex in new[] { "_BaseColorMap", "_MaskMap", "_NormalMap" })
            {
                Texture t = baseMat.HasProperty(tex) ? baseMat.GetTexture(tex) : null;
                if (t == null && tex == "_BaseColorMap" && baseMat.HasProperty("_MainTex")) t = baseMat.GetTexture("_MainTex");
                m.SetTexture(tex, t);
                if (t != null && baseMat.HasProperty(tex))
                {
                    m.SetTextureScale(tex, baseMat.GetTextureScale(tex));
                    m.SetTextureOffset(tex, baseMat.GetTextureOffset(tex));
                }
            }
            foreach (string p in BaseProps)
                if (baseMat.HasProperty(p) && m.HasProperty(p))
                {
                    if (p == "_BaseColor") m.SetColor(p, baseMat.GetColor(p)); else m.SetFloat(p, baseMat.GetFloat(p));
                }
            if (tl != null)
            {
                m.SetTexture("_GRB_TerrainBaseMap", tl.layer.diffuseTexture);
                m.SetTexture("_GRB_TerrainNormalMap", tl.layer.normalMapTexture);
                m.SetFloat("_GRB_TerrainTiling", tl.tiling);
                m.SetVector("_GRB_TerrainUVOffset", new Vector4(Frac(tl.uvOffset.x), Frac(tl.uvOffset.y), 0f, 0f));
                m.SetFloat("_GRB_TerrainNormalScale", tl.layer.normalScale);
                // TerrainLit without a mask map uses the layer's smoothness value; match it so the blend reads like the ground.
                float terrSmooth = tl.layer.maskMapTexture != null ? Mathf.Lerp(tl.layer.maskMapRemapMin.w, tl.layer.maskMapRemapMax.w, 0.5f) : tl.layer.smoothness;
                m.SetFloat("_GRB_TerrainSmoothness", terrSmooth);
                Color tint = tl.layer.diffuseRemapMax; tint.a = 1f;
                m.SetColor("_GRB_TerrainTint", tint);
                // Wind-blown ground on the tops: the top layer takes the terrain layer's maps so deposits match the ground.
                if (m.HasProperty("_GRB_TopFromTerrain") && m.GetFloat("_GRB_TopFromTerrain") > 0.5f)
                {
                    m.SetTexture("_GRB_TopBaseMap", tl.layer.diffuseTexture);
                    m.SetTexture("_GRB_TopNormalMap", tl.layer.normalMapTexture);
                    m.SetTexture("_GRB_TopHeightMap", null);
                    m.SetFloat("_GRB_TopTiling", tl.tiling);
                    m.SetFloat("_GRB_TopNormalScale", tl.layer.normalScale);
                    m.SetColor("_GRB_TopTint", tint);
                    m.SetFloat("_GRB_TopSmoothness", terrSmooth);
                    if (m.HasProperty("_GRB_TopMaskMap")) m.SetTexture("_GRB_TopMaskMap", null);
                    if (m.HasProperty("_GRB_TopAOMap")) m.SetTexture("_GRB_TopAOMap", null);
                    GenesisRockBlendGUI.SyncTopMaps(m);
                }
            }
            // Texture-array bakes: the base layer samples the kit's arrays (slice in UV3); luminance reference from the arrays.
            bool arr = baseMat.HasProperty("_GRB_UseTexArray") && baseMat.GetFloat("_GRB_UseTexArray") > 0.5f && m.HasProperty("_GRB_UseTexArray");
            if (m.HasProperty("_GRB_UseTexArray")) m.SetFloat("_GRB_UseTexArray", arr ? 1f : 0f);
            foreach (string ap in ArrayProps)
                if (m.HasProperty(ap)) m.SetTexture(ap, arr && baseMat.HasProperty(ap) ? baseMat.GetTexture(ap) : null);
            if (arr)
                m.SetFloat("_GRB_BaseLumRef", baseMat.GetFloat("_GRB_BaseLumRef"));
            else
            {
                Texture bt = m.GetTexture("_BaseColorMap");
                Color bc = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                m.SetFloat("_GRB_BaseLumRef", AverageLuminance(bt, bc));
            }
            GenesisRockBlendGUI.Validate(m);
            StampBakeBase(m); // what it was made from: later edits on it are told apart from blend changes (kept on move / copy)
        }

        // ------------------------------------------------------------------------------------------------
        // Base map average luminance (linear), for band / height contrast that does not depend on how dark the rock is.

        private static readonly Dictionary<Texture, float> s_lum = new Dictionary<Texture, float>();

        public static float AverageLuminance(Texture tex, Color tint)
        {
            Color lt = tint.linear;
            float tl = Mathf.Max(0.02f, 0.2126f * lt.r + 0.7152f * lt.g + 0.0722f * lt.b);
            if (tex == null) return tl;
            if (!s_lum.TryGetValue(tex, out float avg))
            {
                const int N = 64;
                var rt = RenderTexture.GetTemporary(N, N, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                var prev = RenderTexture.active;
                Graphics.Blit(tex, rt); // mip selection averages the large map
                RenderTexture.active = rt;
                var t2 = new Texture2D(N, N, TextureFormat.RGBAHalf, false, true);
                t2.ReadPixels(new Rect(0, 0, N, N), 0, 0);
                t2.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                double sum = 0; int cnt = 0;
                foreach (Color p in t2.GetPixels())
                {
                    float l = 0.2126f * p.r + 0.7152f * p.g + 0.0722f * p.b;
                    if (l < 0.002f) continue; // atlas padding
                    sum += l; cnt++;
                }
                Object.DestroyImmediate(t2);
                avg = cnt > 0 ? (float)(sum / cnt) : 0.1f;
                s_lum[tex] = avg;
            }
            return Mathf.Max(0.005f, avg * tl);
        }

        // ------------------------------------------------------------------------------------------------
        // Live preview (unbaked rocks): in-memory variants of the template per pack material, refreshed on template edits.

        private static readonly Dictionary<(Material, Material, string), Material> s_preview = new Dictionary<(Material, Material, string), Material>();
        private static readonly Dictionary<(Material, Material, string), TerrainLayerInfo> s_previewTl = new Dictionary<(Material, Material, string), TerrainLayerInfo>();

        public static Material[] PreviewMaterials(DmRockCombiner c, Material[] mats)
        {
            Material template = c != null ? c.BlendMaterial : null;
            if (!IsBlendMaterial(template) || mats == null) return mats;
            TerrainLayerInfo tl = template.GetFloat("_GRB_TerrainAuto") > 0.5f && c.MatchTerrain ? FindTerrainLayer(c) : null;
            string tk = TerrainKey(tl);
            var o = new Material[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                Material bm = mats[i];
                if (bm == null || IsBlendMaterial(bm)) { o[i] = bm; continue; }
                var key = (template, bm, tk);
                if (!s_preview.TryGetValue(key, out Material m) || m == null)
                {
                    m = new Material(template) { name = template.name + " (preview " + bm.name + ")", hideFlags = HideFlags.DontSave };
                    ConfigureVariant(m, template, bm, tl);
                    s_preview[key] = m;
                    s_previewTl[key] = tl;
                }
                o[i] = m;
            }
            return o;
        }

        /// <summary>Re-applies template edits to its live preview variants (called by the material inspector).</summary>
        public static void RefreshPreviews(Material template)
        {
            foreach (var kv in s_preview)
                if (kv.Value != null && kv.Key.Item1 != null && kv.Key.Item2 != null && (template == null || kv.Key.Item1 == template))
                    ConfigureVariant(kv.Value, kv.Key.Item1, kv.Key.Item2, s_previewTl.TryGetValue(kv.Key, out TerrainLayerInfo tl) ? tl : null);
        }

        /// <summary>
        /// After a blend material edit: live previews follow, every rock re-binds the per-pixel terrain (a toggled Ground Fade /
        /// Terrain Blend changes whether a rock wants it), and baked rocks whose own per-rock blend was edited are rebaked
        /// (debounced, when Rebake After Edit is on), since their baked variant is regenerated from it.
        /// </summary>
        public static void OnBlendEdited(IEnumerable<Material> edited)
        {
            var set = new HashSet<Material>(edited.Where(m => m != null));
            if (Application.isPlaying)
            {
                foreach (Material m in set)
                {
                    if (EditorUtility.IsPersistent(m)) s_editedInPlay.Add(m); // rebaked after Play
                    else DivergePlayEdit(m);
                }
            }
            else
            {
                // Edited the material a rock draws (bake variant / live preview): it is regenerated from the rock's blend on
                // the next bake (move, scale, copy), so the values go into that blend now.
                foreach (Material m in set.ToList())
                    foreach (Material own in WriteBackGeneratedEdits(m)) set.Add(own);
            }
            foreach (Material m in set) RefreshPreviews(m);
            foreach (DmRockCombiner c in Object.FindObjectsByType<DmRockCombiner>(FindObjectsInactive.Exclude))
            {
                if (c.IsBaked && c.OwnBlendMaterial != null && set.Contains(c.OwnBlendMaterial) && IsPerRock(c.OwnBlendMaterial) && c.BakeSettings.rebakeAfterEdit)
                    DmRockBakeScheduler.Request(c, 0.6f, false);
                PcgTerrainSplat.Apply(c.gameObject);
            }
        }

        /// <summary>Re-binds the per-pixel terrain on every rock (scene open, terrain scenes loaded later, menu).</summary>
        [MenuItem("Tools/Genesis PCG Rock Creation/Refresh Terrain Match (all rocks)")]
        public static void RebindAllTerrain()
        {
            foreach (DmRockCombiner c in Object.FindObjectsByType<DmRockCombiner>(FindObjectsInactive.Exclude))
                PcgTerrainSplat.Apply(c.gameObject);
        }

        [InitializeOnLoadMethod]
        private static void RegisterSceneHooks()
        {
            // Terrain scenes loaded after the rock scene (or in another order) register their terrains later than the rocks'
            // OnEnable / Start: re-bind once the editor is idle.
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened -= OnSceneOpened;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene s, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            EditorApplication.delayCall -= RebindAllTerrain;
            EditorApplication.delayCall += RebindAllTerrain;
        }

        [InitializeOnLoadMethod]
        private static void RegisterPreviewHook()
        {
            DmRockCombiner.LiveMaterialHook = PreviewMaterials;
            DmRockCombiner.BlendCheckHook = QueueBlendCheck;
            EditorApplication.update -= ProcessBlendQueue;
            EditorApplication.update += ProcessBlendQueue;
            EditorApplication.CallbackFunction once = null;
            once = () =>
            {
                EditorApplication.update -= once;
                foreach (DmRockCombiner c in Object.FindObjectsByType<DmRockCombiner>(FindObjectsInactive.Exclude))
                {
                    if (c.BlendMaterial != null) c.RefreshLiveMaterials();
                    else PcgTerrainSplat.Apply(c.gameObject);
                    if (!c.IsBaked) QueueBlendCheck(c);
                }
            };
            EditorApplication.update += once;
        }

        private static float Frac(float x) => x - Mathf.Floor(x);
    }
}
#endif
