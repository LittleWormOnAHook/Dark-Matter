# DM Player Hit Reactions Plan (Rev 2, IMPLEMENTED 2026-10-19 d)

Date: 2026-10-19. Owner: Anthony. Status: implemented with all defaults (uncommitted). See "Rev 2 implementation notes" at the end.
Goal: when an enemy hits the player, he reacts with the same directional, weighted, randomized feel as enemies do
in Slice 2b. Knockdowns are much rarer than on enemies.

Rules honoured:
- dark-matter-genesis-player-physics.mdc: no capsule (0.26 / 1.84), layer 8, rigidbody or physics retune; no root
  motion combined with code movement; no new character controller; no NavMesh.
- dark-matter-genesis-core.mdc, dm-naming-no-invector.mdc (all new names `DM*`).
- Reference: `Design/Combat/DM_Directional_Melee_Hit_Reactions_Plan.md` (Rev 3, enemy Slice 2b).

---

## 1. What exists (read-only findings)

**Player animator.** `Player_v7` uses `Assets/_Project/Animations/Player/Invector@ShooterMelee_Jetpack.controller`.
Its layers are Base, RightArm, LeftArm, OnlyArms, UpperBody, UnderBody, Shot (additive), FullBody and Climb. It already has
`TriggerReaction`, `ReactionID`, `HitDirection`, `ResetState` and `RecoilID`, which drive stock Invector reactions on FullBody.
This is a **different asset** from the enemy controller `Invector@ShooterMelee.controller`, which carries the Slice 2b layers.

**Incoming damage path.**
1. `PioneerShooterMeleeInput.OnReceiveAttack` (~L2383) handles block, parry and guard. It sets
   `damage.hitReaction = !successfulBlock`, then calls `cc.TakeDamage`.
2. `PioneerInvectorSurvivalBridge` forwards the damage to `SurvivalStats`, spawns the hit VFX, and then calls
   `TryApplyHitReactionChance` (~L225–260).
   - The roll uses the profile's `playerStaggerChanceMin/Max`, or `PioneerAnimationPlanSettings` at 10–25%.
   - It plays the stock `TriggerReaction` through `TriggerReactionDirect`, because the controller is `isImmortal`.
   - It gives stagger immunity (`playerStaggerImmunitySeconds`), costs stamina (`playerStaggerStaminaCost`), and
     respects `playerStaggerInterruptsCharge`.
   - When the roll fails, it suppresses the stock reaction.

**Already handled around it.**
- God mode and audit: `DMDevCommandState.BlocksDamage` (GodMode || AuditSessionActive). The VFX still spawns under god mode.
- Death: `PlayerDeathHandler` and `PioneerInvectorDeathRagdoll` (ragdoll, death menu, respawn, `PlayStandUpAfterRespawn`).
- Hitboxes: `DMPlayerHitboxRig` uses bone-following hit capsules, so a knocked-down body is hit where it lies.
- Camera shake: `TryShakeOnReaction` and `DMCombatCameraShake`.

**Reusable from Slice 2b:**
- `DMEnemyHitFlinch.PlayStrike` (spring lean, head/spine whip, hit-stop peak hold)
- `DMEnemyHitReactionPlayer` (additive torso layer, override body layer with arm-masked masks, variation picker that never repeats)
- `DMEnemyKnockdown` (Death fall → Dead hold → GetBackUp, FallTag mapping)
- `DMMeleeSwingVelocity` (blade-velocity strike direction)
- `DMHitReactionLayerBuilder` (editor Build / Validate)
- the 20 `DM_*` clips (PROTOFACTOR GetHitLight / Heavy / Death / GetUp)
- masks `DM_HitReact_Torso` / `DM_HitReact_Body`

## 2. Approach

Generalize the enemy runtime into a shared core and add thin player adapters. Do not copy the code.
- **`DMHitReactionCore`** (new, `Scripts/Combat/HitReactions/`). It holds the direction solve (blade velocity →
  hit-from vector → F/B/L/R and diagonals), the damage × weapon-weight intensity, the jitter / randomness, the no-repeat picker
  and the spring lean. The enemy classes call it, and their behaviour stays identical, which an Edit-mode validator checks.
- **`DMPlayerHitReaction`** (new, on `Player_v7 Variant`). It hooks the end of
  `PioneerInvectorSurvivalBridge.TryApplyHitReactionChance` (single entry, after the block, parry and god-mode decisions are
  known). It drives the player animator layers and the spring lean (bones are resolved through the Humanoid avatar).
- **`DMPlayerKnockdown`** (new). It runs the animation-only fall → down → get-up sequence with input and facing locks (§4).
- The stock Invector `TriggerReaction` path is retired for enemy hits. The DM body reaction replaces it, and the existing stagger
  chance and immunity fields carry over as the body-reaction gate.

## 3. Reaction tiers (per enemy hit that is not blocked)

| Tier | When | What plays | Root | Input |
|---|---|---|---|---|
| Lean (every hit) | always | Spring lean 10–14° (player default below enemy's 16°), fast attack + slow settle with overshoot, head/spine whip, twist sign random | 0 m | none |
| Light body | stagger roll (current 10–25%) | GetHitLight on additive torso layer (arm-masked → **grip and weapon pose kept**), weight 0.55–0.8, speed 0.9–1.15 | 0 m | attack input blocked 0.2 s |
| Heavy body | heavy weapon, crit, or damage ≥ 18% max HP, then the stagger roll | GetHitHeavy on override body layer (arms masked out), plus **push 0.25 m over 0.25 s** via motor velocity, plus a 35% chance of an extra stumble step | push only | attack/block locked 0.35 s, move ×0.4, no rotation lock |
| Knockdown | heavy tier only, roll vs **5%** (§5) | Death-fall clip by fall direction → Dead hold 0.6–1.0 s → GetBackUp, on the knockdown layer | 0 m (capsule stays) | full lock until get-up exit (§4) |

Intensity scaling is the same formula as on enemies: damage ÷ maxHealth and weapon weight (light / medium / heavy class
from the attacker's weapon), clamped to a `playerReactionIntensityMax` 0.85. That ceiling keeps the player readable.
The no-repeat picker is per tier, and so is the jitter (±12° angle, ±15% intensity, clip speed and weight variance).
Hit stop: the player-side hit stop is shorter (`playerHitStopScale` 0.6). The lean holds its peak during the hit stop.

## 4. Animator, input and locks

**Animator layers.** Add three DM layers to `Invector@ShooterMelee_Jetpack.controller`:
`DM_HitReact_Add` (additive, Torso mask), `DM_HitReact_Body` (override, Body mask) and `DM_Knockdown` (override, no mask).
Their speed params are `DM_HRAddSpeed`, `DM_HRBodySpeed` and `DM_KDSpeed`.
- They are built by extending `DMHitReactionLayerBuilder` with a "Player" target (Build / Validate menus).
- They are **appended after Climb**, so existing layer indices don't move and Invector resolves layers by name. Weights are
  script-driven: 0 at rest, and forced to 0 while climbing, on the jetpack, dead or ragdolled.
- This is an **animator asset edit, not a Player_v7 retune**. Capsule, layer 8, rigidbody and physics stay untouched, and
  `useRootMotion` stays 0. The knockdown clips play in place: the root is not applied, so no root motion is mixed with code.
- The masks are Humanoid, so the existing `DM_HitReact_*` masks work on the player rig. The builder's Validate confirms it.

**Input locks** are set through the DM flag, **not** `cc.customAction`. Invector's `ControlAnimatorRootMotion` snaps the
transform to the animator root whenever `customAction` or `lockAnimMovement` is set.
- Light: block attack input for 0.2 s.
- Heavy: attack and block locked for 0.35 s, movement ×0.4, and melee auto-face suspended.
- Knockdown:
  - `lockInput` and movement / rotation locks apply. Camera look stays free.
  - Auto-face, the strafe director and charge are suspended.
  - Dodge / roll can cancel the Dead hold after 0.4 s (`knockdownRollCancel`), with the get-up skipped via the roll.
  - The get-up grants i-frames for 0.6 s.

## 5. Knockdown frequency (player)

- `playerKnockdownChance` **0.05** (heavy tier only).
- `playerKnockdownChanceCap` **0.12** (after the low-HP / crit bonus of +0.04).
- `playerKnockdownCooldownSeconds` **20**.
- Never on two consecutive heavy hits.
- Never while blocking, rolling, dashing, climbing, on the jetpack or in the air.
- Damage while down is **×0.5** (`playerDownedDamageScale`), and there is no second knockdown while down.

Compare the enemy defaults: 15% knockdown with a 40% cap.

## 6. Interactions

- **Block / parry**: a successful block plays the guard impact only, with no reaction tiers. A guard break (`BreakPlayerGuard`)
  forces the heavy tier with no knockdown roll.
- **Dodge / roll / dash**: hits during i-frames do nothing. A roll start fades all layers out over 0.08 s.
- **God mode / audit (`BlocksDamage`)**: the lean and light tiers still play, for visual verification. Heavy push and knockdown
  are **off** under `BlocksDamage` by default (`reactionsUnderGodMode` = LeanAndLight), so audit runs stay deterministic.
  The Enemy Runtime Audit can toggle this.
- **Death**: if the hit is lethal, skip every tier and hand over to `PioneerInvectorDeathRagdoll`, the existing workflow.
  `DMPlayerKnockdown` zeroes its layers immediately on death. Respawn and save-load call `ResetForRespawn` on both
  new components (hooked in `PlayerDeathHandler.Respawn` / `ClearDeathPresentationForLoad`).
- **Camera**:
  - The existing reaction shake stays, scaled by tier.
  - An optional directional view kick (yaw/pitch 1.5° light, 3° heavy) is off by default.
  - On knockdown, the camera keeps following the capsule (unchanged), so there is no camera-target change. Optional
    `knockdownCameraLowerOffset` 0.
- **Melee facing / strafe**: suspend `DMMeleeBlockThreatFacing` and the strafe director during heavy and knockdown,
  to avoid the facing fight found in the jitter investigation.
- **Ranged hits**: there is no blade velocity, so the direction comes from projectile velocity, or attacker → player. Ranged hits use
  lean + light only unless the hit is a crit.
- **Shooter / ADS**: while aiming, the lean is scaled ×0.5 so aim stays usable. The arm-masked layers keep the gun on target via the existing aim IK.

## 7. Studio knobs (DM_CombatCoreProfile → new "Player hit reactions" section; Combat Studio + section filter)

| Field | Default |
|---|---|
| `enablePlayerHitReactions` | true |
| `playerLeanDegreesMin / Max` | 10 / 14 |
| `playerLeanAttackSeconds / SettleSeconds / Overshoot` | 0.05 / 0.45 / 0.25 |
| `playerHeadWhipScale` | 0.7 |
| `playerAngleJitterDegrees` | 12 |
| `playerIntensityJitter` | 0.15 |
| `playerReactionIntensityMax` | 0.85 |
| `playerClipSpeedRange` / `playerClipWeightRange` | 0.9–1.15 / 0.55–0.8 |
| `playerHeavyDamageFraction` | 0.18 of max HP |
| `playerHeavyPushMeters / Seconds` | 0.25 / 0.25 |
| `playerStumbleChance` | 0.35 |
| `playerLightAttackLockSeconds` | 0.2 |
| `playerHeavyLockSeconds` / `playerHeavyMoveScale` | 0.35 / 0.4 |
| `playerKnockdownChance / Cap / CooldownSeconds` | 0.05 / 0.12 / 20 |
| `playerDownHoldSeconds` | 0.6–1.0 |
| `playerDownedDamageScale` | 0.5 |
| `playerGetUpIFrameSeconds` | 0.6 |
| `knockdownRollCancel` / `knockdownRollCancelAfterSeconds` | true / 0.4 |
| `reactionsUnderGodMode` | LeanAndLight |
| `playerHitStopScale` | 0.6 |
| `playerAdsLeanScale` | 0.5 |
| `playerViewKickDegrees` | 0 (off) |

The existing `playerStaggerChanceMin/Max`, `playerStaggerImmunitySeconds`, `playerStaggerStaminaCost` and
`playerStaggerInterruptsCharge` are kept as the body-reaction gate.

## 8. Phases

- **P1, shared core.** Extract `DMHitReactionCore` from the enemy scripts with no behaviour change, and validate enemy Slice 2b in Edit mode.
- **P2, lean + light on the player.** Add `DMPlayerHitReaction`, the builder's Player target (layers on the Jetpack controller),
  the profile section and the bridge hook. Retire the stock TriggerReaction for enemy hits.
- **P3, heavy tier.** Push, stumble, locks, auto-face suspension, guard-break mapping.
- **P4, knockdown.** `DMPlayerKnockdown`, cooldown, roll-cancel, i-frames, death and respawn hooks, god mode / audit rules.
- **P5, Play pass with Anthony.** Melee, ranged, ADS, jetpack, climb, block/parry, death mid-reaction, respawn, save-load, audit run.

Each phase ends with Refresh at 0 errors / 0 warnings, `.pre<date>` backups in `rc_deploy\savepoint_20261011_combat_slice0\files\`,
and entries in both logs.

## 9. Risks

- **Two controllers.** The player and enemies use different `.controller` assets, so the builder must target each, and Validate
  must check both. The player controller is shared by all Player_v7 variants.
- **customAction / root snapping.** Using Invector `customAction` for locks would snap the transform to the animator root. The DM flags avoid this.
- **Facing fights.** Auto-face and Invector rotation already fight (see the jitter findings). Reactions must suspend auto-face, or they add more jitter.
- **Knockdown visual vs standing capsule.** Enemy AI may still path into a standing capsule while the body lies down. The
  bone hitboxes handle hits. Enemies should hold attacks on a downed player for `playerDownedAttackGrace` (0.5 s). That needs a small change in the AI engagement code.
- **Anthony's WIP** in `PioneerShooterMeleeInput.cs`. The hooks go in `PioneerInvectorSurvivalBridge` / the new scripts.
  Edits to `OnReceiveAttack` are needed only for the guard-break mapping, and are coordinated with him.

## 10. Decisions for Anthony (max 4)

1. **Knockdown rate:** 5% on heavy hits, 12% cap, 20 s cooldown (proposed)? Or lower or higher?
2. **Replace the stock Invector TriggerReaction** with the DM tiers for all enemy hits (proposed)? Or keep stock for ranged?
3. **While down:** damage ×0.5 + 0.6 s get-up i-frames + roll-cancel after 0.4 s (proposed)? Or full damage / no cancel?
4. **God mode / audit:** show lean + light only, with no push or knockdown (proposed)? Or play everything?

## Rev 2 implementation notes (2026-10-19 d)

- Files: Scripts/Combat/HitReactions/DMHitReactionCore.cs, Scripts/Player/Invector/DMPlayerHitReaction.cs, DMPlayerKnockdown.cs (all runtime-added, no prefab edits); hooks in PioneerInvectorSurvivalBridge; layers built on Invector@ShooterMelee_Jetpack.controller via DMHitReactionLayerBuilder (Build/Validate Player menus).
- Defaults as decided: knockdown heavy-only 5% (+4% crit / HP < 30%), cap 12%, cooldown 20 s; down hold 0.6-1.0 s; damage x0.5 while down; roll-cancel (Q / pad East) after 0.4 s; 0.6 s get-up invulnerability; god mode / audit = lean + light (playerReactionsUnderGodMode).
- Heavy = Invector big reaction id, crit, or damage >= 18% max HP; ranged (>3.5 m) hits never heavy unless crit. Push 0.25 m / 0.25 s (stumble x1.6 at 35%) with capsule-cast wall check and ledge ray.
- NOT implemented: playerHitStopScale and view kick (existing TryShakeOnReaction shake is reused instead).
- Enemy behaviour: shared math moved verbatim; seeded Edit-mode equivalence harness 0 mismatches.
- Open: heavy / knockdown path did not fire in the god-mode audit (HP stays full so no heavy hits); knockdown start + locks + layer weight verified by a forced Play probe; full fall/get-up timing on the player and roll-cancel need a hands-on check.
