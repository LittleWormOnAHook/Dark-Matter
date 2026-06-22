using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Project.Core
{
    public class PostProcessingController : MonoBehaviour
    {
        public static PostProcessingController Instance { get; private set; }

        [SerializeField] private VolumeProfile volumeProfile;
        [SerializeField] private bool createVolumeOnAwake = true;

        private Volume globalVolume;
        private UniversalAdditionalCameraData cameraData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            ResolveVolumeProfile();
            EnsureAudioListener();
            EnsureGlobalVolume();
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
            EnsureAudioListener();
            BindMainCamera();
            ApplyFromSettings();
        }

        public void ApplyFromSettings()
        {
            BindMainCamera();
            GameplayAudioUtility.EnsureListenerOnCamera(Camera.main);
            bool enabled = GameSettings.PostProcessingEnabled;

            if (cameraData != null)
                cameraData.renderPostProcessing = enabled;

            if (globalVolume != null)
            {
                globalVolume.enabled = enabled;
                globalVolume.weight = enabled ? 1f : 0f;
            }
        }

        public void SetPostProcessingEnabled(bool enabled)
        {
            GameSettings.SetPostProcessingEnabled(enabled);
            ApplyFromSettings();
        }

        private void ResolveVolumeProfile()
        {
            if (volumeProfile != null)
                return;

            volumeProfile = Resources.Load<VolumeProfile>("PostProcessing/SampleSceneProfile");
#if UNITY_EDITOR
            if (volumeProfile == null)
            {
                volumeProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                    "Assets/Settings/SampleSceneProfile.asset");
            }
#endif
        }

        private void EnsureGlobalVolume()
        {
            if (!createVolumeOnAwake || volumeProfile == null)
                return;

            globalVolume = FindAnyObjectByType<Volume>();
            if (globalVolume != null && globalVolume.isGlobal)
                return;

            GameObject volumeObject = new GameObject("GlobalPostProcessingVolume");
            volumeObject.transform.SetParent(transform);
            globalVolume = volumeObject.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.priority = 10f;
            globalVolume.profile = volumeProfile;
        }

        private void BindMainCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            cameraData = mainCamera.GetUniversalAdditionalCameraData();
        }

        private static void EnsureAudioListener()
        {
            GameplayAudioUtility.EnsureListenerOnCamera(Camera.main);
        }
    }
}
