# Dark Matter: Genesis — Combat, AI, Companions & Encounters
## Master Plan v2.4 (Sep 29, 2026 design; code-status sync Oct 8, 2026)

**Changelog v2.4 (Oct 8, 2026):** Phase 3 **accepted** — Anthony / user sign-off in chat after v1.6.5 play-test gate. §31 #3 marked **Done**; Reaudit + §0 updated; **Phase 4** (§31 #4 Combat Director) defined as the active next gate. Momentum / Sick Stick remain §31 #6–8, not Phase 4.

Items marked *(fill-in)* are proposals added beyond Anthony's spec. All numbers are starting values exposed in Genesis Studio, never hardcoded.

Status markers come from the Phase 1 code audit (Sep 30, 2026; `DMG_Combat_Audit_Phase1.md`) plus the Oct 2026 disk-truth pass: **[Built]** / **Shipped** works in Play, **[Partial]** / **Partial-on-disk** code exists but is not the finished design, **[Missing]** / **Not started** has no runtime, **[Conflicts]** the code does it differently (Phase 1 decisions locked Oct 3). Each numbered section keeps Anthony's design targets; *Current build* and §0 are execution notes only. Docs-only text and empty Studio placeholders are **not** shipped.

Canonical copies (keep aligned): this file, Desktop `C:\Users\Teabagger\Desktop\DMG_Combat_Plan_v2.md`, and `combat_master_plan_handoff.md` (repo + Desktop).

---

## Reaudit (Oct 8, 2026 — v2.4 acceptance pass)

Disk pass against `Scripts/Combat/`, `Scripts/AI/`, Genesis Combat subtabs, and recent git on combat paths. **Economy:** combat docs use no currency naming; enemy loot fields remain **`acDropMin` / `acDropMax`** on `EnemyDefinition` (internal keys) — player-facing grants are **UEA Credits (UEA)** per GDD 5.0.

| §31 step | Status (Oct 8) | Evidence |
|---|---|---|
| 1 Audit | **DONE** (Oct 3) | `DMG_Combat_Audit_Phase1.md`, §4 locked |
| 2 Core combat | **DONE** (signed off Oct 5) | `59368909c` + v1.6.5 acceptance |
| 3 Unified brain + spacing Part A + hit marks Part B | **DONE** (signed off Oct 8, 2026) | `a94d0d899` (brain + engagement director); hit marks `6646fdc33`, `e692859f1`, `edfab9a6e`; profiles + Studio tabs (`DM_EnemyBrainProfile`, `DM_EnemyEngagementProfile`, **Enemy hit marks**). **Acceptance:** Anthony / user sign-off in chat after v1.6.5 play-test (spacing + hit-mark checklists — `DM_Enemy_Spacing_And_Hit_Marks_Plan.md` §10). `Humanoid_Enemy.asset` may still use global Duelist default until per-enemy tuning in Phase 4+. |
| Post–Phase 3 polish | **Shipped (incremental)** | `beb9b9dd8` enemy ranged/HUD fixes; `c53d6bd49` status VFX attach; `09f6f9e1d` combo/camera/sandbox respawn; per-ammo **`fireTrauma` / `impactTrauma`** (`DMAmmoFxProfile`, `DMCombatCameraShake`) |
| 4 Combat Director (morale, flanking, intensity) | **IN PROGRESS — start on disk (Oct 8, 2026, not play-tested):** `DM_CombatDirectorProfile` + attack slots by enemy count on `DMEnemyEngagementDirector`; flank/morale stubs only | `DMEnemyEngagementDirector` = §31 #3 spacing (D1 one melee engager + Hold ring). **≠** §10 encounter director — **Phase 4 next** |
| 5 Plasma sword template | **PARTIAL** | Invector sword pipeline + Phase 2 polish; §31 #5 skill branches not started |
| 6–8 Momentum / Sick Stick / specials | **NOT STARTED** | Placeholder Studio panels only — **after** §31 #4 unless owner reprioritizes |
| Carve / MT experiment | **DEFERRED (lab)** | `Mesh_Hit_and_Surface_Damage_Plan.md`, `Experiment/Carve/` — not production combat |

**Next phase to start (one gate):** **Phase 4 — Combat Director (§31 #4)** — encounter-level intensity, scaled attack slots, flanking coordination, group morale, reinforcement/retreat hooks. Details under **Phase 4 (Oct 8, 2026)** below §0. **Do not** start §31 #6 Momentum, §31 #7 Sick Stick, or §31 #8 Overdrive until §31 #4 is signed off (§31 order).

---

## 0. Current build status (disk truth, Oct 8, 2026)

**Tuning scene:** `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity`. **Combat_Sandbox is RETIRED** for melee/combat tuning (scene + `DMCombatSandboxSpawner` may still exist as a leftover; do not treat it as the play target).

**Studio → runtime authority:** Combat Studio + Genesis **Combat Core** fields are **Resources profile → `DM_CombatCoreProfile.Live` at runtime and in builds** (not editor-only applier steps). Exception: **Melee Animations** / **Build And Apply** wires **clip assets** on animator YAML; tunable numbers (stagger, charge, hitbox scale, parry window, strong anim speed, etc.) must apply immediately in Play when sliders move. See `Unity_Compile_Iteration_Audit.md` §6.

**Phase 1 (Audit) closed Oct 3, 2026.** Locked: Plasma default status = **Burning**; Ice→Cryo / Electricity→Energy display mapping; Fire/Gunpowder/Explosive/ResonanceStabilizer are damage types; wrap Invector (don't replace); `DamageInfo` + `IDamageReceiver` beside `IDamageable`; skill-branch and companion-role mappings. Animation tag sheet: `DMG_Combat_Animation_Tag_Sheet.md`.

**Gate 0 (attack yaw) — regression-only.** Attack/charge auto-face uses `meleeAttackAutoFaceHalfAngle` (~16°) via `DMMeleeBlockThreatFacing.ApplyAttackFacing`; block stays ~30°. Shipped separately from Phase 2; do not re-prove except on regression.

**Phase 2 = §31 #2 Core combat — SIGNED OFF Oct 5, 2026** (Anthony acceptance in v1.6.5). Shipped: Batch 1 + melee polish — weak chain WeakAttack **A→B→C** + `SwordRandomAttack.B`; strong **charge timing/speed** (`SwordCharge` hold → Strong `SwordAttack.B`); **parry/block stagger** (`DMEnemyGuardBreakStagger`); **SparksLong** parry VFX; **incoming block fix**; **Combat Studio shell** (Core / Melee / Ranged / Play / Roadmap + Genesis placeholders). Primary code commit: `59368909c` (melee polish); earlier Batch 1 / hitbox / charge wiring in branch history (`4524a4069`, `5dcce7743`, etc.).

**Phase 3 = §31 #3 — SIGNED OFF Oct 8, 2026** (Anthony / user acceptance in chat after v1.6.5 play-test). Shipped on disk: utility brain + spacing Part A (`a94d0d899`); hit marks Part B (`6646fdc33`, `e692859f1`, `edfab9a6e`). Checklists: `DM_Enemy_Spacing_And_Hit_Marks_Plan.md` §10.

**Next = Phase 4 — Combat Director (§31 #4)** — see **Phase 4 (Oct 8, 2026)** below. **Do not** start §31 #6–8 (Momentum, Sick Stick, Overdrive) until Phase 4 is signed off.

**Phase 3 shipped (reference):**
- **Brain:** `Scripts/AI/Brain/DMEnemyBrain.cs` (auto-attached to every `EnemyAiController`; no prefab edits) scores Press / Defend / Retreat / Hold = archetype base × personality × condition + seeded randomness. Data: `DM_EnemyBrainProfile` (Resources/Combat; 7 archetype rows, 13 personalities, condition thresholds Healthy/Injured/Severely injured/Critical). Awareness (Unaware/Suspicious/Alert/Combat) is derived from the state machine (no icon yet).
- **Spacing / tokens:** `DMEnemyEngagementDirector` (static, 5 Hz) gives exactly one melee Engager per target (D1); everyone else uses the new `Hold` state on world-anchored hold points with holder sidestep / taunt / feint and the 30°-per-10 s drift cap (D5). Per-frame ring slot rotation removed; range hysteresis, attack facing cone, per-swing tracking windows, hand-off grace and off-screen wind-up added. Tuning: `DM_EnemyEngagementProfile` (Resources/Combat).
- **Studio:** Combat → "AI & awareness" and "Director & tokens" now open the two profiles (were placeholders).
- **Rollback:** `enableEngagementDirector` / `enableUtilityBrain` profile switches. **Save:** no bump (nothing persists); v25 stays reserved.
- **Deferred:** perception upgrades (vertical FOV, darkness, crouch, camouflage), awareness icon, AI debug readout UI, L0–L3 simulation, Flank/Objective/Flee states, remaining archetype rows, per-enemy archetype from `EnemyDefinition`, companions/creatures on the same brain, ranged token pool use, taunt clips, role-aware dodge, Engager cooldown strafe, §31 #4 Director (morale, intensity, flanking).

**Melee animation library (doc, Oct 5–6, 2026):** Some clips are **skill-gated** (base vs unlock tiers); **special attacks** use a **dedicated hold** binding (not light tap, not charge-only on attack). Catalog ↔ skill id plan, Input System **TBD** slot (`Combat/SpecialHold` proposed — **ask-before-assign**), and Jetpack Special sub-SM / AttackID branch notes → `DM_Melee_Animation_Library_Plan.md` **§9**. **Moving melee layer canon** (Base / UpperBody / FullBody, hybrid moving heavies, deprecated `DM_MeleeUpper`, charged release A/B/C) → library plan **§10** and `DM_Melee_Locomotion_Layer_Policy.md`; wiki mirror `Documentation/Wiki/Combat-Moving-Melee-Layer-Policy.md` (sync to [GitHub wiki](https://github.com/LittleWormOnAHook/Dark-Matter/wiki/Combat-Moving-Melee-Layer-Policy)). **No** skill-tree wiring or new bindings yet.

**Batch 1 on disk:** `DamageInfo`, `CombatEvents`, `CombatPoise`, i-frames (`DMCombatIFrameController`), parry/block guard-break (`DMEnemyGuardBreakStagger`), `DM_CombatCoreProfile` + Genesis Combat Core subtab, hitbox tuning + swing dedupe (`PioneerMeleeHitboxTuning`, `PioneerMeleeSwingHitDedupe`).

**Performance (Oct 2026):** Pre-combat-plan Play in v1.6.5 was decent **with terrains + full hierarchy**. ~10 FPS was a UITK `DMUiToolkitWorldChrome` per-frame pickup/dot/bar scan — **FIXED by throttle** (`ExclusivePickupScanInterval` 0.2s, `ShouldRepaintDots()` wired, `SceneComponentCache` 0.2s refresh; nearest-threat via `EnemyHealthSceneRegistry`). Terrains stay **ON**. Test-mode toggles that disable world systems are temporary isolation only; all game functions return.

Sep 30 audit snapshot (historical): 68 systems — 8 Built, 28 Partial, 25 Missing, 7 Conflicts. Built through Invector then: melee (light/heavy/combos/block), ranged aim/fire, crits (10%, x2), stamina, dodge roll, dash, 9 ammo types, 5 statuses, Hot Cross, hex skill tree, 3-companion trio, death/loot.

### Phase 4 (Oct 8, 2026) — start here

**Title:** Combat Director — encounter intensity, scaled attack slots, flanking, group morale (§31 #4)

**Canon:** Design targets in **§10 Combat Director**; implementation order **§31 #4**. Phase 3 **`DMEnemyEngagementDirector`** stays the **melee spacing / D1 single-engager** layer — Phase 4 adds an **encounter-level director** (intensity, coordination, flank permission, morale) that **feeds** brain scores and token limits; do not replace D1 with a free-for-all.

**Why now:** Phase 3 signed off Oct 8, 2026. Large fights still read as “one attacker + orbiters” without §10 intensity scaling, flank states, or morale-driven retreat.

**Goals**
- **Attack slots / intensity:** Scale simultaneous aggressors by enemy count (Studio): 1v1 = 1; 2–4 enemies = 1–2; 5–8 = 2–3; larger fights scale dynamically (§10). Non-slot enemies **circle, reposition, flank, guard, recover, prepare** — wired through utility brain + new director permission flags.
- **Flanking:** Director marks flank opportunities; enable **Flank** behaviour (§12) for eligible holders — personality-weighted (e.g. Cautious + flank bonus). No NavMesh; use existing locomotion / hold points + authored flank anchors where needed.
- **Group morale** *(fill-in)*: Shared per encounter group; leader death, ~50% casualties, or gruesome kill nearby lowers morale. Low morale → retreat / flight / surrender weights from personality (§10). Hook events only — full faction AI later.
- **Coordination:** Overcrowding relief, ranged token pool **use** (deferred in Phase 3), reinforcement / retreat / escalation **hooks** (data + events, minimal v1 behaviour).
- **Studio:** Replace Genesis **Director & tokens** placeholder slice with a live **Combat Director** profile (or extend engagement profile with a filtered §10 section) + Combat Studio section; `playModeSave: true`; runtime `*.Live` pattern.

**Deliverables (smallest shippable)**
- Encounter director service (new type beside `DMEnemyEngagementDirector`) reading a Resources profile.
- Intensity → max melee (+ optional ranged) slots applied on top of engagement hand-off.
- Morale float per group + event sinks (death, leader tag, finisher placeholder event for later Momentum phase).
- Brain integration: director **permits/denies** Press vs Flank vs Retreat for non-engagers.
- **5× `Humanoid_Enemy`** scenario in **v1.6.5** as the acceptance fight (§31 #4 test note).

**Acceptance (done-when)**
- Anthony sign-off on §31 #4 (Reaudit row + §31 checklist → **Done**).
- 5-up vs player: never more than Studio max simultaneous melee attackers; visible flank attempts from Hold ring; at least one morale-driven break (retreat/hold back) when morale crosses threshold in a scripted test (leader kill or half-down).
- Phase 3 spacing **unchanged** when director disabled via profile rollback flag.
- Unity console **zero errors**; no save version bump (v25 still reserved).

**Primary files/systems (expected):** new director + profile under `Scripts/AI/` and `Resources/Combat/`; extend `DMEnemyBrain` / `EnemyAiController` for Flank + morale listeners; Genesis `DMStudioRegistry` Director tab; `DMProjectRoadmapWindow` Phase 4 gate.

**Out of scope for Phase 4 start (§31 order):** §31 #5 plasma template polish; §31 #6 **Momentum** meter + finishers; §31 #7 **Sick Stick**; §31 #8 specials + **Overdrive**; body dismemberment; companion brain migration; carve production path.

**Do not:** retune **`Player_v7`** capsule/physics; add **NavMesh**; mass-migrate scripts to `Features/Combat/Runtime`.

### Shipped vs Not shipped (disk truth, Oct 8, 2026)

Docs-only or Studio placeholders are **Not started**. Evidence is one class/asset on disk.

| System | Status | Evidence |
|---|---|---|
| Invector wrap (body/motor/melee/shooter) | **Shipped** (legacy) | `PioneerShooterMeleeInput`, `EnemyInvector*` / `CompanionInvector*` bridges |
| Melee light / heavy / combos / block | **Shipped** (legacy) | Invector `vMeleeManager` via Pioneer input |
| Ranged aim / fire / ammo / Hot Cross | **Shipped** (legacy) | `DMUiToolkitHotCross`, 16 ammo items, `DMAmmoFxCatalog` |
| Lock-on / death+loot / companion trio | **Shipped** (legacy) | `CombatFocusController`; death overlay + loot; `PioneerRosterManager` |
| Hex skill tree (stat nodes) | **Shipped** (legacy; 5 cats ≠ plan 4 branches) | 51 `SkillDefinition` assets |
| Gate 0 attack yaw (~16°, tighter than block 30°) | **Shipped** | `DMMeleeBlockThreatFacing` + `meleeAttackAutoFaceHalfAngle` on `DM_CombatCoreProfile.asset` |
| WorldChrome FPS throttle | **Shipped** | `DMUiToolkitWorldChrome` 0.2s exclusive pickup + `ShouldRepaintDots`; `EnemyHealthSceneRegistry` |
| Combat Core profile + Genesis Combat Core tab | **Shipped** | `DM_CombatCoreProfile` / `.asset`; `DMStudioRegistry` combat-core; `CombatCoreOnly` filter |
| DamageInfo + CombatEvents + IDamageReceiver | **Shipped** | `DamageInfo.cs`, `CombatEvents.cs`, `IDamageReceiver.cs`, `CombatDamageApplicator` |
| Poise (dummy + humanoid) | **Shipped** | `CombatPoise` on `EnemyHealth` / `EnemyInvectorBootstrap` / dummy |
| Dodge / dash i-frames | **Shipped** | `DMCombatIFrameController` + applicator gate; profile 0.25s |
| Block vs parry guard-break | **Partial-on-disk** | `DMEnemyGuardBreakStagger` + `parryWindowSeconds`; no dedicated parry input / counter |
| Block auto-face (~30°) | **Shipped** | `ApplyBlockFacing` + `meleeBlockAutoFaceHalfAngle` |
| Status stacks / immunity / boss multiplier | **Partial-on-disk** | `CombatStatusEffectController` reads profile; element combos still missing |
| Melee hitbox scale + swing dedupe | **Shipped** (Phase 2) | `PioneerMeleeHitboxTuning`, `PioneerMeleeSwingHitDedupe` — accepted v1.6.5 |
| Strong melee charge + weak chain A→B→C | **Shipped** (Phase 2) | `strongMeleeChargeSeconds` + `SwordCharge` / Strong `SwordAttack.B`; lights Weak `SwordAttack` A→B→C + `SwordRandomAttack.B` |
| Parry VFX (SparksLong) + incoming block fix | **Shipped** (Phase 2) | `59368909c` polish pass |
| Enemy melee reach / creep / reposition | **Partial-on-disk** | `enemyMeleeAttackRangeMultiplier` in `EnemyCombat`; creep in `EnemyAiController.States` |
| Combat Studio (Phase 2 shell: Core/Melee/Ranged/Play/Roadmap) | **Shipped** (shell) | `DMCombatStudioWindow` tabs Core / Melee / Ranged / Play (v1.6.5) / Roadmap. Sandbox demoted to forensics. |
| Genesis placeholders (AI, Director, Momentum, Sick Stick, Body, Encounters) | **Shipped** (labels only) | `DMStudioPanelMode.CombatPlanPlaceholder` — info panels, no empty `.asset` files |
| v1.6.5 as combat tune scene | **Shipped** (scene exists) | `Dark Matter Genesis v1.6.5.unity` |
| Combat_Sandbox as tune target | **Retired** | Leftover `Combat_Sandbox.unity` / spawner — do not use |
| Player hitstop from `hitstopLightFrames` | **Not started** | Profile hooks only; no player TimeScale/hitstop consumer |
| Per-ammo camera trauma (fire / impact) | **Shipped** | `DMAmmoFxProfile.fireTrauma` / `impactTrauma` → `DMCombatCameraShake` (defaults 0 until tuned in Ammo FX) |
| Enemy bullet hit marks + DMHitbox ranged refine | **Shipped** (Phase 3, Oct 8 sign-off) | `DMEnemyHitMarks`, `DMEnemyHitQuery`, `DM_EnemyHitMarkProfile`; FRED `overrideBrain` + Android body type on definition |
| Utility AI brain (§31 #3) | **Shipped** (Phase 3, Oct 8 sign-off) | `DMEnemyBrain` + `DM_EnemyBrainProfile`; `EnemyDefinitionOverrides.ApplyBrain`; auto-attach when `enableUtilityBrain` |
| Engagement spacing (§31 #3 Part A) | **Shipped** | `DMEnemyEngagementDirector` + `DM_EnemyEngagementProfile`: 1 melee token, Hold ring (D1) |
| Combat Director / intensity / flanking / morale (§31 #4) | **Not started** | Placeholder Studio panel; **Phase 4** |
| Momentum / finishers / specials / Overdrive | **Not started** | Skill named "Momentum Strike" is a stat node only |
| Sick Stick | **Not started** | No weapon / trigger / puke |
| Body damage / dismemberment | **Not started** | Ragdoll + death dissolve only |
| Factions / combat memory / L0–L3 sim / personality | **Not started** | No matching types |
| Awareness meter / AI debug readout | **Not started** | No icon / readout |

Systems outside the numbered sections (unchanged design; still true):
- **[Partial] Combat HUD**: UI Toolkit HUD, ranged crosshair HUD, health HUD for the engaged enemy, enemy health bars and damage numbers. Gap: No Momentum bar, Sick Stick ready icon, awareness icons or finisher prompt.
- **[Built] Hot Cross (quick-select)**: A gold cross with 4 quadrants. Top-left: 4 weapon slots, cycled with Tab / Y. Top-right: consumables (slots 4-9). Bottom-left: tools (binoculars, scanner). Includes an ammo-load popup. A code comment notes that element specials may share this face later.
- **[Built] Ammo system**: 9 ammo types across 16 ammo items. Ammo cycles on D-Pad right / X and reloads on R / Select. Ammo FX profiles.
- **[Partial] Combat bindings**: Attack RT/LMB. Aim/Block LT/RMB. Dodge B/Q. Dash B double-tap+hold / L-Alt. SwitchWeapon Y/Tab. AmmoCycle D-Pad right/X. Reload R/Select. Binoculars LB press / B key. Scanner LB hold / N. Journal D-Pad up / J. Crouch RS/Ctrl. Sprint LS/Shift. Use X/E. Jump A/Space. Gap: Sick Stick, finisher, special, Overdrive, parry and the companion radial have no bindings. D-Pad up and right are already taken, which affects the plan's D-Pad companion commands.
- **[Built] Lock-on / combat focus**: CombatFocusController: 3.5 m focus range with break-lock rules. Not in the plan.
- **[Built] Death + loot workflow**: Player death, death overlay, respawn, enemy death sequence, loot bag and loot dialog. Gap: Downed/revive should hook into this workflow.
- **[Partial] Enemy + creature roster**: Humanoid EnemyDefinitions: corrupt_patrol_android, Humanoid_Enemy, The_Evil_One. Non-human: Enemy, Gongo. DMI creatures have 4 brain profiles. Gap: Nothing is tagged by archetype. There's no body profile.

---

## 1. Goal
One modular, data-driven combat and AI framework for enemies, elites, bosses, creatures, androids, companions (Pioneers), and later multiplayer party members. Combat should feel like a simulation that creates stories: the same enemy behaves differently depending on personality, equipment, objective, faction, environment, damage state, allies and the player's own habits.

Design question to optimise for: "How many different experiences can the player have from the same systems?", not "How many enemy types can we make?"

## 2. Non-negotiable rules
- Audit first. Do not delete or replace working systems during the audit.
- Every replacement documents: existing system, problem, replacement, dependencies, migration steps, rollback.
- Composition over inheritance: no EnemyBase / AndroidBase / BossBase chains. An enemy is assembled from Body + Brain + Archetype + Personality + Equipment + Abilities + Perception + Objective + Faction + Reactions.
- Smallest working version first, one enemy first, one weapon pipeline first.
- Per stage: inspect, find reusable code, explain the change, implement smallest version, compile (Anthony presses Ctrl+R), test in editor via the MCP bridge / `dm`, fix errors, document changed files, then move on.
- Backups before edits; no git stash/checkout/reset; scoped commits only with approval.
- Future-proof: new elements, weapons, statuses, body parts, archetypes and objectives are added as data, not rewrites.

*Current build (audit Sep 30, 2026):*
- **[Partial] Composition (Body + Brain + profiles)**: Enemies are assembled from components, with no inheritance chain: an EnemyDefinition asset plus EnemySenses, EnemyCombat and EnemyHealth, and Invector body bridges. Gap: The decision logic is one hard-coded state machine.

## 3. Performance principles
Avoid per-frame raycasts per NPC, constant NavMesh recalculation, excess Animator writes, allocations/GC spikes, one Update per component, and full simulation of distant NPCs.
Use event-driven systems, cached references, pooling, tick-based AI, a shared perception scheduler with a fixed per-frame budget *(fill-in: e.g. 8 perception checks per frame)*, physics layers, animation events, ScriptableObject config, seeded randomness, and LOD-style simulation.

Tiered perception *(fill-in)*: cheap overlap/distance test every few ticks, raycasts only for NPCs that pass it.

*Current build (audit Sep 30, 2026; execution Oct 4, 2026):*
- **[Partial] Performance principles**: Vision checks refresh every 0.12 s, not every frame. Gap: Every enemy runs its own Update.
- **[Shipped] UITK WorldChrome throttle (Oct 2026):** `DMUiToolkitWorldChrome` exclusive pickup / recipe / node scan is 0.2 s; `ShouldRepaintDots()` is wired; floating bars throttled. `EnemyHealthSceneRegistry` caches nearest-threat. Terrains stay on.

## 4. AI simulation levels
- **L0 Dormant**: position, objective, high-level state only. No perception.
- **L1 Background**: movement, schedule, major world events at low frequency.
- **L2 Active**: perception, tactical decisions, navigation, combat prep.
- **L3 Full Combat**: full perception, attack selection, hit reactions, coordination, memory, detailed animation.
Distances and tick rates are Studio values.

*Current build (audit Sep 30, 2026):*
- **[Missing] AI simulation levels L0-L3**: Only render/animator LOD exists (HumanoidPerformanceController). Gap: No Dormant/Background/Active/Full Combat simulation tiers.

## 5. Modular AI Brain
Brain = Archetype + Personality + Tactical Traits + Perception + Combat + Movement + Ability + Objective + Faction/Relationship + Memory profiles.

**Decision layer: utility scoring** *(fill-in)*. Every possible action gets a score:
`archetype base x personality modifiers x tactical traits x combat memory x Combat Director permission`, plus small randomness. Highest score wins. Personality becomes tunable numbers instead of new code.

**Invector decision** *(fill-in)*: the audit must decide early whether the new brain wraps Invector's AI/controllers or replaces them, since this drives animation and hit-reaction work. **Locked Oct 3, 2026:** wrap Invector as the Body (motor, melee, shooter, ragdoll, hit reactions via project bridges). Replace only the decision layer later (§31 #3). Do not rip out Invector.

*Current build (audit Sep 30, 2026; execution Oct 4, 2026):*
- **[Partial] Unified utility-scoring brain**: `DMEnemyBrain` on humanoids when profile enabled (Phase 3 signed off Oct 8, 2026). Gap: creatures and companions still on separate controllers; full shared brain is §31 #11.
- **[Shipped decision] Invector wrap**: Confirmed. Bridges stay; new Brain / DamageInfo / poise / status / Momentum layers sit above them.

## 6. Archetypes (behaviour templates, not classes)
Code note: the code's `EnemyArchetype` enum is a rig type (LegacyCreature / HumanoidInvector), not a behaviour archetype. Current behaviour presets: Custom, AggressiveHunter, Guard, PatrolInvestigator, Ambush.
- Melee: Rusher, Duelist, Berserker, Flanker, Ambusher
- Ranged: Shooter, Kiter, Sniper, Suppressor
- Defensive: Tank, Guardian, Shield, Bodyguard
- Support: Medic, Buffer, Spotter, Engineer, Summoner/Controller
- Group: Swarmer, Pack Hunter, Tactical Squad
- Special: Stalker, Assassin, Disruptor, Desperation attacker, Boss

*Current build (audit Sep 30, 2026):*
- **[Partial] Archetypes**: EnemyBehaviorPreset offers Custom, AggressiveHunter, Guard, PatrolInvestigator and Ambush. Gap: Only 4 presets exist, against 24 archetypes in the plan.

## 7. Personality
Aggressive, Cautious, Cowardly, Brave, Curious, Territorial, Opportunistic, Protective, Reckless, Calculating, Vengeful, Defensive, Predatory. Traits adjust weights, never fully override. Example: Aggressive Rusher attack +30%; Cautious Rusher attack +5%, retreat +20%, flank +15%.

*Current build (audit Sep 30, 2026):*
- **[Missing] Personality**: None. Gap: No personality weights anywhere (0 code hits).

## 8. Tactical traits
Flank, retreat, take cover, protect ally/objective, focus wounded, target healer/companion/player, disarm, destroy equipment, call reinforcements, investigate noise, search last-known position, coordinate, surround, keep distance, push, hold, escape, loot, revive. All configurable in Studio.

*Current build (audit Sep 30, 2026):*
- **[Partial] Tactical traits**: Behaviours exist for investigating noise, searching the last known position, returning home (leash), Defensive state and combat positioning. Gap: No flank, cover, protect, focus-wounded, call-reinforcements or surround traits, and nothing is configurable per trait.

## 9. Perception, awareness and noise (merged)
- **Vision**: range, horizontal/vertical FOV, obstruction, darkness, movement, crouch and camouflage modifiers.
- **Hearing**: range, threshold, direction accuracy, decay.
- **Noise events**: position, intensity, type, source, timestamp. Walking, sprinting, gunfire, explosions, heavy melee, vehicles, destruction, screams, alarms, party size.
- **Future sensors**: thermal, electrical, motion, radio, chemical, biological, psychic.
- **Awareness meter** *(fill-in)*: Unaware, Suspicious, Alert, Combat, shown as a small icon over the enemy. Knife backstab and stealth bonuses only apply while Unaware.
- **Last known position**: confirmed target, last known position, confidence, direction, movement estimate. On losing the target: investigate, search, call allies, split up, guard exits, return to objective, or retreat.

*Current build (audit Sep 30, 2026):*
- **[Partial] Vision and hearing**: Vision is 16 m with a 110° FOV and raycast obstruction. Gap: No vertical FOV, darkness, crouch or camouflage modifiers.
- **[Partial] Noise events**: EnemyNoiseEvents has two kinds, Generic and CombatImpact. Gap: No typed intensity table (walk, sprint, explosion, vehicle, party size, and so on).
- **[Missing] Awareness meter + icon**: None. Gap: No Unaware/Suspicious/Alert/Combat meter and no icon.
- **[Partial] Last known position**: Enemies store the last known position and investigate or search it. Gap: No confidence, direction or movement estimate.

## 10. Combat Director
Manages the encounter, not individual swings: intensity, attack slots, coordination, overcrowding, flank opportunities, reinforcements, retreat, escalation, encounter objectives.

**Attack tokens** (Studio values): 1v1 = 1 attacker; 2-4 enemies = 1-2; 5-8 = 2-3; large fights scale dynamically. Others circle, reposition, flank, guard, recover, prepare or protect. Companions share the player side's token pool.

**Group morale** *(fill-in)*: shared per group. Leader death, half the group down, or a gruesome kill nearby lowers it. Low morale triggers retreat, flight or surrender, weighted by personality.

*Current build (audit Sep 30, 2026):*
- **[Missing] Combat Director + attack tokens**: None for enemies. Gap: No encounter director, no attack tokens, and no shared token pool.
- **[Missing] Group morale**: None.

## 11. Combat memory (lightweight weights, not ML)
Tracks: player blocks often, favours melee/ranged, uses an element, attacks from behind, targets limbs, kills leaders first, uses companions aggressively, uses hazards. Example: frequent backstabs raise rear defence; frequent blocking brings guard-break attacks. Temporary per encounter unless designed to persist.

*Current build (audit Sep 30, 2026):*
- **[Missing] Combat memory**: None.

## 12. States (merged)
**Behaviour states**: Idle, Patrol, Investigate, Alert, Search, Approach, Engage, Flank, Defend, Retreat, Recover, Objective, Flee, Dead.
**Condition states** (separate layer): Healthy, Injured, Severely injured, Critical, Stunned, Disabled, Dismembered, Dying.
Thresholds are Studio values. States are shared, never enemy-specific.

**Desperation at Critical**: retreat, flee, berserk, call allies, special attack, protect objective, escape, take cover, turtle, chosen by personality.

*Current build (audit Sep 30, 2026):*
- **[Partial] Behaviour states**: Enemies use Idle, Wander, Patrol, Investigate, ReturnHome, Chase, Defensive, Attack and Search. Gap: Missing states: Alert, Flank, Retreat, Recover, Objective and Flee.
- **[Missing] Condition states + desperation**: Health only. Gap: No Injured/Critical/Stunned/Disabled layer and no desperation behaviour.

## 13. Objectives
Guard, protect, retrieve, transport, repair, destroy equipment, hunt, capture, investigate, loot, gather, rescue, escape, patrol, faction activity. Encounters exist without the player.

*Current build (audit Sep 30, 2026):*
- **[Partial] Objectives**: Guard and patrol presets plus SurfacePatrolRoute. Gap: No objective system (protect, retrieve, repair, hunt, loot, and so on).

## 14. Core combat rules
**Weapon interface**: damage, speed, reach, resource cost, light/heavy/combo, block, parry, finisher set, status effects, element, dismemberment profile.

**Poise and stamina** *(fill-in)*: hits drain poise; at zero the target staggers. Bosses have more poise instead of special rules. Blocking costs stamina; heavy/guard-break attacks go through blocks; unblockable attacks flash red.

**Parry and dodge** *(fill-in)*: parry window about 0.2 s; perfect parry deals heavy poise damage and opens a counter. The existing dodge roll (gamepad B press / keyboard Q, Invector roll, costs stamina) and dash (B double-tap + hold / Left Alt; 4.5 m, 0.55 s cooldown, 22 stamina, `DM_ClimbDashProfile`) get invulnerability frames (about 0.25 s). **Oct 2026:** i-frames are on disk (`DMCombatIFrameController`); dedicated parry input + counter still later.

**Damage Profile**: base damage, type, element, crit modifier, status, duration, area, dismemberment behaviour, environmental interaction, resource cost. Current contract is `IDamageable.TakeDamage(float, GameObject, bool isCritical)`; add the profile beside it with an adapter so existing callers keep working.

**Status framework** *(fill-in)*: each status has max stacks, duration, and an immunity window after it ends; bosses use a separate multiplier.

**Resistances** *(fill-in)*: per-element resistance on each body profile (e.g. androids weak to Energy and resist Cryo, creatures weak to Plasma, armoured humans weak to Laser at weak points).

**Feedback**: hitstop (2-4 frames light, 6 heavy, more on crit) *(fill-in)*, hit colours (Plasma orange, Cryo cyan, Energy violet, Laser red, Ion white-blue) *(fill-in)*, audio, VFX, UI, light camera feedback. No screen clutter.

*Current build (audit Sep 30, 2026; execution Oct 4, 2026):*
- **[Partial] Weapon interface / data**: ItemData holds melee damage, range, cooldown, stamina cost, knockback, crit chance 0.1 and multiplier x2, invectorWeaponId and grip. Gap: No light/heavy/combo definitions, parry, finisher set, dismemberment profile, or element separate from ammo.
- **[Partial] Melee light / heavy / combos / block**: Built through Invector: vShooterMeleeInput drives vMeleeManager attacks, and combos come from the Invector animator. Gap: Combo strings aren't data-driven.
- **[Shipped] Gate 0 attack yaw** (pre–Phase 2): `DMMeleeBlockThreatFacing.ApplyAttackFacing`, `meleeAttackAutoFaceHalfAngle` 16° vs block 30°.
- **[Built] Ranged aim / fire**: ADS on LT/RMB, fire on RT/LMB, hip-fire spread, custom recoil (Invector recoil suppressed), burst fire, projectiles, hitscan beams and grenades. Gap: Nothing blocking.
- **[Built] Critical hits**: Crits are a random roll per weapon (ItemData.RollCriticalHit, 10% for x2). Gap: No element crit effects and no consecutive-crit tracking (the Sick Stick needs it).
- **[Built] Stamina costs**: Invector stamina, with stamina costs for melee, roll and dash. Gap: Block stamina is left to Invector defaults.
- **[Shipped] Poise**: `CombatPoise` on dummy + humanoids; values from `DM_CombatCoreProfile`. Break calls existing stagger / ragdoll bridge.
- **[Partial] Stagger**: Poise-break stagger plus block/parry guard-break (`DMEnemyGuardBreakStagger`). Ranged ragdoll hit-stagger still exists.
- **[Partial-on-disk] Parry**: Timed window (`parryWindowSeconds` 0.2s) on held block; stronger stagger + parry hitstop on the attacker. Gap: no dedicated parry button, no player counter.
- **[Shipped] Dodge / dash i-frames**: `DMCombatIFrameController` (0.25 s profile) gated in `CombatDamageApplicator`.
- **[Shipped] DamageInfo / events**: `DamageInfo` + `IDamageReceiver` + `CombatEvents` beside `IDamageable`. Body part unused; full element/dismemberment profile still later.
- **[Partial] Status framework**: Statuses: Burning, Frozen, Shocked, Corroded, Stabilized. **Oct 2026:** max stacks, immunity window, boss multiplier live on the Combat Core profile. Gap: no element combos.
- **[Missing] Resistances**: None on enemies. Gap: No per-element resistance by body profile.
- **[Partial] Feedback (hitstop, colours, VFX, audio, camera, rumble)**: Impact VFX and audio per ammo (DMAmmoFxProfile x9, DMAmmoFxCatalog), floating damage numbers, CameraShakeService and laser burn marks. **Parry** uses local animator hitstop on the enemy. **Player swing hitstop** (`hitstopLightFrames` / `hitstopHeavyFrames`) is a profile hook only — not started.

## 15. Melee weapons
- **Knife**: fastest, short reach, backstab bonus, high crit vs unaware, quick execution, later throw/recall.
- **Sword**: balanced, combos, cleave, parry, dismemberment; the only type that can cut bodies in half.
- **Baton**: high stagger and poise damage, knockdown, interrupt, disarm, shield break. No cutting.
- **Sick Stick**: the signature control weapon (see section 18).

**Animator layer policy (Phase 2 Jetpack):** Base = locomotion only; UpperBody = moving / Hold E + LMB combos; FullBody = standing + hybrid moving charge/release; **`DM_MeleeUpper` removed.** Full policy → `DM_Melee_Locomotion_Layer_Policy.md` (library plan §10).

Ranged weapons use the same interface and damage profiles.

*Current build (audit Sep 30, 2026):*
- **[Partial] Melee weapon set**: 8 melee items: swords, axes, a spear and a two-hander. Gap: No knife, baton or Sick Stick.

## 16. Elements
- **Plasma** (plasma fuel): standard damage; crits burn with small area damage; can ignite and overheat machinery.
- **Cryo** (plasma fuel): damage; crits chill; chill stacks into a brief freeze; freeze water/surfaces, slow machinery, temporary paths.
- **Energy** (power): higher base than Plasma, chain arcs, area burst on crit, disrupts androids and electronics, conducts through water and metal.
- **Laser** (power): high damage and limb damage, cauterised glowing cuts with no blood, precision weak-point damage, no area damage; cuts cables, doors, weapons.
- **Ion** (power): very high damage, dismemberment and disintegration with blood splatter, clean cuts, no burning; leaves ash or a scorched skeleton *(fill-in)*. Costs 33% of power per major hit (Studio value, reduced by upgrades).
- **Out of resource** *(fill-in)*: any powered weapon does a weak unpowered strike.

**Code names and current build**: elements live on ammo (`AmmoType`), not on weapons. **Cryo is `Ice` and Energy is `Electricity` in code**; keep enum values and map display names. Gunpowder, Fire, Explosive and ResonanceStabilizer are damage types, not elements. **Locked Oct 3, 2026:** Plasma default status = **Burning** (Corroded → later acid/bio). Fire → Burning, Ice → Frozen, Electricity → Shocked, Laser/Ion → none. Resources today are ammo items with reload; plasma-fuel / power-cell mapping is design-locked, implementation follows C16.

**Combinations** (framework, add more as data): Cryo then Energy (frozen targets take about 1.5x energy and become chain points, the first combo to build); Plasma then Cryo (thermal shock weakens armour); Laser then Ion (exposed internals destroyed); Energy then Water (spreads through water).

*Current build (audit Sep 30, 2026):*
- **[Conflicts] Elements**: AmmoType has Gunpowder, Plasma, Ice, Electricity, ResonanceStabilizer, Laser, Ion, Fire and Explosive. Gap: Names differ: Cryo = code Ice, Energy = code Electricity.
- **[Conflicts] Element resources (plasma fuel / power, Ion 33%)**: Weapons use ammo items (16 ammo assets) with reload. Gap: No plasma-fuel or power resource, no Ion 33% cost and no unpowered strike.
- **[Missing] Element combinations**: None.

## 17. Momentum meter, finishers and special moves (NEW)
**Momentum builds from damage** *(fill-in name)*:
- Fills from damage dealt, crits, parries, perfect dodges, and combo length.
- Drains slowly out of combat; taking heavy damage knocks some off.
- Shown as a segmented bar under the health/power HUD (3 segments *(fill-in)*).

**Spending it**:
- **1 segment: Finisher.** Available on any enemy below the finisher threshold (20%, Studio value), or on a staggered, stunned, frozen or puking enemy. Prompt appears over the target.
- **2 segments: Special move.** A weapon/element signature strike (see skill tree), e.g. Plasma fire ring, Cryo frost nova, Energy chain lightning, Laser precision thrust, Ion overcharge.
- **3 segments: Overdrive** *(fill-in)*. A timed state (about 8-12 s): faster attacks, stronger element effects, no resource cost, and finishers cost nothing. Ends with a short cooldown so it can't be chained.

**Unique finisher signal** (must never be confused with any other cue):
- **Visual**: the target gets a distinct Genesis-only mark, a slow-rotating gold/white fractured "dark matter" sigil over the chest with a thin rim glow on the body outline. No other effect in the game uses that shape or colour.
- **Audio**: a short signature sting (low resonant hum plus a crystalline ping) played once when the window opens, and a soft heartbeat-style pulse while it stays open.
- **Enemy tell**: the enemy plays a unique vulnerable pose (staggered knee-buckle, head down) so it reads without UI.
- **HUD**: the Momentum segment that will be spent flashes in the same gold, and the button prompt appears inside the sigil.
- **Controller**: a distinct double-pulse rumble.
- **Kept separate** from the Sick Stick ready icon (green, HUD corner), crit flashes, element hit colours, and the red unblockable-attack flash.
- Colour, shape, sound and rumble are Studio values, with a colour-blind-safe option.

**Finisher selection**: each finisher is a data entry (weapon type, element, enemy body type, player position front/back/above/ground, required state). Best match wins, random among ties. Element decides the ending: Plasma burns, Cryo shatters, Energy fries, Laser takes limbs, Ion disintegrates, Baton knockdown/control, Sword + Laser from behind = rear execution. Synced player/enemy animations aligned to an anchor, brief invulnerability, optional close camera (toggle in settings). One finisher at a time; nearby enemies take a morale hit. Environmental finishers (hazards, walls, ledges) later.

*Current build (audit Sep 30, 2026):*
- **[Missing] Momentum meter**: None (0 code hits).
- **[Missing] Finishers + unique signal**: One finisher-like clip exists (Human_SwordOneHand_Finisher1.fbx). Gap: No finisher data, sigil, sting or rumble.
- **[Missing] Special moves + Overdrive**: None.

## 18. Sick Stick: the signature move (NEW)
The laugh-out-loud signature of Genesis combat.

**Trigger**: available at any time once conditions are met, with any weapon equipped *(fill-in: a quick swap-strike with the holstered stick)*. Conditions (Studio list, any one works):
- 2 crits in a row
- a specific combo string (e.g. light, light, heavy) *(fill-in)*
- a perfect parry *(fill-in)*
- a target already staggered *(fill-in)*
When ready, a small green icon pulses on the HUD and the next Sick Stick input fires it. Using the Sick Stick as your main weapon makes the conditions easier (e.g. 1 crit) *(fill-in)*.

**Effect**:
- Normal enemy: bends over, pukes, stunned 3-6 s (skill rank + stick upgrade).
- Large enemy: stagger. Boss: brief stagger only.
- Puking enemies are finisher-eligible; finishing a puking enemy gives bonus Momentum *(fill-in)*.
- Anti-chain: 8 s Sick Stick resistance on that target (general status immunity system).
- Nearby enemies may gag/hesitate briefly (upgrade) *(fill-in)*.
- Players are valid targets for future PvP/co-op mishaps.

**Upgrades**: stun length, easier trigger conditions, splash gag radius, cooldown, and cosmetic puke variants (colours per element) *(fill-in)*.

*Current build (audit Sep 30, 2026):*
- **[Missing] Sick Stick**: None. Gap: No weapon, trigger, stun or immunity.

## 19. Player skill tree (NEW)
Four branches, about 12-15 nodes each, top tier needs points in that branch, every node shows its numbers.
**Current build**: a live hex tree with 5 categories (Melee 6, Pistols 6, Rifles 12, Survival 9, Player 18 = 51 nodes), 5 ranks per node, branch-depth costs, saved allocations. Nodes are stat modifiers only. Proposed mapping: Blade = Melee; Marksman = Pistols + Rifles; Survival = Survival + Player (movement/defence); Control = new. Start level 5 / 25 points are testing values.
1. **Blade (melee)**: longer combos, lunge gap-closer, spin cleave, knife throw/recall, bigger parry window, counter after parry, finisher unlocks.
2. **Control (melee utility)**: baton knockdown/disarm, Sick Stick stun length and easier triggers, splash gag, poise damage, guard break, shoulder charge.
3. **Marksman (ranged)**: aim stability, reload speed, weak-point bonus, charged shot, quick-draw sidearm, piercing rounds, melee/ranged swap bonus.
4. **Survival (defence/mobility)**: dodge i-frames, cheaper dash, slow-motion after perfect dodge, stamina/power regen, block damage reduction.

**Momentum nodes** spread across branches: faster build, longer Overdrive, cheaper finishers.
**Elemental mastery** side panel: 3 nodes per element (stronger effect, cheaper cost, signature special move), unlocked by owning that element.
**Points**: 1 per level from the existing level-up system, bonus points from rare blueprints and bosses. Free respec at a Terminal/Build Hub (maybe small resource cost). Skills change moves; weapon upgrades stay separate as gear.

**Animations**: the audit lists and tags Anthony's animation library (light, heavy, combo, finisher, dodge, hit react, idle, special, puke, etc.). Skill nodes point at clips through the same animation profiles the enemies use, so clips can be swapped or edited without code. **`DM_MeleeAnimationSet`** (+ future **`DM_MeleeFinisherCatalog`**) hold clip refs; Studio assigns all clips, **runtime gates playback** by unlocked skills (see `DM_Melee_Animation_Library_Plan.md` §9). Specials: dedicated hold input — separate from charge on attack.

*Current build (audit Sep 30, 2026):*
- **[Conflicts] Skill tree**: A hex skill tree is live with 5 categories: Melee 6, Pistols 6, Rifles 12, Survival 9, Player 18 (51 nodes). Gap: The plan has 4 branches (Blade, Control, Marksman, Survival).
- **[Partial] Animation library + profiles**: About 11,156 .anim/.fbx files. Gap: The clips aren't tagged and there are no animation profiles.

## 20. Weapon upgrades
Blueprint/recipe framework. Universal Mk I to Mk V: Damage, Attack Speed, Resource Efficiency.
Element-specific: Plasma burn duration/damage/crit/area; Cryo chill duration, freeze threshold, freeze duration; Energy arc count/range, burst radius; Laser limb damage, dismember chance, precision; Ion power efficiency (33% down toward 25%), disintegration chance, damage. Costs stay placeholders.

*Current build (audit Sep 30, 2026):*
- **[Missing] Weapon upgrades Mk I-V**: A crafting, recipe and blueprint pipeline exists (CraftingManager, RecipeCreator, BlueprintCraftingManager) that upgrades could reuse. Gap: No weapon upgrade tracks.

## 21. Body components and dismemberment
Functional parts per body profile: head, torso/core, each arm, each leg, plus special parts (android sensors, shield arm, weapon arm). Losing a weapon arm switches to sidearm or melee; legs make it crawl or stationary; sensors cut accuracy; shield loss changes defence; core destroyed kills. Headshots lethal unless helmeted *(fill-in)*.

Dismemberment phase 1: pre-split meshes with stump caps, bone/part swap, detached part with physics, element VFX. No real-time slicing. One enemy first proving head, arm, leg and torso split. (Biggest art cost; stays on one enemy for a while.)

Visual damage via material properties: armour damage, burn marks, frost, electrical scorch, cauterised cuts, missing limbs, broken gear.

*Current build (audit Sep 30, 2026):*
- **[Missing] Body components + dismemberment**: Ragdoll death and a disintegration dissolve on death exist. Gap: No functional body parts and no dismemberment.
- **[Partial] Visual damage**: Laser burn marks and the death dissolve. Gap: No frost, scorch, armour damage or missing-limb visuals.

## 22. Environmental combat
Explosives, electrical gear, flammables, toxic areas, water, ice, steam, lava, doors, machinery, turrets, power systems, structural hazards. Fights can be won through the environment.

*Current build (audit Sep 30, 2026):*
- **[Partial] Environmental combat**: Grenades, explosive ammo, AOE bubbles, splash damage and exposure hazard zones. Gap: No explosive barrels, electrical or water conduction, flammables or interactive machinery.

## 23. Factions and emergent encounters
Relationships: Friendly, Allied, Neutral, Suspicious, Hostile, Fearful, Territorial. Factions fight each other; the player can join, avoid, exploit, attack both, wait, or investigate.

**Encounter definitions** from location, faction, enemy pool, objective, leader, support units, wildlife, weather, time, resources, world events, reputation, seed.
**Encounter types**: Ambush, Hunt, Defence, Crossfire, Faction Battle, Predator, Retreat, Escalation, Investigation, Empty (evidence only). Not every POI is a fight.
**Escalation** depends on noise, location, faction activity, world state, chance and nearby entities; never mandatory.
**Survivor memory** *(fill-in)*: escaped enemies go into a small "known survivors" save list (faction, event note, scar) and can reappear.

*Current build (audit Sep 30, 2026):*
- **[Missing] Factions + relationships**: SurfaceThreatKind (Any, Alien, Lifeform, Android) is a spawn filter only. Gap: No factions (0 code hits).
- **[Partial] Encounters**: SurfaceEncounterZone, a weighted SurfaceEncounterTable and SurfacePatrolRoute. Gap: None of the plan's encounter types (Ambush, Hunt, Crossfire and so on).
- **[Missing] Survivor memory**: None.

## 24. Companions: 1 to 3, same brain (NEW)
- Companions use the same brain as enemies, set friendly, with a role: Tactician, Infiltrator, Medic, Tank, Engineer, Marksman. Personality, perception, memory and relationships come for free.
- **Current build**: companions run their own controllers (`CompanionFollowController`, `CompanionCombatController`, `CompanionCombatCoordinator` turn scheduling) and use 9 pioneer classes. Proposed role mapping: Tactician = CombatTactician, Infiltrator = InfiltratorScout, Medic = MedTech, Engineer = ArchitectEngineer / SalvageEngineer; Tank and Marksman are new roles. Existing group buffs: radiation resistance, expedition efficiency, combat synergy, move speed, debuff resistance.
- Share the player side's attack tokens.
- **Party buffs** by role *(fill-in examples)*: Tactician +10% party damage and weak-point marking; Medic health regen and revives; Tank +15% poise and draws attention; Infiltrator +20% backstab/crit and quieter movement; Engineer +15% power/plasma regen and a turret; Marksman long-range highlights and weak-point damage. Same buff doesn't stack twice.
- **Party debuffs**: each companion adds noise; clashing personalities (e.g. Reckless + Cautious) cost a little effectiveness until bond rises; companions draw some shared ammo/plasma.
- **Commands** on a radial / D-Pad: follow, hold, attack my target, fall back, use ability. One unique signature action each (later). Current build: keyboard H = hold, G = follow only; D-Pad up (Journal) and right (Ammo cycle) are already bound.
- **Downed, not dead**: revivable by player or Medic; dies only if left down too long; ties into the existing death workflow. Current build: a fallen companion is sent to the Science Lab as injured (`CompanionInjuryHandler`).
- **Bond level**: rises by fighting together; unlocks stronger buffs and combo moves (e.g. Tank staggers, player finishes). Companions can build Momentum toward team finishers *(fill-in, later)*.

*Current build (audit Sep 30, 2026):*
- **[Built] Companion trio (1-3)**: Up to 3 expedition companions (expeditionTrioIds in the save), managed by PioneerRosterManager and CompanionRosterBridge.
- **[Conflicts] Companions use the same brain**: Companions have their own brain: CompanionFollowController, CompanionCombatController, CompanionSenseController, CompanionThreatSensor and CompanionCombatCoordinator. Gap: The plan wants companions on the enemy brain, set friendly.
- **[Conflicts] Roles + party buffs**: 9 SkilledPioneerClass values: ArchitectEngineer, ScienceSpecialist, CombatTactician, InfiltratorScout, IoHybrid, MedTech, LogisticsOfficer, SalvageEngineer, CommunicationsOfficer. Gap: The plan has 6 roles (Tactician, Infiltrator, Medic, Tank, Engineer, Marksman) with different buffs.
- **[Missing] Party debuffs**: None.
- **[Partial] Companion commands**: Keyboard H holds and G follows (hard-coded keys, not in the action map). Gap: No radial or D-Pad, and no attack-my-target, fall-back or use-ability commands.
- **[Missing] Downed / revive**: A fallen companion is sent to the Science Lab as injured (CompanionInjuryHandler). Gap: No in-field downed state and no revive.
- **[Missing] Bond**: None.

## 25. Multiplayer DLC readiness (up to 3 party members)
- A party is a list of slots; each slot holds a companion or a human player.
- All combat goes through data profiles and events, never direct calls, so it can be networked later.
- Sick Stick, stuns and finishers already work on players.
- No networking now, only the slot-based party design.

*Current build (audit Sep 30, 2026):*
- **[Missing] Multiplayer readiness (party slots, events)**: A few static events only. Gap: No party-slot abstraction.

## 26. Genesis Studio
Grows every phase. Profiles for: AI (archetype, personality, traits, perception, aggro, leash, memory), combat (damage, speed, reach, poise, attack priority, tokens), elements (status, duration, cost, crit effect, upgrade scaling, combos), Momentum/finishers/specials/Overdrive, Sick Stick triggers, skill tree, body (components, weak points, dismemberment, functional damage), companions (roles, buffs, debuffs, bond), encounters (composition, objectives, reinforcements, escalation, environment), difficulty *(fill-in: scales tokens, detection speed, reaction time and poise damage rather than health)*.

*Current build (audit Sep 30, 2026; execution Oct 4, 2026):*
- **[Partial] Genesis Studio combat knobs**: Live tabs remain Combat Core, Melee Animations, Combat Studio, Ammo FX, Hit Catalog, Surface Damage (+ Carve Tool). Phase 2 shell **shipped**: Combat Studio is Core / Melee / Ranged / Play (opens v1.6.5) / Roadmap. Genesis Combat now has info-only placeholders (AI & awareness, Director & tokens, Momentum & finishers, Sick Stick & specials, Body & dismemberment, Encounters & difficulty) via `CombatPlanPlaceholder` — no empty profile assets. Runtime profiles for those phases still missing.

## 27. Combat sandbox (built in Phase 2)
Start from the existing `TrainingDummy` (+ DummyCombatUI).
Spawn any enemy, weapon, element, archetype, personality, status, body configuration, companion, encounter. Debug: god mode, infinite stamina/power/plasma/Momentum, force stagger/dismember/status/Sick Stick, spawn/reset encounter. Visualise vision cones, hearing radius, aggro, leash, target, last known position, token, state, objective, personality modifiers, utility scores.

*Current build (audit Sep 30, 2026; execution Oct 4, 2026):*
- **[Retired as tune target] Combat_Sandbox**: Leftover scene + `DMCombatSandboxSpawner` / TrainingDummy exist. **Do not use for Phase 2 melee tuning.** Play and accept in **Dark Matter Genesis v1.6.5**. Full sandbox (spawn any archetype/personality, god mode, token viz) remains a later Phase 2/16 goal, not the daily tune scene.

## 28. Debugging
Readable reports, e.g. "AI Brain 042, State: Search, Target: Player, Last Known: X/Y/Z, Reason: lost behind obstruction, Next: Investigate, Confidence: 61%". Hold a dev key and look at an enemy to see it *(fill-in)*. Stripped from release builds.

*Current build (audit Sep 30, 2026):*
- **[Missing] AI debug readout**: None.

## 29. Save/load
Save what persists: enemy state where needed, objectives, faction state, world encounter state, persistent damage, survivor list, skill tree, bond levels, companion roster. Temporary combat memory and Momentum don't persist. Bump the save version and keep current saves loading.

**Save version:** this plan takes **v25**, because v24 is reserved for loot chests (see `Design/Loot/DM_Loot_Chest_System_Plan.md`).

*Current build (audit Sep 30, 2026):*
- **[Partial] Save/load**: Save v23 stores skills, roster, trio and injured companions. Gap: No enemy, faction, encounter, survivor or bond state.

## 30. Testing
Each phase: hit detection, damage, stagger, death, finishers, dismemberment, target acquisition/loss, search, flank, retreat, objectives. Performance at 1, 10, 25, 50 enemies and 100 simulated entities; measure CPU, GPU, GC, frame time, AI tick cost.

*Current build (audit Sep 30, 2026):*
- **[Missing] Combat testing**: EditMode tests exist for Directors, GameState, Validation and WorldState only. Gap: No combat tests and no performance harness.

## 31. Implementation order
1. **Audit** **[Done Oct 3, 2026]**: architecture and dependency maps; enemy, weapon, damage and animation library inventory (tagged); reuse vs replace candidates; Invector decision (**wrap**); migration table; animation tag sheet.
2. **Core combat** **[Done — Phase 2 signed off Oct 5, 2026]**: damage profiles, weapon and element interfaces, hit detection, poise/stamina, status framework with immunity, health/damage events. **Tune in v1.6.5; Combat_Sandbox retired.** Batch 1 + melee polish shipped (`59368909c` + branch history). Gate 0 attack yaw regression-only. Combat Studio + Genesis placeholder shell shipped. Jetpack melee: lights → Weak `SwordAttack` A→B→C + `SwordRandomAttack.B`; charge → `SwordCharge` / Strong `SwordAttack.B`; parry/block stagger, SparksLong, incoming block fix.
3. **Unified brain** **[Done — Phase 3 signed off Oct 8, 2026]** utility scoring, archetype, personality, condition layer, Hold via engagement director, hit marks Part B. Commits `a94d0d899` + hit-mark chain; acceptance in v1.6.5 per Reaudit.
4. **Combat Director** **[Next — Phase 4]**: encounter intensity, scaled attack slots, coordination, flanking, group morale, reinforcement/retreat hooks. **Build on** `DMEnemyEngagementDirector` (D1); test with 5 identical enemies. See **Phase 4 (Oct 8, 2026)** under §0.
5. **Plasma sword template** **[Partial]**: attack, damage, hit react, crit, burn, resource use, upgrade. Start Blade and Survival skill branches.
6. **Momentum meter and finishers** **[Missing]**: build-up, finisher selection, first finishers on the migrated enemy. Skill-gated finisher rows on `DM_MeleeFinisherCatalog` (library plan §9).
7. **Sick Stick signature** **[Missing]**: trigger conditions, puke/stun, boss stagger, immunity, upgrades, VFX/audio.
8. **Special moves and Overdrive** **[Missing]**. Dedicated **special hold** binding (TBD, user approval) + Jetpack AttackID or Special sub-SM (library plan §9.4).
9. **Remaining elements** **[Conflicts, names locked Oct 3]**: Cryo, Energy, Laser, Ion, then the Cryo-to-Energy combo. Elemental mastery panel. Display mapping locked; plasma-fuel / power-cell implementation still later.
10. **Body damage and dismemberment** **[Missing]** on one enemy, element-specific finishers.
11. **Companions** **[Partial]**: roles, buffs/debuffs, commands, downed/revive, bond. Slot-based party.
12. **Remaining skill branches** **[Partial]** (Control, Marksman) and ranged polish.
13. **Environment and noise escalation** **[Partial]**.
14. **Combat memory** **[Missing]**.
15. **Encounters** **[Partial]**: 15a authored definitions with random picks, 15b faction battles, 15c noise escalation chains, 15d world events, survivor memory.
16. **Studio completion and performance pass** **[Partial]**: WorldChrome throttle shipped; Combat Studio Phase 2 shell shipped (Core/Melee/Ranged/Play/Roadmap + Genesis placeholders). Remaining Studio profiles (AI, Director, Momentum, etc.) still wait for their phases.

## 32. Success criteria
- New enemy mostly created through Studio profiles.
- Same model, different behaviour.
- Enemies don't all attack at once; they flank, retreat, search, defend and coordinate, and react to player habits.
- Body components matter; elements are distinct and interact with the environment.
- Momentum, finishers, specials and the Sick Stick feel rewarding and readable.
- Skill tree creates distinct builds.
- Companions use the same brain and change fights through buffs, debuffs and commands.
- Party design is ready for 3-player multiplayer.
- Encounters emerge rather than being scripted; performance holds in large fights.

## 33. Final target
Three scavengers guard a ruin. One retreats; the player follows. It calls its faction. Gunfire draws wildlife, which attacks both sides. The player freezes a creature, then chains Energy through it. Two crits in a row: the Sick Stick flashes ready, and the scavenger leader is puking in the dirt. Momentum is full; Overdrive. A Laser strike takes the leader's weapon arm and he switches to melee, then falls to a finisher. Morale breaks, the rest retreat, one escapes, and later that survivor's faction shows up somewhere else.

Combat that creates stories, not damage numbers.
