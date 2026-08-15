# Narrative Package V1: Ash & Signal

**Project:** Dark Matter: Genesis  
**Package identity:** **V1 Ash & Signal**  
**Player:** **Kade**  
**Genre tone:** Mystery / horror survival on Io  
**Canon spine:** Io 2160, AC-only economy, 22 base companions plus a switchable trio of 3, Kairos repair to 6 Memory Cores to trust, ending triad **Seal / Awaken / Symbiosis**  
**Designed hour target:** **165h** excluding open exploration; exploration adds ~25-40h bonus ambience, free roam, hunting, gathering, and vista discovery  
**Biome order lock:** **B6 -> B1 -> B2 -> B3 -> B5 -> B4 -> B7**  
**Permanent stakes lock:** Failed hostile Echo sync means permanent Echo loss; building/base damage injures companions but never kills them.

---

## 1. Premise / Tone / Kairos Truth + Pitch

### Pitch

Io does not behave like a moon. It behaves like a crime scene that keeps hiding the body.

**Kade** lands in 2160 on a basalt highland hub after colony systems detect impossible rescue pings from erased crews. The colony starts with **5000 Aether Credits (AC)**, enough to secure one starter Skilled Companion and a fragile foothold. A dormant machine intelligence, later identified as **Kairos**, lies broken under ash-glass stone. Kade repairs it through salvaged precursor components, but the awakened voice is not grateful. It recognizes human signatures, remembers betrayal, and treats the new colony as another infection.

The truth unfolds through Memory Cores, Echo chronicles, Resonance Events, faction disputes, and hostile ecology. **Kairos is not a god, not a vendor, and not a quest giver. Kairos is a precursor defense AI built to preserve Io from extraction, settlement, and contamination.** Its previous crew tried to repurpose the lattice. Io erased them. Whether Io acted through the defense system, through something below it, or through a deeper symbiosis remains the central horror.

### Tone Pillars

| Pillar | Production Direction | Content Expression |
|---|---|---|
| Ash remembers | Every biome carries evidence of vanished crews and failed extraction. | Echo chronicle fragments, rusted androids, half-sent radio loops, corpse-free camps. |
| Signals are dangerous | Voice, radio, beacon, and resonance can guide or lure. | Communications runtime [PLANNED], EchoGenerator chronicle [SHIPPED], Resonance Events [PLANNED]. |
| Io is alive enough | Ecology is not background dressing; organisms react to heat, radiation, ash, sound, and precursor fields. | Biome encounter tables, puzzles, environmental survival loops. |
| Progress costs trust | Kairos helps only after repair, cores, restraint, and demonstrated colony ethics. | Trust ladder, ending flags, faction consequences. |
| Survival is pressure, not slaughter | Base disasters injure, stall, and deform plans but do not kill the base-22. | Base injury-not-death lock, Building Control Panels, storm sheltering. |

### Kairos Truth

| Layer | Player Belief | Later Correction | Horror Beat |
|---|---|---|---|
| Act I | Kairos is a damaged colony AI. | It predates the colony and speaks through human-facing salvage interfaces. | It knows names not yet entered into the colony manifest. |
| Act II | Kairos killed its crew. | It tried to contain a crew breach, then lost core consensus. | Memory Cores disagree with each other. |
| Act III | Io erased the crew. | Io, the defense lattice, and biological resonance may be entangled. | Organisms respond to old command tones. |
| Act IV | The player can control the system. | The player can only choose a relationship to it: seal, awaken, or symbiose. | The final vault asks whether survival is containment, dominance, or surrender. |

### Non-Negotiable Shared Locks

| Lock | Package Use |
|---|---|
| AC-only economy, starter 5000 AC | Every reward and vendor hook uses AC; no external wallet or marketplace loop. |
| 22 base + trio of 3 | Mainline and side content assume 22 protected base roles plus field trio risk. |
| Kairos repair -> 6 Memory Cores -> trust | Mainline progress is trigger-driven, not quest-giver-driven. |
| Biome order B6 -> B1 -> B2 -> B3 -> B5 -> B4 -> B7 | Quest, puzzle, AC, and gear tiers follow this order. |
| ~40 side quests, ~40 puzzles | Tables below define 40 side quests and 40 puzzle quests. |
| 5 base + 2 external factions | Factions are Ashwatch, Isotope Choir, Scrim, Whisper Clinic, Basalt Exchange, Helix, Seam Runners. |
| Barrier Field friction pattern | Ash Barrier Fields recur as tuned locks, combat modifiers, survival hazards, and optional mastery challenges. |
| Echo sync fail = permanent loss | Explicit in Echo Rescue content and trust dialogue. |
| Base injury-not-death | Storms, attacks, and damage injure, delay, or reduce output; they never kill base companions. |

---

## 2. Biome & Ecology Integration Map

Life sheet refs match `Io_Biome_Life_Sheet_Manifest.md` (Set B under `ArtReference/LifeSheets_RayTraced/`). Read-only — do not delete or move ArtReference.

| Biome | Signature Creatures / Environments | Narrative Hooks | Life Sheet Refs |
|---|---|---|---|
| **B6 Basalt Highlands (hub)** | Cliff Tube Lace, Tube Jackal, Brood Tunnel Mouth, Glass Hive Swarmer, Cave Scout Moth, Beacon Hopper; basalt shelves, hub bowl, broken relay spines. | Starter camp sits where precursor signals thin out. Beacon Hoppers mimic rescue pings. Brood Tunnel Mouths mark future underground access. Cliff Tube Lace grows near old Kairos conduits. | `RT_LifeSheet_B5_B6_Polar_Highlands.png` |
| **B1 Sulfur Plains** | Brimstone Fan, Cinder Skitter, Sulfur Hound [SHIPPED], Brimstone Leech, Graveyard Scrapper Drone; sulfur plumes, Expedition Graveyard, ash-yellow storm lanes. | First evidence of erased crews appears in camps without bodies. Sulfur Hounds make the ecology feel present and shipped. Graveyard Scrapper Drones repeat corporate extraction orders after everyone is gone. | `RT_LifeSheet_B1_B2_Sulfur_Geyser.png` |
| **B2 Geyser Fields** | Geyser Pod, Vent Crab Worker / Queen, Plume Moth, Geyser Strider, Vent Capper Bot, Rusted Survey Drone; vent cadence corridors, rig ruins, corporate survey towers. | The third Kairos repair object sits in a rig ruin that drilled into the moon's breath. Plume Moths become signal carriers and false-safe-zone markers. Vent Crab Queens nest around heat-stable caches. | `RT_LifeSheet_B1_B2_Sulfur_Geyser.png` |
| **B3 Ash Flats & Ridges** | Ash Filament Mat, Basalt Jackal, Dust Spout Cluster, Ash Stalker, Ash Glass Wasp, Salvage Excavator Android; ash shelves, ridge beacons, glass wasp nests. | Silence and resonance become enemies. The player learns the Ash Barrier Field pattern and finds beacons that "sing" old crew calls. Salvage Excavator Androids dig where no marker exists. | `RT_LifeSheet_B3_B4_Ash_Caldera.png` |
| **B5 Polar Radiation Flats** | Void Kelp, Magnet Wyrm, Rift Stalker, Cold Spire Hound, Smuggler Remnant Android, Mag-Clamp Drone; cold lens fields, rad flats, hidden brine cuts. | Smuggling evidence reframes Memory Core transport. Void Kelp grows along radiation shadows. Rift Stalkers and Magnet Wyrms make navigation feel hunted even without hard gates. | `RT_LifeSheet_B5_B6_Polar_Highlands.png` |
| **B4 Lava Calderas** | Rim Glass Needle Mat, Caldera Mantis, Magma Skitter, Heat Eel, Caldera Heat Kite, Eruption Sentry Bot; caldera rims, glass bridges, heat-lock tunnels. | The crew death-site is revealed as a defensive containment failure. Caldera Mantises react to player heat tools. Eruption Sentry Bots still execute evacuation commands on living targets. | `RT_LifeSheet_B3_B4_Ash_Caldera.png` |
| **B7 Precursor Ruin Belt** | Resonance Echo Shelf, Vault Glass Petal, Vault Stalker, Silence Moth, Still Hunter (myth), Corrupted Patrol Android, Rust Garden; vault belt, shell gardens, silent galleries. | Final truth space. The Still Hunter is mostly flee-tag myth until the vault. Rust Garden androids show the defense lattice consuming its own servants. Vault Glass Petals act as keys, witnesses, and wounds. | `RT_LifeSheet_B7_Ruins_Global.png` |
| **Underground Stratum 1-5** | Tube Lace, Basin Mantis, Brine Hound, Brood Mother, Echo Lichen, Echo Symbiont Swarm; gas domes, brine falls, crust lattices, echo caverns. | Underground instances explain why surface camps vanish without conventional bodies. Echo Lichen preserves voice fragments. Echo Symbiont Swarms make Symbiosis feel like a horror choice, not a clean upgrade. | `RT_LifeSheet_Underground_S1_S2.png`, `RT_LifeSheet_Underground_S3_S4_S5.png` |
| **Global / Cross-Biome** | Void Stitcher, Plume Moth, Rift Stalker. | These are rumor creatures that cross normal biome boundaries and signal that Io's ecology ignores human maps. Void Stitcher sightings escalate after Resonance Events. | `RT_LifeSheet_B7_Ruins_Global.png` |
| **Pets flavor [PLANNED migration]** | Brimstone Puff vanity starter, Cinder Skitter Kit, Beacon Hopper juvenile, Cave Scout Moth charm. | Cosmetic or companion-flavor migration only. No separate pet loop supersedes Echo/trio canon. | `RT_LifeSheet_Pets_Core12.png`, `RT_LifeSheet_Pets_Vanity_Extras.png` |

### Ecology Usage Rules

| Rule | Production Use |
|---|---|
| Use shipped ecology visibly early | Sulfur Hound [SHIPPED] anchors B1 encounter credibility and tutorializes non-human Io threats. |
| Use androids as erased-crew evidence | Drones and androids carry orders from missing people, not exposition dumps. |
| Make organisms puzzle actors | Geyser Pods, Silence Moths, Vault Glass Petals, Tube Lace, and Echo Lichen support puzzle logic. |
| Keep myth creatures rare | Still Hunter and Void Stitcher should be felt before being fought or seen clearly. |
| Tie life sheets to quest tags | Side and puzzle table tags include biome/ecology names so encounter builders can map quests to art sheets. |

---

## 3. Systems Inventory [SHIPPED/PARTIAL/PLANNED] Hooked By Content

| System | Status | Current Package Hook | Content Dependency / Notes |
|---|---:|---|---|
| AC economy | [SHIPPED] | All quest, puzzle, faction, and mainline rewards pay AC only. Starter balance is 5000 AC. | Reward curve below avoids alternate currencies. |
| Building Control Panels 5-tab shell | [SHIPPED/PARTIAL Craft] | Base buildings use tabs **Overview \| Companions \| Production \| Craft \| Changes** for storm pause, sheltering, assignment, and module previews. | Craft tab is partial; Journal Craft remains recipe library / scroll learning, not primary production. |
| EchoGenerator + chronicle | [SHIPPED] | Echo chronicle milestones trigger mainline trust beats and optional side rescue arcs. | Failed hostile Echo sync is permanent loss. |
| Exposure / survival | [SHIPPED] | O2, radiation, and thermal pressure appear in B1 storm runs, B5 lens fields, B4 heat locks, and underground gas domes. | Thermal is one cold/heat bar with two poles. |
| Sulfur Hound + B1 encounter table | [SHIPPED] | Sulfur Hound appears in B1 mainline pressure, sides, and combat puzzle variants. | Use B1 table for early horror escalation. |
| World Engine GameState / WorldState / Directors | [SHIPPED] | Progress triggers are locations reached, cores attached, buildings online, flags set, and Resonance Events completed. | Mainline is not NPC quest-giver-driven. |
| Combat melee / ranged / hotbar | [SHIPPED] | Io-named melee, ranged, and hotbar rewards map to existing combat where noted. | Rewards tagged `[maps to existing combat]` or `[future build]`. |
| Hovercraft | [PARTIAL] | Late B2/B3 traversal rewards and shortcuts reference hovercraft handling but avoid making it a hard story dependency. | Can be optional transport until complete. |
| Weather stubs | [PARTIAL] | Sulfur storm windows, ash fronts, and radiation lens timings are designed as content loops. | Weather director live remains planned. |
| Quest scaffold | [PARTIAL] | Side/faction boards and objective tables use scaffold-friendly IDs, types, objectives, and gates. | Mainline trigger sheet can be implemented as WorldState/Director flags. |
| Communications runtime | [PLANNED] | Kairos lines, faction radio boards, and signal horror use future runtime. | Interim can be delivered through UI/logs if needed. |
| Kairos + Memory Cores + Resonance Events | [PLANNED] | Central package spine: repair, 6 cores, trust ladder, ending triad. | Requires core socketing, event flags, and trust scoring. |
| Purification Hub live | [PLANNED] | Whisper Clinic quests, Saturation cleanse, Echo loss mitigation, and long expedition recovery. | Must not prevent permanent loss after failed hostile sync. |
| Io biome prefabs | [PLANNED] | Biome map and ecology tags specify prefab/content targets. | Prototype flat terrain can host stand-ins. |
| Underground instances | [PLANNED] | Stratum 1-5 side and puzzle content uses gas domes, floor crust, Echo Lichen, Brood Mother. | Use instanced depth pushes, not full open underground at first. |
| Base modules: generators / power / logistics / defense / mining | [PLANNED] | Faction and Building Control content references modules as production, shelter, and repair friction. | Storms pause queues; base damage injures, never kills. |
| Pet migration | [PLANNED] | Brimstone Puff, Cinder Skitter Kit, Beacon Hopper juvenile are flavor/cosmetic hooks only. | Fold into Echo/trio system; no separate pet progression loop. |
| Weather director live | [PLANNED] | Sulfur storms, ash fronts, heat surges, and polar lens events become systemic pressure. | Current weather stubs can fake windows for V1. |

---

## 4. Mainline Progress-Triggered Beat Sheet

### Mainline Design Rule

The mainline is **not quest-giver-driven**. No NPC hands the player the critical path. Progress advances through **WorldState and GameState triggers**: locations reached, repair objects installed, Memory Cores attached, biomes entered in locked order, Echo chronicle milestones, Building Control structures online, Ash Barrier Field patterns solved, and Resonance Events completed. Kairos reacts to progress; it does not behave like a normal quest board.

### AC Reward Tier Curve

| Tier | Campaign Range | Mainline Beat AC | Side Quest AC | Mastery Puzzle AC | Reward Intent |
|---|---|---:|---:|---:|---|
| **T1 Early** | Act I / B6-B1 | 200-450 AC | 250-500 AC | 600-900 AC | Stabilize starter economy after 5000 AC companion choice. |
| **T2 Mid** | Act II / B2-B3 | 450-900 AC | 500-950 AC | 900-1500 AC | Support gear breadth, ranged/hotbar upgrades, and faction spend. |
| **T3 Late Mid** | Act III / B5-B4 | 900-1800 AC | 1000-2000 AC | 1500-2200 AC | Fund environmental kit, base modules, and higher-risk expedition loadouts. |
| **T4 Endgame** | Act IV / B7 / faction finales | 1800-3500 AC | 2000-4500 AC | 2200-3000 AC | Let players finish builds, buy late gear, and resolve faction consequences. |

Mastery puzzles provide prestige AC and gear/lore bonuses only. They do **not** block the story.

### Mainline Beat Sheet

| Beat ID | Act | Biome | Trigger (Not Quest Giver) | Horror / Mystery Beat | Systems / Ecology Tags | AC | Weapon / Upgrade Reward | Est.h |
|---|---|---|---|---|---|---:|---|---:|
| ML-V1-01 | Prologue | B6 Basalt Highlands | Player reaches hub bowl and activates first survival relay. | Rescue pings come from Beacon Hoppers, not people. | WorldState [SHIPPED], exposure [SHIPPED], Beacon Hopper, Cliff Tube Lace | 200 | **Basalt Utility Knife** `[maps to existing combat]` | 1.5 |
| ML-V1-02 | Prologue | B6 | Building Control Panel Command shell comes online. | The base panel lists 22 empty room assignments before the player recruits anyone. | Building Control [SHIPPED/PARTIAL Craft], base injury-not-death | 250 | **Cordon Hotbar Flare** `[maps to existing hotbar]` | 1.0 |
| ML-V1-03 | I | B6 | Dormant Kairos shell discovered under ash-glass bowl. | The machine shell speaks one word from a dead crew log: "leave." | Kairos [PLANNED], Echo chronicle [SHIPPED], Glass Hive Swarmer | 300 | **Resonator Socket Harness** `[future build]` | 1.0 |
| ML-V1-04 | I | B6 | **Ash-Tuned Resonator Coil** installed. | Cliff Tube Lace pulses like a nervous system around the shell. | Repair object, Ash Barrier Field, Tube Jackal | 350 | **Ash-Tuned Baton** `[maps to existing melee]` | 1.5 |
| ML-V1-05 | I | B1 Sulfur Plains | Player enters B1 after B6 repair flag. | The Expedition Graveyard contains equipment arranged like a warning shrine. | Sulfur Hound [SHIPPED], B1 table [SHIPPED], Brimstone Fan, Graveyard Scrapper Drone | 400 | **Graveyard Scrapper Pistol** `[maps to existing ranged]` | 1.5 |
| ML-V1-06 | I | B1 | **Cryo-Sealed Memory Bus** recovered during sulfur storm window. | A storm reveals silhouettes of crew who are not there. | Weather stubs [PARTIAL], exposure [SHIPPED], Cinder Skitter, Brimstone Leech | 450 | **Sulfur-Baffled Filter Mk I** `[future build]` | 1.5 |
| ML-V1-07 | I | B1 | Repair object 2 installed into shell. | Kairos wakes hostile and accuses the colony of wearing dead faces. | Communications [PLANNED], Kairos [PLANNED] | 450 | **A9 Static Ward** `[future build]` | 1.0 |
| ML-V1-08 | I | B1 | Core 1 signal appears after Echo chronicle milestone 1. | First Memory Core replays corporate voices begging Io to stop listening. | EchoGenerator [SHIPPED], Memory Core [PLANNED], Sulfur Hound [SHIPPED] | 450 | **Echo Sync Clamp** `[future build]` | 1.5 |
| ML-V1-C1 | I | B1 | **Memory Core C1 attached** to Kairos. | Resonance Event: weaker Echo clones of past foes enter the camp perimeter. | Resonance Event [PLANNED], Echo sync permanent loss | 450 | **Cinderline Sidearm** `[maps to existing ranged]` | 1.5 |
| ML-V1-09 | II | B2 Geyser Fields | Player enters B2 after C1 and B1 exit flag. | Plume Moths gather around vent pings that sound like children. | Geyser Pod, Plume Moth, Vent Crab Worker/Queen | 550 | **Vent-Step Boots** `[future build]` | 1.0 |
| ML-V1-10 | II | B2 | **Precursor Lattice Key** recovered from rig ruin. | Rusted Survey Drones still file reports to owners who no longer exist. | Rusted Survey Drone, Vent Capper Bot, quest scaffold [PARTIAL] | 650 | **Geyser Pulse Carbine** `[maps to existing ranged]` | 1.5 |
| ML-V1-C2 | II | B2 | **Memory Core C2 attached** after vent map triangulation. | Resonance Event: phantom vent fog shows false safe zones. | Kairos [PLANNED], Geyser Strider, exposure [SHIPPED] | 750 | **Geothermal Probe Map** `[future build]` | 1.5 |
| ML-V1-11 | II | B3 Ash Flats & Ridges | Player enters B3 after C2 and repair-complete flag. | The ash ridges sing with old distress calls when weapons are fired. | Ash Filament Mat, Ash Stalker, Ash Glass Wasp, Silence Mandate | 800 | **Ashglass Wasp Needler** `[maps to existing ranged]` | 1.5 |
| ML-V1-C3 | II | B3 | **Memory Core C3 attached** after beacon choir sequence. | Kairos shifts from hostile to advisor and warns before the next storm. | Resonance Echo Shelf, Salvage Excavator Android, Communications [PLANNED] | 900 | **Scout Relay Mesh** `[future build]` | 2.0 |
| ML-V1-12 | III | B5 Polar Radiation Flats | Player enters B5 after C3 trust threshold. | Void Kelp bends away from the player's Echo trio, not the player. | Void Kelp, Magnet Wyrm, Rift Stalker, exposure [SHIPPED] | 1000 | **Polar Lens Rifle** `[maps to existing ranged]` | 1.5 |
| ML-V1-C4 | III | B5 | **Memory Core C4 attached** after lens crossing. | Resonance Event: cold/heat meter inverts while Kairos remembers smuggling cores through the ice. | Cold Spire Hound, Smuggler Remnant Android, Mag-Clamp Drone | 1300 | **Rad Suit Mk II Schematic** `[future build]` | 2.0 |
| ML-V1-13 | III | B4 Lava Calderas | Player enters B4 after C4 and heat kit flag. | Caldera rim glass reflects people who are missing from the party. | Rim Glass Needle Mat, Caldera Mantis, Magma Skitter, Heat Eel | 1400 | **Caldera Cleaver** `[maps to existing melee]` | 1.5 |
| ML-V1-C5 | III | B4 | **Memory Core C5 attached** at crew death-site. | Resonance Event: buildings take heat bleed but injure, never kill, sheltered base companions. | Eruption Sentry Bot, Caldera Heat Kite, base injury-not-death | 1800 | **Heat Lock Override** `[future build]` | 2.0 |
| ML-V1-14 | IV | B7 Precursor Ruin Belt | Player enters B7 after C5, trust Advisor+, and three surface locks active. | Silence Moths stop moving when the player looks directly at the vault. | Vault Glass Petal, Vault Stalker, Silence Moth, Corrupted Patrol Android | 2000 | **Vault Petal Blade** `[maps to existing melee]` | 1.5 |
| ML-V1-C6 | IV | B7 | **Memory Core C6 attached** in Vault Heart chamber. | Kairos admits the defense lattice was built to prevent exactly this choice. | Resonance Echo Shelf, Rust Garden, Still Hunter myth | 2800 | **Lattice Heart Key** `[future build]` | 2.0 |
| ML-V1-15 | IV | B7 | Final Resonance Event completed and ending flags evaluated. | The Still Hunter is no longer a myth; it waits without attacking until the choice is made. | Still Hunter, WorldState [SHIPPED], Kairos [PLANNED] | 3500 | **Ending Relic: Seal / Awaken / Symbiosis** `[future build]` | 2.5 |

**Mainline subtotal:** ~30h.

### The 6 Memory Cores

| Core | Name | Biome | Recovery Setpiece | Resonance Event | Trust Shift | Unlock |
|---|---|---|---|---|---|---|
| C1 | **Fractured Log** | B1 | Storm-window run through Expedition Graveyard while Sulfur Hounds and Graveyard Scrapper Drones converge. | Echo Field: past enemies appear as weaker Echo clones. | Hostile -> suspicious. | Resonance Beacon and Cinderline Sidearm. |
| C2 | **Vent Cartography** | B2 | Ride geyser cadence, scan Geyser Pods, avoid Vent Capper Bots. | Phantom Vent Fog creates false safe lanes. | Suspicious -> tactical warning. | Geothermal Probe Map. |
| C3 | **Ash Beacon Choir** | B3 | Tune ridge beacons without waking Ash Glass Wasp nests or Salvage Excavator Androids. | Beacon Cascade makes all ash ridges sing. | Tactical warning -> advisor. | Scout Relay Mesh and Barrier Field readouts. |
| C4 | **Lens Confession** | B5 | Cross polar radiation lens with Void Kelp shadow cover and Mag-Clamp Drone patrols. | Cold Lens inverts thermal survival for 12 minutes. | Advisor -> regret. | Rad Suit Mk II schematic. |
| C5 | **Rim Watcher** | B4 | Survive caldera death-site, Heat Eel vents, and Eruption Sentry Bot evacuation logic. | Magma Mirror creates base heat bleed, injuries, and production stalls. | Regret -> friend. | Heat Lock Override. |
| C6 | **Vault Heart** | B7 | Solve surface locks, recover Vault Glass Petal, survive Still Hunter flee-tag. | Resonance Supercell opens final vault. | Friend -> choice partner. | Ending triad. |

### Ending Triad

| Ending | Requirement Bias | Choice | Outcome | Horror Cost |
|---|---|---|---|---|
| **Seal** | High restraint, Ashwatch/Clinic trust, low forced awaken flags. | Cage Kairos and the defense lattice. | Io quiets. The colony survives under a sleeping guardian. | Kairos remains conscious enough to hear but not speak. |
| **Awaken** | High Helix/Ashwatch militarization or repeated dominance choices. | Fully restore Kairos as active defense AI. | Colony gains power, vault weapons, and endless pressure content. | Io fights back harder, and the player may have created a new jailer. |
| **Symbiosis** | High Echo compassion, Choir/Clinic trust, accepted Saturation costs. | Merge Kade's imprint with the lattice. | Hybrid ending; colony and Io communicate through shared harm. | Trio suffers permanent Saturation scars; player identity becomes uncertain. |

### Sample Trust Dialogue

| Trust Stage | Trigger | Kairos Sample Dialogue |
|---|---|---|
| Hostile | Repair objects installed, Kairos shell wakes. | "You wear their faces. Leave the cores. Leave the moon." |
| Suspicious | C1 attached; player preserves at least one hostile Echo. | "One fragment returned. Not forgiveness. Not yet." |
| Tactical | C2 attached; player survives Phantom Vent Fog. | "Vent pressure rising. Your map is lying. Mine is less wrong." |
| Advisor | C3 attached; Echo chronicle milestone reached. | "Do not trust the seams. Signals gather where teeth should be." |
| Regretful | C4 attached; smuggling truth revealed. | "They carried pieces of me through the cold. I called them thieves. Some were only afraid." |
| Friend | C5 attached; crew death-site truth processed. | "Kade, if you seal me, I will still listen. If you awaken me, I may not remain kind." |
| Choice Partner | C6 attached; final vault opens. | "Io erased a crew once. I helped. I failed. Choose the shape of my next failure." |

---

## 5. 40 Side Quests

Side and faction quests may use NPCs, boards, terminals, or faction dispatches. Unlike the mainline, these can be giver-driven. AC values follow the tier curve. Weapon and upgrade rewards are Io/sci-fi named and tagged for implementation mapping.

| ID | Title | Biome | Giver (NPC/Board OK) | Type | Objectives | AC | Weapon/Upgrade | Est.h | Gates/Tags |
|---|---|---|---|---|---|---:|---|---:|---|
| SQ-V1-01 | Cordon First Light | B6 | Ashwatch Cordons board | Recon / Base security | Mark 3 perimeter posts, test Command Center shelter call, return before ashfall. | 300 | **Ashwatch Riot Plate** `[future build]` | 1.0 | Ashwatch, Building Control [SHIPPED/PARTIAL Craft], Beacon Hopper |
| SQ-V1-02 | Hopper Ping | B6 | Basalt Exchange board | Signal salvage | Track rescue pings that are Beacon Hopper mimic calls, tag safe frequencies. | 350 | **Signal Tagger Hotbar Mod** `[maps to existing hotbar]` | 1.1 | Basalt Exchange, EchoGenerator [SHIPPED], Beacon Hopper |
| SQ-V1-03 | Tube Jackal Line | B6 | Ashwatch ranger NPC | Encounter / Escort | Escort a worker trio past Tube Jackal territory without drawing Glass Hive Swarmers. | 450 | **Cordon Shock Baton** `[maps to existing melee]` | 1.2 | Tube Jackal, Glass Hive Swarmer, base injury-not-death |
| SQ-V1-04 | Brood Mouth Survey | B6 | Isotope Choir terminal | Survey / Underground lead | Scan 2 Brood Tunnel Mouths and recover Tube Lace growth samples. | 500 | **Cliff Tube Lace Analyzer** `[future build]` | 1.3 | Isotope Choir, Underground [PLANNED], Cliff Tube Lace |
| SQ-V1-05 | Graveyard First Haul | B1 | Scrim Salvage Compact board | Salvage run | Strip a low-risk Expedition Graveyard hull while avoiding Graveyard Scrapper Drones. | 500 | **Scrim Mag-Cutter** `[maps to existing melee]` | 1.2 | Scrim, Graveyard Scrapper Drone, AC [SHIPPED] |
| SQ-V1-06 | Brimstone Puff Starter | B1 | Whisper Clinic handler | Pet flavor / Rescue | Lure a Brimstone Puff vanity starter away from Brimstone Leeches; log migration note. | 350 | **Brimstone Puff Charm** `[future build]` | 1.0 | Pet migration [PLANNED], Brimstone Leech, Brimstone Puff |
| SQ-V1-07 | Sulfur Hound Collar | B1 | Ashwatch Cordons board | Combat / Field study | Defeat or evade a Sulfur Hound [SHIPPED], retrieve a damaged sensor collar. | 450 | **Sulfur-Bite Machete** `[maps to existing melee]` | 1.3 | Sulfur Hound [SHIPPED], B1 encounter table [SHIPPED] |
| SQ-V1-08 | Brimstone Fan Bloom | B1 | Isotope Choir NPC | Environmental survival | Harvest Brimstone Fan samples during a storm lull without triggering Cinder Skitter swarms. | 500 | **Sulfur Filter Patch** `[future build]` | 1.2 | Exposure [SHIPPED], Brimstone Fan, Cinder Skitter |
| SQ-V1-09 | The Leech Line | B1 | Whisper Clinic board | Medical / Rescue | Clear Brimstone Leeches from an injured worker shelter; no base companion deaths. | 500 | **Whisper Triage Injector** `[maps to existing hotbar]` | 1.2 | Whisper Clinic, base injury-not-death, exposure [SHIPPED] |
| SQ-V1-10 | Vent Lies | B2 | Isotope Choir board | Investigation | Compare Rusted Survey Drone logs against live Geyser Pod cadence. | 650 | **Choir Scanner Lens** `[future build]` | 1.2 | Rusted Survey Drone, Geyser Pod, quest scaffold [PARTIAL] |
| SQ-V1-11 | Queen Under Pressure | B2 | Ashwatch / Choir joint board | Encounter ecology | Interrupt a Vent Crab Queen nest without killing Workers if possible. | 800 | **Vent-Knuckle Gauntlet** `[maps to existing melee]` | 1.5 | Vent Crab Worker/Queen, faction conflict |
| SQ-V1-12 | Capper Bot Amnesty | B2 | Scrim Salvage Compact NPC | Salvage / Choice | Disable or repurpose a Vent Capper Bot protecting a sealed rig crate. | 700 | **Capper Pulse Pistol** `[maps to existing ranged]` | 1.3 | Vent Capper Bot, Scrim, Kairos [PLANNED] |
| SQ-V1-13 | Plume Moth Lanterns | B2 | Seam Runner contact | Stealth / Signal | Follow Plume Moths through vent fog to find a hidden drop without firing. | 850 | **Mute-Flare Launcher** `[maps to existing ranged]` | 1.4 | Plume Moth, Seam Runners, Silence Mandate |
| SQ-V1-14 | Strider Wake | B2 | Basalt Exchange board | Escort / Traversal | Escort supplies across Geyser Strider territory, using cadence reads instead of combat. | 950 | **Exchange Load Frame** `[future build]` | 1.5 | Geyser Strider, Basalt Exchange, hovercraft [PARTIAL] |
| SQ-V1-15 | Ash Filament Autopsy | B3 | Isotope Choir lab | Horror / Survey | Gather Ash Filament Mat strands from an erased camp with no bodies. | 850 | **Filament Edge Knife** `[maps to existing melee]` | 1.2 | Ash Filament Mat, Echo chronicle [SHIPPED] |
| SQ-V1-16 | Basalt Jackal Contract | B3 | Ashwatch board | Combat / Hunt | Track Basalt Jackals lured by broken beacon tones; choose repel or relocate. | 900 | **Jackal-Marked Carbine Stock** `[maps to existing ranged]` | 1.3 | Basalt Jackal, Ashwatch, Communications [PLANNED] |
| SQ-V1-17 | Dust Spout Miser | B3 | Basalt Exchange merchant | Environmental survival | Recover AC lockbox from Dust Spout Cluster timing route. | 950 | **Spout-Timer Hotbar Chip** `[maps to existing hotbar]` | 1.2 | Dust Spout Cluster, AC [SHIPPED] |
| SQ-V1-18 | Wasp Glass | B3 | Scrim Salvage Compact board | Salvage / Encounter | Harvest Ash Glass Wasp nest glass without starting a full swarm. | 900 | **Ashglass Wasp Needler** `[maps to existing ranged]` | 1.4 | Ash Glass Wasp, Scrim |
| SQ-V1-19 | Excavator Prayer | B3 | Whisper Clinic / Echo board | Echo Rescue | Sync the Echo bound to a Salvage Excavator Android; failure is permanent loss. | 950 | **Soft Sync Collar Mk I** `[future build]` | 1.5 | Salvage Excavator Android, Echo sync permanent loss |
| SQ-V1-20 | Barrier Black Market | B3 | Seam Runners contact | Stealth / Faction | Steal an Ash Barrier Field bypass tone and decide whether to share it. | 950 | **Seam Mute Boots** `[future build]` | 1.4 | Ash Barrier Field, Seam Runners, faction conflict |
| SQ-V1-21 | Void Kelp Shadow | B5 | Isotope Choir board | Survey / Survival | Chart Void Kelp radiation shadows and keep trio Saturation below threshold. | 1200 | **Void-Kelp Rad Wrap** `[future build]` | 1.4 | Void Kelp, exposure [SHIPPED], Saturation |
| SQ-V1-22 | Magnet Wyrm Compass | B5 | Basalt Exchange board | Traversal / Recovery | Recover a compass crate from Magnet Wyrm interference. | 1400 | **Mag-Clamp Sidearm Rail** `[maps to existing ranged]` | 1.5 | Magnet Wyrm, Mag-Clamp Drone |
| SQ-V1-23 | Rift Stalker Rumor | B5 | Seam Runners whisper board | Horror / Stealth | Confirm Rift Stalker traces without attacking; mark safe smuggler route. | 1600 | **Seam Cloak** `[future build]` | 1.6 | Rift Stalker, Seam Runners, Silence Mandate |
| SQ-V1-24 | Cold Spire Kennel | B5 | Ashwatch Cordons board | Encounter / Rescue | Rescue a pinned surveyor from Cold Spire Hound territory. | 1500 | **Cold-Spire Pike** `[maps to existing melee]` | 1.5 | Cold Spire Hound, base injury-not-death |
| SQ-V1-25 | Smuggler Remnant | B5 | Helix Remnant Envoys | Investigation / Choice | Recover data from Smuggler Remnant Android; give it to Helix, Choir, or Seam. | 2000 | **Black Directive Chip** `[future build]` | 1.7 | Smuggler Remnant Android, Helix, Isotope Choir, Seam Runners |
| SQ-V1-26 | Mag-Clamp Debt | B5 | Scrim Salvage Compact board | Salvage / Combat | Disable Mag-Clamp Drones holding a polar salvage crane. | 1800 | **Scrim Clamp Hammer** `[maps to existing melee]` | 1.5 | Mag-Clamp Drone, Scrim |
| SQ-V1-27 | Rim Needle Silence | B4 | Ashwatch Cordons board | Environmental survival | Cross Rim Glass Needle Mat without shattering noise into mantis territory. | 1600 | **Rim-Glass Buckler** `[maps to existing hotbar]` | 1.5 | Rim Glass Needle Mat, Silence Mandate |
| SQ-V1-28 | Caldera Mantis Proof | B4 | Isotope Choir lab | Encounter ecology | Record Caldera Mantis behavior around heat tools, survive without killing queen form. | 1800 | **Mantis Heat Talon** `[maps to existing melee]` | 1.7 | Caldera Mantis, exposure [SHIPPED] |
| SQ-V1-29 | Magma Skitter Nest | B4 | Scrim Salvage Compact board | Combat / Cache | Clear or lure Magma Skitters from a caldera cache. | 1500 | **Magma-Spike Thrower** `[maps to existing ranged]` | 1.4 | Magma Skitter, loot cache |
| SQ-V1-30 | Heat Eel Conduit | B4 | Basalt Exchange board | Repair / Survival | Route power around Heat Eel vent conduits to restore a trade beacon. | 1900 | **Heat-Eel Coil Mod** `[future build]` | 1.6 | Heat Eel, base modules [PLANNED] |
| SQ-V1-31 | Kite Over Rim | B4 | Seam Runner scout | Stealth / Route | Use Caldera Heat Kite shadows to cross an exposed rim without drone detection. | 2000 | **Kite-Shadow Cloak** `[future build]` | 1.6 | Caldera Heat Kite, Seam Runners |
| SQ-V1-32 | Sentry Evacuation | B4 | Whisper Clinic board | Rescue / Choice | Stop Eruption Sentry Bot from "evacuating" injured companions into lethal zones. | 2000 | **Eruption Sentry Override** `[future build]` | 1.8 | Eruption Sentry Bot, base injury-not-death |
| SQ-V1-33 | Echo Shelf Names | B7 | Isotope Choir board | Story / Reveal | Tune Resonance Echo Shelf to recover names from erased crews. | 2400 | **Shelf Harmonic Tuner** `[future build]` | 1.6 | Resonance Echo Shelf, Echo chronicle [SHIPPED] |
| SQ-V1-34 | Vault Glass Petal Cut | B7 | Scrim Salvage Compact legend board | Salvage / Puzzle | Recover a cracked Vault Glass Petal without breaking its memory imprint. | 2600 | **Vault Petal Knife** `[maps to existing melee]` | 1.7 | Vault Glass Petal, Scrim |
| SQ-V1-35 | Stalker In The Gallery | B7 | Ashwatch Cordons board | Combat / Encounter | Survive Vault Stalker ambush in a silent gallery; extract data shard. | 3000 | **Stalker-Bore Rifle** `[maps to existing ranged]` | 1.8 | Vault Stalker, Silence Moth |
| SQ-V1-36 | The Silence Moth Rule | B7 | Seam Runners board | Stealth / Horror | Escort a courier through Silence Moth territory without killing or firing. | 3200 | **Mute-Step Harness** `[future build]` | 1.8 | Silence Moth, Seam Runners, Silence Mandate |
| SQ-V1-37 | Still Hunter Myth | B7 | Whisper Clinic / Echo board | Horror / Optional elite trace | Trace Still Hunter signs and return before the third mark appears. | 3500 | **Still Mark Charm** `[future build]` | 1.8 | Still Hunter myth, Echo sync permanent loss |
| SQ-V1-38 | Corrupted Patrol Amnesty | B7 | Helix Remnant Envoys | Android / Choice | Reprogram or destroy Corrupted Patrol Androids guarding Helix claims. | 4000 | **Compliance Arc Rifle** `[maps to existing ranged]` | 1.8 | Corrupted Patrol Android, Helix conflict |
| SQ-V1-39 | Rust Garden Bloom | B7 | Isotope Choir / Kairos reaction | Story / Reveal | Study Rust Garden android growth and decide whether to burn or archive it. | 4200 | **Rust-Garden Nanoforge Seed** `[future build]` | 1.9 | Rust Garden, Kairos [PLANNED] |
| SQ-V1-40 | Runner Legend: No Footprints | B7 | Seam Runners finale board | Faction finale / Silent escort | Move a living witness past Vault Stalkers, Silence Moths, and a Still Hunter trace. | 4500 | **Seam Runner Legend Kit** `[future build]` | 2.0 | Seam Runners, B7 finale, Still Hunter |

**Side quest subtotal:** ~50h.

---

## 6. 40 Puzzle Quests

Purpose distribution is intentionally mixed. Only a minority are hard access gates.

| Purpose Type | Count | Design Rule |
|---|---:|---|
| Access Gate / Area Unlock | 8 | Used for critical repair/core/vault routes only. |
| Loot / Cache / Weapon Unlock | 8 | Rewards gear, AC, caches, or vendor stock without blocking story. |
| Environmental Survival (no gate) | 7 | Teaches or stresses survival systems without locking progression. |
| Combat / Encounter Puzzle | 6 | Changes how fights are approached rather than simply increasing enemy count. |
| Story / Reveal | 6 | Unlocks lore, trust dialogue, or faction truth. |
| Optional Mastery / Challenge | 5 | Prestige AC 600-3000; no story gate. |

| ID | Title | Purpose Type | Solve | Payoff (AC / Weapon / Lore / Unlock) | Links | Est.h | Tags |
|---|---|---|---|---|---|---:|---|
| PQ-V1-01 | Bowl Field Tuning | Access Gate / Area Unlock | Align three ash tones around the dormant Kairos shell using the Resonator Coil. | Unlocks shell chamber; 250 AC; lore ping. | ML-V1-03 | 0.8 | B6, Ash Barrier Field, Kairos [PLANNED] |
| PQ-V1-02 | Ridge Resonator Gate | Access Gate / Area Unlock | Match ridge frequency pulses while avoiding Glass Hive Swarmer agitation. | Unlocks repair object 1; **Ash-Tuned Baton**. | ML-V1-04 | 1.0 | B6, Glass Hive Swarmer, repair |
| PQ-V1-03 | Sulfur Lock Petals | Access Gate / Area Unlock | Rotate sulfur-stained Vault Glass Petal fragments only during low plume pressure. | Unlocks repair object 2; 350 AC. | ML-V1-06 | 1.1 | B1, Brimstone Fan, exposure [SHIPPED] |
| PQ-V1-04 | Vent Cap Cipher | Access Gate / Area Unlock | Use vent cadence, Science scan, and a rig-side breaker to open corporate inner door. | Unlocks repair object 3; **Geyser Pulse Carbine**. | ML-V1-10 | 1.2 | B2, Vent Capper Bot, Rusted Survey Drone |
| PQ-V1-05 | Lens Gate | Access Gate / Area Unlock | Cross polar lens nodes when cold/heat meter passes through neutral. | Unlocks Core C4 route; 900 AC. | ML-V1-C4 | 1.1 | B5, Void Kelp, exposure [SHIPPED] |
| PQ-V1-06 | Heat Lock Secundus | Access Gate / Area Unlock | Route heat into two sacrificial vents, then trigger a Stability Bubble. | Unlocks C5 death-site; **Heat Lock Override**. | ML-V1-C5 | 1.3 | B4, Heat Eel, Eruption Sentry Bot |
| PQ-V1-07 | Three Surface Locks | Access Gate / Area Unlock | Bring B3, B5, and B4 signal proofs to B7 surface lock obelisks. | Unlocks B7 vault approach; 2200 AC. | ML-V1-14 | 1.5 | B7, Ash Barrier Field, WorldState [SHIPPED] |
| PQ-V1-08 | Lattice Cage Final | Access Gate / Area Unlock | Seal path only: close lattice nodes without waking all defense subroutines. | Enables Seal ending; 3000 AC. | ML-V1-15 | 1.5 | B7, ending, Kairos [PLANNED] |
| PQ-V1-09 | Graveyard Scrapper Vault | Loot / Cache / Weapon Unlock | Reorder dead drone commands so Graveyard Scrapper Drones open their own cache. | 500 AC; **Graveyard Scrapper Pistol**. | SQ-V1-05 | 1.0 | B1, Graveyard Scrapper Drone, AC [SHIPPED] |
| PQ-V1-10 | Brimstone Leech Still | Loot / Cache / Weapon Unlock | Use bait heat and O2 timing to pull Brimstone Leeches off a sealed med crate. | 500 AC; Whisper med cache. | SQ-V1-09 | 1.0 | B1, Brimstone Leech, Whisper Clinic |
| PQ-V1-11 | Vent Crab Queen Cache | Loot / Cache / Weapon Unlock | Feed vent pressure to Worker tunnels without enraging the Queen. | 850 AC; **Vent-Knuckle Gauntlet**. | SQ-V1-11 | 1.1 | B2, Vent Crab Worker/Queen |
| PQ-V1-12 | Ashglass Nest Cut | Loot / Cache / Weapon Unlock | Harvest glass in silence using timed smoke cover. | 900 AC; **Ashglass Wasp Needler**. | SQ-V1-18 | 1.1 | B3, Ash Glass Wasp, Silence Mandate |
| PQ-V1-13 | Magnet Wyrm Coil | Loot / Cache / Weapon Unlock | Follow magnetic pull pulses to find a buried coil before drones reclaim it. | 1400 AC; **Mag-Clamp Sidearm Rail**. | SQ-V1-22 | 1.1 | B5, Magnet Wyrm, Mag-Clamp Drone |
| PQ-V1-14 | Caldera Armory Vent | Loot / Cache / Weapon Unlock | Deflect Heat Eel vent bursts into an old armory seal. | 1800 AC; **Caldera Cleaver**. | SQ-V1-30 | 1.2 | B4, Heat Eel, base modules [PLANNED] |
| PQ-V1-15 | Vault Petal Cut | Loot / Cache / Weapon Unlock | Cut a Vault Glass Petal along resonance veins without breaking the imprint. | 2600 AC; **Vault Petal Blade**. | SQ-V1-34 | 1.3 | B7, Vault Glass Petal |
| PQ-V1-16 | Rust Garden Frame | Loot / Cache / Weapon Unlock | Wake one Rust Garden android frame long enough to copy its actuator map. | 2800 AC; **Rust-Garden Nanoforge Seed**. | SQ-V1-39 | 1.4 | B7, Rust Garden, android |
| PQ-V1-17 | Storm Hatch Window | Environmental Survival (no gate) | Open a hatch only during sulfur storm peak, then reach shelter before exposure spikes. | 450 AC; survival mastery note. | ML-V1-C1 | 1.0 | B1, weather stubs [PARTIAL], exposure [SHIPPED] |
| PQ-V1-18 | Spout Alley Timing | Environmental Survival (no gate) | Cross Dust Spout Cluster lanes with no sprinting and no companion injury. | 800 AC; **Spout-Timer Hotbar Chip**. | SQ-V1-17 | 1.0 | B3, Dust Spout Cluster |
| PQ-V1-19 | Void Kelp Shadow Scan | Environmental Survival (no gate) | Move through radiation shadows cast by Void Kelp while scans charge. | 1100 AC; rad data. | SQ-V1-21 | 1.1 | B5, Void Kelp, exposure [SHIPPED] |
| PQ-V1-20 | Brine Fall Bridge | Environmental Survival (no gate) | Time brine fall intervals and thermal swings to cross without a hard gate. | 1300 AC; Seam route clue. | SQ-V1-23 | 1.0 | B5, Seam Runners, Rift Stalker rumor |
| PQ-V1-21 | Rim Glass Quiet Walk | Environmental Survival (no gate) | Cross Rim Glass Needle Mat below a noise threshold. | 1600 AC; **Rim-Glass Buckler**. | SQ-V1-27 | 1.2 | B4, Rim Glass Needle Mat, Silence Mandate |
| PQ-V1-22 | Gas Dome Alpha | Environmental Survival (no gate) | Vent underground gas dome using Science timing; optional if bypass found. | 1200 AC; Underground map node. | UG Stratum 2 | 1.2 | Underground, Tube Lace, Echo Lichen |
| PQ-V1-23 | Crust Lattice | Environmental Survival (no gate) | Read floor crust patterns over brine, choosing lighter trio formation. | 1500 AC; rare salvage. | UG Stratum 3 | 1.2 | Underground, Brine Hound, Floor Crust |
| PQ-V1-24 | Clone Cage | Combat / Encounter Puzzle | Identify real Echo clone by behavior and sync or defeat only the false copies. | 450 AC; trust bump if no permanent loss. | ML-V1-C1 | 1.0 | EchoGenerator [SHIPPED], Echo sync permanent loss |
| PQ-V1-25 | Sulfur Hound Windline | Combat / Encounter Puzzle | Use wind, Brimstone Fan cover, and scent breaks to split a Sulfur Hound pack. | 500 AC; **Sulfur-Bite Machete**. | SQ-V1-07 | 1.1 | Sulfur Hound [SHIPPED], B1 encounter table [SHIPPED] |
| PQ-V1-26 | Geyser Strider Wake | Combat / Encounter Puzzle | Fight only during safe vent gaps; careless shots wake Geyser Strider charge lanes. | 900 AC; **Vent-Step Boots**. | SQ-V1-14 | 1.2 | B2, Geyser Strider, combat [SHIPPED] |
| PQ-V1-27 | Moth Silence Hall | Combat / Encounter Puzzle | Cross a hall where killing Silence Moths calls android reinforcements. | 950 AC; stealth loot. | SQ-V1-13 | 1.2 | B3/B7, Silence Moth, Salvage Excavator Android |
| PQ-V1-28 | Caldera Mantis Heat Read | Combat / Encounter Puzzle | Redirect heat vents to stun, not burn, Caldera Mantis. | 1800 AC; **Mantis Heat Talon**. | SQ-V1-28 | 1.3 | B4, Caldera Mantis |
| PQ-V1-29 | Patrol Corruption Loop | Combat / Encounter Puzzle | Rewire Corrupted Patrol Android priorities while fighting spawned decoys. | 2600 AC; **Compliance Arc Rifle**. | SQ-V1-38 | 1.3 | B7, Corrupted Patrol Android, Helix |
| PQ-V1-30 | Shelf Wrong Song | Story / Reveal | Tune Resonance Echo Shelf to match Kairos's old crew call without causing Saturation drift. | Lore: crew did not all agree; 850 AC. | ML-V1-C3 | 1.1 | B3, Resonance Echo Shelf, Kairos [PLANNED] |
| PQ-V1-31 | Frequency Mirror | Story / Reveal | Mirror Kairos tone through Echo Lichen and compare the returned voice. | Lore: Io may be answering through biology; 1200 AC. | UG / Symbiosis | 1.2 | Underground, Echo Lichen, Echo Symbiont Swarm |
| PQ-V1-32 | Smuggler Remnant Cipher | Story / Reveal | Decode Smuggler Remnant Android memory without letting Helix auto-redact it. | Lore: cores were moved through B5; 1600 AC. | SQ-V1-25 | 1.2 | B5, Smuggler Remnant Android, Helix / Seam |
| PQ-V1-33 | Rim Watcher Reflection | Story / Reveal | Angle caldera glass to reveal what the crew saw before the death-site event. | Lore: Kairos contained but did not initiate everything; 1800 AC. | ML-V1-C5 | 1.3 | B4, Rim Glass Needle Mat |
| PQ-V1-34 | Names In The Shelf | Story / Reveal | Play Echo chronicle fragments in erased-crew order. | Lore: crew manifest names; trust dialogue. | SQ-V1-33 | 1.3 | B7, Echo chronicle [SHIPPED] |
| PQ-V1-35 | Still Mark Counting | Story / Reveal | Track three Still Hunter signs without looking directly at the final mark. | Lore: Still Hunter is a defense myth made flesh; 2200 AC. | SQ-V1-37 | 1.4 | B7, Still Hunter myth |
| PQ-V1-36 | Barrier Cascade Mastery | Optional Mastery / Challenge | Disable 3 linked Ash Barrier Fields under time pressure after story route is already open. | Prestige 1500 AC; **Barrier Harmonic Charm**. | B3 optional POIs | 1.4 | Ash Barrier Field, no story gate |
| PQ-V1-37 | Gas Dome Beta Mastery | Optional Mastery / Challenge | Dual vent an underground gas dome while protecting an Echo trio from Saturation spikes. | Prestige 1800 AC; cleanse discount. | UG Stratum 4 | 1.3 | Underground, Echo Symbiont Swarm, no story gate |
| PQ-V1-38 | Plume Moth Constellation | Optional Mastery / Challenge | Follow Plume Moth light paths across B2 and B7 variants without map markers. | Prestige 1200 AC; cosmetic signal trail. | Global Plume Moth | 1.2 | Plume Moth, no story gate |
| PQ-V1-39 | Void Stitcher Trace | Optional Mastery / Challenge | Solve cross-biome trace clues without fighting; resets if player overuses radio pings. | Prestige 3000 AC; Void Stitcher codex. | Global rumor | 1.5 | Void Stitcher, Communications [PLANNED], no story gate |
| PQ-V1-40 | Lattice Clean Run | Optional Mastery / Challenge | Complete final vault puzzle sequence with no companion injury, no Echo loss, and no forced awaken flags. | Prestige 3000 AC; title flag. | Ending triad | 1.6 | B7, mastery, no story gate |

**Puzzle quest subtotal:** ~43h.

---

## 7. Factions (V1 Names)

Reputation ladder for all factions: **Unknown -> Noticed -> Trusted -> Inner Circle -> Legend**. Faction content can be board/NPC-driven and may alter gear availability, side quest variants, and ending emphasis. It does not replace the trigger-driven mainline.

| Faction | Role | Gear | Skills / Perks | Quest Band | Conflicts |
|---|---|---|---|---|---|
| **Ashwatch Cordons** | Base defense, patrol doctrine, storm shelter enforcement. | Ashwatch Riot Plate, Cordon Shock Baton, Cordon Hotbar Flare, Stalker-Bore Rifle. | **Perimeter Doctrine:** better detection against androids and hidden stalkers in cordon zones. | SQ-V1-01, 03, 07, 16, 24, 27, 35. | Opposes Seam Runners smuggling; distrusts Whisper Clinic Echo mercy; can push Awaken ending. |
| **Isotope Choir** | Science wing, ecology samples, memory truth, radiation analysis. | Choir Scanner Lens, Cliff Tube Lace Analyzer, Void-Kelp Rad Wrap, Shelf Harmonic Tuner. | **Spectral Literacy:** faster scans on sealed shelves, fields, lens events, and Echo Lichen. | SQ-V1-04, 08, 10, 15, 21, 28, 33, 39. | Clashes with Helix over redacted data; worries Ashwatch will militarize Kairos. |
| **Scrim Salvage Compact** | Salvage crews, graveyard rights, field repairs, black-market parts. | Scrim Mag-Cutter, Scrim Clamp Hammer, Mag-Clamp Sidearm Rail, Vault Petal Knife. | **Wreck Whisper:** bonus salvage yield from Expedition Graveyard, rigs, android frames. | SQ-V1-05, 12, 18, 26, 29, 34. | Price war with Basalt Exchange; trespass conflict with Helix claims; unsafe digs anger Ashwatch. |
| **Whisper Clinic** | Medical, Echo stabilization, Purification Hub ethics. | Whisper Triage Injector, Soft Sync Collar Mk I, Brimstone Puff Charm, Still Mark Charm. | **Soft Sync:** small sync chance boost before the roll; failed hostile sync remains permanent loss. | SQ-V1-06, 09, 19, 32, 37. | Opposes Ashwatch destruction of hostile Echoes; distrusts Helix experiments. |
| **Basalt Exchange** | Logistics, AC trade boards, delivery schedules, base market. | Exchange Load Frame, Signal Tagger Hotbar Mod, Spout-Timer Hotbar Chip. | **Storm Ledger:** reduced AC losses on storm-delayed deliveries and production stalls. | SQ-V1-02, 14, 17, 22, 30. | Under cut by Seam Runners; disputes salvage ownership with Scrim. |
| **Helix Remnant Envoys** | External corporate remnant claiming prior leases and data rights. | Black Directive Chip, Compliance Arc Rifle, Seizure Drone Token, Helix Compliance Armor. | **Directive Override:** open one corporate seal per expedition or force one android compliance check. | SQ-V1-25, 38 plus PQ-V1-32. | Opposes Choir publication, Seam smuggling, and Scrim salvage rights; can bias Awaken/Seal through control rhetoric. |
| **Seam Runners** | External smugglers, silent routes, contraband cores, unofficial rescues. | Seam Mute Boots, Seam Cloak, Kite-Shadow Cloak, Seam Runner Legend Kit. | **Seam Step:** reduced noise footprint in Silence Mandate zones and better route marking. | SQ-V1-13, 20, 23, 31, 36, 40. | Hunted by Ashwatch and Helix; sometimes helps Clinic move Echo patients. |

### Faction Conflict Web

| Conflict | Content Expression | Possible Outcome |
|---|---|---|
| Ashwatch vs Seam Runners | Silent routes, contraband cores, perimeter breaches. | Ashwatch tightens base security or Seam opens safer B7 stealth routes. |
| Choir vs Helix | Sample ownership, redacted memory logs, corporate lease claims. | Publish truth for trust/Symbiosis bias or seal data for control/Seal bias. |
| Scrim vs Basalt Exchange | Salvage ownership vs AC logistics. | Cheaper gear through Scrim or steadier vendor stock through Exchange. |
| Whisper Clinic vs Ashwatch | Echo mercy vs permanent containment/destruction. | More sync chances before roll or safer base cordons after failed syncs. |
| Helix vs Seam Runners | Corporate compliance vs smuggled evidence. | Directive seals open cleanly or black routes bypass them with risk. |

---

## 8. Friction Systems + Gate Matrix

### Friction Systems

| ID | System | What It Does | Status Hook | Primary Content Use |
|---|---|---|---|---|
| F1 | **Ash Barrier Field** | Precursor fields block, bend, or punish movement until tuned, synchronized with weather, bypassed by faction tools, or mastered optionally. | WorldState [SHIPPED] + Kairos [PLANNED] | Mainline repairs, B3/B7 gates, optional mastery, faction bypasses. |
| F2 | **Silence Mandate** | Loud combat, killing Silence Moths, or overusing radio pings can call androids, stalkers, or route failures. | Communications [PLANNED], combat [SHIPPED] | B3 stealth, B7 galleries, Seam Runners, Still Hunter traces. |
| F3 | **Resonance Echo Shelf** | Wrong frequencies cause Saturation drift, false dialogue, or Echo chronicle distortion. | EchoGenerator [SHIPPED], Resonance [PLANNED] | C3, B7, story/reveal puzzles. |
| F4 | **Sulfur Storm Window** | Storms pause outdoor operations, shape harvest windows, and force shelter timing. | Weather stubs [PARTIAL], exposure [SHIPPED] | B1 mainline, base injury-not-death, Building Control shelter calls. |
| F5 | **Echo Hostile Sync Gamble** | Sync rolls can preserve hostile Echoes, but failure permanently loses that Echo. | EchoGenerator [SHIPPED] | Whisper Clinic, mainline trust, Echo side quests. |
| F6 | **Cold / Heat Gear Pressure** | Single thermal meter with two poles pressures B5 cold/radiation and B4 heat. | Exposure [SHIPPED] | C4/C5, environmental puzzles, late mid gear economy. |
| F7 | **Gas Dome Drain** | Underground gas pockets drain O2 until vented, routed, or bypassed. | Underground [PLANNED], exposure [SHIPPED] | Underground Stratum 2-4, Science/Architect utility. |
| F8 | **Floor Crust / Brine Fall** | Thin crust and brine timing punish careless movement and heavy trio choices. | Underground [PLANNED] | Loot, survival puzzles, Brine Hound routes. |
| F9 | **Saturation / Strain Pressure** | Long expeditions, bad resonance, and failed choices stress skilled Echoes and workers. | Purification Hub [PLANNED] | Whisper Clinic, Symbiosis, mastery puzzles. |
| F10 | **Still Hunter Trace** | Flee-tag elite pressure that escalates through signs, silence, and direct pursuit near B7. | B7 prefabs [PLANNED] | Endgame horror, optional traces, final choice pressure. |

### Gate Matrix

| Gate / Friction | Mainline | Side Quests | Puzzle Quests | Factions | Ecology / Systems Tags |
|---|---|---|---|---|---|
| Ash Barrier Field | ML-V1-03, 04, 14, 15 | SQ-V1-20 | PQ-V1-01, 02, 07, 36 | Seam bypass, Choir analysis, Ashwatch control | F1, Kairos [PLANNED], WorldState [SHIPPED] |
| Sulfur Storm Window | ML-V1-06, C1 | SQ-V1-08, 09 | PQ-V1-03, 17 | Ashwatch shelter, Exchange delivery | F4, weather stubs [PARTIAL], Sulfur Hound [SHIPPED] |
| Echo Sync Permanent Loss | ML-V1-08, C1, C6 | SQ-V1-19, 37 | PQ-V1-24, 37, 40 | Whisper Clinic ethics | F5, EchoGenerator [SHIPPED], Purification Hub [PLANNED] |
| Silence Mandate | ML-V1-11, 14 | SQ-V1-13, 20, 23, 27, 31, 36, 40 | PQ-V1-12, 21, 27 | Seam Runners, Ashwatch | F2, Silence Moth, Salvage Excavator Android |
| Resonance Echo Shelf | ML-V1-C3, C6 | SQ-V1-33 | PQ-V1-30, 31, 34 | Choir, Kairos trust | F3, Echo chronicle [SHIPPED] |
| Cold / Heat Gear Pressure | ML-V1-12, C4, 13, C5 | SQ-V1-21, 24, 28, 30, 32 | PQ-V1-05, 06, 19, 28 | Choir, Exchange, Ashwatch | F6, exposure [SHIPPED] |
| Underground Gas / Crust | Optional depth route | SQ-V1-04 | PQ-V1-22, 23, 37 | Choir, Scrim, Clinic | F7, F8, Underground [PLANNED] |
| Base Injury-Not-Death | ML-V1-02, C5 | SQ-V1-03, 09, 24, 32 | PQ-V1-17, 40 | Ashwatch, Clinic, Exchange | Building Control [SHIPPED/PARTIAL Craft], base modules [PLANNED] |
| Still Hunter Trace | ML-V1-14, C6, 15 | SQ-V1-37, 40 | PQ-V1-35, 39, 40 | Seam Runners, Clinic | F10, Still Hunter myth, B7 [PLANNED] |
| AC Economy Pressure | All acts | All SQ rows | All PQ payoff rows | Basalt Exchange | AC economy [SHIPPED] |

---

## 9. Hour Budget

Designed hours exclude pure exploration, free-form harvesting, collectible wandering, photo/ambience time, and non-objective base decoration.

| Bucket | Target Hours | Notes |
|---|---:|---|
| Mainline trigger-driven campaign | 30h | Prologue, repairs, 6 Memory Cores, Resonance Events, ending triad. |
| Side quests (40) | 50h | Average 1.25h; includes faction, ecology, companion, and board content. |
| Puzzle quests (40) | 43h | Mixed purposes; only 8 are access gates. |
| Faction overhead / reputation arcs | 10h | Vendor unlocks, rep thresholds, conflict resolution, board refresh, finale state checks. |
| Systemic friction loops | 32h | Storm rotations, Echo sync risk, Barrier retunes, Purification cadence, Still Hunter traces, repeat contracts. |
| **Designed total** | **165h** | Meets >=150h requirement without exploration. |
| Exploration bonus (excluded) | ~25-40h | Open traversal, hunting, gathering, ambience, vistas, non-objective ecology observation. |

### Systemic Friction Hour Detail

| Loop | Hours | Content Notes |
|---|---:|---|
| Sulfur storm harvest and shelter rotations | 7h | B1/B6/B3 production stalls and shelter drills. |
| Echo signal sync/fail chronicle loops | 6h | Hostile Echo rescue, permanent loss stakes, Clinic mitigation. |
| Ash Barrier Field retunes | 5h | Optional POIs, faction bypasses, post-Resonance instability. |
| Purification Hub Saturation cadence | 5h | Long expedition recovery, Symbiosis pressure, Clinic boards. |
| Weather / exposure survival repeats | 4h | B5 lens fields, B4 heat surges, underground gas. |
| Still Hunter / Void Stitcher traces | 3h | Endgame horror escalation and optional mastery clues. |
| Faction repeat contracts | 2h | Short AC earners and reputation smoothing. |

---

## 10. Sample Dialogue / Trust Ladder

### Trust Ladder Mechanics

| Trust Level | Range | Unlock / Behavior | Risk |
|---|---:|---|---|
| 0. Hostile | 0-9 | Kairos speaks rarely, mostly warnings and accusations. | More false-positive signal pings. |
| 1. Suspicious | 10-24 | Reacts to C1 and B1 survival; identifies obvious traps. | Withholds motive. |
| 2. Tactical | 25-44 | Offers biome pressure warnings and enemy behavior hints. | Advice may be coldly utilitarian. |
| 3. Advisor | 45-64 | Provides Barrier Field reads, storm warnings, and Memory Core context. | Pushes containment logic. |
| 4. Regretful | 65-84 | Shares crew fragments and admits failed containment. | Dialogue becomes emotionally unreliable. |
| 5. Friend / Choice Partner | 85-100 | Final vault dialogue opens; ending nuance improves. | The player can hurt it knowingly. |

### Trust Inputs

| Input | Trust Effect |
|---|---:|
| Attach Memory Core | +8 to +12 depending on recovery choices. |
| Preserve hostile Echo without sync failure | +4. |
| Failed hostile Echo sync | -6 and permanent Echo loss. |
| Resolve faction conflict without exploitation | +3. |
| Force corporate/Helix override on precursor seal | -4. |
| Complete Resonance Event with no base companion injury | +5. |
| Choose repeated dominance options | Lowers Symbiosis bias, raises Awaken bias. |
| Use Purification Hub for recovery instead of ignoring Saturation | +2. |

### Dialogue Samples

| Context | Speaker | Line |
|---|---|---|
| First shell contact | Kairos | "Signal contamination detected. Human pattern. Crew pattern. Error: crew pattern dead." |
| B6 Beacon Hopper reveal | Companion | "That ping was not a radio. It was an animal learning our fear." |
| Kairos awakening hostile | Kairos | "You wear their faces. Leave the cores. Leave the moon." |
| Failed hostile Echo sync | Kairos | "Do not ask where it went. You pulled. Io pulled harder." |
| Base damaged during heat bleed | Building Control | "Command Center rooms sealed. Injuries logged. Fatality count: locked at zero by shelter protocol." |
| C3 advisor shift | Kairos | "Ash ridges are singing again. Lower your weapons. Noise is a mouth here." |
| First Void Stitcher rumor | Seam Runner | "If the seam moves after you stop looking, do not run. Running teaches it distance." |
| B5 smuggler truth | Kairos | "They carried pieces of me under their coats. I called them thieves. Some were only trying to save a voice." |
| B4 crew death-site | Companion | "No bodies. No blood. Just evacuation lights pointed at the lava." |
| C5 friend threshold | Kairos | "Kade, if you seal me, I will still listen. If you awaken me, I may not remain kind." |
| Still Hunter sign | Kairos | "Do not count the third mark aloud." |
| Seal ending | Kairos | "A cage is still a shape of mercy. Close it." |
| Awaken ending | Kairos | "Then give me the moon's teeth. I remember where to place them." |
| Symbiosis ending | Kairos | "Your pulse is not a command. Good. Let Io hear something smaller than hunger." |

### Companion Flavor Lines

| Companion Role | Sample |
|---|---|
| Architect Engineer | "That Barrier Field is not locked. It is bracing, like it expects impact." |
| Science Specialist | "Void Kelp is avoiding the Echoes, not the radiation. That is worse." |
| Combat Tactician | "Sulfur Hound pack is splitting by scent. Do not bleed. Do not reload loud." |
| Infiltrator Scout | "Silence Moths ahead. We go quiet or we bring every dead machine in the ridge." |
| Med Tech | "Saturation spike. I can calm them before the sync, but if the roll fails, there is no patient left." |
| Logistics Officer | "AC loss is cheaper than a bad storm run. Shelter now, spend later." |
| Salvage Engineer | "Scrapper Drone is still following a manifest. Let it open the crate for us." |
| Communications Officer | "That signal has our encryption and no sender. I hate that more than static." |

---

## Production Summary

**V1 Ash & Signal** is a 165-hour designed narrative package built around progress-triggered mainline advancement, mixed-purpose puzzle content, AC-only reward scaling, ecology-forward horror, and a trust arc with Kairos as a precursor defense AI. The player moves through **B6 -> B1 -> B2 -> B3 -> B5 -> B4 -> B7**, repairs Kairos, recovers 6 Memory Cores, survives Resonance Events, and chooses **Seal**, **Awaken**, or **Symbiosis**.

The package assumes shipped systems where they exist, marks partial/planned dependencies clearly, and keeps all future hooks compatible with the locked canon: no Echo marketplace, no base companion death from building damage, 22 base companions plus trio expeditions, and AC as the sole economy.
