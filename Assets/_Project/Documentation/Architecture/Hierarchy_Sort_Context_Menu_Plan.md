# Hierarchy Sort Context Menu — Plan

Editor-only utility: right-click in Hierarchy → sort sibling GameObjects.

## Goal

Add a context menu on Hierarchy selection:

| Command | Behavior |
|---------|----------|
| **Sort Children By Name** | Alphabetical (A→Z); optional reverse |
| **Sort Children By Age** | Creation / fileID order (older first) — see Age below |
| **Sort Children By Size** | Approximate world bounds volume / renderer bounds (largest first or smallest first) |

Scope: **children of the selected object(s)** (or selected siblings under a shared parent). Does not reparent across the scene.

## UX

- Context path: `GameObject/Dark Matter Genesis/Sort Children/...`
  - By Name (A→Z)
  - By Name (Z→A)
  - By Age (Oldest first)
  - By Age (Newest first)
  - By Size (Largest first)
  - By Size (Smallest first)
- Also mirror under `Tools/Dark Matter Genesis/Hierarchy/` via `DarkMatterGenesisEditorMenus` for discoverability.
- Multi-select: if several objects share one parent, sort those siblings only; if selection is a single parent, sort its direct children.
- Confirm dialog when child count > N (e.g. 100) to avoid accidental huge reorder.
- Full Undo: `Undo.SetTransformParent` is wrong here — use `Undo.RegisterCompleteObjectUndo` on the parent + `SetSiblingIndex` in a recorded undo group.

## Implementation

**New file:** `Assets/_Project/Editor/Hierarchy/HierarchySortUtility.cs`  
**Menu constants:** `DarkMatterGenesisEditorMenus.Hierarchy` + sort paths.

### Core API

```csharp
SortChildren(Transform parent, SortMode mode, bool ascending)
```

1. Collect `parent.GetChild(i)` into a list.
2. Sort with stable comparer for the mode.
3. Apply indices with `transform.SetSiblingIndex(i)` inside `Undo.IncrementCurrentGroup()` / `Undo.SetCurrentGroupName(...)`.
4. Mark scene dirty.

### Sort definitions

| Mode | Metric |
|------|--------|
| **Name** | `string.CompareOrdinal` / ignore-case culture-invariant on `gameObject.name` |
| **Age** | Prefer `LocalIdentifierInFile` / `Unsupported.GetLocalIdentifierInFile` for scene objects (lower ≈ older in file). Fallback: sibling index (current order) if ID unavailable. Prefab mode: local file ID within prefab. Document that “age” means **scene serialization age**, not wall-clock create time (Unity does not store create timestamps on GameObjects). |
| **Size** | For each child: encapsulate `Renderer.bounds` (and optionally `Collider.bounds`); use `extents` volume or max axis. Objects with no renderer/collider sort last (or treat as zero). Use world bounds so nested meshes count. |

### Validation

- `[MenuItem(..., true)]` validate: selection not empty; for “sort children”, selected transform has ≥ 2 children **or** ≥ 2 selected siblings.
- Disabled in Play Mode (or allow with warning — prefer Edit Mode only).
- Skip if parent is part of a locked prefab asset without unlocking (open Prefab Stage OK).

## Risks / notes

- Reordering can break scripts that assume sibling order (UI layout, waypoints). Undo mitigates; document in menu tooltip.
- “Age” is approximate (fileID), not true creation time — label menus clearly: “By Scene Age (file order)”.
- Size on empty parents / lights / empty transforms may be 0 — group them consistently.
- Do not sort across different parents in one operation.

## Out of scope (v1)

- Recursive sort of entire subtree
- Sort by component type / tag / layer
- Auto-sort on rename
- Project window asset sorting

## Test plan

1. Empty parent with 5 named cubes → Name A→Z / Z→A.
2. Create objects in order A then B then C → Age oldest-first matches creation order via fileID.
3. Large mesh vs small mesh under parent → Size largest-first.
4. Multi-select siblings → only those reorder.
5. Undo restores previous order.
6. Prefab Stage: sort children, apply, save.

## Effort

~1 small editor script + menu constants. Half hour to implement and smoke-test.
