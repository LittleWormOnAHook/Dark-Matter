# Handoff: Grok bot — loot chest rework

**Canonical plan:** [DM_Loot_Chest_System_Plan.md](./DM_Loot_Chest_System_Plan.md) (read fully before coding).

**Decisions (Oct 6, 2026):** Anthony answered the 16 review questions. They are listed in the plan section "Decisions (Oct 6, 2026)" (D1-D16) and override older text. In short: gone chests never respawn; quest and unique items are protected; level-gated items are lootable but locked from use; Loot All takes what fits and a 30-minute hold follows a full inventory; the world fully pauses while the loot window is open; no AC in chests; ammo never auto-loads (Hot Cross stack, then inventory); weapons and tools go to the main grid; tint per type for now; `Invector-Chest.prefab` stays untouched and fixes go in a new DM base prefab; the uGUI loot fallback and old loot panel are removed; save v24 is reserved; no one-shot hint line; the Emergency Crate is a permanent story crate; storage crate controller support is required; pets and companions never loot chests. Follow-up answers (D17-D22): the Emergency Crate teaches "inventory full" by holding more than an empty inventory can take; "locked" uses the vendor blocked-icon overlay (`.dmg-vendor-block`) on loot rows, inventory and Hot Cross, and the same rule covers enemy drops and world pickups; revisiting a chest restarts the 30-minute hold and the normal timer resumes when it ends; prefabs are `DM_Salvage_Cache` and `DM_Story_Crate`; level-locked items stay in the main grid (refused on hotbar, toolbar and Hot Cross) and can be sold; no pickup-level check on chests, enemy drops or world pickups (mining and harvesting keep theirs). The block below has been updated to match.

Copy the block below into the implementing agent if it does not read repo docs.

---

```
TASK: Implement the Dark Matter: Genesis loot chest rework.
Full plan (read first): Assets/_Project/Documentation/Design/Loot/DM_Loot_Chest_System_Plan.md
Project: A:\Dark Matter Genesis (live Unity folder, do not clone). Unity 6 HDRP 6000.4.11f1 (NOT URP).
Scene: Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity. Branch cursor/wip-clean-20260919.

AUTHORITY: .cursor/rules/ > GDD 5.0 > AGENTS.md > skills. Read dark-matter-genesis-core.mdc, unity-agent-workflow.mdc, dark-matter-genesis-studio-system-edit-recall.mdc, dark-matter-genesis-genesis-studio.mdc, dark-matter-genesis-uitk-lock.mdc first.

DECISIONS (Oct 6, 2026) - plan section "Decisions (Oct 6, 2026)" overrides any older text.

HARD RULES
- UI is UI Toolkit only (UXML/USS/DMUiToolkit*). No new uGUI. Palette from DarkMatterGenesisUiPalette.
- DM / DMI names only. No new Invector-branded identifiers.
- No NavMesh. Do NOT edit Invector-Chest.prefab. Remove NavMeshObstacle (fileID 20800156) and the Invector uGUI children in the new DM base prefab (DM_LootChest_Base) via m_RemovedComponents; derive IO_Ancient_Cache and the other chest types from it.
- Do not retune Player_v7. Aether Credits only (QuestRewardType.Pi is legacy name for AC). Chests hold no AC.
- Ammo never auto-loads into weapons (Hot Cross only). Chest and world-pickup ammo goes to a matching Hot Cross stack, then inventory. Chest weapons and tools go to the main grid.
- Shipping start is 0 AC, level 1, empty inventory (200 AC / level 5 / start items are test-only). PC and Mac first.
- Do not git add -A. Do not commit unless the user asks. Never commit with console errors.
- Auto Refresh OFF: after last write MCP refresh_unity (if_dirty, all, compile request, wait_for_ready true), wait for domain reload, read_console errors.
- No per-frame Q() on UITK. Cache BindTree; create up to 25 rows once.

LOCKED DECISIONS — see plan "Decisions (Oct 6, 2026)" and section 2. Summary: four chest modes (WorldReloot, SingleLoot, Story, Storage) told apart by tint; WorldReloot 120s timer (first exit with leftovers, frozen while open); SingleLoot gone 5s after exit (~3.4s dissolve start); quest/unique items protected; 30-min hold after a full-inventory loot; gone chests never respawn; Story crate never expires; Storage permanent with lid + existing crate UI + controller support; per-item Loot + Loot All closes only when emptied; world fully paused while the loot window is open; NotifyWindowClosed on every close path; migrate EnemyLootBag; DM base prefab (Invector prefab untouched); remove uGUI loot fallback and old loot panel once UITK works; save v24; level-locked items use the vendor blocked overlay (plan 8.8); 30-min hold restarts on each revisit; Emergency Crate is DM_Story_Crate and overfills an empty inventory (plan 7.5); SingleLoot prefab DM_Salvage_Cache; level-locked items main grid only, sellable, no pickup-level check except mining/harvesting (plan 7.7, 8.8).

IMPLEMENTATION ORDER — plan section 13.0, phases 1-7 (phase 0 done, phase 8 is backlog). Foundations (profile, Studio, shared grant path, IDMLootContainer) → UITK Loot window (full pause, controller, then remove old loot UI) → DMChestLid + state machine + storage controller → timers/holds + dissolve → save v24 + New Game reset + restore-on-load → DM base prefab and scene → EnemyLootBag hardening.

KNOWN FAILURES — plan section 5 / handoff in chat: no CrossFade on legacy lid; no oneTimeOpen kill on re-loot; no scaled-time lid during pause; registry state wins over the Awake refill from lootSlots (pull in OnEnable); New Game must reset loot chests and storage crates; notify provider on close.

VERIFY — plan section 13.3. Console zero errors. Play tests: per-item, Loot All, Esc re-open, timer pause+dissolve, single 5s, storage under pause, save/load, gamepad, inventory full (30-min hold), death, protected items, level-gated item at level 1, ammo not auto-loaded, New Game reset, Mac build.
Report files changed and what was not verified. Do not commit.
```
