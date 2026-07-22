# Phase D — Dark Matter Stack Validation

**Status:** Not complete — checklist reserved for after World Engine Runs 1–2  
**Authority:** GDD 5.0 Appendix B5 · [World_Engine_Disk_Status.md](World_Engine_Disk_Status.md) · TDB  
**Disk audit:** July 22, 2026 — Features GameState / WorldState / Directors / Validation / Communications Runtime **absent**

This document is the **target validation** for architecture phases A–D. It does **not** claim those phases are implemented on disk.

---

## Scope to validate (when runtime exists)

| Phase | Deliverable | Validation |
|-------|-------------|------------|
| A0 | HLA v1.0 ratified | Doc exists — **done** |
| A | TDB + audits + folder mapping | Doc cross-links — **done** |
| B | WorldState API + Communications bridge | EditMode + F9 — **blocked (no C#)** |
| C | Directors stubs + command intents | EditMode + F10 — **blocked (no C#)** |
| D | This checklist + stack tests + GDD B5 | EditMode `Validation.Tests` — **blocked (no C#)** |

---

## Automated checklist (EditMode) — create with Run 1–2

Run all tests under **Window → General → Test Runner → EditMode** once assemblies exist.

- [ ] `DarkMatterStackValidationTests.BootstrapOrder_MatchesTdbLockedSequence`
- [ ] `DarkMatterStackValidationTests.SmokeKeys_AreUniqueStrings`
- [ ] `DarkMatterStackValidationTests.WorldStateToCommunicationsContext_MapsEvolutionaryFields`
- [ ] `DarkMatterStackValidationTests.DirectorOrchestrator_ReadsWorldStateWithoutGameplayManagers`
- [ ] `DarkMatterStackValidationTests.GameStateSnapshot_EmbeddedInWorldState_HasSameReferenceWhenEmpty`
- [ ] `WorldStateServiceTests` (all)
- [ ] `DirectorOrchestratorTests` (all)
- [ ] `SimulationDirectorServiceTests` + `ExperienceDirectorServiceTests` (all)
- [ ] `ContextBuilderTests` (all, incl. WorldState path)
- [ ] `TransmissionQueueTests` + Communications dialogue tests

**Pass criteria:** zero failures in the Features test assemblies above (assemblies not created yet).

---

## Manual checklist (Play Mode) — after Runtime lands

Enter **Pioneer** scene with companion systems bootstrapped (`SimpleGameManager` → `CompanionSystemsBootstrap` + Features bootstraps).

- [ ] **F5** — radio transmission enqueues (Communications)
- [ ] **F7** — context log includes world fields
- [ ] **F9** — `[WorldState]` one-line summary
- [ ] **F10** — `[Directors]` eval smoke
- [ ] **Shift+F5** — vista banner (optional presentation adapter)

---

## Known gaps (current disk)

- No `Features/GameState`, `WorldState`, `Directors`, `Validation`, `Experience`
- No Communications Runtime / Radio HUD / ContextBuilder C#
- WorldState not in `GameSaveData`; no world seed field
- `EnvironmentalCrisisHudMode` exists without WeatherDirector scheduler
- Echo chronicle + `EchoGenerator` exist in legacy Scripts (reuse in Run 3)
- LLM / Phase 9+ and Phase 8.1 voice — **deferred**

---

## Next engineering priorities (GDD B4)

0. Doc honesty — **this pass**  
1. World Engine spine (GameState → WorldState → Directors → Validation)  
2. Internal Communications (rule-based)  
3. Persistent generated world (seed + Generation + save fields)  
4. Living-world slice (Weather / Simulation directors)  
5. Command Center aggregate sim (B4 #5)

See [Framework_Folder_Mapping.md](Framework_Folder_Mapping.md) and [World_Engine_Disk_Status.md](World_Engine_Disk_Status.md).
