# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-13 00:31:45 -> 2026-10-13 00:33:36 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 1; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit B12DDB game 15B0C8 gear 2F3271; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m50s

**1 enemies audited - 5 FAIL, 5 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 25 PASS, 5 FAIL, 11 warn.**

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
- NEW enemy|SwordMaster|int.roll.clips = PASS
- NEW enemy|SwordMaster|int.roll = FAIL
- NEW enemy|SwordMaster|int.dodgestrike = FAIL
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
- NEW enemy|SwordMaster|int.bodyoverlap = FAIL
- NEW enemy|SwordMaster|int.animready = PASS
- NEW enemy|SwordMaster|int.dodge = PASS
- NEW enemy|SwordMaster|int.slide = WARN
- NEW enemy|SwordMaster|int.roll.diag = PASS
- NEW enemy|SwordMaster|int.shake.static = PASS
- NEW enemy|SwordMaster|int.shake = PASS
- NEW enemy|SwordMaster|int.playermotion = WARN
- NEW enemy|SwordMaster|int.spawnpose = INFO
- NEW enemy|SwordMaster|int.errors = FAIL
- NEW enemy|SwordMaster|console = FAIL
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

Wall time 110s (1.8 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| SwordMaster | enemy | 101.2 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 101.2 | 1833 | 55.9 | 1432.7 | 0.99 | 0.00 | 25.66 |
| enemy|SwordMaster | cleanup | 0.2 | 5 | 46.3 | 69.4 | 0.54 | 0.00 | 17.91 |

Whole run (ran units only): 101s over 1838 frames, avg frame 55.9 ms, physics 0.98 ms, render 0.00 ms, scripts 25.64 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 80 | FAIL | Console errors/warnings | SwordMaster | [Exception] NullReferenceException: Object reference not set to an instance of an object @ BFX.BFX_ShaderProperies.Awake () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_ShaderProperies.cs:35) <- UnityEngine.Object:Instantiate(GameObject, Transform) <- Project.Combat.DMVolumetricBlood:Get(GameOb... | Fix the exception at the listed stack frame (null-guard or initialise the field); it repeats on every spawn. |
| 2 | 20 | FAIL | Dodge roll: direction vs displacement, movement completes with the clip | SwordMaster | an enemy rolled toward the player, stopped short while the clip kept playing, or a roll never finished | Inspect the measured value and cause on the detail row. |
| 3 | 20 | FAIL | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | SwordMaster | a dodge strike ran without the token, never played the clip, changed the distance by more than 0.5m, or no swing followed | Inspect the measured value and cause on the detail row. |
| 4 | 20 | FAIL | Body overlap (capsules of enemies / player interpenetrating by more than 5 cm) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 5 | 20 | FAIL | Console errors during the run | SwordMaster | see samples | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | SwordMaster | an enemy swung from more than its weapon reach + 0.45 m | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | SwordMaster | an enemy attacked less than 40% as often as another, or never | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | No stacking / circling (multi-enemy) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 9 | -5 | WARN | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | SwordMaster | code-driven displacement without a leg cycle | Inspect the measured value and cause on the detail row. |
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
| SwordMaster |  |  | - | - | - | - | - | - | - | - | - | **FAIL** |

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
| Interrupts | Interrupts by reason (SwordMaster, 96s, 66 light + 11 charged swings) | PASS | 12 interrupt(s): early window x6, chance x3, heavy hit vs light swing x1, poise break x2; skipped: immunity x0, super-armor x0, committed x12; chance rolls that failed x4; heavy hits seen x9; poise-break stagger events x18; enemy swing sequences started 35; interrupt rate 34% |  |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 0 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 11 interrupt(s) followed by a new swing; shortest gap 4.80s; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 11.47s (min 2.08, max 27.43, n 30); end-to-next-start gap mean 9.19s (n 17); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 5, engager backsteps 1, holder sidesteps 14 in the run; other kinds 0; quick-step frames 793 (animator speed boosted in 272, max 1.30); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.30 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | PASS | blocked hits 6 (player recoiled 3), guards broken 4 (charged 4, poise 0) in the run |  |
| Interrupts | Dodge roll direction per enemy (never toward the player; Humadroid included) | PASS | Humadroid: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 3, mean moved 2.0m / SwordMaster (1): 5 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 2 / side 3, mean moved 2.4m / Robot: 4 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 3 / side 1, mean moved 2.3m / SwordMaster (2): 4 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 2 / side 2, mean moved 2.4m / SwordMaster: 2 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 1, mean moved 2.3m |  |
| Interrupts | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | PASS | 18 rolls: forward Roll clip 0, back 8, left 7, right 3; animator state never seen 0; ended early (not complete) 0 [] | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete |
| Interrupts | Dodge roll: direction vs displacement, movement completes with the clip | FAIL | 19 started, 18 finished (reasons: complete x18); full distance (>=90% of plan) 17, short while complete 1; toward the player 0; back 8, side 10; mean moved 2.29m, mean animation 0.61s, mean elapsed 0.76s; per roll: left 0.8/2.6m anim 0.00s complete / left 2.6/2.6m anim 0.73s complete / right 2.6/2.6m anim 0.85s complete / back 2.2/2.2m anim 0.58s complete / left 2.6/2.6m anim 0.71s complete / left 2.6/2.6m anim 0.68s complete / left 2.6/2.6m anim 0.63s complete / back 2.1/2.2m anim 0.58s complete / back 2.2/2.2m anim 0.56s complete / right 2.6/2.6m anim 0.78s complete / back 2.1/2.2m anim 0.59s complete / back 2.2/2.2m anim 0.57s complete / back 2.2/2.2m anim 0.54s complete / right 2.6/2.6m anim 0.72s complete / back 2.1/2.2m anim 0.56s complete / left 2.5/2.6m anim 0.70s complete / left 2.5/2.6m anim 0.67s complete / back 2.2/2.2m anim 0.52s complete | an enemy rolled toward the player, stopped short while the clip kept playing, or a roll never finished |
| Interrupts | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | FAIL | 4 dodge strike(s) (reasons: handover x4); mean weave 0.93m of 0.82m planned; max distance change to the target 0.30m (includes the player moving); without token 0; clip never played 0; swing started after the dodge 2x; overridden by a defensive roll 0 | a dodge strike ran without the token, never played the clip, changed the distance by more than 0.5m, or no swing followed |
| Interrupts | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | WARN | SwordMaster: 7 swings, start mean 1.53 (max 1.67, first swing 1.57) vs weapon tip reach 2.29, gate 1.75 / Humadroid: 7 swings, start mean 1.41 (max 1.66, first swing 1.51) vs weapon tip reach 2.05, gate 1.75 / SwordMaster (2): 5 swings, start mean 1.50 (max 1.75, first swing 1.35) vs weapon tip reach 1.73, gate 1.75 / SwordMaster (1): 6 swings, start mean 1.45 (max 1.74, first swing 1.56) vs weapon tip reach 0.79, gate 1.75 / Robot: 8 swings, start mean 1.46 (max 1.69, first swing 1.69) vs weapon tip reach 1.72, gate 1.75 | an enemy swung from more than its weapon reach + 0.45 m |
| Interrupts | Swing-start distance vs weapon reach (first vs later swings) | PASS | first swing per enemy: 5 swings: start distance mean 1.54m (max 1.69), attack range 1.75, swing-start gate mean 1.75, weapon length 1.83m, weapon tip reach from root mean 1.98m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%), cut depth >=30% of torso radius 5/5 / later swings: 28 swings: start distance mean 1.46m (max 1.75), attack range 1.75, swing-start gate mean 1.75, weapon length 1.82m, weapon tip reach from root mean 1.70m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%), cut depth >=30% of torso radius 28/28 |  |
| Interrupts | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | WARN | SwordMaster: 18 swing seq, first swing 1.7s, tokens 8, token-eligible 96% of samples, Engager 20% / SwordMaster (1): 6 swing seq, first swing 12.7s, tokens 7, token-eligible 94% of samples, Engager 18% / SwordMaster (2): 5 swing seq, first swing 8.4s, tokens 7, token-eligible 95% of samples, Engager 18% / Humadroid: 7 swing seq, first swing 4.5s, tokens 6, token-eligible 91% of samples, Engager 21% / Robot: 8 swing seq, first swing 23.6s, tokens 8, token-eligible 92% of samples, Engager 18%; most/least swings 18/5 | an enemy attacked less than 40% as often as another, or never |
| Interrupts | Audit player: melee hits landed (needed for block / interrupt checks) | PASS | hits=SwordMaster:10,SwordMaster (1):11,SwordMaster (2):7,Humadroid:13,Robot:14 attackFrames=1221 minStamina=980 lightClicks=66 pressedFrames=0 flagsAtClick={isAttacking:49} armed=1 |  |
| Interrupts | Hit FX scaling (blood / coolant / sparks use one scaling path) | PASS | Blood: light x1.05 (n 15), heavy x1.42 (n 12), chain>=2 x1.30 (n 4) / Coolant: light x1.07 (n 9), heavy x1.49 (n 4), chain>=2 x1.26 (n 6) / SparksSmall: light x1.07 (n 7), heavy x1.42 (n 5), chain>=2 x1.20 (n 4) // per enemy: SwordMaster: Blood x8 / Humadroid: Coolant x13 / SwordMaster (2): Blood x6 / Robot: SparksSmall x12 / SwordMaster (1): Blood x10 / Humanoid(Clone): Blood x3 |  |
| Interrupts | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | PASS | HolderSidestep x14 moved 1.25m of 1.24m planned (complete 9, timeout 3, cancel 2); Backstep x1 moved 0.16m of 0.16m planned (complete 1); Strafe x3 moved 0.65m of 0.75m planned (complete 1, attack-ready 1, timeout 1); step animation end vs move end: n 53, mean lag 0 ms, max 0 ms, over 150 ms: 0; zero-displacement steps 0, short while complete 0 |  |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: 10.8:Holder, 18.4:Holder, 31.0:Holder, 33.1:Engager, 34.2:Holder, 44.3:Holder, 51.8:Holder, 53.6:Holder, 66.5:Holder, 71.4:Holder, 72.5:Holder, 85.9:Holder (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
| Interrupts | Multi-enemy (5 x SwordMaster): token rotation / spacing | PASS | 102 role samples; max simultaneous Engagers 1; distinct enemies that held Engager 5/5; hand-offs 34 |  |
| Interrupts | No stacking / circling (multi-enemy) | WARN | min enemy-enemy distance 0.59m, frames with a pair under 0.7m: 3/1448; circling suspects (>720deg yaw, <3m net): none; pairs under 0.7m by state/role: Defensive/Engager + Hold/Holder x2 / Attack/Holder + Chase/Engager x1; path per enemy: SwordMaster 67m, SwordMaster (1) 71m, SwordMaster (2) 76m, Humadroid 68m, Robot 67m |  |
| Interrupts | Overlapping attack windows (one attacker at a time, all enemy types, swings still finishing) | PASS | windows=0 seconds=0.00 max=1 samples= |  |
| Interrupts | Camera lens vs player body (near plane inside a player collider = clip-through) | PASS | surfMin=1.70 inFrames=0 inWindows=0 closeFrames=0 of=1448 pivotMin=2.45 at= |  |
| Interrupts | Enemy block (natural part of the run; forced block in the last 15 s) | PASS | natural: blocked 0, guard broken 2, misses 48 (guardNotRaised x48) / forced: blocked/broken 8 of 8 player hits () |  |
| Interrupts | Body overlap (capsules of enemies / player interpenetrating by more than 5 cm) | FAIL | windows=1 frames=2 samples=8.2s SwordMaster_(2)+Player_v7_Combat_Variant |  |
| Interrupts | Stiff pose (alive, not ragdolled, animator off / not initialised) | PASS | notReadyEnemies=0 detail= |  |
| Interrupts | Enemy dodges play a DM_Dodge clip (never forward roll, never a clipless slide) | PASS | played 19, skipped (animator not ready) 0, forward-roll 0, played without clip 0 / [DodgePlayed] Humadroid state=DM_DodgeLeft clipOk=1 // [DodgePlayed] SwordMaster (1) state=DM_DodgeLeft clipOk=1 // [DodgePlayed] Humadroid state=DM_DodgeRight clipOk=1 // [DodgePlayed] SwordMaster (1) state=DM_DodgeBack clipOk=1 // [DodgePlayed] Robot state=DM_DodgeLeft clipOk=1 // [DodgePlayed] SwordMaster (1) state=DM_DodgeLeft clipOk=1 |  |
| Interrupts | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | WARN | 15 episodes: Robot x4 (blocked 3, stagger 3, attacking 0, quick 3, suppress 0, mean speed 1.09, clips Idle/AimedIdle, movers ChaseMove[ChaseMove0.22/]/HitSlide[HitSlide0.72/]/HitSlide[HitSlide0.17/]/HitSlide[HitSlide0.02/], ctrlMag 0.25, states Wander/Attack) / SwordMaster (2) x1 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 0.91, clips Idle, movers ChaseMove[ChaseMove0.16/], ctrlMag 1.00, states Investigate) / Robot(Clone) x8 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 0.79, clips Idle, movers ChaseMove[ChaseMove0.14/]/ChaseMove[ChaseMove0.19/]/ChaseMove[ChaseMove0.16/]/ChaseMove[ChaseMove0.21/]/ChaseMove[ChaseMove0.18/]/ChaseMove[ChaseMove0.20/], ctrlMag 1.00, states Wander) / Humadroid x1 (blocked 1, stagger 1, attacking 0, quick 1, suppress 0, mean speed 1.00, clips Idle, movers HitSlide[HitSlide0.04/], ctrlMag 0.00, states Attack) / The_Evil_One(Clone) x1 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 0.92, clips Idle, movers ChaseMove[ChaseMove0.17/], ctrlMag 1.00, states Wander) | code-driven displacement without a leg cycle |
| Interrupts | Roll diagnostics (clip cut by another transition / no dodge clip chosen) | PASS | none |  |
| Interrupts | Player shake: static physics / animator setup (interpolation, update mode, rig colliders are triggers, nothing pushes the capsule) | PASS | rbInterp=Interpolate profileInterp=Interpolate rbKinematic=0 animatorUpdate=Fixed rootMotion=0 fixedDt=0.0167 autoSync=off(project setting; hitbox queries call SyncTransforms once per frame) playerNonTriggerColliders=38 rigNonTriggerColliders=0 rigRigidbodies=0 nonKinematicBoneRigidbodies=0 rigColliders=18 hitboxLayerCollidesWith= hitstopHold=0.30 freezesAttacker=True forwardDrift=0.12 |  |
| Interrupts | Player shake study (8 phases of 12 s: baseline, player not frozen, hit stop off, hitbox rig off, interpolation None, old hold speed 0.05, no forward drift, player only) | PASS | baseline: frames 1448 (attack 1221, holdFrames 69), posReversals/100f 1.2 (during attack 16, during hold 0), forwardReversals/100f 2.1, yawReversals/100f 20.1, cameraReversals/100f 6.7, speedJitter 0.68 m/s, audit-applied yaw 0 of 8356 deg, max step 0.355 m / playerNoFreeze: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / hitstopOff: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / hitboxRigOff: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / interpolationNone: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / oldHoldSpeed0.05: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / noForwardDrift: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / playerOnly: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m |  |
| Interrupts | Player motion smoothness (frame time, timeScale, animator speed, locomotion param jitter, root speed jitter, W key chatter) | WARN | frames=1448 meanDtMs=55.4 maxDtMs=132 hitchFrames=887 timeScaleFrames=0 animSpeedFrames=1221 animSpeedMin=0.30 wFlips=0 moveSamples=793 moveSpeedMean=1.16 moveSpeedStd=0.93 inputMagMaxStep=0.21 inputMagFlips=57 relHitchFrames=2 meanDtAttMs=55.1 meanDtIdleMs=56.7 animSpeedMax=1.47 hitStopFrames=20 meanDtHitStopMs=54.6 speedJumps=124 maxStepM=0.35 gc0=1 vsync=1 targetFps=-1 maxDeltaTime=0.10 hitches=0.1s 62ms attacking=0;0.4s 52ms attacking=1;0.4s 56ms attacking=1;0.5s 61ms attacking=1;0.6s 68ms attacking=1;0.6s 52ms attacking=1 |  |
| Interrupts | Hand distance (info only; minHand gave false positives, see int.animready for stiff poses) | INFO | SwordMaster minHand=0.07 clip=AimedWalkBack_at_1.6s lowSamples=217/241 firstLowAt=0.5s clip=Idle upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / SwordMaster (1) minHand=0.13 clip=AimedIdle_at_69.2s lowSamples=216/241 firstLowAt=0.5s clip=AimedWalk upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / SwordMaster (2) minHand=0.07 clip=Idle_at_7.1s lowSamples=228/241 firstLowAt=0.5s clip=Walk upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / Humadroid minHand=0.11 clip=Walk_at_25.3s lowSamples=222/241 firstLowAt=0.5s clip=Run upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword / Robot minHand=0.13 clip=Idle_at_3.4s lowSamples=211/241 firstLowAt=0.5s clip=Idle upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword |  |
| Interrupts | Console errors during the run | FAIL | 3 error(s): NullReferenceException: Object reference not set to an instance of an object / NullReferenceException: Object reference not set to an instance of an object / NullReferenceException: Object reference not set to an instance of an object | see samples |
| Console | Console errors/warnings | FAIL | 6 error(s), 0 warning(s) | [Exception] NullReferenceException: Object reference not set to an instance of an object @ BFX.BFX_ShaderProperies.Awake () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_ShaderProperies.cs:35) <- UnityEngine.Object:Instantiate(GameObject, Transform) <- Project.Combat.DMVolumetricBlood:Get(GameObject) (at Assets/_Project/Scripts/Combat/HitMarks/DMVolumetricBlood.cs:248) |

Console during run: 6 error(s), 0 warning(s):
- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ BFX.BFX_ShaderProperies.Awake () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_ShaderProperies.cs:35) <- UnityEngine.Object:Instantiate(GameObject, Transform) <- Project.Combat.DMVolumetricBlood:Get(GameObject) (at Assets/_Project/Scripts/Combat/HitMarks/DMVolumetricBlood.cs:248)`
- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ BFX.BFX_ShaderProperies.OnDisableExtended () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_ShaderProperies.cs:54) <- BFX.IScriptInstance.OnDisable () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_IScriptInstance.cs:23) <- UnityEngine.Object:Instantiate(GameObject, Transform)`
- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ BFX.BFX_DecalSettings.InitializePosition () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_DecalSettings.cs:60) <- BFX.BFX_DecalSettings.ManualUpdate () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_DecalSettings.cs:53) <- BFX.GlobalUpdate.Update () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_GlobalUpdate.cs:53)`
- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ BFX.BFX_ShaderProperies.Awake () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_ShaderProperies.cs:35) <- UnityEngine.Object:Instantiate(GameObject, Transform) <- Project.Combat.DMVolumetricBlood:Get(GameObject) (at Assets/_Project/Scripts/Combat/HitMarks/DMVolumetricBlood.cs:248)`
- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ BFX.BFX_ShaderProperies.OnDisableExtended () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_ShaderProperies.cs:54) <- BFX.IScriptInstance.OnDisable () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_IScriptInstance.cs:23) <- UnityEngine.Object:Instantiate(GameObject, Transform)`
- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ BFX.BFX_DecalSettings.InitializePosition () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_DecalSettings.cs:60) <- BFX.BFX_DecalSettings.ManualUpdate () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_DecalSettings.cs:53) <- BFX.GlobalUpdate.Update () (at Assets/KriptoFX/VolumetricBloodFX/Scripts/BFX_GlobalUpdate.cs:53)`

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
