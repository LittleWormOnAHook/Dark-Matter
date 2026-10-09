# Economy & lore renames (UEA / The Overseer)

**Wiki page:** `Economy-UEA-and-The-Overseer`  
**Repo report:** `Assets/_Project/Documentation/Engineering/RENAME_REPORT.md` (2026-10-08)  
**GDD canon:** `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt` (Appendix A — economy; A6 — The Overseer)

When editing the [GitHub wiki](https://github.com/LittleWormOnAHook/Dark-Matter/wiki), use **UEA Credits (UEA)** and **The Overseer** — retire wiki text that says **Aether Credits**, **AC economy**, or **Aether-9** for product/lore (unless quoting historical archive).

---

## Player-facing canon

| Old (retired in docs/UI) | New |
|--------------------------|-----|
| Aether Credits / AC (economy) | **UEA Credits** — tight UI may show **UEA** |
| Aether-9 (mystery hub / POI lore) | **The Overseer** (player-facing); in-fiction self-name **Kairos** unchanged in GDD A6 |

**Ship targets:** PC and Mac first; Xbox and PlayStation later. **No** Pi marketplace, wallet, or third-party crypto loop — **UEA only** (GDD 5.0).

**New game design lock (Oct 2026):** **0 UEA**, level **1**, **no** starter inventory; Kade lands with **2 Mars-survivor companions** (starter companion pick retired). Code may still expose test values until release reset — see GDD / INDEX §5.

## What did *not* rename (compatibility)

Save JSON field **`aetherCredits`**, methods like `GrantAetherCredits`, serialized `acCost` / `acValue` on items, and legacy UITK type names (`DMUiToolkitAcReward`) — **display strings updated**; internal identifiers kept to avoid breaking saves/assets. Optional code rename pass documented in `RENAME_REPORT.md`.

**Skips:** Vendor packages (`Invector`, etc.); **Malbers AC** = animation controller bones, not currency; protected `Design/ArtReference/` life sheets.

## Wiki / doc hygiene

- Playable scene: **`Dark Matter Genesis v1.6.5.unity`** (not v1.6.2).
- Combat planning: **`DMG_Combat_Plan_v2.md` v2.4** (Phase 3 accepted Oct 8, 2026; Phase 4 Combat Director next — not older v2.2/v2.3 gate labels).

Full file lists, ambiguous strings, and owner follow-ups → **`RENAME_REPORT.md`**.
