---
name: code-architecture
description: >
  General coding standards, SOLID principles, and architecture guidelines.
  Activate when writing/modifying systems, services, data flow, events,
  or reviewing any code change. Not Unity-specific.
---

# Code Architecture & Standards

## Priority: Readability and Maintainability

Every change must leave code **clearer than before**. Prefer simple, obvious code over clever abstractions. When in doubt, choose the approach that is easiest for the next developer to read.

---

## DO NOT Add Comments

**Do not add comments to code unless the user explicitly asks for them.** This includes:
- Doc comments (`/// <summary>`, `/// <param>`, `/// <returns>`, etc.)
- `//` inline comments explaining what code does
- Section divider comments
- "TODO" or "NOTE" comments

Code should be self-explanatory through clear naming and small methods. If you feel a comment is needed, rename the variable/method instead. The only exception is when the user specifically requests comments.

---

## 0. SOLID Principles

Every architectural decision must be traceable to SOLID. These are not abstract ideals — they have concrete meanings in practice.

### S — Single Responsibility

A class has **one reason to change**.

- A class handles **one concern**: visuals OR input OR physics OR networking — not all of them.
- If a main loop method has more than 3 distinct logic blocks, split into separate components or systems.
- Services own one domain: `IAudioService` plays audio, `IInventoryService` manages inventory. They don't cross domains.
- Data classes hold data. Behaviours react to it. Don't mix storage with presentation.

**Smell:** A class named `PlayerManager` that handles input, health, UI, and networking. Split into `PlayerInput`, `PlayerHealth`, `PlayerHUD`, `PlayerNetworkSync`.

### O — Open/Closed

Classes are **open for extension, closed for modification**.

- Use **data-driven config** to change behavior without editing code. New variant? New config asset, not a new `if` branch.
- Use **composition over inheritance**: add/remove modules rather than creating deep inheritance chains.
- Use **interfaces and events** so new systems can hook in without modifying existing ones.
- Prefer **strategy pattern via interface** over `switch`/`if-else` chains that grow with each feature.

**Smell:** Adding a new item type requires editing 5 existing files. Instead, define a new config asset and register it.

### L — Liskov Substitution

Subtypes must be **substitutable for their base types** without breaking behavior.

- Any class implementing an interface must honor the full contract. Half-implementing an interface breaks consumers.
- Don't override a virtual method to do nothing or throw `NotImplementedException`. If a subclass doesn't need the behavior, the hierarchy is wrong — use composition instead.

**Smell:** A subclass overrides a serialization method but only handles writing, not reading. Remote consumers break silently.

### I — Interface Segregation

Clients should not depend on methods they don't use.

- Keep service interfaces **small and focused**: `IAudioService` has `Play`, `Stop`, `SetVolume` — not 30 methods covering every audio subsystem. Split when they serve different consumers.
- Separate **read vs write interfaces** when useful: `IInventoryReader` (UI needs this) vs `IInventoryWriter` (only purchase system needs this).
- Interaction contracts should each cover one capability, not a mega-interface.

**Smell:** A UI panel has to implement `IGameSystem` with 12 methods just to read the player's score.

### D — Dependency Inversion

High-level modules must not depend on low-level modules. Both should depend on **abstractions**.

- Systems depend on **interfaces** (`ICloudDataService`), not concrete implementations (`CloudDataServiceClient`).
- Use a **Service Locator** or DI container to resolve interfaces at runtime.
- Consumers request `IInventoryService`, not `InventoryManager`.
- Networking code depends on `INetworkService`, not raw transport APIs, so the layer can be swapped.

**Smell:** A UI script directly calls a third-party API. Instead, it calls an interface method which the concrete implementation fulfills.

### Quick SOLID Test

When writing or reviewing code, ask:
1. Does this class have **one reason to change**? (S)
2. Can I add new behavior **without modifying** this class? (O)
3. Can I **swap any subclass** for the base and things still work? (L)
4. Do consumers depend only on **what they actually use**? (I)
5. Does this depend on **abstractions**, not concretions? (D)

If any answer is "no" in new code, refactor before committing. In legacy code, note it and refactor when touching that area.

---

## 1. Service-Oriented Architecture

Use the **Service pattern** for shared systems. Services are singletons (or scoped instances) that own data/logic and expose it via **events + public API**.

Rules:
- Consumers subscribe to events — they never poll or call internal methods
- Services own their data; consumers read via properties or request methods
- Keep service APIs small: expose what's needed, hide implementation

### Structural Layers

| Layer | Responsibility | Dependencies |
|---|---|---|
| **Infrastructure (Core)** | Low-level utilities, math extensions, native buffer wrappers | None |
| **Data Layer** | Config data, runtime state | Core |
| **Service Layer** | Interface-driven access via Service Locator (e.g., `IAudioService`, `IInputService`) | Core, Data |
| **Presentation (View)** | UI using MVVM to separate UI state from display logic | Gameplay |

### Module Boundaries

Divide into functional boundaries to minimize recompilation. **No cyclic dependencies.**

Rules:
- Never add upward dependencies (UI must not reference Core directly bypassing Gameplay)
- Keep third-party wrappers in their own module (e.g., wrap external APIs behind an interface)
- Editor-only code goes in separate modules

---

## 2. Dependency Injection & State Separation

- **Dependency Injection:** Use interfaces and inject via a Service Locator or DI framework. Makes code mockable and testable.
- **State Separation:** Game/app state (health, ammo, timers) lives in pure C# classes (POCOs) or structs. Views subscribe to events (`OnHealthChanged`) and update visuals.
- **Single Responsibility:** A class does exactly one thing. If your main loop has more than 3 distinct logic blocks, refactor into separate systems.

---

## 3. Event System

Use a **lightweight event service** for decoupled communication between systems.

### When to use events
- System A needs to notify System B without referencing it
- Multiple listeners need to react to the same thing (inventory changed, item purchased, player died)
- Crossing layer boundaries (service -> UI, gameplay -> audio)

### When NOT to use events
- One-to-one calls where a direct method call is simpler and clearer
- High-frequency per-frame data (use direct references or shared state instead)

### Rules
- Always unsubscribe when the listener is torn down to prevent leaks
- Event handlers must be fast — no heavy work, no async awaits inside handlers. Enqueue work if needed
- Name events as past-tense facts: `OnItemPurchased`, `OnPlayerDied`, `OnInventoryUpdated`
- Keep event payload minimal: pass an ID or small struct, not large objects

---

## 4. Logging Discipline

Do NOT add log statements unless genuinely necessary.

**Log only:**
- Unrecoverable errors — broken invariants, null required refs
- One-time lifecycle milestones — system init confirmation
- Temporary investigation — **remove before committing**

**Never log:**
- Expected null/miss results (handle silently)
- Per-frame or high-frequency callbacks
- Success confirmations (silence = success)
- Defensive "just in case" — use guard clauses instead

Prefer warnings over errors for degraded-but-functional states.

---

## 5. State Machines

Use when behavior has 3+ distinct modes with explicit transitions.

Good fit: multi-phase processes, AI behavior, multi-step UI flows, interaction states.

Rules:
- Define states and transitions up front
- Centralize transition logic in one place
- Isolate per-state enter/update/exit
- Keep transitions allocation-free in hot paths

---

## 6. Readability Rules

- **Name things clearly** — a good name eliminates the need for comments and docs
- **Small methods** — each method does one thing. If you need a comment to explain a block, extract it into a named method
- **No magic numbers** — use `const` or configurable fields with descriptive names
- **Guard clauses early** — return/throw at the top, don't nest deep
- **Consistent patterns** — follow existing conventions in the file/folder you're editing
- **Don't over-abstract** — three similar lines are better than a premature helper. Extract only when there's a real, repeated pattern
