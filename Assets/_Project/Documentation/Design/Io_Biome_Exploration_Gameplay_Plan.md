# Io Biome Exploration & Gameplay Plan

**Status:** Design investigation — **world structure & vehicles locked July 2026**  
**Authority:** GDD 5.0 Chapter 3 (Biome Philosophy), Appendix A2 (pressures), planned A2b (weather).  
**Companion docs:**
- `Io_Underground_Architecture_Plan.md` — subsurface strata & pools  
**Not yet locked** — promote to GDD Appendix A2d after review.

---

## 1. Design goal

Every Io biome must answer: **“What do I do here that I cannot do anywhere else?”**

GDD 5.0 requires five properties per biome:

1. Unique visual identity  
2. Unique survival challenge  
3. Unique resource economy  
4. Unique environmental storytelling  
5. Unique gameplay opportunities  

Exploration is not “walk to marker.” It is a **verb set** — scan, time, route, shelter, extract — that changes per biome and feeds colony progression (research, craft, Echo rescue, Memory Cores).

---

## 2. Exploration framework (shared across all biomes)

### 2.1 Expedition phases

All surface/subsurface runs follow the same **six-phase loop**; biomes change what each phase demands.

| Phase | Player job | Systems involved |
|-------|------------|------------------|
| **Brief** | Pick trio, gear, route, weather window | Roster, Journal map, Colony Ops / Aether-9 comms |
| **Approach** | Traverse from base or drop point to biome edge | Foot, deployable vehicle (if zone allows), cave breach |
| **Operate** | Biome-specific verbs (see §4) | Exposure, weather, combat, scan |
| **Discover** | POI, sample, signal, core fragment | Quest, Echo generator, research |
| **Extract** | Leave with loot / rescued Echo before pressure wins | O₂, carry weight, storm timer |
| **Debrief** | Turn findings into colony value | Science Labs, Purification Hub, BCP queues |

### 2.2 Core exploration verbs (toolkit)

Reuse one interaction + scan + exposure stack; biomes **emphasize different verbs**.

| Verb | Description | Primary input |
|------|-------------|---------------|
| **Route** | Choose safe paths (heat shadows, wind lee, rad cover) | Movement, map, binoculars |
| **Scan** | Reveal POIs, Echo signals, weak rock, gas pockets | Scanner / toolbar |
| **Time** | Move between hazard pulses (geysers, lava, lightning) | Observation, audio telegraph |
| **Shelter** | Pause pressure recovery (cave, habitat bubble, lee zone) | Positioning, Architect deployable |
| **Sample** | Harvest node or research pickup | Interact, inventory |
| **Clear** | Combat arena — nest, patrol, android dig | Trio combat roles |
| **Breach** | Enter subsurface (rappel, tube squeeze, drill) | Infiltrator / Architect |
| **Stabilize** | Short channel — vent seal, door hack, field purify | Class abilities |
| **Extract** | Carry-limited run to exit before escalation | Stamina, O₂, weather clock |

### 2.3 Difficulty layers (stacking)

Biomes differ by **which layers stack**:

```
Zone pressure (always)  +  Weather event (timed)  +  Wildlife tier  +  Resonance modifier
```

Example: *Caldera rim + eruption column + brood wake + post–Memory Core spike* = extreme expedition.

### 2.4 Trio role bias (not hard gates)

| Class | Exploration strength |
|-------|---------------------|
| **Architect Engineer** | Shelters, vent seals, harvest nodes, breach doors |
| **Science Specialist** | Samples, scan range, inoculation efficiency, pool analysis |
| **Combat Tactician** | Nest clears, escort extract, hold lanes during timed crossings |
| **Infiltrator Scout** | Echo signal find, squeeze routes, avoid combat paths, first strike |

Any trio can enter any biome; **synergy bonuses** reward matching comp to activity (see per-biome tables).

### 2.5 World structure & vehicles (locked July 2026)

#### Main map vs instances

| Space type | Role | Content |
|------------|------|---------|
| **Colony main map** | Persistent hub around Command Center | Small traversable mountains, shelter pockets, volatile pool seeps, mineral/ore nodes, instance gates |
| **Biome instances** | Majority of exploration | B1–B7 expeditions, activities, nests, wrecks — loaded for mission |
| **Subsurface instances** | Depth content | Lava tubes, volatile basins, vaults — separate or nested load from surface instance |

**Locked:** Most gameplay biomes are **instanced**, not one open-world Io. The main map is compact and purposeful — not a full moon surface.

#### Main-map terrain — small mountains

- Add **modest mountain areas** on the colony main map (not vast ranges).
- Target height: **~200–300 m** — tall enough to read as highland, small enough to cross in a reasonable expedition.
- Slopes must support **on-foot traversal** and **deployed vehicle play** (see buggy below).
- Mountains provide: vista/wonder beats, scan unlocks, cave breach mouths, ore outcrops, weather shelter lee zones.

#### Resource pockets on main map

Small persistent nodes (regenerate on schedule, not infinite):

- **Shelter** — shallow caves, overhangs, emergency habitat pads  
- **Volatile seeps** — condensate / brine trickles (not full lakes)  
- **Minerals & ore** — starter-tier gather before deep instances unlock  

These teach verbs before the player commits to a full biome instance.

#### Vehicles — deploy from inventory only

**Locked:** No ambient hovercraft or buggy sitting in the world. All vehicles are **packed inventory items** that the player **unpacks** to deploy and **packs** back when done (prototype hovercraft already supports store-in-inventory via `HovercraftUsable`).

| Vehicle | Status | Role |
|---------|--------|------|
| **Hovercraft** | Prototype shipped | Fast transit on **flat / path-tagged** surfaces; low environment resistance |
| **Io Buggy** (6-wheel) | Planned | Environment-resistant; rough terrain, main-map mountains, instance path networks |

**Deploy rules**

1. Player must own packed vehicle in inventory / hotbar.  
2. Unpack only in **Vehicle Deploy Zones** (flat pad, path surface, or flagged instance entry).  
3. Some instances allow deploy **inside** on **path-like surfaces** (splines or wide navmesh lanes) — not every chamber.  
4. Pack before extract if instance rules require (or leave deployed at main-map garage pad — design TBD per instance type).  
5. **Ion lightning** still punishes exposed metal — vehicles are not immunity.  

**Biome / instance vehicle allowance**

| Context | Hovercraft | Io Buggy (6-wheel) |
|---------|------------|-------------------|
| Main map flats & paths | Yes | Yes |
| Main map mountains (200–300 m) | No / risky | Yes (primary design target) |
| B1 Sulfur Plains instance | Path lanes only | Path lanes only |
| B2 Geyser Fields | Rare flat pads | Limited — vent gaps |
| B3 Ash Flats | Flat corridors | Yes on packed ash roads |
| B4 Calderas | No | No (foot + heat routing) |
| B5 Polar Flats | Flat ice crust lanes | Yes between cover points |
| B6 Highlands instance | Path to breach | Yes — highland roads |
| B7 Ruins | No | No (silent / puzzle routes) |
| Underground Stratum 1–2 | No | No (except marked underground hauler path — optional late) |
| Underground Stratum 3+ | No | No |

**Future:** packed **hover-skiff** module for flooded tube paths (see underground plan) — same deploy rules, water-path tag only.

---

## 3. Surface biome roster (locked proposal — 7 biomes)

Each biome links to **dominant pressure**, **signature weather**, **primary verbs**, and **subsurface mouth**.

| ID | Biome | Dominant pressure | Signature weather | Subsurface entry |
|----|-------|-------------------|-------------------|------------------|
| B1 | **Sulfur Plains** | Sulfur | Sulfur Storm, Ash Gale | Shallow seeps → Stratum 1 |
| B2 | **Geyser Fields** | Sulfur + Volcano | Geyser Field Surge | Vent shafts → Stratum 2 |
| B3 | **Ash Flats & Ridges** | Thermal (swing) + Volcano | Ash Gale, Dust Spouts | Wind-cut tube breaches |
| B4 | **Lava Calderas** | Volcano + Thermal (heat) | Lava Surge, Eruption Column | Collapse sinks → Stratum 3–4 |
| B5 | **Polar Radiation Flats** | Radiation + Thermal (cold) | Jovian Rad Pulse, Ion Lightning | Cold trap tubes → Stratum 2–3 |
| B6 | **Basalt Highlands** | Thermal + Radiation (mild) | Tremor Swarm, Lightning | Primary cave network → Stratum 1–2 |
| B7 | **Precursor Ruin Belt** | Radiation + Resonance | Any + Supercell bias | Stratum 5 vault gates |

**Overlay (not a biome):** **Expedition Graveyard** — story prop density layer applied across B1–B6 (wrecks, androids, Rust Gardens).

---

## 4. Per-biome exploration & gameplay

### B1 — Sulfur Plains

| Requirement | Content |
|-------------|---------|
| **Visual** | Yellow flats, brimstone fans, low silhouettes, haze |
| **Survival** | Sulfur saturation; filter drain; low cover |
| **Resources** | Sulfur salts, brimstone fan fiber, condensate at seeps |
| **Story** | First failed camps; “we thought the storm would pass” logs |
| **Gameplay** | **Route + Shelter** — lane navigation between storm fronts |

**Signature activities**
- **Storm window run** — dash between shelter rocks during sulfur lull.
- **Brimstone harvest** — timed gather at fans before storm builds.
- **Echo rescue** — signal in open plain; Tactician holds skitter packs while Scout marks safe lane.

**Trio synergy:** Architect portable filter bubble; Scout storm timing callouts.

**Wildlife:** Sulfur Hounds (pack), Brimstone Fans (flora), Cinder Skitters.

---

### B2 — Geyser Fields

| Requirement | Content |
|-------------|---------|
| **Visual** | Steam columns, rainbow mineral crust, rhythmic pulses |
| **Survival** | Burst heat + sulfur spikes on vent cycle |
| **Resources** | Vent minerals, pressurized gas pods, geyser catalysts |
| **Story** | Corporate drilling rigs destroyed by vent mapping errors |
| **Gameplay** | **Time + Route** — learn vent cadence; audio telegraph |

**Signature activities**
- **Vent crossing** — memorize hiss → blast → cooldown (Echo rescue setpiece from GDD 3.0).
- **Vent Crab nest clear** — destroy queen vent or stealth past for gas harvest.
- **Pressure tap** — Architect seals vent to open rare side tunnel (Stratum 2).

**Trio synergy:** Scout times bursts; Architect seal; Tactician clears crab workers.

**Wildlife:** Vent Crabs (nest), Geyser Pods (flora), Plume Moths.

---

### B3 — Ash Flats & Ridges

| Requirement | Content |
|-------------|---------|
| **Visual** | Bronze sky, ash dunes, ridge silhouettes, dust columns |
| **Survival** | Visibility; aim sway; thermal swings day/night |
| **Resources** | Ash ceramics, ridge ore, wind-scoured alloys from wrecks |
| **Story** | Lost survey teams; half-buried beacons still pinging |
| **Gameplay** | **Route + Scan** — navigate in low vis; dust spout dodge |

**Signature activities**
- **Ridge recon** — scan from high point to unlock map sector (Science bonus).
- **Spout alley** — Infiltrator-led run through wandering dust spouts.
- **Buried wreck salvage** — dig out android or supply cache before ash gale peaks.

**Trio synergy:** Scout leads blind routing; Specialist extends scan through ash.

**Wildlife:** Dust Spout Cluster, Basalt Jackals (pack), Ash Gale embedded spawns.

---

### B4 — Lava Calderas

| Requirement | Content |
|-------------|---------|
| **Visual** | Lava lakes, obsidian rim, eruption plumes, heat shimmer |
| **Survival** | Extreme heat pole; lava instant-kill; tremor knockback |
| **Resources** | Obsidian, heat cells, caldera salts, rare melt-lens shards |
| **Story** | Aether-9 crew death site candidates; “something watched from the rim” |
| **Gameplay** | **Route + Time** — heat-shadow paths; eruption windows for rare nodes |

**Signature activities**
- **Rim survey** — tag eruption timing for colony Ops (unlocks caldera map layer).
- **Lens crossing** — jump silicate melt lenses when crust is cooling (thermal read).
- **Caldera Mantis hunt** — solo apex optional elite; drops shell armor material.
- **Collapse dive** — enter Stratum 3–4 sink; extract-before-heat-returns timer.

**Trio synergy:** Tactician draws mantis; Architect heat shelter on cooldown; Specialist thermal read.

**Wildlife:** Caldera Mantis (solo), Plume Moths, Heat Eel (subsurface edge).

---

### B5 — Polar Radiation Flats

| Requirement | Content |
|-------------|---------|
| **Visual** | Jupiter dome, frost SO₂ crust, aurora-like rad shimmer |
| **Survival** | Radiation + cold pole thermal; exposure between cover |
| **Resources** | Rad-shield gel precursors, void kelp, magnetic ore |
| **Story** | Early isotope rush camps; illegal core smuggling hints |
| **Gameplay** | **Route + Shelter** — sprint between cover; rad pulse prediction |

**Signature activities**
- **Cover-to-cover relay** — place portable beacons for companion path AI.
- **Pulse ride** — enter cold trap tube as rad front passes (Infiltrator squeeze).
- **Void kelp grove scan** — Science setpiece; wrong noise triggers Resonance echo.

**Trio synergy:** Specialist inoculations; Architect rad baffle; Scout finds cover lanes.

**Wildlife:** Void Kelp (flora), Magnet Wyrm (solo, subsurface), Rift Stalkers.

---

### B6 — Basalt Highlands

| Requirement | Content |
|-------------|---------|
| **Visual** | Plateaus, cliff tubes, wind scars, longest horizons |
| **Survival** | Mixed pressures; cliff fall; tremor rockfall |
| **Resources** | Building stone, tube lace, generic expedition supplies |
| **Gameplay** | **Breach + Route** — hub biome linking others; cave camp staging |

**Signature activities**
- **Tube mapping** — first sustained subsurface expedition; map fog clear.
- **Highland vista** — Experience Director “wonder” trigger; photo/chronicle hook.
- **Multi-breach choice** — pick tube by pressure profile (rad vs sulfur vs heat).
- **Brood tunnel discovery** — optional nest dungeon entrance.

**Trio synergy:** All classes; tutorial biome for underground grammar.

**Wildlife:** Tube Jackals, Brood Tunnel mouths, Glass Hive cliff variants.

---

### B7 — Precursor Ruin Belt

| Requirement | Content |
|-------------|---------|
| **Visual** | Non-human geometry, teal Aether glow, silent zones |
| **Survival** | Radiation + Saturation drift; android patrols |
| **Resources** | Aether samples, core fragments, precursor alloys |
| **Story** | Memory Core sites; Aether-9 memory gaps; Still Hunter traces |
| **Gameplay** | **Scan + Stabilize + Clear** — puzzle combat hybrid |

**Signature activities**
- **Vault approach** — align three surface locks to open Stratum 5 gate.
- **Android dig** — Rust Garden nest on corrupted expedition tech.
- **Core fragment recovery** — leads to Aether-9 repair / Resonance arc.
- **Silent escort** — no combat noise or Echo Lichen triggers alarm.

**Trio synergy:** Specialist scan puzzles; Infiltrator silent route; Tactician android clear.

**Wildlife:** Vault Stalker, Rust Garden, Rift Stalkers, corrupted androids.

---

## 5. Subsurface ↔ surface pairing

Surface biomes are **doors**; underground strata are **depth**. One biome can feed multiple strata.

| Surface biome | Typical strata | Distinct underground gameplay |
|---------------|----------------|------------------------------|
| B1 Sulfur Plains | 1 | Quick refuge caves; shallow harvest |
| B2 Geyser Fields | 2 | Timed vent locks; steam navigation |
| B3 Ash Flats | 1–2 | Ash-choked tubes; low vis navigation |
| B4 Calderas | 3–4 | Brine basins; heat timer; Basin Mantis |
| B5 Polar Flats | 2–3 | Condensate pools; rad creep; Void Kelp |
| B6 Highlands | 1–3 | Hub dungeons; brood tunnels; mapping |
| B7 Ruin Belt | 5 | Vault puzzles; Aether seeps; story locks |

**Rule:** surface expedition picks **entry**; depth picks **risk/reward**. Deeper ≠ “harder reskin” — new verbs (wade, gas dome, film ambush).

---

## 6. Activity types (mission grammar)

Procedural expeditions and story quests reuse **activity templates**:

| Template | Goal | Best biomes | Duration |
|----------|------|-------------|----------|
| **Recon Scan** | Unlock map % / POI | B3, B6, B5 | Short |
| **Harvest Window** | Gather before weather peaks | B1, B2, B4 | Short–medium |
| **Echo Rescue** | Setpiece + integrate Echo | Any; signals bias B4, B7 | Medium |
| **Nest Clear** | Brood / crab / hive | B2, B6, underground | Medium |
| **Salvage Run** | Wreck/android loot | Graveyard overlay, B3 | Short |
| **Survey Sample** | Science Specialist delivery | B5, B7, pools | Medium |
| **Depth Push** | Reach stratum milestone | B6 hub → B4/B5 deep | Long |
| **Core Recovery** | Memory Core arc | B7, deep Aether seeps | Long |
| **Escort Extract** | Carry injured / artifact out | Any under active weather | Variable |

**Director weights** activities by: biome unlock mask, weather state, Resonance phase, colony need (O₂ low → condensate jobs).

---

## 7. Progression & unlock flow

### 7.1 Biome discovery order (recommended campaign arc)

```
B6 Highlands (hub) → B1 Plains → B2 Geysers → B3 Ash Flats
    → B4 Calderas OR B5 Polar (player choice branch)
    → B7 Ruin Belt (after first Memory Core thread)
```

Not a hard lock — **exposure without gear** should hurt enough to teach order organically.

### 7.2 Gear & colony gates

| Gate | Unlocks biome activity |
|------|------------------------|
| Base env suits (tier 1) | B1, B6 surface |
| Sulfur filters | B1 storm windows, B2 edges |
| Thermal gel / heat tier | B4 rim (not core) |
| Rad inoculation | B5 relay runs |
| Portable habitat | B4 eruption windows, B2 vent fields |
| Packed hovercraft (inventory) | Fast flat/path transit on main map + allowed instances |
| Io Buggy (6-wheel, inventory) | Main-map mountains, B3/B5/B6 path networks |
| Drill / breach kit | Stratum 2+ reliably |
| Resonance suit tier | B7, Stratum 5 |

### 7.3 Map & fog-of-war

- **Surface:** sector reveal by recon scan + vista points (B6, B3 ridges).
- **Subsurface:** per-tube reveal; compass unreliable Stratum 3+.
- **Journal:** biome tab shows pressure profile, weather affinity, known POIs, active threats.

---

## 8. Weather × biome interaction matrix

Cross-reference planned A2b weather lock:

| Weather | Biomes most affected | Exploration change |
|---------|---------------------|-------------------|
| Sulfur Storm | B1, B2 | Shelter gameplay; extract abort |
| Ion Lightning | B5, B6, B7 | Metal route avoidance; deployed vehicle strike risk |
| Ash Gale | B1, B3 | Scan-led navigation; spout embed |
| Dust Spouts | B3 | Micro-dungeon dodging |
| Lava Surge | B4 | Path reroute; bridge burn |
| Geyser Surge | B2 | Vent cycle acceleration |
| Eruption Column | B4 | Timed rare node spawn |
| Tremor Swarm | B4, B6, caves | Rockfall; brood wake underground |
| Rad Pulse | B5, B7 | Cover sprint; pulse prediction |
| Resonance Supercell | Global | Expedition recall; base pause |

---

## 9. Economy hooks per biome

| Biome | Feeds colony systems |
|-------|---------------------|
| B1 | Sulfur filters, brimstone craft, storm warnings |
| B2 | Gas cells, catalysts, Geothermal Harvester inputs |
| B3 | Ceramics, optics, comms repair parts |
| B4 | Heat cells, obsidian armor, volcano offsets |
| B5 | Rad gel, Purification Hub, Science inoculations |
| B6 | General build stone, tube camp modules, map data |
| B7 | Memory Core arc, Aether research, endgame craft |

**AC** still pays vendors; biomes supply **material identity**, not a second currency.

---

## 10. Multiplayer-of-sorts: trio coordination beats

Designed for **single-player commanding a trio**, not co-op.

| Beat | Description |
|------|-------------|
| **Split attention** | Player fights while companions hold lane / scan (AI commands) |
| **Sync ability** | Architect bubble + Scout rescue + Tactician hold = Echo integration |
| **Extract drama** | One companion down → carry vs abandon vs habitat camp |
| **Weather callout** | Ops radio warns; player chooses push or abort |
| **Class quest** | One activity per class tutorial biome (B2 Engineer, B5 Science, B4 Tactician, B6 Scout) |

---

## 11. Production phasing

| Phase | Deliverable | Depends on |
|-------|-------------|------------|
| **E0** | Biome data SO: pressure profile, verbs, weather weights, instance + vehicle tags | Exposure zones (shipped) |
| **E1** | Main map blockout: colony hub + 200–300 m mountains + resource pockets | Io terrain blockout |
| **E1b** | B6 Highlands **instance** + Route/Scan/Shelter verbs | E1 |
| **E2** | B1 + B2 + activity templates (Recon, Harvest, Rescue) | WeatherDirector sulfur + geyser |
| **E3** | B3 + B4 + Timed verb polish | Thermal/volcano HUD |
| **E4** | B5 + rad relay gameplay | Inoculation loop |
| **E5** | Underground pairing (Stratum 1–3) | Underground architecture P0–P3 |
| **E6** | B7 + Memory Core activities | Aether-9 arc (B4 #8) |
| **E7** | Director activity weighting + biome unlock mask | WorldState persistence |
| **E8** | Io Buggy (6-wheel) deploy/pack + env resistance profile | Vehicle deploy zones on E1/E1b |

**Starts with Io biome pass (B4 #9)** — main map shell first, then instances.

---

## 12. Open questions

1. ~~**Open world vs instances**~~ — **Locked:** mostly instances; small persistent main-map pockets.  
2. ~~**Hovercraft biomes**~~ — **Locked:** no ambient vehicles; deploy from inventory on path-tagged zones only.  
3. **Night cycle** — thermal swing meaningful on B3/B5 or static lighting per biome?  
4. **Player branch** — B4 vs B5 first after mid-game: moral choice or gear-gated only?  
5. **Echo signal density** — fixed per biome or director-driven scarcity?  
6. **Pack-on-extract** — must vehicle repack to leave instance, or allow main-map garage staging?

---

## 13. Promotion path to canon

When approved, fold into **GDD Appendix A2d — Io Biome & Exploration Lock**:

- Seven surface biomes (B1–B7)
- Six-phase expedition loop
- Exploration verb toolkit
- Activity template grammar
- Biome × weather × subsurface pairing table
- Campaign discovery order + gear gates
- **World structure lock:** main map + instances, 200–300 m mountains, deploy-only vehicles

---

*Dark Matter Studios — Dark Matter: Genesis — World Design*
