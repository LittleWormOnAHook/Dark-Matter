# Audit_01 — Core

**HLA:** §2.1 Core · §6 Services · §8 Persistence  
**Paths:** `Scripts/Core/`, `Scripts/Managers/`  
**Audit date:** July 2026

---

## Excellent / keep

- **`GameSession`** — static phase machine (`MainMenu` → `Playing`), `SubsystemRegistration` reset, `GameStarted` event. Clean gameplay gate used across 30+ call sites.
- **`GameSaveData` versioning** — explicit `version = 17`, incremental migration gates in apply path.
- **`SaveSlotScreenshotUtility`** + `SaveSlotInfo` — isolated slot thumbnail I/O.
- **`GameSettings`** + **`PlatformGraphicsBootstrap`** — PC-first quality tiering, settings-driven re-apply.
- **`PoolManager`** — documented DontDestroyOnLoad pooling pattern.
- **`CompanionSystemsBootstrap`** — single composition root for GameState + Communications + legacy bridges.
- **`Features/GameState` direction** — provider adapter pattern is the migration template for WorldState and save contributors.

---

## Move later (→ Features/)

| Current | Target | Rationale |
|---------|--------|-----------|
| `PostProcessingController` | `Features/Presentation/` | Rendering concern |
| `PlatformGraphicsBootstrap` | `Features/Platform/` or Settings | Platform config |
| Save apply blocks per domain inside `GameSaveSystem` | `ISaveContributor` per feature | Core = file I/O only |
| Companion/pet wiring in `CompanionSystemsBootstrap` | respective feature bootstraps | Core registers, features own components |
| Credits fallback via `UIManager` in save | `ColonyGameStateProvider` / roster only | GDD AC canonical source |

---

## Risk

| Risk | Severity | Detail |
|------|----------|--------|
| **`GameSaveSystem` god class** | High | ~675 lines; imports 15+ feature namespaces; save + apply + UI refresh |
| **FindObject in save path** | High | `UIManager`, `QuestManager`, `HovercraftController[]`, `PowerGenerator[]` |
| **Singleton proliferation** | Medium | `SimpleGameManager`, roster, quest, pet, achievement from save/load |
| **Load → `BeginNewGameSession`** | Medium | Save layer depends on manager Awake order |
| **Dual player resolution** | Low | `PlayerLocator` vs `PlayerReference` overlap |
| **Missing WorldStateBootstrap** | Medium | HLA §6.3 gap — evolutionary state only in monolithic save |

---

## WorldState fields (Core domain)

Recommended **`SessionSnapshot`** slice:

| Field | Source today |
|-------|--------------|
| `SessionPhase` | `GameSession.Phase` |
| `HasSessionStarted` | `GameSession.HasStarted` |
| `ActiveSaveSlotIndex` | last load/save slot |
| `LastSaveUtcTicks` | `GameSaveData.savedAtUtcTicks` |
| `SaveFormatVersion` | `GameSaveData.version` |
| `BootstrapReadyFlags` | GameState + Comms booted (future) |

**Interim:** `PlayerSnapshot` already carries `SessionPhase` / `HasSessionStarted` via `PlayerGameStateProvider`.

---

## Dependencies

**Inbound:** `MainMenuController`, `GameStartPopup`, `UIManager`, 30+ `GameSession.HasStarted` gates, `PlayerGameStateProvider`.

**Outbound (Core →):** Player vitals/inventory, QuestManager, CraftingManager, PioneerRosterManager, PetManager, AchievementManager, BuildingOperationRegistry, vehicles, generators, ItemRegistry, UIManager.

```
MainMenu → GameSaveSystem → [15+ managers]
SimpleGameManager → CompanionSystemsBootstrap → GameState + Communications
```

**Phase B action:** Extract `ISaveContributor`; add `SessionWorldStateProvider`; do not expand `GameSaveSystem` further.
