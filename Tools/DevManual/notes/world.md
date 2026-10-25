## Reviewed notes (World / Sky / Lighting)

| System | Runtime script | Profile | Studio |
|---|---|---|---|
| Io sky, Jupiter, sun/eclipse | `Assets/_Project/Scripts/World/Sky/DMIoSkyController.cs` | `Resources/World/DM_IoSkyProfile.asset` | World → Sky & Orbit |
| Night sky + aurora dome | `Scripts/World/Sky/DMIoNightSky.cs` + `DM_IoNightSky.shader` ("DM/IoNightSky") | same profile (Night Sky / Aurora groups) | Sky & Orbit → Night sky / Aurora preset rows |
| Realtime indirect + AAA polish (SSAO, contact shadows, micro shadows) | `DMRealtimeIndirect` | same profile (AAA Polish group) | Sky & Orbit → Polish presets |
| Anti-ghost volume / clouds | `DMIoAntiGhostVolume` | component | — |
| Io Clock | `Scripts/World/Time/DMIoClockProfile.cs` | `Resources/World/DM_IoClockProfile.asset` | World → Io Clock |

**Menus:** `Tools/Dark Matter Genesis/World/Io Sky/Add Sky Controller To Open Scene`, `.../Io Sky/Ensure Sky Profile`, `.../World/Lighting/Add Realtime Indirect Volume To Open Scene`, `.../Lighting/Ensure Io Post Processing Profile (BOTD copy)`.

**Key adjustments**
- **Jupiter Height** `jupiterElevationDeg` -10–90 (default 32; clamped to 89.5). **Sky Rotation** `jupiterAzimuthDeg` 0–360 (default 200); night-sky stars and the aurora dome yaw follow `azimuth - 200`.
- **Eclipse shadows** `eclipseShadowsEnabled` (default on): in eclipse the sun's shadows go off and *DM Jupiter Shine* casts soft shadows (res 512, `eclipseShadowStrength` 0.6, `eclipseShadowSoftnessDeg` 6). The sun's original shadow mode is restored afterwards.
- **Contact shadows**: `sunContactShadows` (now **off**, 2026-10-24bd) and `contactShadowLength` (0.12; Polish presets 0.08 / 0.12 / 0.2). Screen-space contact shadows ignore shader shadow passes, so they draw dark lines at rock bases.
- **Clouds**: `DMIoAntiGhostVolume.disableVolumetricClouds` defaults false. The Cloud Layer needs the BOTD Environment *Visual Environment → cloudType* override (not edited by agents).

**Gotchas**
- HDRP is **Deferred Only**, only one directional light casts shadows, and the main camera uses FXAA (no TAA), so shaders take their "without TAA" paths.
- The AAA Polish group edits a runtime copy of DM_IoPostProcessing only; the BOTD originals are never edited.
