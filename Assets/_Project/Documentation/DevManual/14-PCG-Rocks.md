# PCG Rocks & Rock Blend

**Status:** Full (generated from code + reviewed notes)  
**Open:** Tools → Dark Matter Genesis → PCG Rocks → *Rocks and Cliffs Studio* / *Rock Palette*

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
## Menus

- `GameObject/Genesis PCG Rock Creation/DM PCG Creator (Random)` — `Assets/Genesis PCG Rock Creation/Editor/DmRockCombinerEditor.cs`
- `GameObject/Genesis PCG Rock Creation/DM PCG Creator (from Selection)` — `Assets/Genesis PCG Rock Creation/Editor/DmRockCombinerEditor.cs`
- `Tools/Dark Matter Genesis/PCG Rocks/Bake All DM PCG Creators in Scene` — `Assets/Genesis PCG Rock Creation/Editor/DmRockBaker.cs`
- `Tools/Dark Matter Genesis/PCG Rocks/Build Style + Preset Library` — `Assets/Genesis PCG Rock Creation/Editor/DmRockLibrary.cs`
- `Tools/Dark Matter Genesis/PCG Rocks/Clean Unused Baked Meshes` — `Assets/Genesis PCG Rock Creation/Editor/DmRockBaker.cs`
- `Tools/Dark Matter Genesis/PCG Rocks/Create Rock Blend Materials` — `Assets/Genesis PCG Rock Creation/Editor/DmRockBlendMaterials.cs`
- `Tools/Dark Matter Genesis/PCG Rocks/Rebind All Rocks to Terrain` — `Assets/Genesis PCG Rock Creation/Editor/PcgTerrainBindWatcher.cs`
- `Tools/Dark Matter Genesis/PCG Rocks/Refresh Default Kit` — `Assets/Genesis PCG Rock Creation/Editor/DmRockCombinerEditor.cs`
- `Tools/Dark Matter Genesis/PCG Rocks/Refresh Terrain Match (all rocks)` — `Assets/Genesis PCG Rock Creation/Editor/DmRockBlendMaterials.cs`
- `Tools/Dark Matter Genesis/PCG Rocks/Repair rock colliders (all)` — `Assets/Genesis PCG Rock Creation/Editor/DmRockColliderRepair.cs`
- `Tools/Dark Matter Genesis/PCG Rocks/Report Out-of-Date Bakes in Scene` — `Assets/Genesis PCG Rock Creation/Editor/DmRockBaker.cs`
- `Tools/Dark Matter Genesis/PCG Rocks/Rock Palette` — `Assets/Genesis PCG Rock Creation/Editor/DmRockPaletteWindow.cs`

## Shader `Genesis PCG/Rock Blend Lit`

- File: `Assets/Genesis PCG Rock Creation/Shaders/GenesisRockBlend.shader`
- Inspector: `GenesisRockBlendGUI`

| Property | Label | Type / Range | Default |
|---|---|---|---|
| `_GRB_HeightRange` | Legacy: vertex A height range (fixed 4 m in the shader, unused) | Float | 4 |
| `_GRB_SizeRange` | Legacy: vertex B size range (fixed 8 m in the shader, unused) | Float | 8 |
| `_GRB_BaseLumRef` | Base average luminance (set on bake) | Float | 0.08 |
| `_GRB_VColorRange` | Legacy: vertex R range (m) | Float | 0.5 |
| `_GRB_TopEnable` | Top Layer | Float | 1 |
| `_GRB_TopFromTerrain` | Top Uses Terrain Layer on Bake (wind-blown ground) | Float | 0 |
| `_GRB_TopBaseMap` | Top Albedo | 2D | "white" {} |
| `_GRB_TopColor` | Top Color | Color | (1,1,1,1) |
| `_GRB_TopNormalMap` | Top Normal | 2D | "bump" {} |
| `_GRB_TopNormalScale` | Top Normal Intensity | Range(0, 4) | 1 |
| `_GRB_TopHeightMap` | Top Height | 2D | "gray" {} |
| `_GRB_TopParallax` | Top Parallax (Top Height as height map, m) | Range(0, 0.15) | 0 |
| `_GRB_TopMaskMap` | Top Mask (R metallic, G AO, B detail, A smoothness) | 2D | "white" {} |
| `_GRB_TopMetallicMin` | Top Metallic Remap Min | Range(0, 1) | 0 |
| `_GRB_TopMetallicMax` | Top Metallic Remap Max | Range(0, 1) | 1 |
| `_GRB_TopSmoothMin` | Top Smoothness Remap Min | Range(0, 1) | 0 |
| `_GRB_TopSmoothMax` | Top Smoothness Remap Max | Range(0, 1) | 1 |
| `_GRB_TopAOMap` | Top AO | 2D | "white" {} |
| `_GRB_TopAOStrength` | Top AO Strength | Range(0, 1) | 1 |
| `_GRB_TopMaskOn` | Top Mask Assigned (auto) | Float | 0 |
| `_GRB_TopAOOn` | Top AO Assigned (auto) | Float | 0 |
| `_GRB_TopTint` | Top Tint A | Color | (1,1,1,1) |
| `_GRB_TopTint2` | Top Tint B (noise variation) | Color | (1,1,1,1) |
| `_GRB_TopVarScale` | Tint Variation Scale (per m) | Float | 0.7 |
| `_GRB_TopValueVar` | Value Variation | Range(0, 1) | 0.15 |
| `_GRB_TopSaturation` | Saturation | Range(0, 2) | 1 |
| `_GRB_TopBrightness` | Brightness | Range(0, 2) | 1 |
| `_GRB_TopTiling` | Top Tiling (tiles per m) | Float | 0.5 |
| `_GRB_TopSmoothness` | Top Smoothness | Range(0, 1) | 0.3 |
| `_GRB_TopCoverage` | Top Coverage | Range(0, 1) | 0.5 |
| `_GRB_TopSharpness` | Top Edge Sharpness | Range(1, 32) | 8 |
| `_GRB_TopSlopeStart` | Slope Start (normal.y) | Range(0, 1) | 0.45 |
| `_GRB_TopSlopeEnd` | Slope Full (normal.y) | Range(0, 1) | 0.8 |
| `_GRB_TopCreviceBias` | Collect in Crevices / Ledges (base AO) | Range(0, 2) | 0.5 |
| `_GRB_TopHeightContrast` | Top Height Blend | Range(0, 2) | 0.6 |
| `_GRB_TopVertexWeight` | Vertex G (exposure) vs pixel normal | Range(0, 1) | 0.3 |
| `_GRB_TopNoiseScale` | Top Noise Scale (per m) | Float | 0.6 |
| `_GRB_TopNoiseStrength` | Top Noise Strength | Range(0, 1) | 0.35 |
| `_GRB_TopNormalBlend` | Top Layer Normal Strength | Range(0, 1) | 0.6 |
| `_GRB_TopThroughMin` | Base Normal Through Top Layer: Min (smooth patches) | Range(0, 1) | 0.35 |
| `_GRB_TopThroughMax` | Base Normal Through Top Layer: Max (bumpy patches) | Range(0, 1) | 1 |
| `_GRB_TopThroughNoiseScale` | Base Normal Through Top: Noise Scale (per m) | Float | 0.7 |
| `_GRB_TopThroughContrast` | Base Normal Through Top: Noise Contrast | Range(0.5, 6) | 2.5 |
| `_GRB_TopMaxWeight` | Top Max Weight | Range(0, 1) | 1 |
| `_GRB_TopMinPieceSize` | Min Piece Size (m, smaller pieces get none) | Float | 0 |
| `_GRB_BottomEnable` | Terrain Blend at Base | Float | 1 |
| `_GRB_TerrainAuto` | Match Terrain Layer on Bake | Float | 1 |
| `_GRB_TerrainBaseMap` | Terrain Albedo | 2D | "white" {} |
| `_GRB_TerrainNormalMap` | Terrain Normal | 2D | "bump" {} |
| `_GRB_TerrainTint` | Terrain Tint | Color | (1,1,1,1) |
| `_GRB_TerrainTiling` | Terrain Tiling (tiles per m = 1 / layer tile size) | Float | 0.5 |
| `_GRB_TerrainUVOffset` | Terrain UV Offset (xy, set on bake) | Vector | (0,0,0,0) |
| `_GRB_TerrainNormalScale` | Terrain Normal Strength | Range(0, 2) | 1 |
| `_GRB_TerrainSmoothness` | Terrain Smoothness | Range(0, 1) | 0.15 |
| `_GRB_TerrainPerPixel` | Match Terrain Per Pixel (terrain splat maps under the rock, updates on move) | Float | 1 |
| `_GRB_TParams` |  | Vector | (0,0,0,0) |
| `_GRB_TRect` |  | Vector | (0,0,0,0) |
| `_GRB_TControlTS` |  | Vector | (0,0,0,0) |
| `_GRB_THeightP` |  | Vector | (0,0,0,0) |
| `_GRB_THeightTS` |  | Vector | (0,0,0,0) |
| `_GRB_TST0` |  | Vector | (0,0,0,0) |
| `_GRB_TST1` |  | Vector | (0,0,0,0) |
| `_GRB_TST2` |  | Vector | (0,0,0,0) |
| `_GRB_TST3` |  | Vector | (0,0,0,0) |
| `_GRB_TST4` |  | Vector | (0,0,0,0) |
| `_GRB_TST5` |  | Vector | (0,0,0,0) |
| `_GRB_TST6` |  | Vector | (0,0,0,0) |
| `_GRB_TST7` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff0` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff1` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff2` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff3` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff4` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff5` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff6` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff7` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff0` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff1` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff2` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff3` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff4` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff5` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff6` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff7` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale0` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale1` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale2` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale3` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale4` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale5` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale6` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale7` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer0` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer1` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer2` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer3` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer4` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer5` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer6` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer7` |  | Vector | (0,0,0,0) |
| `_GRB_TControl0` |  | 2D | "black" {} |
| `_GRB_TControl1` |  | 2D | "black" {} |
| `_GRB_THeightMap` |  | 2D | "black" {} |
| `_GRB_TAlb0` |  | 2D | "white" {} |
| `_GRB_TNrm0` |  | 2D | "bump" {} |
| `_GRB_TMsk0` |  | 2D | "white" {} |
| `_GRB_TAlb1` |  | 2D | "white" {} |
| `_GRB_TNrm1` |  | 2D | "bump" {} |
| `_GRB_TMsk1` |  | 2D | "white" {} |
| `_GRB_TAlb2` |  | 2D | "white" {} |
| `_GRB_TNrm2` |  | 2D | "bump" {} |
| `_GRB_TMsk2` |  | 2D | "white" {} |
| `_GRB_TAlb3` |  | 2D | "white" {} |
| `_GRB_TNrm3` |  | 2D | "bump" {} |
| `_GRB_TMsk3` |  | 2D | "white" {} |
| `_GRB_TAlb4` |  | 2D | "white" {} |
| `_GRB_TNrm4` |  | 2D | "bump" {} |
| `_GRB_TMsk4` |  | 2D | "white" {} |
| `_GRB_TAlb5` |  | 2D | "white" {} |
| `_GRB_TNrm5` |  | 2D | "bump" {} |
| `_GRB_TMsk5` |  | 2D | "white" {} |
| `_GRB_TAlb6` |  | 2D | "white" {} |
| `_GRB_TNrm6` |  | 2D | "bump" {} |
| `_GRB_TMsk6` |  | 2D | "white" {} |
| `_GRB_TAlb7` |  | 2D | "white" {} |
| `_GRB_TNrm7` |  | 2D | "bump" {} |
| `_GRB_TMsk7` |  | 2D | "white" {} |
| `_GRB_BottomThroughMin` | Base Normal Through Terrain Blend: Min | Range(0, 1) | 0.15 |
| `_GRB_BottomThroughMax` | Base Normal Through Terrain Blend: Max | Range(0, 1) | 0.75 |
| `_GRB_BottomThroughNoiseScale` | Base Normal Through Terrain: Noise Scale (per m) | Float | 1 |
| `_GRB_BottomThroughContrast` | Base Normal Through Terrain: Noise Contrast | Range(0.5, 6) | 2.5 |
| `_GRB_BottomHeight` | Blend Height (m) | Range(0, 4) | 0.2 |
| `_GRB_BottomFalloff` | Blend Falloff (m) | Range(0.005, 1) | 0.08 |
| `_GRB_BottomSharpness` | Contact Sharpness | Range(1, 16) | 3 |
| `_GRB_BottomNoiseScale` | Blend Noise Scale (per m) | Float | 1.5 |
| `_GRB_BottomNoiseStrength` | Blend Noise Strength | Range(0, 1) | 0.5 |
| `_GRB_BottomEdgeNoiseScale` | Contact Line Jitter Scale (per m) | Float | 7 |
| `_GRB_BottomEdgeNoise` | Contact Line Jitter (m) | Range(0, 0.5) | 0.05 |
| `_GRB_BottomUpBias` | Up-facing Bias | Range(0, 1) | 0.5 |
| `_GRB_BottomMaxWeight` | Max Blend Weight | Range(0, 1) | 0.9 |
| `_GRB_BottomPieceScale` | Small Piece Cap (blend height <= piece size x this) | Range(0, 2) | 0.25 |
| `_GRB_StrataEnable` | World Strata Bands | Float | 0 |
| `_GRB_StrataColorA` | Band Colour A (absolute albedo) | Color | (0.30,0.27,0.24,1) |
| `_GRB_StrataColorB` | Band Colour B (absolute albedo) | Color | (0.55,0.49,0.41,1) |
| `_GRB_StrataDetail` | Keep Base Texture Detail | Range(0, 1) | 0.7 |
| `_GRB_StrataStrength` | Strata Strength | Range(0, 1) | 0.6 |
| `_GRB_StrataSpacing` | Band Spacing (m) | Float | 0.6 |
| `_GRB_StrataThickVar` | Band Thickness Variation | Range(0, 1) | 0.5 |
| `_GRB_StrataTilt` | Band Tilt (deg) | Range(0, 45) | 6 |
| `_GRB_StrataAzimuth` | Tilt Direction (deg) | Range(0, 360) | 30 |
| `_GRB_StrataNoiseScale` | Band Warp Scale (per m) | Float | 0.25 |
| `_GRB_StrataNoiseAmp` | Band Warp (m) | Range(0, 2) | 0.35 |
| `_GRB_StrataLineDarken` | Bedding Line Darken | Range(0, 1) | 0.35 |
| `_GRB_StrataLineWidth` | Bedding Line Width (m) | Range(0, 0.2) | 0.03 |
| `_GRB_StrataLedge` | Ledge Relief (normal) | Range(0, 1) | 0.5 |
| `_GRB_StrataSmoothVar` | Band Smoothness Variation | Range(0, 0.5) | 0.15 |
| `_GRB_MacroStrength` | Macro Variation | Range(0, 1) | 0.25 |
| `_GRB_MacroScale` | Macro Scale (m) | Float | 6 |
| `_GRB_FadeEnable` | Transparency Fade (dithered clip at the base) | Float | 1 |
| `_GRB_FadeClip` | Fade Clips Pixels (legacy dissolve, see-through at the contact) | Float | 0 |
| `_GRB_FadeNoTaaSmooth` | Without TAA: smooth colour blend, no clip (24ba, no AO crease) | Float | 0 |
| `_GRB_FadeHeight` | Fade Height (m above the terrain) | Range(0, 1) | 0.1 |
| `_GRB_FadeSoftness` | Fade Softness | Range(0, 1) | 0.6 |
| `_GRB_FadeNoiseScale` | Fade Noise Scale (per m) | Float | 6 |
| `_GRB_FadeNoiseStrength` | Fade Noise Strength | Range(0, 1) | 0.35 |
| `_GRB_FadeTerrainColor` | Terrain Colour in Fade Zone | Range(0, 1) | 1 |
| `_GRB_FadeNormalUp` | Fade Zone Normal Toward Up | Range(0, 1) | 0.5 |
| `_GRB_FadeShadowLift` | Fade Shadow Lift (no self-shadow under the dithered band) | Float | 1 |
| `_GRB_FadeShadowLiftMargin` | Fade Shadow Lift Margin (m above the fade band) | Range(0, 1) | 0.1 |
| `_GRB_ContactAOKeep` | Contact AO Keep (0 = none in fade zone, 1 = unchanged) | Range(0, 1) | 0.3 |
| `_GRB_ContactAOWidth` | Contact AO Zone Width (x fade height) | Range(1, 4) | 2 |
| `_GRB_BaseAOSuppress` | Suppress AO at base blend (24ax) | Float | 1 |
| `_GRB_BaseAOSuppressWidth` | Base AO Suppress Band (x blend height) | Range(1, 6) | 3 |
| `_GRB_BaseAOSuppressStrength` | Base AO Suppress Strength | Range(0, 1) | 1 |
| `_GRB_ForwardOnly` | Forward-only variant (set by the shader) | Float | 0 |
| `_GRB_UseTexArray` | Base Maps From Texture Arrays (UV0 + slice in UV3) | Float | 0 |
| `_GRB_BaseArray` | Base Color Array (group A) | 2DArray | "" {} |
| `_GRB_NormalArray` | Normal Array (group A) | 2DArray | "" {} |
| `_GRB_MaskArray` | Mask Array (group A) | 2DArray | "" {} |
| `_GRB_BaseArrayB` | Base Color Array (group B, 4K) | 2DArray | "" {} |
| `_GRB_NormalArrayB` | Normal Array (group B, 4K) | 2DArray | "" {} |
| `_GRB_MaskArrayB` | Mask Array (group B, 4K) | 2DArray | "" {} |
| `_AlphaCutoff` | Alpha Cutoff | Range(0.0, 1.0) | 0.5 |

## Shader `Genesis PCG/Rock Blend Lit (Forward Only)`

- File: `Assets/Genesis PCG Rock Creation/Shaders/GenesisRockBlendForward.shader`
- Inspector: `GenesisRockBlendGUI`

| Property | Label | Type / Range | Default |
|---|---|---|---|
| `_GRB_HeightRange` | Legacy: vertex A height range (fixed 4 m in the shader, unused) | Float | 4 |
| `_GRB_SizeRange` | Legacy: vertex B size range (fixed 8 m in the shader, unused) | Float | 8 |
| `_GRB_BaseLumRef` | Base average luminance (set on bake) | Float | 0.08 |
| `_GRB_VColorRange` | Legacy: vertex R range (m) | Float | 0.5 |
| `_GRB_TopEnable` | Top Layer | Float | 1 |
| `_GRB_TopFromTerrain` | Top Uses Terrain Layer on Bake (wind-blown ground) | Float | 0 |
| `_GRB_TopBaseMap` | Top Albedo | 2D | "white" {} |
| `_GRB_TopColor` | Top Color | Color | (1,1,1,1) |
| `_GRB_TopNormalMap` | Top Normal | 2D | "bump" {} |
| `_GRB_TopNormalScale` | Top Normal Intensity | Range(0, 4) | 1 |
| `_GRB_TopHeightMap` | Top Height | 2D | "gray" {} |
| `_GRB_TopParallax` | Top Parallax (Top Height as height map, m) | Range(0, 0.15) | 0 |
| `_GRB_TopMaskMap` | Top Mask (R metallic, G AO, B detail, A smoothness) | 2D | "white" {} |
| `_GRB_TopMetallicMin` | Top Metallic Remap Min | Range(0, 1) | 0 |
| `_GRB_TopMetallicMax` | Top Metallic Remap Max | Range(0, 1) | 1 |
| `_GRB_TopSmoothMin` | Top Smoothness Remap Min | Range(0, 1) | 0 |
| `_GRB_TopSmoothMax` | Top Smoothness Remap Max | Range(0, 1) | 1 |
| `_GRB_TopAOMap` | Top AO | 2D | "white" {} |
| `_GRB_TopAOStrength` | Top AO Strength | Range(0, 1) | 1 |
| `_GRB_TopMaskOn` | Top Mask Assigned (auto) | Float | 0 |
| `_GRB_TopAOOn` | Top AO Assigned (auto) | Float | 0 |
| `_GRB_TopTint` | Top Tint A | Color | (1,1,1,1) |
| `_GRB_TopTint2` | Top Tint B (noise variation) | Color | (1,1,1,1) |
| `_GRB_TopVarScale` | Tint Variation Scale (per m) | Float | 0.7 |
| `_GRB_TopValueVar` | Value Variation | Range(0, 1) | 0.15 |
| `_GRB_TopSaturation` | Saturation | Range(0, 2) | 1 |
| `_GRB_TopBrightness` | Brightness | Range(0, 2) | 1 |
| `_GRB_TopTiling` | Top Tiling (tiles per m) | Float | 0.5 |
| `_GRB_TopSmoothness` | Top Smoothness | Range(0, 1) | 0.3 |
| `_GRB_TopCoverage` | Top Coverage | Range(0, 1) | 0.5 |
| `_GRB_TopSharpness` | Top Edge Sharpness | Range(1, 32) | 8 |
| `_GRB_TopSlopeStart` | Slope Start (normal.y) | Range(0, 1) | 0.45 |
| `_GRB_TopSlopeEnd` | Slope Full (normal.y) | Range(0, 1) | 0.8 |
| `_GRB_TopCreviceBias` | Collect in Crevices / Ledges (base AO) | Range(0, 2) | 0.5 |
| `_GRB_TopHeightContrast` | Top Height Blend | Range(0, 2) | 0.6 |
| `_GRB_TopVertexWeight` | Vertex G (exposure) vs pixel normal | Range(0, 1) | 0.3 |
| `_GRB_TopNoiseScale` | Top Noise Scale (per m) | Float | 0.6 |
| `_GRB_TopNoiseStrength` | Top Noise Strength | Range(0, 1) | 0.35 |
| `_GRB_TopNormalBlend` | Top Layer Normal Strength | Range(0, 1) | 0.6 |
| `_GRB_TopThroughMin` | Base Normal Through Top Layer: Min (smooth patches) | Range(0, 1) | 0.35 |
| `_GRB_TopThroughMax` | Base Normal Through Top Layer: Max (bumpy patches) | Range(0, 1) | 1 |
| `_GRB_TopThroughNoiseScale` | Base Normal Through Top: Noise Scale (per m) | Float | 0.7 |
| `_GRB_TopThroughContrast` | Base Normal Through Top: Noise Contrast | Range(0.5, 6) | 2.5 |
| `_GRB_TopMaxWeight` | Top Max Weight | Range(0, 1) | 1 |
| `_GRB_TopMinPieceSize` | Min Piece Size (m, smaller pieces get none) | Float | 0 |
| `_GRB_BottomEnable` | Terrain Blend at Base | Float | 1 |
| `_GRB_TerrainAuto` | Match Terrain Layer on Bake | Float | 1 |
| `_GRB_TerrainBaseMap` | Terrain Albedo | 2D | "white" {} |
| `_GRB_TerrainNormalMap` | Terrain Normal | 2D | "bump" {} |
| `_GRB_TerrainTint` | Terrain Tint | Color | (1,1,1,1) |
| `_GRB_TerrainTiling` | Terrain Tiling (tiles per m = 1 / layer tile size) | Float | 0.5 |
| `_GRB_TerrainUVOffset` | Terrain UV Offset (xy, set on bake) | Vector | (0,0,0,0) |
| `_GRB_TerrainNormalScale` | Terrain Normal Strength | Range(0, 2) | 1 |
| `_GRB_TerrainSmoothness` | Terrain Smoothness | Range(0, 1) | 0.15 |
| `_GRB_TerrainPerPixel` | Match Terrain Per Pixel (terrain splat maps under the rock, updates on move) | Float | 1 |
| `_GRB_TParams` |  | Vector | (0,0,0,0) |
| `_GRB_TRect` |  | Vector | (0,0,0,0) |
| `_GRB_TControlTS` |  | Vector | (0,0,0,0) |
| `_GRB_THeightP` |  | Vector | (0,0,0,0) |
| `_GRB_THeightTS` |  | Vector | (0,0,0,0) |
| `_GRB_TST0` |  | Vector | (0,0,0,0) |
| `_GRB_TST1` |  | Vector | (0,0,0,0) |
| `_GRB_TST2` |  | Vector | (0,0,0,0) |
| `_GRB_TST3` |  | Vector | (0,0,0,0) |
| `_GRB_TST4` |  | Vector | (0,0,0,0) |
| `_GRB_TST5` |  | Vector | (0,0,0,0) |
| `_GRB_TST6` |  | Vector | (0,0,0,0) |
| `_GRB_TST7` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff0` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff1` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff2` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff3` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff4` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff5` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff6` |  | Vector | (0,0,0,0) |
| `_GRB_TDiff7` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff0` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff1` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff2` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff3` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff4` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff5` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff6` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskOff7` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale0` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale1` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale2` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale3` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale4` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale5` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale6` |  | Vector | (0,0,0,0) |
| `_GRB_TMaskScale7` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer0` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer1` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer2` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer3` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer4` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer5` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer6` |  | Vector | (0,0,0,0) |
| `_GRB_TLayer7` |  | Vector | (0,0,0,0) |
| `_GRB_TControl0` |  | 2D | "black" {} |
| `_GRB_TControl1` |  | 2D | "black" {} |
| `_GRB_THeightMap` |  | 2D | "black" {} |
| `_GRB_TAlb0` |  | 2D | "white" {} |
| `_GRB_TNrm0` |  | 2D | "bump" {} |
| `_GRB_TMsk0` |  | 2D | "white" {} |
| `_GRB_TAlb1` |  | 2D | "white" {} |
| `_GRB_TNrm1` |  | 2D | "bump" {} |
| `_GRB_TMsk1` |  | 2D | "white" {} |
| `_GRB_TAlb2` |  | 2D | "white" {} |
| `_GRB_TNrm2` |  | 2D | "bump" {} |
| `_GRB_TMsk2` |  | 2D | "white" {} |
| `_GRB_TAlb3` |  | 2D | "white" {} |
| `_GRB_TNrm3` |  | 2D | "bump" {} |
| `_GRB_TMsk3` |  | 2D | "white" {} |
| `_GRB_TAlb4` |  | 2D | "white" {} |
| `_GRB_TNrm4` |  | 2D | "bump" {} |
| `_GRB_TMsk4` |  | 2D | "white" {} |
| `_GRB_TAlb5` |  | 2D | "white" {} |
| `_GRB_TNrm5` |  | 2D | "bump" {} |
| `_GRB_TMsk5` |  | 2D | "white" {} |
| `_GRB_TAlb6` |  | 2D | "white" {} |
| `_GRB_TNrm6` |  | 2D | "bump" {} |
| `_GRB_TMsk6` |  | 2D | "white" {} |
| `_GRB_TAlb7` |  | 2D | "white" {} |
| `_GRB_TNrm7` |  | 2D | "bump" {} |
| `_GRB_TMsk7` |  | 2D | "white" {} |
| `_GRB_BottomThroughMin` | Base Normal Through Terrain Blend: Min | Range(0, 1) | 0.15 |
| `_GRB_BottomThroughMax` | Base Normal Through Terrain Blend: Max | Range(0, 1) | 0.75 |
| `_GRB_BottomThroughNoiseScale` | Base Normal Through Terrain: Noise Scale (per m) | Float | 1 |
| `_GRB_BottomThroughContrast` | Base Normal Through Terrain: Noise Contrast | Range(0.5, 6) | 2.5 |
| `_GRB_BottomHeight` | Blend Height (m) | Range(0, 4) | 0.2 |
| `_GRB_BottomFalloff` | Blend Falloff (m) | Range(0.005, 1) | 0.08 |
| `_GRB_BottomSharpness` | Contact Sharpness | Range(1, 16) | 3 |
| `_GRB_BottomNoiseScale` | Blend Noise Scale (per m) | Float | 1.5 |
| `_GRB_BottomNoiseStrength` | Blend Noise Strength | Range(0, 1) | 0.5 |
| `_GRB_BottomEdgeNoiseScale` | Contact Line Jitter Scale (per m) | Float | 7 |
| `_GRB_BottomEdgeNoise` | Contact Line Jitter (m) | Range(0, 0.5) | 0.05 |
| `_GRB_BottomUpBias` | Up-facing Bias | Range(0, 1) | 0.5 |
| `_GRB_BottomMaxWeight` | Max Blend Weight | Range(0, 1) | 0.9 |
| `_GRB_BottomPieceScale` | Small Piece Cap (blend height <= piece size x this) | Range(0, 2) | 0.25 |
| `_GRB_StrataEnable` | World Strata Bands | Float | 0 |
| `_GRB_StrataColorA` | Band Colour A (absolute albedo) | Color | (0.30,0.27,0.24,1) |
| `_GRB_StrataColorB` | Band Colour B (absolute albedo) | Color | (0.55,0.49,0.41,1) |
| `_GRB_StrataDetail` | Keep Base Texture Detail | Range(0, 1) | 0.7 |
| `_GRB_StrataStrength` | Strata Strength | Range(0, 1) | 0.6 |
| `_GRB_StrataSpacing` | Band Spacing (m) | Float | 0.6 |
| `_GRB_StrataThickVar` | Band Thickness Variation | Range(0, 1) | 0.5 |
| `_GRB_StrataTilt` | Band Tilt (deg) | Range(0, 45) | 6 |
| `_GRB_StrataAzimuth` | Tilt Direction (deg) | Range(0, 360) | 30 |
| `_GRB_StrataNoiseScale` | Band Warp Scale (per m) | Float | 0.25 |
| `_GRB_StrataNoiseAmp` | Band Warp (m) | Range(0, 2) | 0.35 |
| `_GRB_StrataLineDarken` | Bedding Line Darken | Range(0, 1) | 0.35 |
| `_GRB_StrataLineWidth` | Bedding Line Width (m) | Range(0, 0.2) | 0.03 |
| `_GRB_StrataLedge` | Ledge Relief (normal) | Range(0, 1) | 0.5 |
| `_GRB_StrataSmoothVar` | Band Smoothness Variation | Range(0, 0.5) | 0.15 |
| `_GRB_MacroStrength` | Macro Variation | Range(0, 1) | 0.25 |
| `_GRB_MacroScale` | Macro Scale (m) | Float | 6 |
| `_GRB_FadeEnable` | Transparency Fade (dithered clip at the base) | Float | 1 |
| `_GRB_FadeClip` | Fade Clips Pixels (legacy dissolve, see-through at the contact) | Float | 0 |
| `_GRB_FadeNoTaaSmooth` | Without TAA: smooth colour blend, no clip (24ba, no AO crease) | Float | 0 |
| `_GRB_FadeHeight` | Fade Height (m above the terrain) | Range(0, 1) | 0.1 |
| `_GRB_FadeSoftness` | Fade Softness | Range(0, 1) | 0.6 |
| `_GRB_FadeNoiseScale` | Fade Noise Scale (per m) | Float | 6 |
| `_GRB_FadeNoiseStrength` | Fade Noise Strength | Range(0, 1) | 0.35 |
| `_GRB_FadeTerrainColor` | Terrain Colour in Fade Zone | Range(0, 1) | 1 |
| `_GRB_FadeNormalUp` | Fade Zone Normal Toward Up | Range(0, 1) | 0.5 |
| `_GRB_FadeShadowLift` | Fade Shadow Lift (no self-shadow under the dithered band) | Float | 1 |
| `_GRB_FadeShadowLiftMargin` | Fade Shadow Lift Margin (m above the fade band) | Range(0, 1) | 0.1 |
| `_GRB_ContactAOKeep` | Contact AO Keep (0 = none in fade zone, 1 = unchanged) | Range(0, 1) | 0.3 |
| `_GRB_ContactAOWidth` | Contact AO Zone Width (x fade height) | Range(1, 4) | 2 |
| `_GRB_BaseAOSuppress` | Suppress AO at base blend (24ax) | Float | 1 |
| `_GRB_BaseAOSuppressWidth` | Base AO Suppress Band (x blend height) | Range(1, 6) | 3 |
| `_GRB_BaseAOSuppressStrength` | Base AO Suppress Strength | Range(0, 1) | 1 |
| `_GRB_ForwardOnly` | Forward-only variant (set by the shader) | Float | 1 |
| `_GRB_UseTexArray` | Base Maps From Texture Arrays (UV0 + slice in UV3) | Float | 0 |
| `_GRB_BaseArray` | Base Color Array (group A) | 2DArray | "" {} |
| `_GRB_NormalArray` | Normal Array (group A) | 2DArray | "" {} |
| `_GRB_MaskArray` | Mask Array (group A) | 2DArray | "" {} |
| `_GRB_BaseArrayB` | Base Color Array (group B, 4K) | 2DArray | "" {} |
| `_GRB_NormalArrayB` | Normal Array (group B, 4K) | 2DArray | "" {} |
| `_GRB_MaskArrayB` | Mask Array (group B, 4K) | 2DArray | "" {} |
| `_AlphaCutoff` | Alpha Cutoff | Range(0.0, 1.0) | 0.5 |

### `PcgRockStudioWindow`

- `Assets/Genesis PCG Rock Creation/Editor/RockStudio/PcgRockStudioWindow.cs`


### `DmRockBakeScheduler`

- `Assets/Genesis PCG Rock Creation/Editor/DmRockBakeScheduler.cs`

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `delay` | float |  |  |  |
|  | `snap` | bool |  |  |  |

### `PcgTerrainBindWatcher`

- `Assets/Genesis PCG Rock Creation/Editor/PcgTerrainBindWatcher.cs`


### `GenesisRockBlendSceneFade`

- `Assets/Genesis PCG Rock Creation/Editor/GenesisRockBlendSceneFade.cs`


