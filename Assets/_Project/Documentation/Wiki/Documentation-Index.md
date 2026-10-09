# Documentation index (agent entry point)

**Wiki page:** `Documentation-Index`  
**Repo canonical copy:** `Assets/_Project/Documentation/INDEX.md`  
**Rules companion:** `Wiki/Cursor-Rules-Index.md` → `.cursor/rules/INDEX.md`

When editing the [GitHub wiki](https://github.com/LittleWormOnAHook/Dark-Matter/wiki), sync from this file and the canonical `INDEX.md` so disk truth and wiki stay aligned.

---

## Summary (Oct 8, 2026)

One scannable map of **all** system docs under `Assets/_Project/Documentation/` and `Assets/_Project/Features/`. **Disk truth** for what is shipped: `Architecture/World_Engine_Disk_Status.md`.

| Section | What it covers |
|---------|----------------|
| **§1 Authority stack** | Rules → GDD 5.0 → AGENTS.md → Framework Standard → disk status |
| **§1b Combat quick path** | Master plan **v2.4** (Phase 3 done, Phase 4 next), Features combat doc, handoffs, melee animation stack |
| **§2.1–§2.19** | Per-system Primary + Related doc lists (Survival, Player, Combat, Building, …) |
| **§3 Plans vs shipped** | What is on disk vs plan-only |
| **§4 Features modules** | Communications, Climb, Combat, Directors, GameState, PPT, Validation, WorldState, … |
| **§5 Archive / wiki / drift** | Archive policy, wiki mirror map, owner-confirmed canon locks |

**Playable scene (Oct 2026):** `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity` — do not use stale **v1.6.2** labels in new docs.

## Index-first discovery (agents)

Before non-trivial system work:

1. Open **`Documentation/INDEX.md`** → find the system **§2.x** row → read **Primary**, then **Related**.
2. **Combat planning / acceptance:** **§1b** + `Design/Combat/DMG_Combat_Plan_v2.md` (**v2.4**).
3. **Which `.mdc` rule applies:** `.cursor/rules/INDEX.md` (23 rules; not a substitute for system docs).
4. **Engineering locks:** `AGENTS.md` + `dark-matter-genesis-core.mdc`.

Full tables and paths → **canonical** `Documentation/INDEX.md` in the repo.
