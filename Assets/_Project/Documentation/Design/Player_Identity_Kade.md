# Player Identity — Kade (Locked Canon)

**Game:** Dark Matter: Genesis  
**Status:** Locked Aug 2026 — applies globally across GDD, narrative packages V1–V4, scripts, and docs  
**Companion doc:** `Narrative_Package_Compare_And_Pick.md` (package-specific tone/endings)

---

## Player Name

| Field | Canon |
|-------|-------|
| **Given name** | **Kade** |
| **Role on Io** | Base camp leader / expedition captain |
| **UI default** | Display **Kade** in dialogue, chronicle, and player-facing narrative copy unless a package-specific alias is explicitly locked |

Do not revert to generic placeholders (**Commander**, **Pioneer**, unnamed player) in new narrative or UI copy.

---

## AI Spine Rename — Kairos

| Legacy | Canon |
|--------|-------|
| Aether-9 / Aether9 / `aether9` domain | **Kairos** / `kairos` |

**Naming beat (all packages):** Pre-prologue UI and radio use liaison shell, probe, ledger machine, or unknown contact labels — **not** Kairos. **Kade learns the name Kairos at prologue end.**

**Code mapping:**

| Legacy type | Canon type |
|-------------|------------|
| `Aether9Snapshot` | `KairosSnapshot` |
| `Aether9WorldStateProvider` | `KairosWorldStateProvider` |
| Echo `coreId: "Aether-9"` | `coreId: "Kairos"` |

---

## Kade — Core Identity (V4 Crimson Contract baseline; name global)

These traits define **Kade** in the locked freelancer spine (V4). Other narrative packages (V1–V3) share the **name** and may emphasize different fantasy tones while keeping Kairos + prologue naming.

| Trait | Canon |
|-------|-------|
| Profession | Freelance space explorer; ex-**terraform director** (started at the bottom, earned the chair) |
| Combat | Weapons-first mind; high field IQ; reads encounters like survey maps |
| Drive | Discovery — new terrain, dead signals, organisms that shouldn't exist, machines still running after crews vanish |
| Mars backstory | Last employer **stranded Kade on Mars** after a budget cut disguised as "phase completion" |
| Starting AC | **5000 AC = 5% of final Mars director pay** (charter + partial retainer — not generic starter charity) |
| Io mission | Assess **terraform viability** + document **why prior expeditions disappeared** |

---

## Starter Crew (V4 package lock)

| Specialist | Name | Class | Relationship |
|------------|------|-------|--------------|
| Combat | **Reid "Iron" Kael** | Combat Tactician | Old friend; Mars dome security; **max trust day one** |
| Science | **Dr. Suri Vale** | Science Specialist | Old friend; promoted from microbe-lab tech; **max trust day one** |

V1–V3 packages retain **1 starter Skilled Companion pick** from 5000 AC unless V4 spine is chosen globally.

---

## Package Truth Variants (Kairos role — pick one spine)

Kairos is always the dormant machine intelligence / Memory Core hub. **Truth variant** depends on chosen narrative package:

| Package | Kairos truth |
|---------|--------------|
| V1 Ash & Signal | Precursor **defense AI** |
| V2 Colony Horizon | Lost expedition **archive**; cores = people |
| V3 Fracture Compact | Corporate **black-box sync weapon** archive |
| V4 Crimson Contract | **Contract Ledger AI**; cores = escrow keys / kill receipts |

Ship **one** truth per build. Do not mix all four AI truths in one release.

---

## References

- GDD 5.0: `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt` — PRODUCT IDENTITY, A6 Kairos
- Narrative packages: `Assets/_Project/Documentation/Design/Narrative_Package_V*.md`
- World State: `Assets/_Project/Features/WorldState/` — `KairosSnapshot`, `KairosWorldStateProvider`
- Echo signals: `Assets/_Project/Scripts/Echoes/EchoWorldEntity.cs` — default `coreId = "Kairos"`

---

*Last updated: Aug 2026 — global Kade + Kairos canon lock.*
