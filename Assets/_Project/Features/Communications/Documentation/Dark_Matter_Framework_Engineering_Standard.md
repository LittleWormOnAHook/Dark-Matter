# Dark Matter Framework Engineering Standard

**Contract for every Dark Matter: Genesis coding session.**

Every future prompt to an AI coding agent should begin with:

> Follow the Dark Matter Framework Engineering Standard.

Architecture (constitutional): [Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md](../../../Documentation/Architecture/Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md) — **HLA v1.0, frozen**  
Technical design: [Dark_Matter_Technical_Design_Bible.md](../../../Documentation/Architecture/Dark_Matter_Technical_Design_Bible.md) — **TDB v1.0**  
Roadmap: [Dark_Matter_Communication_Framework.md](Dark_Matter_Communication_Framework.md)  
GDD authority: `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt`

---

## 1. Product constraints

- Ship targets: **PC first**, then consoles (PS5 / Xbox). No mobile-as-target. No WebGL.
- Economy: **Aether Credits (AC) only**. Do not reintroduce Pi marketplace loops.
- Prefer existing systems under `Assets/_Project/Scripts/` when extending shipped gameplay; new cross-cutting features use the Features layout below.

## 2. Folder structure

### Legacy gameplay (existing)

```
Assets/_Project/Scripts/<Domain>/     → namespace Project.<Domain>
```

Do not mass-migrate legacy scripts in drive-by work.

### Feature modules (new work)

```
Assets/_Project/Features/<FeatureName>/
  Runtime/
  UI/
  Data/
  Audio/
  Editor/
  Tests/
  Documentation/
```

Communications example: `Assets/_Project/Features/Communications/`.

Game State API (Phase 1+): `Assets/_Project/Features/GameState/`.

## 3. Namespaces

| Location | Namespace |
|----------|-----------|
| Feature runtime | `Project.Features.<Feature>` |
| Feature UI | `Project.Features.<Feature>.UI` |
| Feature data types | `Project.Features.<Feature>.Data` |
| Feature audio | `Project.Features.<Feature>.Audio` |
| Feature editor | `Project.Features.<Feature>.Editor` |
| Legacy | `Project.<Domain>` (unchanged) |

## 4. Naming conventions

- Types / public members: `PascalCase`
- Private fields: `camelCase` or `_camelCase` (match nearest file in the feature)
- Interfaces: `I` prefix (`ICommunicationsService`)
- Immutable state DTOs: `*Snapshot` (`InventorySnapshot`)
- ScriptableObject assets: clear CreateAssetMenu paths under Dark Matter: Genesis / feature menus
- Prefixed logs: `[Communications]`, `[GameState]`, etc.

## 5. Script length

- Target **150–300 lines** per file where practical.
- Split large types into partials or collaborator classes (queue, audio, UI presenter).
- No new god MonoBehaviours that own data + UI + AI + persistence.

## 6. SOLID & modularity

- **S**ingle responsibility per type.
- **O**pen for extension via interfaces / ScriptableObjects; closed for drive-by edits to unrelated systems.
- **L**iskov: providers and conversation backends must be swappable without breaking callers.
- **I**nterface segregation: small focused interfaces (`ICommunicationsService`, `IGameStateService`).
- **D**ependency inversion: high-level features depend on abstractions, not concrete managers.

## 7. Data design

- **ScriptableObject-first** for authored content (crew, transmission templates, audio profiles).
- Runtime state lives in services / components, not static mutable globals (except intentional bootstrap singletons).
- Prefer immutable snapshots for cross-feature reads.

## 8. Coupling rules (critical)

- **No manager-to-manager** hard dependencies for new features.
- Features communicate via:
  - service interfaces
  - events / callbacks
  - read-only snapshots
- **AI and Communications must never** call `InventorySystem`, `PioneerRosterManager`, `BuildingOperationRegistry`, `QuestManager`, weather/power managers, etc. directly.
- They read **only** `IGameStateService` / `IWorldStateService` / snapshots / context packs built from snapshots.
- **Directors (Intelligence)** read WorldState; write via command/intent interfaces only (TDB §9).

## 9. Game State & World State API contract

- `GameStateService.GetSnapshot()` — momentary gameplay capture.
- `WorldStateService.GetSnapshot()` — evolutionary capture embedding GameState (Phase B).
- Domain adapters implement `IGameStateProvider` / `IWorldStateProvider` and map legacy managers → snapshots.
- Persistence (`GameSaveData` / `GameSaveSystem`) stays in `Project.Core`; snapshots are **runtime read models** until an explicit save migration phase.

**Bootstrap order (CompanionSystems):** GameState → WorldState → Directors → Communications (`DarkMatterBootstrapOrder`).

## 10. Dependency injection & discovery

- Prefer serialized references and explicit bootstrap wiring.
- Cross-feature services may use a small locator/bootstrap; avoid `FindAnyObjectByType` in hot paths (`Update` / per-frame).
- Do not introduce heavy DI frameworks without an explicit product decision.

## 11. Events over polling

- Prefer events / dirty flags / snapshot hash-diff (see `ExposureStatusService`) over busy `Update` loops.
- Transmission playback is queue-driven, not “play dialogue anywhere.”

## 12. Documentation

- Public APIs: XML `<summary>` on interfaces, public services, and snapshot types.
- Feature Documentation folder holds roadmap and standards (this file).

## 13. Testing

- EditMode tests for: transmission queue ordering, priority preemption rules, snapshot builders, orchestrator eval order.
- Tests live under `Features/<Feature>/Tests/` with a dedicated asmdef.
- Cross-stack validation: `Features/Validation/Tests/` (Phase D).
- Manual play-mode smoke: F5–F8 Communications · F9 WorldState · F10 Directors (`DarkMatterSmokeKeys`).

## 14. Logging & errors

- Prefix logs with `[FeatureName]`.
- Never swallow exceptions silently.
- Fail soft for optional content (missing portrait) with a warning; fail hard for broken contracts (null required service in bootstrap).

## 15. UI

- Colors: `SurvivalPioneerUiPalette` and `ShiftUiTheme` only for new UI.
- Radio / subtitle chrome follows Dark Navy panels, Slate borders, Warm Off-White text, Rich Fuchsia accents (see `.cursor/rules/survival-pioneer-ui-palette.mdc`).

## 16. Assembly definitions

- New Features use `.asmdef` for isolation.
- Legacy `Assets/_Project/Scripts/` may remain on default `Assembly-CSharp` until intentionally modularized.
- Communications must **not** reference Inventory / Building / Quest assemblies directly; GameState / WorldState adapters own those references.
- Directors must **not** reference Communications runtime directly; use `ICommunicationsIntentService` (adapter maps to Presentation).

## 17. What not to do

- Do not invent unrelated systems unless requested.
- Do not connect LLM / cloud APIs before Phases 0–7 foundations exist.
- Do not put communications logic in quest dialog UI or toast systems.
- Do not commit vendor asset dumps or false `.meta` renames with feature work.

## 18. Architecture authority (HLA v1.0)

- Highest engineering authority: `Assets/_Project/Documentation/Architecture/Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md` (**frozen** — change only via versioned HLA revisions).
- WoOS stack: World → Simulation → Intelligence → Experience → Presentation → Player.
- Communications is **Presentation**, not Intelligence. Directors + Aether-9 live in the **Intelligence** layer.
- New cross-cutting modules: `Features/<Name>/` per HLA §13. Do not contradict HLA without an explicit version bump.
