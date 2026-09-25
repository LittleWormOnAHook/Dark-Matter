# Building System — Implementation Tickets

**Parent plan:** [`DMG_Building_System_Scope_Plan.md`](DMG_Building_System_Scope_Plan.md)  
**Last updated:** 2026-09-24  
**Convention:** `BUILD-###` · status: `backlog` | `ready` | `in_progress` | `done` | `blocked`  

**Global constraints (every ticket):** No NavMesh · palette hologram (not gold) · border fences stay in v1.6 · wrecks in `Terrain_X_Y_Content` · drain mats on hold · reuse multitool/BCP/scanner/reverse dissolve. Component track: UITK build hotbar, ten visible slots, arrow scroll, no scrollbar, Stone before other tiers.

---

## Epic map

| Epic | Slice | Goal | Prologue gate |
|------|-------|------|---------------|
| **E0** Foundation | 1 | Hologram + instant place works | — |
| **E1** Materialize | 2 | Hold-construct + ghost + drain | Act I-C/D |
| **E2** Wrecks | 3 | Scan → unlock → repair in place | Act I-B |
| **E3** Authoring | 4 | Editor pipeline for new defs | Content scale |
| **E4** Persistence | 5 | Save/load placed buildings | Act II camp check |
| **E5** BCP depth | 6 | Companions, queues, storm pause | Act I-D |
| **E6** Prologue content | — | Quest items, pads, defs, scenes | Act I ship |
| **E7** Campaign facilities | 7 | Act III buildings + Resonance hooks | Post-prologue |
| **E8** Components | C1–C6 | Stone snap library, hotbar, Studio, map blueprints, tiers | After Slice 1 |

---

## E0 — Foundation (Slice 1)

### BUILD-001 — `BuildingDefinition` ScriptableObject + registry

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | — |
| **Story** | — |

**Scope**
- Create `BuildingDefinition` SO under `Assets/_Project/Features/Building/Data/` (or `Scripts/Building/Data/` if no Feature folder yet).
- Fields per scope plan §7: `id`, `displayName`, `finishedPrefab`, `recipe`, `footprint`, `maxSlope`, `unlock`, `constructTime`, `craftStation`, etc.
- `BuildingDefinitionRegistry` (Resources asset or static loader) — lookup by stable `id`.
- Editor: create asset menu + validate unique ids.

**Acceptance**
- [ ] Can create SO in Project window
- [ ] Registry resolves def by id at runtime
- [ ] Duplicate id logs error in editor validate

**Touch:** new SO + registry + `.meta`

---

### BUILD-002 — Shared hologram material (palette)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | — |

**Scope**
- One hologram shader/material instance using `DarkMatterGenesisUiPalette` (valid = Rich Fuchsia / off-white glyph feel; invalid = Deep Magenta reject — **not gold**).
- Material supports tint property for green/red without second shader.
- Store under `Assets/_Project/Art/Materials/Building/` or similar.

**Acceptance**
- [ ] Single mat works on any mesh
- [ ] Valid/invalid tint driven by script property
- [ ] Matches Shift UI palette in scene view

---

### BUILD-003 — Placement validity service

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | BUILD-001 |

**Scope**
- `BuildingPlacementValidator` static or service:
  - Terrain ray hit (Unity Terrain / Gaia tile collider)
  - Slope vs `maxSlope`
  - Overlap box/capsule vs buildings, wrecks, blockers
  - Inside v1.6 border fence colliders/layers (tag or layer query — document which)
- Returns enum + reason for UI toast later.

**Acceptance**
- [ ] Flat valid ground → pass
- [ ] Steep slope → fail
- [ ] Overlap existing collider → fail
- [ ] Outside fence → fail
- [ ] No NavMesh API calls

**Touch:** `Scripts/Building/BuildingPlacementValidator.cs`

---

### BUILD-004 — Multitool placement controller (preview hologram)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | BUILD-001, BUILD-002, BUILD-003 |

**Scope**
- While build mode is active (Hold B, BUILD-006) and a blueprint is selected: spawn preview mesh at aim ray hit.
- Apply hologram mat + valid/invalid tint from BUILD-003 each frame.
- Reuse deploy-style input from hovercraft/walker drill patterns (read first, match).
- Do not commit on click yet (BUILD-005).

**Acceptance**
- [ ] Hold B → preview follows aim
- [ ] Green on valid flat ground inside fence
- [ ] Red on invalid; no commit when red

**Touch:** `DMBuildingPlacementController.cs` (or under `Features/Building/Runtime/`)

---

### BUILD-005 — Instant complete placement (Slice 1 exit)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | BUILD-004 |

**Scope**
- Click (green only) → spawn `finishedPrefab` at pose.
- Ensure `BuildingControlPanel` on prefab is enabled.
- Wire first **`BuildingDefinition`** for existing workbench OR `PowerGenerator` prefab (no new art).
- Register instance in lightweight runtime list (prep for BUILD-020 save).

**Acceptance**
- [ ] Click green → finished building appears
- [ ] E opens BCP on placed building
- [ ] Cannot place when red

**Touch:** first def asset, existing prefab, placement controller

---

### BUILD-006 — Multitool item + blueprint select (minimal)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-004 |

**Scope**
- Build mode is **Hold B** on keyboard (Input System Hold interaction). Tap B stays `Binoculars` in `InputSystem_Actions` and `GameplayKeyboardShortcuts`.
- Gamepad: Hold on Left Shoulder enters build mode; tap stays binoculars.
- Hold B again exits. While build mode is active, binoculars are suppressed.
- Journal, pause, and other modals block the hold.
- Multitool equip is not the mode switch. The multitool remains the construct tool used inside the mode.
- Minimal blueprint select: one unlocked def for Slice 1 test (debug menu or single bound def).
- Defer full deploy wheel to BUILD-015 if needed for prologue.

**Acceptance**
- [ ] Hold B enters placement mode and shows the build hotbar
- [ ] Tap B still toggles binoculars when build mode is off
- [ ] Hold B again exits and clears the preview
- [ ] A modal open ignores the hold

---

## E1 — Materialize (Slice 2)

### BUILD-010 — `BuildingGhost` runtime component

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | BUILD-005 |

**Scope**
- Component on committed ghost: holds `definitionId`, construct progress 0–1, pose.
- Ghost uses finished mesh + hologram mat (not full gameplay colliders/usables until complete).

**Acceptance**
- [ ] Ghost persists in world after commit click
- [ ] Stores def reference + progress

---

### BUILD-011 — Click commits ghost (replace instant complete)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | BUILD-010 |

**Scope**
- Change BUILD-005 path: click spawns ghost, not finished prefab.
- Freeze last valid pose on commit.

**Acceptance**
- [ ] Green click → ghost only
- [ ] Red click → no ghost

---

### BUILD-012 — Hold-construct input + recipe drain

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | BUILD-011 |

**Scope**
- Hold use/fire while aiming at ghost: advance progress over `constructTime`.
- Drain recipe ingredients from inventory **per tick** (not on ghost commit).
- Release early → cancel ghost + **refund drained** amounts.
- Block construct if inventory insufficient (red feedback on ghost).

**Acceptance**
- [ ] Hold with mats → progress increases, mats decrease
- [ ] Release mid-hold → ghost removed, mats refunded
- [ ] Out of mats → construct stops

---

### BUILD-013 — Reverse dissolve VFX on construct

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | BUILD-012 |

**Scope**
- On ghost mesh: drive `EnemyDisintegrate` / `EnemyDisintegrationEffect` `_DissolveAmount` from 1 → 0 as progress 0 → 1.
- On complete: swap ghost for `finishedPrefab`, enable BCP, destroy ghost.

**Acceptance**
- [ ] Visual undissolve matches hold duration
- [ ] Complete → finished prefab + BCP live

---

### BUILD-014 — `BuildingSnapPad` volume

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-003 |

**Scope**
- Thin component: allowed `definitionId`(s), snap position/rotation, optional “nest must be clear” flag hook for quest.
- Validator: if def `requiresSnapPad`, must be over matching pad.

**Acceptance**
- [ ] Off-pad placement fails for snap-only defs
- [ ] On-pad snaps position/rotation

---

### BUILD-015 — Blueprint select UI (deploy menu extension)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-006, BUILD-001 |

**Scope**
- Player-facing picker is the **building hotbar** (BUILD-093), not a new fullscreen builder.
- Lists **unlocked** `BuildingDefinition`s for the selected material, Kits, or Vital Machines.
- Filter by unlock state (scan / map blueprint / quest / schematic flags).
- `starterUnlocked` Stone first basic kit (seven pieces) is visible before any find.

**Acceptance**
- [ ] Player can pick among unlocked defs from the ten-slot bar
- [ ] Locked defs stay off the bar
- [ ] Side arrows scroll; there is no scrollbar

---

## E2 — Wrecks (Slice 3)

### BUILD-020 — `BuildingWreck` component + scan unlock

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-001, existing `ScannableTarget` |

**Scope**
- `BuildingWreck`: references `BuildingDefinition`, wreck visual prefab/state.
- On scan (optics/scanner): unlock def in player unlock registry (new small service or extend recipe unlock pattern).
- E prompt: “Scan wreck” / “Repair” — not BCP until repaired.

**Acceptance**
- [ ] Scan wreck → def unlocked for placement/repair
- [ ] Scan state persists in session (BUILD-030 for save)

---

### BUILD-021 — In-place wreck repair (reuse construct path)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-012, BUILD-020 |

**Scope**
- Hold multitool on wreck: same drain + dissolve as ghost construct.
- Complete → swap wreck visual for finished prefab in place; enable BCP.

**Acceptance**
- [ ] Repair in place without separate placement step
- [ ] Same cancel/refund rules as ghost

---

### BUILD-022 — Prologue fabricator ruin (content)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P2 |
| **Depends on** | BUILD-020, BUILD-021 |

**Scope**
- Place fabricator ruin in prologue Resource Ring content scene (`Terrain_X_Y_Content` — pick tile).
- Wreck unlocks `craft_station_settlement` (or agreed def from scope §15).
- Helix rumor dressing only — no lore dump.

**Acceptance**
- [ ] Ruin in content scene, loads with tile
- [ ] Scan → unlock → repair → station BCP works

---

## E3 — Authoring (Slice 4)

### BUILD-030 — Editor: Author Building window

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-001, BUILD-002 |

**Scope**
- Menu: `Tools/Dark Matter Genesis/Buildings/Building Studio` opens Genesis Studio on the **Building** category (BUILD-095).
- One-pass action lives **inside** that studio: finished prefab → create/update `BuildingDefinition`, stamp hologram mat, wire BCP id, optional wreck prefab, recipe slot link.
- Validation checklist log (mirror creature prefab builder pattern).

**Acceptance**
- [ ] One-pass author from prefab inside Building Studio
- [ ] Missing BCP on a kit or vital-machine prefab warns
- [ ] Component drops do not require a BCP

---

### BUILD-031 — Migrate variant prefabs to definitions

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P2 |
| **Depends on** | BUILD-030 |

**Scope**
- Author defs for `Command Center Variant`, `Science Lab Variant`, first workbench/power test prefab.
- Document ids in scope plan unlock matrix.

**Acceptance**
- [ ] Variants have SO defs + recipes stub
- [ ] No hand-wired one-offs outside defs

---

## E4 — Persistence (Slice 5)

### BUILD-040 — Extend `BuildingSnapshot` schema

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-010, BUILD-011 |

**Scope**
- Save: list of `{ definitionId, pos, rot, state: Ghost|Complete, progress, drainedSnapshot? }`
- Extend `BuildingGameStateProvider` + `GameSaveSystem` round-trip.

**Acceptance**
- [ ] Save writes placed buildings
- [ ] Load restores list in memory

---

### BUILD-041 — Rehydrate buildings on tile/session load

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-040, BUILD-013 |

**Scope**
- `BuildingInstanceRunner` or hook on scene/tile load: spawn ghosts or finished prefabs from save.
- Never write to Gaia terrain assets.

**Acceptance**
- [ ] Save mid-construct → load → ghost at same progress
- [ ] Finished buildings return with BCP enabled

---

### BUILD-042 — Unlock registry persistence

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P2 |
| **Depends on** | BUILD-020, BUILD-040 |

**Scope**
- Persist scanned/unlocked definition ids in save.
- Load restores blueprint list for BUILD-015.

**Acceptance**
- [ ] Scan wreck → save/load → still unlocked

---

## E5 — BCP depth (Slice 6)

### BUILD-050 — BCP tab gating (Overview-first)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | — (existing BCP UI) |

**Scope**
- Flag on panel or global prologue state: lock Companions/Production/Craft/Changes until Act I-D step.
- Overview shows power + storm stub.

**Acceptance**
- [ ] Early prologue: Overview only
- [ ] Unlock after bootstrap quest step

---

### BUILD-051 — Companions tab ↔ `PioneerRosterManager`

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-050 |

**Scope**
- Replace demo assign with real roster names from `PioneerRosterManager`.
- Persist assign via existing `BuildingOperationRegistry` save path.

**Acceptance**
- [ ] Assign starter companion at shelter/CC
- [ ] Save/load keeps assignment

---

### BUILD-052 — Production queue live tick

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P2 |
| **Depends on** | BUILD-051 |

**Scope**
- Wire Production tab to real queue entries (not demo-only).
- `FacilityTaskRunner` advances while player away (expedition rules per GDD).

**Acceptance**
- [ ] Queue shows progress
- [ ] Completes and grants output (stub item ok)

---

### BUILD-053 — Storm / gust queue pause

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-052 or registry only |

**Scope**
- `EnvironmentalCrisisHudMode` or scripted mini gust → `BuildingOperationRegistry.PauseAll()` / resume.
- Overview tab shows PAUSED state (existing stub).

**Acceptance**
- [ ] Mini gust pauses queues
- [ ] Resume after gust ends
- [ ] F11 / debug gust triggers pause

---

### BUILD-054 — Quest objective hooks (companion assign, camp complete)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P2 |
| **Depends on** | BUILD-051, BUILD-041, prologue quests |

**Scope**
- Custom quest objectives or events: `BuildingCompanionAssigned`, `CampBootstrapComplete` (seed+shelter+station).
- Pulse BCP Companions tab when assign pending.

**Acceptance**
- [ ] Quest can complete on assign
- [ ] Camp flags queryable for Act II

---

## E6 — Prologue content pack

### BUILD-060 — `BuildingDefinition` assets: prologue quartet

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-030 |

**Scope**
- Author SO defs: `cc_seed`, `survival_shelter`, `craft_station_settlement`, `module_o2_scrubber`.
- Recipes, footprints, construct times, BCP/station bindings.

**Acceptance**
- [ ] Four defs in `Resources/Building/` or Data folder
- [ ] Match scope plan §9 matrix

---

### BUILD-061 — Camp Plateau snap pad + blockout

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-014 |

**Scope**
- Snap pad volume on Camp Plateau (v1.6 or content scene).
- Survey paint decals (rumor dressing).
- Nest clear zone hook for quest (collider or script flag).

**Acceptance**
- [ ] CC Seed only places on pad
- [ ] Nest clear quest can gate placement

---

### BUILD-062 — `Item_CampBeaconKit` + placement flow

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-015, BUILD-014, BUILD-060 |

**Scope**
- ItemData for Camp Beacon Kit — use starts placement mode for `cc_seed` (like deploy item).
- Granted by `prologue_02` / Scene B exit (quest wiring separate BUILD-070).

**Acceptance**
- [ ] Use kit → multitool placement for cc_seed
- [ ] Consumes kit on successful ghost commit or on complete (pick one — document)

---

### BUILD-063 — Emergency Cell carry + power hook

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P2 |
| **Depends on** | BUILD-060, existing `PowerConsumer` |

**Scope**
- World item: slows sprint, drop on hit (quest layer).
- Insert at CC Seed → powers `PowerConsumer` → BCP Overview shows online.

**Acceptance**
- [ ] Unpowered seed until cell inserted
- [ ] Powered state persists in save

---

### BUILD-070 — Prologue quest chain (building objectives)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P2 |
| **Depends on** | BUILD-054, BUILD-060–063 |

**Scope**
- Author `QuestDefinition` assets: `prologue_03_claim_site`, `prologue_04_bootstrap` (minimum for building ship).
- Objectives: ReachLocation, Custom (place seed, build shelter/station, assign companion, survive gust).

**Acceptance**
- [ ] P3–P4 mainline completable with building tickets done
- [ ] Journal tracks objectives

---

## E7 — Campaign facilities (Slice 7 / post-prologue)

### BUILD-080 — Resonance → BCP pause + building damage flags

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P3 |
| **Depends on** | BUILD-053, `ResonanceEventDirector` (FUTURE) |

**Scope**
- Interface for Resonance Event to pause queues + mark building injured state.
- Hook for base-22 injury (no death) — data only if sim not ready.

**Acceptance**
- [ ] Test Resonance stub pauses camp
- [ ] Overview shows REDUCED/PAUSED per GDD weather copy

---

### BUILD-081 — Act III facility defs batch 1 (cores 1–3)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P3 |
| **Depends on** | BUILD-030, BUILD-041 |

**Scope**
- Defs + wrecks: Echo Reclamation, Purification Hub (B1 content scenes).
- Unlock tied to Resonance rewards / scan.

**Acceptance**
- [ ] Place after unlock in B1
- [ ] Wrecks in content scenes

---

### BUILD-082 — CC Seed → full Command Center upgrade path

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P3 |
| **Depends on** | BUILD-060, BUILD-081 |

**Scope**
- Upgrade interact or auto-swap after Core 1–2: seed prefab → full CC variant.
- Preserve BCP assignments + save id mapping.

**Acceptance**
- [ ] Upgrade does not wipe assign/queue state
- [ ] Visual swap + expanded BCP tabs if designed

---

### BUILD-083 — Attachment module pilot (generator)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P3 |
| **Depends on** | BUILD-052, BUILD-060 |

**Scope**
- One module def (generator) attaching to CC or power graph.
- Surfaces on BCP Changes tab stub.

**Acceptance**
- [ ] Place module near CC
- [ ] Power graph reflects new source

---

## E8 — Components (C1–C6)

Parent: scope plan **section 18**. Starts after BUILD-001–004. Stone only until a later tier is authored.

### BUILD-090 — Definition fields for pieces, kits, and machines

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | BUILD-001 |

**Scope**
- Extend `BuildingDefinition` with `kind`, `materialTier`, `section`, `footprintMeters`, `sockets`, `upgradeToId`, `starterUnlocked` (scope §7 and §18).
- Kinds: `Component`, `Kit`, `VitalMachine`.
- Tiers: `Stone`, `Iron`, `Steel`, `Silicate`, `Amalgam`.

**Acceptance**
- [ ] Existing kit-style def still validates
- [ ] A Stone wall def stores 4×4 m and a socket list
- [ ] Registry lookup by id unchanged

---

### BUILD-091 — Stone library folders + ProBuilder first basic kit

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | BUILD-090 |

**Scope**
- Create `Assets/_Project/Prefabs/Buildings/Library/` as in scope §18.5. Stone section folders only need meshes; other tier folders may be empty.
- ProBuilder primitives, all `starterUnlocked`: `stone_foundation_4x4`, `stone_wall_4x4`, `stone_floor_4x4`, `stone_slope_4x4`, `stone_wall_window_4x4`, `stone_door_frame_4x4`, `stone_door_basic`.
- Window wall matches the solid wall footprint and includes a glass pane. Door frame matches the solid wall footprint and exposes one door socket. Basic door fills that socket only.

**Acceptance**
- [ ] Seven prefabs and seven definitions exist under Library/Stone
- [ ] Wall, window wall, and door frame are 4 m wide and 4 m tall
- [ ] Basic door cannot commit unless a door frame socket is in range
- [ ] Iron/Steel/Silicate/Amalgam folders exist for later drops

---

### BUILD-092 — Snap to grid and snap to components

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | BUILD-003, BUILD-004, BUILD-090 |

**Scope**
- 1 m grid, 90° yaw.
- Prefer a neighbor socket in range; otherwise the grid point under the aim ray.
- Support rule from scope §18.3 (foundation on ground, wall on foundation or floor edge, and the rest of that table).
- Sizes the validator must accept: 4×4, 1×4, 2×4, 4×8, 8×8, triangle, slope.
- No NavMesh. No chain-collapse.

**Acceptance**
- [ ] Foundation commits on flat ground inside the fence
- [ ] Wall, window wall, and door frame snap to that foundation's edge socket
- [ ] Basic door snaps only into the door frame
- [ ] Wall in open air stays red
- [ ] Red cannot commit

---

### BUILD-093 — Building hotbar (UITK)

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P0 |
| **Depends on** | BUILD-006, BUILD-090, BUILD-091 |

**Scope**
- `BuildingHotbar.uxml` on `UITK_Hud`. Runtime `DMUiToolkitBuildingHotbar`.
- Visible only in build mode (**Hold B** to enter, hold again to exit). Tilde hides it with gameplay HUD. Hide in C# only.
- Layout: up-arrow button, then a holder with a left arrow, **10** slots, and a right arrow.
- Arrows shift one slot. No scrollbar.
- Up panel lists Stone, Iron, Steel, Silicate, Amalgam, then Kits, then Vital Machines. Locked tiers disabled. Selection resets the window.
- Palette: Dark Navy, Slate, Deep Magenta, Warm Off-White, Rich Fuchsia selection. Not gold.
- Selected slot drives the placement hologram.

**Acceptance**
- [ ] Hold B shows the bar; hold B again hides it
- [ ] Tap B does not show the bar
- [ ] More than ten unlocked Stone pieces scroll with the arrows only
- [ ] Up panel changes the list
- [ ] Unaffordable slots stay visible and cannot commit
- [ ] No second HUD document

---

### BUILD-094 — Map blueprint pickups

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-090, BUILD-042 |

**Scope**
- `BuildingBlueprintPickup`: grants one or more definition ids (component, kit, building, or vital machine).
- Same unlock flag as a wreck scan.
- Gathering stone does not unlock anything.

**Acceptance**
- [ ] Pickup adds the entry to the matching hotbar list
- [ ] Reload keeps the unlock
- [ ] Duplicate pickup does not double-grant

---

### BUILD-095 — Building Studio in Genesis Studio

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P1 |
| **Depends on** | BUILD-090, BUILD-002 |

**Scope**
- Genesis Studio category **Building**, menu `Tools/Dark Matter Genesis/Buildings/Building Studio`.
- Material workflow: create tier + folder. Stone seeded.
- Drop wells: foundations, walls, floors, roofs, blocks, mini blocks, door frames, doors, windows, stairs, ramps, triangles, slopes, plus Kits and Vital Machines.
- Drag-drop prefab writes the definition and files it into the library folder.
- Create-primitive action emits the ProBuilder mesh for that section's default size.
- Not a Play-mode save profile.

**Acceptance**
- [ ] Dropping a prefab on Walls under Stone creates a definition and a prefab in `Library/Stone/Walls`
- [ ] New material creates `Library/<Tier>/` and a disabled hotbar row
- [ ] Kit drop warns if the prefab has no BCP

---

### BUILD-096 — Tier upgrade and skill gates

| Field | Value |
|-------|-------|
| **Status** | backlog |
| **Priority** | P2 |
| **Depends on** | BUILD-012, BUILD-090 |

**Scope**
- Hold on a finished piece pays the recipe delta and swaps to `upgradeToId` when that tier is unlocked and the skill gate in scope §18.4 passes.
- Stone is the only tier with meshes. Iron and above can be data-only until art exists.
- Amalgam recipes are mixes (steel + silicate, optional AC), not a single resource.
- Save the resulting definition id (BUILD-040).

**Acceptance**
- [ ] Stone piece with an Iron `upgradeToId` upgrades when the gate passes
- [ ] Missing skill or blueprint leaves the piece unchanged
- [ ] Reload shows the upgraded id

---

## Suggested sprint order

```
Sprint A (vertical slice):  BUILD-001 → 002 → 003 → 004 → 005 → 006
Sprint B (materialize):     BUILD-010 → 011 → 012 → 013 → 014
Sprint C (prologue playable): BUILD-030 → 060 → 061 → 062 → 015 → 050 → 051 → 053
Sprint D (wrecks + save):   BUILD-020 → 021 → 040 → 041 → 022
Sprint E (quests):          BUILD-063 → 054 → 070
Sprint F (production):      BUILD-052 → 042 → 031
Sprint G (campaign):        BUILD-080 → 081 → 082 → 083
Sprint H (stone components): BUILD-090 → 091 → 092 → 093 → 095
Sprint I (blueprints + tiers): BUILD-094 → 096
```

Sprint H starts once Sprint A (BUILD-001–004) is in. It does not wait on campaign tickets.

---

## Ticket count summary

| Epic | Tickets | P0 |
|------|---------|-----|
| E0 Foundation | 6 | 5 |
| E1 Materialize | 6 | 4 |
| E2 Wrecks | 3 | 0 |
| E3 Authoring | 2 | 0 |
| E4 Persistence | 3 | 0 |
| E5 BCP depth | 5 | 0 |
| E6 Prologue content | 5 | 0 |
| E7 Campaign | 4 | 0 |
| E8 Components | 7 | 4 |
| **Total** | **41** | **13** |

---

## Iteration notes (for you)

- Split **BUILD-012** if recipe drain and cancel/refund are too large for one PR.
- **BUILD-070** can ship stub quests before full VO — use Custom objectives + debug complete.
- Defer **BUILD-015** if single-def debug binding is enough for first playtest.
- **Relay Pylon** and **Aether-9 shell** intentionally have **no tickets** — story POIs, not Lite Building v1.

When you iterate, edit status/priority here or say which tickets to merge/split.
