# Dark Matter Technical Design Bible

**Version:** 1.0 (implements HLA v1.0)  
**Parent authority:** [Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md](Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md)  
**Coding contract:** [Dark_Matter_Framework_Engineering_Standard.md](../../Features/Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md)  
**Folder map:** [Framework_Folder_Mapping.md](Framework_Folder_Mapping.md)

This document explains **HOW** systems work. The HLA explains **WHY**.

---

## Table of contents

1. [Product split](#1-product-split) — HLA §1  
2. [WoOS runtime stack](#2-woos-runtime-stack) — HLA §1.2  
3. [Read models: GameState & WorldState](#3-read-models-gamestate--worldstate) — HLA §5, §7  
4. [Intelligence layer contracts](#4-intelligence-layer-contracts) — HLA §2.6, §8  
5. [Experience layer contracts](#5-experience-layer-contracts) — HLA §2.7  
6. [Presentation layer contracts](#6-presentation-layer-contracts) — HLA §2.8  
7. [Bootstrap registry](#7-bootstrap-registry) — HLA §6  
8. [Persistence boundary](#8-persistence-boundary) — HLA §5  
9. [Command & intent interfaces](#9-command--intent-interfaces) — HLA §8  
10. [Assembly boundaries](#10-assembly-boundaries)  
11. [Legacy manager inventory](#11-legacy-manager-inventory)  
12. [Event patterns](#12-event-patterns)  
13. [Design pillars feature gate](#13-design-pillars-feature-gate) — HLA §15  
14. [Planned Features modules](#14-planned-features-modules)  
15. [AI agent implementation checklist](#15-ai-agent-implementation-checklist)

---

## 1. Product split

| Layer | Owns | Does not own |
|-------|------|--------------|
| **Dark Matter** | Framework pillars, services, directors, snapshots, editor patterns | Io lore, AC economy rules, GDD canon |
| **Dark Matter: Genesis** | ScriptableObject content, game adapters, Shift UI theme assets | Framework interface shapes |

Game-specific strings (`Aether-9`, `Aether Credits`, `Neural Echo`) appear in **Data** and **Adapters**, not in framework Runtime interface names where avoidable.

---

## 2. WoOS runtime stack

```
World → Simulation → Intelligence → Experience → Presentation → Player
```

| Layer | Runtime today | Contract entry point |
|-------|---------------|----------------------|
| World | Exposure, Map, spawners | `WeatherGameStateProvider`, future `IWorldService` |
| Simulation | Roster, Building registry, FacilityTaskRunner | Future `ISimulationService` |
| Intelligence | *(planned)* | `IDirectorOrchestrator`, `IAether9Service` |
| Experience | *(planned)* | `IExperienceService`, `ExperienceDirector` |
| Presentation | Communications, UI, Audio | `ICommunicationsService`, HUD presenters |
| Gameplay | Player, Combat, Inventory, … | MonoBehaviours on player/entities |
| Read models | GameState shipped; WorldState planned | `IGameStateService`, `IWorldStateService` |

**Data flow (read path):**

```
Gameplay mutation → Legacy managers → IGameStateProvider adapters
  → GameStateService.GetSnapshot()
  → IWorldStateProvider adapters (planned)
  → WorldStateService.GetSnapshot()
  → Directors + ExperienceDirector
  → Presentation intents
```

---

## 3. Read models: GameState & WorldState

### 3.1 GameState (shipped)

**Location:** `Assets/_Project/Features/GameState/`  
**HLA:** §5.2

#### Interfaces

```csharp
// Project.Features.GameState
public interface IGameStateService
{
    GameStateSnapshot GetSnapshot();
}

public interface IGameStateProvider
{
    string DomainId { get; }
    void Contribute(GameStateSnapshotBuilder builder);
}
```

#### Service

```csharp
public sealed class GameStateService : IGameStateService
{
    public static GameStateService Instance { get; }
    public static void SetInstance(GameStateService service);
    public void RegisterProvider(IGameStateProvider provider);
    public void UnregisterProvider(IGameStateProvider provider);
    public GameStateSnapshot GetSnapshot();
}
```

#### Snapshot root

```csharp
public sealed class GameStateSnapshot
{
    public long CapturedAtUtcTicks { get; }
    public PlayerSnapshot Player { get; }
    public InventorySnapshot Inventory { get; }
    public MissionSnapshot Mission { get; }
    public WeatherSnapshot Weather { get; }
    public PowerSnapshot Power { get; }
    public ColonySnapshot Colony { get; }
    public ResearchSnapshot Research { get; }      // placeholder empty
    public CrewSnapshot Crew { get; }
    public BuildingSnapshot Buildings { get; }
}
```

#### Registered providers (Assembly-CSharp adapters)

| DomainId | Provider | Legacy source |
|----------|----------|---------------|
| `player` | `PlayerGameStateProvider` | `PlayerLocator`, `SurvivalStats`, `GameSession` |
| `inventory` | `InventoryGameStateProvider` | `InventorySystem` |
| `mission` | `MissionGameStateProvider` | `QuestManager` |
| `weather` | `WeatherGameStateProvider` | `ExposureStatusService` |
| `power` | `PowerGameStateProvider` | `FindObjectsByType<PowerGenerator>()` |
| `colony` | `ColonyGameStateProvider` | `PioneerRosterManager` |
| `research` | `ResearchGameStateProvider` | *(empty placeholder)* |
| `crew` | `CrewGameStateProvider` | `PioneerRosterManager.SkilledPioneers` |
| `buildings` | `BuildingGameStateProvider` | `BuildingOperationRegistry` |

**Rules:**
- Snapshots are immutable DTOs — no methods that mutate gameplay.
- Communications and future AI **must** use `IGameStateService` only.
- Adapters may call managers; consumers may not.

### 3.2 WorldState (planned — Phase B)

**Location:** `Assets/_Project/Features/WorldState/` (not yet created)  
**HLA:** §7

#### Planned interfaces

```csharp
public interface IWorldStateService
{
    WorldStateSnapshot GetSnapshot();
}

public interface IWorldStateProvider
{
    string DomainId { get; }
    void Contribute(WorldStateSnapshotBuilder builder);
}
```

#### Planned snapshot shape

```csharp
public sealed class WorldStateSnapshot
{
    public long CapturedAtUtcTicks { get; }
    public GameStateSnapshot Game { get; }              // embedded momentary truth
    public StoryProgressSnapshot Story { get; }
    public PlanetEvolutionSnapshot Planet { get; }
    public ColonyEvolutionSnapshot Colony { get; }
    public Aether9Snapshot Aether9 { get; }
    public SimulationSnapshot Simulation { get; }
    public ThreatSnapshot Threat { get; }
    public ExperienceSnapshot Experience { get; }
    public SessionSnapshot Session { get; }             // Core domain
}
```

#### Planned providers (Phase B minimal)

| DomainId | Provider | Source |
|----------|----------|--------|
| `story` | `StoryWorldStateProvider` | `QuestManager` progress |
| `colony` | `ColonyEvolutionWorldStateProvider` | `PioneerRosterManager` aggregates |
| `aether9` | `Aether9WorldStateProvider` | `CommsQueryService.Aether9AdvisoryUnlocked` + future quest |
| `environment` | `EnvironmentWorldStateProvider` | Exposure / storm debug path |
| `session` | `SessionWorldStateProvider` | `GameSession`, save slot metadata |
| `experience` | `ExperienceWorldStateProvider` | Session telemetry service (stub) |

**Persistence:** WorldState fields map into `GameSaveData` over time — WorldState is not a second save file.

---

## 4. Intelligence layer contracts

**HLA:** §2.6, §8  
**Location (planned):** `Features/Directors/`, `Features/Aether9/`

### 4.1 Director orchestrator

```csharp
public interface IDirectorOrchestrator
{
    void EvaluateAll();                    // full pass
    void Evaluate(DirectorTrigger trigger); // event-driven partial pass
}

public enum DirectorTrigger
{
    SessionStarted,
    QuestCompleted,
    RosterChanged,
    SimulationTick,
    StormPhaseChanged,
    MemoryCoreRestored,
    ManualDebug
}
```

### 4.2 Evaluation order (locked — HLA §8.2)

```
StoryDirector
  → SimulationDirector
  → MissionDirector
  → WeatherDirector
  → EconomyDirector
  → ExperienceDirector
  → EventDirector
  → AIDirector (optional)
```

Aether-9 participates inside Intelligence eval — consulted by Story and Experience; emits knowledge/hint intents to Presentation.

### 4.3 Director interfaces (planned stubs)

```csharp
public interface IStoryDirector
{
    void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger);
}

public interface ISimulationDirector
{
    void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger);
}

public interface IMissionDirector
{
    void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger);
}

public interface IWeatherDirector
{
    void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger);
}

public interface IEconomyDirector
{
    void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger);
}

public interface IExperienceDirector
{
    void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger);
}

public interface IEventDirector
{
    void Evaluate(WorldStateSnapshot world, DirectorTrigger trigger);
}
```

Directors **read** WorldState; **write** only via command/intent interfaces (§9).

### 4.4 Aether-9 service (planned)

```csharp
public interface IAether9KnowledgeService
{
    int MemoryCoresRestored { get; }
    bool IsAdvisoryUnlocked { get; }
    bool TryGetHint(string contextTag, out Aether9Hint hint);
    IReadOnlyList<string> GetUnlockedBlueprintIds();
}
```

Aether-9 is Intelligence, not Presentation. Radio delivery uses `ICommunicationsIntentService`.

---

## 5. Experience layer contracts

**HLA:** §2.7  
**Location (planned):** `Features/Experience/`, `ExperienceDirector` in `Features/Directors/`

### 5.1 Experience snapshot (planned)

```csharp
public sealed class ExperienceSnapshot
{
    // Session telemetry
    public float MinutesSinceLastRelief { get; }
    public float MinutesSinceLastDiscovery { get; }
    public float MinutesSinceLastTransmission { get; }

    // Density meters (0..1)
    public float CommunicationDensity { get; }
    public float RadioDensity { get; }
    public float AmbientDensity { get; }
    public float CognitiveLoad { get; }

    // Estimated player emotional proxies (0..1) — not NPC emotions
    public float EstimatedStress { get; }
    public float EstimatedCuriosity { get; }
    public float EstimatedWonder { get; }
    public float EstimatedConfidence { get; }
    public float EstimatedIsolation { get; }
    public float EstimatedUrgency { get; }
    public float EstimatedFatigue { get; }
    public float EstimatedSatisfaction { get; }
    public float EstimatedFlow { get; }

    // Silence scheduling
    public bool SilenceWindowActive { get; }
    public float ScheduledSilenceRemainingSeconds { get; }
}
```

### 5.2 Experience intents (planned)

```csharp
public enum ExperienceIntentKind
{
    ReduceDangerBudget,
    IncreaseDangerBudget,
    ScheduleSilence,
    BreakSilence,
    RequestVista,
    RequestSoftRadio,
    RequestMusicShift,
    DeferSimulationIncident,
    AmplifySimulationIncident
}

public readonly struct ExperienceIntent
{
    public ExperienceIntentKind Kind { get; }
    public float Magnitude { get; }          // 0..1
    public float DurationSeconds { get; }
    public string DebugReason { get; }
}
```

ExperienceDirector emits intents; WeatherDirector, EventDirector, SimulationDirector, and Presentation consume them. **Experience does not override Story chapter gates.**

### 5.3 Telemetry inputs (planned)

| Signal | Source |
|--------|--------|
| Combat duration | GameState + session clock |
| Vitals stress | `PlayerSnapshot` ratios |
| Storm / night | `WeatherSnapshot` |
| Time since transmission | `CommunicationsManager` events |
| Discovery gap | Map/POI events (future) |

---

## 6. Presentation layer contracts

**HLA:** §2.8  
Communications is **Presentation**, not Intelligence.

### 6.1 Communications service (shipped)

```csharp
public interface ICommunicationsService
{
    bool IsTransmitting { get; }
    void Enqueue(ITransmission transmission);
    void StartTransmission();
    void EndTransmission();
}
```

**Implementation:** `CommunicationsManager` — `Features/Communications/Runtime/`

**Queue:** `TransmissionQueue` — priority insert; Emergency preempts non-Emergency.

**Events:** `TransmissionStarted`, `TransmissionEnded`, `QueueChanged`

### 6.2 Context pack (shipped)

```csharp
public static class ContextBuilder
{
    public static CommunicationsContextPack Build(IGameStateService service);
    public static CommunicationsContextPack Build(GameStateSnapshot snapshot);
}
```

**Phase B extension:** overload or enrich from `WorldStateSnapshot` (story chapter, storm, Aether-9, experience densities) — still no manager calls.

### 6.3 Query pipeline (shipped)

```
Alt+1..7 → CommsQueryService.Ask(CommsQueryKind)
  → ContextBuilder.Build(gameState)
  → DialogueGenerator.Generate(kind, pack, VoiceStyle)
  → ICommunicationsService.Enqueue(...)
```

**Aether-9 flag:** `CommsQueryService.Aether9AdvisoryUnlocked` — migrate to WorldState in Phase B.

### 6.4 Radio HUD (shipped)

**Type:** `RadioHudUI` — `Features/Communications/UI/` (Assembly-CSharp)  
**Factory:** `RadioHudUI.EnsureExists(host)`  
**Subscribes:** `CommunicationsManager.TransmissionStarted/Ended`  
**Palette:** `SurvivalPioneerUiPalette`

### 6.5 Audio pipeline (shipped)

**Assembly:** `Project.Features.Communications.Audio`

| Interface | Default impl |
|-----------|--------------|
| `IRadioVoiceSynthesizer` | `ProceduralRadioVoiceSynthesizer` |
| `IRadioSpeechRecognizer` | `StubRadioSpeechRecognizer` |

**Player:** `RadioTransmissionAudioPlayer` — binds to manager events  
**PTT:** `RadioPttController` — hold V / gamepad L3

---

## 7. Bootstrap registry

**HLA:** §6.3

### 7.1 Companion-systems boot order (July 2026 — Phases B–D shipped)

| Order | Component | Trigger | File |
|-------|-----------|---------|------|
| 6 | `GameStateBootstrap.EnsureExists` | CompanionSystemsBootstrap | `Features/GameState/Adapters/GameStateBootstrap.cs` |
| 6b | `WorldStateBootstrap.EnsureExists` | CompanionSystemsBootstrap | `Features/WorldState/Adapters/WorldStateBootstrap.cs` |
| 7b | `DirectorsBootstrap.EnsureExists` | CompanionSystemsBootstrap | `Features/Directors/Adapters/DirectorsBootstrap.cs` |
| 7 | `CommunicationsBootstrap.EnsureExists` | CompanionSystemsBootstrap | `Features/Communications/Adapters/CommunicationsBootstrap.cs` |
| 8 | Legacy companion/pet/building bridges | CompanionSystemsBootstrap | `Scripts/Managers/CompanionSystemsBootstrap.cs` |

**Locked sequence** (also in `DarkMatterBootstrapOrder.CompanionSystems`):

```csharp
GameStateBootstrap.EnsureExists(host);
WorldStateBootstrap.EnsureExists(host);
DirectorsBootstrap.EnsureExists(host);
CommunicationsBootstrap.EnsureExists(host);
```

Earlier session boot (unchanged): `GameSession` → `PlatformGraphicsBootstrap` → MainMenu → `SimpleGameManager` → step 6 above.

### 7.2 Planned additions

| Order | Component | Status |
|-------|-----------|--------|
| 7c | `ExperienceBootstrap.EnsureExists` | Future |
| — | `DirectorTrigger` gameplay event bus | Future |

### 7.3 Composition root

**Current:** `CompanionSystemsBootstrap.EnsureGameplaySystems(MonoBehaviour host)`  
**Called from:** `SimpleGameManager.Awake`

Do not add new `FindAnyObjectByType` bootstraps outside this chain without TDB update.

---

## 8. Persistence boundary

**HLA:** §5

| Concern | Owner | Type |
|---------|-------|------|
| Save file I/O | `GameSaveSystem` | `Scripts/Core/GameSaveSystem.cs` |
| Save DTO | `GameSaveData` | version 17, 5 slots |
| Runtime read | `GameStateService` | snapshots |
| Evolution read | `WorldStateService` | snapshots (Phase B) |
| Apply on load | `GameSaveSystem.ApplySaveData` | **writes managers directly today** |

**Refactor target (Phase B+):** `GameSaveSystem` becomes file coordinator; per-domain `ISaveContributor` mirrors `IGameStateProvider`.

**Rule:** Snapshots do not replace save format until an explicit migration phase.

---

## 9. Command & intent interfaces

Directors must not call `QuestManager`, `InventorySystem`, etc. directly.

### 9.1 Planned command services

```csharp
public interface IQuestCommandService
{
    bool TryActivateQuest(string questId);
    bool TryCompleteObjective(string questId, int objectiveIndex);
}

public interface ISimulationCommandService
{
    bool TryApplyIncident(SimulationIncident incident);
}

public interface ICommunicationsIntentService
{
    void EnqueueTransmission(ITransmission transmission);
    void RequestSilence(float durationSeconds);
}

public interface IWeatherCommandService
{
    void SetStormPhase(StormPhase phase);
}

public interface IWorldPresentationCommandService
{
    void RequestVista(string vistaTag);
}
```

Implementations live in **Adapters** (Assembly-CSharp) during migration.

---

## 10. Assembly boundaries

| Assembly | References | Notes |
|----------|------------|-------|
| `Project.Features.GameState` | none | Snapshots + service |
| `Project.Features.Communications` | GameState | No gameplay managers |
| `Project.Features.Communications.Audio` | Communications | Audio only |
| `Project.Features.Communications.Editor` | Communications | Editor only |
| `Project.Features.GameState.Tests` | GameState | EditMode |
| `Project.Features.Communications.Tests` | Comm + Audio + GameState + WorldState | EditMode |
| `Project.Features.WorldState` | GameState | Snapshots + service |
| `Project.Features.WorldState.Tests` | WorldState + GameState | EditMode |
| `Project.Features.Directors` | GameState + WorldState | Orchestrator + stubs |
| `Project.Features.Directors.Tests` | Directors + WorldState + GameState | EditMode |
| `Project.Features.Validation` | none | Smoke key + bootstrap constants |
| `Project.Features.Validation.Tests` | Validation + stack assemblies | EditMode cross-stack |
| `Assembly-CSharp` | all above + legacy | Adapters, UI, gameplay |

**Shipped asmdefs:** `Project.Features.WorldState`, `Project.Features.Directors`, `Project.Features.Validation`

**Rule:** New Features assemblies must not reference `Scripts/` domains directly — adapters in Assembly-CSharp only.

---

## 11. Legacy manager inventory

Top runtime singletons — **do not call from new Features code**.

| Manager | Path | Owns |
|---------|------|------|
| `SimpleGameManager` | `Scripts/Managers/SimpleGameManager.cs` | Session start, starting items, save wrappers |
| `PioneerRosterManager` | `Scripts/Pioneers/PioneerRosterManager.cs` | 25 roster, AC, expedition trio, echoes |
| `UIManager` | `Scripts/UI/UIManager.cs` | HUD, lazy spawns Quest/Achievement/Crafting |
| `QuestManager` | `Scripts/Quests/QuestManager.cs` | Quest progress |
| `CraftingManager` | `Scripts/Crafting/CraftingManager.cs` | Recipes, craft execution |
| `CommunicationsManager` | `Features/Communications/Runtime/` | Radio queue (Presentation) |
| `GameStateService` | `Features/GameState/Runtime/` | Snapshot aggregation |
| `PetManager` | `Scripts/Pet/PetManager.cs` | Legacy pet loop |
| `AchievementManager` | `Scripts/Achievements/AchievementManager.cs` | Achievements |

Static registries: `BuildingOperationRegistry`, `ItemRegistry`, `QuestRegistry`, `EchoSignalRegistry`, `PoolManager`.

---

## 12. Event patterns

No central event bus today.

| Pattern | Examples |
|---------|----------|
| Static C# events | `GameSession.GameStarted`, `EnemyKillEvents.EnemyKilled` |
| Instance `Action` | `QuestManager.OnQuestUpdated`, `CommunicationsManager.TransmissionStarted` |
| Snapshot read | `GameStateService.GetSnapshot()` |

**New Features code:** prefer events for "re-evaluate directors" + snapshots for reads.

**Planned:** `DirectorTrigger` events feed `IDirectorOrchestrator.Evaluate(trigger)`.

---

## 13. Design pillars feature gate

**HLA:** §15

Every proposed feature must strengthen **≥1** pillar:

Exploration · Discovery · Survival · Colony · Memory · Consequence · Mystery · Replayability · Emergence · **Meaningful Agency** · **Believability**

**PR checklist:**

1. Which pillar(s)?  
2. Which WoOS layer?  
3. Read path: GameState / WorldState / ExperienceSnapshot?  
4. Write path: command interface or adapter?  
5. If none → reject or redesign.

---

## 14. Planned Features modules

| Module | Phase | HLA | Key deliverables |
|--------|-------|-----|------------------|
| `WorldState` | B | §7 | Service, providers, bootstrap, tests — **shipped** |
| `Directors` | C | §8 | Orchestrator, director stubs, command adapters — **shipped (stubs)** |
| `Validation` | D | §15 | Stack tests, smoke registry, GDD B5 checklist — **shipped** |
| `Experience` | C+ | §2.7 | Telemetry, ExperienceSnapshot builder |
| `Aether9` | Later | §9 | Knowledge service, core tracking |
| `Simulation` | Later | §11 | Incident model, off-screen tick |
| `Generation` | Later | §10 | Seed + generator interfaces |
| `Story` | Later | §2.5 | Quest service wrapper |

---

## 15. AI agent implementation checklist

Before writing code:

1. Read HLA section for the pillar.  
2. Read this TDB section for interfaces and paths.  
3. Read Engineering Standard for folder/namespace rules.  
4. Identify WoOS layer (World / Simulation / Intelligence / Experience / Presentation).  
5. Use snapshot read path — never managers from Features assemblies.  
6. Use command/intent write path — never direct manager mutation from Directors.  
7. Add EditMode test or F-key smoke (match Communications pattern).  
8. Run Design Pillars gate (§13).  
9. Cite `HLA v1.0 §X` in PR / commit description.

**Smoke key convention:** F5–F8 Communications · F9 WorldState · F10 Directors eval · F11 Directors weather command.

**Phase D validation:** [Dark_Matter_Phase_D_Validation.md](Dark_Matter_Phase_D_Validation.md) · GDD Appendix B5.

---

## Subsystem audits

Detailed per-domain findings:

| Audit | Domain |
|-------|--------|
| [Audit_01_Core.md](Audits/Audit_01_Core.md) | Bootstrap, save, session |
| [Audit_02_Player.md](Audits/Audit_02_Player.md) | Player, Invector |
| [Audit_03_Combat.md](Audits/Audit_03_Combat.md) | Combat, AI |
| [Audit_04_World.md](Audits/Audit_04_World.md) | Map, exposure |
| [Audit_05_Colony.md](Audits/Audit_05_Colony.md) | Companions, building |
| [Audit_06_Story.md](Audits/Audit_06_Story.md) | Quests, echoes |
| [Audit_07_Systems.md](Audits/Audit_07_Systems.md) | UI, audio, progression |
| [Audit_08_EditorTools.md](Audits/Audit_08_EditorTools.md) | Editor tooling |

---

## Changelog

| Version | Date | Notes |
|---------|------|-------|
| 1.0 | July 2026 | Initial TDB implementing HLA v1.0. Documents shipped GameState + Communications; specifies planned WorldState, Directors, Experience. |
| 1.1 | July 2026 | Phase B–D shipped: WorldState, Directors stubs, Validation module, bootstrap + smoke keys, GDD B5. |

---

*Subordinate to HLA v1.0. Update only with HLA version alignment.*
