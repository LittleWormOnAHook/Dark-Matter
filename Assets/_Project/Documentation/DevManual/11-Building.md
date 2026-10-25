# Building — Genesis Studio

**Status:** Stub (generated tables; notes pending)  
**Open:** Tools → Dark Matter Genesis → Genesis Studio → **Building**  
Stone build mode — keep Building Studio in sync when adding subsystems.

## Tabs

- [Preview Ghosts](#preview-ghosts)
- [Built Tints](#built-tints)
- [Snap & Grid](#snap--grid)
- [Placement](#placement)
- [Doors](#doors)
- [Creation Effects](#creation-effects)
- [Library](#library)
- [Building Studio](#building-studio)

## Preview Ghosts

**What it does:** Snap/build and blocked hologram materials, colors, and alpha.

**Where:**
- Studio: `Genesis Studio / Building / Preview Ghosts` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Building/DM_BuildingGhostProfile.asset`
- Script: `Assets/_Project/Scripts/Building/DMBuildingGhostProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Preview — Snap & build (valid seat) | `validGhostMaterial` | Material |  |  | Optional HDRP material for the green build-ready hologram. Color and alpha still tint each frame. |
| Preview — Snap & build (valid seat) | `ghostColor` | Color | new Color(0.18f, 0.78f, 0.36f, 1f) |  |  |
| Preview — Snap & build (valid seat) | `ghostAlpha` | float | 0.45f | 0f–1f |  |
| Preview — Blocked seat | `blockedGhostMaterial` | Material |  |  | Optional material when the seat is invalid or unaffordable. |
| Preview — Blocked seat | `blockedGhostColor` | Color | new Color(0.561f, 0.118f, 0.369f, 1f) |  |  |
| Preview — Blocked seat | `blockedGhostAlpha` | float | 0.45f | 0f–1f |  |
| Built piece tints | `finishedColor` | Color | new Color(0.62f, 0.58f, 0.52f, 1f) |  |  |
| Built piece tints | `glassColor` | Color | new Color(0.85f, 0.92f, 0.95f, 1f) |  |  |
| Built piece tints | `glassAlpha` | float | 0.35f | 0f–1f |  |
| Built piece tints | `builtMaterial` | Material |  |  | Material for built pieces when their style has no finish. Tinted by Finished mesh. Empty = plain lit surface. |
| Snap | `largeModuleMeters` | float | 4f |  | Edge-to-edge spacing for 4 m footprints (foundations, walls, floors). |
| Snap | `smallModuleMeters` | float | 2f |  | Edge-to-edge spacing for smaller footprints. |
| Snap | `yawStepDegrees` | float | 45f |  | Left Alt + scroll step for the first foundation. Pieces on the building grid turn in 90 degree steps. |
| Snap | `heightStepMeters` | float | 0.25f |  |  |
| Snap | `maxHeightOffsetMeters` | float | 4f |  |  |
| Snap | `edgeSnapRangeMeters` | float | 4f |  |  |
| Snap | `topSnapRangeMeters` | float | 7f |  |  |
| Snap | `buildLookUpDegrees` | float | 45f |  | Extra degrees the camera may look up while build mode is on. Applied to the look-up limit only, then restored. |
| Snap | `buildModeCameraDistanceMultiplier` | float | 1.35f |  | Pull camera back in build mode: saved distance × this, plus extra meters. Keep low indoors to avoid wall clip fight. |
| Snap | `buildModeCameraExtraMeters` | float | 1.25f |  |  |
| Snap | `doorFrameRangeMeters` | float | 5f |  |  |
| Snap | `edgeFacingDot` | float | 0.5f | 0f–1f |  |
| Placement | `buildSeconds` | float | 2f |  |  |
| Placement | `destroyHoldSeconds` | float | 2f |  |  |
| Placement | `aimDistanceMeters` | float | 80f |  |  |
| Placement | `doorSeatDropMeters` | float | 0.4f |  |  |
| Placement | `overlapPaddingMeters` | float | 0.08f |  |  |
| Placement | `newBaseClearanceMeters` | float | 30f |  | A foundation may stand alone (start a new base) only when no foundation is within this many meters. Closer than this it must connect edge to edge to another foundation. 0 = only the very first foundation in the world may stand alone. |
| Base power (0926-generator) | `basePowerSquareMeters` | float | 100f |  | Side of the square power area around a base, centred on its first placed foundation, in meters. Equipment can be placed on the ground inside it. |
| Base power (0926-generator) | `generatorFuelRangeMeters` | float | 200f |  | Generator refuel reaches Plasma Fuel in storage crates within this many meters. |
| Base power (0926-generator) | `generatorTankUnits` | int | 100 |  | Generator tank size in units. 1 Plasma Fuel item = 1 unit. |
| Base power (0926-generator) | `generatorMinutesPerUnit` | float | 5f |  | Minutes of gameplay one fuel unit lasts. |
| Base power (0926-generator) | `powerLoadPeriodMinutes` | float | 30f |  | Load period in minutes: every powered item and force field in the base adds its units once per period on top of the base burn. |
| Base power (0926-generator) | `poweredItemUnitsPerPeriod` | float | 1f |  | Extra Plasma Fuel units per load period for each powered item (lights, equipment). |
| Base power (0926-generator) | `forceFieldUnitsPerPeriod` | float | 2f |  | Extra Plasma Fuel units per load period for each force field. |
| Base power (0926-generator) | `forceFieldsNeedPower` | bool | true |  | Force fields shut off (anyone can walk through) while their base has no powered generator. |
| Base power (0926-generator) | `lightsNeedPower` | bool | true |  | Lights on built pieces go dark while their base has no powered generator. |
| Build Hub zone (0926-build-hub) | `requireBuildHub` | bool | true |  | Every piece except the Build Hub must stand fully inside a Build Hub zone. Off = build anywhere (testing). |
| Build Hub zone (0926-build-hub) | `buildHubZoneMeters` | float | 50f |  | Width and depth (X and Z) of a level 0 Build Hub zone, centred on the hub, in meters. |
| Build Hub zone (0926-build-hub) | `buildHubZoneHeightMeters` | float | 50f |  | Height (Y) of a level 0 Build Hub zone, centred on the hub (half above, half below), in meters. |
| Build Hub zone (0926-build-hub) | `buildHubZoneStepMeters` | float | 25f |  | Meters added to the zone width and depth per upgrade level. |
| Build Hub zone (0926-build-hub) | `buildHubZoneHeightStepMeters` | float | 25f |  | Meters added to the zone height per upgrade level. |
| Build Hub zone (0926-build-hub) | `buildHubMaxLevel` | int | 10 |  | Highest Build Hub upgrade level. |
| Build Hub zone (0926-build-hub) | `buildHubZoneMustBeClear` | bool | true |  | A Build Hub's whole zone must be free of colliders except terrain (characters, creatures, items and built pieces are ignored; triggers count). Also checked when upgrading. |
| Terrain item bootstrap (building node) | `terrainBootstrapDeployRangeMeters` | float | 8f |  | Horizontal distance from the player when a Terrain-tagged item deploys a Build Hub zone. |
| Terrain item bootstrap (building node) | `terrainBootstrapHubPieceId` | string | "stone_si_build_hub" |  | Built piece id spawned as the zone anchor (default Stone Build Hub). |
| Patrol paths (0926-build-hub) | `blockPatrolPaths` | bool | true |  | Pieces may not be built on or across creature / pet patrol paths (DMIPathFollowProvider). |
| Patrol paths (0926-build-hub) | `patrolPathClearanceMeters` | float | 1.5f |  | Clearance around a patrol path polyline, in meters. The ghost bounds are grown by this much before the test. |
| Patrol paths (0926-build-hub) | `patrolPathHeightToleranceMeters` | float | 2f |  | Extra vertical slack for the patrol path test, in meters (path points rarely sit exactly on the ground). |
| Build crosshair | `crosshairDotPixels` | float | 6f |  | Centre dot shown while build mode is on, in pixels. 0 hides it. |
| Build crosshair | `crosshairDotColor` | Color | new Color(1f, 1f, 1f, 0.9f) |  |  |
| Build crosshair | `crosshairDotOutline` | Color | new Color(0f, 0f, 0f, 0.6f) |  |  |
| Layers | `buildingLayerName` | string | "Building" |  | Layer every placed piece lives on. Rebuild Stone Kit creates it when missing. |
| Layers | `builtAimLayers` | LayerMask |  |  | Crosshair ray that finds built pieces for snapping and destroy. The Building layer is always included. Nothing = Building + Climbable. |
| Layers | `groundLayers` | LayerMask |  |  | Ground ray for the first foundation. The Building layer is never included. Nothing = Default + Terrain. |
| Layers | `blockerLayers` | LayerMask |  |  | What blocks a seat besides other pieces. The Building layer is always included. Nothing = Default, Climbable, Resource, Pushable and the PW object layers. |
| Door swing (E) | `doorSwingDegrees` | float | 90f |  |  |
| Door swing (E) | `doorSwingSeconds` | float | 0.35f |  |  |
| Door swing (E) | `doorInteractRangeMeters` | float | 2.4f |  |  |
| Door swing (E) | `gateSwingDegrees` | float | 90f |  | Kit phase 3: both leaves of an 8 m gate swing this far. |
| Door swing (E) | `gateSwingSeconds` | float | 1.4f |  | Kit phase 3: gates are heavy, so they swing slower than doors. |
| Turrets (placed equipment) | `turretInteractRangeMeters` | float | 10f |  | Radius of each turret's InteractZone trigger (sole interact authority, 2026-10-20f). Player inside = can press E; no aim needed. |
| Turrets (placed equipment) | `turretPlayerCameraBackMeters` | float | 1.8f |  | Player-control camera sits this far behind the barrel. |
| Turrets (placed equipment) | `turretPlayerCameraUpMeters` | float | 0.55f |  | Player-control camera sits this far above the barrel. |
| Turrets (placed equipment) | `turretPlayerCameraFov` | float | 60f |  | FOV while seated in a turret. |

## Built Tints

**What it does:** Finished mesh and window glass colors on the ghost profile.

**Where:**
- Studio: `Genesis Studio / Building / Built Tints` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Building/DM_BuildingGhostProfile.asset`
- Script: `Assets/_Project/Scripts/Building/DMBuildingGhostProfile.cs`

**Key settings:** same script as [Preview Ghosts](#preview-ghosts).

## Snap & Grid

**What it does:** Module size, yaw/height, edge and top snap ranges.

**Where:**
- Studio: `Genesis Studio / Building / Snap & Grid` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Building/DM_BuildingGhostProfile.asset`
- Script: `Assets/_Project/Scripts/Building/DMBuildingGhostProfile.cs`

**Key settings:** same script as [Preview Ghosts](#preview-ghosts).

## Placement

**What it does:** Hold build/destroy, aim, overlap, door seat drop.

**Where:**
- Studio: `Genesis Studio / Building / Placement` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Building/DM_BuildingGhostProfile.asset`
- Script: `Assets/_Project/Scripts/Building/DMBuildingGhostProfile.cs`

**Key settings:** same script as [Preview Ghosts](#preview-ghosts).

## Doors

**What it does:** Swing angle, duration, and E interact range.

**Where:**
- Studio: `Genesis Studio / Building / Doors` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Building/DM_BuildingGhostProfile.asset`
- Script: `Assets/_Project/Scripts/Building/DMBuildingGhostProfile.cs`

**Key settings:** same script as [Preview Ghosts](#preview-ghosts).

## Creation Effects

**What it does:** Pop, bounce, VFX and material swap when a hold-to-build finishes.

**Where:**
- Studio: `Genesis Studio / Building / Creation Effects` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/Building/DM_BuildingCreationFxProfile.asset`
- Script: `Assets/_Project/Scripts/Building/DMBuildingCreationFxProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
|  | `enabled` | bool | true |  | Master switch for the whole sequence. |
| Pop & bounce | `duration` | float | 0.3f | 0f–3f | How long the grow/shrink and bounce take. 0 skips the motion. |
| Pop & bounce | `scalePunch` | float | 0.1f | 0f–0.5f | Extra size at the peak. 0.1 = grows 10% then shrinks back. |
| Pop & bounce | `bounceHeightMeters` | float | 0.1f | 0f–1f | How high the piece lifts at the peak before settling back on its seat (m). |
| Pop & bounce | `scaleCurve` | AnimationCurve | Hump() |  | Size over the duration (0 = normal size, 1 = full punch). |
| Pop & bounce | `bounceCurve` | AnimationCurve | Hump() |  | Lift over the duration (0 = on the seat, 1 = full bounce height). |
| VFX | `vfxPrefab` | GameObject |  |  | Optional particle prefab spawned during the sequence. Not parented to the piece. |
| VFX | `vfxDelay` | float | 0f | 0f–3f | Seconds after the build finishes before the VFX spawns. |
| VFX | `vfxLifetime` | float | 2f | 0.1f–10f | Seconds before the spawned VFX is removed. |
| VFX | `vfxAnchor` | DMBuildingFxAnchor | DMBuildingFxAnchor.Base |  | Where the VFX spawns on the piece. |
| VFX | `vfxScaleWithPiece` | bool | true |  | Scale the VFX to the piece's footprint (4 m foundation = 1x). |
| Material swap | `swapMaterial` | bool |  |  | Swap every renderer on the piece to the material below for a while, then restore its finish. |
| Material swap | `swapMaterialAsset` | Material |  |  |  |
| Material swap | `swapStart` | float | 0f | 0f–3f | Seconds after the build finishes before the swap starts. |
| Material swap | `swapDuration` | float | 0.3f | 0f–3f | How long the swap material stays on. |
| Force fields: look | `forceFieldMaterial` | Material |  |  | Material on every force field door. Empty uses the default DM_ForceField material. The colour and sliders below tint whichever material is here. |
| Force fields: look | `forceFieldColor` | Color | new Color(0.25f, 0.75f, 1.6f, 1f) |  | Main glow colour of the field (HDR, so values above 1 glow brighter). |
| Force fields: look | `forceFieldEdgeColor` | Color | new Color(0.6f, 1.2f, 2.4f, 1f) |  | Colour of the rim, the ripples and the glancing-angle glow. |
| Force fields: look | `forceFieldOpacity` | float | 0.25f | 0f–1f | How see-through the field is when nobody is passing. 0 = almost invisible. |
| Force fields: look | `forceFieldEdgeGlow` | float | 1.5f | 0f–4f | Brightness of the rim where the field touches the frame. |
| Force fields: look | `forceFieldEdgeWidth` | float | 0.06f | 0.005f–0.5f | Width of that rim, as a share of the field (0.06 = 6%). |
| Force fields: look | `forceFieldPatternScale` | float | 1.2f | 0.1f–5f | Size of the moving energy pattern. Higher = finer pattern. |
| Force fields: look | `forceFieldScrollSpeed` | float | 0.35f | 0f–3f | How fast the energy pattern drifts. |
| Force fields: pass-through shimmer | `forceFieldPulseBrightness` | float | 1.5f | 0f–4f | How much brighter the whole field flashes when someone crosses it. |
| Force fields: pass-through shimmer | `forceFieldPulseSeconds` | float | 0.6f | 0.05f–3f | How long the flash and the ripple last (s). |
| Force fields: pass-through shimmer | `forceFieldRippleSpeed` | float | 3f | 0.5f–10f | How fast the ripple ring spreads from the crossing point (m/s). |
| Force fields: pass-through shimmer | `forceFieldRippleWidth` | float | 0.35f | 0.05f–2f | Thickness of the ripple ring (m). |
| Force fields: audio | `forceFieldPassClip` | AudioClip |  |  | Sound played when someone crosses the field. Empty uses the electric crackle generated in code. |
| Force fields: audio | `forceFieldPassVolume` | float | 0.35f | 0f–1f |  |
| Force fields: audio | `forceFieldPitchJitter` | float | 0.08f | 0f–0.5f | Random pitch change per crossing (0.08 = up to 8% higher or lower). |
| Force fields: audio | `forceFieldSoundCooldown` | float | 0.4f | 0f–2f | Minimum time between crossing sounds for the same person (s). |
| Force fields: audio | `forceFieldAudioMaxDistance` | float | 14f | 2f–40f | Distance where the field sounds fade to silence (m). |
| Force fields: audio | `forceFieldIdleHumClip` | AudioClip |  |  | Optional looping idle sound. Empty uses a hum generated in code. Only plays when the volume below is above 0. |
| Force fields: audio | `forceFieldIdleHumVolume` | float | 0f | 0f–1f |  |
| Force fields: behaviour | `forceFieldLetFriendliesThrough` | bool | true |  | Let the player and companions walk through. Off keeps every field solid. Bullets, enemies and weather are always stopped. |
| Force fields: behaviour | `forceFieldSenseDepthMeters` | float | 0.9f | 0.2f–3f | How far in front of or behind the field someone must be for it to open (m). |
| Force fields: behaviour | `forceFieldCloseDelaySeconds` | float | 0.35f | 0f–2f | How long the field stays open after the last person leaves it (s). |
| Force fields: corner blocks | `forceFieldCorners` | bool | true |  | Put a small block in each corner of every force field. |
| Force fields: corner blocks | `forceFieldCornerMaterial` | Material |  |  | Material on the corner blocks. Empty uses DM_ForceFieldCorner in Resources/Building (Building Studio can create it), or a plain dark metal. |
| Force fields: corner blocks | `forceFieldCornerSize` | float | 0.18f | 0.05f–0.6f | Width and height of each corner block (m). |
| Force fields: corner blocks | `forceFieldCornerDepth` | float | 0.24f | 0.05f–0.8f | Thickness of each corner block, measured through the field (m). |
| Force fields: corner blocks | `forceFieldStripMaterial` | Material |  |  | Material for the glowing strip sandwiched in the middle of each block. Empty uses a generated glow. Its emission colour is replaced by the two colours below. |
| Force fields: corner blocks | `forceFieldStripUnpoweredColor` | Color | new Color(1f, 0.05f, 0.03f, 1f) |  | Strip colour while the base has no power. The field material and its collider are off in this state. |
| Force fields: corner blocks | `forceFieldStripPoweredColor` | Color | new Color(0.1f, 1f, 0.2f, 1f) |  | Strip colour once a fuelled generator powers the base and the field is on. |
| Force fields: corner blocks | `forceFieldStripGlow` | float | 4f | 0f–30f | How bright the strip glows (multiplies both colours). |
| Force fields: corner blocks | `forceFieldStripThickness` | float | 0.035f | 0.005f–0.2f | Thickness of the glowing strip (m). |

## Library

**What it does:** Build styles (Stone, Iron, Silicate, ...): each kit's cost item, finishes, kit shapes and parts with prefab, icon, shape and cost.

**Where:**
- Studio: `Genesis Studio / Building / Library` (panel mode `BuildingLibrary`)

## Building Studio

**What it does:** Full grouped editor — preview, snap, placement, doors, M-key materials, Roadmap tab.

**Where:**
- Studio: `Genesis Studio / Building / Building Studio` (panel mode `ExternalTool`)
- Menu: `Tools/Dark Matter Genesis/Buildings/Building Studio`

