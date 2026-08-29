# Io Lore Naming Master List (LNM)

**Revision:** 2 — solar-system ores, Wiffle ration line, Io-prefix harvests, alien tier  
**Status:** Design draft — promote to GDD **Appendix A2g** after review  
**Authority:** GDD 5.0, `Io_Biome_Ecology_Roster.md`, `ItemRegistry` (34 items on disk)  
**Companion:** PPT = **People, Places, Things** (directions / knowledge registry)

---

## 1. Naming rules (revision 2)

| Origin tag | Use for | Pattern | Example |
|------------|---------|---------|---------|
| **UEA** | Anything **brought to Io** — machines, weapons, ammo, tools, hardware, med kits, O₂, fuel, modules, vehicles, suit consumables | `UEA` + `[Mark / role]` | UEA Mark-7 Sidearm |
| **Wiffle** | **Expedition ration containers** left in wrecks, camps, caches — familiar Earth food names in branded tins/pouches/tubes | `Wiffle` + `[familiar food]` | Wiffle Beef Soup |
| **Io Native** | Foraged, harvested, or cooked from Io biology | `[Io prefix]` + `[familiar food word]` | Brimstone Leeks, Sulfur Needles |
| **Solar Catalog** | All **ore and bulk mineral** types — named for **Solar System bodies** (real geology, UEA survey classification) | `[Body]` + `[mineral form]` | Martian Ferric Regolith |
| **Alien** | Rare **non-Terran, non-Io** materials (precursor, resonance, vault) — small curated set | `[phenomenon]` + `[form]` | Resonance Lattice Dust |
| **Field** | Improvised melee / local craft **not** UEA issue | `[place/threat]` + `[tool]` | Caldera Splitter |

### Io-prefix palette (biology harvests & forage)

Use one prefix + one **familiar** noun (vegetable, cut, tuber, bulb, rib, scale — player reads it instantly):

`Brimstone` · `Sulfur` · `Vent` · `Ash` · `Basalt` · `Condensate` · `Geyser` · `Polar` · `Tube` · `Rim` · `Void`

Examples: **Brimstone Leeks**, **Sulfur Needles**, **Vent Kelp Ribbons**, **Ash Vale Tubers**.

### Solar System ore bodies (catalog)

Mars · Luna · Mercury · Venus · Ceres · Vesta · Pallas · Europa · Ganymede · Io *(local Jovic)* · Titan · Enceladus · Asteroid Belt *(generic)*

Alien tier stays **small** — five types at ship target, expandable in DLC.

### Three-name stack (unchanged)

**Display name** · **Field nickname** · **PPT keyword** (`snake_case`)

---

## 2. Category index — every player-usable type

| # | Category | `ItemType` | Origin mix | On disk | +5 new | Total target |
|---|----------|------------|------------|---------|--------|--------------|
| A | Solar System Ores & Minerals | Resource | Solar Catalog | 2 | 5 | 7 |
| B | Alien & Precursor Materials | Resource | Alien | 0 | 5 | 5 |
| C | Io Flora & Fauna Harvest | Resource | Io Native | 2 | 5 | 7 |
| D | Expedition Ration Containers | Consumable | Wiffle | 0 | 5 | 5 |
| E | Io-Foraged Food & Camp Cooking | Consumable | Io Native | 6 | 5 | 11 |
| F | Health, Med & Field Kits | Consumable | UEA + Io | 2 | 5 | 7 |
| G | O₂ & Breathables | Consumable | UEA | 2 | 5 | 7 |
| H | Inoculations, Filters & Suit Gels | Consumable | UEA + Io | 0 | 5 | 5 |
| I | Salvage & Craft Components | Resource | UEA salvage | 2 | 5 | 7 |
| J | Fuel, Cells & Operations | Resource | UEA | 1 | 5 | 6 |
| K | Melee Weapons | MeleeWeapon | UEA + Field | 7 | 5 | 12 |
| L | Ranged Weapons | RangedWeapon | UEA | 3 | 5 | 8 |
| M | Ammunition | Ammo | UEA | 4 | 5 | 9 |
| N | Tools & Survey Gear | Tool | UEA | 2 | 5 | 7 |
| O | Modules, Harness & Attachments | Resource | UEA | 1 | 5 | 6 |
| P | Vehicles & Deployables | Vehicle | UEA | 1 | 5 | 6 |
| Q | Throwables & Tactical Consumables | Consumable | UEA | 0 | 5 | 5 |
| R | Quest, Access & Story Items | Quest | Mixed | 0 | 5 | 5 |

**PPT** (People · Places · Things) — §8–10 unchanged in purpose; keywords refreshed in §11.

---

## A. Solar System Ores & Minerals

*Laser-mined boulders. UEA geological survey names — body of origin or closest spectral match.*

### On disk (rehashed)

| Asset | Was | **Lore name** | Body | PPT keyword |
|-------|-----|---------------|------|-------------|
| `Iron Ore` | Iron Ore | **Martian Ferric Regolith** | Mars | `ore_martian_ferric` |
| `Silicate Ore` | Silicate Ore | **Lunar Anorthite Silicate** | Luna | `ore_lunar_anorthite` |

### +5 new (player-usable)

| **Lore name** | Body | Use | PPT keyword |
|---------------|------|-----|-------------|
| **Mercurian Magnetite Flakes** | Mercury | Conductors, magnetic craft | `ore_mercurian_magnetite` |
| **Venusian Sulfur Cake** | Venus | Sulfur chemistry, filters | `ore_venusian_sulfur` |
| **Ceres Carbonaceous Chondrite** | Ceres | Carbon composites, gel base | `ore_ceres_chondrite` |
| **Europan Brine Evaporite** | Europa | Salt circuits, coolant precursors | `ore_europan_brine` |
| **Vestan Nickel-Iron Ingot** | Vesta | High-grade alloy smelt input | `ore_vestan_nickel_iron` |

---

## B. Alien & Precursor Materials

*Rare tier. Not Terran, not standard Io harvest — vault, resonance, precursor sites.*

### +5 new (ship target — entire category)

| **Lore name** | Source | Use | PPT keyword |
|---------------|--------|-----|-------------|
| **Resonance Lattice Dust** | B7 / supercell | Aether research, elite ammo mod | `alien_resonance_dust` |
| **Precursor Teal Filament** | Vault walls | Precursor alloy refine | `alien_teal_filament` |
| **Void Seam Crystal** | Void Stitcher | Armor mod, codex | `alien_seam_crystal` |
| **Aether Seep Resin** | S5 pools | Memory Core stabilizer craft | `alien_aether_resin` |
| **Vault Glass Petal Shard** | B7 antechamber | Lock puzzles, optics | `alien_vault_petal` |

---

## C. Io Flora & Fauna Harvest

*Hold-E gather. Familiar noun, Io prefix.*

### On disk (rehashed)

| Asset | Was | **Lore name** | PPT keyword |
|-------|-----|---------------|-------------|
| `Brimstone Blade` | Brimstone Blade | **Brimstone Fan Fronds** | `harvest_brimstone_fronds` |
| `Sulfur Needle Tuft` | Sulfur Needle Tuft | **Sulfur Needle Tuft** | `harvest_sulfur_needles` |

### +5 new

| **Lore name** | Familiar read | Use | PPT keyword |
|---------------|---------------|-----|-------------|
| **Brimstone Leek Stalks** | Leeks | Stew, filter mesh | `harvest_brimstone_leeks` |
| **Vent Kelp Ribbons** | Kelp | Broth, insulation fiber | `harvest_vent_kelp` |
| **Ash Vale Tubers** | Tubers | Starch paste, ration filler | `harvest_ash_tubers` |
| **Condensate Pearl Pods** | Peas / pods | O₂ supplement chemistry | `harvest_condensate_pods` |
| **Rim Glass Barley** | Barley | Abrasive, craft binder | `harvest_rim_glass_barley` |

---

## D. Expedition Ration Containers (Wiffle line)

*Familiar foods in **left-behind expedition packaging** — wrecks, camps, quartermaster caches. Not Io biology.*

### +5 new (entire category — Wiffle brand)

| **Lore name** | Container read | Effect (design target) | PPT keyword |
|---------------|----------------|------------------------|-------------|
| **Wiffle Beans** | Tin | Energy + light stamina | `wiffle_beans` |
| **Wiffle Beef Soup** | Pouch | Health + energy | `wiffle_beef_soup` |
| **Wiffle Chicken Tube** | Squeeze tube | Stamina | `wiffle_chicken_tube` |
| **Wiffle Oat Pouch** | Foil pouch | Energy (long shelf) | `wiffle_oat_pouch` |
| **Wiffle Coffee Bulb** | Bulb ampule | Stamina regen buff (short) | `wiffle_coffee_bulb` |

---

## E. Io-Foraged Food & Camp Cooking

*Native ingredients prepared at camp fire / cooking station.*

### On disk (rehashed)

| Asset | Was | **Lore name** | PPT keyword |
|-------|-----|---------------|-------------|
| `Mushroom` | Mushroom | **Brimstone Cap** | `food_brimstone_cap` |
| `Cooked Mushroom` | Cooked Mushroom | **Charred Brimstone Cap** | `food_charred_cap` |
| `Forest Stew` | Forest Stew | **Brimstone Leek & Cap Stew** | `food_leek_stew` |
| `Red Lilly` | Red Lilly | **Sulfur Needle Bulbs** | `food_sulfur_bulbs` |
| `Pimican` | Pimican | **Vent Kelp & Tuber Pemmican** | `food_vent_pemmican` |
| `Rock` | Rock | **Basalt Fragment** *(craft, not food)* | `basalt_fragment` |

### +5 new

| **Lore name** | Recipe / source | Effect (design target) | PPT keyword |
|---------------|-----------------|------------------------|-------------|
| **Geyser Strider Skewers** | Fauna drop + cook | Energy + stamina | `food_strider_skewers` |
| **Condensate Onion Broth** | Condensate pods + cap | O₂ bump + energy | `food_condensate_broth` |
| **Ash Vale Mash** | Ash tubers + mash | Health + energy | `food_ash_mash` |
| **Polar Rim Tea** | Polar filament + water | Cold thermal resist (short) | `food_polar_tea` |
| **Sulfur Needle Pickles** | Needle tuft brine jar | Sulfur resist (short) | `food_needle_pickles` |

---

## F. Health, Med & Field Kits

*UEA issue + Io-native salves.*

### On disk (rehashed)

| Asset | Was | **Lore name** | Origin | PPT keyword |
|-------|-----|---------------|--------|-------------|
| `Medpack` | Medpack | **UEA Field Triage Seal** | UEA | `med_triage_seal` |
| `herbal_medpack` | Herbal Medpack | **Sulfur Needle Salve Kit** | Io Native | `med_needle_salve` |

### +5 new

| **Lore name** | Origin | Effect (design target) | PPT keyword |
|---------------|--------|------------------------|-------------|
| **UEA Trauma Foam Cartridge** | UEA | Large health restore | `med_trauma_foam` |
| **UEA Stim Slap Patch** | UEA | Stamina + minor health | `med_stim_patch` |
| **UEA Coagulant Wrap** | UEA | Bleed stop + heal over time | `med_coagulant_wrap` |
| **Brimstone Poultice Pack** | Io Native | HoT from leek + tuft craft | `med_brimstone_poultice` |
| **UEA Antirad Syrette** | UEA | Rad exposure shave (not full inoc) | `med_antirad_syrette` |

---

## G. O₂ & Breathables

*All UEA — shipped to Io.*

### On disk (rehashed)

| Asset | Was | **Lore name** | PPT keyword |
|-------|-----|---------------|-------------|
| `Oxygen Tank` | Oxygen Tank | **UEA O₂ Reserve Canister** | `o2_reserve_can` |
| `Oxygen Tank Mini` | Oxygen Tank Mini | **UEA Pocket Rebreather Ampule** | `o2_pocket_amp` |

### +5 new

| **Lore name** | Effect (design target) | PPT keyword |
|---------------|------------------------|-------------|
| **UEA O₂ Twin-Pack** | Two canisters, faster deploy | `o2_twin_pack` |
| **UEA Suit Bypass Cartridge** | Emergency suit O₂ bridge | `o2_suit_bypass` |
| **UEA SO₂ Scrubber Tablet** | Sulfur exposure scrub (minor) | `o2_scrubber_tablet` |
| **UEA High-Pressure O₂ Flask** | Deep tube / low pressure zones | `o2_high_pressure` |
| **UEA Condensate Infusion Bulb** | Small O₂ + condensate flavor fiction | `o2_condensate_bulb` |

---

## H. Inoculations, Filters & Suit Gels

*UEA-manufactured doses; Io reagents in craft.*

### +5 new (entire category)

| **Lore name** | Pressure | Effect (design target) | PPT keyword |
|---------------|----------|------------------------|-------------|
| **UEA Rad-Shimmer Gel Dose** | Radiation | Extends safe rad window | `inoc_rad_gel` |
| **UEA Heat-Routing Thermal Gel** | Thermal (heat) | Caldera rim crossing | `inoc_thermal_gel` |
| **UEA Brimstone Filter Gel Dose** | Sulfur | Storm / plains prep | `inoc_sulfur_gel` |
| **UEA Cold-Spike Balm Dose** | Thermal (cold) | Polar night prep | `inoc_cold_balm` |
| **UEA Volcano Stress Wafer** | Volcano | Tremor / eruption buffer | `inoc_volcano_wafer` |

---

## I. Salvage & Craft Components

*Wreckage from prior expeditions — still UEA / corporate hardware origin.*

### On disk (rehashed)

| Asset | Was | **Lore name** | PPT keyword |
|-------|-----|---------------|-------------|
| `metal_scrap` | Metal Scrap | **UEA Wreckage Alloy Shred** | `scrap_alloy_shred` |
| `electronic_scrap` | Electronic Scrap | **UEA Salvage Circuit Slab** | `scrap_circuit_slab` |

### +5 new

| **Lore name** | Use | PPT keyword |
|---------------|-----|-------------|
| **UEA Hull Plate Fragment** | Heavy armor craft | `scrap_hull_plate` |
| **UEA Capacitor Brick** | Energy weapons, fuel craft | `scrap_capacitor` |
| **UEA Polymer Gasket Roll** | Seals, habitat modules | `scrap_gasket_roll` |
| **UEA Optical Shred Bundle** | Scopes, scanners | `scrap_optical_shred` |
| **UEA Hydraulic Piston Core** | Vehicles, excavator repair | `scrap_piston_core` |

---

## J. Fuel, Cells & Operations

*UEA power logistics.*

### On disk (rehashed)

| Asset | Was | **Lore name** | PPT keyword |
|-------|-----|---------------|-------------|
| `Plasma Fuel` | Plasma Fuel | **UEA Plasma Fuel Cell** | `fuel_plasma_cell` |

### +5 new

| **Lore name** | Use | PPT keyword |
|---------------|-----|-------------|
| **UEA Micro-Fusion Cell** | Building generators | `fuel_micro_fusion` |
| **UEA Hover Sled Charge Pack** | Skim-Pak refill | `fuel_hover_pack` |
| **UEA Mining Laser Charge Drum** | DM-MT bulk reload | `fuel_mining_drum` |
| **UEA Emergency Generator Brick** | Camp outage bridge | `fuel_gen_brick` |
| **UEA Ion Capacitor Flask** | Ion storm module shielding | `fuel_ion_flask` |

---

## K. Melee Weapons

*UEA issue blades + field improvisations.*

### On disk (rehashed)

| Asset | Was | **Lore name** | Origin | PPT keyword |
|-------|-----|---------------|--------|-------------|
| `weap2_sword` | weap2_sword | **UEA Cutlass Mark II** | UEA | `melee_uea_cutlass` |
| `Sword of Fear` | Sword of Fear | **Field Rift-Cleaver** | Field | `melee_rift_cleaver` |
| `Death Axe` | Death Axe | **Field Caldera Splitter** | Field | `melee_caldera_splitter` |
| `Spear of Fate` | Spear of Fate | **Field Vent Pike** | Field | `melee_vent_pike` |
| `Wood Axe` | Wood Axe | **UEA Breach Hatchet Mark I** | UEA | `melee_breach_hatchet` |
| `weap_two_handed` | Two-Handed Sword | **UEA Heavy Breach Blade** | UEA | `melee_heavy_breach` |
| `2 Hander` | 2 Hander | **UEA Mortal Breach Blade** *(veteran name)* | UEA | `melee_mortal_breach` |

### +5 new

| **Lore name** | Origin | Role | PPT keyword |
|---------------|--------|------|-------------|
| **UEA Trench Knife Mark IV** | UEA | Fast light melee | `melee_trench_knife` |
| **UEA Riot Baton Mark II** | UEA | Stun / CC melee | `melee_riot_baton` |
| **Field Basalt Pick-Hammer** | Field | Armor shred, mining panic | `melee_basalt_pick` |
| **UEA Engineering Maul** | UEA | Anti-android blunt | `melee_eng_maul` |
| **Field Sulfur Hound Fang Glaive** | Field | Craft from hound drop | `melee_fang_glaive` |

---

## L. Ranged Weapons

*All UEA — expedition issue.*

### On disk (rehashed)

| Asset | Was | **Lore name** | PPT keyword |
|-------|-----|---------------|-------------|
| `sci_fi_pistol` | Sci-Fi Pistol | **UEA Mark-7 Sidearm** | `ranged_mark7` |
| `survival_rifle` | Survival Rifle | **UEA Expedition Carbine** | `ranged_carbine` |
| `DM_Mining_Tool` | DM Mining Tool | **UEA DM-MT Laser Extractor** | `ranged_dm_mt` |

### +5 new

| **Lore name** | Role | PPT keyword |
|---------------|------|-------------|
| **UEA Mark-12 Scatter Pulser** | Shotgun / close android clear | `ranged_scatter_pulser` |
| **UEA Longwatch DMR** | Marksman, weak-point android | `ranged_longwatch_dmr` |
| **UEA Suppression LMG** | Sustained fire, hound packs | `ranged_suppression_lmg` |
| **UEA Rivet Driver Sidearm** | Nail rivets; anti-Rust Garden | `ranged_rivet_driver` |
| **UEA Resonance Dart Launcher** | Sticky trackers; Echo / vault ops | `ranged_dart_launcher` |

---

## M. Ammunition

*All UEA manufacture.*

### On disk (rehashed)

| Asset | Was | **Lore name** | PPT keyword |
|-------|-----|---------------|-------------|
| `Standard` | Standard | **UEA Ballistic Coil Rounds** | `ammo_ballistic_coil` |
| `Plasma` | Plasma | **UEA Plasma-T Field Cartridge** | `ammo_plasma_t` |
| `Laser Pistol Ammo` | Laser Pistol Ammo | **UEA Pulse Cell — Pistol Grade** | `ammo_pulse_pistol` |
| `Laser` | Laser Pulse | **UEA Pulse Cell — Rifle Grade** | `ammo_pulse_rifle` |

### +5 new

| **Lore name** | Weapon feed | PPT keyword |
|---------------|-------------|-------------|
| **UEA Scatter Flechettes** | Scatter Pulser | `ammo_scatter_flechette` |
| **UEA DMR Sabot Rounds** | Longwatch DMR | `ammo_dmr_sabot` |
| **UEA Rivet Nails (100)** | Rivet Driver | `ammo_rivet_nails` |
| **UEA Tracking Resonance Darts** | Dart Launcher | `ammo_resonance_dart` |
| **UEA Incendiary Coil Rounds** | Carbine alt fire | `ammo_incendiary_coil` |

---

## N. Tools & Survey Gear

*All UEA — shipped or legacy issue.*

### On disk (rehashed)

| Asset | Was | **Lore name** | PPT keyword |
|-------|-----|---------------|-------------|
| `Scanner B44` | Scanner B44 | **UEA B44 Geological Survey Scanner** | `tool_scanner_b44` |
| `Binnos 250` | Binoculars 250 | **UEA Mark-250 Optic Ranging Glass** | `tool_mark250_optics` |

### +5 new

| **Lore name** | Role | PPT keyword |
|---------------|------|-------------|
| **UEA Echo Signal Sniffer** | Echo / Memory Core ping | `tool_echo_sniffer` |
| **UEA Thermal Survey Rod** | Heat shadow mapping (B4) | `tool_thermal_rod` |
| **UEA Rad Compass Puck** | Polar pulse timing (B5) | `tool_rad_compass` |
| **UEA Sample Vial Kit** | Science Specialist gather | `tool_sample_vials` |
| **UEA Field Repair Multitool** | Salvage Engineer upkeep | `tool_repair_multitool` |

---

## O. Modules, Harness & Attachments

*UEA camp / suit hardware.*

### On disk (rehashed)

| Asset | Was | **Lore name** | PPT keyword |
|-------|-----|---------------|-------------|
| `Increase Storage Module` | Increase Storage Module | **UEA Harness Loadframe Expansion** | `mod_loadframe` |

### +5 new

| **Lore name** | Effect (design target) | PPT keyword |
|---------------|------------------------|-------------|
| **UEA O₂ Recycler Cartridge** | Slower O₂ drain | `mod_o2_recycler` |
| **UEA Thermal Baffle Plate** | Thermal meter stability | `mod_thermal_baffle` |
| **UEA Sulfur Filter Canister** | Sulfur saturation slow | `mod_sulfur_filter` |
| **UEA Rad Liner Sheet** | Radiation exposure slow | `mod_rad_liner` |
| **UEA Auto-Sort Harness Chip** | QoL inventory sort | `mod_autosort_chip` |

---

## P. Vehicles & Deployables

*All UEA — brought to Io.*

### On disk (rehashed)

| Asset | Was | **Lore name** | PPT keyword |
|-------|-----|---------------|-------------|
| `Hovercraft` | Hovercraft | **UEA Skim-Pak Hover Sled** | `vehicle_skim_pak` |

### +5 new

| **Lore name** | Role | PPT keyword |
|---------------|------|-------------|
| **UEA Io Buggy Chassis Kit** | W8 ship vehicle (folded) | `vehicle_io_buggy` |
| **UEA Cargo Sled Trailer** | Haul salvage from wrecks | `vehicle_cargo_sled` |
| **UEA Deployable Survey Pylon** | Map reveal, comms boost | `deploy_survey_pylon` |
| **UEA Portable Habitat Canister** | Architect seal bubble deploy | `deploy_habitat_can` |
| **UEA Perimeter Mine Pod (non-lethal)** | CC trap, android slow | `deploy_mine_pod` |

---

## Q. Throwables & Tactical Consumables

*UEA expedition tactical — new category.*

### +5 new (entire category)

| **Lore name** | Effect (design target) | PPT keyword |
|---------------|------------------------|-------------|
| **UEA Smoke Canister Mark I** | LOS block, jackal break | `throw_smoke_can` |
| **UEA Arc Flash Grenade** | Android stagger | `throw_arc_flash` |
| **UEA Sulfur Fog Candle** | Sulfur moth / wasp repel | `throw_sulfur_candle` |
| **UEA Foam Wall Capsule** | Short cover deploy | `throw_foam_wall` |
| **UEA Noise Lure Puck** | Draw patrol off path | `throw_noise_lure` |

---

## R. Quest, Access & Story Items

*Mixed origin — not stackable vendor fodder.*

### +5 new (entire category)

| **Lore name** | Origin | Role | PPT keyword |
|---------------|--------|------|-------------|
| **UEA Crew Tag Band** | UEA | Story POI / B4 | `quest_crew_tag` |
| **Precursor Access Shard** | Alien | B7 vault locks | `quest_access_shard` |
| **Aether Memory Core** | Alien | Aether-9 slot | `quest_memory_core` |
| **Smuggler Black-Market Chip** | UEA salvage | B5 cache quest | `quest_smuggler_chip` |
| **Survey Data Spool** | UEA | B6 Lost Survey / pet quest | `quest_survey_spool` |

---

## 3. Craft recipe display names (rehashed)

| Recipe ID | **Lore output name** | Notes |
|-----------|---------------------|-------|
| `grilled_mushroom` | **Charred Brimstone Cap** | |
| `forest_stew` | **Brimstone Leek & Cap Stew** | Uses leeks + caps |
| `Pemican_recipe` | **Vent Kelp & Tuber Pemmican** | |
| `herbal_medpack` | **Sulfur Needle Salve Kit** | |
| `stone_salve` | **Lunar Silicate Salve** | Needs ItemData; fix miswire |
| `Standard_Ammo` | **UEA Ballistic Coil Rounds** | Fix miswire |
| `Plasma_Ammo` | **UEA Plasma-T Field Cartridge** | |
| `Plasma_Fuel` | **UEA Plasma Fuel Cell** | |
| `craft_sci_fi_pistol` | **UEA Mark-7 Sidearm** | |
| `craft_survival_rifle` | **UEA Expedition Carbine** | |
| `increase_storage_module` | **UEA Harness Loadframe Expansion** | |

---

## 4. Buildings & camp (UEA / colony — subtitles)

| GDD building | **Lore display** | PPT keyword |
|--------------|------------------|-------------|
| Command Center | **UEA Founder's Command Nexus** | `command_nexus` |
| Purification Hub | **UEA Strain Purification Array** | `purification_array` |
| Geothermal Harvester | **UEA Magma Tap Harvester** | `geo_harvester` |
| Echo Reclamation Chamber | **UEA Echo Reclamation Chamber** | `echo_reclamation` |
| Resonance Beacon | **UEA Resonance Beacon** | `resonance_beacon` |
| Probe Uplink | **UEA Probe Uplink Spire** | `probe_uplink` |
| Geothermal Stabilizer | **UEA Tremor Stabilizer Ring** | `geo_stabilizer` |
| Medical Facility | **UEA Medical Bay** | `medical_bay` |
| Science Labs | **UEA Science Annex** | `science_annex` |
| Pet Bay | **UEA Companion Stabilizer Bay** | `pet_bay` |

---

## 5. PPT — People

| pptId | **Label** | Notes |
|-------|-----------|-------|
| `npc_pioneer_guide` | **Quartermaster Vela** | Replaces Pioneer Guide |
| `npc_colony_ops` | **Colony Ops** | Default radio |
| `npc_aether_9` | **Aether-9** | Advisory unlock |
| `npc_med_officer` | **Chief Med Tech Aris** | |
| `npc_science_lead` | **Science Director Quill** | |
| `npc_comms_officer` | **Relay Officer Vesper** | |
| `npc_salvage_foreman` | **Salvage Foreman Calder** | |
| `npc_logistics_quarter` | **Quartermaster Routes Desk** | Stocks Wiffle line + UEA basics |

---

## 6. PPT — Places

### Biomes (codenames)

| ID | **Codename** | PPT keyword |
|----|--------------|-------------|
| B1 | Yellowfall Expanse | `biome_b1` |
| B2 | Steamspire Basin | `biome_b2` |
| B3 | Bronzeveil Flats | `biome_b3` |
| B4 | Obsidian Crown | `biome_b4` |
| B5 | Aurora Shroud | `biome_b5` |
| B6 | Spiregate Highlands | `biome_b6` |
| B7 | Tealvault Ruins | `biome_b7` |
| Hub | Founder's Perimeter | `colony_hub` |

### POI (+5 new place keywords)

| **Place** | Region | PPT keyword |
|-----------|--------|-------------|
| Wiffle Cache Trench | Graveyard overlay | `poi_wiffle_cache` |
| Martian Ore Boulder Field | B1 | `poi_martian_boulder` |
| Lunar Silicate Shelf | B6 | `poi_lunar_shelf` |
| Europan Salt Pan | S3 | `poi_europan_pan` |
| Teal Seep Shrine | B7 | `poi_teal_seep` |

---

## 7. PPT — Things

Weather (GDD A2b locked): `weather_sulfur_storm` … `weather_resonance_supercell`  
Pressures: `pressure_o2`, `pressure_rad`, `pressure_thermal`, `pressure_volcano`, `pressure_sulfur`  
Economy: **Aether Credits** · `aether_credits`

---

## 8. Ecology cross-reference

Creature / pet **display names** stay in `Io_Biome_Ecology_Roster.md`.  
Harvest drops that become items use **§C** naming (e.g. hound sinew → future **Sulfur Hound Sinew Strip** if added).

---

## 9. Migration & implementation

| Phase | Work |
|-------|------|
| 1 | Rename on-disk `ItemData` per §A–P on-disk tables |
| 2 | Add `stableItemId` for all new §+5 entries as content phases land |
| 3 | Wiffle line → wreck / quartermaster loot tables |
| 4 | Solar ores → retarget `ResourceNodeDefinition` yield names |
| 5 | Alien tier → B7 / supercell drop tables only |
| 6 | PPT registry seed from §5–7 keywords |

### Spelling / retire list

| Retire | Adopt |
|--------|-------|
| Iron Ore | **Martian Ferric Regolith** |
| Silicate Ore | **Lunar Anorthite Silicate** |
| Forest Stew | **Brimstone Leek & Cap Stew** |
| Red Lilly | **Sulfur Needle Bulbs** |
| Pimican | **Vent Kelp & Tuber Pemmican** |
| Sci-Fi Pistol | **UEA Mark-7 Sidearm** |
| Generic “Plasma Fuel” | **UEA Plasma Fuel Cell** |

---

## 10. Expansion summary

| Metric | Count |
|--------|-------|
| Player-usable categories | **18** (§2 index) |
| New items proposed (+5 per category) | **90** |
| On-disk items rehashed | **34** |
| **Total named player-usable targets** | **~124** (34 rehashed + 90 new) |

---

*Dark Matter Studios — Dark Matter: Genesis — LNM v2*
