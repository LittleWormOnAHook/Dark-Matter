using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Rendering
{
    /// <summary>
    /// Disables GPU instancing on third-party HDRP materials whose shaders do not declare
    /// Unity 6 instancing properties (unity_ObjectToWorldArray, unity_RenderingLayer, etc.).
    /// </summary>
    public static class HdrpGpuInstancingFixup
    {
        private const string LogPrefix = "[HDRP Instancing Fixup]";

        private static readonly string[] SearchRoots =
        {
            "Assets/Procedural Worlds",
            "Assets/NatureManufacture Assets/L.V.E- Lava and Volcano Environment",
            "Assets/QFX",
            "Assets/Gaia User Data",
            "Assets/_Project/Resources/Dash",
            "Assets/Invector-3rdPersonController",
            "Assets/Malbers Animations",
            "Assets/_Project/Resources/Dash",
        };

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Disable Incompatible GPU Instancing (Dry Run)", false, 37)]
        public static void DryRunMenu()
        {
            FixReport report = FixAll(dryRun: true, includeActiveSceneOnly: false);
            EditorUtility.DisplayDialog("HDRP GPU Instancing (Dry Run)", report.ToSummary(), "OK");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Disable Incompatible GPU Instancing (Apply)", false, 38)]
        public static void ApplyMenu()
        {
            FixReport report = FixAll(dryRun: false, includeActiveSceneOnly: false);
            EditorUtility.DisplayDialog("HDRP GPU Instancing", report.ToSummary(), "OK");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Hdrp + "Disable Incompatible GPU Instancing (Active Scene Only)", false, 39)]
        public static void ApplyActiveSceneMenu()
        {
            FixReport report = FixAll(dryRun: false, includeActiveSceneOnly: true);
            EditorUtility.DisplayDialog("HDRP GPU Instancing (Scene)", report.ToSummary(), "OK");
        }

        /// <summary>MCP / automation entry.</summary>
        public static FixReport FixAll(bool dryRun, bool includeActiveSceneOnly)
        {
            var report = new FixReport { DryRun = dryRun, SceneOnly = includeActiveSceneOnly };
            var materials = includeActiveSceneOnly ? CollectSceneMaterials() : CollectFolderMaterials();
            report.Scanned = materials.Count;

            if (!dryRun)
                AssetDatabase.StartAssetEditing();

            try
            {
                foreach (Material mat in materials)
                {
                    if (mat == null || mat.shader == null || !mat.enableInstancing)
                    {
                        report.Skipped++;
                        continue;
                    }

                    if (!ShaderNeedsInstancingDisabled(mat.shader))
                    {
                        report.Skipped++;
                        continue;
                    }

                    report.Fixable++;
                    string path = AssetDatabase.GetAssetPath(mat);
                    if (report.Samples.Count < 40)
                        report.Samples.Add($"{path}  ({mat.shader.name})");

                    if (dryRun)
                        continue;

                    mat.enableInstancing = false;
                    EditorUtility.SetDirty(mat);
                    report.Fixed++;
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

        /// <summary>Used by material converters after HDRP remap.</summary>
        public static void DisableInstancingIfIncompatible(Material mat)
        {
            if (mat == null || mat.shader == null || !mat.enableInstancing)
                return;
            if (!ShaderNeedsInstancingDisabled(mat.shader))
                return;

            mat.enableInstancing = false;
        }

        public static bool ShaderNeedsInstancingDisabled(Shader shader)
        {
            if (shader == null)
                return false;

            string name = shader.name;
            if (string.IsNullOrEmpty(name))
                return false;

            if (name.StartsWith("Shader Graphs/PW_", StringComparison.Ordinal))
                return true;

            if (string.Equals(name, "HDRP/Nature/SpeedTree8", StringComparison.Ordinal))
                return true;

            if (name.StartsWith("NatureManufacture/", StringComparison.Ordinal)
                || name.StartsWith("NatureManufacture Shaders/", StringComparison.Ordinal))
                return true;

            return false;
        }

        private static HashSet<Material> CollectSceneMaterials()
        {
            var set = new HashSet<Material>();
            Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material[] mats = renderer.sharedMaterials;
                if (mats == null)
                    continue;

                foreach (Material mat in mats)
                {
                    if (mat != null)
                        set.Add(mat);
                }
            }

            return set;
        }

        private static HashSet<Material> CollectFolderMaterials()
        {
            var set = new HashSet<Material>();
            foreach (string root in SearchRoots)
            {
                if (!AssetDatabase.IsValidFolder(root))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:Material", new[] { root });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat != null)
                        set.Add(mat);
                }
            }

            return set;
        }

        public sealed class FixReport
        {
            public bool DryRun;
            public bool SceneOnly;
            public int Scanned;
            public int Fixable;
            public int Fixed;
            public int Skipped;
            public List<string> Samples = new List<string>();

            public string ToSummary()
            {
                var sb = new StringBuilder();
                sb.AppendLine(DryRun ? "DRY RUN" : "APPLIED");
                sb.AppendLine(SceneOnly ? "Scope: active scene materials" : "Scope: vendor folders");
                sb.AppendLine($"Scanned: {Scanned}");
                sb.AppendLine($"Fixable: {Fixable}");
                if (!DryRun)
                    sb.AppendLine($"Fixed: {Fixed}  Skipped: {Skipped}");
                else
                    sb.AppendLine($"(Would fix {Fixable}; skipped: {Skipped})");

                if (Samples.Count > 0)
                {
                    sb.AppendLine("Samples:");
                    foreach (string sample in Samples)
                        sb.AppendLine("  - " + sample);
                }

                return sb.ToString().TrimEnd();
            }
        }
    }
}
