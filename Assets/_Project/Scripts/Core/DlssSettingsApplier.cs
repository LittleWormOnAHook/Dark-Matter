using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_RENDER_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Project.Core
{
    /// <summary>
    /// Applies DLSS / HDRP dynamic-resolution upscaling from <see cref="GameSettings"/>.
    /// </summary>
    public static class DlssSettingsApplier
    {
        /// <summary>Maps settings UI dropdown index to <c>UnityEngine.NVIDIA.DLSSQuality</c> uint values.</summary>
        public static readonly int[] QualityDropdownToEnum =
        {
            2, // Quality (MaximumQuality)
            1, // Balanced
            0, // Performance (MaximumPerformance)
            3, // Ultra Performance
        };

        public static readonly string[] QualityDropdownLabels =
        {
            "Quality",
            "Balanced",
            "Performance",
            "Ultra Performance",
        };

        public static bool IsDlssUiAvailable()
        {
#if UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS || UNITY_TVOS
            return false;
#else
            return true;
#endif
        }

        public static bool IsDlssRuntimeSupported()
        {
            if (!IsDlssUiAvailable())
                return false;

#if UNITY_RENDER_PIPELINE_HDRP
            return HDDynamicResolutionPlatformCapabilities.DLSSDetected;
#else
            return false;
#endif
        }

        public static int GetQualityDropdownIndex(int enumValue)
        {
            for (int i = 0; i < QualityDropdownToEnum.Length; i++)
            {
                if (QualityDropdownToEnum[i] == enumValue)
                    return i;
            }

            return 0;
        }

        public static int GetQualityEnumFromDropdownIndex(int dropdownIndex)
        {
            dropdownIndex = Mathf.Clamp(dropdownIndex, 0, QualityDropdownToEnum.Length - 1);
            return QualityDropdownToEnum[dropdownIndex];
        }

        public static void ApplyFromGameSettings()
        {
#if UNITY_RENDER_PIPELINE_HDRP
            HDRenderPipelineAsset pipelineAsset = ResolveActiveHdrpAsset();
            if (pipelineAsset == null)
                return;

            RenderPipelineSettings platformSettings = pipelineAsset.currentPlatformRenderPipelineSettings;
            GlobalDynamicResolutionSettings dynamicResolution = platformSettings.dynamicResolutionSettings;

            bool enableDlss = GameSettings.DlssEnabled && IsDlssUiAvailable();

            if (enableDlss)
            {
                dynamicResolution.enabled = true;
                dynamicResolution.DLSSUseOptimalSettings = true;
                dynamicResolution.DLSSPerfQualitySetting = (uint)Mathf.Max(0, GameSettings.DlssQualityIndex);
                dynamicResolution.dynResType = DynamicResolutionType.Hardware;
                dynamicResolution.DLSSInjectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.BeforePost;

                if (dynamicResolution.advancedUpscalerNames == null)
                    dynamicResolution.advancedUpscalerNames = new List<string>();

                string dlssName = AdvancedUpscalers.DLSS.ToString();
                if (!dynamicResolution.advancedUpscalerNames.Contains(dlssName))
                {
                    dynamicResolution.advancedUpscalerNames.Clear();
                    dynamicResolution.advancedUpscalerNames.Add(dlssName);
                }
            }
            else
            {
                RemoveDlssUpscaler(ref dynamicResolution);

                if (dynamicResolution.advancedUpscalerNames == null ||
                    dynamicResolution.advancedUpscalerNames.Count == 0)
                {
                    dynamicResolution.enabled = false;
                }
            }

            platformSettings.dynamicResolutionSettings = dynamicResolution;
            pipelineAsset.currentPlatformRenderPipelineSettings = platformSettings;

            ApplyCameraDynamicResolution(enableDlss && IsDlssRuntimeSupported());
#endif
        }

#if UNITY_RENDER_PIPELINE_HDRP
        private static HDRenderPipelineAsset ResolveActiveHdrpAsset()
        {
            if (QualitySettings.renderPipeline is HDRenderPipelineAsset qualityAsset)
                return qualityAsset;

            return GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset;
        }

        private static void RemoveDlssUpscaler(ref GlobalDynamicResolutionSettings dynamicResolution)
        {
            if (dynamicResolution.advancedUpscalerNames == null)
                return;

            string dlssName = AdvancedUpscalers.DLSS.ToString();
            for (int i = dynamicResolution.advancedUpscalerNames.Count - 1; i >= 0; i--)
            {
                if (dynamicResolution.advancedUpscalerNames[i] == dlssName)
                    dynamicResolution.advancedUpscalerNames.RemoveAt(i);
            }
        }

        private static void ApplyCameraDynamicResolution(bool allowDynamicResolution)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null)
                    continue;

                HDAdditionalCameraData hdCameraData = camera.GetComponent<HDAdditionalCameraData>();
                if (hdCameraData == null)
                    continue;

                hdCameraData.allowDynamicResolution = allowDynamicResolution;
            }
        }
#endif
    }
}
