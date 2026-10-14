# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-13 21:13:23 -> 2026-10-13 21:30:30 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 3; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 3
- Unit store: 37 stored unit(s); stamps audit 91CBB6 game 057317 gear 11046A; skip-unchanged is OFF (Full mode)
- Player level raised in memory from 5 for equip gates (restored at the end; nothing saved)
- Mode full: 18 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 17m06s

**3 enemies audited - 2 FAIL, 4 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 0 weapon cases + 16 scenarios): 192 PASS, 26 FAIL, 30 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 242 change(s).

- NEW enemy|Humadroid|perf = PASS
- NEW enemy|Humadroid|melee.miss = INFO
- NEW enemy|Humadroid|ranged.miss = INFO
- CHANGED enemy|Humadroid|player.swing: PASS -> WARN
- NEW enemy|Humadroid|loot = FAIL
- NEW enemy|Robot|perf = PASS
- NEW enemy|Robot|melee.miss = INFO
- NEW enemy|Robot|ranged.miss = INFO
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
- NEW enemy|SwordMaster|ranged.damage = PASS
- NEW enemy|SwordMaster|ranged.miss = INFO
- NEW enemy|SwordMaster|ranged.vis = INFO
- NEW enemy|SwordMaster|vis = PASS
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
- CHANGED scenario|Block + parry: player defends against enemy melee|blk.hold: PASS -> FAIL
- CHANGED scenario|Block + parry: player defends against enemy melee|blk.stagger: INFO -> WARN
- CHANGED scenario|Block + parry: player defends against enemy melee|parry.window: PASS -> WARN
- NEW scenario|Block + parry: player defends against enemy melee|parry.counter = INFO
- CHANGED scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.approach: PASS -> WARN
- CHANGED scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.lose: PASS -> WARN
- NEW scenario|Multi-enemy: FRED x3 and x5|multi.who = INFO
- NEW scenario|Multi-enemy: FRED x3 and x5|multi = WARN
- NEW scenario|Multi-enemy: FRED x3 and x5|multi5 = WARN
- NEW scenario|Multi-enemy: FRED x3 and x5|console = PASS
- NEW scenario|Brain: orbit, toggles, archetypes|brain.orbit.still = PASS
- NEW scenario|Brain: orbit, toggles, archetypes|brain.orbit.strafe = WARN
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
- ... 162 more

## Timing

Wall time 1026s (17.1 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Humadroid | enemy | 29.1 | ran |
| Robot | enemy | 29.8 | ran |
| SwordMaster | enemy | 27.7 | ran |
| Camera: faces forward after teleports, zoom in/out | scenario | 9.0 | ran |
| Equip: weapon survives a teleport | scenario | 6.1 | ran |
| Audio: swing / hit sound wiring (static) | scenario | 0.4 | ran |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | scenario | 60.4 | ran |
| Block + parry: player defends against enemy melee | scenario | 56.3 | ran |
| Block: enemy defends against player swings | scenario | 15.1 | ran |
| Movement: retreat, push, flank, backstab, lose target | scenario | 34.6 | ran |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | scenario | 98.0 | ran |
| Multi-enemy: FRED x3 and x5 | scenario | 36.3 | ran |
| Brain: orbit, toggles, archetypes | scenario | 241.4 | ran |
| Player weapons: each melee item vs enemy (swapped in place) | scenario | 49.2 | ran |
| Ammo: default ammo per ranged weapon + ammo-specific FX | scenario | 61.9 | ran |
| Player rig: enemy ranged shot hits a body bone + mark | scenario | 19.3 | ran |
| Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | scenario | 33.5 | ran |
| Behavior tweaks: over / under-exaggerated enemy values (runtime only) | scenario | 195.9 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| scenario|Brain: orbit, toggles, archetypes | setup | 241.6 | 5483 | 44.1 | 385.6 | 0.60 | 0.00 | 19.44 |
| scenario|Behavior tweaks: over / under-exaggerated enemy values (runtime only) | setup | 196.1 | 4421 | 44.4 | 324.7 | 0.56 | 0.00 | 20.63 |
| scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | setup | 98.3 | 2335 | 42.1 | 216.4 | 0.60 | 0.00 | 18.37 |
| scenario|Ammo: default ammo per ranged weapon + ammo-specific FX | setup | 62.2 | 1430 | 43.5 | 225.9 | 0.56 | 0.00 | 19.33 |
| scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | setup | 60.6 | 1793 | 33.8 | 213.2 | 0.41 | 0.00 | 15.32 |
| scenario|Block + parry: player defends against enemy melee | setup | 57.5 | 1415 | 40.6 | 338.1 | 0.56 | 0.00 | 18.04 |
| scenario|Player weapons: each melee item vs enemy (swapped in place) | setup | 49.4 | 1099 | 45.0 | 234.7 | 0.63 | 0.00 | 19.58 |
| scenario|Multi-enemy: FRED x3 and x5 | setup | 37.8 | 719 | 52.6 | 449.6 | 0.75 | 0.00 | 25.14 |
| scenario|Movement: retreat, push, flank, backstab, lose target | setup | 34.8 | 816 | 42.7 | 256.5 | 0.53 | 0.00 | 18.21 |
| scenario|Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | setup | 33.7 | 832 | 40.5 | 128.5 | 0.52 | 0.00 | 19.50 |
| scenario|Player rig: enemy ranged shot hits a body bone + mark | setup | 19.5 | 457 | 42.6 | 239.4 | 0.51 | 0.00 | 19.93 |
| scenario|Block: enemy defends against player swings | setup | 16.3 | 404 | 40.2 | 68.6 | 0.55 | 0.00 | 17.18 |
| scenario|Camera: faces forward after teleports, zoom in/out | setup | 9.2 | 271 | 33.9 | 60.3 | 0.45 | 0.00 | 15.13 |
| enemy|SwordMaster | unaware 9s (+perf) | 9.1 | 252 | 36.0 | 166.5 | 0.45 | 0.00 | 16.71 |
| enemy|Robot | unaware 9s (+perf) | 9.0 | 246 | 36.6 | 63.0 | 0.45 | 0.00 | 17.55 |
| enemy|Humadroid | unaware 9s (+perf) | 9.0 | 261 | 34.5 | 72.5 | 0.47 | 0.00 | 15.98 |
| enemy|Humadroid | death/loot | 6.6 | 183 | 36.1 | 126.2 | 0.52 | 0.00 | 16.00 |
| enemy|Robot | death/loot | 6.6 | 183 | 35.9 | 79.5 | 0.44 | 0.00 | 16.56 |
| enemy|SwordMaster | death/loot | 6.5 | 182 | 35.6 | 79.0 | 0.45 | 0.00 | 16.32 |
| scenario|Equip: weapon survives a teleport | setup | 6.3 | 179 | 35.4 | 199.0 | 0.42 | 0.00 | 15.53 |
| enemy|Robot | ranged test | 4.5 | 117 | 40.0 | 250.6 | 0.49 | 0.00 | 17.54 |
| enemy|Robot | notice/chase/attack | 4.2 | 112 | 37.2 | 75.0 | 0.51 | 0.00 | 17.32 |
| enemy|Humadroid | ranged test | 3.8 | 95 | 42.4 | 275.4 | 0.50 | 0.00 | 17.86 |
| enemy|SwordMaster | ranged test | 3.8 | 91 | 43.5 | 242.6 | 0.56 | 0.00 | 18.85 |
| enemy|Humadroid | notice/chase/attack | 3.1 | 81 | 37.6 | 62.2 | 0.77 | 0.00 | 17.44 |
| enemy|Humadroid | melee test | 2.7 | 61 | 41.5 | 69.7 | 0.66 | 0.00 | 18.13 |
| enemy|SwordMaster | melee test | 2.6 | 46 | 52.6 | 108.1 | 0.65 | 0.00 | 24.77 |
| enemy|SwordMaster | player->enemy | 2.5 | 62 | 40.0 | 75.0 | 0.52 | 0.00 | 17.34 |
| enemy|Robot | player->enemy | 2.4 | 59 | 41.6 | 78.4 | 0.57 | 0.00 | 17.25 |
| enemy|Humadroid | player->enemy | 2.3 | 57 | 41.9 | 77.1 | 0.64 | 0.00 | 17.77 |

Whole run (ran units only): 1011s over 23941 frames, avg frame 42.4 ms, physics 0.56 ms, render 0.00 ms, scripts 18.98 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 100 | FAIL | Player uses melee 'weap2_sword' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 4/4 clicks swung, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 2 | 100 | FAIL | Player uses melee 'Wood Axe' on the enemy | Player weapons: each melee item vs enemy (swapped in place) | 1/4 clicks swung, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 3 | 100 | FAIL | Player uses ranged 'sci_fi_pistol' on the enemy (default ammo) | Ammo: default ammo per ranged weapon + ammo-specific FX | see the ammo damage row | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 4 | 100 | FAIL | Player uses ranged 'survival_rifle' on the enemy (default ammo) | Ammo: default ammo per ranged weapon + ammo-specific FX | see the ammo damage row | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 5 | 70 | FAIL | Holding block negates enemy melee damage | Block + parry: player defends against enemy melee | player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 6 | 55 | WARN | Console errors/warnings | Player weapons: each melee item vs enemy (swapped in place) | [Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0. | Fix the exception at the listed stack frame (null-guard or initialise the field); it repeats on every spawn. |
| 7 | 45 | WARN | Enemy guard-break stagger after a blocked hit | Block + parry: player defends against enemy melee | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 8 | 30 | WARN | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | Multi-enemy: FRED x3 and x5 | enemies stacked: closest pair 0.43m (no separation/avoidance between enemies); 128/337 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Add enemy separation and make the attack token rotate between enemies. |
| 9 | 25 | WARN | Hit marks fire on damage | Humadroid | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) | Add DMEnemyHitMarks + the layer-26 hitbox rig; verify the body-type hit-mark profile. |
| 10 | 20 | FAIL | Loot dropped and can be looted (real loot path), then cleaned up at once | Humadroid, Robot | bag spawned but nothing could be looted (empty entries / TryLootAll refused) | Inspect the measured value and cause on the detail row. |
| 11 | 20 | FAIL | W moves the player forward (camera direction) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move action not reaching the controller: PlayerInput disabled, move locked, or PioneerPlayerInputBinder.HandleAction('Move') not wired | Inspect the measured value and cause on the detail row. |
| 12 | 20 | FAIL | S moves the player back | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (S) produced no backward displacement | Inspect the measured value and cause on the detail row. |
| 13 | 20 | FAIL | A strafes left | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (A) produced no leftward displacement | Inspect the measured value and cause on the detail row. |
| 14 | 20 | FAIL | D strafes right | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (D) produced no rightward displacement | Inspect the measured value and cause on the detail row. |
| 15 | 20 | FAIL | W run speed (reference for sprint) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | player did not move | Inspect the measured value and cause on the detail row. |
| 16 | 20 | FAIL | Shift + W sprints faster than W | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | sprint no faster than run (0.0m): Sprint action not wired, stamina 0, or sprint speed equals run speed | Inspect the measured value and cause on the detail row. |
| 17 | 20 | FAIL | Space jumps | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked; state before the press: isGrounded True, isJumping False, customAction False, isCrouching False, stamina 1000 | Inspect the measured value and cause on the detail row. |
| 18 | 20 | FAIL | Q dodge / roll | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dodge action did not move the player: TryDodgeRollFromInput refused (stamina, cooldown, no roll clip) | Inspect the measured value and cause on the detail row. |
| 19 | 20 | FAIL | Left click swings the drawn melee weapon | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | melee Attack input not received: drawn melee tap/hold logic (UpdateDrawnMeleeCharge) did not start a swing | Inspect the measured value and cause on the detail row. |
| 20 | 20 | FAIL | Right click blocks (melee drawn) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Block input (Mouse rightButton / Block action) did not set isBlocking: stamina 0, customAction, not grounded, or still attacking | Inspect the measured value and cause on the detail row. |
| 21 | 20 | WARN | Player circles behind the enemy: enemy turns to face | Movement: retreat, push, flank, backstab, lose target | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 22 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Standard: fires (rounds consumed) | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 23 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Standard: fires (rounds consumed) | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 24 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: fires | Ammo: default ammo per ranged weapon + ammo-specific FX | no round consumed with this ammo type loaded | Inspect the measured value and cause on the detail row. |
| 25 | 20 | FAIL | Enemy ranged shot ('survival_rifle') resolves to a player body hitbox | Player rig: enemy ranged shot hits a body bone + mark | enemy ranged shot did no damage to the player: the shot missed or the hitscan still passes through (the rig supersedes the capsule but nothing resolves on the bone hitboxes) | Inspect the measured value and cause on the detail row. |
| 26 | 20 | FAIL | Aim follows the moving enemy: chest | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 27 | 20 | FAIL | Aim follows the moving enemy: head | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 28 | 20 | FAIL | Aim follows the moving enemy: arm | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 29 | 20 | FAIL | Aim follows the moving enemy: leg | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 30 | 20 | FAIL | Aim follows the moving enemy: foot | Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) | Inspect the measured value and cause on the detail row. |
| 31 | 15 | WARN | A tap inside the parry window parries (and an early/late tap does not) | Block + parry: player defends against enemy melee | no sampled tap produced a parry: enemy attack timing varies between swings (windup from the baseline swing may not match), or the parry window/stagger in the profile differs from what the rows show | Inspect the measured value and cause on the detail row. |
| 32 | -5 | WARN | Player melee swing via injected Mouse input (best effort) | Humadroid, Robot, SwordMaster | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes | Inspect the measured value and cause on the detail row. |
| 33 | -5 | WARN | Camera zooms in and out | Camera: faces forward after teleports, zoom in/out | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state | Inspect the measured value and cause on the detail row. |
| 34 | -5 | WARN | Left Ctrl crouches | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) | Inspect the measured value and cause on the detail row. |
| 35 | -5 | WARN | Left Alt dash | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dash did not move the player even though the skill gate is unlocked for this run (see ctl.systems row): DMDashController disabled, on cooldown, stamina, or the Dash action not bound to Left Alt | Inspect the measured value and cause on the detail row. |
| 36 | -5 | WARN | Hold Space after jumping boosts (jetpack) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | no sustained lift although the jetpack skill gate is unlocked for this run (see ctl.systems row): fuel 0, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player | Inspect the measured value and cause on the detail row. |
| 37 | -5 | WARN | Tab (SwitchWeapon) cycles the weapon focus | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Tab tap did not change focus: only one weapon on the hotbar, Hot Cross not polling, or tap/hold threshold (hold arms) not met | Inspect the measured value and cause on the detail row. |
| 38 | -5 | WARN | Half-tilt stick walks slower than a run | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | walk speed not distinct from run (0.0m): analog Move magnitude ignored by the locomotion speed | Inspect the measured value and cause on the detail row. |
| 39 | -5 | WARN | Approach with W: enemy notices | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) | Inspect the measured value and cause on the detail row. |
| 40 | -5 | WARN | Jetpack escape (hold Space + S) | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump | Inspect the measured value and cause on the detail row. |
| 41 | -5 | WARN | Sprint away: enemy loses the target | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy kept hostile for 30s of sprinting away: lose-target distance/time too long (senses) or leash not applied | Inspect the measured value and cause on the detail row. |
| 42 | -5 | WARN | Enemy returns to its post after losing the player | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy did not walk back to its spawn within 30s: ReturnHome blocked, leash undefined, or it stays in Search/Investigate | Inspect the measured value and cause on the detail row. |
| 43 | -5 | WARN | 5 FREDs at once for 18s: token rotation, spacing, no stacking/orbiting | Multi-enemy: FRED x3 and x5 | 197/294 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |
| 44 | -5 | WARN | Single FRED, player strafes around it: keeps facing the player, does not orbit on its own | Brain: orbit, toggles, archetypes | enemy turns too slowly to follow a strafing player (turnSpeed / engagerTurnRate / holderTurnRate) | Inspect the measured value and cause on the detail row. |
| 45 | -5 | WARN | 3 FREDs, baseline (brain on, director on) | Brain: orbit, toggles, archetypes | 53/248 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |
| 46 | -5 | WARN | 3 FREDs, legacyRingSlotRotation=true | Brain: orbit, toggles, archetypes | 76/238 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) | Inspect the measured value and cause on the detail row. |
| 47 | -5 | WARN | enableUtilityBrain=false stops the brain acting (no Press/Defend/Retreat/Hold) | Brain: orbit, toggles, archetypes | the brain still selected actions with enableUtilityBrain=false (a per-type ProfileOverride, or a cached decision) | Inspect the measured value and cause on the detail row. |
| 48 | -5 | WARN | sci_fi_pistol + DMAmmoFxProfile_Standard: damages the enemy | Ammo: default ammo per ranged weapon + ammo-specific FX | no shot fired, see fire row | Inspect the measured value and cause on the detail row. |
| 49 | -5 | WARN | survival_rifle + DMAmmoFxProfile_Standard: damages the enemy | Ammo: default ammo per ranged weapon + ammo-specific FX | no shot fired, see fire row | Inspect the measured value and cause on the detail row. |
| 50 | -5 | WARN | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: damages the enemy | Ammo: default ammo per ranged weapon + ammo-specific FX | no hit in the few shots fired: spread/accuracy can miss, too few shots to judge | Inspect the measured value and cause on the detail row. |
| 51 | -5 | WARN | Player hit marks are removed automatically after the configured lifetime | Player rig: enemy ranged shot hits a body bone + mark | no player mark was stamped, so expiry could not be checked | Inspect the measured value and cause on the detail row. |
| 52 | -5 | WARN | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |
| 53 | -5 | WARN | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | Regression of earlier fixes and tweaks |  | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Humadroid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | warn | PASS | PASS |
| Robot | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| SwordMaster | Blade Of Chaos | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |

## Weapons that deal NO damage (headline)

- none detected in this run.

## Scenarios (block/parry, movement, multi-enemy, player weapons, performance)

| Scenario | PASS | FAIL | warn | time |
|---|---|---|---|---|
| Camera: faces forward after teleports, zoom in/out | 2 | 0 | 1 | 9s |
| Equip: weapon survives a teleport | 2 | 0 | 0 | 6s |
| Audio: swing / hit sound wiring (static) | 4 | 0 | 0 | 0s |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | 6 | 10 | 5 | 60s |
| Block + parry: player defends against enemy melee | 1 | 1 | 2 | 56s |
| Block: enemy defends against player swings | 2 | 0 | 0 | 15s |
| Movement: retreat, push, flank, backstab, lose target | 4 | 0 | 1 | 35s |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | 5 | 0 | 4 | 98s |
| Multi-enemy: FRED x3 and x5 | 1 | 0 | 2 | 36s |
| Brain: orbit, toggles, archetypes | 8 | 0 | 4 | 241s |
| Player weapons: each melee item vs enemy (swapped in place) | 20 | 2 | 1 | 49s |
| Ammo: default ammo per ranged weapon + ammo-specific FX | 54 | 5 | 3 | 62s |
| Player rig: enemy ranged shot hits a body bone + mark | 2 | 1 | 1 | 19s |
| Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | 1 | 5 | 0 | 33s |
| Behavior tweaks: over / under-exaggerated enemy values (runtime only) | 29 | 0 | 0 | 196s |
| Regression of earlier fixes and tweaks | 6 | 0 | 2 | 0s |

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
| Controls | Climbing unlocked and controller enabled (not driven: needs a wall) | PASS | DMPlayerSystemsProfile present: ClimbEnabled=True, DashEnabled=True, JetpackEnabled=True; DMClimbController enabled, DMDashController enabled, DMJetpackController enabled |  |
| Controls | Movement systems unlocked for the run (jetpack, dash, climb) | INFO | DMPlayerSystemsProfile present: ClimbEnabled=True, DashEnabled=True, JetpackEnabled=True; DMClimbController enabled, DMDashController enabled, DMJetpackController enabled. The audit unlocks these in memory only (see environment notes) and restores them at exit. |  |
| Controls | Left Alt dash | WARN | start 0.00, min 0.00, max 0.00, end 0.00 m | Dash did not move the player even though the skill gate is unlocked for this run (see ctl.systems row): DMDashController disabled, on cooldown, stamina, or the Dash action not bound to Left Alt |
| Controls | Hold Space after jumping boosts (jetpack) | WARN | start 21.23, min 21.23, max 21.23, end 21.23 m height | no sustained lift although the jetpack skill gate is unlocked for this run (see ctl.systems row): fuel 0, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player |
| Controls | Mouse look left/right orbits the camera | PASS | start 0.00, min 0.00, max 84.71, end 90.28 deg yaw |  |
| Controls | Mouse look up/down tilts the camera | PASS | start 18.00, min 0.00, max 49.81, end 49.88 deg pitch |  |
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
| Block | Unblocked baseline (same enemy, same distance) | INFO | hits 1, player hp lost 26.0 |  |
| Block | Holding block negates enemy melee damage | FAIL | BLOCK = hold right mouse. isBlocking False (raised in 1.53s), hits 2, blocked 0, player hp lost 52.1 (unblocked 26.0), stamina 982.8 | player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking |
| Block | Enemy guard-break stagger after a blocked hit | WARN | longest enemy stagger 0.00s (profile blockStaggerSeconds 1.2s) | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) |
| Parry | Parry = tap right mouse, timed before the incoming hit (several timings) | INFO | tap -0.60s vs predicted hit (windup 0.45s): blow landed 3.56s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 99.8 // tap -0.40s vs predicted hit (windup 0.45s): blow landed 1.34s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 78.4 // tap -0.25s vs predicted hit (windup 0.45s): blow landed 2.89s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 79.4 // tap -0.15s vs predicted hit (windup 0.45s): blow landed 2.62s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 104.5 // tap -0.08s vs predicted hit (windup 0.45s): blow landed 2.70s after the tap -> HIT (tap too early/late, not blocking when it landed), enemy stagger 0.00s, hp lost 105.4 // tap -0.02s vs predicted hit (windup 0.45s): blow landed 3.31s after the tap -> BLOCK, enemy stagger 0.26s, hp lost 78.4 // tap +0.10s vs predicted hit (windup 0.45s): blow landed 3.29s after the tap -> BLOCK, enemy stagger 0.24s, hp lost 79.7 | input: PARRY = TAP right mouse (0.08s), BLOCK = HOLD right mouse; profile: parryWindowSeconds 0.2, blockStagger 1.2s, parryStagger 5.0s |
| Parry | A tap inside the parry window parries (and an early/late tap does not) | WARN | no parry observed (0 taps landed inside the window; see sweep rows) | no sampled tap produced a parry: enemy attack timing varies between swings (windup from the baseline swing may not match), or the parry window/stagger in the profile differs from what the rows show |
| Parry | A successful parry staggers the attacker (counter opening) | INFO | no parry happened, so the counter stagger could not be observed |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block: enemy defends against player swings

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Enemy block reduces player melee damage | PASS | open: 30 hits, enemy hp lost 165.5; blocking (animator IsBlocking True, duration 40.0s): 28 hits (4 flagged blocked), enemy hp lost 30.7; block clips: Idle, Walk, Idle@Pistol, Defense_Unarmed |  |
| Parry | Enemy parry | INFO | not implemented: EnemyInvectorCombatBridge only has TryBeginBlock/EndBlock (no parry window/riposte API) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Movement: retreat, push, flank, backstab, lose target

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Movement | Player retreats 9m at 3 m/s: enemy follows | PASS | distance 2.0m -> 5.1m (min 2.0m), states Attack/Chase, attacks 1 |  |
| Movement | Player pushes into the enemy: spacing and facing | PASS | min distance 0.50m, max facing error 0deg, attack-state frames 62, states Attack/Chase |  |
| Movement | Player circles behind the enemy: enemy turns to face | WARN | reached behind at never, enemy faced player (<30deg) after never, max facing error 0deg, attacks while behind 0, player hp lost -15.7, states Attack | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player |
| Movement | Hit from behind vs from the front (player melee, AI off) | INFO | front: 25 hits, 155.5 hp; behind: 18 hits, 157.6 hp; ratio 1.01 | no backstab bonus implemented if ratio is 1.0 |
| Movement | Player disappears 70m away: enemy loses the target | PASS | was aware True, investigate/search after never, back to calm state after 6.0s, states Chase/ReturnHome |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Play | Approach with W: enemy notices | WARN | walked 21.3m in 12.7s to 8.7m; enemy noticed after never at n/a; state Wander | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) |
| Play | Run away with S: enemy pursues | PASS | player backed away for 4.0s (gap now 9.6m), enemy moved 6.6m, hostile state seen True, states Wander/Hold |  |
| Play | Strafe around the enemy: it turns to face the player | PASS | enemy facing error avg 7deg, max 29deg over 95 frames while strafing D then A, state Attack |  |
| Play | Retreat then re-engage: enemy fights back | PASS | re-engaged to 1.2m, enemy attacks 3, player hp lost 125.5, state Attack |  |
| Play | Jump away: player clears distance | PASS | jump height 0.91m, gap 1.2m -> 3.5m, enemy state Attack | player could not gain distance (enemy kept pace or player blocked) |
| Play | Jetpack escape (hold Space + S) | WARN | peak height gain 0.9m, gap 3.5m -> 2.0m, enemy state Attack | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump |
| Play | Sprint away: enemy loses the target | WARN | lost after never at n/am (sprinting), state Attack, states Wander/Hold/Chase/Attack | enemy kept hostile for 30s of sprinting away: lose-target distance/time too long (senses) or leash not applied |
| Play | Enemy returns to its post after losing the player | WARN | back home after never, now 49.8m from spawn, state Attack | enemy did not walk back to its spawn within 30s: ReturnHome blocked, leash undefined, or it stays in Search/Investigate |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Multi-enemy: FRED x3 and x5

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Multi-enemy | Enemy used | INFO | HumanoidEnemy_Invector (FRED / Hybrid Droid) |  |
| Multi-enemy | 3 FREDs at once for 16s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 2 (128/337 frames with >1), distinct attackers 3/3, attacker hand-offs 9, attack-state frames [228,181,158], chase/hold frames [107,155,176], closest pair 0.43m, closest to player 1.16m, orbit around player avg 71deg max 91deg (net 54/-57/-21), player hp lost 46, brain actions Press:657/Hold:348/None:6 | enemies stacked: closest pair 0.43m (no separation/avoidance between enemies); 128/337 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Multi-enemy | 5 FREDs at once for 18s: token rotation, spacing, no stacking/orbiting | WARN | max simultaneous attackers 4 (197/294 frames with >1), distinct attackers 5/5, attacker hand-offs 13, attack-state frames [181,150,231,67,212], chase/hold frames [111,143,60,225,81], closest pair 0.71m, closest to player 1.16m, orbit around player avg 60deg max 85deg (net -12/-40/68/15/28), player hp lost 99, brain actions Press:932/Hold:529/None:9 | 197/294 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Brain: orbit, toggles, archetypes

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Brain | Single FRED, player standing still: does not orbit the player | PASS | notice 0.1s, chase 0.1s, first attack 0.9s, attacks 5, dmg 131, min dist 1.3m, path 5.0m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:247, states Chase/Attack |  |
| Brain | Single FRED, player strafes around it: keeps facing the player, does not orbit on its own | WARN | player circled 4.0m radius for 9s; enemy avg facing error 105deg, within 40deg 16% of frames, enemy angular travel around the player 232deg, states Attack/ReturnHome, path 0.1m | enemy turns too slowly to follow a strafing player (turnSpeed / engagerTurnRate / holderTurnRate) |
| Multi-enemy | 3 FREDs, baseline (brain on, director on) | WARN | max simultaneous attackers 2 (53/248 frames with >1), distinct attackers 3/3, attacker hand-offs 7, attack-state frames [175,123,121], chase/hold frames [72,122,125], closest pair 2.24m, closest to player 1.16m, orbit around player avg 28deg max 56deg (net 2/0/-28), player hp lost 125, brain actions Press:474/Hold:264/None:6 | 53/248 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:474/Hold:264/None:6 |  |
| Multi-enemy | 3 FREDs, enableUtilityBrain=false | PASS | max simultaneous attackers 3 (77/231 frames with >1), distinct attackers 3/3, attacker hand-offs 8, attack-state frames [158,196,128], chase/hold frames [72,32,101], closest pair 2.25m, closest to player 1.16m, orbit around player avg 4deg max 4deg (net -3/1/3), player hp lost 72, brain actions None:666/Press:27 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions None:666/Press:27 |  |
| Multi-enemy | 3 FREDs, enableEngagementDirector=false | PASS | max simultaneous attackers 3 (160/240 frames with >1), distinct attackers 3/3, attacker hand-offs 11, attack-state frames [199,204,204], chase/hold frames [39,35,33], closest pair 2.28m, closest to player 1.23m, orbit around player avg 9deg max 10deg (net 0/1/1), player hp lost 119, brain actions Press:384/None:336 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = False; brain actions Press:384/None:336 |  |
| Multi-enemy | 3 FREDs, legacyRingSlotRotation=true | WARN | max simultaneous attackers 2 (76/238 frames with >1), distinct attackers 3/3, attacker hand-offs 7, attack-state frames [111,203,72], chase/hold frames [126,32,164], closest pair 1.90m, closest to player 1.16m, orbit around player avg 75deg max 117deg (net 16/24/13), player hp lost 52, brain actions Press:455/Hold:253/None:6 | 76/238 frames with more than one simultaneous attacker (D1 expects exactly one melee attacker per target) |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = True; brain actions Press:455/Hold:253/None:6 |  |
| Multi-enemy | 3 FREDs, brain off + director off | PASS | max simultaneous attackers 3 (167/236 frames with >1), distinct attackers 3/3, attacker hand-offs 13, attack-state frames [197,201,199], chase/hold frames [37,34,34], closest pair 2.33m, closest to player 1.34m, orbit around player avg 1deg max 1deg (net 0/0/1), player hp lost 130, brain actions Press:508/None:200 |  |
| Brain | Profile toggle state while this variation ran | INFO | director IsEnabled read inside the scope = False; brain actions Press:508/None:200 |  |
| Brain | enableUtilityBrain=false stops the brain acting (no Press/Defend/Retreat/Hold) | WARN | distinct brain actions: brain on 2 (None:6/Hold:264/Press:474), brain off 1 (None:666/Press:27) | the brain still selected actions with enableUtilityBrain=false (a per-type ProfileOverride, or a cached decision) |
| Brain | enableEngagementDirector=false: no single-attacker token, enemies fall back to the old behaviour | PASS | with the director on: max simultaneous attackers 2 (53 frames with >1); director off: 3 (160 frames with >1), distinct attackers 3 vs 3, hand-offs 7 vs 11 |  |
| Brain | legacyRingSlotRotation=true restores the old ring-slot rotation (more orbiting than the director) | PASS | orbit around the player avg 28deg (director) vs 75deg (legacy ring), distinct attackers 3 vs 3, hand-offs 7 vs 7, max simultaneous attackers 2 vs 2 |  |
| Brain | Archetype Duelist: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 4, dmg 231, min dist 1.2m, path 5.3m, yaw 48deg, orbit around player net 48deg / abs 48deg, avg facing error 0deg, brain Press:241/None:1, states Chase/Attack |  |
| Brain | Archetype Rusher: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 8.7s, attacks 1, dmg 47, min dist 2.1m, path 4.8m, yaw 47deg, orbit around player net -47deg / abs 47deg, avg facing error 0deg, brain Press:260, states Chase/Attack |  |
| Brain | Archetype Berserker: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 4, dmg 53, min dist 1.3m, path 4.7m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:266/None:2, states Wander/Chase/Attack |  |
| Brain | Archetype Flanker: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.9s, attacks 5, dmg 126, min dist 1.2m, path 4.3m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:253/None:2, states Wander/Chase/Attack |  |
| Brain | Archetype Ambusher: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 3, dmg 79, min dist 1.6m, path 6.3m, yaw 40deg, orbit around player net -38deg / abs 60deg, avg facing error 0deg, brain Press:242/None:2, states Wander/Chase/Attack |  |
| Brain | Archetype Tank: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 0.7s, attacks 3, dmg 26, min dist 1.7m, path 3.3m, yaw 4deg, orbit around player net 3deg / abs 4deg, avg facing error 0deg, brain Press:230/Defend:16, states Chase/Attack/Defensive |  |
| Brain | Archetype Guardian: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 3, dmg 131, min dist 1.3m, path 6.4m, yaw 3deg, orbit around player net -1deg / abs 3deg, avg facing error 0deg, brain Press:193/Defend:23, states Chase/Attack/Defensive |  |
| Brain | Archetype Shooter: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 0.9s, attacks 2, dmg 26, min dist 1.2m, path 4.5m, yaw 4deg, orbit around player net 0deg / abs 27deg, avg facing error 0deg, brain Press:235/Defend:11/None:1, states Chase/Attack/Defensive |  |
| Brain | Archetype Kiter: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 4, dmg 0, min dist 1.3m, path 4.1m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:251/None:1, states Chase/Attack |  |
| Brain | Archetype Swarmer: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 5, dmg 53, min dist 1.1m, path 8.5m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:231/Defend:16/None:1, states Chase/Attack/Defensive |  |
| Brain | Archetype Stalker: brain actions and attack rate (10s, player at 6m) | INFO | notice 0.0s, chase 0.0s, first attack 1.3s, attacks 4, dmg 105, min dist 1.2m, path 9.8m, yaw 103deg, orbit around player net 45deg / abs 104deg, avg facing error 1deg, brain Press:203/Defend:27/None:1, states Chase/Attack/Defensive |  |
| Brain | Personality Aggressive (Duelist): brain actions and attack rate | INFO | notice 0.1s, chase 0.1s, first attack 0.8s, attacks 3, dmg 78, min dist 1.5m, path 5.9m, yaw 32deg, orbit around player net 22deg / abs 42deg, avg facing error 1deg, brain Press:226/None:1, states Chase/Attack |  |
| Brain | Personality Cowardly (Duelist): brain actions and attack rate | INFO | notice 0.1s, chase 0.1s, first attack 1.3s, attacks 5, dmg 131, min dist 0.4m, path 4.7m, yaw 58deg, orbit around player net -39deg / abs 66deg, avg facing error 1deg, brain Press:219/Defend:13, states Chase/Attack/Defensive |  |
| Brain | Personality Cautious (Duelist): brain actions and attack rate | INFO | notice 0.1s, chase 0.1s, first attack 0.7s, attacks 5, dmg 156, min dist 1.2m, path 4.4m, yaw 23deg, orbit around player net -22deg / abs 22deg, avg facing error 0deg, brain Press:252, states Chase/Attack |  |
| Brain | Brain DOES | INFO | scores Press / Defend / Retreat / Hold every decisionInterval from archetype rows (press, defend, retreat, tokenBias, holderTaunt, holderFeint) x personality multipliers x health condition (Healthy/Injured/SeverelyInjured/Critical); feeds the engagement director a token bias; scales holder idle actions (sidestep / taunt / feint); issues defend requests (block vs dodge) and retreat windows; reacts to intentional melee misses (miss recovery). Observed actions per archetype are in the rows above. |  |
| Brain | Brain does NOT | INFO | no weapon choice: DMEnemyBrain contains no weapon-switch call (weapon selection stays with the loadout bridge / combat bridge; brain-driven weapon choice and grenade throwing are deferred). It does not move the enemy itself (movement stays in EnemyAiController / the engagement director), does not pick targets, and does nothing when enableUtilityBrain is off. |  |
| Brain | Toggle / archetype tests left nothing on disk | PASS | Resources/Combat profiles, Data/Enemies definitions and enemy prefabs hashed before and after: identical |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player weapons: each melee item vs enemy (swapped in place)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Audio | Player '2 Hander' swing plays a swish | PASS | 4/4 clicks started a swing; 4 non-ambient audio start(s) outside the hit windows: SfxPool_4:Melee Weapon Air Swing 2, SfxPool_7:Melee Weapon Attack 4, SfxPool_1:Melee Weapon Attack 4 |  |
| Audio | Player '2 Hander' hitting the enemy plays a hit sound | PASS | 41/41 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee '2 Hander' on the enemy | PASS | enemy hp lost 120.2 (item dmg 28), hits 41, player right weapon Drawn_2_Hander |  |
| Audio | Player 'Blade Of Chaos' swing plays a swish | PASS | 3/4 clicks started a swing; 3 non-ambient audio start(s) outside the hit windows: SfxPool_5:neverworld, SfxPool_7:Melee Weapon Air Swing 4, SfxPool_1:Melee Weapon Attack 1 |  |
| Audio | Player 'Blade Of Chaos' hitting the enemy plays a hit sound | PASS | 31/31 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Blade Of Chaos' on the enemy | PASS | enemy hp lost 102.6 (item dmg 21), hits 31, player right weapon Drawn_Blade_of_Chaos |  |
| Audio | Player 'Death Axe' swing plays a swish | PASS | 3/4 clicks started a swing; 5 non-ambient audio start(s) outside the hit windows: SfxPool_5:no way out, SfxPool_6:neverworld, SfxPool_8:Melee Weapon Air Swing 4 |  |
| Audio | Player 'Death Axe' hitting the enemy plays a hit sound | PASS | 3/3 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Death Axe' on the enemy | PASS | enemy hp lost 47.9 (item dmg 18), hits 3, player right weapon Drawn_Death_Axe |  |
| Audio | Player 'Spear of Fate' swing plays a swish | PASS | 4/4 clicks started a swing; 5 non-ambient audio start(s) outside the hit windows: SfxPool_1:Melee Weapon Attack 4, SfxPool_0:Melee Weapon Air Swing 4, SfxPool_7:Melee Weapon Attack 4 |  |
| Audio | Player 'Spear of Fate' hitting the enemy plays a hit sound | PASS | 22/22 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Spear of Fate' on the enemy | PASS | enemy hp lost 91.7 (item dmg 16), hits 22, player right weapon Drawn_Spear_of_Fate |  |
| Audio | Player 'Sword of Fear' swing plays a swish | PASS | 4/4 clicks started a swing; 4 non-ambient audio start(s) outside the hit windows: SfxPool_6:Melee Weapon Air Swing 2, SfxPool_7:Melee Weapon Attack 28, SfxPool_1:Melee Weapon Attack 3 |  |
| Audio | Player 'Sword of Fear' hitting the enemy plays a hit sound | PASS | 6/6 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Sword of Fear' on the enemy | PASS | enemy hp lost 82.7 (item dmg 15), hits 6, player right weapon Drawn_Sword_of_Fear |  |
| Audio | Player 'weap2_sword' swing plays a swish | PASS | 4/4 clicks started a swing; 5 non-ambient audio start(s) outside the hit windows: SfxPool_3:neverworld, SfxPool_5:Melee Weapon Air Swing 1, SfxPool_8:Melee Weapon Air Swing 2 |  |
| Player weapons | Player uses melee 'weap2_sword' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon Drawn_weap2_sword | 4/4 clicks swung, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'weap_two_handed' swing plays a swish | PASS | 4/4 clicks started a swing; 6 non-ambient audio start(s) outside the hit windows: SfxPool_7:neverworld, SfxPool_8:Melee Weapon Air Swing 1, SfxPool_1:Melee Weapon Attack 4 |  |
| Audio | Player 'weap_two_handed' hitting the enemy plays a hit sound | PASS | 10/10 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'weap_two_handed' on the enemy | PASS | enemy hp lost 211.0 (item dmg 28), hits 10, player right weapon Drawn_Two_Handed_Sword |  |
| Audio | Player 'Wood Axe' swing plays a swish | PASS | 1/4 clicks started a swing; 3 non-ambient audio start(s) outside the hit windows: SfxPool_4:no way out, SfxPool_5:Melee Weapon Air Swing 4, SfxPool_7:neverworld |  |
| Player weapons | Player uses melee 'Wood Axe' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 26), hits 0, player right weapon Drawn_Wood_Axe | 1/4 clicks swung, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Player weapons | Player uses 'DM_Mining_Tool' | INFO | mining tool: Plasma Fuel scanner/laser, not a damage weapon (damage 0 by design) |  |
| Console | Console errors/warnings | WARN | 0 errors, 55 warning(s) | [Warning] Attempting to get `time` on an audio source that has a resource that is not a clip will always return 0. |

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
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Electricity', ammoType Electricity |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: fires | PASS | 1 trigger pull(s), rounds 30 -> 29 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: damages the enemy | PASS | enemy hp lost 16.0 (ammo type Electricity) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: hit rate over aimed shots (shots may miss by design) | PASS | 1/1 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: hit marks / FX | PASS | hit marks stamped 0 -> 1, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 0 -> 1, enemy hp lost 16.0 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Explosive', ammoType Explosive |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires | PASS | 2 trigger pull(s), rounds 30 -> 28 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: damages the enemy | PASS | enemy hp lost 92.3 (ammo type Explosive) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: hit rate over aimed shots (shots may miss by design) | PASS | 2/2 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: hit marks / FX | PASS | hit marks stamped 1 -> 3, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=True; hit marks 1 -> 3, enemy hp lost 92.3 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Fire', ammoType Fire |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: fires | PASS | 2 trigger pull(s), rounds 30 -> 28 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: damages the enemy | PASS | enemy hp lost 75.9 (ammo type Fire) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: hit rate over aimed shots (shots may miss by design) | PASS | 2/2 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: hit marks / FX | PASS | hit marks stamped 3 -> 5, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 3 -> 5, enemy hp lost 75.9 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ice', ammoType Ice |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: fires | PASS | 2 trigger pull(s), rounds 30 -> 28 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: damages the enemy | PASS | enemy hp lost 135.9 (ammo type Ice) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: hit rate over aimed shots (shots may miss by design) | PASS | 2/2 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: hit marks / FX | PASS | hit marks stamped 5 -> 7, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 5 -> 7, enemy hp lost 135.9 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ion', ammoType Ion |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: fires | PASS | 2 trigger pull(s), rounds 30 -> 28 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: damages the enemy | PASS | enemy hp lost 131.8 (ammo type Ion) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: hit rate over aimed shots (shots may miss by design) | PASS | 2/2 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 89), ammo accuracy 89, spread 1.8 deg (ammo 1.2), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: hit marks / FX | INFO | hit marks stamped 7 -> 7, body type Humanoid | this ammo type is configured to leave no hit marks (useHitMarks off / blood and burn Never): by design |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=False; hit marks 7 -> 7, enemy hp lost 131.8 | useHitMarks is off on this ammo: no mark by design |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Laser', ammoType Laser |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: fires | PASS | 2 trigger pull(s), rounds 30 -> 28 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: damages the enemy | PASS | enemy hp lost 135.0 (ammo type Laser) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: hit rate over aimed shots (shots may miss by design) | PASS | 2/2 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.5), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: hit marks / FX | PASS | hit marks stamped 7 -> 9, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=True, delivery=Auto, useHitMarks=False; hit marks 7 -> 9, enemy hp lost 135.0 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: ammo type loads (Hot Cross path) | PASS | loaded 45/45 of 'DMAmmoFxProfile_Plasma', ammoType Plasma |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: fires | PASS | 2 trigger pull(s), rounds 45 -> 43 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: damages the enemy | PASS | enemy hp lost 133.1 (ammo type Plasma) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: hit rate over aimed shots (shots may miss by design) | PASS | 2/2 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 78), ammo accuracy 78, spread 1.8 deg (ammo 1.2), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: hit marks / FX | PASS | hit marks stamped 9 -> 11, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=False; hit marks 9 -> 11, enemy hp lost 133.1 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Resonance Stabilizer', ammoType ResonanceStabilizer |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: fires | PASS | 2 trigger pull(s), rounds 30 -> 28 |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: damages the enemy | PASS | enemy hp lost 63.3 (ammo type ResonanceStabilizer) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: hit rate over aimed shots (shots may miss by design) | PASS | 2/2 aimed shots hit (100 %); weapon accuracy 70 (effective incl. skill bonus 75), ammo accuracy 75, spread 1.8 deg (ammo 1.4), close-range scale 0.22 within 12m, hip multiplier 1.15; aimed at 6m, RMB held, stationary chest target |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: hit marks / FX | PASS | hit marks stamped 11 -> 13, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: per-ammo FX rules (blood / burn / glow / explosive handling) | PASS | enemyBlood=Auto, enemyBurnMark=Auto, spawnLaserBurn=False, delivery=Auto, useHitMarks=True; hit marks 11 -> 13, enemy hp lost 63.3 |  |
| Ammo | X (AmmoCycle) switches the loaded ammo type | PASS | 3 ammo stack(s) on the Hot Cross utility slots; loaded DMAmmoFxProfile_Resonance Stabilizer -> DMAmmoFxProfile_CarveExperiment -> DMAmmoFxProfile_Electricity |  |
| Ammo | Ammo coverage | INFO | 2 weapon(s) tested with their default ammo (full: load, fill, fire, damage, marks, empty, reload); 9 ammo-specific case(s) (all non-default ammo types), each once on a host weapon (load, 2 shots, damage, marks, per-ammo FX rules); no weapon x ammo matrix. Mining tool excluded: it runs on Plasma Fuel charge. |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player rig: enemy ranged shot hits a body bone + mark

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Player rig | Player hitbox rig is built and active (per-bone hitboxes on layer DMHitbox) | PASS | rig present, active True, active rigs 1, hitboxes 18, not on the DMHitbox layer 0, body type Humanoid |  |
| Player rig | Enemy ranged shot ('survival_rifle') resolves to a player body hitbox | FAIL | player hp lost 0.0, shots/projectiles 0, rig marks 3 -> 3, active mark objects 0: on a bone 0, on the root/capsule 0, parents [], range 7m, states Wander/Defensive/Attack | enemy ranged shot did no damage to the player: the shot missed or the hitscan still passes through (the rig supersedes the capsule but nothing resolves on the bone hitboxes) |
| Player rig | Player hit marks are removed automatically after the configured lifetime | WARN | no marks to expire (marks 0), lifetime 10s | no player mark was stamped, so expiry could not be checked |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Aim | Aim follows the moving enemy: chest | FAIL | 3 shots, 0 fired, aim error before firing avg 0.9deg; crosshair hitbox zone Torso (bone VBOT_:Spine2); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Aim follows the moving enemy: head | FAIL | 3 shots, 0 fired, aim error before firing avg 0.5deg; crosshair hitbox zone Arm/Head (bone VBOT_:RightArm/VBOT_:Head); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Aim follows the moving enemy: arm | FAIL | 3 shots, 0 fired, aim error before firing avg 1.0deg; crosshair hitbox zone Torso/none (bone VBOT_:Spine2/none); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Aim follows the moving enemy: leg | FAIL | 3 shots, 0 fired, aim error before firing avg 0.6deg; crosshair hitbox zone Torso/none (bone VBOT_:Hips/none); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Aim follows the moving enemy: foot | FAIL | 3 shots, 0 fired, aim error before firing avg 0.7deg; crosshair hitbox zone none/Leg (bone none/VBOT_:LeftFoot); hit-mark bones none; avg damage 0.0 (xn/a chest) | aimed correctly but no damage registered (ammo empty or projectile missed a moving target) |
| Aim | Body-part damage table (for later headshot/limb multipliers) | INFO | chest: zone Torso, avg dmg 0.0, xn/a vs chest / head: zone Arm/Head, avg dmg 0.0, xn/a vs chest / arm: zone Torso/none, avg dmg 0.0, xn/a vs chest / leg: zone Torso/none, avg dmg 0.0, xn/a vs chest / foot: zone none/Leg, avg dmg 0.0, xn/a vs chest | damage multipliers per zone come from DMCombatBodyPartUtility.ApplyZoneDamageMultiplier (DM_EnemyHitMarkProfile); equal ratios here mean no zone multiplier is configured |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Behavior tweaks: over / under-exaggerated enemy values (runtime only)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Behavior | How the tweaks are applied | INFO | runtime only: values are written to the live component instances of a freshly spawned enemy (destroyed after each variation) and profile tweaks go through in-memory clones swapped in for the global Resources profile and restored afterwards; a disk hash of the profiles, definitions and enemy prefabs is compared at the end. Hit-mark / effect budgets are not covered here (they are visual budgets, see the hit-mark rows). |  |
| Behavior | Normal behaviour (reference): player 9m in front, 8s | INFO | notice 0.1s, chase 0.1s, first attack 1.4s, attacks 3, dmg 109, min dist 1.2m, path 7.9m, yaw 48deg, orbit around player net 46deg / abs 48deg, avg facing error 0deg, brain Press:176/Hold:10, states Hold/Chase/Attack |  |
| Behavior | Aggression / attack rate: high (hi = attacks much faster) | PASS | EnemyCombat.attackCooldown 1 -> 0.25, EnemyCombat.meleeRecoveryPauseMin 0.6 -> 0.15, EnemyCombat.meleeRecoveryPauseMax 1.8 -> 0.45; notice 0.1s, chase 0.1s, first attack 1.8s, attacks 4, dmg 27, min dist 1.2m, path 7.5m, yaw 18deg, orbit around player net 18deg / abs 18deg, avg facing error 0deg, brain Press:152/Hold:9/Defend:8, states Hold/Chase/Attack/Defensive |  |
| Behavior | Aggression / attack rate: low (hi = attacks much faster) | PASS | EnemyCombat.attackCooldown 1 -> 3, EnemyCombat.meleeRecoveryPauseMin 0.6 -> 1.8, EnemyCombat.meleeRecoveryPauseMax 1.8 -> 5.4; notice 0.1s, chase 0.1s, first attack 1.2s, attacks 3, dmg 109, min dist 1.2m, path 7.4m, yaw 29deg, orbit around player net -29deg / abs 29deg, avg facing error 0deg, brain Press:178/Hold:9, states Hold/Chase/Attack |  |
| Behavior | Aggression / attack rate: suggested safe range | PASS | both x0.25 and x3 of the normal values stayed healthy: safe range at least x3 .. x0.25 |  |
| Behavior | Standoff distance: high (hi = stands further off) | PASS | EnemyAiController.attackStandoffFraction 0.62 -> 0.992, EnemyAiController.minCombatSeparation 0.95 -> 1.1; notice 0.1s, chase 0.1s, first attack 1.4s, attacks 3, dmg 130, min dist 1.2m, path 8.0m, yaw 39deg, orbit around player net -39deg / abs 39deg, avg facing error 0deg, brain Press:177/Hold:11/None:1, states Hold/Chase/Attack |  |
| Behavior | Standoff distance: low (hi = stands further off) | PASS | EnemyAiController.attackStandoffFraction 0.62 -> 0.248, EnemyAiController.minCombatSeparation 0.95 -> 0.38; notice 0.1s, chase 0.1s, first attack 1.3s, attacks 3, dmg 158, min dist 1.2m, path 7.2m, yaw 5deg, orbit around player net 1deg / abs 1deg, avg facing error 0deg, brain Press:181/Hold:8/None:1, states Hold/Chase/Attack |  |
| Behavior | Standoff distance: suggested safe range | PASS | both x1.6 and x0.4 of the normal values stayed healthy: safe range at least x0.4 .. x1.6 |  |
| Behavior | Chase / lose-target timings: high (hi = never gives up, lo = gives up at once) | PASS | EnemyAiController.loseTargetDelay 4 -> 16, EnemyAiController.maxChaseDuration 6 -> 24, EnemyAiController.chaseSpeedMultiplier 0.95 -> 3.8; notice 0.1s, chase 0.1s, first attack 0.5s, attacks 4, dmg 159, min dist 1.4m, path 6.5m, yaw 15deg, orbit around player net 15deg / abs 15deg, avg facing error 0deg, never lost the target within 14s, brain Press:179/Hold:11/None:1, states Hold/Chase/Attack |  |
| Behavior | Chase / lose-target timings: low (hi = never gives up, lo = gives up at once) | PASS | EnemyAiController.loseTargetDelay 4 -> 0.8, EnemyAiController.maxChaseDuration 6 -> 1.2, EnemyAiController.chaseSpeedMultiplier 0.95 -> 0.19; notice 0.1s, chase 0.1s, first attack 3.4s, attacks 2, dmg 131, min dist 1.2m, path 8.9m, yaw 28deg, orbit around player net -28deg / abs 28deg, avg facing error 0deg, lost target after 0.8s, brain Press:173/Hold:10, states Hold/Chase/ReturnHome/Attack |  |
| Behavior | Chase / lose-target timings: suggested safe range | PASS | both x4 and x0.2 of the normal values stayed healthy: safe range at least x0.2 .. x4 |  |
| Behavior | Move speeds: high () | PASS | EnemyAiController.walkSpeed 2.4 -> 5.28, EnemyAiController.runSpeed 5.2 -> 11.44; notice 0.1s, chase 0.1s, first attack 0.5s, attacks 4, dmg 185, min dist 1.2m, path 5.7m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:174/Hold:6, states Hold/Chase/Attack |  |
| Behavior | Move speeds: low () | PASS | EnemyAiController.walkSpeed 2.4 -> 0.84, EnemyAiController.runSpeed 5.2 -> 1.82; notice 0.1s, chase 0.1s, first attack 4.3s, attacks 2, dmg 55, min dist 1.7m, path 7.4m, yaw 9deg, orbit around player net -8deg / abs 9deg, avg facing error 0deg, brain Press:180/Hold:20/None:2, states Wander/Hold/Chase/Attack |  |
| Behavior | Move speeds: suggested safe range | PASS | both x2.2 and x0.35 of the normal values stayed healthy: safe range at least x0.35 .. x2.2 |  |
| Behavior | Senses ranges: high () | PASS | EnemySenses.visionRange 16 -> 32, EnemySenses.hearingRange 18 -> 36, EnemySenses.visionFov 110 -> 220; notice 0.1s, chase 0.1s, first attack 1.4s, attacks 3, dmg 159, min dist 1.3m, path 7.9m, yaw 29deg, orbit around player net -29deg / abs 30deg, avg facing error 0deg, brain Press:174/Hold:13/None:2, states Wander/Hold/Chase/Attack |  |
| Behavior | Senses ranges: low () | PASS | EnemySenses.visionRange 16 -> 4.8, EnemySenses.hearingRange 18 -> 5.4, EnemySenses.visionFov 110 -> 33; notice 1.8s, chase 1.8s, first attack 2.6s, attacks 3, dmg 131, min dist 1.2m, path 7.3m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:145/None:39, states Wander/Chase/Attack |  |
| Behavior | Senses ranges: suggested safe range | PASS | both x2 and x0.3 of the normal values stayed healthy: safe range at least x0.3 .. x2 |  |
| Behavior | Reaction chance / stagger: high () | PASS | EnemyInvectorRagdollBridge.baseReactionChance 0.25 -> 1, EnemyInvectorRagdollBridge.defaultStaggerSeconds 0.28 -> 1, EnemyInvectorRagdollBridge.knockdownImpulse 0.28 -> 1; notice 0.1s, chase 0.1s, first attack 1.3s, attacks 4, dmg 110, min dist 1.2m, path 8.3m, yaw 39deg, orbit around player net 39deg / abs 39deg, avg facing error 0deg, brain Press:174/Hold:8, states Hold/Chase/Attack |  |
| Behavior | Reaction chance / stagger: low () | PASS | EnemyInvectorRagdollBridge.baseReactionChance 0.25 -> 0, EnemyInvectorRagdollBridge.defaultStaggerSeconds 0.28 -> 0, EnemyInvectorRagdollBridge.knockdownImpulse 0.28 -> 0; notice 0.1s, chase 0.1s, first attack 1.5s, attacks 4, dmg 130, min dist 1.3m, path 7.9m, yaw 31deg, orbit around player net -31deg / abs 31deg, avg facing error 0deg, brain Press:178/Hold:10/None:2, states Wander/Hold/Chase/Attack |  |
| Behavior | Reaction chance / stagger: suggested safe range | PASS | both x4 and x0 of the normal values stayed healthy: safe range at least x0 .. x4 |  |
| Behavior | Brain archetype weights: high (hi = press x3 / defend x0.3 / retreat 0, lo = press x0.2 / defend x3 / retreat x3) | PASS | archetype rows: press x3, defend x0.3, retreat x0; notice 0.1s, chase 0.1s, first attack 1.4s, attacks 3, dmg 158, min dist 1.2m, path 6.1m, yaw 26deg, orbit around player net 26deg / abs 26deg, avg facing error 0deg, brain Press:182/None:1, states Chase/Attack |  |
| Behavior | Brain archetype weights: low (hi = press x3 / defend x0.3 / retreat 0, lo = press x0.2 / defend x3 / retreat x3) | PASS | archetype rows: press x0.2, defend x3, retreat x3; notice 0.1s, chase 0.1s, first attack 1.0s, attacks 3, dmg 135, min dist 1.2m, path 9.3m, yaw 0deg, orbit around player net 0deg / abs 0deg, avg facing error 0deg, brain Press:167/Defend:17, states Chase/Attack/Defensive |  |
| Behavior | Brain archetype weights: suggested safe range | PASS | both x3 and x0.2 of the normal values stayed healthy: safe range at least x0.2 .. x3 |  |
| Behavior | Brain decision rate / randomness: high (hi = decision every 0.15s and fully random, lo = every 3s and deterministic) | PASS | decisionInterval 0.15, scoreRandomness 1; notice 0.1s, chase 0.1s, first attack 1.0s, attacks 3, dmg 108, min dist 1.2m, path 9.9m, yaw 99deg, orbit around player net 32deg / abs 99deg, avg facing error 0deg, brain Press:166/Defend:26/None:1, states Chase/Attack/Defensive |  |
| Behavior | Brain decision rate / randomness: low (hi = decision every 0.15s and fully random, lo = every 3s and deterministic) | PASS | decisionInterval 3, scoreRandomness 0; notice 0.1s, chase 0.1s, first attack 1.6s, attacks 3, dmg 158, min dist 1.2m, path 6.5m, yaw 50deg, orbit around player net -50deg / abs 50deg, avg facing error 0deg, brain Press:188/None:1, states Chase/Attack |  |
| Behavior | Brain decision rate / randomness: suggested safe range | PASS | both x1 and x0 of the normal values stayed healthy: safe range at least x0 .. x1 |  |
| Behavior | Engagement tuning (3 FREDs): high (token hold, ring radius, holder spacing) | PASS | tokenMinHoldSeconds 7, holdRingRadius 7.5 (6..9), holderMinSpacing 3.5; max simultaneous 2 (12/206), distinct attackers 2/3, hand-offs 0, closest pair 1.68m, orbit max 56deg, player hp lost 81 |  |
| Behavior | Engagement tuning (3 FREDs): low (token hold, ring radius, holder spacing) | PASS | tokenMinHoldSeconds 0, holdRingRadius 2.2 (1.8..3.2), holderMinSpacing 0.6; max simultaneous 2 (55/194), distinct attackers 3/3, hand-offs 6, closest pair 1.16m, orbit max 96deg, player hp lost 54 |  |
| Behavior | Engagement tuning (3 FREDs): suggested safe range | PASS | both x3 and x0.2 of the normal values stayed healthy: safe range at least x0.2 .. x3 |  |
| Behavior | Tweaks left nothing on disk (profiles, definitions, enemy prefabs hashed before/after) | PASS | identical |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | PASS | code present: True; 8 PASS / 0 FAIL / 0 warn of 8 runtime row(s) |  |
| Fix check | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | WARN | code present: True; 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s) |  |
| Fix check | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | PASS | 1 PASS / 0 FAIL / 0 warn of 1 runtime row(s); weapon reverts recorded this run: 0 |  |
| Fix check | vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons) | PASS | code present: True; console errors across 0 weapon-matrix case(s): 0 |  |
| Fix check | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | WARN | code present: True; dual-slot override case: not run |  |
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 18 value(s) compared on 2 enemies |  |
| Fix check | No circling while unaware (patrol-anchor fix) on every enemy | PASS | 3 PASS / 0 FAIL of 3 enemies |  |
| Fix check | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | PASS | 3 PASS / 0 FAIL of 3 enemies |  |
| Fix check | Profile adjustments made by earlier audit passes | INFO | none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes |  |

## Per-enemy detail

### Humadroid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 29s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 45ms, 761 components, frame avg 34.5ms max 72.5ms (261 frames), GC collections 0, managed delta 9312KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 223deg, path 12.3m, net 5.8m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.6s (at 1.6m, weapon weap2_sword), first player damage 3.0s, attacks 1, states Patrol/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 3.0s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.5s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.9 (item dmg 14), shots/projectiles 0, attack-state frames 47, attacks 1, first hit 2.5s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 1 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 197.6 (-2.4); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 197.6 -> 197.6; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | WARN | marks stamped 0 -> 0, body type Android | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | FAIL | loot bag held 0 entries, TryLootAll -> None, left 0; definition random loot count 0-1 | bag spawned but nothing could be looted (empty entries / TryLootAll refused) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 27 distinct clips seen in total |  |
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

### Robot

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `Robot_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 30s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Robot_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=15, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 37ms, 698 components, frame avg 36.6ms max 63.0ms (246 frames), GC collections 0, managed delta 16876KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 141deg, path 9.0m, net 4.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 2.7s (at 1.7m, weapon weap2_sword), first player damage 4.2s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 4.2s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 15.0 (item dmg 14), shots/projectiles 1, attack-state frames 47, attacks 1, first hit 3.3s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 469.2 (-30.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 469.2 -> 469.2; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Robot |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | FAIL | loot bag held 0 entries, TryLootAll -> None, left 0; definition random loot count 0-1 | bag spawned but nothing could be looted (empty entries / TryLootAll refused) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 30 distinct clips seen in total |  |
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
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 37ms, 693 components, frame avg 36.0ms max 166.5ms (252 frames), GC collections 1, managed delta -249972KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 130deg, path 7.1m, net 3.3m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.7s (at 1.7m, weapon Blade Of Chaos), first player damage 2.2s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 30.0 hp in 2.2s using Blade Of Chaos |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 30.0 (item dmg 21), attacks 1, hitbox-active frames 6 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Blade Of Chaos': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 30.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.9 (item dmg 14), shots/projectiles 1, attack-state frames 40, attacks 1, first hit 2.5s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 2.3m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Blade_Of_Chaos] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 276.0 (-24.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 276.0 -> 276.0; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 1 entry, TryLootAll -> Granted, left 0, 1 granted item(s) removed again from the audit inventory; definition random loot count 0-1 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.4s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 23 distinct clips seen in total |  |
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
