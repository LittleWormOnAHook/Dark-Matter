using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Project.EditorTools.Rendering
{
    /// <summary>
    /// Converts URP materials under Assets/_Project to HDRP Lit / Unlit equivalents.
    /// Does not change GraphicsSettings / QualitySettings (Phase 6 remains manual).
    /// </summary>
    public static class GenesisUrpToHdrpMaterialConverter
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string LogPrefix = "[Genesis HDRP MatConvert]";

        private const string UrpLitName = "Universal Render Pipeline/Lit";
        private const string UrpSimpleLitName = "Universal Render Pipeline/Simple Lit";
        private const string UrpUnlitName = "Universal Render Pipeline/Unlit";
        private const string UrpParticlesUnlitName = "Universal Render Pipeline/Particles/Unlit";
        private const string UrpParticlesLitName = "Universal Render Pipeline/Particles/Lit";
        private const string UrpBakedLitName = "Universal Render Pipeline/Baked Lit";

        private const string HdrpLitName = "HDRP/Lit";
        private const string HdrpUnlitName = "HDRP/Unlit";

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Inventory _Project Material Shaders", false, 20)]
        public static void InventoryProjectMaterialsMenu()
        {
            InventoryProjectMaterials(showDialog: true);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Convert _Project Materials URP→HDRP", false, 21)]
        public static void ConvertProjectMaterialsMenu()
        {
            ConvertProjectMaterialsInternal(showDialog: true);
        }

        /// <summary>MCP / automation entry — no modal dialogs.</summary>
        public static ConversionReport ConvertProjectMaterialsInternal(bool showDialog)
        {
            ConversionReport report = RunConversion();
            string summary = report.ToSummary();
            Debug.Log($"{LogPrefix} {summary}");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Convert _Project Materials URP→HDRP",
                    summary + "\n\nGraphics pipeline left on URP (Phase 6 not run).\n" +
                    "Custom shaders / Shader Graphs were skipped — convert those separately.",
                    "OK");
            }

            return report;
        }

        public static string InventoryProjectMaterials(bool showDialog)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { ProjectRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null)
                    continue;

                string key = mat.shader.name;
                if (!counts.TryGetValue(key, out int n))
                    n = 0;
                counts[key] = n + 1;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"{LogPrefix} Material inventory under {ProjectRoot} ({guids.Length} assets):");
            List<string> keys = new List<string>(counts.Keys);
            keys.Sort(StringComparer.Ordinal);
            for (int i = 0; i < keys.Count; i++)
                sb.AppendLine($"  {counts[keys[i]],4}  {keys[i]}");

            string text = sb.ToString();
            Debug.Log(text);
            if (showDialog)
                EditorUtility.DisplayDialog("_Project Material Inventory", text, "OK");
            return text;
        }

        private static ConversionReport RunConversion()
        {
            Shader hdrpLit = Shader.Find(HdrpLitName);
            Shader hdrpUnlit = Shader.Find(HdrpUnlitName);
            if (hdrpLit == null || hdrpUnlit == null)
            {
                Debug.LogError($"{LogPrefix} HDRP shaders not found (Lit={hdrpLit != null}, Unlit={hdrpUnlit != null}). Is HDRP package imported?");
                return new ConversionReport { Failed = 1 };
            }

            // Snapshot Graphics + Quality so we can restore if anything touches them.
            RenderPipelineAsset graphicsBefore = GraphicsSettings.defaultRenderPipeline;
            int qualityBefore = QualitySettings.GetQualityLevel();
            RenderPipelineAsset qualityRpBefore = QualitySettings.renderPipeline;
            RenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");

            var report = new ConversionReport();
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { ProjectRoot });

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    // Skip embedded model materials (FBX/GLB) — only convert standalone .mat assets.
                    if (!path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                    {
                        report.Skipped++;
                        report.SkippedPaths.Add(path + " (non-.mat asset)");
                        continue;
                    }

                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null)
                    {
                        report.Failed++;
                        report.FailedPaths.Add(path + " (load failed)");
                        continue;
                    }

                    if (mat.shader == null)
                    {
                        report.Skipped++;
                        report.SkippedPaths.Add(path + " (null shader)");
                        continue;
                    }

                    string shaderName = mat.shader.name;
                    ConversionKind kind = Classify(shaderName);
                    if (kind == ConversionKind.AlreadyHdrp)
                    {
                        report.Skipped++;
                        report.SkippedPaths.Add(path + " (already HDRP)");
                        continue;
                    }

                    if (kind == ConversionKind.Skip)
                    {
                        report.Skipped++;
                        report.SkippedPaths.Add(path + $" ({shaderName})");
                        continue;
                    }

                    try
                    {
                        if (kind == ConversionKind.ToLit)
                            ConvertToHdrpLit(mat, hdrpLit);
                        else
                            ConvertToHdrpUnlit(mat, hdrpUnlit, particlesStyle: kind == ConversionKind.ToUnlitParticles);

                        EditorUtility.SetDirty(mat);
                        report.Converted++;
                        report.ConvertedPaths.Add(path + $" ← {shaderName}");
                    }
                    catch (Exception ex)
                    {
                        report.Failed++;
                        report.FailedPaths.Add($"{path} ({ex.Message})");
                        Debug.LogError($"{LogPrefix} Failed {path}: {ex}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();

                // Never leave Phase 6 half-applied. Prefer PC_RPAsset when present.
                RenderPipelineAsset restorePipeline = urpAsset != null ? urpAsset : graphicsBefore;
                if (restorePipeline != null)
                {
                    GraphicsSettings.defaultRenderPipeline = restorePipeline;
                    QualitySettings.renderPipeline = restorePipeline;
                }
                else if (GraphicsSettings.defaultRenderPipeline != graphicsBefore)
                {
                    GraphicsSettings.defaultRenderPipeline = graphicsBefore;
                }

                if (QualitySettings.GetQualityLevel() != qualityBefore)
                    QualitySettings.SetQualityLevel(qualityBefore, applyExpensiveChanges: false);

                // Keep active High/playable tier on URP even if a prior snapshot was wrong.
                if (urpAsset != null)
                    QualitySettings.renderPipeline = urpAsset;
                else if (QualitySettings.renderPipeline != qualityRpBefore)
                    QualitySettings.renderPipeline = qualityRpBefore;

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return report;
        }

        private static ConversionKind Classify(string shaderName)
        {
            if (string.IsNullOrEmpty(shaderName))
                return ConversionKind.Skip;

            if (shaderName.StartsWith("HDRP/", StringComparison.Ordinal) ||
                shaderName.StartsWith("Hidden/HDRP", StringComparison.Ordinal))
                return ConversionKind.AlreadyHdrp;

            if (shaderName == UrpLitName ||
                shaderName == UrpSimpleLitName ||
                shaderName == UrpBakedLitName)
                return ConversionKind.ToLit;

            if (shaderName == UrpUnlitName)
                return ConversionKind.ToUnlit;

            if (shaderName == UrpParticlesUnlitName || shaderName == UrpParticlesLitName)
                return ConversionKind.ToUnlitParticles;

            return ConversionKind.Skip;
        }

        private static void ConvertToHdrpLit(Material mat, Shader hdrpLit)
        {
            Texture baseMap = GetTex(mat, "_BaseMap", "_MainTex");
            Vector2 baseScale = GetTexScale(mat, "_BaseMap", "_MainTex");
            Vector2 baseOffset = GetTexOffset(mat, "_BaseMap", "_MainTex");
            Color baseColor = GetColor(mat, "_BaseColor", "_Color", Color.white);

            Texture bumpMap = GetTex(mat, "_BumpMap");
            float bumpScale = GetFloat(mat, "_BumpScale", 1f);

            Texture metallicGloss = GetTex(mat, "_MetallicGlossMap");
            Texture occlusion = GetTex(mat, "_OcclusionMap");
            float metallic = GetFloat(mat, "_Metallic", 0f);
            float smoothness = GetFloat(mat, "_Smoothness", GetFloat(mat, "_Glossiness", 0.5f));
            float occlusionStrength = GetFloat(mat, "_OcclusionStrength", 1f);

            Texture emissionMap = GetTex(mat, "_EmissionMap");
            Color emissionColor = GetColor(mat, "_EmissionColor", null, Color.black);
            bool hasEmission = mat.IsKeywordEnabled("_EMISSION") ||
                               (emissionMap != null) ||
                               emissionColor.maxColorComponent > 0.0001f;

            Texture heightMap = GetTex(mat, "_ParallaxMap");
            float alphaClip = GetFloat(mat, "_AlphaClip", 0f);
            float cutoff = GetFloat(mat, "_Cutoff", 0.5f);
            float surface = GetFloat(mat, "_Surface", 0f);
            float blend = GetFloat(mat, "_Blend", 0f);
            float cull = GetFloat(mat, "_Cull", 2f); // 0=Off,1=Front,2=Back
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
                mat.SetFloat("_NormalMapSpace", 0f); // Tangent space
            }

            // URP MetallicGlossMap packing (R metallic, G AO, A smoothness) matches HDRP MaskMap closely.
            if (metallicGloss != null)
            {
                mat.SetTexture("_MaskMap", metallicGloss);
                mat.SetFloat("_Metallic", 1f);
                mat.SetFloat("_Smoothness", 1f);
                mat.SetFloat("_SmoothnessRemapMin", 0f);
                mat.SetFloat("_SmoothnessRemapMax", smoothness);
                mat.SetFloat("_AORemapMin", 1f - occlusionStrength);
                mat.SetFloat("_AORemapMax", 1f);
            }
            else
            {
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smoothness);
                if (occlusion != null)
                {
                    // No combined mask — leave MaskMap empty; AO map alone isn't a full MaskMap.
                    mat.SetFloat("_AORemapMin", 1f - occlusionStrength);
                    mat.SetFloat("_AORemapMax", 1f);
                }
            }

            if (heightMap != null)
                mat.SetTexture("_HeightMap", heightMap);

            if (hasEmission)
            {
                if (emissionMap != null)
                    mat.SetTexture("_EmissiveColorMap", emissionMap);

                HDMaterial.SetUseEmissiveIntensity(mat, false);
                HDMaterial.SetEmissiveColor(mat, emissionColor);
                mat.EnableKeyword("_EMISSIVE_COLOR_MAP");
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
                // URP: 0 Alpha, 1 Premultiply, 2 Additive, 3 Multiply
                // HDRP BlendMode: Alpha=0, Additive=1, Premultiply=4 (approx — validate remaps)
                float hdrpBlend = blend switch
                {
                    1f => 4f, // Premultiply
                    2f => 1f, // Additive
                    _ => 0f,  // Alpha
                };
                mat.SetFloat("_BlendMode", hdrpBlend);
            }

            if (mat.HasProperty("_DoubleSidedEnable"))
                mat.SetFloat("_DoubleSidedEnable", doubleSided ? 1f : 0f);
            mat.doubleSidedGI = doubleSided;

            // Material ID Metallic (standard lit)
            if (mat.HasProperty("_MaterialID"))
                mat.SetFloat("_MaterialID", 1f);

            ApplyHdrpPassDefaults(mat);
            HDMaterial.ValidateMaterial(mat);
        }

        private static void ConvertToHdrpUnlit(Material mat, Shader hdrpUnlit, bool particlesStyle)
        {
            Texture baseMap = GetTex(mat, "_BaseMap", "_MainTex");
            Vector2 baseScale = GetTexScale(mat, "_BaseMap", "_MainTex");
            Vector2 baseOffset = GetTexOffset(mat, "_BaseMap", "_MainTex");
            Color baseColor = GetColor(mat, "_BaseColor", "_Color", Color.white);

            Texture emissionMap = GetTex(mat, "_EmissionMap");
            Color emissionColor = GetColor(mat, "_EmissionColor", null, Color.black);
            bool hasEmission = mat.IsKeywordEnabled("_EMISSION") ||
                               (emissionMap != null) ||
                               emissionColor.maxColorComponent > 0.0001f;

            float alphaClip = GetFloat(mat, "_AlphaClip", 0f);
            float cutoff = GetFloat(mat, "_Cutoff", 0.5f);
            float surface = GetFloat(mat, "_Surface", 0f);
            float blend = GetFloat(mat, "_Blend", 0f);
            float cull = GetFloat(mat, "_Cull", 2f);
            bool doubleSided = mat.doubleSidedGI || Mathf.Approximately(cull, 0f) || particlesStyle;

            // Particles often use additive transparent.
            if (particlesStyle && surface < 0.5f && mat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"))
                surface = 1f;

            mat.shader = hdrpUnlit;

            if (baseMap != null)
            {
                mat.SetTexture("_UnlitColorMap", baseMap);
                mat.SetTextureScale("_UnlitColorMap", baseScale);
                mat.SetTextureOffset("_UnlitColorMap", baseOffset);
            }

            mat.SetColor("_UnlitColor", baseColor);

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

            bool transparent = surface > 0.5f || particlesStyle;
            HDMaterial.SetSurfaceType(mat, transparent);
            if (transparent && mat.HasProperty("_BlendMode"))
            {
                float hdrpBlend = blend switch
                {
                    1f => 4f,
                    2f => 1f,
                    _ => particlesStyle ? 1f : 0f, // default particles → Additive
                };
                mat.SetFloat("_BlendMode", hdrpBlend);
            }

            if (mat.HasProperty("_DoubleSidedEnable"))
                mat.SetFloat("_DoubleSidedEnable", doubleSided ? 1f : 0f);
            mat.doubleSidedGI = doubleSided;

            ApplyHdrpPassDefaults(mat);
            HDMaterial.ValidateMaterial(mat);
        }

        private static void ApplyHdrpPassDefaults(Material mat)
        {
            // Match typical HDRP opaque defaults; ValidateMaterial also adjusts these.
            mat.SetShaderPassEnabled("DistortionVectors", false);
            mat.SetShaderPassEnabled("TransparentDepthPrepass", false);
            mat.SetShaderPassEnabled("TransparentDepthPostpass", false);
            mat.SetShaderPassEnabled("TransparentBackface", false);
            mat.SetShaderPassEnabled("MOTIONVECTORS", false);
        }

        private static Texture GetTex(Material mat, string primary, string fallback = null)
        {
            if (mat.HasProperty(primary))
            {
                Texture t = mat.GetTexture(primary);
                if (t != null)
                    return t;
            }

            if (fallback != null && mat.HasProperty(fallback))
                return mat.GetTexture(fallback);

            return null;
        }

        private static Vector2 GetTexScale(Material mat, string primary, string fallback = null)
        {
            if (mat.HasProperty(primary))
                return mat.GetTextureScale(primary);
            if (fallback != null && mat.HasProperty(fallback))
                return mat.GetTextureScale(fallback);
            return Vector2.one;
        }

        private static Vector2 GetTexOffset(Material mat, string primary, string fallback = null)
        {
            if (mat.HasProperty(primary))
                return mat.GetTextureOffset(primary);
            if (fallback != null && mat.HasProperty(fallback))
                return mat.GetTextureOffset(fallback);
            return Vector2.zero;
        }

        private static Color GetColor(Material mat, string primary, string fallback, Color defaultColor)
        {
            if (primary != null && mat.HasProperty(primary))
                return mat.GetColor(primary);
            if (fallback != null && mat.HasProperty(fallback))
                return mat.GetColor(fallback);
            return defaultColor;
        }

        private static float GetFloat(Material mat, string name, float defaultValue)
        {
            return mat.HasProperty(name) ? mat.GetFloat(name) : defaultValue;
        }

        private enum ConversionKind
        {
            Skip,
            AlreadyHdrp,
            ToLit,
            ToUnlit,
            ToUnlitParticles,
        }

        public sealed class ConversionReport
        {
            public int Converted;
            public int Skipped;
            public int Failed;
            public readonly List<string> ConvertedPaths = new List<string>();
            public readonly List<string> SkippedPaths = new List<string>();
            public readonly List<string> FailedPaths = new List<string>();

            public string ToSummary()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"Converted: {Converted}  Skipped: {Skipped}  Failed: {Failed}");
                if (FailedPaths.Count > 0)
                {
                    sb.AppendLine("Failures:");
                    for (int i = 0; i < Mathf.Min(FailedPaths.Count, 20); i++)
                        sb.AppendLine("  - " + FailedPaths[i]);
                }

                if (SkippedPaths.Count > 0)
                {
                    sb.AppendLine($"Skipped ({SkippedPaths.Count}):");
                    for (int i = 0; i < Mathf.Min(SkippedPaths.Count, 30); i++)
                        sb.AppendLine("  - " + SkippedPaths[i]);
                    if (SkippedPaths.Count > 30)
                        sb.AppendLine($"  … +{SkippedPaths.Count - 30} more");
                }

                return sb.ToString().TrimEnd();
            }
        }
    }
}
