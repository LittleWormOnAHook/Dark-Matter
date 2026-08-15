# PPT System — People, Places, Things

**Status:** Phase 1 playable slice (Aug 2026)

Discovery-aware NPC directions: **Tap E** talks / opens the quest board. **Hold E** opens a keyword menu of places and things the player has heard about. A precise answer points and draws a 3-second lime terrain tracer; an unknown answer shrugs and barks.

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
| Radio | Reads `PptKnowledgeSnapshot` only; never calls `PptManager` |

## Player flow

1. **Tap E** — existing talk / quest board (`QuestGiverNpc`; shop via `IVendor` when added).
2. **Directions button** on the NPC dialog / quest board (when the NPC has PPT Directions) opens the keyword menu. **Hold E** still works as a shortcut.
3. Pick keyword → resolver returns precise / general / refer / unknown.
4. Precise → point gesture + lime terrain tracer. Unknown → shrug + bark.

## Key paths

```
Assets/_Project/Scripts/PPT/          Runtime + data SOs
Assets/_Project/Scripts/Vendor/       IVendor
Assets/_Project/Scripts/UI/           PptDirectionsMenuUI
Assets/_Project/Features/PPT/         GameState adapter
Assets/_Project/Resources/PPT/        Phase 1 sample registry
Assets/_Project/Editor/PPT/           Phase 0+1 verify / Phase 1 wiring menus
```

## Editor re-run (required after disk-only agent merges)

Prior cloud agents authored Phase 0+1 on disk without Unity Editor execution. On a machine with Unity open on the playable scene, run:

1. `Tools/Dark Matter Genesis/PPT/Phase 0+1 - Verify Foundation + Wire Sample Registry`
2. Confirm dialog reports **PASS** and Console has no new PPT errors after compile/domain reload
3. Play Mode: Tap E on GERALD → **Directions** (or Hold E) → pick Camp / Sulfur Dunes / Old Runes

## Phase 1 sample content

Loaded from `Resources.Load("PPT/PptRegistry")`.

| Asset | Id | Notes |
|-------|----|-------|
| `PptEntry_Camp` | `place_camp` | Pioneer Camp at GERALD / prefab position |
| `PptEntry_SulfurDunes` | `place_sulfur_dunes` | Sulfur Plains, authored (-80, 2, 60) |
| `PptEntry_OldRunesOfPedra` | `place_old_runes_of_pedra` | Precursor Ruin Belt, authored (40, 2, -50) |
| `PptEntry_Mushrooms` | `thing_mushrooms` | Logged when `guide_supply_run` is accepted |
| `PptNpcProfile_PioneerGuide` | `pioneer_guide` | Talk options QuestBoard + Directions |
| `PptKeywordSource_Starter` | `session_start` + `guide_supply_run` | Camp briefing keywords on session start |

`session_start` logs Camp, Sulfur Dunes, and Old Runes of Pedra so Hold E works without accepting a quest. Sample entries use `requiresDiscovery: 0` until scanner POIs exist.

### Wired NPCs

- Prefab `Assets/_Project/Prefabs/NPCs/QuestGiver_PioneerGuide.prefab`
- Playable scene NPC **GERALD** in `Assets/Dark Matter Genesis v1.56.unity` and `Assets/_Project/Scenes/Dark Matter Genesis v1.57.unity`
- Editor menus:
  - `Tools/Dark Matter Genesis/PPT/Phase 0+1 - Verify Foundation + Wire Sample Registry`
  - `Tools/Dark Matter Genesis/PPT/Phase 1 - Wire Pioneer Guide + Sample Registry`
- ALEXO is **not** wired (shares `npcId: pioneer_guide` in scene data)

### Playtest

1. Start a game (or load a save).
2. Aim at GERALD. Prompt should include **Hold E — Ask directions**.
3. Tap E → quest/talk panel → **Directions** (or Hold E) → 3 keywords (Camp, Old Runes of Pedra, Sulfur Dunes).
4. Pick one → phrase + lime tracer, general area, or shrug.
5. Accept Supply Run → Mushrooms is added to the keyword log.

## Wiring a new NPC

1. Add `PptNpcInteractor` + `PptNpcGestureController` to the quest giver prefab or scene object.
2. Assign `PptNpcProfile` (or register the profile in `Resources/PPT/PptRegistry`).
3. Add `Point` / `Shrug` states to the NPC animator (optional; gestures skip if missing).
4. Author `PptEntry` assets; link `mapMarkerDiscoveryId` to `MapMarker` when scanner discovery should gate the keyword.

## Save

`GameSaveData.pptKnownKeywordIds` (save version **22**). Old saves still receive `session_start` briefing keywords after load.

## Communications

`PptGameStateProvider` exposes `PptKnowledgeSnapshot` for future radio context packs. Radio must not call `PptManager`.

## Phase checklist

| Phase | Name | Status |
|-------|------|--------|
| 0 | Foundation (runtime, save v22, Hold E contract) | On disk — re-verify via Phase 0+1 Editor menu |
| 1 | MVP Directions (sample registry + GERALD / Pioneer Guide + dialog Directions) | On disk — re-wire / playtest via Phase 0+1 Editor menu |
| 2 | Talk hub on `QuestGiverDialogUI` + Journal Knowledge tab + `[ppt:]` tags | Directions button on dialog (partial); Knowledge tab / tags not started |
| 3 | Point / Shrug animator states (procedural rotate already in Phase 1) | Not started |
| 4 | Conversation player for `PptConversationDefinition` | Schema only |
| 5 | Radio consumes `PptKnowledgeSnapshot` | Snapshot hook only |
