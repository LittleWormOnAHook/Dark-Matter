# Io World Content — Milestone Tickets

**Status:** Production tracking — derived from `Io_World_Content_Phase_Map.md`  
**Executive rollup:** `Io_World_Content_Executive_Summary.md`  
**Ticket ID format:** `IO-W{phase}-{seq}` · **Phase:** W0–W8

Use these tickets in your tracker (GitHub Issues, Linear, etc.) — copy title + acceptance criteria verbatim.

---

## How to use

| Field | Meaning |
|-------|---------|
| **Blocked by** | Ticket IDs or GDD B4 items that must land first |
| **Unlocks** | Tickets that can start after this closes |
| **Refs** | Design docs — not duplicate spec here |

**Definition of Done (global):** Unity console clean; playable in target scene; design doc checklist items checked where applicable.

---

## Phase W0 — Data & authoring foundations

### IO-W0-01 · BiomeRegionData ScriptableObject pipeline

| | |
|--|--|
| **Track** | Engineering + Design |
| **Blocked by** | Exposure zone kinds (shipped) |
| **Unlocks** | IO-W1-01, IO-W2-01 |

**Description:** Create `BiomeRegionData` SO with pressure profile, exploration verb tags, weather weight table, vehicle allow tags (B1–B7). Editor create menu + validation for campaign unlock order field.

**Acceptance criteria:**
- [ ] SO holds: biome ID, display name, dominant pressures, verb list, weather weights, vehicle tags, unlock phase enum
- [ ] Seven assets stubbed (B1–B7) matching biome plan table
- [ ] Journal can read biome ID for fog/tab display (stub OK)
- [ ] No hardcoded Invector names in new types

**Refs:** Biome plan E0, §3; Phase map W0.

---

### IO-W0-02 · Ecology organism registry (IDs + metadata)

| | |
|--|--|
| **Track** | Engineering + Design |
| **Blocked by** | — |
| **Unlocks** | IO-W2-03, IO-W3-04 |

**Description:** Registry mapping organism ID → tier, category (flora/fauna/android/humanoid/machine), biome bias, `SurfaceThreatKind` where applicable. Data-only; prefabs optional.

**Acceptance criteria:**
- [ ] All roster anchor names from `Io_Biome_Ecology_Roster.md` §1.1, §4, §5–6 have stable string IDs
- [ ] Void Stitcher registered as global elite
- [ ] Rust Garden flagged machine-coral (not fauna)
- [ ] Editor CSV or SO import path documented in ticket comment

**Refs:** Ecology roster full doc.

---

### IO-W0-03 · PetDefinition schema (core 12 + vanity + DLC flag)

| | |
|--|--|
| **Track** | Engineering |
| **Blocked by** | — |
| **Unlocks** | IO-W2-05, IO-W3-06 |

**Description:** `PetDefinition` SO: `isVanityExtra`, `isDlc`, acquisition enum (Stray, Tame, Capture, SalvageRepair, Quest), skill tags (MinorDps, Cc, Loot, Utility, Vanity), biome unlock mask.

**Acceptance criteria:**
- [ ] 12 core + 4 vanity rows authored as data (placeholder icons OK)
- [ ] Acquisition enum matches ecology §4.6.2 grammar
- [ ] DLC flag present; no runtime shop coupling

**Refs:** Ecology roster §4.6.

---

### IO-W0-04 · Encounter table templates per biome category

| | |
|--|--|
| **Track** | Engineering |
| **Blocked by** | IO-W0-02 |
| **Unlocks** | IO-W2-04, IO-W7-02 |

**Description:** `SurfaceEncounterTable` template assets for Alien / Lifeform / Android per biome family; document combat zone humanoid cap default.

**Acceptance criteria:**
- [ ] At least one template per threat category
- [ ] `SurfaceEncounterZone` can pick by anchor `PreferredThreatKind`
- [ ] Weight fields documented for ExperienceDirector handoff (IO-W7-01)

**Refs:** GDD A2 surface encounters; Phase map W0.

---

### IO-W0-05 · ExperienceDirector stubs (biome weights + elite pool)

| | |
|--|--|
| **Track** | Systems |
| **Blocked by** | GDD B4 #1 World Engine spine |
| **Unlocks** | IO-W7-01 |

**Description:** Read-only stubs: biome weight snapshot, elite pool entry for Void Stitcher (max 1/expedition), Echo cap placeholders.

**Acceptance criteria:**
- [ ] Director compiles; no runtime spawn until IO-W7-01
- [ ] Void Stitcher slot documented with seam-volume trigger enum stub
- [ ] Unit test or debug menu prints weight table from B6→B7 order

**Refs:** Biome plan §2.8; Ecology §3.5.

---

## Phase W1 — Main map shell + underground pipeline

### IO-W1-01 · Full-scale main map blockout (colony + B6 hub + mountains)

| | |
|--|--|
| **Track** | World art + Engineering |
| **Blocked by** | IO-W0-01, terrain/streaming plan |
| **Unlocks** | IO-W2-01, IO-W2-02 |

**Description:** Greybox full main map: Command Center anchor, B6 Basalt Highlands region, 200–300 m mountain zones, road/path tags for future vehicles.

**Acceptance criteria:**
- [ ] Player spawns at colony; can foot-travel into B6 hub zone
- [ ] Region boundaries align with `BiomeRegionData` B6 asset
- [ ] Exposure volumes placed for B6 mixed pressures (stub values OK)
- [ ] Map fog: colony + B6 sector reveal test hook

**Refs:** Biome plan E1; Phase map W1.

---

### IO-W1-02 · Underground instance pipeline (breach teleport + anchor)

| | |
|--|--|
| **Track** | Engineering |
| **Blocked by** | IO-W1-01 |
| **Unlocks** | IO-W2-06, IO-W5-01 |

**Description:** Breach interact → load underground scene → exit returns to surface anchor position. Vehicle auto-pack volume 10–20 m at entry; manual unpack on exit.

**Acceptance criteria:**
- [ ] Enter/exit prompt works; trio follows through teleport
- [ ] Surface anchor stored per breach ID
- [ ] Hovercraft auto-packs in entry zone; foot-only inside instance
- [ ] One test Stratum 1 greybox scene wired

**Refs:** Biome plan E1b, §2.5; Underground P0.

---

### IO-W1-03 · Walk-in shallow tubes (no teleport)

| | |
|--|--|
| **Track** | World art |
| **Blocked by** | IO-W1-01 |
| **Unlocks** | IO-W2-06 |

**Description:** Colony refuge tubes + one B6 skylight mouth as on-map geometry (additive scene OK); no loading screen.

**Acceptance criteria:**
- [ ] Foot transition colony ↔ refuge tube without teleport
- [ ] O₂ relief volume in refuge (design targets from underground plan)
- [ ] Documented list of walk-in exceptions matches biome plan §2.5

**Refs:** Biome plan §2.5; Underground plan §0.

---

### IO-W1-04 · Stratum 1 greybox module kit

| | |
|--|--|
| **Track** | World art |
| **Blocked by** | IO-W1-02 |
| **Unlocks** | IO-W5-01 |

**Description:** Minimal tube modules: straight, curve, T, skylight, collapse — modular greybox only.

**Acceptance criteria:**
- [ ] At least 5 module prefabs snap together
- [ ] Cave offset volume component reduces surface weather (blocked flag)
- [ ] One 3–5 module test loop in instance scene

**Refs:** Underground plan §4.1.

---

## Phase W2 — B6 hub + B1/B2 + ecology batch 1 + pet foundation

### IO-W2-01 · Biome regions B6, B1, B2 on main map

| | |
|--|--|
| **Track** | World art + Design |
| **Blocked by** | IO-W1-01, GDD B4 #4 WeatherDirector (sulfur + geyser) |
| **Unlocks** | IO-W2-02, IO-W2-03, IO-W2-04 |

**Description:** Author B6 hub, B1 Sulfur Plains, B2 Geyser Fields regions with pressure volumes, POI placeholders, breach anchors to S1/S2.

**Acceptance criteria:**
- [ ] All three regions pass ecology checklist §5 (blockout level)
- [ ] B6 multi-breach choice: at least 2 breach POIs with different pressure labels
- [ ] B1 storm lane shelter rocks placed; B2 vent nodes with telegraph audio hooks
- [ ] Campaign unlock: B6→B1→B2 gating via WorldState or quest stub

**Refs:** Phase map W2; Biome plan §4 B1/B2/B6.

---

### IO-W2-02 · Activity templates v1 (Recon, Harvest, Nest Clear)

| | |
|--|--|
| **Track** | Systems + Design |
| **Blocked by** | IO-W2-01 |
| **Unlocks** | IO-W7-03 |

**Description:** Three activity templates drivable by director or manual quest: Recon Scan, Harvest Window, Nest Clear — wired to B1/B2/B6 POIs.

**Acceptance criteria:**
- [ ] Each template completable once in test scene
- [ ] Nest Clear tied to Vent Crab queen POI (B2) or jackal pack (B6)
- [ ] Harvest Window respects sulfur storm abort (WeatherDirector hook)

**Refs:** Biome plan §6.

---

### IO-W2-03 · Ecology content batch 1 (flora + fauna POIs)

| | |
|--|--|
| **Track** | Content |
| **Blocked by** | IO-W0-02, IO-W2-01 |
| **Unlocks** | IO-W3-03 |

**Description:** Place harvest/hazard flora and fauna for B1, B2, B6 per phase map table. Placeholder meshes acceptable; behaviors from encounter registry.

**Acceptance criteria:**
- [ ] B1: Brimstone Fan, Haze Spore Shelf, Cinder Skitter, Sulfur Hound pack
- [ ] B2: Geyser Pod, Vent Crab nest, Plume Moth ambient
- [ ] B6: Tube Lace Shelf, Tube Jackal pack, Cave Scout Moth ambient
- [ ] Graveyard: Scrapper Drone + Survey Drone on one wreck each

**Refs:** Ecology roster B1/B2/B6 sections.

---

### IO-W2-04 · Surface encounters B1/B2/B6

| | |
|--|--|
| **Track** | Engineering + Content |
| **Blocked by** | IO-W0-04, IO-W2-03 |
| **Unlocks** | IO-W7-02 |

**Description:** `SurfaceEncounterZone` + tables for first regions; patrol routes on jackals/hounds.

**Acceptance criteria:**
- [ ] Zones spawn on region enter; humanoid cap respected
- [ ] Alien/Lifeform/Android categories represented per GDD A2
- [ ] Patrol loop + ping-pong on at least one route

**Refs:** GDD A2; Phase map W2 threats.

---

### IO-W2-05 · Pet system foundation (Pet Bay + inventory + retire placeholders)

| | |
|--|--|
| **Track** | Engineering |
| **Blocked by** | IO-W0-03, GDD B4 #6 pet fold |
| **Unlocks** | IO-W2-07, IO-W3-06 |

**Description:** Pet Bay terminal; owned-pet inventory; one active pet on expedition; remove/disable Ricky, Probe, Fox Cub from ship paths; UI tab fold toward Companions/Echoes.

**Acceptance criteria:**
- [ ] Equip/swap pet at colony
- [ ] Active pet follows player; does not join trio combat AI
- [ ] Legacy pet prefabs not referenced in new game flow
- [ ] Save/load persists owned pets list

**Refs:** Ecology §4.6; GDD B4 #6.

---

### IO-W2-06 · Tame + capture grammar (organic pets)

| | |
|--|--|
| **Track** | Systems |
| **Blocked by** | IO-W2-05 |
| **Unlocks** | IO-W2-07, IO-W4-04 |

**Description:** Trust meter tame flow; capture + colony stabilizer flow for skittish species.

**Acceptance criteria:**
- [ ] Tame: bait/interact over 2-step test (Condensate Snail path)
- [ ] Capture: trap item → stabilizer prop → pet inventory unlock
- [ ] Failed capture does not kill organism (design tone)

**Refs:** Ecology §4.6.2.

---

### IO-W2-07 · Pets batch 1 — V1 Puff + C1, C2, C4

| | |
|--|--|
| **Track** | Content + Systems |
| **Blocked by** | IO-W2-05, IO-W2-06 |
| **Unlocks** | — |

**Description:** Ship first pets: post-prologue **Brimstone Puff** camp stray; **Cinder Skitter Kit**, **Condensate Snail**, **Geyser Strider Fledgling** in B1/B2.

**Acceptance criteria:**
- [ ] Prologue beat: puff wanders to scrap pile; food offer unlocks V1
- [ ] C1 capture in B1; C2 tame at seep; C4 tame at B2 vent field
- [ ] Each pet: follow + at least one skill (loot ping, sniff, or vent chirp)
- [ ] Vanity puff has no meaningful DPS

**Refs:** Ecology §4.6.3–4.6.4; Phase map pet table.

---

## Phase W3 — B3/B4 + Void Stitcher + pets batch 2

### IO-W3-01 · Biome regions B3, B4 on main map

| | |
|--|--|
| **Track** | World art + Design |
| **Blocked by** | IO-W2-01, thermal/volcano HUD |
| **Unlocks** | IO-W3-02, IO-W3-03 |

**Description:** B3 Ash Flats, B4 Lava Calderas — foot only B4, heat-tier gate messaging, collapse sink to S3 edge instance.

**Acceptance criteria:**
- [ ] B3 ash gale low-vis zones; ridge recon POI
- [ ] B4 rim heat shadows; lava kill volume; no vehicles in B4
- [ ] Ecology checklist §5 blockout for B3/B4

**Refs:** Biome plan B3/B4.

---

### IO-W3-02 · Void Stitcher elite implementation

| | |
|--|--|
| **Track** | Content + Systems |
| **Blocked by** | IO-W0-05, IO-W3-01 |
| **Unlocks** | IO-W7-01 |

**Description:** Global seam ambush predator: hide shader, 0.5 s glass-stress telegraph, lunge, disengage. ExperienceDirector max 1/expedition.

**Acceptance criteria:**
- [ ] Spawns from seam/shimmer volumes any biome B1–B7 (not B7 silent puzzles)
- [ ] Audio telegraph before damage; Scout sense widens window (if kit live)
- [ ] Ops/Kairos comms line on first encounter stub
- [ ] Does not replace Caldera Mantis apex role in B4

**Refs:** Ecology §3.5.

---

### IO-W3-03 · Ecology batch 2 + threat families (B3/B4)

| | |
|--|--|
| **Track** | Content |
| **Blocked by** | IO-W3-01 |
| **Unlocks** | IO-W7-02 |

**Description:** B3/B4 flora/fauna; humanoid scrappers; scrap mite + turret crawler; Ash Glass Wasp embed.

**Acceptance criteria:**
- [ ] B3: Ash Filament Mat, Basalt Jackal, Ash Stalker, dust spout embed
- [ ] B4: Caldera Mantis elite POI, Magma Skitter, Heat Kite flyer
- [ ] Humanoid: Stim-Sick Scrapper on graveyard POI
- [ ] Machines: M1 mite swarm, M2 turret crawler on wreck

**Refs:** Ecology roster §4, B3/B4.

---

### IO-W3-04 · Machine pet repair loop

| | |
|--|--|
| **Track** | Systems |
| **Blocked by** | IO-W2-05 |
| **Unlocks** | IO-W3-05 |

**Description:** Salvage broken chassis in world → repair queue at Pet Bay → programming choice (Aggressive / Balanced / Loot).

**Acceptance criteria:**
- [ ] Damaged core item in inventory → repair consumes scrap + AC
- [ ] Programming choice alters minor skill bias
- [ ] Repaired pet enters owned inventory

**Refs:** Ecology §4.6.2 machine path.

---

### IO-W3-05 · Pets batch 2 — C3, C5, C8, C9, C10

| | |
|--|--|
| **Track** | Content |
| **Blocked by** | IO-W3-04, IO-W2-06 |
| **Unlocks** | — |

**Description:** Vent Hatchling, Ash Glass Wasp Drone, Brine Rim Snapper, Field Puck, Scrap Mite Handler.

**Acceptance criteria:**
- [ ] C9/C10 require repair loop; organic pets use tame/capture
- [ ] Each has minor DPS or loot skill per roster
- [ ] Auto-loot radius tuned lower than C7 (future)

**Refs:** Phase map §4 pet table.

---

## Phase W4 — B5 polar + night cycle + pets batch 3

### IO-W4-01 · Biome region B5 + polar night thermal

| | |
|--|--|
| **Track** | World art + Systems |
| **Blocked by** | IO-W3-01, Purification Hub / inoculation loop |
| **Unlocks** | IO-W4-02 |

**Description:** B5 Polar Flats; day/night cycle shifts cold pole; foot only; cover-to-cover POIs.

**Acceptance criteria:**
- [ ] Night intensifies cold stress per biome plan §2.6
- [ ] Void Kelp grove scan POI
- [ ] Ecology checklist §5 for B5

**Refs:** Biome plan B5, §2.6.

---

### IO-W4-02 · Ecology B5 + Magnet Wyrm elite

| | |
|--|--|
| **Track** | Content |
| **Blocked by** | IO-W4-01 |
| **Unlocks** | — |

**Description:** B5 flora/fauna; Magnet Wyrm; Rift Stalker; Cold Spire Hound packs.

**Acceptance criteria:**
- [ ] Magnet Wyrm 0–1 per zone director gate
- [ ] Rad pulse + cover sprint gameplay viable
- [ ] Smuggler Remnant Android on cache story POI

**Refs:** Ecology roster B5.

---

### IO-W4-03 · Side quest “Lost Survey” (Beacon Hopper)

| | |
|--|--|
| **Track** | Narrative + Systems |
| **Blocked by** | IO-W3-04 |
| **Unlocks** | IO-W4-04 |

**Description:** B6 quest awarding damaged Beacon Hopper core → repair → C11 pet.

**Acceptance criteria:**
- [ ] Quest completable; rewards repairable chassis
- [ ] C11 breach map ping works after repair
- [ ] Communications Officer dialogue stub

**Refs:** Ecology C11 card.

---

### IO-W4-04 · Pets batch 3 + vanity V2, V3 — C6, C7, C11

| | |
|--|--|
| **Track** | Content |
| **Blocked by** | IO-W4-02, IO-W4-03, IO-W2-06 |
| **Unlocks** | — |

**Description:** Polar Skimmer Pup, Tube Lace Grub (best loot), Plume Mothling, Ridge Pebble Roller.

**Acceptance criteria:**
- [ ] C7 auto-loot radius ≥ all other core pets
- [ ] C6 capture night bias documented in POI
- [ ] V2/V3 cosmetic skills only; counted as vanity extras not core 12

**Refs:** Phase map W4.

---

## Phase W5 — Underground S1–S3 + pool ecology

### IO-W5-01 · Stratum 1–3 instance content

| | |
|--|--|
| **Track** | World art + Content |
| **Blocked by** | IO-W1-02, IO-W1-04, IO-W2–W4 surface regions |
| **Unlocks** | IO-W6-02 |

**Description:** Build S1–S3 instance scenes; surface pairing B1→S1, B2→S2, B4→S3, B5→S2–3, B6 hub.

**Acceptance criteria:**
- [ ] Wade-only pools: slow, stamina drain, no swim
- [ ] S3 Basin Mantis ambush POI; Brine Hound pack at rim
- [ ] Brood chamber optional dungeon with warden patrol
- [ ] Instance camp prop: rest + stash (one far site)

**Refs:** Underground P1–P4; Phase map W5.

---

### IO-W5-02 · Tremor / flood coupling underground

| | |
|--|--|
| **Track** | Systems |
| **Blocked by** | WeatherDirector tremor (B4 #4), IO-W5-01 |
| **Unlocks** | IO-W7-04 |

**Description:** Tremor Swarm doubles brood spawn; geyser back-pressure floods S2–S3 tubes.

**Acceptance criteria:**
- [ ] Tremor event triggers rockfall + brood wake in S2–S3
- [ ] Flood timer opens temporary wade lanes (design doc behavior)

**Refs:** Underground §8; Biome plan weather matrix.

---

## Phase W6 — B7 + S4–S5 + pets complete

### IO-W6-01 · Biome region B7 + precursor vault approach

| | |
|--|--|
| **Track** | World art + Narrative |
| **Blocked by** | IO-W5-01, GDD B4 #8 Kairos / Memory Cores |
| **Unlocks** | IO-W6-02, IO-W6-03 |

**Description:** B7 Ruin Belt surface; silent zones; vault lock POIs; android patrol routes.

**Acceptance criteria:**
- [ ] Silent escort activity playable
- [ ] Corrupted Patrol Android patrol loops
- [ ] Ecology checklist §5 B7

**Refs:** Biome plan B7.

---

### IO-W6-02 · Stratum 4–5 instances + Still Hunter trace

| | |
|--|--|
| **Track** | World art + Content |
| **Blocked by** | IO-W6-01, IO-W5-01 |
| **Unlocks** | — |

**Description:** Geothermal S4 (Heat Eel); Resonance S5 vaults (Echo Lichen puzzles, symbiont swarm); Still Hunter myth ambient.

**Acceptance criteria:**
- [ ] S5 Saturation drift on loud combat in lichen zones
- [ ] Still Hunter trace: flee encounter, codex entry, no farm
- [ ] Aether seep scan POIs for Memory Core thread

**Refs:** Underground P5; Ecology B7/S5.

---

### IO-W6-03 · Pets batch final — C12 + V4 Echo Mote

| | |
|--|--|
| **Track** | Content |
| **Blocked by** | IO-W6-01, IO-W3-04 |
| **Unlocks** | — |

**Description:** Core-Sniffer Pup (B7 repair); Echo Mote vanity (B7 silent zone).

**Acceptance criteria:**
- [ ] All **12 core + 4 vanity** obtainable in campaign without DLC
- [ ] C12 resonance loot ping functional

**Refs:** Phase map §4.

---

## Phase W7 — Director tuning + polish

### IO-W7-01 · ExperienceDirector integration (Echo, elites, activities)

| | |
|--|--|
| **Track** | Systems |
| **Blocked by** | IO-W0-05, IO-W2–W6 content, GDD B4 #3 WorldState |
| **Unlocks** | IO-W8-05 |

**Description:** Live director: biome unlock mask, Echo weights, Void Stitcher pool, activity template weights, pet POI anchors (authored only).

**Acceptance criteria:**
- [ ] No static “always one Echo here” — weights per biome plan §2.8
- [ ] Stitcher max 1/expedition enforced
- [ ] Strain/colony inputs reduce Echo/pet POI density when overwhelmed

**Refs:** Biome plan §2.8; Phase map W7.

---

### IO-W7-02 · Per-biome encounter tables + patrol polish

| | |
|--|--|
| **Track** | Content + Engineering |
| **Blocked by** | IO-W2-04, IO-W3-03, all biome regions |
| **Unlocks** | — |

**Description:** Final `SurfaceEncounterTable` per biome; patrol coverage; flying spawn budgets.

**Acceptance criteria:**
- [ ] Each biome B1–B7 has encounter table asset
- [ ] Flying fauna caps per ecology §4.4
- [ ] Android + fauna never same anchor (spawn rules)

**Refs:** Ecology §4.5.

---

### IO-W7-03 · Activity grammar full set

| | |
|--|--|
| **Track** | Systems + Design |
| **Blocked by** | IO-W2-02, IO-W7-01 |
| **Unlocks** | — |

**Description:** Remaining templates: Echo Rescue, Salvage Run, Survey Sample, Depth Push, Core Recovery, Escort Extract.

**Acceptance criteria:**
- [ ] At least 8 templates defined; 2+ playable per biome category in test
- [ ] Director can weight by colony need (O₂ low → condensate jobs)

**Refs:** Biome plan §6.

---

### IO-W7-04 · Weather × ecology coupling pass

| | |
|--|--|
| **Track** | Systems |
| **Blocked by** | IO-W5-02, WeatherDirector full |
| **Unlocks** | — |

**Description:** Ash gale embed spouts; storm fauna suppress; resonance supercell Stitcher spike.

**Acceptance criteria:**
- [ ] Sulfur storm suppresses surface combat spawns
- [ ] Ash gale embeds dust spouts in B3
- [ ] Documented in debug overlay

**Refs:** Ecology §1.3; Phase map W7.

---

### IO-W7-05 · Ops radio + pet/Stitcher comms lines

| | |
|--|--|
| **Track** | Narrative + Communications |
| **Blocked by** | GDD B4 #2 Communications, IO-W3-02 |
| **Unlocks** | — |

**Description:** Template lines: biome warnings, pet tame hints, Void Stitcher first encounter, polar night warning.

**Acceptance criteria:**
- [ ] At least 5 template strings in comms system
- [ ] Triggered by director/context builder stubs

**Refs:** Biome plan §2.6 comms.

---

## Phase W8 — Vehicles + console + DLC hooks

### IO-W8-01 · Io Buggy (6-wheel) + deploy zones

| | |
|--|--|
| **Track** | Engineering + Content |
| **Blocked by** | IO-W1-01 vehicle tags, IO-W7-02 |
| **Unlocks** | — |

**Description:** Inventory-packed Io Buggy; path-tagged B3/B6; blocked B4/B5; pack at breach.

**Acceptance criteria:**
- [ ] Manual unpack in deploy zone; auto-pack 10–20 m at breach
- [ ] Env resistance profile per biome table

**Refs:** Biome plan E8, vehicle table §2.5.

---

### IO-W8-02 · Hovercraft biome gates enforced

| | |
|--|--|
| **Track** | Engineering |
| **Blocked by** | IO-W8-01 |
| **Unlocks** | — |

**Description:** Enforce per-region hovercraft/buggy allow table on main map.

**Acceptance criteria:**
- [ ] B4/B5 reject vehicle deploy with player-facing message
- [ ] Colony flats allow both types per table

**Refs:** Biome plan §2.5 vehicle table.

---

### IO-W8-03 · Console pet + expedition UX

| | |
|--|--|
| **Track** | UI + Input |
| **Blocked by** | IO-W2-05 |
| **Unlocks** | — |

**Description:** Gamepad pet summon/dismiss, skill activate, readable skill icons on TV safe area.

**Acceptance criteria:**
- [ ] Full pet loop on gamepad without keyboard
- [ ] UI palette per `DarkMatterGenesisUiPalette`

**Refs:** UI palette rule; GDD A9.

---

### IO-W8-04 · Performance pass (ambient fauna + instances)

| | |
|--|--|
| **Track** | Engineering |
| **Blocked by** | IO-W7-02 |
| **Unlocks** | — |

**Description:** Pool ambient skitters/moths; unload underground instances; encounter budget profiling on target hardware.

**Acceptance criteria:**
- [ ] Soft caps from phase map §5 encounter budget documented in profiler notes
- [ ] No leak on breach cycle ×10

**Refs:** Phase map encounter budget.

---

### IO-W8-05 · DLC pet pipeline hook

| | |
|--|--|
| **Track** | Engineering + Design |
| **Blocked by** | IO-W0-03, IO-W7-01 |
| **Unlocks** | Post-ship content |

**Description:** `PetDefinition.isDlc` + optional addressable load; vanity/core extension without schema break.

**Acceptance criteria:**
- [ ] New pet row can ship without code fork
- [ ] Documented in ecology roster §7 DLC table
- [ ] No paywall on core 12 in base game

**Refs:** Phase map §7; Ecology §4.6.

---

## Ticket dependency graph (summary)

```
W0 (data) ──► W1 (map shell) ──► W2 (B6/B1/B2 + pets 1) ──► W3 (B3/B4 + Stitcher)
                                      │                              │
                                      └──────────────► W4 (B5 + pets 3)
                                                              │
W1 ──► W5 (underground S1–S3) ◄──────────────────────────────┘
                    │
W5 + B4#8 ──► W6 (B7 + S4–S5 + pets done)
                    │
All regions ──► W7 (directors + polish) ──► W8 (vehicles + console + DLC)
```

---

## Milestone close checklist (release gate)

- [ ] All tickets IO-W0-01 through IO-W8-05 **closed** or explicitly deferred with GDD note  
- [ ] Executive summary success criteria (6 bullets) met  
- [ ] Ecology roster + phase map promoted or marked **locked** in GDD A2f  
- [ ] Unity console: zero errors on world content test scene  

---

*Dark Matter Studios — Dark Matter: Genesis — Production tracking*
