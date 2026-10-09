# Handoff: Dark Matter Genesis Combat Master Plan (status + next steps)

Written Oct 3, 2026; **execution sync Oct 8, 2026 (plan v2.4)** for a Cursor agent picking up combat work in the live project `A:\Dark Matter Genesis` (Unity 6000.4.11f1 HDRP).

## Read first

1. `AGENTS.md` and `.cursor/rules/` (they win over this file). Especially `dark-matter-genesis-core.mdc`, `unity-agent-workflow.mdc`, `dark-matter-genesis-genesis-studio.mdc`, `dark-matter-genesis-uitk-lock.mdc`, `dm-naming-no-invector.mdc`, `dark-matter-genesis-player-physics.mdc`.
2. **The plan:** `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md` — **Master Plan v2.4** (Sep 29 design; code-status sync Oct 8, 2026). Keep aligned with Desktop `C:\Users\Teabagger\Desktop\DMG_Combat_Plan_v2.md` and this handoff. Each numbered section keeps Anthony's design targets; §0 and *Current build* blocks are execution notes. Docs-only / empty Studio placeholders are not shipped.
3. **The audit:** `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Audit_Phase1.md` (read-only code audit vs the plan, Sep 30 2026). Historical snapshot. Section 4 conflicts are **locked** (see below). Do not treat Sep 30 "Missing poise/i-frames" as current disk truth.

## Shipped vs Not shipped (disk truth, Oct 8, 2026)

| Status | Systems (1-line evidence) |
|---|---|
| **Shipped** | Gate 0 attack yaw (`DMMeleeBlockThreatFacing` + `meleeAttackAutoFaceHalfAngle` 16°; regression-only); WorldChrome FPS throttle; Combat Core profile + Genesis Combat Core tab; `DamageInfo` / `CombatEvents` / `IDamageReceiver`; poise; dodge/dash i-frames; block auto-face 30°; v1.6.5 tune scene. **Phase 2:** weak chain A→B→C + random B; charge timing/speed; hitbox + swing dedupe; parry/block stagger; SparksLong; incoming block fix; Combat Studio shell. **Phase 3 (signed off Oct 8):** `DMEnemyBrain` + `DM_EnemyBrainProfile`; `DMEnemyEngagementDirector` + `DM_EnemyEngagementProfile` (D1 single melee engager + Hold); hit marks (`DMEnemyHitMarks`, `DM_EnemyHitMarkProfile`). Commits `a94d0d899`, `6646fdc33`, `e692859f1`, `edfab9a6e`. Legacy Invector melee/ranged/Hot Cross/lock-on/death+loot/companion trio/hex tree. |
| **Partial-on-disk** | Block vs parry guard-break (no dedicated parry input/counter); status stacks/immunity (`CombatStatusEffectController`); enemy reach/creep (`EnemyCombat`, `EnemyAiController.States`); **§31 #4 Combat Director** — only Phase 3 spacing layer (`DMEnemyEngagementDirector`), not §10 encounter director. |
| **Shipped (shell)** | Combat Studio Phase 2 tabs (Core / Melee / Ranged / Play v1.6.5 / Roadmap); Genesis Combat → **AI & awareness** + **Director & tokens** open brain + engagement profiles (Combat Director §10 slice still placeholder). |
| **Not started** | Player swing hitstop (`hitstopLightFrames` hooks only); **encounter Combat Director (Phase 4 next — §31 #4)**; Momentum/finishers/specials/Overdrive (§31 #6–8 — **after** #4); Sick Stick; body damage; factions; combat memory; L0–L3 sim; awareness icon UI; full Flank/Objective/Flee states. |
| **Retired** | Combat_Sandbox as the combat tune target. Leftover scene/spawner may remain — do not use. |

## Where things stand

- **Tuning scene:** `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity`. Combat_Sandbox **retired**.
- **Gate 0 (Oct 2026):** attack/charge yaw shipped — **regression-only**. Block assist unchanged (~30°).
- **FPS:** pre-combat-plan was decent with terrains + full hierarchy. ~10 FPS was UITK WorldChrome per-frame pickup/dot/bar scans — **throttled**. Terrains stay ON.
- **Phase 1 closed Oct 3, 2026.** Decisions locked; animation tag sheet added.
- **Phase 2 = §31 #2 Core combat — COMPLETE, signed off Oct 5, 2026.** Primary commit **`59368909c`** (+ branch history for Batch 1).
- **Phase 3 = §31 #3 — COMPLETE, signed off Oct 8, 2026** (Anthony / user acceptance in chat after v1.6.5 play-test). Checklists: `DM_Enemy_Spacing_And_Hit_Marks_Plan.md` §10. **`Humanoid_Enemy.asset`** may still use global Duelist default until per-enemy tuning in Phase 4+.
- **Start Phase 4 → §31 #4 Combat Director** (encounter intensity, scaled attack slots, flanking, group morale). **Do not** start §31 #6 Momentum, §31 #7 Sick Stick, or §31 #8 Overdrive until Phase 4 is signed off. Phase 3 **`DMEnemyEngagementDirector`** stays the melee spacing / D1 layer — Phase 4 adds encounter-level director beside it (see plan **Phase 4 (Oct 8, 2026)**).
- **Built through Invector (unchanged):** melee light/heavy/combos/block, ranged aim/fire with custom recoil, crits (10%, x2), stamina, dodge roll, dash (`DMDashController`), 9 ammo types / 16 ammo items, 5 status effects, Hot Cross, hex skill tree (51 stat nodes), 3-companion trio with group buffs and H/G commands, lock-on (`CombatFocusController`), death + loot, UITK combat HUD pieces.

## Phase 1 decisions (locked Oct 3, 2026)

| # | Topic | Decision |
|---|---|---|
| C15 | Element names | Keep serialized `AmmoType` values. Display Ice → Cryo, Electricity → Energy. Fire, Gunpowder, Explosive, ResonanceStabilizer are **damage types**, not elements. |
| C15 | Plasma status | **Plasma applies Burning by default.** Corroded reserved for a later acid/bio type. |
| C16 | Resource model | Plasma/Cryo ammo = plasma-fuel weapons. Power cells = Energy, Laser, Ion. Ion cost = ammo-per-hit multiplier. |
| P01 | Skill branches | Blade = Melee; Marksman = Pistols + Rifles; Survival = Survival + Player; Control = new branch. Keep hex assets; add move-unlock modifiers later. |
| F03 | Companion roles | Combat **role** layered over class. Tank and Marksman are new roles any class can take. |
| A04/F02 | One brain | Build utility brain beside old FSMs. Humanoid enemies Phase 3 **done**; companions Phase 11. |
| C10 | Damage contract | `DamageInfo` + `IDamageReceiver` beside `IDamageable` with adapter. |
| A06 | Archetype name | Code `EnemyArchetype` = **rig type** in docs; behaviour archetype = new profile later. |
| Invector | Body layer | **Wrap, don't replace.** |

Animation tag sheet: `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Animation_Tag_Sheet.md`.

**Moving melee layers (Oct 6, 2026 canon):** `DM_Melee_Locomotion_Layer_Policy.md` + library plan §10; wiki mirror `Documentation/Wiki/Combat-Moving-Melee-Layer-Policy.md`. Do not reintroduce `DM_MeleeUpper` or crossfade full melee on Base.

## Phase 2 (closed Oct 5, 2026)

Do **not** re-implement attack yaw (regression tests only). Delivered Combat Studio shell, Batch-1 + v1.6.5 acceptance, animation pass, polish (`59368909c`). Details in plan §0.

## Phase 3 (closed Oct 8, 2026)

1. Utility-scoring brain (`DMEnemyBrain`) beside `EnemyAiController` FSM — auto-attached when `enableUtilityBrain`.
2. Engagement director (D1 one melee engager, Hold ring, holder sidestep/taunt/feint) — `DMEnemyEngagementDirector`, profile rollback flags.
3. Enemy bullet hit marks Part B — profiles + Studio **Enemy hit marks** tab.
4. Acceptance: v1.6.5 play-test + spacing/hit-mark checklists (§10 in spacing plan).

**Deferred to Phase 4+:** perception upgrades, awareness icon, Flank state (until director permits), ranged token pool **use**, §10 morale/intensity at encounter level.

## Phase 4 next (§31 #4 — Combat Director)

**Canon:** plan §10 + **Phase 4 (Oct 8, 2026)** block under §0. Smallest shippable: encounter director service + Resources profile, Genesis/Combat Studio registration (`playModeSave: true`, `*.Live`), intensity-scaled attack slots, flank permission hooks, group morale events, do not break D1 when director disabled.

**Out of scope for Phase 4 start:** Momentum, Sick Stick, Overdrive (§31 #6–8).

## Full phase order (plan section 31)

1 Audit **[Done Oct 3]** → 2 Core combat **[Done Oct 5]** → 3 Unified brain + spacing + hit marks **[Done Oct 8]** → 4 Combat Director **[Next — Phase 4]** → 5 Plasma sword template **[Partial]** → 6 Momentum + finishers **[Missing]** → 7 Sick Stick **[Missing]** → 8 Specials + Overdrive **[Missing]** → 9 Remaining elements **[names locked]** → 10 Body damage **[Missing]** → 11 Companions **[Partial]** → 12 Control/Marksman + ranged polish **[Partial]** → 13 Environment + noise **[Partial]** → 14 Combat memory **[Missing]** → 15 Encounters **[Partial]** → 16 Studio + performance **[Partial; WorldChrome + Combat Studio shell shipped]**.

## Key existing code

- `Assets/_Project/Scripts/Combat/DamageInfo.cs`, `CombatEvents.cs`, `CombatDamageApplicator.cs`, `CombatHitResolver.cs`
- `Assets/_Project/Scripts/Interaction/IDamageReceiver.cs` (legacy `IDamageable` stays as adapter)
- `Assets/_Project/Scripts/Combat/CombatPoise.cs`, `DMCombatIFrameController.cs`, `DM_CombatCoreProfile.cs` + `Resources/Combat/DM_CombatCoreProfile.asset`
- `Assets/_Project/Scripts/Combat/DMMeleeBlockThreatFacing.cs` (Gate 0 attack + block face)
- `Assets/_Project/Scripts/Combat/DMEnemyGuardBreakStagger.cs` (block/parry)
- `Assets/_Project/Scripts/Combat/PioneerMeleeHitboxTuning.cs`, `PioneerMeleeSwingHitDedupe.cs`
- **Phase 3:** `Assets/_Project/Scripts/AI/Brain/DMEnemyBrain.cs`, `DMEnemyEngagementDirector.cs`; profiles `DM_EnemyBrainProfile`, `DM_EnemyEngagementProfile`; hit marks under `Scripts/Combat/` / AI as wired in plan Reaudit
- `Assets/_Project/Scripts/AI/EnemyHealthSceneRegistry.cs`
- `Assets/UI Toolkit/Runtime/DMUiToolkitWorldChrome.cs` (FPS throttle)
- `Assets/_Project/Scripts/Combat/CombatStatusEffectController.cs`
- `Assets/_Project/Scripts/Combat/TrainingDummy.cs`
- `Assets/_Project/Scripts/AI/Invector/EnemyInvectorRagdollBridge.cs` (stagger used by poise)
- `Assets/_Project/Scripts/AI/EnemySenses.cs` (keep, extend with awareness meter later)
- `Assets/_Project/Scripts/AI/EnemyAiController.cs` (replace gradually; NavMesh path stays dormant)
- `Assets/_Project/Scripts/Creatures/DMICreatureAiController.cs` (replace later)
- `Assets/_Project/Scripts/Companions/CompanionCombatCoordinator.cs` (seed for player-side token pool)
- `Assets/_Project/Features/Dash/Runtime/DMDashController.cs`
- `Assets/_Project/Scripts/Player/CombatFocusController.cs` (lock-on)
- `Assets/_Project/Scripts/Player/Invector/PioneerShooterMeleeInput.cs` (attack/block yaw, strong charge)
- Hot Cross: `DMUiToolkitHotCross` (keep; specials/Sick Stick may share it later)

## Project locks that apply to combat

- No NavMesh. Don't retune `Player_v7` capsule/layers/physics; wire both the `Player_v7 Variant` prefab and the hierarchy `Player_v7`.
- All new UI is UI Toolkit only. New editor windows/inspectors use the Frontier theme (`Assets/_Project/Editor/GenesisTheme`).
- Every combat number is a Genesis Studio profile value, never hardcoded.
- Real game start is 0 UEA, level 1, empty inventory (200 UEA / level 5 / starter items are test values). Kade lands with 2 companions; more come only from restoring Ethers/Echoes. Never mention wallets or crypto.
- Bindings: D-Pad up (Journal) and D-Pad right (ammo cycle) are already taken; Sick Stick, finisher, special, Overdrive, parry and the companion radial have no bindings yet. Ask before assigning.
- Tune combat in **v1.6.5**. Do not permanently disable Gaia terrain, streaming, or HDRP world systems for FPS.

## Workflow rules

- Editor Auto Refresh is off. After Unity edits, agent runs MCP `refresh_unity` (`if_dirty`, `compile: request` when scripts change, `wait_for_ready: true`). Ask Anthony for Ctrl+R only if MCP fails.
- No commit while the console has errors. Stage exact paths only: no `git add -A`, stash, checkout, restore or reset, and never force-push.
- No silent git/depot restore. Known console noise to ignore: Unity AI "NoSubscription", Account API / XR package-list timeouts, the Input Manager deprecation notice, PhysX "cleaning the mesh failed".
- After each batch, update the plan's *Current build* blocks, §0 shipped table, Reaudit table, and this handoff so the docs keep matching the code.
