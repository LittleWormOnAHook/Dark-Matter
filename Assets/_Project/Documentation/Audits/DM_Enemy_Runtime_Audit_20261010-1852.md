# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-10 18:32:30 -> 2026-10-10 18:52:13 (complete)
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
- WEAPON REVERTS: the drawn weapon changed on its own 80 time(s) during the run (the harness re-armed it): wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 59.9s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.3s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.9s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.3s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 24.2s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming || wanted 'weap2_sword', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming || wanted '2 Hander', found 'sci_fi_pistol' (drawn True, active slot 1) 0.2s after the last teleport, 1.2s after arming || wanted '2 Hander', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.3s after arming || wanted '2 Hander', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming || wanted '2 Hander', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming || wanted 'Blade Of Chaos', found 'sci_fi_pistol' (drawn True, active slot 1) 0.2s after the last teleport, 1.2s after arming || wanted 'Blade Of Chaos', found 'sci_fi_pistol' (drawn True, active slot 1) 0.1s after the last teleport, 1.4s after arming

**10 enemies audited - 1 FAIL, 0 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 12 weapon cases + 13 scenarios): 318 PASS, 52 FAIL, 11 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 17:30: 207 change(s); baseline updated.

- CHANGED enemy|GreySpire Soldier|notice: WARN -> PASS
- CHANGED enemy|Humanoid|player.swing: WARN -> PASS
- CHANGED enemy|The_Evil_One|melee.damage: FAIL -> PASS
- NEW weapon|2 Hander|audio.swing.2 Hander = FAIL
- NEW weapon|2 Hander|audio.hitplayer.2 Hander = FAIL
- CHANGED weapon|2 Hander|w.damage: PASS -> FAIL
- NEW weapon|Blade Of Chaos|audio.swing.Blade Of Chaos = FAIL
- NEW weapon|Blade Of Chaos|audio.hitplayer.Blade Of Chaos = FAIL
- CHANGED weapon|Blade Of Chaos|w.damage: PASS -> FAIL
- NEW weapon|Death Axe|audio.swing.Death Axe = FAIL
- NEW weapon|Death Axe|audio.hitplayer.Death Axe = PASS
- NEW weapon|Spear of Fate|audio.swing.Spear of Fate = PASS
- NEW weapon|Spear of Fate|audio.hitplayer.Spear of Fate = FAIL
- NEW weapon|Sword of Fear|audio.swing.Sword of Fear = FAIL
- NEW weapon|Sword of Fear|audio.hitplayer.Sword of Fear = PASS
- NEW weapon|weap2_sword|audio.swing.weap2_sword = FAIL
- NEW weapon|weap2_sword|audio.hitplayer.weap2_sword = FAIL
- NEW weapon|weap_two_handed|audio.swing.weap_two_handed = FAIL
- NEW weapon|weap_two_handed|audio.hitplayer.weap_two_handed = FAIL
- NEW weapon|Wood Axe|audio.swing.Wood Axe = FAIL
- NEW weapon|Wood Axe|audio.hitplayer.Wood Axe = FAIL
- NEW scenario|Camera: faces forward after teleports, zoom in/out|cam.forward = PASS
- NEW scenario|Camera: faces forward after teleports, zoom in/out|cam.zoom = WARN
- NEW scenario|Camera: faces forward after teleports, zoom in/out|console = PASS
- NEW scenario|Equip: weapon survives a teleport|equip.teleport = PASS
- NEW scenario|Equip: weapon survives a teleport|console = PASS
- NEW scenario|Audio: swing / hit sound wiring (static)|audio.profile = PASS
- NEW scenario|Audio: swing / hit sound wiring (static)|audio.swing.callers = FAIL
- NEW scenario|Audio: swing / hit sound wiring (static)|audio.hit.callers = WARN
- NEW scenario|Audio: swing / hit sound wiring (static)|console = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.W = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.S = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.A = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.D = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.run = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.sprint = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.jump = FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.crouch = FAIL
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.dodge = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.dash = WARN
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.jetpack = WARN
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.look.x = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.look.y = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.lmb = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.rmb = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.mmb = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.mmb.note = INFO
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.switch = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.walk = PASS
- NEW scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|console = PASS
- CHANGED scenario|Block + parry: player defends against enemy melee|blk.hold: FAIL -> PASS
- CHANGED scenario|Block: enemy defends against player swings|eblk: FAIL -> WARN
- CHANGED scenario|Movement: retreat, push, flank, backstab, lose target|mv.backstab: INFO -> WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.approach = WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.kite = PASS
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.strafe = PASS
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.reengage = PASS
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.jump = PASS
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.jetpack = WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.lose = PASS
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.return = WARN
- NEW scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|console = PASS
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.2 Hander = PASS
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.Blade Of Chaos = PASS
- CHANGED scenario|Player weapons: each melee/ranged item vs enemy|pw.Blade Of Chaos: PASS -> FAIL
- NEW scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.Death Axe = PASS
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
- NEW scenario|Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload|ammo.inventory = INFO
- NEW scenario|Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload|ammo.sci_fi_pistol.DMAmmoFxProfile_CarveExperiment.load = PASS
- NEW scenario|Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload|ammo.sci_fi_pistol.DMAmmoFxProfile_CarveExperiment.fill = PASS
- NEW scenario|Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload|ammo.sci_fi_pistol.DMAmmoFxProfile_CarveExperiment.fire = PASS
- ... 127 more

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 100 | FAIL | Melee '2 Hander' damages the player | 2 Hander, Blade Of Chaos | only 1.1 hp lost with 0 weapon hit events (passive survival chip, not the weapon) / attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon | Check the weapon's hit box: collider layer/tag against the player's hit tags, animation events that open the hitbox (ActiveDamage/OnEnableAttack), and that the weapon's vMeleeWeapon damage and the item damage are > 0. |
| 2 | 100 | FAIL | Player uses melee '2 Hander' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 3 | 100 | FAIL | Player uses melee 'Blade Of Chaos' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 4 | 100 | FAIL | Player uses melee 'Death Axe' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 5 | 100 | FAIL | Player uses melee 'Spear of Fate' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 6 | 100 | FAIL | Player uses melee 'Sword of Fear' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 7 | 100 | FAIL | Player uses melee 'weap2_sword' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 8 | 100 | FAIL | Player uses melee 'weap_two_handed' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 9 | 100 | FAIL | Player uses melee 'Wood Axe' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 10 | 100 | FAIL | Player uses ranged 'sci_fi_pistol' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 shots at 6m did nothing: no ammo/empty magazine (needs reload), aim/fire input not received, or projectile damage mapped to 0 (item dmg 14) | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 11 | 100 | FAIL | Player uses ranged 'survival_rifle' on the enemy | Player weapons: each melee/ranged item vs enemy | 4 shots at 6m did nothing: no ammo/empty magazine (needs reload), aim/fire input not received, or projectile damage mapped to 0 (item dmg 18) | Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0. |
| 12 | 75 | FAIL | No error-pink / null materials | Lisa_Hybrid | broken visuals: character_head:null material, eye_L:null material, eye_R:null material, Helmet_inside:null material (shader missing for the HDRP pipeline or material slot empty) | Repair the prefab (re-add the script / fix the material / assign controller+avatar) via the Character Creator Fix or by hand. |
| 13 | 75 | WARN | Ranged 'DM_Mining_Tool' damages the player | DM_Mining_Tool | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 14 | 45 | WARN | Enemy block reduces player melee damage | Block: enemy defends against player swings | player swings registered no hits on the open enemy, so the comparison is inconclusive (input focus, reach, or the player weapon) | Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate. |
| 15 | 20 | FAIL | '2 Hander' swing plays a swish | 2 Hander | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 16 | 20 | FAIL | '2 Hander' hitting the player plays a hit sound | 2 Hander | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 17 | 20 | FAIL | 'Blade Of Chaos' swing plays a swish | Blade Of Chaos | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 18 | 20 | FAIL | 'Blade Of Chaos' hitting the player plays a hit sound | Blade Of Chaos | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 19 | 20 | FAIL | 'Death Axe' swing plays a swish | Death Axe | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 20 | 20 | FAIL | 'Spear of Fate' hitting the player plays a hit sound | Spear of Fate | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 21 | 20 | FAIL | 'Sword of Fear' swing plays a swish | Sword of Fear | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 22 | 20 | FAIL | 'weap2_sword' swing plays a swish | weap2_sword | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 23 | 20 | FAIL | 'weap2_sword' hitting the player plays a hit sound | weap2_sword | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 24 | 20 | FAIL | 'weap_two_handed' swing plays a swish | weap_two_handed | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 25 | 20 | FAIL | 'weap_two_handed' hitting the player plays a hit sound | weap_two_handed | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 26 | 20 | FAIL | 'Wood Axe' swing plays a swish | Wood Axe | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 27 | 20 | FAIL | 'Wood Axe' hitting the player plays a hit sound | Wood Axe | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 28 | 20 | FAIL | Swing swish (GameAudioManager.PlayWeaponSwing) is called by gameplay code | Audio: swing / hit sound wiring (static) | PlayWeaponSwing / PlayPunchSwing are never called, so NO weapon (player or enemy) ever plays a swish: add a call where a melee attack window opens (vMeleeManager.SetActiveAttack active=true or the animation event OnEnableAttack) | Inspect the measured value and cause on the detail row. |
| 29 | 20 | FAIL | Space jumps | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked | Inspect the measured value and cause on the detail row. |
| 30 | 20 | FAIL | Left Ctrl crouches | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Crouch action not wired or blocked (ammo popup open, building mode) | Inspect the measured value and cause on the detail row. |
| 31 | 20 | WARN | Player circles behind the enemy: enemy turns to face | Movement: retreat, push, flank, backstab, lose target | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 32 | 20 | WARN | Hit from behind vs from the front (player melee, AI off) | Movement: retreat, push, flank, backstab, lose target | swings did not land from one side (reach/facing/input), comparison inconclusive | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 33 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 34 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Electricity: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 35 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Explosive: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 36 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Fire: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 37 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Ice: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 38 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Ion: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 39 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Laser: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 40 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Plasma: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 41 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 42 | 20 | FAIL | sci_fi_pistol + DMAmmoFxProfile_Standard: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 43 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory | Inspect the measured value and cause on the detail row. |
| 44 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_CarveExperiment: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 45 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Electricity: ammo type loads (Hot Cross path) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory | Inspect the measured value and cause on the detail row. |
| 46 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Electricity: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 47 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory | Inspect the measured value and cause on the detail row. |
| 48 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Explosive: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 49 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Fire: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 50 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Ice: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 51 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Ion: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 52 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Laser: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 53 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Plasma: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 54 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 55 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Standard: damages the enemy | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox | Inspect the measured value and cause on the detail row. |
| 56 | -5 | WARN | Camera zooms in and out | Camera: faces forward after teleports, zoom in/out | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state | Inspect the measured value and cause on the detail row. |
| 57 | -5 | WARN | Melee hit sound has a caller on the damage path | Audio: swing / hit sound wiring (static) | the only callers are the enemy-body receiver (PioneerInvectorDamageReceiver), MeleeCombatController and CombatHitResolver: damage TO THE PLAYER (PioneerInvectorDamageBridge / the player's vDamageReceiver) has no hit-sound call, so enemy weapons hitting the player are silent | Inspect the measured value and cause on the detail row. |
| 58 | -5 | WARN | Left Alt dash | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dash action not wired or on cooldown (DMDashController) | Inspect the measured value and cause on the detail row. |
| 59 | -5 | WARN | Hold Space after jumping boosts (jetpack) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | no sustained lift: jetpack not unlocked/fuelled, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player | Inspect the measured value and cause on the detail row. |
| 60 | -5 | WARN | Approach with W: enemy notices | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) | Inspect the measured value and cause on the detail row. |

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

- **Weapon matrix: 2 Hander** (melee): player hp lost 1.1 (item dmg 28), attacks 1, hitbox-active frames 0, hit events 0, sub-1hp chip 1.2, first hit 0.3s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** only 1.1 hp lost with 0 weapon hit events (passive survival chip, not the weapon) | attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon
- **Weapon matrix: Blade Of Chaos** (melee): player hp lost 1.1 (item dmg 21), attacks 1, hitbox-active frames 0, hit events 0, sub-1hp chip 1.3, first hit 0.3s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, .... **Probable cause:** only 1.1 hp lost with 0 weapon hit events (passive survival chip, not the weapon) | attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon

## Weapon matrix (every weapon item on the template, spawn override)

12 cases, 15 FAIL. Each weapon is equipped on `HumanoidEnemy_Invector` through ConfigureSpawnLoadout (the spawner path) and made to attack the player.

| Weapon | Kind | Equips | Damages player | One drawn | Measured | Probable cause |
|---|---|---|---|---|---|---|
| 2 Hander | melee | PASS | **FAIL** | PASS | player hp lost 1.1 (item dmg 28), attacks 1, hitbox-active frames 0, hit events 0, sub-1hp chip 1.2, first hit 0.3s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ig... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event / weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it / only 1.1 hp lost with 0 weapon hit events (passive survival chip, not the weapon) / attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon |
| Blade Of Chaos | melee | PASS | **FAIL** | PASS | player hp lost 1.1 (item dmg 21), attacks 1, hitbox-active frames 0, hit events 0, sub-1hp chip 1.3, first hit 0.3s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitB... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event / weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it / only 1.1 hp lost with 0 weapon hit events (passive survival chip, not the weapon) / attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon |
| Death Axe | melee | PASS | PASS | PASS | player hp lost 24.2 (item dmg 18), attacks 1, hitbox-active frames 7, hit events 1, sub-1hp chip 1.1, first hit 0.1s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Spear of Fate | melee | PASS | PASS | PASS | player hp lost 20.3 (item dmg 16), attacks 2, hitbox-active frames 10, hit events 1, sub-1hp chip 6.1, first hit 2.3s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitB... | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Sword of Fear | melee | PASS | PASS | PASS | player hp lost 19.0 (item dmg 15), attacks 2, hitbox-active frames 20, hit events 1, sub-1hp chip 6.6, first hit 2.7s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitB... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| weap2_sword | melee | PASS | PASS | PASS | player hp lost 27.9 (item dmg 18), attacks 1, hitbox-active frames 8, hit events 1, sub-1hp chip 0.0, first hit 0.1s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: l... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event / weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| weap_two_handed | melee | PASS | PASS | PASS | player hp lost 68.1 (item dmg 28), attacks 2, hitbox-active frames 12, hit events 1, sub-1hp chip 7.2, first hit 2.9s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38;... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event / weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Wood Axe | melee | PASS | PASS | PASS | player hp lost 62.6 (item dmg 26), attacks 2, hitbox-active frames 17, hit events 2, sub-1hp chip 5.5, first hit 2.3s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event / weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| DM_Mining_Tool | ranged | PASS | warn | PASS | player hp lost 14.9 (item dmg 0), shots/projectiles 1, attack-state frames 22, attacks 1, first hit 0.9s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCoo... |  |
| sci_fi_pistol | ranged | PASS | PASS | PASS | player hp lost 17.9 (item dmg 14), shots/projectiles 3, attack-state frames 90, attacks 3, first hit 3.8s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRang... |  |
| survival_rifle | ranged | PASS | PASS | PASS | player hp lost 17.9 (item dmg 18), shots/projectiles 1, attack-state frames 19, attacks 1, first hit 0.7s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRa... |  |
| OVERRIDE both slots | dual-slot spawn override | - | - | PASS | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)): PASS ; Starts with the ranged override drawn (prefer-ranged on, start-melee off): PASS ; Switch to the melee override vi... |  |

### Weapon: 2 Hander

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item 2 Hander; drawn [Drawn_2_Hander]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | '2 Hander' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | '2 Hander' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_2_Hander': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee '2 Hander' damages the player | FAIL | player hp lost 1.1 (item dmg 28), attacks 1, hitbox-active frames 0, hit events 0, sub-1hp chip 1.2, first hit 0.3s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 | only 1.1 hp lost with 0 weapon hit events (passive survival chip, not the weapon) / attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 23, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Blade Of Chaos

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 21 |  |
| Audio | 'Blade Of Chaos' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Blade Of Chaos' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_Blade_Of_Chaos': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee 'Blade Of Chaos' damages the player | FAIL | player hp lost 1.1 (item dmg 21), attacks 1, hitbox-active frames 0, hit events 0, sub-1hp chip 1.3, first hit 0.3s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 | only 1.1 hp lost with 0 weapon hit events (passive survival chip, not the weapon) / attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.4m), first damage 0.3s, attacks 1, attack-state frames 25, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Death Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'Death Axe' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Death Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_5:QP01 0005 Wind howling; vHitEffects on 'Drawn_Death_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 24.2 (item dmg 18), attacks 1, hitbox-active frames 7, hit events 1, sub-1hp chip 1.1, first hit 0.1s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.1m), first damage 0.1s, attacks 1, attack-state frames 17, clips seen: WeakAttack_SwordA, WeakAttack_SwordB, WeakAttack_SwordC |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Spear of Fate

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 16 |  |
| Audio | 'Spear of Fate' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_4:QP01 0005 Wind howling |  |
| Audio | 'Spear of Fate' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_Spear_of_Fate': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 20.3 (item dmg 16), attacks 2, hitbox-active frames 10, hit events 1, sub-1hp chip 6.1, first hit 2.3s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.3m), first damage 2.3s, attacks 2, attack-state frames 79, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Sword of Fear

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Sword of Fear; drawn [Drawn_Sword_of_Fear]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 15 |  |
| Audio | 'Sword of Fear' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Sword of Fear' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_6:QP01 0005 Wind howling; vHitEffects on 'Drawn_Sword_of_Fear': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Sword of Fear' damages the player | PASS | player hp lost 19.0 (item dmg 15), attacks 2, hitbox-active frames 20, hit events 1, sub-1hp chip 6.6, first hit 2.7s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.4m), first damage 2.7s, attacks 2, attack-state frames 89, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap2_sword

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'weap2_sword' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'weap2_sword' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_weap2_sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 27.9 (item dmg 18), attacks 1, hitbox-active frames 8, hit events 1, sub-1hp chip 0.0, first hit 0.1s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.1m), first damage 0.1s, attacks 1, attack-state frames 20, clips seen: WeakAttack_SwordA, WeakAttack_SwordB, WeakAttack_SwordC |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap_two_handed

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | 'weap_two_handed' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'weap_two_handed' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_Two_Handed_Sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 68.1 (item dmg 28), attacks 2, hitbox-active frames 12, hit events 1, sub-1hp chip 7.2, first hit 2.9s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 2.9s, attacks 2, attack-state frames 93, clips seen: WeakAttack_SwordA, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Wood Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 26 |  |
| Audio | 'Wood Axe' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Wood Axe' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_Wood_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee 'Wood Axe' damages the player | PASS | player hp lost 62.6 (item dmg 26), attacks 2, hitbox-active frames 17, hit events 2, sub-1hp chip 5.5, first hit 2.3s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.1m), first damage 2.3s, attacks 2, attack-state frames 75, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: DM_Mining_Tool

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item DM_Mining_Tool; drawn []; holstered []; item dmg 0 | ItemData rangedDamage = 0 |
| Weapons | Ranged 'DM_Mining_Tool' damages the player | WARN | player hp lost 14.9 (item dmg 0), shots/projectiles 1, attack-state frames 22, attacks 1, first hit 0.9s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item |
| Timing | Attack timing | INFO | first attack 0.8s (at 7.2m), first damage 0.9s, attacks 1, attack-state frames 22, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: sci_fi_pistol

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; item dmg 14 |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.9 (item dmg 14), shots/projectiles 3, attack-state frames 90, attacks 3, first hit 3.8s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Timing | Attack timing | INFO | first attack 0.9s (at 7.2m), first damage 3.8s, attacks 3, attack-state frames 90, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: survival_rifle

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_weap2_sword]; item dmg 18 |  |
| Weapons | Ranged 'survival_rifle' damages the player | PASS | player hp lost 17.9 (item dmg 18), shots/projectiles 1, attack-state frames 19, attacks 1, first hit 0.7s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Timing | Attack timing | INFO | first attack 0.7s (at 7.1m), first damage 0.7s, attacks 1, attack-state frames 19, clips seen: none matched |  |
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
| Equip: weapon survives a teleport | 2 | 0 | 0 | 7s |
| Audio: swing / hit sound wiring (static) | 2 | 1 | 1 | 0s |
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | 15 | 2 | 2 | 56s |
| Block + parry: player defends against enemy melee | 3 | 0 | 0 | 49s |
| Block: enemy defends against player swings | 1 | 0 | 1 | 16s |
| Movement: retreat, push, flank, backstab, lose target | 4 | 0 | 2 | 36s |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | 6 | 0 | 3 | 82s |
| Multi-enemy: 3 at once | 2 | 0 | 0 | 23s |
| Player weapons: each melee/ranged item vs enemy | 9 | 10 | 0 | 93s |
| Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | 59 | 23 | 0 | 164s |
| Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits | 6 | 0 | 0 | 34s |
| Performance per enemy (spawn cost, frame time, GC) | 11 | 0 | 0 | 73s |

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
| Audio | Swing swish (GameAudioManager.PlayWeaponSwing) is called by gameplay code | FAIL | callers outside GameAudioManager: NONE | PlayWeaponSwing / PlayPunchSwing are never called, so NO weapon (player or enemy) ever plays a swish: add a call where a melee attack window opens (vMeleeManager.SetActiveAttack active=true or the animation event OnEnableAttack) |
| Audio | Melee hit sound has a caller on the damage path | WARN | CombatHitAudio.PlayWeaponHit callers: PioneerInvectorDamageReceiver.cs, MeleeCombatController.cs | the only callers are the enemy-body receiver (PioneerInvectorDamageReceiver), MeleeCombatController and CombatHitResolver: damage TO THE PLAYER (PioneerInvectorDamageBridge / the player's vDamageReceiver) has no hit-sound call, so enemy weapons hitting the player are silent |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Controls | W moves the player forward (camera direction) | PASS | start 0.00, min 0.00, max 1.95, end 2.01 m along forward |  |
| Controls | S moves the player back | PASS | start 0.00, min -1.94, max 0.00, end -2.08 m along forward |  |
| Controls | A strafes left | PASS | start 0.00, min 0.00, max 1.90, end 1.97 m along left |  |
| Controls | D strafes right | PASS | start 0.00, min 0.00, max 1.90, end 1.97 m along right |  |
| Controls | W run speed (reference for sprint) | PASS | start 0.00, min 0.00, max 2.61, end 2.64 m in 1.5s |  |
| Controls | Shift + W sprints faster than W | PASS | start 0.00, min 0.00, max 4.98, end 5.18 m in 1.5s |  |
| Controls | Space jumps | FAIL | start 21.23, min 21.23, max 21.38, end 21.44 m height | Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked |
| Controls | Left Ctrl crouches | FAIL | start 0.00, min 0.00, max 0.00, end 0.00 isCrouching | Crouch action not wired or blocked (ammo popup open, building mode) |
| Controls | Q dodge / roll | PASS | start 0.00, min 0.00, max 14.18, end 14.25 m or isRolling |  |
| Controls | Left Alt dash | WARN | start 0.00, min 0.00, max 1.16, end 1.22 m | Dash action not wired or on cooldown (DMDashController) |
| Controls | Hold Space after jumping boosts (jetpack) | WARN | start 21.23, min 21.23, max 21.85, end 21.23 m height | no sustained lift: jetpack not unlocked/fuelled, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player |
| Controls | Mouse look left/right orbits the camera | PASS | start 0.00, min 0.00, max 93.80, end 99.98 deg yaw |  |
| Controls | Mouse look up/down tilts the camera | PASS | start 18.00, min 0.00, max 49.87, end 49.92 deg pitch |  |
| Controls | Left click swings the drawn melee weapon | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isAttacking |  |
| Controls | Right click blocks (melee drawn) | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isBlocking |  |
| Controls | Middle click does not break gameplay | PASS | start 0.00, min 0.00, max 0.00, end 0.00 none |  |
| Controls | Middle click has no gameplay binding | INFO | InputSystem_Actions Player map binds no action to Mouse/middleButton (only the UI map MiddleClick) |  |
| Controls | Tab (SwitchWeapon) cycles the weapon focus | PASS | Hot Cross weapon focus 0 -> 1, drawn weap2_sword -> sci_fi_pistol |  |
| Controls | Half-tilt stick walks slower than a run | PASS | start 0.00, min 0.00, max 0.84, end 0.86 m in 1.5s |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block + parry: player defends against enemy melee

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Unblocked baseline (same enemy, same distance) | INFO | hits 1, player hp lost 26.7 |  |
| Block | Holding block negates enemy melee damage | PASS | isBlocking True (raised in 0.04s), hits 2, blocked 2, player hp lost 0.0 (unblocked 26.7), stamina 1000.0 |  |
| Block | Enemy guard-break stagger after a blocked hit | INFO | longest enemy stagger 1.18s (profile blockStaggerSeconds 1.2s) |  |
| Parry | Parry timing windows (block pressed relative to the enemy swing) | INFO | press +0.00s after swing start: hit 3.31s after press -> HIT (block came too late), stagger 0.00s, hp lost 78.3 // press +0.15s after swing start: hit 0.90s after press -> HIT (block came too late), stagger 0.00s, hp lost 47.1 // press +0.30s after swing start: hit 0.18s after press -> PARRY, stagger 3.26s, hp lost 0.0 // press +0.45s after swing start: hit 0.07s after press -> PARRY, stagger 3.39s, hp lost 0.0 // press +0.60s after swing start: hit 0.27s after press -> BLOCK, stagger 1.17s, hp lost 26.6 | profile: parryWindowSeconds 0.2, blockStagger 1.2s, parryStagger 5.0s |
| Parry | A parry happens when block is pressed inside the window | PASS | parry observed inside the window |  |
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
| Movement | Player retreats 9m at 3 m/s: enemy follows | PASS | distance 3.4m -> 5.1m (min 2.2m), states Chase/Attack, attacks 1 |  |
| Movement | Player pushes into the enemy: spacing and facing | PASS | min distance 0.50m, max facing error 0deg, attack-state frames 55, states Attack/Chase |  |
| Movement | Player circles behind the enemy: enemy turns to face | WARN | reached behind at never, enemy faced player (<30deg) after never, max facing error 0deg, attacks while behind 0, player hp lost -25.6, states Attack | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player |
| Movement | Hit from behind vs from the front (player melee, AI off) | WARN | front: 0 hits, 0.0 hp; behind: 0 hits, 0.0 hp; ratio n/a | swings did not land from one side (reach/facing/input), comparison inconclusive |
| Movement | Player disappears 70m away: enemy loses the target | PASS | was aware True, investigate/search after never, back to calm state after 6.0s, states Chase/ReturnHome |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Play | Approach with W: enemy notices | WARN | walked 21.0m in 14.0s to 9.0m; enemy noticed after never at n/a; state Wander | enemy never left a calm state while the player walked up to 9m in front of it (vision range/FOV/LOS or notice delay) |
| Play | Run away with S: enemy pursues | PASS | player backed away for 4.0s (gap now 9.5m), enemy moved 6.9m, hostile state seen True, states Wander/Hold |  |
| Play | Strafe around the enemy: it turns to face the player | PASS | enemy facing error avg 4deg, max 34deg over 95 frames while strafing D then A, state Attack |  |
| Play | Retreat then re-engage: enemy fights back | PASS | re-engaged to 1.2m, enemy attacks 2, player hp lost 73.3, state Attack |  |
| Play | Jump away: player clears distance | PASS | jump height 0.91m, gap 1.2m -> 4.3m, enemy state Attack | player could not gain distance (enemy kept pace or player blocked) |
| Play | Jetpack escape (hold Space + S) | WARN | peak height gain 0.9m, gap 4.3m -> 2.1m, enemy state Attack | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump |
| Play | Sprint away: enemy loses the target | PASS | lost after 12.9s at 6.7m (sprinting), state Chase, states Wander/Hold/Chase/Attack/ReturnHome |  |
| Play | Enemy returns to its post after losing the player | WARN | back home after never, now 64.4m from spawn, state Attack | enemy did not walk back to its spawn within 30s: ReturnHome blocked, leash undefined, or it stays in Search/Investigate |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Multi-enemy: 3 at once

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Multi-enemy | 3 enemies at once for 22s | PASS | max simultaneous attackers 2, distinct attackers 3/3, attack-state frames [160,281,374], chase/hold frames [331,211,119], closest pair 1.84m, closest to player 1.16m, player hp lost 25.5 (topped up every 2s), 494 frames |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player weapons: each melee/ranged item vs enemy

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Audio | Player '2 Hander' swing plays a swish | PASS | 3 audio start(s) outside the hit windows over 4 swings: SfxPool_7:QP01 0005 Wind howling, SfxPool_0:QP01 0005 Wind howling, SfxPool_3:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee '2 Hander' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 28), hits 0, player right weapon Drawn_2_Hander | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Blade Of Chaos' swing plays a swish | PASS | 4 audio start(s) outside the hit windows over 4 swings: SfxPool_2:QP01 0005 Wind howling, SfxPool_5:QP01 0005 Wind howling, SfxPool_8:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Blade Of Chaos' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 21), hits 0, player right weapon InvectorWeapon_Blade Of Chaos | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Death Axe' swing plays a swish | PASS | 1 audio start(s) outside the hit windows over 4 swings: SfxPool_7:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Death Axe' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon Drawn_Death_Axe | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Spear of Fate' swing plays a swish | PASS | 3 audio start(s) outside the hit windows over 4 swings: SfxPool_0:QP01 0005 Wind howling, SfxPool_6:QP01 0005 Wind howling, SfxPool_9:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Spear of Fate' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 16), hits 0, player right weapon Drawn_Spear_of_Fate | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Sword of Fear' swing plays a swish | PASS | 1 audio start(s) outside the hit windows over 4 swings: SfxPool_8:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Sword of Fear' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 15), hits 0, player right weapon Drawn_Sword_of_Fear | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'weap2_sword' swing plays a swish | PASS | 4 audio start(s) outside the hit windows over 4 swings: SfxPool_7:QP01 0005 Wind howling, SfxPool_0:QP01 0005 Wind howling, SfxPool_3:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'weap2_sword' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon Drawn_weap2_sword | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'weap_two_handed' swing plays a swish | PASS | 3 audio start(s) outside the hit windows over 4 swings: SfxPool_5:QP01 0005 Wind howling, SfxPool_1:QP01 0005 Wind howling, SfxPool_3:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'weap_two_handed' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 28), hits 0, player right weapon Drawn_Two_Handed_Sword | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Audio | Player 'Wood Axe' swing plays a swish | PASS | 2 audio start(s) outside the hit windows over 4 swings: SfxPool_6:QP01 0005 Wind howling, SfxPool_3:QP01 0005 Wind howling |  |
| Player weapons | Player uses melee 'Wood Axe' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 26), hits 0, player right weapon Drawn_Wood_Axe | 4 swings, 0 weapon hit event(s) on the enemy: weapon hitbox never connected (reach/angle/hitbox layer, animation event) or swing input not received |
| Player weapons | Player uses 'DM_Mining_Tool' | INFO | mining tool: Plasma Fuel scanner/laser, not a damage weapon (damage 0 by design) |  |
| Player weapons | Player uses ranged 'sci_fi_pistol' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 14), hits 0, player right weapon none | 4 shots at 6m did nothing: no ammo/empty magazine (needs reload), aim/fire input not received, or projectile damage mapped to 0 (item dmg 14) |
| Player weapons | Player uses ranged 'survival_rifle' on the enemy | FAIL | enemy hp lost 0.0 (item dmg 18), hits 0, player right weapon none | 4 shots at 6m did nothing: no ammo/empty magazine (needs reload), aim/fire input not received, or projectile damage mapped to 0 (item dmg 18) |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Ammo | Ammo items and ranged weapons found | INFO | 10 ammo item(s): DMAmmoFxProfile_CarveExperiment(Gunpowder), DMAmmoFxProfile_Electricity(Electricity), DMAmmoFxProfile_Explosive(Explosive), DMAmmoFxProfile_Fire(Fire), DMAmmoFxProfile_Ice(Ice), DMAmmoFxProfile_Ion(Ion), DMAmmoFxProfile_Laser(Laser), DMAmmoFxProfile_Plasma(Plasma), DMAmmoFxProfile_Resonance Stabilizer(ResonanceStabilizer), DMAmmoFxProfile_Standard(Gunpowder); 2 ranged weapon(s): sci_fi_pistol, survival_rifle |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_CarveExperiment', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.20s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 14, ammo type Gunpowder, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Electricity', ammoType Electricity |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 14, ammo type Electricity, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Explosive', ammoType Explosive |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 1.00s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 14, ammo type Explosive, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Fire', ammoType Fire |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.20s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 14, ammo type Fire, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ice', ammoType Ice |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.40s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 14, ammo type Ice, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ion', ammoType Ion |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.50s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 14, ammo type Ion, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Laser', ammoType Laser |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 14, ammo type Laser, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: ammo type loads (Hot Cross path) | PASS | loaded 45/45 of 'DMAmmoFxProfile_Plasma', ammoType Plasma |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: magazine fills to capacity | PASS | magazine 45/45 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 45 -> 42, shot interval 0.20s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 14, ammo type Plasma, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: empty + reload | INFO | magazine capacity 45 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Resonance Stabilizer', ammoType ResonanceStabilizer |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 14, ammo type ResonanceStabilizer, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.20s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 14, ammo type Gunpowder, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | X (AmmoCycle) switches the loaded ammo type | PASS | 3 ammo stack(s) on the Hot Cross utility slots; loaded DMAmmoFxProfile_Standard -> DMAmmoFxProfile_CarveExperiment -> DMAmmoFxProfile_Electricity |  |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | FAIL | loaded 24/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder, error: ammo is not in the inventory | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.20s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 18, ammo type Gunpowder, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: ammo type loads (Hot Cross path) | FAIL | loaded 27/30 of 'DMAmmoFxProfile_CarveExperiment', ammoType Electricity, error: ammo is not in the inventory | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 18, ammo type Electricity, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | FAIL | loaded 27/30 of 'DMAmmoFxProfile_CarveExperiment', ammoType Explosive, error: ammo is not in the inventory | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 1.00s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 18, ammo type Explosive, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Fire', ammoType Fire |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.20s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 18, ammo type Fire, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ice', ammoType Ice |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.40s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 18, ammo type Ice, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ion', ammoType Ion |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.50s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 18, ammo type Ion, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Laser', ammoType Laser |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 18, ammo type Laser, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: ammo type loads (Hot Cross path) | PASS | loaded 45/45 of 'DMAmmoFxProfile_Plasma', ammoType Plasma |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: magazine fills to capacity | PASS | magazine 45/45 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 45 -> 41, shot interval 0.20s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 18, ammo type Plasma, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: empty + reload | INFO | magazine capacity 45 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Resonance Stabilizer', ammoType ResonanceStabilizer |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 18, ammo type ResonanceStabilizer, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 28, shot interval 0.20s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: damages the enemy | FAIL | enemy hp lost 0.0 (weapon dmg 18, ammo type Gunpowder, ammo is DMAmmoFxProfile) | rounds were spent but the enemy took no damage: projectile missed (aim), projectile damage mapped to 0, or the hit did not resolve on the enemy hitbox |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: hit marks / FX | INFO | active hit marks 0 -> 0, body type Humanoid | no hit to mark |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | Ammo matrix size | INFO | 20 weapon x ammo combination(s) tested (mining tool excluded: it runs on Plasma Fuel charge, not discrete ammo) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Aim | Aim follows the moving enemy: chest | PASS | 3 shots, 3 fired, aim error before firing avg 0.9deg; crosshair hitbox zone Torso (bone VBOT_:Spine2); hit-mark bones VBOT_:Spine2, VBOT_:LeftForeArm; avg damage 12.3 (x1.00 chest) |  |
| Aim | Aim follows the moving enemy: head | PASS | 3 shots, 3 fired, aim error before firing avg 0.6deg; crosshair hitbox zone Arm/Head (bone VBOT_:RightArm/VBOT_:Head); hit-mark bones VBOT_:Head; avg damage 18.8 (x1.52 chest) |  |
| Aim | Aim follows the moving enemy: arm | PASS | 3 shots, 3 fired, aim error before firing avg 0.9deg; crosshair hitbox zone Torso/none/Arm (bone VBOT_:Spine2/none/VBOT_:LeftArm); hit-mark bones VBOT_:LeftHand, VBOT_:Head, VBOT_:Spine2; avg damage 21.1 (x1.71 chest) |  |
| Aim | Aim follows the moving enemy: leg | PASS | 3 shots, 3 fired, aim error before firing avg 1.0deg; crosshair hitbox zone Leg/Torso/none (bone VBOT_:RightUpLeg/VBOT_:Hips/none); hit-mark bones VBOT_:RightUpLeg, VBOT_:LeftUpLeg; avg damage 10.4 (x0.84 chest) |  |
| Aim | Aim follows the moving enemy: foot | PASS | 3 shots, 3 fired, aim error before firing avg 0.4deg; crosshair hitbox zone none/Leg (bone none/VBOT_:LeftFoot); hit-mark bones VBOT_:LeftFoot; avg damage 6.2 (x0.50 chest) |  |
| Aim | Body-part damage table (for later headshot/limb multipliers) | INFO | chest: zone Torso, avg dmg 12.3, x1.00 vs chest / head: zone Arm/Head, avg dmg 18.8, x1.52 vs chest / arm: zone Torso/none/Arm, avg dmg 21.1, x1.71 vs chest / leg: zone Leg/Torso/none, avg dmg 10.4, x0.84 vs chest / foot: zone none/Leg, avg dmg 6.2, x0.50 vs chest | damage multipliers per zone come from DMCombatBodyPartUtility.ApplyZoneDamageMultiplier (DM_EnemyHitMarkProfile); equal ratios here mean no zone multiplier is configured |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Performance per enemy (spawn cost, frame time, GC)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Performance | Axe_Droid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 26ms, 794 components, frame avg 36.1ms max 83.7ms (167 frames), GC collections 0, managed delta 6216KB |  |
| Performance | corrupt_patrol_android: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 26ms, 736 components, frame avg 33.9ms max 82.1ms (177 frames), GC collections 0, managed delta 5608KB |  |
| Performance | GreySpire Soldier: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 27ms, 797 components, frame avg 32.2ms max 84.7ms (187 frames), GC collections 0, managed delta 3908KB |  |
| Performance | Humadroid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 26ms, 787 components, frame avg 32.4ms max 70.1ms (186 frames), GC collections 0, managed delta 5708KB |  |
| Performance | Humanoid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 33ms, 696 components, frame avg 32.3ms max 92.3ms (186 frames), GC collections 0, managed delta 6792KB |  |
| Performance | HumanoidEnemy_Invector: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 32ms, 722 components, frame avg 31.8ms max 73.5ms (189 frames), GC collections 0, managed delta 6128KB |  |
| Performance | Hybrid Droid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 31ms, 696 components, frame avg 34.1ms max 77.1ms (176 frames), GC collections 0, managed delta 8444KB |  |
| Performance | Lisa_Hybrid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 31ms, 1162 components, frame avg 32.9ms max 74.0ms (183 frames), GC collections 0, managed delta 7300KB |  |
| Performance | Robot: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 31ms, 698 components, frame avg 32.4ms max 77.7ms (186 frames), GC collections 0, managed delta 6492KB |  |
| Performance | The_Evil_One: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 33ms, 707 components, frame avg 31.9ms max 76.3ms (189 frames), GC collections 0, managed delta 8276KB |  |
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
| AI | No circling while unaware (player 50m away, 9s) | PASS | yaw turned 160deg, path 10.1m, net 3.2m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 2.0s (at 2.2m, weapon Spear of Fate), first player damage 2.6s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 20.0 hp in 2.6s using Spear of Fate |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 20.2 (item dmg 16), attacks 1, hitbox-active frames 6 (unarmed 0), hit events 1, sub-1hp chip 0.5, first hit 0.4s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 220.0 (-30.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 220.0 -> 121.8; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.4s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 11 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/corrupt_patrol_android.prefab` - definition `corrupt_patrol_android` - melee `Death Axe` / ranged `sci_fi_pistol` - audited in 42s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=corrupt_patrol_android, melee=Death Axe (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=350 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 109deg, path 11.1m, net 8.4m, states Wander |  |
| AI | Notice / chase / attack timing (player 5m in front) | PASS | notice 0.2s, chase 0.2s, first attack 0.8s (at 2.2m, weapon Death Axe), first player damage 1.4s, attacks 1, states Wander/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 24.0 hp in 1.4s using Death Axe |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 24.0 (item dmg 18), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.0s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.9 (item dmg 14), shots/projectiles 1, attack-state frames 48, attacks 1, first hit 2.2s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Death_Axe] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 350.0 -> 324.2 (-25.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 324.2 -> 228.5; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.29s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/GreySpire Soldier.prefab` - definition `GreySpire Soldier` - melee `weap_two_handed` / ranged `-` - audited in 39s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=GreySpire Soldier, melee=weap_two_handed (dmg 28), ranged=- (dmg -), preferRanged=False, def.attackDamage=8, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| AI | No circling while unaware (player 55m away, 9s) | PASS | yaw turned 140deg, path 9.0m, net 3.5m, states Wander |  |
| AI | Notice / chase / attack timing (player 8m in front) | PASS | notice 0.0s, chase 0.0s, first attack 1.8s (at 2.2m, weapon weap_two_handed), first player damage 2.3s, attacks 1, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 38.0 hp in 2.3s using weap_two_handed |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 38.1 (item dmg 28), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 1.7, first hit 0.8s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 220.0 (-30.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 220.0 -> 146.9; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 45s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 223deg, path 11.6m, net 6.4m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.9s (at 2.2m, weapon weap2_sword), first player damage 2.5s, attacks 1, states Patrol/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.5s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 47.4 (item dmg 18), attacks 1, hitbox-active frames 3 (unarmed 0), hit events 1, sub-1hp chip 3.5, first hit 1.7s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 15.1 (item dmg 14), shots/projectiles 1, attack-state frames 48, attacks 1, first hit 2.3s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 172.6 (-27.4); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 172.6 -> 91.6; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.7s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humanoid.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 47s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 157deg, path 11.9m, net 5.4m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.8s (at 2.3m, weapon weap2_sword), first player damage 5.2s, attacks 2, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 5.2s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 47.3 (item dmg 18), attacks 1, hitbox-active frames 4 (unarmed 0), hit events 1, sub-1hp chip 2.6, first hit 1.2s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.3 (item dmg 14), shots/projectiles 1, attack-state frames 52, attacks 1, first hit 2.3s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 277.0 (-23.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 277.0 -> 160.0; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 5 clip(s) observed playing; 23 distinct clips seen in total |  |
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
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 177deg, path 7.2m, net 0.2m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.0s, chase 0.0s, first attack 1.8s (at 2.2m, weapon weap2_sword), first player damage 2.4s, attacks 1, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.4s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.2 (item dmg 18), attacks 1, hitbox-active frames 5 (unarmed 0), hit events 1, sub-1hp chip 0.6, first hit 0.2s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.4 (item dmg 14), shots/projectiles 1, attack-state frames 48, attacks 1, first hit 2.2s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 274.3 (-25.7); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 274.3 -> 126.4; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Hybrid Droid.prefab` - definition `FRED_Android` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 48s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=FRED_Android, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 140deg, path 8.1m, net 8.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.0s, chase 0.0s, first attack 1.8s (at 2.2m, weapon weap2_sword), first player damage 4.9s, attacks 2, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 4.9s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.3 (item dmg 18), attacks 2, hitbox-active frames 26 (unarmed 0), hit events 1, sub-1hp chip 6.6, first hit 3.2s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 13.9 (item dmg 14), shots/projectiles 1, attack-state frames 50, attacks 1, first hit 2.2s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 203.8 (-96.2); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 203.8 -> 101.6; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.4s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### Lisa_Hybrid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Lisa_Hybrid.prefab` - definition `Lisa_Hybrid` - melee `-` / ranged `survival_rifle` - audited in 39s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Lisa_Hybrid, melee=- (dmg -), ranged=survival_rifle (dmg 18), preferRanged=True, def.attackDamage=12, hp=400 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | FAIL | 4 renderer(s) with broken material | broken visuals: character_head:null material, eye_L:null material, eye_R:null material, Helmet_inside:null material (shader missing for the HDRP pipeline or material slot empty) |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 17 collider(s) on the hitbox layer, 91 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 146deg, path 12.7m, net 7.7m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.7s (at 9.6m, weapon survival_rifle), first player damage 3.2s, attacks 2, states Wander/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 19.3 hp in 3.2s using survival_rifle |  |
| Weapons | Melee weapon damages the player | SKIP | no resolved melee weapon |  |
| Weapons | Ranged 'survival_rifle' damages the player | PASS | player hp lost 17.6 (item dmg 18), shots/projectiles 1, attack-state frames 16, attacks 1, first hit 0.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 400.0 -> 375.1 (-24.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 375.1 -> 297.2; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.4s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `Robot_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 48s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Robot_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=15, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 88deg, path 7.6m, net 5.9m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.4s (at 2.2m, weapon weap2_sword), first player damage 3.8s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 47.0 hp in 3.8s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.3 (item dmg 18), attacks 2, hitbox-active frames 7 (unarmed 0), hit events 1, sub-1hp chip 5.1, first hit 2.4s, min distance 1.32m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.3 (item dmg 14), shots/projectiles 2, attack-state frames 92, attacks 2, first hit 4.3s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 476.3 (-23.7); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 476.3 -> 384.7; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Robot |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.4s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 18 distinct clips seen in total |  |
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

### The_Evil_One

Prefab `Assets/_Project/Prefabs/Combat/Enemies/The_Evil_One.prefab` - definition `The_Evil_One` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 49s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=The_Evil_One, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=True, def.attackDamage=12, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 170deg, path 11.9m, net 5.7m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.6s (at 9.4m, weapon sci_fi_pistol), first player damage 7.5s, attacks 5, states Wander/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 17.9 hp in 7.5s using sci_fi_pistol |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.2 (item dmg 18), attacks 1, hitbox-active frames 7 (unarmed 0), hit events 1, sub-1hp chip 2.8, first hit 1.5s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; no vMeleeWeapon on the drawn/bound weapon |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.0 (item dmg 14), shots/projectiles 2, attack-state frames 52, attacks 2, first hit 2.3s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.65 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 415.1 (-84.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 415.1 -> 305.2; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.5s, corpse gone respawns, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 4 clip(s) observed playing; 29 distinct clips seen in total |  |
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
| stagger/hit | Hit Big Front * | 1.17 |
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
