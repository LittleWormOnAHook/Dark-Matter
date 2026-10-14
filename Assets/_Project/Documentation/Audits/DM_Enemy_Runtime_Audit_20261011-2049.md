# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-11 20:47:12 -> 2026-10-11 20:49:07 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 3; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit EAD922 game D75B2F gear FB6DFE; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m54s

**1 enemies audited - 3 FAIL, 5 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 23 PASS, 3 FAIL, 11 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 38 change(s).

- NEW enemy|SwordMaster|int.counts = PASS
- NEW enemy|SwordMaster|int.early = PASS
- NEW enemy|SwordMaster|int.immunity = PASS
- NEW enemy|SwordMaster|int.recovery = PASS
- NEW enemy|SwordMaster|int.cadence = PASS
- NEW enemy|SwordMaster|int.moves = PASS
- NEW enemy|SwordMaster|int.block = PASS
- NEW enemy|SwordMaster|int.roll.byenemy = PASS
- NEW enemy|SwordMaster|int.roll.clips = FAIL
- NEW enemy|SwordMaster|int.roll = FAIL
- NEW enemy|SwordMaster|int.dodgestrike = PASS
- NEW enemy|SwordMaster|int.swingreach.byenemy = WARN
- NEW enemy|SwordMaster|int.swingreach = PASS
- NEW enemy|SwordMaster|int.fairness = PASS
- NEW enemy|SwordMaster|int.playerhits = PASS
- NEW enemy|SwordMaster|int.hitfx = PASS
- NEW enemy|SwordMaster|int.steps = PASS
- NEW enemy|SwordMaster|int.token = PASS
- NEW enemy|SwordMaster|int.multi = PASS
- NEW enemy|SwordMaster|int.stack = WARN
- NEW enemy|SwordMaster|int.overlap = PASS
- NEW enemy|SwordMaster|int.camera = PASS
- NEW enemy|SwordMaster|int.slide = WARN
- NEW enemy|SwordMaster|int.shake.static = PASS
- NEW enemy|SwordMaster|int.shake = PASS
- NEW enemy|SwordMaster|int.playermotion = WARN
- NEW enemy|SwordMaster|int.spawnpose = FAIL
- NEW enemy|SwordMaster|int.errors = PASS
- NEW enemy|SwordMaster|console = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.swish = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hitsound = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hotcross = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hiteffects = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.loadout = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.tuning = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.patrol = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.onedrawn = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.profiles = INFO

## Timing

Wall time 115s (1.9 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| SwordMaster | enemy | 100.6 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 100.6 | 2180 | 48.8 | 5865.2 | 0.83 | 0.00 | 19.46 |
| enemy|SwordMaster | cleanup | 0.2 | 4 | 52.3 | 60.9 | 0.58 | 0.00 | 14.76 |

Whole run (ran units only): 101s over 2184 frames, avg frame 48.8 ms, physics 0.83 ms, render 0.00 ms, scripts 19.45 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 55 | WARN | Console errors/warnings | SwordMaster | [Warning] Game object with animator is inactive | Fix the exception at the listed stack frame (null-guard or initialise the field); it repeats on every spawn. |
| 2 | 20 | FAIL | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | SwordMaster | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete | Inspect the measured value and cause on the detail row. |
| 3 | 20 | FAIL | Dodge roll: direction vs displacement, movement completes with the clip | SwordMaster | an enemy rolled toward the player, stopped short while the clip kept playing, or a roll never finished | Inspect the measured value and cause on the detail row. |
| 4 | 20 | FAIL | Spawn pose (idle hand distance in the first 4 s; under 0.35 = crossed arms) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | SwordMaster | an enemy swung from more than its weapon reach + 0.45 m | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | No stacking / circling (multi-enemy) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | SwordMaster | code-driven displacement without a leg cycle | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | Player motion smoothness (frame time, timeScale, animator speed, locomotion param jitter, root speed jitter, W key chatter) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 9 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 10 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 11 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 12 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 13 | -5 | WARN | No circling while unaware (patrol-anchor fix) on every enemy | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 14 | -5 | WARN | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| SwordMaster |  |  | - | - | - | - | - | - | - | - | - | warn |

## Weapons that deal NO damage (headline)

- none detected in this run.

## Scenarios (block/parry, movement, multi-enemy, player weapons, performance)

| Scenario | PASS | FAIL | warn | time |
|---|---|---|---|---|
| Regression of earlier fixes and tweaks | 2 | 0 | 6 | 0s |

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | WARN | 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s); weapon reverts recorded this run: 0 | no Hot Cross row ran (cached or scenario skipped) |
| Fix check | vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons) | PASS | code present: True; console errors across 0 weapon-matrix case(s): 0 |  |
| Fix check | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | WARN | code present: True; dual-slot override case: not run |  |
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 6 value(s) compared on 0 enemies |  |
| Fix check | No circling while unaware (patrol-anchor fix) on every enemy | WARN | 0 PASS / 0 FAIL of 0 enemies |  |
| Fix check | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | WARN | 0 PASS / 0 FAIL of 0 enemies |  |
| Fix check | Profile adjustments made by earlier audit passes | INFO | none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes |  |

## Per-enemy detail

### SwordMaster

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `` - melee `` / ranged `` - audited in 101s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Interrupts | Interrupts by reason (SwordMaster, 96s, 68 light + 11 charged swings) | PASS | 14 interrupt(s): early window x9, heavy hit vs light swing x2, poise break x3; skipped: immunity x0, super-armor x0, committed x6; chance rolls that failed x6; heavy hits seen x10; poise-break stagger events x19; enemy swing sequences started 33; interrupt rate 42% |  |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 0 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 12 interrupt(s) followed by a new swing; shortest gap 6.22s; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 12.67s (min 2.05, max 34.03, n 28); end-to-next-start gap mean 11.81s (n 16); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 5, engager backsteps 4, holder sidesteps 11 in the run; other kinds 0; quick-step frames 1395 (animator speed boosted in 294, max 1.30); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.14 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | PASS | blocked hits 1 (player recoiled 1), guards broken 0 (charged 0, poise 0) in the run |  |
| Interrupts | Dodge roll direction per enemy (never toward the player; Humadroid included) | PASS | AUDIT_W_int_5: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 3, mean moved 2.6m / AUDIT_W_int_2_Humadroid: 4 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 3, mean moved 2.5m / AUDIT_W_int_3_Robot: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 3, mean moved 2.0m / AUDIT_W_int: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 3, mean moved 1.7m / AUDIT_W_int_4: 1 rolls, toward the player 0, max dir/disp dot vs the player -1.00, back 1 / side 0, mean moved 2.2m |  |
| Interrupts | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | FAIL | 14 rolls: forward Roll clip 1, back 2, left 6, right 5; animator state never seen 1; ended early (not complete) 1 [noclip] | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete |
| Interrupts | Dodge roll: direction vs displacement, movement completes with the clip | FAIL | 15 started, 14 finished (reasons: complete x13, noclip x1); full distance (>=90% of plan) 12, short while complete 1; toward the player 0; back 2, side 12; mean moved 2.22m, mean animation 0.47s, mean elapsed 1.03s; per roll: left 2.6/2.6m anim 0.39s complete / left 2.6/2.6m anim 0.69s complete / left 2.6/2.6m anim 0.72s complete / left 2.6/2.6m anim 0.70s complete / right 0.7/2.6m anim 0.00s complete / left 2.7/2.6m anim 0.06s complete / right 2.6/2.6m anim 0.77s complete / right 2.6/2.6m anim 0.72s complete / back 2.2/2.2m anim 0.59s complete / left 2.6/2.6m anim 0.68s complete / right 2.6/2.6m anim 0.70s complete / back 2.2/2.2m anim 0.10s complete / right 2.6/2.6m anim 0.52s complete / roll 0.0/3.2m anim 0.00s noclip | an enemy rolled toward the player, stopped short while the clip kept playing, or a roll never finished |
| Interrupts | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | PASS | 7 dodge strike(s) (reasons: handover x7); mean weave 0.86m of 0.85m planned; max distance change to the target 0.58m (includes the player moving); without token 0; clip never played 0; swing started after the dodge 6x; overridden by a defensive roll 0 |  |
| Interrupts | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | WARN | AUDIT_W_int: 6 swings, start mean 1.75 (max 2.02, first swing 1.99) vs weapon tip reach 2.10, gate 2.06 / AUDIT_W_int_2_Humadroid: 6 swings, start mean 1.92 (max 2.07, first swing 1.90) vs weapon tip reach 2.30, gate 2.27 / AUDIT_W_int_5: 7 swings, start mean 1.76 (max 1.95, first swing 1.95) vs weapon tip reach 1.30, gate 2.03 / AUDIT_W_int_4: 4 swings, start mean 1.27 (max 1.78, first swing 1.05) vs weapon tip reach 1.94, gate 2.03 / AUDIT_W_int_3_Robot: 8 swings, start mean 1.51 (max 1.79, first swing 1.77) vs weapon tip reach 1.61, gate 1.82 | an enemy swung from more than its weapon reach + 0.45 m |
| Interrupts | Swing-start distance vs weapon reach (first vs later swings) | PASS | first swing per enemy: 5 swings: start distance mean 1.73m (max 1.99), attack range 2.25, swing-start gate mean 2.08, weapon length 1.56m, weapon tip reach from root mean 1.76m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) / later swings: 26 swings: start distance mean 1.65m (max 2.07), attack range 2.24, swing-start gate mean 2.02, weapon length 1.56m, weapon tip reach from root mean 1.82m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) |  |
| Interrupts | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | PASS | AUDIT_W_int: 6 swing seq, first swing 1.5s, tokens 7, token-eligible 78% of samples, Engager 17% / AUDIT_W_int_3_Robot: 8 swing seq, first swing 9.7s, tokens 7, token-eligible 77% of samples, Engager 19% / AUDIT_W_int_4: 4 swing seq, first swing 8.0s, tokens 7, token-eligible 77% of samples, Engager 14% / AUDIT_W_int_5: 7 swing seq, first swing 5.4s, tokens 8, token-eligible 76% of samples, Engager 12%; most/least swings 8/4 |  |
| Interrupts | Audit player: melee hits landed (needed for block / interrupt checks) | PASS | hits=AUDIT_W_int:13,AUDIT_W_int_2_Humadroid:6,AUDIT_W_int_3_Robot:10,AUDIT_W_int_4:8,AUDIT_W_int_5:12 attackFrames=1597 minStamina=980 lightClicks=68 pressedFrames=0 flagsAtClick={isAttacking:64} armed=1 |  |
| Interrupts | Hit FX scaling (blood / coolant / sparks use one scaling path) | PASS | Blood: light x1.07 (n 16), heavy x1.46 (n 16), chain>=2 x1.33 (n 12) / Coolant: light x1.03 (n 2), heavy x1.46 (n 4), chain>=2 x1.49 (n 2) / SparksSmall: light x1.08 (n 6), heavy x1.44 (n 4), chain>=2 x1.23 (n 4) // per enemy: AUDIT_W_int: Blood x13 / AUDIT_W_int_2_Humadroid: Coolant x6 / AUDIT_W_int_5: Blood x12 / AUDIT_W_int_3_Robot: SparksSmall x10 / AUDIT_W_int_4: Blood x7 |  |
| Interrupts | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | PASS | HolderSidestep x11 moved 1.34m of 1.28m planned (complete 7, timeout 3, cancel 1); Strafe x3 moved 1.10m of 1.00m planned (timeout 1, complete 2); Backstep x3 moved 0.72m of 0.65m planned (complete 3); step animation end vs move end: n 45, mean lag 0 ms, max 0 ms, over 150 ms: 0; zero-displacement steps 0, short while complete 0 |  |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: 11.0:Holder, 15.3:Holder, 25.4:Holder, 32.6:Engager, 36.8:Holder, 45.6:Holder, 51.5:Holder, 53.3:Engager, 54.3:Holder, 61.5:Engager, 62.1:Holder, 72.1:Holder (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
| Interrupts | Multi-enemy (5 x SwordMaster): token rotation / spacing | PASS | 115 role samples; max simultaneous Engagers 1; distinct enemies that held Engager 5/5; hand-offs 29 |  |
| Interrupts | No stacking / circling (multi-enemy) | WARN | min enemy-enemy distance 0.10m, frames with a pair under 0.7m: 22/1782; circling suspects (>720deg yaw, <3m net): none; pairs under 0.7m by state/role: Attack/Engager + Attack/None x16 / Attack/None + Defensive/Engager x5 / Attack/None + Chase/Engager x1; path per enemy: AUDIT_W_int 50m, AUDIT_W_int_2_Humadroid 51m, AUDIT_W_int_3_Robot 55m, AUDIT_W_int_4 53m, AUDIT_W_int_5 57m |  |
| Interrupts | Overlapping attack windows (one attacker at a time, all enemy types, swings still finishing) | PASS | windows=0 seconds=0.00 max=1 samples= |  |
| Interrupts | Camera distance to the player pivot during the fight (enemies never pull the lens into the player) | PASS | min=2.23 closeFrames=0 of=1782 |  |
| Interrupts | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | WARN | 7 episodes: AUDIT_W_int_5 x3 (blocked 1, stagger 1, attacking 0, quick 1, suppress 0, mean speed 1.64, clips Idle/AimedIdle, movers ChaseMove[ChaseMove0.43/]/HitSlide[HitSlide0.31/]/StepFlat[StepFlat0.34/], ctrlMag 0.67, states Wander/Attack/Hold) / AUDIT_W_int_4 x1 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 1.64, clips AimedIdle, movers StepFlat[StepFlat0.30/], ctrlMag 1.00, states Hold) / AUDIT_W_int x3 (blocked 3, stagger 3, attacking 0, quick 3, suppress 0, mean speed 1.13, clips AimedIdle/Idle, movers HitSlide[HitSlide0.02/]/HitSlide[HitSlide0.32/], ctrlMag 0.00, states Attack) | code-driven displacement without a leg cycle |
| Interrupts | Player shake: static physics / animator setup (interpolation, update mode, rig colliders are triggers, nothing pushes the capsule) | PASS | rbInterp=Interpolate profileInterp=Interpolate rbKinematic=0 animatorUpdate=Fixed rootMotion=0 fixedDt=0.0167 autoSync=False playerNonTriggerColliders=38 rigNonTriggerColliders=0 rigRigidbodies=0 nonKinematicBoneRigidbodies=0 rigColliders=18 hitboxLayerCollidesWith= hitstopHold=0.30 freezesAttacker=True forwardDrift=0.12 |  |
| Interrupts | Player shake study (8 phases of 12 s: baseline, player not frozen, hit stop off, hitbox rig off, interpolation None, old hold speed 0.05, no forward drift, player only) | PASS | baseline: frames 188 (attack 164, holdFrames 10), posReversals/100f 0.5 (during attack 1, during hold 0), forwardReversals/100f 1.1, yawReversals/100f 6.9, cameraReversals/100f 5.3, speedJitter 0.64 m/s, audit-applied yaw 2120 of 835 deg, max step 0.300 m / playerNoFreeze: frames 218 (attack 189, holdFrames 0), posReversals/100f 0.9 (during attack 2, during hold 0), forwardReversals/100f 0.5, yawReversals/100f 13.8, cameraReversals/100f 4.1, speedJitter 0.78 m/s, audit-applied yaw 2824 of 920 deg, max step 0.281 m / hitstopOff: frames 210 (attack 194, holdFrames 0), posReversals/100f 2.4 (during attack 5, during hold 0), forwardReversals/100f 1.0, yawReversals/100f 13.3, cameraReversals/100f 4.8, speedJitter 0.61 m/s, audit-applied yaw 3245 of 906 deg, max step 0.288 m / hitboxRigOff: frames 195 (attack 174, holdFrames 13), posReversals/100f 1.0 (during attack 2, during hold 0), forwardReversals/100f 0.5, yawReversals/100f 9.7, cameraReversals/100f 5.6, speedJitter 0.75 m/s, audit-applied yaw 1387 of 819 deg, max step 0.240 m / interpolationNone: frames 213 (attack 186, holdFrames 11), posReversals/100f 0.5 (during attack 1, during hold 0), forwardReversals/100f 0.5, yawReversals/100f 10.3, cameraReversals/100f 4.2, speedJitter 0.87 m/s, audit-applied yaw 1360 of 1017 deg, max step 0.361 m / oldHoldSpeed0.05: frames 208 (attack 190, holdFrames 9), posReversals/100f 3.8 (during attack 8, during hold 0), forwardReversals/100f 4.3, yawReversals/100f 10.6, cameraReversals/100f 7.2, speedJitter 0.54 m/s, audit-applied yaw 2524 of 1171 deg, max step 0.353 m / noForwardDrift: frames 216 (attack 192, holdFrames 13), posReversals/100f 0.9 (during attack 2, during hold 0), forwardReversals/100f 3.2, yawReversals/100f 19.9, cameraReversals/100f 5.6, speedJitter 0.55 m/s, audit-applied yaw 3621 of 1146 deg, max step 0.241 m / playerOnly: frames 334 (attack 308, holdFrames 0), posReversals/100f 0.9 (during attack 3, during hold 0), forwardReversals/100f 0.6, yawReversals/100f 0.0, cameraReversals/100f 4.8, speedJitter 0.62 m/s, audit-applied yaw 2611 of 58 deg, max step 0.248 m |  |
| Interrupts | Player motion smoothness (frame time, timeScale, animator speed, locomotion param jitter, root speed jitter, W key chatter) | WARN | frames=1782 meanDtMs=45.7 maxDtMs=104 hitchFrames=680 timeScaleFrames=0 animSpeedFrames=1597 animSpeedMin=0.05 wFlips=0 moveSamples=860 moveSpeedMean=1.21 moveSpeedStd=1.05 inputMagMaxStep=0.25 inputMagFlips=65 relHitchFrames=1 meanDtAttMs=45.5 meanDtIdleMs=47.1 animSpeedMax=1.47 hitStopFrames=20 meanDtHitStopMs=46.4 speedJumps=122 maxStepM=0.36 gc0=2 vsync=1 targetFps=-1 maxDeltaTime=0.10 hitches=0.1s 64ms attacking=0;0.1s 69ms attacking=0;0.4s 62ms attacking=1;0.4s 58ms attacking=1;0.5s 72ms attacking=1;0.7s 76ms attacking=1 |  |
| Interrupts | Spawn pose (idle hand distance in the first 4 s; under 0.35 = crossed arms) | FAIL | AUDIT_W_int minHand=0.07 clip=Idle_at_78.4s lowSamples=281/308 firstLowAt=1.4s clip=AimedIdle upperId=-99 moveSet=-99 attackId=1 weapon=Blade_Of_Chaos / AUDIT_W_int_2_Humadroid minHand=0.14 clip=Idle_at_60.7s lowSamples=148/272 firstLowAt=0.6s clip=Walk upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword / AUDIT_W_int_3_Robot minHand=0.08 clip=Walk_at_10.2s lowSamples=281/308 firstLowAt=0.6s clip=Walk upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword / AUDIT_W_int_4 minHand=0.12 clip=AimedIdle_at_71.0s lowSamples=292/308 firstLowAt=0.6s clip=Idle upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / AUDIT_W_int_5 minHand=0.13 clip=AimedIdle_at_83.7s lowSamples=283/308 firstLowAt=0.6s clip=AimedIdle upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos |  |
| Interrupts | Console errors during the run | PASS | 0 errors/exceptions |  |
| Console | Console errors/warnings | WARN | 0 errors, 2 warning(s) | [Warning] Game object with animator is inactive |

Console during run: 0 error(s), 2 warning(s):
- `[Warning] Game object with animator is inactive`
- `[Warning] Game object with animator is inactive`

## Manual checklist (things only a human can judge)

- [ ] Weapon pose: the drawn weapon sits in the hand correctly (grip, no floating/offset blade) for every enemy.
- [ ] Attack animations look right and the visual swing lines up with when damage lands (no hits from a long way off, no whiffs while visibly connecting).
- [ ] Ranged enemies: muzzle position/flash and tracer origin match the weapon; aim looks natural.
- [ ] Idle/patrol/walk/run animation quality; no foot sliding or crossed-arm idle.
- [ ] Hit-mark art (decals, glow) reads correctly on the body type; hit reactions/stagger feel right.
- [ ] Death: ragdoll/dissolve looks right, loot bag pickup works, no weapon left floating.
- [ ] Audio: swing, impact, shot, hurt, death sounds present and not doubled.
- [ ] Difficulty feel: time-to-notice, chase speed and attack cadence are fair against the measured numbers above.
- [ ] Check the Game view during a run for any menu/loading overlay still covering the screen.

_Generated by Tools > Dark Matter Genesis > Audit > Run Enemy Runtime Audit (editor-only)._
