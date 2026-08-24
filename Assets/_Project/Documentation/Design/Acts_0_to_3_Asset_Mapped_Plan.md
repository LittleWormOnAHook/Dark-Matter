# Acts 0–3 — Expanded Plan (Current + Future Assets)

**Status:** Design draft — August 2026  
**Authority:** `Quests_And_Story_Plan.md` · `Prologue_Acts_Expanded.md` · `Prologue_Playthrough_Step_By_Step.md` · GDD 5.0  
**Player:** Kade · 0 AC · free starter companion · Hard Mode optional · **10 Memory Cores**  
**Legend**

| Tag | Meaning |
|-----|---------|
| **CURRENT** | Exists in repo now (reuse / wire) |
| **EXTEND** | Exists; needs fields, content, or scene wiring |
| **FUTURE** | Does not exist yet — create |

---

## Act overview (0–3)

| Act | Name | Time band | End state |
|-----|------|-----------|-----------|
| **0** | Charter | 5–10 min | Background + companion locked; drop committed |
| **1** | Landing & Camp | ~1.5–2.75 h | CC Seed + Shelter + Craft Station live |
| **2** | Cert & Machine | ~1.0–2.25 h | Lv5 skills · Aether-9 awake · Echo #1 · 10-core hunt **accepted** |
| **3** | Ten Memories | Multi-session (post-prologue) | Cores **1–10** attached; Resonance cadence; trust climbing |

Acts **0–2** = timed prologue (2–5 h mainline). Act **3** = campaign Memory Core hunt.

---

# ACT 0 — Charter

**Fantasy:** Who is Kade before Io?  
**Radio:** Colony Ops only.

## Beats (expanded)

| Step | Beat | Detail |
|------|------|--------|
| 0.1 | New Game | Create save; credits **0**; no cores; Aether-9 flag false |
| 0.2 | Background select | Six Kade backgrounds + Hard Mode (−20% Kade damage) |
| 0.3 | Free companion | Cost **0 AC**; synergy preview if class matches |
| 0.4 | Controls popup | KBM + gamepad sheet; GameSession gate |
| 0.5 | Shuttle cinematic | Descent → hard landing → Landing Scar spawn |

## Asset map — Act 0

### CURRENT (use now)

| Asset | Path / type | Role in Act 0 |
|-------|-------------|----------------|
| Main menu / New Game | `MainMenuController.cs` | Entry |
| Game session phases | `GameSession.cs` (`StarterPioneerSelect`, etc.) | Flow gating |
| Starter companion UI | `StarterPioneerSelectUI.cs` | Companion pick |
| Starter catalog | `StarterPioneerCatalog.cs` (offers Kael-9, etc.) | Offer data |
| Roster + credits | `PioneerRosterManager.cs` | Persist pick |
| Welcome / controls | `GameStartPopup.cs` | 0.4 |
| Loading overlay | `LoadingOverlayController.cs` + `Resources/UI/DMLoadingStarfield.mat` | Transition veil |
| UI theme / palette | `ShiftUiTheme`, `DarkMatterGenesisUiPalette` | Card chrome |
| Save system | `GameSaveSystem.cs` / `GameSaveData` | Persist slot |
| Playable scenes | `Dark Matter Genesis v1.56.unity`, `_Project/Scenes/Dark Matter Genesis v1.57.unity` | Post-charter deploy target (until prologue map scene exists) |

### EXTEND

| Asset | Change needed |
|-------|----------------|
| `StarterPioneerCatalog.StarterAcGrant` | **5000 → 0**; `acCost` free |
| `PioneerRosterManager` new-game credit grant | Stop granting 5000; keep free recruit |
| `GameSaveData` | Add `playerBackgroundId`, `hardModeEnabled` |
| `GameStartPopup` / flow order | Insert background select **before** starter companion |
| Hard Mode damage | Hook −20% on Kade outgoing damage only |

### FUTURE (create)

| Asset | Notes |
|-------|--------|
| `KadeBackgroundDefinition` SO + 6 assets | Per Kade plan |
| `KadeBackgroundSelectUI` | Full-screen cards + Hard Mode toggle |
| Shuttle descent Timeline / cutscene | Cinemachine or Timeline + VO |
| Colony Ops VO clips (charter brief) | Short radio lines |
| Background portrait silhouettes | UI art |
| Synergy highlight VFX on companion cards | When class matches |

### Act 0 exit checklist

- [ ] Background + Hard Mode saved  
- [ ] Companion free-recruited  
- [ ] Spawn ready at Landing Scar (or interim Genesis spawn until map ships)

---

# ACT 1 — Landing & Camp

**Fantasy:** Crash → scavenge → claim mesa → real foothold.  
**Geography:** Landing Scar → Resource Ring → Camp Plateau.  
**Quests:** `prologue_01`–`prologue_04` (**FUTURE** assets; stand-ins below).

## Scene A — Landing Scar

| Step | Do |
|------|-----|
| 1.1–1.2 | Spawn at shuttle; leave thruster heat |
| 1.3 | Loot Emergency Crate |
| 1.4–1.5 | Reach + scan Survey Stake Alpha |
| 1.6 | Soft moth contact |

### CURRENT

| Asset | Role |
|-------|------|
| Player controller / kit | Invector player prefabs under `Prefabs/Players` |
| Survival meters / thermal | Survival + Exposure HUD stack |
| Inventory + hotbar | `InventorySystem`, hotbar UI |
| Optics / scanner stack | `OpticsOverlayUI`, mining scanner scripts |
| Map / minimap | `MapUI*`, `WorldMapProvider` |
| Interaction prompts | `WorldUseController`, interaction UI |
| Combat moths stand-in | Weak fauna: `Ember_Skitter.prefab` or low HP enemy until moth prefab |
| Quest UI | `ActiveQuestHudUI`, Journal, `QuestManager` / `QuestDefinition` |

### EXTEND

| Asset | Change |
|-------|--------|
| Exposure / thermal volumes | Author thruster heat volume on landing pad |
| `QuestDefinition` | New `prologue_01_touchdown` collect/reach/scan objectives |
| Minimap ping | Ops mark → Survey Stake |

### FUTURE

| Asset | Notes |
|-------|--------|
| `POI_CharterShuttle_Wreck` prefab | Crash shell + cargo hatch + crate sockets |
| `POI_SurveyStake_Alpha` | Scan interact + map unlock |
| Landing Scar terrain blockout | B6 shelf ~40 m (ProBuilder/terrain) |
| Cave Scout Moth prefab | Or retag Ember Skitter as “moth” for prologue |
| Ops VO: telemetry / stake | Audio |

---

## Scene B — Resource Ring

| Step | Do |
|------|-----|
| 2.1–2.4 | Gather Basalt ×8, Scrap ×5, Sulfur ×3 |
| 2.5 | Power Breaker (3 relays) |
| 2.6 | First craft (rations / patch) |
| 2.7 | Tube Jackals ×2 |
| 2.8 | Grant Camp Beacon Kit |

### CURRENT

| Asset | Role |
|-------|------|
| Item data + nodes | `Data/Items/Resources`, `Nodes`, tools |
| Crafting | `CraftingStation`, `CraftingManager`, `BlueprintRegistry` |
| Recipes | `Resources/Crafting/BlueprintRegistry.asset` |
| Consumables | `Data/Items/Consumables` |
| Combat + pooling | `CombatProjectileSpawner`, `PoolManager`, weapons in `Data/Items/Melee|Ranged` |
| Companion combat | `PioneerCompanionAgent` / companion combat controllers |
| Enemy stand-ins | `Sulfur_Hound*.prefab`, `Ember_Skitter` as jackal stand-in until Tube Jackal ships |
| Quest board stand-in | `QuestGiver_PioneerGuide.prefab` (temporary only) |

### EXTEND

| Asset | Change |
|-------|--------|
| Resource node volumes | Place quest-tagged gather nodes for exact counts |
| CraftingStation | Portable Fabricator instance (Workbench type) |
| Recipe | Field Rations / Patch Kit if missing — add to BlueprintRegistry |
| Enemy leash / spawn | SurfaceEncounterZone or scripted 2-pack |

### FUTURE

| Asset | Notes |
|-------|--------|
| `POI_PortableFabricator_Ruin` | + Power Breaker puzzle MB |
| `Enemy_TubeJackal` prefab | Replace hound stand-in |
| `Item_CampBeaconKit` | Quest grant → placement item |
| Sulfur creek hazard strip | Thermal edge teaching |
| `prologue_02_scavenge` quest asset | |

---

## Scene C — Claim Camp Plateau

| Step | Do |
|------|-----|
| 3.1–3.3 | Reach mesa; bridge plates / companion boost |
| 3.4 | Clear Brood Mouth nest |
| 3.5–3.7 | Place CC Seed; carry Emergency Cell; power seed |
| 3.8 | Open Building Control Panel |

### CURRENT

| Asset | Role |
|-------|------|
| `BuildingControlPanel.cs` | In-world E → BCP UI |
| Power | `PowerGenerator.cs`, `PowerConsumer.cs` |
| Facility / roles | `FacilityTaskRunner`, `BuildingOperationRegistry` |
| Shelter volume | `Prefabs/Environment/Exposure/Shelter_Safe_Zone.prefab` |
| Crafting stations in scene | `CraftingSceneBootstrap` (Cooking / Workbench) |
| Companion assign hints | `BuildingControlAssignmentHints.cs` |

### EXTEND

| Asset | Change |
|-------|--------|
| BCP Overview-first | Gate other tabs until Act 1 Scene D |
| Placement | Wire Camp Beacon Kit → Lite Building place-on-pad (may use existing building place flow if any; else FUTURE) |
| Carry item | Emergency Cell as slow-carry world item |

### FUTURE

| Asset | Notes |
|-------|--------|
| `Building_CommandCenter_Seed` prefab | Pad snap + power socket + BCP |
| Helix survey paint decals | Rumor-only glyphs |
| Collapsed bridge + salvage plate props | Dual-solution traversal |
| `Enemy_BroodMouth` | Nest clear |
| `Item_EmergencyCell` | Carry friction |
| Sealed tube grate prop | Tease Act 2 caldera |
| `prologue_03_claim_site` quest | |

---

## Scene D — Settlement Bootstrap

| Step | Do |
|------|-----|
| 4.1–4.2 | Build Shelter + Crafting Station |
| 4.3 | Assign companion on BCP |
| 4.4–4.5 | Framing ×4 + Oxygen Scrubber |
| 4.6 | Mini Sulfur Gust (60–90 s) |

### CURRENT

| Asset | Role |
|-------|------|
| `EnvironmentalCrisisHudMode.cs` | Crisis HUD / retract |
| Shelter safe zone prefab | Gust interior |
| Crafting station + recipes | Settlement craft |
| Skills early | `skill_gather_efficiency` (Lv1), `skill_artisan_focus` (Lv3) |
| Companion BCP assign | Building control companions tab |

### EXTEND

| Asset | Change |
|-------|--------|
| Crisis director | Scripted mini gust (not full WeatherDirector yet) |
| Queue pause | Hook craft/production pause during gust (GDD rule preview) |
| Scrubber | Soft O₂ relief volume near camp |

### FUTURE

| Asset | Notes |
|-------|--------|
| `Building_SurvivalShelter` module | If distinct from Shelter_Safe_Zone |
| `Building_CraftingStation_Settlement` | Player-placed variant |
| `Module_OxygenScrubber` | Attachment module |
| Recipe: Reinforced Framing | Item + blueprint |
| `prologue_04_bootstrap` quest | |

### Act 1 exit

CC Seed + Shelter + Station + companion assigned + gust survived → unlock Act 2 cert.

---

# ACT 2 — Cert & Machine

**Fantasy:** Earn deep-gather rights → clear ridge → wake Aether-9 → first Echo → accept **10** cores.  
**Quests:** `prologue_05`–`prologue_08` + `prologue_end_ten_cores` (**FUTURE**).

## Scene E — Level 5 Field Certification

### CURRENT

| Asset | Role |
|-------|------|
| `skill_mining` / `skill_harvesting` | **requiredPlayerLevel: 5** (live) |
| Skill registry / UI | `Resources/Progression/SkillRegistry`, Skills panel |
| Require-level popup | Existing equip/skill gate UX |
| `TrainingDummy.prefab` + `TrainingDummy.cs` | Dummy Yard XP |
| Mining tool item | `Data/Items/Ranged/DM_Mining_Tool.asset` (+ tools folder) |
| Scanner / vein flow | `DMIMiningResourceScanner` stack |
| Gather efficiency / artisan | Pre-gate skills |

### EXTEND

| Asset | Change |
|-------|--------|
| XP sources | Weight dummy / jackal / craft orders for cert loop |
| Gated nodes | Reject tool until skill rank ≥1 |
| Level-up spare point | Guarantee spendable point at Lv5 during cert |

### FUTURE

| Asset | Notes |
|-------|--------|
| `POI_CertificationBeacon` | |
| `POI_DeepBasaltVein` + Vein Cap Lock puzzle | |
| `POI_TubeLaceShelf` | Harvest proof |
| Recipes: Settlement Drill Bit / Harvest Sickle Mk1 | |
| Dummy Yard / Gather Yard blockouts | |
| `prologue_05_field_cert` quest | |

---

## Scene F — Ridge Gauntlet + Ash-Warden

### CURRENT

| Asset | Role |
|-------|------|
| Android enemies | `corrupt_patrol_android.prefab`, `Corrupted Patrol Droid.prefab` → **Ash-Warden stand-in** |
| Humanoid enemy | `HumanoidEnemy_Invector.prefab` |
| Combat VFX / pooling | Laser burn, projectiles, Resources combat prefabs |
| Companion interrupt hooks | Companion combat opportunity patterns |

### EXTEND

| Asset | Change |
|-------|--------|
| Android → boss phases | Beam telegraph, overheat weakpoint, stagger window |
| Arena cover pillars | Level art + collision |
| Soft wipe checkpoint | Arena gate |

### FUTURE

| Asset | Notes |
|-------|--------|
| `Boss_AshWarden_Drone` prefab + boss controller | Or EXTEND corrupt android with boss component |
| `POI_RelayPylon` + dish align puzzle | |
| Glass Hive enemy set | Or skitter pack stand-in |
| `Item_InterfaceKeyFragment` | Repair part 1/3 |
| `prologue_06_ridge` quest | |

---

## Scene G — Wake Aether-9

### CURRENT

| Asset | Role |
|-------|------|
| Quest multi-objective | `QuestDefinition` objective types (Collect/Reach/Talk/Custom) |
| Interactables | `IWorldUsable` pattern |
| Companion escort | Companion AI follow/combat |
| Dialog UI patterns | `QuestGiverDialogUI` (repurpose lines / FUTURE Aether dialog) |

### EXTEND

| Asset | Change |
|-------|--------|
| Custom objectives | Repair part slotting as Custom / Collect |
| Save flags | `aether9Awakened`, trust tier |

### FUTURE

| Asset | Notes |
|-------|--------|
| `POI_Aether9_Shell` idle machine prefab | Centerpiece |
| Valve Vault puzzle prefab | Power Coupler |
| Sparking Conduit extract volume | Memory Bus Ribbon |
| `Item_PowerCoupler`, `Item_MemoryBusRibbon` | Parts 2–3 |
| `Aether9Dialogue` / comms templates | Angry awaken |
| Machine Caldera terrain bowl | ~60 m |
| `prologue_07_aether_repair` quest | |

**Lore:** Say “many cores” here; exact **ten** in Scene H.

---

## Scene H — First Echo + Ten-Core Mandate

### CURRENT

| Asset | Role |
|-------|------|
| `EchoWorldEntity.cs` | World Echo interact / rescue |
| `EchoSignalSpawner.cs` / `EchoSignalRegistry` | Signal ping list |
| `EchoDefinitionSeed.cs` | Seed from NamedPioneerDefinition |
| Companion sense | `CompanionSenseController` nearest Echo |
| Echoes UI | `EchoesPanelUI` |
| Journal quest tracker | Active quest HUD |

### EXTEND

| Asset | Change |
|-------|--------|
| Spawner | Fire on Aether awaken pulse (story hook) |
| Journal | Show `Memory Cores 0/10` tracker after accept |
| Distinguish | UI copy: surface Echo ≠ Aether-9 machine Echo |

### FUTURE

| Asset | Notes |
|-------|--------|
| `POI_EchoCradle` + Frequency Align puzzle | |
| Authored first Echo definition | Named pioneer / imprint |
| `prologue_08_first_echo` + `prologue_end_ten_cores` | |
| Core Site 01 map marker (teaser) | Points into Act 3 |

### Act 2 exit

Lv5 + mining/harvest · Aether-9 Angry · Echo #1 rescued · **0/10** hunt accepted.

---

# ACT 3 — Ten Memories (Memory Cores 1–10)

**Fantasy:** Prove Io’s past with evidence — one core at a time.  
**Starts when:** Act 2 ends. **Not** inside 2–5 h prologue clock.  
**Loop per core:** Find → setpiece → attach to Aether-9 → Resonance Event (10–15 min) → fragment + unlock.

## Act 3 structure

| Band | Cores | Biome bias (phase map) | Player feel |
|------|-------|------------------------|-------------|
| **3A Opening** | 1–2 | B6 → B1 | First Resonance; trust still Angry/wary |
| **3B Ring** | 3–5 | B1–B3 | Expedition rhythm; side content allowed |
| **3C Polar branch** | 6–7 | **B5 Polar** | Cold/rad pressure; big lore shards |
| **3D Calderas** | 8–9 | **B4** | Living-world danger; high Resonance risk |
| **3E Capstone core** | **10** | B7 / deep | Campaign hinge toward Act IV/V |

## Per-core template (every core)

1. **Lead** — Aether-9 or Ops (early) marks sector; rumor, not truth.  
2. **Travel** — Expedition with trio; exposure + enemies.  
3. **Setpiece** — Puzzle and/or elite/boss; unique POI.  
4. **Secure core** — Carry or sealed case (friction optional).  
5. **Return / field attach** — Slot into Aether-9 (prefer return to shell for 1–3; later allow field uplink).  
6. **Resonance Event** — 10–15 min world change; may injure base-22, pause queues, spawn Echo chance.  
7. **Aftermath** — Journal fragment; trust tick; next core unlock gate.

## Asset map — Act 3 shared

### CURRENT

| Asset | Role |
|-------|------|
| Quest framework | `QuestManager`, `QuestDefinition`, registry |
| Echo systems | Spawn/rescue during Resonance |
| Combat / creatures | Hounds, skitter, androids, dummies |
| Building injury / crisis HUD | Shelter, `EnvironmentalCrisisHudMode` |
| Map / FOW | `MapUI`, `WorldMapProvider` |
| Save | Extend for core list |
| Achievements | `AchievementRegistry` + dynamic templates (optional hooks) |
| Skills mid/late | `skill_field_logistics` (Lv6), combat trees Lv5+ |

### EXTEND

| Asset | Change |
|-------|--------|
| `GameSaveData.memoryCoresAttached[]` | Length 10; ids + timestamps |
| Aether-9 trust enum | Angry → Wary → Advisor → Friend |
| Quest rewards | AC / XP / recipes per core |
| Weather / Resonance | Hook Resonance Supercell weather id when directors land |
| BCP pause | Full pause on Resonance Supercell / sulfur (GDD) |

### FUTURE

| Asset | Notes |
|-------|--------|
| `MemoryCoreDefinition` SO ×10 | Id, biome, setpiece type, Resonance profile, journal text |
| `Item_MemoryCore_01`…`_10` | Or single item + instance id |
| `POI_CoreSite_01`…`_10` | Unique layouts |
| `ResonanceEventDirector` | 10–15 min modifiers |
| Aether-9 attach VFX / UI | Slot cinematic |
| Comms: Aether advisory unlock | After trust gate (not day one) |
| Biome art passes B1–B7 | Per Io phase map |

## Core-by-core sketch (Act 3)

| Core | Working title | Biome | Setpiece type | CURRENT stand-in | FUTURE hero asset |
|------|---------------|-------|---------------|------------------|-------------------|
| 1 | Ash Beacon | B6 fringe | Relay + light elite | Corrupt android + pylon reuse | `POI_CoreSite_01_AshBeacon` |
| 2 | Tube Mouth Cache | B6→B1 tube | Valve / pressure (reuse vault grammar) | Crafting/power props | `POI_CoreSite_02_TubeCache` |
| 3 | Sulfur Blind | B1 | Fog + jackal/hound waves | Sulfur Hound packs | Storm + core case carry |
| 4 | Glass Choir | B2 | Hive nest clear | Skitter / FUTURE Glass Hive | Hive bosslet |
| 5 | Ridge Archive | B3 | Android archive puzzle | Patrol droid + UI puzzle | Data-ghost VO (rumor) |
| 6 | Polar Needle | B5 | Cold/rad shelter hop | Exposure zones + Shelter_Safe_Zone | Polar art + Magnet Wyrm tease |
| 7 | Void Kelp Crypt | B5 | Scan grove puzzle | Scanner tools | Void Kelp set dressing |
| 8 | Caldera Heart | B4 | Boss-tier encounter | Hound V3 / android elite | Caldera boss |
| 9 | Rust Garden Vault | B4/Graveyard overlay | Multi-path puzzle + ambush | Mixed enemies | Rust Garden kit |
| 10 | Deep Memory | B7 / deep | Capstone dungeon | Combo of prior puzzles | `POI_CoreSite_10_DeepMemory` |

After Core 10 attach: Act IV trust/advisor handoff + path to campaign capstone (see quests spine Act V).

## Act 3 exit criteria

- [ ] All **10** cores attached  
- [ ] At least one full Resonance Event experienced  
- [ ] First Echo still in roster (or documented loss rules if any — default: keep)  
- [ ] Aether-9 trust ≥ Wary (Advisor preferred)  
- [ ] Side content (Lost Survey, etc.) optionally available from Core 2+

---

## Cross-act dependency graph (assets)

```
Act0 FUTURE KadeBackgroundSelectUI
  -> EXTEND StarterPioneerCatalog (0 AC)
  -> CURRENT MainMenu / GameSession
  -> FUTURE Shuttle Timeline
       ->
Act1 FUTURE prologue_01..04 + Landing/Ring/Plateau blockouts
  -> CURRENT Inventory, Craft, Combat, BCP, Shelter_Safe_Zone, Crisis HUD
  -> FUTURE CC Seed / Shelter / Station modules
       ->
Act2 CURRENT skill_mining/harvesting Lv5 + TrainingDummy + EchoWorldEntity + Android prefabs
  -> FUTURE Aether9 Shell + Boss Ash-Warden + prologue_05..08
       ->
Act3 FUTURE MemoryCoreDefinition x10 + ResonanceEventDirector
  -> CURRENT QuestManager, Echo, Map, Enemies, Exposure
  -> EXTEND Save + trust + weather pause
```

---

## Prototype stand-ins (ship playable before art)

Until FUTURE prefabs land, play Acts 0–2 on **Genesis v1.56 / v1.57** using:

| Prologue need | Stand-in CURRENT |
|---------------|------------------|
| Tube Jackal | Sulfur Hound / Ember Skitter retune |
| Ash-Warden | `corrupt_patrol_android` + boss HP script |
| Brood Mouth | Larger hound or humanoid elite |
| CC Seed | Empty pad + `BuildingControlPanel` on prop |
| Shelter | `Shelter_Safe_Zone` |
| Craft station | Existing Workbench |
| Aether-9 | Distinct prop + `QuestGiverNpc` dialog lines retargeted |
| First Echo | `EchoSignalSpawner.SpawnTestSignalNearPlayer` authored position |
| Moths | Ember Skitter low damage |

Replace stand-ins without changing quest ids.

---

## Implementation priority (Acts 0–3)

| P | Work | Acts |
|---|------|------|
| P0 | 0 AC + free companion code | 0 |
| P1 | `prologue_01`–`04` quests + Landing/Ring/Plateau greybox | 1 |
| P2 | CC Seed / Shelter / Station placeables + BCP assign | 1 |
| P3 | Lv5 cert yard + mining/harvest proofs | 2 |
| P4 | Ash-Warden + Aether-9 shell + repair quest | 2 |
| P5 | First Echo cradle + 0/10 tracker | 2 |
| P6 | `MemoryCoreDefinition` + Core 1–2 | 3 |
| P7 | ResonanceEventDirector stub | 3 |
| P8 | Cores 3–10 + biome POIs | 3 |

---

## Doc links

| Doc | Use |
|-----|-----|
| `Prologue_Playthrough_Step_By_Step.md` | Ordinal steps 0.1–8.7 |
| `Prologue_Acts_Expanded.md` | Narrative/friction depth Acts 0–2 |
| **This file** | Current vs future asset bible Acts **0–3** |
| `Kade_Background_And_Universe_Backstory_Plan.md` | Background definitions |
| `Io_World_Content_Phase_Map.md` | Biome order for Act 3 |

---

*Update this file when a FUTURE asset ships (move row to CURRENT) or when core sites are renamed.*
