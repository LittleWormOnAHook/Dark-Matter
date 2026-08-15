using Project.Core;
using Project.EditorTools.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.EditorTools
{
    /// <summary>
    /// Wires PC / macOS / console quality tiers to Genesis HDRP assets (or legacy URP fallback).
    /// </summary>
    public static class PlatformQualitySetupUtility
    {
        private const string HdrpRoot = "Assets/Settings/HDRP";
        private const string PcPipelinePath = "Assets/Settings/PC_RPAsset.asset";
        private const string MobilePipelinePath = "Assets/Settings/Mobile_RPAsset.asset";

        [MenuItem(DarkMatterGenesisEditorMenus.Maintenance + "Configure Platform Quality Tiers", false, 10)]
        public static void ConfigurePlatformQualityTiers()
        {
            if (AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>($"{HdrpRoot}/Genesis_HDRP_High.asset") != null)
            {
                GenesisHdrpMigrationUtility.CreateGenesisHdrpFoundation();
                return;
            }

            ConfigureLegacyUrpQualityTiers();
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Combat + "Add Combat Zone To Selection", false, 45)]
        public static void AddCombatZoneToSelection()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Combat Zone", "Select one or more GameObjects in the hierarchy.", "OK");
                return;
            }

            int added = 0;
            for (int i = 0; i < selected.Length; i++)
            {
                GameObject target = selected[i];
                if (target == null)
                    continue;

                if (target.GetComponent<Project.AI.CombatZoneController>() == null)
                {
                    Undo.AddComponent<Project.AI.CombatZoneController>(target);
                    added++;
                }
            }

            Debug.Log($"PlatformQualitySetupUtility: added CombatZoneController to {added} object(s).");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Maintenance + "Audit _Project Resources Size", false, 20)]
        public static void AuditProjectResourcesSize()
        {
            string resourcesRoot = "Assets/_Project/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesRoot))
            {
                EditorUtility.DisplayDialog("Build Audit", "No Assets/_Project/Resources folder found.", "OK");
                return;
            }

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { resourcesRoot });
            long totalBytes = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                    continue;

                System.IO.FileInfo info = new System.IO.FileInfo(path);
                if (info.Exists)
                    totalBytes += info.Length;
            }

            float megabytes = totalBytes / (1024f * 1024f);
            EditorUtility.DisplayDialog(
                "Resources Size Audit",
                $"Assets/_Project/Resources footprint (on disk, excluding meta):\n{megabytes:0.0} MB\n\n" +
                "PC / console target — use this as a sanity check for runtime-loaded content only.",
                "OK");
        }

        private static void ConfigureLegacyUrpQualityTiers()
        {
            RenderPipelineAsset pcPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PcPipelinePath);
            RenderPipelineAsset mobilePipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(MobilePipelinePath);

            if (pcPipeline == null || mobilePipeline == null)
            {
                EditorUtility.DisplayDialog(
                    "Platform Quality Setup",
                    "Could not find PC_RPAsset or Mobile_RPAsset under Assets/Settings/.",
                    "OK");
                return;
            }

            string[] tierNames = QualitySettings.names;
            int lowIndex = FindTierIndex(tierNames, "Performance", "Low", "Web GL", "Level 0", "Very Low");
            int pcIndex = FindTierIndex(tierNames, "High", "Level 1", "PC");

            if (lowIndex < 0)
                lowIndex = 0;
            if (pcIndex < 0)
                pcIndex = Mathf.Min(1, tierNames.Length - 1);

            SetPipelineForTier(lowIndex, mobilePipeline);
            SetPipelineForTier(pcIndex, pcPipeline);

            QualitySettings.SetQualityLevel(pcIndex);
            PlatformGraphicsBootstrap.ApplyTierOverrides(pcIndex);

            EditorUtility.DisplayDialog(
                "Platform Quality Setup",
                $"Configured legacy URP quality tiers:\n\n" +
                $"- Index {lowIndex} ({tierNames[lowIndex]}): lighter URP\n" +
                $"- Index {pcIndex} ({tierNames[pcIndex]}): PC URP\n\n" +
                "Run Tools/Dark Matter Genesis/HDRP/Phase 0/1 for Genesis HDRP tiers.",
                "OK");
        }

        private static int FindTierIndex(string[] tierNames, params string[] candidates)
        {
            for (int i = 0; i < tierNames.Length; i++)
            {
                for (int c = 0; c < candidates.Length; c++)
                {
                    if (tierNames[i] == candidates[c])
                        return i;
                }
            }

            return -1;
        }

        private static void SetPipelineForTier(int tierIndex, RenderPipelineAsset pipeline)
        {
            if (pipeline == null || tierIndex < 0 || tierIndex >= QualitySettings.names.Length)
                return;

            QualitySettings.SetQualityLevel(tierIndex);
            QualitySettings.renderPipeline = pipeline;
        }
    }
}
