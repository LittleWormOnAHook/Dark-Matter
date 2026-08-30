---
name: code-authoring
description: >
  Human-readable code craft. Naming, structure, flow, cognitive load
  reduction, minimal surgical diffs (replace only needed lines, don't
  restructure whole files), and keeping debug/profiler scaffolding out of
  product PRs. Activate when writing new code, refactoring for clarity,
  editing existing files, reviewing readability, or preparing a PR.
  Complements code-architecture (SOLID, services) and project naming
  conventions.
---

# Human-Readable Code

Code is read far more than it is written. Every line should minimize the reader's cognitive load. This skill covers the craft of writing code that reads like well-structured prose.

For SOLID principles and service architecture, see `code-architecture`.
For casing, style, and member ordering, follow the project's naming conventions.

---

## 1. Names Reveal Intent

A name should answer **why it exists, what it does, and how it's used** — without requiring the reader to look at surrounding code.

### Variables — name the meaning, not the type

```csharp
// Bad
float t = 0.5f;
int c = items.Count;
List<Player> list = GetPlayers();

// Good
float damageMultiplier = 0.5f;
int remainingSlots = items.Count;
List<Player> playersInRange = GetPlayers();
```

### Booleans — phrase as yes/no questions

Use `is`, `has`, `can`, `should`, `was` prefixes so the name reads naturally in `if` statements:

```csharp
// Bad
bool active, lock, check;

// Good
bool isActive, hasLock, canFire, shouldRespawn, wasDestroyed;
```

### Methods — name the action and the outcome

Use verb-object pairs. The name should tell you what happens without reading the body:

```csharp
// Bad
void Process();
void HandleData();
void DoStuff();

// Good
void ApplyDamageToTarget();
void SyncInventoryWithServer();
void DisableInputDuringCutscene();
```

### Avoid encoding and noise

- No Hungarian notation (`strName`, `iCount`)
- No type suffixes on variables (`playerList` → `players`)
- No meaningless prefixes (`data`, `info`, `temp`, `my`) unless they genuinely distinguish
- No single-letter names outside tiny loop indices (`i`, `j`)

---

## 2. Methods Tell a Story

Each method should read as a single step in a narrative. A reader scanning method names in a class should understand the class's behavior without reading any method body.

### One level of abstraction per method

Don't mix high-level intent with low-level mechanics:

```csharp
// Bad — mixes abstraction levels
void StartRound()
{
    _timer = _config.RoundDuration;
    _isActive = true;
    BroadcastRoundStartRpc();
    foreach (var player in _players)
    {
        player.transform.position = _spawnPoints[player.Index].position;
        player.GetComponent<PlayerHealth>().Reset();
    }
    _ui.ShowCountdown();
}

// Good — each call is the same abstraction level
void StartRound()
{
    ResetTimer();
    TeleportPlayersToSpawns();
    ResetPlayerHealth();
    BroadcastRoundStart();
    ShowCountdownUI();
}
```

### Extract to explain, not to reuse

If a block needs a comment, extract it into a method whose name **is** the comment:

```csharp
// Bad
// Check if the player can afford and has inventory space
if (player.Gold >= item.Price && player.Inventory.Count < player.MaxSlots)

// Good
if (CanPurchase(player, item))
```

### Keep methods short

Aim for **10-20 lines**. If a method exceeds 30 lines, it almost certainly does more than one thing.

---

## 3. Reduce Nesting, Linearize Flow

Deeply nested code forces the reader to maintain a mental stack. Flatten it.

### Guard clauses — exit early, indent less

```csharp
// Bad — nested happy path
void TakeDamage(float amount)
{
    if (isAlive)
    {
        if (amount > 0)
        {
            if (_shield == false)
            {
                _health -= amount;
                if (_health <= 0)
                {
                    Die();
                }
            }
        }
    }
}

// Good — guards at the top, happy path at indent level 1
void TakeDamage(float amount)
{
    if (isAlive == false) return;
    if (amount <= 0) return;
    if (_shield) return;

    _health -= amount;

    if (_health <= 0)
    {
        Die();
    }
}
```

### One thing per `if`

Don't chain unrelated conditions. Split into guard clauses or named booleans:

```csharp
// Bad
if (player != null && player.IsAlive && player.Team == myTeam && Vector3.Distance(player.Position, transform.position) < _healRange)

// Good
if (player == null) return;
if (player.IsAlive == false) return;
if (player.Team != myTeam) return;
if (IsInHealRange(player) == false) return;
```

---

## 4. Explaining Variables

When an expression is complex, assign it to a well-named variable even if it's only used once. The variable name serves as documentation:

```csharp
// Bad
if (Vector3.Dot(transform.forward, (target.position - transform.position).normalized) > 0.7f && Physics.Linecast(transform.position, target.position, out _, _obstacleMask) == false)

// Good
bool isFacingTarget = Vector3.Dot(transform.forward, directionToTarget) > ConeThreshold;
bool hasLineOfSight = Physics.Linecast(origin, target.position, out _, _obstacleMask) == false;

if (isFacingTarget && hasLineOfSight)
```

---

## 5. Consistent Patterns

### Symmetry in naming

If you have `Enable()`, pair it with `Disable()`, not `TurnOff()`. If one event is `OnStarted`, the other is `OnStopped`, not `OnFinished` (unless there's a semantic reason).

### Symmetry in structure

Related methods should have the same shape:

```csharp
// Good — parallel structure is easy to scan
void OnEnable()  { _events.OnDamage += HandleDamage; }
void OnDisable() { _events.OnDamage -= HandleDamage; }
```

### Predictable parameter order

Follow a consistent convention: `(subject, action, context)` or `(what, where, options)`. Don't shuffle parameter order across similar methods.

---

## 6. Eliminate Ambiguity

### No double negatives

```csharp
// Bad
if (isNotInvalid == false)
if (!isDisabled)

// Good
if (isValid)
if (isEnabled)
```

### No boolean parameters

A bare `true`/`false` at the call site tells the reader nothing:

```csharp
// Bad — what does 'true' mean here?
SpawnItem(itemData, position, true);

// Good — named argument or separate methods
SpawnItem(itemData, position, networked: true);
// or
SpawnNetworkedItem(itemData, position);
```

### Magic numbers → named constants

```csharp
// Bad
if (distance < 2.5f)
yield return new WaitForSeconds(0.3f);

// Good
private const float PickupRange = 2.5f;
private const float RespawnDelay = 0.3f;
```

---

## 7. Whitespace as Punctuation

Use blank lines to separate logical paragraphs within a method, the same way you'd separate paragraphs in prose. Group related statements, separate unrelated ones:

```csharp
void Initialize()
{
    _health = _config.MaxHealth;
    _shield = _config.StartingShield;
    _stamina = _config.MaxStamina;

    _weaponSlots = new Weapon[_config.MaxWeapons];
    _activeWeaponIndex = 0;

    RegisterCallbacks();
    SyncWithNetwork();
}
```

No blank line between tightly related lines. One blank line between logical groups. Two blank lines between nothing — one is always enough.

---

## 8. Keep Debug Code Extremely Low on PRs

Product / performance / feature PRs must stay review-focused. **Do not ship temporary debug, profiler, or timing scaffolding in the same PR as the fix.**

### Do not commit unless the user explicitly asks

**Never stage or commit debug / measurement scaffolding unless the user directly instructs you to include it.** Default action: leave it unstaged, strip it, or delete it before commit — even if it helped gather before/after evidence in the same session.

That includes:
- One-off Profiler / load-timing probes, capture menus, or Editor “Arm Capture” tooling
- Production hooks whose only purpose is that probe (one-off load timers, reflection into private fields, etc.)
- Extra `Debug.Log` / spammy logging added while investigating
- `#if UNITY_EDITOR` helpers that exist solely for a single measurement session
- Commented-out experiments, leftover `TODO: remove`, or “temporary” MonoBehaviours left in scenes/prefabs

If the user says “commit” without mentioning the debug files, **exclude them**. Only include them when told something like “commit the probe too” / “keep the timing tool in the PR.”

### Allowed (keep minimal)

- Reusing **existing** project log gates (e.g. a shared `LogsEnabled` flag)
- Tiny, permanent DeveloperTool utilities only when the team already owns that pattern **and** the change is intentional product tooling — not session scaffolding
- Evidence belongs in Profiler screenshots / Notion / PR description — **not** in committed debug code

### Rule of thumb

If it was added to *measure* the change, it does not belong in the commit that *implements* the change — unless the user **directly** asks to commit it. Capture locally, then strip before commit / push.

---

## 9. Surgical Edits — Minimize the Diff

Change the fewest lines needed to achieve the goal. A small, targeted diff is faster to review, safer to merge, and easier to bisect when something breaks. Restructuring a whole file to make one fix buries the real change in noise.

### Replace only what needs to change

- Edit the specific lines involved. **Do not** rewrite, reorder, or reformat surrounding code that the task didn't touch.
- Preserve existing indentation, spacing, brace style, member ordering, and line endings (LF vs CRLF). Don't let an editor/formatter reflow the file.
- No drive-by renames, no "while I'm here" cleanups, no re-sorting `using`s/imports unless that *is* the task.

```
// Bad — one-line fix delivered as a 400-line reformatted file
// Reviewer can't tell what actually changed.

// Good — the diff shows exactly the lines that implement the fix.
```

### When removing code, remove only its footprint

- Delete the dead block and the members it *solely* used (fields, locals, imports) — nothing more.
- Fold cleanly into surrounding lines; don't re-indent an entire method just because one block left.

### Don't hand-edit tool-owned files

Some files are rewritten by tooling, not by you: package manifests / lockfiles (`Packages/manifest.json`, `packages-lock.json`), `.meta` files, generated assets. If one shows up changed, confirm *what* actually changed (often a single line + line-ending churn) before assuming it was an intended edit — and revert incidental churn.

### If a bigger refactor is genuinely warranted

Do it as a **separate, clearly-scoped change** from the fix — not bundled in. Say so first; don't surprise the reviewer with structural churn inside a bug fix.

---

## Quick Readability Test

Before finishing any code:
- [ ] Can I understand each method from its name alone?
- [ ] Do variable names reveal intent without needing surrounding context?
- [ ] Is the happy path at the lowest indent level?
- [ ] Are complex conditions extracted into named booleans or methods?
- [ ] Do similar things look similar? (symmetry)
- [ ] No double negatives, no unexplained boolean args, no magic numbers?
- [ ] Would a teammate understand this without asking me a question?
- [ ] No temporary debug / profiler / timing scaffolding staged or committed unless the user explicitly asked?
- [ ] Is the diff minimal — only the lines the task needed, with no incidental reformatting or restructuring of untouched code?
