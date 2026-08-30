# Dark Matter: Genesis — Agent Instructions

**If anything here conflicts with `.cursor/rules/` or GDD 5.0, those win.**

## Authority stack

1. `.cursor/rules/` — start with `dark-matter-genesis-core.mdc`
2. `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt` (GDD 5.0)
3. This file (`AGENTS.md`)
4. `.cursor/skills/` (additive only — see `skill-precedence.mdc`)

## Framework & disk truth

- **Engineering standard:** `Assets/_Project/Features/Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md`
- **Communications roadmap:** `Assets/_Project/Features/Communications/Documentation/Dark_Matter_Communication_Framework.md`
- **Shipped vs planned:** `Assets/_Project/Documentation/Architecture/World_Engine_Disk_Status.md`

## Agent workflow (summary)

- Code under `Assets/_Project/`; reuse existing systems.
- Wait for Unity compile/domain reload; no commit while console has errors.
- Stage scenes, prefabs, materials, terrain, and new assets with related script commits.
- No silent git/depot restore without explicit user confirmation in the current message.
- No forced Unity refresh unless the user asks.

Full locks (platforms, AC economy, Echoes, thermal, BCP, DM naming, UI palette) live in `.cursor/rules/dark-matter-genesis-core.mdc` and GDD 5.0.
