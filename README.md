# Dark Matter: Genesis

**Studio:** Dark Matter Studios · **Runtime target:** [The World Engine](Assets/_Project/Documentation/Architecture/Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md) (WoOS) on Unity 6 + HDRP  
**Progress truth:** [World_Engine_Disk_Status.md](Assets/_Project/Documentation/Architecture/World_Engine_Disk_Status.md) — World Engine Run 1 spine (`Features/GameState`, `WorldState`, `Directors`, `Validation`) is on disk; Communications and Experience runtimes are not. Most gameplay lives under `Scripts/`.

| Doc | Path | Role |
|-----|------|------|
| **GDD (authority)** | `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt` | Production bible — Appendices A/B (B4–B5 = roadmap vs disk) |
| Game summary | `Assets/_Project/GAME_SUMMARY.txt` | Optional one-page export |
| Feature breakdown | `Assets/_Project/GAME_BREAKDOWN.txt` | Codebase / scene inventory |
| Architecture | `Assets/_Project/Documentation/Architecture/README.md` | The World Engine / WoOS docs |
| Disk status | `Assets/_Project/Documentation/Architecture/World_Engine_Disk_Status.md` | What exists vs designed |
| Building scope | `Assets/_Project/Documentation/Design/DMG_Building_System_Scope_Plan.md` | Building design + as-built status (section 19) |
| Building tickets | `Assets/_Project/Documentation/Design/DMG_Building_System_Tickets.md` | `BUILD-###` ticket status |

## Implementation status (as of Sep 30, 2026)

Full list: GDD 5.0 **Appendix B6**. Main scene: `Dark Matter Genesis v1.6.5.unity` (latest v1.6). Save version **23**. Platforms: PC and Mac first; Xbox and PlayStation later.

- **Shipped:** Hold-B build mode with UITK build hotbar, Stone / Iron / Silicate piece sets, 4 m snap, Left Alt + scroll rotate, Right Alt finishes, hold-to-build bar, force-field doors, generator + base power, Build Hub zone, storage crates; Genesis Studio; movement (jog / sprint, climb, dash, jetpack, landing); survival + exposure; melee + ranged combat (9 ammo types); UI Toolkit HUD / journal / menus; vendors; skill tree; map + fog of war; Io clock; streamed Gaia terrain tiles with Io textures.
- **In progress:** Steel / Amalgam tiers, skill gates, blueprint unlocks, full materialization; Building Control Panels on real facilities; live weather scheduler; base-22 sim; Mac build checks.
- **Planned:** Combat & AI overhaul (Combat Plan v2, 16 phases — see GDD B6 and the wiki page *Combat-Plan*); Communications runtime; world seed; Io biome world; underground map.

**When asked for the GDD:** use **`Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt`** only. A revision renames that file to the next whole number (`6.0`, then `7.0`). The previous filename does not remain.

