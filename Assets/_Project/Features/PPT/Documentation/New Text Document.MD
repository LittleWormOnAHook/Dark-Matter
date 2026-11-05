# Dark Matter: Genesis � Documentation Index

> **Purpose:** one scannable entry point to every system doc, plan, and roadmap under `Assets/_Project/Documentation/` and `Assets/_Project/Features/`. **Disk truth:** `Architecture/World_Engine_Disk_Status.md` � "shipped" means `.cs` on disk, not "mentioned in a design doc."
> **Scene note (Oct 2026):** newest Genesis scene on disk is `../Scenes/Dark Matter Genesis v1.6.5.unity`. Do not rename scenes; treat `v1.6.5` as playable until the owner confirms otherwise.
> **Art lock:** never delete/move/empty `Design/ArtReference/` (see `.cursor/rules/protect-design-art-reference.mdc`).

## 1. Authority stack (how to reference)

1. `.cursor/rules/` (23 `.mdc` files � listed, not edited here; project rules win all ties)
2. `../GAME_DESIGN_DOCUMENT_5.0.txt` (GDD 5.0 � game-content canon; rev Sep 30, 2026)
3. `AGENTS.md` (repo root � agent workflow)
4. `../Features/Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md` (coding contract, every session)
5. `Architecture/World_Engine_Disk_Status.md` (audit Jul 22, 2026 � what is actually on disk)

Constitutional architecture: `Architecture/Dark_Matter_Framework_2.0_High_Level_Architecture_v1.0.md` (HLA v1.0, frozen) ? `Architecture/Dark_Matter_Technical_Design_Bible.md` (TDB v1.0, the HOW) ? `Architecture/Framework_Folder_Mapping.md` (HLA pillars ? repo paths) ? `Architecture/World_Engine_Reasoning_Map.md` (M3 read path / M4 intelligence loop). Folder overview: `Architecture/README.md`. Validation gate: `Architecture/Dark_Matter_Phase_D_Validation.md` (Runs 1�2 open).

## 2. Canonical by system

| System | Canonical doc(s) | Code it describes (verified on disk) |
|---|---|---|
| Survival / thermal / exposure | GDD 5.0 App. A2; `Architecture/Audits/Audit_04_World.md` | `Scripts/Map/`, `Scripts/Survival/` |
| Player / climb / dash / jetpack | `../Features/Climb/CLIMB_PROBE_BAKER.md` (tool); `Architecture/Audits/Audit_02_Player.md` | `Scripts/Player/`, `Features/Climb/` |
| Combat (master) | `Design/Combat/DMG_Combat_Plan_v2.md` (v2.2, sync Oct 5, 2026 � **start here**) + `Architecture/CursorPlans/combat_master_plan_handoff.md` (agent pickup) + `Design/Combat/DMG_Combat_Audit_Phase1.md` (Sep 30 snapshot, �4 locked) | `Scripts/Combat/`, `Scripts/AI/` |
| Combat melee layers | `Design/Combat/DM_Melee_Locomotion_Layer_Policy.md` (canon; wiki mirror: `Wiki/Combat-Moving-Melee-Layer-Policy.md`) + `Design/Combat/DM_Melee_Animation_Library_Plan.md` (shopping plan, no imports) + `Design/Combat/DMG_Combat_Animation_Tag_Sheet.md` (clip counts) | `Animations/Player/Invector@ShooterMelee_Jetpack.controller` |
| Combat spacing / hit marks | `Design/Combat/DM_Enemy_Spacing_And_Hit_Marks_Plan.md` (rev 3, Oct 7 � Part A built/untested, compiles clean) + `Architecture/Combat_Weapon_Base_Stats.md` (shipped Jul 2026) + `Combat/Moller-Trumbore Technique/Mesh_Hit_and_Surface_Damage_Plan.md` (narrow-phase kernel plan) | `Scripts/Combat/HitMarks/`, `Scripts/AI/Brain/DMEnemyEngagementDirector.cs` |
| Building (stone mode) | `Design/DMG_Building_System_Scope_Plan.md` (**single scope**, sync Sep 30) + `Design/DMG_Building_System_Tickets.md` (`BUILD-###` tracker) | `Scripts/Building/`, `Resources/Building/` |
| Companions / creatures | `Architecture/CursorPlans/companion_invector_rebuild.plan.md` + `Architecture/CursorPlans/dmi_creatures_manager_malbers.plan.md` (Sulfur Hound active) + `Architecture/CursorPlans/sulfur_hound_blender_wolf_reskin.plan.md` | `Scripts/Pioneers/`, `Scripts/Pet/` |
| World / map / terrain | `Architecture/Audits/Audit_04_World.md` + `Architecture/Terrain_Subtile_Split_64_Plan.md` (**planned, do not build**) + `Rendering/DmReflectionProbeLodAuthoring.md` + `Design/Io_Genesis_World_Map_Geography.md` (plan visual ref) | `Scenes/Terrain_*_Content.unity` |
| UI / UITK | UITK-only lock (rules). Loading: `Design/UI/DMG_LoadingScreen_Plan.md` (**implemented**) | `Scripts/UI/` |
| HDRP / graphics | `Architecture/HDRP_Migration_Plan.md` (historical; scene paths stale, see �5) + `Architecture/HDRP_Settings_UI_Plan.md` (plan) + `Architecture/HDRP_Vendor_Material_Audit.md` (Aug 15) | `Scripts/UI/SettingsPanelController.cs`, `Scripts/Core/GameSettings.cs` |
| Audio / comms | `../Features/Communications/Documentation/Dark_Matter_Communication_Framework.md` (roadmap) + `../Features/Communications/Audio/README.md` + `../Features/Communications/Data/README.md` (both placeholders � runtime not on disk) | `Scripts/Audio/` (planned adapters Phase 8) |
| NPC directions (PPT) | `../Features/PPT/Documentation/PPT_System_Design.md` (Phase 1 shipped Aug 2026) | `Features/PPT/` |
| World Engine spine | `../Features/GameState/README.md` + `../Features/WorldState/README.md` + `../Features/Directors/README.md` + `../Features/Validation/README.md` (all Run 1, Jul 2026) | `Features/GameState|WorldState|Directors|Validation/` |
| Loot / chests | `Design/Loot/DM_Loot_Chest_System_Plan.md` (decisions D1�D22 Oct 6, **not implemented**) + `Design/Loot/DM_Loot_Chest_System_Handoff.md` (implementer brief) | `IO_Ancient_Cache`, `DMItemCollection` (planned) |
| Story / quests | `Design/Player_Identity_Kade.md` (**locked** � Kade, 0 UEA, Lv 1, 2 Mars survivors) + `Design/Quests_And_Story_Plan.md` (restored Oct 8, 2026) + `Design/Narrative_Package_Compare_And_Pick.md` + `Design/Narrative_Package_V1_Ash_And_Signal.md` / `V2_Colony_Horizon.md` / `V3_Fracture_Compact.md` / `V4_Crimson_Contract.md` + `Design/Prologue_Acts_Expanded.md` + `Design/Prologue_Playthrough_And_Camp_Bootstrap_Plan.md` + `Design/Stages_0_1_Playthrough_Quest_Stages.md` + `Design/Acts_0_to_3_Asset_Mapped_Plan.md` + `Architecture/Audits/Audit_06_Story.md` | `Scripts/Quests/`, `Scripts/Echoes/` |
| Io world content | `Design/Io_World_Content_Phase_Map.md` (**master W0�W8 map**) + `Design/Io_World_Content_Executive_Summary.md` (1-page) + `Design/Io_World_Content_Milestone_Tickets.md` (`IO-W*`) + `Design/Io_Biome_Ecology_Roster.md` (promote to GDD A2e later) + `Design/Io_Biome_Exploration_Gameplay_Plan.md` + `Design/Io_Underground_Architecture_Plan.md` (later content) + `Design/Io_Biome_Life_Image_Sheet_Plan.md` + `Design/Io_Biome_Life_Sheet_Manifest.md` + `Design/Io_Biome_Scientific_Naming_Trial.md` (trial only, B1�B7 stable) | Flat-terrain prototype (Unity spawn deferred) |
| Colony / sim | `Architecture/Audits/Audit_05_Colony.md` | `Scripts/Pioneers/`, `Scripts/Building/`, `Scripts/Pet/` |
| Core / services | `Architecture/Audits/Audit_01_Core.md` | `Scripts/Core/`, `Scripts/Managers/` |
| Systems (UI/audio/progression) | `Architecture/Audits/Audit_07_Systems.md` | `Scripts/UI/`, `Scripts/Audio/`, `Scripts/Progression/` |
| Editor tools | `Architecture/Audits/Audit_08_EditorTools.md` + `Architecture/Hierarchy_Sort_Context_Menu_Plan.md` (plan) | `Assets/_Project/Editor/` |
| AI 3D pipeline | `Architecture/AI_3D_Asset_Pipeline_Stage1.md` / `Stage2.md` / `Stage3.md` (research only � DMAE V5 is the factory, no models downloaded) | None (no pipeline built) |

## 3. Plans & roadmaps vs shipped

**Shipped** (code on disk): combat weapon base stats, PPT Phase 1, World Engine spine (GameState/WorldState/Directors/Validation), loading screen, companion rebuild track, engagement director Part A (uncommitted/untested).
**Plans (not shipped):** loot chest rework, enemy spacing Part A refinement, melee animation library (shopping only), M�ller�Trumbore carve kernel, terrain subtile split (explicitly deferred), underground map (later content), comms audio/data runtime, AI 3D pipeline.
**Process docs (not game systems):** `Engineering/Unity_Git_Disk_Truth.md` (disk-is-truth policy) + `Engineering/Cloud_Agent_Unity_Safeguards.md` (checklist) + `Engineering/Vendor_Assets_And_Git_Policy.md` + `Engineering/Vendor_Install_Manifest.md` + `Engineering/Unity_Compile_Iteration_Audit.md` (Oct 5) + `Engineering/Grok_Commit_Push_Handoff.md` (Oct 6 handoff) + `Architecture/Unity_Safe_Mode_Recovery.md` + `System_Requirements.md` (aligned Oct 8, 2026 � PC and Mac first, consoles later, per GDD 5.0).

## 4. Features modules

| Module | Doc | Status |
|---|---|---|
| Communications (contract) | `../Features/Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md` | Canonical coding contract |
| Communications (roadmap) | `../Features/Communications/Documentation/Dark_Matter_Communication_Framework.md` | Authoritative radio/crew roadmap |
| Comms Audio / Data | `../Features/Communications/Audio/README.md`, `../Features/Communications/Data/README.md` | Placeholders, runtime not on disk |
| Climb | `../Features/Climb/CLIMB_PROBE_BAKER.md` | v3 probe tool |
| GameState / WorldState / Directors / Validation | `../Features/<Module>/README.md` | Run 1 shipped |
| PPT | `../Features/PPT/Documentation/PPT_System_Design.md` | Phase 1 shipped |

## 5. Archived / superseded / needs-owner-review

- `Architecture/CursorPlans/README.md` � plan archive (completed/superseded agent plans; reference only).
- `Wiki/README.md` � repo mirror map for the GitHub wiki; sync wiki when these files change.
- `Design/Microsoft365/README.txt` � generated Office exports; `.md` files stay canonical.
- `ANIM_PLAN_REVERT.md` � historical revert guide (Aug 2026), not current canon.
- `REPAIR_LOG_2026-08-22.md` � historical log on a restore branch; item statuses are not disk truth.
- `Architecture/HDRP_Migration_Plan.md` � historical migration record; `Main scene` path (`Assets/Dark Matter Genesis v1.56.unity`) predates the `Assets/_Project/Scenes/` layout. Left untouched as history.
- **Quest doc links (resolved Oct 8, 2026):** Restored `Design/Quests_And_Story_Plan.md` from git (`c701c830f`). Retargeted `Prologue_Playthrough_Step_By_Step.md` ? `Design/Prologue_Playthrough_And_Camp_Bootstrap_Plan.md` (�3) in `Acts_0_to_3_Asset_Mapped_Plan.md`, `Prologue_Acts_Expanded.md`, and `Stages_0_1_Playthrough_Quest_Stages.md`.
- **Rule/GDD drift (resolved Oct 8, 2026):** `dark-matter-genesis-gdd.mdc` aligned to GDD 5.0 (rev Sep 30, 2026) � **0 UEA, level 1, no inventory, 2 Mars-survivor companions, starter pick retired**.
- **Scene-label drift (resolved Oct 8, 2026):** `unity-agent-workflow.mdc` + `dark-matter-genesis-core.mdc` aligned to playable `v1.6.5` (matches disk + `AGENTS.md` + GDD). No scenes renamed.
- **Platform lock (confirmed Oct 8, 2026 by owner):** PC and Mac first, consoles later (Xbox and PlayStation); no mobile ship target. Matches GDD 5.0 and `dark-matter-genesis-core.mdc`.
- **Nothing here is proposed for deletion.** If the owner wants cleanup, candidates for archive (not delete) are the historical items above � confirm before moving anything.