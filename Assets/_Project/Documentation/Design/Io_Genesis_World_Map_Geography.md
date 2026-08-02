# Io Surface — Plan Maps + W1 Terrain Blockout

**Status:** Plan art on disk; **W1 blockout tooling shipped** (run in Unity Editor)  
**Authority:** `Io_Biome_Exploration_Gameplay_Plan.md`, `Io_World_Content_Phase_Map.md`, IO-W1-01

These three images are the **authoritative visual plan** for Io surface layout.

| File | Purpose |
|------|---------|
| `Io_Plan_BiomeMap_TopDown.png` | Biome regions B1–B7 with labels; hot sub-Jovian south / cold anti-Jovian north |
| `Io_Plan_BiomeMap_Isometric.png` | Isometric elevation + biome color read for art direction |
| `Io_Plan_Heightmap.png` | Grayscale relief for terrain import (**0–1000 m**) |

**Unity paths (same files):**
- `Assets/_Project/World/WorldMap/` — next to `World/Terrain`
- `Assets/_Project/Documentation/Design/ArtReference/WorldMap/` — design archive copy

---

## W1 scale (locked for blockout)

| Parameter | Value |
|-----------|-------|
| Unit | **1 Unity unit = 1 m** |
| Main map span | **4096 × 4096 m** |
| Peak height | **1000 m** |
| Height source | `World/WorldMap/Io_Plan_Heightmap.png` → `World/Terrain/Io_Plan_Heightmap_R16.raw` |
| Terrain asset | `World/Terrain/Io_MainMap_W1.asset` (created by importer) |
| Scene | `Scenes/Io_MainMap_W1.unity` (created by blockout menu) |

**UV convention:** bottom-left origin, **V+ = north / anti-Jovian cold**, U+ = east. World XZ origin = map center.

| ID | Biome | Center (u, v) | Radius | Thermal bias |
|----|-------|---------------|--------|--------------|
| B6 | Basalt Highlands | (0.48, 0.60) | 0.13 | +0.1 |
| — | **Command Center** | **(0.48, 0.62)** | — | — |
| B1 | Sulfur Plains | (0.50, 0.38) | 0.12 | +0.45 |
| B2 | Geyser Fields | (0.20, 0.40) | 0.11 | +0.65 |
| B3 | Ash Flats (east primary) | (0.80, 0.55) | 0.14 | +0.2 |
| B4 | Lava Calderas | (0.48, 0.20) | 0.13 | +0.95 |
| B5 | Polar Radiation Flats (north) | (0.50, 0.92) | 0.10 | −0.85 |
| B7 | Precursor Ruin Belt | (0.50, 0.78) | 0.12 | −0.55 |

**Unlock order:** B6 → B1 → B2 → B3 → B5 → B4 → B7

---

## Editor workflow (Unity)

1. Open the project and wait for compile.
2. **Tools → Dark Matter Genesis → World → W1 Build Main Map Blockout (IO-W1-01)**  
   Or step-by-step:
   - **Create / Refresh Biome Region Assets (B1–B7)**
   - **Import Io Plan Heightmap → Terrain**
3. Open `Io_MainMap_W1` scene. Confirm:
   - Terrain relief from plan heightmap
   - `IoW1_Blockout` root: Command Center, B6 hub, path tags, shelter + mixed exposure
   - Play → map fog reveals colony + B6 sector (`IoW1BlockoutMarkers`)

---

## IO-W1-01 acceptance mapping

| Criterion | How |
|-----------|-----|
| Player / colony at Command Center | Anchor UV (0.48, 0.62); place player there in scene |
| Foot-travel into B6 hub | Path tags + continuous terrain between colony and hub |
| Boundaries align with BiomeRegionData B6 | `Data/World/Biomes/Biome_BasaltHighlands.asset` |
| B6 exposure volumes (stub OK) | `B6_MixedHazard_Exposure` from Mixed Hazard prefab |
| Map fog colony + B6 reveal test | `IoW1BlockoutMarkers.RevealColonyAndB6SectorFog` |

**Still deferred (later W1 tickets):** breach cutouts / underground pipeline (IO-W1-02), walk-in tubes (IO-W1-03), Stratum 1 kit (IO-W1-04).

---

*Dark Matter Studios — Dark Matter: Genesis — World Design*
