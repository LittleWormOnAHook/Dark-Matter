# Io World Map — Geography & Map Art Plan

**Status:** Design + blockout art foundation (August 2026)  
**Authority:** GDD 5.0 Chapter 3; `Io_Biome_Exploration_Gameplay_Plan.md`; `Io_World_Content_Phase_Map.md`  
**Art output:** `ArtReference/WorldMap/` — top-view + isometric reference sheets  
**Generator:** `Tools/WorldMap/generate_io_world_map.py` (unified — also produces 2048 UI textures)

---

## 1. Scope

Build the **entire Io surface** as one persistent main map with:

| Layer | Purpose |
|-------|---------|
| **Top view** | UI world map, fog-of-war, biome sectors, POI placement |
| **Isometric view** | Art direction reference — elevation read, mountain silhouettes, breach mouths |
| **Height field** | 0–**1000 m** peaks (semi-mountain ranges; not Everest-scale) |
| **Thermal axis** | Sub-Jovian hot → anti-Jovian cold (tidal lock) |
| **Illumination axis** | Jupiter-lit hemisphere vs shadow hemisphere (day/night cycle still runs in-game) |
| **Underground holes** | Reserved breach markers (no terrain cutouts yet — W1 pipeline) |

This document **extends** the biome plan with **full-moon geography**. It does not replace B1–B7 identity tables.

---

## 2. Full-moon model (tidally locked)

Io is **tidally locked to Jupiter**. The playable map is the **full spherical surface** unfolded for design as a **4096×4096** square with a circular mask (orthographic “globe stamp”).

```
                    NORTH POLE (B5 Polar)
                           ▲
                           │
     LEADING (rad) ◄───────┼───────► TRAILING (rad + wreck drift)
                           │
              ┌────────────┴────────────┐
              │   ANTI-JOVIAN (dark)    │  ← cold, low sun, B7 Ruin Belt
              │         COLD            │
              └────────────┬────────────┘
                           │
              ┌────────────┴────────────┐
              │   SUB-JOVIAN (light)    │  ← hot, Jupiter glow, B4 Calderas
              │         HOT             │
              └────────────┬────────────┘
                           │
                    SOUTH POLE (B5 Polar)
```

### 2.1 Axes (orthogonal — both matter)

| Axis | Map direction | Gameplay |
|------|---------------|----------|
| **Sub-Jovian ↔ Anti-Jovian** | Bottom ↔ Top (map north = anti-Jovian) | Heat gradient; B4 hot core sub-Jovian; B7 on anti-Jovian equator |
| **Poles ↔ Equator** | Top/bottom rim ↔ center band | B5 polar flats; stronger rad + cold at night |
| **Leading ↔ Trailing** | Left ↔ Right | Jovian radiation pulse bias; Expedition Graveyard drift on trailing edge |

### 2.2 Light vs dark hemisphere

- **Light hemisphere:** sub-Jovian + leading wedge — Jupiter dominates sky, sulfur haze glow.  
- **Dark hemisphere:** anti-Jovian + trailing wedge — starlight + faint Jupiter rim; **polar night** hits hardest on B5.  
- In-game **day/night cycle** still rotates over this baseline (locked July 2026).

### 2.3 Elevation (updated August 2026)

| Tier | Height | Distribution |
|------|--------|--------------|
| **Valley floor** | 0–80 m | B1 flats, B2 vent basins, polar sinks |
| **Rolling highland** | 80–350 m | B6 hub, B3 ash corridors |
| **Ridge / semi-range** | 350–700 m | B3 ridges, B6 skylight rims |
| **Peak** | 700–**1000 m** | Rare — 3–5 named ranges, Io Buggy paths only |

*Prior design note of 200–300 m superseded for world-map blockout; gameplay streaming can LOD-blend distant peaks.*

---

## 3. Surface biome placement (B1–B7)

Approximate **normalized map UV** (0–1, origin bottom-left, circular mask applied):

| ID | Biome | Region shape | Center UV (x, y) | Notes |
|----|-------|--------------|------------------|-------|
| B6 | Basalt Highlands | Sub-Jovian equatorial hub | (0.50, 0.22) | **Command Center** anchor; colony + skylight tubes |
| B4 | Lava Calderas | Sub-Jovian hot spot | (0.50, 0.12) | Foot-only; caldera bowl |
| B2 | Geyser Fields | Ring around B4 | (0.42, 0.18) / (0.58, 0.16) | Split clusters |
| B1 | Sulfur Plains | Equatorial band | (0.30, 0.35) / (0.70, 0.30) | Wide flats, shallow seeps |
| B3 | Ash Flats & Ridges | Mountain transition | (0.25, 0.45) / (0.75, 0.42) | Ridges up to 1000 m |
| B5 | Polar Radiation Flats | North + south caps | (0.50, 0.92) / (0.50, 0.08) | Foot-only; night cold teach |
| B7 | Precursor Ruin Belt | Anti-Jovian equator | (0.50, 0.78) | Radiation + resonance |

**Overlay:** Expedition Graveyard — trailing hemisphere streak (x > 0.55, y 0.35–0.65).

### 3.1 Campaign unlock flow (unchanged)

B6 → B1 → B2 → B3 → **B5** → **B4** → B7

---

## 4. Underground breach reservations (future holes)

No terrain cutouts in blockout art — **marked circles** only. Types:

| Kind | Stratum | Example placement |
|------|---------|-------------------|
| Walk-in tube mouth | S1 | B6 hub, colony |
| Instanced breach | S1–S2 | B1 seeps, B3 foothills |
| Deep breach | S3–S5 | B4 collapse sink, B5 rad tube, B7 vault |

Blockout PNG layer: `IoWorldMap_BreachMarkers.png` (gold dots, 8 px at 4K).

---

## 5. Map art deliverables

| Asset | Size | Use |
|-------|------|-----|
| `IoWorldMap_TopView_4K.png` | 4096×4096 | Primary UI map texture candidate |
| `IoWorldMap_TopView_4K_BiomeMask.png` | 4096×4096 | R = biome ID 0–7 per pixel |
| `IoWorldMap_TopView_4K_Height.png` | 4096×4096 | Grayscale 0–1000 m |
| `IoWorldMap_IsoView_4K.png` | 4096×4096 | Art reference — 30° isometric |
| `IoWorldMap_Legend.png` | 1536×1024 | Biome color key + axes |

Palette aligns with life sheets: sulfur-amber, ash-bronze, heat-obsidian, polar-rad, aether-teal.

---

## 6. Engineering hooks (W0/W1)

| Item | Path | Status |
|------|------|--------|
| Biome colors | `Scripts/Map/IoWorldMapPalette.cs` | This pass |
| Region data SO | `Scripts/Data/BiomeRegionData.cs` | This pass |
| Map override | `WorldMapProvider.mapTextureOverride` | Wire in W1 |
| Sector fog | `MapFogOfWar` + biome mask sample | W1 |
| Terrain holes | Terrain API cutouts at breach UV | W1+ |

---

## 7. Regeneration

```bash
python3 Assets/_Project/Tools/WorldMap/generate_io_world_map.py
```

Commit regenerated PNGs + `.meta` when layout constants change.
