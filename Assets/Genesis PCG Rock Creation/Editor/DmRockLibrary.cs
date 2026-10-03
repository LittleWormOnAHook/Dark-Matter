#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GenesisPCG.RockCreation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using K = GenesisPCG.RockCreation.DmRockStyle.StyleKind;
using M = GenesisPCG.RockCreation.DmRockPieceClassMask;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Creates / refreshes the package's data library: one DmRockStyle per kind (research numbers), the formation recipe,
    /// Io Moon Surface + alien placeholder HDRP/Lit materials, and DmRockPresets (one per style plus combos).
    /// Writes assets directly (no Refresh); only the generated mask textures are imported, by path.
    /// </summary>
    public static class DmRockLibrary
    {
        public const string Root = "Assets/Genesis PCG Rock Creation";
        public const string StylesFolder = Root + "/Data/Styles";
        public const string PresetsFolder = Root + "/Data/Presets";
        public const string RecipesFolder = Root + "/Data/Recipes";
        public const string MaterialsFolder = Root + "/Generated/Materials";
        public const string IoMaterialsFolder = MaterialsFolder + "/Io";
        public const string FormationRecipePath = RecipesFolder + "/RockCombineRecipe_Formation.asset";
        public const string IoSourceRoot = "Assets/_Project/Art/Terrain/Io_Moon_Surface";
        public const string AlienMaterialPath = MaterialsFolder + "/M_Rock_AlienGlass_Placeholder.mat";
        /// <summary>4K CC0 Io sets (terrain only: TerrainLayers; not offered to the rock tool): HiRes_4K/&lt;Subset&gt;/&lt;SetName&gt;/Io4K_SetNN_&lt;SetName&gt;_{Color,Normal,MaskHDRP,Height}.</summary>
        public const string Io4KSourceRoot = IoSourceRoot + "/HiRes_4K";
        public const string Io4KMaterialsFolder = MaterialsFolder + "/Io4K";
        public const string Io4KTerrainLayersFolder = Io4KMaterialsFolder + "/TerrainLayers";

        public static readonly string[] IoSets =
        {
            "Set01_Basalt_Sulfur_Dust", "Set02_Sulfur_Plains", "Set03_Geyser_Sinter", "Set04_Lava_Cooled_Glass", "Set05_Ash_Regolith",
            "Set06_Brimstone_Crystal", "Set07_Radial_Flow_Bands", "Set08_Radiation_Scorched", "Set09_Alien_Residue", "Set10_Anthropogenic_Trace",
        };

        [MenuItem("Tools/Genesis PCG Rock Creation/Build Style + Preset Library")]
        public static void BuildAllMenu()
        {
            string log = BuildAll(false);
            Debug.Log("Genesis PCG Rock Creation library:\n" + log);
        }

        /// <summary>Everything (kit analysis, styles, recipe, materials, presets). <paramref name="overwriteStyles"/> re-applies the research numbers.</summary>
        public static string BuildAll(bool overwriteStyles)
        {
            var log = new System.Text.StringBuilder();
            DmRockKit kit = DmRockCombinerEditor.EnsureKit(false);
            if (kit.pieces == null || kit.pieces.Count == 0)
            {
                DmRockKitEditor.Analyze(kit);
                AssetDatabase.SaveAssetIfDirty(kit);
                log.AppendLine($"kit analyzed: {kit.pieces.Count} pieces");
            }
            EnsureMaterials(log);
            DmRockCombineRecipe recipe = EnsureFormationRecipe(log);
            UpgradeAtlasSettings(log);
            Dictionary<K, DmRockStyle> styles = EnsureStyles(overwriteStyles, log);
            EnsurePresets(kit, recipe, styles, overwriteStyles, log);
            return log.ToString();
        }

        // ------------------------------------------------------------------------------------------------
        // Styles

        public static Dictionary<K, DmRockStyle> EnsureStyles(bool overwrite, System.Text.StringBuilder log = null)
        {
            EnsureFolder(StylesFolder);
            var map = new Dictionary<K, DmRockStyle>();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (K kind in (K[])Enum.GetValues(typeof(K)))
                {
                    string path = $"{StylesFolder}/DM_RockStyle_{kind}.asset";
                    var st = AssetDatabase.LoadAssetAtPath<DmRockStyle>(path);
                    bool isNew = st == null;
                    if (isNew) st = ScriptableObject.CreateInstance<DmRockStyle>();
                    if (isNew || overwrite)
                    {
                        Configure(st, kind);
                        if (isNew) AssetDatabase.CreateAsset(st, path);
                        else EditorUtility.SetDirty(st);
                        st.Version++;
                        log?.AppendLine((isNew ? "created " : "updated ") + path);
                    }
                    map[kind] = st;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            foreach (DmRockStyle st in map.Values) AssetDatabase.SaveAssetIfDirty(st);
            return map;
        }

        /// <summary>Per-style numbers from the research brief (section 3a) on top of the general rules.</summary>
        public static void Configure(DmRockStyle s, K kind)
        {
            string keepName = s.name;
            var d = ScriptableObject.CreateInstance<DmRockStyle>();
            EditorUtility.CopySerialized(d, s);
            Object.DestroyImmediate(d);
            s.name = keepName;
            s.kind = kind;
            switch (kind)
            {
                case K.BoulderPile:
                    s.notes = "5-15 pieces; anchor 1.0, mid 0.5-0.7, rest 0.2-0.4; <=2 tiers, upper <=0.6x; one grain; sink 15-30%; passage 5%; debris ring ~1.3x.";
                    s.height = new Vector2(2.4f, 3.8f); s.footprintRadius = new Vector2(2.5f, 3.8f); s.pieceCount = new Vector2Int(5, 12);
                    s.midScale = new Vector2(0.5f, 0.7f); s.fillScale = new Vector2(0.2f, 0.4f); s.midShare = 0.4f;
                    s.heroClasses = M.Boulder; s.midClasses = M.Boulder | M.Slab; s.fillClasses = M.Boulder | M.Small | M.Slab; s.debrisClasses = M.Small | M.Boulder;
                    s.leanDegrees = new Vector2(0f, 12f); s.grainJitter = 8f; s.beddingAlign = 0.7f;
                    s.sink = new Vector2(0.15f, 0.3f); s.maxTiers = 2; s.upperMaxRatio = 0.6f;
                    s.apronCount = new Vector2Int(5, 10); s.apronScale = new Vector2(0.06f, 0.16f); s.apronReach = 0.4f; s.downhillBias = 0.5f;
                    s.passageChance = 0.05f; s.nookChance = 0.35f;
                    break;
                case K.OutcropShelf:
                    s.notes = "3-8 shingled slabs stepped back 0.5-2 m; dip 5-25 deg, strike +-5; sink 30-50%; undercut nooks 20%; downslope slab fragments.";
                    s.height = new Vector2(1.8f, 3.2f); s.footprintRadius = new Vector2(3f, 4.5f); s.pieceCount = new Vector2Int(4, 8);
                    s.heroClasses = M.Slab | M.Boulder; s.midClasses = M.Slab; s.fillClasses = M.Slab | M.Small; s.debrisClasses = M.Slab | M.Small;
                    s.leanDegrees = new Vector2(5f, 25f); s.grainJitter = 4f; s.beddingAlign = 0.95f;
                    s.sink = new Vector2(0.3f, 0.5f); s.maxTiers = 3; s.upperMaxRatio = 0.9f; s.maxWidthOverSupport = 1.2f;
                    s.apronCount = new Vector2Int(4, 9); s.apronScale = new Vector2(0.08f, 0.2f); s.apronReach = 0.8f; s.downhillBias = 0.8f;
                    s.passageChance = 0.05f; s.nookChance = 0.5f;
                    break;
                case K.Spire:
                    s.notes = "1 tall hero + siblings 0.4-0.7x with 1-3 m gaps; lean <=10 deg, shared; no stacking; sink 20-35%; debris ring biased to the lean side; 1-2 base rocks.";
                    s.height = new Vector2(6f, 9.5f); s.footprintRadius = new Vector2(2f, 3.5f); s.pieceCount = new Vector2Int(2, 5);
                    s.midScale = new Vector2(0.4f, 0.7f); s.heroClasses = M.Tall; s.midClasses = M.Tall; s.debrisClasses = M.Small | M.Boulder;
                    s.leanDegrees = new Vector2(0f, 10f); s.grainJitter = 3f; s.sink = new Vector2(0.2f, 0.35f); s.maxTiers = 0;
                    s.apronCount = new Vector2Int(4, 9); s.apronScale = new Vector2(0.04f, 0.11f); s.apronReach = 0.45f; s.downhillBias = 0.75f;
                    s.passageChance = 0.05f; s.nookChance = 0f;
                    break;
                case K.Arch:
                    s.notes = "2 tall legs + lintel (thickness >= 0.25 x span); fin aligned to strike; legs sink 25-40%; passage 80% (span >= 2.5 m, clear >= 3 m); blocks off the walk line.";
                    s.height = new Vector2(4f, 5.5f); s.footprintRadius = new Vector2(3f, 4f); s.pieceCount = new Vector2Int(3, 6);
                    s.heroClasses = M.Tall; s.midClasses = M.Slab | M.Boulder; s.debrisClasses = M.Small | M.Boulder;
                    s.leanDegrees = new Vector2(0f, 4f); s.grainJitter = 3f; s.sink = new Vector2(0.25f, 0.4f); s.maxTiers = 1;
                    s.archSpan = new Vector2(2.6f, 4f); s.archHeight = new Vector2(3f, 4f); s.lintelThicknessRatio = 0.3f;
                    s.apronCount = new Vector2Int(4, 8); s.apronScale = new Vector2(0.04f, 0.1f); s.apronReach = 0.4f; s.downhillBias = 0.4f;
                    s.passageChance = 0.8f; s.nookChance = 0f;
                    break;
                case K.RubbleScatter:
                    s.notes = "10-60 pieces at 0.1-0.4; single layer, flats lie flat; sink 20-40%; density falls off from the source.";
                    s.height = new Vector2(2.2f, 3.2f); s.footprintRadius = new Vector2(4f, 6.5f); s.pieceCount = new Vector2Int(26, 48);
                    s.fillScale = new Vector2(0.1f, 0.4f); s.fillClasses = M.Small | M.Boulder | M.Slab; s.heroClasses = M.Boulder;
                    s.leanDegrees = new Vector2(0f, 4f); s.grainJitter = 10f; s.beddingAlign = 0.9f; s.sink = new Vector2(0.2f, 0.4f); s.maxTiers = 0;
                    s.apronCount = new Vector2Int(0, 0); s.passageChance = 0f; s.nookChance = 0f;
                    break;
                case K.VolcanicColumnar:
                    s.notes = "8-40 columns, equal diameter +-15%; hex lattice, stepped tops +-0.2-1.5 m, vertical or fan <=30 deg; sink 10-20%; missing columns = nooks; broken segments on a 34-37 deg cone.";
                    s.height = new Vector2(3.2f, 5.5f); s.footprintRadius = new Vector2(2.5f, 4f); s.columnCount = new Vector2Int(10, 26);
                    s.columnStep = new Vector2(0.2f, 1.5f); s.columnFan = 10f; s.missingColumnChance = 0.12f; s.maxStretch = 1.5f;
                    s.heroClasses = M.Tall; s.debrisClasses = M.Tall | M.Boulder | M.Small;
                    s.leanDegrees = new Vector2(0f, 6f); s.grainJitter = 1.5f; s.sink = new Vector2(0.1f, 0.2f); s.maxTiers = 0;
                    s.apronCount = new Vector2Int(6, 12); s.apronScale = new Vector2(0.06f, 0.14f); s.apronReach = 0.6f; s.reposeDegrees = 35f; s.downhillBias = 0.5f;
                    s.passageChance = 0.05f; s.nookChance = 0.45f;
                    break;
                case K.VolcanicEjecta:
                    s.notes = "10-40 blocks; density ~1/r^2, size ~1/r, optional fan sector; tilt 10-30 deg; sink 30-60%.";
                    s.height = new Vector2(2f, 3f); s.footprintRadius = new Vector2(8f, 12f); s.pieceCount = new Vector2Int(24, 40);
                    s.heroClasses = M.Boulder; s.fillClasses = M.Boulder | M.Small | M.Slab; s.preferSharp = 0.4f;
                    s.leanDegrees = new Vector2(0f, 0f); s.sink = new Vector2(0.3f, 0.6f); s.maxTiers = 0; s.ejectaFanDegrees = 360f;
                    s.apronCount = new Vector2Int(0, 0); s.passageChance = 0f; s.nookChance = 0f;
                    break;
                case K.VolcanicDome:
                    s.notes = "8-20 pieces; core + angular mids on a 35-40 deg pile, radial; spines on top; sink 20-30%; block-and-ash apron.";
                    s.height = new Vector2(3f, 4.5f); s.footprintRadius = new Vector2(3f, 4.5f); s.pieceCount = new Vector2Int(9, 16);
                    s.midScale = new Vector2(0.35f, 0.55f); s.heroClasses = M.Boulder; s.midClasses = M.Boulder | M.Slab; s.debrisClasses = M.Small | M.Boulder;
                    s.preferSharp = 0.6f; s.leanDegrees = new Vector2(0f, 6f); s.grainJitter = 8f; s.sink = new Vector2(0.2f, 0.3f); s.maxTiers = 2;
                    s.maxContactSlope = 42f; s.upperMaxRatio = 0.6f;
                    s.apronCount = new Vector2Int(10, 18); s.apronScale = new Vector2(0.04f, 0.12f); s.apronReach = 0.8f; s.downhillBias = 0.35f;
                    s.passageChance = 0.03f; s.nookChance = 0.25f;
                    break;
                case K.ErodedHoodoo:
                    s.notes = "3-12 stems at 0.5-1.0 height variety sharing one base; caps 1-1.5x the stem top (only style allowed to cap); sink 10-20%; lanes 2-4 m (high); fallen caps.";
                    s.height = new Vector2(4f, 6.5f); s.footprintRadius = new Vector2(2.6f, 4f); s.pieceCount = new Vector2Int(3, 8);
                    s.heroClasses = M.Tall; s.midClasses = M.Tall; s.debrisClasses = M.Small | M.Boulder;
                    s.leanDegrees = new Vector2(0f, 4f); s.grainJitter = 4f; s.sink = new Vector2(0.1f, 0.2f); s.maxTiers = 1;
                    s.allowCaps = true; s.capWidthRatio = new Vector2(1f, 1.5f);
                    s.apronCount = new Vector2Int(4, 8); s.apronScale = new Vector2(0.04f, 0.1f); s.apronReach = 0.45f;
                    s.passageChance = 0.6f; s.nookChance = 0.3f;
                    break;
                case K.ErodedMesa:
                    s.notes = "1-3 big bodies + flat caprock; full 34-37 deg rubble skirt; sink 30-50%; gullies (medium).";
                    s.height = new Vector2(3.5f, 5.5f); s.footprintRadius = new Vector2(3.5f, 5.5f); s.pieceCount = new Vector2Int(1, 3);
                    s.heroClasses = M.Boulder | M.Slab; s.midClasses = M.Slab; s.debrisClasses = M.Small | M.Boulder | M.Slab;
                    s.leanDegrees = new Vector2(0f, 3f); s.grainJitter = 2f; s.sink = new Vector2(0.3f, 0.5f); s.maxTiers = 1;
                    s.apronCount = new Vector2Int(14, 24); s.apronScale = new Vector2(0.04f, 0.13f); s.apronReach = 1.0f; s.reposeDegrees = 35f; s.downhillBias = 0.2f;
                    s.passageChance = 0.35f; s.nookChance = 0.4f;
                    break;
                case K.ErodedYardang:
                    s.notes = "3-10 wind-aligned ridges (+-10 deg); 4:1 to 10:2:1, stretch <=1.5x; streamlined tails; sink 20-30%; sand corridors (high).";
                    s.height = new Vector2(1.4f, 2.4f); s.footprintRadius = new Vector2(4f, 6f); s.ridgeCount = new Vector2Int(3, 6);
                    s.heroClasses = M.Slab | M.Boulder; s.debrisClasses = M.Small;
                    s.leanDegrees = new Vector2(0f, 2f); s.grainJitter = 8f; s.sink = new Vector2(0.2f, 0.3f); s.maxTiers = 0; s.maxStretch = 1.5f;
                    s.apronCount = new Vector2Int(2, 5); s.apronScale = new Vector2(0.05f, 0.12f); s.apronReach = 0.6f; s.downhillBias = 0.6f;
                    s.passageChance = 0.7f; s.nookChance = 0f;
                    break;
                case K.SharpClean:
                    s.notes = "3-10 pieces; 1 hero + 2-3 mid, wedged not perched; +-3 deg grain; planar slices with crisp cap faces; sink 20-35%; clefts 1-2 m; angular scree.";
                    s.height = new Vector2(4.5f, 7f); s.footprintRadius = new Vector2(2.5f, 3.8f); s.pieceCount = new Vector2Int(4, 8);
                    s.midShare = 0.4f; s.fillScale = new Vector2(0.15f, 0.3f);
                    s.heroClasses = M.Tall; s.midClasses = M.Tall | M.Slab; s.fillClasses = M.Slab | M.Small; s.debrisClasses = M.Small | M.Slab;
                    s.preferSharp = 1f; s.leanDegrees = new Vector2(4f, 14f); s.grainJitter = 3f; s.alignmentDegrees = 3f;
                    s.sink = new Vector2(0.2f, 0.35f); s.maxTiers = 1;
                    s.planeCutChance = 0.65f; s.planeCutMaxAngle = 22f;
                    s.apronCount = new Vector2Int(5, 10); s.apronScale = new Vector2(0.04f, 0.1f); s.apronReach = 0.5f; s.downhillBias = 0.7f;
                    s.passageChance = 0.2f; s.nookChance = 0.3f;
                    break;
                case K.AlienShards:
                    s.notes = "3-15 shards from 1-3 seed points; hero 1.0, cluster 0.3-0.7, splinters 0.1; lean outward 10-35 deg, curve 5-20 deg (bend), tapered tips; sink 25-45%.";
                    s.height = new Vector2(4.5f, 7f); s.footprintRadius = new Vector2(2.5f, 4f);
                    s.midScale = new Vector2(0.3f, 0.7f); s.heroClasses = M.Tall; s.debrisClasses = M.Small;
                    s.shardSeeds = new Vector2Int(1, 3); s.shardsPerSeed = new Vector2Int(3, 6);
                    s.shardLean = new Vector2(10f, 35f); s.shardCurve = new Vector2(5f, 20f); s.shardTipScale = 0.12f; s.shardTaperPower = 1.6f; s.shardStretch = new Vector2(1.3f, 1.8f);
                    s.preferSharp = 1f; s.leanDegrees = new Vector2(0f, 0f); s.sink = new Vector2(0.25f, 0.45f); s.maxTiers = 0;
                    s.apronCount = new Vector2Int(0, 0); s.passageChance = 0.4f; s.nookChance = 0f;
                    break;
                case K.AlienFloating:
                    s.notes = "Monolith with inverted taper hovering 2-30 m + 3-8 satellites; high under-mass passage; no conform.";
                    s.height = new Vector2(4f, 6.5f); s.footprintRadius = new Vector2(3f, 4.5f);
                    s.heroClasses = M.Tall | M.Boulder; s.hover = new Vector2(3.5f, 7f); s.satellites = new Vector2Int(3, 7);
                    s.leanDegrees = new Vector2(0f, 0f); s.maxTiers = 0; s.apronCount = new Vector2Int(0, 0);
                    s.passageChance = 1f; s.nookChance = 0f;
                    break;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Recipe

        /// <summary>
        /// Every combine recipe of the package -> 8192 atlas, padding scaled with the atlas (16 px @ 4096 = 32 px @ 8192).
        /// Only the recipe assets are written; atlases are rebuilt by the next bake (new cache key).
        /// </summary>
        public static int UpgradeAtlasSettings(System.Text.StringBuilder log = null)
        {
            int n = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:DmRockCombineRecipe", new[] { Root }))
            {
                var r = AssetDatabase.LoadAssetAtPath<DmRockCombineRecipe>(AssetDatabase.GUIDToAssetPath(guid));
                if (r == null) continue;
                r.bake ??= new DmRockBakeSettings();
                int oldSize = (int)r.bake.atlasSize;
                int want = DmRockBakeSettings.StandardAtlasSize;
                int pad = Mathf.Max(DmRockBakeSettings.StandardAtlasPadding, Mathf.RoundToInt(r.bake.atlasPadding * (float)want / Mathf.Max(1, oldSize)));
                if (oldSize == want && r.bake.atlasPadding >= DmRockBakeSettings.StandardAtlasPadding) continue;
                r.bake.atlasSize = (DmRockAtlasSize)want;
                r.bake.atlasPadding = oldSize == want ? DmRockBakeSettings.StandardAtlasPadding : pad;
                EditorUtility.SetDirty(r);
                AssetDatabase.SaveAssetIfDirty(r);
                log?.AppendLine($"Recipe {r.name}: atlas {oldSize} -> {want}, padding -> {r.bake.atlasPadding}px");
                n++;
            }
            return n;
        }

        public static DmRockCombineRecipe EnsureFormationRecipe(System.Text.StringBuilder log = null)
        {
            var r = AssetDatabase.LoadAssetAtPath<DmRockCombineRecipe>(FormationRecipePath);
            if (r != null) return r;
            DmRockCombineRecipe src = DmRockCombinerEditor.EnsureRecipe();
            r = Object.Instantiate(src);
            r.name = Path.GetFileNameWithoutExtension(FormationRecipePath);
            // Whole formations follow the terrain normal only slightly (spires stay plumb).
            r.snap.alignStrength = 0.25f;
            r.snap.maxAlignAngle = 8f;
            r.snap.sinkDepth = 0.02f;
            EnsureFolder(RecipesFolder);
            AssetDatabase.CreateAsset(r, FormationRecipePath);
            AssetDatabase.SaveAssetIfDirty(r);
            log?.AppendLine("created " + FormationRecipePath);
            return r;
        }

        // ------------------------------------------------------------------------------------------------
        // Materials

        public static string IoMaterialPath(int set) => $"{IoMaterialsFolder}/M_Rock_Io_{IoSets[set - 1]}.mat";

        public static List<Material> MaterialChoices()
        {
            var list = new List<Material>();
            for (int i = 1; i <= IoSets.Length; i++)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(IoMaterialPath(i));
                if (m != null) list.Add(m);
            }
            var a = AssetDatabase.LoadAssetAtPath<Material>(AlienMaterialPath);
            if (a != null) list.Add(a);
            return list;
        }

        /// <summary>Per-file import settings for one 4K set texture (no postprocessor). kind: 0 colour, 1 normal, 2 HDRP mask, 3 height.</summary>
        public static bool ApplyIo4KImport(string path, int kind)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return false;
            ti.textureType = kind == 1 ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = kind == 0;
            ti.alphaSource = kind == 2 ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 4;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            TextureImporterFormat fmt = kind == 1 ? TextureImporterFormat.BC5 : (kind == 3 ? TextureImporterFormat.BC4 : TextureImporterFormat.BC7);
            foreach (string plat in new[] { "Standalone" })
            {
                TextureImporterPlatformSettings ps = ti.GetPlatformTextureSettings(plat);
                ps.overridden = true; ps.maxTextureSize = 4096; ps.format = fmt; ps.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.SetPlatformTextureSettings(ps);
            }
            ti.SaveAndReimport();
            return true;
        }

        public struct Io4KSet { public string subset, setName, dir, prefix; }

        /// <summary>All 4K sets found on disk (HiRes_4K/&lt;Subset&gt;/&lt;SetName&gt;/ with an Io4K_*_Color file).</summary>
        public static List<Io4KSet> FindIo4KSets()
        {
            var list = new List<Io4KSet>();
            if (!Directory.Exists(Io4KSourceRoot)) return list;
            foreach (string sub in Directory.GetDirectories(Io4KSourceRoot).OrderBy(x => x, StringComparer.Ordinal))
                foreach (string dir in Directory.GetDirectories(sub).OrderBy(x => x, StringComparer.Ordinal))
                {
                    string d = dir.Replace('\\', '/');
                    string col = Directory.GetFiles(d, "Io4K_*_Color.*").FirstOrDefault();
                    if (col == null) continue;
                    string f = Path.GetFileName(col);
                    list.Add(new Io4KSet { subset = Path.GetFileName(sub), setName = Path.GetFileName(d), dir = d, prefix = f.Substring(0, f.LastIndexOf("_Color", StringComparison.Ordinal)) });
                }
            return list;
        }

        public static string Io4KFile(Io4KSet s, string map)
        {
            string hit = Directory.GetFiles(s.dir, s.prefix + "_" + map + ".*").FirstOrDefault(x => !x.EndsWith(".meta", StringComparison.Ordinal));
            return hit?.Replace('\\', '/');
        }

        public static string Io4KMaterialPath(Io4KSet s) => $"{Io4KMaterialsFolder}/M_Rock_{s.prefix}.mat";

        /// <summary>
        /// HDRP/Lit rock materials (and unassigned TerrainLayer assets) for every 4K set. Existing assets are kept unless
        /// <paramref name="overwrite"/>. Normal strength 1.25 (the legacy Set01 material sits at 0), smoothness from the mask
        /// (A = 1 - roughness) remapped to 0..0.8, AO from mask G. The height map is assigned for reference only (no POM / tessellation).
        /// </summary>
        public static int EnsureIo4KMaterials(bool overwrite, System.Text.StringBuilder log = null)
        {
            Shader lit = Shader.Find("HDRP/Lit");
            if (lit == null) { log?.AppendLine("HDRP/Lit not found"); return 0; }
            EnsureFolder(Io4KMaterialsFolder); EnsureFolder(Io4KTerrainLayersFolder);
            int made = 0;
            foreach (Io4KSet s in FindIo4KSets())
            {
                var col = AssetDatabase.LoadAssetAtPath<Texture2D>(Io4KFile(s, "Color"));
                var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(Io4KFile(s, "Normal"));
                var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(Io4KFile(s, "MaskHDRP"));
                var hgt = AssetDatabase.LoadAssetAtPath<Texture2D>(Io4KFile(s, "Height"));
                if (col == null) { log?.AppendLine("not imported yet: " + s.dir); continue; }
                string path = Io4KMaterialPath(s);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool isNew = m == null;
                if (!isNew && !overwrite) { log?.AppendLine("kept " + path); }
                else
                {
                    if (isNew) m = new Material(lit) { name = Path.GetFileNameWithoutExtension(path) };
                    m.shader = lit;
                    m.SetTexture("_BaseColorMap", col);
                    m.SetColor("_BaseColor", Color.white);
                    m.SetTexture("_NormalMap", nrm); m.SetFloat("_NormalScale", 1.25f);
                    m.SetTexture("_MaskMap", mask);
                    m.SetFloat("_MetallicRemapMin", 0f); m.SetFloat("_MetallicRemapMax", 0f);
                    m.SetFloat("_SmoothnessRemapMin", 0f); m.SetFloat("_SmoothnessRemapMax", s.subset.StartsWith("Set04", StringComparison.Ordinal) ? 0.9f : 0.8f);
                    m.SetFloat("_AORemapMin", 0f); m.SetFloat("_AORemapMax", 1f);
                    if (hgt != null) { m.SetTexture("_HeightMap", hgt); m.SetFloat("_DisplacementMode", 0f); m.SetFloat("_HeightAmplitude", 0.02f); }
                    m.SetTextureScale("_BaseColorMap", new Vector2(1f, 1f));
                    ResetKeywords(m);
                    if (isNew) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
                    AssetDatabase.SaveAssetIfDirty(m);
                    made++;
                    log?.AppendLine((isNew ? "created " : "updated ") + path);
                }
                string tlPath = $"{Io4KTerrainLayersFolder}/TL_{s.prefix}.terrainlayer";
                if (AssetDatabase.LoadAssetAtPath<TerrainLayer>(tlPath) == null)
                {
                    var tl = new TerrainLayer { diffuseTexture = col, normalMapTexture = nrm, maskMapTexture = mask, normalScale = 1f, tileSize = new Vector2(4f, 4f),
                        smoothness = 0f, metallic = 0f, maskMapRemapMin = Vector4.zero, maskMapRemapMax = new Vector4(0f, 1f, 1f, 0.8f) };
                    AssetDatabase.CreateAsset(tl, tlPath);
                    AssetDatabase.SaveAssetIfDirty(tl);
                    log?.AppendLine("terrain layer (unassigned) " + tlPath);
                }
            }
            return made;
        }

        public static void EnsureMaterials(System.Text.StringBuilder log = null)
        {
            EnsureFolder(IoMaterialsFolder);
            Shader lit = Shader.Find("HDRP/Lit");
            if (lit == null) { log?.AppendLine("HDRP/Lit not found: materials skipped"); return; }

            // 1) HDRP-packed masks (R metallic 0, G AO, B detail 1, A smoothness = source A) from the Io masks' raw PNG bytes.
            var newMasks = new List<string>();
            for (int i = 1; i <= IoSets.Length; i++)
            {
                string outPath = $"{IoMaterialsFolder}/Io_Set{i:00}_MaskHDRP.png";
                if (File.Exists(outPath)) continue;
                string src = $"{IoSourceRoot}/{IoSets[i - 1]}/Io_Set{i:00}_Mask_Periodic_2048.png";
                if (!File.Exists(src)) { log?.AppendLine("missing " + src); continue; }
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                tex.LoadImage(File.ReadAllBytes(src), false);
                Color32[] px = tex.GetPixels32();
                for (int p = 0; p < px.Length; p++)
                {
                    Color32 c = px[p];
                    px[p] = new Color32(0, c.g, 255, c.a); // source masks are already HDRP-packed: A is smoothness (R is a constant metal value)
                }
                tex.SetPixels32(px);
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                newMasks.Add(outPath);
            }
            if (newMasks.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try { foreach (string p in newMasks) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport); }
                finally { AssetDatabase.StopAssetEditing(); }
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string p in newMasks)
                    {
                        var imp = AssetImporter.GetAtPath(p) as TextureImporter;
                        if (imp == null) continue;
                        imp.sRGBTexture = false;
                        imp.mipmapEnabled = true;
                        imp.maxTextureSize = 2048;
                        imp.textureCompression = TextureImporterCompression.Compressed;
                        imp.SaveAndReimport();
                    }
                }
                finally { AssetDatabase.StopAssetEditing(); }
                log?.AppendLine($"generated {newMasks.Count} HDRP mask maps");
            }

            // 2) Materials.
            AssetDatabase.StartAssetEditing();
            var touched = new List<Material>();
            try
            {
                for (int i = 1; i <= IoSets.Length; i++)
                {
                    string path = IoMaterialPath(i);
                    if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) continue;
                    string dir = $"{IoSourceRoot}/{IoSets[i - 1]}";
                    var col = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/Io_Set{i:00}_Color_Periodic_2048.png");
                    var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/Io_Set{i:00}_Normal_Periodic_2048.png");
                    var mask = AssetDatabase.LoadAssetAtPath<Texture2D>($"{IoMaterialsFolder}/Io_Set{i:00}_MaskHDRP.png");
                    if (col == null) { log?.AppendLine("missing color for " + IoSets[i - 1]); continue; }
                    var m = new Material(lit) { name = Path.GetFileNameWithoutExtension(path) };
                    m.SetTexture("_BaseColorMap", col);
                    m.SetColor("_BaseColor", Color.white);
                    if (nrm != null) { m.SetTexture("_NormalMap", nrm); m.SetFloat("_NormalScale", 1f); }
                    if (mask != null)
                    {
                        m.SetTexture("_MaskMap", mask);
                        m.SetFloat("_MetallicRemapMin", 0f); m.SetFloat("_MetallicRemapMax", 0f);
                        m.SetFloat("_SmoothnessRemapMin", 0f); m.SetFloat("_SmoothnessRemapMax", 0.55f);
                        m.SetFloat("_AORemapMin", 0f); m.SetFloat("_AORemapMax", 1f);
                    }
                    else m.SetFloat("_Smoothness", 0.3f);
                    m.SetTextureScale("_BaseColorMap", new Vector2(1.5f, 1.5f));
                    ResetKeywords(m);
                    AssetDatabase.CreateAsset(m, path);
                    touched.Add(m);
                    log?.AppendLine("created " + path);
                }
                if (AssetDatabase.LoadAssetAtPath<Material>(AlienMaterialPath) == null)
                {
                    string dir = $"{IoSourceRoot}/{IoSets[3]}";
                    var col = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/Io_Set04_Color_Periodic_2048.png");
                    var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>($"{dir}/Io_Set04_Normal_Periodic_2048.png");
                    var m = new Material(lit) { name = Path.GetFileNameWithoutExtension(AlienMaterialPath) };
                    if (col != null) m.SetTexture("_BaseColorMap", col);
                    m.SetColor("_BaseColor", new Color(0.09f, 0.1f, 0.13f, 1f));
                    if (nrm != null) { m.SetTexture("_NormalMap", nrm); m.SetFloat("_NormalScale", 0.6f); }
                    m.SetFloat("_Metallic", 0.15f);
                    m.SetFloat("_Smoothness", 0.85f);
                    Color emit = new Color(0.05f, 0.55f, 0.5f) * 1.5f; // slight tint only (an emissive mask comes with the shader pass)
                    m.SetColor("_EmissiveColorLDR", new Color(0.05f, 0.55f, 0.5f));
                    m.SetColor("_EmissiveColor", emit);
                    m.SetColor("_EmissionColor", emit);
                    m.SetFloat("_UseEmissiveIntensity", 0f);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    ResetKeywords(m);
                    AssetDatabase.CreateAsset(m, AlienMaterialPath);
                    touched.Add(m);
                    log?.AppendLine("created " + AlienMaterialPath + " (placeholder until the shader pass)");
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            foreach (Material m in touched) AssetDatabase.SaveAssetIfDirty(m);
        }

        private static void ResetKeywords(Material m)
        {
            Type t = Type.GetType("UnityEditor.Rendering.HighDefinition.HDShaderUtils, Unity.RenderPipelines.HighDefinition.Editor");
            var mi = t?.GetMethod("ResetMaterialKeywords", new[] { typeof(Material) });
            if (mi != null)
            {
                try { mi.Invoke(null, new object[] { m }); return; }
                catch (Exception e) { Debug.LogWarning("HDRP keyword reset failed: " + (e.InnerException ?? e).Message); }
            }
            if (m.GetTexture("_NormalMap") != null) { m.EnableKeyword("_NORMALMAP"); m.EnableKeyword("_NORMALMAP_TANGENT_SPACE"); }
            if (m.GetTexture("_MaskMap") != null) m.EnableKeyword("_MASKMAP");
        }

        // ------------------------------------------------------------------------------------------------
        // Presets

        private struct PresetDef
        {
            public string name; public K style; public Material mat; public string desc; public float passage, nook; public bool custom;
        }

        public static void EnsurePresets(DmRockKit kit, DmRockCombineRecipe recipe, Dictionary<K, DmRockStyle> styles, bool overwrite, System.Text.StringBuilder log = null)
        {
            EnsureFolder(PresetsFolder);
            Material io1 = AssetDatabase.LoadAssetAtPath<Material>(IoMaterialPath(1));
            Material io5 = AssetDatabase.LoadAssetAtPath<Material>(IoMaterialPath(5));
            Material alien = AssetDatabase.LoadAssetAtPath<Material>(AlienMaterialPath);
            var defs = new List<PresetDef>();
            foreach (K kind in (K[])Enum.GetValues(typeof(K)))
                defs.Add(new PresetDef { name = "Style " + Nice(kind), style = kind, mat = kind == K.AlienShards || kind == K.AlienFloating ? alien : null, desc = styles[kind].notes });
            defs.Add(new PresetDef { name = "Io Volcanic Outcrop", style = K.OutcropShelf, mat = io1 != null ? io1 : io5, desc = "Shingled basalt shelf with Io Set01 (basalt / sulfur dust) material.", custom = true });
            defs.Add(new PresetDef { name = "Alien Shard Cluster", style = K.AlienShards, mat = alien, desc = "Curved, tapered shards radiating from 1-3 seeds; dark glass placeholder material.", custom = true, passage = -1 });
            defs.Add(new PresetDef { name = "Eroded Hoodoo Field Piece", style = K.ErodedHoodoo, mat = null, desc = "Capped stems on a shared base with lanes between them.", custom = true });
            defs.Add(new PresetDef { name = "Sharp Slab Spire", style = K.SharpClean, mat = null, desc = "Tall sheared slabs with crisp planar fractures, wedged, +-3 deg grain.", custom = true });
            defs.Add(new PresetDef { name = "Boulder Pile", style = K.BoulderPile, mat = null, desc = "Anchor boulder with nested mids and a debris ring.", custom = true });

            AssetDatabase.StartAssetEditing();
            var touched = new List<DmRockPreset>();
            try
            {
                foreach (PresetDef d in defs)
                {
                    string path = $"{PresetsFolder}/DM_RockPreset_{d.name.Replace(" ", "")}.asset";
                    var p = AssetDatabase.LoadAssetAtPath<DmRockPreset>(path);
                    bool isNew = p == null;
                    if (!isNew && !overwrite) continue;
                    if (isNew) p = ScriptableObject.CreateInstance<DmRockPreset>();
                    p.kit = kit; p.style = styles[d.style]; p.recipe = recipe; p.materialOverride = d.mat; p.description = d.desc;
                    p.features ??= new DmRockFeatureSettings();
                    if (isNew) AssetDatabase.CreateAsset(p, path);
                    else EditorUtility.SetDirty(p);
                    p.Version++;
                    touched.Add(p);
                    log?.AppendLine((isNew ? "created " : "updated ") + path);
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            foreach (DmRockPreset p in touched) AssetDatabase.SaveAssetIfDirty(p);
        }

        public static string Nice(K kind)
        {
            switch (kind)
            {
                case K.BoulderPile: return "Boulder Pile";
                case K.OutcropShelf: return "Outcrop Shelf";
                case K.RubbleScatter: return "Rubble Scatter";
                case K.VolcanicColumnar: return "Volcanic Columnar";
                case K.VolcanicEjecta: return "Volcanic Ejecta";
                case K.VolcanicDome: return "Volcanic Dome";
                case K.ErodedHoodoo: return "Eroded Hoodoo";
                case K.ErodedMesa: return "Eroded Mesa Butte";
                case K.ErodedYardang: return "Eroded Yardang";
                case K.SharpClean: return "Sharp Clean";
                case K.AlienShards: return "Alien Shards";
                case K.AlienFloating: return "Alien Floating Monolith";
                default: return kind.ToString();
            }
        }

        public static List<DmRockPreset> AllPresets()
        {
            return AssetDatabase.FindAssets("t:DmRockPreset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p.Contains("/DM_RockPreset_Style") ? 1 : 0).ThenBy(p => p, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<DmRockPreset>)
                .Where(p => p != null).ToList();
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
