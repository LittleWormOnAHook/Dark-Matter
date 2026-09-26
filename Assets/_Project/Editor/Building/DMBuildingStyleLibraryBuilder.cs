using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Project.Building;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// Creates and maintains the building style libraries (Stone, Iron, Silicate) in Resources/Building/Styles.
    /// Also migrates the retired DM_BuildingLibrary / DM_BuildingMaterialLibrary assets into the Stone style.
    /// </summary>
    public static class DMBuildingStyleLibraryBuilder
    {
        public const string PrefabLibraryRoot = "Assets/_Project/Prefabs/Buildings/Library";
        const string LegacyPieceLibraryPath = "Assets/_Project/Resources/Building/DM_BuildingLibrary.asset";
        const string LegacyMaterialLibraryPath = "Assets/_Project/Resources/Building/DM_BuildingMaterialLibrary.asset";
        const string StoneGlassPath = PrefabLibraryRoot + "/Stone/Materials/WindowGlass.mat";
        const string BuiltTintPath = PrefabLibraryRoot + "/Materials/DM_BuiltTint.mat";
        const string StoneRoughPath = PrefabLibraryRoot + "/Stone/Materials/StoneRough.mat";
        const string GhostProfilePath = "Assets/_Project/Resources/Building/DM_BuildingGhostProfile.asset";

        struct FinishSeed
        {
            public string Id;
            public string DisplayName;
            public string File;
            public Color Color;
            public float Metallic;
            public float Smoothness;

            public FinishSeed(string id, string displayName, string file, Color color, float metallic, float smoothness)
            {
                Id = id;
                DisplayName = displayName;
                File = file;
                Color = color;
                Metallic = metallic;
                Smoothness = smoothness;
            }
        }

        sealed class StyleSeed
        {
            public string Id;
            public string Name;
            public int Order;
            public Color Accent;
            public string CostItemPath;
            public string[] CostNames;
            public FinishSeed[] Finishes;
            /// <summary>Door file inside the style Materials folder. Empty means the last finish is the door.</summary>
            public string DoorFile;
        }

        static readonly StyleSeed[] Seeds =
        {
            new StyleSeed
            {
                Id = "stone",
                Name = "Stone",
                Order = 0,
                Accent = new Color(0.75f, 0.72f, 0.68f, 1f),
                CostItemPath = "Assets/_Project/Data/Items/Consumables/Rock.asset",
                CostNames = new[] { "Rock", "Stone" },
                Finishes = new[]
                {
                    new FinishSeed("stone_rough", "Rough Stone", "StoneRough", new Color(0.45f, 0.42f, 0.38f, 1f), 0f, 0.2f),
                    new FinishSeed("stone_cut", "Cut Stone", "StoneCut", new Color(0.62f, 0.58f, 0.52f, 1f), 0f, 0.3f),
                },
                DoorFile = "Door",
            },
            new StyleSeed
            {
                Id = "iron",
                Name = "Iron",
                Order = 1,
                Accent = new Color(0.55f, 0.58f, 0.62f, 1f),
                CostItemPath = "Assets/_Project/Data/Items/Resources/Mining/Iron Ore.asset",
                CostNames = new[] { "Iron Ore" },
                Finishes = new[]
                {
                    new FinishSeed("iron_plate", "Iron Plate", "IronPlate", new Color(0.36f, 0.37f, 0.39f, 1f), 0.8f, 0.45f),
                    new FinishSeed("iron_brushed", "Brushed Iron", "IronBrushed", new Color(0.56f, 0.57f, 0.59f, 1f), 0.9f, 0.65f),
                    new FinishSeed("iron_trim", "Iron Trim", "IronTrim", new Color(0.26f, 0.19f, 0.15f, 1f), 0.6f, 0.3f),
                },
            },
            new StyleSeed
            {
                Id = "silicate",
                Name = "Silicate",
                Order = 2,
                Accent = new Color(0.55f, 0.74f, 0.84f, 1f),
                CostItemPath = "Assets/_Project/Data/Items/Resources/Mining/Silicate Ore.asset",
                CostNames = new[] { "Silicate Ore" },
                Finishes = new[]
                {
                    new FinishSeed("silicate_smooth", "Smooth Silicate", "SilicateSmooth", new Color(0.78f, 0.80f, 0.82f, 1f), 0f, 0.7f),
                    new FinishSeed("silicate_crystal", "Crystal Silicate", "SilicateCrystal", new Color(0.55f, 0.72f, 0.82f, 1f), 0.1f, 0.9f),
                    new FinishSeed("silicate_trim", "Silicate Trim", "SilicateTrim", new Color(0.35f, 0.40f, 0.46f, 1f), 0.2f, 0.5f),
                },
            },
        };

        public static string StyleAssetPath(string styleName)
        {
            return DMBuildingStyleLibrary.AssetFolder + "/DM_BuildingStyle_" + styleName + ".asset";
        }

        public static string StyleFolderName(DMBuildingStyleLibrary style)
        {
            string name = style != null && !string.IsNullOrEmpty(style.displayName) ? style.displayName : "Style";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c.ToString(), string.Empty);
            return name.Replace(" ", string.Empty);
        }

        public static string StyleRoot(DMBuildingStyleLibrary style)
        {
            return PrefabLibraryRoot + "/" + StyleFolderName(style);
        }

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Ensure Style Libraries (Stone, Iron, Silicate)")]
        public static void EnsureStylesMenu()
        {
            List<DMBuildingStyleLibrary> styles = EnsureStyles();
            Debug.Log("[DM Building Library] Styles ready: " + styles.Count + " in " + DMBuildingStyleLibrary.AssetFolder);
        }

        public static List<DMBuildingStyleLibrary> EnsureStyles()
        {
            EnsureFolder(DMBuildingStyleLibrary.AssetFolder);
            var result = new List<DMBuildingStyleLibrary>();
            DMBuildingStyleLibrary stone = null;
            for (int i = 0; i < Seeds.Length; i++)
            {
                DMBuildingStyleLibrary style = EnsureStyle(Seeds[i]);
                if (style == null)
                    continue;
                result.Add(style);
                if (style.styleId == "stone")
                    stone = style;
            }

            if (stone != null)
                MigrateLegacy(stone);

            EnsureBuiltTintMaterial();
            AssetDatabase.SaveAssets();
            DMBuildingStyles.Invalidate();
            return result;
        }

        /// <summary>
        /// 0926-tint: textured lit material for built pieces whose style has no finish (copied from Stone rough, base
        /// colour white so Finished mesh tints it). Assigned to the ghost profile's Built material when that is empty.
        /// </summary>
        static void EnsureBuiltTintMaterial()
        {
            Material tint = AssetDatabase.LoadAssetAtPath<Material>(BuiltTintPath);
            if (tint == null)
            {
                EnsureFolder(PrefabLibraryRoot + "/Materials");
                Material source = AssetDatabase.LoadAssetAtPath<Material>(StoneRoughPath);
                Shader lit = Shader.Find("HDRP/Lit");
                if (source == null && lit == null)
                    return;
                tint = source != null ? new Material(source) : new Material(lit);
                tint.name = "DM_BuiltTint";
                if (tint.HasProperty("_BaseColor"))
                    tint.SetColor("_BaseColor", Color.white);
                AssetDatabase.CreateAsset(tint, BuiltTintPath);
                Debug.Log("[DM Building Library] Created " + BuiltTintPath);
            }

            DMBuildingGhostProfile profile = AssetDatabase.LoadAssetAtPath<DMBuildingGhostProfile>(GhostProfilePath);
            if (profile != null && profile.builtMaterial == null)
            {
                profile.builtMaterial = tint;
                EditorUtility.SetDirty(profile);
            }
        }

        static DMBuildingStyleLibrary EnsureStyle(StyleSeed seed)
        {
            string path = StyleAssetPath(seed.Name);
            DMBuildingStyleLibrary style = AssetDatabase.LoadAssetAtPath<DMBuildingStyleLibrary>(path);
            bool created = false;
            if (style == null)
            {
                style = ScriptableObject.CreateInstance<DMBuildingStyleLibrary>();
                style.styleId = seed.Id;
                style.displayName = seed.Name;
                style.order = seed.Order;
                style.accent = seed.Accent;
                style.partIdPrefix = seed.Id + "_";
                style.costItemNames = new List<string>(seed.CostNames);
                AssetDatabase.CreateAsset(style, path);
                created = true;
            }

            if (style.costItem == null && !string.IsNullOrEmpty(seed.CostItemPath))
                style.costItem = AssetDatabase.LoadAssetAtPath<ItemData>(seed.CostItemPath);
            if (style.costItemNames == null || style.costItemNames.Count == 0)
                style.costItemNames = new List<string>(seed.CostNames);
            if (style.kit == null)
                style.kit = new DMBuildingKitSettings();

            EnsureSeedMaterials(style, seed);
            EnsureKitParts(style);
            EditorUtility.SetDirty(style);
            if (created)
                Debug.Log("[DM Building Library] Created " + path);
            return style;
        }

        static void EnsureSeedMaterials(DMBuildingStyleLibrary style, StyleSeed seed)
        {
            string folder = StyleRoot(style) + "/Materials";
            EnsureFolder(folder);
            if (style.finishes == null)
                style.finishes = new List<DMBuildingMaterialVariant>();

            Material last = null;
            for (int i = 0; i < seed.Finishes.Length; i++)
            {
                FinishSeed finish = seed.Finishes[i];
                Material material = EnsureMaterial(folder + "/" + finish.File + ".mat", finish.Color, false, finish.Metallic, finish.Smoothness);
                last = material;
                DMBuildingMaterialVariant variant = style.FindFinish(finish.Id);
                if (variant == null)
                {
                    variant = new DMBuildingMaterialVariant { id = finish.Id, displayName = finish.DisplayName };
                    style.finishes.Add(variant);
                }

                if (variant.finishedMaterial == null)
                    variant.finishedMaterial = material;
            }

            if (style.doorMaterial == null)
            {
                style.doorMaterial = string.IsNullOrEmpty(seed.DoorFile)
                    ? last
                    : EnsureMaterial(folder + "/" + seed.DoorFile + ".mat", new Color(0.32f, 0.24f, 0.16f, 1f), false, 0f, 0.3f);
            }

            if (style.glassMaterial == null)
                style.glassMaterial = EnsureMaterial(StoneGlassPath, new Color(0.75f, 0.88f, 0.92f, 0.35f), true, 0f, 0.9f);
        }

        /// <summary>Every kit shape gets a part row. Prefabs are linked when the kit builder has made them.</summary>
        public static void EnsureKitParts(DMBuildingStyleLibrary style)
        {
            if (style.parts == null)
                style.parts = new List<DMBuildingPartEntry>();
            string prefix = PrefixOf(style);
            string root = StyleRoot(style);
            for (int i = 0; i < DMBuildingCatalog.KitTemplate.Length; i++)
            {
                DMBuildingCatalog.KitPartTemplate template = DMBuildingCatalog.KitTemplate[i];
                string id = prefix + template.Suffix;
                DMBuildingPartEntry part = style.FindPart(id);
                if (part == null)
                {
                    part = NewKitPart(id, template);
                    style.parts.Add(part);
                }

                if (part.prefab == null)
                    part.prefab = FindPrefab(root, id);
            }
        }

        public static DMBuildingPartEntry NewKitPart(string id, DMBuildingCatalog.KitPartTemplate template)
        {
            return new DMBuildingPartEntry
            {
                id = id,
                displayName = template.DisplayName,
                shape = template.Shape,
                category = DMBuildingCatalog.DefaultCategory(template.Shape),
                cost = template.Cost,
                enabled = true,
                applyStyleFinish = true,
            };
        }

        public static string PrefixOf(DMBuildingStyleLibrary style)
        {
            return string.IsNullOrEmpty(style.partIdPrefix) ? style.styleId + "_" : style.partIdPrefix;
        }

        static GameObject FindPrefab(string root, string id)
        {
            if (!AssetDatabase.IsValidFolder(root))
                return null;
            string[] guids = AssetDatabase.FindAssets(id + " t:Prefab", new[] { root });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (Path.GetFileNameWithoutExtension(path) == id)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            return null;
        }

        // ---------------------------------------------------------------- legacy migration

        static void MigrateLegacy(DMBuildingStyleLibrary stone)
        {
            bool hadPieces = File.Exists(LegacyPieceLibraryPath);
            bool hadMaterials = File.Exists(LegacyMaterialLibraryPath);
            if (!hadPieces && !hadMaterials)
                return;

            if (hadPieces)
            {
                string yaml = File.ReadAllText(LegacyPieceLibraryPath);
                foreach (Match match in Regex.Matches(yaml, @"-\s*id:\s*(\S+)\s*\r?\n\s*prefab:\s*\{[^}]*guid:\s*([0-9a-f]{32})"))
                {
                    DMBuildingPartEntry part = stone.FindPart(match.Groups[1].Value);
                    if (part == null || part.prefab != null)
                        continue;
                    part.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(match.Groups[2].Value));
                }

                // Kit detail sizes used to live on the ghost profile. Carry them over once.
                if (File.Exists(DMBuildingGhostProfile.AssetPath))
                {
                    string profile = File.ReadAllText(DMBuildingGhostProfile.AssetPath);
                    DMBuildingKitSettings kit = stone.kit ?? (stone.kit = new DMBuildingKitSettings());
                    kit.windowWidthMeters = ReadFloat(profile, "kitWindowWidthMeters", kit.windowWidthMeters);
                    kit.windowHeightMeters = ReadFloat(profile, "kitWindowHeightMeters", kit.windowHeightMeters);
                    kit.windowSillMeters = ReadFloat(profile, "kitWindowSillMeters", kit.windowSillMeters);
                    kit.glassThicknessMeters = ReadFloat(profile, "kitGlassThicknessMeters", kit.glassThicknessMeters);
                    kit.passageWidthMeters = ReadFloat(profile, "kitPassageWidthMeters", kit.passageWidthMeters);
                    kit.passageHeightMeters = ReadFloat(profile, "kitPassageHeightMeters", kit.passageHeightMeters);
                    kit.hatchOpeningMeters = ReadFloat(profile, "kitHatchOpeningMeters", kit.hatchOpeningMeters);
                    kit.stairSteps = Mathf.RoundToInt(ReadFloat(profile, "kitStairSteps", kit.stairSteps));
                    kit.railingPosts = Mathf.RoundToInt(ReadFloat(profile, "kitRailingPosts", kit.railingPosts));
                }
            }

            if (hadMaterials)
            {
                string yaml = File.ReadAllText(LegacyMaterialLibraryPath);
                string[] blocks = Regex.Split(yaml, @"\r?\n\s*-\s*id:\s*");
                for (int i = 1; i < blocks.Length; i++)
                {
                    string block = blocks[i];
                    string id = block.Split('\n')[0].Trim();
                    Match guid = Regex.Match(block, @"finishedMaterial:\s*\{[^}]*guid:\s*([0-9a-f]{32})");
                    Material material = guid.Success
                        ? AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid.Groups[1].Value))
                        : null;
                    if (id == "door")
                    {
                        if (material != null)
                            stone.doorMaterial = material;
                        continue;
                    }

                    if (id == "window_glass")
                    {
                        if (material != null)
                            stone.glassMaterial = material;
                        continue;
                    }

                    DMBuildingMaterialVariant variant = stone.FindFinish(id);
                    if (variant == null)
                    {
                        Match name = Regex.Match(block, @"displayName:\s*(.+)");
                        variant = new DMBuildingMaterialVariant { id = id, displayName = name.Success ? name.Groups[1].Value.Trim() : id };
                        stone.finishes.Add(variant);
                    }

                    if (material != null)
                        variant.finishedMaterial = material;
                    Match overrideTint = Regex.Match(block, @"overrideGhostTint:\s*(\d)");
                    if (overrideTint.Success)
                        variant.overrideGhostTint = overrideTint.Groups[1].Value == "1";
                }
            }

            EditorUtility.SetDirty(stone);
            AssetDatabase.SaveAssets();
            if (hadPieces)
                AssetDatabase.DeleteAsset(LegacyPieceLibraryPath);
            if (hadMaterials)
                AssetDatabase.DeleteAsset(LegacyMaterialLibraryPath);
            Debug.Log("[DM Building Library] Moved DM_BuildingLibrary and DM_BuildingMaterialLibrary into " + AssetDatabase.GetAssetPath(stone) + " and removed the old assets.");
        }

        static float ReadFloat(string yaml, string key, float fallback)
        {
            Match match = Regex.Match(yaml, @"\n\s*" + key + @":\s*([-0-9.eE]+)");
            return match.Success && float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? value
                : fallback;
        }

        // ---------------------------------------------------------------- new styles, materials, icons

        /// <summary>Creates an empty style (Studio "+ New Style"). It starts with every kit shape and three HDRP/Lit finishes.</summary>
        public static DMBuildingStyleLibrary CreateStyle(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return null;
            string id = Regex.Replace(displayName.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
            if (string.IsNullOrEmpty(id) || DMBuildingStyles.Find(id) != null)
                return null;

            var seed = new StyleSeed
            {
                Id = id,
                Name = displayName.Trim(),
                Order = DMBuildingStyles.All.Count,
                Accent = new Color(0.75f, 0.45f, 0.62f, 1f),
                CostNames = new[] { displayName.Trim() },
                Finishes = new[]
                {
                    new FinishSeed(id + "_base", displayName.Trim(), StyleFileStem(displayName) + "Base", new Color(0.5f, 0.5f, 0.52f, 1f), 0f, 0.4f),
                    new FinishSeed(id + "_fine", "Fine " + displayName.Trim(), StyleFileStem(displayName) + "Fine", new Color(0.64f, 0.64f, 0.66f, 1f), 0.2f, 0.6f),
                    new FinishSeed(id + "_trim", displayName.Trim() + " Trim", StyleFileStem(displayName) + "Trim", new Color(0.3f, 0.3f, 0.34f, 1f), 0.3f, 0.4f),
                },
            };
            EnsureFolder(DMBuildingStyleLibrary.AssetFolder);
            DMBuildingStyleLibrary style = EnsureStyle(seed);
            AssetDatabase.SaveAssets();
            DMBuildingStyles.Invalidate();
            return style;
        }

        static string StyleFileStem(string displayName)
        {
            return Regex.Replace(displayName ?? "Style", @"[^A-Za-z0-9]", string.Empty);
        }

        /// <summary>Adds one more HDRP/Lit finish to a style (Studio "+ Finish").</summary>
        public static DMBuildingMaterialVariant AddFinish(DMBuildingStyleLibrary style)
        {
            if (style == null)
                return null;
            string folder = StyleRoot(style) + "/Materials";
            EnsureFolder(folder);
            int n = (style.finishes != null ? style.finishes.Count : 0) + 1;
            string file = StyleFileStem(style.displayName) + "Finish" + n;
            Material material = EnsureMaterial(folder + "/" + file + ".mat", style.accent, false, 0f, 0.4f);
            var variant = new DMBuildingMaterialVariant { id = style.styleId + "_finish" + n, displayName = style.displayName + " Finish " + n, finishedMaterial = material };
            style.finishes.Add(variant);
            EditorUtility.SetDirty(style);
            return variant;
        }

        struct IconJob
        {
            public DMBuildingStyleLibrary Style;
            public int Index;
            public string Folder;
        }

        static readonly List<IconJob> IconJobs = new List<IconJob>();
        static readonly HashSet<DMBuildingStyleLibrary> IconDirtyStyles = new HashSet<DMBuildingStyleLibrary>();
        static double iconDeadline;
        static int iconBaked;
        static int iconSkipped;

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Bake Missing Part Icons (All Styles)")]
        public static void BakeAllMissingIconsMenu()
        {
            int queued = 0;
            foreach (DMBuildingStyleLibrary style in DMBuildingStyles.All)
                queued += BakeIcons(style, false);
            Debug.Log("[DM Building Library] Queued " + queued + " part icons across " + DMBuildingStyles.All.Count + " styles.");
        }

        /// <summary>
        /// Queues part prefab thumbnails to be written as PNG icons (Library/&lt;Style&gt;/Icons) and assigned.
        /// Unity builds asset previews over several editor frames, so the bake finishes on EditorApplication.update.
        /// Returns the number of icons queued.
        /// </summary>
        public static int BakeIcons(DMBuildingStyleLibrary style, bool overwrite)
        {
            if (style == null || style.parts == null)
                return 0;
            string folder = StyleRoot(style) + "/Icons";
            EnsureFolder(folder);
            AssetPreview.SetPreviewTextureCacheSize(Mathf.Max(512, IconJobs.Count + style.parts.Count + 64));
            int queued = 0;
            for (int i = 0; i < style.parts.Count; i++)
            {
                DMBuildingPartEntry part = style.parts[i];
                if (part == null || part.prefab == null || (!overwrite && part.icon != null))
                    continue;
                bool already = false;
                for (int j = 0; j < IconJobs.Count; j++)
                    already |= IconJobs[j].Style == style && IconJobs[j].Index == i;
                if (already)
                    continue;
                AssetPreview.GetAssetPreview(part.prefab);
                IconJobs.Add(new IconJob { Style = style, Index = i, Folder = folder });
                queued++;
            }

            if (queued > 0)
            {
                iconDeadline = EditorApplication.timeSinceStartup + 60.0;
                EditorApplication.update -= PumpIconJobs;
                EditorApplication.update += PumpIconJobs;
            }

            return queued;
        }

        static void PumpIconJobs()
        {
            bool timedOut = EditorApplication.timeSinceStartup > iconDeadline;
            for (int i = IconJobs.Count - 1; i >= 0; i--)
            {
                IconJob job = IconJobs[i];
                DMBuildingPartEntry part = job.Style != null && job.Style.parts != null && job.Index < job.Style.parts.Count
                    ? job.Style.parts[job.Index]
                    : null;
                if (part == null || part.prefab == null)
                {
                    IconJobs.RemoveAt(i);
                    continue;
                }

                Texture2D preview = AssetPreview.GetAssetPreview(part.prefab);
                if (preview == null)
                {
                    if (!timedOut)
                        continue;
                    iconSkipped++;
                    IconJobs.RemoveAt(i);
                    continue;
                }

                try
                {
                    WriteIcon(job, part, preview);
                    iconBaked++;
                    // 0926: a cold preview cache after an editor restart is slow; keep waiting while icons still arrive.
                    iconDeadline = EditorApplication.timeSinceStartup + 60.0;
                }
                catch (System.Exception ex)
                {
                    iconSkipped++;
                    Debug.LogWarning("[DM Building Library] Icon bake failed for " + part.id + ": " + ex.Message);
                }

                IconJobs.RemoveAt(i);
            }

            if (IconJobs.Count > 0)
                return;

            EditorApplication.update -= PumpIconJobs;
            foreach (DMBuildingStyleLibrary style in IconDirtyStyles)
            {
                if (style != null)
                    EditorUtility.SetDirty(style);
            }

            IconDirtyStyles.Clear();
            AssetDatabase.SaveAssets();
            DMBuildingStyles.Invalidate();
            Debug.Log("[DM Building Library] Icon bake finished: " + iconBaked + " baked, " + iconSkipped + " skipped (no preview).");
            iconBaked = 0;
            iconSkipped = 0;
        }

        static void WriteIcon(IconJob job, DMBuildingPartEntry part, Texture2D preview)
        {
            string path = job.Folder + "/" + part.id + ".png";
            RenderTexture rt = RenderTexture.GetTemporary(preview.width, preview.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(preview, rt);
            RenderTexture.active = rt;
            var readable = new Texture2D(preview.width, preview.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, preview.width, preview.height), 0, 0);
            readable.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, readable.EncodeToPNG());
            Object.DestroyImmediate(readable);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = true;
                importer.SaveAndReimport();
            }

            part.icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            IconDirtyStyles.Add(job.Style);
        }

        static Material EnsureMaterial(string path, Color color, bool transparent, float metallic, float smoothness)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            Shader shader = Shader.Find("HDRP/Lit") ?? Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Color");
            if (shader == null)
                return null;
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_UnlitColor"))
                material.SetColor("_UnlitColor", color);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            material.color = color;
            if (transparent && material.HasProperty("_SurfaceType"))
            {
                material.SetFloat("_SurfaceType", 1f);
                if (material.HasProperty("_BlendMode"))
                    material.SetFloat("_BlendMode", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}