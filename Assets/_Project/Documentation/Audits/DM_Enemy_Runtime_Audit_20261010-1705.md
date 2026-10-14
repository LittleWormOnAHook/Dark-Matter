# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-10 17:03:53 -> 2026-10-10 17:05:59 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-780.0, 21.2, -122.1), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Scene enemies deactivated for the run (runtime only): 4; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Weapon matrix: 11 weapon items equipped one at a time (spawn override) on HumanoidEnemy_Invector; plus one dual-slot override case

**1 enemies audited - 0 FAIL, 1 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Axe_Droid | Spear of Fate | - | PASS | PASS | PASS | PASS | PASS | n/a | PASS | warn | PASS | PASS |

## Weapons that deal NO damage (headline)

- none detected in this run.

## Weapon matrix (every weapon item on the template, spawn override)

12 cases, 9 FAIL. Each weapon is equipped on `HumanoidEnemy_Invector` through ConfigureSpawnLoadout (the spawner path) and made to attack the player.

| Weapon | Kind | Equips | Damages player | One drawn | Measured | Probable cause |
|---|---|---|---|---|---|---|
| 2 Hander | melee | PASS | PASS | PASS | player hp lost 38.3 (item dmg 28), attacks 2, hitbox-active frames 7, hit events 1, sub-1hp chip 3.5, first hit 1.7s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer I... | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |
| Blade Of Chaos | melee | PASS | PASS | PASS | player hp lost 1.1 (item dmg 21), attacks 1, hitbox-active frames 7, hit events 0, sub-1hp chip 0.0, first hit 0.4s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitB... | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |
| Death Axe | melee | PASS | PASS | PASS | player hp lost 43.3 (item dmg 18), attacks 1, hitbox-active frames 2, hit events 1, sub-1hp chip 3.8, first hit 1.9s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer... | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |
| Spear of Fate | melee | PASS | PASS | PASS | player hp lost 36.1 (item dmg 16), attacks 1, hitbox-active frames 2, hit events 1, sub-1hp chip 2.9, first hit 1.3s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBo... | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |
| Sword of Fear | melee | PASS | PASS | PASS | player hp lost 19.0 (item dmg 15), attacks 1, hitbox-active frames 6, hit events 1, sub-1hp chip 3.4, first hit 1.5s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBo... | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |
| weap2_sword | melee | PASS | PASS | PASS | player hp lost 26.6 (item dmg 18), attacks 2, hitbox-active frames 9, hit events 1, sub-1hp chip 3.8, first hit 1.9s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: l... |  |
| weap_two_handed | melee | PASS | PASS | PASS | player hp lost 68.2 (item dmg 28), attacks 2, hitbox-active frames 2, hit events 1, sub-1hp chip 6.8, first hit 3.3s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; ... | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |
| Wood Axe | melee | PASS | PASS | PASS | player hp lost 31.0 (item dmg 26), attacks 1, hitbox-active frames 1, hit events 1, sub-1hp chip 0.0, first hit 0.0s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer I... | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |
| DM_Mining_Tool | ranged | PASS | PASS | PASS | player hp lost 14.9 (item dmg 0), shots/projectiles 1, attack-state frames 22, attacks 1, first hit 0.8s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCoo... |  |
| sci_fi_pistol | ranged | PASS | PASS | PASS | player hp lost 14.1 (item dmg 14), shots/projectiles 2, attack-state frames 61, attacks 2, first hit 2.3s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRang... |  |
| survival_rifle | ranged | PASS | PASS | PASS | player hp lost 16.3 (item dmg 18), shots/projectiles 2, attack-state frames 57, attacks 2, first hit 2.2s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRa... |  |
| OVERRIDE both slots | dual-slot spawn override | - | - | PASS | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)): PASS ; Starts with the ranged override drawn (prefer-ranged on, start-melee off): PASS ; Switch to the melee override vi... | equal-to-definition items kept as overrides (would be flagged as mismatches by the Fix check) / [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |

### Weapon: 2 Hander

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item 2 Hander; drawn [Drawn_2_Hander]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Weapons | Melee '2 Hander' damages the player | PASS | player hp lost 38.3 (item dmg 28), attacks 2, hitbox-active frames 7, hit events 1, sub-1hp chip 3.5, first hit 1.7s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.3m), first damage 1.7s, attacks 2, attack-state frames 50, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | FAIL | 1 error(s), 0 warning(s) | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |

- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21)`

### Weapon: Blade Of Chaos

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 21 |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 1.1 (item dmg 21), attacks 1, hitbox-active frames 7, hit events 0, sub-1hp chip 0.0, first hit 0.4s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.4s, attacks 1, attack-state frames 8, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | FAIL | 1 error(s), 0 warning(s) | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |

- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21)`

### Weapon: Death Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 43.3 (item dmg 18), attacks 1, hitbox-active frames 2, hit events 1, sub-1hp chip 3.8, first hit 1.9s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.7s (at 1.4m), first damage 1.9s, attacks 1, attack-state frames 47, clips seen: WeakAttack_SwordA, WeakAttack_SwordB, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | FAIL | 1 error(s), 0 warning(s) | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |

- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21)`

### Weapon: Spear of Fate

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 16 |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 36.1 (item dmg 16), attacks 1, hitbox-active frames 2, hit events 1, sub-1hp chip 2.9, first hit 1.3s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.2s (at 1.5m), first damage 1.3s, attacks 1, attack-state frames 34, clips seen: WeakAttack_SwordA, WeakAttack_SwordB, WeakAttack_SwordC, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | FAIL | 1 error(s), 0 warning(s) | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |

- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21)`

### Weapon: Sword of Fear

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Sword of Fear; drawn [Drawn_Sword_of_Fear]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 15 |  |
| Weapons | Melee 'Sword of Fear' damages the player | PASS | player hp lost 19.0 (item dmg 15), attacks 1, hitbox-active frames 6, hit events 1, sub-1hp chip 3.4, first hit 1.5s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 1.0s (at 1.2m), first damage 1.5s, attacks 1, attack-state frames 40, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | FAIL | 1 error(s), 0 warning(s) | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |

- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21)`

### Weapon: weap2_sword

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.6 (item dmg 18), attacks 2, hitbox-active frames 9, hit events 1, sub-1hp chip 3.8, first hit 1.9s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 1.9s, attacks 2, attack-state frames 56, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap_two_handed

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 68.2 (item dmg 28), attacks 2, hitbox-active frames 2, hit events 1, sub-1hp chip 6.8, first hit 3.3s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 3.3s, attacks 2, attack-state frames 93, clips seen: WeakAttack_SwordA, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | FAIL | 1 error(s), 0 warning(s) | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |

- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21)`

### Weapon: Wood Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 26 |  |
| Weapons | Melee 'Wood Axe' damages the player | PASS | player hp lost 31.0 (item dmg 26), attacks 1, hitbox-active frames 1, hit events 1, sub-1hp chip 0.0, first hit 0.0s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.0s, attacks 1, attack-state frames 1, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | FAIL | 1 error(s), 0 warning(s) | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |

- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21)`

### Weapon: DM_Mining_Tool

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item DM_Mining_Tool; drawn []; holstered []; item dmg 0 | ItemData rangedDamage = 0 |
| Weapons | Ranged 'DM_Mining_Tool' damages the player | PASS | player hp lost 14.9 (item dmg 0), shots/projectiles 1, attack-state frames 22, attacks 1, first hit 0.8s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Timing | Attack timing | INFO | first attack 0.7s (at 7.2m), first damage 0.8s, attacks 1, attack-state frames 22, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: sci_fi_pistol

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; item dmg 14 |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.1 (item dmg 14), shots/projectiles 2, attack-state frames 61, attacks 2, first hit 2.3s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Timing | Attack timing | INFO | first attack 0.8s (at 6.8m), first damage 2.3s, attacks 2, attack-state frames 61, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: survival_rifle

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_weap2_sword]; item dmg 18 |  |
| Weapons | Ranged 'survival_rifle' damages the player | PASS | player hp lost 16.3 (item dmg 18), shots/projectiles 2, attack-state frames 57, attacks 2, first hit 2.2s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Timing | Attack timing | INFO | first attack 0.7s (at 7.2m), first damage 2.2s, attacks 2, attack-state frames 57, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: OVERRIDE both slots

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)) | PASS | resolved melee=Wood Axe (Project.Data.ItemData), ranged=survival_rifle (Project.Data.ItemData), active=survival_rifle (Project.Data.ItemData); active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_Wood_Axe] |  |
| Weapons | Starts with the ranged override drawn (prefer-ranged on, start-melee off) | PASS | active=survival_rifle (Project.Data.ItemData) |  |
| Weapons | Switch to the melee override via the single switch call | PASS | active=Wood Axe (Project.Data.ItemData); active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Survival_Rifle] |  |
| Weapons | Spawn config equal to the definition falls back to the definition | FAIL | resolved melee=Wood Axe (Project.Data.ItemData), ranged=survival_rifle (Project.Data.ItemData) | equal-to-definition items kept as overrides (would be flagged as mismatches by the Fix check) |
| Weapons | Exactly one weapon drawn through the whole case | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | FAIL | 1 error(s), 0 warning(s) | [Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21) |

- `[Exception] NullReferenceException: Object reference not set to an instance of an object @ Invector.vMelee.vHitEffects.Start () (at Assets/Invector-3rdPersonController/Melee Combat/Scripts/MeleeWeapon/vHitEffects.cs:21)`

## Per-enemy detail

### Axe_Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Axe_Droid.prefab` - definition `Axe_Droid` - melee `Spear of Fate` / ranged `-` - audited in 42s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Axe_Droid, melee=Spear of Fate (dmg 16), ranged=- (dmg -), preferRanged=False, def.attackDamage=12, hp=250 |  |
| AI | No circling while unaware (player 50m away, 9s) | PASS | yaw turned 152deg, path 10.8m, net 4.4m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 2.4s (at 2.2m, weapon Spear of Fate), first player damage 2.9s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 20.0 hp in 2.9s using Spear of Fate |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 20.0 (item dmg 16), attacks 1, hitbox-active frames 9 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 168.2 (-81.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 168.2 -> 0.0; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | WARN | active marks 0 -> 0, body type Android | no mark after receiver hits (marks need the hitbox collider + body-type profile; check DMEnemyHitboxRig layer 26 and the hit-mark profile) |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 5.8s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 10 distinct clips seen in total |  |
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
