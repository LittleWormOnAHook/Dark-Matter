# Io Lore Naming Master List

**Status:** Design draft — promote to GDD **Appendix A2g** after review  
**Authority:** GDD 5.0 (2160 Io, Aether Credits, four pressures, Aether-9), `Io_Biome_Ecology_Roster.md`, `ItemRegistry` (34 items on disk)  
**Companion:** PPT = **People, Places, Things** (NPC directions / knowledge registry — Phase 1 designed, assets pending)  
**Purpose:** One canonical rename table for player-facing copy, tooltips, PPT registry labels, and comms templates.

---

## 1. Naming rules (locked for this pass)

| Rule | Detail |
|------|--------|
| **Setting voice** | United Earth Authority (UEA) expedition hardware + Io-native chemosynthetic biology. Year **2160**. |
| **No Earth fantasy** | Retire forest / lily / sword-of-doom placeholder reads. No grass, deer, or photosynthesis flora names. |
| **Three-name stack** | **Official designation** (inventory) · **Field nickname** (comms / veterans) · **PPT keyword** (short id for directions) |
| **Biology** | Chemosynthetic, sulfur-silicon, resonance-fed — amber, teal, matte mineral, glass filament language. |
| **Machines** | Corporate / military / precursor chassis — salvage, patrol, survey, smuggler remnant. |
| **Echoes** | Keep locked format: `[Io Prefix] [Core Name] [Designation]` (see GDD A5). |
| **Currency** | **Aether Credits (AC)** only — never alternate wallet names in UI. |
| **Asset IDs** | `stableItemId` / file names may stay snake_case; **display names** in this doc are player-facing targets. |
| **Comms speakers** | **Colony Ops** (default radio) → **Aether-9** (advisory unlock). |

### Name pattern cheat sheet

| Category | Pattern | Example |
|----------|---------|---------|
| Ore / mineral | `[Element] + [Io form]` | Ferric Mass, Silicate Shard |
| Flora harvest | `[Biome cue] + [structure]` | Brimstone Fan Frond |
| Food | `[source organism] + [prep]` | Charred Cap Ration |
| Med / O₂ | `UEA` or `Field` + `[function]` | UEA O₂ Reserve Canister |
| UEA weapon | `UEA Mark-#` + `[role]` | UEA Mark-7 Sidearm |
| Improvised melee | `[threat/place] + [tool]` | Caldera Splitter |
| Ammo | `[tech] + [grade]` | Plasma-T Field Cartridge |
| Building | `[function] + [nexus/array/hub]` | Strain Purification Array |
| Place (PPT) | `[biome codename] + [landmark type]` | Yellowfall Relay Spire |
| Weather (PPT) | Locked GDD A2b event names | Sulfur Storm, Resonance Supercell |

---

## 2. Inventory items — current disk (34 assets)

### 2.1 Consumables — food & rations

| Asset / `stableItemId` | Current name | **Lore display name** | Field nickname | PPT keyword | Tooltip one-liner |
|------------------------|--------------|----------------------|----------------|-------------|-------------------|
| `Mushroom` | Mushroom | **Brimstone Cap** | yellow-cap | `brimstone_cap` | Chemosynthetic fungus from sulfur seeps. Restores stamina. |
| `Cooked Mushroom` | Cooked Mushroom | **Charred Brimstone Cap** | char-cap | `charred_cap` | Grilled over vent heat. Restores energy and stamina. |
| `Forest Stew` | Forest Stew | **Lace-Vapor Broth** | lace stew | `lace_broth` | Highland tube-lace and cap fungus reduction. Restores health, energy, stamina. |
| `Red Lilly` | Red Lilly | **Crimson Needle Bloom** | crimson bloom | `crimson_bloom` | Io forage bulb; sharp-sweet pulp. Energy and light stamina. |
| `Pimican` | Pimican | **Condensate Ration Block** | cond-block | `ration_block` | Expedition pemmican analog — dense, shelf-stable. Major health restore. |
| `Rock` | Rock | **Basalt Fragment** | shard | `basalt_shard` | Loose silicate rubble. Craft reagent; not food. |

**Craft recipe display names (no separate ItemData yet):**

| Recipe ID | Current | **Lore name** |
|-----------|---------|---------------|
| `grilled_mushroom` | Grilled Mushroom | **Charred Brimstone Cap** |
| `forest_stew` | Forest Stew | **Lace-Vapor Broth** |
| `Pemican_recipe` / `Pimican_recipe` | Pemican | **Condensate Ration Block** |
| `stone_salve` | Stone Salve *(miswired)* | **Silicate Salve** *(needs ItemData)* |

---

### 2.2 Consumables — health, O₂, field kits

| Asset | Current | **Lore display name** | Field nickname | PPT keyword | Tooltip one-liner |
|-------|---------|----------------------|----------------|-------------|-------------------|
| `Medpack` | Medpack | **UEA Field Triage Seal** | triage seal | `triage_seal` | Standard expedition medfoam pack. Restores health. |
| `herbal_medpack` (output) | Herbal Medpack | **Needle-Tuft Salve Kit** | tuft kit | `tuft_salve` | Sulfur-needle antiseptic + cap extract. Restores health. |
| `Oxygen Tank` | Oxygen Tank | **UEA O₂ Reserve Canister** | O₂ can | `o2_canister` | Ten minutes of breathable mix at Io surface pressure. |
| `Oxygen Tank Mini` | Oxygen Tank Mini | **Pocket Rebreather Ampule** | puff amp | `rebreather_amp` | Emergency O₂ bump for suit bypass. |

---

### 2.3 Resources — mining (laser)

| Asset | Current | **Lore display name** | Field nickname | PPT keyword | Notes |
|-------|---------|----------------------|----------------|-------------|-------|
| `Iron Ore` | Iron Ore | **Ferric Extraction Mass** | ferric mass | `ferric_mass` | Keep science tone; laser-mined from boulders. |
| `Silicate Ore` | Silicate Ore | **Silicate Shard Ore** | silicate | `silicate_ore` | Ceramics, abrasives, structural craft. |

---

### 2.4 Resources — harvest (Hold-E)

| Asset | Current | **Lore display name** | Field nickname | PPT keyword | Notes |
|-------|---------|----------------------|----------------|-------------|-------|
| `Brimstone Blade` | Brimstone Blade | **Brimstone Fan Frond** | fan frond | `brimstone_frond` | Aligns with ecology **Brimstone Fan** flora. |
| `Sulfur Needle Tuft` | Sulfur Needle Tuft | **Sulfur Needle Tuft** | needle tuft | `needle_tuft` | **Keep** — already Io-canonical. |

---

### 2.5 Resources — scrap & operations

| Asset | Current | **Lore display name** | Field nickname | PPT keyword | Notes |
|-------|---------|----------------------|----------------|-------------|-------|
| `metal_scrap` | Metal Scrap | **Wreckage Alloy Shred** | alloy shred | `alloy_shred` | Salvaged hull and rig plating. |
| `electronic_scrap` | Electronic Scrap | **Salvage Circuit Slab** | circuit slab | `circuit_slab` | Dead drone and comms boards. |
| `Plasma Fuel` | Plasma Fuel | **Plasma Fuel Cell** | plasma cell | `plasma_cell` | Hovercraft, generators, mining tool reload. |

---

### 2.6 Melee weapons

| Asset | Current | **Lore display name** | Field nickname | PPT keyword | Notes |
|-------|---------|----------------------|----------------|-------------|-------|
| `Sword of Fear` | Sword of Fear | **Rift-Cleaver Blade** | cleaver | `rift_cleaver` | Improvised officer blade; high crit. |
| `Death Axe` | Death Axe | **Caldera Splitter** | splitter | `caldera_axe` | Heavy breaching axe. |
| `Spear of Fate` | Spear of Fate | **Vent Pike** | vent pike | `vent_pike` | Reach weapon for skitter packs. |
| `Wood Axe` | Wood Axe | **Field Breach Hatchet** | hatchet | `breach_hatchet` | No wood on Io — field tool name. |
| `weap_two_handed` | Two-Handed Sword | **Heavy Breach Blade** | breach blade | `breach_blade` | Cannot block while equipped. |
| `2 Hander` | 2 Hander | **Mortal Breach Blade** | mortal blade | `mortal_blade` | Veteran nickname for heavy breach blade. |
| `weap2_sword` | weap2_sword | **UEA Cutlass Mark II** | cutlass | `uea_cutlass` | Standard issue side blade. |

---

### 2.7 Ranged weapons & tools

| Asset | Current | **Lore display name** | Field nickname | PPT keyword | Notes |
|-------|---------|----------------------|----------------|-------------|-------|
| `sci_fi_pistol` | Sci-Fi Pistol | **UEA Mark-7 Sidearm** | mark-seven | `mark7_sidearm` | One-hand aim/fire. |
| `survival_rifle` | Survival Rifle | **UEA Expedition Carbine** | carbine | `expedition_carbine` | Two-hand rifle; plasma secondary feed. |
| `DM_Mining_Tool` | DM Mining Tool | **DM-MT Laser Extractor** | laser MT | `dm_mining_tool` | **Keep DM prefix** per project rules. |
| `Scanner B44` | Scanner B44 | **B44 Geological Survey Scanner** | B44 | `scanner_b44` | Ai Wars legacy hardware (2120–2121). |
| `Binnos 250` | Binoculars 250 | **Mark-250 Optic Ranging Glass** | mark-250 | `optic_glass` | Long-range route scouting. |

---

### 2.8 Ammo

| Asset | Current | **Lore display name** | Field nickname | PPT keyword | Notes |
|-------|---------|----------------------|----------------|-------------|-------|
| `Standard` | Standard | **UEA Ballistic Coil Rounds** | coil rounds | `ballistic_coil` | Gunpowder-class ballistic ammo. |
| `Plasma` | Plasma | **Plasma-T Field Cartridge** | plasma-T | `plasma_t` | UEA standard energy projectile (recipe lore). |
| `Laser Pistol Ammo` | Laser Pistol Ammo | **Pulse Cell — Pistol Grade** | pistol pulse | `pulse_pistol` | Hitscan pistol feed. |
| `Laser` / Laser Pulse | Laser Pulse | **Pulse Cell — Rifle Grade** | rifle pulse | `pulse_rifle` | Hitscan rifle / mining alias grade. |

**Craft recipe display:**

| Recipe | Current | **Lore name** |
|--------|---------|---------------|
| `Standard_Ammo` | Stardard Ammo | **UEA Ballistic Coil Rounds** |
| `Plasma_Ammo` | Plasma T | **Plasma-T Field Cartridge** |

---

### 2.9 Modules & vehicles

| Asset | Current | **Lore display name** | Field nickname | PPT keyword | Notes |
|-------|---------|----------------------|----------------|-------------|-------|
| `Increase Storage Module` | Increase Storage Module | **Harness Loadframe Expansion** | loadframe | `loadframe_mod` | +1 inventory row (10 slots). |
| `Hovercraft` | Hovercraft | **UEA Skim-Pak Hover Sled** | skim-pak | `hover_sled` | Folded deployable surface transport. |

---

## 3. Planned consumables (GDD — not yet ItemData)

| Design item | **Lore display name** | Field nickname | PPT keyword | Function |
|-------------|----------------------|----------------|-------------|----------|
| Rad inoculation | **Rad-Shimmer Gel Dose** | rad gel | `rad_inoc` | B5 polar; extends safe rad window |
| Thermal gel | **Heat-Routing Thermal Gel** | heat gel | `thermal_gel` | B4 caldera rim crossing |
| Sulfur filter gel | **Brimstone Filter Gel Dose** | sulfur gel | `sulfur_inoc` | Sulfur plains / storm prep |
| Stone / silicate salve | **Silicate Salve** | sil salve | `silicate_salve` | Craft output for `stone_salve` recipe |
| Condensate vial | **SO₂ Condensate Vial** | cond vial | `condensate` | From B1 condensate mats |
| Memory Core (item) | **Aether Memory Core** | core | `memory_core` | Aether-9 slot item — not tradeable |
| Keycard / access | **Precursor Access Shard** | shard key | `access_shard` | B7 vault locks |

---

## 4. Buildings, camp structures & attachment modules

### 4.1 Major buildings (GDD A7 — rename subtitles optional)

| GDD name | **Lore display name** | Comms shorthand | PPT keyword | PPT type |
|----------|----------------------|-----------------|-------------|----------|
| Command Center | **Founder's Command Nexus** | Command | `command_nexus` | Place |
| Purification Hub | **Strain Purification Array** | Purification | `purification_array` | Place |
| Geothermal Harvester | **Magma Tap Harvester** | Harvester | `geo_harvester` | Place |
| Echo Reclamation Chamber | **Echo Reclamation Chamber** | Reclamation | `echo_reclamation` | Place |
| Resonance Beacon | **Resonance Beacon** | Beacon | `resonance_beacon` | Place |
| Probe Uplink | **Probe Uplink Spire** | Uplink | `probe_uplink` | Place |
| Geothermal Stabilizer | **Tremor Stabilizer Ring** | Stabilizer | `geo_stabilizer` | Place |
| Medical Facility | **UEA Medical Bay** | Med Bay | `medical_bay` | Place |
| Science Labs | **Colony Science Annex** | Science | `science_annex` | Place |
| Pet Bay | **Companion Stabilizer Bay** | Pet Bay | `pet_bay` | Place |
| Building Control Panel | **Structure Control Terminal** | BCP | `bcp_terminal` | Thing |
| IO Ancient Cache | **IO Ancient Cache** | Cache | `ancient_cache` | Thing |
| Cache Lid | **Cache Lid** | Lid | `cache_lid` | Thing |

### 4.2 Lite Building & attachment modules (planned)

| Module role | **Lore display name** | PPT keyword |
|-------------|----------------------|-------------|
| Generator | **Plasma Generator Pod** | `gen_pod` |
| Power grid | **Camp Grid Relay** | `grid_relay` |
| Auto gather | **Autonomous Gather Arm** | `gather_arm` |
| Logistics | **Supply Crawler Hub** | `logistics_hub` |
| Communications | **Surface Relay Mast** | `comms_mast` |
| Defense | **Perimeter Sentry Pod** | `defense_pod` |
| Mining | **Remote Mining Head** | `mining_head` |
| Portable habitat | **Architect Seal Bubble** | `seal_bubble` |

---

## 5. PPT — People

PPT registry entries use: `pptId`, `label`, `PptType.Person`, optional `npcId`, discovery keywords.

| pptId | **Display label** | Role | Comms / lore notes |
|-------|-------------------|------|-------------------|
| `npc_pioneer_guide` | **Pioneer Guide** *(retire)* → **Quartermaster Vela** | Tutorial quest giver | Camp orientation; supply runs; directions hub |
| `npc_colony_ops` | **Colony Ops** | Radio persona | Default query voice until Aether-9 advisory |
| `npc_aether_9` | **Aether-9** | Ancient Echo machine | Prologue idle probe; Memory Core hub |
| `npc_med_officer` | **Chief Med Tech Aris** | Medical Facility lead | Injury recovery, triage seal restock |
| `npc_science_lead` | **Science Director Quill** | Science Annex | Samples, inoculations, pool analysis |
| `npc_comms_officer` | **Relay Officer Vesper** | Communications | Uplink, Echo signal routing |
| `npc_salvage_foreman` | **Salvage Foreman Calder** | Salvage yard | Wreck recovery, repair queues |
| `npc_logistics_quarter` | **Quartermaster Routes Desk** | Logistics | AC vendor, resupply routes |
| `npc_echo_reclamation` | **Reclamation Attendant** | Echo chamber | Pet stabilizer / Echo intake |
| `class_architect` | **Architect Engineer** | Class anchor | Shields, habitat |
| `class_science` | **Science Specialist** | Class anchor | Scan, inoculation |
| `class_tactician` | **Combat Tactician** | Class anchor | Aggro, clear |
| `class_scout` | **Infiltrator Scout** | Class anchor | Echo signals, routes |
| `class_medtech` | **Med Tech** | Class anchor | Field triage |
| `class_logistics` | **Logistics Officer** | Class anchor | Quartermaster |
| `class_salvage` | **Salvage Engineer** | Class anchor | Upkeep, salvage |
| `class_comms` | **Communications Officer** | Class anchor | Uplink matrix |

**Neural Echoes:** use procedural names only — do not hand-author PPT Person rows per Echo; register runtime on rescue.

---

## 6. PPT — Places

### 6.1 Surface biomes (B1–B7)

| Biome ID | GDD name | **Expedition codename** | PPT keyword | Comms example |
|----------|----------|------------------------|-------------|---------------|
| B1 | Sulfur Plains | **Yellowfall Expanse** | `biome_b1` | "Yellowfall seeps are active." |
| B2 | Geyser Fields | **Steamspire Basin** | `biome_b2` | "Steamspire vent cycle is tight." |
| B3 | Ash Flats & Ridges | **Bronzeveil Flats** | `biome_b3` | "Bronzeveil visibility is down." |
| B4 | Lava Calderas | **Obsidian Crown** | `biome_b4` | "Obsidian Crown rim is unstable." |
| B5 | Polar Radiation Flats | **Aurora Shroud** | `biome_b5` | "Aurora Shroud night in six hours." |
| B6 | Basalt Highlands | **Spiregate Highlands** | `biome_b6` | "Spiregate breach is marked." |
| B7 | Precursor Ruin Belt | **Tealvault Ruins** | `biome_b7` | "Tealvault patrols are live." |
| overlay | Expedition Graveyard | **Wreckfall Overlay** | `graveyard_overlay` | "Wreckfall scrapper activity." |
| hub | Colony camp | **Founder's Perimeter** | `colony_hub` | "Return to Founder's Perimeter." |

### 6.2 Underground strata (S1–S5)

| Stratum | **Lore name** | PPT keyword |
|---------|---------------|-------------|
| S1 | **Upper Ember Tubes** | `stratum_s1` |
| S2 | **Mid-Gallery Network** | `stratum_s2` |
| S3 | **Brine Basin Depths** | `stratum_s3` |
| S4 | **Geothermal Root Lenses** | `stratum_s4` |
| S5 | **Resonance Vault Undercroft** | `stratum_s5` |

### 6.3 Authored POI landmarks (world content)

| Region | **Place name** | PPT keyword | Activity |
|--------|----------------|-------------|----------|
| B1 | **Condensate Seep Field** | `poi_condensate_seep` | Sample / O₂ chemistry |
| B1 | **Fanbelt Harvest Lane** | `poi_fanbelt` | Brimstone fan harvest |
| B2 | **Vent Crown Nest** | `poi_vent_nest` | Vent crab nest clear |
| B2 | **Dead Rig Surveyor Loop** | `poi_dead_rig` | Rusted survey drone |
| B3 | **Buried Beacon Trench** | `poi_buried_beacon` | Excavator android |
| B4 | **Crew Tag Sentry Post** | `poi_crew_tag` | Aether-9 story death site |
| B5 | **Smuggler Core Cache** | `poi_smuggler_cache` | Black-market android |
| B6 | **Tutorial Breach Pad** | `poi_tutorial_breach` | Underground entry |
| B6 | **Broodmouth Gate** | `poi_broodmouth` | Nest breach |
| B7 | **Silent Corridor Antechamber** | `poi_silent_corridor` | Puzzle / no Stitcher |
| B7 | **Vault Glass Petal Hall** | `poi_vault_petal` | Precursor lock |
| Any | **Seam Shimmer Zone** | `poi_void_seam` | Void Stitcher spawn bias |
| Colony | **Command Nexus Plaza** | `poi_command_plaza` | Hub spawn |
| Colony | **Pet Stabilizer Bay** | `poi_pet_bay` | Pet repair / swap |

---

## 7. PPT — Things

### 7.1 Weather events (GDD A2b — **keep locked names**)

| ID | **Display name** | PPT keyword |
|----|------------------|-------------|
| 01 | Sulfur Storm | `weather_sulfur_storm` |
| 02 | Ion Lightning Storm | `weather_ion_lightning` |
| 03 | Ash Gale | `weather_ash_gale` |
| 04 | Dust Spout Cluster | `weather_dust_spout` |
| 05 | Lava Flow Surge | `weather_lava_surge` |
| 06 | Geyser Field Surge | `weather_geyser_surge` |
| 07 | Caldera Eruption Column | `weather_caldera_eruption` |
| 08 | Tremor Swarm | `weather_tremor_swarm` |
| 09 | Jovian Radiation Pulse | `weather_rad_pulse` |
| 10 | Resonance Supercell | `weather_resonance_supercell` |

### 7.2 Environmental pressures (four + O₂)

| System | **Display name** | PPT keyword |
|--------|------------------|-------------|
| Oxygen | **O₂ Reserve** | `pressure_o2` |
| Radiation | **Radiation Exposure** | `pressure_rad` |
| Thermal | **Thermal Stress** | `pressure_thermal` |
| Volcano | **Volcanic Stress** | `pressure_volcano` |
| Sulfur | **Sulfur Saturation** | `pressure_sulfur` |
| Strain | **Neural Strain** | `strain_meter` |
| Saturation | **Echo Saturation** | `saturation_meter` |

### 7.3 Technology & systems

| Thing | **Display name** | PPT keyword |
|-------|------------------|-------------|
| AC | **Aether Credits** | `aether_credits` |
| Memory Core | **Aether Memory Core** | `memory_core` |
| Resonance Event | **Resonance Event** | `resonance_event` |
| Neural Echo | **Neural Echo** | `neural_echo` |
| Building Control UI | **Structure Control Interface** | `bcp_ui` |
| Journal Craft | **Recipe Archive** | `recipe_archive` |
| Trio | **Expedition Trio** | `expedition_trio` |
| Base 22 | **Camp Complement** | `base_roster` |
| Hover deploy | **Skim-Pak Deploy** | `hover_deploy` |
| Mining laser | **DM-MT Extractor Beam** | `mining_beam` |

### 7.4 Threat families (for comms / journal)

| Family | **Display label** | PPT keyword |
|--------|-------------------|-------------|
| Native fauna | **Io Lifeform** | `threat_lifeform` |
| Android | **Machine Hostile** | `threat_android` |
| Humanoid | **Expedition Remnant** | `threat_humanoid` |
| Elite global | **Void Stitcher** | `threat_void_stitcher` |
| Elite myth | **Still Hunter** | `threat_still_hunter` |
| Machine-coral | **Rust Garden** | `threat_rust_garden` |

---

## 8. Ecology & pets (cross-reference — names already Io-canonical)

**Do not rename** entries in `Io_Biome_Ecology_Roster.md` unless a dedicated lore pass flags a conflict.  
PPT keywords for creature directions use snake_case from roster name: e.g. `fauna_sulfur_hound`, `flora_brimstone_fan`, `pet_brimstone_puff`.

| Category | Count | Doc section |
|----------|-------|-------------|
| Surface flora/fauna per biome | 40+ | Ecology roster §6 |
| Android / humanoid / machine threats | 24 types | Ecology roster §4 |
| Core expedition pets | 12 | Ecology roster §4.6.4 |
| Vanity pets | 4+ | Ecology roster §4.6.5 |

---

## 9. Vendors & economy labels

| Vendor context | **Display name** | Sells |
|----------------|------------------|-------|
| Camp quartermaster | **Founder's Supply Terminal** | Rations, O₂, triage seals, basic ammo |
| Science annex shop | **Annex Reagent Counter** | Inoculations, sample kits |
| Salvage foreman | **Wreckage Reclamation Desk** | Mods, repair mats |
| Black market (B5) | **Shroud Cache Trader** *(POI-only)* | Illegal cores, magnetic ore |
| AC HUD | **Aether Credits** | — |

---

## 10. Migration checklist (implementation order)

| Phase | Work | Owner |
|-------|------|-------|
| **1** | Apply §2 display names to `ItemData.itemName` + `tooltipDescription` | Content / engineering |
| **2** | Fix miswired recipes (`stone_salve`, `Standard_Ammo` outputs) + add `Silicate Salve` ItemData | Engineering |
| **3** | Seed `Resources/PPT/PptRegistry` with §5–7 keywords | PPT Phase 1 |
| **4** | Wire Building Control `buildingDisplayName` to §4.1 | Level design |
| **5** | Comms templates: Colony Ops uses codenames (§6.1) | Communications framework |
| **6** | Promote this doc → GDD Appendix **A2g** after review | Design lead |

### Spelling normalization

| Retire | Adopt |
|--------|-------|
| Pimican / Pemican | **Condensate Ration Block** |
| Red Lilly | **Crimson Needle Bloom** |
| Forest Stew | **Lace-Vapor Broth** |
| Stardard Ammo | **UEA Ballistic Coil Rounds** |
| Binnos 250 | **Mark-250 Optic Ranging Glass** |

---

## 11. Backup & authority map

| Source | What it contributes |
|--------|---------------------|
| `GAME_DESIGN_DOCUMENT_5.0.txt` | Pressures, weather, buildings, economy, Echo format |
| `Io_Biome_Ecology_Roster.md` | Creature / flora names (canonical) |
| `Io_Biome_Exploration_Gameplay_Plan.md` | Biome verbs, gear gates, POI activities |
| `Io_World_Content_Executive_Summary.md` | Ship scope, pet counts, phase order |
| `ItemRegistry.asset` | 34 inventory items (§2) |
| `RecipeRegistry.asset` | 11 craft recipes |
| GDD 3.0 archive | Inoculation classes, building list |
| `Plasma_Ammo.asset` description | **UEA (United Earth Authority)** ammo lore |
| `DmEvents.cs` | **IO Ancient Cache** display string |
| PPT design (referenced in `World_Engine_Disk_Status.md`) | People / Places / Things registry pattern |

---

*Dark Matter Studios — Dark Matter: Genesis — Lore Naming (draft)*
