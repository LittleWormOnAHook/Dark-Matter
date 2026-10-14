# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-11 18:30:04 -> 2026-10-11 18:31:34 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 3; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit A7D74E game 8F1F5E gear FB6DFE; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was False). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m30s

**1 enemies audited - 0 FAIL, 4 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 17 PASS, 0 FAIL, 10 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 28 change(s).

- NEW enemy|SwordMaster|int.counts = WARN
- NEW enemy|SwordMaster|int.early = PASS
- NEW enemy|SwordMaster|int.immunity = PASS
- NEW enemy|SwordMaster|int.recovery = PASS
- NEW enemy|SwordMaster|int.cadence = PASS
- NEW enemy|SwordMaster|int.moves = PASS
- NEW enemy|SwordMaster|int.block = WARN
- NEW enemy|SwordMaster|int.roll.byenemy = PASS
- NEW enemy|SwordMaster|int.roll = PASS
- NEW enemy|SwordMaster|int.dodgestrike = PASS
- NEW enemy|SwordMaster|int.swingreach = PASS
- NEW enemy|SwordMaster|int.fairness = PASS
- NEW enemy|SwordMaster|int.playerhits = WARN
- NEW enemy|SwordMaster|int.steps = PASS
- NEW enemy|SwordMaster|int.token = PASS
- NEW enemy|SwordMaster|int.multi = PASS
- NEW enemy|SwordMaster|int.stack = WARN
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
| SwordMaster | enemy | 80.2 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 80.2 | 1509 | 54.0 | 1351.3 | 0.88 | 0.00 | 24.64 |
| enemy|SwordMaster | cleanup | 0.2 | 4 | 62.6 | 85.2 | 0.76 | 0.00 | 20.03 |

Whole run (ran units only): 80s over 1513 frames, avg frame 54.0 ms, physics 0.88 ms, render 0.00 ms, scripts 24.63 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | -5 | WARN | Interrupts by reason (SwordMaster, 75s, 54 light + 8 charged swings) | SwordMaster | no interrupt fired: enemy never swung while the player hit it (check logs), or enableAttackInterrupts is off | Inspect the measured value and cause on the detail row. |
| 2 | -5 | WARN | Enemy block (brain Defend -> guard, blocked hits, player recoil) | SwordMaster | no enemy block landed (block needs a Defend decision + a player hit inside the block window) | Inspect the measured value and cause on the detail row. |
| 3 | -5 | WARN | Audit player: melee hits landed (needed for block / interrupt checks) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | No stacking / circling (multi-enemy) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `` - melee `` / ranged `` - audited in 80s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Interrupts | Interrupts by reason (SwordMaster, 75s, 54 light + 8 charged swings) | WARN | 0 interrupt(s): none; skipped: immunity x0, super-armor x0, committed x0; chance rolls that failed x0; heavy hits seen x0; poise-break stagger events x0; enemy swing sequences started 23; interrupt rate 0% | no interrupt fired: enemy never swung while the player hit it (check logs), or enableAttackInterrupts is off |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 0 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 0 interrupt(s) followed by a new swing; shortest gap n/a; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 14.93s (min 1.51, max 30.45, n 18); end-to-next-start gap mean 13.26s (n 18); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 5, engager backsteps 4, holder sidesteps 11 in the run; other kinds 0; quick-step frames 1019 (animator speed boosted in 190, max 1.30); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.10 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | WARN | blocked hits 0 (player recoiled 0), guards broken 0 (charged 0, poise 0) in the run | no enemy block landed (block needs a Defend decision + a player hit inside the block window) |
| Interrupts | Dodge roll direction per enemy (never toward the player; Humadroid included) | PASS | AUDIT_W_int: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 2, mean moved 2.9m / AUDIT_W_int_5: 2 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 1, mean moved 2.7m / AUDIT_W_int_2_Humadroid: 2 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 2, mean moved 3.2m / AUDIT_W_int_4: 1 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 0 / side 1, mean moved 3.2m / AUDIT_W_int_3_Robot: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 2, mean moved 2.9m |  |
| Interrupts | Dodge roll: direction vs displacement, movement completes with the clip | PASS | 11 started, 11 finished (reasons: complete x11); full distance (>=90% of plan) 11, short while complete 0; toward the player 0; back 3, side 8; mean moved 2.92m, mean animation 0.83s, mean elapsed 0.97s; per roll: roll 3.2/3.2m anim 1.03s complete / back 2.2/2.2m anim 0.63s complete / roll 3.2/3.2m anim 0.96s complete / roll 3.2/3.2m anim 0.98s complete / roll 3.2/3.2m anim 1.01s complete / roll 3.2/3.2m anim 0.97s complete / back 2.2/2.2m anim 0.56s complete / roll 3.2/3.2m anim 0.99s complete / roll 3.2/3.2m anim 0.98s complete / back 2.2/2.2m anim 0.00s complete / roll 3.2/3.2m anim 1.03s complete |  |
| Interrupts | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | PASS | 5 dodge strike(s) (reasons: handover x5); mean weave 0.80m of 0.80m planned; max distance change to the target 0.04m (includes the player moving); without token 0; clip never played 0; swing started after the dodge 5x; overridden by a defensive roll 1 |  |
| Interrupts | Swing-start distance vs weapon reach (first vs later swings) | PASS | first swing per enemy: 5 swings: start distance mean 1.89m (max 2.11), attack range 2.27, swing-start gate mean 2.11, weapon length 1.56m, weapon tip reach from root mean 2.43m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) / later swings: 18 swings: start distance mean 1.76m (max 2.23), attack range 2.27, swing-start gate mean 2.09, weapon length 1.48m, weapon tip reach from root mean 2.31m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) |  |
| Interrupts | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | PASS | AUDIT_W_int: 7 swing seq, first swing 2.2s, tokens 5, token-eligible 98% of samples, Engager 21% / AUDIT_W_int_2_Humadroid: 3 swing seq, first swing 19.1s, tokens 4, token-eligible 98% of samples, Engager 16% / AUDIT_W_int_3_Robot: 3 swing seq, first swing 15.8s, tokens 5, token-eligible 93% of samples, Engager 22% / AUDIT_W_int_4: 5 swing seq, first swing 8.5s, tokens 5, token-eligible 99% of samples, Engager 21% / AUDIT_W_int_5: 5 swing seq, first swing 12.0s, tokens 5, token-eligible 98% of samples, Engager 20%; most/least swings 7/3 |  |
| Interrupts | Audit player: melee hits landed (needed for block / interrupt checks) | WARN | hits=AUDIT_W_int:0,AUDIT_W_int_2_Humadroid:0,AUDIT_W_int_3_Robot:0,AUDIT_W_int_4:0,AUDIT_W_int_5:0 attackFrames=0 minStamina=1000 armed=1 |  |
| Interrupts | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | PASS | HolderSidestep x11 moved 1.24m of 1.45m planned (complete 10, cancel 1); Backstep x3 moved 0.40m of 0.40m planned (complete 3); Strafe x2 moved 0.98m of 1.00m planned (complete 2); step animation end vs move end: n 35, mean lag 0 ms, max 0 ms, over 150 ms: 0; zero-displacement steps 0, short while complete 0 |  |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: no interrupts (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
| Interrupts | Multi-enemy (5 x SwordMaster): token rotation / spacing | PASS | 81 role samples; max simultaneous Engagers 1; distinct enemies that held Engager 5/5; hand-offs 22 |  |
| Interrupts | No stacking / circling (multi-enemy) | WARN | min enemy-enemy distance 0.63m, frames with a pair under 0.7m: 2/1190; circling suspects (>720deg yaw, <3m net): none; pairs under 0.7m by state/role: Defensive/Holder + Hold/Holder x2; path per enemy: AUDIT_W_int 47m, AUDIT_W_int_2_Humadroid 30m, AUDIT_W_int_3_Robot 37m, AUDIT_W_int_4 39m, AUDIT_W_int_5 40m |  |
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
