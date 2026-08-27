using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine;

namespace Project.EditorTools.Rendering
{
    /// <summary>
    /// Remaps Nature Manufacture L.V.E Built-in Standard lava shaders to the pack's HDRP
    /// equivalents (NM_Lit_* cover lava, NM Lava River Vertex Color*, NM_Volcano Smoke).
    /// Does not hand-port Standard lighting — uses vendor HDRP shaders already in the pack.
    /// </summary>
    public static class NmLveLavaHdrpMaterialConverter
    {
        private const string LogPrefix = "[NM L.V.E HDRP]";
        private const string LveRoot = "Assets/NatureManufacture Assets/L.V.E- Lava and Volcano Environment";

        // Built-in Standard / Specular cover lava → NM HDRP Lit cover lava
        private const string StandardUvFreeGuid = "3ef0cb5a2319771478ac44698ebb6ef9";
        private const string StandardMetallicGuid = "4e059242ef121004cb8a66e0fd817e9c";
        private const string StandardMetallicCutoutGuid = "4d02fc391a20121438e42845fcb3e7c5";
        private const string StandardMetallicFrozenRiverGuid = "c8416289398e93647aa94743c97939a8";
        private const string SpecularUvFreeGuid = "55ba1fa7c44f29242ae2c0f1fcb0d494";
        private const string SpecularLavaGuid = "9b6be94e5e62b844491511eaec06d8c1";
        private const string SpecularCutoutGuid = "b52ea6be2e7c8954ab7ca92c21d90837";

        private const string HdrpFullTriplanarGuid = "ce11364fad77f7c4f83f61151424ddaa";
        private const string HdrpTopCoverGuid = "b5e906738b549f14f86d395bb883ce5d";

        // Built-in river → NM HDRP river (non-tess / tess pairs)
        private const string BuiltInFlowmapGuid = "951e551eb5a54334ab498637e84fe777";
        private const string BuiltInFlowmapCheapGuid = "a078bba5a980e3447a3b8f0ef47227d2";
        private const string BuiltInFrozenGuid = "c0dfe728864ec2447a8672eec7e6bdd0";
        private const string BuiltInVertexColorGuid = "d5aff4267d23a1344aaf15b1633043dc";

        private const string HdrpFlowmapGuid = "bc527e0817d82d4489cd35462d4e68b0";
        private const string HdrpFlowmapTessGuid = "224b880707fc836458b721e4d5a86eb9";
        private const string HdrpFlowmapCheapGuid = "f8ee384eec2163d44a375a36b7b4726a";
        private const string HdrpFlowmapCheapTessGuid = "e3c21255a9f890b4c97d777b47f9178e";
        private const string HdrpFrozenGuid = "32a65825244304d4ca196f347ee6ca97";
        private const string HdrpFrozenTessGuid = "7e3717c777e06bf4da27fec119bd3465";

        // Built-in smoke → NM HDRP smoke
        private const string BuiltInVolcanoSmokeGuid = "83adc62d3a7ed22439f449a848283984";
        private const string HdrpVolcanoSmokeGuid = "fe2bc8314ee86e740978756ecc0b7275";

        private static readonly Dictionary<string, string> FixedCoverMap = new Dictionary<string, string>
        {
            { StandardUvFreeGuid, HdrpFullTriplanarGuid },
            { SpecularUvFreeGuid, HdrpFullTriplanarGuid },
            { StandardMetallicFrozenRiverGuid, HdrpFullTriplanarGuid },
            { StandardMetallicGuid, HdrpTopCoverGuid },
            { StandardMetallicCutoutGuid, HdrpTopCoverGuid },
            { SpecularLavaGuid, HdrpTopCoverGuid },
            { SpecularCutoutGuid, HdrpTopCoverGuid },
            { BuiltInVertexColorGuid, HdrpFrozenGuid },
            { BuiltInVolcanoSmokeGuid, HdrpVolcanoSmokeGuid },
        };

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Convert L.V.E Lava Standard→HDRP (Dry Run)", false, 35)]
        public static void ConvertDryRunMenu()
        {
            ConversionReport report = ConvertAll(dryRun: true);
            EditorUtility.DisplayDialog("L.V.E Lava Standard→HDRP (Dry Run)", report.ToSummary(), "OK");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Convert L.V.E Lava Standard→HDRP (Apply)", false, 36)]
        public static void ConvertApplyMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Convert L.V.E Lava Standard→HDRP",
                    "Remap materials under the Nature Manufacture L.V.E pack from Built-in Standard " +
                    "lava shaders to the vendor HDRP equivalents already shipped in the pack.\n\n" +
                    "Standard_Metalic_* → NM_Lit_* cover lava\n" +
                    "Lava River Built-in → NM HDRP Lava River\n" +
                    "Vulcano Smoke → NM_Volcano Smoke\n\n" +
                    "Continue?",
                    "Convert",
                    "Cancel"))
                return;

            ConversionReport report = ConvertAll(dryRun: false);
            EditorUtility.DisplayDialog("L.V.E Lava Standard→HDRP", report.ToSummary(), "OK");
        }

        /// <summary>MCP / automation entry.</summary>
        public static ConversionReport ConvertAll(bool dryRun)
        {
            var report = new ConversionReport { DryRun = dryRun };
            if (!AssetDatabase.IsValidFolder(LveRoot))
            {
                report.Failed = 1;
                report.Errors.Add($"Folder not found: {LveRoot}");
                return report;
            }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { LveRoot });
            report.Scanned = guids.Length;

            if (!dryRun)
                AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null || mat.shader == null)
                    {
                        report.Skipped++;
                        continue;
                    }

                    string shaderGuid = GetShaderGuid(mat.shader);
                    if (string.IsNullOrEmpty(shaderGuid))
                    {
                        report.Skipped++;
                        continue;
                    }

                    if (!TryResolveTargetShader(shaderGuid, mat.name, out Shader targetShader, out string reason))
                    {
                        if (IsBuiltInLveShader(shaderGuid))
                            report.SkippedBuiltInRemaining++;
                        else
                            report.Skipped++;
                        continue;
                    }

                    report.Convertible++;
                    if (report.Samples.Count < 40)
                        report.Samples.Add($"{path}  {mat.shader.name} → {targetShader.name} ({reason})");

                    if (dryRun)
                        continue;

                    try
                    {
                        MigrateProperties(mat, shaderGuid);
                        mat.shader = targetShader;
                        ApplyHdrpFixups(mat, shaderGuid);
                        EditorUtility.SetDirty(mat);
                        report.Converted++;
                    }
                    catch (Exception ex)
                    {
                        report.Failed++;
                        report.Errors.Add($"{path}: {ex.Message}");
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
            }

            Debug.Log($"{LogPrefix} {report.ToSummary()}");
            return report;
        }

        private static bool TryResolveTargetShader(
            string sourceGuid,
            string materialName,
            out Shader targetShader,
            out string reason)
        {
            targetShader = null;
            reason = null;

            if (FixedCoverMap.TryGetValue(sourceGuid, out string fixedTargetGuid))
            {
                targetShader = LoadShaderByGuid(fixedTargetGuid);
                reason = "cover/smoke";
                return targetShader != null;
            }

            if (TryResolveRiverPair(sourceGuid, materialName, out string riverTargetGuid, out reason))
            {
                targetShader = LoadShaderByGuid(riverTargetGuid);
                return targetShader != null;
            }

            return false;
        }

        private static bool TryResolveRiverPair(
            string sourceGuid,
            string materialName,
            out string targetGuid,
            out string reason)
        {
            targetGuid = null;
            reason = null;
            bool tess = materialName.IndexOf("Tesseled", StringComparison.OrdinalIgnoreCase) >= 0
                        || materialName.IndexOf("Tessell", StringComparison.OrdinalIgnoreCase) >= 0;

            switch (sourceGuid)
            {
                case BuiltInFlowmapGuid:
                    targetGuid = tess ? HdrpFlowmapTessGuid : HdrpFlowmapGuid;
                    reason = tess ? "river flowmap tess" : "river flowmap";
                    return true;
                case BuiltInFlowmapCheapGuid:
                    targetGuid = tess ? HdrpFlowmapCheapTessGuid : HdrpFlowmapCheapGuid;
                    reason = tess ? "river flowmap cheap tess" : "river flowmap cheap";
                    return true;
                case BuiltInFrozenGuid:
                    targetGuid = tess ? HdrpFrozenTessGuid : HdrpFrozenGuid;
                    reason = tess ? "river frozen tess" : "river frozen";
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBuiltInLveShader(string guid)
        {
            return FixedCoverMap.ContainsKey(guid)
                   || guid == BuiltInFlowmapGuid
                   || guid == BuiltInFlowmapCheapGuid
                   || guid == BuiltInFrozenGuid;
        }

        private static void MigrateProperties(Material mat, string sourceGuid)
        {
            switch (sourceGuid)
            {
                case StandardUvFreeGuid:
                case SpecularUvFreeGuid:
                case StandardMetallicFrozenRiverGuid:
                    MigrateTriplanarCover(mat);
                    break;
                case StandardMetallicGuid:
                case StandardMetallicCutoutGuid:
                case SpecularLavaGuid:
                case SpecularCutoutGuid:
                    MigrateTopCover(mat);
                    break;
            }
        }

        private static void MigrateTriplanarCover(Material mat)
        {
            CopyColor(mat, "_BottomColor", "_BaseColor");
            CopyTexture(mat, "_BottomAlbedo_Sm", "_BaseColorMap");
            CopyTexture(mat, "_BottomNormal", "_BaseNormalMap");
            CopyTexture(mat, "_BottomMetalicRAmbientOcclusionGEmissionA", "_BaseMaskMap");
            CopyFloat(mat, "_BottomNormalScale", "_BaseNormalScale");
            CopyFloat(mat, "_BottomMetalicPower", "_BaseMetallic");
            CopyFloat(mat, "_BottomAmbientOcclusionPower", "_BaseAORemapMax");
            CopyFloat(mat, "_BottomSmoothnessPower", "_BaseSmoothnessRemapMax");
            CopyFloat(mat, "_BottomLavaEmissionMaskIntensivity", "_BaseEmissionMaskIntensivity");
            CopyFloat(mat, "_BottomLavaEmissionMaskTreshold", "_BaseEmissionMaskTreshold");
            CopyFloat(mat, "_BottomTiling", "_BaseTilingOffset", component: 0);
            CopyFloat(mat, "_BottomTriplanarFalloff", "_BaseTriplanarThreshold");

            CopyColor(mat, "_TopColor", "_CoverBaseColor");
            CopyTexture(mat, "_TopAlbedo_Sm", "_CoverBaseColorMap");
            CopyTexture(mat, "_TopNormal", "_CoverNormalMap");
            CopyTexture(mat, "_TopMetalicRAmbientOcclusionGEmissionA", "_CoverMaskMap");
            CopyFloat(mat, "_TopNormalScale", "_CoverNormalScale");
            CopyFloat(mat, "_TopMetalicPower", "_CoverMetallic");
            CopyFloat(mat, "_TopAmbientOcclusionPower", "_CoverAORemapMax");
            CopyFloat(mat, "_TopSmoothnessPower", "_CoverSmoothnessRemapMax");
            CopyFloat(mat, "_TopLavaEmissionMaskIntensivity", "_CoverEmissionMaskIntensivity");
            CopyFloat(mat, "_TopLavaEmissionMaskTreshold", "_CoverEmissionMaskTreshold");
            CopyFloat(mat, "_TopTiling", "_CoverTilingOffset", component: 0);
            CopyFloat(mat, "_TopTriplanarFalloff", "_CoverTriplanarThreshold");

            CopyTexture(mat, "_BumpMap", "_ShapeNormalMap");
            CopyTexture(mat, "_ShapeAmbientOcclusionG", "_ShapeAO");
            CopyFloat(mat, "_ShapeNormalScale", "_shapeNormalScale");
            CopyFloat(mat, "_ShapeAmbientOcclusionPower", "_ShapeAORemapMax");
            CopyFloat(mat, "_CoverMaxAngle", "_Cover_Max_Angle");

            CopyTexture(mat, "_LavaNoiseR", "_Noise");
            CopyFloat(mat, "_LavaNoisePower", "_EmissionNoisePower");
            CopyVector(mat, "_LavaNoiseSpeed", "_NoiseSpeed");
            CopyColor(mat, "_LavaEmissionColor", "_LavaEmissionColor");
            CopyColor(mat, "_RimColor", "_RimColor");
            CopyFloat(mat, "_RimLightPower", "_RimLightPower");
        }

        private static void MigrateTopCover(Material mat)
        {
            CopyColor(mat, "_Color", "_BaseColor");
            CopyTexture(mat, "_MainTex", "_BaseColorMap");
            CopyTexture(mat, "_BumpMap", "_BaseNormalMap");
            CopyTexture(mat, "_MetalicRAmbientOcclusionGEmissionA", "_BaseMaskMap");
            CopyFloat(mat, "_MetalicPower", "_BaseMetallic");
            CopyFloat(mat, "_SmothnessPower", "_BaseSmoothnessRemapMax");
            CopyFloat(mat, "_AmbientOcclusionPower", "_BaseAORemapMax");
            CopyFloat(mat, "_LavaEmissionMaskIntensivity", "_BaseEmissionMaskIntensivity");
            CopyFloat(mat, "_LavaEmissionMaskTreshold", "_BaseEmissionMaskTreshold");

            CopyTexture(mat, "_DetailMapAlbedoRNyGNxA", "_DetailMap");
            CopyFloat(mat, "_DetailAlbedoPower", "_DetailAlbedoScale");
            CopyFloat(mat, "_DetailNormalScale", "_DetailNormalScale");
            CopyFloat(mat, "_DetailSmoothnessPower", "_DetailSmoothnessScale");

            CopyTexture(mat, "_LavaNoiseR", "_Noise");
            CopyFloat(mat, "_LavaNoisePower", "_EmissionNoisePower");
            CopyVector(mat, "_LavaNoiseSpeed", "_NoiseSpeed");
            CopyColor(mat, "_LavaEmissionColor", "_LavaEmissionColor");
            CopyColor(mat, "_RimColor", "_RimColor");
            CopyFloat(mat, "_RimLightPower", "_RimLightPower");
        }

        private static void ApplyHdrpFixups(Material mat, string sourceGuid)
        {
            if (sourceGuid == StandardMetallicCutoutGuid || sourceGuid == SpecularCutoutGuid)
            {
                if (mat.HasProperty("_AlphaCutoffEnable"))
                    mat.SetFloat("_AlphaCutoffEnable", 1f);
                HDMaterial.SetAlphaClipping(mat, true);
                if (mat.HasProperty("_Cutoff"))
                    HDMaterial.SetAlphaCutoff(mat, mat.GetFloat("_Cutoff"));
            }

            if (mat.HasProperty("_USEDYNAMICCOVERTSTATICMASKF"))
                mat.SetFloat("_USEDYNAMICCOVERTSTATICMASKF", 1f);

            mat.SetShaderPassEnabled("MOTIONVECTORS", false);
            mat.SetShaderPassEnabled("TransparentBackface", false);
            mat.SetShaderPassEnabled("TransparentDepthPrepass", false);
            mat.SetShaderPassEnabled("TransparentDepthPostpass", false);
            mat.SetShaderPassEnabled("RayTracingPrepass", false);

            HDMaterial.ValidateMaterial(mat);
        }

        private static Shader LoadShaderByGuid(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Shader>(path);
        }

        private static string GetShaderGuid(Shader shader)
        {
            if (shader == null)
                return null;
            string path = AssetDatabase.GetAssetPath(shader);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
        }

        private static void CopyTexture(Material mat, string from, string to)
        {
            if (!mat.HasProperty(from) || !mat.HasProperty(to))
                return;
            Texture tex = mat.GetTexture(from);
            if (tex != null && mat.GetTexture(to) == null)
                mat.SetTexture(to, tex);
        }

        private static void CopyColor(Material mat, string from, string to)
        {
            if (!mat.HasProperty(from) || !mat.HasProperty(to))
                return;
            Color value = mat.GetColor(from);
            if (mat.GetColor(to) == default && value != default)
                mat.SetColor(to, value);
        }

        private static void CopyFloat(Material mat, string from, string to, int component = -1)
        {
            if (!mat.HasProperty(from) || !mat.HasProperty(to))
                return;
            float value = mat.GetFloat(from);
            if (component >= 0 && mat.HasProperty(to))
            {
                Vector4 vec = mat.GetVector(to);
                vec[component] = value;
                mat.SetVector(to, vec);
                return;
            }

            if (Mathf.Approximately(mat.GetFloat(to), 0f) && !Mathf.Approximately(value, 0f))
                mat.SetFloat(to, value);
        }

        private static void CopyVector(Material mat, string from, string to)
        {
            if (!mat.HasProperty(from) || !mat.HasProperty(to))
                return;
            Vector4 value = mat.GetVector(from);
            if (mat.GetVector(to) == Vector4.zero && value != Vector4.zero)
                mat.SetVector(to, value);
        }

        public sealed class ConversionReport
        {
            public bool DryRun;
            public int Scanned;
            public int Convertible;
            public int Converted;
            public int Skipped;
            public int SkippedBuiltInRemaining;
            public int Failed;
            public List<string> Samples = new List<string>();
            public List<string> Errors = new List<string>();

            public string ToSummary()
            {
                var sb = new StringBuilder();
                sb.AppendLine(DryRun ? "DRY RUN" : "APPLIED");
                sb.AppendLine($"Scanned: {Scanned}");
                sb.AppendLine($"Convertible: {Convertible}");
                if (!DryRun)
                    sb.AppendLine($"Converted: {Converted}  Failed: {Failed}  Skipped: {Skipped}");
                else
                    sb.AppendLine($"(Would convert {Convertible}; skipped: {Skipped})");

                if (Samples.Count > 0)
                {
                    sb.AppendLine("Samples:");
                    foreach (string sample in Samples)
                        sb.AppendLine("  - " + sample);
                }

                if (Errors.Count > 0)
                {
                    sb.AppendLine("Errors:");
                    foreach (string error in Errors)
                        sb.AppendLine("  - " + error);
                }

                return sb.ToString();
            }
        }
    }
}
