# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-11 19:54:47 -> 2026-10-11 19:56:19 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 3; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit 99CF38 game 5AE8DF gear FB6DFE; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m31s

**1 enemies audited - 3 FAIL, 5 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 21 PASS, 3 FAIL, 11 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 36 change(s).

- NEW enemy|SwordMaster|int.counts = PASS
- NEW enemy|SwordMaster|int.early = PASS
- NEW enemy|SwordMaster|int.immunity = PASS
- NEW enemy|SwordMaster|int.recovery = PASS
- NEW enemy|SwordMaster|int.cadence = PASS
- NEW enemy|SwordMaster|int.moves = PASS
- NEW enemy|SwordMaster|int.block = WARN
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
- NEW enemy|SwordMaster|int.playermotion = WARN
- NEW enemy|SwordMaster|int.spawnpose = FAIL
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

Wall time 92s (1.5 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| SwordMaster | enemy | 81.5 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 81.5 | 1673 | 49.8 | 1914.0 | 0.90 | 0.00 | 20.70 |
| enemy|SwordMaster | cleanup | 0.2 | 6 | 38.0 | 52.2 | 0.60 | 0.00 | 12.74 |

Whole run (ran units only): 82s over 1679 frames, avg frame 49.8 ms, physics 0.90 ms, render 0.00 ms, scripts 20.67 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 20 | FAIL | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | SwordMaster | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete | Inspect the measured value and cause on the detail row. |
| 2 | 20 | FAIL | Dodge roll: direction vs displacement, movement completes with the clip | SwordMaster | an enemy rolled toward the player, stopped short while the clip kept playing, or a roll never finished | Inspect the measured value and cause on the detail row. |
| 3 | 20 | FAIL | Spawn pose (idle hand distance in the first 4 s; under 0.35 = crossed arms) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | Enemy block (brain Defend -> guard, blocked hits, player recoil) | SwordMaster | no enemy block landed (block needs a Defend decision + a player hit inside the block window) | Inspect the measured value and cause on the detail row. |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `` - melee `` / ranged `` - audited in 81s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Interrupts | Interrupts by reason (SwordMaster, 75s, 54 light + 9 charged swings) | PASS | 14 interrupt(s): early window x8, heavy hit vs light swing x2, poise break x4; skipped: immunity x0, super-armor x0, committed x9; chance rolls that failed x5; heavy hits seen x14; poise-break stagger events x14; enemy swing sequences started 31; interrupt rate 45% |  |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 0 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 11 interrupt(s) followed by a new swing; shortest gap 6.74s; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 12.83s (min 1.62, max 32.53, n 26); end-to-next-start gap mean 10.74s (n 15); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 5, engager backsteps 3, holder sidesteps 20 in the run; other kinds 0; quick-step frames 1613 (animator speed boosted in 418, max 1.30); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.10 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | WARN | blocked hits 0 (player recoiled 0), guards broken 0 (charged 0, poise 0) in the run | no enemy block landed (block needs a Defend decision + a player hit inside the block window) |
| Interrupts | Dodge roll direction per enemy (never toward the player; Humadroid included) | PASS | AUDIT_W_int_2_Humadroid: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 2 / side 1, mean moved 2.4m / AUDIT_W_int_4: 2 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 2, mean moved 2.6m / AUDIT_W_int_3_Robot: 2 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 2, mean moved 2.6m / AUDIT_W_int_5: 3 rolls, toward the player 0, max dir/disp dot vs the player -1.00, back 3 / side 0, mean moved 1.6m / AUDIT_W_int: 1 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 1, mean moved 2.6m |  |
| Interrupts | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | FAIL | 11 rolls: forward Roll clip 0, back 5, left 3, right 3; animator state never seen 1; ended early (not complete) 0 [] | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete |
| Interrupts | Dodge roll: direction vs displacement, movement completes with the clip | FAIL | 11 started, 11 finished (reasons: complete x11); full distance (>=90% of plan) 10, short while complete 1; toward the player 0; back 5, side 6; mean moved 2.25m, mean animation 0.59s, mean elapsed 0.77s; per roll: back 2.2/2.2m anim 0.54s complete / left 2.6/2.6m anim 0.68s complete / right 2.6/2.6m anim 0.69s complete / back 2.2/2.2m anim 0.57s complete / right 2.6/2.6m anim 0.68s complete / right 2.7/2.6m anim 0.71s complete / left 2.6/2.6m anim 0.68s complete / back 2.2/2.2m anim 0.56s complete / back 2.2/2.2m anim 0.58s complete / back 0.4/2.2m anim 0.00s complete / left 2.6/2.6m anim 0.84s complete | an enemy rolled toward the player, stopped short while the clip kept playing, or a roll never finished |
| Interrupts | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | PASS | 6 dodge strike(s) (reasons: handover x6); mean weave 0.76m of 0.77m planned; max distance change to the target 0.61m (includes the player moving); without token 0; clip never played 0; swing started after the dodge 6x; overridden by a defensive roll 0 |  |
| Interrupts | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | WARN | AUDIT_W_int: 6 swings, start mean 1.74 (max 2.03, first swing 1.94) vs weapon tip reach 1.70, gate 2.07 / AUDIT_W_int_4: 7 swings, start mean 1.53 (max 1.99, first swing 1.42) vs weapon tip reach 1.21, gate 2.03 / AUDIT_W_int_2_Humadroid: 4 swings, start mean 1.66 (max 1.90, first swing 1.61) vs weapon tip reach 2.18, gate 2.27 / AUDIT_W_int_3_Robot: 6 swings, start mean 1.87 (max 2.16, first swing 2.12) vs weapon tip reach 1.45, gate 2.17 / AUDIT_W_int_5: 7 swings, start mean 1.55 (max 1.97, first swing 1.43) vs weapon tip reach 2.10, gate 2.03 | an enemy swung from more than its weapon reach + 0.45 m |
| Interrupts | Swing-start distance vs weapon reach (first vs later swings) | PASS | first swing per enemy: 5 swings: start distance mean 1.70m (max 2.12), attack range 2.27, swing-start gate mean 2.11, weapon length 1.56m, weapon tip reach from root mean 1.75m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) / later swings: 25 swings: start distance mean 1.65m (max 2.16), attack range 2.27, swing-start gate mean 2.10, weapon length 1.50m, weapon tip reach from root mean 1.68m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) |  |
| Interrupts | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | PASS | AUDIT_W_int: 6 swing seq, first swing 0.5s, tokens 6, token-eligible 95% of samples, Engager 15% / AUDIT_W_int_2_Humadroid: 4 swing seq, first swing 6.7s, tokens 5, token-eligible 94% of samples, Engager 18% / AUDIT_W_int_3_Robot: 6 swing seq, first swing 10.2s, tokens 6, token-eligible 94% of samples, Engager 22% / AUDIT_W_int_4: 7 swing seq, first swing 3.1s, tokens 7, token-eligible 96% of samples, Engager 19% / AUDIT_W_int_5: 7 swing seq, first swing 12.2s, tokens 6, token-eligible 96% of samples, Engager 26%; most/least swings 7/4 |  |
| Interrupts | Audit player: melee hits landed (needed for block / interrupt checks) | PASS | hits=AUDIT_W_int:8,AUDIT_W_int_2_Humadroid:7,AUDIT_W_int_3_Robot:8,AUDIT_W_int_4:11,AUDIT_W_int_5:9 attackFrames=1196 minStamina=980 lightClicks=54 pressedFrames=0 flagsAtClick={isAttacking:52} armed=1 |  |
| Interrupts | Hit FX scaling (blood / coolant / sparks use one scaling path) | PASS | Blood: light x1.08 (n 17), heavy x1.45 (n 12), chain>=2 x1.29 (n 12) / Coolant: light x1.08 (n 4), heavy x1.42 (n 3), chain>=2 x1.13 (n 2) / SparksSmall: light x1.10 (n 2), heavy x1.43 (n 6), chain>=2 x1.38 (n 3) // per enemy: AUDIT_W_int_4: Blood x12 / AUDIT_W_int_2_Humadroid: Coolant x7 / AUDIT_W_int_3_Robot: SparksSmall x8 / AUDIT_W_int_5: Blood x9 / AUDIT_W_int: Blood x8 |  |
| Interrupts | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | PASS | HolderSidestep x20 moved 1.15m of 1.32m planned (complete 14, timeout 3, cancel 3); Strafe x1 moved 0.63m of 0.69m planned (timeout 1); Backstep x2 moved 0.68m of 0.68m planned (complete 2); step animation end vs move end: n 48, mean lag 0 ms, max 0 ms, over 150 ms: 0; zero-displacement steps 0, short while complete 0 |  |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: 9.3:Holder, 16.5:Holder, 17.8:Holder, 23.8:Engager, 32.8:Holder, 39.8:Holder, 40.8:Holder, 53.8:Holder, 61.5:Holder, 66.8:Engager, 71.1:Holder, 74.7:Holder (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
| Interrupts | Multi-enemy (5 x SwordMaster): token rotation / spacing | PASS | 93 role samples; max simultaneous Engagers 1; distinct enemies that held Engager 5/5; hand-offs 29 |  |
| Interrupts | No stacking / circling (multi-enemy) | WARN | min enemy-enemy distance 0.28m, frames with a pair under 0.7m: 4/1337; circling suspects (>720deg yaw, <3m net): none; pairs under 0.7m by state/role: Chase/Engager + Chase/Holder x4; path per enemy: AUDIT_W_int 47m, AUDIT_W_int_2_Humadroid 39m, AUDIT_W_int_3_Robot 47m, AUDIT_W_int_4 57m, AUDIT_W_int_5 56m |  |
| Interrupts | Overlapping attack windows (one attacker at a time, all enemy types, swings still finishing) | PASS | windows=0 seconds=0.00 max=1 samples= |  |
| Interrupts | Camera distance to the player pivot during the fight (enemies never pull the lens into the player) | PASS | min=2.29 closeFrames=0 of=1337 |  |
| Interrupts | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | WARN | 5 episodes: AUDIT_W_int x1 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 1.24, clips Idle) / AUDIT_W_int_3_Robot x1 (blocked 1, stagger 1, attacking 0, quick 1, suppress 0, mean speed 2.84, clips Idle) / AUDIT_W_int_4 x3 (blocked 2, stagger 2, attacking 0, quick 2, suppress 0, mean speed 1.25, clips AimedIdle) | code-driven displacement without a leg cycle |
| Interrupts | Player motion smoothness (frame time, timeScale, animator speed, locomotion param jitter, root speed jitter, W key chatter) | WARN | frames=1337 meanDtMs=48.7 maxDtMs=124 hitchFrames=593 timeScaleFrames=0 animSpeedFrames=1196 animSpeedMin=0.05 wFlips=0 moveSamples=0 moveSpeedMean=0.00 moveSpeedStd=0.00 inputMagMaxStep=0.20 inputMagFlips=51 hitches=0.1s 84ms attacking=0;0.3s 51ms attacking=0;0.4s 56ms attacking=1;0.4s 62ms attacking=1;0.5s 75ms attacking=1;0.7s 67ms attacking=1 |  |
| Interrupts | Spawn pose (idle hand distance in the first 4 s; under 0.35 = crossed arms) | FAIL | AUDIT_W_int minHand=0.12 clip=AimedWalkRight_InPlace_at_16.8s lowSamples=198/223 firstLowAt=0.6s clip=Idle upperId=-99 moveSet=-99 attackId=1 weapon=Blade_Of_Chaos / AUDIT_W_int_2_Humadroid minHand=0.12 clip=Idle_at_58.0s lowSamples=206/223 firstLowAt=0.6s clip=standing_walk_forward upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword / AUDIT_W_int_3_Robot minHand=0.13 clip=AimedIdle_at_77.1s lowSamples=199/223 firstLowAt=0.6s clip=AimedIdle upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword / AUDIT_W_int_4 minHand=0.10 clip=AimedWalk_at_13.8s lowSamples=206/223 firstLowAt=0.6s clip=Walk upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / AUDIT_W_int_5 minHand=0.07 clip=AimedWalkRight_InPlace_at_12.7s lowSamples=190/223 firstLowAt=0.6s clip=WalkTurnLeft upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos |  |
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
