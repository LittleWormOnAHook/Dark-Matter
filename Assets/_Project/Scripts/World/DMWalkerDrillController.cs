using System.Collections;
using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// Walker Drill mining animation driver: Move for startup, then looping Spin until stopped.
    /// Animator states are expected to be named Idle, Move, and Spin.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMWalkerDrillController : MonoBehaviour
    {
        public const string IdleState = "Idle";
        public const string MoveState = "Move";
        public const string SpinState = "Spin";

        [Header("References")]
        [SerializeField] private Animator drillAnimator;

        [Header("Timing")]
        [SerializeField] private float startupMoveSeconds = 2f;
        [SerializeField] private float crossFadeSeconds = 0.12f;

        private enum MiningPhase
        {
            Idle = 0,
            Starting = 1,
            Spinning = 2
        }

        private MiningPhase phase = MiningPhase.Idle;
        private Coroutine miningRoutine;

        public bool IsMining => phase != MiningPhase.Idle;
        public bool IsSpinning => phase == MiningPhase.Spinning;

        private void Awake()
        {
            if (drillAnimator == null)
                drillAnimator = GetComponent<Animator>();
            if (drillAnimator == null)
                drillAnimator = GetComponentInChildren<Animator>();
        }

        public void Configure(Animator animator, float moveStartupSeconds = 2f)
        {
            drillAnimator = animator;
            startupMoveSeconds = Mathf.Max(0.05f, moveStartupSeconds);
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
            StopMiningRoutine();
            phase = MiningPhase.Idle;
            CrossFadeState(IdleState);
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
            phase = MiningPhase.Starting;
            CrossFadeState(MoveState);
            yield return new WaitForSeconds(startupMoveSeconds);

            if (phase != MiningPhase.Starting)
                yield break;

            phase = MiningPhase.Spinning;
            CrossFadeState(SpinState);
            miningRoutine = null;
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
    }
}
