# DM Enemy Script Sync Plan (PHASE 1: report only)

Status: **PLAN ONLY. Nothing in the project was changed to produce this.** Generated 2026-10-10 from read-only inspection of the
prefab assets (no instantiation, no saves). Phase 2 starts only after Anthony confirms.

Goal (Anthony): every enemy carries the **same script set as the singular template** (`HumanoidEnemy_Invector` plus the creator output),
then is customised manually or by PCG / brain / AI later. Prefab-side profile values are TUNING for testing (a brain may change them);
the shipped game uses fixed parameters. Brain-driven weapon choice is deferred.

Enemy prefabs with `EnemyInvectorBootstrap` (9): HumanoidEnemy_Invector (template), Axe_Droid, Humadroid, Lisa_Hybrid (creator outputs),
corrupt_patrol_android, Humanoid, Hybrid Droid (FRED), Robot, The_Evil_One (older, hand-built). `Gongo`, `TrainingDummy` and the DMI creature
prefabs are not humanoid-bootstrap enemies and are out of scope.

## 1. Headline findings

1. **The template is the dirty one, not the old enemies.** The old enemies are within 5-7 root components of the template, and the gap is
   mostly player-only baggage the template carries and the creator copies into every new prefab (Axe_Droid, Humadroid, Lisa_Hybrid have it).
2. **`PlayerProgressionManager` is on the template and on 3 creator outputs.** It is a singleton (`Awake`: if `Instance` exists, destroy self,
   else become `Instance`). An enemy that wakes before the player can become `Instance` and the player's manager is destroyed (XP, level,
   save/load use `Instance` / `FindAnyObjectByType`). Live right now: the active scene holds 1 each of Axe_Droid, Humadroid, Lisa_Hybrid and the
   template. **Highest-priority hygiene item.**
3. `DMIGrenadeCookController` / `DMIGrenadeThrowBridge` (player G-throw, read player input) and `PioneerShooterManager` (player shooter manager,
   expects `WeaponAmmoState` that the enemy stripper deletes) are also on the template and the 3 creator outputs. The runtime stripper
   (`EnemyInvectorComponentStripper`) does NOT remove any of them.
4. The old enemies use a plain `vShooterManager` and a baked `DMEnemyHitMarks`; the template has `PioneerShooterManager` and no baked hit marks
   (both get ensured at runtime: `DMEnemyHitMarks.Ensure`, Bootstrap adds `CombatPoise`).
5. Template child hygiene: `Drawn_/Holstered_DM_Mining_Tool` slots (player mining tool with a ProBuilder scan cone, MeshCollider and a pulse
   script), a stray `ScifiPistolFlashlightBlack` mesh with a Point Light + lens flare under `WeaponHolders/RifleHolder`, 22
   `EquippedVisualMarker`, 11 `vCollectableStandalone`, 16 `vComment`, 4 `vEntityPreview` (player item leftovers; markers are stripped at runtime).
6. The template bakes `meleeWeaponItem = weap2_sword` and `rangedWeaponItem = sci_fi_pistol` into the LoadoutBridge override fields, so every
   enemy cloned from it starts with "Bridge override" weapons equal to that definition (the Fix already clears redundant ones).

## 2. Root component matrix

Legend: `T` template has it, `ok` same as template, `MISSING` template has it but the enemy does not, `+EXTRA` enemy has it but the template does not.

| Root component | Class | TEMPLATE | Axe_Droid | Humadroid | Lisa_Hybrid | corrupt_patrol | Humanoid | Hybrid Droid | Robot | The_Evil_One |
|---|---|---|---|---|---|---|---|---|---|---|
| vRagdoll | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| vThirdPersonController | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| vMeleeManager | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| vShooterManager | Core (enemy shooter manager, proven on shipped enemies) | - | - | - | - | +EXTRA | +EXTRA | +EXTRA | +EXTRA | +EXTRA |
| EnemyAiController | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyCombat | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyDeathSequence | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyHealth | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyHealthBarPresenter | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyLootable | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemySenses | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyTerrainRescue | Core (also runtime-ensured) | T | ok | ok | ok | ok | ok | ok | ok | ok |
| HumanoidPerformanceController | Core (also runtime-ensured) | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyInvectorBootstrap | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyInvectorCombatBridge | Core (also runtime-ensured) | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyInvectorDeathPresenter | Core (also runtime-ensured) | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyInvectorLoadoutBridge | Core (also runtime-ensured) | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyInvectorMotorBridge | Core (also runtime-ensured) | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyInvectorOutgoingDamageBridge | Core (also runtime-ensured) | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyInvectorPhysicsCache | Core (also runtime-ensured) | T | ok | ok | ok | ok | ok | ok | ok | ok |
| EnemyInvectorRagdollBridge | Core (also runtime-ensured) | T | ok | ok | ok | ok | ok | ok | ok | ok |
| CombatPoise | Runtime-ensured (Bootstrap adds if missing) | T | ok | ok | ok | MISSING | ok | ok | ok | MISSING |
| DMEnemyHitMarks | Runtime-ensured (Bootstrap + DMEnemyHitMarks.Ensure) | - | - | - | - | +EXTRA | +EXTRA | +EXTRA | +EXTRA | +EXTRA |
| DMIGrenadeCookController | PLAYER-ONLY | T | ok | ok | ok | MISSING | MISSING | MISSING | MISSING | MISSING |
| DMIGrenadeThrowBridge | PLAYER-ONLY | T | ok | ok | ok | MISSING | MISSING | MISSING | MISSING | MISSING |
| EnemyDisintegrationEffect | Core (also runtime-ensured) | T | ok | ok | ok | ok | ok | ok | ok | ok |
| PioneerInvectorDamageReceiver | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| PioneerShooterManager | PLAYER-ONLY (decision) | T | ok | ok | ok | MISSING | MISSING | MISSING | MISSING | MISSING |
| PlayerProgressionManager | PLAYER-ONLY (singleton!) | T | ok | ok | ok | MISSING | MISSING | MISSING | MISSING | MISSING |
| Animator | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |
| CapsuleCollider | Core (definition-driven size) | T | ok | ok | ok | ok | ok | ok | ok | ok |
| Rigidbody | Core | T | ok | ok | ok | ok | ok | ok | ok | ok |

Notes on the matrix:
- `MISSING` on a PLAYER-ONLY row is **good** (those enemies are already cleaner than the template).
- `CombatPoise` missing on corrupt_patrol_android / The_Evil_One: Bootstrap adds it at runtime, so behaviour is the same; baking it only helps the editor/Fix see it.
- `vShooterManager` vs `PioneerShooterManager`: `EnemyInvectorLoadoutBridge` / bridges use `GetComponent<vShooterManager>()` so both satisfy it.
- Components Bootstrap adds at runtime that are on NO prefab: `EnemyInvectorProjectileBridge`, `EnemyProgressionXp` (added by `ApplyDefinition`), hitbox rig (`DMEnemyHitboxRig`), `EnemyInvectorRagdollSetup`.
- Runtime `EnemyInvectorComponentStripper` removes: Pioneer/Invector player input, inventory, `WeaponAmmoState`, `vAmmoManager`, `vDamageReceiver`, camera, HUD, `EquippedVisualMarker`. It does not remove the 4 player-only components above.

## 3. Child-level differences (vs template)

| Enemy | Weapon slots missing vs template | Extra / one-off children | Notes |
|---|---|---|---|
| Axe_Droid, Humadroid, Lisa_Hybrid | none | none | identical hierarchy shape to the template (including the player-only mining tool + flashlight) |
| corrupt_patrol_android | Drawn_/Holstered_ `Blade_Of_Chaos`, `DM_Mining_Tool` | none | no flashlight mesh/light; 6 scene instances with 12 hand-placed children + 12 added components |
| Humanoid, Hybrid Droid, Robot | same 4 slots | 4x `vSnapToBody` (body snap, applied at runtime by `EnemyInvectorBodySnapSetup`) | no flashlight |
| The_Evil_One | same 4 slots | 4x `vSnapToBody`, 11x `vDamageReceiver` on bones (stripped at runtime) | no flashlight |

Weapon slots matter because the template carries a Drawn_/Holstered_ slot for every weapon; an enemy without `Drawn_Blade_Of_Chaos` can never
equip that weapon through a definition or per-instance override.

## 4. Serialized settings of shared scripts that differ from the template

### 4a. Definition-driven (NOT a sync target; the definition wins at runtime, Fix `SyncValues` can align the fallback)
Capsule radius/height/center, `EnemyHealth.maxHealth`, senses (vision/hearing), `EnemyCombat` attack values, `EnemyAiController` speeds/radii/
timers, `EnemyLootable` loot table and display names, `EnemyInvectorBootstrap.enemyDefinition`, bridge weapon override fields,
health bar fields. Every enemy differs from the template here by design (e.g. capsule 0.15/1.88 on Axe_Droid, 0.75/3.31 on the template).

### 4b. NOT definition-driven and differing from the template

| Component.field | Template | Differs on | Class | Sync verdict |
|---|---|---|---|---|
| Animator culling mode | Cull Update Transforms | all 8 enemies are Always Animate | template wrong (creator standard is AlwaysAnimate) | SAFE: fix the TEMPLATE |
| Animator `m_Enabled` | true | Axe_Droid, Humadroid, Lisa_Hybrid = false (Bootstrap re-enables at runtime) | hygiene | SAFE: set true |
| vThirdPersonController `deathBy` | Animation With Ragdoll | creator outputs = Ragdoll | creator choice | NEEDS DECISION (default: keep Ragdoll) |
| vThirdPersonController `_isDead` | false | Humanoid, Hybrid, Robot, Evil One = **true baked** | stale bake | SAFE: set false (Bootstrap calls `DisableInvectorHealthDeath`) but verify in play |
| vThirdPersonController `isImmortal` | true | the 4 older = false | template default | NEEDS DECISION (default: leave) |
| vThirdPersonController `fillHealthOnStart`, `_maxHealth`, `_currentHealth`, `selectedToolbar` | false / 500 / 500 / 0 | older enemies 100 / 50-300 / true / 5 | Invector health is disabled for enemies (`EnemyHealth` is the authority) | SAFE to ignore; align to the template |
| EnemyInvectorCombatBridge `enemyPistolAmmo`, `enemyRifleAmmo` | **null** | all 5 older = `DMAmmoFxProfile_Standard` / `DMAmmoFxProfile_Plasma` | template gap: creator enemies have no ammo FX profile | SAFE: set Standard/Plasma on the TEMPLATE, fill-when-null on enemies; verify what a null falls back to |
| EnemyAiController tuning: `chaseStaminaPauseMin/Max` | 0.5 / 0.9 | older = 0.35 / 1.15 | TUNING | DO NOT SYNC (default) |
| EnemyAiController `playerThreatRange`, `minCombatSeparation`, `attackStandoffFraction`, `playerStandoffBonus` | 3 / 0.95 / 0.62 / 0.15 | Humanoid, Hybrid, Robot = 6 / 1.35 / 0.67 / 0.35 (Evil One 1.35 / 0.78 / 0.35) | TUNING | DO NOT SYNC (default) |
| EnemyInvectorRagdollBridge `defaultStaggerSeconds`, `baseReactionChance`, `criticalReactionChanceMultiplier` | 0.28 / 0.25 / 1.5 | Humanoid, Hybrid, Robot = 0.6 / 0.414 / 2.243 | TUNING | DO NOT SYNC (default) |
| CombatPoise `maxPoise` | 100 | Humanoid, Hybrid, Robot = 80 | TUNING | DO NOT SYNC (default) |
| vRagdoll `collisionClip`, `horizontalMultiplier`, `openCloseWindow` | clip set / 0 / true | older = null clip / 1 / false | stale | NEEDS DECISION (default: leave) |
| EnemyAiController `engagementProfileOverride` | null | Robot = `DM_EnemyEngagementProfile` | profile override (definition-owned) | KEEP |
| EnemyAiController `patrolPoints` | empty | Humadroid = 4 in-prefab patrol point transforms | **hand-placed data in the prefab** | RISKY: never overwrite |
| vMeleeManager members | own hitbox refs | all (object ids only) | noise | IGNORE |

## 5. Gap list with safety verdict

SAFE = additive/inert, no hand-placed data lost. RISKY = could lose hand-placed data or change play. DECISION = Anthony chooses.

| # | Gap | Where | Verdict | Recommended default |
|---|---|---|---|---|
| G1 | `PlayerProgressionManager` on enemies | template, Axe_Droid, Humadroid, Lisa_Hybrid | SAFE to remove (enemy copy is dead weight; if it loses the singleton race it breaks the player). URGENT | remove from all four |
| G2 | `DMIGrenadeCookController`, `DMIGrenadeThrowBridge` | same four | SAFE to remove (no `vThrowManager` on the template root so they look inert, but they read player input; confirm in a play test) | remove |
| G3 | `PioneerShooterManager` vs `vShooterManager` | template + 3 creator outputs vs 5 older | DECISION (swap changes ranged behaviour: Pioneer overrides `Shoot`/ammo with `WeaponAmmoState`, which the stripper deletes) | enemies use plain `vShooterManager`; swap template + creator + 3 outputs; play-test a pistol enemy |
| G4 | `CombatPoise` not baked | corrupt_patrol, The_Evil_One | SAFE (Bootstrap adds it anyway) | bake on all |
| G5 | `DMEnemyHitMarks` baked on 5 older, not on template | | SAFE (Ensure at runtime; baked `bodyType` is overwritten by the definition) | add to template; leave baked values |
| G6 | Missing weapon slots `Blade_Of_Chaos` (Drawn+Holstered) | 5 older | SAFE-ish: adds children under existing handler/holster bones; reuse `EnemyInvectorWeaponHolderRebind`; needs per-rig bone check (Lisa is ORG rig) | add the weapon slots, skip mining tool |
| G7 | Player mining tool slots (`DM_Mining_Tool`) with ProBuilder cone + MeshCollider | template, Axe_Droid, Humadroid, Lisa_Hybrid | SAFE to remove (player tool) | remove |
| G8 | Stray flashlight mesh + Point Light + lens flare under `RifleHolder` | template + 3 outputs | SAFE (bridge already hides the mesh; the light is a child of it) | remove |
| G9 | Template bakes `weap2_sword` / `sci_fi_pistol` into bridge override fields | template + all older (redundant overrides) | SAFE (Fix `ClearRedundantOverride` exists; the definition supplies the same weapons) | clear |
| G10 | Template `Animator.cullingMode` = CullUpdateTransforms | template | SAFE | AlwaysAnimate |
| G11 | Template bridge `enemyPistolAmmo` / `enemyRifleAmmo` null | template + 3 outputs | SAFE (fill when null) | Standard / Plasma |
| G12 | Stale `_isDead = true` | 4 older | SAFE after a play test | set false |
| G13 | `vSnapToBody` x4, `vDamageReceiver` x11, patrol points | older / Evil One / Humadroid | RISKY to touch (hand data / body snaps) | LEAVE; list only |
| G14 | Tuning values (AI standoff, stagger, poise, stamina pause) | older vs template | TUNING | do not sync; keep per prefab |
| G15 | `corrupt_patrol_android` scene instances (6 in the active scene, 12 hand-placed child objects + 12 added components) | scene | RISKY only for a full Rebuild | Sync only edits root components + slot children; never Rebuild |

## 6. Per-enemy summary

| Enemy | Root script set vs template | What a sync would change | Hand-placed / risky data |
|---|---|---|---|
| HumanoidEnemy_Invector (template, protected) | n/a | hygiene (G1-3, G7-11) | protected: needs explicit approval |
| Axe_Droid | identical (30/30) | remove G1/G2/G3(if approved)/G7/G8 | none; 1 scene instance, 0 added children |
| Humadroid | identical | same as Axe_Droid | 4 in-prefab patrol points; 1 scene instance |
| Lisa_Hybrid | identical | same; Wood Axe is listed as her RANGED weapon (not a ranged item): data fix for Anthony | 1 scene instance |
| corrupt_patrol_android | -5 (CombatPoise, Pioneer shooter mgr, grenade x2, PlayerProgression); +2 (vShooterManager base, DMEnemyHitMarks) | add CombatPoise, slots (G6); keep vShooterManager | **6 scene instances with 12 hand-placed children + 12 added components** |
| Humanoid | -4; +2 | slots (G6) | 4 vSnapToBody; 1 scene instance (+1 in DM_HitMarks_Test) |
| Hybrid Droid (FRED) | -4; +2 | slots (G6) | 4 vSnapToBody; 2 scene instances (+1 test) ; `FRED.prefab` shows as deleted in git (needs a look) |
| Robot | -4; +2 | slots (G6) | 4 vSnapToBody; 1 scene instance (+1 test) |
| The_Evil_One | -5; +2 | CombatPoise, slots (G6) | 11 vDamageReceiver, 4 vSnapToBody; 1 scene instance |

## 7. Proposed mechanism ("Sync to Template")

Principle: one manifest drives creator, check and Fix, so there is no per-prefab code.

1. **Manifest** (`DMEnemyTemplateManifest`, static class or ScriptableObject next to the creator): lists
   - `Required` root components (the enemy core set),
   - `RuntimeEnsured` (bake when missing, never an error),
   - `Forbidden` player-only components (G1, G2, optionally G3) and forbidden child names (`Drawn_/Holstered_DM_Mining_Tool`, the flashlight holder mesh),
   - `RequiredWeaponSlots` (Drawn_/Holstered_ for every ItemData weapon in the library),
   - `DefinitionDriven` field list (shared with `SyncValues`, so definition values are never touched),
   - `TuningFields` (reported, never synced unless "Align tuning" is ticked),
   - `FillWhenNull` defaults (ammo FX profiles).
2. **Creator action `Sync to Template`** (enemy panel and prefab list): operates in place on the existing prefab asset (`LoadPrefabContents`
   -> edit -> `SaveAsPrefabAsset` at the same path, so the GUID is kept). Steps: backup to `Library/DMCreatorBackups` + `rc_deploy`;
   add missing required/runtime-ensured components with default values (copy serialized defaults from the template component, skipping
   definition-driven fields); remove forbidden components; add missing weapon slots through `EnemyInvectorWeaponHolderRebind`; apply
   `FillWhenNull`; re-run the post-build check and show before/after.
3. **Check + Fix** in `DMCharacterCreatorPostBuildCheck` / `DMCharacterCreatorFixer`: new `TemplateSync` check emits warnings
   ("missing template component X", "player-only component Y on an enemy", "missing weapon slot Z", "tuning differs (info only)").
   New fix flags: `AddMissingTemplateComponents` (auto), `RemovePlayerOnlyComponents` (auto for the manifest list, inert), `AddWeaponSlots`
   (auto when the holder bones exist, else manual), tuning = info only.
4. **Safety rails**: never touch scene instances; before removing a component, scan scenes for prefab-instance overrides that target it and
   show the instance count (the Fix confirm dialog); protected prefabs (template, Player_v7*) are blocked unless explicitly approved;
   hand-placed data (`patrolPoints`, `vSnapToBody`, `vDamageReceiver`) is on a do-not-touch list; a Full Rebuild is never part of Sync.
5. **Template hygiene first** (approval needed because it is protected): clean `HumanoidEnemy_Invector` once (G1-3, G7-11), then the
   creator output and the Sync both derive from the clean template. Without that the creator keeps re-importing the player-only components.

## 8. Decisions needed from Anthony (recommended default in brackets)

1. Sync target set = template MINUS player-only, plus runtime-ensured baked? [YES: sync to "enemy core set", not the literal template]
2. Remove `PlayerProgressionManager` + grenade components now from the template and the 3 creator outputs (hotfix, before the rest)? [YES, first]
3. Enemies use plain `vShooterManager` (G3)? [YES; play-test one pistol enemy]
4. Add Blade_Of_Chaos (and any other library weapon) slots to the 5 older enemies, drop mining-tool slots everywhere? [YES / YES]
5. Clean the template's baked weapon overrides, culling, ammo FX profiles, flashlight mesh? [YES]
6. Tuning values (AI standoff, stagger, poise, stamina pause): keep per prefab, never sync? [YES keep; revisit when brain profiles land]
7. `isImmortal`, `deathBy`, ragdoll `collisionClip` template-vs-enemy differences: leave? [leave]
8. Stale `_isDead = true` on 4 older enemies: clear after a play check? [YES]
9. `vSnapToBody` / `vDamageReceiver` / Humadroid patrol points: leave untouched? [YES]
10. Lisa_Hybrid lists Wood Axe as her ranged weapon: change it (your data)? [your call; not touched]
11. `FRED.prefab` is deleted in the git working tree while `Hybrid Droid.prefab` exists: intentional rename? [please confirm]
12. Template baked `DMEnemyHitMarks` and `CombatPoise`: add to template so the editor/Fix see them? [YES]

## 9. Phase 2 order (after confirmation)

1. Hotfix G1/G2 on template + Axe_Droid + Humadroid + Lisa_Hybrid (backup first, GUID kept, 0 console errors).
2. Template hygiene (G3, G7-G11) on a throwaway copy, verify, then the real template with explicit approval.
3. Build manifest + check + Fix + creator `Sync to Template`; test on throwaway copies of every enemy.
4. Run Sync on the older enemies one by one (corrupt_patrol_android last), confirm scene instances resolve.
5. Update this doc with Built status and a test checklist.

---

# PHASE 2 RESULTS (applied 2026-10-10)

## A. Manifest (what "Sync to Template" does, in `DMEnemyTemplateSync`)
| Item | Rule | Class |
|---|---|---|
| Player-only root components (`PlayerProgressionManager`) | removed | player-only |
| Grenade components (`DMIGrenadeCookController`, `DMIGrenadeThrowBridge`) | present ONLY when `EnemyDefinition.canThrowGrenades` is on (default off); Sync adds them when on, removes them when off; Check treats them as allowed, never "extra" | OPTIONAL |
| `PioneerShooterManager` | replaced by plain `vShooterManager` (serialized values copied, references re-pointed) | decided G3 |
| Player-only children (`Drawn_/Holstered_DM_Mining_Tool`, `ScifiPistolFlashlightBlack`) | removed | player-only |
| Template root components missing on an enemy (`CombatPoise`, `DMEnemyHitboxRig`, `DMEnemyHitMarks`, any other template root script) | added, EXCEPT scripts in `UnusedPendingAudit` (never added) | USED |
| `DMEnemyHitMarks.bodyType` | taken from the EnemyDefinition (Humanoid / Android / Robot) on add and when it differs | definition-driven |
| `DMEnemyHitboxRig` | component baked on the root; the per-bone hitboxes on layer `DMHitbox` (26) are built at spawn by `EnemyInvectorBootstrap` (`DMEnemyHitboxRig.Ensure`). The Check warns if layer DMHitbox is missing. | USED |
| Weapon slots the template has and the enemy lacks (`Blade_Of_Chaos` Drawn_/Holstered_) | cloned under the matching holder (found through a sibling slot both rigs share); UNUSED-class components (vComment, vWeaponConstrain, vHitEffects...) are left off the clone; foreign references nulled | USED |
| Animator culling AlwaysAnimate, Animator enabled, ammo FX (`DMAmmoFxProfile_Standard` / `_Plasma`) when null, stale `_isDead`, `fillHealthOnStart`, `_maxHealth`, `_currentHealth`, `selectedToolbar` | aligned to the template | hygiene |
| Template bridge override fields (`meleeWeaponItem`, `rangedWeaponItem`) | cleared on the TEMPLATE only (the definition supplies weapons) | hygiene |
| NEVER touched | definition values, tuning values (AI standoff, stagger, poise, stamina pause), `vSnapToBody`, `vDamageReceiver`, patrol points, `deathBy`, `isImmortal`, vRagdoll fields, scene instance children/overrides, Lisa_Hybrid's Wood Axe slot | protected |

Check/Fix: `DMCharacterCreatorPostBuildCheck` adds a `Template sync:` warning per open item; `DMCharacterCreatorFixer` has the new `TemplateSync` fix (auto; "(manual)" items stay manual); the prefab list has a **Sync to Template** button and its Fix/Fix All include it. Integrity line says "object ids removed by the template sync (expected)".

## B. Hotfix + template hygiene (DONE)
- PlayerProgressionManager and both grenade components removed from `HumanoidEnemy_Invector`, `Axe_Droid`, `Humadroid`, `Lisa_Hybrid` (GUIDs and metas unchanged). Scene scan: the PPM / grenade script GUIDs appear only in Player prefabs and `Pioneer v1.5.6.unity`; no enemy scene instance carried an override for them (scene instance property-modification counts unchanged after the sync).
- Template: 11 changes (Pioneer shooter -> vShooterManager, 3 player-only children, 2 baked bridge overrides, culling, 2 ammo FX, DMEnemyHitboxRig, DMEnemyHitMarks). 793 -> 716 components, file 2.20 MB -> 1.69 MB.
- Template warnings (Creator check on `HumanoidEnemy_Invector`): 9 were reported without a definition: (1) no EnemyDefinition linked -> now an Info line for the template itself; (2) culling not AlwaysAnimate -> fixed; (3) Pioneer shooter manager -> replaced; (4) player-only mining/flashlight children -> removed; (5) baked bridge overrides -> cleared; (6) the same culling item from the sync check -> fixed; (7) empty ammo FX -> filled; (8) missing DMEnemyHitboxRig -> added; (9) missing DMEnemyHitMarks -> added. The Studio's 12 appear to include definition-dependent checks from the working draft definition; those are not template properties (see report).
- Bug found and fixed on the way: `EnemyInvectorCombatBridge` pointed at `Ammo/Standard.asset` / `Plasma.asset`, which do not exist (the ItemData ammo assets are `DMAmmoFxProfile_Standard` / `_Plasma`), so its editor auto-fill never worked.

## C. Per-enemy sync (one at a time, corrupt_patrol_android last; all GUIDs kept, 0 console errors after each)
| Enemy | Changes | Components | Missing before | Scene instances |
|---|---|---|---|---|
| Axe_Droid | Pioneer->vShooter, 3 player-only children, ammo FX, Animator enabled, HitboxRig, HitMarks (Android) | 865 -> 788 | HitMarks, HitboxRig | 1, propMods 12 -> 12 |
| Humadroid | same (patrol points untouched) | 858 -> 781 | HitMarks, HitboxRig | 1, 17 -> 17 |
| Lisa_Hybrid | same (Wood Axe slot untouched) | 1233 -> 1156 | HitMarks, HitboxRig | 1, 22 -> 22 |
| Humanoid | Blade_Of_Chaos slots, 4 align fields (incl. stale _isDead), HitboxRig | 662 -> 690 | HitboxRig | 1, 16 -> 16 |
| Robot | slots, 5 align fields, HitboxRig, grenades (definition flag on) | 662 -> 692 | HitboxRig | 1, 16 -> 16 |
| Hybrid Droid (FRED) | slots, 4 align fields, HitboxRig | 662 -> 690 | HitboxRig | 2, 24 -> 24 |
| The_Evil_One | slots, 4 align fields, CombatPoise, HitboxRig, HitMarks body type Android -> Humanoid (definition), grenades (flag on) | 670 -> 701 | CombatPoise, HitboxRig | 1, 20 -> 20 |
| corrupt_patrol_android | slots, 4 align fields, CombatPoise, HitboxRig | 701 -> 730 | CombatPoise, HitboxRig | **6 (hand-placed children kept)**, 192 -> 192 |
Backups: `Library/DMCreatorBackups/<name>_pre_templatesync_20261010.prefab` and `rc_deploy/enemy_sync_20261010/<name>.pre_templatesync.prefab`.

After the sync every enemy has the identical root script set as the template (grenades only on Robot and The_Evil_One by their definition flag).

## D. Left for Anthony's decision
Tuning values (EnemyAiController standoff/stamina, RagdollBridge stagger/reaction, CombatPoise.maxPoise) still differ from the template on the older enemies. You asked for "template values" but the approved Phase 2 rules protect tuning; they were NOT overwritten. Also left: `deathBy`, `isImmortal`, vRagdoll fields, Lisa_Hybrid's 3 weapon warnings (starting rifle slot inactive / sword slot still active; the Wood Axe decision), Humadroid patrol points, The_Evil_One's vDamageReceiver x11.

## E. Phase 4 / Brain note (grenades)
`EnemyDefinition.canThrowGrenades` is the hook: it only adds the two grenade components and lets Sync/Check treat them as allowed. **Brain-driven throwing comes later** (decision to throw, aiming, cook time, ammo). Nothing in this pass reads those components at runtime.

## F. Tuning applied + Lisa re-check (2026-10-10, afternoon)
**Tuning to template (Anthony's decision):** applied one enemy at a time, corrupt_patrol_android last, backups `Library/DMCreatorBackups/<name>_pre_tuning_20261010.prefab` + `rc_deploy/enemy_sync_20261010/<name>.pre_tuning.prefab`, 0 console errors after each, scene instance counts and property modifications unchanged. Fields (now part of Sync to Template, `Tuning` item): EnemyAiController chaseStaminaPauseMin/Max, playerThreatRange, minCombatSeparation, attackStandoffFraction, playerStandoffBonus; EnemyInvectorRagdollBridge defaultStaggerSeconds, baseReactionChance, criticalReactionChanceMultiplier; CombatPoise.maxPoise.
| Enemy | Changed |
|---|---|
| Humanoid, Robot, Hybrid Droid | all 10 fields (e.g. stagger 0.6 -> 0.28, poise 80 -> 100, playerThreatRange 6 -> 3) |
| The_Evil_One | 5 (stamina pause 0.35/1.15 -> 0.5/0.9, minCombatSeparation 1.35 -> 0.95, attackStandoffFraction 0.78 -> 0.62, playerStandoffBonus 0.35 -> 0.15) |
| corrupt_patrol_android | 2 (stamina pause 0.35/1.15 -> 0.5/0.9) |
| Axe_Droid, Humadroid, Lisa_Hybrid | none needed (already equal) |
Deliberately left alone: all definition-driven fields (health, senses, attack range/damage/cooldown, speeds, turn speed, wander/chase radii, loseTargetDelay, returnToHome, hearing aggro chance, loot, capsule, hit capsule, body type, weapons, movement/patrol mode); Humadroid patrol points; vShooterManager values (`allAmmoInfinity` on Humadroid is a functional ammo setting, the camera recoil/sway and selectedToolbar fields are inert on enemies); Animator `WriteDefaultValuesOnDisable`; vThirdPersonController `deathBy` / `isImmortal`; the vRagdoll `openCloseWindow` / `horizontalMultiplier` / collision clip (physics feel, not AI tuning; one line to add if wanted); Humanoid/Robot engagement-profile overrides.
**Lisa_Hybrid re-check:** Anthony had edited it (2:44 PM): the sword-slot warnings ("both drawn", "weapon objects") were gone. Two warnings remained: a redundant bridge ranged override (survival_rifle = the definition's rangedWeaponItem) and the survival_rifle Drawn_ slot inactive. Fixed only those via the standard Fix (ClearRedundantOverride + WeaponSlots): prefab diff = 6 lines (override field emptied, Drawn_survival_rifle active, one nested activation value). Check now 0 warnings. Backup: `Lisa_Hybrid_pre_weaponfix_20261010.prefab`.
