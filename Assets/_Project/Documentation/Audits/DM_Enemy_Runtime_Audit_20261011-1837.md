# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-11 18:36:19 -> 2026-10-11 18:37:49 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 3; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit CC8487 game 8F1F5E gear FB6DFE; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m29s

**1 enemies audited - 1 FAIL, 1 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 20 PASS, 1 FAIL, 7 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 29 change(s).

- NEW enemy|SwordMaster|int.counts = PASS
- NEW enemy|SwordMaster|int.early = PASS
- NEW enemy|SwordMaster|int.immunity = PASS
- NEW enemy|SwordMaster|int.recovery = PASS
- NEW enemy|SwordMaster|int.cadence = PASS
- NEW enemy|SwordMaster|int.moves = PASS
- NEW enemy|SwordMaster|int.block = PASS
- NEW enemy|SwordMaster|int.roll.byenemy = PASS
- NEW enemy|SwordMaster|int.roll = FAIL
- NEW enemy|SwordMaster|int.dodgestrike = PASS
- NEW enemy|SwordMaster|int.swingreach = PASS
- NEW enemy|SwordMaster|int.fairness = PASS
- NEW enemy|SwordMaster|int.playerhits = PASS
- NEW enemy|SwordMaster|int.hitfx = PASS
- NEW enemy|SwordMaster|int.steps = WARN
- NEW enemy|SwordMaster|int.token = PASS
- NEW enemy|SwordMaster|int.multi = PASS
- NEW enemy|SwordMaster|int.stack = PASS
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

Wall time 90s (1.5 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| SwordMaster | enemy | 79.9 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 79.9 | 1325 | 61.3 | 1359.1 | 1.02 | 0.00 | 22.36 |
| enemy|SwordMaster | cleanup | 0.2 | 3 | 74.7 | 87.5 | 0.87 | 0.00 | 17.12 |

Whole run (ran units only): 80s over 1328 frames, avg frame 61.3 ms, physics 1.02 ms, render 0.00 ms, scripts 22.34 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 20 | FAIL | Dodge roll: direction vs displacement, movement completes with the clip | SwordMaster | an enemy rolled toward the player, stopped short while the clip kept playing, or a roll never finished | Inspect the measured value and cause on the detail row. |
| 2 | -5 | WARN | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | SwordMaster | a step started but the enemy did not move (blocked / interrupted before the first frames) | Inspect the measured value and cause on the detail row. |
| 3 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | No circling while unaware (patrol-anchor fix) on every enemy | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `` - melee `` / ranged `` - audited in 80s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Interrupts | Interrupts by reason (SwordMaster, 75s, 51 light + 8 charged swings) | PASS | 14 interrupt(s): early window x6, heavy hit vs light swing x4, poise break x3, chance x1; skipped: immunity x1, super-armor x0, committed x8; chance rolls that failed x1; heavy hits seen x12; poise-break stagger events x20; enemy swing sequences started 27; interrupt rate 52% |  |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 1 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 14 interrupt(s) followed by a new swing; shortest gap 1.57s; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 8.46s (min 1.63, max 27.11, n 24); end-to-next-start gap mean 7.88s (n 9); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 8, engager backsteps 4, holder sidesteps 13 in the run; other kinds 0; quick-step frames 531 (animator speed boosted in 224, max 1.30); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.10 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | PASS | blocked hits 0 (player recoiled 0), guards broken 2 (charged 2, poise 0) in the run |  |
| Interrupts | Dodge roll direction per enemy (never toward the player; Humadroid included) | PASS | AUDIT_W_int_3: 5 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 4, mean moved 2.5m / AUDIT_W_int_2: 5 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 3 / side 2, mean moved 2.8m / AUDIT_W_int: 2 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 2, mean moved 3.2m |  |
| Interrupts | Dodge roll: direction vs displacement, movement completes with the clip | FAIL | 12 started, 12 finished (reasons: complete x12); full distance (>=90% of plan) 11, short while complete 1; toward the player 0; back 4, side 8; mean moved 2.76m, mean animation 0.64s, mean elapsed 1.05s; per roll: roll 0.9/3.2m anim 1.15s complete / roll 3.2/3.2m anim 1.11s complete / roll 3.2/3.2m anim 0.36s complete / back 2.2/2.2m anim 0.53s complete / roll 3.2/3.2m anim 0.91s complete / back 3.3/2.2m anim 0.16s complete / roll 3.2/3.2m anim 0.51s complete / back 2.1/2.2m anim 0.00s complete / roll 3.3/3.2m anim 0.55s complete / roll 3.2/3.2m anim 1.00s complete / roll 3.2/3.2m anim 1.04s complete / back 2.2/2.2m anim 0.40s complete | an enemy rolled toward the player, stopped short while the clip kept playing, or a roll never finished |
| Interrupts | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | PASS | 3 dodge strike(s) (reasons: handover x3); mean weave 0.69m of 0.70m planned; max distance change to the target 0.61m (includes the player moving); without token 0; clip never played 0; swing started after the dodge 4x; overridden by a defensive roll 1 |  |
| Interrupts | Swing-start distance vs weapon reach (first vs later swings) | PASS | first swing per enemy: 3 swings: start distance mean 1.77m (max 1.97), attack range 2.27, swing-start gate mean 2.03, weapon length 1.38m, weapon tip reach from root mean 1.14m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) / later swings: 22 swings: start distance mean 1.76m (max 2.03), attack range 2.27, swing-start gate mean 2.05, weapon length 1.38m, weapon tip reach from root mean 1.62m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) |  |
| Interrupts | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | PASS | AUDIT_W_int: 8 swing seq, first swing 0.4s, tokens 6, token-eligible 88% of samples, Engager 26% / AUDIT_W_int_2: 8 swing seq, first swing 9.0s, tokens 6, token-eligible 89% of samples, Engager 36% / AUDIT_W_int_3: 9 swing seq, first swing 3.6s, tokens 6, token-eligible 85% of samples, Engager 38%; most/least swings 9/8 |  |
| Interrupts | Audit player: melee hits landed (needed for block / interrupt checks) | PASS | hits=AUDIT_W_int:16,AUDIT_W_int_2:14,AUDIT_W_int_3:19 attackFrames=913 minStamina=923 lightClicks=51 pressedFrames=0 flagsAtClick={isAttacking:48,lockMeleeInput:1,lockInput:1} armed=1 |  |
| Interrupts | Hit FX scaling (blood / coolant / sparks use one scaling path) | PASS | Blood: light x1.10 (n 22), heavy x1.48 (n 27), chain>=2 x1.37 (n 25) |  |
| Interrupts | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | WARN | Backstep x4 moved 0.59m of 0.59m planned (complete 4); HolderSidestep x13 moved 1.09m of 1.27m planned (complete 10, cancel 2, timeout 1); Strafe x8 moved 0.82m of 1.02m planned (complete 5, attack-ready 1, interrupted 1, timeout 1); step animation end vs move end: n 41, mean lag 0 ms, max 0 ms, over 150 ms: 0; zero-displacement steps 1, short while complete 0 | a step started but the enemy did not move (blocked / interrupted before the first frames) |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: 6.8:Engager, 9.1:Holder, 25.9:Holder, 29.0:Engager, 32.4:Holder, 35.2:Holder, 44.3:Holder, 46.2:Holder, 55.2:Holder, 60.9:Holder, 63.8:Holder, 64.5:Holder (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
| Interrupts | Multi-enemy (3 x SwordMaster): token rotation / spacing | PASS | 73 role samples; max simultaneous Engagers 1; distinct enemies that held Engager 3/3; hand-offs 17 |  |
| Interrupts | No stacking / circling (multi-enemy) | PASS | min enemy-enemy distance 1.16m, frames with a pair under 0.7m: 0/1028; circling suspects (>720deg yaw, <3m net): none; pairs under 0.7m by state/role: none; path per enemy: AUDIT_W_int 55m, AUDIT_W_int_2 66m, AUDIT_W_int_3 56m |  |
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
