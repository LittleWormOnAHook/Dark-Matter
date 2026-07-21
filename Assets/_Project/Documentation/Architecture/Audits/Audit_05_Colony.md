# Audit_05 — Colony

**HLA:** §2.4 Simulation · WoOS layer 2  
**Paths:** `Scripts/Pioneers/`, `Scripts/Building/`, `Scripts/Pet/`  
**Audit date:** July 2026

---

## Excellent / keep

- **`PioneerRosterManager`** — 25-cap roster, expedition trio, AC, echo chronicle — core colony hub.
- **`SkilledPioneerRecord`** — runtime pioneer data model usable for Simulation.
- **`BuildingOperationRegistry`** — assignments, production queues, save snapshot builder.
- **`BuildingControlPanel` + 5-tab UI shell** — GDD-aligned in-world terminal pattern.
- **`FacilityTaskRunner`** — production tick bridge.
- **`ColonyGameStateProvider` + `CrewGameStateProvider`** — read model already wired.

---

## Move later

| Current | Target |
|---------|--------|
| `PioneerRosterManager` | `Features/Simulation/` service + adapter |
| `BuildingOperationRegistry` static | scene-bound or service with events |
| `PetManager` | fold into Echo/trio (`Features/Simulation/`) |
| Legacy Pi wallet fields | strip on economy UI touch (GDD AC-only) |

---

## Risk

| Risk | Detail |
|------|--------|
| **Roster hub coupling** | 6+ domains read `PioneerRosterManager.Instance` |
| **`EnsureExists` host roulette** | attaches to SimpleGameManager, UIManager, or player |
| **Static building registry** | process-global; not scene-bound |
| **String pioneer names as worker IDs** | fragile for save/simulation |
| **Pet loop separate** | GDD says fold into Echo/trio — not done |
| **Building logic in UI** | `BuildingControlPanelUI` 5 partials own craft/roster/health |

---

## WorldState fields

| Field | Source |
|-------|--------|
| `ColonyStage` | roster size, facilities, milestones |
| `AetherCredits` | already `ColonySnapshot` (GameState) |
| `EchoPopulation` | roster echo chronicle |
| `ExpeditionTrioIds` | `CrewSnapshot` |
| Building queue state | `BuildingSnapshot` (GameState) |
| `HumanExpansion` | outpost count (future) |

**Providers:** `ColonyEvolutionWorldStateProvider`, existing colony/building GameState adapters.

---

## Dependencies

**Inbound:** Quest rewards, echoes, building UI, companions, save system, GameState.  
**Outbound:** `NamedPioneerCatalog`, companions, building registry, UI panels, AC grants.

**SimulationDirector (planned):** off-screen colony tick, job assignment, incidents.
