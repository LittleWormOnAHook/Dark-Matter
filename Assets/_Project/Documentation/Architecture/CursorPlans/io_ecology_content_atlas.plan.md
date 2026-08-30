---
name: Io Ecology Content Atlas
overview: Re-open the six Io Design docs and build a Cursor digest canvas (biomes × flora × fauna × pets × threats) from the mobile-agent ecology package already on main.
todos:
  - id: open-design-suite
    content: Re-open all six Io Design markdown docs in the editor
    status: pending
  - id: digest-canvas
    content: Build io-ecology-digest.canvas.tsx with biome matrix, threat families, pets, W0–W8 phases
    status: pending
  - id: life-image-sheets
    content: "See Io_Biome_Life_Image_Sheet_Plan.md — manifest + 10 PNG contact sheets"
    status: pending
  - id: w0-stub-later
    content: "Out of scope this pass: W0 ScriptableObject stubs"
    status: cancelled
isProject: false
---

# Re-open Io Design docs + digest canvas

## Goal

Surface the mobile agent’s Io world content package for review:

1. **Re-open** all six Design markdown docs in the editor.
2. **Build a digest canvas** (included — not deferred) summarizing biomes, plants, lifeforms, enemies, pets, and W0–W8 phasing.

No Unity systems, no GDD rewrite this pass.

## Step 1 — Re-open docs

Open via `open_resource` (file URIs):

| Doc | Path |
|-----|------|
| Executive Summary | [`Assets/_Project/Documentation/Design/Io_World_Content_Executive_Summary.md`](Assets/_Project/Documentation/Design/Io_World_Content_Executive_Summary.md) |
| Phase Map | [`Assets/_Project/Documentation/Design/Io_World_Content_Phase_Map.md`](Assets/_Project/Documentation/Design/Io_World_Content_Phase_Map.md) |
| Milestone Tickets | [`Assets/_Project/Documentation/Design/Io_World_Content_Milestone_Tickets.md`](Assets/_Project/Documentation/Design/Io_World_Content_Milestone_Tickets.md) |
| Ecology Roster | [`Assets/_Project/Documentation/Design/Io_Biome_Ecology_Roster.md`](Assets/_Project/Documentation/Design/Io_Biome_Ecology_Roster.md) |
| Biome Exploration | [`Assets/_Project/Documentation/Design/Io_Biome_Exploration_Gameplay_Plan.md`](Assets/_Project/Documentation/Design/Io_Biome_Exploration_Gameplay_Plan.md) |
| Underground Architecture | [`Assets/_Project/Documentation/Design/Io_Underground_Architecture_Plan.md`](Assets/_Project/Documentation/Design/Io_Underground_Architecture_Plan.md) |

## Step 2 — Digest canvas

**File:** `C:/Users/Teabagger/.cursor/projects/a-Survival-Pioneer/canvases/io-ecology-digest.canvas.tsx`

Follow Cursor canvas skill (`cursor/canvas` only; theme tokens; no gradients/emojis).

### Canvas sections (inline data from roster + phase map)

1. **Header** — Io world content digest; source docs + commit `497a1fda`; fantasy line (chemosynthetic / sulfur-silicon / resonance-fed).
2. **Biome matrix table** — B1–B7 rows: dominant pressure, signature flora (2–3), signature fauna (2–3), machine/android hook, campaign unlock order note.
3. **Underground strata table** — S1–S5: feel, anchor flora, anchor fauna, surface pairing.
4. **Threat families** — compact counts/lists: Androids A1–A10, Humanoids H1–H7, Machines M1–M7, Flyers F1–F8, Void Stitcher callout.
5. **Pets** — Core 12 table (name / type / acquisition / biome / role) + vanity extras (V1–V4); note retire Ricky/Probe/Fox Cub.
6. **Phase strip** — W0→W8 one-line outcomes (from executive summary).
7. **Caption** — Links to the six markdown paths; “design investigation — Unity deferred to W0+”.

Pull concrete names from [`Io_Biome_Ecology_Roster.md`](Assets/_Project/Documentation/Design/Io_Biome_Ecology_Roster.md) and [`Io_World_Content_Phase_Map.md`](Assets/_Project/Documentation/Design/Io_World_Content_Phase_Map.md) §1 (already on disk — no inventing new organisms).

## Out of scope

- W0 ScriptableObject stubs
- Editing design markdown content
- Commit/push (unless you ask after review)

## Acceptance

- All six docs open in the IDE.
- Digest canvas file exists and renders biome/threat/pet/phase tables with real roster data.
