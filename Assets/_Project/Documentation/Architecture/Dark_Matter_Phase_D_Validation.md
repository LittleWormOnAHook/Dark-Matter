# Phase D — Dark Matter Stack Validation

**Status:** Complete (July 2026)  
**Authority:** GDD 5.0 Appendix B5 · TDB v1.0 §7 · §15

Validates architecture phases **A–D** without replacing subsystem audits (Audit_01–08).

---

## Scope validated

| Phase | Deliverable | Validation |
|-------|-------------|------------|
| A0 | HLA v1.0 ratified | Doc exists; Engineering Standard cites it |
| A | TDB + audits + folder mapping | Doc cross-links |
| B | WorldState API + Communications bridge | EditMode + F9 |
| C | Directors stubs + command intents | EditMode + F10 |
| D | This checklist + stack tests + GDD B5 | EditMode `Validation.Tests` |

---

## Automated checklist (EditMode)

Run all tests under **Window → General → Test Runner → EditMode**.

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

**Pass criteria:** zero failures in the five Features test assemblies above.

---

## Manual checklist (Play Mode)

Enter **Pioneer** scene with companion systems bootstrapped (`SimpleGameManager` → `CompanionSystemsBootstrap`).

- [ ] **F5** — radio transmission enqueues (Communications)
- [ ] **F7** — context log includes `chapter=` / colony / stress world fields
- [ ] **F9** — `[WorldState]` one-line summary
- [ ] **F10** — `[Directors] trigger=ManualDebug directors=7`
- [ ] **Shift+F5** (logical F13) — vista banner via presentation adapter
- [ ] Or **Tools → Dark Matter → Smoke** menu while in Play Mode

Optional: **F6/F8** audio/emergency smokes if audio pipeline enabled.
Extended slots **F14–F24**: Shift+F6–F12 and Ctrl+Shift+F5–F8 (see `DarkMatterSmokeKeys.GetBindingLabel`).

---

## Design pillars gate (TDB §13)

Dark Matter stack phases strengthen:

| Pillar | How |
|--------|-----|
| **Believability** | WorldState + Directors read models; world continues off-screen |
| **Meaningful Agency** | Command/intent write path (future gameplay wiring) |
| **Memory** | Story/Aether-9 fields in WorldState → Communications context |
| **Emergence** | Director orchestration order (stubs → logic later) |

---

## Known gaps (not Phase D failures)

- ~~`IWorldPresentationCommandService` vista adapter remains log/stub~~ **B4 #2 shipped:** `WorldVistaPresenterUI` + `WorldPresentationCommandServiceAdapter`
- WorldState not persisted in `GameSaveData`
- ~~Experience module (full telemetry) not shipped — ExperienceDirector stub only~~ **B4 #1 shipped:** `Features/Experience` telemetry + `ExperienceDirectorService`
- Aether-9 Intelligence service not a separate Features module yet
- ~~Simulation incidents do not yet append Echo chronicle entries~~ **B4 #2 shipped:** `SimulationDirectorService` + chronicle rows via `SimulationCommandServiceAdapter`

---

## Next engineering priorities (GDD B4)

1. ~~Experience telemetry module + richer director stub logic~~ **shipped (B4 #1)**
2. ~~Presentation vista adapter + Echo chronicle simulation incidents~~ **shipped (B4 #2)**
3. ~~AC-only cleanup pass~~ **shipped (B4 #3)**
4. ~~Exposure + sulfur storm vertical slice~~ **shipped (B4 #4)** — live scheduler, global storm sulfur, facility pause on Active
5. Gameplay vertical slices (base 22 shelter, materialization)

See [Framework_Folder_Mapping.md](Framework_Folder_Mapping.md) for module status.
