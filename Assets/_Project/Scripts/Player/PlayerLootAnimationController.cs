using ECM2;
using Project.Interaction;
using Project.Survival;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Plays BasicMotions loot Start → Loop → End on item pickup. Cancels into End on movement or combat input.
    /// </summary>
    [DefaultExecutionOrder(-20)]
    public class PlayerLootAnimationController : MonoBehaviour
    {
        public const string LootStartStateName = "LootStart";
        public const string LootLoopStateName = "LootLoop";
        public const string LootEndStateName = "LootEnd";

        [SerializeField] private float blendTime = 0.08f;
        [SerializeField] private float lootPlaybackSpeed = 1.65f;
        [SerializeField] private float startToLoopNormalizedTime = 0.78f;
        [SerializeField] private int loopCyclesBeforeEnd = 1;
        [SerializeField] private float endToLocomotionNormalizedTime = 0.78f;
        [SerializeField] private float cancelMoveSpeedThreshold = 0.2f;

        private enum LootPhase
        {
            None,
            Start,
            Loop,
            End
        }

        private Character _character;
        private PlayerController _playerController;
        private SurvivalStats _survivalStats;
        private MeleeCombatController _melee;
        private Animator _animator;
        private LootPhase _phase = LootPhase.None;
        private int _loopCyclesCompleted;
        private float _savedAnimatorSpeed = 1f;

        public bool IsLooting => _phase != LootPhase.None;
        public bool IsLootingActive => _phase == LootPhase.Start || _phase == LootPhase.Loop;

        private void Awake()
        {
            _character = GetComponentInParent<Character>();
            _playerController = GetComponentInParent<PlayerController>();
            _survivalStats = GetComponentInParent<SurvivalStats>();
            _melee = GetComponentInParent<MeleeCombatController>();
            _animator = _character != null ? _character.GetAnimator() : GetComponent<Animator>();
        }

        public void BeginLoot()
        {
            if (_animator == null || (_survivalStats != null && _survivalStats.IsDead))
                return;

            if (!HasLootState(LootStartStateName))
                return;

            _phase = LootPhase.Start;
            _loopCyclesCompleted = 0;
            _savedAnimatorSpeed = _animator.speed;
            _animator.speed = lootPlaybackSpeed;
            CrossFadeState(LootStartStateName);
        }

        /// <summary>
        /// Immediately returns the base layer to locomotion. Used when block/attack would overlap loot poses.
        /// </summary>
        public void CancelForCombat()
        {
            if (_animator == null)
                return;

            if (_phase == LootPhase.None && !IsInLootStateOnBaseLayer())
                return;

            FinishLoot();
        }

        private void OnDisable()
        {
            if (_animator != null)
                _animator.speed = _savedAnimatorSpeed;
        }

        private void Update()
        {
            if (_phase == LootPhase.None || _animator == null)
                return;

            if (ShouldCancelLoot())
            {
                if (IsCombatInterrupt())
                    FinishLoot();
                else
                    BeginEndPhase();
                return;
            }

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

            switch (_phase)
            {
                case LootPhase.Start:
                    if (state.IsName(LootStartStateName) && state.normalizedTime >= startToLoopNormalizedTime)
                        BeginLoopPhase();
                    break;

                case LootPhase.Loop:
                    if (!state.IsName(LootLoopStateName))
                        break;

                    if (state.normalizedTime >= 1f)
                    {
                        _loopCyclesCompleted++;
                        if (_loopCyclesCompleted >= Mathf.Max(1, loopCyclesBeforeEnd))
                            BeginEndPhase();
                    }

                    break;

                case LootPhase.End:
                    if (state.IsName(LootEndStateName) && state.normalizedTime >= endToLocomotionNormalizedTime)
                        FinishLoot();
                    else if (!HasLootState(LootEndStateName) && state.normalizedTime >= endToLocomotionNormalizedTime)
                        FinishLoot();
                    break;
            }
        }

        private void BeginLoopPhase()
        {
            if (!HasLootState(LootLoopStateName))
            {
                BeginEndPhase();
                return;
            }

            _phase = LootPhase.Loop;
            CrossFadeState(LootLoopStateName);
        }

        private void BeginEndPhase()
        {
            if (_phase == LootPhase.End)
                return;

            _phase = LootPhase.End;

            if (HasLootState(LootEndStateName))
            {
                CrossFadeState(LootEndStateName);
                return;
            }

            FinishLoot();
        }

        private void FinishLoot()
        {
            _phase = LootPhase.None;
            if (_animator != null)
                _animator.speed = _savedAnimatorSpeed;
            CrossFadeState(GkcAnimatorConstants.GroundedState);
        }

        private bool ShouldCancelLoot()
        {
            if (_phase == LootPhase.End && !IsCombatInterrupt())
                return false;

            if (_survivalStats != null && _survivalStats.IsDead)
                return true;

            if (_playerController != null)
            {
                if (_playerController.MoveInput.sqrMagnitude > 0.01f)
                    return true;

                if (_playerController.BlocksCombatInput && !_playerController.IsInventoryOpen)
                    return true;
            }

            if (_character != null && _character.GetSpeed() > cancelMoveSpeedThreshold)
                return true;

            if (_character != null && !_character.IsGrounded())
                return true;

            return IsCombatInterrupt();
        }

        private bool IsCombatInterrupt()
        {
            if (_melee != null && (_melee.IsBlocking || _melee.IsAttackInputActive))
                return true;

            PlayerGkcAnimatorDriver driver = GetComponentInParent<PlayerGkcAnimatorDriver>();
            return driver != null && driver.IsActionBlockingLocomotion;
        }

        private bool IsInLootStateOnBaseLayer()
        {
            if (_animator == null)
                return false;

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            return state.IsName(LootStartStateName)
                || state.IsName(LootLoopStateName)
                || state.IsName(LootEndStateName);
        }

        private bool HasLootState(string stateName)
        {
            return _animator != null && _animator.HasState(0, Animator.StringToHash(stateName));
        }

        private void CrossFadeState(string stateName)
        {
            if (_animator == null || string.IsNullOrEmpty(stateName))
                return;

            int hash = Animator.StringToHash(stateName);
            if (!_animator.HasState(0, hash))
                return;

            _animator.CrossFadeInFixedTime(stateName, blendTime, 0, 0f);
        }
    }
}
