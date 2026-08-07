---
name: unity-architecture
description: >
  Unity-specific architecture, editor workflow, performance optimization,
  and data/pooling patterns. Activate when writing/modifying Unity code:
  MonoBehaviours, ScriptableObjects, physics, networking, pooling, VR
  performance, or editor tooling.
---

# Unity Architecture & Performance

This skill covers Unity-specific standards. For general SOLID, service architecture, event patterns, and readability rules, see `code-architecture` (if available in your skill set).

---

## 0. Editor-Time Setup Over Runtime Checks

**Do the work in the editor, not at runtime.** If something can be configured, wired, validated, or caught at edit-time, it must be — never defer to runtime what the editor can guarantee.

This means:
- **Wire references in prefabs/scenes**, not via `GetComponent`/`Find*` at runtime
- **Validate configuration in `OnValidate()` and editor scripts**, not with runtime null-checks or try/catch
- **Set up component state in the inspector or `Reset()`**, not in `Awake()`/`Start()` init blocks
- **Catch errors at import/save time** with editor tools and custom inspectors, not during gameplay
- **Use `[RequireComponent]`** to enforce component dependencies at the prefab level
- **Build editor tooling** (custom windows, auto-wirers, validators) to eliminate manual setup errors before they become runtime bugs

Runtime initialization should only handle things that genuinely cannot be known until play — network state, dynamic spawns, player input. Everything else is an editor responsibility.

---

## 1. SOLID in Unity

The SOLID principles have specific Unity applications:

### S — Single Responsibility
- A MonoBehaviour handles **one concern**: visuals OR input OR physics OR networking — not all of them.
- If `Update()` has more than 3 distinct logic blocks, split into separate components.

### O — Open/Closed
- Use **ScriptableObject-driven config** to change behavior without editing code. New weapon? New SO asset, not a new `if` branch.
- Prefer **composition via serialized modules** on a base item/config SO (see section 8) over deep inheritance trees.

### L — Liskov Substitution
- If a base SO exposes `GetPrice()`, every subclass must return a valid price.
- Network callback interfaces must fully implement their contracts. A partial implementation causes silent networking bugs.

### D — Dependency Inversion
- MonoBehaviours receive dependencies via **SerializeField** (editor-injected) or **interface lookup** — never by finding concrete types with `FindObjectOfType<ConcreteClass>()`.
- Services live on `DontDestroyOnLoad` GameObjects or use `ServiceLocator`.

---

## 2. SerializeField Conventions

### Prefer SerializeField over GetComponent — always

**Do not use `GetComponent` at runtime.** All component references should be resolved at edit-time and stored in `[SerializeField]` fields. This is faster (zero runtime cost), explicit (you see what's wired in the inspector), and catches missing references before play mode.

Use one of these approaches to populate SerializeField references:

1. **`Reset()` method** — auto-populates when the component is added in the editor:

```csharp
private void Reset()
{
    _rigidbody = GetComponent<Rigidbody>();
    _collider = GetComponentInChildren<Collider>();
    _audioSource = GetComponentInChildren<AudioSource>();
}
```

2. **Editor scripts / custom inspectors** — for complex wiring, cross-prefab references, or batch operations. Use `OnValidate()` or dedicated editor tools to auto-resolve references.

3. **Manual inspector assignment** — acceptable for scene-level or cross-hierarchy references that can't be auto-resolved.

**Rules:**
- Never call `GetComponent`, `GetComponentInChildren`, or `GetComponentInParent` in `Awake()`, `Start()`, `OnEnable()`, or any runtime method. Wire it in the editor instead.
- The only exception is dynamically spawned objects where the reference truly cannot be known at edit-time (e.g., runtime-instantiated prefabs wiring to scene objects). Even then, prefer passing the reference via an init method rather than searching for it.
- If you need a reference that `Reset()` can't find (different hierarchy, scene object), write an editor script or use `OnValidate()` to populate it.

### Never null-check a SerializeField

A `[SerializeField]` field is a contract: it **must** be assigned in the inspector or via `Reset()`. Do not write `if (myField == null)` guards around them — if they're null, that's a configuration bug that should surface immediately, not be silently swallowed.

Exceptions:
- Fields marked with `[CanBeNull]`, `[Optional]`, or a similar attribute — these explicitly signal the field may be empty
- Fields on components that are reused across prefabs where the reference is genuinely optional by design

If a reference is truly optional, mark it clearly so the intent is obvious:

```csharp
[SerializeField, Tooltip("Optional — leave empty to disable trail")]
private TrailRenderer _optionalTrail;
```

---

## 3. Zero-Allocation Hot Paths (Mandatory)

Mandatory for any code in `Update`, `FixedUpdate`, or high-frequency callbacks. VR requires 72-90hz; even minor GC pauses cause motion sickness.

### Physics Queries
Always use non-allocating variants with pre-allocated buffers and strict LayerMasks:

```csharp
private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

int count = Physics.RaycastNonAlloc(origin, direction, HitBuffer, maxDistance, layerMask);
for (int i = 0; i < count; i++) { /* process HitBuffer[i] */ }
```

### Strings & Collections
- **No `+` or `$""` inside `Update()`:** Use `FixedString32Bytes` or a pooled `StringBuilder`
- **Collections:** No LINQ (`.Where`, `.Select`, `.ToList`) in hot paths — use `for` loops
- **Boxing:** Don't pass value types as `object` in hot paths

### General
- All component references must be `[SerializeField]` wired at edit-time (see section 2) — no `GetComponent` at runtime
- Cache `Camera.main` in `Awake`/`Start` — it calls `FindObjectOfType` internally
- Use `CompareTag("Tag")` instead of `gameObject.tag == "Tag"` (avoids string allocation)

---

## 4. Object Pooling

Never raw `Instantiate`/`Destroy` for frequently spawned objects. Use a dedicated pool.

### Recommended shape

| Piece | Role |
|---|---|
| `GenericPool<T>` | Queue-based generic pool: `Get()`, `Release(T)`, `Prewarm(int)` |
| Networked pool | Implements your netcode prefab-pool interface |
| Local pool | Non-networked objects only |
| `PooledObject` (or equivalent) | Lifecycle component on every pooled prefab; return-to-pool API |
| Pool catalog SO | ScriptableObject listing prefabs by category with `startingCount` / networked flags |

### Spawn/despawn API (illustrative)

```csharp
// Networked spawn (routes through netcode when in a room)
var go = NetworkPool.Get(prefabId, position, rotation);

// Local-only spawn
var go = LocalPool.Get(prefabId, position, rotation);

// Return to pool
pooledObject.Release();
```

### Rules
- Every pooled prefab carries a pool lifecycle component
- Register prefabs in a catalog SO (particles, VFX, weapons, projectiles, cosmetics, etc.)
- Detect networked vs local via presence of a network identity component — avoid manual branching at call sites
- Non-owner despawn requests go through RPC/ownership to the owner
- Higher-level spawn helpers should route through the pool, not call `Instantiate` directly
- Prewarm with `startingCount` to avoid runtime alloc spikes; grow on demand if needed

---

## 5. Networking

- Only the owning client processes input and authoritative local state
- Critical state (health, scores, inventory) validated by the host / server
- RPCs: use a clear `Rpc_` prefix, pass IDs/indices not strings, throttle unchanged data
- Continuous sync should use change detection — don't send every frame

---

## 6. Async Patterns (UniTask)

Prefer **UniTask** over Coroutines. UniTask is zero-alloc and supports cancellation.

```csharp
// Preferred
async UniTask DoWorkAsync(CancellationToken ct)
{
    await UniTask.Delay(500, cancellationToken: ct);
}

// Acceptable for legacy code only
IEnumerator LegacyRoutine() { yield return new WaitForSeconds(0.5f); }
```

Always pass `destroyCancellationToken` or `GetCancellationTokenOnDestroy()` to prevent work on destroyed objects.

### Centralized Update Management
Instead of 1,000 `MonoBehaviour.Update()` calls, prefer a centralized `TickManager` that iterates over an array of `ITickable` objects when applicable.

---

## 7. Job System & Burst

Use C# Jobs + Burst for CPU-bound batch operations (damage falloff, spatial queries, AI evaluation) when applicable.

- Always `Dispose()` `NativeArray` / `NativeList` — prefer explicit cleanup in `finally` blocks
- Use `[ReadOnly]` on all input arrays to enable Burst load-store optimizations
- Never access managed types (classes, strings, `UnityEngine.Object`) inside Burst jobs
- Use `math.*` (`Unity.Mathematics`) instead of `Mathf.*` inside jobs for SIMD instructions

---

## 8. Data Architecture & ScriptableObject Hierarchy

### General rule
- **Config** = `ScriptableObject` (read-only at runtime)
- **State** = plain C# struct or class (mutable at runtime)
- Never mutate SO fields at runtime — copy to runtime state

### Catalog item pattern (composition over inheritance)

Prefer a base catalog SO with **serialized modules** rather than a deep type tree:

```
CatalogItemData (base SO)
├── WeaponItemData
├── CosmeticItemData
├── BundleItemData
└── CharacterItemData
```

### Composition modules (illustrative)

| Module | Purpose |
|---|---|
| `CurrencyPriceModule` | Soft/hard currency prices |
| `SessionCurrencyModule` | In-session / match currency prices |
| `SettingsModule` | Consumable / stackable flags |
| `ShopConfigModule` | Shop availability flags |
| `IAPModule` | Real-money IAP (SKU, store price) |
| `ConsumableModule` | Remaining uses tracking |

### Container nesting

Organize catalog assets in nested ScriptableObject containers (groups → subgroups → item refs). At runtime, flatten into a `Dictionary<string, CatalogItemData>` (or similar) keyed by stable item id, and sync with your backend catalog/inventory if you have one.

Keep enums for item type, purchase channel, rarity, and category project-specific — define them in your game’s data layer, not as hardcoded skill assumptions.

---

## 9. Unity Logging

Use `Debug.Log` / `Debug.LogWarning` / `Debug.LogError`. Unity-specific additions:

- `LogError` for broken invariants, null required refs
- `LogWarning` for degraded-but-functional states
- Never log in `Update`, `FixedUpdate`, or per-frame callbacks

---

## Quick Checklist

Before submitting any Unity code change:
- [ ] No allocations in Update/FixedUpdate
- [ ] Physics uses NonAlloc with static buffers
- [ ] No runtime GetComponent — all refs are SerializeField wired at edit-time
- [ ] Setup/validation done in editor (`Reset()`, `OnValidate()`, editor tools) — not at runtime
- [ ] Camera.main cached
- [ ] Async uses UniTask with cancellation
- [ ] Events unsubscribed in OnDestroy
- [ ] No comments added (unless user explicitly requested them)
- [ ] No unnecessary Debug.Log added
- [ ] SerializeFields auto-wired in `Reset()` where possible
- [ ] No null-checks on SerializeFields (unless marked optional)
- [ ] Frequently spawned objects use a pool
- [ ] ScriptableObject not mutated at runtime
- [ ] CompareTag used for tag checks
- [ ] Assembly boundaries respected — no upward/cyclic dependencies
