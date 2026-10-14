# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-10 19:47:12 -> 2026-10-10 20:06:27 (complete)
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

**10 enemies audited - 1 FAIL, 2 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 12 weapon cases + 13 scenarios): 387 PASS, 10 FAIL, 13 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 19:42: 41 change(s); baseline updated.

- CHANGED enemy|GreySpire Soldier|player.swing: WARN -> PASS
- CHANGED enemy|Humadroid|melee.damage: FAIL -> PASS
- CHANGED enemy|Humadroid|player.swing: WARN -> PASS
- CHANGED enemy|Humadroid|hitmarks: PASS -> WARN
- CHANGED enemy|Humanoid|player.swing: WARN -> PASS
- CHANGED enemy|Humanoid|anim: WARN -> INFO
- CHANGED enemy|HumanoidEnemy_Invector|player.swing: WARN -> PASS
- CHANGED enemy|HumanoidEnemy_Invector|anim: WARN -> INFO
- CHANGED enemy|Hybrid Droid|melee.damage: FAIL -> PASS
- CHANGED enemy|Hybrid Droid|player.swing: WARN -> PASS
- CHANGED enemy|Hybrid Droid|anim: WARN -> INFO
- CHANGED enemy|Lisa_Hybrid|player.swing: WARN -> PASS
- CHANGED enemy|Robot|player.swing: WARN -> PASS
- CHANGED enemy|The_Evil_One|player.swing: WARN -> PASS
- CHANGED enemy|The_Evil_One|anim: WARN -> INFO
- CHANGED weapon|2 Hander|audio.swing.2 Hander: FAIL -> PASS
- CHANGED weapon|2 Hander|audio.hitplayer.2 Hander: FAIL -> PASS
- CHANGED weapon|Blade Of Chaos|audio.swing.Blade Of Chaos: PASS -> FAIL
- CHANGED weapon|Blade Of Chaos|audio.hitplayer.Blade Of Chaos: FAIL -> PASS
- NEW weapon|Death Axe|audio.hitplayer.Death Axe = FAIL
- NEW weapon|Spear of Fate|audio.hitplayer.Spear of Fate = PASS
- CHANGED weapon|Spear of Fate|w.damage: FAIL -> PASS
- CHANGED weapon|Sword of Fear|audio.swing.Sword of Fear: FAIL -> PASS
- CHANGED weapon|weap2_sword|audio.swing.weap2_sword: FAIL -> PASS
- CHANGED weapon|weap_two_handed|audio.hitplayer.weap_two_handed: FAIL -> PASS
- CHANGED weapon|Wood Axe|audio.swing.Wood Axe: FAIL -> PASS
- CHANGED weapon|Wood Axe|audio.hitplayer.Wood Axe: FAIL -> PASS
- CHANGED scenario|Camera: faces forward after teleports, zoom in/out|cam.zoom: PASS -> WARN
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.sprint: FAIL -> PASS
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.jump: FAIL -> PASS
- CHANGED scenario|Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch|ctl.crouch: PASS -> WARN
- CHANGED scenario|Block: enemy defends against player swings|eblk: WARN -> PASS
- CHANGED scenario|Movement: retreat, push, flank, backstab, lose target|mv.backstab: WARN -> INFO
- CHANGED scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.approach: WARN -> PASS
- CHANGED scenario|Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return|play.jump: WARN -> PASS
- CHANGED scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.2 Hander: FAIL -> PASS
- CHANGED scenario|Player weapons: each melee/ranged item vs enemy|audio.pswing.weap2_sword: FAIL -> PASS
- CHANGED scenario|Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload|ammo.sci_fi_pistol.DMAmmoFxProfile_Plasma.load: FAIL -> PASS
- CHANGED scenario|Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload|ammo.sci_fi_pistol.DMAmmoFxProfile_Plasma.fill: WARN -> PASS
- CHANGED scenario|Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload|ammo.sci_fi_pistol.DMAmmoFxProfile_Resonance Stabilizer.load: FAIL -> PASS
- CHANGED scenario|Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload|ammo.sci_fi_pistol.DMAmmoFxProfile_Resonance Stabilizer.fill: WARN -> PASS

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 100 | FAIL | Melee 'Death Axe' damages the player | Death Axe | only 1.1 hp lost with 0 weapon hit events (passive survival chip, not the weapon) / attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon | Check the weapon's hit box: collider layer/tag against the player's hit tags, animation events that open the hitbox (ActiveDamage/OnEnableAttack), and that the weapon's vMeleeWeapon damage and the item damage are > 0. |
| 2 | 75 | FAIL | No error-pink / null materials | Lisa_Hybrid | broken visuals: character_head:null material, eye_L:null material, eye_R:null material, Helmet_inside:null material (shader missing for the HDRP pipeline or material slot empty) | Repair the prefab (re-add the script / fix the material / assign controller+avatar) via the Character Creator Fix or by hand. |
| 3 | 75 | WARN | Ranged 'DM_Mining_Tool' damages the player | DM_Mining_Tool | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 4 | 25 | WARN | Hit marks fire on damage | Humadroid | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) | Add DMEnemyHitMarks + the layer-26 hitbox rig; verify the body-type hit-mark profile. |
| 5 | 20 | FAIL | 'Blade Of Chaos' swing plays a swish | Blade Of Chaos | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 6 | 20 | FAIL | 'Death Axe' swing plays a swish | Death Axe | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 7 | 20 | FAIL | 'Death Axe' hitting the player plays a hit sound | Death Axe | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 8 | 20 | FAIL | 'Sword of Fear' hitting the player plays a hit sound | Sword of Fear | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 9 | 20 | FAIL | Swing swish (GameAudioManager.PlayWeaponSwing) is called by gameplay code | Audio: swing / hit sound wiring (static) | PlayWeaponSwing / PlayPunchSwing are never called, so NO weapon (player or enemy) ever plays a swish: add a call where a melee attack window opens (vMeleeManager.SetActiveAttack active=true or the animation event OnEnableAttack) | Inspect the measured value and cause on the detail row. |
| 10 | 20 | WARN | Player circles behind the enemy: enemy turns to face | Movement: retreat, push, flank, backstab, lose target | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player | Tune AI turn speed / personal space / lose-target time in the definition or brain profile. |
| 11 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory | Inspect the measured value and cause on the detail row. |
| 12 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Electricity: ammo type loads (Hot Cross path) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory | Inspect the measured value and cause on the detail row. |
| 13 | 20 | FAIL | survival_rifle + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory | Inspect the measured value and cause on the detail row. |
| 14 | -5 | WARN | Animation clip timings | Lisa_Hybrid |  | Inspect the measured value and cause on the detail row. |
| 15 | -5 | WARN | Camera zooms in and out | Camera: faces forward after teleports, zoom in/out | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state | Inspect the measured value and cause on the detail row. |
| 16 | -5 | WARN | Melee hit sound has a caller on the damage path | Audio: swing / hit sound wiring (static) | the only callers are the enemy-body receiver (PioneerInvectorDamageReceiver), MeleeCombatController and CombatHitResolver: damage TO THE PLAYER (PioneerInvectorDamageBridge / the player's vDamageReceiver) has no hit-sound call, so enemy weapons hitting the player are silent | Inspect the measured value and cause on the detail row. |
| 17 | -5 | WARN | Left Ctrl crouches | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) | Inspect the measured value and cause on the detail row. |
| 18 | -5 | WARN | Left Alt dash | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | Dash action not wired or on cooldown (DMDashController) | Inspect the measured value and cause on the detail row. |
| 19 | -5 | WARN | Hold Space after jumping boosts (jetpack) | Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | no sustained lift: jetpack not unlocked/fuelled, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player | Inspect the measured value and cause on the detail row. |
| 20 | -5 | WARN | Jetpack escape (hold Space + S) | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump | Inspect the measured value and cause on the detail row. |
| 21 | -5 | WARN | Enemy returns to its post after losing the player | Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | enemy did not walk back to its spawn within 30s: ReturnHome blocked, leash undefined, or it stays in Search/Investigate | Inspect the measured value and cause on the detail row. |
| 22 | -5 | WARN | sci_fi_pistol + DMAmmoFxProfile_Ion: hit marks / FX | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | damage landed but no mark was stamped: this ammo type has no body-type hit-mark entry (DMAmmoFxProfile) or the hit skipped DMEnemyHitMarks.HandleRangedHit | Inspect the measured value and cause on the detail row. |
| 23 | -5 | WARN | survival_rifle + DMAmmoFxProfile_Ion: hit marks / FX | Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | damage landed but no mark was stamped: this ammo type has no body-type hit-mark entry (DMAmmoFxProfile) or the hit skipped DMEnemyHitMarks.HandleRangedHit | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Axe_Droid | Spear of Fate | - | PASS | PASS | PASS | PASS | PASS | n/a | PASS | PASS | PASS | PASS |
| corrupt_patrol_android | Death Axe | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| GreySpire Soldier | weap_two_handed | - | PASS | PASS | PASS | PASS | PASS | n/a | PASS | PASS | PASS | PASS |
| Humadroid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | warn | PASS | PASS |
| Humanoid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| HumanoidEnemy_Invector | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Hybrid Droid | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| Lisa_Hybrid | - | survival_rifle | PASS | PASS | PASS | PASS | n/a | PASS | PASS | PASS | PASS | PASS |
| Robot | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |
| The_Evil_One | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |

## Weapons that deal NO damage (headline)

- **Weapon matrix: Death Axe** (melee): player hp lost 1.1 (item dmg 18), attacks 1, hitbox-active frames 0, hit events 0, sub-1hp chip 1.2, first hit 0.3s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1. **Probable cause:** only 1.1 hp lost with 0 weapon hit events (passive survival chip, not the weapon) | attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon

## Weapon matrix (every weapon item on the template, spawn override)

12 cases, 5 FAIL. Each weapon is equipped on `HumanoidEnemy_Invector` through ConfigureSpawnLoadout (the spawner path) and made to attack the player.

| Weapon | Kind | Equips | Damages player | One drawn | Measured | Probable cause |
|---|---|---|---|---|---|---|
| 2 Hander | melee | PASS | PASS | PASS | player hp lost 38.0 (item dmg 28), attacks 2, hitbox-active frames 18, hit events 1, sub-1hp chip 6.3, first hit 2.2s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer ... |  |
| Blade Of Chaos | melee | PASS | PASS | PASS | player hp lost 54.0 (item dmg 21), attacks 1, hitbox-active frames 13, hit events 1, sub-1hp chip 1.1, first hit 0.4s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hi... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Death Axe | melee | PASS | **FAIL** | PASS | player hp lost 1.1 (item dmg 18), attacks 1, hitbox-active frames 0, hit events 0, sub-1hp chip 1.2, first hit 0.3s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event / weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it / only 1.1 hp lost with 0 weapon hit events (passive survival chip, not the weapon) / attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon |
| Spear of Fate | melee | PASS | PASS | PASS | player hp lost 20.2 (item dmg 16), attacks 2, hitbox-active frames 13, hit events 1, sub-1hp chip 6.3, first hit 2.2s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitB... |  |
| Sword of Fear | melee | PASS | PASS | PASS | player hp lost 34.0 (item dmg 15), attacks 1, hitbox-active frames 11, hit events 1, sub-1hp chip 1.0, first hit 0.5s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitB... | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| weap2_sword | melee | PASS | PASS | PASS | player hp lost 26.1 (item dmg 18), attacks 1, hitbox-active frames 9, hit events 1, sub-1hp chip 1.6, first hit 0.1s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: l... |  |
| weap_two_handed | melee | PASS | PASS | PASS | player hp lost 38.1 (item dmg 28), attacks 2, hitbox-active frames 15, hit events 1, sub-1hp chip 6.5, first hit 2.5s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38;... |  |
| Wood Axe | melee | PASS | PASS | PASS | player hp lost 62.1 (item dmg 26), attacks 2, hitbox-active frames 23, hit events 2, sub-1hp chip 6.1, first hit 2.2s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer ... |  |
| DM_Mining_Tool | ranged | PASS | warn | PASS | player hp lost 15.4 (item dmg 0), shots/projectiles 2, attack-state frames 59, attacks 2, first hit 2.2s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCoo... |  |
| sci_fi_pistol | ranged | PASS | PASS | PASS | player hp lost 17.9 (item dmg 14), shots/projectiles 1, attack-state frames 21, attacks 1, first hit 0.7s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRang... |  |
| survival_rifle | ranged | PASS | PASS | PASS | player hp lost 15.9 (item dmg 18), shots/projectiles 1, attack-state frames 22, attacks 1, first hit 0.7s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRa... |  |
| OVERRIDE both slots | dual-slot spawn override | - | - | PASS | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)): PASS ; Starts with the ranged override drawn (prefer-ranged on, start-melee off): PASS ; Switch to the melee override vi... |  |

### Weapon: 2 Hander

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item 2 Hander; drawn [Drawn_2_Hander]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | '2 Hander' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_6:Melee Weapon Air Swing 4 |  |
| Audio | '2 Hander' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_7:Melee Weapon Attack 1; vHitEffects on 'Drawn_2_Hander': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee '2 Hander' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 2, hitbox-active frames 18, hit events 1, sub-1hp chip 6.3, first hit 2.2s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 2.2s, attacks 2, attack-state frames 89, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Blade Of Chaos

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 21 |  |
| Audio | 'Blade Of Chaos' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Blade Of Chaos' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_6:Melee Weapon Attack 3; vHitEffects on 'Drawn_Blade_Of_Chaos': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 54.0 (item dmg 21), attacks 1, hitbox-active frames 13, hit events 1, sub-1hp chip 1.1, first hit 0.4s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.2m), first damage 0.4s, attacks 1, attack-state frames 29, clips seen: Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Death Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'Death Axe' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Death Axe' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_Death_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee 'Death Axe' damages the player | FAIL | player hp lost 1.1 (item dmg 18), attacks 1, hitbox-active frames 0, hit events 0, sub-1hp chip 1.2, first hit 0.3s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 | only 1.1 hp lost with 0 weapon hit events (passive survival chip, not the weapon) / attacks started but the weapon damage window never opened (hitbox canApplyDamage stayed false): attack animation event (ActiveDamage/OnEnableAttack) never fires or vMeleeManager.rightWeapon is not the drawn weapon |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 27, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Spear of Fate

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 16 |  |
| Audio | 'Spear of Fate' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_6:QP01 0005 Wind howling, SfxPool_8:Melee Weapon Air Swing 4 |  |
| Audio | 'Spear of Fate' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_9:Melee Weapon Attack 3; vHitEffects on 'Drawn_Spear_of_Fate': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 20.2 (item dmg 16), attacks 2, hitbox-active frames 13, hit events 1, sub-1hp chip 6.3, first hit 2.2s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 2.2s, attacks 2, attack-state frames 89, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Sword of Fear

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Sword of Fear; drawn [Drawn_Sword_of_Fear]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 15 |  |
| Audio | 'Sword of Fear' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_8:QP01 0005 Wind howling, SfxPool_9:Melee Weapon Air Swing 1 |  |
| Audio | 'Sword of Fear' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_Sword_of_Fear': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee 'Sword of Fear' damages the player | PASS | player hp lost 34.0 (item dmg 15), attacks 1, hitbox-active frames 11, hit events 1, sub-1hp chip 1.0, first hit 0.5s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.5s, attacks 1, attack-state frames 31, clips seen: Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap2_sword

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'weap2_sword' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_2:Melee Weapon Attack 3 |  |
| Audio | 'weap2_sword' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_2:Melee Weapon Attack 3; vHitEffects on 'Drawn_weap2_sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.1 (item dmg 18), attacks 1, hitbox-active frames 9, hit events 1, sub-1hp chip 1.6, first hit 0.1s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.1s, attacks 1, attack-state frames 20, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap_two_handed

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | 'weap_two_handed' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_3:QP01 0005 Wind howling, SfxPool_5:Melee Weapon Air Swing 1 |  |
| Audio | 'weap_two_handed' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_6:Melee Weapon Attack 3; vHitEffects on 'Drawn_Two_Handed_Sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 38.1 (item dmg 28), attacks 2, hitbox-active frames 15, hit events 1, sub-1hp chip 6.5, first hit 2.5s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 2.5s, attacks 2, attack-state frames 93, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Wood Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 26 |  |
| Audio | 'Wood Axe' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_8:Melee Weapon Air Swing 4 |  |
| Audio | 'Wood Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_9:Melee Weapon Attack 3, SfxPool_2:Melee Weapon Attack 3; vHitEffects on 'Drawn_Wood_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Wood Axe' damages the player | PASS | player hp lost 62.1 (item dmg 26), attacks 2, hitbox-active frames 23, hit events 2, sub-1hp chip 6.1, first hit 2.2s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 2.2s, attacks 2, attack-state frames 86, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: DM_Mining_Tool

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item DM_Mining_Tool; drawn []; holstered []; item dmg 0 | ItemData rangedDamage = 0 |
| Weapons | Ranged 'DM_Mining_Tool' damages the player | WARN | player hp lost 15.4 (item dmg 0), shots/projectiles 2, attack-state frames 59, attacks 2, first hit 2.2s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item |
| Timing | Attack timing | INFO | first attack 0.7s (at 7.2m), first damage 2.2s, attacks 2, attack-state frames 59, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: sci_fi_pistol

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; item dmg 14 |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.9 (item dmg 14), shots/projectiles 1, attack-state frames 21, attacks 1, first hit 0.7s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Timing | Attack timing | INFO | first attack 0.7s (at 7.2m), first damage 0.7s, attacks 1, attack-state frames 21, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: survival_rifle

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_weap2_sword]; item dmg 18 |  |
| Weapons | Ranged 'survival_rifle' damages the player | PASS | player hp lost 15.9 (item dmg 18), shots/projectiles 1, attack-state frames 22, attacks 1, first hit 0.7s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Timing | Attack timing | INFO | first attack 0.6s (at 7.1m), first damage 0.7s, attacks 1, attack-state frames 22, clips seen: none matched |  |
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
| Player controls: WASD, look, mouse buttons, jump, jetpack, sprint, crouch, dodge, dash, switch | 17 | 0 | 3 | 59s |
| Block + parry: player defends against enemy melee | 3 | 0 | 0 | 45s |
| Block: enemy defends against player swings | 2 | 0 | 0 | 16s |
| Movement: retreat, push, flank, backstab, lose target | 4 | 0 | 1 | 36s |
| Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return | 7 | 0 | 2 | 73s |
| Multi-enemy: 3 at once | 2 | 0 | 0 | 23s |
| Player weapons: each melee/ranged item vs enemy | 27 | 0 | 0 | 91s |
| Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload | 97 | 3 | 2 | 159s |
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
| Controls | S moves the player back | PASS | start 0.00, min -1.98, max 0.00, end -2.01 m along forward |  |
| Controls | A strafes left | PASS | start 0.00, min 0.00, max 1.97, end 2.03 m along left |  |
| Controls | D strafes right | PASS | start 0.00, min 0.00, max 1.93, end 2.00 m along right |  |
| Controls | W run speed (reference for sprint) | PASS | start 0.00, min 0.00, max 2.58, end 2.61 m in 1.5s |  |
| Controls | Shift + W sprints faster than W | PASS | start 0.00, min 0.00, max 5.04, end 5.18 m in 1.5s |  |
| Controls | Space jumps | PASS | start 21.23, min 21.23, max 1021.72, end 21.40 m height (+1000 while isJumping) |  |
| Controls | Left Ctrl crouches | WARN | start 0.00, min 0.00, max 0.00, end 0.00 isCrouching | Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch()) |
| Controls | Crouch through the controller code path (cc.Crouch) | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isCrouching |  |
| Controls | Q dodge / roll | PASS | start 0.00, min 0.00, max 14.22, end 14.28 m or isRolling |  |
| Controls | Left Alt dash | WARN | start 0.00, min 0.00, max 1.19, end 1.22 m | Dash action not wired or on cooldown (DMDashController) |
| Controls | Hold Space after jumping boosts (jetpack) | WARN | start 21.23, min 21.23, max 21.85, end 21.23 m height | no sustained lift: jetpack not unlocked/fuelled, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player |
| Controls | Mouse look left/right orbits the camera | PASS | start 0.00, min 0.00, max 109.45, end 112.40 deg yaw |  |
| Controls | Mouse look up/down tilts the camera | PASS | start 18.00, min 0.00, max 49.91, end 49.91 deg pitch |  |
| Controls | Left click swings the drawn melee weapon | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isAttacking |  |
| Controls | Right click blocks (melee drawn) | PASS | start 0.00, min 0.00, max 1.00, end 1.00 isBlocking |  |
| Controls | Middle click does not break gameplay | PASS | start 0.00, min 0.00, max 0.00, end 0.00 none |  |
| Controls | Middle click has no gameplay binding | INFO | InputSystem_Actions Player map binds no action to Mouse/middleButton (only the UI map MiddleClick) |  |
| Controls | Tab (SwitchWeapon) cycles the weapon focus | PASS | Hot Cross weapon focus 0 -> 1, drawn weap2_sword -> sci_fi_pistol |  |
| Controls | Half-tilt stick walks slower than a run | PASS | start 0.00, min 0.00, max 0.81, end 0.85 m in 1.5s |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block + parry: player defends against enemy melee

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Unblocked baseline (same enemy, same distance) | INFO | hits 1, player hp lost 26.1 |  |
| Block | Holding block negates enemy melee damage | PASS | isBlocking True (raised in 0.04s), hits 2, blocked 2, player hp lost 0.0 (unblocked 26.1), stamina 1000.0 |  |
| Block | Enemy guard-break stagger after a blocked hit | INFO | longest enemy stagger 1.17s (profile blockStaggerSeconds 1.2s) |  |
| Parry | Parry timing windows (block pressed relative to the enemy swing) | INFO | press +0.00s after swing start: hit 0.51s after press -> HIT (block came too late), stagger 0.00s, hp lost 26.3 // press +0.15s after swing start: hit 0.36s after press -> BLOCK, stagger 1.17s, hp lost 0.0 // press +0.30s after swing start: hit 0.19s after press -> PARRY, stagger 3.27s, hp lost 0.0 // press +0.45s after swing start: hit 0.04s after press -> PARRY, stagger 3.42s, hp lost 0.0 // press +0.60s after swing start: hit 0.27s after press -> BLOCK, stagger 1.16s, hp lost 26.0 | profile: parryWindowSeconds 0.2, blockStagger 1.2s, parryStagger 5.0s |
| Parry | A parry happens when block is pressed inside the window | PASS | parry observed inside the window |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Block: enemy defends against player swings

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Block | Enemy block reduces player melee damage | PASS | open: 33 hits, enemy hp lost 184.2; blocking (animator IsBlocking True, duration 40.0s): 11 hits (3 flagged blocked), enemy hp lost 115.8; block clips: Idle, Walk, Idle@Pistol, Defense_Unarmed |  |
| Parry | Enemy parry | INFO | not implemented: EnemyInvectorCombatBridge only has TryBeginBlock/EndBlock (no parry window/riposte API) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Movement: retreat, push, flank, backstab, lose target

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Movement | Player retreats 9m at 3 m/s: enemy follows | PASS | distance 3.8m -> 5.5m (min 2.2m), states Chase/Attack, attacks 1 |  |
| Movement | Player pushes into the enemy: spacing and facing | PASS | min distance 0.50m, max facing error 0deg, attack-state frames 79, states Attack/Chase |  |
| Movement | Player circles behind the enemy: enemy turns to face | WARN | reached behind at never, enemy faced player (<30deg) after never, max facing error 0deg, attacks while behind 0, player hp lost -18.6, states Attack | player never got 140deg behind the moving enemy (enemy rotated with the circle): that itself shows it tracks the player |
| Movement | Hit from behind vs from the front (player melee, AI off) | INFO | front: 21 hits, 82.4 hp; behind: 27 hits, 119.3 hp; ratio 1.45 | no backstab bonus implemented if ratio is 1.0 |
| Movement | Player disappears 70m away: enemy loses the target | PASS | was aware True, investigate/search after never, back to calm state after 6.0s, states Wander/Chase/ReturnHome |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Play: approach, kite, strafe, re-engage, jump, jetpack escape, lose target, return

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Play | Approach with W: enemy notices | PASS | walked 21.5m in 10.7s to 8.5m; enemy noticed after 10.2s at 12.3m; state Hold |  |
| Play | Run away with S: enemy pursues | PASS | player backed away for 4.0s (gap now 3.9m), enemy moved 12.0m, hostile state seen True, states Hold/Chase/Attack |  |
| Play | Strafe around the enemy: it turns to face the player | PASS | enemy facing error avg 0deg, max 0deg over 93 frames while strafing D then A, state Attack |  |
| Play | Retreat then re-engage: enemy fights back | PASS | re-engaged to 1.9m, enemy attacks 2, player hp lost 47.0, state Attack |  |
| Play | Jump away: player clears distance | PASS | jump height 0.91m, gap 1.9m -> 4.1m, enemy state Attack | player could not gain distance (enemy kept pace or player blocked) |
| Play | Jetpack escape (hold Space + S) | WARN | peak height gain 0.9m, gap 4.1m -> 2.4m, enemy state Attack | no sustained lift: jetpack locked/no fuel or DMJetpackInputBridge not polling Space; the enemy-reaction part still ran as a jump |
| Play | Sprint away: enemy loses the target | PASS | lost after 6.8s at 3.0m (sprinting), state Chase, states Wander/Hold/Chase/Attack/ReturnHome |  |
| Play | Enemy returns to its post after losing the player | WARN | back home after never, now 57.8m from spawn, state Attack | enemy did not walk back to its spawn within 30s: ReturnHome blocked, leash undefined, or it stays in Search/Investigate |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Multi-enemy: 3 at once

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Multi-enemy | 3 enemies at once for 22s | PASS | max simultaneous attackers 2, distinct attackers 3/3, attack-state frames [257,408,229], chase/hold frames [275,125,305], closest pair 1.92m, closest to player 1.16m, player hp lost 98.6 (topped up every 2s), 535 frames |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Player weapons: each melee/ranged item vs enemy

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Audio | Player '2 Hander' swing plays a swish | PASS | 2 audio start(s) outside the hit windows over 4 swings: SfxPool_4:Melee Weapon Air Swing 3, SfxPool_1:Melee Weapon Attack 3, SfxPool_9:Melee Weapon Attack 1 |  |
| Audio | Player '2 Hander' hitting the enemy plays a hit sound | PASS | 40/45 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee '2 Hander' on the enemy | PASS | enemy hp lost 157.0 (item dmg 28), hits 45, player right weapon Drawn_2_Hander |  |
| Audio | Player 'Blade Of Chaos' swing plays a swish | PASS | 8 audio start(s) outside the hit windows over 4 swings: SfxPool_5:QP01 0005 Wind howling, SfxPool_6:Melee Weapon Air Swing 2, SfxPool_9:Melee Weapon Attack 4 |  |
| Audio | Player 'Blade Of Chaos' hitting the enemy plays a hit sound | PASS | 20/20 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Blade Of Chaos' on the enemy | PASS | enemy hp lost 119.7 (item dmg 21), hits 20, player right weapon InvectorWeapon_Blade Of Chaos |  |
| Audio | Player 'Death Axe' swing plays a swish | PASS | 5 audio start(s) outside the hit windows over 4 swings: SfxPool_7:QP01 0005 Wind howling, SfxPool_8:Melee Weapon Air Swing 1, SfxPool_0:Melee Weapon Attack 3 |  |
| Audio | Player 'Death Axe' hitting the enemy plays a hit sound | PASS | 45/46 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Death Axe' on the enemy | PASS | enemy hp lost 178.7 (item dmg 18), hits 46, player right weapon Drawn_Death_Axe |  |
| Audio | Player 'Spear of Fate' swing plays a swish | PASS | 5 audio start(s) outside the hit windows over 4 swings: SfxPool_7:QP01 0005 Wind howling, SfxPool_8:Melee Weapon Air Swing 1, SfxPool_0:Melee Weapon Attack 3 |  |
| Audio | Player 'Spear of Fate' hitting the enemy plays a hit sound | PASS | 46/46 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Spear of Fate' on the enemy | PASS | enemy hp lost 112.8 (item dmg 16), hits 46, player right weapon Drawn_Spear_of_Fate |  |
| Audio | Player 'Sword of Fear' swing plays a swish | PASS | 5 audio start(s) outside the hit windows over 4 swings: SfxPool_7:Melee Weapon Air Swing 3, SfxPool_0:Melee Weapon Attack 1, SfxPool_1:Melee Weapon Attack 4 |  |
| Audio | Player 'Sword of Fear' hitting the enemy plays a hit sound | PASS | 40/40 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Sword of Fear' on the enemy | PASS | enemy hp lost 130.1 (item dmg 15), hits 40, player right weapon Drawn_Sword_of_Fear |  |
| Audio | Player 'weap2_sword' swing plays a swish | PASS | 5 audio start(s) outside the hit windows over 4 swings: SfxPool_1:Melee Weapon Air Swing 2, SfxPool_2:QP01 0005 Wind howling, SfxPool_9:Melee Weapon Attack 4 |  |
| Audio | Player 'weap2_sword' hitting the enemy plays a hit sound | PASS | 20/36 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'weap2_sword' on the enemy | PASS | enemy hp lost 124.6 (item dmg 18), hits 36, player right weapon Drawn_weap2_sword |  |
| Audio | Player 'weap_two_handed' swing plays a swish | PASS | 6 audio start(s) outside the hit windows over 4 swings: SfxPool_2:QP01 0005 Wind howling, SfxPool_4:QP01 0005 Wind howling, SfxPool_3:Melee Weapon Air Swing 3 |  |
| Audio | Player 'weap_two_handed' hitting the enemy plays a hit sound | PASS | 56/56 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'weap_two_handed' on the enemy | PASS | enemy hp lost 142.2 (item dmg 28), hits 56, player right weapon Drawn_Two_Handed_Sword |  |
| Audio | Player 'Wood Axe' swing plays a swish | PASS | 4 audio start(s) outside the hit windows over 4 swings: SfxPool_6:QP01 0005 Wind howling, SfxPool_5:Melee Weapon Air Swing 3, SfxPool_8:Melee Weapon Attack 3 |  |
| Audio | Player 'Wood Axe' hitting the enemy plays a hit sound | PASS | 30/42 hit(s) had audio within 0.5s |  |
| Player weapons | Player uses melee 'Wood Axe' on the enemy | PASS | enemy hp lost 163.7 (item dmg 26), hits 42, player right weapon Drawn_Wood_Axe |  |
| Player weapons | Player uses 'DM_Mining_Tool' | INFO | mining tool: Plasma Fuel scanner/laser, not a damage weapon (damage 0 by design) |  |
| Player weapons | Player uses ranged 'sci_fi_pistol' on the enemy | PASS | enemy hp lost 92.6 (item dmg 14), hits 0, player right weapon none |  |
| Player weapons | Player uses ranged 'survival_rifle' on the enemy | PASS | enemy hp lost 156.9 (item dmg 18), hits 0, player right weapon none |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Ammo: fill clips, change ammo types, fire / damage / marks / empty / reload

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Ammo | Ammo items and ranged weapons found | INFO | 10 ammo item(s): DMAmmoFxProfile_CarveExperiment(Gunpowder), DMAmmoFxProfile_Electricity(Electricity), DMAmmoFxProfile_Explosive(Explosive), DMAmmoFxProfile_Fire(Fire), DMAmmoFxProfile_Ice(Ice), DMAmmoFxProfile_Ion(Ion), DMAmmoFxProfile_Laser(Laser), DMAmmoFxProfile_Plasma(Plasma), DMAmmoFxProfile_Resonance Stabilizer(ResonanceStabilizer), DMAmmoFxProfile_Standard(Gunpowder); 2 ranged weapon(s): sci_fi_pistol, survival_rifle |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_CarveExperiment', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.20s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: damages the enemy | PASS | enemy hp lost 43.3 (weapon dmg 14, ammo type Gunpowder, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_CarveExperiment: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Electricity', ammoType Electricity |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: damages the enemy | PASS | enemy hp lost 41.5 (weapon dmg 14, ammo type Electricity, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Electricity: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Explosive', ammoType Explosive |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 1.00s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: damages the enemy | PASS | enemy hp lost 152.1 (weapon dmg 14, ammo type Explosive, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Explosive: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Fire', ammoType Fire |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.20s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: damages the enemy | PASS | enemy hp lost 132.4 (weapon dmg 14, ammo type Fire, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Fire: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ice', ammoType Ice |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.40s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: damages the enemy | PASS | enemy hp lost 140.8 (weapon dmg 14, ammo type Ice, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ice: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ion', ammoType Ion |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.50s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: damages the enemy | PASS | enemy hp lost 172.0 (weapon dmg 14, ammo type Ion, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: hit marks / FX | WARN | active hit marks 0 -> 0, body type Humanoid | damage landed but no mark was stamped: this ammo type has no body-type hit-mark entry (DMAmmoFxProfile) or the hit skipped DMEnemyHitMarks.HandleRangedHit |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Ion: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Laser', ammoType Laser |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: damages the enemy | PASS | enemy hp lost 147.2 (weapon dmg 14, ammo type Laser, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Laser: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: ammo type loads (Hot Cross path) | PASS | loaded 45/45 of 'DMAmmoFxProfile_Plasma', ammoType Plasma |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: magazine fills to capacity | PASS | magazine 45/45 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 45 -> 42, shot interval 0.20s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: damages the enemy | PASS | enemy hp lost 97.0 (weapon dmg 14, ammo type Plasma, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Plasma: empty + reload | INFO | magazine capacity 45 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Resonance Stabilizer', ammoType ResonanceStabilizer |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: damages the enemy | PASS | enemy hp lost 47.9 (weapon dmg 14, ammo type ResonanceStabilizer, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Resonance Stabilizer: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.20s |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: damages the enemy | PASS | enemy hp lost 53.4 (weapon dmg 14, ammo type Gunpowder, ammo is DMAmmoFxProfile) |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | sci_fi_pistol + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | X (AmmoCycle) switches the loaded ammo type | PASS | 3 ammo stack(s) on the Hot Cross utility slots; loaded DMAmmoFxProfile_Standard -> DMAmmoFxProfile_CarveExperiment -> DMAmmoFxProfile_Electricity |  |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: ammo type loads (Hot Cross path) | FAIL | loaded 22/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder, error: ammo is not in the inventory | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.20s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: damages the enemy | PASS | enemy hp lost 40.1 (weapon dmg 18, ammo type Gunpowder, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | survival_rifle + DMAmmoFxProfile_CarveExperiment: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: ammo type loads (Hot Cross path) | FAIL | loaded 27/30 of 'DMAmmoFxProfile_CarveExperiment', ammoType Electricity, error: ammo is not in the inventory | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: damages the enemy | PASS | enemy hp lost 43.3 (weapon dmg 18, ammo type Electricity, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Electricity: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: ammo type loads (Hot Cross path) | FAIL | loaded 27/30 of 'DMAmmoFxProfile_CarveExperiment', ammoType Explosive, error: ammo is not in the inventory | TryEquipAmmoToActiveRangedWeapon / WeaponAmmoState.TryEquipAmmoToWeaponSlot refused: ammo is not in the inventory |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 1.00s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: damages the enemy | PASS | enemy hp lost 45.0 (weapon dmg 18, ammo type Explosive, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Explosive: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Fire', ammoType Fire |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 26, shot interval 0.20s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: damages the enemy | PASS | enemy hp lost 173.1 (weapon dmg 18, ammo type Fire, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: hit marks / FX | PASS | active hit marks 0 -> 4, body type Humanoid |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Fire: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ice', ammoType Ice |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.40s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: damages the enemy | PASS | enemy hp lost 161.0 (weapon dmg 18, ammo type Ice, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ice: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Ion', ammoType Ion |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.50s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: damages the enemy | PASS | enemy hp lost 173.8 (weapon dmg 18, ammo type Ion, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: hit marks / FX | WARN | active hit marks 0 -> 0, body type Humanoid | damage landed but no mark was stamped: this ammo type has no body-type hit-mark entry (DMAmmoFxProfile) or the hit skipped DMEnemyHitMarks.HandleRangedHit |
| Ammo | survival_rifle + DMAmmoFxProfile_Ion: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Laser', ammoType Laser |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: damages the enemy | PASS | enemy hp lost 157.9 (weapon dmg 18, ammo type Laser, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Laser: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: ammo type loads (Hot Cross path) | PASS | loaded 45/45 of 'DMAmmoFxProfile_Plasma', ammoType Plasma |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: magazine fills to capacity | PASS | magazine 45/45 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 45 -> 40, shot interval 0.20s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: damages the enemy | PASS | enemy hp lost 152.8 (weapon dmg 18, ammo type Plasma, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: hit marks / FX | PASS | active hit marks 0 -> 5, body type Humanoid |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Plasma: empty + reload | INFO | magazine capacity 45 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Resonance Stabilizer', ammoType ResonanceStabilizer |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.25s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: damages the enemy | PASS | enemy hp lost 43.7 (weapon dmg 18, ammo type ResonanceStabilizer, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Resonance Stabilizer: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: ammo type loads (Hot Cross path) | PASS | loaded 30/30 of 'DMAmmoFxProfile_Standard', ammoType Gunpowder |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: magazine fills to capacity | PASS | magazine 30/30 after top-up (90 rounds were in the inventory) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: fires (rounds consumed) | PASS | 3 trigger pulls, rounds 30 -> 27, shot interval 0.20s |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: damages the enemy | PASS | enemy hp lost 57.5 (weapon dmg 18, ammo type Gunpowder, ammo is DMAmmoFxProfile) |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: hit marks / FX | PASS | active hit marks 0 -> 3, body type Humanoid |  |
| Ammo | survival_rifle + DMAmmoFxProfile_Standard: empty + reload | INFO | magazine capacity 30 too large to drain in an audit run; reload covered by the smaller magazines |  |
| Ammo | Ammo matrix size | INFO | 20 weapon x ammo combination(s) tested (mining tool excluded: it runs on Plasma Fuel charge, not discrete ammo) |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Aim: tracks a moving enemy, chest / head / arm / leg / foot, body-part hits

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Aim | Aim follows the moving enemy: chest | PASS | 3 shots, 3 fired, aim error before firing avg 0.7deg; crosshair hitbox zone Torso (bone VBOT_:Spine2); hit-mark bones VBOT_:Spine2, VBOT_:LeftHand; avg damage 20.1 (x1.00 chest) |  |
| Aim | Aim follows the moving enemy: head | PASS | 3 shots, 3 fired, aim error before firing avg 0.5deg; crosshair hitbox zone Head (bone VBOT_:Head); hit-mark bones VBOT_:RightArm, VBOT_:Head; avg damage 25.3 (x1.26 chest) |  |
| Aim | Aim follows the moving enemy: arm | PASS | 3 shots, 3 fired, aim error before firing avg 0.9deg; crosshair hitbox zone Torso/none (bone VBOT_:Spine2/none); hit-mark bones VBOT_:LeftHand, VBOT_:Head; avg damage 14.9 (x0.74 chest) |  |
| Aim | Aim follows the moving enemy: leg | PASS | 3 shots, 3 fired, aim error before firing avg 0.7deg; crosshair hitbox zone Leg/Torso/none (bone VBOT_:RightUpLeg/VBOT_:Hips/none); hit-mark bones VBOT_:RightUpLeg, VBOT_:LeftUpLeg; avg damage 10.6 (x0.53 chest) |  |
| Aim | Aim follows the moving enemy: foot | PASS | 3 shots, 3 fired, aim error before firing avg 0.6deg; crosshair hitbox zone none/Leg (bone none/VBOT_:LeftFoot); hit-mark bones VBOT_:LeftFoot; avg damage 6.2 (x0.31 chest) |  |
| Aim | Body-part damage table (for later headshot/limb multipliers) | INFO | chest: zone Torso, avg dmg 20.1, x1.00 vs chest / head: zone Head, avg dmg 25.3, x1.26 vs chest / arm: zone Torso/none, avg dmg 14.9, x0.74 vs chest / leg: zone Leg/Torso/none, avg dmg 10.6, x0.53 vs chest / foot: zone none/Leg, avg dmg 6.2, x0.31 vs chest | damage multipliers per zone come from DMCombatBodyPartUtility.ApplyZoneDamageMultiplier (DM_EnemyHitMarkProfile); equal ratios here mean no zone multiplier is configured |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Scenario: Performance per enemy (spawn cost, frame time, GC)

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Performance | Axe_Droid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 26ms, 794 components, frame avg 29.8ms max 48.9ms (202 frames), GC collections 0, managed delta 9824KB |  |
| Performance | corrupt_patrol_android: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 32ms, 736 components, frame avg 30.4ms max 67.7ms (198 frames), GC collections 0, managed delta 10596KB |  |
| Performance | GreySpire Soldier: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 27ms, 797 components, frame avg 33.1ms max 57.7ms (182 frames), GC collections 0, managed delta 10236KB |  |
| Performance | Humadroid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 28ms, 787 components, frame avg 33.4ms max 61.2ms (180 frames), GC collections 0, managed delta 10836KB |  |
| Performance | Humanoid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 138ms, 696 components, frame avg 29.6ms max 47.9ms (203 frames), GC collections 0, managed delta 1000KB |  |
| Performance | HumanoidEnemy_Invector: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 31ms, 722 components, frame avg 30.3ms max 55.7ms (199 frames), GC collections 0, managed delta 3740KB |  |
| Performance | Hybrid Droid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 32ms, 696 components, frame avg 30.0ms max 47.5ms (200 frames), GC collections 0, managed delta 4660KB |  |
| Performance | Lisa_Hybrid: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 33ms, 1162 components, frame avg 30.1ms max 48.0ms (200 frames), GC collections 0, managed delta 5552KB |  |
| Performance | Robot: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 31ms, 698 components, frame avg 29.7ms max 52.4ms (202 frames), GC collections 0, managed delta 5776KB |  |
| Performance | The_Evil_One: spawn cost / frame time / GC (editor, 6s, player 14m away) | PASS | spawn 33ms, 707 components, frame avg 29.8ms max 44.9ms (202 frames), GC collections 0, managed delta 5940KB |  |
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
| AI | No circling while unaware (player 50m away, 9s) | PASS | yaw turned 139deg, path 8.8m, net 3.6m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.9s (at 2.2m, weapon Spear of Fate), first player damage 2.4s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 20.0 hp in 2.4s using Spear of Fate |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 20.0 (item dmg 16), attacks 1, hitbox-active frames 7 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.6s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 220.0 (-30.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 220.0 -> 151.2; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.6s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 9 distinct clips seen in total |  |
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

### corrupt_patrol_android

Prefab `Assets/_Project/Prefabs/Combat/Enemies/corrupt_patrol_android.prefab` - definition `corrupt_patrol_android` - melee `Death Axe` / ranged `sci_fi_pistol` - audited in 44s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=corrupt_patrol_android, melee=Death Axe (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=350 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 157deg, path 14.7m, net 12.2m, states Wander |  |
| AI | Notice / chase / attack timing (player 5m in front) | PASS | notice 0.2s, chase 0.2s, first attack 0.8s (at 2.2m, weapon Death Axe), first player damage 3.7s, attacks 2, states Wander/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 24.0 hp in 3.7s using Death Axe |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 24.1 (item dmg 18), attacks 1, hitbox-active frames 1 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.0s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.5 (item dmg 14), shots/projectiles 1, attack-state frames 50, attacks 1, first hit 2.1s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Death_Axe] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 350.0 -> 320.8 (-29.2); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 320.8 -> 175.3; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.7s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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
| AI | No circling while unaware (player 55m away, 9s) | PASS | yaw turned 173deg, path 10.1m, net 4.6m, states Wander |  |
| AI | Notice / chase / attack timing (player 8m in front) | PASS | notice 0.0s, chase 0.0s, first attack 2.1s (at 2.2m, weapon weap_two_handed), first player damage 3.4s, attacks 1, states Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 68.0 hp in 3.4s using weap_two_handed |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.5s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 222.9 (-27.1); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 222.9 -> 151.9; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.29s, loot bag 6.4s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### Humadroid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 43s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 223deg, path 11.6m, net 6.4m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.8s (at 2.3m, weapon weap2_sword), first player damage 3.1s, attacks 1, states Patrol/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 47.0 hp in 3.1s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 6 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.2s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 15.1 (item dmg 14), shots/projectiles 1, attack-state frames 51, attacks 1, first hit 2.3s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 173.1 (-26.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 173.1 -> 0.0; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | WARN | active marks 0 -> 0, body type Android | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.1s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 20 distinct clips seen in total |  |
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

### Humanoid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humanoid.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 45s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 133deg, path 10.6m, net 6.9m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.8s (at 2.2m, weapon weap2_sword), first player damage 4.9s, attacks 2, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 4.9s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 4 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.1s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.9 (item dmg 14), shots/projectiles 1, attack-state frames 51, attacks 1, first hit 2.2s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 277.4 (-22.6); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 277.4 -> 97.3; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.5s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### HumanoidEnemy_Invector

Prefab `Assets/_Project/Prefabs/Combat/Enemies/HumanoidEnemy_Invector.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 44s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 87deg, path 6.6m, net 5.0m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.9s (at 2.2m, weapon weap2_sword), first player damage 2.4s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.4s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.4 (item dmg 18), attacks 1, hitbox-active frames 6 (unarmed 0), hit events 1, sub-1hp chip 1.7, first hit 1.0s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.3 (item dmg 14), shots/projectiles 1, attack-state frames 45, attacks 1, first hit 2.3s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 279.4 (-20.6); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 279.4 -> 201.3; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.6s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### Hybrid Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Hybrid Droid.prefab` - definition `FRED_Android` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 43s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=FRED_Android, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 151deg, path 12.9m, net 7.9m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.8s (at 2.2m, weapon weap2_sword), first player damage 2.3s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.3s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 47.0 (item dmg 18), attacks 1, hitbox-active frames 2 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.2s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 13.9 (item dmg 14), shots/projectiles 1, attack-state frames 52, attacks 1, first hit 2.2s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 270.6 (-29.4); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 270.6 -> 189.7; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.29s, loot bag 6.6s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 19 distinct clips seen in total |  |
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

### Lisa_Hybrid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Lisa_Hybrid.prefab` - definition `Lisa_Hybrid` - melee `-` / ranged `survival_rifle` - audited in 38s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Lisa_Hybrid, melee=- (dmg -), ranged=survival_rifle (dmg 18), preferRanged=True, def.attackDamage=12, hp=400 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | FAIL | 4 renderer(s) with broken material | broken visuals: character_head:null material, eye_L:null material, eye_R:null material, Helmet_inside:null material (shader missing for the HDRP pipeline or material slot empty) |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 17 collider(s) on the hitbox layer, 91 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 228deg, path 8.9m, net 0.4m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.7s (at 9.6m, weapon survival_rifle), first player damage 1.8s, attacks 1, states Wander/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 18.9 hp in 1.8s using survival_rifle |  |
| Weapons | Melee weapon damages the player | SKIP | no resolved melee weapon |  |
| Weapons | Ranged 'survival_rifle' damages the player | PASS | player hp lost 5.4 (item dmg 18), shots/projectiles 1, attack-state frames 26, attacks 1, first hit 1.0s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 400.0 -> 369.7 (-30.3); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 369.7 -> 31.8; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 3, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.5s, corpse gone 15.7s, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | WARN | no Animator/controller |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Robot

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `Robot_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 43s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Robot_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=15, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 214deg, path 4.7m, net 4.6m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.2s (at 2.2m, weapon weap2_sword), first player damage 2.8s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.8s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 3 (unarmed 0), hit events 1, sub-1hp chip 0.5, first hit 0.2s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.0 (item dmg 14), shots/projectiles 1, attack-state frames 52, attacks 1, first hit 2.5s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 472.2 (-27.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 472.2 -> 387.7; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Robot |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.6s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### The_Evil_One

Prefab `Assets/_Project/Prefabs/Combat/Enemies/The_Evil_One.prefab` - definition `The_Evil_One` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 54s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=The_Evil_One, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=True, def.attackDamage=12, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 147deg, path 9.2m, net 5.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.7s (at 9.6m, weapon sci_fi_pistol), first player damage 8.8s, attacks 6, states Wander/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 15.9 hp in 8.8s using sci_fi_pistol |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 2, hitbox-active frames 11 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.7s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; no vMeleeWeapon on the drawn/bound weapon |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 15.2 (item dmg 14), shots/projectiles 6, attack-state frames 203, attacks 6, first hit 8.0s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.65 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 471.5 (-28.5); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 471.5 -> 318.8; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.6s, corpse gone respawns, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 21 distinct clips seen in total |  |
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
