# Audit_07 — Systems (UI, Audio, Progression)

**HLA:** §2.8 Presentation · §2.10 Audio  
**Paths:** `Scripts/UI/`, `Scripts/Audio/`, `Scripts/Progression/`, `Scripts/Achievements/`  
**Audit date:** July 2026

---

## Excellent / keep

- **`ShiftUiTheme` + `SurvivalPioneerUiPalette`** — locked palette; Communications HUD compliant.
- **`FullscreenUiNavigator`** — modal stack / pause coordination pattern.
- **`JournalPanelUI` + tab rail** — hub for map, quests, roster, crafting library.
- **`GameAudioManager`** — mixer routing, volume profiles.
- **`Communications` Presentation stack** — RadioHudUI, audio pipeline (Phase 8 MVP).
- **`PlayerProgressionManager`** — level/XP/skills isolated domain.
- **`AchievementManager` + bridges** — event-driven progress from gameplay signals.

---

## Move later

| Current | Target |
|---------|--------|
| Manager bootstrapping in `UIManager` | Core bootstrap registry |
| Legacy Pi wallet UI | remove on AC cleanup pass |
| 134 UI scripts | document modules; migrate presenters per feature |
| `RadioHudUI` in Assembly-CSharp | optional `Project.Features.Communications.UI` asmdef |

---

## Risk

| Risk | Detail |
|------|--------|
| **`UIManager` god bootstrap** | spawns QuestManager, roster, achievements, crafting |
| **14+ UI singletons** | tooltips, dialogs, context menus |
| **Gameplay managers in UI layer** | violates Presentation boundary |
| **Legacy Pi UI** | `piBalanceText` contradicts GDD |
| **Partial-class sprawl** | MapUI, PioneerRosterPanelUI, BuildingControlPanelUI |
| **Communications Update loop** | manager auto-advance in Update (acceptable for queue; monitor perf) |

---

## WorldState / Experience fields

Presentation consumes snapshots — does not own fields.

Experience telemetry from Presentation:

| Signal | Source |
|--------|--------|
| `MinutesSinceLastTransmission` | CommunicationsManager events |
| `RadioDensity` | queue depth + frequency |
| `CommunicationDensity` | HUD toasts + radio + journal opens |
| `SilenceWindowActive` | ExperienceDirector schedules |

---

## Dependencies

**Inbound:** GameSession, SurvivalStats, ExposureStatusService, all managers for display.  
**Outbound:** user commands → gameplay; `CommunicationsBootstrap` wires radio to canvas.

**Rule:** New UI reads `IGameStateService` / `IWorldStateService` for display data — not managers.
