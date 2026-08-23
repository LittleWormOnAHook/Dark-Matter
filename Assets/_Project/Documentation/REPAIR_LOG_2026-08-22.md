# Dark Matter Genesis Repair Log — 2026-08-22

Working copy: `A:\dark Matter Genesis`  
Branch: `restore/v16-midnight-local` @ `a76ce96fb` (plus local dirt)  
Unity: 6000.4.11f1 HDRP  
Constraint: no commit, no push, no Unity launch.

---

## 1. CRITICAL — Inventory drop NRE (Quora Shelter / box2)

- **id:** 1-inventory-drop-nre
- **severity:** CRITICAL
- **status:** DONE
- **files changed:**
  - `Assets/_Project/Scripts/Inventory/InventorySystem.cs`
  - `Assets/_Project/Data/Items/Resources/Quora Shelter.asset`
- **what was wrong:**
  `SpawnDroppedItem` instantiated `worldPrefab` then `AddComponent<ItemPickup>()` before any collider existed. `ItemPickup` has `[RequireComponent(typeof(Collider))]`. Quora Shelter's `worldPrefab` pointed at `box2.prefab` (`guid d39eb7fdfc7cc4b4ba472bd562193eb3`) which is MeshFilter+MeshRenderer only. Unity logged "Adding component failed on box2(Clone)" and NRE'd at the pickup assign / `PrepareForWorldDrop`. Ghost meshes could remain.
- **what changed:**
  - Call `EnsureDroppedPhysicsAndPickup` (adds a trigger `SphereCollider` when none exists) *before* attaching `ItemPickup`.
  - Guard `AddComponent<ItemPickup>`: if still no collider, add a trigger sphere; if attach still fails, `Destroy` the instance and return false so the inventory item is not removed.
  - Retargeted `Quora Shelter.asset` `worldPrefab` from box2 (`170598` / `d39eb7fdfc7cc4b4ba472bd562193eb3`) to `Quora Shelter_World.prefab` (`5590124727653140618` / `896b4ed8564e08b43963f11ec039d336`), which already has a trigger `BoxCollider` + `ItemPickup`.
- **how to verify in play mode:**
  1. Give the player a Quora Shelter (inventory or cheat).
  2. Drop it from the inventory UI.
  3. Expect: one world pickup (shelter mesh, not a raw box2), trigger collider present, item removed from inventory, no "Adding component failed" / NRE in Console.
  4. Walk up and E-collect: item returns to inventory; no ghost leftover.

---

## 2. HIGH — Resource nodes break when resized

- **id:** 2-resource-node-scale
- **severity:** HIGH
- **status:** DONE
- **files changed:**
  - `Assets/_Project/Scripts/Interaction/ResourceNode.cs`
  - `Assets/_Project/Scripts/Interaction/ResourceNodeInteractionVolume.cs` (NEW; Unity will generate `.meta`)
  - `Assets/_Project/Scripts/Interaction/DMIMiningController.cs`
  - `Assets/_Project/Scripts/Interaction/DMIMiningResourceScanner.cs`
  - `Assets/_Project/Editor/ResourceManagerWindow.cs`
- **what was wrong:**
  A) `EnsureMineralMeshCollider` deleted boxes and left non-convex MeshColliders that miss rays under non-uniform scale (template 0.5/0.3/0.5, Silicate 0.3/0.18/0.3, scene IronOre (2) 1.20/0.72/1.20).
  B) Harvest/scan/mine used unscaled world meters from `transform.position` / AABB center (hold 3.5, max 6, min standoff 2, 1.1m aim slop).
  C) Plants had a root BoxCollider while the mesh lived on Visual; scaling Visual left the box behind. WebPlant box was solid.
- **what changed:**
  - `ResourceNode.GetInteractionCollider` / `GetClosestPoint` (child colliders else renderer AABB).
  - Hold reach (`CanBeginHold`, `TickHold`, `GetUsePriority`) is distance to closest surface point. 3.5m is reach-past-surface.
  - Dropped 1.1m center slop: aim accepts a collider ray hit or a 0.35m AABB graze.
  - `GetNodeCenter()` still uses renderer bounds center for VFX/UI.
  - Mining + scan lock/standoff use player → closest point. Max 6m kept. Min 2m is "don't stand inside" via closest-point distance.
  - After ray/sphere miss, first acquire falls back to `TryGetLockPointOnNode` on nearby nodes.
  - Mine acquire/lock raycasts use `QueryTriggerInteraction.Collide` (visual world-beam sparks still Ignore). HoldHarvest still no-ops mine.
  - New `ResourceNodeInteractionVolume` added at play in `ResourceNode.OnEnable` (no scene YAML edits). OnEnable / lossyScale change / OnValidate encapsulate child renderer bounds into a root trigger BoxCollider. Mesh colliders left intact.
  - Editor `EnsureMineralMeshCollider` no longer destroys BoxColliders; keeps/creates a fitted interaction box. `EnsurePlantTriggerBox` kept; runtime volume also refits Visual-only scale.
  - Scene instance scales were not retouched.
- **how to verify in play mode:**
  1. Enter play. Select a scaled IronOre / Silicate boulder and a scaled plant (WebPlant / Visual-scaled). Confirm a root trigger BoxCollider now fits the mesh (InteractionVolume added at runtime).
  2. Stand ~3m from a large boulder's surface (center may be farther) and Hold-F scan / laser mine — lock should work; standing inside should fail standoff.
  3. Aim at the mesh (not the old center slop): harvest E-hold and mine lock only when the reticle is on/near the AABB.
  4. Scale a node's Visual child in play — volume should refit next LateUpdate. Walk collision mesh colliders remain.
  5. No mass scene dirty on enter play (volume is runtime-added).

---


## 3. HIGH — Play hitch / memory (safe subset)

- **id:** 3-hitch-memory-safe
- **severity:** HIGH
- **status:** DONE (partial; deletions SKIPPED)
- **files changed:**
  - `ProjectSettings/QualitySettings.asset`
- **what was wrong:**
  All 5 quality levels had `streamingMipmapsActive: 0`. Duplicate Meshy FBX trees exist under both `Assets/_Project/Models` and `Assets/_Project/Prefabs/Models` (same filenames, matching byte sizes).
- **what changed:**
  - Enabled mip streaming on all 5 quality levels (`streamingMipmapsActive: 1`).
  - **Did NOT change `m_CurrentQuality` (still 3)** so Anthony is not surprised mid-session.
  - **Did NOT delete** any Prefabs/Models FBX. GUID retarget across prefabs/scenes is risky; Brimmy / unique meshes must stay. Duplicates listed below for a later dedicated pass.
- **duplicate FBX list (same filename + same size; SKIPPED deletion):**
  - Apex Legends Heirlooms Sky Piercer / Twin Razor / Mozambique Shotgun
  - Brimmy.fbx (do not delete — unique art)
  - CAT-350, DM Rifle, Drill aniamted + Drill Animations set (Door/Lower/Upper/Static/1a/Dirt Ring)
  - Ember Skitter attack/base/idle/walk
  - Meshy_AI_Character_output, Meshy_AI_Drill_Static, Emberclad Dragon biped set (5 anims + character)
  - Meshy_AI_Futuristic_Cyberpunk (Ammo Crate), Meshy_AI_Neon_Quantum_Laser
  - Player v4, Replicator anims, scannerf, Troll
  - Sulfur Hound: not in this duplicate set (unique; not touched)
- **how to verify in play mode:**
  1. Play — quality tier should still be the same (index 3). Memory/hitch may improve as mips stream.
  2. Confirm Console has no new missing-mesh errors (nothing was deleted).

---

## 4. MEDIUM — Settings Apply toast + FastPlay statics

- **id:** 4-toast-fastplay-statics
- **severity:** MEDIUM
- **status:** DONE
- **files changed:**
  - `Assets/_Project/Scripts/UI/PickupToastUI.cs`
  - `Assets/_Project/Scripts/Core/SettingsSceneReloader.cs`
- **what was wrong:**
  FastPlay / scene reload left `PickupToastUI.instance` pointing at a dead object. `Show` could toast on an unloaded canvas. Settings Apply toasted after reload onto a dead singleton.
- **what changed:**
  - `PickupToastUI`: `RuntimeInitializeOnLoadMethod(SubsystemRegistration)` + `sceneUnloaded` null the static instance.
  - `Show` resolves a live canvas, activates the full parent chain, then `EnsureExists`.
  - `EnsureExists` treats Unity fake-null / destroyed instances as missing.
  - `SettingsSceneReloader`: domain-reload clears pending statics (scene-reload flags still work — no domain reload on Apply). Toast goes through `TryShowSettingsAppliedToast` which requires a live canvas + `EnsureExists`.
- **how to verify in play mode:**
  1. Apply graphics settings in-game — after reload, "Settings applied. Progress saved." should appear on the main-menu canvas, no missing-ref / NRE.
  2. FastPlay into a gameplay scene, pick up an item — toast still shows.

---

## 5. MEDIUM — URP leftovers / Shader.Find

- **id:** 5-shader-find-cache
- **severity:** MEDIUM
- **status:** DONE (URP package removal DEFERRED)
- **files changed:**
  - `Assets/_Project/Scripts/Inventory/InventorySystem.cs`
  - `Assets/_Project/Scripts/Interaction/DMIMiningController.cs`
  - `Assets/_Project/Scripts/Interaction/ScannerWorldHighlight.cs`
  - `Assets/_Project/Scripts/Interaction/ResourceLootAttractVfx.cs`
  - `Assets/_Project/Scripts/Combat/EnemyLootBag.cs`
- **what was wrong:**
  Runtime `Shader.Find` fallbacks on every drop / laser / loot orb / bag (URP names fail in HDRP player builds; repeated Find is hitchy).
- **what changed:**
  Serialized Shader fields default via `Shader.Find` once in Awake/OnEnable, then a static cache. Hot paths use the cache.
  **`com.unity.render-pipelines.universal` left in `Packages/manifest.json` (17.4.0).** Removal deferred — too risky mid-HDRP migration without Anthony in the Editor.
- **how to verify in play mode:**
  1. Drop an item with no worldPrefab (cube ghost) — material still appears.
  2. Fire mining laser — beam visible.
  3. Mine a node — loot orb flies.
  4. Kill an enemy — loot bag visible. Player build should not lose these mats.

---

## 6. MEDIUM — Directors/Comms/Experience stubs

- **id:** 6-directors-stubs
- **severity:** MEDIUM
- **status:** DEFERRED
- **files changed:** none
- **what was wrong:**
  Unfinished feature (`StubDirector` is a no-op until domain logic lands). Not a v1.6 defect.
- **what changed:**
  Did **not** implement World Engine. No one-line README exists under `Assets/_Project/Features` to annotate. Existing stub comment kept: "No-op director used until domain logic lands."
- **how to verify in play mode:**
  N/A — deferred by design.

---

## 7. MEDIUM — Mine/harvest SFX silent in player builds

- **id:** 7-harvest-sfx-resources
- **severity:** MEDIUM
- **status:** DONE
- **files changed:**
  - `Assets/_Project/Scripts/Interaction/ResourceNode.cs`
  - `Assets/_Project/Resources/Audio/Break Stone.wav` (NEW copy)
  - `Assets/_Project/Resources/Audio/Break Wood Effect.wav` (NEW copy)
- **what was wrong:**
  `LoadBuiltinClip` used `#if UNITY_EDITOR AssetDatabase.LoadAssetAtPath`. Player builds got null clips (silent mine/harvest yield).
- **what changed:**
  Copies of `Assets/Audio/Others/Break Stone.wav` (696 KB) and `Break Wood Effect.wav` (141 KB) into `Assets/_Project/Resources/Audio/` (originals kept; not already under Resources). `LoadBuiltinClip` now `Resources.Load<AudioClip>("Audio/Break Stone")` / `"Audio/Break Wood Effect"`. Unity will generate `.meta` on next Editor refresh.
- **how to verify in play mode:**
  1. Laser-mine a boulder — Break Stone plays at the node when a wave grants.
  2. Hold-E harvest a plant — Break Wood Effect plays.
  3. Confirm the same in a player build (not just Editor).

---

## 8. LOW — IMGUI crosshair

- **id:** 8-imgui-crosshair
- **severity:** LOW
- **status:** DONE
- **files changed:**
  - `Assets/_Project/Scripts/UI/RangedCombatHud.cs`
- **what was wrong:**
  `OnGUI` always drew an IMGUI crosshair when `showCrosshair` was true, even if a UGUI/optics crosshair was already on screen.
- **what changed:**
  Left OnGUI (no dedicated hip-fire UGUI HUD exists besides `TooManyCrosshairs` / optics library). Skip OnGUI when an active UGUI `Graphic` named *crosshair* is present. Cached `Texture2D.whiteTexture`. Did not invent a new HUD. Default `showCrosshair` stays true so hip-fire still has a reticle when no UGUI one is live.
- **how to verify in play mode:**
  1. Draw a ranged weapon with no UGUI crosshair — IMGUI + still shows.
  2. If an optics/UGUI widget named Crosshair is active, IMGUI + should not double-draw.

---

## 9. LOW — ItemDataCreatorWindow layout poison

- **id:** 9-itemdata-layout-cleanup
- **severity:** LOW
- **status:** DONE (runs on next Editor load)
- **files changed:**
  - `Assets/_Project/Editor/ObsoleteEditorWindowLayoutCleanup.cs`
- **what was wrong:**
  Saved `.wlt`/`.dwlt` layouts still reference obsolete `ItemDataCreatorWindow` / "Item Data Creator", causing Play Mode FinalizePlaymodeLayout invalid-window errors.
- **what changed:**
  Bumped SessionState PrefKey `DM.LayoutCleanup.ItemDataCreator.v2` → `v3` so `[InitializeOnLoad]` runs again on next Editor load. **Did not edit Library/*.dwlt blindly.** Cleanup path: `Assets/_Project/Editor/ObsoleteEditorWindowLayoutCleanup.cs` scrubs UserSettings/Layouts, `_Project/Scenes` layouts, and known Unity layout folders, remapping the obsolete type to Inspector.
- **how to verify:**
  1. Restart / re-enter the Editor (do not need Play).
  2. Console should log `ObsoleteEditorWindowLayoutCleanup: scrubbed ...` if a leftover pane was found.
  3. Enter Play — no ItemDataCreatorWindow invalid-window errors.

---

## 10. LOW — Book of the Dead shadergraphs

- **id:** 10-book-of-the-dead
- **severity:** LOW
- **status:** SKIPPED / ignored
- **files changed:** none
- **what was wrong:**
  Gaia install cache (`Assets/Procedural Worlds/Packages - Cache`), not in the v1.6 scene.
- **what changed:**
  Nothing. Left Gaia cache alone.
- **how to verify in play mode:**
  N/A — not in v1.6.

---

## File change list (all items)

1. `Assets/_Project/Scripts/Inventory/InventorySystem.cs` (items 1, 5)
2. `Assets/_Project/Data/Items/Resources/Quora Shelter.asset` (item 1)
3. `Assets/_Project/Scripts/Interaction/ResourceNode.cs` (items 2, 7)
4. `Assets/_Project/Scripts/Interaction/ResourceNodeInteractionVolume.cs` (NEW, item 2)
5. `Assets/_Project/Scripts/Interaction/DMIMiningController.cs` (items 2, 5)
6. `Assets/_Project/Scripts/Interaction/DMIMiningResourceScanner.cs` (item 2)
7. `Assets/_Project/Editor/ResourceManagerWindow.cs` (item 2)
8. `ProjectSettings/QualitySettings.asset` (item 3)
9. `Assets/_Project/Scripts/UI/PickupToastUI.cs` (item 4)
10. `Assets/_Project/Scripts/Core/SettingsSceneReloader.cs` (item 4)
11. `Assets/_Project/Scripts/Interaction/ScannerWorldHighlight.cs` (item 5)
12. `Assets/_Project/Scripts/Interaction/ResourceLootAttractVfx.cs` (item 5)
13. `Assets/_Project/Scripts/Combat/EnemyLootBag.cs` (item 5)
14. `Assets/_Project/Resources/Audio/Break Stone.wav` (NEW copy, item 7)
15. `Assets/_Project/Resources/Audio/Break Wood Effect.wav` (NEW copy, item 7)
16. `Assets/_Project/Scripts/UI/RangedCombatHud.cs` (item 8)
17. `Assets/_Project/Editor/ObsoleteEditorWindowLayoutCleanup.cs` (item 9)
18. `Assets/_Project/Documentation/REPAIR_LOG_2026-08-22.md` (this log)

**Not changed:** Packages/manifest.json (URP stays), scene YAML scales, Prefabs/Models FBX, Sulfur Hound / Brimmy meshes, Book of the Dead / Gaia cache. No commit, no push, Unity was not launched.

---

## Status summary 1–10

| # | Status | Verify |
|---|--------|--------|
| 1 | DONE | Drop Quora Shelter → one world pickup, no NRE |
| 2 | DONE | Scaled ore/plant: scan/mine/harvest to surface, 6m max, 2m standoff |
| 3 | DONE / SKIP delete | Quality still 3; mip streaming on; dups listed only |
| 4 | DONE | Settings Apply toast after reload; pickup toast after FastPlay |
| 5 | DONE / URP DEFERRED | Laser / drop / loot orb / bag mats; URP package stays |
| 6 | DEFERRED | World Engine not implemented |
| 7 | DONE | Mine/harvest SFX in Editor and player build |
| 8 | DONE | IMGUI + only when no UGUI Crosshair |
| 9 | DONE | Next Editor load runs layout cleanup v3 |
| 10 | SKIPPED | Gaia cache ignored |

