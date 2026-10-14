# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-13 22:50:56 -> 2026-10-13 22:52:28 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, UITK_Loading/loading-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-777.4, 21.2, -117.8), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 3; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1
- Unit store: 37 stored unit(s); stamps audit 21E636 game 190DB0 gear 11046A; skip-unchanged is ON (Quick mode)
- Weapon matrix: 11 weapon items equipped one at a time (spawn override) on HumanoidEnemy_Invector; plus one dual-slot override case
- Mode quick: 13 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Also (restored): InputSystem backgroundBehavior ResetAndDisableNonBackgroundDevices -> IgnoreFocus, editorInputBehaviorInPlayMode PointersAndKeyboardsRespectGameViewFocus -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was True). Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (skill_system_jetpack absent->1; skill_system_dash absent->1; skill_system_climb absent->1; restored at exit; no skill points spent, no save, no asset written). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **quick**, wall time 1m31s

**1 enemies audited - 0 FAIL, 0 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 12 weapon cases + 1 scenarios): 85 PASS, 6 FAIL, 2 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 40 change(s).

- NEW enemy|The_Evil_One|perf = PASS
- NEW enemy|The_Evil_One|melee.miss = INFO
- NEW enemy|The_Evil_One|ranged.miss = INFO
- NEW enemy|The_Evil_One|ranged.range.7 = PASS
- NEW enemy|The_Evil_One|ranged.range.12 = PASS
- NEW enemy|The_Evil_One|ranged.range.18 = INFO
- NEW enemy|The_Evil_One|ranged.range.25 = INFO
- NEW enemy|The_Evil_One|ranged.range.11 = PASS
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
- CHANGED weapon|DM_Mining_Tool|w.equip: PASS -> FAIL
- NEW weapon|DM_Mining_Tool|w.miss = INFO
- NEW weapon|sci_fi_pistol|w.miss = INFO
- NEW weapon|survival_rifle|w.miss = INFO
- NEW scenario|Regression of earlier fixes and tweaks|fix.swish = FAIL
- NEW scenario|Regression of earlier fixes and tweaks|fix.hitsound = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.hotcross = WARN
- NEW scenario|Regression of earlier fixes and tweaks|fix.hiteffects = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.loadout = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.tuning = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.patrol = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.onedrawn = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.profiles = INFO
- MISSING enemy|The_Evil_One|player.swing (was PASS)

## Timing

Wall time 92s (1.5 min), mode quick. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| The_Evil_One | enemy | 53.8 | ran |
| 2 Hander | weapon | 2.8 | ran |
| Blade Of Chaos | weapon | 2.1 | ran |
| Death Axe | weapon | 1.7 | ran |
| Spear of Fate | weapon | 3.0 | ran |
| Sword of Fear | weapon | 1.9 | ran |
| weap2_sword | weapon | 1.9 | ran |
| weap_two_handed | weapon | 2.4 | ran |
| Wood Axe | weapon | 1.9 | ran |
| DM_Mining_Tool | weapon | 3.1 | ran |
| sci_fi_pistol | weapon | 1.1 | ran |
| survival_rifle | weapon | 2.0 | ran |
| OVERRIDE both slots | weapon | 4.2 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|The_Evil_One | ranged test | 32.0 | 1019 | 31.5 | 208.4 | 0.36 | 0.00 | 13.00 |
| enemy|The_Evil_One | unaware 9s (+perf) | 9.3 | 321 | 28.9 | 366.2 | 0.33 | 0.00 | 12.02 |
| enemy|The_Evil_One | death/loot | 6.8 | 211 | 31.9 | 117.0 | 0.35 | 0.00 | 13.25 |
| weapon|DM_Mining_Tool | setup | 3.1 | 92 | 33.8 | 59.9 | 0.39 | 0.00 | 13.29 |
| weapon|Spear of Fate | setup | 3.0 | 77 | 38.5 | 216.2 | 0.48 | 0.00 | 13.87 |
| weapon|2 Hander | setup | 2.8 | 76 | 36.6 | 203.3 | 0.43 | 0.00 | 14.02 |
| weapon|weap_two_handed | setup | 2.4 | 61 | 40.3 | 197.3 | 0.46 | 0.00 | 13.88 |
| enemy|The_Evil_One | notice/chase/attack | 2.1 | 70 | 29.7 | 55.5 | 0.34 | 0.00 | 13.40 |
| weapon|Blade Of Chaos | setup | 2.1 | 57 | 36.6 | 187.5 | 0.45 | 0.00 | 13.49 |
| enemy|The_Evil_One | melee test | 2.0 | 54 | 35.2 | 87.0 | 0.61 | 0.00 | 13.78 |
| weapon|survival_rifle | setup | 2.0 | 53 | 37.8 | 55.4 | 0.43 | 0.00 | 13.96 |
| weapon|Wood Axe | setup | 1.9 | 46 | 40.7 | 207.6 | 0.53 | 0.00 | 13.93 |
| weapon|weap2_sword | setup | 1.9 | 46 | 40.7 | 213.4 | 0.48 | 0.00 | 14.10 |
| weapon|Sword of Fear | setup | 1.9 | 47 | 39.8 | 219.4 | 0.51 | 0.00 | 13.29 |
| weapon|Death Axe | setup | 1.7 | 46 | 37.8 | 196.2 | 0.60 | 0.00 | 13.17 |
| weapon|sci_fi_pistol | setup | 1.1 | 29 | 36.3 | 57.6 | 0.41 | 0.00 | 13.97 |
| enemy|The_Evil_One | spawn | 1.0 | 30 | 35.3 | 115.0 | 0.47 | 0.00 | 12.33 |
| enemy|The_Evil_One | player->enemy | 0.6 | 14 | 42.7 | 60.4 | 0.50 | 0.00 | 13.43 |
| enemy|The_Evil_One | cleanup | 0.2 | 5 | 40.6 | 65.1 | 0.37 | 0.00 | 12.24 |
| enemy|The_Evil_One | setup | 0.1 | 1 | 2359.9 | 2359.9 | 0.96 | 0.00 | 28.31 |

Whole run (ran units only): 78s over 2355 frames, avg frame 34.0 ms, physics 0.39 ms, render 0.00 ms, scripts 13.11 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 75 | FAIL | Equips on the template (spawn override) | DM_Mining_Tool | ConfigureSpawnLoadout + TrySwitchTo did not make this the active weapon: the ItemData has no equip prefab / the template has no matching Drawn_ slot for it; ItemData rangedDamage = 0 | Inspect the measured value and cause on the detail row. |
| 2 | 75 | WARN | Ranged 'DM_Mining_Tool' damages the player | DM_Mining_Tool | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 3 | 20 | FAIL | '2 Hander' swing plays a swish | 2 Hander | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 4 | 20 | FAIL | 'Sword of Fear' swing plays a swish | Sword of Fear | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 5 | 20 | FAIL | 'weap2_sword' swing plays a swish | weap2_sword | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 6 | 20 | FAIL | 'Wood Axe' swing plays a swish | Wood Axe | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 7 | 20 | FAIL | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks | swing rows still silent for: '2 Hander' swing plays a swish, 'Sword of Fear' swing plays a swish, 'weap2_sword' swing plays a swish, 'Wood Axe' swing plays a swish | Inspect the measured value and cause on the detail row. |
| 8 | -5 | WARN | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | Regression of earlier fixes and tweaks | no Hot Cross row ran (cached or scenario skipped) | Inspect the measured value and cause on the detail row. |

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| The_Evil_One | weap2_sword | sci_fi_pistol | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS | PASS |

## Weapons that deal NO damage (headline)

- none detected in this run.

## Weapon matrix (every weapon item on the template, spawn override)

12 cases, 5 FAIL. Each weapon is equipped on `HumanoidEnemy_Invector` through ConfigureSpawnLoadout (the spawner path) and made to attack the player.

| Weapon | Kind | Equips | Damages player | One drawn | Measured | Probable cause |
|---|---|---|---|---|---|---|
| 2 Hander | melee | PASS | PASS | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 13, hit events 1, sub-1hp chip 0.0, first hit 0.2s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Blade Of Chaos | melee | PASS | PASS | PASS | player hp lost 30.0 (item dmg 21), attacks 1, hitbox-active frames 20, hit events 1, sub-1hp chip 0.0, first hit 0.5s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hi... |  |
| Death Axe | melee | PASS | PASS | PASS | player hp lost 48.5 (item dmg 18), attacks 1, hitbox-active frames 16, hit events 2, sub-1hp chip 0.0, first hit 0.2s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: laye... |  |
| Spear of Fate | melee | PASS | PASS | PASS | player hp lost 36.0 (item dmg 16), attacks 1, hitbox-active frames 14, hit events 1, sub-1hp chip 0.0, first hit 1.3s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitB... |  |
| Sword of Fear | melee | PASS | PASS | PASS | player hp lost 38.0 (item dmg 15), attacks 1, hitbox-active frames 19, hit events 2, sub-1hp chip 0.4, first hit 0.2s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitB... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| weap2_sword | melee | PASS | PASS | PASS | player hp lost 52.5 (item dmg 18), attacks 1, hitbox-active frames 18, hit events 2, sub-1hp chip 0.0, first hit 0.2s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| weap_two_handed | melee | PASS | PASS | PASS | player hp lost 68.0 (item dmg 28), attacks 1, hitbox-active frames 12, hit events 1, sub-1hp chip 0.0, first hit 0.9s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38;... |  |
| Wood Axe | melee | PASS | PASS | PASS | player hp lost 62.0 (item dmg 26), attacks 1, hitbox-active frames 17, hit events 2, sub-1hp chip 0.0, first hit 0.2s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| DM_Mining_Tool | ranged | **FAIL** | warn | PASS | player hp lost 17.9 (item dmg 0), shots/projectiles 1, attack-state frames 39, attacks 1, first hit 1.3s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange... | ConfigureSpawnLoadout + TrySwitchTo did not make this the active weapon: the ItemData has no equip prefab / the template has no matching Drawn_ slot for it; ItemData rangedDamage = 0 |
| sci_fi_pistol | ranged | PASS | PASS | PASS | player hp lost 13.9 (item dmg 14), shots/projectiles 0, attack-state frames 3, attacks 1, first hit 0.0s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange... |  |
| survival_rifle | ranged | PASS | PASS | PASS | player hp lost 24.5 (item dmg 18), shots/projectiles 0, attack-state frames 28, attacks 2, first hit 1.0s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRa... |  |
| OVERRIDE both slots | dual-slot spawn override | - | - | PASS | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)): PASS ; Starts with the ranged override drawn (prefer-ranged on, start-melee off): PASS ; Switch to the melee override vi... |  |

### Weapon: 2 Hander

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item 2 Hander; drawn [Drawn_2_Hander]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | '2 Hander' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | '2 Hander' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_8:Melee Weapon Attack 1; vHitEffects on 'Drawn_2_Hander': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee '2 Hander' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 13, hit events 1, sub-1hp chip 0.0, first hit 0.2s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee '2 Hander': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 1 (100 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 38.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.4m, first attack at 1.4m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.4m), first damage 0.2s, attacks 1, attack-state frames 19, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Blade Of Chaos

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 21 |  |
| Audio | 'Blade Of Chaos' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_5:Melee Weapon Air Swing 2 |  |
| Audio | 'Blade Of Chaos' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_7:Melee Weapon Attack 4, SfxPool_8:Melee Weapon Air Swing 2; vHitEffects on 'Drawn_Blade_Of_Chaos': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 30.0 (item dmg 21), attacks 1, hitbox-active frames 20, hit events 1, sub-1hp chip 0.0, first hit 0.5s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Blade Of Chaos': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 30.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.5s, attacks 1, attack-state frames 27, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Death Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'Death Axe' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_5:neverworld |  |
| Audio | 'Death Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_5:neverworld, SfxPool_6:Melee Weapon Attack 1, SfxPool_7:Melee Weapon Air Swing 4, SfxPool_8:Melee Weapon Attack 4; vHitEffects on 'Drawn_Death_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 48.5 (item dmg 18), attacks 1, hitbox-active frames 16, hit events 2, sub-1hp chip 0.0, first hit 0.2s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Death Axe': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 2, player hp lost 48.5; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.2s, attacks 1, attack-state frames 18, clips seen: WeakAttack_SwordB, WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Spear of Fate

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 16 |  |
| Audio | 'Spear of Fate' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_8:Melee Weapon Air Swing 3 |  |
| Audio | 'Spear of Fate' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_8:Melee Weapon Air Swing 3, SfxPool_9:Melee Weapon Attack 3; vHitEffects on 'Drawn_Spear_of_Fate': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 36.0 (item dmg 16), attacks 1, hitbox-active frames 14, hit events 1, sub-1hp chip 0.0, first hit 1.3s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Spear of Fate': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 36.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.2s (at 1.5m), first damage 1.3s, attacks 1, attack-state frames 49, clips seen: WeakAttack_SwordB, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Sword of Fear

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Sword of Fear; drawn [Drawn_Sword_of_Fear]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 15 |  |
| Audio | 'Sword of Fear' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Sword of Fear' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_8:Melee Weapon Attack 1, SfxPool_9:Melee Weapon Air Swing 4, SfxPool_0:Melee Weapon Attack 3; vHitEffects on 'Drawn_Sword_of_Fear': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Sword of Fear' damages the player | PASS | player hp lost 38.0 (item dmg 15), attacks 1, hitbox-active frames 19, hit events 2, sub-1hp chip 0.4, first hit 0.2s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Sword of Fear': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 1 (100 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 2, player hp lost 38.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.2s, attacks 1, attack-state frames 20, clips seen: Attack2Hand_C, WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap2_sword

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'weap2_sword' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'weap2_sword' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_8:Melee Weapon Attack 3, SfxPool_0:Melee Weapon Air Swing 1, SfxPool_1:Melee Weapon Attack 1; vHitEffects on 'Drawn_weap2_sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 52.5 (item dmg 18), attacks 1, hitbox-active frames 18, hit events 2, sub-1hp chip 0.0, first hit 0.2s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 2, player hp lost 52.5; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.2s, attacks 1, attack-state frames 19, clips seen: WeakAttack_SwordB, WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap_two_handed

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | 'weap_two_handed' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_8:Melee Weapon Air Swing 4 |  |
| Audio | 'weap_two_handed' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_8:Melee Weapon Air Swing 4, SfxPool_9:Melee Weapon Attack 1; vHitEffects on 'Drawn_Two_Handed_Sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 68.0 (item dmg 28), attacks 1, hitbox-active frames 12, hit events 1, sub-1hp chip 0.0, first hit 0.9s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'weap_two_handed': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 68.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.9s, attacks 1, attack-state frames 35, clips seen: WeakAttack_SwordB, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Wood Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 26 |  |
| Audio | 'Wood Axe' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Wood Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_7:Melee Weapon Attack 3, SfxPool_8:Melee Weapon Air Swing 2, SfxPool_9:Melee Weapon Attack 1; vHitEffects on 'Drawn_Wood_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Wood Axe' damages the player | PASS | player hp lost 62.0 (item dmg 26), attacks 1, hitbox-active frames 17, hit events 2, sub-1hp chip 0.0, first hit 0.2s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee 'Wood Axe': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 2, player hp lost 62.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.5m, first attack at 1.5m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.2s, attacks 1, attack-state frames 19, clips seen: Attack2Hand_C, WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: DM_Mining_Tool

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | FAIL | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Wood_Axe]; item dmg 0 | ConfigureSpawnLoadout + TrySwitchTo did not make this the active weapon: the ItemData has no equip prefab / the template has no matching Drawn_ slot for it; ItemData rangedDamage = 0 |
| Weapons | Ranged 'DM_Mining_Tool' damages the player | WARN | player hp lost 17.9 (item dmg 0), shots/projectiles 1, attack-state frames 39, attacks 1, first hit 1.3s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item |
| Weapons | Ranged 'DM_Mining_Tool': hit rate vs configured accuracy | INFO | shots/projectiles 1, damage events 1 (100 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 130 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 1 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 1.2s (at 7.2m), first damage 1.3s, attacks 1, attack-state frames 39, clips seen: WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: sci_fi_pistol

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Wood_Axe]; item dmg 14 |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 13.9 (item dmg 14), shots/projectiles 0, attack-state frames 3, attacks 1, first hit 0.0s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 1 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 7.2m), first damage 0.0s, attacks 1, attack-state frames 3, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: survival_rifle

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_Wood_Axe]; item dmg 18 |  |
| Weapons | Ranged 'survival_rifle' damages the player | PASS | player hp lost 24.5 (item dmg 18), shots/projectiles 0, attack-state frames 28, attacks 2, first hit 1.0s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged 'survival_rifle': hit rate vs configured accuracy | INFO | shots/projectiles 0, damage events 1 (0 %) vs about 75 % expected from definition missRate 0.25 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 300 %); item spread survival_rifle: projectileSpread 0.9 deg, accuracy 82, close-range scale 0.15 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 0 shot(s) in the window: too few to judge the rate (need ~10) |
| Timing | Attack timing | INFO | first attack 0.0s (at 7.2m), first damage 1.0s, attacks 2, attack-state frames 28, clips seen: none matched |  |
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
| Regression of earlier fixes and tweaks | 6 | 1 | 1 | 0s |

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | FAIL | code present: True; 4 PASS / 4 FAIL / 0 warn of 8 runtime row(s) | swing rows still silent for: '2 Hander' swing plays a swish, 'Sword of Fear' swing plays a swish, 'weap2_sword' swing plays a swish, 'Wood Axe' swing plays a swish |
| Fix check | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | PASS | code present: True; 8 PASS / 0 FAIL / 0 warn of 8 runtime row(s) |  |
| Fix check | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | WARN | 0 PASS / 0 FAIL / 0 warn of 0 runtime row(s); weapon reverts recorded this run: 0 | no Hot Cross row ran (cached or scenario skipped) |
| Fix check | vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons) | PASS | code present: True; console errors across 12 weapon-matrix case(s): 0 |  |
| Fix check | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | PASS | code present: True; dual-slot override case: 6 PASS / 0 FAIL |  |
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 6 value(s) compared on 0 enemies |  |
| Fix check | No circling while unaware (patrol-anchor fix) on every enemy | PASS | 1 PASS / 0 FAIL of 1 enemies |  |
| Fix check | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | PASS | 1 PASS / 0 FAIL of 1 enemies |  |
| Fix check | Profile adjustments made by earlier audit passes | INFO | none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes |  |

## Per-enemy detail

### The_Evil_One

Prefab `Assets/_Project/Prefabs/Combat/Enemies/The_Evil_One.prefab` - definition `The_Evil_One` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 54s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=The_Evil_One, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=True, def.attackDamage=12, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 39ms, 707 components, frame avg 28.9ms max 366.2ms (321 frames), GC collections 0, managed delta 20060KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 170deg, path 9.6m, net 2.3m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.0s (at 9.5m, weapon sci_fi_pistol), first player damage 2.1s, attacks 1, states Wander/Defensive/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 15.0 hp in 2.1s using sci_fi_pistol |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 10 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 0.9s, min distance 1.20m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; no vMeleeWeapon on the drawn/bound weapon |  |
| Weapons | Melee 'weap2_sword': intentional miss rate vs configured chance | INFO | swings 1, intentional misses rolled 0 (0 %) vs configured 10 % (+-90 % at 3 sigma), weapon hit events 1, player hp lost 26.0; conditions: the roll applies only while the target is in range, miss-recovery step 0.4s follows a miss, whiff check range x0.88; the test player never blocks/parries/dodges; closest approach 1.2m, first attack at 1.2m | only 1 swing(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.4 (item dmg 14), shots/projectiles 2, attack-state frames 50, attacks 2, first hit 1.5s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.65 |  |
| Weapons | Ranged 'sci_fi_pistol': hit rate vs configured accuracy | INFO | shots/projectiles 2, damage events 1 (50 %) vs about 35 % expected from definition missRate 0.65 (enemy rolls a miss first, then applies a smaller natural spread, so the observed rate sits below that; accepted down to half of it minus 3 sigma 101 %); item spread sci_fi_pistol: projectileSpread 1.8 deg, accuracy 70, close-range scale 0.22 within 12m; the test player never blocks/dodges and stands still; closest approach 7.2m | only 2 shot(s) in the window: too few to judge the rate (need ~10) |
| Weapons | Ranged 'sci_fi_pistol' at 7.0 m | PASS | player hp lost 14.9, shots/projectiles 2, attacks 1, attack-state frames 11, states Attack, engage 12.0 |  |
| Weapons | Ranged 'sci_fi_pistol' at 12.0 m | PASS | player hp lost 15.9, shots/projectiles 4, attacks 4, attack-state frames 15, states Chase/Attack, engage 12.0 |  |
| Weapons | Ranged 'sci_fi_pistol' at 18.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states Chase, engage 12.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 25.0 m | INFO | player hp lost 0.0, shots/projectiles 0, attacks 0, attack-state frames 0, states Chase, engage 12.0 | beyond engage range (by settings) |
| Weapons | Ranged 'sci_fi_pistol' at 11.4 m | PASS | player hp lost 17.9, shots/projectiles 6, attacks 6, attack-state frames 201, states Attack, engage 12.0 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 472.7 (-27.3); 1 receiver(s) |  |
| Hit marks | Hit marks fire on damage | PASS | marks stamped 0 -> 1, body type Humanoid |  |
| Death | Loot dropped and can be looted (real loot path), then cleaned up at once | PASS | loot bag held 2 item entries, TryLootAll -> Granted, left 0, 2 granted item(s) removed again from the audit inventory; definition random loot count 0-2 |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.26s, loot bag 6.6s, corpse gone held by the loot bag (removed by the audit), dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 30 distinct clips seen in total |  |
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
