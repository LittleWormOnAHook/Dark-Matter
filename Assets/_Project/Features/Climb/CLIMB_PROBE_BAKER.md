# Climb Probe Baker (v3)

AAA-style grab probes on rocks / cliffs / climbable assets.

## Open the tool

Unity menu: **Dark Matter Genesis → Climb → Probe Baker**

No Ctrl+R / force refresh needed for the editor window once scripts have imported. If the menu is missing after first pull, let Unity finish compiling (or reimport the Climb feature folder).

## Bake probes

1. Select a rock / cliff prefab instance in the Scene (or drag it into **Target**).
2. Click **Ensure ProbeSet** (or just **Bake Probes** — it adds `DMClimbProbeSet` if missing).
3. Adjust **Probe Radius**, **Min Spacing**, **Bake Grid**, **Hand Span (L/R width)** (default ~0.5m).
4. Click **Bake Probes**.

Bake:

- Finds `MeshCollider` / `MeshFilter` under the target (prefers **Climbable** layer when present).
- Casts a grid of rays from outside the bounds inward.
- Keeps hits with upward-ish or outward normals; rejects strong underside normals.
- Spaces samples by **Min Spacing**.
- Tags the highest band near the local AABB top as **Lip**.
- For each kept sample, bakes a **Left + Right** pair spaced by **Hand Span** along wall-right (`Cross(up, normal)`), each re-snapped to the surface. Stores `pairId` + `hand` on probes.
- Stores positions/normals in local space of the ProbeSet transform.
- After bake, automatically runs **Apply Climbable** (see below).

Gizmos (spheres + normal ticks) draw in the Scene View. **Gizmo Size** / **Gizmo Color** update live. **Selected Probe Color** (default red) tints the panel-selected / Scene-selected probe.

## Manual place + move

1. Enable **Manual Place (Scene View click on mesh)**.
2. Click the asset mesh — a probe is added at the surface hit (`isManual = true`, unpaired).
3. Click an existing probe sphere to **select** it (list highlights; draws in Selected Probe Color).
4. Drag the **Position handle** on the selected probe (and on any manual probe) — after each drag the pose is **projected back onto the mesh** (MeshCollider preferred) and the normal is aligned to the surface.
5. Or use **Add Manual at Hit** (ray from Scene View center).

**Delete Selected** removes the highlighted probe. **Clear** wipes all. List rows marked `*` are manual; `L`/`R` + `pN` show hand side and pair id.

Undo is supported (`Undo.RecordObject`). Prefab instances get dirty with serialized probes — apply / save the prefab as usual.

## Apply Climbable (tag + layer)

Project canon: Unity **tag** `Climbable` and **layer** `Climbable` (**index 23**). Both already exist in `TagManager`.

- **Bake Probes** applies this automatically when finished.
- Or click **Apply Climbable (tag + layer 23)** explicitly.

What gets marked:

1. **Target root** — tag `Climbable`, layer 23.
2. **MeshColliders** used for bake/climb under the target (same set the baker raycasts against). Prefer colliders already on Climbable; otherwise the bake MeshCollider set.
3. If there are **no** MeshColliders, eligible **MeshFilters** (sharedMesh with >= 8 verts) under the target are tagged/layered instead.

Unrelated children without those colliders/meshes are left alone. Undo records each changed object.

## Runtime

- `DMClimbProbeSet` on the climbable root holds the list (`isManual`, `pairId`, `hand` per probe).
- Climb profile flag **`preferBakedProbes`** (default true): when attaching, `DMClimbController` prefers the nearest baked probe in range if a ProbeSet is on/near the hit collider. Mesh lip / lip8 path stays as fallback.
- API: `FindNearestProbe` / `FindNearestFacingProbe` / `FindInDirection` / `FindNearestPair` / `TryGetPairPartner` on `DMClimbProbeSet` for runtime locomotion.

Stamp: `DMClimb probe-bake-v3` (L/R hand-span pairs + select red + manual place + surface handles + Apply Climbable).
