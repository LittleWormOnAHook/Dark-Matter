# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-12 19:38:08 -> 2026-10-12 19:40:06 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 4; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit 28C61F game 428AA4 gear 065554; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was False). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m57s

**1 enemies audited - 2 FAIL, 6 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 26 PASS, 2 FAIL, 12 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 41 change(s).

- NEW enemy|SwordMaster|int.counts = WARN
- NEW enemy|SwordMaster|int.early = PASS
- NEW enemy|SwordMaster|int.immunity = PASS
- NEW enemy|SwordMaster|int.recovery = PASS
- NEW enemy|SwordMaster|int.cadence = PASS
- NEW enemy|SwordMaster|int.moves = PASS
- NEW enemy|SwordMaster|int.block = FAIL
- NEW enemy|SwordMaster|int.roll.byenemy = PASS
- NEW enemy|SwordMaster|int.roll.clips = PASS
- NEW enemy|SwordMaster|int.roll = PASS
- NEW enemy|SwordMaster|int.dodgestrike = PASS
- NEW enemy|SwordMaster|int.swingreach.byenemy = PASS
- NEW enemy|SwordMaster|int.swingreach = PASS
- NEW enemy|SwordMaster|int.fairness = WARN
- NEW enemy|SwordMaster|int.playerhits = WARN
- NEW enemy|SwordMaster|int.steps = WARN
- NEW enemy|SwordMaster|int.token = PASS
- NEW enemy|SwordMaster|int.multi = PASS
- NEW enemy|SwordMaster|int.stack = PASS
- NEW enemy|SwordMaster|int.overlap = PASS
- NEW enemy|SwordMaster|int.camera = FAIL
- NEW enemy|SwordMaster|int.bodyoverlap = PASS
- NEW enemy|SwordMaster|int.animready = PASS
- NEW enemy|SwordMaster|int.dodge = PASS
- NEW enemy|SwordMaster|int.slide = WARN
- NEW enemy|SwordMaster|int.roll.diag = PASS
- NEW enemy|SwordMaster|int.shake.static = PASS
- NEW enemy|SwordMaster|int.shake = PASS
- NEW enemy|SwordMaster|int.playermotion = PASS
- NEW enemy|SwordMaster|int.spawnpose = INFO
- NEW enemy|SwordMaster|int.errors = PASS
- NEW enemy|SwordMaster|console = PASS
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

Wall time 117s (2.0 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| SwordMaster | enemy | 102.9 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 102.9 | 2471 | 43.9 | 5647.5 | 0.69 | 0.00 | 17.66 |
| enemy|SwordMaster | cleanup | 0.2 | 6 | 35.8 | 50.0 | 0.38 | 0.00 | 11.44 |

Whole run (ran units only): 103s over 2477 frames, avg frame 43.9 ms, physics 0.69 ms, render 0.00 ms, scripts 17.65 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 20 | FAIL | Camera lens vs player body (near plane inside a player collider = clip-through) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 2 | 20 | FAIL | Enemy block (natural part of the run; forced block in the last 15 s) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 3 | -5 | WARN | Interrupts by reason (SwordMaster, 96s, 71 light + 11 charged swings) | SwordMaster | no interrupt fired: enemy never swung while the player hit it (check logs), or enableAttackInterrupts is off | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | Enemy block (brain Defend -> guard, blocked hits, player recoil) | SwordMaster | no enemy block landed (block needs a Defend decision + a player hit inside the block window) | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | SwordMaster | an enemy attacked less than 40% as often as another, or never | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | Audit player: melee hits landed (needed for block / interrupt checks) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | SwordMaster | a step started but the enemy did not move (blocked / interrupted before the first frames) | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | SwordMaster | code-driven displacement without a leg cycle | Inspect the measured value and cause on the detail row. |
| 9 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 10 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 11 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 12 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 13 | -5 | WARN | No circling while unaware (patrol-anchor fix) on every enemy | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 14 | -5 | WARN | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| SwordMaster |  |  | - | - | - | - | - | - | - | - | - | PASS |

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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `` - melee `` / ranged `` - audited in 103s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Interrupts | Interrupts by reason (SwordMaster, 96s, 71 light + 11 charged swings) | WARN | 0 interrupt(s): none; skipped: immunity x0, super-armor x0, committed x0; chance rolls that failed x0; heavy hits seen x0; poise-break stagger events x0; enemy swing sequences started 31; interrupt rate 0% | no interrupt fired: enemy never swung while the player hit it (check logs), or enableAttackInterrupts is off |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 0 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 0 interrupt(s) followed by a new swing; shortest gap n/a; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 15.68s (min 2.08, max 37.65, n 26); end-to-next-start gap mean 14.02s (n 26); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 9, engager backsteps 5, holder sidesteps 24 in the run; other kinds 0; quick-step frames 3093 (animator speed boosted in 408, max 1.30); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.10 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | WARN | blocked hits 0 (player recoiled 0), guards broken 0 (charged 0, poise 0) in the run | no enemy block landed (block needs a Defend decision + a player hit inside the block window) |
| Interrupts | Dodge roll direction per enemy (never toward the player; Humadroid included) | PASS | Robot: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 2, mean moved 2.5m / SwordMaster (2): 4 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 3, mean moved 2.5m / Humadroid: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 2, mean moved 2.5m / SwordMaster: 5 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 5, mean moved 2.6m |  |
| Interrupts | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | PASS | 15 rolls: forward Roll clip 0, back 3, left 3, right 9; animator state never seen 0; ended early (not complete) 0 [] | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete |
| Interrupts | Dodge roll: direction vs displacement, movement completes with the clip | PASS | 15 started, 15 finished (reasons: complete x15); full distance (>=90% of plan) 15, short while complete 0; toward the player 0; back 3, side 12; mean moved 2.51m, mean animation 0.67s, mean elapsed 0.74s; per roll: right 2.6/2.6m anim 0.69s complete / right 2.6/2.6m anim 0.70s complete / left 2.6/2.6m anim 0.72s complete / back 2.2/2.2m anim 0.55s complete / back 2.2/2.2m anim 0.53s complete / left 2.6/2.6m anim 0.69s complete / right 2.6/2.6m anim 0.70s complete / left 2.6/2.6m anim 0.73s complete / right 2.6/2.6m anim 0.69s complete / right 2.6/2.6m anim 0.69s complete / right 2.6/2.6m anim 0.71s complete / right 2.6/2.6m anim 0.72s complete / back 2.2/2.2m anim 0.54s complete / right 2.6/2.6m anim 0.70s complete / right 2.6/2.6m anim 0.70s complete |  |
| Interrupts | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | PASS | 3 dodge strike(s) (reasons: handover x3); mean weave 0.84m of 0.84m planned; max distance change to the target 0.06m (includes the player moving); without token 0; clip never played 0; swing started after the dodge 3x; overridden by a defensive roll 0 |  |
| Interrupts | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | PASS | SwordMaster: 6 swings, start mean 2.18 (max 2.26, first swing 2.26) vs weapon tip reach 2.23, gate 2.27 / Robot: 8 swings, start mean 1.72 (max 1.81, first swing 1.78) vs weapon tip reach 2.26, gate 1.82 / SwordMaster (1): 5 swings, start mean 2.17 (max 2.22, first swing 2.22) vs weapon tip reach 2.33, gate 2.27 / SwordMaster (2): 5 swings, start mean 2.17 (max 2.25, first swing 2.22) vs weapon tip reach 2.33, gate 2.27 / Humadroid: 7 swings, start mean 2.03 (max 2.25, first swing 2.16) vs weapon tip reach 2.72, gate 2.27 |  |
| Interrupts | Swing-start distance vs weapon reach (first vs later swings) | PASS | first swing per enemy: 5 swings: start distance mean 2.13m (max 2.26), attack range 2.25, swing-start gate mean 2.18, weapon length 1.83m, weapon tip reach from root mean 1.92m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%), cut depth >=30% of torso radius 5/5 / later swings: 26 swings: start distance mean 2.00m (max 2.25), attack range 2.24, swing-start gate mean 2.15, weapon length 1.82m, weapon tip reach from root mean 2.47m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%), cut depth >=30% of torso radius 26/26 |  |
| Interrupts | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | WARN | SwordMaster: 16 swing seq, first swing 0.3s, tokens 6, token-eligible 96% of samples, Engager 18% / SwordMaster (1): 5 swing seq, first swing 9.5s, tokens 5, token-eligible 100% of samples, Engager 15% / SwordMaster (2): 5 swing seq, first swing 12.0s, tokens 5, token-eligible 96% of samples, Engager 16% / Humadroid: 7 swing seq, first swing 15.3s, tokens 6, token-eligible 97% of samples, Engager 21% / Robot: 8 swing seq, first swing 3.7s, tokens 7, token-eligible 97% of samples, Engager 31%; most/least swings 16/5 | an enemy attacked less than 40% as often as another, or never |
| Interrupts | Audit player: melee hits landed (needed for block / interrupt checks) | WARN | hits=SwordMaster:0,SwordMaster (1):0,SwordMaster (2):0,Humadroid:0,Robot:0 attackFrames=0 minStamina=1000 lightClicks=71 pressedFrames=0 flagsAtClick={} armed=1 |  |
| Interrupts | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | WARN | HolderSidestep x24 moved 1.03m of 1.23m planned (complete 21, cancel 2, timeout 1); Backstep x5 moved 0.43m of 0.43m planned (complete 5); Strafe x3 moved 0.42m of 0.80m planned (attack-ready 1, interrupted 1, timeout 1); step animation end vs move end: n 46, mean lag 0 ms, max 0 ms, over 150 ms: 0; zero-displacement steps 1, short while complete 0 | a step started but the enemy did not move (blocked / interrupted before the first frames) |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: no interrupts (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
| Interrupts | Multi-enemy (5 x SwordMaster): token rotation / spacing | PASS | 131 role samples; max simultaneous Engagers 1; distinct enemies that held Engager 5/5; hand-offs 28 |  |
| Interrupts | No stacking / circling (multi-enemy) | PASS | min enemy-enemy distance 0.82m, frames with a pair under 0.7m: 0/2054; circling suspects (>720deg yaw, <3m net): none; pairs under 0.7m by state/role: none; path per enemy: SwordMaster 64m, SwordMaster (1) 44m, SwordMaster (2) 37m, Humadroid 52m, Robot 57m |  |
| Interrupts | Overlapping attack windows (one attacker at a time, all enemy types, swings still finishing) | PASS | windows=0 seconds=0.00 max=1 samples= |  |
| Interrupts | Camera lens vs player body (near plane inside a player collider = clip-through) | FAIL | surfMin=-0.07 inFrames=2054 inWindows=1 closeFrames=2054 of=2054 pivotMin=2.50 at=0.1s |  |
| Interrupts | Enemy block (natural part of the run; forced block in the last 15 s) | FAIL | natural: blocked 0, guard broken 0, misses 0 () / forced: blocked/broken 0 of 0 player hits () |  |
| Interrupts | Body overlap (capsules of enemies / player interpenetrating by more than 5 cm) | PASS | windows=0 frames=0 samples= |  |
| Interrupts | Stiff pose (alive, not ragdolled, animator off / not initialised) | PASS | notReadyEnemies=0 detail= |  |
| Interrupts | Enemy dodges play a DM_Dodge clip (never forward roll, never a clipless slide) | PASS | played 15, skipped (animator not ready) 0, forward-roll 0, played without clip 0 / [DodgePlayed] Robot state=DM_DodgeRight clipOk=1 // [DodgePlayed] SwordMaster (2) state=DM_DodgeRight clipOk=1 // [DodgePlayed] Humadroid state=DM_DodgeLeft clipOk=1 // [DodgePlayed] SwordMaster (2) state=DM_DodgeBack clipOk=1 // [DodgePlayed] Humadroid state=DM_DodgeBack clipOk=1 // [DodgePlayed] SwordMaster state=DM_DodgeLeft clipOk=1 |  |
| Interrupts | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | WARN | 1 episodes: Robot x1 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 0.74, clips Idle, movers ChaseMove[ChaseMove0.24/], ctrlMag 1.00, states Wander) | code-driven displacement without a leg cycle |
| Interrupts | Roll diagnostics (clip cut by another transition / no dodge clip chosen) | PASS | none |  |
| Interrupts | Player shake: static physics / animator setup (interpolation, update mode, rig colliders are triggers, nothing pushes the capsule) | PASS | rbInterp=Interpolate profileInterp=Interpolate rbKinematic=0 animatorUpdate=Fixed rootMotion=0 fixedDt=0.0167 autoSync=off(project setting; hitbox queries call SyncTransforms once per frame) playerNonTriggerColliders=38 rigNonTriggerColliders=0 rigRigidbodies=0 nonKinematicBoneRigidbodies=0 rigColliders=18 hitboxLayerCollidesWith= hitstopHold=0.30 freezesAttacker=True forwardDrift=0.12 |  |
| Interrupts | Player shake study (8 phases of 12 s: baseline, player not frozen, hit stop off, hitbox rig off, interpolation None, old hold speed 0.05, no forward drift, player only) | PASS | baseline: frames 2054 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.3, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / playerNoFreeze: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / hitstopOff: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / hitboxRigOff: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / interpolationNone: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / oldHoldSpeed0.05: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / noForwardDrift: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / playerOnly: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m |  |
| Interrupts | Player motion smoothness (frame time, timeScale, animator speed, locomotion param jitter, root speed jitter, W key chatter) | PASS | frames=2054 meanDtMs=41.7 maxDtMs=2321 hitchFrames=300 timeScaleFrames=0 animSpeedFrames=0 animSpeedMin=1.00 wFlips=0 moveSamples=0 moveSpeedMean=0.00 moveSpeedStd=0.00 inputMagMaxStep=0.00 inputMagFlips=0 relHitchFrames=4 meanDtAttMs=0.0 meanDtIdleMs=41.7 animSpeedMax=1.00 hitStopFrames=0 meanDtHitStopMs=0.0 speedJumps=0 maxStepM=0.00 gc0=3 vsync=1 targetFps=-1 maxDeltaTime=0.10 hitches=0.1s 59ms attacking=0;0.1s 59ms attacking=0;0.4s 57ms attacking=0;0.5s 50ms attacking=0;0.9s 76ms attacking=0;1.3s 51ms attacking=0 |  |
| Interrupts | Hand distance (info only; minHand gave false positives, see int.animready for stiff poses) | INFO | SwordMaster minHand=0.07 clip=Idle_at_51.4s lowSamples=315/344 firstLowAt=0.5s clip=Idle upperId=-99 moveSet=-99 attackId=1 weapon=Blade_Of_Chaos / SwordMaster (1) minHand=0.12 clip=AimedWalk_at_39.5s lowSamples=323/344 firstLowAt=0.5s clip=Walk upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / SwordMaster (2) minHand=0.12 clip=AimedWalkRight_InPlace_at_42.1s lowSamples=312/344 firstLowAt=0.5s clip=WalkRight_InPlace upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / Humadroid minHand=0.11 clip=Idle_at_16.6s lowSamples=311/344 firstLowAt=0.5s clip=Walk upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword / Robot minHand=0.12 clip=Idle_at_67.4s lowSamples=298/344 firstLowAt=0.5s clip=Idle upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword |  |
| Interrupts | Console errors during the run | PASS | 0 errors/exceptions |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

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
