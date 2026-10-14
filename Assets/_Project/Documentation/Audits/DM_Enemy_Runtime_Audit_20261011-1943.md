# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-11 19:42:16 -> 2026-10-11 19:43:48 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 3; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit 513A39 game 8C3D21 gear FB6DFE; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was False). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m31s

**1 enemies audited - 1 FAIL, 4 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 22 PASS, 1 FAIL, 10 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 34 change(s).

- NEW enemy|SwordMaster|int.counts = WARN
- NEW enemy|SwordMaster|int.early = PASS
- NEW enemy|SwordMaster|int.immunity = PASS
- NEW enemy|SwordMaster|int.recovery = PASS
- NEW enemy|SwordMaster|int.cadence = PASS
- NEW enemy|SwordMaster|int.moves = PASS
- NEW enemy|SwordMaster|int.block = WARN
- NEW enemy|SwordMaster|int.roll.byenemy = PASS
- NEW enemy|SwordMaster|int.roll.clips = PASS
- NEW enemy|SwordMaster|int.roll = PASS
- NEW enemy|SwordMaster|int.dodgestrike = PASS
- NEW enemy|SwordMaster|int.swingreach.byenemy = PASS
- NEW enemy|SwordMaster|int.swingreach = PASS
- NEW enemy|SwordMaster|int.fairness = PASS
- NEW enemy|SwordMaster|int.playerhits = WARN
- NEW enemy|SwordMaster|int.steps = PASS
- NEW enemy|SwordMaster|int.token = PASS
- NEW enemy|SwordMaster|int.multi = PASS
- NEW enemy|SwordMaster|int.stack = PASS
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

Wall time 91s (1.5 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| SwordMaster | enemy | 79.2 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 79.2 | 2174 | 38.2 | 3962.6 | 0.53 | 0.00 | 15.79 |
| enemy|SwordMaster | cleanup | 0.2 | 6 | 35.4 | 51.2 | 0.38 | 0.00 | 11.35 |

Whole run (ran units only): 79s over 2180 frames, avg frame 38.2 ms, physics 0.53 ms, render 0.00 ms, scripts 15.78 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 20 | FAIL | Spawn pose (idle hand distance in the first 4 s; under 0.35 = crossed arms) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 2 | -5 | WARN | Interrupts by reason (SwordMaster, 75s, 56 light + 9 charged swings) | SwordMaster | no interrupt fired: enemy never swung while the player hit it (check logs), or enableAttackInterrupts is off | Inspect the measured value and cause on the detail row. |
| 3 | -5 | WARN | Enemy block (brain Defend -> guard, blocked hits, player recoil) | SwordMaster | no enemy block landed (block needs a Defend decision + a player hit inside the block window) | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | Audit player: melee hits landed (needed for block / interrupt checks) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | SwordMaster | code-driven displacement without a leg cycle | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 9 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 10 | -5 | WARN | No circling while unaware (patrol-anchor fix) on every enemy | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 11 | -5 | WARN | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

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
| Interrupts | Interrupts by reason (SwordMaster, 75s, 56 light + 9 charged swings) | WARN | 0 interrupt(s): none; skipped: immunity x0, super-armor x0, committed x0; chance rolls that failed x0; heavy hits seen x0; poise-break stagger events x0; enemy swing sequences started 29; interrupt rate 0% | no interrupt fired: enemy never swung while the player hit it (check logs), or enableAttackInterrupts is off |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 0 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 0 interrupt(s) followed by a new swing; shortest gap n/a; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 8.05s (min 1.38, max 18.59, n 26); end-to-next-start gap mean 6.43s (n 26); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 5, engager backsteps 3, holder sidesteps 16 in the run; other kinds 0; quick-step frames 1603 (animator speed boosted in 292, max 1.30); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.10 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | WARN | blocked hits 0 (player recoiled 0), guards broken 0 (charged 0, poise 0) in the run | no enemy block landed (block needs a Defend decision + a player hit inside the block window) |
| Interrupts | Dodge roll direction per enemy (never toward the player; Humadroid included) | PASS | AUDIT_W_int_2: 4 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 3, mean moved 2.5m / AUDIT_W_int: 4 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 3 / side 1, mean moved 2.3m / AUDIT_W_int_3: 3 rolls, toward the player 0, max dir/disp dot vs the player 0.00, back 1 / side 2, mean moved 2.4m |  |
| Interrupts | Roll clip actually used per roll (no forward Roll clip; side = DM_DodgeLeft/Right, back = DM_DodgeBack) | PASS | 11 rolls: forward Roll clip 0, back 5, left 3, right 3; animator state never seen 0; ended early (not complete) 0 [] | a roll used the forward Roll clip, its clip state was never entered, or it ended before complete |
| Interrupts | Dodge roll: direction vs displacement, movement completes with the clip | PASS | 11 started, 11 finished (reasons: complete x11); full distance (>=90% of plan) 11, short while complete 0; toward the player 0; back 5, side 6; mean moved 2.40m, mean animation 0.62s, mean elapsed 0.69s; per roll: left 2.6/2.6m anim 0.68s complete / right 2.6/2.6m anim 0.68s complete / back 2.2/2.2m anim 0.54s complete / back 2.2/2.2m anim 0.54s complete / left 2.6/2.6m anim 0.66s complete / right 2.6/2.6m anim 0.67s complete / back 2.2/2.2m anim 0.53s complete / right 2.6/2.6m anim 0.69s complete / back 2.2/2.2m anim 0.53s complete / back 2.2/2.2m anim 0.55s complete / left 2.6/2.6m anim 0.73s complete |  |
| Interrupts | Dodge strike (HumanM@Dodge01 -> quick swing): token, spacing, hand-over | PASS | 3 dodge strike(s) (reasons: handover x3); mean weave 0.79m of 0.79m planned; max distance change to the target 0.03m (includes the player moving); without token 0; clip never played 0; swing started after the dodge 3x; overridden by a defensive roll 0 |  |
| Interrupts | Swing-start distance per enemy (Humadroid / Robot use their own weapon reach) | PASS | AUDIT_W_int: 8 swings, start mean 1.85 (max 1.99, first swing 1.98) vs weapon tip reach 2.30, gate 2.09 / AUDIT_W_int_2: 11 swings, start mean 1.86 (max 2.02, first swing 1.99) vs weapon tip reach 2.40, gate 2.03 / AUDIT_W_int_3: 9 swings, start mean 1.72 (max 2.03, first swing 2.03) vs weapon tip reach 2.35, gate 2.03 |  |
| Interrupts | Swing-start distance vs weapon reach (first vs later swings) | PASS | first swing per enemy: 3 swings: start distance mean 2.00m (max 2.03), attack range 2.27, swing-start gate mean 2.11, weapon length 1.38m, weapon tip reach from root mean 2.37m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) / later swings: 25 swings: start distance mean 1.79m (max 2.02), attack range 2.27, swing-start gate mean 2.04, weapon length 1.38m, weapon tip reach from root mean 2.35m, closest weapon-to-player gap mean -0.26m, out of reach (gap>0.15m) 0 (0%) |  |
| Interrupts | Attack fairness across enemies (swing sequences, time to first attack, token eligibility) | PASS | AUDIT_W_int: 8 swing seq, first swing 1.4s, tokens 7, token-eligible 97% of samples, Engager 28% / AUDIT_W_int_2: 11 swing seq, first swing 3.0s, tokens 6, token-eligible 95% of samples, Engager 38% / AUDIT_W_int_3: 9 swing seq, first swing 7.6s, tokens 6, token-eligible 95% of samples, Engager 34%; most/least swings 11/8 |  |
| Interrupts | Audit player: melee hits landed (needed for block / interrupt checks) | WARN | hits=AUDIT_W_int:0,AUDIT_W_int_2:0,AUDIT_W_int_3:0 attackFrames=0 minStamina=1000 lightClicks=56 pressedFrames=0 flagsAtClick={} armed=1 |  |
| Interrupts | Quick steps (strafe / backstep / holder sidestep): displacement vs plan | PASS | HolderSidestep x16 moved 1.14m of 1.24m planned (complete 15, cancel 1); Backstep x3 moved 0.44m of 0.44m planned (complete 3); Strafe x1 moved 0.71m of 1.06m planned (attack-ready 1); step animation end vs move end: n 31, mean lag 0 ms, max 0 ms, over 150 ms: 0; zero-displacement steps 0, short while complete 0 |  |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: no interrupts (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
| Interrupts | Multi-enemy (3 x SwordMaster): token rotation / spacing | PASS | 119 role samples; max simultaneous Engagers 1; distinct enemies that held Engager 3/3; hand-offs 18 |  |
| Interrupts | No stacking / circling (multi-enemy) | PASS | min enemy-enemy distance 1.94m, frames with a pair under 0.7m: 0/1816; circling suspects (>720deg yaw, <3m net): none; pairs under 0.7m by state/role: none; path per enemy: AUDIT_W_int 37m, AUDIT_W_int_2 46m, AUDIT_W_int_3 57m |  |
| Interrupts | Overlapping attack windows (one attacker at a time, all enemy types, swings still finishing) | PASS | windows=0 seconds=0.00 max=1 samples= |  |
| Interrupts | Camera distance to the player pivot during the fight (enemies never pull the lens into the player) | PASS | min=2.50 closeFrames=0 of=1816 |  |
| Interrupts | Sliding without leg animation (body moving > 0.4 m/s with no locomotion blend, rolls / dodge strikes excluded) | WARN | 1 episodes: AUDIT_W_int_3 x1 (blocked 0, stagger 0, attacking 0, quick 0, suppress 0, mean speed 1.78, clips Idle) | code-driven displacement without a leg cycle |
| Interrupts | Spawn pose (idle hand distance in the first 4 s; under 0.35 = crossed arms) | FAIL | AUDIT_W_int minHand=0.13 clip=AimedWalkRight_InPlace at 1.8s / AUDIT_W_int_2 minHand=0.13 clip=WalkTurnLeft at 1.8s / AUDIT_W_int_3 minHand=0.13 clip=Idle at 0.7s |  |
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
