# Phase D — Dark Matter Stack Validation

**Status:** Run 1 checklist available — Communications / full Phase D still open  
**Authority:** GDD 5.0 Appendix B5 · [World_Engine_Disk_Status.md](World_Engine_Disk_Status.md) · TDB  
**Disk:** Features GameState / WorldState / Directors / Validation **present** (July 22, 2026 Run 1)

---

## Scope to validate

| Phase | Deliverable | Validation |
|-------|-------------|------------|
| A0 | HLA v1.0 ratified | Doc exists — **done** |
| A | TDB + audits + folder mapping | Doc cross-links — **done** |
| B | WorldState API | EditMode + F9 — **Run 1 landed** |
| C | Directors stubs + command intents | EditMode + F10/F11 — **Run 1 landed** |
| D | Stack tests + GDD B5 | Validation.Tests — **partial** (Comms bridge still Run 2) |

---

## Automated checklist (EditMode)

Run under **Window → General → Test Runner → EditMode**:

- [ ] `DarkMatterStackValidationTests.*` (`Project.Features.Validation.Tests`)
- [ ] `GameStateServiceTests` (all)
- [ ] `WorldStateServiceTests` (all)
- [ ] `DirectorOrchestratorTests` + Simulation/Experience director tests

**Pass criteria:** zero failures in the Features test assemblies above.

---

## Manual checklist (Play Mode)

Enter **Pioneer** scene (`SimpleGameManager` → `CompanionSystemsBootstrap`).

- [ ] **F9** — `[WorldState]` one-line summary
- [ ] **F10** — `[Directors] trigger=ManualDebug directors=7`
- [ ] **F11** — storm phase cycle + crisis HUD
- [ ] **F5–F8** — Communications (Run 2 — not yet)

---

## Known gaps

- Communications Runtime / Radio HUD / ContextBuilder — Run 2
- World seed / WorldState persistence in `GameSaveData` — Run 3
- Full WeatherDirector scheduler (F11 command adapter exists)
- LLM / Phase 8.1 / Phase 9+ — deferred

Safe Mode: [Unity_Safe_Mode_Recovery.md](Unity_Safe_Mode_Recovery.md)

---

## Next engineering priorities (GDD B4)

2. Internal Communications (rule-based)  
3. Persistent generated world  
4. Living-world slice  
5. Command Center aggregate sim
