using System.Collections.Generic;
using Invector;
using Invector.vCharacterController;
using Project.AI.Invector;
using Project.Combat;
using Project.Survival;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player.Invector
{
    /// <summary>
    /// 2026-10-19 d: player knockdown (animation only, NO ragdoll) on the player's DM_KnockDown layer, same states and
    /// shared math as the enemy DMEnemyKnockdown (DMHitReactionCore.FallTag / FallExit): KD_Fall -> KD_Down -> KD_GetUp.
    /// While active: movement/rotation/melee locked, incoming damage x playerDownedDamageScale; roll-cancel (Q / pad East)
    /// after playerRollCancelAfterSeconds; invulnerable for playerGetUpIFrameSeconds from get-up start.
    /// Runtime-added by DMPlayerHitReaction; no prefab edits. Capsule / physics untouched.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(510)] // after PioneerShooterMeleeInput (500) so the input zeroing wins
    public sealed class DMPlayerKnockdown : MonoBehaviour
    {
        private const float MaxSequenceSeconds = 8f;
        private static readonly int SpeedHash = Animator.StringToHash(DMEnemyKnockdown.SpeedParam);

        public static int StatKnockdowns;
        public static int StatRollCancels;

        private enum Phase { None, Fall, Down, GetUp, Recover }

        private vThirdPersonController _cc;
        private PioneerShooterMeleeInput _melee;
        private SurvivalStats _stats;
        private Animator _animator;
        private int _layer = -1;
        private bool _resolved;
        private Phase _phase;
        private DMHitSide _side;
        private int _fallHash, _downHash, _getUpHash;
        private float _fallLength, _getUpLength;
        private float _phaseStart, _sequenceStart, _downUntil, _invulnerableUntil, _weight;
        private bool _savedLockMovement, _savedLockRotation, _savedLockMelee;

        public bool IsActive => _phase != Phase.None && _phase != Phase.Recover;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public bool IsSupported => Resolve();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            StatKnockdowns = 0;
            StatRollCancels = 0;
        }

        public static DMPlayerKnockdown Ensure(GameObject player)
        {
            if (player == null)
                return null;
            DMPlayerKnockdown kd = player.GetComponent<DMPlayerKnockdown>();
            return kd != null ? kd : player.AddComponent<DMPlayerKnockdown>();
        }

        /// <summary>Called by PioneerInvectorSurvivalBridge before damage is applied: zero while invulnerable, scaled while down.</summary>
        public static void FilterIncomingDamage(GameObject player, vDamage damage)
        {
            if (player == null || damage == null)
                return;
            DMPlayerKnockdown kd = player.GetComponent<DMPlayerKnockdown>();
            if (kd == null)
                return;
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (kd.IsInvulnerable)
                damage.damageValue = 0f;
            else if (kd.IsActive && p != null)
                damage.damageValue *= Mathf.Clamp01(p.playerDownedDamageScale);
        }

        public bool TryBegin(DMHitSide side, DM_CombatCoreProfile p)
        {
            if (p == null || !Resolve() || _phase != Phase.None || _cc == null || (_stats != null && _stats.IsDead))
                return false;

            _side = side;
            string tag = DMHitReactionCore.FallTag(side);
            _fallHash = Animator.StringToHash("KD_Fall_" + tag);
            _downHash = Animator.StringToHash("KD_Down_" + tag);
            _getUpHash = Animator.StringToHash("KD_GetUp_" + tag);
            _fallLength = DMHitReactionCore.ClipLength(_animator, "DM_KD_Fall_" + tag, 3f);
            _getUpLength = DMHitReactionCore.ClipLength(_animator, "DM_KD_GetUp_" + tag, 3f);

            DMEnemyHitFlinch flinch = GetComponent<DMEnemyHitFlinch>();
            if (flinch != null)
                flinch.Cancel();
            DMEnemyHitReactionPlayer clips = GetComponent<DMEnemyHitReactionPlayer>();
            if (clips != null)
                clips.Cancel();

            _savedLockMovement = _cc.lockMovement;
            _savedLockRotation = _cc.lockRotation;
            _savedLockMelee = _melee != null && _melee.lockMeleeInput;
            _phase = Phase.Fall;
            _phaseStart = _sequenceStart = Time.time;
            _weight = 0f;
            HoldStill();

            _animator.SetFloat(SpeedHash, Mathf.Max(0.1f, p.knockdownFallSpeed));
            _animator.CrossFadeInFixedTime(_fallHash, 0.08f, _layer, 0f);
            _animator.SetLayerWeight(_layer, 0f);
            StatKnockdowns++;
            if (p.logPlayerHitReactions)
                Debug.Log($"[PlayerHitReaction] knockdown side={side} fall={tag}", this);
            return true;
        }

        private void Update()
        {
            if (_phase == Phase.None)
                return;

            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || _animator == null || !_animator.isActiveAndEnabled || (_stats != null && _stats.IsDead) ||
                (_cc != null && (_cc.isDead || _cc.ragdolled)) || Time.time - _sequenceStart > MaxSequenceSeconds)
            {
                Abort();
                return;
            }

            float now = Time.time;
            if (_phase != Phase.Recover)
            {
                HoldStill();
                _weight = Mathf.MoveTowards(_weight, 1f, Time.deltaTime / 0.08f);

                if (p.playerRollCancel && now - _sequenceStart >= p.playerRollCancelAfterSeconds && RollPressed())
                {
                    Abort();
                    _invulnerableUntil = now + p.playerGetUpIFrameSeconds;
                    if (_cc != null && _cc.isGrounded)
                        _cc.Roll();
                    StatRollCancels++;
                    if (p.logPlayerHitReactions)
                        Debug.Log("[PlayerHitReaction] knockdown roll-cancel", this);
                    return;
                }
            }

            switch (_phase)
            {
                case Phase.Fall:
                    if (StateNormalized(_fallHash, out float fallN) && fallN >= DMHitReactionCore.FallExit(p, _side) ||
                        now - _phaseStart > _fallLength / Mathf.Max(0.1f, p.knockdownFallSpeed) + 0.5f)
                    {
                        _animator.SetFloat(SpeedHash, 1f);
                        _animator.CrossFadeInFixedTime(_downHash, 0.25f, _layer, 0f);
                        _phase = Phase.Down;
                        _phaseStart = now;
                        _downUntil = now + Mathf.Max(0.1f, DMHitReactionCore.RandomRange(p.playerDownHoldSecondsMin, p.playerDownHoldSecondsMax));
                    }
                    break;

                case Phase.Down:
                    if (now >= _downUntil)
                    {
                        _animator.SetFloat(SpeedHash, Mathf.Max(0.1f, p.getUpPlaybackSpeed));
                        _animator.CrossFadeInFixedTime(_getUpHash, 0.2f, _layer, 0f);
                        _phase = Phase.GetUp;
                        _phaseStart = now;
                        _invulnerableUntil = now + p.playerGetUpIFrameSeconds;
                    }
                    break;

                case Phase.GetUp:
                {
                    float speed = Mathf.Max(0.1f, p.getUpPlaybackSpeed);
                    bool known = StateNormalized(_getUpHash, out float upN);
                    float exitN = Mathf.Clamp(p.getUpExitNormalized, 0.5f, 1f);
                    if (known && upN >= exitN || now - _phaseStart > _getUpLength / speed + 0.5f)
                    {
                        _phase = Phase.Recover;
                        _phaseStart = now;
                        ReleaseLocks();
                    }
                    break;
                }

                case Phase.Recover:
                {
                    float fade = Mathf.Max(0.02f, p.knockdownAimFadeInSeconds);
                    _weight = Mathf.MoveTowards(_weight, 0f, Time.deltaTime / fade);
                    if (_weight <= 0.0001f)
                    {
                        Finish();
                        return;
                    }
                    break;
                }
            }

            _animator.SetLayerWeight(_layer, _weight);
        }

        private void HoldStill()
        {
            if (_cc == null)
                return;
            _cc.lockMovement = true;
            _cc.lockRotation = true;
            _cc.input = Vector3.zero;
            _cc.isSprinting = false;
            if (_melee != null)
                _melee.lockMeleeInput = true;
        }

        private void ReleaseLocks()
        {
            if (_cc != null)
            {
                _cc.lockMovement = _savedLockMovement;
                _cc.lockRotation = _savedLockRotation;
            }
            if (_melee != null)
                _melee.lockMeleeInput = _savedLockMelee;
        }

        private void Finish()
        {
            _phase = Phase.None;
            _weight = 0f;
            if (_animator != null && _layer >= 0)
            {
                _animator.SetLayerWeight(_layer, 0f);
                _animator.SetFloat(SpeedHash, 1f);
                if (_animator.isActiveAndEnabled)
                    _animator.CrossFadeInFixedTime("Empty", 0.05f, _layer);
            }
        }

        private void Abort()
        {
            if (_phase == Phase.None)
                return;
            bool locked = _phase != Phase.Recover;
            if (locked)
                ReleaseLocks();
            Finish();
        }

        private void OnDisable()
        {
            Abort();
            _invulnerableUntil = 0f;
        }

        private static bool RollPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.qKey.wasPressedThisFrame)
                return true;
            Gamepad pad = Gamepad.current;
            return pad != null && pad.buttonEast.wasPressedThisFrame;
        }

        private bool StateNormalized(int hash, out float normalized)
        {
            normalized = 0f;
            AnimatorStateInfo info = _animator.IsInTransition(_layer)
                ? _animator.GetNextAnimatorStateInfo(_layer)
                : _animator.GetCurrentAnimatorStateInfo(_layer);
            if (info.shortNameHash != hash)
                return false;
            normalized = info.normalizedTime;
            return true;
        }

        private bool Resolve()
        {
            if (_resolved)
                return _layer >= 0;
            _resolved = true;
            _cc = GetComponent<vThirdPersonController>();
            _melee = GetComponent<PioneerShooterMeleeInput>();
            _stats = GetComponent<SurvivalStats>();
            _animator = GetComponent<Animator>();
            if (_animator == null || !_animator.isHuman || _animator.runtimeAnimatorController == null)
                return false;
            _layer = _animator.GetLayerIndex(DMEnemyKnockdown.LayerName);
            return _layer >= 0;
        }
    }
}
