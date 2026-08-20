# Io World Content — Executive Summary

**Project:** Dark Matter: Genesis · **Setting:** Jupiter’s moon Io, 2160  
**Document:** One-page rollup · **Detail:** `Io_World_Content_Phase_Map.md` · **Tickets:** `Io_World_Content_Milestone_Tickets.md`  
**Status:** Design investigation (July 2026) · **Disk:** flat terrain prototype — full Io world not built

---

## What we are shipping

A **persistent full-scale Io surface** (seven biomes + colony hub) with **instanced underground** (five strata), **chemosynthetic ecology** (flora/fauna — not Earth wildlife), **expedition threats** (native life + machines + human remnants), and a **12-pet collection** (+ vanity extras, DLC later). Io is the antagonist; exploration drives research, pets, and base growth.

| Pillar | Ship v1.0 scope |
|--------|-----------------|
| **Surface biomes** | B1–B7 — sulfur plains through precursor ruins; campaign order B6→B1→B2→B3→**B5**→**B4**→B7 |
| **Underground** | Strata 1–5 — tubes, pools (wade-only), broods, resonance vaults; breach teleport from surface |
| **Ecology** | ~40+ named organisms; migratory species; **Void Stitcher** global stealth elite |
| **Threats** | Android frames, humanoid expedition survivors, small ground machines, flying fauna |
| **Pets** | **12 core** (tame / capture / repair) + **4 vanity** extras; Brimstone Puff starter after prologue |
| **World systems** | WeatherDirector storms, ExperienceDirector Echo/elite budgets, encounter zones |

---

## Why it is phased

Content depends on **World Engine spine** (GDD B4): communications, save seed, living-world weather, pet migration, then geography. **Nine Io phases (W0–W8)** stack engineering and authoring so each milestone is playable — not “all art at once.”

```
W0 Data  →  W1 Map shell  →  W2 First regions (B6/B1/B2) + pets start
         →  W3 Heat/ash (B3/B4) + Void Stitcher
         →  W4 Polar (B5) + night cycle
         →  W5 Underground S1–S3
         →  W6 Endgame (B7 + vaults) + all pets
         →  W7 Director polish  →  W8 Vehicles + console + DLC hooks
```

**GDD B4 #9 “Io biome pass”** = W1–W8 delivered together for ship-quality world.

---

## Phase milestones (headline deliverables)

| Phase | Player-facing outcome | Key content |
|-------|----------------------|-------------|
| **W0** | Authoring ready | Biome/ecology/pet/encounter data schemas |
| **W1** | “Io exists” | Main map blockout, colony, B6 hub, breach in/out underground |
| **W2** | First expeditions feel like Io | B6/B1/B2 playable; Brimstone Puff; 3 core pets; storm + vent gameplay |
| **W3** | Danger escalates | B3/B4; Caldera Mantis; **Void Stitcher**; machine repair pets |
| **W4** | Rad/cold mastery | B5 polar night; best loot pet; vanity moths/rollers |
| **W5** | Depth matters | Wade pools, brood nests, Stratum 1–3 instances |
| **W6** | Mystery payoff | B7 ruins, S4–S5 vaults, full pet roster |
| **W7** | World feels alive | Directors balance Echo, weather, fauna, activities |
| **W8** | Ship polish | Io Buggy, console, performance, DLC pet pipeline |

---

## Content numbers (design lock)

| Category | v1.0 target |
|----------|-------------|
| Surface biomes | 7 (+ Graveyard overlay) |
| Underground strata | 5 |
| Core pets | 12 |
| Vanity pets | 4 (more via DLC) |
| Android / machine threat types | 10 + 7 small ground |
| Humanoid expedition types | 7 |
| Flying fauna types | 8 |

---

## Critical dependencies (blockers)

| Blocker | Blocks |
|---------|--------|
| GDD B4 #4 Living-world / WeatherDirector | W2+ (storms, geyser surges) |
| GDD B4 #6 Pet fold (Pet Bay, retire placeholders) | W2 pet content |
| GDD B4 #3 World seed / WorldState | W7 director tuning |
| GDD B4 #8 Kairos / Memory Cores | W6 B7 vault fiction |
| Main map + instance pipeline (W1) | All regional content |

---

## Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Scope creep on 7 biomes | Phase gates + per-biome authoring checklist; greybox before polish |
| Pet system parallel to Echo trio | One active pet; UI in Companions tab; no 4th combat AI tier |
| Void Stitcher frustration | 0.5 s audio telegraph; max 1/expedition; default no pet targeting |
| Underground vs surface duplication | Pressure-adapted ecology roster; shared verbs, different organisms |

---

## Success criteria (world content “done”)

- [ ] Player can traverse **full main map** from colony through all seven biomes in campaign order  
- [ ] Each biome passes **ecology authoring checklist** (flora, fauna, machine hook, activities)  
- [ ] Underground breach loop works with **vehicle auto-pack** and **wade-only** pools  
- [ ] **12 core + 4 vanity** pets obtainable without DLC  
- [ ] ExperienceDirector drives Echo weights, elite spawns (incl. Void Stitcher), activity mix  
- [ ] No legacy pet placeholders (Ricky, Probe, Fox Cub) in ship build  

---

## Document map

| Need | Read |
|------|------|
| Production order & matrices | `Io_World_Content_Phase_Map.md` |
| **Milestone tickets (W0–W8)** | `Io_World_Content_Milestone_Tickets.md` |
| Every creature / pet card | `Io_Biome_Ecology_Roster.md` |
| Exploration verbs & activities | `Io_Biome_Exploration_Gameplay_Plan.md` |
| Caves & pools | `Io_Underground_Architecture_Plan.md` |
| Canon / disk truth | `GAME_DESIGN_DOCUMENT_5.0.txt` Appendix B |

**Promotion:** Executive summary + phase map → GDD **Appendix A2f** after review.

---

*Dark Matter Studios — Dark Matter: Genesis*
