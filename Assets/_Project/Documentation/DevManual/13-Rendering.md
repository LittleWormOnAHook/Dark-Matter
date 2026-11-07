# Rendering — Genesis Studio

**Status:** Stub (generated tables; notes pending)  
**Open:** Tools → Dark Matter Genesis → Genesis Studio → **Rendering**  
DM shaders (DM_ copies only; nothing is reassigned automatically).

## Tabs

- [DM Shaders: Terrain](#dm-shaders-terrain)
- [DM Shaders: Master](#dm-shaders-master)

## DM Shaders: Terrain

**What it does:** DM_TerrainParallax material: height blend, parallax offset / POM, distance + grazing fade, per-layer strength, anti-tiling.

**Where:**
- Studio: `Genesis Studio / Rendering / DM Shaders: Terrain` (panel mode `ExternalTool`)

## DM Shaders: Master

**What it does:** DM/Master (HDRP Lit copy + extras). Converter creates DM_ material copies only; dry run by default; CSV manifest.

**Where:**
- Studio: `Genesis Studio / Rendering / DM Shaders: Master` (panel mode `ExternalTool`)

## Shutdown GPU / job leak audit (2026-11-05b)

Nov 5 Player.log shutdown listed ComputeBuffer (×13), GraphicsBuffer (×11), TempJob (×1), Persistent (×168), and JobTempAlloc warnings **without allocation stacks**. Repo scan: **no** `ComputeBuffer` / `GraphicsBuffer` / `NativeArray` with `Allocator.Persistent` under `Assets/_Project` runtime scripts. Owned mitigation: `DMTerrainParallaxHeightArrayBinder` now destroys runtime-created `Texture2DArray` / gray fallback on `SubsystemRegistration`. `ScannerHdrpCustomPass.Cleanup()` already destroys its engine material. Further attribution needs `-stacktrace` / Memory Profiler repro — do not dispose HDRP/vendor buffers from gameplay code.

## DM Terrain Parallax: D3D11 64-texture limit (2026-11-05)

Eight per-layer `_DMHeightMap0..7` Properties pushed `DM/TerrainParallax` over the D3D11 64-texture bound (~65/66 in Player.log). The shader now declares one `_DMHeightMapArray` (2DArray). `DMTerrainParallaxHeightArrayBinder` copies legacy `_DMHeightMap#` material assignments into that array at runtime (materials keep their existing slot data; no `.mat` commit required). Terrain-layer mask **B** remains the fallback when **Use Height Map** is off. Editor height-map UI still writes `_DMHeightMap#` on the material template.

## DM Terrain Parallax: Editor import warning vs compile (2026-11-05f)

After the height-array merge, `Editor.log` may briefly log `Shader Unsupported: DM/TerrainParallax - All subshaders removed` during the first import pass. Check `Logs/shadercompiler-UnityShaderCompiler.exe-*.log` for the same file: d3d11 `compileSnippet` lines with `ok=1` mean the raster passes compiled. If `ok=0` appears, fix owned HLSL before assuming a false-positive unsupported warning. Ray-tracing subshaders use `#pragma only_renderers` lists that exclude some Editor APIs; raster terrain must still compile for Mars play.

## DM Terrain Parallax: debug baseColor (2026-11-04a)

`DM_TerrainParallax_Splatmap.hlsl` `TerrainLitDebug()` sets `baseColor` to black first. The 8-layer chain now ends in a texture-streaming fallback `else`. This removes the "use of potentially uninitialized variable (baseColor)" warning at line 591. Normal rendering is unchanged, because the function only feeds HDRP debug display.

## Vendor shader graphs: DXR pass stripping (2026-11-04a)

`Assets/_Project/Editor/Rendering/DMRayTracingPassStripper.cs` is an `IPreprocessShaders` build hook. It strips only the ray tracing (DXR) passes of `Shader Graphs/PW_Water_RP` and `Shader Graphs/BotD_Graph_Lit_TranslucentAlphaCutoff`. Those passes use ddx/ddy and implicit-LOD sampling, which `lib_6_3` closesthit shaders reject, so they never compiled. Stripping them removes about 141 build shader errors. The vendor graphs are not edited and raster rendering is untouched. These surfaces still do not show in RT reflections or path tracing, the same as before. To restore the old behaviour, set `DMRayTracingPassStripper.Enabled = false` or add more names to its list.

## DMRealtimeIndirect: edit preview teardown on save (2026-11-04b)

`KillEditPreviews()` destroys the hidden edit-preview GameObject. Its `OnDisable` then runs inside that deactivation, where `DestroyImmediate` is illegal ("Cannot destroy Component while GameObject is being activated or deactivated"). While `KillEditPreviews` runs, `Kill()` now queues edit-mode destroys in `deferredKills`. `FlushDeferredKills()` empties the queue from `EditorTick` and from `EditorApplication.delayCall` after a scene save. **2026-11-05:** any edit-mode `OnDisable` on `DMRealtimeIndirect` uses the same deferred `Kill()` path (not only `KillEditPreviews`). Play-mode teardown is unchanged.


