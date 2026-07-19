# Pioneer Companion Invector Rebuild — Revised Plan

**Branch context:** `cursor/pioneer-companion-hud-and-ui-polish`  
**Player reference:** `Player_Invector.prefab`  
**Companion reference:** scene `PioneerCompanion` (legacy GKC stack → migrate to Invector)

## Goals

- Rebuild expedition companions on the **same Invector body/combat stack** as the player.
- **Keep** Pioneer companion brains: roster spawn, formation follow, trio combat coordination, health/injury, Echo sensing, Journal/menu loadouts.
- Support **class-based loadouts** (weapons, tools, deployables, buffs) configured later via existing companion menu + new data assets.
- **Preload all weapon/device slots** on the companion prefab; prune unused slots per pioneer at `BindRecord` when arsenal grows.

---

## Architecture

```
Journal / menus → PioneerRosterManager.OnTrioChanged
  → CompanionRosterBridge.RefreshCompanions()
  → Instantiate(PioneerCompanion_Invector)
  → PioneerCompanionAgent.BindRecord(record, player, slot)

Pioneer companion layer (KEEP)
  PioneerCompanionAgent, CompanionFollowController, CompanionCombatController,
  CompanionCombatCoordinator, CompanionHealth, CompanionInjuryHandler,
  CompanionSenseController, CompanionThreatSensor, CompanionTaskQueue,
  PioneerExpeditionCommandInput, ExpeditionPioneerHudUI

Invector body layer (NEW)
  CompanionInvectorBootstrap, CompanionInvectorMotorBridge,
  CompanionInvectorLoadoutBridge, CompanionInvectorCombatBridge,
  CompanionInvectorDamageBridge, vThirdPersonController,
  vMeleeManager, vShooterManager, Drawn_*/Holstered_* slots

Ability layer (NEW — stubs in Phase 1, config later)
  CompanionAbilityData, CompanionClassProfile, CompanionAbilityController
```

**Do not use** Invector `vSimpleMeleeAI_Companion` as the primary controller — Pioneer formation follow and trio logic stay custom.

---

## Ability Layer (class loadouts)

### Data types

| Asset | Purpose |
|-------|---------|
| `CompanionAbilityKind` | Weapon, Deployable, Buff, Tool |
| `CompanionAbilityData` | ScriptableObject: id, kind, class mask, cooldown, animation hook, AI priority hint |
| `CompanionClassProfile` | Per-class allowed ability categories, default loadout slots, validation rules |
| `CompanionAbilityController` | Runtime: resolves record loadout → abilities, cooldowns, `OnAbilityUsed` event for HUD |

### Class examples (configure later)

| Class | Weapons | Devices / skills |
|-------|---------|------------------|
| Combat Tactician | Melee (shields) | Party buffs, defensive auras |
| Architect Engineer | Small ranged | Turrets, deployables, scan tools |
| Science Specialist | — | Med/heal tools, Echo boost |
| Infiltrator Scout | Light melee/ranged | Scan, stealth tools |

Loadout menu edits `SkilledPioneerRecord` against `CompanionClassProfile` constraints.

### Cross-cutting rules (decide during config)

- **Deployables:** team damage attribution via `PioneerInvectorDamageResolver`, despawn on companion injury/death, max active per pioneer.
- **Buffs:** stacking policy (refresh vs exclusive) defined once in buff system.
- **Resources:** cooldown-only for v1 (no AC drain from abilities).
- **Sulfur storms:** consider suppressing outdoor deployables/scans (GDD alignment).
- **Persistence:** loadout on record; cooldown state does not persist across saves.
- **Animation:** generic "use item" fallback until class-specific clips exist.
- **Memory:** `CompanionInvectorLoadoutBridge.PruneUnusedSlots(record)` at bind time.

---

## Phases

### Phase 1 — Prefab, bootstrap, damage resolver, ability stubs ✅ (in progress)

- [x] `IInvectorOutgoingDamageSource` + `PioneerInvectorDamageResolver` (attacker lookup, not player singleton)
- [x] `CompanionAbilityData`, `CompanionClassProfile`, `CompanionAbilityController` stubs
- [x] `CompanionInvectorBootstrap` — strip player-only components, no singleton
- [x] `CompanionInvectorLoadoutBridge`, `CompanionInvectorMotorBridge`, `CompanionInvectorDamageBridge` stubs
- [x] `CompanionInvectorSetupUtility` — build `PioneerCompanion_Invector.prefab` from `Player_Invector`
- [x] `PioneerCompanionAgent` — detect Invector stack, skip GKC setup path
- [ ] Run setup menu in Unity, tune prefab, verify scene reference companion

### Phase 2 — Motor bridge ✅

- `CompanionInvectorMotorBridge` feeds follow controller movement into `vThirdPersonController` animator params
- Update `CompanionFollowController.SyncLocomotionLimitsFromOwner()` for Invector player motor
- Hybrid: follow controller owns translation; motor bridge drives Invector animation

### Phase 3 — Loadout bridge (weapons + ability slots) ✅

- Extend `CompanionInvectorLoadoutBridge` with preloaded Drawn/Holstered pairs (reuse player slot tooling)
- Bind `SkilledPioneerRecord.weaponItemId` + future ability ids from loadout menu
- Draw/holster from combat engagement via `CompanionCombatController`
- `PruneUnusedSlots` for companion memory / loadout cleanup
- Legacy `CompanionEquipmentVisual` bypassed when Invector loadout bridge is present

### Phase 4 — Combat bridge ✅

- [x] `CompanionInvectorCombatBridge`: companion brain → Invector melee/shooter/unarmed attacks
- [x] `CompanionCombatController` routes to combat bridge; skips manual damage + GKC fallback when Invector stack present
- [x] Outgoing damage via Invector hitboxes/projectiles → `CompanionInvectorDamageBridge` (0.25×)
- [x] Coordinator, threat sensor, and player `CombatFocusController` targeting unchanged
- [x] GKC combo / `PlayerGkcAnimatorDriver` path bypassed for Invector companions (stripped at bootstrap)

### Phase 5 — Ability execution

- Wire `CompanionAbilityController` to combat/follow state machine
- Implement deployable spawner, buff applier, tool executors per `CompanionAbilityKind`
- Class profiles as ScriptableObject assets under `Assets/_Project/Data/Companions/`
- HUD: subscribe to `OnAbilityUsed` for cooldown icons on `ExpeditionPioneerHudUI`

### Phase 6 — Spawn integration & legacy removal

- Point `CompanionRosterBridge` / `PioneerCompanionDefaults` at Invector prefab (feature flag during rollout)
- Deprecate: `CompanionAnimationDriver`, `CompanionGkcAnimationAssets`, GKC branch in agent
- Update `PioneerCompanionPrefabCreator` menu labels (Legacy vs Invector)

### Phase 7 — Test matrix

| Area | Test |
|------|------|
| Journal spawn | Trio assignment → 3 companions, correct class/name |
| Follow | Formation, catch-up, idle wander, hold H / release G |
| Melee | Drawn slot, attack player lock target, damage popups |
| Ranged | Pistol/rifle holster/draw cycle |
| Class loadout | Menu edits validated against class profile |
| Injury | Death → roster injured → Science Lab recovery |
| Player UI | Menus pause player only; companions keep acting |
| Performance | 3 companions + player, pruned slots, no duplicate PlayerInput |

---

## Prefab strategy

- **`PioneerCompanion_Invector.prefab`**: full weapon/device slot library (same authoring workflow as player).
- **Runtime:** `BindRecord` activates loadout-relevant slots; optional prune destroys unused slot objects.
- **Future weapons/tools:** add Drawn/Holstered (or device anchor) children + refresh utility; no spawn-flow changes.

---

## Key files

| Path | Role |
|------|------|
| `Scripts/Companions/Invector/CompanionInvectorBootstrap.cs` | Companion Invector init |
| `Scripts/Companions/Invector/CompanionInvectorLoadoutBridge.cs` | Weapons + ability slot binding |
| `Scripts/Companions/Abilities/*` | Ability data + controller stubs |
| `Scripts/Player/Invector/PioneerInvectorDamageResolver.cs` | Shared outgoing damage lookup |
| `Editor/Invector/CompanionInvectorSetupUtility.cs` | Prefab builder |
| `Prefabs/Companions/PioneerCompanion_Invector.prefab` | Output prefab |
| `Resources/Companions/PioneerCompanion_Invector.prefab` | Runtime load path (when enabled) |
