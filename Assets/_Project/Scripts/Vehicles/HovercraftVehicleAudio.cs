using UnityEngine;

namespace Project.Vehicles
{
    /// <summary>
    /// One-shot hovercraft audio: board, exit, and boost.
    /// Profile clips can be overridden per prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public class HovercraftVehicleAudio : MonoBehaviour
    {
        [SerializeField] private HovercraftProfile profile;
        [SerializeField] private AudioSource oneShotSource;

        [Header("Clip Overrides")]
        [Tooltip("Uses profile.boardClip when empty.")]
        [SerializeField] private AudioClip boardClip;
        [Tooltip("Uses profile.exitClip when empty.")]
        [SerializeField] private AudioClip exitClip;
        [Tooltip("Uses profile.boostAudioClip when empty.")]
        [SerializeField] private AudioClip boostClip;

        private void Awake()
        {
            EnsureAudioSource();
        }

        public void Configure(HovercraftProfile hoverProfile, AudioSource source = null)
        {
            profile = hoverProfile;
            if (source != null)
                oneShotSource = source;

            EnsureAudioSource();
        }

        public void SetBoardClip(AudioClip clip) => boardClip = clip;
        public void SetExitClip(AudioClip clip) => exitClip = clip;
        public void SetBoostClip(AudioClip clip) => boostClip = clip;

        public void PlayBoard()
        {
            PlayOneShot(boardClip, profile != null ? profile.boardClip : null, profile != null ? profile.boardVolume : 1f);
        }

        public void PlayExit()
        {
            PlayOneShot(exitClip, profile != null ? profile.exitClip : null, profile != null ? profile.exitVolume : 1f);
        }

        public void PlayBoost()
        {
            PlayOneShot(boostClip, profile != null ? profile.boostAudioClip : null, profile != null ? profile.boostVolume : 1f);
        }

        private void PlayOneShot(AudioClip overrideClip, AudioClip profileClip, float volume)
        {
            AudioClip clip = overrideClip != null ? overrideClip : profileClip;
            if (clip == null || oneShotSource == null)
                return;

            oneShotSource.PlayOneShot(clip, volume);
        }

        private void EnsureAudioSource()
        {
            if (oneShotSource != null)
                return;

            oneShotSource = GetComponent<AudioSource>();
            if (oneShotSource == null)
                oneShotSource = gameObject.AddComponent<AudioSource>();

            oneShotSource.loop = false;
            oneShotSource.playOnAwake = false;
            oneShotSource.spatialBlend = 1f;
            oneShotSource.minDistance = 4f;
            oneShotSource.maxDistance = 45f;
            oneShotSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
    }
}
