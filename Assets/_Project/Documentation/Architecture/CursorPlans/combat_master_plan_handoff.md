# Handoff: Dark Matter Genesis Combat Master Plan (status + next steps)

Written Oct 3, 2026 for a Cursor agent picking up combat work in the live project `A:\Dark Matter Genesis` (Unity 6000.4.11f1 HDRP, branch `cursor/wip-clean-20260919`).

## Read first

1. `AGENTS.md` and `.cursor/rules/` (they win over this file). Especially `dark-matter-genesis-core.mdc`, `unity-agent-workflow.mdc`, `dark-matter-genesis-genesis-studio.mdc`, `dark-matter-genesis-uitk-lock.mdc`, `dm-naming-no-invector.mdc`, `dark-matter-genesis-player-physics.mdc`.
2. **The plan:** `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md` (Master Plan v2.1, Sep 29 2026, code-status synced Sep 30). 34 sections; each ends with a *Current build* block and a [Built]/[Partial]/[Missing]/[Conflicts] marker.
3. **The audit:** `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Audit_Phase1.md` (read-only code audit vs the plan, Sep 30 2026). Section 2 = per-system table with file evidence, section 4 = conflicts, section 7 = gaps per phase, section 8 = first batch.

## Where things stand

- **Phase 1 (Audit) closed Oct 3, 2026.** Decisions locked; animation tag sheet added. **Phase 2 batch 1 code landed** (DamageInfo, events, poise, status immunity/stacks, i-frames, Combat Core profile + Studio tab). Sandbox scene: run **Tools → Dark Matter Genesis → Combat → Create Combat Sandbox Scene** after **Ctrl+R**.
- **Built today (mostly through Invector):** melee light/heavy/combos/block, ranged aim/fire with custom recoil, crits (10%, x2), stamina, dodge roll, dash (`DMDashController`), 9 ammo types / 16 ammo items, 5 status effects, Hot Cross quick-select, hex skill tree (51 stat nodes), 3-companion trio with group buffs and H/G commands, lock-on (`CombatFocusController`), death + loot workflow, UITK combat HUD pieces.
- **Not started:** unified utility AI brain, Combat Director / attack tokens, poise, parry, i-frames, hitstop, Momentum meter, finishers (signal is designed in plan section 17: spinning gold-white "dark matter" chest sigil, own sound, pose, double rumble), Sick Stick, specials/Overdrive, body damage/dismemberment, factions, combat memory.
- **No combat code has been changed for this plan yet.** Phase 2 has not started.

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

## Next: first implementation batch (audit section 8)

Small, testable, nothing working gets removed or replaced.

1. **Close Phase 1:** record the decisions above in the plan and audit (with Anthony's answers), build the animation tag sheet.
2. **`DamageInfo` + `IDamageReceiver` + `CombatEvents` bus** (hit, crit, stagger, kill, status applied). `CombatHitResolver` builds the DamageInfo; `IDamageable` stays as the adapter. Fields: amount, damage type, element, crit, poise damage, status, source, hit point, body part (unused for now).
3. **Poise component** on enemies and `TrainingDummy`; at zero it calls the existing `EnemyInvectorRagdollBridge` stagger. Values from a Studio "Combat Core" profile.
4. **Extend `CombatStatusEffectController`:** max stacks, immunity window, boss multiplier, from a Status profile.
5. **Dodge and dash i-frames** (0.25 s, Studio value) gated in the player damage path, plus a perfect-dodge event on CombatEvents.
6. **Combat sandbox scene** (a copy, never an edit of an existing scene): TrainingDummy, one Humanoid_Enemy, a simple spawner. Add a Genesis Studio **Combat Core** tab (poise, i-frames, hitstop, parry window, status rules) in UITK with the Frontier editor theme.

**Done when:** the dummy staggers at poise zero; a status can't re-apply during its immunity window; a roll through a swing takes no damage; CombatEvents logs every hit with its DamageInfo; 0 new console errors.

## Full phase order (plan section 31)

1 Audit [in progress] -> 2 Core combat [partial] -> 3 Unified brain [missing] -> 4 Combat Director [missing] -> 5 Plasma sword template [partial] -> 6 Momentum + finishers [missing] -> 7 Sick Stick [missing] -> 8 Specials + Overdrive [missing] -> 9 Remaining elements [conflicts] -> 10 Body damage [missing] -> 11 Companions [partial] -> 12 Control/Marksman + ranged polish [partial] -> 13 Environment + noise [partial] -> 14 Combat memory [missing] -> 15 Encounters [partial] -> 16 Studio + performance [partial].

## Key existing code

- `Assets/_Project/Scripts/Combat/CombatHitResolver.cs` (keep, extend: builds DamageInfo, raises events)
- `Assets/_Project/Scripts/Interaction/IDamageable.cs` (wrap with IDamageReceiver)
- `Assets/_Project/Scripts/Combat/CombatStatusEffectController.cs` (keep, extend)
- `Assets/_Project/Scripts/Combat/TrainingDummy.cs`
- `Assets/_Project/Scripts/AI/Invector/EnemyInvectorRagdollBridge.cs` (stagger used by poise)
- `Assets/_Project/Scripts/AI/EnemySenses.cs` (keep, extend with awareness meter)
- `Assets/_Project/Scripts/AI/EnemyAiController.cs` (replace gradually; NavMesh path stays dormant)
- `Assets/_Project/Scripts/Creatures/DMICreatureAiController.cs` (replace later)
- `Assets/_Project/Scripts/Companions/CompanionCombatCoordinator.cs` (seed for player-side token pool)
- `Assets/_Project/Features/Dash/Runtime/DMDashController.cs` (add i-frames + perfect dodge)
- `Assets/_Project/Scripts/Progression/SkillDefinition.cs` (add move-unlock modifiers, branch mapping)
- `Assets/_Project/Scripts/Player/CombatFocusController.cs` (lock-on)
- `Assets/_Project/Scripts/Player/Invector/PioneerInvectorSurvivalBridge.cs` (player damage path; i-frame gate goes here or beside it)
- Hot Cross: `DMUiToolkitHotCross` (keep; specials/Sick Stick may share it later)

## Project locks that apply to combat

- No NavMesh. Don't retune `Player_v7` capsule/layers/physics; wire both the `Player_v7 Variant` prefab and the hierarchy `Player_v7`.
- All new UI is UI Toolkit only. New editor windows/inspectors use the Frontier theme (`Assets/_Project/Editor/GenesisTheme`).
- Every combat number is a Genesis Studio profile value, never hardcoded.
- Real game start is 0 AC, level 1, empty inventory (200 AC / level 5 / starter items are test values). Kade lands with 2 companions; more come only from restoring Ethers/Echoes. Never mention wallets or crypto.
- Bindings: D-Pad up (Journal) and D-Pad right (ammo cycle) are already taken; Sick Stick, finisher, special, Overdrive, parry and the companion radial have no bindings yet. Ask before assigning.

## Workflow rules

- Editor Auto Refresh is off. **Anthony presses Ctrl+R himself.** Never force a refresh/compile; tell him when to press it, then check the console.
- No commit while the console has errors. Commit only when Anthony approves. Stage exact paths only: no `git add -A`, stash, checkout, restore or reset, and never force-push. The branch is currently 3 commits ahead of origin (unpushed) plus unrelated uncommitted rock/cliff work in `Assets/Genesis PCG Rock Creation`; leave that alone.
- Back up any file before editing it. Known console noise to ignore: Unity AI "NoSubscription", Account API / XR package-list timeouts, the Input Manager deprecation notice, PhysX "cleaning the mesh failed".
- After each batch, update the plan's *Current build* blocks and status markers so the docs keep matching the code.
