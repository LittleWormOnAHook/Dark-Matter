# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-26 14:27:56 -> 2026-10-26 14:28:25 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.7.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: already clean; attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_Kade_v2 at (-818.4, 21.2, -127.9), hp 500.0/600.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 5; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 10
- Unit store: 49 stored unit(s); stamps audit 159008 game 2DAC22 gear 91622C; skip-unchanged is OFF (Full mode)
- Mode full: 1 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=0; lodBias=1.5; targetFrameRate=-1; qualityLevel=1 (Balanced); qualityRenderPipeline=Genesis_HDRP_Balanced; graphicsDefaultRenderPipeline=Genesis_HDRP_Balanced; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 0m28s

**0 enemies audited - 0 FAIL, 0 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 2 scenarios): 8 PASS, 0 FAIL, 7 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 18 change(s).

- NEW scenario|Humadroid melee lock: damage + spin|terrain = PASS
- NEW scenario|Humadroid melee lock: damage + spin|hm.drawn = PASS
- NEW scenario|Humadroid melee lock: damage + spin|hm.lock = PASS
- NEW scenario|Humadroid melee lock: damage + spin|hm.close = PASS
- NEW scenario|Humadroid melee lock: damage + spin|hm.spin = PASS
- NEW scenario|Humadroid melee lock: damage + spin|hm.damage = PASS
- NEW scenario|Humadroid melee lock: damage + spin|hm.diag = INFO
- NEW scenario|Humadroid melee lock: damage + spin|hm.spin2 = INFO
- NEW scenario|Humadroid melee lock: damage + spin|console = WARN
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

Wall time 29s (0.5 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Humadroid melee lock: damage + spin | scenario | 15.6 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| scenario|Humadroid melee lock: damage + spin | setup | 15.9 | 402 | 39.5 | 408.2 | 0.53 | 0.00 | 11.71 |

Whole run (ran units only): 16s over 402 frames, avg frame 39.5 ms, physics 0.53 ms, render 0.00 ms, scripts 11.71 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 55 | WARN | Console errors/warnings | Humadroid melee lock: damage + spin | [Warning] Setting linear velocity of a kinematic body is not supported. | Fix the exception at the listed stack frame (null-guard or initialise the field); it repeats on every spawn. |
| 2 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 3 | -5 | WARN | Enemy melee hit on the player plays a hit sound (SurvivalStats confirmed loss / god-mode cue -> GameAudioManager.PlayPlayerBodyHit) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | No circling while unaware (patrol-anchor fix) on every enemy | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|

## Weapons that deal NO damage (headline)

- none detected in this run.

## Scenarios (block/parry, movement, multi-enemy, player weapons, performance)

| Scenario | PASS | FAIL | warn | time |
|---|---|---|---|---|
| Humadroid melee lock: damage + spin | 6 | 0 | 1 | 16s |
| Regression of earlier fixes and tweaks | 2 | 0 | 6 | 0s |

### Scenario: Humadroid melee lock: damage + spin

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| HumaMelee | Terrain loaded under player + enemies | PASS | ground 3/3, active terrains 4, waited 0.0s |  |
| HumaMelee | Player has a melee weapon drawn | PASS | drawn=True |  |
| HumaMelee | Melee lock engages vs melee player | PASS | locked=166/169 hold=1.0 |  |
| HumaMelee | Humadroid closes to melee range on its own (<= 1.6 m) | PASS | minDist=1.5 |  |
| HumaMelee | Momentum spin hits + damages the Humadroid | PASS | started=True reject= spinHits=1 hp 200.0->172.0 dist=1.5 |  |
| HumaMelee | Player melee swings damage the Humadroid | PASS | hp 172.0->0.0 hits(gains)=4/10 avgDist=1.3 maxDist=1.5 swingsSeen=4 playerHitBy=0 |  |
| HumaMelee | Contact diagnostics (info) | INFO | frames=56 dmgWindowFrames=4 weaponTriggers=4 overlapDMHitbox=1 overlapAnyEnemyCollider=3 minDist(weapon->chest)=0.2 touched=[DMHitbox:DMHitbox_VBOT_:LeftArm,DMHitbox:DMHitbox_VBOT_:LeftForeArm,DMHitbox:DMHitbox_VBOT_:LeftHand] |  |
| HumaMelee | Second spin after swings (info) | INFO | Humadroid already killed by swings |  |
| Console | Console errors/warnings | WARN | 0 errors, 1 warning(s) | [Warning] Setting linear velocity of a kinematic body is not supported. |

- `[Warning] Setting linear velocity of a kinematic body is not supported.`

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Enemy melee hit on the player plays a hit sound (SurvivalStats confirmed loss / god-mode cue -> GameAudioManager.PlayPlayerBodyHit) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | WARN | 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s); weapon reverts recorded this run: 0 | no Hot Cross row ran (cached or scenario skipped) |
| Fix check | vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons) | PASS | code present: True; console errors across 0 weapon-matrix case(s): 0 |  |
| Fix check | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | WARN | code present: True; dual-slot override case: not run |  |
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 54 value(s) compared on 9 enemies |  |
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
