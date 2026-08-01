using Project.Core;
using UnityEngine;

namespace Project.Creatures
{
    /// <summary>
    /// RiggedNative creature SFX: interval footsteps while moving, one-shots for ranged/melee/death.
    /// Clips and volumes come from <see cref="DMICreatureDefinition"/> (Creatures Manager).
    /// </summary>
    [DisallowMultipleComponent]
    public class DMICreatureAudioDriver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private DMICreatureAiController ai;

        [Header("Walk / Footsteps")]
        [SerializeField] private AudioClip walkFootstepClip;
        [SerializeField] private AudioClip[] walkFootstepVariants = System.Array.Empty<AudioClip>();
        [SerializeField] [Range(0f, 1f)] private float walkVolume = 0.55f;
        [SerializeField] [Min(0.05f)] private float footstepInterval = 0.4f;
        [SerializeField] private float moveSpeedThreshold = 0.15f;

        [Header("Attack")]
        [SerializeField] private AudioClip rangedAttackClip;
        [SerializeField] [Range(0f, 1f)] private float rangedAttackVolume = 0.85f;
        [SerializeField] private AudioClip meleeAttackClip;
        [SerializeField] [Range(0f, 1f)] private float meleeAttackVolume = 0.85f;

        [Header("Death")]
        [SerializeField] private AudioClip deathClip;
        [SerializeField] [Range(0f, 1f)] private float deathVolume = 0.9f;

        [Header("Spatial")]
        [SerializeField] private float audioMinDistance = 2f;
        [SerializeField] private float audioMaxDistance = 28f;

        private float nextFootstepTime;
        private bool isDead;
        private bool deathSfxPlayed;

        private void Awake()
        {
            CacheReferences();
            EnsureAudioSource();
        }

        private void OnEnable()
        {
            CacheReferences();
            EnsureAudioSource();
            nextFootstepTime = 0f;
            isDead = false;
            deathSfxPlayed = false;
        }

        /// <summary>Called from AI Update — plays footstep SFX while moving, not when idle.</summary>
        public void Tick(DMICreatureAiController creatureAi)
        {
            if (isDead)
                return;

            if (creatureAi != null)
                ai = creatureAi;

            if (ai == null || !GameplayAudioUtility.CanPlaySpatialSource(audioSource))
                return;

            float speed = ai.CurrentSpeed;
            if (speed < moveSpeedThreshold)
            {
                nextFootstepTime = 0f;
                return;
            }

            if (Time.time < nextFootstepTime)
                return;

            AudioClip clip = PickWalkClip();
            if (clip == null)
                return;

            audioSource.pitch = Random.Range(0.94f, 1.06f);
            audioSource.PlayOneShot(clip, Mathf.Clamp01(walkVolume));
            audioSource.pitch = 1f;

            float interval = Mathf.Max(0.05f, footstepInterval);
            // Slightly faster cadence when running.
            if (speed > 3.2f)
                interval *= 0.72f;

            nextFootstepTime = Time.time + interval;
        }

        public void PlayRangedAttack()
        {
            PlayOneShot(rangedAttackClip, rangedAttackVolume);
        }

        public void PlayMeleeAttack()
        {
            PlayOneShot(meleeAttackClip, meleeAttackVolume);
        }

        /// <summary>
        /// One-shot death SFX at the start of death (with Death anim, before dissolve). Idempotent.
        /// </summary>
        public void PlayDeath()
        {
            if (deathSfxPlayed)
                return;

            deathSfxPlayed = true;
            // Play before marking dead so the one-shot is not blocked by the isDead guard.
            PlayOneShotAllowWhileDying(deathClip, deathVolume);
            isDead = true;
            nextFootstepTime = 0f;
        }

        public void NotifyDeath()
        {
            PlayDeath();
        }

        public void ConfigureFromDefinition(DMICreatureDefinition definition)
        {
            if (definition == null)
                return;

            walkFootstepClip = definition.walkFootstepClip;
            walkFootstepVariants = definition.walkFootstepVariants != null
                ? definition.walkFootstepVariants
                : System.Array.Empty<AudioClip>();
            walkVolume = definition.walkVolume;
            footstepInterval = Mathf.Max(0.05f, definition.footstepInterval);
            rangedAttackClip = definition.rangedAttackClip;
            rangedAttackVolume = definition.rangedAttackVolume;
            meleeAttackClip = definition.meleeAttackClip;
            meleeAttackVolume = definition.meleeAttackVolume;
            deathClip = definition.deathAudioClip;
            deathVolume = definition.deathVolume;
            audioMinDistance = definition.audioMinDistance;
            audioMaxDistance = definition.audioMaxDistance;

            EnsureAudioSource();
            ApplySpatialSettings();
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || isDead)
                return;

            PlayOneShotAllowWhileDying(clip, volume);
        }

        private void PlayOneShotAllowWhileDying(AudioClip clip, float volume)
        {
            if (clip == null)
                return;

            EnsureAudioSource();
            if (!GameplayAudioUtility.CanPlaySpatialSource(audioSource))
                return;

            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private AudioClip PickWalkClip()
        {
            int variantCount = walkFootstepVariants != null ? walkFootstepVariants.Length : 0;
            int total = variantCount + (walkFootstepClip != null ? 1 : 0);
            if (total == 0)
                return null;

            if (total == 1)
                return walkFootstepClip != null ? walkFootstepClip : FirstNonNullVariant();

            int pick = Random.Range(0, total);
            if (walkFootstepClip != null)
            {
                if (pick == 0)
                    return walkFootstepClip;
                pick--;
            }

            if (walkFootstepVariants == null || pick < 0 || pick >= walkFootstepVariants.Length)
                return walkFootstepClip;

            AudioClip variant = walkFootstepVariants[pick];
            return variant != null ? variant : walkFootstepClip;
        }

        private AudioClip FirstNonNullVariant()
        {
            if (walkFootstepVariants == null)
                return null;

            for (int i = 0; i < walkFootstepVariants.Length; i++)
            {
                if (walkFootstepVariants[i] != null)
                    return walkFootstepVariants[i];
            }

            return null;
        }

        private void CacheReferences()
        {
            if (ai == null)
                ai = GetComponent<DMICreatureAiController>();
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            ApplySpatialSettings();
        }

        private void ApplySpatialSettings()
        {
            if (audioSource == null)
                return;

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            GameplayAudioUtility.ConfigureWorldSpatialSource(
                audioSource,
                Mathf.Max(0.1f, audioMinDistance),
                Mathf.Max(audioMinDistance + 0.1f, audioMaxDistance));
        }
    }
}
