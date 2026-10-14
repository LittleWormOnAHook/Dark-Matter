# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-11 16:47:09 -> 2026-10-11 16:48:41 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 3; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit 6E9F1C game 388004 gear FB6DFE; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was False). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m31s

**1 enemies audited - 0 FAIL, 3 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 10 PASS, 0 FAIL, 9 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 20 change(s).

- NEW enemy|SwordMaster|int.counts = WARN
- NEW enemy|SwordMaster|int.early = PASS
- NEW enemy|SwordMaster|int.immunity = PASS
- NEW enemy|SwordMaster|int.recovery = PASS
- NEW enemy|SwordMaster|int.cadence = PASS
- NEW enemy|SwordMaster|int.moves = PASS
- NEW enemy|SwordMaster|int.block = WARN
- NEW enemy|SwordMaster|int.roll = WARN
- NEW enemy|SwordMaster|int.token = PASS
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
| SwordMaster | enemy | 81.5 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|SwordMaster | setup | 81.5 | 2088 | 39.7 | 1491.8 | 0.50 | 0.00 | 13.47 |
| enemy|SwordMaster | cleanup | 0.2 | 7 | 32.9 | 43.9 | 0.33 | 0.00 | 11.78 |

Whole run (ran units only): 82s over 2095 frames, avg frame 39.7 ms, physics 0.50 ms, render 0.00 ms, scripts 13.47 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | -5 | WARN | Interrupts by reason (SwordMaster, 75s, 54 light + 9 charged swings) | SwordMaster | no interrupt fired: enemy never swung while the player hit it (check logs), or enableAttackInterrupts is off | Inspect the measured value and cause on the detail row. |
| 2 | -5 | WARN | Enemy block (brain Defend -> guard, blocked hits, player recoil) | SwordMaster | no enemy block landed (block needs a Defend decision + a player hit inside the block window) | Inspect the measured value and cause on the detail row. |
| 3 | -5 | WARN | Dodge roll direction vs displacement (dot with direction to the player must be <= 0) | SwordMaster |  | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | No circling while unaware (patrol-anchor fix) on every enemy | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 9 | -5 | WARN | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

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
| Interrupts | Interrupts by reason (SwordMaster, 75s, 54 light + 9 charged swings) | WARN | 0 interrupt(s): none; skipped: immunity x0, super-armor x0, committed x0; chance rolls that failed x0; heavy hits seen x0; poise-break stagger events x0; enemy swing sequences started 32; interrupt rate 0% | no interrupt fired: enemy never swung while the player hit it (check logs), or enableAttackInterrupts is off |
| Interrupts | Early-window / commit-window rules | PASS | early-window interrupts all inside the profile window (0.20): True; no non-poise interrupt at or past the 0.45 commit window: True |  |
| Interrupts | Immunity window (2s, no repeat chance interrupts) | PASS | 0 repeat interrupt(s) inside 2s of the previous one (poise break excepted); immunity blocked 0 hit(s) |  |
| Interrupts | Recovery lock (1.5s no new attack after an interrupt) | PASS | 0 interrupt(s) followed by a new swing; shortest gap n/a; violations 0 |  |
| Interrupts | Cadence (swing-to-swing, recovery pause) | PASS | swing-to-swing mean 2.47s (min 1.38, max 3.15, n 31); end-to-next-start gap mean 0.69s (n 31); old model mean about 2.1s swing-to-swing |  |
| Interrupts | Slice 3 cooldown moves (engager strafe / backstep, holder sidestep) | PASS | engager strafes 3, engager backsteps 3, holder sidesteps 0 in the run; other kinds 0; quick-step frames 0 (animator speed boosted in 0, max 1.00); animator speed above 1 outside quick steps (max, 1.00 = none; combo/hit-stop speeds count): 1.00 |  |
| Interrupts | Enemy block (brain Defend -> guard, blocked hits, player recoil) | WARN | blocked hits 0 (player recoiled 0), guards broken 0 (charged 0, poise 0) in the run | no enemy block landed (block needs a Defend decision + a player hit inside the block window) |
| Interrupts | Dodge roll direction vs displacement (dot with direction to the player must be <= 0) | WARN | no dodge roll was logged |  |
| Interrupts | Token / role kept through interrupts | PASS | engagement role at each interrupt: no interrupts (single enemy: role must stay Engager; a two-enemy hand-off check is not part of this run) |  |
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
