using System.Collections;
using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// Walker Drill mining animation driver: Move for startup, looping Spin, reverse-Move on stop.
    /// Animator states are expected to be named Idle, Move, and Spin.
    /// Audio is driven from code (states are CrossFaded; animation events would miss blends).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMWalkerDrillController : MonoBehaviour
    {
        public const string IdleState = "Idle";
        public const string MoveState = "Move";
        public const string SpinState = "Spin";

        private const float AudioVolume = 0.7f;
        private const float RetractSpeed = -0.5f;
        private const float RetractAudioPitch = 0.5f;
        private const float RetractDoneNormalized = 0.02f;

        [Header("References")]
        [SerializeField] private Animator drillAnimator;
        [SerializeField] private AudioSource drillAudioSource;

        [Header("Timing")]
        [SerializeField] private float startupMoveSeconds = 2f;
        [SerializeField] private float crossFadeSeconds = 0.12f;

        [Header("Audio")]
        [SerializeField] private AudioClip moveAudioClip;
        [SerializeField] private AudioClip spinAudioClip;

        private enum MiningPhase
        {
            Idle = 0,
            Starting = 1,
            Spinning = 2,
            Retracting = 3
        }

        private MiningPhase phase = MiningPhase.Idle;
        private Coroutine miningRoutine;

        public bool IsMining => phase != MiningPhase.Idle;
        public bool IsSpinning => phase == MiningPhase.Spinning;
        public bool IsRetracting => phase == MiningPhase.Retracting;

        private void Awake()
        {
            if (drillAnimator == null)
                drillAnimator = GetComponent<Animator>();
            if (drillAnimator == null)
                drillAnimator = GetComponentInChildren<Animator>();

            EnsureAudioSource();
        }

        private void OnDisable()
        {
            StopMiningRoutine();
            ResetAnimatorSpeed();
            StopAudio();
        }

        private void OnDestroy()
        {
            StopMiningRoutine();
            ResetAnimatorSpeed();
            StopAudio();
        }

        public void Configure(Animator animator, float moveStartupSeconds = 2f)
        {
            drillAnimator = animator;
            startupMoveSeconds = Mathf.Max(0.05f, moveStartupSeconds);
            EnsureAudioSource();
        }

        public void SetAudioClips(AudioClip moveClip, AudioClip spinClip)
        {
            moveAudioClip = moveClip;
            spinAudioClip = spinClip;
        }

        public void StartMining()
        {
            if (IsMining)
                return;

            StopMiningRoutine();
            miningRoutine = StartCoroutine(MiningSequence());
        }

        public void StopMining()
        {
            if (phase == MiningPhase.Idle || phase == MiningPhase.Retracting)
                return;

            bool fromSpin = phase == MiningPhase.Spinning;
            float startNormalized = fromSpin
                ? 1f
                : Mathf.Clamp(GetMoveNormalizedTime(), 0.02f, 1f);

            StopMiningRoutine();
            miningRoutine = StartCoroutine(RetractSequence(startNormalized));
        }

        private void StopMiningRoutine()
        {
            if (miningRoutine != null)
            {
                StopCoroutine(miningRoutine);
                miningRoutine = null;
            }
        }

        private IEnumerator MiningSequence()
        {
            ResetAnimatorSpeed();
            phase = MiningPhase.Starting;
            CrossFadeState(MoveState);
            PlayMoveAudio(pitch: 1f, loopIfShorterThan: startupMoveSeconds);
            yield return new WaitForSeconds(startupMoveSeconds);

            if (phase != MiningPhase.Starting)
                yield break;

            phase = MiningPhase.Spinning;
            StopAudio();
            CrossFadeState(SpinState);
            PlaySpinAudio();
            miningRoutine = null;
        }

        private IEnumerator RetractSequence(float startNormalizedTime)
        {
            phase = MiningPhase.Retracting;
            StopAudio();

            float clipLength = GetMoveClipLength();
            float maxWait = clipLength * 2f + 0.25f;

            if (drillAnimator != null)
                drillAnimator.speed = RetractSpeed;
            PlayMoveStateAt(startNormalizedTime);

            PlayMoveAudio(pitch: RetractAudioPitch, loopIfShorterThan: maxWait);

            int moveHash = Animator.StringToHash(MoveState);
            bool reachedMove = false;
            float elapsed = 0f;
            while (elapsed < maxWait)
            {
                if (phase != MiningPhase.Retracting)
                    yield break;

                if (drillAnimator != null)
                {
                    AnimatorStateInfo current = drillAnimator.GetCurrentAnimatorStateInfo(0);
                    if (current.shortNameHash == moveHash)
                    {
                        reachedMove = true;
                        if (UnwrapNormalized(current.normalizedTime) <= RetractDoneNormalized)
                            break;
                    }
                    else if (reachedMove)
                    {
                        break;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            EnterIdle();
            miningRoutine = null;
        }

        private void EnterIdle()
        {
            phase = MiningPhase.Idle;
            ResetAnimatorSpeed();
            StopAudio();
            CrossFadeState(IdleState);
        }

        private void PlayMoveStateAt(float normalizedTime)
        {
            if (drillAnimator == null)
                return;

            if (!drillAnimator.isActiveAndEnabled)
                drillAnimator.enabled = true;

            float t = Mathf.Clamp(normalizedTime, 0.02f, 1f);
            int hash = Animator.StringToHash(MoveState);
            if (drillAnimator.HasState(0, hash))
                drillAnimator.Play(hash, 0, t);
            else
                drillAnimator.Play(MoveState, 0, t);

            drillAnimator.Update(0f);
        }

        private void CrossFadeState(string stateName)
        {
            if (drillAnimator == null || string.IsNullOrWhiteSpace(stateName))
                return;

            if (!drillAnimator.isActiveAndEnabled)
                drillAnimator.enabled = true;

            int hash = Animator.StringToHash(stateName);
            if (drillAnimator.HasState(0, hash))
            {
                drillAnimator.CrossFade(hash, crossFadeSeconds, 0, 0f);
                return;
            }

            drillAnimator.Play(stateName, 0, 0f);
        }

        private float GetMoveNormalizedTime()
        {
            return Mathf.Clamp(GetMoveNormalizedTimeRaw(), 0f, 1f);
        }

        private float GetMoveNormalizedTimeRaw()
        {
            if (drillAnimator == null)
                return 0f;

            int moveHash = Animator.StringToHash(MoveState);
            AnimatorStateInfo next = drillAnimator.GetNextAnimatorStateInfo(0);
            if (drillAnimator.IsInTransition(0) && next.shortNameHash == moveHash)
                return UnwrapNormalized(next.normalizedTime);

            AnimatorStateInfo current = drillAnimator.GetCurrentAnimatorStateInfo(0);
            if (current.shortNameHash == moveHash)
                return UnwrapNormalized(current.normalizedTime);

            return UnwrapNormalized(current.normalizedTime);
        }

        /// <summary>
        /// 1.0 is the end of a non-looping clip — do not wrap it to 0 (that would skip retract).
        /// </summary>
        private static float UnwrapNormalized(float normalizedTime)
        {
            if (normalizedTime < 0f)
                return 0f;
            if (normalizedTime <= 1.0001f)
                return Mathf.Min(normalizedTime, 1f);
            return normalizedTime - Mathf.Floor(normalizedTime);
        }

        private float GetMoveClipLength()
        {
            if (drillAnimator != null && drillAnimator.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = drillAnimator.runtimeAnimatorController.animationClips;
                if (clips != null)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        AnimationClip clip = clips[i];
                        if (clip == null || clip.length < 0.05f)
                            continue;

                        string clipName = clip.name;
                        if (clipName.IndexOf("Move", System.StringComparison.OrdinalIgnoreCase) >= 0
                            && clipName.IndexOf("Spin", System.StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            return clip.length;
                        }
                    }
                }
            }

            if (drillAnimator != null)
            {
                float stateLength = drillAnimator.GetCurrentAnimatorStateInfo(0).length;
                if (stateLength > 0.05f)
                    return stateLength;
            }

            return Mathf.Max(0.25f, startupMoveSeconds);
        }

        private void ResetAnimatorSpeed()
        {
            if (drillAnimator != null)
                drillAnimator.speed = 1f;
        }

        private void PlayMoveAudio(float pitch, float loopIfShorterThan)
        {
            EnsureAudioSource();
            if (drillAudioSource == null || moveAudioClip == null)
                return;

            bool loop = moveAudioClip.length + 0.01f < loopIfShorterThan;
            drillAudioSource.Stop();
            drillAudioSource.clip = moveAudioClip;
            drillAudioSource.loop = loop;
            drillAudioSource.pitch = pitch;
            drillAudioSource.volume = AudioVolume;
            drillAudioSource.Play();
        }

        private void PlaySpinAudio()
        {
            EnsureAudioSource();
            if (drillAudioSource == null || spinAudioClip == null)
                return;

            drillAudioSource.Stop();
            drillAudioSource.clip = spinAudioClip;
            drillAudioSource.loop = true;
            drillAudioSource.pitch = 1f;
            drillAudioSource.volume = AudioVolume;
            drillAudioSource.Play();
        }

        private void StopAudio()
        {
            if (drillAudioSource != null && drillAudioSource.isPlaying)
                drillAudioSource.Stop();
        }

        public void EnsureAudioSource()
        {
            if (drillAudioSource == null)
                drillAudioSource = GetComponent<AudioSource>();
            if (drillAudioSource == null)
                drillAudioSource = GetComponentInChildren<AudioSource>();
            if (drillAudioSource == null)
                drillAudioSource = gameObject.AddComponent<AudioSource>();

            drillAudioSource.playOnAwake = false;
            drillAudioSource.spatialBlend = 1f;
            drillAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            drillAudioSource.minDistance = 3f;
            drillAudioSource.maxDistance = 28f;
            drillAudioSource.volume = AudioVolume;
            drillAudioSource.loop = false;
        }
    }
}
