# Framework Folder Mapping

**Parent authority:** [Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md](Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md) (HLA v1.0, frozen)  
**Disk truth:** [World_Engine_Disk_Status.md](World_Engine_Disk_Status.md) (July 22, 2026)

Maps HLA WoOS pillars and layers to **current** repo paths and **planned** `Features/` modules.

**Rule:** No physical mass migration. New work uses `Features/`. Legacy `Scripts/` moves only when a domain is actively refactored.  
**Status rule:** “Shipped” means `.cs` present on disk — not “mentioned in a design doc.”

---

## WoOS layer → location

| WoOS layer | HLA § | Current (legacy) | Planned Features module | Status (disk) |
|------------|-------|------------------|-------------------------|---------------|
| **World** | §2.3 | `Scripts/Map/`, `Scripts/Survival/Exposure/` | `Features/World/` | Partial (legacy Exposure) |
| **Simulation** | §2.4 | `Scripts/Pioneers/`, `Scripts/Building/`, `FacilityTaskRunner` | `Features/Simulation/` | Partial (legacy) |
| **Intelligence** | §2.6, §8 | `Scripts/Quests/` (exec) | `Features/Directors/`, `Features/Kairos/` | **Not started** (no Directors C#) |
| **Experience** | §2.7 | — | `Features/Experience/` | **Not started** |
| **Presentation** | §2.8 | `Scripts/UI/`, `Scripts/Audio/`; Communications **docs only** | `Features/Communications/` Runtime | Partial UI; Comms Runtime **absent** |
| **Gameplay** | §2.2 | `Scripts/{Player,Combat,Interaction,Inventory,Crafting,Vehicles}/` | migrate when touched | Shipped (legacy) |
| **Core** | §2.1 | `Scripts/Core/`, `Scripts/Managers/` | `Features/Core/` (future) | Shipped (legacy) |
| **Generation** | §2.10 | `EchoGenerator`, spawners | `Features/Generation/` | Partial (legacy EchoGenerator only) |
| **Editor** | §2.11 | `Assets/_Project/Editor/` | `Features/*/Editor/` per module | Strong |
| **GameState (read model)** | §5 | `Features/GameState/` | extend only | **Shipped (Run 1)** |
| **WorldState (read model)** | §7 | `Features/WorldState/` | extend only | **Shipped (Run 1)** |
| **Directors (Intelligence)** | §8 | `Features/Directors/` | orchestrator + stubs | **Shipped stubs (Run 1)** |
| **Validation (stack)** | D | `Features/Validation/` | cross-stack tests | **Shipped (Run 1)** |

---

## Presentation layer breakdown

| Component | Path | Namespace | Disk |
|-----------|------|-----------|------|
| Communications docs | `Features/Communications/Documentation/` | — | Present |
| Communications Data/Audio placeholders | `Features/Communications/{Data,Audio}/README.md` | — | Present (no Runtime C#) |
| Communications runtime (planned) | `Features/Communications/Runtime/` | `Project.Features.Communications` | **Absent** |
| Communications UI (planned) | `Features/Communications/UI/` | `Project.Features.Communications.UI` | **Absent** |
| HUD / Journal / Menus | `Scripts/UI/` | `Project.UI` | Present |
| Game audio | `Scripts/Audio/` | `Project.Audio` | Present |

---

## Intelligence layer breakdown (planned)

| Component | Planned path | Notes |
|-----------|--------------|-------|
| DirectorOrchestrator | `Features/Directors/Runtime/` | Eval order HLA §8.2 — Run 1 |
| StoryDirector adapter | `Features/Directors/Adapters/` | Reads WorldState; commands `IQuestCommandService` |
| ExperienceDirector | `Features/Directors/Runtime/` | Silence, densities — after spine |
| Kairos knowledge | `Features/Kairos/` | Cores, codex — later |
| Comms delivery | `Features/Communications/` | Presentation only — Run 2 |

---

## Legacy Scripts domain map

| Folder | Count (approx) | HLA owner | Migration priority |
|--------|----------------|-----------|-------------------|
| `Scripts/UI/` | 132 | Presentation | Low (works; huge) |
| `Scripts/AI/` | 44 | Gameplay + World | Medium |
| `Scripts/Companions/` | 36 | Gameplay + Simulation | Medium |
| `Scripts/Player/` | 26 | Gameplay | Low |
| `Scripts/Pioneers/` | 24 | Simulation | High (WorldState / Generation) |
| `Scripts/Interaction/` | 22 | Gameplay | Low |
| `Scripts/Survival/` | 20 | World + Gameplay | Medium (Exposure feeds WorldState) |
| `Scripts/Core/` | 15 | Core | Medium (save + world seed) |
| `Scripts/Progression/` | 15 | Gameplay | Low |
| `Scripts/Quests/` | 13 | Story + Intelligence | High |
| `Scripts/Achievements/` | 10 | Core/Systems | Low |
| `Scripts/Crafting/` | 8 | Gameplay | Low |
| `Scripts/Pet/` | 8 | Simulation (fold to Echo) | Medium |
| `Scripts/Building/` | 6 | Simulation | High |
| `Scripts/Echoes/` | 4 | Simulation + Story | Medium |
| `Scripts/Combat/` | 17 | Gameplay | Low |
| `Scripts/Vehicles/` | 16 | Gameplay | Low |
| `Scripts/Managers/` | 2 | Core | High (bootstrap) |
| `Scripts/Inventory/` | 4 | Gameplay | Low |
| `Scripts/Map/` | 4 | World | Medium |

---

## Features module standard layout

Every new module:

```
Assets/_Project/Features/<Name>/
  Runtime/          → interfaces, services, snapshots
  UI/               → optional presenters
  Data/             → ScriptableObjects
  Audio/            → optional
  Adapters/         → Assembly-CSharp bridges to legacy managers
  Editor/           → optional
  Tests/            → EditMode asmdef
  Documentation/    → README linking HLA §
```

---

## Namespace convention

| Location | Namespace |
|----------|-----------|
| Feature runtime | `Project.Features.<Name>` |
| Feature adapters | `Project.Features.<Name>.Adapters` |
| Legacy Scripts | `Project.<Domain>` (unchanged) |

---

## Target vs current bootstrap (gap)

| HLA §6.3 step | On disk |
|---------------|---------|
| GameStateBootstrap | **Yes** (Run 1) |
| WorldStateBootstrap | **Yes** (Run 1) |
| DirectorsBootstrap | **Yes** (Run 1) |
| CommunicationsBootstrap | **No** (Run 2) |
| ExperienceBootstrap | **No** |
| Legacy CompanionSystemsBootstrap | **Yes** (wires Features spine first, then companions/pet/exposure/facilities) |

See TDB Bootstrap Registry and GDD B4 Runs 1–2.
