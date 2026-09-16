# Quest Navigation, Lore, PPT & Side Quest — Objectives + Tracking

**Project:** Dark Matter: Genesis  
**Status:** Design lock (Sep 2026)  
**Applies to:** Mainline (progress-triggered), **side quests**, **lore** beats, **PPT** (point-person / direction NPC moments), Field Logs, companions, co-protagonist dialogue  
**Related:** `Map_Search_Zone_System.md`, `Field_Log_Devices.md`, `Prologue_Intro_Crash_To_Horizon.md`  
**Shipped baseline:** `QuestManager`, `QuestDefinition`, `JournalQuestFullscreenWindow` (migrate UI to **Quests** tab), `ActiveQuestHudUI` (right-middle HUD — extend per this doc)  
**Journal shell:** `JournalTabRail`, `JournalWindowId` — add dedicated **Quests** top tab; general **Journal** tab reserved for Field Logs / chronicle (`Field_Log_Devices.md`)

---

## 1. Design goal

One **navigation grammar** for “where do I go?” that supports **low hand-holding** and **multiple information sources**. A quest (or lore step) declares a **navigation mode**; the player may receive it from an **EEB**, **quest giver**, **companion**, **second protagonist** (co-lead / trusted specialist on comms or in camp), or **PPT** conversation (approximate directions).

**Journal Coordinates** (bold gold, from EEB) remain the canonical store for grid strings; quest log links to the same entries when applicable.

---

## 2. Navigation modes (per objective or quest step)

Author **one primary mode** per active step; optional secondary (e.g. coords + search ring).

| Mode | ID | Map | Minimap | Compass | Journal |
|------|-----|-----|---------|---------|---------|
| **Search zone** | `SearchZone` | Alpha **thin ring** | Same ring | **Gold bearing dot** (no ring) — see `Map_Search_Zone_System.md` | Coords + zone label |
| **Map / compass dot** | `PreciseMarker` | Standard `MapMarker` icon | Yes | Normal POI dot (palette color by quest type) | Optional coords in detail |
| **Symbol** | `MapSymbol` | Custom **sprite** (lore shrine, Echo, cache, danger) | Scaled symbol | Symbol at bearing (size per type) | Symbol legend in quest text |
| **Coordinates only** | `CoordsOnly` | **No** marker until player plots from Journal | No | No | **Bold gold** coords; player navigates manually |
| **Approximate** | `Approximate` | **Search zone** (large radius) **or** coords only | Per sub-mode | Gold dot if zone plotted | “~sector” text + optional grid |
| **PPT direction** | `PptBearing` | No new marker | No | **Temporary** gold wedge/dot **3–8 s** after NPC points | Phrase + optional coords in dialog log |

### 2.1 When to use which

| Content | Typical mode |
|---------|----------------|
| Intro horizon pad | EEB → Journal coords + `SearchZone` |
| Side quest “find the cache” | `SearchZone` or `MapSymbol` after first clue |
| Lore-only (no gameplay target) | `CoordsOnly` or no nav — Field Log entry only |
| Mainline progress trigger | Often **no quest giver** — `SearchZone` or environmental resolve |
| Quest giver handoff | Dialog grants quest + `PreciseMarker` or `Approximate` |
| Companion / **2nd protagonist** | Radio or camp talk → `Approximate` coords or `PptBearing` |
| PPT NPC in world | Gesture + voice → `PptBearing`; may also push `Approximate` zone |

**Undisclosed** future areas: start `Locked` (no UI) → unlock via EEB or dialog → `SearchZone` or `CoordsOnly`.

### 2.2 Quest type → default map icon color (symbols / dots)

| Type | Color / notes |
|------|----------------|
| Main / chain | Gold title on tracker; map dot Gold or Warm Off-White |
| Side | Rich Fuchsia accent |
| Lore | Soft Beige-Gray, smaller symbol |
| PPT / local | Deep Magenta brief flash only |

---

## 3. Information sources (who gives the player “where”)

| Source | Delivery | Nav data |
|--------|----------|----------|
| **EEB field log** | Pickup + read | `coordinateEntries` → Journal + optional `searchZoneId` (`Field_Log_Devices.md`) |
| **Quest giver NPC** | Board / dialog (`QuestGiverNpc`) | Quest step sets navigation mode on accept or objective advance |
| **Companion** (trio / base-22) | Comms line or camp interact | `Approximate` or `PreciseMarker` when trust/role allows |
| **Second protagonist** | Dedicated co-lead beats (e.g. Reid/Suri in V4, or package lead) | Same as companion; **max trust** may give tighter coords |
| **PPT NPC** | `PptDirectionResolver` + gesture | Bearing flash + phrase set; may append Journal coord line |
| **World trigger** | Enter biome, loot item | Progress-triggered mainline — no NPC |

Approximate dialog copy examples: “**Somewhere east of the broken relay**,” “**Grid block 14–18** — that’s all they gave us.”

---

## 4. Quests — dedicated Journal tab (category shelves)

Quest browsing lives in a **new top-level Journal tab: Quests** (`JournalWindowId.Quests`). It is **not** mixed with Field Logs or personal chronicle (those stay on the **Journal** tab).

| Tab label | `JournalWindowId` | Contents |
|-----------|-------------------|----------|
| **Quests** | `Quests` *(new)* | All quest definitions / progress, filtered by **category shelf** below |
| **Journal** | `JournalQuest` → rename window to **Chronicle** or repurpose id to `FieldLogs` when implemented | Field Logs, coordinates panel, optional personal notes |
| *(existing)* | Map, Inventory, … | Unchanged |

### 4.0 Keyboard shortcuts (journal tabs)

**Do not use Q** — reserved / conflicts with gameplay (Invector or future bind); not available for Quests.

| Tab | Key | `JournalWindowId` | Notes |
|-----|-----|-------------------|--------|
| **Quests** | **G** | `Quests` *(new)* | **G** unused in `InputSystem_Actions` + `JournalWindowShortcuts`; label **Quests (G)** |
| **Journal** (chronicle / Field Logs) | **J** | `JournalQuest` → chronicle id | Opens journal shell; default tab = chronicle |
| Inventory | I | `Inventory` | Shipped |
| Map | M | `Map` | Shipped |
| Pet | F *(UI label)* / **K** *(input action)* | `Pet` | Align pet key in a future pass |
| Companions | P | `Pioneers` | Shipped |
| Character | U | `Character` | Shipped |
| Craft | C | `Craft` | Shipped |
| Blueprints | R | `Recipes` | Shipped |
| Skills | T | `Skills` | Shipped |
| Echoes | L | `Echoes` | Shipped |
| Achievements | A | `Achievements` | Shipped |

**Implementation:** add `ToggleQuests` → `<Keyboard>/g` in `InputSystem_Actions.inputactions`; `UIManager.OnToggleQuests` → `TryToggleTab(JournalWindowId.Quests)`; `JournalWindowShortcuts.GetShortcutKey(Quests) => 'G'`.

Other unused letters if **G** is ever taken: **O** (objectives), **H**, **N**, **V**, **B** — none bound in project input today.

### 4.1 Category shelves (primary organization)

Every `QuestDefinition` has exactly one **`journalCategory`** (shelf). The Quests UI uses a **left rail or top chip row** — player picks a shelf; the list shows only quests in that category (plus status filters).

| Shelf | `QuestJournalCategory` | What goes here |
|-------|------------------------|----------------|
| **Main Quests** | `Main` | Mainline, prologue, act beats, progress-triggered chains, Kairos repair arc steps authored as quests |
| **Side Quests** | `Side` | Board jobs, NPC one-offs, faction errands, rewards-focused content |
| **Biome Areas** | `Biome` | Regional arcs keyed to **surface/UG biomes** — discovery, biome gates, ecology setpieces |
| **Specific Areas** | `Area` | Named **sub-regions** that are not a full biome shelf — hub bowl, caldera rim, polar lens, command ring, underground instance names, intro crash march, etc. |

**Biome shelf sub-filters** (second row when `Biome` selected):

| Code | Biome name (canon order note) |
|------|-------------------------------|
| B6 | Basalt Highlands (hub) |
| B1 | Sulfur Plains |
| B2 | Geyser Fields |
| B3 | Ash Flats & Ridges |
| B5 | Polar Radiation Flats |
| B4 | Lava Calderas |
| B7 | Precursor Ruin Belt |
| UG1–UG5 | Underground strata 1–5 |

**Specific Areas shelf** uses authored **`areaTag`** strings (e.g. `horizon_pad`, `expedition_graveyard`, `command_center_ring`, `prologue_crash_corridor`). Multiple quests may share one tag; UI groups by tag optionally.

| Field on `QuestDefinition` | Type | Required |
|----------------------------|------|----------|
| `journalCategory` | `QuestJournalCategory` | Yes |
| `biomeCode` | `B1`…`B7`, `UG1`…`UG5` | When category = `Biome` |
| `areaTag` | string | When category = `Area` (display name + id) |
| `sortOrder` | int | Within shelf |

**Cross-listing:** A quest appears in **one** shelf only. HUD tracker and map nav do not depend on shelf — category is for **finding** content in a large campaign.

### 4.2 List + detail behavior (Quests tab)

- Per shelf: list **Active**, **Completed** (turn-in), **Available** (discovered at giver/board), **Failed/Abandoned** — status chips or sub-filters.
- **Badge counts** on shelf rail: e.g. `Side (2 active)` in Gold.
- Detail pane: title, description, objectives, rewards, **linked coordinates**, navigation mode summary (“Search area,” “Marked on map,” “Coordinates only”).
- **Main** shelf: sort by `chainIndex` / act; collapsed chain headers optional.

### 4.3 Right-click context menu (PC); long-press (gamepad)

| Action | Effect |
|--------|--------|
| **Track quest** | Adds to **HUD tracker** if under cap (§5) |
| **Make current quest** | Sets **pinned current** — primary compass/map emphasis for multi-track (optional stronger pulse on that zone/dot) |
| **Stop tracking** / **Remove from tracker** | Removes from HUD; quest stays **Active** in log |
| **Show on map** | Toggles map overlay for that quest’s nav mode (if authored) |

**Track** vs **Make current:** “Current” is one quest id (`pinnedQuestId`); tracked list is ordered set (max 3). Current should appear **top** of HUD stack.

Implementation: extend `QuestProgress` save data — `isHudTracked`, `trackOrder`, `isPinnedCurrent` (or store list on `QuestManager`).

---

## 5. HUD quest tracker (`ActiveQuestHudUI`)

**Anchor:** right side of screen (existing right-middle layout).

### 5.1 Cap: three tracked quests

| Rule | Detail |
|------|--------|
| **Max HUD entries** | **3** quests with `isHudTracked == true` |
| **Chain exception** | A **quest chain** counts as **one HUD slot** for the **parent** quest id |
| **Chain display** | Parent title — **bold**, normal tracker size (~24 px reference) |
| **Chain substeps** | **One** active sub-objective line beneath parent — **~80–85% font size**, Warm Off-White, indented / linked visually (smaller text “linked” under main) |
| **Additional chain steps** | Not stacked as separate HUD rows; advance sub-line when objective index changes |
| **Over cap** | Track attempt → toast: “Tracker full (3). Stop tracking a quest in **Quests**.” |

**Completed** quests on tracker: keep until turn-in (existing); still count toward cap.

### 5.2 Default tracking

- Accepting a quest from giver: **auto-track** if HUD slots available (designer `autoTrackOnAccept` per quest, default true for sides, false for mainline if desired).
- Mainline progress-triggered: **no auto-track** unless player tracks from log or step explicitly enables it.

### 5.3 Interaction with navigation UI

For each **tracked** quest, HUD shows **title + active objective text** only — not duplicate full map. Map/compass read **active navigation** from:

1. `pinnedQuestId` if set, else  
2. First tracked quest in `trackOrder`, else  
3. Most recently updated active quest.

Multiple tracked quests with zones: **pinned/current** quest’s ring/dot takes **gold**; others **muted** Slate outline / smaller dot.

---

## 6. Quest chains (data model)

**`QuestDefinition` extensions (authoring):**

| Field | Purpose |
|-------|---------|
| `chainId` | Shared id for series |
| `chainIndex` | Order in chain |
| `chainParentQuestId` | If set, HUD rolls up under parent |
| `showAsChainChildOnHud` | Subtitle under parent (default true) |

When parent is tracked, **child activations** update the **sub-line** only — do not consume extra HUD slots.

---

## 7. Lore & PPT (non-board quests)

| Kind | Quests tab? | Tracker? |
|------|-------------|----------|
| **Lore step** | Optional — shelf `Area` or `Side`; or **Journal** tab only via Field Log | Usually **not** tracked unless player opts in |
| **PPT moment** | **Journal** dialog chronicle; quest entry only if tied to `Side` / `Area` | **PptBearing** only — no permanent track unless tied to a quest |

PPT integration: on direction resolve, call same **3 s bearing flash** as scanner ping (`PptNpcGestureController` + compass pulse).

---

## 8. Objective types (navigation hooks) — implementation backlog

Extend `QuestObjectiveDefinition` / `QuestObjectiveType`:

| Type | Completes when | Nav |
|------|----------------|-----|
| `ReachSearchZone` | Enter zone + optional ping | `SearchZone` |
| `ReachMarker` | Enter radius of `targetId` marker | `PreciseMarker` |
| `CollectFromEeb` | Field log read with coord entry | Journal |
| `TalkToNpc` | Dialog node complete | Giver may grant next nav |
| `Custom` | Script / WorldState | Per quest |

---

## 9. Save & multiplayer-of-one

Persist: `trackedQuestIds` (max 3), `pinnedQuestId`, `chainHudCollapse` states, plotted search zones, Journal coordinate list.

---

## 10. Do / don’t

| Do | Don’t |
|----|-------|
| Mix coords-only beats with search rings for tension | Force every quest to precise GPS dot |
| Let companions give **approximate** info | Break AC-only / GDD economy in dialog rewards |
| Respect 3-track cap + chain rollup | Show unlimited HUD list |
| Right-click track / untrack | Auto-track everything mainline without player opt-in (package choice) |

---

## 11. Related implementation files

- `Assets/_Project/Scripts/UI/JournalTabRail.cs` — add **Quests** tab; clarify **Journal** vs **Quests** labels  
- `Assets/_Project/Scripts/UI/JournalWindowId.cs` — add `Quests`; migrate quest UI from `JournalQuest` host  
- `Assets/_Project/Scripts/UI/ActiveQuestHudUI.cs` — tracker cap + chain layout  
- `Assets/_Project/Scripts/UI/JournalPanelUI.cs` — Quests window: category shelves + context menu  
- `Assets/_Project/Scripts/Quests/QuestManager.cs`, `QuestDefinition.cs`, `QuestProgress.cs`  
- `Assets/_Project/Scripts/PPT/Runtime/PptDirectionResolver.cs`  
- `Assets/_Project/Documentation/Design/Map_Search_Zone_System.md`  
- `Assets/_Project/Documentation/Design/Field_Log_Devices.md`
