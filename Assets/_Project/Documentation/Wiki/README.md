# Dark Matter: Genesis — documentation wiki (repo mirror)

GitHub wiki home: [Dark-Matter wiki](https://github.com/LittleWormOnAHook/Dark-Matter/wiki).

This folder holds **markdown mirrors** of wiki pages so agents and git track design/engineering canon beside `Assets/_Project/Documentation/`. **Repo markdown is source of truth** — when you change a page here, sync the matching GitHub wiki page (web editor or wiki git clone).

**Documentation discovery:** start at [`../INDEX.md`](../INDEX.md) (§1b combat, §2.1–§2.19 systems, §4 Features). **Rules:** [`.cursor/rules/INDEX.md`](../../../../.cursor/rules/INDEX.md).

## Mirror map

| GitHub wiki page | Repo file | Canonical / master copy |
|---|---|---|
| **Documentation-Index** | `Wiki/Documentation-Index.md` | `Documentation/INDEX.md` |
| **Cursor-Rules-Index** | `Wiki/Cursor-Rules-Index.md` | `.cursor/rules/INDEX.md` |
| **Combat-System** | `Wiki/Combat-System.md` | `Features/Combat/Documentation/Dark_Matter_Combat_System.md` + plan `Design/Combat/DMG_Combat_Plan_v2.md` (**v2.4**) |
| **Combat-Plan** | *(wiki summary optional)* | `Design/Combat/DMG_Combat_Plan_v2.md` (master plan; primary disk copy) |
| **Combat — Moving melee layer policy** | `Wiki/Combat-Moving-Melee-Layer-Policy.md` | `Design/Combat/DM_Melee_Locomotion_Layer_Policy.md` |
| **Archive-Policy** | `Wiki/Archive-Policy.md` | `Archive/Historical_Engineering_Notes.md` |
| **Economy-UEA-and-The-Overseer** | `Wiki/Economy-UEA-and-The-Overseer.md` | `Engineering/RENAME_REPORT.md` + GDD 5.0 |

Pages marked **summary** in-repo point at longer canonical files — paste or edit wiki from the repo mirror, then verify links match `INDEX.md`.

## Syncing to GitHub wiki (Anthony)

`gh` was not available in the agent shell; use one of:

1. **Web:** [wiki](https://github.com/LittleWormOnAHook/Dark-Matter/wiki) → edit/create page → title must match **GitHub wiki page** column above → body from repo `Wiki/*.md` (skip the YAML-style header block if you prefer a cleaner wiki home; keep the Summary section).
2. **Wiki git (recommended for bulk sync):**
   ```powershell
   git clone https://github.com/LittleWormOnAHook/Dark-Matter.wiki.git
   cd Dark-Matter.wiki
   # Copy bodies from repo: Assets/_Project/Documentation/Wiki/<Page>.md → <Page>.md
   git add .
   git commit -m "Sync wiki mirrors from Documentation/Wiki (Oct 2026 index pass)"
   git push
   ```
3. **GitHub CLI** (when installed): push individual pages or use `gh api` / extensions — same content as step 2.

After sync, spot-check: **UEA** / **The Overseer**, scene **v1.6.5**, combat plan **v2.4** (Phase 3 done, Phase 4 next), no stale **Aether Credits** / **Aether-9** / **v1.6.2** playable scene labels.

## Stale-term grep (repo mirrors)

Last sweep **2026-10-08 (v2.4 pass):** no matches in `Documentation/Wiki/` for Aether Credits, Aether-9, AC economy, v1.6.2 scene, or pre-v2.4 combat plan / open Phase 3 acceptance labels. Re-run before large wiki pushes:

```powershell
rg -i "Aether Credits|Aether-9|AC economy|v1\.6\.2" "Assets/_Project/Documentation/Wiki"
```
