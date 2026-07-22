# Unity Safe Mode recovery (World Engine Run 1)

Unity enters **Safe Mode** when scripts fail to compile. After pulling this branch, use this checklist.

## 0. Hard sync (do this first if Safe Mode returns after a fix)

**If the console still reports `SetCrisisActive` at adapter line 54, or `ResolveNextPhase` at test lines 11–14, those files on disk are not the git tip.** On tip, adapter line 54 is the full 4-arg call and tests call `ResolveNextPhase` near lines 20–23.

Close Unity. From the **same folder Unity opens as the project** (must contain both `Assets\` and `.git\`):

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\sync-world-engine-branch.ps1
```

Or manually:

```powershell
git fetch origin
git checkout cursor/world-engine-docs-honesty-782b
git reset --hard origin/cursor/world-engine-docs-honesty-782b
git clean -fd "Assets/_Project/Features"
Select-String -Path "Assets\_Project\Features\Directors\Adapters\WeatherCommandServiceAdapter.cs" -Pattern "SYNC_MARKER"
Select-String -Path "Assets\_Project\Features\Directors\Runtime\WeatherDirectorService.cs" -Pattern "ResolveNextPhase"
```

You must see `SYNC_MARKER: world-engine-782b-v3`. Then reopen Unity.

If markers are present but Unity still shows the old line numbers, Unity is opening a **different project path** than the git folder you synced.

## 1. Prefer git Features over local ChatGPT stubs

If you previously created incomplete `Features/GameState`, `WorldState`, `Directors`, etc. locally (outside git), they can conflict with this Run 1 implementation.

1. Close Unity.
2. Run the hard sync in §0.
3. Reopen Unity and let it reimport.

## 2. Expected assemblies after reimport

Console should show (after play or domain reload):

- `Project.Features.GameState`
- `Project.Features.WorldState`
- `Project.Features.Directors`
- `Project.Features.Validation`

Adapters under `Features/*/Adapters/` compile into **Assembly-CSharp** (no asmdef on Adapters folders).

## 3. Smoke (Play Mode, Pioneer scene)

After `CompanionSystemsBootstrap` runs:

- **F9** — `[WorldState]` one-line summary  
- **F10** — `[Directors] trigger=ManualDebug directors=7`  
- **F11** — cycles storm phase → crisis HUD  

## 4. Known compile traps fixed on this branch

| Error | Cause | Fix on branch |
|-------|--------|----------------|
| `CS0246` `WeatherCommandServiceAdapter` | Missing `using` in EnvironmentWorldStateProvider | Added `using Project.Features.Directors.Adapters` |
| `CS7036` `SetCrisisActive` | HUD / adapter arity mismatch | HUD: `(bool, string, bool, bool)` + bool overload; adapter positional 4-arg |
| `CS0117` `ResolveNextPhase` | Smoke called missing API | `WeatherDirectorService.ResolveNextPhase` |
| `CS0117` `IsOperationsPaused` | Overview called property never in git HUD | `EnvironmentalCrisisHudMode.IsOperationsPaused` → `IsCrisisActive` |

## 5. If Safe Mode persists

Open the Safe Mode console, copy the **first** `error CS****` line (file + line), and paste it in chat. Common leftover causes:

- Duplicate class names from old ChatGPT stubs still on disk
- Missing `.meta` / broken asmdef references (reimport `Assets/_Project/Features`)
- Mixing Input Manager-only code (this project uses Input System — smoke uses `Keyboard.current`)
