# Prologue Acts — Expanded Detail

**Status:** Design draft — August 2026  
**Parents:** `Prologue_Playthrough_Step_By_Step.md` (checklist) · `Prologue_Playthrough_And_Camp_Bootstrap_Plan.md` (systems/POIs) · `Quests_And_Story_Plan.md` (spine)  
**Scope:** Mainline Acts **0 → I → II** only (2–5 hours). Act **III+** summarized as handoff.  
**Locks:** Kade fixed name · 0 AC · free starter companion · Level 5 mining/harvest gate · **10 Memory Cores** post-prologue · rumors until cores prove truth.

---

## Act overview

| Act | Name | Player fantasy | End state |
|-----|------|----------------|-----------|
| **0** | Charter | Who is Kade before Io? | Dropship committed; companion chosen |
| **I** | Landing & Camp | We survived the crash. We have a foothold. | CC Seed + Shelter + Crafting Station live |
| **II** | Cert & Machine | We earned the right to dig — and woke something. | Lv5 skills · Aether-9 awake · Echo #1 · 10-core hunt accepted |
| **III+** | (Post-prologue) | Hunt the ten memories. | Outside 2–5 h budget |

---

# ACT 0 — Charter

**Duration:** 5–10 minutes  
**Tone:** Clinical Ops briefing over personal background flavor; no Io truth dumps.  
**Location:** UI only → shuttle cinematic.

### Purpose

1. Lock Kade identity (background + optional Hard Mode).  
2. Lock free starter companion (synergy preview if class matches).  
3. Sell the charter: establish a camp on Basalt Highlands, report to Colony Ops.  
4. Transition emotionally from menu → crash.

### Expanded beats

**0.1 New Game**  
- Main Menu → New Expedition / New Game.  
- Save slot created empty: `aetherCredits = 0`, no cores, Aether-9 not awakened.

**0.2 Kade Background Select**  
- Full-screen cards (Shift / Dark Matter palette).  
- Each card: tagline, 3-line hook, expandable “Rumors you have heard,” stat/perk summary, companion synergy highlights (≥2 classes).  
- Hard Mode toggle visible: **−20% Kade damage**, same kit, same 0 AC.  
- Confirm writes `playerBackgroundId` + `hardModeEnabled`.  
- Do **not** reveal Memory Core count or Aether-9 existence here.

**0.3 Free Starter Companion**  
- Same flow as today’s starter pick, but **cost 0 AC** (design; code may still show 5000 until fixed).  
- If companion class matches a synergy row: show Kade stat + free skill-rank preview before confirm.  
- Companion is present on landing (not “arrive later”).

**0.4 Welcome / controls**  
- Short KBM + gamepad sheet.  
- Must acknowledge before cinematic (GameSession gate pattern).

**0.5 Shuttle descent cinematic**  
- Visual: Io sulfur haze, basalt ridges, Colony Ops VO: *“Charter drop to Basalt Highlands. Establish camp. Report.”*  
- Impact: hard landing, blackout frames, fade up exterior.  
- Optional: companion one-liner by class (scout / medic / engineer flavor only).

### Failure / skip

- No gameplay fail states. Backing out of background/companion returns to previous panel.  
- Cinematic skip allowed after first playthrough (settings).

### Act 0 exit criteria

- [ ] Background + Hard Mode saved  
- [ ] Starter companion saved  
- [ ] Player spawned at Landing Scar with hotbar kit and 0 AC  

---

# ACT I — Landing & Camp

**Duration:** ~1.5–2.75 hours of the 2–5 h total  
**Tone:** Survival scramble → competent foothold. Colony Ops is the only radio voice.  
**Geography spine:** Landing Scar → Resource Ring → Camp Plateau (bootstrap).

Act I is four scenes. Each scene expands **space, story, systems, combat, friction, and exit**.

---

## Act I · Scene A — Landing Scar (Steps 1.1–1.6)

**Quest:** `prologue_01_touchdown`  
**Target time:** 20–35 min (with Act 0)  
**Story beat:** We are alive. The charter is broken. Move.

### Space and terrain

- **Landing Scar:** basalt shelf ~40 m across, scorch bloom under thrusters, yellow sulfur crust patches, wreck debris funneling **north**.  
- Cliff walls prevent early soft-lock wandering west/east; north corridor is the only comfortable exit.  
- Ambient: heat shimmer VFX on thruster cone; distant Io sky; companion idle near cargo hatch.

### Player start state

- Kade at shuttle nose/side (not inside cockpit).  
- Hotbar = background kit (Tier-1 weapon + flavor tool).  
- Inventory nearly empty except kit. **0 AC.**  
- Survival meters visible; O₂ and thermal may already tick if standing in heat.

### Step detail

| Step | Expanded detail |
|------|-----------------|
| **1.1 Spawn** | Camera settles third-person. Companion bark: *“Hull held. Barely.”* Ops: *“Telemetry noisy. Find Survey Stake Alpha.”* |
| **1.2 Leave heat cone** | Standing in thruster volume applies thermal pressure. UI callout first time. Companion warns if player idles in cone >3 s. |
| **1.3 Emergency Crate** | Interact on crate: O₂ canister ×1, bandage ×2, rock pick / starter tool. Teaches loot + inventory full check. |
| **1.4 Walk to Survey Stake Alpha** | 50–80 m along debris corridor. Minimap/Ops ping. Optional moths flutter — no forced fight. |
| **1.5 Scan stake** | Hold scan / optics lite. Stake paints Resource Ring on map. First scanner use if kit includes it; else Ops describes “marking beacon.” |
| **1.6 Moths** | 1–2 Cave Scout Moths: flee or die in 1–2 hits. Prove attack input without real threat. |

### Systems taught

Movement, camera, hotbar, thermal/O₂ HUD, loot, ping navigation, light scan.

### Friction and recovery

- Death (rare): respawn at shuttle with crate already looted if opened.  
- Missing crate: quest marker persists.  
- Skipping scan: stake still interactable; cannot open Scene B markers until scanned.

### Story / VO (rumor-safe)

- Ops never names Aether-9.  
- Background-specific rumor line optional (one sentence max) on stake scan.

### Exit

Resource Ring marked; `prologue_01` complete.

---

## Act I · Scene B — Resource Ring (Steps 2.1–2.8)

**Quest:** `prologue_02_scavenge`  
**Target time:** 25–40 min  
**Story beat:** Scavenge enough to build a camp kit — and learn the planet bites.

### Space and terrain

- Semicircle ~80 m along a **dry sulfur creek bed**.  
- Nodes: basalt chunks, scrap alloy piles, sulfur crystals near creek edge (thermal/cold edge teaching).  
- Center/side: **Portable Fabricator Ruin** (dead Helix survey gear — rumor only).  
- Sightlines open enough for jackals to flank; one rock spur for cover.

### Step detail

| Step | Expanded detail |
|------|-----------------|
| **2.1 Enter ring** | Ops: *“Prior survey left scrap. Take what still reads clean.”* |
| **2.2–2.4 Gather trio** | Exact counts: Basalt ×8, Scrap Alloy ×5, Sulfur Crystal ×3. Nodes glow weakly when quest-active. Creek crystals pulse hazard tint. |
| **2.5 Power Breaker puzzle** | Fabricator dead. Three relays with visible cables; correct order painted faintly on chassis. Wrong flips reset + spark FX (no softlock — hints after 2 fails). |
| **2.6 First craft** | Craft Field Rations **or** Patch Kit. Opens craft UI tutorial (station-lite). Consumable proves craft matters. |
| **2.7 Tube Jackals ×2** | First lethal-capable fight. Jackals aggro if player gathers in open too long or crosses scent line. Companion can tank one. Teach stamina / hotbar weapon. |
| **2.8 Camp Beacon Kit** | Return to shuttle cargo hatch → grant kit used in Scene C. Ops: *“Find high ground. Anchor the colony.”* |

### Systems taught

Gather loops, craft, first combat, companion assist, quest item grant.

### Enemies

| Enemy | Count | Notes |
|-------|-------|-------|
| Tube Jackal | 2 | Pack AI light; leash to ring |
| Remount jackal | 0–1 | Only if fight abandoned mid-way |

### Friction and recovery

- Out of bandages: ration craft or return to shuttle med crate respawn once.  
- Puzzle stuck: Ops ping highlights next relay after 90 s.  
- Underleveled feel: jackals telegraph lunges; companion draw aggro on bark / auto.

### Pacing / XP

Expect **Level 2–3** by 2.8. If still Level 1: allow one extra jackal script near creek (still mainline).

### Exit

Camp Beacon Kit owned; plateau marked.

---

## Act I · Scene C — Claim Camp Plateau (Steps 3.1–3.8)

**Quest:** `prologue_03_claim_site`  
**Target time:** 25–40 min  
**Story beat:** This pad was surveyed before. We take it anyway.

### Space and terrain

- **Camp Plateau:** highland mesa ~60–80 m across, clear sky dome, faded **Helix Meridian survey paint** (glyphs — rumor, not exposition).  
- Three ridge approaches; one **collapsed bridge** forces craft/companion bypass.  
- West ravine (lethal fall; optional rescue snap).  
- Northeast **sealed tube grate** — visible, locked until Act II caldera path (tease only).  
- Pad center: snap grid for Command Center Seed.

### Step detail

| Step | Expanded detail |
|------|-----------------|
| **3.1 Reach mesa** | ReachLocation trigger. Ops: *“Elevation good. Storm profile better than the scar.”* |
| **3.2 Salvage plates** | If bridge down: craft 2 plates at fabricator or from kit recipe using Scene B leftover mats. |
| **3.3 Bridge bypass** | Deploy plates **or** companion “boost” hold-interact. Teaches multi-solution traversal. |
| **3.4 Brood Mouth nest** | Elite trash at pad edge — spit / lunge. Clear before placement (prevents build-in-combat). |
| **3.5 Place CC Seed** | Use Camp Beacon Kit → placement ghost snaps to pad. First Lite Building place. |
| **3.6–3.7 Emergency Cell carry** | Cell at shuttle. Carry slows sprint; hit = drop. Jackal remount chance on return path. Insert cell → seed powers on. |
| **3.8 Open BCP** | Terminal E → Building Control Panel **Overview** only at first. Show power state, empty companion slots. |

### Systems taught

Lite Building placement, carry-friction, elite clear-before-build, BCP Overview.

### Friction and recovery

- Cell lost in ravine: respawns at shuttle after 30 s.  
- Placement invalid: ghost red until on pad snap.  
- Nest not cleared: placement blocked with toast.

### Environmental storytelling

- Broken antenna, survey paint, sealed grate — **no** readable lore proving aliens/Aether. Companion may speculate; Ops dismisses as “old survey junk.”

### Exit

CC Seed online; BCP opened once.

---

## Act I · Scene D — Settlement Bootstrap (Steps 4.1–4.6)

**Quest:** `prologue_04_bootstrap`  
**Target time:** 30–50 min  
**Story beat:** A seed is not a camp. Shelter. Craft. Assign. Breathe.

### Space and terrain

Same plateau, now with build footprints:

- Shelter pad adjacent to CC Seed (storm-safe interior volume).  
- Crafting Station footprint opposite.  
- Scrubber mount on rim facing prevailing wind VFX.  
- Perimeter jackal spawns soft-leashed outside build radius.

### Step detail

| Step | Expanded detail |
|------|-----------------|
| **4.1 Survival Shelter** | Place/materialize via Lite Building. Interior = mini-gust safe space. |
| **4.2 Crafting Station** | Primary settlement craft (Journal remains recipe library only). |
| **4.3 Assign companion** | BCP → Companions tab → assign starter to camp role (even if flavor “Camp Watch”). Teaches GDD BCP habit. |
| **4.4 Reinforced Framing ×4** | Gather rim nodes + craft. May require second gather loop (intentional friction). |
| **4.5 Oxygen Scrubber** | Craft parts + mount. Soft O₂ relief near camp (teach base vs expedition exposure). |
| **4.6 Mini Sulfur Gust** | 60–90 s scripted soft crisis: EnvironmentalCrisisHudMode, production/craft queues pause if any, Ops: *“Gust cell. Get inside.”* Shelter satisfy. Queues resume after. |

### Systems taught

Full Lite Building trio (Seed/Shelter/Station), BCP Companions, crisis HUD, queue pause rule (storm preview), settlement craft.

### Skills available (pre–Level 5)

- `skill_gather_efficiency` (Lv1)  
- `skill_artisan_focus` (Lv3) if leveled  

Deep mine/harvest still locked.

### Friction and recovery

- Mat shortfall: rim nodes respawn on timer while quest active.  
- Gust fail (stood outside): damage tick + retry gust once; do not brick quest.  
- Forgot companion assign: quest objective pulses Companions tab.

### Act I exit criteria

- [ ] CC Seed powered  
- [ ] Shelter built  
- [ ] Crafting Station built  
- [ ] Companion assigned on BCP  
- [ ] Mini gust survived once  
- [ ] Ops unlocks Field Certification (Act II Scene E)

**Player fantasy check:** We have a real foothold on Io.

---

# ACT II — Certification, Ridge & Aether-9

**Duration:** ~1.0–2.25 hours  
**Tone:** Competence test → buried machine → hostile awakening → curiosity hook (Echo + ten cores).  
**Geography spine:** Gather/Dummy Yards → Ridge Gauntlet → Machine Caldera → Echo Cradle → Aether-9.

---

## Act II · Scene E — Level 5 Field Certification (Steps 5.1–5.10)

**Quest:** `prologue_05_field_cert`  
**Target time:** 25–45 min  
**Story beat:** Ops will not authorize deep yield tools until you are certified.

### Why this exists

Live data already gates `skill_mining` and `skill_harvesting` at **requiredPlayerLevel: 5**. Prologue makes that gate a **story objective**, not a silent UI lock.

### Space and terrain

- **Dummy Yard** (~30 m): TrainingDummy + painted lanes east of plateau.  
- **Gather Yard** (~50 m terraces): Deep Basalt Vein + Tube Lace Shelf; **Certification Beacon** between them.  
- Gated nodes reject tools until skill owned (require-level popup).

### Step detail

| Step | Expanded detail |
|------|-----------------|
| **5.1 Accept cert** | Ops: *“Deep veins chew unrated drills. Hit Level 5. Take Mining or Harvesting. Prove it on the yard.”* |
| **5.2–5.4 XP loop** | Only if Level < 5. Dummy Yard, Jackal Patrol ×3, Craft Orders. All marked mainline — not side quests. |
| **5.5 Spend skill** | Skills panel: ≥1 rank in **mining OR harvesting** (both allowed). |
| **5.6 Recipes** | Unlock Settlement Drill Bit / Harvest Sickle Mk1. |
| **5.7 Beacon feedback** | Interact beacon; attempt locked node → popup teaches gate. |
| **5.8 Vein Cap Lock** | Scan weak points in numbered order, then mine. |
| **5.9–5.10 Proof gathers** | Mine Deep Basalt Vein ×1; harvest Tube Lace Shelf ×1. |

**Recommended production rule:** Cert completes when Level ≥5, ≥1 rank in mining **or** harvesting, and **both** nodes yield once. If the player only bought one skill, companion may operate the other tool **once** for the second proof.

### Systems taught

Progression, skill spend, require-level UX, mining tool loop, harvesting loop, scanner puzzle.

### Friction and recovery

- Stuck at Level 4: craft orders infinite while quest active.  
- Spent points elsewhere: grant a spare skill point on hitting Level 5 during this quest (prefer over free full respec).

### Exit

Ops marks Ridge Gauntlet. Settlement advanced recipes unlock.

---

## Act II · Scene F — Ridge Gauntlet + Ash-Warden (Steps 6.1–6.7)

**Quest:** `prologue_06_ridge`  
**Target time:** 20–35 min  
**Story beat:** Something older sits past the ridge. Clear the path.

### Space and terrain

- Linear ridge ~120 m, two loot alcoves on main sightline (optional chests — still main path visible).  
- Mid: Glass Hive nests on cliff teeth.  
- End: circular basalt bowl arena (~20 m) with **two cover pillars** + sulfur vent rim.  
- Overlook: view into **Machine Caldera** blue idle glow.

### Step detail

| Step | Expanded detail |
|------|-----------------|
| **6.1 Prep** | Craft Relay part at station; travel. Companion ammo/heal check bark. |
| **6.2 Relay Pylon** | Insert cell; rotate dish through 4 stops until Ops ping locks. Teaches device interact under light pressure (1 skitter enemy optional). |
| **6.3 Glass Hive skirmish** | Mid trash — ranged spitters. Teach cover on ridge spine. |
| **6.4–6.5 Ash-Warden Drone boss** | Damaged survey android (rumor: prior expedition leftover). Length ~3–6 min. |
| **6.6 Loot Interface Key Fragment** | Repair part **1/3** for Aether-9. Distinct icon/quest flag. |
| **6.7 Overlook** | Scripted look-at to caldera glow. Ops uneasy: *“That signature is not on the charter map.”* |

### Boss phases (expanded)

| Phase | Player read | Fail advice |
|-------|-------------|-------------|
| Beam | Line telegraph on ground | Hide behind pillar |
| Swarm add | 2 small drones once | Companion cleave / focus fire |
| Overheat | Core glows | Dump damage into core |
| Stagger | Kneel animation | Companion opportunity / melee finisher |

### Friction and recovery

- Soft wipe: restart at arena gate with boss HP reset; fragment not duplicated.  
- Pylon confusion: ghost arrow on correct rotate after 60 s.

### Exit

Path into Machine Caldera unlocked; Key Fragment held.

---

## Act II · Scene G — Wake Aether-9 (Steps 7.1–7.9)

**Quest:** `prologue_07_aether_repair`  
**Target time:** 30–50 min  
**Story beat:** We repair a dead machine. It is not dead. It is angry.

### Space and terrain

- **Machine Caldera:** sunken bowl ~60 m, rubble rings, cool blue idle lights (contrast camp magenta UI).  
- Center: dormant **Aether-9** probe shell (interactable).  
- Lip: **Valve Vault** entrance.  
- Trench: **Sparking Conduit** for ribbon escort.  
- Tube mouth beyond — sealed for post-prologue.

### Step detail

| Step | Expanded detail |
|------|-----------------|
| **7.1 Discover** | Interact shell. Boot glyphs. Repair quest starts. Ops confused; does not claim ownership. |
| **7.2 Ops line** | *“Whatever that is, it was not on the drop plan. Do not assume it is friendly.”* |
| **7.3 Part 1** | Interface Key Fragment already from boss — slot UI shows 1/3. |
| **7.4–7.5 Valve Vault** | Pipe pressure valves; order = worn Helix marks around room. Wrong order → gas damage + reset. Loot **Power Coupler** (2/3). |
| **7.6–7.7 Bus Ribbon escort** | Companion channels extract; player defends vs Tube Jackals + 1 Brood Mouth. Fail → restart wave; after first serious attempt keep 50% progress. Gain **Memory Bus Ribbon** (3/3). |
| **7.8 Slot all** | Insert three parts; short boot cinematic. |
| **7.9 Awaken** | Aether-9 online: hostile / angry. Speaks of murdered group and missing memories. Keep exact **“ten”** for Scene H; here say “many” / “the cores that lived in this shell.” |

### Systems taught

Multi-objective story quest, vault puzzle, escort defense, mystery hub introduction.

### Lore rules

- Still **rumor-grade** to the player: a voice in a machine claiming trauma.  
- No absolute confirmation of Io’s full history beyond what was fought (Ash-Warden as wrecked hardware).

### Friction and recovery

- Escort wipe: companion downed → revive interact; extract pauses.  
- Vault softlock: after 3 fails, Ops highlights next valve (diegetic pressure schematic ping).

### Exit

Aether-9 awakened; trust tier = **Angry**; Colony Ops steps back for story focus (still available for logistics).

---

## Act II · Scene H — First Echo and Ten-Core Mandate (Steps 8.1–8.7)

**Quest:** `prologue_08_first_echo` + `prologue_end_ten_cores`  
**Target time:** 15–25 min  
**Story beat:** A surface Echo answers the pulse. Aether-9 names the price of memory: ten cores.

### Space and terrain

- Short spur ~40 m to **Echo Cradle** (resonant niche, tone nodes).  
- Return path clear to caldera (no new enemies required; optional moths).

### Step detail

| Step | Expanded detail |
|------|-----------------|
| **8.1 Hostility beat** | Dialogue pressure; optional camera shake / stumble. **No** HP boss bar on Aether-9. |
| **8.2 Echo Signal** | Awaken pulse spawns first **Echo Signal** ping (sense/map). Distinct from Ops pings. |
| **8.3 Frequency Align** | Three tone nodes; match pitch/color order from cradle plaque. Gate opens. |
| **8.4 Rescue first Echo** | Reclaim into roster or holding bay. Teach Echo ≠ Aether-9 (Aether is sealed machine Echo; this is surface rescue). |
| **8.5 Return** | Mandatory return talk. |
| **8.6 Ten-core mandate** | Aether-9: machine once held **ten** Memory Cores; without them he cannot remember / function. Hunt accepted. |
| **8.7 Prologue end turn-in** | `prologue_end_ten_cores` → 0/10 tracker in Journal; optional Core Site 01 map teaser outside timed prologue. |

### Systems taught

Echo rescue, mystery mandate, journal campaign tracker.

### Act II / Prologue exit criteria

- [ ] Level ≥ 5  
- [ ] Mining or harvesting ranked (+ cert proofs)  
- [ ] Ash-Warden defeated; Key Fragment used  
- [ ] Aether-9 awakened (Angry)  
- [ ] First Echo rescued  
- [ ] 10-core hunt accepted (0/10)  
- [ ] Camp still standing (Seed + Shelter + Station)

**Rewards:** small AC, XP, recipes, journal tracker, trust = Angry.

---

# ACT III+ — Handoff (not in 2–5 h)

**Not expanded as prologue steps.** Summary only:

| Thread | Detail |
|--------|--------|
| Memory Cores 1–10 | Find → setpiece → attach → Resonance Event (10–15 min world change) each |
| Biome order | B6 → B1 → B2 → B3 → B5 → B4 → B7 |
| Trust ladder | Angry → wary → advisor → friend with cores / behavior |
| Side content | Lost Survey, pets, free exploration — **after** prologue end |

Core Site 01 may be marked at 8.7 but its setpiece is the first Act III mission.

---

## Cross-Act pacing map

| Clock (approx) | Act / Scene | Player should feel |
|----------------|-------------|--------------------|
| 0:00–0:10 | Act 0 | Chosen identity |
| 0:10–0:40 | I-A Landing | Vulnerable but guided |
| 0:40–1:15 | I-B Ring | Capable scavenger |
| 1:15–1:50 | I-C Plateau | Claimed ground |
| 1:50–2:30 | I-D Bootstrap | Colonist, not tourist |
| 2:30–3:10 | II-E Cert | Systems mastery / Lv5 |
| 3:10–3:40 | II-F Ridge | Combat confidence |
| 3:40–4:20 | II-G Aether | Unease / wonder |
| 4:20–4:45 | II-H Echo + mandate | Hooked for campaign |

Ranges flex with skill; soft XP loops keep slower players inside 5 h without side content.

---

## QA beat checklist (expanded)

**Act 0:** background save · companion free · cinematic lands on scar  

**Act I:** heat teach · gather counts · fabricator puzzle · jackals · kit grant · bridge solutions · nest clear · cell carry · BCP open · shelter/station · companion assign · mini gust  

**Act II:** Lv5 gate · skill popup · vein+lace proofs · pylon · hive · boss phases · 3 repair parts · awaken · echo puzzle · 10-core tracker 0/10  

---

*Expanded act bible for narrative, level design, and QA. Use `Prologue_Playthrough_Step_By_Step.md` for ordinal steps; this file for depth.*
