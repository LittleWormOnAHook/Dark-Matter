# Terrain Content Reflection Probes (v1)

Companion scenes pair with Gaia **regular** terrain tiles only. Impostor, collider, and backup terrain scenes never load content.

## Scene naming

| Gaia terrain (additive) | Companion content scene |
|-------------------------|-------------------------|
| `Terrain_0_0-<timestamp>` | `Assets/_Project/Scenes/Terrain_0_0_Content.unity` |
| `Terrain_1_2-<timestamp>` | `Assets/_Project/Scenes/Terrain_1_2_Content.unity` |

Grid: `0_0` through `3_3` (4×4 tiles, 2048 m each, origin at -4096, -4096).

## Runtime

- `DmTerrainContentSceneLoader` loads/unloads `Terrain_X_Y_Content` when matching regular Gaia tiles stream in/out.
- `DmReflectionProbeRingManager` disables `BOTD Reflection Probe(Clone)` and tiers chunk probes by **distance to the probe**:
  - ≤ 150 m: enabled, importance 1
  - 150–400 m: enabled, importance 0 / weight 0.5 (HDRP)
  - > 400 m or tile unloaded: disabled
- Probes without baked cubemaps stay disabled until content is baked.

## Authoring workflow (later — do not bake yet)

When Anthony finishes placing props in a content scene:

1. Open master scene `Dark Matter Genesis v1.6.unity`.
2. Load **one** regular Gaia terrain tile (e.g. `Terrain_0_0-*`) additively — not all 16, not impostors.
3. Open the matching `Terrain_X_Y_Content` scene additively for editing.
4. Move `DmChunkProbe` to the prefab cluster center; scale the influence box to cover the cluster only (~150 m outdoor default).
5. Configure probe: **Baked**, resolution **128**, far clip **80–150**, **Render Dynamic Objects** off, **box projection** off for outdoor Io, exclude terrain from capture mask.
6. Bake that one content scene. Do not bake the 10 km BOTD world probe.
7. Leave runtime tiering enabled; manager will activate probes only when baked and within distance rings.

## Editor setup

Menu: **Dark Matter Genesis → World → Create Terrain Content Scenes**

Creates/refreshes all 16 placeholder scenes with disabled `DmChunkProbe` objects and adds them to Editor Build Settings.

## v1 placeholders

Current content scenes contain a disabled `DmChunkProbe` (baked settings, no cubemap). No Main Camera, no Directional Light. Props and rebakes are authored later.
