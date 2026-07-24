# Features/Directors

**HLA §8** — Intelligence orchestrator + director stubs.  
**Run 1** — World Engine spine.

- `Runtime/` — `DirectorOrchestrator`, Weather/Simulation/Experience stubs, command interfaces
- `Adapters/` — weather → crisis HUD, simulation → Echo chronicle
- Smoke: **F10** evaluate · **F11** cycle storm phase

Bootstrap: `DirectorsBootstrap.EnsureExists(host)` after WorldState.
