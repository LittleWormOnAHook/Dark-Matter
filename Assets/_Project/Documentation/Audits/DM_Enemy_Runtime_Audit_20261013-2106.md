# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-13 21:04:22 -> 2026-10-13 21:06:05 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 3; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 3
- Unit store: 37 stored unit(s); stamps audit 91CBB6 game 057317 gear 11046A; skip-unchanged is OFF (Full mode)
- Mode full: 3 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was False). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 1m43s

**3 enemies audited - 1 FAIL, 4 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 50 PASS, 1 FAIL, 8 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 44 change(s).

- NEW enemy|Humadroid|perf = PASS
- NEW enemy|Humadroid|melee.miss = INFO
- NEW enemy|Humadroid|ranged.miss = INFO
- CHANGED enemy|Humadroid|player.swing: PASS -> WARN
- CHANGED enemy|Humadroid|hitmarks: WARN -> PASS
- NEW enemy|Humadroid|loot = PASS
- NEW enemy|Robot|perf = PASS
- NEW enemy|Robot|melee.miss = INFO
- NEW enemy|Robot|ranged.miss = INFO
- CHANGED enemy|Robot|player.swing: PASS -> WARN
- CHANGED enemy|Robot|hitmarks: PASS -> WARN
- NEW enemy|Robot|loot = PASS
- NEW enemy|SwordMaster|cfg = PASS
- NEW enemy|SwordMaster|silent.missingscripts = PASS
- NEW enemy|SwordMaster|silent.materials = PASS
- NEW enemy|SwordMaster|silent.animator = PASS
- NEW enemy|SwordMaster|silent.hitboxes = PASS
- NEW enemy|SwordMaster|perf = PASS
- NEW enemy|SwordMaster|unaware = PASS
- NEW enemy|SwordMaster|notice = PASS
- NEW enemy|SwordMaster|natural.damage = INFO
- NEW enemy|SwordMaster|melee.damage = PASS
- NEW enemy|SwordMaster|melee.miss = INFO
- NEW enemy|SwordMaster|melee.vis = INFO
- NEW enemy|SwordMaster|ranged.damage = PASS
- NEW enemy|SwordMaster|ranged.miss = INFO
- NEW enemy|SwordMaster|ranged.vis = INFO
- NEW enemy|SwordMaster|vis = PASS
- NEW enemy|SwordMaster|player.dmg = PASS
- NEW enemy|SwordMaster|player.swing = WARN
- NEW enemy|SwordMaster|hitmarks = PASS
- NEW enemy|SwordMaster|loot = FAIL
- NEW enemy|SwordMaster|death = PASS
- NEW enemy|SwordMaster|anim = INFO
- NEW enemy|SwordMaster|console = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.swish = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hitsound = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hotcross = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hiteffects = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.loadout = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.tuning = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.patrol = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.onedrawn = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.profiles = INFO

## Timing

Wall time 103s (1.7 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Humadroid | enemy | 32.5 | ran |
| Robot | enemy | 30.4 | ran |
| SwordMaster | enemy | 28.1 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|Robot | unaware 9s (+perf) | 9.9 | 225 | 44.2 | 948.1 | 0.47 | 0.00 | 17.27 |
| enemy|Humadroid | unaware 9s (+perf) | 9.3 | 254 | 36.7 | 391.6 | 0.48 | 0.00 | 15.08 |
| enemy|SwordMaster | unaware 9s (+perf) | 9.0 | 254 | 35.5 | 91.2 | 0.43 | 0.00 | 15.58 |
| enemy|Humadroid | death/loot | 6.6 | 170 | 38.8 | 127.3 | 0.58 | 0.00 | 16.68 |
| enemy|Robot | death/loot | 6.6 | 180 | 36.5 | 100.7 | 0.44 | 0.00 | 15.64 |
| enemy|SwordMaster | death/loot | 6.5 | 175 | 37.2 | 84.0 | 0.43 | 0.00 | 16.34 |
| enemy|Humadroid | ranged test | 6.3 | 88 | 73.2 | 2267.2 | 0.57 | 0.00 | 19.69 |
| enemy|Robot | ranged test | 4.5 | 110 | 42.3 | 249.8 | 0.48 | 0.00 | 16.68 |
| enemy|Humadroid | melee test | 4.2 | 82 | 48.9 | 98.3 | 0.74 | 0.00 | 18.44 |
| enemy|SwordMaster | ranged test | 3.9 | 93 | 43.0 | 239.6 | 0.49 | 0.00 | 16.89 |
| enemy|Robot | notice/chase/attack | 3.3 | 81 | 40.3 | 78.8 | 0.49 | 0.00 | 17.95 |
| enemy|SwordMaster | notice/chase/attack | 3.2 | 49 | 64.4 | 1206.1 | 0.51 | 0.00 | 17.15 |
| enemy|Robot | melee test | 2.6 | 51 | 47.2 | 97.7 | 0.55 | 0.00 | 18.39 |
| enemy|Humadroid | player->enemy | 2.5 | 50 | 52.0 | 127.3 | 0.67 | 0.00 | 18.77 |
| enemy|SwordMaster | player->enemy | 2.5 | 59 | 43.0 | 95.0 | 0.49 | 0.00 | 16.65 |
| enemy|Robot | player->enemy | 2.4 | 54 | 44.7 | 99.3 | 0.58 | 0.00 | 16.84 |
| enemy|Humadroid | notice/chase/attack | 2.2 | 55 | 40.4 | 76.7 | 0.85 | 0.00 | 17.03 |
| enemy|SwordMaster | melee test | 2.0 | 40 | 44.1 | 80.2 | 0.54 | 0.00 | 17.13 |
| enemy|Humadroid | spawn | 1.1 | 16 | 77.0 | 201.1 | 1.00 | 0.00 | 19.71 |
| enemy|SwordMaster | spawn | 1.0 | 21 | 49.9 | 88.7 | 0.67 | 0.00 | 16.95 |
| enemy|Robot | spawn | 1.0 | 19 | 55.2 | 106.6 | 0.69 | 0.00 | 18.81 |
| enemy|SwordMaster | cleanup | 0.2 | 6 | 39.1 | 71.8 | 0.36 | 0.00 | 13.14 |
| enemy|Robot | cleanup | 0.2 | 4 | 52.2 | 74.4 | 0.51 | 0.00 | 15.38 |
| enemy|Humadroid | setup | 0.2 | 1 | 2039.1 | 2039.1 | 0.83 | 0.00 | 27.79 |
| enemy|Humadroid | cleanup | 0.2 | 3 | 76.8 | 87.0 | 0.68 | 0.00 | 18.93 |
| enemy|Robot | setup | 0.1 | 1 | 236.8 | 236.8 | 0.76 | 0.00 | 18.19 |
| enemy|SwordMaster | setup | 0.1 | 1 | 290.0 | 290.0 | 0.68 | 0.00 | 30.06 |

Whole run (ran units only): 92s over 2142 frames, avg frame 43.9 ms, physics 0.51 ms, render 0.00 ms, scripts 16.72 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 25 | WARN | Hit marks fire on damage | Robot | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) | Add DMEnemyHitMarks + the layer-26 hitbox rig; verify the body-type hit-mark profile. |
| 2 | 20 | FAIL | Loot dropped and can be looted (real loot path), then cleaned up at once | SwordMaster | bag spawned but nothing could be looted (empty entries / TryLootAll refused) | Inspect the measured value and cause on the detail row. |
| 3 | -5 | WARN | Player melee swing via injected Mouse input (best effort) | Humadroid, Robot, SwordMaster | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes | Inspect the measured value and cause on the detail row. |
| 4 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 5 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Humadroid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Robot | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | warn | PASS | PASS |
| SwordMaster | Blade Of Chaos | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |

## Weapons that deal NO damage (headline)

- none detected in this run.

## Scenarios (block/parry, movement, multi-enemy, player weapons, performance)

| Scenario | PASS | FAIL | warn | time |
|---|---|---|---|---|
| Regression of earlier fixes and tweaks | 4 | 0 | 4 | 0s |

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | WARN | 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s); weapon reverts recorded this run: 0 | no Hot Cross row ran (cached or scenario skipped) |
| Fix check | vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons) | PASS | code present: True; console errors across 0 weapon-matrix case(s): 0 |  |
| Fix check | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | WARN | code present: True; dual-slot override case: not run |  |
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 18 value(s) compared on 2 enemies |  |
| Fix check | No circling while unaware (patrol-anchor fix) on every enemy | PASS | 3 PASS / 0 FAIL of 3 enemies |  |
| Fix check | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | PASS | 3 PASS / 0 FAIL of 3 enemies |  |
| Fix check | Profile adjustments made by earlier audit passes | INFO | none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes |  |

## Per-enemy detail

### Humadroid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 32s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 35ms, 761 components, frame avg 36.7ms max 391.6ms (254 frames), GC collections 0, managed delta 13360KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 223deg, path 12.3m, net 5.8m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.7s (at 1.7m, weapon weap2_sword), first player damage 2.2s, attacks 1, states Patrol/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.2s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 47.0 (item dmg 18), attacks 1, hitbox-active frames 5 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 2.1s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 47.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 15.2 (item dmg 14), shots/projectiles 1, attack-state frames 40, attacks 1, first hit 2.6s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 1.9m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 176.0 (-24.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 176.0 -> 176.0; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.30s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 29 distinct clips seen in total |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

Animation clip timings (controller clips; * = seen playing during the run):

| Category | Clip | Length (s) |
|---|---|---|
| attack | Attack2Hand_A | 0.60 |
| attack | Attack2Hand_B | 0.90 |
| attack | Attack2Hand_C * | 1.83 |
| attack | StrongAttack_PunchA | 0.88 |
| attack | StrongAttack_PunchB | 0.46 |
| attack | StrongAttack_PunchC | 0.75 |
| attack | WeakAttack_SwordA * | 1.00 |
| attack | WeakAttack_SwordB * | 0.79 |
| attack | WeakAttack_SwordC * | 0.63 |
| attack | WeakAttack_UnarmedA | 0.54 |
| attack | WeakAttack_UnarmedB | 0.54 |
| attack | WeakAttack_UnarmedC | 1.21 |
| death | Death | 3.13 |
| draw/equip | Draw Weapon HighLeft | 0.71 |
| draw/equip | Draw Weapon LowLeft | 0.71 |
| draw/equip | Draw Weapon LowRight | 0.71 |
| stagger/hit | Hit Big Back | 1.13 |
| stagger/hit | Hit Big Front | 1.17 |
| stagger/hit | Hit Big Left | 1.38 |
| stagger/hit | Hit Big Right | 1.42 |
| stagger/hit | Hit Small Back | 0.97 |
| stagger/hit | Hit Small Front | 0.93 |
| stagger/hit | Hit Small Left | 0.93 |
| stagger/hit | Hit Small Right | 0.93 |

### Robot

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `Robot_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 30s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Robot_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=15, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 34ms, 698 components, frame avg 44.2ms max 948.1ms (225 frames), GC collections 0, managed delta 120624KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 116deg, path 8.8m, net 6.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 2.7s (at 1.7m, weapon weap2_sword), first player damage 3.3s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 3.3s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.3 (item dmg 18), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 1.0, first hit 0.5s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.3; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.2m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.1 (item dmg 14), shots/projectiles 1, attack-state frames 46, attacks 1, first hit 3.2s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 489.2 (-10.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 489.2 -> 489.2; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | WARN | marks stamped 0 -> 0, body type Robot | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.31s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 17 distinct clips seen in total |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

Animation clip timings (controller clips; * = seen playing during the run):

| Category | Clip | Length (s) |
|---|---|---|
| attack | Attack2Hand_A | 0.60 |
| attack | Attack2Hand_B | 0.90 |
| attack | Attack2Hand_C | 1.83 |
| attack | StrongAttack_PunchA | 0.88 |
| attack | StrongAttack_PunchB | 0.46 |
| attack | StrongAttack_PunchC | 0.75 |
| attack | WeakAttack_SwordA * | 1.00 |
| attack | WeakAttack_SwordB * | 0.79 |
| attack | WeakAttack_SwordC * | 0.63 |
| attack | WeakAttack_UnarmedA | 0.54 |
| attack | WeakAttack_UnarmedB | 0.54 |
| attack | WeakAttack_UnarmedC | 1.21 |
| death | Death * | 3.13 |
| draw/equip | Draw Weapon HighLeft | 0.71 |
| draw/equip | Draw Weapon LowLeft | 0.71 |
| draw/equip | Draw Weapon LowRight | 0.71 |
| stagger/hit | Hit Big Back | 1.13 |
| stagger/hit | Hit Big Front | 1.17 |
| stagger/hit | Hit Big Left | 1.38 |
| stagger/hit | Hit Big Right | 1.42 |
| stagger/hit | Hit Small Back | 0.97 |
| stagger/hit | Hit Small Front | 0.93 |
| stagger/hit | Hit Small Left | 0.93 |
| stagger/hit | Hit Small Right | 0.93 |

### SwordMaster

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `Humanoid_Enemy` - melee `Blade Of Chaos` / ranged `sci_fi_pistol` - audited in 28s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=Blade Of Chaos (dmg 21), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 32ms, 693 components, frame avg 35.5ms max 91.2ms (254 frames), GC collections 0, managed delta 20128KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 179deg, path 11.1m, net 0.3m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.0s, chase 0.0s, first attack 1.5s (at 1.7m, weapon Blade Of Chaos), first player damage 2.0s, attacks 1, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 30.0 hp in 2.0s using Blade Of Chaos |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 30.0 (item dmg 21), attacks 1, hitbox-active frames 9 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.8s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Blade Of Chaos': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 30.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.2m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 13.9 (item dmg 14), shots/projectiles 1, attack-state frames 45, attacks 1, first hit 2.5s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.0m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Blade_Of_Chaos] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 274.6 (-25.4); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 274.6 -> 274.6; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | FAIL | loot bag held 0 entries, TryLootAll -> None, left 0; definition random loot count 0-1 | bag spawned but nothing could be looted (empty entries / TryLootAll refused) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.31s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 20 distinct clips seen in total |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

Animation clip timings (controller clips; * = seen playing during the run):

| Category | Clip | Length (s) |
|---|---|---|
| attack | Attack2Hand_A | 0.60 |
| attack | Attack2Hand_B | 0.90 |
| attack | Attack2Hand_C | 1.83 |
| attack | StrongAttack_PunchA | 0.88 |
| attack | StrongAttack_PunchB | 0.46 |
| attack | StrongAttack_PunchC | 0.75 |
| attack | WeakAttack_SwordA * | 1.00 |
| attack | WeakAttack_SwordB | 0.79 |
| attack | WeakAttack_SwordC | 0.63 |
| attack | WeakAttack_UnarmedA | 0.54 |
| attack | WeakAttack_UnarmedB | 0.54 |
| attack | WeakAttack_UnarmedC | 1.21 |
| death | Death * | 3.13 |
| draw/equip | Draw Weapon HighLeft | 0.71 |
| draw/equip | Draw Weapon LowLeft | 0.71 |
| draw/equip | Draw Weapon LowRight | 0.71 |
| stagger/hit | Hit Big Back | 1.13 |
| stagger/hit | Hit Big Front | 1.17 |
| stagger/hit | Hit Big Left | 1.38 |
| stagger/hit | Hit Big Right | 1.42 |
| stagger/hit | Hit Small Back | 0.97 |
| stagger/hit | Hit Small Front | 0.93 |
| stagger/hit | Hit Small Left | 0.93 |
| stagger/hit | Hit Small Right | 0.93 |

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
