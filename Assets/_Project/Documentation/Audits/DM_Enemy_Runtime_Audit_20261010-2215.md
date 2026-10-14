# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-10 21:59:29 -> 2026-10-10 22:15:00 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-780.0, 21.2, -122.1), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 4; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 9
- Unit store: 36 stored unit(s); stamps audit 15C9D7 game A1BCD9 gear 204DA0; skip-unchanged is ON (Quick mode)
- Weapon matrix: 11 weapon items equipped one at a time (spawn override) on HumanoidEnemy_Invector; plus one dual-slot override case
- Player level raised in memory from 5 for equip gates (restored at the end; nothing saved)
- Mode quick: 36 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was False). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **quick**, wall time 15m31s

**9 enemies audited - 3 FAIL, 9 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 12 weapon cases + 16 scenarios): 275 PASS, 42 FAIL, 39 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 212 change(s).

- NEW enemy|Axe_Droid|perf = PASS
- NEW enemy|Axe_Droid|melee.miss = INFO
- CHANGED enemy|Axe_Droid|player.swing: PASS -> WARN
- NEW enemy|Axe_Droid|loot = PASS
- NEW enemy|corrupt_patrol_android|perf = PASS
- NEW enemy|corrupt_patrol_android|melee.miss = INFO
- NEW enemy|corrupt_patrol_android|ranged.miss = INFO
- CHANGED enemy|corrupt_patrol_android|player.swing: PASS -> WARN
- NEW enemy|corrupt_patrol_android|loot = FAIL
- NEW enemy|GreySpire Soldier|perf = PASS
- NEW enemy|GreySpire Soldier|melee.miss = INFO
- CHANGED enemy|GreySpire Soldier|player.swing: PASS -> WARN
- NEW enemy|GreySpire Soldier|loot = PASS
- NEW enemy|Humadroid|perf = PASS
- NEW enemy|Humadroid|melee.miss = INFO
- NEW enemy|Humadroid|ranged.miss = INFO
- CHANGED enemy|Humadroid|player.swing: PASS -> WARN
- CHANGED enemy|Humadroid|hitmarks: WARN -> PASS
- NEW enemy|Humadroid|loot = FAIL
- NEW enemy|Humanoid|perf = PASS
- NEW enemy|Humanoid|melee.miss = INFO
- NEW enemy|Humanoid|ranged.miss = INFO
- CHANGED enemy|Humanoid|player.swing: PASS -> WARN
- NEW enemy|Humanoid|loot = PASS
- NEW enemy|HumanoidEnemy_Invector|perf = PASS
- NEW enemy|HumanoidEnemy_Invector|melee.miss = INFO
- NEW enemy|HumanoidEnemy_Invector|ranged.miss = INFO
- CHANGED enemy|HumanoidEnemy_Invector|player.swing: PASS -> WARN
- NEW enemy|HumanoidEnemy_Invector|loot = FAIL
- NEW enemy|Hybrid Droid|perf = PASS
- NEW enemy|Hybrid Droid|melee.miss = INFO
- NEW enemy|Hybrid Droid|ranged.miss = INFO
- CHANGED enemy|Hybrid Droid|player.swing: PASS -> WARN
- NEW enemy|Hybrid Droid|loot = PASS
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
- CHANGED weapon|Death Axe|audio.hitplayer.Death Axe: FAIL -> PASS
- CHANGED weapon|Death Axe|w.damage: FAIL -> PASS
- NEW weapon|Death Axe|w.miss = INFO
- CHANGED weapon|Spear of Fate|audio.swing.Spear of Fate: PASS -> FAIL
- NEW weapon|Spear of Fate|w.miss = INFO
- CHANGED weapon|Sword of Fear|audio.hitplayer.Sword of Fear: FAIL -> PASS
- NEW weapon|Sword of Fear|w.miss = INFO
- NEW weapon|weap2_sword|w.miss = INFO
- NEW weapon|weap_two_handed|w.miss = INFO
- NEW weapon|Wood Axe|w.miss = INFO
- NEW weapon|DM_Mining_Tool|w.miss = INFO
- CHANGED weapon|DM_Mining_Tool|vis: PASS -> FAIL
- NEW weapon|sci_fi_pistol|w.miss = INFO
- NEW weapon|survival_rifle|w.miss = INFO
- CHANGED scenario|Audio: swing / hit sound wiring (static)|audio.swing.callers: FAIL -> PASS
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
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.walk: PASS -> WARN
- CHANGED scenario|Block + parry: player defends against enemy melee|blk.hold: PASS -> FAIL
- CHANGED scenario|Block + parry: player defends against enemy melee|blk.stagger: INFO -> WARN
- CHANGED scenario|Block + parry: player defends against enemy melee|parry.window: PASS -> WARN
- CHANGED scenario|Block: enemy defends against player swings|eblk: PASS -> WARN
- ... 132 more

## Timing

Wall time 931s (15.5 min), mode quick. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Axe_Droid | enemy | 29.1 | ran |
| corrupt_patrol_android | enemy | 29.0 | ran |
| GreySpire Soldier | enemy | 24.9 | ran |
| Humadroid | enemy | 28.1 | ran |
| Humanoid | enemy | 29.3 | ran |
| HumanoidEnemy_Invector | enemy | 30.7 | ran |
| Hybrid Droid | enemy | 31.8 | ran |
| Robot | enemy | 29.3 | ran |
| The_Evil_One | enemy | 26.3 | ran |
| 2 Hander | weapon | 3.0 | ran |
| Blade Of Chaos | weapon | 2.8 | ran |
| Death Axe | weapon | 2.0 | ran |
| Spear of Fate | weapon | 2.0 | ran |
| Sword of Fear | weapon | 2.8 | ran |
| weap2_sword | weapon | 2.0 | ran |
| weap_two_handed | weapon | 2.3 | ran |
| Wood Axe | weapon | 2.4 | ran |
| DM_Mining_Tool | weapon | 2.4 | ran |
| sci_fi_pistol | weapon | 5.7 | ran |
| survival_rifle | weapon | 1.5 | ran |
| OVERRIDE both slots | weapon | 4.2 | ran |
| Camera: faces forward after teleports, zoom in/out | scenario | 9.2 | ran |
| Equip: weapon survives a teleport | scenario | 6.1 | ran |
| Audio: swing / hit sound wiring (static) | scenario | 0.4 | ran |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | scenario | 58.8 | ran |
| Block + parry: player defends against enemy melee | scenario | 38.8 | ran |
| Block: enemy defends against player swings | scenario | 9.4 | ran |
| Movement: retreat, push, flank, backstab, lose target | scenario | 27.7 | ran |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | scenario | 57.3 | ran |
| Multi-enemy: FRED x3 and x5 | scenario | 36.1 | ran |
| Brain: orbit, toggles, archetypes | scenario | 128.1 | ran |
| Player weapons: each melee item vs enemy (swapped in place) | scenario | 27.8 | ran |
| Ammo: default ammo per ranged weapon + ammo-specific FX | scenario | 59.4 | ran |
| Player rig: enemy ranged shot hits a body bone + mark | scenario | 16.6 | ran |
| Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | scenario | 35.8 | ran |
| Behavior tweaks: over / under-exaggerated enemy values (runtime only) | scenario | 105.2 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| scenario|Brain: orbit, toggles, archetypes | setup | 128.4 | 3261 | 39.4 | 492.1 | 0.63 | 0.00 | 17.91 |
| scenario|Behavior tweaks: over / under-exaggerated enemy values (runtime only) | setup | 105.4 | 1842 | 57.3 | 654.1 | 0.86 | 0.00 | 28.94 |
| scenario|Ammo: default ammo per ranged weapon + ammo-specific FX | setup | 59.7 | 1754 | 34.0 | 163.1 | 0.44 | 0.00 | 16.49 |
| scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | setup | 59.0 | 2111 | 28.0 | 161.8 | 0.37 | 0.00 | 14.63 |
| scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | setup | 57.6 | 1802 | 31.9 | 122.0 | 0.39 | 0.00 | 15.62 |
| scenario|Block + parry: player defends against enemy melee | setup | 40.0 | 1128 | 35.4 | 285.4 | 0.54 | 0.00 | 16.20 |
| scenario|Multi-enemy: FRED x3 and x5 | setup | 37.6 | 892 | 42.1 | 424.1 | 0.73 | 0.00 | 20.27 |
| scenario|Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | setup | 36.1 | 490 | 73.6 | 198.5 | 1.14 | 0.00 | 42.26 |
| scenario|Player weapons: each melee item vs enemy (swapped in place) | setup | 28.0 | 840 | 33.3 | 148.6 | 0.44 | 0.00 | 15.84 |
| scenario|Movement: retreat, push, flank, backstab, lose target | setup | 28.0 | 811 | 34.5 | 112.7 | 0.48 | 0.00 | 15.79 |
| scenario|Player rig: enemy ranged shot hits a body bone + mark | setup | 16.8 | 229 | 73.3 | 220.9 | 0.99 | 0.00 | 40.22 |
| enemy|Axe_Droid | unaware 9s (+perf) | 12.8 | 274 | 46.9 | 2853.3 | 0.44 | 0.00 | 15.44 |
| scenario|Block: enemy defends against player swings | setup | 10.6 | 332 | 31.8 | 67.7 | 0.42 | 0.00 | 14.78 |
| scenario|Camera: faces forward after teleports, zoom in/out | setup | 9.4 | 255 | 37.0 | 74.5 | 0.45 | 0.00 | 14.82 |
| enemy|GreySpire Soldier | unaware 9s (+perf) | 9.0 | 244 | 37.0 | 95.0 | 0.50 | 0.00 | 16.10 |
| enemy|Humanoid | unaware 9s (+perf) | 9.0 | 250 | 36.1 | 68.4 | 0.43 | 0.00 | 15.53 |
| enemy|corrupt_patrol_android | unaware 9s (+perf) | 9.0 | 263 | 34.3 | 73.9 | 0.43 | 0.00 | 14.91 |
| enemy|The_Evil_One | unaware 9s (+perf) | 9.0 | 255 | 35.4 | 60.2 | 0.44 | 0.00 | 15.48 |
| enemy|Robot | unaware 9s (+perf) | 9.0 | 257 | 35.1 | 60.0 | 0.44 | 0.00 | 15.57 |
| enemy|HumanoidEnemy_Invector | unaware 9s (+perf) | 9.0 | 184 | 49.0 | 93.2 | 0.58 | 0.00 | 16.98 |
| enemy|Hybrid Droid | unaware 9s (+perf) | 9.0 | 260 | 34.6 | 59.2 | 0.43 | 0.00 | 15.43 |
| enemy|Humadroid | unaware 9s (+perf) | 9.0 | 247 | 36.5 | 65.5 | 0.45 | 0.00 | 15.49 |
| enemy|Hybrid Droid | death/loot | 6.8 | 173 | 39.3 | 378.5 | 0.43 | 0.00 | 15.66 |
| enemy|Axe_Droid | death/loot | 6.8 | 199 | 34.0 | 106.1 | 0.47 | 0.00 | 14.78 |
| enemy|Humanoid | death/loot | 6.7 | 172 | 39.1 | 157.8 | 0.43 | 0.00 | 15.49 |
| enemy|corrupt_patrol_android | death/loot | 6.7 | 182 | 36.6 | 90.5 | 0.47 | 0.00 | 15.35 |
| enemy|GreySpire Soldier | death/loot | 6.6 | 171 | 38.9 | 124.7 | 0.54 | 0.00 | 16.01 |
| enemy|Humadroid | death/loot | 6.6 | 176 | 37.6 | 86.1 | 0.58 | 0.00 | 16.05 |
| enemy|The_Evil_One | death/loot | 6.6 | 179 | 36.8 | 84.2 | 0.45 | 0.00 | 15.49 |
| enemy|HumanoidEnemy_Invector | death/loot | 6.5 | 180 | 36.2 | 77.1 | 0.42 | 0.00 | 15.41 |

Whole run (ran units only): 913s over 23033 frames, avg frame 39.7 ms, physics 0.56 ms, render 0.00 ms, scripts 18.12 ms per frame.

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
| 14 | 45 | WARN | Enemy guard-break stagger after a blocked hit | Block + parry: player defends against enemy melee | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 15 | 45 | WARN | Enemy block reduces player melee damage | Block: enemy defends against player swings | player swings registered no hits on the open enemy, so the comparison is inconclusive (input focus, reach, or the player weapon) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 16 | 30 | WARN | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | Multi-enemy: FRED x3 and x5 | 126/405 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Add enemy separation and make the attack token rotate between enemies. |
| 17 | 20 | FAIL | Loot dropped and can be looted (real loot path), then cleaned up at once | corrupt_patrol_android, Humadroid, HumanoidEnemy_Invector | bag spawned but nothing could be looted (empty entries / TryLootAll refused) | Inspect the measured value and cause on the detail row. |
| 18 | 20 | FAIL | '2 Hander' swing plays a swish | 2 Hander | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 19 | 20 | FAIL | 'Death Axe' swing plays a swish | Death Axe | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 20 | 20 | FAIL | 'Spear of Fate' swing plays a swish | Spear of Fate | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 21 | 20 | FAIL | W moves the player forward (camera direction) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move action not reaching the controller: PlayerInput disabled, move locked, or PioneerPlayerInputBinder.HandleAction('Move') not wired | Inspect the measured value and cause on the detail row. |
| 22 | 20 | FAIL | S moves the player back | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (S) produced no backward displacement | Inspect the measured value and cause on the detail row. |
| 23 | 20 | FAIL | A strafes left | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (A) produced no leftward displacement | Inspect the measured value and cause on the detail row. |
| 24 | 20 | FAIL | D strafes right | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (D) produced no rightward displacement | Inspect the measured value and cause on the detail row. |
| 25 | 20 | FAIL | W run speed (reference for sprint) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | player did not move | Inspect the measured value and cause on the detail row. |
| 26 | 20 | FAIL | Shift + W sprints faster than W | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | sprint no faster than run (0.0m): Sprint action not wired, stamina 0, or sprint speed equals run speed | Inspect the measured value and cause on the detail row. |
| 27 | 20 | FAIL | Space jumps | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked; state before the press: isGrounded True, isJumping False, customAction False, isCrouching False, stamina 1000 | Inspect the measured value and cause on the detail row. |
| 28 | 20 | FAIL | Q dodge / roll | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dodge action did not move the player: TryDodgeRollFromInput refused (stamina, cooldown, no roll clip) | Inspect the measured value and cause on the detail row. |
| 29 | 20 | FAIL | Mouse look left/right orbits the camera | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Look action (Pointer/delta) not reaching vThirdPersonCamera: PlayerController.OnLook, cursor lock state, or camera input muted | Inspect the measured value and cause on the detail row. |
| 30 | 20 | FAIL | Left click swings the drawn melee weapon | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | melee Attack input not received: drawn melee tap/hold logic (UpdateDrawnMeleeCharge) did not start a swing | Inspect the measured value and cause on the detail row. |
| 31 | 20 | FAIL | Right click blocks (melee drawn) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Block input (Mouse rightButton / Block action) did not set isBlocking: stamina 0, customAction, not grounded, or still attacking | Inspect the measured value and cause on the detail row. |
| 32 | 20 | WARN | Player circles behind the enemy: enemy turns to face | Movement: retreat, push, flank, backstab, lose target | slow turn: > 2.5s to face a flanking player (turn speed / only turns when attacking) | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 33 | 20 | WARN | Hit from behind vs from the front (player melee, AI off) | Movement: retreat, push, flank, backstab, lose target | swings did not land from one side (reach/facing/input), comparison inconclusive | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 34 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Standard: fires (rounds consumed) | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 35 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Standard: fires (rounds consumed) | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 36 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 37 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 38 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Ion: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 39 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Laser: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 40 | 20 | FAIL | X (AmmoCycle) switches the loaded ammo type | Ammo: default ammo per ranged weapon + ammo-specific FX | X tap did not cycle: AmmoCycle owned by GameplayKeyboardShortcuts poll (needs drawn ranged weapon + Hot Cross focus) or the next type does not fit the weapon | Inspect the measured value and cause on the detail row. |
| 41 | 20 | FAIL | Aim follows the moving enemy: chest | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | crosshair never rested on a DM hitbox of this body part: aim tracking lagging the strafing enemy, or the enemy has no hitbox rig/bone | Inspect the measured value and cause on the detail row. |
| 42 | 20 | FAIL | Aim follows the moving enemy: head | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 43 | 20 | FAIL | Aim follows the moving enemy: arm | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 44 | 20 | FAIL | Aim follows the moving enemy: leg | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 45 | 20 | FAIL | Aim follows the moving enemy: foot | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 46 | 20 | FAIL | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks | swing rows still silent for: '2 Hander' swing plays a swish, 'Death Axe' swing plays a swish, 'Spear of Fate' swing plays a swish | Inspect the measured value and cause on the detail row. |
| 47 | 15 | WARN | A parry happens when block is pressed inside the window | Block + parry: player defends against enemy melee | none of the sampled press times produced a parry: sweep resolution (0.15s steps at editor frame rate) may have missed the 0.2s window, or the stagger length did not match the parry profile | Inspect the measured value and cause on the detail row. |
| 48 | -5 | WARN | Player melee swing via injected Mouse input (best effort) | Axe_Droid, corrupt_patrol_android, GreySpire Soldier, Humadroid +5 more | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes | Inspect the measured value and cause on the detail row. |
| 49 | -5 | WARN | Camera zooms in and out | Camera: faces forward after teleports, zoom in/out | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state | Inspect the measured value and cause on the detail row. |
| 50 | -5 | WARN | Melee hit sound has a caller on the damage path | Audio: swing / hit sound wiring (static) | the only callers are the enemy-body receiver (PioneerInvectorDamageReceiver), MeleeCombatController and CombatHitResolver: damage TO THE PLAYER (PioneerInvectorDamageBridge / the player's vDamageReceiver) has no hit-sound call, so enemy weapons hitting the player are silent | Inspect the measured value and cause on the detail row. |
| 51 | -5 | WARN | Left Ctrl crouches | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) | Inspect the measured value and cause on the detail row. |
| 52 | -5 | WARN | Left Alt dash | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dash action not wired or on cooldown (DMDashController) | Inspect the measured value and cause on the detail row. |
| 53 | -5 | WARN | Hold Space after jumping boosts (jetpack) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | no sustained lift: jetpack not unlocked/fuelled, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player | Inspect the measured value and cause on the detail row. |
| 54 | -5 | WARN | Tab (SwitchWeapon) cycles the weapon focus | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Tab tap did not change focus: only one weapon on the hotbar, Hot Cross not polling, or tap/hold threshold (hold arms) not met | Inspect the measured value and cause on the detail row. |
| 55 | -5 | WARN | Half-tilt stick walks slower than a run | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | walk speed not distinct from run (0.0m): analog Move magnitude ignored by the locomotion speed | Inspect the measured value and cause on the detail row. |
| 56 | -5 | WARN | Approach with W: enemy notices | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) | Inspect the measured value and cause on the detail row. |
| 57 | -5 | WARN | Run away with S: enemy pursues | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy did not chase a retreating player: chase blocked (standoff/hold token, ranged keep-distance) or target lost immediately | Inspect the measured value and cause on the detail row. |
| 58 | -5 | WARN | Retreat then re-engage: enemy fights back | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy never attacked a player standing next to it: attack cooldown/token starvation or stagger lock | Inspect the measured value and cause on the detail row. |
| 59 | -5 | WARN | Jump away: player clears distance | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | jump did not leave the ground (Jump input / grounded) | Inspect the measured value and cause on the detail row. |
| 60 | -5 | WARN | Jetpack escape (hold Space + S) | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Axe_Droid | Spear of Fate | - | PASS | PASS | PASS | PASS | PASS | n/a | PASS | PASS | PASS | PASS |
| corrupt_patrol_android | Death Axe | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| GreySpire Soldier | weap_two_handed | - | PASS | PASS | PASS | PASS | PASS | n/a | PASS | PASS | PASS | PASS |
| Humadroid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Humanoid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| HumanoidEnemy_Invector | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Hybrid Droid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Robot | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| The_Evil_One | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |

## Weapons that deal NO damage (headline)

- none detected in this run.

## Weapon matrix (every weapon item on the template, spawn override)

12 cases, 4 FAIL. Each weapon is equipped on `HumanoidEnemy_Invector` through ConfigureSpawnLoadout (the spawner path) and made to attack the player.

| Weapon | Kind | Equips | Damages player | One drawn | Measured | Probable cause |
|---|---|---|---|---|---|---|
| 2 Hander | melee | PASS | PASS | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 10, hit events 1, sub-1hp chip 0.4, first hit 0.2s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Blade Of Chaos | melee | PASS | PASS | PASS | player hp lost 54.0 (item dmg 21), attacks 1, hitbox-active frames 10, hit events 1, sub-1hp chip 1.0, first hit 1.0s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hi... |  |
| Death Axe | melee | PASS | PASS | PASS | player hp lost 48.4 (item dmg 18), attacks 1, hitbox-active frames 14, hit events 2, sub-1hp chip 0.4, first hit 0.3s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: laye... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Spear of Fate | melee | PASS | PASS | PASS | player hp lost 40.5 (item dmg 16), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.0, first hit 0.3s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitB... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Sword of Fear | melee | PASS | PASS | PASS | player hp lost 34.0 (item dmg 15), attacks 1, hitbox-active frames 10, hit events 1, sub-1hp chip 1.0, first hit 1.0s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitB... |  |
| weap2_sword | melee | PASS | PASS | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 7, hit events 1, sub-1hp chip 0.0, first hit 0.3s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: l... |  |
| weap_two_handed | melee | PASS | PASS | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 13, hit events 1, sub-1hp chip 0.4, first hit 0.5s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38;... |  |
| Wood Axe | melee | PASS | PASS | PASS | player hp lost 32.0 (item dmg 26), attacks 1, hitbox-active frames 9, hit events 1, sub-1hp chip 0.0, first hit 0.7s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer I... |  |
| DM_Mining_Tool | ranged | PASS | warn | **FAIL** | player hp lost 17.1 (item dmg 0), shots/projectiles 1, attack-state frames 32, attacks 1, first hit 1.2s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCoo... | second weapon visual left active, or the equipped weapon has no drawn visual |
| sci_fi_pistol | ranged | PASS | PASS | PASS | player hp lost 16.1 (item dmg 14), shots/projectiles 4, attack-state frames 93, attacks 4, first hit 4.5s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRang... |  |
| survival_rifle | ranged | PASS | PASS | PASS | player hp lost 19.9 (item dmg 18), shots/projectiles 0, attack-state frames 6, attacks 1, first hit 0.2s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRan... |  |
| OVERRIDE both slots | dual-slot spawn override | - | - | PASS | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)): PASS ; Starts with the ranged override drawn (prefer-ranged on, start-melee off): PASS ; Switch to the melee override vi... |  |

### Weapon: 2 Hander

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item 2 Hander; drawn [Drawn_2_Hander]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | '2 Hander' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | '2 Hander' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_3:Melee Weapon Attack 1; vHitEffects on 'Drawn_2_Hander': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee '2 Hander' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 10, hit events 1, sub-1hp chip 0.4, first hit 0.2s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee '2 Hander': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 38.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.3m, first attack at 1.3m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.3m), first damage 0.2s, attacks 1, attack-state frames 13, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Blade Of Chaos

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 21 |  |
| Audio | 'Blade Of Chaos' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_9:QP01 0005 Wind howling, SfxPool_0:QP01 0005 Wind howling, SfxPool_1:Melee Weapon Air Swing 1 |  |
| Audio | 'Blade Of Chaos' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_2:Melee Weapon Attack 4; vHitEffects on 'Drawn_Blade_Of_Chaos': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 54.0 (item dmg 21), attacks 1, hitbox-active frames 10, hit events 1, sub-1hp chip 1.0, first hit 1.0s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Blade Of Chaos': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 54.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 1.0s, attacks 1, attack-state frames 36, clips seen: Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Death Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'Death Axe' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Death Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_0:Melee Weapon Attack 4, SfxPool_1:Melee Weapon Air Swing 1, SfxPool_2:Melee Weapon Attack 1; vHitEffects on 'Drawn_Death_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 48.4 (item dmg 18), attacks 1, hitbox-active frames 14, hit events 2, sub-1hp chip 0.4, first hit 0.3s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Death Axe': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 2, player hp lost 48.4; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 16, clips seen: Attack2Hand_C, WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Spear of Fate

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 16 |  |
| Audio | 'Spear of Fate' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Spear of Fate' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_9:Melee Weapon Attack 4, SfxPool_0:Melee Weapon Air Swing 4, SfxPool_1:Melee Weapon Attack 4; vHitEffects on 'Drawn_Spear_of_Fate': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 40.5 (item dmg 16), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.0, first hit 0.3s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Spear of Fate': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 2, player hp lost 40.5; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 18, clips seen: WeakAttack_SwordB, WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Sword of Fear

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Sword of Fear; drawn [Drawn_Sword_of_Fear]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 15 |  |
| Audio | 'Sword of Fear' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_8:QP01 0005 Wind howling, SfxPool_9:Melee Weapon Air Swing 1 |  |
| Audio | 'Sword of Fear' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_0:Melee Weapon Attack 4; vHitEffects on 'Drawn_Sword_of_Fear': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Sword of Fear' damages the player | PASS | player hp lost 34.0 (item dmg 15), attacks 1, hitbox-active frames 10, hit events 1, sub-1hp chip 1.0, first hit 1.0s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Sword of Fear': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 34.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 1.0s, attacks 1, attack-state frames 36, clips seen: WeakAttack_SwordB, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap2_sword

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'weap2_sword' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_7:QP01 0005 Wind howling |  |
| Audio | 'weap2_sword' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_8:Melee Weapon Attack 4; vHitEffects on 'Drawn_weap2_sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 7, hit events 1, sub-1hp chip 0.0, first hit 0.3s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 16, clips seen: Attack2Hand_C, WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap_two_handed

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | 'weap_two_handed' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_4:Melee Weapon Air Swing 3 |  |
| Audio | 'weap_two_handed' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_5:Melee Weapon Attack 1, SfxPool_6:QP01 0005 Wind howling; vHitEffects on 'Drawn_Two_Handed_Sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 13, hit events 1, sub-1hp chip 0.4, first hit 0.5s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap_two_handed': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 38.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.5s, attacks 1, attack-state frames 22, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Wood Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 26 |  |
| Audio | 'Wood Axe' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_2:Melee Weapon Air Swing 2 |  |
| Audio | 'Wood Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_3:Melee Weapon Attack 4; vHitEffects on 'Drawn_Wood_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Wood Axe' damages the player | PASS | player hp lost 32.0 (item dmg 26), attacks 1, hitbox-active frames 9, hit events 1, sub-1hp chip 0.0, first hit 0.7s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Wood Axe': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 32.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.2s (at 1.5m), first damage 0.7s, attacks 1, attack-state frames 26, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: DM_Mining_Tool

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item DM_Mining_Tool; drawn []; holstered []; item dmg 0 | ItemData rangedDamage = 0 |
| Weapons | Ranged 'DM_Mining_Tool' damages the player | WARN | player hp lost 17.1 (item dmg 0), shots/projectiles 1, attack-state frames 32, attacks 1, first hit 1.2s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item |
| Weapons | Ranged 'DM_Mining_Tool': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread DM_Mining_Tool: projectileSpread 0 deg, accuracy 100, close-range scale 0.2 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 1.1s (at 7.2m), first damage 1.2s, attacks 1, attack-state frames 32, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | FAIL | max drawn 0: ; stray renderers  | second weapon visual left active, or the equipped weapon has no drawn visual |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: sci_fi_pistol

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Wood_Axe]; item dmg 14 |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.1 (item dmg 14), shots/projectiles 4, attack-state frames 93, attacks 4, first hit 4.5s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 4, damage events 1 (25 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 65 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 4 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.2s (at 7.2m), first damage 4.5s, attacks 4, attack-state frames 93, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: survival_rifle

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_Wood_Axe]; item dmg 18 |  |
| Weapons | Ranged 'survival_rifle' damages the player | PASS | player hp lost 19.9 (item dmg 18), shots/projectiles 0, attack-state frames 6, attacks 1, first hit 0.2s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'survival_rifle': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 1 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread survival_rifle: projectileSpread 0.9 deg, accuracy 82, close-range scale 0.15 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.1s (at 7.2m), first damage 0.2s, attacks 1, attack-state frames 6, clips seen: none matched |  |
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
| Block + parry: player defends against enemy melee | 1 | 1 | 2 | 39s |
| Block: enemy defends against player swings | 1 | 0 | 1 | 9s |
| Movement: retreat, push, flank, backstab, lose target | 4 | 0 | 2 | 28s |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | 4 | 0 | 5 | 57s |
| Multi-enemy: FRED x3 and x5 | 1 | 0 | 2 | 36s |
| Brain: orbit, toggles, archetypes | 8 | 0 | 4 | 128s |
| Player weapons: each melee item vs enemy (swapped in place) | 9 | 8 | 0 | 28s |
| Ammo: default ammo per ranged weapon + ammo-specific FX | 9 | 9 | 6 | 59s |
| Player rig: enemy ranged shot hits a body bone + mark | 5 | 0 | 0 | 17s |
| Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | 1 | 5 | 0 | 36s |
| Behavior tweaks: over / under-exaggerated enemy values (runtime only) | 14 | 0 | 0 | 105s |
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
| Controls | Half-tilt stick walks slower than a run | WARN | start 0.00, min 0.00, max 0.85, end 0.87 m in 1.5s | walk speed not distinct from run (0.0m): analog Move magnitude ignored by the locomotion speed |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block + parry: player defends against enemy melee

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Unblocked baseline (same enemy, same distance) | INFO | hits 1, player hp lost 47.0 |  |
| Block | Holding block negates enemy melee damage | FAIL | isBlocking False (raised in 1.51s), hits 2, blocked 0, player hp lost 52.8 (unblocked 47.0), stamina 1000.0 | player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking |
| Block | Enemy guard-break stagger after a blocked hit | WARN | longest enemy stagger 0.00s (profile blockStaggerSeconds 1.2s) | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) |
| Parry | Parry timing windows (block pressed relative to the enemy swing) | INFO | press +0.00s after swing start: hit 1.19s after press -> HIT (block came too late), stagger 0.00s, hp lost 47.0 // press +0.15s after swing start: hit 0.95s after press -> HIT (block came too late), stagger 0.00s, hp lost 47.1 // press +0.30s after swing start: hit 1.98s after press -> HIT (block came too late), stagger 0.00s, hp lost 52.6 // press +0.45s after swing start: hit 3.19s after press -> HIT (block came too late), stagger 0.00s, hp lost 74.0 // press +0.60s after swing start: hit 3.29s after press -> HIT (block came too late), stagger 0.00s, hp lost 78.2 | profile: parryWindowSeconds 0.2, blockStagger 1.2s, parryStagger 5.0s |
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
| Movement | Player retreats 9m at 3 m/s: enemy follows | PASS | distance 3.5m -> 4.7m (min 2.2m), states Chase/Attack, attacks 1 |  |
| Movement | Player pushes into the enemy: spacing and facing | PASS | min distance 0.50m, max facing error 0deg, attack-state frames 78, states Attack/Chase |  |
| Movement | Player circles behind the enemy: enemy turns to face | WARN | reached behind at 1.2s, enemy faced player (<30deg) after 4.3s, max facing error 180deg, attacks while behind 0, player hp lost 38.1, states Attack/ReturnHome/Chase | slow turn: > 2.5s to face a flanking player (turn speed / only turns when attacking) |
| Movement | Hit from behind vs from the front (player melee, AI off) | WARN | front: 0 hits, 0.0 hp; behind: 0 hits, 0.0 hp; ratio n/a | swings did not land from one side (reach/facing/input), comparison inconclusive |
| Movement | Player disappears 70m away: enemy loses the target | PASS | was aware True, investigate/search after never, back to calm state after 3.4s, states Chase/ReturnHome |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Play | Approach with W: enemy notices | WARN | walked -5.2m in 14.0s to 35.2m; enemy noticed after never at n/a; state Wander | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) |
| Play | Run away with S: enemy pursues | WARN | player backed away for 4.0s (gap now 34.0m), enemy moved 1.9m, hostile state seen False, states Wander | enemy did not chase a retreating player: chase blocked (standoff/hold token, ranged keep-distance) or target lost immediately |
| Play | Strafe around the enemy: it turns to face the player | PASS | enemy facing error avg 55deg, max 58deg over 127 frames while strafing D then A, state Wander |  |
| Play | Retreat then re-engage: enemy fights back | WARN | re-engaged to 26.0m, enemy attacks 0, player hp lost 0.0, state Wander | enemy never attacked a player standing next to it: attack cooldown/token starvation or stagger lock |
| Play | Jump away: player clears distance | WARN | jump height 0.00m, gap 26.0m -> 25.0m, enemy state Wander | jump did not leave the ground (Jump input / grounded) |
| Play | Jetpack escape (hold Space + S) | WARN | peak height gain 0.0m, gap 25.0m -> 29.6m, enemy state Wander | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump |
| Play | Sprint away: enemy loses the target | PASS | lost after 1.5s at 31.2m (sprinting), state Wander, states Wander |  |
| Play | Enemy returns to its post after losing the player | PASS | back home after 9.5s, now 3.5m from spawn, state Wander |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Multi-enemy: FRED x3 and x5

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Multi-enemy | Enemy used | INFO | Hybrid Droid (FRED / Hybrid Droid) |  |
| Multi-enemy | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 2 (126/405 frames with >1), distinct attackers 3/3, attacker hand-offs 9, attack-state frames [206,190,261], chase/hold frames [198,212,142], closest pair 2.00m, closest to player 1.16m, orbit around player avg 0deg max 1deg (net 0/0/0), player hp lost 0, brain actions Press:763/Hold:446/None:6 | 126/405 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Multi-enemy | 5 FREDs at once for 18s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 3 (185/393 frames with >1), distinct attackers 5/5, attacker hand-offs 16, attack-state frames [100,279,150,144,226], chase/hold frames [290,112,242,246,165], closest pair 1.35m, closest to player 1.16m, orbit around player avg 1deg max 3deg (net 0/0/3/0/-1), player hp lost 77, brain actions Press:1001/Hold:953/None:11 | 185/393 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Brain: orbit, toggles, archetypes

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Brain | Single FRED, player standing still: does not orbit the player | PASS | notice 0.1s, chase 0.1s, first attack 1.3s, attacks 3, dmg 106, min dist 1.2m, path 5.4m, yaw 7deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:287/None:1, states Chase/Attack |  |
| Brain | Single FRED, player strafes around it: keeps facing the player, does not orbit on its own | PASS | player circled 4.0m radius for 9s; enemy avg facing error 6deg, within 40deg 100% of frames, enemy angular travel around the player 274deg, states Attack/Chase, path 8.7m |  |
| Multi-enemy | 3 FREDs, baseline (brain on, director on) | WARN | max simultaneous attackers 2 (70/283 frames with >1), distinct attackers 3/3, attacker hand-offs 4, attack-state frames [98,126,231], chase/hold frames [182,155,51], closest pair 1.84m, closest to player 1.16m, orbit around player avg 5deg max 15deg (net -14/-1/0), player hp lost 0, brain actions Press:511/Hold:332/None:6 | 70/283 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:511/Hold:332/None:6 |  |
| Multi-enemy | 3 FREDs, enableUtilityBrain=false | PASS | max simultaneous attackers 2 (114/296 frames with >1), distinct attackers 3/3, attacker hand-offs 6, attack-state frames [81,236,138], chase/hold frames [214,57,156], closest pair 2.02m, closest to player 1.16m, orbit around player avg 1deg max 2deg (net 0/-2/-1), player hp lost 0, brain actions None:883/Press:5 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions None:883/Press:5 |  |
| Multi-enemy | 3 FREDs, enableEngagementDirector=false | PASS | max simultaneous attackers 3 (139/292 frames with >1), distinct attackers 3/3, attacker hand-offs 9, attack-state frames [245,242,246], chase/hold frames [46,47,44], closest pair 2.31m, closest to player 1.34m, orbit around player avg 0deg max 1deg (net 0/0/0), player hp lost 104, brain actions Press:445/None:431 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = False; brain actions Press:445/None:431 |  |
| Multi-enemy | 3 FREDs, legacyRingSlotRotation=true | WARN | max simultaneous attackers 2 (124/305 frames with >1), distinct attackers 3/3, attacker hand-offs 9, attack-state frames [200,169,116], chase/hold frames [102,134,188], closest pair 1.61m, closest to player 1.16m, orbit around player avg 17deg max 37deg (net 5/10/37), player hp lost 98, brain actions Press:588/Hold:321/None:6 | 124/305 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:588/Hold:321/None:6 |  |
| Multi-enemy | 3 FREDs, brain off + director off | PASS | max simultaneous attackers 3 (152/280 frames with >1), distinct attackers 3/3, attacker hand-offs 11, attack-state frames [237,233,231], chase/hold frames [41,46,46], closest pair 2.30m, closest to player 1.34m, orbit around player avg 1deg max 2deg (net 0/0/-2), player hp lost 78, brain actions None:431/Press:409 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = False; brain actions None:431/Press:409 |  |
| Brain | enableUtilityBrain=false stops the brain acting (no Press/Defend/Retreat/Hold) | WARN | distinct brain actions: brain on 2 (None:6/Hold:332/Press:511), brain off 1 (None:883/Press:5) | the brain still selected actions with enableUtilityBrain=false (a per-type ProfileOverride, or a cached decision) |
| Brain | enableEngagementDirector=false: no single-attacker token, enemies fall back to the old behaviour | PASS | with the director on: max simultaneous attackers 2 (70 frames with >1); director off: 3 (139 frames with >1), distinct attackers 3 vs 3, hand-offs 4 vs 9 |  |
| Brain | legacyRingSlotRotation=true restores the old ring-slot rotation (more orbiting than the director) | WARN | orbit around the player avg 5deg (director) vs 17deg (legacy ring), distinct attackers 3 vs 3, hand-offs 4 vs 9, max simultaneous attackers 2 vs 2 | legacyRingSlotRotation made no measurable difference in 12s (the director may still own spacing, or the toggle is read only at spawn) |
| Brain | Archetype Duelist: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.9s, attacks 4, dmg 78, min dist 1.2m, path 4.2m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:282, states Chase/Attack |  |
| Brain | Archetype Rusher: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 4, dmg 157, min dist 1.2m, path 4.0m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:278, states Chase/Attack |  |
| Brain | Archetype Berserker: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 0.9s, attacks 4, dmg 79, min dist 1.2m, path 4.3m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:291, states Chase/Attack |  |
| Brain | Personality Cowardly (Duelist): brain actions and attack rate | INFO | notice 0.0s, chase 0.0s, first attack 0.8s, attacks 4, dmg 78, min dist 1.2m, path 4.2m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:287, states Chase/Attack |  |
| Brain | Brain DOES | INFO | scores Press / Defend / Retreat / Hold every decisionInterval from archetype rows (press, defend, retreat, tokenBias, holderTaunt, holderFeint) x personality multipliers x health condition (Healthy/Injured/SeverelyInjured/Critical); feeds the engagement director a token bias; scales holder idle actions (sidestep / taunt / feint); issues defend requests (block vs dodge) and retreat windows; reacts to intentional melee misses (miss recovery). Observed actions per archetype are in the rows above. |  |
| Brain | Brain does NOT | INFO | no weapon choice: DMEnemyBrain contains no weapon-switch call (weapon selection stays with the loadout bridge / combat bridge; brain-driven weapon choice and grenade throwing are deferred). It does not move the enemy itself (movement stays in EnemyAiController / the engagement director), does not pick targets, and does nothing when enableUtilityBrain is off. |  |
| Brain | Toggle / archetype tests left nothing on disk | PASS | Resources/Combat profiles, Data/Enemies definitions and enemy prefabs hashed before and after: identical |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player weapons: each melee item vs enemy (swapped in place)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Audio | Player '2 Hander' swing plays a swish | PASS | 3 audio start(s) outside the hit windows over 4 swings: SfxPool_2:QP01 0005 Wind howling, SfxPool_3:QP01 0005 Wind howling, SfxPool_4:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee '2 Hander' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 28), hits 0, player right weapon Drawn_2_Hander | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Blade Of Chaos' swing plays a swish | PASS | 1 audio start(s) outside the hit windows over 4 swings: SfxPool_5:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Blade Of Chaos' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 21), hits 0, player right weapon InvectorWeapon_Blade Of Chaos | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Death Axe' swing plays a swish | PASS | 1 audio start(s) outside the hit windows over 4 swings: SfxPool_7:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Death Axe' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon Drawn_Death_Axe | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Spear of Fate' swing plays a swish | PASS | 3 audio start(s) outside the hit windows over 4 swings: SfxPool_9:QP01 0005 Wind howling, SfxPool_0:QP01 0005 Wind howling, SfxPool_1:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Spear of Fate' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 16), hits 0, player right weapon Drawn_Spear_of_Fate | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Sword of Fear' swing plays a swish | PASS | 1 audio start(s) outside the hit windows over 4 swings: SfxPool_2:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Sword of Fear' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 15), hits 0, player right weapon Drawn_Sword_of_Fear | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'weap2_sword' swing plays a swish | PASS | 3 audio start(s) outside the hit windows over 4 swings: SfxPool_3:QP01 0005 Wind howling, SfxPool_4:QP01 0005 Wind howling, SfxPool_5:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'weap2_sword' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon Drawn_weap2_sword | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'weap_two_handed' swing plays a swish | PASS | 2 audio start(s) outside the hit windows over 4 swings: SfxPool_6:QP01 0005 Wind howling, SfxPool_7:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'weap_two_handed' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 28), hits 0, player right weapon Drawn_Two_Handed_Sword | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Wood Axe' swing plays a swish | PASS | 2 audio start(s) outside the hit windows over 4 swings: SfxPool_8:QP01 0005 Wind howling, SfxPool_9:QP01 0005 Wind howling |  |
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
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: fires | FAIL | 0 trigger pull(s), rounds 30 -> 30 | no round consumed with this ammo type loaded |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: damages the enemy | WARN | enemy hp lost 0.0 (ammo type Ion) | no hit in the few shots fired: spread/accuracy can miss, too few shots to judge |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: hit rate over aimed shots (shots may miss by design) | INFO | 0/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 89), ammo accuracy 89, spread 1.8 deg (ammo 1.2), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: hit marks / FX | INFO | hit marks stamped 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: per-ammo FX rules (blood / burn / glow / explosive handling) | INFO | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=False; hit marks 0 -> 0, enemy hp lost 0.0 | no hit to evaluate the rules against |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Laser', ammoType Laser |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: fires | FAIL | 0 trigger pull(s), rounds 30 -> 30 | no round consumed with this ammo type loaded |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: damages the enemy | WARN | enemy hp lost 0.0 (ammo type Laser) | no hit in the few shots fired: spread/accuracy can miss, too few shots to judge |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: hit rate over aimed shots (shots may miss by design) | INFO | 0/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.5), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: hit marks / FX | INFO | hit marks stamped 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: per-ammo FX rules (blood / burn / glow / explosive handling) | INFO | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=False; hit marks 0 -> 0, enemy hp lost 0.0 | no hit to evaluate the rules against |
| Ammo | X (AmmoCycle) switches the loaded ammo type | FAIL | 3 ammo stack(s) on the Hot Cross utility slots; loaded DMAmmoFxProfile_Laser -> DMAmmoFxProfile_Laser -> DMAmmoFxProfile_Laser | X tap did not cycle: AmmoCycle owned by GameplayKeyboardShortcuts poll (needs drawn ranged weapon + Hot Cross focus) or the next type does not fit the weapon |
| Ammo | Ammo coverage | INFO | 2 weapon(s) tested with their default ammo (full: load, fill, fire, damage, marks, empty, reload); 4 ammo-specific case(s) (Quick: only ammo types with special FX rules, max 4), each once on a host weapon (load, 2 shots, damage, marks, per-ammo FX rules); no weapon x ammo matrix. Mining tool excluded: it runs on Plasma Fuel charge. |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player rig: enemy ranged shot hits a body bone + mark

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Player rig | Player hitbox rig is built and active (per-bone hitboxes on layer DMHitbox) | PASS | rig present, active True, active rigs 1, hitboxes 18, not on the DMHitbox layer 0, body type Humanoid |  |
| Player rig | Enemy ranged shot ('survival_rifle') resolves to a player body hitbox | PASS | player hp lost 15.9, shots/projectiles 1, rig marks 11 -> 12, active mark objects 1: on a bone 1, on the root/capsule 0, parents [Spine01], range 7m, states Wander/Attack |  |
| Player rig | Player hit marks are small and shallow (not head-sized, not projected through the body) | PASS | 2 projector(s): largest footprint 0.100m, deepest projection 0.160m (profile playerMarkSizeScale 0.4, playerMarkProjectionDepth 0.16); a 0.2m enemy mark would cover most of a head |  |
| Player rig | Player hit marks are removed automatically after ~10s (fade, then clear) | PASS | 1 mark object(s) active right after the shots, 0 still active 11.0s later (playerMarkLifetimeSeconds 10, fade 1.5s) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Aim | Aim follows the moving enemy: chest | FAIL | 3 shots, 0 fired, aim error before firing avg 1.8deg; crosshair hitbox zone none (bone none); hit-mark bones none; avg damage 0.0 (xn/a chest) | crosshair never rested on a DM hitbox of this body part: aim tracking lagging the strafing enemy, or the enemy has no hitbox rig/bone |
| Aim | Aim follows the moving enemy: head | FAIL | 3 shots, 0 fired, aim error before firing avg 1.0deg; crosshair hitbox zone none/Head (bone none/VBOT_:Head); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Aim follows the moving enemy: arm | FAIL | 3 shots, 0 fired, aim error before firing avg 1.9deg; crosshair hitbox zone Arm/none/Torso (bone VBOT_:RightArm/none/VBOT_:Spine2); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Aim follows the moving enemy: leg | FAIL | 3 shots, 0 fired, aim error before firing avg 1.0deg; crosshair hitbox zone Torso/none (bone VBOT_:Hips/none); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Aim follows the moving enemy: foot | FAIL | 3 shots, 0 fired, aim error before firing avg 1.5deg; crosshair hitbox zone none/Leg (bone none/VBOT_:RightFoot); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Body-part damage table (for later headshot/limb multipliers) | INFO | chest: zone none, avg dmg 0.0, xn/a vs chest / head: zone none/Head, avg dmg 0.0, xn/a vs chest / arm: zone Arm/none/Torso, avg dmg 0.0, xn/a vs chest / leg: zone Torso/none, avg dmg 0.0, xn/a vs chest / foot: zone none/Leg, avg dmg 0.0, xn/a vs chest | damage multipliers per zone come from DMCombatBodyPartUtility.ApplyZoneDamageMultiplier (DM_EnemyHitMarkProfile); equal ratios here mean no zone multiplier is configured |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Behavior tweaks: over / under-exaggerated enemy values (runtime only)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Behavior | How the tweaks are applied | INFO | runtime only: values are written to the live component instances of a freshly spawned enemy (destroyed after each variation) and profile tweaks go through in-memory clones swapped in for the global Resources profile and restored afterwards; a disk hash of the profiles, definitions and enemy prefabs is compared at the end. Hit-mark / effect budgets are not covered here (they are visual budgets, see the hit-mark rows). |  |
| Behavior | Normal behaviour (reference): player 9m in front, 8s | INFO | notice 0.1s, chase 0.1s, first attack 1.4s, attacks 3, dmg 81, min dist 1.2m, path 6.7m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:155/Hold:10, states Hold/Chase/Attack |  |
| Behavior | Aggression / attack rate: high (hi = attacks much faster) | PASS | EnemyCombat.attackCooldown 1.4 -> 0.35, EnemyCombat.meleeRecoveryPauseMin 0.6 -> 0.15, EnemyCombat.meleeRecoveryPauseMax 1.8 -> 0.45; notice 0.1s, chase 0.1s, first attack 1.7s, attacks 3, dmg 84, min dist 1.4m, path 6.7m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:87/Hold:12/None:1, states Hold/Chase/Attack |  |
| Behavior | Aggression / attack rate: low (hi = attacks much faster) | PASS | EnemyCombat.attackCooldown 1.4 -> 4.2, EnemyCombat.meleeRecoveryPauseMin 0.6 -> 1.8, EnemyCombat.meleeRecoveryPauseMax 1.8 -> 5.4; notice 0.1s, chase 0.1s, first attack 1.8s, attacks 2, dmg 55, min dist 1.2m, path 6.9m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:119/Hold:8, states Hold/Chase/Attack |  |
| Behavior | Aggression / attack rate: suggested safe range | PASS | both x0.25 and x3 of the normal values stayed healthy: safe range at least x3 .. x0.25 |  |
| Behavior | Standoff distance: high (hi = stands further off) | PASS | EnemyAiController.attackStandoffFraction 0.62 -> 0.992, EnemyAiController.combatRingSlotSpread 42 -> 1.1, EnemyAiController.minCombatSeparation 0.95 -> 1.1; notice 0.1s, chase 0.1s, first attack 1.5s, attacks 3, dmg 82, min dist 1.3m, path 6.8m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:109/Hold:13, states Hold/Chase/Attack |  |
| Behavior | Standoff distance: low (hi = stands further off) | PASS | EnemyAiController.attackStandoffFraction 0.62 -> 0.248, EnemyAiController.combatRingSlotSpread 42 -> 1.1, EnemyAiController.minCombatSeparation 0.95 -> 0.38; notice 0.1s, chase 0.1s, first attack 1.7s, attacks 3, dmg 83, min dist 1.2m, path 6.9m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:98/Hold:9, states Hold/Chase/Attack |  |
| Behavior | Standoff distance: suggested safe range | PASS | both x1.6 and x0.4 of the normal values stayed healthy: safe range at least x0.4 .. x1.6 |  |
| Behavior | Chase / lose-target timings: high (hi = never gives up, lo = gives up at once) | PASS | EnemyAiController.loseTargetDelay 4 -> 16, EnemyAiController.maxChaseDuration 6 -> 24, EnemyAiController.chaseSpeedMultiplier 0.88 -> 3.52; notice 0.1s, chase 0.1s, first attack 0.4s, attacks 3, dmg 110, min dist 1.2m, path 5.7m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, never lost the target within 14s, brain Press:145/Hold:11, states Hold/Chase/Attack |  |
| Behavior | Chase / lose-target timings: low (hi = never gives up, lo = gives up at once) | PASS | EnemyAiController.loseTargetDelay 4 -> 0.8, EnemyAiController.maxChaseDuration 6 -> 1.2, EnemyAiController.chaseSpeedMultiplier 0.88 -> 0.176; notice 0.1s, chase 0.1s, first attack 2.9s, attacks 3, dmg 54, min dist 1.2m, path 7.1m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, lost target after 0.8s, brain Press:157/Hold:15, states Hold/Chase/ReturnHome/Attack |  |
| Behavior | Chase / lose-target timings: suggested safe range | PASS | both x4 and x0.2 of the normal values stayed healthy: safe range at least x0.2 .. x4 |  |
| Behavior | Brain archetype weights: high (hi = press x3 / defend x0.3 / retreat 0, lo = press x0.2 / defend x3 / retreat x3) | PASS | archetype rows: press x3, defend x0.3, retreat x0; notice 0.1s, chase 0.1s, first attack 1.0s, attacks 4, dmg 82, min dist 1.2m, path 5.0m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:168, states Chase/Attack |  |
| Behavior | Brain archetype weights: low (hi = press x3 / defend x0.3 / retreat 0, lo = press x0.2 / defend x3 / retreat x3) | PASS | archetype rows: press x0.2, defend x3, retreat x3; notice 0.1s, chase 0.1s, first attack 1.1s, attacks 2, dmg 131, min dist 1.2m, path 4.9m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:150/Defend:37, states Chase/Attack/Defensive |  |
| Behavior | Brain archetype weights: suggested safe range | PASS | both x3 and x0.2 of the normal values stayed healthy: safe range at least x0.2 .. x3 |  |
| Behavior | Tweaks left nothing on disk (profiles, definitions, enemy prefabs hashed before/after) | PASS | identical |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | FAIL | code present: True; 13 PASS / 3 FAIL / 0 warn of 16 runtime row(s) | swing rows still silent for: '2 Hander' swing plays a swish, 'Death Axe' swing plays a swish, 'Spear of Fate' swing plays a swish |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Axe_Droid.prefab` - definition `Axe_Droid` - melee `Spear of Fate` / ranged `-` - audited in 29s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Axe_Droid, melee=Spear of Fate (dmg 16), ranged=- (dmg -), preferRanged=False, def.attackDamage=12, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 32ms, 794 components, frame avg 46.9ms max 2853.3ms (274 frames), GC collections 1, managed delta -83412KB |  |
| AI | No circling while unaware (player 50m away, 9s) | PASS | yaw turned 229deg, path 8.7m, net 3.0m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.7s (at 2.3m, weapon Spear of Fate), first player damage 2.2s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 20.0 hp in 2.2s using Spear of Fate |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 20.1 (item dmg 16), attacks 1, hitbox-active frames 10 (unarmed 0), hit events 1, sub-1hp chip 3.5, first hit 1.5s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Spear of Fate': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 1 (100 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 20.1; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 219.5 (-30.5); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 219.5 -> 219.5; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-2 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.7s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 10 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/corrupt_patrol_android.prefab` - definition `corrupt_patrol_android` - melee `Death Axe` / ranged `sci_fi_pistol` - audited in 29s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=corrupt_patrol_android, melee=Death Axe (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=350 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 28ms, 736 components, frame avg 34.3ms max 73.9ms (263 frames), GC collections 0, managed delta 12492KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 136deg, path 16.0m, net 8.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 5m in front) | PASS | notice 0.1s, chase 0.1s, first attack 0.7s (at 2.3m, weapon Death Axe), first player damage 1.3s, attacks 1, states Wander/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 24.0 hp in 1.3s using Death Axe |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 24.0 (item dmg 18), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 3.5, first hit 1.4s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Death Axe': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 24.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 13.8 (item dmg 14), shots/projectiles 1, attack-state frames 36, attacks 1, first hit 2.8s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.3m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Death_Axe] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 350.0 -> 267.2 (-82.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 267.2 -> 267.2; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | FAIL | loot bag held 0 entries, TryLootAll -> None, left 0; definition random loot count 0-2 | bag spawned but nothing could be looted (empty entries / TryLootAll refused) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 18 distinct clips seen in total |  |
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

### GreySpire Soldier

Prefab `Assets/_Project/Prefabs/Combat/Enemies/GreySpire Soldier.prefab` - definition `GreySpire Soldier` - melee `weap_two_handed` / ranged `-` - audited in 25s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=GreySpire Soldier, melee=weap_two_handed (dmg 28), ranged=- (dmg -), preferRanged=False, def.attackDamage=8, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 28ms, 797 components, frame avg 37.0ms max 95.0ms (244 frames), GC collections 1, managed delta -270672KB |  |
| AI | No circling while unaware (player 55m away, 9s) | PASS | yaw turned 174deg, path 8.3m, net 1.9m, states Wander |  |
| AI | Notice / chase / attack timing (player 8m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.9s (at 2.1m, weapon weap_two_handed), first player damage 2.5s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 38.0 hp in 2.5s using weap_two_handed |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 38.5 (item dmg 28), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 1.7, first hit 0.9s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap_two_handed': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 38.5; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.2m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 226.6 (-23.4); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 226.6 -> 226.6; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 2 entries, TryLootAll -> Granted, left 0, 2 granted item(s) removed again from the audit inventory; definition random loot count 2-2 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 28s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 28ms, 787 components, frame avg 36.5ms max 65.5ms (247 frames), GC collections 0, managed delta 13140KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 174deg, path 11.7m, net 5.9m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.3s, chase 0.3s, first attack 1.8s (at 2.2m, weapon weap2_sword), first player damage 2.4s, attacks 1, states Patrol/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.4s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 10 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.4s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.2m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 15.3 (item dmg 14), shots/projectiles 1, attack-state frames 43, attacks 1, first hit 2.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 172.9 (-27.1); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 172.9 -> 172.9; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | FAIL | loot bag held 0 entries, TryLootAll -> None, left 0; definition random loot count 0-1 | bag spawned but nothing could be looted (empty entries / TryLootAll refused) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.29s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### Humanoid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humanoid.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 29s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 34ms, 696 components, frame avg 36.1ms max 68.4ms (250 frames), GC collections 0, managed delta 14828KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 192deg, path 6.5m, net 0.5m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.3s, chase 0.3s, first attack 1.8s (at 2.1m, weapon weap2_sword), first player damage 2.3s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.3s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.2 (item dmg 18), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 3.5, first hit 1.6s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.2; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.9 (item dmg 14), shots/projectiles 0, attack-state frames 45, attacks 1, first hit 2.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 1 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.1m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 272.2 (-27.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 272.2 -> 272.2; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### HumanoidEnemy_Invector

Prefab `Assets/_Project/Prefabs/Combat/Enemies/HumanoidEnemy_Invector.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 31s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 32ms, 722 components, frame avg 49.0ms max 93.2ms (184 frames), GC collections 0, managed delta 31968KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 144deg, path 10.6m, net 5.4m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.3s, chase 0.3s, first attack 2.3s (at 2.2m, weapon weap2_sword), first player damage 2.9s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.9s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 47.3 (item dmg 18), attacks 1, hitbox-active frames 4 (unarmed 0), hit events 1, sub-1hp chip 4.5, first hit 2.4s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 47.3; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.1 (item dmg 14), shots/projectiles 1, attack-state frames 43, attacks 1, first hit 2.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 274.7 (-25.3); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 274.7 -> 274.7; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | FAIL | loot bag held 0 entries, TryLootAll -> None, left 0; definition random loot count 0-1 | bag spawned but nothing could be looted (empty entries / TryLootAll refused) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 5 clip(s) observed playing; 17 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Hybrid Droid.prefab` - definition `FRED_Android` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 32s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=FRED_Android, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 33ms, 696 components, frame avg 34.6ms max 59.2ms (260 frames), GC collections 0, managed delta 9900KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 165deg, path 13.5m, net 7.5m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 2.0s (at 2.2m, weapon weap2_sword), first player damage 5.9s, attacks 2, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 5.9s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 10 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.8s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.5 (item dmg 14), shots/projectiles 1, attack-state frames 44, attacks 1, first hit 2.9s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.1m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 270.5 (-29.5); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 270.5 -> 270.5; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 20 distinct clips seen in total |  |
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

### Robot

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `Robot_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 29s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Robot_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=15, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 33ms, 698 components, frame avg 35.1ms max 60.0ms (257 frames), GC collections 0, managed delta 13456KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 167deg, path 8.7m, net 2.5m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.5s (at 2.2m, weapon weap2_sword), first player damage 3.1s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 3.1s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 16 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.2s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 15.0 (item dmg 14), shots/projectiles 1, attack-state frames 44, attacks 1, first hit 3.0s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 470.7 (-29.3); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 470.7 -> 470.7; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Robot |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.4s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 18 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/The_Evil_One.prefab` - definition `The_Evil_One` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 26s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=The_Evil_One, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=True, def.attackDamage=12, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 37ms, 707 components, frame avg 35.4ms max 60.2ms (255 frames), GC collections 0, managed delta 14588KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 157deg, path 15.8m, net 7.4m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.7s (at 9.2m, weapon sci_fi_pistol), first player damage 1.8s, attacks 1, states Wander/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 16.0 hp in 1.8s using sci_fi_pistol |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 9 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.9s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; no vMeleeWeapon on the drawn/bound weapon |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.55s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.2m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.9 (item dmg 14), shots/projectiles 2, attack-state frames 41, attacks 2, first hit 1.9s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.65 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 2, damage events 1 (50 %) vs about 35 % expected from definition missRate 0.65 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 101 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 2 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 471.9 (-28.1); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 471.9 -> 471.9; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-2 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 25 distinct clips seen in total |  |
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
