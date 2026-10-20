using Project.AI.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-19 d: shared directional hit-reaction math for enemies (Slice 2b, DMEnemyHitReaction / DMEnemyKnockdown)
    /// and the player (DMPlayerHitReaction / DMPlayerKnockdown). Pure functions, moved verbatim from the enemy code so
    /// enemy behaviour is unchanged; both sides call these. The bone lean (DMEnemyHitFlinch) and the layer clip player
    /// (DMEnemyHitReactionPlayer) components are shared as-is (they only need a Humanoid Animator with the DM layers).
    /// </summary>
    public static class DMHitReactionCore
    {
        /// <summary>Side the blow lands on, from where the strike pushes the body (PROTOFACTOR naming).</summary>
        public static DMHitSide ResolveSide(Transform target, Vector3 pushDir)
        {
            float x = Vector3.Dot(pushDir, target.right);
            float z = Vector3.Dot(pushDir, target.forward);
            if (Mathf.Abs(x) > Mathf.Abs(z))
                return x > 0f ? DMHitSide.Left : DMHitSide.Right; // pushed to its right = struck on its left
            return z < 0f ? DMHitSide.Front : DMHitSide.Back;     // pushed backward = struck in front
        }

        /// <summary>
        /// KD_* states are named by the direction the body FALLS (PROTOFACTOR DeathFront falls forward).
        /// The fall goes with the push: struck in front -> falls back, struck on the left -> falls right.
        /// </summary>
        public static string FallTag(DMHitSide side)
        {
            switch (side)
            {
                case DMHitSide.Front: return "B";
                case DMHitSide.Back: return "F";
                case DMHitSide.Left: return "R";
                default: return "L";
            }
        }

        /// <summary>Normalized land point of the fall clip for this side (profile, measured Oct 19).</summary>
        public static float FallExit(DM_CombatCoreProfile p, DMHitSide side)
        {
            switch (FallTag(side))
            {
                case "B": return p.knockdownFallExitBack;
                case "L": return p.knockdownFallExitLeft;
                case "R": return p.knockdownFallExitRight;
                default: return p.knockdownFallExitFront;
            }
        }

        public static DMEnemyHitFlinch.Strike MakeStrike(DM_CombatCoreProfile p, Vector3 dir, float degrees, float twist, float settleSeconds, float frequencyScale)
        {
            return new DMEnemyHitFlinch.Strike
            {
                LeanDirection = dir,
                Degrees = degrees,
                TwistDegrees = twist,
                AttackSeconds = p.lightLeanAttackSeconds,
                SettleSeconds = settleSeconds,
                SpringFrequency = p.leanSpringFrequency * frequencyScale,
                SpringDamping = p.leanSpringDamping,
                SpineWeight = p.leanSpineWeight,
                ChestWeight = p.leanChestWeight,
                HeadWeight = p.leanHeadWeight,
                HeadDelaySeconds = p.headWhipDelaySeconds,
                HeadWhipMultiplier = p.headWhipMultiplier,
                HoldPeakThroughHitstop = p.leanHoldPeakThroughHitstop
            };
        }

        /// <summary>Random start offset into a hit clip (early or late bucket).</summary>
        public static float StartOffset(DM_CombatCoreProfile p, bool late)
        {
            float max = Mathf.Max(0f, p.clipStartOffsetRandomMax);
            return late ? Random.Range(max * 0.55f, max) : Random.Range(0f, max * 0.35f);
        }

        public static float RandomRange(float a, float b)
        {
            return Random.Range(Mathf.Min(a, b), Mathf.Max(a, b));
        }

        /// <summary>Per-hit lean direction jitter, kept apart from the previous hit's jitter when avoidRepeat.</summary>
        public static float PickJitter(float jitterRange, float lastJitter, bool avoidRepeat)
        {
            float jitter = Random.Range(-jitterRange, jitterRange);
            if (avoidRepeat && jitterRange > 0.5f && Mathf.Abs(jitter - lastJitter) < jitterRange * 0.35f)
                jitter = Mathf.Clamp(lastJitter + (lastJitter > 0f ? -1f : 1f) * jitterRange * 0.6f, -jitterRange, jitterRange);
            return jitter;
        }

        /// <summary>Length of a clip on the animator's controller, or the fallback.</summary>
        public static float ClipLength(Animator animator, string clipName, float fallback)
        {
            RuntimeAnimatorController rac = animator != null ? animator.runtimeAnimatorController : null;
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
    }
}
