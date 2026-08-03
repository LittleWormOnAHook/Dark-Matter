# Subsurface Drill Transit Hub

**Status:** Game design idea — **not locked** (August 2026)  
**Origin:** Design conversation — drill capsule for underground access with survey-based travel  
**Related canon (locked July 2026):** `Io_Underground_Architecture_Plan.md`, `Io_Biome_Exploration_Gameplay_Plan.md` §2.5–2.7  
**Companion runtime:** `IoWorldTransitionRules.cs`, scanner/map stack, hovercraft deploy pattern

---

## 1. Elevator pitch

Players deploy a **human-operated drill capsule** at surface breach sites found by **scanning**. The capsule drills into **instanced underground** scenes (passages, caverns, strata biomes). Once underground, entering the drill opens a **survey travel map** to any **previously unlocked** destination — other underground POIs **or** surface breach anchors the player has visited before.

The drill is the fantasy wrapper for breach entry and subsurface routing — **not** real-time terrain carving.

---

## 2. Core player fantasy

- **Surface:** Scan → find weak rock / tube signal → deploy drill → enter → short drill vignette → underground instance.
- **Underground:** Explore on foot first; discover camps, pools, nests, vault antechambers.
- **Transit (underground only):** Re-enter drill → survey map → travel to any unlocked node (surface or subsurface) with a short drill transit sequence.
- **Return:** Drill up to a surface breach anchor (not necessarily the breach you entered from).

Subsurface is the mystery; the drill is the **survey capsule** that rides known routes between surveyed nodes.

---

## 3. Travel rules (proposed lock)

### 3.1 Surface ↔ underground

| Action | Rule |
|--------|------|
| **First entry** | Surface only — deploy drill at a **scanned breach POI**, enter, drill down into that network’s instance |
| **Exit to surface** | Via drill transit map (underground-initiated) **or** legacy walk-to exit breach (see §6 open question) |
| **Anchor** | Each surface breach stores a **return spawn**; arriving at a surface node uses **that** breach’s anchor |
| **Vehicles** | **Auto-pack** within 10–20 m of surface breach entry (per locked `IoWorldTransitionRules`); **manual unpack** after surface arrival |
| **Underground locomotion** | **Foot only** — no hovercraft, no skiff inside instances |

### 3.2 Underground transit hub

| Action | Rule |
|--------|------|
| **Who can open travel map** | Only when **entering the drill from underground** (entry node, instance camp, designated drill bay) |
| **Destinations** | All **previously unlocked** nodes in the **survey network** — underground POIs **and** surface breach mouths |
| **First visit** | Always **on foot** (or scripted arrival) — map travel does not skip unseen content |
| **Network scope** | Travel within one **survey graph** keyed to region/stratum; no cross-network jump without unlocking (e.g. B6 S1 network ≠ B4 caldera network until both opened) |
| **Stratum gates** | Deeper strata (S3+, vaults) appear on map only after first visit + any story/key gate |
| **Cost / friction** | Short drill vignette (15–45 s) plus one of: fuel, O₂ tick, survey charge consumable, or tremor risk during transit |
| **Surface restrictions** | Optional: block surface destinations during sulfur storm or without region-appropriate gear (B4 heat, B5 cold/rad) |

### 3.3 What this is not

- Not Minecraft-style digging or voxel destruction
- Not ambient vehicles underground
- Not free instant teleport with no cost
- Not surface-to-surface breach hopping **without** going underground first (optional rule — see §6)

---

## 4. Survey network (data model sketch)

Unified registry of **survey nodes**:

```
SurveyNode
  id: stable save id
  displayName: "B6 Highland Breach" | "Tube Lace Camp" | ...
  layer: Surface | Underground
  region: IoSurfaceRegionId
  stratum: 0 (surface) | 1–5
  sceneId: Unity scene or additive sub-scene
  spawnTransform: arrival point
  unlockKind: FirstVisit | Scan | StoryKey
  icon: map / journal sprite
```

**Links:** nodes in the same `SurveyNetwork` (e.g. `B6_S1_TubeLace`) can be travel targets once unlocked.

**Persistence:** `SurveyNetworkRegistry` (save/load) — parallel to `ScannerDiscoveryRegistry` / `MapMarker` discovery IDs.

---

## 5. Player flows

### 5.1 First expedition

```
Surface: scan breach → deploy drill → enter → drill vignette → load underground entry
  → explore on foot → unlock Camp, Pool (added to survey map)
  → enter drill → map shows: B6 Breach (surface), Camp, Pool
  → travel to Pool OR drill up to B6 Breach
```

### 5.2 Return run

```
Surface: deploy at B6 breach → underground entry
  → enter drill immediately → travel to Pool (skip backtracking)
  → harvest / clear → drill up to B6 or travel to another unlocked surface breach
```

### 5.3 Multi-breach surface graph (late game)

```
Player has unlocked: B6 breach, B1 seep breach, Camp, Pool, Nest
Underground drill map lists all five; can surface at B1 without walking from B6
```

---

## 6. Alignment with existing locked design

| Locked (July 2026) | This idea |
|--------------------|-----------|
| Full-scale surface main map | Unchanged |
| Most underground **instanced** | Unchanged — drill loads instances |
| 10–20 m vehicle auto-pack at breach | Unchanged on surface entry |
| Foot only underground | Unchanged |
| Five strata S1–S5 | Unchanged — nodes tagged by stratum |
| Instance camps (stash, O₂, scrapper) | Natural **survey nodes** / travel hubs |
| Expedition drill shafts (surface layer mention) | **This idea names the vehicle** |
| Exit breach → return anchor | **Refined:** drill hub may **replace** walk-to exit as primary return; see open question |

### Open questions

1. **Walk-to exit breach** — keep as backup when drill is destroyed / out of fuel, or remove in favor of drill-only exit?
2. **Surface drill travel map** — ever allow opening map from surface-deployed drill, or strictly underground-initiated?
3. **Trio** — all three expedition members transit with player, or hold at last camp?
4. **Nested instances** — brood mother / vault core as map nodes or separate loads only on foot?
5. **Geothermal Harvester `DeepDrillMode`** — narrative tie to expedition drill tech, or separate building loop?

---

## 7. Technical feasibility (disk truth, August 2026)

**Exists today**

| System | Reuse |
|--------|-------|
| Scanner / optics | Find surface breach POIs |
| `ScannerDiscoveryRegistry` + `MapMarker` | Unlock + map icon pattern |
| `MapUI` / Journal | Host survey travel map UI |
| `HovercraftDeploymentUtility` + occupancy | Deploy / enter / store drill capsule |
| `IoWorldTransitionRules` | Pack radius, `IoUndergroundAccessKind` |
| `LoadingOverlayController` | Mask scene loads during drill vignette |

**Not built**

| System | Notes |
|--------|-------|
| Underground instance pipeline (IO-W1-02) | Breach load/unload + anchor save |
| Underground scenes | Greybox S1+ content |
| Drill capsule prefab | Art + interact + fuel (optional) |
| `SurveyNetworkRegistry` + `DrillTransitController` | New — moderate scope |
| `DrillTransitMapUI` | Journal tab or map layer |

**Verdict:** **Highly feasible** as simulated transition + unlock graph. Content cost (strata kits, POI scenes) exceeds code cost.

---

## 8. Vertical slice (recommended proof)

1. One scannable surface breach (B6 tutorial grammar).
2. Deployable drill capsule + 20–30 s drill vignette.
3. One greybox S1 instance: **Entry**, **Camp**, **Pool** spawn nodes.
4. Unlock on first foot visit; drill map with three destinations after unlock.
5. Drill up to surface anchor; verify vehicle pack state.

**Tickets (when promoted):** extend IO-W1-02 (breach pipeline) + new IO-W1-xx (survey registry + drill transit UI).

---

## 9. Promotion checklist

- [ ] Review against GDD 5.0 Appendix A2 / Chapter 3
- [ ] Resolve open questions in §6
- [ ] Update `Io_Underground_Architecture_Plan.md` §9 (gameplay loops) and breach flow diagram
- [ ] Update `Io_Biome_Exploration_Gameplay_Plan.md` §2.5 if drill hub replaces exit-only breach
- [ ] Add milestone tickets to `Io_World_Content_Milestone_Tickets.md`
- [ ] Optional: GDD Appendix **A2c** subsurface drill transit lock

---

## 10. Revision log

| Date | Change |
|------|--------|
| 2026-08-03 | Initial capture from design conversation — breach-only round trip refined to underground-initiated survey map travel to unlocked surface + subsurface nodes |
