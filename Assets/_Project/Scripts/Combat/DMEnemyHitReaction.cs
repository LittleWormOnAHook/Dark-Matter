using Invector;
using Project.AI;
using Project.AI.Invector;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Slice 2 hit reactions for a landed PLAYER melee hit on an enemy (all numbers in DM_CombatCoreProfile "Slice 2 hit reactions"):
    ///   B1 directional flinch: torso twist toward the swing side (light combo step A/B/C, charged) + a small backstep.
    ///   B2 hit-chain escalation: hit 2 in a chain = stronger flinch + backstep, hit 3 = full-body stumble (Big reaction, two backsteps, lean back);
    ///      a Big-reaction immunity window caps stun-lock.
    ///   B3 heavy hit: Big reaction + slide-back + waist bend (skipped for committed swings / super armor).
    ///   B4 hit recovery: poise break, heavy Big reaction and stumble lock the enemy's next swing for hitRecoverySeconds.
    /// Called from PioneerInvectorDamageReceiver after the interrupt evaluator, so an already-interrupted enemy is handled consistently.
    /// </summary>
    public static class DMEnemyHitReaction
    {
        public static void OnPlayerMeleeHit(
            EnemyHealth enemy,
            EnemyInvectorRagdollBridge bridge,
            vDamage snapshot,
            float damage,
            GameObject source,
            bool isCritical,
            bool poiseBreak)
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.enableHitReactions || enemy == null || bridge == null || enemy.IsDead || source == null)
                return;

            // A dodge roll plays to the end: no flinch / stagger / hit slide cuts the clip mid-roll (damage still applied by the caller).
            EnemyAiController rollAi = enemy.GetComponent<EnemyAiController>();
            if (rollAi != null && rollAi.DodgeRollBlocksHitReactions)
                return;

            PioneerShooterMeleeInput melee = source.GetComponentInParent<PioneerShooterMeleeInput>();
            if (melee == null)
                return; // player attacks only (companions do not trigger these)

            Transform playerT = melee.transform;
            Transform enemyT = enemy.transform;
            float now = Time.time;

            bool heavy = AttackInterruptEvaluator.IsPlayerChargedHit(source, out int step);

            // Hit chain bookkeeping (own counter + window, independent of the interrupt evaluator's chain).
            CombatPoise poise = enemy.GetComponent<CombatPoise>();
            int chain = 1;
            if (poise != null)
            {
                chain = now - poise.LastReactionHitTime <= p.hitChainWindowSeconds ? poise.ReactionChainCount + 1 : 1;
                poise.ReactionChainCount = chain;
                poise.LastReactionHitTime = now;
            }

            Vector3 away = enemyT.position - playerT.position;
            away.y = 0f;
            away = away.sqrMagnitude < 0.0001f ? playerT.forward : away.normalized;

            float side = heavy
                ? p.swingSideCharged
                : (step == 1 ? p.swingSideLightA : step == 2 ? p.swingSideLightB : step >= 3 ? p.swingSideLightC : 0f);
            Vector3 lateral = playerT.right * side;
            Vector3 slideDir = (away + lateral * p.swingLateralBias).normalized;
            float twist = 0f;
            if (p.directionalFlinchEnabled)
                twist = Mathf.Sign(Vector3.Dot(lateral, enemyT.right) >= 0f ? 1f : -1f) * Mathf.Abs(side) * p.swingTwistDegrees;
            else
                twist = float.NaN;

            Vector3 from = enemyT.position - away; // flinch leans away from here

            EnemyAiController ai = enemy.GetComponent<EnemyAiController>();
            EnemyDefinition def = ai != null ? ai.AppliedDefinition : null;
            bool armored = def != null && def.attackArmor == EnemyAttackArmor.AllAttacks;
            // Slice 3: per-enemy variety of the backsteps (engagement profile moveVariationScale x crowding variation roll).
            float variety = ai != null ? ai.MoveVariation() : 1f;

            // Committed = still mid-swing after the interrupt evaluator ran (it did not interrupt).
            EnemyCombat combat = enemy.GetComponent<EnemyCombat>();
            bool midSwing = combat != null && combat.ComboDriver != null && combat.ComboDriver.TryGetSwingProgress(out _, out _);
            bool blockBig = armored || (p.heavyRespectsCommitment && midSwing);
            bool bigAllowed = poise == null || now >= poise.BigReactionImmuneUntil;

            bool doHeavyBig = heavy && p.heavyReactionEnabled && !poiseBreak && !blockBig && bigAllowed;
            bool doStumble = !heavy && !poiseBreak && p.chainReactionsEnabled && chain >= p.chainStumbleHitCount && !blockBig && bigAllowed;
            bool doStrong = !doHeavyBig && !doStumble && !poiseBreak && !heavy && p.chainReactionsEnabled && chain >= p.chainStrongHitCount;

            string kind;
            if (poiseBreak)
            {
                kind = "poiseBreak";
                if (p.hitRecoveryOnPoiseBreak)
                    bridge.ApplyHitRecovery(p.hitRecoverySeconds);
                DMEnemyHitSlide.Begin(enemy.gameObject, slideDir, p.poiseBreakBackstepMeters * variety, p.lightBackstepSeconds * 2f, 1, p.hitSlideMaxLedgeDrop, p.hitSlideMaxTotalMeters);
                MarkBig(poise, p, now);
            }
            else if (doHeavyBig)
            {
                kind = "heavyBig";
                bridge.TryHitStagger(snapshot, damage, isCritical, true, p.heavyBigReactionSeconds);
                if (p.hitRecoveryOnHeavyHit)
                    bridge.ApplyHitRecovery(p.hitRecoverySeconds);
                bridge.PlayDirectionalFlinch(from, p.heavyWaistBendDegrees, p.heavyWaistBendSeconds, twist);
                DMEnemyHitSlide.Begin(enemy.gameObject, slideDir, p.heavySlideMeters, p.heavySlideSeconds, 1, p.hitSlideMaxLedgeDrop, p.hitSlideMaxTotalMeters);
                MarkBig(poise, p, now);
            }
            else if (doStumble)
            {
                kind = "chainStumble";
                bridge.TryHitStagger(snapshot, damage, isCritical, true, p.chainStumbleSeconds);
                if (p.hitRecoveryOnChainStumble)
                    bridge.ApplyHitRecovery(p.hitRecoverySeconds);
                bridge.PlayDirectionalFlinch(from, p.chainStumbleLeanDegrees, p.heavyWaistBendSeconds, twist);
                DMEnemyHitSlide.Begin(enemy.gameObject, slideDir, p.chainStumbleBackstepMeters * variety, p.chainStumbleSeconds * 0.8f, 2, p.hitSlideMaxLedgeDrop, p.hitSlideMaxTotalMeters);
                MarkBig(poise, p, now);
                if (poise != null)
                    poise.ReactionChainCount = 0;
            }
            else if (heavy)
            {
                // Heavy hit without a Big reaction (committed swing, super armor or immunity): bend the waist, small shove.
                kind = armored || midSwing ? "heavyBendOnly" : "heavyImmune";
                bridge.PlayDirectionalFlinch(from, p.heavyWaistBendDegrees * 0.5f, p.heavyWaistBendSeconds, twist);
                if (!blockBig)
                    DMEnemyHitSlide.Begin(enemy.gameObject, slideDir, p.heavySlideMeters * p.heavyFallbackScale, p.heavySlideSeconds, 1, p.hitSlideMaxLedgeDrop, p.hitSlideMaxTotalMeters);
            }
            else if (doStrong)
            {
                kind = "chainStrong";
                bridge.PlayDirectionalFlinch(from, p.directionalFlinchDegrees * p.chainStrongFlinchMultiplier, p.directionalFlinchSeconds, twist);
                DMEnemyHitSlide.Begin(enemy.gameObject, slideDir, p.chainStrongBackstepMeters * variety, p.lightBackstepSeconds * 1.4f, 1, p.hitSlideMaxLedgeDrop, p.hitSlideMaxTotalMeters);
            }
            else
            {
                kind = "light";
                if (!bridge.IsHitStaggerActive)
                {
                    bridge.PlayDirectionalFlinch(from, p.directionalFlinchDegrees * (isCritical ? 1.3f : 1f), p.directionalFlinchSeconds, twist);
                    DMEnemyHitSlide.Begin(enemy.gameObject, slideDir, p.lightBackstepMeters * variety, p.lightBackstepSeconds, 1, p.hitSlideMaxLedgeDrop, p.hitSlideMaxTotalMeters);
                }
            }

            DM_CombatCoreProfile log = p;
            if (log.logInterruptsInPlay)
                Debug.Log($"[HitReaction] {enemy.name} kind={kind} chain={chain} step={step} heavy={heavy} side={side:0.#} twist={twist:0.#} blockBig={blockBig} bigAllowed={bigAllowed}", enemy);
        }

        private static void MarkBig(CombatPoise poise, DM_CombatCoreProfile p, float now)
        {
            if (poise != null)
                poise.BigReactionImmuneUntil = now + p.bigReactionImmunitySeconds;
        }
    }
}