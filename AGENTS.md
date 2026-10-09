# Dark Matter: Genesis — Agent Instructions

**If anything here conflicts with `.cursor/rules/` or GDD 5.0, those win.**

## Authority stack

1. `.cursor/rules/` — start with `dark-matter-genesis-core.mdc`
2. `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt` (GDD 5.0)
3. This file (`AGENTS.md`)
4. `.cursor/skills/` (additive only — see `skill-precedence.mdc`)

## Live editor (Sep 2026)

- **Unity 6 HDRP** `6000.4.11f1` (the `unity-urp` rule filename is leftover — do not target URP)
- Playable scene: `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity` (main scene, latest v1.6)
- Git branch for UITK cutover: `cursor/uitoolkit-ui`
- Editor **Auto Refresh is off**. After script/asset edits, agent runs MCP `refresh_unity` (`if_dirty`, `compile: request` when scripts change, `wait_for_ready: true`). Anthony Ctrl+R only if MCP is unavailable.
- Git is agent source of truth. Plastic check-in is Anthony in the Plastic window (no `cm` CLI).
- Do not clone this repo onto the agent box. Work the live project folder Unity has open (Play uses that path).
- Do not add untracked L.V.E, mocap packs, `UIElementsSchema`, PlanetPack02, OlegWER, or GDKEditionAutoGen. Policy: `Assets/_Project/Documentation/Engineering/Vendor_Assets_And_Git_Policy.md`.
- **Disk = truth / commit+Unity sync:** `Assets/_Project/Documentation/Engineering/Unity_Git_Disk_Truth.md` (pre-commit checklist + agent paste block).

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
- **Do not retune `Player_v7`** capsule, layers, or physics. **Tune/wire both** `Assets/_Project/Prefabs/Players/Player_v7 Variant.prefab` **and** hierarchy `Player_v7`. See `dark-matter-genesis-player-physics.mdc`.
- **Stone build mode:** any `DMBuilding*` / placement / door / ghost tuning change must update `DMBuildingGhostProfile`, **Building Studio** (`Tools/Dark Matter Genesis/Buildings/Building Studio`), and Genesis **Building** subtabs — see `.cursor/rules/dark-matter-genesis-building-studio.mdc`.
- **Studio/system edits:** recall prior worked vs failed attempts (git, docs, transcripts); after changes follow refresh + console checks — `.cursor/rules/dark-matter-genesis-studio-system-edit-recall.mdc`.

**All created UI is UITK only** (UXML/USS/`DMUiToolkit*` runtime) — `.cursor/rules/dark-matter-genesis-uitk-lock.mdc` and `dark-matter-genesis-ui-toolkit.mdc`. No new uGUI. Hot Cross uses `Assets/_Project/Resources/UI/HotCrossIcons` cutouts only.

Full locks (platforms, AC economy, Echoes, thermal, BCP, DM naming, UI palette) live in `.cursor/rules/dark-matter-genesis-core.mdc` and GDD 5.0.

## When you need more detail

**Index-first (before non-trivial system work):**

1. `Assets/_Project/Documentation/INDEX.md` — find the system **§2.1–§2.19** row; read **Primary**, then **Related**. Features modules also **§4**.
2. **Combat planning & acceptance** — INDEX **§1b**; phased roadmap `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md` (**v2.4**); disk authority `Assets/_Project/Features/Combat/Documentation/Dark_Matter_Combat_System.md`.
3. **Rule locks & workflow** — `.cursor/rules/INDEX.md` (all `.mdc` by topic; start with `dark-matter-genesis-core.mdc`).
4. **New plan/handoff under Documentation or Features/Documentation** — update INDEX **§2** (and **§4** if Features) in the same task.

Also:

- Framework standard → `Assets/_Project/Features/Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md`
- Full GDD canon → `.cursor/rules/dark-matter-genesis-gdd.mdc` or GDD 5.0 file
- Unity engineering patterns → `.cursor/rules/dark-matter-genesis-unity-hdrp.mdc`
- Studio/system recall (git, prior outcomes) → `.cursor/rules/dark-matter-genesis-studio-system-edit-recall.mdc`
