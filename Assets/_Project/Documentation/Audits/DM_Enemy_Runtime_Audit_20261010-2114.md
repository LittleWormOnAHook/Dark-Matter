# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-10 21:08:04 -> 2026-10-10 21:14:56 (aborted: play mode exited before the run completed)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity` (never saved by the audit)
- Menu/loading bypass: boot loader finished; state before bypass: GameSession not started, world frozen, timeScale 0, main menu visible, UITK_MainMenu/mainmenu-root still displayed, PlayerInput disabled (player not controllable); attempt 1: verified clean (phase=Playing, frozen=False, timeScale=1, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput=enabled, cursor=Locked); Game view screenshot at first test step: A:\Dark Matter Genesis\Temp\DMAudit_first_step.png
- Player: Player_v7 Combat Variant at (-780.0, 21.2, -122.1), hp 1000.0/1200.0
- Player armed for the audit: weap2_sword drawn (vMeleeManager.rightWeapon = Drawn_weap2_sword); in-memory only, nothing saved
- Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available
- Scene enemies deactivated for the run (runtime only): 4; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 9
- Unit store: 0 stored unit(s); stamps audit DDCA04 game 0CC347 gear 204DA0; skip-unchanged is OFF (Full mode)
- Weapon matrix: 11 weapon items equipped one at a time (spawn override) on HumanoidEnemy_Invector; plus one dual-slot override case
- Mode full: 22 unit(s) ran, 0 reused from the unit store (unchanged and previously passing)
- Excluded characters (broken, not used): Lisa_Hybrid
- Settings the audit changed (all restored at exit): Application.runInBackground False -> true -> False; stack traces for Log and Warning set to None during the run (were ScriptOnly/ScriptOnly, restored). Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.
- Settings at start: runInBackground=False; vSyncCount=1; lodBias=2; targetFrameRate=-1; qualityLevel=3 (High); qualityRenderPipeline=Genesis_HDRP_High; graphicsDefaultRenderPipeline=Genesis_HDRP_Performance; fixedDeltaTime=0.0167; maximumDeltaTime=0.1
- Settings at end: identical to the start (no drift during the run)
- Main scene file Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity: not written during the run (the audit has no SaveScene/SaveOpenScenes call)
- Mode: **full**, wall time 7m52s

**9 enemies audited - 0 FAIL, 2 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.
**Overall (enemies + 12 weapon cases + 3 scenarios): 210 PASS, 7 FAIL, 4 warn.**

## Changes since baseline

compared with baseline from 2026-10-10 20:06: 32 change(s).

- NEW enemy|Axe_Droid|perf = PASS
- NEW enemy|corrupt_patrol_android|perf = PASS
- NEW enemy|GreySpire Soldier|perf = PASS
- NEW enemy|Humadroid|perf = PASS
- CHANGED enemy|Humadroid|hitmarks: WARN -> PASS
- NEW enemy|Humanoid|perf = PASS
- NEW enemy|HumanoidEnemy_Invector|perf = PASS
- NEW enemy|Hybrid Droid|perf = PASS
- NEW enemy|Robot|perf = PASS
- CHANGED enemy|Robot|anim: INFO -> WARN
- NEW enemy|The_Evil_One|perf = PASS
- CHANGED enemy|The_Evil_One|player.swing: PASS -> WARN
- CHANGED weapon|2 Hander|audio.swing.2 Hander: PASS -> FAIL
- CHANGED weapon|2 Hander|audio.hitplayer.2 Hander: PASS -> FAIL
- CHANGED weapon|Blade Of Chaos|audio.swing.Blade Of Chaos: FAIL -> PASS
- CHANGED weapon|Death Axe|audio.swing.Death Axe: FAIL -> PASS
- CHANGED weapon|Death Axe|audio.hitplayer.Death Axe: FAIL -> PASS
- CHANGED weapon|Death Axe|w.damage: FAIL -> PASS
- CHANGED weapon|Sword of Fear|audio.swing.Sword of Fear: PASS -> FAIL
- CHANGED weapon|Sword of Fear|audio.hitplayer.Sword of Fear: FAIL -> PASS
- CHANGED weapon|weap2_sword|audio.swing.weap2_sword: PASS -> FAIL
- CHANGED weapon|DM_Mining_Tool|vis: PASS -> FAIL
- NEW scenario|Regression of earlier fixes and tweaks|fix.swish = FAIL
- NEW scenario|Regression of earlier fixes and tweaks|fix.hitsound = FAIL
- NEW scenario|Regression of earlier fixes and tweaks|fix.hotcross = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.hiteffects = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.loadout = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.tuning = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.patrol = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.onedrawn = PASS
- NEW scenario|Regression of earlier fixes and tweaks|fix.profiles = INFO
- MISSING scenario|Equip: weapon survives a teleport|console (was PASS)

## Timing

Wall time 412s (6.9 min), mode full. frame split from ProfilerRecorder markers (approximate, editor): FixedUpdate.PhysicsFixedUpdate, Camera.Render, Update.ScriptRunBehaviourUpdate, PreLateUpdate.ScriptRunBehaviourLateUpdate

| Unit | Scope | Seconds | Source |
|---|---|---|---|
| Axe_Droid | enemy | 37.0 | ran |
| corrupt_patrol_android | enemy | 38.6 | ran |
| GreySpire Soldier | enemy | 36.7 | ran |
| Humadroid | enemy | 40.6 | ran |
| Humanoid | enemy | 41.0 | ran |
| HumanoidEnemy_Invector | enemy | 39.8 | ran |
| Hybrid Droid | enemy | 41.6 | ran |
| Robot | enemy | 39.2 | ran |
| The_Evil_One | enemy | 40.3 | ran |
| 2 Hander | weapon | 2.7 | ran |
| Blade Of Chaos | weapon | 1.8 | ran |
| Death Axe | weapon | 1.7 | ran |
| Spear of Fate | weapon | 2.3 | ran |
| Sword of Fear | weapon | 1.7 | ran |
| weap2_sword | weapon | 1.8 | ran |
| weap_two_handed | weapon | 2.3 | ran |
| Wood Axe | weapon | 1.7 | ran |
| DM_Mining_Tool | weapon | 2.2 | ran |
| sci_fi_pistol | weapon | 1.4 | ran |
| survival_rifle | weapon | 1.4 | ran |
| OVERRIDE both slots | weapon | 4.2 | ran |
| Camera: faces forward after teleports, zoom in/out | scenario | 9.1 | ran |
| Equip: weapon survives a teleport | scenario | 0.0 | ran |
| Regression of earlier fixes and tweaks | scenario | 0.0 | ran |

Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):

| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |
|---|---|---|---|---|---|---|---|---|
| enemy|Hybrid Droid | death/loot | 17.6 | 581 | 30.2 | 73.2 | 0.38 | 0.00 | 14.32 |
| enemy|Humadroid | death/loot | 17.5 | 576 | 30.5 | 92.2 | 0.41 | 0.00 | 14.48 |
| enemy|corrupt_patrol_android | death/loot | 17.5 | 578 | 30.3 | 106.7 | 0.40 | 0.00 | 14.35 |
| enemy|Axe_Droid | death/loot | 17.5 | 579 | 30.2 | 106.5 | 0.40 | 0.00 | 14.42 |
| enemy|GreySpire Soldier | death/loot | 17.5 | 571 | 30.6 | 78.3 | 0.42 | 0.00 | 14.47 |
| enemy|HumanoidEnemy_Invector | death/loot | 17.5 | 573 | 30.5 | 79.2 | 0.38 | 0.00 | 14.39 |
| enemy|Humanoid | death/loot | 17.4 | 563 | 30.8 | 92.5 | 0.38 | 0.00 | 14.53 |
| enemy|The_Evil_One | death/loot | 15.4 | 481 | 32.1 | 80.4 | 0.38 | 0.00 | 15.37 |
| enemy|Robot | death/loot | 12.6 | 390 | 32.2 | 91.4 | 0.41 | 0.00 | 15.20 |
| scenario|Camera: faces forward after teleports, zoom in/out | setup | 9.4 | 308 | 30.4 | 62.0 | 0.38 | 0.00 | 13.85 |
| enemy|Humadroid | unaware 9s (+perf) | 9.0 | 290 | 31.1 | 51.9 | 0.39 | 0.00 | 14.86 |
| enemy|corrupt_patrol_android | unaware 9s (+perf) | 9.0 | 295 | 30.6 | 49.9 | 0.40 | 0.00 | 14.74 |
| enemy|Hybrid Droid | unaware 9s (+perf) | 9.0 | 293 | 30.8 | 53.1 | 0.39 | 0.00 | 14.71 |
| enemy|The_Evil_One | unaware 9s (+perf) | 9.0 | 280 | 32.2 | 61.8 | 0.41 | 0.00 | 15.77 |
| enemy|GreySpire Soldier | unaware 9s (+perf) | 9.0 | 291 | 31.0 | 54.8 | 0.40 | 0.00 | 14.72 |
| enemy|HumanoidEnemy_Invector | unaware 9s (+perf) | 9.0 | 291 | 31.0 | 51.8 | 0.40 | 0.00 | 14.65 |
| enemy|Humanoid | unaware 9s (+perf) | 9.0 | 293 | 30.8 | 53.9 | 0.39 | 0.00 | 14.67 |
| enemy|Axe_Droid | unaware 9s (+perf) | 9.0 | 293 | 30.7 | 49.1 | 0.41 | 0.00 | 14.65 |
| enemy|Robot | unaware 9s (+perf) | 9.0 | 291 | 30.9 | 54.1 | 0.40 | 0.00 | 14.69 |
| enemy|The_Evil_One | notice/chase/attack | 8.9 | 235 | 37.7 | 97.6 | 0.47 | 0.00 | 16.63 |
| scenario|Equip: weapon survives a teleport | setup | 8.0 | 165 | 32.7 | 162.4 | 0.40 | 0.00 | 14.61 |
| enemy|Robot | notice/chase/attack | 6.2 | 189 | 32.8 | 81.9 | 0.46 | 0.00 | 15.29 |
| enemy|Robot | ranged test | 4.3 | 127 | 33.8 | 58.3 | 0.43 | 0.00 | 15.52 |
| enemy|HumanoidEnemy_Invector | ranged test | 4.1 | 122 | 33.3 | 56.7 | 0.42 | 0.00 | 15.32 |
| enemy|Humanoid | ranged test | 4.0 | 121 | 33.4 | 53.8 | 0.43 | 0.00 | 15.40 |
| enemy|Hybrid Droid | ranged test | 4.0 | 122 | 32.9 | 52.3 | 0.44 | 0.00 | 15.29 |
| enemy|corrupt_patrol_android | ranged test | 4.0 | 118 | 33.4 | 49.8 | 0.44 | 0.00 | 15.42 |
| enemy|Humadroid | ranged test | 3.9 | 117 | 33.5 | 58.3 | 0.46 | 0.00 | 15.35 |
| enemy|Axe_Droid | player->enemy | 3.8 | 95 | 40.5 | 87.9 | 0.60 | 0.00 | 16.42 |
| enemy|Humanoid | player->enemy | 3.8 | 101 | 37.8 | 57.0 | 0.53 | 0.00 | 15.84 |

Whole run (ran units only): 395s over 12052 frames, avg frame 32.8 ms, physics 0.44 ms, render 0.00 ms, scripts 15.00 ms per frame.

## Findings ranked by player impact

Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.

| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |
|---|---|---|---|---|---|---|
| 1 | 85 | FAIL | Exactly one weapon drawn | DM_Mining_Tool | second weapon visual left active, or the equipped weapon has no drawn visual | Let EnemyInvectorLoadoutBridge be the only weapon authority; hide every Drawn_/Holstered_ slot except the resolved ones. |
| 2 | 75 | WARN | Ranged 'DM_Mining_Tool' damages the player | DM_Mining_Tool | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item | Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix. |
| 3 | 20 | FAIL | '2 Hander' swing plays a swish | 2 Hander | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 4 | 20 | FAIL | '2 Hander' hitting the player plays a hit sound | 2 Hander | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it | Inspect the measured value and cause on the detail row. |
| 5 | 20 | FAIL | 'Sword of Fear' swing plays a swish | Sword of Fear | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 6 | 20 | FAIL | 'weap2_sword' swing plays a swish | weap2_sword | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event | Inspect the measured value and cause on the detail row. |
| 7 | 20 | FAIL | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | Regression of earlier fixes and tweaks | swing rows still silent for: '2 Hander' swing plays a swish, 'Sword of Fear' swing plays a swish, 'weap2_sword' swing plays a swish | Inspect the measured value and cause on the detail row. |
| 8 | 20 | FAIL | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | Regression of earlier fixes and tweaks | still silent for: '2 Hander' hitting the player plays a hit sound | Inspect the measured value and cause on the detail row. |
| 9 | -5 | WARN | Animation clip timings | Robot |  | Inspect the measured value and cause on the detail row. |
| 10 | -5 | WARN | Player melee swing via injected Mouse input (best effort) | The_Evil_One | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes | Inspect the measured value and cause on the detail row. |
| 11 | -5 | WARN | Camera zooms in and out | Camera: faces forward after teleports, zoom in/out | injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state | Inspect the measured value and cause on the detail row. |

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

12 cases, 5 FAIL. Each weapon is equipped on `HumanoidEnemy_Invector` through ConfigureSpawnLoadout (the spawner path) and made to attack the player.

| Weapon | Kind | Equips | Damages player | One drawn | Measured | Probable cause |
|---|---|---|---|---|---|---|
| 2 Hander | melee | PASS | PASS | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 15, hit events 1, sub-1hp chip 0.5, first hit 0.3s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event / weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Blade Of Chaos | melee | PASS | PASS | PASS | player hp lost 30.0 (item dmg 21), attacks 1, hitbox-active frames 11, hit events 1, sub-1hp chip 0.4, first hit 0.3s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hi... |  |
| Death Axe | melee | PASS | PASS | PASS | player hp lost 48.1 (item dmg 18), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.4, first hit 0.3s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: laye... |  |
| Spear of Fate | melee | PASS | PASS | PASS | player hp lost 36.0 (item dmg 16), attacks 1, hitbox-active frames 11, hit events 1, sub-1hp chip 0.0, first hit 0.9s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitB... |  |
| Sword of Fear | melee | PASS | PASS | PASS | player hp lost 38.5 (item dmg 15), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.0, first hit 0.3s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitB... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| weap2_sword | melee | PASS | PASS | PASS | player hp lost 52.5 (item dmg 18), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.0, first hit 0.3s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: ... | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| weap_two_handed | melee | PASS | PASS | PASS | player hp lost 68.0 (item dmg 28), attacks 1, hitbox-active frames 10, hit events 1, sub-1hp chip 0.4, first hit 0.9s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38;... |  |
| Wood Axe | melee | PASS | PASS | PASS | player hp lost 62.1 (item dmg 26), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.4, first hit 0.3s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer ... |  |
| DM_Mining_Tool | ranged | PASS | warn | **FAIL** | player hp lost 13.9 (item dmg 0), shots/projectiles 1, attack-state frames 32, attacks 1, first hit 1.2s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCoo... | second weapon visual left active, or the equipped weapon has no drawn visual |
| sci_fi_pistol | ranged | PASS | PASS | PASS | player hp lost 14.0 (item dmg 14), shots/projectiles 0, attack-state frames 10, attacks 1, first hit 0.4s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRang... |  |
| survival_rifle | ranged | PASS | PASS | PASS | player hp lost 19.2 (item dmg 18), shots/projectiles 1, attack-state frames 9, attacks 1, first hit 0.4s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRan... |  |
| OVERRIDE both slots | dual-slot spawn override | - | - | PASS | Override 'Wood Axe' + 'survival_rifle' replaces the definition (weap2_sword (Project.Data.ItemData)/sci_fi_pistol (Project.Data.ItemData)): PASS ; Starts with the ranged override drawn (prefer-ranged on, start-melee off): PASS ; Switch to the melee override vi... |  |

### Weapon: 2 Hander

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item 2 Hander; drawn [Drawn_2_Hander]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | '2 Hander' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | '2 Hander' hitting the player plays a hit sound | FAIL | new audio at impact: none; vHitEffects on 'Drawn_2_Hander': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 | weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it |
| Weapons | Melee '2 Hander' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 15, hit events 1, sub-1hp chip 0.5, first hit 0.3s, active item 2 Hander; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_2_Hander hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.3m), first damage 0.3s, attacks 1, attack-state frames 20, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Blade Of Chaos

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Blade Of Chaos; drawn [Drawn_Blade_Of_Chaos]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 21 |  |
| Audio | 'Blade Of Chaos' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_9:QP01 0005 Wind howling |  |
| Audio | 'Blade Of Chaos' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_0:Melee Weapon Attack 4; vHitEffects on 'Drawn_Blade_Of_Chaos': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Blade Of Chaos' damages the player | PASS | player hp lost 30.0 (item dmg 21), attacks 1, hitbox-active frames 11, hit events 1, sub-1hp chip 0.4, first hit 0.3s, active item Blade Of Chaos; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Blade_Of_Chaos hitBoxes=1, vDamage=30; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 20, clips seen: WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Death Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'Death Axe' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_6:Melee Weapon Air Swing 3 |  |
| Audio | 'Death Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_7:Melee Weapon Attack 4, SfxPool_8:QP01 0005 Wind howling, SfxPool_0:Melee Weapon Attack 3; vHitEffects on 'Drawn_Death_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 48.1 (item dmg 18), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.4, first hit 0.3s, active item Death Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 19, clips seen: WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Spear of Fate

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 16 |  |
| Audio | 'Spear of Fate' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_6:QP01 0005 Wind howling |  |
| Audio | 'Spear of Fate' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_7:Melee Weapon Air Swing 2; vHitEffects on 'Drawn_Spear_of_Fate': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 36.0 (item dmg 16), attacks 1, hitbox-active frames 11, hit events 1, sub-1hp chip 0.0, first hit 0.9s, active item Spear of Fate; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.9s, attacks 1, attack-state frames 32, clips seen: WeakAttack_SwordB, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Sword of Fear

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Sword of Fear; drawn [Drawn_Sword_of_Fear]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 15 |  |
| Audio | 'Sword of Fear' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'Sword of Fear' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_7:Melee Weapon Air Swing 2, SfxPool_8:Melee Weapon Attack 3; vHitEffects on 'Drawn_Sword_of_Fear': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Sword of Fear' damages the player | PASS | player hp lost 38.5 (item dmg 15), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.0, first hit 0.3s, active item Sword of Fear; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Sword_of_Fear hitBoxes=1, vDamage=19; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 16, clips seen: Attack2Hand_C, WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap2_sword

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 18 |  |
| Audio | 'weap2_sword' swing plays a swish | FAIL | new audio between attack start and impact: none | no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event |
| Audio | 'weap2_sword' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_5:Melee Weapon Attack 1, SfxPool_6:Melee Weapon Air Swing 3, SfxPool_7:Melee Weapon Attack 4; vHitEffects on 'Drawn_weap2_sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 52.5 (item dmg 18), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.0, first hit 0.3s, active item weap2_sword; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 17, clips seen: WeakAttack_SwordB, WeakAttack_SwordA |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: weap_two_handed

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 28 |  |
| Audio | 'weap_two_handed' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_4:Melee Weapon Air Swing 1 |  |
| Audio | 'weap_two_handed' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_4:Melee Weapon Air Swing 1, SfxPool_5:Melee Weapon Attack 4; vHitEffects on 'Drawn_Two_Handed_Sword': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 68.0 (item dmg 28), attacks 1, hitbox-active frames 10, hit events 1, sub-1hp chip 0.4, first hit 0.9s, active item weap_two_handed; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.0s (at 1.5m), first damage 0.9s, attacks 1, attack-state frames 30, clips seen: WeakAttack_SwordB, Attack2Hand_C |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: Wood Axe

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item Wood Axe; drawn [Drawn_Wood_Axe]; holstered [Holstered_Sci_Fi_Pistol]; item dmg 26 |  |
| Audio | 'Wood Axe' swing plays a swish | PASS | new audio between attack start and impact: SfxPool_1:Melee Weapon Air Swing 4 |  |
| Audio | 'Wood Axe' hitting the player plays a hit sound | PASS | new audio at impact: SfxPool_2:Melee Weapon Attack 3, SfxPool_4:Melee Weapon Air Swing 2, SfxPool_5:Melee Weapon Attack 4; vHitEffects on 'Drawn_Wood_Axe': audioSource prefab set, hitSounds 3, recoilSounds 3, defSounds 3 |  |
| Weapons | Melee 'Wood Axe' damages the player | PASS | player hp lost 62.1 (item dmg 26), attacks 1, hitbox-active frames 15, hit events 2, sub-1hp chip 0.4, first hit 0.3s, active item Wood Axe; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Wood_Axe hitBoxes=1, vDamage=31; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Timing | Attack timing | INFO | first attack 0.1s (at 1.5m), first damage 0.3s, attacks 1, attack-state frames 17, clips seen: Attack2Hand_C, WeakAttack_SwordA, WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: DM_Mining_Tool

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item DM_Mining_Tool; drawn []; holstered []; item dmg 0 | ItemData rangedDamage = 0 |
| Weapons | Ranged 'DM_Mining_Tool' damages the player | WARN | player hp lost 13.9 (item dmg 0), shots/projectiles 1, attack-state frames 32, attacks 1, first hit 1.2s, range 7m (engage 12), active item DM_Mining_Tool; vShooterManager present, rightWeapon=null; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 | damage was dealt although the item's damage is 0: it comes from a fallback (definition/ammo/projectile defaults), not from this item |
| Timing | Attack timing | INFO | first attack 1.1s (at 7.2m), first damage 1.2s, attacks 1, attack-state frames 32, clips seen: WeakAttack_SwordB |  |
| Weapons | Exactly one weapon drawn | FAIL | max drawn 0: ; stray renderers  | second weapon visual left active, or the equipped weapon has no drawn visual |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: sci_fi_pistol

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Wood_Axe]; item dmg 14 |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.0 (item dmg 14), shots/projectiles 0, attack-state frames 10, attacks 1, first hit 0.4s, range 7m (engage 12), active item sci_fi_pistol; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Timing | Attack timing | INFO | first attack 0.3s (at 7.2m), first damage 0.4s, attacks 1, attack-state frames 10, clips seen: none matched |  |
| Weapons | Exactly one weapon drawn | PASS | max drawn 1, max holstered 1 |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### Weapon: survival_rifle

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Weapons | Equips on the template (spawn override) | PASS | active item survival_rifle; drawn [Drawn_Survival_Rifle]; holstered [Holstered_Wood_Axe]; item dmg 18 |  |
| Weapons | Ranged 'survival_rifle' damages the player | PASS | player hp lost 19.2 (item dmg 18), shots/projectiles 1, attack-state frames 9, attacks 1, first hit 0.4s, range 7m (engage 12), active item survival_rifle; vShooterManager present, rightWeapon=Drawn_Survival_Rifle; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Timing | Attack timing | INFO | first attack 0.3s (at 7.2m), first damage 0.4s, attacks 1, attack-state frames 9, clips seen: none matched |  |
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
| Equip: weapon survives a teleport | 1 | 0 | 0 | 0s |
| Regression of earlier fixes and tweaks | 6 | 2 | 0 | 0s |

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

### Scenario: Regression of earlier fixes and tweaks

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Fix check | vMeleeManager swing swish (PlayWeaponSwing on attack-window open) | FAIL | code present: True; 5 PASS / 3 FAIL / 0 warn of 8 runtime row(s) | swing rows still silent for: '2 Hander' swing plays a swish, 'Sword of Fear' swing plays a swish, 'weap2_sword' swing plays a swish |
| Fix check | Enemy melee hit on the player plays a hit sound (vMeleeManager.OnDamageHit -> CombatHitAudio) | FAIL | code present: True; 7 PASS / 1 FAIL / 0 warn of 8 runtime row(s) | still silent for: '2 Hander' hitting the player plays a hit sound |
| Fix check | Hot Cross weapon focus stays in sync with the equipped weapon (no revert to the first weapon) | PASS | 1 PASS / 0 FAIL / 0 warn of 1 runtime row(s); weapon reverts recorded this run: 0 |  |
| Fix check | vHitEffects null-safe AddListener (no NullReferenceException on runtime weapons) | PASS | code present: True; console errors across 12 weapon-matrix case(s): 0 |  |
| Fix check | EnemyInvectorLoadoutBridge: an override equal to the definition's item does not stick | PASS | code present: True; dual-slot override case: 6 PASS / 0 FAIL |  |
| Fix check | Template tuning values (AI standoff/stamina, ragdoll stagger/reaction, CombatPoise) still match on the enemies | PASS | 48 value(s) compared on 8 enemies |  |
| Fix check | No circling while unaware (patrol-anchor fix) on every enemy | PASS | 9 PASS / 0 FAIL of 9 enemies |  |
| Fix check | Exactly one weapon drawn on every enemy (loadout bridge melee rebind / hide-unresolved) | PASS | 9 PASS / 0 FAIL of 9 enemies |  |
| Fix check | Profile adjustments made by earlier audit passes | INFO | none: no brain / engagement / hit-mark / ammo profile or definition value was changed by the audit passes |  |

## Per-enemy detail

### Axe_Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Axe_Droid.prefab` - definition `Axe_Droid` - melee `Spear of Fate` / ranged `-` - audited in 37s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Axe_Droid, melee=Spear of Fate (dmg 16), ranged=- (dmg -), preferRanged=False, def.attackDamage=12, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 32ms, 794 components, frame avg 30.7ms max 49.1ms (293 frames), GC collections 0, managed delta 12804KB |  |
| AI | No circling while unaware (player 50m away, 9s) | PASS | yaw turned 119deg, path 7.2m, net 4.5m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.7s (at 2.1m, weapon Spear of Fate), first player damage 2.2s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 20.0 hp in 2.2s using Spear of Fate |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 20.3 (item dmg 16), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 4.0, first hit 1.9s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 225.5 (-24.5); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 225.5 -> 114.9; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.6s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 11 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/corrupt_patrol_android.prefab` - definition `corrupt_patrol_android` - melee `Death Axe` / ranged `sci_fi_pistol` - audited in 39s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=corrupt_patrol_android, melee=Death Axe (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=350 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 28ms, 736 components, frame avg 30.6ms max 49.9ms (295 frames), GC collections 0, managed delta 9600KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 79deg, path 9.6m, net 7.8m, states Wander |  |
| AI | Notice / chase / attack timing (player 5m in front) | PASS | notice 0.2s, chase 0.2s, first attack 0.8s (at 2.2m, weapon Death Axe), first player damage 1.4s, attacks 1, states Wander/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 24.0 hp in 1.4s using Death Axe |  |
| Weapons | Melee 'Death Axe' damages the player | PASS | player hp lost 24.0 (item dmg 18), attacks 1, hitbox-active frames 7 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.0s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Death_Axe hitBoxes=1, vDamage=24; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.1 (item dmg 14), shots/projectiles 0, attack-state frames 49, attacks 1, first hit 2.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_Death_Axe] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Death Axe; drawn [Drawn_Death_Axe]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 350.0 -> 322.9 (-27.1); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 322.9 -> 154.2; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.6s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 1 clip(s) observed playing; 16 distinct clips seen in total |  |
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

### GreySpire Soldier

Prefab `Assets/_Project/Prefabs/Combat/Enemies/GreySpire Soldier.prefab` - definition `GreySpire Soldier` - melee `weap_two_handed` / ranged `-` - audited in 37s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=GreySpire Soldier, melee=weap_two_handed (dmg 28), ranged=- (dmg -), preferRanged=False, def.attackDamage=8, hp=250 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 28ms, 797 components, frame avg 31.0ms max 54.8ms (291 frames), GC collections 0, managed delta 7880KB |  |
| AI | No circling while unaware (player 55m away, 9s) | PASS | yaw turned 138deg, path 8.2m, net 3.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 8m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.9s (at 2.2m, weapon weap_two_handed), first player damage 2.4s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 38.0 hp in 2.4s using weap_two_handed |  |
| Weapons | Melee 'weap_two_handed' damages the player | PASS | player hp lost 38.0 (item dmg 28), attacks 1, hitbox-active frames 9 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.5s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Two_Handed_Sword hitBoxes=1, vDamage=38; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap_two_handed; drawn [Drawn_Two_Handed_Sword]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 229.2 (-20.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 229.2 -> 147.8; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.6s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### Humadroid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humadroid.prefab` - definition `Humadroid` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 41s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humadroid, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=200 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 92 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 26ms, 787 components, frame avg 31.1ms max 51.9ms (290 frames), GC collections 0, managed delta 12212KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 167deg, path 11.8m, net 5.9m, states Patrol |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.0s (at 2.1m, weapon weap2_sword), first player damage 2.6s, attacks 1, states Patrol/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.6s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 9 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.8s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.3 (item dmg 14), shots/projectiles 0, attack-state frames 51, attacks 1, first hit 2.7s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 200.0 -> 177.0 (-23.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 177.0 -> 75.3; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.25s, loot bag 6.6s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 1 clip(s) observed playing; 18 distinct clips seen in total |  |
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

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Humanoid.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 41s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 37ms, 696 components, frame avg 30.8ms max 53.9ms (293 frames), GC collections 0, managed delta 13144KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 98deg, path 6.5m, net 5.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 1.9s (at 2.2m, weapon weap2_sword), first player damage 2.5s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.5s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 10 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.8s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 14.3 (item dmg 14), shots/projectiles 1, attack-state frames 51, attacks 1, first hit 2.8s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 279.2 (-20.8); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 279.2 -> 194.3; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.4s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 19 distinct clips seen in total |  |
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

### HumanoidEnemy_Invector

Prefab `Assets/_Project/Prefabs/Combat/Enemies/HumanoidEnemy_Invector.prefab` - definition `Humanoid_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 40s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Humanoid_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 33ms, 722 components, frame avg 31.0ms max 51.8ms (291 frames), GC collections 0, managed delta 5500KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 32deg, path 5.5m, net 5.5m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.2s, chase 0.2s, first attack 2.0s (at 2.3m, weapon weap2_sword), first player damage 2.5s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 2.5s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 8 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.0s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 13.9 (item dmg 14), shots/projectiles 1, attack-state frames 50, attacks 1, first hit 2.9s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 271.0 (-29.0); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 271.0 -> 29.2; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Humanoid |  |
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
| stagger/hit | Hit Big Front * | 1.17 |
| stagger/hit | Hit Big Left | 1.38 |
| stagger/hit | Hit Big Right | 1.42 |
| stagger/hit | Hit Small Back | 0.97 |
| stagger/hit | Hit Small Front | 0.93 |
| stagger/hit | Hit Small Left | 0.93 |
| stagger/hit | Hit Small Right | 0.93 |

### Hybrid Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Hybrid Droid.prefab` - definition `FRED_Android` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 42s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=FRED_Android, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=12, hp=300 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 33ms, 696 components, frame avg 30.8ms max 53.1ms (293 frames), GC collections 0, managed delta 9080KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 137deg, path 9.4m, net 3.6m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.9s (at 2.2m, weapon weap2_sword), first player damage 3.5s, attacks 1, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 26.0 hp in 3.5s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 11 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.8s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.1 (item dmg 14), shots/projectiles 1, attack-state frames 50, attacks 1, first hit 2.8s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 300.0 -> 273.6 (-26.4); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 273.6 -> 173.4; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.27s, loot bag 6.6s, corpse gone not within window, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
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

### Robot

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Robot.prefab` - definition `Robot_Enemy` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 39s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Robot_Enemy, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=False, def.attackDamage=15, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 35ms, 698 components, frame avg 30.9ms max 54.1ms (291 frames), GC collections 0, managed delta 12232KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 96deg, path 5.6m, net 4.9m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 2.5s (at 2.2m, weapon weap2_sword), first player damage 6.2s, attacks 2, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 47.0 hp in 6.2s using weap2_sword |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 10 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.0s, min distance 1.50m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_weap2_sword hitBoxes=1, vDamage=26; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 16.9 (item dmg 14), shots/projectiles 1, attack-state frames 49, attacks 1, first hit 3.1s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Chase/Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.25 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 479.1 (-20.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 479.1 -> 393.7; player rightWeapon=Drawn_weap2_sword |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Robot |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.29s, loot bag 6.4s, corpse gone 12.5s, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | WARN | no Animator/controller |  |
| Console | Console errors/warnings | PASS | 0 errors, 0 warnings |  |

### The_Evil_One

Prefab `Assets/_Project/Prefabs/Combat/Enemies/The_Evil_One.prefab` - definition `The_Evil_One` - melee `weap2_sword` / ranged `sci_fi_pistol` - audited in 40s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=The_Evil_One, melee=weap2_sword (dmg 18), ranged=sci_fi_pistol (dmg 14), preferRanged=True, def.attackDamage=12, hp=500 |  |
| Silent | No missing scripts on the spawned enemy | PASS | 0 missing MonoBehaviour reference(s) |  |
| Silent | No error-pink / null materials | PASS | 0 renderer(s) with broken material |  |
| Silent | Animator controller + humanoid avatar valid | PASS | ok |  |
| Silent | Body hitbox rig present (layer 26) | PASS | 18 collider(s) on the hitbox layer, 77 colliders total |  |
| Performance | Spawn cost / frame time / GC (editor, 9s unaware, player far away) | PASS | spawn 36ms, 707 components, frame avg 32.2ms max 61.8ms (280 frames), GC collections 0, managed delta 13148KB |  |
| AI | No circling while unaware (player 45m away, 9s) | PASS | yaw turned 126deg, path 9.6m, net 7.1m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.1s, chase 0.1s, first attack 1.6s (at 9.6m, weapon sci_fi_pistol), first player damage 8.8s, attacks 6, states Wander/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 13.9 hp in 8.8s using sci_fi_pistol |  |
| Weapons | Melee 'weap2_sword' damages the player | PASS | player hp lost 26.0 (item dmg 18), attacks 1, hitbox-active frames 10 (unarmed 0), hit events 1, sub-1hp chip 0.0, first hit 1.0s, min distance 1.16m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; no vMeleeWeapon on the drawn/bound weapon |  |
| Weapons | Melee drawn state | INFO | active item weap2_sword; drawn [Drawn_weap2_sword]; holstered [Holstered_Sci_Fi_Pistol] |  |
| Weapons | Ranged 'sci_fi_pistol' damages the player | PASS | player hp lost 17.3 (item dmg 14), shots/projectiles 1, attack-state frames 12, attacks 1, first hit 0.4s, range 7m (engage 12), target 'Player_v7 Combat Variant', states Attack; vShooterManager present, rightWeapon=Drawn_Sci_Fi_Pistol; ProjectileBridge present; def rangedEngageRange=12, rangedCooldown=1.2, missRate=0.65 |  |
| Weapons | Ranged drawn state | INFO | active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword] |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item sci_fi_pistol; drawn [Drawn_Sci_Fi_Pistol]; holstered [Holstered_weap2_sword]; max drawn 1, max holstered 1 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 500.0 -> 468.8 (-31.2); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | WARN | 3 left-click attacks at 1.4m: enemy hp 468.8 -> 468.8; player rightWeapon=Drawn_weap2_sword | inconclusive: injected input needs the Game view focused and a melee weapon drawn on the player; use the manual checklist if the receiver path above passes |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 1, body type Humanoid |  |
| Death | Death, loot bag, cleanup | PASS | dead yes in 0.28s, loot bag 6.6s, corpse gone respawns, dropped weapon none; corpse is held until the loot bag is looted/dissolves (by design) |  |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 2 clip(s) observed playing; 22 distinct clips seen in total |  |
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
