# Io Underground Architecture — Investigation & Plan

**Status:** Design investigation (July 2026)  
**Authority:** Supports GDD 5.0 Chapter 3 (World of Io), Appendix A2 (pressures/offsets), A2b (weather).  
**Companion docs:** `Io_Biome_Exploration_Gameplay_Plan.md` — surface biome verbs, activities, unlock flow  
**Not yet locked** — review before promoting to GDD Appendix A2c.

---

## 1. Design goal

Io’s surface is the antagonist. The **subsurface is the mystery**.

Underground spaces should deliver:

- **Wonder** — bioluminescent pools, glass cathedrals, precursor vaults.
- **Refuge** — partial offset from surface weather (not immunity).
- **Risk** — new pressures (confined O₂, radon buildup, toxic brines, brood nests).
- **Mastery** — learning which tubes are safe, which pools are harvestable, which lakes are lethal.

The player should feel that going deeper is a **deliberate expedition decision**, not a generic cave reskin.

---

## 2. Investigation — what “water” means on Io

Io has **no stable surface water**. Game canon should not imply Earth-like lakes without explanation.

Subsurface fluids are **volatile chemistry**, not Caribbean swimming holes:

| Fluid type | Real-ish basis | Game read |
|------------|----------------|-----------|
| **SO₂ condensate pools** | Cold traps in shaded tubes / polar-linked pockets | Milky pale pools; cough hazard; harvestable condensate |
| **Brimstone brine** | Sulfur + salts + superheated subsurface melt | Amber viscous “lakes”; corrosive; rich chemistry loot |
| **Silicate melt lenses** | Thin magma films over rock (not swimmable) | Orange mirror surfaces; radiant heat; path hazard |
| **Aether seep** | Fiction — precursor / Resonance-touched volatile | Teal glow; Memory Core adjacency; scan POIs |
| **Condensate rain** | Tube ceiling drip from thermal cycling | Ambient hazard; feeds pool ecology |

**Player-facing term:** use **volatile pools** or **brine basins** in UI/comms. Reserve **“lake”** for large chamber-scale bodies (still not H₂O — e.g. *Brimstone Basin*, *Condensate Mirror*).

---

## 3. Vertical architecture (five strata)

Think **stacked biomes**, not one generic “cave” tag.

```
┌─────────────────────────────────────────────  SURFACE  ─────────────────────────────────────────────┐
│  Breach mouths · collapsed tubes · expedition drill shafts · precursor surface locks                  │
└───────────────────────────────  STRATUM 1 — UPPER LAVA TUBES  ──────────────────────────────────────┘
│  Wide tunnels · skylight shafts · ash choke · refugee camps from failed expeditions                 │
│  Life: skitters, lichen, opportunist scavengers                                                    │
└───────────────────────────────  STRATUM 2 — MID GALLERIES  ─────────────────────────────────────────┘
│  Branching networks · thermal seeps · first volatile pools · glass formations                       │
│  Life: glassfish schools, vent crabs, brood tunnel entrances                                        │
└───────────────────────────────  STRATUM 3 — DEEP VOLATILE BASINS  ────────────────────────────────────┘
│  Chamber lakes · brine falls · gas domes · flooded tube junctions                                   │
│  Life: pool-edge colonies, ambush predators, chemo-mats                                            │
└───────────────────────────────  STRATUM 4 — GEOTHERMAL ROOTS  ────────────────────────────────────────┘
│  Magma proximity · silicate lenses · extreme heat · seismic sensitivity                             │
│  Life: heat-phase organisms; rare harvest; minimal permanent nests                                  │
└───────────────────────────────  STRATUM 5 — RESONANCE VAULTS  ──────────────────────────────────────┘
│  Precursor architecture · Aether seeps · Memory Core adjacency · non-natural geometry             │
│  Life: echo symbionts, corrupted machines, “still hunter” myth traces                              │
└─────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Stratum summary

| Stratum | Depth feel | Primary challenge | Offset from surface |
|---------|------------|-------------------|---------------------|
| 1 Upper tubes | 50–200 m | Ash choke, collapse, low light | Strong vs wind/lightning; weak vs sulfur seep |
| 2 Mid galleries | 200–800 m | Navigation, thermal seeps | Stable thermal; rad accumulates slowly |
| 3 Volatile basins | 800 m–2 km | Toxic brine, drowning in viscous fluid, broods | No sulfur storm; gas pockets |
| 4 Geothermal roots | 2 km+ | Lethal heat, tremor amplification | None without high-tier suit |
| 5 Resonance vaults | Pockets / gates | Story hazards, android patrols | Scripted per vault |

---

## 4. Room grammar (modular underground architecture)

Reuse **semi-low-poly modular kit** — same philosophy as surface camp.

### 4.1 Tube modules (connective)

- `Tube_Straight` / `Tube_Curve` / `Tube_T_Junction` / `Tube_Collapse`
- `Tube_Skylight` — surface breach; weather peek; rope/rappel entry
- `Tube_Squeeze` — Infiltrator Scout advantage; slows trio with heavy gear
- `Tube_Bridge` — over brine channel or silicate lens

### 4.2 Chamber modules (setpiece)

- `Chamber_Pool_Small` — condensate pool + edge harvest nodes
- `Chamber_Basin_Large` — “lake” scale; boat/hover-skiff fantasy (late expeditions)
- `Chamber_Grotto` — biolum ceiling; low combat; scan POIs
- `Chamber_Brood` — nest arena; egg galleries; wave escalation
- `Chamber_Expedition_Wreck` — environmental storytelling prop cluster
- `Chamber_Precursor_Antechamber` — terminal, locked door, Resonance hint

### 4.3 Hazard modules (gameplay verbs)

- `Hazard_Brine_Fall` — corrosive drip zone
- `Hazard_Gas_Dome` — O₂ spike consumption until vented (Architect tool)
- `Hazard_Floor_Crust` — thin shell over pool; break on weight
- `Hazard_Rockfall_Zone` — tremor-triggered (links to A2b Tremor Swarm)

### 4.4 Traversal gates

- **Rappel** — upper breach entry
- **Sealed door** — Science Specialist scan / Architect breach
- **Flooded tube** — requires condensate-rated waders or hover-skiff module
- **Heat lock** — thermal suit tier gate for Stratum 4

---

## 5. Volatile pools & lakes — taxonomy

| Class | Visual | Hazard | Harvest | Gameplay role |
|-------|--------|--------|---------|---------------|
| **Condensate pool** | Pale, still, mist | Low sulfur; slip | Condensate vials | Safe intro underwater-adjacent content |
| **Brimstone brine lake** | Amber, viscous | Corrosive; slow wade | Brine salts, catalysts | Mid-tier chemistry economy |
| **Glass melt lens** | Mirror-orange | Radiant heat | Obsidian shards | Path denial / timing traversal |
| **Aether seep pool** | Teal, pulsing | Saturation drift | Scan samples | Memory Core / Aether-9 arc |
| **Brood basin** | Cloudy; egg rims | Aggro on contact | Rare proteins (if cleared) | Nest setpiece |
| **Flooded junction** | Waist-deep brine | Drowning if stunned | None | Route choice; companion rescue beat |

**Lake vs pool rule (lock candidate):**

- **Pool** = single chamber body; edge interaction; no boat.
- **Lake** = multi-tile basin module; current drift; may require craft or rope lines.

---

## 6. Life in underground zones

Aligned with surface ecology fantasy (chemosynthetic, sulfur-silicon, resonance-fed).  
Underground life is **not** surface wildlife copy-pasted — it is **pressure-adapted**.

### 6.1 Flora (subsurface)

| Organism | Stratum | Pattern | Role |
|----------|---------|---------|------|
| **Tube Lace** | 1–2 | Colony mats on ceilings | O₂ micro-buffer near harvest; marks safe camps |
| **Brine Fan** | 3 | Solo / ring around pools | Filters brine; harvest gel; wilts if pool drained |
| **Glass Kelp** | 2–3 | Nest-like groves in flooded tubes | Cover; blocks line of sight; hides schools |
| **Echo Lichen** | 5 | Precursor symbiont | Resonance audio puzzles; Saturation if loud combat |
| **Chemo Mantle** | 3–4 | Sheet on pool floor | Slip hazard; buff if sampled (Science Specialist) |

### 6.2 Fauna — by social pattern

#### Solo

| Creature | Stratum | Behavior |
|----------|---------|----------|
| **Basin Mantis** | 3 | Ambush from pool surface film; drag under |
| **Vault Stalker** | 5 | Patrols precursor edges; avoids brine |
| **Heat Eel** | 4 | Solo in silicate lenses; burst damage |

#### Pack

| Creature | Stratum | Behavior |
|----------|---------|----------|
| **Tube Jackals** | 1–2 | Scavenger packs; flee deeper if chased |
| **Brine Hounds** | 3 | Hunt at pool rims; alpha + 2 flankers |
| **Rift Skimmers** | 2–3 | 3-pack gliders over flooded junctions |

#### Nest / colony

| Creature | Stratum | Behavior |
|----------|---------|----------|
| **Brood Tunnels** (mega-nest) | 2–3 | Wardens patrol; mother in deepest chamber |
| **Lamprey Spires** | 3 | Colony on pool ceiling; drop on vibration |
| **Glass Hive** (subterranean variant) | 2 | Anchored to Glass Kelp; sonic stagger |
| **Rust Garden** | 1–5 | Machine-coral on expedition wrecks; swarmer spawn |

### 6.3 Pool-specific ecology rules

1. **Edge zone** — 80% of encounters; predators hunt where oxygen meets brine.
2. **Surface film** — Basin Mantis and spores hide under reflective film (tell: ripples without wind).
3. **Depth penalty** — ranged weapons weaker in brine; melee + tools favored.
4. **Light attracts** — biolum prey clusters; player flashlight pulls skimmers.
5. **Tremor wakes broods** — Tremor Swarm (A2b) doubles spawn rate in Stratum 2–3.

---

## 7. Survival & pressure modifiers underground

Extends GDD A2 four pressures — **caves are offsets, not safe rooms**.

| Pressure | Underground modifier |
|----------|----------------------|
| **O₂** | Consumption **reduced** in sealed tubes; **increased** in gas domes / brood chambers |
| **Radiation** | Surface rad **blocked**; **radon buildup** over time in deep galleries (new meter or rad creep) |
| **Thermal** | Stable in Stratum 1–2; **heat pole** spikes near Stratum 4; cold pockets at condensate pools |
| **Sulfur** | Lower than surface; **spikes** near brine falls and geyser back-pressure events |
| **Weather (A2b)** | Surface storms **muted**; **Tremor Swarm amplified**; lava surge can **flood** lower tubes |

### Protection matrix (underground column — design target)

| Zone | O₂ | Rad | Thermal | Sulfur | Surface weather |
|------|----|-----|---------|--------|-----------------|
| Upper tube camp | slow | partial block | stable | partial | blocked |
| Mid gallery | slow | creep | stable | low | blocked |
| Pool edge | normal | creep | variable | medium | blocked |
| Geothermal root | fast drain | high | extreme heat | low | blocked |
| Resonance vault | scripted | scripted | scripted | scripted | blocked |

---

## 8. Underground events (weather-adjacent)

Pair with A2b scheduler — these fire **primarily underground** or **amplify in caves**:

| Event | Effect |
|-------|--------|
| **Tremor Swarm** | Rockfall; brood wake; pool film breaks |
| **Geyser back-pressure** | Floods Stratum 2–3 tubes; temporary lake expansion |
| **Brine rise** | Basins connect; new swim routes; new ambush lanes |
| **Gas belch** | O₂ drain bubble; clear with Architect vent tool |
| **Aether pulse** | Stratum 5 only; Saturation + android patrol |

---

## 9. Gameplay loops

### Expedition loop

1. **Breach** — choose entry (skylight vs drill vs precursor lift).
2. **Route** — map reveals tubes; compass unreliable deep.
3. **Pool interaction** — scan, harvest, or bypass.
4. **Nest decision** — clear brood vs sneak for core resource.
5. **Extract** — tremor or flood timer pressure on exit.

### Colony loop (later)

- **Purification Hub** — brine condensate → clean O₂ supplement (not immunity).
- **Science Labs** — chemo samples from pools unlock inoculations.
- **Lite Building** — tube seals, camp nodes in Stratum 1 only (design lock candidate: **no full base in Stratum 3+**).

### Story hooks

- Mid-game arc (GDD 3.0): *Human–AI symbiosis experiments in lava tubes* → wreck modules in Stratum 2.
- Aether-9 Memory Cores → vault antechambers in Stratum 5.
- Failed expeditions built **inside brood tunnels** — environmental storytelling prop.

---

## 10. Resource & economy sketch

| Resource | Source | Use |
|----------|--------|-----|
| Condensate | Pools Stratum 2–3 | O₂ supplement, coolant |
| Brine salts | Brimstone lakes | Craft, inoculations |
| Glass shards | Melt lenses / Tube Lace | Building fiber, tools |
| Brood proteins | Nest clears | Med-tech, companion heals |
| Aether samples | Seep pools / vaults | Memory Core research |
| Obsidian vein | Stratum 4 edge | High-tier armor |

Economy remains **AC + gathered materials** — pools are not a second currency.

---

## 11. Production phasing (when to build)

| Phase | Deliverable | Depends on |
|-------|-------------|------------|
| **P0** | Stratum 1 tube kit + breach entry + cave offset volumes | Io biome pass (B4 #9) |
| **P1** | Condensate pools + edge harvest + Tube Lace / skitters | Exposure system (shipped) |
| **P2** | Mid galleries + flooded junction + Glass Kelp / glassfish | Navigation / map fog |
| **P3** | Brimstone basins + Brine Hounds + Basin Mantis | Combat AI pack roles |
| **P4** | Brood chambers + tremor flood coupling | WeatherDirector (A2b) |
| **P5** | Resonance vaults + Aether seeps | Aether-9 / Memory Cores (B4 #8) |

**Start underground architecture after:**

1. **Io biome pass** (B4 #9) — needs surface↔subsurface zone graph  
2. **WeatherDirector** foundation (A2b) — tremor/flood coupling  

Can **greybox Stratum 1** earlier on flat terrain with a single test tube prefab.

---

## 12. Open questions (need design call)

1. **Swimming / wading** — full swim in brine or wade-only with stamina drain?
2. **Radon creep** — separate meter or slow rad pressure increase?
3. **Hover-skiff on lakes** — expedition vehicle fantasy or skip for scope?
4. **Companion depth limits** — do all trio members enter deep strata or hold at camp?
5. **Base building underground** — Stratum 1 camps only, or allow sealed modules at pool rims?

---

## 13. Promotion path to canon

When approved, fold into GDD 5.0 as **Appendix A2c — Io Subsurface Lock**:

- Five strata names
- Volatile pool/lake taxonomy (no surface H₂O)
- Underground pressure modifiers
- Pool ecology social patterns (solo / pack / nest)
- Production phasing table

---

*Dark Matter Studios — Dark Matter: Genesis — World Design*
