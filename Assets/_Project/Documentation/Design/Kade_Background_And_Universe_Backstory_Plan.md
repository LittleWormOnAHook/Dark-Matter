# Kade Background System & Universe Backstory Plan

**Status:** Design draft — August 2026 (decisions locked §1.1)  
**Authority:** Supports GDD 5.0 (2160 Io, Aether-9, Memory Cores, failed expeditions, alien/android threats).  
**Player:** **Kade** (fixed name) — expedition lead and base-camp commander (the human player character).  
**Companion pick remains separate:** 5000 AC → 1 starter Skilled Companion (`StarterPioneerSelectUI`).

---

## 1.1 Locked decisions

| Decision | Lock |
|----------|------|
| **Player name** | **Kade** is fixed — no rename field at New Game. UI and comms always use “Kade.” |
| **Background + companion** | **Dialogue-only synergy.** `preferredCompanionHint` suggests flavor pairings; **no** mechanical duo bonuses, shared perks, or stat stacking. |
| **Starting economy** | Every background (including Hard Mode) starts at **5000 AC** — no AC tradeoffs. |
| **Starting kit tier** | Every background uses the **same kit power band** (see §4.0). Weapons and items differ by **role flavor**, not DPS or vendor value. |
| **Hard Mode** | Optional toggle on background select. **−20% Kade damage** (melee + ranged). **Same AC (5000)** and **same kit** as the chosen background — no extra gear, no stripped loadout. |

---

## 1. Design intent

At **New Game**, **before** starter companion selection, the player chooses **Kade’s background** — similar to *Fallout*, *Dragon Age Origins*, or *Cyberpunk 2077* lifepath picks. Optional **Hard Mode** toggle applies the damage penalty only.

Each background answers three questions:

1. **Who was Kade before Io?** (flavor + rumor knowledge)
2. **What can Kade do on day one?** (stats, skills, gear)
3. **What does the colony believe about Kade?** (Ops radio tone, minor dialogue flags)

Backgrounds are **not** companion classes. They bias Kade’s **personal** kit; the starter companion still fills a roster role (Architect, Scout, etc.).

---

## 2. New-game flow (proposed)

```
Main Menu → New Game
  → Kade Background Select  (NEW — includes Hard Mode toggle)
  → Starter Skilled Companion Select  (existing)
  → Welcome / controls popup  (existing)
  → Deploy to Pioneer scene
```

**UI:** Full-screen panel matching `StarterPioneerSelectUI` / Shift theme.  
**Cards:** Portrait silhouette, 3-line hook, expandable “Rumors you’ve heard” blurb, stat/perk summary.  
**Hard Mode:** Checkbox or toggle — “Hard Mode (−20% Kade damage)” — visible before confirm.  
**Lock:** One background per save; stored in `GameSaveData.playerBackgroundId`. Hard flag: `GameSaveData.hardModeEnabled`.

---

## 3. Data model (implementation sketch)

### ScriptableObject: `KadeBackgroundDefinition`

| Field | Purpose |
|-------|---------|
| `backgroundId` | Save key, e.g. `kade_bg_corporate_survey` |
| `displayName` | UI title |
| `tagline` | One-line hook |
| `flavorBody` | 2–4 sentences pre-Io history |
| `rumorKnowledge` | Bullet list — what Kade already suspects about Io |
| `maxHealthBonus` | Flat add to `SurvivalStats` max |
| `maxStaminaBonus` | Flat add |
| `maxEnergyBonus` | Flat add (optional) |
| `startingSkillGrants` | `{ skillId, rank }[]` — pre-allocated free ranks |
| `startingItems` | `{ itemId, count }[]` |
| `startingWeaponItemId` | Hotbar slot 0 or inventory grant |
| `unlockedRecipeIds` | Optional — e.g. rad gel recipe for smuggler |
| `passivePerkId` | Unique background trait (see §5) |
| `commsToneTag` | Colony Ops first-contact line variant |
| `preferredCompanionHint` | UI + **dialogue-only** companion pairing (see §8.1) |

### Save / apply hooks

| System | Hook |
|--------|------|
| `GameSaveData` | Add `string playerBackgroundId`, `bool hardModeEnabled` |
| `SimpleGameManager.BeginNewGameSession` | Merge background `startingItems` after base grant |
| `PlayerProgressionManager` | Apply `startingSkillGrants` before first skill UI |
| `SurvivalStats` | Apply max stat bonuses once at init |
| `StoryWorldStateProvider` | Expose `PlayerBackgroundId` for future dialogue |
| `CommsQueryService` / templates | `commsToneTag` for first Ops transmission |

**Balance rule:** Total power ≈ equal across backgrounds. Differentiate via **stats, skills, perks, and flavor items** — not AC or weapon DPS. No background strictly dominates combat **and** survival **and** exploration.

**Hard Mode apply:** When `hardModeEnabled`, multiply Kade outgoing damage by **0.80** (melee + ranged). Does not affect companions, pets, or turrets.

---

## 4. Background options (six)

### 4.0 Shared kit band (all backgrounds + Hard Mode)

Every background starts with the **same economic and combat baseline**:

| Shared grant | Value |
|--------------|-------|
| **Aether Credits** | **5000 AC** (always — Hard Mode included) |
| **Starter weapon tier** | Tier-1 — same base damage band; only animation/type differs |
| **Consumable band** | 3× utility consumables + 1× O₂ canister equivalent |
| **Tool / fiction item** | 1× background-flavored non-weapon (scanner, map fragment, story shell, etc.) |

Hard Mode does **not** remove items, swap weapons, or change AC. It only applies **−20% Kade damage**.

Baseline player stats (prototype reference): use current `SurvivalStats` defaults; backgrounds apply **small** deltas (+5–15% or flat +10–25) so companions remain specialists.

### BG-1 · Corporate Survey Attaché

**Who Kade was:** Junior field analyst for **Helix Meridian** during the late isotope-rush audits. Kade read telemetry, never held a rifle. Transferred to Io after a “accounting discrepancy” buried a rad spike near a polar camp.

| Category | Grant |
|----------|-------|
| **Stats** | +10 max Energy, +5 max Stamina |
| **Skills** | `skill_recon_sweep` rank 1 (free) |
| **Weapon** | Compact sidearm (Tier-1 sidearm band) |
| **Items** | Survey tablet (fiction), 2× rad sample vials, 1× O₂ canister |
| **AC** | 5000 |
| **Perk — *Calibrated Eye*:** +10% scan range; first map sector reveal bonus at B6 vista |
| **Rumors known** | “The rush numbers never matched the bodies.” “Polar camps went quiet before the calderas did.” |
| **Dialogue pairing** | Science Specialist or Infiltrator Scout — Ops / companion banter only |

---

### BG-2 · Jovian Guard Dropout

**Who Kade was:** Three years in **Jovian Orbital Defense** — boarding actions, android suppression drills. Kade left after a classified incident report was shredded. Io is exile and paycheck.

| Category | Grant |
|----------|-------|
| **Stats** | +20 max Health, +10 max Stamina |
| **Skills** | `skill_blade_training` rank 1, `skill_endurance` rank 1 |
| **Weapon** | Standard issue machete (Tier-1 melee band) |
| **Items** | Light armor patch kit, 2× stim patch, 1× O₂ canister |
| **AC** | 5000 |
| **Perk — *Suppressing Presence*:** First melee combo in an encounter costs −10% stamina |
| **Rumors known** | “Something got aboard during the last caldera evac — not fauna.” “Corporate security still scrubs android memory cores.” |
| **Dialogue pairing** | Combat Tactician or Med Tech — Ops / companion banter only |

---

### BG-3 · Salvage Guild Contractor

**Who Kade was:** Licensed wrecker out of **Callisto yards**. Kade bids on dead probes, sells alloy and intact cores to the highest bidder — sometimes twice.

| Category | Grant |
|----------|-------|
| **Stats** | +10 max Health, +5 max Stamina |
| **Skills** | `skill_mining` rank 1, `skill_harvesting` rank 1 |
| **Weapon** | Salvage hammer (Tier-1 melee band) |
| **Items** | Scrap bundle ×3, repair foam ×2, 1× O₂ canister |
| **AC** | 5000 |
| **Perk — *Strip & Save*:** +15% salvage from wreck POIs; chance for bonus scrap roll |
| **Rumors known** | “Half the ‘lost’ expeditions left hulls you can still walk through.” “Aether-9 is a myth — or a trademark.” |
| **Dialogue pairing** | Salvage Engineer or Architect Engineer — Ops / companion banter only |

---

### BG-4 · Polar Run Smuggler

**Who Kade was:** Ice-route runner moving **isotope cores** and unmarked med crates between B5 caches and inner-system buyers. Kade never asked what was in the teal-wrapped packages.

| Category | Grant |
|----------|-------|
| **Stats** | +10 max Stamina, +5 max Energy |
| **Skills** | `skill_recon_sweep` rank 1, `skill_gather_efficiency` rank 1 |
| **Weapon** | Silenced pistol (Tier-1 sidearm band) |
| **Items** | Stolen rad inoculation ×1, cold-tier glove liners, smuggler cache map fragment (fiction) |
| **AC** | 5000 |
| **Perk — *Ghost Route*:** −15% encounter weight for first expedition exit from camp |
| **Rumors known** | “Teal packages hum near caldera glass.” “Still Hunter isn’t a myth — smugglers lost two runners to ‘seams in the air.’” |
| **Dialogue pairing** | Infiltrator Scout or Logistics Officer — Ops / companion banter only |

---

### BG-5 · Field Medic (Red Cross Io)

**Who Kade was:** **Red Cross Io** triage lead on the sulfur plains refugee corridor. Kade signed up to save lives; corporate PMCs signed up to count them.

| Category | Grant |
|----------|-------|
| **Stats** | +25 max Health, +10 max Energy |
| **Skills** | `skill_vital_boost` rank 1 |
| **Weapon** | Shock baton (Tier-1 melee band) |
| **Items** | Field med kit ×3, sulfur respirator filter ×1, 1× O₂ canister |
| **AC** | 5000 |
| **Perk — *Triage Priority*:** Self-heal items +20% effectiveness; expedition downed companion bleed timer +15% |
| **Rumors known** | “Injured miners described ‘voices in the sulfur.’” “Some evac pods launched empty — autopilot only.” |
| **Dialogue pairing** | Med Tech or Science Specialist — Ops / companion banter only |

---

### BG-6 · Probe Relay Technician

**Who Kade was:** Maintained **deep-space relay buoys** for the **Aether Initiative** — a defunded program that listened for “non-natural seismic harmonics.” Kade is the only applicant who didn’t laugh at the job posting.

| Category | Grant |
|----------|-------|
| **Stats** | +10 max Energy, +10 max Stamina |
| **Skills** | `skill_weapon_accuracy` rank 1 |
| **Weapon** | Pulse carbine (Tier-1 ranged band) |
| **Items** | Relay spool ×1, Echo signal booster (consumable fiction), cracked Memory Core **shell** (story item, empty) |
| **AC** | 5000 |
| **Perk — *Harmonic Ear*:** Echo signal UI pings 20% sooner; Aether-9 repair quest gets unique “I’ve heard this frequency” line |
| **Rumors known** | “Aether-9 wasn’t built — it was *found*.” “Memory Cores aren’t human tech.” “Something answers when the cores are near.” |
| **Dialogue pairing** | Communications Officer or Science Specialist — Ops / companion banter only |

---

## 5. Passive perks (background traits)

Separate from skill tree — always on, not rankable:

| Perk ID | Background | Effect |
|---------|------------|--------|
| `perk_calibrated_eye` | Survey Attaché | Scan range +10%; first vista map bonus |
| `perk_suppressing_presence` | Guard Dropout | First melee combo −10% stamina |
| `perk_strip_and_save` | Salvage Contractor | +15% wreck salvage |
| `perk_ghost_route` | Polar Smuggler | First expedition exit −15% encounter weight |
| `perk_triage_priority` | Field Medic | +20% self-heal; +15% ally down timer |
| `perk_harmonic_ear` | Relay Technician | Echo ping lead time; Aether-9 dialogue flag |

Implement as `BackgroundPerkRegistry` or flags on `PlayerProgressionManager` — **do not** consume skill points.

---

## 6. Balance summary

| Background | Combat | Survival | Exploration | Story lens |
|------------|--------|----------|-------------|------------|
| Survey Attaché | Low | Med | **High** | Corporate cover-ups |
| Guard Dropout | **High** | Med | Low | Android / classified |
| Salvage Contractor | Med | Med | Med | Wreck lore |
| Polar Smuggler | Med | **High** (rad/cold fiction) | High | Smuggling / Still Hunter |
| Field Medic | Low | **High** | Med | Humanitarian horror |
| Relay Technician | Med | Med | **High** (Echo) | **Aether-9 primed** |

**All rows:** 5000 AC, Tier-1 weapon band, shared consumable band (§4.0).  
**Hard Mode:** Same row as chosen background + **−20% Kade damage** only.

**Recommended default for story-first players:** Relay Technician or Survey Attaché.  
**Recommended default for combat-first players:** Guard Dropout.

---

## 7. Universe backstory — Io and the human push (2160)

### 7.1 The promise

By **2160**, Earth’s belt economy runs on Jovian metals and isotopes. **Io** was never meant to be a home — it was a ** furnace with a payroll**: sulfur, silicates, volcanic gases, and rare isotopes cooked under Jupiter’s shadow.

Then someone noticed the **harmonics**.

Seismic arrays picked up repeating patterns in the calderas — too regular for magma, too deep for human drills. Funding flowed. Faith followed. **Helix Meridian**, **Aether Initiative**, and a dozen shell corps built habitats that were never designed to last.

Kade arrives at the **tail end of the gold rush**, when the corps are still smiling and the graves are still fresh.

---

### 7.2 Timeline (player-facing history)

| Era | Name | What happened |
|-----|------|----------------|
| **2140–2148** | **First Rush** | Corporate isotope scramble; polar camps, illegal core extraction; first mass casualties from rad pulses |
| **2149–2154** | **Symbiosis Decade** | Human–AI co-pilot experiments; android labor on Io; “Corporate Directive” labs in lava tubes |
| **2155–2158** | **The Silence** | Three flagship expeditions stop transmitting within six months; rescue teams find intact hulls, empty suits, running life support |
| **2159** | **Aether Initiative collapse** | Program shuttered after “unreproducible anomalies”; probe **Aether-9** sealed in a vault — or so the public ledger says |
| **2160** | **Genesis push** | **Dark Matter: Genesis** charter — small base camp, Echo rescue doctrine, official story: *reclaim*, not *colonize* |

---

### 7.3 Failed expeditions (environmental story seeds)

These are **rumors and wreckage**, not fully scripted yet — background blurbs and POI labels:

| Expedition | Last known | What survivors say |
|------------|------------|-------------------|
| **Helix Meridian Survey Seven** | B5 polar flats | Found “glass trees” that grow toward Jupiter; team stopped sleeping |
| **Symbiosis Lab Caravan** | B4 caldera rim | Androids walked out alone, carrying human tags; tags didn’t match roster |
| **Stillwater Protocol** | B6 highland tubes | Sent a perfect hourly status for eleven days — all from the same timestamp |
| **Aether Initiative Team Nine** | B7 ruin belt | Recovered a machine that **answered questions before they were asked**; team Nine renamed themselves in logs |

Kade’s background determines **which two expeditions** appear as “I’ve read the file” vs “I’ve heard bar talk.”

---

### 7.4 Aether-9 (mystery hub)

**Official line:** Weather probe from the First Rush, reactivated for storm prediction.

**Street line:** A **found object** — a shell older than human Io presence, housing an Echo that calls itself **Aether-9**.

**Truth (design canon):** Aether-9 is an **ancient Neural Echo** sealed in precursor hardware. It once held **Memory Cores** that recorded Io’s deep history. Without them, it wakes **angry, fragmented, and afraid**.

Kade with **Relay Technician** background starts with an empty core shell and harmonic vocabulary — the prologue repair quest (3 objects) should acknowledge: *“You didn’t come here blind.”*

---

### 7.5 Aliens, precursors, and the unknown

The game never needs to fully reveal “aliens” in act one. Layers of belief:

#### Layer A — What corporate PR admits
- “Unidentified seismic sources.”
- “Non-terrestrial alloy traces.”
- “Recommend avoidance of B7 geometry.”

#### Layer B — What expedition logs claim
- **Precursor ruins** — non-Euclidean angles, teal **Aether seeps**, silence zones where comms die.
- **Native Io life** — chemosynthetic, sulfur-silicon, resonance-fed; not “little green men” but ** ecology that responds to Memory Core leakage**.
- **Still Hunter** — mythic predator; one witness per decade; “seams in the air.”
- **Void Stitcher** — real, rare, kills and vanishes; Ops says *“Do not trust the seams.”*

#### Layer C — What Aether-9 will eventually say
- Io was **visited** before life crawled from Earth’s oceans.
- Memory Cores are **archives**, not batteries.
- Something **watches** caldera rims when cores are moved — not necessarily hostile; ** curious ** in a way humans misread as predation.

#### The unknown force (working name: **The Resonance**)
Not a faction — a **planetary immune response** to core theft and harmonic noise. Resonance Events (10–15 min world changes) are Io ** adjusting **. Endgame question: can humanity **partner** with it, or only **provoke** it?

---

### 7.6 Rumor table (campfire / Ops radio fodder)

Roll or gate by background + biome progress:

| Rumor | Grain of truth |
|-------|----------------|
| “Io breathes when Jupiter aligns.” | Weather + Resonance director coupling |
| “The cores scream when you slot them.” | Memory Core recovery setpieces |
| “Corps built labs inside living lava tubes.” | Symbiosis Decade sites in B4/S3 |
| “Smugglers moved teal cores for Helix.” | B5 polar arc, illegal isotope trade |
| “Aether-9 killed its last crew.” | Partial — something on Io killed Team Nine; Aether-9 remembers fragments |
| “Androids pray when they think you’re not listening.” | Corrupted AI prayer loops in B7 |
| “There’s a fifth pressure they don’t put on the HUD.” | Saturation / Resonance drift (future meter) |
| “The moon is hollow under the calderas.” | Stratum 4–5 vault network |

---

### 7.7 Kade’s place in the story

Kade is **not** a blank slate. Each background is a **lens**:

- **Corporate** Kade wants proof the rush lied.
- **Guard** Kade wants redemption for walking away.
- **Salvage** Kade wants a big score — or a ship off Io.
- **Smuggler** Kade owes the wrong people money.
- **Medic** Kade swore to reduce body counts.
- **Relay** Kade heard the harmonics and **has to know**.

All paths converge on: **restore Aether-9 → recover Memory Cores → survive Resonance → decide if Io becomes home.**

Companion selection still defines **who stands beside Kade**; background defines **who Kade already was**. Pairings unlock **extra dialogue lines only** (§8.1) — never stat bonuses.

---

## 8. Narrative integration beats

### 8.1 Background × companion dialogue (dialogue-only synergy)

When `playerBackgroundId` matches `preferredCompanionHint` class on the starter pick or expedition trio, unlock **optional banter lines** — no gameplay modifier.

| Background | Companion class | Example line (companion → Kade) |
|------------|-----------------|----------------------------------|
| Survey Attaché | Science Specialist | “You read the telemetry. I read the chemistry. We’ll argue until we’re right.” |
| Guard Dropout | Combat Tactician | “You know boarding drills. I know aggro. Don’t hero the rim alone.” |
| Salvage Contractor | Salvage Engineer | “You strip hulls; I strip schedules. Helix left plenty out here.” |
| Polar Smuggler | Infiltrator Scout | “You ran ice routes. I run signal routes. Same ghosts.” |
| Field Medic | Med Tech | “Red Cross Io? I’ve patched worse on the plains.” |
| Relay Technician | Communications Officer | “You heard the harmonics first. I’ll keep the channel open.” |

If the player ignores the hint, **no penalty** — generic companion lines still play.

### 8.2 Story beats

| Beat | Background hook |
|------|-----------------|
| Prologue crash / camp establish | Ops greeting uses `commsToneTag`; always addresses **Kade** |
| First Pioneer Guide / future Aether-9 interact | Relay Tech gets unique line; Smuggler mentions teal packages |
| B5 polar entry | Smuggler + Survey get extra **dialogue** POI callouts (not map markers) |
| B4 caldera escalation | Guard + Medic recognize evac pod horror (voice lines) |
| First Echo rescue | Relay + Survey extra chronicle **text** fragment |
| Aether-9 repair quest (3 objects) | Each background suggests a different first object location rumor |
| Memory Core #1 slot | Relay: “It’s listening.” Medic: “It’s in pain.” Salvage: “It’s worth a fortune.” |
| Hard Mode | Ops optional one-liner: “Command flagged your charter as high-risk. Watch your margins, Kade.” |

---

## 9. Production phasing

| Phase | Deliverable | Depends on |
|-------|-------------|------------|
| **P0 — Design lock** | This doc + background table sign-off | — |
| **P1 — Data** | 6× `KadeBackgroundDefinition` assets, registry | Item/weapon IDs stable |
| **P2 — UI** | `KadeBackgroundSelectUI` before companion pick | `StarterPioneerSelectUI` pattern |
| **P3 — Apply** | Save field, stat/skill/item grants on new game | `SimpleGameManager`, `PlayerProgressionManager` |
| **P4 — Perks** | Passive trait hooks (combat, scan, salvage) | Skill modifier pipeline |
| **P5 — Story** | Comms templates + Aether-9 lines keyed by `playerBackgroundId` | Communications Runtime (Run 2) |

**Out of scope for v1:** Respec background, hybrid backgrounds, multiplayer, mechanical background×companion bonuses.

---

## 10. Open questions

1. **Promote to GDD:** Fold §7 universe backstory into Appendix A narrative after review?
2. **Hard Mode damage value:** Confirm **−20%** Kade damage or tune in playtest (10–25% band)?

**Resolved (locked):**
- Kade = fixed name
- Companion synergy = dialogue only
- Hard Mode = −20% Kade damage; same 5000 AC and same kit as selected background

---

## 11. References

- `GAME_DESIGN_DOCUMENT_5.0.txt` — A6 Aether-9, environmental storytelling, classes
- `GAME_DESIGN_DOCUMENT_3.0.txt` — §2 mystery arc, §7 Memory Core examples
- `Io_Biome_Exploration_Gameplay_Plan.md` — B5→B4 story branch
- `Scripts/Progression/SkillDefinition.cs` — skill modifier types
- `Scripts/UI/StarterPioneerSelectUI.cs` — companion pick flow
- `Scripts/Managers/SimpleGameManager.cs` — starting items grant

---

*End of plan.*
