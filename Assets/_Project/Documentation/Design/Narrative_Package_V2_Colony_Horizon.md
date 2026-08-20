# Narrative Package V2 - Colony Horizon

**Project:** Dark Matter: Genesis  
**Package identity:** **V2 Colony Horizon**  
**Player:** **Kade**  
**Tone:** Hope / colony-builder / competent survival / earned legacy  
**Designed hour target:** **170 hours** excluding exploration, free roam, harvesting drift, build decoration, and post-ending sandbox  
**Canon locks:** Io 2160, Aether Credits only, starter **5000 AC**, 22 base companions + expedition trio, Kairos repair -> 6 Memory Cores -> trust, biome order **B6 -> B1 -> B2 -> B3 -> B5 -> B4 -> B7**, Echo fail permanent, base injury-not-death.

---

## 1. Premise, Tone, And Kairos Truth

**Kade** does not come to Io to win a war against the moon. Kade comes to prove that people can build a future where every previous expedition failed.

The first colony horizon is a fragile line: a pressure shell, a storm shelter, a Building Control Panel that barely boots, and a roster of survivors who need reasons to keep working. The fantasy is not domination. It is **construction under schedule pressure**: raising habs before sulfur storms, routing power through colony Barrier Fields, restoring production queues, rescuing Echoes before sync collapse, and watching a hostile world become legible one building at a time.

**Kairos is not an enemy AI.** It is a lost expedition archive. Its Memory Cores are not loot containers or superweapons; they are people, crew imprints, field journals, unfinished promises, and names preserved badly by a machine that has been alone too long. Repairing Kairos wakes a grieving witness. Restoring all six cores gives the colony enough truth to choose what kind of future it deserves.

**Endings:**

- **Build:** Ratify a colony charter and remain on Io. Kairos becomes the Colony Voice, an advisor bound by public trust.
- **Evacuate:** Leave with the restored cores and names. Io stays dangerous, but the expedition stops being erased.
- **Seed:** Plant a hybrid Echo-colony legacy. The colony accepts ongoing Resonance/Saturation costs to create something neither Earth nor old Io intended.

**Hopeful dialogue rule:** Even fear lines should point toward agency: shelter, repair, witness, vote, seed, carry names forward.

---

## 2. Biome And Ecology Integration Map

### 2.1 Biome Progression

| Code | Biome | Role In V2 Colony Horizon | Ecology And Encounter Use | Content Status |
|---|---|---|---|---|
| B6 | **Basalt Highlands** | Hub, landing zone, first BCP shell, first colony ring, companion base. | Cliff Tube Lace, Tube Jackal, Brood Tunnel Mouth, Glass Hive Swarmer, Cave Scout Moth, Beacon Hopper; Brimstone Puff pet migration hook. | [PLANNED] biome, [SHIPPED] BCP shell hooks |
| B1 | **Sulfur Plains** | First storm shelter pressure, Core 1, proof that base injury is not death. | Sulfur Hound [SHIPPED], Brimstone Fan, Cinder Skitter, Brimstone Leech, Graveyard Scrapper Drone, sulfur storm behavior, B1 ecology table [SHIPPED]. | [SHIPPED] Sulfur Hound + B1 table, [PARTIAL] weather stubs |
| B2 | **Geyser Fields** | Greenhouse repairs, vent timing, C2 fertility systems. | Vent Crab Worker/Queen, Plume Moth, Geyser Pod, Geyser Strider, Vent Capper Bot, Rusted Survey Drone. | [PLANNED] biome, [PARTIAL] hovercraft traversal |
| B3 | **Ash Flats & Ridges** | Waystations, beacons, Barrier Field routing, caravan weather schedules. | Basalt Jackal, Dust Spout Cluster, Ash Stalker, Ash Glass Wasp, Salvage Excavator Android, Claim Jumpers (H6). | [PLANNED] biome |
| B5 | **Polar Radiation Flats** | Radiation/cold survival, Core 4, Void ecology, Freeboot Relief arrival. | Void Kelp, Magnet Wyrm, Rift Stalker, Cold Spire Hound, Smuggler Remnant Android, Mag-Clamp Drone. | [PLANNED] biome, [SHIPPED] exposure base |
| B4 | **Lava Calderas** | Heat survival, memorial forge, Core 5, geothermal modules. | Caldera Mantis, Heat Eel, Magma Skitter, Caldera Heat Kite, Eruption Sentry Bot, Rim Glass Needle Mat. | [PLANNED] biome |
| B7 | **Precursor Ruin Belt** | Final archive, C6 charter vault, ending triad. | Vault Stalker, Silence Moth, Still Hunter (myth), Resonance Echo Shelf, Rust Garden, Corrupted Patrol Android, Void Stitcher (global). | [PLANNED] biome |

### 2.2 Underground Strata

| Stratum | Name | Use | Ecology |
|---|---|---|---|
| UG Stratum 1 | Upper Lava Tubes | First cellar routes and BCP utility runs. | Tube Lace, Tube Jackal, Cinder Tunnel Skitter, Rust Garden. |
| UG Stratum 2 | Mid Galleries | Gas dome and greenhouse support puzzles. | Glass Kelp, Rift Skimmer, Glass Hive Swarmer, Brood Tunnel Warden. |
| UG Stratum 3 | Deep Volatile Basins | Risk/reward farms and survival puzzles with no hard gate. | Brine Fan, Basin Mantis, Brine Hound, Lamprey Spire Colony. |
| UG Stratum 4 | Geothermal Roots | Combat setpieces and heat loot. | Heat Eel, Magma Phase Crawler, Silicate Mirror Bloom. |
| UG Stratum 5 | Resonance Vaults | B7 precursor support route and Seed ending prep. | Echo Lichen, Vault Stalker, Echo Symbiont Swarm, Corrupted Patrol Android. |

### 2.3 Required Organism Roster

Use the real ecology names in quests, puzzles, combat tables, life sheets, and codex text:

- **Shipped ecology:** Sulfur Hound [SHIPPED].
- **Surface fauna:** Vent Crabs, Plume Moth, Basalt Jackal, Dust Spout, Caldera Mantis, Heat Eel, Void Kelp, Magnet Wyrm, Rift Stalker.
- **Underground fauna:** Tube Jackal, Brood Tunnel, Glass Hive, Vault Stalker, Silence Moth, Still Hunter, Void Stitcher, Rust Garden.
- **Synthetic families:** androids **A1-A10**, humanoids **H1-H7**, machines **M1-M7**, flyers **F1-F8**.
- **Pets:** Brimstone Puff + core12 pet roster [PLANNED], folded into the Echo/trio framework as migration work.

### 2.4 Life Sheet References

Durable art/design references (read-only — never delete/move):

| Sheet | Path |
|-------|------|
| Manifest | `Assets/_Project/Documentation/Design/Io_Biome_Life_Sheet_Manifest.md` |
| Plan | `Assets/_Project/Documentation/Design/Io_Biome_Life_Image_Sheet_Plan.md` |
| Set A PBR | `Assets/_Project/Documentation/Design/ArtReference/LifeSheets/` |
| Set B RT | `Assets/_Project/Documentation/Design/ArtReference/LifeSheets_RayTraced/` (`RT_LifeSheet_B1_B2_Sulfur_Geyser.png`, `…B3_B4_Ash_Caldera.png`, `…B5_B6_Polar_Highlands.png`, `…B7_Ruins_Global.png`, underground + threats + pets sheets) |

V2 uses them as ecology vocabulary and encounter composition references.

---

## 3. Systems Inventory Hooked By Content

| System | Status | V2 Content Hook |
|---|---|---|
| Aether Credits (AC) | [SHIPPED] | Starter **5000 AC**; all quest, puzzle, vendor, upgrade, and faction rewards use AC only. |
| Building Control Panel shell | [SHIPPED] | Every major colony structure has an in-world E terminal with Overview, Companions, Production, Craft, Changes tabs. |
| BCP Craft / Changes tabs | [PARTIAL] | V2 quests teach production queues, module changes, and repair schedules through BCP tasks. |
| EchoGenerator / chronicle | [SHIPPED] | Echo names, core memories, trust dialogue, and permanent fail stakes. |
| Exposure | [SHIPPED] | Cold/heat, radiation, sulfur storms, shelter timing, B5/B4 survival planning. |
| Sulfur Hound + B1 table | [SHIPPED] | First reliable predator table; used in B1 storm escort and shelter perimeter encounters. |
| World Engine spine | [SHIPPED] | World -> Simulation -> Intelligence -> Experience -> Presentation -> Player supports triggered mainline beats. |
| Combat / hotbar | [SHIPPED] | Named weapons map to current melee/ranged/hotbar behaviors where possible. |
| Weather stubs | [PARTIAL] | V2 treats weather as schedule: storm building, peak, dissipating, clear windows. |
| Hovercraft | [PARTIAL] | Used as later logistics/traversal fiction, not required for Act I gates. |
| Quest scaffold | [PARTIAL] | Mainline advances via progress triggers; sides/factions can use NPCs and boards. |
| Pets legacy | [PARTIAL] | Brimstone Puff and core12 migrate into Echo/trio companion design. |
| Kairos / cores / Resonance | [PLANNED] | Six Memory Cores restore people and trigger settlement-scale events. |
| Communications | [PLANNED] | Beacon Homestead, Scout Relay Mesh, and later colony scheduling. |
| Purification Hub live | [PLANNED] | Saturation cleansing, Hearth Clinic, Seed ending cost management. |
| Biomes | [PLANNED] | V2 content assumes biome implementation in locked order. |
| Underground | [PLANNED] | UG Strata 1-5 support farms, caches, combat hives, and B7 archive routes. |
| Modules | [PLANNED] | Generators, power grids, automated gather/logistics, communications, defense, mining. |
| Pet migration | [PLANNED] | Fold pets into Echo/trio roster instead of separate pet loop. |
| Weather Director | [PLANNED] | Required to make weather windows, storm shelter mandates, and scheduled colony fantasy live. |

---

## 4. AC, Weapons, And Upgrade Curve

### 4.1 AC Scale Curve

The economy stays AC-only. Rewards climb from early hundreds to late thousands so upgrades feel meaningful without replacing scavenging, crafting, or BCP production.

| Tier | Coverage | Mainline AC | Side AC | Puzzle AC | Notes |
|---|---|---:|---:|---:|---|
| T1 | Act I, B6-B1 | 200-450 | 250-500 | 150-450 | Establish shelter, BCP basics, starter tool upgrades. |
| T2 | Act II, B2-B3 | 450-900 | 500-950 | 350-900 | Greenhouse, beacons, caravan logistics, first named weapons. |
| T3 | Act III, B5-B4 | 900-1800 | 1000-2000 | 800-1800 | Radiation/heat survival, advanced kits, faction conflicts. |
| T4 | Act IV, B7/finales | 1800-3500 | 2000-4500 | 1500-3000 | Charter, endings, prestige gear, colony-scale choices. |
| Mastery | Optional puzzle mastery | N/A | N/A | 600-3000 | Prestige AC, cosmetics, weapon kits, and mastery tags. |

### 4.2 Named Weapons And Upgrade Kits

**Existing combat mapping:** map named rewards to current combat/hotbar surfaces first: melee weapon stats, projectile/hotbar item data, stamina/tension costs, and inventory equip slots.  
**Future combat mapping:** later systems can add unique animations, elemental behaviors, companion combo perks, and faction mod sockets without renaming the rewards.

| Reward | Tier | Mapping | Source Examples |
|---|---|---|---|
| **Horizon Shield** | T1 | Existing block/parry or defensive equip mapping; future Guard rally aura. | Horizon Guard SQs |
| **Shelter Flare** | T1 | Hotbar consumable; future shelter path ping. | B1 storm quests |
| **Lens Spade** | T1-T2 | Tool/harvest weapon; future agri node bonus. | Green Lens |
| **Rivet Gun** | T1-T2 | Ranged/hotbar projectile mapping; future construction stagger. | Rebuild Guild |
| **Beacon Rifle** | T2 | Existing ranged mapping; future waypoint tagging. | B3 Guard |
| **Crane-Link** | T2 | Utility weapon/tool; future heavy object manipulation. | Rebuild Guild |
| **Hearth Injector** | T2 | Hotbar support item; future companion triage perk. | Hearth Clinic |
| **Mute Cart Rig** | T2-T3 | Stealth logistics tool; future convoy noise suppression. | Freeboot Relief |
| **Radglass Carbine** | T3 | Ranged weapon; future radiation venting mod. | B5 puzzle/cache |
| **Geothermal Maul** | T3 | Heavy melee mapping; future heat-charge swings. | B4 memorial forge |
| **Vault-Cut Spear** | T4 | Melee reach weapon; future Precursor armor break. | B7 vault |
| **Charter Sidearm** | T4 | Ranged prestige weapon; future ending-based mod. | C6/ending |
| **Upgrade Kit: Field Mk I** | T1 | +baseline damage/durability. | Early sides/puzzles |
| **Upgrade Kit: Shelter Mk II** | T2 | +survival utility, storm resistance, or reload safety. | B1-B3 |
| **Upgrade Kit: Resonance Mk III** | T3 | +core-linked effects, exposure tolerance. | B5-B4 |
| **Upgrade Kit: Horizon Mk IV** | T4 | +prestige stat package and ending visual. | B7/finales |

---

## 5. Mainline Progress-Triggered Beat Sheet

Mainline beats advance through **progress triggers**, not quest givers. Triggers include locations reached, repairs completed, cores restored, biomes stabilized, buildings online, Echo milestones, and Resonance Events. NPCs can comment, but they do not hand out the spine.

| Beat | Act | Trigger | Biome | Content | AC | Weapon / Kit | Core / Resonance | Trust Dialogue |
|---|---|---|---|---|---:|---|---|---|
| ML-01 First Horizon | I | Player reaches B6 landing ridge and activates first BCP Overview. | B6 | Establish hab shell, starter 5000 AC noted, assign first base worker. | 200 | Field Mk I kit | None | "One panel lit. That is how a horizon starts." |
| ML-02 Shelter Law | I | Command Center storm shelter comes online. | B6 -> B1 | Storm shelter tutorial, base injury-not-death rule, B1 access. | 250 | Shelter Flare | None | "People bend in storms. Buildings must bend first." |
| ML-03 Coupler Repair | I | Habitat Power Coupler installed through BCP Changes. | B6 | Repair object 1; Horizon Barrier pattern introduced. | 300 | Rivet Gun | Kairos stirs | "Do not build on their graves." |
| ML-04 Gyro Recovery | I | Storm Shelter Gyro recovered during B1 weather window. | B1 | Repair object 2; Sulfur Hound pressure around shelter. | 350 | Horizon Shield | Kairos hostile night | "You came back before the peak. That matters." |
| ML-05 Warm-Boot Prism | I | Archive Warm-Boot Prism installed after B2 vent sync. | B2 | Repair object 3; Kairos speaks clearly. | 450 | Lens Spade | Archive repair complete | "They were going to stay. Why did you come to finish dying?" |
| ML-06 Core 1: Founders' Roll | I | First Memory Core recovered from B1 roll vault. | B1 | Save workers, read names, allied Echo phantoms aid harvest. | 450 | Shelter Mk II kit | C1 Resonance Beacon | "Read the roll aloud." |
| ML-07 Greenhouse Schema | II | Greenhouse module online in B2 Production tab. | B2 | Fertility Mist event, crop trial, vent farming. | 650 | Nutrient Scanner | C2 Greenhouse Schema | "They mapped life onto poison." |
| ML-08 Beacon Homestead | II | Three B3 waystations powered. | B3 | Scout Relay Mesh, caravan schedule, ash ridge map clarity. | 800 | Beacon Rifle | C3 Waystation Lights | "Homes need lights, not silence." |
| ML-09 Advisor Trust | II | Three cores/major Echo milestones restored. | B3 | Kairos shifts from antagonist to advisor. | 900 | Field Mk II kit | Trust Stage: Advisor | "Ash gale building. Move harvest inside before it has a cost." |
| ML-10 Cold Promise | III | B5 habitat uplift plus rad suit installed. | B5 | Polar camp recovery, Freeboot Relief arrival, Void Kelp survey. | 1200 | Radglass Carbine | C4 Warm Bubble | "Smugglers stole names. Steal them back." |
| ML-11 Hearth Of The Rim | III | B4 geothermal stabilizer attached to colony grid. | B4 | Memorial forge, heat relay, grief-to-industry beat. | 1600 | Geothermal Maul | C5 Stability Bubble | "Mourn, then heat the forges." |
| ML-12 Charter Vault | IV | B7 Barrier Field opened by restored power grid. | B7 | Three locks, precursor archive, C6 recovery. | 2200 | Vault-Cut Spear | C6 Charter Vault | "Write the next charter, or leave with the names." |
| ML-13 Charter Vote | IV | Six cores restored and BCP colony Overview reaches quorum. | B7 / Base | Charter UI opens: Build / Evacuate / Seed. | 3000 | Charter Sidearm | Final Resonance | "Hope is a system. Vote it into shape." |
| ML-14 Build Ending | IV | Player chooses Build, colony defenses and production online. | Base | Ratify permanent colony; Kairos becomes Colony Voice. | 3500 | Horizon Mk IV kit | Build resonance | "Then we stay, and we make staying worthy." |
| ML-15 Evacuate Ending | IV | Player chooses Evacuate, cores secured and launch route clear. | B7 -> B6 | Leave with names, prevent erasure. | 3500 | Charter Mk IV kit | Evacuation archive | "Carry us where the sulfur cannot reach." |
| ML-16 Seed Ending | IV | Player chooses Seed, Purification Hub accepts Saturation cost. | UG5 / B7 | Hybrid Echo-colony planted; ongoing legacy. | 3500 | Resonance Mk IV kit | Seed resonance | "Not ghosts. Not settlers. A third promise." |

**Mainline subtotal:** 32h designed.

---

## 6. Side Quests - 40

Side and faction quests may use NPCs, faction boards, BCP work orders, companion prompts, and colony notices. AC follows the T1-T4 curve. Weapon rewards include named gear or upgrade kits where appropriate.

| ID | Tier | Title | Biome | Giver / Board | Objectives | Ecology / Systems | Reward | Est. |
|---|---|---|---|---|---|---|---|---:|
| SQ-V2-01 | T1 | Wall Raising | B6 | Horizon Guard board | Place 3 cordon posts through BCP Changes and assign workers in Companions tab. | BCP shell [SHIPPED], modules [PLANNED] | 300 AC, Horizon Shield | 1.2h |
| SQ-V2-02 | T1 | Shelter Drill | B1 | Horizon Guard | Move 4 workers to Command Center before storm Peak. | Weather stubs [PARTIAL], Sulfur Hound [SHIPPED] | 350 AC, Shelter Flare x2 | 1.4h |
| SQ-V2-03 | T1 | Friendly Fire Check | B6 | Horizon Guard / Hearth Clinic | Sync a frightened Echo while guards hold fire. | EchoGenerator [SHIPPED], Echo fail permanent | 400 AC, Field Mk I kit | 1.2h |
| SQ-V2-04 | T2 | Ridge Patrol | B3 | Horizon Guard | Mark 5 waystations and repel Basalt Jackal ambushes. | Basalt Jackal [PLANNED], combat [SHIPPED] | 700 AC, Beacon Rifle | 1.3h |
| SQ-V2-05 | T2 | Barrier Power Share | B3 | Horizon Guard | Route BCP Production power into a Horizon Barrier segment. | Barrier Field, BCP Craft/Changes [PARTIAL] | 850 AC, Shelter Mk II kit | 1.3h |
| SQ-V2-06 | T3 | Guard Legend | B4 | Horizon Guard | Escort memorial banners through Caldera Mantis territory. | Caldera Mantis [PLANNED], exposure [SHIPPED] | 1600 AC, Guard Banner mod | 1.8h |
| SQ-V2-07 | T1 | Seed Trial | B2 | Green Lens Collective | Harvest during a Dissipating weather window and seed the first test bed. | Weather-as-schedule, Plume Moth [PLANNED] | 450 AC, Lens Spade | 1.2h |
| SQ-V2-08 | T2 | Nutrient Map | B2 | Green Lens board | Sample 5 vent-fed nodes without overharvesting. | Vent Crabs, Dust Spout [PLANNED] | 650 AC, Nutrient Scanner | 1.1h |
| SQ-V2-09 | T3 | Polar Sprout | B5 | Green Lens | Grow one cold crop under radiation pressure. | Void Kelp, exposure [SHIPPED] | 1400 AC, Seedvault Case | 1.5h |
| SQ-V2-10 | T2 | IP Or Open | B6 | Green Lens / Helix | Choose whether greenhouse schema stays open-source or licensed. | Faction conflict, AC-only economy | 750 AC, rep fork | 1.0h |
| SQ-V2-11 | T2 | Bloom Night | B3 | Green Lens | Harvest a Resonance bloom while Dust Spouts move across the ridge. | Resonance [PLANNED], Dust Spout | 900 AC, Shelter Mk II kit | 1.3h |
| SQ-V2-12 | T4 | Collective Finale | B7 | Green Lens | Add a flora protection clause to the charter vote. | B7 [PLANNED], Charter UI [PLANNED] | 2500 AC, Horizon Mk IV kit | 1.5h |
| SQ-V2-13 | T1 | Hab From Wreck | B1 | Rebuild Guild | Convert a wreck into a hab frame with Production and Changes tabs. | BCP [SHIPPED]/[PARTIAL] | 450 AC, Rivet Gun | 1.3h |
| SQ-V2-14 | T2 | Crane Rescue | B2 | Rebuild Guild | Recover a Crane-Link from a geyser pit. | Vent timing, Heat Eel | 700 AC, Crane-Link | 1.2h |
| SQ-V2-15 | T2 | Scaffold War | B3 | Rebuild Guild | Clear Tube Jackals from disputed scaffold piles without wrecking supplies. | Tube Jackal [PLANNED], combat [SHIPPED] | 800 AC, Hab Patch kit | 1.2h |
| SQ-V2-16 | T3 | Crust Foundation | UG3 | Rebuild Guild | Mark safe brine crust for underground cellar farms. | Magnet Wyrm, UG [PLANNED] | 1300 AC, Resonance Mk III kit | 1.5h |
| SQ-V2-17 | T2 | Materials Court | B6 | Rebuild / Caravan | Mediate access to salvage and common storage. | Quest scaffold [PARTIAL] | 600 AC, rep fork | 0.9h |
| SQ-V2-18 | T3 | Guild Legend | B4 | Rebuild Guild | Build a geothermal memorial forge from a death-site. | Heat Eel, memorial pressure | 1800 AC, Geothermal Maul | 1.7h |
| SQ-V2-19 | T1 | Print A Name | B6 | Hearth Clinic | Reveal an Echo chronicle name and print a colony tag. | Echo chronicle [SHIPPED] | 350 AC, Name Tag Printer | 1.0h |
| SQ-V2-20 | T1 | Ryn's Hab | B6 | Ryn / Hearth | Let Architect Ryn solve a habitability flaw in BCP Overview. | Companion trio, BCP shell | 450 AC, Ryn bond, Field Mk I kit | 1.3h |
| SQ-V2-21 | T1 | Nova's Storm Triage | B1 | Nova / Hearth | Treat injured workers during storm Peak; no base deaths. | Exposure [SHIPPED], base injury-not-death | 500 AC, Hearth Injector | 1.4h |
| SQ-V2-22 | T3 | Calder's Care | B5 | Calder / Hearth | Treat isotope exposure and choose public/private case notes. | Radiation, Void Kelp | 1200 AC, Resonance Mk III kit | 1.3h |
| SQ-V2-23 | T2 | Mira's Waylight | B3 | Mira Storm / Hearth | Light a scout beacon chain without drawing Rift Stalkers. | Rift Stalker [PLANNED] | 850 AC, Beacon charm | 1.2h |
| SQ-V2-24 | T2 | Hub Debt | B6 | Hearth Clinic | Cleanse 4 Saturation cases in Purification Hub prototype flow. | Purification Hub [PLANNED] | 900 AC, Named Sync perk | 1.3h |
| SQ-V2-25 | T1 | Commons Open | B6 | Caravan Commons | Found the shared market and post first AC ledger. | AC [SHIPPED] | 300 AC, Commons Pack | 0.8h |
| SQ-V2-26 | T1 | Storm Schedule | B1 | Caravan Commons | Deliver supplies before Peak or delay safely through shelter. | Weather stubs [PARTIAL] | 450 AC, Schedule Slate | 1.2h |
| SQ-V2-27 | T2 | Vent Caravan | B2 | Caravan Commons | Escort freight across Dust Spout alley. | Dust Spout, Vent Crabs | 650 AC, Shelter Mk II kit | 1.2h |
| SQ-V2-28 | T2 | Ridge Freight | B3 | Caravan Commons | Move freight past humanoid H2 raiders without losing crates. | H1-H3 humanoids [PLANNED] | 850 AC, Shared Manifest perk | 1.3h |
| SQ-V2-29 | T3 | Polar Chain | B5 | Caravan Commons | Maintain a cold chain shipment to Green Lens crops. | Exposure, Void Kelp | 1500 AC, Storm Ledger | 1.2h |
| SQ-V2-30 | T3 | Commons Legend | B4 | Caravan Commons | Escort heat-proof convoy to the memorial forge. | Caldera Mantis, Heat Eel | 1900 AC, vendor stock unlock | 1.6h |
| SQ-V2-31 | T2 | Continuity Offer | B2 | Helix Continuity Mission | Accept, refuse, or restrict Helix funding for greenhouse IP. | External faction conflict | 700 AC, Lease Stylus | 0.8h |
| SQ-V2-32 | T2 | Audit Greenhouse | B2 | Helix | Complete an audit without damaging live crops. | Greenhouse Schema [PLANNED] | 900 AC, Audit Drone | 1.2h |
| SQ-V2-33 | T2 | Echo As Asset | B6 | Helix / Hearth | Decide whether Echo imprints can be classified as property. | Cores-are-people | 950 AC, ending lean | 1.1h |
| SQ-V2-34 | T2 | Grant Gate Trial | B3 | Helix | Fund one upgrade, then handle dependency strings. | BCP Changes [PARTIAL] | 950 AC, Grant Gate perk | 1.3h |
| SQ-V2-35 | T4 | Continuity Ultimatum | B7 | Helix | Add, reject, or rewrite Helix charter terms. | Charter Vote [PLANNED] | 3000 AC, Charter Sidearm mod | 1.0h |
| SQ-V2-36 | T3 | Grey Seeds | B5 | Freeboot Relief | Accept contraband seeds for sick workers. | Void Kelp, Freeboot conflict | 1200 AC, Grey Seed Pouch | 0.8h |
| SQ-V2-37 | T1 | Relief Run | B1 | Freeboot Relief | Smuggle meds through a sulfur storm without Guard panic. | Sulfur Hound [SHIPPED], weather [PARTIAL] | 500 AC, Relief Cloak | 1.3h |
| SQ-V2-38 | T2 | Mute Cart | B3 | Freeboot Relief | Deliver supplies past Guard sensors. | Barrier Field, H2 patrols | 900 AC, Mute Cart Rig | 1.3h |
| SQ-V2-39 | T3 | Barrier Mercy | B3 | Freeboot / Guard | Open Horizon Field briefly for refugees. | Colony Barrier, faction conflict | 1800 AC, Grey Bloom perk | 1.4h |
| SQ-V2-40 | T4 | Freeboot Legend | B7 | Freeboot Relief | Escort a charter witness past Vault Stalkers. | Vault Stalker, B7 | 4500 AC, Vault-Cut Spear | 1.8h |

**Side quest subtotal:** 52h designed.

---

## 7. Puzzle Quests - 40 Mixed Purpose

Required purpose distribution:

- **Access Gate:** 8, a minority.
- **Loot/Cache/Weapon:** 8.
- **Environmental Survival no gate:** 7.
- **Combat/Encounter:** 6.
- **Story/Reveal:** 6.
- **Optional Mastery:** 5.

| ID | Tier | Purpose Type | Title | Biome | Solve Pattern | Ecology / Setpiece | Reward | Est. |
|---|---|---|---|---|---|---|---|---:|
| PQ-V2-01 | T1 | Access Gate | Pad Field | B6 | Route BCP power to a Horizon Barrier field. | Android A1 maintenance units | 250 AC, Field Mk I kit | 0.9h |
| PQ-V2-02 | T1 | Story/Reveal | Coupler Lattice | B6 | Align habitat coupler symbols to expedition initials. | Kairos first grief fragments | 300 AC, Ryn trust | 1.1h |
| PQ-V2-03 | T1 | Environmental Survival no gate | Gyro Storm Lock | B1 | Open wreck only between Peak and Dissipating storm phases. | Sulfur Hound patrol [SHIPPED] | 350 AC, Shelter Flare | 1.1h |
| PQ-V2-04 | T2 | Story/Reveal | Prism Vent Choir | B2 | Sync three geyser tones to wake archive audio. | Plume Moth swarm, Vent Crabs | 650 AC, Lens Spade mod | 1.2h |
| PQ-V2-05 | T1 | Access Gate | Roll Vault | B1 | Hold worker pressure plates while shelter siren cycles. | C1 Founders' Roll | 450 AC, Shelter Mk II kit | 1.2h |
| PQ-V2-06 | T1 | Loot/Cache/Weapon | Phantom Ally Cache | B1 | Guide allied Echo phantoms to buried harvest crates. | EchoGenerator [SHIPPED] | 400 AC, Horizon Shield mod | 1.0h |
| PQ-V2-07 | T2 | Access Gate | Greenhouse Seals | B2 | Balance contamination seals before agri module opens. | Heat Eel vent channels | 750 AC, Nutrient Scanner | 1.1h |
| PQ-V2-08 | T2 | Environmental Survival no gate | Fertility Valves | B2 | Tune valves to avoid crop burn during Fertility Mist. | Plume Moth pollination hazard | 700 AC, seed cache | 1.0h |
| PQ-V2-09 | T2 | Access Gate | Waystation Triad | B3 | Power three beacons in ridge order. | Basalt Jackal pressure | 850 AC, Beacon Rifle | 1.2h |
| PQ-V2-10 | T3 | Environmental Survival no gate | Warm Bubble Calibrator | B5 | Balance cold/heat poles around a temporary habitat bubble. | Void Kelp radiation pockets | 1100 AC, Radglass mod | 1.1h |
| PQ-V2-11 | T3 | Access Gate | Lens Habitat Gate | B5 | Pair rad pulse timing with habitat uplift. | Magnet Wyrm tremor intervals | 1400 AC, Resonance Mk III kit | 1.2h |
| PQ-V2-12 | T3 | Story/Reveal | Memorial Forge Lock | B4 | Heat-lock a forge using recovered death-site names. | Caldera Mantis pressure | 1700 AC, Geothermal Maul | 1.3h |
| PQ-V2-13 | T4 | Access Gate | Charter Locks A | B7 | Solve three surface locks tied to C1-C3 testimony. | Vault Stalker patrols | 2200 AC, Vault-Cut Spear | 1.5h |
| PQ-V2-14 | T4 | Access Gate | Charter Locks B | B7 | Solve C4-C6 locks and prepare charter vote conditions. | Glass Hive resonance | 2600 AC, Horizon Mk IV kit | 1.5h |
| PQ-V2-15 | T4 | Story/Reveal | Charter Seal | B7 | Align Build/Evacuate/Seed clauses with restored people. | Kairos final truth | 3000 AC, Charter Sidearm | 1.6h |
| PQ-V2-16 | T3 | Environmental Survival no gate | Gas Dome Farm | UG2 | Vent gases into a safe agri mix. | Heat Eel, Tube Jackal | 1200 AC, agri module kit | 1.2h |
| PQ-V2-17 | T3 | Loot/Cache/Weapon | Dual Dome Cache | UG2 | Sync two gas domes to uncover rare seed crates. | Plume Moth larvae | 1300 AC, Seedvault upgrade | 1.1h |
| PQ-V2-18 | T3 | Environmental Survival no gate | Crust Terrace | UG3 | Mark safe brine crust without triggering collapse. | Magnet Wyrm tremors | 1200 AC, cellar plan | 1.2h |
| PQ-V2-19 | T2 | Loot/Cache/Weapon | Flooded Hydro Cache | UG3 | Wade hydroponics route by pressure rhythm. | Void Kelp roots | 900 AC, Hearth Injector cache | 1.0h |
| PQ-V2-20 | T2 | Environmental Survival no gate | Rockfall Terrace | UG1 | Drop controlled rockfall into future farm terraces. | Tube Jackal routes | 800 AC, building materials | 1.1h |
| PQ-V2-21 | T2 | Loot/Cache/Weapon | Guard Armory Code | B6 | Decode Guard trust keypad after shelter drills. | Horizon Guard HQ | 900 AC, Beacon Rifle mod | 0.8h |
| PQ-V2-22 | T2 | Loot/Cache/Weapon | Cleanroom Bloom | B2 | Sterilize grow beds in correct order. | Vent Crabs in ducts | 850 AC, Lens Spade upgrade | 1.0h |
| PQ-V2-23 | T2 | Loot/Cache/Weapon | Guild Crane Puzzle | B1 | Reverse Crane-Link polarity to open a sealed crate. | Rebuild yard | 800 AC, Rivet Gun upgrade | 0.9h |
| PQ-V2-24 | T2 | Story/Reveal | Name Door | B6 | Use printed Echo name tags as keys. | Hearth Clinic, Echo chronicle | 750 AC, Named Sync record | 0.9h |
| PQ-V2-25 | T2 | Loot/Cache/Weapon | Commons Freezer | B6 | Re-power freezer through storm-safe grid. | Caravan Commons | 900 AC, cold-chain kit | 1.0h |
| PQ-V2-26 | T2 | Story/Reveal | Helix Lease Seal | B2 | Break or validate lease clauses in a stylus cipher. | Helix Mission | 950 AC, Continuity documents | 1.1h |
| PQ-V2-27 | T2 | Loot/Cache/Weapon | Freeboot False Wall | B3 | Move Mute Cart through an ash-cut wall. | Freeboot Relief | 900 AC, Mute Cart Rig mod | 1.0h |
| PQ-V2-28 | T3 | Access Gate | Barrier Grid | B3 | Disable three linked Horizon Barrier fields. | Rift Stalker in field gaps | 1600 AC, route unlock | 1.4h |
| PQ-V2-29 | T3 | Combat/Encounter | Power Ethics | B3 | Choose power siphon route while H3 raiders attack. | Humanoids H1-H3 | 1500 AC, Resonance Mk III kit | 1.3h |
| PQ-V2-30 | T2 | Optional Mastery | Beacon Harmony | B3 | Tune friendly frequencies without overloading beacons. | Silence Moth teaser | 900 AC prestige, beacon cosmetic | 1.1h |
| PQ-V2-31 | T3 | Environmental Survival no gate | Void Kelp Survey | B5 | Scan live Void Kelp without exceeding exposure thresholds. | Void Kelp, Still Hunter shadow | 1300 AC, rad filter | 1.1h |
| PQ-V2-32 | T2 | Optional Mastery | Pulse Ride | B2 | Ride geyser pulse cycles for a shortcut medal. | Dust Spout, flyers F1-F3 | 600 AC prestige, movement badge | 1.0h |
| PQ-V2-33 | T2 | Optional Mastery | Spout Alley | B2 | Cross a Dust Spout lane without shelter use. | Dust Spout | 700 AC prestige, plume decal | 1.0h |
| PQ-V2-34 | T2 | Combat/Encounter | Multi-Breach Hab | B3 | Repair three breaches while Basalt Jackals attack. | Basalt Jackal | 950 AC, Shelter Mk II kit | 1.2h |
| PQ-V2-35 | T3 | Combat/Encounter | Cover Relay | B4 | Rebuild cover relays during Caldera Mantis waves. | Caldera Mantis, Heat Eel | 1600 AC, Geothermal mod | 1.2h |
| PQ-V2-36 | T4 | Combat/Encounter | Android Rehab Cipher | B7 | Reprogram or scrap android A8-A10 under fire. | Androids A8-A10, machines M6-M7 | 2200 AC, android frame | 1.3h |
| PQ-V2-37 | T4 | Optional Mastery | Silent Refugee Path | B7 | Mark a quiet route past Vault Stalkers with no alarm. | Vault Stalker, Silence Moth | 2500 AC prestige, stealth charm | 1.1h |
| PQ-V2-38 | T3 | Optional Mastery | Crust Lottery Farm | UG3 | Solve a perfect safe lattice for bonus farm yield. | Magnet Wyrm, Rust Garden roots | 3000 AC prestige, farm cosmetic | 1.2h |
| PQ-V2-39 | T3 | Combat/Encounter | Dual Faction Barrier | B3 | Use Guard and Freeboot tools while Rift Stalkers breach. | Rift Stalker | 1800 AC, Grey Bloom perk | 1.4h |
| PQ-V2-40 | T4 | Combat/Encounter | Legacy Cage | B7 | Hold off Glass Hive and Void Stitcher while Evacuate seal opens. | Glass Hive, Void Stitcher | 3000 AC, Charter Mk IV kit | 1.4h |

**Puzzle subtotal:** 45h designed.

---

## 8. Factions V2

### Horizon Guard

- **Role:** Colony security, storm shelter discipline, wall rings, Barrier Field ethics.
- **Gear:** Horizon Shield, Beacon Rifle, Shelter Flare, Guard Banner mod.
- **Skills:** Shelter Rally, defensive regroup, escort discipline.
- **Quest identity:** Protect workers without turning the colony into a bunker.
- **Conflicts:** Freeboot Relief over smuggling and refugee access; Rebuild Guild over unsafe scaffolds; Helix over privatized security.

### Green Lens Collective

- **Role:** Science, agriculture, vent greenhouse work, ecology observation.
- **Gear:** Lens Spade, Nutrient Scanner, Seedvault Case.
- **Skills:** Bloom Read, weather-window crop prediction, contamination reads.
- **Quest identity:** Turn poisonous landscapes into food systems without pretending Io is harmless.
- **Conflicts:** Helix wants schema ownership; Caravan Commons wants wider food access.

### Rebuild Guild

- **Role:** Salvage-to-construction, hab conversion, BCP Changes tab work, module install.
- **Gear:** Rivet Gun, Crane-Link, Hab Patch kit.
- **Skills:** Fast Frame, salvage yield, structural repair.
- **Quest identity:** Builders learning to stop thinking like scrappers.
- **Conflicts:** Horizon Guard over safety; Caravan Commons over stockpiles.

### Hearth Clinic

- **Role:** Medical care, Echo ethics, Purification Hub, no-base-death fiction.
- **Gear:** Hearth Injector, Name Tag Printer, Triage Cart.
- **Skills:** Named Sync, faster Echo stabilization when chronicle names are restored.
- **Quest identity:** People are not resources, including Memory Cores and Echoes.
- **Conflicts:** Helix classifies imprints as IP; Guard sometimes sees Echoes as threats.

### Caravan Commons

- **Role:** Logistics, AC market ledgers, weather schedules, convoy work.
- **Gear:** Commons Pack, Schedule Slate, Storm Ledger.
- **Skills:** Shared Manifest, carry capacity, convoy timing.
- **Quest identity:** A colony becomes real when people trust the same ledger.
- **Conflicts:** Rebuild over materials priority; Guard over risky schedule windows.

### Helix Continuity Mission

- **Role:** External corporate visitors, funding, contracts, continuity claims.
- **Gear:** Continuity Suit, Audit Drone, Lease Stylus.
- **Skills:** Grant Gate, funded upgrades with strings attached.
- **Quest identity:** Offers stability at the cost of ownership.
- **Conflicts:** Green Lens over greenhouse IP; Hearth Clinic over Echo personhood; final charter clauses.

### Freeboot Relief

- **Role:** External smugglers turned aid runners, contraband medicine, refugee mercy.
- **Gear:** Relief Cloak, Mute Cart Rig, Grey Seed Pouch.
- **Skills:** Grey Bloom, quiet routes, nonstandard crop nodes.
- **Quest identity:** Law bends when people are outside the Barrier Field.
- **Conflicts:** Horizon Guard over access; Helix over unlicensed supplies; ally to Hearth Clinic.

---

## 9. Friction And Gate Matrix

Colony Horizon uses friction to schedule hope, not stall it. The best gates ask the player to build, shelter, route, repair, vote, or take responsibility.

| ID | Friction / Gate | Primary Pattern | Emphasis | Content Using It |
|---|---|---|---|---|
| F1 | **Colony Barrier Field** | Power route / BCP repair / faction access choice | Expansion corridors open when colony infrastructure earns them. | ML-03, ML-12, SQ-05, SQ-39, PQ-01, PQ-28 |
| F2 | **Weather Window Cadence** | Building -> Peak -> Dissipating -> Clear | Weather is a schedule; quests can be delayed, sheltered, or timed. | SQ-02, SQ-07, SQ-26, PQ-03, PQ-08 |
| F3 | **Storm Shelter Mandate** | Workers must reach Command Center during Peak. | Base injury-not-death; poor planning injures, does not kill base-22. | ML-02, SQ-02, SQ-21 |
| F4 | **Building Control Panel Tabs** | Overview, Companions, Production, Craft, Changes | Colony construction fantasy lives in in-world terminals. | ML-01, ML-07, SQ-01, SQ-13 |
| F5 | **Echo Sync Stakes** | Timed sync / name restore / permanent fail | Echo fail is permanent; retry means future content changes. | SQ-03, SQ-19, PQ-24 |
| F6 | **Exposure Thresholds** | Cold/heat/radiation load | B5/B4 do not only gate by item; they demand habitat planning. | ML-10, ML-11, SQ-09, PQ-10 |
| F7 | **Gas Dome / Vent Farm** | Valve timing and O2/agri mix | Survival puzzle with production payoff. | ML-07, PQ-16, PQ-17 |
| F8 | **Brine Crust Risk** | Safe path mapping / tremor timing | Optional survival and farm expansion, usually no hard gate. | SQ-16, PQ-18, PQ-38 |
| F9 | **Strain / Saturation** | Workload, Purification Hub, Echo cost | Hope has a maintenance cost, especially in Seed ending. | SQ-24, ML-16 |
| F10 | **Memorial Pressure** | Death-site respect / morale events | Old expedition deaths become colony practices, not loot flavor. | ML-11, SQ-06, SQ-18, PQ-12 |

---

## 10. Hour Budget

Designed hours exclude exploration, optional harvesting drift, base decoration, free roam, and post-ending sandbox.

| Bucket | Hours |
|---|---:|
| Mainline progress-triggered story | 32 |
| Side quests (40) | 52 |
| Puzzle quests (40) | 45 |
| Faction reputation overhead | 12 |
| Systemic repeatables: shelter drills, weather windows, BCP production, Echo sync, Barrier reroutes, memorial events | 29 |
| **Designed total excluding exploration** | **170** |
| Exploration / free roam / building decoration bonus | **25-45 bonus** |

**Verdict:** V2 Colony Horizon lands in the requested **165-175 designed hour** range and clears the 150h minimum without counting exploration.

---

## 11. Sample Dialogue

### Kairos - Waking Hostile

> "Do not route power through that wall. They sealed it while singing because the storm had already taken the roof. You do not get to call their graves infrastructure."

### Kairos - First Trust Shift

> "One name restored. One less person buried under a file hash. Keep going, builder."

### Kairos - Advisor State

> "Ash gale building west of Ridge 7. Move the harvest inside. Tell the Caravan Commons they have forty minutes if they want pride, twenty if they want people safe."

### Kairos - Core 6

> "The charter was never finished. They argued until the last shelter door closed. That is not failure. That is a colony trying to be honest before it was ready."

### Horizon Guard

> "A wall is not a threat. It is a promise that the worker behind it gets to sleep."

### Green Lens Collective

> "Nothing here is barren. It is just speaking in chemistry we have not earned yet."

### Rebuild Guild

> "Scrap is what you call a thing before somebody needs it. Give me a panel and a crew."

### Hearth Clinic

> "No more unnamed patients. Not Echoes, not cores, not the ones who wake up scared and useful."

### Caravan Commons

> "The ledger is not greed. It is how we prove the last crate went to a person, not a rumor."

### Helix Continuity Mission

> "We can protect your colony from collapse. All we ask is continuity of ownership, terminology, and future yield."

### Freeboot Relief

> "Call it smuggling when everyone is inside the field. Call it medicine when someone is still outside."

### Build Ending

> Kairos: "Then we stay. Not because Io softened. Because you wrote rules strong enough to hold people through the next storm."

### Evacuate Ending

> Kairos: "Take the names. Leave the buildings if you must. A colony can fail and still save its people from erasure."

### Seed Ending

> Kairos: "Not ghosts. Not settlers. A third promise. Feed it carefully."

---

## 12. Phase 5 Legacy And Implementation Notes

- V2 should inherit Phase 5 legacy content as emotional archaeology, not as a competing canon layer.
- All economy text says **Aether Credits / AC** only.
- Journal Craft remains recipe library / scroll learning; production lives in Building Control Panels.
- Building Control Panels must remain central: Overview for status, Companions for assignments, Production for queues, Craft for recipes, Changes for modules and upgrades.
- Weather Director [PLANNED] is the major missing scheduler for storm shelter mandates and weather-window quest design.
- Kairos, six cores, Resonance Events, Communications, Purification Hub live behavior, biomes, underground, modules, and pet migration are [PLANNED].
- AC, EchoGenerator/chronicle, exposure, Sulfur Hound+B1 table, World Engine spine, BCP shell, combat, and hotbar are [SHIPPED] surfaces to build around.
- Weather stubs, BCP Craft/Changes, hovercraft, quest scaffold, and pets legacy are [PARTIAL] and should be connected carefully rather than treated as finished.
