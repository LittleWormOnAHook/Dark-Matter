# Prologue Playthrough — Shuttle Landing to Aether-9

**Status:** Design draft — August 2026  
**Authority:** Extends `Quests_And_Story_Plan.md` + GDD 5.0 Appendix A6/A7; B6 Basalt Highlands hub.  
**Player:** **Kade** (fixed name) + free starter companion (0 AC).  
**Scope:** **Main quest line only** — target **2–5 hours**. Side quests, open exploration, and pet hunts are **out of band** (optional after prologue).

### Locked for this plan

| Lock | Value |
|------|--------|
| Memory Cores (campaign) | **10** primary cores (supersedes older “≥3” design target until GDD A6 is updated) |
| Prologue end condition | Kade meets / awakens **Aether-9**, slots **0** cores yet, accepts the **10-core hunt**, rescues **first Echo** |
| Level gate | Must reach **Player Level 5** before advanced mining / harvesting / settlement craft skills unlock (matches live `skill_mining`, `skill_harvesting` at `requiredPlayerLevel: 5`) |
| Biome | Entire prologue stays in **B6 Basalt Highlands** + shallow Stratum-1 tube mouth |
| Radio | **Colony Ops** only until Aether-9 awakens |

---

## 1. Design intent

The prologue teaches the **full survival-lite loop** without leaving the hub valley:

1. Land → survive exposure  
2. Gather → craft → place Command Center seed  
3. Fight / puzzle through friction zones  
4. Level to **5** → unlock settlement skills  
5. Rebuild a working camp  
6. Find the dormant machine → repair → meet Aether-9  
7. First Echo rescue → receive the **10 Memory Core** mandate  

Everything after that is Act III+ (core hunt across biomes) — not timed into the 2–5h prologue budget.

---

## 2. Timing budget (mainline only)

| Phase | Beat | Target length |
|-------|------|----------------|
| **P0** | Charter + dropship cinematic | 5–10 min |
| **P1** | Crash / landing + first survival | 20–35 min |
| **P2** | Resource ring + first craft | 25–40 min |
| **P3** | Claim camp plateau + Command Center seed | 25–40 min |
| **P4** | Settlement bootstrap (shelter, station, BCP) | 30–50 min |
| **P5** | Level-5 skill gate + deeper gather / mine | 25–45 min |
| **P6** | Ridge gauntlet + first boss | 20–35 min |
| **P7** | Aether-9 discovery + 3-part repair | 30–50 min |
| **P8** | Awaken → first Echo → 10-core charge | 15–25 min |
| | **Total mainline** | **~2.0–5.0 h** |

Soft fail: if a player is under Level 5 at P5, quest holds on a **training loop** (dummy + gather dens) until Level 5 — still counted as mainline time.

---

## 3. Step-by-step typical playthrough

### P0 — Charter (menu / pre-world)

1. New Game → **Kade Background** (+ optional Hard Mode −20% Kade damage).  
2. Free **starter Skilled Companion** pick (synergy preview if class matches).  
3. Colony Ops brief (radio): *“Charter drop to Basalt Highlands. Establish camp. Report.”*  
4. Cutscene / loading: **shuttle descent through Io sulfur haze** → hard landing.

**Systems taught:** menus, Hard Mode, companion identity.  
**Story scene:** cockpit / drop bay → blackout → exterior.

---

### P1 — Landing scar (Story Scene A)

**Where Kade starts:** beside the **downed charter shuttle** on a basalt shelf (**Landing Scar**), south of the future camp plateau. Starter companion stands near cargo hatch. Hotbar = background kit. **0 AC**.

**Objectives (quest `prologue_01_touchdown`):**

1. Exit hazard cone (shuttle thruster heat zone — thermal tick).  
2. Loot **Emergency Crate** (O₂ canister ×1, bandage ×2, rock pick / starter tool).  
3. Follow Ops ping to **Survey Stake Alpha** (50–80 m).  
4. Scan stake (tutorial scanner / optics if unlocked by kit).  

**Terrain:** gentle basalt steps, yellow sulfur crust patches, wreck debris corridor. Cliff walls funnel north toward Resource Ring.  
**Enemies:** none lethal — 1–2 **Cave Scout Moths** (flee / weak).  
**Friction:** O₂ / thermal HUD first spike; companion bark if player stands in heat.  
**POIs:** Shuttle wreck (interact: “Salvage later”), Survey Stake Alpha, heat shimmer VFX.

**Exit:** Ops marks **Resource Ring** on map/minimap.

---

### P2 — Resource Ring (Story Scene B)

**Layout:** semicircle of marked nodes around a dry sulfur creek bed — rocks, scrap metal, sulfur bloom, plant fiber (Tube Lace edge).

**Objectives (`prologue_02_scavenge`):**

1. Gather **Basalt Chunk ×8**, **Scrap Alloy ×5**, **Sulfur Crystal ×3**.  
2. Craft **Field Rations** or **Patch Kit** at **Portable Fabricator** (quest prop near stake — temporary craft UI).  
3. Defeat / drive off **Tube Jackal ×2** (first real combat — melee or ranged from kit).  
4. Return to shuttle cargo to unlock **Camp Beacon Kit** item.

**Skills / XP:** combat + gather XP — expect Level **2–3** by end.  
**Puzzle (light):** **Power Breaker** on fabricator — flip sequence of 3 relays (visual cables) before craft unlocks.  
**Friction:** jackals aggro if player gathers too loudly / stays in open; companion tanks one.  
**POIs:** Fabricator ruin, sulfur creek (cold/heat edge), scrap pile with glowing loot.

**Exit:** Camp Beacon Kit granted; Ops: *“Find high ground. Anchor the colony.”*

---

### P3 — Camp Plateau claim (Story Scene C)

**Layout:** flat **highland mesa** with clear sightlines — future Command Center pad etched as faded Helix Meridian survey paint (rumor only). Three approach ridges; one collapsed bridge.

**Objectives (`prologue_03_claim_site`):**

1. Reach **Camp Plateau** (ReachLocation).  
2. Clear **Brood Mouth** nest at pad edge (elite trash, not boss).  
3. Place **Command Center Seed** (Lite Building placement tutorial — snap to pad).  
4. Power seed with **Emergency Cell** salvaged from shuttle (carry item, 1 slot).  

**Terrain:** mesa top ~60–80 m across; ravine west; tube mouth northeast (blocked until P7).  
**Puzzle:** **Collapsed Bridge Bypass** — stack / deploy 2 salvage plates (crafted in P2) or companion “boost” interact to cross.  
**Friction:** carrying Emergency Cell slows sprint; drop if hit — jackal remount.  
**POIs:** Survey paint glyphs, broken antenna, sealed tube grate (Aether-9 path tease).

**Exit:** Command Center Seed online → Building Control Panel (Overview only) opens. Ops: *“Shelter. Craft. Survive the night window.”*

---

### P4 — Settlement bootstrap (Story Scene D)

**Objectives (`prologue_04_bootstrap`):**

1. Build / materialize **Survival Shelter** module (Lite Building).  
2. Build **Crafting Station** (or attach Craft tab module).  
3. Assign starter companion to **Overview → Companions** on BCP (teach assignment).  
4. Craft **Reinforced Framing ×4** + **Oxygen Scrubber Parts ×1** from plateau nodes.  
5. Optional mainline beat: survive **Mini Sulfur Gust** (60–90 s soft crisis — queues pause tutorial, retract HUD).  

**Systems taught:** Building Control Panel tabs Overview | Companions | Production | Craft (Craft may be light); Journal = recipe library only.  
**Skills:** `skill_gather_efficiency`, `skill_artisan_focus` (Lv3) available before Lv5.  
**Enemies:** ambient Tube Jackals on perimeter; no new boss.  
**Friction:** material shortfall forces a second gather loop on the plateau rim (still mainline).  
**POIs:** Shelter footprint, station, scrubber mount, storm shelter marker inside CC seed.

**Exit:** Ops unlocks **Skill Charter** quest — *“You need certified field ranks before deep mining. Hit Level 5.”*

---

### P5 — Level 5 gate (Story Scene E)

**Purpose:** Explicit skill / progression lesson. No story skip.

**Objectives (`prologue_05_field_cert`):**

1. Reach **Player Level 5** (XP from listed activities only — no side content required).  
2. Spend points into **`skill_mining`** OR **`skill_harvesting`** (at least one rank).  
3. Unlock recipe **Settlement Drill Bit** / **Harvest Sickle Mk1**.  
4. Mine **Deep Basalt Vein ×1** + harvest **Tube Lace Shelf ×1** in **Certified Gather Yard** (gated nodes that reject tools until skill owned).  

**Training loop (if underleveled):**

- **Combat Dummy Yard** (TrainingDummy) — melee/ranged XP  
- **Jackal Patrol Script** — 3 waves  
- **Craft Orders** — turn-ins for framing/bolts  

**Terrain:** Gather Yard = terraced quarry east of plateau + lace shelf on cliff.  
**Puzzle:** **Vein Cap Lock** — scan weak points in order (scanner) then mine.  
**Friction:** wrong tool on gated node shows **Require Level 5 / skill** popup (existing require-level UX).  
**POIs:** Certification beacon, dummy yard, sealed high-yield nodes.

**Exit:** Settlement skills online; Ops marks **Ridge Gauntlet** — *“Something older is buried past the ridge. You’ll need a working camp and a clear path.”*

---

### P6 — Ridge Gauntlet + first boss (Story Scene F)

**Layout:** linear ridge path with 2 side alcoves (optional loot — still on main path sightlines). Ends at **Machine Caldera** overlook.

**Objectives (`prologue_06_ridge`):**

1. Repair **Relay Pylon** (crafted part from P5).  
2. Clear **Glass Hive Skirmish** (mid trash).  
3. Defeat boss **Ash-Warden Drone** (damaged survey android — rumor: “prior expedition leftover”).  
4. Loot **Interface Key Fragment** (repair part 1 of 3 for Aether-9).  

**Boss kit:** telegraphed beam, weakpoint after overheat, companion interrupt window. Arena = circular basalt bowl with 2 pillars (cover).  
**Puzzle:** **Relay Pylon** — insert power cell + align dish toward Ops (minigame or 4-position rotate).  
**Friction:** beam forces cover use; sulfur vent ticks if player hugs edge.  
**Enemies:** Glass Hive drones → Ash-Warden.  
**POIs:** Relay pylon, android scrap mural (environmental storytelling — no lore dump), overlook to glowing machine below.

**Exit:** Path opens into **Machine Caldera**.

---

### P7 — Aether-9 discovery & repair (Story Scene G)

**Layout:** sunken caldera / crater with **dormant Aether-9 machine** (probe shell) center-stage. Rubble rings. Tube mouth connects later expeditions. Cool blue idle lights (vs camp magenta UI).

**Objectives (`prologue_07_aether_repair`):**

1. Interact with dormant machine → repair quest starts (Ops confused: *“That signature isn’t on the charter map.”*).  
2. Collect repair objects (**3**):  
   - **Interface Key Fragment** (from P6)  
   - **Power Coupler** — puzzle vault under caldera lip (pipe pressure valves)  
   - **Memory Bus Ribbon** — combat escort: defend companion while they extract from sparking conduit  
3. Slot all three → machine boots.

**Puzzle vault:** valve order painted as worn Helix marks; wrong order vents gas (damage + reset).  
**Friction:** conduit defense wave (Tube Jackals + 1 Brood Mouth).  
**POIs:** Aether-9 shell, valve vault, conduit trench, rumor plaques (illegible).

**Exit:** Cutscene / dialogue — Aether-9 awakens **angry / hostile**. Colony Ops steps back; Aether-9 becomes story focus (not yet advisory radio).

---

### P8 — Prologue end: Echo + 10-core mandate (Story Scene H)

**Objectives (`prologue_08_first_echo`):**

1. Survive Aether-9’s first hostility beat (dialogue + optional small shockwave stumble — no full combat vs Aether).  
2. Follow **Echo Signal** ping spawned by awaken pulse (**first Echo** — surface Neural Echo, rescueable).  
3. Reach Echo cradle POI (short path, 1 puzzle gate: **Frequency Align** — match 3 tone nodes).  
4. Rescue / reclaim **first Echo** into roster (or holding bay → Reclamation later).  
5. Return to Aether-9 → he demands **Memory Cores**: *his machine once held **ten**. Without them he cannot remember / function.*  

**Quest turn-in:** `prologue_end_ten_cores` — accepts hunt; maps **Core Site 01** as post-prologue teaser (can be outside B6 — player may stop here).  

**Prologue complete when:**

- [x] Command Center Seed + Shelter + Crafting Station live  
- [x] Level ≥ 5 + mining or harvesting unlocked  
- [x] Aether-9 awakened  
- [x] First Echo rescued  
- [x] 10-core hunt accepted (0/10 attached)

**Reward:** small AC payout, XP, recipe unlocks, Aether-9 trust tier = **Angry**.

---

## 4. Story geography (B6 prologue map)

```
                    [Tube Mouth — sealed]
                           ↑
              [Machine Caldera / Aether-9]
                           ↑
                   [Ridge Gauntlet]
                           ↑
     [Gather Yard] ← [CAMP PLATEAU] → [Dummy Yard]
                           ↑
                   [Resource Ring]
                           ↑
                   [Landing Scar / Shuttle]
```

| Zone | Size feel | Role |
|------|-----------|------|
| Landing Scar | ~40 m | Spawn, tutorial HUD |
| Resource Ring | ~80 m arc | Gather / first craft / first fight |
| Camp Plateau | ~70 m mesa | Lite Building hub |
| Gather Yard | ~50 m terraces | Lv5 skill proof |
| Dummy Yard | ~30 m | XP filler |
| Ridge Gauntlet | ~120 m linear | Combat + boss |
| Machine Caldera | ~60 m bowl | Aether-9 + repair |
| Echo Cradle | ~40 m spur | First Echo |

Keep **total authored walkable prologue footprint** compact enough that backtracking is ≤3–4 minutes between hubs.

---

## 5. POI catalog (prologue)

| ID | Name | Zone | Interact |
|----|------|------|----------|
| POI-01 | Charter Shuttle Wreck | Landing | Salvage crates; later scrap |
| POI-02 | Survey Stake Alpha | Landing | Scan tutorial |
| POI-03 | Portable Fabricator Ruin | Resource Ring | First craft + relay puzzle |
| POI-04 | Sulfur Creek Bed | Resource Ring | Hazard edge |
| POI-05 | Helix Pad Glyphs | Camp Plateau | Rumor flavor |
| POI-06 | Command Center Seed | Camp Plateau | Lite Building + BCP |
| POI-07 | Survival Shelter | Camp Plateau | Storm tutorial |
| POI-08 | Crafting Station | Camp Plateau | Settlement craft |
| POI-09 | Certification Beacon | Gather Yard | Level/skill gate UI |
| POI-10 | Deep Basalt Vein | Gather Yard | Needs `skill_mining` |
| POI-11 | Tube Lace Shelf | Gather Yard | Needs `skill_harvesting` |
| POI-12 | Relay Pylon | Ridge | Dish align puzzle |
| POI-13 | Ash-Warden Arena | Ridge | Boss |
| POI-14 | Aether-9 Shell | Caldera | Repair / awaken |
| POI-15 | Valve Vault | Caldera | Power Coupler |
| POI-16 | Sparking Conduit | Caldera | Bus Ribbon escort |
| POI-17 | Echo Cradle | Echo spur | First Echo rescue |

---

## 6. Enemies, elites, boss

| Encounter | Phase | Role |
|-----------|-------|------|
| Cave Scout Moth | P1 | Non-lethal teach |
| Tube Jackal (pack 2) | P2 | First combat |
| Brood Mouth (nest) | P3 | Pad clear |
| Tube Jackal perimeter | P4–P5 | Ambient pressure |
| Glass Hive skirmish | P6 | Mid trash |
| **Ash-Warden Drone** (boss) | P6 | Interface Key Fragment |
| Conduit defense wave | P7 | Escort friction |
| *(No Aether-9 fight)* | P8 | Dialogue threat only |

**Boss design notes (Ash-Warden):** 1 armor break phase, 1 companion opportunity on stagger, loot = repair part. Soft DPS check for ~3–6 minutes.

---

## 7. Puzzles & friction points

| Type | Example | Fail state |
|------|---------|------------|
| Relay / cable | Fabricator power | No craft until solved |
| Bridge plates | Plateau approach | Soft lock path — plates craftable nearby |
| Carry object | Emergency Cell | Drop on hit |
| Skill gate | Lv5 vein/lace | Require-level popup |
| Dish align | Relay Pylon | No ridge progress |
| Valve order | Power Coupler vault | Gas damage + reset |
| Frequency align | Echo Cradle | Gate closed |
| Soft crisis | Mini Sulfur Gust | Queues pause; shelter teach |
| Thermal / O₂ | Landing heat + creek | HUD literacy |

Avoid hard adventure-game softlocks: every puzzle piece is craftable or lying ≤60 m from the gate.

---

## 8. Systems taught (full gameplay slice)

| System | When | Proof of learn |
|--------|------|----------------|
| Movement / camera / hotbar | P1 | Reach stake |
| Survival meters (O₂, thermal) | P1–P2 | Exit hazard |
| Scanner / optics (light) | P1 / P5 | Scan stake / vein |
| Gather + inventory | P2 | Quest item counts |
| Crafting | P2 / P4 | Fabricator + station |
| Melee / ranged / stamina | P2 / P6 | Jackals + boss |
| Companion presence | P1–P8 | Assign on BCP; escort |
| Lite Building placement | P3–P4 | CC Seed + Shelter |
| Building Control Panel | P4 | Assign companion |
| Weather / crisis HUD | P4 | Mini gust |
| Progression / skills | P5 | Lv5 + mining/harvest |
| Mining tool loop | P5 | Deep vein |
| Elite / boss combat | P6 | Ash-Warden |
| Multi-part story quest | P7 | 3 repair objects |
| Echo rescue | P8 | First Echo |
| Mystery hub (Aether-9) | P8 | 10-core hunt accepted |

---

## 9. Level 5 & skill charter (explicit)

**Required before P6–P7 deep gather rewards and settlement recipes:**

| Skill | Live `requiredPlayerLevel` | Prologue use |
|-------|----------------------------|--------------|
| `skill_mining` | **5** | Deep Basalt Vein, drill bit recipe |
| `skill_harvesting` | **5** | Tube Lace Shelf, sickle recipe |
| `skill_artisan_focus` | 3 | Early craft quality (pre-gate) |
| `skill_gather_efficiency` | 1 | Open from start |
| `skill_field_logistics` | 6 | **Post-prologue** (not required) |

**Quest rule:** `prologue_05_field_cert` cannot complete without Level ≥ 5 and ≥1 rank in mining **or** harvesting. Building recipes for **Reinforced Framing Mk2 / Scrubber** stay locked behind that cert.

---

## 10. Quest asset list (to author)

| Quest ID | Title | Phase |
|----------|-------|-------|
| `prologue_01_touchdown` | Touchdown | P1 |
| `prologue_02_scavenge` | Scavenge the Ring | P2 |
| `prologue_03_claim_site` | Claim the Plateau | P3 |
| `prologue_04_bootstrap` | Raise the Camp | P4 |
| `prologue_05_field_cert` | Field Certification | P5 |
| `prologue_06_ridge` | Ridge Gauntlet | P6 |
| `prologue_07_aether_repair` | Wake the Machine | P7 |
| `prologue_08_first_echo` | First Echo | P8 |
| `prologue_end_ten_cores` | Ten Memories | P8 end → Act III |

Replace `QuestGiver_PioneerGuide` board as primary driver once these ship; Guide may remain as optional chatter only.

---

## 11. After prologue (handoff — not in 2–5h budget)

- **Act III:** Hunt **Memory Cores 1–10** across campaign biomes (B6→B1→…→B7 per phase map).  
- Each core: find → setpiece → attach → Resonance Event.  
- Aether-9 trust ladder progresses with cores returned.  
- Side content (Lost Survey / Beacon Hopper, etc.) unlocks **after** `prologue_end_ten_cores`.

**Core 1 teaser:** marker can appear at prologue end, but the recovery setpiece is the first Act III mission (may leave B6).

---

## 12. Acceptance checks (prologue design)

1. Mainline completable in **2–5 hours** without side quests.  
2. Player ends at **Level ≥ 5** with mining or harvesting unlocked.  
3. Camp has CC Seed + Shelter + Crafting Station.  
4. Aether-9 awakened; **10-core** hunt accepted; **first Echo** rescued.  
5. All systems in §8 exercised once.  
6. Player-facing lore stays rumor-grade until cores prove truth.

---

*Companion doc to `Quests_And_Story_Plan.md`. Update both when core count, level gates, or prologue timing change.*
