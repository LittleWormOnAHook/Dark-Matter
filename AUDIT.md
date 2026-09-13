# Dark Matter: Genesis — Runtime Performance & C# Architecture Audit

**Branch surveyed:** `backup/wip-pre-restore-20260907` (Cloud Agent workspace)  
**Scope:** `Assets/_Project/` gameplay/runtime C# (~806 scripts under `_Project`, excluding third-party packages except project misuse in hot paths)  
**Render pipeline:** HDRP-oriented project code (`ScannerHdrpOverlayGate`, HDRP material property IDs in `DMIMaterialPulseScroll`)  
**Unity:** 6000.x line (per team context)  
**Audit date:** 2026-09-13  
**Remediation pass:** 2026-09-13 — High findings H1–H6 addressed in code (see [Remediation](#remediation-2026-09-13) below).

This report was originally read-only; the remediation section tracks what landed on branch `cursor/unity-perf-architecture-audit-0b61`.

---

## Remediation (2026-09-13)

| ID | Status | Change |
|----|--------|--------|
| **H1** | **Fixed** | Added `Core/GameplayActorRegistry.cs` with OnEnable registration for pets, companions, creatures, scannables, pickups. `DMICreatureTargetResolver` reads registry + `PlayerReference` (no `FindObjectsByType`). `DMICreatureBridge` registers creatures (replaces internal `Live` list). |
| **H2** | **Fixed** | `ScannableTarget` registers with registry; `OpticsController.ScanScannableComponents` iterates `GameplayActorRegistry.ActiveScannables`. |
| **H3** | **Fixed** | `CompanionRosterBridge.Instance` + per-enemy cached bridge via `ResolveCompanionRosterBridge()`; `PioneerCompanionAgent.Health` avoids retarget `GetComponent`. |
| **H4** | **Fixed** | `Core/DmGroundProbe.cs` uses `RaycastNonAlloc`; `PlayerPathTrail` calls it (removed local `RaycastAll` + sort). |
| **H5** | **Fixed** | `ItemPickup` + `PetController` registry; `FindNearestPickup` scans `ActiveItemPickups`. |
| **H6** | **Fixed** | `ExposureStatusService` reuses per-slot buff/debuff buffers via `CopyTickScratch` (same pattern as player ticks). |
| **Extra** | **Fixed** | `DMIGrenadeExplosive` → `OverlapSphereNonAlloc`; `InventorySystem` drop grounding → `RaycastNonAlloc`; `HumanoidPerformanceController` prefers `PlayerReference` before `Camera.main`. |

Remaining Medium items (M3–M5, hold-harvest prompts, fog upload budget) still warrant Profiler passes on target hardware.

---

The codebase mixes a **mature gameplay layer** (`Assets/_Project/Scripts/`) with an early **World Engine spine** (`Assets/_Project/Features/`). Several systems already show deliberate perf hygiene (`SceneComponentCache`, phased enemy vision, humanoid distance culling, fog-of-war dirty uploads, `OverlapSphereNonAlloc` in combat). The largest **scalability risks** cluster around **scene-wide queries** (`FindObjectsByType`, `FindWithTag`, `FindAnyObjectByType`) still used in **AI, creatures, optics, and pet fetch**, and around **per-frame work on companions + player trail grounding** that allocates via `Physics.RaycastAll`. **Architecture debt** is dominated by **Invector bridge proliferation**, **parallel Pet vs Pioneer companion models**, and **central hubs** (`WorldUseController`, `UIManager` partials) that couple many domains.

---

## 1. Ranked findings

### High — likely FPS hitches or Script CPU that scales badly with entity count

| ID | Finding | Evidence (path) | Why it matters |
|----|---------|-----------------|----------------|
| H1 | **Creature threat resolution scans the whole scene** | `Assets/_Project/Scripts/Creatures/DMICreatureTargetResolver.cs` — `CollectPets`, `CollectCompanions`, `CollectOtherCreatures` each call `Object.FindObjectsByType<...>()`; invoked from `DMICreatureBridge.RefreshThreatTarget` (~every `targetRefreshInterval`, default **0.5s** per creature, `DMICreatureBridge.cs` L35, L107–111) and from Malbers brain `DMIIsValidSpitTargetDecision.Decide` (`Brain/DMIIsValidSpitTargetDecision.cs` L29–31) | Cost grows as **O(creatures × scene object count)**. A pack of sulfur hounds + surface encounters can spike Script CPU and cause staggered hitches. |
| H2 | **Optics scanner refresh uses uncached `FindObjectsByType<ScannableTarget>`** | `Assets/_Project/Scripts/Interaction/OpticsController.cs` — `ScanScannableComponents` L721–724; called from `RefreshScannerTargets` on **`scanRefreshInterval` 0.15s** while scanner active (L19, L167–170) | Full scene scan ~6–7×/second during scanner use. Physics path uses `OverlapSphereNonAlloc` (good), but component scan bypasses `SceneComponentCache`. |
| H3 | **Enemy pioneer retargeting repeats `FindAnyObjectByType<CompanionRosterBridge>`** | `Assets/_Project/Scripts/AI/EnemyAiController.CombatPositioning.cs` — `PickClosestNearbyPioneerWithin`, `PickRandomNearbyPioneer`, and related paths L265, L321, L349; `TryRetargetToNearbyPioneer` called from attack state (`EnemyAiController.States.cs` L360) | Each fighting enemy can trigger **uncached singleton lookup** plus `GetComponent<CompanionHealth>` per companion. Scales with **active combatants × companions**. |
| H4 | **Player path trail: `RaycastAll` + sort on movement samples** | `Assets/_Project/Scripts/Player/PlayerPathTrail.cs` — `LateUpdate` → `RecordCurrentPosition` → `SampleGroundHeight` L381–387 (`Physics.RaycastAll` + `Array.Sort`) | Runs whenever the player moves beyond `minRecordDistance`. **GC allocations** per sample; companions use trail backtracking (`CompanionFollowController.Movement.cs` references trail recovery). |
| H5 | **Pet fetch uses full-scene pickup query** | `Assets/_Project/Scripts/Pet/PetController.cs` — `FindNearestPickup` L750–752: `FindObjectsByType<ItemPickup>()` | Uncached; called on fetch attempts (not every frame, but can hitch when many pickups exist). |
| H6 | **Exposure HUD snapshot builds allocate `ToArray()` for companion buff/debuff ticks** | `Assets/_Project/Scripts/Survival/Exposure/ExposureStatusService.cs` — `BuildCompanionModifierSlots` L438–443; service refreshes on **`LateUpdate` every 0.1s** (L93–106) and on stat events | Player buff paths reuse buffers (`CopyScratchToBuffer` L380–388), but companion slots still **`ToArray()` per slot** when non-empty. With trio + exposure UI listeners, adds **steady GC pressure**. |

### Medium — meaningful cost or maintenance/perf coupling; worse under load

| ID | Finding | Evidence | Notes |
|----|---------|----------|-------|
| M1 | **`GameObject.FindWithTag("Player")` in AI senses fallback** | `EnemySenses.EnsurePlayer` L262–269; also `EnemyAiController.Threat.cs`, `CombatZoneController`, Invector motor bridge, pets, creatures (`DMICreatureTargetResolver` L107) | Fallback after cache miss; still a **tag walk** if `PlayerReference` not registered. Prefer single registrar (`PlayerReference.Register`). |
| M2 | **`Camera.main` in hot/adjacent paths** | Many files (e.g. `HumanoidPerformanceController.TryGetCameraTransform` L322–328, combat/UI bridges). `PlayerReference` caches camera with throttled resolve (`PlayerReference.cs` L37–50) but is **underused** | `Camera.main` compares tags on cameras each call. Cumulative cost across **many enemies** (humanoid perf) + UI world-to-screen. |
| M3 | **`FindAnyObjectByType` in prompt/hold paths** | `WorldUseController.ClearOwnedWorldPrompt` L1188–1189; multiple `EnsureExists` singletons across UI/roster | Less frequent than H1–H2 but adds latency spikes when first resolving UI. |
| M4 | **Hold-harvest `Update` rebuilds prompt context with raycast** | `WorldUseController.Update` L95–109 → `BuildPromptContext` L1194–1223 (raycast when gatherer present) | Cheap when idle; during **hold harvest** runs every frame. Prompt string building can scan cached interactables (`BuildWorldInteractionPrompt` L1286+). |
| M5 | **Map fog-of-war CPU + GPU upload** | `MapFogOfWar.LateUpdate` L95–110 — walk stamp + throttled `UploadTexture`; supports up to **4096²** (`ResolveFogResolution` L324–327). Dirty-rect path L530–537 (good) | Walking + scanning keeps mask dirty. High calibration resolution → **large `SetPixels32`/`Apply`** cost. Monitor with map open + exploration. |
| M6 | **Companion follow: per-frame terrain snap + movement depenetration** | `CompanionFollowController` — `Update` locomotion + `LateUpdate` `SnapToTerrain` (L461–464); movement partial uses static raycast buffer (good) but heavy logic (`CompanionFollowController.Movement.cs`) | **3 expedition companions ×** grounding/avoidance/step-back/trail recovery. Primary Script CPU during exploration. |
| M7 | **Invector `LateUpdate` recoil suppression while armed** | `PioneerShooterManager.LateUpdate` L64–85 — utility calls when `CurrentWeapon != null` or shooting | Extra work on **player camera stack** every frame while weapon drawn; acceptable for one player but competes with HDRP frame budget. |
| M8 | **Weapon aim laser `GetComponent` in `LateUpdate`** | `WeaponModeSwitchController.LateUpdate` L111–118 | Could cache `HitscanBeamMuzzleFollow` on laser root when aim mode toggles. |
| M9 | **Optics + scanner sweep mesh/terrain probing** | `ScannerSweepController` — coroutine rebuilds sweep disc mesh, topology raycasts (`RadialSegments` 64, rings 6); `RunSweep` L80+ | Burst cost during middle-mouse sweep, not continuous; still a **frame spike** risk on console. |
| M10 | **Exposure zone volumes: always-on `Update` when pulsing or occupied** | `ExposureZoneVolume.Update` L176–203 — shake, ambient, particles | Per-volume cost; many overlapping zones → multiply audio/particle/shake work. |
| M11 | **Grenade explosion uses allocating `OverlapSphere`** | `DMIGrenadeExplosive.cs` L321 (`Physics.OverlapSphere` returns new array) | Combat spike on throw; prefer `OverlapSphereNonAlloc` like `CombatProjectile.cs` L261. |
| M12 | **Inventory drop grounding uses `RaycastAll`** | `InventorySystem.TryGetDropGroundY` L769–774 | Drop bursts allocate; noticeable when dropping many items. |

### Low — hygiene, edge cases, or already mitigated

| ID | Finding | Evidence | Notes |
|----|---------|----------|-------|
| L1 | **`SceneComponentCache` mitigates interaction scans** | `Core/SceneComponentCache.cs`; used by `WorldInteractionDotUI`, `WorldUseController` | TTL **0.4s** default. Good pattern — extend to H2/H5/H1 where safe. |
| L2 | **Humanoid enemy distance culling (phased)** | `AI/HumanoidPerformanceController.cs` — interval + entity phase L14–16, L152–156; disables SMRs/animator at distance | Aligns with console budget thinking. Verify death/ragdoll path (`ForceVisibleForDeathPresentation`). |
| L3 | **Enemy vision throttling** | `EnemySenses` — `visionRefreshInterval` 0.12s, hash phase L18–19, L51 | Reduces LOS raycasts vs every-frame naive AI. |
| L4 | **HDRP scanner custom pass gated off** | `Rendering/ScannerHdrpOverlayGate.cs` — documents D3D12 crash; `enableDuringSweep` default false | Correct safety tradeoff; world-space sweep is active path. |
| L5 | **`GameObject.Find("MainCanvas")` at runtime** | e.g. `QuoraShelterController.cs` L346, several UI files | Typically **once** at open; low steady cost. |
| L6 | **Training dummy / editor utilities scan transforms** | `Combat/TrainingDummy.cs` L187 | Dev/prototype; exclude from shipping scenes. |
| L7 | **PPT registry index uses `FindObjectsByType` at build** | `PPT/Runtime/PptRegistryIndex.cs` | Likely startup/index rebuild, not per-frame — verify call sites. |
| L8 | **Partial-class splits for large controllers** | `EnemyAiController.*.cs`, `CompanionFollowController.*.cs`, `UIManager.*.cs`, `BuildingControlPanelUI.*.cs` | Helps navigation; signals **high cyclomatic complexity** in originals. |

#### Invited corrections (hunches downgraded after read)

- *SyncPoint / Unity Jobs*: No `IJob` / `SyncPoint` usage found under `_Project` gameplay scripts — **not a current risk**.  
- *LINQ in `Update`*: No widespread LINQ in Update loops; most LINQ/`ToArray` appears in **save/UI build/editor** paths or exposure snapshots (H6).  
- *Instantiate/Destroy thrash*: Combat leans on pools (`Core/GameObjectPool`, `PoolManager`, VFX caches); remaining `Instantiate` is mostly **deployment/UI spawn** — not the dominant hitch source on this branch.

---

## 2. Script structure map (how the game is organized)

### Top-level layout

| Area | Role | How systems talk |
|------|------|------------------|
| **`Assets/_Project/Scripts/`** | Primary gameplay (~34 topic folders) | MonoBehaviour singletons, static registries, C# events, direct references, `Find*` fallbacks |
| **`Assets/_Project/Features/`** | World Engine spine (GameState, WorldState, Directors, Validation, PPT adapter) | Snapshot builders + bootstrap chain; defers LLM Communications |
| **`Assets/_Project/Resources/`**, **Prefabs/**, **Scenes/** | Data & authoring | Loaded by registries (`ItemRegistry`, creature definitions, map calibration profiles) |
| **`Assets/_Project/Editor/`** | Setup tooling | Not runtime; large prefab/scene builders |

### `Scripts/` folder scale (file count indicative)

Largest clusters: **UI (~159)**, **AI (~53)**, **Companions (~41)**, **Player (~38)**, **Interaction (~34)**, **Combat (~34)**, **Pioneers (~28)**, **Survival/Exposure (~27)**, **Core (~23)**, **Creatures (~22)**.

### Runtime bootstrap & ownership

```
SimpleGameManager (Managers/)
  └─ CompanionSystemsBootstrap.EnsureGameplaySystems
       ├─ GameStateBootstrap → WorldStateBootstrap → DirectorsBootstrap  (Features/)
       ├─ CompanionRosterBridge, FacilityTaskRunner, PetManager, …
       └─ PioneerRosterManager.EnsureExists (also UIManager / player fallbacks)

Player prefab stack
  ├─ PlayerController OR Invector stack (PioneerInvectorBootstrap + many *Bridge behaviours)
  ├─ InventorySystem, WorldUseController, OpticsController, ExposureReceiver
  └─ Registers PlayerReference (intended cache for Transform/Camera)

Parallel character stacks
  ├─ Player / Enemy / Companion each: gameplay scripts + Invector/ subfolder bridges
  └─ Creatures: DMICreatureBridge + Malbers brain ScriptableObjects + optional Invector overlap on enemies
```

### Domain quick reference

| Domain | Key types | Integration |
|--------|-----------|-------------|
| **Economy / roster** | `PioneerRosterManager`, building ops registries | UI panels, save via `GameSaveSystem` |
| **Combat** | `CombatHitResolver`, projectiles, Invector weapon bridges | AI `EnemyCombat`, companion combat coordinator |
| **Interaction** | `WorldUseController`, `ResourceGatherer`, mining/optics | UI prompts (`UIManager`), `SceneComponentCache` scans |
| **Survival** | `SurvivalStats`, `Exposure/*` | `ExposureStatusService` snapshot → HUD gauges |
| **World sim** | `GameplayWorldSimulation.IsFrozen`, Directors | Freezes AI/creatures/exposure updates when paused |
| **Map** | `WorldMapProvider`, `MapFogOfWar`, markers | Scanner reveals tie into fog mask |
| **Echoes / quests** | `EchoSignalRegistry`, `QuestManager` | Companion sense scans registry list (efficient) vs creature resolver (not) |
| **Pet (legacy parallel)** | `PetManager`, `PetController` | Overlaps GDD direction to fold into Echo/trio; still uses scene queries |

### World Engine (Features) vs legacy scripts

- **Features/** implements **snapshot-oriented** state (GameState/WorldState/Directors) with tests under `Features/*/Tests`.  
- **Gameplay feel** still lives primarily in **Scripts/**; Features adapters (e.g. `PptGameStateProvider`) read legacy systems.  
- **Communications** module is **not on disk** yet (`World_Engine_Disk_Status.md`).

### Architectural friction points (maintainability ↔ perf)

1. **Three parallel humanoid stacks** (player Invector, enemy Invector, companion Invector) → duplicated bridge patterns and large prefab component counts.  
2. **Singleton discovery** (`EnsureExists` + `FindAnyObjectByType`) instead of a narrow service registry or scene-scoped `GameplayContext`.  
3. **Hub monoliths**: `WorldUseController` (~1.4k lines) and `UIManager` partials centralize cross-cutting concerns (input, prompts, scans, UI).  
4. **Pet + Pioneer companion** duplication increases scan surfaces (`PetController`, `CompanionRosterBridge`, creature resolver).  
5. **Positive counter-pattern**: `SceneComponentCache`, event-driven exposure refresh, phased AI/humanoid updates — use as standard for new work.

---

## 3. Top five next steps (concrete, perf + structure)

1. **Centralize combatant discovery (fixes H1, H2, H5 partially)**  
   Add a **`GameplayActorRegistry`** (or extend `PlayerReference`) maintaining lists of `PetController`, `CompanionHealth`, `DMICreatureBridge`, `ScannableTarget`, `ItemPickup` via **register/unregister in OnEnable/OnDisable**. Refactor `DMICreatureTargetResolver`, `OpticsController.ScanScannableComponents`, and pet fetch to read lists — keep `SceneComponentCache` as fallback only after registry miss.

2. **Kill `FindAnyObjectByType<CompanionRosterBridge>` in AI combat (H3)**  
   Cache bridge reference on `EnemyAiController` at spawn (or static weak singleton set in `CompanionRosterBridge.OnEnable`). Cache `CompanionHealth` on `PioneerCompanionAgent` to avoid per-retarget `GetComponent`.

3. **Ground sampling without `RaycastAll` (H4, M12)**  
   Replace `PlayerPathTrail.SampleGroundHeight` and inventory drop grounding with **`RaycastNonAlloc` + sorted buffer** (reuse static hit buffer like companions) or **`Terrain.SampleHeight` + single ray** confirmation. Companions already use a static buffer pattern in `CompanionFollowController.Movement.cs` — unify into `DmGroundProbe` utility.

4. **Exposure snapshot: eliminate per-slot `ToArray` (H6)**  
   Mirror player buff/debuff handling: copy companion scratch lists into **reused `ExposureModifierTick[]` buffers** per slot or expose `IReadOnlyList` views for UI without allocating each 0.1s refresh.

5. **Profiler-driven budget pass on “expedition slice” (validates M5–M7)**  
   In HDRP Play Mode with **player + 3 companions + 8–12 enemies + 2 creatures + scanner active**, capture Unity Profiler (Scripts + GC Alloc):  
   - `CompanionFollowController` / `CompanionFollowController.Movement`  
   - `DMICreatureTargetResolver` / `DMICreatureBridge.Update`  
   - `OpticsController.RefreshScannerTargets`  
   - `MapFogOfWar.UploadTexture`  
   Set **console-style budgets** (e.g. Script CPU ≤ X ms, GC ≤ Y KB/frame) and gate map fog resolution / scan refresh intervals via `PlatformGraphicsProfile` (already used in `HumanoidPerformanceController`).

---

## Appendix A — Suggested profiling checklist (HDRP)

- [ ] Baseline: Genesis scene, 1080p/4K PC, empty combat  
- [ ] + Expedition trio following through terrain  
- [ ] + Surface encounter (multiple `EnemyAiController` + `HumanoidPerformanceController`)  
- [ ] + Sulfur hound pack (`DMICreatureBridge` target refresh)  
- [ ] + Scanner optics active 30s (`OpticsController` + sweep coroutine)  
- [ ] + Exposure storm zone entered (particles + `ExposureZoneVolume.Update`)  
- [ ] Deep profile GC: drop 20 items, pet fetch, exposure HUD open  

## Appendix B — Related project docs

- `Assets/_Project/Documentation/Architecture/World_Engine_Disk_Status.md` — shipped vs deferred Features  
- `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt` — canon (AC-only economy, trio companions, thermal/exposure)  
- `.cursor/rules/unity-console-check-after-edit.mdc` — compile/console gate for future fixes  

---

*End of audit.*
