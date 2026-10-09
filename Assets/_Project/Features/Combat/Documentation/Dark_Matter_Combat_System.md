# Dark Matter Combat System

**Dark Matter: Genesis** — authoritative **Features-layer** map for player melee/ranged combat, enemy engagement, projectiles, hit feedback, surface damage experiments, and studio tuning.

> **Design targets and phased roadmap** live in the master plan — do not duplicate that document here. Use this file for **where code lives**, **how hits flow**, and **what is shipped vs planned**.

| Authority | Path |
|-----------|------|
| GDD 5.0 (product locks) | `Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt` |
| Master combat plan (v2.4) | [DMG_Combat_Plan_v2.md](../../../Documentation/Design/Combat/DMG_Combat_Plan_v2.md) — §0 + **Reaudit (Oct 8, 2026 — v2.4)** for phase status; **Phase 4** gate under §0 |
| Agent pickup / phase order | [combat_master_plan_handoff.md](../../../Documentation/Architecture/CursorPlans/combat_master_plan_handoff.md) |
| Disk truth (whole project) | [World_Engine_Disk_Status.md](../../../Documentation/Architecture/World_Engine_Disk_Status.md) |
| Engineering contract | [Dark_Matter_Framework_Engineering_Standard.md](../../Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md) |
| Weapon stat model | [Combat_Weapon_Base_Stats.md](../../../Documentation/Architecture/Combat_Weapon_Base_Stats.md) |

**WoOS layer:** Combat sits primarily in **Experience** (player/enemy moment-to-moment) and **Simulation** (damage, status, AI state). It reads progression/inventory via existing managers today; long-term cross-feature reads should go through **Game State snapshots** when combat code is touched for new Features work.

**Disk status (Oct 2026):** This Features folder is **Documentation + module index only**. Shipped combat runtime remains under `Assets/_Project/Scripts/Combat/`, `Scripts/AI/`, and related legacy bridges. Do not mass-migrate scripts in drive-by work.

---

## 1. Purpose

Deliver **tactical light combat** on PC and Mac first (console parity later): responsive melee (combos, block, charge, poise), mouse/gamepad ranged fire with hotbar ammo, stamina-aware actions, enemy pressure with spacing tokens, and readable UITK combat chrome — without NavMesh-driven enemies or in-game language models.

Combat is **data-driven**: tunables on Resources profiles and `ItemData` weapon bases, edited in **Genesis Studio → Combat** and **Combat Studio**, applied at runtime via `*.Live` / profile reload patterns (see §5).

---

## 2. Scope

### In scope (this module describes)

| Area | Summary |
|------|---------|
| **Player melee** | Invector melee motor wrapped by Pioneer input; weak chain, strong charge, block/parry stagger hooks, auto-face (attack ~16°, block ~30°), hitbox scale + per-swing dedupe |
| **Player ranged** | Aim/fire, ammo types, hitscan and projectile paths sharing one resolver; recoil utility; Hot Cross weapon/ammo UX (UITK) |
| **Damage contract** | `DamageInfo`, `IDamageReceiver` beside legacy `IDamageable`, `CombatDamageApplicator`, poise, i-frames, status stacks (partial) |
| **Enemies** | FSM + partial utility brain (`DMEnemyBrain`), engagement director (one melee token per target), humanoid hit marks, spacing/hold ring |
| **Projectiles & FX** | `CombatProjectile` / spawner, `DMAmmoFxProfile` + catalog, pooled VFX, molten/laser burn marks where authored |
| **Hit marks & body zones** | `DMEnemyHitMarks`, `DMEnemyHitbox`, `CombatBodyPart` — decals and query merge for ranged |
| **Surface carve (non-production)** | Möller–Trumbore experiment, carve ammo lab, world `DMCarvable` impacts (enemies excluded from production carve path) |
| **Tuning & tools** | Combat Studio window, Genesis Combat category, sandbox scene builder (forensics; v1.6.5 is the tune scene) |

### Out of scope here (see §8 Non-goals)

NavMesh agents, Player_v7 physics retune, in-game LLM, full body dismemberment pipeline. **Next roadmap gate:** Combat Director morale/flanking/intensity (§31 #4 — Phase 4). Momentum/finishers/Sick Stick (§31 #6–8) remain after Phase 4 sign-off.

---

## 3. Module layout (Features vs legacy)

Per the Framework Engineering Standard, new cross-cutting combat code should eventually live here:

```
Assets/_Project/Features/Combat/
  Runtime/          ← future: Project.Features.Combat
  UI/               ← combat-specific UITK presenters (HUD mostly under Assets/UI Toolkit/)
  Data/             ← future SO profiles if migrated from Resources/
  Editor/
  Tests/
  Documentation/    ← this file
```

**Today:** treat `Scripts/Combat/` as the **legacy runtime root** (`Project.Combat` namespace). `Scripts/AI/` owns enemy controllers, brain, and engagement director. Editor tooling: `Assets/_Project/Editor/Combat/`.

Naming for **new** types: **DM** / **DMI** prefixes; do not introduce new Invector-branded identifiers in `_Project` (wrap legacy, don’t extend branding).

---

## 4. Disk status (verified paths)

### Runtime — `Assets/_Project/Scripts/Combat/`

Core resolution and damage:

- `CombatHitResolver.cs` — shared on-hit (direct, splash, status, impact VFX) for projectile and hitscan
- `CombatDamageApplicator.cs`, `DamageInfo.cs`, `CombatEvents.cs`, `CombatPoise.cs`
- `DMCombatIFrameController.cs`, `CombatStatusEffectController.cs` (partial element combos)
- `DM_CombatCoreProfile.cs` + live refresh (`DMMeleeCombatProfileLiveRefresh.cs`)

Melee / facing / guard:

- `DMMeleeBlockThreatFacing.cs`, `DMEnemyGuardBreakStagger.cs`
- `PioneerMeleeHitboxTuning.cs`, `PioneerMeleeSwingHitDedupe.cs`, `PioneerInvectorDamageReceiver.cs`

Ranged:

- `RangedFireSolver.cs`, `DMRangedAmmoStats.cs`, `CombatProjectile` family (see spawner under Player/Invector bridges)
- `HitMarks/DMCombatRangedResolver.cs`, `DMEnemyHitQuery.cs`

Hit marks & training:

- `HitMarks/` — `DMEnemyHitMarks`, `DMEnemyHitbox`, `DM_EnemyHitMarkProfile`, body-part utilities
- `TrainingDummy.cs`, `DMCombatSandboxSpawner.cs` (sandbox retired as tune target)

FX & world impact:

- `DMAmmoFxProfile.cs`, `DMAmmoFxCatalog.cs`, `CombatHitVfx.cs`, `PooledOneShotVfx.cs`
- `MoltenImpact/`, laser burn mark spawner/host interfaces
- `Experiment/Carve/` — carve experiment ammo + utilities (**lab only**)

### AI — `Assets/_Project/Scripts/AI/`

- `EnemyAiController` FSM (legacy chase/attack — **no NavMesh**)
- `Brain/DMEnemyBrain.cs` — utility Press / Defend / Retreat / Hold (**Phase 3 signed off Oct 8, 2026**)
- `Brain/DMEnemyEngagementDirector.cs` — melee token + hold ring (**Phase 3 Part A**; encounter director §31 #4 is Phase 4)
- `EnemyHealthSceneRegistry.cs` (HUD/threat perf)

### Editor — `Assets/_Project/Editor/Combat/`

- `DMCombatStudioWindow.cs` — Core / Melee / Ranged / Play / Roadmap
- `DMEnemyHitMarksBuilder.cs`, `DMMeleeAnimationSetApplier.cs`, `DMCombatSandboxSceneBuilder.cs`
- `Experiment/DMCarveExperimentAuthoring.cs`, `DMCarveStaticMeshSetupUtility.cs`

### Resources profiles — `Assets/_Project/Resources/Combat/`

| Asset | Role |
|-------|------|
| `DM_CombatCoreProfile.asset` | Stagger, parry window, hitbox scale, auto-face angles, i-frame duration, enemy reach multipliers |
| `DM_MeleeAnimationSet.asset` | Melee clip set wired via editor applier |
| `DM_EnemyHitMarkProfile.asset` | Bullet/slash decal limits, body-type VFX |
| `DM_EnemyBrainProfile.asset` | Archetype × personality × condition utility weights |
| `DM_EnemyEngagementProfile.asset` | Token hand-off, hold ring, facing/tracking |
| `DM_CombatDirectorProfile.asset` | Phase 4 start: attack slots by enemy count (1v1=1, 2–4=1–2, 5–8=2–3, larger scales), intensity, rotation; flank/morale stubs. `enableCombatDirector` off = Phase 3 only |
| `DMMoltenImpactMap.asset` | Molten surface response |
| Prefabs/VFX under same folder | Dummy target, floating damage/bar, projectiles, blood/smoke |

Ammo FX profiles: `Assets/_Project/Data/Items/Ammo/` (`DMAmmoFxProfile` assets) + **`DMAmmoFxCatalog.asset`** (Genesis **Ammo FX** subtab).

Weapon bases: `ItemData` fields documented in [Combat_Weapon_Base_Stats.md](../../../Documentation/Architecture/Combat_Weapon_Base_Stats.md).

### Phase snapshot (from plan §0, Oct 2026)

- **Phase 2 core combat:** signed off Oct 5, 2026 (v1.6.5)
- **Phase 3 utility brain + spacing + hit marks:** signed off Oct 8, 2026 (Anthony / user acceptance in chat; plan **v2.4**)
- **Phase 4 Combat Director (§31 #4):** **next** — intensity, scaled attack slots, flanking, group morale (see plan §0 **Phase 4** block)
- **Retired:** `Combat_Sandbox` as primary tune scene — use **v1.6.5**

---

## 5. Runtime flow (high level)

### Player input → action

1. **Input System** + Pioneer bridges (`PioneerShooterMeleeInput`, hotbar, dodge/dash controllers) drive Invector melee/shooter motors (**wrap, don’t replace** — Phase 1 lock).
2. **Melee swings** emit hit events from Invector hitboxes → dedupe/tuning components → damage receivers.
3. **Ranged** chooses projectile vs hitscan from ammo/weapon; muzzle follow for beams where used.

### Hit resolution → outcomes

```
Hit contact (physics ray/cast/swing overlap)
    → DMEnemyHitQuery (merge DM hitboxes + colliders) [ranged]
    → CombatHitResolver.Resolve… (damage roll, crit, splash, noise radius)
    → CombatDamageApplicator → IDamageReceiver / IDamageable adapters
    → CombatPoise / guard-break stagger / i-frame gate
    → CombatStatusEffectController (where element applies)
    → CombatEvents (listeners: UI, audio, AI)
    → FX: CombatHitVfx, DMAmmoFxProfile, DMEnemyHitMarks, world impact (DMCarvable path)
```

Stamina/tension and skill modifiers sit in progression/survival systems (`ItemData` roll helpers, skill flat bonuses) — weapon owns base damage roll per [Combat_Weapon_Base_Stats.md](../../../Documentation/Architecture/Combat_Weapon_Base_Stats.md).

**Per-ammo camera trauma (Oct 2026):** `DMAmmoFxProfile` exposes `fireTrauma` / `impactTrauma` (plus optional duration, impact radius, and min distance). Player shots route through `CombatProjectileSpawner` → `DMCombatCameraShake.TryPlayAmmoFire` and `DMCombatRangedResolver` → `TryPlayAmmoImpact`, reusing the same `CameraShake` hub as `DM_CombatCoreProfile` parry/block/charged hits. Defaults are 0 so existing ammo is unchanged until tuned in Genesis Studio → Combat → Ammo FX.

### Enemy loop (partial Phase 3)

1. Perception/hearing from existing senses (no NavMesh pathing).
2. `DMEnemyBrain` scores actions when utility brain enabled on profile.
3. `DMEnemyEngagementDirector` assigns **one melee engager** per target; others **Hold** on ring points with hand-off rules. With `DM_CombatDirectorProfile.enableCombatDirector`, extra **attack slots** (Engagers beside the token holder) scale with enemy count; non-slot enemies stay Holders and cannot swing until a slot opens.
4. Legacy FSM still runs beside brain — migration is incremental (**Humanoid_Enemy** first).

---

## 6. Tuning surfaces

| Surface | Menu / location | Profiles |
|---------|-----------------|----------|
| **Genesis Studio → Combat** | In-editor studio registry | Combat Core, Melee Animations, Ammo FX, Hit Marks, AI & awareness, Director & tokens; §31 placeholders (doc links only) |
| **Combat Studio** | `Tools/Dark Matter Genesis/Combat/Combat Studio` | Grouped view of Combat Core + melee/ranged/play roadmap |
| **Live Play edits** | Sliders in studio | `DM_CombatCoreProfile.Live`, brain/engagement/hit-mark profiles where `playModeSave` registered |

Registry reference: `Assets/_Project/Editor/GenesisStudio/DMStudioRegistry.cs` (Combat category).

**Rule:** tunable combat numbers belong on **Resources profiles**, not magic constants on prefabs — when adding fields, update Combat Studio + `DMStudioProfileSectionFilter` per genesis-studio rules.

---

## 7. Design plans (summary + links)

Use the master plan for numbered sections, phase order (§31), and acceptance criteria.

| Topic | Doc |
|-------|-----|
| Full roadmap & §0 disk table | [DMG_Combat_Plan_v2.md](../../../Documentation/Design/Combat/DMG_Combat_Plan_v2.md) |
| Phase 1 audit (historical) | [DMG_Combat_Audit_Phase1.md](../../../Documentation/Design/Combat/DMG_Combat_Audit_Phase1.md) |
| Moving melee layers | [DM_Melee_Locomotion_Layer_Policy.md](../../../Documentation/Design/Combat/DM_Melee_Locomotion_Layer_Policy.md) |
| Animation shopping (no imports) | [DM_Melee_Animation_Library_Plan.md](../../../Documentation/Design/Combat/DM_Melee_Animation_Library_Plan.md) |
| Clip/tag counts | [DMG_Combat_Animation_Tag_Sheet.md](../../../Documentation/Design/Combat/DMG_Combat_Animation_Tag_Sheet.md) |
| Spacing + hit marks Part A | [DM_Enemy_Spacing_And_Hit_Marks_Plan.md](../../../Documentation/Design/Combat/DM_Enemy_Spacing_And_Hit_Marks_Plan.md) |
| MT mesh hit & carve | [Mesh_Hit_and_Surface_Damage_Plan.md](../../../Documentation/Combat/Moller-Trumbore%20Technique/Mesh_Hit_and_Surface_Damage_Plan.md) |

**GDD locks relevant to combat:** PC/Mac first then consoles; **UEA-only** economy; **no NavMesh** for enemy pathing; **gameplay AI = authored logic only** (no in-game LLM); companion combat anims paused until UITK HUD tranche complete (see core rules).

---

## 8. Experiment & lab

| Experiment | Location | Notes |
|------------|----------|-------|
| Mesh combat compare | `_MeshCombatExperiment` in v1.6.5 | Collider vs MT mesh hit — **no production integration** until approved |
| Carve ammo lab | `DMAmmoFxProfile_CarveExperiment`, `Scripts/Combat/Experiment/Carve/` | Play Mode only; can force mesh deformation / carve-on-enemy for tests |
| World carve | `DMCarvable`, `DMCombatFx.PlayWorldImpact` | Production world impacts; **enemies excluded** from carve by design |
| Editor setup | `Editor/Combat/Experiment/` | Static mesh setup, experiment authoring |

Details: [Mesh_Hit_and_Surface_Damage_Plan.md](../../../Documentation/Combat/Moller-Trumbore%20Technique/Mesh_Hit_and_Surface_Damage_Plan.md).

---

## 9. Non-goals

- **NavMesh** baking, `NavMeshAgent`, or terrain NavMesh references for combat AI
- **Retuning `Player_v7`** capsule, layers, or physics matrix (variant wiring OK)
- **Replacing Invector** body/melee/shooter in one pass — wrap and migrate by phase
- **In-game language models** (LocalVoiceLLM, cloud conversation, Ollama in Play)
- **Combat Director** morale/intensity/flanking — **Phase 4 (§31 #4)** in progress when implementation starts; engagement spacing from Phase 3 stays. **Momentum/finishers/Sick Stick** — §31 #6–8, after Phase 4. Full **body dismemberment** — §31 #10, not started
- **Mass migration** of `Scripts/Combat/` into `Features/Combat/Runtime/` without an explicit migration task

---

## 10. Related UI & presentation

Combat HUD pieces are **UI Toolkit** under `Assets/UI Toolkit/` (`DMUiToolkitHotCross`, world chrome, engaged enemy health). Do not add new uGUI combat surfaces. Hot Cross icons: `Assets/_Project/Resources/UI/HotCrossIcons` only.

Lock-on: `CombatFocusController`. Death/loot: existing death overlay + loot bag flow (downed/revive hook planned).

---

*Last updated: Oct 8, 2026 — Phase 3 accepted (plan v2.4); Phase 4 Combat Director is the active gate.*
