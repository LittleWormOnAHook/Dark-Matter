# Audio — Genesis Studio

**Status:** Stub (generated tables; notes pending)  
**Open:** Tools → Dark Matter Genesis → Genesis Studio → **Audio**  
Global music, SFX, and footstep libraries.

## Tabs

- [Game Audio](#game-audio)

## Game Audio

**What it does:** Music, body impacts, weapon clashes, player pain and circular hit-direction feedback.

**Where:**
- Studio: `Genesis Studio / Audio / Game Audio` (panel mode `SingletonAsset`)
- Asset: `Assets/_Project/Resources/GameAudioProfile.asset`
- Script: `Assets/_Project/Scripts/Audio/GameAudioProfile.cs`

**Key settings** (from code):

| Group | Field | Type | Default | Range | Notes |
|---|---|---|---|---|---|
| Music | `musicTracks` | AudioClip[] |  |  |  |
| Music | `musicVolume` | float | 0.45f | 0f–1f |  |
| Music | `shuffleMusic` | bool | true |  |  |
| Music | `loopCurrentTrack` | bool | true |  |  |
| Loading | `loadingAmbience` | AudioClip |  |  |  |
| Loading | `loadingAmbienceVolume` | float | 0.55f | 0f–1f |  |
| Footsteps | `defaultFootsteps` | FootstepSurfaceSet | FootstepSurfaceSet.CreateDefault() |  | Fallback when a tag or terrain layer 0-10 has no clips of its own. |
| Footsteps | `surfaceFootsteps` | FootstepSurfaceSet[] |  |  | Unity tag matches (Terrain, Climbable, Default). Empty clip arrays fall back to Default. |
| Footsteps | `terrainLayerFootsteps` | FootstepTerrainLayerSet[] |  |  | Terrain splat slots 0-10. Disabled or empty slots fall back to tag, then Default. |
| Landing | `defaultLandingClips` | AudioClip[] |  |  |  |
| Landing | `minLandingSpeed` | float | 2.5f | ≥0.5f |  |
| Landing | `hardLandingSpeed` | float | 9f | ≥0.5f |  |
| Landing | `landingVolume` | float | 0.9f | 0f–1f |  |
| Combat — Melee | `meleeSwingClips` | AudioClip[] |  |  |  |
| Combat — Melee | `meleeHitClips` | AudioClip[] |  |  |  |
| Combat — Melee | `meleeCriticalHitClips` | AudioClip[] |  |  |  |
| Combat — Body impacts (not weapon clashes) | `meleeBodyHitClips` | AudioClip[] |  |  | Confirmed melee damage to a body. Empty means silent, never falls back to metallic clash clips. |
| Combat — Body impacts (not weapon clashes) | `meleeBodyCriticalHitClips` | AudioClip[] |  |  |  |
| Combat — Body impacts (not weapon clashes) | `meleeBodyHitVolume` | float | 0.95f | 0f–1f |  |
| Combat — Body impacts (not weapon clashes) | `meleeClashVolume` | float | 0.9f | 0f–1f | Existing Melee Hit / Critical Hit libraries above are now weapon/guard clashes only. |
| Combat — Player receives damage | `playerBodyHitClips` | AudioClip[] |  |  |  |
| Combat — Player receives damage | `playerPainClips` | AudioClip[] |  |  |  |
| Combat — Player receives damage | `playerBodyHitVolume` | float | 1f | 0f–1f |  |
| Combat — Player receives damage | `playerPainVolume` | float | 0.65f | 0f–1f |  |
| Combat — Player receives damage | `playerPainMinInterval` | float | 0.8f | ≥0f |  |
| Combat — Center circular hit direction | `playerHitIndicatorEnabled` | bool | true |  |  |
| Combat — Center circular hit direction | `playerHitIndicatorRadius` | float | 500f | 25f–750f |  |
| Combat — Center circular hit direction | `playerHitIndicatorThickness` | float | 5f | 1f–16f |  |
| Combat — Center circular hit direction | `playerHitIndicatorArc` | float | 55f | 15f–100f |  |
| Combat — Center circular hit direction | `playerHitIndicatorSeconds` | float | 0.65f | 0.15f–2f |  |
| Combat — Center circular hit direction | `playerHitIndicatorColor` | Color | Project.UI.DarkMatterGenesisUiPalette.RichFuchsia |  |  |
| Combat — Ranged | `rangedHitClips` | AudioClip[] |  |  |  |
| Combat — Ranged | `rangedCriticalHitClips` | AudioClip[] |  |  |  |
| Combat — Ranged | `punchSwingClips` | AudioClip[] |  |  |  |
| Combat — Ranged | `punchHitClips` | AudioClip[] |  |  |  |
| Combat — Ranged | `punchCriticalHitClips` | AudioClip[] |  |  |  |
| Combat — Ranged | `resourceHitClips` | AudioClip[] |  |  |  |
| Combat — Ranged | `poiseStaggerDingClips` | AudioClip[] |  |  | Short ding played three times (rising pitch) when poise breaks / stagger triggers. |
| Combat — Ranged | `poiseStaggerDingSpacing` | float | 0.075f | 0.04f–0.2f |  |
| Combat — Ranged | `poiseStaggerDingVolume` | float | 0.92f | 0f–1f |  |
| Combat — Ranged | `combatVolume` | float | 1f | 0f–1f |  |
| UI | `buttonClickClips` | AudioClip[] |  |  |  |
| UI | `uiDenyClips` | AudioClip[] |  |  |  |
| UI | `inventoryItemClickClips` | AudioClip[] |  |  |  |
| UI | `itemUseClips` | AudioClip[] |  |  |  |
| UI | `itemEquipClips` | AudioClip[] |  |  |  |
| UI | `itemUnequipClips` | AudioClip[] |  |  |  |
| UI | `itemSplitClips` | AudioClip[] |  |  |  |
| UI | `itemDropClips` | AudioClip[] |  |  |  |
| UI | `itemPickupClips` | AudioClip[] |  |  |  |
| UI | `achievementUnlockClips` | AudioClip[] |  |  |  |
| UI | `levelUpClips` | AudioClip[] |  |  |  |
| Loot Chests | `lootChestOpenClips` | AudioClip[] |  |  | World loot chest lid opening (3D at the chest). Empty = silent. |
| Loot Chests | `lootChestCloseClips` | AudioClip[] |  |  | World loot chest lid closing. Empty = silent. |
| Loot Chests | `lootChestDissolveClips` | AudioClip[] |  |  | Loot chest dissolve start. Empty = silent. |
| Loot Chests | `lootChestVolume` | float | 0.8f | 0f–1f |  |
| Loot Chests | `uiVolume` | float | 0.85f | 0f–1f |  |
| 3D Playback | `sfxSpatialBlend` | float | 1f | 0f–1f |  |
| 3D Playback | `sfxMinDistance` | float | 1f |  |  |
| 3D Playback | `sfxMaxDistance` | float | 22f |  |  |
| 3D Playback | `clipField` | string |  |  |  |
| 3D Playback | `folderPath` | string |  |  |  |
| 3D Playback | `clips` | AudioClip[] |  |  |  |
| 3D Playback | `surfaceTag` | string | "Default" |  |  |
| 3D Playback | `walkClips` | AudioClip[] |  |  |  |
| 3D Playback | `runClips` | AudioClip[] |  |  |  |
| 3D Playback | `landingClips` | AudioClip[] |  |  |  |
| 3D Playback | `walkStepDistance` | float | 2.1f | ≥0.05f |  |
| 3D Playback | `runStepDistance` | float | 2.8f | ≥0.05f |  |
| 3D Playback | `volume` | float | 0.85f | 0f–1f |  |
| 3D Playback | `layerIndex` | int |  | 0–10 |  |
| 3D Playback | `label` | string | "Terrain Layer" |  | Authoring label. Live tiles: 0-1 sand, 2-5 rock/cliff, 6 mud, 7 snow. |
| 3D Playback | `enabled` | bool | true |  | Off or empty clips = use tag, then Default. |
| 3D Playback | `walkClips` | AudioClip[] |  |  |  |
| 3D Playback | `runClips` | AudioClip[] |  |  |  |
| 3D Playback | `landingClips` | AudioClip[] |  |  |  |
| 3D Playback | `walkStepDistance` | float | 2.1f | ≥0.05f |  |
| 3D Playback | `runStepDistance` | float | 2.8f | ≥0.05f |  |
| 3D Playback | `volume` | float | 0.85f | 0f–1f |  |
| 3D Playback | `layerName` | string | "Ambient Layer" |  |  |
| 3D Playback | `clips` | AudioClip[] |  |  |  |
| 3D Playback | `minInterval` | float | 4f | ≥0.5f |  |
| 3D Playback | `maxInterval` | float | 12f | ≥0.5f |  |
| 3D Playback | `volume` | float | 0.65f | 0f–1f |  |
| 3D Playback | `spatialBlend` | float | 1f | 0f–1f |  |
| 3D Playback | `pitchMin` | float | 0.95f | 0.8f–1.2f |  |
| 3D Playback | `pitchMax` | float | 1.05f | 0.8f–1.2f |  |
| 3D Playback | `playAtRandomPointInZone` | bool | true |  |  |

