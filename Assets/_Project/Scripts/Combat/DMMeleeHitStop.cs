using Project.AI;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Slice 1 / A1: local hit stop on a landed player melee hit. The HIT enemy's Animator (and the player's swing) dip to
    /// DM_CombatCoreProfile.hitstopHoldSpeed for light / heavy (+crit) frames. Never touches Time.timeScale; other enemies keep moving.
    /// </summary>
    public static class DMMeleeHitStop
    {
        private const float FrameSeconds = 1f / 60f;

        /// <summary>Player swing hold, read by PioneerMeleeSwingHitDedupe each LateUpdate.</summary>
        public static float PlayerHoldUntil;
        public static float PlayerHoldSpeed = 0.05f;
        // Stale-value guard: a hold can never run longer than a few frames, so a leftover PlayerHoldUntil from a previous
        // Play session (e.g. with domain reload off, unscaledTime restarts at 0) is ignored.
        private const float MaxHoldSeconds = 0.5f;

        public static bool PlayerHoldActive
        {
            get
            {
                float remaining = PlayerHoldUntil - Time.unscaledTime;
                return remaining > 0f && remaining <= MaxHoldSeconds;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PlayerHoldUntil = 0f;
        }

        /// <summary>Slice 2b: true while this enemy Animator is inside a hit-stop freeze (the hit lean holds its peak meanwhile).</summary>
        public static bool IsEnemyAnimatorHeld(Animator anim)
        {
            return anim != null && anim.TryGetComponent(out DMMeleeHitStopHold hold) && hold.IsHolding;
        }

        public static void OnPlayerMeleeHit(GameObject source, EnemyHealth enemy, bool isCritical)
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.enableMeleeHitstop || enemy == null || enemy.IsDead || source == null)
                return;

            PioneerShooterMeleeInput melee = source.GetComponentInParent<PioneerShooterMeleeInput>();
            if (melee == null)
                return;

            bool jumpStrike = DMPlayerJumpAttack.IsStrikeActiveFor(source); // 2026-10-25g
            bool heavy = melee.IsStrongMeleeDamageActive || melee.IsChargedStrongSwingActive || jumpStrike;
            int frames = heavy ? p.hitstopHeavyFrames : p.hitstopLightFrames;
            if (jumpStrike)
                frames += p.jumpAttackHitstopBonusFrames;
            if (isCritical)
                frames += p.hitstopCritBonusFrames;
            // Slice 2b: heavier hits (damage / heavy attack weight) earn a few extra freeze frames for impact punch.
            frames += DMEnemyHitReaction.ConsumeHitstopBonusFrames(enemy);
            if (frames <= 0)
                return;

            float seconds = frames * FrameSeconds;
            Animator anim = enemy.GetComponentInChildren<Animator>();
            if (anim != null)
                DMMeleeHitStopHold.Begin(anim, seconds, p.hitstopHoldSpeed);

            if (p.hitstopFreezesAttacker)
            {
                PlayerHoldSpeed = p.hitstopHoldSpeed;
                PlayerHoldUntil = Time.unscaledTime + seconds;
            }
        }
    }

    /// <summary>Holds one enemy Animator near-frozen for a few frames, then restores the speed other systems last asked for.</summary>
    [DisallowMultipleComponent]
    internal sealed class DMMeleeHitStopHold : MonoBehaviour
    {
        private Animator _anim;
        private float _until;
        private float _holdSpeed;
        private float _saved = 1f;
        private bool _active;

        internal bool IsHolding => _active && Time.unscaledTime < _until;

        public static void Begin(Animator anim, float seconds, float holdSpeed)
        {
            DMMeleeHitStopHold hold = anim.GetComponent<DMMeleeHitStopHold>();
            if (hold == null)
                hold = anim.gameObject.AddComponent<DMMeleeHitStopHold>();

            hold._anim = anim;
            hold._holdSpeed = holdSpeed;
            if (!hold._active)
            {
                hold._saved = anim.speed > 0.01f ? anim.speed : 1f;
                hold._active = true;
            }

            hold._until = Mathf.Max(hold._until, Time.unscaledTime + seconds);
            anim.speed = holdSpeed;
        }

        private void LateUpdate()
        {
            if (!_active || _anim == null)
                return;

            // Whatever other systems set this frame (combo driver, light-swing speed) is what to return to.
            float seen = _anim.speed;
            if (Mathf.Abs(seen - _holdSpeed) > 0.001f && seen > 0.01f)
                _saved = seen;

            if (Time.unscaledTime >= _until)
            {
                _anim.speed = _saved;
                _active = false;
                return;
            }

            _anim.speed = _holdSpeed;
        }

        private void OnDisable()
        {
            if (_active && _anim != null)
                _anim.speed = _saved;
            _active = false;
        }
    }
}
