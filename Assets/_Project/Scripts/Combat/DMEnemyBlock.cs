using Project.AI;
using Project.AI.Invector;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Brain-driven enemy block vs player melee. The brain (DMEnemyBrain Defend -> EnemyAiController.TryBrainBlock) raises the guard;
    /// this helper only answers "did this player hit land on a raised guard" and plays the feedback. Every number lives in
    /// DM_EnemyBrainProfile "Enemy block". Called from PioneerInvectorDamageReceiver.
    /// Blocked hit: reduced damage (and poise damage), block sparks + shake, the enemy does not flinch, and the PLAYER may be recoiled with the
    /// existing Invector BreakAttack path (player clip recoil_low by default; enemies react with procedural flinch / Big reactions, so it looks different).
    /// Charged / heavy hits break the guard (profile toggle); a poise break drops the guard too.
    /// </summary>
    public static class DMEnemyBlock
    {
        public enum Result { None, Blocked, GuardBroken }

        private static float lastPlayerRecoilTime = -999f;

        /// <summary>Runtime-only test switch: every player melee hit meets a raised guard (block or guard break). Never saved.</summary>
        public static bool ForceBlockTest;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Dark Matter Genesis/Combat/Test/Force Enemy Block (toggle)")]
        private static void ToggleForce() { ForceBlockTest = !ForceBlockTest; Debug.Log("[EnemyBlock] ForceBlockTest=" + ForceBlockTest); }
        [UnityEditor.MenuItem("Tools/Dark Matter Genesis/Combat/Test/Force Enemy Block (toggle)", true)]
        private static bool ToggleForceCheck() { UnityEditor.Menu.SetChecked("Tools/Dark Matter Genesis/Combat/Test/Force Enemy Block (toggle)", ForceBlockTest); return true; }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            lastPlayerRecoilTime = -999f;
        }

        public static Result Evaluate(EnemyHealth enemy, GameObject source, ref float damage)
        {
            if (enemy == null || source == null)
                return Result.None;

            DMHitchMonitor.Mark("player hit -> " + enemy.name);
            DM_EnemyBrainProfile bp = DM_EnemyBrainProfile.Live;
            if (!bp.enableEnemyBlock)
                return Result.None;

            EnemyInvectorCombatBridge bridge = enemy.GetComponent<EnemyInvectorCombatBridge>();
            if (bridge == null)
                return Result.None;

            // Player melee only (companion hits and other sources land normally).
            if (source.GetComponentInParent<PioneerShooterMeleeInput>() == null)
                return Result.None;

            // Forced block test (Tools > Dark Matter Genesis > Combat > Test > Force Enemy Block, runtime only): every player melee hit meets a raised guard.
            if (ForceBlockTest && !bridge.IsBlockActive)
                bridge.TryBeginBlock(Mathf.Max(0.4f, bp.blockDurationSeconds), out _);

            if (!bridge.IsBlockActive && bp.reactiveBlockChance > 0f)
            {
                EnemyAiController ai = enemy.GetComponent<EnemyAiController>();
                if (ai != null && ai.TryReactiveBlock(source.transform, bp.reactiveBlockChance))
                    Log("[EnemyBlock] reactive block raised on " + enemy.name);
            }

            if (!bridge.IsBlockActive)
            {
                Log("[EnemyBlock] miss reason=guardNotRaised on " + enemy.name);
                return Result.None;
            }

            Vector3 toSource = source.transform.position - enemy.transform.position;
            toSource.y = 0f;
            if (!ForceBlockTest && toSource.sqrMagnitude > 0.0001f)
            {
                float cos = Mathf.Cos(Mathf.Clamp(bp.blockArcDegrees, 0f, 360f) * 0.5f * Mathf.Deg2Rad);
                if (Vector3.Dot(enemy.transform.forward, toSource.normalized) < cos)
                {
                    Log("[EnemyBlock] miss reason=outsideArc on " + enemy.name);
                    return Result.None;
                }
            }

            bool charged = AttackInterruptEvaluator.IsPlayerChargedHit(source, out _);
            if (charged ? !bp.blockChargedHits : !bp.blockLightHits)
            {
                Log("[EnemyBlock] miss reason=" + (charged ? "chargedNotBlockable" : "lightNotBlockable") + " on " + enemy.name);
                return Result.None;
            }

            if (charged && bp.chargedBreaksBlock)
            {
                BreakGuard(enemy, "charged hit");
                return Result.GuardBroken;
            }

            damage *= Mathf.Clamp01(bp.blockDamageFraction);
            return Result.Blocked;
        }

        public static void BreakGuard(EnemyHealth enemy, string reason)
        {
            EnemyAiController ai = enemy != null ? enemy.GetComponent<EnemyAiController>() : null;
            if (ai != null)
                ai.BreakGuard();

            Log("[EnemyBlock] guard broken (" + reason + ") on " + (enemy != null ? enemy.name : "?"));
        }

        /// <summary>Feedback for a hit that stayed blocked: block sparks, shake, optional player recoil.</summary>
        public static void OnBlocked(EnemyHealth enemy, GameObject source, Invector.vDamage snapshot)
        {
            DM_EnemyBrainProfile bp = DM_EnemyBrainProfile.Live;
            DMParryClashVfx.TryPlay(snapshot, enemy.transform, source != null ? source.transform : null, bp.blockSparkScale, bp.blockSparkPrefab);
            DMCombatCameraShake.Play(bp.blockedHitShakeAmplitude, bp.blockedHitShakeDurationSeconds);

            bool recoiled = false;
            PioneerShooterMeleeInput melee = source != null ? source.GetComponentInParent<PioneerShooterMeleeInput>() : null;
            if (melee != null
                && bp.playerBlockedRecoilChance > 0f
                && Time.time - lastPlayerRecoilTime >= bp.playerBlockedRecoilImmunitySeconds
                && Random.value < bp.playerBlockedRecoilChance)
            {
                lastPlayerRecoilTime = Time.time;
                melee.BreakAttack(bp.playerBlockedRecoilId);
                recoiled = true;
            }

            Log("[EnemyBlock] blocked hit on " + enemy.name + (recoiled ? ", player recoil id " + bp.playerBlockedRecoilId : ", no recoil (chance / immunity)"));
        }

        private static void Log(string message)
        {
            DM_CombatCoreProfile core = DM_CombatCoreProfile.Live;
            if (core != null && core.logInterruptsInPlay)
                Debug.Log(message);
        }
    }
}
