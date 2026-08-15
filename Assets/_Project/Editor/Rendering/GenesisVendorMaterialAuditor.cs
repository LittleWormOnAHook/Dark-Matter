using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Project.EditorTools.Rendering
{
    /// <summary>
    /// Vendor / pack material pink audit + optional per-folder URP→HDRP conversion (dry-run first).
    /// Does not change GraphicsSettings (Phase 6 remains manual).
    /// </summary>
    public static class GenesisVendorMaterialAuditor
    {
        private const string LogPrefix = "[Genesis HDRP VendorAudit]";
        private const string AuditDocPath = "Assets/_Project/Documentation/Architecture/HDRP_Vendor_Material_Audit.md";
        private const string MainScenePath = "Assets/Dark Matter Genesis v1.56.unity";
        private const string MainScenePathAlt = "Assets/_Project/Scenes/Dark Matter Genesis v1.56.unity";
        private const string ProjectRoot = "Assets/_Project";

        private static readonly (string Pack, string Folder)[] PackFolders =
        {
            ("Invector", "Assets/Invector-3rdPersonController"),
            ("Gaia / Procedural Worlds", "Assets/Procedural Worlds"),
            ("Gaia User Data", "Assets/Gaia User Data"),
            ("Malbers", "Assets/Malbers Animations"),
            ("Hovl Studio", "Assets/Hovl Studio"),
            ("PolygonSciFiWorlds", "Assets/PolygonSciFiWorlds"),
            ("PolygonNature", "Assets/PolygonNature"),
            ("PolygonTown", "Assets/PolygonTown"),
            ("QFX", "Assets/QFX"),
            ("Buildings_constructor", "Assets/Buildings_constructor"),
            ("Shift UI", "Assets/Shift - Complete Sci-Fi UI"),
            ("Blink", "Assets/Blink"),
            ("JMO / Cartoon FX", "Assets/JMO Assets"),
            ("Magic Spells & Particles", "Assets/Magic Spells & Particles"),
        };

        private const string UrpLitName = "Universal Render Pipeline/Lit";
        private const string UrpSimpleLitName = "Universal Render Pipeline/Simple Lit";
        private const string UrpUnlitName = "Universal Render Pipeline/Unlit";
        private const string UrpParticlesUnlitName = "Universal Render Pipeline/Particles/Unlit";
        private const string UrpParticlesLitName = "Universal Render Pipeline/Particles/Lit";
        private const string UrpBakedLitName = "Universal Render Pipeline/Baked Lit";
        private const string HdrpLitName = "HDRP/Lit";
        private const string HdrpUnlitName = "HDRP/Unlit";

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Audit Vendor Materials (write report)", false, 30)]
        public static void AuditVendorMaterialsMenu()
        {
            string report = BuildAndWriteAuditReport(showDialog: true);
            Debug.Log($"{LogPrefix}\n{report}");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Convert Folder URP→HDRP (Dry Run)...", false, 31)]
        public static void ConvertFolderDryRunMenu()
        {
            ConvertSelectedOrPromptFolder(dryRun: true, showDialog: true);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Convert Folder URP→HDRP (Apply)...", false, 32)]
        public static void ConvertFolderApplyMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Convert Folder URP→HDRP",
                    "Apply conversion to .mat files under the chosen folder?\n\n" +
                    "Only URP Lit/Unlit/Particles/Baked Lit are converted.\n" +
                    "Custom / Shader Graph / Built-in are skipped.\n" +
                    "Graphics pipeline stays on URP (Phase 6 not run).",
                    "Convert",
                    "Cancel"))
                return;

            ConvertSelectedOrPromptFolder(dryRun: false, showDialog: true);
        }

        /// <summary>MCP entry — writes audit markdown, returns summary text.</summary>
        public static string BuildAndWriteAuditReport(bool showDialog)
        {
            HashSet<string> referencedMats = CollectReferencedMaterialPaths();
            var packRows = new List<PackAuditRow>();

            foreach ((string pack, string folder) in PackFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder) && !Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), folder)))
                {
                    packRows.Add(new PackAuditRow
                    {
                        Pack = pack,
                        Folder = folder,
                        Exists = false,
                    });
                    continue;
                }

                PackAuditRow row = AuditFolder(pack, folder, referencedMats);
                packRows.Add(row);
            }

            // Also summarize _Project remaining non-HDRP for context
            PackAuditRow projectRow = AuditFolder("_Project (context)", ProjectRoot, referencedMats);
            packRows.Add(projectRow);

            string markdown = FormatMarkdown(packRows, referencedMats.Count);
            string dir = Path.GetDirectoryName(AuditDocPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                EnsureFolder(dir);

            File.WriteAllText(AuditDocPath, markdown, Encoding.UTF8);
            AssetDatabase.ImportAsset(AuditDocPath);

            string summary = Summarize(packRows);
            if (showDialog)
                EditorUtility.DisplayDialog("HDRP Vendor Material Audit", summary + $"\n\nWrote {AuditDocPath}", "OK");

            return summary;
        }

        public static string ConvertFolderInternal(string folder, bool dryRun, bool showDialog)
        {
            if (string.IsNullOrEmpty(folder) || !folder.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return "Invalid folder (must be under Assets/).";

            Shader hdrpLit = Shader.Find(HdrpLitName);
            Shader hdrpUnlit = Shader.Find(HdrpUnlitName);
            if (hdrpLit == null || hdrpUnlit == null)
                return "HDRP Lit/Unlit shaders not found.";

            RenderPipelineAsset graphicsBefore = GraphicsSettings.defaultRenderPipeline;
            int qualityBefore = QualitySettings.GetQualityLevel();
            RenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            int convertible = 0;
            int converted = 0;
            int skipped = 0;
            int failed = 0;
            var samples = new List<string>();

            try
            {
                if (!dryRun)
                    AssetDatabase.StartAssetEditing();

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                    {
                        skipped++;
                        continue;
                    }

                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null || mat.shader == null)
                    {
                        skipped++;
                        continue;
                    }

                    string shaderName = mat.shader.name;
                    ConversionKind kind = Classify(shaderName);
                    if (kind == ConversionKind.Skip || kind == ConversionKind.AlreadyHdrp)
                    {
                        skipped++;
                        continue;
                    }

                    convertible++;
                    if (samples.Count < 25)
                        samples.Add($"{path} ← {shaderName}");

                    if (dryRun)
                        continue;

                    try
                    {
                        if (kind == ConversionKind.ToLit)
                            ConvertToHdrpLit(mat, hdrpLit);
                        else
                            ConvertToHdrpUnlit(mat, hdrpUnlit, particlesStyle: kind == ConversionKind.ToUnlitParticles);

                        EditorUtility.SetDirty(mat);
                        converted++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Debug.LogError($"{LogPrefix} Failed {path}: {ex.Message}");
                    }
                }
            }
            finally
            {
                if (!dryRun)
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                }

                // Guard: never leave Phase 6 applied.
                if (urpAsset != null)
                {
                    GraphicsSettings.defaultRenderPipeline = urpAsset;
                    QualitySettings.renderPipeline = urpAsset;
                }
                else if (graphicsBefore != null)
                {
                    GraphicsSettings.defaultRenderPipeline = graphicsBefore;
                }

                if (QualitySettings.GetQualityLevel() != qualityBefore)
                    QualitySettings.SetQualityLevel(qualityBefore, applyExpensiveChanges: false);
            }

            string mode = dryRun ? "DRY RUN" : "APPLIED";
            var sb = new StringBuilder();
            sb.AppendLine($"{mode} folder={folder}");
            sb.AppendLine($"Materials scanned: {guids.Length}");
            sb.AppendLine($"Convertible (URP Lit/Unlit/Particles): {convertible}");
            if (!dryRun)
                sb.AppendLine($"Converted: {converted}  Failed: {failed}  Skipped: {skipped}");
            else
                sb.AppendLine($"(Would convert {convertible}; skipped non-URP/custom: {skipped})");

            if (samples.Count > 0)
            {
                sb.AppendLine("Samples:");
                foreach (string s in samples)
                    sb.AppendLine("  - " + s);
            }

            string text = sb.ToString().TrimEnd();
            Debug.Log($"{LogPrefix} {text}");
            if (showDialog)
                EditorUtility.DisplayDialog($"Convert Folder ({mode})", text, "OK");
            return text;
        }

        private static void ConvertSelectedOrPromptFolder(bool dryRun, bool showDialog)
        {
            string folder = null;
            if (Selection.activeObject != null)
            {
                string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (AssetDatabase.IsValidFolder(path))
                    folder = path;
                else if (!string.IsNullOrEmpty(path))
                    folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            }

            if (string.IsNullOrEmpty(folder))
            {
                folder = EditorUtility.OpenFolderPanel("Select folder under Assets", Application.dataPath, "");
                if (string.IsNullOrEmpty(folder))
                    return;

                folder = folder.Replace('\\', '/');
                int idx = folder.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
                if (idx < 0 && folder.EndsWith("/Assets", StringComparison.OrdinalIgnoreCase))
                    folder = "Assets";
                else if (idx >= 0)
                    folder = folder.Substring(idx + 1);
                else if (folder.StartsWith(Application.dataPath.Replace('\\', '/')))
                    folder = "Assets" + folder.Substring(Application.dataPath.Replace('\\', '/').Length);
                else
                {
                    EditorUtility.DisplayDialog("Convert Folder", "Folder must be inside the project Assets directory.", "OK");
                    return;
                }
            }

            ConvertFolderInternal(folder, dryRun, showDialog);
        }

        private static HashSet<string> CollectReferencedMaterialPaths()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var roots = new List<string>();

            if (File.Exists(MainScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null)
                roots.Add(MainScenePath);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePathAlt) != null)
                roots.Add(MainScenePathAlt);

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectRoot });
            for (int i = 0; i < prefabGuids.Length; i++)
                roots.Add(AssetDatabase.GUIDToAssetPath(prefabGuids[i]));

            // Cap dependency crawl cost — scene first, then a bounded prefab sample.
            int rootLimit = Mathf.Min(roots.Count, 400);
            for (int i = 0; i < rootLimit; i++)
            {
                // Non-recursive for prefabs keeps this interactive; scene uses recursive.
                bool recursive = roots[i].EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
                string[] deps = AssetDatabase.GetDependencies(roots[i], recursive);
                for (int d = 0; d < deps.Length; d++)
                {
                    if (deps[d].EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                        set.Add(deps[d]);
                }
            }

            // Second pass: recursive only for _Project prefabs that already pulled a .mat (expand those).
            var expand = new List<string>();
            for (int i = 0; i < Mathf.Min(roots.Count, 800); i++)
            {
                if (!roots[i].EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!roots[i].StartsWith(ProjectRoot, StringComparison.OrdinalIgnoreCase))
                    continue;
                expand.Add(roots[i]);
            }

            for (int i = 0; i < Mathf.Min(expand.Count, 200); i++)
            {
                string[] deps = AssetDatabase.GetDependencies(expand[i], recursive: true);
                for (int d = 0; d < deps.Length; d++)
                {
                    if (deps[d].EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                        set.Add(deps[d]);
                }
            }

            return set;
        }

        private static PackAuditRow AuditFolder(string pack, string folder, HashSet<string> referencedMats)
        {
            var row = new PackAuditRow { Pack = pack, Folder = folder, Exists = true };
            if (!AssetDatabase.IsValidFolder(folder))
            {
                // Try alternate short names for packs that may live elsewhere
                row.Exists = Directory.Exists(folder);
                if (!row.Exists)
                {
                    row.Exists = false;
                    return row;
                }
            }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
            row.TotalMats = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                    continue;

                row.TotalMats++;
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null)
                {
                    row.BrokenOrMissingShader++;
                    if (referencedMats.Contains(path))
                        row.ReferencedPinkOrBroken++;
                    continue;
                }

                string shaderName = mat.shader.name;
                bool supported = mat.shader.isSupported;
                bool isHdrp = shaderName.StartsWith("HDRP/", StringComparison.Ordinal) ||
                              shaderName.StartsWith("Hidden/HDRP", StringComparison.Ordinal);
                bool isUrp = shaderName.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) ||
                             shaderName.StartsWith("Shader Graphs/", StringComparison.Ordinal);
                bool isBuiltin = shaderName.StartsWith("Standard", StringComparison.Ordinal) ||
                                 shaderName.StartsWith("Legacy Shaders/", StringComparison.Ordinal) ||
                                 shaderName.StartsWith("Particles/", StringComparison.Ordinal) ||
                                 shaderName.StartsWith("Mobile/", StringComparison.Ordinal) ||
                                 shaderName.StartsWith("Skybox/", StringComparison.Ordinal) ||
                                 shaderName.StartsWith("Unlit/", StringComparison.Ordinal) ||
                                 shaderName.StartsWith("Nature/", StringComparison.Ordinal) ||
                                 shaderName.StartsWith("FX/", StringComparison.Ordinal);

                if (isHdrp)
                    row.AlreadyHdrp++;
                else if (Classify(shaderName) != ConversionKind.Skip)
                    row.UrpConvertible++;
                else if (isUrp || isBuiltin || !supported)
                    row.CustomOrBuiltin++;
                else
                    row.CustomOrBuiltin++;

                if (!supported)
                    row.UnsupportedShader++;

                bool referenced = referencedMats.Contains(path);
                if (referenced)
                {
                    row.ReferencedTotal++;
                    if (!supported || (!isHdrp && Classify(shaderName) != ConversionKind.Skip && Classify(shaderName) != ConversionKind.AlreadyHdrp))
                    {
                        // Referenced URP-convertible or unsupported → high priority when on HDRP
                        if (!supported)
                            row.ReferencedPinkOrBroken++;
                        else if (Classify(shaderName) != ConversionKind.Skip)
                            row.ReferencedUrpConvertible++;
                        else
                            row.ReferencedCustom++;
                    }
                    else if (!isHdrp)
                    {
                        row.ReferencedCustom++;
                    }
                    else
                    {
                        row.ReferencedHdrp++;
                    }
                }
            }

            row.Severity = ComputeSeverity(row);
            row.RecommendedAction = Recommend(row);
            return row;
        }

        private static string ComputeSeverity(PackAuditRow row)
        {
            if (!row.Exists)
                return "N/A";
            if (row.ReferencedPinkOrBroken > 0)
                return "Critical";
            if (row.ReferencedUrpConvertible > 50)
                return "High";
            if (row.ReferencedUrpConvertible > 0 || row.ReferencedCustom > 20)
                return "Medium";
            if (row.UrpConvertible > 200)
                return "Low (catalog)";
            return "Defer";
        }

        private static string Recommend(PackAuditRow row)
        {
            if (!row.Exists)
                return "Pack folder missing — skip.";
            if (row.Pack.StartsWith("_Project", StringComparison.Ordinal))
                return "Already largely converted; finish custom/Shader Graph leftovers only.";
            if (row.Severity == "Critical")
                return "Convert referenced subset now (folder dry-run → apply only referenced paths).";
            if (row.Severity == "High")
                return "Convert gameplay-referenced URP mats via folder tool; leave unused catalog.";
            if (row.Severity == "Medium")
                return "Convert critical referenced URP mats; leave custom/VFX until Phase 6.";
            if (row.Severity.StartsWith("Low", StringComparison.Ordinal))
                return "Leave until Phase 6 bulk pass; do not blind-convert thousands.";
            return "Leave until Phase 6.";
        }

        private static string FormatMarkdown(List<PackAuditRow> rows, int referencedCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# HDRP Vendor Material Audit");
            sb.AppendLine();
            sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd} UTC");
            sb.AppendLine();
            sb.AppendLine("## Scope");
            sb.AppendLine();
            sb.AppendLine("- Graphics / Quality **High remain URP** (Phase 6 not run).");
            sb.AppendLine("- Counts prioritize materials referenced by `Dark Matter Genesis v1.56` and `Assets/_Project` prefabs.");
            sb.AppendLine($"- Referenced `.mat` dependency set size: **{referencedCount}**.");
            sb.AppendLine("- Tools: `Tools/Dark Matter Genesis/HDRP/Audit Vendor Materials`, `Convert Folder URP→HDRP (Dry Run|Apply)`.");
            sb.AppendLine();
            sb.AppendLine("## Custom / _Project shaders (prep status)");
            sb.AppendLine();
            sb.AppendLine("| Asset | Choice | Notes |");
            sb.AppendLine("|-------|--------|-------|");
            sb.AppendLine("| `Project/EnemyDisintegrate` | Dual SubShader URP+HDRP | Same shader name; `Shader.Find` unchanged. |");
            sb.AppendLine("| `Project/EnemyDissolveSmoke` | Dual SubShader URP+HDRP | Transparent ForwardOnly on HDRP. |");
            sb.AppendLine("| `Project/SmokeParticle` | Dual SubShader URP+HDRP | Replaces Legacy Particles on `SmokeParticle.mat`. |");
            sb.AppendLine("| `Custom/ScannerPostProcess` | Dual SubShader + HDRP Custom Pass | URP: blit / OnRenderImage; HDRP: `ScannerHdrpCustomPass`. |");
            sb.AppendLine("| `Custom/ScannerPostProcessPBR` | Dual SubShader | Overlay scanline unlit (not full PBR). |");
            sb.AppendLine("| Needle Plant `glTF-pbrMetallicRoughness` | Package dual-target Shader Graph | Already has UniversalTarget + HDTarget — no local fork. |");
            sb.AppendLine();
            sb.AppendLine("## Pack summary");
            sb.AppendLine();
            sb.AppendLine("| Pack | Exists | Total .mat | URP convertible | Custom/Built-in | HDRP | Unsupported | Ref total | Ref URP | Ref custom | Ref pink/broken | Severity | Action |");
            sb.AppendLine("|------|--------|------------|-----------------|-----------------|------|--------------|-----------|---------|------------|-----------------|----------|--------|");

            foreach (PackAuditRow r in rows)
            {
                if (!r.Exists)
                {
                    sb.AppendLine($"| {r.Pack} | no | — | — | — | — | — | — | — | — | — | N/A | {Escape(r.RecommendedAction)} |");
                    continue;
                }

                sb.AppendLine(
                    $"| {r.Pack} | yes | {r.TotalMats} | {r.UrpConvertible} | {r.CustomOrBuiltin} | {r.AlreadyHdrp} | {r.UnsupportedShader} | {r.ReferencedTotal} | {r.ReferencedUrpConvertible} | {r.ReferencedCustom} | {r.ReferencedPinkOrBroken} | {r.Severity} | {Escape(r.RecommendedAction)} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Recommended Phase 6 leftovers");
            sb.AppendLine();
            sb.AppendLine("1. Bulk-convert remaining vendor URP catalogs (Gaia / Invector) only after playable scene is on HDRP.");
            sb.AppendLine("2. Replace or reauthor pack-specific custom shaders (Malbers, Hovl, QFX, WarFX) that are not Lit/Unlit.");
            sb.AppendLine("3. Wire scanner Custom Pass volumes into gameplay cameras; retire `OnRenderImage` path.");
            sb.AppendLine("4. Rebake lighting / reflection probes on `Dark Matter Genesis v1.56`.");
            sb.AppendLine("5. PPT / cinematic HDR / optional RT — still held.");
            sb.AppendLine();
            sb.AppendLine("## Guardrails");
            sb.AppendLine();
            sb.AppendLine("- Do **not** blind-convert entire Gaia or Invector trees in one click.");
            sb.AppendLine("- Prefer dry-run → apply on a pack subfolder that is actually referenced.");
            sb.AppendLine("- Keep `PC_RPAsset` on Graphics until Phase 6 menu is explicitly run.");
            sb.AppendLine();
            return sb.ToString();
        }

        private static string Escape(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace("|", "/");

        private static string Summarize(List<PackAuditRow> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Vendor material audit complete.");
            foreach (PackAuditRow r in rows.Where(x => x.Exists).OrderByDescending(x => x.ReferencedUrpConvertible + x.ReferencedPinkOrBroken * 10))
            {
                sb.AppendLine(
                    $"{r.Pack}: mats={r.TotalMats}, refURP={r.ReferencedUrpConvertible}, refPink={r.ReferencedPinkOrBroken}, severity={r.Severity}");
            }

            return sb.ToString().TrimEnd();
        }

        private static void EnsureFolder(string assetFolder)
        {
            string[] parts = assetFolder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        private enum ConversionKind
        {
            Skip,
            AlreadyHdrp,
            ToLit,
            ToUnlit,
            ToUnlitParticles,
        }

        private static ConversionKind Classify(string shaderName)
        {
            if (string.IsNullOrEmpty(shaderName))
                return ConversionKind.Skip;

            if (shaderName.StartsWith("HDRP/", StringComparison.Ordinal) ||
                shaderName.StartsWith("Hidden/HDRP", StringComparison.Ordinal))
                return ConversionKind.AlreadyHdrp;

            if (shaderName == UrpLitName || shaderName == UrpSimpleLitName || shaderName == UrpBakedLitName)
                return ConversionKind.ToLit;

            if (shaderName == UrpUnlitName)
                return ConversionKind.ToUnlit;

            if (shaderName == UrpParticlesUnlitName || shaderName == UrpParticlesLitName)
                return ConversionKind.ToUnlitParticles;

            return ConversionKind.Skip;
        }

        // --- Conversion helpers (mirrors GenesisUrpToHdrpMaterialConverter) ---

        private static void ConvertToHdrpLit(Material mat, Shader hdrpLit)
        {
            Texture baseMap = GetTex(mat, "_BaseMap", "_MainTex");
            Vector2 baseScale = GetTexScale(mat, "_BaseMap", "_MainTex");
            Vector2 baseOffset = GetTexOffset(mat, "_BaseMap", "_MainTex");
            Color baseColor = GetColor(mat, "_BaseColor", "_Color", Color.white);
            Texture bumpMap = GetTex(mat, "_BumpMap");
            float bumpScale = GetFloat(mat, "_BumpScale", 1f);
            Texture metallicGloss = GetTex(mat, "_MetallicGlossMap");
            float metallic = GetFloat(mat, "_Metallic", 0f);
            float smoothness = GetFloat(mat, "_Smoothness", GetFloat(mat, "_Glossiness", 0.5f));
            float occlusionStrength = GetFloat(mat, "_OcclusionStrength", 1f);
            Texture emissionMap = GetTex(mat, "_EmissionMap");
            Color emissionColor = GetColor(mat, "_EmissionColor", null, Color.black);
            bool hasEmission = mat.IsKeywordEnabled("_EMISSION") || emissionMap != null || emissionColor.maxColorComponent > 0.0001f;
            float alphaClip = GetFloat(mat, "_AlphaClip", 0f);
            float cutoff = GetFloat(mat, "_Cutoff", 0.5f);
            float surface = GetFloat(mat, "_Surface", 0f);
            float blend = GetFloat(mat, "_Blend", 0f);
            float cull = GetFloat(mat, "_Cull", 2f);
            bool doubleSided = mat.doubleSidedGI || Mathf.Approximately(cull, 0f);

            mat.shader = hdrpLit;
            if (baseMap != null)
            {
                mat.SetTexture("_BaseColorMap", baseMap);
                mat.SetTextureScale("_BaseColorMap", baseScale);
                mat.SetTextureOffset("_BaseColorMap", baseOffset);
            }

            mat.SetColor("_BaseColor", baseColor);
            if (bumpMap != null)
            {
                mat.SetTexture("_NormalMap", bumpMap);
                mat.SetFloat("_NormalScale", bumpScale);
            }

            if (metallicGloss != null)
            {
                mat.SetTexture("_MaskMap", metallicGloss);
                mat.SetFloat("_Metallic", 1f);
                mat.SetFloat("_Smoothness", 1f);
                mat.SetFloat("_SmoothnessRemapMax", smoothness);
                mat.SetFloat("_AORemapMin", 1f - occlusionStrength);
            }
            else
            {
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smoothness);
            }

            if (hasEmission)
            {
                if (emissionMap != null)
                    mat.SetTexture("_EmissiveColorMap", emissionMap);
                HDMaterial.SetUseEmissiveIntensity(mat, false);
                HDMaterial.SetEmissiveColor(mat, emissionColor);
            }

            if (alphaClip > 0.5f)
            {
                HDMaterial.SetAlphaClipping(mat, true);
                HDMaterial.SetAlphaCutoff(mat, cutoff);
            }

            bool transparent = surface > 0.5f;
            HDMaterial.SetSurfaceType(mat, transparent);
            if (transparent && mat.HasProperty("_BlendMode"))
            {
                float hdrpBlend = blend switch { 1f => 4f, 2f => 1f, _ => 0f };
                mat.SetFloat("_BlendMode", hdrpBlend);
            }

            if (mat.HasProperty("_DoubleSidedEnable"))
                mat.SetFloat("_DoubleSidedEnable", doubleSided ? 1f : 0f);
            mat.doubleSidedGI = doubleSided;
            if (mat.HasProperty("_MaterialID"))
                mat.SetFloat("_MaterialID", 1f);

            mat.SetShaderPassEnabled("DistortionVectors", false);
            mat.SetShaderPassEnabled("TransparentDepthPrepass", false);
            mat.SetShaderPassEnabled("TransparentDepthPostpass", false);
            mat.SetShaderPassEnabled("TransparentBackface", false);
            mat.SetShaderPassEnabled("MOTIONVECTORS", false);
            HDMaterial.ValidateMaterial(mat);
        }

        private static void ConvertToHdrpUnlit(Material mat, Shader hdrpUnlit, bool particlesStyle)
        {
            Texture baseMap = GetTex(mat, "_BaseMap", "_MainTex");
            Vector2 baseScale = GetTexScale(mat, "_BaseMap", "_MainTex");
            Vector2 baseOffset = GetTexOffset(mat, "_BaseMap", "_MainTex");
            Color baseColor = GetColor(mat, "_BaseColor", "_Color", Color.white);
            float surface = GetFloat(mat, "_Surface", 0f);
            float blend = GetFloat(mat, "_Blend", 0f);
            float cull = GetFloat(mat, "_Cull", 2f);
            bool doubleSided = mat.doubleSidedGI || Mathf.Approximately(cull, 0f) || particlesStyle;

            mat.shader = hdrpUnlit;
            if (baseMap != null)
            {
                mat.SetTexture("_UnlitColorMap", baseMap);
                mat.SetTextureScale("_UnlitColorMap", baseScale);
                mat.SetTextureOffset("_UnlitColorMap", baseOffset);
            }

            mat.SetColor("_UnlitColor", baseColor);
            bool transparent = surface > 0.5f || particlesStyle;
            HDMaterial.SetSurfaceType(mat, transparent);
            if (transparent && mat.HasProperty("_BlendMode"))
            {
                float hdrpBlend = blend switch { 1f => 4f, 2f => 1f, _ => particlesStyle ? 1f : 0f };
                mat.SetFloat("_BlendMode", hdrpBlend);
            }

            if (mat.HasProperty("_DoubleSidedEnable"))
                mat.SetFloat("_DoubleSidedEnable", doubleSided ? 1f : 0f);
            mat.doubleSidedGI = doubleSided;
            HDMaterial.ValidateMaterial(mat);
        }

        private static Texture GetTex(Material mat, string primary, string fallback = null)
        {
            if (mat.HasProperty(primary))
            {
                Texture t = mat.GetTexture(primary);
                if (t != null) return t;
            }

            if (fallback != null && mat.HasProperty(fallback))
                return mat.GetTexture(fallback);
            return null;
        }

        private static Vector2 GetTexScale(Material mat, string primary, string fallback = null)
        {
            if (mat.HasProperty(primary)) return mat.GetTextureScale(primary);
            if (fallback != null && mat.HasProperty(fallback)) return mat.GetTextureScale(fallback);
            return Vector2.one;
        }

        private static Vector2 GetTexOffset(Material mat, string primary, string fallback = null)
        {
            if (mat.HasProperty(primary)) return mat.GetTextureOffset(primary);
            if (fallback != null && mat.HasProperty(fallback)) return mat.GetTextureOffset(fallback);
            return Vector2.zero;
        }

        private static Color GetColor(Material mat, string primary, string fallback, Color defaultColor)
        {
            if (primary != null && mat.HasProperty(primary)) return mat.GetColor(primary);
            if (fallback != null && mat.HasProperty(fallback)) return mat.GetColor(fallback);
            return defaultColor;
        }

        private static float GetFloat(Material mat, string name, float defaultValue) =>
            mat.HasProperty(name) ? mat.GetFloat(name) : defaultValue;

        private sealed class PackAuditRow
        {
            public string Pack;
            public string Folder;
            public bool Exists;
            public int TotalMats;
            public int UrpConvertible;
            public int CustomOrBuiltin;
            public int AlreadyHdrp;
            public int UnsupportedShader;
            public int BrokenOrMissingShader;
            public int ReferencedTotal;
            public int ReferencedUrpConvertible;
            public int ReferencedCustom;
            public int ReferencedHdrp;
            public int ReferencedPinkOrBroken;
            public string Severity;
            public string RecommendedAction;
        }
    }
}
