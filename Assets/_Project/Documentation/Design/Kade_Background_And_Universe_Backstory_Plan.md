# Kade Background System & Universe Backstory Plan

**Status:** Design draft — August 2026 (decisions locked §1.1)  
**Authority:** Supports GDD 5.0 (2160 Io, Aether-9, Memory Cores, failed expeditions, alien/android threats).  
**Player:** **Kade** (fixed name) — expedition lead and base-camp commander (the human player character).  
**Companion pick remains separate:** **0 AC** at New Game → **free** pick of 1 starter Skilled Companion (no AC spend); earn AC in play (`StarterPioneerSelectUI` refactor).  
**Story / quests spine:** See `Quests_And_Story_Plan.md` (main arc, side quests, biome order, prototype board).

**GDD note:** GDD 5.0 Appendix A8 already locks **0 AC** starter. This plan remains the background / Hard Mode / rumor authority.

---

## 1.1 Locked decisions

| Decision | Lock |
|----------|------|
| **Player name** | **Kade** is fixed — no rename field at New Game. UI and comms always use “Kade.” |
| **Background + companion** | **Minimum 2 companion synergies per background.** When the **starter** Skilled Companion’s class matches a synergy row, Kade gains **stat bonuses** + **1 free skill rank** (+ bonus dialogue). Synergies shown on background card and highlighted on companion select. |
| **Starting economy** | Every background (including Hard Mode) starts at **0 AC**. First companion is a **free charter pick** — not purchased. AC comes from quests, loot, and base output. |
| **Starting kit tier** | Every background uses the **same kit power band** (see §4.0). Weapons and items differ by **role flavor**, not DPS or vendor value. |
| **Hard Mode** | Optional toggle on background select. **−20% Kade damage** (melee + ranged). **Same 0 AC** and **same kit** as the chosen background — no extra gear, no stripped loadout. |
| **Io history (player-facing)** | Expeditions, aliens, Aether-9, and “what happened” on Io are **suspicions and rumors** until Memory Cores / evidence prove otherwise (see §7.0). |

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
  → Starter Skilled Companion Select  (free pick — 0 AC; synergy bonuses if class matches)
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
| `companionSynergies` | **≥2** entries — `{ companionClass, statBonuses, skillGrant, dialogueId }` (see §8.1) |

### Save / apply hooks

| System | Hook |
|--------|------|
| `GameSaveData` | Add `string playerBackgroundId`, `bool hardModeEnabled` |
| `SimpleGameManager.BeginNewGameSession` | Merge background `startingItems` after base grant |
| `PlayerProgressionManager` | Apply background `startingSkillGrants` + **companion synergy skill** after starter pick |
| `StarterPioneerSelectUI` | Highlight offers whose `pioneerClass` matches a synergy; preview Kade bonus on hover |
| `SurvivalStats` | Apply max stat bonuses once at init |
| `StoryWorldStateProvider` | Expose `PlayerBackgroundId` for future dialogue |
| `CommsQueryService` / templates | `commsToneTag` for first Ops transmission |
| `PioneerRosterManager` / `StarterPioneerCatalog` | 0 AC grant; free starter recruit; apply companion synergy after pick |

**Balance rule:** Total power ≈ equal across backgrounds. Differentiate via **stats, skills, perks, and flavor items** — not AC or weapon DPS. No background strictly dominates combat **and** survival **and** exploration.

**Hard Mode apply:** When `hardModeEnabled`, multiply Kade outgoing damage by **0.80** (melee + ranged). Does not affect companions, pets, or turrets.

---

## 4. Background options (six)

### 4.0 Shared kit band (all backgrounds + Hard Mode)

Every background starts with the **same economic and combat baseline**:

| Shared grant | Value |
|--------------|-------|
| **Aether Credits** | **0 AC** (always — Hard Mode included). Companion recruit costs **0** at New Game. |
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
| **AC** | 0 |
| **Perk — *Calibrated Eye*:** +10% scan range; first map sector reveal bonus at B6 vista |
| **Rumors known** | “The rush numbers never matched the bodies.” “Polar camps went quiet before the calderas did.” |
| **Dialogue pairing (≥2)** | See §8.1 synergy table |

---

### BG-2 · Jovian Guard Dropout

**Who Kade was:** Three years in **Jovian Orbital Defense** — boarding actions, android suppression drills. Kade left after a classified incident report was shredded. Io is exile and paycheck.

| Category | Grant |
|----------|-------|
| **Stats** | +20 max Health, +10 max Stamina |
| **Skills** | `skill_blade_training` rank 1, `skill_endurance` rank 1 |
| **Weapon** | Standard issue machete (Tier-1 melee band) |
| **Items** | Light armor patch kit, 2× stim patch, 1× O₂ canister |
| **AC** | 0 |
| **Perk — *Suppressing Presence*:** First melee combo in an encounter costs −10% stamina |
| **Rumors known** | “Something got aboard during the last caldera evac — not fauna.” “Corporate security still scrubs android memory cores.” |
| **Dialogue pairing (≥2)** | See §8.1 synergy table |

---

### BG-3 · Salvage Guild Contractor

**Who Kade was:** Licensed wrecker out of **Callisto yards**. Kade bids on dead probes, sells alloy and intact cores to the highest bidder — sometimes twice.

| Category | Grant |
|----------|-------|
| **Stats** | +10 max Health, +5 max Stamina |
| **Skills** | `skill_mining` rank 1, `skill_harvesting` rank 1 |
| **Weapon** | Salvage hammer (Tier-1 melee band) |
| **Items** | Scrap bundle ×3, repair foam ×2, 1× O₂ canister |
| **AC** | 0 |
| **Perk — *Strip & Save*:** +15% salvage from wreck POIs; chance for bonus scrap roll |
| **Rumors known** | “Half the ‘lost’ expeditions left hulls you can still walk through.” “Aether-9 is a myth — or a trademark.” |
| **Dialogue pairing (≥2)** | See §8.1 synergy table |

---

### BG-4 · Polar Run Smuggler

**Who Kade was:** Ice-route runner moving **isotope cores** and unmarked med crates between B5 caches and inner-system buyers. Kade never asked what was in the teal-wrapped packages.

| Category | Grant |
|----------|-------|
| **Stats** | +10 max Stamina, +5 max Energy |
| **Skills** | `skill_recon_sweep` rank 1, `skill_gather_efficiency` rank 1 |
| **Weapon** | Silenced pistol (Tier-1 sidearm band) |
| **Items** | Stolen rad inoculation ×1, cold-tier glove liners, smuggler cache map fragment (fiction) |
| **AC** | 0 |
| **Perk — *Ghost Route*:** −15% encounter weight for first expedition exit from camp |
| **Rumors known** | “Teal packages hum near caldera glass.” “Still Hunter isn’t a myth — smugglers lost two runners to ‘seams in the air.’” |
| **Dialogue pairing (≥2)** | See §8.1 synergy table |

---

### BG-5 · Field Medic (Red Cross Io)

**Who Kade was:** **Red Cross Io** triage lead on the sulfur plains refugee corridor. Kade signed up to save lives; corporate PMCs signed up to count them.

| Category | Grant |
|----------|-------|
| **Stats** | +25 max Health, +10 max Energy |
| **Skills** | `skill_vital_boost` rank 1 |
| **Weapon** | Shock baton (Tier-1 melee band) |
| **Items** | Field med kit ×3, sulfur respirator filter ×1, 1× O₂ canister |
| **AC** | 0 |
| **Perk — *Triage Priority*:** Self-heal items +20% effectiveness; expedition downed companion bleed timer +15% |
| **Rumors known** | “Injured miners described ‘voices in the sulfur.’” “Some evac pods launched empty — autopilot only.” |
| **Dialogue pairing (≥2)** | See §8.1 synergy table |

---

### BG-6 · Probe Relay Technician

**Who Kade was:** Maintained **deep-space relay buoys** for the **Aether Initiative** — a defunded program that listened for “non-natural seismic harmonics.” Kade is the only applicant who didn’t laugh at the job posting.

| Category | Grant |
|----------|-------|
| **Stats** | +10 max Energy, +10 max Stamina |
| **Skills** | `skill_weapon_accuracy` rank 1 |
| **Weapon** | Pulse carbine (Tier-1 ranged band) |
| **Items** | Relay spool ×1, Echo signal booster (consumable fiction), cracked Memory Core **shell** (story item, empty) |
| **AC** | 0 |
| **Perk — *Harmonic Ear*:** Echo signal UI pings 20% sooner; Aether-9 repair quest gets unique “I’ve heard this frequency” line |
| **Rumors known** | “Aether-9 wasn’t built — it was *found*.” “Memory Cores aren’t human tech.” “Something answers when the cores are near.” |
| **Dialogue pairing (≥2)** | See §8.1 synergy table |

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

**All rows:** 0 AC, free starter companion, Tier-1 weapon band, shared consumable band (§4.0).  
**Hard Mode:** Same row as chosen background + **−20% Kade damage** only.

**Recommended default for story-first players:** Relay Technician or Survey Attaché.  
**Recommended default for combat-first players:** Guard Dropout.

---

## 7. Universe backstory — suspicions, rumors, and Io (2160)

### 7.0 Narrative rule — nothing is confirmed at start

**Player-facing default:** Everything Kade “knows” about Io’s past — failed expeditions, aliens, precursor tech, Aether-9, corporate crimes — is **suspicion, bar talk, redacted files, or wreckage hints**. Ops and companions use language like *“report claims,”* *“unverified,”* *“rumor,”* *“could be cover story.”*

**Evidence ladder:** Rumor → environmental POI → Echo / chronicle fragment → Memory Core slot → Aether-9 confirmation. Act one **never** states alien canon as fact in UI copy.

**Writer canon (internal only — §7.8):** Design truth for directors and core arc; **not** shown to the player until earned.

---

### 7.1 The promise (what Kade was told)

By **2160**, belt corps sell Io as a ** furnace with a payroll** — isotopes, sulfur, silicates under Jupiter’s shadow. Charter briefs mention “harmonic surveys” and “reclamation.”

Kade arrives at the **tail end of the gold rush**. Official smiles. Unofficial whispers say the graves outnumber the ledgers.

---

### 7.2 Rumored timeline (what Kade has heard — not verified)

| Era (rumored) | Name | Suspicion / rumor (player-facing) |
|---------------|------|-----------------------------------|
| **2140–2148** | **First Rush** | Corps fought over isotopes; polar camps “went dark”; rad spikes buried in accounting |
| **2149–2154** | **Symbiosis Decade** | Human–AI “co-pilot” trials; android labor; labs rumored in lava tubes |
| **2155–2158** | **The Silence** | Flagships stopped calling; rescues found **empty suits** and running life support — “no bodies, no answers” |
| **2159** | **Aether Initiative collapse** | Program killed for “bad data”; **Aether-9** sealed — or **found**, depending who you ask |
| **2160** | **Genesis push** | Your charter: *reclaim*, not *colonize* — some think that’s PR |

Background blurbs use **“Kade suspects…”** / **“Kade was told…”** — not “this happened.”

---

### 7.3 Failed expeditions — rumors tied to wreckage

| Expedition (rumored) | Last known (reported) | What people **claim** — unverified |
|----------------------|----------------------|-------------------------------------|
| **Helix Meridian Survey Seven** | B5 polar flats | “Glass trees” toward Jupiter; crew “stopped sleeping” |
| **Symbiosis Lab Caravan** | B4 caldera rim | Androids walked out alone with **wrong** human tags |
| **Stillwater Protocol** | B6 highland tubes | Hourly status for eleven days — **same timestamp** |
| **Aether Initiative Team Nine** | B7 ruin belt | Machine “answered before asked”; crew renamed themselves in logs |

Kade’s background sets which rumors feel like **file gossip** vs **street talk**. Finding a wreck POI adds: *“The rumor might be true.”*

---

### 7.4 Aether-9 — layers of belief (player-facing)

| Layer | What Kade might believe |
|-------|-------------------------|
| **Corporate** | Old weather probe; reactivate for storm prediction |
| **Street** | **Found shell** — older than human Io; something inside calls itself Aether-9 |
| **Smuggler / relay circles** | Memory Cores aren’t batteries; something **answers** when cores get close |
| **Your background** | Relay Tech: empty core shell + harmonic vocabulary — still **theory** until repair arc |

Awakening dialogue stays hostile and fragmented — Aether-9 may **lie, omit, or misremember** until cores return.

---

### 7.5 Aliens, precursors, and the unknown — rumor tiers

#### Tier 1 — Campfire / Ops rumors (always “maybe”)
- “Non-human geometry in B7 — **if** the maps aren’t faked.”
- “Still Hunter — **one witness per decade**, could be stress hallucination.”
- “Void Stitcher — **do not trust the seams**” (Ops line; creature may be real, myth grows around it).
- “Androids **praying** in ruins — corrupted loop or something else?”
- “Fifth pressure not on the HUD” — Saturation / Resonance drift (future meter).

#### Tier 2 — Wreckage & scan hints (stronger suspicion)
- Precursor angles, teal **Aether seeps**, silence zones.
- Native Io life — chemosynthetic / sulfur-silicon — **confirmed ecology**, alien **intent** unknown.
- Illegal teal core shipments (B5 smuggler rumor).

#### Tier 3 — Memory Core + Aether-9 arc (earned truth)
- Cores hold **archives**, not just power.
- Prior visitors **may** have been on Io before Earth oceans — phrasing stays cautious until late arc.
- **The Resonance** (working internal name): planetary response to harmonic theft — players experience Events before naming it.

---

### 7.6 Rumor table (campfire / Ops radio)

| Rumor (always suspect until proven) | Possible grain of truth (writer) |
|-------------------------------------|----------------------------------|
| “Io breathes when Jupiter aligns.” | Weather + Resonance coupling |
| “The cores scream when you slot them.” | Core recovery setpieces |
| “Corps built labs inside living lava tubes.” | Symbiosis sites B4/S3 |
| “Smugglers moved teal cores for Helix.” | B5 polar arc |
| “Aether-9 killed its last crew.” | Partial — Team Nine; Aether-9 fragments |
| “The moon is hollow under the calderas.” | Stratum 4–5 vaults |

---

### 7.7 Kade’s place in the story

Each background is a **lens on rumors**, not a truth receipt:

- **Corporate** Kade — suspects the rush lied; wants proof.
- **Guard** Kade — suspects classified android incidents; wants redemption.
- **Salvage** Kade — suspects corps left fortunes in wrecks; wants out or score.
- **Smuggler** Kade — suspects teal cores and Still Hunter; owes dangerous people.
- **Medic** Kade — suspects casualties were covered up; wants fewer bodies.
- **Relay** Kade — suspects Aether-9 was **found**; needs to hear the harmonics again.

All paths converge on: **restore Aether-9 → recover Memory Cores → test rumors against evidence → decide if Io becomes home.**

Companion selection defines **who stands beside Kade**; matching class unlocks **Kade stat + skill synergy** (§8.1) plus extra dialogue.

---

### 7.8 Writer canon (internal — do not dump on player in act one)

*For designers / StoryDirector only. Reveal through cores and Resonance.*

- Aether-9: ancient Neural Echo in precursor hardware; Memory Cores = archives.
- Io likely had prior visitors; Resonance = planetary response, not a faction.
- Something may **observe** caldera activity when cores move — curiosity misread as hostility.

---

## 8. Narrative integration beats

### 8.1 Background × companion synergy (≥2 per background)

When the **starter** companion’s `SkilledPioneerClass` matches a row, apply **Kade stat bonuses** + **1 free skill rank** (stacks with background grants; no skill points spent) + **synergy dialogue** on first deploy.

**UI:** Background card lists all synergies. Companion select **highlights** matching offers and previews bonuses. Non-match = **no penalty** — background base kit only.

**Starter pool today** (`StarterPioneerCatalog`): Combat Tactician, Infiltrator Scout, Science Specialist, Architect Engineer, Med Tech — **bold** rows below are pickable day one.

#### BG-1 · Corporate Survey Attaché

| Companion class | Kade stats | Skill grant | Starter pool? |
|---------------|------------|-------------|---------------|
| **Science Specialist** | +10 max Energy | `skill_gather_efficiency` rank 1 | **Yes** |
| **Infiltrator Scout** | +5 max Stamina | `skill_weapon_accuracy` rank 1 | **Yes** |
| Communications Officer | +5 max Energy | `skill_artisan_focus` rank 1 | Roster later |

#### BG-2 · Jovian Guard Dropout

| Companion class | Kade stats | Skill grant | Starter pool? |
|---------------|------------|-------------|---------------|
| **Combat Tactician** | +15 max Health | `skill_marksman_training` rank 1 | **Yes** |
| **Med Tech** | +10 max Health | `skill_stamina_core` rank 1 | **Yes** |
| Salvage Engineer | +5 max Stamina | `skill_endurance` rank 1 | Roster later |

#### BG-3 · Salvage Guild Contractor

| Companion class | Kade stats | Skill grant | Starter pool? |
|---------------|------------|-------------|---------------|
| **Architect Engineer** | +5 max Stamina | `skill_stamina_core` rank 1 | **Yes** |
| Salvage Engineer | +10 max Health | `skill_gather_efficiency` rank 1 | Roster later |
| Logistics Officer | +5 max Energy | `skill_harvesting` rank 1 | Roster later |

#### BG-4 · Polar Run Smuggler

| Companion class | Kade stats | Skill grant | Starter pool? |
|---------------|------------|-------------|---------------|
| **Infiltrator Scout** | +10 max Stamina | `skill_endurance` rank 1 | **Yes** |
| Logistics Officer | +5 max Energy | `skill_weapon_accuracy` rank 1 | Roster later |
| **Science Specialist** | +5 max Energy | `skill_recon_sweep` rank 1 | **Yes** |

#### BG-5 · Field Medic (Red Cross Io)

| Companion class | Kade stats | Skill grant | Starter pool? |
|---------------|------------|-------------|---------------|
| **Med Tech** | +15 max Health | `skill_endurance` rank 1 | **Yes** |
| **Science Specialist** | +10 max Energy | `skill_recon_sweep` rank 1 | **Yes** |
| Combat Tactician | +5 max Health | `skill_vital_boost` rank 1 | **Yes** |

#### BG-6 · Probe Relay Technician

| Companion class | Kade stats | Skill grant | Starter pool? |
|---------------|------------|-------------|---------------|
| **Science Specialist** | +5 max Energy | `skill_recon_sweep` rank 1 | **Yes** |
| Communications Officer | +10 max Energy | `skill_artisan_focus` rank 1 | Roster later |
| **Infiltrator Scout** | +5 max Stamina | `skill_gather_efficiency` rank 1 | **Yes** |

**Example synergy dialogue (first deploy):**

| Pair | Line (companion → Kade) |
|------|-------------------------|
| Survey + Science | “You read the telemetry. I read the chemistry. We’ll argue until we’re right.” |
| Guard + Tactician | “You know boarding drills. I know aggro. Don’t hero the rim alone.” |
| Salvage + Architect | “You strip hulls; I strip schedules. Helix left plenty out here.” |
| Smuggler + Scout | “You ran ice routes. I run signal routes. Same ghosts.” |
| Medic + Med Tech | “Red Cross Io? I’ve patched worse on the plains.” |
| Relay + Science | “You heard the harmonics. I’ll tell you what the samples say.” |

**Implementation:** `KadeCompanionSynergyDefinition` on background SO; apply in `PioneerRosterManager` after starter pick; persist synergy skill in `allocatedSkillIds`.

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
| **P3 — Apply** | Save field, stat/skill/item grants, **0 AC** new game, free starter pick | `SimpleGameManager`, `PlayerProgressionManager`, `StarterPioneerCatalog` |
| **P4 — Perks + synergy** | Background perks + companion synergy stat/skill apply | Skill modifier pipeline |
| **P5 — Story** | Comms templates + Aether-9 lines keyed by `playerBackgroundId` | Communications Runtime (Run 2) |

**Out of scope for v1:** Respec background, hybrid backgrounds, multiplayer.

**Code touch for 0 AC:** `StarterPioneerCatalog.StarterAcGrant` → **0**; offers `acCost` → **0**; `StarterPioneerSelectUI` copy → “Choose one specialist (charter included)”; `GameStartPopup` AC blurb updated.

---

## 10. Open questions

1. **Promote to GDD:** Fold §7 + 0 AC start into Appendix A (supersedes 5000 AC grant)?
2. **Hard Mode damage value:** Confirm **−20%** Kade damage or tune in playtest (10–25% band)?

**Resolved (locked):**
- Kade = fixed name
- Io history = **suspicions and rumors** until evidence / Memory Cores
- Companion synergy = **≥2 classes per background** → Kade **stat bonuses + skill rank** + dialogue
- New Game = **0 AC**; starter companion = **free pick**
- Hard Mode = −20% Kade damage; same 0 AC and same kit as selected background

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
