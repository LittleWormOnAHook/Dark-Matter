# Io World Content — Master Phase Map

**Status:** Design investigation — **integration document** for production planning.  
**Authority:** GDD 5.0 Chapter 3 + Appendix A2/A2b; GDD Appendix B4–B5 (disk truth).  
**Companion docs (detail layers):**

| Doc | Scope |
|-----|--------|
| `Io_World_Map_Geography_Plan.md` | **Full-moon map** — top/iso art, elevation (0–1000 m), light/dark + hot/cold axes, breach reservations |
| `Io_Biome_Exploration_Gameplay_Plan.md` | B1–B7 verbs, activities, vehicles, weather matrix, story unlock |
| `Io_Underground_Architecture_Plan.md` | Strata 1–5, pools, tube grammar, underground pressure |
| `Io_Biome_Ecology_Roster.md` | Flora/fauna cards, threat families, pets (core 12 + vanity extras), Void Stitcher |

**Microsoft 365 exports:** `Microsoft365/` — Word (`.docx`) + Excel ticket tracker (`.xlsx`). Regenerate: `python3 Microsoft365/export_to_office365.py`.

**Promotion target:** fold approved phases into GDD **Appendix A2f — Io World Content Phase Map** after review.

**Production package:**
- `Io_World_Content_Executive_Summary.md` — one-page stakeholder rollup  
- `Io_World_Content_Milestone_Tickets.md` — actionable tickets IO-W0-01 through IO-W8-05  

**Disk today (July 2026):**** flat terrain prototype; exposure + partial combat AI + `SurfaceEncounterZone` scaffolding; **no** full biomes, ecology prefabs, or pet migration.

---

## 1. What we are building (content inventory)

### 1.1 Surface — seven biomes (B1–B7)

| ID | Biome | Dominant pressure | Signature flora (anchor) | Signature fauna (anchor) | Machine / graveyard hook |
|----|-------|-------------------|--------------------------|--------------------------|--------------------------|
| B1 | Sulfur Plains | Sulfur | Brimstone Fan, Haze Spore Shelf | Sulfur Hound, Cinder Skitter | Scrapper Drone |
| B2 | Geyser Fields | Sulfur + Volcano | Geyser Pod, Vent Bloom Crust | Vent Crab nest, Plume Moth | Survey Drone |
| B3 | Ash Flats & Ridges | Thermal + Volcano | Ash Filament Mat | Basalt Jackal, Dust Spout, Ash Stalker | Excavator Android |
| B4 | Lava Calderas | Volcano + Heat | Rim Glass Needle Mat | Caldera Mantis, Heat Eel edge | Eruption Sentry Bot |
| B5 | Polar Radiation Flats | Radiation + Cold | Void Kelp | Magnet Wyrm, Rift Stalker, Cold Spire Hound | Smuggler Remnant Android |
| B6 | Basalt Highlands | Mixed (hub) | Cliff Tube Lace Shelf | Tube Jackal, Brood mouth, Glass Hive | Survey Beacon Drone |
| B7 | Precursor Ruin Belt | Radiation + Resonance | Resonance Echo Shelf | Vault Stalker, Still Hunter trace | Corrupted Patrol Android, Rust Garden |

**Overlay:** Expedition Graveyard (wrecks, androids, Rust Gardens) across B1–B6.

**Campaign unlock order:** B6 hub → B1 → B2 → B3 → **B5 Polar** → **B4 Calderas** → B7 (post–Memory Core thread).

### 1.2 Underground — five strata

| Stratum | Depth feel | Anchor flora | Anchor fauna | Surface pairing |
|---------|------------|--------------|--------------|-----------------|
| 1 Upper tubes | 50–200 m | Tube Lace, Ash Choke Filament | Tube Jackal, Cinder Tunnel Skitter | B1, B6, walk-in colony tubes |
| 2 Mid galleries | 200–800 m | Glass Kelp, Thermal Seep Bloom | Glass Hive, Rift Skimmer, Brood warden | B2, B3, B6 |
| 3 Volatile basins | 800 m–2 km | Brine Fan, Chemo Mantle | Basin Mantis, Brine Hound, Lamprey Spire | B4, B5 |
| 4 Geothermal roots | 2 km+ | Silicate Mirror Bloom | Heat Eel, Magma Phase Crawler | B4 collapse sinks |
| 5 Resonance vaults | Pockets | Echo Lichen, Aether Seep Petal | Vault Stalker, Echo Symbiont | B7 |

**Locked rules:** full-scale surface main map; most underground **instanced** (10–20 m breach pack); wade-only pools; no hover-skiff.

### 1.3 Global threats (not biome-exclusive)

| Category | Examples | Data kind |
|----------|----------|-----------|
| **Migratory fauna** | Plume Moth, Rift Stalker, Dust Spout Cluster | Lifeform / hazard |
| **Moon-wide elite** | **Void Stitcher** (stealth seam striker) | Alien elite |
| **Android types** | Patrol, Survey, Scrapper, Sentry, Loader, Symbiosis Test Unit… (10 types) | Android |
| **Humanoid threats** | Stim-sick scrapper, corporate remnant, smuggler enforcer… (7 types) | Humanoid expedition |
| **Small ground machines** | Scrap Mite, Turret Crawler, Repair Tick, Beacon Hopper… (7 types) | Android / machine |
| **Flying fauna** | Ash Glass Wasp, Caldera Heat Kite, Ion Glass Bat… (8 types) | Alien / Lifeform |
| **Machine-coral** | Rust Garden | Wreck nest spawner |

### 1.4 Pets

| Layer | Ship v1.0 count | Acquisition mix |
|-------|-----------------|-----------------|
| **Core pets** | **12** | Exploration majority; few side quests; tame / capture / salvage-repair |
| **Vanity extras** | **4** (seed) | Starter camp stray + exploration; **not** counted in core 12 |
| **DLC / updates** | TBD | More core + vanity over time |

**Starter vanity:** Brimstone Puff wanders into camp post-prologue (food scraps).  
**Placeholders to retire:** Ricky, Probe, Fox Cub.

### 1.5 Systems that gate content (must exist before phases)

| System | Role | Disk status |
|--------|------|-------------|
| Exposure + four pressures + thermal meter | Zone identity | Shipped (partial) |
| WeatherDirector (A2b) | Storm schedule, tremor, embed spawns | Partial |
| ExperienceDirector | Echo signals, danger budget, elite pool | Partial / planned |
| SurfaceEncounterZone + Table | Weighted threats + patrols | Prototype |
| Pet Bay + inventory + tame/repair loops | Pet collection | Not started (legacy `Scripts/Pet/`) |
| Full main map + streaming | B1–B7 geography | Not started |
| Underground instance pipeline | Breach teleport + anchor | Not started |

---

## 2. Unified phase map (master)

Phases are **content + engineering** tracks merged into one ordered map.  
**Prerequisite track (GDD B4)** runs in parallel until Phase **W3**; Io world content **starts in earnest at W3** (after living-world slice + pet fold decision).

```
GDD B4 prereq          Io world content (this document)
─────────────────      ─────────────────────────────────────────────
B4 #0–1 World Engine   W0  Data & authoring foundations
B4 #2–3 Comms + seed   W1  Main map shell + underground pipeline
B4 #4 Living-world     W2  B6 hub + B1/B2 + ecology batch 1 + pet foundation
B4 #5 Colony sim       W3  B3/B4 + threats + Void Stitcher + pets batch 2
B4 #6 Pet fold         W4  B5 polar arc + night cycle + pets batch 3
B4 #7–8 Story/Aether   W5  Underground S1–S3 + pool ecology
B4 #9 Io biome pass    W6  B7 + vaults S4–S5 + core pets complete
                       W7  Director tuning + activity grammar + polish
                       W8  Vehicles + console + DLC pet pipeline hook
```

---

### Phase W0 — Data & authoring foundations

**Goal:** Designers can author biomes, ecology, and encounters without blocking on final art.

| Track | Deliverable | Source doc |
|-------|-------------|------------|
| Biome | `BiomeRegionData` SO: pressure, verbs, weather weights, vehicle tags | Biome plan E0 |
| Ecology | Organism registry IDs aligned to `Io_Biome_Ecology_Roster.md` | Ecology roster |
| Encounters | `SurfaceEncounterTable` templates per biome category (Alien / Lifeform / Android) | GDD A2 |
| Pets | `PetDefinition` SO: core vs vanity, acquisition kind, skill tags | Ecology §4.6 |
| Directors | ExperienceDirector biome weights + elite pool entries (Void Stitcher slot) | Biome plan §2.8 |

**Depends on:** Exposure zones (shipped).  
**Does not ship to players:** content visible in flat test scene only.

---

### Phase W1 — Main map shell + underground pipeline

**Goal:** Geographic truth — colony, mountains, breach flow.

| Track | Deliverable | Source doc |
|-------|-------------|------------|
| Surface | Full-scale main map blockout: Command Center, **B6 Highlands** hub, 200–300 m mountains | Biome plan E1 |
| Underground | Instance pipeline: breach → load → exit teleport; 10–20 m vehicle auto-pack | Biome plan E1b |
| Walk-in | Colony refuge tubes, B6 skylight mouths, shallow B1 seeps (no teleport) | Biome plan §2.5 |
| Subsurface | Stratum 1 greybox tube kit + cave offset volumes | Underground P0 |
| UI | Map fog sector for B6; breach icons | Biome plan §7.3 |

**Depends on:** W0, terrain/streaming plan.  
**Unlocks:** All regional biome authoring.

---

### Phase W2 — B6 hub + B1/B2 + ecology batch 1 + pet foundation

**Goal:** First playable region loop; first pets; first native ecology.

#### Biomes & exploration

| Item | Content |
|------|---------|
| **B6** | Tube breaches, highland vistas, multi-breach choice, tutorial underground grammar |
| **B1** | Sulfur Plains storm lanes, Brimstone harvest, seeps |
| **B2** | Vent crossing timing, geyser surge pairing, Vent Crab nest activity |
| Activities | Recon Scan, Harvest Window, Nest Clear templates (first pass) |

#### Flora & fauna (authoring)

| Biome | Flora POIs | Fauna / hazards | Encounters |
|-------|------------|-----------------|------------|
| B1 | Brimstone Fan, Haze Spore Shelf, Condensate Crust | Cinder Skitter, Sulfur Hound, Storm Scavenger Mite | Pack hound, skitter ambient |
| B2 | Geyser Pod, Vent Bloom Crust, Steam Filament Mat | Vent Crab workers/queen, Plume Moth, Geyser Strider | Nest clear, moth ambient |
| B6 | Tube Lace Shelf, Basalt Needle Mat | Tube Jackal, Cave Scout Moth, Brood mouth (optional) | Jackal pack |

#### Pets

| Track | Deliverable |
|-------|-------------|
| Engineering | Pet Bay terminal; pet inventory; retire Ricky/Fox Cub/Probe |
| **Vanity V1** | **Brimstone Puff** — post-prologue camp stray |
| **Core** | C1 Cinder Skitter Kit, C2 Condensate Snail, C4 Geyser Strider Fledgling |
| Systems | Tame + capture + stabilizer grammar (organic) |

#### Threats

| Item | Notes |
|------|-------|
| Graveyard overlay | Scrapper Drone, Rusted Survey Drone on wreck POIs |
| Flying | Plume Moth ambient (F1) |

**Depends on:** W1, WeatherDirector sulfur + geyser (B4 #4).  
**Biome plan refs:** E2.

---

### Phase W3 — B3/B4 + threat families + Void Stitcher + pets batch 2

**Goal:** Visibility + heat extremity; global dread predator; machine repair pets.

#### Biomes & exploration

| Item | Content |
|------|---------|
| **B3** | Ash gale navigation, ridge recon scan, spout alley, buried wreck salvage |
| **B4** | Rim survey, lens crossing, caldera mantis hunt, collapse dive instance (foot only, heat suit) |
| Story | B4 sites tease Aether-9 crew loss (prep for B4 #8) |

#### Flora & fauna

| Biome | Flora | Fauna | Notes |
|-------|-------|-------|-------|
| B3 | Ash Filament Mat, Bronze Dust Curtain | Basalt Jackal, Ash Stalker, Dust Spout embed | Visibility hunter |
| B4 | Heat Mirror Lichen, Obsidian Spire Lattice | Caldera Mantis, Magma Skitter, Tremor Husk | Heat Kite (F7) on rim |

#### Threat families (first full pass)

| Family | Ship in this phase |
|--------|-------------------|
| Humanoid | Stim-Sick Scrapper, Claim Jumper (Graveyard) |
| Ground machine | Scrap Mite (M1), Turret Crawler (M2) |
| Flying combat | Ash Glass Wasp (F6) in B3 ash embed |
| **Global elite** | **Void Stitcher** — seam ambush; director max 1/expedition |

#### Pets

| ID | Pet | Acquisition |
|----|-----|-------------|
| C3 | Vent Hatchling | B2 egg POI (if not in W2) |
| C5 | Ash Glass Wasp Drone | B3 tame |
| C8 | Brine Rim Snapper | B4 / Stratum 3 edge capture |
| C9 | Field Puck | Salvage + **repair loop** live |
| C10 | Scrap Mite Handler | Salvage + repair |

**Depends on:** W2, thermal/volcano HUD polish.  
**Biome plan refs:** E3.

---

### Phase W4 — B5 polar arc + night cycle + pets batch 3

**Goal:** Radiation/cold teaching biome; polar night; loot-focused pet capstone.

#### Biomes & exploration

| Item | Content |
|------|---------|
| **B5** | Cover-to-cover relay, pulse ride, void kelp grove scan |
| Systems | **Polar night** thermal intensify; rad inoculation craft loop |
| Story | Isotope rush / smuggling lore (before B4 escalation) |

#### Flora & fauna

| Biome | Flora | Fauna |
|-------|-------|-------|
| B5 | Void Kelp, Frost SO₂ Crust Bloom, Rad-Root Filament | Magnet Wyrm, Rift Stalker, Cold Spire Hound, Polar Skimmer |

#### Pets

| ID | Pet | Acquisition |
|----|-----|-------------|
| C6 | Polar Skimmer Pup | B5 capture (night bias) |
| C7 | Tube Lace Grub | B6 / Stratum 1 tame — **best auto-loot** |
| C11 | Beacon Hopper | B6 side quest “Lost Survey” + repair |

#### Vanity extras

| ID | Pet |
|----|-----|
| V2 | Plume Mothling |
| V3 | Ridge Pebble Roller |

**Depends on:** W3, Purification Hub / inoculation loop depth.  
**Biome plan refs:** E4.

---

### Phase W5 — Underground S1–S3 + pool ecology

**Goal:** Subsurface as distinct content layer; wade-only pools; brood dungeons.

| Stratum | Modules | Ecology | Activities |
|---------|---------|---------|------------|
| **S1** | Tube kit, skylight, wreck chambers | Tube Lace, skitters, jackals, Rust Garden | Refuge, first breach tutorial |
| **S2** | Flooded junction, glass grotto | Glass Kelp, Glassfish, Rift Skimmer, Glass Hive | Nest clear, wade routes |
| **S3** | Brimstone basin, brood chamber | Brine Fan, Basin Mantis, Brine Hound, Lamprey Spire | Pool edge combat, brood mother optional |

**Surface pairing live:** B1→S1, B2→S2, B4→S3–4 sink, B5→S2–3 cold traps, B6→hub instances.

**Instance camps:** far-reach rest + stash + scrapper (no Lite Building).

**Depends on:** W2–W4 surface regions, WeatherDirector tremor/flood coupling.  
**Underground refs:** P1–P4. **Biome plan refs:** E5.

---

### Phase W6 — B7 + Stratum 4–5 + core pets complete + story capstone

**Goal:** Precursor endgame region; resonance vaults; remaining core pets.

#### Biomes & exploration

| Item | Content |
|------|---------|
| **B7** | Vault approach locks, android dig, core fragment recovery, silent escort |
| **S4** | Geothermal roots, silicate lenses, Heat Eel | 
| **S5** | Resonance vaults, Echo Lichen puzzles, Aether seeps |

#### Flora & fauna

| Zone | Anchors |
|------|---------|
| B7 | Resonance Echo Shelf, Vault Stalker, Silence Moth, corrupted patrols |
| S5 | Echo Lichen, Echo Symbiont Swarm, Still Hunter trace (myth) |

#### Pets

| ID | Pet | Acquisition |
|----|-----|-------------|
| C12 | Core-Sniffer Pup | B7 salvage + repair |
| V4 | Echo Mote | B7 silent zone vanity find |

**All 12 core + 4 vanity seed** achievable in campaign.

**Depends on:** W5, Aether-9 / Memory Core arc (GDD B4 #8).  
**Biome plan refs:** E6. **Underground refs:** P5.

---

### Phase W7 — Director tuning + activity grammar + ecology polish

**Goal:** World feels authored by directors, not static tables.

| Deliverable | Detail |
|-------------|--------|
| ExperienceDirector | Biome unlock mask; activity template weights; Echo + elite (Stitcher) budgets |
| Encounter polish | Per-biome `SurfaceEncounterTable`; patrol routes; combat zone humanoid cap |
| Weather × ecology | Ash gale embed spouts; tremor brood wake; storm fauna suppress |
| Pet POIs | Authored pet candidate anchors (no random encounter roll) |
| Chronicle / Ops radio | Biome warnings, pet tame hints, Stitcher comms line |

**Depends on:** W2–W6 content exists, WorldState persistence (B4 #3).  
**Biome plan refs:** E7.

---

### Phase W8 — Vehicles + console + live expansion hooks

| Deliverable | Detail |
|-------------|--------|
| Io Buggy (6-wheel) | B3/B6 path tags; env resistance; pack/unpack zones |
| Hovercraft gates | Per-biome vehicle table enforced on main map |
| Console parity | Gamepad pet commands, UI readable pet skills |
| **DLC pipeline** | `PetDefinition` DLC flag; vanity + core extension slots documented in roster |
| Performance pass | Ambient fauna budgets, flyer pools, underground instance unload |

**Depends on:** W1 vehicle zones, W7 tuning.  
**Biome plan refs:** E8.

---

## 3. Phase × content matrix (at-a-glance)

| Phase | Biomes | Strata | Core pets | Vanity | Key elites / globals |
|-------|--------|--------|-----------|--------|----------------------|
| W0 | Data only | — | Schema | Schema | Director stubs |
| W1 | B6 blockout | S1 greybox | — | — | — |
| W2 | B6, B1, B2 | Walk-in + S1 start | C1, C2, C4 | **V1 Puff** | Hounds, Vent Crabs |
| W3 | B3, B4 | S3 edge | C3, C5, C8, C9, C10 | — | Mantis, **Void Stitcher** |
| W4 | B5 | S2–S3 link | C6, C7, C11 | V2, V3 | Magnet Wyrm |
| W5 | — | S1–S3 full | — | — | Basin Mantis, broods |
| W6 | B7 | S4–S5 | C12 | V4 | Vault Stalker, Still Hunter trace |
| W7 | All tune | All tune | POI tune | POI tune | Director budgets |
| W8 | Vehicle tags | — | DLC hook | DLC hook | Polish |

---

## 4. Pet rollout summary (cross-phase)

| Core # | Name | Phase | Method |
|--------|------|-------|--------|
| — | Brimstone Puff (V1) | W2 | Camp stray vanity |
| C1 | Cinder Skitter Kit | W2 | Capture + tame (B1) |
| C2 | Condensate Snail | W2 | Tame (B1) |
| C4 | Geyser Strider Fledgling | W2 | Tame (B2) |
| C3 | Vent Hatchling | W2–W3 | Capture (B2) |
| C5 | Ash Glass Wasp Drone | W3 | Tame (B3) |
| C8 | Brine Rim Snapper | W3 | Capture (B4/S3) |
| C9 | Field Puck | W3 | Salvage + repair |
| C10 | Scrap Mite Handler | W3 | Salvage + repair |
| C6 | Polar Skimmer Pup | W4 | Capture (B5) |
| C7 | Tube Lace Grub | W4 | Tame (B6/S1) |
| C11 | Beacon Hopper | W4 | Quest + repair |
| C12 | Core-Sniffer Pup | W6 | Salvage + repair (B7) |
| V2 | Plume Mothling | W4 | Trust (B2) |
| V3 | Ridge Pebble Roller | W4 | Find (B3/B6) |
| V4 | Echo Mote | W6 | Find (B7) |

---

## 5. Ecology authoring checklist (per biome)

Use when marking a biome **content complete** for a phase:

- [ ] Region boundary + pressure volumes on main map  
- [ ] Dominant weather weights hooked to WeatherDirector  
- [ ] 3–5 flora harvest / hazard POIs placed  
- [ ] 4–7 fauna prefabs or placeholders with encounter table rows  
- [ ] 1 machine/android hook POI (Graveyard or regional)  
- [ ] Flying ambient or combat volume (if applicable)  
- [ ] Pet candidate POI(s) per phase matrix  
- [ ] Breach anchor(s) to correct stratum instance  
- [ ] Activity templates playable (min 2 per biome)  
- [ ] Journal biome tab: pressure profile + known threats  

**Art reference (done):** life contact sheets — Set A [`ArtReference/LifeSheets/`](ArtReference/LifeSheets/) (PBR concept) + Set B [`ArtReference/LifeSheets_RayTraced/`](ArtReference/LifeSheets_RayTraced/) (PC Ultra ray tracing) + [`Io_Biome_Life_Sheet_Manifest.md`](Io_Biome_Life_Sheet_Manifest.md).

---

## 6. Dependencies on GDD B4 roadmap

| GDD B4 # | Must land before Io phase |
|----------|---------------------------|
| 4 Living-world slice | W2 (storms affect B1/B2 verbs) |
| 5 Command Center sim | W4+ (sulfur shelter fiction for base-22) |
| 6 Pet fold | W2 (Pet Bay + inventory; retire placeholders) |
| 8 Aether-9 / Memory Cores | W6 (B7, S5, Core-Sniffer fiction) |
| 9 Io biome pass | **W1–W8 aggregate** (this document) |

---

## 7. DLC & live updates (post W8)

| Content type | Pipeline |
|--------------|----------|
| **Core pets** | New `PetDefinition` rows; optional new acquisition quests; **beyond core 12** |
| **Vanity extras** | Low-risk; emote/VFX heavy; no encounter balance impact |
| **Biome variants** | New activity templates or regional POI packs (no new biome ID required) |
| **Fauna variants** | New encounter table rows + director weights |
| **Elite variants** | Void Stitcher reskins or regional seam types (director pool) |

---

## 8. Promotion path

| This document section | Promote to |
|-----------------------|------------|
| Full phase map W0–W8 | GDD **Appendix A2f** |
| Content inventory §1 | A2e ecology + A2d biome cross-index |
| Pet matrix §4 | A2e §pets + B4 #6 implementation checklist |
| Per-biome checklist §5 | Production wiki / milestone tracking |
| Executive summary | GDD **A2f** front matter / studio wiki |
| Milestone tickets | GitHub Issues / production tracker (import IO-W* IDs) |

**Do not duplicate:** organism card detail stays in `Io_Biome_Ecology_Roster.md`; verb/activity detail stays in `Io_Biome_Exploration_Gameplay_Plan.md`; strata module names stay in `Io_Underground_Architecture_Plan.md`.

---

*Dark Matter Studios — Dark Matter: Genesis — World Design*
