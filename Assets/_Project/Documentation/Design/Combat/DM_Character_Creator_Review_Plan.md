# Character Creator: Pre-Create Settings Review and Fix Plan

Date: Oct 10, 2026. Status: BUILT (see section 7+). Sections 1-6 are the original review; section 7 onward records what was built, deferred and left.
Scope (per Anthony): every field you set BEFORE pressing Create / Rebuild on the Enemy and Player panels. For each: is it read, validated, applied to the prefab and definition?

Files reviewed (all under `Assets/_Project/Editor/` unless noted):
`GenesisStudio/DMStudioCharacterCreatorPanel.cs` (Studio host, "Studio"), `EnemyPrefabCreatorPanel.cs` ("EPanel"), `EnemyHumanoidPrefabCreatorPanel.cs` ("EHum"), `PlayerPrefabCreatorPanel.cs` ("PPanel"), `DMCharacterCreatorActionValidation.cs` ("Val"), `DMCharacterCreatorDefinitionSidebar.cs`, `EnemyPrefabVisualSetupUtility.cs` ("EVis"), `PlayerPrefabVisualSetupUtility.cs` ("PVis"), `DMHumanoidVisualRebuildUtility.cs` ("Rebuild"), `Invector/EnemyInvectorSetupUtility.cs` ("ESetup"), `EnemyPrefabBuilder.cs`; runtime `Scripts/Data/EnemyDefinition.cs`, `Scripts/AI/Invector/EnemyInvectorBootstrap.cs` ("Boot"), `EnemyInvectorGameplaySetup.cs` ("GSetup"), `Scripts/AI/EnemyDefinitionOverrides.cs`, `Scripts/Data/PlayerVisualDefinition.cs`. Data checked: `Data/Enemies/*.asset`, `Prefabs/Combat/Enemies/*.prefab`.

## 1. How the pre-create settings flow today

1. Pick a definition (Studio:303-351) or "New Custom" (Studio:693-709). Selecting makes `workingEnemyDef = Object.Instantiate(def)` (Studio:475), a clone, not the asset.
2. Each frame the panel pulls and pushes state: `SyncEnemyStateFromWorking` (711) and `SyncEnemyStateToWorking` (726) -> `EHum.SyncToDefinition` (EHum:473-484) copies name, prefab name, template, model, visual child, and forces `archetype = HumanoidInvector`.
3. Create buttons: `Save Definition + Create Prefab + Apply Visual` (Studio:536), `Create/Apply Visual/Rebuild` (EHum:143-157), `Rebuild From Template + Apply Visual` (EHum:159-170).
4. Validation = `Val.CollectEnemyCreateBlockers` (Val:146-173): output path not protected template, template file exists, model Humanoid-ready (or auto-prepare on), visual child name not empty. Save = `CollectEnemySaveBlockers` (Val:89-116): id, display name, prefab name, definition file name characters.
5. Build: `EVis.CreateOrRebuildEnemyPrefab` (EVis:82-177): clone template (112-124), attach model (154), `FinalizeVisualCommon`, then `ESetup.RepairHumanoidRoot` (ESetup:125-153) which bakes the definition into the prefab (`EnemyPrefabBuilder.ApplyGameplayComponents`, weapons loadout, bootstrap link, collider, layers) and saves.
6. After create, `OnEnemyPrefabCreatedOrRebuilt` (Studio:581) saves the definition asset (`EPanel.SaveDefinitionAsset`, EPanel:430-472, CopySerialized over an existing asset with no prompt).
7. At runtime `EnemyInvectorBootstrap.Awake` calls `EnemyInvectorGameplaySetup.Ensure(go, enemyDefinition)` (Boot:61): with a definition it re-applies stats and `EnemyDefinitionOverrides` (body type, brain, profile overrides); without one it applies hard-coded defaults (GSetup:40-41, 44-78).
8. Player flow is the same but thinner: `PlayerVisualDefinition` has only 7 fields (name, prefab name, template, playerPrefab, visual child, last model, notes). Everything else comes from the Player_Invector template.

## 2. Field-by-field audit (pre-create settings)

Legend: OK = read, validated, applied. Verdicts are from code plus the saved assets.

### Enemy panel

| Setting | Read/applied | Validated | Verdict / problem |
|---|---|---|---|
| Template Prefab | Yes (EVis:22-32, 98) | File exists only (Val:241). Only `HumanoidEnemy_Invector` is protected (EVis:48-60) | PARTIAL: any GameObject accepted as template; no "is this an Invector humanoid root" check |
| Model FBX / Prefab | Yes (EVis:126-161) | Humanoid-ready (Val:271-291) | PARTIAL: no mesh/SMR exists, scale/height or HDRP material check |
| Visual Child Name | Yes | Non-empty only | OK (low) |
| Auto-prepare import | Used only by the Create button (EHum:303). `Rebuild From Template` forces true + force-Humanoid (EHum:338-343, Rebuild:99-100) | n/a | WRONG: toggle ignored by Rebuild, which silently rewrites the shared FBX importer; not persisted in the definition |
| Display Name | Yes (loot name, health bar, path fallback) | Non-empty | OK |
| Prefab File Name | Yes -> `Prefabs/Combat/Enemies/<name>.prefab` (EVis:34-39) | Non-empty, protected template only (EVis:62-80) | RISK: no collision check vs existing prefabs (Humanoid, Robot, The_Evil_One, Hybrid Droid, ...); case-insensitive and space->underscore (EnemyPrefabBuilder.cs:409-420) differences are not shown, e.g. existing `Hybrid Droid.prefab` vs generated `Hybrid_Droid` |
| Enemy Id | Saved only | Non-empty only (Val:100) | RISK: no uniqueness/format check. Real data: `Lisa_Hybrid.asset` has id `Lisa_Hydric`; ids mix `fred_android` / `Axe_Droid` (EnemyRegistry + achievements match by id) |
| Definition Asset Name | **Ignored** | Characters only | WRONG: overwritten from Prefab File Name on every save (Studio:637 -> 612-615 -> Sidebar:206-211). Typing a name does nothing. Same on Player (Studio:599-603) |
| Archetype (EnemyArchetype popup) | **Ignored** | none | WRONG: forced to HumanoidInvector every frame (EHum:483); `AutoDetect` flip to LegacyCreature (EHum:234-250) is reset immediately |
| Melee / Ranged Weapon, Prefer Ranged | Yes (ESetup:599-621, runtime loadout) | **None** | GAP: null allowed; PreferRanged with no ranged item allowed; any ItemData type accepted. New Custom starts empty while the template def has defaults (ESetup:499-501) |
| Behavior Preset | Yes | n/a | RISK: dropdown change and "Apply Preset Values" overwrite tuned movement, senses, combat values (EnemyDefinition.cs:231-332). Label does not flip to Custom after manual edits |
| Movement / Patrol | Baked | none | PARTIAL: Patrol Path Creator is not stored anywhere; Apply only hits a selected scene enemy (Studio:491-503); help text mentions "Place in Scene" which does not exist (EPanel:168) |
| Loot (enable, AC, random count, pool, delay, range) | Baked (EnemyPrefabBuilder:231-248) + runtime | **None** | GAP: min > max allowed (Shared UI:288-310), null pool entries allowed. Loot Bag prefab/mesh/texture are not in the panel (only the raw asset); default bag resolves from `DM_LootChestProfile` (OK) |
| Health (max, destroy, respawn) | Baked + runtime | **None** | GAP: max health <= 0 allowed; destroy/respawn conflict only explained in a help box |
| Health Bar (show, hide until damaged, offset) | Baked + runtime | none | PARTIAL: offset default (0,2,0) is not derived from model height. Presenter is destroyed at build when off, then re-added at runtime (GSetup:32); definition decides (OK) |
| Senses / Combat stats | Baked + runtime | none | OK, but no range sanity (ranged engage vs melee range ordering) |
| **Collider (radius, height, center, fit)** | Edit-time only (ESetup:636-651). **Runtime ignores it**: `EnemyInvectorBootstrap` keeps its own serialized hitCapsule* (Boot:20-24) and re-applies them at Awake and Start (Boot:65-74, 124); `WireBootstrapDefinition` writes only `enemyDefinition` + `infiniteAmmo` (ESetup:508-523) | none | WRONG: every enemy prefab has `hitCapsuleRadius: 0.45` regardless of definition |
| **Body Type (hit marks)** | **Not in panel.** Applied only at runtime via `EnemyDefinitionOverrides` (:22) | none | MISSING + wrong default: all 4 creator-made definitions (Axe_Droid, Humadroid, Lisa_Hybrid, Test) have `bodyType: 0 (Humanoid = blood)` although they are droids |
| Enemy Category, Surface Threat Kind | **Not in panel** | none | MISSING: all `enemyCategory: Grunt`; Axe_Droid and Test have threat kind Lifeform |
| Brain override, brain archetype, personalities, brain/engagement/hit-mark profile overrides | **Not in panel** (Enemy Types table / raw asset only) | none | MISSING: new enemies cannot get a brain from the creator. `Humadroid.asset` has archetype 8, personalities 3/9 and 3 profile overrides but `overrideBrain: 0`, so the archetype/personalities are ignored (profile overrides still apply) |
| XP Reward | Not in panel; **no runtime reader** for `EnemyDefinition.xpReward` (only Achievement/Creature/Quest types read their own) | none | DEAD field (25 everywhere) |
| Chase Speed / Multiplier | Not in panel, applied at runtime (GSetup:128-129) | none | MISSING (low) |

### Definition link (the biggest pre-create problem)

`Axe_Droid.prefab` and `Humadroid.prefab` both have `enemyDefinition: {fileID: 0}` on `EnemyInvectorBootstrap`. Lisa_Hybrid, Humanoid, Robot, Hybrid Droid (FRED) are linked correctly.
Cause: the working definition is always an in-memory `Object.Instantiate` clone (Studio:465, 475, 696), including after "Save" (SaveEnemyDefinitionAndSelect re-selects a fresh clone, Studio:641-646). `WireBootstrapDefinition` (ESetup:508-523) stores that clone, Unity serializes a non-asset reference as null, and nothing re-wires after the asset is saved (OnEnemyPrefabCreatedOrRebuilt, Studio:581-597).
Effect at runtime (code-verified, confirm in Play): `Ensure(go, null)` runs `ApplyRuntimeDefaults` (60 HP, melee 2 m, no body type, no brain override), so Humadroid (definition says 400 HP, FRED-style brain) plays as a 60 HP default humanoid with blood hit marks.

### Player panel

| Setting | Verdict |
|---|---|
| Template Prefab | PARTIAL: only `Player_Invector` is protected (PVis:54-66). Any GameObject accepted. |
| Prefab File Name | RISK: output `Player_v7`, `Player_v7 Variant`, `Player_v7 Combat Variant` etc. are NOT protected. `Rebuild From Template` would delete and re-clone `Player_v7` (violates the Player_v7 lock). Default `Player_Custom`. |
| Display Name, Visual Child Name, Model, Auto-prepare | Same behavior as enemy (auto-prepare ignored by Rebuild, PPanel:470-486). |
| Definition Asset Name | **Ignored**, overwritten from Prefab File Name (Studio:599-603, 617-619). Save validation falls back to meaningless "Player_Default" (Val:137-141). |
| Notes | Stored only. |
| `playerPrefab` back-reference | Set at save from "does the file exist now"; stays null if Save runs before Create (PPanel:233-235). |
| Player gameplay settings (weapon, health, camera) | None exist. Inherited from the template only. That is by design; nothing is ignored. |

## 3. Issues ranked by severity (pre-create only)

**S1 Data loss / broken output**
1. **Wrong prefab can be overwritten.** Only `HumanoidEnemy_Invector` / `Player_Invector` are protected; typing an existing output name (`Humanoid`, `Robot`, `Player_v7`) then "Rebuild From Template" deletes and re-clones it (Rebuild:68-83). Fix: add an "is this prefab already owned by another definition / not created by the creator" check (stamp prefabs with a `DMCreatorOrigin` marker or compare against definition `prefabFileName`s), block locked prefabs (`Player_v7*`, any prefab in a deny list), and show a clear overwrite warning with a diff of what will be reset.
2. **"New Custom" copies the previous identity.** `StartNewEnemyCustom` reads `enemyState.prefabFileName/displayName` (Studio:693-709), so after selecting Axe_Droid the "new" enemy has id, prefab name and asset name Axe_Droid; one Save overwrites `Axe_Droid.asset` (EPanel:461 has no prompt). Same for player (Studio:678-691). Fix: New Custom resets to blank defaults and a unique suggested name; Save asks before overwriting a different asset (compare enemyId/prefab name against the selected asset).
3. **Rebuild breaks references and manual tweaks.** `DeleteAsset` + `CopyAsset` (Rebuild:70, 83) gives the prefab a new GUID; Lisa_Hybrid and Humadroid are placed in `Dark Matter Genesis v1.6.5.unity`, so a rebuild turns those instances into missing prefabs and drops weapon grip / manual component edits. Fix: rebuild in place (`PrefabUtility.LoadPrefabContents`, replace contents, keep the file and .meta) or preserve the GUID; preserve designated override fields (section 5).
4. **Rig check runs after the delete.** In `EVis:112-141` the clone-over-output happens before the model rig check can fail, so a bad model leaves a bare template clone. Fix: run all validation and the model rig check first, then clone.

**S2 Setting ignored or wrongly applied**
5. **Definition link null in prefab** (Axe_Droid, Humadroid; see above). Fix: always build against the persistent asset (save first, then re-load `AssetDatabase.LoadAssetAtPath` and pass that); add `EditorUtility.IsPersistent(def)` to the create validation; re-wire `enemyDefinition` after save; post-build check "Bootstrap.definition == asset".
6. **Collider fields ignored at runtime** (Boot:20-24, 65-74, 124). Fix: `WireBootstrapDefinition` also writes hitCapsule radius/height/center from the resolved capsule (including the fitted result when Fit is on), or Boot reads them from the definition. Keep one source of truth.
7. **Definition Asset Name field is dead** and **Archetype popup is dead** (Studio:637, EHum:483). Fix: either honor them (name field used unless blank; hide Archetype for the Humanoid creator and show it only in the legacy window) or remove them from the UI.
8. **Auto-prepare toggle ignored by Rebuild** and Rebuild rewrites the shared FBX importer (EHum:338-343). Fix: honor the toggle, or show an explicit "this will change import settings of <fbx>" line and list other prefabs using it.
9. **Body type, category, brain override, personalities, profile overrides, surface threat kind are not settable in the creator** and default to Humanoid blood / Grunt / Lifeform. Fix: add a "Type, Body & Brain" section (mirrors EnemyDefinition.cs:78-96) with a smart default from the model/template (see Q1), plus a note when `overrideBrain` is off but archetype/personality values exist (Humadroid).

**S3 Missing validation**
10. Prefab name collisions, case-insensitive duplicates, reserved names; enemyId uniqueness and lowercase_snake format (`Lisa_Hydric` typo; `EnemyRegistry` matches by id).
11. Weapons: null melee/ranged, PreferRanged without ranged item, wrong ItemData category.
12. Numeric sanity: maxHealth > 0, loot min <= max, `rangedEngageRange` > `attackRange`, `visionFov` 0-360, positive durations, respawn vs destroy conflict.
13. Template is not an Invector humanoid root (no `EnemyInvectorBootstrap` / `vThirdPersonController` / Animator isHuman check).
14. Model: no SMR/mesh found, absurd scale/height, non-HDRP materials left as-is.

**S4 Low / cleanup**
15. Behavior Preset overwrites tuned values and does not flip to Custom.
16. `xpReward` is a dead field; Patrol Path is not persisted and the help text references a missing "Place in Scene".
17. Stale assets: `Data/Enemies/Test.asset` has no prefab; FRED_Android uses prefab name `FRED` but the file is `Hybrid Droid.prefab` (Ping Output finds nothing).
18. Working copies are plain `CreateInstance` objects (lost on domain reload) and the panel reloads all definitions with `FindAssets` + `LoadAssetAtPath` every OnGUI (Studio:60, 779-783); validation re-inspects the model every repaint (Val:282-285). Cache and refresh on events.

## 4. Recommended fixes (only for the above)

Phase A: stop losing data and broken links (small, do first)
- A1 Persistent-definition build + re-wire + post-build link check (#5). A2 Overwrite guard + protect `Player_v7*` / existing non-creator prefabs (#1). A3 New Custom resets cleanly + overwrite confirm on Save (#2). A4 Validate before delete (#4). A5 Write hitCapsule* into the Bootstrap (#6).
- Scope: ~6 files, mostly editor code, about half a day. No runtime logic change except Bootstrap capsule wiring.

Phase B: make every visible field real
- B1 Honor or remove Definition Asset Name, Archetype, Auto-prepare (#7, #8). B2 Add Type/Body/Brain section with defaults and the overrideBrain warning (#9). B3 Add validation list for #10-#14 using the existing `DMStudioActionValidation` blocker box. B4 Preset -> Custom flip (#15).
- Scope: ~4 files plus one new validation section, about 1 day.

Phase C: rebuild without destroying (#3)
- In-place rebuild keeping GUID, and a "keep overrides" list (weapon grips, extra components, health bar offset, hit capsule). Scope: 1-2 days, needs testing on Lisa_Hybrid/Humadroid (scene refs).

Phase D: cleanup (#16-#18): ~2-3 hours.

## 5. Optional improvements (max 5 lines)
- Creator profile asset (default body type, archetype, personalities, weapons, loot, health bar, hit capsule) so new enemies start from a sane "droid" or "humanoid" preset.
- Post-build report (pass/fail): definition linked, body type set, Humanoid avatar, layer-26 hitbox rig preview, SMR bounds, HDRP materials, loot, brain.
- Dry run / preview that lists what will be replaced; batch rebuild of all creator outputs.
- Auto-add new definitions to the Enemy Types table and `EnemyRegistry`.
- Weapon grip presets per template.

## 6. Questions for Anthony (with my recommended default)
1. **Default body type for new enemies?** Recommend: derive from a new "Enemy Kind" popup (Humanoid / Android / Robot) that also sets surface threat kind and category; default Android for anything cloned from HumanoidEnemy_Invector with an FBX in the droid folders, otherwise Humanoid.
2. **Rebuild behavior:** replace in place and keep the GUID, preserving weapon grips and extra components? Recommend: yes, in place, with a one-click "reset to template" for the old behavior.
3. **Protect which prefabs?** Recommend: block `Player_v7*`, `Player_Invector*`, `HumanoidEnemy_Invector`, and warn on any other existing prefab not owned by the same definition.
4. **Definition Asset Name / Archetype fields:** remove them (always use Prefab File Name; Humanoid only in this creator)? Recommend: remove both.
5. **Hit capsule:** should the definition (fit-to-renderers) drive the runtime capsule, replacing the Bootstrap's own 0.45/2/1 fields? Recommend: yes, definition wins.

## 7. Built status (Oct 10, 2026)

Rules kept: EnemyDefinition is the authority, one template, no commit/stash/reset, scenes not saved, Player_v7 untouched. Backups: `Library/DMCreatorBackups` and `rc_deploy/creator_fixes_20261010`.

| # | Item | Status |
|---|---|---|
| 1 | Enemy Kind popup (Humanoid / Android / Robot) sets body type, threat kind, category; brain override + archetype + personalities, brain/engagement/hit-mark profile overrides, XP reward in the Studio Combat section | Built (`EPanel.DrawKindAndBrain`, `ApplyEnemyKind`) |
| 2 | Rebuild in place (GUID + .meta kept, backup to `Library/DMCreatorBackups`), rig check before any destructive step | Built, proven: two rebuilds, same GUID |
| 3 | Protect `Player_v7*`, `Player_Invector*`, `HumanoidEnemy_Invector` (enemy and player paths); confirm dialog when the output prefab exists; collision check prefab-name/id vs other definitions; number-range and weapon validation (melee must be a melee item, ranged must be ranged, both must resolve to an Invector weapon prefab, Prefer Ranged needs a ranged weapon) | Built |
| 4 | Dead fields removed from Studio (Definition Asset Name, Archetype); Rebuild respects Auto-prepare (enemy and player; force-Humanoid no longer silent) | Built |
| 5 | Definition drives the runtime hit capsule (`CapsuleRadius/Height/Center` properties on Bootstrap, baked fallbacks written from the definition); default capsule 0.15 / 1.88, fit-to-renderers off | Built |
| 6 | Saved definition linked on every build (`TryEnsureSaved` then `TryWireBootstrapDefinition`); build fails with a clear message if the link cannot be set; `SaveDefinitionAsset` routed through the helper (`working.name` set after save) | Built |
| 7 | Crossed arms root cause fixed (below) | Built + applied to Axe_Droid, Humadroid |
| 8 | Weapon armed on spawn: loadout arms the slot matching `preferRangedWeapon`; post-build check for missing `Drawn_` slots and loadout/definition mismatch; weapon-warning dialog after build | Built |
| 9 | "Re-apply Definition + Repair Visual (No Mesh)" button (calls the repair path, relinks definition, rebakes capsule, reports) | Built |
| 10 | New Custom resets to a blank unique definition (`NewEnemy`, `NewEnemy_2`, ...), nothing copied from the previous enemy | Built |
| 11 | XP: `EnemyInvectorGameplaySetup` now applies `xpReward` to `EnemyProgressionXp` (field was dead) | Built |
| 12 | Post-build check warns when Animator culling is not AlwaysAnimate (creator standard in `RepairHumanoidLocomotionAnimator`: CullUpdateTransforms causes chase glides). Corrected from an earlier inverted check | Built |

Root cause of crossed arms: `DMHumanoidBoneRenameUtility.RebuildHumanoidAvatar` rebuilt the avatar with the visual's authored (A-)pose as the T-pose reference, so every retargeted Invector clip (Basic_FreeMovement Idle) folded the arms in. Fix: it now takes the T-pose from the source FBX avatar `HumanDescription.skeleton` (renamed through the bone rename map). The avatar asset is rewritten in place (GUID kept), so prefabs and scenes need no change. Measured idle hand distance (clip Idle at 37%): fresh build 0.57 (relaxed), Axe_Droid 0.15 -> 0.57, Humadroid 0.06 -> 0.60.

### Throwaway build test (temp path, deleted afterwards)
Two in-place rebuilds at `DMTEST_AxeTmp2.prefab`: GUID identical, hands 0.57, Bootstrap link = `Axe_Droid` asset, capsule 0.15/1.88 on Bootstrap and the root CapsuleCollider, `Drawn_Spear_of_Fate` present and active. All temp files, avatars and backups removed.

### Repairs on existing prefabs
| Prefab | Done | Not done |
|---|---|---|
| Axe_Droid | Definition relinked (was `fileID: 0`), capsule fallback 0.15/1.88 baked, avatar re-applied in place (hands 0.15 -> 0.57). Prefab file keeps all 382 object ids, GUID unchanged | **Full Rebuild deferred, see below** |
| Humadroid | Definition relinked, avatar re-applied in place (0.06 -> 0.60). All 1087 object ids kept, GUID unchanged | none |
| Lisa_Hybrid | Checked: uses the FBX's own avatar (`type: 3`), not a creator-generated avatar, so there is nothing to re-apply; link already valid | Her hands are still model-proportion dependent; confirm in Play |

**Why Axe_Droid was not fully rebuilt:** the scene `Dark Matter Genesis v1.6.5` holds an Axe_Droid instance with 4 added GameObjects (hand-placed weapon/hit objects, scale 0.15), added components and `Members` overrides parented to bones inside the old visual. A rebuild assigns new ids to the whole visual subtree (193 of 1067 ids differ per build), so those scene additions would lose their parent and drop. Needs Anthony's call: (a) keep the hand-placed setup as is (current state, works, prefab itself still has no `Drawn_/Holstered_` holders), or (b) rebuild and re-place the axe in the scene after. Definition-side the weapon is correct (Spear of Fate, Start With Melee on).

## 8. Live-linked vs baked

| Value | Where it lives | Behavior |
|---|---|---|
| Stats, health, senses, melee/ranged/AI timings, movement | Definition | Live: re-read at every spawn (`Ensure(go, definition)`) |
| Loot, loot bag, health bar, XP, chase speed | Definition | Live |
| Body type, threat kind, category, brain override/archetype/personalities, brain/engagement/hit-mark profile overrides | Definition (+ profiles) | Live |
| Weapon items, Prefer Ranged | Definition | Live into `EnemyInvectorLoadoutBridge.ConfigureFromDefinition` |
| Hit capsule radius/height/center | Definition | Live (Bootstrap properties prefer the definition) |
| Bootstrap `hitCapsule*` fields, root `CapsuleCollider` | Prefab | Baked fallback; refresh with Re-apply Definition |
| `Drawn_/Holstered_` weapon slot visuals, active slot | Prefab | Baked; refresh with Re-apply Definition |
| Visual model, avatar, animator controller, culling mode | Prefab | Baked; avatar refresh keeps GUID |
| Definition link | Prefab | Saved asset reference; checked after every build |

Editing a definition or profile asset updates every spawned enemy of that definition on the next spawn. Baked rows need "Re-apply Definition" on each prefab. Creator edits to a definition are saved (`SetDirty` + `SaveAssets`) on save/build.

## 9. Reference-enemy audit (one-template rule)

Compared root components, MonoBehaviour counts, definition link, capsule, loadout and animator against `HumanoidEnemy_Invector` (template: 31 root components, 956 objects).

| Enemy | What it has | What the creator was missing / deviation |
|---|---|---|
| HumanoidEnemy_Invector (template) | 31 root comps incl. `PioneerShooterManager`, `CombatPoise`, grenade bridges, `PlayerProgressionManager`; 12 vHitBox, 7 vWeaponHolder, 22 EquippedVisualMarker, 8 WeaponHitbox | Reference. Carries player-only components (PlayerProgressionManager, DMIGrenade*) on an enemy root |
| Axe_Droid | Same 31 root comps; link now valid; capsule 0.15/1.88; loadout Spear of Fate, Start With Melee | No vHitBox (0 of 12), no vWeaponHolder, no WeaponHitbox, no EquippedVisualMarker, no Drawn_/Holstered_ (old build); crossed-arms avatar (fixed) |
| Humadroid | Same 31 root comps; link now valid; 8 vHitBox; loadout both slots | Crossed-arms avatar (fixed); capsule 0.45/2 baked vs definition 0.45/2 (consistent) |
| Lisa_Hybrid | Same 31 root comps; FBX avatar; 8 vHitBox | **Definition bug: Ranged Weapon = "Wood Axe" (a melee item)**; creator validation now blocks saving this until fixed (not changed, data decision) |
| corrupt_patrol_android | 28 root comps; plain `vShooterManager` + baked `DMEnemyHitMarks` instead of Pioneer variant; link valid | No CombatPoise, no grenade/Player components baked (runtime adds what it needs); 10 vHitBox; `DMIMaterialPulseScroll` missing vs template; capsule 0.45/2 |
| Hybrid Droid (FRED) | 29 root comps, plain `vShooterManager`, baked `DMEnemyHitMarks`, 4 `vSnapToBody`, 6 vHitBox, no EquippedVisualMarker | Per-prefab leftovers (below); prefab name `Hybrid Droid` vs definition `FRED_Android` mismatch (not renamed) |
| Humanoid | Same shape as Hybrid Droid (754 objects) | Same leftovers |
| Robot | Same shape as Hybrid Droid | Same leftovers |
| The_Evil_One | 28 root comps, plain `vShooterManager`, baked `DMEnemyHitMarks`, **11 extra `vDamageReceiver`**, 4 `vSnapToBody`; Start With Melee = off on the prefab | One-off hit receivers and a prefab-side loadout flag |

Creator output already matches the template at root level (31 components, no extras) for Axe_Droid, Humadroid and Lisa_Hybrid. Nothing from the references needed to be hard-coded into the creator: `DMEnemyHitMarks` and `CombatPoise` are added at runtime by Bootstrap/GameplaySetup, and body type/brain live in the definition. Added to the post-build check instead: definition link, weapons resolve and are equipped, `Drawn_` slots present, capsule matches, rig/avatar/controller, animator culling, extra root components vs the template, live-vs-baked info.

## 10. Singular template: what still breaks the rule

1. **Template hygiene:** `HumanoidEnemy_Invector` carries player-only components (`PlayerProgressionManager`, `DMIGrenadeCookController`, `DMIGrenadeThrowBridge`) and `PioneerShooterManager`. Propose: strip from the enemy template (protected, so Anthony's call).
2. **Legacy prefabs with plain `vShooterManager` + baked `DMEnemyHitMarks`** (corrupt_patrol_android, Hybrid Droid, Humanoid, Robot, The_Evil_One): runtime adds the hit-mark component; migrate by rebuilding from the template with the same definition (visual swap only).
3. **`The_Evil_One`:** 11 per-prefab `vDamageReceiver` and `startWithMeleeWeapon = off` on the prefab. Move to definition (`preferRangedWeapon` / start slot) and drop the receivers in favor of the template's hit box rig.
4. **`vSnapToBody` x4** on Hybrid Droid, Humanoid, Robot, The_Evil_One: one-off clothing helpers; propose moving into the visual prefab, not the enemy root.
5. **Scene-instance overrides:** Axe_Droid (22 mods incl. 4 added GameObjects, hand-placed weapon), Humadroid (146 mods: weapon holder transforms, active flags). These are per-instance weapon placement; propose a weapon grip preset on the definition (existing optional improvement) and then clear the overrides.
6. **FRED:** prefab name `Hybrid Droid` vs definition `FRED_Android`; `Test.asset` is an unused definition. Propose rename/removal after Anthony confirms.
7. **Lisa_Hybrid** definition: Ranged slot holds a melee item.
8. **Bootstrap `hitCapsule*` / root CapsuleCollider** are baked fallbacks of definition values (kept as fallback by design, refreshed by Re-apply).

None of these existing enemies were migrated. Only broken links were fixed (Axe_Droid, Humadroid) plus the avatar assets.

## 11. Test checklist for Anthony

1. Open Genesis Studio > Character Creator > Enemy: pick **New Custom**. Name/id must be `NewEnemy` (not Axe_Droid), capsule 0.15 / 1.88, no Definition Asset Name or Archetype field.
2. Set Enemy Kind = Android; confirm Threat Kind/Category change; set a brain override and a profile override.
3. Assign a melee weapon, a model, press Create. Expect: no crossed arms in the Idle, weapon drawn on spawn, Bootstrap definition = saved asset (not None), capsule 0.15/1.88.
4. Press Create again on the same name: confirm dialog appears. Try a prefab name `Player_v7_X` or `HumanoidEnemy_Invector`: blocked.
5. Try saving with max health 0, loot min > max, or a non-melee item in Melee Weapon: blocked with a message.
6. Rebuild From Template with Auto-prepare off on a non-humanoid FBX: no silent importer change, clear message.
7. Play with Axe_Droid and Humadroid in the scene: relaxed idle, Humadroid 400 HP (definition), Axe_Droid armed (hand-placed axe still there).
8. Open `Lisa_Hybrid` definition: replace the Ranged Weapon ("Wood Axe") or clear it.
9. Decide the Axe_Droid full rebuild (section 7) and the template-hygiene items (section 10).

## 12. Prefab list (Genesis Studio > Player > Enemy Prefab / Player Prefab)

New foldout at the top of both subtabs (`DMStudioCharacterCreatorPrefabList.cs`; two lines of glue in `DMStudioCharacterCreatorPanel.cs`).

How to use:
1. Open the foldout; it scans on first open. **Refresh** rescans. The search box filters by name, kind/body type, definition or status.
2. List columns: prefab, kind/body type (players: Player / Player (locked)), linked definition, check badge (OK / N warn / N errors). Enemies = every prefab under `Prefabs/Combat/Enemies` with `EnemyInvectorBootstrap` (9 today, incl. the protected template). Players = every prefab under `Prefabs/Players` with `vThirdPersonController` (12 today).
3. Click a row: Ping/Select for the prefab and the definition; post-build check results (no definition link, no weapon holders / weapon not equippable, capsule mismatch, crossed-arm idle hand distance, culling, singular-template extras, definition validation); definition stats (HP/XP, damage, ranges, walk/run/chase, senses, weapons, body type, brain + personalities, profile overrides, loot, capsule, baked fallback vs root collider); a live-linked vs baked foldout.
4. **Edit definition** foldout (enemies): the same drawers as the creator (kind and brain, weapons, health, health bar, senses, combat stats, capsule, loot). Edits go to a working copy; **Save Definition** saves through `SaveDefinitionAsset` / `TryEnsureSaved` (validation and collision checks apply). Baked values need Re-apply Definition afterwards.
5. Actions: **Dry Run (check only)** (re-checks and reports what a rebuild would use plus scene-instance counts, changes nothing), **Re-apply Definition** (enemies; players get "Repair Visual"), **Rebuild From Template + Apply Visual** (linked definition, its Last Model Source or current FBX visual, its template). Unlinked enemies get a Link Definition picker (in place).

Safeguards: protected prefabs (Player_v7*, Player_Invector*, HumanoidEnemy_Invector) are listed but every action is disabled and re-blocked in code; confirm dialog on every action; backup to `Library/DMCreatorBackups` first; rebuild is in place (GUID kept); rebuild is blocked if the definition has validation problems, the model is not Humanoid-ready or the template is missing (the list never changes importers); rebuild confirm shows the scene-instance count (loaded scenes plus scene files under `Assets/_Project/Scenes`) and an orphan warning for hand-placed children. Actions run after layout scopes close, then `GUIUtility.ExitGUI()`.

Limits: players have no stats beyond `PlayerVisualDefinition` (gameplay comes from the template), and a player needs a `PlayerVisualDefinition` with a Last Model Source to rebuild; scene-file counts cover `Assets/_Project/Scenes` only; hand-distance check uses the Invector Idle clip (enemies only); the first open scans and loads each prefab (a second or two); Re-apply on legacy prefabs runs the normal repair path and may add weapon slots.

Tested (no real prefab rebuilt): scan of all 9 enemies and 12 players, dry run on a real prefab (Axe_Droid: 1 scene instance with 8 hand-placed additions), full preflight + backup + in-place rebuild on a throwaway copy (GUID unchanged, hands 0.60), protected block. The throwaway and its backups were deleted.

### Fix / Fix All (prefab list, enemies)

Each enemy row has a **Fix** button (tooltip lists the changes) and the toolbar has **Fix All (N)** for the filtered list. Both: confirm dialog listing exactly what changes, backup to `Library/DMCreatorBackups` first, GUID kept, object ids compared before/after (any lost id or GUID change is reported with the backup path), check re-run, BEFORE / AFTER shown in the detail panel. Fix never runs a rebuild and never touches protected prefabs. Code: `DMCharacterCreatorFixer.cs` (analysis + in-place repairs, no per-prefab logic).

| Issue found by the check | Fix |
|---|---|
| No definition link (and a definition matches the prefab file/display name exactly; no generic fallback) | AUTO: link the saved definition + bake capsule fallback |
| Bootstrap capsule fallback differs from definition | AUTO: bake from definition |
| Root CapsuleCollider differs from definition | AUTO: set from definition |
| Definition weapons not in LoadoutBridge | AUTO: copy melee/ranged/prefer into the loadout |
| Starting weapon Drawn_ slot not active | AUTO: sync slot visuals and arm the starting weapon |
| Crossed-arm idle (hand distance under 0.35) with a creator-generated avatar and a Last Model Source | AUTO: rebuild the avatar T-pose in place (avatar GUID and prefab untouched) |
| Animator culling not AlwaysAnimate | AUTO: set AlwaysAnimate |
| Animator controller missing | AUTO: copy the template's controller |
| Weapon holders / Drawn_ slot missing | MANUAL: Rebuild From Template (may orphan scene children; scene count shown there) |
| Singular-template extras (vShooterManager, DMEnemyHitMarks, ...) | MANUAL: migration by rebuild |
| Definition validation problems (e.g. Lisa_Hybrid Ranged = Wood Axe) | MANUAL: edit the definition |
| No definition exists, no valid humanoid avatar, avatar not a saved asset, crossed arms with an FBX avatar, missing bootstrap/loadout, any other error | MANUAL: Rebuild or fix by hand |
| Protected prefab | Never modified; informational only |

Players: no Fix button (the player list only checks avatar/controller); findings are informational.

Tested on throwaway copies only (deleted afterwards): a deliberately broken fresh build (link removed, capsule 0.9, culling changed, loadout cleared, weapon slot inactive, crossed-arm avatar) went from 1 error to OK with hands 0.15 to 0.57, GUID kept, 0 object ids lost; a copy of Axe_Droid went from 2 warnings to 1 (the missing weapon holders stay manual). Also fixed while testing: the in-place avatar rewrite left the avatar's internal name as `__dm_tmp_avatar` (importer warning); code fixed and the Axe_Droid and Humadroid avatar assets renamed correctly.

Current real prefabs: no Fix was run on them. Auto-fixable today: Axe_Droid (root capsule), Humadroid, Lisa_Hybrid (culling), corrupt_patrol_android, Humanoid, Hybrid Droid, Robot, The_Evil_One (culling + arm starting weapon slot).
