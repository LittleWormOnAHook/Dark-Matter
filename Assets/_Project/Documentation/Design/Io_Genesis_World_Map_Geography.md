# Io Genesis World Map — Geography Spec

**Status:** Phase W1 authoring foundation (prototype map art + data scaffold)  
**Authority:** GDD 5.0 Appendix A2; `Io_World_Content_Phase_Map.md` W0–W1  
**Companion:** `Io_Biome_Exploration_Gameplay_Plan.md` §2.5 (full-scale surface map)

---

## 1. Purpose

Authoritative geography for the **full-scale Io surface main map** — the single persistent overworld that hosts B1–B7, the Command Center colony, and surface breach anchors. This spec drives:

- Procedural **top-down** and **isometric** placeholder map art (`GenesisMoonMap_*.png`)
- `BiomeRegionData` ScriptableObject placement (UV centers, unlock order, pressure tags)
- Future terrain blockout and streaming region boundaries

**Deferred (not in this pass):** underground breach holes, instanced load volumes, final heightmap terrain sculpt.

---

## 2. Celestial layout (tidally locked Io)

Io is **tidally locked** to Jupiter. Map UV space uses a **square texture** with a **circular moon disc** mask.

| Map axis | In-world meaning |
|----------|------------------|
| **+U (east / right)** | **Sub-Jovian hemisphere** — Jupiter dominates sky; persistent dayside heat |
| **−U (west / left)** | **Anti-Jovian hemisphere** — cold nightside; polar radiation flats |
| **+V (north / up)** | North polar cap band |
| **−V (south / down)** | South equatorial ruin belt approach |

**Terminator band:** diagonal soft gradient from NW (cold/dark) to SE (hot/light). Gameplay day/night cycle still runs globally; this is geographic lighting bias, not a separate level.

**Real Io diameter:** ~3,643 km. Gameplay scale is **1 Unity unit = 1 meter** on the eventual main map; prototype flat terrain remains 512×512 m until W1 terrain blockout lands.

---

## 3. Elevation model

| Parameter | Value | Notes |
|-----------|-------|-------|
| **Max peak height** | **1,000 m** | User-directed cap for Genesis map art (overrides older 200–300 m blockout note until terrain pass rebakes) |
| **Highland core (B6)** | 400–1,000 m | Semi-mountain ranges, hub vistas, tube skylights |
| **Caldera rims (B4)** | 300–900 m | Volcanic rims and obsidian spires |
| **Ash ridges (B3)** | 150–500 m | Wind-scoured ridges |
| **Plains (B1, B2, B5)** | 0–200 m | Flats with local vent bumps |
| **Ruin belt (B7)** | 50–350 m | Low resonance mesas |

Elevation is encoded in map art as **height shading** (top-down) and **relief** (isometric). Runtime heightmap terrain will be authored separately in W1.

---

## 4. Biome placement (B1–B7)

Regions are **soft Voronoi cells** blended with fBm noise — no hard pixel borders in placeholder art.

| ID | Biome | Map UV center (u, v) | Radius | Palette | Dominant pressure |
|----|-------|----------------------|--------|---------|-------------------|
| B6 | Basalt Highlands (hub) | (0.52, 0.58) | 0.18 | ash-bronze | Mixed — colony hub |
| B1 | Sulfur Plains | (0.72, 0.42) | 0.16 | sulfur-amber | Sulfur |
| B2 | Geyser Fields | (0.78, 0.62) | 0.14 | sulfur-amber | Sulfur + Volcano |
| B3 | Ash Flats & Ridges | (0.48, 0.28) | 0.17 | ash-bronze | Thermal + Volcano |
| B4 | Lava Calderas | (0.68, 0.52) | 0.13 | heat-obsidian | Volcano + Heat |
| B5 | Polar Radiation Flats | (0.22, 0.50) | 0.15 | polar-rad | Radiation + Cold |
| B7 | Precursor Ruin Belt | (0.50, 0.22) | 0.12 | aether-teal | Radiation + Resonance |

**Campaign unlock order:** B6 → B1 → B2 → B3 → B5 → B4 → B7 (per `Io_World_Content_Phase_Map.md`).

**Command Center:** anchored inside B6 at approximately **(0.50, 0.56)** — starter colony flats.

---

## 5. Palette reference (life sheets)

| Token | Hex (base) | Hex (accent) | Usage |
|-------|------------|--------------|-------|
| sulfur-amber | `#8B6914` | `#D4A017` | B1, B2 |
| ash-bronze | `#4A4038` | `#8C7362` | B3, B6 |
| heat-obsidian | `#1A1212` | `#5C2E2E` | B4 |
| polar-rad | `#2A3040` | `#6B7FA8` | B5 |
| aether-teal | `#1E4A4A` | `#3D8B8B` | B7 |
| vacuum | `#0A0A12` | — | Off-disc space |

---

## 6. Generated assets

| File | Resolution | Role |
|------|------------|------|
| `Textures/WorldMap/GenesisMoonMap_TopDown.png` | 2048×2048 | Primary world map texture (UI, design) |
| `Textures/WorldMap/GenesisMoonMap_Isometric.png` | 2048×2048 | Isometric reference / future map mode |
| `Resources/UI/GenesisMoonMap.png` | 2048×2048 | Runtime Resources load for `WorldMapProvider` |

**Regenerate:**

```bash
python3 Assets/_Project/Tools/WorldMap/generate_genesis_moon_maps.py
```

---

## 7. Data scaffold

- `BiomeRegionData` — per-biome SO: region ID, display name, map UV, unlock order, verb tags, vehicle flags
- `BiomeRegionRegistry` — resolves all B1–B7 regions for directors and map fog

Editor: **Tools → Dark Matter Genesis → World → Create Biome Region Assets (B1–B7)**

---

## 8. Out of scope (this pass)

- Underground breach anchor placement
- NavMesh / terrain heightmap sculpt for full 3600 km map
- Map fog of war sector unlock wiring
- Streaming sub-scenes per biome

---

*Dark Matter Studios — Dark Matter: Genesis — World Design*
