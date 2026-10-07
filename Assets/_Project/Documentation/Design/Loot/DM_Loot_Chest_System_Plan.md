# DM Loot Chest System Plan

**Project:** Dark Matter: Genesis (Unity 6 HDRP 6000.4.11f1)
**Status:** PLAN - decisions updated 2026-10-06 (see "Decisions (Oct 6, 2026)"), not yet implemented
**Date:** 2026-10-06 (design review, Anthony's decisions and his follow-up answers folded in the same day)
**Branch:** `cursor/wip-clean-20260919`
**Scope:** `IO_Ancient_Cache` (scene hierarchy + project prefab), new DM base chest prefab, `DMItemCollection` / `DmEvents`, storage chest (including controller support), world re-loot chests, single-loot chest, story crate, shared loot grant path, UITK loot window, `EnemyLootBag` migration, save v24.

**Read first when implementing:** `AGENTS.md`, `.cursor/rules/dark-matter-genesis-core.mdc`, `dark-matter-genesis-uitk-lock.mdc`, `dark-matter-genesis-ui-toolkit.mdc`, `dm-naming-no-invector.mdc`, `dark-matter-genesis-genesis-studio.mdc`, `dark-matter-genesis-studio-system-edit-recall.mdc`, `unity-agent-workflow.mdc`.

---

## Decisions (Oct 6, 2026)

Anthony's answers to the 16 questions from the Oct 6 design review (D1-D16) and to his follow-up questions (D17-D22). They override any older text in this plan, and the sections below have been updated to match. The rest of the doc refers to them as D1-D22.

| # | Topic | Decision |
|---|---|---|
| D1 | Respawn | Chests that are gone stay gone for good. World-placed chests never respawn. Chest loot can only come back through a respawned enemy or boss (its drops). |
| D2 | Quest and unique items | Protected and persistent. A chest holding an `ItemType.Quest` or `ItemRarity.Unique` entry never dissolves or expires until those entries have been taken. |
| D3 | Level-gated items | Lootable at any level. Shown as locked from use until the player reaches the required level. The look is set by D18; placement and selling by D21 and D22. |
| D4 | Loot All with a full inventory | Take whatever fits, in list order, and leave the rest in the chest. The window stays open and usable; nothing is forfeited. If the chest is not fully looted, its dissolve/expiry pauses and the player gets a 30-minute hold to clear inventory and come back to reloot. Hold details are set by D19. |
| D5 | World time while the loot window is open | Full pause, like the storage crate. Not the 0.2 slow motion used today. |
| D6 | AC in chests | No. Chests never hold Aether Credits. |
| D7 | Ammo from chests and world pickups | Never auto-loads into weapons (ammo only loads from Hot Cross slots, an existing lock). It goes to a matching Hot Cross stack first, then the inventory. |
| D8 | Weapons and tools from chests | Always land in the main inventory grid. No auto-slotting into weapon or tool slots. |
| D9 | Telling chest types apart | A colour tint per type is enough for now. Anthony replaces the meshes later himself. |
| D10 | Invector prefab | Leave `Invector-Chest.prefab` untouched. All fixes (NavMeshObstacle removal, uGUI children removal) go in a new DM base chest prefab. |
| D11 | Old loot UI | Once the UITK loot window works, remove the uGUI fallback in `EnemyLootDialogUI` and the old WorldMenus loot panel. |
| D12 | Save version | v24 is reserved for loot chests. Combat Plan v2 takes the next one (v25). |
| D13 | One-shot hint line | Removed. No "Closing destroys this cache" line. |
| D14 | Prologue Emergency Crate | A permanent story crate (`Story` mode, section 7.5). It still teaches "inventory full" (D17). |
| D15 | Storage crate on controller | Required for the PC/Mac release, not deferred. |
| D16 | Pets and companions | Pets auto-loot only nearby pickup items and enemy drop loot, never chests. Companions do not loot chests. |
| D17 | Emergency Crate teaches "inventory full" | Yes. Kept simple: the crate holds more than the empty starting inventory can take, so Loot All fills the free slots and the rest stays in the crate (section 7.5). |
| D18 | What "shown as locked" looks like | Same as the vendor icons: the vendor's blocked-icon overlay (section 8.8). Used on loot rows, inventory slots and Hot Cross icons. The same rule (lootable at any level, shown locked) covers enemy drops and world pickups. |
| D19 | Full-inventory hold details | When the 30-minute hold ends, the chest's normal timer resumes (a SingleLoot chest dissolves 5 s after the hold ends). Revisiting the chest restarts the hold at the full 30 minutes. A refused per-row Loot starts the hold the same way as Loot All. |
| D20 | Prefab names | `DM_Salvage_Cache` for the SingleLoot chest and `DM_Story_Crate` for the Story crate. |
| D21 | Where level-locked items can sit | Main inventory only until the level is reached. Keep the existing refusal on hotbar, toolbar and Hot Cross slots (`EquipmentController.cs:546-565`, `InventorySlotUI.cs:582-584`) and apply it to every level-locked item. No "place anywhere". |
| D22 | Pickup level | No pickup-level check on chests, enemy drops or world pickups: level-locked items are always picked up. They **can be sold** to vendors, but cannot be slotted or used until the level is reached. Mining and harvesting keep their own level check (`ResourceGatherer.cs:51`). |

---

## 1. User requirements (verbatim intent)

1. Edit the `IO_Ancient_Cache` chest in the hierarchy **and** project, using the DM events script.
2. The loot window lists **each item**. The player can click an item individually and press **Loot** for that item.
3. **Loot All** loots every remaining item and then **closes** the window.
4. If the player exits the loot window mid-loot, the **lid animates closed**, and the chest can be **opened again** to loot the remaining items (world re-loot chests).
5. The **storage chest** also opens and closes (lid animation) and the player can **move items in and out**.
6. **World looting chests:** a timer lets the player loot leftovers again. Window set to **2 minutes**, after which the chest **dissolves**.
7. A **second lootable chest type:** open/close and **loot once**. It is **gone 5 seconds after exiting the loot menu**.
8. Research unique looting styles and list what people enjoy (see section 12).

Items 3, 6 and 7 are refined by the Oct 6 decisions: Loot All closes only once the chest is emptied (D4), and protected items (D2) and the full-inventory hold (D4) pause the 2-minute and 5-second rules.

---

## 2. Locked decisions (user answers)

| Topic | Decision |
|---|---|
| Chest type readability | Each chest type has a **distinct look** (one-shot / 2-min re-loot / story / permanent storage). The player knows the rules by look. For now this is a **colour tint per type on the same mesh** (D9). Anthony replaces the meshes later. |
| One-shot close warning | **No** "N items remain" prompt on close and **no hint line** (D13). The chest's look carries the rule. |
| One-shot cancel | **Cannot be cancelled** once the menu is closed, except for the two holds below. |
| One-shot timing | **Fully gone at 5 s** after exit. Dissolve starts around **3.4 s**. Leftovers are lost, unless a protected item is still inside (D2) or the full-inventory hold is running (D4). |
| 2-min timer end | **Everything dissolves with the chest**, with the same two exceptions. No rare-item drop-out. |
| Respawn | **None.** A gone chest is gone for good (D1). |
| Loot All | Takes whatever fits and **closes only when the chest is emptied** (D4). |
| Loot window time | **Full pause** (D5). |
| Prefab base | **New DM base chest prefab** (a variant of the Invector base) carries all fixes. `Invector-Chest.prefab` stays untouched (D10). |
| `EnemyLootBag` | **Migrate** to the new per-item loot window. |
| Old loot UI | **Removed** once the UITK window works (D11). |
| Save version | **v24** (D12). |

---

## 3. Hard constraints

- All new UI is **UI Toolkit only** (UXML/USS/`DMUiToolkit*` on an existing `UIDocument` host). No new uGUI. The uGUI loot fallback and the old WorldMenus loot panel are removed once the new window works (D11).
- **DM / DMI naming.** No new Invector-branded identifiers in `_Project`. Legacy Invector package files stay untouched (D10).
- **Aether Credits only** economy. Chests hold no AC (D6). No wallet or crypto wording in new code, save DTOs or UI text; do not copy or route through the legacy `piWalletBalance`, the "player wallet" tooltip on `ItemData.isAcInfused` or the legacy marketplace methods in `PioneerRosterManager`.
- **Shipping start:** 0 AC, level 1, empty inventory. 200 AC, level 5 and the start items are test-only. Every chest rule is designed for the real start.
- **No NavMesh.** Do not retune `Player_v7`.
- **Full AAA semi-HD art**, not low poly. Type tints are HDRP/Lit material variants, and the dissolve must not cause a visible lighting pop.
- **Ammo only loads into weapons from Hot Cross slots** (D7).
- **PC and Mac first**, consoles later. Controller support for the loot window and the storage crate is in scope for PC/Mac (D15). Verify a Mac (Metal) build.
- Tunables live on a durable ScriptableObject under `Assets/_Project/Resources/` and register in Genesis Studio (`DMStudioRegistry` + `DMStudioProfileSectionFilter`, `playModeSave: true`).
- After edits: MCP `refresh_unity`, wait for compile, `read_console` errors, fix. No commit with console errors.

---

## 4. Current-state findings (investigated, read-only)

### 4.1 Real names

| Role | Real name | Path |
|---|---|---|
| Loot table and grant logic ("DM events script") | `DmEvents` (implements `IEnemyLootProvider`) | `Assets/_Project/Scripts/Events/DmEvents.cs` |
| Interaction trigger and lid presentation | `DMItemCollection` (implements `IWorldUsable`) | `Assets/_Project/Scripts/Events/DMItemCollection.cs` |
| Storage chest | `DMStorageCrate` + `DMStorageCrateRuntime` (state and save) | `Assets/_Project/Scripts/Storage/DMStorageCrate.cs` |
| Loot provider contract | `IEnemyLootProvider` (`HasRemainingLoot`, `TryLootNextEntry`, `TryLootAll`, `BuildLootSummary`) | `Assets/_Project/Scripts/AI/IEnemyLootProvider.cs` |

### 4.2 Prefabs

| Prefab | Path | Notes |
|---|---|---|
| Base | `Assets/Invector-3rdPersonController/Melee Combat/Prefabs/Items/Inventory_Collectables/Invector-Chest.prefab` (guid `833597b2...`) | Root `Invector-Chest`: legacy `Animation` (`:220-235`), `DmEvents` (`:237-272`, 10 loot slots), `ScannableTarget`, `OutlineController`. Child `collection` is a trigger `BoxCollider` with `DMItemCollection` (`:811-831`): `openAnimationName: Cache-Lid-Open`, `openLootDialogDelay: 1`, `oneTimeOpen: 1`, `disableTriggerAfterOpen: 1`. Child `Cache Lid` is the animated mesh. Leftover Invector uGUI children: `vActionText (1)` canvas, `icon_keyboard`, `icon_controller`, `bg`, `Text`. Still carries a `NavMeshObstacle` (`!u!208 &20800156`). |
| Project variant | `Assets/_Project/Prefabs/World/IO_Ancient_Cache.prefab` (guid `7e7c8d28...`, `:1-75`) | Overrides name, clip reference, `openAnimationName`. **Does not remove the `NavMeshObstacle`** (breaks the no-NavMesh lock). Fixed in the new DM base prefab, not in the Invector prefab (D10). |
| Storage variant | `Assets/_Project/Prefabs/Storage/Storage Crate.prefab` (guid `23120755...`, `:1-155`) | Removes `DmEvents`, `DMItemCollection`, NavMesh obstacle. Adds `DMStorageCrate` with `crateId: camp_storage_01`, `slotCount: 20`. |

### 4.3 Scene instances (`Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity`)

| Instance | Location | Source and notes |
|---|---|---|
| `IO_Ancient_Cache` | PrefabInstance `1471806888` (`:199547-199733`), parent transform `1549374688` | Variant prefab on scene root "Combat Scene Items". Overrides `lootSlots` to 15 items, amounts 1 / 300 / 500. |
| `IO Ancient Cache` | PrefabInstance `1027063537` (`:145303-145371`), parent `127228831` ("-=ENVIRONMENT=-") | Direct instance of the raw Invector base, default 10 slots. Solid collider (`6596274`) and `NavMeshObstacle` (`20800156`) are disabled. |
| `Storage Crate` | PrefabInstance `1884952838923642682` (`:281954-281976`), parent `127228831` | One camp crate. |

`Combat_Sandbox.unity` also references the cache (2 hits). It is not the playable scene, but check it after edits.

The 15-item override on `IO_Ancient_Cache` (amounts 1 / 300 / 500) is test content, not shipping loot. The raw Invector instance `1027063537` is replaced by a DM variant (section 13.2).

### 4.4 Lid animation

- No Animator controller. `Assets/_Project/Art/Animation/Cache-Lid-Open.anim` is a **legacy** clip (`m_Legacy: 1`, path `Cache Lid`, 1 s) played through a legacy `Animation` component.
- `DMItemCollection.cs:213` notes "Prefer Play over CrossFade" (CrossFade was tried and failed).
- Legacy `Animation` uses scaled time (`m_UpdateMode: 0`).

### 4.5 E-interaction path

1. `DMItemCollection` registers with `WorldUseController`, priority `97 - distance`.
2. `TryUse` starts `OpenSequence`: player loot animation (`PlayerLootAnimationController.BeginLoot`) and lid.
3. After `openLootDialogDelay`, `DmEvents.OpenLootDialogFromCollection()` calls `EnemyLootDialogUI.Show(...)`.
4. It then sets `opened = true` and disables the trigger. This is why the chest opens only once.

### 4.6 Loot UI: UITK versus legacy

- **UITK (exists, text-only):** `EnemyLootDialogUI.Show` first calls `DMUiToolkitWorldMenus.TryShowLoot` (`Assets/UI Toolkit/Runtime/DMUiToolkitWorldMenus.cs:193`). Layout in `Assets/UI Toolkit/Screens/WorldMenus.uxml:44-51`: `loot-host`, `loot-title`, one `loot-body` Label, buttons `loot-next`, `loot-all`, `loot-close`. Behaviour in `ShowLootInternal` / `HideLootInternal` / `OnLootNext` / `OnLootAll` (`:927-982`). It does **not** notify the provider on close, so a lid-close hook is impossible today.
- **uGUI fallback (legacy):** `Assets/_Project/Scripts/UI/EnemyLootDialogUI.cs` builds a TMP `Canvas` dialog. Only runs when the UITK host is not driving. **Do not extend it.** Removed once the UITK window works (D11).
- **Per-item loot:** does not exist. `DmEvents.TryLootNextEntry` always grants `remainingLoot[0]` (`:164-175`).
- **Invector-branded legacy** (wrap or replace, do not extend): the base prefab path, `Invector-Chest-01/02` materials, `vActionText`, `icon_*`, `vChangeInputTypeTrigger`.

### 4.7 Storage chest

- `DMStorageCrate.TryUse` opens `DMUiToolkitCrate.TryShow` (`Assets/UI Toolkit/Runtime/DMUiToolkitCrate.cs`). Layout `Assets/UI Toolkit/Screens/Crate.uxml`, overlay document `UITK_Crate`.
- Window has a player grid and a crate grid. Items move by drag, or right-click Split / Drop / Transfer. Transfers go through `TransferPlayerToCrate[At]` and `TransferCrateToPlayer[At]`. **Items in and out already work.**
- Lid: `PlayOpenPresentation` / `PlayClosePresentation` exist and close by reverse playback (`speed = -1`, `time = length`). `HideInternal` calls `crate.NotifyClosed()`.
- **Probable bug (not run-verified):** `TryUse` calls `DMUiToolkitCrate.TryShow` first, which calls `ApplyOverlaySession(true)` and applies `GameplayMenuTime.SetPause(ReasonStorageCrate)` (`Time.timeScale = 0`). `PlayOpenPresentation` runs afterwards and the scaled-time legacy `Animation` freezes. The loot dialog has a milder version (0.2 slow-motion).
- Persistence: `DMStorageCrateRuntime.BuildSave` / `ApplySave` are called from `GameSaveSystem.cs:407` and `:523` / `:844-852`. `CurrentSaveVersion` is **23** and the gate is `data.version < 23`. Save DTOs in `Scripts/Storage/DMStorageCrateSave.cs`.

### 4.8 Item model, inventory, economy

- A loot entry is a `QuestRewardDefinition` (`type`, `item`, `amount`).
- **`QuestRewardType.Pi` is the legacy enum name for Aether Credits.** `QuestRewardGranter.GrantReward` routes it to `GrantAetherCredits`. AC is the only currency.
- Grant path: gate `LevelUnlockUtility.PassesPickupGate`; grant `QuestRewardGranter.GrantReward` (returns items actually added); `TryGrantLootEntry` in `DmEvents` handles partial grants by reducing `entry.amount`; full-inventory feedback `PickupToastUI.ShowInventoryFull`.
- Inventory API: `InventorySystem.AddItem` and `AddItemToMainInventory` (`Scripts/Inventory/InventorySystem.cs:207`, `:372`). `ItemData.maxStack` drives stacking.
- **Nothing is saved for `DmEvents`.** `remainingLoot` rebuilds from `lootSlots` on every `Awake`, so chests refill on every load. `GameSaveData` has no loot field.
- Items save by `item.name` and resolve with `ItemRegistry.Resolve` (as `DMStorageCrateState.ToSave` does).

### 4.9 Dissolve assets

- Shader: `Assets/_Project/Shaders/EnemyDisintegrate.shader` (`Project/EnemyDisintegrate`). Has a URP SubShader and an **HDRP SubShader** (`RenderPipeline=HDRenderPipeline`, `LightMode=ForwardOnly`, `:104-118`). Properties: `_BaseMap`, `_BaseColor`, `_DissolveAmount`, `_DissolveEdgeWidth`, `_DissolveEdgeColor`, `_DissolveNoiseScale`, `_DissolveSpread`.
- Reusable pattern: `EnemyLootBag.DissolveRoutine` (`Scripts/Combat/EnemyLootBag.cs:305-330`) swaps a renderer's material and animates `_DissolveAmount`. `EnemyDisintegrationEffect` handles only `SkinnedMeshRenderer`, so it cannot be reused directly.
- **Gotcha:** chest materials use `HDRP/Lit` (guid `6e4ae406...`) with the texture in `_BaseColorMap`. `EnemyDisintegrationEffect.CreateDissolveMaterial` copies only `_BaseMap` / `_MainTex`, and `EnemyLootBag.EnsureDissolveMaterial` copies colour only. A chest dissolve needs a `_BaseColorMap` to `_BaseMap` copy.

### 4.10 Timer, respawn, pause patterns

- `EnemyLootBag`: `expireTime` check plus `DissolveAfterDelay` coroutine: 20 s unlooted, 2 s after looted.
- `GameplayMenuTime` (`Scripts/UI/GameplayMenuTime.cs`): `ReasonLootDialog` is slow-mo 0.2 today (D5 changes the loot window to a full pause), `ReasonStorageCrate` is a hard pause. Scaled-time timers stop under a full pause; chest timers are also phase-gated (section 11).
- `DMIoClock` exists but real-world-scale timers do not need it.

### 4.11 Other findings (Oct 6 review)

- **Ammo auto-load.** `QuestRewardGranter.GrantItem` calls `inventory.AddItem(item, amount)` with the default `autoCreditAmmoToWeapons = true`, which fills magazines through `WeaponAmmoState.CreditAmmoPickup` (`QuestRewardGranter.cs:97`, `InventorySystem.cs:207-220`, `WeaponAmmoState.cs:282-331`). `ItemPickup.cs:396` and `ResourceGatherer.cs:54` do the same. This breaks the Hot Cross ammo lock (D7).
- **Auto-slotting.** `InventorySystem.AddItem` puts weapons into empty weapon hotbar slots and tools into toolbar slots (`InventorySystem.cs:235-236, 256-302`). The crate uses `AddItemToMainInventory` (`DMUiToolkitCrate.cs:1017`).
- **Level gate.** `TryGrantLootEntry` refuses items that fail `LevelUnlockUtility.PassesPickupGate` (`DmEvents.cs:242-244`, gate at `LevelUnlockUtility.cs:87-88`), so at level 1 a gated chest item could never be looted. D3 changes this.
- **No AC path.** `DmEvents.LootSlot` holds only `ItemData + amount` and always builds an Item reward (`DmEvents.cs:25-30, 108-113`). This matches D6.
- **Duplicated grant code.** `TryGrantLootEntry` is copy-pasted in `EnemyLootBag.cs:606-634`.
- **New Game gap.** `MainMenuController.StartNewGame` (`:912-1010`) resets vendors, the Io clock, progression and built pieces, but not `DMStorageCrateRuntime`. The scene is not reloaded on New Game (`DMBuildingSceneCleaner.CleanForSessionChange` is used instead), so static registries carry over.
- **Awake refill.** `DmEvents.Awake` rebuilds `remainingLoot` from `lootSlots` (`DmEvents.cs:58-63, 93-115`).
- **Controller.** `DMUiJournalGamepadNav.ResolveNavigationRoot` (`:277-295`) has no branch for a loot host, so focus classes alone do nothing. The existing `dmg-list-row` class is already focusable (`:34`). The crate window is drag and right-click only.
- **Prompt versus use priority.** Prompt resolution checks the storage crate before `DMItemCollection`, but use priority is collection 97 > bag 94 > crate 90 (aim-gated) (`WorldUseController.cs:1486-1514`). With both in range the prompt can name the wrong chest.
- **`Shader.Find` in builds.** The dissolve pattern resolves `Project/EnemyDisintegrate` with `Shader.Find` (`EnemyLootBag.cs:389-421`), already flagged as failing or hitching in player builds (`REPAIR_LOG_2026-08-22.md`).
- **Map marker.** `DmEvents.RefreshEmptyState` hides the scanner target (`:221-234`) but the map marker added at `:138-141` stays.
- **Pets.** Pets are designed to auto-pickup loot (`Io_Biome_Ecology_Roster.md:316`). D16 limits this.
- **Vendor icons have no level-specific lock.** The vendor's only "can't do this" look is the blocked overlay, used when an item cannot be bought or sold (`DMUiToolkitVendor.cs:372, 448-450`). `DMVendorListing.requiredLevel` (`DMVendorListing.cs:16`) is declared but not read anywhere. Level gates only show as text in the item tooltip ("Equip Lv N+", "Use Lv N+", "Pickup Lv N+", `ItemTooltipFormatter.cs:304-331`). D18 reuses the blocked overlay as the level-lock look.
- **Hotbar placement gate.** Equip-gated items are already refused on hotbar and toolbar slots (`EquipmentController.CanPlaceItemAt`, `EquipmentController.cs:546-565`; drag path `InventorySlotUI.cs:582-584`), and selecting them is refused (`:396-399`, `:424-425`). D21 keeps this. Today the hotbar check only covers equippable items (`item.IsEquippable && ...`, `EquipmentController.cs:561`), so a use-gated consumable or ammo can still land in a Hot Cross utility slot; D21 closes that gap.
- **Selling.** `DMVendorService.CanPlayerSellItem` (`DMVendorService.cs:33-51`) has no level check, so level-locked items can already be sold. D22 keeps it that way.
- **Pickup gate callers.** `requiredLevelToPickup` is enforced in `DmEvents.cs:243`, `EnemyLootBag.cs:612`, `ItemPickup.cs:277, 392` and `ResourceGatherer.cs:51`.

---

## 5. Worked vs did not work

Git history for the chest is thin. Commit `7cbfd9532` ("Add DM ancient cache loot with lid anim and E-to-loot popup") introduced all of it. No chest-specific discussion was found in agent transcripts.

| Worked | Did not work / missing |
|---|---|
| Forward legacy `Animation.Play("Cache-Lid-Open")` with the clip bound by child path `Cache Lid` | CrossFade on the legacy clip (`DMItemCollection.cs:213`) |
| `DMItemCollection` + `DmEvents` as world-use interaction and loot grant | Per-item loot: `TryLootNextEntry` always takes entry `[0]` |
| UITK Storage Crate window (drag, transfer, Split/Drop) with save/load at version 23 | Any "window closed" notification, so a lid-close hook is impossible |
| `EnemyLootBag` dissolve pattern with the HDRP SubShader | Re-opening: `oneTimeOpen` and `disableTriggerAfterOpen` kill the trigger permanently |
| | Loot persistence (nothing saved) |
| | Probable frozen storage lid under pause (not run-verified) |
| | `NavMeshObstacle` left on the cache prefab (contradicts no-NavMesh lock) |

---

## 6. Data model

### 6.1 `DMLootChestProfile` (ScriptableObject)

- Asset: `Assets/_Project/Resources/Loot/DM_LootChestProfile.asset` (new folder).
- `ResourcePath = "Loot/DM_LootChestProfile"` and a `static Live` accessor, same pattern as `DMBuildingGhostProfile`.

| Group | Fields (defaults) |
|---|---|
| World Reloot | `relootWindowSeconds = 120`, `timerStart = FirstExitWithLeftovers`, `emptiedDissolveDelay = 2` |
| Single Loot | `postExitDestroySeconds = 5` (counts from window close), dissolve plays in the final `dissolveSeconds` (starts about 3.4 s); `dissolveSeconds` is clamped to `postExitDestroySeconds` |
| Full-inventory hold | `fullInventoryHoldSeconds = 1800` (30 minutes, D4; restarts on each revisit, D19) |
| Storage | `defaultSlotCount = 20`, `rangeCloseAppliesToStorage = false` |
| Shared presentation | `lidOpenSeconds = 1`, `lidCloseSeconds = 0.8`, `openLootWindowDelay = 1`, `interactRange = 3`, `closeRangeMeters = 4.5`, `dissolveSeconds = 1.6`, `dissolveEdgeWidth`, `dissolveEdgeColor = Gold`, serialized dissolve shader/material refs (no `Shader.Find`) |

Per-type tints are HDRP/Lit material variants on the DM prefab variants, not profile fields (D9).

### 6.2 Mode enum

`DMLootChestMode { WorldReloot, SingleLoot, Story, Storage }` lives on the chest component as a **per-instance** setting, not a profile field. `Story` is the permanent story crate (D14).

### 6.3 Runtime registry

- Static `DMLootChestRuntime`, keyed by `chestId`, same pattern as `DMStorageCrateRuntime` (`GetOrCreate`, `BuildSave`, `ApplySave`, `ResetAll`, `StatesChanged`). Also covers area unload and scene reload.
- State holds `phase`, `timerStarted`, `remainingSeconds`, `holdRemainingSeconds`, and the remaining entries (stable `entryId`, `itemId`, `amount`). Items only; no AC entries (D6).
- A registry entry wins over the serialized `lootSlots`. The chest pulls its state in `OnEnable`, which also covers streamed tiles that enable after `ApplySave`.
- Holds an active-chest list (bucketed by tile if needed) so `WorldUseController` prompt resolution does not scan every chest.
- Loot chests never register in `DMStorageCrateRuntime`. Building costs and generator fuel read crates in range, so the two must stay separate.
- `chestId` is serialized per instance and assigned at authoring time as a GUID-style id by an editor tool. A duplicate-id validator runs on scene save (Ctrl+D copies ids). The rounded position (`lootchest_x_y_z`) is a fallback only, because it changes when a chest is nudged or a tile streams with an offset.
- Enemy and boss drop containers are spawned at runtime with their enemy and are not stored in `lootChests`. They come back only when that enemy or boss respawns (D1).

### 6.4 Genesis Studio registration

- Add two subtabs under the existing `"world"` category in `DMStudioRegistry.cs`: **Loot Chests - Timers** and **Loot Chests - Presentation**.
- Both `DMStudioPanelMode.SingletonAsset` pointing at `.../DM_LootChestProfile.asset`, `playModeSave: true`.
- Add `DMStudioProfileSectionFilter` values `LootChestTimersOnly` and `LootChestPresentationOnly` with field lists and `GetSectionNote` text. The `switch` default is `_ => true`, so both new cases must be added explicitly. The hold seconds go in the Timers list.
- Play-mode save roots derive from the asset folder, so `Resources/Loot` registers automatically.

---

## 7. Component design

### 7.1 Controller

Evolve **`DMItemCollection` in place** (keep its script guid so prefab references survive).

- Add `DMLootChestMode mode` and `chestId`.
- State machine: `Closed -> Opening -> Open (window up) -> Closing -> Closed`, then `Dissolving -> Gone`.
- `Gone` means `SetActive(false)`, not `Destroy`, so a load or New Game can bring the chest back (section 9).
- Two holds sit on top of the phase: **protected** (a quest or unique entry is still inside, D2) and **full-inventory** (the 30-minute hold, D4). While either applies, the chest cannot enter `Dissolving`, no expiry timer runs, and the chest stays re-openable.

### 7.2 `DmEvents` and `IDMLootContainer`

- `DmEvents` stays the loot table.
- New `IDMLootContainer` (next to `DmEvents`): `Entries` view, `TryLootEntry(entryId)`, `TryLootAll()`, `NotifyWindowClosed()`, `IsEmpty`, `HasProtectedEntries`.
- `TryLootEntry` is keyed by a **stable entry id**, not a list index. Indices shift after `RemoveAt`, and a click after a queued refresh could hit a stale row.
- Both calls go through the shared grant path (section 7.7). The partial-grant behaviour of today's `TryGrantLootEntry` (reduce `entry.amount` in place) is kept.
- Chest `lootSlots` hold items only. An editor validator flags anything else (D6).
- Keep `IEnemyLootProvider` so `EnemyLootBag` is unaffected until its migration step.

### 7.3 `DMChestLid`

- Extract from the copy-pasted code in `DMItemCollection` and `DMStorageCrate`.
- **Manually samples** `AnimationState.time` with `Time.unscaledDeltaTime`, forward and reverse, resuming from the current time. Fixes the frozen lid under pause; this matters more now that the loot window is a full pause too (D5).
- Coroutine reports completion.
- Keep the clip legacy. Converting to an Animator controller is optional later work. A DM procedural hinge (like the `DMBuildingDoor` hatch lid, `DMBuildingDoor.cs:280-300`) is an allowed alternative and makes Anthony's later mesh swap (D9) easier.

### 7.4 `DMChestDissolve`

- Swap materials, **copy `_BaseColorMap` into the dissolve material's `_BaseMap`**, drive `_DissolveAmount`.
- Shader and material come from serialized refs on the profile, not `Shader.Find` (player builds, Mac included).
- Cover every sub-mesh and the lid child renderer (`IO_Ancient_Cache.prefab:71-74` overrides `m_Materials[1]`). One material instance per renderer, destroyed on cleanup (follow `EnemyLootBag.OnDestroy`). Prefer an HDRP Lit-compatible dissolve so the swap does not pop the lighting (art lock).
- Disable colliders and unregister from `WorldUseController` at the start of `Dissolving`.
- At `Gone`: remove the map marker, hide the scanner target, stop `openParticle`, drop `SceneComponentCache` entries.

### 7.5 Flows

**WorldReloot**
1. E opens the lid, then the window. The world is paused while the window is up (D5).
2. Exit closes the window. If items remain, the lid closes (`Closed`) and the chest can be opened again.
3. The 120 s timer starts on the first exit with leftovers (see section 11). It does not start or tick while a protected item is inside or the full-inventory hold is running.
4. At 0 the chest goes to `Dissolving`, then `Gone` for good (D1). **Everything left dissolves.**
5. If emptied, it dissolves `emptiedDissolveDelay` (2 s) after the window closes.

**SingleLoot**
1. Open, loot.
2. On any exit, or when the last item is taken, the lid closes and interaction is disabled.
3. **Cannot be cancelled.** Fully gone at **5 s** after exit. Dissolve plays in the final `dissolveSeconds` (starts about 3.4 s). The profile clamps this. Leftovers are forfeited.
4. Exceptions: while a protected item is inside (D2) or the full-inventory hold is running (D4), the chest does not lock or dissolve and stays re-openable. When neither applies any more, the 5 s rule runs from the next exit, or from the end of the hold (section 11).

**Story** (D14, e.g. the prologue Emergency Crate)
- Opens and closes like WorldReloot but **never times out or dissolves**. Leftovers stay until taken.
- Once emptied, the crate stays in the world with the lid closed and its prompt and scanner entry off.
- State is saved, so after death or a load it stays looted ("respawn at shuttle with crate already looted if opened", `Prologue_Acts_Expanded.md:120`).

**Emergency Crate: teaching "inventory full" (D17)**
- The crate is a `DM_Story_Crate` and holds more than an empty starting inventory can take. Entries in this order: O2 canister x1, medical pack x1, rock pick / starter tool (`Prologue_Acts_Expanded.md:109`), then one entry of a low-value salvage item that does not stack (`maxStack = 1`). Set the salvage amount so the whole crate needs 3 more main-grid slots than a fresh start has free (20 unlocked slots today, `GameSaveData.cs:43`, so 20 salvage pieces).
- Loot All takes entries in list order, so the story items always land first. The free slots fill, the window stays open with the normal "Inventory full - N left" banner, and the rest stays in the crate. No special code: this is the standard D4 behaviour.
- The player drops, uses or sells items to make room and comes back. The Story crate never expires, so no hold or timer applies.
- Quest step 1.3 completes when the three story items are taken, not when the crate is empty, so the player is never forced to clear it.
- Optional: one Ops or companion line the first time the banner shows (for example "Pack's full. Drop what you don't need, the crate isn't going anywhere.").
- Pick the salvage item from existing items in phase 6, or add one if none fits.
- **Status (Oct 7, 2026, Anthony):** the medical pack (`Medpack`) is the healing item for this crate. `DM_Story_Crate.prefab` holds, in order: Oxygen Tank x1, DM_Mining_Tool x1 (starter tool), Medpack x1, Sci-Fi Pistol x1 and Standard ammo x50. The non-stacking (`maxStack = 1`) salvage item is still **pending** (no such item exists yet); it is added, with the salvage amount set per D17, once it does. Until then the crate does not yet teach "inventory full".

**Storage**
- `DMStorageCrate` stays separate (no loot table, never registers in `DMLootChestRuntime`). Switches to `DMChestLid` and calls `NotifyClosed()` from `DMUiToolkitCrate.HideInternal` (already wired). **Never expires.** Controller support is required (D15, section 8.5).

### 7.6 Other design points

- Interaction stays through `WorldUseController.Register` / `Unregister` with the existing priority maths.
- **Prompt order matches use priority.** Prompt resolution must name the chest that E will actually open (collection 97 > bag 94 > crate 90, aim-gated). The prompt shows the chest type and, when a timer runs, the time left, with gamepad glyphs from `DMInputSchemeRouter`.
- Polling: one light range check every 0.25 s only while the window is open, and one ticker only while a timer or hold is active. No per-frame work otherwise.
- **Visual identity per type** (locked decision): one-shot, 2-min re-loot, story and storage each look different. For now a distinct HDRP/Lit tint per type on the same mesh (D9). Anthony replaces the meshes later; keep the tint in material variants so the swap is simple.

### 7.7 Grant path

One `DMLootGrant.TryGrant(entry, source, out result)` replaces the two copies of `TryGrantLootEntry` (`DmEvents`, `EnemyLootBag`). Result: `Granted | Partial | InventoryFull`.

- **Main grid only** (D8). Items go in through `AddItemToMainInventory`. Weapons and tools are never auto-slotted into weapon or tool slots.
- **Ammo never loads into magazines** (D7). Order: an existing matching stack in a Hot Cross slot, then a matching stack in the main grid, then a free unlocked main-grid slot. Model it on `WeaponAmmoState.ReturnRoundsToInventory` (`:534-560`) and always pass `autoCreditAmmoToWeapons: false`. World pickups (`ItemPickup.cs:396`, `ResourceGatherer.cs:54`) switch to the same rule.
- **Level-gated items** (D3, D22) are granted at any level: no pickup-level check on chests, enemy drops or world pickups. Remove the pickup-gate refusal in `DmEvents.cs:243`, `EnemyLootBag.cs:612` and `ItemPickup.cs:277, 392`. Mining and harvesting keep their own check (`ResourceGatherer.cs:51`). Items show the level-lock look (section 8.8) while the player is below the item's equip or use gate (`LevelUnlockUtility.GetEffectiveEquipRequiredLevel` / `GetEffectiveUseRequiredLevel`). `requiredLevelToPickup` is not part of the lock, so an item with only a pickup gate shows no lock. Trying to equip or use a locked item shows the existing Require Level popup, once per action.
- **Level-locked items stay in the main grid** (D21). They never go to hotbar, toolbar or Hot Cross slots until the level is reached, including through `AddItem` auto-slotting on world pickups and enemy drops. Level-locked ammo skips the Hot Cross step of the ammo order and goes straight to the main grid. They can still be sold (D22).
- **No AC** (D6). The chest path is item-only. If an enemy bag carries AC, it keeps one feedback path (the roster's AC reward card, no extra "+N AC" toast).
- **Partial grants** reduce `entry.amount` in place; the toast reads "+X (Y left)". `PickupToastUI.ShowInventoryFull` fires once per action, not once per row.

### 7.8 Pets and companions (D16)

- Pet auto-loot targets only nearby pickup items and enemy drop loot (`EnemyLootBag`). It never takes from `DMItemCollection` / `DmEvents` chests, story crates or storage crates.
- Companions never loot chests.
- Once `EnemyLootBag` implements `IDMLootContainer`, the pet filter must check the source type, not just the interface.

---

## 8. UITK loot window spec

### 8.1 Host

- New overlay: `Assets/UI Toolkit/Runtime/DMUiToolkitLoot.cs`, `Assets/UI Toolkit/Screens/Loot.uxml`, `Loot.uss`.
- Uses `DMUiToolkitOverlayDocument.Ensure(...)` with new constants `LootName`, `LootUxml`, `LootUss`, `LootSort = ModalInteractiveSort`. **Not a second HUD root.**
- `EnemyLootDialogUI.IsDialogOpen`, `CloseAnyOpenLoot`, and `DMUiToolkitWorldMenus.IsAnyModalOpen` must include the new host (`DMItemCollection.TryUse` and `DmEvents` gate on them). Also set `PlayerController.SetLootDialogOpen` while the window is up.
- `EnemyLootBag` migrates to this window (locked decision).
- **Old surfaces are removed once the window works (D11):** the Canvas/TMP fallback in `EnemyLootDialogUI`, and the `loot-host` panel in `WorldMenus.uxml` with its code in `DMUiToolkitWorldMenus` (`ShowLootInternal` / `HideLootInternal` / `OnLootNext` / `OnLootAll`). The static gates (`IsDialogOpen`, `CloseAnyOpenLoot`) move to `DMUiToolkitLoot` or stay as thin forwards, and callers are updated.

### 8.2 Structure

```
loot-root
  loot-veil
  loot-panel                (Dark Navy, Slate Gray border)
    loot-header             (Charcoal; title; close X)
    loot-list (ScrollView)
      loot-row x N          (icon, name, amount; focusable; per-row Loot button;
                             level-lock overlay when gated, section 8.8)
    loot-footer
      loot-all              (Loot All; Deep Magenta, Rich Fuchsia hover)
      loot-close            (Close)
      loot-hint             (WorldReloot: time left;
                             any type: "Inventory full - N left" banner)
```

There is no one-shot hint line (D13).

### 8.3 Palette (`DarkMatterGenesisUiPalette`)

- Panel Dark Navy, border Slate Gray, header Charcoal.
- Buttons Deep Magenta, Rich Fuchsia on hover/selected.
- Body text Warm Off-White, helper text Soft Beige-Gray.
- "Loot All" accent Gold. There are no AC rows (D6).
- Level-locked rows (D3): the row icon gets the vendor blocked overlay (section 8.8). The row can still be looted.

### 8.4 Binding (no per-frame `Q()`)

- Cache `root.Q<...>()` once in a `BindTree` (like `DMUiToolkitCrate`).
- Create rows once, up to `DmEvents.MaxLootSlots = 25`. Each row's `userData` holds its element refs and the stable entry id (like Crate's `SlotKey`).
- Refresh through a queued `schedule.Execute` on content change (events only), like Crate's `RefreshAll`. Toggle row visibility with `DMUiToolkitOverlayDocument.SetShown`.

### 8.5 Input

- **KBM:** click a row to select; the per-row Loot button grants that entry; `E` loots the focused row (or the top row if none is focused); `Shift+E` is Loot All (existing behaviour); `Esc` closes via `UiEscapeGate.TryConsumeEscape`. The `E` press on the frame the window opened is ignored (same idea as `weaponOpenedFrame`, `DMUiToolkitWorldMenus.cs:432`), so the press that opens an enemy bag does not also loot.
- **Controller (required for PC/Mac, D15):** add a loot branch to `DMUiJournalGamepadNav.ResolveNavigationRoot` (`:277-295`) and reuse the existing focusable `dmg-list-row` class (no new `dmg-loot-row`). First focus through `NotifyMenuOpened`. A loots the focused row, Y loots all, B closes. Glyphs via `DMInputSchemeRouter`.
- **Storage crate controller (D15):** add a crate branch to the same navigator. Player and crate grids are focusable; A transfers the focused stack, X splits, B closes. Today the crate is drag and right-click only.

### 8.6 Window-closed callback

The host calls `IDMLootContainer.NotifyWindowClosed()` on **every** close path: Close button, Esc, Loot All, last item taken, range, death. This is the missing hook that drives the lid-close, the timers, the full-inventory hold check (D4) and the release of the pause (D5).

### 8.7 World time while open (D5)

- The loot window applies a **full pause** (`Time.timeScale = 0`) through `GameplayMenuTime`, like the storage crate. Change `ReasonLootDialog` from slow motion 0.2 to a pause, or add a loot pause reason.
- The lid runs on unscaled time (section 7.3), so it still animates.
- Chest timers are also gated on phase (section 11), so they never tick while the window is open, whatever the time scale.
- Autosave stays blocked while the window is open (the host is part of `IsAnyModalOpen`, read by `DMGameAutosave`).

### 8.8 Level-locked look (D18)

"Shown as locked" matches the vendor icons exactly. The vendor screen has one blocked look:

- A child `VisualElement` with class `dmg-vendor-block`, added once per slot on top of the icon and amount (`DMUiToolkitVendor.cs:489-492`), `pickingMode = Ignore`.
- Style (`Vendor.uss:172-179`): `position: absolute; left: 0; top: 0; right: 0; bottom: 0; background-color: rgba(143, 30, 94, 0.55);`. A Deep Magenta wash over the whole slot.
- Toggled with `DMUiToolkitOverlayDocument.SetShown(block, showBlock)` in `PaintSlot` (`DMUiToolkitVendor.cs:536-582`, shown only when an item is present, `:549`, `:572`). The icon, amount text and slot border do not change. There is no lock glyph and no level text on the icon.
- `DMUiToolkitCrate` already reuses the same class (`DMUiToolkitCrate.cs:311, 345`).

Apply it the same way to:

- **Loot rows** in `DMUiToolkitLoot` (overlay over the row icon).
- **Inventory main-grid slots** (`DMUiToolkitMenus.cs:1254-1268`, `dmg-inv-slot` / `dmg-inv-icon`).
- **Inventory hotbar slots** (`DMUiToolkitMenus.InventoryHotbar.cs:92-98`).
- **Hot Cross icons** (`DMUiToolkitHotCross.cs`, icon paint around `:963-990`).

Rules:

- Shown while the player's level is below the item's effective equip or use gate. Repaint on level-up.
- Use the same values as `.dmg-vendor-block`. Either add the class to `Journal.uss`, `HotCross.uss` and `Loot.uss` with identical values, or move it to one shared stylesheet. Do not invent a new colour.
- Do not confuse it with `.dmg-inv-slot--locked` (`Journal.uss:364-368`), which marks locked inventory capacity, not a level gate.
- The level number stays in the existing tooltip lines (`ItemTooltipFormatter.cs:304-331`). Nothing new is added to the icon.
- The same look and rule apply to items from enemy drops and world pickups (D18).
- **Placement (D21):** a level-locked item can only sit in the main inventory grid. Dragging it to a hotbar, toolbar or Hot Cross slot is refused with the existing Require Level popup, once per failed drag (`EquipmentController.CanPlaceItemAt`, `EquipmentController.cs:546-565`; `InventorySlotUI.cs:582-584`). Extend the hotbar check at `:561` from equippable items to any item that fails its equip or use gate. Nothing moves on its own when the level is reached; the player slots it.
- **Selling (D22):** level-locked items can be sold to vendors. `DMVendorService.CanPlayerSellItem` (`DMVendorService.cs:33-51`) already has no level check and must not get one. On the vendor screen the `dmg-vendor-block` overlay keeps its vendor meaning (cannot sell here), so a sellable level-locked item shows without the overlay there and sells normally.

---

## 9. Persistence and lifecycle

### 9.1 Save data

- New `data.lootChests` array of `DMLootChestSave` on `GameSaveData`.
- Per chest: `chestId`, `phase`, `timerStarted`, `remainingSeconds` (float, **not** an absolute timestamp), `holdRemainingSeconds`, and the entry list (`itemId` via `item.name`, `amount`). Items only; no AC entries (D6). Do not store `QuestRewardType`, so the legacy `Pi` name never reaches the save format.
- Bump `GameSaveSystem.CurrentSaveVersion` **23 -> 24**. v24 is reserved for loot chests; Combat Plan v2 takes v25 (D12). Add `DMLootChestRuntime.BuildSave` / `ApplySave` beside the crate hooks at `:407` and `:523`, with a `data.version < 24` guard.
- Saves from v23 or older have no loot data, so every chest starts fresh.
- Unknown `chestId` entries are kept, not dropped, so tiles that are not loaded yet keep their state.
- Saving in between phases: `Opening`, `Open` and `Closing` save as `Closed` with leftovers (autosave is blocked while the window is open). `Dissolving` saves as `Gone`. A SingleLoot inside its 5 s post-exit countdown saves as `Gone`, because the loss is already locked in. A chest under a protected or full-inventory hold saves as `Closed` with its hold.

### 9.2 Load and restore

- `Gone` chests apply instantly on load (deactivate, no dissolve).
- A chest that is not `Gone` in the save but already dissolved in the current session comes back: re-activate it, restore its original materials, reset the lid pose, re-register with `WorldUseController`, and restore the scanner target and map marker.
- The registry wins over `lootSlots`. `DmEvents.Awake` may still build the authored defaults, but if the registry has an entry for the `chestId`, the chest takes it in `OnEnable`.

### 9.3 New Game reset

- `MainMenuController.StartNewGame` calls `DMLootChestRuntime.ResetAll()` and re-enables and restores every scene chest (same steps as 9.2).
- It also calls `DMStorageCrateRuntime.ResetAll()`, which is missing today.
- Without this, gone or half-looted chests carry over, because the scene is not reloaded on New Game.

### 9.4 Respawn and death (D1)

- World-placed chests never respawn. `Gone` is permanent for that save.
- Enemy and boss drop containers come back only when their enemy or boss respawns.
- Chest state is kept through player death and respawn. The window closes on death (section 10) and the normal exit rules apply.

### 9.5 Clock

- Timers tick on scaled game time and only while the phase is `Closed`, the timer has started, and no hold applies. They never tick while the window is open (it is also a full pause, D5).
- The 30-minute hold also counts scaled game time while the chest is `Closed`.
- Save and quit stores the remaining seconds; load resumes from them. The runtime registry keeps ticking if a tile unloads, as long as the process lives. An unvisited chest never starts a timer.
- Other slow-motion menus (vendor, map, crafting) slow the timers along with the world. No special case.

---

## 10. Edge cases

| Case | Handling |
|---|---|
| Player dies mid-loot | Subscribe to `SurvivalStats.PlayerDied`; close the window and notify. The normal exit rules then apply (timer, holds). Chest state is kept through death. |
| Walk away mid-loot | The world is paused while open, so this is a safety net (teleport, forced move): range check every 0.25 s, close the window if beyond `closeRangeMeters`. Not applied to storage. |
| Inventory full (Loot All or per-row Loot) | Take what fits, in list order. Partial amounts stay in the row. One toast. The window stays open with "Inventory full - N left". Loot All closes only when the chest is emptied. Closing with leftovers after a full-inventory refusal starts the 30-minute hold (D4). |
| Level-gated item at level 1 | Looted normally. Shown with the vendor blocked overlay until the level is met (D3, D18). |
| Level-gated enemy drop or world pickup | Picked up at any level, same overlay (D18, D22). Lands in the main grid, never auto-slotted (D21). |
| Level-locked item dragged to hotbar, toolbar or Hot Cross | Refused, Require Level popup once per failed drag (D21). |
| Selling a level-locked item | Allowed at any level (D22). |
| Quest or unique item left in an expiring chest | The chest stays, re-openable, with no timer and no dissolve until those entries are taken (D2). |
| Ammo looted with a matching weapon equipped | Magazine unchanged. Ammo lands on the matching Hot Cross stack or in the inventory (D7). Same for world pickups. |
| Weapon or tool looted with empty hotbar or tool slots | Lands in the main grid (D8). |
| Looted while dissolve timer runs | Block new opens once `Dissolving` starts; unregister from `WorldUseController`. |
| Re-open after lid closed | WorldReloot and Story: always while items remain. SingleLoot: only while a protected item is inside or the full-inventory hold runs. |
| Two chests at once | One loot window at a time (`IsDialogOpen` gate), same as today. |
| Crate and cache both in range | The prompt names the one that E will open (section 7.6). |
| Same-frame E on an enemy bag | The opening press is ignored by the window (section 8.5). |
| Dissolve while player stands near | Visual only. Disable colliders and interaction at the start of `Dissolving`. |
| Save during Opening, Open or Closing | Persist the state as `Closed` with leftovers. `Dissolving` and the SingleLoot post-exit countdown save as `Gone` (section 9.1). |
| Timer reaches 0 while window is open | Cannot happen: the timer is phase-gated and the world is paused while the window is open. |
| One-shot closed with items left | Items are lost (locked decision). No cancel. Exceptions: protected items (D2) and the full-inventory hold (D4). |
| New Game after a half-looted session | Every chest and storage crate resets (section 9.3). |
| Pet near a chest | Pets never loot chests (D16). |

---

## 11. Timer rule

- **120 s starts at the first exit with leftovers.**
- **Frozen while the window is open** (phase gate; the world is also paused, D5).
- **Not reset by later closes.** Reopening and closing again does not extend the window.
- **Protected items (D2).** While any quest or unique entry is inside, no timer starts or ticks and the chest never dissolves. Once the last one is taken, the normal rule applies from the next exit.
- **Full-inventory hold (D4, D19).** If a loot action during the visit (Loot All or a per-row Loot) could not take everything because the inventory was full, closing with leftovers starts a 30-minute hold (`fullInventoryHoldSeconds = 1800`, game time). During the hold the 120 s timer and the SingleLoot countdown are paused and the chest stays re-openable. **Each revisit restarts the hold:** when the window closes again with items left, the hold goes back to the full 30 minutes. The hold ends when the chest is emptied, or when 30 minutes pass without a revisit. Then the normal rule resumes: WorldReloot continues its remaining 120 s (starting it if it had not started) and SingleLoot runs its 5 s countdown, so it dissolves 5 s after the hold ends.
- Story and Storage have no timer.
- Rationale: a predictable 120 s window that cannot be extended and does not tick during inventory management, without letting a full inventory or a quest item turn into a permanent loss. Only the full-inventory hold can be renewed, and only by coming back to the chest.

---

## 12. Looting-style research summary

Evidence tags: **[Doc]** documented developer/industry material, **[Comm]** community discussion (Reddit, Steam, forums), **[Op]** inference. Most "what players like" evidence is anecdotal; the strongest hard sources are the Bungie and Gearbox talks and the Blizzard and Gearbox developer posts.

This section is research, not build spec. The telegraph layers in 12.3 item 2 and items 4-8 in 12.3 are backlog (section 13.0, phase 8).

### 12.1 What players enjoy (ranked)

1. **Hybrid list: per-item take plus a separate Take All.** Most repeated pattern. [Fallout 76 thread](https://www.reddit.com/r/fo76/comments/18gft0h/do_you_pick_choose_your_loot_or_are_you_a_take/) (players cherry-pick while exploring, Loot All after events); [GameDev StackExchange](https://gamedev.stackexchange.com/questions/148513/how-can-i-design-loot-to-be-enjoyable-to-acquire-and-neither-a-chore-nor-frustra) (automate when frequent, "decision" moment for rarer gear); Skyrim Take All must not share a button with Use/Store ([gamesas thread](https://www.gamesas.com/interface-change-t184878-100.html)). [Comm]
2. **Hold-to-collect.** Monster Hunter gather/carve chaining ([r/MonsterHunterWorld](https://www.reddit.com/r/MonsterHunterWorld/comments/ajica0/psa_hold_the_carvegather_button/), [Kotaku on Wilds](https://kotaku.com/monster-hunter-wilds-buttons-skip-carve-slinger-gather-1851785338)). [Comm]
3. **Instant, animation-free pickup in action games.** Elden Ring blue-orb pickups ([r/Eldenring](https://www.reddit.com/r/Eldenring/comments/qvubf3/picking_up_items_instantly_without_an_animation/)). Dying Light 2 drew complaints for hold-E on every pickup and long chest animations ([Steam](https://steamcommunity.com/app/534380/discussions/0/3192492886087002597/?l=polish)). [Comm]
4. **Area or type auto-loot.** Path of Exile players want Diablo 3-style vacuum of one stack type ([r/pathofexile](https://www.reddit.com/r/pathofexile/comments/1j3hq7h/ggg_can_we_finally_get_aoe_loot_pretty_please/)). [Comm]
5. **Loot filters.** Diablo 4 filters ([Blizzard Watch](https://blizzardwatch.com/2026/04/29/embargo-loot-filter-diablo-4-lord-hatred-use/), [Pocket-lint interview](https://www.pocket-lint.com/diablo-4-loot-reborn-interview/)); Path of Exile NeverSink ([GitHub](https://www.github.com/NeverSinkDev/NeverSink-Filter)). [Doc/Comm]
6. **The compare-and-decide loop is itself the fun.** [IGN on Pitchford](https://www.ign.com/articles/borderlands-4-chief-randy-pitchford-says-if-more-developers-better-understood-why-gamers-love-making-decisions-about-loot-wed-have-good-competitors); Elden Ring's missing gear comparison ([SUPERJUMP](https://www.superjumpmagazine.com/analysing-the-ux-design-of-elden-ring/)). [Doc/Comm]
7. **Grid inventories (Resident Evil 4).** Attache case as a puzzle ([IGN](https://me.ign.com/en/pc/157950/resident-evil-4s-inventory-is-the-best-in-the-series)); Auto-Sort is divisive ([Kotaku](https://kotaku.com/resident-evil-4-remake-auto-sort-new-feature-case-1850274535), [GameRant](https://gamerant.com/resident-evil-4-remake-auto-sort-storage-attache-case-worse/)). [Comm]
8. **Tension while searching.** Dead Space stomp-looting ([r/DeadSpace](https://www.reddit.com/r/DeadSpace/comments/te38zb/thoughts_on_enemies_dropping_ammo_in_the_dead/)); The Last of Us live backpack ([CriticalHit](https://www.criticalhit.net/gaming/the-last-of-us-is-about-consequence/)) [Doc]; Dark and Darker vulnerable chest-open ([guide](https://www.rsgoldnow.com/News/dark-and-darker-how-to-complete-the-shiny-secrets-quest.html)). [Comm]
9. **Fair traps and mimics.** Dark Souls mimics as a learnable tell ([Kotaku](https://kotaku.com/i-love-how-mimics-work-in-the-dark-souls-series-1825072124)); Dark and Darker tells ([r/DarkAndDarker](https://www.reddit.com/r/DarkAndDarker/comments/162rss7/psa_you_can_expose_mimics_by_hitting_the_chest_with_your_weapon_if_it_makes_a_metal_sound_instead_of_a_wooden_sound_it_is_a_mimic/)). [Comm/Doc]
10. **Telegraphed or chase-based timers.** Diablo Treasure Goblins ([PC Gamer](https://www.pcgamer.com/crushing-diablo-3s-treasure-goblins/), [Blizzard](https://news.blizzard.com/en-us/article/20149717/developer-insights-behind-the-goblin-giggle)); Dying Light airdrops ([wiki](https://dyinglight.fandom.com/wiki/Airdrops)); Hunt: Showdown 30 s extraction ([r/HuntShowdown](https://www.reddit.com/r/HuntShowdown/comments/r1yl5a/discussion_is_the_extraction_countdown_too_short/)). [Doc/Comm]
11. **Single-use hand-placed loot.** Genshin chests do not respawn ([GameRant](https://gamerant.com/genshin-impact-chest-respawn-guide/)); Elden Ring unique loot ([XboxAchievements](https://www.xboxachievements.com/news/news-41141-elden-ring-might-have-the-best-loot-in-gaming.html)); Hunt: Showdown two loots per corpse ([r/HuntShowdown](https://www.reddit.com/r/HuntShowdown/comments/smlwwt/questions_about_looting_in_hunt/)). [Comm]
12. **Opt-in companion carry.** Skyrim/Fallout 4 followers as storage ([GamingSE](https://gaming.stackexchange.com/questions/37288/can-dismissed-companions-be-trusted-with-my-loot), [Nexus](https://www.nexusmods.com/skyrim/mods/57506)); Genshin chest-open voice lines ([r/Genshin_Impact](https://www.reddit.com/r/Genshin_Impact/comments/qpyr9y/with_these_new_chest_voice_lines_who_do_you/)); Outward pack mounts want capacity caps ([r/outwardgame](https://www.reddit.com/r/outwardgame/comments/1u3178m/pack_mounts_good_idea_rough_execution_outward_2/)). [Comm]
13. **Reward-feel systems.** Anticipation beats the item ([Psychology of Games](https://www.psychologyofgames.com/2019/08/what-the-heck-are-surprise-mechanics/), [peer-reviewed study](https://link.springer.com/article/10.1007/s10899-019-09913-5)); Overwatch dropped early rarity lights ([Kotaku](https://kotaku.com/why-opening-loot-boxes-feels-like-christmas-according-1793446800)); Borderlands legibility ([GameBanshee](https://www.gamebanshee.com/news/107774-borderlands-2-interview-v15-107774.html), [Gearbox](https://www.gearboxsoftware.com/2013/09/inside-the-box-evolution-of-loot/)); Diablo 4 rarity audio cues ([vhpg](https://www.vhpg.com/diablo-4-gear-audio-cues/)); Last Epoch staggered drops ([forum](https://forum.lastepoch.com/t/drop-sounds-notifications-for-loot-filter-like-poe/75090)); hidden pity timers ([Blizzard forum](https://us.forums.blizzard.com/en/d3/t/bad-luck-protection/10010)); loot-per-effort lesson ([Polygon on GDC 2015](https://www.polygon.com/2015/3/6/8152719/destiny-loot-cave-omnivore-bungie-gdc-2015/)). [Doc/Comm]
14. **Diegetic / scanner looting.** Dead Space tools ([GamesRadar](https://www.gamesradar.com/how-dead-spaces-innovative-ideas-and-design-created-one-of-the-most-innovative-and-distinctive-horror-games-of-recent-time/)); The Last of Us scannable drops ([HardcoreGamer](https://hardcoregamer.com/articles/opinion/features/an-examination-of-looting-in-bioshock-infinite-and-the-last-of-us/60448/)); Starfield scanner ([Gamer Guides](https://www.gamerguides.com/starfield/guide/gameplay/basics/how-to-highlight-loot-and-scan-objects-in-starfield), [Always Scan mod](https://www.nexusmods.com/starfield/mods/18268)); Metroid Prime Scan Visor ([TheGamer](https://www.thegamer.com/metroid-primes-scanner-is-the-original-detective-mode/)); Deep Rock Galactic loot bug ([wiki](https://deeprockgalactic.wiki.gg/wiki/Loot_Bug)). [Comm/Doc]

### 12.2 Pitfalls players hate

1. **Silent or sudden despawn**, especially of high-rarity loot: Fallout 76 bodies vanishing ([Steam](https://steamcommunity.com/app/1151340/discussions/0/4762082044191389032/), [r/fo76](https://www.reddit.com/r/fo76/comments/1f8enpu/better_loot_fast_bodies_disappearing_quickly_at/)); Warframe loot vanishing as you reach it ([r/Warframe](https://www.reddit.com/r/Warframe/comments/1pry78j/please_make_reactant_despawn_after_countdown_not/)); Destiny 2 Postmaster overflow ([Bungie Help](https://help.bungie.net/hc/en-us/articles/360049023872-Destiny-2-Eververse-Items-Guide)).
2. **Inventory-full lock-outs and junk-heavy Take All** (Starfield weight: [Forbes](https://www.forbes.com/sites/paultassi/2023/09/01/how-to-fix-starfields-inventory-storage-and-encumbrance-problems/); Outer Worlds 2 removing weight: [Kotaku](https://kotaku.com/outer-worlds-2-rpg-inventory-weight-encumbered-2000650641)).
3. **Forced long searches** (Tarkov search timers: [r/EscapefromTarkov](https://www.reddit.com/r/EscapefromTarkov/comments/8fwt5t/searching_timer_and_examining_items_should_not_be/), [Fast-Forward Search mod](https://forge.sp-tarkov.com/mod/872/fast-forward-search-ffs); Dying Light 2).
4. **Repeated lock minigames** with trivial rewards (Starfield digipicks: [r/Starfield](https://www.reddit.com/r/Starfield/comments/169awpx/how_do_you_feel_about_the_new_digipick_system/); Oblivion: [r/BethesdaSoftworks](https://www.reddit.com/r/BethesdaSoftworks/comments/1cox0ch/evolution_of_lockpicking_in_bethesda_games/)).
5. **Ambiguous interaction priority** (Hunt: Showdown: [r/HuntShowdown](https://www.reddit.com/r/HuntShowdown/comments/1rtfw3q/loot_system_feels_outdated_in_2026/)).
6. **Auto-transfer between characters** (Baldur's Gate 3 Shared Stash: [PC Gamer](https://www.pcgamer.com/baldurs-gate-3s-latest-patch-has-introduced-a-very-frustrating-borderline-unplayable-glitch-that-makes-companions-dump-their-inventories-on-you/)).
7. **Looting every body and container when most are empty** (Bioshock Infinite).
8. **Blanket 100-credit drops** (Dead Space remake: [Steam](https://steamcommunity.com/app/1693980/discussions/0/3770110614229942496/)).
9. **No gear comparison** (Elden Ring merchant).

**Accessibility guidance on UI timers:** [Xbox Accessibility Guideline 116](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/116) says warn before expiry and give at least 20 s to extend; it exempts core gameplay mechanics, and a loot-dissolve timer is arguably core. [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/do-not-make-precise-timing-essential-to-gameplay-offer-alternatives-actions-that-can-be-carried-out-while-paused-or-a-skip-mechanism/). A relaxed or longer option is the safest approach. [Doc]

### 12.3 Fit for Dark Matter: Genesis (ranked by value / effort)

All [Op] unless noted. Evidence is thin for "chest dissolves after you have opened it"; no documented example was found.

1. **Hybrid chest UI** (low effort, very high value). Per-item take + Loot All + auto-close. Sort by value or rarity. Loot All must say what it left behind and never skip silently. Reveal at most about 0.4-0.8 s and skippable; do not gate input on the reveal. Show rarity only when the lid opens.
2. **Dissolving world chests with a 2-minute re-loot window** (medium effort, high value if telegraphed). Dissolve feedback, all layered:
   - **Visual:** lid stays ajar with a slow shimmer; edge dissolves gradually, ramps up in the last 15-20 s, goes pink/magenta (palette) in the last 10 s.
   - **UI:** small countdown ring on the world prompt plus a thin HUD pip; **show the number only in the last 10 s**.
   - **Audio:** low power-down hum that rises in pitch (not continuous ticking), a soft **ping at 30 s**, a distinct **stinger at 10 s**; offer a captioned or visual-only option.
   - **Companion bark** at about 30 s ("Cache's destabilizing - grab what you want").
3. **One-shot caches** (low effort, biggest risk). Closest to the hated pattern because loss follows an input the player may not have meant. **Locked decision:** rely on a distinct look (a tint per type for now, D9) rather than a close warning, with no hint line (D13). A clear icon/look distinguishing one-shot, re-lootable, and permanent chests from afar is essential.
4. **Storage chest at base camp** (medium effort, high value). Symmetric Take All / Store All, quick-stack, favourites/lock, sort, search (Valheim mod ecosystem shows demand: [Quick Stack Store Sort](https://thunderstore.io/c/valheim/p/Goldenrevolver/Quick_Stack_Store_Sort_Trash_Restock/), [TidyChests](https://thunderstore.io/c/valheim/p/Muindor/TidyChests/)). Clearly different look from expiring caches. Shared base stash with explicit per-character carry, not auto-merge.
5. **Companion carry, opt-in with a visible cost** (medium effort, medium-high value). Per-companion slots, explicit transfer, no auto-dump, optional filtered auto-pickup, visible pack load, chest-open barks, per-companion capacity cap.
6. **Diegetic scanner pass** (medium effort, medium-high value). Short scan pulse outlines caches/bodies/containers, colour-coded by type; shows a tier hint, not exact contents.
7. **Rarity alert layer** (low effort, medium value). Distinct sound and short glint per rarity with a single "alert at or above" setting.
8. **Optional short lock/hack minigame on special caches** (medium effort, low-medium value, do last). Rare, skippable, tied to high-value caches. Avoid routine loot.

Principle: make loss predictable and visible, give the player control over the decision, keep the reward moment fast and satisfying.

---

## 13. Ordered implementation steps

### 13.0 Phases (implementation order)

Build in this order. Phase 2 can ship before phases 3-5, because it fixes per-item loot for the existing caches and bags without the new lifecycle.

| Phase | Goal | Effort | Files likely touched (T) or created (C) |
|---|---|---|---|
| **0. Spec patch** | Done 2026-10-06: decisions and review folded into this plan and the handoff. Moving section 12 to a separate research doc is optional and not done. | S | T: this plan, `DM_Loot_Chest_System_Handoff.md` |
| **1. Foundations** | Profile plus Studio. One grant path: main grid only, ammo to Hot Cross stack then inventory with no auto-load, level-gated items granted, no AC. World pickups switch to the same ammo rule. `IDMLootContainer` with stable entry ids. Item-only validator. Level-lock overlay and use gate; pickup gate removed for chests, enemy drops and world pickups; hotbar, toolbar and Hot Cross refusal extended to every level-locked item. | M | C: `Scripts/Events/DMLootChestProfile.cs`, `Resources/Loot/DM_LootChestProfile.asset`, `Scripts/Events/IDMLootContainer.cs`, `Scripts/Loot/DMLootGrant.cs` (or under `Events/`); T: `DmEvents.cs`, `EnemyLootBag.cs`, `QuestRewardGranter.cs`, `InventorySystem.cs` (only if an overload is needed), `ItemPickup.cs`, `ResourceGatherer.cs` (ammo rule only), `EquipmentController.cs`, `DMUiToolkitMenus.cs`, `DMUiToolkitMenus.InventoryHotbar.cs`, `DMUiToolkitHotCross.cs`, `Journal.uss`, `HotCross.uss` (level-lock overlay), `DMStudioRegistry.cs`, `DMStudioProfileSectionFilter.cs` |
| **2. UITK loot window** | Per-item window with full pause, KBM and controller input, modal gating. Then remove the uGUI fallback and the old loot panel (D11). | M-L | C: `Assets/UI Toolkit/Runtime/DMUiToolkitLoot.cs`, `Screens/Loot.uxml`, `Screens/Loot.uss`; T: `DMUiToolkitOverlayDocument.cs`, `DMUiToolkitWorldMenus.cs`, `WorldMenus.uxml`, `EnemyLootDialogUI.cs`, `DMUiJournalGamepadNav.cs`, `GameplayMenuTime.cs`, `PlayerController.cs` (call site only) |
| **3. Lid, state machine, storage** | `DMChestLid` (unscaled). `DMItemCollection` modes (including Story), phases and holds. Storage lid under pause. Storage crate controller support (D15). Prompt order and prompt text. | M | C: `Scripts/Events/DMChestLid.cs`; T: `DMItemCollection.cs`, `DMStorageCrate.cs`, `DMUiToolkitCrate.cs`, `Crate.uxml` / `Crate.uss`, `DMUiJournalGamepadNav.cs`, `WorldUseController.cs` |
| **4. Timers, dissolve, cleanup** | Phase-gated ticker with protected and full-inventory holds. Dissolve with serialized shader refs. Map marker, scanner and VFX cleanup. Audio hooks. | M | C: `Scripts/Events/DMChestDissolve.cs`, `Scripts/Events/DMLootChestRuntime.cs`, optional dissolve Shader Graph under `_Project/Shaders/`; T: `GameAudioManager.cs` (open, close, dissolve; Genesis **Audio** category), `DmEvents.cs` |
| **5. Persistence and lifecycle** | v24 DTOs, build/apply, restore-on-load, New Game reset (loot chests and storage crates), chest id tool and validator. | M | C: `Scripts/Events/DMLootChestSave.cs`, `Editor/Loot/DMLootChestIdTool.cs`; T: `GameSaveData.cs`, `GameSaveSystem.cs`, `MainMenuController.cs` |
| **6. Content and prefabs** | DM base prefab with the NavMeshObstacle and uGUI children removed. Variants for WorldReloot (`IO_Ancient_Cache`), SingleLoot, Story and Storage with per-type tint materials. Replace the raw Invector scene instance. Emergency Crate contents per D17. `Invector-Chest.prefab` untouched (D10). | S-M | C: `Prefabs/World/DM_LootChest_Base.prefab`, `Prefabs/World/DM_Salvage_Cache.prefab`, `Prefabs/World/DM_Story_Crate.prefab`, tint materials; T: `IO_Ancient_Cache.prefab`, `Prefabs/Storage/Storage Crate.prefab`, `Scenes/Dark Matter Genesis v1.6.5.unity`, `Combat_Sandbox.unity` (check only) |
| **7. Enemy loot migration hardening** | Bags on `IDMLootContainer`. Pet auto-loot filter (D16). Pooled bags. Dead per-frame `Update` work removed. Respawn chain regression-tested. | S-M | T: `EnemyLootBag.cs`, `EnemyLootable.cs`, `EnemyDeathSequence.cs` (verify only), pet auto-loot code (name to confirm), `DMCombatSandboxSpawner.cs` (test bed) |
| **8. Backlog (separate tickets)** | Loot tables (rarity, level band, seed rolled once and saved), rarity cues, timer telegraph layers, lock stub, research items 12.3 #4-8. New chest meshes are Anthony's own task (D9). | L | C: `Scripts/Loot/DMLootTable.cs` plus assets; T: `EnemyDefinition`, `EnemyLootable.cs` |

### 13.1 Create

1. `Assets/_Project/Scripts/Events/DMLootChestProfile.cs` and `Assets/_Project/Resources/Loot/DM_LootChestProfile.asset`.
2. `Assets/_Project/Scripts/Events/DMLootChestRuntime.cs` and `DMLootChestSave.cs`.
3. `Assets/_Project/Scripts/Events/DMChestLid.cs` (unscaled-time lid driver) and `DMChestDissolve.cs` (material swap, `_BaseColorMap` copy, `_DissolveAmount`, serialized shader refs).
4. `IDMLootContainer` (new file next to `DmEvents`).
5. `DMLootGrant` (shared grant path, section 7.7).
6. `Assets/UI Toolkit/Runtime/DMUiToolkitLoot.cs`, `Assets/UI Toolkit/Screens/Loot.uxml`, `Loot.uss`.
7. `Assets/_Project/Prefabs/World/DM_LootChest_Base.prefab`: a variant of `Invector-Chest.prefab` that removes the `NavMeshObstacle` (`20800156`) through `m_RemovedComponents` (as `Storage Crate.prefab` already does) and removes the uGUI children `vActionText (1)`, `icon_keyboard`, `icon_controller`, `bg` and `Text` (D10).
8. Chest variants on the DM base: `Assets/_Project/Prefabs/World/DM_Salvage_Cache.prefab` (SingleLoot) and `Assets/_Project/Prefabs/World/DM_Story_Crate.prefab` (Story, used for the Emergency Crate) (D20). Each type gets its own HDRP/Lit tint material (D9).
9. `Editor/Loot/DMLootChestIdTool.cs`: assigns GUID-style `chestId`s, runs the duplicate-id validator on scene save, and flags non-item loot slots.

### 13.2 Modify

- `DMItemCollection.cs`: state machine, modes (including Story), chest id, holds.
- `DmEvents.cs`: implement `IDMLootContainer`, per-entry loot by stable id, shared grant path, registry over `lootSlots` in `OnEnable`, map marker cleanup. Fix the stale "clamped to 10" comment (`:79`; the limit is 25).
- `DMStorageCrate.cs`: use `DMChestLid` and `DMLootChestProfile`.
- `DMUiToolkitCrate.cs` (and `Crate.uxml` / `.uss` if needed): lid order under pause; controller support (D15).
- `DMUiToolkitOverlayDocument.cs`: new overlay constants.
- `EnemyLootDialogUI.cs`: `IsDialogOpen`, `CloseAnyOpenLoot` for the new host; then remove the uGUI fallback (D11).
- `DMUiToolkitWorldMenus.cs` and `WorldMenus.uxml`: `IsAnyModalOpen`, route `TryShowLoot` to the new window; then remove the old `loot-host` panel (D11).
- `DMUiJournalGamepadNav.cs`: loot and crate navigation roots (required, D15).
- `GameplayMenuTime.cs`: loot window as a full pause (D5).
- `QuestRewardGranter.cs`, `InventorySystem.cs`, `ItemPickup.cs`, `ResourceGatherer.cs`: no ammo auto-load (D7); chest items to the main grid (D8).
- `DMUiToolkitMenus.cs`, `DMUiToolkitMenus.InventoryHotbar.cs`, `DMUiToolkitHotCross.cs`, `Journal.uss`, `HotCross.uss`: level-lock overlay matching `.dmg-vendor-block` (D18, section 8.8).
- `EnemyLootBag.cs` (`:612`) and `ItemPickup.cs` (`:277, 392`): stop refusing on the pickup gate (D18, D22). `ResourceGatherer.cs:51` keeps its check.
- `EquipmentController.cs` (`CanPlaceItemAt`, `:546-565`): keep the refusal on hotbar, toolbar and Hot Cross slots and extend the hotbar check at `:561` to any item that fails its equip or use gate (D21).
- `DMVendorService.cs`: no change. Selling level-locked items stays allowed (D22).
- `WorldUseController.cs`: prompt order, type and time text.
- `EnemyLootBag.cs`: migrate to the new per-item window and the shared grant path; verify existing timers (20 s unlooted, 2 s after looted) still behave.
- Pet auto-loot code: never targets chests (D16).
- `GameSaveData.cs`, `GameSaveSystem.cs`: `lootChests`, `CurrentSaveVersion` 24, build/apply hooks.
- `MainMenuController.cs`: `StartNewGame` resets loot chests and storage crates.
- `DMStudioRegistry.cs`, `DMStudioProfileSectionFilter.cs`.
- `GameAudioManager.cs`: chest open, close and dissolve sounds, registered in the Genesis **Audio** category.
- Prefabs: `IO_Ancient_Cache.prefab` and `Storage Crate.prefab` derive from the DM base (rebuild them as DM base variants if re-basing is not practical); set the mode on `IO_Ancient_Cache`. **Do not edit `Invector-Chest.prefab`** (D10).
- Scene `Dark Matter Genesis v1.6.5.unity`: replace the raw Invector instance `1027063537` with a DM variant; set mode and `chestId` on the cache instances and the Storage Crate. The 15-item override on `IO_Ancient_Cache` (amounts 1 / 300 / 500) is test content, not shipping loot. When the prologue content exists, the Emergency Crate is a `DM_Story_Crate` with the D17 contents.

### 13.3 Verification checklist

1. MCP `refresh_unity` (`mode: if_dirty`, `scope: all`, `compile: request`, `wait_for_ready: true`).
2. Wait for compile and domain reload to finish (no MCP spam during reload).
3. `read_console` (errors) and fix before claiming done.
4. Genesis Studio tabs load; Play-mode edits persist (`playModeSave: true`).
5. Play-test:
   - Per-item Loot.
   - Loot All closes the window when the chest is emptied.
   - Loot All with a full inventory takes what fits, the window stays open with the banner, and closing starts the 30-minute hold. The chest re-opens and does not dissolve during the hold (WorldReloot and SingleLoot).
   - The world is fully paused while the window is open, and the lid still animates.
   - Esc mid-loot closes the lid and the chest re-opens (WorldReloot).
   - 120 s timer is frozen while the window is open, then the chest dissolves with all leftovers.
   - SingleLoot chest is gone 5 s after exit (dissolve starts about 3.4 s), no cancel, when it holds no protected item and no hold runs.
   - A quest or unique item in a SingleLoot or WorldReloot chest: the chest stays until it is taken.
   - A level-gated item at level 1: looted, shown with the vendor blocked overlay in the loot row, inventory and Hot Cross, use refused, overlay gone after reaching the level. Same for an enemy drop and a world pickup.
   - Level-locked weapon, tool, consumable and ammo: stay in the main grid, a drag to hotbar, toolbar or Hot Cross is refused with one popup, a world pickup does not auto-slot, and selling to a vendor works.
   - Mining or harvesting a node above the player's level is still refused.
   - Full-inventory hold: revisiting restarts it at 30 minutes; letting it run out resumes the normal timer (SingleLoot dissolves 5 s later).
   - Emergency Crate on a fresh start: story items land first, the banner shows 3 left, the crate stays, and step 1.3 completes.
   - Ammo looted with a matching weapon equipped: magazine unchanged, ammo on the Hot Cross stack or in the inventory. Same for a world pickup.
   - A weapon or tool looted with empty hotbar slots lands in the main grid.
   - Story crate never dissolves and stays looted after death and load.
   - Storage lid opens visibly under pause; items move in and out with mouse and with a controller.
   - Save and load a half-looted chest, a chest with a running timer, and a chest under a hold. Load a v23 save (all chests fresh). Load an older save over a chest that dissolved this session (it comes back).
   - A gone chest stays gone after time passes and after reload.
   - New Game after a half-looted session: chests and crates reset.
   - Enemy loot bag uses the new per-item window and its timers still work. The opening E press does not loot.
   - Controller-only play through the loot window.
   - Crate and cache in range together: prompt matches what E opens.
   - Autosave is blocked while the window is open.
   - Pets do not loot chests.
   - The uGUI fallback and the old loot panel are gone with no missing references.
6. Check both scene instances and `Combat_Sandbox`.
7. Mac (Metal) build smoke test: dissolve shader present, no pink materials.
8. Do not commit with console errors. Stage scenes, prefabs, materials, and new assets with the scripts (`unity-agent-workflow.mdc`).

---

## 14. Open questions

None. All questions are answered (D1-D22).
