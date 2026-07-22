# Unity Safe Mode recovery (World Engine Run 1)

Unity enters **Safe Mode** when scripts fail to compile. After pulling this branch, use this checklist.

## 0. Unity won’t recompile / stuck in Safe Mode

Close Unity completely (Task Manager: no `Unity.exe`). From the **same folder Unity Hub opens** (must contain `Assets\` and `.git\`):

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\force-unity-recompile.ps1
```

That script:

1. `git reset --hard` to `cursor/world-engine-docs-honesty-782b`
2. Deletes `Library\ScriptAssemblies`, `Library\Bee`, `Library\BurstCache`, `Temp`, `Obj`
3. Touches a reimport stamp under `Assets/_Project/Features`

Then open **that same path** in Unity Hub and wait for a full reimport (can take several minutes).

You must see `SYNC_MARKER: world-engine-782b-v3` in:

- `Assets/_Project/Features/Directors/Adapters/WeatherCommandServiceAdapter.cs`
- `Assets/_Project/Features/Directors/Runtime/WeatherDirectorService.cs`

If markers are missing, you synced the wrong folder. If markers are present but the console still cites old line numbers (adapter L54 wrong arity / tests L11–14), Unity is opening a **different project path**.

### Still stuck after cache wipe

1. Close Unity again.
2. Delete the entire `Library` folder (slow full reimport next open).
3. Reopen the project from Hub.
4. If Safe Mode shows **0 errors**, click **Exit Safe Mode**.
5. If errors remain, paste the **first** `error CS****` line.

Do **not** keep clicking Exit Safe Mode while errors are listed — Unity will refuse to leave Safe Mode.

## 1. Hard sync only (no Library wipe)

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
```

## 2. Expected assemblies after reimport

- `Project.Features.GameState`
- `Project.Features.WorldState`
- `Project.Features.Directors`
- `Project.Features.Validation`

Adapters under `Features/*/Adapters/` compile into **Assembly-CSharp** (no asmdef on Adapters folders).

## 3. Smoke (Play Mode, Pioneer scene)

- **F9** — `[WorldState]` one-line summary  
- **F10** — `[Directors] trigger=ManualDebug directors=7`  
- **F11** — cycles storm phase → crisis HUD  

## 4. Known compile traps fixed on this branch

| Error | Cause | Fix on branch |
|-------|--------|----------------|
| `CS0246` `WeatherCommandServiceAdapter` | Missing `using` in EnvironmentWorldStateProvider | Added `using Project.Features.Directors.Adapters` |
| `CS7036` `SetCrisisActive` | HUD / adapter arity mismatch | HUD: `(bool, string, bool, bool)` + bool overload; adapter positional 4-arg |
| `CS0117` `ResolveNextPhase` | Smoke/tests called missing API | `WeatherDirectorService.ResolveNextPhase` |
| `CS0117` `IsOperationsPaused` | Overview called property never in git HUD | `EnvironmentalCrisisHudMode.IsOperationsPaused` → `IsCrisisActive` |

## 5. If Safe Mode persists after §0

Paste the **first** `error CS****` line (file + line). Also paste:

```powershell
pwd
git log -1 --oneline
Select-String -Path "Assets\_Project\Features\Directors\Adapters\WeatherCommandServiceAdapter.cs" -Pattern "SYNC_MARKER"
```
