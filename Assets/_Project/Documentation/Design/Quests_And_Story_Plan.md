# Quests & Story Plan

**Status:** Design draft — August 2026  
**Authority:** GDD 5.0 (Appendix A6 story lock; Appendix B disk truth), Io world-content phase map, Kade background plan.  
**Player:** **Kade** (fixed name) — see `Kade_Background_And_Universe_Backstory_Plan.md`.

This document is the **single quests + story roadmap** for Dark Matter: Genesis. It ties the main arc, side quests, prototype quests, and biome campaign order into one place so implementation does not invent competing story tracks.

---

## 1. Design pillars

| Pillar | Rule |
|--------|------|
| **Rumors until proven** | Io history, failed expeditions, aliens, and Aether-9’s past are **suspicions** until Memory Cores / evidence prove otherwise (Kade plan §7). |
| **One quest framework** | Story, side, and procedural missions all use `QuestDefinition` / `QuestManager` (GDD). No parallel quest engines. |
| **Quests are outputs** | Long-term: `StoryDirector` drives beats; QuestManager executes tracked objectives (`Audit_06_Story.md`). |
| **AC only** | Rewards in AC / items / XP / unlocks — never a second currency. New Game starts at **0 AC** (free starter companion). |
| **Aether-9 is the mystery hub** | Not a day-one chatty bot. Colony Ops handles radio until Aether-9 awakens and climbs the trust ladder. |

---

## 2. Main story arc (canon)

### 2.1 Spine

```
New Game (Kade background → free starter companion)
  → Deploy to Io / B6 hub
  → Discover dormant Aether-9 machine (prologue)
  → Repair quest (3 parts) → awaken (hostile → wary)
  → Memory Core loop (find → setpiece → attach → Resonance Event)
  → Trust ladder unlocks advisory radio + deeper lore
  → Campaign biomes escalate toward B7 / story capstone
```

### 2.2 Act map

| Act | Name | Player goal | Story outcome |
|-----|------|-------------|---------------|
| **0** | **Charter** | Choose Kade background + starter companion | Origin flags, synergies, 0 AC, Hard Mode optional |
| **I** | **Prologue — Dormant Machine** | Find Aether-9 shell; accept repair quest | Machine marked; Colony Ops is still the radio voice |
| **II** | **Repair** | Recover **3** repair objects; restore power/core/interface | Aether-9 awakens angry; first true lore dump (still incomplete) |
| **III** | **Memory Cores** | Recover primary cores (design target: **≥3**) | Each attach → Resonance Event (10–15 min world change) + fragment |
| **IV** | **Trust & Advisory** | Raise Aether-9 trust (wary → advisor → friend) | Trust-gated radio; tips; companion chat relay later |
| **V** | **Capstone** | B7 / deep Resonance thread | Resolve “what happened on Io” with evidence, not rumor |

### 2.3 Aether-9 beats (GDD A6)

1. **Discovery** — Idle machine / probe shell; interact starts repair quest.  
2. **Awakening** — Slightly hostile; group killed by something sinister; Memory Cores once housed here.  
3. **Core loop** — Find → recovery setpiece → slot into Aether-9 → Resonance Event + unlocks.  
4. **Trust ladder** — Angry → wary → advisor → friend (unprompted radio later).  
5. **Identity** — Aether-9 **is** an Echo sealed in a machine — distinct from rescueable surface Echoes.

### 2.4 Resonance Events (constraints)

- Duration band: **10–15 minutes** world change.  
- May spike storms, damage Command Center, injure (not kill) base-22, spawn Echo opportunities.  
- Weather lock: Resonance Supercell can force **FULL PAUSE** (same shelter rules as sulfur storms).  
- Player-facing copy stays evidence-based; writer canon stays internal until cores prove it (Kade §7.8).

---

## 3. Campaign biome order (story geography)

From `Io_World_Content_Phase_Map.md` — do not reorder without updating that map + GDD.

| Order | Biome | Story role |
|-------|-------|------------|
| 1 | **B6 Basalt Highlands** | Hub, tutorial underground grammar, early Aether-9 discovery |
| 2 | **B1** | First expedition ring; ecology + early side quests |
| 3 | **B2** | Mid-ring pressure; deeper tubes |
| 4 | **B3** | Path/vehicle tags; mid-campaign |
| 5 | **B5 Polar Radiation Flats** | Story branch — rad/cold; Memory Core bias |
| 6 | **B4 Calderas** | Volatile / living-world pressure after Polar |
| 7 | **B7** | Post–Memory Core thread / capstone |

**Overlay:** Expedition Graveyard (wrecks, androids, Rust Gardens) across B1–B6 — environmental storytelling, not a separate act.

---

## 4. Side quests (named + templates)

### 4.1 Named side quests (design tickets)

| ID / Name | Biome | Purpose | Reward / unlock |
|-----------|-------|---------|-----------------|
| **Lost Survey** | B6 | Find / repair survey beacon | Pet: **Beacon Hopper** (C11) |
| *(Polar pet quest — TBD)* | B5 | Capture / rescue polar lifeform | Polar Skimmer / related pet |
| Class tutorial quests | B6→B1 | Soft onboarding for companion classes | Skill tip + small AC |

Additional named side quests land via `Io_World_Content_Milestone_Tickets.md` (IO-W\* narrative tickets). Prefer **one clear verb** per quest (gather, deliver, repair, escort, scan, recover).

### 4.2 Activity templates (reuse)

From `Io_Biome_Exploration_Gameplay_Plan.md` — author story and procedural missions from the same verbs:

- Relay / cover-to-cover  
- Probe repair / uplink  
- Scan grove / anomaly  
- Recover crate / Memory Core fragment  
- Escort / extract Echo signal  
- Storm shelter / crisis assist  

Procedural missions may use dynamic achievement-style goals (`DynamicAchievementGenerator`) but **story missions stay authored** `QuestDefinition` assets.

---

## 5. Prototype quests (disk truth — shipped now)

Runtime assets under `Assets/_Project/Resources/Quests/`:

| Asset / id | Type | Notes |
|------------|------|-------|
| `GatherRocks` (`gather_rocks`) | CollectItem | Collect 3 Rocks → AC |
| `Get more Rocks` | CollectItem | Collect 10 Rocks → item + AC |
| `GuideSupplyRun` (`guide_supply_run`) | Talk + gather | Companion Guide stew / mushrooms |
| `One_More` | Prototype | Additional board filler |

**Quest giver:** `QuestGiver_PioneerGuide` — **temporary stand-in**. Replace with idle **Aether-9** machine interactable for Act I–II. Do not invent a second permanent human quest-hub NPC.

**HUD / UI already live:** `ActiveQuestHudUI`, Journal quest preview, `QuestGiverDialogUI`.

---

## 6. Implementation backlog (story track)

Ordered for value — aligns with GDD B3/B4 and World Engine spine.

| Priority | Work | Depends on |
|----------|------|------------|
| P0 | Keep prototype board working in playable scene | — |
| P1 | Aether-9 idle machine prefab + interact → repair quest (3 objectives) | QuestDefinition assets |
| P2 | Awaken dialogue + trust state on save (`GameSaveData`) | P1 |
| P3 | First Memory Core recovery + attach UX + stub Resonance Event | P2 |
| P4 | Colony Ops → Aether-9 advisory handoff (comms) | Communications runtime |
| P5 | Lost Survey side quest + Beacon Hopper grant | B6 content |
| P6 | `StoryDirector` + migrate QuestManager behind `IQuestCommandService` | HLA / Audit_06 |
| P7 | Full Resonance Events + B5→B4 story branch + B7 capstone | Biome world |

**Out of scope for this plan file:** full Dialogue UI rewrite, LLM Phase 9+, marketplace Echo trading (forbidden).

---

## 7. New-game → first quest flow

```
Main Menu → New Game
  → Kade Background Select (+ Hard Mode)     [Kade plan — not coded yet]
  → Free Starter Companion Select (0 AC)     [exists; still uses 5000 AC grant — align to 0]
  → Welcome / controls
  → Deploy (playable Genesis scene)
  → Optional: accept prototype board quests from Pioneer Guide
  → Story gate: discover Aether-9 (when P1 lands) → repair quest becomes primary
```

Background choice feeds **rumor knowledge** and **comms tone** only at first; it does not skip Memory Cores or change the repair object list.

---

## 8. Data & save hooks (story)

| Field / system | Purpose |
|----------------|---------|
| `GameSaveData` quest lists | Active / completed / turned-in ids (existing) |
| `playerBackgroundId` / `hardModeEnabled` | Kade plan — add with background system |
| `aether9TrustTier` / `aether9Awakened` | Future — trust ladder |
| `memoryCoresAttached[]` | Future — Resonance progression |
| `StoryWorldStateProvider` | Expose chapter + background for dialogue / directors |
| `QuestRegistry` | Authored catalog under Resources/Quests |

---

## 9. Related docs (do not duplicate)

| Doc | Owns |
|-----|------|
| `GAME_DESIGN_DOCUMENT_5.0.txt` | Canon locks (A6, economy, platforms) |
| `Kade_Background_And_Universe_Backstory_Plan.md` | Origins, Hard Mode, rumor rules, companion synergies |
| `Io_World_Content_Phase_Map.md` | Biome unlock phases + pet quest tickets |
| `Io_Biome_Exploration_Gameplay_Plan.md` | Biome verbs + activity templates |
| `GAME_BREAKDOWN.txt` §11 | What is coded today |
| `Audit_06_Story.md` | Architecture migrate path to StoryDirector |
| `World_Engine_Disk_Status.md` | Disk truth before claiming shipped |

---

## 10. Acceptance checks

A story/quests pass is “done enough” when:

1. One written spine (this file) matches GDD A6 and Io campaign order.  
2. Prototype quests still complete in Editor Play Mode.  
3. Pioneer Guide is labeled temporary; Aether-9 repair quest assets exist or are ticketed.  
4. New Game economy matches **0 AC + free companion** (code + GDD A8).  
5. No player-facing lore contradicts the rumor-until-cores rule.

---

*Supersedes scattered one-off story notes in agent chats. Update this file when acts, named side quests, or Aether-9 trust steps change.*
