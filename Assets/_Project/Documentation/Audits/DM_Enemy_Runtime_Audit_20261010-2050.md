# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-10 20:44:19 -> 2026-10-10 20:50:54 (aborted: play mode exited before the run completed)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-780.0, 21.2, -122.1), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 4; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 9
- Unit store: 0 stored unit(s); stamps audit 84313C game 0CC347 gear D50B5A; skip-unchanged is OFF (Full mode)
- Mode full: 7 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Mode: **full**, wall time 7m35s

**8 enemies audited - 3 FAIL, 7 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 1 scenarios): 105 PASS, 3 FAIL, 11 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 44 change(s).

- NEW enemy|Axe_Droid|perf = PASS
- CHANGED enemy|Axe_Droid|anim: INFO -> WARN
- NEW enemy|corrupt_patrol_android|perf = PASS
- NEW enemy|GreySpire Soldier|perf = PASS
- CHANGED enemy|GreySpire Soldier|notice: PASS -> FAIL
- CHANGED enemy|GreySpire Soldier|melee.damage: PASS -> FAIL
- CHANGED enemy|GreySpire Soldier|player.swing: PASS -> WARN
- CHANGED enemy|GreySpire Soldier|hitmarks: PASS -> WARN
- NEW enemy|Humadroid|perf = PASS
- CHANGED enemy|Humadroid|hitmarks: WARN -> PASS
- NEW enemy|Humanoid|perf = WARN
- CHANGED enemy|Humanoid|melee.damage: PASS -> FAIL
- NEW enemy|HumanoidEnemy_Invector|perf = WARN
- CHANGED enemy|HumanoidEnemy_Invector|player.swing: PASS -> WARN
- NEW enemy|Hybrid Droid|perf = PASS
- CHANGED enemy|Hybrid Droid|player.swing: PASS -> WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.swish = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hitsound = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hotcross = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hiteffects = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.loadout = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.tuning = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.patrol = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.onedrawn = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.profiles = INFO
- MISSING enemy|Robot|cfg (was PASS)
- MISSING enemy|Robot|silent.missingscripts (was PASS)
- MISSING enemy|Robot|silent.materials (was PASS)
- MISSING enemy|Robot|silent.animator (was PASS)
- MISSING enemy|Robot|silent.hitboxes (was PASS)
- MISSING enemy|Robot|unaware (was PASS)
- MISSING enemy|Robot|notice (was PASS)
- MISSING enemy|Robot|natural.damage (was INFO)
- MISSING enemy|Robot|melee.damage (was PASS)
- MISSING enemy|Robot|melee.vis (was INFO)
- MISSING enemy|Robot|ranged.damage (was PASS)
- MISSING enemy|Robot|ranged.vis (was INFO)
- MISSING enemy|Robot|vis (was PASS)
- MISSING enemy|Robot|player.dmg (was PASS)
- MISSING enemy|Robot|player.swing (was PASS)
- MISSING enemy|Robot|hitmarks (was PASS)
- MISSING enemy|Robot|death (was PASS)
- MISSING enemy|Robot|anim (was INFO)
- MISSING enemy|Robot|console (was PASS)

## Timing

Wall time 395s (6.6 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Axe_Droid | enemy | 44.3 | ran |
| corrupt_patrol_android | enemy | 45.8 | ran |
| GreySpire Soldier | enemy | 54.7 | ran |
| Humadroid | enemy | 43.8 | ran |
| Humanoid | enemy | 75.4 | ran |
| HumanoidEnemy_Invector | enemy | 41.9 | ran |
| Hybrid Droid | enemy | 39.6 | ran |
| Robot | enemy | 0.0 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|Humanoid | unaware 9s (+perf) | 24.9 | 141 | 176.9 | 4403.1 | 1.50 | 0.00 | 79.59 |
| enemy|Humanoid | death/loot | 24.0 | 320 | 78.0 | 1097.9 | 0.81 | 0.00 | 42.32 |
| enemy|GreySpire Soldier | notice/chase/attack | 23.0 | 545 | 42.4 | 887.8 | 0.46 | 0.00 | 19.12 |
| enemy|Axe_Droid | death/loot | 21.6 | 511 | 42.1 | 1769.8 | 0.35 | 0.00 | 17.41 |
| enemy|Humadroid | death/loot | 18.5 | 449 | 41.3 | 295.1 | 0.55 | 0.00 | 20.76 |
| enemy|Hybrid Droid | death/loot | 17.8 | 477 | 37.4 | 255.2 | 0.44 | 0.00 | 18.59 |
| enemy|corrupt_patrol_android | death/loot | 17.6 | 507 | 34.7 | 121.4 | 0.48 | 0.00 | 16.10 |
| enemy|HumanoidEnemy_Invector | death/loot | 17.4 | 514 | 33.8 | 81.8 | 0.44 | 0.00 | 15.86 |
| enemy|GreySpire Soldier | melee test | 15.7 | 488 | 32.1 | 876.4 | 0.37 | 0.00 | 13.97 |
| enemy|HumanoidEnemy_Invector | unaware 9s (+perf) | 12.2 | 203 | 60.1 | 1653.9 | 0.68 | 0.00 | 28.52 |
| enemy|Humanoid | melee test | 11.6 | 124 | 93.3 | 2207.9 | 0.84 | 0.00 | 43.71 |
| enemy|GreySpire Soldier | unaware 9s (+perf) | 11.0 | 260 | 42.1 | 1997.3 | 0.47 | 0.00 | 14.85 |
| enemy|Humadroid | unaware 9s (+perf) | 10.0 | 256 | 39.2 | 1111.2 | 0.47 | 0.00 | 16.39 |
| enemy|corrupt_patrol_android | unaware 9s (+perf) | 9.2 | 218 | 42.3 | 183.3 | 0.54 | 0.00 | 21.84 |
| enemy|Axe_Droid | unaware 9s (+perf) | 9.2 | 209 | 43.8 | 161.0 | 0.61 | 0.00 | 22.00 |
| enemy|Hybrid Droid | unaware 9s (+perf) | 9.0 | 279 | 32.4 | 55.1 | 0.41 | 0.00 | 14.95 |
| enemy|Humanoid | player->enemy | 8.0 | 76 | 91.9 | 457.1 | 0.97 | 0.00 | 40.40 |
| enemy|corrupt_patrol_android | melee test | 6.2 | 58 | 106.6 | 1997.9 | 0.61 | 0.00 | 21.01 |
| enemy|corrupt_patrol_android | ranged test | 5.3 | 77 | 68.1 | 1007.9 | 0.68 | 0.00 | 27.99 |
| enemy|Humadroid | ranged test | 5.0 | 118 | 42.2 | 905.5 | 0.44 | 0.00 | 15.51 |
| enemy|Axe_Droid | melee test | 4.8 | 65 | 73.5 | 258.9 | 1.63 | 0.00 | 34.15 |
| enemy|HumanoidEnemy_Invector | ranged test | 4.2 | 119 | 35.4 | 83.2 | 0.44 | 0.00 | 15.78 |
| enemy|corrupt_patrol_android | player->enemy | 4.1 | 70 | 58.9 | 136.0 | 0.86 | 0.00 | 24.72 |
| enemy|Axe_Droid | player->enemy | 4.0 | 82 | 48.7 | 108.0 | 0.59 | 0.00 | 20.30 |
| enemy|Humadroid | player->enemy | 4.0 | 92 | 42.9 | 66.6 | 0.58 | 0.00 | 17.48 |
| enemy|Hybrid Droid | ranged test | 3.9 | 104 | 37.7 | 62.4 | 0.49 | 0.00 | 17.10 |
| enemy|Humanoid | notice/chase/attack | 3.8 | 69 | 54.9 | 332.3 | 0.94 | 0.00 | 29.11 |
| enemy|Axe_Droid | notice/chase/attack | 3.8 | 62 | 60.6 | 1238.3 | 1.13 | 0.00 | 20.87 |
| enemy|GreySpire Soldier | player->enemy | 3.6 | 112 | 32.3 | 52.3 | 0.47 | 0.00 | 13.82 |
| enemy|Hybrid Droid | melee test | 3.1 | 88 | 35.4 | 53.3 | 0.57 | 0.00 | 15.17 |

Whole run (ran units only): 348s over 7439 frames, avg frame 49.2 ms, physics 0.55 ms, render 0.00 ms, scripts 21.03 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 100 | FAIL | Melee 'weap_two_handed' damages the player | GreySpire Soldier | enemy never entered an attack in 14s with the player 1.5m away (AI states Wander, target ''; min distance 1.5m) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 2 | 100 | FAIL | Melee 'weap2_sword' damages the player | Humanoid | only 5.0 hp lost with 0 weapon hit events (passive chip, not the weapon) / damage window opened but no hit registered: hitbox collider missing/too small/not overlapping the player, or the player's receiver filters it | Check the weapon's hit box: collider layer/tag against the player's hit tags, animation events that open the hitbox (ActiveDamage/OnEnableAttack), and that the weapon's vMeleeWeapon damage and the item damage are > 0. |
| 3 | 85 | FAIL | Notice / chase / attack timing (player 8m in front) | GreySpire Soldier | never left calm states with the player in front, inside vision range (EnemySenses LOS/FOV, vision range or aggro rules) | Review senses (vision range/FOV/LOS) and aggro rules; check the EnemySenses reference on the prefab. |
| 4 | 25 | WARN | Hit marks fire on damage | GreySpire Soldier | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) | Add DMEnemyHitMarks + the layer-26 hitbox rig; verify the body-type hit-mark profile. |
| 5 | -5 | WARN | Animation clip timings | Axe_Droid |  | Inspect the measured value and cause on the detail row. |
| 6 | -5 | WARN | Player melee swing via injected Mouse input (best effort) | GreySpire Soldier, HumanoidEnemy_Invector, Hybrid Droid | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes | Inspect the measured value and cause on the detail row. |
| 7 | -5 | WARN | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | Humanoid | average editor frame 176.9ms with one enemy active | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | HumanoidEnemy_Invector | average editor frame 60.1ms with one enemy active | Inspect the measured value and cause on the detail row. |
| 9 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 10 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 11 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |
| 12 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Axe_Droid | Spear of Fate | - | PASS | PASS | PASS | PASS | PASS | n/a | PASS | PASS | PASS | PASS |
| corrupt_patrol_android | Death Axe | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| GreySpire Soldier | weap_two_handed | - | PASS | PASS | PASS | **FAIL** | **FAIL** | n/a | PASS | warn | PASS | PASS |
| Humadroid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Humanoid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | **FAIL** | PASS | PASS | PASS | PASS | PASS |
| HumanoidEnemy_Invector | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Hybrid Droid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Robot |  |  | - | - | - | - | - | - | - | - | - | - |

## Weapons that deal NO damage (headline)

- **GreySpire Soldier** - melee `weap_two_handed`: player hp lost 0.0 (item dmg 28), attacks 0, hitbox-active frames 0 (unarmed 0), hit events 0, sub-1hp chip 0.0, first hit never, min distance 1.50m, target '', states Wander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** enemy never entered an attack in 14s with the player 1.5m away (AI states Wander, target ''; min distance 1.5m)
- **Humanoid** - melee `weap2_sword`: player hp lost 5.0 (item dmg 18), attacks 2, hitbox-active frames 17 (unarmed 0), hit events 0, sub-1hp chip 0.0, first hit 5.5s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** only 5.0 hp lost with 0 weapon hit events (passive chip, not the weapon) | damage window opened but no hit registered: hitbox collider missing/too small/not overlapping the player, or the player's receiver filters it

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
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 48 value(s) compared on 8 enemies |  |
| Fix check | No circling while unaware (patrol-anchor fix) on every enemy | PASS | 7 PASS / 0 FAIL of 7 enemies |  |
| Fix check | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | PASS | 7 PASS / 0 FAIL of 7 enemies |  |
| Fix check | Profile adjustments made by earlier audit passes | INFO | none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes |  |

## Per-enemy detail

### Axe_Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Axe_Droid.prefab` - definition `Axe_Droid` - melee `Spear of Fate` / ranged `-` - audited in 44s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Axe_Droid, melee=Spear of Fate (dmg 16), ranged=- (dmg -), preferRanged=False, def.attackDamage=12, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 35ms, 794 components, frame avg 43.8ms max 161.0ms (209 frames), GC collections 0, managed delta 11116KB |  |
| AI | No circling while unaware (player 50m away, 9s) | PASS | yaw turned 129deg, path 8.8m, net 4.3m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.3s, chase 0.3s, first attack 2.1s (at 2.1m, weapon Spear of Fate), first player damage 2.6s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 20.0 hp in 2.6s using Spear of Fate |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 36.0 (item dmg 16), attacks 1, hitbox-active frames 2 (unarmed 0), hit events 1, sub-1hp chip 3.8, first hit 2.0s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 221.3 (-28.7); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 221.3 -> 142.4; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.5s, corpse gone 13.8s, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | WARN | no Animator/controller |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### corrupt_patrol_android

Prefab `Assets/_Project/Prefabs/Combat/Enemies/corrupt_patrol_android.prefab` - definition `corrupt_patrol_android` - melee `Death Axe` / ranged `sci_fi_pistol` - audited in 46s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=corrupt_patrol_android, melee=Death Axe (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=350 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 31ms, 736 components, frame avg 42.3ms max 183.3ms (218 frames), GC collections 0, managed delta 9156KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 162deg, path 14.4m, net 3.9m, states Wander |  |
| AI | Notice / chase / attack timing (player 5m in front) | PASS | notice 0.0s, chase 0.0s, first attack 0.6s (at 2.1m, weapon Death Axe), first player damage 1.2s, attacks 1, states Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 24.0 hp in 1.2s using Death Axe |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 24.0 (item dmg 18), attacks 1, hitbox-active frames 7 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.7s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 13.8 (item dmg 14), shots/projectiles 1, attack-state frames 27, attacks 1, first hit 2.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Death_Axe] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 350.0 -> 328.3 (-21.7); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 328.3 -> 258.6; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.29s, loot bag 6.6s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 1 clip(s) observed playing; 16 distinct clips seen in total |  |
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

### GreySpire Soldier

Prefab `Assets/_Project/Prefabs/Combat/Enemies/GreySpire Soldier.prefab` - definition `GreySpire Soldier` - melee `weap_two_handed` / ranged `-` - audited in 55s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=GreySpire Soldier, melee=weap_two_handed (dmg 28), ranged=- (dmg -), preferRanged=False, def.attackDamage=8, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 28ms, 797 components, frame avg 42.1ms max 1997.3ms (260 frames), GC collections 1, managed delta -134568KB |  |
| AI | No circling while unaware (player 55m away, 9s) | PASS | yaw turned 75deg, path 4.1m, net 3.3m, states Wander |  |
| AI | Notice / chase / attack timing (player 8m in front) | FAIL | notice never, chase never, first attack never (at -1.0m, weapon ), first player damage 0.1s, attacks 0, states Wander | never left calm states with the player in front, inside vision range (EnemySenses LOS/FOV, vision range or aggro rules) |
| Weapons | Damage to player during natural AI behaviour | INFO | 288.4 hp in 0.1s using  |  |
| Weapons | Melee 'weap_two_handed' damages the player | FAIL | player hp lost 0.0 (item dmg 28), attacks 0, hitbox-active frames 0 (unarmed 0), hit events 0, sub-1hp chip 0.0, first hit never, min distance 1.50m, target '', states Wander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 | enemy never entered an attack in 14s with the player 1.5m away (AI states Wander, target ''; min distance 1.5m) |
| Weapons | Melee drawn state | INFO | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 226.2 (-23.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 226.2 -> 226.2; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | WARN | active marks 0 -> 0, body type Humanoid | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag none, corpse gone 0.3s, dropped weapon none;  |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 0 clip(s) observed playing; 6 distinct clips seen in total |  |
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
| attack | WeakAttack_SwordA | 1.00 |
| attack | WeakAttack_SwordB | 0.79 |
| attack | WeakAttack_SwordC | 0.63 |
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

### Humadroid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 44s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 27ms, 787 components, frame avg 39.2ms max 1111.2ms (256 frames), GC collections 1, managed delta -190892KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 165deg, path 11.6m, net 5.9m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.0s, chase 0.0s, first attack 1.8s (at 2.3m, weapon weap2_sword), first player damage 2.4s, attacks 1, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.4s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 9 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.4 (item dmg 14), shots/projectiles 1, attack-state frames 47, attacks 1, first hit 3.0s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 179.2 (-20.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 179.2 -> 11.2; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 1 clip(s) observed playing; 19 distinct clips seen in total |  |
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

### Humanoid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humanoid.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 75s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | WARN | spawn 36ms, 696 components, frame avg 176.9ms max 4403.1ms (141 frames), GC collections 1, managed delta -155780KB | average editor frame 176.9ms with one enemy active |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 170deg, path 10.7m, net 4.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.8s (at 2.2m, weapon weap2_sword), first player damage 3.2s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 47.0 hp in 3.2s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | FAIL | player hp lost 5.0 (item dmg 18), attacks 2, hitbox-active frames 17 (unarmed 0), hit events 0, sub-1hp chip 0.0, first hit 5.5s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 | only 5.0 hp lost with 0 weapon hit events (passive chip, not the weapon) / damage window opened but no hit registered: hitbox collider missing/too small/not overlapping the player, or the player's receiver filters it |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 5.2 (item dmg 14), shots/projectiles 1, attack-state frames 0, attacks 0, first hit 0.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 276.6 (-23.4); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 276.6 -> 142.6; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 3, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.4s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 5 clip(s) observed playing; 10 distinct clips seen in total |  |
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

### HumanoidEnemy_Invector

Prefab `Assets/_Project/Prefabs/Combat/Enemies/HumanoidEnemy_Invector.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 42s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | WARN | spawn 36ms, 722 components, frame avg 60.1ms max 1653.9ms (203 frames), GC collections 1, managed delta -206920KB | average editor frame 60.1ms with one enemy active |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 151deg, path 9.6m, net 6.2m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 2.1s (at 2.2m, weapon weap2_sword), first player damage 2.7s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.7s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.8 (item dmg 14), shots/projectiles 1, attack-state frames 45, attacks 1, first hit 3.0s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 270.3 (-29.7); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 270.3 -> 270.3; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 1, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 17 distinct clips seen in total |  |
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

### Hybrid Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Hybrid Droid.prefab` - definition `FRED_Android` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 40s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=FRED_Android, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 32ms, 696 components, frame avg 32.4ms max 55.1ms (279 frames), GC collections 0, managed delta 10748KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 156deg, path 9.9m, net 3.2m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.0s, chase 0.0s, first attack 1.7s (at 2.2m, weapon weap2_sword), first player damage 2.2s, attacks 1, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.2s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.6 (item dmg 18), attacks 1, hitbox-active frames 10 (unarmed 0), hit events 1, sub-1hp chip 2.4, first hit 1.2s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 15.9 (item dmg 14), shots/projectiles 1, attack-state frames 46, attacks 1, first hit 2.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 274.8 (-25.2); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 274.8 -> 274.8; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 1, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 22 distinct clips seen in total |  |
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

### Robot

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `` - melee `` / ranged `` - audited in 0s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|

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
