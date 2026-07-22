# Features/GameState

**HLA §5** — momentary gameplay read model.  
**Run 1** — World Engine spine.

- `Runtime/` — `GameStateService`, snapshots, providers interface (`Project.Features.GameState`)
- `Adapters/` — legacy manager bridges (Assembly-CSharp)
- `Tests/` — EditMode

Bootstrap: `GameStateBootstrap.EnsureExists(host)` via `CompanionSystemsBootstrap`.
