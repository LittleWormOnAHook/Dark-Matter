# DMG Unified Item Icon Plan v1

**Status:** Phase 1 in progress — art folders created; no `ItemData`, blueprint, or UI wiring yet.  
**Scope:** 38 `ItemData` assets + **13 crafting blueprints** (`RecipeDefinition` under `Assets/_Project/Data/Crafting/Blueprints/`). Excludes `Nodes/`.  
**Preview sheet:** `DMG_Item_Icon_Sheet_Preview_v1.png` — regenerate after border pass: `python process_illustrated_icons.py --normalize-borders` (includes post-audit). All tiles show **4 px** uniform tier rings per §2.3.  
**Phase 1 deliverable count:** **51** unique 512×512 PNGs (38 items + 13 blueprints).

---

## 1. Goals

| Goal | Detail |
|------|--------|
| Unified look | Illustrated isometric 2.5D pictograms on dark slot fill — matches `DMG_Item_Icon_Sheet_Preview_v1.png` |
| Resolution | **512×512 px** master PNG per item; downsample in UI via sprite PPU |
| Rarity read | Slot **background tint / border** encodes tier; silhouette stays white |
| One icon per item | Break all duplicate GUID shares (see §5) |
| DM naming | `DM_<Category>_<ItemSlug>_Icon.png` under `_Project/Art/Icons/` |

---

## 2. Style Guide

> **Phase 1 illustrated pass (Aug 2026):** Icons are AI-generated isometric illustrations, not flat PIL silhouettes. Authoritative visual reference: `Planning/DMG_Item_Icon_Sheet_Preview_v1.png`.

### 2.1 Illustration (glyph)

| Property | Value |
|----------|-------|
| Style | **Isometric / 2.5D illustrated** — cream body with thick dark-navy strokes and internal shadow/highlight shading |
| Fill base | Warm Off-White `#EDE9E4` body; subtle warm/cool shading for depth |
| Stroke | Dark Navy `#1C2A38` outer linework, 3–6 px effective at 512 px |
| Padding | **12% inset** from cell edge (safe for 64 px hotbar) — glyph composited at **~88%** of inner plate (`GLYPH_MAX_SCALE` in `process_illustrated_icons.py`) |
| Orientation | 3/4 isometric consistent **per category** (weapons: barrel upper-right; resources: slight tilt) |
| Negative space | Holes and cutouts show slot background; no flat silhouette-only fills |
| **No inner plate** | Do **not** bake secondary beveled inner square / slate stroke into AI art — flat `#1C2A38` navy only |
| **No pedestal** | Harvest/mining resources: subject on flat navy — **no isometric slab/platform** under glyph |

**Do not** ship flat vector silhouettes (legacy `generate_phase1_icons.py` PIL drawers — deprecated for final art).

### 2.1b Legacy silhouette note (deprecated)

Prior PIL generator used flat `#EDE9E4` fills with no interior shading. Retained only for blueprint overlay tooling (`process_illustrated_icons.py`).

### 2.2 Slot plate (applied programmatically — **never bake in AI art**)

| Layer | Color | Notes |
|-------|-------|-------|
| Base fill | Dark Navy `#1C2A38` @ 100% | Flat plate only — `process_illustrated_icons.py` rebuilds on repair |
| Inner stroke | **None baked** | Legacy §2.2 inner slate stroke removed in Aug 2026 repair pass |
| Corner radius | 8% of cell | Matches Shift slot frames (outer rarity ring only) |

**Repair rules (Aug 2026):** `extract_illustrated_interior()` strips AI outer ring (24–36 px), inner plate/bevel, corner halos, and harvest-resource pedestals before compositing glyph with 12% padding.

### 2.3 Rarity backgrounds

Applied as **outer ring** (uniform **4 px** semi-thin width on every tier @ 512×512) behind silhouette (not on the glyph itself). Applied programmatically via `process_illustrated_icons.py --normalize-borders`.

| Tier | Enum | Ring color | When to use |
|------|------|------------|-------------|
| **Common** | `Common` | Slate Gray `#4A4A5A` | Starter loot, bulk resources, basic consumables, default ammo |
| **Rare** | `Rare` | Steel blue `#4A7FB5` | Crafted mid-tier, sidearms, specialist tools, named low-mid weapons |
| **Ultra Rare** | `UltraRare` | Purple `#9B59B6` | High level gates (req ≥ 6), expedition gear, mining tool, storage module |
| **Unique** | `Unique` | Gold `#D4A017` + corner ticks | Single-purpose progression items, deployables, vehicles |

**Border width:** `BORDER_WIDTH_PX = 4` in `generate_phase1_icons.py` / `process_illustrated_icons.py` — same thickness for Common through Unique; do not vary by tier.
**Palette authority:** rarity gold = `DarkMatterGenesisUiPalette.Gold`. Do **not** use legacy cyan `#63C6FF`.

### 2.4 Do / Don't

- **Do** keep ammo silhouettes distinct (bullet stack vs plasma cell vs laser rod).
- **Do** differentiate raw vs cooked food (mushroom cap vs steam lines).
- **Don't** reuse generic sword for four melee items.
- **Don't** bake item names into PNG (tooltip / slot label handles text).
- **Don't** bake inner plate frames, pedestals, or rarity rings in AI art — rings applied by `--normalize-borders` / `--repair-all`.
- **Don't** mix 3D mesh renders (current `survival_rifle_Icon.png` path) with silhouette set.

---

## 3. Folder Structure & Naming

```
Assets/_Project/Art/Icons/
├── Planning/                          # This plan + preview only
│   ├── DMG_Unified_Item_Icon_Plan_v1.md
│   ├── DMG_Item_Icon_Sheet_Preview_v1.png      # Set1 preview
│   ├── DMG_Item_Icon_Sheet_Preview_Set2_v1.png # Set2 preview
│   ├── DMG_Item_Icon_Sheet_Preview_Set3_v1.png # Set3 preview (reference-matched)
│   ├── DMG_Item_Icon_Sheet_Reference_User_v1.png # User style authority
│   └── Source/                        # Reference-extracted + AI originals (38 items)
├── Set2/                              # Conservative pass — parallel to Set1 (Aug 2026)
│   ├── Ammo/ … Vehicles/              # 51 lossless PNGs (mirrors §3 categories)
│   └── Jpeg/                          # 51 JPEG @ q95, flattened on #1C2A38
├── Set3/                              # Reference-matched pass (Aug 2026)
│   ├── Ammo/ … Vehicles/              # 51 PNGs from reference sheet extraction
│   └── Jpeg/                          # 51 JPEG @ q95
├── Ammo/
│   └── DM_Ammo_<Slug>_Icon.png        # Set1 (installed / aggressive repair pass)
```
├── Consumables/
│   └── DM_Consumable_<Slug>_Icon.png
├── Components/
│   └── DM_Component_<Slug>_Icon.png
├── Melee/
│   └── DM_Melee_<Slug>_Icon.png
├── Ranged/
│   └── DM_Ranged_<Slug>_Icon.png
├── Resources/
│   ├── Harvest/
│   └── Mining/
├── Tools/
├── Throwables/
├── Operations/
├── Modules/
├── Vehicles/
└── Blueprints/
    └── DM_Blueprint_<Slug>_Icon.png
```

**Slug rules:** PascalCase words → `Snake_Case` (e.g. `Sci-Fi Pistol` → `Sci_Fi_Pistol`). Strip legacy prefixes (`weap2_sword` → `Sword_Mk2` display, file `DM_Melee_Sword_Mk2_Icon.png`).

---

## 4. Unity Import Settings (per icon PNG)

Mirror existing `_Project/Art/Icons/survival_rifle_Icon.png.meta` with these locked values:

| Setting | Value |
|---------|-------|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single |
| Mesh Type | Full Rect |
| Pixels Per Unit | **512** (1 world unit = one icon width at 512 px) |
| Pivot | Center (0.5, 0.5) |
| Max Size | 512 |
| Mip Maps | Off |
| Filter Mode | Bilinear |
| Compression | Uncompressed (Default + Standalone) |
| Alpha is Transparency | On |
| sRGB | On |

**Optional:** create `Assets/_Project/Art/Icons/IconImportPreset.preset` in Phase 2 for batch reimport.

---

## 5. Duplicate Icon Issues (must fix)

| Shared GUID | Source asset (known) | Items sharing it | Count | Planned fix |
|-------------|---------------------|------------------|-------|-------------|
| `4638050222352034880def4aac84f246` | `Assets/_Project/Art/Icons/icons8-sword-100.png` | 2 Hander, Two-Handed Sword, weap2_sword, Sword of Fear | **4** | Four unique silhouettes: cleaver, longsword, short sword, notched blade |
| `01f306e51ae729b4dbc6cd5279f0e6e7` | `Assets/Survival Flat Icons…/mushroom.png` | Mushroom, Cooked Mushroom | **2** | Raw cap vs grilled cap + steam |
| `cbf720c275e518644ae08d241225e93a` | *(third-party / missing meta in scan)* | Quora Shelter (Consumable), Quora Shelter (Resource) | **2** | Deployed tent (Unique) vs blueprint scroll/crate (Rare) |
| `87659ae3b00c1174cb502ce5f5604634` | *(shared placeholder)* | Plasma Fuel, Increase Storage Module | **2** | Fuel canister vs rack/module chip |

**Net new art:** 38 unique icons − 32 unique GUIDs today = **6** minimum new drawings; **4** duplicate groups ⇒ **+6** replacements = **38** total deliverables.

---

## 6. Proposed `ItemRarity` Enum

Add to `Assets/_Project/Scripts/Data/ItemData.cs` (Phase 3 — not implemented yet):

```csharp
public enum ItemRarity
{
    Common = 0,    // Slate gray ring (#4A4A5A)
    Rare = 1,      // Blue ring
    UltraRare = 2, // Purple ring
    Unique = 3     // Gold ring
}
```

**Field (Phase 3):**

```csharp
[Header("Presentation")]
public ItemRarity rarity = ItemRarity.Common;
[Tooltip("Optional override; empty = auto from rarity.")]
public Sprite icon;
```

**Runtime (Phase 4):** `InventorySlotUI` reads `item.rarity` → tints `backgroundImage` or spawns rarity frame child; icon image stays white (`Color.white`).

**Editor (Phase 3):** custom inspector drawer with color swatch; bulk assign tool from this plan CSV.

---

## 7. Rarity Assignment — All 38 Items

| # | Item | Asset path | Tier | Rationale |
|---|------|------------|------|-----------|
| 1 | Rock | Consumables/Rock | Common | Junk / crafting filler; no gates |
| 2 | Mushroom | Consumables/Mushroom | Common | Raw forage starter |
| 3 | Cooked Mushroom | Consumables/Cooked Mushroom | Common | Basic crafted food |
| 4 | Red Lilly | Consumables/Red Lilly | Common | Forage consumable |
| 5 | Forest Stew | Consumables/Forest Stew | Common | Mid stew; widely craftable |
| 6 | Pimican | Consumables/Pimican | Common | Standard ration |
| 7 | Medpack | Consumables/Medpack | Common | Field heal; early game |
| 8 | Bio Gel | Consumables/Bio_Gel | **Rare** | Triple restore + `requiredLevelToUse: 5` |
| 9 | Oxygen Tank Mini | Consumables/Oxygen Tank Mini | Common | Small O₂ refill |
| 10 | Oxygen Tank | Consumables/Oxygen Tank | **Rare** | Full cylinder; significant restore |
| 11 | Quora Shelter | Consumables/Quora Shelter | **Unique** | Deployable shelter; massive multi-stat restore on use |
| 12 | Frag Grenade | Throwables/DM_Frag_Grenade | **Rare** | Tactical throwable; not starter loot |
| 13 | Metal Scrap | Components/metal_scrap | Common | Bulk craft component |
| 14 | Electronic Scrap | Components/electronic_scrap | Common | Bulk craft component |
| 15 | Iron Ore | Resources/Mining/Iron Ore | Common | Base mining yield |
| 16 | Silicate Ore | Resources/Mining/Silicate Ore | Common | Base mining yield |
| 17 | Web Plant | Resources/Harvest/Web Plant | Common | Common harvest fiber |
| 18 | Brimstone Blade | Resources/Harvest/Brimstone Blade | Common | Hazard harvest |
| 19 | Sulfur Needle Tuft | Resources/Harvest/Sulfur Needle Tuft | Common | Hazard harvest |
| 20 | Plasma Fuel | Operations/Plasma Fuel | **Rare** | Powers mining tool; ops loop |
| 21 | Quora Shelter | Resources/Quora Shelter | **Rare** | Crafted blueprint; L5 pickup / L10 craft gates |
| 22 | Wood Axe | Melee/Wood Axe | Common | Starter gather weapon (high dmg but L1) |
| 23 | Spear of Fate | Melee/Spear of Fate | **Rare** | Named melee; mid stats |
| 24 | weap2_sword | Melee/weap2_sword | **Rare** | Mid sword (18 dmg) |
| 25 | Sword of Fear | Melee/Sword of Fear | **Rare** | Named sword; crit focus |
| 26 | 2 Hander | Melee/2 Hander | **UltraRare** | `requiredLevelToEquip: 6`; top cleaver |
| 27 | Two-Handed Sword | Melee/weap_two_handed | **UltraRare** | `requiredLevelToEquip: 7`; same power tier |
| 28 | Death Axe | Melee/Death Axe | **UltraRare** | `requiredLevelToEquip: 7`; endgame axe |
| 29 | Sci-Fi Pistol | Ranged/sci_fi_pistol | **Rare** | Energy sidearm |
| 30 | Survival Rifle | Ranged/survival_rifle | **Rare** | `requiredLevelToEquip: 2`; primary ranged |
| 31 | DM Mining Tool | Ranged/DM_Mining_Tool | **UltraRare** | Core mining loop; unique behavior |
| 32 | Standard | Ammo/Standard | Common | Default ammo |
| 33 | Plasma | Ammo/Plasma | **Rare** | Higher damage energy rounds |
| 34 | Laser Pistol Ammo | Ammo/Laser Pistol Ammo | **Rare** | Hitscan specialty ammo |
| 35 | Scanner B44 | Tools/Scanner B44 | Common | Starter scan tool |
| 36 | Binoculars 250 | Tools/Binnos 250 | **Rare** | Optics / zoom tool |
| 37 | Hovercraft | Vehicles/Hovercraft | **Unique** | Only vehicle item; `requiredLevelToEquip: 10` |
| 38 | Increase Storage Module | Modules/Increase Storage Module | **UltraRare** | Unlocks inventory row; L10 equip gate |

**Tier counts:** Common 18 · Rare 13 · Ultra Rare 5 · Unique 2

---

## 8. Icon Filename Map (planned)

| Item | Planned PNG |
|------|-------------|
| Rock | `Consumables/DM_Consumable_Rock_Icon.png` |
| Mushroom | `Consumables/DM_Consumable_Mushroom_Raw_Icon.png` |
| Cooked Mushroom | `Consumables/DM_Consumable_Mushroom_Cooked_Icon.png` |
| Red Lilly | `Consumables/DM_Consumable_Red_Lilly_Icon.png` |
| Forest Stew | `Consumables/DM_Consumable_Forest_Stew_Icon.png` |
| Pimican | `Consumables/DM_Consumable_Pimican_Icon.png` |
| Medpack | `Consumables/DM_Consumable_Medpack_Icon.png` |
| Bio Gel | `Consumables/DM_Consumable_Bio_Gel_Icon.png` |
| Oxygen Tank Mini | `Consumables/DM_Consumable_Oxygen_Tank_Mini_Icon.png` |
| Oxygen Tank | `Consumables/DM_Consumable_Oxygen_Tank_Icon.png` |
| Quora Shelter (use) | `Consumables/DM_Consumable_Quora_Shelter_Icon.png` |
| Frag Grenade | `Throwables/DM_Throwable_Frag_Grenade_Icon.png` |
| Metal Scrap | `Components/DM_Component_Metal_Scrap_Icon.png` |
| Electronic Scrap | `Components/DM_Component_Electronic_Scrap_Icon.png` |
| Iron Ore | `Resources/Mining/DM_Resource_Iron_Ore_Icon.png` |
| Silicate Ore | `Resources/Mining/DM_Resource_Silicate_Ore_Icon.png` |
| Web Plant | `Resources/Harvest/DM_Resource_Web_Plant_Icon.png` |
| Brimstone Blade | `Resources/Harvest/DM_Resource_Brimstone_Blade_Icon.png` |
| Sulfur Needle Tuft | `Resources/Harvest/DM_Resource_Sulfur_Needle_Tuft_Icon.png` |
| Plasma Fuel | `Operations/DM_Ops_Plasma_Fuel_Icon.png` |
| Quora Shelter (craft) | `Resources/DM_Resource_Quora_Shelter_Blueprint_Icon.png` |
| Wood Axe | `Melee/DM_Melee_Wood_Axe_Icon.png` |
| Spear of Fate | `Melee/DM_Melee_Spear_Of_Fate_Icon.png` |
| weap2_sword | `Melee/DM_Melee_Sword_Mk2_Icon.png` |
| Sword of Fear | `Melee/DM_Melee_Sword_Of_Fear_Icon.png` |
| 2 Hander | `Melee/DM_Melee_Two_Hander_Cleaver_Icon.png` |
| Two-Handed Sword | `Melee/DM_Melee_Two_Handed_Sword_Icon.png` |
| Death Axe | `Melee/DM_Melee_Death_Axe_Icon.png` |
| Sci-Fi Pistol | `Ranged/DM_Ranged_Sci_Fi_Pistol_Icon.png` |
| Survival Rifle | `Ranged/DM_Ranged_Survival_Rifle_Icon.png` |
| DM Mining Tool | `Ranged/DM_Ranged_Mining_Tool_Icon.png` |
| Standard | `Ammo/DM_Ammo_Standard_Icon.png` |
| Plasma | `Ammo/DM_Ammo_Plasma_Icon.png` |
| Laser Pistol Ammo | `Ammo/DM_Ammo_Laser_Pistol_Icon.png` |
| Scanner B44 | `Tools/DM_Tool_Scanner_B44_Icon.png` |
| Binoculars 250 | `Tools/DM_Tool_Binoculars_250_Icon.png` |
| Hovercraft | `Vehicles/DM_Vehicle_Hovercraft_Icon.png` |
| Increase Storage Module | `Modules/DM_Module_Storage_Row_Icon.png` |

---

## 8b. Crafting Blueprint Catalog (Phase 1 scope)

**Source:** `Assets/_Project/Data/Crafting/Blueprints/` — 13 `RecipeDefinition` assets registered in `BlueprintRegistry.asset`.  
**Orphan pickup (no asset):** `BlueprintPickup_craft_gunpowder_rounds.prefab` references `craft_gunpowder_rounds` — not in registry; excluded until asset exists.

### Blueprint icon style (extends §2)

| Layer | Detail |
|-------|--------|
| Base slot | Same Dark Navy plate + Slate inner stroke as items (§2.2) |
| Primary glyph | **Identical drawer + scale to crafted output item** — full-size warm off-white silhouette (§2.1); player must recognize the item at a glance |
| Blueprint cue | **Secondary only:** light steel-blue grid wash (`#4A7FB5` @ 18%, sparse step) **behind** glyph + thin L-shaped corner marks on top; **no** scroll fold, schematic line-art, or shrunken glyph |
| Rarity ring | Baked per §2.3 — tier follows **output item** rarity (§7) unless noted |
| Naming | `Blueprints/DM_Blueprint_<Slug>_Icon.png` |
| Generator | `Planning/generate_phase1_icons.py` — `BLUEPRINT_OUTPUT_MAP` links each blueprint PNG → output item PNG; run `--blueprints-only` to refresh |

**Rule:** blueprint icons = **same** item silhouette as the crafted output, plus subtle blueprint wash/corners. Do **not** use a separate schematic art pass or smaller glyph (legacy 60% scale removed Aug 2026).

### Blueprint inventory (13)

| # | Asset | recipeId | Display name | Crafts (output) | Source item icon | Tier | Planned PNG |
|---|-------|----------|--------------|-----------------|------------------|------|-------------|
| 1 | `Consumables/grilled_mushroom` | grilled_mushroom | Grilled Mushroom | Cooked Mushroom | `Consumables/DM_Consumable_Mushroom_Cooked_Icon.png` | Common | `DM_Blueprint_Grilled_Mushroom_Icon.png` |
| 2 | `Consumables/forest_stew` | forest_stew | Forest Stew | Forest Stew | `Consumables/DM_Consumable_Forest_Stew_Icon.png` | Common | `DM_Blueprint_Forest_Stew_Icon.png` |
| 3 | `Consumables/stone_salve` | stone_salve | Stone Salve | Plasma *(asset output)* | `Ammo/DM_Ammo_Plasma_Icon.png` | Rare | `DM_Blueprint_Stone_Salve_Icon.png` |
| 4 | `Consumables/herbal_medpack` | herbal_medpack | Herbal Medpack | Medpack | `Consumables/DM_Consumable_Medpack_Icon.png` | Common | `DM_Blueprint_Herbal_Medpack_Icon.png` |
| 5 | `Consumables/Bio_Gel` | Bio_Gel | Bio Gel | Bio Gel | `Consumables/DM_Consumable_Bio_Gel_Icon.png` | Rare | `DM_Blueprint_Bio_Gel_Icon.png` |
| 6 | `Consumables/Pemican_Lilly` | Pimican_recipe | Pemican | Pimican | `Consumables/DM_Consumable_Pimican_Icon.png` | Common | `DM_Blueprint_Pimican_Icon.png` |
| 7 | `Ammo/Standard_Ammo` | Standard_Ammo | Standard Ammo | Standard *(asset refs Plasma GUID — data bug)* | `Ammo/DM_Ammo_Plasma_Icon.png` *(follows disk outputItem)* | Common | `DM_Blueprint_Standard_Ammo_Icon.png` |
| 8 | `Ammo/Plasma_Ammo` | Plasma_Ammo | Plasma T | Plasma | `Ammo/DM_Ammo_Plasma_Icon.png` | Rare | `DM_Blueprint_Plasma_Ammo_Icon.png` |
| 9 | `Resources/Plasma_Fuel` | Plasma_Fuel | Plasma Fuel | Plasma Fuel | `Operations/DM_Ops_Plasma_Fuel_Icon.png` | Rare | `DM_Blueprint_Plasma_Fuel_Icon.png` |
| 10 | `Resources/Quora_Shelter` | Quora Shelter | Quora Temporary Shelter | Quora Shelter (resource) | `Resources/DM_Resource_Quora_Shelter_Blueprint_Icon.png` | Rare | `DM_Blueprint_Quora_Shelter_Icon.png` |
| 11 | `Weapons/craft_sci_fi_pistol` | craft_sci_fi_pistol | Sci-Fi Pistol | Sci-Fi Pistol | `Ranged/DM_Ranged_Sci_Fi_Pistol_Icon.png` | Rare | `DM_Blueprint_Sci_Fi_Pistol_Icon.png` |
| 12 | `Weapons/craft_survival_rifle` | craft_survival_rifle | Survival Rifle | Survival Rifle | `Ranged/DM_Ranged_Survival_Rifle_Icon.png` | Rare | `DM_Blueprint_Survival_Rifle_Icon.png` |
| 13 | `Modules/increase_storage_module` | increase_storage_module | Increase Storage Module | Increase Storage Module | `Modules/DM_Module_Storage_Row_Icon.png` | UltraRare | `DM_Blueprint_Storage_Module_Icon.png` |

**Blueprint duplicate / placeholder issues:** 4 blueprints share third-party icons with items or each other (mushroom, aidkit×2, Plasma icon×2, plastic_bottle). All replaced in Phase 1.

**Tier counts (blueprints):** Common 5 · Rare 7 · Ultra Rare 1 · Unique 0

**Combined Phase 1 total:** 38 items + 13 blueprints = **51 icons**

### 8c. Illustrated production workflow (Phase 1 pass)

| Step | Tool | Notes |
|------|------|-------|
| 1. Style lock | `DMG_Item_Icon_Sheet_Preview_v1.png` | Attach as reference on every AI gen |
| 2. Item art | Cursor `GenerateImage` (1 icon / call) | Isometric cream + dark strokes + baked rarity ring |
| 3. Install | `install_illustrated_batch.py` | Copy 512 PNG → `Assets/_Project/Art/Icons/<category>/` |
| 4. Blueprints | `process_illustrated_icons.py --blueprints-only` | Grid wash + corner marks over output item art |
| 5. Normalize | PIL resize → 512×512 if needed | PPU 512 import preset §4 |

**Future items:** reuse the prompt template in `Planning/generate_illustrated_icons.py` (prompt constants per category). Always attach preview sheet reference. Run blueprint overlay script — never redraw blueprint glyphs separately.

---

## 3b. Set 2 — Conservative icon pass (Aug 2026)

**Why:** Set1 `--repair-all` uses aggressive glyph masking (pedestal CC removal, navy-stroke stripping) that can fade or punch holes in illustrated linework. Set2 keeps full AI source fidelity.

| Aspect | Set1 (category folders) | Set2 (`Set2/` + `Set2/Jpeg/`) |
|--------|-------------------------|-------------------------------|
| Source | `Planning/Source/` → aggressive mask | `Planning/Source/` → Cursor cache only; **never** Set1 PNG |
| Strip | 36 px outer AI ring | Same 36 px ring strip only |
| Mask | Pedestal/tray CC + stroke grow | Visible non-ring pixels only |
| Scale | 88% on flat navy plate | Same |
| Rarity ring | Programmatic 4 px | Same |
| Corners | Slot frame (outer ring radius) | **Rounded alpha clip** (`CORNER_R`) baked in PNG + JPEG |
| PNG | Category folders (Set1) | `Set2/<category>/DM_*_Icon.png` — lossless, compress_level=1 |
| JPEG | — | `Set2/Jpeg/<category>/DM_*_Icon.jpg` — RGB @ `#1C2A38`, quality **95** |
| Preview | `DMG_Item_Icon_Sheet_Preview_v1.png` | `DMG_Item_Icon_Sheet_Preview_Set2_v1.png` |
| Unity wiring | Phase 2 target (Set1 today) | **Not wired** — review Set2 preview first |

**CLI:**

```bash
python process_illustrated_icons.py --build-set2
python process_illustrated_icons.py --audit-borders --set2
```

**When to use Set2:** Phase 2 import when visual QA prefers cleaner glyphs (Wood Axe, Medpack, Brimstone, Hovercraft spot-check). Set1 remains untouched until explicit swap.

**Deliverable count:** 51 PNG + 51 JPEG + 51 `.meta` (PNG only) = **153 files** under `Set2/`.

---

## 3c. Set 3 — Reference-matched pass (Aug 2026)

**Why:** User reference sheet `Planning/DMG_Item_Icon_Sheet_Reference_User_v1.png` is the style authority. Set1/Set2 AI sources drift (grain, pedestals, inner plate). Set3 extracts all **38** reference cells and rebuilds with minimal processing.

| Aspect | Set1 | Set2 | Set3 (`Set3/` + `Set3/Jpeg/`) |
|--------|------|------|-------------------------------|
| Style source | AI gen + repair | AI Source / cache | **Reference sheet extraction** |
| Extract script | — | — | `extract_reference_sheet.py` |
| Strip | Aggressive mask | 36 px ring only | Ring + bottom label band; **no 36 px inset** |
| Scale | 88% flat navy | 88% | 88% (`extract_glyph_reference`) |
| Preview | `DMG_Item_Icon_Sheet_Preview_v1.png` | `…Set2_v1.png` | `DMG_Item_Icon_Sheet_Preview_Set3_v1.png` |
| Unity wiring | Set1 today | Review only | **Not wired** — swap after QA |

**CLI:**

```bash
python extract_reference_sheet.py
python process_illustrated_icons.py --build-set3
python process_illustrated_icons.py --audit-borders --set3
```

**Reference sheet layout (38 items):** Row1 resources/ops · Row2 consumables · Row3 melee · Row4 ranged/tools/ammo · Row5 Hovercraft / Storage Module / Quora deploy / Quora blueprint wireframe (explicit crop boxes).

**Deliverable count:** 51 PNG + 51 JPEG + 51 `.meta` = **153 files** under `Set3/`.

---

## 3d. HD atlas — 4096×4096 reference pass (Aug 2026)

**Why 512px not 125px:** 51 icons at 125px = only **2 rows** on a 4096 canvas (~6% height) — icons looked like a tiny top strip. **4096 ÷ 512 = 8** → **512×512 cells** in an **8×8 grid** uses **88%** of sheet height.

| Aspect | Recommended (v4) |
|--------|------------------|
| Canvas | **4096×4096 px** |
| Cell size | **512×512 px** (also export **128** / **256** for UI) |
| Grid | **8×8** = 64 slots, 51 filled |
| Source | `DMG_Item_Icon_Sheet_Reference_User_v2.png` **only** |
| Master | `DMG_Item_Icon_Sheet_4096_512px_Master_v4.png` |
| Slice | `DMG_Item_Icon_Sheet_4096_512px_Slice_v4.png` |
| Individuals | `ReferenceExtract_512/`, `ReferenceExtract_128/` |

```bash
python build_reference_atlas_4096.py                      # 512 master + 128 export
python build_reference_atlas_4096.py --cell-size 256 --also-export 128
```

**125px note:** 4096 is not divisible by 125; use **128px** for compact UI downscale instead.

**Ammo distinct silhouettes (verified):**
- Standard — fanned bullet cartridge stack (Common)
- Plasma — rectangular energy cell with bolt motif (Rare)
- Laser Pistol Ammo — bundled slim charge rods (Rare)

---

## 9. New Item Checklist (human + AI agent)

Use this whenever adding a **new** `ItemData`, blueprint, or pickup that needs an inventory icon. Do **not** bulk-regenerate existing Phase 1 icons unless explicitly requested.

### 9.1 Authoring (Unity / design)

| Step | Action |
|------|--------|
| 1 | Create `ItemData` (or blueprint) with final `itemName`, `itemType`, and level gates. |
| 2 | Pick **category folder** + slug → `DM_<Category>_<Slug>_Icon.png` (§3). |
| 3 | Assign **rarity tier** from §7/§8b or heuristics (Common / Rare / UltraRare / Unique). |
| 4 | Run **Tools → Dark Matter Genesis → Art → Create Icon For Selected ItemData** — copies checklist + target path. |

### 9.2 Art generation (Cursor agent)

| Step | Action |
|------|--------|
| 1 | Attach style reference: `Planning/DMG_Item_Icon_Sheet_Preview_v1.png` |
| 2 | Prepend style anchor from `generate_illustrated_icons.py` (512×512 isometric cream + dark strokes + navy tile). |
| 3 | Add subject description + baked rarity ring suffix for tier. |
| 4 | Generate **one** 512×512 PNG; save to `Planning/Source/<slug>.png`. |

### 9.3 Install & wire

| Step | Action |
|------|--------|
| 1 | `python process_illustrated_icons.py --from-dir Planning/Source` |
| 2 | Blueprint only: `python process_illustrated_icons.py --blueprints-only` |
| 3 | Confirm Unity import §4 (PPU 512, uncompressed). |
| 4 | Assign `ItemData.icon` — unique GUID per item (no sharing). |
| 5 | Fix any `[ItemData Icon]` console warnings before commit. |

### 9.4 Validation rules (automated)

`ItemData` logs a warning on save when:

- `icon` is null or missing
- Sprite path is outside `Assets/_Project/Art/Icons/`
- Legacy paths: `Ammo_*.png`, `icons8`, `Survival Flat Icons`, Invector folders
- Source texture is not 512×512

**Editor menu:** `Tools/Dark Matter Genesis/Art/Create Icon For Selected ItemData`  
**Cursor rule:** `.cursor/rules/dmg-illustrated-item-icons.mdc`

---

## 10. Phased Implementation Plan

### Phase 0 — Approval (now)
- [ ] Review preview sheet + rarity table
- [ ] Confirm tier counts and Quora Shelter dual-art direction
- [ ] Confirm baked rarity ring **in PNG** vs **runtime slot tint**

### Phase 1 — Art production
- [x] Create folder structure §3 + `Blueprints/`
- [x] Export **51×** 512 px PNGs (38 items §8 + 13 blueprints §8b) — **illustrated isometric pass**
- [x] Fix 4 item duplicate GUID groups with distinct silhouettes (priority)
- [x] Replace third-party `Survival Flat Icons` / `icons8` / colored ammo deps (art files ready; wiring Phase 2+)
- [x] Blueprint icons match output item art; subtle grid wash + corner marks only
- [x] Ammo trio distinct: bullet stack (Standard), plasma cell (Plasma), laser rods (Laser Pistol)
- [ ] Keep `Planning/` preview; add `Source/` PSD or SVG if available

### Phase 2 — Unity import
- [ ] Drop PNGs into folder structure §3
- [ ] Apply import preset §4 (PPU 512, uncompressed)
- [ ] Optional: `ItemIconRegistry` ScriptableObject mapping `stableItemId` → Sprite

### Phase 3 — Data & editor
- [ ] Add `ItemRarity` enum + field on `ItemData`
- [ ] Bulk assign rarity from §7 (Editor script reading this doc or CSV)
- [ ] Wire `icon` references to new sprites (one GUID per item)
- [ ] `ItemDataEditor` rarity swatch + validation (warn on duplicate icons)

### Phase 4 — UI wiring
- [ ] `InventorySlotUI` / `RecipeCraftSlotUI`: rarity frame on `backgroundImage`
- [ ] Tooltip: optional rarity label ("Ultra Rare") in MutedText
- [ ] Verify hotbar @ 64 px and journal @ 80–96 px scales

### Phase 5 — QA & cleanup
- [ ] Visual pass in `Dark Matter Genesis v1.56.unity` play mode
- [ ] Console error check before commit
- [ ] Deprecate unused legacy icons (do not delete third-party package originals)

---

## 11. Open Questions for Approval

1. **Baked vs runtime rarity ring** — Preview shows baked rings; runtime tint allows palette tweaks without re-export.
2. **weap2_sword display name** — Rename to `Sword Mk2` (or similar) when icons land?
3. **Ammo folder art** — Existing colored `Ammo_*.png` set: replace entirely with white silhouettes for consistency?
4. **Quora Shelter** — Confirm Unique (deployable) vs Rare (resource blueprint) split matches design intent.

---

*Generated for Dark Matter: Genesis — planning pass v1. No ItemData or scene changes included.*
