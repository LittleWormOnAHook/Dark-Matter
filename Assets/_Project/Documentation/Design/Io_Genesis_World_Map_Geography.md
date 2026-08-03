# Io Surface — Plan Maps (Art Reference Only)

**Status:** Plan mode — **visual reference only**. Future Io plan TerrainData is a separate asset; Pioneer prototype Terrain is unchanged.  
**Authority:** `Io_Biome_Exploration_Gameplay_Plan.md`, `Io_World_Content_Phase_Map.md`

These three images are the **authoritative visual plan** for Io surface layout before W1 terrain blockout.

| Spec | Value |
|------|-------|
| Image resolution | **4096 × 4096** (4K square) |
| Future Io plan terrain size | **2048 × 2048** world units (separate asset) |
| Future Io plan terrain Height (primary) | **100** (peaks ~25–40 units; same RAW at Height 1000 → ~250–400) |
| Unity-ready heightmap pixels | **2048 × 2048** smooth PNG/RAW on disk |
| TerrainData.heightmapResolution | usually **2049** (2ⁿ+1) when sculpting in Unity — see Import.txt |

**Do not** resize the Pioneer prototype scene Terrain for these specs. Prototype Mesh Resolution stays at its current values (e.g. 500 × 500, height 600).

| File | Purpose |
|------|---------|
| `Io_Plan_BiomeMap_TopDown.png` | Biome regions B1–B7 mapped with labels, hot sub-Jovian south / cold anti-Jovian north (label authority) |
| `Io_Plan_BiomeMap_Isometric.png` | Isometric elevation + biome color read; on-map B1–B7 region labels + lower-left index/legend |
| `Io_Plan_Heightmap.png` | **Art** shaded-relief height reference only (lighting/shadows — not Import Raw elevation) |

**Unity-ready elevation (separate from art):**

| File | Purpose |
|------|---------|
| `World/Terrain/Io_Plan_Heightmap_Unity.png` | **2048×2048** 16-bit grayscale elevation (black=ocean, white=high) |
| `World/Terrain/Io_Plan_Heightmap_Unity.raw` | **2048×2048** 16-bit little-endian — Terrain **Import Raw…** |
| `World/Terrain/Io_Plan_Heightmap_Unity_Import.txt` | Import settings + 2048 vs 2049 notes |
| `World/Terrain/Io_Plan_Terrain.asset` | Optional separate TerrainData shell (2048×2048×1000 m) |

**Import Raw…:** Bit 16, Windows (LE), Width/Height **2048**, terrain **Width 2048 / Length 2048 / Height 100**. See `Io_Plan_Heightmap_Unity_Import.txt`.

**Biome labels (B1–B7):**
| ID | Name |
|----|------|
| B1 | Sulfur Plains |
| B2 | Geyser Fields |
| B3 | Ash Flats & Ridges |
| B4 | Lava Calderas |
| B5 | Polar Radiation Flats |
| B6 | Basalt Highlands (hub) |
| B7 | Precursor Ruin Belt |

**Unity paths (same files):**
- `Assets/_Project/World/WorldMap/` — art plan maps
- `Assets/_Project/World/Terrain/` — Unity heightmaps + `Io_Plan_Terrain.asset`
- `Assets/_Project/Documentation/Design/ArtReference/WorldMap/` — design archive copy

**Editor setup:**
- `Tools → Dark Matter Genesis → Scene → Setup Io Plan Terrain Shell (2048 / 1000m)` — flat asset shell only (no SetHeights)

Does **not** assign to or modify the Pioneer prototype Terrain. Apply heights via Import Raw after restart.

**Not included:** runtime UI textures, `BiomeRegionData`, or breach cutouts.

**W1 next:** paint splatmaps / place exposure volumes per biome plan; refine sculpt from Unity heightmap.

---

*Dark Matter Studios — Dark Matter: Genesis — World Design*
