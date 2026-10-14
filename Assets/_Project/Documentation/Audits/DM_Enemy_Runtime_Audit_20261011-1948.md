# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-11 19:46:49 -> 2026-10-11 19:48:18 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 3; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit A8B2EE game C6358A gear FB6DFE; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m29s

**1 enemies audited - 1 FAIL, 3 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 24 PASS, 1 FAIL, 9 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 35 change(s).

- NEW enemy|SwordMaster|int.counts = PASS
- NEW enemy|SwordMaster|int.early = PASS
- NEW enemy|SwordMaster|int.immunity = PASS
- NEW enemy|SwordMaster|int.recovery = PASS
- NEW enemy|SwordMaster|int.cadence = PASS
- NEW enemy|SwordMaster|int.moves = PASS
- NEW enemy|SwordMaster|int.block = PASS
- NEW enemy|SwordMaster|int.roll.byenemy = PASS
- NEW enemy|SwordMaster|int.roll.clips = PASS
- NEW enemy|SwordMaster|int.roll = PASS
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

Wall time 89s (1.5 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| SwordMaster | enemy | 79.2 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 79.2 | 1536 | 52.4 | 1370.4 | 0.89 | 0.00 | 21.56 |
| enemy|SwordMaster | cleanup | 0.2 | 4 | 53.6 | 63.2 | 0.64 | 0.00 | 14.52 |

Whole run (ran units only): 79s over 1540 frames, avg frame 52.4 ms, physics 0.89 ms, render 0.00 ms, scripts 21.54 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 20 | FAIL | Spawn pose (idle hand distance in the first 4 s; under 0.35 = crossed arms) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 2 | -5 | WARN | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | SwordMaster | an enemy swung from more than its weapon reach + 0.45 m | Inspect the measured value and cause on the detail row. |
| 3 | -5 | WARN | No stacking / circling (multi-enemy) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | SwordMaster | code-driven displacement without a leg cycle | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 9 | -5 | WARN | No circling while unaware (patrol-anchor fix) on every enemy | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 10 | -5 | WARN | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `` - melee `` / ranged `` - audited in 79s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Interrupts | Interrupts by reason (SwordMaster, 75s, 54 light + 8 charged swings) | PASS | 13 interrupt(s): early window x9, poise break x2, chance x1, heavy hit vs light swing x1; skipped: immunity x0, super-armor x0, committed x10; chance rolls that failed x1; heavy hits seen x13; poise-break stagger events x18; enemy swing sequences started 30; interrupt rate 43% |  |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 0 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 11 interrupt(s) followed by a new swing; shortest gap 5.00s; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 11.91s (min 1.64, max 27.87, n 25); end-to-next-start gap mean 10.67s (n 13); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 2, engager backsteps 3, holder sidesteps 12 in the run; other kinds 0; quick-step frames 641 (animator speed boosted in 230, max 1.30); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.10 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | PASS | blocked hits 1 (player recoiled 1), guards broken 1 (charged 1, poise 0) in the run |  |
| Interrupts | Dodge roll direction per enemy (never toward the player; Humadroid included) | PASS | AUDIT_W_int: 1 rolls, toward the player 0, max dir/disp dot vs the player -1.00, back 1 / side 0, mean moved 2.2m / AUDIT_W_int_4: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 2, mean moved 2.5m / AUDIT_W_int_3_Robot: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 3, mean moved 2.6m / AUDIT_W_int_5: 5 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 3 / side 2, mean moved 2.3m / AUDIT_W_int_2_Humadroid: 1 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 1, mean moved 2.6m |  |
| Interrupts | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | PASS | 13 rolls: forward Roll clip 0, back 5, left 6, right 2; animator state never seen 0; ended early (not complete) 0 [] | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete |
| Interrupts | Dodge roll: direction vs displacement, movement completes with the clip | PASS | 13 started, 13 finished (reasons: complete x13); full distance (>=90% of plan) 13, short while complete 0; toward the player 0; back 5, side 8; mean moved 2.43m, mean animation 0.58s, mean elapsed 0.78s; per roll: back 2.2/2.2m anim 0.50s complete / left 2.6/2.6m anim 0.30s complete / back 2.2/2.2m anim 0.59s complete / left 2.6/2.6m anim 0.14s complete / right 2.6/2.6m anim 0.69s complete / right 2.6/2.6m anim 0.72s complete / left 2.6/2.6m anim 0.85s complete / back 2.2/2.2m anim 0.57s complete / left 2.6/2.6m anim 0.72s complete / back 2.2/2.2m anim 0.59s complete / back 2.2/2.2m anim 0.56s complete / left 2.6/2.6m anim 0.66s complete / left 2.6/2.6m anim 0.68s complete |  |
| Interrupts | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | PASS | 5 dodge strike(s) (reasons: handover x5); mean weave 0.83m of 0.83m planned; max distance change to the target 0.54m (includes the player moving); without token 0; clip never played 0; swing started after the dodge 4x; overridden by a defensive roll 0 |  |
| Interrupts | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | WARN | AUDIT_W_int: 4 swings, start mean 1.52 (max 1.93, first swing 1.93) vs weapon tip reach 1.70, gate 2.15 / AUDIT_W_int_3_Robot: 7 swings, start mean 1.89 (max 2.17, first swing 2.06) vs weapon tip reach 2.04, gate 2.18 / AUDIT_W_int_4: 5 swings, start mean 1.82 (max 2.03, first swing 1.58) vs weapon tip reach 1.04, gate 2.08 / AUDIT_W_int_5: 5 swings, start mean 1.83 (max 1.93, first swing 1.52) vs weapon tip reach 1.59, gate 2.03 / AUDIT_W_int_2_Humadroid: 8 swings, start mean 1.76 (max 2.06, first swing 2.06) vs weapon tip reach 1.88, gate 2.27 | an enemy swung from more than its weapon reach + 0.45 m |
| Interrupts | Swing-start distance vs weapon reach (first vs later swings) | PASS | first swing per enemy: 5 swings: start distance mean 1.83m (max 2.06), attack range 2.27, swing-start gate mean 2.11, weapon length 1.56m, weapon tip reach from root mean 1.73m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) / later swings: 24 swings: start distance mean 1.77m (max 2.17), attack range 2.27, swing-start gate mean 2.17, weapon length 1.64m, weapon tip reach from root mean 1.69m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) |  |
| Interrupts | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | PASS | AUDIT_W_int: 4 swing seq, first swing 0.5s, tokens 6, token-eligible 96% of samples, Engager 14% / AUDIT_W_int_2_Humadroid: 8 swing seq, first swing 10.0s, tokens 6, token-eligible 94% of samples, Engager 20% / AUDIT_W_int_3_Robot: 7 swing seq, first swing 3.1s, tokens 7, token-eligible 93% of samples, Engager 26% / AUDIT_W_int_4: 5 swing seq, first swing 5.1s, tokens 6, token-eligible 91% of samples, Engager 19% / AUDIT_W_int_5: 5 swing seq, first swing 7.8s, tokens 6, token-eligible 90% of samples, Engager 22%; most/least swings 8/4 |  |
| Interrupts | Audit player: melee hits landed (needed for block / interrupt checks) | PASS | hits=AUDIT_W_int:7,AUDIT_W_int_2_Humadroid:12,AUDIT_W_int_3_Robot:13,AUDIT_W_int_4:8,AUDIT_W_int_5:7 attackFrames=1075 minStamina=980 lightClicks=54 pressedFrames=0 flagsAtClick={isAttacking:51} armed=1 |  |
| Interrupts | Hit FX scaling (blood / coolant / sparks use one scaling path) | PASS | Blood: light x1.06 (n 11), heavy x1.42 (n 10), chain>=2 x1.21 (n 4) / SparksSmall: light x1.08 (n 8), heavy x1.46 (n 5), chain>=2 x1.32 (n 6) / Coolant: light x1.10 (n 4), heavy x1.45 (n 8), chain>=2 x1.37 (n 5) // per enemy: AUDIT_W_int: Blood x6 / AUDIT_W_int_3_Robot: SparksSmall x13 / AUDIT_W_int_4: Blood x8 / AUDIT_W_int_5: Blood x7 / AUDIT_W_int_2_Humadroid: Coolant x12 |  |
| Interrupts | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | PASS | Backstep x2 moved 0.45m of 0.45m planned (complete 2); HolderSidestep x12 moved 1.11m of 1.33m planned (complete 11, cancel 1); Strafe x2 moved 0.52m of 0.91m planned (attack-ready 2); step animation end vs move end: n 43, mean lag 0 ms, max 0 ms, over 150 ms: 0; zero-displacement steps 0, short while complete 1 |  |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: 10.7:Holder, 15.7:Holder, 16.6:Engager, 18.4:Holder, 23.9:Holder, 28.4:Holder, 32.8:Holder, 44.5:Holder, 50.9:Holder, 62.3:Holder, 63.3:Holder, 66.9:Holder (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
| Interrupts | Multi-enemy (5 x SwordMaster): token rotation / spacing | PASS | 81 role samples; max simultaneous Engagers 1; distinct enemies that held Engager 5/5; hand-offs 29 |  |
| Interrupts | No stacking / circling (multi-enemy) | WARN | min enemy-enemy distance 0.35m, frames with a pair under 0.7m: 13/1210; circling suspects (>720deg yaw, <3m net): none; pairs under 0.7m by state/role: Attack/Engager + Attack/Holder x6 / Chase/Engager + Hold/Holder x4 / Defensive/Engager + Hold/Holder x3; path per enemy: AUDIT_W_int 53m, AUDIT_W_int_2_Humadroid 48m, AUDIT_W_int_3_Robot 55m, AUDIT_W_int_4 50m, AUDIT_W_int_5 53m |  |
| Interrupts | Overlapping attack windows (one attacker at a time, all enemy types, swings still finishing) | PASS | windows=0 seconds=0.00 max=1 samples= |  |
| Interrupts | Camera distance to the player pivot during the fight (enemies never pull the lens into the player) | PASS | min=2.33 closeFrames=0 of=1210 |  |
| Interrupts | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | WARN | 9 episodes: AUDIT_W_int_2_Humadroid x2 (blocked 1, stagger 1, attacking 0, quick 1, suppress 0, mean speed 1.35, clips Idle) / AUDIT_W_int_4 x3 (blocked 1, stagger 1, attacking 0, quick 1, suppress 0, mean speed 2.10, clips Idle) / AUDIT_W_int_5 x4 (blocked 2, stagger 2, attacking 0, quick 2, suppress 0, mean speed 1.23, clips Idle/AimedIdle) | code-driven displacement without a leg cycle |
| Interrupts | Spawn pose (idle hand distance in the first 4 s; under 0.35 = crossed arms) | FAIL | AUDIT_W_int minHand=0.12 clip=AimedIdle_at_74.8s lowSamples=189/204 firstLowAt=0.5s clip=Idle upperId=-99 moveSet=-99 attackId=1 weapon=Blade_Of_Chaos / AUDIT_W_int_2_Humadroid minHand=0.12 clip=Idle_at_27.4s lowSamples=181/204 firstLowAt=0.5s clip=Idle upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword / AUDIT_W_int_3_Robot minHand=0.07 clip=AimedIdle_at_14.7s lowSamples=186/204 firstLowAt=0.5s clip=Walk upperId=-99 moveSet=-99 attackId=0 weapon=weap2_sword / AUDIT_W_int_4 minHand=0.10 clip=mixamo.com_at_5.3s lowSamples=187/204 firstLowAt=0.5s clip=Idle upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos / AUDIT_W_int_5 minHand=0.12 clip=Idle_at_74.8s lowSamples=182/204 firstLowAt=0.5s clip=AimedWalkRight_InPlace upperId=-99 moveSet=-99 attackId=0 weapon=Blade_Of_Chaos |  |
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
