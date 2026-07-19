using UnityEngine;

namespace Project.Vehicles
{
    /// <summary>
    /// Looping engine audio scaled by hovercraft planar speed.
    /// </summary>
    [DisallowMultipleComponent]
    public class HovercraftEngineAudio : MonoBehaviour
    {
        [SerializeField] private HovercraftProfile profile;
        [SerializeField] private HoverPhysicsDriver physicsDriver;
        [SerializeField] private HovercraftOccupancy occupancy;
        [SerializeField] private AudioSource engineSource;

        [Header("Engine Clip")]
        [Tooltip("Optional per-prefab override. Drag an imported MP3/WAV/OGG AudioClip from the Project window. Uses profile.engineRunningClip when empty.")]
        [SerializeField] private AudioClip engineRunningClip;

        private void Awake()
        {
            if (physicsDriver == null)
                physicsDriver = GetComponent<HoverPhysicsDriver>();

            if (occupancy == null)
                occupancy = GetComponent<HovercraftOccupancy>();

            EnsureAudioSource();
        }

        public void Configure(HovercraftProfile hoverProfile, HoverPhysicsDriver driver, HovercraftOccupancy craftOccupancy = null, AudioSource source = null)
        {
            profile = hoverProfile;
            physicsDriver = driver;
            if (craftOccupancy != null)
                occupancy = craftOccupancy;
            if (source != null)
                engineSource = source;

            EnsureAudioSource();
        }

        public void SetEngineRunningClip(AudioClip clip)
        {
            engineRunningClip = clip;
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;

            if (engineSource == null || profile == null || occupancy == null || physicsDriver == null)
                return;

            AudioClip clip = ResolveEngineClip();
            if (!occupancy.IsOccupied || clip == null)
            {
                if (engineSource.isPlaying)
                    engineSource.Stop();
                return;
            }

            if (engineSource.clip != clip)
                engineSource.clip = clip;

            float speedRatio = physicsDriver.CurrentSpeedRatio;
            engineSource.pitch = Mathf.Lerp(profile.enginePitchRange.x, profile.enginePitchRange.y, speedRatio);
            engineSource.volume = profile.engineVolume * Mathf.Lerp(0.35f, 1f, speedRatio);

            if (!engineSource.isPlaying)
                engineSource.Play();
        }

        private AudioClip ResolveEngineClip()
        {
            if (engineRunningClip != null)
                return engineRunningClip;

            return profile != null ? profile.engineRunningClip : null;
        }

        private void EnsureAudioSource()
        {
            if (engineSource != null)
                return;

            engineSource = GetComponent<AudioSource>();
            if (engineSource == null)
                engineSource = GetComponentInChildren<AudioSource>();

            if (engineSource == null)
                engineSource = gameObject.AddComponent<AudioSource>();

            engineSource.loop = true;
            engineSource.playOnAwake = false;
            engineSource.spatialBlend = 1f;
            engineSource.minDistance = 4f;
            engineSource.maxDistance = 45f;
            engineSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
    }
}
