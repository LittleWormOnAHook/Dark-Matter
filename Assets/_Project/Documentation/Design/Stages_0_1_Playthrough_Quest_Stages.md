# Stages 0–1 — Playthrough Flow & Quest Task Stages

**Status:** Design draft — August 2026  
**Covers:** **Stage 0 (Charter)** + **Stage 1 (Landing & Camp)** only  
**Parents:** `Acts_0_to_3_Asset_Mapped_Plan.md` · `Prologue_Playthrough_Step_By_Step.md` · `Prologue_Acts_Expanded.md`  
**Scope:** Mainline only. Side quests off. Target contribution to the 2–5 h prologue: **~1.5–3.0 hours** for Stages 0–1 together.  
**Legend:** **CURRENT** / **EXTEND** / **FUTURE** · Quest status flow: Locked → Available → Active → Completed → TurnedIn

---

## How to read this doc

Each **quest** is broken into:

1. **Flow** — player-facing sequence  
2. **Task stages** — S1, S2… with objective type, count, fail/recovery  
3. **UI / VO** — what appears on screen / radio  
4. **Assets** — CURRENT vs FUTURE  
5. **Exit gate** — what unlocks next  

---

# STAGE 0 — Charter

**Duration:** 5–10 min  
**Location:** Menus → cinematic → fade to Stage 1 spawn  
**No combat. No inventory grind.**

## Stage 0 master flow

```
Main Menu
  -> [Q0.1] New Game / slot
  -> [Q0.2] Kade Background Select (+ Hard Mode)
  -> [Q0.3] Free Starter Companion Select
  -> [Q0.4] Controls / Welcome acknowledge
  -> [Q0.5] Shuttle descent cinematic
  -> SPAWN Stage 1 Scene A (Landing Scar)
```

---

## Q0.1 — New Expedition

**Id:** `charter_01_new_game` (flow beat; may be code-only, not QuestDefinition)  
**Type:** System  

### Task stages

| Stage | Player task | System result | Fail / back |
|-------|-------------|---------------|-------------|
| S1 | Choose **New Game** / New Expedition | Enter charter pipeline | Return main menu |
| S2 | Pick save slot (if multi-slot) | Slot reserved | Cancel → menu |
| S3 | Init empty save | `aetherCredits=0`, no cores, `aether9Awakened=false`, starter unset | — |

### UI / VO

- Main menu button highlight.  
- No Ops VO yet.

### Assets

| Tag | Asset |
|-----|--------|
| CURRENT | `MainMenuController`, `GameSession`, `GameSaveSystem` |
| EXTEND | Ensure new game does **not** grant 5000 AC |
| FUTURE | Optional charter splash (“Basalt Highlands Charter”) |

### Exit

→ Q0.2 available.

---

## Q0.2 — Kade Background

**Id:** `charter_02_background` (**FUTURE** UI + SO; design-locked)  
**Type:** Choice  

### Flow

```
Show 6 background cards
  -> Optional expand "Rumors you've heard"
  -> Toggle Hard Mode
  -> Confirm
  -> Write save flags
```

### Task stages

| Stage | Player task | Objective detail | Notes |
|-------|-------------|------------------|-------|
| S1 | Browse backgrounds | Hover/focus each card; read tagline | Gamepad: d-pad / stick |
| S2 | (Optional) Open rumor panel | Expand body text | Rumor-safe Io lines only |
| S3 | Review grants | Stats / skills / kit summary visible | Power band equal across BGs |
| S4 | Set Hard Mode | Off default; On = −20% Kade damage | Same kit, same 0 AC |
| S5 | Confirm background | Hold confirm or double-confirm if Hard Mode on | Writes `playerBackgroundId`, `hardModeEnabled` |

### UI / VO

- Title: **“Who was Kade before Io?”**  
- Hard Mode label: **“Hard Mode (−20% Kade damage)”**  
- No Aether-9, no “ten cores,” no alien confirmation.

### Assets

| Tag | Asset |
|-----|--------|
| CURRENT | `ShiftUiTheme` / palette for cards |
| FUTURE | `KadeBackgroundSelectUI`, 6× `KadeBackgroundDefinition`, portraits |
| EXTEND | `GameSaveData` fields |

### Exit

→ Q0.3; companion list may highlight synergies for chosen BG.

---

## Q0.3 — Free Starter Companion

**Id:** `charter_03_starter_companion`  
**Type:** Choice (EXTEND existing UI)

### Flow

```
Show starter offers (CURRENT catalog)
  -> Highlight synergy matches from Q0.2
  -> Preview Kade bonus if match
  -> Recruit at 0 AC
  -> Companion flagged for Landing Scar spawn
```

### Task stages

| Stage | Player task | Detail |
|-------|-------------|--------|
| S1 | View offers | CURRENT: Kael-9 and other `StarterPioneerCatalog` entries |
| S2 | Read class / traits | CombatTactician, etc. |
| S3 | See synergy badge | If class ∈ background `companionSynergies` → gold badge + bonus preview |
| S4 | Confirm recruit | **Cost 0**; no “not enough AC” |
| S5 | Apply synergy | If matched: apply stat bonuses + 1 free skill rank (**FUTURE** apply hook; until then show preview only) |

### UI / VO

- Header: **“Choose your first Skilled Companion — Charter covers the cost.”**  
- Strike through any 5000 AC label (EXTEND).

### Assets

| Tag | Asset |
|-----|--------|
| CURRENT | `StarterPioneerSelectUI`, `StarterPioneerCatalog`, `PioneerRosterManager` |
| EXTEND | `StarterAcGrant = 0`, `acCost = 0`, flow order after background |
| FUTURE | Synergy apply on `PlayerProgressionManager` / `SurvivalStats` |

### Exit

→ Q0.4; `StarterPioneerSelected = true`.

---

## Q0.4 — Controls / Welcome

**Id:** `charter_04_controls`  
**Type:** Gate  

### Task stages

| Stage | Player task | Detail |
|-------|-------------|--------|
| S1 | Open welcome | CURRENT `GameStartPopup` |
| S2 | Review KBM + gamepad | Movement, interact E, journal, hotbar |
| S3 | Press Start / Continue | Clears gate (`GameSession`) |

### Assets

| Tag | Asset |
|-----|--------|
| CURRENT | `GameStartPopup`, controls reference art if present |
| EXTEND | Mention Hard Mode only if enabled (“damage reduced”) |

### Exit

→ Q0.5 cinematic (or loading overlay into cinematic).

---

## Q0.5 — Shuttle Descent

**Id:** `charter_05_descent` (**FUTURE** Timeline; stand-in: loading overlay)

### Task stages

| Stage | Player task | Detail |
|-------|-------------|--------|
| S1 | Watch descent | Io haze, basalt, Ops VO charter line |
| S2 | Impact beat | Shake / blackout |
| S3 | Fade up | Landing Scar; companion at cargo hatch |

### VO (Ops)

> “Charter drop to Basalt Highlands. Establish camp. Report.”

### Assets

| Tag | Asset |
|-----|--------|
| CURRENT | `LoadingOverlayController`, starfield mat (veil stand-in) |
| FUTURE | Timeline + shuttle exterior + VO clip |

### Stage 0 exit checklist

- [ ] Background + Hard Mode saved  
- [ ] Companion free-recruited  
- [ ] Controls acknowledged  
- [ ] Player + companion exist at Landing Scar  
- [ ] Credits = 0  
- [ ] `prologue_01_touchdown` → **Available**

---

# STAGE 1 — Landing & Camp

**Duration:** ~1.5–2.75 h  
**Quests (chain):** `prologue_01` → `02` → `03` → `04`  
**Auto-start:** `prologue_01` on spawn (or Ops radio push).

## Stage 1 master flow

```
[prologue_01] Touchdown (Landing Scar)
  -> [prologue_02] Scavenge the Ring
  -> [prologue_03] Claim the Plateau
  -> [prologue_04] Raise the Camp
  -> UNLOCK Stage 2 Field Certification
```

Journal always shows **one** primary active quest; completed stay in log.

---

# Quest P1 — Touchdown

**Quest id:** `prologue_01_touchdown` (**FUTURE** asset)  
**Giver:** Colony Ops (radio) — not Pioneer Guide  
**Zone:** Landing Scar  
**Target time:** 15–25 min (excl. Stage 0)  
**XP intent:** nudge toward Level 2  

## Playthrough flow

```
Spawn at shuttle
  -> Leave heat cone
  -> Loot Emergency Crate
  -> Follow ping to Survey Stake Alpha
  -> Scan stake
  -> (Optional) shoo moths
  -> Turn-in auto / Ops confirm
  -> Unlock Resource Ring markers
```

## Task stages (detailed)

### P1-S1 — Survive the pad

| Field | Value |
|-------|--------|
| Objective type | Custom / ReachLocation (safe volume) |
| Task | Exit **Thruster Heat Cone** |
| Fail | Thermal damage ticks; companion bark if idle >3s in cone |
| Recovery | Walk out; no quest fail |
| Teach | Thermal HUD |
| Assets | CURRENT exposure/thermal · FUTURE heat volume on shuttle prefab |

### P1-S2 — Emergency crate

| Field | Value |
|-------|--------|
| Objective type | CollectItem / Interact |
| Task | Open **Emergency Crate** |
| Grants (design) | `Oxygen Tank Mini` ×1, `Medpack` ×1–2, starter tool / rock pick (CURRENT items where possible) |
| Fail | None |
| UI | “Loot Emergency Crate” |
| Assets | CURRENT consumable ItemData · FUTURE crate prop on shuttle |

### P1-S3 — Reach Survey Stake Alpha

| Field | Value |
|-------|--------|
| Objective type | ReachLocation |
| Task | Enter stake trigger (50–80 m north corridor) |
| Marker | Ops ping + minimap |
| Fail | Soft — player can wander; marker persists |
| Assets | CURRENT `MapUI` / ping · FUTURE stake POI + corridor blockout |

### P1-S4 — Scan the stake

| Field | Value |
|-------|--------|
| Objective type | Custom (Scan) |
| Task | Complete scan / optics pulse on stake |
| Result | Unlocks Resource Ring fog/markers; plays Ops line |
| Fail | Partial scan — must finish channel |
| Teach | Scanner / optics lite |
| Assets | CURRENT optics/scanner stack · FUTURE stake scan socket |

### P1-S5 — Contact fauna (soft)

| Field | Value |
|-------|--------|
| Objective type | Optional Custom OR KillEnemy (0 required) |
| Task | Encounter 1–2 weak moths / skitters |
| Stand-in | CURRENT `Ember_Skitter` |
| Future | Cave Scout Moth |
| Note | Not required to kill to complete quest if optional; if required, Kill 1 |

### P1 — Rewards & turn-in

| Reward | Value |
|--------|--------|
| XP | Small (landing band) |
| AC | 0–25 (optional; keep low) |
| Items | — |
| Unlock | `prologue_02` Available; Resource Ring marked |

**VO Ops:** *“Telemetry noisy. Stake Alpha live — scavenge the ring. Take what still reads clean.”*

### P1 exit gate

- [ ] Heat left once  
- [ ] Crate looted  
- [ ] Stake scanned  
- [ ] Ring visible on map  

---

# Quest P2 — Scavenge the Ring

**Quest id:** `prologue_02_scavenge` (**FUTURE**)  
**Zone:** Resource Ring  
**Target time:** 25–40 min  
**XP intent:** Level 2–3  

## Playthrough flow

```
Enter Resource Ring
  -> Gather Basalt / Scrap / Sulfur (can interleave)
  -> Power Fabricator (relay puzzle)
  -> Craft survival consumable
  -> Defeat Tube Jackals x2
  -> Return to shuttle cargo
  -> Receive Camp Beacon Kit
```

## Task stages (detailed)

### P2-S1 — Enter the ring

| Field | Value |
|-------|--------|
| Type | ReachLocation |
| Task | Cross Resource Ring entry trigger |
| VO | Ops: prior survey scrap |

### P2-S2 — Gather basalt

| Field | Value |
|-------|--------|
| Type | CollectItem |
| Item | Prefer CURRENT `Rock` / Mining resource nodes; display name **Basalt Chunk** (EXTEND rename or alias) |
| Count | **8** |
| Nodes | Quest-tagged; glow while active |
| Teach | Gather + inventory |

### P2-S3 — Gather scrap alloy

| Field | Value |
|-------|--------|
| Type | CollectItem |
| Item | **Scrap Alloy** (**FUTURE** ItemData; stand-in: any CURRENT salvage/resource) |
| Count | **5** |
| Nodes | Scrap piles along creek |

### P2-S4 — Gather sulfur crystal

| Field | Value |
|-------|--------|
| Type | CollectItem |
| Item | **Sulfur Crystal** (**FUTURE**; stand-in: harvest node) |
| Count | **3** |
| Hazard | Creek edge thermal/cold tick — teach edge awareness |
| Order | S2–S4 tracked in parallel (any order) |

**UI objective list example:**

```
[ ] Basalt Chunk  3/8
[ ] Scrap Alloy   5/5
[ ] Sulfur Crystal 1/3
```

### P2-S5 — Power Breaker puzzle

| Field | Value |
|-------|--------|
| Type | Custom |
| Task | Restore power to Portable Fabricator (3 relays) |
| Rules | Correct cable order; wrong = spark reset |
| Hint | After 2 fails OR 90s: Ops highlights next relay |
| Teach | Light puzzle / device interact |
| Assets | FUTURE fabricator POI + puzzle MB |

### P2-S6 — First craft

| Field | Value |
|-------|--------|
| Type | CraftItem |
| Task | Craft **one** of: Field Rations / Patch Kit |
| CURRENT stand-ins | `Pimican`, `Cooked Mushroom`, `Medpack`, `Bio_Gel` via Workbench/Cooking |
| FUTURE | Dedicated Field Rations recipe on fabricator |
| Station | Portable Fabricator (Workbench type) |
| Teach | Craft UI |

### P2-S7 — First fight

| Field | Value |
|-------|--------|
| Type | KillEnemy |
| Task | Defeat **Tube Jackal ×2** |
| Stand-in | CURRENT `Sulfur_Hound` / skitter retune |
| FUTURE | `Enemy_TubeJackal` |
| Aggro | Open gather too long / scent line |
| Companion | Can tank 1 |
| Teach | Hotbar combat + stamina |
| Fail | Death → respawn at shuttle; jackals reset; gather progress kept |

### P2-S8 — Camp Beacon Kit

| Field | Value |
|-------|--------|
| Type | TalkTo / Interact (shuttle cargo) |
| Task | Return to shuttle cargo hatch |
| Grant | `Item_CampBeaconKit` (**FUTURE**) |
| VO | Ops: *“Find high ground. Anchor the colony.”* |
| Unlock | Plateau marker; `prologue_03` Available |

### P2 — Rewards

| Reward | Value |
|--------|--------|
| XP | Mid band |
| AC | ~50–100 optional |
| Item | Camp Beacon Kit |
| Recipe | Optional unlock: salvage plates |

### P2 exit gate

- [ ] All three gather counts complete  
- [ ] Fabricator powered + one craft done  
- [ ] 2 jackals down  
- [ ] Camp Beacon Kit in inventory  

---

# Quest P3 — Claim the Plateau

**Quest id:** `prologue_03_claim_site` (**FUTURE**)  
**Zone:** Camp Plateau (+ return path to shuttle for cell)  
**Target time:** 25–40 min  

## Playthrough flow

```
Travel to Camp Plateau
  -> Solve collapsed bridge (plates OR companion boost)
  -> Clear Brood Mouth nest
  -> Place Command Center Seed (Camp Beacon Kit)
  -> Fetch Emergency Cell from shuttle (carry)
  -> Insert cell / power seed
  -> Open Building Control Panel (Overview)
```

## Task stages (detailed)

### P3-S1 — Reach Camp Plateau

| Field | Value |
|-------|--------|
| Type | ReachLocation |
| Task | Enter mesa volume |
| VO | Ops: elevation / storm profile |

### P3-S2 — Bridge access

| Field | Value |
|-------|--------|
| Type | Custom (multi-solution) |
| Task A | Craft + deploy **Salvage Plates ×2** on gap |
| Task B | Companion **Boost** hold-interact |
| Complete when | Either A or B succeeds |
| Teach | Craft-for-traversal / companion interact |
| Assets | FUTURE plates + bridge; companion boost prompt EXTEND |

### P3-S3 — Clear the pad

| Field | Value |
|-------|--------|
| Type | KillEnemy |
| Task | Destroy **Brood Mouth** nest / kill elite |
| Stand-in | CURRENT larger hound or humanoid elite |
| FUTURE | `Enemy_BroodMouth` |
| Gate | Cannot place CC Seed while nest alive (toast) |

### P3-S4 — Place Command Center Seed

| Field | Value |
|-------|--------|
| Type | Custom (Build/Place) |
| Task | Use Camp Beacon Kit; snap ghost to Helix pad |
| Invalid | Red ghost off-pad |
| Result | `Building_CommandCenter_Seed` instance (**FUTURE**) |
| Teach | Lite Building placement |
| CURRENT stand-in | Place prop + attach `BuildingControlPanel` |

### P3-S5 — Retrieve Emergency Cell

| Field | Value |
|-------|--------|
| Type | CollectItem / Interact |
| Task | Take **Emergency Cell** from shuttle socket |
| Carry rules | Move speed down; sprint limited; **drop on hit** |
| Marker | Shuttle + return path |
| Assets | FUTURE `Item_EmergencyCell` world carry |

### P3-S6 — Power the seed

| Field | Value |
|-------|--------|
| Type | Custom / Interact |
| Task | Insert cell into seed power socket |
| If dropped | Pick up again; cell respawns at shuttle after 30s if lost in ravine |
| Result | Seed powered; lights on |
| CURRENT | Hook `PowerConsumer` / generator pattern |

### P3-S7 — Open BCP Overview

| Field | Value |
|-------|--------|
| Type | Interact |
| Task | Use terminal E → Building Control Panel |
| UI | **Overview** tab only (Companions/Craft gated until P4) |
| Teach | BCP habit |
| Assets | CURRENT `BuildingControlPanel.cs` |

### P3 — Rewards

| Reward | Value |
|--------|--------|
| XP | Mid |
| Unlock | Shelter + Station build recipes / blueprints for P4 |
| Quest | `prologue_04` Available |

### P3 exit gate

- [ ] On mesa  
- [ ] Bridge solved  
- [ ] Nest clear  
- [ ] Seed placed + powered  
- [ ] BCP Overview opened once  

---

# Quest P4 — Raise the Camp

**Quest id:** `prologue_04_bootstrap` (**FUTURE**)  
**Zone:** Camp Plateau  
**Target time:** 30–50 min  
**XP intent:** Approach Level 3–4 (Stage 2 will push to 5)

## Playthrough flow

```
Build Survival Shelter
  -> Build Crafting Station
  -> Assign companion on BCP Companions tab
  -> Craft Reinforced Framing x4 (gather rim if needed)
  -> Craft + mount Oxygen Scrubber
  -> Survive Mini Sulfur Gust (shelter)
  -> Ops unlocks Field Certification (Stage 2)
```

## Task stages (detailed)

### P4-S1 — Survival Shelter

| Field | Value |
|-------|--------|
| Type | Custom (Build) / Craft+Place |
| Task | Place **Survival Shelter** module |
| CURRENT stand-in | `Shelter_Safe_Zone.prefab` |
| FUTURE | `Building_SurvivalShelter` |
| Proof | Shelter footprint active + interior volume |

### P4-S2 — Crafting Station

| Field | Value |
|-------|--------|
| Type | Custom (Build) |
| Task | Place settlement **Crafting Station** |
| CURRENT stand-in | Scene `CraftingStation` Workbench via bootstrap |
| FUTURE | Player-placed settlement station |
| Teach | Journal = recipe library; station = craft |

### P4-S3 — Assign companion

| Field | Value |
|-------|--------|
| Type | Custom |
| Task | BCP → **Companions** → assign starter to camp role |
| UI pulse | If skipped, objective highlights tab |
| Assets | CURRENT BCP + assignment hints |
| Teach | GDD Building Control Companions tab |

### P4-S4 — Reinforced Framing ×4

| Field | Value |
|-------|--------|
| Type | CraftItem |
| Task | Craft **Reinforced Framing ×4** |
| Inputs | Rim basalt/scrap (second gather loop OK) |
| Recipe | FUTURE blueprint; stand-in craft any “structure part” ×4 |
| Friction | Intentional mat shortfall once |

### P4-S5 — Oxygen Scrubber

| Field | Value |
|-------|--------|
| Type | CraftItem + Interact (mount) |
| Task | Craft scrubber parts; mount on rim socket |
| Effect | Soft O₂ relief near camp |
| CURRENT | May use `Oxygen Tank` lore; FUTURE module prefab |
| Optional item tie | CURRENT `Oxygen Tank Mini` as temp scrubber fuel demo |

### P4-S6 — Mini Sulfur Gust

| Field | Value |
|-------|--------|
| Type | Custom / Survive |
| Task | Endure **60–90 s** gust |
| Rules | Enter shelter (or safe zone); crisis HUD on; craft queues pause if any |
| Fail | Outside damage; **retry once** — do not brick quest |
| VO | Ops: *“Gust cell. Get inside.”* |
| Assets | CURRENT `EnvironmentalCrisisHudMode` · EXTEND scripted gust · CURRENT `Shelter_Safe_Zone` |

### P4-S7 — Charter checkpoint (turn-in)

| Field | Value |
|-------|--------|
| Type | TalkTo / Auto |
| Task | Ops confirms foothold |
| Unlock | Stage 2 `prologue_05_field_cert` Available |
| Tracker | Journal: “Camp Charter — Complete” |

### P4 — Rewards

| Reward | Value |
|--------|--------|
| XP | Large-for-stage (push toward Lv4) |
| AC | ~100–150 optional |
| Recipe | Settlement recipes for Stage 2 yard tools (locked behind Lv5 still) |
| Flag | `campBootstrapComplete = true` |

### P4 / Stage 1 exit checklist

- [ ] Shelter built  
- [ ] Crafting Station built  
- [ ] Companion assigned on BCP  
- [ ] Framing ×4 crafted  
- [ ] Scrubber mounted  
- [ ] Mini gust survived  
- [ ] Stage 2 cert quest unlocked  

---

## Stage 0–1 HUD / Journal snapshots

### After Stage 0

```
Active: (none) — cinematic
Party: Kade + [Starter]
AC: 0
```

### During P2 (example)

```
Active: Scavenge the Ring
  [====----] Basalt 4/8
  [========] Scrap 5/5
  [==------] Sulfur 1/3
  [ ] Power Fabricator
  [ ] Craft field supply
  [ ] Defeat Jackals 0/2
  [ ] Collect Camp Beacon Kit
```

### End of P4

```
Active: (none) / next: Field Certification
Camp: CC Seed ONLINE | Shelter OK | Station OK
Companion: Assigned
Crisis: Gust cleared
```

---

## Quest dependency graph (0–1)

```
charter_01_new_game
  -> charter_02_background
  -> charter_03_starter_companion
  -> charter_04_controls
  -> charter_05_descent
  -> prologue_01_touchdown
  -> prologue_02_scavenge
  -> prologue_03_claim_site
  -> prologue_04_bootstrap
  -> prologue_05_field_cert   (Stage 2 — not detailed here)
```

No branching required for mainline. Background only changes VO flavor lines and starting kit ids inside the same stages.

---

## Authoring checklist (QuestDefinition fields)

For each `prologue_0X` asset, author:

| Field | Example |
|-------|---------|
| `questId` | `prologue_02_scavenge` |
| Title | Scavenge the Ring |
| Objectives[] | Typed stages above |
| Rewards | XP / AC / items |
| UnlockNext | Next quest id |
| ZoneHint | Landing Scar / Resource Ring / Plateau |
| FailPolicy | Preserve gather progress on death |

---

## Prototype stand-in matrix (ship on Genesis now)

| Stage need | Use CURRENT |
|------------|-------------|
| Oxygen / meds | `Oxygen Tank Mini`, `Medpack` |
| Food craft | `Pimican`, `Cooked Mushroom`, `Forest Stew` |
| Gather | `Rock` + Mining/Harvest folders |
| Jackals | `Sulfur_Hound` / `Ember_Skitter` |
| Shelter | `Shelter_Safe_Zone` |
| Craft | Workbench `CraftingStation` |
| BCP | `BuildingControlPanel` on prop |
| Quest push | Temporary `QuestGiver_PioneerGuide` lines **or** Ops radio-only objectives |
| Gust | Toggle `EnvironmentalCrisisHudMode` via debug/script |

---

*Stage 0–1 quest/task bible. Stage 2–3 remain in `Acts_0_to_3_Asset_Mapped_Plan.md` and prologue step sheet until expanded the same way.*
