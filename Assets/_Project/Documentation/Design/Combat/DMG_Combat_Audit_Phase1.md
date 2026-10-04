# Dark Matter: Genesis: Combat Audit, Phase 1
## Read-only audit of the Unity project against Master Plan v2 (September 30, 2026)

Scope: `A:\Dark Matter Genesis`, branch `cursor/wip-clean-20260919`, read only. No project, repo or wiki file was changed. All evidence paths are relative to `Assets/_Project/` unless they start with `Assets/`. Known testing values (200 AC, start level 5, 25 skill points, starting items) and the no-NavMesh rule are intentional and are not gaps.

Status key: **Built** = works as the plan intends (or better). **Partial** = some of it exists, gap stated. **Missing** = nothing usable yet. **Conflicts** = the code does it differently from the plan; a decision is needed before building.

## 1. Summary

| Status | Count |
|---|---|
| Built | 8 |
| Partial | 28 |
| Missing | 25 |
| Conflicts | 7 |
| **Total systems checked** | **68** |

**Headline.** The core loop is further along than the plan documents say: third-person melee and ranged combat (through Invector), crits, stamina, dodge roll, dash, ammo types, a 5-type status system, the Hot Cross, a live hex skill tree, a 3-companion expedition trio, and death/loot are all working. Everything that makes the plan distinctive (utility brain, Director and tokens, poise/parry, Momentum, finishers, Sick Stick, body damage, factions) is missing. The seven conflicts are mostly naming and structure (elements, skill branches, companion roles, damage contract, separate brains) and should be decided before Phase 2 code is written.

**Top 5 gaps**
1. **No Damage Profile / event layer** (C10). `IDamageable.TakeDamage(float, GameObject, bool)` carries no type, element, poise or status. Poise, resistances, Momentum, the Sick Stick crit chain, finishers and multiplayer all depend on this.
2. **No unified brain, Director or tokens** (A04, A13). There are three separate hard-coded state machines (enemy, creature, companion) and no attack tokens, so every enemy can attack at once.
3. **No poise, parry or i-frames** (C06, C08, C09). Stagger exists only as a ragdoll hit-stagger, and dodge and dash give no invulnerability.
4. **Momentum, finishers, specials, Overdrive and Sick Stick are all missing** (M01-M04). This includes animation gaps: 3 finisher clips and 0 puke clips.
5. **Element model conflicts with the plan** (C15, C16). The code uses Ice/Electricity/Fire, Plasma corrodes instead of burning, elements exist on ammo only, and there's no fuel/power resource.

## 2. Summary table

| ID | Plan § | System | Status | Current build (short) |
|---|---|---|---|---|
| A01 | §2 | Composition (Body + Brain + profiles) | **Partial** | Enemies are assembled from components, with no inheritance chain: an EnemyDefinition asset plus EnemySenses, EnemyCombat and EnemyHealth, and Invector body bridges |
| A02 | §3 | Performance principles | **Partial** | Vision checks refresh every 0.12 s, not every frame |
| A03 | §4 | AI simulation levels L0-L3 | **Missing** | Only render/animator LOD exists (HumanoidPerformanceController) |
| A04 | §5 | Unified utility-scoring brain | **Conflicts** | There are three separate hard-coded brains |
| A05 | §5 | Invector decision (wrap vs replace) | **Partial** | In practice the code already wraps Invector: project bridges drive Invector's motor, melee, shooter, ragdoll and damage for the player, enemies and companions |
| A06 | §6 | Archetypes | **Partial** | EnemyBehaviorPreset offers Custom, AggressiveHunter, Guard, PatrolInvestigator and Ambush |
| A07 | §7 | Personality | **Missing** | None |
| A08 | §8 | Tactical traits | **Partial** | Behaviours exist for investigating noise, searching the last known position, returning home (leash), Defensive state and combat positioning |
| A09 | §9 | Vision and hearing | **Partial** | Vision is 16 m with a 110° FOV and raycast obstruction |
| A10 | §9 | Noise events | **Partial** | EnemyNoiseEvents has two kinds, Generic and CombatImpact |
| A11 | §9 | Awareness meter + icon | **Missing** | None |
| A12 | §9 | Last known position | **Partial** | Enemies store the last known position and investigate or search it |
| A13 | §10 | Combat Director + attack tokens | **Missing** | None for enemies |
| A14 | §10 | Group morale | **Missing** | None |
| A15 | §11 | Combat memory | **Missing** | None |
| A16 | §12 | Behaviour states | **Partial** | Enemies use Idle, Wander, Patrol, Investigate, ReturnHome, Chase, Defensive, Attack and Search |
| A17 | §12 | Condition states + desperation | **Missing** | Health only |
| A18 | §13 | Objectives | **Partial** | Guard and patrol presets plus SurfacePatrolRoute |
| C01 | §14 | Weapon interface / data | **Partial** | ItemData holds melee damage, range, cooldown, stamina cost, knockback, crit chance 0.1 and multiplier x2, invectorWeaponId and grip |
| C02 | §14 | Melee light / heavy / combos / block | **Partial** | Built through Invector: vShooterMeleeInput drives vMeleeManager attacks, and combos come from the Invector animator |
| C03 | §14 | Ranged aim / fire | **Built** | ADS on LT/RMB, fire on RT/LMB, hip-fire spread, custom recoil (Invector recoil suppressed), burst fire, projectiles, hitscan beams and grenades |
| C04 | §14 | Critical hits | **Built** | Crits are a random roll per weapon (ItemData.RollCriticalHit, 10% for x2) |
| C05 | §14 | Stamina costs | **Built** | Invector stamina, with stamina costs for melee, roll and dash |
| C06 | §14 | Poise | **Missing** | None |
| C07 | §14 | Stagger | **Partial** | Ranged hits and crits trigger a ragdoll hit-stagger (0.28 s by default) |
| C08 | §14 | Parry | **Missing** | None |
| C09 | §14 | Dodge roll + dash | **Partial** | Dodge roll (gamepad B press / keyboard Q) uses Invector Roll with a stamina cost |
| C10 | §14 | Damage Profile / damage events | **Conflicts** | IDamageable.TakeDamage(float, GameObject, bool isCritical) is the only damage contract |
| C11 | §14 | Status framework | **Partial** | Statuses: Burning, Frozen, Shocked, Corroded, Stabilized |
| C12 | §14 | Resistances | **Missing** | None on enemies |
| C13 | §14 | Feedback (hitstop, colours, VFX, audio, camera, rumble) | **Partial** | Impact VFX and audio per ammo (DMAmmoFxProfile x9, DMAmmoFxCatalog), floating damage numbers, CameraShakeService and laser burn marks |
| C14 | §15 | Melee weapon set | **Partial** | 8 melee items: swords, axes, a spear and a two-hander |
| C15 | §16 | Elements | **Conflicts** | AmmoType has Gunpowder, Plasma, Ice, Electricity, ResonanceStabilizer, Laser, Ion, Fire and Explosive |
| C16 | §16 | Element resources (plasma fuel / power, Ion 33%) | **Conflicts** | Weapons use ammo items (16 ammo assets) with reload |
| C17 | §16 | Element combinations | **Missing** | None |
| M01 | §17 | Momentum meter | **Missing** | None (0 code hits) |
| M02 | §17 | Finishers + unique signal | **Missing** | One finisher-like clip exists (Human_SwordOneHand_Finisher1.fbx) |
| M03 | §17 | Special moves + Overdrive | **Missing** | None |
| M04 | §18 | Sick Stick | **Missing** | None |
| P01 | §19 | Skill tree | **Conflicts** | A hex skill tree is live with 5 categories: Melee 6, Pistols 6, Rifles 12, Survival 9, Player 18 (51 nodes) |
| P02 | §19 | Animation library + profiles | **Partial** | About 11,156 .anim/.fbx files |
| P03 | §20 | Weapon upgrades Mk I-V | **Missing** | A crafting, recipe and blueprint pipeline exists (CraftingManager, RecipeCreator, BlueprintCraftingManager) that upgrades could reuse |
| B01 | §21 | Body components + dismemberment | **Missing** | Ragdoll death and a disintegration dissolve on death exist |
| B02 | §21 | Visual damage | **Partial** | Laser burn marks and the death dissolve |
| E01 | §22 | Environmental combat | **Partial** | Grenades, explosive ammo, AOE bubbles, splash damage and exposure hazard zones |
| E02 | §23 | Factions + relationships | **Missing** | SurfaceThreatKind (Any, Alien, Lifeform, Android) is a spawn filter only |
| E03 | §23 | Encounters | **Partial** | SurfaceEncounterZone, a weighted SurfaceEncounterTable and SurfacePatrolRoute |
| E04 | §23 | Survivor memory | **Missing** | None |
| F01 | §24 | Companion trio (1-3) | **Built** | Up to 3 expedition companions (expeditionTrioIds in the save), managed by PioneerRosterManager and CompanionRosterBridge |
| F02 | §24 | Companions use the same brain | **Conflicts** | Companions have their own brain: CompanionFollowController, CompanionCombatController, CompanionSenseController, CompanionThreatSensor and CompanionCombatCoordinator |
| F03 | §24 | Roles + party buffs | **Conflicts** | 9 SkilledPioneerClass values: ArchitectEngineer, ScienceSpecialist, CombatTactician, InfiltratorScout, IoHybrid, MedTech, LogisticsOfficer, SalvageEngineer,... |
| F04 | §24 | Party debuffs | **Missing** | None |
| F05 | §24 | Companion commands | **Partial** | Keyboard H holds and G follows (hard-coded keys, not in the action map) |
| F06 | §24 | Downed / revive | **Missing** | A fallen companion is sent to the Science Lab as injured (CompanionInjuryHandler) |
| F07 | §24 | Bond | **Missing** | None |
| F08 | §25 | Multiplayer readiness (party slots, events) | **Missing** | A few static events only |
| T01 | §26 | Genesis Studio combat knobs | **Partial** | Combat group: Ammo FX and Hit Catalog |
| T02 | §27 | Combat sandbox | **Partial** | A TrainingDummy exists |
| T03 | §28 | AI debug readout | **Missing** | None |
| T04 | §29 | Save/load | **Partial** | Save v23 stores skills, roster, trio and injured companions |
| T05 | §30 | Combat testing | **Missing** | EditMode tests exist for Directors, GameState, Validation and WorldState only |
| H01 | HUD | Combat HUD | **Partial** | UI Toolkit HUD, ranged crosshair HUD, health HUD for the engaged enemy, enemy health bars and damage numbers |
| H02 | HUD | Hot Cross (quick-select) | **Built** | A gold cross with 4 quadrants |
| H03 | Weapons | Ammo system | **Built** | 9 ammo types across 16 ammo items |
| H04 | Input | Combat bindings | **Partial** | Attack RT/LMB |
| H05 | Extra | Lock-on / combat focus | **Built** | CombatFocusController: 3.5 m focus range with break-lock rules |
| H06 | Extra | Death + loot workflow | **Built** | Player death, death overlay, respawn, enemy death sequence, loot bag and loot dialog |
| H07 | Content | Enemy + creature roster | **Partial** | Humanoid EnemyDefinitions: corrupt_patrol_android, Humanoid_Enemy, The_Evil_One |

## 3. Findings by system

### AI architecture, brain and perception (§2-§13)

**A01 Composition (Body + Brain + profiles) (§2): Partial**
- Current build: Enemies are assembled from components, with no inheritance chain: an EnemyDefinition asset plus EnemySenses, EnemyCombat and EnemyHealth, and Invector body bridges.
- Missing: The decision logic is one hard-coded state machine. There are no Personality, Objective, Faction or Memory components.
- Evidence: `Scripts/AI/EnemyDefinition.cs, EnemyAiController*.cs (partial class), EnemySenses.cs, EnemyCombat.cs, EnemyHealth.cs, Scripts/AI/Invector/*`
- Invector dependency: Body = Invector

**A02 Performance principles (§3): Partial**
- Current build: Vision checks refresh every 0.12 s, not every frame. Humanoids get a distance render/animator LOD (32/64 m, checked every 0.25 s). VFX and tracers are pooled. Configuration uses ScriptableObjects.
- Missing: Every enemy runs its own Update. There is no shared perception scheduler or per-frame budget, and no tiered perception.
- Evidence: `Scripts/AI/EnemySenses.cs, Scripts/AI/HumanoidPerformanceController.cs`

**A03 AI simulation levels L0-L3 (§4): Missing**
- Current build: Only render/animator LOD exists (HumanoidPerformanceController).
- Missing: No Dormant/Background/Active/Full Combat simulation tiers.
- Evidence: `Scripts/AI/HumanoidPerformanceController.cs`

**A04 Unified utility-scoring brain (§5): Conflicts**
- Current build: There are three separate hard-coded brains. Enemies: EnemyAiController, 9 states. Creatures: DMICreatureAiController, 6 states. Companions: CompanionFollow/Combat controllers.
- Conflict: The plan wants one utility-scored brain shared by enemies, creatures and companions. None of the current brains scores actions.
- Evidence: `Scripts/AI/EnemyAiController*.cs, Scripts/Creatures/DMICreatureAiController.cs, Scripts/Companions/CompanionFollowController*.cs, CompanionCombatController.cs`
- Invector dependency: Invector motor/melee/shooter under all three

**A05 Invector decision (wrap vs replace) (§5): Partial**
- Current build: In practice the code already wraps Invector: project bridges drive Invector's motor, melee, shooter, ragdoll and damage for the player, enemies and companions.
- Missing: The decision still needs to be confirmed and written down. Recommendation: wrap Invector as the Body layer and replace only the decision layer.
- Evidence: `Scripts/AI/Invector/EnemyInvector*.cs, Scripts/Player/Invector/Pioneer*.cs, Scripts/Companions/Invector/*`
- Invector dependency: Core

**A06 Archetypes (§6): Partial**
- Current build: EnemyBehaviorPreset offers Custom, AggressiveHunter, Guard, PatrolInvestigator and Ambush. DMICreatureBrainProfile has 4 creature assets.
- Missing: Only 4 presets exist, against 24 archetypes in the plan. The name clashes: code EnemyArchetype means rig type (LegacyCreature / HumanoidInvector), not a behaviour template.
- Evidence: `Scripts/AI/EnemyDefinition.cs, Scripts/Creatures/DMICreatureBrainProfile.cs`

**A07 Personality (§7): Missing**
- Current build: None.
- Missing: No personality weights anywhere (0 code hits).

**A08 Tactical traits (§8): Partial**
- Current build: Behaviours exist for investigating noise, searching the last known position, returning home (leash), Defensive state and combat positioning.
- Missing: No flank, cover, protect, focus-wounded, call-reinforcements or surround traits, and nothing is configurable per trait.
- Evidence: `Scripts/AI/EnemyAiController*.cs`

**A09 Vision and hearing (§9): Partial**
- Current build: Vision is 16 m with a 110° FOV and raycast obstruction. Hearing is 18 m. Noise memory lasts 8 s.
- Missing: No vertical FOV, darkness, crouch or camouflage modifiers. No hearing threshold or direction accuracy.
- Evidence: `Scripts/AI/EnemySenses.cs`

**A10 Noise events (§9): Partial**
- Current build: EnemyNoiseEvents has two kinds, Generic and CombatImpact. Weapon impacts emit a 10 m noise.
- Missing: No typed intensity table (walk, sprint, explosion, vehicle, party size, and so on).
- Evidence: `Scripts/AI/EnemyNoiseEvents.cs, Scripts/Combat/CombatHitResolver.cs`

**A11 Awareness meter + icon (§9): Missing**
- Current build: None.
- Missing: No Unaware/Suspicious/Alert/Combat meter and no icon.

**A12 Last known position (§9): Partial**
- Current build: Enemies store the last known position and investigate or search it.
- Missing: No confidence, direction or movement estimate. Losing the target has no call-allies, split-up or guard-exits options.
- Evidence: `Scripts/AI/EnemyAiController*.cs`

**A13 Combat Director + attack tokens (§10): Missing**
- Current build: None for enemies. On the companion side, CompanionCombatCoordinator schedules turn order and paired bursts, which is close to a token system.
- Missing: No encounter director, no attack tokens, and no shared token pool.
- Evidence: `Scripts/Companions/CompanionCombatCoordinator.cs; Features/Directors (weather, simulation and experience only)`

**A14 Group morale (§10): Missing**
- Current build: None.

**A15 Combat memory (§11): Missing**
- Current build: None.

**A16 Behaviour states (§12): Partial**
- Current build: Enemies use Idle, Wander, Patrol, Investigate, ReturnHome, Chase, Defensive, Attack and Search. Creatures use Idle, Wander, Patrol, Chase, Melee and Spit.
- Missing: Missing states: Alert, Flank, Retreat, Recover, Objective and Flee. The two state sets aren't shared.
- Evidence: `Scripts/AI/EnemyAiController*.cs, Scripts/Creatures/DMICreatureAiController.cs`

**A17 Condition states + desperation (§12): Missing**
- Current build: Health only.
- Missing: No Injured/Critical/Stunned/Disabled layer and no desperation behaviour.
- Evidence: `Scripts/AI/EnemyHealth.cs`

**A18 Objectives (§13): Partial**
- Current build: Guard and patrol presets plus SurfacePatrolRoute.
- Missing: No objective system (protect, retrieve, repair, hunt, loot, and so on).
- Evidence: `Scripts/AI/Encounters/SurfacePatrolRoute.cs`

### Core combat, weapons and elements (§14-§16)

**C01 Weapon interface / data (§14): Partial**
- Current build: ItemData holds melee damage, range, cooldown, stamina cost, knockback, crit chance 0.1 and multiplier x2, invectorWeaponId and grip. It also holds ranged fire rate, accuracy, recoil, ammo type, splash and status data.
- Missing: No light/heavy/combo definitions, parry, finisher set, dismemberment profile, or element separate from ammo.
- Evidence: `Scripts/Data/ItemData.cs`
- Invector dependency: Invector weapon ids

**C02 Melee light / heavy / combos / block (§14): Partial**
- Current build: Built through Invector: vShooterMeleeInput drives vMeleeManager attacks, and combos come from the Invector animator. Block comes from Invector input and shares LT/RMB with aim.
- Missing: Combo strings aren't data-driven. The project's MeleeCombatController.IsBlocking is a stub that always returns false, so project systems never see a block.
- Evidence: `Scripts/Player/Invector/PioneerShooterMeleeInput.cs, Scripts/Interaction/MeleeCombatController.cs, Scripts/Player/Invector/PioneerInvectorBootstrap.cs`
- Invector dependency: vMeleeManager, vShooterMeleeInput

**C03 Ranged aim / fire (§14): Built**
- Current build: ADS on LT/RMB, fire on RT/LMB, hip-fire spread, custom recoil (Invector recoil suppressed), burst fire, projectiles, hitscan beams and grenades.
- Missing: Nothing blocking. Ranged polish is in Phase 12.
- Evidence: `Scripts/Player/Invector/PioneerShooterMeleeInput.cs, Scripts/Combat/RangedFireSolver.cs, CombatProjectile.cs, HitscanBeamMuzzleFollow.cs, DMIGrenade*.cs, Scripts/Player/Invector/PioneerInvectorRecoilUtility.cs`
- Invector dependency: vShooterManager

**C04 Critical hits (§14): Built**
- Current build: Crits are a random roll per weapon (ItemData.RollCriticalHit, 10% for x2). Enemies crit at 8%. Crits can stagger and show crit damage numbers.
- Missing: No element crit effects and no consecutive-crit tracking (the Sick Stick needs it).
- Evidence: `Scripts/Data/ItemData.cs, Scripts/AI/Invector/EnemyInvectorOutgoingDamageBridge.cs, Scripts/Combat/CombatHitResolver.cs`

**C05 Stamina costs (§14): Built**
- Current build: Invector stamina, with stamina costs for melee, roll and dash.
- Missing: Block stamina is left to Invector defaults.
- Evidence: `ItemData.meleeStaminaCost, Resources/Climb/DM_ClimbDashProfile.asset`
- Invector dependency: vThirdPersonController stamina

**C06 Poise (§14): Missing**
- Current build: None.
- Missing: No poise meter. Guard-break and unblockable red flash are also missing.

**C07 Stagger (§14): Partial**
- Current build: Ranged hits and crits trigger a ragdoll hit-stagger (0.28 s by default). Invector hit reactions play on the player and on enemies.
- Missing: Stagger isn't driven by poise.
- Evidence: `Scripts/AI/Invector/EnemyInvectorRagdollBridge.cs, Scripts/Player/Invector/PioneerInvectorSurvivalBridge.cs`
- Invector dependency: vRagdoll, Invector hit reactions

**C08 Parry (§14): Missing**
- Current build: None. HumanF Parry clips exist in the library.
- Missing: No parry window and no counter.

**C09 Dodge roll + dash (§14): Partial**
- Current build: Dodge roll (gamepad B press / keyboard Q) uses Invector Roll with a stamina cost. Dash (B double-tap + hold / Left Alt) uses DMDashController: 4.5 m, 0.55 s cooldown, 22 stamina, with skill modifiers.
- Missing: Neither has invulnerability frames. No perfect-dodge detection.
- Evidence: `Settings/Input/InputSystem_Actions.inputactions, Scripts/Player/PioneerPlayerInputBinder.cs, Features/Dash/Runtime/DMDashController.cs`
- Invector dependency: vThirdPersonController.Roll

**C10 Damage Profile / damage events (§14): Conflicts**
- Current build: IDamageable.TakeDamage(float, GameObject, bool isCritical) is the only damage contract. CombatHitResolver applies direct hits, splash, status, VFX, audio, noise and damage numbers through direct calls. A few static events exist (PlayerCombatEvents, EnemyKillEvents).
- Conflict: The call carries no type, element, poise, status or dismemberment data, so it conflicts with the plan's Damage Profile. Combat isn't event-driven.
- Evidence: `Scripts/Interaction/IDamageable.cs, Scripts/Combat/CombatHitResolver.cs, Scripts/AI/PlayerCombatEvents.cs, EnemyKillEvents.cs`
- Invector dependency: Invector vDamage via bridges

**C11 Status framework (§14): Partial**
- Current build: Statuses: Burning, Frozen, Shocked, Corroded, Stabilized. Re-applying a status refreshes its duration.
- Missing: No max stacks, no immunity window, no boss multiplier.
- Evidence: `Scripts/Combat/StatusEffectType.cs, CombatStatusEffectController.cs`

**C12 Resistances (§14): Missing**
- Current build: None on enemies. EnemyHealth is a flat 60 HP.
- Missing: No per-element resistance by body profile.
- Evidence: `Scripts/AI/EnemyHealth.cs`

**C13 Feedback (hitstop, colours, VFX, audio, camera, rumble) (§14): Partial**
- Current build: Impact VFX and audio per ammo (DMAmmoFxProfile x9, DMAmmoFxCatalog), floating damage numbers, CameraShakeService and laser burn marks.
- Missing: No hitstop. No controller rumble (no SetMotorSpeeds calls). Hit colours follow ammo FX, not the plan's colour table.
- Evidence: `Scripts/Combat/DMAmmoFx*.cs, Scripts/UI/FloatingDamageNumber.cs, CameraShakeService, DMILaserBurnMark*.cs`

**C14 Melee weapon set (§15): Partial**
- Current build: 8 melee items: swords, axes, a spear and a two-hander.
- Missing: No knife, baton or Sick Stick. No backstab bonus.
- Evidence: `Data/Items/Melee/*`
- Invector dependency: Invector melee weapons

**C15 Elements (§16): Conflicts**
- Current build: AmmoType has Gunpowder, Plasma, Ice, Electricity, ResonanceStabilizer, Laser, Ion, Fire and Explosive. Default statuses: Plasma to Corroded, Fire to Burning, Ice to Frozen, Electricity to Shocked. Laser and Ion apply none.
- Conflict: Names differ: Cryo = code Ice, Energy = code Electricity. Code Plasma corrodes, but the plan says Plasma burns. Fire is a separate ammo type. Elements exist only on ammo, never on melee. There's no Ion disintegration and no Energy chain arcs.
- Evidence: `Scripts/Combat/AmmoType.cs, StatusEffectType.cs, CombatStatusEffectController.cs`

**C16 Element resources (plasma fuel / power, Ion 33%) (§16): Conflicts**
- Current build: Weapons use ammo items (16 ammo assets) with reload.
- Conflict: No plasma-fuel or power resource, no Ion 33% cost and no unpowered strike. Needs a decision on how ammo maps to fuel/power.
- Evidence: `Data/Items/Ammo/*, Scripts/Combat/DMRangedAmmoStats.cs`

**C17 Element combinations (§16): Missing**
- Current build: None.

### Momentum, finishers, specials and Sick Stick (§17-§18)

**M01 Momentum meter (§17): Missing**
- Current build: None (0 code hits).

**M02 Finishers + unique signal (§17): Missing**
- Current build: One finisher-like clip exists (Human_SwordOneHand_Finisher1.fbx).
- Missing: No finisher data, sigil, sting or rumble.

**M03 Special moves + Overdrive (§17): Missing**
- Current build: None.

**M04 Sick Stick (§18): Missing**
- Current build: None. The library has no puke or sick clips.
- Missing: No weapon, trigger, stun or immunity.

### Progression: skills, animation, upgrades (§19-§20)

**P01 Skill tree (§19): Conflicts**
- Current build: A hex skill tree is live with 5 categories: Melee 6, Pistols 6, Rifles 12, Survival 9, Player 18 (51 nodes). Each node has 5 ranks, costs scale with branch depth, and allocations are saved.
- Conflict: The plan has 4 branches (Blade, Control, Marksman, Survival). Current nodes are stat-only. skill_momentum_strike, skill_guard_break and skill_counter_rhythm, for example, only add flat melee damage. No move unlocks, Momentum nodes, elemental mastery or respec. Start level 5 and 25 points are known testing values.
- Evidence: `Scripts/Progression/SkillDefinition.cs, PlayerProgressionManager.cs, Resources/Progression/Skills/*`

**P02 Animation library + profiles (§19): Partial**
- Current build: About 11,156 .anim/.fbx files. Keyword counts: attack 321, combo 121, heavy 150, finisher 3, dodge/roll 113, hit react 354, block/parry 120, stun/stagger 5, death 127, draw/holster 30, kick 55, sword 334, knife 17, baton/club 2, puke 0.
- Missing: The clips aren't tagged and there are no animation profiles. Puke, baton and stun clips are the gaps.
- Evidence: `Assets/**/*.anim, *.fbx`
- Invector dependency: Invector animator controllers

**P03 Weapon upgrades Mk I-V (§20): Missing**
- Current build: A crafting, recipe and blueprint pipeline exists (CraftingManager, RecipeCreator, BlueprintCraftingManager) that upgrades could reuse.
- Missing: No weapon upgrade tracks.
- Evidence: `Scripts/Crafting/*, Editor/BlueprintCraftingManagerWindow.cs`

### Body damage (§21)

**B01 Body components + dismemberment (§21): Missing**
- Current build: Ragdoll death and a disintegration dissolve on death exist.
- Missing: No functional body parts and no dismemberment.
- Evidence: `Scripts/AI/Invector/EnemyInvectorRagdollBridge.cs, EnemyDisintegrationEffect.cs`
- Invector dependency: vRagdoll

**B02 Visual damage (§21): Partial**
- Current build: Laser burn marks and the death dissolve.
- Missing: No frost, scorch, armour damage or missing-limb visuals.
- Evidence: `DMILaserBurnMark*.cs, EnemyDisintegrationEffect.cs`

### Environment, factions and encounters (§22-§23)

**E01 Environmental combat (§22): Partial**
- Current build: Grenades, explosive ammo, AOE bubbles, splash damage and exposure hazard zones.
- Missing: No explosive barrels, electrical or water conduction, flammables or interactive machinery.
- Evidence: `Scripts/Combat/DMIGrenade*.cs, DMAoeTriggerBubble.cs`

**E02 Factions + relationships (§23): Missing**
- Current build: SurfaceThreatKind (Any, Alien, Lifeform, Android) is a spawn filter only.
- Missing: No factions (0 code hits).
- Evidence: `Scripts/AI/EnemyDefinition.cs`

**E03 Encounters (§23): Partial**
- Current build: SurfaceEncounterZone, a weighted SurfaceEncounterTable and SurfacePatrolRoute.
- Missing: None of the plan's encounter types (Ambush, Hunt, Crossfire and so on). No escalation and no world events.
- Evidence: `Scripts/AI/Encounters/*`

**E04 Survivor memory (§23): Missing**
- Current build: None.

### Companions and multiplayer (§24-§25)

**F01 Companion trio (1-3) (§24): Built**
- Current build: Up to 3 expedition companions (expeditionTrioIds in the save), managed by PioneerRosterManager and CompanionRosterBridge.
- Evidence: `Scripts/Companions/CompanionRosterBridge.cs, Scripts/Pioneers/PioneerRosterManager.cs`
- Invector dependency: Invector companion bridges

**F02 Companions use the same brain (§24): Conflicts**
- Current build: Companions have their own brain: CompanionFollowController, CompanionCombatController, CompanionSenseController, CompanionThreatSensor and CompanionCombatCoordinator.
- Conflict: The plan wants companions on the enemy brain, set friendly.
- Evidence: `Scripts/Companions/*`
- Invector dependency: Invector

**F03 Roles + party buffs (§24): Conflicts**
- Current build: 9 SkilledPioneerClass values: ArchitectEngineer, ScienceSpecialist, CombatTactician, InfiltratorScout, IoHybrid, MedTech, LogisticsOfficer, SalvageEngineer, CommunicationsOfficer. Group buffs cover radiation resistance, expedition efficiency, combat synergy, move speed and debuff resistance.
- Conflict: The plan has 6 roles (Tactician, Infiltrator, Medic, Tank, Engineer, Marksman) with different buffs. Tank and Marksman have no class.
- Evidence: `Scripts/Pioneers/SkilledPioneerClass.cs, Scripts/Companions/CompanionGroupBuffService.cs, Scripts/Pioneers/CompanionBuffModifier.cs`

**F04 Party debuffs (§24): Missing**
- Current build: None.

**F05 Companion commands (§24): Partial**
- Current build: Keyboard H holds and G follows (hard-coded keys, not in the action map).
- Missing: No radial or D-Pad, and no attack-my-target, fall-back or use-ability commands.
- Evidence: `Scripts/Pioneers/PioneerExpeditionCommandInput.cs`

**F06 Downed / revive (§24): Missing**
- Current build: A fallen companion is sent to the Science Lab as injured (CompanionInjuryHandler).
- Missing: No in-field downed state and no revive.
- Evidence: `Scripts/Companions/CompanionInjuryHandler.cs`

**F07 Bond (§24): Missing**
- Current build: None.

**F08 Multiplayer readiness (party slots, events) (§25): Missing**
- Current build: A few static events only.
- Missing: No party-slot abstraction. Combat uses direct calls.

### Tools, sandbox, debug, save and tests (§26-§30)

**T01 Genesis Studio combat knobs (§26): Partial**
- Current build: Combat group: Ammo FX and Hit Catalog. Companions group: Companion Editor, Class Profiles and Loadouts & AI. Also Dash, Bindings (pointer only) and Skill Tree Lines (visual only). Enemies, creatures and weapons have separate editor windows.
- Missing: No AI, combat-core, element, Momentum, finisher, Sick Stick, skill-data, body, encounter or difficulty profiles.
- Evidence: `Editor/GenesisStudio/DMStudioRegistry.cs, EnemyPrefabCreatorWindow, DMICreatureManagerWindow, WeaponPrefabCreatorWindow`

**T02 Combat sandbox (§27): Partial**
- Current build: A TrainingDummy exists.
- Missing: No sandbox scene, spawner or debug toggles.
- Evidence: `Scripts/Combat/TrainingDummy.cs`

**T03 AI debug readout (§28): Missing**
- Current build: None.

**T04 Save/load (§29): Partial**
- Current build: Save v23 stores skills, roster, trio and injured companions.
- Missing: No enemy, faction, encounter, survivor or bond state.
- Evidence: `Save system (v23)`

**T05 Combat testing (§30): Missing**
- Current build: EditMode tests exist for Directors, GameState, Validation and WorldState only.
- Missing: No combat tests and no performance harness.
- Evidence: `Features/*/Tests`

### HUD, input and extras

**H01 Combat HUD (HUD): Partial**
- Current build: UI Toolkit HUD, ranged crosshair HUD, health HUD for the engaged enemy, enemy health bars and damage numbers.
- Missing: No Momentum bar, Sick Stick ready icon, awareness icons or finisher prompt.
- Evidence: `Assets/UI Toolkit/Runtime/DMUiToolkitHud.cs, Scripts/UI/RangedCombatHud.cs, EngagedEnemyHealthHud.cs, EnemyHealthBarPresenter.cs`

**H02 Hot Cross (quick-select) (HUD): Built**
- Current build: A gold cross with 4 quadrants. Top-left: 4 weapon slots, cycled with Tab / Y. Top-right: consumables (slots 4-9). Bottom-left: tools (binoculars, scanner). Includes an ammo-load popup. A code comment notes that element specials may share this face later.
- Evidence: `Assets/UI Toolkit/Runtime/DMUiToolkitHotCross.cs, Assets/UI Toolkit/Screens/HotCross.uxml, Scripts/UI/DMHotCrossIconRegistry.cs`

**H03 Ammo system (Weapons): Built**
- Current build: 9 ammo types across 16 ammo items. Ammo cycles on D-Pad right / X and reloads on R / Select. Ammo FX profiles.
- Evidence: `Data/Items/Ammo/*, Scripts/Combat/DMRangedAmmoStats.cs`
- Invector dependency: vShooterManager

**H04 Combat bindings (Input): Partial**
- Current build: Attack RT/LMB. Aim/Block LT/RMB. Dodge B/Q. Dash B double-tap+hold / L-Alt. SwitchWeapon Y/Tab. AmmoCycle D-Pad right/X. Reload R/Select. Binoculars LB press / B key. Scanner LB hold / N. Journal D-Pad up / J. Crouch RS/Ctrl. Sprint LS/Shift. Use X/E. Jump A/Space.
- Missing: Sick Stick, finisher, special, Overdrive, parry and the companion radial have no bindings. D-Pad up and right are already taken, which affects the plan's D-Pad companion commands.
- Evidence: `Settings/Input/InputSystem_Actions.inputactions, Scripts/Input/DMInputSchemeRouter.cs`
- Invector dependency: Invector input

**H05 Lock-on / combat focus (Extra): Built**
- Current build: CombatFocusController: 3.5 m focus range with break-lock rules. Not in the plan.
- Evidence: `Scripts/Player/CombatFocusController.cs`

**H06 Death + loot workflow (Extra): Built**
- Current build: Player death, death overlay, respawn, enemy death sequence, loot bag and loot dialog.
- Missing: Downed/revive should hook into this workflow.
- Evidence: `Scripts/Player/PlayerDeathHandler.cs, Scripts/AI/EnemyDeathSequence.cs, Scripts/Combat/EnemyLootBag.cs`
- Invector dependency: Invector death/ragdoll

**H07 Enemy + creature roster (Content): Partial**
- Current build: Humanoid EnemyDefinitions: corrupt_patrol_android, Humanoid_Enemy, The_Evil_One. Non-human: Enemy, Gongo. DMI creatures have 4 brain profiles.
- Missing: Nothing is tagged by archetype. There's no body profile.
- Evidence: `Data/Enemies/*, Data/Non Human Enemies/*`
- Invector dependency: Invector (humanoids)

## 4. Conflicts — decisions locked (Oct 3, 2026)

| ID | Decision |
|---|---|
| C15 | Keep serialised enum values. Display Cryo = Ice, Energy = Electricity. Gunpowder, Fire, Explosive, ResonanceStabilizer = damage types, not elements. |
| C15 | **Plasma default status = Burning.** Corroded reserved for later acid/bio. |
| C16 | Plasma/Cryo ammo = plasma-fuel weapons; power cells = Energy/Laser/Ion; Ion = ammo-per-hit multiplier. |
| P01 | Blade = Melee; Marksman = Pistols + Rifles; Survival = Survival + Player; Control = new. Keep assets; add move-unlock modifiers. |
| F03 | Combat role layered over class; Tank and Marksman are new roles any class can take. |
| A04 / F02 | Utility brain beside old FSMs; Humanoid_Enemy Phase 3; companions Phase 11. |
| C10 | `DamageInfo` + `IDamageReceiver` + adapter on `IDamageable`. |
| A06 | Code `EnemyArchetype` documented as rig type; behaviour archetype = new profile. |
| Invector | Wrap — body layer unchanged. |

Animation tag sheet: `DMG_Combat_Animation_Tag_Sheet.md`.

## 5. Invector dependency map and recommendation

| Layer | Player | Enemies (humanoid) | Companions |
|---|---|---|---|
| Motor / locomotion / roll | vThirdPersonController (PioneerPlayerInputBinder) | EnemyInvectorMotor bridge | Companion Invector bridges |
| Melee | vMeleeManager via PioneerShooterMeleeInput | EnemyInvectorCombat bridge | Invector melee via bridges |
| Shooter | vShooterManager, custom recoil (PioneerInvectorRecoilUtility) | EnemyInvectorLoadout / OutgoingDamage bridges | Invector shooter via bridges |
| Damage / hit reactions | PioneerInvectorSurvivalBridge, PioneerInvectorDamage path | EnemyInvectorHitSetup, OutgoingDamage bridge | Invector |
| Ragdoll / death | PioneerInvectorDeathRagdoll | EnemyInvectorRagdollBridge (also hit-stagger), EnemyInvectorDeathPresenter | Invector |

**Recommendation: wrap Invector.** Keep it as the Body (motor, animator, melee and shooter execution, ragdoll). Put the new Brain, Damage Profile, poise, status and Momentum layers above the existing bridges. Replacing Invector would mean redoing locomotion, melee, shooter and ragdoll for all three actor types before any of the new plan features could be built.

## 6. Reuse vs replace (migration table)

| Existing system | Keep / wrap / replace | Notes |
|---|---|---|
| CombatHitResolver | Keep, extend | Becomes the place that builds a DamageInfo and raises CombatEvents. |
| IDamageable | Wrap | Add IDamageReceiver(DamageInfo); IDamageable stays as the adapter. |
| CombatStatusEffectController | Keep, extend | Add stacks, immunity window, boss multiplier from a StatusProfile. |
| EnemyInvectorRagdollBridge hit-stagger | Keep | Poise-zero triggers the existing stagger. |
| EnemySenses | Keep, extend | Feed a shared perception scheduler; add awareness meter. |
| EnemyAiController FSM | Replace (gradually) | New utility brain on one enemy first; old FSM stays for the rest until migrated. |
| DMICreatureAiController | Replace later | Migrate after the humanoid brain proves out. |
| Companion controllers + CompanionCombatCoordinator | Replace later / reuse | Coordinator logic seeds the player-side token pool. |
| DMDashController + Invector Roll | Keep | Add i-frame window and perfect-dodge event. |
| SkillDefinition / hex tree | Keep, extend | Add move-unlock modifier types and branch mapping; no asset rewrite. |
| DMUiToolkitHotCross | Keep | Specials and Sick Stick can share it later. |
| SurfaceEncounterZone/Table | Keep, extend | Base for encounter definitions. |
| NavMesh leftovers (useNavMeshForChaseAndWander=false, NavMeshAgentSafeBoot) | Leave dormant | Consistent with no-NavMesh rule; don't build on them. |

## 7. Gaps by build phase (§31)

| Phase | Status | Gaps |
|---|---|---|
| 1 Audit | In progress | This document. Remaining: confirm the Invector decision (wrap), approve the conflict decisions in section 4, and tag the animation library (P02). |
| 2 Core combat | Partial | Have: hit detection, crits, stamina, 5 statuses, health/death, TrainingDummy. Need: DamageInfo + events (C10), poise (C06), status stacks/immunity (C11), resistances (C12), i-frames (C09), hitstop (C13), sandbox scene (T02). |
| 3 Unified brain | Missing | Utility scoring, archetype/personality/trait profiles, awareness meter, condition states. Migrate one enemy (suggest Humanoid_Enemy). |
| 4 Combat Director | Missing | Tokens, coordination, flanking, morale. Seed from CompanionCombatCoordinator ideas. |
| 5 Plasma sword template | Partial | Swords exist; need a Plasma element on melee, burn-on-crit (decide C15), resource use, upgrade hook, Blade/Survival branch remap. |
| 6 Momentum + finishers | Missing | Meter, HUD bar, finisher data, signal (sigil/sting/rumble), clips (only 3 finisher clips). |
| 7 Sick Stick | Missing | Weapon, consecutive-crit tracker, puke/stun (no clips), immunity via status framework, ready icon, binding. |
| 8 Specials + Overdrive | Missing | Needs Momentum and bindings. |
| 9 Remaining elements | Conflicts | Resolve names/resources first; then Cryo freeze stacks, Energy arcs, Laser limb damage, Ion disintegration, Cryo-Energy combo, mastery panel. |
| 10 Body damage | Missing | Body profile, parts, pre-split meshes on one enemy. |
| 11 Companions | Partial | Have trio, group buffs, H/G commands, injury workflow. Need roles (F03), same brain (F02), radial commands, downed/revive, bond, debuffs, party slots. |
| 12 Control/Marksman + ranged polish | Partial | Ranged is solid; Control branch is new; Marksman maps from Pistols + Rifles. |
| 13 Environment + noise escalation | Partial | Have grenades, AOE, exposure zones, 2 noise kinds. Need interactive hazards and typed noise. |
| 14 Combat memory | Missing |  |
| 15 Encounters | Partial | Have zones/tables/patrol routes. Need encounter types, factions, escalation, survivor memory. |
| 16 Studio + performance | Partial | Studio has Ammo FX / Hit Catalog / Companions / Dash only. Need all combat profiles, perception scheduler, simulation levels, perf harness. |

## 8. Recommended first implementation batch

Small, testable, and it unblocks the most later phases. Nothing here removes or replaces a working system.

1. **Close Phase 1.** Record the Invector decision (wrap) and Anthony's choices on the section 4 conflicts. Build an animation tag sheet from the counts in P02.
2. **Add `DamageInfo` + `IDamageReceiver` + a `CombatEvents` bus** (hit, crit, stagger, kill, status applied). `CombatHitResolver` builds the DamageInfo and `IDamageable` stays as an adapter. Fields: amount, damage type, element, crit, poise damage, status, source, hit point, body part (unused for now).
3. **Add a poise component** on enemies and the TrainingDummy. At zero poise it calls the existing `EnemyInvectorRagdollBridge` stagger. Values come from a Studio Combat Core profile.
4. **Extend `CombatStatusEffectController`** with max stacks, an immunity window and a boss multiplier, from a Status profile.
5. **Add dodge and dash i-frames** (0.25 s, Studio value), gated in the player damage path, plus a perfect-dodge event on CombatEvents.
6. **Build a combat sandbox scene** (copy, not an edit of an existing scene): TrainingDummy, one Humanoid_Enemy and a simple spawner. Add a Studio "Combat Core" tab (poise, i-frames, hitstop, parry window, status rules).

Test criteria: dummy staggers at poise zero; a status can't re-apply during its immunity window; a roll through a swing takes no damage; CombatEvents log every hit with its DamageInfo; 0 new console errors.

## 9. Notes
- NavMesh is dormant: `EnemyAiController.useNavMeshForChaseAndWander` defaults to false and removes the agent, and `NavMeshAgentSafeBoot.cs` and some creature scripts still reference NavMesh. This matches the no-NavMesh rule. Leave the code dormant and don't build on it.
- Current console: one unrelated error type (Unity AI generators 'NoSubscription', repeated 5 times).
- The combat docs on the box (Plan v2.1, Design Doc v2.1, Designer Manual v1.1, Player Manual v1.1) were updated to match this audit. The originals are in `/workspace/plans/backup_2026-09-30_pre-audit-sync/`, and every change is listed in `DMG_Combat_Docs_Sync_Changes_2026-09-30.md`.
