# Io Genesis World Map — Geography Spec (merged)

**Status:** Phase W0/W1 authoring foundation — **merged PR #13 + #14** (August 2026)  
**Authority:** GDD 5.0 Appendix A2; `Io_World_Content_Phase_Map.md` W0–W1  
**Companion:** `Io_World_Map_Geography_Plan.md`, `Io_Biome_Exploration_Gameplay_Plan.md` §2.5

---

## 1. Purpose

Authoritative geography for the **full-scale Io surface main map** — one persistent overworld for B1–B7, Command Center, and breach anchors.

**Generator (unified):**

```bash
python3 Assets/_Project/Tools/WorldMap/generate_io_world_map.py
```

---

## 2. Tidally locked axes (hot vs cold)

| Map direction | In-world meaning | Biomes |
|---------------|------------------|--------|
| **Bottom (low V)** | **Sub-Jovian — HOT** | B4 Calderas, B2 Geysers, B1 Sulfur |
| **Top (high V)** | **Anti-Jovian — COLD** | B7 Ruin Belt |
| **North / south rim** | **Polar caps — COLD** | B5 Polar Flats (both poles) |
| **Center sub-Jovian** | Hub | B6 Basalt Highlands + **Command Center (0.50, 0.22)** |
| **Left ↔ right** | Leading ↔ trailing radiation | Graveyard drift on trailing edge |

---

## 3. Elevation

| Tier | Height |
|------|--------|
| Max peaks | **1,000 m** |
| B6 Highlands | 400–1,000 m |
| B4 Calderas | 300–900 m (bowl sink at core) |
| B3 Ash ridges | 150–500 m |
| B1/B2/B5 plains | 0–200 m |
| B7 Ruins | 50–350 m |

---

## 4. Biome UV placement (normalized 0–1, bottom-left origin)

| ID | Biome | Center (u, v) | Radius | Thermal bias | Pressure |
|----|-------|---------------|--------|--------------|----------|
| B6 | Basalt Highlands | (0.50, 0.22) | 0.14 | +0.1 | Mixed hub |
| B1 | Sulfur Plains | (0.50, 0.30) | 0.16 | +0.45 | Sulfur (hot) |
| B2 | Geyser Fields | (0.50, 0.17) | 0.12 | +0.65 | Sulfur + volcano (hot) |
| B3 | Ash Flats | (0.50, 0.43) | 0.14 | +0.2 | Thermal + volcano |
| B5 | Polar Flats | (0.50, 0.90) | 0.11 | **−0.85** | Rad + **cold** (north cap; south mirrored in art) |
| B4 | Lava Calderas | (0.50, 0.12) | 0.10 | **+0.95** | Volcano + **heat** |
| B7 | Ruin Belt | (0.50, 0.78) | 0.13 | **−0.55** | Rad + resonance (**cold**) |

**Unlock order:** B6 → B1 → B2 → B3 → B5 → B4 → B7

---

## 5. Generated assets

| Output | Path | Size |
|--------|------|------|
| Top view (4K) | `ArtReference/WorldMap/IoWorldMap_TopView_4K.png` | 4096 |
| Isometric (4K) | `ArtReference/WorldMap/IoWorldMap_IsoView_4K.png` | 4096 |
| Biome mask | `ArtReference/WorldMap/IoWorldMap_TopView_4K_BiomeMask.png` | 4096 |
| Height field | `ArtReference/WorldMap/IoWorldMap_TopView_4K_Height.png` | 4096 |
| Legend | `ArtReference/WorldMap/IoWorldMap_Legend.png` | 1536×1024 |
| UI top-down | `Textures/WorldMap/GenesisMoonMap_TopDown.png` | 2048 |
| UI isometric | `Textures/WorldMap/GenesisMoonMap_Isometric.png` | 2048 |
| Runtime map | `Resources/UI/GenesisMoonMap.png` | 2048 |

---

## 6. Deferred (W1+)

- Underground breach terrain cutouts (markers only in art)
- Full heightmap terrain sculpt + streaming
- Map fog-of-war sector unlock

---

*Dark Matter Studios — Dark Matter: Genesis — World Design*
