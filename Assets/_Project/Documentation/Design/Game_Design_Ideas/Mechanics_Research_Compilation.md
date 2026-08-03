# Mechanics Research Compilation

**Status:** Game design idea — research backlog (August 2026)  
**Origin:** Web/community research for unique mechanics that could fit Dark Matter: Genesis  
**Authority:** GDD 5.0 and locked Design docs take precedence when promoting any item below.

This document curates mechanics from survival, extraction, colony, and exploration games — mapped to DMG pillars: Io pressures (O₂, thermal, rad, storms), trio + base-22 companions, Neural Echoes, scanning, Lite Building / BCP, underground instances, Aether Credits, moral choices, Resonance Events, and World Engine directors.

---

## How to read this list

| Column | Meaning |
|--------|---------|
| **Fit** | How naturally it slots into GDD 5.0 + current design |
| **Hook** | Existing system or doc to attach to |
| **Inspiration** | Reference title or community source |

---

## 1. Environmental pressure & shelter

| # | Mechanic | What it does | Inspiration | Fit | Hook |
|---|----------|--------------|-------------|-----|------|
| 1 | **Wind shield positioning** | Solid terrain between you and wind removes windchill; UI shows when sheltered. Encourages route choice, not just “find a building.” | *The Long Dark* wind shield | **High** | Exposure zones, sulfur storms |
| 2 | **Deployable micro-shelter** | Temporary wind/thermal shelter that degrades over time; repairable with camp materials. Emergency extract during blizzard/storm. | *The Long Dark* snow shelter | **High** | Architect deployables, storm timers |
| 3 | **Radiant heat before contact** | Lava/silicate lenses damage from distance before touch — teaches positioning. | *Volcanoids* heat radius | **High** | B4 calderas, S4 geothermal |
| 4 | **Dual-pole thermal comfort band** | One meter, two extremes; debuffs at edges, not instant death. Seek warming/cooling stations. | *Oxygen Not Included*, GDD thermal lock | **High** | Locked thermal bar |
| 5 | **Wade lanes, not swim** | Slow movement + stamina drain in brine/condensate; aim penalty. Basin predators punish mid-crossing. | Underground design lock | **High** | `Io_Underground_Architecture_Plan.md` |
| 6 | **Smoke / ash low-profile crawl** | In sulfur/ash events, standing drains O₂ faster; crouch extends survival and visibility under canopy. | Firefighter smoke games | **Medium** | B3 ash flats, storm VFX |
| 7 | **Tremor amplification zones** | Caves/lava tubes increase stagger + rockfall chance during Tremor Swarm. Surface shelter ≠ underground safety. | GDD weather + underground | **High** | WeatherDirector (planned) |
| 8 | **Polar day/night thermal swing** | B5 gets harder at night; colony radio warns before expeditions. | Biome plan lock | **High** | B5 polar flats |

---

## 2. Exploration, scanning & discovery

| # | Mechanic | What it does | Inspiration | Fit | Hook |
|---|----------|--------------|-------------|-----|------|
| 9 | **Scan-to-blueprint** | Fragments/tech scans unlock fabricator recipes; duplicate scans salvage to materials. | *Subnautica* scanner | **High** | `ScannerDiscoveryRegistry`, Journal craft |
| 10 | **Base scanner station** | Powered module sweeps radius, shows hologram contacts; pick one to HUD-track. | *Subnautica* scanner room | **High** | BCP, Communications, `MapUI` |
| 11 | **Biome-specific scan verbs** | Weak-rock, gas-pocket, pool-analysis results — not generic POI pings. Science Specialist amplifies. | Biome plan §2.2 | **High** | `ScannableTarget` categories |
| 12 | **Knowledge-only progression** | Unlocking understanding opens routes (safe tube vs toxic pool) without stat power creep. | *Outer Wilds* (GDC curiosity loop) | **Medium** | Echo lore, Resonance vaults |
| 13 | **Narrative web, not icon janitor** | Journal links discoveries (breach ↔ stratum ↔ Echo signal ↔ core). | *Outer Wilds* ship computer | **Medium** | Journal, Aether-9 comms |
| 14 | **Radio frequency puzzles** | Tune comms to decode cache codes, android patrol channels, Echo harmonics. | *SIGNALIS* REM-64 radio | **High** | Communications framework |
| 15 | **Changing POIs over time** | Ash buries a breach mouth; tremor opens a skylight tube. World changes between visits. | *Outer Wilds* time-changing worlds | **Medium** | World Engine / directors |
| 16 | **Drone waypoint beacons** | Leave scout drone at POI; compass target for return trips (pre-drill unlock). | *Subnautica* scanner drones | **Medium** | Scanner + deployables |

---

## 3. Expedition, extraction & risk

| # | Mechanic | What it does | Inspiration | Fit | Hook |
|---|----------|--------------|-------------|-----|------|
| 17 | **Six-phase expedition loop** | Brief → approach → operate → discover → extract → debrief. Biome changes verbs, not the loop. | `Io_Biome_Exploration_Gameplay_Plan.md` | **High** | Already designed |
| 18 | **Carry-weight extract timer** | Loot weight slows you; storm/O₂ clock forces leave-now-or-greed. | Extraction survival genre | **High** | Inventory, exposure |
| 19 | **Safe pocket stash** | One guaranteed slot/box survives failed extract (not full wipe). | *ARC Raiders* safe pocket | **Medium** | Expedition debrief, AC economy |
| 20 | **Visible channel under pressure** | Long interact (breach, vent seal, Echo stabilise) escalates nearby risk. | *Hunt: Showdown* banish timer | **Medium** | Breach entry, class abilities |
| 21 | **Contested extraction** | Exit breach is noisy; tremor or patrol spikes if you linger. | *Hunt* extraction camps | **Medium** | Underground exit / drill hub |
| 22 | **Cataclysm reshapes routes** | Pyroclastic flow / lava blocks surface path; map updates; new breach valuable. | *Into the Fire* dynamic volcano | **High** | Resonance Events, weather |
| 23 | **Briefing weather window** | Colony Ops / Aether-9 recommends go vs shelter from director state. | *ARC Raiders* hub prep | **High** | Communications, ExperienceDirector |

---

## 4. Base camp, building & production

| # | Mechanic | What it does | Inspiration | Fit | Hook |
|---|----------|--------------|-------------|-----|------|
| 24 | **Hearth / zone crafting pull** | Crafting in building zone auto-pulls from linked stash crates in radius. | *Return to Moria* hearth zones | **High** | BCP, Lite Building |
| 25 | **Storm pauses production queues** | Sulfur storm locks exterior queues; instance camps exempt (limited). | GDD locked | **High** | Building operations |
| 26 | **Building injures, never kills** | Structural damage hurts occupants; med/repair, not base wipe. | GDD locked | **High** | Already canon |
| 27 | **Attachment module specialization** | Generators, defense, mining, comms as swappable building modules. | GDD + *Volcanoids* drillship modules | **High** | Building Control Panels |
| 28 | **Deep drill risk toggle** | Geothermal deep drill: higher yield, higher tremor/spawn risk. | BCP `DeepDrillMode` | **High** | UI flag exists |
| 29 | **Forward instance camp** | Far underground: O₂ top-up, stash, NPC scrapper — no Lite Building. | Underground design lock | **High** | Drill survey nodes |
| 30 | **Hope / strain meter (camp-wide)** | Aggregate morale from injuries, storm losses; affects production or Echo odds. | *Frostpunk* hope/discontent | **Medium** | Base 22 roster sim |
| 31 | **Craft-from-nearby-storage** | Field terminal pulls mats from linked camp stash within range. | Terraria-style QoL (community-loved) | **Medium** | Inventory UX at camps |

---

## 5. Companions, trio & Neural Echoes

| # | Mechanic | What it does | Inspiration | Fit | Hook |
|---|----------|--------------|-------------|-----|------|
| 32 | **Class verb bias (soft)** | Architect seals vents; Infiltrator squeeze routes; Science scans pools — synergy bonus, not gates. | Biome plan §2.4 | **High** | Trio system |
| 33 | **Formation role in combat** | Tactician holds lane; Med Tech tends during channel; Scout flanks nests. | Tactical RPG + companion AI | **High** | Companion formations |
| 34 | **Procedural Echo traits** | Rescued Echoes roll traits (cautious, greedy, storm-phobic) affecting AI and BCP assignment. | *RimWorld* traits | **High** | Echo generator |
| 35 | **Injury affects capability** | Leg injury = limp; arm injury = one-handed; carry wounded to safety (light version). | *Kenshi* limb system | **Medium** | Injury system |
| 36 | **Companion depth limit** | Deep strata: full trio enters or one holds at camp. | Underground open question | **Medium** | S4/S5 gating |
| 37 | **Pet → Echo migration** | Fold legacy pet into Echo/trio system. | GDD locked direction | **High** | Roster cap 25 |
| 38 | **Director-driven Echo signals** | Signal density from roster gap, danger budget, recent deaths — not fixed spawns. | Echo director lock | **High** | `IoEchoSignalDirectorPolicy` |

---

## 6. Underground, drill & survey transit

| # | Mechanic | What it does | Inspiration | Fit | Hook |
|---|----------|--------------|-------------|-----|------|
| 39 | **Drill as mobile fortress** | Capsule = shelter during transit; module slots (O₂, survey, armor). | *Volcanoids* drillship | **High** | `Subsurface_Drill_Transit_Hub.md` |
| 40 | **Survey map (underground-initiated)** | Enter drill underground → travel to unlocked surface + subsurface nodes. | Drill hub design idea | **High** | Survey network registry |
| 41 | **Eruption forces underground** | Periodic surface eruption: drill down or shelter; resources respawn after event. | *Volcanoids* eruption cycle | **High** | Sulfur storms, tremors |
| 42 | **Modular tube grammar** | `Tube_Straight`, `Chamber_Pool`, `Hazard_Gas_Dome` kits — authored, not carved. | Underground plan §4 | **High** | W1 content pipeline |
| 43 | **Stratum-specific ecology verbs** | S1 skitter ambient; S3 wade + brood; S5 vault puzzles. | Ecology roster | **High** | Five strata |
| 44 | **Nested instance arenas** | Brood mother / vault core as separate load from tube network. | `IoUndergroundAccessKind.NestedInstance` | **Medium** | `IoWorldTransitionRules.cs` |

---

## 7. Combat & tactical

| # | Mechanic | What it does | Inspiration | Fit | Hook |
|---|----------|--------------|-------------|-----|------|
| 45 | **Light as weapon vs androids** | UV/flare disrupts machine vision. | *Level Zero: Extraction* | **Medium** | Ranged + tools |
| 46 | **Parry → companion opportunity** | Player parry opens companion finisher window (event-driven). | Project engineering rules | **High** | Combat events |
| 47 | **Nest clear vs sneak extract** | Brood chamber: fight for protein loot or sneak for core resource. | Underground expedition loop | **High** | S3 brood content |
| 48 | **Tremor stagger windows** | Seismic bursts create brief stagger openings for heavy hits. | GDD Tremor Swarm | **High** | Weather + combat |

---

## 8. Narrative, moral choices & Resonance

| # | Mechanic | What it does | Inspiration | Fit | Hook |
|---|----------|--------------|-------------|-----|------|
| 49 | **Alignment drift from choices** | Small moral choices accumulate toward stances; dialogue/thought bonuses, not world rewrites. | *Disco Elysium* political alignment | **High** | Moral choice system |
| 50 | **Partner rapport (hidden)** | Trio lead choices affect companion trust; debrief lines and combat assist eagerness. | *Disco Elysium* Kim rapport | **High** | Skilled companions |
| 51 | **Memory Core = world rule change** | Resonance Event temporarily changes spawns, weather, or comms — short authored beat. | GDD Resonance + *Outer Wilds* knowledge | **High** | Resonance Events |
| 52 | **Moral choice without savior fantasy** | Choices change who you become and camp culture, not “save Io.” | *Disco Elysium* tone | **High** | Narrative pillar |
| 53 | **Aether-9 as unreliable narrator** | Scan/comms data flagged provisional; player verifies in field. | *Subnautica* PDA disclaimers | **High** | Aether-9, Communications |

---

## 9. Economy, logistics & meta

| # | Mechanic | What it does | Inspiration | Fit | Hook |
|---|----------|--------------|-------------|-----|------|
| 54 | **AC-only vendor loop** | Expedition junk → instance scrapper → AC at colony vendors. | GDD economy lock | **High** | Aether Credits |
| 55 | **Purification / science pipeline** | Brine samples at Purification Hub; unlocks inoculations for B4/B5. | Underground resource sketch | **High** | Colony buildings |
| 56 | **Danger-budget loot** | Deeper / worse weather = better material tiers; director caps farming. | Extraction shooters | **Medium** | ExperienceDirector |
| 57 | **Roster-at-cap suppresses signals** | At 25 companions, Echo signal rate drops — pushes class/building goals. | Echo director lock | **High** | Roster cap |

---

## Top 12 — highest uniqueness × fit × reuse

1. **Survey drill transit hub** — `Subsurface_Drill_Transit_Hub.md` + *Volcanoids* fantasy  
2. **Biome-specific scan verbs** — weak rock, gas dome, pool analysis  
3. **Tremor-amplified underground** — surface weather ≠ cave safety  
4. **Six-phase expedition with extract clock** — weight + O₂ + storm  
5. **BCP zone stash pull + storm queue pause** — *Moria* hearth feel  
6. **Radio frequency gameplay** — Echo harmonics, cache codes  
7. **Procedural Echo traits** on rescue  
8. **Resonance Events as temporary world rule patches**  
9. **Dynamic route blockage** — ash/lava reshapes map  
10. **Class synergy bonuses** on breach/vent/sample verbs  
11. **Forward underground camps** as survey nodes  
12. **Moral alignment drift + companion rapport** (lightweight, not full Disco scale)

---

## Defer or avoid (for DMG)

| Mechanic | Why skip or defer |
|----------|-------------------|
| Full voxel digging | Conflicts with instanced underground + authored encounters |
| Permadeath hunter roster | Fights companion-driven identity and base-22 fantasy |
| PvP extraction | GDD is PvE colony + Io hazards + androids |
| Hunger as primary clock | GDD emphasizes O₂, thermal, rad, storms |
| Mobile/WebGL-first gimmicks | Target is PC + consoles |

---

## Reference sources

| Source | URL |
|--------|-----|
| Outer Wilds GDC 2021 — curiosity-driven exploration | https://ubm-twvideo01.s3.amazonaws.com/o1/vault/GDC+2021/beachum_gdc_2021(1).pdf |
| Volcanoids — drillship as base | https://store.steampowered.com/app/951440 |
| Return to Moria — hearth zones | https://steamcommunity.com/app/2933130/discussions/ |
| Subnautica scanner | https://wiki.subnautica.com/sn/Scanner_(Subnautica) |
| Subnautica scanner room | https://wiki.subnautica.com/sn/Scanner_Room_(Subnautica) |
| The Long Dark — weather / wind shield | https://thelongdark.fandom.com/wiki/Weather |
| Into the Fire — cataclysm reshaping | https://store.steampowered.com/app/2988030/Into_the_Fire/ |
| ARC Raiders — hub prep, extraction | https://store.steampowered.com/app/1800940/ARC_Raiders/ |
| Hunt: Showdown — banish + extract | https://www.huntshowdown.com/manual |
| Frostpunk — hope / discontent | https://frostpunk.fandom.com/wiki/Hope |
| Kenshi — injury affects locomotion | https://store.steampowered.com/app/233860/Kenshi/ |
| SIGNALIS — radio tuning | https://signalis.wiki.gg/wiki/Receiver |
| Oxygen Not Included — body temperature | https://oxygennotincluded.wiki.gg/wiki/Body_Temperature |
| RimWorld — traits | https://rimworldwiki.com/wiki/Traits |
| Disco Elysium — political alignment | https://discoelysium.fandom.com/wiki/Political_Alignment |

---

## Revision log

| Date | Change |
|------|--------|
| 2026-08-03 | Initial compilation from web/community mechanics research |
