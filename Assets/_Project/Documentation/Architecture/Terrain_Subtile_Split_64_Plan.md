# Terrain Subtile Split (16 → 64) — Plan

**Status:** Planned (not implemented)  
**Date:** 2026-09-11  
**Owner:** World / Gaia streaming  
**Does not ship this pass.** Do not split tiles or rewrite `TerrainScenes.asset` until a named follow-up asks for Phase 1 (single-tile pilot).

## Goal

Slice each of the **16** DM Genesis Gaia tiles into **4** subtiles (**64** terrain scenes + matching impostors / content) so Terrain Loader Manager (TLM) can keep **less full terrain area** resident while walking. World extent stays **8192 × 8192 m**.

## Current disk truth (do not contradict)

| Item | Value |
|------|--------|
| Grid | **4 × 4 = 16** additive `Terrain_*.unity` scenes |
| Tile size | **2048 m** (`BindDmGenesisGaiaTerrainScenes`) |
| Origin | `m_pos00X/Z = -4096` |
| Storage | `Assets/Gaia User Data/Sessions/DM Genesis/TerrainScenes.asset` |
| Scenes dir | `Assets/Gaia User Data/Sessions/DM Genesis/Terrain Scenes/` |
| Runtime streamer | **Gaia `TerrainLoaderManager`** in playable Genesis scenes |
| Impostors | `BakeTlmImpostors` — one bake set per current tile |
| Neighbor quality | `PioneerGaiaTerrainFollow` on `Player_v7` (hero vs cheap neighbors; **does not load tiles**) |
| `_Project` `TerrainChunkStreamer` | **Not** on playable scenes. Hierarchy-baked worlds only. |

**One owner:** Gaia TLM + `TerrainScenes.asset`. Do **not** run `TerrainChunkStreamer` on the same tiles as TLM.

Disabling Gaia `m_terrainLoadingEnabled` without a replacement loader would unload the expedition world. `TerrainChunkStreamer` default `updateInterval` (0.25 s) does **not** affect the current Gaia pipeline.

## Target

| Item | Value |
|------|--------|
| Grid | **8 × 8 = 64** additive terrain scenes |
| Tile size | **1024 m** |
| Naming | `Terrain_0_0` … `Terrain_7_7` (keep `Terrain_(\d+)_(\d+)` regex) |
| Storage | Same asset; `m_terrainTilesX/Z = 8`, `m_terrainTilesSize = 1024`, same origin |
| Impostors | Rebake **64** TLM impostor scenes (or keep 16 coarse impostors only if a pilot proves that is enough) |
| Loaded at play | Aim **4–9** subtiles, not “load more because they are smaller” |
| Content | Optional later: per-subtile content scenes keyed to the same grid |

World-space coords and saves stay valid if indices map 1:1 to position (`tileX = floor((x - originX) / 1024)`).

## Why (performance)

Walking FPS is often **HDRP terrain cull/draw** (`TerrainManager.CullAllTerrains`, HD render graph) plus TLM `Update`, not locomotion C#. Smaller stream units cut **heightmap / splat / foliage / collider** work on the loaded set.

### Approximate savings (terrain-heavy outdoor only)

There is no 4× FPS from 4× scene count. You trade one large tile for more scenes, stitches, and load events.

**Area model (if TLM keeps ~4 mega-tiles today):**

- Today: ~4 × (2048 m)² ≈ **16 km²** of full terrain in play.
- After split, **4–6** subtiles: ~**4–6 km²** → about **50–65%** less loaded terrain area.
- After split, **9** subtiles (3×3): ~**9 km²** → about **~40%** less area.

**Expected walking frame-time change (terrain-bound views):**

| Scenario | Approx. vs today |
|----------|------------------|
| Optimistic (~4–6 subtiles, GPU/cull dominated) | **~25–40%** lower terrain-related frame time |
| Realistic (6–9 subtiles, extra load/stitch) | **~15–25%** |
| Pessimistic (too many loaded, hitchy TLM) | **~0–10%** or **worse hitches** until ranges tuned |

UITK / HDRP non-terrain cost does **not** scale with this split. Peak memory often **better**; disk/build size **up** (64 scenes + 64 impostor assets).

## Implementation phases

### Phase 0 — Locks (now)

1. Keep **Gaia TLM** as the only terrain streamer on expedition scenes.
2. Do not add `TerrainChunkStreamer` to v1.6.x playable scenes.
3. Confirm in Play: no `TerrainChunkStreamer`; `Terrain.activeTerrains.Length` stays small (~4), not 16.
4. Baseline one walking Profiler frame: terrain count, TLM load range, HDRP terrain ms.

### Phase 1 — Single-tile pilot (first implementation ask)

Split **one** 2048 m tile into **four** 1024 m quadrants:

1. Copy height + splat + layers into four `TerrainData` assets.
2. Four additive scenes + four storage entries (or a sidecar list until full rebind).
3. Stitch neighbors (Gaia stitch / existing `_Project` splat-edge tools).
4. Move trees / details / objects into the correct quadrant scenes.
5. Impostor bake for those four cells (or keep the parent impostor for distance).
6. Play: seams, load count, hitch vs FPS.

**Go / no-go:** proceed to 64 only if active terrain area / terrain ms drop without worse hitching.

### Phase 2 — Full 64 + storage + TLM

1. Split remaining 15 tiles the same way.
2. Rebuild `TerrainScenes.asset` (64 entries, 8×8, 1024 m).
3. Run **Bind Gaia Terrain Scenes** (`BindDmGenesisGaiaTerrainScenes`) and add all 64 scenes to **Build Settings**.
4. Update bind constants (`m_terrainTilesX/Z`, `m_terrainTilesSize`) — do not leave 4×4 / 2048 hardcoded after cutover.
5. Tune TLM **player load range** so Play does not load 12–16 subtiles.

### Phase 3 — Impostors

Re-run / extend `BakeTlmImpostors` for 64 cells. Align impostor loading bounds with TLM so distant Io uses impostors, not full terrains.

### Phase 4 — Runtime polish (existing `_Project` paths)

- Keep `PioneerGaiaTerrainFollow` (hero full quality; neighbors cheap shadows/basemap/colliders).
- Pixel error **25** on terrains (existing bind menu). Optional: slightly higher PE on non-hero subtiles after pilot.
- Re-profile walking: active count, TLM load/unload spikes, HDRP terrain.

### Phase 5 — Optional content scenes

Per-subtile content (rocks, loot, POI) keyed to the same 8×8 grid. Load with the terrain scene or a thin content toggle — **not** a second heightmap streamer.

## Do / do not

**Do**

- Pilot one tile before a 64-scene commit.
- Keep world origin and floating-point fix as today.
- Treat editor split + content move + impostor rebake as a **multi-day world pipeline**, not a small C# patch.

**Do not**

- Dual-run TLM and `TerrainChunkStreamer` on the same tiles.
- Disable Gaia runtime loading “to use `_Project` streamer” on the current expedition scenes.
- Retune `Player_v7` capsule / physics for this work.
- Add NavMesh streaming as part of the split (`TerrainChunkStreamer.streamNavMeshData` stays off).

## Related tools / files

| Path | Role |
|------|------|
| `Assets/_Project/Editor/World/BindDmGenesisGaiaTerrainScenes.cs` | Bind 4×4 / 2048 storage to TLM (must update at cutover) |
| `Assets/_Project/Editor/World/BakeTlmImpostors.cs` | Impostor bake + “4 terrains + impostors” editor helper |
| `Assets/_Project/Editor/World/BindGaiaPlayerTerrainLoader.cs` | Ensures `PioneerGaiaTerrainFollow` on `Player_v7` |
| `Assets/_Project/Scripts/World/PioneerGaiaTerrainFollow.cs` | Quality on already-loaded tiles |
| `Assets/_Project/Scripts/World/TerrainChunkStreamer.cs` | Alternate streamer — unused on current playable scenes |
| Playable scenes | `Dark Matter Genesis v1.6.3.1.unity` / `v1.6.4.unity` — TLM present |

Future editor work (when Phase 1 is requested): `SplitDmGenesisTerrainQuadrant` menu under Dark Matter Genesis / World — heightmap quadrant copy + new `TerrainData` + scene stubs. Not written yet.

## Suggested first implementation ask

> Phase 1 pilot: split one named 2048 m tile (`Terrain_X_Y`) into four 1024 m scenes, stitch, play-test seams and `Terrain.activeTerrains` count. No full 64 cutover.
