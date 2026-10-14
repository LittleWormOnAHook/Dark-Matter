# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-13 00:45:46 -> 2026-10-13 00:47:39 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 1; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit 91CBB6 game B162C4 gear 2F3271; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m53s

**1 enemies audited - 2 FAIL, 7 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 26 PASS, 2 FAIL, 13 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 42 change(s).

- NEW enemy|SwordMaster|int.counts = PASS
- NEW enemy|SwordMaster|int.early = PASS
- NEW enemy|SwordMaster|int.immunity = PASS
- NEW enemy|SwordMaster|int.recovery = PASS
- NEW enemy|SwordMaster|int.cadence = PASS
- NEW enemy|SwordMaster|int.moves = PASS
- NEW enemy|SwordMaster|int.block = PASS
- NEW enemy|SwordMaster|int.roll.byenemy = PASS
- NEW enemy|SwordMaster|int.roll.clips = FAIL
- NEW enemy|SwordMaster|int.roll = PASS
- NEW enemy|SwordMaster|int.dodgestrike = PASS
- NEW enemy|SwordMaster|int.swingreach.byenemy = WARN
- NEW enemy|SwordMaster|int.swingreach = PASS
- NEW enemy|SwordMaster|int.fairness = WARN
- NEW enemy|SwordMaster|int.playerhits = PASS
- NEW enemy|SwordMaster|int.hitfx = PASS
- NEW enemy|SwordMaster|int.steps = WARN
- NEW enemy|SwordMaster|int.token = PASS
- NEW enemy|SwordMaster|int.multi = PASS
- NEW enemy|SwordMaster|int.stack = WARN
- NEW enemy|SwordMaster|int.overlap = PASS
- NEW enemy|SwordMaster|int.camera = PASS
- NEW enemy|SwordMaster|int.bodyoverlap = FAIL
- NEW enemy|SwordMaster|int.animready = PASS
- NEW enemy|SwordMaster|int.dodge = PASS
- NEW enemy|SwordMaster|int.slide = WARN
- NEW enemy|SwordMaster|int.roll.diag = WARN
- NEW enemy|SwordMaster|int.shake.static = PASS
- NEW enemy|SwordMaster|int.shake = PASS
- NEW enemy|SwordMaster|int.playermotion = WARN
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

Wall time 113s (1.9 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| SwordMaster | enemy | 102.5 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 102.5 | 1809 | 57.4 | 1380.1 | 0.99 | 0.00 | 26.50 |
| enemy|SwordMaster | cleanup | 0.2 | 4 | 59.5 | 95.0 | 0.64 | 0.00 | 18.24 |

Whole run (ran units only): 103s over 1813 frames, avg frame 57.4 ms, physics 0.99 ms, render 0.00 ms, scripts 26.48 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 20 | FAIL | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | SwordMaster | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete | Inspect the measured value and cause on the detail row. |
| 2 | 20 | FAIL | Body overlap (capsules of enemies / player interpenetrating by more than 5 cm) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 3 | -5 | WARN | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | SwordMaster | an enemy swung from more than its weapon reach + 0.45 m | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | SwordMaster | an enemy attacked less than 40% as often as another, or never | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | SwordMaster | a step started but the enemy did not move (blocked / interrupted before the first frames) | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | No stacking / circling (multi-enemy) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | SwordMaster | code-driven displacement without a leg cycle | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | Roll diagnostics (clip cut by another transition / no dodge clip chosen) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 9 | -5 | WARN | Player motion smoothness (frame time, timeScale, animator speed, locomotion param jitter, root speed jitter, W key chatter) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 10 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 11 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 12 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 13 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 14 | -5 | WARN | No circling while unaware (patrol-anchor fix) on every enemy | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 15 | -5 | WARN | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

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
| Interrupts | Interrupts by reason (SwordMaster, 96s, 66 light + 11 charged swings) | PASS | 8 interrupt(s): early window x5, heavy hit vs light swing x2, chance x1; skipped: immunity x0, super-armor x0, committed x12; chance rolls that failed x2; heavy hits seen x11; poise-break stagger events x13; enemy swing sequences started 23; interrupt rate 35% |  |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 0 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 5 interrupt(s) followed by a new swing; shortest gap 6.24s; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 20.23s (min 2.25, max 55.32, n 18); end-to-next-start gap mean 19.86s (n 13); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 8, engager backsteps 1, holder sidesteps 16 in the run; other kinds 0; quick-step frames 746 (animator speed boosted in 221, max 1.30); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.30 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | PASS | blocked hits 11 (player recoiled 3), guards broken 3 (charged 3, poise 0) in the run |  |
| Interrupts | Dodge roll direction per enemy (never toward the player; Humadroid included) | PASS | SwordMaster (1): 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 2, mean moved 2.5m / SwordMaster (2): 4 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 2 / side 2, mean moved 2.0m / Humadroid: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 2, mean moved 2.4m / Robot: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 3, mean moved 2.6m / SwordMaster: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 2, mean moved 2.4m |  |
| Interrupts | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | FAIL | 16 rolls: forward Roll clip 0, back 5, left 8, right 3; animator state never seen 1; ended early (not complete) 1 [noclip] | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete |
| Interrupts | Dodge roll: direction vs displacement, movement completes with the clip | PASS | 16 started, 16 finished (reasons: complete x15, noclip x1); full distance (>=90% of plan) 15, short while complete 0; toward the player 0; back 5, side 11; mean moved 2.37m, mean animation 0.62s, mean elapsed 0.84s; per roll: left 2.6/2.6m anim 0.74s complete / back 2.2/2.2m anim 0.62s complete / left 2.6/2.6m anim 0.77s complete / right 2.6/2.6m anim 0.74s complete / back 2.2/2.2m anim 0.58s complete / left 2.5/2.6m anim 0.70s complete / left 2.6/2.6m anim 0.81s complete / right 2.6/2.6m anim 0.73s complete / right 2.6/2.6m anim 0.70s complete / back 2.2/2.2m anim 0.61s complete / back 0.8/2.2m anim 0.00s noclip / left 2.6/2.6m anim 0.63s complete / left 2.6/2.6m anim 0.16s complete / left 2.6/2.6m anim 0.69s complete / left 2.6/2.6m anim 0.85s complete / back 2.2/2.2m anim 0.59s complete |  |
| Interrupts | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | PASS | 3 dodge strike(s) (reasons: handover x3); mean weave 0.70m of 0.71m planned; max distance change to the target 0.24m (includes the player moving); without token 0; clip never played 0; swing started after the dodge 3x; overridden by a defensive roll 0 |  |
| Interrupts | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | WARN | SwordMaster: 5 swings, start mean 1.55 (max 1.72, first swing 1.57) vs weapon tip reach 1.69, gate 1.75 / Robot: 6 swings, start mean 1.53 (max 1.70, first swing 1.60) vs weapon tip reach 1.73, gate 1.75 / Humadroid: 5 swings, start mean 1.63 (max 1.74, first swing 1.71) vs weapon tip reach 2.30, gate 1.75 / SwordMaster (2): 4 swings, start mean 1.48 (max 1.57, first swing 1.46) vs weapon tip reach 2.19, gate 1.75 / SwordMaster (1): 3 swings, start mean 1.56 (max 1.65, first swing 1.65) vs weapon tip reach 1.15, gate 1.75 | an enemy swung from more than its weapon reach + 0.45 m |
| Interrupts | Swing-start distance vs weapon reach (first vs later swings) | PASS | first swing per enemy: 5 swings: start distance mean 1.60m (max 1.71), attack range 1.75, swing-start gate mean 1.75, weapon length 1.83m, weapon tip reach from root mean 2.02m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%), cut depth >=30% of torso radius 5/5 / later swings: 18 swings: start distance mean 1.54m (max 1.74), attack range 1.75, swing-start gate mean 1.75, weapon length 1.81m, weapon tip reach from root mean 1.80m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%), cut depth >=30% of torso radius 18/18 |  |
| Interrupts | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | WARN | SwordMaster: 12 swing seq, first swing 0.9s, tokens 5, token-eligible 98% of samples, Engager 13% / SwordMaster (1): 3 swing seq, first swing 19.8s, tokens 5, token-eligible 96% of samples, Engager 10% / SwordMaster (2): 4 swing seq, first swing 12.4s, tokens 4, token-eligible 94% of samples, Engager 14% / Humadroid: 5 swing seq, first swing 9.2s, tokens 5, token-eligible 95% of samples, Engager 19% / Robot: 6 swing seq, first swing 3.5s, tokens 6, token-eligible 92% of samples, Engager 16%; most/least swings 12/3 | an enemy attacked less than 40% as often as another, or never |
| Interrupts | Audit player: melee hits landed (needed for block / interrupt checks) | PASS | hits=SwordMaster:7,SwordMaster (1):9,SwordMaster (2):8,Humadroid:12,Robot:17 attackFrames=1185 minStamina=976 lightClicks=66 pressedFrames=0 flagsAtClick={isAttacking:51} armed=1 |  |
| Interrupts | Hit FX scaling (blood / coolant / sparks use one scaling path) | PASS | Blood: light x1.06 (n 17), heavy x1.43 (n 9), chain>=2 x1.23 (n 10) / SparksSmall: light x1.08 (n 13), heavy x1.46 (n 5), chain>=2 x1.21 (n 9) / Coolant: light x1.08 (n 2), heavy x1.42 (n 4), chain>=2 x1.30 (n 2) // per enemy: SwordMaster: Blood x7 / Robot: SparksSmall x16 / Humadroid: Coolant x6 / SwordMaster (2): Blood x8 / SwordMaster (1): Blood x8 / Robot(Clone): SparksSmall x2 / Humanoid(Clone): Blood x1 / Player_v7 Combat Variant: Blood x2 |  |
| Interrupts | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | WARN | Strafe x4 moved 0.30m of 0.90m planned (interrupted 2, attack-ready 2); HolderSidestep x16 moved 1.26m of 1.28m planned (complete 13, cancel 1, timeout 2); Backstep x1 moved 0.38m of 0.38m planned (complete 1); step animation end vs move end: n 50, mean lag 0 ms, max 0 ms, over 150 ms: 0; zero-displacement steps 2, short while complete 0 | a step started but the enemy did not move (blocked / interrupted before the first frames) |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: 6.5:Engager, 27.1:Holder, 63.9:Holder, 71.3:Holder, 75.7:Holder, 77.0:Holder, 78.8:Holder, 96.1:Holder (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
| Interrupts | Multi-enemy (5 x SwordMaster): token rotation / spacing | PASS | 100 role samples; max simultaneous Engagers 1; distinct enemies that held Engager 5/5; hand-offs 23 |  |
| Interrupts | No stacking / circling (multi-enemy) | WARN | min enemy-enemy distance 0.16m, frames with a pair under 0.7m: 11/1424; circling suspects (>720deg yaw, <3m net): none; pairs under 0.7m by state/role: Hold/Holder + Hold/Holder x9 / Chase/Engager + Defensive/Holder x2; path per enemy: SwordMaster 61m, SwordMaster (1) 69m, SwordMaster (2) 71m, Humadroid 76m, Robot 72m |  |
| Interrupts | Overlapping attack windows (one attacker at a time, all enemy types, swings still finishing) | PASS | windows=0 seconds=0.00 max=1 samples= |  |
| Interrupts | Camera lens vs player body (near plane inside a player collider = clip-through) | PASS | surfMin=1.83 inFrames=0 inWindows=0 closeFrames=0 of=1424 pivotMin=2.45 at= |  |
| Interrupts | Enemy block (natural part of the run; forced block in the last 15 s) | PASS | natural: blocked 3, guard broken 1, misses 45 (guardNotRaised x45) / forced: blocked/broken 10 of 10 player hits () |  |
| Interrupts | Body overlap (capsules of enemies / player interpenetrating by more than 5 cm) | FAIL | windows=1 frames=1 samples=89.9s SwordMaster+SwordMaster_(1) |  |
| Interrupts | Stiff pose (alive, not ragdolled, animator off / not initialised) | PASS | notReadyEnemies=0 detail= |  |
| Interrupts | Enemy dodges play a DM_Dodge clip (never forward roll, never a clipless slide) | PASS | played 16, skipped (animator not ready) 0, forward-roll 0, played without clip 0 / [DodgePlayed] SwordMaster (1) state=DM_DodgeLeft clipOk=1 // [DodgePlayed] SwordMaster (2) state=DM_DodgeBack clipOk=1 // [DodgePlayed] SwordMaster (1) state=DM_DodgeLeft clipOk=1 // [DodgePlayed] SwordMaster (2) state=DM_DodgeRight clipOk=1 // [DodgePlayed] SwordMaster (1) state=DM_DodgeBack clipOk=1 // [DodgePlayed] Humadroid state=DM_DodgeLeft clipOk=1 |  |
| Interrupts | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | WARN | 7 episodes: Robot x3 (blocked 2, stagger 2, attacking 0, quick 2, suppress 0, mean speed 1.26, clips Idle, movers ChaseMove[ChaseMove0.25/]/HitSlide[HitSlide0.21/]/HitSlide[HitSlide0.90/], ctrlMag 0.33, states Wander/Attack) / Robot(Clone) x3 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 0.74, clips Idle, movers ChaseMove[ChaseMove0.15/]/ChaseMove[ChaseMove0.18/]/ChaseMove[ChaseMove0.12/], ctrlMag 1.00, states Wander/Investigate) / corrupt_patrol_android(Clone) x1 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 0.93, clips Idle, movers ChaseMove[ChaseMove0.17/], ctrlMag 1.00, states Wander) | code-driven displacement without a leg cycle |
| Interrupts | Roll diagnostics (clip cut by another transition / no dodge clip chosen) | WARN | [RollClipLeft] SwordMaster (2) NOCLIP clip=back nowClip=AimedIdle enabled=1 inTransition=0 speed=1.00 state=Defensive attacking=0 stagger=0 |  |
| Interrupts | Player shake: static physics / animator setup (interpolation, update mode, rig colliders are triggers, nothing pushes the capsule) | PASS | rbInterp=Interpolate profileInterp=Interpolate rbKinematic=0 animatorUpdate=Fixed rootMotion=0 fixedDt=0.0167 autoSync=off(project setting; hitbox queries call SyncTransforms once per frame) playerNonTriggerColliders=38 rigNonTriggerColliders=0 rigRigidbodies=0 nonKinematicBoneRigidbodies=0 rigColliders=18 hitboxLayerCollidesWith= hitstopHold=0.30 freezesAttacker=True forwardDrift=0.12 |  |
| Interrupts | Player shake study (8 phases of 12 s: baseline, player not frozen, hit stop off, hitbox rig off, interpolation None, old hold speed 0.05, no forward drift, player only) | PASS | baseline: frames 1424 (attack 1185, holdFrames 64), posReversals/100f 1.0 (during attack 13, during hold 0), forwardReversals/100f 2.1, yawReversals/100f 26.5, cameraReversals/100f 6.7, speedJitter 0.64 m/s, audit-applied yaw 0 of 9660 deg, max step 0.408 m / playerNoFreeze: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / hitstopOff: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / hitboxRigOff: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / interpolationNone: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / oldHoldSpeed0.05: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / noForwardDrift: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / playerOnly: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m |  |
| Interrupts | Player motion smoothness (frame time, timeScale, animator speed, locomotion param jitter, root speed jitter, W key chatter) | WARN | frames=1424 meanDtMs=56.8 maxDtMs=400 hitchFrames=880 timeScaleFrames=0 animSpeedFrames=1185 animSpeedMin=0.30 wFlips=0 moveSamples=740 moveSpeedMean=1.17 moveSpeedStd=0.95 inputMagMaxStep=0.22 inputMagFlips=60 relHitchFrames=2 meanDtAttMs=56.1 meanDtIdleMs=60.2 animSpeedMax=1.47 hitStopFrames=18 meanDtHitStopMs=54.6 speedJumps=95 maxStepM=0.41 gc0=2 vsync=1 targetFps=-1 maxDeltaTime=0.10 hitches=0.1s 400ms attacking=0;0.2s 86ms attacking=0;0.3s 64ms attacking=0;0.4s 62ms attacking=1;0.5s 73ms attacking=1;0.5s 53ms attacking=1 |  |
| Interrupts | Hand distance (info only; minHand gave false positives, see int.animready for stiff poses) | INFO | SwordMaster minHand=0.11 clip=Idle_at_55.6s lowSamples=227/243 firstLowAt=1.7s clip=AimedRunForward/Back upperId=-99 moveSet=-99 attackId=1 weapon=Blade_Of_Chaos / SwordMaster (1) minHand=0.12 clip=AimedIdle_at_88.4s lowSamples=229/243 firstLowAt=0.6s clip=AimedWalk upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / SwordMaster (2) minHand=0.12 clip=AimedIdle_at_32.7s lowSamples=224/243 firstLowAt=0.6s clip=Idle upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / Humadroid minHand=0.12 clip=Idle_at_31.4s lowSamples=225/243 firstLowAt=0.6s clip=Idle upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword / Robot minHand=0.07 clip=Idle_at_35.4s lowSamples=221/243 firstLowAt=0.6s clip=Idle upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword |  |
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
