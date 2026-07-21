# Framework Folder Mapping

**Parent authority:** [Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md](Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md) (HLA v1.0, frozen)

Maps HLA WoOS pillars and layers to **current** repo paths and **planned** `Features/` modules.

**Rule:** No physical mass migration. New work uses `Features/`. Legacy `Scripts/` moves only when a domain is actively refactored.

---

## WoOS layer → location

| WoOS layer | HLA § | Current (legacy) | Planned Features module | Status |
|------------|-------|------------------|-------------------------|--------|
| **World** | §2.3 | `Scripts/Map/`, `Scripts/Survival/Exposure/` | `Features/World/` | Partial |
| **Simulation** | §2.4 | `Scripts/Pioneers/`, `Scripts/Building/`, `FacilityTaskRunner` | `Features/Simulation/` | Partial |
| **Intelligence** | §2.6, §8 | `Scripts/Quests/` (exec), `Features/Directors/` stubs | `Features/Directors/`, `Features/Aether9/` | Partial (Phase C stubs) |
| **Experience** | §2.7 | `ExperienceWorldStateProvider` heuristic | `Features/Experience/` | Partial (stub) |
| **Presentation** | §2.8 | `Features/Communications/`, `Scripts/UI/`, `Scripts/Audio/` | unify docs; keep paths | Partial |
| **Gameplay** | §2.2 | `Scripts/{Player,Combat,Interaction,Inventory,Crafting,Vehicles}/` | migrate when touched | Shipped |
| **Core** | §2.1 | `Scripts/Core/`, `Scripts/Managers/` | `Features/Core/` (future) | Shipped |
| **Generation** | §2.10 | scattered (`EchoGenerator`, spawners) | `Features/Generation/` | Not started |
| **Editor** | §2.11 | `Assets/_Project/Editor/` (106 `.cs`) | `Features/*/Editor/` per module | Strong |
| **GameState (read model)** | §5 | `Features/GameState/` | extend only | **Shipped** |
| **WorldState (read model)** | §7 | `Features/WorldState/` | extend only | **Shipped (Phase B)** |
| **Directors (Intelligence)** | §8 | `Features/Directors/` | orchestrator + stubs | **Shipped (Phase C)** |
| **Validation (stack)** | D | `Features/Validation/` | cross-stack tests | **Shipped (Phase D)** |

---

## Presentation layer breakdown

| Component | Path | Namespace | Assembly |
|-----------|------|-----------|----------|
| Communications runtime | `Features/Communications/Runtime/` | `Project.Features.Communications` | `Project.Features.Communications` |
| Communications UI | `Features/Communications/UI/` | `Project.Features.Communications.UI` | Assembly-CSharp |
| Communications Audio | `Features/Communications/Audio/` | `Project.Features.Communications.Audio` | `Project.Features.Communications.Audio` |
| Communications adapters | `Features/Communications/Adapters/` | `Project.Features.Communications.Adapters` | Assembly-CSharp |
| HUD / Journal / Menus | `Scripts/UI/` | `Project.UI` | Assembly-CSharp |
| Game audio | `Scripts/Audio/` | `Project.Audio` | Assembly-CSharp |

---

## Intelligence layer breakdown (planned)

| Component | Planned path | Notes |
|-----------|--------------|-------|
| DirectorOrchestrator | `Features/Directors/Runtime/` | Eval order HLA §8.2 |
| StoryDirector adapter | `Features/Directors/Adapters/` | Reads WorldState; commands `IQuestCommandService` |
| ExperienceDirector | `Features/Directors/Runtime/` | Silence, densities, player emotional estimates |
| Aether-9 knowledge | `Features/Aether9/` | Cores, codex, unlocks — not Presentation |
| Comms delivery | `Features/Communications/` | Presentation only |

---

## Legacy Scripts domain map

| Folder | Count (approx) | HLA owner | Migration priority |
|--------|----------------|-----------|-------------------|
| `Scripts/UI/` | 134 | Presentation | Low (works; huge) |
| `Scripts/AI/` | 44 | Gameplay + World | Medium |
| `Scripts/Companions/` | 36 | Gameplay + Simulation | Medium |
| `Scripts/Player/` | 26 | Gameplay | Low |
| `Scripts/Pioneers/` | 24 | Simulation | High (WorldState) |
| `Scripts/Interaction/` | 22 | Gameplay | Low |
| `Scripts/Survival/` | 20 | World + Gameplay | Medium |
| `Scripts/Core/` | 15 | Core | Medium (save split) |
| `Scripts/Progression/` | 15 | Gameplay | Low |
| `Scripts/Quests/` | 13 | Story (data) + Intelligence (exec) | High |
| `Scripts/Achievements/` | 10 | Core/Systems | Low |
| `Scripts/Crafting/` | 8 | Gameplay | Low |
| `Scripts/Pet/` | 8 | Simulation (fold to Echo) | Medium |
| `Scripts/Building/` | 6 | Simulation | High |
| `Scripts/Echoes/` | 4 | Simulation + Story | Medium |
| `Scripts/Combat/` | 17 | Gameplay | Low |
| `Scripts/Vehicles/` | 16 | Gameplay | Low |
| `Scripts/Managers/` | 2 | Core | High |
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

| HLA §6.3 step | Implemented |
|---------------|-------------|
| GameStateBootstrap | Yes |
| WorldStateBootstrap | **Yes** |
| CommunicationsBootstrap | Yes |
| DirectorsBootstrap | **No** |
| ExperienceBootstrap | **No** |

See TDB §6 Bootstrap Registry.
