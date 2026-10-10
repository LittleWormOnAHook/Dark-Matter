# Möller–Trumbore Technique — Mesh Hit & Surface Damage Plan

Dark Matter: Genesis — technical plan for precise mesh hits, surface carve, and future limb/deform work.

## Core idea

Use **Möller–Trumbore (MT)** ray–triangle intersection as the shared **narrow-phase** kernel. Pair it with:

- **Broad phase:** physics layers, renderer bounds, optional existing colliders / DM hitboxes
- **Skinned targets:** `SkinnedMeshRenderer.BakeMesh` on hit (not every frame)
- **Output:** `MeshHitResult` (point, normal, triangle, UV, barycentrics, renderer)

Existing reference: `DMCarveToolWindow.RaycastMesh` (static meshes). Death-time bake: `EnemyDisintegrationEffect`.

## Production combat today (baseline)

- Ranged: `Physics.Raycast` / `SphereCast` + **DMHitbox** merge (`DMEnemyHitQuery`)
- World carve: `DMCarvable` + `DMCarveImpacts` via `DMCombatFx.PlayWorldImpact` (enemies excluded by design)
- Body FX: `DMEnemyHitMarks` on hit bones; `CombatBodyPart` zones exist; per-zone damage multipliers planned (B4/D4)

## Two-track character architecture

### Track A — Modular limb SMRs (gameplay + equipment)

- One shared skeleton; **limb SMRs only where useful** (detach, swap mech arm, armor modules, variants)
- Hitboxes + `CombatBodyPart` → limb state, damage, VFX
- MT refines surface for decals / future deform region

### Track B — Local mesh deform (holes, chunks, caps)

- Hit → region on baked mesh → `DMCarveCutter` (Fracture / Erosion / Blast) → debris + cap materials
- Reuse `Project.SurfaceCarve`; budget per enemy (max patches, max tris)
- Shader clip fallback for cheap “through” look before full CSG

## Mesh combat experiment (v1.6.5)

- Parent: `_MeshCombatExperiment` in **Dark Matter Genesis v1.6.5** (no new scene)
- Collider-free targets on isolated layers for true mesh tests
- Compare Genesis collider hit vs MT mesh hit (debug markers + UITK panel)
- **No production integration** until approval

## Experimental carve ammo (Play Mode lab)

Asset: **`DMAmmoFxProfile_CarveExperiment`** (`DMCarveExperimentAmmoProfile`)

- Projectile + simple muzzle flash + tracer
- Strong **Custom** surface damage (Blast-style carve + debris)
- **`useAllPhysicsLayers`:** projectile / hitscan query all layers except Player (8)
- **`forceMeshDeformation`:** carves even when global `DMCarveSettings.ammoDeformsMeshes` is off
- **`allowCarveOnEnemyReceivers`:** carve-only path on enemies (still no production hit-mark swap)

Pickup: **`Assets/_Project/Prefabs/Items/Ammo/CarveExperiment_Pickup.prefab`** — use with any **Gunpowder**-compatible ranged weapon.

### Play Mode quick start

1. Open **Dark Matter Genesis v1.6.5**.
2. Menu: **Tools → Dark Matter Genesis → Combat → Experiment → Place Carve Pickup In Open Scene** (or drag the pickup prefab near Combat Sandbox ~ `-680, 22, -100`).
3. Pick up **Carve Experiment** ammo, equip a Gunpowder rifle/pistol, fire at **carvable** meshes (rocks with `DMCarvable`, or objects matching `DMCarveSettings` auto-carve rules).
4. Tune carve on the ammo asset: **`Assets/_Project/Data/Items/Ammo/DMAmmoFxProfile_CarveExperiment.asset`** → `surfaceDamage.custom` (style Fracture / Erosion / Blast, radius, depth, debris).
5. Rebuild FX/pickup after script changes: **Tools → … → Build Carve Experiment Ammo**.

**Note:** Global **`Resources/Carve/DMCarveSettings`** may keep `ammoDeformsMeshes` off for shipping ammo; this lab profile sets **`forceMeshDeformation`** so it still cuts mesh in Play Mode.

### Skinned meshes (Ember Skitter, Meshy dragon, etc.)

Surface Carve only cuts **`MeshFilter`** geometry. A **`SkinnedMeshRenderer` + `MeshCollider`** alone is not enough.

1. Select the creature **root** in the Hierarchy.
2. **Tools → Dark Matter Genesis → Combat → Experiment → Setup Carve Target On Selected (Skinned Mesh)**  
   Adds **`DMCarveSkinnedCarveTarget`** (creates bake child + `DMCarvable`).
3. On the **FBX** in Project: **Model → Read/Write Enabled → Apply**  
   (Dragon Meshy FBX ships with Read/Write **off** — carve will fail until this is on.)  
   Shortcut menu: **Enable Read/Write On Selected Model FBX**.
4. **`MeshCollider`**: optional on the SMR for hits; the setup moves collider sync to the bake child after first bake.
5. Play: first hits **rebake pose → carve**; skinned renderer can hide after first carve so holes stay visible.

Crates/loot work because they already use **`MeshFilter` + `MeshRenderer`** (static mesh), not skinned rigs.

## Future integration (post-report)

```text
Existing weapon → hit event → MeshHitResult (MT) → optional carve / limb SMR / hit marks
```

Recommend **Option D hybrid:** hitscan/projectile keep broad-phase; MT refines surface; limb SMRs for detach/equipment; local carve for heavy ammo.

## Open questions (measure in experiment)

1. Static mesh without colliders — MT only?
2. Skinned bake cost vs hitbox count
3. Fast projectile tunneling vs segment MT
4. Readable mesh requirement for carve on creatures
5. Can body-part hitboxes be reduced after MT + limb SMRs are proven?

---

## End state: animated skinned meshes (target)

**Goal:** While a character is **animating and moving**, weapon hits produce **correct surface location** (MT) and **persistent visible damage** (carve holes, caps, debris, or limb policy) — not static-mesh-only or “freeze then hide SMR” as the final UX.

### What “done” looks like

| Requirement | End-state behavior |
|-------------|-------------------|
| **Hit while idle / walk / attack** | Impact point on **current deformed** surface (bake-at-hit or MT on baked snapshot aligned to frame) |
| **Holes stay meaningful** | Carved topology on a **display mesh** that stays aligned with the skeleton as animation continues |
| **Gameplay** | Broad-phase can stay **hitbox / capsule**; surface is **VFX + optional damage zone**, not necessarily physics mesh |
| **Performance** | Bounded: max carves per actor, max verts, LOD policy, no full-scene bake every frame |
| **Creatures** | Profile-driven (`DMEnemyBodyType`, creature def): android sparks vs humanoid gore vs robot panels |

### Core problem (why today’s bridge is not the end state)

`DMCarveSkinnedCarveTarget` (lab):

- Bakes SMR → **`MeshFilter`** child → **`DMCarveOps`** carve
- Optional **hide SMR after first carve** → animation stops driving the **visible** body

That proves **topology change on skinned source data**. It does **not** yet solve **continuous animation + accumulated damage**.

### Recommended end architecture (three layers)

```text
Animator + shared skeleton
        │
        ├─ Gameplay: DMHitbox / CombatBodyPart (unchanged broad-phase)
        │
        ├─ Display A (default): SkinnedMeshRenderer(s) — locomotion, equipment modules
        │
        └─ Display B (damage): DMSkinnedDamageDisplay
              • Persistent carved Mesh (or per-limb carved meshes)
              • Re-skinned OR bone-bound mesh chunks updated from skeleton each frame
              • Materials: fracture / rim / char (reuse DMCarve cap pipeline)
```

**Hit pipeline (production-shaped):**

```text
Weapon hit (existing) → hitbox/collider snapshot
        → MeshHitResult (MT on bake-at-hit OR cached BVH on last bake)
        → DMSurfaceDamageSettings (ammo/profile)
        → Apply damage to Display B (carve), optional Track A (limb detach)
        → Existing hit marks / SFX (point from MeshHitResult, not capsule center)
```

### Animation strategies (pick per actor tier)

| Tier | Use when | Approach |
|------|----------|----------|
| **1 — Hit-sync bake** | Low-poly creatures, few hits | Rebake SMR at impact time only; append carve to **damage mesh** parented to root; **disable SMR on that submesh region** or swap to limb SMR (Track A) |
| **2 — Dual mesh rig** | Humanoids with many shots | SMR for clean skin; **second SMR or MeshRenderer** using **same bones** but **carved mesh asset** updated on hit; both follow animator |
| **3 — Modular limbs (Track A)** | Detach / mech swap | Carve on **limb SMR** only; on threshold, detach SMR + spawn rigidbody chunk; torso stays SMR + local carve |
| **4 — Shader + topology** | Distant LOD / budget | Near: real carve (Tier 1–2); far: clip/decal only |

**Do not** require full-body CSG every frame. **Do** require **skeleton-locked damage mesh** after each carve so idle/walk/attack still reads as one creature.

### Möller–Trumbore role on animated targets

- **On impact:** Ray/projectile segment vs **pose baked at hit time** → triangle, UV, normal → cutter placement
- **Not** vs bind-pose shared mesh
- Optional: bone weight at barycentric → **`CombatBodyPart`** / limb id for Track A

### Data & tooling (Genesis-shaped)

| Asset / system | Purpose |
|----------------|---------|
| `DMSkinnedDamageProfile` (new) | Max carves, rebake policy, tier, hide-SMR rules, LOD |
| Creature / enemy prefab | `DMCarveSkinnedCarveTarget` → evolves to **`DMSkinnedDamageDisplay`** |
| Import rules | **Read/Write** on combat creature FBXs; max tris budget in Creature Manager |
| Ammo / `DMAmmoFxProfile` | Surface damage row per tag/body type (already pattern) |
| Combat Studio | Sliders: carve radius/debris + **skinned tier** override |

### Phased roadmap (animated SMR as north star)

| Phase | Focus | Exit |
|-------|--------|------|
| **0 (now)** | Carve Experiment ammo + `DMCarveSkinnedCarveTarget` | Holes on posed/animating target at **hit frames**; Read/Write validated |
| **1** | `_MeshCombatExperiment` + MT `MeshHitResult` on patrol SMR | Compare collider vs MT while **walking** |
| **2** | **Damage display v1:** carved mesh **parented to root**, follows transform; SMR hidden only on carved regions or full switch after N hits | Walk cycle with **visible** carved body |
| **3** | **Bone-aligned damage mesh** (same skeleton as SMR) | Attack animations; holes don’t “slide” on torso |
| **4** | Integrate selected ammo + creature profiles; keep shipping ammo unchanged by default | One canonical enemy + one ammo type |
| **5** | Track A limb detach + Track B local carve **policy matrix** | Design-approved per body type |

### Non-goals for end state (unless explicitly approved)

- Replacing **DMHitbox** with mesh-only damage for all enemies on day one
- Carving **every** ammo type on **every** skinned mesh in the world
- NavMesh / new character controller changes
- Real-time **full-body** remesh every frame

### Success metrics (before production hook-up)

1. Hit **walking** Ember Skitter / dragon: MT point within ε of visual surface  
2. After 5 carves, mesh still animates without exploding bounds (Meshy scale pitfalls)  
3. ms per hit (bake + carve + collider refresh) on PC target; stricter budget for console  
4. Memory: pooled bake meshes, cap live carved instances  
5. Designer can tune **only** profile + ammo, not per-creature C#

### Recommendation

**End state = Option D hybrid**, with **animated skinned** work centered on **`DMSkinnedDamageDisplay` + MT at hit time**, not on extending **`DMCarvable`** alone:

- **Hitscan / projectile:** existing query → MT refine on baked pose → carve **damage display**  
- **Limbs / equipment:** Track A SMR modules  
- **Heavy/spam damage:** Track B carve caps on damage display  
- **Static world:** keep current `DMCarvable` path (crates, rocks)

Current lab component is **Phase 0**; next engineering step after mesh-hit experiment is **Phase 2 (damage display v1)**, not wiring all production weapons.

---

*Last updated: end-state plan for animated skinned meshes.*
