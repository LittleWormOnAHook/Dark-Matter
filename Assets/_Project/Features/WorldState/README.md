# Features/WorldState

**HLA §7** — evolutionary read model embedding GameState.  
**Run 1** — World Engine spine.

- `Runtime/` — `WorldStateService`, snapshots (`Project.Features.WorldState`)
- `Adapters/` — story/colony/environment/session/… providers (Assembly-CSharp)
- Smoke: **F9** one-line summary (`DarkMatterSmokeDriver`)

Bootstrap: `WorldStateBootstrap.EnsureExists(host)` after GameState.
