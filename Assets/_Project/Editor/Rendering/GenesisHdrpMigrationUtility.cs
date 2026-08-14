using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.Rendering
{
    /// <summary>
    /// One-click HDRP migration helpers for Dark Matter: Genesis.
    /// Keeps URP active until Phase 6 switch is run explicitly.
    /// </summary>
    public static class GenesisHdrpMigrationUtility
    {
        private const string HdrpRoot = "Assets/Settings/HDRP";
        private const string HdrpDefaultResources = "Assets/HDRPDefaultResources";
        private const string TestScenePath = "Assets/_Project/Scenes/Genesis_HDRP_Test.unity";

        private static readonly (string fileName, GenesisHdrpTier tier)[] TierAssets =
        {
            ("Genesis_HDRP_Performance.asset", GenesisHdrpTier.Performance),
            ("Genesis_HDRP_Balanced.asset", GenesisHdrpTier.Balanced),
            ("Genesis_HDRP_Quality.asset", GenesisHdrpTier.Quality),
            ("Genesis_HDRP_High.asset", GenesisHdrpTier.High),
            ("Genesis_HDRP_Ultra.asset", GenesisHdrpTier.Ultra),
        };

        [MenuItem(SurvivalPioneerEditorMenus.Hdrp + "Phase 0/1 - Create Genesis HDRP Foundation", false, 0)]
        public static void CreateGenesisHdrpFoundation()
        {
            EnsureFolder(HdrpRoot);
            EnsureFolder(HdrpDefaultResources);

            HDRenderPipelineGlobalSettings globalSettings = EnsureGlobalSettings();
            HDRenderPipelineAsset[] tierAssets = CreateOrUpdateTierAssets();

            ConfigureQualityTiers(tierAssets);
            RegisterGlobalSettingsMap(globalSettings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Genesis HDRP Foundation",
                "Created Genesis HDRP tier assets under Assets/Settings/HDRP/.\n\n" +
                "Configured five quality tiers (Performance → Ultra).\n" +
                "Global URP pipeline is unchanged until you run Phase 6 switch.\n\n" +
                "Next: open Genesis_HDRP_Test scene (menu) or assign HDRP assets in Quality Settings.",
                "OK");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Hdrp + "Phase 1 - Create HDRP Test Scene", false, 10)]
        public static void CreateHdrpTestScene()
        {
            CreateGenesisHdrpFoundation();

            if (File.Exists(TestScenePath))
            {
                EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
                EditorUtility.DisplayDialog("Genesis HDRP Test Scene", "Opened existing test scene.", "OK");
                return;
            }

            EnsureFolder("Assets/_Project/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject sun = GameObject.Find("Directional Light");
            if (sun != null)
            {
                Light light = sun.GetComponent<Light>();
                if (light != null)
                {
                    light.type = LightType.Directional;
                    light.intensity = 100000f;
                }
            }

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);

            GameObject volumeGo = new GameObject("Global Volume");
            Volume volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;

            EditorSceneManager.SaveScene(scene, TestScenePath);
            AddSceneToBuildSettings(TestScenePath, enabled: true);

            EditorUtility.DisplayDialog(
                "Genesis HDRP Test Scene",
                $"Saved {TestScenePath}.\n\n" +
                "Switch Quality to Performance/Ultra to validate tier assets.\n" +
                "Run Phase 6 only when ready to move the whole project to HDRP.",
                "OK");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Hdrp + "Phase 6 - Switch Global Pipeline To HDRP High", false, 60)]
        public static void SwitchGlobalPipelineToHdrpHigh()
        {
            string highPath = $"{HdrpRoot}/Genesis_HDRP_High.asset";
            HDRenderPipelineAsset highAsset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(highPath);
            if (highAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "Genesis HDRP",
                    "Genesis_HDRP_High.asset not found. Run Phase 0/1 foundation first.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Switch Global Pipeline To HDRP",
                    "This sets Project Settings > Graphics to Genesis_HDRP_High and enables HDRP on the active quality tier.\n\n" +
                    "Ensure materials/scenes are converted before relying on the main game scene.",
                    "Switch",
                    "Cancel"))
            {
                return;
            }

            GraphicsSettings.defaultRenderPipeline = highAsset;
            QualitySettings.SetQualityLevel(Project.Core.PlatformGraphicsProfile.HighTierIndex, applyExpensiveChanges: true);
            QualitySettings.renderPipeline = highAsset;

            EditorUtility.SetDirty(GraphicsSettings.renderPipelineAsset);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Genesis HDRP",
                "Global pipeline switched to Genesis_HDRP_High.\nCheck Unity Console for pink materials or compile errors.",
                "OK");
        }

        private static HDRenderPipelineGlobalSettings EnsureGlobalSettings()
        {
            string[] existing = AssetDatabase.FindAssets("t:HDRenderPipelineGlobalSettings", new[] { HdrpDefaultResources });
            HDRenderPipelineGlobalSettings settings;
            if (existing.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(existing[0]);
                settings = AssetDatabase.LoadAssetAtPath<HDRenderPipelineGlobalSettings>(path);
            }
            else
            {
                settings = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettings>();
                AssetDatabase.CreateAsset(settings, $"{HdrpDefaultResources}/HDRenderPipelineGlobalSettings.asset");
            }

            return settings;
        }

        private static HDRenderPipelineAsset[] CreateOrUpdateTierAssets()
        {
            HDRenderPipelineAsset template = LoadTemplateAsset();
            HDRenderPipelineAsset[] results = new HDRenderPipelineAsset[TierAssets.Length];

            for (int i = 0; i < TierAssets.Length; i++)
            {
                string path = $"{HdrpRoot}/{TierAssets[i].fileName}";
                HDRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(path);
                if (asset == null)
                {
                    asset = template != null
                        ? UnityEngine.Object.Instantiate(template)
                        : ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                ApplyTierSettings(asset, TierAssets[i].tier);
                EditorUtility.SetDirty(asset);
                results[i] = asset;
            }

            return results;
        }

        private static HDRenderPipelineAsset LoadTemplateAsset()
        {
            string[] guids = AssetDatabase.FindAssets("t:HDRenderPipelineAsset");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.StartsWith(HdrpRoot, StringComparison.Ordinal))
                    continue;

                HDRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(path);
                if (asset != null)
                    return asset;
            }

            return null;
        }

        private static void ApplyTierSettings(HDRenderPipelineAsset asset, GenesisHdrpTier tier)
        {
            asset.name = Path.GetFileNameWithoutExtension(TierAssets[(int)tier].fileName);

            SerializedObject serialized = new SerializedObject(asset);
            SerializedProperty settings = serialized.FindProperty("m_RenderPipelineSettings");
            if (settings == null)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return;
            }

            SetBool(settings, "supportRayTracing", tier == GenesisHdrpTier.Ultra);
            SetInt(settings, "supportedRayTracingMode", tier == GenesisHdrpTier.Ultra ? 3 : 0);
            SetBool(settings, "supportSSR", tier >= GenesisHdrpTier.Quality);
            SetBool(settings, "supportSSAO", tier >= GenesisHdrpTier.Balanced);
            SetBool(settings, "supportVolumetrics", tier >= GenesisHdrpTier.Balanced);
            SetInt(settings, "msaaSampleCount", tier >= GenesisHdrpTier.High ? 2 : 1);

            SerializedProperty shadowInit = settings.FindPropertyRelative("hdShadowInitParams");
            if (shadowInit != null)
            {
                int dirShadow = tier switch
                {
                    GenesisHdrpTier.Performance => 1024,
                    GenesisHdrpTier.Balanced => 1536,
                    GenesisHdrpTier.Quality => 2048,
                    GenesisHdrpTier.High => 2048,
                    _ => 4096,
                };
                SetInt(shadowInit, "maxDirectionalShadowMapResolution", dirShadow);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureQualityTiers(HDRenderPipelineAsset[] tierAssets)
        {
            string[] tierNames =
            {
                "Performance",
                "Balanced",
                "Quality",
                "High",
                "Ultra",
            };

            SerializedObject qualitySettings = LoadQualitySettingsObject();
            SerializedProperty tiers = qualitySettings.FindProperty("m_QualitySettings");
            tiers.arraySize = tierNames.Length;

            for (int i = 0; i < tierNames.Length; i++)
            {
                SerializedProperty tier = tiers.GetArrayElementAtIndex(i);
                tier.FindPropertyRelative("name").stringValue = tierNames[i];
                tier.FindPropertyRelative("customRenderPipeline").objectReferenceValue = tierAssets[i];
                tier.FindPropertyRelative("maximumLODLevel").intValue =
                    i == (int)GenesisHdrpTier.Performance ? 2 : i == (int)GenesisHdrpTier.Balanced ? 1 : 0;
                tier.FindPropertyRelative("lodBias").floatValue =
                    i == (int)GenesisHdrpTier.Performance ? 1.2f :
                    i == (int)GenesisHdrpTier.Balanced ? 1.5f :
                    i >= (int)GenesisHdrpTier.High ? 2f : 1.8f;
                tier.FindPropertyRelative("shadowDistance").floatValue =
                    i == (int)GenesisHdrpTier.Performance ? 25f :
                    i == (int)GenesisHdrpTier.Balanced ? 30f :
                    i >= (int)GenesisHdrpTier.High ? 40f : 35f;
                tier.FindPropertyRelative("antiAliasing").intValue =
                    i >= (int)GenesisHdrpTier.High ? 2 : i >= (int)GenesisHdrpTier.Quality ? 1 : 0;
            }

            qualitySettings.FindProperty("m_CurrentQuality").intValue = Project.Core.PlatformGraphicsProfile.DefaultQualityIndex;
            qualitySettings.ApplyModifiedPropertiesWithoutUndo();
            QualitySettings.SetQualityLevel(Project.Core.PlatformGraphicsProfile.DefaultQualityIndex, applyExpensiveChanges: false);
        }

        private static SerializedObject LoadQualitySettingsObject()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
            return new SerializedObject(assets[0]);
        }

        private static void RegisterGlobalSettingsMap(HDRenderPipelineGlobalSettings globalSettings)
        {
            if (globalSettings == null)
                return;

            GraphicsSettings.RegisterRenderPipelineSettings<HDRenderPipelineGlobalSettings>(globalSettings);
        }

        private static void AddSceneToBuildSettings(string scenePath, bool enabled)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                    return;
            }

            Array.Resize(ref scenes, scenes.Length + 1);
            scenes[scenes.Length - 1] = new EditorBuildSettingsScene(scenePath, enabled);
            EditorBuildSettings.scenes = scenes;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
                return;

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void SetBool(SerializedProperty parent, string propertyName, bool value)
        {
            SerializedProperty property = parent.FindPropertyRelative(propertyName);
            if (property != null)
                property.boolValue = value;
        }

        private static void SetInt(SerializedProperty parent, string propertyName, int value)
        {
            SerializedProperty property = parent.FindPropertyRelative(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private enum GenesisHdrpTier
        {
            Performance = 0,
            Balanced = 1,
            Quality = 2,
            High = 3,
            Ultra = 4,
        }
    }
}
