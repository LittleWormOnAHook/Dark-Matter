## Reviewed notes

| Piece | Location |
|---|---|
| Creator component / bake | `Assets/Genesis PCG Rock Creation/Editor/DmRockBaker.cs` (flush ClipToGround, Bake All), `DmRockBakeScheduler` (debounced auto-rebake on move) |
| Studio | `Editor/RockStudio/PcgRockStudioWindow.cs` (Rocks and Cliffs Studio), `DmRockPaletteWindow.cs` |
| Terrain match | `PcgTerrainSplat` (LayerColor, RefreshAll, ApplyFresh), `PcgTerrainBindWatcher` (auto-rebind), menu `PCG Rocks/Rebind All Rocks to Terrain` |
| Shaders | `Shaders/GenesisRockBlend.shader` (Deferred), `GenesisRockBlendForward.shader`; shared `GenesisRockBlendData/Functions/Properties.hlsl` |
| Without-TAA mode | global `_GRB_FadeNoTaaMode` (0 dither, 1 hard edge, 2 off), menu `PCG Rocks/Transparency Fade Without TAA (Scene View)/…`, EditorPrefs |

**Ground fade / shadow lift:** with *Fade Clip* plus alpha test on, the shadow pass clips below `FadeHeight × (1 + NoiseStrength) + FadeShadowLiftMargin` (margin 0.1 on most materials). This hasn't changed since Oct 3.
**Base AO Suppression** (24ax): `_GRB_BaseAOSuppress` (1), `Width` (3), `Strength` (1) lift AO and specular occlusion in the blend band.
**`_GRB_FadeNoTaaSmooth`** (24ba): default **0**. When on it removes the cut, so keep it off.

**Gotchas:** a new shader default doesn't reach materials that already serialise the value. The menus *Create Rock Blend Materials*, *Refresh Terrain Match (all rocks)* and *Bake All* touch many assets and need Anthony's OK. Never edit the BOTD/Gaia originals.
