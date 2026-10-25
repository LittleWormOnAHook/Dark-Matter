# Combat — Genesis Studio

**Status:** Full (generated from code + reviewed notes)  
**Open:** Tools → Dark Matter Genesis → Genesis Studio → **Combat**  
Combat Core, melee animations, Combat Studio, ammo FX, enemy hit marks, AI / director, and §31 phase placeholders.

## Reviewed notes (Combat)

- **Combat Studio** (separate window): `Tools/Dark Matter Genesis/Combat/Combat Studio` (`Assets/_Project/Editor/Combat/DMCombatStudioWindow.cs`).
- **Hit Marks / Blood FX**: profile `Resources/Combat/DM_EnemyHitMarkProfile.asset`; menus `Combat/Hit Marks/Blood FX - Build Android + Mech Variants` and `... - Recolor Variants From Profile`; `Combat/Build Laser Burn Mark Prefab`.
- **Carve**: settings `Resources/Carve/DMCarveSettings.asset`; menus `Tools/Dark Matter Genesis/Carve/Enable Read/Write For Auto-Carvable Tags` and `Carve/Rebuild Static Mesh Cache`.
- **Hit reactions**: `Combat/Build|Validate (Player) Hit Reaction Layers` (`Editor/Combat/DMHitReactionLayerBuilder.cs`).
- **Weapon placement**: `Combat/Weapon Placement/*` (bake window, apply profile, bake all enemy prefabs).
- **Audits** (Play only): `Combat/Enemy Obstacle Jitter|Rock Mesh|Thin Wall Endpoint Audit (Play)`; Audit Hub at `Tools/Dark Matter Genesis/Audit/Audit Hub`.
- Design roadmap: `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md` (v2.4). The engineering doc is `Assets/_Project/Features/Combat/Documentation/Dark_Matter_Combat_System.md`.

**Gotchas:** never edit FORGE3D (turrets are only *registered*, via `Buildings/Register ... turrets in Equipment`). Enemy/player prefabs are not saved by agents, and "Bake all enemy prefabs" touches many prefabs, so it needs Anthony's OK.
## Tabs

- [Combat Core](#combat-core)
- [Melee Animations](#melee-animations)
- [Combat Studio](#combat-studio)
- [Ranged](#ranged)
- [Throwables](#throwables)
- [Turrets](#turrets)
- [Ammo FX](#ammo-fx)
- [Scope Overlay](#scope-overlay)
- [Scope Profile](#scope-profile)
- [Hit Catalog](#hit-catalog)
- [Hit Marks](#hit-marks)
- [Surface Damage](#surface-damage)
- [Carve Tool](#carve-tool)
- [AI & awareness](#ai--awareness)
- [Chase wall & fan](#chase-wall--fan)
- [Enemy Types](#enemy-types)
- [Director & tokens](#director--tokens)
- [Combat Director](#combat-director)
- [Momentum & finishers](#momentum--finishers)
- [Sick Stick & specials](#sick-stick--specials)
- [Body & dismemberment](#body--dismemberment)
- [Encounters & difficulty](#encounters--difficulty)

## Combat Core

**What it does:** DM_CombatCoreProfile — flat inspector here; Combat Studio groups the same fields under Player vs Enemy.

**Where:**
- Studio: `Genesis Studio / Combat / Combat Core` (panel mode `AssetFolder`)
- Asset folder: `Assets/_Project/Resources/Combat` (`t:DM_CombatCoreProfile`)
- Script: `Assets/_Project/Scripts/Combat/DM_CombatCoreProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Poise | `trainingDummyPoiseMax` | float | 80f |  |  |
| Poise | `poiseDamageFromHealthMultiplier` | float | 0.35f | 0f–2f |  |
| Poise | `maxPoiseDamageFractionPerHit` | float | 0.4f | 0.1f–1f | Caps poise lost on one hit as a fraction of max. 0.4 ≈ never one-shot; 1 = allow a single swing to break. |
| Poise | `poiseRegenPerSecond` | float | 12f |  |  |
| Poise | `poiseRegenDelayAfterHit` | float | 2f |  |  |
| Humanoid poise | `humanoidPoiseMax` | float | 100f |  |  |
| Humanoid poise | `humanoidPoiseStaggerSeconds` | float | 0.32f | 0.1f–1f | Poise-break hitstun on humanoids (~0.32s baseline). Block/parry guard-break uses longer lockouts below. |
| Block / parry guard-break (attacker stagger) | `blockStaggerSeconds` | float | 0.4f | 0f–3f | Attack lockout after a normal block. Tunable 0–3s (default 0.4). Typical Souls / God of War guard recoil is ~0.35–0.45s — shorter than a parry. |
| Block / parry guard-break (attacker stagger) | `parryStaggerSeconds` | float | 5f | 0f–10f | Attack lockout after a parry (block inside parryWindowSeconds). Riposte opening on the attacker — tunable 0–10s (default 5). Shorter than block at low values; Sekiro/Souls-style at ~0.7–0.85s. |
| Block / parry guard-break (attacker stagger) | `blockParryStaggerImpulseScale` | float | 0.85f | 0.5f–3f | Flinch shove on a normal block. Kept under 1 so a guard tap is a mild stagger, not a launch. |
| Block / parry guard-break (attacker stagger) | `parryStaggerImpulseBonus` | float | 1.75f | 1f–2f | Multiplies the block shove on a parry. 1.75 × 0.85 ≈ 1.5, a clearly harder impact than a block. |
| Block / parry guard-break (attacker stagger) | `blockStaggerHitstopSeconds` | float | 0f | 0f–0.25f | Enemy animator freeze on a normal block. Leave at 0 — a block does not hitstop the enemy or the player. The mild flinch and the short guard pose carry the impact. |
| Block / parry guard-break (attacker stagger) | `parryStaggerHitstopSeconds` | float | 0.12f | 0f–0.3f | Enemy animator freeze on a parry only. About 0.10–0.15s (default 0.12s), the short pose-hold used by Souls, Sekiro, and God of War on a real parry. Local animator speed, not Time.timeScale. Does not freeze the player. |
| Dummy spring / stagger | `trainingDummyPositionSpring` | float | 20f | 1f–80f |  |
| Dummy spring / stagger | `trainingDummyPositionDamping` | float | 7f | 0f–30f |  |
| Dummy spring / stagger | `trainingDummyRotationSpring` | float | 24f | 1f–80f |  |
| Dummy spring / stagger | `trainingDummyRotationDamping` | float | 8f | 0f–30f |  |
| Dummy spring / stagger | `trainingDummyHitImpulse` | float | 0.85f | 0f–5f |  |
| Dummy spring / stagger | `trainingDummyHitTorque` | float | 32f | 0f–180f |  |
| Dummy spring / stagger | `trainingDummyMaxPositionOffset` | float | 0.45f | 0.05f–2f |  |
| Dummy spring / stagger | `trainingDummyMaxRotationOffset` | float | 24f | 1f–90f |  |
| Dummy spring / stagger | `trainingDummyPoiseStaggerImpulseScale` | float | 1f | 0.25f–6f |  |
| Dummy pivot reaction (2026-10-24m) | `trainingDummyPivotBase` | bool | true |  | On = base stays planted, the dummy only tilts around its base (no sliding). Off = Classic legacy spring above (slides + tilts). |
| Dummy pivot reaction (2026-10-24m) | `trainingDummyTiltFrequency` | float | 5f | 0.5f–30f | Return-to-upright speed (spring frequency, Hz). Higher = snappier. |
| Dummy pivot reaction (2026-10-24m) | `trainingDummyTiltDampingRatio` | float | 0.8f | 0.05f–3f | Damping ratio. 1 = critically damped (no overshoot), <1 wobbles, >1 sluggish. |
| Dummy pivot reaction (2026-10-24m) | `trainingDummyMaxTiltDegrees` | float | 8f | 0f–80f | Hard cap on tilt angle (degrees). |
| Dummy pivot reaction (2026-10-24m) | `trainingDummyMeleeTiltKick` | float | 110f | 0f–2000f | Tilt kick (deg/s) from a melee hit (source within Melee Distance). |
| Dummy pivot reaction (2026-10-24m) | `trainingDummyRangedTiltKick` | float | 60f | 0f–2000f | Tilt kick (deg/s) from a ranged hit / lightning bolt or chain arrival. |
| Dummy pivot reaction (2026-10-24m) | `trainingDummyBeamTickTiltKick` | float | 8f | 0f–500f | Tilt kick (deg/s) from each continuous beam / lightning damage tick. |
| Dummy pivot reaction (2026-10-24m) | `trainingDummyKickWindowSeconds` | float | 0.3f | 0f–3f | Window (s) over which hit kicks accumulate before the cap applies. 0 = no cap. |
| Dummy pivot reaction (2026-10-24m) | `trainingDummyKickWindowCap` | float | 140f | 0f–5000f | Max total kick (deg/s) per window, so fast ticks / chains cannot stack the dummy over. |
| Dummy pivot reaction (2026-10-24m) | `trainingDummyMeleeDistance` | float | 3f | 0.25f–15f | Hits whose source is within this distance count as melee (TakeDamage carries no hit type). |
| I-Frames | `dodgeIFrameSeconds` | float | 0.25f | 0.05f–1f |  |
| I-Frames | `chargedMaxHoldSeconds` | float | 1f | 0f–5f | Max charged-attack hold (s), player and enemies, counted from the press / charged start. When reached the attack auto-releases and follows through. 0 = no cap. |
| Player melee stamina (spent when the swing starts, hit or miss) | `meleeLightStaminaCost` | float | 8f | 0f–100f | Stamina per light swing. |
| Player melee stamina (spent when the swing starts, hit or miss) | `meleeHeavyStaminaCost` | float | 14f | 0f–100f | Stamina per heavy swing (interact-hold combo attack). |
| Player melee stamina (spent when the swing starts, hit or miss) | `meleeChargedStaminaCost` | float | 22f | 0f–100f | Stamina per charged (hold) attack release. |
| Player melee stamina (spent when the swing starts, hit or miss) | `meleeStaminaAlsoOnHit` | bool | false |  | Also spend the weapon's own ItemData meleeStaminaCost on every landed hit (old behaviour; double-charges with the costs above). |
| Player block stamina | `playerBlockHoldCostBase` | float | 2f | 0f–30f | Holding block drains stamina per second = base x growth ^ seconds held (capped). Runs out = guard breaks. |
| Player block stamina | `playerBlockHoldCostGrowth` | float | 1.6f | 1f–4f |  |
| Player block stamina | `playerBlockHoldCostCap` | float | 25f | 0f–100f |  |
| Player block stamina | `playerBlockedHitStaminaCost` | float | 10f | 0f–100f | Flat stamina per blocked hit. |
| Player block stamina | `playerStaggerStaminaCost` | float | 18f | 0f–100f | Stamina lost when the player is staggered (more than a simple block). |
| Player block stamina | `playerGuardBreakLockSeconds` | float | 1.2f | 0f–5f | Seconds block is locked after the guard breaks from empty stamina. |
| Player block stamina | `playerRollGuard` | bool | true |  | Player roll guard: the Roll clip cannot be cut (snap back to standing) or re-triggered before playerRollMinNormalized. |
| Player block stamina | `playerRollMinNormalized` | float | 0.7f | 0f–1f |  |
| Player block stamina | `dashIFrameSeconds` | float | 0.25f | 0.05f–1f |  |
| Player holsters (2026-10-12) | `holsterAllHotbarWeapons` | bool | true |  | Every weapon in the 4 hotbar weapon slots (mining tool, pistol, rifles, melee) shows holstered on the body when it is not the drawn one. Off = only the equipped weapon shows (old rule). |
| Player holsters (2026-10-12) | `holsterOneMeleePerLocation` | bool | true |  | Melee: only one holstered melee per body location (Spine / Left leg / Right leg). Location comes from where the Holstered_ slot is placed in the rig. |
| Player holsters (2026-10-12) | `holsterMaxMeleeVisible` | int | 4 | 1–4 | Max melee weapons visible at once, the drawn one included (1 spine + 1 left leg + 1 right leg + 1 in hand = 4). |
| Player holsters (2026-10-12) | `holsterOneRangedPerLocation` | bool | false |  | Also allow only one ranged weapon per location (off = all hotbar ranged weapons show). |
| Player stagger (enemy hits on the player) | `playerStaggerUseProfile` | bool | true |  | On = these chances drive the player hit reaction (the player controller is immortal, so Invector never played it; the reaction is now triggered directly). Off = PioneerAnimationPlanSettings small-hit chances. |
| Player stagger (enemy hits on the player) | `playerStaggerChanceMin` | float | 0.2f | 0f–1f | Stagger chance at full health / normal hit. |
| Player stagger (enemy hits on the player) | `playerStaggerChanceMax` | float | 0.4f | 0f–1f | Stagger chance at low health or on a crit. |
| Player stagger (enemy hits on the player) | `playerStaggerImmunitySeconds` | float | 1.5f | 0f–6f | After a stagger, no new stagger for this long (no stun-lock in group fights). |
| Player stagger (enemy hits on the player) | `playerStaggerInterruptsCharge` | bool | true |  | Allow a stagger while the player holds a charged attack (interrupts the charge). |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `enablePlayerHitReactions` | bool | true |  | On = DMPlayerHitReaction (lean / light / heavy / rare knockdown) replaces the stock Invector TriggerReaction. The stagger chances above still gate light/heavy. |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerLeanDegreesMin` | float | 10f | 0f–40f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerLeanDegreesMax` | float | 14f | 0f–40f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerLeanAttackSeconds` | float | 0.06f | 0.01f–0.3f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerLeanSettleSeconds` | float | 0.3f | 0.05f–1f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerHeadWhipScale` | float | 0.7f | 0f–1.5f | Multiplier on the shared headWhipMultiplier (player camera rides the head less). |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerLeanJitterDegrees` | float | 18f | 0f–45f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerIntensityJitter` | float | 0.15f | 0f–0.5f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerReactionIntensityMax` | float | 0.85f | 0.5f–1.5f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerClipSpeedMin` | float | 0.95f | 0.5f–1.5f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerClipSpeedMax` | float | 1.1f | 0.5f–1.5f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerClipWeightMin` | float | 0.7f | 0.1f–1.2f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerClipWeightMax` | float | 0.9f | 0.1f–1.2f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerHeavyDamageFraction` | float | 0.18f | 0.05f–0.6f | A hit of at least this fraction of max HP counts as heavy (also Invector big reaction / crit). |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerPushMeters` | float | 0.25f | 0f–1f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerPushSeconds` | float | 0.25f | 0.05f–1f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerStumbleChance` | float | 0.35f | 0f–1f | Chance a heavy push becomes a longer stumble (x1.6). |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerLightLockSeconds` | float | 0.2f | 0f–1f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerHeavyLockSeconds` | float | 0.35f | 0f–1.5f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerHeavyMoveScale` | float | 0.4f | 0f–1f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerKnockdownChance` | float | 0.05f | 0f–0.5f | Knockdown chance on a HEAVY hit only. |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerKnockdownBonusChance` | float | 0.04f | 0f–0.5f | Added on crit or HP < 30%. |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerKnockdownChanceCap` | float | 0.12f | 0f–0.5f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerKnockdownCooldownSeconds` | float | 20f | 0f–120f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerDownHoldSecondsMin` | float | 0.6f | 0.1f–3f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerDownHoldSecondsMax` | float | 1f | 0.1f–3f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerDownedDamageScale` | float | 0.5f | 0f–1f | Damage multiplier while knocked down. |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerGetUpIFrameSeconds` | float | 0.6f | 0f–2f | Invulnerable for this long from get-up start (and after a roll-cancel). |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerRollCancel` | bool | true |  |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerRollCancelAfterSeconds` | float | 0.4f | 0f–2f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerReactionsUnderGodMode` | DMPlayerReactionsUnderGodMode | DMPlayerReactionsUnderGodMode.LeanAndLight |  | God mode / audit: which tiers still play (default lean + light; no heavy push, no knockdown). |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerAdsLeanScale` | float | 0.5f | 0f–1f |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `logPlayerHitReactions` | bool | false |  |  |
| Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits) | `playerHitSoundUnderGodMode` | bool | true |  | God mode / audit: enemy hits still play the player body-hit sound (health does not drop, so SurvivalStats stays silent). |
| Player feel guards (shake while blocking / charging) | `guardImpactMinIntervalSeconds` | float | 0.45f | 0f–2f | Min seconds between guard-impact recoil poses while holding block. Hits inside the window are still absorbed, just no new pose (group fights re-triggered it every hit = shaking). |
| Player feel guards (shake while blocking / charging) | `blockShakeMinIntervalSeconds` | float | 0.35f | 0f–2f | Min seconds between block camera shakes. |
| Player feel guards (shake while blocking / charging) | `chargeHoldPinTolerance` | float | 0.05f | 0f–0.15f | Charged-attack hold: the charge clip is re-pinned only after it drifts this far (normalized) past the hold frame. 0 = old behaviour (re-pinned every frame = jitter). |
| Melee facing target stickiness (block / attack auto-face) | `meleeFacingTargetLockSeconds` | float | 1.5f | 0f–5f | Once auto-face picks an enemy it keeps it at least this long (unless it dies / leaves range). Stops the body flipping between enemies of a group between swings. |
| Melee facing target stickiness (block / attack auto-face) | `meleeFacingSwitchCloserFraction` | float | 0.7f | 0.1f–1f | After the lock, switch only when another enemy is closer than this fraction of the current target's distance (0.7 = 30% closer). |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeComboTargetLock` | bool | true |  | On = one target per combo, eased wind-up-only turn in FixedUpdate (single rotation owner). Off = legacy per-frame ApplyAttackFacing. |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeTargetAcquireDistance` | float | 4.5f | 1f–10f |  |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeTargetReleaseDistance` | float | 6.5f | 1f–15f |  |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeTargetIntentConeHalfAngle` | float | 60f | 10f–180f | Half-angle of the intent cone around the stick (else camera) direction for acquiring. |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeTargetAngleWeight` | float | 1f | 0f–3f | Score = distance/acquire + weight * angle/cone. |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeComboLockGraceSeconds` | float | 0.3f | 0f–2f |  |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeRetargetStickAngle` | float | 45f | 10f–180f | Stick must point more than this away from the target to retarget. |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeRetargetMinSeconds` | float | 0.4f | 0f–2f |  |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeRetargetStickMagnitude` | float | 0.5f | 0.1f–1f |  |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeAssistTurnSpeed` | float | 360f | 30f–1080f | Max assist turn speed (deg/s), eased. |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeAssistSmoothTime` | float | 0.08f | 0.01f–0.5f |  |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeAssistMaxPerSwingDegrees` | float | 70f | 0f–180f |  |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeAssistWindupFraction` | float | 0.35f | 0.05f–1f | Turn only during the first fraction of each swing (wind-up). |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeAssistSoftFadeDegrees` | float | 22f | 1f–90f | Turn speed fades in over this error (no hard deadband edge). |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `meleeTargetIgnoreDowned` | bool | true |  |  |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `lightComboForcesStrafe` | bool | false |  | Legacy: light combo attack-move forces strafe on (caused feet pivot). Default off. |
| Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d) | `logMeleeFacing` | bool | false |  |  |
| Melee block target lock (DMMeleeAttackFacing, 2026-10-19 e) | `meleeBlockTargetLock` | bool | true |  | On = block auto-face uses the same sticky target / cone / eased FixedUpdate turn as attacks (acquire / release / cone / retarget settings above). Off = legacy per-frame nearest pick. |
| Melee block target lock (DMMeleeAttackFacing, 2026-10-19 e) | `meleeBlockAssistTurnSpeed` | float | 300f | 30f–1080f |  |
| Melee block target lock (DMMeleeAttackFacing, 2026-10-19 e) | `meleeBlockAssistSmoothTime` | float | 0.1f | 0.01f–0.5f |  |
| Melee block target lock (DMMeleeAttackFacing, 2026-10-19 e) | `meleeBlockLockGraceSeconds` | float | 0.25f | 0f–2f |  |
| Melee block target lock (DMMeleeAttackFacing, 2026-10-19 e) | `meleeBlockFacingToleranceDegrees` | float | 3f | 0f–20f | No turn while the guard already faces the target within this angle (no micro-turning). |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `enableChargedSlowMo` | bool | true |  | Charged attack: slow-mo when the hit window opens and an enemy is predicted to be hit (in reach + cone). |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `chargedSlowMoChance` | float | 0.1f | 0f–1f |  |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `chargedSlowMoCooldownSeconds` | float | 15f | 0f–120f |  |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `chargedSlowMoSeconds` | float | 3f | 2f–4f | Total real-time length incl. ease in / out. |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `chargedSlowMoTimeScale` | float | 0.3f | 0.05f–1f |  |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `chargedSlowMoEaseInSeconds` | float | 0.12f | 0f–1f |  |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `chargedSlowMoEaseOutSeconds` | float | 0.8f | 0.05f–2f |  |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `chargedSlowMoReach` | float | 2.4f | 0.5f–5f | Predicted hit: nearest standing enemy within this distance... |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `chargedSlowMoConeHalfAngle` | float | 35f | 5f–90f | ...and within this half-angle of the player's forward. |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `enableParrySlowMo` | bool | true |  | Perfect parry: block press with an incoming enemy attack starts the before half; a confirmed parry holds the after half. |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `parrySlowMoChance` | float | 1f | 0f–1f |  |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `parrySlowMoTotalSeconds` | float | 2f | 1f–3f | Total real time, split evenly before / after the parry moment. |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `parrySlowMoTimeScale` | float | 0.25f | 0.05f–1f |  |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `parrySlowMoEaseInSeconds` | float | 0.08f | 0f–1f |  |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `parrySlowMoEaseOutSeconds` | float | 0.35f | 0.05f–2f |  |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `parrySlowMoThreatRadius` | float | 3.5f | 1f–8f |  |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `parrySlowMoCooldownSeconds` | float | 3f | 0f–30f |  |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `slowMoAllowInAudit` | bool | false |  | Audit only: allow slow-mo while an audit session runs (off = audits never slow time). |
| Slow motion (DMSlowMotionDirector, 2026-10-19 e) | `logSlowMo` | bool | false |  |  |
| Hitch monitor (lightweight, play mode) | `logHitchesInPlay` | bool | false |  | Logs any frame longer than hitchThresholdMs with GC counts and the last combat event (blood spawn, hit, etc.). Off = zero cost. |
| Hitch monitor (lightweight, play mode) | `hitchThresholdMs` | float | 120f | 30f–1000f |  |
| Player quick step (triple-tap A / D = sidestep, triple-tap S = backstep; pad: triple stick flick) | `quickStepEnabled` | bool | true |  | Master switch. Dash (B) and jetpack (Space double-tap) are separate and unchanged. No roll fallback. |
| Player quick step (triple-tap A / D = sidestep, triple-tap S = backstep; pad: triple stick flick) | `quickStepTapWindowSeconds` | float | 0.5f | 0.2f–1.2f | All three taps must land within this many seconds. |
| Player quick step (triple-tap A / D = sidestep, triple-tap S = backstep; pad: triple stick flick) | `sidestepDistance` | float | 1.8f | 0.3f–5f |  |
| Player quick step (triple-tap A / D = sidestep, triple-tap S = backstep; pad: triple stick flick) | `backstepDistance` | float | 1.5f | 0.3f–5f |  |
| Player quick step (triple-tap A / D = sidestep, triple-tap S = backstep; pad: triple stick flick) | `quickStepDurationSeconds` | float | 0.2f | 0.05f–0.6f | Seconds the step takes (eased). |
| Player quick step (triple-tap A / D = sidestep, triple-tap S = backstep; pad: triple stick flick) | `quickStepCooldownSeconds` | float | 0.4f | 0f–3f |  |
| Player quick step (triple-tap A / D = sidestep, triple-tap S = backstep; pad: triple stick flick) | `quickStepStickFlick` | float | 0.8f | 0.3f–1f | Gamepad: stick must pass this to count a flick, and drop below quickStepStickRelease to re-arm. |
| Player quick step (triple-tap A / D = sidestep, triple-tap S = backstep; pad: triple stick flick) | `quickStepStickRelease` | float | 0.3f | 0f–0.6f |  |
| Player quick step (triple-tap A / D = sidestep, triple-tap S = backstep; pad: triple stick flick) | `quickStepStaminaCost` | float | 0f | 0f–100f | Stamina cost per step (0 = free). |
| Holstered weapon sway (player + enemies, runtime only) | `holsterSwayAmount` | float | 1f | 0f–3f | Holstered_ weapons swing on a spring when walking / running / fighting. Rest pose = the hand-set placement; nothing is saved. 0 = off. |
| Holstered weapon sway (player + enemies, runtime only) | `holsterSwayPivot` | float | 0.66f | 0.3f–1f | Pivot position along the weapon, measured from its lower end (0.66 = two thirds up). |
| Holstered weapon sway (player + enemies, runtime only) | `holsterSwayAdvanced` | HolsterSwayAdvanced | new HolsterSwayAdvanced() |  | Advanced spring settings (collapsed). |
| Holstered weapon sway (player + enemies, runtime only) | `stiffness` | float | 60f | 5f–400f |  |
| Holstered weapon sway (player + enemies, runtime only) | `damping` | float | 6f | 0f–40f |  |
| Holstered weapon sway (player + enemies, runtime only) | `inertia` | float | 4f | 0f–20f | How strongly body acceleration swings the weapon. |
| Holstered weapon sway (player + enemies, runtime only) | `gravity` | float | 6f | 0f–50f | Pull toward hanging straight down (0 = rest pose only). |
| Holstered weapon sway (player + enemies, runtime only) | `maxAngle` | float | 14f | 1f–45f |  |
| Status (global rules) | `statusMaxStacks` | int | 3 | 1–10 |  |
| Status (global rules) | `statusImmunityWindowSeconds` | float | 2f | 0f–10f |  |
| Status (global rules) | `statusBossMultiplier` | float | 0.5f | 0.1f–1f |  |
| Status (global rules) | `statusVfxNormalOffset` | float | 0.004f | 0f–0.05f | World offset along the hit outward normal when parenting status DoT VFX (ION corroded orb, etc.). |
| Melee hit detection | `playerMeleeHitboxUseAuthoredSize` | bool | true |  | 2026-10-20d: ON = player weapon hitboxes keep their hand-authored size (width/reach scales below are NOT applied to the player). Enemy scales still apply. OFF = legacy runtime enlargement. |
| Melee hit detection | `meleeHitboxWidthScale` | float | 2.1f | 1f–2.5f | Scales Invector vHitBox trigger width (left/right thickness). |
| Melee hit detection | `meleeHitboxReachScale` | float | 1.75f | 1f–2.5f | Scales reach along the blade (box depth / length). |
| Melee hit detection | `meleeBlockDefenseHalfAngle` | float | 130f | 45f–180f | Half-angle (degrees) from body forward for melee block/parry. Invector default is 90 (180° frontal). |
| Melee hit detection | `meleeBlockAutoFaceHalfAngle` | float | 30f | 5f–75f | Hold block (RMB): auto-face the nearest threat when their bearing exceeds this many degrees off forward. |
| Melee hit detection | `meleeBlockAutoFaceMaxDistance` | float | 4.5f | 2f–8f | Max horizontal distance to snap facing while blocking. |
| Melee hit detection | `meleeBlockAutoFaceTurnSpeed` | float | 680f | 180f–900f | Yaw degrees per second while block-assist turning. |
| Melee hit detection | `meleeAttackAutoFaceHalfAngle` | float | 22f | 5f–45f | Light/strong swing and strong charge: auto-face nearest threat when bearing exceeds this half-angle (tighter than block). |
| Melee hit detection | `enemyMeleeHitboxWidthScale` | float | 2.25f | 1f–2.5f | Extra reach scale for humanoid enemy melee hitboxes only. |
| Melee hit detection | `enemyMeleeHitboxReachScale` | float | 2.15f | 1f–2.5f |  |
| Melee hit detection | `enemyMeleeHitboxUseAuthoredSize` | bool | true |  | 2026-10-20d: ON = enemy weapon hitboxes keep their authored (mesh-fit) size; the enemy width/reach scales above are not applied. |
| Melee hit detection | `enemyMeleeAttackRangeMultiplier` | float | 1.26f | 1f–1.5f | Multiplies EnemyCombat.attackRange for AI strike distance (prefab values are often short). |
| Melee hit detection | `enemyMeleeStandoffFraction` | float | 0.56f | 0f–0.95f | When > 0, overrides each enemy prefab attackStandoffFraction for melee ring / creep stop (lower = stand closer). 0 = prefab only. |
| Melee hit detection | `enemyMeleeMaxOrbitFactor` | float | 0.9f | 0.75f–1f | Caps combat standoff at effective attack range × this (contact band inside weapon reach). |
| Melee hit detection | `enemyIntentionalMeleeMissChance` | float | 0.1f | 0f–1f | Per swing sequence: roll to play the attack anim but suppress outgoing melee damage (Invector hitboxes stay on for feedback). |
| Melee hit detection | `enemyMeleeMissStepSeconds` | float | 0.55f | 0.15f–1.5f | After an intentional miss, the engager brain steps in toward the target for this long (no NavMesh). |
| Melee hit detection | `enemyMeleeMissStepStopFactor` | float | 0.72f | 0.5f–0.95f | Miss recovery creep stops once planar distance is within effective attack range × this. |
| Melee hit detection | `enemyMeleeMissStepSpeedMultiplier` | float | 0.95f | 0.5f–1.2f | Walk speed multiplier while miss-recovery step-in is active. |
| Melee hit detection | `meleeHitYawForgivenessDegrees` | float | 0f | 0f–45f | Unused (legacy). Forgiveness is handled by scaled primary hitbox + per-swing target dedupe. |
| Light melee combo (Weak SwordAttack A→B→C) | `lightComboAnimSpeedA` | float | 1f | 0.75f–2f | Play-mode Animator.speed in WeakAttacks/SwordAttack A. 1 = clip default, 1.25 = 25% faster. Future knife/baton: add parallel A/B/C triplets or a weapon-family nested profile — keep named slots for now. |
| Light melee combo (Weak SwordAttack A→B→C) | `lightComboAnimSpeedB` | float | 1f | 0.75f–2f | WeakAttacks/SwordAttack B. 1 = normal, 1.25 = +25% faster. |
| Light melee combo (Weak SwordAttack A→B→C) | `lightComboAnimSpeedC` | float | 1f | 0.75f–2f | WeakAttacks/SwordAttack C. 1 = normal, 1.25 = +25% faster. |
| Light melee combo (Weak SwordAttack A→B→C) | `enableLightComboAttackMove` | bool | true |  | Crimson-style attack move: camera-relative strafe during chained WeakAttacks/SwordAttack A→B→C only (not strong, charge, parry, or Hold-E upper-body combo). |
| Light melee combo (Weak SwordAttack A→B→C) | `lightComboStrafeSpeedMultiplier` | float | 0.65f | 0.35f–1.2f | Multiplies locomotion speed while strafing during the weak A→B→C chain when attack move is on. |
| Light melee combo (Weak SwordAttack A→B→C) | `lightComboForwardDriftMultiplier` | float | 0.12f | 0f–0.45f | Minimum forward input (0–1) blended in during weak combo attack move when strafing or idle — small drift toward camera forward. |
| Light melee combo (Weak SwordAttack A→B→C) | `lockMovementDuringLightCombo` | bool | false |  | When off (default with attack move on), Invector LockMovement on weak A/B/C is bypassed so WASD strafe works. When on, weak combo plants like legacy Invector. |
| Light random pool (Weak SwordRandomAttack A→B→C) | `lightRandomAnimSpeedA` | float | 1f | 0.75f–2f | Parallel random weak swings (not the A→B→C chain). 1 = clip default, 1.25 = 25% faster. |
| Light random pool (Weak SwordRandomAttack A→B→C) | `lightRandomAnimSpeedB` | float | 1f | 0.75f–2f |  |
| Light random pool (Weak SwordRandomAttack A→B→C) | `lightRandomAnimSpeedC` | float | 1f | 0.75f–2f |  |
| Strong melee slots (A/C — B + charge below) | `strongMeleeAnimSpeedA` | float | 1f | 0.75f–2f | StrongAttacks/SwordAttack A (non-charge heavy). Charge + Strong B use strongMeleeAnimSpeedMultiplier. |
| Strong melee slots (A/C — B + charge below) | `strongMeleeAnimSpeedC` | float | 1f | 0.75f–2f | StrongAttacks/SwordAttack C. 1 = normal, 1.25 = +25% faster. |
| Strong melee (hold light attack, then release) | `strongMeleeChargeSeconds` | float | 0.4f | 0.2f–0.8f | Seconds the light-attack button (left mouse / Attack) must be held before a release plays the strong sword swing. After this threshold the charge pose stays until release. There is no maximum hold and the swing does not fire by itself. A shorter press-and-release stays a light tap. Right mouse stays block. |
| Strong melee (hold light attack, then release) | `strongMeleeDamageMultiplier` | float | 1.8f | 1.2f–3f | Scales the normal melee damage roll on that charged swing. 1.8 is clearly above a light hit. |
| Strong melee (hold light attack, then release) | `strongMeleeDamageStartNormalized` | float | 0.72f | 0.05f–0.95f | Normalized clip time when charged release (Strong SwordAttack B) hitboxes turn on. Late in the swing. |
| Strong melee (hold light attack, then release) | `strongMeleeDamageEndNormalized` | float | 0.98f | 0.1f–1f | Normalized clip time when charged release hitboxes turn off. |
| Strong melee (hold light attack, then release) | `strongMeleeAnimSpeedMultiplier` | float | 1.25f | 0.75f–2f | Play-mode speed for StrongAttacks/SwordCharge and charged Strong A/B/C release (1 = clip default, 1.25 = 25% faster, 0.75 = slowest allowed). Applied at runtime from profile.Live; Build And Apply only syncs controller clips. |
| Strong melee (hold light attack, then release) | `strongReleaseWeightA` | float | 0.55f | 0f–1f | Relative weights when picking Strong A/B/C on charged release (normalized at runtime). Favor A for the default heavy. |
| Strong melee (hold light attack, then release) | `strongReleaseWeightB` | float | 0.22f | 0f–1f |  |
| Strong melee (hold light attack, then release) | `strongReleaseWeightC` | float | 0.23f | 0f–1f |  |
| Strong melee (hold light attack, then release) | `interactHoldComboAnimSpeed` | float | 1.75f | 0.75f–2.5f | Hold E + light attack (InteractHoldCombo state). Default 1.75 matches design. |
| Strong melee (hold light attack, then release) | `chargedHitsIgnoreEnemyWeapons` | bool | true |  | Charged Strong B / AttackC ignores the defender's sword and other outgoing weapon volumes. Damage waits for a real body / dummy hit. Block and parry still register as guard. |
| Slice 1 feel pack: melee hit stop (A1) | `enableMeleeHitstop` | bool | true |  | On = a landed player melee hit briefly freezes the HIT enemy's animator (and the player's swing) for hitstopLightFrames / hitstopHeavyFrames below. Local animator speed dip only, never Time.timeScale. Other enemies keep moving. |
| Slice 1 feel pack: melee hit stop (A1) | `hitstopCritBonusFrames` | int | 2 | 0–6 | Extra freeze frames (60 fps) on a critical hit, added to the light/heavy frames. |
| Slice 1 feel pack: melee hit stop (A1) | `hitstopHoldSpeed` | float | 0.3f | 0f–0.5f | Animator speed during the freeze (enemy and, with Hitstop Freezes Attacker, the player swing). 0 = fully frozen. Was 0.05 (near-frozen) which made the player judder when root motion / the motor stopped and restarted; 0.3 is a soft hold (shakefix). |
| Slice 1 feel pack: melee hit stop (A1) | `playerRigidbodyInterpolation` | PlayerRigidbodyInterpolationMode | PlayerRigidbodyInterpolationMode.Interpolate |  | Rigidbody interpolation applied at runtime to the live player root (Player_v7 prefab untouched). The prefab ships with None: the motor moves in FixedUpdate (60 Hz) while rendering runs at the frame rate, which shows up as a small shake / judder. Interpolate smooths it. Keep = leave as the prefab has it. |
| Slice 1 feel pack: melee hit stop (A1) | `playerSelfCollisionGuard` | bool | true |  | Keep the player's own child colliders (weapons, foot spheres, bones) from shoving the root capsule: IgnoreCollision re-applied every interval, solid colliders in the root rigidbody compound become triggers. Off = legacy. |
| Slice 1 feel pack: melee hit stop (A1) | `playerSelfCollisionGuardInterval` | float | 0.25f | 0.05f–2f | Seconds between guard passes (Unity drops ignore pairs when a collider is re-enabled). |
| Slice 1 feel pack: melee hit stop (A1) | `playerSelfCollisionForceRootTriggers` | bool | true |  | Solid colliders attached to the player ROOT rigidbody (e.g. a re-equipped weapon pickup collider) become triggers. |
| Slice 1 feel pack: melee hit stop (A1) | `playerSelfCollisionGuardLog` | bool | true |  | Console log (once per collider) whenever the guard fixes something. Use to find the offender. |
| Slice 1 feel pack: melee hit stop (A1) | `playerCameraFixedStepInterpolation` | bool |  |  | Invector's camera moves in FixedUpdate while the player Rigidbody is interpolated per frame, so the player shimmers on screen while strafing + panning. ON = render the camera interpolated between its fixed steps (Invector logic untouched). Off = legacy. |
| Slice 1 feel pack: melee hit stop (A1) | `playerRotateViaRigidbody` | bool |  |  | Player turn (RotateToDirection, e.g. strafe facing the camera) goes through Rigidbody.MoveRotation when the body is interpolated, instead of writing transform.rotation (which breaks interpolation and steps the turn at physics rate). Off = legacy. |
| Slice 1 feel pack: melee hit stop (A1) | `turretPooledLightRescanLegacy` | bool |  |  | ON = legacy: rescan the whole FORGE3D GeneratedPool for lights every 2 s while turrets fire (frame hitch, seen as a 2 s player jerk). OFF = strip each pooled light once, rescan only when the pool grows. |
| Slice 1 feel pack: melee hit stop (A1) | `playerAnimatorPerFrameUpdate` | bool |  |  | Run the player's Animator every rendered frame (Normal) instead of Invector's physics-tick (Fixed) update, so the pose and aim IK stop stepping against the smooth, interpolated root. Off = Invector default (Fixed). |
| Slice 1 feel pack: melee hit stop (A1) | `playerSnapProbeLog` | bool | true |  | Console warning whenever the moving player snaps backward: RB (a position correction) or VISUAL (animation loop vs capsule), with the gap since the last one and the DM script that corrected position, if any. |
| Slice 1 feel pack: melee hit stop (A1) | `playerSnapProbeLogEveryCorrection` | bool |  |  | Also log every DM position correction (terrain rescue, landing feet snap, step lift) even without a detected snap. Noisy. |
| Slice 1 feel pack: melee hit stop (A1) | `terrainRescueSkipWhenGrounded` | bool | true |  | Terrain rescue: skip while the player is grounded per Invector (only rescue real fall-through). Off = legacy (checks every 0.1 s regardless). |
| Slice 1 feel pack: melee hit stop (A1) | `hitstopFreezesAttacker` | bool | true |  | Also freeze the player's own swing for the same time (the blade 'sticks'). Off = victim only. |
| Slice 1 feel pack: light / heavy melee hit shake (A2) | `lightHitShakeAmplitude` | float | 0.05f | 0f–1f | Trauma on a normal player melee hit that damages an enemy. Very subtle by default. 0 = off. |
| Slice 1 feel pack: light / heavy melee hit shake (A2) | `lightHitShakeDurationSeconds` | float | 0.04f | 0f–0.4f |  |
| Slice 1 feel pack: light / heavy melee hit shake (A2) | `heavyHitShakeAmplitude` | float | 0.20f | 0f–1f | Trauma on a heavy non-charged hit: a critical, a poise break or a killing blow. Charged hits keep chargedHitShake*. |
| Slice 1 feel pack: light / heavy melee hit shake (A2) | `heavyHitShakeDurationSeconds` | float | 0.08f | 0f–0.4f |  |
| Slice 1 feel pack: damage number tiers (A3) | `useDamageNumberTiers` | bool | true |  | On = damage numbers on enemies use the tier colors/sizes below (normal / crit / heavy). Off = legacy red and yellow. Orange is a number colour only, not a telegraph. |
| Slice 1 feel pack: damage number tiers (A3) | `damageNumberNormalColor` | Color | new Color(1f, 0.96f, 0.78f, 1f) |  |  |
| Slice 1 feel pack: damage number tiers (A3) | `damageNumberCritColor` | Color | new Color(1f, 0.55f, 0.10f, 1f) |  |  |
| Slice 1 feel pack: damage number tiers (A3) | `damageNumberHeavyColor` | Color | new Color(1f, 0.40f, 0.05f, 1f) |  |  |
| Slice 1 feel pack: damage number tiers (A3) | `damageNumberCritScale` | float | 1.25f | 0.6f–2f |  |
| Slice 1 feel pack: damage number tiers (A3) | `damageNumberHeavyScale` | float | 1.4f | 0.6f–2f |  |
| Slice 2 hit reactions (B1-B4) | `enableHitReactions` | bool | true |  | Master switch for the Slice 2 hit reactions: directional flinch / backstep (B1), hit-chain escalation (B2), heavy hit slide-back + waist bend (B3) and hit recovery before the enemy swings again (B4). Off = Slice 1 behaviour. |
| Slice 2 hit reactions (B1-B4) | `hitRecoverySeconds` | float | 1.6f | 0f–4f | B4: seconds the enemy cannot start a new swing after a poise break, a heavy-hit Big reaction or a chain stumble. Extends the attack lockout only; postSwingLockSeconds is not touched. |
| Slice 2 hit reactions (B1-B4) | `hitRecoveryOnPoiseBreak` | bool | true |  |  |
| Slice 2 hit reactions (B1-B4) | `hitRecoveryOnHeavyHit` | bool | true |  |  |
| Slice 2 hit reactions (B1-B4) | `hitRecoveryOnChainStumble` | bool | true |  |  |
| Slice 2: directional flinch (B1) | `directionalFlinchEnabled` | bool | true |  | On = a normal player melee hit twists the enemy's torso toward the swing direction and gives a tiny backstep (procedural, on top of the existing light flinch). |
| Slice 2: directional flinch (B1) | `swingSideLightA` | float | 1f | -1f–1f | Swing side of the player's light combo steps: +1 = left-to-right slash, -1 = right-to-left, 0 = overhead / thrust (no sideways twist). |
| Slice 2: directional flinch (B1) | `swingSideLightB` | float | -1f | -1f–1f |  |
| Slice 2: directional flinch (B1) | `swingSideLightC` | float | 0f | -1f–1f |  |
| Slice 2: directional flinch (B1) | `swingSideCharged` | float | 0f | -1f–1f | Swing side of charged / heavy swings. |
| Slice 2: directional flinch (B1) | `swingTwistDegrees` | float | 10f | 0f–25f | Torso twist (degrees) at full swing side. |
| Slice 2: directional flinch (B1) | `directionalFlinchDegrees` | float | 6f | 0f–15f | Lean-back (degrees) and duration of the directional flinch on a plain hit. |
| Slice 2: directional flinch (B1) | `directionalFlinchSeconds` | float | 0.28f | 0.15f–0.5f |  |
| Slice 2: directional flinch (B1) | `swingLateralBias` | float | 0.35f | 0f–1f | Share of the backstep that follows the swing sideways (0 = straight away from the player). |
| Slice 2: directional flinch (B1) | `lightBackstepMeters` | float | 0f | 0f–0.5f | Backstep on a plain hit (meters) and how long it takes. Slice 2b default 0 = light hits never move the enemy (was 0.08). |
| Slice 2: directional flinch (B1) | `lightBackstepSeconds` | float | 0.18f | 0.05f–0.5f |  |
| Slice 2: directional flinch (B1) | `poiseBreakBackstepMeters` | float | 0.35f | 0f–1.5f | Extra backstep (meters) on a poise break. |
| Slice 2: hit-chain escalation (B2) | `chainReactionsEnabled` | bool | true |  |  |
| Slice 2: hit-chain escalation (B2) | `hitChainWindowSeconds` | float | 1.2f | 0.3f–3f | Hits in a row on the same enemy inside this window count as a chain (resets when it lapses). |
| Slice 2: hit-chain escalation (B2) | `chainStrongHitCount` | int | 2 | 2–6 | Chain hit number that gets the stronger flinch + backstep. |
| Slice 2: hit-chain escalation (B2) | `chainStumbleHitCount` | int | 3 | 2–8 | Chain hit number that gets the full-body stumble (Big reaction, two backsteps, lean back). |
| Slice 2: hit-chain escalation (B2) | `chainStrongFlinchMultiplier` | float | 1.6f | 1f–3f |  |
| Slice 2: hit-chain escalation (B2) | `chainStrongBackstepMeters` | float | 0.25f | 0f–1f |  |
| Slice 2: hit-chain escalation (B2) | `chainStumbleSeconds` | float | 0.9f | 0.3f–2f |  |
| Slice 2: hit-chain escalation (B2) | `chainStumbleBackstepMeters` | float | 0.8f | 0f–2f |  |
| Slice 2: hit-chain escalation (B2) | `chainStumbleLeanDegrees` | float | 18f | 0f–30f |  |
| Slice 2: hit-chain escalation (B2) | `bigReactionImmunitySeconds` | float | 2.5f | 0f–8f | After a stumble, heavy-hit Big reaction or poise break the enemy cannot be Big-staggered again by hit chains / heavy hits for this long (cap against stun-lock). Poise breaks still always work. |
| Slice 2: heavy hit slide-back + waist bend (B3) | `heavyReactionEnabled` | bool | true |  |  |
| Slice 2: heavy hit slide-back + waist bend (B3) | `heavyBigReactionSeconds` | float | 0.7f | 0.3f–2f | Big reaction length on a charged / heavy hit (seconds). |
| Slice 2: heavy hit slide-back + waist bend (B3) | `heavySlideMeters` | float | 0.35f | 0f–2f | Slide-back distance (meters) and duration on a heavy hit. Slice 2b default 0.35 m (was 0.8) = push a bit. |
| Slice 2: heavy hit slide-back + waist bend (B3) | `heavySlideSeconds` | float | 0.3f | 0.1f–1f |  |
| Slice 2: heavy hit slide-back + waist bend (B3) | `heavyWaistBendDegrees` | float | 20f | 0f–40f | Procedural waist bend (degrees) and duration on a heavy hit. |
| Slice 2: heavy hit slide-back + waist bend (B3) | `heavyWaistBendSeconds` | float | 0.5f | 0.2f–1f |  |
| Slice 2: heavy hit slide-back + waist bend (B3) | `heavyRespectsCommitment` | bool | true |  | On = a heavy hit does not Big-stagger an enemy that is committed to a swing (swing not interrupted) or has attackArmor AllAttacks; it only bends the waist. |
| Slice 2: heavy hit slide-back + waist bend (B3) | `heavyFallbackScale` | float | 0.4f | 0f–1f | Slide distance multiplier when a heavy hit does not Big-stagger (immunity window). |
| Slice 2: heavy hit slide-back + waist bend (B3) | `hitSlideMaxLedgeDrop` | float | 0.5f | 0.1f–2f | Slides stop at a ledge: max ground drop (meters) under the next step. Total distance of stacked slides is capped at Max Total Slide. |
| Slice 2: heavy hit slide-back + waist bend (B3) | `hitSlideMaxTotalMeters` | float | 1.2f | 0.2f–3f |  |
| Slice 2b: directional reactions v2 (strike direction) | `enableDirectionalReactionsV2` | bool | true |  | Master switch for Slice 2b: lean along the actual strike direction, weighted spring lean with head whip, directional hit clips (medium / heavy) and the animation-only knockdown. Off = Slice 2 behaviour. |
| Slice 2b: directional reactions v2 (strike direction) | `swingDirectionSource` | DMSwingDirectionSource | DMSwingDirectionSource.BladeVelocity |  | BladeVelocity = read the player's blade motion at the hit (fallback: combo table below). ComboTable = Slice 2 swingSide table only. |
| Slice 2b: directional reactions v2 (strike direction) | `bladeVelocityMinSpeed` | float | 2f | 0f–10f | Blade speed (m/s, relative to the player root) below which the combo table is used instead. |
| Slice 2b: directional reactions v2 (strike direction) | `bladeVelocityWindowSeconds` | float | 0.05f | 0.016f–0.15f | How far back (seconds) the blade sampler looks to measure the swing. |
| Slice 2b: directional reactions v2 (strike direction) | `strikeLeanSideShare` | float | 0.7f | 0f–1f | Share of the lean that follows the blade sideways (rest = straight away from the player). 1 = pure strike direction. |
| Slice 2b: light lean (procedural, weighted, root never moves) | `lightLeanDegrees` | float | 16f | 0f–35f | Peak lean (degrees) of a plain light hit, spread over spine / chest / head. Light hits never move the root (lightBackstepMeters 0). |
| Slice 2b: light lean (procedural, weighted, root never moves) | `lightTwistDegrees` | float | 12f | 0f–30f | Torso twist (degrees) toward the strike side on a light hit. |
| Slice 2b: light lean (procedural, weighted, root never moves) | `lightLeanAttackSeconds` | float | 0.045f | 0.02f–0.15f | Seconds to snap to the peak lean (fast attack = impact). |
| Slice 2b: light lean (procedural, weighted, root never moves) | `lightLeanSettleSeconds` | float | 0.6f | 0.15f–1.5f | Seconds of the weighted settle after the peak (spring with overshoot). |
| Slice 2b: light lean (procedural, weighted, root never moves) | `leanSpringFrequency` | float | 2.6f | 0.5f–8f | Settle spring frequency (Hz). Lower = heavier, slower sway back. |
| Slice 2b: light lean (procedural, weighted, root never moves) | `leanSpringDamping` | float | 0.42f | 0.1f–1f | Settle spring damping ratio. Lower = more overshoot past neutral (weight); 1 = no overshoot. |
| Slice 2b: light lean (procedural, weighted, root never moves) | `leanSpineWeight` | float | 0.35f | 0f–1f |  |
| Slice 2b: light lean (procedural, weighted, root never moves) | `leanChestWeight` | float | 0.4f | 0f–1f |  |
| Slice 2b: light lean (procedural, weighted, root never moves) | `leanHeadWeight` | float | 0.25f | 0f–1f |  |
| Slice 2b: light lean (procedural, weighted, root never moves) | `headWhipDelaySeconds` | float | 0.035f | 0f–0.12f | Head whip: the head starts this many seconds after the torso and swings further (multiplier) - reads as a snap. |
| Slice 2b: light lean (procedural, weighted, root never moves) | `headWhipMultiplier` | float | 1.7f | 1f–3f |  |
| Slice 2b: light lean (procedural, weighted, root never moves) | `critLeanMultiplier` | float | 1.4f | 1f–2.5f | Lean / twist multiplier on a critical hit. |
| Slice 2b: light lean (procedural, weighted, root never moves) | `leanHoldPeakThroughHitstop` | bool | true |  | Hit stop punch: the lean snaps to its peak during the hit-stop freeze and holds there until the freeze ends, then settles. |
| Slice 2b: weight scaling (damage / heavy attack) | `reactionDamageReference` | float | 25f | 1f–500f | Damage that gives a 1x reaction. Reactions scale with (damage / this) ^ exponent, clamped to min / max. Weapon heft is read through damage (heavier weapons hit harder). |
| Slice 2b: weight scaling (damage / heavy attack) | `reactionDamageExponent` | float | 0.6f | 0.1f–2f |  |
| Slice 2b: weight scaling (damage / heavy attack) | `reactionDamageScaleMin` | float | 0.75f | 0.2f–1f |  |
| Slice 2b: weight scaling (damage / heavy attack) | `reactionDamageScaleMax` | float | 1.6f | 1f–3f |  |
| Slice 2b: weight scaling (damage / heavy attack) | `heavyAttackWeightMultiplier` | float | 1.35f | 1f–3f | Extra multiplier for charged / strong player attacks. |
| Slice 2b: weight scaling (damage / heavy attack) | `hitstopWeightBonusFrames` | int | 2 | 0–6 | Hit stop frames added at the top of the weight range (scaled by weight). Adds punch to big hits. 0 = hit stop unchanged. |
| Slice 2b: randomness (per hit) | `leanAngleJitterDegrees` | float | 14f | 0f–45f | Random +/- degrees added to the lean direction each hit. |
| Slice 2b: randomness (per hit) | `reactionIntensityRandomMin` | float | 0.85f | 0.3f–1f | Random intensity range multiplied into every reaction (lean, twist, clip weight). |
| Slice 2b: randomness (per hit) | `reactionIntensityRandomMax` | float | 1.2f | 1f–2f |  |
| Slice 2b: randomness (per hit) | `twistRandomDegrees` | float | 4f | 0f–15f | Random +/- degrees added to the twist each hit. |
| Slice 2b: randomness (per hit) | `twistSignFlipChance` | float | 0.2f | 0f–0.5f | Chance (0-1) the twist goes the opposite way (sign variance). |
| Slice 2b: randomness (per hit) | `clipSpeedRandomMin` | float | 0.9f | 0.5f–1f | Random hit clip playback speed range (medium / heavy). |
| Slice 2b: randomness (per hit) | `clipSpeedRandomMax` | float | 1.15f | 1f–1.6f |  |
| Slice 2b: randomness (per hit) | `clipWeightRandomMin` | float | 0.85f | 0.3f–1f | Random hit clip layer weight range (multiplied into the tier weight). |
| Slice 2b: randomness (per hit) | `clipWeightRandomMax` | float | 1f | 0.3f–1f |  |
| Slice 2b: randomness (per hit) | `clipStartOffsetRandomMax` | float | 0.1f | 0f–0.3f | Max random start offset (normalized) into a hit clip, for variety. |
| Slice 2b: randomness (per hit) | `avoidRepeatVariation` | bool | true |  | Never play the same clip / variation twice in a row on the same enemy, and keep consecutive lean directions apart. |
| Slice 2b: randomness (per hit) | `heavyStumbleStepChance` | float | 0.3f | 0f–1f | Heavy hits: chance (0-1) of an extra stumble step after the push, and its length (meters). |
| Slice 2b: randomness (per hit) | `heavyStumbleStepMeters` | float | 0.15f | 0f–0.5f |  |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `mediumUsesClip` | bool | true |  | Medium = chain hit 2: PROTOFACTOR GetHitLight* on the additive torso layer (arms / hands masked). |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `mediumClipWeight` | float | 0.75f | 0f–1f |  |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `mediumClipSpeed` | float | 1.15f | 0.5f–2f |  |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `mediumLeanScale` | float | 1.25f | 0f–2f | Lean scale on top of the medium clip (x light lean). |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `mediumAltHeavyClipChance` | float | 0.35f | 0f–1f | Medium variation: chance of using the GetHitHeavy clip at reduced weight instead of GetHitLight. |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `mediumAltHeavyClipWeight` | float | 0.55f | 0f–1f |  |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `heavyUsesClip` | bool | true |  | Heavy = charged / strong hit, chain stumble, poise break: GetHitHeavy* on the body layer (hips, spine, head, legs; arms masked). |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `heavyClipWeight` | float | 1f | 0f–1f |  |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `heavyClipSpeed` | float | 1.2f | 0.5f–2f |  |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `heavyLeanDegrees` | float | 24f | 0f–45f | Peak procedural lean (degrees) on top of the heavy clip. |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `heavyLeanSettleSeconds` | float | 0.8f | 0.2f–2f |  |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `reactionBlendInSeconds` | float | 0.06f | 0.02f–0.3f |  |
| Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept) | `reactionBlendOutSeconds` | float | 0.22f | 0.05f–0.6f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownEnabled` | bool | true |  | Heavy hits can knock the enemy down: PROTOFACTOR Death* fall (cut at the land point) -> Dead* hold -> GetBackUp*, matched by strike direction. No ragdoll (ragdoll is death only). Weapon stays in hand. |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownChancePercent` | float | 15f | 0f–100f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownLowPoiseBonusPercent` | float | 15f | 0f–100f | Bonus when the enemy's poise is below 30%. |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownCritBonusPercent` | float | 10f | 0f–100f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownOnPoiseBreakPercent` | float | 25f | 0f–100f | Replaces the base chance when the hit breaks poise. |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownMaxChancePercent` | float | 40f | 0f–100f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownPushMeters` | float | 0.5f | 0f–1.5f | Push along the strike direction during the fall (meters). Clip root motion is ignored. |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownPushSeconds` | float | 0.4f | 0.1f–1f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownFallSpeed` | float | 1.1f | 0.5f–2f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownFallExitFront` | float | 0.75f | 0.1f–1f | Normalized time in each Death* fall clip where the body lands (cut there, cross-fade to the lying hold). Named by FALL direction: Front = falls forward (hit from behind), Back = falls backward (hit in front), Left / Right = falls to that side. Defaults measured from the clips' body height (Oct 19). |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownFallExitBack` | float | 0.63f | 0.1f–1f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownFallExitLeft` | float | 0.45f | 0.1f–1f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownFallExitRight` | float | 0.52f | 0.1f–1f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownDownSeconds` | float | 1f | 0.2f–4f | Seconds lying down before the get-up (hits while down do not extend it). |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownDownRandomSeconds` | float | 0.25f | 0f–1f | Random +/- seconds on the lying time. |
| Slice 2b: knockdown (animation only, no ragdoll) | `getUpPlaybackSpeed` | float | 1.25f | 0.5f–2.5f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `getUpExitNormalized` | float | 0.92f | 0.6f–1f | Normalized time in the GetBackUp clip where control returns (layer blends out, aim fades back in). |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownAimFadeInSeconds` | float | 0.25f | 0.05f–1f | Aim / chest pitch fade back in after the get-up (the knockdown layer blends out over this time). |
| Slice 2b: knockdown (animation only, no ragdoll) | `getUpInvulnerableSeconds` | float | 0.4f | 0f–1.5f | Player melee cannot damage the enemy during the last part of the get-up (seconds). |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownImmunitySeconds` | float | 8f | 0f–30f | Seconds after a get-up before the same enemy can be knocked down again. |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownReleasesAttackSlot` | bool | true |  | A downed enemy gives up its melee token / attack slot (the next attacker still waits its staggered start). |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownMaxSlopeDegrees` | float | 20f | 5f–45f | Skip the knockdown (heavy reaction instead) on ground steeper than this or near a ledge. |
| Slice 2b: knockdown (animation only, no ragdoll) | `knockdownLedgeCheckMeters` | float | 1f | 0.2f–3f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `enemyAnimStallFailsafe` | bool | true |  | 2026-10-20o failsafe: if an alive, non-ragdolled enemy's animator is disabled, near speed 0, or frozen on one frame while its root moves (outside hit-stop), for longer than Enemy Anim Stall Seconds, force it back: speed 1, knockdown layer off, ResetState to locomotion. |
| Slice 2b: knockdown (animation only, no ragdoll) | `enemyAnimStallSeconds` | float | 0.6f | 0.1f–5f |  |
| Slice 2b: knockdown (animation only, no ragdoll) | `enemyAnimStallMinSpeed` | float | 0.3f | 0f–1f | Animator speed below this (outside hit-stop) counts as stalled. |
| Slice 2b: knockdown (animation only, no ragdoll) | `enemyAnimStallLog` | bool | true |  | Log [AnimStall] when the failsafe fires. |
| Downed Combo (2026-10-20q) | `downedComboEnabled` | bool | true |  | When the player's melee target (soft-lock target, else nearest in front within reach) is knocked down, light attacks switch to the ground-strike combo; reverts when it gets up. |
| Downed Combo (2026-10-20q) | `downedComboReach` | float | 2.6f | 0.5f–6f | Max planar distance to a downed target (m). |
| Downed Combo (2026-10-20q) | `downedComboConeDegrees` | float | 75f | 10f–180f | Half-angle of the front cone used to find a downed target (deg). |
| Downed Combo (2026-10-20q) | `downedComboDamageMultiplier` | float | 1.25f | 0f–5f | Damage multiplier per hit on a downed enemy (applies to the low strike, and tops up normal hitbox hits). |
| Downed Combo (2026-10-20q) | `downedComboBaseDamage` | float | 20f | 0f–500f | Base damage of the low strike when the weapon damage is unknown. |
| Downed Combo (2026-10-20q) | `downedComboStates` | string[] | new string[0] |  | Default Downed Attack 1..N: FullBody layer state paths (e.g. Attacks.DownedAttacks.Downed_1). Empty = fallback (normal light combo + downward pitch + low strike). |
| Downed Combo (2026-10-20q) | `downedComboWeaponSets` | DownedComboWeaponSet[] | new DownedComboWeaponSet[0] |  | Per-weapon overrides (matched by the right-hand weapon GameObject name containing the key). |
| Downed Combo (2026-10-20q) | `downedComboBlendSeconds` | float | 0.08f | 0f–0.5f | Crossfade into a downed attack state (s). |
| Downed Combo (2026-10-20q) | `downedComboFallbackPitchDegrees` | float | 28f | 0f–80f | Fallback: spine/chest pitch down while swinging at a downed target (deg). |
| Downed Combo (2026-10-20q) | `downedComboPitchBlendSeconds` | float | 0.1f | 0.02f–0.5f | Pitch blend in/out time (s). |
| Downed Combo (2026-10-20q) | `downedComboLowStrike` | bool | true |  | Low strike: guaranteed low hit on the downed target inside this normalized window of the swing (covers bodies under the weapon hitbox). Visual hitboxes untouched. |
| Downed Combo (2026-10-20q) | `downedComboStrikeWindow` | Vector2 | new Vector2(0.3f, 0.55f) |  |  |
| Downed Combo (2026-10-20q) | `downedComboFaceTarget` | bool | true |  | Rotate the player toward the downed target at each swing start. |
| Downed Combo (2026-10-20q) | `downedComboFaceMaxDegrees` | float | 60f | 0f–180f | Max yaw snap toward the downed target per swing (deg). |
| Downed Combo (2026-10-20q) | `downedComboLog` | bool | false |  |  |
| Aim Rating (2026-10-20r) | `aimRatingEnabled` | bool | true |  | Scoped reticle accuracy popup: on a shot, red reticle offset from the center dot as a fraction of its max sway travel -> Perfect / Good / OK. |
| Aim Rating (2026-10-20r) | `aimRatingRequireHit` | bool | true |  | Only rate shots that hit a target (within Hit Window). Off = rate every shot. |
| Aim Rating (2026-10-20r) | `aimRatingHitWindowSeconds` | float | 0.6f | 0.05f–3f | Seconds after the shot a hit still counts (projectile travel). |
| Aim Rating (2026-10-20r) | `aimRatingScopedOnly` | bool | true |  | Only while the scope reticle is shown (the reticle that sways). |
| Aim Rating (2026-10-20r) | `aimRatingPerfect` | float | 0.10f | 0f–1f |  |
| Aim Rating (2026-10-20r) | `aimRatingGood` | float | 0.25f | 0f–1f |  |
| Aim Rating (2026-10-20r) | `aimRatingOk` | float | 0.50f | 0f–1f |  |
| Aim Rating (2026-10-20r) | `aimRatingDuration` | float | 0.9f | 0.2f–4f | Popup lifetime (s). |
| Aim Rating (2026-10-20r) | `aimRatingScale` | float | 1f | 0.2f–4f | Popup size multiplier. |
| Aim Rating (2026-10-20r) | `aimRatingPopScale` | float | 1.45f | 1f–3f | Pop overshoot scale at the start of the popup. |
| Aim Rating (2026-10-20r) | `aimRatingOffset` | Vector2 | new Vector2(0f, -72f) |  | Offset from the reticle (px, +x right, +y down). |
| Aim Rating (2026-10-20r) | `aimRatingFollowReticle` | bool | true |  | Follow the swaying reticle (else anchored at the center dot). |
| Aim Rating (2026-10-20r) | `aimRatingPerfectColor` | Color | new Color(1f, 0.82f, 0.2f, 1f) |  |  |
| Aim Rating (2026-10-20r) | `aimRatingGoodColor` | Color | new Color(0.45f, 1f, 0.45f, 1f) |  |  |
| Aim Rating (2026-10-20r) | `aimRatingOkColor` | Color | new Color(0.85f, 0.9f, 1f, 1f) |  |  |
| Aim Rating (2026-10-20r) | `aimRatingFontSize` | float | 30f | 8f–96f |  |
| Aim Rating (2026-10-20r) | `weaponNameContains` | string | "" |  | Matched against the right-hand weapon GameObject name (contains, case-insensitive). |
| Aim Rating (2026-10-20r) | `states` | string[] | new string[0] |  | FullBody state paths: Downed Attack 1..N for this weapon. |
| Aim Rating (2026-10-20r) | `damageMultiplier` | float | 0f | 0f–5f | 0 = use the default multiplier. |
| Aim Rating (2026-10-20r) | `reach` | float | 0f | 0f–6f | 0 = use the default reach. |
| Aim Rating (2026-10-20r) | `retireCritRagdollKnockdown` | bool | true |  | On = player melee crits never take the old crit-ragdoll knockdown (EnemyInvectorRagdollBridge). Crit damage / numbers / VFX unchanged. |
| Aim Rating (2026-10-20r) | `logHitReactionsInPlay` | bool | false |  |  |
| Future tuning hooks | `hitstopLightFrames` | int | 2 | 0–12 |  |
| Future tuning hooks | `hitstopHeavyFrames` | int | 6 | 0–12 |  |
| Future tuning hooks | `parryWindowSeconds` | float | 0.2f | 0.05f–0.5f |  |
| Parry clash VFX | `parryClashVfxPrefab` | GameObject |  |  | One-shot sparks at the blade contact on a successful parry. Assign SparksLong.prefab under Prefabs/Combat/VFX. |
| Parry clash VFX | `parryClashVfxScale` | float | 1.15f | 0.5f–2.5f |  |
| Parry clash VFX | `parryClashVfxLifetimeSeconds` | float | 0.45f | 0.1f–1.5f |  |
| Combat camera snap | `enableCombatCameraSnap` | bool | false |  | When on, melee block/attack auto-face eases Invector tpCamera yaw behind the body and ECM2 combat focus uses combatCameraSnapSeconds for yaw damp. When off, camera yaw is not snapped on auto-face and combat focus uses the CombatFocusController yawSmoothLambda only. |
| Combat camera snap | `combatCameraSnapSeconds` | float | 0.35f | 0.1f–0.5f | Seconds to ease the camera behind the player when melee attack/block auto-face rotates the body toward a threat (Invector tpCamera yaw + ECM2 combat focus). Ignored when enableCombatCameraSnap is off. |
| Player combat buffer (enemy standoff) | `enableGlobalCombatSeparation` | bool | true |  | When on, enemies treat playerCombatBufferRadius as a global minimum planar separation vs Kade (Mathf.Max with each enemy prefab minCombatSeparation). Off or radius 0 = prefab-only spacing. |
| Player combat buffer (enemy standoff) | `playerCombatBufferRadius` | float | 1.35f | 0f–4f | Minimum keep-away radius (meters) between Kade and engaging enemies. Does not change Player_v7 capsule — AI ring/standoff and step clamps only. |
| Camera shake | `chargedHitShakeAmplitude` | float | 0.55f | 0f–2f | Trauma added on a charged strong hit that actually damages an enemy. 0 = off. Useful range 0–1 (CameraShake trauma caps at 1). |
| Camera shake | `chargedHitShakeDurationSeconds` | float | 0.22f | 0f–0.4f | Hold trauma this long after a charged hit, then decay. 0 = one-shot punch only. |
| Camera shake | `parryShakeAmplitude` | float | 0.32f | 0f–2f | Trauma on a successful parry. 0 = off. Medium vs block; weaker than a charged hit. |
| Camera shake | `parryShakeDurationSeconds` | float | 0.14f | 0f–0.4f |  |
| Camera shake | `blockShakeAmplitude` | float | 0.18f | 0f–2f | Trauma on a successful guard connect that is not a parry. 0 = off. Smallest of the three. |
| Camera shake | `blockShakeDurationSeconds` | float | 0.08f | 0f–0.4f |  |
| Grenades | `enableGrenades` | bool | false |  | Master switch for throwable grenades. On: G (LB) arms; hold LMB (LT) to cook; release LMB (LT) to throw. Off = stacks stay in inventory but cannot be thrown. Default off. |
| Grenades | `grenadeCookSecondsBase` | float | 5f | 1f–15f | Uncooked / minimum cook fuse before Grenade Cook Mastery skill ranks. |
| Grenades | `grenadeCookSecondsMax` | float | 10f | 1f–20f | Hard cap on cook fuse (base + skill ranks, +1s per rank up to this). |
| Grenades | `grenadeLandingRingUsesSplashSkills` | bool | true |  | Landing projector ring diameter scales with splash radius skills. |
| Grenades | `grenadeThrowArcLineColor` | Color | new Color(1f, 0f, 0f, 0.88f) |  | Tint for the throw arc LineRenderer while aiming. |
| Grenades | `grenadeLandingRingColor` | Color | new Color(1f, 0f, 0f, 0.42f) |  | Tint for the landing target projector ring. |
| Grenades | `grenadeLandingRingSizeMultiplier` | float | 10f | 1f–25f | Multiplies blast radius for the throw landing projector ring (ring diameter vs splash). |

## Melee Animations

**What it does:** Per-weapon Mixamo / PROTOFACTOR clip families for Invector attack states.

**Where:**
- Studio: `Genesis Studio / Combat / Melee Animations` (panel mode `AssetFolder`)
- Asset folder: `Assets/_Project/Resources/Combat` (`t:DM_MeleeAnimationSet`)
- Script: `Assets/_Project/Scripts/Combat/DM_MeleeAnimationSet.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `lightA` | AnimationClip |  |  | WeakAttacks / SwordAttack A — regular chained light. |
|  | `lightB` | AnimationClip |  |  | WeakAttacks / SwordAttack B — regular chained light. |
|  | `lightC` | AnimationClip |  |  | WeakAttacks / SwordAttack C — regular chained light. |
|  | `strongA` | AnimationClip |  |  | StrongAttacks / SwordAttack A — other heavy. Not the charge release. |
|  | `strongB` | AnimationClip |  |  | StrongAttacks / SwordAttack B — charged release (AttackC). |
|  | `strongC` | AnimationClip |  |  | StrongAttacks / SwordAttack C — other heavy. |
|  | `chargeHold` | AnimationClip |  |  | Attacks.StrongAttacks.SwordCharge hold pose. Must be a looping wind-up (1HandSwordChargeUp), not a swing. |
| Overlays (parry / interact hold) | `parry01` | AnimationClip |  |  | 1H-RH@Parry01 — tap parry presentation (optional). |
| Overlays (parry / interact hold) | `parry01Hit` | AnimationClip |  |  | 1H-RH@Parry01_Hit — successful parry hit reaction. |
| Overlays (parry / interact hold) | `interactHoldCombo` | AnimationClip |  |  | Hold E + light attack — One Hand Sword Combo. |
| One-hand sword (AttackID 1 / Weak SwordAttack A→B→C combo chain) | `oneHandSword` | DMMeleeClipSlots | new DMMeleeClipSlots() |  |  |
| Other families (catalog for future weapons) | `swordAndShield` | DMMeleeClipSlots | new DMMeleeClipSlots() |  |  |
| Other families (catalog for future weapons) | `knife` | DMMeleeClipSlots | new DMMeleeClipSlots() |  |  |
| Other families (catalog for future weapons) | `axe` | DMMeleeClipSlots | new DMMeleeClipSlots() |  |  |
| Other families (catalog for future weapons) | `twoHand` | DMMeleeClipSlots | new DMMeleeClipSlots() |  |  |
| Other families (catalog for future weapons) | `dualWield` | DMMeleeClipSlots | new DMMeleeClipSlots() |  |  |
| Other families (catalog for future weapons) | `torch` | DMMeleeClipSlots | new DMMeleeClipSlots() |  |  |

## Combat Studio

**What it does:** Combat Studio — Core & Melee use Player / Enemy headings; Ranged links, Play (v1.6.5), §31 Roadmap (Oct 2026 disk sync).

**Where:**
- Studio: `Genesis Studio / Combat / Combat Studio` (panel mode `ExternalTool`)
- Menu: `Tools/Dark Matter Genesis/Combat/Combat Studio`

## Ranged

**What it does:** Genesis ranged authoring jump list — same links as Combat Studio → Ranged (Ammo FX, scope, hit catalog, surface damage).

**Where:**
- Studio: `Genesis Studio / Combat / Ranged` (panel mode `CombatRangedLinks`)

## Throwables

**What it does:** DM_CombatCoreProfile — grenade enable, splash radius/damage, VFX/force/shake, cook fuse, landing ring, Hot Cross arm hold. Live in Play.

**Where:**
- Studio: `Genesis Studio / Combat / Throwables` (panel mode `AssetFolder`)
- Asset folder: `Assets/_Project/Resources/Combat` (`t:DM_CombatCoreProfile`)
- Script: `Assets/_Project/Scripts/Combat/DM_CombatCoreProfile.cs`

**Key settings:** same script as [Combat Core](#combat-core).

## Turrets

**What it does:** DM_TurretProfile — defaults for placed turrets. Per-prefab faction, ammo mode, vendor VFX, barrel, and ranges live on DMTurretRuntime / Instance Authority. Playable menu is Auto or Enter. Live in Play.

**Where:**
- Studio: `Genesis Studio / Combat / Turrets` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Combat/DM_TurretProfile.asset`
- Script: `Assets/_Project/Scripts/Combat/Turrets/DMTurretProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Placement defaults | `defaultPlacedFaction` | DMTurretFaction | DMTurretFaction.Player |  | Faction applied to player-placed turrets (scene FORGE3D turrets bootstrap as Enemy). |
| Placement defaults | `defaultControlMode` | DMTurretControlMode | DMTurretControlMode.Auto |  | Control mode applied to player-placed turrets. |
| Ranges (meters) | `scanRange` | float | 45f |  | Acquire / keep a target. No target or beyond this → Scan. |
| Ranges (meters) | `trackRange` | float | 32f |  | Aim at a target without firing. Typically between fire and scan. |
| Ranges (meters) | `fireRange` | float | 22f |  | Auto-fire when a legal target is this close and the barrel can see it. |
| Auto idle | `autoRestAfterNoTargetSeconds` | float | 15f |  | Auto mode: seconds with no engageable target (within track range + LOS) before long rest. Barrel stays still; silent FindTarget continues but slows (~1.2s) to reduce CPU. |
| Auto idle | `targetLosGraceSeconds` | float | 0.65f |  | Keep target lock this long after the last confirmed LOS ray (reduces flicker). |
| Auto idle | `targetLosFailuresBeforeDrop` | int | 4 | 1–12 | Drop target after this many consecutive LOS failures (still capped by grace time). |
| Scan motion (non-auto only) | `scanHeadingSpeed` | float | 18f |  | Yaw sweep speed while scanning (degrees / second). Used when control mode is not Auto. |
| Scan motion (non-auto only) | `scanRetargetSeconds` | float | 2.4f |  | Seconds between picking a new scan heading / elevation (non-auto visual sweep). |
| Scan motion (non-auto only) | `scanElevationDegrees` | Vector2 | new Vector2(-8f, 12f) |  | Random barrel elevation while scanning, in degrees (mapped onto F3D elevation limits). |
| Tracking (FORGE3D F3DTurret) | `headingTrackingSpeed` | float | 40f |  |  |
| Tracking (FORGE3D F3DTurret) | `elevationTrackingSpeed` | float | 30f |  |  |
| Tracking (FORGE3D F3DTurret) | `headingLimit` | Vector2 | new Vector2(-180f, 180f) |  |  |
| Tracking (FORGE3D F3DTurret) | `elevationLimit` | Vector2 | new Vector2(-15f, 45f) |  |  |
| Ammo mode | `defaultAmmoMode` | DMTurretAmmoMode | DMTurretAmmoMode.Forge3DVfx |  | Default for placed turrets and instances without overrideAmmoMode. |
| Ammo mode | `gameWeaponItem` | ItemData |  |  | Weapon ItemData for GameCombatAmmo mode (spread, fire rate, projectile prefab). |
| Ammo mode | `gameAmmoItem` | ItemData |  |  | Optional ammo row; empty uses weapon default or catalog lookup from ammoType. |
| Ammo mode | `gameFireInterval` | float | 0.15f |  | Seconds between game-ammo shots when weapon fire rate is unset. |
| Ammo mode | `gameSpreadDegrees` | float | 1.5f | 0f–12f | Cone spread (degrees) for game-ammo shots. |
| Damage | `ammoType` | AmmoType | AmmoType.Gunpowder |  |  |
| Damage | `damage` | float | 18f |  |  |
| Damage | `splashRadius` | float | 0f |  | 0 = no splash. Explosive ammo should be > 0. |
| Damage | `beamDamageInterval` | float | 0.2f |  | Beam / lightning damage interval so continuous FX do not melt HP every frame. |
| Durability | `maxHealth` | float | 100f |  |  |
| Enemy response | `enemyDestroyChanceMin` | float | 0.10f | 0f–1f | When a player turret threatens an enemy, chance to attack the turret instead of fleeing. |
| Enemy response | `enemyDestroyChanceMax` | float | 0.40f | 0f–1f |  |
| Enemy response | `enemyFleeMargin` | float | 6f |  | Extra meters past fire range before flee counts as safe. |
| Enemy response | `enemyTurretDecisionSeconds` | float | 8f |  |  |
| Player seat camera (prefab slot on *SOCKET_SWIVEL) | `playerCameraLocalPosition` | Vector3 | new Vector3(0f, 2.22f, -5.38f) |  | Local position on the swivel — follows turret yaw/pitch while seated. |
| Player seat camera (prefab slot on *SOCKET_SWIVEL) | `playerCameraLocalEuler` | Vector3 | Vector3.zero |  |  |
| Player seat camera (prefab slot on *SOCKET_SWIVEL) | `playerCameraFov` | float | 60f |  |  |
| Player seat camera (prefab slot on *SOCKET_SWIVEL) | `playerAimMouseSensitivity` | float | 0.35f |  | Degrees per mouse unit while seated. Idle mouse holds swivel and barrel. |
| Player seat camera (prefab slot on *SOCKET_SWIVEL) | `playerReticleScreenBias` | float | 0.06f | 0f–0.2f | Normalized screen Y bias below center for the turret reticle and aim ray (0.06 ≈ lower crosshair). |
| Alarm light (swivel tracking indicator) | `useCustomAlarmColors` | bool |  |  | When off, uses palette defaults (Gold scan, Map POI red track, PositiveGreen fire). |
| Alarm light (swivel tracking indicator) | `alarmScanColor` | Color | DarkMatterGenesisUiPalette.Gold |  |  |
| Alarm light (swivel tracking indicator) | `alarmTrackColor` | Color | DarkMatterGenesisUiPalette.MapPoiRed |  |  |
| Alarm light (swivel tracking indicator) | `alarmFireColor` | Color | DarkMatterGenesisUiPalette.PositiveGreen |  |  |
| Alarm light (swivel tracking indicator) | `alarmEmissionIntensity` | float | 5f | ≥0f | HDR emission multiplier for the Alarm sphere MaterialPropertyBlock. |
| Debug | `debugDraw` | bool | false |  |  |
| Debug | `FireRange` | float | > Positive(fireRange, 22f) |  |  |
| Debug | `TrackRange` | float | > Mathf.Max(FireRange, Positive(trackRange, 32f)) |  |  |
| Debug | `ScanRange` | float | > Mathf.Max(TrackRange, Positive(scanRange, 45f)) |  |  |
| Debug | `MaxHealth` | float | > Positive(maxHealth, 100f) |  |  |
| Debug | `Damage` | float | > Mathf.Max(0f, damage) |  |  |
| Debug | `SplashRadius` | float | > Mathf.Max(0f, splashRadius) |  |  |
| Debug | `BeamDamageInterval` | float | > Positive(beamDamageInterval, 0.2f) |  |  |
| Debug | `AutoRestAfterNoTargetSeconds` | float | > Positive(autoRestAfterNoTargetSeconds, 15f) |  |  |
| Debug | `TargetLosGraceSeconds` | float | > Positive(targetLosGraceSeconds, 0.65f) |  |  |
| Debug | `TargetLosFailuresBeforeDrop` | int | > Mathf.Clamp(targetLosFailuresBeforeDrop, 1, 12) |  |  |
| Debug | `ScanHeadingSpeed` | float | > Positive(scanHeadingSpeed, 18f) |  |  |
| Debug | `ScanRetargetSeconds` | float | > Positive(scanRetargetSeconds, 2.4f) |  |  |
| Debug | `HeadingTrackingSpeed` | float | > Positive(headingTrackingSpeed, 40f) |  |  |
| Debug | `ElevationTrackingSpeed` | float | > Positive(elevationTrackingSpeed, 30f) |  |  |
| Debug | `EnemyFleeMargin` | float | > Mathf.Max(0f, enemyFleeMargin) |  |  |
| Debug | `EnemyTurretDecisionSeconds` | float | > Positive(enemyTurretDecisionSeconds, 8f) |  |  |
| Debug | `PlayerCameraFov` | float | > Positive(playerCameraFov, 60f) |  |  |
| Debug | `PlayerCameraLocalPosition` | Vector3 | > playerCameraLocalPosition |  |  |
| Debug | `PlayerCameraLocalEuler` | Vector3 | > playerCameraLocalEuler |  |  |
| Debug | `PlayerAimMouseSensitivity` | float | > Positive(playerAimMouseSensitivity, 0.35f) |  |  |
| Debug | `PlayerReticleScreenBias` | float | > Mathf.Clamp(playerReticleScreenBias, 0f, 0.2f) |  |  |
| Debug | `DefaultAmmoMode` | DMTurretAmmoMode | > defaultAmmoMode |  |  |
| Debug | `GameFireInterval` | float | > Positive(gameFireInterval, 0.15f) |  |  |
| Debug | `GameSpreadDegrees` | float | > Mathf.Clamp(gameSpreadDegrees, 0f, 12f) |  |  |
| Debug | `AlarmScanColor` | Color | > useCustomAlarmColors ? alarmScanColor : DarkMatterGenesisUiPalette.Gold |  |  |
| Debug | `AlarmTrackColor` | Color | > useCustomAlarmColors ? alarmTrackColor : DarkMatterGenesisUiPalette.MapPoiRed |  |  |
| Debug | `AlarmFireColor` | Color | > useCustomAlarmColors ? alarmFireColor : DarkMatterGenesisUiPalette.PositiveGreen |  |  |
| Debug | `AlarmEmissionIntensity` | float | > alarmEmissionIntensity > 0.001f ? alarmEmissionIntensity : 5f |  |  |

## Ammo FX

**What it does:** Per-type projectile / impact profiles (ItemData + combat FX).

**Where:**
- Studio: `Genesis Studio / Combat / Ammo FX` (panel mode `AssetFolder`)
- Asset folder: `Assets/_Project/Data/Items/Ammo` (`t:DMAmmoFxProfile`)
- Script: `Assets/_Project/Scripts/Combat/DMAmmoFxProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Delivery (Combat Plan §16 / phase D) | `delivery` | DMAmmoDeliveryMode | DMAmmoDeliveryMode.Auto |  | Auto = infer from isHitscanBeam and projectile prefab. All nine ammo types resolve through DMCombatRangedResolver. |
| Muzzle flash / vendor FX (2026-10-21c) | `useF3DDespawn` | bool | true |  | Use the muzzle prefab's FORGE3D F3DDespawn delay as its lifetime when the component is present (highest priority). |
| Muzzle flash / vendor FX (2026-10-21c) | `muzzleLifetime` | float | 0f | ≥0f | Muzzle flash lifetime (s) when no F3DDespawn delay applies. 0 = global default (2 s, or until particles finish). Vendor ammo: 0.05. |
| Muzzle flash / vendor FX (2026-10-21c) | `beamUsesAmmoPrefab` | bool | false |  | Hitscan beam: always draw this ammo's Beam Vfx Prefab instead of pulsing the weapon's own laser stack (e.g. Lightning Gun). |
| Lightning beam (2026-10-21c; used when Beam Uses Ammo Prefab is on) | `beamDurationSeconds` | float | 1.5f | 0f–6f | Hold (s) after the bolt head reaches the contact point, before the tail retracts. 0 = retract immediately. |
| Lightning beam (2026-10-21c; used when Beam Uses Ammo Prefab is on) | `boltSpeed` | float | 120f | 5f–1000f | 2026-10-21d: bolt head travel speed (m/s) from the barrel to the contact point (chain links too). |
| Lightning beam (2026-10-21c; used when Beam Uses Ammo Prefab is on) | `boltMaxLength` | float | 12f | 0.5f–200f | 2026-10-21d: max visible bolt length (m) while travelling; the tail follows the head. |
| Lightning beam (2026-10-21c; used when Beam Uses Ammo Prefab is on) | `boltTailRetractTime` | float | 0.12f | 0.01f–2f | 2026-10-21d: time (s) for the tail to retract from the shooter end into the target. |
| Lightning beam (2026-10-21c; used when Beam Uses Ammo Prefab is on) | `boltWidth` | float | 0.12f | 0.01f–2f | 2026-10-21d: bolt line width (m). |
| Lightning beam (2026-10-21c; used when Beam Uses Ammo Prefab is on) | `beamDamagePerTick` | bool | true |  | On = damage every tick while the beam is active. Off = damage once on fire. |
| Lightning beam (2026-10-21c; used when Beam Uses Ammo Prefab is on) | `beamDamageTicksPerSecond` | float | 5f | 1f–30f | Damage ticks per second while active (per-tick mode). |
| Lightning beam (2026-10-21c; used when Beam Uses Ammo Prefab is on) | `beamTickDamageScale` | float | 0.2f | 0f–2f | Each tick deals shot damage x this (per-tick mode). The fire frame always deals full shot damage. |
| Chain lightning (2026-10-21c) | `chainEnabled` | bool | true |  | Chain lightning on/off for this ammo. |
| Chain lightning (2026-10-21c) | `chainSkillUnlocked` | bool | true |  | Chain Lightning skill unlocked (temporary flag until the skill tree drives it). |
| Chain lightning (2026-10-21c) | `chainMaxTargets` | int | 8 | 0–50 | Extra enemies the bolt jumps to after the first hit (1-5 normal; up to 20 for testing). |
| Chain lightning (2026-10-21c) | `chainRange` | float | 8f | 0.5f–100f | Max jump distance (m) from the current enemy to the next. |
| Chain lightning (2026-10-21c) | `chainDamageFalloff` | float | 0.85f | 0f–2f | Damage multiplier per jump (1 = no falloff, 0.85 = -15% per jump). |
| Chain lightning (2026-10-21c) | `chainRequiresLineOfSight` | bool | true |  | Jumps need a clear line between enemies. |
| Chain lightning (2026-10-21c) | `chainDebugLog` | bool | false |  | 2026-10-21e: log each chain link (spawn, endpoints, damage, why a jump was skipped) to the Console. |
| Chain lightning (2026-10-21c) | `chainJumpDelay` | float | 0.05f | 0f–2f | 2026-10-24i: delay (s) before each jump leaves the current enemy. |
| Chain lightning (2026-10-21c) | `chainWidthScale` | float | 1f | 0.05f–10f | 2026-10-24i: chain arc width x the main bolt width. |
| Chain lightning (2026-10-21c) | `chainColor` | Color | Color.white |  | 2026-10-24i: chain arc tint (line vertex colour; white = prefab colours). |
| Chain lightning (2026-10-21c) | `chainSpeedScale` | float | 1f | 0.05f–10f | 2026-10-24i: chain arc travel speed x boltSpeed. |
| Chain lightning (2026-10-21c) | `chainUseCenterMass` | bool | true |  | 2026-10-24l: chain arcs start/end at each target's center mass (humanoid chest bone, else body collider / renderer bounds center) and track it while held. Off = legacy (humanoid chest, else pivot + 1.2 m). |
| Chain lightning (2026-10-21c) | `chainMode` | ChainMode | ChainMode.FanOutFromFirst |  | 2026-10-24l: FanOutFromFirst = arcs from the first target to EVERY live target within chainRange (nearest first, capped by chainMaxTargets; target k gets falloff^k). SequentialNearest = hop to the nearest, then from there (legacy). |
| Chain lightning (2026-10-21c) | `chainCenterMassHeightOffset` | float | 0f | -1f–1f | 2026-10-24l: vertical offset (m) added to the chain center-mass point. |
| Vendor Impact Audio (24n) | `vendorImpactAudio` | VendorImpactAudioMode | VendorImpactAudioMode.Auto |  | Auto = detect from the tracer/projectile prefab's FORGE3D projectile type. Off = no vendor explosion audio. |
| Vendor Impact Audio (24n) | `impactAudioMinInterval` | float | 0.05f | 0f–0.5f | Anti-spam: min seconds between impact sounds for this ammo (0 = every hit). |
| Vendor Impact Audio (24n) | `impactAudioMaxVoices` | int | 6 | 1–32 | Anti-spam: max vendor impact sounds playing at once (all ammo). |
| Vendor Impact Audio (24n) | `impactAudioVolume` | float | 1f | 0f–2f |  |
| Vendor Impact Audio (24n) | `streamSegments` | int | 3 | 1–4 | 2026-10-21i: Stream - fire the bolt as a short burst of segments along the same snapshot path (1 = single bolt). Only the lead segment hits. |
| Vendor Impact Audio (24n) | `streamSegmentGap` | float | 0.045f | 0.01f–0.2f | 2026-10-21i: seconds between stream segments. |
| Vendor Impact Audio (24n) | `showSplashBubble` | bool | true |  | 2026-10-21e: show the magenta splash-radius bubble (DMAoeTriggerBubble) for this ammo's splash. Off for Lightning Gun. |
| Hit VFX | `spawnLaserBurn` | bool |  |  | Stamp the laser burn mark (pulse / continuous laser). |
| Enemy hit marks (Combat Plan §16) | `enemyBlood` | DMEnemyFxRule | DMEnemyFxRule.Auto |  | Red blood on Humanoid enemies. Auto = §16 (Laser never bleeds). Coolant / sparks on Android and Robot always play. |
| Enemy hit marks (Combat Plan §16) | `enemyBurnMark` | DMEnemyFxRule | DMEnemyFxRule.Auto |  | Burn mark decal on enemies. Auto = §16 (Ion never burns). |
| Hit Marks (this ammo) | `useHitMarks` | bool |  |  | Author holes + bursts on this ammo. Rows below override the shared catalog. |
| Hit Marks (this ammo) | `defaultDecals` | GameObject[] |  |  | Default hole for this ammo when the tag has no row. |
| Hit Marks (this ammo) | `defaultHitEffects` | GameObject[] |  |  | Default burst for this ammo when the tag has no row. |
| Hit Marks (this ammo) | `surfaces` | DMHitMarkSurface[] |  |  | Per-tag marks for this ammo only. Empty tag list still uses the defaults above. |
| Hit Marks (this ammo) | `fallBackToCatalog` | bool | true |  | If this ammo has no row/default, use the shared catalog. |
| Hit Marks (this ammo) | `catalog` | DMAmmoFxCatalog |  |  |  |
| Surface Damage (carve) | `surfaceDamage` | DMAmmoSurfaceDamage | new DMAmmoSurfaceDamage() |  | How this ammo carves rocks / carvable surfaces. Auto = built-in preset for the ammo type. Per-tag rows above can override it. |
| Camera shake (trauma) | `fireTrauma` | float |  | 0f–2f | Trauma when the player fires this ammo. 0 = off. Aim kick stays on Recoil Vertical/Horizontal — this is the CameraShake hub only. |
| Camera shake (trauma) | `fireTraumaDurationSeconds` | float |  | 0f–0.4f | Hold fire trauma this long, then decay. 0 = one-shot punch only. |
| Camera shake (trauma) | `impactTrauma` | float |  | 0f–2f | Trauma when the player's shot resolves a hit (enemy or world). 0 = off. |
| Camera shake (trauma) | `impactTraumaDurationSeconds` | float |  | 0f–0.4f |  |
| Camera shake (trauma) | `impactTraumaRadius` | float |  | 0f–80f | When > 0, impact shake uses CameraShake proximity falloff at the hit point (meters). 0 = global trauma at the listener. |
| Camera shake (trauma) | `impactTraumaMinDistance` | float |  | 0f–8f | When > 0, skip impact trauma if the hit is closer than this distance to the active camera (meters). Suppresses muzzle-adjacent jitter. |

## Scope Overlay

**What it does:** UITK scoped ADS art — reticle, ring, outer mil-dot, center mask (DM_ScopeProfile). Live in Play; skills still gate sway/zoom/cadence/accuracy/damage.

**Where:**
- Studio: `Genesis Studio / Combat / Scope Overlay` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Combat/DM_ScopeProfile.asset`
- Script: `Assets/_Project/Scripts/Combat/Scope/DM_ScopeProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Master | `scopeEnabled` | bool | true |  |  |
| Master | `scopeUnlockedByDefault` | bool | true |  | Scope is an upgrade; unlocked for now. When false, ScopeUnlockedHook (code) decides. |
| Range (metres, scoped) | `rifleScopedRange` | float | 50f | 5f–150f |  |
| Range (metres, scoped) | `pistolScopedRange` | float | 28f | 5f–100f |  |
| Zoom / ADS input (Scope mode armed in hold-R menu) | `scopeAdsRightMouseToggle` | bool | true |  | When Scope mode is armed (hold-R menu): ON = RMB/LT click toggles ADS zoom, overlay, and scoped fire rules until the next click. OFF = hold RMB/LT to ADS (legacy). When Scope mode is off, RMB hold aim is unchanged. |
| Zoom / ADS input (Scope mode armed in hold-R menu) | `scopeAdsEnablesZoomOverlay` | bool | true |  | When false, scoped cadence/range/damage still apply while ADS is active, but FOV zoom and the UITK scope overlay stay off. |
| Zoom / ADS input (Scope mode armed in hold-R menu) | `rifleScopedFov` | float | 14f | 3f–60f |  |
| Zoom / ADS input (Scope mode armed in hold-R menu) | `pistolScopedFov` | float | 26f | 3f–60f |  |
| Zoom / ADS input (Scope mode armed in hold-R menu) | `scopeInSeconds` | float | 0.3f | 0.02f–2f | Seconds to reach full zoom (Quick Scope skill shortens it). |
| Zoom / ADS input (Scope mode armed in hold-R menu) | `scopeOutSeconds` | float | 0.15f | 0.02f–2f |  |
| Fire interval (seconds per scoped single shot) | `rifleScopedFireInterval` | float | 2.5f | 0.2f–5f | Minimum seconds between scoped shots (burst is disabled while ADS). Skills and min interval apply after ammo ±10%. |
| Fire interval (seconds per scoped single shot) | `pistolScopedFireInterval` | float | 2f | 0.2f–5f |  |
| Fire interval (seconds per scoped single shot) | `minScopedFireInterval` | float | 1f | 0.1f–5f | Scoped Cadence skill never goes below this. |
| Fire interval (seconds per scoped single shot) | `rifleScopedPostShotWaitSeconds` | float |  | 0f–5f | Extra wait after each scoped shot, added to the fire interval above (0 = interval alone). |
| Fire interval (seconds per scoped single shot) | `pistolScopedPostShotWaitSeconds` | float |  | 0f–5f |  |
| Damage (scoped hits on enemies; base damage comes from the ammo profile) | `scopedHeadMultiplier` | float | 2f | 1f–5f |  |
| Damage (scoped hits on enemies; base damage comes from the ammo profile) | `scopedChestMultiplier` | float | 1.25f | 1f–5f |  |
| Accuracy (hooks: skills / future attachments add to these) | `scopedAccuracyBonusPercent` | float | 25f | 0f–100f |  |
| Accuracy (hooks: skills / future attachments add to these) | `reticleSwayPixels` | float | 6f | 0f–40f | Reticle sway in pixels at full zoom; Steady Aim reduces it. |
| Accuracy (hooks: skills / future attachments add to these) | `reticleSwaySpeed` | float | 0.6f | 0.05f–3f |  |
| Ammo allowed while scoped (matched against the loaded ammo item / FX name) | `allowedAmmoKeywords` | string[] | { "Standard", "Plasma" } |  |  |
| Shot audio while scoped | `scopedShotVolume` | float | 1.2f | 0.1f–3f |  |
| Shot audio while scoped | `scopedShotPitch` | float | 0.85f | 0.3f–2f |  |
| Skill scaling (percent per skill rank comes from the skill asset; these clamp the result) | `maxSwayReduction` | float | 0.85f | 0f–0.95f |  |
| Skill scaling (percent per skill rank comes from the skill asset; these clamp the result) | `maxScopeInReduction` | float | 0.7f | 0f–0.9f |  |
| Skill scaling (percent per skill rank comes from the skill asset; these clamp the result) | `maxExtraZoom` | float | 0.5f | 0f–1f |  |
| Overlay (binoculars / legacy procedural) | `scopeCrosshairSprite` | Sprite |  |  | Rifle scope: large mil-dot crosshair sprite (Invector sniperCrossHair). Also used for pistol ADS when sprite overlay is on. |
| Overlay (binoculars / legacy procedural) | `scopeOverlayUseSpriteCrosshair` | bool | true |  | When true, rifle and pistol ADS use the mil-dot sprite for the outer crosshair plus procedural inner ring/reticle. |
| Overlay (binoculars / legacy procedural) | `rifleScopeMaskRadius` | float | 0.34f | 0.15f–0.55f |  |
| Overlay (binoculars / legacy procedural) | `rifleScopeMaskScale` | float | 0.8f | 0.5f–1f | Multiplies rifle circular mask radius (0.8  20% smaller hole). |
| Overlay (binoculars / legacy procedural) | `rifleScopeCrosshairScale` | float | 0.8f | 0.35f–1.5f | Legacy alias  use rifleOuterCrosshairScale. Kept for existing assets. |
| Overlay (binoculars / legacy procedural) | `pistolScopeMaskRadius` | float | 0.4f | 0.15f–0.55f |  |
| Overlay (binoculars / legacy procedural) | `pistolScopeReticleScale` | float | 0.25f | 0.1f–1f | Pistol scope procedural reticle size (0.25 = quarter size). |
| Overlay (binoculars / legacy procedural) | `useNewScopeArt` | bool | true |  | New UITK reticle art for the scope AND the binoculars (replaces the old sprite frame). |
| Overlay (binoculars / legacy procedural) | `retireUguiOptics` | bool | true |  | Retire the old uGUI optics canvas: the UITK overlay always drives optics. |
| Overlay (binoculars / legacy procedural) | `reticleColor` | Color | new Color(0.05f, 0.05f, 0.05f, 0.95f) |  |  |
| Overlay (binoculars / legacy procedural) | `ringColor` | Color | new Color(0f, 0f, 0f, 1f) |  |  |
| Overlay (binoculars / legacy procedural) | `reticleLineWidth` | float | 1.6f | 0.5f–6f |  |
| Overlay (binoculars / legacy procedural) | `reticleCenterGap` | float | 10f | 0f–40f |  |
| Overlay (binoculars / legacy procedural) | `milDotsPerArm` | int | 4 | 0–10 |  |
| Overlay (binoculars / legacy procedural) | `rifleScopeLabel` | string | "SCOPE" |  |  |
| Overlay (binoculars / legacy procedural) | `pistolScopeLabel` | string | "SCOPE" |  |  |
| Overlay (binoculars / legacy procedural) | `scopeHint` | string | "[RMB] Toggle ADS  \|  Standard / Plasma ammo only" |  |  |
| Overlay (binoculars / legacy procedural) | `rangeLabelGapPx` | float | 4f |  | Gap (px) between the top edge of the outer orange circle and the bottom of the range label (e.g. '11 m'). Negative = overlap. |
| Scope overlay  rifle ADS (reticle / ring / outer crosshair) | `rifleReticleColor` | Color | new Color(0.05f, 0.05f, 0.05f, 0.95f) |  | Center dot and inner plus inside the small ring. |
| Scope overlay  rifle ADS (reticle / ring / outer crosshair) | `rifleRingColor` | Color | new Color(0f, 0f, 0f, 1f) |  | Small circular crosshair outer ring. |
| Scope overlay  rifle ADS (reticle / ring / outer crosshair) | `rifleOuterCrosshairColor` | Color | Color.white |  | Large mil-dot crosshair sprite tint. |
| Scope overlay  rifle ADS (reticle / ring / outer crosshair) | `rifleOuterCrosshairScale` | float | 0.8f | 0.35f–2.5f | Scale of the large mil-dot sprite (base 196 px). |
| Scope overlay  rifle ADS (reticle / ring / outer crosshair) | `rifleOuterCrosshairEmission` | float | 0f | 0f–8f | HDR-style bloom on UITK overlay (multiplies RGB; 0 = off). |
| Scope overlay  rifle ADS (reticle / ring / outer crosshair) | `rifleOuterCrosshairEmissionColor` | Color | Color.white |  |  |
| Scope overlay  rifle ADS (reticle / ring / outer crosshair) | `rifleRingOuterDiameterPx` | float | 56f | 8f–320f | Outer diameter in screen pixels of the small scoped inner ring. |
| Scope overlay  pistol ADS | `pistolReticleColor` | Color | new Color(0.05f, 0.05f, 0.05f, 0.95f) |  |  |
| Scope overlay  pistol ADS | `pistolRingColor` | Color | new Color(0f, 0f, 0f, 1f) |  |  |
| Scope overlay  pistol ADS | `pistolOuterCrosshairColor` | Color | Color.white |  |  |
| Scope overlay  pistol ADS | `pistolOuterCrosshairScale` | float | 0.45f | 0.35f–2.5f |  |
| Scope overlay  pistol ADS | `pistolOuterCrosshairEmission` | float | 0f | 0f–8f |  |
| Scope overlay  pistol ADS | `pistolOuterCrosshairEmissionColor` | Color | Color.white |  |  |
| Scope overlay  pistol ADS | `pistolRingOuterDiameterPx` | float | 40f | 8f–320f |  |
| Scope overlay  outer viewport mask (deprecated, runtime ignored) | `scopeCenterMaskEnabled` | bool | false |  | Deprecated: scope viewport masking removed from runtime overlay. |
| Scope overlay  outer viewport mask (deprecated, runtime ignored) | `scopeCenterMaskColor` | Color | new Color(0f, 0f, 0f, 1f) |  | Deprecated: no longer applied at runtime. |
| Scope overlay  outer viewport mask (deprecated, runtime ignored) | `scopeCenterMaskAlpha` | float | 0.32f | 0f–1f | Deprecated: no longer applied at runtime. |
| Scope overlay  outer viewport mask (deprecated, runtime ignored) | `scopeCenterMaskBlurPixels` | float | 12f | 0f–64f | Deprecated: no longer applied at runtime. |
| Scope overlay  outer viewport mask (deprecated, runtime ignored) | `scopeInnerRingViewportEdgeSoftness` | float | 0.012f | 0.005f–0.08f | Deprecated: passthrough vignette for scope removed from runtime. |

## Scope Profile

**What it does:** Full DM_ScopeProfile — scoped range, FOV, scopeAdsRightMouseToggle (RMB/LT click toggle vs hold ADS when Scope mode armed), scopeAdsEnablesZoomOverlay, fire interval, damage multipliers, ammo gate, audio, overlay. Rifles skill_scope_* modifiers apply in Play.

**Where:**
- Studio: `Genesis Studio / Combat / Scope Profile` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Combat/DM_ScopeProfile.asset`
- Script: `Assets/_Project/Scripts/Combat/Scope/DM_ScopeProfile.cs`

**Key settings:** same script as [Scope Overlay](#scope-overlay).

## Hit Catalog

**What it does:** Surface tag → hit mark mapping for ranged combat.

**Where:**
- Studio: `Genesis Studio / Combat / Hit Catalog` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Data/Items/Ammo/DMAmmoFxCatalog.asset`
- Script: `Assets/_Project/Scripts/Combat/DMAmmoFxCatalog.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `tag` | string | "Untagged" |  | Unity tag on the hit object. Untagged is the Io / world default. |
|  | `hitMark` | DMHitMarkSet |  |  |  |
|  | `fallback` | DMHitMarkSet |  |  | Used when the hit tag has no row. |

## Hit Marks

**What it does:** Enemy bullet and melee slash hit marks — DM_EnemyHitMarkProfile (Resources/Combat): mark limit, glow cool-down, bullet + slash decal size, surface normal offset / depth centre bias, hitbox template scale, debug emissive visibility, effect budget / distance, and blood / coolant / spark response per body type (Humanoid, Android, Robot). Live in Play.

**Where:**
- Studio: `Genesis Studio / Combat / Hit Marks` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Combat/DM_EnemyHitMarkProfile.asset`
- Script: `Assets/_Project/Scripts/Combat/HitMarks/DM_EnemyHitMarkProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `splatter` | DMEnemyHitFxKind | DMEnemyHitFxKind.None |  | Main splatter burst. Blood is always skipped for ammo that bans blood (Laser, Combat Plan §16). |
|  | `splatterScale` | float | 1f | 0.25f–3f |  |
|  | `sparks` | DMEnemyHitFxKind | DMEnemyHitFxKind.None |  | Second burst played with the splatter (sparks on mechs). |
|  | `sparkScale` | float | 1f | 0.25f–3f |  |
|  | `exitSpray` | bool | true |  | Heavy hits (see Exit spray damage) also spray the splatter out along the bullet path. |
|  | `burnStyle` | DMHitMarkStyle | DMHitMarkStyle.GlowBurn |  | Burn mark look. BloodChar = red blood spot with a char ring; GlowBurn = Laser scorch with a cooling glow. |
|  | `embers` | bool | true |  | Spawn a short ember / glow burst on glowing burn marks. |
| Mark limit & lifetime | `maxBurnMarksPerEnemy` | int | 5 | 1–MaxMarksPerEnemy | Burn marks kept per enemy (oldest rotates out). Lowering it in Play trims live enemies. |
| Mark limit & lifetime | `regenFadeSeconds` | float | 1.5f | 0.1f–5f | If an enemy's health goes back up (regen), marks stamped at or below the new health fade out over this time. |
| Player marks (enemy ranged hits on the player hitbox rig) | `playerMarkLifetimeSeconds` | float | 10f | 0f–60f | Seconds a hit mark stays on the PLAYER before it is removed (fades out during the last Player Mark Fade seconds). 0 = keep until death / revive. Enemy marks are unaffected (they persist until death or regen). |
| Player marks (enemy ranged hits on the player hitbox rig) | `playerMarkFadeSeconds` | float | 1.5f | 0.1f–5f | Fade-out time at the end of the player mark lifetime. |
| Player marks (enemy ranged hits on the player hitbox rig) | `playerMarkSizeScale` | float | 0.4f | 0.1f–1f | Player marks are this fraction of the enemy mark size (a 0.1 m x 2 enemy bullet mark is far too big for a head). |
| Player marks (enemy ranged hits on the player hitbox rig) | `playerMarkProjectionDepth` | float | 0.16f | 0.02f–0.5f | Real projection depth (metres) of a player mark, centred on the struck surface, so it cannot paint through the body onto the far side. |
| Glow & cool-down | `glowCoolSeconds` | float | 2f | 0.2f–6f | Seconds for a glowing burn to cool from hot to the dim residual. |
| Glow & cool-down | `glowResidual` | float | 0.15f | 0f–1f | Glow left after cooling (0 = fully out). Applies to cooled marks live. |
| Glow & cool-down | `glowSizeScale` | float | 0.7f | 0.2f–1.2f | Glow layer size relative to the burn mark. |
| Glow & cool-down | `emberGlowSeconds` | float | 1.5f | 0.2f–4f | Lifetime of the ember / glow burst on glowing burns. |
| Glow & cool-down | `laserAlwaysGlowBurn` | bool | true |  | Laser always leaves a glowing burn (cauterised, §16), even on Humanoid bodies. |
| Decal size & projection | `burnSize` | Vector2 | new Vector2(0.10f, 0.10f) |  | Random ranged ammo burn mark size range in metres (min, max) before rangedAmmoMarkSizeMultiplier. |
| Decal size & projection | `burnProjectionDepth` | float | 0.6f | 0.02f–1f |  |
| Decal size & projection | `rangedAmmoMarkSizeMultiplier` | float | 2f | 1f–4f | Multiplies bullet / glow burn decal footprint and projection depth (default 2×). |
| Decal size & projection | `markSurfaceNormalOffset` | float | 0.004f | 0f–0.05f | Push stamped marks along the outward surface normal (metres) so the decal volume straddles the struck face. |
| Decal size & projection | `markDepthCenterBias` | float | 0.5f | 0f–1f | When > 0, also shifts the stamp by this fraction of projection depth along the normal (centres the HDRP box on the face). |
| Decal size & projection | `burnAngleFade` | Vector2 | new Vector2(50f, 80f) |  | Start / end angle fade in degrees so marks do not smear along grazing surfaces. Applies to live marks. |
| Decal size & projection | `decalDrawDistance` | float | 40f |  | Marks stop drawing beyond this camera distance. Applies to live marks. |
| Effect budget & distance | `fxBurstBudget` | int | 24 | 4–64 | Max splatter / spark bursts alive at once across all enemies. |
| Effect budget & distance | `perEnemyMinInterval` | float | 0.06f | 0f–0.5f | Min seconds between splatter bursts on the same enemy (automatic fire). |
| Effect budget & distance | `fxMaxDistance` | float | 40f |  | No splatter / sparks / embers beyond this camera distance (marks still stamp). |
| Splatter shape | `splatterDamageScale` | Vector2 | new Vector2(0.8f, 1.3f) |  | Burst scale at zero damage (x) and at Full-scale damage (y). |
| Splatter shape | `splatterFullScaleDamage` | float | 30f | ≥1f | Damage that reaches the full splatter scale. |
| Splatter shape | `bloodEntryNormalBlend` | float | 0.35f | 0f–1f | How much the entry spray leans toward the surface normal (0) vs back at the shooter (1). |
| Hit FX scaling (Slice 1 / A4) - one shared path for blood (Humanoid), coolant (Android) and sparks (Robot) | `goreIntensity` | float | 1f | 0.25f–2.5f | Master slider: multiplies every blood splatter, coolant splatter and spark burst size. 1 = as before, 0.25 = barely, 2.5 = gory. |
| Hit FX scaling (Slice 1 / A4) - one shared path for blood (Humanoid), coolant (Android) and sparks (Robot) | `heavyHitSplatterMultiplier` | float | 1.35f | 1f–3f | Extra splatter size on a heavy / charged player hit. |
| Hit FX scaling (Slice 1 / A4) - one shared path for blood (Humanoid), coolant (Android) and sparks (Robot) | `chainHitSplatterBonus` | float | 0.05f | 0f–0.3f | Extra splatter size per hit already chained on the enemy (poise chain counter), added to 1. |
| Hit FX scaling (Slice 1 / A4) - one shared path for blood (Humanoid), coolant (Android) and sparks (Robot) | `chainHitSplatterMaxBonus` | float | 0.3f | 0f–1f | Cap on the total chain bonus. |
| Hit FX scaling (Slice 1 / A4) - one shared path for blood (Humanoid), coolant (Android) and sparks (Robot) | `coolantScaleMultiplier` | float | 1f | 0.25f–3f | Extra multiplier on top of the shared scaling for Android coolant splatter. 1 = same as blood. |
| Hit FX scaling (Slice 1 / A4) - one shared path for blood (Humanoid), coolant (Android) and sparks (Robot) | `sparkScaleMultiplier` | float | 1f | 0.25f–3f | Extra multiplier on top of the shared scaling for Robot / Android sparks. 1 = same as blood. |
| Hit FX scaling (Slice 1 / A4) - one shared path for blood (Humanoid), coolant (Android) and sparks (Robot) | `exitSprayDamageThreshold` | float | 25f |  | Hits at or above this damage also play an exit spray (per body type toggle below). |
| Hit FX scaling (Slice 1 / A4) - one shared path for blood (Humanoid), coolant (Android) and sparks (Robot) | `exitSprayStrength` | float | 0.3f | 0f–1f |  |
| Body type responses | `humanoidResponse` | DMEnemyBodyHitResponse | DMEnemyBodyHitResponse.HumanoidDefault() |  | Humanoid (D8): red blood splatter + blood spot with a char ring. |
| Body type responses | `androidResponse` | DMEnemyBodyHitResponse | DMEnemyBodyHitResponse.AndroidDefault() |  | Android: green coolant + small sparks + glowing burn. |
| Body type responses | `robotResponse` | DMEnemyBodyHitResponse | DMEnemyBodyHitResponse.RobotDefault() |  | Robot: large sparks + glowing burn. |
| Effect prefabs | `bloodHitPrefab` | GameObject |  |  | Blood kind. |
| Effect prefabs | `coolantHitPrefab` | GameObject |  |  | Coolant kind. |
| Effect prefabs | `sparkHitSmallPrefab` | GameObject |  |  | SparksSmall kind. |
| Effect prefabs | `sparkHitLargePrefab` | GameObject |  |  | SparksLarge kind (falls back to small when empty). |
| Effect prefabs | `burnEmberPrefab` | GameObject |  |  | Ember / glow burst on glowing burns. |
| Decal spin (all hit marks / decals) | `decalRandomSpin` | bool | true |  | Random spin around the surface normal for every hit mark / decal (bullet holes, burns, blood floor decals, impact decals, laser / molten marks). Keeps surface alignment. |
| Decal spin (all hit marks / decals) | `decalSpinRange` | Vector2 | new Vector2(0f, 360f) |  | Spin range in degrees (min, max). Default 0-360. |
| Decal spin (all hit marks / decals) | `slashSpinJitter` | float | 8f | 0f–45f | Melee slash marks stay aligned to the swing; this is their extra random jitter (+/- deg). |
| Melee slash marks | `slashSize` | Vector2 | new Vector2(0.6f, 0.1f) |  | Elongated decal along the swing (length × width in metres) before meleeSlashMarkSizeMultiplier. |
| Melee slash marks | `slashProjectionDepth` | float | 0.6f | 0.02f–1f |  |
| Melee slash marks | `meleeSlashMarkSizeMultiplier` | float | 3f | 1f–4f | Multiplies melee slash decal length, width, and projection depth (default 3×). |
| Melee slash marks | `androidMarkTint` | Color | new Color(0.32f, 0.34f, 0.32f, 1f) |  | Albedo tint for burn / slash decals on Androids (GlowBurn base is red; this keeps mech marks a scorch, not blood). |
| Melee slash marks | `robotMarkTint` | Color | new Color(0.34f, 0.33f, 0.3f, 1f) |  | Albedo tint for burn / slash decals on Robots / machines. |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `useVolumetricBlood` | bool | true |  | Spawn KriptoFX volumetric fluid on enemy hits (replaces the small blood / coolant particle; sparks stay). |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `prewarmVolumetricBlood` | bool | true |  | Pre-instantiates one of every blood prefab below (one per frame, hidden, after scene load) so the first hit of each type does not hitch while assets / shaders load. |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `fleshVolumetric` | DMVolumetricBloodSet | new DMVolumetricBloodSet() |  |  |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `androidVolumetric` | DMVolumetricBloodSet | new DMVolumetricBloodSet() |  |  |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `mechVolumetric` | DMVolumetricBloodSet | new DMVolumetricBloodSet() |  |  |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricDecalMode` | DMVolumetricDecalMode | DMVolumetricDecalMode.PlaneBeyondDistance |  | Projector = HDRP decal projector prefabs; Plane = SimplePlaneDecals (cheaper, mesh decal); PlaneBeyondDistance = projector up close, plane beyond volumetricPlaneLodDistance. |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricPlaneLodDistance` | float | 12f |  |  |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `fleshPlaneDecal` | DMVolumetricBloodSet | new DMVolumetricBloodSet() |  |  |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `androidPlaneDecal` | DMVolumetricBloodSet | new DMVolumetricBloodSet() |  |  |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `mechPlaneDecal` | DMVolumetricBloodSet | new DMVolumetricBloodSet() |  |  |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `androidFluidColor` | Color | new Color(0.35f, 2.4f, 0.5f, 1f) |  | Fluid colour of the _Android variants (BFX_Blood _Color; values above 1 read brighter). Apply with Tools > Dark Matter Genesis > Combat > Hit Marks > Blood FX - Recolor. |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `androidDecalTint` | Color | new Color(0.1f, 1.4f, 0.2f, 1f) |  |  |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `mechFluidColor` | Color | new Color(2.4f, 2.1f, 0.2f, 1f) |  |  |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `mechDecalTint` | Color | new Color(1.4f, 1.2f, 0.08f, 1f) |  |  |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricSideThreshold` | float | 0.2f | 0f–1f | \|dot(swing, enemy right)\| above this picks the left / right list; below = straight (any list). |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricSwapSides` | bool | false |  | Swap left / right if the vendor naming reads mirrored in game. |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricBudget` | int | 6 | 1–24 | Max volumetric fluids alive at once (pooled). |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricAirborneScale` | float | 0.7f | 0.2f–1.5f | Size of the airborne blood / coolant splatter (the flying fluid only; ground decals keep their own scale). 1 = vendor size. Colour and glow are untouched. |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `lightHitBloodChance` | float | 0.5f | 0f–1f | Chance that a normal / light hit plays the legacy blood / coolant particle burst (KriptoFX volumetric is not gated by this). |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `heavyHitBloodChance` | float | 1f | 0f–1f | Chance that a heavy / charged hit shows the blood / coolant spray. |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `heavyHitBloodScale` | float | 1.3f | 1f–2f | Heavy / charged hits: extra size on top of the airborne scale (heavier spray). |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricStraightUsesSideChance` | float | 0.5f | 0f–1f | Hits whose swing reads straight (no clear left / right) still use the Left / Right variants this often (random side). The swing direction is usually straight-on, so without this the side variants almost never played. |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `logVolumetricSide` | bool | false |  | Debug: log the chosen side + prefab of every volumetric spray (off by default). |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricMaxDistance` | float | 20f |  | No volumetric fluid beyond this camera distance (falls back to the small particle). |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricLifetime` | float | 8f |  | Seconds before a pooled fluid is recycled. |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricLightIntensity` | float | 1f |  | BFX_BloodSettings.LightIntensityMultiplier for spawned fluids. |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricFollowSwing` | bool | true |  | Volumetric fluid sprays along the blade swing (weapon travel at the hit point); off = away from the surface normal (KriptoFX demo rule). Left/right list is picked from the same swing vector. |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricYawOffset` | float | 90f | -180f–180f | Yaw added to the spray direction (90 = KriptoFX demo convention). |
| Volumetric blood (KriptoFX) - flesh = originals, Android = _Android, Robot / machines = _Mech | `volumetricOnPlayer` | bool | true |  | Red (flesh) KriptoFX volumetric blood on hits taken by the player; the old splatter is the fallback (budget / distance). |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricGroundSnap` | bool | true |  | Place the floor decal on the ground under the hit (terrain included, characters skipped) instead of the vendor RaycastAll. |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricGroundMask` | LayerMask | ~0 |  |  |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricGroundRayDistance` | float | 6f | 1f–20f |  |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricDecalRandomRotation` | bool | true |  | Landed decals get a random 0-360 yaw around the surface normal so splats don't stack aligned. |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricDecalScaleMin` | float | 0.85f | 0.3f–2f | Random landed-decal scale range per spawn (min). |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricDecalScaleMax` | float | 1.15f | 0.3f–2f | Random landed-decal scale range per spawn (max). |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricDecalSizeMultiplier` | float | 1f | 0.05f–4f | 2026-10-13: overall landed-decal size multiplier on top of the random range (0.25 = 75% smaller). Rotation / random range / projection unchanged. |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricDecalDepth` | float | 0.25f | 0f–2f | Ground decal projection box depth (m). Thin = no blood on legs / boots; too thin can miss sloped terrain. 0 = vendor depth. |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricDecalGroundOffset` | float | 0.08f | 0f–0.5f | How far the decal box reaches above the ground snap point (m). The box spans [ground + offset - depth, ground + offset]. |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricGroundTowardTarget` | float | 0.55f | 0f–1f | Nudge ground decals toward the victim root on XZ (0 = impact point only, 1 = at victim feet). |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricSpawnGroundDecals` | bool | true |  | When off, KriptoFX still spawns airborne fluid but skips floor HDRP decals (JitterDecal / BFX_DecalSettings). |
| KriptoFX HDRP fixes (2026-10-12) | `logVolumetricDecals` | bool | false |  | 2026-10-24t debug: one line per spray: ground hit, height above ground, ground decals on/off and why (off by default). |
| KriptoFX HDRP fixes (2026-10-12) | `volumetricGroundDecalCap` | int | 24 | 1–200 | 2026-10-24af: max KriptoFX sprays that keep ground blood decals at once (all enemies + player). Oldest spray loses its floor decal first. Effective max is also bounded by Volumetric Budget. |
| KriptoFX HDRP fixes (2026-10-12) | `logGroundDecalCap` | bool | false |  | 2026-10-24af: log when the ground decal cap strips decals from an older spray. |
| Melee slash style (2026-10-24af) | `meleePhysicalAlwaysCut` | bool | true |  | Physical blades (Gunpowder / no element) always stamp a neutral Cut slash; elemental weapons keep the burn style. |
| Melee slash style (2026-10-24af) | `cutSlashMaterial` | Material |  |  | Material for the Cut slash. Empty = Bullet Burn Material. |
| Melee slash style (2026-10-24af) | `slashOnKillingBlow` | bool | true |  | The killing blow also stamps a slash (stays on the corpse until it is removed). |
| Melee slash style (2026-10-24af) | `volumetricPlayerFluidGroundSnap` | bool | false |  | When on, player-hit fluids snap BFX ground height (often hides airborne spray). Off = keep vendor airborne arc on the player. |
| Melee slash style (2026-10-24af) | `volumetricFluidExposureComp` | bool | true |  | BFX_Blood is unlit: under HDRP physical exposure it renders black. On = its color is scaled by the sun intensity (lux / pi) at spawn. |
| Melee slash style (2026-10-24af) | `volumetricFluidBrightness` | float | 0.07f | 0f–4f | Fluid brightness multiplier on top of the exposure compensation (1 = lit like a 100% albedo surface). |
| Melee slash style (2026-10-24af) | `fluidMaxChannel` | float | 0.75f | 0.1f–4f | Airborne fluid: the material color is brought down to this max channel before the glow (hue kept). The Android / Mech copies are HDR (2.4) and bloomed into a screen-filling glow; 0.75 = the vendor red's brightness. |
| Melee slash style (2026-10-24af) | `fluidGlowFlesh` | float | 1f | 0f–4f | Per body type glow multiplier on the airborne fluid brightness (hue unchanged). 1 = natural. |
| Melee slash style (2026-10-24af) | `fluidGlowAndroid` | float | 1.4f | 0f–4f |  |
| Melee slash style (2026-10-24af) | `fluidGlowMech` | float | 1.4f | 0f–4f |  |
| Burn mark materials | `bulletBurnMaterial` | Material |  |  | BloodChar look: bright red blood spot with a circular char ring. 2x2 atlas. |
| Burn mark materials | `glowBurnMaterial` | Material |  |  | GlowBurn look: the Laser burn (scorch + char). 2x2 atlas. |
| Burn mark materials | `glowBurnHotMaterial` | Material |  |  | Emissive-only glow layer stacked on the glow burn; faded with the projector fade factor. |
| Burn mark materials | `bulletBurnHotMaterial` | Material |  |  | HDRP emissive flash for BloodChar bullet holes (debug / tuning). Empty = reuse glow hot material. |
| Hitboxes (template scale applies on next rig build / spawn) | `hitboxTemplateScale` | float | 1f | 0.5f–1.5f | Scales player-like per-bone DMHitbox templates (~1m humanoid). Live for newly built rigs. |
| Hitboxes (template scale applies on next rig build / spawn) | `hitboxInflate` | float | 1.05f | 1f–1.3f | Legacy ragdoll-copy inflate (unused — templates replace ragdoll copy). Kept for asset compatibility. |
| Hitboxes (template scale applies on next rig build / spawn) | `hitboxSweepRadius` | float | 0.02f | 0.005f–0.08f | Projectile sweep radius against hitboxes (world sweep keeps the projectile's own radius). Live. |
| Hitboxes (template scale applies on next rig build / spawn) | `addFillerHitboxes` | bool | true |  | Add filler hitboxes (neck) where the ragdoll leaves gaps. |
| Debug hit mark visibility | `debugBrightHitMarkEmission` | bool | true |  | Boost HDRP decal emission on bullet + melee marks so hits are easy to spot in Play. |
| Debug hit mark visibility | `debugHitMarkEmissionIntensity` | float | 10f | 1f–24f |  |
| Debug hit mark visibility | `debugMeleeSlashEmissiveGlow` | bool | true |  | Melee slash marks get the hot emissive glow layer when debug emission is on. |
| Debug hit mark visibility | `debugBrightRangedMarks` | bool | true |  | Ranged bullet marks get a hot emissive flash (round impact), including humanoid BloodChar holes. |
| Debug hit mark visibility | `debugHotGlowSizeScale` | float | 1.25f | 0.5f–2.5f | Scales the hot emissive decal when debug bright marks are active. |
| Debug hit mark visibility | `debugRangedImpactSpark` | bool | true |  | Brief spark burst on ranged BloodChar impacts (humanoids have no default sparks). |
| Zone damage multipliers (Combat Plan D4) | `headMultiplier` | float | 1.75f |  | Head shots (+75% default). |
| Zone damage multipliers (Combat Plan D4) | `torsoMultiplier` | float | 1.25f |  | Chest / torso (+25% default). |
| Zone damage multipliers (Combat Plan D4) | `armMultiplier` | float | 1f |  |  |
| Zone damage multipliers (Combat Plan D4) | `legMultiplier` | float | 1f |  |  |
| Rendering layer (advanced) | `enemyDecalRenderingLayer` | string | "DM Enemy Marks" |  | Rendering layer the enemy marks project onto (enemy renderers get this bit when first marked). |
| Rendering layer (advanced) | `enemyDecalRenderingLayerFallbackIndex` | int | 15 | 0–15 | Fallback rendering layer index when the named layer is missing. |

**Preset / apply methods:** `ApplyGroundDecalCapPreset()`

## Surface Damage

**What it does:** Rock carving from ammo hits: global switch, cut-face / rim look, debris, limits. Per-ammo carve lives on each Ammo FX profile.

**Where:**
- Studio: `Genesis Studio / Combat / Surface Damage` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Carve/DMCarveSettings.asset`
- Script: `Assets/_Project/Scripts/Environment/Carve/DMCarveSettings.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `source` | Material |  |  | Rock (or other) material on the carved object. |
|  | `fracture` | Material |  |  | Material for the inside of the cut. Empty = auto (tinted copy of the source). |
|  | `rim` | Material |  |  | Material for the dark jagged rim band. Empty = auto (dark copy of the source). |
| Ammo Carving (Play Mode) | `enableAmmoCarving` | bool | true |  | Master switch: ammo impacts carve DMCarvable objects using each ammo's Surface Damage. |
| Ammo Carving (Play Mode) | `ammoRadiusScale` | float | 1f | 0.1f–3f | Global multiplier on every ammo carve radius. |
| Ammo Carving (Play Mode) | `autoCarvableTags` | string[] | new string[0] |  | Hit objects with one of these Unity tags get a DMCarvable added on first hit. |
| Ammo Carving (Play Mode) | `autoCarvableNameContains` | string[] | { "rock", "boulder", "cliff", "stone", "outcrop" } |  | Hit objects whose name contains one of these words (case-insensitive) get a DMCarvable added on first hit. Clear the list to only carve objects marked carvable. |
| Ammo Carving (Play Mode) | `carveUnbatchesStatic` | bool | true |  | 2026-10-20x: carving a statically batched object detaches it at runtime (unbatched readable copy of its original mesh). Scene static flags are not changed. |
| Ammo Carving (Play Mode) | `maxTrianglesPerObject` | int | 300000 |  | Stop carving an object once it reaches this many triangles. |
| Ammo Carving (Play Mode) | `ammoDeformsMeshes` | bool | false |  | Ammo impacts dig craters into meshes. Off = meshes stay intact (no runtime mesh copy, no collider re-cook); shots leave molten marks (Resources/Combat/DMMoltenImpactMap) and, on carvable rocks, debris only. The Carve Tool is not affected. |
| Ammo Carving (Play Mode) | `debrisWithoutDeform` | bool | true |  | When ammo does not deform meshes: still throw debris chunks from carvable rocks. |
| Cutter Detail | `cutterSubdivisions` | int | 3 | 1–4 | Icosphere subdivisions for normal cutters (3 = 1280 triangles). |
| Cutter Detail | `smallCutterSubdivisions` | int | 2 | 1–4 | Subdivisions for small cutters (bullets). |
| Cutter Detail | `smallCutterRadius` | float | 0.2f |  | Cutters at or below this radius (m) use the small subdivision level. |
| Cutter Detail | `surfaceSplitEdge` | float | 0.3f | 0.1f–1f | Surface triangles under the cutter are split until their longest edge is below radius x this, so even big flat faces get a clean cut line. |
| Cut Face Look | `fractureTint` | Color | new Color(0.62f, 0.58f, 0.55f, 1f) |  | Tint on the auto fracture material (a copy of the rock's own material). |
| Cut Face Look | `rimTint` | Color | new Color(0.16f, 0.145f, 0.135f, 1f) |  | Tint on the auto rim material - the dark outline where the cut meets the surface. |
| Cut Face Look | `rimWidth` | float | 0.14f | 0f–0.6f | Width of the dark rim band, in cutter-radius units. |
| Cut Face Look | `rimJitter` | float | 0.65f | 0f–1f | How ragged the rim band is. 0 = even band, 1 = very jagged. |
| Cut Face Look | `cutUvTileMeters` | float | 1.5f | ≥0.05f | World meters per texture tile on cut faces (box-projected UVs). |
| Cut Face Look | `materialOverrides` | DMCarveMaterialRow[] | new DMCarveMaterialRow[0] |  | Explicit cut materials per source material. Unlisted materials get auto copies. |
| Debris (Play Mode) | `spawnDebris` | bool | true |  |  |
| Debris (Play Mode) | `debrisLifetime` | float | 6f | ≥0.1f |  |
| Debris (Play Mode) | `debrisImpulse` | float | 2.5f | ≥0f |  |
| Debris (Play Mode) | `maxLiveDebris` | int | 40 | ≥0 |  |
| Debris (Play Mode) | `debrisSizeScale` | float | 3f | 0.1f–10f | Global multiplier on debris chunk size, on top of each recipe's Debris Scale. |
| Impact VFX (2026-10-20v) | `explosionRing` | bool | false |  | Explosive impact ground ring / glow circle (horizontal billboard ring in the explosion VFX). Off = hidden at spawn (assets untouched). |
| Impact VFX (2026-10-20v) | `impactVfxHdrpFix` | bool | true |  | Impact VFX particles using non-HDRP shaders (render pink) get runtime HDRP materials (one cached copy per source material; assets untouched). |
| Impact VFX (2026-10-20v) | `impactDebrisSurfaceColor` | bool | true |  | Debris-like impact particles (mesh particles, or names with debris/chunk/rock/dirt/stone/dots) become HDRP Lit tinted with the hit surface color (Debris Look settings). |
| Debris Look (2026-10-20u, MaterialPropertyBlock - no material/prefab changes) | `debrisLookEnabled` | bool | true |  | Tint debris chunks from the surface that was hit, with a rough overlay. |
| Debris Look (2026-10-20u, MaterialPropertyBlock - no material/prefab changes) | `debrisColorSource` | DebrisColorSource | DebrisColorSource.SurfaceSample |  | SurfaceSample: base map at the hit UV when readable / terrain layer mix at the hit point, else material average. MaterialAverage: averaged material color. FractureMaterial: keep the cut material color (overlay still applies). |
| Debris Look (2026-10-20u, MaterialPropertyBlock - no material/prefab changes) | `debrisTintStrength` | float | 0.85f | 0f–1f | 0 = fracture material color, 1 = fully the sampled surface color. |
| Debris Look (2026-10-20u, MaterialPropertyBlock - no material/prefab changes) | `debrisOverlayTexture` | Texture2D |  |  | Rough grayscale overlay (base map). Empty = generated tileable noise. |
| Debris Look (2026-10-20u, MaterialPropertyBlock - no material/prefab changes) | `debrisOverlayStrength` | float | 0.55f | 0f–1f |  |
| Debris Look (2026-10-20u, MaterialPropertyBlock - no material/prefab changes) | `debrisOverlayTiling` | float | 2f | 0.1f–16f | Overlay tiles per UV unit (debris UVs are box-projected at Cut UV Tile Meters). |
| Debris Look (2026-10-20u, MaterialPropertyBlock - no material/prefab changes) | `debrisBrightness` | float | 1f | 0f–3f |  |
| Debris Look (2026-10-20u, MaterialPropertyBlock - no material/prefab changes) | `debrisSaturation` | float | 0.9f | 0f–2f |  |
| Debris Look (2026-10-20u, MaterialPropertyBlock - no material/prefab changes) | `debrisColorVariance` | float | 0.08f | 0f–0.5f | Random per-chunk variance (value / slight hue). |
| Tools | `dragStampSpacing` | float | 0.35f | 0.05f–2f | Drag spacing between stamps, in cutter-radius units. |
| Tools | `runtimeColliderRefresh` | float | 0.12f | 0f–1f | Seconds between MeshCollider rebuilds on an object being carved in Play Mode. |
| Tools | `keepDedicatedColliderInPlay` | bool | true |  | Play Mode: a carved object whose MeshCollider uses its own (simplified) mesh keeps that collider instead of switching to the full-detail carved render mesh. Colliders that already share the render mesh still update. |
| Tools | `colliderFollowsCarvedMesh` | bool | true |  | 2026-10-24af: Play Mode carves also cut the collider exactly like the visible mesh (overrides Keep Dedicated Collider; box / sphere / capsule colliders on the carved object are swapped for a MeshCollider of the carved mesh). Player, enemies and projectiles pass through carved holes. Costs a collider re-cook per carve. |
| Tools | `carveAmmoSkipsHitMarks` | bool | true |  | 2026-10-24af: Carve Lab experiment ammo leaves no hit mark / laser burn / molten spot (crater + glow only). |
| Hot Cut Glow (2026-10-24af) | `carveGlowEnabled` | bool | true |  | Cut faces glow like hot metal / rock right after a Play Mode carve, then cool (MaterialPropertyBlock _EmissiveColor; materials untouched). |
| Hot Cut Glow (2026-10-24af) | `carveSurfaceGlowEnabled` | bool | false |  | 2026-10-24ah: glow on carved cut faces and falling chunks. OFF = no emission / property block on carved meshes or chunks (molten drips still glow). |
| Hot Cut Glow (2026-10-24af) | `carveGlowColor` | Color | new Color(1f, 0.38f, 0.08f, 1f) |  |  |
| Hot Cut Glow (2026-10-24af) | `carveGlowIntensity` | float | 60f | 0f–5000f | Emission strength at curve value 1 (HDRP units; wide range for exposure). |
| Hot Cut Glow (2026-10-24af) | `carveGlowCoolSeconds` | float | 4f | 0.1f–60f | Seconds from carve to fully cooled. |
| Hot Cut Glow (2026-10-24af) | `carveGlowCurve` | AnimationCurve | new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.25f, 0.6f), new Keyframe(1f, 0f)) |  | Glow over normalised cool time (0..1). 1 = full hot. |
| Molten Drips (2026-10-24af) | `carveDripsEnabled` | bool | true |  |  |
| Molten Drips (2026-10-24af) | `carveDripRate` | float | 8f | 0f–200f | Drips per second per hot carved object. |
| Molten Drips (2026-10-24af) | `carveDripSize` | float | 0.025f | 0.002f–0.5f | Drip size (m). |
| Molten Drips (2026-10-24af) | `carveDripLifetime` | float | 2.5f | 0.1f–30f | Seconds a drip lives (glow follows the glow curve over this time). |
| Molten Drips (2026-10-24af) | `carveDripWindow` | float | 0.6f | 0f–1f | Fraction of the cool time during which the rim keeps dripping. |
| Molten Drips (2026-10-24af) | `carveDripMaxLive` | int | 80 | 0–1000 | Global cap on live drips (oldest recycled). |
| Molten Drips (2026-10-24af) | `carveDripGravity` | float | 1f | 0f–3f |  |
| Molten Drips (2026-10-24af) | `carveDripIntensity` | float | 2.5f | 0f–5000f |  |
| Molten Drips (2026-10-24af) | `carveDripCooledColor` | Color | new Color(0.08f, 0.07f, 0.065f, 1f) |  | Colour of a fully cooled drip. |
| Through-Holes (2026-10-24ag) | `throughHoleEnabled` | bool | true |  | When a carve hit goes through a wall / prop thinner than Max Thickness, cut one clean straight tunnel (closed inner wall, no ragged rim) instead of a noisy crater. |
| Through-Holes (2026-10-24ag) | `throughHoleMaxThickness` | float | 0.8f | 0.02f–10f | Max object thickness (m) along the hit normal that gets a clean through-hole. |
| Through-Holes (2026-10-24ag) | `throughHoleRadiusScale` | float | 1f | 0.2f–4f | Through-hole radius = carve radius x this. |
| Through-Holes (2026-10-24ag) | `throughHoleRimWidth` | float | 0.02f | 0f–0.3f | Dark rim band width on through-holes (cutter-radius units). 0 = none. |
| Through-Holes (2026-10-24ag) | `throughHoleSubdivisions` | int | 4 | 2–5 | Tunnel wall smoothness (icosphere subdivisions of the tunnel cutter). |
| Falling Chunks (2026-10-24ag) | `chunkSplitEnabled` | bool | true |  | A carve that fully separates a piece turns it into its own falling object (convex collider + Rigidbody), with the hot glow. |
| Falling Chunks (2026-10-24ag) | `chunkDensity` | float | 2400f | 10f–10000f | Chunk density (kg/m3 of bounds volume x 0.5). |
| Falling Chunks (2026-10-24ag) | `chunkMaxMass` | float | 400f | 0.1f–2000f |  |
| Falling Chunks (2026-10-24ag) | `chunkMinSize` | float | 0.04f | 0.001f–1f | Separated pieces smaller than this (m, largest side) are deleted instead of dropped (no slivers). |
| Falling Chunks (2026-10-24ag) | `chunkSearchRadii` | float | 3f | 1f–10f | Only pieces within this many cutter radii of the cut are split off (keeps separate parts of a model attached). |
| Falling Chunks (2026-10-24ag) | `chunkLifetime` | float | 20f | 1f–600f | Seconds before a chunk despawns. |
| Falling Chunks (2026-10-24ag) | `chunkMaxLive` | int | 24 | 0–200 | Max live chunks (oldest despawns). |
| Falling Chunks (2026-10-24ag) | `logChunkSplit` | bool | false |  | Log one line per carve that splits off / deletes pieces. |

**Preset / apply methods:** `ApplyCarveChunkPreset()`, `ApplyCarveHeatPreset()`

## Carve Tool

**What it does:** Edit Mode brush: click = one cut, hold + drag = continuous. Fracture / Erosion / Blast presets.

**Where:**
- Studio: `Genesis Studio / Combat / Carve Tool` (panel mode `ExternalTool`)

## AI & awareness

**What it does:** §31 #3 utility brain — DM_EnemyBrainProfile (Resources/Combat): archetype rows, personality multipliers, condition thresholds, defend / retreat scoring, chase wall standoff (~0.5 m) / stuck retreat. Live in Play.

**Where:**
- Studio: `Genesis Studio / Combat / AI & awareness` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Combat/DM_EnemyBrainProfile.asset`
- Script: `Assets/_Project/Scripts/AI/Brain/DM_EnemyBrainProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `archetype` | DMEnemyArchetype |  |  |  |
|  | `press` | float |  |  | Base utility of pressing the attack while holding the token. |
|  | `defend` | float |  |  | Base utility of a block / dodge when threatened. |
|  | `retreat` | float |  |  | Base utility of a desperation retreat (scaled by condition). |
|  | `tokenBias` | float |  |  | Added to the director focus score (wants the token more / less). |
|  | `holderTaunt` | float |  |  | Multiplies holder taunt weight. |
|  | `holderFeint` | float |  |  | Multiplies holder feint weight. |
|  | `personality` | DMEnemyPersonality |  |  |  |
|  | `pressMul` | float |  |  |  |
|  | `defendMul` | float |  |  |  |
|  | `retreatMul` | float |  |  |  |
|  | `tokenBiasAdd` | float |  |  |  |
|  | `holderSidestepMul` | float |  |  |  |
|  | `holderTauntMul` | float |  |  |  |
|  | `holderFeintMul` | float |  |  |  |
| Master | `enableUtilityBrain` | bool | true |  | Off = enemies skip utility decisions (no brain-driven defend / retreat, no token bias). Spacing still runs from the engagement profile. |
| Master | `autoAttachBrain` | bool | true |  | Add a DMEnemyBrain to every EnemyAiController that doesn't have one (uses the defaults below). |
| Master | `defaultArchetype` | DMEnemyArchetype | DMEnemyArchetype.Duelist |  |  |
| Master | `defaultPersonality` | DMEnemyPersonality | DMEnemyPersonality.None |  |  |
| Master | `debugLogDecisions` | bool | false |  |  |
| Master | `enableMovementPathDebug` | bool | false |  | Play mode: gold movement history on enemies with DMEnemyMovementPathDebug. Buffer reserved for future path analysis. |
| Master | `drawVirtualSteerPath` | bool | false |  | When path debug is visible: dashed yellow virtual steer polyline (root â†’ fan hint â†’ move target). Memory only â€” no path objects. |
| Decisions | `decisionInterval` | float | 0.8f | 0.1f–3f | The brain re-scores this often and holds its pick until the next decision. |
| Decisions | `scoreRandomness` | float | 0.25f | 0f–1f | Ã‚Â± random added to every score (seeded per enemy). |
| Decisions | `defendThreatMemory` | float | 1.2f | 0f–5f | Defend is only scored if the target hit this enemy within this many seconds, or is inside its attack range. |
| Decisions | `defendWhenHitBonus` | float | 0.6f |  | Added to Defend when the target hit this enemy recently. |
| Decisions | `defendCooldown` | float | 3f | 0f–20f |  |
| Decisions | `defendBlockShare` | float | 0.6f | 0f–1f |  |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `enableEnemyBlock` | bool | true |  | Master switch. Off = the guard pose is cosmetic again (hits are not absorbed, the player is not recoiled). |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `reactiveBlockChance` | float | 0.25f | 0f–1f | Reactive block: chance an enemy facing the player snaps its guard up as a player melee hit arrives (when the brain guard is not already up). Uses blockCooldown, blockArcDegrees and the definition blockChanceMultiplier. 0 = brain-only blocks (rare). |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockCooldown` | float | 4f | 0f–30f | Seconds before THIS enemy may start another block. Blocks only start between swings while the enemy holds the attack token, so the one-attacker cap is untouched. |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockDurationSeconds` | float | 1f | 0f–4f | How long a block lasts (s). 0 = use the prefab defensiveBlockDuration (1.2). |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockDamageFraction` | float | 0.1f | 0f–1f | Fraction of the damage taken by a blocked hit (0 = full absorb). Poise is fed with the reduced damage, so a block also reduces poise damage. |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockArcDegrees` | float | 150f | 30f–360f | Frontal arc (degrees, total) in which a blocking enemy can block. Hits from behind land normally. |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockLightHits` | bool | true |  | Light (non-charged) player melee hits can be blocked. |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockChargedHits` | bool | true |  | Charged / heavy player melee hits can be blocked (otherwise they land fully and the block stays up). |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `chargedBreaksBlock` | bool | true |  | A blockable charged / heavy hit breaks the guard: the block drops and the hit lands normally (full damage, normal reaction). A poise break also drops the guard. |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `playerBlockedRecoilChance` | float | 0.6f | 0f–1f | Chance a blocked hit recoils the player (Invector BreakAttack with playerBlockedRecoilId). Rolled per blocked hit. |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `playerBlockedRecoilId` | int | 2 | 1–3 | Player animator recoil id used for a blocked-hit recoil. 1 = recoil_unarmed (the clip the player own guard impact uses), 2 = recoil_low (default, visibly different), 3 = recoil_hard (CustomAction, long; clears the player block). The enemy block reaction is a procedural flinch, so these never look the same. |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `playerBlockedRecoilImmunitySeconds` | float | 1.5f | 0f–6f | After a recoil, the player cannot be recoiled again by a blocked hit for this long (no stun-lock). |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockSparkScale` | float | 0.8f | 0.3f–2.5f | Blocked-hit sparks at the blade contact point: scale (player parry sparks use DM_CombatCoreProfile parryClashVfxScale 1.15). |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockSparkPrefab` | GameObject |  |  | Optional spark prefab for enemy blocks. Empty = the same SparksLong prefab the parry uses. |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockedHitShakeAmplitude` | float | 0.3f | 0f–2f | Camera shake on a blocked hit (player block uses 0.18 / 0.08). |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockedHitShakeDurationSeconds` | float | 0.1f | 0f–0.4f |  |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockedHitSfx` | AudioClip |  |  | Sound played when an enemy blocks a player hit (empty = none). |
| Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance) | `blockedHitSfxVolume` | float | 0.8f | 0f–1f |  |
| Melee attacker cap (2026-10-13) | `meleeAttackerCapEnabled` | bool | true |  | Limit simultaneous melee swings on one target; ranged enemies are not counted. |
| Melee attacker cap (2026-10-13) | `maxMeleeAttackers` | int | 2 | 1–6 |  |
| Melee attacker cap (2026-10-13) | `meleeSwingStaggerMin` | float | 0.4f | 0f–2f | Random delay between melee swing starts (seconds). |
| Melee attacker cap (2026-10-13) | `meleeSwingStaggerMax` | float | 0.8f | 0f–2f |  |
| Enemy ranged aim (2026-10-13) | `enemyAimTargetBone` | HumanBodyBones | HumanBodyBones.Chest |  | Player bone enemy guns aim at by default (pistol + rifle). |
| Enemy ranged aim (2026-10-13) | `enemyHeadshotChance` | float | 0.15f | 0f–1f | Chance per shot to aim at the head instead. |
| Enemy visual aim correction (2026-10-13e) | `enemyAimCorrectionEnabled` | bool | true |  | While aiming, rotate the chest so the gun barrel points at the aim point (yaw + pitch). Projectiles unchanged. |
| Enemy visual aim correction (2026-10-13e) | `enemyAimCorrectionStrength` | float | 1f | 0f–1f |  |
| Enemy visual aim correction (2026-10-13e) | `enemyAimCorrectionMaxAngle` | float | 60f | 0f–90f |  |
| Enemy visual aim correction (2026-10-13e) | `enemyAimCorrectionSpeed` | float | 360f |  | Smoothing speed, degrees per second. |
| Enemy visual aim correction (2026-10-13e) | `enemyAimCorrectionFullDistance` | float | 8f | 1f–24f | Horizontal metres from target at which barrel aim correction reaches full strength. |
| Enemy visual aim correction (2026-10-13e) | `enemyAimCorrectionCloseDisableDistance` | float | 2.5f | 0.5f–8f | Below this horizontal distance (metres), barrel aim correction fades out (avoids close-range hunch / spin). |
| Enemy visual aim correction (2026-10-13e) | `enemyAimCorrectionCloseMaxPitch` | float | 28f | 5f–45f | At close range, clamp chest pitch aim to this many degrees (upper body only). |
| Enemy visual aim correction (2026-10-13e) | `enemySkipInvectorShootRecoil` | bool | true |  | Fire via Invector weapon.Shoot without ShooterManager.ApplyRecoil (stops per-shot spine kick at close range). |
| Enemy visual aim correction (2026-10-13e) | `logRangedFireGates` | bool | false |  | Debug: once-per-second per-enemy log of the ranged fire gate that blocks a shot. |
| Enemy visual aim correction (2026-10-13e) | `rangedFireWhileRetreating` | bool | true |  | Ranged enemies keep firing while they back off to their hold distance (else a close player is never shot). |
| Enemy visual aim correction (2026-10-13e) | `reconcileRangedEngageToVision` | bool | true |  | Ranged engage range = max(definition engage, min(weapon range, vision range)): enemies shoot as far as they can see and hit. |
| Enemy visual aim correction (2026-10-13e) | `rifleEngageRangeBase` | float | 25f | 0f–80f | Rifle (two-handed ranged) users: base engage range and vision floor, metres. Pistol unchanged. Extra range from upgrades is added via EnemyInvectorCombatBridge.RifleRangeBonus. |
| Enemy visual aim correction (2026-10-13e) | `rifleVisionFloor` | float | 25f | 0f–80f |  |
| Chase stuck recovery (2026-10-24ac) | `enableChaseStuckRecovery` | bool | true |  | Chase / investigate / search: if the enemy barely moves for chaseStuckSeconds while its goal is still far, sidestep around the blocker (alternating sides). |
| Chase stuck recovery (2026-10-24ac) | `chaseStuckSeconds` | float | 1.5f | 0.5f–5f |  |
| Chase stuck recovery (2026-10-24ac) | `chaseStuckMinMove` | float | 0.4f | 0.05f–2f | Less horizontal movement than this (m) over chaseStuckSeconds = stuck. |
| Chase stuck recovery (2026-10-24ac) | `chaseStuckSidestepMeters` | float | 2f | 0.5f–5f | Sideways distance of the recovery sidestep (m). |
| Chase stuck recovery (2026-10-24ac) | `chaseTerrainMaxSlopeDeg` | float | 60f | 30f–89f | Terrain steeper than this (deg) blocks like a wall; gentler terrain is ground, never a wall (slope stalls). |
| Chase stuck recovery (2026-10-24ac) | `groundMeshMaxSlopeDeg` | float | 50f | 10f–80f | Mesh colliders (rocks, props) count as ground only where their surface is flatter than this (deg): no snapping up rock flanks. |
| Chase stuck recovery (2026-10-24ac) | `chaseStuckHardAfter` | int | 2 | 0–6 | 24ad: after this many failed sidesteps in a row (enemy wedged inside / against a rock mesh), move it to the nearest free grounded spot. 0 = never. |
| Chase stuck recovery (2026-10-24ac) | `chaseStuckHardMaxRadius` | float | 4f | 1f–8f | 24ad: max search radius (m) for the hard-unstick free spot. |
| Chase stuck recovery (2026-10-24ac) | `chaseStuckLog` | bool | true |  |  |
| Stall watchdog (2026-10-24ae) | `stallWatchdogPauseSeconds` | float | 6f | 0f–15f | Force-release AI locomotion pause if it is held longer than this (s) while alive (a hit reaction / stagger that never released). 0 = off. |
| Stall watchdog (2026-10-24ae) | `stallWatchdogAnimatorSeconds` | float | 1.5f | 0f–5f | Restore animator speed to 1 if it stays at 0 longer than this (s) (hit-stop never ended = frozen pose). 0 = off. |
| Stall watchdog (2026-10-24ae) | `stallWatchdogLog` | bool | true |  |  |
| Chase wall slide (no NavMesh) | `enableChaseWallSlide` | bool | true |  | Capsule-cast chase steps against Default / Building / Climbable / Resource / PW blockers and slide along walls. |
| Chase wall slide (no NavMesh) | `chaseWallSlideRadius` | float | 0.35f | 0.15f–0.8f | Fallback capsule radius when no CapsuleCollider is found on the enemy. |
| Chase wall slide (no NavMesh) | `chaseWallSlideHeight` | float | 1.75f | 0.8f–2.5f | Fallback capsule height when no CapsuleCollider is found on the enemy. |
| Chase wall slide (no NavMesh) | `chaseWallSlideSkin` | float | 0.05f | 0.01f–0.2f | Shrink allowed move distance before the hit normal (metres). |
| Chase wall slide (no NavMesh) | `chaseWallStandoffMeters` | float | 0.5f | 0.15f–1.2f | Standoff buffer from Default / Building / Climbable walls during chase / investigate / search (capsule cast skin). |
| Chase wall slide (no NavMesh) | `chaseWallBufferStuckSeconds` | float | 1.5f | 0.5f–4f | Seconds hugging the wall buffer or blocked before a brief retreat / sidestep nudge. |
| Chase wall slide (no NavMesh) | `chaseWallRetreatStepMeters` | float | 0.85f | 0.2f–2f | Metres to step along the wall outward normal when unsticking. |
| Chase wall slide (no NavMesh) | `chaseWallRetreatSidestepMeters` | float | 0.55f | 0f–1.5f | Optional tangent sidestep mixed into the retreat nudge (metres). |
| Chase wall slide (no NavMesh) | `chaseWallRetreatSidestepSeconds` | float | 0.65f | 0.2f–2.5f | Seconds to steer toward the sidestep waypoint after a wall retreat nudge. |
| Chase wall slide (no NavMesh) | `chaseWallSlidePasses` | int | 2 | 1–4 | Slide-along-wall iterations per chase step. |
| Chase wall slide (no NavMesh) | `chaseWallMinProgress` | float | 0.002f | 0.0005f–0.05f | Chase steps smaller than this (metres) count as blocked for wall-follow. |
| Chase wall slide (no NavMesh) | `chaseWallBlockedFramesBeforeFollow` | int | 2 | 1–12 | Blocked frames before tangential wall-follow toward the chase target. |
| Chase wall slide (no NavMesh) | `chaseWallProbeDistance` | float | 0.6f | 0.2f–1.5f | Forward capsule probe distance used to read the wall normal when head-on. |
| Chase wall slide (no NavMesh) | `chaseWallFollowFaceSlide` | bool | true |  | Face the slide / wall-follow direction instead of staring into the wall. |
| Chase wall slide (no NavMesh) | `chaseWallBlockedTurnRate` | float | 240f | 120f–720f | Turn rate (deg/s) while blocked and steering along a wall. |
| Chase wall slide (no NavMesh) | `chaseWallFollowMinSideSeconds` | float | 0.55f | 0.1f–2f | Min seconds to hold a wall-follow tangent side before picking the other (reduces left/right jitter). |
| Chase wall slide (no NavMesh) | `chaseWallFollowSideHysteresis` | float | 0.18f | 0.05f–0.5f | Dot-product gap required before swapping wall-follow side. |
| Chase wall slide (no NavMesh) | `chaseWallFollowFlipAfterFails` | int | 3 | 2–8 | Consecutive failed wall-follow steps before flipping tangential side (reduces left-right hunting). |
| Chase wall slide (no NavMesh) | `enableChaseWallPredictiveFan` | bool | false |  | Capsule fan ahead of the chase target to pick a clearer direction before slide / wall-follow. |
| Chase wall slide (no NavMesh) | `chaseWallFanRayCount` | int | 7 | 3–11 |  |
| Chase wall slide (no NavMesh) | `chaseWallFanHalfAngleDegrees` | float | 42f | 10f–75f |  |
| Chase wall slide (no NavMesh) | `chaseWallFanLookAheadScale` | float | 1.35f | 0.8f–2.5f | Multiplies chaseWallProbeDistance for fan clearance samples. |
| Chase wall slide (no NavMesh) | `chaseWallFanLatchSeconds` | float | 0.4f | 0.15f–1.2f |  |
| Chase wall slide (no NavMesh) | `chaseWallFanDirectionHysteresis` | float | 0.08f | 0.02f–0.25f | Score margin required before replacing a latched fan direction (reduces L/R hunting). |
| Chase wall slide (no NavMesh) | `chaseWallFanMinRetargetSeconds` | float | 0.32f | 0.1f–1.2f | Min seconds between full fan re-scans when the latched direction still clears ahead. |
| Chase wall slide (no NavMesh) | `chaseWallRingLockSeconds` | float | 1.05f | 0.4f–2.5f | Seconds to hold combat ring bearing / chase point while in wall standoff buffer. |
| Chase wall slide (no NavMesh) | `enableChaseWallLongRangeFan` | bool | false |  | Sparse long-range fan toward chase target for macro bearing only (local slide unchanged). |
| Chase wall slide (no NavMesh) | `chaseWallLongRangeFanDistance` | float | 20f | 8f–24f |  |
| Chase wall slide (no NavMesh) | `chaseWallLongRangeFanRayCount` | int | 3 | 3–5 |  |
| Chase wall slide (no NavMesh) | `chaseWallLongRangeFanBlend` | float | 0.35f | 0.1f–0.55f | Blend weight from long-range macro bearing into short fan wish direction. |
| Chase wall slide (no NavMesh) | `chaseWallStandoffPushMinDeficit` | float | 0.07f | 0.02f–0.15f | Ignore tiny standoff buffer pushes below this clearance deficit (metres). |
| Chase wall slide (no NavMesh) | `chaseWallFollowRequiresHeadOnBlock` | bool | true |  | Only enter tangential wall-follow when a forward probe hits geometry (not buffer-only stalls). |
| Chase wall slide (no NavMesh) | `chaseWallStuckFramesEarlyInvestigate` | int | 24 | 4–90 | Blocked/stutter frames at a wall with no chase progress â†’ start lost-sight investigate early. |
| Chase wall slide (no NavMesh) | `chaseUnreachableMinVerticalDelta` | float | 2.25f | 1f–8f | Player this many metres above the enemy with no LOS â†’ treat as unreachable direct chase (flatten / investigate). |
| Chase wall slide (no NavMesh) | `chaseUnreachableMaxHorizontal` | float | 12f | 2f–20f | Horizontal range for unreachable-roof rule (metres). |
| Chase wall slide (no NavMesh) | `requireClearLosForRangedFire` | bool | true |  | Ranged attacks require EnemySenses line-of-sight (melee touch range still bypasses LOS). |
| Lost sight â€” investigate | `enableInvestigateOnLostSight` | bool | true |  | When chase loses line-of-sight to the player, go to last known position then search instead of immediately giving up. |
| Lost sight â€” investigate | `investigateUseProfileSearchTiming` | bool | true |  | Use the search duration / radius below instead of each enemy prefab's search fields. |
| Lost sight â€” investigate | `investigateSearchDurationSeconds` | float | 8f | 2f–30f |  |
| Lost sight â€” investigate | `investigateSearchRadiusMeters` | float | 5f | 1f–12f |  |
| Lost sight â€” investigate | `investigateSearchSpeedScale` | float | 0.65f | 0.3f–1f | Walk speed scale while searching the last-known area. |
| Lost sight â€” investigate | `investigateLookSweepSeconds` | float | 1.25f | 0f–4f | Seconds to pause and sweep look direction at each search waypoint. |
| Lost sight â€” investigate | `investigateLookSweepTurnRate` | float | 140f | 30f–360f | Degrees per second while paused on a search look sweep. |
| Lost sight â€” investigate | `investigateWallBaseRayDistance` | float | 22f | 4f–40f | When player was elevated / unreachable, ray toward last seen and anchor search at the wall impact (metres). |
| Lost sight â€” investigate | `investigateWallAnchorOffset` | float | 0.45f | 0.1f–1.2f | Stand-off from the wall plane at the investigate anchor. |
| Lost sight â€” investigate | `investigateSearchAlongWall` | bool | true |  | Search by walking along the wall tangent instead of orbiting last-known XZ. |
| Lost sight â€” investigate | `investigateWallPatrolSpanMeters` | float | 7f | 2f–16f | Max patrol distance each way from the wall-base anchor (metres). |
| Lost sight â€” investigate | `investigateElevatedThreatSecondsToSearch` | float | 1.35f | 0.5f–8f | Seconds blind + elevated threat at the wall foot before forcing Search (hide on roof). |
| Lost sight â€” investigate | `investigateStuckCommitSeconds` | float | 2f | 0.5f–6f | Seconds stuck within ~1m of investigate anchor before committing to Search. |
| Lost sight â€” investigate | `allowMeleeVsClimbingPlayer` | bool | false |  | When false, melee enemies do not swing at a climbing player (ranged is unaffected). |
| Enemy ranged ammo pool | `enemyRangedAmmoPoolDefault` | int | 1 | 0–999 | Default shots each ranged enemy may fire before reload. EnemyDefinition.rangedAmmoPool > 0 overrides per type. 0 = unlimited (infinite mag). |
| Enemy ranged ammo pool | `enemyRangedAmmoReloadSeconds` | float | 2.5f | 0f–30f | Seconds after the last shot in a mag before the pool refills. 0 = no auto-reload (empty until respawn / Reapply). |
| Enemy guard stamina | `enemyGuardStaminaMax` | float | 60f | 1f–300f | Guard stamina pool per enemy (EnemyDefinition.guardStaminaMax overrides when > 0). Empty = guard breaks. |
| Enemy guard stamina | `enemyGuardRegenPerSec` | float | 12f | 0f–100f |  |
| Enemy guard stamina | `enemyGuardRegenDelay` | float | 1f | 0f–5f | Seconds after the last spend before regen starts. |
| Enemy guard stamina | `enemyBlockHoldCostBase` | float | 3f | 0f–30f | Holding the guard drains per second = base x growth ^ seconds held (capped). |
| Enemy guard stamina | `enemyBlockHoldCostGrowth` | float | 1.6f | 1f–4f |  |
| Enemy guard stamina | `enemyBlockHoldCostCap` | float | 25f | 0f–100f |  |
| Enemy guard stamina | `enemyBlockedHitStaminaCost` | float | 12f | 0f–100f | Flat cost per blocked hit. |
| Enemy guard stamina | `enemyStaggerStaminaCost` | float | 20f | 0f–100f | Cost when staggered or the guard is broken (more than a simple block). |
| Enemy guard stamina | `enemyMinGuardStaminaToBlock` | float | 15f | 0f–100f | An enemy will not raise its guard below this stamina. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeRollChance` | float | 1f | 0f–1f | When a Defend decision does not block, chance the enemy actually rolls (otherwise it falls back to a block, then to attacking). 1 = old behaviour. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeRollCooldown` | float | 2f | 0f–20f | Seconds before THIS enemy may roll again (per enemy). Defend cooldown still applies on top. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeRollSideWeight` | float | 1f | ≥0f | Relative weight of rolling to the LEFT and, separately, to the RIGHT of the threat (each side gets this weight). Old behaviour: sides 1 each, back 1. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeRollBackWeight` | float | 1f | ≥0f | Relative weight of rolling directly AWAY from the threat (backwards). |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeRollForwardWeight` | float | 0f | ≥0f | Relative weight of rolling TOWARD the threat. 0 = never roll forward (the forward roll does not look right). If every weight is 0 the roll goes backwards. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeRollDistance` | float | 3.2f | 0f–6f | Meters the dodge roll travels along the chosen direction (the roll clip is played in place on transform-driven enemies, so the AI moves the body). 0 = roll in place. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeRollMoveEndNormalized` | float | 0.75f | 0.3f–1f | Movement of the side roll runs with the roll clip: it reaches full distance when the Roll animator state reaches this normalized time (the state exits at 0.7 and blends out for 0.1 s). Higher = movement lasts longer into the exit blend. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeBackMoveEndNormalized` | float | 0.85f | 0.3f–1f | Same for the backward dodge clip (DM_DodgeBack): the displacement finishes at this normalized time of the clip. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeRollSeconds` | float | 1.1f | 0.3f–2f | Fallback / timeout length (s) of a side roll when the animator state cannot be read. Movement normally follows the clip, not this timer (was a fixed 0.65 s). |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeBackUseClip` | bool | true |  | BACKWARD dodges play the Humanoid@DodgeBackwardsCombat clip (animator state DM_DodgeBack in Invector@ShooterMelee.controller) instead of the forward-roll clip turned around. The body faces the threat and the AI moves it away. Off, or no such state on the controller = old turned-around roll. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeBackSeconds` | float | 0.75f | 0.3f–1.6f | Length (s) of the backward dodge. The clip (1.4 s) is sped up to fit (animator parameter DMDodgeBackSpeed). |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeBackDistance` | float | 2.2f | 0f–5f | Meters the backward dodge travels away from the threat (the clip itself is in place; the AI moves the body). |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeBackBlendIn` | float | 0.08f | 0f–0.4f | Cross-fade IN time (s) from whatever the animator was playing into the backward dodge clip. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeBackBlendOut` | float | 0.15f | 0f–0.5f | Cross-fade OUT time (s) from the backward dodge clip back to locomotion (starts this long before the dodge ends). |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeRollSnapFacing` | bool | true |  | Turn the enemy to the roll direction instantly when the roll starts, so the roll animation (a forward roll) does not first travel toward the threat. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `allowForwardRollFallback` | bool | false |  | When no DM_Dodge clip fits (or the animator is not ready), play the forward Roll instead of skipping the dodge. Off = never forward roll. |
| Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll) | `dodgeTurnDegPerSec` | float | 720f | 0f–2000f | Dodge start / in-dodge turn rate (deg/s): the body turns toward the dodge facing instead of snapping. 0 = old instant snap. |
| Side dodge clip (clipfix): side rolls use a real side-dodge clip, the body faces the threat and never plays the forward roll | `dodgeSideUseClip` | bool | true |  | On = a side roll plays DM_DodgeLeft / DM_DodgeRight (Standing Dodge Left/Right) with the body facing the threat. Off = old forward Roll clip turned sideways. |
| Side dodge clip (clipfix): side rolls use a real side-dodge clip, the body faces the threat and never plays the forward roll | `dodgeSideSeconds` | float | 0.95f | 0.3f–1.6f | Total length of a side dodge (clip is time-scaled to this). |
| Side dodge clip (clipfix): side rolls use a real side-dodge clip, the body faces the threat and never plays the forward roll | `dodgeSideDistance` | float | 2.6f | 0f–5f | Distance travelled by a side dodge (m). |
| Side dodge clip (clipfix): side rolls use a real side-dodge clip, the body faces the threat and never plays the forward roll | `dodgeSideMoveEndNormalized` | float | 0.85f | 0.3f–1f | Normalized clip time at which the side dodge movement is complete. |
| Side dodge clip (clipfix): side rolls use a real side-dodge clip, the body faces the threat and never plays the forward roll | `dodgeSideBlendIn` | float | 0.08f | 0f–0.4f |  |
| Side dodge clip (clipfix): side rolls use a real side-dodge clip, the body faces the threat and never plays the forward roll | `dodgeSideBlendOut` | float | 0.15f | 0f–0.5f |  |
| Side dodge clip (clipfix): side rolls use a real side-dodge clip, the body faces the threat and never plays the forward roll | `dodgeRollBlocksHitReactions` | bool | true |  | On = hit reactions (flinch, stagger, hit slide) are ignored while a dodge roll plays, so the roll is never cut mid-animation. Damage still applies. |
| Side dodge clip (clipfix): side rolls use a real side-dodge clip, the body faces the threat and never plays the forward roll | `retreatSeconds` | float | 5f | 0f–20f | Desperation retreat: give up the token and back off to the outer ring for this long. |
| Side dodge clip (clipfix): side rolls use a real side-dodge clip, the body faces the threat and never plays the forward roll | `retreatCooldown` | float | 10f | 0f–60f |  |
| Low-health retreat stability (2026-10-24a) | `conditionRecoverHysteresis` | float | 0.08f | 0f–0.5f | Health fraction a condition must recover past its threshold before it improves again (hysteresis; stops retreat toggling near a threshold). 0 = off. |
| Low-health retreat stability (2026-10-24a) | `retreatCancelGuardSeconds` | float | 1f | 0f–10f | An attack-slot award cannot cancel a retreat during its first N seconds. |
| Low-health retreat stability (2026-10-24a) | `retreatStuckSeconds` | float | 1f | 0.2f–10f | Seconds without progress toward the retreat point before one re-pick, then giving up and fighting again. |
| Low-health retreat stability (2026-10-24a) | `retreatMinProgress` | float | 0.3f | 0.01f–3f | Metres the enemy must close on its retreat point within the stuck window to count as progress. |
| Low-health retreat stability (2026-10-24a) | `retreatStuckRepickDegrees` | float | 60f | 0f–180f | Bearing offset (deg) for the single stuck re-pick before giving up. |
| Low-health retreat stability (2026-10-24a) | `retreatRepickInterval` | float | 0.75f | 0f–10f | Minimum seconds between hold / retreat point re-picks (a ring-radius change no longer bypasses the re-anchor cooldown). |
| Low-health retreat stability (2026-10-24a) | `retreatAnchorObstacleCheck` | bool | true |  | Reject hold / retreat point candidates whose body-radius path is blocked, that overlap a collider, or have no ground (collider checks only, no NavMesh). |
| Low-health retreat stability (2026-10-24a) | `holderArriveRadius` | float | 0.5f | 0.05f–3f | Holder stops moving inside this distance of its point; counts as arrived at +0.1 m. |
| Low-health retreat stability (2026-10-24a) | `rangedRetreatHysteresis` | float | 1f | 0f–10f | Ranged back-off continues until the player is this far beyond the retreat distance (hysteresis). |
| Condition thresholds (health fraction) | `injuredBelow` | float | 0.75f | 0f–1f |  |
| Condition thresholds (health fraction) | `severelyInjuredBelow` | float | 0.5f | 0f–1f |  |
| Condition thresholds (health fraction) | `criticalBelow` | float | 0.25f | 0f–1f |  |
| Condition thresholds (health fraction) | `conditionPress` | Vector4 | new Vector4(1f, 1f, 0.9f, 0.8f) |  | Press multiplier at Healthy / Injured / Severely injured / Critical. |
| Condition thresholds (health fraction) | `conditionDefend` | Vector4 | new Vector4(1f, 1.15f, 1.3f, 1.5f) |  |  |
| Condition thresholds (health fraction) | `conditionRetreat` | Vector4 | new Vector4(0f, 0f, 0.4f, 1.2f) |  |  |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `enableDodgeStrike` | bool | true |  | Master switch. Melee engagers (the enemy holding the attack token) only; the dodge is a short side weave at the current distance, then the normal attack runs. |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `dodgeStrikeChance` | float | 0.15f | 0f–1f | Chance per roll that a ready engager opens its swing with a dodge strike (scaled per enemy type by EnemyDefinition.dodgeStrikeChanceMultiplier). |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `dodgeStrikeRollInterval` | float | 1.5f | 0.2f–5f | Seconds between rolls while the attack is ready (so the chance is per opportunity, not per frame). |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `dodgeStrikeCooldown` | float | 6f | 0f–30f | Minimum seconds between two dodge strikes of the same enemy. |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `dodgeStrikeMaxRangeFactor` | float | 1.1f | 0.3f–2f | Only starts when the target is inside this multiple of the enemy's attack range (keeps the D1 melee band; the weave moves around the target at constant distance). |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `dodgeStrikeSideDistance` | Vector2 | new Vector2(0.6f, 1.0f) |  | Sideways weave length in metres, taken around the target at constant distance (min, max). |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `dodgeStrikeSeconds` | float | 0.8f | 0.4f–1.5f | Length in seconds the HumanM@Dodge01 clip (1.07 s natural) is played at before the swing starts; smaller = snappier. |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `dodgeStrikeAttackAtNormalized` | float | 0.6f | 0.3f–1f | The dodge hands over to the normal attack when the clip reaches this normalized time (the clip is blended out to locomotion). |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `dodgeStrikeMoveEndNormalized` | float | 0.5f | 0.2f–1f | The side weave reaches its full length at this normalized time of the clip (ease-out ends with the weave, before the hand-over). |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `dodgeStrikeBlendIn` | float | 0.08f | 0f–0.4f |  |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `dodgeStrikeBlendOut` | float | 0.12f | 0f–0.4f |  |
| Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip) | `dodgeStrikeTurnRate` | float | 540f | 90f–1080f | Turn rate (deg/s) used to keep facing the target during the weave. |

**Preset / apply methods:** `ApplyChaseStuckPreset()`, `ApplyObstacleAvoidancePreset()`

## Chase wall & fan

**What it does:** Filtered slice of DM_EnemyBrainProfile: wall slide, standoff buffer, predictive fan rays, hysteresis, and stuck retreat — NavMesh-free chase geometry.

**Where:**
- Studio: `Genesis Studio / Combat / Chase wall & fan` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Combat/DM_EnemyBrainProfile.asset`
- Script: `Assets/_Project/Scripts/AI/Brain/DM_EnemyBrainProfile.cs`

**Key settings:** same script as [AI & awareness](#ai--awareness).

## Enemy Types

**What it does:** Every EnemyDefinition plus prefabs that use it: category, archetype, personalities, body type, prefer-ranged, and profile overrides. Global brain / engagement profiles remain the defaults.

**Where:**
- Studio: `Genesis Studio / Combat / Enemy Types` (panel mode `EnemyTypesTable`)

## Director & tokens

**What it does:** Phase 3 spacing — DM_EnemyEngagementProfile (Resources/Combat): one melee attacker per target, token hand-off / anti-thrash, hold ring, holder sidestep / taunt / feint, facing and tracking. Live in Play. Attack slots by enemy count live under Combat Director.

**Where:**
- Studio: `Genesis Studio / Combat / Director & tokens` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Combat/DM_EnemyEngagementProfile.asset`
- Script: `Assets/_Project/Scripts/AI/Brain/DM_EnemyEngagementProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Master | `enableEngagementDirector` | bool | true |  | Off = legacy behaviour (every enemy attacks whenever in range). Rollback switch for the whole spacing system. |
| Master | `legacyRingSlotRotation` | bool | false |  | Legacy per-enemy ±42° ring slot rotation (the cause of the constant circling). Leave off. |
| Master | `debugLogHandoffs` | bool | false |  | Logs token hand-offs to the console (editor/dev builds). |
| Ticks | `directorTickHz` | float | 5f | 1f–20f |  |
| Ticks | `agentRoleTickHz` | float | 10f | 2f–30f |  |
| Melee token (D1: exactly one attacker per target) | `tokenMinHoldSeconds` | float | 2.5f | 0f–10f | Current token holder keeps it at least this long unless it becomes ineligible. |
| Melee token (D1: exactly one attacker per target) | `switchScoreMargin` | float | 2f | 0f–10f | A challenger needs holder score + this margin… |
| Melee token (D1: exactly one attacker per target) | `switchConfirmSeconds` | float | 0.35f | 0f–3f | …sustained for this long. |
| Melee token (D1: exactly one attacker per target) | `minSecondsBetweenHandoffs` | float | 1.5f | 0f–10f | At most one hand-off per target in this window. |
| Melee token (D1: exactly one attacker per target) | `postSwingLockSeconds` | float | 0.6f | 0f–2f | Token can't move for this long after a swing / combo / charged attack ends. |
| Melee token (D1: exactly one attacker per target) | `tokenHoldMaxSequences` | int | 2 | 1–10 | Holder releases the token after this many attack sequences (if anyone else is eligible). |
| Melee token (D1: exactly one attacker per target) | `tokenHoldMaxSeconds` | float | 6f | 1f–30f | Holder releases the token after this long (if anyone else is eligible). |
| Melee token (D1: exactly one attacker per target) | `handoffGraceBeforeSwing` | Vector2 | new Vector2(0.6f, 1.2f) |  | New Engager waits a random time in this range before its first swing (hand-over tell). |
| Melee token (D1: exactly one attacker per target) | `reawardCooldown` | float | 1.5f | 0f–10f | The old holder can't be re-awarded the token for this long (ignored when nobody else is eligible). |
| Melee token (D1: exactly one attacker per target) | `candidateMaxDistance` | float | 8f | 2f–30f | Enemies farther than this from the target are not token candidates. |
| Focus score weights (§5.4) | `weightPlayerHit` | float | 5f |  |  |
| Focus score weights (§5.4) | `playerHitMemory` | float | 1.5f | 0.1f–6f | Seconds the 'player hit this enemy' bonus lasts (linear decay). |
| Focus score weights (§5.4) | `weightPlayerTargeting` | float | 4f |  |  |
| Focus score weights (§5.4) | `weightPlayerFacing` | float | 3f |  |  |
| Focus score weights (§5.4) | `facingConeFull` | float | 20f | 0f–90f | Full facing score inside this half-angle of the player's facing / camera forward… |
| Focus score weights (§5.4) | `facingConeZero` | float | 60f | 1f–180f | …falling to zero at this half-angle. |
| Focus score weights (§5.4) | `weightPositioning` | float | 2f |  |  |
| Focus score weights (§5.4) | `positioningRange` | float | 6f | 1f–20f | Positioning score reaches zero at this distance. |
| Focus score weights (§5.4) | `weightWaitPerSecond` | float | 0.25f |  | Fairness: score per second since this enemy last held the token. |
| Focus score weights (§5.4) | `weightWaitMax` | float | 2f |  |  |
| Focus score weights (§5.4) | `weightPunishPriority` | float | 3f |  | Bonus while the punish rule is armed on this enemy (3 hits in 4 s). |
| Engager range hysteresis (× effective attack range) | `engageInFactor` | float | 1f | 0.5f–1.5f |  |
| Engager range hysteresis (× effective attack range) | `engageOutFactor` | float | 1.35f | 1f–2.5f |  |
| Engager range hysteresis (× effective attack range) | `engageOutGrace` | float | 0.4f | 0f–2f | Must stay outside engageOutFactor this long before leaving Attack. |
| Engager range hysteresis (× effective attack range) | `creepStartFactor` | float | 0.95f | 0.5f–1.2f | Creep in only when farther than this × effective range… |
| Engager range hysteresis (× effective attack range) | `creepGrace` | float | 0.25f | 0f–2f | …for this long. |
| Engager range hysteresis (× effective attack range) | `creepSpeedFactor` | float | 0.62f | 0.2f–1.2f |  |
| Engager facing | `attackFacingConeHalfAngle` | float | 25f | 5f–90f | Swing only when the target is inside this half-angle (turn first, at most attackFacingMaxTurnSeconds). |
| Engager facing | `attackFacingMaxTurnSeconds` | float | 0.5f | 0f–2f |  |
| Engager facing | `engagerTurnRate` | float | 200f | 30f–720f |  |
| Engager facing | `nonAttackTurnRate` | float | 260f | 30f–720f | Turn rate (deg/s) while chasing / defending / searching with a combat target, i.e. outside the Attack state, so an enemy swings round to a player who moves behind it (180 deg in about 0.7 s). Only used while the engagement director is enabled; the legacy turnSpeed applies otherwise. |
| Engager facing | `lightTrackRate` | float | 120f | 0f–720f | Light swings track the target at this rate until trackUntil (normalized), then lock. |
| Engager facing | `lightTrackUntil` | float | 0.30f | 0f–1f |  |
| Engager facing | `chargedTrackRate` | float | 60f | 0f–720f |  |
| Engager facing | `chargedTrackUntil` | float | 0.28f | 0f–1f |  |
| Engager facing | `offscreenAttackExtraWindup` | float | 0.25f | 0f–2f | Extra wind-up when the Engager attacks from outside the camera view (fairness). |
| Cadence and responsiveness (Slice 0b) | `meleeRecoveryPauseMin` | float | 0.25f | 0f–3f | Extra wait (s) after a melee swing ends before the next one, random between Min and Max. Replaces the hard-coded EnemyCombat prefab values (were 0.6 - 1.8). |
| Cadence and responsiveness (Slice 0b) | `meleeRecoveryPauseMax` | float | 0.9f | 0f–3f |  |
| Cadence and responsiveness (Slice 0b) | `chaseStaminaPauseChance` | float | 0.15f | 0f–1f | Chance a chase stamina roll stops the enemy briefly (was 0.38 on the prefab). |
| Cadence and responsiveness (Slice 0b) | `chaseStaminaPauseMin` | float | 0.2f | 0f–2f | Chase pause length range in seconds (was 0.5 - 0.9 on the prefab). |
| Cadence and responsiveness (Slice 0b) | `chaseStaminaPauseMax` | float | 0.4f | 0f–2f |  |
| Cadence and responsiveness (Slice 0b) | `chaseStaminaPauseMinTargetDistance` | float | 6f | 0f–20f | The chase stamina pause only happens while the target is farther than this (m). 0 = allow at any range. |
| Cadence and responsiveness (Slice 0b) | `comboSecondHitChance` | float | 0.7f | 0f–1f | Chance a light attack continues into a 2nd swing (was 0.6 on the combo driver prefab). |
| Cadence and responsiveness (Slice 0b) | `comboThirdHitChance` | float | 0.55f | 0f–1f | Chance a two-swing chain continues into a 3rd swing (was 0.45). |
| Cadence and responsiveness (Slice 0b) | `turnRateClampMax` | float | 360f | 220f–720f | Upper clamp (deg/s) for the legacy turnSpeed -> degrees conversion on enemies (was hard-coded 220). 220 = old behaviour. |
| Cadence and responsiveness (Slice 0b) | `facingMoveScaleFloor` | float | 0.45f | 0.1f–1f | Lowest fraction of move speed while the enemy is turned away from its move direction (was hard-coded 0.18). 1 = never slows to turn. |
| Cadence and responsiveness (Slice 0b) | `lightSwingSpeedMultiplier` | float | 1.10f | 1f–1.3f | Animator speed multiplier during enemy LIGHT swings only (never the charged wind-up). 1 = off, 1.08-1.15 = snappier. |
| Enemy-vs-enemy crowding (applies to every enemy; prefab values are fallback only) | `applyCrowdingToAllEnemies` | bool | true |  | On = enemyAvoidanceRadius/Strength (and minCombatSeparation when > 0) below replace each enemy prefab's own values. Off = prefab values. A definition engagementProfileOverride still wins. |
| Enemy-vs-enemy crowding (applies to every enemy; prefab values are fallback only) | `enemyAvoidanceRadius` | float | 1.8f | 0.5f–4f | Enemies inside this radius (m) of another enemy get pushed apart while chasing / attacking. Prefab default was 1.45. |
| Enemy-vs-enemy crowding (applies to every enemy; prefab values are fallback only) | `enemyAvoidanceStrength` | float | 1.6f | 0f–4f | Push strength (m/s) of that separation. Prefab default was 0.85. |
| Enemy-vs-enemy crowding (applies to every enemy; prefab values are fallback only) | `waitingCrowdingStrength` | float | 1.2f | 0f–4f | Push (m/s) applied to enemies that are waiting (Hold / Holder) or standing between swings when another enemy overlaps them. Moving enemies keep using enemyAvoidanceStrength. 0 = waiting enemies are never pushed. |
| Enemy-vs-enemy crowding (applies to every enemy; prefab values are fallback only) | `crowdingDepthGain` | float | 1.5f | 0f–4f | The push grows with overlap depth: strength x (1 + gain x depth), depth 0 = at the radius edge, 1 = fully overlapping. 0 = fixed push like before; 1.5 = up to 2.5x when fully overlapping. |
| Enemy-vs-enemy crowding (applies to every enemy; prefab values are fallback only) | `minCombatSeparation` | float | 0f | 0f–3f | Closest an enemy tries to stand to its combat target (m). 0 = keep each prefab's own minCombatSeparation (0.95 on SwordMaster). |
| Enemy-vs-enemy crowding (applies to every enemy; prefab values are fallback only) | `crowdingVariationSpread` | float | 0.2f | 0f–0.5f | Per-enemy random variety: each enemy instance rolls a fixed -1..1 value at spawn; its back-off distance (min combat separation / ranged retreat) and crowding radius are scaled by 1 + spread * roll. 0.2 = +/-20%. 0 = every enemy identical. Per-type multiplier: EnemyDefinition.backoffDistanceMultiplier. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `enableEngagerCooldownMove` | bool | true |  | On = the engager (the one enemy allowed to swing) no longer stands still while its attack is on cooldown: the brain may roll a short slow strafe or a small backstep. Stays inside the melee band (see Max Distance Factor); never delays a swing; Phase 3 one-attacker spacing is untouched. Off = old standing-still behaviour. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerMoveChance` | float | 0.7f | 0f–1f | Chance per roll (brain decision) that a cooldown move starts. Multiplied by the brain personality sidestep weight. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerMoveRollInterval` | Vector2 | new Vector2(0.35f, 0.9f) |  | Seconds between brain rolls while the engager is on attack cooldown. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerMoveCooldown` | Vector2 | new Vector2(0.8f, 1.8f) |  | Seconds between the end of one cooldown move and the start of the next. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerMoveMinCooldown` | float | 0.3f | 0.1f–2f | A move only starts when the attack still needs at least this many seconds to become ready. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerMoveStopBeforeAttack` | float | 0.15f | 0f–1f | A running move ends when the attack is ready within this many seconds, so the engager faces and swings on time. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerStrafeSpeedFactor` | float | 0.95f | 0.2f–1.6f | Slow strafe: speed as a fraction of walk speed, duration (s) and the maximum arc (degrees) around the target per move. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerStrafeSeconds` | Vector2 | new Vector2(0.3f, 0.6f) |  |  |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerStrafeMaxArc` | float | 24f | 5f–60f |  |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerStrafeMaxNetArc` | float | 45f | 10f–120f | Anti-orbit: running total of strafed degrees (left minus right) is kept inside +/- this value; beyond it the next strafe flips side. The total halves at every move start and resets when the token is awarded. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerStrafeFlipChance` | float | 0.7f | 0f–1f | Chance the next strafe goes the opposite way from the previous one. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerStrafeMaxDistanceFactor` | float | 1.35f | 1f–2f | Cooldown moves only run while the target is within effective attack range x this (the melee band). |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerBackstepChance` | float | 0.3f | 0f–1f | Chance a cooldown move is a small backstep instead of a strafe. Never backs out past the creep-start distance. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerBackstepDistance` | Vector2 | new Vector2(0.35f, 0.8f) |  | Backstep distance (m) and speed as a fraction of walk speed. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `engagerBackstepSpeedFactor` | float | 1.3f | 0.3f–2f |  |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `moveVariationScale` | float | 1f | 0f–2f | How much of the per-enemy crowding variety (crowdingVariationSpread roll x definition backoffDistanceMultiplier) applies to cooldown move speed / distance / arc and to the post-hit backstep. 0 = every enemy identical, 1 = full variety. |
| Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings) | `moveEaseSeconds` | float | 0.08f | 0f–0.3f | Quick ease-in / ease-out time (s) of cooldown moves and holder sidesteps: the step starts at ~30% speed and reaches full speed in this time, and slows over the last quarter of the distance. 0 = constant speed. |
| Step animation (quick steps: strafe / backstep / holder sidestep / hit backstep) | `stepAnimSpeedScale` | float | 1.35f | 1f–2.5f | Leg-cycle exaggeration. While an enemy quick-steps, the locomotion blend (walk/strafe clip) is fed this x the real step speed, so the legs cycle faster than the root moves and the feet read as quick steps instead of a glide. 1 = feet matched exactly to ground speed. |
| Step animation (quick steps: strafe / backstep / holder sidestep / hit backstep) | `stepAnimMaxPlaybackBoost` | float | 1.3f | 1f–2f | When the step is faster than the fastest strafe / run clip, the animator playback speed is raised up to this multiplier (only while it is exactly 1 = no swing / hit stop / windup speed change running). 1 = never change playback speed. |
| Step animation (quick steps: strafe / backstep / holder sidestep / hit backstep) | `stepVelocitySmoothing` | float | 0.03f | 0.005f–0.1f | Smoothing time (s) of the measured root velocity that drives the locomotion clip during quick steps (normal 0.08 lags a 0.3-0.6 s step). |
| Step animation (quick steps: strafe / backstep / holder sidestep / hit backstep) | `stepAnimBlendOut` | float | 0.06f | 0f–0.3f | Blend-out (s) of the leg animation when a quick step ends: the locomotion animator inputs are pulled to 0 in this time (instead of Invector's slow input damping) so the legs stop with the body. 0 = snap to idle at the step end. |
| Holders (world-anchored hold points) | `holdRingRadius` | float | 4f | 1.5f–12f |  |
| Holders (world-anchored hold points) | `holdRingRadiusMin` | float | 3.2f | 1.5f–12f |  |
| Holders (world-anchored hold points) | `holdRingRadiusMax` | float | 5.5f | 1.5f–12f |  |
| Holders (world-anchored hold points) | `holdBandMin` | float | 3f | 1f–10f | Re-anchor if the holder stays outside [min, max] from the target… |
| Holders (world-anchored hold points) | `holdBandMax` | float | 6.5f | 2f–15f |  |
| Holders (world-anchored hold points) | `holdBandGrace` | float | 0.6f | 0f–3f | …for this long. |
| Holders (world-anchored hold points) | `holdReanchorPlayerMove` | float | 2f | 0.5f–10f | Re-anchor once the target has moved this far from the anchor origin. |
| Holders (world-anchored hold points) | `holdReanchorCooldown` | Vector2 | new Vector2(2.5f, 4f) |  | Random per-holder gap between re-anchors (seconds). |
| Holders (world-anchored hold points) | `holderMinSpacing` | float | 1.6f | 0.5f–5f |  |
| Holders (world-anchored hold points) | `holderEngagerLineHalfAngle` | float | 20f | 0f–60f | Holders keep out of a wedge this wide (half-angle) around the target→Engager line. |
| Holders (world-anchored hold points) | `holderWalkSpeed` | float | 1.4f | 0.5f–4f |  |
| Holders (world-anchored hold points) | `holderRunToAnchorDistance` | float | 3f | 1f–10f | Farther than this from the hold point: run there facing the move direction. |
| Holders (world-anchored hold points) | `holderTurnRate` | float | 90f | 10f–360f |  |
| Holders (world-anchored hold points) | `holderFacingDeadZone` | float | 35f | 0f–90f | Holder only turns its body when the target is more than this off its facing… |
| Holders (world-anchored hold points) | `holderFacingSettle` | float | 10f | 0f–45f | …and settles once within this. |
| Holder idle life (D5) | `holderActionInterval` | Vector2 | new Vector2(3.5f, 6f) |  | Each holder rolls one action every random interval in this range (seconds). |
| Holder idle life (D5) | `holderWeightSidestep` | float | 0.45f | 0f–1f |  |
| Holder idle life (D5) | `holderWeightTaunt` | float | 0.30f | 0f–1f |  |
| Holder idle life (D5) | `holderWeightFeint` | float | 0.15f | 0f–1f |  |
| Holder idle life (D5) | `holderWeightNone` | float | 0.10f | 0f–1f |  |
| Holder idle life (D5) | `sidestepDistance` | Vector2 | new Vector2(0.6f, 1.2f) |  |  |
| Holder idle life (D5) | `sidestepSpeed` | float | 3.4f | 0.8f–8f | Holder sidestep speed (m/s). Was holderWalkSpeed 1.4 (a slow glide); now its own value. Scaled per enemy by moveVariationScale. |
| Holder idle life (D5) | `sidestepDurationPad` | float | 0.2f | 0f–1f | Seconds added to distance / speed for the sidestep action length (was a fixed 0.6 s pad). Keep it short so the step stays snappy. |
| Holder idle life (D5) | `sidestepMaxArc` | float | 15f | 1f–45f |  |
| Holder idle life (D5) | `sidestepCooldown` | float | 4f | 0f–20f |  |
| Holder idle life (D5) | `tauntCooldown` | float | 6f | 0f–20f |  |
| Holder idle life (D5) | `tauntDuration` | Vector2 | new Vector2(1f, 2f) |  |  |
| Holder idle life (D5) | `feintDistance` | float | 0.5f | 0.1f–2f |  |
| Holder idle life (D5) | `feintMinDistance` | float | 3.5f | 0f–10f | Feints only happen when farther than this from the target. |
| Holder idle life (D5) | `holderGroupActionGap` | float | 1f | 0f–5f | Gap between any two holder actions on the same target. |
| Holder idle life (D5) | `holderMaxConcurrentActions` | int | 1 | 1–4 |  |
| Holder idle life (D5) | `holderMaxDriftDegPer10s` | float | 30f | 0f–180f | Anti-circling: net angular drift around the target per holder in any 10 s window. |
| Holder idle life (D5) | `noSidestepPlayerSpeed` | float | 1.5f | 0f–6f | No sidesteps while the target moves faster than this. |
| Holder idle life (D5) | `noSidestepAfterReanchor` | float | 2f | 0f–6f | No sidesteps for this long after a re-anchor. |
| Pools (designed now, used later) | `chargedTokensPerTarget` | int | 1 |  | Only the melee token holder may use the charged attack, so this is 1 by construction. |
| Pools (designed now, used later) | `rangedShotTokensPerTarget` | int | 2 |  | Future ranged enemies. Ranged enemies keep legacy behaviour in this build. |
| Swing start distance (first swing starts inside the weapon's real reach) | `swingStartUseWeaponReach` | bool | true |  | Master switch. The melee swing only starts when the target is inside the equipped weapon's real reach (instead of the generic attack range); the engager closes in first. Off = old behaviour (swing at the generic range). |
| Swing start distance (first swing starts inside the weapon's real reach) | `weaponReachArm` | float | 0.9f | 0f–2f | Metres from the enemy root to the weapon grip when the arm is extended in a swing. Weapon tip reach = this + the weapon's hitbox length. Calibrated on the Blade of Chaos swing animations. |
| Swing start distance (first swing starts inside the weapon's real reach) | `weaponReachArmMech` | float | 0.55f | 0f–2f | Same as Weapon Reach Arm but for Androids / Robots (non-Humanoid body types): their swing clips do not extend the arm as far, measured tip about 1.6-1.7 m with a 1.08 m sword (shakefix, was 0.9 for everyone). |
| Swing start distance (first swing starts inside the weapon's real reach) | `weaponReachArmRobot` | float | 0.55f | 0f–2f | Arm extension for Robots / machines (their own swing reach). Tip reach = this + weapon blade length. |
| Swing start distance (first swing starts inside the weapon's real reach) | `pushSnapsLegs` | bool | true |  | Crowd / hard-separation pushes write the leg locomotion blend un-damped for 0.25 s so the legs cycle with the push instead of lagging behind it (no sliding). |
| Swing start distance (first swing starts inside the weapon's real reach) | `snapLegsToMovement` | bool | true |  | Write the leg blend un-damped whenever the body moves faster than the legs show (step / chase / hold moves). Fixes idle-leg sliding. |
| Swing start distance (first swing starts inside the weapon's real reach) | `unarmedTipReach` | float | 1.1f | 0.3f–2.5f | Tip reach (m from the root) of an unarmed enemy (fists / no melee weapon hitbox). |
| Swing start distance (first swing starts inside the weapon's real reach) | `swingStartInnerMargin` | float | 0.25f | -0.5f–1f | The swing starts when the target is closer than tip reach minus this margin (so the hit window lands on the body, not on its edge). Small tolerance: lower = starts later / closer. |
| Swing start distance (first swing starts inside the weapon's real reach) | `swingCutDepthFraction` | float | 0.2f | 0f–1f | Visual blade cut into the target body radius at swing start (0.2 = 20%; Anthony's window 10-30%). Gate = arm + ItemData.meleeBladeLength + radius*(1-fraction). |
| Swing start distance (first swing starts inside the weapon's real reach) | `swingGateAllAttackers` | bool | true |  | Apply the weapon-reach swing gate to every melee attacker, not only the token engager. |
| Swing start distance (first swing starts inside the weapon's real reach) | `swingStartLeadSeconds` | float | 0.2f | 0f–0.6f | A target moving away is compensated by this many seconds of its outward speed (the swing needs ~this long to connect), so a kiting player is not swung at from the edge. |
| Swing start distance (first swing starts inside the weapon's real reach) | `swingCloseInReadySeconds` | float | 0.5f | 0f–1.5f | An engager whose attack becomes ready within this many seconds closes in (creeps) until it is inside the swing start distance. |
| Token fairness (random but starvation-free rotation, focus bias, group pace) | `fairnessEnabled` | bool | true |  | Master switch. On: the token is awarded by a weighted random pick among eligible enemies (softmax of the focus score), a starvation bonus favours enemies that waited longest / got fewer turns, and bigger mobs attack at a slower pace. Off = old behaviour (highest score always wins). |
| Token fairness (random but starvation-free rotation, focus bias, group pace) | `fairnessTemperature` | float | 2.5f | 0.3f–10f | Randomness of the pick, in focus-score units. Small = the best score nearly always wins; large = almost uniform. The enemy the player targets / hit gets its focus-score bonus (weightPlayerTargeting / weightPlayerHit), so it is picked more often but the others still get turns. |
| Token fairness (random but starvation-free rotation, focus bias, group pace) | `starvationBonusPerSecond` | float | 0.6f | 0f–3f | Starvation: focus-score bonus per second since this enemy last held the token (or joined the ring). |
| Token fairness (random but starvation-free rotation, focus bias, group pace) | `starvationBonusMax` | float | 6f | 0f–20f | Cap of the time-based starvation bonus (focus-score units). |
| Token fairness (random but starvation-free rotation, focus bias, group pace) | `starvationBonusPerMissedTurn` | float | 1.5f | 0f–5f | Starvation: focus-score bonus for every token this enemy has had fewer than the ring's most-served enemy. |
| Token fairness (random but starvation-free rotation, focus bias, group pace) | `groupPaceStartCount` | int | 3 | 2–12 | Group pace: from this many enemies in the ring the attack pace slows (longer hand-off grace and recovery pause). |
| Token fairness (random but starvation-free rotation, focus bias, group pace) | `groupPaceExtraPerEnemy` | float | 0.25f | 0f–1f | Group pace: extra gap per enemy at or above the start count (0.25 = +25% gap per extra enemy). |
| Token fairness (random but starvation-free rotation, focus bias, group pace) | `groupPaceMaxMultiplier` | float | 2f | 1f–4f | Group pace: cap of the gap multiplier (1 = no slowing). Per enemy type: EnemyDefinition.attackPaceMultiplier. |
| Token fairness (random but starvation-free rotation, focus bias, group pace) | `fairnessFirstTurnBonus` | float | 8f | 0f–20f | Extra focus score for an enemy that has never had an attack turn yet (clipfix: nobody in a big mob waits for the first swing). |
| Token fairness (random but starvation-free rotation, focus bias, group pace) | `fairnessFirstTurnGuaranteed` | bool | false |  | Always on in Testing attack mode. On here = also in Gameplay: an enemy that never held the token goes before anyone's second turn. |
| Token fairness (random but starvation-free rotation, focus bias, group pace) | `fairnessLargeMobCount` | int | 4 | 2–12 | At or above this many enemies in the ring a token holder keeps the token for only ONE attack sequence before handing off. |
| Real weapon reach (clipfix): the generic attack range is capped to the equipped weapon's real reach | `capAttackRangeToWeaponReach` | bool | true |  | On = melee attack range = min(prefab attackRange, weapon tip reach + slack). Unarmed enemies (Humadroid, Robot fists) no longer swing from the generic 2.2 m. |
| Real weapon reach (clipfix): the generic attack range is capped to the equipped weapon's real reach | `attackRangeReachSlack` | float | 0.1f | 0f–1f |  |
| Hard enemy separation (clipfix): enemies never merge into the same spot | `hardEnemySeparation` | float | 0.95f | 0f–2f | Hard minimum distance between any two enemies (m, root to root). Overlap is pushed apart every frame (not during rolls / staggers / swings). 0 = off. |
| Hard enemy separation (clipfix): enemies never merge into the same spot | `hardSeparationMaxSpeed` | float | 3.5f | 0.5f–6f | Max speed (m/s) of the hard separation push. The push never goes below ~0.9 m/s while active so the legs animate. |

## Combat Director

**What it does:** Phase 4 start (§31 #4) — DM_CombatDirectorProfile (Resources/Combat): enable / rollback (off = Phase 3 one-attacker only), attack slots by enemy count (1v1 = 1, 2–4 = 1–2, 5–8 = 2–3, larger scales), intensity dial, hard max, slot rotation. Flank and morale are stub fields. Live in Play.

**Where:**
- Studio: `Genesis Studio / Combat / Combat Director` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Combat/DM_CombatDirectorProfile.asset`
- Script: `Assets/_Project/Scripts/AI/Brain/DM_CombatDirectorProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Attack mode (testing vs gameplay) | `attackMode` | DMCombatAttackMode | DMCombatAttackMode.Gameplay |  | Testing: one attacker per target and every enemy gets a first turn before anyone gets a second (audit / clip checks). Gameplay: random token picks, up to maxSimultaneousAttackers at once plus a random chance for extra attackers. |
| Attack mode (testing vs gameplay) | `extraAttackerChance` | float | 0.25f | 0f–1f | Gameplay mode: chance, rolled every extraAttackerRollSeconds per target, that extra enemies attack on top of the cap. |
| Attack mode (testing vs gameplay) | `extraAttackersMax` | int | 1 | 1–4 |  |
| Attack mode (testing vs gameplay) | `extraAttackerRollSeconds` | float | 6f | 1f–30f |  |
| Attack mode (testing vs gameplay) | `IsTestingMode` | bool | > ForceTestingMode \|\| attackMode == DMCombatAttackMode.Testing |  |  |
| Master / rollback | `enableCombatDirector` | bool | true |  | Off = Phase 3 only: exactly one melee Engager per target, everyone else Holds. Rollback switch for attack slots. |
| Master / rollback | `debugLogSlots` | bool | false |  | Logs slot awards / releases to the console (editor/dev builds). |
| Attack slots by enemy count (the token holder counts as slot 1) | `intensity` | float | 0.5f | 0f–1f | 0 = each band uses its Min slots, 1 = its Max slots. Encounter intensity dial (§10). Bands with Min = Max ignore it. |
| Attack slots by enemy count (the token holder counts as slot 1) | `smallGroupMaxEnemies` | int | 4 | 2–8 | 1v1 is always 1 attacker. Small band: 2 up to this many enemies. |
| Attack slots by enemy count (the token holder counts as slot 1) | `smallGroupSlotsMin` | int | 1 | 1–6 |  |
| Attack slots by enemy count (the token holder counts as slot 1) | `smallGroupSlotsMax` | int | 2 | 1–6 |  |
| Attack slots by enemy count (the token holder counts as slot 1) | `mediumGroupMaxEnemies` | int | 8 | 3–16 | Medium band: above the small band up to this many enemies. |
| Attack slots by enemy count (the token holder counts as slot 1) | `mediumGroupSlotsMin` | int | 2 | 1–8 |  |
| Attack slots by enemy count (the token holder counts as slot 1) | `mediumGroupSlotsMax` | int | 3 | 1–8 |  |
| Attack slots by enemy count (the token holder counts as slot 1) | `largeGroupBaseSlots` | int | 3 | 1–10 | Larger fights (above the medium band): this many slots at the first count above the medium band… |
| Attack slots by enemy count (the token holder counts as slot 1) | `largeGroupEnemiesPerExtraSlot` | int | 4 | 1–16 | …plus one extra slot for every this-many additional enemies. |
| Attack slots by enemy count (the token holder counts as slot 1) | `largeGroupSlotsMax` | int | 5 | 1–12 | Large band never exceeds this many slots. |
| Attack slots by enemy count (the token holder counts as slot 1) | `maxSimultaneousAttackers` | int | 5 | 1–12 | Gameplay mode cap: never more simultaneous melee attackers than this per target (before the random extra attackers). Testing mode always uses 1. |
| Slot rotation (secondary attackers) | `slotMinHoldSeconds` | float | 1.5f | 0f–10f | A secondary attacker keeps its slot at least this long unless it becomes ineligible. |
| Slot rotation (secondary attackers) | `slotHoldMaxSequences` | int | 2 | 1–10 | Secondary attacker yields its slot after this many attack sequences (if another enemy is waiting). |
| Slot rotation (secondary attackers) | `slotHoldMaxSeconds` | float | 5f | 1f–30f | Secondary attacker yields its slot after this long (if another enemy is waiting). |
| Slot rotation (secondary attackers) | `slotEntryGrace` | Vector2 | new Vector2(0.5f, 1f) |  | A newly slotted attacker waits a random time in this range before its first swing (tell). |
| Slot rotation (secondary attackers) | `minSecondsBetweenSlotChanges` | float | 0.6f | 0f–5f | At most one slot change per target in this window (anti-thrash). |
| Slot rotation (secondary attackers) | `slotReawardCooldown` | float | 1.5f | 0f–10f | A yielding attacker can't be re-slotted for this long (ignored when nobody else is eligible). |
| Flanking (stub — not wired yet) | `enableFlank` | bool | false |  |  |
| Flanking (stub — not wired yet) | `flankMaxConcurrent` | int | 1 | 0–4 |  |
| Flanking (stub — not wired yet) | `flankMinHoldSeconds` | float | 2f | 0f–10f |  |
| Flanking (stub — not wired yet) | `flankPersonalityBonus` | float | 0.25f | 0f–1f |  |
| Group morale (stub — not wired yet) | `enableMorale` | bool | false |  |  |
| Group morale (stub — not wired yet) | `startingMorale` | float | 1f | 0f–1f |  |
| Group morale (stub — not wired yet) | `retreatMoraleThreshold` | float | 0.35f | 0f–1f |  |
| Group morale (stub — not wired yet) | `leaderDeathMoraleDrop` | float | 0.4f | 0f–1f |  |
| Group morale (stub — not wired yet) | `casualtyMoraleDrop` | float | 0.1f | 0f–1f |  |
| Group morale (stub — not wired yet) | `gruesomeKillMoraleDrop` | float | 0.15f | 0f–1f |  |
| Group morale (stub — not wired yet) | `moraleRecoverPerSecond` | float | 0.02f | 0f–0.5f |  |

## Momentum & finishers

**What it does:** §31 #6 — Momentum meter, finisher selection, first finishers on the migrated enemy. Info only; no profile asset yet.

**Where:**
- Studio: `Genesis Studio / Combat / Momentum & finishers` (panel mode `CombatPlanPlaceholder`)
- Asset: `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md`

## Sick Stick & specials

**What it does:** §31 #7–8 — Sick Stick trigger/puke, specials, Overdrive. Info only; no profile asset yet.

**Where:**
- Studio: `Genesis Studio / Combat / Sick Stick & specials` (panel mode `CombatPlanPlaceholder`)
- Asset: `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md`

## Body & dismemberment

**What it does:** §31 #10 — body components, weak points, dismemberment, functional damage. Info only; no profile asset yet.

**Where:**
- Studio: `Genesis Studio / Combat / Body & dismemberment` (panel mode `CombatPlanPlaceholder`)
- Asset: `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md`

## Encounters & difficulty

**What it does:** §31 #15–16 — authored encounters, faction battles, difficulty (tokens / detection / poise, not health). Info only; no profile asset yet.

**Where:**
- Studio: `Genesis Studio / Combat / Encounters & difficulty` (panel mode `CombatPlanPlaceholder`)
- Asset: `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md`

