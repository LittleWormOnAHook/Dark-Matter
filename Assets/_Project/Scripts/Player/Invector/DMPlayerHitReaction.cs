using Invector;
using Invector.vCharacterController;
using Project.AI.Invector;
using Project.Combat;
using Project.Survival;
using Project.Core;
using Project.Features.Climb;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// 2026-10-19 d: DM player hit reactions (replaces the stock Invector TriggerReaction for enemy hits on the player).
    /// Tiers: lean on every hit (shared DMEnemyHitFlinch bone spring), light (HR_Light on DM_HitReact_Add, short melee
    /// lock), heavy (HR_Heavy on DM_HitReact_Body, 0.25 m push, short lock + move slow) and a rare knockdown on heavy hits
    /// only (DMPlayerKnockdown; chance + bonus, capped, with cooldown). Under god mode / audit only lean + light by default.
    /// Shared math: DMHitReactionCore. Runtime-added from PioneerInvectorSurvivalBridge; no prefab edits.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(510)] // after PioneerShooterMeleeInput (500) so the move scale applies to this frame's input
    public sealed class DMPlayerHitReaction : MonoBehaviour
    {
        public enum Result { None, Lean, Light, Heavy, Knockdown }

        public static int StatHits, StatLeans, StatLights, StatHeavies, StatKnockdownRolls;

        private vThirdPersonController _cc;
        private PioneerShooterMeleeInput _melee;
        private SurvivalStats _stats;
        private Rigidbody _rb;
        private CapsuleCollider _capsule;
        private DMEnemyHitFlinch _flinch;
        private DMEnemyHitReactionPlayer _clips;
        private DMPlayerKnockdown _kd;
        private bool _resolved;
        private float _lockUntil, _moveScaleUntil, _knockdownReadyAt;
        private bool _ownsMeleeLock;
        private Vector3 _pushDir;
        private float _pushRemaining, _pushSpeed;

        public bool IsKnockedDown => _kd != null && _kd.IsActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ResetStats();

        public static void ResetStats()
        {
            StatHits = StatLeans = StatLights = StatHeavies = StatKnockdownRolls = 0;
        }

        public static DMPlayerHitReaction Ensure(GameObject player)
        {
            if (player == null)
                return null;
            DMPlayerHitReaction r = player.GetComponent<DMPlayerHitReaction>();
            return r != null ? r : player.AddComponent<DMPlayerHitReaction>();
        }

        /// <summary>
        /// One enemy hit on the player. rolled = the existing stagger roll passed (gates light/heavy); bigRoll = Invector
        /// reaction id 1 (crit or low HP). Returns the tier played.
        /// </summary>
        public Result OnEnemyHit(vDamage damage, bool rolled, bool bigRoll, bool crit)
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || damage == null || !Resolve())
                return Result.None;
            if ((_stats != null && _stats.IsDead) || _cc.isDead || _cc.ragdolled || IsKnockedDown)
                return Result.None;
            if (_melee != null && _melee.isBlocking)
                return Result.None;

            DMPlayerReactionsUnderGodMode godMode = DMDevCommandState.BlocksDamage ? p.playerReactionsUnderGodMode : DMPlayerReactionsUnderGodMode.All;
            if (godMode == DMPlayerReactionsUnderGodMode.None)
                return Result.None;
            StatHits++;

            Vector3 from = damage.sender != null ? damage.sender.position : damage.hitPosition;
            Vector3 dir = transform.position - from;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                dir = -transform.forward;
            dir.Normalize();
            bool ranged = damage.sender != null && Vector3.Distance(damage.sender.position, transform.position) > 3.5f;

            float jitter = DMHitReactionCore.PickJitter(p.playerLeanJitterDegrees, _flinch.LastJitterDegrees, p.avoidRepeatVariation);
            _flinch.LastJitterDegrees = jitter;
            dir = Quaternion.AngleAxis(jitter, Vector3.up) * dir;
            DMHitSide side = DMHitReactionCore.ResolveSide(transform, dir);

            float maxHp = _stats != null ? Mathf.Max(1f, _stats.maxHealth) : 100f;
            float hpRatio = _stats != null ? _stats.CurrentHealth / maxHp : 1f;
            float severity = Mathf.Clamp01(damage.damageValue / Mathf.Max(1f, maxHp * p.playerHeavyDamageFraction));
            float intensity = Mathf.Min(p.playerReactionIntensityMax, 1f + Random.Range(-p.playerIntensityJitter, p.playerIntensityJitter));
            float lean = Mathf.Lerp(p.playerLeanDegreesMin, p.playerLeanDegreesMax, severity) * intensity;
            if (_melee != null && _melee.IsAiming)
                lean *= p.playerAdsLeanScale;
            float twist = (Random.value < 0.5f ? -1f : 1f) * Random.Range(3f, 8f) * intensity;

            bool heavy = rolled && (bigRoll || crit || damage.damageValue >= maxHp * p.playerHeavyDamageFraction) && (!ranged || crit);
            bool light = rolled && !heavy;
            if (godMode == DMPlayerReactionsUnderGodMode.LeanOnly)
                heavy = light = false;
            else if (godMode == DMPlayerReactionsUnderGodMode.LeanAndLight && heavy)
            {
                heavy = false;
                light = true;
            }

            bool canMove = _cc.isGrounded && !_cc.isRolling && !_cc.customAction && !IsClimbing();
            if (heavy && godMode == DMPlayerReactionsUnderGodMode.All && canMove && Time.time >= _knockdownReadyAt)
            {
                StatKnockdownRolls++;
                float chance = p.playerKnockdownChance + (crit || hpRatio < 0.3f ? p.playerKnockdownBonusChance : 0f);
                chance = Mathf.Min(chance, p.playerKnockdownChanceCap);
                if (Random.value < chance && _kd != null && _kd.TryBegin(side, p))
                {
                    _knockdownReadyAt = Time.time + p.playerKnockdownCooldownSeconds;
                    Log(p, "knockdown", side, damage.damageValue, lean);
                    return Result.Knockdown;
                }
            }

            Result result = Result.Lean;
            if (heavy)
            {
                if (_clips.IsSupported)
                {
                    _clips.PickVariation(DMEnemyHitReactionPlayer.Tier.Heavy, false, 0f, p.avoidRepeatVariation, out _, out bool late);
                    float w = p.heavyClipWeight * DMHitReactionCore.RandomRange(p.playerClipWeightMin, p.playerClipWeightMax);
                    float speed = p.heavyClipSpeed * DMHitReactionCore.RandomRange(p.playerClipSpeedMin, p.playerClipSpeedMax);
                    _clips.Play(DMEnemyHitReactionPlayer.Tier.Heavy, side, false, w, speed, DMHitReactionCore.StartOffset(p, late), p.reactionBlendInSeconds, p.reactionBlendOutSeconds);
                }
                if (canMove)
                {
                    float meters = p.playerPushMeters * (Random.value < p.playerStumbleChance ? 1.6f : 1f);
                    StartPush(dir, meters, p.playerPushSeconds);
                }
                Lock(p.playerHeavyLockSeconds, p.playerHeavyLockSeconds + 0.15f);
                lean = Mathf.Max(lean, p.playerLeanDegreesMax);
                StatHeavies++;
                result = Result.Heavy;
            }
            else if (light)
            {
                if (_clips.IsSupported)
                {
                    _clips.PickVariation(DMEnemyHitReactionPlayer.Tier.Medium, false, 0f, p.avoidRepeatVariation, out _, out bool late);
                    float w = p.mediumClipWeight * DMHitReactionCore.RandomRange(p.playerClipWeightMin, p.playerClipWeightMax);
                    float speed = p.mediumClipSpeed * DMHitReactionCore.RandomRange(p.playerClipSpeedMin, p.playerClipSpeedMax);
                    _clips.Play(DMEnemyHitReactionPlayer.Tier.Medium, side, false, w, speed, DMHitReactionCore.StartOffset(p, late), p.reactionBlendInSeconds, p.reactionBlendOutSeconds);
                }
                Lock(p.playerLightLockSeconds, 0f);
                StatLights++;
                result = Result.Light;
            }
            else
            {
                StatLeans++;
            }

            DMEnemyHitFlinch.Strike strike = DMHitReactionCore.MakeStrike(p, dir, lean, twist, p.playerLeanSettleSeconds, 1f);
            strike.AttackSeconds = p.playerLeanAttackSeconds;
            strike.HeadWhipMultiplier = Mathf.Max(1f, p.headWhipMultiplier * p.playerHeadWhipScale);
            strike.HoldPeakThroughHitstop = false;
            _flinch.PlayStrike(strike);
            Log(p, result.ToString(), side, damage.damageValue, lean);
            return result;
        }

        private void Lock(float meleeSeconds, float moveSeconds)
        {
            float now = Time.time;
            _lockUntil = Mathf.Max(_lockUntil, now + meleeSeconds);
            if (moveSeconds > 0f)
                _moveScaleUntil = Mathf.Max(_moveScaleUntil, now + moveSeconds);
        }

        private void StartPush(Vector3 dir, float meters, float seconds)
        {
            _pushDir = dir;
            _pushRemaining = Mathf.Max(0f, meters);
            _pushSpeed = _pushRemaining / Mathf.Max(0.05f, seconds);
        }

        private void Update()
        {
            if (!_resolved)
                return;
            float now = Time.time;
            if (_melee != null && !IsKnockedDown)
            {
                if (now < _lockUntil)
                {
                    _melee.lockMeleeInput = true;
                    _ownsMeleeLock = true;
                }
                else if (_ownsMeleeLock)
                {
                    _melee.lockMeleeInput = false;
                    _ownsMeleeLock = false;
                }
            }

            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (now < _moveScaleUntil && p != null && _cc != null)
                _cc.input *= Mathf.Clamp01(p.playerHeavyMoveScale);
        }

        private void FixedUpdate()
        {
            if (_pushRemaining <= 0.0001f || _rb == null)
                return;
            if (_cc == null || !_cc.isGrounded || IsKnockedDown || (_stats != null && _stats.IsDead))
            {
                _pushRemaining = 0f;
                return;
            }

            float step = Mathf.Min(_pushRemaining, _pushSpeed * Time.fixedDeltaTime);
            _pushRemaining -= step;
            if (!CanPush(step))
            {
                _pushRemaining = 0f;
                return;
            }
            _rb.position += _pushDir * step;
        }

        private bool CanPush(float step)
        {
            if (_capsule == null)
                return true;
            float radius = _capsule.radius * 0.95f;
            Vector3 center = transform.TransformPoint(_capsule.center);
            float half = Mathf.Max(0f, _capsule.height * 0.5f - _capsule.radius);
            Vector3 a = center + Vector3.up * half;
            Vector3 b = center - Vector3.up * (half - 0.1f);
            int mask = ~(1 << gameObject.layer);
            if (Physics.CapsuleCast(a, b, radius, _pushDir, step + 0.05f, mask, QueryTriggerInteraction.Ignore))
                return false;

            // Ledge check: never push off a drop.
            Vector3 ahead = transform.position + _pushDir * (step + _capsule.radius) + Vector3.up * 0.5f;
            return Physics.Raycast(ahead, Vector3.down, 1.1f, mask, QueryTriggerInteraction.Ignore);
        }

        private bool IsClimbing()
        {
            DMClimbController climb = GetComponent<DMClimbController>();
            return climb != null && climb.IsClimbing;
        }

        private void OnDisable()
        {
            _pushRemaining = 0f;
            if (_ownsMeleeLock && _melee != null)
                _melee.lockMeleeInput = false;
            _ownsMeleeLock = false;
            _lockUntil = _moveScaleUntil = 0f;
        }

        private static void Log(DM_CombatCoreProfile p, string tier, DMHitSide side, float dmg, float lean)
        {
            if (p.logPlayerHitReactions)
                Debug.Log($"[PlayerHitReaction] {tier} side={side} dmg={dmg:0.#} lean={lean:0.#}");
        }

        private bool Resolve()
        {
            if (_resolved)
                return _cc != null && _flinch != null && _clips != null;
            _resolved = true;
            _cc = GetComponent<vThirdPersonController>();
            _melee = GetComponent<PioneerShooterMeleeInput>();
            _stats = GetComponent<SurvivalStats>();
            _rb = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();
            _flinch = GetComponent<DMEnemyHitFlinch>();
            if (_flinch == null)
                _flinch = gameObject.AddComponent<DMEnemyHitFlinch>();
            _clips = GetComponent<DMEnemyHitReactionPlayer>();
            if (_clips == null)
                _clips = gameObject.AddComponent<DMEnemyHitReactionPlayer>();
            _kd = DMPlayerKnockdown.Ensure(gameObject);
            return _cc != null && _flinch != null && _clips != null;
        }
    }
}
