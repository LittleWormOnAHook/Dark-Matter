# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-13 20:21:51 -> 2026-10-13 20:39:19 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 0; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 3
- Unit store: 37 stored unit(s); stamps audit 8A4119 game B26CCB gear 11046A; skip-unchanged is OFF (Full mode)
- Player level raised in memory from 5 for equip gates (restored at the end; nothing saved)
- Mode full: 18 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 17m28s

**3 enemies audited - 6 FAIL, 2 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 16 scenarios): 186 PASS, 16 FAIL, 41 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 226 change(s).

- NEW enemy|Humadroid|perf = PASS
- NEW enemy|Humadroid|melee.miss = INFO
- CHANGED enemy|Humadroid|ranged.damage: PASS -> FAIL
- NEW enemy|Humadroid|ranged.miss = INFO
- CHANGED enemy|Humadroid|vis: PASS -> FAIL
- CHANGED enemy|Humadroid|hitmarks: WARN -> PASS
- NEW enemy|Humadroid|loot = PASS
- CHANGED enemy|Humadroid|console: PASS -> WARN
- NEW enemy|Robot|perf = PASS
- NEW enemy|Robot|melee.miss = INFO
- CHANGED enemy|Robot|ranged.damage: PASS -> FAIL
- NEW enemy|Robot|ranged.miss = INFO
- CHANGED enemy|Robot|vis: PASS -> FAIL
- NEW enemy|Robot|loot = PASS
- NEW enemy|SwordMaster|cfg = PASS
- NEW enemy|SwordMaster|silent.missingscripts = PASS
- NEW enemy|SwordMaster|silent.materials = PASS
- NEW enemy|SwordMaster|silent.animator = PASS
- NEW enemy|SwordMaster|silent.hitboxes = PASS
- NEW enemy|SwordMaster|perf = PASS
- NEW enemy|SwordMaster|unaware = PASS
- NEW enemy|SwordMaster|notice = WARN
- NEW enemy|SwordMaster|natural.damage = INFO
- NEW enemy|SwordMaster|melee.damage = PASS
- NEW enemy|SwordMaster|melee.miss = INFO
- NEW enemy|SwordMaster|melee.vis = INFO
- NEW enemy|SwordMaster|ranged.damage = FAIL
- NEW enemy|SwordMaster|ranged.miss = INFO
- NEW enemy|SwordMaster|ranged.vis = INFO
- NEW enemy|SwordMaster|vis = FAIL
- NEW enemy|SwordMaster|player.dmg = PASS
- NEW enemy|SwordMaster|player.swing = PASS
- NEW enemy|SwordMaster|hitmarks = PASS
- NEW enemy|SwordMaster|loot = PASS
- NEW enemy|SwordMaster|death = PASS
- NEW enemy|SwordMaster|anim = INFO
- NEW enemy|SwordMaster|console = PASS
- CHANGED scenario|Audio: swing / hit sound wiring (static)|audio.swing.callers: FAIL -> PASS
- CHANGED scenario|Audio: swing / hit sound wiring (static)|audio.hit.callers: WARN -> PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.inputdiag = INFO
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.climb = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.systems = INFO
- NEW scenario|Block + parry: player defends against enemy melee|parry.counter = PASS
- CHANGED scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.approach: PASS -> WARN
- CHANGED scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.jump: PASS -> WARN
- NEW scenario|Multi-enemy: FRED x3 and x5|multi.who = INFO
- NEW scenario|Multi-enemy: FRED x3 and x5|multi = WARN
- NEW scenario|Multi-enemy: FRED x3 and x5|multi5 = WARN
- NEW scenario|Multi-enemy: FRED x3 and x5|console = PASS
- NEW scenario|Brain: orbit, toggles, archetypes|brain.orbit.still = PASS
- NEW scenario|Brain: orbit, toggles, archetypes|brain.orbit.strafe = PASS
- NEW scenario|Brain: orbit, toggles, archetypes|brain.toggle.0 = WARN
- NEW scenario|Brain: orbit, toggles, archetypes|brain.toggle.0.state = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.toggle.1 = PASS
- NEW scenario|Brain: orbit, toggles, archetypes|brain.toggle.1.state = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.toggle.2 = PASS
- NEW scenario|Brain: orbit, toggles, archetypes|brain.toggle.2.state = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.toggle.3 = WARN
- NEW scenario|Brain: orbit, toggles, archetypes|brain.toggle.3.state = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.toggle.4 = PASS
- NEW scenario|Brain: orbit, toggles, archetypes|brain.toggle.4.state = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.expect.brainoff = WARN
- NEW scenario|Brain: orbit, toggles, archetypes|brain.expect.diroff = PASS
- NEW scenario|Brain: orbit, toggles, archetypes|brain.expect.legacy = PASS
- NEW scenario|Brain: orbit, toggles, archetypes|brain.arch.Duelist = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.arch.Rusher = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.arch.Berserker = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.arch.Flanker = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.arch.Ambusher = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.arch.Tank = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.arch.Guardian = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.arch.Shooter = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.arch.Kiter = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.arch.Swarmer = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.arch.Stalker = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.pers.Aggressive = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.pers.Cowardly = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.pers.Cautious = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.facts.does = INFO
- NEW scenario|Brain: orbit, toggles, archetypes|brain.facts.doesnot = INFO
- ... 146 more

## Timing

Wall time 1049s (17.5 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Humadroid | enemy | 43.3 | ran |
| Robot | enemy | 42.1 | ran |
| SwordMaster | enemy | 62.8 | ran |
| Camera: faces forward after teleports, zoom in/out | scenario | 8.8 | ran |
| Equip: weapon survives a teleport | scenario | 6.0 | ran |
| Audio: swing / hit sound wiring (static) | scenario | 0.4 | ran |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | scenario | 60.1 | ran |
| Block + parry: player defends against enemy melee | scenario | 52.2 | ran |
| Block: enemy defends against player swings | scenario | 14.3 | ran |
| Movement: retreat, push, flank, backstab, lose target | scenario | 31.9 | ran |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | scenario | 81.3 | ran |
| Multi-enemy: FRED x3 and x5 | scenario | 36.4 | ran |
| Brain: orbit, toggles, archetypes | scenario | 241.5 | ran |
| Player weapons: each melee item vs enemy (swapped in place) | scenario | 36.1 | ran |
| Ammo: default ammo per ranged weapon + ammo-specific FX | scenario | 65.6 | ran |
| Player rig: enemy ranged shot hits a body bone + mark | scenario | 19.0 | ran |
| Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | scenario | 32.9 | ran |
| Behavior tweaks: over / under-exaggerated enemy values (runtime only) | scenario | 194.6 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| scenario|Brain: orbit, toggles, archetypes | setup | 241.7 | 6171 | 39.2 | 914.7 | 0.47 | 0.00 | 14.43 |
| scenario|Behavior tweaks: over / under-exaggerated enemy values (runtime only) | setup | 194.9 | 5135 | 37.9 | 333.2 | 0.44 | 0.00 | 14.74 |
| scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | setup | 81.4 | 2116 | 38.5 | 239.4 | 0.50 | 0.00 | 13.56 |
| scenario|Ammo: default ammo per ranged weapon + ammo-specific FX | setup | 65.9 | 1588 | 41.5 | 717.9 | 0.46 | 0.00 | 14.80 |
| scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | setup | 60.3 | 1978 | 30.5 | 210.6 | 0.35 | 0.00 | 11.24 |
| scenario|Block + parry: player defends against enemy melee | setup | 53.4 | 1570 | 34.0 | 300.7 | 0.40 | 0.00 | 12.73 |
| scenario|Multi-enemy: FRED x3 and x5 | setup | 37.9 | 871 | 43.6 | 393.4 | 0.57 | 0.00 | 17.59 |
| scenario|Player weapons: each melee item vs enemy (swapped in place) | setup | 36.3 | 1128 | 32.2 | 125.0 | 0.38 | 0.00 | 12.50 |
| scenario|Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | setup | 33.1 | 1010 | 32.8 | 110.4 | 0.37 | 0.00 | 13.60 |
| scenario|Movement: retreat, push, flank, backstab, lose target | setup | 32.1 | 768 | 41.9 | 126.3 | 0.48 | 0.00 | 13.97 |
| enemy|SwordMaster | notice/chase/attack | 22.1 | 755 | 29.3 | 201.8 | 0.34 | 0.00 | 12.21 |
| scenario|Player rig: enemy ranged shot hits a body bone + mark | setup | 19.2 | 591 | 32.5 | 129.8 | 0.36 | 0.00 | 13.71 |
| enemy|Humadroid | ranged test | 17.3 | 579 | 30.2 | 235.3 | 0.35 | 0.00 | 12.20 |
| enemy|SwordMaster | ranged test | 17.2 | 589 | 29.5 | 208.0 | 0.34 | 0.00 | 12.06 |
| enemy|Robot | ranged test | 17.2 | 581 | 29.9 | 214.8 | 0.34 | 0.00 | 12.21 |
| scenario|Block: enemy defends against player swings | setup | 15.4 | 429 | 36.0 | 215.9 | 0.44 | 0.00 | 12.74 |
| enemy|Humadroid | unaware 9s (+perf) | 9.3 | 304 | 30.6 | 370.0 | 0.35 | 0.00 | 11.97 |
| scenario|Camera: faces forward after teleports, zoom in/out | setup | 9.1 | 319 | 28.4 | 69.0 | 0.31 | 0.00 | 10.90 |
| enemy|Robot | unaware 9s (+perf) | 9.0 | 309 | 29.2 | 69.0 | 0.34 | 0.00 | 11.87 |
| enemy|SwordMaster | unaware 9s (+perf) | 9.0 | 310 | 29.0 | 70.9 | 0.33 | 0.00 | 11.79 |
| enemy|Humadroid | death/loot | 6.7 | 216 | 31.1 | 146.3 | 0.39 | 0.00 | 11.62 |
| enemy|Robot | death/loot | 6.7 | 215 | 31.2 | 88.9 | 0.33 | 0.00 | 11.91 |
| enemy|SwordMaster | death/loot | 6.7 | 217 | 31.0 | 112.6 | 0.33 | 0.00 | 11.81 |
| scenario|Equip: weapon survives a teleport | setup | 6.2 | 203 | 30.8 | 175.6 | 0.32 | 0.00 | 11.60 |
| enemy|Humadroid | player->enemy | 3.9 | 91 | 42.9 | 80.9 | 0.56 | 0.00 | 13.96 |
| enemy|Robot | player->enemy | 3.8 | 106 | 35.4 | 77.4 | 0.47 | 0.00 | 12.61 |
| enemy|SwordMaster | player->enemy | 3.8 | 105 | 35.4 | 78.1 | 0.46 | 0.00 | 12.64 |
| enemy|SwordMaster | melee test | 3.0 | 87 | 32.0 | 82.7 | 0.42 | 0.00 | 12.48 |
| enemy|Robot | notice/chase/attack | 2.8 | 88 | 32.2 | 72.7 | 0.37 | 0.00 | 12.59 |
| enemy|Humadroid | melee test | 2.5 | 64 | 36.6 | 82.4 | 0.43 | 0.00 | 13.69 |

Whole run (ran units only): 1036s over 28714 frames, avg frame 36.2 ms, physics 0.43 ms, render 0.00 ms, scripts 13.63 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 100 | FAIL | Ranged 'sci_fi_pistol' damages the player | Humadroid, Robot, SwordMaster | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 2 | 85 | FAIL | Exactly one weapon drawn (resolved), other holstered | Humadroid, Robot, SwordMaster | 2 Holstered_ slots active at once | Let EnemyInvectorLoadoutBridge be the only weapon authority; hide every Drawn_/Holstered_ slot except the resolved ones. |
| 3 | 75 | WARN | Player uses melee '2 Hander' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 4 | 75 | WARN | Player uses melee 'Blade Of Chaos' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 5 | 75 | WARN | Player uses melee 'Spear of Fate' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 6 | 75 | WARN | Player uses melee 'Sword of Fear' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 7 | 75 | WARN | Player uses melee 'weap2_sword' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 8 | 75 | WARN | Player uses melee 'weap_two_handed' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 9 | 75 | WARN | Player uses melee 'Wood Axe' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 10 | 60 | WARN | Notice / chase / attack timing (player 10m in front) | SwordMaster | noticed but never attacked within 22s (chase blocked, standoff/hold token, attack range, or cooldown) | Review senses (vision range/FOV/LOS) and aggro rules; check the EnemySenses reference on the prefab. |
| 11 | 55 | WARN | Console errors/warnings | Humadroid | [Warning] The referenced script on this Behaviour (Game Object '<null>') is missing! | Fix the exception at the listed stack frame (null-guard or initialise the field); it repeats on every spawn. |
| 12 | 30 | WARN | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | Multi-enemy: FRED x3 and x5 | 136/381 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Add enemy separation and make the attack token rotate between enemies. |
| 13 | 20 | WARN | Player circles behind the enemy: enemy turns to face | Movement: retreat, push, flank, backstab, lose target | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 14 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 15 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Fire: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 16 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Ice: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 17 | 20 | FAIL | Enemy ranged shot ('survival_rifle') resolves to a player body hitbox | Player rig: enemy ranged shot hits a body bone + mark | enemy ranged shot did no damage to the player: the shot missed or the hitscan still passes through (the rig supersedes the capsule but nothing resolves on the bone hitboxes) | Inspect the measured value and cause on the detail row. |
| 18 | 20 | FAIL | Aim follows the moving enemy: chest | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 19 | 20 | FAIL | Aim follows the moving enemy: head | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 20 | 20 | FAIL | Aim follows the moving enemy: arm | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 21 | 20 | FAIL | Aim follows the moving enemy: leg | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 22 | 20 | FAIL | Aim follows the moving enemy: foot | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 23 | 20 | FAIL | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | Regression of earlier fixes and tweaks | Humadroid, Robot, SwordMaster | Inspect the measured value and cause on the detail row. |
| 24 | -5 | WARN | Camera zooms in and out | Camera: faces forward after teleports, zoom in/out | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state | Inspect the measured value and cause on the detail row. |
| 25 | -5 | WARN | Left Ctrl crouches | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) | Inspect the measured value and cause on the detail row. |
| 26 | -5 | WARN | Left Alt dash | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dash did not move the player even though the skill gate is unlocked for this run (see ctl.systems row): DMDashController disabled, on cooldown, stamina, or the Dash action not bound to Left Alt | Inspect the measured value and cause on the detail row. |
| 27 | -5 | WARN | Hold Space after jumping boosts (jetpack) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | no sustained lift although the jetpack skill gate is unlocked for this run (see ctl.systems row): fuel 0, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player | Inspect the measured value and cause on the detail row. |
| 28 | -5 | WARN | Approach with W: enemy notices | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) | Inspect the measured value and cause on the detail row. |
| 29 | -5 | WARN | Jump away: player clears distance | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | player could not gain distance (enemy kept pace or player blocked) | Inspect the measured value and cause on the detail row. |
| 30 | -5 | WARN | Jetpack escape (hold Space + S) | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump | Inspect the measured value and cause on the detail row. |
| 31 | -5 | WARN | Enemy returns to its post after losing the player | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy did not walk back to its spawn within 30s: ReturnHome blocked, leash undefined, or it stays in Search/Investigate | Inspect the measured value and cause on the detail row. |
| 32 | -5 | WARN | 5 FREDs at once for 18s: token rotation, spacing, no stacking/orbiting | Multi-enemy: FRED x3 and x5 | 96/394 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |
| 33 | -5 | WARN | 3 FREDs, baseline (brain on, director on) | Brain: orbit, toggles, archetypes | 68/274 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |
| 34 | -5 | WARN | 3 FREDs, legacyRingSlotRotation=true | Brain: orbit, toggles, archetypes | 95/284 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |
| 35 | -5 | WARN | enableUtilityBrain=false stops the brain acting (no Press/Defend/Retreat/Hold) | Brain: orbit, toggles, archetypes | the brain still selected actions with enableUtilityBrain=false (a per-type ProfileOverride, or a cached decision) | Inspect the measured value and cause on the detail row. |
| 36 | -5 | WARN | Player '2 Hander' swing plays a swish | Player weapons: each melee item vs enemy (swapped in place) | no swing started, so the swing sound could not be judged (click input not received) | Inspect the measured value and cause on the detail row. |
| 37 | -5 | WARN | Player 'Blade Of Chaos' swing plays a swish | Player weapons: each melee item vs enemy (swapped in place) | no swing started, so the swing sound could not be judged (click input not received) | Inspect the measured value and cause on the detail row. |
| 38 | -5 | WARN | Player 'Spear of Fate' swing plays a swish | Player weapons: each melee item vs enemy (swapped in place) | no swing started, so the swing sound could not be judged (click input not received) | Inspect the measured value and cause on the detail row. |
| 39 | -5 | WARN | Player 'Sword of Fear' swing plays a swish | Player weapons: each melee item vs enemy (swapped in place) | no swing started, so the swing sound could not be judged (click input not received) | Inspect the measured value and cause on the detail row. |
| 40 | -5 | WARN | Player 'weap2_sword' swing plays a swish | Player weapons: each melee item vs enemy (swapped in place) | no swing started, so the swing sound could not be judged (click input not received) | Inspect the measured value and cause on the detail row. |
| 41 | -5 | WARN | Player 'weap_two_handed' swing plays a swish | Player weapons: each melee item vs enemy (swapped in place) | no swing started, so the swing sound could not be judged (click input not received) | Inspect the measured value and cause on the detail row. |
| 42 | -5 | WARN | Player 'Wood Axe' swing plays a swish | Player weapons: each melee item vs enemy (swapped in place) | no swing started, so the swing sound could not be judged (click input not received) | Inspect the measured value and cause on the detail row. |
| 43 | -5 | WARN | survival_rifle + DMAmmoFxProfile_Standard: magazine fills to capacity | Ammo: default ammo per ranged weapon + ammo-specific FX | RefillMagazineFromInventory did not reach capacity (reserve exhausted or capacity resolves differently: ammo.magazineSize vs weapon.magazineSize) | Inspect the measured value and cause on the detail row. |
| 44 | -5 | WARN | survival_rifle + DMAmmoFxProfile_Standard: hit rate over aimed shots (shots may miss by design) | Ammo: default ammo per ranged weapon + ammo-specific FX | hit rate well below what the accuracy settings suggest | Inspect the measured value and cause on the detail row. |
| 45 | -5 | WARN | sci_fi_pistol + DMAmmoFxProfile_Explosive: damages the enemy | Ammo: default ammo per ranged weapon + ammo-specific FX | no hit in the few shots fired: spread/accuracy can miss, too few shots to judge | Inspect the measured value and cause on the detail row. |
| 46 | -5 | WARN | sci_fi_pistol + DMAmmoFxProfile_Fire: damages the enemy | Ammo: default ammo per ranged weapon + ammo-specific FX | no hit in the few shots fired: spread/accuracy can miss, too few shots to judge | Inspect the measured value and cause on the detail row. |
| 47 | -5 | WARN | sci_fi_pistol + DMAmmoFxProfile_Ice: damages the enemy | Ammo: default ammo per ranged weapon + ammo-specific FX | no hit in the few shots fired: spread/accuracy can miss, too few shots to judge | Inspect the measured value and cause on the detail row. |
| 48 | -5 | WARN | Player hit marks are removed automatically after the configured lifetime | Player rig: enemy ranged shot hits a body bone + mark | no player mark was stamped, so expiry could not be checked | Inspect the measured value and cause on the detail row. |
| 49 | -5 | WARN | Engagement tuning (3 FREDs): low (token hold, ring radius, holder spacing) | Behavior tweaks: over / under-exaggerated enemy values (runtime only) | degenerate: attack spam (many simultaneous attackers) | Inspect the measured value and cause on the detail row. |
| 50 | -5 | WARN | Engagement tuning (3 FREDs): suggested safe range | Behavior tweaks: over / under-exaggerated enemy values (runtime only) | keep this group between the normal value and about halfway to the degenerate end (high: x2, low: x0.6 of normal) until a finer sweep is run | Inspect the measured value and cause on the detail row. |
| 51 | -5 | WARN | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 52 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 53 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Humadroid | weap2_sword | sci_fi_pistol | PASS | **FAIL** | PASS | PASS | PASS | **FAIL** | PASS | PASS | PASS | warn |
| Robot | weap2_sword | sci_fi_pistol | PASS | **FAIL** | PASS | PASS | PASS | **FAIL** | PASS | PASS | PASS | PASS |
| SwordMaster | Blade Of Chaos | sci_fi_pistol | PASS | **FAIL** | PASS | warn | PASS | **FAIL** | PASS | PASS | PASS | PASS |

## Weapons that deal NO damage (headline)

- **Humadroid** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 517, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised)
- **Robot** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 507, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised)
- **SwordMaster** - ranged `sci_fi_pistol`: player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 523, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25. **Probable cause:** attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised)

## Scenarios (block/parry, movement, multi-enemy, player weapons, performance)

| Scenario | PASS | FAIL | warn | time |
|---|---|---|---|---|
| Camera: faces forward after teleports, zoom in/out | 2 | 0 | 1 | 9s |
| Equip: weapon survives a teleport | 2 | 0 | 0 | 6s |
| Audio: swing / hit sound wiring (static) | 4 | 0 | 0 | 0s |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | 18 | 0 | 3 | 60s |
| Block + parry: player defends against enemy melee | 4 | 0 | 0 | 52s |
| Block: enemy defends against player swings | 2 | 0 | 0 | 14s |
| Movement: retreat, push, flank, backstab, lose target | 4 | 0 | 1 | 32s |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | 5 | 0 | 4 | 81s |
| Multi-enemy: FRED x3 and x5 | 1 | 0 | 2 | 36s |
| Brain: orbit, toggles, archetypes | 9 | 0 | 3 | 241s |
| Player weapons: each melee item vs enemy (swapped in place) | 4 | 0 | 14 | 36s |
| Ammo: default ammo per ranged weapon + ammo-specific FX | 54 | 3 | 5 | 66s |
| Player rig: enemy ranged shot hits a body bone + mark | 2 | 1 | 1 | 19s |
| Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | 1 | 5 | 0 | 33s |
| Behavior tweaks: over / under-exaggerated enemy values (runtime only) | 27 | 0 | 2 | 195s |
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
| Controls | Input path diagnostics (W held 0.35s) | INFO | isFocused True, cursor Locked, keyboard id 1 enabled True W pressed False, mouse enabled, keyboards 1, gamepads 0, InputSystem IgnoreFocus/AllDeviceInputAlwaysGoesToGameView, Player_v7 Combat Variant: scheme 'Keyboard&Mouse', enabled True, actions enabled True, devices [Keyboard,Mouse], Move (0.00, 0.00) // while W held: isFocused True, cursor Locked, keyboard id 1 enabled True W pressed False, mouse enabled, keyboards 1, gamepads 0, InputSystem IgnoreFocus/AllDeviceInputAlwaysGoesToGameView, Player_v7 Combat Variant: scheme 'Keyboard&Mouse', enabled True, actions enabled True, devices [Keyboard,Mouse], Move (0.00, 0.00) |  |
| Controls | W moves the player forward (camera direction) | PASS | start 0.00, min 0.00, max 1.94, end 2.02 m along forward |  |
| Controls | S moves the player back | PASS | start 0.00, min -1.91, max 0.00, end -1.96 m along forward |  |
| Controls | A strafes left | PASS | start 0.00, min 0.00, max 1.92, end 1.97 m along left |  |
| Controls | D strafes right | PASS | start 0.00, min 0.00, max 1.91, end 2.04 m along right |  |
| Controls | W run speed (reference for sprint) | PASS | start 0.00, min 0.00, max 2.55, end 2.59 m in 1.5s |  |
| Controls | Shift + W sprints faster than W | PASS | start 0.00, min 0.00, max 4.93, end 5.09 m in 1.5s |  |
| Controls | Space jumps | PASS | start 21.23, min 21.23, max 1021.72, end 21.23 m height (+1000 while isJumping) |  |
| Controls | Left Ctrl crouches | WARN | start 0.00, min 0.00, max 0.00, end 0.00 isCrouching | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) |
| Controls | Crouch through the controller code path (cc.Crouch) | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isCrouching |  |
| Controls | Q dodge / roll | PASS | start 0.00, min 0.00, max 14.14, end 14.21 m or isRolling |  |
| Controls | Climbing unlocked and controller enabled (not driven: needs a wall) | PASS | DMPlayerSystemsProfile present: ClimbEnabled=True, DashEnabled=True, JetpackEnabled=True; DMClimbController enabled, DMDashController enabled, DMJetpackController enabled |  |
| Controls | Movement systems unlocked for the run (jetpack, dash, climb) | INFO | DMPlayerSystemsProfile present: ClimbEnabled=True, DashEnabled=True, JetpackEnabled=True; DMClimbController enabled, DMDashController enabled, DMJetpackController enabled. The audit unlocks these in memory only (see environment notes) and restores them at exit. |  |
| Controls | Left Alt dash | WARN | start 0.00, min 0.00, max 1.16, end 1.21 m | Dash did not move the player even though the skill gate is unlocked for this run (see ctl.systems row): DMDashController disabled, on cooldown, stamina, or the Dash action not bound to Left Alt |
| Controls | Hold Space after jumping boosts (jetpack) | WARN | start 21.23, min 21.23, max 21.85, end 21.23 m height | no sustained lift although the jetpack skill gate is unlocked for this run (see ctl.systems row): fuel 0, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player |
| Controls | Mouse look left/right orbits the camera | PASS | start 0.00, min 0.00, max 109.17, end 112.17 deg yaw |  |
| Controls | Mouse look up/down tilts the camera | PASS | start 18.00, min 0.00, max 49.93, end 49.95 deg pitch |  |
| Controls | Left click swings the drawn melee weapon | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isAttacking |  |
| Controls | Right click blocks (melee drawn) | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isBlocking |  |
| Controls | Middle click does not break gameplay | PASS | start 0.00, min 0.00, max 0.00, end 0.00 none |  |
| Controls | Middle click has no gameplay binding | INFO | InputSystem_Actions Player map binds no action to Mouse/middleButton (only the UI map MiddleClick) |  |
| Controls | Tab (SwitchWeapon) cycles the weapon focus | PASS | Hot Cross weapon focus 0 -> 1, drawn weap2_sword -> sci_fi_pistol |  |
| Controls | Half-tilt stick walks slower than a run | PASS | start 0.00, min 0.00, max 0.80, end 0.84 m in 1.5s |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block + parry: player defends against enemy melee

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Unblocked baseline (same enemy, same distance) | INFO | hits 1, player hp lost 26.0 |  |
| Block | Holding block negates enemy melee damage | PASS | BLOCK = hold right mouse. isBlocking True (raised in 0.03s), hits 2, blocked 2, player hp lost 0.0 (unblocked 26.0), stamina 989.8 |  |
| Block | Enemy guard-break stagger after a blocked hit | INFO | longest enemy stagger 1.18s (profile blockStaggerSeconds 1.2s) |  |
| Parry | Parry = tap right mouse, timed before the incoming hit (several timings) | INFO | tap -0.60s vs predicted hit (windup 0.60s): blow landed 3.48s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 73.0 // tap -0.40s vs predicted hit (windup 0.60s): blow landed 3.55s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 125.0 // tap -0.25s vs predicted hit (windup 0.60s): blow landed 0.11s after the tap -> PARRY, enemy stagger 3.46s, hp lost 0.0 // tap -0.15s vs predicted hit (windup 0.60s): blow landed 2.91s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 101.1 // tap -0.08s vs predicted hit (windup 0.60s): blow landed 1.91s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 78.3 // tap -0.02s vs predicted hit (windup 0.60s): blow landed 3.10s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 99.5 // tap +0.10s vs predicted hit (windup 0.60s): blow landed 2.61s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 73.6 | input: PARRY = TAP right mouse (0.08s), BLOCK = HOLD right mouse; profile: parryWindowSeconds 0.2, blockStagger 1.2s, parryStagger 5.0s |
| Parry | A tap inside the parry window parries (and an early/late tap does not) | PASS | 1 parry(ies) from taps landing inside the 0.2s window (of 1 such taps), 0 outside it |  |
| Parry | A successful parry staggers the attacker (counter opening) | PASS | 1 parry(ies) left the enemy staggered (longest values in the sweep rows) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block: enemy defends against player swings

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Enemy block reduces player melee damage | PASS | open: 32 hits, enemy hp lost 105.9; blocking (animator IsBlocking True, duration 40.0s): 39 hits (4 flagged blocked), enemy hp lost 83.0; block clips: AimedWalkRight_InPlace, AimedIdle, AimedWalk, AimedWalkBack, AimedWalkLeft, AimedWalkRight |  |
| Parry | Enemy parry | INFO | not implemented: EnemyInvectorCombatBridge only has TryBeginBlock/EndBlock (no parry window/riposte API) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Movement: retreat, push, flank, backstab, lose target

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Movement | Player retreats 9m at 3 m/s: enemy follows | PASS | distance 3.5m -> 2.1m (min 2.1m), states Chase/Attack, attacks 0 |  |
| Movement | Player pushes into the enemy: spacing and facing | PASS | min distance 0.50m, max facing error 0deg, attack-state frames 62, states Attack/Chase |  |
| Movement | Player circles behind the enemy: enemy turns to face | WARN | reached behind at never, enemy faced player (<30deg) after never, max facing error 0deg, attacks while behind 0, player hp lost 0.0, states Attack | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player |
| Movement | Hit from behind vs from the front (player melee, AI off) | INFO | front: 29 hits, 231.3 hp; behind: 30 hits, 160.6 hp; ratio 0.69 | no backstab bonus implemented if ratio is 1.0 |
| Movement | Player disappears 70m away: enemy loses the target | PASS | was aware True, investigate/search after never, back to calm state after 3.5s, states Chase/ReturnHome |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Play | Approach with W: enemy notices | WARN | walked 21.2m in 14.0s to 8.8m; enemy noticed after never at n/a; state Wander | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) |
| Play | Run away with S: enemy pursues | PASS | player backed away for 4.0s (gap now 5.4m), enemy moved 8.4m, hostile state seen True, states Wander/Hold/Chase |  |
| Play | Strafe around the enemy: it turns to face the player | PASS | enemy facing error avg 6deg, max 28deg over 98 frames while strafing D then A, state Attack |  |
| Play | Retreat then re-engage: enemy fights back | PASS | re-engaged to 1.4m, enemy attacks 3, player hp lost 78.6, state Attack |  |
| Play | Jump away: player clears distance | WARN | jump height 0.91m, gap 1.4m -> 2.4m, enemy state Attack | player could not gain distance (enemy kept pace or player blocked) |
| Play | Jetpack escape (hold Space + S) | WARN | peak height gain 0.9m, gap 2.4m -> 2.0m, enemy state Attack | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump |
| Play | Sprint away: enemy loses the target | PASS | lost after 11.6s at 4.3m (sprinting), state Chase, states Wander/Hold/Chase/Attack/ReturnHome |  |
| Play | Enemy returns to its post after losing the player | WARN | back home after never, now 65.9m from spawn, state Attack | enemy did not walk back to its spawn within 30s: ReturnHome blocked, leash undefined, or it stays in Search/Investigate |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Multi-enemy: FRED x3 and x5

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Multi-enemy | Enemy used | INFO | HumanoidEnemy_Invector (FRED / Hybrid Droid) |  |
| Multi-enemy | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 2 (136/381 frames with >1), distinct attackers 3/3, attacker hand-offs 8, attack-state frames [198,194,250], chase/hold frames [180,184,128], closest pair 1.52m, closest to player 1.16m, orbit around player avg 36deg max 61deg (net 2/41/-3), player hp lost 104, brain actions Press:722/Hold:412/None:9 | 136/381 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Multi-enemy | 5 FREDs at once for 18s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 2 (96/394 frames with >1), distinct attackers 5/5, attacker hand-offs 10, attack-state frames [131,214,105,111,84], chase/hold frames [260,177,286,280,307], closest pair 0.84m, closest to player 1.16m, orbit around player avg 43deg max 80deg (net 78/-67/-22/33/1), player hp lost 26, brain actions Hold:1206/Press:749/None:15 | 96/394 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Brain: orbit, toggles, archetypes

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Brain | Single FRED, player standing still: does not orbit the player | PASS | notice 0.1s, chase 0.1s, first attack 1.1s, attacks 4, dmg 229, min dist 1.2m, path 6.2m, yaw 49deg, orbit around player net 47deg / abs 47deg, avg facing error 0deg, brain Press:278, states Chase/Attack |  |
| Brain | Single FRED, player strafes around it: keeps facing the player, does not orbit on its own | PASS | player circled 4.0m radius for 9s; enemy avg facing error 5deg, within 40deg 100% of frames, enemy angular travel around the player 281deg, states Attack/Chase, path 12.0m |  |
| Multi-enemy | 3 FREDs, baseline (brain on, director on) | WARN | max simultaneous attackers 2 (68/274 frames with >1), distinct attackers 3/3, attacker hand-offs 7, attack-state frames [161,136,148], chase/hold frames [110,135,123], closest pair 2.04m, closest to player 1.16m, orbit around player avg 15deg max 44deg (net 0/-43/-1), player hp lost 0, brain actions Press:539/Hold:274/None:9 | 68/274 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:539/Hold:274/None:9 |  |
| Multi-enemy | 3 FREDs, enableUtilityBrain=false | PASS | max simultaneous attackers 2 (65/291 frames with >1), distinct attackers 3/3, attacker hand-offs 6, attack-state frames [172,151,136], chase/hold frames [117,138,153], closest pair 2.01m, closest to player 1.16m, orbit around player avg 1deg max 1deg (net 0/0/-1), player hp lost 26, brain actions None:866/Press:7 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions None:866/Press:7 |  |
| Multi-enemy | 3 FREDs, enableEngagementDirector=false | PASS | max simultaneous attackers 3 (181/252 frames with >1), distinct attackers 3/3, attacker hand-offs 9, attack-state frames [210,215,214], chase/hold frames [39,34,35], closest pair 2.34m, closest to player 1.34m, orbit around player avg 1deg max 2deg (net -1/1/2), player hp lost 104, brain actions Press:534/None:222 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = False; brain actions Press:534/None:222 |  |
| Multi-enemy | 3 FREDs, legacyRingSlotRotation=true | WARN | max simultaneous attackers 2 (95/284 frames with >1), distinct attackers 3/3, attacker hand-offs 5, attack-state frames [184,186,88], chase/hold frames [99,97,195], closest pair 0.82m, closest to player 1.16m, orbit around player avg 32deg max 48deg (net 13/37/-37), player hp lost 52, brain actions Press:537/Hold:312/None:3 | 95/284 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:537/Hold:312/None:3 |  |
| Multi-enemy | 3 FREDs, brain off + director off | PASS | max simultaneous attackers 3 (189/252 frames with >1), distinct attackers 3/3, attacker hand-offs 12, attack-state frames [216,218,221], chase/hold frames [35,33,30], closest pair 2.32m, closest to player 1.34m, orbit around player avg 0deg max 0deg (net 0/0/0), player hp lost 104, brain actions Press:397/None:359 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = False; brain actions Press:397/None:359 |  |
| Brain | enableUtilityBrain=false stops the brain acting (no Press/Defend/Retreat/Hold) | WARN | distinct brain actions: brain on 2 (None:9/Hold:274/Press:539), brain off 1 (None:866/Press:7) | the brain still selected actions with enableUtilityBrain=false (a per-type ProfileOverride, or a cached decision) |
| Brain | enableEngagementDirector=false: no single-attacker token, enemies fall back to the old behaviour | PASS | with the director on: max simultaneous attackers 2 (68 frames with >1); director off: 3 (181 frames with >1), distinct attackers 3 vs 3, hand-offs 7 vs 9 |  |
| Brain | legacyRingSlotRotation=true restores the old ring-slot rotation (more orbiting than the director) | PASS | orbit around the player avg 15deg (director) vs 32deg (legacy ring), distinct attackers 3 vs 3, hand-offs 7 vs 5, max simultaneous attackers 2 vs 2 |  |
| Brain | Archetype Duelist: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.9s, attacks 5, dmg 152, min dist 1.2m, path 4.8m, yaw 1deg, orbit around player net -1deg / abs 1deg, avg facing error 0deg, brain Press:273, states Chase/Attack |  |
| Brain | Archetype Rusher: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 5, dmg 178, min dist 1.2m, path 4.3m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:255/None:1, states Chase/Attack |  |
| Brain | Archetype Berserker: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 0.9s, attacks 5, dmg 181, min dist 1.2m, path 5.0m, yaw 21deg, orbit around player net 21deg / abs 21deg, avg facing error 0deg, brain Press:277/None:1, states Chase/Attack |  |
| Brain | Archetype Flanker: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 0.7s, attacks 4, dmg 179, min dist 1.2m, path 5.3m, yaw 54deg, orbit around player net 2deg / abs 55deg, avg facing error 0deg, brain Press:276, states Chase/Attack |  |
| Brain | Archetype Ambusher: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.7s, attacks 5, dmg 130, min dist 1.2m, path 6.1m, yaw 19deg, orbit around player net -18deg / abs 19deg, avg facing error 0deg, brain Press:288, states Chase/Attack |  |
| Brain | Archetype Tank: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.7s, attacks 4, dmg 229, min dist 1.3m, path 6.2m, yaw 82deg, orbit around player net -34deg / abs 82deg, avg facing error 0deg, brain Press:270/None:2, states Wander/Chase/Attack |  |
| Brain | Archetype Guardian: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 4, dmg 204, min dist 1.2m, path 9.6m, yaw 42deg, orbit around player net -42deg / abs 42deg, avg facing error 0deg, brain Press:258/Defend:19, states Chase/Attack/Defensive |  |
| Brain | Archetype Shooter: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 0.8s, attacks 4, dmg 205, min dist 1.2m, path 5.6m, yaw 31deg, orbit around player net -31deg / abs 31deg, avg facing error 0deg, brain Press:277, states Chase/Attack |  |
| Brain | Archetype Kiter: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 4, dmg 198, min dist 1.2m, path 6.9m, yaw 24deg, orbit around player net 23deg / abs 24deg, avg facing error 0deg, brain Press:292/None:2, states Wander/Chase/Attack |  |
| Brain | Archetype Swarmer: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 0.8s, attacks 5, dmg 158, min dist 1.2m, path 5.9m, yaw 36deg, orbit around player net 36deg / abs 36deg, avg facing error 0deg, brain Press:289/None:1, states Chase/Attack |  |
| Brain | Archetype Stalker: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 4, dmg 257, min dist 1.3m, path 4.0m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:272/None:1, states Chase/Attack |  |
| Brain | Personality Aggressive (Duelist): brain actions and attack rate | INFO | notice 0.0s, chase 0.0s, first attack 0.8s, attacks 4, dmg 225, min dist 1.2m, path 4.9m, yaw 35deg, orbit around player net 35deg / abs 35deg, avg facing error 0deg, brain Press:280, states Chase/Attack |  |
| Brain | Personality Cowardly (Duelist): brain actions and attack rate | INFO | notice 0.1s, chase 0.1s, first attack 0.7s, attacks 4, dmg 231, min dist 1.2m, path 5.4m, yaw 62deg, orbit around player net -62deg / abs 62deg, avg facing error 0deg, brain Press:275, states Chase/Attack |  |
| Brain | Personality Cautious (Duelist): brain actions and attack rate | INFO | notice 0.0s, chase 0.0s, first attack 1.3s, attacks 4, dmg 203, min dist 1.2m, path 6.3m, yaw 26deg, orbit around player net -26deg / abs 26deg, avg facing error 0deg, brain Press:274/None:1, states Chase/Attack |  |
| Brain | Brain DOES | INFO | scores Press / Defend / Retreat / Hold every decisionInterval from archetype rows (press, defend, retreat, tokenBias, holderTaunt, holderFeint) x personality multipliers x health condition (Healthy/Injured/SeverelyInjured/Critical); feeds the engagement director a token bias; scales holder idle actions (sidestep / taunt / feint); issues defend requests (block vs dodge) and retreat windows; reacts to intentional melee misses (miss recovery). Observed actions per archetype are in the rows above. |  |
| Brain | Brain does NOT | INFO | no weapon choice: DMEnemyBrain contains no weapon-switch call (weapon selection stays with the loadout bridge / combat bridge; brain-driven weapon choice and grenade throwing are deferred). It does not move the enemy itself (movement stays in EnemyAiController / the engagement director), does not pick targets, and does nothing when enableUtilityBrain is off. |  |
| Brain | Toggle / archetype tests left nothing on disk | PASS | Resources/Combat profiles, Data/Enemies definitions and enemy prefabs hashed before and after: identical |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player weapons: each melee item vs enemy (swapped in place)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Audio | Player '2 Hander' swing plays a swish | WARN | 0/4 clicks started a swing; 1 non-ambient audio start(s) outside the hit windows: SfxPool_4:neverworld | no swing started, so the swing sound could not be judged (click input not received) |
| Player weapons | Player uses melee '2 Hander' on the enemy | WARN | enemy hp lost 0.0 (item dmg 28), hits 0, player right weapon Drawn_2_Hander | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged |
| Audio | Player 'Blade Of Chaos' swing plays a swish | WARN | 0/4 clicks started a swing; 1 non-ambient audio start(s) outside the hit windows: SfxPool_0:neverworld | no swing started, so the swing sound could not be judged (click input not received) |
| Player weapons | Player uses melee 'Blade Of Chaos' on the enemy | WARN | enemy hp lost 0.0 (item dmg 21), hits 0, player right weapon Drawn_Blade_of_Chaos | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged |
| Audio | Player 'Death Axe' swing plays a swish | PASS | 1/4 clicks started a swing; 3 non-ambient audio start(s) outside the hit windows: SfxPool_3:Melee Weapon Air Swing 1, SfxPool_2:Melee Weapon Attack 3, SfxPool_0:Melee Weapon Attack 1 |  |
| Audio | Player 'Death Axe' hitting the enemy plays a hit sound | PASS | 10/10 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Death Axe' on the enemy | PASS | enemy hp lost 27.6 (item dmg 18), hits 10, player right weapon Drawn_Death_Axe |  |
| Audio | Player 'Spear of Fate' swing plays a swish | WARN | 0/4 clicks started a swing; 1 non-ambient audio start(s) outside the hit windows: SfxPool_9:neverworld | no swing started, so the swing sound could not be judged (click input not received) |
| Player weapons | Player uses melee 'Spear of Fate' on the enemy | WARN | enemy hp lost 0.0 (item dmg 16), hits 0, player right weapon Drawn_Spear_of_Fate | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged |
| Audio | Player 'Sword of Fear' swing plays a swish | WARN | 0/4 clicks started a swing; 0 non-ambient audio start(s) outside the hit windows: none | no swing started, so the swing sound could not be judged (click input not received) |
| Player weapons | Player uses melee 'Sword of Fear' on the enemy | WARN | enemy hp lost 0.0 (item dmg 15), hits 0, player right weapon Drawn_Sword_of_Fear | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged |
| Audio | Player 'weap2_sword' swing plays a swish | WARN | 0/4 clicks started a swing; 1 non-ambient audio start(s) outside the hit windows: SfxPool_6:neverworld | no swing started, so the swing sound could not be judged (click input not received) |
| Player weapons | Player uses melee 'weap2_sword' on the enemy | WARN | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon Drawn_weap2_sword | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged |
| Audio | Player 'weap_two_handed' swing plays a swish | WARN | 0/4 clicks started a swing; 1 non-ambient audio start(s) outside the hit windows: SfxPool_2:neverworld | no swing started, so the swing sound could not be judged (click input not received) |
| Player weapons | Player uses melee 'weap_two_handed' on the enemy | WARN | enemy hp lost 0.0 (item dmg 28), hits 0, player right weapon Drawn_Two_Handed_Sword | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged |
| Audio | Player 'Wood Axe' swing plays a swish | WARN | 0/4 clicks started a swing; 0 non-ambient audio start(s) outside the hit windows: none | no swing started, so the swing sound could not be judged (click input not received) |
| Player weapons | Player uses melee 'Wood Axe' on the enemy | WARN | enemy hp lost 0.0 (item dmg 26), hits 0, player right weapon Drawn_Wood_Axe | no swing started: the left-click never reached the player (harness/input, not the weapon); the weapon could not be judged |
| Player weapons | Player uses 'DM_Mining_Tool' | INFO | mining tool: Plasma Fuel scanner/laser, not a damage weapon (damage 0 by design) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Ammo: default ammo per ranged weapon + ammo-specific FX

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Ammo | Ammo items and ranged weapons found | INFO | 10 ammo item(s): DMAmmoFxProfile_CarveExperiment(Gunpowder), DMAmmoFxProfile_Electricity(Electricity), DMAmmoFxProfile_Explosive(Explosive), DMAmmoFxProfile_Fire(Fire), DMAmmoFxProfile_Ice(Ice), DMAmmoFxProfile_Ion(Ion), DMAmmoFxProfile_Laser(Laser), DMAmmoFxProfile_Plasma(Plasma), DMAmmoFxProfile_Resonance Stabilizer(ResonanceStabilizer), DMAmmoFxProfile_Standard(Gunpowder); 2 ranged weapon(s): sci_fi_pistol, survival_rifle |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: fires (rounds consumed) | PASS | 1 trigger pull(s), rounds 30 -> 29, shot interval 0.20s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: damages the enemy | PASS | enemy hp lost 23.4 (weapon dmg 14, ammo type Gunpowder, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: hit rate over aimed shots (shots may miss by design) | PASS | 1/1 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: hit marks / FX | PASS | hit marks stamped 0 -> 1, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 0 -> 1, enemy hp lost 23.4 |  |
| Player weapons | Player uses ranged 'sci_fi_pistol' on the enemy (default ammo) | PASS | enemy hp lost 23.4 (item dmg 14), rounds consumed 1 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 28/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: magazine fills to capacity | WARN | magazine 28/30 after top-up (90 rounds were in the inventory) | RefillMagazineFromInventory did not reach capacity (reserve exhausted or capacity resolves differently: ammo.magazineSize vs weapon.magazineSize) |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: fires (rounds consumed) | PASS | 15 trigger pull(s), rounds 28 -> 13, shot interval 0.20s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: damages the enemy | PASS | enemy hp lost 323.0 (weapon dmg 18, ammo type Gunpowder, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: hit rate over aimed shots (shots may miss by design) | WARN | 3/15 aimed shots hit (20 %); weapon accuracy 82 (effective incl. skill bonus 75), ammo accuracy 75, spread 0.9 deg (ammo 1.4), close-range scale 0.15 within 12m, hip multiplier 1; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: hit marks / FX | PASS | hit marks stamped 2 -> 18, body type Humanoid |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 2 -> 18, enemy hp lost 323.0 |  |
| Player weapons | Player uses ranged 'survival_rifle' on the enemy (default ammo) | PASS | enemy hp lost 323.0 (item dmg 18), rounds consumed 15 |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_CarveExperiment', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: fires | PASS | 2 trigger pull(s), rounds 30 -> 28 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: damages the enemy | PASS | enemy hp lost 9.8 (ammo type Gunpowder) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: hit rate over aimed shots (shots may miss by design) | PASS | 1/2 aimed shots hit (50 %); weapon accuracy 70 (effective incl. skill bonus 92), ammo accuracy 92, spread 1.8 deg (ammo 0.8), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: hit marks / FX | PASS | hit marks stamped 18 -> 19, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Projectile, useHitMarks=True; hit marks 18 -> 19, enemy hp lost 9.8 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Electricity', ammoType Electricity |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: fires | PASS | 1 trigger pull(s), rounds 30 -> 29 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: damages the enemy | PASS | enemy hp lost 13.5 (ammo type Electricity) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: hit rate over aimed shots (shots may miss by design) | PASS | 1/1 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: hit marks / FX | PASS | hit marks stamped 19 -> 20, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 19 -> 20, enemy hp lost 13.5 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Explosive', ammoType Explosive |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires | FAIL | 0 trigger pull(s), rounds 30 -> 30 | no round consumed with this ammo type loaded |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: damages the enemy | WARN | enemy hp lost 0.0 (ammo type Explosive) | no hit in the few shots fired: spread/accuracy can miss, too few shots to judge |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: hit rate over aimed shots (shots may miss by design) | INFO | 0/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: hit marks / FX | INFO | hit marks stamped 20 -> 20, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: per-ammo FX rules (blood / burn / glow / explosive handling) | INFO | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=True; hit marks 20 -> 20, enemy hp lost 0.0 | no hit to evaluate the rules against |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Fire', ammoType Fire |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: fires | FAIL | 0 trigger pull(s), rounds 30 -> 30 | no round consumed with this ammo type loaded |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: damages the enemy | WARN | enemy hp lost 0.0 (ammo type Fire) | no hit in the few shots fired: spread/accuracy can miss, too few shots to judge |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: hit rate over aimed shots (shots may miss by design) | INFO | 0/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: hit marks / FX | INFO | hit marks stamped 20 -> 20, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: per-ammo FX rules (blood / burn / glow / explosive handling) | INFO | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 20 -> 20, enemy hp lost 0.0 | no hit to evaluate the rules against |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ice', ammoType Ice |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: fires | FAIL | 0 trigger pull(s), rounds 30 -> 30 | no round consumed with this ammo type loaded |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: damages the enemy | WARN | enemy hp lost 0.0 (ammo type Ice) | no hit in the few shots fired: spread/accuracy can miss, too few shots to judge |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: hit rate over aimed shots (shots may miss by design) | INFO | 0/0 aimed shots hit (0 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target | hit rate well below what the accuracy settings suggest |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: hit marks / FX | INFO | hit marks stamped 20 -> 20, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: per-ammo FX rules (blood / burn / glow / explosive handling) | INFO | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 20 -> 20, enemy hp lost 0.0 | no hit to evaluate the rules against |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ion', ammoType Ion |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: fires | PASS | 1 trigger pull(s), rounds 30 -> 29 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: damages the enemy | PASS | enemy hp lost 49.2 (ammo type Ion) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: hit rate over aimed shots (shots may miss by design) | PASS | 1/1 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 89), ammo accuracy 89, spread 1.8 deg (ammo 1.2), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: hit marks / FX | INFO | hit marks stamped 20 -> 20, body type Humanoid | this ammo type is configured to leave no hit marks (useHitMarks off / blood and burn Never): by design |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=False; hit marks 20 -> 20, enemy hp lost 49.2 | useHitMarks is off on this ammo: no mark by design |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Laser', ammoType Laser |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: fires | PASS | 2 trigger pull(s), rounds 30 -> 28 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: damages the enemy | PASS | enemy hp lost 98.9 (ammo type Laser) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: hit rate over aimed shots (shots may miss by design) | PASS | 2/2 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.5), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: hit marks / FX | PASS | hit marks stamped 20 -> 22, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=False; hit marks 20 -> 22, enemy hp lost 98.9 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: ammo type loads (Hot Cross path) | PASS | loaded 45/45 of 'DMAmmoFxProfile_Plasma', ammoType Plasma |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: fires | PASS | 2 trigger pull(s), rounds 45 -> 43 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: damages the enemy | PASS | enemy hp lost 86.0 (ammo type Plasma) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: hit rate over aimed shots (shots may miss by design) | PASS | 2/2 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 78), ammo accuracy 78, spread 1.8 deg (ammo 1.2), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: hit marks / FX | PASS | hit marks stamped 22 -> 24, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=False; hit marks 22 -> 24, enemy hp lost 86.0 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Resonance Stabilizer', ammoType ResonanceStabilizer |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: fires | PASS | 2 trigger pull(s), rounds 30 -> 28 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: damages the enemy | PASS | enemy hp lost 63.4 (ammo type ResonanceStabilizer) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: hit rate over aimed shots (shots may miss by design) | PASS | 2/2 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: hit marks / FX | PASS | hit marks stamped 24 -> 26, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 24 -> 26, enemy hp lost 63.4 |  |
| Ammo | X (AmmoCycle) switches the loaded ammo type | PASS | 3 ammo stack(s) on the Hot Cross utility slots; loaded DMAmmoFxProfile_Resonance Stabilizer -> DMAmmoFxProfile_CarveExperiment -> DMAmmoFxProfile_Electricity |  |
| Ammo | Ammo coverage | INFO | 2 weapon(s) tested with their default ammo (full: load, fill, fire, damage, marks, empty, reload); 9 ammo-specific case(s) (all non-default ammo types), each once on a host weapon (load, 2 shots, damage, marks, per-ammo FX rules); no weapon x ammo matrix. Mining tool excluded: it runs on Plasma Fuel charge. |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player rig: enemy ranged shot hits a body bone + mark

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Player rig | Player hitbox rig is built and active (per-bone hitboxes on layer DMHitbox) | PASS | rig present, active True, active rigs 1, hitboxes 18, not on the DMHitbox layer 0, body type Humanoid |  |
| Player rig | Enemy ranged shot ('survival_rifle') resolves to a player body hitbox | FAIL | player hp lost 0.0, shots/projectiles 0, rig marks 0 -> 0, active mark objects 0: on a bone 0, on the root/capsule 0, parents [], range 7m, states Wander/Defensive/Attack | enemy ranged shot did no damage to the player: the shot missed or the hitscan still passes through (the rig supersedes the capsule but nothing resolves on the bone hitboxes) |
| Player rig | Player hit marks are removed automatically after the configured lifetime | WARN | no marks to expire (marks 0), lifetime 10s | no player mark was stamped, so expiry could not be checked |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Aim | Aim follows the moving enemy: chest | FAIL | 3 shots, 0 fired, aim error before firing avg 0.6deg; crosshair hitbox zone Torso (bone VBOT_:Spine2); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Aim follows the moving enemy: head | FAIL | 3 shots, 0 fired, aim error before firing avg 0.4deg; crosshair hitbox zone Head (bone VBOT_:Head); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Aim follows the moving enemy: arm | FAIL | 3 shots, 0 fired, aim error before firing avg 0.8deg; crosshair hitbox zone Torso/none/Arm (bone VBOT_:Spine2/none/VBOT_:LeftArm); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Aim follows the moving enemy: leg | FAIL | 3 shots, 0 fired, aim error before firing avg 0.7deg; crosshair hitbox zone Leg/Torso/none (bone VBOT_:RightUpLeg/VBOT_:Hips/none); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Aim follows the moving enemy: foot | FAIL | 3 shots, 0 fired, aim error before firing avg 0.6deg; crosshair hitbox zone none/Leg (bone none/VBOT_:LeftFoot); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Body-part damage table (for later headshot/limb multipliers) | INFO | chest: zone Torso, avg dmg 0.0, xn/a vs chest / head: zone Head, avg dmg 0.0, xn/a vs chest / arm: zone Torso/none/Arm, avg dmg 0.0, xn/a vs chest / leg: zone Leg/Torso/none, avg dmg 0.0, xn/a vs chest / foot: zone none/Leg, avg dmg 0.0, xn/a vs chest | damage multipliers per zone come from DMCombatBodyPartUtility.ApplyZoneDamageMultiplier (DM_EnemyHitMarkProfile); equal ratios here mean no zone multiplier is configured |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Behavior tweaks: over / under-exaggerated enemy values (runtime only)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Behavior | How the tweaks are applied | INFO | runtime only: values are written to the live component instances of a freshly spawned enemy (destroyed after each variation) and profile tweaks go through in-memory clones swapped in for the global Resources profile and restored afterwards; a disk hash of the profiles, definitions and enemy prefabs is compared at the end. Hit-mark / effect budgets are not covered here (they are visual budgets, see the hit-mark rows). |  |
| Behavior | Normal behaviour (reference): player 9m in front, 8s | INFO | notice 0.1s, chase 0.1s, first attack 1.4s, attacks 3, dmg 131, min dist 1.2m, path 7.0m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:216/Hold:8, states Hold/Chase/Attack |  |
| Behavior | Aggression / attack rate: high (hi = attacks much faster) | PASS | EnemyCombat.attackCooldown 1 -> 0.25, EnemyCombat.meleeRecoveryPauseMin 0.6 -> 0.15, EnemyCombat.meleeRecoveryPauseMax 1.8 -> 0.45; notice 0.0s, chase 0.0s, first attack 1.3s, attacks 3, dmg 157, min dist 1.3m, path 8.9m, yaw 76deg, orbit around player net -76deg / abs 76deg, avg facing error 0deg, brain Press:203/Hold:13/None:1, states Hold/Chase/Attack |  |
| Behavior | Aggression / attack rate: low (hi = attacks much faster) | PASS | EnemyCombat.attackCooldown 1 -> 3, EnemyCombat.meleeRecoveryPauseMin 0.6 -> 1.8, EnemyCombat.meleeRecoveryPauseMax 1.8 -> 5.4; notice 0.0s, chase 0.0s, first attack 1.4s, attacks 3, dmg 130, min dist 1.2m, path 8.0m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:216/Hold:15, states Hold/Chase/Attack |  |
| Behavior | Aggression / attack rate: suggested safe range | PASS | both x0.25 and x3 of the normal values stayed healthy: safe range at least x3 .. x0.25 |  |
| Behavior | Standoff distance: high (hi = stands further off) | PASS | EnemyAiController.attackStandoffFraction 0.62 -> 0.992, EnemyAiController.minCombatSeparation 0.95 -> 1.1; notice 0.1s, chase 0.1s, first attack 1.2s, attacks 4, dmg 131, min dist 1.3m, path 8.3m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:208/Hold:14, states Hold/Chase/Attack |  |
| Behavior | Standoff distance: low (hi = stands further off) | PASS | EnemyAiController.attackStandoffFraction 0.62 -> 0.248, EnemyAiController.minCombatSeparation 0.95 -> 0.38; notice 0.0s, chase 0.0s, first attack 1.4s, attacks 4, dmg 130, min dist 1.2m, path 8.0m, yaw 35deg, orbit around player net 35deg / abs 35deg, avg facing error 0deg, brain Press:200/Hold:13/None:1, states Hold/Chase/Attack |  |
| Behavior | Standoff distance: suggested safe range | PASS | both x1.6 and x0.4 of the normal values stayed healthy: safe range at least x0.4 .. x1.6 |  |
| Behavior | Chase / lose-target timings: high (hi = never gives up, lo = gives up at once) | PASS | EnemyAiController.loseTargetDelay 4 -> 16, EnemyAiController.maxChaseDuration 6 -> 24, EnemyAiController.chaseSpeedMultiplier 0.95 -> 3.8; notice 0.1s, chase 0.1s, first attack 0.6s, attacks 4, dmg 185, min dist 1.3m, path 7.9m, yaw 40deg, orbit around player net -40deg / abs 40deg, avg facing error 0deg, never lost the target within 14s, brain Press:185/Hold:13/None:1, states Hold/Chase/Attack |  |
| Behavior | Chase / lose-target timings: low (hi = never gives up, lo = gives up at once) | PASS | EnemyAiController.loseTargetDelay 4 -> 0.8, EnemyAiController.maxChaseDuration 6 -> 1.2, EnemyAiController.chaseSpeedMultiplier 0.95 -> 0.19; notice 0.0s, chase 0.0s, first attack 2.5s, attacks 3, dmg 159, min dist 1.2m, path 7.4m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, lost target after 0.8s, brain Press:203/Hold:12, states Hold/Chase/ReturnHome/Attack |  |
| Behavior | Chase / lose-target timings: suggested safe range | PASS | both x4 and x0.2 of the normal values stayed healthy: safe range at least x0.2 .. x4 |  |
| Behavior | Move speeds: high () | PASS | EnemyAiController.walkSpeed 2.4 -> 5.28, EnemyAiController.runSpeed 5.2 -> 11.44; notice 0.0s, chase 0.0s, first attack 0.8s, attacks 3, dmg 215, min dist 1.2m, path 6.8m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:193/Hold:9/None:1, states Hold/Chase/Attack |  |
| Behavior | Move speeds: low () | PASS | EnemyAiController.walkSpeed 2.4 -> 0.84, EnemyAiController.runSpeed 5.2 -> 1.82; notice 0.0s, chase 0.0s, first attack 3.7s, attacks 2, dmg 81, min dist 1.7m, path 7.9m, yaw 30deg, orbit around player net 30deg / abs 30deg, avg facing error 0deg, brain Press:200/Hold:23/None:1, states Hold/Chase/Attack |  |
| Behavior | Move speeds: suggested safe range | PASS | both x2.2 and x0.35 of the normal values stayed healthy: safe range at least x0.35 .. x2.2 |  |
| Behavior | Senses ranges: high () | PASS | EnemySenses.visionRange 16 -> 32, EnemySenses.hearingRange 18 -> 36, EnemySenses.visionFov 110 -> 220; notice 0.0s, chase 0.0s, first attack 1.3s, attacks 3, dmg 130, min dist 1.2m, path 8.3m, yaw 37deg, orbit around player net 37deg / abs 37deg, avg facing error 0deg, brain Press:207/Hold:13, states Hold/Chase/Attack |  |
| Behavior | Senses ranges: low () | PASS | EnemySenses.visionRange 16 -> 4.8, EnemySenses.hearingRange 18 -> 5.4, EnemySenses.visionFov 110 -> 33; notice 1.7s, chase 1.7s, first attack 2.2s, attacks 3, dmg 130, min dist 1.2m, path 8.0m, yaw 31deg, orbit around player net 31deg / abs 31deg, avg facing error 0deg, brain Press:175/None:49, states Wander/Chase/Attack |  |
| Behavior | Senses ranges: suggested safe range | PASS | both x2 and x0.3 of the normal values stayed healthy: safe range at least x0.3 .. x2 |  |
| Behavior | Reaction chance / stagger: high () | PASS | EnemyInvectorRagdollBridge.baseReactionChance 0.25 -> 1, EnemyInvectorRagdollBridge.defaultStaggerSeconds 0.28 -> 1, EnemyInvectorRagdollBridge.knockdownImpulse 0.28 -> 1; notice 0.1s, chase 0.1s, first attack 1.3s, attacks 3, dmg 158, min dist 1.4m, path 6.4m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:204/Hold:9, states Hold/Chase/Attack |  |
| Behavior | Reaction chance / stagger: low () | PASS | EnemyInvectorRagdollBridge.baseReactionChance 0.25 -> 0, EnemyInvectorRagdollBridge.defaultStaggerSeconds 0.28 -> 0, EnemyInvectorRagdollBridge.knockdownImpulse 0.28 -> 0; notice 0.1s, chase 0.1s, first attack 1.5s, attacks 3, dmg 130, min dist 1.2m, path 8.0m, yaw 36deg, orbit around player net 36deg / abs 36deg, avg facing error 0deg, brain Press:212/Hold:12, states Hold/Chase/Attack |  |
| Behavior | Reaction chance / stagger: suggested safe range | PASS | both x4 and x0 of the normal values stayed healthy: safe range at least x0 .. x4 |  |
| Behavior | Brain archetype weights: high (hi = press x3 / defend x0.3 / retreat 0, lo = press x0.2 / defend x3 / retreat x3) | PASS | archetype rows: press x3, defend x0.3, retreat x0; notice 0.0s, chase 0.0s, first attack 1.0s, attacks 3, dmg 185, min dist 1.2m, path 5.4m, yaw 1deg, orbit around player net 0deg / abs 1deg, avg facing error 0deg, brain Press:209/None:1, states Chase/Attack |  |
| Behavior | Brain archetype weights: low (hi = press x3 / defend x0.3 / retreat 0, lo = press x0.2 / defend x3 / retreat x3) | PASS | archetype rows: press x0.2, defend x3, retreat x3; notice 0.1s, chase 0.1s, first attack 2.3s, attacks 2, dmg 131, min dist 1.9m, path 11.8m, yaw 105deg, orbit around player net -105deg / abs 105deg, avg facing error 0deg, brain Press:174/Defend:43, states Chase/Defensive/Attack |  |
| Behavior | Brain archetype weights: suggested safe range | PASS | both x3 and x0.2 of the normal values stayed healthy: safe range at least x0.2 .. x3 |  |
| Behavior | Brain decision rate / randomness: high (hi = decision every 0.15s and fully random, lo = every 3s and deterministic) | PASS | decisionInterval 0.15, scoreRandomness 1; notice 0.0s, chase 0.0s, first attack 0.9s, attacks 3, dmg 131, min dist 1.2m, path 9.5m, yaw 106deg, orbit around player net 106deg / abs 106deg, avg facing error 0deg, brain Press:201/Defend:22/None:1, states Chase/Attack/Defensive |  |
| Behavior | Brain decision rate / randomness: low (hi = decision every 0.15s and fully random, lo = every 3s and deterministic) | PASS | decisionInterval 3, scoreRandomness 0; notice 0.1s, chase 0.1s, first attack 1.1s, attacks 3, dmg 212, min dist 1.2m, path 5.3m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:207/None:1, states Chase/Attack |  |
| Behavior | Brain decision rate / randomness: suggested safe range | PASS | both x1 and x0 of the normal values stayed healthy: safe range at least x0 .. x1 |  |
| Behavior | Engagement tuning (3 FREDs): high (token hold, ring radius, holder spacing) | PASS | tokenMinHoldSeconds 7, holdRingRadius 7.5 (6..9), holderMinSpacing 3.5; max simultaneous 2 (28/242), distinct attackers 2/3, hand-offs 2, closest pair 1.76m, orbit max 51deg, player hp lost 81 |  |
| Behavior | Engagement tuning (3 FREDs): low (token hold, ring radius, holder spacing) | WARN | tokenMinHoldSeconds 0, holdRingRadius 2.2 (1.8..3.2), holderMinSpacing 0.6; max simultaneous 2 (76/221), distinct attackers 3/3, hand-offs 5, closest pair 2.02m, orbit max 2deg, player hp lost 54 | degenerate: attack spam (many simultaneous attackers) |
| Behavior | Engagement tuning (3 FREDs): suggested safe range | WARN | degenerate: low: attack spam (many simultaneous attackers) | keep this group between the normal value and about halfway to the degenerate end (high: x2, low: x0.6 of normal) until a finer sweep is run |
| Behavior | Tweaks left nothing on disk (profiles, definitions, enemy prefabs hashed before/after) | PASS | identical |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | WARN | code present: True; 1 PASS / 0 FAIL / 7 warn of 8 runtime row(s) |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 43s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 71ms, 762 components, frame avg 30.6ms max 370.0ms (304 frames), GC collections 0, managed delta 9300KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 223deg, path 12.3m, net 5.8m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.8s (at 1.7m, weapon weap2_sword), first player damage 2.3s, attacks 1, states Patrol/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.3s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 517, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | FAIL | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 2 | 2 Holstered_ slots active at once |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 123.0 (-77.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 123.0 -> 52.6; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 3, body type Android |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 19 distinct clips seen in total |  |
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
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 34ms, 699 components, frame avg 29.2ms max 69.0ms (309 frames), GC collections 0, managed delta 19624KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 176deg, path 8.3m, net 2.8m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.3s (at 1.7m, weapon weap2_sword), first player damage 2.8s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.8s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 11 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.5s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 507, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | FAIL | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 2 | 2 Holstered_ slots active at once |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 497.7 (-2.3); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 497.7 -> 460.0; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Robot |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.7s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 13 distinct clips seen in total |  |
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

### SwordMaster

Prefab `Assets/_Project/Prefabs/Combat/Enemies/SwordMaster.prefab` - definition `Humanoid_Enemy` - melee `Blade Of Chaos` / ranged `sci_fi_pistol` - audited in 63s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=Blade Of Chaos (dmg 21), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 31ms, 694 components, frame avg 29.0ms max 70.9ms (310 frames), GC collections 0, managed delta 20444KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 135deg, path 11.9m, net 5.2m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | WARN | notice 0.1s, chase 0.1s, first attack never (at -1.0m, weapon ), first player damage never, attacks 0, states Wander/Hold/Chase/Attack | noticed but never attacked within 22s (chase blocked, standoff/hold token, attack range, or cooldown) |
| Weapons | Damage to player during natural AI behaviour | INFO | no damage in 22s of natural behaviour |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 30.0 (item dmg 21), attacks 1, hitbox-active frames 9 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Blade Of Chaos': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 30.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | FAIL | player hp lost 0.0 (item dmg 14), shots/projectiles 0, attack-state frames 523, attacks 0, first hit never, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | attack state reached but no CombatProjectile spawned: weapon never fired (aim not ready, shooter weapon without ammo/clip, or onShot not raised) |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 0 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Blade_Of_Chaos] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | FAIL | spawn: active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 2 | 2 Holstered_ slots active at once |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 269.7 (-30.3); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 269.7 -> 204.1; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 3, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.7s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 27 distinct clips seen in total |  |
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
