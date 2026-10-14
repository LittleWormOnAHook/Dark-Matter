# Dark Matter Genesis - Enemy Runtime Audit

- Run: 2026-10-10 16:23:17 -> 2026-10-10 16:24:35 (complete)
- Unity 6000.4.11f1, scene `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity` (never saved by the audit)
- Menu bypass: world already running (no menu bypass needed)
- Player: Player_v7 Combat Variant at (-780.0, 21.2, -65.8), hp 1000.0/1000.0
- Scene enemies deactivated for the run (runtime only): 5; spawner components disabled: 0
- Time.timeScale=1, runInBackground=True
- Enemy prefabs found: 1

**1 enemies audited - 1 FAIL, 0 warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.

## Summary table

| Enemy | Melee | Ranged | Config | Weapons shown | No circling | Notice/chase | Melee dmg | Ranged dmg | Player->enemy | Hit marks | Death/loot | Console |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Axe_Droid | Spear of Fate | - | PASS | PASS | PASS | PASS | PASS | n/a | PASS | PASS | **FAIL** | PASS |

## Weapons that deal NO damage (headline)

- none detected in this run.

## Per-enemy detail

### Axe_Droid

Prefab `Assets/_Project/Prefabs/Combat/Enemies/Axe_Droid.prefab` - definition `Axe_Droid` - melee `Spear of Fate` / ranged `-` - audited in 44s

| Group | Check | Status | Measured | Probable cause / note |
|---|---|---|---|---|
| Config | Definition / weapon data | PASS | def=Axe_Droid, melee=Spear of Fate (dmg 16), ranged=- (dmg -), preferRanged=False, def.attackDamage=12, hp=250 |  |
| AI | No circling while unaware (player 50m away, 9s) | PASS | yaw turned 172deg, path 10.5m, net 2.7m, states Wander |  |
| AI | Notice / chase / attack timing (player 10m in front) | PASS | notice 0.3s, chase 0.3s, first attack 1.2s (at 2.2m, weapon Spear of Fate), first player damage 9.2s, attacks 3, states Wander/Hold/Chase/Attack |  |
| Weapons | Damage to player during natural AI behaviour | INFO | 20.0 hp in 9.2s using Spear of Fate |  |
| Weapons | Melee 'Spear of Fate' damages the player | PASS | player hp lost 0.5 (item dmg 16), attacks 0, hitbox-active frames 0 (unarmed 0), hit events 0, first hit 0.2s, min distance 1.51m, target 'Player_v7 Combat Variant', states Attack; hitDamageTags=[Player,CompanionAI] vs player tag 'Player'; weapon Drawn_Spear_of_Fate hitBoxes=1, vDamage=20; hitBox: layer Ignore Raycast, trigger=True, type=-1 |  |
| Weapons | Melee drawn state | INFO | active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered [] |  |
| Weapons | Ranged weapon damages the player | SKIP | no resolved ranged weapon |  |
| Weapons | Exactly one weapon drawn (resolved), other holstered | PASS | spawn: active item Spear of Fate; drawn [Drawn_Spear_of_Fate]; holstered []; max drawn 1, max holstered 0 |  |
| Player->enemy | Player damage hurts the enemy (receiver path) | PASS | receiver hit: enemy hp 250.0 -> 232.1 (-17.9); 1 receiver(s) |  |
| Player->enemy | Player melee swing via injected Mouse input (best effort) | PASS | 3 left-click attacks at 1.4m: enemy hp 232.1 -> 158.5; player rightWeapon=Drawn_Sword_of_Fear |  |
| Hit marks | Hit marks fire on damage | PASS | active marks 0 -> 4, body type Android |  |
| Death | Death, loot bag, cleanup | FAIL | dead yes in 0.26s, loot bag 6.6s, corpse gone NO, dropped weapon none; no loot configured | corpse not destroyed/disabled after 17s (destroyDelay 3s) |
| Animation | Animation clip timings | INFO | draw/equip 3, attack 12, stagger/hit 8, death 1; 3 clip(s) observed playing; 12 distinct clips seen in total |  |
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
