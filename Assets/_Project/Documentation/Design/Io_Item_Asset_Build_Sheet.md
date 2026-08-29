# Io Item Asset Build Sheet

**Companion:** `Io_Lore_Naming_Master_List.md` (LNM v3.1)  
**Purpose:** One row per player-usable item — everything needed to author the Unity asset.  
**Legend:** `PROTOTYPE` = on disk under old name · `NEW` = not authored yet · `SHIP` = target v1 name applied

---

## 0. Before you author (every item)

### 0.1 Folder layout

```
Assets/_Project/Data/Items/
  Resources/Mining/          MineHarvestItemData — Basic Ores (laser)
  Resources/Harvest/         MineHarvestItemData — Io harvest (Hold-E)
  Resources/Alien/           ItemData Resource — Alien tier (NEW folder)
  Components/                ItemData Resource — UEA salvage
  Operations/                ItemData Resource — fuel / power cells
  Consumables/Food/          Io food + Wiffle
  Consumables/Med/           Med + inoculations
  Consumables/Oxygen/        O₂ items
  Consumables/Throwables/    UEA throwables (NEW)
  Melee/
  Ranged/
  ammo/
  Tools/
  Modules/
  Vehicles/
  Quest/                     ItemType.Quest (NEW folder)
Assets/_Project/Data/Items/Nodes/     ResourceNodeDefinition (paired with mine/harvest)
Assets/_Project/Data/Crafting/Recipes/
Assets/_Project/Resources/ItemRegistry.asset   ← register every ItemData
Assets/_Project/Resources/Crafting/RecipeRegistry.asset
```

### 0.2 Asset type picker

| Kind | Script | Menu |
|------|--------|------|
| Standard item | `ItemData` | Project → Survival → Item Data |
| Mine / harvest yield | `MineHarvestItemData` | Project → Survival → Mine-Harvest Resource Item |
| World node | `ResourceNodeDefinition` | Project → Survival → Resource Node Definition |
| Craft recipe | `RecipeDefinition` | (existing crafting menu) |

### 0.3 Universal `ItemData` fields (all items)

| Field | Rule |
|-------|------|
| `itemName` | LNM display name |
| `icon` | 64–128px inventory sprite |
| `worldPrefab` | Pickup mesh (optional for quest-only) |
| `maxStack` | Ore/harvest 40–64 · food 20–100 · weapons 1 · ammo 99 |
| `itemType` | See category tables |
| `tooltipDescription` | One sentence; Io tone |
| `requiredLevelToCraft` | 1 unless gated |
| `requiredLevelToEquip` | Weapons/tools only |

**After create:** add asset to `ItemRegistry.asset` items list.

### 0.4 `MineHarvestItemData` extras

| Field | Mining ore | Io harvest |
|-------|------------|------------|
| `gatherKind` | `Mining` | `Harvest` |
| `lootYieldClip` / `lootGrantClip` | Optional SFX | Optional SFX |
| Pair with | `ResourceNodeDefinition` `NodeKind.Mining` `LaserMine` | `NodeKind.Plant` `HoldInteract` |

### 0.5 `ResourceNodeDefinition` (when node exists)

| Field | Notes |
|-------|-------|
| `displayName` | Same as item or node label |
| `resourceItem` | → ItemData / MineHarvestItemData |
| `interactionMode` | LaserMine vs HoldInteract |
| `durationSeconds` | ~5s default |
| `dropMin` / `dropMax` | 1–3 typical |
| `meshTemplate` | Boulder / plant prefab |
| `itemTooltip` | Copied to item on bake |

### 0.6 Consumable restore targets (design defaults)

| Tier | health | energy | stamina | oxygen |
|------|--------|--------|---------|--------|
| Snack | 0 | 15–35 | 5–15 | 0 |
| Meal | 10–25 | 50–70 | 5–15 | 0 |
| Ration block | 50 | 0 | 0 | 0 |
| Med small | 25 | 0 | 0 | 0 |
| Med large | 60–80 | 0 | 0 | 0 |
| O₂ can | 0 | 0 | 0 | 600 |
| O₂ ampule | 0 | 0 | 0 | 10 |

### 0.7 Weapon / ammo / tool checklist

**Melee:** `weaponGrip`, `meleeDamage`, `meleeDamageRandomRange`, `criticalChance`, `meleeRange`, `meleeCooldown`, `heldPrefab`, `invectorWeaponPrefab`  
**Ranged:** `rangedDamage`, `fireRate`, `magazineSize`, `defaultAmmoType`, `compatibleAmmoTypes`, `defaultAmmoItem`, `invectorWeaponPrefab`  
**Mining tool:** `isMiningTool`, `isContinuousLaser`, `miningPassDuration`, `miningChargePerPlasmaFuel`, reload = Plasma Fuel Cell  
**Ammo:** `ammoType`, `ammoPerPickup`, `rangedDamage` (modifier), `isHitscanBeam` for laser grades  
**Tool:** `toolType` (Scanner / Binoculars / Multitool), `scanRange` or optics FOV fields  
**Vehicle:** `deployedPrefab`, `itemType = Vehicle`

### 0.8 PPT keyword column

Register in `Resources/PPT/PptRegistry` when Phase 1 lands — `snake_case` id per row.

---

## A. Basic Ores (7) — `MineHarvestItemData` · Mining

| stableId | itemName | Status | Asset path (target) | Node def | maxStack | tooltip (draft) |
|----------|----------|--------|---------------------|----------|----------|-----------------|
| `iron_ore` | Iron Ore | PROTOTYPE | `Resources/Mining/Iron Ore.asset` | `Nodes/ResourceNode_IronOre` | 40 | Dense iron-bearing ore. Laser-mined from boulders. |
| `silicate_ore` | Silicate Ore | PROTOTYPE | `Resources/Mining/Silicate Ore.asset` | `Nodes/ResourceNode_SilicateOre` | 40 | Silicate fragments for ceramics and abrasives. |
| `copper_ore` | Copper Ore | NEW | `Resources/Mining/Copper Ore.asset` | `Nodes/ResourceNode_CopperOre` | 40 | Copper-bearing ore for wiring and circuits. |
| `titanium_ore` | Titanium Ore | NEW | `Resources/Mining/Titanium Ore.asset` | `Nodes/ResourceNode_TitaniumOre` | 40 | Titanium ore for armor and stress parts. |
| `nickel_ore` | Nickel Ore | NEW | `Resources/Mining/Nickel Ore.asset` | `Nodes/ResourceNode_NickelOre` | 40 | Nickel ore for alloys and cells. |
| `cobalt_ore` | Cobalt Ore | NEW | `Resources/Mining/Cobalt Ore.asset` | `Nodes/ResourceNode_CobaltOre` | 40 | Cobalt ore for capacitors and energy craft. |
| `aluminum_ore` | Aluminum Ore | NEW | `Resources/Mining/Aluminum Ore.asset` | `Nodes/ResourceNode_AluminumOre` | 40 | Aluminum ore for light frames and hull panels. |

**Craft uses:** weapons, ammo, UEA scrap recipes, building materialization (future).

---

## B. Alien Materials (5) — `ItemData` · Resource

| stableId | itemName | Status | Asset path | maxStack | drop source | tooltip (draft) |
|----------|----------|--------|------------|----------|-------------|-----------------|
| `resonance_dust` | Resonance Dust | NEW | `Resources/Alien/Resonance Dust.asset` | 20 | Supercell, B7 | Fine resonance powder. Research and elite mods. |
| `teal_filament` | Teal Filament | NEW | `Resources/Alien/Teal Filament.asset` | 20 | Vault walls | Precursor symbiont filament. Alloy refine. |
| `seam_crystal` | Seam Crystal | NEW | `Resources/Alien/Seam Crystal.asset` | 10 | Void Stitcher | Crystallized seam matter. Unique armor mod. |
| `aether_resin` | Aether Resin | NEW | `Resources/Alien/Aether Resin.asset` | 20 | S5 seep | Volatile Aether resin. Core stabilizer craft. |
| `vault_petal` | Vault Petal | NEW | `Resources/Alien/Vault Petal.asset` | 20 | B7 antechamber | Precursor glass petal. Locks and optics. |

---

## C. Io Harvest (7) — `MineHarvestItemData` · Harvest

| stableId | itemName | Status | Asset path | Node def | maxStack | tooltip (draft) |
|----------|----------|--------|------------|----------|----------|-----------------|
| `brimstone_fans` | Brimstone Fans | PROTOTYPE | `Resources/Harvest/Brimstone Blade.asset` → rename | `Nodes/ResourceNode_BrimstoneBlade` | 40 | Fan fronds seeping brimstone reagent. Hold E. |
| `sulfur_needles` | Sulfur Needles | PROTOTYPE | `Resources/Harvest/Sulfur Needle Tuft.asset` → rename | `Nodes/ResourceNode_SulfurNeedleTuft` | 40 | Bristly sulfur-rich fiber. Antiseptic reagent. |
| `brimstone_leeks` | Brimstone Leeks | NEW | `Resources/Harvest/Brimstone Leeks.asset` | `Nodes/ResourceNode_BrimstoneLeeks` | 40 | Stalk vegetable from sulfur seeps. Stew and filters. |
| `vent_kelp` | Vent Kelp | NEW | `Resources/Harvest/Vent Kelp.asset` | `Nodes/ResourceNode_VentKelp` | 40 | Ribbons from vent margins. Broth and insulation. |
| `ash_tubers` | Ash Tubers | NEW | `Resources/Harvest/Ash Tubers.asset` | `Nodes/ResourceNode_AshTubers` | 40 | Starch tubers from ash lee. Mash and filler. |
| `condensate_pods` | Condensate Pods | NEW | `Resources/Harvest/Condensate Pods.asset` | `Nodes/ResourceNode_CondensatePods` | 40 | SO₂ condensate pods. O₂ chemistry. |
| `rim_barley` | Rim Barley | NEW | `Resources/Harvest/Rim Barley.asset` | `Nodes/ResourceNode_RimBarley` | 40 | Glass-barley heads. Abrasive and binder. |

---

## D. Wiffle Rations (5) — `ItemData` · Consumable

*Loot: wrecks, camps, quartermaster. No world nodes.*

| stableId | itemName | Status | Asset path | maxStack | health | energy | stamina | tooltip (draft) |
|----------|----------|--------|------------|----------|--------|--------|---------|-----------------|
| `wiffle_beans` | Wiffle Beans | NEW | `Consumables/Food/Wiffle Beans.asset` | 20 | 0 | 25 | 10 | Expedition tin. Familiar beans. |
| `wiffle_soup` | Wiffle Soup | NEW | `Consumables/Food/Wiffle Soup.asset` | 20 | 15 | 40 | 0 | Left-behind ration pouch. Beef soup. |
| `wiffle_tube` | Wiffle Tube | NEW | `Consumables/Food/Wiffle Tube.asset` | 20 | 0 | 10 | 25 | Squeeze tube meal. Quick stamina. |
| `wiffle_oats` | Wiffle Oats | NEW | `Consumables/Food/Wiffle Oats.asset` | 20 | 0 | 35 | 0 | Foil pouch oats. Long shelf. |
| `wiffle_coffee` | Wiffle Coffee | NEW | `Consumables/Food/Wiffle Coffee.asset` | 20 | 0 | 5 | 30 | Bulb ampule coffee. Short stamina buff. |

---

## E. Io Food (11) — `ItemData` · Consumable

| stableId | itemName | Status | Asset path | maxStack | health | energy | stamina | oxygen | recipe |
|----------|----------|--------|------------|----------|--------|--------|---------|--------|--------|
| `brimstone_cap` | Brimstone Cap | PROTOTYPE | `Consumables/Mushroom.asset` | 100 | 0 | 0 | 20 | 0 | — |
| `charred_cap` | Charred Cap | PROTOTYPE | `Consumables/Cooked Mushroom.asset` | 100 | 0 | 35 | 15 | 0 | `grilled_mushroom` |
| `leek_stew` | Leek Stew | PROTOTYPE | `Consumables/Forest Stew.asset` | 50 | 10 | 70 | 5 | 0 | `forest_stew` |
| `sulfur_bulbs` | Sulfur Bulbs | PROTOTYPE | `Consumables/Red Lilly.asset` | 50 | 0 | 30 | 5 | 0 | — |
| `kelp_pemmican` | Kelp Pemmican | PROTOTYPE | `Consumables/Pimican.asset` | 20 | 50 | 0 | 0 | 0 | `Pemican_recipe` |
| `strider_skewers` | Strider Skewers | NEW | `Consumables/Food/Strider Skewers.asset` | 20 | 0 | 40 | 15 | 0 | NEW recipe |
| `condensate_broth` | Condensate Broth | NEW | `Consumables/Food/Condensate Broth.asset` | 20 | 0 | 25 | 0 | 30 | NEW recipe |
| `ash_mash` | Ash Mash | NEW | `Consumables/Food/Ash Mash.asset` | 20 | 5 | 45 | 0 | 0 | NEW recipe |
| `polar_tea` | Polar Tea | NEW | `Consumables/Food/Polar Tea.asset` | 20 | 0 | 15 | 10 | 0 | NEW recipe · buff TBD |
| `needle_pickles` | Needle Pickles | NEW | `Consumables/Food/Needle Pickles.asset` | 20 | 0 | 10 | 0 | 0 | NEW recipe · buff TBD |
| `basalt_shard` | Basalt Shard | PROTOTYPE | `Consumables/Rock.asset` | 64 | 0 | 0 | 0 | 0 | craft reagent only |

---

## F. Health & Med (7) — `ItemData` · Consumable

| stableId | itemName | Status | Asset path | health | stamina | recipe |
|----------|----------|--------|------------|--------|---------|--------|
| `uea_triage_seal` | UEA Field Triage Seal | PROTOTYPE | `Consumables/Medpack.asset` | 25 | 0 | — |
| `needle_salve` | Needle Salve | NEW | `Consumables/Med/Needle Salve.asset` | 25 | 0 | `herbal_medpack` output |
| `uea_trauma_foam` | UEA Trauma Foam Cartridge | NEW | `Consumables/Med/UEA Trauma Foam.asset` | 75 | 0 | craft TBD |
| `uea_stim_patch` | UEA Stim Slap Patch | NEW | `Consumables/Med/UEA Stim Patch.asset` | 10 | 20 | craft TBD |
| `uea_coagulant_wrap` | UEA Coagulant Wrap | NEW | `Consumables/Med/UEA Coagulant Wrap.asset` | 40 HoT | 0 | craft TBD |
| `brimstone_poultice` | Brimstone Poultice | NEW | `Consumables/Med/Brimstone Poultice.asset` | 30 HoT | 0 | Io craft |
| `uea_antirad_syrette` | UEA Antirad Syrette | NEW | `Consumables/Med/UEA Antirad Syrette.asset` | 0 | 0 | rad shave buff |

---

## G. O₂ & Breathables (7) — `ItemData` · Consumable

| stableId | itemName | Status | Asset path | oxygen | tooltip (draft) |
|----------|----------|--------|------------|--------|-----------------|
| `uea_o2_canister` | UEA O₂ Reserve Canister | PROTOTYPE | `Consumables/Oxygen Tank.asset` | 600 | Ten minutes breathable mix. |
| `uea_rebreather_amp` | UEA Pocket Rebreather Ampule | PROTOTYPE | `Consumables/Oxygen Tank Mini.asset` | 10 | Emergency O₂ bump. |
| `uea_o2_twin` | UEA O₂ Twin-Pack | NEW | `Consumables/Oxygen/UEA O2 Twin-Pack.asset` | 600×2 | Double canister deploy. |
| `uea_suit_bypass` | UEA Suit Bypass Cartridge | NEW | `Consumables/Oxygen/UEA Suit Bypass.asset` | 120 | Bridge failing suit seals. |
| `uea_scrubber_tablet` | UEA Scrubber Tablet | NEW | `Consumables/Oxygen/UEA Scrubber Tablet.asset` | 0 | Minor sulfur scrub (buff). |
| `uea_o2_high_pressure` | UEA High-Pressure O₂ Flask | NEW | `Consumables/Oxygen/UEA O2 Flask.asset` | 400 | Deep tube pressure mix. |
| `uea_condensate_bulb` | UEA Condensate Infusion Bulb | NEW | `Consumables/Oxygen/UEA Condensate Bulb.asset` | 25 | Small O₂ + condensate. |

---

## H. Inoculations & Gels (5) — `ItemData` · Consumable

| stableId | itemName | Status | Asset path | pressure | effect |
|----------|----------|--------|------------|----------|--------|
| `uea_rad_gel` | UEA Rad-Shimmer Gel Dose | NEW | `Consumables/Med/UEA Rad Gel.asset` | Radiation | Extend safe rad window |
| `uea_thermal_gel` | UEA Thermal Gel Dose | NEW | `Consumables/Med/UEA Thermal Gel.asset` | Heat | Caldera rim prep |
| `uea_sulfur_gel` | UEA Brimstone Filter Gel Dose | NEW | `Consumables/Med/UEA Sulfur Gel.asset` | Sulfur | Storm / plains prep |
| `uea_cold_balm` | UEA Cold-Spike Balm Dose | NEW | `Consumables/Med/UEA Cold Balm.asset` | Cold | Polar night prep |
| `uea_volcano_wafer` | UEA Volcano Wafer | NEW | `Consumables/Med/UEA Volcano Wafer.asset` | Volcano | Tremor / eruption buffer |

---

## I. Salvage & Components (7) — `ItemData` · Resource · `componentCategory`

| stableId | itemName | Status | Asset path | componentCategory | maxStack |
|----------|----------|--------|------------|-------------------|----------|
| `uea_alloy_shred` | UEA Alloy Shred | PROTOTYPE | `Components/metal_scrap.asset` | MetalScrap | 64 |
| `uea_circuit_slab` | UEA Circuit Slab | PROTOTYPE | `Components/electronic_scrap.asset` | ElectronicScrap | 64 |
| `uea_hull_plate` | UEA Hull Plate | NEW | `Components/UEA Hull Plate.asset` | MetalScrap | 32 |
| `uea_capacitor_brick` | UEA Capacitor Brick | NEW | `Components/UEA Capacitor Brick.asset` | ElectronicScrap | 32 |
| `uea_gasket_roll` | UEA Gasket Roll | NEW | `Components/UEA Gasket Roll.asset` | Unique | 32 |
| `uea_optical_shred` | UEA Optical Shred | NEW | `Components/UEA Optical Shred.asset` | ElectronicScrap | 32 |
| `uea_piston_core` | UEA Piston Core | NEW | `Components/UEA Piston Core.asset` | Unique | 16 |

---

## J. Fuel & Power (6) — `ItemData` · Resource

| stableId | itemName | Status | Asset path | maxStack | use |
|----------|----------|--------|------------|----------|-----|
| `uea_plasma_cell` | UEA Plasma Fuel Cell | PROTOTYPE | `Operations/Plasma Fuel.asset` | 32 | Hover, gens, DM-MT reload |
| `uea_fusion_cell` | UEA Micro-Fusion Cell | NEW | `Operations/UEA Fusion Cell.asset` | 16 | Building generators |
| `uea_hover_pack` | UEA Hover Charge Pack | NEW | `Operations/UEA Hover Pack.asset` | 8 | Skim-Pak refill |
| `uea_mining_drum` | UEA Mining Charge Drum | NEW | `Operations/UEA Mining Drum.asset` | 8 | DM-MT bulk reload |
| `uea_gen_brick` | UEA Generator Brick | NEW | `Operations/UEA Generator Brick.asset` | 8 | Emergency camp power |
| `uea_ion_flask` | UEA Ion Capacitor Flask | NEW | `Operations/UEA Ion Flask.asset` | 16 | Storm module shielding |

---

## K. Melee Weapons (12) — `ItemData` · MeleeWeapon

| stableId | itemName | Status | Asset path | grip | dmg | crit | notes |
|----------|----------|--------|------------|------|-----|------|-------|
| `uea_cutlass_mk2` | UEA Cutlass Mark II | PROTOTYPE | `Melee/weap2_sword.asset` | 1H | 18–28 | 0.12 | invector prefab |
| `rift_cleaver` | Rift Cleaver | PROTOTYPE | `Melee/Sword of Fear.asset` | 1H | 15–19 | 0.15 | field |
| `caldera_axe` | Caldera Axe | PROTOTYPE | `Melee/Death Axe.asset` | 1H | 18–24 | 0.18 | field |
| `vent_pike` | Vent Pike | PROTOTYPE | `Melee/Spear of Fate.asset` | 1H | 16–20 | 0.12 | field |
| `uea_breach_hatchet` | UEA Breach Hatchet Mark I | PROTOTYPE | `Melee/Wood Axe.asset` | 1H | 26–30 | 0.12 | |
| `uea_heavy_breach` | UEA Heavy Breach Blade | PROTOTYPE | `Melee/weap_two_handed.asset` | 2H | 28–40 | 0.14 | no block |
| `uea_mortal_breach` | UEA Mortal Breach Blade | PROTOTYPE | `Melee/2 Hander.asset` | 2H | 28–40 | 0.14 | veteran name |
| `uea_trench_knife` | UEA Trench Knife Mark IV | NEW | `Melee/UEA Trench Knife.asset` | 1H | 12–16 | 0.10 | fast |
| `uea_riot_baton` | UEA Riot Baton Mark II | NEW | `Melee/UEA Riot Baton.asset` | 1H | 8–12 | 0.05 | stun CC |
| `basalt_pick` | Basalt Pick | NEW | `Melee/Basalt Pick.asset` | 1H | 20–24 | 0.08 | armor shred |
| `uea_eng_maul` | UEA Engineering Maul | NEW | `Melee/UEA Eng Maul.asset` | 2H | 32–38 | 0.10 | anti-android |
| `fang_glaive` | Fang Glaive | NEW | `Melee/Fang Glaive.asset` | 2H | 22–28 | 0.14 | hound craft |

---

## L. Ranged Weapons (8) — `ItemData` · RangedWeapon

| stableId | itemName | Status | Asset path | dmg | fireRate | mag | ammo |
|----------|----------|--------|------------|-----|----------|-----|------|
| `uea_mark7` | UEA Mark-7 Sidearm | PROTOTYPE | `Ranged/sci_fi_pistol.asset` | 14–18 | 3.8 | 12 | Gunpowder |
| `uea_carbine` | UEA Expedition Carbine | PROTOTYPE | `Ranged/survival_rifle.asset` | 18–22 | 5.5 | 30 | Gunpowder + Plasma |
| `uea_dm_mt` | UEA DM-MT Laser Extractor | PROTOTYPE | `Ranged/DM_Mining_Tool.asset` | — | 12/s | 100 | Plasma cell reload |
| `uea_scatter_pulser` | UEA Mark-12 Scatter Pulser | NEW | `Ranged/UEA Scatter Pulser.asset` | 8×6 | 1.2 | 6 | Scatter flechettes |
| `uea_longwatch_dmr` | UEA Longwatch DMR | NEW | `Ranged/UEA Longwatch DMR.asset` | 28–34 | 1.5 | 8 | Sabot |
| `uea_suppression_lmg` | UEA Suppression LMG | NEW | `Ranged/UEA Suppression LMG.asset` | 16–20 | 8 | 60 | Coil |
| `uea_rivet_driver` | UEA Rivet Driver Sidearm | NEW | `Ranged/UEA Rivet Driver.asset` | 10–14 | 2 | 20 | Rivet nails |
| `uea_dart_launcher` | UEA Resonance Dart Launcher | NEW | `Ranged/UEA Dart Launcher.asset` | 5 | 1 | 4 | Resonance darts |

---

## M. Ammunition (9) — `ItemData` · Ammo

| stableId | itemName | Status | Asset path | ammoType | perPickup | rangedDmg mod |
|----------|----------|--------|------------|----------|-----------|---------------|
| `uea_ballistic_coil` | UEA Ballistic Coil Rounds | PROTOTYPE | `ammo/Standard.asset` | Gunpowder | 50 | 8 |
| `uea_plasma_t` | UEA Plasma-T Field Cartridge | PROTOTYPE | `ammo/Plasma.asset` | Plasma | 50 | 12.6 |
| `uea_pulse_pistol` | UEA Pulse Cell — Pistol Grade | PROTOTYPE | `ammo/Laser Pistol Ammo.asset` | Laser | 20 | 14 |
| `uea_pulse_rifle` | UEA Pulse Cell — Rifle Grade | PROTOTYPE | `ammo/Laser.asset` | Laser | 40 | 10 |
| `uea_scatter_flechette` | UEA Scatter Flechettes | NEW | `ammo/UEA Scatter Flechettes.asset` | Gunpowder | 24 | 6 |
| `uea_dmr_sabot` | UEA DMR Sabot Rounds | NEW | `ammo/UEA DMR Sabot.asset` | Gunpowder | 20 | 18 |
| `uea_rivet_nails` | UEA Rivet Nails (100) | NEW | `ammo/UEA Rivet Nails.asset` | Gunpowder | 100 | 6 |
| `uea_resonance_dart` | UEA Resonance Darts | NEW | `ammo/UEA Resonance Darts.asset` | Plasma | 8 | 4 |
| `uea_incendiary_coil` | UEA Incendiary Coil Rounds | NEW | `ammo/UEA Incendiary Coil.asset` | Gunpowder | 40 | 10 + burn |

---

## N. Tools (7) — `ItemData` · Tool

| stableId | itemName | Status | Asset path | toolType | key stat |
|----------|----------|--------|------------|----------|----------|
| `uea_scanner_b44` | UEA B44 Geological Survey Scanner | PROTOTYPE | `Tools/Scanner B44.asset` | Scanner | scanRange 24 |
| `uea_mark250_optics` | UEA Mark-250 Optic Ranging Glass | PROTOTYPE | `Tools/Binnos 250.asset` | Binoculars | optics FOV |
| `uea_echo_sniffer` | UEA Echo Signal Sniffer | NEW | `Tools/UEA Echo Sniffer.asset` | Scanner | Echo ping |
| `uea_thermal_rod` | UEA Thermal Survey Rod | NEW | `Tools/UEA Thermal Rod.asset` | Scanner | heat map |
| `uea_rad_compass` | UEA Rad Compass Puck | NEW | `Tools/UEA Rad Compass.asset` | Scanner | pulse timing |
| `uea_sample_vials` | UEA Sample Vial Kit | NEW | `Tools/UEA Sample Vials.asset` | Multitool | gather bonus |
| `uea_repair_multitool` | UEA Field Repair Multitool | NEW | `Tools/UEA Repair Multitool.asset` | Multitool | salvage upkeep |

---

## O. Modules (6) — `ItemData` · Resource

| stableId | itemName | Status | Asset path | special flag |
|----------|----------|--------|------------|--------------|
| `uea_loadframe` | UEA Harness Loadframe Expansion | PROTOTYPE | `Modules/Increase Storage Module.asset` | `unlocksInventoryStorageRow` |
| `uea_o2_recycler` | UEA O₂ Recycler Cartridge | NEW | `Modules/UEA O2 Recycler.asset` | suit mod TBD |
| `uea_thermal_baffle` | UEA Thermal Baffle Plate | NEW | `Modules/UEA Thermal Baffle.asset` | suit mod TBD |
| `uea_sulfur_filter` | UEA Sulfur Filter Canister | NEW | `Modules/UEA Sulfur Filter.asset` | suit mod TBD |
| `uea_rad_liner` | UEA Rad Liner Sheet | NEW | `Modules/UEA Rad Liner.asset` | suit mod TBD |
| `uea_autosort_chip` | UEA Auto-Sort Harness Chip | NEW | `Modules/UEA Autosort Chip.asset` | inventory QoL |

---

## P. Vehicles & Deployables (6)

| stableId | itemName | Status | Asset path | itemType | deployedPrefab |
|----------|----------|--------|------------|----------|----------------|
| `uea_skim_pak` | UEA Skim-Pak Hover Sled | PROTOTYPE | `Vehicles/Hovercraft.asset` | Vehicle | hover prefab |
| `uea_io_buggy` | UEA Io Buggy Chassis Kit | NEW | `Vehicles/UEA Io Buggy.asset` | Vehicle | buggy prefab W8 |
| `uea_cargo_sled` | UEA Cargo Sled Trailer | NEW | `Vehicles/UEA Cargo Sled.asset` | Vehicle | trailer prefab |
| `uea_survey_pylon` | UEA Deployable Survey Pylon | NEW | `Vehicles/UEA Survey Pylon.asset` | Consumable* | pylon prefab |
| `uea_habitat_can` | UEA Portable Habitat Canister | NEW | `Vehicles/UEA Habitat Can.asset` | Consumable* | bubble prefab |
| `uea_mine_pod` | UEA Perimeter Mine Pod | NEW | `Consumables/Throwables/UEA Mine Pod.asset` | Consumable | mine prefab |

\*Deployables may use `Consumable` + world spawn script until deploy pipeline extends.

---

## Q. Throwables (5) — `ItemData` · Consumable

| stableId | itemName | Status | Asset path | effect |
|----------|----------|--------|------------|--------|
| `uea_smoke_can` | UEA Smoke Canister Mark I | NEW | `Consumables/Throwables/UEA Smoke Can.asset` | LOS block |
| `uea_arc_grenade` | UEA Arc Flash Grenade | NEW | `Consumables/Throwables/UEA Arc Grenade.asset` | Android stagger |
| `uea_sulfur_candle` | UEA Sulfur Fog Candle | NEW | `Consumables/Throwables/UEA Sulfur Candle.asset` | Wasp repel |
| `uea_foam_capsule` | UEA Foam Wall Capsule | NEW | `Consumables/Throwables/UEA Foam Capsule.asset` | Short cover |
| `uea_noise_lure` | UEA Noise Lure Puck | NEW | `Consumables/Throwables/UEA Noise Lure.asset` | Draw patrol |

---

## R. Quest & Access (5) — `ItemData` · Quest

| stableId | itemName | Status | Asset path | quest hook |
|----------|----------|--------|------------|------------|
| `uea_crew_tag` | UEA Crew Tag | NEW | `Quest/UEA Crew Tag.asset` | B4 story POI |
| `access_shard` | Access Shard | NEW | `Quest/Access Shard.asset` | B7 vault |
| `memory_core` | Memory Core | NEW | `Quest/Memory Core.asset` | Aether-9 |
| `smuggler_chip` | Smuggler Chip | NEW | `Quest/Smuggler Chip.asset` | B5 cache |
| `survey_spool` | Survey Spool | NEW | `Quest/Survey Spool.asset` | B6 Lost Survey |

---

## Craft recipes to wire (existing + new)

| recipeId | displayName | station | output item | ingredients (draft) |
|----------|-------------|---------|-------------|---------------------|
| `grilled_mushroom` | Charred Cap | Cooking | `charred_cap` | 2× Brimstone Cap |
| `forest_stew` | Leek Stew | Cooking | `leek_stew` | 2× Cap, 1× Leeks, 1× Sulfur Bulbs |
| `Pemican_recipe` | Kelp Pemmican | Crafting | `kelp_pemmican` | 3× Bulbs, 1× Vent Kelp |
| `herbal_medpack` | Needle Salve | Crafting | `needle_salve` | 1× Needles, 2× Cap, 2× Fans |
| `stone_salve` | Basalt Salve | Crafting | `basalt_salve` NEW | 3× Basalt, 2× Silicate, 1× Needles |
| `Standard_Ammo` | Ballistic Coil | Crafting | `uea_ballistic_coil` | fix output wire |
| `Plasma_Ammo` | Plasma-T | Crafting | `uea_plasma_t` | Silicate, Alloy, Needles |
| `Plasma_Fuel` | Plasma Cell | Crafting | `uea_plasma_cell` | — |
| `craft_sci_fi_pistol` | Mark-7 | Crafting | `uea_mark7` | Alloy, Circuit, Iron |
| `craft_survival_rifle` | Carbine | Crafting | `uea_carbine` | Alloy, Circuit, Iron |
| `increase_storage_module` | Loadframe | Crafting | `uea_loadframe` | — |

---

## PPT registry rows (Things + Places sample)

| pptId | label | type | keyword |
|-------|-------|------|---------|
| `item_iron_ore` | Iron Ore | Thing | `ore_iron` |
| `item_wiffle_beans` | Wiffle Beans | Thing | `wiffle_beans` |
| `item_uea_mark7` | UEA Mark-7 Sidearm | Thing | `uea_mark7` |
| `biome_b1` | Yellowfall | Place | `biome_b1` |
| `poi_wiffle_cache` | Wiffle Cache | Place | `poi_wiffle_cache` |

*(Full PPT set in LNM §3.)*

---

## Build order (recommended)

1. Rename PROTOTYPE `itemName` + tooltips to LNM v3.1  
2. Register any missing entries in `ItemRegistry`  
3. Basic ores: add 5 new MineHarvest + nodes  
4. Io harvest: add 5 new + nodes  
5. Wiffle line (5) — loot tables only  
6. Med / O₂ / inoculation expansion  
7. Weapons / ammo pairs (author ammo before ranged)  
8. Alien + quest items last (content-gated drops)

---

## Counts

| | PROTOTYPE on disk | NEW to author | Total |
|--|-------------------|--------------|-------|
| Items | 34 | 90 | 124 |
| Resource nodes (paired) | 4 | ~10 | ~14 |
| Recipes | 11 | 4+ | 15+ |

---

*Dark Matter Studios — Io Item Asset Build Sheet v1*
