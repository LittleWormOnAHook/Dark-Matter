# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-15 19:33:56 -> 2026-10-15 19:35:22 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: already clean; attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 6; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 39 stored unit(s); stamps audit 3FFEC6 game 4BCF5B gear E69E18; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m25s

**0 enemies audited - 0 FAIL, 0 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 2 scenarios): 8 PASS, 0 FAIL, 9 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 18 change(s).

- NEW scenario|Obstacle movement: scene rock MeshColliders|rock.setup = PASS
- NEW scenario|Obstacle movement: scene rock MeshColliders|rock.0 = WARN
- NEW scenario|Obstacle movement: scene rock MeshColliders|rock.1 = WARN
- NEW scenario|Obstacle movement: scene rock MeshColliders|rock.2 = WARN
- NEW scenario|Obstacle movement: scene rock MeshColliders|rock.3 = PASS
- NEW scenario|Obstacle movement: scene rock MeshColliders|rock.combat.0 = PASS
- NEW scenario|Obstacle movement: scene rock MeshColliders|rock.combat.1 = PASS
- NEW scenario|Obstacle movement: scene rock MeshColliders|rock.combat.2 = PASS
- NEW scenario|Obstacle movement: scene rock MeshColliders|console = PASS
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

Wall time 86s (1.4 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Obstacle movement: scene rock MeshColliders | scenario | 52.1 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| scenario|Obstacle movement: scene rock MeshColliders | setup | 52.3 | 1024 | 54.8 | 3812.4 | 0.82 | 0.00 | 24.59 |

Whole run (ran units only): 52s over 1024 frames, avg frame 54.8 ms, physics 0.82 ms, render 0.00 ms, scripts 24.59 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | -5 | WARN | Scene rock approach Rock_02_Moss | Obstacle movement: scene rock MeshColliders | Skipped instead of teleporting into/on top of a rock. | Inspect the measured value and cause on the detail row. |
| 2 | -5 | WARN | Scene rock approach Rock_02_Moss | Obstacle movement: scene rock MeshColliders | Skipped instead of teleporting into/on top of a rock. | Inspect the measured value and cause on the detail row. |
| 3 | -5 | WARN | Scene rock approach Rock_02_Moss (17) | Obstacle movement: scene rock MeshColliders | Skipped instead of teleporting into/on top of a rock. | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | No circling while unaware (patrol-anchor fix) on every enemy | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 9 | -5 | WARN | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|

## Weapons that deal NO damage (headline)

- none detected in this run.

## Scenarios (block/parry, movement, multi-enemy, player weapons, performance)

| Scenario | PASS | FAIL | warn | time |
|---|---|---|---|---|
| Obstacle movement: scene rock MeshColliders | 6 | 0 | 3 | 52s |
| Regression of earlier fixes and tweaks | 2 | 0 | 6 | 0s |

### Scenario: Obstacle movement: scene rock MeshColliders

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Rock | Active scene rock MeshColliders | PASS | 13 rock colliders; existing scene geometry retained, not replaced with convex proxies |  |
| Rock | Scene rock approach Rock_02_Moss | WARN | No clear ground-level endpoints found in the existing cluster | Skipped instead of teleporting into/on top of a rock. |
| Rock | Scene rock approach Rock_02_Moss | WARN | No clear ground-level endpoints found in the existing cluster | Skipped instead of teleporting into/on top of a rock. |
| Rock | Scene rock approach Rock_02_Moss (17) | WARN | No clear ground-level endpoints found in the existing cluster | Skipped instead of teleporting into/on top of a rock. |
| Rock | Scene rock approach Rock_02_Moss (17) | PASS | arrived=True; mesh=Rock_02_LOD2, convex=False; samples=85, path=23.69m, movementReversals=0, yawReversals=8, sideFlips=0, routeRecoveries=0, maxYawRate=240.0deg/s, maxStall=0.00s, stationaryWalk=0.00s, penetrationSamples=0, territoryExcess=0.00m; firstStall=[]; yawTrace=[t=7.04 yawDelta=-11.4 move=(-0.332, 0.000, -0.089) pos=(-784.21, 21.27, -63.59) normal=(-0.40, 0.00, 0.91); t=7.97 yawDelta=7.8 move=(-0.111, 0.000, -0.216) pos=(-785.66, 21.27, -65.07) normal=(-0.66, 0.00, 0.75); t=8.29 yawDelta=-7.3 move=(-0.161, 0.000, -0.182) pos=(-786.21, 21.27, -65.61) normal=(-0.79, 0.00, 0.61); t=8.53 yawDelta=9.1 move=(-0.214, 0.000, -0.152) pos=(-786.56, 21.27, -65.99) normal=(-0.52, 0.00, 0.86); t=8.74 yawDelta=-27.9 move=(-0.039, 0.000, -0.254) pos=(-786.75, 21.27, -66.41) normal=(-0.99, 0.00, 0.16); t=8.97 yawDelta=9.7 move=(-0.096, 0.000, -0.018) pos=(-786.91, 21.27, -66.50) normal=(0.74, 0.00, 0.67); t=10.14 yawDelta=-7.6 move=(-0.247, 0.000, -0.051) pos=(-789.49, 21.27, -66.65) normal=(-0.21, 0.00, 0.98); t=13.51 yawDelta=16.7 move=(0.166, 0.000, -0.160) pos=(-790.17, 21.27, -72.76) normal=(-0.80, 0.00, -0.60); ] |  |
| RockCombat | Live group combat around scene rock | PASS | reachedEngagementRange=True, enteredAttack=True; samples=273, path=25.32m, movementReversals=0, yawReversals=1, sideFlips=0, routeRecoveries=0, maxYawRate=240.1deg/s, maxStall=0.00s, stationaryWalk=0.00s, penetrationSamples=0, territoryExcess=0.00m; firstStall=[]; yawTrace=[t=20.13 yawDelta=11.8 move=(0.041, 0.000, -0.617) pos=(-779.35, 21.27, -67.54) normal=(1.00, 0.00, 0.07); ] | Stalls measured only during Chase; Hold/Attack pauses are intentional. Penetration checks cover the sampled rock only. |
| RockCombat | Live group combat around scene rock | PASS | reachedEngagementRange=True, enteredAttack=True; samples=273, path=26.07m, movementReversals=0, yawReversals=3, sideFlips=0, routeRecoveries=0, maxYawRate=203.0deg/s, maxStall=0.00s, stationaryWalk=0.00s, penetrationSamples=0, territoryExcess=0.00m; firstStall=[]; yawTrace=[t=20.13 yawDelta=11.1 move=(0.531, 0.000, -0.410) pos=(-780.26, 21.27, -64.55) normal=(0.71, 0.00, 0.70); t=21.28 yawDelta=-27.9 move=(0.016, 0.000, -0.707) pos=(-779.65, 21.27, -70.02) normal=(1.00, 0.00, 0.02); t=21.43 yawDelta=16.7 move=(-0.002, 0.000, -0.166) pos=(-779.65, 21.27, -70.18) normal=(0.00, 0.00, 0.00); ] | Stalls measured only during Chase; Hold/Attack pauses are intentional. Penetration checks cover the sampled rock only. |
| RockCombat | Live group combat around scene rock | PASS | reachedEngagementRange=True, enteredAttack=True; samples=272, path=36.63m, movementReversals=0, yawReversals=7, sideFlips=0, routeRecoveries=0, maxYawRate=240.0deg/s, maxStall=0.00s, stationaryWalk=0.00s, penetrationSamples=0, territoryExcess=0.00m; firstStall=[]; yawTrace=[t=20.61 yawDelta=-10.5 move=(-0.321, 0.000, -0.550) pos=(-785.55, 21.27, -64.93) normal=(-0.95, 0.00, 0.31); t=20.75 yawDelta=22.4 move=(-0.491, 0.000, -0.411) pos=(-786.04, 21.27, -65.34) normal=(-0.67, 0.00, 0.74); t=21.03 yawDelta=-21.1 move=(-0.269, 0.000, -0.675) pos=(-786.63, 21.27, -66.39) normal=(-0.99, 0.00, 0.12); t=21.28 yawDelta=33.0 move=(-0.329, 0.000, 0.136) pos=(-787.20, 21.27, -66.40) normal=(0.03, 0.00, 1.00); t=21.69 yawDelta=-9.5 move=(-0.661, 0.000, -0.084) pos=(-789.02, 21.27, -66.56) normal=(-0.18, 0.00, 0.98); t=24.26 yawDelta=10.2 move=(0.031, 0.000, -0.249) pos=(-789.43, 21.27, -73.34) normal=(-0.85, 0.00, -0.52); t=24.38 yawDelta=-14.4 move=(0.512, 0.000, -0.290) pos=(-788.92, 21.27, -73.63) normal=(-0.25, 0.00, -0.97); ] | Stalls measured only during Chase; Hold/Attack pauses are intentional. Penetration checks cover the sampled rock only. |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | WARN | 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s); weapon reverts recorded this run: 0 | no Hot Cross row ran (cached or scenario skipped) |
| Fix check | vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons) | PASS | code present: True; console errors across 0 weapon-matrix case(s): 0 |  |
| Fix check | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | WARN | code present: True; dual-slot override case: not run |  |
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 0 value(s) compared on 0 enemies |  |
| Fix check | No circling while unaware (patrol-anchor fix) on every enemy | WARN | 0 PASS / 0 FAIL of 0 enemies |  |
| Fix check | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | WARN | 0 PASS / 0 FAIL of 0 enemies |  |
| Fix check | Profile adjustments made by earlier audit passes | INFO | none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes |  |

## Per-enemy detail

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
