using Invector;
using Project.AI;
using Project.AI.Invector;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Decides whether a player hit interrupts the enemy attack that is in progress.
    /// Sits beside <see cref="CombatPoise"/>; all numbers live in DM_CombatCoreProfile "Attack interrupts"
    /// (Combat Studio, Core tab) plus EnemyDefinition.interruptResistance / attackArmor / poiseMultiplier.
    ///
    /// Rules (one decision per player hit that lands on a living enemy):
    ///  1. Poise break on this hit: always interrupts (ignores armor, commit window, immunity).
    ///  2. No decision while the enemy is not mid-swing, in interrupt immunity, or super-armored.
    ///  3. Committed window (swing progress at or past the commit start): only a poise break interrupts.
    ///  4. Guaranteed: hit inside the early window, or a heavy/charged hit on a light swing.
    ///  5. Otherwise a chance roll: base(class) + early + crit + heavy + chain + low poise + low health - resistance, capped.
    /// Result: swing sequence cancelled, attack lock for interruptRecoverySeconds, Small hit reaction.
    /// Token / slot ownership is untouched (the interrupted attacker keeps its place until recovery ends).
    /// </summary>
    public static class AttackInterruptEvaluator
    {
        public enum AttackClass
        {
            None,
            Light,
            FollowUp,
            Charged
        }

        /// <summary>Call once per player melee hit after the damage was applied. Returns true when the enemy attack was interrupted.</summary>
        public static bool TryInterruptFromPlayerHit(
            EnemyHealth enemy,
            in DamageInfo info,
            vDamage sourceDamage)
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (enemy == null || profile == null || !profile.enableAttackInterrupts || enemy.IsDead)
                return false;

            CombatPoise poise = enemy.GetComponent<CombatPoise>() ?? enemy.GetComponentInChildren<CombatPoise>();
            float now = Time.time;

            // Chain tracking runs on every player hit, even when the enemy is not attacking.
            int chainHits = 1;
            if (poise != null)
            {
                chainHits = now - poise.LastPlayerHitTime <= profile.interruptChainWindowSeconds
                    ? poise.ChainHitCount + 1
                    : 1;
                poise.ChainHitCount = chainHits;
                poise.LastPlayerHitTime = now;
            }

            EnemyCombat combat = enemy.GetComponent<EnemyCombat>();
            DMEnemyMeleeComboDriver driver = combat != null ? combat.ComboDriver : null;
            if (driver == null || !driver.TryGetSwingProgress(out float progress, out bool charged))
                return false;

            AttackClass attackClass = charged
                ? AttackClass.Charged
                : (driver.CurrentSwingStep >= 2 ? AttackClass.FollowUp : AttackClass.Light);

            bool brokePoise = poise != null && poise.LastBreakFrame == Time.frameCount;
            bool heavy = info.IsChargedHit;
            if (profile.logInterruptsInPlay)
                Debug.Log($"[AttackInterrupt] {enemy.name} hit: heavy={heavy} crit={info.IsCritical} chain={chainHits} brokePoise={brokePoise} class={attackClass} progress={progress:0.00}", enemy);
            EnemyAiController ai = enemy.GetComponent<EnemyAiController>();
            EnemyDefinition def = ai != null ? ai.AppliedDefinition : null;

            if (brokePoise && profile.interruptGuaranteedOnPoiseBreak)
                return Apply(enemy, poise, sourceDamage, profile, attackClass, progress, 1f, 1f, "poise break", playReaction: false);

            if (poise != null && now - poise.LastInterruptTime < profile.interruptImmunitySeconds)
                return Log(profile, enemy, attackClass, progress, "skipped: interrupt immunity");

            EnemyAttackArmor armor = def != null ? def.attackArmor : EnemyAttackArmor.None;
            if (armor == EnemyAttackArmor.AllAttacks || (armor == EnemyAttackArmor.ChargedOnly && charged))
                return Log(profile, enemy, attackClass, progress, "skipped: super-armor");

            float commitStart = charged ? profile.interruptCommitStartCharged : profile.interruptCommitStartLight;
            if (progress >= commitStart)
                return Log(profile, enemy, attackClass, progress, "skipped: committed window (only poise break)");

            bool early = progress < profile.interruptEarlyWindowNormalized;
            if (early && profile.interruptEarlyWindowGuaranteed)
                return Apply(enemy, poise, sourceDamage, profile, attackClass, progress, 1f, 1f, "early window", true);

            if (heavy && attackClass != AttackClass.Charged && profile.interruptGuaranteedOnHeavyVsLight)
                return Apply(enemy, poise, sourceDamage, profile, attackClass, progress, 1f, 1f, "heavy hit vs light swing", true);

            float chance = attackClass == AttackClass.Charged
                ? profile.interruptBaseChanceCharged
                : (attackClass == AttackClass.FollowUp ? profile.interruptBaseChanceFollowUp : profile.interruptBaseChanceLight);
            if (early)
                chance += profile.interruptEarlyBonus;
            if (info.IsCritical)
                chance += profile.interruptCritBonus;
            if (heavy)
                chance += profile.interruptHeavyHitBonus;
            chance += Mathf.Min(profile.interruptChainBonusMax, profile.interruptChainBonusPerHit * Mathf.Max(0, chainHits - 1));
            if (poise != null && poise.MaxPoise > 0f && poise.CurrentPoise / poise.MaxPoise < profile.interruptLowPoiseThreshold)
                chance += profile.interruptLowPoiseBonus;
            if (enemy.MaxHealth > 0f && enemy.CurrentHealth / enemy.MaxHealth < profile.interruptLowHealthThreshold)
                chance += profile.interruptLowHealthBonus;
            if (def != null)
                chance -= def.interruptResistance;

            chance = Mathf.Clamp(chance, 0f, profile.interruptMaxChance);
            float roll = Random.value;
            if (roll >= chance)
                return Log(profile, enemy, attackClass, progress, $"no interrupt (chance {chance:0.00}, roll {roll:0.00})");

            return Apply(enemy, poise, sourceDamage, profile, attackClass, progress, chance, roll, "chance", true);
        }

        private static bool Apply(
            EnemyHealth enemy,
            CombatPoise poise,
            vDamage sourceDamage,
            DM_CombatCoreProfile profile,
            AttackClass attackClass,
            float progress,
            float chance,
            float roll,
            string reason,
            bool playReaction)
        {
            EnemyInvectorRagdollBridge bridge = enemy.GetComponent<EnemyInvectorRagdollBridge>()
                ?? enemy.GetComponentInChildren<EnemyInvectorRagdollBridge>();
            if (bridge == null)
                return false;

            bridge.InterruptAttackFromPlayerHit(
                sourceDamage,
                profile.interruptRecoverySeconds,
                profile.interruptReactionSeconds,
                playReaction);

            if (poise != null)
                poise.LastInterruptTime = Time.time;

            if (profile.logInterruptsInPlay)
                Debug.Log($"[AttackInterrupt] {enemy.name} {attackClass} @ {progress:0.00}: INTERRUPTED ({reason}, chance {chance:0.00}, roll {roll:0.00})", enemy);

            return true;
        }

        private static bool Log(DM_CombatCoreProfile profile, EnemyHealth enemy, AttackClass attackClass, float progress, string message)
        {
            if (profile.logInterruptsInPlay)
                Debug.Log($"[AttackInterrupt] {enemy.name} {attackClass} @ {progress:0.00}: {message}", enemy);

            return false;
        }

        /// <summary>True when the player's current melee hit is a charged / heavy swing (same source the charged-hit camera shake uses).</summary>
        public static bool IsPlayerChargedHit(GameObject source, out int swingStep)
        {
            swingStep = 0;
            if (source == null)
                return false;

            PioneerShooterMeleeInput melee = source.GetComponentInParent<PioneerShooterMeleeInput>();
            if (melee == null)
                return false;

            return melee.IsStrongMeleeDamageActive || melee.IsChargedStrongSwingActive;
        }
    }
}
