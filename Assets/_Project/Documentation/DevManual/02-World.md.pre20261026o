# World — Genesis Studio

**Status:** Full (generated from code + reviewed notes)  
**Open:** Tools → Dark Matter Genesis → Genesis Studio → **World**  
Map calibration, fog-of-war, the Io clock, and loot chests.

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
## Tabs

- [Map Calibration](#map-calibration)
- [Terrain Layer Render](#terrain-layer-render)
- [Sliding Doors](#sliding-doors)
- [Io Clock](#io-clock)
- [Sky & Orbit](#sky--orbit)
- [Loot Chests - Timers](#loot-chests---timers)
- [Loot Chests - Presentation](#loot-chests---presentation)
- [Vendors](#vendors)
- [Vendor Stock](#vendor-stock)
- [Vendor Buy Logs](#vendor-buy-logs)

## Map Calibration

**What it does:** Journal / minimap UV, zoom meters, fog-of-war.

**Where:**
- Studio: `Genesis Studio / World / Map Calibration` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Map/DMWorldMapCalibrationProfile.asset`
- Script: `Assets/_Project/Scripts/Map/DMWorldMapCalibrationProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Runtime tuning | `enableRuntimeTuning` | bool | true |  | Arrow keys nudge map UV offset while playing. |
| Editor (Play) | `editorPlaySnapshotIntervalSeconds` | float | 1f | ≥0.1f | Editor only: seconds between Play-mode snapshots of this profile (DMWorldMapCalibrationSceneSync). Higher = cheaper. A final snapshot always runs when Play stops. |
| Map fog of war | `enableMapFogOfWar` | bool | true |  | Gold fog overlay on the world map / minimap. Off skips land-mask bake and walk stamps (much cheaper while moving). |
| Map fog of war | `walkRevealRadiusMeters` | float | 5f | ≥0.5f | Walk reveal hole radius (meters). |
| Map fog of war | `walkRevealEdgeSoftnessMeters` | float | 1.5f | ≥0f | Soft edge of the walk reveal hole (meters). |
| Map fog of war | `walkStampIntervalMeters` | float | 0.75f | ≥0.1f | Meters walked between walk stamps. Higher = cheaper. |
| Map fog of war | `textureUploadInterval` | float | 0.18f | ≥0.02f | Seconds between fog texture uploads. Higher = cheaper. |
| Map fog of war | `fogResolution` | int | 1024 | 64–4096 | Fog texture size. 1024 matches minimap sharpness; 2048+ is costly while moving. Changing this in Play rebuilds the mask. |
| Map fog of war | `scanRevealRadiusMeters` | float | 40f | ≥1f | Scanner sweep base radius (meters). Skill ranks add on top. |
| Map fog of war | `scanSkillBonusPerRankMeters` | float | 10f | ≥0f | Extra scan radius per Scan skill rank. |
| Map fog of war | `maxScanSkillRanks` | int | 5 | 0–10 | Max Scan skill ranks that add radius. |
| Map fog of war | `scanRevealEdgeSoftnessMeters` | float | 5f | ≥0f | Soft edge of scanner reveal (meters). |
| Map fog of war | `fogOverlayAlpha` | float | 0.95f | 0f–1f | How solid unrevealed gold fog is (0–1). |
| Map fog of war | `revealThreshold` | float | 0.35f | 0f–1f | Reveal mask value that counts as explored. |
| Map fog of war | `landMaskGreenThreshold` | float | 0.82f | 0f–1f | Land-mask green cutoff. Darker than this = land (fog). Peach void stays clear. |
| Map fog of war | `fogColor` | Color | new Color(0.8313726f, 0.627451f, 0.09019608f, 1f) |  | Unrevealed fog tint. Defaults to palette Gold #D4A017. |
| Map fog of war | `terrainMask` | Texture2D |  |  | Optional land/void mask. Empty uses DM Terrain Mask.jpg. |
| Player icon | `mapPlayerIconBaseDegrees` | float |  |  | Extra rotation on the full-map / journal player arrow (degrees). |
| Player icon | `minimapPlayerIconBaseDegrees` | float | 180f |  | Extra rotation on the minimap / HUD / pilot-cluster player arrow (degrees). 180 = chevron tip on heading. |
| Player icon | `mapPlayerIconUvOffset` | Vector2 |  |  | Move the player arrow on the full / journal map without shifting terrain UVs. |
| Player icon | `mapPlayerIconSizePixels` | float | 18f | ≥8f | Full-map / journal player arrow size (pixels). |
| Player icon | `minimapPlayerIconSizePixels` | float | 24f | ≥8f | Minimap / HUD player arrow size (pixels). |
| Display | `invertMapVertical` | bool |  |  | Journal leftover. Affine travel ignores this. Use Minimap Flip Vertical for the HUD. |
| Display | `invertMapHorizontal` | bool |  |  | Journal leftover. Affine travel ignores this. Use Minimap Flip Horizontal for the HUD. |
| Display | `mapDisplayNorthOffsetDegrees` | float | -90f | -180f–180f | Extra yaw on minimap/compass (-90 = world +X reads as North). |
| Display | `mapUvOffset` | Vector2 |  |  | Fine-tune world→UV alignment after affine calibration. |
| Display | `mapUvScaleX` | float | 1.12f | 0.5f–2f | Stretch map-east travel UV. >1 if the token stops short east/west. |
| Display | `mapUvScaleY` | float | 1.14f | 0.5f–2f | Stretch map-north travel UV. >1 if the token stops short north/south. |
| Full map / journal | `fullMapMaxZoomVisibleMeters` | float | 1000f | 50f–2000f | Closest zoom: world meters visible across the square map viewport when scrolled all the way in. |
| Full map / journal | `fullMapOpenVisibleMeters` | float | 1200f | 200f–8000f | World meters visible across the map viewport when the full map first opens. |
| Full map / journal | `fullMapMarkerScreenPixels` | float | 7f | 4f–20f | POI marker size on screen (pixels) — stays constant while zooming; shrinks on the map texture as you zoom in. |
| Full map / journal | `fullMapPlayerScreenPixels` | float | 10f | 4f–24f | Player arrow size on screen (pixels) on the full / journal map. |
| Minimap | `minimapFlipHorizontal` | bool |  |  | Flip the minimap crop left/right. Does not change the journal. |
| Minimap | `minimapFlipVertical` | bool |  |  | Flip the minimap crop up/down. Does not change the journal. |
| Minimap | `minimapRotateX` | float |  | -180f–180f | Extra minimap pitch (X). Applied on top of heading-up yaw. |
| Minimap | `minimapRotateY` | float |  | -180f–180f | Extra minimap roll (Y). Applied on top of heading-up yaw. |
| Minimap | `minimapRotateZ` | float |  | -180f–180f | Extra minimap yaw (Z) added to heading-up rotation. |
| Grid 0,0 calibration | `mapZeroUv01` | Vector2 | new Vector2(0.518f, 0.498f) |  | Texture UV where world grid origin (Map Zero) sits on the authored map. |
| Grid 0,0 calibration | `mapCalibrationWorldXz` | Vector2 | new Vector2(-767f, -108f) |  | Known world XZ (grid coords) for the second calibration point. |
| Grid 0,0 calibration | `mapCalibrationUv01` | Vector2 | new Vector2(0.563f, 0.440f) |  | Texture UV for that second calibration point on the authored map. |
| Grid 0,0 calibration | `useAffineMapProjection` | bool | true |  |  |

## Terrain Layer Render

**What it does:** Per splat: height blend on/off, parallax on select Io layers, normal/tile overrides.

**Where:**
- Studio: `Genesis Studio / World / Terrain Layer Render` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/World/DM_TerrainSplatRenderProfile.asset`

## Sliding Doors

**What it does:** Slide/rotate motion, open direction, timing, pooled VFX, and door SFX profiles.

**Where:**
- Studio: `Genesis Studio / World / Sliding Doors` (panel mode `AssetFolder`)
- Asset folder: `Assets/_Project/Resources/Environment/SlidingDoors` (`t:DMSlidingDoorProfile`)
- Script: `Assets/_Project/Scripts/Environment/Doors/DMSlidingDoorProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `openDelaySeconds` | float |  | ≥0f | Seconds after set 1 begins opening before this additional set starts. |
| Registry | `profileId` | string | "default" |  | Unique id for DMSlidingDoorProfileRegistry (e.g. scifi_big_horizontal). |
| Motion | `motionMode` | DMSlidingDoorMotionMode | DMSlidingDoorMotionMode.Slide |  |  |
| Motion | `localAxis` | DMSlidingDoorLocalAxis | DMSlidingDoorLocalAxis.Z |  |  |
| Motion | `openDirection` | DMSlidingDoorOpenDirection | DMSlidingDoorOpenDirection.PanelSymmetric |  |  |
| Motion | `slideDistanceMeters` | float | 1.45f | ≥0.01f | Meters along local axis when motionMode is Slide. |
| Motion | `rotateAngleDegrees` | float | 90f | 1f–175f | Degrees when motionMode is Rotate. |
| Motion | `leftPanelSign` | float | -1f |  | Custom mode only. Multiplier on distance/angle for the left leaf. |
| Motion | `rightPanelSign` | float | 1f |  | Custom mode only. Multiplier on distance/angle for the right leaf. |
| Timing | `moveDurationSeconds` | float | 0.75f | ≥0.05f |  |
| Timing | `closeDelaySeconds` | float | 2f | ≥0f |  |
| Additional panel sets (timing defaults) | `additionalSetTimings` | DMSlidingDoorAdditionalSetTiming[] |  |  | Open delay per extra set (B, C, …). Wires to controller Additional Panel Sets by index. |
| Trigger | `playerTag` | string | "Player" |  |  |
| VFX | `openVfxPrefab` | GameObject |  |  |  |
| VFX | `closeVfxPrefab` | GameObject |  |  |  |
| VFX | `vfxLifetimeSeconds` | float | 2f | ≥0.05f |  |
| VFX | `vfxLocalOffset` | Vector3 |  |  |  |
| Audio | `openClip` | AudioClip |  |  |  |
| Audio | `closeClip` | AudioClip |  |  |  |
| Audio | `sfxVolume` | float | 1f | 0f–1f |  |
| Audio | `leftPanel` | Transform |  |  | Left leaf for this set (e.g. DoorHoriz_B). |
| Audio | `rightPanel` | Transform |  |  | Right leaf for this set (e.g. DoorHoriz_B (1)). |
| Audio | `openDelaySeconds` | float |  | ≥0f | Seconds after set 1 begins opening before this set starts. Ignored for the primary pair. |

## Io Clock

**What it does:** 30-hour Io day, real-time scale, and new-game start time.

**Where:**
- Studio: `Genesis Studio / World / Io Clock` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/World/DM_IoClockProfile.asset`
- Script: `Assets/_Project/Scripts/World/Time/DMIoClockProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Io Day | `hoursPerDay` | int | 30 | ≥1 | Game hours in one Io day. |
| Io Day | `realSecondsPerGameHour` | float | 120f | ≥1f | Real seconds that equal one Io hour. 120 = 1 real hour per 30-hour Io day. |
| Io Day | `uiPaintRealSeconds` | float | 60f | ≥1f | How often clock labels repaint, in real seconds. |
| New Game | `startDay` | int | 1 | ≥1 |  |
| New Game | `startHour` | int | 6 | 0–29 |  |
| New Game | `startMinute` | int |  | 0–59 |  |

## Sky & Orbit

**What it does:** Io sky on the 30 h clock: sun path, noon eclipse (length/fade/ambient), Jupiter placement + size, Jupiter-shine, Europa/Ganymede/Callisto.

**Where:**
- Studio: `Genesis Studio / World / Sky & Orbit` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/World/DM_IoSkyProfile.asset`
- Script: `Assets/_Project/Scripts/World/Sky/DM_IoSkyProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `name` | string | "Moon" |  |  |
|  | `enabled` | bool | true |  |  |
|  | `periodRatio` | float | 2f | ≥0.1f | Orbital period as a multiple of Io's (Io = 1 = one 30 h Io day). Europa 2, Ganymede 4, Callisto 9.43. |
|  | `orbitRadiusKm` | float | 671100f | ≥1000f | Orbit radius around Jupiter in km (drives direction + realistic apparent size from Io). |
|  | `diameterKm` | float | 3121.6f | ≥10f |  |
|  | `sizeMultiplier` | float | 1f | 0.1f–10f | 1 = realistic apparent size. Raise for a stylized, easier-to-see moon. |
|  | `startPhaseDeg` | float | 40f | 0f–360f | Orbital angle at Io day 1, 00:00. |
| Look (procedural) | `colorA` | Color | new Color(0.92f, 0.88f, 0.8f, 1f) |  |  |
| Look (procedural) | `colorB` | Color | new Color(0.55f, 0.38f, 0.25f, 1f) |  |  |
| Look (procedural) | `noiseScale` | float | 3f | 0.5f–20f |  |
| Look (procedural) | `lineae` | float | 0.6f | 0f–1f |  |
| Look (procedural) | `craterSpots` | float |  | 0f–1f |  |
| Look (procedural) | `brightness` | float | 1f | 0f–5f |  |
| Controller | `skyEnabled` | bool | true |  |  |
| Controller | `autoSpawnController` | bool | true |  | Spawn DMIoSkyController automatically in Play (no scene edit needed). |
| Controller | `onlyInScenesWithJupiter` | bool | true |  | Only run in scenes that contain the Jupiter object (keeps menus untouched). |
| Controller | `skyBodyShader` | Shader |  |  | Shader for Jupiter + moons (DM/IoSkyBody). Lit by the sun in space, so phases show even at night. |
| Controller | `debugHourOverride` | float | -1f | -1f–30f | -1 = follow the Io clock. 0-30 = force this hour (preview / testing in Play). |
| Controller | `startHour` | float | 6f | 0f–30f | 2026-10-24ao: Io hour the clock starts at when Play starts (and on New Game), then runs normally. 15 = noon (eclipse centre). Loading a save restores the saved time instead. Debug Hour Override >= 0 still wins. |
| Sun | `sunLightName` | string | "Day DirectionalLight" |  | Scene directional light the controller drives as the sun. |
| Sun | `disableOtherDirectionalLights` | bool | true |  | Turn off every other enabled directional light (BOTD sun etc.) at runtime. |
| Sun | `sunIntensityMultiplier` | float | 1f | 0f–5f | Multiplier on the sun light's scene intensity. |
| Sun | `horizonFadeDegrees` | float | 4f | 0.5f–20f | Sun fades out over +/- this many degrees around the horizon. |
| Sun | `sunHorizonDimDegrees` | float | 20f | 0f–45f | 2026-10-24al: low-sun dimming band above the horizon (degrees). 0 = off. |
| Sun | `sunHorizonMinFactor` | float | 0.5f | 0f–1f | Sun intensity factor right at the horizon (ramps to 1 at sunHorizonDimDegrees). |
| Sun | `sunHorizonDesaturate` | float | 0.6f | 0f–1f | Desaturate the sun colour near the horizon (0 = keep scene colour, 1 = white). |
| Sun | `sunMaxLux` | float | 8000f | 0f–130000f | Hard cap on sun lux after all multipliers (0 = no cap). Scene Day light is ~9400 lux. |
| Sun | `shadeFillEnabled` | bool | true |  | 24al: shadowless, specular-free fill light from the anti-sun side (bounce from sun-lit ground SSGI can't see). Scales with sun elevation + eclipse. |
| Sun | `shadeFillLux` | float | 700f | 0f–3000f |  |
| Sun | `shadeFillElevationDeg` | float | -10f | -45f–45f | Degrees the fill travels downward (0 = horizontal, negative = from below like ground bounce). |
| Sun | `shadeFillColor` | Color | new Color(1f, 0.8f, 0.6f, 1f) |  |  |
| Sun | `orbitPoleTiltDeg` | float |  | -60f–60f | Tilt of the sun path around the Jupiter direction (degrees). |
| Sun | `reverseSkyMotion` | bool |  |  | Flip the direction the sky turns. |
| Eclipse (centered on local noon, 15:00) | `eclipseEnabled` | bool | true |  |  |
| Eclipse (centered on local noon, 15:00) | `eclipseDurationHours` | float | 10f | 0.5f–20f | Gameplay darkness length in Io hours. Realistic Io eclipse is ~1.6 h (use the Realistic preset). Default 10 h is stylized. |
| Eclipse (centered on local noon, 15:00) | `eclipseFadeHours` | float | 1.5f | 0.05f–5f | Fade in / out length (hours), centered on each eclipse edge. |
| Eclipse (centered on local noon, 15:00) | `eclipseSunRemaining` | float |  | 0f–1f | Sun intensity left at full eclipse (0 = black). |
| Eclipse (centered on local noon, 15:00) | `eclipseAmbientMultiplier` | float | 0.25f | 0f–1f | Indirect diffuse + reflection multiplier at full eclipse (dim ambient). |
| Eclipse (centered on local noon, 15:00) | `sunHidesBehindJupiter` | bool | true |  | Keep the sun parked behind Jupiter for the whole eclipse (the sun path speeds up outside it). Off = geometric path. |
| Eclipse (centered on local noon, 15:00) | `ambientVolumePriority` | int | 100 | ≥0f |  |
| Jupiter | `jupiterObjectName` | string | "Jupiter" |  |  |
| Jupiter | `jupiterAzimuthDeg` | float | 200f | 0f–360f | 24aw: turns the whole sky (Jupiter, sun path/eclipse, moons, chunks, ring light, stars, aurora) around the vertical. 200 = original. |
| Jupiter | `jupiterElevationDeg` | float | 32f | -10f–90f | 24aw: Jupiter elevation above the horizon (deg). Sun path, eclipse, moons, chunks and ring light follow. |
| Jupiter | `jupiterAngularDiameterDeg` | float | 19.5f | 1f–60f | Apparent diameter from Io. Realistic ~19.5 deg. |
| Jupiter | `skyDistance` | float | 2000f | 200f–20000f | Fixed render distance of sky bodies (clamped to 75% of camera far clip). |
| Jupiter | `jupiterSpinHours` | float | 7f | 0.5f–100f | Jupiter's spin period in Io hours (real 9.93 h vs Io 42.5 h -> 7.0 on the 30 h day). |
| Jupiter | `jupiterTiltEuler` | Vector3 |  |  | Mesh orientation fix (euler) so the cloud bands line up with the moons' orbit plane. |
| Jupiter | `jupiterBrightness` | float | 1f | 0f–5f |  |
| Jupiter | `jupiterLimbDarkening` | float | 0.35f | 0f–1f |  |
| Jupiter | `jupiterRimColor` | Color | new Color(1f, 0.72f, 0.45f, 1f) |  |  |
| Jupiter | `jupiterEclipseRimLux` | float | 400f | 0f–5000f | Backlit atmosphere rim during eclipse (nits-ish). |
| Jupiter-lit sky | `jupiterShineColor` | Color | new Color(1f, 0.86f, 0.68f, 1f) |  |  |
| Jupiter-lit sky | `jupiterShineLux` | float | 25f | 0f–500f | Jupiter-shine at full Jupiter (midnight). Realistic ~25 lux. |
| Jupiter-lit sky | `eclipseJupiterShineLux` | float | 15f | 0f–500f | Extra stylized Jupiter-shine during eclipse so the sky stays readable. |
| Other moons (existing small ones around Jupiter) | `otherMoonPeriodsHours` | float[] | { 8.4f, 11.4f } |  | Orbit periods in Io hours for the existing anchors under Jupiter (Amalthea ~8.4, Thebe ~11.4). |
| Other moons (existing small ones around Jupiter) | `otherMoonsUseSkyMaterial` | bool | true |  |  |
| Other moons (existing small ones around Jupiter) | `otherMoonColor` | Color | new Color(0.55f, 0.42f, 0.36f, 1f) |  |  |
| Realtime Indirect Light (2026-10-24aj, SSGI, no baking) | `realtimeIndirectEnabled` | bool | true |  | Spawn a global Volume with DM_IoPostProcessing (BOTD PP copy tuned for Io) in Play (above Gaia/BOTD). Undone on exit. |
| Realtime Indirect Light (2026-10-24aj, SSGI, no baking) | `indirectVolumeProfile` | UnityEngine.Rendering.VolumeProfile |  |  | Resources/World/DM_IoPostProcessing (VolumeProfile). Cloned at runtime, the asset is never modified. |
| Realtime Indirect Light (2026-10-24aj, SSGI, no baking) | `indirectVolumePriority` | float | 90f |  | Above BOTD/Gaia volumes (0). Sky eclipse ambient volume is 100 but is muted while this drives ambient. |
| Realtime Indirect Light (2026-10-24aj, SSGI, no baking) | `ssgiEnabled` | bool | true |  | Screen Space Global Illumination (ray marched, DX11 ok). |
| Realtime Indirect Light (2026-10-24aj, SSGI, no baking) | `ssgiQuality` | IndirectQuality | IndirectQuality.Custom |  | Custom = use the steps/resolution/denoise below (BOTD uses Custom). |
| Realtime Indirect Light (2026-10-24aj, SSGI, no baking) | `ssgiMaxRaySteps` | int | 64 | 8–128 |  |
| Realtime Indirect Light (2026-10-24aj, SSGI, no baking) | `ssgiFullResolution` | bool | true |  |  |
| Realtime Indirect Light (2026-10-24aj, SSGI, no baking) | `ssgiDenoise` | bool | true |  |  |
| Realtime Indirect Light (2026-10-24aj, SSGI, no baking) | `ssgiHalfResDenoiser` | bool | true |  | Off = second (full-res) denoiser pass for cleaner bounce. |
| Realtime Indirect Light (2026-10-24aj, SSGI, no baking) | `ssgiDenoiserRadius` | float | 0.6f | 0.05f–1f |  |
| Realtime Indirect Light (2026-10-24aj, SSGI, no baking) | `ssgiDepthThickness` | float | 0.1f | 0.01f–0.5f | Max SSGI ray thickness. Higher = fewer leaks through thin geometry, more bounce on walls. |
| Io Shade Look (2026-10-24ak) | `shadowFloorLift` | float | 0.08f | 0f–0.3f | Lift the shadow floor so back sides read as warm shade, not black (Shadows/Midtones/Highlights, shadows w). |
| Io Shade Look (2026-10-24ak) | `shadowFloorTint` | Color | new Color(1f, 0.86f, 0.62f, 1f) |  | Tint of the lifted shadows (Io sulfur-orange ground bounce). |
| Io Shade Look (2026-10-24ak) | `overrideGroundTint` | bool | true |  | Physically Based Sky ground tint: feeds warm ground bounce into the ambient probe for back sides. |
| Io Shade Look (2026-10-24ak) | `groundBounceTint` | Color | new Color(0.45f, 0.36f, 0.24f, 1f) |  |  |
| Io Shade Look (2026-10-24ak) | `ssaoIntensity` | float | 0.9f | 0f–4f |  |
| Io Shade Look (2026-10-24ak) | `ssaoDirectLightingStrength` | float | 0.15f | 0f–1f |  |
| Io Shade Look (2026-10-24ak) | `fixCameraVolumeMask` | bool | true |  | If a camera's Volume Layer Mask excludes this volume's layer (some turret cameras use 0 = no volumes), add it at runtime; restored on exit. |
| Io Shade Look (2026-10-24ak) | `indirectDiffuseMultiplier` | float | 1.2f | 0f–4f | Indirect diffuse multiplier (bounce + sky ambient). BOTD Environment uses 0.5. Eclipse dimming multiplies on top. |
| Io Shade Look (2026-10-24ak) | `indirectReflectionMultiplier` | float | 1f | 0f–4f |  |
| Io Shade Look (2026-10-24ak) | `ambientModeDynamic` | bool | true |  | Dynamic = ambient follows the live sky (sun path, eclipse, night). |
| Io Shade Look (2026-10-24ak) | `exposureOverride` | bool | true |  | Override exposure. BOTD = physical camera (ISO200 1/200 f16) + comp 8 => fixed ~EV 6.6, which blows out sun-facing slopes. On = auto histogram. |
| Io Shade Look (2026-10-24ak) | `exposureHistogram` | bool | true |  | On = Automatic Histogram, off = Automatic. |
| Io Shade Look (2026-10-24ak) | `exposureLimitMin` | float | 6.4f | -6f–20f | EV100 floor (brightest the image gets). ~6.6 = BOTD look, keeps shaded views as before. |
| Io Shade Look (2026-10-24ak) | `exposureLimitMax` | float | 9f | -6f–20f | EV100 ceiling (how far it darkens when facing the sun). |
| Io Shade Look (2026-10-24ak) | `exposureHistogramLow` | float | 40f | 0f–100f | Histogram percentages: ignore darkest X% / brightest (100-Y)% when metering. |
| Io Shade Look (2026-10-24ak) | `exposureHistogramHigh` | float | 90f | 0f–100f |  |
| Io Shade Look (2026-10-24ak) | `exposureAdaptDarkToLight` | float | 2f | 0.1f–10f |  |
| Io Shade Look (2026-10-24ak) | `exposureAdaptLightToDark` | float | 1.5f | 0.1f–10f |  |
| Io Shade Look (2026-10-24ak) | `bloomIntensity` | float | 0.3f | 0f–1f | Bloom intensity (BOTD 0.416, threshold 0). |
| Io Shade Look (2026-10-24ak) | `bloomThreshold` | float | 0.5f | 0f–5f |  |
| Io Shade Look (2026-10-24ak) | `exposureCompensation` | float |  | -5f–5f |  |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `polishEnabled` | bool | true |  |  |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `ssaoRadius` | float | 0.5f | 0.1f–2f | SSAO world radius (BOTD 0.25). |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `ssaoMaxRadiusPixels` | int | 24 | 8–64 | SSAO max radius in pixels (BOTD 16). Higher = broader contact darkening, a bit more cost. |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `contactShadowLength` | float | 0.12f | 0f–1f | Contact shadow ray length in metres (BOTD 0 = effectively off). |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `sunContactShadows` | bool | true |  | Enable contact shadows on the sun light at runtime (restored on exit). |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `microShadowOpacity` | float | 0.5f | 0f–1f | Micro shadowing from normal/AO maps (0 = off). |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `shadowMaxDistance` | float | 150f | 50f–400f | Max directional shadow distance (BOTD 150). |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `shadowCascadeSplit0` | float | 0.06f | 0.02f–0.2f | First cascade split (BOTD 0.076). Smaller = sharper shadows near the character. |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `fogMeanFreePath` | float | 3000f | 0f–8000f | Fog attenuation distance / mean free path (scene 2000). 0 = leave scene fog. |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `gradeContrast` | float | 5f | -20f–30f | Colour Adjustments contrast (0 = none). |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `splitToneStrength` | float | 0.15f | 0f–1f | Split toning strength: cooler shadows / warmer highlights (0 = off). |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `splitToneShadows` | Color | new Color(0.42f, 0.52f, 0.66f, 1f) |  |  |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `splitToneHighlights` | Color | new Color(0.66f, 0.55f, 0.42f, 1f) |  |  |
| AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only) | `vignetteIntensity` | float | 0.22f | 0f–0.5f | Vignette intensity (BOTD 0.201). |
| Eclipse Readability Floor (24ap) | `eclipseFloorEnabled` | bool | true |  | Keeps eclipse from going pitch black: ambient floor, dim fill light, and a brighter exposure limit, all scaled by eclipse depth. |
| Eclipse Readability Floor (24ap) | `eclipseMinAmbient` | float | 0.35f | 0f–1f | Minimum indirect (ambient) multiplier during eclipse; floors eclipseAmbientMultiplier. |
| Eclipse Readability Floor (24ap) | `eclipseFillLux` | float | 40f | 0f–500f | Dim shadowless fill (lux) added at full eclipse via the shade-fill light (Jupiter-shine side). |
| Eclipse Readability Floor (24ap) | `eclipseExposureLimitMin` | float | 2f | -6f–20f | Exposure Limit Min (EV) at full eclipse. Lower = auto exposure may brighten more in the dark. |
| Night Sky - DM_IoNightSky (24au) | `nightSkyEnabled` | bool | true |  | Procedural stars + Milky Way on a runtime dome (additive, no temporal effects). |
| Night Sky - DM_IoNightSky (24au) | `nightSkyShader` | Shader |  |  |  |
| Night Sky - DM_IoNightSky (24au) | `starDensity` | float | 180f | 40f–600f |  |
| Night Sky - DM_IoNightSky (24au) | `starBrightness` | float | 4f | 0f–40f |  |
| Night Sky - DM_IoNightSky (24au) | `starSize` | float | 1f | 0.3f–3f |  |
| Night Sky - DM_IoNightSky (24au) | `starTwinkle` | float | 0.3f | 0f–1f |  |
| Night Sky - DM_IoNightSky (24au) | `starColorVariation` | float | 0.6f | 0f–1f |  |
| Night Sky - DM_IoNightSky (24au) | `milkyWayIntensity` | float | 1.5f | 0f–20f |  |
| Night Sky - DM_IoNightSky (24au) | `milkyWayWidth` | float | 0.18f | 0.02f–0.6f |  |
| Night Sky - DM_IoNightSky (24au) | `milkyWayDust` | float | 0.7f | 0f–1f |  |
| Night Sky - DM_IoNightSky (24au) | `milkyWayCoreColor` | Color | new Color(1f, 0.85f, 0.7f, 1f) |  |  |
| Night Sky - DM_IoNightSky (24au) | `milkyWayEdgeColor` | Color | new Color(0.55f, 0.65f, 1f, 1f) |  |  |
| Night Sky - DM_IoNightSky (24au) | `milkyWayTiltDeg` | float | 62f | -90f–90f | Milky Way plane orientation. |
| Night Sky - DM_IoNightSky (24au) | `milkyWayYawDeg` | float | 35f | 0f–360f |  |
| Night Sky - DM_IoNightSky (24au) | `starRotationDeg` | float |  | 0f–360f |  |
| Night Sky - DM_IoNightSky (24au) | `starsRotateWithClock` | bool | true |  | Stars turn once per Io day with the clock. |
| Night Sky - DM_IoNightSky (24au) | `nightFadeStartDeg` | float | -6f | -30f–30f | Sun elevation (deg) where stars start to fade out / are fully gone. |
| Night Sky - DM_IoNightSky (24au) | `nightFadeEndDeg` | float | 8f | -30f–45f |  |
| Night Sky - DM_IoNightSky (24au) | `nightSkyDayVisibility` | float | 0f | 0f–1f | Star/Milky Way visibility left in full daylight (exposure dims them further). |
| Aurora (24au) | `auroraEnabled` | bool | true |  | Io aurora: always on, strongest at night / eclipse. Low = Na/S blue-green, high = O red/violet. |
| Aurora (24au) | `auroraIntensity` | float | 1.5f | 0f–20f |  |
| Aurora (24au) | `auroraColorLow` | Color | new Color(0.2f, 0.9f, 0.75f, 1f) |  |  |
| Aurora (24au) | `auroraColorHigh` | Color | new Color(0.85f, 0.2f, 0.55f, 1f) |  |  |
| Aurora (24au) | `auroraSpeed` | float | 0.03f | 0f–0.3f |  |
| Aurora (24au) | `auroraHeight` | float | 0.3f | 0.05f–0.8f | Height of the curtains above the horizon (fraction of the sky dome). |
| Aurora (24au) | `auroraCoverage` | float | 0.55f | 0f–1f |  |
| Aurora (24au) | `auroraDayVisibility` | float | 0.1f | 0f–1f |  |
| Anti-ghosting (24as) | `disableVolumetricClouds` | bool | false |  | Io has no atmosphere: force HDRP Volumetric Clouds OFF (Edit + Play, Scene + Game view) via a hidden, unsaved priority-95 volume. Their temporal reprojection smears terrain/lights across the sky. |
| Eclipse Shadows (24av) | `eclipseShadowsEnabled` | bool | true |  | During the eclipse the Jupiter/ring light casts subtle soft shadows (takes over the single HDRP directional shadow from the hidden sun). |
| Eclipse Shadows (24av) | `eclipseShadowStrength` | float | 0.6f | 0f–1f |  |
| Eclipse Shadows (24av) | `eclipseShadowSoftnessDeg` | float | 6f | 0.5f–20f |  |
| Eclipse Ring & Flare (24aq) | `sunOcclusionSoftDeg` | float | 0.6f | 0.05f–3f | Soft edge (deg) where the sun disc slides behind Jupiter's limb. Direct sun = 0 when fully behind. |
| Eclipse Ring & Flare (24aq) | `ringSunBias` | float | 3f | 0f–8f | Ring brightness bias toward the side nearest the sun (higher = more one-sided). |
| Eclipse Ring & Flare (24aq) | `ringEdgeGrowDeg` | float | 10f | 0.5f–30f | Ring starts growing when the sun is within this many degrees outside the limb. |
| Eclipse Ring & Flare (24aq) | `ringMinFactor` | float | 0.35f | 0f–1f | Ring strength at deepest totality (sun at Jupiter's centre) relative to at the limb. |
| Eclipse Ring & Flare (24aq) | `ringLightLux` | float | 20f | 0f–300f | Dim light (lux) cast from the ring / Jupiter direction, scaled by ring strength. |
| Eclipse Ring & Flare (24aq) | `ringFlareIntensity` | float | 0.25f | 0f–1f | Sun lens-flare strength while the sun is behind the limb (ring flare). Flare is otherwise scaled by sun visibility, so it never shows through Jupiter. |
| Eclipse Ring & Flare (24aq) | `eclipseDisableSsgi` | bool | true |  | Turn SSGI off during deep eclipse (kills temporal GI trails behind bright emissives in the dark). |
| Eclipse Ring & Flare (24aq) | `disableOtherSunFlares` | bool | true |  | Disable lens flares on non-sun directional lights (BOTD clone etc.). |
| Eclipse Ring & Flare (24aq) | `Moons` | DMIoMoonSettings[] | > new[] { europa, ganymede, callisto } |  |  |

**Preset / apply methods:** `ApplyAuroraPreset()`, `ApplyEclipseFloorPreset()`, `ApplyIndirectPreset()`, `ApplyNightSkyPreset()`, `ApplyPolishPreset()`, `ApplyPreset()`, `ApplyRealisticEclipse()`, `ApplyRingPreset()`, `ApplyStartHourPreset()`

## Loot Chests - Timers

**What it does:** World Reloot window, emptied dissolve, Single Loot removal, and full-inventory hold.

**Where:**
- Studio: `Genesis Studio / World / Loot Chests - Timers` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Loot/DM_LootChestProfile.asset`
- Script: `Assets/_Project/Scripts/Events/DMLootChestProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| World Reloot | `relootWindowSeconds` | float | 120f | ≥0f | Seconds (game time, closed only) a World Reloot chest keeps its leftovers before it dissolves for good (D1). |
| World Reloot | `timerStart` | DMLootChestTimerStart | DMLootChestTimerStart.FirstExitWithLeftovers |  | When the reloot window starts counting. |
| World Reloot | `emptiedDissolveDelay` | float | 2f | ≥0f | Seconds after the last item is taken before an emptied chest dissolves. |
| Single Loot | `postExitDestroySeconds` | float | 5f | ≥0f | Seconds from window close until a Single Loot chest is removed. The dissolve plays in the final Dissolve Seconds. |
| Full-inventory hold | `fullInventoryHoldSeconds` | float | 1800f | ≥0f | Seconds a chest keeps items the player could not carry (30 minutes). Restarts on every revisit. |
| Storage | `defaultSlotCount` | int | 20 | ≥1 | Slot count for storage chests that do not set their own. |
| Storage | `rangeCloseAppliesToStorage` | bool |  |  | When on, walking away also closes the storage window. |
| Shared presentation | `lidOpenSeconds` | float | 1f | ≥0f |  |
| Shared presentation | `lidCloseSeconds` | float | 0.8f | ≥0f |  |
| Shared presentation | `openLootWindowDelay` | float | 1f | ≥0f | Seconds after the lid starts opening before the loot window shows. |
| Shared presentation | `interactRange` | float | 3f | ≥0f | Meters from the chest where the open prompt shows. |
| Shared presentation | `closeRangeMeters` | float | DefaultCloseRangeMeters | ≥0f | Meters from the container where an open loot window closes on its own. |
| Shared presentation | `dissolveSeconds` | float | 1.6f | ≥0f | Dissolve length in seconds. Never longer than Post Exit Destroy Seconds. |
| Shared presentation | `dissolveEdgeWidth` | float | 0.06f | ≥0f |  |
| Shared presentation | `dissolveEdgeColor` | Color | new Color(0.831f, 0.627f, 0.090f, 1f) |  |  |
| Shared presentation | `dissolveShader` | Shader |  |  | Dissolve shader reference (no Shader.Find at runtime). |
| Shared presentation | `dissolveMaterial` | Material |  |  | Optional dissolve material template. Wins over the shader when set. |
| Shared presentation | `enemyLootBagPrefab` | GameObject |  |  |  |
| Shared presentation | `EffectiveDissolveSeconds` | float | > Mathf.Min(dissolveSeconds, postExitDestroySeconds) |  |  |

## Loot Chests - Presentation

**What it does:** Lid timing, loot window delay, ranges, storage defaults, and dissolve look.

**Where:**
- Studio: `Genesis Studio / World / Loot Chests - Presentation` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Loot/DM_LootChestProfile.asset`
- Script: `Assets/_Project/Scripts/Events/DMLootChestProfile.cs`

**Key settings:** same script as [Loot Chests - Timers](#loot-chests---timers).

## Vendors

**What it does:** Commissary / Tech markups and daily AC purse.

**Where:**
- Studio: `Genesis Studio / World / Vendors` (panel mode `AssetFolder`)
- Asset folder: `Assets/_Project/Resources/Vendors` (`t:DMVendorProfile`)
- Script: `Assets/_Project/Scripts/Vendor/DMVendorProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `vendorId` | string | "vendor_commissary" |  |  |
|  | `displayName` | string | "Commissary" |  |  |
|  | `kind` | DMVendorKind | DMVendorKind.Commissary |  |  |
|  | `catalog` | DMVendorCatalog |  |  | Daily shop stock only. Not the buy log. |
|  | `buyLog` | DMVendorBuyLog |  |  | Items this vendor will buy from the player. Player slots use this, not the catalog. |
|  | `buyMarkup` | float | DMVendorPricing.DefaultBuyMarkup | ≥0.1f |  |
|  | `sellRate` | float | DMVendorPricing.DefaultSellRate | 0.05f–1f |  |
|  | `purseMin` | int | 500 | ≥0 |  |
|  | `purseMax` | int | 800 | ≥0 |  |
|  | `interactRange` | float | 3.5f | ≥1f |  |
|  | `shopSlots` | int | 42 | ≥1 |  |
|  | `promptText` | string | "Press E — Commissary" |  |  |

## Vendor Stock

**What it does:** Daily listing stock min/max and catalog rows.

**Where:**
- Studio: `Genesis Studio / World / Vendor Stock` (panel mode `AssetFolder`)
- Asset folder: `Assets/_Project/Resources/Vendors` (`t:DMVendorCatalog`)
- Script: `Assets/_Project/Scripts/Vendor/DMVendorCatalog.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `listings` | DMVendorListing[] |  |  |  |

## Vendor Buy Logs

**What it does:** What each vendor will buy from the player. Not shop stock.

**Where:**
- Studio: `Genesis Studio / World / Vendor Buy Logs` (panel mode `AssetFolder`)
- Asset folder: `Assets/_Project/Resources/Vendors` (`t:DMVendorBuyLog`)
- Script: `Assets/_Project/Scripts/Vendor/DMVendorBuyLog.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `items` | ItemData[] |  |  | Every pickup this vendor will buy. Not shop inventory — that is the catalog. |
|  | `Count` | int | > items != null ? items.Length : 0 |  |  |


## 2026-10-26f Night Light
=== 2026-10-26f Night Light (trilight ambient fill on DM_IoSkyProfile) ===
- Studio World > Sky & Orbit: Night Light section - Enable, Ground / Mid / Sky colours, Light Amount (0-400 lux). Fades in with existing nightFadeStartDeg/nightFadeEndDeg (shared with stars). Blends additively with Jupiter-shine / eclipse fill; day lighting unchanged. No aurora-driven light.
- Runtime: DMIoSkyController creates three shadowless specular-free directional fills (sky/mid/ground), NightLightFactor exposed. Live in Play and edit-mode preview (skyRoot DontSave lights).
- Presets Subtle (22 lux) / Default (55 lux, noticeably brighter than prior night) / Extreme (140 lux). ApplyNightLightPreset. Backups *.pre20261026f. No scene saves, no commit.
Docs: Assets/_Project/Documentation/DevManual/02-World.md (2026-10-26f Night Light).

## 2026-10-26g Night Light strength
=== 2026-10-26g Night Light strength (exposure was eating the fills) ===
- Cause: auto-exposure Exposure Limit Min stayed at ~6.4 EV at night (only lowered in eclipse), so added Night Light fills were adapted away. Ambient/indirect was also unchanged by the colours.
- Fix: Light Amount max 400->1200 lux; Extreme preset ~420 lux. New Night Exposure Limit Min (EV, fades with nightFade*/NightLightFactor like eclipse floor) and Night Ambient Boost (adds to Indirect Diffuse + half to reflections at night). Studio Night Light section updated. Presets Subtle 40/4.5/0.35, Default 120/3.5/0.75, Extreme 420/2/1.6. Backups *.pre20261026g.
Docs: Assets/_Project/Documentation/DevManual/02-World.md (2026-10-26g).
## 2026-10-26h Night Light sky disc
=== 2026-10-26h Night Light sky disc removed (fills must not render as celestial bodies) ===
- Cause: HDRP HDAdditionalLightData defaults (interactsWithSky=true, angularDiameter=0.5 deg) made the night sky fill draw as a bright white sun-like disc/glow at zenith when Light Amount was high. Mid/ground fills could contribute flare/glow the same way.
- Fix (DMIoSkyController.MakeNightFill): interactsWithSky=false, angularDiameter=0, flareSize=0, flareMultiplier=0, surfaceTexture=null; kill any LensFlareComponentSRP on the fill. Ground lighting unchanged (ApplyNightLights intensities/colours/directions same). Real sun, Jupiter shine, and moons untouched.
- Backups *.pre20261026h. No scene/prefab saves, no commit.
Docs: Assets/_Project/Documentation/DevManual/02-World.md (2026-10-26h).
## 2026-10-26i Milky Way day fade
=== 2026-10-26i Milky Way day fade + smoothness ===
- Cause: MW (and stars) used nightSkyDayVisibility; Anthony's asset had it ~0.95 so the MW dust/band noise showed as blotchy daytime fog/clouds, amplified by exposure.
- Fix: milkyWayDayVisibility (default 0) + milkyWaySmoothness; fades with nightFadeStart/End via DMIoNightSky. Shader: separate _MWVis, smoother FBM/dust, AE-tempered MW/aurora. Aurora Day Visibility default 0 + Smoothness. Studio Night Sky > Milky Way presets. Clouds/fog untouched.
Docs: Assets/_Project/Documentation/DevManual/02-World.md (2026-10-26i).
## 2026-10-26j Night Shadow
=== 2026-10-26j Night Shadow (Jupiter-shine directional at night) ===
- One night key casts shadows: Jupiter-shine (not the Night Light fills). Hands off the single HDRP directional cascade from the sun when NightLightFactor > 0.5 and sun elev is in/below the nightFade band (also keeps eclipse shadow handoff). Edit preview: no cast.
- Settings on DM_IoSkyProfile under Night Light: enableNightShadow, nightShadowStrength, nightShadowSoftnessDeg, nightShadowDirectionMode + custom yaw/pitch, nightShadowResolutionTier, nightShadowMaxDistance. Presets Subtle/Default/Extreme.
Docs: Assets/_Project/Documentation/DevManual/02-World.md (2026-10-26j).

## 2026-10-26m Night sky dither / noise + Sky Look presets
- **Banding:** `DM_IoNightSky.shader` adds triangular/IGN dither (`_DitherEnabled` / `_DitherAmount`) after exposure; softExp uses `_SoftExpPower` (profile `nightSkySoftExpPower`, default 0.28). MW/aurora use 6-octave rotated FBM + `FbmWarp`. Driven live from `DMIoNightSky` / `DM_IoSkyProfile`.
- **Sky Look Preset:** Top of Studio World > Sky & Orbit — Custom / Subtle / Normal / Extreme (body-weight style stash). Custom seeds from current asset (MW 0.15, aurora 0.1). Subtle/Normal MW+aurora <= Custom; Extreme brighter with dither. Excludes orbit/time. Edit flips to Custom.

## 2026-10-26n Moon halo + aurora over bodies
- `_DMSkyOcc` star/MW cutout: tight disc + small soft (`starOcclusionMarginDeg` / `starOcclusionSoftDeg`); soft straddles the limb. Aurora uses `_AuroraOverBodies` / `auroraOverSkyBodies` (default on) and is not cut out — additive over moons/Jupiter. `DM_IoSkyBody` limb floor avoids black rims. In Sky Look presets.
