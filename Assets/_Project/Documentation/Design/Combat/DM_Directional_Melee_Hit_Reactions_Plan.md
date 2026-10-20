# DM Directional Melee Hit Reactions - Plan (Slice 2b)

Date: Oct 19, 2026 (EDT). Status: **Rev 3 - IMPLEMENTED (2b.1-2b.3 + Edit-mode check), NOT play-tested, NOT committed** (see section 8). Author: Grok Bot for Anthony.
**Rev 2 (10:41 EDT, Anthony):** knockdown is **animation only** (no ragdoll). Ragdoll stays for **death only**. `EnemyInvectorRagdollBridge.cs` is **not modified** for this work (it holds Anthony's uncommitted changes). The crit-ragdoll knockdown is retired for player melee.
Scope: enemy reactions to **player melee** hits. Light = lean/stagger in the strike direction with **zero relocation**; heavy = small push + chance knockdown + get-up.
Primary clip source (Anthony): `Assets/PROTOFACTOR/Ultimate Animation Collection/Animations/1Handed Melee Weapon Animset/FBX Motions/`.

---
## 1. What already exists (disk truth)

| Area | Today | File / commit |
|---|---|---|
| Slice 2 hit reactions (B1-B4) | **Shipped Oct 11** (`616302884`). Light flinch + 0.08 m backstep, chain hit 2 stronger, hit 3 stumble, heavy = Invector Big reaction 0.7 s + 0.8 m slide + 20 deg waist bend, hit recovery 1.6 s, Big-reaction immunity 2.5 s. 37 fields in `DM_CombatCoreProfile` (Combat Studio Core tab). | `Scripts/Combat/DMEnemyHitReaction.cs`, `AI/Invector/DMEnemyHitFlinch.cs`, `AI/Invector/DMEnemyHitSlide.cs` |
| Swing direction | **Not measured.** "Side" comes from a per-combo-step table (`swingSideLightA` +1, `B` -1, `C` 0, `Charged` 0). Light flinch leans **away from the attacker**, not along the swing. Hit marks get `source -> hit point`, not blade velocity. | `DMEnemyHitReaction` L60-70, `PioneerInvectorDamageReceiver` ~L252 |
| Hit stop | Shipped Slice 1 (`DMMeleeHitStop`): local animator speed 0.3 for 2 / 6 frames (+2 crit) on the hit enemy and the player swing. No `Time.timeScale`. | `Scripts/Combat/DMMeleeHitStop.cs` |
| Interrupts / poise | `AttackInterruptEvaluator` runs first; `CombatPoise` (100, max 40% per hit); committed swings / `attackArmor` = bend only. | `AttackInterruptEvaluator.cs`, `CombatPoise.cs` |
| Knockdown | Only a **crit ragdoll** path (`HitKnockdownRoutine`: Invector `vRagdoll` + `StandUp@FromBack/FromBelly`, `knockdownDownSeconds` 1.65), reachable from the player-melee call `PioneerInvectorDamageReceiver` ~L161 (`TryHitStagger(..., isCritical, wantsHitStagger, ...)` -> `knockDown = !poiseBreak && criticalReactionKnocksDown && isCritical`). **To be retired for player melee** (see 3.5). | `EnemyInvectorRagdollBridge.cs` (**has uncommitted WIP - Anthony's; do not edit**) |
| Block | `DMEnemyBlock` (Slice 3): guard, reduced damage, guard break, sparks. | `Scripts/Combat/DMEnemyBlock.cs` |
| Attacker cap | `maxMeleeAttackers` 2 with 0.4-0.8 s staggered start (`DM_EnemyBrainProfile` / `DMEnemyEngagementDirector.CanStartAttackGlobal`). Interrupted engager keeps its token through recovery. | Brain / director |
| Turrets | Not involved: `DMEnemyHitReaction` only runs for `PioneerShooterMeleeInput` sources. Ranged / turret hits use separate paths. | - |

**Enemy animator:** all humanoid enemies except Gongo use Invector's tracked `Assets/Invector-3rdPersonController/Shooter/Animator/Invector@ShooterMelee.controller` (already extended by Slice 3 dodge states). Layers, in order:

| # | Layer | Mask | Blend | Holds |
|---|---|---|---|---|
| 0 | Base Layer | none | override | locomotion |
| 1-3 | RightArm / LeftArm / OnlyArms | arm masks | override | **weapon grip poses** (Defense etc.) |
| 4 | UpperBody | upper body | override | Aiming, EquipWeapon, **Small Hit Reaction** (Small_From_Front/Back/Left/Right) |
| 5 | UnderBody | legs | override | - |
| 6 | Shot | arms | **additive** | ShootRecoil |
| 7 | FullBody | **none** | override | **Big Hit Reaction** (Big_From_*), Hit Recoil, StandUpFromBack/Belly, Dead |

Today the FullBody Big reaction overrides the arms too, so it already stomps the weapon grip. The new design avoids that.

---
## 2. PROTOFACTOR clip inventory (measured in Unity, read-only)

All clips are **Humanoid** (animationType 3, avatar copied from `Protof-Actor@1hMeleeWeaponAnimset`), 30 fps, no events. They retarget onto any humanoid enemy avatar. "In place" = root XZ baked into pose. The naming means **the side the blow lands on** (verified from `_RM` root travel: HeavyLeft pushes the body to its right +0.95 m, HeavyRight pushes it left -1.20 m, HeavyFront pushes it back -0.57 m, HeavyBack pushes it forward +0.53 m).

| Clip (file `Humanoid@<name>.fbx`) | Use | Dir | Length | Root motion | Notes |
|---|---|---|---|---|---|
| GetHitLightFront1hMelee | medium | F | 0.80 s | in place (sway 0.12 m) | import `loopTime`=1 (must be off) |
| GetHitLightBack1hMelee | medium | B | 1.00 s | in place (0.20) | loop on |
| GetHitLightLeft1hMelee | medium | L | 1.00 s | in place (0.09) | loop on |
| GetHitLightRight1hMelee | medium | R | 1.00 s | in place (0.10) | loop on |
| GetHitHeavyFront1hMelee | heavy | F | 1.17 s | in place (0.08) | loop on |
| GetHitHeavyBack1hMelee | heavy | B | 1.50 s | in place (0.20) | loop on |
| GetHitHeavyLeft1hMelee | heavy | L | 1.67 s | in place (0.19) | loop on |
| GetHitHeavyRight1hMelee | heavy | R | 1.50 s | in place (0.20) | loop on |
| GetHitHeavy{Front,Back,Left,Right}1hMelee_RM | (not recommended) | F/B/L/R | same | **root motion** 0.57 / 0.53 / 0.95 / 1.23 m | Too far and uncapped. Use the in-place clip + `DMEnemyHitSlide` instead |
| DeathFront1hMelee | knockdown fall | F | 2.90 s | root 0.78 m | Death clip. Use only the fall part (~first 40%) then cross-fade. **Verify** which way it falls |
| DeathBack1hMelee | knockdown fall | B | 3.17 s | root 0.74 m | same |
| DeathLeft1hMelee | knockdown fall | L | 3.20 s | root 0.46 m | same |
| DeathRight1hMelee | knockdown fall | R | 2.50 s | root 0.87 m | same |
| DeadFront / DeadBack / DeadLeft / DeadRight | knockdown hold | F/B/L/R | 0.50 s loop | none | lying pose that matches the Death* end pose |
| GetBackUpFront | get-up | F | 2.67 s | root 0.84 m | pairs with DeadFront |
| GetBackUpBack | get-up | B | 3.20 s | root 0.73 m | pairs with DeadBack |
| GetBackUpLeft | get-up | L | 3.50 s | root 0.63 m | pairs with DeadLeft |
| GetBackUpRight | get-up | R | 3.00 s | root 0.96 m | pairs with DeadRight |
| Blocked1hMelee | (skip) | - | 0.67 s | - | **Generic** rig (type 2), not usable as is. Block visuals stay `DMEnemyBlock` |
| Falling1hMelee, Landing* | (skip) | - | - | - | not hit reactions |

There is **no dedicated knockdown clip**. The knockdown is built from Death* (fall) -> Dead* (hold) -> GetBackUp* (rise), which are authored as matching sets per direction.

**Arm motion (weapon-pose conflict):** every clip moves the arms more than the spine (muscle range: arms 0.4-1.1 for hits, 0.8-1.5 for death / get-up; spine/chest 0.1-0.56). All of them would impose the 1H-sword arm pose and fight a rifle, pistol or unarmed grip. **The arms and hands must be masked out** for every hit clip. Worst offenders: GetHitHeavyRight/Left (arms 0.9-1.1), all Death* and all GetBackUp* (hands push off the floor).

**Repo note:** `Assets/PROTOFACTOR/` is **gitignored** (Vendor_Install_Manifest: local only). Clips not referenced by any `_Project` asset yet. Plan copies the ~16 used clips into `_Project` as `.anim` (vendor policy "prefer a `_Project` slice"), with loop off and an additive reference pose, so fresh clones work and the vendor FBX import settings stay untouched.

---
## 3. Recommended approach

### 3.1 Strike direction (all tiers)
1. **Blade velocity (primary).** A small `DMMeleeSwingVelocity` sampler on the player weapon hitbox. It records the hitbox centre each LateUpdate **only while a swing's hit window is open** (hooked to `PioneerMeleeSwingHitDedupe` swing start/end; one Vector3 per frame, no allocations). At hit time `v = (pos - prevPos) / dt`.
2. Convert to the enemy's local frame: `x = dot(v, enemy.right)`, `z = dot(v, enemy.forward)`.
   - Lateral dominant: x > 0 (blade moving toward the enemy's right) -> blow lands on its **Left** -> lean / push **right** (strike direction). x < 0 -> **Right** clip, lean left.
   - Forward dominant: thrust into the enemy (z < 0) -> **Front**; attacker behind (dot(attacker - enemy, forward) < 0) -> **Back**. Overhead (mostly -Y) -> Front with extra forward bend.
   - Example from Anthony: a right-to-left swing by a player facing the enemy moves the blade toward the enemy's right -> "Left" reaction, torso pushed toward its right, in the strike direction.
3. **Fallback:** if |v| < `bladeVelocityMinSpeed` (2 m/s) or no sample, use the existing combo-step table (`swingSideLightA/B/C/Charged`). Existing fields keep their meaning.
4. Pass the result as `DamageInfo.SwingDirection` (world vector) + `HitSide` enum (Front/Back/Left/Right) so hit marks can also use it later.

### 3.2 Tiers

| Tier | Trigger (existing classification) | Visual | Movement | AI |
|---|---|---|---|---|
| **Light** | normal / crit hit, chain hit 1 | **Procedural only**: `DMEnemyHitFlinch` gets a spring lean (spine 40% / chest 40% / neck+head 20%) along the strike vector + twist, peak ~9 deg at 0.08 s, settle by 0.35 s; head lags slightly. Applied in LateUpdate after all layers, so it works on top of any grip (sword, rifle, pistol, unarmed) | **None.** Default `lightBackstepMeters` 0.08 -> **0** (the field already exists) | no pause (current rule kept) |
| **Medium** | chain hit 2, or crit with low poise | Directional `GetHitLight*` on the new **additive torso layer** (weight 0.7, speed 1.15) + a smaller procedural lean | none, or 0.05 m max (`mediumPushMeters` 0) | no pause; existing interrupt rules |
| **Heavy** | charged / strong hit, chain stumble, poise break | Directional `GetHitHeavy*` (in-place) on the **body layer** (hips, spine, head, legs; arms masked) | `DMEnemyHitSlide` push **0.35 m** over 0.3 s along the strike direction (was 0.8 m), with the existing wall / body / ledge stops | `ApplyHitRecovery` 1.6 s (existing) |
| **Knockdown** | heavy hit + roll passes (chance below) | **Animation only, no ragdoll.** `Death<Dir>` fall cut at the land point (~0.4 normalized, verified per clip) -> `Dead<Dir>` hold `knockdownDownSeconds` -> `GetBackUp<Dir>`, all matched by strike direction, played from `_Project` `.anim` copies. Full body **including arms** (hands must reach the floor) | 0.5 m push during the fall via `DMEnemyHitSlide`; clip root motion ignored (in-place copies) | AI paused, swing locked, optional slot release (Decision 3) |

Committed swings / `attackArmor` / super armor keep today's rule: **bend only** (procedural), no clip, no knockdown.

### 3.3 Keeping each enemy's weapon grip
- **New layer `DM_HitReact`** on the enemy controller, placed **after FullBody** (last), with two avatar masks created in `_Project/Animations/Masks/`:
  - `DM_HitReact_Torso`: Hips (rotation), Spine, Chest, UpperChest, Neck, Head. Arms, hands, fingers, hand IK **off**. **Additive** layer for the Medium tier.
  - `DM_HitReact_Body`: Torso + legs + feet, arms / hands still off. **Override** layer for the Heavy tier (weight curve from script: 0 -> 1 in 0.08 s, -> 0 over 0.2 s at the end).
  - Knockdown needs the arms (hands push off the floor). Mecanim cannot swap a layer's mask per state, so it gets a second layer `DM_KnockDown`: no mask, override, weight 1 only while down.
- Humanoid arm muscles are local to the chest, so masked arms keep the grip pose from the lower layers (RightArm / LeftArm / UpperBody / Shot) **and still follow the torso lean**. Rifle support-hand IK (Invector `vIKSolver`) keeps working during light / medium / heavy.
- Additive for Medium: the clip's delta from its own first frame is added on top of each enemy's stance, so a rifle crouch-stance and a sword stance both get the same flinch without the 1H idle pose leaking in.
- **Knockdown / get-up and the weapon:** the weapon stays parented to its hand bone exactly as placed on the prefab (**no placement changes, no re-parenting, no holstering**). For the duration: aim off, support-hand IK weight 0 (`vIKSolver`), shooter fire blocked, melee hitbox off. At the end of the get-up, aim and support IK **fade back in over 0.25 s**. A rifle may clip the floor briefly while lying; accepted (alternative: holster / un-holster is not recommended - it touches weapon handling).
- Invector's own `Big_From_*` / `Small_From_*` states stay for ranged and legacy paths. `DMEnemyHitReaction` stops calling `TryHitStagger` for player melee (it would double-play a full-body override that stomps the grip).

### 3.4 Interactions
- **Hit stop:** reactions start on the hit frame. The procedural lean runs on unscaled time with its spring sampled through the hit stop, so the lean "loads" during the freeze and releases after it. Clips start at normalized 0 and the 0.3 hold speed simply slows their first 2-6 frames (desired).
- **Interrupt evaluator / poise:** unchanged order (evaluator first). Poise break = Heavy tier (guaranteed) + knockdown roll with the low-poise bonus. A chance interrupt still uses the existing Small reaction rules.
- **Blocking:** a blocked hit gets `DMEnemyBlock`'s guard flinch only (no lean beyond it, no clip, no push, no knockdown). A guard break = Heavy tier, **no knockdown** on the same hit.
- **Attacker cap 2 / staggered start:** light / medium / heavy keep the token and slot through recovery (current rule). Knockdown: see Decision 4 (default: release the slot; the next attacker still waits its 0.4-0.8 s stagger).
- **Big-reaction immunity** (2.5 s) still caps chains. New `knockdownImmunitySeconds` 8 prevents juggling.
- **Dodge roll:** `DodgeRollBlocksHitReactions` early-out stays.
- **Death while down:** the knockdown component stops on the death event (layer weights to 0, IK/aim state restored), then death runs exactly as today (`PrepareForDeath` / corpse ragdoll - **ragdoll is used for death only**).
- **No NavMesh, root stays put:** light = transform untouched; heavy / knockdown push = `DMEnemyHitSlide` (transform, wall / body / ledge checks, `hitSlideMaxTotalMeters` cap). In-place clips only, so Invector root motion never moves the enemy.
- **Non-humanoid / unsupported rigs** (`!animator.isHuman`, Gongo, creatures, any enemy whose avatar fails the clip check): procedural lean + existing behaviour only.
- **Turrets / ranged:** untouched (player melee only).

### 3.5 Knockdown ownership (no ragdoll, bridge untouched)
- New runtime component **`DMEnemyKnockdown`** (runtime-added like `DMEnemyHitSlide`, no prefab edits) owns the whole sequence: chance roll result from `DMEnemyHitReaction`, `DM_KnockDown` layer weight, state play (`Fall_<Dir>` -> `Down_<Dir>` -> `GetUp_<Dir>`), push, AI pause (through `EnemyAiController` / `EnemyInvectorMotorBridge` public APIs and the existing public `bridge.ApplyHitRecovery`), IK / aim suspend and 0.25 s fade-in, immunity, i-frames, slot release, death abort. It exposes `IsDown`; `DMEnemyHitReaction` skips all reactions while `IsDown`.
- **No ragdoll calls** anywhere in the knockdown path (no `ActivateRagdoll`, no `vRagdoll` get-up, no `ReleaseForRagdoll`).
- **Retire crit-ragdoll knockdown for player melee without editing the bridge:** in `PioneerInvectorDamageReceiver` (~L161), the stagger call passes `isCritical: false` for **player melee** sources when `retireCritRagdollKnockdown` is on (crit damage, crit numbers and crit VFX unchanged). `DMEnemyHitReaction` stops calling `bridge.TryHitStagger` for player melee entirely (it already passed `weaponRequestsStagger: true`, which blocks the ragdoll branch). Ranged, poise (`CombatPoise` L143, `isCritical: false`) and death paths are unchanged.
- **Existing `EnemyInvectorRagdollBridge.cs` WIP:** not touched and not reverted. No `.pre20261019c` backup exists because no edit was ever made to that file in this work (verified: last write Oct 17 7:55 PM, diff +17/-2 is Anthony's).

---
## 4. Studio knobs (new `DM_CombatCoreProfile` section "Slice 2b directional reactions", Combat Studio Core tab, runtime `.Live`)

| Field | Default | Notes |
|---|---|---|
| `enableDirectionalReactionsV2` | true | master switch; off = Slice 2 behaviour |
| `swingDirectionSource` | BladeVelocity | BladeVelocity / ComboTable |
| `bladeVelocityMinSpeed` | 2.0 m/s | below = combo table fallback |
| `lightLeanDegrees` / `lightTwistDegrees` | 9 / 8 | |
| `lightLeanRiseSeconds` / `lightLeanSettleSeconds` | 0.08 / 0.35 | spring shape |
| `lightLeanSpineChestHeadWeights` | 0.4 / 0.4 / 0.2 | |
| `critLeanMultiplier` | 1.4 | |
| `lightBackstepMeters` (existing) | **0.08 -> 0** | zero relocation for light |
| `mediumUsesClip` / `mediumClipWeight` / `mediumClipSpeed` | true / 0.7 / 1.15 | GetHitLight* additive |
| `heavyClipWeight` / `heavyClipSpeed` | 1.0 / 1.2 | GetHitHeavy* body layer |
| `heavySlideMeters` (existing) | **0.8 -> 0.35** | "push a bit" |
| `heavySlideSeconds` (existing) | 0.35 -> 0.3 | |
| `reactionBlendInSeconds` / `reactionBlendOutSeconds` | 0.08 / 0.2 | layer weight curve |
| `knockdownEnabled` | true | |
| `knockdownChancePercent` | **15** | per eligible heavy hit |
| `knockdownLowPoiseBonusPercent` / `knockdownCritBonusPercent` | 15 / 10 | |
| `knockdownMaxChancePercent` | 40 | |
| `knockdownOnPoiseBreakPercent` | 25 | replaces base when poise breaks |
| `knockdownPushMeters` | 0.5 | |
| `knockdownFallExitNormalized` | 0.4 | cut point in the Death* clip |
| `knockdownDownSeconds` | 1.0 | lying hold before get-up |
| `getUpPlaybackSpeed` | 1.25 | GetBackUp clips are 2.7-3.5 s |
| `getUpInvulnerableSeconds` | 0.4 | last part of the get-up |
| `knockdownImmunitySeconds` | 8 | |
| `knockdownReleasesAttackSlot` | true | Decision 3 |
| `retireCritRagdollKnockdown` | true | player melee crits never take the ragdoll knockdown path |
| `knockdownFallExitNormalizedFront/Back/Left/Right` | 0.4 each | per-clip land point, set after the preview check |
| `logHitReactionsInPlay` | false | |

Per `EnemyDefinition`: `knockdownResistance` 0-1 (Grunt 0, Elite 0.5, Boss 1 = never), `allowDirectionalClips` (true). No prefab edits.

---
## 5. Phases (placement in the combat plan)

Add to `DMG_Combat_Plan_v2.md` / reference plan as **Slice 2b - Directional reactions v2**, after Slice 3 (shipped `4a2435455`), before Slice 4 (soft-lock). It does not block Phase 4 (Combat Director) and does not touch A31 #6-8.

| Phase | Content | Effort | Files (expected) |
|---|---|---|---|
| **2b.1** | Blade-velocity sampler, strike side in `DamageInfo`, procedural spring lean in strike direction, `lightBackstepMeters` 0, Studio section | S-M | new `Scripts/Combat/DMMeleeSwingVelocity.cs`; `PioneerMeleeSwingHitDedupe`, `DamageInfo`, `DMEnemyHitReaction`, `DMEnemyHitFlinch`, `DM_CombatCoreProfile`, `DMCombatStudioWindow` |
| **2b.2** | Copy 8 hit clips to `_Project/Animations/HitReactions/` (.anim, loop off, additive ref pose), 2 avatar masks, `DM_HitReact` layer + 4+4 directional states (Build And Apply style editor step with backups), heavy push 0.35 m | M | controller (Decision 2), new masks/clips, new `DMEnemyHitReactionPlayer` (plays the layer states; **no `EnemyInvectorRagdollBridge` edits**), `DMEnemyHitReaction` |
| **2b.3** | Animation-only knockdown: 12 clips (Death fall cut / Dead / GetBackUp x4) copied to `_Project` `.anim`, `DM_KnockDown` layer, `DMEnemyKnockdown`, chance roll, IK/aim suspend + 0.25 s fade-in, slot release, immunity, i-frames, death-while-down; retire crit-ragdoll knockdown for player melee | M | new `DMEnemyKnockdown`, `DMEnemyHitReaction`, `PioneerInvectorDamageReceiver` (crit flag on the stagger call only), `DMEnemyEngagementDirector` (slot release), `EnemyDefinition`, `DM_CombatCoreProfile` |
| **2b.4** | Edit-mode validator (clips present, humanoid, masks exclude arms, layer order) + Anthony play-test in v1.6.6 (sword, rifle, pistol, unarmed enemies) | S | editor validator |

Acceptance: light hits never move the enemy (position delta 0); lean visibly follows swing direction for A/B/C and charged; heavy push <= 0.35 m; knockdown about 1 in 7 heavy hits, always gets up; rifle / pistol / unarmed grips unchanged during light / medium / heavy; attacker cap 2 holds; 0 errors / 0 warnings.

---
## 6. Risks
1. **Arm-masked heavy clips** can look stiff (arms frozen while the body jerks). Mitigation: the procedural lean adds chest motion the arms follow; tune `heavyClipWeight`.
2. **Get-up with weapons:** hands push off the floor through a held rifle / sword. Accepted for v1 (weapon stays in hand, IK off). Alternative needs holstering (weapon handling change, not proposed).
3. **Death* direction / cut point** must be checked in the clip preview (naming is per landing side; the 0.4 exit point is an estimate).
4. **Double reactions:** Invector `Big_From_*` (FullBody) must no longer fire for player melee, or it overrides the arms again.
5. **Controller edits** on an Invector asset (Decision 2). `EnemyInvectorRagdollBridge.cs` (Anthony's uncommitted WIP) is deliberately left out of the design.
6. **Rigs:** Robot / Axe_Droid / Humadroid avatars must be humanoid with all spine bones; otherwise fall back to procedural only (validator checks this).
7. **Spacing:** 0.35 m pushes and knockdown slot release can open gaps in the hold ring; covered by existing D1 hand-off grace, but needs the play-test.
8. **Fresh clones:** PROTOFACTOR is gitignored; without the `_Project` copies the states would have missing clips.
9. **Lying pose vs ground:** with no ragdoll, slopes and steps are not conformed; the Dead* hold can float or clip on uneven ground. Mitigation: ground the hips with `EnemyGroundUtility` before the fall, and skip knockdown when the ground under the fall direction is steeper than ~20 deg or a ledge is within 1 m (heavy reaction plays instead).

---
## 7. Decisions for Anthony (4)
1. **Numbers:** knockdown chance **15%** of heavy hits (+15% low poise, +10% crit, cap 40%, 8 s immunity) and heavy push **0.35 m** (down from 0.8 m)? Light hits move **0 m**?
2. **Where the new layers go:** add `DM_HitReact` / `DM_KnockDown` to the tracked `Invector@ShooterMelee.controller` (precedent: Slice 3 dodge states) **or** a `_Project` copy of the controller assigned to enemies (cleaner, but swaps the controller reference on enemy instances at runtime / via definitions).
3. **Attack slot on knockdown:** release the attacker slot while down (default, the next enemy steps in after its 0.4-0.8 s stagger) **or** keep it (no one else attacks until the downed enemy gets up)?
4. **Hits while down:** downed enemies take damage but get no new reactions and the down time is not extended (default), **or** they are invulnerable until the get-up starts?

Resolved (Rev 2, Anthony): knockdown = PROTOFACTOR Death* fall (cut at the land point) -> Dead* hold -> GetBackUp*, matched by strike direction, copied to `_Project` `.anim`; no ragdoll; crit-ragdoll knockdown retired for player melee; ragdoll for death only.

---
## 8. Rev 3 - implementation (Oct 19, 2026, ~11:40 EDT)

**Decisions (final, Anthony):** 1) knockdown 15% of heavy-tier hits (+15% low poise, +10% crit, 25% on poise break, cap 40%, 8 s immunity), heavy push 0.35 m, light 0 m. 2) New layers on the tracked shared `Invector@ShooterMelee.controller`. 3) Downed enemy releases its token / attack slot. 4) Hits while down: damage only, no new reaction, down time not extended. Knockdown is animation only; ragdoll stays for death; `EnemyInvectorRagdollBridge.cs` untouched; crit-ragdoll knockdown retired for player melee.
**Feel pass (Anthony, 10:52 EDT):** hits must feel dramatic and weighted, with randomness - built into 2b.1 (below).

### What was built
- **2b.1 strike direction + weighted lean.** `DMMeleeSwingVelocity` (runtime-added by `PioneerMeleeDamageWindowTracker.Awake`) samples the blade (right weapon hitbox centres, else right hand) relative to the player root each LateUpdate; at the hit the sideways share `s` of the blade velocity across the enemy decides the lean: `dir = away*(1 - 0.7|s|) + side*0.7s`. Fallback = combo-step table. `DMEnemyHitFlinch.PlayStrike`: ease-out attack 0.045 s to the peak, **holds the peak through the hit-stop freeze**, then a damped spring (2.6 Hz, damping 0.42 = visible overshoot past neutral) over 0.6 s; head starts 35 ms late and swings 1.7x (whip); a new hit cross-fades from the one still settling. Light peak 16 deg, twist 12 deg, x damage weight `(dmg/25)^0.6` clamped 0.75-1.6, x1.35 heavy attack, x1.4 crit. Per hit: +/-14 deg direction jitter (kept apart from the previous hit), intensity 0.85-1.2, twist +/-4 deg with a 20% sign flip. Heavy hits add up to 2 hit-stop frames by weight. Light root motion 0 m.
- **2b.2 clips.** 20 PROTOFACTOR clips copied to `Assets/_Project/Animations/HitReactions/DM_*.anim` (vendor FBX untouched). Masks `Animations/Masks/DM_HitReact_Torso` (body + head) and `DM_HitReact_Body` (+ legs / feet); arms, hands, fingers, hand IK and root off. Three layers appended after FullBody (weight 0, driven by script): `DM_HitReact_Add` (additive, torso, 8 states), `DM_HitReact_Body` (override, body, 8 states), `DM_KnockDown` (override, no mask, 12 states); speed params `DM_HRAddSpeed`, `DM_HRBodySpeed`, `DM_KDSpeed`. Built by `Tools > Dark Matter > Combat > Build Hit Reaction Layers` (idempotent), checked by `Validate Hit Reaction Layers`. `DMEnemyHitReactionPlayer` plays them: medium (chain hit 2) = GetHitLight additive 0.75 (35% chance GetHitHeavy at 0.55), heavy = GetHitHeavy on the body layer 1.0 at 1.2x; speed 0.9-1.15x, weight 0.85-1.0, start offset 0-0.1, never the same variation twice in a row; AI locomotion held while the heavy body clip plays. Heavy push 0.35 m (+0.15 m stumble step, 30%). Player melee no longer calls the Invector Big reaction when the DM layers exist (fallback kept for other controllers).
- **2b.3 knockdown.** `DMEnemyKnockdown`: fall -> lying hold 1.0 s (+/-0.25) -> get-up 1.25x -> layer blends out over 0.25 s (aim fades back in). 0.5 m push during the fall. While down: AI paused, swing locked, melee hitboxes off, aim stance off, token / slot released, last 0.4 s of the get-up immune to player melee, no poise stagger. Death while down aborts and the normal death runs. Skipped on slopes > 20 deg or a ledge within 1 m. `EnemyDefinition.knockdownResistance` (0 default, 1 = never).
- **Correction to section 2:** PROTOFACTOR `Death*` / `Dead*` / `GetBackUp*` are named by the **fall direction** (DeathFront falls forward, measured from the body orientation curves), not the side struck. `DMEnemyKnockdown` maps struck Front -> falls Back, Back -> Front, Left -> Right, Right -> Left. Measured land points (body height near the floor): Front 0.75, Back 0.63, Left 0.45, Right 0.52 (the 0.4 estimate was too early for Front / Back). `GetHit*` naming (side struck) is confirmed by the in-place body sway.
- **Edit-mode check:** compile clean, validator OK (clips humanoid, loop flags, masks exclude arms/hands, layer order after FullBody, motions assigned, params), all 75 new profile fields resolve as serialized properties and are drawn in Combat Studio (Core tab, 6 new sections). Console 0 errors / 0 warnings after clear + refresh.

### Open issues
1. Clip size: the 20 `.anim` copies are ~68 MB of text YAML (dense FBX keys). Before committing consider Git LFS or trimming the Death* copies after the land point.
2. Saving the controller also wrote 4 Base Layer state-node positions that were changed in editor memory (layout only, no logic).
3. No enemy support-hand IK component exists in project code; "IK off" = aim stance / chest aim off while down, back in over the 0.25 s blend.
4. Weapon heft has no data field; reactions scale with damage (heavier weapons deal more).
5. `DamageInfo` not extended; the strike direction is read from the sampler at hit time.

Reference docs: `Design/Combat/DMG_Combat_Plan_v2.md` (v2.4), `combat_reference_plan.md` Slice 2 B1-B4 (box copy `/workspace/plans/`, not in the repo), commits `616302884` (Slice 2), `a6ce30d19` (Slice 0/1), `4a2435455` (Slice 3).
