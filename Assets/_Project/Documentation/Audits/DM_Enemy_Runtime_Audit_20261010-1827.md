# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-10 18:07:52 -> 2026-10-10 18:27:08 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-780.0, 21.2, -122.1), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 4; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 10
- Weapon matrix: 11 weapon items equipped one at a time (spawn override) on HumanoidEnemy_Invector; plus one dual-slot override case
- Player level raised in memory from 5 for equip gates (restored at the end; nothing saved)

**10 enemies audited - 1 FAIL, 0 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 12 weapon cases + 11 scenarios): 289 PASS, 50 FAIL, 39 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 17:30: 202 change(s).

- CHANGED enemy|GreySpire Soldier|notice: WARN -> PASS
- CHANGED enemy|Humanoid|player.swing: WARN -> PASS
- CHANGED enemy|The_Evil_One|melee.damage: FAIL -> PASS
- NEW weapon|2 Hander|audio.swing.2 Hander = PASS
- NEW weapon|2 Hander|audio.hitplayer.2 Hander = FAIL
- NEW weapon|Blade Of Chaos|audio.swing.Blade Of Chaos = PASS
- NEW weapon|Blade Of Chaos|audio.hitplayer.Blade Of Chaos = PASS
- NEW weapon|Death Axe|audio.swing.Death Axe = PASS
- NEW weapon|Death Axe|audio.hitplayer.Death Axe = PASS
- NEW weapon|Spear of Fate|audio.swing.Spear of Fate = PASS
- NEW weapon|Spear of Fate|audio.hitplayer.Spear of Fate = PASS
- NEW weapon|Sword of Fear|audio.swing.Sword of Fear = PASS
- CHANGED weapon|Sword of Fear|w.damage: PASS -> FAIL
- NEW weapon|weap2_sword|audio.swing.weap2_sword = FAIL
- NEW weapon|weap2_sword|audio.hitplayer.weap2_sword = FAIL
- NEW weapon|weap_two_handed|audio.swing.weap_two_handed = PASS
- NEW weapon|weap_two_handed|audio.hitplayer.weap_two_handed = FAIL
- NEW weapon|Wood Axe|audio.swing.Wood Axe = FAIL
- NEW weapon|Wood Axe|audio.hitplayer.Wood Axe = FAIL
- NEW scenario|Camera: faces forward after teleports, zoom in/out|cam.forward = PASS
- NEW scenario|Camera: faces forward after teleports, zoom in/out|cam.zoom = WARN
- NEW scenario|Camera: faces forward after teleports, zoom in/out|console = PASS
- NEW scenario|Audio: swing / hit sound wiring (static)|audio.profile = PASS
- NEW scenario|Audio: swing / hit sound wiring (static)|audio.swing.callers = FAIL
- NEW scenario|Audio: swing / hit sound wiring (static)|audio.hit.callers = WARN
- NEW scenario|Audio: swing / hit sound wiring (static)|console = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.W = FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.S = FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.A = FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.D = FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.run = FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.sprint = FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.jump = FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.crouch = FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.dodge = FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.dash = WARN
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.jetpack = WARN
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.look.x = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.look.y = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.lmb = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.rmb = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.mmb = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.mmb.note = INFO
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.switch = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.walk = WARN
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|console = PASS
- CHANGED scenario|Block + parry: player defends against enemy melee|blk.stagger: INFO -> WARN
- CHANGED scenario|Block: enemy defends against player swings|eblk: FAIL -> WARN
- CHANGED scenario|Movement: retreat, push, flank, backstab, lose target|mv.backstab: INFO -> WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.approach = WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.kite = WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.strafe = WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.reengage = WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.jump = WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.jetpack = WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.lose = PASS
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.return = WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|console = PASS
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.2 Hander = PASS
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.phit.2 Hander = PASS
- CHANGED scenario|Player weapons: each melee/ranged item vs enemy|pw.2 Hander: FAIL -> PASS
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.Blade Of Chaos = PASS
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.phit.Blade Of Chaos = PASS
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.Death Axe = PASS
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.phit.Death Axe = PASS
- CHANGED scenario|Player weapons: each melee/ranged item vs enemy|pw.Death Axe: FAIL -> PASS
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.Spear of Fate = PASS
- CHANGED scenario|Player weapons: each melee/ranged item vs enemy|pw.Spear of Fate: PASS -> FAIL
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.Sword of Fear = PASS
- CHANGED scenario|Player weapons: each melee/ranged item vs enemy|pw.Sword of Fear: PASS -> FAIL
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.weap2_sword = PASS
- CHANGED scenario|Player weapons: each melee/ranged item vs enemy|pw.weap2_sword: PASS -> FAIL
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.weap_two_handed = PASS
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.Wood Axe = PASS
- CHANGED scenario|Player weapons: each melee/ranged item vs enemy|pw.Wood Axe: PASS -> FAIL
- CHANGED scenario|Player weapons: each melee/ranged item vs enemy|pw.DM_Mining_Tool: FAIL -> INFO
- NEW scenario|Player weapons: each melee/ranged item vs enemy|pw.sci_fi_pistol.ammo = WARN
- NEW scenario|Player weapons: each melee/ranged item vs enemy|pw.survival_rifle.ammo = WARN
- NEW scenario|Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload|ammo.inventory = INFO
- NEW scenario|Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload|ammo.sci_fi_pistol.DMAmmoFxProfile_CarveExperiment.load = PASS
- ... 122 more

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 100 | FAIL | Melee 'Sword of Fear' damages the player | Sword of Fear | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=23 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 23 frames (definition missRate 0.25) | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 2 | 100 | FAIL | Player uses melee 'Spear of Fate' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 3 | 100 | FAIL | Player uses melee 'Sword of Fear' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 4 | 100 | FAIL | Player uses melee 'weap2_sword' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 5 | 100 | FAIL | Player uses melee 'weap_two_handed' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 6 | 100 | FAIL | Player uses melee 'Wood Axe' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 7 | 100 | FAIL | Player uses ranged 'sci_fi_pistol' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 shots at 6m did nothing: no ammo/empty magazine (needs reload), aim/fire input not received, or projectile damage mapped to 0 (item dmg 14) | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 8 | 100 | FAIL | Player uses ranged 'survival_rifle' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 shots at 6m did nothing: no ammo/empty magazine (needs reload), aim/fire input not received, or projectile damage mapped to 0 (item dmg 18) | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 9 | 75 | FAIL | No error-pink / null materials | Lisa_Hybrid | broken visuals: character_head:null material, eye_L:null material, eye_R:null material, Helmet_inside:null material (shader missing for the HDRP pipeline or material slot empty) | Repair the prefab (re-add the script / fix the material / assign controller+avatar) via the Character Creator Fix or by hand. |
| 10 | 75 | WARN | Ranged 'DM_Mining_Tool' damages the player | DM_Mining_Tool | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 11 | 75 | WARN | 'sci_fi_pistol' ammo loads | Player weapons: each melee/ranged item vs enemy | default ammo could not be loaded through the Hot Cross path | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 12 | 75 | WARN | 'survival_rifle' ammo loads | Player weapons: each melee/ranged item vs enemy | default ammo could not be loaded through the Hot Cross path | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 13 | 70 | FAIL | Holding block negates enemy melee damage | Block + parry: player defends against enemy melee | player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 14 | 45 | WARN | Enemy guard-break stagger after a blocked hit | Block + parry: player defends against enemy melee | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 15 | 45 | WARN | Enemy block reduces player melee damage | Block: enemy defends against player swings | player swings registered no hits on the open enemy, so the comparison is inconclusive (input focus, reach, or the player weapon) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 16 | 20 | FAIL | '2 Hander' hitting the player plays a hit sound | 2 Hander | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 17 | 20 | FAIL | 'weap2_sword' swing plays a swish | weap2_sword | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 18 | 20 | FAIL | 'weap2_sword' hitting the player plays a hit sound | weap2_sword | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 19 | 20 | FAIL | 'weap_two_handed' hitting the player plays a hit sound | weap_two_handed | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 20 | 20 | FAIL | 'Wood Axe' swing plays a swish | Wood Axe | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 21 | 20 | FAIL | 'Wood Axe' hitting the player plays a hit sound | Wood Axe | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 22 | 20 | FAIL | Swing swish (GameAudioManager.PlayWeaponSwing) is called by gameplay code | Audio: swing / hit sound wiring (static) | PlayWeaponSwing / PlayPunchSwing are never called, so NO weapon (player or enemy) ever plays a swish: add a call where a melee attack window opens (vMeleeManager.SetActiveAttack active=true or the animation event OnEnableAttack) | Inspect the measured value and cause on the detail row. |
| 23 | 20 | FAIL | W moves the player forward (camera direction) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move action not reaching the controller: PlayerInput disabled, move locked, or PioneerPlayerInputBinder.HandleAction('Move') not wired | Inspect the measured value and cause on the detail row. |
| 24 | 20 | FAIL | S moves the player back | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (S) produced no backward displacement | Inspect the measured value and cause on the detail row. |
| 25 | 20 | FAIL | A strafes left | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (A) produced no leftward displacement | Inspect the measured value and cause on the detail row. |
| 26 | 20 | FAIL | D strafes right | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Move (D) produced no rightward displacement | Inspect the measured value and cause on the detail row. |
| 27 | 20 | FAIL | W run speed (reference for sprint) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | player did not move | Inspect the measured value and cause on the detail row. |
| 28 | 20 | FAIL | Shift + W sprints faster than W | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | sprint no faster than run (0.0m): Sprint action not wired, stamina 0, or sprint speed equals run speed | Inspect the measured value and cause on the detail row. |
| 29 | 20 | FAIL | Space jumps | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked | Inspect the measured value and cause on the detail row. |
| 30 | 20 | FAIL | Left Ctrl crouches | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Crouch action not wired or blocked (ammo popup open, building mode) | Inspect the measured value and cause on the detail row. |
| 31 | 20 | FAIL | Q dodge / roll | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dodge action did not move the player: TryDodgeRollFromInput refused (stamina, cooldown, no roll clip) | Inspect the measured value and cause on the detail row. |
| 32 | 20 | WARN | Player circles behind the enemy: enemy turns to face | Movement: retreat, push, flank, backstab, lose target | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 33 | 20 | WARN | Hit from behind vs from the front (player melee, AI off) | Movement: retreat, push, flank, backstab, lose target | swings did not land from one side (reach/facing/input), comparison inconclusive | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 34 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 35 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Electricity: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 36 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 37 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Fire: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 38 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Ice: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 39 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Ion: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 40 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Laser: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 41 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Plasma: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 42 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 43 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Standard: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 44 | 20 | FAIL | X (AmmoCycle) switches the loaded ammo type | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | X tap did not cycle: AmmoCycle owned by GameplayKeyboardShortcuts poll (needs drawn ranged weapon + Hot Cross focus) or the next type does not fit the weapon | Inspect the measured value and cause on the detail row. |
| 45 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory | Inspect the measured value and cause on the detail row. |
| 46 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_CarveExperiment: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 47 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Electricity: ammo type loads (Hot Cross path) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory | Inspect the measured value and cause on the detail row. |
| 48 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Electricity: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 49 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory | Inspect the measured value and cause on the detail row. |
| 50 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Explosive: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 51 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Fire: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 52 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Ice: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 53 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Ion: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 54 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Laser: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 55 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Plasma: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 56 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 57 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Standard: fires (rounds consumed) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused | Inspect the measured value and cause on the detail row. |
| 58 | -5 | WARN | Camera zooms in and out | Camera: faces forward after teleports, zoom in/out | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state | Inspect the measured value and cause on the detail row. |
| 59 | -5 | WARN | Melee hit sound has a caller on the damage path | Audio: swing / hit sound wiring (static) | the only callers are the enemy-body receiver (PioneerInvectorDamageReceiver), MeleeCombatController and CombatHitResolver: damage TO THE PLAYER (PioneerInvectorDamageBridge / the player's vDamageReceiver) has no hit-sound call, so enemy weapons hitting the player are silent | Inspect the measured value and cause on the detail row. |
| 60 | -5 | WARN | Left Alt dash | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dash action not wired or on cooldown (DMDashController) | Inspect the measured value and cause on the detail row. |

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
| Lisa_Hybrid | - | survival_rifle | PASS | PASS | PASS | PASS | n/a | PASS | PASS | PASS | PASS | PASS |
| Robot | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| The_Evil_One | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |

## Weapons that deal NO damage (headline)

- **Weapon matrix: Sword of Fear** (melee): player hp lost 0.0 (item dmg 15), attacks 6, hitbox-active frames 272, hit events 3, sub-1hp chip 10.1, first hit never, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True.... **Probable cause:** hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=23 frames, ItemData roll) or the player blocked/ignored it | intentional-miss active for 23 frames (definition missRate 0.25)

## Weapon matrix (every weapon item on the template, spawn override)

12 cases, 7 FAIL. Each weapon is equipped on `HumanoidEnemy_Invector` through ConfigureSpawnLoadout (the spawner path) and made to attack the player.

| Weapon | Kind | Equips | Damages player | One drawn | Measured | Probable cause |
|---|---|---|---|---|---|---|
| 2 Hander | melee | PASS | PASS | PASS | player hp lost 38.0 (item dmg 28), attacks 2, hitbox-active frames 12, hit events 1, sub-1hp chip 6.0, first hit 2.3s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer ... | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Blade Of Chaos | melee | PASS | PASS | PASS | player hp lost 30.0 (item dmg 21), attacks 2, hitbox-active frames 182, hit events 1, sub-1hp chip 1.1, first hit 8.5s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; h... |  |
| Death Axe | melee | PASS | PASS | PASS | player hp lost 24.0 (item dmg 18), attacks 5, hitbox-active frames 266, hit events 4, sub-1hp chip 0.9, first hit 12.8s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: la... |  |
| Spear of Fate | melee | PASS | PASS | PASS | player hp lost 20.2 (item dmg 16), attacks 1, hitbox-active frames 7, hit events 1, sub-1hp chip 1.1, first hit 0.1s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBo... |  |
| Sword of Fear | melee | PASS | **FAIL** | PASS | player hp lost 0.0 (item dmg 15), attacks 6, hitbox-active frames 272, hit events 3, sub-1hp chip 10.1, first hit never, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hi... | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=23 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 23 frames (definition missRate 0.25) |
| weap2_sword | melee | PASS | PASS | PASS | player hp lost 26.1 (item dmg 18), attacks 1, hitbox-active frames 7, hit events 1, sub-1hp chip 1.4, first hit 0.1s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: l... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event / weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| weap_two_handed | melee | PASS | PASS | PASS | player hp lost 38.0 (item dmg 28), attacks 2, hitbox-active frames 154, hit events 1, sub-1hp chip 1.0, first hit 6.4s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38... | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Wood Axe | melee | PASS | PASS | PASS | player hp lost 31.1 (item dmg 26), attacks 1, hitbox-active frames 5, hit events 1, sub-1hp chip 1.0, first hit 0.0s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer I... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event / weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| DM_Mining_Tool | ranged | PASS | warn | PASS | player hp lost 14.9 (item dmg 0), shots/projectiles 1, attack-state frames 24, attacks 1, first hit 0.9s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCoo... |  |
| sci_fi_pistol | ranged | PASS | PASS | PASS | player hp lost 17.9 (item dmg 14), shots/projectiles 1, attack-state frames 19, attacks 1, first hit 0.7s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRang... |  |
| survival_rifle | ranged | PASS | PASS | PASS | player hp lost 18.3 (item dmg 18), shots/projectiles 1, attack-state frames 25, attacks 1, first hit 0.9s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRa... |  |
| OVERRIDE both slots | dual-slot spawn override | - | - | PASS | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)): PASS ; Starts with the ranged override drawn (prefer-ranged on, start-melee off): PASS ; Switch to the melee override vi... |  |

### Weapon: 2 Hander

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item 2 Hander; drawn [Drawn_2_Hander]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | '2 Hander' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_3:QP01 0005 Wind howling, SfxPool_5:gravel2, SfxPool_6:Melee Weapon Attack 1 |  |
| Audio | '2 Hander' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_2_Hander': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee '2 Hander' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 2, hitbox-active frames 12, hit events 1, sub-1hp chip 6.0, first hit 2.3s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 2.3s, attacks 2, attack-state frames 73, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Blade Of Chaos

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 21 |  |
| Audio | 'Blade Of Chaos' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_9:QP01 0005 Wind howling, SfxPool_0:QP01 0005 Wind howling, SfxPool_3:Melee Weapon Attack 3 |  |
| Audio | 'Blade Of Chaos' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_4:Melee Weapon Attack 1; vHitEffects on 'Drawn_Blade_Of_Chaos': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 30.0 (item dmg 21), attacks 2, hitbox-active frames 182, hit events 1, sub-1hp chip 1.1, first hit 8.5s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 4.9s (at 1.5m), first damage 8.5s, attacks 2, attack-state frames 241, clips seen: WeakAttack_SwordA, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Death Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'Death Axe' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_4:bellSound, SfxPool_6:QP01 0005 Wind howling, SfxPool_7:QP01 0005 Wind howling, GameAudioManager:(one-shot) |  |
| Audio | 'Death Axe' hitting the player plays a hit sound | PASS | new audio at impact: GameAudioManager:(one-shot); vHitEffects on 'Drawn_Death_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 24.0 (item dmg 18), attacks 5, hitbox-active frames 266, hit events 4, sub-1hp chip 0.9, first hit 12.8s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 12.8s, attacks 5, attack-state frames 371, clips seen: Attack2Hand_C, WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Spear of Fate

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 16 |  |
| Audio | 'Spear of Fate' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_4:QP01 0005 Wind howling |  |
| Audio | 'Spear of Fate' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_4:QP01 0005 Wind howling; vHitEffects on 'Drawn_Spear_of_Fate': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 20.2 (item dmg 16), attacks 1, hitbox-active frames 7, hit events 1, sub-1hp chip 1.1, first hit 0.1s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.3m), first damage 0.1s, attacks 1, attack-state frames 17, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Sword of Fear

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Sword of Fear; drawn [Drawn_Sword_of_Fear]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 15 |  |
| Audio | 'Sword of Fear' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_3:QP01 0005 Wind howling |  |
| Weapons | Melee 'Sword of Fear' damages the player | FAIL | player hp lost 0.0 (item dmg 15), attacks 6, hitbox-active frames 272, hit events 3, sub-1hp chip 10.1, first hit never, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True, type=-1 | hit events fired but the player's health did not drop: damage resolved to 0 (EnemyInvectorOutgoingDamageBridge intentional-miss=23 frames, ItemData roll) or the player blocked/ignored it / intentional-miss active for 23 frames (definition missRate 0.25) |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage never, attacks 6, attack-state frames 398, clips seen: WeakAttack_SwordA, WeakAttack_SwordB, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap2_sword

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'weap2_sword' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'weap2_sword' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_weap2_sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.1 (item dmg 18), attacks 1, hitbox-active frames 7, hit events 1, sub-1hp chip 1.4, first hit 0.1s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.0m), first damage 0.1s, attacks 1, attack-state frames 18, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap_two_handed

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | 'weap_two_handed' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_1:QP01 0005 Wind howling |  |
| Audio | 'weap_two_handed' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_Two_Handed_Sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 2, hitbox-active frames 154, hit events 1, sub-1hp chip 1.0, first hit 6.4s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 4.2s (at 1.5m), first damage 6.4s, attacks 2, attack-state frames 198, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Wood Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 26 |  |
| Audio | 'Wood Axe' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Wood Axe' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_Wood_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee 'Wood Axe' damages the player | PASS | player hp lost 31.1 (item dmg 26), attacks 1, hitbox-active frames 5, hit events 1, sub-1hp chip 1.0, first hit 0.0s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.0s, attacks 1, attack-state frames 15, clips seen: WeakAttack_SwordA, WeakAttack_SwordB, WeakAttack_SwordC |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: DM_Mining_Tool

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item DM_Mining_Tool; drawn []; holstered []; item dmg 0 | ItemData rangedDamage = 0 |
| Weapons | Ranged 'DM_Mining_Tool' damages the player | WARN | player hp lost 14.9 (item dmg 0), shots/projectiles 1, attack-state frames 24, attacks 1, first hit 0.9s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item |
| Timing | Attack timing | INFO | first attack 0.9s (at 6.8m), first damage 0.9s, attacks 1, attack-state frames 24, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: sci_fi_pistol

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; item dmg 14 |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.9 (item dmg 14), shots/projectiles 1, attack-state frames 19, attacks 1, first hit 0.7s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Timing | Attack timing | INFO | first attack 0.6s (at 7.2m), first damage 0.7s, attacks 1, attack-state frames 19, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: survival_rifle

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_weap2_sword]; item dmg 18 |  |
| Weapons | Ranged 'survival_rifle' damages the player | PASS | player hp lost 18.3 (item dmg 18), shots/projectiles 1, attack-state frames 25, attacks 1, first hit 0.9s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Timing | Attack timing | INFO | first attack 0.8s (at 6.8m), first damage 0.9s, attacks 1, attack-state frames 25, clips seen: none matched |  |
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
| Audio: swing / hit sound wiring (static) | 2 | 1 | 1 | 0s |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | 7 | 9 | 3 | 55s |
| Block + parry: player defends against enemy melee | 2 | 1 | 1 | 49s |
| Block: enemy defends against player swings | 1 | 0 | 1 | 21s |
| Movement: retreat, push, flank, backstab, lose target | 4 | 0 | 2 | 33s |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | 2 | 0 | 7 | 81s |
| Multi-enemy: 3 at once | 2 | 0 | 0 | 23s |
| Player weapons: each melee/ranged item vs enemy | 15 | 7 | 2 | 96s |
| Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | 38 | 24 | 20 | 161s |
| Performance per enemy (spawn cost, frame time, GC) | 11 | 0 | 0 | 73s |

### Scenario: Camera: faces forward after teleports, zoom in/out

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Camera | Camera sits behind the player and faces the same way after teleports | PASS | yaw 0: dot 1.00; yaw 90: dot 1.00; yaw 180: dot 1.00; yaw 270: dot 1.00 (camera vThirdPersonCamera) |  |
| Camera | Camera zooms in and out | WARN | distance start 2.5m, zoomed in 2.5m, zoomed out 6.2m via ForceSetZoomDistance fallback (scroll input did not move the camera) | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Audio: swing / hit sound wiring (static)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Audio | GameAudioProfile combat clips assigned | PASS | weaponSwingClips 4, weaponHitClips 3, weaponCriticalHitClips 2 |  |
| Audio | Swing swish (GameAudioManager.PlayWeaponSwing) is called by gameplay code | FAIL | callers outside GameAudioManager: NONE | PlayWeaponSwing / PlayPunchSwing are never called, so NO weapon (player or enemy) ever plays a swish: add a call where a melee attack window opens (vMeleeManager.SetActiveAttack active=true or the animation event OnEnableAttack) |
| Audio | Melee hit sound has a caller on the damage path | WARN | CombatHitAudio.PlayWeaponHit callers: PioneerInvectorDamageReceiver.cs, MeleeCombatController.cs | the only callers are the enemy-body receiver (PioneerInvectorDamageReceiver), MeleeCombatController and CombatHitResolver: damage TO THE PLAYER (PioneerInvectorDamageBridge / the player's vDamageReceiver) has no hit-sound call, so enemy weapons hitting the player are silent |
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
| Controls | Space jumps | FAIL | start 21.23, min 21.23, max 21.23, end 21.23 m height | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked |
| Controls | Left Ctrl crouches | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 isCrouching | Crouch action not wired or blocked (ammo popup open, building mode) |
| Controls | Q dodge / roll | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 m or isRolling | Dodge action did not move the player: TryDodgeRollFromInput refused (stamina, cooldown, no roll clip) |
| Controls | Left Alt dash | WARN | start 0.00, min 0.00, max 0.00, end 0.00 m | Dash action not wired or on cooldown (DMDashController) |
| Controls | Hold Space after jumping boosts (jetpack) | WARN | start 21.23, min 21.23, max 21.23, end 21.23 m height | no sustained lift: jetpack not unlocked/fuelled, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player |
| Controls | Mouse look left/right orbits the camera | PASS | start 90.00, min 0.00, max 90.00, end 59.00 deg yaw |  |
| Controls | Mouse look up/down tilts the camera | PASS | start 16.53, min 0.00, max 48.45, end 48.48 deg pitch |  |
| Controls | Left click swings the drawn melee weapon | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isAttacking |  |
| Controls | Right click blocks (melee drawn) | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isBlocking |  |
| Controls | Middle click does not break gameplay | PASS | start 0.00, min 0.00, max 0.00, end 0.00 none |  |
| Controls | Middle click has no gameplay binding | INFO | InputSystem_Actions Player map binds no action to Mouse/middleButton (only the UI map MiddleClick) |  |
| Controls | Tab (SwitchWeapon) cycles the weapon focus | PASS | Hot Cross weapon focus 0 -> 1, drawn weap2_sword -> sci_fi_pistol |  |
| Controls | Half-tilt stick walks slower than a run | WARN | start 0.00, min 0.00, max 0.85, end 0.87 m in 1.5s | walk speed not distinct from run (0.0m): analog Move magnitude ignored by the locomotion speed |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block + parry: player defends against enemy melee

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Unblocked baseline (same enemy, same distance) | INFO | hits 1, player hp lost 1.1 |  |
| Block | Holding block negates enemy melee damage | FAIL | isBlocking False (raised in 1.52s), hits 2, blocked 0, player hp lost 52.7 (unblocked 1.1), stamina 1000.0 | player isBlocking never became true after holding RMB: injected right-click not received (Game view focus), stamina 0, or player in customAction/attacking |
| Block | Enemy guard-break stagger after a blocked hit | WARN | longest enemy stagger 0.00s (profile blockStaggerSeconds 1.2s) | no enemy hit-stagger observed after the block (DMEnemyGuardBreakStagger skipped: no EnemyInvectorRagdollBridge/attacker, or profile blockStaggerSeconds 0) |
| Parry | Parry timing windows (block pressed relative to the enemy swing) | INFO | press +0.00s after swing start: hit 1.10s after press -> HIT (block came too late), stagger 0.00s, hp lost 47.6 // press +0.15s after swing start: hit 3.35s after press -> HIT (block came too late), stagger 0.00s, hp lost 52.6 // press +0.30s after swing start: hit 0.24s after press -> BLOCK, stagger 1.18s, hp lost 0.0 // press +0.45s after swing start: hit 0.06s after press -> PARRY, stagger 3.40s, hp lost 0.0 // press +0.60s after swing start: hit 1.00s after press -> HIT (block came too late), stagger 0.00s, hp lost 79.0 | profile: parryWindowSeconds 0.2, blockStagger 1.2s, parryStagger 5.0s |
| Parry | A parry happens when block is pressed inside the window | PASS | parry observed inside the window |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block: enemy defends against player swings

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Enemy block reduces player melee damage | WARN | open: 0 hits, enemy hp lost 0.0; blocking (animator IsBlocking True, duration 40.0s): 0 hits (0 flagged blocked), enemy hp lost 31.1; block clips: Idle, Walk, Idle@Pistol, Defense_Unarmed | player swings registered no hits on the open enemy, so the comparison is inconclusive (input focus, reach, or the player weapon) |
| Parry | Enemy parry | INFO | not implemented: EnemyInvectorCombatBridge only has TryBeginBlock/EndBlock (no parry window/riposte API) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Movement: retreat, push, flank, backstab, lose target

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Movement | Player retreats 9m at 3 m/s: enemy follows | PASS | distance 3.7m -> 5.3m (min 2.2m), states Chase/Attack, attacks 1 |  |
| Movement | Player pushes into the enemy: spacing and facing | PASS | min distance 0.50m, max facing error 0deg, attack-state frames 47, states Attack/Chase |  |
| Movement | Player circles behind the enemy: enemy turns to face | WARN | reached behind at never, enemy faced player (<30deg) after never, max facing error 0deg, attacks while behind 0, player hp lost -25.7, states Chase/ReturnHome | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player |
| Movement | Hit from behind vs from the front (player melee, AI off) | WARN | front: 0 hits, 0.0 hp; behind: 0 hits, 0.0 hp; ratio n/a | swings did not land from one side (reach/facing/input), comparison inconclusive |
| Movement | Player disappears 70m away: enemy loses the target | PASS | was aware True, investigate/search after never, back to calm state after 3.5s, states Chase/ReturnHome |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Play | Approach with W: enemy notices | WARN | walked 9.2m in 14.0s to 20.8m; enemy noticed after never at n/a; state Wander | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) |
| Play | Run away with S: enemy pursues | WARN | player backed away for 4.0s (gap now 20.7m), enemy moved 0.4m, hostile state seen False, states Wander | enemy did not chase a retreating player: chase blocked (standoff/hold token, ranged keep-distance) or target lost immediately |
| Play | Strafe around the enemy: it turns to face the player | WARN | enemy facing error avg 152deg, max 164deg over 124 frames while strafing D then A, state Wander | enemy did not keep facing a strafing player: turn speed too slow, turn-in-place gated, or enemy in a state that ignores the target |
| Play | Retreat then re-engage: enemy fights back | WARN | re-engaged to 22.9m, enemy attacks 0, player hp lost 0.0, state Wander | enemy never attacked a player standing next to it: attack cooldown/token starvation or stagger lock |
| Play | Jump away: player clears distance | WARN | jump height 0.00m, gap 22.9m -> 26.2m, enemy state Wander | jump did not leave the ground (Jump input / grounded) |
| Play | Jetpack escape (hold Space + S) | WARN | peak height gain 0.0m, gap 26.2m -> 25.7m, enemy state Wander | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump |
| Play | Sprint away: enemy loses the target | PASS | lost after 1.5s at 25.7m (sprinting), state Wander, states Wander |  |
| Play | Enemy returns to its post after losing the player | WARN | back home after never, now 20.6m from spawn, state Attack | enemy did not walk back to its spawn within 30s: ReturnHome blocked, leash undefined, or it stays in Search/Investigate |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Multi-enemy: 3 at once

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Multi-enemy | 3 enemies at once for 22s | PASS | max simultaneous attackers 2, distinct attackers 3/3, attack-state frames [195,251,318], chase/hold frames [236,209,143], closest pair 1.93m, closest to player 0.82m, player hp lost 25.6 (topped up every 2s), 462 frames |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player weapons: each melee/ranged item vs enemy

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Audio | Player '2 Hander' swing plays a swish | PASS | 3 audio start(s) outside the hit windows over 4 swings: SfxPool_2:QP01 0005 Wind howling, SfxPool_6:QP01 0005 Wind howling, SfxPool_7:Melee Weapon Attack 4 |  |
| Audio | Player '2 Hander' hitting the enemy plays a hit sound | PASS | 14/30 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee '2 Hander' on the enemy | PASS | enemy hp lost 127.7 (item dmg 28), hits 30, player right weapon Drawn_weap2_sword |  |
| Audio | Player 'Blade Of Chaos' swing plays a swish | PASS | 2 audio start(s) outside the hit windows over 4 swings: SfxPool_1:QP01 0005 Wind howling, SfxPool_5:Melee Weapon Attack 3, SfxPool_9:Melee Weapon Attack 3 |  |
| Audio | Player 'Blade Of Chaos' hitting the enemy plays a hit sound | PASS | 14/20 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Blade Of Chaos' on the enemy | PASS | enemy hp lost 95.6 (item dmg 21), hits 20, player right weapon Drawn_weap2_sword |  |
| Audio | Player 'Death Axe' swing plays a swish | PASS | 1 audio start(s) outside the hit windows over 4 swings: SfxPool_2:Melee Weapon Attack 3, SfxPool_6:Melee Weapon Attack 3, SfxPool_0:Melee Weapon Attack 4 |  |
| Audio | Player 'Death Axe' hitting the enemy plays a hit sound | PASS | 27/27 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Death Axe' on the enemy | PASS | enemy hp lost 142.6 (item dmg 18), hits 27, player right weapon Drawn_Death_Axe |  |
| Audio | Player 'Spear of Fate' swing plays a swish | PASS | 6 audio start(s) outside the hit windows over 4 swings: SfxPool_2:gravel5, SfxPool_3:QP01 0005 Wind howling, SfxPool_6:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Spear of Fate' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 16), hits 0, player right weapon Drawn_Spear_of_Fate | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Sword of Fear' swing plays a swish | PASS | 4 audio start(s) outside the hit windows over 4 swings: SfxPool_0:QP01 0005 Wind howling, SfxPool_3:QP01 0005 Wind howling, SfxPool_8:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Sword of Fear' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 15), hits 0, player right weapon Drawn_Sword_of_Fear | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'weap2_sword' swing plays a swish | PASS | 4 audio start(s) outside the hit windows over 4 swings: SfxPool_6:QP01 0005 Wind howling, SfxPool_7:QP01 0005 Wind howling, SfxPool_1:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'weap2_sword' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon Drawn_weap2_sword | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'weap_two_handed' swing plays a swish | PASS | 5 audio start(s) outside the hit windows over 4 swings: SfxPool_2:QP01 0005 Wind howling, SfxPool_4:gravel, SfxPool_7:gravel4 |  |
| Player weapons | Player uses melee 'weap_two_handed' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 28), hits 0, player right weapon Drawn_Two_Handed_Sword | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Wood Axe' swing plays a swish | PASS | 4 audio start(s) outside the hit windows over 4 swings: SfxPool_9:QP01 0005 Wind howling, SfxPool_2:QP01 0005 Wind howling, SfxPool_7:gravel |  |
| Player weapons | Player uses melee 'Wood Axe' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 26), hits 0, player right weapon Drawn_Wood_Axe | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Player weapons | Player uses 'DM_Mining_Tool' | INFO | mining tool: Plasma Fuel scanner/laser, not a damage weapon (damage 0 by design) |  |
| Player weapons | 'sci_fi_pistol' ammo loads | WARN | ammo is not in the inventory | default ammo could not be loaded through the Hot Cross path |
| Player weapons | Player uses ranged 'sci_fi_pistol' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 14), hits 0, player right weapon none | 4 shots at 6m did nothing: no ammo/empty magazine (needs reload), aim/fire input not received, or projectile damage mapped to 0 (item dmg 14) |
| Player weapons | 'survival_rifle' ammo loads | WARN | ammo is not in the inventory | default ammo could not be loaded through the Hot Cross path |
| Player weapons | Player uses ranged 'survival_rifle' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon none | 4 shots at 6m did nothing: no ammo/empty magazine (needs reload), aim/fire input not received, or projectile damage mapped to 0 (item dmg 18) |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Ammo | Ammo items and ranged weapons found | INFO | 10 ammo item(s): DMAmmoFxProfile_CarveExperiment(Gunpowder), DMAmmoFxProfile_Electricity(Electricity), DMAmmoFxProfile_Explosive(Explosive), DMAmmoFxProfile_Fire(Fire), DMAmmoFxProfile_Ice(Ice), DMAmmoFxProfile_Ion(Ion), DMAmmoFxProfile_Laser(Laser), DMAmmoFxProfile_Plasma(Plasma), DMAmmoFxProfile_Resonance Stabilizer(ResonanceStabilizer), DMAmmoFxProfile_Standard(Gunpowder); 2 ranged weapon(s): sci_fi_pistol, survival_rifle |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_CarveExperiment', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 14, ammo type Gunpowder, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Electricity', ammoType Electricity |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.25s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 14, ammo type Electricity, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Explosive', ammoType Explosive |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 1.00s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 14, ammo type Explosive, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Fire', ammoType Fire |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 14, ammo type Fire, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ice', ammoType Ice |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.40s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 14, ammo type Ice, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ion', ammoType Ion |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.50s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 14, ammo type Ion, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Laser', ammoType Laser |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.25s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 14, ammo type Laser, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: ammo type loads (Hot Cross path) | PASS | loaded 45/45 of 'DMAmmoFxProfile_Plasma', ammoType Plasma |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: magazine fills to capacity | PASS | magazine 45/45 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 45 -> 45, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 14, ammo type Plasma, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: empty + reload | INFO | magazine capacity 45 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Resonance Stabilizer', ammoType ResonanceStabilizer |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.25s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 14, ammo type ResonanceStabilizer, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 14, ammo type Gunpowder, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | X (AmmoCycle) switches the loaded ammo type | FAIL | 3 ammo stack(s) on the Hot Cross utility slots; loaded DMAmmoFxProfile_Standard -> DMAmmoFxProfile_Standard -> DMAmmoFxProfile_Standard | X tap did not cycle: AmmoCycle owned by GameplayKeyboardShortcuts poll (needs drawn ranged weapon + Hot Cross focus) or the next type does not fit the weapon |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | FAIL | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder, error: ammo is not in the inventory | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 18, ammo type Gunpowder, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: ammo type loads (Hot Cross path) | FAIL | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Electricity, error: ammo is not in the inventory | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.25s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 18, ammo type Electricity, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | FAIL | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Explosive, error: ammo is not in the inventory | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 1.00s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 18, ammo type Explosive, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Fire', ammoType Fire |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 18, ammo type Fire, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ice', ammoType Ice |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.40s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 18, ammo type Ice, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ion', ammoType Ion |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.50s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 18, ammo type Ion, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Laser', ammoType Laser |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.25s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 18, ammo type Laser, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: ammo type loads (Hot Cross path) | PASS | loaded 45/45 of 'DMAmmoFxProfile_Plasma', ammoType Plasma |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: magazine fills to capacity | PASS | magazine 45/45 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 45 -> 45, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 18, ammo type Plasma, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: empty + reload | INFO | magazine capacity 45 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Resonance Stabilizer', ammoType ResonanceStabilizer |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.25s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 18, ammo type ResonanceStabilizer, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: fires (rounds consumed) | FAIL | 3 trigger pulls, rounds 30 -> 30, shot interval 0.20s | no round consumed: aim (RMB) not held, weapon not drawn/ready, fire input not received, or PioneerInvectorAmmoBridge.TryProcessShotAmmo refused |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: damages the enemy | WARN | enemy hp lost 0.0 (weapon dmg 18, ammo type Gunpowder, ammo is DMAmmoFxProfile) | no shot fired, see fire row |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | Ammo matrix size | INFO | 20 weapon x ammo combination(s) tested (mining tool excluded: it runs on Plasma Fuel charge, not discrete ammo) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Performance per enemy (spawn cost, frame time, GC)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Performance | Axe_Droid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 26ms, 794 components, frame avg 31.7ms max 77.4ms (191 frames), GC collections 0, managed delta 6160KB |  |
| Performance | corrupt_patrol_android: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 26ms, 736 components, frame avg 31.6ms max 80.1ms (190 frames), GC collections 0, managed delta 6692KB |  |
| Performance | GreySpire Soldier: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 25ms, 797 components, frame avg 31.8ms max 76.6ms (189 frames), GC collections 0, managed delta 7344KB |  |
| Performance | Humadroid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 25ms, 787 components, frame avg 31.4ms max 73.5ms (191 frames), GC collections 0, managed delta 7288KB |  |
| Performance | Humanoid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 32ms, 696 components, frame avg 35.3ms max 79.6ms (171 frames), GC collections 0, managed delta 9288KB |  |
| Performance | HumanoidEnemy_Invector: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 31ms, 722 components, frame avg 33.8ms max 78.4ms (178 frames), GC collections 0, managed delta 9404KB |  |
| Performance | Hybrid Droid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 32ms, 696 components, frame avg 31.8ms max 77.2ms (191 frames), GC collections 0, managed delta 9324KB |  |
| Performance | Lisa_Hybrid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 31ms, 1162 components, frame avg 36.7ms max 83.3ms (164 frames), GC collections 0, managed delta 14540KB |  |
| Performance | Robot: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 31ms, 698 components, frame avg 33.8ms max 72.8ms (178 frames), GC collections 0, managed delta 11436KB |  |
| Performance | The_Evil_One: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 31ms, 707 components, frame avg 31.8ms max 77.7ms (189 frames), GC collections 0, managed delta 9808KB |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

## Per-enemy detail

### Axe_Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Axe_Droid.prefab` - definition `Axe_Droid` - melee `Spear of Fate` / ranged `-` - audited in 38s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Axe_Droid, melee=Spear of Fate (dmg 16), ranged=- (dmg -), preferRanged=False, def.attackDamage=12, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| AI | No circling while unaware (player 50m away, 9s) | PASS | yaw turned 145deg, path 7.0m, net 6.2m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.9s (at 2.2m, weapon Spear of Fate), first player damage 2.5s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 20.0 hp in 2.5s using Spear of Fate |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 36.0 (item dmg 16), attacks 1, hitbox-active frames 2 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.4s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 219.5 (-30.5); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 219.5 -> 144.8; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.4s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 9 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/corrupt_patrol_android.prefab` - definition `corrupt_patrol_android` - melee `Death Axe` / ranged `sci_fi_pistol` - audited in 43s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=corrupt_patrol_android, melee=Death Axe (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=350 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 169deg, path 9.7m, net 1.2m, states Wander |  |
| AI | Notice / chase / attack timing (player 5m in front) | PASS | notice 0.1s, chase 0.1s, first attack 0.6s (at 2.3m, weapon Death Axe), first player damage 1.2s, attacks 1, states Wander/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 24.0 hp in 1.2s using Death Axe |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 24.0 (item dmg 18), attacks 1, hitbox-active frames 6 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.4s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.1 (item dmg 14), shots/projectiles 2, attack-state frames 79, attacks 2, first hit 3.5s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Death_Axe] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 350.0 -> 322.0 (-28.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 322.0 -> 146.7; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 18 distinct clips seen in total |  |
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

### GreySpire Soldier

Prefab `Assets/_Project/Prefabs/Combat/Enemies/GreySpire Soldier.prefab` - definition `GreySpire Soldier` - melee `weap_two_handed` / ranged `-` - audited in 40s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=GreySpire Soldier, melee=weap_two_handed (dmg 28), ranged=- (dmg -), preferRanged=False, def.attackDamage=8, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| AI | No circling while unaware (player 55m away, 9s) | PASS | yaw turned 118deg, path 10.7m, net 5.6m, states Wander |  |
| AI | Notice / chase / attack timing (player 8m in front) | PASS | notice 0.0s, chase 0.0s, first attack 1.8s (at 2.2m, weapon weap_two_handed), first player damage 2.3s, attacks 1, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 38.0 hp in 2.3s using weap_two_handed |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 68.2 (item dmg 28), attacks 1, hitbox-active frames 2 (unarmed 0), hit events 1, sub-1hp chip 3.9, first hit 1.8s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 229.1 (-20.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 229.1 -> 149.5; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 10 distinct clips seen in total |  |
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

### Humadroid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 44s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 223deg, path 11.7m, net 6.4m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.9s (at 2.1m, weapon weap2_sword), first player damage 3.6s, attacks 1, states Patrol/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 3.6s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 3 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.1s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.0 (item dmg 14), shots/projectiles 1, attack-state frames 47, attacks 1, first hit 2.3s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 175.2 (-24.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 175.2 -> 48.6; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 21 distinct clips seen in total |  |
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

### Humanoid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humanoid.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 46s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 145deg, path 8.7m, net 8.7m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.8s (at 2.2m, weapon weap2_sword), first player damage 3.5s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 3.5s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 12 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.0s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 13.5 (item dmg 14), shots/projectiles 1, attack-state frames 49, attacks 1, first hit 2.2s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 275.7 (-24.3); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 275.7 -> 192.4; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### HumanoidEnemy_Invector

Prefab `Assets/_Project/Prefabs/Combat/Enemies/HumanoidEnemy_Invector.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 45s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 146deg, path 15.7m, net 8.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.9s (at 2.2m, weapon weap2_sword), first player damage 2.5s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.5s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.4 (item dmg 18), attacks 1, hitbox-active frames 6 (unarmed 0), hit events 1, sub-1hp chip 1.7, first hit 0.7s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.9 (item dmg 14), shots/projectiles 2, attack-state frames 88, attacks 2, first hit 3.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 198.1 (-101.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 198.1 -> 117.2; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.29s, loot bag 6.7s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### Hybrid Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Hybrid Droid.prefab` - definition `FRED_Android` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 44s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=FRED_Android, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 176deg, path 7.4m, net 1.5m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.8s (at 2.2m, weapon weap2_sword), first player damage 3.4s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 3.4s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.6 (item dmg 18), attacks 1, hitbox-active frames 5 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.3s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 15.1 (item dmg 14), shots/projectiles 1, attack-state frames 47, attacks 1, first hit 2.2s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 272.6 (-27.4); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 272.6 -> 182.8; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.7s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### Lisa_Hybrid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Lisa_Hybrid.prefab` - definition `Lisa_Hybrid` - melee `-` / ranged `survival_rifle` - audited in 38s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Lisa_Hybrid, melee=- (dmg -), ranged=survival_rifle (dmg 18), preferRanged=True, def.attackDamage=12, hp=400 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | FAIL | 4 renderer(s) with broken material | broken visuals: character_head:null material, eye_L:null material, eye_R:null material, Helmet_inside:null material (shader missing for the HDRP pipeline or material slot empty) |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 17 collider(s) on the hitbox layer, 91 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 176deg, path 11.8m, net 9.7m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.6s (at 9.6m, weapon survival_rifle), first player damage 1.7s, attacks 1, states Wander/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 17.9 hp in 1.7s using survival_rifle |  |
| Weapons | Melee weapon damages the player | SKIP | no resolved melee weapon |  |
| Weapons | Ranged 'survival_rifle' damages the player | PASS | player hp lost 17.4 (item dmg 18), shots/projectiles 1, attack-state frames 15, attacks 1, first hit 0.6s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 400.0 -> 373.7 (-26.3); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 373.7 -> 228.6; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.4s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 0 clip(s) observed playing; 10 distinct clips seen in total |  |
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

### Robot

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `Robot_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 47s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Robot_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=15, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 157deg, path 7.8m, net 3.2m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.4s (at 2.2m, weapon weap2_sword), first player damage 5.5s, attacks 2, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 5.5s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 11 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.6s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.1 (item dmg 14), shots/projectiles 1, attack-state frames 50, attacks 1, first hit 2.6s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 407.0 (-93.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 407.0 -> 318.4; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Robot |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/The_Evil_One.prefab` - definition `The_Evil_One` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 39s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=The_Evil_One, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=True, def.attackDamage=12, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 171deg, path 10.5m, net 8.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.6s (at 9.6m, weapon sci_fi_pistol), first player damage 1.6s, attacks 1, states Wander/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 14.9 hp in 1.6s using sci_fi_pistol |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 10 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.8s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; no vMeleeWeapon on the drawn/bound weapon |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.4 (item dmg 14), shots/projectiles 1, attack-state frames 20, attacks 1, first hit 1.0s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.65 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 475.3 (-24.7); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 475.3 -> 331.2; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone respawns, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 24 distinct clips seen in total |  |
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
