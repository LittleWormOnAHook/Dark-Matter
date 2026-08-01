using UnityEngine;
using UnityEngine.AI;

namespace Project.Creatures
{
    /// <summary>
    /// Drives RiggedNative creature Animator via CrossFade (Idle/Walk/Run/Attack/Death).
    /// When Run has no distinct clip, Walk plays at <see cref="runPlaybackSpeed"/>.
    /// Attack/Death always force-restart; locomotion only CrossFades on state change
    /// (re-CrossFading every frame during a transition freezes the clip at the blend start).
    /// </summary>
    [DisallowMultipleComponent]
    public class DMICreatureAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent navAgent;
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private string walkStateName = "Walk";
        [SerializeField] private string runStateName = "Run";
        [SerializeField] private string attackStateName = "Attack";
        [SerializeField] private string deathStateName = "Death";
        [SerializeField] private float crossFadeDuration = 0.12f;
        [SerializeField] private float walkSpeedThreshold = 0.15f;
        [SerializeField] private float runSpeedThreshold = 3.2f;
        [Tooltip("Playback speed when using Walk as a Run substitute (no Run clip).")]
        [SerializeField] private float runPlaybackSpeed = 1.65f;
        [SerializeField] private float attackLockDuration = 0.55f;

        private bool isDead;
        private string currentState;
        private float attackLockUntil;
        private bool runUsesWalkClip;

        /// <summary>Configured attack hold window (seconds) used by melee and spit.</summary>
        public float AttackLockDuration => attackLockDuration;

        /// <summary>True while Attack anim is held after <see cref="PlayAttack"/>.</summary>
        public bool IsAttackLocked => Time.time < attackLockUntil;

        private void Awake()
        {
            CacheRefs();
            DetectRunFallback();
        }

        private void OnEnable()
        {
            CacheRefs();
            DetectRunFallback();
        }

        private void CacheRefs()
        {
            if (animator == null)
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
            if (navAgent == null)
                navAgent = GetComponent<NavMeshAgent>() ?? GetComponentInChildren<NavMeshAgent>(true);
        }

        public void Tick(DMICreatureAiController ai)
        {
            if (animator == null || isDead || ai == null)
                return;

            // Hold Attack through the lock window (melee + spit share Attack).
            if (Time.time < attackLockUntil)
                return;

            // Prefer AI intent speed, but fall back to actual agent motion so we never
            // stick on Idle while the NavMeshAgent is clearly moving.
            float speed = ResolveLocomotionSpeed(ai);
            if (speed < walkSpeedThreshold)
            {
                PlayLocomotion(idleStateName, 1f);
            }
            else if (speed >= runSpeedThreshold)
            {
                if (runUsesWalkClip || !HasAnimState(runStateName))
                    PlayLocomotion(walkStateName, runPlaybackSpeed);
                else
                    PlayLocomotion(runStateName, 1f);
            }
            else
            {
                PlayLocomotion(walkStateName, 1f);
            }
        }

        private float ResolveLocomotionSpeed(DMICreatureAiController ai)
        {
            float speed = ai != null ? ai.CurrentSpeed : 0f;
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                float agentSpeed = navAgent.velocity.magnitude;
                if (agentSpeed > speed)
                    speed = agentSpeed;
            }

            return speed;
        }


        public void ConfigureAttackLock(float durationSeconds)
        {
            attackLockDuration = Mathf.Max(0.05f, durationSeconds);
        }

        public void PlayAttack()
        {
            if (animator == null || isDead)
                return;

            // Ranged (spit) and melee share Attack. Restart every pulse even if already Attack.
            if (HasAnimState(attackStateName))
            {
                ForcePlay(attackStateName, 1f);
                attackLockUntil = Time.time + attackLockDuration;
            }
            else
            {
                PlayLocomotion(idleStateName, 1f);
                attackLockUntil = 0f;
            }
        }

        public void PlayDeath()
        {
            if (animator == null)
                return;

            isDead = true;
            attackLockUntil = 0f;
            if (HasAnimState(deathStateName))
                ForcePlay(deathStateName, 1f);
            else
                ForcePlay(idleStateName, 1f);
        }

        private void DetectRunFallback()
        {
            runUsesWalkClip = false;
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            // If Run state is missing or has no motion, treat Run as sped-up Walk.
            if (!HasAnimState(runStateName))
            {
                runUsesWalkClip = true;
                return;
            }

            RuntimeAnimatorController rac = animator.runtimeAnimatorController;
            AnimationClip[] clips = rac.animationClips;
            // Heuristic: if controller only has Idle/Walk/Attack (no dedicated run clip name), speed up walk.
            bool hasRunNamedClip = false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;
                string n = clips[i].name.ToLowerInvariant();
                if (n.Contains("run") || n.Contains("sprint") || n.Contains("trot"))
                {
                    hasRunNamedClip = true;
                    break;
                }
            }

            if (!hasRunNamedClip && HasAnimState(walkStateName))
                runUsesWalkClip = true;
        }

        /// <summary>
        /// CrossFade into locomotion once per state change. Never re-CrossFade while already
        /// targeting the same state — during a transition GetCurrentAnimatorStateInfo still
        /// reports the previous state, so the old "re-enter if not InExpected" path called
        /// CrossFade every frame and froze the clip at the blend start (looked like Idle).
        /// </summary>
        private void PlayLocomotion(string stateName, float playbackSpeed)
        {
            if (string.IsNullOrWhiteSpace(stateName) || animator == null)
                return;

            if (!HasAnimState(stateName))
                return;

            animator.speed = Mathf.Max(0.01f, playbackSpeed);

            if (currentState == stateName)
            {
                // Only restart if a non-looping locomotion clip finished (rare; Idle/Walk loop).
                if (!animator.IsInTransition(0))
                {
                    AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                    bool inExpected = info.IsName(stateName)
                                      || info.IsName("Base Layer." + stateName);
                    if (inExpected && info.normalizedTime >= 1f && !info.loop)
                        animator.Play(stateName, 0, 0f);
                }

                return;
            }

            animator.CrossFadeInFixedTime(stateName, crossFadeDuration, 0);
            currentState = stateName;
        }


        /// <summary>
        /// Hard restart of a one-shot state (Attack / Death) even if already playing it.
        /// </summary>
        private void ForcePlay(string stateName, float playbackSpeed)
        {
            if (string.IsNullOrWhiteSpace(stateName) || animator == null)
                return;

            if (!HasAnimState(stateName))
                return;

            animator.speed = Mathf.Max(0.01f, playbackSpeed);
            // normalizedTimeOffset 0 = restart from beginning; fade 0 for snappy attack pulses.
            animator.CrossFadeInFixedTime(stateName, 0f, 0, 0f);
            currentState = stateName;
        }

        private bool HasAnimState(string stateName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
                return false;

            int shortHash = Animator.StringToHash(stateName);
            if (animator.HasState(0, shortHash))
                return true;

            // Full path form required by some Unity versions / controllers.
            int pathHash = Animator.StringToHash("Base Layer." + stateName);
            return animator.HasState(0, pathHash);
        }
    }
}
