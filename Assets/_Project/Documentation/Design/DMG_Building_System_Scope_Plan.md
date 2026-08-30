# Dark Matter: Genesis — Building System Scope Plan (Merged + Expanded)

**Status:** Scope lock for implementation  
**Merged from:** GDD 5.0 Appendix A7/A6, `GAME_BREAKDOWN.txt`, prologue/act bibles, asset-mapped plans, `Audit_05_Colony.md`, `World_Engine_Disk_Status.md`, desktop `DMG-building-system-plan.md` (2026-08-27)  
**Last updated:** 2026-08-28 (story + disk expansion)  

This document is the **single implementation scope** for building. Where the desktop plan and GDD disagree on *feel*, the desktop plan wins for placement/materialization UX. Where GDD locks economy, BCP tabs, storms, colony sim, and story beats, GDD wins.

---

## 0. Executive summary

**Player fantasy:** Death Stranding construction + NMS/Subnautica hologram validity — scan Io wrecks to learn, hold-multitool to materialize, operate camp through Building Control Panels while Colony Ops (then Kairos) drives the Memory Core hunt.

**Story spine:** Kade crashes on Io (Act 0) → claims Camp Plateau and bootstraps a real colony (Act I) → earns field certification and wakes **Aether-9 / Kairos** (Act II) → accepts the **10 Memory Core** mandate (Act II end) → each core triggers **Resonance Events** that stress and grow the base (Act III+).

**Engineering spine:** Six ordered slices (definition → hold-construct → wrecks → authoring → save → BCP depth), reusing multitool, BCP, scanner, reverse dissolve, and World Engine `BuildingSnapshot`.

**Gap today:** BCP shell + queue registry exist; **no** `BuildingDefinition`, ghost/wreck pipeline, or prologue placement quests on disk.

---

## 1. Fantasy & pillars

| Pillar | Description |
|--------|-------------|
| **Feel** | Death Stranding construction + No Man’s Sky / Subnautica hologram validity |
| **Not** | Fallout 4 junk-scrap snap; freeform glue parts; NavMesh-driven placement |
| **Learn loop** | Wrecks teach — scan Io ruin → unlock blueprint → repair in place or place new copy later |
| **Materialize** | Hold-construct: silhouette undissolves into finished prefab (`EnemyDisintegrate` reversed) |
| **Operate** | Finished major structures use **Building Control Panels** (GDD lock) |
| **Story** | Buildings are narrative proof of foothold (Act I), competence (Act II), and campaign growth (Act III Resonance) |
| **Economy** | Resources + AC where GDD applies; Journal Craft = library only |

**Validity hologram:** green = valid seat, red = invalid. Cannot commit red.  
**Resources:** drain **during hold**, not on ghost commit (cancel refunds drained ticks).

---

## 2. Main story — how building serves the campaign

### Story overview (Acts 0 → III)

| Act | Name | Player fantasy | Building role |
|-----|------|----------------|---------------|
| **0** | Charter | Who is Kade before Io? | None — UI only |
| **I** | Landing & Camp | We survived; we have a foothold | **First camp:** CC Seed → Shelter → Crafting Station; learn BCP |
| **II** | Cert & Machine | We earned deep tools; we woke something | Camp must **still stand** at prologue end; relay/caldera are story POIs not player-placed |
| **III+** | Ten Memories | Hunt cores; Io pushes back | Resonance Events **injure base-22, pause queues, unlock new facilities** |

**Locked lore rules (building-adjacent):**
- Pre-prologue: no Aether-9 name; dormant shell = liaison/probe/ledger contact only.
- Act I: Helix Meridian survey paint = **rumor**, not exposition.
- Act II end: Kairos names **ten** Memory Cores; trust = **Angry**.
- Act III: each core attach → 10–15 min Resonance Event (storms, base injury, Echo spawns).

### Act 0 — Charter (no building)

- Kade background, free starter companion, 0 AC, shuttle cinematic → Landing Scar.
- **Building scope:** none. Establishes companion who later appears on BCP Companions tab.

### Act I — Landing & Camp (building is the act climax)

| Scene | Quest (FUTURE) | Building beat | Implementation |
|-------|----------------|---------------|----------------|
| **A — Landing Scar** | `prologue_01_touchdown` | None | Movement, scan stake, moths |
| **B — Resource Ring** | `prologue_02_scavenge` | Fabricator ruin (Helix rumor); grant **Camp Beacon Kit** | Wreck scan could unlock CC Seed def (Slice 3) |
| **C — Claim Plateau** | `prologue_03_claim_site` | Clear nest → **place CC Seed** on snap pad → Emergency Cell power → **BCP Overview** | Slices 1–2; pad snap validity |
| **D — Bootstrap** | `prologue_04_bootstrap` | **Shelter** + **Crafting Station** → assign companion → **mini sulfur gust** (queue pause) | Slices 2–6 partial |

**Act I exit criteria (story):**
- [ ] CC Seed powered
- [ ] Shelter + Crafting Station live
- [ ] Companion assigned on BCP
- [ ] Mini gust survived (EnvironmentalCrisisHudMode + queue pause preview)

**Player line at exit:** *“We have a real foothold on Io.”*

### Act II — Certification, Ridge & Aether-9 (camp must persist)

| Scene | Quest (FUTURE) | Building beat |
|-------|----------------|---------------|
| **E — Lv5 cert** | `prologue_05_field_cert` | Settlement recipes (framing, scrubber) gated behind cert — crafted at **existing station** |
| **F — Ridge** | `prologue_06_ridge` | Relay Pylon = **story device interact**, not Lite Building place (reuse puzzle grammar) |
| **G — Wake Aether-9** | `prologue_07_aether_repair` | **Aether-9 shell** = authored caldera POI; repair = quest slots, not multitool pipeline (v1) |
| **H — Echo + mandate** | `prologue_08` + `prologue_end_ten_cores` | Camp standing check; Journal **0/10 cores** tracker |

**Act II exit criteria (story + building):**
- [ ] Camp still standing (Seed + Shelter + Station from Act I)
- [ ] Lv5 + mining or harvesting cert
- [ ] Aether-9 awakened (Angry)
- [ ] First Echo rescued
- [ ] 10-core hunt accepted

**Building scope in Act II:** no new major placements required for mainline — proves **save/load** and **storm pause** on existing camp matter.

### Act III+ — Ten Memories (building grows under pressure)

**Loop per core:** Find → setpiece → attach to Kairos → **Resonance Event** → fragment + unlock.

| Resonance impact on building (GDD + Acts plan) | System hook |
|------------------------------------------------|-------------|
| Sulfur / supercell weather | `BuildingOperationRegistry` queue **pause** |
| Command Center damage | Building injury / heal loop (base-22 shelter in CC rooms — future) |
| New facility unlock | Quest reward → `BuildingDefinition` unlock or wreck scan in new biome |
| Echo spawn chance | `EchoGenerator` during event — not building, but same director slice |

**Post-prologue named buildings (GDD A7) — campaign unlock order (draft):**

| Building | Story gate | Role |
|----------|------------|------|
| Command Center (full) | After Core 1–2 Resonance | Upgrade from Seed; aggregate base-22 sim |
| Echo Reclamation Chamber | Core 2–3 aftermath | Echo holding / reclaim UX |
| Purification Hub | B1 sulfur pressure | O₂ / strain gameplay |
| Medical Facility | Core 3–4 + injured companion beat | Heals, inoculations |
| Science Labs | Core 4–5 | Research queues on BCP Production |
| Probe Uplink / Comms module | Trust ≥ Wary | Kairos advisory path |
| Geothermal Harvester / Stabilizer | B4 caldera band | Mining attachment module |
| Resonance Beacon | Late Act III | Core hunt navigation |

Each becomes a `BuildingDefinition` + optional **world wreck** in the target biome content scene (`Terrain_X_Y_Content`).

---

## 3. What we have in the game today (disk truth)

### Playable prototype core (GDD B1 — shipped)

| System | On disk | Building relevance |
|--------|---------|-------------------|
| Player / combat / survival | Invector bridge, `SurvivalStats`, exposure zones | Carry friction for Emergency Cell (quest layer) |
| Inventory + hotbar + craft | 24-slot inventory, `CraftingUI`, stations | Recipe language for `BuildingDefinition.recipe` |
| Journal hub | Quest, Map, Craft library, Companions, Skills | **Not** primary production UI (GDD lock) |
| Quests | `QuestManager` + 4 live quests (`GatherRocks`, etc.) | Prologue quests **not authored yet** |
| Economy | AC on save/HUD; starter companion pick | Lite Building costs resources + AC at vendors |
| Roster / trio | `PioneerRosterManager`, expedition UI | BCP Companions tab assigns base-22 |
| Echoes | `EchoGenerator`, world entities, rescue path | Parallel to building; chronicle hooks |
| Scanner / optics | `ScannableTarget`, `OpticsController`, scanner sweep | **Wreck → blueprint unlock** |
| Crisis HUD | `EnvironmentalCrisisHudMode` | Mini gust + storm queue pause preview |
| World Engine spine | `Features/GameState`, `WorldState`, `Directors`, `Validation` | `BuildingSnapshot` adapter exists |
| Gaia terrain | 4×4 tiles + 16 content scenes + impostors | Wrecks/dressing in `Terrain_X_Y_Content` |
| Active scene | `Dark Matter Genesis v1.6.2.unity` | Border fences in v1.6.x; systems scene |

### Building-specific (partial — GDD B2)

| Asset / script | Status | Notes |
|----------------|--------|-------|
| `BuildingControlPanel.cs` | **Shipped** | `IWorldUsable`, E prompt, craft station bind |
| `BuildingControlPanelUI.cs` (+ partials) | **Shipped** | Overview, Companions, Production, Craft, Changes, Health |
| `BuildingOperationRegistry.cs` | **Shipped** | Assignments (4 slots), demo queue, save snapshot |
| `FacilityTaskRunner.cs` | **Shipped** | Production tick bridge |
| `BuildingControlAssignmentHints.cs` | **Shipped** | Companion assign UX hints |
| `PowerGenerator.cs` / `PowerConsumer.cs` | **Shipped** | CC Seed power hook (Emergency Cell quest) |
| `PioneerClassTaskAffinity` / `BaseRoleCompanionBonusService` | **Shipped** | BCP role bonuses |
| `BuildingSnapshot` / `BuildingGameStateProvider` | **Shipped** | Save spine — extend for placed instances |
| `Shelter_Safe_Zone.prefab` | **Shipped** | Gust safe volume stand-in for Shelter |
| `Prefabs/Buildings/Command Center Variant.prefab` | **Shipped** | Art reference — not wired to placement pipeline |
| `Prefabs/Buildings/Science Lab Variant.prefab` | **Shipped** | Art reference |
| `ItemType.Multitool` | **Enum only** | No placement controller yet |
| `BuildingDefinition` / `BuildingGhost` / `BuildingWreck` | **Not started** | Core of this plan |
| Materialization / hold-construct | **Not started** | GDD B3 #7 |
| Prologue building quests | **Not started** | `prologue_01`–`prologue_08` FUTURE |
| Live `WeatherDirector` storm scheduler | **Partial** | Crisis HUD without full factory sim |
| Kairos shell + repair quest | **Not started** | Separate from multitool (authored POI) |

### Live quests vs story plan

| On disk today | Story plan (FUTURE) |
|---------------|---------------------|
| `GatherRocks`, `Get more Rocks`, `GuideSupplyRun`, `One_More` | `prologue_01_touchdown` … `prologue_end_ten_cores` |

Building implementation **does not block** on full prologue quest authoring — Slice 1 can prove on existing workbench/power prefab; prologue hooks land in Slice 3+.

### Communications / Kairos (context)

- **Colony Ops** is the radio voice through Act I–II until Kairos awakens.
- `Features/Communications` runtime **absent** (World Engine Run 2) — building queues and crisis copy can use stubs until rule-based comms lands.
- Kairos attach UI / trust ladder = Act II–III story, not Slice 1.

---

## 4. GDD alignment (non-negotiable)

### Lite Building → Full materialization path

GDD **Lite Building** is the camp-scale loop (Command Center anchor, shelters, utilities). This scope plan **is** the materialization pipeline GDD Appendix B lists as not started — implemented in slices below.

### Building Control Panels (Appendix A7 — locked UX)

- In-world terminal (**E**) → fullscreen overlay (**not** Journal)
- Tabs: **Overview | Companions | Production | Craft | Changes**
- Assign base-22 companions, production/craft queues, per-building settings
- Queues run on expedition; **pause during sulfur storms / Resonance Supercells**
- **Extend** existing UI — do not replace

### Attachment modules (post–Slice 5 / Act III band)

Generators, power grids, auto gather, logistics, communications, defense, mining — attach to cores; feed BCP Production / Changes tabs.

### Base-22 colony rules (Appendix A)

- Base companions impervious to most pressures; **sulfur storms** → Command Center rooms
- Building damage **injures, never kills** companions
- Resonance Events may spike storms and injure base-22 — hooks in Slice 6 + Act III directors

---

## 5. Two object kinds, one multitool

```mermaid
flowchart TB
  subgraph learn [Learn]
    scan[Scan wreck / quest / schematic]
    def[BuildingDefinition unlocked]
  end
  subgraph place [Place]
    holo[Aim hologram green/red]
    ghost[Commit ghost]
  end
  subgraph build [Build]
    hold[Hold multitool + mats]
    done[Finished prefab]
  end
  subgraph operate [Operate]
    bcp[BuildingControlPanel E]
    queues[Production queues]
  end
  scan --> def
  def --> holo
  holo --> ghost
  ghost --> hold
  hold --> done
  done --> bcp
  bcp --> queues
  wreck[World wreck in content scene] --> scan
  wreck --> hold
```

### A. World wreck (authored dressing)

- Lives in **`Terrain_X_Y_Content`** scenes — **not** gitignored Gaia terrain YAML
- **Act I example:** Portable Fabricator Ruin (Scene B) — scan teaches craft station or CC-related def
- **Act III example:** Ridge Archive android shell — scan teaches comms module wreck
- Walk up → **scan** → unlock `BuildingDefinition`
- Hold multitool + mats → **repair in place** (same construct path)
- **E on wreck:** scan/repair — **not** finished BCP

### B. New build (learned blueprint)

- Unlock via scan, schematic, quest, or Resonance reward
- Equip multitool → blueprint select (deploy menu pattern)
- Hologram → ghost → hold → finished → BCP

---

## 6. Validity rules (NMS / Subnautica)

Green only if **all** pass:

| Check | Method |
|-------|--------|
| Ground contact | Gaia tile / Unity Terrain ray — **not** NavMesh |
| Slope | ≤ `BuildingDefinition.maxSlope` |
| Overlap | No other buildings, wrecks, blocking colliders |
| Playable bound | Inside v1.6 **construction / world border fences** |
| Story snap pads | Optional `BuildingSnapPad` volumes (CC Seed plateau — Act I-C) |
| Keep-clear | Optional later (hover paths) — **not v1** |

**No NavMesh** for placement or buildings.

---

## 7. Data model

### `BuildingDefinition` (ScriptableObject) — **new**

| Field | Purpose |
|-------|---------|
| `id` | Stable save key — never rename after ship |
| `displayName` | BCP header + UI |
| `finishedPrefab` | Usable building |
| `wreckPrefab` | Optional ruined visual |
| `recipe` | Mats + counts — crafting inventory language |
| `footprint` | Box/capsule check shape |
| `maxSlope` | Degrees |
| `unlock` | Scan / schematic / quest / `requiredPlayerLevel` |
| `constructTime` | Hold duration (seconds) |
| `controlPanel` / `craftStation` | BCP mode + `CraftingStationType` |
| `ghostMaterial` | Palette hologram instance |
| `storyAct` | Optional: `Prologue`, `Act3`, etc. — for authoring filters |
| `requiresSnapPad` | Optional: CC Seed pad on plateau |

### Runtime components

| Component | Role | Status |
|-----------|------|--------|
| `BuildingGhost` | Committed silhouette, progress | **New** |
| `BuildingWreck` | Scan target + in-place repair | **New** |
| `BuildingSnapPad` | Story placement volumes | **New** (thin) |
| `BuildingControlPanel` | E terminal | **Exists** |
| `BuildingOperationRegistry` | Queues / assignments | **Exists** |
| `PowerGenerator` / `PowerConsumer` | Power graph | **Exists** |

### Save payload (Slice 5)

Per player-placed instance: `definitionId`, world pose, ghost vs complete, construct progress, drained resources state. Extend `BuildingSnapshot` / `BuildingGameStateProvider`. Rehydrate on tile load — **never** write into Gaia terrain YAML.

---

## 8. Reuse map (do not reinvent)

| Piece | Location / notes |
|-------|------------------|
| Multitool item type | `ItemType.Multitool` in `ItemData.cs` |
| BCP interact + UI | `BuildingControlPanel`, `BuildingControlPanelUI` (+ partials) |
| Deploy UX pattern | Hovercraft / walker drill deploy |
| Scanner unlocks | `ScannableTarget` + optics flow |
| Construct VFX | `EnemyDisintegrate` / `EnemyDisintegrationEffect` — `_DissolveAmount` **1 → 0** |
| UI palette | `DarkMatterGenesisUiPalette` — hologram **not gold** |
| Playable fence | v1.6 border fences — placement tests against them |
| Crisis / storm pause | `EnvironmentalCrisisHudMode` + `BuildingOperationRegistry` |
| Editor menus | `DarkMatterGenesisEditorMenus` → `Tools/Dark Matter Genesis/Buildings/` |
| World save | `BuildingSnapshot`, `GameSaveSystem` |
| Companion assign | `PioneerRosterManager` + BCP Companions tab |

---

## 9. Story ↔ building unlock matrix (implementation targets)

| ID | Display name | Story gate | Unlock method | Slice | Content scene |
|----|--------------|------------|---------------|-------|---------------|
| `cc_seed` | Command Center Seed | Act I-C | Camp Beacon Kit quest item → snap pad | 2–3 | Plateau content / v1.6 pad |
| `survival_shelter` | Survival Shelter | Act I-D | Quest `prologue_04` + blueprint from CC | 2 | Camp plateau |
| `craft_station_settlement` | Crafting Station | Act I-D | Quest step / schematic | 2 | Camp plateau |
| `module_o2_scrubber` | O₂ Scrubber mount | Act I-D | Craft + place (small footprint) | 4 | Camp rim |
| `relay_pylon_story` | Relay Pylon | Act II-F | **Story interact only** — not Lite Building v1 | — | Ridge content |
| `aether9_shell` | Aether-9 Machine | Act II-G | **Quest repair slots** — not multitool v1 | — | Caldera POI |
| `echo_reclamation` | Echo Reclamation Chamber | Act III Core 2–3 | Resonance unlock + wreck scan | 5–6 | B1 content |
| `purification_hub` | Purification Hub | Act III Core 3–4 | Resonance + B1 wreck | 6 | B1 content |
| `medical_facility` | Medical Facility | Act III Core 4+ | Quest + wreck | 6 | B2+ content |
| `science_labs` | Science Labs | Act III Core 5+ | Resonance reward | 6 | Use existing Science Lab Variant prefab |
| `command_center_full` | Command Center | Act III Core 1–2 upgrade | Upgrade path from Seed | 6 | Camp plateau |

**Prologue minimum ship set:** `cc_seed`, `survival_shelter`, `craft_station_settlement`, `module_o2_scrubber` (+ fabricator ruin as wreck teacher).

---

## 10. World vs save boundaries

| Content type | Where it lives |
|--------------|----------------|
| Authored wrecks, tile dressing | `Terrain_X_Y_Content.unity` (16 scenes on disk) |
| Gaia terrain tiles | Session terrain scenes — not player base truth |
| Construction fences | **Dark Matter Genesis v1.6.x** main scene |
| Player-placed ghosts + buildings | Save → `BuildingSnapshot` |
| Camp Plateau snap pad / survey paint | Content scene or v1.6 authored blockout |
| Aether-9 caldera shell | Authored POI — Act II-G |

---

## 11. Implementation slices (ordered — do not skip)

### Slice 1 — Definition + hologram + instant complete

**Story:** None required — tech proof.  
**Goal:** Prove placement pipeline.

- [ ] `BuildingDefinition` SO + registry by `id`
- [ ] Multitool equip → aim hologram (green/red)
- [ ] Validity: footprint, slope, overlap, v1.6 fence
- [ ] Click → **instant** finished prefab
- [ ] First target: existing **workbench** or `PowerGenerator` mesh in project

**Exit:** Place inside fence; E → BCP opens.

---

### Slice 2 — Hold-construct + reverse dissolve + resource drain

**Story gate:** Enables **Act I-C / I-D** prologue building.  
**Goal:** Materialize feel.

- [ ] Click commits **ghost**
- [ ] Hold: `_DissolveAmount` 1→0, recipe drains over `constructTime`
- [ ] Cancel → refund drained
- [ ] Complete → finished prefab + `BuildingControlPanel` enabled
- [ ] **`BuildingSnapPad`** for CC Seed plateau
- [ ] **`Item_CampBeaconKit`** → starts placement mode for `cc_seed`
- [ ] Hook **mini gust queue pause** on `BuildingOperationRegistry`

**Exit:** Act I-D shelter + station playable with hold-construct.

---

### Slice 3 — Wrecks that teach blueprints

**Story gate:** Act I-B fabricator ruin; Act III biome wrecks.  
**Goal:** Scan-learn-repair loop.

- [ ] `BuildingWreck` + scan → unlock definition
- [ ] Hold-repair in place (Slice 2 path)
- [ ] Place fabricator ruin in prologue content scene
- [ ] Emergency Cell carry = quest item friction (no new building code)

**Exit:** Ruin in content scene → scan → repair → BCP online.

---

### Slice 4 — Authoring window

**Story gate:** Content team can author Act III buildings without programmer per prefab.  
**Goal:** Scale to 10+ definitions.

- [ ] `Tools/Dark Matter Genesis/Buildings/Author Building`
- [ ] Stamp definition, hologram mat, wreck, BCP, recipe
- [ ] Migrate `Command Center Variant` / `Science Lab Variant` to defs

**Exit:** New bench authored in one editor pass.

---

### Slice 5 — Save / load placed instances

**Story gate:** Act II requires **camp still standing** after ridge/caldera.  
**Goal:** Persistent base.

- [ ] Extend `BuildingSnapshot` for ghosts + finished poses
- [ ] Rehydrate on tile/session load
- [ ] Prologue state machine can query “camp complete” flags

**Exit:** Save/load restores camp layout.

---

### Slice 6 — BCP production depth + Act III Resonance hooks

**Story gate:** Act I-D companion assign; Act III queue pause under Resonance.  
**Depends on:** Slices 1–5; World Engine directors (roadmap #4).

- [ ] Companions tab ↔ `PioneerRosterManager` (real assign, not demo)
- [ ] Production tab live queues (`FacilityTaskRunner`)
- [ ] Craft tab ↔ per-definition `craftStation`
- [ ] Changes tab stub → attachment modules
- [ ] Full sulfur + **Resonance Supercell** pause
- [ ] Building injury flags for base-22 (interface for future CC room sim)
- [ ] Quest hooks: `prologue_04` companion assign objective pulse

**Exit:** Assign companion; queue ticks; pauses in crisis test; Act I-D gust pass.

---

### Slice 7 — Post-prologue facility rollout (campaign)

**Story gate:** Act III cores 1–10.  
**Not prologue-blocking.**

- [ ] Author Act III `BuildingDefinition`s per unlock matrix (§9)
- [ ] Wrecks in B1–B7 content scenes
- [ ] `ResonanceEventDirector` → queue pause + damage flags
- [ ] Upgrade path CC Seed → full Command Center
- [ ] Attachment modules: generator, mining, comms (one module at a time)

---

## 12. Prologue QA checklist (building)

Cross-ref `Prologue_Acts_Expanded.md` QA section:

**Act I building beats:**
- [ ] Nest clear blocks red hologram on pad
- [ ] CC Seed snap pad only accepts `cc_seed` def
- [ ] Emergency Cell insert powers seed (`PowerConsumer`)
- [ ] BCP Overview opens; other tabs gated until Scene D
- [ ] Shelter interior = safe during mini gust
- [ ] Companion assign on BCP completes quest objective
- [ ] Queues pause during gust; resume after

**Act II building beats:**
- [ ] Return from caldera — camp buildings still in save
- [ ] No accidental placement during ridge/caldera quests

---

## 13. Out of scope (v1 / prologue)

- Fallout junk-scrap snap
- NavMesh placement or pathing
- Moving border fences to content scenes
- Gold hologram look
- New fullscreen builder HUD
- Multi-tile mega structures
- Gaia User Data as base truth
- Aether-9 shell as multitool build (quest POI only)
- Relay Pylon as player-placed building (story interact)
- Full attachment module graph (Slice 7)
- Maintenance decay loop (design after Slice 5)
- Full Command Center 22-companion room sim (roadmap #5)

---

## 14. Acceptance tests (per slice)

| Slice | Playtest proof |
|-------|----------------|
| 1 | Multitool → green hologram → instant finish → E → BCP |
| 2 | Ghost → hold → dissolve → complete; cancel refunds; snap pad works |
| 3 | Content wreck → scan unlock → repair → BCP |
| 4 | Author Science Lab from variant prefab in editor |
| 5 | Save/load camp after Act I-D sequence |
| 6 | Companion assigned; gust pauses queue; Act I-D completable |
| 7 | Core 1 Resonance pauses camp; new wreck unlock in B1 |

---

## 15. Open decisions

| Question | Recommendation |
|----------|----------------|
| Cancel refund | Refund all drained ticks |
| Blueprint UI | Extend deploy menu first |
| BCP tab gating | Overview-only until Act I-D step 4.3 |
| First author mesh | Scene workbench or `PowerGenerator` |
| Fabricator ruin | Teaches `craft_station_settlement` or `cc_seed`? → **station** (Scene B craft teach) |
| Starter AC | GDD says 5000; story Act 0 says 0 — **follow story doc for prologue** when quests land |
| Kairos comms | Ops only until trust gate; building UI unchanged |

---

## 16. Related documents

| Doc | Use |
|-----|-----|
| `GAME_DESIGN_DOCUMENT_5.0.txt` | A6 Kairos/cores, A7 buildings, A2b weather, B3/B4 |
| `GAME_BREAKDOWN.txt` | §10 building + code pointers |
| `Prologue_Acts_Expanded.md` | Act beat depth + QA |
| `Prologue_Playthrough_And_Camp_Bootstrap_Plan.md` | POI catalog + timing |
| `Acts_0_to_3_Asset_Mapped_Plan.md` | CURRENT/FUTURE assets per act |
| `Stages_0_1_Playthrough_Quest_Stages.md` | Quest stage breakdown |
| `Audit_05_Colony.md` | Architecture risks |
| `World_Engine_Disk_Status.md` | What is actually shipped |

---

## 17. Cross-act dependency graph (building)

```
Act 0 (no building)
  → Act I Slice 1–2: cc_seed, shelter, station on Camp Plateau
  → Act I Slice 3: fabricator wreck (Resource Ring content)
  → Act I Slice 5–6: save camp + gust pause + companion assign
  → Act II: camp persistence check only
  → Act III Slice 7: Resonance + new defs/wrecks per core
  → Campaign: full GDD facility set + attachment modules
```

---

*Constraints:* No NavMesh · Palette hologram not gold · Fences in v1.6 · Wrecks in content scenes · Reuse multitool/BCP/scanner/dissolve · Drain on hold · Instant Slice 1 before hold Slice 2 · Story POIs (Aether-9, Relay) stay quest-driven until explicitly promoted to `BuildingDefinition`.
