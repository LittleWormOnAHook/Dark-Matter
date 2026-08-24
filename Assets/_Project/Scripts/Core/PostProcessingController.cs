using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_RENDER_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

namespace Project.Core
{
    /// <summary>
    /// Applies master post-processing toggle and per-effect overrides from <see cref="GameSettings"/>.
    /// Supports HDRP (primary) with URP fallback.
    /// </summary>
    public class PostProcessingController : MonoBehaviour
    {
        public static PostProcessingController Instance { get; private set; }

        private const float RuntimeVolumePriority = 10000f;
        private const float FallbackBloomIntensityBaseline = 0.234f;

        [SerializeField] private VolumeProfile volumeProfileTemplate;
        // Scene volumes (BOTD Post Processing) are the play-mode source of truth.
        // Do not spawn a competing high-priority GlobalPostProcessingVolume.
        [SerializeField] private bool createVolumeOnAwake = false;

        private Volume globalVolume;
        private VolumeProfile runtimeVolumeProfile;
#if UNITY_RENDER_PIPELINE_HDRP
        private float baselineBloomIntensity = FallbackBloomIntensityBaseline;
        private int baselineFogType = 1;
#endif
        private bool baselineValuesCached;
#if UNITY_RENDER_PIPELINE_UNIVERSAL
        private UniversalAdditionalCameraData urpCameraData;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapAfterSceneLoad()
        {
            if (!Application.isPlaying)
                return;

            EnsureExists();
            Instance?.ApplyFromSettings();
        }

        public static void EnsureExists()
        {
            if (!Application.isPlaying)
                return;

            if (Instance != null)
                return;

            if (FindAnyObjectByType<PostProcessingController>() != null)
                return;

            GameObject bootstrap = new GameObject("PostProcessingController");
            bootstrap.AddComponent<PostProcessingController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ResolveVolumeProfileTemplate();
            EnsureAudioListener();
            EnsureRuntimeGlobalVolume();
            BindMainCamera();
            ApplyFromSettings();

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            baselineValuesCached = false;
            EnsureAudioListener();
            BindMainCamera();
            ApplyFromSettings();
        }

        public void RebuildRuntimeProfile()
        {
            if (volumeProfileTemplate == null)
                ResolveVolumeProfileTemplate();

            if (volumeProfileTemplate == null)
                return;

            if (runtimeVolumeProfile != null)
                Destroy(runtimeVolumeProfile);

            runtimeVolumeProfile = Instantiate(volumeProfileTemplate);
            baselineValuesCached = false;
            EnsureRuntimeGlobalVolume();

            if (globalVolume != null)
                globalVolume.profile = runtimeVolumeProfile;
        }

        public void ApplyFromSettings()
        {
            BindMainCamera();
            GameplayAudioUtility.EnsureListenerOnCamera(Camera.main);

            bool masterEnabled = GameSettings.PostProcessingEnabled;
            ApplyCameraPostProcessing(masterEnabled);
            ApplyVolumeSettings(masterEnabled);
        }

        public void SetPostProcessingEnabled(bool enabled)
        {
            GameSettings.SetPostProcessingEnabled(enabled);
            ApplyFromSettings();
        }

        private void ApplyCameraPostProcessing(bool masterEnabled)
        {
#if UNITY_RENDER_PIPELINE_HDRP
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null)
                    continue;

                HDAdditionalCameraData hdCameraData = camera.GetComponent<HDAdditionalCameraData>();
                if (hdCameraData == null)
                    continue;

                hdCameraData.renderPostProcessing = masterEnabled;
            }
#endif
#if UNITY_RENDER_PIPELINE_UNIVERSAL
            if (urpCameraData != null)
                urpCameraData.renderPostProcessing = masterEnabled;
#endif
        }

        private void ApplyVolumeSettings(bool masterEnabled)
        {
            // Only touch a volume this controller created. Never clone/stomp
            // BOTD or other scene profiles via Volume.profile.
            if (globalVolume == null)
                return;

            globalVolume.enabled = true;
            globalVolume.weight = masterEnabled ? 1f : 0f;

            if (runtimeVolumeProfile != null)
                ApplyProfileOverrides(runtimeVolumeProfile, masterEnabled);
        }

        private void ApplyProfileOverrides(VolumeProfile profile, bool masterEnabled)
        {
            if (profile == null)
                return;

            CacheBaselineValues(profile);

            IReadOnlyList<VolumeComponent> components = profile.components;
            for (int i = 0; i < components.Count; i++)
            {
                VolumeComponent component = components[i];
                if (component == null)
                    continue;

                string componentTypeName = component.GetType().Name;
                if (!IsManagedPostEffect(componentTypeName))
                    continue;

                bool effectEnabled = masterEnabled && GameSettings.IsPostEffectEnabled(componentTypeName);
                component.active = effectEnabled;

#if UNITY_RENDER_PIPELINE_HDRP
                if (componentTypeName == "Bloom" && component is Bloom bloom)
                    ApplyBloomIntensity(bloom, effectEnabled);
#endif
            }

            ApplyFogSettings(profile, masterEnabled);
        }

        private void CacheBaselineValues(VolumeProfile profile)
        {
            if (baselineValuesCached || profile == null)
                return;

#if UNITY_RENDER_PIPELINE_HDRP
            if (profile.TryGet(out Bloom bloom) && bloom.intensity.overrideState)
                baselineBloomIntensity = bloom.intensity.value;
            else
                baselineBloomIntensity = FallbackBloomIntensityBaseline;

            if (profile.TryGet(out VisualEnvironment visualEnvironment) && visualEnvironment.fogType.overrideState)
                baselineFogType = visualEnvironment.fogType.value;
#endif

            baselineValuesCached = true;
        }

#if UNITY_RENDER_PIPELINE_HDRP
        private void ApplyBloomIntensity(Bloom bloom, bool bloomEnabled)
        {
            if (bloom == null || !bloomEnabled)
                return;

            float multiplier = 1f + GameSettings.BloomIntensity;
            bloom.intensity.Override(Mathf.Max(0f, baselineBloomIntensity * multiplier));
        }
#endif

        private void ApplyFogSettings(VolumeProfile profile, bool masterEnabled)
        {
            if (profile == null)
                return;

#if UNITY_RENDER_PIPELINE_HDRP
            bool fogEnabled = masterEnabled && GameSettings.FogEnabled;

            if (profile.TryGet(out Fog fog))
            {
                fog.active = fogEnabled;
                fog.enabled.Override(fogEnabled);
            }

            if (profile.TryGet(out VisualEnvironment visualEnvironment))
            {
                visualEnvironment.fogType.Override(fogEnabled ? baselineFogType : 0);
            }
#else
            _ = masterEnabled;
#endif
        }

        private static bool IsManagedPostEffect(string componentTypeName)
        {
            return componentTypeName switch
            {
                "Bloom" => true,
                "Fog" => true,
                "VisualEnvironment" => true,
                "MotionBlur" => true,
                "DepthOfField" => true,
                "AmbientOcclusion" => true,
                "ScreenSpaceAmbientOcclusion" => true,
                "ColorAdjustments" => true,
                "LiftGammaGain" => true,
                "SplitToning" => true,
                "Tonemapping" => true,
                "WhiteBalance" => true,
                "Vignette" => true,
                "RayTracingSettings" => true,
                "ChromaticAberration" => true,
                "FilmGrain" => true,
                "LensDistortion" => true,
                "PaniniProjection" => true,
                _ => false,
            };
        }

        private void ResolveVolumeProfileTemplate()
        {
            if (volumeProfileTemplate != null)
                return;

            VolumeProfile defaultProfile = ResolveHdrpDefaultVolumeProfile();
            if (defaultProfile != null)
            {
                volumeProfileTemplate = defaultProfile;
                return;
            }

            volumeProfileTemplate = Resources.Load<VolumeProfile>("PostProcessing/SampleSceneProfile");
#if UNITY_EDITOR
            if (volumeProfileTemplate == null)
            {
                volumeProfileTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                    "Assets/HDRPDefaultResources/DefaultSettingsVolumeProfile.asset");
            }

            if (volumeProfileTemplate == null)
            {
                volumeProfileTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                    "Assets/Settings/SampleSceneProfile.asset");
            }
#endif
        }

        private static VolumeProfile ResolveHdrpDefaultVolumeProfile()
        {
#if UNITY_RENDER_PIPELINE_HDRP && UNITY_6000_0_OR_NEWER
            HDRPDefaultVolumeProfileSettings defaultSettings =
                GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>();
            if (defaultSettings != null && defaultSettings.volumeProfile != null)
                return defaultSettings.volumeProfile;
#endif
            return null;
        }

        private void EnsureRuntimeGlobalVolume()
        {
            if (!createVolumeOnAwake || volumeProfileTemplate == null)
                return;

            if (runtimeVolumeProfile == null)
                runtimeVolumeProfile = Instantiate(volumeProfileTemplate);

            if (globalVolume == null)
            {
                GameObject volumeObject = new GameObject("GlobalPostProcessingVolume");
                volumeObject.transform.SetParent(transform);
                globalVolume = volumeObject.AddComponent<Volume>();
                globalVolume.isGlobal = true;
                globalVolume.priority = RuntimeVolumePriority;
            }

            globalVolume.profile = runtimeVolumeProfile;
        }

        private void BindMainCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
#if UNITY_RENDER_PIPELINE_UNIVERSAL
                urpCameraData = null;
#endif
                return;
            }

#if UNITY_RENDER_PIPELINE_UNIVERSAL
            urpCameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
#endif
        }

        private static void EnsureAudioListener()
        {
            GameplayAudioUtility.EnsureListenerOnCamera(Camera.main);
        }
    }
}
