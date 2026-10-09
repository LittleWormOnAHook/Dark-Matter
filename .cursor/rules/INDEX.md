# Dark Matter: Genesis — Cursor Rules Index

> **Status:** Living index (Oct 8, 2026). **23** rule files (`.mdc` only; no `.md` siblings).
> **Authority:** On conflict, **`.cursor/rules/` wins** over GDD 5.0, `AGENTS.md`, imported skills, and generic Cursor/plugin guidance (`skill-precedence.mdc`, `AGENTS.md` header).
> **Related:** Game-content canon → [`Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt`](../Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt); **all system docs (complete per-system lists)** → [`Assets/_Project/Documentation/INDEX.md`](../Assets/_Project/Documentation/INDEX.md) **§2.1–§2.19** (+ Features **§4**); combat quick path **§1b** + [`Design/Combat/DMG_Combat_Plan_v2.md`](../Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md) **v2.4**; agent workflow summary → [`AGENTS.md`](../AGENTS.md).

## 1. Authority stack (read order)

**Documentation discovery:** For system work, start at [`Documentation/INDEX.md`](../Assets/_Project/Documentation/INDEX.md) (**§2.x** Primary → Related; combat **§1b**). Use **this file** when you need which **`.mdc` rule** applies — not as a substitute for system docs.

Use this when two sources disagree. **Do not delete or move rules** — update canon here and in the rule file, then cross-link.

| Priority | Source | Rule / doc |
|---:|---|---|
| 1 | Always-applied `.cursor/rules/*.mdc` | Start with `dark-matter-genesis-core.mdc` |
| 2 | `AGENTS.md` + Framework Engineering Standard | `Assets/_Project/Features/Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md` |
| 3 | GDD 5.0 + GDD rule | `GAME_DESIGN_DOCUMENT_5.0.txt`, `dark-matter-genesis-gdd.mdc` |
| 4 | Imported skills | `.cursor/skills/` — additive only (`skill-precedence.mdc`) |
| 5 | Disk truth (implementation) | `Documentation/Architecture/World_Engine_Disk_Status.md` |

**Nuances:** `dark-matter-genesis-core.mdc` lists GDD before `AGENTS.md`; `skill-precedence.mdc` lists `AGENTS.md` before GDD. Both agree **project rules beat skills** and **rules beat ad-hoc templates**. For game design vs engineering locks, use GDD 5.0 for content and **always-applied rules** for engineering non-negotiables (NavMesh, Player_v7, UITK, git restore, etc.).

**Application mode**

- **Always-applied (14):** injected every agent session — see §2 tables marked **Always**.
- **Agent-requestable (9):** loaded when relevant (globs, topic, or user task) — marked **On demand**.

## 2. Rules by topic (A→Z within group)

### Art & design protection

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `protect-design-art-reference.mdc` | Always | Touching `Design/ArtReference/`, LifeSheets, Io biome art | Never delete/move/empty LifeSheets without explicit user approval; commit `.meta` with art | Documentation INDEX art lock |

### Building (stone mode)

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `dark-matter-genesis-building-studio.mdc` | Always | Any `DMBuilding*` / stone placement / ghost / door / snap change | Profile on `DMBuildingGhostProfile`; update Building Studio + Genesis Building subtabs; `Profile.Live` at runtime | `dark-matter-genesis-genesis-studio.mdc` |

### Core product & workflow hub

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `dark-matter-genesis-core.mdc` | Always | Every session (canonical locks) | PC+Mac first; AC only; no NavMesh; no in-game LLM; scene v1.6.5; UITK; Player_v7; git source of truth | Links most other rules |
| `skill-precedence.mdc` | Always | Imported skills vs project guidance | Skills additive; rules override badjano/generic templates | `unity-agent-workflow.mdc`, core |

### DCC — 3ds Max (on demand)

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `3dsmax-aaa-asset-pipeline.mdc` | On demand | AAA hard-surface / creature / env in 3ds Max | Sub-D high, remesh low, PBR bake, mechanical rig | Other 3dsmax rules |
| `3dsmax-image-to-3d-decomposition.mdc` | On demand | Image-to-3D blockouts in Max | Anti-primitive compounds, ProBoolean, silhouette carving | `3dsmax-technical-modeling.mdc` |
| `3dsmax-technical-modeling.mdc` | On demand | MAXScript/Python procedural modeling in Max | Expert TA patterns for scene generation | — |

### DCC — Blender (on demand)

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `blender-aaa-scifi-pipeline.mdc` | On demand | AAA sci-fi automation, Voxel Fusion | Hard-surface, retopo/UV, mechanical rig; textures only when asked | `blender-modeling-pipeline.mdc` |
| `blender-modeling-pipeline.mdc` | On demand | Blender MCP modeling for DMG | Geometry-first, Unity-oriented exports | Points to `blender-aaa-scifi-pipeline.mdc` |

### Editor menus

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `dark-matter-genesis-editor-menu-order.mdc` | On demand | `Assets/_Project/Editor/**/*.cs` menu items | Single root `Tools/Dark Matter Genesis/`; A→Z siblings; `DarkMatterGenesisMenuPriority` | — |

### GDD & game design canon

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `dark-matter-genesis-gdd.mdc` | On demand | Game design, economy, roster, BCP, Echoes | GDD 5.0 file only; 0 UEA / Lv1 / 2 Mars companions; thermal single meter; underground later | `dark-matter-genesis-unity-hdrp.mdc` |

### Genesis Studio & profiles

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `dark-matter-genesis-genesis-studio.mdc` | Always | New SO profile / adjuster / tunable subsystem | Register in `DMStudioRegistry`; section filters; `playModeSave` when needed | `dark-matter-genesis-building-studio.mdc` |
| `dark-matter-genesis-studio-system-edit-recall.mdc` | Always | Non-trivial studio or profile-registration edits | Git/docs/transcript recall; refresh Unity; zero console errors before done | `unity-agent-workflow.mdc` |

### Git, depot & cloud agents

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `confirm-before-depot-restore.mdc` | Always | checkout/reset/Plastic/recovery scene | Named save point + user confirm; never silent `_Recovery` as working scene | `cloud-agent-unity-safeguards.mdc` |
| `cloud-agent-unity-safeguards.mdc` | Always | git checkout/merge/commit on live Unity folder | Run `Tools/DmUnityGitSafeguard.ps1`; no commit with console errors | `confirm-before-depot-restore.mdc`, `unity-agent-workflow.mdc` |

### Icons & naming

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `dmg-illustrated-item-icons.mdc` | On demand | New `ItemData`, blueprints, inventory pickups | Illustrated DMG icons mandatory | — |
| `dm-naming-no-invector.mdc` | Always | New scripts, prefabs, repurposed systems | DM/DMI naming; no new Invector-branded identifiers in `_Project` | UI toolkit rule |

### Player physics & climb

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `dark-matter-genesis-player-physics.mdc` | Always | Climb, capsule, jetpack, death retry | Do not retune `Player_v7`; climb layer 23; restore solid capsule + non-kinematic RB | `dark-matter-genesis-core.mdc` |

### UI / UITK

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `dark-matter-genesis-uitk-lock.mdc` | Always | Any new UI surface | UITK only; no new uGUI; Hot Cross → `HotCrossIcons` only | `dark-matter-genesis-ui-toolkit.mdc` |
| `dark-matter-genesis-ui-toolkit.mdc` | Always | Implementing HUD, menus, overlays | UXML/USS/`DMUiToolkit*`; reuse hosts; no second HUD `UIDocument` | `dark-matter-genesis-ui-palette.mdc`, uitk-lock |
| `dark-matter-genesis-ui-palette.mdc` | Always | Colors for any UI | `DarkMatterGenesisUiPalette`; map POI colors; no legacy cyan accent | UI toolkit rule |

### Unity engineering (HDRP)

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `dark-matter-genesis-unity-hdrp.mdc` | On demand | `_Project` gameplay/engineering work | Unity 6 HDRP 6000.4.11f1; no NavMesh; PC+Mac first; companion anims paused | `dark-matter-genesis-gdd.mdc`, player-physics; combat → Documentation INDEX §1b, `DMG_Combat_Plan_v2.md` (v2.4) |

### Unity agent workflow

| Rule file | Mode | When agents must read it | Key locks | Related |
|---|---|---|---|---|
| `unity-agent-workflow.mdc` | Always | After `Assets/` edits, before commit | Auto Refresh off → MCP `refresh_unity`; wait compile; `read_console` errors; stage content assets | Referenced by core, cloud safeguards, studio recall |

## 3. Overlap & duplication (index only — no rule merges)

| Cluster | Files | Note |
|---|---|---|
| UITK | `uitk-lock`, `ui-toolkit`, `ui-palette` | Lock is hard gate; toolkit is how-to; palette is colors. Read all three for new UI. |
| Studios | `genesis-studio`, `building-studio`, `studio-system-edit-recall` | Registration vs building-specific vs edit discipline. |
| Workflow / git | `unity-agent-workflow`, `cloud-agent-unity-safeguards`, `confirm-before-depot-restore` | Refresh/console vs live-folder git vs restore consent. |
| Canon | `core`, `gdd`, `unity-hdrp` | Core = always-on locks; gdd = content; hdrp = engineering patterns on demand. |
| Blender | `blender-modeling-pipeline`, `blender-aaa-scifi-pipeline` | General MCP path vs AAA sci-fi automation detail. |
| 3ds Max | three `3dsmax-*` rules | Split by pipeline stage; all on demand. |

## 4. Recently aligned (Oct 8, 2026)

- **Economy / roster:** GDD rule + GDD 5.0 — **0 UEA**, level 1, **2 Mars-survivor companions**, starter pick retired (see Documentation INDEX §5).
- **Playable scene:** `Dark Matter Genesis v1.6.5.unity` in `core`, `unity-agent-workflow`, `AGENTS.md`; **`confirm-before-depot-restore.mdc`** updated from stale **v1.6.2** labels this pass.
- **Platforms:** PC and Mac first; Xbox and PlayStation later; no mobile ship target (`core`, `gdd`, `unity-hdrp`).

## 5. Git last touch (optional reference)

Most recent rule commits on disk (not uncommitted edits): **Oct 7, 2026** — `building-studio`, `studio-system-edit-recall`, `skill-precedence`. **Oct 4, 2026** — `core`, `unity-agent-workflow`, `cloud-agent-unity-safeguards`. **`dark-matter-genesis-editor-menu-order.mdc`** — untracked/new (no git history yet). DCC rules mostly **Aug–Sep 2026**.

## 6. Remaining drifts to watch

| Item | Status |
|---|---|
| Scene v1.6.2 in rules | **Fixed** in `confirm-before-depot-restore.mdc` (Oct 8, 2026) |
| `5000 UEA` / PC-only in rules | **None** in `.cursor/rules/` (grep Oct 8, 2026) |
| `skill-precedence` "PC+consoles" shorthand | Informal label; canon is PC+Mac first (`core`, `gdd`) |
| Authority: GDD vs AGENTS order | Documented in §1 — both subordinate to always-applied rules |
