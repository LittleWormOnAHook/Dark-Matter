# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-13 00:24:56 -> 2026-10-13 00:26:49 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 1; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit 43F44C game 005CCF gear 2F3271; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m52s

**1 enemies audited - 2 FAIL, 6 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 27 PASS, 2 FAIL, 12 warn.**

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
- NEW enemy|SwordMaster|int.bodyoverlap = PASS
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
| SwordMaster | enemy | 101.7 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 101.7 | 1873 | 55.3 | 1894.8 | 1.06 | 0.00 | 24.98 |
| enemy|SwordMaster | cleanup | 0.2 | 3 | 82.7 | 99.7 | 1.00 | 0.02 | 22.95 |

Whole run (ran units only): 102s over 1876 frames, avg frame 55.3 ms, physics 1.06 ms, render 0.00 ms, scripts 24.98 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 20 | FAIL | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | SwordMaster | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete | Inspect the measured value and cause on the detail row. |
| 2 | 20 | FAIL | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | SwordMaster | a dodge strike ran without the token, never played the clip, changed the distance by more than 0.5m, or no swing followed | Inspect the measured value and cause on the detail row. |
| 3 | -5 | WARN | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | SwordMaster | an enemy swung from more than its weapon reach + 0.45 m | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | SwordMaster | an enemy attacked less than 40% as often as another, or never | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | No stacking / circling (multi-enemy) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | SwordMaster | code-driven displacement without a leg cycle | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | Roll diagnostics (clip cut by another transition / no dodge clip chosen) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `` - melee `` / ranged `` - audited in 102s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Interrupts | Interrupts by reason (SwordMaster, 96s, 66 light + 11 charged swings) | PASS | 14 interrupt(s): early window x12, poise break x2; skipped: immunity x0, super-armor x0, committed x5; chance rolls that failed x2; heavy hits seen x7; poise-break stagger events x11; enemy swing sequences started 28; interrupt rate 50% |  |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 0 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 13 interrupt(s) followed by a new swing; shortest gap 2.85s; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 16.27s (min 1.56, max 72.79, n 23); end-to-next-start gap mean 17.19s (n 10); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 7, engager backsteps 1, holder sidesteps 15 in the run; other kinds 0; quick-step frames 594 (animator speed boosted in 244, max 1.30); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.20 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | PASS | blocked hits 5 (player recoiled 3), guards broken 2 (charged 2, poise 0) in the run |  |
| Interrupts | Dodge roll direction per enemy (never toward the player; Humadroid included) | PASS | SwordMaster (2): 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 2 / side 1, mean moved 2.3m / SwordMaster (1): 2 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 2, mean moved 1.3m / Robot: 4 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 3, mean moved 1.8m / SwordMaster: 4 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 3 / side 1, mean moved 2.3m / Humadroid: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 2 / side 1, mean moved 2.3m |  |
| Interrupts | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | FAIL | 16 rolls: forward Roll clip 0, back 8, left 5, right 3; animator state never seen 2; ended early (not complete) 2 [noclip, noclip] | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete |
| Interrupts | Dodge roll: direction vs displacement, movement completes with the clip | PASS | 16 started, 16 finished (reasons: complete x14, noclip x2); full distance (>=90% of plan) 14, short while complete 0; toward the player 0; back 8, side 8; mean moved 2.06m, mean animation 0.51s, mean elapsed 0.70s; per roll: back 2.2/2.2m anim 0.53s complete / left 0.0/2.6m anim 0.00s noclip / right 0.0/2.6m anim 0.00s noclip / back 2.2/2.2m anim 0.52s complete / back 2.2/2.2m anim 0.57s complete / back 2.2/2.2m anim 0.28s complete / right 2.5/2.6m anim 0.68s complete / back 2.1/2.2m anim 0.57s complete / left 2.6/2.6m anim 0.70s complete / right 2.6/2.6m anim 0.73s complete / back 2.2/2.2m anim 0.19s complete / left 2.6/2.6m anim 0.69s complete / back 2.2/2.2m anim 0.61s complete / back 2.2/2.2m anim 0.55s complete / left 2.6/2.6m anim 0.79s complete / left 2.6/2.6m anim 0.68s complete |  |
| Interrupts | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | FAIL | 7 dodge strike(s) (reasons: handover x7); mean weave 0.87m of 0.87m planned; max distance change to the target 0.88m (includes the player moving); without token 0; clip never played 0; swing started after the dodge 5x; overridden by a defensive roll 2 | a dodge strike ran without the token, never played the clip, changed the distance by more than 0.5m, or no swing followed |
| Interrupts | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | WARN | SwordMaster: 10 swings, start mean 1.42 (max 1.73, first swing 1.73) vs weapon tip reach 2.08, gate 1.75 / Robot: 6 swings, start mean 1.51 (max 1.71, first swing 1.71) vs weapon tip reach 1.41, gate 1.75 / Humadroid: 3 swings, start mean 1.39 (max 1.53, first swing 1.53) vs weapon tip reach 2.03, gate 1.75 / SwordMaster (2): 6 swings, start mean 1.26 (max 1.57, first swing 1.51) vs weapon tip reach 1.65, gate 1.75 / SwordMaster (1): 3 swings, start mean 1.42 (max 1.58, first swing 1.40) vs weapon tip reach 0.61, gate 1.75 | an enemy swung from more than its weapon reach + 0.45 m |
| Interrupts | Swing-start distance vs weapon reach (first vs later swings) | PASS | first swing per enemy: 5 swings: start distance mean 1.58m (max 1.73), attack range 1.75, swing-start gate mean 1.75, weapon length 1.83m, weapon tip reach from root mean 0.60m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%), cut depth >=30% of torso radius 5/5 / later swings: 23 swings: start distance mean 1.36m (max 1.70), attack range 1.75, swing-start gate mean 1.75, weapon length 1.79m, weapon tip reach from root mean 1.91m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%), cut depth >=30% of torso radius 23/23 |  |
| Interrupts | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | WARN | SwordMaster: 19 swing seq, first swing 0.8s, tokens 7, token-eligible 95% of samples, Engager 30% / SwordMaster (1): 3 swing seq, first swing 20.8s, tokens 4, token-eligible 95% of samples, Engager 8% / SwordMaster (2): 6 swing seq, first swing 10.0s, tokens 7, token-eligible 92% of samples, Engager 15% / Humadroid: 3 swing seq, first swing 4.3s, tokens 5, token-eligible 91% of samples, Engager 10% / Robot: 6 swing seq, first swing 1.3s, tokens 7, token-eligible 94% of samples, Engager 14%; most/least swings 19/3 | an enemy attacked less than 40% as often as another, or never |
| Interrupts | Audit player: melee hits landed (needed for block / interrupt checks) | PASS | hits=SwordMaster:18,SwordMaster (1):5,SwordMaster (2):8,Humadroid:7,Robot:8 attackFrames=1228 minStamina=980 lightClicks=66 pressedFrames=0 flagsAtClick={isAttacking:52} armed=1 |  |
| Interrupts | Hit FX scaling (blood / coolant / sparks use one scaling path) | PASS | Blood: light x1.05 (n 18), heavy x1.43 (n 10), chain>=2 x1.26 (n 5) / SparksSmall: light x1.06 (n 7), heavy x1.42 (n 1), chain>=2 x1.13 (n 2) / Coolant: light x1.05 (n 4), heavy x1.42 (n 3), chain>=2 x1.10 (n 1) // per enemy: SwordMaster: Blood x15 / Robot: SparksSmall x8 / SwordMaster (2): Blood x7 / Humadroid: Coolant x7 / SwordMaster (1): Blood x5 / Humanoid(Clone): Blood x1 |  |
| Interrupts | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | PASS | HolderSidestep x15 moved 1.27m of 1.26m planned (complete 12, timeout 3); Strafe x5 moved 0.47m of 0.67m planned (attack-ready 2, complete 3); Backstep x1 moved 0.41m of 0.41m planned (complete 1); step animation end vs move end: n 55, mean lag 0 ms, max 0 ms, over 150 ms: 0; zero-displacement steps 0, short while complete 1 |  |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: 6.5:Engager, 7.1:Holder, 10.0:Holder, 15.8:Holder, 18.0:Holder, 25.3:Holder, 26.6:Holder, 30.2:Holder, 35.2:Holder, 36.7:Engager, 38.8:Holder, 41.2:Engager (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
| Interrupts | Multi-enemy (5 x SwordMaster): token rotation / spacing | PASS | 98 role samples; max simultaneous Engagers 2; distinct enemies that held Engager 5/5; hand-offs 27 |  |
| Interrupts | No stacking / circling (multi-enemy) | WARN | min enemy-enemy distance 0.50m, frames with a pair under 0.7m: 13/1486; circling suspects (>720deg yaw, <3m net): none; pairs under 0.7m by state/role: Hold/Holder + Hold/Holder x11 / Chase/Engager + Defensive/Holder x2; path per enemy: SwordMaster 66m, SwordMaster (1) 68m, SwordMaster (2) 86m, Humadroid 80m, Robot 75m |  |
| Interrupts | Overlapping attack windows (one attacker at a time, all enemy types, swings still finishing) | PASS | windows=0 seconds=0.00 max=1 samples= |  |
| Interrupts | Camera lens vs player body (near plane inside a player collider = clip-through) | PASS | surfMin=1.74 inFrames=0 inWindows=0 closeFrames=0 of=1486 pivotMin=2.38 at= |  |
| Interrupts | Enemy block (natural part of the run; forced block in the last 15 s) | PASS | natural: blocked 3, guard broken 1, misses 41 (guardNotRaised x41) / forced: blocked/broken 3 of 3 player hits () |  |
| Interrupts | Body overlap (capsules of enemies / player interpenetrating by more than 5 cm) | PASS | windows=0 frames=0 samples= |  |
| Interrupts | Stiff pose (alive, not ragdolled, animator off / not initialised) | PASS | notReadyEnemies=0 detail= |  |
| Interrupts | Enemy dodges play a DM_Dodge clip (never forward roll, never a clipless slide) | PASS | played 16, skipped (animator not ready) 0, forward-roll 0, played without clip 0 / [DodgePlayed] SwordMaster (2) state=DM_DodgeBack clipOk=1 // [DodgePlayed] SwordMaster (1) state=DM_DodgeLeft clipOk=1 // [DodgePlayed] Robot state=DM_DodgeRight clipOk=1 // [DodgePlayed] SwordMaster state=DM_DodgeBack clipOk=1 // [DodgePlayed] Humadroid state=DM_DodgeBack clipOk=1 // [DodgePlayed] Robot state=DM_DodgeBack clipOk=1 |  |
| Interrupts | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | WARN | 9 episodes: Robot x1 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 0.72, clips Idle, movers ChaseMove[ChaseMove0.22/], ctrlMag 1.00, states Wander) / Humadroid x1 (blocked 1, stagger 1, attacking 0, quick 1, suppress 0, mean speed 0.89, clips Idle, movers HitSlide[HitSlide0.29/], ctrlMag 0.00, states Attack) / SwordMaster x4 (blocked 3, stagger 3, attacking 0, quick 4, suppress 0, mean speed 1.29, clips Idle/AimedIdle, movers HitSlide[HitSlide0.66/]/HitSlide[HitSlide0.23/]/HitSlide[HitSlide0.30/]/HitSlide[HitSlide0.24/], ctrlMag 0.00, states Attack/Hold) / Robot(Clone) x1 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 0.81, clips Idle, movers ChaseMove[ChaseMove0.14/], ctrlMag 1.00, states Wander) / SwordMaster (2) x1 (blocked 1, stagger 1, attacking 0, quick 1, suppress 0, mean speed 1.11, clips AimedIdle, movers HitSlide[HitSlide0.24/], ctrlMag 0.00, states Attack) / The_Evil_One(Clone) x1 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 1.15, clips Idle, movers ChaseMove[ChaseMove0.20/], ctrlMag 1.00, states Wander) | code-driven displacement without a leg cycle |
| Interrupts | Roll diagnostics (clip cut by another transition / no dodge clip chosen) | WARN | [RollClipLeft] SwordMaster (1) NOCLIP clip=left nowClip=AimedWalk enabled=1 inTransition=0 speed=1.00 state=Defensive attacking=0 stagger=0 // [RollClipLeft] Robot NOCLIP clip=right nowClip=AimedIdle enabled=1 inTransition=0 speed=1.00 state=Defensive attacking=0 stagger=0 |  |
| Interrupts | Player shake: static physics / animator setup (interpolation, update mode, rig colliders are triggers, nothing pushes the capsule) | PASS | rbInterp=Interpolate profileInterp=Interpolate rbKinematic=0 animatorUpdate=Fixed rootMotion=0 fixedDt=0.0167 autoSync=off(project setting; hitbox queries call SyncTransforms once per frame) playerNonTriggerColliders=38 rigNonTriggerColliders=0 rigRigidbodies=0 nonKinematicBoneRigidbodies=0 rigColliders=18 hitboxLayerCollidesWith= hitstopHold=0.30 freezesAttacker=True forwardDrift=0.12 |  |
| Interrupts | Player shake study (8 phases of 12 s: baseline, player not frozen, hit stop off, hitbox rig off, interpolation None, old hold speed 0.05, no forward drift, player only) | PASS | baseline: frames 1486 (attack 1228, holdFrames 56), posReversals/100f 1.6 (during attack 24, during hold 0), forwardReversals/100f 2.4, yawReversals/100f 25.9, cameraReversals/100f 7.3, speedJitter 0.68 m/s, audit-applied yaw 0 of 11222 deg, max step 0.431 m / playerNoFreeze: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / hitstopOff: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / hitboxRigOff: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / interpolationNone: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / oldHoldSpeed0.05: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / noForwardDrift: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m / playerOnly: frames 0 (attack 0, holdFrames 0), posReversals/100f 0.0 (during attack 0, during hold 0), forwardReversals/100f 0.0, yawReversals/100f 0.0, cameraReversals/100f 0.0, speedJitter 0.00 m/s, audit-applied yaw 0 of 0 deg, max step 0.000 m |  |
| Interrupts | Player motion smoothness (frame time, timeScale, animator speed, locomotion param jitter, root speed jitter, W key chatter) | WARN | frames=1486 meanDtMs=54.2 maxDtMs=145 hitchFrames=862 timeScaleFrames=0 animSpeedFrames=1228 animSpeedMin=0.30 wFlips=0 moveSamples=899 moveSpeedMean=1.27 moveSpeedStd=0.97 inputMagMaxStep=0.20 inputMagFlips=61 relHitchFrames=0 meanDtAttMs=54.2 meanDtIdleMs=54.1 animSpeedMax=1.47 hitStopFrames=14 meanDtHitStopMs=60.2 speedJumps=152 maxStepM=0.43 gc0=1 vsync=1 targetFps=-1 maxDeltaTime=0.10 hitches=0.1s 69ms attacking=0;0.3s 55ms attacking=1;0.4s 66ms attacking=1;0.4s 53ms attacking=1;0.5s 59ms attacking=1;0.7s 91ms attacking=1 |  |
| Interrupts | Hand distance (info only; minHand gave false positives, see int.animready for stiff poses) | INFO | SwordMaster minHand=0.07 clip=Idle_at_42.2s lowSamples=229/262 firstLowAt=1.7s clip=AimedWalk upperId=-99 moveSet=-99 attackId=1 weapon=Blade_Of_Chaos / SwordMaster (1) minHand=0.09 clip=Idle_at_20.7s lowSamples=259/262 firstLowAt=0.3s clip=Walk upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / SwordMaster (2) minHand=0.07 clip=AimedWalk_at_26.0s lowSamples=238/262 firstLowAt=0.3s clip=Run upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / Humadroid minHand=0.15 clip=Idle_at_40.3s lowSamples=247/262 firstLowAt=0.3s clip=Walk upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword / Robot minHand=0.13 clip=AimedIdle_at_61.4s lowSamples=237/262 firstLowAt=0.3s clip=Walk upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword |  |
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
