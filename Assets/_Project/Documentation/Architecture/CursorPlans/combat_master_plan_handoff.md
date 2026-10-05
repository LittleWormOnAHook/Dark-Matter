# Handoff: Dark Matter Genesis Combat Master Plan (status + next steps)

Written Oct 3, 2026; **execution sync Oct 4, 2026** for a Cursor agent picking up combat work in the live project `A:\Dark Matter Genesis` (Unity 6000.4.11f1 HDRP).

## Read first

1. `AGENTS.md` and `.cursor/rules/` (they win over this file). Especially `dark-matter-genesis-core.mdc`, `unity-agent-workflow.mdc`, `dark-matter-genesis-genesis-studio.mdc`, `dark-matter-genesis-uitk-lock.mdc`, `dm-naming-no-invector.mdc`, `dark-matter-genesis-player-physics.mdc`.
2. **The plan:** `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md` — **Master Plan v2.2** (Sep 29 design; code-status sync Oct 4, 2026). Keep aligned with Desktop `C:\Users\Teabagger\Desktop\DMG_Combat_Plan_v2.md`. Each numbered section keeps Anthony's design targets; §0 and *Current build* blocks are execution notes. Docs-only / empty Studio placeholders are not shipped.
3. **The audit:** `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Audit_Phase1.md` (read-only code audit vs the plan, Sep 30 2026). Historical snapshot. Section 4 conflicts are **locked** (see below). Do not treat Sep 30 "Missing poise/i-frames" as current disk truth.

## Shipped vs Not shipped (disk truth, Oct 4, 2026)

| Status | Systems (1-line evidence) |
|---|---|
| **Shipped** | Gate 0 attack yaw (`DMMeleeBlockThreatFacing` + `meleeAttackAutoFaceHalfAngle` 16°); WorldChrome FPS throttle (`DMUiToolkitWorldChrome` 0.2s + `ShouldRepaintDots`); Combat Core profile + Genesis Combat Core tab (`DM_CombatCoreProfile`); `DamageInfo` / `CombatEvents` / `IDamageReceiver`; poise (`CombatPoise`); dodge/dash i-frames (`DMCombatIFrameController`); block auto-face 30°; v1.6.5 tune scene. Legacy Invector melee/ranged/Hot Cross/lock-on/death+loot/companion trio/hex tree. |
| **Partial-on-disk** | Block vs parry guard-break (`DMEnemyGuardBreakStagger` — no dedicated parry input/counter); status stacks/immunity (`CombatStatusEffectController`); hitbox scale + swing dedupe; strong charge + AttackC on Jetpack `SwordCharge` / Strong `SwordAttack.B` (lights → Weak `SwordAttack` A→B→C + `SwordRandomAttack.B`; Play acceptance open); enemy reach/creep (`EnemyCombat`, `EnemyAiController.States`). |
| **Shipped (shell)** | Combat Studio Phase 2 tabs (Core / Melee / Ranged / Play v1.6.5 / Roadmap); Genesis `CombatPlanPlaceholder` labels (AI, Director, Momentum, Sick Stick, Body, Encounters) — no empty assets. |
| **Not started** | Player swing hitstop (`hitstopLightFrames` hooks only); utility brain; Combat Director/tokens; Momentum/finishers/specials/Overdrive; Sick Stick; body damage; factions; combat memory; L0–L3 sim; personality; awareness meter. |
| **Retired** | Combat_Sandbox as the combat tune target. Leftover scene/spawner may remain — do not use. |

## Where things stand

- **Tuning scene:** `Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity`. Combat_Sandbox **retired**.
- **Gate 0 (Oct 2026):** attack/charge yaw shipped, **not mixed into Phase 2**. Block assist unchanged (~30°).
- **FPS:** pre-combat-plan was decent with terrains + full hierarchy. ~10 FPS was UITK WorldChrome per-frame pickup/dot/bar scans — **throttled**. Terrains stay ON. Test-mode toggles are temporary; all game functions return.
- **Phase 1 closed Oct 3, 2026.** Decisions locked; animation tag sheet added.
- **Phase 2 = §31 #2 Core combat (in progress).** Batch 1 on disk. Combat Studio + Genesis placeholder shell shipped. Jetpack melee mapped: lights → Weak `SwordAttack` A→B→C (Invector combo) + `SwordRandomAttack.B`; charge hold `SwordCharge`; release Strong `SwordAttack.B`. Remaining: v1.6.5 acceptance Play checklist, melee polish.
- **After Phase 2 sign-off → §31 #3 utility brain** (not Director/Momentum).
- **Built through Invector (unchanged):** melee light/heavy/combos/block, ranged aim/fire with custom recoil, crits (10%, x2), stamina, dodge roll, dash (`DMDashController`), 9 ammo types / 16 ammo items, 5 status effects, Hot Cross, hex skill tree (51 stat nodes), 3-companion trio with group buffs and H/G commands, lock-on (`CombatFocusController`), death + loot, UITK combat HUD pieces.

## Phase 1 decisions (locked Oct 3, 2026)

| # | Topic | Decision |
|---|---|---|
| C15 | Element names | Keep serialized `AmmoType` values. Display Ice → Cryo, Electricity → Energy. Fire, Gunpowder, Explosive, ResonanceStabilizer are **damage types**, not elements. |
| C15 | Plasma status | **Plasma applies Burning by default.** Corroded reserved for a later acid/bio type. |
| C16 | Resource model | Plasma/Cryo ammo = plasma-fuel weapons. Power cells = Energy, Laser, Ion. Ion cost = ammo-per-hit multiplier. |
| P01 | Skill branches | Blade = Melee; Marksman = Pistols + Rifles; Survival = Survival + Player; Control = new branch. Keep hex assets; add move-unlock modifiers later. |
| F03 | Companion roles | Combat **role** layered over class. Tank and Marksman are new roles any class can take. |
| A04/F02 | One brain | Build utility brain beside old FSMs. Migrate Humanoid_Enemy Phase 3, companions Phase 11. |
| C10 | Damage contract | `DamageInfo` + `IDamageReceiver` beside `IDamageable` with adapter. |
| A06 | Archetype name | Code `EnemyArchetype` = **rig type** in docs; behaviour archetype = new profile later. |
| Invector | Body layer | **Wrap, don't replace.** |

Animation tag sheet: `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Animation_Tag_Sheet.md`.

## Phase 2 remaining (after Gate 0)

Do **not** re-implement attack yaw. Phase 2 work packages:

1. **Combat Studio + Genesis shell (§26)** — **done.** Core / Melee / Ranged / Play (v1.6.5) / Roadmap. Genesis placeholders are labels only.
2. **Doc sync** — this handoff + plan §0 / §26 / §31 (Oct 4, 2026). Keep Desktop copies aligned.
3. **Batch-1 acceptance in v1.6.5** — single hit per swing, hitbox connect, block facing, block vs parry, poise break, i-frames, strong attack (after anim pass), enemy melee reach/creep.
4. **Animation pass** — **corrected.** Lights → Jetpack Weak `SwordAttack` A→B→C (Invector WeakAttack combo) plus `SwordRandomAttack.B` as an extra regular. Charge hold `SwordCharge` (speed 0). Charge release Strong `SwordAttack.B` (AttackC). Strong A/C are other heavies. No finishers runtime.
5. **Polish order** — hitboxes → block cone (not attack) → strong charge → enemy range → optional player hitstop from profile frames.
6. **Git** — only when Anthony asks; console clean; no `git add -A`.

## Full phase order (plan section 31)

1 Audit **[Done Oct 3]** → 2 Core combat **[Partial, Phase 2 in progress; studio shell shipped]** → 3 Unified brain **[Missing, next after Phase 2]** → 4 Combat Director **[Missing]** → 5 Plasma sword template **[Partial]** → 6 Momentum + finishers **[Missing]** → 7 Sick Stick **[Missing]** → 8 Specials + Overdrive **[Missing]** → 9 Remaining elements **[names locked]** → 10 Body damage **[Missing]** → 11 Companions **[Partial]** → 12 Control/Marksman + ranged polish **[Partial]** → 13 Environment + noise **[Partial]** → 14 Combat memory **[Missing]** → 15 Encounters **[Partial]** → 16 Studio + performance **[Partial; WorldChrome + Combat Studio shell shipped]**.

## Key existing code

- `Assets/_Project/Scripts/Combat/DamageInfo.cs`, `CombatEvents.cs`, `CombatDamageApplicator.cs`, `CombatHitResolver.cs`
- `Assets/_Project/Scripts/Interaction/IDamageReceiver.cs` (legacy `IDamageable` stays as adapter)
- `Assets/_Project/Scripts/Combat/CombatPoise.cs`, `DMCombatIFrameController.cs`, `DM_CombatCoreProfile.cs` + `Resources/Combat/DM_CombatCoreProfile.asset`
- `Assets/_Project/Scripts/Combat/DMMeleeBlockThreatFacing.cs` (Gate 0 attack + block face)
- `Assets/_Project/Scripts/Combat/DMEnemyGuardBreakStagger.cs` (block/parry)
- `Assets/_Project/Scripts/Combat/PioneerMeleeHitboxTuning.cs`, `PioneerMeleeSwingHitDedupe.cs`
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
- Real game start is 0 AC, level 1, empty inventory (200 AC / level 5 / starter items are test values). Kade lands with 2 companions; more come only from restoring Ethers/Echoes. Never mention wallets or crypto.
- Bindings: D-Pad up (Journal) and D-Pad right (ammo cycle) are already taken; Sick Stick, finisher, special, Overdrive, parry and the companion radial have no bindings yet. Ask before assigning.
- Tune combat in **v1.6.5**. Do not permanently disable Gaia terrain, streaming, or HDRP world systems for FPS.

## Workflow rules

- Editor Auto Refresh is off. After Unity edits, agent runs MCP `refresh_unity` (`if_dirty`, `compile: request` when scripts change, `wait_for_ready: true`). Ask Anthony for Ctrl+R only if MCP fails.
- No commit while the console has errors. Commit only when Anthony approves. Stage exact paths only: no `git add -A`, stash, checkout, restore or reset, and never force-push.
- No silent git/depot restore. Known console noise to ignore: Unity AI "NoSubscription", Account API / XR package-list timeouts, the Input Manager deprecation notice, PhysX "cleaning the mesh failed".
- After each batch, update the plan's *Current build* blocks, §0 shipped table, and this handoff so the docs keep matching the code.
