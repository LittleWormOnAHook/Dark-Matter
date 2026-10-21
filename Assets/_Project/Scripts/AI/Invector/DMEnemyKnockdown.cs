using System.Collections.Generic;
using Project.Combat;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Slice 2b.3: animation-only knockdown (NO ragdoll - ragdoll is death only). Plays on the controller's DM_KnockDown
    /// layer (override, no mask, weight 1 only while down):
    ///   KD_Fall_{F,B,L,R} (PROTOFACTOR Death* cut at the land point) -> KD_Down_* (Dead* lying hold) -> KD_GetUp_* (GetBackUp*),
    /// states named by the fall direction; the enemy falls along the strike (struck in front = falls backward). The weapon stays parented to the hand exactly as placed on the prefab.
    /// While down: AI locomotion paused, swing locked (EnemyAiController / EnemyCombat via the public hit recovery), melee
    /// hitboxes off, aim stance suppressed (EnemyInvectorCombatBridge), token / attack slot released (IsTokenEligible),
    /// hits deal damage but add no reactions and do not extend the down time. At the get-up exit the layer blends out over
    /// knockdownAimFadeInSeconds, which also fades the aim / grip pose back in. Death while down aborts cleanly and the
    /// normal death presentation runs. Runtime-added, no prefab edits. EnemyInvectorRagdollBridge is not involved.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMEnemyKnockdown : MonoBehaviour
    {
        public const string LayerName = "DM_KnockDown";
        public const string SpeedParam = "DM_KDSpeed";
        private static readonly int SpeedHash = Animator.StringToHash(SpeedParam);
        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int CanAimHash = Animator.StringToHash("CanAim");
        private const float MaxSequenceSeconds = 15f;

        private static readonly HashSet<GameObject> s_down = new HashSet<GameObject>();
        private static readonly HashSet<GameObject> s_invulnerable = new HashSet<GameObject>();

        private enum Phase
        {
            None,
            Fall,
            Down,
            GetUp,
            Recover
        }

        private Animator _animator;
        private EnemyHealth _health;
        private EnemyAiController _ai;
        private EnemyInvectorRagdollBridge _bridge;
        private bool _resolved;
        private int _layer = -1;
        private bool _hasIsAiming;
        private bool _hasCanAim;
        private Phase _phase;
        private DMHitSide _side;
        private float _phaseStart;
        private float _sequenceStart;
        private float _downUntil;
        private float _weight;
        private float _immuneUntil = float.NegativeInfinity;
        private int _fallHash;
        private int _downHash;
        private int _getUpHash;
        private float _getUpLength = 3f;
        private float _fallLength = 3f;
        private GameObject _id;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_down.Clear();
            s_invulnerable.Clear();
        }

        /// <summary>True while this enemy is falling, lying or getting up (until control returns).</summary>
        public static bool IsDown(GameObject enemy)
        {
            return enemy != null && s_down.Count > 0 && s_down.Contains(enemy);
        }

        /// <summary>Last part of the get-up (getUpInvulnerableSeconds): player melee deals no damage.</summary>
        public static bool IsInvulnerable(GameObject enemy)
        {
            return enemy != null && s_invulnerable.Count > 0 && s_invulnerable.Contains(enemy);
        }

        public static DMEnemyKnockdown Ensure(GameObject enemy)
        {
            if (enemy == null)
                return null;

            DMEnemyKnockdown kd = enemy.GetComponent<DMEnemyKnockdown>();
            return kd != null ? kd : enemy.AddComponent<DMEnemyKnockdown>();
        }

        public bool IsSupported => Resolve();
        public bool IsActive => _phase != Phase.None;
        public bool IsImmune => Time.time < _immuneUntil;

        /// <summary>Ground under the fall is flat enough and there is no ledge in the push direction.</summary>
        public bool GroundAllowsKnockdown(Vector3 pushDirection, DM_CombatCoreProfile p)
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.5f, ~0, QueryTriggerInteraction.Ignore) ||
                hit.transform.IsChildOf(transform))
                return EnemyGroundUtility.TryGetGroundY(transform.position, out _, 0f); // own collider in the way: trust the utility

            if (Vector3.Angle(hit.normal, Vector3.up) > p.knockdownMaxSlopeDegrees)
                return false;

            Vector3 dir = pushDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return true;

            Vector3 ahead = transform.position + dir.normalized * p.knockdownLedgeCheckMeters;
            if (!EnemyGroundUtility.TryGetGroundY(ahead, out float aheadY, 0f, transform, float.NegativeInfinity))
                return false;

            return transform.position.y - aheadY <= Mathf.Max(0.1f, p.hitSlideMaxLedgeDrop);
        }

        /// <summary>Starts the knockdown. False when unsupported, dead, already down or immune.</summary>
        public bool TryBegin(DMHitSide side, Vector3 pushDirection, DM_CombatCoreProfile p)
        {
            if (p == null || !Resolve() || _phase != Phase.None || IsImmune)
                return false;

            if (_health != null && _health.IsDead)
                return false;

            if (_bridge != null && (_bridge.HasActiveRagdoll || _bridge.IsCorpseRagdolled))
                return false;

            _side = side;
            string tag = DMHitReactionCore.FallTag(side);
            _fallHash = Animator.StringToHash("KD_Fall_" + tag);
            _downHash = Animator.StringToHash("KD_Down_" + tag);
            _getUpHash = Animator.StringToHash("KD_GetUp_" + tag);
            _fallLength = ClipLength("DM_KD_Fall_" + tag, 3f);
            _getUpLength = ClipLength("DM_KD_GetUp_" + tag, 3f);

            DMEnemyHitFlinch flinch = GetComponent<DMEnemyHitFlinch>();
            if (flinch != null)
                flinch.Cancel();
            DMEnemyHitReactionPlayer clips = GetComponent<DMEnemyHitReactionPlayer>();
            if (clips != null)
                clips.Cancel();

            _id = gameObject;
            s_down.Add(_id);
            _phase = Phase.Fall;
            _phaseStart = Time.time;
            _sequenceStart = Time.time;
            _weight = 0f;

            float fallSeconds = _fallLength * FallExit(p) / Mathf.Max(0.1f, p.knockdownFallSpeed);
            float getUpSeconds = _getUpLength * p.getUpExitNormalized / Mathf.Max(0.1f, p.getUpPlaybackSpeed);
            float total = fallSeconds + p.knockdownDownSeconds + p.knockdownDownRandomSeconds + getUpSeconds;

            // Cancel any swing and lock attacks for the whole sequence (public hit recovery = attack lockout only).
            if (_bridge != null)
                _bridge.ApplyHitRecovery(total + 0.3f);
            EnemyInvectorCombatShutdown.DisableMeleeBeforeAnimatorEvents(gameObject);
            HoldStill();
            SuppressAim();

            _animator.SetFloat(SpeedHash, Mathf.Max(0.1f, p.knockdownFallSpeed));
            _animator.CrossFadeInFixedTime(_fallHash, 0.08f, _layer, 0f);
            _animator.SetLayerWeight(_layer, 0f);

            if (p.knockdownPushMeters > 0.001f)
                DMEnemyHitSlide.Begin(gameObject, pushDirection, p.knockdownPushMeters, p.knockdownPushSeconds, 1, p.hitSlideMaxLedgeDrop, p.hitSlideMaxTotalMeters);

            if (p.logHitReactionsInPlay)
                Debug.Log($"[Knockdown] {name} begin side={side} fall={fallSeconds:0.00}s total~{total:0.00}s", this);
            return true;
        }

        private void Awake()
        {
            Resolve();
            if (GetComponent<DMEnemyAnimStallGuard>() == null)
                gameObject.AddComponent<DMEnemyAnimStallGuard>(); // 2026-10-20o failsafe
            if (GetComponent<DMEnemyBodyWeight>() == null) gameObject.AddComponent<DMEnemyBodyWeight>(); // 2026-10-20w body weight
        }

        private void OnEnable()
        {
            if (_health == null)
                _health = GetComponent<EnemyHealth>();
            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Died -= OnDied;
            Abort(resumeAi: true);
        }

        private void OnDied()
        {
            // Death presentation (corpse ragdoll / death clip) runs exactly as today.
            Abort(resumeAi: false);
        }

        private void Update()
        {
            if (_phase == Phase.None)
                return;

            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || _animator == null || !_animator.isActiveAndEnabled || (_health != null && _health.IsDead) ||
                (_bridge != null && _bridge.HasActiveRagdoll) || Time.time - _sequenceStart > MaxSequenceSeconds)
            {
                Abort(resumeAi: _health == null || !_health.IsDead);
                return;
            }

            float now = Time.time;
            if (_phase != Phase.Recover)
            {
                HoldStill();
                SuppressAim();
                _weight = Mathf.MoveTowards(_weight, 1f, Time.deltaTime / 0.08f);
            }

            switch (_phase)
            {
                case Phase.Fall:
                    if (StateNormalized(_fallHash, out float fallN) && fallN >= FallExit(p) ||
                        now - _phaseStart > _fallLength / Mathf.Max(0.1f, p.knockdownFallSpeed) + 0.5f)
                    {
                        _animator.SetFloat(SpeedHash, 1f);
                        _animator.CrossFadeInFixedTime(_downHash, 0.25f, _layer, 0f);
                        _phase = Phase.Down;
                        _phaseStart = now;
                        _downUntil = now + Mathf.Max(0.1f, p.knockdownDownSeconds + Random.Range(-p.knockdownDownRandomSeconds, p.knockdownDownRandomSeconds));
                    }
                    break;

                case Phase.Down:
                    if (now >= _downUntil)
                    {
                        _animator.SetFloat(SpeedHash, Mathf.Max(0.1f, p.getUpPlaybackSpeed));
                        _animator.CrossFadeInFixedTime(_getUpHash, 0.2f, _layer, 0f);
                        _phase = Phase.GetUp;
                        _phaseStart = now;
                    }
                    break;

                case Phase.GetUp:
                {
                    float speed = Mathf.Max(0.1f, p.getUpPlaybackSpeed);
                    bool known = StateNormalized(_getUpHash, out float upN);
                    float exitN = Mathf.Clamp(p.getUpExitNormalized, 0.5f, 1f);
                    float remaining = known ? (exitN - upN) * _getUpLength / speed : float.PositiveInfinity;
                    if (remaining <= p.getUpInvulnerableSeconds)
                        s_invulnerable.Add(_id);

                    if (known && upN >= exitN || now - _phaseStart > _getUpLength / speed + 0.5f)
                    {
                        _phase = Phase.Recover;
                        _phaseStart = now;
                        s_down.Remove(_id); // control returns: aim stance, token eligibility and reactions resume
                        s_invulnerable.Remove(_id);
                        ResumeAi();
                    }
                    break;
                }

                case Phase.Recover:
                {
                    float fade = Mathf.Max(0.02f, p.knockdownAimFadeInSeconds);
                    _weight = Mathf.MoveTowards(_weight, 0f, Time.deltaTime / fade);
                    if (_weight <= 0.0001f)
                    {
                        Finish(p);
                        return;
                    }
                    break;
                }
            }

            _animator.SetLayerWeight(_layer, _weight);
        }

        private void Finish(DM_CombatCoreProfile p)
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

            _immuneUntil = Time.time + (p != null ? p.knockdownImmunitySeconds : 0f);
            if (p != null && p.logHitReactionsInPlay)
                Debug.Log($"[Knockdown] {name} up after {Time.time - _sequenceStart:0.00}s", this);
        }

        private void Abort(bool resumeAi)
        {
            if (_phase == Phase.None)
                return;

            bool wasDown = _phase != Phase.Recover;
            _phase = Phase.None;
            _weight = 0f;
            s_down.Remove(_id);
            s_invulnerable.Remove(_id);
            if (_animator != null && _layer >= 0)
            {
                _animator.SetLayerWeight(_layer, 0f);
                _animator.SetFloat(SpeedHash, 1f);
            }

            if (resumeAi && wasDown)
                ResumeAi();
        }

        private void HoldStill()
        {
            if (_ai != null)
                _ai.SetLocomotionPaused(true);
        }

        private void ResumeAi()
        {
            if (_health != null && _health.IsDead)
                return;

            // A legacy stagger routine that is still running owns the pause; it releases it when it ends.
            if (_ai != null && (_bridge == null || !_bridge.IsHitStaggerActive))
                _ai.SetLocomotionPaused(false);
            EnemyInvectorCombatShutdown.EnableMeleeIfSafe(gameObject);
        }

        private void SuppressAim()
        {
            if (_animator == null)
                return;

            if (_hasIsAiming)
                _animator.SetBool(IsAimingHash, false);
            if (_hasCanAim)
                _animator.SetBool(CanAimHash, false);
        }

        private bool StateNormalized(int stateHash, out float normalized)
        {
            normalized = 0f;
            if (_animator.IsInTransition(_layer))
            {
                AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(_layer);
                if (next.shortNameHash != stateHash)
                    return false;
                normalized = next.normalizedTime;
                return true;
            }

            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(_layer);
            if (info.shortNameHash != stateHash)
                return false;
            normalized = info.normalizedTime;
            return true;
        }

        // 2026-10-19 d: shared with the player via DMHitReactionCore (moved verbatim).
        private float FallExit(DM_CombatCoreProfile p) => DMHitReactionCore.FallExit(p, _side);

        private float ClipLength(string clipName, float fallback)
        {
            RuntimeAnimatorController rac = _animator != null ? _animator.runtimeAnimatorController : null;
            if (rac == null)
                return fallback;

            AnimationClip[] clips = rac.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].name == clipName)
                    return Mathf.Max(0.1f, clips[i].length);
            }

            return fallback;
        }

        private bool Resolve()
        {
            if (_resolved)
                return _layer >= 0;

            _resolved = true;
            _health = _health != null ? _health : GetComponent<EnemyHealth>();
            _ai = GetComponent<EnemyAiController>();
            _bridge = GetComponent<EnemyInvectorRagdollBridge>();
            _animator = GetComponent<Animator>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null || !_animator.isHuman || _animator.runtimeAnimatorController == null)
                return false;

            _layer = _animator.GetLayerIndex(LayerName);
            foreach (AnimatorControllerParameter param in _animator.parameters)
            {
                if (param.type != AnimatorControllerParameterType.Bool)
                    continue;
                if (param.nameHash == IsAimingHash)
                    _hasIsAiming = true;
                else if (param.nameHash == CanAimHash)
                    _hasCanAim = true;
            }

            return _layer >= 0;
        }
    }
}
