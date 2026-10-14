# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-13 19:53:27 -> 2026-10-13 20:00:38 (aborted: play mode exited before the run completed)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 0; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 3
- Unit store: 37 stored unit(s); stamps audit 91CBB6 game 727B6D gear 11046A; skip-unchanged is OFF (Full mode)
- Mode full: 10 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 7m11s

**3 enemies audited - 8 FAIL, 5 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 9 scenarios): 68 PASS, 19 FAIL, 19 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 70 change(s).

- NEW enemy|Humadroid|perf = PASS
- NEW enemy|Humadroid|melee.miss = INFO
- CHANGED enemy|Humadroid|ranged.damage: PASS -> FAIL
- NEW enemy|Humadroid|ranged.miss = INFO
- CHANGED enemy|Humadroid|vis: PASS -> FAIL
- CHANGED enemy|Humadroid|player.swing: PASS -> WARN
- NEW enemy|Humadroid|loot = FAIL
- CHANGED enemy|Humadroid|console: PASS -> WARN
- NEW enemy|Robot|perf = PASS
- NEW enemy|Robot|melee.miss = INFO
- CHANGED enemy|Robot|ranged.damage: PASS -> FAIL
- NEW enemy|Robot|ranged.miss = INFO
- CHANGED enemy|Robot|vis: PASS -> FAIL
- CHANGED enemy|Robot|player.swing: PASS -> WARN
- NEW enemy|Robot|loot = FAIL
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
- NEW enemy|SwordMaster|ranged.damage = FAIL
- NEW enemy|SwordMaster|ranged.miss = INFO
- NEW enemy|SwordMaster|ranged.vis = INFO
- NEW enemy|SwordMaster|vis = FAIL
- NEW enemy|SwordMaster|player.dmg = PASS
- NEW enemy|SwordMaster|player.swing = WARN
- NEW enemy|SwordMaster|hitmarks = PASS
- NEW enemy|SwordMaster|loot = PASS
- NEW enemy|SwordMaster|death = PASS
- NEW enemy|SwordMaster|anim = INFO
- NEW enemy|SwordMaster|console = PASS
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
- CHANGED scenario|Block + parry: player defends against enemy melee|parry.window: PASS -> WARN
- NEW scenario|Block + parry: player defends against enemy melee|parry.counter = INFO
- CHANGED scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.jump: PASS -> WARN
- CHANGED scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.lose: PASS -> WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.swish = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hitsound = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hotcross = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.hiteffects = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.loadout = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.tuning = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.patrol = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.onedrawn = FAIL
- NEW scenario|Regression of earlier fixes and tweaks|fix.profiles = INFO
- MISSING scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.return (was WARN)
- MISSING scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|console (was PASS)

## Timing

Wall time 431s (7.2 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Humadroid | enemy | 42.3 | ran |
| Robot | enemy | 42.0 | ran |
| SwordMaster | enemy | 42.6 | ran |
| Camera: faces forward after teleports, zoom in/out | scenario | 9.2 | ran |
| Equip: weapon survives a teleport | scenario | 6.1 | ran |
| Audio: swing / hit sound wiring (static) | scenario | 0.4 | ran |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | scenario | 60.6 | ran |
| Block + parry: player defends against enemy melee | scenario | 66.3 | ran |
| Block: enemy defends against player swings | scenario | 15.4 | ran |
| Movement: retreat, push, flank, backstab, lose target | scenario | 32.5 | ran |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | scenario | 0.0 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | setup | 97.5 | 2084 | 46.5 | 959.9 | 0.64 | 0.00 | 17.47 |
| scenario|Block + parry: player defends against enemy melee | setup | 67.6 | 1562 | 43.3 | 544.3 | 0.56 | 0.00 | 17.01 |
| scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | setup | 60.8 | 1734 | 35.1 | 169.9 | 0.44 | 0.00 | 14.31 |
| scenario|Movement: retreat, push, flank, backstab, lose target | setup | 32.8 | 596 | 55.0 | 294.4 | 0.70 | 0.00 | 17.77 |
| enemy|Humadroid | ranged test | 17.5 | 472 | 37.6 | 272.8 | 0.49 | 0.00 | 16.07 |
| enemy|Robot | ranged test | 17.3 | 479 | 36.6 | 263.3 | 0.48 | 0.00 | 15.63 |
| enemy|SwordMaster | ranged test | 17.3 | 478 | 36.7 | 299.8 | 0.48 | 0.00 | 15.37 |
| scenario|Block: enemy defends against player swings | setup | 16.5 | 357 | 46.2 | 111.0 | 0.61 | 0.00 | 16.89 |
| scenario|Camera: faces forward after teleports, zoom in/out | setup | 9.4 | 264 | 35.6 | 215.4 | 0.45 | 0.00 | 14.31 |
| enemy|Humadroid | unaware 9s (+perf) | 9.3 | 244 | 38.2 | 401.2 | 0.50 | 0.00 | 15.70 |
| enemy|Robot | unaware 9s (+perf) | 9.0 | 254 | 35.6 | 85.7 | 0.47 | 0.00 | 15.22 |
| enemy|SwordMaster | unaware 9s (+perf) | 9.0 | 249 | 36.2 | 83.9 | 0.45 | 0.00 | 15.44 |
| enemy|Humadroid | death/loot | 6.7 | 181 | 36.9 | 188.7 | 0.53 | 0.00 | 14.83 |
| enemy|SwordMaster | death/loot | 6.7 | 183 | 36.2 | 82.5 | 0.46 | 0.00 | 15.09 |
| enemy|Robot | death/loot | 6.5 | 181 | 35.7 | 93.0 | 0.45 | 0.00 | 14.78 |
| scenario|Equip: weapon survives a teleport | setup | 6.3 | 172 | 36.7 | 218.3 | 0.47 | 0.00 | 14.06 |
| enemy|SwordMaster | melee test | 4.1 | 85 | 44.6 | 92.5 | 0.64 | 0.00 | 16.50 |
| enemy|Humadroid | player->enemy | 3.3 | 65 | 49.8 | 105.3 | 0.68 | 0.00 | 19.90 |
| enemy|Robot | melee test | 2.9 | 60 | 44.8 | 91.5 | 0.58 | 0.00 | 17.13 |
| enemy|Robot | notice/chase/attack | 2.7 | 70 | 38.8 | 78.7 | 0.55 | 0.00 | 15.96 |
| enemy|Robot | player->enemy | 2.5 | 57 | 44.0 | 94.5 | 0.56 | 0.00 | 16.39 |
| enemy|SwordMaster | player->enemy | 2.4 | 62 | 39.5 | 89.4 | 0.54 | 0.00 | 15.68 |
| enemy|Humadroid | notice/chase/attack | 2.2 | 51 | 42.2 | 82.0 | 0.83 | 0.00 | 17.35 |
| enemy|SwordMaster | notice/chase/attack | 2.1 | 53 | 40.1 | 83.5 | 0.59 | 0.00 | 16.28 |
| enemy|Humadroid | melee test | 2.1 | 41 | 46.3 | 88.7 | 0.69 | 0.00 | 17.17 |
| enemy|Humadroid | spawn | 1.0 | 19 | 60.1 | 162.6 | 0.91 | 0.00 | 18.30 |
| enemy|SwordMaster | spawn | 1.0 | 24 | 43.8 | 88.0 | 0.75 | 0.00 | 15.78 |
| enemy|Robot | spawn | 1.0 | 23 | 44.6 | 90.5 | 0.64 | 0.00 | 15.90 |
| scenario|Audio: swing / hit sound wiring (static) | setup | 0.6 | 9 | 70.7 | 374.6 | 0.48 | 0.00 | 13.53 |
| enemy|Robot | cleanup | 0.2 | 4 | 54.7 | 71.0 | 0.49 | 0.00 | 14.45 |

Whole run (ran units only): 419s over 10123 frames, avg frame 41.6 ms, physics 0.54 ms, render 0.00 ms, scripts 16.14 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 100 | FAIL | Ranged 'sci_fi_pistol' damages the player | Humadroid, Robot, SwordMaster | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 2 | 85 | FAIL | Exactly one weapon drawn (resolved), other holstered | Humadroid, Robot, SwordMaster | 2 Holstered_ slots active at once | Let EnemyInvectorLoadoutBridge be the only weapon authority; hide every Drawn_/Holstered_ slot except the resolved ones. |
| 3 | 55 | WARN | Console errors/warnings | Humadroid | [Warning] The referenced script on this Behaviour (Game Object '<null>') is missing! | Fix the exception at the listed stack frame (null-guard or initialise the field); it repeats on every spawn. |
| 4 | 25 | WARN | Hit marks fire on damage | Humadroid | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) | Add DMEnemyHitMarks + the layer-26 hitbox rig; verify the body-type hit-mark profile. |
| 5 | 20 | FAIL | Loot dropped and can be looted (real loot path), then cleaned up at once | Humadroid, Robot | bag spawned but nothing could be looted (empty entries / TryLootAll refused) | Inspect the measured value and cause on the detail row. |
| 6 | 20 | FAIL | W moves the player forward (camera direction) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move action not reaching the controller: PlayerInput disabled, move locked, or PioneerPlayerInputBinder.HandleAction('Move') not wired | Inspect the measured value and cause on the detail row. |
| 7 | 20 | FAIL | S moves the player back | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (S) produced no backward displacement | Inspect the measured value and cause on the detail row. |
| 8 | 20 | FAIL | A strafes left | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (A) produced no leftward displacement | Inspect the measured value and cause on the detail row. |
| 9 | 20 | FAIL | D strafes right | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (D) produced no rightward displacement | Inspect the measured value and cause on the detail row. |
| 10 | 20 | FAIL | W run speed (reference for sprint) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | player did not move | Inspect the measured value and cause on the detail row. |
| 11 | 20 | FAIL | Shift + W sprints faster than W | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | sprint no faster than run (0.0m): Sprint action not wired, stamina 0, or sprint speed equals run speed | Inspect the measured value and cause on the detail row. |
| 12 | 20 | FAIL | Space jumps | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked; state before the press: isGrounded True, isJumping False, customAction False, isCrouching False, stamina 1000 | Inspect the measured value and cause on the detail row. |
| 13 | 20 | FAIL | Q dodge / roll | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dodge action did not move the player: TryDodgeRollFromInput refused (stamina, cooldown, no roll clip) | Inspect the measured value and cause on the detail row. |
| 14 | 20 | FAIL | Left click swings the drawn melee weapon | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | melee Attack input not received: drawn melee tap/hold logic (UpdateDrawnMeleeCharge) did not start a swing | Inspect the measured value and cause on the detail row. |
| 15 | 20 | FAIL | Right click blocks (melee drawn) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Block input (Mouse rightButton / Block action) did not set isBlocking: stamina 0, customAction, not grounded, or still attacking | Inspect the measured value and cause on the detail row. |
| 16 | 20 | WARN | Player circles behind the enemy: enemy turns to face | Movement: retreat, push, flank, backstab, lose target | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 17 | 20 | FAIL | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks | Humadroid, Robot, SwordMaster | Inspect the measured value and cause on the detail row. |
| 18 | 15 | WARN | A tap inside the parry window parries (and an early/late tap does not) | Block + parry: player defends against enemy melee | no sampled tap produced a parry: enemy attack timing varies between swings (windup from the baseline swing may not match), or the parry window/stagger in the profile differs from what the rows show | Inspect the measured value and cause on the detail row. |
| 19 | -5 | WARN | Player melee swing via injected Mouse input (best effort) | Humadroid, Robot, SwordMaster | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes | Inspect the measured value and cause on the detail row. |
| 20 | -5 | WARN | Camera zooms in and out | Camera: faces forward after teleports, zoom in/out | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state | Inspect the measured value and cause on the detail row. |
| 21 | -5 | WARN | Left Ctrl crouches | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) | Inspect the measured value and cause on the detail row. |
| 22 | -5 | WARN | Left Alt dash | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dash did not move the player even though the skill gate is unlocked for this run (see ctl.systems row): DMDashController disabled, on cooldown, stamina, or the Dash action not bound to Left Alt | Inspect the measured value and cause on the detail row. |
| 23 | -5 | WARN | Hold Space after jumping boosts (jetpack) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | no sustained lift although the jetpack skill gate is unlocked for this run (see ctl.systems row): fuel 0, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player | Inspect the measured value and cause on the detail row. |
| 24 | -5 | WARN | Tab (SwitchWeapon) cycles the weapon focus | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Tab tap did not change focus: only one weapon on the hotbar, Hot Cross not polling, or tap/hold threshold (hold arms) not met | Inspect the measured value and cause on the detail row. |
| 25 | -5 | WARN | Half-tilt stick walks slower than a run | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | walk speed not distinct from run (0.0m): analog Move magnitude ignored by the locomotion speed | Inspect the measured value and cause on the detail row. |
| 26 | -5 | WARN | Jump away: player clears distance | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | player could not gain distance (enemy kept pace or player blocked) | Inspect the measured value and cause on the detail row. |
| 27 | -5 | WARN | Jetpack escape (hold Space + S) | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump | Inspect the measured value and cause on the detail row. |
| 28 | -5 | WARN | Sprint away: enemy loses the target | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy kept hostile for 30s of sprinting away: lose-target distance/time too long (senses) or leash not applied | Inspect the measured value and cause on the detail row. |
| 29 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 30 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 31 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Humadroid | weap2_sword | sci_fi_pistol | PASS | **FAIL** | PASS | PASS | PASS | **FAIL** | PASS | warn | PASS | warn |
| Robot | weap2_sword | sci_fi_pistol | PASS | **FAIL** | PASS | PASS | PASS | **FAIL** | PASS | PASS | PASS | PASS |
| SwordMaster | Blade Of Chaos | sci_fi_pistol | PASS | **FAIL** | PASS | PASS | PASS | **FAIL** | PASS | PASS | PASS | PASS |

## Weapons that deal NO damage (headline)

- **Humadroid** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 427, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised)
- **Robot** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 428, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised)
- **SwordMaster** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 429, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised)

## Scenarios (block/parry, movement, multi-enemy, player weapons, performance)

| Scenario | PASS | FAIL | warn | time |
|---|---|---|---|---|
| Camera: faces forward after teleports, zoom in/out | 2 | 0 | 1 | 9s |
| Equip: weapon survives a teleport | 2 | 0 | 0 | 6s |
| Audio: swing / hit sound wiring (static) | 4 | 0 | 0 | 0s |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | 6 | 10 | 5 | 61s |
| Block + parry: player defends against enemy melee | 2 | 0 | 1 | 66s |
| Block: enemy defends against player swings | 2 | 0 | 0 | 15s |
| Movement: retreat, push, flank, backstab, lose target | 4 | 0 | 1 | 33s |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | 4 | 0 | 3 | 0s |
| Regression of earlier fixes and tweaks | 4 | 1 | 3 | 0s |

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
| Controls | Input path diagnostics (W held 0.35s) | INFO | isFocused False, cursor Locked, keyboard id 1 enabled True W pressed False, mouse enabled, keyboards 1, gamepads 0, InputSystem IgnoreFocus/AllDeviceInputAlwaysGoesToGameView, Player_v7 Combat Variant: scheme 'Keyboard&Mouse', enabled True, actions enabled True, devices [Keyboard,Mouse], Move (0.00, 0.00) // while W held: isFocused False, cursor Locked, keyboard id 1 enabled True W pressed False, mouse enabled, keyboards 1, gamepads 0, InputSystem IgnoreFocus/AllDeviceInputAlwaysGoesToGameView, Player_v7 Combat Variant: scheme 'Keyboard&Mouse', enabled True, actions enabled True, devices [Keyboard,Mouse], Move (0.00, 0.00) |  |
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
| Controls | Mouse look left/right orbits the camera | PASS | start 0.00, min 0.00, max 77.48, end 81.77 deg yaw |  |
| Controls | Mouse look up/down tilts the camera | PASS | start 18.00, min 0.00, max 49.75, end 49.87 deg pitch |  |
| Controls | Left click swings the drawn melee weapon | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 isAttacking | melee Attack input not received: drawn melee tap/hold logic (UpdateDrawnMeleeCharge) did not start a swing |
| Controls | Right click blocks (melee drawn) | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 isBlocking | Block input (Mouse rightButton / Block action) did not set isBlocking: stamina 0, customAction, not grounded, or still attacking |
| Controls | Middle click does not break gameplay | PASS | start 0.00, min 0.00, max 0.00, end 0.00 none |  |
| Controls | Middle click has no gameplay binding | INFO | InputSystem_Actions Player map binds no action to Mouse/middleButton (only the UI map MiddleClick) |  |
| Controls | Tab (SwitchWeapon) cycles the weapon focus | WARN | Hot Cross weapon focus 0 -> 0, drawn weap2_sword -> weap2_sword | Tab tap did not change focus: only one weapon on the hotbar, Hot Cross not polling, or tap/hold threshold (hold arms) not met |
| Controls | Half-tilt stick walks slower than a run | WARN | start 0.00, min 0.00, max 0.00, end 0.00 m in 1.5s | walk speed not distinct from run (0.0m): analog Move magnitude ignored by the locomotion speed |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block + parry: player defends against enemy melee

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Unblocked baseline (same enemy, same distance) | INFO | hits 1, player hp lost 26.3 |  |
| Block | Holding block negates enemy melee damage | PASS | BLOCK = hold right mouse. isBlocking True (raised in 0.03s), hits 2, blocked 2, player hp lost 0.0 (unblocked 26.3), stamina 984.1 |  |
| Block | Enemy guard-break stagger after a blocked hit | INFO | longest enemy stagger 4.70s (profile blockStaggerSeconds 1.2s) |  |
| Parry | Parry = tap right mouse, timed before the incoming hit (several timings) | INFO | tap -0.60s vs predicted hit (windup 1.41s): blow landed 3.08s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 125.3 // tap -0.40s vs predicted hit (windup 1.41s): blow landed 3.18s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 126.8 // tap -0.25s vs predicted hit (windup 1.41s): blow landed 2.26s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 53.6 // tap -0.15s vs predicted hit (windup 1.41s): blow landed 2.07s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 73.1 // tap -0.08s vs predicted hit (windup 1.41s): blow landed 2.67s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 125.7 // tap -0.02s vs predicted hit (windup 1.41s): blow landed 2.93s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 104.5 // tap +0.10s vs predicted hit (windup 1.41s): blow landed 2.73s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 105.4 | input: PARRY = TAP right mouse (0.08s), BLOCK = HOLD right mouse; profile: parryWindowSeconds 0.2, blockStagger 1.2s, parryStagger 5.0s |
| Parry | A tap inside the parry window parries (and an early/late tap does not) | WARN | no parry observed (0 taps landed inside the window; see sweep rows) | no sampled tap produced a parry: enemy attack timing varies between swings (windup from the baseline swing may not match), or the parry window/stagger in the profile differs from what the rows show |
| Parry | A successful parry staggers the attacker (counter opening) | INFO | no parry happened, so the counter stagger could not be observed |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block: enemy defends against player swings

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Enemy block reduces player melee damage | PASS | open: 23 hits, enemy hp lost 180.8; blocking (animator IsBlocking True, duration 40.0s): 22 hits (4 flagged blocked), enemy hp lost 72.2; block clips: AimedIdle, AimedWalk, AimedWalkBack, AimedWalkLeft, AimedWalkRight, Idle@Pistol |  |
| Parry | Enemy parry | INFO | not implemented: EnemyInvectorCombatBridge only has TryBeginBlock/EndBlock (no parry window/riposte API) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Movement: retreat, push, flank, backstab, lose target

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Movement | Player retreats 9m at 3 m/s: enemy follows | PASS | distance 2.6m -> 1.8m (min 2.0m), states Chase/Attack, attacks 1 |  |
| Movement | Player pushes into the enemy: spacing and facing | PASS | min distance 0.50m, max facing error 0deg, attack-state frames 49, states Attack/Chase |  |
| Movement | Player circles behind the enemy: enemy turns to face | WARN | reached behind at never, enemy faced player (<30deg) after never, max facing error 0deg, attacks while behind 0, player hp lost 0.0, states Attack | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player |
| Movement | Hit from behind vs from the front (player melee, AI off) | INFO | front: 10 hits, 69.7 hp; behind: 17 hits, 123.0 hp; ratio 1.76 | no backstab bonus implemented if ratio is 1.0 |
| Movement | Player disappears 70m away: enemy loses the target | PASS | was aware True, investigate/search after never, back to calm state after 3.1s, states Wander/Chase/ReturnHome |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Play | Approach with W: enemy notices | PASS | walked 21.6m in 11.5s to 8.4m; enemy noticed after 10.6s at 15.3m; state Hold |  |
| Play | Run away with S: enemy pursues | PASS | player backed away for 4.1s (gap now 2.7m), enemy moved 13.3m, hostile state seen True, states Hold/Chase/Attack |  |
| Play | Strafe around the enemy: it turns to face the player | PASS | enemy facing error avg 10deg, max 41deg over 79 frames while strafing D then A, state Attack |  |
| Play | Retreat then re-engage: enemy fights back | PASS | re-engaged to 1.6m, enemy attacks 3, player hp lost 104.7, state Attack |  |
| Play | Jump away: player clears distance | WARN | jump height 0.91m, gap 1.6m -> 2.3m, enemy state Attack | player could not gain distance (enemy kept pace or player blocked) |
| Play | Jetpack escape (hold Space + S) | WARN | peak height gain 0.9m, gap 2.3m -> 1.9m, enemy state Attack | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump |
| Play | Sprint away: enemy loses the target | WARN | lost after never at n/am (sprinting), state Chase, states Wander/Hold/Chase/Attack | enemy kept hostile for 30s of sprinting away: lose-target distance/time too long (senses) or leash not applied |

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | PASS | 1 PASS / 0 FAIL / 0 warn of 1 runtime row(s); weapon reverts recorded this run: 0 |  |
| Fix check | vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons) | PASS | code present: True; console errors across 0 weapon-matrix case(s): 0 |  |
| Fix check | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | WARN | code present: True; dual-slot override case: not run |  |
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 18 value(s) compared on 2 enemies |  |
| Fix check | No circling while unaware (patrol-anchor fix) on every enemy | PASS | 3 PASS / 0 FAIL of 3 enemies |  |
| Fix check | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | FAIL | 0 PASS / 3 FAIL of 3 enemies | Humadroid, Robot, SwordMaster |
| Fix check | Profile adjustments made by earlier audit passes | INFO | none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes |  |

## Per-enemy detail

### Humadroid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 42s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 82ms, 762 components, frame avg 38.2ms max 401.2ms (244 frames), GC collections 0, managed delta 21380KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 223deg, path 12.3m, net 5.7m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.6s (at 1.7m, weapon weap2_sword), first player damage 2.2s, attacks 1, states Patrol/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.2s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 7 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 427, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.1m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | FAIL | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol,Holstered_weap2_sword]; max drawn 1, max holstered 2 | 2 Holstered_ slots active at once |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 197.3 (-2.7); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 197.3 -> 197.3; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | WARN | marks stamped 0 -> 0, body type Android | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | FAIL | loot bag held 0 entries, TryLootAll -> None, left 0; definition random loot count 0-1 | bag spawned but nothing could be looted (empty entries / TryLootAll refused) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.29s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 1 clip(s) observed playing; 18 distinct clips seen in total |  |
| Console | Console errors/warnings | WARN | 0 errors, 1 warning(s) | [Warning] The referenced script on this Behaviour (Game Object '<null>') is missing! |

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

Console during run: 0 error(s), 1 warning(s):
- `[Warning] The referenced script on this Behaviour (Game Object '<null>') is missing!`

### Robot

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `Robot_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 42s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Robot_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=15, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 40ms, 699 components, frame avg 35.6ms max 85.7ms (254 frames), GC collections 0, managed delta 15596KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 156deg, path 10.6m, net 2.7m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.2s (at 1.7m, weapon weap2_sword), first player damage 2.7s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.7s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 47.0 (item dmg 18), attacks 1, hitbox-active frames 6 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.6s, min distance 1.30m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 47.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.3m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 428, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | FAIL | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol,Holstered_weap2_sword]; max drawn 1, max holstered 2 | 2 Holstered_ slots active at once |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 476.3 (-23.7); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 476.3 -> 476.3; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Robot |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | FAIL | loot bag held 0 entries, TryLootAll -> None, left 0; definition random loot count 0-1 | bag spawned but nothing could be looted (empty entries / TryLootAll refused) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.4s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 22 distinct clips seen in total |  |
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

### SwordMaster

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `Humanoid_Enemy` - melee `Blade Of Chaos` / ranged `sci_fi_pistol` - audited in 43s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=Blade Of Chaos (dmg 21), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 34ms, 694 components, frame avg 36.2ms max 83.9ms (249 frames), GC collections 0, managed delta 18768KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 255deg, path 9.6m, net 4.4m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.0s, chase 0.0s, first attack 1.6s (at 1.6m, weapon Blade Of Chaos), first player damage 2.1s, attacks 1, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 30.0 hp in 2.1s using Blade Of Chaos |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 30.0 (item dmg 21), attacks 1, hitbox-active frames 18 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.9s, min distance 1.66m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Blade Of Chaos': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 30.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.7m, first attack at 1.7m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 429, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.3m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Blade_Of_Chaos] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | FAIL | spawn: active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol,Holstered_Blade_Of_Chaos]; max drawn 1, max holstered 2 | 2 Holstered_ slots active at once |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 207.1 (-92.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 207.1 -> 207.1; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 21 distinct clips seen in total |  |
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
