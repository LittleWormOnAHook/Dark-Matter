# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-14 12:39:44 -> 2026-10-14 13:17:53 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: already clean; attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 4; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 10
- Unit store: 37 stored unit(s); stamps audit 65F183 game 9978E0 gear 37F290; skip-unchanged is ON (Quick mode)
- Weapon matrix: 10 weapon items equipped one at a time (spawn override) on HumanoidEnemy_Invector; plus one dual-slot override case
- Player level raised in memory from 5 for equip gates (restored at the end; nothing saved)
- Mode quick: 36 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **quick**, wall time 38m09s

**10 enemies audited - 61 FAIL, 2 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 11 weapon cases + 16 scenarios): 298 PASS, 93 FAIL, 23 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 303 change(s).

- NEW enemy|Axe_Droid|perf = PASS
- CHANGED enemy|Axe_Droid|melee.damage: PASS -> FAIL
- NEW enemy|Axe_Droid|melee.miss = INFO
- CHANGED enemy|Axe_Droid|death: PASS -> FAIL
- NEW enemy|corrupt_patrol_android|perf = PASS
- CHANGED enemy|corrupt_patrol_android|notice: PASS -> WARN
- CHANGED enemy|corrupt_patrol_android|melee.damage: PASS -> FAIL
- NEW enemy|corrupt_patrol_android|melee.miss = INFO
- CHANGED enemy|corrupt_patrol_android|ranged.damage: PASS -> FAIL
- NEW enemy|corrupt_patrol_android|ranged.miss = FAIL
- NEW enemy|corrupt_patrol_android|ranged.range.7 = FAIL
- NEW enemy|corrupt_patrol_android|ranged.range.12 = FAIL
- NEW enemy|corrupt_patrol_android|ranged.range.18 = INFO
- NEW enemy|corrupt_patrol_android|ranged.range.25 = INFO
- NEW enemy|corrupt_patrol_android|ranged.range.11 = FAIL
- CHANGED enemy|corrupt_patrol_android|death: PASS -> FAIL
- NEW enemy|GreySpire Soldier|perf = PASS
- CHANGED enemy|GreySpire Soldier|melee.damage: PASS -> SKIP
- CHANGED enemy|GreySpire Soldier|ranged.damage: SKIP -> FAIL
- NEW enemy|GreySpire Soldier|ranged.miss = FAIL
- NEW enemy|GreySpire Soldier|ranged.range.7 = FAIL
- NEW enemy|GreySpire Soldier|ranged.range.12 = FAIL
- NEW enemy|GreySpire Soldier|ranged.range.18 = FAIL
- NEW enemy|GreySpire Soldier|ranged.range.25 = FAIL
- NEW enemy|GreySpire Soldier|ranged.range.24 = FAIL
- NEW enemy|GreySpire Soldier|ranged.vis = INFO
- NEW enemy|GreySpire Soldier|loot = PASS
- NEW enemy|Humadroid|perf = PASS
- CHANGED enemy|Humadroid|melee.damage: PASS -> FAIL
- NEW enemy|Humadroid|melee.miss = INFO
- CHANGED enemy|Humadroid|ranged.damage: PASS -> FAIL
- NEW enemy|Humadroid|ranged.miss = FAIL
- NEW enemy|Humadroid|ranged.range.7 = FAIL
- NEW enemy|Humadroid|ranged.range.12 = FAIL
- NEW enemy|Humadroid|ranged.range.18 = INFO
- NEW enemy|Humadroid|ranged.range.25 = INFO
- NEW enemy|Humadroid|ranged.range.15 = FAIL
- CHANGED enemy|Humadroid|hitmarks: WARN -> PASS
- NEW enemy|Humadroid|loot = PASS
- NEW enemy|Humanoid|perf = PASS
- CHANGED enemy|Humanoid|melee.damage: PASS -> FAIL
- NEW enemy|Humanoid|melee.miss = INFO
- CHANGED enemy|Humanoid|ranged.damage: PASS -> FAIL
- NEW enemy|Humanoid|ranged.miss = FAIL
- NEW enemy|Humanoid|ranged.range.7 = FAIL
- NEW enemy|Humanoid|ranged.range.12 = FAIL
- NEW enemy|Humanoid|ranged.range.18 = INFO
- NEW enemy|Humanoid|ranged.range.25 = INFO
- NEW enemy|Humanoid|ranged.range.15 = FAIL
- NEW enemy|Humanoid|loot = PASS
- NEW enemy|HumanoidEnemy_Invector|perf = PASS
- CHANGED enemy|HumanoidEnemy_Invector|melee.damage: PASS -> FAIL
- NEW enemy|HumanoidEnemy_Invector|melee.miss = INFO
- CHANGED enemy|HumanoidEnemy_Invector|ranged.damage: PASS -> FAIL
- NEW enemy|HumanoidEnemy_Invector|ranged.miss = FAIL
- NEW enemy|HumanoidEnemy_Invector|ranged.range.7 = FAIL
- NEW enemy|HumanoidEnemy_Invector|ranged.range.12 = FAIL
- NEW enemy|HumanoidEnemy_Invector|ranged.range.18 = INFO
- NEW enemy|HumanoidEnemy_Invector|ranged.range.25 = INFO
- NEW enemy|HumanoidEnemy_Invector|ranged.range.15 = FAIL
- CHANGED enemy|HumanoidEnemy_Invector|player.dmg: PASS -> FAIL
- NEW enemy|HumanoidEnemy_Invector|loot = PASS
- NEW enemy|Hybrid Droid|perf = PASS
- CHANGED enemy|Hybrid Droid|melee.damage: PASS -> FAIL
- NEW enemy|Hybrid Droid|melee.miss = INFO
- CHANGED enemy|Hybrid Droid|ranged.damage: PASS -> FAIL
- NEW enemy|Hybrid Droid|ranged.miss = FAIL
- NEW enemy|Hybrid Droid|ranged.range.7 = FAIL
- NEW enemy|Hybrid Droid|ranged.range.12 = FAIL
- NEW enemy|Hybrid Droid|ranged.range.18 = INFO
- NEW enemy|Hybrid Droid|ranged.range.25 = INFO
- NEW enemy|Hybrid Droid|ranged.range.15 = FAIL
- NEW enemy|Hybrid Droid|loot = PASS
- NEW enemy|Robot|perf = PASS
- CHANGED enemy|Robot|melee.damage: PASS -> FAIL
- NEW enemy|Robot|melee.miss = INFO
- CHANGED enemy|Robot|ranged.damage: PASS -> FAIL
- NEW enemy|Robot|ranged.miss = FAIL
- NEW enemy|Robot|ranged.range.7 = FAIL
- NEW enemy|Robot|ranged.range.12 = FAIL
- ... 223 more

## Timing

Wall time 2289s (38.2 min), mode quick. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Axe_Droid | enemy | 73.8 | ran |
| corrupt_patrol_android | enemy | 135.7 | ran |
| GreySpire Soldier | enemy | 135.8 | ran |
| Humadroid | enemy | 142.4 | ran |
| Humanoid | enemy | 134.5 | ran |
| HumanoidEnemy_Invector | enemy | 143.7 | ran |
| Hybrid Droid | enemy | 140.0 | ran |
| Robot | enemy | 150.4 | ran |
| SwordMaster | enemy | 155.5 | ran |
| The_Evil_One | enemy | 141.4 | ran |
| 2 Hander | weapon | 16.8 | ran |
| Blade Of Chaos | weapon | 18.4 | ran |
| Death Axe | weapon | 18.7 | ran |
| Spear of Fate | weapon | 18.6 | ran |
| Sword of Fear | weapon | 18.9 | ran |
| weap2_sword | weapon | 19.1 | ran |
| weap_two_handed | weapon | 15.8 | ran |
| Wood Axe | weapon | 17.0 | ran |
| sci_fi_pistol | weapon | 17.1 | ran |
| survival_rifle | weapon | 17.2 | ran |
| OVERRIDE both slots | weapon | 4.2 | ran |
| Camera: faces forward after teleports, zoom in/out | scenario | 14.1 | ran |
| Equip: weapon survives a teleport | scenario | 6.1 | ran |
| Audio: swing / hit sound wiring (static) | scenario | 7.7 | ran |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | scenario | 62.2 | ran |
| Block + parry: player defends against enemy melee | scenario | 53.2 | ran |
| Block: enemy defends against player swings | scenario | 12.8 | ran |
| Movement: retreat, push, flank, backstab, lose target | scenario | 34.2 | ran |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | scenario | 100.3 | ran |
| Multi-enemy: FRED x3 and x5 | scenario | 36.0 | ran |
| Brain: orbit, toggles, archetypes | scenario | 129.0 | ran |
| Player weapons: each melee item vs enemy (swapped in place) | scenario | 52.2 | ran |
| Ammo: default ammo per ranged weapon + ammo-specific FX | scenario | 31.3 | ran |
| Player rig: enemy ranged shot hits a body bone + mark | scenario | 30.0 | ran |
| Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | scenario | 33.3 | ran |
| Behavior tweaks: over / under-exaggerated enemy values (runtime only) | scenario | 98.8 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| scenario|Brain: orbit, toggles, archetypes | setup | 129.2 | 3019 | 42.8 | 517.6 | 0.53 | 0.00 | 19.70 |
| scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | setup | 100.5 | 2735 | 36.7 | 149.3 | 0.50 | 0.00 | 16.02 |
| scenario|Behavior tweaks: over / under-exaggerated enemy values (runtime only) | setup | 99.0 | 2784 | 35.5 | 189.7 | 0.40 | 0.00 | 15.99 |
| enemy|GreySpire Soldier | ranged test | 93.3 | 2346 | 39.8 | 204.3 | 0.45 | 0.00 | 17.93 |
| enemy|HumanoidEnemy_Invector | ranged test | 83.0 | 1829 | 45.5 | 239.8 | 0.57 | 0.00 | 21.30 |
| enemy|SwordMaster | ranged test | 81.5 | 2422 | 33.7 | 213.1 | 0.40 | 0.00 | 14.55 |
| enemy|Humadroid | ranged test | 79.1 | 2043 | 38.8 | 216.3 | 0.47 | 0.00 | 17.75 |
| enemy|The_Evil_One | ranged test | 78.0 | 2011 | 38.9 | 214.3 | 0.47 | 0.00 | 17.68 |
| enemy|Robot | ranged test | 77.6 | 2223 | 35.0 | 215.3 | 0.39 | 0.00 | 15.42 |
| enemy|Hybrid Droid | ranged test | 76.6 | 1820 | 42.2 | 224.2 | 0.51 | 0.00 | 20.00 |
| enemy|Humanoid | ranged test | 73.6 | 1877 | 39.3 | 233.0 | 0.48 | 0.00 | 17.92 |
| enemy|corrupt_patrol_android | ranged test | 67.1 | 1809 | 37.2 | 235.2 | 0.45 | 0.00 | 16.80 |
| scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | setup | 62.4 | 1823 | 34.2 | 728.5 | 0.36 | 0.00 | 15.67 |
| scenario|Block + parry: player defends against enemy melee | setup | 54.4 | 1422 | 38.3 | 295.8 | 0.48 | 0.00 | 17.44 |
| scenario|Player weapons: each melee item vs enemy (swapped in place) | setup | 52.4 | 1106 | 47.4 | 622.5 | 0.64 | 0.00 | 21.26 |
| scenario|Multi-enemy: FRED x3 and x5 | setup | 37.5 | 860 | 43.6 | 423.8 | 0.57 | 0.00 | 21.00 |
| scenario|Movement: retreat, push, flank, backstab, lose target | setup | 34.4 | 940 | 36.6 | 80.5 | 0.45 | 0.00 | 15.90 |
| scenario|Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | setup | 33.5 | 825 | 40.6 | 117.9 | 0.58 | 0.00 | 18.54 |
| scenario|Ammo: default ammo per ranged weapon + ammo-specific FX | setup | 31.5 | 620 | 50.9 | 793.1 | 0.64 | 0.00 | 21.91 |
| scenario|Player rig: enemy ranged shot hits a body bone + mark | setup | 30.2 | 767 | 39.4 | 155.9 | 0.45 | 0.00 | 18.29 |
| enemy|Robot | notice/chase/attack | 22.5 | 524 | 42.9 | 329.4 | 0.51 | 0.00 | 20.77 |
| enemy|Humadroid | notice/chase/attack | 22.2 | 492 | 45.0 | 155.0 | 0.59 | 0.00 | 21.39 |
| enemy|Hybrid Droid | notice/chase/attack | 22.1 | 530 | 41.6 | 106.5 | 0.52 | 0.00 | 20.00 |
| enemy|Humanoid | notice/chase/attack | 22.0 | 603 | 36.5 | 72.4 | 0.46 | 0.00 | 16.77 |
| enemy|The_Evil_One | notice/chase/attack | 22.0 | 562 | 39.2 | 97.1 | 0.47 | 0.00 | 16.90 |
| enemy|GreySpire Soldier | notice/chase/attack | 22.0 | 578 | 38.1 | 90.8 | 0.44 | 0.00 | 17.39 |
| enemy|corrupt_patrol_android | notice/chase/attack | 22.0 | 645 | 34.2 | 94.8 | 0.39 | 0.00 | 16.92 |
| enemy|SwordMaster | notice/chase/attack | 22.0 | 615 | 35.8 | 81.6 | 0.47 | 0.00 | 16.14 |
| enemy|HumanoidEnemy_Invector | notice/chase/attack | 22.0 | 589 | 37.4 | 71.4 | 0.46 | 0.00 | 17.58 |
| enemy|Axe_Droid | notice/chase/attack | 22.0 | 529 | 41.6 | 105.1 | 0.64 | 0.00 | 19.02 |

Whole run (ran units only): 2245s over 57918 frames, avg frame 39.3 ms, physics 0.47 ms, render 0.00 ms, scripts 17.55 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 100 | FAIL | Melee 'Spear of Fate' damages the player | Axe_Droid, Robot, SwordMaster, The_Evil_One | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 2 | 100 | FAIL | Melee 'Death Axe' damages the player | corrupt_patrol_android | enemy never entered an attack in 14s with the player 1.5m away (AI states Chase/Wander, target 'Player_v7 Combat Variant'; min distance 1.4m) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 3 | 100 | FAIL | Ranged 'sci_fi_pistol' damages the player | corrupt_patrol_android, GreySpire Soldier, Humadroid, Humanoid +4 more | 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 4 | 100 | FAIL | Melee 'weap2_sword' damages the player | Humadroid | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=43 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 43 frames (definition missRate 0.25) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 5 | 100 | FAIL | Melee 'weap2_sword' damages the player | Humanoid | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=82 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 82 frames (definition missRate 0.25) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 6 | 100 | FAIL | Melee 'weap2_sword' damages the player | HumanoidEnemy_Invector | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=38 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 38 frames (definition missRate 0.25) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 7 | 100 | FAIL | Melee 'weap2_sword' damages the player | Hybrid Droid | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=52 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 52 frames (definition missRate 0.25) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 8 | 100 | FAIL | Ranged 'sci_fi_pistol' damages the player | The_Evil_One | 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.65, intentional-miss frames 0) | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 9 | 100 | FAIL | Melee '2 Hander' damages the player | 2 Hander, weap_two_handed | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=54 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 54 frames (definition missRate 0.25) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 10 | 100 | FAIL | Melee 'Blade Of Chaos' damages the player | Blade Of Chaos | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=55 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 55 frames (definition missRate 0.25) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 11 | 100 | FAIL | Melee 'Death Axe' damages the player | Death Axe | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=83 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 83 frames (definition missRate 0.25) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 12 | 100 | FAIL | Melee 'Spear of Fate' damages the player | Spear of Fate, Sword of Fear, weap2_sword, Wood Axe | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 13 | 100 | FAIL | Ranged 'sci_fi_pistol' damages the player | sci_fi_pistol, survival_rifle | 13 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 14 | 100 | FAIL | Player uses melee 'weap2_sword' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 4/4 clicks swung, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 15 | 100 | FAIL | Player uses melee 'Wood Axe' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 4/4 clicks swung, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 16 | 90 | FAIL | Death, loot bag, cleanup | Axe_Droid, corrupt_patrol_android, Robot, SwordMaster | no loot bag and corpse not destroyed/disabled after 17s (destroyDelay 3s) | Check EnemyLootable/EnemyLootBag wiring and destroyDelay in the definition. |
| 17 | 90 | FAIL | Player damage hurts the enemy (receiver path) | HumanoidEnemy_Invector | damage receiver chain returns 0 (PioneerInvectorDamageResolver resolves the sender's weapon damage to 0 - player has no damaging weapon equipped, or hit dedupe rejected) though EnemyHealth itself works | Inspect the measured value and cause on the detail row. |
| 18 | 70 | FAIL | Holding block negates enemy melee damage | Block + parry: player defends against enemy melee | player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 19 | 60 | WARN | Notice / chase / attack timing (player 5m in front) | corrupt_patrol_android | noticed but never attacked within 22s (chase blocked, standoff/hold token, attack range, or cooldown) | Review senses (vision range/FOV/LOS) and aggro rules; check the EnemySenses reference on the prefab. |
| 20 | 55 | WARN | Console errors/warnings | Player weapons: each melee item vs enemy (swapped in place) | [Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0. | Fix the exception at the listed stack frame (null-guard or initialise the field); it repeats on every spawn. |
| 21 | 45 | WARN | Enemy guard-break stagger after a blocked hit | Block + parry: player defends against enemy melee | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 22 | 45 | WARN | Enemy block reduces player melee damage | Block: enemy defends against player swings | player swings registered no hits on the open enemy, so the comparison is inconclusive (input focus, reach, or the player weapon) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 23 | 30 | WARN | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | Multi-enemy: FRED x3 and x5 | 144/392 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Add enemy separation and make the attack token rotate between enemies. |
| 24 | 25 | WARN | Hit marks fire on damage | SwordMaster | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) | Add DMEnemyHitMarks + the layer-26 hitbox rig; verify the body-type hit-mark profile. |
| 25 | 20 | FAIL | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | corrupt_patrol_android, GreySpire Soldier, Humadroid, Humanoid +5 more | zero hits across 12 shots | Inspect the measured value and cause on the detail row. |
| 26 | 20 | FAIL | Ranged 'sci_fi_pistol' at 7.0 m | corrupt_patrol_android, GreySpire Soldier, Humadroid, Humanoid +5 more | inside engage range but no damage (see [DMRangedShot] log) | Inspect the measured value and cause on the detail row. |
| 27 | 20 | FAIL | Ranged 'sci_fi_pistol' at 12.0 m | corrupt_patrol_android, GreySpire Soldier, Humadroid, Humanoid +5 more | inside engage range but no damage (see [DMRangedShot] log) | Inspect the measured value and cause on the detail row. |
| 28 | 20 | FAIL | Ranged 'sci_fi_pistol' at 11.4 m | corrupt_patrol_android | inside engage range but no damage (see [DMRangedShot] log) | Inspect the measured value and cause on the detail row. |
| 29 | 20 | FAIL | Ranged 'survival_rifle' at 18.0 m | GreySpire Soldier | inside engage range but no damage (see [DMRangedShot] log) | Inspect the measured value and cause on the detail row. |
| 30 | 20 | FAIL | Ranged 'survival_rifle' at 25.0 m | GreySpire Soldier | inside engage range but no damage (see [DMRangedShot] log) | Inspect the measured value and cause on the detail row. |
| 31 | 20 | FAIL | Ranged 'survival_rifle' at 23.8 m | GreySpire Soldier | inside engage range but no damage (see [DMRangedShot] log) | Inspect the measured value and cause on the detail row. |
| 32 | 20 | FAIL | Ranged 'sci_fi_pistol' at 15.2 m | Humadroid, Humanoid, HumanoidEnemy_Invector, Hybrid Droid +3 more | inside engage range but no damage (see [DMRangedShot] log) | Inspect the measured value and cause on the detail row. |
| 33 | 20 | FAIL | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | sci_fi_pistol, survival_rifle | zero hits across 13 shots | Inspect the measured value and cause on the detail row. |
| 34 | 20 | FAIL | W moves the player forward (camera direction) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move action not reaching the controller: PlayerInput disabled, move locked, or PioneerPlayerInputBinder.HandleAction('Move') not wired | Inspect the measured value and cause on the detail row. |
| 35 | 20 | FAIL | S moves the player back | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (S) produced no backward displacement | Inspect the measured value and cause on the detail row. |
| 36 | 20 | FAIL | A strafes left | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (A) produced no leftward displacement | Inspect the measured value and cause on the detail row. |
| 37 | 20 | FAIL | D strafes right | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (D) produced no rightward displacement | Inspect the measured value and cause on the detail row. |
| 38 | 20 | FAIL | Shift + W sprints faster than W | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | sprint no faster than run (1.6m): Sprint action not wired, stamina 0, or sprint speed equals run speed | Inspect the measured value and cause on the detail row. |
| 39 | 20 | FAIL | Space jumps | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked; state before the press: isGrounded True, isJumping False, customAction False, isCrouching False, stamina 1000 | Inspect the measured value and cause on the detail row. |
| 40 | 20 | FAIL | Q dodge / roll | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dodge action did not move the player: TryDodgeRollFromInput refused (stamina, cooldown, no roll clip) | Inspect the measured value and cause on the detail row. |
| 41 | 20 | FAIL | Left click swings the drawn melee weapon | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | melee Attack input not received: drawn melee tap/hold logic (UpdateDrawnMeleeCharge) did not start a swing | Inspect the measured value and cause on the detail row. |
| 42 | 20 | FAIL | Right click blocks (melee drawn) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Block input (Mouse rightButton / Block action) did not set isBlocking: stamina 0, customAction, not grounded, or still attacking | Inspect the measured value and cause on the detail row. |
| 43 | 20 | WARN | Player circles behind the enemy: enemy turns to face | Movement: retreat, push, flank, backstab, lose target | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 44 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Standard: fires (rounds consumed) | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 45 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Standard: fires (rounds consumed) | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 46 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 47 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 48 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Ion: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 49 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Laser: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 50 | 20 | FAIL | Enemy ranged shot ('survival_rifle') resolves to a player body hitbox | Player rig: enemy ranged shot hits a body bone + mark | enemy ranged shot did no damage to the player: the shot missed or the hitscan still passes through (the rig supersedes the capsule but nothing resolves on the bone hitboxes) | Inspect the measured value and cause on the detail row. |
| 51 | 20 | FAIL | Player hit marks are small and shallow (not head-sized, not projected through the body) | Player rig: enemy ranged shot hits a body bone + mark | player marks are larger or deeper than a small bullet decal: check DM_EnemyHitMarkProfile playerMarkSizeScale / playerMarkProjectionDepth | Inspect the measured value and cause on the detail row. |
| 52 | 15 | WARN | A tap inside the parry window parries (and an early/late tap does not) | Block + parry: player defends against enemy melee | no sampled tap produced a parry: enemy attack timing varies between swings (windup from the baseline swing may not match), or the parry window/stagger in the profile differs from what the rows show | Inspect the measured value and cause on the detail row. |
| 53 | -5 | WARN | Left Ctrl crouches | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) | Inspect the measured value and cause on the detail row. |
| 54 | -5 | WARN | Left Alt dash | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dash did not move the player even though the skill gate is unlocked for this run (see ctl.systems row): DMDashController disabled, on cooldown, stamina, or the Dash action not bound to Left Alt | Inspect the measured value and cause on the detail row. |
| 55 | -5 | WARN | Hold Space after jumping boosts (jetpack) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | no sustained lift although the jetpack skill gate is unlocked for this run (see ctl.systems row): fuel 0, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player | Inspect the measured value and cause on the detail row. |
| 56 | -5 | WARN | Tab (SwitchWeapon) cycles the weapon focus | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Tab tap did not change focus: only one weapon on the hotbar, Hot Cross not polling, or tap/hold threshold (hold arms) not met | Inspect the measured value and cause on the detail row. |
| 57 | -5 | WARN | Approach with W: enemy notices | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) | Inspect the measured value and cause on the detail row. |
| 58 | -5 | WARN | Run away with S: enemy pursues | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy did not chase a retreating player: chase blocked (standoff/hold token, ranged keep-distance) or target lost immediately | Inspect the measured value and cause on the detail row. |
| 59 | -5 | WARN | Strafe around the enemy: it turns to face the player | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy did not keep facing a strafing player: turn speed too slow, turn-in-place gated, or enemy in a state that ignores the target | Inspect the measured value and cause on the detail row. |
| 60 | -5 | WARN | Jetpack escape (hold Space + S) | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Axe_Droid | Spear of Fate | - | PASS | PASS | PASS | PASS | **FAIL** | n/a | PASS | PASS | **FAIL** | PASS |
| corrupt_patrol_android | Death Axe | sci_fi_pistol | PASS | PASS | PASS | warn | **FAIL** | **FAIL** | PASS | PASS | **FAIL** | PASS |
| GreySpire Soldier | - | survival_rifle | PASS | PASS | PASS | PASS | n/a | **FAIL** | PASS | PASS | PASS | PASS |
| Humadroid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | **FAIL** | **FAIL** | PASS | PASS | PASS | PASS |
| Humanoid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | **FAIL** | **FAIL** | PASS | PASS | PASS | PASS |
| HumanoidEnemy_Invector | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | **FAIL** | **FAIL** | **FAIL** | PASS | PASS | PASS |
| Hybrid Droid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | **FAIL** | **FAIL** | PASS | PASS | PASS | PASS |
| Robot | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | **FAIL** | **FAIL** | PASS | PASS | **FAIL** | PASS |
| SwordMaster | Blade Of Chaos | sci_fi_pistol | PASS | PASS | PASS | PASS | **FAIL** | **FAIL** | PASS | warn | **FAIL** | PASS |
| The_Evil_One | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | **FAIL** | **FAIL** | PASS | PASS | PASS | PASS |

## Weapons that deal NO damage (headline)

- **Axe_Droid** - melee `Spear of Fate`: player hp lost 0.0 (item dmg 16), attacks 8, hitbox-active frames 115 (unarmed 0), hit events 14, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it
- **corrupt_patrol_android** - melee `Death Axe`: player hp lost 0.0 (item dmg 18), attacks 0, hitbox-active frames 0 (unarmed 0), hit events 0, sub-1hp chip 0.0, first hit never, min distance 1.35m, target 'Player_v7 Combat Variant', states Chase/Wander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** enemy never entered an attack in 14s with the player 1.5m away (AI states Chase/Wander, target 'Player_v7 Combat Variant'; min distance 1.4m)
- **corrupt_patrol_android** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 391, attacks 18, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0)
- **GreySpire Soldier** - ranged `survival_rifle`: player hp lost 0.0 (item dmg 18), shots/projectiles 12, attack-state frames 477, attacks 12, first hit never, range 11m (engage 25), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=16, rangedCooldown=1.6, missRate=0.25. **Probable cause:** 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0)
- **Humadroid** - melee `weap2_sword`: player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 155 (unarmed 0), hit events 8, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=43 frames, ItemData roll) or the player blocked/ignored it | intentional-miss active for 43 frames (definition missRate 0.25)
- **Humadroid** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 484, attacks 19, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0)
- **Humanoid** - melee `weap2_sword`: player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 125 (unarmed 0), hit events 10, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=82 frames, ItemData roll) or the player blocked/ignored it | intentional-miss active for 82 frames (definition missRate 0.25)
- **Humanoid** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 391, attacks 15, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0)
- **HumanoidEnemy_Invector** - melee `weap2_sword`: player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 112 (unarmed 0), hit events 12, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=38 frames, ItemData roll) or the player blocked/ignored it | intentional-miss active for 38 frames (definition missRate 0.25)
- **HumanoidEnemy_Invector** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 448, attacks 21, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack/Defensive; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0)
- **Hybrid Droid** - melee `weap2_sword`: player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 125 (unarmed 0), hit events 12, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=52 frames, ItemData roll) or the player blocked/ignored it | intentional-miss active for 52 frames (definition missRate 0.25)
- **Hybrid Droid** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 416, attacks 17, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0)
- **Robot** - melee `weap2_sword`: player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 114 (unarmed 0), hit events 11, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it
- **Robot** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 465, attacks 14, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0)
- **SwordMaster** - melee `Blade Of Chaos`: player hp lost 0.0 (item dmg 21), attacks 8, hitbox-active frames 96 (unarmed 0), hit events 6, sub-1hp chip 0.0, first hit never, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it
- **SwordMaster** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 634, attacks 22, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0)
- **The_Evil_One** - melee `weap2_sword`: player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 149 (unarmed 0), hit events 9, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; no vMeleeWeapon on the drawn/bound weapon. **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it
- **The_Evil_One** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 436, attacks 17, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.65. **Probable cause:** 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.65, intentional-miss frames 0)
- **Weapon matrix: 2 Hander** (melee): player hp lost 0.0 (item dmg 28), attacks 9, hitbox-active frames 99, hit events 8, sub-1hp chip 0.0, first hit never, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=54 frames, ItemData roll) or the player blocked/ignored it | intentional-miss active for 54 frames (definition missRate 0.25)
- **Weapon matrix: Blade Of Chaos** (melee): player hp lost 0.0 (item dmg 21), attacks 8, hitbox-active frames 130, hit events 12, sub-1hp chip 0.0, first hit never, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=Tr.... **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=55 frames, ItemData roll) or the player blocked/ignored it | intentional-miss active for 55 frames (definition missRate 0.25)
- **Weapon matrix: Death Axe** (melee): player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 135, hit events 14, sub-1hp chip 0.0, first hit never, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-.... **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=83 frames, ItemData roll) or the player blocked/ignored it | intentional-miss active for 83 frames (definition missRate 0.25)
- **Weapon matrix: Spear of Fate** (melee): player hp lost 0.0 (item dmg 16), attacks 8, hitbox-active frames 115, hit events 12, sub-1hp chip 0.0, first hit never, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True.... **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it
- **Weapon matrix: Sword of Fear** (melee): player hp lost 0.0 (item dmg 15), attacks 8, hitbox-active frames 109, hit events 12, sub-1hp chip 0.0, first hit never, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True.... **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it
- **Weapon matrix: weap2_sword** (melee): player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 119, hit events 13, sub-1hp chip 0.0, first hit never, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, ty.... **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it
- **Weapon matrix: weap_two_handed** (melee): player hp lost 0.0 (item dmg 28), attacks 9, hitbox-active frames 99, hit events 8, sub-1hp chip 0.0, first hit never, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=T.... **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=54 frames, ItemData roll) or the player blocked/ignored it | intentional-miss active for 54 frames (definition missRate 0.25)
- **Weapon matrix: Wood Axe** (melee): player hp lost 0.0 (item dmg 26), attacks 8, hitbox-active frames 100, hit events 13, sub-1hp chip 0.0, first hit never, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it
- **Weapon matrix: sci_fi_pistol** (ranged): player hp lost 0.0 (item dmg 14), shots/projectiles 13, attack-state frames 387, attacks 15, first hit never, range 10m (engage 16), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.... **Probable cause:** 13 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0)
- **Weapon matrix: survival_rifle** (ranged): player hp lost 0.0 (item dmg 18), shots/projectiles 13, attack-state frames 348, attacks 16, first hit never, range 11m (engage 25), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate.... **Probable cause:** 13 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0)

## Weapon matrix (every weapon item on the template, spawn override)

11 cases, 12 FAIL. Each weapon is equipped on `HumanoidEnemy_Invector` through ConfigureSpawnLoadout (the spawner path) and made to attack the player.

| Weapon | Kind | Equips | Damages player | One drawn | Measured | Probable cause |
|---|---|---|---|---|---|---|
| 2 Hander | melee | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 28), attacks 9, hitbox-active frames 99, hit events 8, sub-1hp chip 0.0, first hit never, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer ... | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=54 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 54 frames (definition missRate 0.25) |
| Blade Of Chaos | melee | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 21), attacks 8, hitbox-active frames 130, hit events 12, sub-1hp chip 0.0, first hit never, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; ... | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=55 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 55 frames (definition missRate 0.25) |
| Death Axe | melee | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 135, hit events 14, sub-1hp chip 0.0, first hit never, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: la... | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=83 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 83 frames (definition missRate 0.25) |
| Spear of Fate | melee | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 16), attacks 8, hitbox-active frames 115, hit events 12, sub-1hp chip 0.0, first hit never, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hi... | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| Sword of Fear | melee | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 15), attacks 8, hitbox-active frames 109, hit events 12, sub-1hp chip 0.0, first hit never, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hi... | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| weap2_sword | melee | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 119, hit events 13, sub-1hp chip 0.0, first hit never, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox... | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| weap_two_handed | melee | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 28), attacks 9, hitbox-active frames 99, hit events 8, sub-1hp chip 0.0, first hit never, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38;... | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=54 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 54 frames (definition missRate 0.25) |
| Wood Axe | melee | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 26), attacks 8, hitbox-active frames 100, hit events 13, sub-1hp chip 0.0, first hit never, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: laye... | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| sci_fi_pistol | ranged | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 14), shots/projectiles 13, attack-state frames 387, attacks 15, first hit never, range 10m (engage 16), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngage... | 13 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) / zero hits across 13 shots |
| survival_rifle | ranged | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 18), shots/projectiles 13, attack-state frames 348, attacks 16, first hit never, range 11m (engage 25), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEnga... | 13 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) / zero hits across 13 shots |
| OVERRIDE both slots | dual-slot spawn override | - | - | PASS | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)): PASS ; Starts with the ranged override drawn (prefer-ranged on, start-melee off): PASS ; Switch to the melee override vi... |  |

### Weapon: 2 Hander

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item 2 Hander; drawn [Drawn_2_Hander]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | '2 Hander' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_0:Melee Weapon Attack 4, SfxPool_2:neverworld |  |
| Weapons | Melee '2 Hander' damages the player | FAIL | player hp lost 0.0 (item dmg 28), attacks 9, hitbox-active frames 99, hit events 8, sub-1hp chip 0.0, first hit never, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=54 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 54 frames (definition missRate 0.25) |
| Weapons | Melee '2 Hander': intentional miss rate vs configured chance | INFO | swings 9, intentional misses rolled 2 (22 %) vs configured 10 % (+-30 % at 3 sigma), weapon hit events 8, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.5m | only 9 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage never, attacks 9, attack-state frames 397, clips seen: WeakAttack_SwordA, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Blade Of Chaos

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 21 |  |
| Audio | 'Blade Of Chaos' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_0:Melee Weapon Attack 4, SfxPool_1:Melee Weapon Air Swing 1, SfxPool_2:Melee Weapon Attack 3 |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | FAIL | player hp lost 0.0 (item dmg 21), attacks 8, hitbox-active frames 130, hit events 12, sub-1hp chip 0.0, first hit never, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=55 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 55 frames (definition missRate 0.25) |
| Weapons | Melee 'Blade Of Chaos': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 1 (13 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 12, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.5m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage never, attacks 8, attack-state frames 479, clips seen: WeakAttack_SwordA, WeakAttack_SwordB, WeakAttack_SwordC, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Death Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'Death Axe' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_8:Melee Weapon Attack 1, SfxPool_9:neverworld, SfxPool_1:Melee Weapon Attack 4, SfxPool_0:Melee Weapon Air Swing 1 |  |
| Weapons | Melee 'Death Axe' damages the player | FAIL | player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 135, hit events 14, sub-1hp chip 0.0, first hit never, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=83 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 83 frames (definition missRate 0.25) |
| Weapons | Melee 'Death Axe': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 2 (25 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 14, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.5m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage never, attacks 8, attack-state frames 487, clips seen: WeakAttack_SwordA, WeakAttack_SwordB, WeakAttack_SwordC, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Spear of Fate

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 16 |  |
| Audio | 'Spear of Fate' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_1:Melee Weapon Attack 1 |  |
| Weapons | Melee 'Spear of Fate' damages the player | FAIL | player hp lost 0.0 (item dmg 16), attacks 8, hitbox-active frames 115, hit events 12, sub-1hp chip 0.0, first hit never, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| Weapons | Melee 'Spear of Fate': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 0 (0 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 12, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.5m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage never, attacks 8, attack-state frames 478, clips seen: WeakAttack_SwordA, WeakAttack_SwordB, Attack2Hand_C, WeakAttack_SwordC |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Sword of Fear

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Sword of Fear; drawn [Drawn_Sword_of_Fear]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 15 |  |
| Audio | 'Sword of Fear' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_4:Melee Weapon Air Swing 2, SfxPool_5:Melee Weapon Attack 3, SfxPool_6:Melee Weapon Air Swing 2, SfxPool_7:Melee Weapon Attack 4 |  |
| Weapons | Melee 'Sword of Fear' damages the player | FAIL | player hp lost 0.0 (item dmg 15), attacks 8, hitbox-active frames 109, hit events 12, sub-1hp chip 0.0, first hit never, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| Weapons | Melee 'Sword of Fear': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 0 (0 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 12, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.5m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 1.1s (at 1.5m), first damage never, attacks 8, attack-state frames 429, clips seen: Attack2Hand_C, WeakAttack_SwordA, WeakAttack_SwordB, WeakAttack_SwordC |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap2_sword

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'weap2_sword' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_7:Melee Weapon Air Swing 4, SfxPool_8:Melee Weapon Attack 4, SfxPool_9:Melee Weapon Air Swing 2, SfxPool_0:Melee Weapon Attack 3 |  |
| Weapons | Melee 'weap2_sword' damages the player | FAIL | player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 119, hit events 13, sub-1hp chip 0.0, first hit never, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 0 (0 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 13, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.4s (at 1.5m), first damage never, attacks 8, attack-state frames 446, clips seen: Attack2Hand_C, WeakAttack_SwordA, WeakAttack_SwordB, WeakAttack_SwordC |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap_two_handed

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | 'weap_two_handed' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_8:Melee Weapon Air Swing 1, SfxPool_9:Melee Weapon Attack 3 |  |
| Weapons | Melee 'weap_two_handed' damages the player | FAIL | player hp lost 0.0 (item dmg 28), attacks 9, hitbox-active frames 99, hit events 8, sub-1hp chip 0.0, first hit never, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=54 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 54 frames (definition missRate 0.25) |
| Weapons | Melee 'weap_two_handed': intentional miss rate vs configured chance | INFO | swings 9, intentional misses rolled 2 (22 %) vs configured 10 % (+-30 % at 3 sigma), weapon hit events 8, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.5m | only 9 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 1.1s (at 1.5m), first damage never, attacks 9, attack-state frames 382, clips seen: Attack2Hand_C, WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Wood Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 26 |  |
| Audio | 'Wood Axe' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_5:Melee Weapon Attack 1, SfxPool_6:no way out |  |
| Weapons | Melee 'Wood Axe' damages the player | FAIL | player hp lost 0.0 (item dmg 26), attacks 8, hitbox-active frames 100, hit events 13, sub-1hp chip 0.0, first hit never, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| Weapons | Melee 'Wood Axe': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 0 (0 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 13, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.5m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage never, attacks 8, attack-state frames 399, clips seen: WeakAttack_SwordA, WeakAttack_SwordB, Attack2Hand_C, WeakAttack_SwordC |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: sci_fi_pistol

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Wood_Axe]; item dmg 14 |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 13, attack-state frames 387, attacks 15, first hit never, range 10m (engage 16), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | 13 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | FAIL | shots/projectiles 13, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 36 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 9.6m | zero hits across 13 shots |
| Timing | Attack timing | INFO | first attack 1.1s (at 9.6m), first damage never, attacks 15, attack-state frames 387, clips seen: Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: survival_rifle

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_Wood_Axe]; item dmg 18 |  |
| Weapons | Ranged 'survival_rifle' damages the player | FAIL | player hp lost 0.0 (item dmg 18), shots/projectiles 13, attack-state frames 348, attacks 16, first hit never, range 11m (engage 25), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | 13 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) |
| Weapons | Ranged 'survival_rifle': hit rate vs configured accuracy | FAIL | shots/projectiles 13, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 36 %); item spread survival_rifle: projectileSpread 0.9 deg, accuracy 82, close-range scale 0.15 within 12m; the test player never blocks/dodges and stands still; closest approach 11.0m | zero hits across 13 shots |
| Timing | Attack timing | INFO | first attack 0.7s (at 11.0m), first damage never, attacks 16, attack-state frames 348, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: OVERRIDE both slots

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)) | PASS | resolved melee=Wood Axe (Project.Data.ItemData), ranged=survival_rifle (Project.Data.ItemData), active=survival_rifle (Project.Data.ItemData); active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_Wood_Axe] |  |
| Weapons | Starts with the ranged override drawn (prefer-ranged on, start-melee off) | PASS | active=survival_rifle (Project.Data.ItemData) |  |
| Weapons | Switch to the melee override via the single switch call | PASS | active=Wood Axe (Project.Data.ItemData); active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Survival_Rifle] |  |
| Weapons | Spawn config equal to the definition falls back to the definition | PASS | resolved melee=weap2_sword (Project.Data.ItemData), ranged=sci_fi_pistol (Project.Data.ItemData) |  |
| Weapons | Exactly one weapon drawn through the whole case | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

## Scenarios (block/parry, movement, multi-enemy, player weapons, performance)

| Scenario | PASS | FAIL | warn | time |
|---|---|---|---|---|
| Camera: faces forward after teleports, zoom in/out | 3 | 0 | 0 | 14s |
| Equip: weapon survives a teleport | 2 | 0 | 0 | 6s |
| Audio: swing / hit sound wiring (static) | 4 | 0 | 0 | 8s |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | 8 | 9 | 4 | 62s |
| Block + parry: player defends against enemy melee | 1 | 1 | 2 | 53s |
| Block: enemy defends against player swings | 1 | 0 | 1 | 13s |
| Movement: retreat, push, flank, backstab, lose target | 4 | 0 | 1 | 34s |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | 3 | 0 | 6 | 100s |
| Multi-enemy: FRED x3 and x5 | 1 | 0 | 2 | 36s |
| Brain: orbit, toggles, archetypes | 9 | 0 | 3 | 129s |
| Player weapons: each melee item vs enemy (swapped in place) | 20 | 2 | 1 | 52s |
| Ammo: default ammo per ranged weapon + ammo-specific FX | 29 | 6 | 0 | 31s |
| Player rig: enemy ranged shot hits a body bone + mark | 3 | 2 | 0 | 30s |
| Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | 6 | 0 | 0 | 33s |
| Behavior tweaks: over / under-exaggerated enemy values (runtime only) | 14 | 0 | 0 | 99s |
| Regression of earlier fixes and tweaks | 7 | 0 | 1 | 0s |

### Scenario: Camera: faces forward after teleports, zoom in/out

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Camera | Camera sits behind the player and faces the same way after teleports | PASS | yaw 0: dot 1.00; yaw 90: dot 1.00; yaw 180: dot 1.00; yaw 270: dot 1.00 (camera vThirdPersonCamera) |  |
| Camera | Camera zooms in and out | PASS | distance start 3.0m, zoomed in 2.5m, zoomed out 4.0m via scroll wheel |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Equip: weapon survives a teleport

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Equip | Equipped weapon survives a teleport | PASS | armed 'sci_fi_pistol' after 'weap2_sword' (armed ok True); after teleport: unchanged for 4s, drawn True, active slot 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Audio: swing / hit sound wiring (static)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Audio | GameAudioProfile combat clips assigned | PASS | weaponSwingClips 4, weaponHitClips 3, weaponCriticalHitClips 2 |  |
| Audio | Swing swish (GameAudioManager.PlayWeaponSwing) is called by gameplay code | PASS | callers outside GameAudioManager: vMeleeManager.cs |  |
| Audio | Melee hit sound has a caller on the damage path | PASS | CombatHitAudio.PlayWeaponHit callers: PioneerInvectorDamageReceiver.cs, MeleeCombatController.cs, vMeleeManager.cs |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Controls | Input path diagnostics (W held 0.35s) | INFO | isFocused True, cursor Locked, keyboard id 1 enabled True W pressed False, mouse enabled, keyboards 1, gamepads 0, InputSystem IgnoreFocus/AllDeviceInputAlwaysGoesToGameView, Player_v7 Variant: scheme '', enabled False, actions enabled True, devices [], Move (0.00, 0.00); Player_v7 Combat Variant: scheme 'Keyboard&Mouse', enabled True, actions enabled True, devices [Keyboard,Mouse], Move (0.00, 0.00) // while W held: isFocused True, cursor Locked, keyboard id 1 enabled True W pressed False, mouse enabled, keyboards 1, gamepads 0, InputSystem IgnoreFocus/AllDeviceInputAlwaysGoesToGameView, Player_v7 Variant: scheme '', enabled False, actions enabled True, devices [], Move (0.00, 0.00); Player_v7 Combat Variant: scheme 'Keyboard&Mouse', enabled True, actions enabled True, devices [Keyboard,Mouse], Move (0.00, 0.00) |  |
| Controls | W moves the player forward (camera direction) | FAIL | start 0.00, min -1.74, max 0.00, end -1.80 m along forward | Move action not reaching the controller: PlayerInput disabled, move locked, or PioneerPlayerInputBinder.HandleAction('Move') not wired |
| Controls | S moves the player back | FAIL | start 0.00, min 0.00, max 1.35, end 1.43 m along forward | Move (S) produced no backward displacement |
| Controls | A strafes left | FAIL | start 0.00, min -1.69, max 0.00, end -1.75 m along left | Move (A) produced no leftward displacement |
| Controls | D strafes right | FAIL | start 0.01, min 0.01, max 0.36, end 0.38 m along right | Move (D) produced no rightward displacement |
| Controls | W run speed (reference for sprint) | PASS | start 0.00, min 0.00, max 1.59, end 1.59 m in 1.5s |  |
| Controls | Shift + W sprints faster than W | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 m in 1.5s | sprint no faster than run (1.6m): Sprint action not wired, stamina 0, or sprint speed equals run speed |
| Controls | Space jumps | FAIL | start 21.23, min 21.23, max 21.23, end 21.23 m height (+1000 while isJumping) | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked; state before the press: isGrounded True, isJumping False, customAction False, isCrouching False, stamina 1000 |
| Controls | Left Ctrl crouches | WARN | start 0.00, min 0.00, max 0.00, end 0.00 isCrouching | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) |
| Controls | Crouch through the controller code path (cc.Crouch) | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isCrouching |  |
| Controls | Q dodge / roll | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 m or isRolling | Dodge action did not move the player: TryDodgeRollFromInput refused (stamina, cooldown, no roll clip) |
| Controls | Climbing unlocked and controller enabled (not driven: needs a wall) | PASS | DMPlayerSystemsProfile present: ClimbEnabled=True, DashEnabled=True, JetpackEnabled=True; DMClimbController enabled, DMDashController enabled, DMJetpackController enabled |  |
| Controls | Movement systems unlocked for the run (jetpack, dash, climb) | INFO | DMPlayerSystemsProfile present: ClimbEnabled=True, DashEnabled=True, JetpackEnabled=True; DMClimbController enabled, DMDashController enabled, DMJetpackController enabled. The audit unlocks these in memory only (see environment notes) and restores them at exit. |  |
| Controls | Left Alt dash | WARN | start 0.00, min 0.00, max 0.00, end 0.00 m | Dash did not move the player even though the skill gate is unlocked for this run (see ctl.systems row): DMDashController disabled, on cooldown, stamina, or the Dash action not bound to Left Alt |
| Controls | Hold Space after jumping boosts (jetpack) | WARN | start 21.23, min 21.23, max 21.23, end 21.23 m height | no sustained lift although the jetpack skill gate is unlocked for this run (see ctl.systems row): fuel 0, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player |
| Controls | Mouse look left/right orbits the camera | PASS | start 177.83, min 0.00, max 177.83, end 96.01 deg yaw |  |
| Controls | Mouse look up/down tilts the camera | PASS | start 6.68, min 0.00, max 25.31, end 25.31 deg pitch |  |
| Controls | Left click swings the drawn melee weapon | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 isAttacking | melee Attack input not received: drawn melee tap/hold logic (UpdateDrawnMeleeCharge) did not start a swing |
| Controls | Right click blocks (melee drawn) | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 isBlocking | Block input (Mouse rightButton / Block action) did not set isBlocking: stamina 0, customAction, not grounded, or still attacking |
| Controls | Middle click does not break gameplay | PASS | start 0.00, min 0.00, max 0.00, end 0.00 none |  |
| Controls | Middle click has no gameplay binding | INFO | InputSystem_Actions Player map binds no action to Mouse/middleButton (only the UI map MiddleClick) |  |
| Controls | Tab (SwitchWeapon) cycles the weapon focus | WARN | Hot Cross weapon focus 0 -> 0, drawn weap2_sword -> weap2_sword | Tab tap did not change focus: only one weapon on the hotbar, Hot Cross not polling, or tap/hold threshold (hold arms) not met |
| Controls | Half-tilt stick walks slower than a run | PASS | start 0.00, min 0.00, max 0.84, end 0.86 m in 1.5s |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block + parry: player defends against enemy melee

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Unblocked baseline (same enemy, same distance) | INFO | hits 1, player hp lost 0.0 |  |
| Block | Holding block negates enemy melee damage | FAIL | BLOCK = hold right mouse. isBlocking False (raised in 1.52s), hits 2, blocked 0, player hp lost 0.0 (unblocked 0.0), stamina 1000.0 | player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking |
| Block | Enemy guard-break stagger after a blocked hit | WARN | longest enemy stagger 0.00s (profile blockStaggerSeconds 1.2s) | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) |
| Parry | Parry = tap right mouse, timed before the incoming hit (several timings) | INFO | tap -0.60s vs predicted hit (windup 1.10s): blow landed 3.16s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 0.0 // tap -0.40s vs predicted hit (windup 1.10s): blow landed 2.05s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 0.0 // tap -0.25s vs predicted hit (windup 1.10s): blow landed 2.23s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 0.0 // tap -0.15s vs predicted hit (windup 1.10s): blow landed 2.99s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 0.0 // tap -0.08s vs predicted hit (windup 1.10s): blow landed 3.03s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 0.0 // tap -0.02s vs predicted hit (windup 1.10s): blow landed 3.48s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 0.0 // tap +0.10s vs predicted hit (windup 1.10s): blow landed 1.66s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 0.0 | input: PARRY = TAP right mouse (0.08s), BLOCK = HOLD right mouse; profile: parryWindowSeconds 0.2, blockStagger 1.2s, parryStagger 5.0s |
| Parry | A tap inside the parry window parries (and an early/late tap does not) | WARN | no parry observed (0 taps landed inside the window; see sweep rows) | no sampled tap produced a parry: enemy attack timing varies between swings (windup from the baseline swing may not match), or the parry window/stagger in the profile differs from what the rows show |
| Parry | A successful parry staggers the attacker (counter opening) | INFO | no parry happened, so the counter stagger could not be observed |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block: enemy defends against player swings

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Enemy block reduces player melee damage | WARN | open: 0 hits, enemy hp lost 0.0; blocking (animator IsBlocking True, duration 40.0s): 19 hits (2 flagged blocked), enemy hp lost 5.2; block clips: Idle, Walk, Idle@Pistol, Defense_HoldingSword | player swings registered no hits on the open enemy, so the comparison is inconclusive (input focus, reach, or the player weapon) |
| Parry | Enemy parry | INFO | not implemented: EnemyInvectorCombatBridge only has TryBeginBlock/EndBlock (no parry window/riposte API) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Movement: retreat, push, flank, backstab, lose target

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Movement | Player retreats 9m at 3 m/s: enemy follows | PASS | distance 3.2m -> 3.6m (min 2.1m), states Chase/Attack, attacks 1 |  |
| Movement | Player pushes into the enemy: spacing and facing | PASS | min distance 0.50m, max facing error 0deg, attack-state frames 70, states Attack/Chase |  |
| Movement | Player circles behind the enemy: enemy turns to face | WARN | reached behind at never, enemy faced player (<30deg) after never, max facing error 0deg, attacks while behind 0, player hp lost 0.0, states Attack | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player |
| Movement | Hit from behind vs from the front (player melee, AI off) | INFO | front: 25 hits, 78.2 hp; behind: 33 hits, 82.5 hp; ratio 1.06 | no backstab bonus implemented if ratio is 1.0 |
| Movement | Player disappears 70m away: enemy loses the target | PASS | was aware True, investigate/search after never, back to calm state after 6.0s, states Chase/ReturnHome |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Play | Approach with W: enemy notices | WARN | walked 21.1m in 11.3s to 8.9m; enemy noticed after never at n/a; state Wander | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) |
| Play | Run away with S: enemy pursues | WARN | player backed away for 4.0s (gap now 21.3m), enemy moved 6.3m, hostile state seen False, states Wander | enemy did not chase a retreating player: chase blocked (standoff/hold token, ranged keep-distance) or target lost immediately |
| Play | Strafe around the enemy: it turns to face the player | WARN | enemy facing error avg 90deg, max 179deg over 119 frames while strafing D then A, state Wander | enemy did not keep facing a strafing player: turn speed too slow, turn-in-place gated, or enemy in a state that ignores the target |
| Play | Retreat then re-engage: enemy fights back | PASS | re-engaged to 1.2m, enemy attacks 2, player hp lost 0.0, state Attack |  |
| Play | Jump away: player clears distance | PASS | jump height 0.91m, gap 1.2m -> 4.7m, enemy state Attack | player could not gain distance (enemy kept pace or player blocked) |
| Play | Jetpack escape (hold Space + S) | WARN | peak height gain 0.9m, gap 4.7m -> 2.0m, enemy state Attack | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump |
| Play | Sprint away: enemy loses the target | WARN | lost after never at n/am (sprinting), state Attack, states Wander/Hold/Chase/Attack/Defensive | enemy kept hostile for 30s of sprinting away: lose-target distance/time too long (senses) or leash not applied |
| Play | Enemy returns to its post after losing the player | WARN | back home after never, now 61.0m from spawn, state Attack | enemy did not walk back to its spawn within 30s: ReturnHome blocked, leash undefined, or it stays in Search/Investigate |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Multi-enemy: FRED x3 and x5

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Multi-enemy | Enemy used | INFO | Hybrid Droid (FRED / Hybrid Droid) |  |
| Multi-enemy | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 2 (144/392 frames with >1), distinct attackers 3/3, attacker hand-offs 12, attack-state frames [295,211,250], chase/hold frames [96,178,140], closest pair 1.04m, closest to player 1.16m, orbit around player avg 49deg max 75deg (net 19/-45/-45), player hp lost 0, brain actions Press:877/Hold:293/None:6 | 144/392 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Multi-enemy | 5 FREDs at once for 18s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 2 (223/375 frames with >1), distinct attackers 5/5, attacker hand-offs 12, attack-state frames [220,200,196,251,171], chase/hold frames [152,173,178,121,202], closest pair 0.80m, closest to player 1.16m, orbit around player avg 88deg max 155deg (net 43/37/-2/100/81), player hp lost 0, brain actions Press:1176/Hold:688/None:11 | 223/375 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Brain: orbit, toggles, archetypes

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Brain | Single FRED, player standing still: does not orbit the player | PASS | notice 0.1s, chase 0.1s, first attack 1.0s, attacks 4, dmg 0, min dist 1.2m, path 5.9m, yaw 25deg, orbit around player net 25deg / abs 25deg, avg facing error 0deg, brain Press:287, states Chase/Attack |  |
| Brain | Single FRED, player strafes around it: keeps facing the player, does not orbit on its own | PASS | player circled 4.0m radius for 9s; enemy avg facing error 4deg, within 40deg 100% of frames, enemy angular travel around the player 289deg, states Attack/Chase, path 12.0m |  |
| Multi-enemy | 3 FREDs, baseline (brain on, director on) | WARN | max simultaneous attackers 2 (57/267 frames with >1), distinct attackers 3/3, attacker hand-offs 7, attack-state frames [168,86,161], chase/hold frames [97,180,103], closest pair 2.16m, closest to player 1.16m, orbit around player avg 30deg max 68deg (net 22/0/1), player hp lost 0, brain actions Press:516/Hold:279/None:6 | 57/267 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:516/Hold:279/None:6 |  |
| Multi-enemy | 3 FREDs, enableUtilityBrain=false | PASS | max simultaneous attackers 2 (139/286 frames with >1), distinct attackers 3/3, attacker hand-offs 9, attack-state frames [112,245,234], chase/hold frames [171,39,51], closest pair 1.84m, closest to player 1.16m, orbit around player avg 8deg max 22deg (net 18/1/0), player hp lost 0, brain actions None:852/Press:6 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions None:852/Press:6 |  |
| Multi-enemy | 3 FREDs, enableEngagementDirector=false | PASS | max simultaneous attackers 3 (165/252 frames with >1), distinct attackers 3/3, attacker hand-offs 10, attack-state frames [214,218,208], chase/hold frames [36,33,41], closest pair 2.23m, closest to player 1.26m, orbit around player avg 1deg max 2deg (net 0/0/-2), player hp lost 0, brain actions None:576/Press:180 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = False; brain actions None:576/Press:180 |  |
| Multi-enemy | 3 FREDs, legacyRingSlotRotation=true | WARN | max simultaneous attackers 2 (23/277 frames with >1), distinct attackers 3/3, attacker hand-offs 3, attack-state frames [147,217,197], chase/hold frames [128,59,77], closest pair 1.27m, closest to player 1.16m, orbit around player avg 67deg max 85deg (net -59/-37/29), player hp lost 0, brain actions Press:654/Hold:171/None:6 | 23/277 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:654/Hold:171/None:6 |  |
| Multi-enemy | 3 FREDs, brain off + director off | PASS | max simultaneous attackers 3 (162/249 frames with >1), distinct attackers 3/3, attacker hand-offs 8, attack-state frames [207,211,215], chase/hold frames [39,36,33], closest pair 2.18m, closest to player 1.26m, orbit around player avg 1deg max 1deg (net -1/-1/0), player hp lost 0, brain actions None:393/Press:354 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = False; brain actions None:393/Press:354 |  |
| Brain | enableUtilityBrain=false stops the brain acting (no Press/Defend/Retreat/Hold) | WARN | distinct brain actions: brain on 2 (None:6/Hold:279/Press:516), brain off 1 (None:852/Press:6) | the brain still selected actions with enableUtilityBrain=false (a per-type ProfileOverride, or a cached decision) |
| Brain | enableEngagementDirector=false: no single-attacker token, enemies fall back to the old behaviour | PASS | with the director on: max simultaneous attackers 2 (57 frames with >1); director off: 3 (165 frames with >1), distinct attackers 3 vs 3, hand-offs 7 vs 10 |  |
| Brain | legacyRingSlotRotation=true restores the old ring-slot rotation (more orbiting than the director) | PASS | orbit around the player avg 30deg (director) vs 67deg (legacy ring), distinct attackers 3 vs 3, hand-offs 7 vs 3, max simultaneous attackers 2 vs 2 |  |
| Brain | Archetype Duelist: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 0.8s, attacks 5, dmg 0, min dist 1.2m, path 6.8m, yaw 116deg, orbit around player net 40deg / abs 116deg, avg facing error 0deg, brain Press:238/None:1, states Chase/Attack |  |
| Brain | Archetype Rusher: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 0.9s, attacks 4, dmg 0, min dist 1.2m, path 6.1m, yaw 90deg, orbit around player net 87deg / abs 90deg, avg facing error 0deg, brain Press:256, states Chase/Attack |  |
| Brain | Archetype Berserker: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 5, dmg 0, min dist 1.2m, path 4.9m, yaw 37deg, orbit around player net -37deg / abs 37deg, avg facing error 0deg, brain Press:241, states Chase/Attack |  |
| Brain | Personality Cowardly (Duelist): brain actions and attack rate | INFO | notice 0.1s, chase 0.1s, first attack 1.3s, attacks 4, dmg 0, min dist 1.3m, path 6.5m, yaw 101deg, orbit around player net 24deg / abs 101deg, avg facing error 0deg, brain Press:255, states Chase/Attack |  |
| Brain | Brain DOES | INFO | scores Press / Defend / Retreat / Hold every decisionInterval from archetype rows (press, defend, retreat, tokenBias, holderTaunt, holderFeint) x personality multipliers x health condition (Healthy/Injured/SeverelyInjured/Critical); feeds the engagement director a token bias; scales holder idle actions (sidestep / taunt / feint); issues defend requests (block vs dodge) and retreat windows; reacts to intentional melee misses (miss recovery). Observed actions per archetype are in the rows above. |  |
| Brain | Brain does NOT | INFO | no weapon choice: DMEnemyBrain contains no weapon-switch call (weapon selection stays with the loadout bridge / combat bridge; brain-driven weapon choice and grenade throwing are deferred). It does not move the enemy itself (movement stays in EnemyAiController / the engagement director), does not pick targets, and does nothing when enableUtilityBrain is off. |  |
| Brain | Toggle / archetype tests left nothing on disk | PASS | Resources/Combat profiles, Data/Enemies definitions and enemy prefabs hashed before and after: identical |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player weapons: each melee item vs enemy (swapped in place)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Audio | Player '2 Hander' swing plays a swish | PASS | 3/4 clicks started a swing; 4 non-ambient audio start(s) outside the hit windows: SfxPool_1:neverworld, SfxPool_2:Melee Weapon Air Swing 3, SfxPool_3:Melee Weapon Attack 29 |  |
| Audio | Player '2 Hander' hitting the enemy plays a hit sound | PASS | 33/33 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee '2 Hander' on the enemy | PASS | enemy hp lost 205.7 (item dmg 28), hits 33, player right weapon Drawn_2_Hander |  |
| Audio | Player 'Blade Of Chaos' swing plays a swish | PASS | 3/4 clicks started a swing; 2 non-ambient audio start(s) outside the hit windows: SfxPool_4:Melee Weapon Air Swing 2, SfxPool_5:Melee Weapon Attack 4, SfxPool_1:Melee Weapon Attack 4 |  |
| Audio | Player 'Blade Of Chaos' hitting the enemy plays a hit sound | PASS | 34/34 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Blade Of Chaos' on the enemy | PASS | enemy hp lost 36.8 (item dmg 21), hits 34, player right weapon Drawn_Blade_of_Chaos |  |
| Audio | Player 'Death Axe' swing plays a swish | PASS | 3/4 clicks started a swing; 3 non-ambient audio start(s) outside the hit windows: SfxPool_6:Melee Weapon Air Swing 2, GameAudioManager:(one-shot), SfxPool_1:Melee Weapon Attack 4 |  |
| Audio | Player 'Death Axe' hitting the enemy plays a hit sound | PASS | 20/20 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Death Axe' on the enemy | PASS | enemy hp lost 55.8 (item dmg 18), hits 20, player right weapon Drawn_Death_Axe |  |
| Audio | Player 'Spear of Fate' swing plays a swish | PASS | 4/4 clicks started a swing; 5 non-ambient audio start(s) outside the hit windows: SfxPool_8:neverworld, SfxPool_7:Melee Weapon Air Swing 3, SfxPool_1:Melee Weapon Attack 4 |  |
| Audio | Player 'Spear of Fate' hitting the enemy plays a hit sound | PASS | 38/38 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Spear of Fate' on the enemy | PASS | enemy hp lost 109.5 (item dmg 16), hits 38, player right weapon Drawn_Spear_of_Fate |  |
| Audio | Player 'Sword of Fear' swing plays a swish | PASS | 4/4 clicks started a swing; 4 non-ambient audio start(s) outside the hit windows: SfxPool_7:Melee Weapon Air Swing 4, SfxPool_1:Melee Weapon Attack 4, SfxPool_9:Melee Weapon Attack 1 |  |
| Audio | Player 'Sword of Fear' hitting the enemy plays a hit sound | PASS | 14/14 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Sword of Fear' on the enemy | PASS | enemy hp lost 86.1 (item dmg 15), hits 14, player right weapon Drawn_Sword_of_Fear |  |
| Audio | Player 'weap2_sword' swing plays a swish | PASS | 4/4 clicks started a swing; 6 non-ambient audio start(s) outside the hit windows: SfxPool_5:Melee Weapon Air Swing 4, SfxPool_7:Melee Weapon Air Swing 1, SfxPool_9:neverworld |  |
| Player weapons | Player uses melee 'weap2_sword' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon Drawn_weap2_sword | 4/4 clicks swung, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'weap_two_handed' swing plays a swish | PASS | 4/4 clicks started a swing; 4 non-ambient audio start(s) outside the hit windows: SfxPool_7:Melee Weapon Air Swing 1, SfxPool_9:Melee Weapon Air Swing 4, SfxPool_3:Melee Weapon Air Swing 4 |  |
| Audio | Player 'weap_two_handed' hitting the enemy plays a hit sound | PASS | 4/4 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'weap_two_handed' on the enemy | PASS | enemy hp lost 170.9 (item dmg 28), hits 4, player right weapon Drawn_Two_Handed_Sword |  |
| Audio | Player 'Wood Axe' swing plays a swish | PASS | 4/4 clicks started a swing; 6 non-ambient audio start(s) outside the hit windows: SfxPool_2:Melee Weapon Air Swing 2, SfxPool_4:no way out, SfxPool_7:Melee Weapon Air Swing 2 |  |
| Player weapons | Player uses melee 'Wood Axe' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 26), hits 0, player right weapon Drawn_Wood_Axe | 4/4 clicks swung, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Console | Console errors/warnings | WARN | 0 errors, 68 warning(s) | [Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0. |

- `[Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0.`
- `[Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0.`
- `[Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0.`
- `[Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0.`
- `[Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0.`
- `[Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0.`
- `[Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0.`
- `[Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0.`

### Scenario: Ammo: default ammo per ranged weapon + ammo-specific FX

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Ammo | Ammo items and ranged weapons found | INFO | 10 ammo item(s): DMAmmoFxProfile_CarveExperiment(Gunpowder), DMAmmoFxProfile_Electricity(Electricity), DMAmmoFxProfile_Explosive(Explosive), DMAmmoFxProfile_Fire(Fire), DMAmmoFxProfile_Ice(Ice), DMAmmoFxProfile_Ion(Ion), DMAmmoFxProfile_Laser(Laser), DMAmmoFxProfile_Plasma(Plasma), DMAmmoFxProfile_Resonance Stabilizer(ResonanceStabilizer), DMAmmoFxProfile_Standard(Gunpowder); 2 ranged weapon(s): sci_fi_pistol, survival_rifle |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: fires (rounds consumed) | FAIL | 0 trigger pull(s), rounds 30 -> 30, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: damages the enemy | PASS | enemy hp lost 62.6 (weapon dmg 14, ammo type Gunpowder, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: hit rate over aimed shots (shots may miss by design) | INFO | 3/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: hit marks / FX | PASS | hit marks stamped 0 -> 3, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 0 -> 3, enemy hp lost 62.6 |  |
| Player weapons | Player uses ranged 'sci_fi_pistol' on the enemy (default ammo) | PASS | enemy hp lost 62.6 (item dmg 14), rounds consumed 0 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: fires (rounds consumed) | FAIL | 0 trigger pull(s), rounds 30 -> 30, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: damages the enemy | PASS | enemy hp lost 103.1 (weapon dmg 18, ammo type Gunpowder, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: hit rate over aimed shots (shots may miss by design) | INFO | 3/0 aimed shots hit (0 %); weapon accuracy 82 (effective incl. skill bonus 75), ammo accuracy 75, spread 0.9 deg (ammo 1.4), close-range scale 0.15 within 12m, hip multiplier 1; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: hit marks / FX | PASS | hit marks stamped 3 -> 8, body type Humanoid |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 3 -> 8, enemy hp lost 103.1 |  |
| Player weapons | Player uses ranged 'survival_rifle' on the enemy (default ammo) | PASS | enemy hp lost 103.1 (item dmg 18), rounds consumed 0 |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_CarveExperiment', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: fires | FAIL | 0 trigger pull(s), rounds 30 -> 30 | no round consumed with this ammo type loaded |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: damages the enemy | PASS | enemy hp lost 28.9 (ammo type Gunpowder) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: hit rate over aimed shots (shots may miss by design) | INFO | 2/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 92), ammo accuracy 92, spread 1.8 deg (ammo 0.8), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: hit marks / FX | PASS | hit marks stamped 8 -> 10, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Projectile, useHitMarks=True; hit marks 8 -> 10, enemy hp lost 28.9 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Explosive', ammoType Explosive |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires | FAIL | 0 trigger pull(s), rounds 30 -> 30 | no round consumed with this ammo type loaded |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: damages the enemy | PASS | enemy hp lost 103.8 (ammo type Explosive) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: hit rate over aimed shots (shots may miss by design) | INFO | 2/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: hit marks / FX | PASS | hit marks stamped 10 -> 12, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=True; hit marks 10 -> 12, enemy hp lost 103.8 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ion', ammoType Ion |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: fires | FAIL | 0 trigger pull(s), rounds 30 -> 30 | no round consumed with this ammo type loaded |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: damages the enemy | PASS | enemy hp lost 110.0 (ammo type Ion) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: hit rate over aimed shots (shots may miss by design) | INFO | 2/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 89), ammo accuracy 89, spread 1.8 deg (ammo 1.2), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: hit marks / FX | INFO | hit marks stamped 12 -> 12, body type Humanoid | this ammo type is configured to leave no hit marks (useHitMarks off / blood and burn Never): by design |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=False; hit marks 12 -> 12, enemy hp lost 110.0 | useHitMarks is off on this ammo: no mark by design |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Laser', ammoType Laser |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: fires | FAIL | 0 trigger pull(s), rounds 30 -> 30 | no round consumed with this ammo type loaded |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: damages the enemy | PASS | enemy hp lost 104.6 (ammo type Laser) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: hit rate over aimed shots (shots may miss by design) | INFO | 2/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.5), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: hit marks / FX | PASS | hit marks stamped 12 -> 14, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=False; hit marks 12 -> 14, enemy hp lost 104.6 |  |
| Ammo | X (AmmoCycle) switches the loaded ammo type | PASS | 3 ammo stack(s) on the Hot Cross utility slots; loaded DMAmmoFxProfile_Laser -> DMAmmoFxProfile_CarveExperiment -> DMAmmoFxProfile_Electricity |  |
| Ammo | Ammo coverage | INFO | 2 weapon(s) tested with their default ammo (full: load, fill, fire, damage, marks, empty, reload); 4 ammo-specific case(s) (Quick: only ammo types with special FX rules, max 4), each once on a host weapon (load, 2 shots, damage, marks, per-ammo FX rules); no weapon x ammo matrix. Mining tool excluded: it runs on Plasma Fuel charge. |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player rig: enemy ranged shot hits a body bone + mark

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Player rig | Player hitbox rig is built and active (per-bone hitboxes on layer DMHitbox) | PASS | rig present, active True, active rigs 1, hitboxes 18, not on the DMHitbox layer 0, body type Humanoid |  |
| Player rig | Enemy ranged shot ('survival_rifle') resolves to a player body hitbox | FAIL | player hp lost 0.0, shots/projectiles 12, rig marks 314 -> 327, active mark objects 5: on a bone 5, on the root/capsule 0, parents [Head, Head, Spine01, Spine01, Spine01], range 11m, states Wander/Attack | enemy ranged shot did no damage to the player: the shot missed or the hitscan still passes through (the rig supersedes the capsule but nothing resolves on the bone hitboxes) |
| Player rig | Player hit marks are small and shallow (not head-sized, not projected through the body) | FAIL | 10 projector(s): largest footprint 1.000m, deepest projection 1.000m (profile playerMarkSizeScale 0.4, playerMarkProjectionDepth 0.16); a 0.2m enemy mark would cover most of a head | player marks are larger or deeper than a small bullet decal: check DM_EnemyHitMarkProfile playerMarkSizeScale / playerMarkProjectionDepth |
| Player rig | Player hit marks are removed automatically after ~10s (fade, then clear) | PASS | 5 mark object(s) active right after the shots, 0 still active 11.0s later (playerMarkLifetimeSeconds 10, fade 1.5s) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Aim | Aim follows the moving enemy: chest | PASS | 3 shots, 0 fired, aim error before firing avg 0.9deg; crosshair hitbox zone Torso (bone VBOT_:Spine2); hit-mark bones VBOT_:Spine2; avg damage 12.7 (x1.00 chest) |  |
| Aim | Aim follows the moving enemy: head | PASS | 3 shots, 0 fired, aim error before firing avg 0.4deg; crosshair hitbox zone Head (bone VBOT_:Head); hit-mark bones VBOT_:RightArm, VBOT_:Head; avg damage 24.3 (x1.91 chest) |  |
| Aim | Aim follows the moving enemy: arm | PASS | 3 shots, 0 fired, aim error before firing avg 1.0deg; crosshair hitbox zone none/Torso (bone none/VBOT_:Spine2); hit-mark bones VBOT_:Head; avg damage 8.7 (x0.68 chest) |  |
| Aim | Aim follows the moving enemy: leg | PASS | 3 shots, 0 fired, aim error before firing avg 0.7deg; crosshair hitbox zone Torso/none (bone VBOT_:Hips/none); hit-mark bones VBOT_:Hips; avg damage 13.9 (x1.10 chest) |  |
| Aim | Aim follows the moving enemy: foot | PASS | 3 shots, 0 fired, aim error before firing avg 0.8deg; crosshair hitbox zone none/Leg (bone none/VBOT_:LeftFoot); hit-mark bones VBOT_:LeftFoot; avg damage 5.3 (x0.42 chest) |  |
| Aim | Body-part damage table (for later headshot/limb multipliers) | INFO | chest: zone Torso, avg dmg 12.7, x1.00 vs chest / head: zone Head, avg dmg 24.3, x1.91 vs chest / arm: zone none/Torso, avg dmg 8.7, x0.68 vs chest / leg: zone Torso/none, avg dmg 13.9, x1.10 vs chest / foot: zone none/Leg, avg dmg 5.3, x0.42 vs chest | damage multipliers per zone come from DMCombatBodyPartUtility.ApplyZoneDamageMultiplier (DM_EnemyHitMarkProfile); equal ratios here mean no zone multiplier is configured |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Behavior tweaks: over / under-exaggerated enemy values (runtime only)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Behavior | How the tweaks are applied | INFO | runtime only: values are written to the live component instances of a freshly spawned enemy (destroyed after each variation) and profile tweaks go through in-memory clones swapped in for the global Resources profile and restored afterwards; a disk hash of the profiles, definitions and enemy prefabs is compared at the end. Hit-mark / effect budgets are not covered here (they are visual budgets, see the hit-mark rows). |  |
| Behavior | Normal behaviour (reference): player 9m in front, 8s | INFO | notice 0.0s, chase 0.0s, first attack 1.5s, attacks 3, dmg 0, min dist 1.2m, path 7.5m, yaw 14deg, orbit around player net 14deg / abs 14deg, avg facing error 0deg, brain Press:193/Hold:15, states Hold/Chase/Attack |  |
| Behavior | Aggression / attack rate: high (hi = attacks much faster) | PASS | EnemyCombat.attackCooldown 1 -> 0.25, EnemyCombat.meleeRecoveryPauseMin 0.6 -> 0.15, EnemyCombat.meleeRecoveryPauseMax 1.8 -> 0.45; notice 0.1s, chase 0.1s, first attack 1.4s, attacks 3, dmg 0, min dist 1.2m, path 7.7m, yaw 24deg, orbit around player net 24deg / abs 24deg, avg facing error 0deg, brain Press:194/Hold:12/None:1, states Hold/Chase/Attack |  |
| Behavior | Aggression / attack rate: low (hi = attacks much faster) | PASS | EnemyCombat.attackCooldown 1 -> 3, EnemyCombat.meleeRecoveryPauseMin 0.6 -> 1.8, EnemyCombat.meleeRecoveryPauseMax 1.8 -> 5.4; notice 0.0s, chase 0.0s, first attack 1.5s, attacks 3, dmg 0, min dist 1.3m, path 8.7m, yaw 80deg, orbit around player net 0deg / abs 80deg, avg facing error 0deg, brain Press:201/Hold:9, states Hold/Chase/Attack |  |
| Behavior | Aggression / attack rate: suggested safe range | PASS | both x0.25 and x3 of the normal values stayed healthy: safe range at least x3 .. x0.25 |  |
| Behavior | Standoff distance: high (hi = stands further off) | PASS | EnemyAiController.attackStandoffFraction 0.62 -> 0.992, EnemyAiController.minCombatSeparation 0.95 -> 1.1; notice 0.1s, chase 0.1s, first attack 1.3s, attacks 3, dmg 0, min dist 1.2m, path 7.9m, yaw 47deg, orbit around player net -47deg / abs 47deg, avg facing error 0deg, brain Press:205/Hold:11, states Hold/Chase/Attack |  |
| Behavior | Standoff distance: low (hi = stands further off) | PASS | EnemyAiController.attackStandoffFraction 0.62 -> 0.248, EnemyAiController.minCombatSeparation 0.95 -> 0.38; notice 0.0s, chase 0.0s, first attack 1.4s, attacks 3, dmg 0, min dist 1.2m, path 7.1m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:223/Hold:10, states Hold/Chase/Attack |  |
| Behavior | Standoff distance: suggested safe range | PASS | both x1.6 and x0.4 of the normal values stayed healthy: safe range at least x0.4 .. x1.6 |  |
| Behavior | Chase / lose-target timings: high (hi = never gives up, lo = gives up at once) | PASS | EnemyAiController.loseTargetDelay 4 -> 16, EnemyAiController.maxChaseDuration 6 -> 24, EnemyAiController.chaseSpeedMultiplier 0.95 -> 3.8; notice 0.1s, chase 0.1s, first attack 0.5s, attacks 3, dmg 0, min dist 1.5m, path 5.9m, yaw 24deg, orbit around player net -22deg / abs 22deg, avg facing error 0deg, never lost the target within 14s, brain Press:212/Hold:12, states Hold/Chase/Attack |  |
| Behavior | Chase / lose-target timings: low (hi = never gives up, lo = gives up at once) | PASS | EnemyAiController.loseTargetDelay 4 -> 0.8, EnemyAiController.maxChaseDuration 6 -> 1.2, EnemyAiController.chaseSpeedMultiplier 0.95 -> 0.19; notice 0.0s, chase 0.0s, first attack 2.8s, attacks 3, dmg 0, min dist 1.3m, path 7.3m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, lost target after 0.8s, brain Press:234/Hold:13, states Hold/Chase/ReturnHome/Attack |  |
| Behavior | Chase / lose-target timings: suggested safe range | PASS | both x4 and x0.2 of the normal values stayed healthy: safe range at least x0.2 .. x4 |  |
| Behavior | Brain archetype weights: high (hi = press x3 / defend x0.3 / retreat 0, lo = press x0.2 / defend x3 / retreat x3) | PASS | archetype rows: press x3, defend x0.3, retreat x0; notice 0.0s, chase 0.0s, first attack 0.9s, attacks 4, dmg 0, min dist 1.2m, path 5.7m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:245, states Chase/Attack |  |
| Behavior | Brain archetype weights: low (hi = press x3 / defend x0.3 / retreat 0, lo = press x0.2 / defend x3 / retreat x3) | PASS | archetype rows: press x0.2, defend x3, retreat x3; notice 0.0s, chase 0.0s, first attack 2.3s, attacks 2, dmg 0, min dist 1.2m, path 13.1m, yaw 66deg, orbit around player net -66deg / abs 66deg, avg facing error 0deg, brain Press:206/Defend:39/None:1, states Chase/Defensive/Attack |  |
| Behavior | Brain archetype weights: suggested safe range | PASS | both x3 and x0.2 of the normal values stayed healthy: safe range at least x0.2 .. x3 |  |
| Behavior | Tweaks left nothing on disk (profiles, definitions, enemy prefabs hashed before/after) | PASS | identical |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | PASS | code present: True; 16 PASS / 0 FAIL / 0 warn of 16 runtime row(s) |  |
| Fix check | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | PASS | 1 PASS / 0 FAIL / 0 warn of 1 runtime row(s); weapon reverts recorded this run: 0 |  |
| Fix check | vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons) | PASS | code present: True; console errors across 11 weapon-matrix case(s): 0 |  |
| Fix check | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | PASS | code present: True; dual-slot override case: 6 PASS / 0 FAIL |  |
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 54 value(s) compared on 9 enemies |  |
| Fix check | No circling while unaware (patrol-anchor fix) on every enemy | PASS | 10 PASS / 0 FAIL of 10 enemies |  |
| Fix check | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | PASS | 10 PASS / 0 FAIL of 10 enemies |  |
| Fix check | Profile adjustments made by earlier audit passes | INFO | none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes |  |

## Per-enemy detail

### Axe_Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Axe_Droid.prefab` - definition `Axe_Droid` - melee `Spear of Fate` / ranged `-` - audited in 74s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Axe_Droid, melee=Spear of Fate (dmg 16), ranged=- (dmg -), preferRanged=False, def.attackDamage=12, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 42ms, 769 components, frame avg 37.9ms max 106.0ms (238 frames), GC collections 0, managed delta 11088KB |  |
| AI | No circling while unaware (player 50m away, 9s) | PASS | yaw turned 149deg, path 10.5m, net 5.4m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.7s (at 1.4m, weapon Spear of Fate), first player damage never, attacks 9, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee 'Spear of Fate' damages the player | FAIL | player hp lost 0.0 (item dmg 16), attacks 8, hitbox-active frames 115 (unarmed 0), hit events 14, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| Weapons | Melee 'Spear of Fate': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 0 (0 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 14, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.2m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 221.0 (-29.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 221.0 -> 191.6; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 2, body type Android |  |
| Death | Death, loot bag, cleanup | FAIL | dead yes in 0.25s, loot bag none, corpse gone not within window, dropped weapon Drawn_Spear_of_Fate;  | no loot bag and corpse not destroyed/disabled after 17s (destroyDelay 3s) |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 5 clip(s) observed playing; 21 distinct clips seen in total |  |
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
| stagger/hit | Hit Small Front * | 0.93 |
| stagger/hit | Hit Small Left | 0.93 |
| stagger/hit | Hit Small Right | 0.93 |

### corrupt_patrol_android

Prefab `Assets/_Project/Prefabs/Combat/Enemies/corrupt_patrol_android.prefab` - definition `corrupt_patrol_android` - melee `Death Axe` / ranged `sci_fi_pistol` - audited in 136s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=corrupt_patrol_android, melee=Death Axe (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=350 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 31ms, 727 components, frame avg 35.5ms max 79.6ms (254 frames), GC collections 0, managed delta 12072KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 151deg, path 10.7m, net 4.4m, states Wander |  |
| AI | Notice / chase / attack timing (player 5m in front) | WARN | notice 0.0s, chase 0.0s, first attack never (at -1.0m, weapon ), first player damage never, attacks 0, states Chase/Wander | noticed but never attacked within 22s (chase blocked, standoff/hold token, attack range, or cooldown) |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee 'Death Axe' damages the player | FAIL | player hp lost 0.0 (item dmg 18), attacks 0, hitbox-active frames 0 (unarmed 0), hit events 0, sub-1hp chip 0.0, first hit never, min distance 1.35m, target 'Player_v7 Combat Variant', states Chase/Wander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 | enemy never entered an attack in 14s with the player 1.5m away (AI states Chase/Wander, target 'Player_v7 Combat Variant'; min distance 1.4m) |
| Weapons | Melee 'Death Axe': intentional miss rate vs configured chance | INFO | swings 0, intentional misses rolled 0 (0 %) vs configured 10 % (+-300 % at 3 sigma), weapon hit events 0, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.4m, first attack at -1.0m | only 0 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 391, attacks 18, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | FAIL | shots/projectiles 12, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 38 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 1.3m | zero hits across 12 shots |
| Weapons | Ranged 'sci_fi_pistol' at 7.0 m | FAIL | player hp lost 0.0, shots/projectiles 11, attacks 15, attack-state frames 331, states Chase/Attack, engage 12.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 12.0 m | FAIL | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 134, states Attack/Wander, engage 12.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 18.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states Wander, engage 12.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 25.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states Wander, engage 12.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 11.4 m | FAIL | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states Wander, engage 12.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Death_Axe] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 350.0 -> 243.1 (-106.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 243.1 -> 138.7; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 3, body type Android |  |
| Death | Death, loot bag, cleanup | FAIL | dead yes in 0.28s, loot bag none, corpse gone not within window, dropped weapon Drawn_Sci_Fi_Pistol;  | no loot bag and corpse not destroyed/disabled after 17s (destroyDelay 3s) |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 0 clip(s) observed playing; 23 distinct clips seen in total |  |
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

### GreySpire Soldier

Prefab `Assets/_Project/Prefabs/Combat/Enemies/GreySpire Soldier.prefab` - definition `GreySpire Soldier` - melee `-` / ranged `survival_rifle` - audited in 136s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=GreySpire Soldier, melee=- (dmg -), ranged=survival_rifle (dmg 18), preferRanged=True, def.attackDamage=8, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 27ms, 771 components, frame avg 32.8ms max 61.9ms (275 frames), GC collections 0, managed delta 13580KB |  |
| AI | No circling while unaware (player 55m away, 9s) | PASS | yaw turned 94deg, path 7.2m, net 6.8m, states Wander |  |
| AI | Notice / chase / attack timing (player 8m in front) | PASS | notice 0.2s, chase 0.2s, first attack 3.0s (at 8.4m, weapon survival_rifle), first player damage never, attacks 12, states Wander/Defensive/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee weapon damages the player | SKIP | no resolved melee weapon |  |
| Weapons | Ranged 'survival_rifle' damages the player | FAIL | player hp lost 0.0 (item dmg 18), shots/projectiles 12, attack-state frames 477, attacks 12, first hit never, range 11m (engage 25), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=16, rangedCooldown=1.6, missRate=0.25 | 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) |
| Weapons | Ranged 'survival_rifle': hit rate vs configured accuracy | FAIL | shots/projectiles 12, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 38 %); item spread survival_rifle: projectileSpread 0.9 deg, accuracy 82, close-range scale 0.15 within 12m; the test player never blocks/dodges and stands still; closest approach 11.0m | zero hits across 12 shots |
| Weapons | Ranged 'survival_rifle' at 7.0 m | FAIL | player hp lost 0.0, shots/projectiles 9, attacks 9, attack-state frames 364, states Attack, engage 25.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'survival_rifle' at 12.0 m | FAIL | player hp lost 0.0, shots/projectiles 9, attacks 9, attack-state frames 356, states Attack, engage 25.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'survival_rifle' at 18.0 m | FAIL | player hp lost 0.0, shots/projectiles 9, attacks 9, attack-state frames 352, states Attack, engage 25.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'survival_rifle' at 25.0 m | FAIL | player hp lost 0.0, shots/projectiles 9, attacks 9, attack-state frames 335, states Attack, engage 25.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'survival_rifle' at 23.8 m | FAIL | player hp lost 0.0, shots/projectiles 9, attacks 9, attack-state frames 367, states Attack, engage 25.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged drawn state | INFO | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 219.6 (-30.4); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 219.6 -> 128.7; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 3, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 2 item entries, TryLootAll -> Granted, left 0, 2 granted item(s) removed again from the audit inventory; definition random loot count 2-2 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 0 clip(s) observed playing; 16 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 142s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 28ms, 761 components, frame avg 34.0ms max 66.8ms (265 frames), GC collections 0, managed delta 17692KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 223deg, path 12.3m, net 5.8m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.7s (at 1.7m, weapon weap2_sword), first player damage never, attacks 8, states Patrol/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee 'weap2_sword' damages the player | FAIL | player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 155 (unarmed 0), hit events 8, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=43 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 43 frames (definition missRate 0.25) |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 1 (13 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 8, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.5m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 484, attacks 19, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | FAIL | shots/projectiles 12, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 38 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 1.2m | zero hits across 12 shots |
| Weapons | Ranged 'sci_fi_pistol' at 7.0 m | FAIL | player hp lost 0.0, shots/projectiles 9, attacks 14, attack-state frames 309, states Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 12.0 m | FAIL | player hp lost 0.0, shots/projectiles 7, attacks 10, attack-state frames 315, states Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 18.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 103, states Attack/ReturnHome, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 25.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states ReturnHome, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 15.2 m | FAIL | player hp lost 0.0, shots/projectiles 0, attacks 3, attack-state frames 137, states Patrol/Hold/Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 171.1 (-28.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 171.1 -> 90.7; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 4, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 item entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 30 distinct clips seen in total |  |
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

### Humanoid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humanoid.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 134s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 37ms, 687 components, frame avg 33.6ms max 63.0ms (268 frames), GC collections 0, managed delta 12640KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 187deg, path 5.2m, net 3.9m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.6s (at 2.0m, weapon weap2_sword), first player damage never, attacks 10, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee 'weap2_sword' damages the player | FAIL | player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 125 (unarmed 0), hit events 10, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=82 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 82 frames (definition missRate 0.25) |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 2 (25 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 10, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 2.0m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 391, attacks 15, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | FAIL | shots/projectiles 12, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 38 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 1.2m | zero hits across 12 shots |
| Weapons | Ranged 'sci_fi_pistol' at 7.0 m | FAIL | player hp lost 0.0, shots/projectiles 10, attacks 14, attack-state frames 312, states Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 12.0 m | FAIL | player hp lost 0.0, shots/projectiles 9, attacks 11, attack-state frames 311, states Hold/Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 18.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 100, states Attack/ReturnHome, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 25.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states ReturnHome/Wander, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 15.2 m | FAIL | player hp lost 0.0, shots/projectiles 0, attacks 3, attack-state frames 143, states Wander/Hold/Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 278.2 (-21.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 278.2 -> 270.7; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 item entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 5 clip(s) observed playing; 35 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/HumanoidEnemy_Invector.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 144s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 42ms, 713 components, frame avg 34.0ms max 53.0ms (266 frames), GC collections 0, managed delta 14972KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 214deg, path 10.2m, net 4.7m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 2.4s (at 2.0m, weapon weap2_sword), first player damage never, attacks 10, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee 'weap2_sword' damages the player | FAIL | player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 112 (unarmed 0), hit events 12, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=38 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 38 frames (definition missRate 0.25) |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 1 (13 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 12, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.5m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 448, attacks 21, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack/Defensive; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | FAIL | shots/projectiles 12, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 38 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 0.9m | zero hits across 12 shots |
| Weapons | Ranged 'sci_fi_pistol' at 7.0 m | FAIL | player hp lost 0.0, shots/projectiles 9, attacks 13, attack-state frames 281, states Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 12.0 m | FAIL | player hp lost 0.0, shots/projectiles 6, attacks 11, attack-state frames 264, states Hold/Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 18.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 3, attack-state frames 107, states Chase/Hold/Attack, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 25.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 2, attack-state frames 76, states Chase/Hold/Attack, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 15.2 m | FAIL | player hp lost 0.0, shots/projectiles 0, attacks 3, attack-state frames 144, states Hold/Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | FAIL | receiver hit did 0; EnemyHealth.TakeDamage direct: 87.1 -> 82.1; 1 receiver(s) | damage receiver chain returns 0 (PioneerInvectorDamageResolver resolves the sender's weapon damage to 0 - player has no damaging weapon equipped, or hit dedupe rejected) though EnemyHealth itself works |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 82.1 -> -46.4; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 4 -> 6, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 0 item entries + AC only, TryLootAll -> None, left 0; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 5 clip(s) observed playing; 35 distinct clips seen in total |  |
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

### Hybrid Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Hybrid Droid.prefab` - definition `FRED_Android` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 140s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=FRED_Android, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 39ms, 687 components, frame avg 37.5ms max 64.2ms (240 frames), GC collections 0, managed delta 16412KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 190deg, path 9.6m, net 4.3m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.9s (at 1.7m, weapon weap2_sword), first player damage never, attacks 9, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee 'weap2_sword' damages the player | FAIL | player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 125 (unarmed 0), hit events 12, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=52 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 52 frames (definition missRate 0.25) |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 1 (13 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 12, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.5m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 416, attacks 17, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | FAIL | shots/projectiles 12, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 38 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 1.2m | zero hits across 12 shots |
| Weapons | Ranged 'sci_fi_pistol' at 7.0 m | FAIL | player hp lost 0.0, shots/projectiles 6, attacks 13, attack-state frames 291, states Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 12.0 m | FAIL | player hp lost 0.0, shots/projectiles 9, attacks 11, attack-state frames 284, states Hold/Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 18.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 78, states Attack/ReturnHome, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 25.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states ReturnHome, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 15.2 m | FAIL | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states Wander/Hold, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 270.4 (-29.6); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 270.4 -> 245.6; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 2, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 0 item entries + AC only, TryLootAll -> None, left 0; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.29s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 5 clip(s) observed playing; 34 distinct clips seen in total |  |
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

### Robot

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `Robot_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 150s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Robot_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=15, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 37ms, 698 components, frame avg 42.3ms max 81.1ms (213 frames), GC collections 0, managed delta 7972KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 190deg, path 9.6m, net 7.6m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.0s, chase 0.0s, first attack 2.4s (at 1.7m, weapon weap2_sword), first player damage never, attacks 9, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee 'weap2_sword' damages the player | FAIL | player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 114 (unarmed 0), hit events 11, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 0 (0 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 11, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.7m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 465, attacks 14, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | FAIL | shots/projectiles 12, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 38 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 1.5m | zero hits across 12 shots |
| Weapons | Ranged 'sci_fi_pistol' at 7.0 m | FAIL | player hp lost 0.0, shots/projectiles 4, attacks 9, attack-state frames 347, states Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 12.0 m | FAIL | player hp lost 0.0, shots/projectiles 4, attacks 8, attack-state frames 322, states Hold/Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 18.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 116, states Attack/ReturnHome, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 25.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states ReturnHome, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 15.2 m | FAIL | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states ReturnHome/Wander, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 474.2 (-25.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 474.2 -> 367.1; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 4, body type Robot |  |
| Death | Death, loot bag, cleanup | FAIL | dead yes in 0.28s, loot bag none, corpse gone not within window, dropped weapon Drawn_Sci_Fi_Pistol;  | no loot bag and corpse not destroyed/disabled after 17s (destroyDelay 3s) |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 5 clip(s) observed playing; 33 distinct clips seen in total |  |
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

### SwordMaster

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `Humanoid_Enemy` - melee `Blade Of Chaos` / ranged `sci_fi_pistol` - audited in 155s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=Blade Of Chaos (dmg 21), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 40ms, 693 components, frame avg 32.1ms max 59.9ms (281 frames), GC collections 0, managed delta 17204KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 176deg, path 13.5m, net 5.9m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.8s (at 1.6m, weapon Blade Of Chaos), first player damage never, attacks 9, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | FAIL | player hp lost 0.0 (item dmg 21), attacks 8, hitbox-active frames 96 (unarmed 0), hit events 6, sub-1hp chip 0.0, first hit never, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| Weapons | Melee 'Blade Of Chaos': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 0 (0 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 6, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 634, attacks 22, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.25, intentional-miss frames 0) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | FAIL | shots/projectiles 12, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 38 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 1.2m | zero hits across 12 shots |
| Weapons | Ranged 'sci_fi_pistol' at 7.0 m | FAIL | player hp lost 0.0, shots/projectiles 11, attacks 13, attack-state frames 384, states Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 12.0 m | FAIL | player hp lost 0.0, shots/projectiles 7, attacks 11, attack-state frames 360, states Hold/Chase/Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 18.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 116, states Attack/ReturnHome, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 25.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states ReturnHome, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 15.2 m | FAIL | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states ReturnHome/Wander/Hold, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Blade_Of_Chaos] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 297.4 (-2.6); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 297.4 -> 292.5; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | WARN | marks stamped 0 -> 0, body type Humanoid | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) |
| Death | Death, loot bag, cleanup | FAIL | dead yes in 0.25s, loot bag none, corpse gone not within window, dropped weapon Drawn_Sci_Fi_Pistol;  | no loot bag and corpse not destroyed/disabled after 17s (destroyDelay 3s) |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 5 clip(s) observed playing; 35 distinct clips seen in total |  |
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

### The_Evil_One

Prefab `Assets/_Project/Prefabs/Combat/Enemies/The_Evil_One.prefab` - definition `The_Evil_One` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 141s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=The_Evil_One, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=True, def.attackDamage=12, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 34ms, 707 components, frame avg 33.4ms max 63.2ms (270 frames), GC collections 0, managed delta 9176KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 157deg, path 12.7m, net 5.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.1s (at 9.5m, weapon sci_fi_pistol), first player damage never, attacks 20, states Wander/Defensive/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee 'weap2_sword' damages the player | FAIL | player hp lost 0.0 (item dmg 18), attacks 8, hitbox-active frames 149 (unarmed 0), hit events 9, sub-1hp chip 0.0, first hit never, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; no vMeleeWeapon on the drawn/bound weapon | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=0 frames, ItemData roll) or the player blocked/ignored it |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 8, intentional misses rolled 0 (0 %) vs configured 10 % (+-32 % at 3 sigma), weapon hit events 9, player hp lost 0.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 2.0m | only 8 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 12, attack-state frames 436, attacks 17, first hit never, range 10m (engage 16), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.65 | 12 projectile(s) spawned but none reduced player hp: projectile collision mask/Player layer, CombatHitResolver target filter, or damage scaled to 0 (missRate 0.65, intentional-miss frames 0) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | FAIL | shots/projectiles 12, damage events 0 (0 %) vs about 35 % expected from definition missRate 0.65 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 41 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 9.6m | zero hits across 12 shots |
| Weapons | Ranged 'sci_fi_pistol' at 7.0 m | FAIL | player hp lost 0.0, shots/projectiles 12, attacks 14, attack-state frames 350, states Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 12.0 m | FAIL | player hp lost 0.0, shots/projectiles 12, attacks 14, attack-state frames 297, states Attack, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged 'sci_fi_pistol' at 18.0 m | INFO | player hp lost 0.0, shots/projectiles 2, attacks 0, attack-state frames 88, states Attack/ReturnHome/Wander, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 25.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states Wander, engage 16.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 15.2 m | FAIL | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states Wander, engage 16.0 | inside engage range but no damage (see [DMRangedShot] log) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 478.1 (-21.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 478.1 -> 380.3; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 4, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 item entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-2 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.4s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 5 clip(s) observed playing; 27 distinct clips seen in total |  |
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
