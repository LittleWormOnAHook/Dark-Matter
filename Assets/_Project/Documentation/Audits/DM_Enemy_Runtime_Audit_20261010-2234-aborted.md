# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-10 22:20:57 -> 2026-10-10 22:34:04 (aborted: left play mode)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-780.0, 21.2, -122.1), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 4; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 9
- Unit store: 36 stored unit(s); stamps audit D462F7 game A1BCD9 gear 204DA0; skip-unchanged is ON (Quick mode)
- Weapon matrix: 11 weapon items equipped one at a time (spawn override) on HumanoidEnemy_Invector; plus one dual-slot override case
- Player level raised in memory from 5 for equip gates (restored at the end; nothing saved)
- Mode quick: 32 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was False). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- SETTINGS DRIFT during the run (changed by something other than the audit, e.g. game code or another tool): start [runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1] -> end [runInBackground=False; vSyncCount=1; lodBias=1; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1]
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **quick**, wall time 13m07s

**9 enemies audited - 3 FAIL, 10 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 12 weapon cases + 13 scenarios): 251 PASS, 35 FAIL, 38 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 177 change(s).

- NEW enemy|Axe_Droid|perf = PASS
- NEW enemy|Axe_Droid|melee.miss = INFO
- CHANGED enemy|Axe_Droid|player.swing: PASS -> WARN
- NEW enemy|Axe_Droid|loot = FAIL
- NEW enemy|corrupt_patrol_android|perf = PASS
- NEW enemy|corrupt_patrol_android|melee.miss = INFO
- NEW enemy|corrupt_patrol_android|ranged.miss = INFO
- CHANGED enemy|corrupt_patrol_android|player.swing: PASS -> WARN
- NEW enemy|corrupt_patrol_android|loot = PASS
- NEW enemy|GreySpire Soldier|perf = PASS
- CHANGED enemy|GreySpire Soldier|notice: PASS -> WARN
- NEW enemy|GreySpire Soldier|melee.miss = INFO
- CHANGED enemy|GreySpire Soldier|player.swing: PASS -> WARN
- NEW enemy|GreySpire Soldier|loot = PASS
- NEW enemy|Humadroid|perf = PASS
- NEW enemy|Humadroid|melee.miss = INFO
- NEW enemy|Humadroid|ranged.miss = INFO
- CHANGED enemy|Humadroid|player.swing: PASS -> WARN
- CHANGED enemy|Humadroid|hitmarks: WARN -> PASS
- NEW enemy|Humadroid|loot = PASS
- NEW enemy|Humanoid|perf = PASS
- NEW enemy|Humanoid|melee.miss = INFO
- NEW enemy|Humanoid|ranged.miss = INFO
- CHANGED enemy|Humanoid|player.swing: PASS -> WARN
- NEW enemy|Humanoid|loot = FAIL
- NEW enemy|HumanoidEnemy_Invector|perf = PASS
- NEW enemy|HumanoidEnemy_Invector|melee.miss = INFO
- NEW enemy|HumanoidEnemy_Invector|ranged.miss = INFO
- CHANGED enemy|HumanoidEnemy_Invector|player.swing: PASS -> WARN
- NEW enemy|HumanoidEnemy_Invector|loot = PASS
- NEW enemy|Hybrid Droid|perf = PASS
- NEW enemy|Hybrid Droid|melee.miss = INFO
- NEW enemy|Hybrid Droid|ranged.miss = INFO
- CHANGED enemy|Hybrid Droid|player.swing: PASS -> WARN
- NEW enemy|Hybrid Droid|loot = FAIL
- NEW enemy|Robot|perf = PASS
- NEW enemy|Robot|melee.miss = INFO
- NEW enemy|Robot|ranged.miss = INFO
- CHANGED enemy|Robot|player.swing: PASS -> WARN
- NEW enemy|Robot|loot = PASS
- NEW enemy|The_Evil_One|perf = PASS
- NEW enemy|The_Evil_One|melee.miss = INFO
- NEW enemy|The_Evil_One|ranged.miss = INFO
- CHANGED enemy|The_Evil_One|player.swing: PASS -> WARN
- NEW enemy|The_Evil_One|loot = PASS
- CHANGED weapon|2 Hander|audio.swing.2 Hander: PASS -> FAIL
- NEW weapon|2 Hander|w.miss = INFO
- CHANGED weapon|Blade Of Chaos|audio.swing.Blade Of Chaos: FAIL -> PASS
- NEW weapon|Blade Of Chaos|w.miss = INFO
- CHANGED weapon|Death Axe|audio.swing.Death Axe: FAIL -> PASS
- CHANGED weapon|Death Axe|audio.hitplayer.Death Axe: FAIL -> PASS
- CHANGED weapon|Death Axe|w.damage: FAIL -> PASS
- NEW weapon|Death Axe|w.miss = INFO
- NEW weapon|Spear of Fate|w.miss = INFO
- CHANGED weapon|Sword of Fear|audio.swing.Sword of Fear: PASS -> FAIL
- CHANGED weapon|Sword of Fear|audio.hitplayer.Sword of Fear: FAIL -> PASS
- NEW weapon|Sword of Fear|w.miss = INFO
- CHANGED weapon|weap2_sword|audio.swing.weap2_sword: PASS -> FAIL
- NEW weapon|weap2_sword|w.miss = INFO
- NEW weapon|weap_two_handed|w.miss = INFO
- CHANGED weapon|Wood Axe|audio.swing.Wood Axe: PASS -> FAIL
- NEW weapon|Wood Axe|w.miss = INFO
- NEW weapon|DM_Mining_Tool|w.miss = INFO
- CHANGED weapon|DM_Mining_Tool|vis: PASS -> FAIL
- NEW weapon|sci_fi_pistol|w.miss = INFO
- NEW weapon|survival_rifle|w.miss = INFO
- CHANGED scenario|Audio: swing / hit sound wiring (static)|audio.swing.callers: FAIL -> PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.inputdiag = INFO
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.W: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.S: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.A: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.D: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.run: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.sprint: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.jump: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.dodge: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.look.x: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.lmb: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.rmb: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.switch: PASS -> WARN
- ... 97 more

## Timing

Wall time 788s (13.1 min), mode quick. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Axe_Droid | enemy | 24.7 | ran |
| corrupt_patrol_android | enemy | 27.2 | ran |
| GreySpire Soldier | enemy | 43.1 | ran |
| Humadroid | enemy | 30.1 | ran |
| Humanoid | enemy | 32.3 | ran |
| HumanoidEnemy_Invector | enemy | 29.1 | ran |
| Hybrid Droid | enemy | 29.3 | ran |
| Robot | enemy | 30.1 | ran |
| The_Evil_One | enemy | 29.0 | ran |
| 2 Hander | weapon | 2.9 | ran |
| Blade Of Chaos | weapon | 2.0 | ran |
| Death Axe | weapon | 2.0 | ran |
| Spear of Fate | weapon | 2.6 | ran |
| Sword of Fear | weapon | 2.0 | ran |
| weap2_sword | weapon | 2.1 | ran |
| weap_two_handed | weapon | 2.6 | ran |
| Wood Axe | weapon | 2.0 | ran |
| DM_Mining_Tool | weapon | 2.3 | ran |
| sci_fi_pistol | weapon | 2.9 | ran |
| survival_rifle | weapon | 2.8 | ran |
| OVERRIDE both slots | weapon | 4.1 | ran |
| Camera: faces forward after teleports, zoom in/out | scenario | 8.9 | ran |
| Equip: weapon survives a teleport | scenario | 6.1 | ran |
| Audio: swing / hit sound wiring (static) | scenario | 0.4 | ran |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | scenario | 59.5 | ran |
| Block + parry: player defends against enemy melee | scenario | 42.7 | ran |
| Block: enemy defends against player swings | scenario | 9.6 | ran |
| Movement: retreat, push, flank, backstab, lose target | scenario | 31.0 | ran |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | scenario | 53.4 | ran |
| Multi-enemy: FRED x3 and x5 | scenario | 36.3 | ran |
| Brain: orbit, toggles, archetypes | scenario | 128.0 | ran |
| Player weapons: each melee item vs enemy (swapped in place) | scenario | 28.0 | ran |
| Ammo: default ammo per ranged weapon + ammo-specific FX | scenario | 0.0 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| scenario|Brain: orbit, toggles, archetypes | setup | 128.3 | 3183 | 40.3 | 299.0 | 0.67 | 0.00 | 18.55 |
| scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | setup | 59.7 | 1947 | 30.6 | 126.2 | 0.41 | 0.00 | 14.41 |
| scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | setup | 53.6 | 1691 | 31.7 | 139.3 | 0.42 | 0.00 | 15.26 |
| scenario|Block + parry: player defends against enemy melee | setup | 43.8 | 1246 | 35.2 | 291.6 | 0.60 | 0.00 | 16.06 |
| scenario|Multi-enemy: FRED x3 and x5 | setup | 37.8 | 839 | 45.1 | 444.1 | 0.81 | 0.00 | 21.65 |
| scenario|Movement: retreat, push, flank, backstab, lose target | setup | 31.2 | 859 | 36.3 | 369.2 | 0.49 | 0.00 | 16.43 |
| scenario|Player weapons: each melee item vs enemy (swapped in place) | setup | 28.2 | 807 | 35.0 | 140.0 | 0.51 | 0.00 | 16.38 |
| enemy|GreySpire Soldier | notice/chase/attack | 22.0 | 699 | 31.5 | 54.7 | 0.43 | 0.00 | 15.34 |
| scenario|Block: enemy defends against player swings | setup | 10.8 | 319 | 33.8 | 94.1 | 0.46 | 0.00 | 16.16 |
| scenario|Camera: faces forward after teleports, zoom in/out | setup | 9.2 | 285 | 32.2 | 64.9 | 0.45 | 0.00 | 14.75 |
| enemy|The_Evil_One | unaware 9s (+perf) | 9.0 | 288 | 31.3 | 62.0 | 0.42 | 0.00 | 14.97 |
| enemy|Hybrid Droid | unaware 9s (+perf) | 9.0 | 283 | 31.9 | 55.9 | 0.45 | 0.00 | 15.17 |
| enemy|Robot | unaware 9s (+perf) | 9.0 | 289 | 31.2 | 57.1 | 0.41 | 0.00 | 14.75 |
| enemy|Axe_Droid | unaware 9s (+perf) | 9.0 | 288 | 31.3 | 53.0 | 0.45 | 0.00 | 14.72 |
| enemy|GreySpire Soldier | unaware 9s (+perf) | 9.0 | 277 | 32.6 | 70.3 | 0.44 | 0.00 | 15.76 |
| enemy|Humadroid | unaware 9s (+perf) | 9.0 | 284 | 31.8 | 54.9 | 0.44 | 0.00 | 15.20 |
| enemy|HumanoidEnemy_Invector | unaware 9s (+perf) | 9.0 | 283 | 31.8 | 52.6 | 0.44 | 0.00 | 15.02 |
| enemy|Humanoid | unaware 9s (+perf) | 9.0 | 287 | 31.4 | 51.1 | 0.41 | 0.00 | 15.03 |
| enemy|corrupt_patrol_android | unaware 9s (+perf) | 9.0 | 293 | 30.7 | 55.1 | 0.44 | 0.00 | 14.48 |
| enemy|Axe_Droid | death/loot | 6.7 | 205 | 32.9 | 111.1 | 0.49 | 0.00 | 14.44 |
| enemy|Hybrid Droid | death/loot | 6.7 | 204 | 32.9 | 73.8 | 0.43 | 0.00 | 15.67 |
| enemy|GreySpire Soldier | death/loot | 6.7 | 203 | 33.0 | 79.8 | 0.51 | 0.00 | 14.93 |
| enemy|Humadroid | death/loot | 6.6 | 211 | 31.3 | 77.6 | 0.47 | 0.00 | 14.46 |
| enemy|Humanoid | death/loot | 6.5 | 201 | 32.3 | 76.4 | 0.42 | 0.00 | 15.04 |
| enemy|The_Evil_One | death/loot | 6.5 | 192 | 33.7 | 82.3 | 0.44 | 0.00 | 15.78 |
| enemy|HumanoidEnemy_Invector | death/loot | 6.5 | 193 | 33.6 | 79.6 | 0.44 | 0.00 | 15.72 |
| enemy|Robot | death/loot | 6.5 | 205 | 31.7 | 77.9 | 0.42 | 0.00 | 14.65 |
| enemy|corrupt_patrol_android | death/loot | 6.5 | 208 | 31.1 | 77.5 | 0.47 | 0.00 | 14.35 |
| scenario|Equip: weapon survives a teleport | setup | 6.3 | 186 | 33.7 | 171.6 | 0.44 | 0.00 | 15.06 |
| enemy|Humanoid | notice/chase/attack | 5.9 | 180 | 32.9 | 63.7 | 0.48 | 0.00 | 15.43 |

Whole run (ran units only): 712s over 20234 frames, avg frame 35.4 ms, physics 0.52 ms, render 0.00 ms, scripts 16.24 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 100 | FAIL | Player uses melee '2 Hander' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 2 | 100 | FAIL | Player uses melee 'Blade Of Chaos' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 3 | 100 | FAIL | Player uses melee 'Death Axe' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 4 | 100 | FAIL | Player uses melee 'Spear of Fate' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 5 | 100 | FAIL | Player uses melee 'Sword of Fear' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 6 | 100 | FAIL | Player uses melee 'weap2_sword' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 7 | 100 | FAIL | Player uses melee 'weap_two_handed' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 8 | 100 | FAIL | Player uses melee 'Wood Axe' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 9 | 100 | FAIL | Player uses ranged 'sci_fi_pistol' on the enemy (default ammo) | Ammo: default ammo per ranged weapon + ammo-specific FX | see the ammo damage row | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 10 | 100 | FAIL | Player uses ranged 'survival_rifle' on the enemy (default ammo) | Ammo: default ammo per ranged weapon + ammo-specific FX | see the ammo damage row | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 11 | 85 | FAIL | Exactly one weapon drawn | DM_Mining_Tool | second weapon visual left active, or the equipped weapon has no drawn visual | Let EnemyInvectorLoadoutBridge be the only weapon authority; hide every Drawn_/Holstered_ slot except the resolved ones. |
| 12 | 75 | WARN | Ranged 'DM_Mining_Tool' damages the player | DM_Mining_Tool | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 13 | 70 | FAIL | Holding block negates enemy melee damage | Block + parry: player defends against enemy melee | player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 14 | 60 | WARN | Notice / chase / attack timing (player 8m in front) | GreySpire Soldier | noticed but never attacked within 22s (chase blocked, standoff/hold token, attack range, or cooldown) | Review senses (vision range/FOV/LOS) and aggro rules; check the EnemySenses reference on the prefab. |
| 15 | 45 | WARN | Enemy guard-break stagger after a blocked hit | Block + parry: player defends against enemy melee | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 16 | 45 | WARN | Enemy block reduces player melee damage | Block: enemy defends against player swings | player swings registered no hits on the open enemy, so the comparison is inconclusive (input focus, reach, or the player weapon) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 17 | 30 | WARN | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | Multi-enemy: FRED x3 and x5 | 108/390 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Add enemy separation and make the attack token rotate between enemies. |
| 18 | 20 | FAIL | Loot dropped and can be looted (real loot path), then cleaned up at once | Axe_Droid, Humanoid, Hybrid Droid | bag spawned but nothing could be looted (empty entries / TryLootAll refused) | Inspect the measured value and cause on the detail row. |
| 19 | 20 | FAIL | '2 Hander' swing plays a swish | 2 Hander | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 20 | 20 | FAIL | 'Sword of Fear' swing plays a swish | Sword of Fear | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 21 | 20 | FAIL | 'weap2_sword' swing plays a swish | weap2_sword | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 22 | 20 | FAIL | 'Wood Axe' swing plays a swish | Wood Axe | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 23 | 20 | FAIL | W moves the player forward (camera direction) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move action not reaching the controller: PlayerInput disabled, move locked, or PioneerPlayerInputBinder.HandleAction('Move') not wired | Inspect the measured value and cause on the detail row. |
| 24 | 20 | FAIL | S moves the player back | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (S) produced no backward displacement | Inspect the measured value and cause on the detail row. |
| 25 | 20 | FAIL | A strafes left | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (A) produced no leftward displacement | Inspect the measured value and cause on the detail row. |
| 26 | 20 | FAIL | D strafes right | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (D) produced no rightward displacement | Inspect the measured value and cause on the detail row. |
| 27 | 20 | FAIL | W run speed (reference for sprint) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | player did not move | Inspect the measured value and cause on the detail row. |
| 28 | 20 | FAIL | Shift + W sprints faster than W | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | sprint no faster than run (0.0m): Sprint action not wired, stamina 0, or sprint speed equals run speed | Inspect the measured value and cause on the detail row. |
| 29 | 20 | FAIL | Space jumps | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked; state before the press: isGrounded True, isJumping False, customAction False, isCrouching False, stamina 1000 | Inspect the measured value and cause on the detail row. |
| 30 | 20 | FAIL | Q dodge / roll | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dodge action did not move the player: TryDodgeRollFromInput refused (stamina, cooldown, no roll clip) | Inspect the measured value and cause on the detail row. |
| 31 | 20 | FAIL | Mouse look left/right orbits the camera | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Look action (Pointer/delta) not reaching vThirdPersonCamera: PlayerController.OnLook, cursor lock state, or camera input muted | Inspect the measured value and cause on the detail row. |
| 32 | 20 | FAIL | Left click swings the drawn melee weapon | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | melee Attack input not received: drawn melee tap/hold logic (UpdateDrawnMeleeCharge) did not start a swing | Inspect the measured value and cause on the detail row. |
| 33 | 20 | FAIL | Right click blocks (melee drawn) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Block input (Mouse rightButton / Block action) did not set isBlocking: stamina 0, customAction, not grounded, or still attacking | Inspect the measured value and cause on the detail row. |
| 34 | 20 | WARN | Player circles behind the enemy: enemy turns to face | Movement: retreat, push, flank, backstab, lose target | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 35 | 20 | WARN | Hit from behind vs from the front (player melee, AI off) | Movement: retreat, push, flank, backstab, lose target | swings did not land from one side (reach/facing/input), comparison inconclusive | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 36 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Standard: fires (rounds consumed) | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 37 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Standard: fires (rounds consumed) | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 38 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 39 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 40 | 20 | FAIL | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks | swing rows still silent for: '2 Hander' swing plays a swish, 'Sword of Fear' swing plays a swish, 'weap2_sword' swing plays a swish, 'Wood Axe' swing plays a swish | Inspect the measured value and cause on the detail row. |
| 41 | 15 | WARN | A parry happens when block is pressed inside the window | Block + parry: player defends against enemy melee | none of the sampled press times produced a parry: sweep resolution (0.15s steps at editor frame rate) may have missed the 0.2s window, or the stagger length did not match the parry profile | Inspect the measured value and cause on the detail row. |
| 42 | -5 | WARN | Player melee swing via injected Mouse input (best effort) | Axe_Droid, corrupt_patrol_android, GreySpire Soldier, Humadroid +5 more | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes | Inspect the measured value and cause on the detail row. |
| 43 | -5 | WARN | Camera zooms in and out | Camera: faces forward after teleports, zoom in/out | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state | Inspect the measured value and cause on the detail row. |
| 44 | -5 | WARN | Melee hit sound has a caller on the damage path | Audio: swing / hit sound wiring (static) | the only callers are the enemy-body receiver (PioneerInvectorDamageReceiver), MeleeCombatController and CombatHitResolver: damage TO THE PLAYER (PioneerInvectorDamageBridge / the player's vDamageReceiver) has no hit-sound call, so enemy weapons hitting the player are silent | Inspect the measured value and cause on the detail row. |
| 45 | -5 | WARN | Left Ctrl crouches | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) | Inspect the measured value and cause on the detail row. |
| 46 | -5 | WARN | Left Alt dash | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dash action not wired or on cooldown (DMDashController) | Inspect the measured value and cause on the detail row. |
| 47 | -5 | WARN | Hold Space after jumping boosts (jetpack) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | no sustained lift: jetpack not unlocked/fuelled, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player | Inspect the measured value and cause on the detail row. |
| 48 | -5 | WARN | Tab (SwitchWeapon) cycles the weapon focus | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Tab tap did not change focus: only one weapon on the hotbar, Hot Cross not polling, or tap/hold threshold (hold arms) not met | Inspect the measured value and cause on the detail row. |
| 49 | -5 | WARN | Half-tilt stick walks slower than a run | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | walk speed not distinct from run (0.0m): analog Move magnitude ignored by the locomotion speed | Inspect the measured value and cause on the detail row. |
| 50 | -5 | WARN | Approach with W: enemy notices | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) | Inspect the measured value and cause on the detail row. |
| 51 | -5 | WARN | Run away with S: enemy pursues | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy did not chase a retreating player: chase blocked (standoff/hold token, ranged keep-distance) or target lost immediately | Inspect the measured value and cause on the detail row. |
| 52 | -5 | WARN | Retreat then re-engage: enemy fights back | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy never attacked a player standing next to it: attack cooldown/token starvation or stagger lock | Inspect the measured value and cause on the detail row. |
| 53 | -5 | WARN | Jump away: player clears distance | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | jump did not leave the ground (Jump input / grounded) | Inspect the measured value and cause on the detail row. |
| 54 | -5 | WARN | Jetpack escape (hold Space + S) | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump | Inspect the measured value and cause on the detail row. |
| 55 | -5 | WARN | 5 FREDs at once for 18s: token rotation, spacing, no stacking/orbiting | Multi-enemy: FRED x3 and x5 | 183/353 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |
| 56 | -5 | WARN | 3 FREDs, baseline (brain on, director on) | Brain: orbit, toggles, archetypes | 105/294 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |
| 57 | -5 | WARN | 3 FREDs, legacyRingSlotRotation=true | Brain: orbit, toggles, archetypes | 95/282 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |
| 58 | -5 | WARN | enableUtilityBrain=false stops the brain acting (no Press/Defend/Retreat/Hold) | Brain: orbit, toggles, archetypes | the brain still selected actions with enableUtilityBrain=false (a per-type ProfileOverride, or a cached decision) | Inspect the measured value and cause on the detail row. |
| 59 | -5 | WARN | legacyRingSlotRotation=true restores the old ring-slot rotation (more orbiting than the director) | Brain: orbit, toggles, archetypes | legacyRingSlotRotation made no measurable difference in 12s (the director may still own spacing, or the toggle is read only at spawn) | Inspect the measured value and cause on the detail row. |
| 60 | -5 | WARN | sci_fi_pistol + DMAmmoFxProfile_Standard: damages the enemy | Ammo: default ammo per ranged weapon + ammo-specific FX | no shot fired, see fire row | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Axe_Droid | Spear of Fate | - | PASS | PASS | PASS | PASS | PASS | n/a | PASS | PASS | PASS | PASS |
| corrupt_patrol_android | Death Axe | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| GreySpire Soldier | weap_two_handed | - | PASS | PASS | PASS | warn | PASS | n/a | PASS | PASS | PASS | PASS |
| Humadroid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Humanoid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| HumanoidEnemy_Invector | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Hybrid Droid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Robot | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| The_Evil_One | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |

## Weapons that deal NO damage (headline)

- none detected in this run.

## Weapon matrix (every weapon item on the template, spawn override)

12 cases, 5 FAIL. Each weapon is equipped on `HumanoidEnemy_Invector` through ConfigureSpawnLoadout (the spawner path) and made to attack the player.

| Weapon | Kind | Equips | Damages player | One drawn | Measured | Probable cause |
|---|---|---|---|---|---|---|
| 2 Hander | melee | PASS | PASS | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 16, hit events 1, sub-1hp chip 0.4, first hit 0.3s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Blade Of Chaos | melee | PASS | PASS | PASS | player hp lost 30.0 (item dmg 21), attacks 1, hitbox-active frames 12, hit events 1, sub-1hp chip 0.4, first hit 0.3s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hi... |  |
| Death Axe | melee | PASS | PASS | PASS | player hp lost 48.6 (item dmg 18), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.0, first hit 0.4s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: laye... |  |
| Spear of Fate | melee | PASS | PASS | PASS | player hp lost 36.0 (item dmg 16), attacks 1, hitbox-active frames 11, hit events 1, sub-1hp chip 0.5, first hit 0.9s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitB... |  |
| Sword of Fear | melee | PASS | PASS | PASS | player hp lost 38.6 (item dmg 15), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.0, first hit 0.3s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitB... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| weap2_sword | melee | PASS | PASS | PASS | player hp lost 52.0 (item dmg 18), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.8, first hit 0.3s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| weap_two_handed | melee | PASS | PASS | PASS | player hp lost 68.0 (item dmg 28), attacks 1, hitbox-active frames 9, hit events 1, sub-1hp chip 0.4, first hit 1.0s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; ... |  |
| Wood Axe | melee | PASS | PASS | PASS | player hp lost 62.5 (item dmg 26), attacks 1, hitbox-active frames 16, hit events 2, sub-1hp chip 0.0, first hit 0.3s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| DM_Mining_Tool | ranged | PASS | warn | **FAIL** | player hp lost 16.1 (item dmg 0), shots/projectiles 1, attack-state frames 31, attacks 1, first hit 1.3s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCoo... | second weapon visual left active, or the equipped weapon has no drawn visual |
| sci_fi_pistol | ranged | PASS | PASS | PASS | player hp lost 16.9 (item dmg 14), shots/projectiles 2, attack-state frames 40, attacks 2, first hit 1.8s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRang... |  |
| survival_rifle | ranged | PASS | PASS | PASS | player hp lost 16.9 (item dmg 18), shots/projectiles 2, attack-state frames 39, attacks 2, first hit 1.6s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRa... |  |
| OVERRIDE both slots | dual-slot spawn override | - | - | PASS | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)): PASS ; Starts with the ranged override drawn (prefer-ranged on, start-melee off): PASS ; Switch to the melee override vi... |  |

### Weapon: 2 Hander

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item 2 Hander; drawn [Drawn_2_Hander]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | '2 Hander' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | '2 Hander' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_8:Melee Weapon Attack 1, SfxPool_9:QP01 0005 Wind howling; vHitEffects on 'Drawn_2_Hander': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee '2 Hander' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 16, hit events 1, sub-1hp chip 0.4, first hit 0.3s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee '2 Hander': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 38.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.3m, first attack at 1.3m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.3m), first damage 0.3s, attacks 1, attack-state frames 20, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Blade Of Chaos

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 21 |  |
| Audio | 'Blade Of Chaos' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_5:QP01 0005 Wind howling |  |
| Audio | 'Blade Of Chaos' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_6:Melee Weapon Attack 1; vHitEffects on 'Drawn_Blade_Of_Chaos': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 30.0 (item dmg 21), attacks 1, hitbox-active frames 12, hit events 1, sub-1hp chip 0.4, first hit 0.3s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Blade Of Chaos': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 30.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 22, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Death Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'Death Axe' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_2:Melee Weapon Air Swing 2 |  |
| Audio | 'Death Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_3:Melee Weapon Attack 3, SfxPool_4:Melee Weapon Air Swing 1, SfxPool_5:Melee Weapon Attack 3; vHitEffects on 'Drawn_Death_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 48.6 (item dmg 18), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.0, first hit 0.4s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Death Axe': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 2, player hp lost 48.6; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.4s, attacks 1, attack-state frames 18, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Spear of Fate

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 16 |  |
| Audio | 'Spear of Fate' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_2:QP01 0005 Wind howling, SfxPool_3:Melee Weapon Air Swing 3 |  |
| Audio | 'Spear of Fate' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_3:Melee Weapon Air Swing 3, SfxPool_4:Melee Weapon Attack 4; vHitEffects on 'Drawn_Spear_of_Fate': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 36.0 (item dmg 16), attacks 1, hitbox-active frames 11, hit events 1, sub-1hp chip 0.5, first hit 0.9s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Spear of Fate': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 36.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 0.9s, attacks 1, attack-state frames 35, clips seen: WeakAttack_SwordB, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Sword of Fear

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Sword of Fear; drawn [Drawn_Sword_of_Fear]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 15 |  |
| Audio | 'Sword of Fear' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Sword of Fear' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_2:Melee Weapon Attack 1, SfxPool_3:Melee Weapon Air Swing 1, SfxPool_4:Melee Weapon Attack 4; vHitEffects on 'Drawn_Sword_of_Fear': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Sword of Fear' damages the player | PASS | player hp lost 38.6 (item dmg 15), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.0, first hit 0.3s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Sword of Fear': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 2, player hp lost 38.6; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 17, clips seen: Attack2Hand_C, WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap2_sword

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'weap2_sword' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'weap2_sword' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_1:Melee Weapon Attack 4, SfxPool_2:QP01 0005 Wind howling, SfxPool_4:Melee Weapon Attack 4, SfxPool_3:Melee Weapon Air Swing 1; vHitEffects on 'Drawn_weap2_sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 52.0 (item dmg 18), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.8, first hit 0.3s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 2, player hp lost 52.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 17, clips seen: WeakAttack_SwordB, WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap_two_handed

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | 'weap_two_handed' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_0:Melee Weapon Air Swing 3 |  |
| Audio | 'weap_two_handed' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_0:Melee Weapon Air Swing 3, SfxPool_1:Melee Weapon Attack 4, SfxPool_2:QP01 0005 Wind howling; vHitEffects on 'Drawn_Two_Handed_Sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 68.0 (item dmg 28), attacks 1, hitbox-active frames 9, hit events 1, sub-1hp chip 0.4, first hit 1.0s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap_two_handed': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 68.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 1.0s, attacks 1, attack-state frames 31, clips seen: WeakAttack_SwordB, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Wood Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 26 |  |
| Audio | 'Wood Axe' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Wood Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_9:Melee Weapon Attack 4, SfxPool_0:Melee Weapon Air Swing 3, SfxPool_1:Melee Weapon Attack 4; vHitEffects on 'Drawn_Wood_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Wood Axe' damages the player | PASS | player hp lost 62.5 (item dmg 26), attacks 1, hitbox-active frames 16, hit events 2, sub-1hp chip 0.0, first hit 0.3s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Wood Axe': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 2, player hp lost 62.5; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 18, clips seen: Attack2Hand_C, WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: DM_Mining_Tool

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item DM_Mining_Tool; drawn []; holstered []; item dmg 0 | ItemData rangedDamage = 0 |
| Weapons | Ranged 'DM_Mining_Tool' damages the player | WARN | player hp lost 16.1 (item dmg 0), shots/projectiles 1, attack-state frames 31, attacks 1, first hit 1.3s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item |
| Weapons | Ranged 'DM_Mining_Tool': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread DM_Mining_Tool: projectileSpread 0 deg, accuracy 100, close-range scale 0.2 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 1.2s (at 7.2m), first damage 1.3s, attacks 1, attack-state frames 31, clips seen: WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | FAIL | max drawn 0: ; stray renderers  | second weapon visual left active, or the equipped weapon has no drawn visual |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: sci_fi_pistol

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Wood_Axe]; item dmg 14 |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.9 (item dmg 14), shots/projectiles 2, attack-state frames 40, attacks 2, first hit 1.8s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 2, damage events 1 (50 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 92 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 2 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.3s (at 7.2m), first damage 1.8s, attacks 2, attack-state frames 40, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: survival_rifle

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_Wood_Axe]; item dmg 18 |  |
| Weapons | Ranged 'survival_rifle' damages the player | PASS | player hp lost 16.9 (item dmg 18), shots/projectiles 2, attack-state frames 39, attacks 2, first hit 1.6s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'survival_rifle': hit rate vs configured accuracy | INFO | shots/projectiles 2, damage events 1 (50 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 92 %); item spread survival_rifle: projectileSpread 0.9 deg, accuracy 82, close-range scale 0.15 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 2 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.2s (at 7.2m), first damage 1.6s, attacks 2, attack-state frames 39, clips seen: none matched |  |
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
| Camera: faces forward after teleports, zoom in/out | 2 | 0 | 1 | 9s |
| Equip: weapon survives a teleport | 2 | 0 | 0 | 6s |
| Audio: swing / hit sound wiring (static) | 3 | 0 | 1 | 0s |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | 4 | 11 | 5 | 59s |
| Block + parry: player defends against enemy melee | 1 | 1 | 2 | 43s |
| Block: enemy defends against player swings | 1 | 0 | 1 | 10s |
| Movement: retreat, push, flank, backstab, lose target | 4 | 0 | 2 | 31s |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | 4 | 0 | 5 | 53s |
| Multi-enemy: FRED x3 and x5 | 1 | 0 | 2 | 36s |
| Brain: orbit, toggles, archetypes | 8 | 0 | 4 | 128s |
| Player weapons: each melee item vs enemy (swapped in place) | 9 | 8 | 0 | 28s |
| Ammo: default ammo per ranged weapon + ammo-specific FX | 7 | 6 | 4 | 0s |
| Regression of earlier fixes and tweaks | 7 | 1 | 0 | 0s |

### Scenario: Camera: faces forward after teleports, zoom in/out

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Camera | Camera sits behind the player and faces the same way after teleports | PASS | yaw 0: dot 1.00; yaw 90: dot 1.00; yaw 180: dot 1.00; yaw 270: dot 1.00 (camera vThirdPersonCamera) |  |
| Camera | Camera zooms in and out | WARN | distance start 2.5m, zoomed in 2.5m, zoomed out 6.2m via ForceSetZoomDistance fallback (scroll input did not move the camera) | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state |
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
| Audio | Melee hit sound has a caller on the damage path | WARN | CombatHitAudio.PlayWeaponHit callers: PioneerInvectorDamageReceiver.cs, MeleeCombatController.cs, vMeleeManager.cs | the only callers are the enemy-body receiver (PioneerInvectorDamageReceiver), MeleeCombatController and CombatHitResolver: damage TO THE PLAYER (PioneerInvectorDamageBridge / the player's vDamageReceiver) has no hit-sound call, so enemy weapons hitting the player are silent |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Controls | Input path diagnostics (W held 0.35s) | INFO | isFocused False, cursor Locked, keyboard id 1 enabled True W pressed False, mouse enabled, keyboards 1, gamepads 0, InputSystem IgnoreFocus/AllDeviceInputAlwaysGoesToGameView, Player_v7 Combat Variant: scheme 'Keyboard&Mouse', enabled True, actions enabled True, devices [Keyboard,Mouse], Move (0.00, 0.00); Player_v7 Variant: scheme '', enabled False, actions enabled True, devices [], Move (0.00, 0.00) // while W held: isFocused False, cursor Locked, keyboard id 1 enabled True W pressed False, mouse enabled, keyboards 1, gamepads 0, InputSystem IgnoreFocus/AllDeviceInputAlwaysGoesToGameView, Player_v7 Combat Variant: scheme 'Keyboard&Mouse', enabled True, actions enabled True, devices [Keyboard,Mouse], Move (0.00, 0.00); Player_v7 Variant: scheme '', enabled False, actions enabled True, devices [], Move (0.00, 0.00) |  |
| Controls | W moves the player forward (camera direction) | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 m along forward | Move action not reaching the controller: PlayerInput disabled, move locked, or PioneerPlayerInputBinder.HandleAction('Move') not wired |
| Controls | S moves the player back | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 m along forward | Move (S) produced no backward displacement |
| Controls | A strafes left | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 m along left | Move (A) produced no leftward displacement |
| Controls | D strafes right | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 m along right | Move (D) produced no rightward displacement |
| Controls | W run speed (reference for sprint) | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 m in 1.5s | player did not move |
| Controls | Shift + W sprints faster than W | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 m in 1.5s | sprint no faster than run (0.0m): Sprint action not wired, stamina 0, or sprint speed equals run speed |
| Controls | Space jumps | FAIL | start 21.23, min 21.23, max 21.23, end 21.23 m height (+1000 while isJumping) | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked; state before the press: isGrounded True, isJumping False, customAction False, isCrouching False, stamina 1000 |
| Controls | Left Ctrl crouches | WARN | start 0.00, min 0.00, max 0.00, end 0.00 isCrouching | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) |
| Controls | Crouch through the controller code path (cc.Crouch) | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isCrouching |  |
| Controls | Q dodge / roll | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 m or isRolling | Dodge action did not move the player: TryDodgeRollFromInput refused (stamina, cooldown, no roll clip) |
| Controls | Left Alt dash | WARN | start 0.00, min 0.00, max 0.00, end 0.00 m | Dash action not wired or on cooldown (DMDashController) |
| Controls | Hold Space after jumping boosts (jetpack) | WARN | start 21.23, min 21.23, max 21.23, end 21.23 m height | no sustained lift: jetpack not unlocked/fuelled, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player |
| Controls | Mouse look left/right orbits the camera | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 deg yaw | Look action (Pointer/delta) not reaching vThirdPersonCamera: PlayerController.OnLook, cursor lock state, or camera input muted |
| Controls | Mouse look up/down tilts the camera | PASS | start 18.00, min 0.00, max 18.00, end 0.00 deg pitch |  |
| Controls | Left click swings the drawn melee weapon | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 isAttacking | melee Attack input not received: drawn melee tap/hold logic (UpdateDrawnMeleeCharge) did not start a swing |
| Controls | Right click blocks (melee drawn) | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 isBlocking | Block input (Mouse rightButton / Block action) did not set isBlocking: stamina 0, customAction, not grounded, or still attacking |
| Controls | Middle click does not break gameplay | PASS | start 0.00, min 0.00, max 0.00, end 0.00 none |  |
| Controls | Middle click has no gameplay binding | INFO | InputSystem_Actions Player map binds no action to Mouse/middleButton (only the UI map MiddleClick) |  |
| Controls | Tab (SwitchWeapon) cycles the weapon focus | WARN | Hot Cross weapon focus 0 -> 0, drawn weap2_sword -> weap2_sword | Tab tap did not change focus: only one weapon on the hotbar, Hot Cross not polling, or tap/hold threshold (hold arms) not met |
| Controls | Half-tilt stick walks slower than a run | WARN | start 0.00, min 0.00, max 0.86, end 0.87 m in 1.5s | walk speed not distinct from run (0.0m): analog Move magnitude ignored by the locomotion speed |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block + parry: player defends against enemy melee

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Unblocked baseline (same enemy, same distance) | INFO | hits 1, player hp lost 26.5 |  |
| Block | Holding block negates enemy melee damage | FAIL | isBlocking False (raised in 1.52s), hits 2, blocked 0, player hp lost 52.3 (unblocked 26.5), stamina 1000.0 | player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking |
| Block | Enemy guard-break stagger after a blocked hit | WARN | longest enemy stagger 0.00s (profile blockStaggerSeconds 1.2s) | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) |
| Parry | Parry timing windows (block pressed relative to the enemy swing) | INFO | press +0.00s after swing start: hit 3.24s after press -> HIT (block came too late), stagger 0.00s, hp lost 52.5 // press +0.15s after swing start: hit 3.30s after press -> HIT (block came too late), stagger 0.00s, hp lost 104.7 // press +0.30s after swing start: hit 0.77s after press -> HIT (block came too late), stagger 0.00s, hp lost 47.0 // press +0.45s after swing start: hit 3.47s after press -> HIT (block came too late), stagger 0.00s, hp lost 99.6 // press +0.60s after swing start: hit 0.27s after press -> HIT (block came too late), stagger 0.00s, hp lost 52.5 | profile: parryWindowSeconds 0.2, blockStagger 1.2s, parryStagger 5.0s |
| Parry | A parry happens when block is pressed inside the window | WARN | no parry observed (see sweep rows) | none of the sampled press times produced a parry: sweep resolution (0.15s steps at editor frame rate) may have missed the 0.2s window, or the stagger length did not match the parry profile |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block: enemy defends against player swings

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Enemy block reduces player melee damage | WARN | open: 0 hits, enemy hp lost 0.0; blocking (animator IsBlocking True, duration 40.0s): 0 hits (0 flagged blocked), enemy hp lost 0.0; block clips: Idle, Walk, Idle@Pistol, Defense_Unarmed | player swings registered no hits on the open enemy, so the comparison is inconclusive (input focus, reach, or the player weapon) |
| Parry | Enemy parry | INFO | not implemented: EnemyInvectorCombatBridge only has TryBeginBlock/EndBlock (no parry window/riposte API) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Movement: retreat, push, flank, backstab, lose target

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Movement | Player retreats 9m at 3 m/s: enemy follows | PASS | distance 3.3m -> 4.3m (min 2.3m), states Chase/Attack, attacks 1 |  |
| Movement | Player pushes into the enemy: spacing and facing | PASS | min distance 0.50m, max facing error 0deg, attack-state frames 73, states Attack/Chase |  |
| Movement | Player circles behind the enemy: enemy turns to face | WARN | reached behind at never, enemy faced player (<30deg) after never, max facing error 0deg, attacks while behind 0, player hp lost -20.8, states Attack | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player |
| Movement | Hit from behind vs from the front (player melee, AI off) | WARN | front: 0 hits, 0.0 hp; behind: 0 hits, 0.0 hp; ratio n/a | swings did not land from one side (reach/facing/input), comparison inconclusive |
| Movement | Player disappears 70m away: enemy loses the target | PASS | was aware True, investigate/search after never, back to calm state after 6.0s, states Chase/ReturnHome |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Play | Approach with W: enemy notices | WARN | walked -3.9m in 14.0s to 33.9m; enemy noticed after never at n/a; state Wander | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) |
| Play | Run away with S: enemy pursues | WARN | player backed away for 4.0s (gap now 27.1m), enemy moved 8.0m, hostile state seen False, states Wander | enemy did not chase a retreating player: chase blocked (standoff/hold token, ranged keep-distance) or target lost immediately |
| Play | Strafe around the enemy: it turns to face the player | PASS | enemy facing error avg 39deg, max 89deg over 130 frames while strafing D then A, state Wander |  |
| Play | Retreat then re-engage: enemy fights back | WARN | re-engaged to 26.7m, enemy attacks 0, player hp lost 0.0, state Wander | enemy never attacked a player standing next to it: attack cooldown/token starvation or stagger lock |
| Play | Jump away: player clears distance | WARN | jump height 0.00m, gap 26.7m -> 26.0m, enemy state Wander | jump did not leave the ground (Jump input / grounded) |
| Play | Jetpack escape (hold Space + S) | WARN | peak height gain 0.0m, gap 26.0m -> 32.7m, enemy state Wander | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump |
| Play | Sprint away: enemy loses the target | PASS | lost after 1.5s at 34.9m (sprinting), state Wander, states Wander |  |
| Play | Enemy returns to its post after losing the player | PASS | back home after 5.5s, now 3.5m from spawn, state Wander |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Multi-enemy: FRED x3 and x5

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Multi-enemy | Enemy used | INFO | Hybrid Droid (FRED / Hybrid Droid) |  |
| Multi-enemy | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 2 (108/390 frames with >1), distinct attackers 3/3, attacker hand-offs 9, attack-state frames [196,244,183], chase/hold frames [191,144,206], closest pair 2.00m, closest to player 1.16m, orbit around player avg 0deg max 1deg (net 0/0/0), player hp lost 0, brain actions Press:735/Hold:429/None:6 | 108/390 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Multi-enemy | 5 FREDs at once for 18s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 3 (183/353 frames with >1), distinct attackers 5/5, attacker hand-offs 15, attack-state frames [147,248,262,105,89], chase/hold frames [205,102,89,247,261], closest pair 1.34m, closest to player 1.16m, orbit around player avg 5deg max 13deg (net -9/1/0/13/0), player hp lost 51, brain actions Press:930/Hold:825/None:10 | 183/353 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Brain: orbit, toggles, archetypes

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Brain | Single FRED, player standing still: does not orbit the player | PASS | notice 0.0s, chase 0.0s, first attack 1.1s, attacks 3, dmg 126, min dist 1.2m, path 5.4m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:272/None:1, states Chase/Attack |  |
| Brain | Single FRED, player strafes around it: keeps facing the player, does not orbit on its own | PASS | player circled 4.0m radius for 9s; enemy avg facing error 4deg, within 40deg 100% of frames, enemy angular travel around the player 277deg, states Attack/Chase, path 9.5m |  |
| Multi-enemy | 3 FREDs, baseline (brain on, director on) | WARN | max simultaneous attackers 2 (105/294 frames with >1), distinct attackers 3/3, attacker hand-offs 6, attack-state frames [93,239,129], chase/hold frames [200,52,163], closest pair 1.99m, closest to player 1.16m, orbit around player avg 1deg max 1deg (net 0/1/-1), player hp lost 25, brain actions Press:519/Hold:357/None:6 | 105/294 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:519/Hold:357/None:6 |  |
| Multi-enemy | 3 FREDs, enableUtilityBrain=false | PASS | max simultaneous attackers 2 (87/293 frames with >1), distinct attackers 3/3, attacker hand-offs 7, attack-state frames [135,235,78], chase/hold frames [156,57,212], closest pair 2.01m, closest to player 1.16m, orbit around player avg 1deg max 2deg (net -1/0/2), player hp lost 25, brain actions None:876/Press:3 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions None:876/Press:3 |  |
| Multi-enemy | 3 FREDs, enableEngagementDirector=false | PASS | max simultaneous attackers 3 (174/289 frames with >1), distinct attackers 3/3, attacker hand-offs 8, attack-state frames [243,243,247], chase/hold frames [45,43,40], closest pair 2.31m, closest to player 1.34m, orbit around player avg 0deg max 1deg (net 0/1/0), player hp lost 98, brain actions Press:595/None:272 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = False; brain actions Press:595/None:272 |  |
| Multi-enemy | 3 FREDs, legacyRingSlotRotation=true | WARN | max simultaneous attackers 2 (95/282 frames with >1), distinct attackers 3/3, attacker hand-offs 8, attack-state frames [154,227,70], chase/hold frames [127,52,210], closest pair 2.05m, closest to player 1.16m, orbit around player avg 10deg max 20deg (net -6/1/-7), player hp lost 0, brain actions Press:477/Hold:363/None:6 | 95/282 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:477/Hold:363/None:6 |  |
| Multi-enemy | 3 FREDs, brain off + director off | PASS | max simultaneous attackers 3 (151/273 frames with >1), distinct attackers 3/3, attacker hand-offs 9, attack-state frames [225,220,224], chase/hold frames [45,51,48], closest pair 2.31m, closest to player 1.34m, orbit around player avg 0deg max 0deg (net 0/0/0), player hp lost 52, brain actions Press:555/None:264 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = False; brain actions Press:555/None:264 |  |
| Brain | enableUtilityBrain=false stops the brain acting (no Press/Defend/Retreat/Hold) | WARN | distinct brain actions: brain on 2 (None:6/Hold:357/Press:519), brain off 1 (None:876/Press:3) | the brain still selected actions with enableUtilityBrain=false (a per-type ProfileOverride, or a cached decision) |
| Brain | enableEngagementDirector=false: no single-attacker token, enemies fall back to the old behaviour | PASS | with the director on: max simultaneous attackers 2 (105 frames with >1); director off: 3 (174 frames with >1), distinct attackers 3 vs 3, hand-offs 6 vs 8 |  |
| Brain | legacyRingSlotRotation=true restores the old ring-slot rotation (more orbiting than the director) | WARN | orbit around the player avg 1deg (director) vs 10deg (legacy ring), distinct attackers 3 vs 3, hand-offs 6 vs 8, max simultaneous attackers 2 vs 2 | legacyRingSlotRotation made no measurable difference in 12s (the director may still own spacing, or the toggle is read only at spawn) |
| Brain | Archetype Duelist: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 0.9s, attacks 3, dmg 132, min dist 1.2m, path 4.3m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:268, states Chase/Attack |  |
| Brain | Archetype Rusher: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 0.8s, attacks 4, dmg 105, min dist 1.2m, path 4.4m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:281/None:1, states Chase/Attack |  |
| Brain | Archetype Berserker: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 1.0s, attacks 4, dmg 126, min dist 1.2m, path 4.4m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:270/None:2, states Wander/Chase/Attack |  |
| Brain | Personality Cowardly (Duelist): brain actions and attack rate | INFO | notice 0.1s, chase 0.1s, first attack 0.9s, attacks 3, dmg 126, min dist 1.2m, path 4.6m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:268/None:1, states Chase/Attack/Defensive |  |
| Brain | Brain DOES | INFO | scores Press / Defend / Retreat / Hold every decisionInterval from archetype rows (press, defend, retreat, tokenBias, holderTaunt, holderFeint) x personality multipliers x health condition (Healthy/Injured/SeverelyInjured/Critical); feeds the engagement director a token bias; scales holder idle actions (sidestep / taunt / feint); issues defend requests (block vs dodge) and retreat windows; reacts to intentional melee misses (miss recovery). Observed actions per archetype are in the rows above. |  |
| Brain | Brain does NOT | INFO | no weapon choice: DMEnemyBrain contains no weapon-switch call (weapon selection stays with the loadout bridge / combat bridge; brain-driven weapon choice and grenade throwing are deferred). It does not move the enemy itself (movement stays in EnemyAiController / the engagement director), does not pick targets, and does nothing when enableUtilityBrain is off. |  |
| Brain | Toggle / archetype tests left nothing on disk | PASS | Resources/Combat profiles, Data/Enemies definitions and enemy prefabs hashed before and after: identical |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player weapons: each melee item vs enemy (swapped in place)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Audio | Player '2 Hander' swing plays a swish | PASS | 3 audio start(s) outside the hit windows over 4 swings: SfxPool_2:QP01 0005 Wind howling, SfxPool_3:QP01 0005 Wind howling, SfxPool_4:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee '2 Hander' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 28), hits 0, player right weapon Drawn_2_Hander | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Blade Of Chaos' swing plays a swish | PASS | 2 audio start(s) outside the hit windows over 4 swings: SfxPool_5:QP01 0005 Wind howling, SfxPool_6:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Blade Of Chaos' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 21), hits 0, player right weapon InvectorWeapon_Blade Of Chaos | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Death Axe' swing plays a swish | PASS | 3 audio start(s) outside the hit windows over 4 swings: SfxPool_7:QP01 0005 Wind howling, SfxPool_8:QP01 0005 Wind howling, SfxPool_9:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Death Axe' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon Drawn_Death_Axe | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Spear of Fate' swing plays a swish | PASS | 3 audio start(s) outside the hit windows over 4 swings: SfxPool_0:QP01 0005 Wind howling, SfxPool_1:QP01 0005 Wind howling, SfxPool_2:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Spear of Fate' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 16), hits 0, player right weapon Drawn_Spear_of_Fate | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Sword of Fear' swing plays a swish | PASS | 2 audio start(s) outside the hit windows over 4 swings: SfxPool_4:QP01 0005 Wind howling, SfxPool_5:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Sword of Fear' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 15), hits 0, player right weapon Drawn_Sword_of_Fear | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'weap2_sword' swing plays a swish | PASS | 2 audio start(s) outside the hit windows over 4 swings: SfxPool_6:QP01 0005 Wind howling, SfxPool_7:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'weap2_sword' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon Drawn_weap2_sword | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'weap_two_handed' swing plays a swish | PASS | 2 audio start(s) outside the hit windows over 4 swings: SfxPool_8:QP01 0005 Wind howling, SfxPool_9:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'weap_two_handed' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 28), hits 0, player right weapon Drawn_Two_Handed_Sword | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Wood Axe' swing plays a swish | PASS | 3 audio start(s) outside the hit windows over 4 swings: SfxPool_0:QP01 0005 Wind howling, SfxPool_1:QP01 0005 Wind howling, SfxPool_2:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Wood Axe' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 26), hits 0, player right weapon Drawn_Wood_Axe | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Player weapons | Player uses 'DM_Mining_Tool' | INFO | mining tool: Plasma Fuel scanner/laser, not a damage weapon (damage 0 by design) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Ammo: default ammo per ranged weapon + ammo-specific FX

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Ammo | Ammo items and ranged weapons found | INFO | 10 ammo item(s): DMAmmoFxProfile_CarveExperiment(Gunpowder), DMAmmoFxProfile_Electricity(Electricity), DMAmmoFxProfile_Explosive(Explosive), DMAmmoFxProfile_Fire(Fire), DMAmmoFxProfile_Ice(Ice), DMAmmoFxProfile_Ion(Ion), DMAmmoFxProfile_Laser(Laser), DMAmmoFxProfile_Plasma(Plasma), DMAmmoFxProfile_Resonance Stabilizer(ResonanceStabilizer), DMAmmoFxProfile_Standard(Gunpowder); 2 ranged weapon(s): sci_fi_pistol, survival_rifle |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: fires (rounds consumed) | FAIL | 0 trigger pull(s), rounds 30 -> 30, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 14, ammo type Gunpowder, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: hit rate over aimed shots (shots may miss by design) | INFO | 0/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: hit marks / FX | INFO | hit marks stamped 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: per-ammo FX rules (blood / burn / glow / explosive handling) | INFO | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 0 -> 0, enemy hp lost 0.0 | no hit to evaluate the rules against |
| Player weapons | Player uses ranged 'sci_fi_pistol' on the enemy (default ammo) | FAIL | enemy hp lost 0.0 (item dmg 14), rounds consumed 0 | see the ammo damage row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: fires (rounds consumed) | FAIL | 0 trigger pull(s), rounds 30 -> 30, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 18, ammo type Gunpowder, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: hit rate over aimed shots (shots may miss by design) | INFO | 0/0 aimed shots hit (0 %); weapon accuracy 82 (effective incl. skill bonus 75), ammo accuracy 75, spread 0.9 deg (ammo 1.4), close-range scale 0.15 within 12m, hip multiplier 1; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: hit marks / FX | INFO | hit marks stamped 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: per-ammo FX rules (blood / burn / glow / explosive handling) | INFO | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 0 -> 0, enemy hp lost 0.0 | no hit to evaluate the rules against |
| Player weapons | Player uses ranged 'survival_rifle' on the enemy (default ammo) | FAIL | enemy hp lost 0.0 (item dmg 18), rounds consumed 0 | see the ammo damage row |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_CarveExperiment', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: fires | FAIL | 0 trigger pull(s), rounds 30 -> 30 | no round consumed with this ammo type loaded |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: damages the enemy | WARN | enemy hp lost 0.0 (ammo type Gunpowder) | no hit in the few shots fired: spread/accuracy can miss, too few shots to judge |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: hit rate over aimed shots (shots may miss by design) | INFO | 0/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 92), ammo accuracy 92, spread 1.8 deg (ammo 0.8), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: hit marks / FX | INFO | hit marks stamped 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: per-ammo FX rules (blood / burn / glow / explosive handling) | INFO | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Projectile, useHitMarks=True; hit marks 0 -> 0, enemy hp lost 0.0 | no hit to evaluate the rules against |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Explosive', ammoType Explosive |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires | FAIL | 0 trigger pull(s), rounds 30 -> 30 | no round consumed with this ammo type loaded |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: damages the enemy | WARN | enemy hp lost 0.0 (ammo type Explosive) | no hit in the few shots fired: spread/accuracy can miss, too few shots to judge |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: hit rate over aimed shots (shots may miss by design) | INFO | 0/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: hit marks / FX | INFO | hit marks stamped 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: per-ammo FX rules (blood / burn / glow / explosive handling) | INFO | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=True; hit marks 0 -> 0, enemy hp lost 0.0 | no hit to evaluate the rules against |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ion', ammoType Ion |  |

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | FAIL | code present: True; 12 PASS / 4 FAIL / 0 warn of 16 runtime row(s) | swing rows still silent for: '2 Hander' swing plays a swish, 'Sword of Fear' swing plays a swish, 'weap2_sword' swing plays a swish, 'Wood Axe' swing plays a swish |
| Fix check | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | PASS | code present: True; 8 PASS / 0 FAIL / 0 warn of 8 runtime row(s) |  |
| Fix check | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | PASS | 1 PASS / 0 FAIL / 0 warn of 1 runtime row(s); weapon reverts recorded this run: 0 |  |
| Fix check | vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons) | PASS | code present: True; console errors across 12 weapon-matrix case(s): 0 |  |
| Fix check | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | PASS | code present: True; dual-slot override case: 6 PASS / 0 FAIL |  |
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 48 value(s) compared on 8 enemies |  |
| Fix check | No circling while unaware (patrol-anchor fix) on every enemy | PASS | 9 PASS / 0 FAIL of 9 enemies |  |
| Fix check | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | PASS | 9 PASS / 0 FAIL of 9 enemies |  |
| Fix check | Profile adjustments made by earlier audit passes | INFO | none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes |  |

## Per-enemy detail

### Axe_Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Axe_Droid.prefab` - definition `Axe_Droid` - melee `Spear of Fate` / ranged `-` - audited in 25s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Axe_Droid, melee=Spear of Fate (dmg 16), ranged=- (dmg -), preferRanged=False, def.attackDamage=12, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 31ms, 794 components, frame avg 31.3ms max 53.0ms (288 frames), GC collections 0, managed delta 14132KB |  |
| AI | No circling while unaware (player 50m away, 9s) | PASS | yaw turned 57deg, path 3.7m, net 3.6m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.9s (at 2.3m, weapon Spear of Fate), first player damage 2.5s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 20.0 hp in 2.5s using Spear of Fate |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 20.0 (item dmg 16), attacks 1, hitbox-active frames 9 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Spear of Fate': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 20.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 225.3 (-24.7); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 225.3 -> 225.3; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | FAIL | loot bag held 0 entries, TryLootAll -> None, left 0; definition random loot count 0-2 | bag spawned but nothing could be looted (empty entries / TryLootAll refused) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 12 distinct clips seen in total |  |
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

### corrupt_patrol_android

Prefab `Assets/_Project/Prefabs/Combat/Enemies/corrupt_patrol_android.prefab` - definition `corrupt_patrol_android` - melee `Death Axe` / ranged `sci_fi_pistol` - audited in 27s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=corrupt_patrol_android, melee=Death Axe (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=350 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 27ms, 736 components, frame avg 30.7ms max 55.1ms (293 frames), GC collections 0, managed delta 10888KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 163deg, path 11.3m, net 1.7m, states Wander |  |
| AI | Notice / chase / attack timing (player 5m in front) | PASS | notice 0.0s, chase 0.0s, first attack 0.7s (at 2.3m, weapon Death Axe), first player damage 1.2s, attacks 1, states Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 24.0 hp in 1.2s using Death Axe |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 24.0 (item dmg 18), attacks 1, hitbox-active frames 10 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Death Axe': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 24.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.4 (item dmg 14), shots/projectiles 1, attack-state frames 50, attacks 1, first hit 2.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Death_Axe] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 350.0 -> 252.4 (-97.6); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 252.4 -> 252.4; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-2 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.4s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 1 clip(s) observed playing; 17 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/GreySpire Soldier.prefab` - definition `GreySpire Soldier` - melee `weap_two_handed` / ranged `-` - audited in 43s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=GreySpire Soldier, melee=weap_two_handed (dmg 28), ranged=- (dmg -), preferRanged=False, def.attackDamage=8, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 31ms, 797 components, frame avg 32.6ms max 70.3ms (277 frames), GC collections 0, managed delta 14312KB |  |
| AI | No circling while unaware (player 55m away, 9s) | PASS | yaw turned 89deg, path 9.3m, net 6.7m, states Wander |  |
| AI | Notice / chase / attack timing (player 8m in front) | WARN | notice 0.2s, chase 0.2s, first attack never (at -1.0m, weapon ), first player damage never, attacks 0, states Wander/Hold/ReturnHome | noticed but never attacked within 22s (chase blocked, standoff/hold token, attack range, or cooldown) |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 11 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.8s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap_two_handed': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 38.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 226.5 (-23.5); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 226.5 -> 226.5; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 2 entries, TryLootAll -> Granted, left 0, 2 granted item(s) removed again from the audit inventory; definition random loot count 2-2 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 9 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 30s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 27ms, 787 components, frame avg 31.8ms max 54.9ms (284 frames), GC collections 0, managed delta 16484KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 169deg, path 11.7m, net 5.9m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.0s, chase 0.0s, first attack 1.7s (at 2.2m, weapon weap2_sword), first player damage 2.3s, attacks 1, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.3s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 2, hitbox-active frames 22 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 3.2s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 2, intentional misses rolled 0 (0 %) vs configured 10 % (+-64 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 2 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.9 (item dmg 14), shots/projectiles 1, attack-state frames 50, attacks 1, first hit 2.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 172.9 (-27.1); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 172.9 -> 172.9; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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
| attack | WeakAttack_SwordB * | 0.79 |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humanoid.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 32s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 35ms, 696 components, frame avg 31.4ms max 51.1ms (287 frames), GC collections 0, managed delta 10164KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 240deg, path 5.8m, net 5.2m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.7s (at 2.2m, weapon weap2_sword), first player damage 5.9s, attacks 2, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 47.0 hp in 5.9s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 11 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.4 (item dmg 14), shots/projectiles 1, attack-state frames 52, attacks 1, first hit 2.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 277.8 (-22.2); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 277.8 -> 277.8; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | FAIL | loot bag held 0 entries, TryLootAll -> None, left 0; definition random loot count 0-1 | bag spawned but nothing could be looted (empty entries / TryLootAll refused) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.4s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 21 distinct clips seen in total |  |
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

### HumanoidEnemy_Invector

Prefab `Assets/_Project/Prefabs/Combat/Enemies/HumanoidEnemy_Invector.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 29s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 34ms, 722 components, frame avg 31.8ms max 52.6ms (283 frames), GC collections 0, managed delta 13880KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 238deg, path 9.3m, net 2.7m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.0s, chase 0.0s, first attack 1.7s (at 2.1m, weapon weap2_sword), first player damage 2.8s, attacks 1, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 47.0 hp in 2.8s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 11 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.6s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.5 (item dmg 14), shots/projectiles 1, attack-state frames 52, attacks 1, first hit 2.8s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 270.5 (-29.5); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 270.5 -> 270.5; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.4s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 21 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Hybrid Droid.prefab` - definition `FRED_Android` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 29s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=FRED_Android, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 32ms, 696 components, frame avg 31.9ms max 55.9ms (283 frames), GC collections 0, managed delta 15352KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 178deg, path 15.3m, net 6.9m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.8s (at 2.1m, weapon weap2_sword), first player damage 3.4s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 3.4s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 11 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.6s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.2m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.0 (item dmg 14), shots/projectiles 1, attack-state frames 53, attacks 1, first hit 2.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 275.1 (-24.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 275.1 -> 275.1; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | FAIL | loot bag held 0 entries, TryLootAll -> None, left 0; definition random loot count 0-1 | bag spawned but nothing could be looted (empty entries / TryLootAll refused) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.7s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 20 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `Robot_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 30s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Robot_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=15, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 35ms, 698 components, frame avg 31.2ms max 57.1ms (289 frames), GC collections 0, managed delta 16020KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 171deg, path 9.9m, net 2.4m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.5s (at 2.2m, weapon weap2_sword), first player damage 3.1s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 3.1s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 11 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 15.0 (item dmg 14), shots/projectiles 1, attack-state frames 51, attacks 1, first hit 3.2s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 397.2 (-102.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 397.2 -> 397.2; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Robot |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.4s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 19 distinct clips seen in total |  |
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

### The_Evil_One

Prefab `Assets/_Project/Prefabs/Combat/Enemies/The_Evil_One.prefab` - definition `The_Evil_One` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 29s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=The_Evil_One, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=True, def.attackDamage=12, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 34ms, 707 components, frame avg 31.3ms max 62.0ms (288 frames), GC collections 0, managed delta 11296KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 169deg, path 11.8m, net 5.3m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.7s (at 9.6m, weapon sci_fi_pistol), first player damage 3.2s, attacks 2, states Wander/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 15.2 hp in 3.2s using sci_fi_pistol |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 9 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.0s, min distance 1.01m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; no vMeleeWeapon on the drawn/bound weapon |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.0m, first attack at 1.0m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.2 (item dmg 14), shots/projectiles 2, attack-state frames 87, attacks 3, first hit 3.4s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.65 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 2, damage events 1 (50 %) vs about 35 % expected from definition missRate 0.65 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 101 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 2 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 473.7 (-26.3); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 473.7 -> 473.7; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 2 entries, TryLootAll -> Granted, left 0, 2 granted item(s) removed again from the audit inventory; definition random loot count 0-2 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.4s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 22 distinct clips seen in total |  |
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
