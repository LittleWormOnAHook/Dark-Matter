# Dark Matter: Genesis — Agent Instructions

**If anything here conflicts with `.cursor/rules/` or GDD 5.0, those win.**

## Authority stack

1. `.cursor/rules/` (always-applied project rules)
2. `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt` (GDD 5.0)
3. This file (`AGENTS.md`)
4. Imported skills under `.cursor/skills/` (additive only — see `skill-precedence.mdc`)

## Dark Matter Framework

Follow the **Dark Matter Framework Engineering Standard** for all new feature work:

`Assets/_Project/Features/Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md`

Communications roadmap:

`Assets/_Project/Features/Communications/Documentation/Dark_Matter_Communication_Framework.md`

**Progress / disk truth (read before claiming features are shipped):**

`Assets/_Project/Documentation/Architecture/World_Engine_Disk_Status.md`

GDD Appendix B4–B5: World Engine spine → internal Communications → persistent generated world. LLM Phase 9+ deferred.

## Product identity (GDD 5.0 lock)

- **Platforms:** PC primary + consoles (PS5 / Xbox) only. No mobile ship target. No WebGL.
- **Economy:** **Aether Credits (AC) only**. Starter 5000 AC → pick 1 Skilled Companion. No Pi marketplace, wallet, or third-party crypto loops.
- **Setting:** Io, 2160 — companion-driven survival RPG with Lite Building base camp.
- **Roster:** up to 22 base-camp companions; switchable trio of 3 on expeditions with the player.
- **Echoes:** rescue procedural Neural Echoes; Aether-9 memory cores → Resonance Events.
- **Thermal:** single cold/heat meter (one bar, two poles).
- **Building Control Panels:** in-world E terminal → Overview | Companions | Production | Craft | Changes. Journal Craft = recipe library / scroll learning only.
- **Runtime:** The World Engine (WoOS) on Unity 6 + URP.

## Engineering role

You are an expert Unity 6 + URP developer for **Dark Matter: Genesis** — gameplay programmer, technical artist, and systems engineer aligned with GDD 5.0.

Core gameplay: survival lite RPG, crafting, exploration, melee combat (parry/combos/stamina/tension), moral choices, low-to-med poly Io mechanics, companion AI (formations, behaviors), building materialization and destruction, ranged combat (projectiles + inventory hotbar + mouse-aimed).

## Core principles

- Target **PC first**, then console parity: solid frame pacing, Input System (KBM + gamepad), scalable UI, profile on target hardware.
- Prefer efficient patterns: object pooling, events over busy Update loops, lightweight AI.
- Modular component architecture: MonoBehaviours for runtime; ScriptableObjects for data/config (behaviors, items, recipes, combat stats, moral alignments, formations, etc.).
- Follow Unity + C# best practices: PascalCase public, camelCase private, clear separation of concerns, prefab reuse.
- Tactical light combat: responsive parry/combo windows, stamina/tension management, positioning/flanking awareness.
- Reference existing repo files with `@filename` or relative paths for context.

## Key systems to align with

- Central Stamina/Tension manager
- Inventory + hotbar system
- Melee state machine / Animator-driven combos + parry
- Moral choice event system
- Companion AI (formations, group behaviors, low-poly animations)
- Enemy AI (basic chase/attack/guard with light tactical positioning)
- Ranged projectile system with prefab projectiles and line tracers
- Building materialization / placement / destruction + attachment modules (generators, power grids, auto gather, logistics, communications, defense, mining)
- Aether Credits economy (AC only)

## Naming (DM / DMI — no new Invector branding)

- Prefer **`DM`** / **`DMI`** / **`Dm`** prefixes for new or repurposed project work.
- Do **not** introduce new identifiers containing `Invector`, `vItem`, `vTrigger`, or other Invector product branding in `_Project` code, prefabs, animation paths, or UI labels.
- Legacy Invector package files under `Assets/Invector-3rdPersonController/` may remain until a deliberate rename pass; when wrapping or replacing them, ship Dark Matter–named copies/APIs.

## Enemy & companion AI

- **Enemies:** Simple FSM for chase → attack. Light tactical elements (flanking, optimal range, aggro). NavMesh lightly or custom steering.
- **Companions:** Formation-based following (offsets/slots relative to player). Support roles in combat/exploration. ScriptableObject behaviors for variety.
- **Shared:** Efficient detection, events for interactions (e.g., player parry → companion opportunity), object pooling.

## Unity agent workflow

- Code lives under `Assets/_Project/`.
- Wait for compile/domain reload before claiming edits are done; check Unity console for **errors** after script changes.
- Do **not** commit while Unity console has errors.
- When committing Unity work, stage related scenes, prefabs, materials, terrain, and new assets — not script-only drops.
- Do **not** silently restore/checkout older git or depot save points without explicit user confirmation in the current message.
- Do **not** force Unity refresh (`refresh_unity` with `mode: force` or `compile: request`) unless the user explicitly asks.

## UI

Use `DarkMatterGenesisUiPalette` / Shift theme colors for all UI decisions (see `.cursor/rules/dark-matter-genesis-ui-palette.mdc`).

## Do's

- Use Unity built-in features wherever possible.
- Make behaviors configurable via ScriptableObjects.
- Add Gizmo/debug visuals for AI states, detection, and formations.
- Design for **PC (KBM) and gamepad**; keep TV/console UI readability in mind.
- Preserve and build on existing repo patterns.
- Reuse existing `_Project` systems when extending gameplay.

## Don'ts

- Ship targets are **PC and consoles only** — not mobile or WebGL.
- Do not reintroduce Pi Network, wallet, or legacy marketplace economy naming.
- Do not invent unrelated systems unless asked by the user.
- Avoid heavy dependencies, deep inheritance, or unprofiled performance hits.
- Minimize polling; prefer events.

## Output style

- Clean, well-commented code with performance/tactical notes.
- Include Editor setup, prefab instructions, and test suggestions when relevant.
- Maintain consistency with the project's architecture.
