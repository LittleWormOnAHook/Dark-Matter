# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-13 22:04:10 -> 2026-10-13 22:15:07 (aborted: play mode exited before the run completed)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 3; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 4
- Unit store: 37 stored unit(s); stamps audit 948B5E game 65A8B6 gear 11046A; skip-unchanged is ON (Quick mode)
- Weapon matrix: 11 weapon items equipped one at a time (spawn override) on HumanoidEnemy_Invector; plus one dual-slot override case
- Mode quick: 25 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **quick**, wall time 10m57s

**4 enemies audited - 1 FAIL, 4 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 12 weapon cases + 11 scenarios): 156 PASS, 21 FAIL, 25 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 111 change(s).

- NEW enemy|Humadroid|perf = WARN
- NEW enemy|Humadroid|melee.miss = INFO
- NEW enemy|Humadroid|ranged.miss = INFO
- CHANGED enemy|Humadroid|hitmarks: WARN -> PASS
- NEW enemy|Humadroid|loot = PASS
- NEW enemy|Robot|perf = WARN
- NEW enemy|Robot|melee.miss = INFO
- NEW enemy|Robot|ranged.miss = INFO
- NEW enemy|Robot|loot = PASS
- NEW enemy|SwordMaster|cfg = PASS
- NEW enemy|SwordMaster|silent.missingscripts = PASS
- NEW enemy|SwordMaster|silent.materials = PASS
- NEW enemy|SwordMaster|silent.animator = PASS
- NEW enemy|SwordMaster|silent.hitboxes = PASS
- NEW enemy|SwordMaster|perf = WARN
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
- NEW enemy|SwordMaster|player.swing = PASS
- NEW enemy|SwordMaster|hitmarks = PASS
- NEW enemy|SwordMaster|loot = PASS
- NEW enemy|SwordMaster|death = PASS
- NEW enemy|SwordMaster|anim = INFO
- NEW enemy|SwordMaster|console = PASS
- NEW enemy|The_Evil_One|perf = WARN
- NEW enemy|The_Evil_One|melee.miss = INFO
- CHANGED enemy|The_Evil_One|ranged.damage: PASS -> FAIL
- NEW enemy|The_Evil_One|ranged.miss = INFO
- NEW enemy|The_Evil_One|loot = PASS
- CHANGED weapon|2 Hander|audio.swing.2 Hander: PASS -> FAIL
- NEW weapon|2 Hander|w.miss = INFO
- CHANGED weapon|Blade Of Chaos|audio.swing.Blade Of Chaos: FAIL -> PASS
- NEW weapon|Blade Of Chaos|w.miss = INFO
- CHANGED weapon|Death Axe|audio.hitplayer.Death Axe: FAIL -> PASS
- CHANGED weapon|Death Axe|w.damage: FAIL -> PASS
- NEW weapon|Death Axe|w.miss = INFO
- NEW weapon|Spear of Fate|w.miss = INFO
- CHANGED weapon|Sword of Fear|audio.hitplayer.Sword of Fear: FAIL -> PASS
- NEW weapon|Sword of Fear|w.miss = INFO
- NEW weapon|weap2_sword|w.miss = INFO
- CHANGED weapon|weap_two_handed|audio.swing.weap_two_handed: PASS -> FAIL
- NEW weapon|weap_two_handed|w.miss = INFO
- CHANGED weapon|Wood Axe|audio.swing.Wood Axe: PASS -> FAIL
- NEW weapon|Wood Axe|w.miss = INFO
- CHANGED weapon|DM_Mining_Tool|w.equip: PASS -> FAIL
- CHANGED weapon|DM_Mining_Tool|w.damage: WARN -> FAIL
- NEW weapon|DM_Mining_Tool|w.miss = INFO
- CHANGED weapon|sci_fi_pistol|w.damage: PASS -> FAIL
- NEW weapon|sci_fi_pistol|w.miss = INFO
- CHANGED weapon|survival_rifle|w.damage: PASS -> FAIL
- NEW weapon|survival_rifle|w.miss = INFO
- CHANGED scenario|Audio: swing / hit sound wiring (static)|audio.swing.callers: FAIL -> PASS
- CHANGED scenario|Audio: swing / hit sound wiring (static)|audio.hit.callers: WARN -> PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.inputdiag = INFO
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.W: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.S: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.A: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.D: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.run: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.sprint: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.jump: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.dodge: PASS -> FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.climb = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.systems = INFO
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.lmb: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.rmb: PASS -> FAIL
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.switch: PASS -> WARN
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.walk: PASS -> WARN
- CHANGED scenario|Block + parry: player defends against enemy melee|blk.hold: PASS -> FAIL
- CHANGED scenario|Block + parry: player defends against enemy melee|blk.stagger: INFO -> WARN
- CHANGED scenario|Block + parry: player defends against enemy melee|parry.window: PASS -> WARN
- NEW scenario|Block + parry: player defends against enemy melee|parry.counter = INFO
- ... 31 more

## Timing

Wall time 657s (11.0 min), mode quick. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Humadroid | enemy | 35.0 | ran |
| Robot | enemy | 34.8 | ran |
| SwordMaster | enemy | 34.6 | ran |
| The_Evil_One | enemy | 51.4 | ran |
| 2 Hander | weapon | 3.7 | ran |
| Blade Of Chaos | weapon | 6.1 | ran |
| Death Axe | weapon | 2.7 | ran |
| Spear of Fate | weapon | 4.1 | ran |
| Sword of Fear | weapon | 4.5 | ran |
| weap2_sword | weapon | 3.4 | ran |
| weap_two_handed | weapon | 2.5 | ran |
| Wood Axe | weapon | 2.5 | ran |
| DM_Mining_Tool | weapon | 18.7 | ran |
| sci_fi_pistol | weapon | 17.7 | ran |
| survival_rifle | weapon | 17.6 | ran |
| OVERRIDE both slots | weapon | 4.4 | ran |
| Camera: faces forward after teleports, zoom in/out | scenario | 11.1 | ran |
| Equip: weapon survives a teleport | scenario | 6.7 | ran |
| Audio: swing / hit sound wiring (static) | scenario | 0.8 | ran |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | scenario | 85.5 | ran |
| Block + parry: player defends against enemy melee | scenario | 56.0 | ran |
| Block: enemy defends against player swings | scenario | 12.4 | ran |
| Movement: retreat, push, flank, backstab, lose target | scenario | 34.6 | ran |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | scenario | 51.7 | ran |
| Multi-enemy: FRED x3 and x5 | scenario | 40.1 | ran |
| Brain: orbit, toggles, archetypes | scenario | 0.0 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | setup | 85.8 | 1020 | 84.1 | 1020.4 | 0.79 | 0.00 | 27.59 |
| scenario|Brain: orbit, toggles, archetypes | setup | 82.3 | 1077 | 75.3 | 441.6 | 0.80 | 0.00 | 20.28 |
| scenario|Block + parry: player defends against enemy melee | setup | 57.3 | 1069 | 53.6 | 332.0 | 0.58 | 0.00 | 14.87 |
| scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | setup | 52.0 | 1218 | 42.7 | 363.7 | 0.45 | 0.00 | 13.63 |
| scenario|Multi-enemy: FRED x3 and x5 | setup | 41.7 | 554 | 75.4 | 810.8 | 0.83 | 0.00 | 22.96 |
| scenario|Movement: retreat, push, flank, backstab, lose target | setup | 34.9 | 505 | 69.0 | 117.3 | 0.70 | 0.00 | 15.91 |
| weapon|DM_Mining_Tool | setup | 18.7 | 370 | 50.8 | 134.9 | 0.60 | 0.00 | 18.16 |
| enemy|The_Evil_One | ranged test | 18.3 | 314 | 59.0 | 274.6 | 0.65 | 0.00 | 19.97 |
| weapon|sci_fi_pistol | setup | 17.7 | 349 | 50.7 | 282.6 | 0.57 | 0.00 | 18.93 |
| weapon|survival_rifle | setup | 17.6 | 316 | 55.7 | 134.0 | 0.61 | 0.00 | 19.17 |
| scenario|Block: enemy defends against player swings | setup | 13.7 | 320 | 43.0 | 118.8 | 0.46 | 0.00 | 13.21 |
| scenario|Camera: faces forward after teleports, zoom in/out | setup | 11.3 | 192 | 59.3 | 992.9 | 0.59 | 0.00 | 17.28 |
| enemy|The_Evil_One | player->enemy | 9.8 | 202 | 48.2 | 868.2 | 0.22 | 0.00 | 10.90 |
| enemy|Humadroid | unaware 9s (+perf) | 9.5 | 154 | 61.8 | 145.9 | 0.71 | 0.00 | 18.16 |
| enemy|Robot | unaware 9s (+perf) | 9.4 | 162 | 58.4 | 290.4 | 0.63 | 0.00 | 18.43 |
| enemy|The_Evil_One | unaware 9s (+perf) | 9.3 | 160 | 58.2 | 140.2 | 0.64 | 0.00 | 20.07 |
| enemy|SwordMaster | unaware 9s (+perf) | 9.2 | 169 | 54.4 | 124.3 | 0.58 | 0.00 | 17.12 |
| enemy|SwordMaster | death/loot | 7.2 | 117 | 61.8 | 362.8 | 0.57 | 0.00 | 17.64 |
| enemy|Humadroid | death/loot | 7.0 | 101 | 68.7 | 160.5 | 0.74 | 0.00 | 19.78 |
| scenario|Equip: weapon survives a teleport | setup | 7.0 | 110 | 62.8 | 185.6 | 0.62 | 0.00 | 19.35 |
| enemy|The_Evil_One | death/loot | 7.0 | 126 | 55.1 | 159.6 | 0.60 | 0.00 | 19.88 |
| enemy|Robot | death/loot | 6.8 | 128 | 54.1 | 174.9 | 0.53 | 0.00 | 16.73 |
| enemy|SwordMaster | ranged test | 6.2 | 96 | 66.1 | 278.2 | 0.66 | 0.00 | 20.17 |
| weapon|Blade Of Chaos | setup | 6.1 | 76 | 80.2 | 341.9 | 0.78 | 0.00 | 22.23 |
| enemy|Humadroid | player->enemy | 5.7 | 66 | 88.3 | 170.8 | 0.96 | 0.00 | 22.60 |
| enemy|SwordMaster | player->enemy | 5.4 | 54 | 102.0 | 168.2 | 1.10 | 0.00 | 26.21 |
| enemy|Robot | player->enemy | 5.1 | 57 | 90.1 | 148.3 | 0.99 | 0.00 | 22.36 |
| enemy|Humadroid | notice/chase/attack | 4.6 | 87 | 52.6 | 128.7 | 0.93 | 0.00 | 18.25 |
| enemy|Robot | ranged test | 4.6 | 76 | 62.6 | 297.0 | 0.68 | 0.00 | 19.05 |
| weapon|Sword of Fear | setup | 4.5 | 57 | 78.3 | 337.4 | 0.89 | 0.00 | 22.21 |

Whole run (ran units only): 628s over 9940 frames, avg frame 64.2 ms, physics 0.66 ms, render 0.00 ms, scripts 18.86 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 100 | FAIL | Ranged 'sci_fi_pistol' damages the player | The_Evil_One | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 2 | 100 | FAIL | Ranged 'DM_Mining_Tool' damages the player | DM_Mining_Tool | ItemData rangedDamage = 0 / weapon never became the active weapon (see equip row) / ItemData 'DM_Mining_Tool'.rangedDamage = 0 / attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 3 | 100 | FAIL | Ranged 'sci_fi_pistol' damages the player | sci_fi_pistol, survival_rifle | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 4 | 75 | FAIL | Equips on the template (spawn override) | DM_Mining_Tool | ConfigureSpawnLoadout + TrySwitchTo did not make this the active weapon: the ItemData has no equip prefab / the template has no matching Drawn_ slot for it; ItemData rangedDamage = 0 | Inspect the measured value and cause on the detail row. |
| 5 | 70 | FAIL | Holding block negates enemy melee damage | Block + parry: player defends against enemy melee | player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 6 | 45 | WARN | Enemy guard-break stagger after a blocked hit | Block + parry: player defends against enemy melee | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 7 | 45 | WARN | Enemy block reduces player melee damage | Block: enemy defends against player swings | player swings registered no hits on the open enemy, so the comparison is inconclusive (input focus, reach, or the player weapon) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 8 | 30 | WARN | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | Multi-enemy: FRED x3 and x5 | 94/249 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Add enemy separation and make the attack token rotate between enemies. |
| 9 | 20 | FAIL | '2 Hander' swing plays a swish | 2 Hander | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 10 | 20 | FAIL | 'Death Axe' swing plays a swish | Death Axe | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 11 | 20 | FAIL | 'weap_two_handed' swing plays a swish | weap_two_handed | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 12 | 20 | FAIL | 'Wood Axe' swing plays a swish | Wood Axe | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 13 | 20 | FAIL | W moves the player forward (camera direction) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move action not reaching the controller: PlayerInput disabled, move locked, or PioneerPlayerInputBinder.HandleAction('Move') not wired | Inspect the measured value and cause on the detail row. |
| 14 | 20 | FAIL | S moves the player back | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (S) produced no backward displacement | Inspect the measured value and cause on the detail row. |
| 15 | 20 | FAIL | A strafes left | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (A) produced no leftward displacement | Inspect the measured value and cause on the detail row. |
| 16 | 20 | FAIL | D strafes right | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (D) produced no rightward displacement | Inspect the measured value and cause on the detail row. |
| 17 | 20 | FAIL | W run speed (reference for sprint) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | player did not move | Inspect the measured value and cause on the detail row. |
| 18 | 20 | FAIL | Shift + W sprints faster than W | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | sprint no faster than run (0.0m): Sprint action not wired, stamina 0, or sprint speed equals run speed | Inspect the measured value and cause on the detail row. |
| 19 | 20 | FAIL | Space jumps | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked; state before the press: isGrounded True, isJumping False, customAction False, isCrouching False, stamina 1000 | Inspect the measured value and cause on the detail row. |
| 20 | 20 | FAIL | Q dodge / roll | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dodge action did not move the player: TryDodgeRollFromInput refused (stamina, cooldown, no roll clip) | Inspect the measured value and cause on the detail row. |
| 21 | 20 | FAIL | Left click swings the drawn melee weapon | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | melee Attack input not received: drawn melee tap/hold logic (UpdateDrawnMeleeCharge) did not start a swing | Inspect the measured value and cause on the detail row. |
| 22 | 20 | FAIL | Right click blocks (melee drawn) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Block input (Mouse rightButton / Block action) did not set isBlocking: stamina 0, customAction, not grounded, or still attacking | Inspect the measured value and cause on the detail row. |
| 23 | 20 | WARN | Player circles behind the enemy: enemy turns to face | Movement: retreat, push, flank, backstab, lose target | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 24 | 20 | WARN | Hit from behind vs from the front (player melee, AI off) | Movement: retreat, push, flank, backstab, lose target | swings did not land from one side (reach/facing/input), comparison inconclusive | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 25 | 20 | FAIL | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks | swing rows still silent for: '2 Hander' swing plays a swish, 'Death Axe' swing plays a swish, 'weap_two_handed' swing plays a swish, 'Wood Axe' swing plays a swish | Inspect the measured value and cause on the detail row. |
| 26 | 15 | WARN | A tap inside the parry window parries (and an early/late tap does not) | Block + parry: player defends against enemy melee | no sampled tap produced a parry: enemy attack timing varies between swings (windup from the baseline swing may not match), or the parry window/stagger in the profile differs from what the rows show | Inspect the measured value and cause on the detail row. |
| 27 | -5 | WARN | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | Humadroid | average editor frame 61.8ms with one enemy active | Inspect the measured value and cause on the detail row. |
| 28 | -5 | WARN | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | Robot | average editor frame 58.4ms with one enemy active | Inspect the measured value and cause on the detail row. |
| 29 | -5 | WARN | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | SwordMaster | average editor frame 54.4ms with one enemy active | Inspect the measured value and cause on the detail row. |
| 30 | -5 | WARN | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | The_Evil_One | average editor frame 58.2ms with one enemy active | Inspect the measured value and cause on the detail row. |
| 31 | -5 | WARN | Camera zooms in and out | Camera: faces forward after teleports, zoom in/out | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state | Inspect the measured value and cause on the detail row. |
| 32 | -5 | WARN | Left Ctrl crouches | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) | Inspect the measured value and cause on the detail row. |
| 33 | -5 | WARN | Left Alt dash | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dash did not move the player even though the skill gate is unlocked for this run (see ctl.systems row): DMDashController disabled, on cooldown, stamina, or the Dash action not bound to Left Alt | Inspect the measured value and cause on the detail row. |
| 34 | -5 | WARN | Hold Space after jumping boosts (jetpack) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | no sustained lift although the jetpack skill gate is unlocked for this run (see ctl.systems row): fuel 0, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player | Inspect the measured value and cause on the detail row. |
| 35 | -5 | WARN | Tab (SwitchWeapon) cycles the weapon focus | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Tab tap did not change focus: only one weapon on the hotbar, Hot Cross not polling, or tap/hold threshold (hold arms) not met | Inspect the measured value and cause on the detail row. |
| 36 | -5 | WARN | Half-tilt stick walks slower than a run | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | walk speed not distinct from run (0.0m): analog Move magnitude ignored by the locomotion speed | Inspect the measured value and cause on the detail row. |
| 37 | -5 | WARN | Approach with W: enemy notices | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) | Inspect the measured value and cause on the detail row. |
| 38 | -5 | WARN | Run away with S: enemy pursues | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy did not chase a retreating player: chase blocked (standoff/hold token, ranged keep-distance) or target lost immediately | Inspect the measured value and cause on the detail row. |
| 39 | -5 | WARN | Strafe around the enemy: it turns to face the player | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy did not keep facing a strafing player: turn speed too slow, turn-in-place gated, or enemy in a state that ignores the target | Inspect the measured value and cause on the detail row. |
| 40 | -5 | WARN | Retreat then re-engage: enemy fights back | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy never attacked a player standing next to it: attack cooldown/token starvation or stagger lock | Inspect the measured value and cause on the detail row. |
| 41 | -5 | WARN | Jump away: player clears distance | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | jump did not leave the ground (Jump input / grounded) | Inspect the measured value and cause on the detail row. |
| 42 | -5 | WARN | Jetpack escape (hold Space + S) | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump | Inspect the measured value and cause on the detail row. |
| 43 | -5 | WARN | 5 FREDs at once for 18s: token rotation, spacing, no stacking/orbiting | Multi-enemy: FRED x3 and x5 | 130/232 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |
| 44 | -5 | WARN | 3 FREDs, baseline (brain on, director on) | Brain: orbit, toggles, archetypes | 48/176 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |
| 45 | -5 | WARN | 3 FREDs, legacyRingSlotRotation=true | Brain: orbit, toggles, archetypes | 76/176 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Humadroid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Robot | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| SwordMaster | Blade Of Chaos | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| The_Evil_One | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | **FAIL** | PASS | PASS | PASS | PASS |

## Weapons that deal NO damage (headline)

- **The_Evil_One** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 294, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.65. **Probable cause:** attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised)
- **Weapon matrix: DM_Mining_Tool** (ranged): player hp lost 0.0 (item dmg 0), shots/projectiles 0, attack-state frames 340, attacks 0, first hit never, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** ItemData rangedDamage = 0 | weapon never became the active weapon (see equip row) | ItemData 'DM_Mining_Tool'.rangedDamage = 0 | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised)
- **Weapon matrix: sci_fi_pistol** (ranged): player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 332, attacks 0, first hit never, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised)
- **Weapon matrix: survival_rifle** (ranged): player hp lost 0.0 (item dmg 18), shots/projectiles 0, attack-state frames 299, attacks 0, first hit never, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0..... **Probable cause:** attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised)

## Weapon matrix (every weapon item on the template, spawn override)

12 cases, 8 FAIL. Each weapon is equipped on `HumanoidEnemy_Invector` through ConfigureSpawnLoadout (the spawner path) and made to attack the player.

| Weapon | Kind | Equips | Damages player | One drawn | Measured | Probable cause |
|---|---|---|---|---|---|---|
| 2 Hander | melee | PASS | PASS | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 4, hit events 1, sub-1hp chip 0.0, first hit 0.2s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer I... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Blade Of Chaos | melee | PASS | PASS | PASS | player hp lost 30.0 (item dmg 21), attacks 3, hitbox-active frames 26, hit events 1, sub-1hp chip 0.0, first hit 3.6s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hi... |  |
| Death Axe | melee | PASS | PASS | PASS | player hp lost 24.0 (item dmg 18), attacks 1, hitbox-active frames 4, hit events 1, sub-1hp chip 0.9, first hit 0.3s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Spear of Fate | melee | PASS | PASS | PASS | player hp lost 20.0 (item dmg 16), attacks 1, hitbox-active frames 16, hit events 1, sub-1hp chip 0.0, first hit 1.5s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitB... |  |
| Sword of Fear | melee | PASS | PASS | PASS | player hp lost 38.2 (item dmg 15), attacks 1, hitbox-active frames 10, hit events 2, sub-1hp chip 2.2, first hit 1.0s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitB... |  |
| weap2_sword | melee | PASS | PASS | PASS | player hp lost 47.0 (item dmg 18), attacks 1, hitbox-active frames 5, hit events 1, sub-1hp chip 0.0, first hit 1.1s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: l... |  |
| weap_two_handed | melee | PASS | PASS | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 3, hit events 1, sub-1hp chip 0.0, first hit 0.2s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Wood Axe | melee | PASS | PASS | PASS | player hp lost 31.0 (item dmg 26), attacks 1, hitbox-active frames 2, hit events 1, sub-1hp chip 0.0, first hit 0.2s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer I... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| DM_Mining_Tool | ranged | **FAIL** | **FAIL** | PASS | player hp lost 0.0 (item dmg 0), shots/projectiles 0, attack-state frames 340, attacks 0, first hit never, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRang... | ConfigureSpawnLoadout + TrySwitchTo did not make this the active weapon: the ItemData has no equip prefab / the template has no matching Drawn_ slot for it; ItemData rangedDamage = 0 / ItemData rangedDamage = 0 / weapon never became the active weapon (see equip row) / ItemData 'DM_Mining_Tool'.rangedDamage = 0 / attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| sci_fi_pistol | ranged | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 332, attacks 0, first hit never, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRan... | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| survival_rifle | ranged | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 18), shots/projectiles 0, attack-state frames 299, attacks 0, first hit never, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageR... | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| OVERRIDE both slots | dual-slot spawn override | - | - | PASS | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)): PASS ; Starts with the ranged override drawn (prefer-ranged on, start-melee off): PASS ; Switch to the melee override vi... |  |

### Weapon: 2 Hander

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item 2 Hander; drawn [Drawn_2_Hander]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | '2 Hander' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | '2 Hander' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_4:Melee Weapon Attack 4; vHitEffects on 'Drawn_2_Hander': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee '2 Hander' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 4, hit events 1, sub-1hp chip 0.0, first hit 0.2s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee '2 Hander': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 38.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.4m, first attack at 1.4m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.4m), first damage 0.2s, attacks 1, attack-state frames 10, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Blade Of Chaos

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 21 |  |
| Audio | 'Blade Of Chaos' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_2:neverworld, SfxPool_4:Melee Weapon Air Swing 1, SfxPool_5:Melee Weapon Air Swing 2, SfxPool_7:Melee Weapon Air Swing 4 |  |
| Audio | 'Blade Of Chaos' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_0:Melee Weapon Attack 3, SfxPool_1:neverworld, SfxPool_2:Melee Weapon Air Swing 4; vHitEffects on 'Drawn_Blade_Of_Chaos': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 30.0 (item dmg 21), attacks 3, hitbox-active frames 26, hit events 1, sub-1hp chip 0.0, first hit 3.6s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Blade Of Chaos': intentional miss rate vs configured chance | INFO | swings 3, intentional misses rolled 0 (0 %) vs configured 10 % (+-52 % at 3 sigma), weapon hit events 1, player hp lost 30.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 3 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 3.6s, attacks 3, attack-state frames 59, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Death Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'Death Axe' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Death Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_0:Melee Weapon Attack 3; vHitEffects on 'Drawn_Death_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 24.0 (item dmg 18), attacks 1, hitbox-active frames 4, hit events 1, sub-1hp chip 0.9, first hit 0.3s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Death Axe': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 24.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 10, clips seen: WeakAttack_SwordB, WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Spear of Fate

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 16 |  |
| Audio | 'Spear of Fate' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_6:Melee Weapon Air Swing 3, SfxPool_8:Melee Weapon Air Swing 4, SfxPool_9:Melee Weapon Air Swing 3 |  |
| Audio | 'Spear of Fate' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_9:Melee Weapon Air Swing 3, SfxPool_0:Melee Weapon Attack 3, SfxPool_1:neverworld; vHitEffects on 'Drawn_Spear_of_Fate': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 20.0 (item dmg 16), attacks 1, hitbox-active frames 16, hit events 1, sub-1hp chip 0.0, first hit 1.5s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Spear of Fate': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 20.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.6m, first attack at 1.6m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.6m), first damage 1.5s, attacks 1, attack-state frames 30, clips seen: WeakAttack_SwordA, WeakAttack_SwordB, WeakAttack_SwordC |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Sword of Fear

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Sword of Fear; drawn [Drawn_Sword_of_Fear]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 15 |  |
| Audio | 'Sword of Fear' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_1:Melee Weapon Air Swing 2 |  |
| Audio | 'Sword of Fear' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_2:Melee Weapon Attack 3, SfxPool_3:Melee Weapon Air Swing 1, SfxPool_5:Melee Weapon Attack 3; vHitEffects on 'Drawn_Sword_of_Fear': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Sword of Fear' damages the player | PASS | player hp lost 38.2 (item dmg 15), attacks 1, hitbox-active frames 10, hit events 2, sub-1hp chip 2.2, first hit 1.0s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Sword of Fear': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 2, player hp lost 38.2; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.7m, first attack at 2.0m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.5s (at 2.0m), first damage 1.0s, attacks 1, attack-state frames 25, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap2_sword

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'weap2_sword' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_0:neverworld, SfxPool_2:Melee Weapon Air Swing 1 |  |
| Audio | 'weap2_sword' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_3:Melee Weapon Attack 3; vHitEffects on 'Drawn_weap2_sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 47.0 (item dmg 18), attacks 1, hitbox-active frames 5, hit events 1, sub-1hp chip 0.0, first hit 1.1s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 47.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 1.1s, attacks 1, attack-state frames 19, clips seen: WeakAttack_SwordB, WeakAttack_SwordC, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap_two_handed

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | 'weap_two_handed' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'weap_two_handed' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_0:Melee Weapon Attack 1; vHitEffects on 'Drawn_Two_Handed_Sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 3, hit events 1, sub-1hp chip 0.0, first hit 0.2s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap_two_handed': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 38.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 0.2s, attacks 1, attack-state frames 7, clips seen: Attack2Hand_C, WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Wood Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 26 |  |
| Audio | 'Wood Axe' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Wood Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_8:Melee Weapon Attack 1; vHitEffects on 'Drawn_Wood_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Wood Axe' damages the player | PASS | player hp lost 31.0 (item dmg 26), attacks 1, hitbox-active frames 2, hit events 1, sub-1hp chip 0.0, first hit 0.2s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Wood Axe': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 31.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 0.2s, attacks 1, attack-state frames 7, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: DM_Mining_Tool

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | FAIL | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Wood_Axe]; item dmg 0 | ConfigureSpawnLoadout + TrySwitchTo did not make this the active weapon: the ItemData has no equip prefab / the template has no matching Drawn_ slot for it; ItemData rangedDamage = 0 |
| Weapons | Ranged 'DM_Mining_Tool' damages the player | FAIL | player hp lost 0.0 (item dmg 0), shots/projectiles 0, attack-state frames 340, attacks 0, first hit never, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | ItemData rangedDamage = 0 / weapon never became the active weapon (see equip row) / ItemData 'DM_Mining_Tool'.rangedDamage = 0 / attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| Weapons | Ranged 'DM_Mining_Tool': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack never (at -1.0m), first damage never, attacks 0, attack-state frames 340, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: sci_fi_pistol

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Wood_Axe]; item dmg 14 |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 332, attacks 0, first hit never, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack never (at -1.0m), first damage never, attacks 0, attack-state frames 332, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: survival_rifle

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_Wood_Axe]; item dmg 18 |  |
| Weapons | Ranged 'survival_rifle' damages the player | FAIL | player hp lost 0.0 (item dmg 18), shots/projectiles 0, attack-state frames 299, attacks 0, first hit never, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| Weapons | Ranged 'survival_rifle': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread survival_rifle: projectileSpread 0.9 deg, accuracy 82, close-range scale 0.15 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack never (at -1.0m), first damage never, attacks 0, attack-state frames 299, clips seen: none matched |  |
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
| Camera: faces forward after teleports, zoom in/out | 2 | 0 | 1 | 11s |
| Equip: weapon survives a teleport | 2 | 0 | 0 | 7s |
| Audio: swing / hit sound wiring (static) | 4 | 0 | 0 | 1s |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | 6 | 10 | 5 | 86s |
| Block + parry: player defends against enemy melee | 1 | 1 | 2 | 56s |
| Block: enemy defends against player swings | 1 | 0 | 1 | 12s |
| Movement: retreat, push, flank, backstab, lose target | 4 | 0 | 2 | 35s |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | 3 | 0 | 6 | 52s |
| Multi-enemy: FRED x3 and x5 | 1 | 0 | 2 | 40s |
| Brain: orbit, toggles, archetypes | 4 | 0 | 2 | 0s |
| Regression of earlier fixes and tweaks | 7 | 1 | 0 | 0s |

### Scenario: Camera: faces forward after teleports, zoom in/out

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Camera | Camera sits behind the player and faces the same way after teleports | PASS | yaw 0: dot 1.00; yaw 90: dot 1.00; yaw 180: dot 1.00; yaw 270: dot 1.00 (camera vThirdPersonCamera) |  |
| Camera | Camera zooms in and out | WARN | distance start 2.5m, zoomed in 2.5m, zoomed out 6.5m via ForceSetZoomDistance fallback (scroll input did not move the camera) | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state |
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
| Controls | Input path diagnostics (W held 0.35s) | INFO | isFocused False, cursor Locked, keyboard id 1 enabled True W pressed False, mouse enabled, keyboards 1, gamepads 0, InputSystem IgnoreFocus/AllDeviceInputAlwaysGoesToGameView, Player_v7 Variant: scheme '', enabled False, actions enabled True, devices [], Move (0.00, 0.00); Player_v7 Combat Variant: scheme 'Keyboard&Mouse', enabled True, actions enabled True, devices [Keyboard,Mouse], Move (0.00, 0.00) // while W held: isFocused False, cursor Locked, keyboard id 1 enabled True W pressed False, mouse enabled, keyboards 1, gamepads 0, InputSystem IgnoreFocus/AllDeviceInputAlwaysGoesToGameView, Player_v7 Variant: scheme '', enabled False, actions enabled True, devices [], Move (0.00, 0.00); Player_v7 Combat Variant: scheme 'Keyboard&Mouse', enabled True, actions enabled True, devices [Keyboard,Mouse], Move (0.00, 0.00) |  |
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
| Controls | Climbing unlocked and controller enabled (not driven: needs a wall) | PASS | DMPlayerSystemsProfile present: ClimbEnabled=True, DashEnabled=True, JetpackEnabled=True; DMClimbController enabled, DMDashController enabled, DMJetpackController enabled |  |
| Controls | Movement systems unlocked for the run (jetpack, dash, climb) | INFO | DMPlayerSystemsProfile present: ClimbEnabled=True, DashEnabled=True, JetpackEnabled=True; DMClimbController enabled, DMDashController enabled, DMJetpackController enabled. The audit unlocks these in memory only (see environment notes) and restores them at exit. |  |
| Controls | Left Alt dash | WARN | start 0.00, min 0.00, max 0.00, end 0.00 m | Dash did not move the player even though the skill gate is unlocked for this run (see ctl.systems row): DMDashController disabled, on cooldown, stamina, or the Dash action not bound to Left Alt |
| Controls | Hold Space after jumping boosts (jetpack) | WARN | start 21.23, min 21.23, max 21.23, end 21.23 m height | no sustained lift although the jetpack skill gate is unlocked for this run (see ctl.systems row): fuel 0, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player |
| Controls | Mouse look left/right orbits the camera | PASS | start 0.00, min 0.00, max 30.00, end 35.40 deg yaw |  |
| Controls | Mouse look up/down tilts the camera | PASS | start 18.00, min 0.00, max 24.60, end 30.00 deg pitch |  |
| Controls | Left click swings the drawn melee weapon | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 isAttacking | melee Attack input not received: drawn melee tap/hold logic (UpdateDrawnMeleeCharge) did not start a swing |
| Controls | Right click blocks (melee drawn) | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 isBlocking | Block input (Mouse rightButton / Block action) did not set isBlocking: stamina 0, customAction, not grounded, or still attacking |
| Controls | Middle click does not break gameplay | PASS | start 0.00, min 0.00, max 0.00, end 0.00 none |  |
| Controls | Middle click has no gameplay binding | INFO | InputSystem_Actions Player map binds no action to Mouse/middleButton (only the UI map MiddleClick) |  |
| Controls | Tab (SwitchWeapon) cycles the weapon focus | WARN | Hot Cross weapon focus 0 -> 0, drawn weap2_sword -> weap2_sword | Tab tap did not change focus: only one weapon on the hotbar, Hot Cross not polling, or tap/hold threshold (hold arms) not met |
| Controls | Half-tilt stick walks slower than a run | WARN | start 0.00, min 0.00, max 0.81, end 0.83 m in 1.5s | walk speed not distinct from run (0.0m): analog Move magnitude ignored by the locomotion speed |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block + parry: player defends against enemy melee

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Unblocked baseline (same enemy, same distance) | INFO | hits 1, player hp lost 26.3 |  |
| Block | Holding block negates enemy melee damage | FAIL | BLOCK = hold right mouse. isBlocking False (raised in 1.54s), hits 2, blocked 0, player hp lost 52.9 (unblocked 26.3), stamina 1000.0 | player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking |
| Block | Enemy guard-break stagger after a blocked hit | WARN | longest enemy stagger 0.00s (profile blockStaggerSeconds 1.2s) | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) |
| Parry | Parry = tap right mouse, timed before the incoming hit (several timings) | INFO | tap -0.60s vs predicted hit (windup 0.42s): blow landed 2.54s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 106.2 // tap -0.40s vs predicted hit (windup 0.42s): blow landed 2.53s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 104.9 // tap -0.25s vs predicted hit (windup 0.42s): blow landed 3.30s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 126.9 // tap -0.15s vs predicted hit (windup 0.42s): blow landed 3.40s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 78.5 // tap -0.08s vs predicted hit (windup 0.42s): blow landed 2.05s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 53.6 // tap -0.02s vs predicted hit (windup 0.42s): blow landed 2.33s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 104.9 // tap +0.10s vs predicted hit (windup 0.42s): blow landed 3.02s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 126.9 | input: PARRY = TAP right mouse (0.08s), BLOCK = HOLD right mouse; profile: parryWindowSeconds 0.2, blockStagger 1.2s, parryStagger 5.0s |
| Parry | A tap inside the parry window parries (and an early/late tap does not) | WARN | no parry observed (0 taps landed inside the window; see sweep rows) | no sampled tap produced a parry: enemy attack timing varies between swings (windup from the baseline swing may not match), or the parry window/stagger in the profile differs from what the rows show |
| Parry | A successful parry staggers the attacker (counter opening) | INFO | no parry happened, so the counter stagger could not be observed |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block: enemy defends against player swings

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Enemy block reduces player melee damage | WARN | open: 0 hits, enemy hp lost 0.0; blocking (animator IsBlocking True, duration 40.0s): 0 hits (0 flagged blocked), enemy hp lost 0.0; block clips: Idle, Walk, Idle@Pistol, Defense_HoldingSword | player swings registered no hits on the open enemy, so the comparison is inconclusive (input focus, reach, or the player weapon) |
| Parry | Enemy parry | INFO | not implemented: EnemyInvectorCombatBridge only has TryBeginBlock/EndBlock (no parry window/riposte API) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Movement: retreat, push, flank, backstab, lose target

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Movement | Player retreats 9m at 3 m/s: enemy follows | PASS | distance 2.5m -> 2.5m (min 2.0m), states Chase/Attack, attacks 1 |  |
| Movement | Player pushes into the enemy: spacing and facing | PASS | min distance 0.50m, max facing error 0deg, attack-state frames 29, states Chase/Attack |  |
| Movement | Player circles behind the enemy: enemy turns to face | WARN | reached behind at never, enemy faced player (<30deg) after never, max facing error 0deg, attacks while behind 0, player hp lost 0.0, states Attack | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player |
| Movement | Hit from behind vs from the front (player melee, AI off) | WARN | front: 0 hits, 0.0 hp; behind: 0 hits, 0.0 hp; ratio n/a | swings did not land from one side (reach/facing/input), comparison inconclusive |
| Movement | Player disappears 70m away: enemy loses the target | PASS | was aware True, investigate/search after never, back to calm state after 6.1s, states Chase/ReturnHome |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Play | Approach with W: enemy notices | WARN | walked -1.7m in 14.0s to 31.7m; enemy noticed after never at n/a; state Wander | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) |
| Play | Run away with S: enemy pursues | WARN | player backed away for 4.0s (gap now 36.1m), enemy moved 4.7m, hostile state seen False, states Wander | enemy did not chase a retreating player: chase blocked (standoff/hold token, ranged keep-distance) or target lost immediately |
| Play | Strafe around the enemy: it turns to face the player | WARN | enemy facing error avg 87deg, max 179deg over 103 frames while strafing D then A, state Wander | enemy did not keep facing a strafing player: turn speed too slow, turn-in-place gated, or enemy in a state that ignores the target |
| Play | Retreat then re-engage: enemy fights back | WARN | re-engaged to 33.6m, enemy attacks 0, player hp lost 0.0, state Wander | enemy never attacked a player standing next to it: attack cooldown/token starvation or stagger lock |
| Play | Jump away: player clears distance | WARN | jump height 0.00m, gap 33.6m -> 30.3m, enemy state Wander | jump did not leave the ground (Jump input / grounded) |
| Play | Jetpack escape (hold Space + S) | WARN | peak height gain 0.0m, gap 30.3m -> 34.0m, enemy state Wander | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump |
| Play | Sprint away: enemy loses the target | PASS | lost after 1.5s at 34.3m (sprinting), state Wander, states Wander |  |
| Play | Enemy returns to its post after losing the player | PASS | back home after 3.2s, now 3.4m from spawn, state Wander |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Multi-enemy: FRED x3 and x5

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Multi-enemy | Enemy used | INFO | HumanoidEnemy_Invector (FRED / Hybrid Droid) |  |
| Multi-enemy | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 2 (94/249 frames with >1), distinct attackers 3/3, attacker hand-offs 7, attack-state frames [199,153,71], chase/hold frames [48,95,175], closest pair 1.43m, closest to player 1.16m, orbit around player avg 41deg max 53deg (net 11/-34/5), player hp lost 51, brain actions Press:460/Hold:281/None:6 | 94/249 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Multi-enemy | 5 FREDs at once for 18s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 2 (130/232 frames with >1), distinct attackers 5/5, attacker hand-offs 15, attack-state frames [88,99,119,123,115], chase/hold frames [142,132,110,107,116], closest pair 1.08m, closest to player 1.16m, orbit around player avg 40deg max 61deg (net 41/-11/34/27/-13), player hp lost 25, brain actions Press:632/Hold:519/None:9 | 130/232 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Brain: orbit, toggles, archetypes

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Brain | Single FRED, player standing still: does not orbit the player | PASS | notice 0.1s, chase 0.1s, first attack 1.5s, attacks 4, dmg 177, min dist 1.2m, path 6.3m, yaw 23deg, orbit around player net 23deg / abs 23deg, avg facing error 0deg, brain Press:191/None:1, states Chase/Attack |  |
| Brain | Single FRED, player strafes around it: keeps facing the player, does not orbit on its own | PASS | player circled 4.0m radius for 9s; enemy avg facing error 5deg, within 40deg 100% of frames, enemy angular travel around the player 274deg, states Attack/Chase, path 9.7m |  |
| Multi-enemy | 3 FREDs, baseline (brain on, director on) | WARN | max simultaneous attackers 2 (48/176 frames with >1), distinct attackers 3/3, attacker hand-offs 6, attack-state frames [78,65,138], chase/hold frames [97,108,36], closest pair 1.81m, closest to player 1.16m, orbit around player avg 19deg max 37deg (net 0/17/36), player hp lost 0, brain actions Press:327/Hold:195/None:6 | 48/176 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:327/Hold:195/None:6 |  |
| Multi-enemy | 3 FREDs, enableUtilityBrain=false | PASS | max simultaneous attackers 2 (119/175 frames with >1), distinct attackers 3/3, attacker hand-offs 10, attack-state frames [149,146,139], chase/hold frames [25,26,34], closest pair 2.09m, closest to player 1.18m, orbit around player avg 1deg max 2deg (net 0/2/0), player hp lost 120, brain actions None:525 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions None:525 |  |
| Multi-enemy | 3 FREDs, enableEngagementDirector=false | PASS | max simultaneous attackers 3 (106/148 frames with >1), distinct attackers 3/3, attacker hand-offs 8, attack-state frames [122,123,125], chase/hold frames [23,23,22], closest pair 2.36m, closest to player 1.34m, orbit around player avg 1deg max 1deg (net 1/-1/0), player hp lost 77, brain actions None:343/Press:101 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = False; brain actions None:343/Press:101 |  |
| Multi-enemy | 3 FREDs, legacyRingSlotRotation=true | WARN | max simultaneous attackers 2 (76/176 frames with >1), distinct attackers 3/3, attacker hand-offs 5, attack-state frames [100,135,117], chase/hold frames [74,40,56], closest pair 1.88m, closest to player 1.16m, orbit around player avg 55deg max 86deg (net 9/27/-9), player hp lost 0, brain actions Press:409/Hold:113/None:6 | 76/176 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:409/Hold:113/None:6 |  |

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | FAIL | code present: True; 4 PASS / 4 FAIL / 0 warn of 8 runtime row(s) | swing rows still silent for: '2 Hander' swing plays a swish, 'Death Axe' swing plays a swish, 'weap_two_handed' swing plays a swish, 'Wood Axe' swing plays a swish |
| Fix check | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | PASS | code present: True; 8 PASS / 0 FAIL / 0 warn of 8 runtime row(s) |  |
| Fix check | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | PASS | 1 PASS / 0 FAIL / 0 warn of 1 runtime row(s); weapon reverts recorded this run: 0 |  |
| Fix check | vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons) | PASS | code present: True; console errors across 12 weapon-matrix case(s): 0 |  |
| Fix check | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | PASS | code present: True; dual-slot override case: 6 PASS / 0 FAIL |  |
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 24 value(s) compared on 3 enemies |  |
| Fix check | No circling while unaware (patrol-anchor fix) on every enemy | PASS | 4 PASS / 0 FAIL of 4 enemies |  |
| Fix check | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | PASS | 4 PASS / 0 FAIL of 4 enemies |  |
| Fix check | Profile adjustments made by earlier audit passes | INFO | none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes |  |

## Per-enemy detail

### Humadroid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 35s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | WARN | spawn 38ms, 761 components, frame avg 61.8ms max 145.9ms (154 frames), GC collections 0, managed delta 22784KB | average editor frame 61.8ms with one enemy active |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 223deg, path 12.0m, net 5.9m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.3s (at 1.7m, weapon weap2_sword), first player damage 4.4s, attacks 2, states Patrol/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 4.4s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 6 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.4s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.2m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.8 (item dmg 14), shots/projectiles 1, attack-state frames 32, attacks 1, first hit 2.6s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 175.7 (-24.3); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 175.7 -> 149.1; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 2, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 0 item entries + AC only, TryLootAll -> None, left 0; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.30s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 1 clip(s) observed playing; 25 distinct clips seen in total |  |
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

### Robot

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `Robot_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 35s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Robot_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=15, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | WARN | spawn 33ms, 698 components, frame avg 58.4ms max 290.4ms (162 frames), GC collections 1, managed delta -191656KB | average editor frame 58.4ms with one enemy active |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 145deg, path 10.2m, net 8.0m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.3s (at 1.6m, weapon weap2_sword), first player damage 2.8s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.8s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 2, hitbox-active frames 21 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 2.8s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 2, intentional misses rolled 1 (50 %) vs configured 10 % (+-64 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 2 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 15.0 (item dmg 14), shots/projectiles 1, attack-state frames 35, attacks 1, first hit 3.0s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 476.2 (-23.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 476.2 -> 370.6; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 3, body type Robot |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 item entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.30s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 18 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `Humanoid_Enemy` - melee `Blade Of Chaos` / ranged `sci_fi_pistol` - audited in 35s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=Blade Of Chaos (dmg 21), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | WARN | spawn 36ms, 693 components, frame avg 54.4ms max 124.3ms (169 frames), GC collections 0, managed delta 31732KB | average editor frame 54.4ms with one enemy active |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 145deg, path 10.8m, net 3.9m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.6s (at 1.6m, weapon Blade Of Chaos), first player damage 3.1s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 30.0 hp in 3.1s using Blade Of Chaos |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 30.0 (item dmg 21), attacks 1, hitbox-active frames 6 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.7s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Blade Of Chaos': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 30.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.2m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.9 (item dmg 14), shots/projectiles 1, attack-state frames 62, attacks 3, first hit 4.4s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 1.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Blade_Of_Chaos] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 278.3 (-21.7); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 278.3 -> 261.8; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 0 item entries + AC only, TryLootAll -> None, left 0; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.30s, loot bag 6.7s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### The_Evil_One

Prefab `Assets/_Project/Prefabs/Combat/Enemies/The_Evil_One.prefab` - definition `The_Evil_One` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 51s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=The_Evil_One, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=True, def.attackDamage=12, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | WARN | spawn 44ms, 707 components, frame avg 58.2ms max 140.2ms (160 frames), GC collections 0, managed delta 20976KB | average editor frame 58.2ms with one enemy active |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 142deg, path 7.2m, net 4.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.3s (at 9.6m, weapon sci_fi_pistol), first player damage 3.5s, attacks 2, states Wander/Defensive/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 15.9 hp in 3.5s using sci_fi_pistol |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 3 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.2s, min distance 1.28m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; no vMeleeWeapon on the drawn/bound weapon |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.3m, first attack at 1.3m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 294, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.65 | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 0 (0 %) vs about 35 % expected from definition missRate 0.65 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 479.8 (-20.2); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 479.8 -> 453.1; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 2, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 2 item entries, TryLootAll -> Granted, left 0, 2 granted item(s) removed again from the audit inventory; definition random loot count 0-2 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.29s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 19 distinct clips seen in total |  |
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
