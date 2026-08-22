using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Project.EditorTools.Rendering
{
    /// <summary>
    /// Converts materials on the dual-target glTFast Shader Graph
    /// (<c>Shader Graphs/glTF-pbrMetallicRoughness</c>) to <c>HDRP/Lit</c>.
    /// <para>
    /// That graph expands URP + HDRP + DXR keyword spaces during player builds and has
    /// repeatedly crashed the Windows player build in
    /// <c>ShaderCompilation::PrepareStageVariants</c> (~27GB variant prep).
    /// Removing material references keeps the shader out of <c>BuildPlayerData</c>.
    /// </para>
    /// Handles standalone <c>.mat</c> assets and materials embedded in <c>.glb</c>/<c>.gltf</c>.
    /// </summary>
    public static class DmGltfShaderMaterialToHdrpConverter
    {
        private const string LogPrefix = "[DM glTF→HDRP]";
        private const string GltfPbrShaderName = "Shader Graphs/glTF-pbrMetallicRoughness";
        private const string HdrpLitName = "HDRP/Lit";

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Convert glTF-pbr Materials → HDRP Lit (Dry Run)", false, 40)]
        public static void ConvertDryRunMenu()
        {
            ConversionReport report = ConvertAll(dryRun: true);
            EditorUtility.DisplayDialog("glTF→HDRP Lit (Dry Run)", report.ToSummary(), "OK");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Convert glTF-pbr Materials → HDRP Lit (Apply)", false, 41)]
        public static void ConvertApplyMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Convert glTF-pbr → HDRP Lit",
                    "Reassign every material using Shader Graphs/glTF-pbrMetallicRoughness " +
                    "to HDRP/Lit (including materials embedded in .glb/.gltf).\n\n" +
                    "This unblocks Windows player builds that crash during shader variant prep.\n\n" +
                    "Continue?",
                    "Convert",
                    "Cancel"))
                return;

            ConversionReport report = ConvertAll(dryRun: false);
            EditorUtility.DisplayDialog("glTF→HDRP Lit", report.ToSummary(), "OK");
        }

        /// <summary>MCP / automation entry.</summary>
        public static ConversionReport ConvertAll(bool dryRun)
        {
            Shader hdrpLit = Shader.Find(HdrpLitName);
            if (hdrpLit == null)
            {
                Debug.LogError($"{LogPrefix} {HdrpLitName} not found.");
                return new ConversionReport { Failed = 1 };
            }

            var report = new ConversionReport { DryRun = dryRun };
            var targets = FindGltfPbrMaterials();
            report.Found = targets.Count;

            if (!dryRun)
                AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    Material mat = targets[i];
                    string path = AssetDatabase.GetAssetPath(mat);
                    string label = $"{path} :: {mat.name}";

                    try
                    {
                        if (dryRun)
                        {
                            report.Converted++;
                            report.ConvertedPaths.Add(label);
                            continue;
                        }

                        ConvertGltfToHdrpLit(mat, hdrpLit);
                        EditorUtility.SetDirty(mat);
                        UnityEngine.Object main = AssetDatabase.LoadMainAssetAtPath(path);
                        if (main != null)
                            EditorUtility.SetDirty(main);
                        report.Converted++;
                        report.ConvertedPaths.Add(label);
                    }
                    catch (Exception ex)
                    {
                        report.Failed++;
                        report.FailedPaths.Add($"{label} ({ex.Message})");
                        Debug.LogError($"{LogPrefix} Failed {label}: {ex}");
                    }
                }
            }
            finally
            {
                if (!dryRun)
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            }

            string summary = report.ToSummary();
            Debug.Log($"{LogPrefix} {summary}");
            return report;
        }

        private static List<Material> FindGltfPbrMaterials()
        {
            var results = new List<Material>();
            var seen = new HashSet<int>();

            string[] guids = AssetDatabase.FindAssets("t:Material");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                    continue;

                // Package shader graph itself can appear in FindAssets — skip non-project content
                // except we still want to convert project embeds; never mutate PackageCache mats
                // that belong to glTFast defaults (none expected as .mat, but be safe).
                if (path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase) &&
                    !path.StartsWith("Packages/com.unity.cloud.gltfast/", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (path.StartsWith("Packages/com.unity.cloud.gltfast/", StringComparison.OrdinalIgnoreCase))
                    continue;

                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                if (assets == null || assets.Length == 0)
                {
                    Material single = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (single != null)
                        Consider(single, results, seen);
                    continue;
                }

                for (int a = 0; a < assets.Length; a++)
                {
                    if (assets[a] is Material mat)
                        Consider(mat, results, seen);
                }
            }

            return results;
        }

        private static void Consider(Material mat, List<Material> results, HashSet<int> seen)
        {
            if (mat == null || mat.shader == null)
                return;
            if (mat.shader.name != GltfPbrShaderName)
                return;
            EntityId id = mat.GetEntityId();
            if (!seen.Add(id.GetHashCode()))
                return;
            results.Add(mat);
        }

        private static void ConvertGltfToHdrpLit(Material mat, Shader hdrpLit)
        {
            Color baseColor = mat.HasProperty("baseColorFactor")
                ? mat.GetColor("baseColorFactor")
                : Color.white;
            Texture baseMap = mat.HasProperty("baseColorTexture")
                ? mat.GetTexture("baseColorTexture")
                : null;
            Vector2 baseScale = baseMap != null && mat.HasProperty("baseColorTexture")
                ? mat.GetTextureScale("baseColorTexture")
                : Vector2.one;
            Vector2 baseOffset = baseMap != null && mat.HasProperty("baseColorTexture")
                ? mat.GetTextureOffset("baseColorTexture")
                : Vector2.zero;

            Texture normalMap = mat.HasProperty("normalTexture")
                ? mat.GetTexture("normalTexture")
                : null;
            float normalScale = mat.HasProperty("normalTexture_scale")
                ? mat.GetFloat("normalTexture_scale")
                : 1f;

            float metallic = mat.HasProperty("metallicFactor")
                ? mat.GetFloat("metallicFactor")
                : 0f;
            float roughness = mat.HasProperty("roughnessFactor")
                ? mat.GetFloat("roughnessFactor")
                : 1f;
            float smoothness = 1f - Mathf.Clamp01(roughness);
            Texture metallicRoughness = mat.HasProperty("metallicRoughnessTexture")
                ? mat.GetTexture("metallicRoughnessTexture")
                : null;

            Color emissive = mat.HasProperty("emissiveFactor")
                ? mat.GetColor("emissiveFactor")
                : Color.black;
            Texture emissiveMap = mat.HasProperty("emissiveTexture")
                ? mat.GetTexture("emissiveTexture")
                : null;
            bool hasEmission = mat.IsKeywordEnabled("_EMISSIVE") ||
                               emissiveMap != null ||
                               emissive.maxColorComponent > 0.0001f;

            float alphaCutoff = mat.HasProperty("alphaCutoff")
                ? mat.GetFloat("alphaCutoff")
                : 0.5f;
            bool alphaClip = mat.HasProperty("_AlphaCutoffEnable") &&
                             mat.GetFloat("_AlphaCutoffEnable") > 0.5f;
            bool transparent = mat.HasProperty("_SurfaceType") &&
                               mat.GetFloat("_SurfaceType") > 0.5f;
            bool doubleSided = mat.HasProperty("_DoubleSidedEnable") &&
                               mat.GetFloat("_DoubleSidedEnable") > 0.5f;

            mat.shader = hdrpLit;

            if (baseMap != null)
            {
                mat.SetTexture("_BaseColorMap", baseMap);
                mat.SetTextureScale("_BaseColorMap", baseScale);
                mat.SetTextureOffset("_BaseColorMap", baseOffset);
            }

            mat.SetColor("_BaseColor", baseColor);

            if (normalMap != null)
            {
                mat.SetTexture("_NormalMap", normalMap);
                mat.SetFloat("_NormalScale", normalScale);
                mat.SetFloat("_NormalMapSpace", 0f);
            }

            // glTF metallic-roughness texture: G=roughness, B=metallic (not URP MaskMap packing).
            // Prefer scalar metallic/smoothness; keep map only as a detail hint if present.
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            if (metallicRoughness != null)
            {
                // HDRP MaskMap expects R=metallic, G=AO, A=smoothness — glTF packing differs.
                // Leave MaskMap empty and rely on scalars to avoid wrong channel mapping.
            }

            if (hasEmission)
            {
                if (emissiveMap != null)
                    mat.SetTexture("_EmissiveColorMap", emissiveMap);

                HDMaterial.SetUseEmissiveIntensity(mat, false);
                HDMaterial.SetEmissiveColor(mat, emissive);
            }

            if (alphaClip)
            {
                HDMaterial.SetAlphaClipping(mat, true);
                HDMaterial.SetAlphaCutoff(mat, alphaCutoff);
            }

            HDMaterial.SetSurfaceType(mat, transparent);
            if (doubleSided)
            {
                mat.SetFloat("_DoubleSidedEnable", 1f);
                mat.doubleSidedGI = true;
                mat.EnableKeyword("_DOUBLESIDED_ON");
            }

            HDMaterial.ValidateMaterial(mat);
        }

        public sealed class ConversionReport
        {
            public bool DryRun;
            public int Found;
            public int Converted;
            public int Failed;
            public readonly List<string> ConvertedPaths = new List<string>();
            public readonly List<string> FailedPaths = new List<string>();

            public string ToSummary()
            {
                var sb = new StringBuilder();
                sb.Append(DryRun ? "Dry run — " : "Applied — ");
                sb.Append($"found {Found}, {(DryRun ? "would convert" : "converted")} {Converted}, failed {Failed}.");
                if (ConvertedPaths.Count > 0)
                {
                    sb.AppendLine();
                    int show = Math.Min(ConvertedPaths.Count, 20);
                    for (int i = 0; i < show; i++)
                        sb.AppendLine("  " + ConvertedPaths[i]);
                    if (ConvertedPaths.Count > show)
                        sb.AppendLine($"  … +{ConvertedPaths.Count - show} more");
                }

                if (FailedPaths.Count > 0)
                {
                    sb.AppendLine("Failures:");
                    for (int i = 0; i < FailedPaths.Count; i++)
                        sb.AppendLine("  " + FailedPaths[i]);
                }

                return sb.ToString();
            }
        }
    }
}
