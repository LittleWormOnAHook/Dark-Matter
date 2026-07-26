# Io Biome Ecology Roster

**Status:** Design investigation — promote later to **GDD Appendix A2e** after review.  
**Authority:** GDD 5.0 Chapter 3 (Living Planet), Appendix A2 (surface threats: alien life + AI androids).  
**Companion docs:**
- `Io_Biome_Exploration_Gameplay_Plan.md` — biome verbs, activities, unlock flow  
- `Io_Underground_Architecture_Plan.md` — strata, pools, underground pressure modifiers  

**Not yet locked** — prototype remains flat terrain; Unity spawn/prefab work deferred to GDD B4 #9 (Io biome pass).

---

## 1. Ecology pillars

Io life is **not Earth biology**. All organisms in this roster obey three locked metabolic fantasies:

| Pillar | Read | Design rule |
|--------|------|-------------|
| **Chemosynthetic** | Energy from sulfur compounds, vent minerals, brine chemistry | No photosynthesis, no grass/trees/deer analogues |
| **Sulfur-silicon** | Chitin-silicate shells, glass filaments, brimstone salts in tissue | Visual language: matte mineral, refractive edges, amber/yellow/teal palettes |
| **Resonance-fed** | Aether / Memory Core leakage powers symbionts and rare elites | Saturation-sensitive; loud combat or scans can wake resonance fauna |

### 1.1 Taxonomy (design categories)

| Category | Examples | Combat? | Encounter kind |
|----------|----------|---------|----------------|
| **Flora** | Brimstone Fan, Void Kelp, Tube Lace | Usually no | Ambient / harvest / hazard |
| **Fauna** | Sulfur Hound, Vent Crab, Basin Mantis | Yes | Alien or Lifeform (`SurfaceThreatKind`) |
| **Machine-coral** | Rust Garden | Yes (swarmers) | Lifeform visually; machine origin |
| **Android** | Patrol frame, survey drone, sentry bot | Yes | `SurfaceThreatKind.Android` — machine origin, **not fauna** |
| **Humanoid threat** | Stim-sick scrapper, corporate remnant, smuggler enforcer | Yes | `SurfaceThreatKind.Android` or dedicated humanoid archetype in data — **expedition humans**, not native life |
| **Ground machine** | Scrap mite, turret crawler, repair tick | Yes | `SurfaceThreatKind.Android`; small non-humanoid AI |
| **Flying fauna** | Plume Moth, Ash Glass Wasp, Heat Kite | Yes / ambient | Alien or Lifeform; see §4.5 |

### 1.2 Social patterns

| Pattern | AI sketch | Player verb bias |
|---------|-----------|------------------|
| **Solo** | Ambush, patrol, apex hunt | Clear, time, route |
| **Pack** | 3–6 units, flanker logic | Clear, route |
| **Nest / colony** | Warden patrol + core chamber | Clear, breach, extract |
| **Ambient** | Non-aggressive unless provoked | Sample, scan, route |

### 1.3 Pressure coupling (weather wakes life)

| Pressure / weather | Typical ecological response |
|--------------------|------------------------------|
| **Sulfur storm** | Skitters burrow; hounds howl then vanish; flora folds spores |
| **Ash gale** | Jackals hunt by vibration; dust spouts embed |
| **Geyser surge** | Vent crabs surface; pods pressurize (timed harvest window) |
| **Tremor swarm** | Brood tunnels wake; pool film breaks; rockfall flushes jackals |
| **Rad pulse** | Polar fauna shelter; Rift Stalkers spike aggression post-pulse |
| **Resonance supercell** | Echo symbionts glow; android patrols erratic; Still Hunter myth traces; **Void Stitcher** seam-spawn rate spikes |

---

## 2. Shared organism card template

Every entry below uses these fields:

| Field | Purpose |
|-------|---------|
| **Name** | Player-facing / comms label |
| **Tier** | `ambient` · `common` · `elite` |
| **Biome / stratum** | B1–B7 and/or Stratum 1–5 |
| **Visual** | 1–2 sentences, art direction |
| **Habitat / density** | Where it clusters; soft spawn density note |
| **Behavior / verb** | route · scan · clear · sample · time · shelter |
| **Pressure interaction** | How sulfur / heat / rad / storms change it |
| **Harvest / loot** | Material or research sample |
| **Trio synergy** | Class bias (not a hard gate) |
| **Prototype note** | `deferred` · `legacy creature AI` · `humanoid/android AI` · `hazard volume` |

---

## 3. Cross-biome & migratory species

Shared tables — home biome sets spawn **weight bias**, not exclusivity.

### 3.1 Plume Moth

| Field | Detail |
|-------|--------|
| **Tier** | ambient → common (when swarming) |
| **Home bias** | B2 Geyser Fields; migrates B1, B3, B4 rims |
| **Visual** | Iridescent sulfur-silicate wings; steam-lit silhouette; reads as living geyser ash |
| **Habitat** | Steam columns, vent thermals, caldera updrafts |
| **Behavior** | **Route / time** — swarm crosses player lane on vent cooldown; harmless unless startled (dust blind) |
| **Pressure** | Geyser surge triples swarm density; sulfur storm grounds them |
| **Harvest** | Wing scale dust → catalyst reagent (Science) |
| **Trio** | Scout predicts swarm lane; Specialist samples scales in flight window |
| **Prototype** | deferred — ambient VFX + optional debuff volume |

### 3.2 Rift Stalker

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Home bias** | B5 Polar; appears B6 ridges, B7 ruins, Stratum 2–3 |
| **Visual** | Low, rad-shimmer hide; three-jointed legs; faint aurora stripe along spine |
| **Habitat** | Cover-to-cover sprint lanes, ruin shadows, flooded junction edges |
| **Behavior** | **Clear / route** — ambush from cover after rad pulse; prefers wounded targets |
| **Pressure** | Jovian rad pulse + polar night = pack cohesion; day = solo stalk |
| **Harvest** | Rad-shimmer membrane → inoculation precursor |
| **Trio** | Specialist inoculation extends safe window; Tactician holds lane during pulse |
| **Prototype** | deferred — `legacy creature AI`, ambush preset |

### 3.3 Dust Spout Cluster

| Field | Detail |
|-------|--------|
| **Tier** | common (hazard) |
| **Home bias** | B3 Ash Flats; embedded in B1/B3 ash gales |
| **Visual** | Wandering bronze ash funnel; mineral debris orbit; not a single creature — colony superorganism |
| **Habitat** | Open flats, ridge saddles; 2–4 per zone during ash gale |
| **Behavior** | **Route / time** — dodge wandering volumes; brief lift + chip damage |
| **Pressure** | Ash gale spawns embed; tremor fixes spout in place (easier to route) |
| **Harvest** | Core filament → ash ceramic binder (after collapse) |
| **Trio** | Scout calls spout paths; Architect deployable anchor for companions |
| **Prototype** | deferred — hazard volume + director spawn |

### 3.4 Rust Garden

| Field | Detail |
|-------|--------|
| **Tier** | common → elite (mature garden) |
| **Home bias** | Expedition Graveyard overlay; B7 android digs; Stratum 1–5 wrecks |
| **Visual** | Machine-coral: oxidized plating sprouting silicate “buds”; teal leakage at fractures |
| **Habitat** | Wreck hulls, abandoned camps, corrupted charging pads |
| **Behavior** | **Clear / scan** — melee hits spawn swarmer drones; quiet scan avoids wake |
| **Pressure** | Ion lightning charges garden (extra swarm burst); resonance supercell merges gardens |
| **Harvest** | Scrap alloy, precursor wire, swarmer core (Tactician clear) |
| **Trio** | Infiltrator silent approach; Tactician burst clear before swarm |
| **Prototype** | deferred — nest spawner on wreck prop; **not fauna** origin |

### 3.5 Void Stitcher *(moon-wide — deadly stealth striker)*

> **Design intent:** One creepy organism the player learns to fear on **any** biome. Not the Still Hunter (myth flee encounter) — the Stitcher **fights**, **kills**, and **vanishes**. Colony Ops / Aether-9 comms: *"Do not trust the seams."*

| Field | Detail |
|-------|--------|
| **Tier** | elite (global rare) |
| **Biome / stratum** | **Any** B1–B7 surface; Stratum 1–4 underground. Never inside B7 silent puzzle corridors or Stratum 5 vault logic rooms |
| **Visual** | Wrong geometry: too many joints folded flat against rock, glass-silicate skin matching local mineral — reads as terrain until it moves. In motion: unfolds upward like a torn seam; no face, only a split vent maw and needle limbs |
| **Habitat** | Heat shimmer, mineral film, crack shadows, wade-pool edges, ash lee — anywhere a **seam** exists. Director max **1 active** per expedition |
| **Behavior** | **Stealth / route / clear** — hidden until trigger: player isolation, companion down, low health, or standing on reflective crust >2 s. **0.5 s** glass-stress audio telegraph → **fast lunge** (high damage, brief stagger). Disengages into seam if blocked or trio focuses fire |
| **Pressure** | Sulfur haze and heat shimmer **improve** camouflage; tremor **forces** one surface reveal per swarm; resonance supercell doubles spawn weight |
| **Harvest** | Seam needle → unique armor mod / Aether-9 codex entry (first kill only per save bias) |
| **Trio** | Scout sense widens telegraph window; Tactician body-block lunge; Med Tech revive target after strike — **never** send one companion alone on wounded extract |
| **Prototype** | deferred — `legacy creature AI`, ambush preset + custom seam-hide shader; **not** humanoid/android |

**Food-web position (global):** apex seam predator — no consistent prey chain; opportunist on wounded fauna and distracted expeditions. Scavengers avoid stitched ground (environmental tell: no mites on seam rocks).

---

## 4. Machine, humanoid & flying threat families

Expedition threats split into **three machine/human buckets** plus **flying fauna**. All use `EnemyArchetype.HumanoidInvector` or a future **small-ground-machine** archetype in data — design labels here are player-facing.

### 4.1 Android types (machine frames)

Humanoid or semi-humanoid **corporate / military / precursor** chassis. Always machine origin — salvage reads as tech, not biology.

| ID | Name | Tier | Home bias | Role | Behavior sketch |
|----|------|------|-----------|------|-----------------|
| A1 | **Corrupted Patrol Android** | common | B7, Stratum 5 | Ruin guard | Silent patrol loop; LOS aggro; teal corruption VFX |
| A2 | **Rusted Survey Drone** | common | B2, Graveyard | Vent mapper | Orbits POI; scan hijack aggro; ranged chip damage |
| A3 | **Graveyard Scrapper Drone** | common | B1, overlay | Camp salvage | Three-arm grab; aggro on crate touch |
| A4 | **Eruption Sentry Bot** | common | B4 story POI | Heat-hardened turret torso | Burst fire; half-buried spawn |
| A5 | **Salvage Excavator Android** | common | B3 wrecks | Tracked digger | Slow; high HP; exposes loot cache when stopped |
| A6 | **Smuggler Remnant Android** | common | B5 caches | Stripped humanoid frame | Melee rush; drops black-market chip |
| A7 | **Loader Automaton** | common | B2 rigs, colony ruins | Industrial hauler | Swings cargo arm; blocks narrow lanes |
| A8 | **Symbiosis Test Unit** | elite | Stratum 2, B6 | Human-AI experiment wreck | Hybrid frame; alternates melee and pulse stagger |
| A9 | **Comms Relay Walker** | common | B6 breaches, B5 relays | Tall antenna chassis | Calls **+1 scrap mite swarm** on alert; priority destroy |
| A10 | **Mine Sweeper Beetle-bot** | common | Graveyard, B1–B3 roads | Medium treaded sweeper | Explodes mine props; chain reaction risk |

**Prototype note (all androids):** `humanoid/android AI` or simple turret AI for A4; deferred until B4 #9.

#### A1 Corrupted Patrol Android — reference card

| Field | Detail |
|-------|--------|
| **Visual** | Standard expedition security shell; precursor teal veins; jaw plate missing |
| **Verb** | **Clear / route** — patrol ping-pong; weak to resonance puzzle traps in B7 |
| **Harvest** | Patrol chip, precursor wire |
| **Trio** | Infiltrator bypass; Tactician clear |

### 4.2 Humanoid expedition threats (organic)

**Living humans** — deserters, stim addicts, corporate remnants, smugglers. Distinct from androids in comms and loot (tags, journals, AC bounties). Use humanoid Invector rig with **non-corruption** materials.

| ID | Name | Tier | Home bias | Role | Behavior sketch |
|----|------|------|-----------|------|-----------------|
| H1 | **Stim-Sick Scrapper** | common | Graveyard overlay, B1 | Loot thief | Erratic wander; flails melee; low accuracy |
| H2 | **Corporate Security Remnant** | common | B2, B4 rigs | Armored guard | Guard preset; short leash; prefers ranged |
| H3 | **Smuggler Enforcer** | common | B5 | Black-market muscle | Pack of 2; flank on rad pulse |
| H4 | **Tunnel Deserter** | common | Stratum 1–2, B6 | Broken expedition survivor | Flashlight lure; flees to jackal pack |
| H5 | **Symbiosis Subject** | elite | Stratum 2 story | Partial neural lace | Phase dash; screams attract brood wake |
| H6 | **Claim Jumper** | common | B3, B6 | Rival prospector | Snipes resource nodes; steals harvest |
| H7 | **Isotope Rush Prospector** | common | B5 early story | Rad-suited digger | Desperate; throws rad flares |

**Design rule:** humanoid threats **never** spawn from native nest anchors — only wreck, camp, and story POI anchors.

#### H5 Symbiosis Subject — reference card

| Field | Detail |
|-------|--------|
| **Visual** | Human silhouette; visible lace ports; one arm chrome, one flesh |
| **Verb** | **Clear** — mid-game lava-tube arc; noise pulls **Brood Tunnel** wardens |
| **Harvest** | Lace fragment → Science Labs research |
| **Trio** | Specialist silence buff; Tactician burst before scream phase |

### 4.3 Small ground machines (non-humanoid AI)

Low profile tread, hop, or crawl — **not** full humanoid rig. Future `EnemyArchetype` extension or scaled drone prefab.

| ID | Name | Tier | Size read | Home bias | Behavior sketch |
|----|------|------|-----------|-----------|-----------------|
| M1 | **Scrap Mite** | ambient → common | Palm | Rust Garden, any wreck | Swarm on alert; chip damage; flee from flame |
| M2 | **Turret Crawler** | common | Dog-sized | B4, B7 perimeter | Deploys mini turret mode when stationary |
| M3 | **Repair Tick** | ambient | Fist | Wreck hulls | Repairs Rust Garden; kill to slow swarm |
| M4 | **Beacon Hopper** | ambient | Knee | B6 breaches | Harmless; maps breach; ion lightning magnet |
| M5 | **Core-Sniffer Rover** | common | Dog-sized | B7 approach | Follows Memory Core signal; explodes on proximity |
| M6 | **Vent Capper Bot** | common | Small box | B2 | Seals vent for corporate map; blocks gas harvest |
| M7 | **Mag-Clamp Drone** | common | Dinner-plate | B5 ore fields | Latches metal gear; drag slow debuff |

**Prototype note:** simple FSM + NavMesh or steering; pool-friendly swarm for M1.

#### M2 Turret Crawler — reference card

| Field | Detail |
|-------|--------|
| **Visual** | Low tread platform; gun pops from carapace hatch |
| **Verb** | **Clear / time** — vulnerable during tread move; armored in turret mode |
| **Harvest** | Crawler cell, barrel scrap |
| **Trio** | Architect smoke deploy blocks LOS |

### 4.4 Flying fauna

Native **alien flyers** — not drones. Consolidates migratory entries from §3 and biome sections.

| ID | Name | Tier | Flight style | Home bias | Combat? |
|----|------|------|--------------|-----------|---------|
| F1 | **Plume Moth** | ambient | Thermal soar | B2; migrates B1/B3/B4 | No — debuff if swarmed |
| F2 | **Ridge Carrion Skimmer** | ambient | Low glide | B3 | No — POI tell |
| F3 | **Polar Skimmer** | ambient | Rad-shimmer glide | B5 | No — pulse tell |
| F4 | **Cave Scout Moth** | ambient | Tube dive | B6 breaches | No — route guide |
| F5 | **Rift Skimmer** | common | 3-pack junction glide | Stratum 2–3 | Yes — light chip |
| F6 | **Ash Glass Wasp** | common | Aggressive dart | B1, B3 ash weather | Yes — swarm 4–6 |
| F7 | **Caldera Heat Kite** | common | Heat-column rider | B4 rim | Yes — dive burn |
| F8 | **Ion Glass Bat** | ambient → common | Storm rider | B5, B6 during ion lightning | Yes during storm embed |

#### F6 Ash Glass Wasp — reference card

| Field | Detail |
|-------|--------|
| **Visual** | Angular glass wings; amber abdomen; buzz like cracked crystal |
| **Habitat** | Ash gale embed; 4–6 per swarm |
| **Behavior** | **Clear / route** — marks player who disturbs ash mats; short chase |
| **Pressure** | Ash gale spawns embed; calm = single scouts only |
| **Harvest** | Glass stinger → filter abrasive |
| **Trio** | Tactician AoE clear; Scout marks nest hole |

#### F7 Caldera Heat Kite — reference card

| Field | Detail |
|-------|--------|
| **Visual** | Kite-shaped thermal membrane; black skeleton; glows when diving |
| **Habitat** | 1–2 circling each caldera rim overlook |
| **Behavior** | **Time / clear** — dive attack during eruption window; otherwise ambient |
| **Harvest** | Kite membrane → heat cell insulator |
| **Trio** | Specialist thermal read predicts dive |

#### F8 Ion Glass Bat — reference card

| Field | Detail |
|-------|--------|
| **Visual** | Flock of small silica bats; refract lightning strikes |
| **Habitat** | Ion lightning storm only; 8–12 flock |
| **Behavior** | **Route** — flying near metal gear increases strike risk; bats harmless unless startled |
| **Harvest** | Bat silica → lightning rod craft |
| **Trio** | Drop metal weapons during storm crossing |

**Flying spawn budget:** max **2** ambient flocks OR **1** aggressive swarm per zone (stacks with §5 encounter table).

### 4.5 Threat family spawn rules (director)

| Family | Anchor types | Mix rule |
|--------|--------------|----------|
| Android | Wreck, rig, ruin patrol | Never more than **1 humanoid-frame android** + **1 small machine** in same 200 m anchor |
| Humanoid expedition | Camp, graveyard, story POI | Max **1 pack (2–3)** or **2 solos**; no android at same POI |
| Small ground machine | Wreck, garden, road | M1 mites only in swarms tied to A9 alert or Rust Garden |
| Flying fauna | Open sky volumes | Weather-gated for F6–F8 |
| **Void Stitcher** | Seam / shimmer volumes | **Global** 1 per expedition; competes with elite slot |
| **Expedition pet** | Player loadout (follower) | **Never** director world spawn — see §4.6 |

### 4.6 Expedition pet companion *(design lock — prototype placeholders)*

The legacy **pet / AI follower** loop (`Scripts/Pet/`, Journal Pet tab) is **prototype debt**. Ship target is **one** expedition pet per player — not a fourth combat companion and **not** part of the 25 Echo / trio roster cap.

**Current disk placeholders (not canon):** `Ricky`, `Probe`, `Fox Cub` — Earth-animal and generic sci-fi reads; replace before ship.

**Locked fork (choose one before art lock):**

| Branch | Identity | Combat | Utility read |
|--------|----------|--------|--------------|
| **A — Io-native adorable pet** | Single unique species that could **live on Io**; chemosynthetic / sulfur-silicon cute silhouette | **No meaningful DPS** — distress call, flee, minor debuff assist at most | Fetch ping, sample sniff, camp morale, O₂ twitch-warn |
| **B — Small robotic AI** | Pocket expedition drone; scrapper / survey puck aesthetic | **Minor attacks only** — chip damage, stagger pulse; never rivals trio DPS | Scan relay, loot ping, comms squeak, flashlight bob |

**Shared rules (both branches):**

- One pet slot on expedition loadout; stays at colony during base-22 sim (aggregate, not full agent).
- UI folds into **Companions / Echoes** presentation per GDD — no separate pet progression track or AC shop loop.
- Pet **never** spawns from `SurfaceEncounterZone` / director tables — player-owned follower only.
- Sulfur storm: pet retreats to player bubble / colony safe state (no separate pet death loop).
- Void Stitcher and apex elites can **ignore** pet unless player is isolated (pet is not a decoy tank).

#### Option A — Io-native pet (reference concept: **Brimstone Puff**)

| Field | Detail |
|-------|--------|
| **Tier** | player companion (non-roster) |
| **Visual** | Round sulfur-silicate puff; soft frill; two large dark eyes; rolls when scared; faint yellow biolum when happy |
| **Ecology read** | Scavenger cousin of Cinder Skitter; eats condensate film; too small for hound prey — **adorable because harmless** |
| **Habitat fiction** | B1 seeps, colony tube edges; player-imprinted after rescue/hatch from fan cluster |
| **Behavior** | **Follow / fetch / sniff** — points nose toward harvest nodes and Echo signal bearing; squeaks before sulfur saturation spike |
| **Pressure** | Burrows in sulfur storm (cosmetic inside Architect bubble); shivers in polar night |
| **Combat** | None — hides behind player; optional **distress chirp** pulls aggro 1 s (trio save beat, not tanking) |
| **Trio synergy** | Med Tech calms puff faster; Scout uses sniff ping on map |
| **Prototype** | replace Fox Cub placeholder; `legacy creature AI` follow + fetch |

#### Option B — Small robotic AI (reference concept: **Field Puck**)

| Field | Detail |
|-------|--------|
| **Tier** | player companion (non-roster) |
| **Visual** | Knee-high hover puck; scratched corporate yellow; one wary LED “eye”; tool arm folds flat |
| **Ecology read** | **Machine** — rebuilt from Graveyard scrapper parts; not fauna |
| **Habitat fiction** | Built at colony craft bench; personality from bootleg firmware |
| **Behavior** | **Follow / scan / minor zap** — short-range arc on player command; overheats after 3–4 shots |
| **Pressure** | Ion lightning magnet (stays near player metal); sulfur storm powered down |
| **Combat** | **Minor only** — 2–4 DPS chip; stagger pulse on cooldown; cannot kill elites alone |
| **Trio synergy** | Communications Officer extends scan relay; Architect recharge pad refills faster |
| **Prototype** | replace Probe placeholder; simple drone FSM + ranged ping |

**Promotion:** fold chosen branch into GDD **A2e** §expedition loadout + Appendix B pet migration note when art locks.

---

## 5. Encounter budget sketch

Aligns with ExperienceDirector danger-budget language (GDD A2, biome plan §2.8). **Soft caps per expedition zone** — director may undershoot during high Strain.

| Layer | Soft cap (per 500 m zone) | Notes |
|-------|---------------------------|-------|
| **Ambient flora nodes** | 8–14 harvestable | Higher in B1/B2 resource biomes |
| **Ambient fauna (non-combat)** | 4–8 | Moths, skitters, mites |
| **Common combat packs** | 1–2 packs OR 2–3 solos | Max one pack + one solo elite |
| **Nest / colony** | 0–1 | Only if activity template = Nest Clear |
| **Android / machine** | 0–1 | Graveyard overlay or B7; never mixed with native nest in same anchor |
| **Elite** | 0–1 | Director gate; Caldera Mantis, Magnet Wyrm, Still Hunter trace, **Void Stitcher** (global seam predator) |

**Resonance modifier:** +1 common spawn weight; brood wake doubles nest warden count (underground).  
**Sulfur storm:** surface combat spawns suppressed; flora harvest nodes pause.  
**Echo Storm:** fauna unchanged; Echo signals compete for director budget (separate channel).

---

## 6. Surface biomes (B1–B7)

---

### B1 — Sulfur Plains

#### Ecosystem overview

Open yellow flats with low cover. Chemistry runs **fan → skitter → hound**: Brimstone Fans condense sulfur vapor; Cinder Skitters graze fan edges; Sulfur Hounds cull skitter blooms and scavenge storm-killed carrion. Failed corporate camps feed **Graveyard Scrapper Drones** (machine, not fauna).

**Food web (5 nodes):** Brimstone Fan → condensate film → Cinder Skitter → Sulfur Hound → storm carrion → Scrapper Drone (salvage loop).

#### Flora

##### Brimstone Fan

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B1 |
| **Visual** | Frilled sulfur-silicate plates, waist-high, matte yellow with wet gloss at seams |
| **Habitat** | Dense bands along seeps; 6–10 per 200 m lane |
| **Behavior** | **Sample / time** — harvest fiber before sulfur storm folds frills |
| **Pressure** | Sulfur storm closes frills (harvest blocked); post-storm burst regrowth |
| **Harvest** | Brimstone fan fiber, sulfur salts |
| **Trio** | Architect filter bubble protects harvest window |
| **Prototype** | deferred — interactable harvest node |

##### Haze Spore Shelf

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B1 |
| **Visual** | Horizontal mineral shelves on rock fins; releases pale yellow spore haze when disturbed |
| **Habitat** | Cliff lee of shelter rocks; 3–5 per cluster |
| **Behavior** | **Route / scan** — spore haze reduces vision; scan reveals safe stepping stones |
| **Pressure** | Ash gale mixes spores (double haze); calm wind clears in 30 s |
| **Harvest** | Spore cake → sulfur filter media |
| **Trio** | Specialist scan pierces haze; Scout marks clean path |
| **Prototype** | deferred — debuff volume on disturb |

##### Condensate Crust Mat

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B1 edges, shallow seeps |
| **Visual** | Milky SO₂ condensate skin over mud-crust; cracks like dried brine |
| **Habitat** | Shallow seep pockets; walk-in zones (GDD lock) |
| **Behavior** | **Sample** — vial condensate; slip hazard if rushed |
| **Pressure** | Sulfur storm thickens crust (bonus yield); heat spike evaporates (loss) |
| **Harvest** | Condensate vials → O₂ supplement chemistry |
| **Trio** | Science Specialist bonus yield on sample |
| **Prototype** | deferred |

##### Sulfur Needle Tuft

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B1 |
| **Visual** | Clusters of hollow glass needles, knee-high, faint whistle in wind |
| **Habitat** | Storm-scoured lanes between fans |
| **Behavior** | **Route** — needles chip suit if sprinted; crouch path between tufts |
| **Pressure** | Tremor snaps needles (audio telegraph); storm buries tufts (safer route) |
| **Harvest** | Silicate needles → craft fiber |
| **Trio** | Infiltrator squeeze through dense tuft lanes |
| **Prototype** | deferred — slow zone / micro-damage |

#### Fauna

##### Cinder Skitter

| Field | Detail |
|-------|--------|
| **Tier** | ambient → common |
| **Biome** | B1 |
| **Visual** | Six-legged, flat carapace, ember-orange joint glow; size of a large cat |
| **Habitat** | Fan edges; packs of 4–8 |
| **Behavior** | **Route / clear** — flees unless cornered; bites in pack if fan grazed |
| **Pressure** | Storm burrow; post-storm surface frenzy (director spawn bump) |
| **Harvest** | Chitin flake, skitter gland (catalyst) |
| **Trio** | Tactician holds lane while Scout marks fan harvest |
| **Prototype** | deferred — `legacy creature AI`, wander + flee |

##### Sulfur Hound

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B1 |
| **Visual** | Elongated muzzle, sulfur-yellow hide, pack silhouettes on flat horizon |
| **Habitat** | 1 pack per 400 m (3–5 hounds + optional alpha) |
| **Behavior** | **Clear / route** — flanking chase; howl before sulfur storm (audio warning) |
| **Pressure** | Storm disperses pack; clear skies increase patrol radius |
| **Harvest** | Hound sinew, sulfur sac |
| **Trio** | Tactician draws alpha; Architect bubble for ranged support |
| **Prototype** | deferred — `legacy creature AI`, pack preset |

##### Brimstone Leech

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B1 seeps, B2 edges |
| **Visual** | Segmented, flat, adheres to boots; amber slime trail |
| **Habitat** | Wet fan bases; 2–4 per seep |
| **Behavior** | **Route / sample** — attach on wade; shake via sprint stamina |
| **Pressure** | Sulfur saturation speeds attach; filter gel repels |
| **Harvest** | Leech enzyme → inoculation base |
| **Trio** | Med Tech removes leech from companions quickly (future kit) |
| **Prototype** | deferred — status effect on seep enter |

##### Storm Scavenger Mite

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B1 post-storm |
| **Visual** | Tiny armored mites rolling sulfur pellets; glitter like static |
| **Habitat** | Swarms on fresh carrion and storm debris; 20–40 ambient |
| **Behavior** | **Scan / sample** — indicates recent kill or wreck; harmless |
| **Pressure** | Only active 5–10 min after sulfur storm |
| **Harvest** | Mite pellet → sulfur salt concentrate |
| **Trio** | Specialist scan identifies wreck direction |
| **Prototype** | deferred — ambient particle swarm |

##### Sulfur Flats Stalker (juvenile)

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B1 twilight lanes |
| **Visual** | Smaller, solitary cousin to hound; mottled gray-yellow; low crawl |
| **Habitat** | 1–2 solos per zone; avoids packs |
| **Behavior** | **Clear / time** — ambush at storm lull boundaries |
| **Pressure** | Low visibility during haze spore events = double ambush range |
| **Harvest** | Stalker claw → blade resin |
| **Trio** | Scout sense warns before ambush radius |
| **Prototype** | deferred — ambush preset |

#### Machine hook — Graveyard Scrapper Drone

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B1 Graveyard overlay |
| **Visual** | Corroded corporate salvage drone; three arms; sulfur pitting |
| **Habitat** | 0–1 near failed camp props |
| **Behavior** | **Clear / salvage** — aggressive if crate touched; drops scrap |
| **Pressure** | Ion lightning stuns drone 3 s (exploit window) |
| **Harvest** | Scrap alloy, drone cell |
| **Trio** | Tactician burst while drone stunned |
| **Prototype** | deferred — `humanoid/android AI` or simple turret AI |

> **Full roster:** this section. Signature names inline in `Io_Biome_Exploration_Gameplay_Plan.md` §B1.

---

### B2 — Geyser Fields

#### Ecosystem overview

Rhythmic vent chemistry drives **pod → crab → moth**: Geyser Pods store pressurized gas; Vent Crabs farm pods and defend nests; Plume Moths feed on vent minerals in steam columns. Corporate rigs failed here — **Rusted Survey Drones** still circle mapped vents.

**Food web (5 nodes):** Vent mineral crust → Geyser Pod → Vent Crab worker → Vent Crab queen → Plume Moth scale → condensate rain.

#### Flora

##### Geyser Pod

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B2 |
| **Visual** | Bulbous silicate bladder on vent rim; pulses with vent cycle |
| **Habitat** | 1–3 per active vent node |
| **Behavior** | **Time / sample** — harvest at cooldown; rupture if mistimed (damage) |
| **Pressure** | Geyser surge tightens window; Architect seal creates safe harvest |
| **Harvest** | Pressurized gas pod, vent catalyst |
| **Trio** | Scout audio callout for hiss → blast → cooldown |
| **Prototype** | deferred — timed interact |

##### Vent Bloom Crust

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B2 |
| **Visual** | Rainbow mineral film on rock; blooms only during vent cooldown |
| **Habitat** | Vent shoulders; regenerates each cycle |
| **Behavior** | **Time** — 8–12 s harvest window per vent |
| **Pressure** | Geyser field surge shrinks window by 50% |
| **Harvest** | Vent minerals, ceramic glaze |
| **Trio** | Architect vent seal extends bloom window |
| **Prototype** | deferred |

##### Steam Filament Mat

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B2 steam lanes |
| **Visual** | Hanging glass threads collecting steam; chime in vent blast |
| **Habitat** | Between vents; 5–8 mats per lane |
| **Behavior** | **Sample / route** — harvest filaments; threads snap in blast (cut route) |
| **Pressure** | Surge severs mats (new path); calm collects condensate drip |
| **Harvest** | Steam filament → insulation craft |
| **Trio** | Specialist timed sample between blasts |
| **Prototype** | deferred |

##### Mineral Rainbow Shelf

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B2 |
| **Visual** | Layered mineral striations on vent collars; saturated color bands |
| **Habitat** | Permanent on major vents |
| **Behavior** | **Scan** — reveals vent phase offset for zone |
| **Pressure** | Ash gale dulls colors (scan harder) |
| **Harvest** | Mineral plate → science benchmark |
| **Trio** | Science Specialist unlocks vent map layer on scan |
| **Prototype** | deferred |

#### Fauna

##### Vent Crab Worker

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B2 |
| **Visual** | Wide shell, mineral-encrusted claws, steam venting from carapace joints |
| **Habitat** | 3–6 per nest zone |
| **Behavior** | **Clear / time** — defends pods; retreats during blast |
| **Pressure** | Surge forces surface (aggressive); cooldown = docile |
| **Harvest** | Crab plate, gas sac |
| **Trio** | Tactician clears during cooldown |
| **Prototype** | deferred — nest defender AI |

##### Vent Crab Queen

| Field | Detail |
|-------|--------|
| **Tier** | elite |
| **Biome** | B2 nest heart |
| **Visual** | Buried in vent throat; visible claw crown during surge |
| **Habitat** | 1 per nest |
| **Behavior** | **Clear** — nest clear objective; steam blast AoE |
| **Pressure** | Destroy queen OR Architect seal bypass for stealth gas harvest |
| **Harvest** | Queen catalyst, rare vent core |
| **Trio** | Architect seal + Tactician DPS window |
| **Prototype** | deferred — boss volume + adds |

##### Plume Moth

See §3.1 — signature B2 migrant.

##### Geyser Strider

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B2 |
| **Visual** | Tall stilt-legs; heat-shimmer silhouette; steps only on cool crust |
| **Habitat** | 1–2 solos crossing vent fields |
| **Behavior** | **Route / time** — follow strider path = safe crust; wrong step = breakthrough |
| **Pressure** | Surge panics strider (unpredictable path) |
| **Harvest** | Strider tendon → heat-resistant strap |
| **Trio** | Scout marks strider footprints |
| **Prototype** | deferred — ambient pathfinder prop + optional aggro |

##### Pressure Wisp

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B2 |
| **Visual** | Floating mineral dust motes in steam; coalesces near pods |
| **Habitat** | Ambient around active vents |
| **Behavior** | **Scan** — indicates overpressure vent (bonus risk/reward node) |
| **Pressure** | Surge makes wisps visible (warning) |
| **Harvest** | Wisp condensate (rare) |
| **Trio** | Specialist scan flags overpressure |
| **Prototype** | deferred — VFX tell |

##### Vent Hatchling

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B2 nest outskirts |
| **Visual** | Palm-sized crabs; click carapace; swarm if stepped on |
| **Habitat** | 10–20 near nests |
| **Behavior** | **Route** — avoid crush; noise pulls workers |
| **Pressure** | Surge buries hatchlings (safer route) |
| **Harvest** | none (ethical/sample penalty if mass-killed — design tone) |
| **Trio** | Infiltrator silent route |
| **Prototype** | deferred |

#### Machine hook — Rusted Survey Drone

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B2 corporate wreck sites |
| **Visual** | Half-melted surveyor; still orbiting dead vent map points |
| **Habitat** | 1 near destroyed rig POI |
| **Behavior** | **Clear / scan** — scan hijacks vent data; drone aggro |
| **Harvest** | Survey chip, scrap |
| **Trio** | Specialist hijack from range; Tactician deletes on fail |
| **Prototype** | deferred — `android` threat kind |

> **Full roster:** this section. Signatures: Vent Crabs, Geyser Pods, Plume Moths — `Io_Biome_Exploration_Gameplay_Plan.md` §B2.

---

### B3 — Ash Flats & Ridges

#### Ecosystem overview

Low visibility shapes **mat → jackal → spout**: Ash Filament Mats stabilize dune lee; Basalt Jackals hunt by vibration; Dust Spout Colonies wander bronze flats. Buried beacons attract **Salvage Excavator Androids** still digging for dead crews.

**Food web (5 nodes):** Ash Filament Mat → Beacon Mite → Basalt Jackal → carrion → Dust Spout → ash ceramic cycle.

#### Flora

##### Ash Filament Mat

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B3 |
| **Visual** | Bronze fibrous sheets binding ash dunes; faint static crackle |
| **Habitat** | Dune lee, 8–12 per slope |
| **Behavior** | **Sample / route** — harvest ceramic fiber; hides loose ash |
| **Pressure** | Ash gale exposes mats (easy harvest); calm buries (scan needed) |
| **Harvest** | Ash ceramic fiber |
| **Trio** | Specialist extended scan through ash |
| **Prototype** | deferred |

##### Ridge Ceramist Sheet

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B3 ridges |
| **Visual** | Kiln-like mineral glaze on rock faces; heat-cured patterns |
| **Habitat** | Ridge crests |
| **Behavior** | **Scan** — maps wind direction for spout prediction |
| **Harvest** | Ridge glaze shard |
| **Trio** | Scout vantage unlock |
| **Prototype** | deferred |

##### Bronze Dust Curtain

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B3 |
| **Visual** | Hanging mineral fronds that shed bronze dust when brushed |
| **Habitat** | Ridge cuts, 4–6 clusters |
| **Behavior** | **Route** — brushing triggers dust puff (aim sway debuff) |
| **Pressure** | Ash gale makes curtains continuous debuff zone |
| **Harvest** | Dust frond → optics coating |
| **Trio** | Infiltrator avoids brush |
| **Prototype** | deferred |

#### Fauna

##### Basalt Jackal

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B3 |
| **Visual** | Blocky shoulders, ash-matted hide, glowing vibration pits along jaw |
| **Habitat** | Packs of 3–5 in flats |
| **Behavior** | **Clear / route** — hunts by footfall; crouch-walk reduces aggro |
| **Pressure** | Low vis = wider hunt radius; tremor = disoriented pack |
| **Harvest** | Basalt hide, jaw sensor organ |
| **Trio** | Infiltrator silent path; Tactician engages at ridge choke |
| **Prototype** | deferred — pack AI |

##### Dust Spout Cluster

See §3.3 — signature B3 hazard colony.

##### Ash Stalker

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B3 |
| **Visual** | Flattened predator; ash-coated; nearly invisible until movement |
| **Habitat** | 1–2 solos per ridge maze |
| **Behavior** | **Clear / scan** — visibility-hunter; scan ping reveals heat seam |
| **Pressure** | Ash gale hides stalker entirely (danger up) |
| **Harvest** | Stalker membrane → stealth suit liner |
| **Trio** | Specialist thermal scan; Scout mark |
| **Prototype** | deferred — ambush + invis shader |

##### Ridge Carrion Skimmer

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B3 |
| **Visual** | Gliding silicate ray; low altitude; bronze underside |
| **Habitat** | Circles recent kills; 2–4 ambient |
| **Behavior** | **Route / scan** — indicates wreck or combat; flees |
| **Pressure** | Dust spout lifts skimmers (odd path tell) |
| **Harvest** | Skimmer fin → glide foil (flavor craft) |
| **Trio** | Scout tracks flight line to POI |
| **Prototype** | deferred |

##### Beacon Burrow Mite

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B3 buried beacon sites |
| **Visual** | Burrows into beacon housing; blue spark saliva |
| **Habitat** | 1 colony per buried beacon POI |
| **Behavior** | **Scan / sample** — repair beacon = fight or smoke repellent |
| **Harvest** | Mite solder → comms repair part |
| **Trio** | Communications Officer relay boost on repair |
| **Prototype** | deferred |

##### Ash Gale Embryo Spout

| Field | Detail |
|-------|--------|
| **Tier** | common (embedded) |
| **Biome** | B3 during ash gale |
| **Visual** | Fist-sized spinning ash seed; grows into spout if ignored |
| **Habitat** | 2–4 embedded per ash gale event |
| **Behavior** | **Time / clear** — destroy embryo before mature spout |
| **Pressure** | Only spawns during ash gale embed rule |
| **Harvest** | Embryo core → storm predictor data |
| **Trio** | Tactician priority target callout |
| **Prototype** | deferred — director sub-spawn |

#### Machine hook — Salvage Excavator Android

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B3 wreck overlay |
| **Visual** | Tracked excavator torso; relentless dig animation toward beacon |
| **Habitat** | 1 per buried wreck activity |
| **Behavior** | **Clear / salvage** — exposes loot cache if stopped or hacked |
| **Harvest** | Alloy tread, excavator arm |
| **Trio** | Infiltrator hack; Tactician destroy |
| **Prototype** | deferred — `android` |

> **Full roster:** this section. Signatures: Dust Spout, Basalt Jackals — `Io_Biome_Exploration_Gameplay_Plan.md` §B3.

---

### B4 — Lava Calderas

#### Ecosystem overview

Extreme heat selects **lichen → scavenger → mantis**: Rim glass mats survive heat shadows; Rim Cinder Scavengers pick obsidian flecks; Caldera Mantis apex-hunts at rim. Heat Eels breach from subsurface collapse sinks. **Eruption Sentry Bots** guard corporate death sites.

**Food web (6 nodes):** Rim glass mat → condensate drip → Cinder Scavenger → Magma Skitter → Caldera Mantis → Heat Eel (subsurface edge) → eruption ash feeds Plume Moth.

#### Flora

##### Rim Glass Needle Mat

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B4 rim |
| **Visual** | Black glass needles in heat-shadow cracks; orange reflected glow |
| **Habitat** | Rim shadows only; 4–6 per overlook |
| **Behavior** | **Route / sample** — safe standing zones; needles cut if pushed into sun |
| **Pressure** | Eruption column shifts shadow map (dynamic safe lanes) |
| **Harvest** | Glass needle → obsidian fiber |
| **Trio** | Specialist thermal read for shadow timing |
| **Prototype** | deferred |

##### Obsidian Spire Lattice

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B4 |
| **Visual** | Self-organized obsidian spires; hum in tremor |
| **Habitat** | Cooling crust islands |
| **Behavior** | **Time** — cross when crust cool (thermal meter green) |
| **Pressure** | Lava surge melts lattice paths |
| **Harvest** | Obsidian shard |
| **Trio** | Architect heat shelter on cooldown crossing |
| **Prototype** | deferred |

##### Heat Mirror Lichen

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B4 rim |
| **Visual** | Silver-orange lichen sheets reflecting heat like foil |
| **Habitat** | Rock faces facing lava lakes |
| **Behavior** | **Sample** — harvest thermal gel precursor |
| **Pressure** | Night rim cool = easier sample; day = burn hazard |
| **Harvest** | Thermal gel precursor |
| **Trio** | Science Specialist sample bonus |
| **Prototype** | deferred |

##### Caldera Salt Bloom

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B4 fallout zones |
| **Visual** | White-yellow mineral rosettes after eruption ash |
| **Habitat** | Post-eruption only; 6–10 per event |
| **Behavior** | **Time / sample** — limited window before heat destroys |
| **Harvest** | Caldera salt → heat cell chemistry |
| **Trio** | Scout eruption timer callout |
| **Prototype** | deferred |

#### Fauna

##### Caldera Mantis

| Field | Detail |
|-------|--------|
| **Tier** | elite |
| **Biome** | B4 rim |
| **Visual** | Mantid, obsidian shell, heat-haze camouflage; forearms like glass blades |
| **Habitat** | 0–1 per caldera zone (director gate) |
| **Behavior** | **Clear** — optional hunt; leap attack; drops shell armor material |
| **Pressure** | Eruption column forces mantis to rim (predictable) |
| **Harvest** | Mantis shell plate → armor tier |
| **Trio** | Tactician draw; Architect heat bubble |
| **Prototype** | deferred — elite melee AI |

##### Plume Moth

See §3.1 — caldera updraft migrant.

##### Rim Cinder Scavenger

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B4 rim |
| **Visual** | Small, hunched, metallic-ash fur; picks at glass mats |
| **Habitat** | Pairs along rim trails |
| **Behavior** | **Route** — flees; leads to heat-shadow safe path if followed |
| **Pressure** | Tremor husk phase = scavengers hide |
| **Harvest** | Scavenger ash gland |
| **Trio** | Scout track follow |
| **Prototype** | deferred |

##### Magma Skitter

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B4 crust edges |
| **Visual** | Ember-lit centipede; leaves glass trail |
| **Habitat** | Swarms of 6–10 on cooling crust |
| **Behavior** | **Clear / time** — swarm if crust stomped |
| **Pressure** | Lava surge wipes swarm (safe after surge on new crust) |
| **Harvest** | Skitter glass trail → melt-lens shard refine |
| **Trio** | Tactician AoE clear on crust |
| **Prototype** | deferred |

##### Heat Eel

| Field | Detail |
|-------|--------|
| **Tier** | elite |
| **Biome** | B4 collapse sinks (Stratum 3–4 edge) |
| **Visual** | Serpentine heat shimmer in silicate lens pools; burst leap |
| **Habitat** | 0–1 per collapse dive instance |
| **Behavior** | **Clear / time** — ambush from lens; wade-only zone |
| **Pressure** | Linked to subsurface heat pole |
| **Harvest** | Eel scale → heat routing tool |
| **Trio** | Specialist lens thermal read |
| **Prototype** | deferred — shares Basin Mantis ambush grammar |

##### Tremor Husk

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B4, B6 |
| **Visual** | Petrified carapace animates during tremor; hollow clicking |
| **Habitat** | 2–3 per tremor swarm event |
| **Behavior** | **Clear** — only hostile during tremor swarm |
| **Pressure** | **Tremor swarm only** |
| **Harvest** | Husk plate → building stress dampener |
| **Trio** | Architect brace during swarm |
| **Prototype** | deferred |

#### Machine hook — Eruption Sentry Bot

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B4 Aether-9 crew death site candidates |
| **Visual** | Heat-shielded corporate sentry; half-buried in ash |
| **Habitat** | 1 per story POI |
| **Behavior** | **Clear / scan** — logs story; drops crew tag |
| **Harvest** | Sentry core, corporate tag |
| **Trio** | Tactician + Specialist decode |
| **Prototype** | deferred — `android` |

> **Full roster:** this section. Signatures: Caldera Mantis, Heat Eel, Plume Moths — `Io_Biome_Exploration_Gameplay_Plan.md` §B4.

---

### B5 — Polar Radiation Flats

#### Ecosystem overview

Cold + rad selects **kelp → mite → wyrm/stalker**: Void Kelp groves filter rad precursors; Aurora Mites swarm kelp; Magnet Wyrm solo patrols magnetic ore; Rift Stalker packs hunt between cover. **Smuggler Remnant Androids** guard illegal core caches.

**Food web (6 nodes):** Void Kelp → Aurora Mite → Cold Spire Hound → Rift Stalker → Magnet Wyrm → frost crust bloom → rad pulse reset.

#### Flora

##### Void Kelp

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B5 groves |
| **Visual** | Pale translucent fronds under frost SO₂ crust; faint violet tips |
| **Habitat** | Grove pools; 10–15 fronds per grove |
| **Behavior** | **Sample / scan** — wrong noise triggers resonance echo (GDD setpiece) |
| **Pressure** | Polar night intensifies cold stress near kelp; rad pulse wilts tips |
| **Harvest** | Void kelp → rad-shield gel precursor |
| **Trio** | Specialist silent scan; Infiltrator noise discipline |
| **Prototype** | deferred |

##### Frost SO₂ Crust Bloom

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B5 |
| **Visual** | Cracked white crust with sulfur frost flowers; crunch audio |
| **Habitat** | Open flats between cover |
| **Behavior** | **Route** — crunch alerts predators; crouch crossing |
| **Pressure** | Day = thinner crust (faster crossing); night = brittle + cold spike |
| **Harvest** | Frost bloom → coolant salt |
| **Trio** | Scout silent route |
| **Prototype** | deferred |

##### Rad-Root Filament

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B5 cover rocks |
| **Visual** | Glassy roots gripping boulders; aurora shimmer |
| **Habitat** | Cover-to-cover lanes |
| **Behavior** | **Scan** — marks safe cover during rad pulse |
| **Harvest** | Rad-root fiber → inoculation mesh |
| **Trio** | Specialist pulse prediction HUD |
| **Prototype** | deferred |

##### Polar Glass Filament

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B5 |
| **Visual** | Wind-aligned glass hairs on boulders; sing in Jupiter wind |
| **Habitat** | Cover stones |
| **Behavior** | **Sample** — harvest during lull between pulses |
| **Harvest** | Polar filament → optics |
| **Trio** | Communications Officer signal clarity buff near filaments |
| **Prototype** | deferred |

#### Fauna

##### Magnet Wyrm

| Field | Detail |
|-------|--------|
| **Tier** | elite |
| **Biome** | B5; subsurface magnetic veins |
| **Visual** | Buried serpent; iron filings dance on surface above it |
| **Habitat** | 0–1 per zone near magnetic ore |
| **Behavior** | **Clear / route** — surface tremor trail; metal gear attracts |
| **Pressure** | Ion lightning + wyrm = double metal risk |
| **Harvest** | Wyrm magnet gland → ore compass |
| **Trio** | Drop metal weapons before crossing (tactical choice) |
| **Prototype** | deferred — solo burrow ambush |

##### Rift Stalker

See §3.2 — B5 home bias.

##### Cold Spire Hound

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B5 |
| **Visual** | Tall spine plates; frost breath; pack silhouettes on Jupiter horizon |
| **Habitat** | Pack 3–4 between cover spires |
| **Behavior** | **Clear / route** — cover-to-cover; night = wider patrol |
| **Pressure** | Polar night + cold pole = faster pack |
| **Harvest** | Spire plate → cold suit liner |
| **Trio** | Architect rad baffle; Tactician hold between covers |
| **Prototype** | deferred — pack AI |

##### Polar Skimmer

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B5 |
| **Visual** | Small ray gliding in rad shimmer; harmless |
| **Habitat** | 3–5 per open flat |
| **Behavior** | **Scan** — flight pattern mirrors rad front |
| **Harvest** | Skimmer scale (cosmetic) |
| **Trio** | Specialist teaches pulse timing via skimmer |
| **Prototype** | deferred |

##### Aurora Mite

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B5 kelp groves |
| **Visual** | Swarm glitter mimicking aurora; feeds on kelp film |
| **Habitat** | 30–50 swarm per grove |
| **Behavior** | **Sample** — disturb = noise debuff + stalker aggro risk |
| **Harvest** | Mite film → gel catalyst |
| **Trio** | Infiltrator silent approach |
| **Prototype** | deferred |

##### Frost Rim Leech

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B5 kelp edges |
| **Visual** | Crystalline sucker disks; slow cold drain on attach |
| **Habitat** | 2–3 per grove edge |
| **Behavior** | **Route** — wade-only kelp lanes |
| **Harvest** | Leech ice enzyme |
| **Trio** | Med Tech cleanse (future) |
| **Prototype** | deferred |

#### Machine hook — Smuggler Remnant Android

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B5 smuggler POI |
| **Visual** | Stripped-down humanoid frame; rad-shield tarp; core slot empty |
| **Habitat** | 1 per illegal cache story node |
| **Behavior** | **Clear / scan** — drops smuggling lore + magnetic ore |
| **Harvest** | Black market chip, ore |
| **Trio** | Infiltrator backstab; Specialist decode |
| **Prototype** | deferred — `android` |

> **Full roster:** this section. Signatures: Void Kelp, Magnet Wyrm, Rift Stalkers — `Io_Biome_Exploration_Gameplay_Plan.md` §B5.

---

### B6 — Basalt Highlands

#### Ecosystem overview

Hub biome bridging surface and tubes: **lace → jackal → brood**. Cliff Tube Lace marks safe camps; Tube Jackals scavenge breaches; Brood Tunnel mouths connect to Stratum 2–3 nests; Glass Hive cliff variants sonic-stagger intruders. **Survey Beacon Drone** (abandoned) still pings breaches.

**Food web (5 nodes):** Tube Lace shelf → Cave Scout Moth → Tube Jackal → Brood warden → Glass Hive → carrion → Highland Carrion Picker.

#### Flora

##### Cliff Tube Lace Shelf

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B6 cliffs, Stratum 1 mouths |
| **Visual** | Ceiling lace cascading over breach lips; soft biolum |
| **Habitat** | Every major breach; O₂ micro-buffer |
| **Behavior** | **Shelter / sample** — marks camp-safe zones |
| **Harvest** | Tube lace fiber → camp module |
| **Trio** | Architect camp deploy bonus under lace |
| **Prototype** | deferred |

##### Highland Spore Curtain

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B6 wind scars |
| **Visual** | Vertical spore sheets between pillars; whistle in wind |
| **Habitat** | Wind corridors |
| **Behavior** | **Route** — passing sheds spores (stamina regen debuff) |
| **Harvest** | Spore sheet → filter mesh |
| **Trio** | Scout wind timing |
| **Prototype** | deferred |

##### Wind-Scoured Glass Fan

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B6 |
| **Visual** | Fan-shaped glass colonies; polished by abrasive wind |
| **Habitat** | Plateau edges |
| **Behavior** | **Sample** — sharp; tool harvest only |
| **Harvest** | Glass fan → building stone refine |
| **Trio** | Salvage Engineer tool bonus (class flavor) |
| **Prototype** | deferred |

##### Basalt Needle Mat

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B6 |
| **Visual** | Short basalt spikes with chemo green tips |
| **Habitat** | Mixed pressure zones |
| **Behavior** | **Route** — indicates multi-pressure breach nearby |
| **Harvest** | Basalt needle |
| **Trio** | All classes tutorial gather |
| **Prototype** | deferred |

#### Fauna

##### Tube Jackal

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B6, Stratum 1–2 |
| **Visual** | Lean, long-toed, pale underbelly; eyes adapted to tube dark |
| **Habitat** | Packs 3–5 near breaches |
| **Behavior** | **Clear / route** — flee into tube if pressed |
| **Pressure** | Tremor = jackals surface on highlands |
| **Harvest** | Jackal sinew |
| **Trio** | Tactician blocks tube retreat |
| **Prototype** | deferred — pack flee AI |

##### Brood Tunnel Mouth

| Field | Detail |
|-------|--------|
| **Tier** | elite (nest gate) |
| **Biome** | B6 → Stratum 2–3 |
| **Visual** | Organic arch of fused chitin; pulsing interior |
| **Habitat** | 0–1 optional dungeon entrance per sector |
| **Behavior** | **Clear / breach** — wardens patrol; mother below |
| **Pressure** | Tremor doubles warden spawn |
| **Harvest** | Brood chitin → armor resin |
| **Trio** | Full trio nest clear template |
| **Prototype** | deferred — nest volume |

##### Glass Hive Swarmer

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B6 cliffs, Stratum 2 |
| **Visual** | Hex glass cells on cliff; insect-sized silicon flyers |
| **Habitat** | 1 hive per cliff face |
| **Behavior** | **Clear** — sonic stagger on proximity; quiet tools safe |
| **Harvest** | Hive wax → sonic dampener |
| **Trio** | Infiltrator silent clear |
| **Prototype** | deferred |

##### Highland Carrion Picker

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B6 hub trails |
| **Visual** | Carrion bird analog built from glass rods; harsh cry |
| **Habitat** | 2–3 circling jackal kills |
| **Behavior** | **Scan** — leads to recent combat or wreck |
| **Harvest** | Picker rod |
| **Trio** | Scout follow |
| **Prototype** | deferred |

##### Ridge Tremor Beetle

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B6 |
| **Visual** | Heavy dome beetle; vibrates before tremor |
| **Habitat** | 2–4 solos |
| **Behavior** | **Scan / time** — living tremor early warning |
| **Harvest** | Beetle resonator → seismic sensor craft |
| **Trio** | Science Specialist records pattern |
| **Prototype** | deferred |

##### Cave Scout Moth

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B6 breaches |
| **Visual** | Smaller than Plume Moth; blue-white; dives into tubes |
| **Habitat** | 4–6 per breach |
| **Behavior** | **Route** — follow into correct tube instance |
| **Harvest** | none |
| **Trio** | Scout highlights moth path |
| **Prototype** | deferred |

#### Machine hook — Survey Beacon Drone

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B6 breach pads |
| **Visual** | Idle floater; projects holographic breach map |
| **Habitat** | 1 per tutorial breach |
| **Behavior** | **Scan** — unlocks tube pressure profile |
| **Harvest** | Beacon data (no combat unless corrupted) |
| **Trio** | Communications Officer boost |
| **Prototype** | deferred — interactable only |

> **Full roster:** this section. Signatures: Tube Jackals, Brood mouths, Glass Hive — `Io_Biome_Exploration_Gameplay_Plan.md` §B6.

---

### B7 — Precursor Ruin Belt

#### Ecosystem overview

Resonance taints native life: **echo shelf → silence moth → stalker**. Resonance Echo Shelves (surface Echo Lichen cousin) puzzle audio; Silence Moths damp sound; Vault Stalkers patrol geometry; Still Hunter exists as rare myth trace. **Corrupted Patrol Androids** and **Rust Gardens** infest expedition tech — machines, not fauna.

**Food web (4 nodes):** Echo Shelf → Silence Moth → Vault Stalker → Resonance Skimmer → **android patrol (machine)** — no natural apex; food web breaks by design.

#### Flora

##### Resonance Echo Shelf

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B7, Stratum 5 surface gates |
| **Visual** | Teal precursor-symbiont sheets on ruin walls; chimes at wrong frequencies |
| **Habitat** | Puzzle corridors; 5–8 per vault approach |
| **Behavior** | **Scan / stabilize** — loud combat triggers Saturation drift |
| **Pressure** | Resonance supercell = maximum chime (puzzle hard mode) |
| **Harvest** | Echo shelf sample → Aether research |
| **Trio** | Specialist scan puzzle; Infiltrator silent |
| **Prototype** | deferred |

##### Aether Rim Filament

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B7 |
| **Visual** | Hair-thin teal filaments along ruin edges; pulse with Memory Core proximity |
| **Habitat** | Core fragment POIs |
| **Behavior** | **Sample** — proximity warning for android patrol |
| **Harvest** | Aether filament |
| **Trio** | Specialist core scan amplifier |
| **Prototype** | deferred |

##### Vault Glass Petal

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B7 antechambers |
| **Visual** | Glass petals arranged in non-human symmetry; fold on approach |
| **Habitat** | 3–5 per antechamber |
| **Behavior** | **Scan** — petal alignment puzzle for vault locks |
| **Harvest** | Petal shard → precursor alloy refine |
| **Trio** | Specialist puzzle lead |
| **Prototype** | deferred |

#### Fauna

##### Vault Stalker

| Field | Detail |
|-------|--------|
| **Tier** | elite |
| **Biome** | B7, Stratum 5 |
| **Visual** | Angular predator; teal seam eyes; avoids brine |
| **Habitat** | 1–2 patrol routes per ruin sector |
| **Behavior** | **Clear / route** — patrols precursor edges; weak to resonance puzzle traps |
| **Harvest** | Stalker prism → armor mod |
| **Trio** | Tactician pull into trap; Infiltrator bypass |
| **Prototype** | deferred |

##### Rift Stalker

See §3.2 — B7 elevated weight post–Memory Core.

##### Still Hunter (myth trace)

| Field | Detail |
|-------|--------|
| **Tier** | elite (ambient myth) |
| **Biome** | B7 rare |
| **Visual** | Never fully seen — heat-haze silhouette, three limbs, wrong proportions |
| **Habitat** | 0–1 director myth spawn per playthrough bias |
| **Behavior** | **Route** — fleeing encounter; drops myth tag only; no farm |
| **Pressure** | Resonance supercell only |
| **Harvest** | Still Hunter trace → codex / Aether-9 reaction |
| **Trio** | No combat recommended — escape verb |
| **Prototype** | deferred — scripted chase |

##### Silence Moth

| Field | Detail |
|-------|--------|
| **Tier** | ambient |
| **Biome** | B7 |
| **Visual** | Moth wings absorb sound; visual ripple when flapping |
| **Habitat** | Silent zones; 6–10 |
| **Behavior** | **Route** — killing moth = noise burst → android alert |
| **Harvest** | Silence scale → stealth mod |
| **Trio** | Infiltrator escort through silent zone |
| **Prototype** | deferred |

##### Resonance Skimmer

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B7 shallow ruins |
| **Visual** | Skates on teal resonance fields; small pack of 3 |
| **Habitat** | Open ruin plazas |
| **Behavior** | **Clear** — light combat; Saturation on death burst |
| **Harvest** | Skimmer node |
| **Trio** | Ranged preferred at distance |
| **Prototype** | deferred |

#### Machine hooks

##### Corrupted Patrol Android

| Field | Detail |
|-------|--------|
| **Tier** | common |
| **Biome** | B7 |
| **Visual** | Humanoid frame; teal corruption veins; silent until line-of-sight |
| **Habitat** | 1–2 per ruin patrol loop |
| **Behavior** | **Clear** — android threat; not fauna |
| **Harvest** | Patrol chip, precursor wire |
| **Trio** | Tactician clear; Specialist hack disable |
| **Prototype** | deferred — `android` / humanoid AI |

##### Rust Garden

See §3.4 — B7 android dig signature.

> **Full roster:** this section. Signatures: Vault Stalker, Rust Garden, corrupted androids — `Io_Biome_Exploration_Gameplay_Plan.md` §B7.

---

## 7. Underground ecology (Stratum 1–5)

Underground life is **pressure-adapted**, not surface copy-paste. Pool ecology rules from `Io_Underground_Architecture_Plan.md` §6.3 remain in force.

**Full cards:** this section. **Quick index:** underground plan §6.1–6.2 summary tables.  
**Global threats:** android / humanoid / machine / Void Stitcher — §4.

---

### Stratum 1 — Upper Lava Tubes

**Profile:** Refuge offset from wind/lightning; ash choke; skitter/jackal scavengers; Rust Gardens on wrecks.

**Food web:** Tube Lace → Cinder Tunnel Skitter → Tube Jackal → wreck Rust Garden swarmer.

#### Flora

| Name | Tier | Card summary |
|------|------|--------------|
| **Tube Lace** | ambient | Ceiling colony mats; O₂ micro-buffer; safe camp marker. **Harvest:** lace fiber. **Verb:** shelter/sample. **Prototype:** deferred |
| **Ash Choke Filament** | ambient | Grey sheets in ash-choked tubes; filters particulates; slip when dry. **Harvest:** choke filter. **Pressure:** ash events clog (block path). **Prototype:** deferred |
| **Skylight Drip Mat** | ambient | Drip-fed mats under skylight breaches; condensate bonus. **Harvest:** condensate. **Verb:** sample. **Prototype:** deferred |

#### Fauna

| Name | Tier | Card summary |
|------|------|--------------|
| **Tube Jackal** | common | Scavenger pack 3–5; flees deeper. See B6. **Prototype:** deferred |
| **Cinder Tunnel Skitter** | ambient | Pale skitter; eats lace drip; harmless unless swarmed. **Harvest:** chitin. **Prototype:** deferred |
| **Rust Garden** | common | Machine-coral on wrecks. See §3.4. **Prototype:** deferred |

---

### Stratum 2 — Mid Galleries

**Profile:** Branching networks; thermal seeps; Glass Kelp groves; brood tunnel entrances.

**Food web:** Glass Kelp → Glassfish school → Rift Skimmer → Glass Hive → Brood tunnel warden.

#### Flora

| Name | Tier | Card summary |
|------|------|--------------|
| **Glass Kelp** | ambient | Flooded tube groves; LOS cover. **Harvest:** kelp strand. **Verb:** route. **Prototype:** deferred |
| **Mid-Gallery Spore Veil** | ambient | Hanging spore curtains; vision debuff; marks seeps. **Harvest:** spore gel. **Prototype:** deferred |
| **Thermal Seep Bloom** | ambient | Orange crust at seeps; harvest only when seep cool. **Verb:** time/sample. **Prototype:** deferred |

#### Fauna

| Name | Tier | Card summary |
|------|------|--------------|
| **Glassfish School** | ambient | Silicon fish in kelp; flees light; **verb:** scan. **Prototype:** deferred |
| **Rift Skimmer** | common | 3-pack gliders over flooded junctions. **Clear/route.** **Harvest:** fin foil. **Prototype:** deferred |
| **Glass Hive Swarmer** | common | Sonic stagger hive on kelp. See B6. **Prototype:** deferred |
| **Vent Crab Migrant** | common | Lost B2 crabs in steam tubes; smaller, pale. **Clear.** **Prototype:** deferred |
| **Brood Tunnel Warden** | elite | Patrols tunnel mouths; tremor wake. **Nest grammar.** **Prototype:** deferred |

---

### Stratum 3 — Deep Volatile Basins

**Profile:** Brine lakes (wade-only); pool-edge predators; brood basins.

**Food web:** Brine Fan → Chemo Mantle slip → Lamprey Spire drop → Brine Hound → Basin Mantis.

#### Flora

| Name | Tier | Card summary |
|------|------|--------------|
| **Brine Fan** | ambient | Ring around pools; filters brine; wilts if drained. **Harvest:** gel. **Prototype:** deferred |
| **Chemo Mantle** | ambient | Pool floor sheet; slip hazard; Science sample buff. **Prototype:** deferred |
| **Pool Edge Filament** | ambient | Pale threads at O₂/brine boundary; marks ambush zone. **Verb:** scan/route. **Prototype:** deferred |

#### Fauna

| Name | Tier | Card summary |
|------|------|--------------|
| **Basin Mantis** | elite | Ambush from surface film; drag under wade. **Pressure:** film ripples tell. **Prototype:** deferred |
| **Brine Hound** | common | Pack alpha + 2 at pool rim. **Clear.** **Harvest:** brine sac. **Prototype:** deferred |
| **Lamprey Spire Colony** | common | Ceiling colony; drops on vibration. **Verb:** quiet route. **Prototype:** deferred |
| **Brood Mother Chamber** | elite | Deepest nest; optional dungeon boss. **Tremor wake.** **Prototype:** deferred |

#### Pool-type modifiers

| Pool class | Extra flora | Extra fauna bias |
|------------|-------------|------------------|
| Condensate pool | Brine Fan (pale variant) | Cinder Tunnel Skitter |
| Brimstone brine lake | Chemo Mantle dense | Brine Hound alpha |
| Brood basin | Pool Edge Filament | Basin Mantis + Lamprey |
| Flooded junction | Glass Kelp spillover | Rift Skimmer |

---

### Stratum 4 — Geothermal Roots

**Profile:** Lethal heat; silicate lenses; minimal permanent nests.

**Food web:** Silicate Mirror Bloom → Heat Root Lattice → Magma Phase Crawler → Heat Eel.

#### Flora

| Name | Tier | Card summary |
|------|------|--------------|
| **Chemo Mantle (heat strain)** | ambient | Heat-tolerant floor sheet; sample = thermal gel. **Prototype:** deferred |
| **Silicate Mirror Bloom** | ambient | Mirror-orange crust on lenses; **verb:** time crossing. **Prototype:** deferred |
| **Heat Root Lattice** | ambient | Root-like mineral net in lens rock; harvest with heat suit. **Prototype:** deferred |

#### Fauna

| Name | Tier | Card summary |
|------|------|--------------|
| **Heat Eel** | elite | Lens burst ambush. See B4. **Prototype:** deferred |
| **Magma Phase Crawler** | common | Solo; phases through thin crust; **clear/time.** **Prototype:** deferred |
| **Tremor Larva** | ambient | Burrows during swarm; indicates lens thin spot. **Scan.** **Prototype:** deferred |

---

### Stratum 5 — Resonance Vaults

**Profile:** Precursor architecture; Aether seeps; echo symbionts; android patrols.

**Food web:** Echo Lichen → Echo Symbiont Swarm → Vault Stalker → **android patrol (machine)**.

#### Flora

| Name | Tier | Card summary |
|------|------|--------------|
| **Echo Lichen** | ambient | Audio puzzle surfaces; Saturation if gunfire. **Prototype:** deferred |
| **Aether Seep Petal** | ambient | Teal pools; Memory Core adjacency. **Sample/scan.** **Prototype:** deferred |
| **Vault Symbiont Mat** | ambient | Non-Euclidean growth on floors; **stabilize** verb. **Prototype:** deferred |

#### Fauna

| Name | Tier | Card summary |
|------|------|--------------|
| **Vault Stalker** | elite | Patrols precursor edges. See B7. **Prototype:** deferred |
| **Echo Symbiont Swarm** | common | Teal particles; aggro on loud resonance. **Clear/quiet.** **Prototype:** deferred |
| **Still Hunter Trace** | elite myth | Environmental only; same rules as B7. **Prototype:** deferred |

#### Machine

| Name | Tier | Card summary |
|------|------|--------------|
| **Corrupted Patrol Android** | common | Vault patrol loops. **Android threat.** **Prototype:** deferred |
| **Rust Garden** | common | On expedition corpses in vault antechambers. §3.4. **Prototype:** deferred |

---

## 8. Promotion path to canon

| Content | Promote to | Keep art-only / engineering |
|---------|------------|------------------------------|
| Ecology pillars + taxonomy | GDD **A2e** intro | — |
| Per-biome roster tables + food webs | GDD **A2e** §surface | Individual concept art sheets |
| **§4 threat families** (android, humanoid, ground machine, flying fauna) | GDD **A2e** §expedition threats | Prefab rig list, animator sets |
| **§4.6 expedition pet** (Io-native **or** small robot; placeholders retired) | GDD **A2e** §loadout + B pet migration | Ricky / Fox Cub / Probe assets |
| **Void Stitcher** global elite | GDD **A2e** + ExperienceDirector elite pool | Seam-hide VFX tech |
| Underground stratum rosters | GDD **A2e** §subsurface (merge with A2c underground lock) | Modular kit names (Tube_Straight, etc.) |
| Cross-biome migratory table | GDD **A2e** + ExperienceDirector spawn weights | Exact weight numbers (tuning) |
| Encounter budget soft caps | GDD **A2e** + director docs | Per-zone JSON/SO tuning |
| Machine/android hooks | GDD A2 surface threats (already locked) | Prefab list, encounter tables |
| Prototype notes | **Do not promote** — track in B4 #9 only | Unity implementation |

**Do not rewrite GDD 5.0 in this pass.** After review, fold A2e and cross-reference from:
- `Io_Biome_Exploration_Gameplay_Plan.md` §4 wildlife lines (pointer only)
- `Io_Underground_Architecture_Plan.md` §6 (index + pointer)

**Out of scope (unchanged):** Unity prefabs, `SurfaceEncounterTable` wiring, animator/AI, art production, locked world rules (wade-only, instanced underground, vehicle gates). **Pet placeholder retirement** tracks with pet migration (GDD B4 #6), not B4 #9 biome pass alone.

---

*Dark Matter Studios — Dark Matter: Genesis — World Design*
