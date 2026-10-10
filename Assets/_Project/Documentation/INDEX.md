# Dark Matter: Genesis — Documentation Index

> **Purpose:** one scannable entry point to every system doc, plan, and roadmap under `Assets/_Project/Documentation/` and `Assets/_Project/Features/`. **§2 lists all known `_Project` docs per system** (Primary vs Related); update this index when adding Design, Architecture, or Features documentation. **Disk truth:** `Architecture/World_Engine_Disk_Status.md` — "shipped" means `.cs` on disk, not "mentioned in a design doc."
> **Scene note (Oct 2026):** newest Genesis scene on disk is `../Scenes/Dark Matter Genesis v1.6.5.unity`. Do not rename scenes; treat `v1.6.5` as playable until the owner confirms otherwise.
> **Art lock:** never delete/move/empty `Design/ArtReference/` LifeSheets PNGs (see `.cursor/rules/protect-design-art-reference.mdc`); scene-concept `.md` files under `Design/ArtReference/SceneConcepts/` are listed under **Io world content**.

## 1. Authority stack (how to reference)

1. `.cursor/rules/` (**23** `.mdc` files — full inventory, topics, and locks: [`../../.cursor/rules/INDEX.md`](../../.cursor/rules/INDEX.md); project rules win all ties)
2. `../GAME_DESIGN_DOCUMENT_5.0.txt` (GDD 5.0 — game-content canon; rev Sep 30, 2026)
3. `AGENTS.md` (repo root — agent workflow)
4. `../Features/Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md` (coding contract, every session)
5. `Architecture/World_Engine_Disk_Status.md` (audit Jul 22, 2026 — what is actually on disk)

Constitutional architecture: `Architecture/Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md` (HLA v1.0, frozen) → `Architecture/Dark_Matter_Technical_Design_Bible.md` (TDB v1.0, the HOW) → `Architecture/Framework_Folder_Mapping.md` (HLA pillars ↔ repo paths) → `Architecture/World_Engine_Reasoning_Map.md` (M3 read path / M4 intelligence loop). Folder overview: `Architecture/README.md`. Validation gate: `Architecture/Dark_Matter_Phase_D_Validation.md` (Runs 1–2 open).

### 1b. Agent quick paths — Combat

**Design combat roadmap (start here for combat planning):** [`Design/Combat/DMG_Combat_Plan_v2.md`](Design/Combat/DMG_Combat_Plan_v2.md) — **Master Plan v2.4** (Oct 8, 2026 — Phase 3 accepted; Phase 4 Combat Director next; filename stays `DMG_Combat_Plan_v2.md` until a v3+ file replaces it — then update this row to the new path and version line in that doc).

| Role | Doc | Use when |
|---|---|---|
| Phased roadmap, §31 checklist, acceptance gates | `Design/Combat/DMG_Combat_Plan_v2.md` (**v2.4**) | Planning combat work, phase sign-off, reaudit status |
| Disk / engineering authority | `../Features/Combat/Documentation/Dark_Matter_Combat_System.md` | What is on disk, combat flow, tuning hooks, code map |
| Agent session pickup | `Architecture/CursorPlans/combat_master_plan_handoff.md` | Handoff between agents / chats |
| Spacing & hit marks | `Design/Combat/DM_Enemy_Spacing_And_Hit_Marks_Plan.md` + `Architecture/Combat_Weapon_Base_Stats.md` | Engagement director, hit marks, weapon base stats |
| Melee layers / animation | `Design/Combat/DM_Melee_Locomotion_Layer_Policy.md`, `Design/Combat/DM_Melee_Animation_Library_Plan.md`, `Design/Combat/DMG_Combat_Animation_Tag_Sheet.md` | Animator layer policy, clip shopping, tag sheet |

**Relationship:** `Dark_Matter_Combat_System.md` = **Features-layer disk/engineering authority**; `DMG_Combat_Plan_v2.md` = **design phased roadmap & acceptance** (keep both aligned; plan reaudit tracks git/disk, Features doc tracks implementation shape).

## 2. By system (all related docs)

Quick map — full lists in **§2.1–§2.19**.

| System | Primary (start here) | Code it describes (verified on disk) |
|---|---|---|
| Survival / thermal / exposure | GDD 5.0 App. A2; `Architecture/Audits/Audit_04_World.md` | `Scripts/Map/`, `Scripts/Survival/` |
| Player / climb / dash / jetpack | `../Features/Climb/CLIMB_PROBE_BAKER.md`; `Architecture/Audits/Audit_02_Player.md` | `Scripts/Player/`, `Features/Climb/`, `Features/Jetpack/`, `Features/Locomotion/` |
| Combat | `Design/Combat/DMG_Combat_Plan_v2.md` (**v2.4**); `../Features/Combat/Documentation/Dark_Matter_Combat_System.md` | `Scripts/Combat/`, `Scripts/AI/`, `Features/Combat/` |
| Building (stone mode) | `Design/DMG_Building_System_Scope_Plan.md` | `Scripts/Building/`, `Resources/Building/` |
| Companions / creatures | `Architecture/CursorPlans/companion_invector_rebuild.plan.md` | `Scripts/Pioneers/`, `Scripts/Pet/` |
| World / map / terrain | `Architecture/Audits/Audit_04_World.md` | `Scenes/Terrain_*_Content.unity`, `Scripts/Map/` |
| UI / UITK | `Design/UI/DMG_LoadingScreen_Plan.md`; UITK rules (`.cursor/rules/dark-matter-genesis-uitk-lock.mdc`) | `Scripts/UI/`, `Assets/UI Toolkit/` |
| HDRP / graphics | `Architecture/HDRP_Settings_UI_Plan.md`; `System_Requirements.md` | `Scripts/UI/SettingsPanelController.cs`, `Scripts/Core/GameSettings.cs` |
| Audio / comms | `../Features/Communications/Documentation/Dark_Matter_Communication_Framework.md` | `Scripts/Audio/` (Phase 8 adapters planned) |
| NPC directions (PPT) | `../Features/PPT/Documentation/PPT_System_Design.md` | `Features/PPT/` |
| World Engine spine | `Architecture/World_Engine_Disk_Status.md`; `../Features/GameState/README.md` | `Features/GameState|WorldState|Directors|Validation/` |
| Loot / chests | `Design/Loot/DM_Loot_Chest_System_Plan.md` | `IO_Ancient_Cache`, `DMItemCollection` (planned) |
| Story / quests | `Design/Quests_And_Story_Plan.md`; `Design/Player_Identity_Kade.md` | `Scripts/Quests/`, `Scripts/Echoes/` |
| Io world content | `Design/Io_World_Content_Phase_Map.md` | Flat-terrain prototype (Unity spawn deferred) |
| Colony / sim | `Architecture/Audits/Audit_05_Colony.md` | `Scripts/Pioneers/`, `Scripts/Building/`, `Scripts/Pet/` |
| Core / services | `Architecture/Audits/Audit_01_Core.md` | `Scripts/Core/`, `Scripts/Managers/` |
| Systems (UI/audio/progression) | `Architecture/Audits/Audit_07_Systems.md` | `Scripts/UI/`, `Scripts/Audio/`, `Scripts/Progression/` |
| Editor tools | `Architecture/Audits/Audit_08_EditorTools.md` | `Assets/_Project/Editor/` |
| AI 3D pipeline | `Architecture/AI_3D_Asset_Pipeline_Stage1.md` | None (research only) |

### 2.1 Survival / thermal / exposure

- **Primary:** `../GAME_DESIGN_DOCUMENT_5.0.txt` (Appendix A — thermal single meter, exposure canon); `Architecture/Audits/Audit_04_World.md` (world/survival audit slice)
- **Related:** `Architecture/World_Engine_Disk_Status.md` (shipped vs planned); `Design/Io_Biome_Exploration_Gameplay_Plan.md` (surface hazards / exploration loops)

### 2.2 Player / climb / dash / jetpack

- **Primary:** `Architecture/Audits/Audit_02_Player.md`; `../Features/Climb/CLIMB_PROBE_BAKER.md` (climb probe bake tool v3)
- **Related:** `.cursor/rules/dark-matter-genesis-player-physics.mdc` (capsule/climb/jetpack locks — not under Documentation); `Design/Combat/DM_Melee_Locomotion_Layer_Policy.md` (melee-on-locomotion layers — cross-ref combat); **Genesis Studio → Player → Player Prefab / Enemy Prefab / Definitions** (Meshy Invector prefab swap + definition library; Tools → Prefab Creator windows keep the left Custom list)

### 2.3 Combat

- **Primary:** `Design/Combat/DMG_Combat_Plan_v2.md` (**Master Plan v2.4**, Oct 8 2026 — Phase 3 accepted, Phase 4 next — roadmap & acceptance); `../Features/Combat/Documentation/Dark_Matter_Combat_System.md` (disk map, flow, tuning); `../Features/Combat/README.md` (module entry)
- **Related — audits & handoffs:** `Design/Combat/DMG_Combat_Audit_Phase1.md` (Sep 30, §4 locked); `Architecture/Audits/Audit_03_Combat.md`; `Architecture/CursorPlans/combat_master_plan_handoff.md`; `Architecture/CursorPlans/README.md` (plan archive index)
- **Related — spacing & hit marks:** `Design/Combat/DM_Enemy_Spacing_And_Hit_Marks_Plan.md` (rev 3, Oct 7 — Part A built/untested); `Architecture/Combat_Weapon_Base_Stats.md` (shipped Jul 2026); `Combat/Moller-Trumbore Technique/Mesh_Hit_and_Surface_Damage_Plan.md` (narrow-phase kernel / MT plan)
- **Related — melee & animation:** `Design/Combat/DM_Melee_Locomotion_Layer_Policy.md` (canon); `Design/Combat/DM_Melee_Animation_Library_Plan.md` (shopping plan, no imports); `Design/Combat/DMG_Combat_Animation_Tag_Sheet.md` (clip counts); `Wiki/Combat-Moving-Melee-Layer-Policy.md` (wiki mirror of melee layer policy)
- **Related — wiki mirrors (GitHub sync):** `Wiki/README.md` (map + push steps); `Wiki/Documentation-Index.md`; `Wiki/Cursor-Rules-Index.md`; `Wiki/Combat-System.md`; `Wiki/Archive-Policy.md`; `Wiki/Economy-UEA-and-The-Overseer.md`

### 2.4 Building (stone mode)

- **Primary:** `Design/DMG_Building_System_Scope_Plan.md` (**single scope**, sync Sep 30)
- **Related:** `Design/DMG_Building_System_Tickets.md` (`BUILD-###` tracker); `Architecture/Audits/Audit_05_Colony.md` (colony/build overlap); `.cursor/rules/dark-matter-genesis-building-studio.mdc` (Building Studio registration)

### 2.5 Companions / creatures

- **Primary:** `Architecture/CursorPlans/companion_invector_rebuild.plan.md`
- **Related:** `Architecture/CursorPlans/dmi_creatures_manager_malbers.plan.md` (Sulfur Hound active); `Architecture/CursorPlans/sulfur_hound_blender_wolf_reskin.plan.md`; GDD 5.0 (roster / Echo / trio canon)

### 2.6 World / map / terrain

- **Primary:** `Architecture/Audits/Audit_04_World.md`
- **Related:** `Architecture/Terrain_Subtile_Split_64_Plan.md` (**planned — do not build**); `Rendering/DmReflectionProbeLodAuthoring.md`; `Design/Io_Genesis_World_Map_Geography.md` (plan visual ref); `Design/Io_Underground_Architecture_Plan.md` (later content — surface map `?` teaser only until owner asks)

### 2.7 UI / UITK

- **Primary:** `.cursor/rules/dark-matter-genesis-uitk-lock.mdc` + `dark-matter-genesis-ui-toolkit.mdc` + `dark-matter-genesis-ui-palette.mdc` (UITK-only lock & palette)
- **Related:** `Design/UI/DMG_LoadingScreen_Plan.md` (**implemented**); `Architecture/HDRP_Settings_UI_Plan.md` (graphics settings UI plan); `Architecture/Audits/Audit_07_Systems.md` (UI/audio/progression audit)

### 2.8 HDRP / graphics

- **Primary:** `Architecture/HDRP_Settings_UI_Plan.md` (plan); `Architecture/HDRP_Vendor_Material_Audit.md` (refresh via HDRP menu); `System_Requirements.md` (RT advisories, PC/Mac first)
- **Related:** `Rendering/DmReflectionProbeLodAuthoring.md`; **Archive (historical only):** stub `Architecture/HDRP_Migration_Plan.md` → `Archive/HDRP_Migration_Plan.md`; summary `Archive/Historical_Engineering_Notes.md` §3

### 2.9 Audio / comms

- **Primary:** `../Features/Communications/Documentation/Dark_Matter_Communication_Framework.md` (radio/crew roadmap)
- **Related:** `../Features/Communications/Audio/README.md`, `../Features/Communications/Data/README.md` (placeholders — runtime not on disk); `Architecture/Audits/Audit_07_Systems.md` (audio slice)

### 2.10 NPC directions (PPT)

- **Primary:** `../Features/PPT/Documentation/PPT_System_Design.md` (Phase 1 shipped Aug 2026)

### 2.11 World Engine spine

- **Primary:** `Architecture/World_Engine_Disk_Status.md`; `Architecture/World_Engine_Reasoning_Map.md`
- **Related — architecture:** `Architecture/Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md`; `Architecture/Dark_Matter_Technical_Design_Bible.md`; `Architecture/Framework_Folder_Mapping.md`; `Architecture/README.md`; `Architecture/Dark_Matter_Phase_D_Validation.md` (Runs 1–2 open)
- **Related — Features modules:** `../Features/GameState/README.md`, `../Features/WorldState/README.md`, `../Features/Directors/README.md`, `../Features/Validation/README.md` (Run 1, Jul 2026)

### 2.12 Loot / chests

- **Primary:** `Design/Loot/DM_Loot_Chest_System_Plan.md` (decisions D1–D22 Oct 6, **not implemented**)
- **Related:** `Design/Loot/DM_Loot_Chest_System_Handoff.md` (implementer brief)

### 2.13 Story / quests

- **Primary:** `Design/Player_Identity_Kade.md` (**locked** — Kade, 0 UEA, Lv 1, 2 Mars survivors); `Design/Quests_And_Story_Plan.md` (restored Oct 8, 2026)
- **Related — prologue & stages:** `Design/Prologue_Playthrough_And_Camp_Bootstrap_Plan.md` (§3 step-by-step canon); `Design/Prologue_Acts_Expanded.md`; `Design/Stages_0_1_Playthrough_Quest_Stages.md`; `Design/Acts_0_to_3_Asset_Mapped_Plan.md`
- **Related — narrative packages:** `Design/Narrative_Package_Compare_And_Pick.md`; `Design/Narrative_Package_V1_Ash_And_Signal.md` / `V2_Colony_Horizon.md` / `V3_Fracture_Compact.md` / `V4_Crimson_Contract.md`
- **Related — audit:** `Architecture/Audits/Audit_06_Story.md`

### 2.14 Io world content

- **Primary:** `Design/Io_World_Content_Phase_Map.md` (**master W0–W8 map**); `Design/Io_World_Content_Executive_Summary.md` (1-page)
- **Related — tickets & ecology:** `Design/Io_World_Content_Milestone_Tickets.md` (`IO-W*`); `Design/Io_Biome_Ecology_Roster.md` (promote to GDD A2e later); `Design/Io_Biome_Exploration_Gameplay_Plan.md`; `Design/Io_Biome_Scientific_Naming_Trial.md` (trial only, B1–B7 stable)
- **Related — life sheets (plans only; PNGs under ArtReference/LifeSheets — art lock):** `Design/Io_Biome_Life_Image_Sheet_Plan.md`; `Design/Io_Biome_Life_Sheet_Manifest.md`
- **Related — geography & underground:** `Design/Io_Genesis_World_Map_Geography.md`; `Design/Io_Underground_Architecture_Plan.md` (later content)
- **Related — scene concepts (markdown only):** `Design/ArtReference/SceneConcepts/Io_Biome_Scene_Concept_Master.md`; `Design/ArtReference/SceneConcepts/Io_Scene_Concept_Reference.md`; `Design/ArtReference/SceneConcepts/Io_Scene_Concept_ShotList_Meridian6_AshVein.md`
- **Related — agent plans:** `Architecture/CursorPlans/io_biome_ecology_expand_013bcb2e.plan.md`; `Architecture/CursorPlans/io_ecology_content_atlas.plan.md`

### 2.15 Colony / sim

- **Primary:** `Architecture/Audits/Audit_05_Colony.md`
- **Related:** `Design/DMG_Building_System_Scope_Plan.md`; `Architecture/CursorPlans/companion_invector_rebuild.plan.md` (base-camp companion track)

### 2.16 Core / services

- **Primary:** `Architecture/Audits/Audit_01_Core.md`
- **Related:** `Architecture/World_Engine_Disk_Status.md`; `Engineering/Unity_Git_Disk_Truth.md` (save/load & disk policy — see also §3)

### 2.17 Systems (UI / audio / progression)

- **Primary:** `Architecture/Audits/Audit_07_Systems.md`
- **Related:** `Design/UI/DMG_LoadingScreen_Plan.md`; `../Features/Communications/Documentation/Dark_Matter_Communication_Framework.md`; `Architecture/HDRP_Settings_UI_Plan.md`

### 2.18 Editor tools

- **Primary:** `Architecture/Audits/Audit_08_EditorTools.md`
- **Related:** `Architecture/Hierarchy_Sort_Context_Menu_Plan.md` (plan); `../Features/Climb/CLIMB_PROBE_BAKER.md` (Editor bake tool); Genesis / Building / Combat Studio behavior — `.cursor/rules/dark-matter-genesis-genesis-studio.mdc`, `dark-matter-genesis-building-studio.mdc`, `dark-matter-genesis-studio-system-edit-recall.mdc`

### 2.19 AI 3D pipeline

- **Primary:** `Architecture/AI_3D_Asset_Pipeline_Stage1.md`
- **Related:** `Architecture/AI_3D_Asset_Pipeline_Stage2.md`; `Architecture/AI_3D_Asset_Pipeline_Stage3.md` (research only — DMAE V5 is the factory, no models downloaded)

## 3. Plans & roadmaps vs shipped

**Shipped** (code on disk): combat weapon base stats, PPT Phase 1, World Engine spine (GameState/WorldState/Directors/Validation), loading screen, companion rebuild track, engagement director Part A (uncommitted/untested).
**Plans (not shipped):** loot chest rework, enemy spacing Part A refinement, melee animation library (shopping only), Möller–Trumbore carve kernel, terrain subtile split (explicitly deferred), underground map (later content), comms audio/data runtime, AI 3D pipeline.
**Process docs (not game systems):** `Engineering/Unity_Git_Disk_Truth.md` (disk-is-truth policy) + `Engineering/Cloud_Agent_Unity_Safeguards.md` (checklist) + `Engineering/Vendor_Assets_And_Git_Policy.md` + `Engineering/Vendor_Install_Manifest.md` + `Engineering/Unity_Compile_Iteration_Audit.md` (Oct 5) + `Engineering/Grok_Commit_Push_Handoff.md` (Oct 6 handoff) + `Architecture/Unity_Safe_Mode_Recovery.md` + `System_Requirements.md` (aligned Oct 8, 2026 — PC and Mac first, consoles later, per GDD 5.0).

## 4. Features modules (all module docs)

| Module | Primary | Related / README | Status |
|---|---|---|---|
| **Communications** | `../Features/Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md` | `../Features/Communications/Documentation/Dark_Matter_Communication_Framework.md`; `../Features/Communications/Audio/README.md`; `../Features/Communications/Data/README.md` | Contract canonical; comms runtime placeholders |
| **Climb** | `../Features/Climb/CLIMB_PROBE_BAKER.md` | Runtime profiles under `Features/Climb/Runtime/` (no separate `.md`) | v3 probe tool + climb runtime |
| **Combat** | `../Features/Combat/Documentation/Dark_Matter_Combat_System.md` | `../Features/Combat/README.md`; design stack §2.3 + §1b | Features-layer authority; legacy in `Scripts/Combat/` |
| **Directors** | `../Features/Directors/README.md` | WoOS director services | Run 1 shipped (Jul 2026) |
| **GameState** | `../Features/GameState/README.md` | Snapshot providers under `Features/GameState/` | Run 1 shipped |
| **Jetpack** | — | Code only: `Features/Jetpack/` (no module `.md` yet) | On disk |
| **Locomotion** | — | Code only: `Features/Locomotion/` (no module `.md` yet) | On disk |
| **PlayerSystems** | — | Code only: `Features/PlayerSystems/` (no module `.md` yet) | On disk |
| **PPT** | `../Features/PPT/Documentation/PPT_System_Design.md` | — | Phase 1 shipped (Aug 2026) |
| **Validation** | `../Features/Validation/README.md` | Smoke keys / validation runtime | Run 1 shipped |
| **WorldState** | `../Features/WorldState/README.md` | Snapshot providers under `Features/WorldState/` | Run 1 shipped |

Cross-module architecture index: **§2.11** and `Architecture/Framework_Folder_Mapping.md`.

## 5. Archived / superseded / needs-owner-review

- **`Archive/`** — historical engineering notes moved 2026-10-08. **Entry point:** `Archive/Historical_Engineering_Notes.md` (consolidated). Full originals: `Archive/ANIM_PLAN_REVERT.md`, `Archive/REPAIR_LOG_2026-08-22.md`, `Archive/HDRP_Migration_Plan.md`. Stubs at former paths (`ANIM_PLAN_REVERT.md`, `REPAIR_LOG_2026-08-22.md`, `Architecture/HDRP_Migration_Plan.md`) point here; **not referenced unless the owner asks.**
- `Architecture/CursorPlans/README.md` — plan archive (completed/superseded agent plans; reference only).
- **`Wiki/`** — repo mirrors for [GitHub wiki](https://github.com/LittleWormOnAHook/Dark-Matter/wiki). **Start:** `Wiki/README.md` (page map, stale-term grep, clone/push steps). Summaries: Documentation Index, Cursor Rules Index, Combat System (+ plan v2.4), Archive policy, UEA / The Overseer rename — sync wiki when these change.
- `Design/Microsoft365/README.txt` — generated Office exports; `.md` files stay canonical (HTML mirrors under `Design/Exports/` are export-only).
- **Quest / prologue canon (confirmed Oct 8, 2026 by owner):** `Design/Quests_And_Story_Plan.md` (**0 UEA, 2 Mars survivors**) and `Design/Prologue_Playthrough_And_Camp_Bootstrap_Plan.md` **§3** are step-by-step canonical — no edits unless drift found.
- **Quest doc links (resolved Oct 8, 2026):** Restored `Design/Quests_And_Story_Plan.md` from git (`c701c830f`). Retargeted `Prologue_Playthrough_Step_By_Step.md` → `Design/Prologue_Playthrough_And_Camp_Bootstrap_Plan.md` (§3) in `Acts_0_to_3_Asset_Mapped_Plan.md`, `Prologue_Acts_Expanded.md`, and `Stages_0_1_Playthrough_Quest_Stages.md`.
- **Rule/GDD drift (resolved Oct 8, 2026):** `dark-matter-genesis-gdd.mdc` aligned to GDD 5.0 (rev Sep 30, 2026) — **0 UEA, level 1, no inventory, 2 Mars-survivor companions, starter pick retired**.
- **Scene-label drift (resolved Oct 8, 2026):** `unity-agent-workflow.mdc` + `dark-matter-genesis-core.mdc` + `confirm-before-depot-restore.mdc` aligned to playable `v1.6.5` (matches disk + `AGENTS.md` + GDD). No scenes renamed.
- **Platform lock (confirmed Oct 8, 2026 by owner):** PC and Mac first, consoles later (Xbox and PlayStation); no mobile ship target. Matches GDD 5.0 and `dark-matter-genesis-core.mdc`.
