# Dark Matter: Genesis — Agent Instructions

**If anything here conflicts with `.cursor/rules/` or GDD 5.0, those win.**

## Authority stack

1. `.cursor/rules/` — start with `dark-matter-genesis-core.mdc`
2. `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt` (GDD 5.0)
3. This file (`AGENTS.md`)
4. `.cursor/skills/` (additive only — see `skill-precedence.mdc`)

## Live editor (Sep 2026)

- **Unity 6 HDRP** `6000.4.11f1` (the `unity-urp` rule filename is leftover — do not target URP)
- Playable scene: `Assets/_Project/Scenes/Dark Matter Genesis v1.6.2.unity`
- Git branch for UITK cutover: `cursor/uitoolkit-ui`
- Editor **Auto Refresh is off**. After script/asset edits, Anthony Ctrl+R / Assets → Refresh. Do not force-refresh unless asked.
- Git is agent source of truth. Plastic check-in is Anthony in the Plastic window (no `cm` CLI).
- Do not clone this repo onto the agent box. Work the live project folder Unity has open (Play uses that path).
- Do not add untracked L.V.E, mocap packs, `UIElementsSchema`, PlanetPack02, OlegWER, or GDKEditionAutoGen.

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
- **No NavMesh** (baking, NavMeshSurface, NavMeshAgent, terrain NavMesh refs).
- **Do not retune `Player_v7`** capsule, layers, or physics. See `dark-matter-genesis-player-physics.mdc`.

**New UI:** UI Toolkit only (UXML/USS/`DMUiToolkit*` runtime) — see `.cursor/rules/dark-matter-genesis-ui-toolkit.mdc`. Do not add new uGUI menus or HUD panels unless explicitly requested.

Full locks (platforms, AC economy, Echoes, thermal, BCP, DM naming, UI palette) live in `.cursor/rules/dark-matter-genesis-core.mdc` and GDD 5.0.
