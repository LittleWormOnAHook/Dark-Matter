---
name: Io Biome Ecology Expand
overview: Bring the completed Io biome ecology + world-content documentation from branch origin/cursor/io-biome-ecology-roster-9916 onto main, verify cross-links against current GDD/biome docs, and leave Unity spawn work deferred.
todos:
  - id: merge-ecology-branch
    content: Merge or cherry-pick docs from origin/cursor/io-biome-ecology-roster-9916 onto main (Design package + Microsoft365 exports)
    status: completed
  - id: verify-crosslinks
    content: Verify biome/underground plans point at ecology roster + phase map; fix any main conflicts
    status: completed
  - id: gdd-pointer
    content: Add lightweight GDD/disk-status pointers to A2e/A2f promotion targets without rewriting locked GDD body
    status: completed
  - id: commit-push-docs
    content: Commit documentation package on main and push (console-clean; include .md/.meta/docx per Unity commit rules)
    status: completed
isProject: false
---

# Bring Io biome ecology docs up to date from mobile branch

## Context change

The earlier plan assumed writing [`Io_Biome_Ecology_Roster.md`](Assets/_Project/Documentation/Design/Io_Biome_Ecology_Roster.md) from scratch on `main`. That work **already exists** on:

**`origin/cursor/io-biome-ecology-roster-9916`** (10 commits, tip `2e15fdc7`)

`main` does **not** have these files yet. This plan is now an **integrate / bring-up-to-date** pass, not a rewrite.

## What the mobile branch already contains

| Asset | Role |
|-------|------|
| [`Io_Biome_Ecology_Roster.md`](Assets/_Project/Documentation/Design/Io_Biome_Ecology_Roster.md) | Full B1–B7 + underground cards; migratory species; Void Stitcher; android/humanoid/machine/flyer families; 12-pet + vanity roster |
| Edits to [`Io_Biome_Exploration_Gameplay_Plan.md`](Assets/_Project/Documentation/Design/Io_Biome_Exploration_Gameplay_Plan.md) | Cross-links to ecology roster |
| Edits to [`Io_Underground_Architecture_Plan.md`](Assets/_Project/Documentation/Design/Io_Underground_Architecture_Plan.md) | Cross-links to ecology roster |
| [`Io_World_Content_Phase_Map.md`](Assets/_Project/Documentation/Design/Io_World_Content_Phase_Map.md) | Master W0–W8 production map (biomes + ecology + pets) |
| [`Io_World_Content_Executive_Summary.md`](Assets/_Project/Documentation/Design/Io_World_Content_Executive_Summary.md) | One-page rollup |
| [`Io_World_Content_Milestone_Tickets.md`](Assets/_Project/Documentation/Design/Io_World_Content_Milestone_Tickets.md) | IO-W* tickets |
| [`Microsoft365/`](Assets/_Project/Documentation/Design/Microsoft365/) | `.docx` / `.xlsx` exports + `export_to_office365.py` |

Ecology fantasy on branch matches locks: **chemosynthetic / sulfur-silicon / resonance-fed**; androids ≠ fauna; flat prototype / Unity spawn deferred to GDD B4 #9.

## Execute steps (after approval)

1. **Integrate branch onto `main`**
   - Prefer: `git merge origin/cursor/io-biome-ecology-roster-9916` (docs-only history is clean vs `main` for Design paths).
   - If unrelated conflicts appear outside Design, resolve Design files favoring the ecology branch; keep `main` for unrelated scene/script noise.
   - Alternate if merge is messy: checkout only the Design tree from that branch into `Assets/_Project/Documentation/Design/`.

2. **Verify / lightly patch cross-links**
   - Biome plan wildlife lines → roster section pointers.
   - Underground §6 → “full cards in ecology roster”.
   - Phase map / executive summary / tickets companion table consistency.
   - Confirm no Earth-flora contradictions with GDD 5.0 alien-life lock.

3. **GDD / disk-status pointer (minimal)**
   - Add short Appendix-facing note or `World_Engine_Disk_Status.md` / GDD revision note that **A2e Ecology Roster** and **A2f Phase Map** exist as design investigation docs (do not rewrite locked GDD chapters wholesale unless you ask).

4. **Commit + push on `main`**
   - Gate: Unity console errors = 0.
   - Stage all Design markdown, `.meta`, and Microsoft365 exports (Unity commit-include-assets rule).
   - Exclude `.vscode`, `Assets/_Recovery/`, Quality/ProBuilder settings.
   - Concise message, e.g. *Import Io biome ecology roster and world-content phase docs from mobile branch.*

## Out of scope

- Implementing Unity biomes, ecology prefabs, pet migration, or encounter directors
- Re-authoring organism cards (branch is source of truth unless you request edits)
- Merging unrelated non-doc work from other mobile branches

## Acceptance

- `main` contains the full Design package listed above.
- Cross-links between biome / underground / ecology / phase-map docs resolve.
- `main` pushed to `origin`; remaining local dirt matches prior exclude list.
