# Intro and Quests — Design Plan (Index)

**Project:** Dark Matter: Genesis  
**Status:** Design lock package (Sep 2026)  
**Branch:** `cursor/prologue-intro-crash-design-e9c9`  
**Player:** Kade

This page indexes the **intro slice** (geomagnetic crash → horizon camp) and **quests / navigation / journal** systems. Implementation is backlog; disk truth for shipped code remains `World_Engine_Disk_Status.md`.

---

## Intro (prologue Act 0)

| Doc | Scope |
|-----|--------|
| [Prologue_Intro_Crash_To_Horizon.md](Prologue_Intro_Crash_To_Horizon.md) | Crash, salvage (UEA pistol + EEB), corral tutorial march, empty horizon pad → prologue ML beats |
| [Map_Search_Zone_System.md](Map_Search_Zone_System.md) | Map/minimap alpha search rings; compass **gold bearing dot** (no ring); scanner 3s ping |
| [Field_Log_Devices.md](Field_Log_Devices.md) | EEB pickups; Journal **Coordinates** (bold gold); Io recordings |

**Intro beats (short):** geomagnetic shuttle miss → crate loot + first EEB → Journal coords plot search zone → walk corrals → resolve pad → `intro_complete`.

---

## Quests (journal, tracking, navigation)

| Doc | Scope |
|-----|--------|
| [Quest_Navigation_And_Tracking.md](Quest_Navigation_And_Tracking.md) | **Quests (G)** tab; shelves Main / Side / Biome / Area; nav modes (zone, dot, symbol, coords, PPT); HUD **3-track** + chains; right-click track / current / untrack |
| [Map_Search_Zone_System.md](Map_Search_Zone_System.md) | Shared search-zone grammar for quests and intro |
| [Field_Log_Devices.md](Field_Log_Devices.md) | Coordinate grants from EEB; dialog from givers / companions / co-protagonist |

**Journal tabs (target):** **Quests (G)** — quest shelves; **Journal (J)** — Field Logs + coordinates chronicle.

---

## Canon refs

- GDD 5.0 A6 (prologue / Kairos naming)  
- `Player_Identity_Kade.md`  
- `Narrative_Package_V2_Colony_Horizon.md` (default spine for hub handoff)

---

## Implementation touchpoints (Unity)

| Area | Files |
|------|--------|
| Quests tab + shortcut G | `JournalWindowId.cs`, `JournalTabRail.cs`, `JournalPanelUI.cs`, `InputSystem_Actions.inputactions`, `UIManager.cs` |
| HUD tracker cap | `ActiveQuestHudUI.cs`, `QuestProgress` / `QuestManager` |
| Search zones | `MapUI` overlays, `CompassHudUI`, `ScannerSweepController` |
| Field logs | `FieldLogDefinition`, `DMFieldLogDevice`, save `collectedLogIds` |
