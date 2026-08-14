# PPT System — People, Places, Things

**Status:** Phase 1 MVP on disk (Aug 2026)

## Locked product decisions

| Topic | Decision |
|-------|----------|
| Direction tracer | 3-second **lime** (`PositiveGreen`) terrain-hugging path; curves around static/dynamic colliders via spherecast detours |
| Unknown keyword | NPC **shrug** gesture + bark variation |
| Shop | Separate **`IVendor`** interface (`Project.Vendor`); not on quest giver |
| Biome / hazard areas | `PptSurfaceRegionAnchor` + `ExposureZoneVolume` centers / `displayName` tags |
| Conversation | Minimal in-house node graph (`PptConversationDefinition`) until Communications Phase 4 |
| Directions menu | **2–3** choices per page + **More…** paging |
| Quest givers | Usually **do not** give precise directions; prefer general area, refer NPC, or unknown |

## Player flow

1. **Tap E** — existing talk / quest board (`QuestGiverNpc`; shop via `IVendor` when added).
2. **Hold E** — `PptNpcInteractor` opens directions menu (discovered keywords only).
3. Pick keyword → resolver returns precise / general / refer / unknown.
4. Precise → point gesture + lime terrain tracer. Unknown → shrug + bark.

## Key paths

```
Assets/_Project/Scripts/PPT/          Runtime + data SOs
Assets/_Project/Scripts/Vendor/       IVendor
Assets/_Project/Scripts/UI/           PptDirectionsMenuUI
Assets/_Project/Features/PPT/         GameState adapter
```

## Wiring a new NPC

1. Add `PptNpcInteractor` + `PptNpcGestureController` to quest giver prefab.
2. Assign `PptNpcProfile` (or register profile in `Resources/PPT/PptRegistry`).
3. Add `Point` / `Shrug` states to NPC animator (optional; gestures skip if missing).
4. Author `PptEntry` assets for places; link `mapMarkerDiscoveryId` to `MapMarker`.

## Save

`GameSaveData.pptKnownKeywordIds` (save version **22**).

## Communications

`PptGameStateProvider` exposes `PptKnowledgeSnapshot` for future radio context packs.
