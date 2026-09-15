# Field Log Devices (Io Recordings)

**Project:** Dark Matter: Genesis  
**Status:** Design lock (Sep 2026) — diegetic lore journal distinct from Quest board and Kairos radio  
**Player:** Recordings are **left by others**; Kade collects and replays them in Journal  
**Tone range:** informational, mundane ops, mysterious, bitter, desperate, stranded, hopeful, corporate, smuggler, precursor-adjacent (no Kairos name until prologue end in mainline)

---

## 1. Purpose

Small **electronic field devices** (slates, pucks, helmet chips, survey spikes with memory) are scattered across Io. Each holds **one or more audio/text logs** from prior explorers, colonists, androids, or machines. This is the **primary environmental story channel** for:

- Why people came to Io  
- Why they stayed or failed  
- Why they **cannot leave** (orbit slot lost, charter void, storm, sabotage, infection, guilt)  
- Attitudes toward conditions — **wide range** (clinical, angry, poetic, broken, joking)

**Not** a replacement for Echo chronicle or Memory Cores — Field Logs are **human-scale** fragments; Kairos cores are **plot spine**.

---

## 2. Player UX

| Action | Behavior |
|--------|----------|
| **Find device** | World interact on prop (`DMFieldLogDevice`) |
| **Collect** | Adds entry to Journal → new tab **`Field Logs`** (or sub-tab under Journal Quest) |
| **Replay** | List UI: title, speaker tag, biome, optional transcript; play audio + scroll text |
| **Duplicate** | First pickup only; replays from Journal |

Devices can be **required** (intro tutorial log in shuttle) or **optional** (90% of catalog).

**UI palette:** Dark Navy panels, Warm Off-White transcript, Gold for “new log” dot, Soft Beige-Gray metadata.

---

## 3. Content taxonomy

| Category | Examples | Frequency |
|----------|----------|-----------|
| **Ops / informational** | Vent schedules, suit maintenance, grid coords (flavor, not quest GPS) | Common |
| **Personal** | Letters home, argument between crew | Common |
| **Mystery** | Cuts mid-sentence, wrong timestamps, third voice | Uncommon |
| **Stranded** | “Rescue window closed,” “orbit isn’t answering” | Common |
| **Desperate** | Ration counts, moral collapse (handle with care — not torture porn) | Uncommon |
| **Hostile Io** | Sulfur storm, fauna, android still running | Common |
| **Corporate / charter** | UEA, Helix, contract legalese (package variants) | Medium |
| **Foreshadow** | Precursor hum, “don’t wake the shell” | Rare |

Tag each asset: `biome`, `act`, `packageSpine` (V1–V4 optional lines), `spoilerTier`.

---

## 4. Authorship voices (rotate)

- **Survey lead** — competent, tired  
- **Medic** — clinical → frayed  
- **Smuggler** — bitter humor  
- **Android** — literal, tragic misread of empty hab  
- **Child/guardian** — rare, high impact (one per act max)  
- **Kade** — only in intro optional “personal note” if authored; no spam  

Avoid single-tone “everyone hates Io.” Mix **gallows hope** (V2) with **horror** (V1) via placement, not one voice.

---

## 5. Data model (implementation target)

**`FieldLogDefinition`** (ScriptableObject):

| Field | Notes |
|-------|-------|
| `logId` | Save / achievement key |
| `displayTitle` | Journal list |
| `speakerLabel` | “UEA Survey — Reyes” |
| `transcript` | Localized string |
| `audioClip` | Optional VO |
| `duration` | UI |
| `categories` | Flags for filter |
| `prerequisiteWorldState` | Optional gate |

**`DMFieldLogDevice`** (MonoBehaviour):

- Reference `FieldLogDefinition` or inline bundle  
- `destroyOnCollect` vs persistent prop  
- Optional link to `MapSearchZone` (log inside search ring)

**Save:** `HashSet<string> collectedLogIds` in `GameSaveData`.

---

## 6. Journal integration

Add **`JournalWindowId.FieldLogs`** (implementation backlog) or nest under **JournalQuest** as “Recordings” until tab exists.

List sort: **newest first**, filter by biome/category.

Cross-link: Echo chronicle entries may **cite** log IDs (“matches Field Log 12-B”).

---

## 7. Intro placement (crash → horizon)

| Device | Location | Content sketch |
|--------|----------|----------------|
| **Shuttle EEB** | Crash crate / cockpit | UEA safety + “horizon pad is live” — teaches Field Log UI |
| **Wreck lane** | Optional outside | Prior survey team: “geomagnetic drops aren’t myth” |
| **Corral 2** | Broken pen | Rancher joke turns dark — fauna not livestock |
| **Horizon pad** | Dead beacon housing | Empty camp schedule; **nobody boarded** |

First pickup during **crate salvage** before leaving shuttle (see intro doc).

---

## 8. Production rules

| Rule | Rationale |
|------|-----------|
| **~30–90 s** per log average | Skimmable; binge optional |
| **Transcript always** | Accessibility + console |
| **No mandatory log for progression** | Except intro EEB if used as tutorial |
| **AC-only / no loot power creep** | Logs are lore; rare log may unlock codex skin only |
| **Protect ArtReference** | Device prop art separate from Life Sheets |

---

## 9. Volume targets (campaign)

| Scope | Target |
|-------|--------|
| Intro slice | 3–5 devices |
| Per biome | 8–15 |
| Underground | 5–10 per stratum |
| Unique / mystery chain | 1 arc × 6 linked logs |

---

## 10. Related files

- `Prologue_Intro_Crash_To_Horizon.md` — crash loot + first logs  
- `Map_Search_Zone_System.md` — logs often inside search rings  
- `Narrative_Package_V1_Ash_And_Signal.md` — silence / signal horror tone  
- `GAME_DESIGN_DOCUMENT_5.0.txt` — Echo vs liaison / Kairos naming
