# Io Biome Exploration & Gameplay Plan

**Status:** Design investigation — **world structure, vehicles, day/night locked July 2026**  
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

#### Full-scale surface map + instanced underground

| Layer | Role | Content |
|-------|------|---------|
| **Main map (full scale)** | Persistent Io **surface** world | Colony, B1–B7 regions, 200–300 m mountains, shelter, seeps, ore, weather, wildlife, vehicles |
| **Underground (instanced)** | Loaded on enter, unloaded on exit | Stratum 1–5 tubes, pools, broods, vaults — **separate scenes** reached by breach interact / teleport |
| **Surface ↔ underground link** | Anchor points on main map | Each breach stores return position; exit prompt teleports player (and trio) back to surface map |

**Locked (July 2026):**

- The **surface is one full-scale main map** — geographic regions, not separate overworld levels.  
- **Most underground is instanced** (teleport on enter/exit at breaches).  
- **Select few underground areas** are **walk-in on the main map** — no teleport (see §2.7).  
- Optional **nested instance** within an underground run (brood mother, vault core) still allowed.

#### Seamless underground exceptions (no teleport)

A **small curated set** of shallow subsurface spaces exist as **geometry on the main map** (or additive sub-scenes with no loading screen):

| Zone | Location | Purpose |
|------|----------|---------|
| **Colony refuge tubes** | Stratum 1 under / beside Command Center | Tutorial shelter, O₂ relief, first breach without load |
| **B6 Highland skylight tubes** | Walk-in cave mouths on main map | Hub exploration, breach staging |
| **Shallow sulfur seeps** | B1 edge pockets | Early volatile harvest |

All **deep** content (Stratum 3+, brood mothers, vaults, large basins) uses **instance teleport**. When in doubt, **teleport**.

#### Underground enter / exit flow (instanced breaches)

```
Surface main map → enter 10–20 m pack zone → [auto-pack vehicle] → "Enter?" prompt
    → load underground instance
    → play content (wade-only pools; weak rad unless strong-rad zone)
    → exit breach → "Return to surface?" → teleport to anchor → [manual unpack]
```

- **Entry pack zone:** **10–20 m radius** around breach (per-breach tunable within band; default 15 m).  
- **Enter:** interact or volume at breach mouth — player **chooses** to enter.  
- **Exit:** paired return node inside instance.  
- **Walk-in zones:** no pack zone, no teleport — foot transition only.

#### Vehicles — inventory deploy + underground auto-pack

**Locked:** No ambient vehicles. Packed in inventory; unpack manually on surface.

| Action | Behavior |
|--------|----------|
| **Unpack (surface)** | Manual — in **Vehicle Deploy Zone** |
| **Enter instanced breach zone** | **Auto-pack** within **10–20 m** of entry |
| **Exit to surface** | **Manual unpack** |
| **Inside underground** | **Foot only** — no vehicles, **no hover-skiff** |

| Vehicle | Status | Role |
|---------|--------|------|
| **Hovercraft** | Prototype shipped | Flat / path-tagged surface |
| **Io Buggy** (6-wheel) | Planned | Mountains, B3/B6 paths |

**Vehicle allowance — surface regions only**

| Region | Hovercraft | Io Buggy | Notes |
|--------|------------|----------|-------|
| Colony flats & paths | Yes | Yes | Manual unpack at pads |
| Mountains (200–300 m) | No / risky | Yes | |
| B1 Sulfur Plains | Path lanes | Path lanes | |
| B2 Geyser Fields | Rare pads | Limited | |
| B3 Ash Flats | Flat corridors | Ash roads | |
| **B4 Lava Calderas** | **No** | **No** | **Foot only — extreme heat gear required** |
| **B5 Polar Flats** | **No** | **No** | **Foot only — extreme cold + rad gear required** |
| B6 Highlands | Path to breach | Highland roads | |
| B7 Ruins | No | No | Foot — silent routes |
| All underground (instanced) | No | No | Auto-pack at 10–20 m entry zone |

#### Main-map terrain — mountains (200–300 m)

Mountain zones on the full surface map — crossable on foot or Io Buggy where path-tagged. Breach mouths into underground instances at foothills and caldera/polar rims.

#### Resource pockets

Shelter, volatile seeps, minerals, and ore scattered on **surface main map** and in **shallow underground instances** (Stratum 1–2).

---

### 2.6 Day / night cycle & polar temperature (locked July 2026)

**Locked:** Io runs a **day/night cycle**. **Polar regions (B5)** shift **thermal pressure** with the cycle:

| Phase | B5 Polar thermal | Gameplay read |
|-------|------------------|---------------|
| **Day** | Cold pole — severe but manageable with gear | Longer relay windows between cover |
| **Night** | Cold pole **intensifies** — pushes thermal meter toward cold extreme | Shorter exposure windows; inoculation drain faster |

- Night lighting + Jupiter glow on horizon; aurora-like rad shimmer at poles.  
- **B3 Ash Flats** may get minor thermal swing (optional polish); **B5 is the primary night-cycle teaching biome**.  
- **B4 Calderas** stay **heat extreme** day and night; night only slightly cools **rim** zones — core/lava unchanged.  
- Colony Ops / Aether-9 can radio **polar night warnings** when B5 expeditions are planned.

**Gear gates (B4 & B5 — foot only):**

| Region | Required kit (design target) | Without kit |
|--------|------------------------------|-------------|
| **B4 Calderas** | Heat-tier env suit, thermal gel, heat routing tools | Rapid heat pole; health drain near rim |
| **B5 Polar Flats** | Cold-tier env suit, rad inoculation, polar cover gear | Cold pole spike at night; rad stacking |

Different gear **loadouts** — not one suit for both. Player prepares at colony before long B4/B5 pushes.

---

### 2.7 Underground fluids, radiation & instance camps (locked July 2026)

#### Wade only — no swimming, no hover-skiff

**Locked:** All volatile pools, brine lakes, and flooded tubes are **wade-only**.

- Waist-deep max; **slow movement**, **stamina drain**, reduced aim stability.  
- **No swim** animation or dive gameplay. **No hover-skiff** or boat vehicles.  
- Basin Mantis and edge predators still threaten wading players.  
- Deeper basins = wider wade lanes + stronger drain — not immersion swimming.

#### Underground radiation

**Default underground:** radiation pressure is **slow** and **weaker** than open surface (rock shielding).

- Use existing rad meter at **reduced rate** in generic tube zones.  
- **Strong Rad Zones** — authored volumes (B5-linked tubes, precursor leaks, ore veins) spike rad to surface-like or higher.  
- **No separate radon meter** — weak creep is the default underground rad behavior; strong zones are explicit POIs on the map.

#### Small instance camps (far underground / distant instances)

**Locked:** Underground **camps are small and limited** — not colony building.

| Camp function | Allowed | Not allowed |
|---------------|---------|-------------|
| **Recuperate** | O₂ refill (limited), stamina, minor Saturation soothe | Full medical bay |
| **Stock inventory** | Small stash / shared crate for expedition loot | Production queues |
| **NPC scrapper** | **Far-out instances only** — buys junk, sells basics, rumor hooks | Full vendor / roster recruit |

- Colony Command Center remains the **main** base.  
- Instance camps are **forward operating rest stops** — especially in deep B4/B5/B7 instances.  
- Lite Building **does not** apply inside instances except pre-placed camp props.

---

### 2.8 Echo signals — ExperienceDirector (locked July 2026)

**Locked:** Echo signal placement and density are **director-driven**, not fixed per-biome spawn tables.

**Owner:** `ExperienceDirector` (with `SimulationDirector` / WorldState inputs) schedules when and where signals appear. `EchoSignalRegistry` + `EchoGenerator` remain the runtime surface; the director decides **if**, **when**, and **where** to register new signals.

#### Director inputs (read-only snapshots)

| Input | Effect on signals |
|-------|-------------------|
| **Roster vs 25 cap** | Low roster → higher spawn weight; at cap → suppress new signals |
| **Time since last rescue** | Long drought → gentle increase; recent rescue → cooldown |
| **Colony Strain / Saturation** | High Strain → fewer signals (colony overwhelmed); stable → normal |
| **Player region (B1–B7)** | Biome as **weight**, not guarantee — see table below |
| **Story phase** | B5 arc boosts polar-adjacent weights; post-B5 boosts B4/B7 |
| **Weather / Resonance** | Echo Storm (Resonance) = temporary density spike + extra hazards |
| **Active expeditions** | Avoid spawning on top of player; prefer adjacent sectors |

#### Biome weights (not fixed spawns)

Director picks a **weighted region** when spawning; no biome has a static “always one signal here” node.

| Region | Weight bias | Notes |
|--------|-------------|-------|
| B6 Highlands | High early | Tutorial / first rescues near hub |
| B1–B3 | Medium | Mid-early roster fill |
| B5 Polar | High during B5 story arc | Rad/cold rescue setpieces |
| B4 Calderas | High post-B5 | Aether-9 mystery escalation |
| B7 Ruins | High post–Memory Core | Special / rare dispositions |
| Colony safe radius | **Zero** | Never inside Command Center perimeter |

#### Scarcity rules

- **Active signal cap** scales with progression (e.g. 1–2 early, 3–4 mid, 5+ late) — director enforces world-wide max.  
- **One rescue cooldown** after success before next signal of same tier.  
- **Failed rescue** — signal lost permanently; director may spawn replacement elsewhere after delay.  
- **Infiltrator Scout** + player **scan** reveal signals the director has already placed — they do not create new ones.

#### Echo Storm (Resonance modifier)

When a Resonance Event fires (GDD A6): director enters **Echo Storm** mode for 10–15 min — elevated spawn weight, multiple concurrent signals allowed, paired hazard spike. Returns to normal scheduling after event.

#### Comms

Colony Ops / Aether-9 (when unlocked): *“Anomalous Echo trace flagged in [region] — Scout recommended.”* — reflects director spawn, not scripted POI.

---

## 3. Surface biome roster (locked proposal — 7 regions on main map)

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

**Wildlife:** Sulfur Hounds (pack), Brimstone Fans (flora), Cinder Skitters — **full ecology:** `Io_Biome_Ecology_Roster.md` §B1.

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

**Wildlife:** Vent Crabs (nest), Geyser Pods (flora), Plume Moths — **full ecology:** `Io_Biome_Ecology_Roster.md` §B2.

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

**Wildlife:** Dust Spout Cluster, Basalt Jackals (pack), Ash Gale embedded spawns — **full ecology:** `Io_Biome_Ecology_Roster.md` §B3.

---

### B4 — Lava Calderas

| Requirement | Content |
|-------------|---------|
| **Visual** | Lava lakes, obsidian rim, eruption plumes, heat shimmer |
| **Survival** | Extreme heat pole; lava instant-kill; tremor knockback — **foot only, heat-tier suit required** |
| **Resources** | Obsidian, heat cells, caldera salts, rare melt-lens shards |
| **Story** | Aether-9 crew death site candidates; “something watched from the rim” |
| **Gameplay** | **Route + Time** — heat-shadow paths; eruption windows for rare nodes |

**Signature activities**
- **Rim survey** — tag eruption timing for colony Ops (unlocks caldera map layer).
- **Lens crossing** — jump silicate melt lenses when crust is cooling (thermal read).
- **Caldera Mantis hunt** — solo apex optional elite; drops shell armor material.
- **Collapse dive** — enter Stratum 3–4 **instance** via sink; extract-before-heat-returns timer.

**Access:** **No vehicles.** Heat-tier environmental suit + thermal gel minimum; rim vs core tier gates.

**Trio synergy:** Tactician draws mantis; Architect heat shelter on cooldown; Specialist thermal read.

**Wildlife:** Caldera Mantis (solo), Plume Moths, Heat Eel (subsurface edge) — **full ecology:** `Io_Biome_Ecology_Roster.md` §B4.

---

### B5 — Polar Radiation Flats

| Requirement | Content |
|-------------|---------|
| **Visual** | Jupiter dome, frost SO₂ crust, aurora-like rad shimmer |
| **Survival** | Radiation + cold pole thermal (**night intensifies cold**); exposure between cover — **foot only, polar kit required** |
| **Resources** | Rad-shield gel precursors, void kelp, magnetic ore |
| **Story** | Early isotope rush camps; illegal core smuggling hints |
| **Gameplay** | **Route + Shelter** — sprint between cover; rad pulse prediction |

**Signature activities**
- **Cover-to-cover relay** — place portable beacons for companion path AI.
- **Pulse ride** — enter cold trap tube as rad front passes (Infiltrator squeeze).
- **Void kelp grove scan** — Science setpiece; wrong noise triggers Resonance echo.

**Access:** **No vehicles.** Cold-tier suit + rad inoculation; plan around **polar night** windows.

**Trio synergy:** Specialist inoculations; Architect rad baffle; Scout finds cover lanes.

**Wildlife:** Void Kelp (flora), Magnet Wyrm (solo, subsurface), Rift Stalkers — **full ecology:** `Io_Biome_Ecology_Roster.md` §B5.

---

### B6 — Basalt Highlands

| Requirement | Content |
|-------------|---------|
| **Visual** | Plateaus, cliff tubes, wind scars, longest horizons |
| **Survival** | Mixed pressures; cliff fall; tremor rockfall |
| **Resources** | Building stone, tube lace, generic expedition supplies |
| **Gameplay** | **Breach + Route** — hub biome linking others; cave camp staging |

**Signature activities**
- **Tube mapping** — first underground **instance** run via breach; map fog clear.
- **Highland vista** — Experience Director “wonder” trigger; photo/chronicle hook.
- **Multi-breach choice** — pick tube by pressure profile (rad vs sulfur vs heat).
- **Brood tunnel discovery** — optional nest dungeon entrance.

**Trio synergy:** All classes; tutorial biome for underground grammar.

**Wildlife:** Tube Jackals, Brood Tunnel mouths, Glass Hive cliff variants — **full ecology:** `Io_Biome_Ecology_Roster.md` §B6.

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

**Wildlife:** Vault Stalker, Rust Garden, Rift Stalkers, corrupted androids — **full ecology:** `Io_Biome_Ecology_Roster.md` §B7.

---

## 5. Subsurface ↔ surface pairing (instances anchored on main map)

Surface regions sit on the **full main map**. Each breach opens an **underground instance** (teleport/load) keyed to region + stratum. Exit returns to the **surface anchor** at that breach.

| Surface region | Typical instance strata | Distinct underground gameplay |
|---------------|-------------------------|------------------------------|
| B1 Sulfur Plains | 1 | Shallow tubes; quick refuge |
| B2 Geyser Fields | 2 | Vent locks; steam navigation |
| B3 Ash Flats | 1–2 | Ash-choked tubes; low vis |
| B4 Calderas | 3–4 | Brine basins; heat timer; Basin Mantis |
| B5 Polar Flats | 2–3 | Condensate pools; rad creep; **night cold spike** |
| B6 Highlands | 1–3 | Hub instances; brood dungeons |
| B7 Ruin Belt | 5 | Vault puzzles; Aether seeps |

**Rule:** region picks **which instance** loads; stratum picks **risk/reward**. Vehicle **auto-packs** at entry; **manual unpack** on return.

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

**Director weights** activities by: biome unlock mask, weather state, Resonance phase, colony need (O₂ low → condensate jobs). **Echo Rescue** signal availability is **ExperienceDirector** — not a static map POI.

---

## 7. Progression & unlock flow

### 7.1 Biome discovery order & story branch (locked)

```
B6 Highlands (hub) → B1 Plains → B2 Geysers → B3 Ash Flats
    → B5 Polar (story branch #1) → B4 Calderas (story branch #2)
    → B7 Ruin Belt (after first Memory Core thread)
```

**Locked story logic — B5 before B4:**

| Order | Region | Why first |
|-------|--------|-----------|
| **1** | **B5 Polar** | Teaches **rad + cold + night cycle**; feeds **Purification Hub** and rad inoculation craft; isotope-rush / smuggling lore sets up corporate failure themes |
| **2** | **B4 Calderas** | **Escalation** — extreme heat, Aether-9 crew death-site candidates, caldera mystery; player arrives with rad/cold lessons and colony science unlocked |

Player can still **stumble unprepared** into either region; guided quests, Ops radio, and gear checks push **B5 → B4**. After the polar arc, the **ExperienceDirector** raises spawn weights toward B4/B7 (not fixed biome spawns).

### 7.2 Gear & colony gates

| Gate | Unlocks biome activity |
|------|------------------------|
| Base env suits (tier 1) | B1, B6 surface |
| Sulfur filters | B1 storm windows, B2 edges |
| **Heat-tier suit + thermal gel** | **B4 calderas (foot only)** |
| **Cold-tier suit + rad inoculation** | **B5 polar flats (foot only); critical at night** |
| Portable habitat | B4 eruption windows, B2 vent fields |
| Packed hovercraft (inventory) | Surface regions that allow vehicles; auto-packs at breaches |
| Io Buggy (6-wheel, inventory) | Mountains, B3/B6 paths — not B4/B5 |
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
| **E0** | Biome region data SO: pressure, verbs, weather weights, vehicle tags | Exposure zones (shipped) |
| **E1** | **Full-scale main map** blockout: colony + B6 region + 200–300 m mountains | Io terrain / streaming plan |
| **E1b** | Underground instance pipeline: breach → load → exit teleport + vehicle auto-pack | E1 |
| **E2** | B1 + B2 regions + activity templates | WeatherDirector sulfur + geyser |
| **E3** | B3 + B4 + Timed verb polish | Thermal/volcano HUD |
| **E4** | B5 + rad relay gameplay | Inoculation loop |
| **E5** | Underground pairing (Stratum 1–3) | Underground architecture P0–P3 |
| **E6** | B7 + Memory Core activities | Aether-9 arc (B4 #8) |
| **E7** | Director activity weighting + biome unlock mask | WorldState persistence |
| **E8** | Io Buggy (6-wheel) deploy/pack + env resistance profile | Vehicle deploy zones on E1/E1b |

**Starts with Io biome pass (B4 #9)** — full surface main map + underground instance/teleport pipeline.

---

## 12. Open questions

1. ~~**World scale**~~ — **Locked:** full-scale surface; instanced underground + select walk-in zones.  
2. ~~**Vehicles**~~ — **Locked:** auto-pack 10–20 m at breach; manual unpack on exit; no skiff.  
3. ~~**Night cycle**~~ — **Locked:** B5 polar thermal day/night shift.  
4. ~~**Story branch**~~ — **Locked:** **B5 Polar → B4 Calderas** (science/rad before heat/mystery escalation).  
5. ~~**Wading**~~ — **Locked:** wade-only, slow, stamina drain; no swim.  
6. ~~**Underground rad**~~ — **Locked:** slow/weak default; **Strong Rad Zones** authored separately.  
7. ~~**Entry pack radius**~~ — **Locked:** **10–20 m** per breach.  
8. ~~**Echo signal density**~~ — **Locked:** **ExperienceDirector** schedules spawns; biomes are weights only.

---

## 13. Promotion path to canon

When approved, fold into **GDD Appendix A2d — Io Biome & Exploration Lock**:

- Seven surface biomes (B1–B7)
- Six-phase expedition loop
- Exploration verb toolkit
- Activity template grammar
- Biome × weather × subsurface pairing table
- Campaign discovery order + gear gates
- **World structure lock:** surface main map; instanced underground + walk-in exceptions; camps; wade-only; B5→B4 story branch; **Echo signals per ExperienceDirector**

---

*Dark Matter Studios — Dark Matter: Genesis — World Design*
