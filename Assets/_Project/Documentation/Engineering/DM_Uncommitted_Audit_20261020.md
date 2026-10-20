# Uncommitted Work Audit — 2026-10-20

Read-only audit of `A:\Dark Matter Genesis`, branch `cursor/wip-clean-20260919`, HEAD `af5ae31ba` (1 ahead of origin, not pushed).
Nothing was staged, committed, deleted, or run in Play. About 2,000 status entries, plus `.meta` files.

**Method**
- **Status:** `git status --porcelain --untracked-files=all`.
- **GUID closure:** each candidate's `.meta` GUID was searched for in a set of roots:
  - the v1.6.6 scene
  - `Player_v7.prefab` and `Player_v7 Combat Variant.prefab`
  - `_Project/Resources/**`, `_Project/Data/**`, `_Project/Prefabs/Items/**`
  - `ProjectSettings` and `Assets/Settings`

  The search then followed references through the uncommitted files themselves. This is a transitive closure over the YAML assets.
- **Project-wide probe:** a second pass searched all YAML under `Assets` and `Packages` for a sample of terrain, font, scene and lightmap GUIDs.
- **Code:** dependencies come from text search, and compile state from the editor console: 0 errors / 0 warnings today on the full working tree.

**Limits**
- Binary assets were not parsed: `LightingData.asset`, FBX internals, and `TerrainData` stored as binary.
- `Resources.Load` by name is only noted where it applies.
- No prefabs were loaded.

**Legend:** **Commit** = commit it. **Anthony** = his to commit. **Ignore** = add to `.gitignore`. **Delete?** = candidate for deletion.

---

## TL;DR

1. **HEAD is not clone-safe today.** Committed `DMTurretRuntime.cs` and `DMTurretForgeFxCleanup.cs` use `F3DPoolManager`. That class lives in `Assets/FORGE3D/PoolManager/`, which is entirely **untracked** (0 files tracked). The prefabs from the committed vendor ammo also reference untracked FORGE3D scripts, prefabs, materials and sounds.
   - **First thing to commit:** `Assets/FORGE3D/PoolManager/**`, `Sci-Fi Effects/Code/**`, and the FORGE3D files the closure reaches (list in §4.1).
2. **Commit as one "build/grenade" set:**
   - **Scripts:** the Building changes, `DMIGrenadeExplosive`, `DMEnemyGrenadeBlastReaction`, `DMIGrenadeThrowLandingRing`, and the input bridge/binder.
   - **Assets they need:** `Camp Build Zone Seed.asset`, `skill_grenade_cook_mastery.asset`, `SkillRegistry`, `ItemRegistry`, the Building profiles, and `DM_Frag_Grenade.prefab`.

   These pieces cross-reference each other.
3. **Anthony's own files:** scene, `Player_v7` prefabs, `Kade v2`, `Fred`, enemy data, lightmaps/APV, and ProjectSettings. Two things to check:
   - **Tripo:** `Packages/manifest.json` points at a local `file:A:/Unity Assets/...` path, and `Kade v2` and the scene use the Tripo FBX. Commit them together or not at all.
   - **APV baking set:** the scene's `m_LightingDataAsset` is `{fileID: 0}`, so the v1.6.6 lightmap folder is not linked from this scene.
4. **Do not commit:**
   - **Ignore or delete:** Screenshots, `Packages/com.jbooth.microsplat.texture-clusters/` (no `package.json`, so Unity never loads it as a package), FORGE3D demo scenes, and the `New *` placeholder assets.
   - **Delete the stray files:** `Test.asset`, `DMEnemyRuntimeAudit.PlayerArm.cs` (3 bytes), and the `New Text Document.MD`.

---

## 1. Scripts (modified + new)

**Compile:** the full working tree compiles (0/0). The open question is whether a commit that leaves some of these out still compiles.

| Path | In use? (evidence) | Depends on | Rec |
|---|---|---|---|
| `Scripts/Building/DMBuildHub.cs`, `DMBuildingCatalog.cs`, `DMBuildingGhostProfile.cs`, `DMBuildingPlacementController.BuildZoneRule.cs`, `.Equipment.cs` | Y. Compiled; `DMBuildingGhostProfile` is used by `Resources/Building/DM_BuildingGhostProfile.asset`. | Each other. New ghost-profile fields are serialized in the modified `DM_BuildingGhostProfile.asset` and `DM_BuildingStyle_Stone.asset`. The build-zone rule is fed by `Camp Build Zone Seed.asset` (in `ItemRegistry`). | Commit as one set with the assets in §2 |
| `Editor/Building/DMBuildingStudioWindow.cs`, `Editor/GenesisStudio/GenesisStudioWindow.cs`, `Editor/ItemDataEditor.cs` | Y (editor) | Building and `ItemData` changes | Commit with the Building set |
| `Scripts/Combat/DMIGrenadeExplosive.cs` (+394/−?) | Y. Used by `DM_Frag_Grenade.prefab`, which `Player_v7` uses. | **Needs the untracked `DMEnemyGrenadeBlastReaction.cs`** (it would not compile without it). | Commit together |
| `Scripts/Combat/DMEnemyGrenadeBlastReaction.cs` (new) | Y. Called from `DMIGrenadeExplosive`. | `EnemyInvectorRagdollBridge.TryGuardBreakStagger` | Commit. Check that the `RagdollBridge` member exists in HEAD. |
| `Scripts/Combat/DMIGrenadeThrowLandingRing.cs` (new) | Y. On `Player_v7.prefab` (GUID). | Invector `vThrowManager` | Commit. Without it, `Player_v7` gets a missing script. |
| `Scripts/Player/Invector/PioneerInvectorInputBridge.cs`, `PioneerPlayerInputBinder.cs` | Y (`Player_v7`) | `DMBuildingMode.BlocksWeaponFireInput` (in HEAD) and grenade cook | Commit with the grenade set |
| `Scripts/Player/Invector/PioneerShooterMeleeInput.cs` (unstaged leftovers: grenade/building/aim hunks) | Y (`Player_v7`) | `BlocksWeaponFireForGrenade` (HEAD), `DMBuildingMode` | Commit with the grenade set |
| `Scripts/Data/ItemData.cs`, `Inventory/InventoryItemActions.cs`, `InventorySystem.cs`, `UI/ItemTooltipFormatter.cs`, `UI/InventoryContextMenu.cs`, `Interaction/WorldUseController.cs` | Y (`Player_v7`, item assets) | New `ItemData` fields are probably serialized in `ItemRegistry` and Camp Seed | Commit with Building/items |
| `Scripts/Progression/PlayerSkillAllocator.cs`, `SkillDefinition.cs` | Y (skill assets) | `skill_grenade_cook_mastery.asset`, `SkillRegistry` | Commit with the grenade set |
| `Scripts/AI/Invector/EnemyInvectorRagdollBridge.cs` | Y (enemy prefabs, e.g. `Fred.prefab`) | Possibly `DMEnemyGrenadeBlastReaction` | Anthony's WIP; commit with the grenade set |
| `Scripts/AI/Validation/DMEnemyCollisionWalkValidation.cs`, `Companions/PioneerExpeditionCommandInput.cs`, `Player/PlayerController.cs`, `UI/GameStartPopup.cs`, `UI/GameplayMenuTime.cs` | Y (compiled; `PlayerController` is on `Player_v7`) | Small edits | Commit (review diffs) |
| `Scripts/Diagnostics/DMPlayerMovementPathDebug.cs` (new) | Partly. Referenced by committed `Player/PlayerPathTrail.cs`? (text match; confirm); not on any root prefab. | — | Commit if `PlayerPathTrail` uses it, otherwise Anthony decides |
| `Invector-3rdPersonController/.../vMeleeManager.cs` | Y | Vendor edit | Anthony (record it in the vendor manifest) |
| `Assets/DMEnemyRuntimeAudit.PlayerArm.cs` (new, 3 bytes, at the Assets root) | N (empty) | — | **Delete?** |
| `Assets/FORGE3D/PoolManager/*.cs`, `Sci-Fi Effects/Code/*.cs` (untracked) | **Y. Committed HEAD turret code calls `F3DPoolManager`.** | — | **Commit now (HEAD is broken without it)** |

## 2. Data and profile assets

Root assets (everything under `Resources/`, plus registries) count as in use through `Resources.Load` or the registry, even when no GUID points at them.

| Path | In use? | Rec |
|---|---|---|
| `Resources/ItemRegistry.asset` | Y (registry; refs `Camp Build Zone Seed`) | Commit with `Camp Build Zone Seed.asset` |
| `Data/Items/Operations/Camp Build Zone Seed.asset` (new) | Y, by `ItemRegistry` | Commit |
| `Resources/Progression/SkillRegistry.asset` + `Skills/Rifles/skill_grenade_cook_mastery.asset` (new) | Y, the registry refs the new skill | Commit together |
| `Resources/Building/DM_BuildingGhostProfile.asset`, `Styles/DM_BuildingStyle_Stone.asset` | Y (Resources) | Commit with the Building scripts |
| `Resources/Combat/DM_ScopeProfile.asset` | Y; refs untracked `TooManyCrosshairs/.../ChevronScopeOuter2048 1.png` | Commit along with that PNG |
| `Resources/Combat/DM_TurretProfile.asset`, `DefaultBullet.prefab` | Y (Resources) | Commit (turret work) |
| `Resources/Achievements/AchievementRegistry.asset`, `Player/DM_FootstepProfile.asset`, `Pets/Definitions/brimmy.asset` | Y (Resources / `Brimmy.prefab`) | Commit (review diffs) |
| `Resources/Dash/Sparks-SmokeHits.prefab` (new) | Y, by `DMAmmoFxProfile_CarveExperiment` | Commit |
| `Resources/Dash/SparksHit.prefab` (new) | N (no GUID refs; maybe `Resources.Load` by name, unverified) | Anthony / Delete? |
| `Data/Items/ammo/DMAmmoFxProfile_*.asset` (6 modified) | Y | Commit. **Note:** git tracks a lowercase `ammo/` path while the disk folder is `Ammo/`. This is a case-only duplicate, so normalise with `git mv` first. |
| `Data/Enemies/*.asset` (8 modified) | Y (`EnemyRegistry`; `The_Evil_One` ← `Fred.prefab`) | Anthony (enemy data; weapon placements are off-limits) |
| `Data/Enemies/Test.asset` (new) | N | **Delete?** |
| `AddressableAssetsData/AddressableAssetSettings.asset` | Y (global) | Anthony (review) |
| `Settings/HDRP/Genesis_HDRP_High/Performance.asset`, `HDRPDefaultResources/DefaultSettingsVolumeProfile.asset` | Y (scene / quality) | Anthony (pair with QualitySettings) |
| `Documentation/Audits/DM_Enemy_Runtime_Audit_*.json/.md` | N (generated reports) | Commit or Ignore (regenerated every run, so consider `.gitignore`) |
| `Documentation/INDEX.md` (remaining turret lines), `Engineering/Grok_Session_Handoff_2026-10-18_Turrets.md` | Docs | Commit together |
| `Features/PPT/Documentation/New Text Document.MD` | N | **Delete?** |

## 3. Anthony's own: prefabs, scene, lightmaps, ProjectSettings (recommendation only)

| Path | In use? | Rec |
|---|---|---|
| `Scenes/Dark Matter Genesis v1.6.6.unity` | Y (main scene) | Anthony. Commit after the FORGE3D/Tripo/Kade/Fred dependencies are in, or the scene will carry missing refs. |
| `Prefabs/Players/Player_v7.prefab`, `Player_v7 Combat Variant.prefab` | Y (scene → Variant → v7) | Anthony. Needs `DMIGrenadeThrowLandingRing.cs` and the grenade prefab committed. |
| `Prefabs/Players/Kade v2.prefab` (new) | Y, placed in the v1.6.6 scene; uses the Tripo FBX | Anthony, together with `TripoModels/` |
| `Prefabs/Combat/Enemies/Fred.prefab` (new) | Y, in the v1.6.6 scene | Anthony |
| `Prefabs/Combat/Throwables/DM_Frag_Grenade.prefab` | Y (`Player_v7`) | Anthony / the grenade set |
| `Prefabs/Items/Ammo/Vendor/*_Beam`/`_Muzzle.prefab` (9) | Y (vendor `DMAmmoFxProfile_*`) | Commit (vendor-ammo follow-up; they need FORGE3D) |
| `Scenes/Dark Matter Genesis v1.6.6/` lightmaps, `LightingData.asset`, APV Baking Set + `.bytes` (~136 MB) | **Unclear / N from this scene.** The scene's `m_LightingDataAsset` is `{fileID: 0}`; only `Assets/_Recovery/0 (75).unity` references `LightingData` and the Baking Set. | Anthony. Either re-link and commit (consider LFS: no `.gitattributes` exists) or leave it out until the bake is final. |
| `ProjectSettings/GraphicsSettings.asset` (render pipeline asset GUID swapped) | Y | Anthony. Confirm it's the intended HDRP asset. |
| `ProjectSettings/ProjectSettings.asset` (`m_APIs` order changed) | Y | Anthony. Likely revert (graphics API reorder side-effect). |
| `ProjectSettings/QualitySettings.asset` (current quality 3→1, `lodBias` 1.2/1.8→1, platform defaults) | Y | Anthony. Looks like an editor-session side effect; review before committing. |
| `ANGRY MESH/.../Sources` materials (40 modified) | 2/39 reached from roots | Anthony (his WIP) |
| Misc vendor material/prefab edits (Scifi Melee Weapons mats, WarFX impact, Magic Spells ×2, Malbers Trail, QFX CalibrationFloor, PolygonSciFi glow, SCF decals, Invector ScopeLens/ConcreteDoorWall, TMP Fallback) | Mostly Y (scene / ammo FX). `SCF_Decals_B.mat` not reached. | Anthony. These are usually reserialisation/shader-upgrade noise; commit only intentional ones. |

## 4. Bulk

### 4.1 FORGE3D (912 MB on disk, 363 files tracked, ~1,300 untracked)

Reached by the closure from committed vendor ammo prefabs, the scene and Resources (the `.meta` files are needed too):

| Type | Used / untracked |
|---|---|
| `.cs` | 16 / 46 (`PoolManager/F3DPoolManager`, `Code/F3DBeam`, `F3DProjectile`, `F3DLightning`, `F3DFlameThrower`, `F3DShotgun`, `F3DFXController`, `F3DAudioController`, `F3DDespawn`, `F3DRandomize`, `F3DTime`, `Turrets/F3DTurret*`, `Player/F3DPlayerTurretController`) |
| `.prefab` | 73 / 190 |
| `.wav` | 48 / 66 |
| `.mat` / `.tga` / `.png` / `.psd` / `.fbx` / `.asset` | 15/119, 8/107, 5/19, 2/5, 1/24, 1/10 |
| `.unity` demo scenes (Burnout, Debris Field, Effects, Holographic, Missiles, Nebula, Turrets, Warp Jump, Warp Tunnel) | **0 / 9**, unused |
| `.shader`, `.vfx`, `.shadergraph`, `.anim`, `.controller`, `.exr` | 0 used by GUID (shaders are still needed by name via materials, so keep them) |
| `HelpMenu/`, `Readme.*`, `Install.txt`, `Resources/F3DPoolManagerCache/*.asset` | Not referenced. `F3DPoolManagerCache` is loaded by `F3DPoolManager` via Resources: **keep**. |

**Rec:**
- **Commit (vendor):**
  - all of `FORGE3D/PoolManager/**`, `Sci-Fi Effects/Code/**`, `Shaders/**`, `Resources/**`
  - the referenced prefabs, materials, textures and sounds
  - simplest option: all of `Sci-Fi Effects/{Effects,Turrets,Sounds,Misc,Examples,Code,Shaders}`, roughly 0.9 GB. Use LFS or a vendor-asset policy per `Engineering/Vendor_Assets_And_Git_Policy.md`.
- **Ignore or delete:** the 9 demo `.unity` scenes, `HelpMenu/`, `Readme.asset`/`.txt`, `Install.txt`, `Warp Tunnel/`.

### 4.2 MicroSplat texture clusters

| Path | In use? | Rec |
|---|---|---|
| `Assets/_Project/Textures/TextureClusters/**` (68 files, 96 MB) | Indirect only. `moss_01_diff` is used by `_TerrainAutoUpgrade/layer_moss_01…terrainlayer`, but **nothing references that layer** (the terrain may hold it in binary `TerrainData`, not verified). | Anthony. Commit if the v1.6.6 terrain uses these layers, else Delete? |
| `Packages/com.jbooth.microsplat.texture-clusters/` (`Samples~` 95 MB + `Textures/`) | **N.** There is no `package.json`, so Unity never imports it. The `Textures` files are byte-identical duplicates of the `Assets` copies. | **Delete?** (or Ignore) |

### 4.3 Fonts, Tripo, Screenshots, TerrainAutoUpgrade, TooManyCrosshairs

| Path | In use? | Rec |
|---|---|---|
| `_Project/UI/Fonts/Genesis/*.ttf` + `SDF/*.asset` + `Materials/*.mat` (4 MB) | N by GUID. The TTFs are used only by their own SDF assets; nothing references the SDFs or materials (UI Toolkit might load them by path, unverified). | Commit if planned for UI (small), otherwise Anthony |
| `TripoModels/rugged_male_character_3d_model/` (15 MB) | Y. The FBX is used by `Kade v2.prefab` and the scene; its loose `.fbm` textures and `.mat` are not referenced. | Anthony, together with `Kade v2` + manifest |
| `Assets/Screenshots/` (48 PNGs, 37 MB) | N | **Ignore** (move outside `Assets/`) |
| `_TerrainAutoUpgrade/*.terrainlayer` (12) | N (no YAML refs; maybe in binary `TerrainData`) | Anthony; Delete? if the terrain doesn't use them |
| `TooManyCrosshairs/2048px/Scopes/* 1.png` (3 duplicates) | Only `ChevronScopeOuter2048 1.png` is used (`DM_ScopeProfile`) | Commit that one; Delete? the other two |

### 4.4 Placeholder assets

| Path | In use? | Rec |
|---|---|---|
| `Assets/New Script Graph.asset`, `Assets/New Terrain Material.mat` (and the untracked `New Terrain.asset` / `New Terrain 1.asset` / `NewLayer.terrainlayer` at the Assets root, if untracked) | N | **Delete?** |
| `Assets/DMI Default.wlt` (editor layout) | N | Ignore |
| `Data/Enemies/Test.asset` | N | **Delete?** |

### 4.5 Legacy deletes (unstaged `D`)

| Path | Notes | Rec |
|---|---|---|
| `Assets/Dark Matter Genesis v1.56/` LightingData, OcclusionCulling, ReflectionProbe | Old scene bake data | Commit the deletions (Anthony) |
| `Assets/Pioneer v1.5.1/`, `v1.5.5/` NavMesh, `v1.5new/` LightingData/ReflectionProbe | Old bake data | Commit the deletions (Anthony) |
| `Assets/TutorialInfo/**`, `Assets/Readme.asset` | Unity template junk | Commit the deletions |
| `Assets/FlatKit` (2) | Check that nothing still uses FlatKit shaders | Anthony |

### 4.6 Packages

| Path | Change | Rec |
|---|---|---|
| `Packages/manifest.json` | Adds `com.tripo3d.unitybridge: file:A:/Unity Assets/Tripo3d_Unity_Bridge-latest (1)/…`, an **absolute local path**, so other clones will break | Anthony. Don't commit as-is: embed the package under `Packages/`, or keep it out of the committed manifest. |
| `Packages/packages-lock.json` | Matching Tripo entry | Same as above |

## 5. `.gitignore` / delete instead of committing

**Add to `.gitignore`:**
- `Assets/Screenshots/` (+ `.meta`)
- `*.wlt` at the Assets root
- `Packages/com.jbooth.microsplat.texture-clusters/`
- `Assets/_Recovery/`
- `rc_deploy/` backups, if not already ignored
- optionally the generated `Documentation/Audits/DM_Enemy_Runtime_Audit_Latest.*` / `_Units.json`

**Delete candidates (Anthony to confirm):**
- `Assets/DMEnemyRuntimeAudit.PlayerArm.cs` (3 bytes)
- `Data/Enemies/Test.asset`
- `New Script Graph.asset`, `New Terrain Material.mat`
- `Features/PPT/Documentation/New Text Document.MD`
- `Resources/Dash/SparksHit.prefab` (if unused)
- the two unused TooManyCrosshairs ` 1.png` duplicates
- FORGE3D demo scenes and `HelpMenu`
- the Packages MicroSplat folder (duplicate)

**Suggested commit order:**
1. FORGE3D runtime (code + referenced assets): fixes HEAD.
2. The grenade/building/items set: scripts + registries + skill + Camp Seed + Building profiles + `DM_Frag_Grenade` + leftover input hunks.
3. Turret/scope/dash resources + docs.
4. Anthony: scene, prefabs, Tripo, lighting, ProjectSettings, vendor material noise, legacy deletes.
