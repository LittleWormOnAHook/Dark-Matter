---
name: unity-runtime-guardian
description: >
  Unity runtime debugging: assertions, console checks, MCP tools, logging format.
  Use only in Unity projects when debugging scripts, features, or errors.
disable-model-invocation: true
---

# Unity Runtime Guardian (Merged Expert Level)

Most bugs in VR multiplayer projects surface at runtime — not at compile time — so proactive logging, assertions, and console monitoring are essential.

## 1. Fail Fast: Assertions Are Mandatory

Before writing logic, ensure invariants are protected:
- Use `Debug.Assert(condition, "Message")` in `Awake()` and at the start of complex methods.
- Example: `Debug.Assert(_weaponConfig != null, "[Weapon] Weapon config is missing!");`
- Catch configuration and state errors at the boundary, not deep in the call stack.

## 2. Check Unity Console First

Before reading code, planning changes, or answering questions:
1. Use `read_console` MCP tool (filter by `Error` first, then `Warning`).
2. If there are compilation errors, address them **before** proceeding.
3. A dirty console means a broken state. Never build on top of errors.

## 3. Use Unity MCP Tools Actively

The live editor state is the ground truth:
- `read_console` — check for errors/warnings.
- `manage_scene` / `manage_gameobject` — verify live hierarchy and components.
- `manage_asset` — search for assets, prefabs, ScriptableObjects.
- `refresh_unity` — trigger a recompile after script changes.
- `validate_script` — check scripts for errors before saving.

## 4. Prefab and Scene Changes: MCP First, Editor Scripts When Needed

When a task requires changing prefab or scene assets:
1. **Prefer Unity MCP tools** (`manage_asset`, `manage_scene`, etc.) so changes go through Editor APIs.
2. **Avoid hand-editing YAML** (serialization format is brittle).
3. **Use Editor scripts** only when mutation cannot be done reliably through MCP (batch updates, `SerializedObject` logic, project settings).
4. Place scripts under `Assets/Editor/Automation/`, make them idempotent, and expose via `[MenuItem]`.

## 5. Add Debug.Log Statements for Runtime Visibility

Add consistent, filterable logs to every modified script:
`Debug.Log($"[{nameof(ClassName)}.{nameof(MethodName)}] description: {variable}");`

**Prioritize:** State transitions, RPC send/receive, null-guard hits, pooling ops, VR interaction events.
**Throttling:** Never log inside `Update`/`FixedUpdate` unless throttled or triggered by an event.

## 6. Advanced Profiling & Memory

- Use **Unity Profiler** for CPU spikes.
- Use **Memory Profiler** for un-disposed `NativeArray`s or event listener leaks.
- Always verify that `OnDestroy` correctly un-subscribes from events.

## 7. Verify After Changes

1. `refresh_unity`
2. Use `read_console` to check for new compilation errors.
3. Fix errors immediately before reporting completion.

## Quick Reference: Start-of-Response Checklist
1. `read_console` (Error filter) — any compile errors?
2. Add `Debug.Assert` to all boundaries of new code.
3. Add `Debug.Log` statements to modified code.
4. After changes: `read_console` again to verify clean state.
