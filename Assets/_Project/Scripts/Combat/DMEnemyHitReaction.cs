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
    /// Slice 2b (enableDirectionalReactionsV2, "Slice 2b" profile sections): same tiers, but
    ///   - the lean follows the real strike direction (blade velocity, combo table fallback), weighted spring with head whip,
    ///     scaled by damage / heavy attack, randomized per hit, never the same variation twice in a row;
    ///   - light = lean only, root never moves; medium (chain hit 2) = additive GetHitLight* clip on the torso; heavy (charged,
    ///     stumble, poise break) = GetHitHeavy* on the body layer + 0.35 m push (+ occasional stumble step);
    ///     arms / hands are masked in every clip so each enemy keeps its own weapon grip;
    ///   - heavy hits roll an animation-only knockdown (DMEnemyKnockdown, no ragdoll); downed enemies take damage but get no reactions;
    ///   - no Invector Big reaction (TryHitStagger) for player melee when the DM layers exist (it overrides the arms).
    /// Called from PioneerInvectorDamageReceiver after the interrupt evaluator, so an already-interrupted enemy is handled consistently.
    /// </summary>
    public static class DMEnemyHitReaction
    {
        private static int s_bonusFrame = -1;
        private static EnemyHealth s_bonusEnemy;
        private static int s_bonusFrames;
        private static int s_guardBreakFrame = -1;
        private static EnemyHealth s_guardBreakEnemy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_bonusFrame = -1;
            s_guardBreakFrame = -1;
            s_bonusEnemy = null;
            s_guardBreakEnemy = null;
        }

        /// <summary>Receiver: this hit broke the enemy's guard (heavy tier, but no knockdown on the same hit).</summary>
        public static void MarkGuardBreak(EnemyHealth enemy)
        {
            if (enemy == null)
                return;
            s_guardBreakFrame = Time.frameCount;
            s_guardBreakEnemy = enemy;
        }

        /// <summary>Hit stop: extra freeze frames earned by this hit's weight (read once, same frame).</summary>
        public static int ConsumeHitstopBonusFrames(EnemyHealth enemy)
        {
            if (enemy == null || s_bonusFrame != Time.frameCount || s_bonusEnemy != enemy)
                return 0;
            s_bonusFrame = -1;
            return s_bonusFrames;
        }

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

            // Slice 2b: a downed enemy takes the damage (caller) but gets no new reaction and its down time is not extended.
            if (DMEnemyKnockdown.IsDown(enemy.gameObject))
            {
                if (p.logHitReactionsInPlay)
                    Debug.Log($"[HitReaction] {enemy.name} kind=whileDown (damage only)", enemy);
                return;
            }

            PioneerShooterMeleeInput melee = source.GetComponentInParent<PioneerShooterMeleeInput>();
            if (melee == null)
                return; // player attacks only (companions do not trigger these)

            Transform playerT = melee.transform;
            Transform enemyT = enemy.transform;
            float now = Time.time;

            bool heavy = AttackInterruptEvaluator.IsPlayerChargedHit(source, out int step);
            if (!heavy && p.jumpAttackHeavyReaction && DMPlayerJumpAttack.IsStrikeActiveFor(source))
                heavy = true; // 2026-10-25g jump attack = heavy stagger / knockback

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

            EnemyAiController ai = rollAi;
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

            if (p.enableDirectionalReactionsV2)
            {
                PlayV2(p, enemy, bridge, melee, poise, def, away, side, heavy, step, chain, damage, isCritical, poiseBreak,
                    blockBig, armored, midSwing, bigAllowed, doHeavyBig, doStumble, doStrong, variety, now);
                return;
            }

            Vector3 lateral = playerT.right * side;
            Vector3 slideDir = (away + lateral * p.swingLateralBias).normalized;
            float twist = 0f;
            if (p.directionalFlinchEnabled)
                twist = Mathf.Sign(Vector3.Dot(lateral, enemyT.right) >= 0f ? 1f : -1f) * Mathf.Abs(side) * p.swingTwistDegrees;
            else
                twist = float.NaN;

            Vector3 from = enemyT.position - away; // flinch leans away from here

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

        // ---------------- Slice 2b ----------------

        private static void PlayV2(
            DM_CombatCoreProfile p,
            EnemyHealth enemy,
            EnemyInvectorRagdollBridge bridge,
            PioneerShooterMeleeInput melee,
            CombatPoise poise,
            EnemyDefinition def,
            Vector3 away,
            float comboSide,
            bool heavy,
            int step,
            int chain,
            float damage,
            bool isCritical,
            bool poiseBreak,
            bool blockBig,
            bool armored,
            bool midSwing,
            bool bigAllowed,
            bool doHeavyBig,
            bool doStumble,
            bool doStrong,
            float variety,
            float now)
        {
            GameObject go = enemy.gameObject;
            Transform enemyT = enemy.transform;
            Transform playerT = melee.transform;

            // 1. Strike direction: s = sideways share of the blade motion across the enemy (-1 .. 1).
            Vector3 perp = Vector3.Cross(Vector3.up, away); // the enemy's right as seen from the player
            float s = 0f;
            bool fromBlade = false;
            if (p.swingDirectionSource == DMSwingDirectionSource.BladeVelocity)
            {
                DMMeleeSwingVelocity sampler = DMMeleeSwingVelocity.Ensure(melee.gameObject);
                if (sampler != null && sampler.TryGetBladeVelocity(p.bladeVelocityWindowSeconds, out Vector3 v))
                {
                    v.y = 0f;
                    if (v.magnitude >= p.bladeVelocityMinSpeed)
                    {
                        s = Mathf.Clamp(Vector3.Dot(v.normalized, perp), -1f, 1f);
                        fromBlade = true;
                    }
                }
            }

            if (!fromBlade)
                s = Mathf.Clamp(Vector3.Dot(playerT.right * comboSide, perp), -1f, 1f);

            float share = Mathf.Clamp01(p.strikeLeanSideShare);
            Vector3 dir = away * (1f - share * Mathf.Abs(s)) + perp * (s * share);
            dir.y = 0f;
            dir = dir.sqrMagnitude < 0.0001f ? away : dir.normalized;

            // 2. Randomness: direction jitter (kept apart from the previous hit), intensity, twist sign.
            DMEnemyHitFlinch flinch = go.GetComponent<DMEnemyHitFlinch>();
            if (flinch == null)
                flinch = go.AddComponent<DMEnemyHitFlinch>();
            float jitterRange = p.leanAngleJitterDegrees;
            float jitter = DMHitReactionCore.PickJitter(jitterRange, flinch.LastJitterDegrees, p.avoidRepeatVariation); // 2026-10-19 d: shared core
            flinch.LastJitterDegrees = jitter;
            dir = Quaternion.AngleAxis(jitter, Vector3.up) * dir;

            float intensity = Random.Range(Mathf.Min(p.reactionIntensityRandomMin, p.reactionIntensityRandomMax), Mathf.Max(p.reactionIntensityRandomMin, p.reactionIntensityRandomMax));

            float twistSign = Vector3.Dot(perp * s, enemyT.right) >= 0f ? 1f : -1f;
            if (Random.value < p.twistSignFlipChance)
                twistSign = -twistSign;
            float twistBase = Mathf.Abs(s) * p.lightTwistDegrees;
            float twist = twistSign * twistBase + Random.Range(-p.twistRandomDegrees, p.twistRandomDegrees);

            // 3. Weight: damage (weapon heft shows up as damage) and heavy attacks.
            float dmgRef = Mathf.Max(1f, p.reactionDamageReference);
            float weight = Mathf.Clamp(Mathf.Pow(Mathf.Max(0.01f, damage) / dmgRef, p.reactionDamageExponent), p.reactionDamageScaleMin, p.reactionDamageScaleMax);
            if (heavy)
                weight *= p.heavyAttackWeightMultiplier;
            float crit = isCritical ? p.critLeanMultiplier : 1f;
            float topWeight = Mathf.Max(1.001f, p.reactionDamageScaleMax * p.heavyAttackWeightMultiplier);
            float weight01 = Mathf.Clamp01(Mathf.InverseLerp(1f, topWeight, weight * crit));
            s_bonusFrame = Time.frameCount;
            s_bonusEnemy = enemy;
            s_bonusFrames = Mathf.RoundToInt(p.hitstopWeightBonusFrames * weight01);

            DMHitSide hitSide = ResolveSide(enemyT, dir);
            DMEnemyHitReactionPlayer clips = DMEnemyHitReactionPlayer.Ensure(go);
            bool clipsOk = clips != null && clips.IsSupported;

            bool heavyTier = poiseBreak || doHeavyBig || doStumble;
            string kind;

            // 4. Knockdown roll (heavy tier only, never on a guard break / committed / armored hit).
            if (heavyTier && TryKnockdown(p, enemy, poise, def, dir, hitSide, isCritical, poiseBreak, blockBig))
            {
                kind = "knockdown";
                MarkBig(poise, p, now);
                if (poise != null)
                    poise.ReactionChainCount = 0;
                Log(p, enemy, kind, chain, step, heavy, s, fromBlade, hitSide, weight, intensity, twist, jitter);
                return;
            }

            float lean = Mathf.Min(35f, p.lightLeanDegrees * weight * intensity * crit);
            if (heavyTier)
            {
                kind = poiseBreak ? "poiseBreak" : doHeavyBig ? "heavyBig" : "chainStumble";
                bool recovery = poiseBreak ? p.hitRecoveryOnPoiseBreak : doHeavyBig ? p.hitRecoveryOnHeavyHit : p.hitRecoveryOnChainStumble;
                if (recovery)
                    bridge.ApplyHitRecovery(p.hitRecoverySeconds);

                bool clipPlayed = false;
                if (p.heavyUsesClip && clipsOk)
                {
                    clips.PickVariation(DMEnemyHitReactionPlayer.Tier.Heavy, false, 0f, p.avoidRepeatVariation, out _, out bool late);
                    float w = p.heavyClipWeight * RandomRange(p.clipWeightRandomMin, p.clipWeightRandomMax) * Mathf.Clamp(intensity, 0.85f, 1.1f);
                    float speed = p.heavyClipSpeed * RandomRange(p.clipSpeedRandomMin, p.clipSpeedRandomMax) / Mathf.Clamp(weight, 0.85f, 1.3f);
                    clipPlayed = clips.Play(DMEnemyHitReactionPlayer.Tier.Heavy, hitSide, false, w, speed, StartOffset(p, late), p.reactionBlendInSeconds, p.reactionBlendOutSeconds);
                }

                if (!clipPlayed)
                {
                    // No DM layers on this controller: keep the Slice 2 Invector Big reaction.
                    float seconds = doStumble ? p.chainStumbleSeconds : p.heavyBigReactionSeconds;
                    if (!poiseBreak)
                        bridge.TryHitStagger(new vDamage(Mathf.Max(1, Mathf.RoundToInt(damage))) { sender = playerT, hitPosition = enemyT.position }, damage, false, true, seconds);
                }

                float heavyLean = Mathf.Min(45f, p.heavyLeanDegrees * weight * intensity * crit);
                flinch.PlayStrike(MakeStrike(p, dir, heavyLean, twist * 1.3f, p.heavyLeanSettleSeconds, 0.8f));

                float push = (poiseBreak ? Mathf.Max(p.heavySlideMeters, p.poiseBreakBackstepMeters) : p.heavySlideMeters) * Mathf.Min(1f, weight) * variety;
                int pulses = 1;
                if (Random.value < p.heavyStumbleStepChance && p.heavyStumbleStepMeters > 0.001f)
                {
                    push += p.heavyStumbleStepMeters;
                    pulses = 2;
                    kind += "+step";
                }
                if (push > 0.001f)
                    DMEnemyHitSlide.Begin(go, dir, push, p.heavySlideSeconds * (pulses > 1 ? 1.4f : 1f), pulses, p.hitSlideMaxLedgeDrop, p.hitSlideMaxTotalMeters);

                MarkBig(poise, p, now);
                if (doStumble && poise != null)
                    poise.ReactionChainCount = 0;
            }
            else if (heavy)
            {
                // Committed swing / super armor / immunity: bend only (procedural), no clip, no knockdown.
                kind = armored || midSwing ? "heavyBendOnly" : "heavyImmune";
                flinch.PlayStrike(MakeStrike(p, dir, Mathf.Min(45f, p.heavyLeanDegrees * 0.6f * weight * intensity * crit), twist, p.heavyLeanSettleSeconds, 0.9f));
                if (!blockBig)
                    DMEnemyHitSlide.Begin(go, dir, p.heavySlideMeters * p.heavyFallbackScale, p.heavySlideSeconds, 1, p.hitSlideMaxLedgeDrop, p.hitSlideMaxTotalMeters);
            }
            else if (doStrong)
            {
                kind = "medium";
                if (p.mediumUsesClip && clipsOk)
                {
                    clips.PickVariation(DMEnemyHitReactionPlayer.Tier.Medium, true, p.mediumAltHeavyClipChance, p.avoidRepeatVariation, out bool alt, out bool late);
                    float w = (alt ? p.mediumAltHeavyClipWeight : p.mediumClipWeight) * RandomRange(p.clipWeightRandomMin, p.clipWeightRandomMax) * Mathf.Clamp(intensity * weight, 0.7f, 1.2f);
                    float speed = p.mediumClipSpeed * RandomRange(p.clipSpeedRandomMin, p.clipSpeedRandomMax);
                    if (clips.Play(DMEnemyHitReactionPlayer.Tier.Medium, hitSide, alt, w, speed, StartOffset(p, late), p.reactionBlendInSeconds, p.reactionBlendOutSeconds))
                        kind += alt ? "(altHeavyClip)" : "(clip)";
                }

                flinch.PlayStrike(MakeStrike(p, dir, Mathf.Min(40f, lean * p.mediumLeanScale), twist * 1.15f, p.lightLeanSettleSeconds * 1.15f, 1f));
            }
            else
            {
                kind = "light";
                if (!bridge.IsHitStaggerActive)
                {
                    flinch.PlayStrike(MakeStrike(p, dir, lean, twist, p.lightLeanSettleSeconds, 1f));
                    // Light hits never relocate the enemy (lightBackstepMeters 0 = pure physical reaction).
                    if (p.lightBackstepMeters > 0.001f)
                        DMEnemyHitSlide.Begin(go, dir, p.lightBackstepMeters * variety, p.lightBackstepSeconds, 1, p.hitSlideMaxLedgeDrop, p.hitSlideMaxTotalMeters);
                }
            }

            Log(p, enemy, kind, chain, step, heavy, s, fromBlade, hitSide, weight, intensity, twist, jitter);
        }

        private static bool TryKnockdown(
            DM_CombatCoreProfile p,
            EnemyHealth enemy,
            CombatPoise poise,
            EnemyDefinition def,
            Vector3 dir,
            DMHitSide side,
            bool isCritical,
            bool poiseBreak,
            bool blockBig)
        {
            if (!p.knockdownEnabled || blockBig)
                return false;

            if (s_guardBreakFrame == Time.frameCount && s_guardBreakEnemy == enemy)
                return false; // guard break = heavy tier, no knockdown on the same hit

            float resistance = def != null ? Mathf.Clamp01(def.knockdownResistance) : 0f;
            if (resistance >= 0.999f)
                return false;

            float chance = poiseBreak ? p.knockdownOnPoiseBreakPercent : p.knockdownChancePercent;
            if (poise != null && poise.MaxPoise > 0.01f && poise.CurrentPoise / poise.MaxPoise < 0.3f)
                chance += p.knockdownLowPoiseBonusPercent;
            if (isCritical)
                chance += p.knockdownCritBonusPercent;
            chance = Mathf.Min(chance, p.knockdownMaxChancePercent) * (1f - resistance);
            if (chance <= 0f || Random.value * 100f >= chance)
                return false;

            DMEnemyKnockdown kd = DMEnemyKnockdown.Ensure(enemy.gameObject);
            if (kd == null || !kd.IsSupported || kd.IsActive || kd.IsImmune || !kd.GroundAllowsKnockdown(dir, p))
                return false;

            return kd.TryBegin(side, dir, p);
        }

        // 2026-10-19 d: shared with the player via DMHitReactionCore (moved verbatim; enemy behaviour unchanged).
        private static DMHitSide ResolveSide(Transform enemyT, Vector3 pushDir) => DMHitReactionCore.ResolveSide(enemyT, pushDir);

        private static DMEnemyHitFlinch.Strike MakeStrike(DM_CombatCoreProfile p, Vector3 dir, float degrees, float twist, float settleSeconds, float frequencyScale)
            => DMHitReactionCore.MakeStrike(p, dir, degrees, twist, settleSeconds, frequencyScale);

        private static float StartOffset(DM_CombatCoreProfile p, bool late) => DMHitReactionCore.StartOffset(p, late);

        private static float RandomRange(float a, float b) => DMHitReactionCore.RandomRange(a, b);

        private static void Log(DM_CombatCoreProfile p, EnemyHealth enemy, string kind, int chain, int step, bool heavy, float s, bool fromBlade,
            DMHitSide side, float weight, float intensity, float twist, float jitter)
        {
            if (p.logHitReactionsInPlay || p.logInterruptsInPlay)
                Debug.Log($"[HitReaction] {enemy.name} kind={kind} chain={chain} step={step} heavy={heavy} strikeSide={s:0.00} src={(fromBlade ? "blade" : "combo")} hitSide={side} weight={weight:0.00} rnd={intensity:0.00} twist={twist:0.#} jitter={jitter:0.#}", enemy);
        }

        private static void MarkBig(CombatPoise poise, DM_CombatCoreProfile p, float now)
        {
            if (poise != null)
                poise.BigReactionImmuneUntil = now + p.bigReactionImmunitySeconds;
        }
    }
}
