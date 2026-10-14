# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-11 22:58:24 -> 2026-10-11 23:00:33 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 0; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit B51D9D game 5001C5 gear FB6DFE; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 2m09s

**1 enemies audited - 2 FAIL, 8 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 22 PASS, 2 FAIL, 14 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 39 change(s).

- NEW enemy|SwordMaster|int.counts = PASS
- NEW enemy|SwordMaster|int.early = PASS
- NEW enemy|SwordMaster|int.immunity = PASS
- NEW enemy|SwordMaster|int.recovery = PASS
- NEW enemy|SwordMaster|int.cadence = PASS
- NEW enemy|SwordMaster|int.moves = PASS
- NEW enemy|SwordMaster|int.block = WARN
- NEW enemy|SwordMaster|int.roll.byenemy = PASS
- NEW enemy|SwordMaster|int.roll.clips = FAIL
- NEW enemy|SwordMaster|int.roll = PASS
- NEW enemy|SwordMaster|int.dodgestrike = PASS
- NEW enemy|SwordMaster|int.swingreach.byenemy = WARN
- NEW enemy|SwordMaster|int.swingreach = PASS
- NEW enemy|SwordMaster|int.fairness = WARN
- NEW enemy|SwordMaster|int.playerhits = PASS
- NEW enemy|SwordMaster|int.hitfx = PASS
- NEW enemy|SwordMaster|int.steps = PASS
- NEW enemy|SwordMaster|int.token = PASS
- NEW enemy|SwordMaster|int.multi = PASS
- NEW enemy|SwordMaster|int.stack = WARN
- NEW enemy|SwordMaster|int.overlap = PASS
- NEW enemy|SwordMaster|int.camera = PASS
- NEW enemy|SwordMaster|int.slide = WARN
- NEW enemy|SwordMaster|int.roll.diag = WARN
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

Wall time 130s (2.2 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| SwordMaster | enemy | 102.8 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 102.8 | 1356 | 86.8 | 14987.6 | 1.34 | 0.00 | 41.08 |
| enemy|SwordMaster | cleanup | 0.2 | 4 | 54.2 | 72.9 | 0.57 | 0.00 | 23.45 |

Whole run (ran units only): 103s over 1360 frames, avg frame 86.7 ms, physics 1.34 ms, render 0.00 ms, scripts 41.03 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 55 | WARN | Console errors/warnings | SwordMaster | [Warning] The referenced script on this Behaviour (Game Object '<null>') is missing! | Fix the exception at the listed stack frame (null-guard or initialise the field); it repeats on every spawn. |
| 2 | 20 | FAIL | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | SwordMaster | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete | Inspect the measured value and cause on the detail row. |
| 3 | 20 | FAIL | Spawn pose (idle hand distance in the first 4 s; under 0.35 = crossed arms) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | Enemy block (brain Defend -> guard, blocked hits, player recoil) | SwordMaster | no enemy block landed (block needs a Defend decision + a player hit inside the block window) | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | SwordMaster | an enemy swung from more than its weapon reach + 0.45 m | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | SwordMaster | an enemy attacked less than 40% as often as another, or never | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | No stacking / circling (multi-enemy) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | SwordMaster | code-driven displacement without a leg cycle | Inspect the measured value and cause on the detail row. |
| 9 | -5 | WARN | Roll diagnostics (clip cut by another transition / no dodge clip chosen) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 10 | -5 | WARN | Player motion smoothness (frame time, timeScale, animator speed, locomotion param jitter, root speed jitter, W key chatter) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 11 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 12 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 13 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 14 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 15 | -5 | WARN | No circling while unaware (patrol-anchor fix) on every enemy | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 16 | -5 | WARN | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `` - melee `` / ranged `` - audited in 103s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Interrupts | Interrupts by reason (SwordMaster, 96s, 61 light + 10 charged swings) | PASS | 10 interrupt(s): early window x10; skipped: immunity x0, super-armor x0, committed x7; chance rolls that failed x4; heavy hits seen x8; poise-break stagger events x13; enemy swing sequences started 33; interrupt rate 30% |  |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 0 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 8 interrupt(s) followed by a new swing; shortest gap 2.65s; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 12.36s (min 1.83, max 25.99, n 28); end-to-next-start gap mean 12.00s (n 17); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 3, engager backsteps 1, holder sidesteps 17 in the run; other kinds 0; quick-step frames 733 (animator speed boosted in 372, max 1.30); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.10 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | WARN | blocked hits 0 (player recoiled 0), guards broken 0 (charged 0, poise 0) in the run | no enemy block landed (block needs a Defend decision + a player hit inside the block window) |
| Interrupts | Dodge roll direction per enemy (never toward the player; Humadroid included) | PASS | SwordMaster (1): 4 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 4 / side 0, mean moved 1.6m / Robot: 2 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 2, mean moved 1.2m / SwordMaster (2): 4 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 3, mean moved 2.4m / Humadroid: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 2 / side 1, mean moved 1.6m / SwordMaster: 2 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 2, mean moved 1.3m |  |
| Interrupts | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | FAIL | 15 rolls: forward Roll clip 1, back 7, left 5, right 2; animator state never seen 3; ended early (not complete) 4 [noclip, noclip, noclip, clipleft] | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete |
| Interrupts | Dodge roll: direction vs displacement, movement completes with the clip | PASS | 16 started, 15 finished (reasons: complete x11, noclip x3, clipleft x1); full distance (>=90% of plan) 11, short while complete 0; toward the player 0; back 7, side 8; mean moved 1.73m, mean animation 1.29s, mean elapsed 1.59s; per roll: back 2.2/2.2m anim 0.59s complete / left 2.5/2.6m anim 0.68s complete / right 2.5/2.6m anim 0.75s complete / back 0.0/2.2m anim 0.00s noclip / left 2.6/2.6m anim 0.28s complete / back 2.2/2.2m anim 0.66s complete / back 2.1/2.2m anim 0.52s complete / right 2.5/2.6m anim 0.77s complete / left 2.6/2.6m anim 0.21s complete / back 2.2/2.2m anim 0.60s complete / back 0.0/2.2m anim 0.00s noclip / left 2.5/2.6m anim 0.77s complete / back 2.1/2.2m anim 0.58s complete / roll 0.0/3.2m anim 0.00s noclip / left 0.0/2.6m anim 12.93s clipleft |  |
| Interrupts | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | PASS | 3 dodge strike(s) (reasons: handover x3); mean weave 0.90m of 0.89m planned; max distance change to the target 0.58m (includes the player moving); without token 0; clip never played 0; swing started after the dodge 5x; overridden by a defensive roll 2 |  |
| Interrupts | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | WARN | Humadroid: 5 swings, start mean 1.79 (max 2.04, first swing 2.04) vs weapon tip reach 1.89, gate 2.27 / SwordMaster (1): 8 swings, start mean 1.69 (max 2.07, first swing 1.96) vs weapon tip reach 1.67, gate 2.27 / SwordMaster (2): 5 swings, start mean 1.89 (max 2.14, first swing 1.68) vs weapon tip reach 2.20, gate 2.27 / SwordMaster: 7 swings, start mean 1.76 (max 2.19, first swing 1.94) vs weapon tip reach 1.18, gate 2.27 / Robot: 7 swings, start mean 1.44 (max 1.73, first swing 1.52) vs weapon tip reach 0.96, gate 1.82 | an enemy swung from more than its weapon reach + 0.45 m |
| Interrupts | Swing-start distance vs weapon reach (first vs later swings) | PASS | first swing per enemy: 5 swings: start distance mean 1.83m (max 2.04), attack range 2.25, swing-start gate mean 2.18, weapon length 1.83m, weapon tip reach from root mean 2.23m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) / later swings: 27 swings: start distance mean 1.67m (max 2.19), attack range 2.25, swing-start gate mean 2.17, weapon length 1.81m, weapon tip reach from root mean 1.39m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) |  |
| Interrupts | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | WARN | SwordMaster: 20 swing seq, first swing 5.7s, tokens 6, token-eligible 78% of samples, Engager 9% / SwordMaster: 20 swing seq, first swing 5.7s, tokens 7, token-eligible 74% of samples, Engager 22% / SwordMaster: 20 swing seq, first swing 5.7s, tokens 7, token-eligible 71% of samples, Engager 25% / Humadroid: 5 swing seq, first swing 2.6s, tokens 6, token-eligible 78% of samples, Engager 12% / Robot: 7 swing seq, first swing 14.2s, tokens 7, token-eligible 75% of samples, Engager 12%; most/least swings 20/5 | an enemy attacked less than 40% as often as another, or never |
| Interrupts | Audit player: melee hits landed (needed for block / interrupt checks) | PASS | hits=SwordMaster:5,SwordMaster (1):10,SwordMaster (2):9,Humadroid:6,Robot:11 attackFrames=895 minStamina=980 lightClicks=61 pressedFrames=0 flagsAtClick={isAttacking:53} armed=1 |  |
| Interrupts | Hit FX scaling (blood / coolant / sparks use one scaling path) | PASS | Coolant: light x1.05 (n 6), heavy xn/a, chain>=2 x1.10 (n 1) / Blood: light x1.06 (n 11), heavy x1.43 (n 13), chain>=2 x1.28 (n 7) / SparksSmall: light x1.06 (n 7), heavy x1.44 (n 4), chain>=2 x1.20 (n 4) // per enemy: Humadroid: Coolant x6 / SwordMaster (1): Blood x10 / SwordMaster: Blood x5 / Robot: SparksSmall x11 / SwordMaster (2): Blood x9 |  |
| Interrupts | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | PASS | HolderSidestep x17 moved 1.26m of 1.37m planned (timeout 1, complete 16); Strafe x1 moved 0.86m of 0.88m planned (complete 1); Backstep x1 moved 0.34m of 0.34m planned (complete 1); step animation end vs move end: n 48, mean lag 0 ms, max 0 ms, over 150 ms: 0; zero-displacement steps 0, short while complete 0 |  |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: 27.5:Engager, 28.8:Holder, 30.7:Holder, 40.7:Holder, 46.8:Holder, 48.5:Holder, 56.4:Holder, 63.5:Engager, 64.3:Holder, 75.8:Engager, 77.6:Holder, 78.9:Engager (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
| Interrupts | Multi-enemy (5 x SwordMaster): token rotation / spacing | PASS | 65 role samples; max simultaneous Engagers 1; distinct enemies that held Engager 5/5; hand-offs 24 |  |
| Interrupts | No stacking / circling (multi-enemy) | WARN | min enemy-enemy distance 0.58m, frames with a pair under 0.7m: 2/1018; circling suspects (>720deg yaw, <3m net): none; pairs under 0.7m by state/role: Attack/Engager + Attack/Holder x1 / Defensive/Engager + Hold/Holder x1; path per enemy: SwordMaster 48m, SwordMaster (1) 49m, SwordMaster (2) 68m, Humadroid 51m, Robot 56m |  |
| Interrupts | Overlapping attack windows (one attacker at a time, all enemy types, swings still finishing) | PASS | windows=0 seconds=0.00 max=1 samples= |  |
| Interrupts | Camera distance to the player pivot during the fight (enemies never pull the lens into the player) | PASS | min=2.39 closeFrames=0 of=1018 |  |
| Interrupts | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | WARN | 24 episodes: SwordMaster x2 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 1.97, clips Idle/AimedIdle, movers ChaseMove[ChaseMove0.68/]/StepFlat[StepFlat0.37/], ctrlMag 1.00, states Wander/Hold) / SwordMaster (1) x5 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 1.70, clips Idle/AimedIdle, movers ChaseMove[ChaseMove0.42/]/StepFlat[StepFlat0.35/]/StepFlat[StepFlat0.34/]/StepFlat[StepFlat0.36/]/StepFlat[StepFlat0.37/], ctrlMag 1.00, states Wander/Hold) / SwordMaster (2) x2 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 1.28, clips Idle/AimedIdle, movers ChaseMove[ChaseMove0.27/]/StepFlat[StepFlat0.30/], ctrlMag 1.00, states Wander/Hold) / Humadroid x8 (blocked 1, stagger 1, attacking 0, quick 1, suppress 0, mean speed 1.60, clips Idle, movers ChaseMove[ChaseMove0.55/]/StepFlat[StepFlat0.37/]/StepFlat[StepFlat0.31/]/StepFlat[StepFlat0.38/]/HitSlide[HitSlide0.31/]/StepFlat[StepFlat0.33/]/ChaseMove[StepFlat0.14/ChaseMove0.21/], ctrlMag 0.88, states Patrol/Hold/Attack/Chase) / Robot x7 (blocked 1, stagger 1, attacking 0, quick 1, suppress 0, mean speed 1.90, clips Idle/AimedIdle, movers ChaseMove[ChaseMove0.20/]/ChaseMove[ChaseMove0.56/]/HitSlide[StepFlat0.18/HitSlide0.29/ChaseMove0.13/]/StepFlat[StepFlat0.30/]/HitSlide[HitSlide0.36/]/StepFlat[StepFlat0.32/]/ChaseMove[ChaseMove0.60/], ctrlMag 0.86, states Wander/Chase/Hold/Attack) | code-driven displacement without a leg cycle |
| Interrupts | Roll diagnostics (clip cut by another transition / no dodge clip chosen) | WARN | [RollFallback] SwordMaster no dodge clip chosen: backward=1 right=0 left=0 hasBack=0 hasLeft=0 hasRight=0 useBack=1 useSide=1 dotAway=1.00 dotRight=0.00 // [RollFallback] SwordMaster no dodge clip chosen: backward=0 right=1 left=0 hasBack=0 hasLeft=0 hasRight=0 useBack=1 useSide=1 dotAway=0.00 dotRight=1.00 // [RollClipLeft] Robot clip=left lastProgress=0.00 nowClip=Idle grounded=1 sliding=0 state=Wander attacking=0 stagger=0 |  |
| Interrupts | Player shake: static physics / animator setup (interpolation, update mode, rig colliders are triggers, nothing pushes the capsule) | PASS | rbInterp=Interpolate profileInterp=Interpolate rbKinematic=0 animatorUpdate=Fixed rootMotion=0 fixedDt=0.0167 autoSync=off(project setting; hitbox queries call SyncTransforms once per frame) playerNonTriggerColliders=38 rigNonTriggerColliders=0 rigRigidbodies=0 nonKinematicBoneRigidbodies=0 rigColliders=18 hitboxLayerCollidesWith= hitstopHold=0.30 freezesAttacker=True forwardDrift=0.12 |  |
| Interrupts | Player shake study (8 phases of 12 s: baseline, player not frozen, hit stop off, hitbox rig off, interpolation None, old hold speed 0.05, no forward drift, player only) | PASS | baseline: frames 115 (attack 101, holdFrames 6), posReversals/100f 3.5 (during attack 3, during hold 0), forwardReversals/100f 3.5, yawReversals/100f 3.5, cameraReversals/100f 7.8, speedJitter 0.48 m/s, audit-applied yaw 1307 of 500 deg, max step 0.253 m / playerNoFreeze: frames 118 (attack 100, holdFrames 0), posReversals/100f 2.5 (during attack 2, during hold 0), forwardReversals/100f 1.7, yawReversals/100f 16.9, cameraReversals/100f 9.3, speedJitter 1.14 m/s, audit-applied yaw 2465 of 941 deg, max step 0.503 m / hitstopOff: frames 115 (attack 96, holdFrames 0), posReversals/100f 3.5 (during attack 4, during hold 0), forwardReversals/100f 2.6, yawReversals/100f 7.8, cameraReversals/100f 6.1, speedJitter 0.92 m/s, audit-applied yaw 1562 of 732 deg, max step 0.499 m / hitboxRigOff: frames 125 (attack 114, holdFrames 4), posReversals/100f 4.8 (during attack 4, during hold 0), forwardReversals/100f 4.0, yawReversals/100f 16.8, cameraReversals/100f 4.0, speedJitter 0.87 m/s, audit-applied yaw 2744 of 1122 deg, max step 0.517 m / interpolationNone: frames 128 (attack 116, holdFrames 6), posReversals/100f 3.1 (during attack 4, during hold 0), forwardReversals/100f 3.9, yawReversals/100f 9.4, cameraReversals/100f 7.0, speedJitter 0.53 m/s, audit-applied yaw 1620 of 753 deg, max step 0.378 m / oldHoldSpeed0.05: frames 114 (attack 102, holdFrames 8), posReversals/100f 1.8 (during attack 2, during hold 0), forwardReversals/100f 1.8, yawReversals/100f 9.6, cameraReversals/100f 7.9, speedJitter 0.90 m/s, audit-applied yaw 875 of 443 deg, max step 0.316 m / noForwardDrift: frames 108 (attack 90, holdFrames 7), posReversals/100f 2.8 (during attack 3, during hold 0), forwardReversals/100f 2.8, yawReversals/100f 9.3, cameraReversals/100f 7.4, speedJitter 0.91 m/s, audit-applied yaw 1754 of 687 deg, max step 0.351 m / playerOnly: frames 195 (attack 176, holdFrames 0), posReversals/100f 1.5 (during attack 3, during hold 0), forwardReversals/100f 1.5, yawReversals/100f 1.0, cameraReversals/100f 10.8, speedJitter 0.68 m/s, audit-applied yaw 753 of 163 deg, max step 0.352 m |  |
| Interrupts | Player motion smoothness (frame time, timeScale, animator speed, locomotion param jitter, root speed jitter, W key chatter) | WARN | frames=1018 meanDtMs=75.2 maxDtMs=156 hitchFrames=913 timeScaleFrames=0 animSpeedFrames=895 animSpeedMin=0.05 wFlips=0 moveSamples=497 moveSpeedMean=1.29 moveSpeedStd=1.08 inputMagMaxStep=0.30 inputMagFlips=57 relHitchFrames=1 meanDtAttMs=74.5 meanDtIdleMs=80.1 animSpeedMax=1.47 hitStopFrames=8 meanDtHitStopMs=77.4 speedJumps=78 maxStepM=0.52 gc0=1 vsync=1 targetFps=-1 maxDeltaTime=0.10 hitches=0.1s 55ms attacking=0;0.1s 84ms attacking=0;0.4s 74ms attacking=0;0.5s 71ms attacking=1;0.5s 86ms attacking=1;0.6s 80ms attacking=1 |  |
| Interrupts | Spawn pose (idle hand distance in the first 4 s; under 0.35 = crossed arms) | FAIL | SwordMaster minHand=0.13 clip=AimedIdle_at_0.6s lowSamples=164/172 firstLowAt=0.1s clip=AimedWalk upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / SwordMaster (1) minHand=0.09 clip=Idle_at_25.6s lowSamples=157/172 firstLowAt=0.1s clip=AimedIdle upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / SwordMaster (2) minHand=0.13 clip=AimedIdle_at_45.2s lowSamples=158/172 firstLowAt=0.1s clip=Idle upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / Humadroid minHand=0.14 clip=Idle_at_77.9s lowSamples=126/172 firstLowAt=0.1s clip=Walk upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword / Robot minHand=0.10 clip=Idle_at_51.1s lowSamples=131/172 firstLowAt=0.1s clip=Idle upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword |  |
| Interrupts | Console errors during the run | PASS | 0 errors/exceptions |  |
| Console | Console errors/warnings | WARN | 0 errors, 3 warning(s) | [Warning] The referenced script on this Behaviour (Game Object '<null>') is missing! |

Console during run: 0 error(s), 3 warning(s):
- `[Warning] The referenced script on this Behaviour (Game Object '<null>') is missing!`
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
