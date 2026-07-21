# Audit_02 — Player

**HLA:** §2.2 Gameplay  
**Paths:** `Scripts/Player/`, `Scripts/Player/Invector/`  
**Audit date:** July 2026

---

## Excellent / keep

- **`PioneerInvectorBootstrap`** — centralized Invector wiring with early execution order.
- **Bridge decomposition** — damage, weapon, projectile, input bridges are separable concerns.
- **`PioneerShooterManager`** — extends Invector shooter with project ammo integration.
- **`CombatFocusController`** — ranged aim/focus without polluting locomotion.
- **`PlayerGameStateProvider`** — correct snapshot bridge for vitals + session phase.

---

## Move later

| Current | Target |
|---------|--------|
| Invector bridge cluster | `Features/Gameplay/Player/Invector/` when player domain refactored |
| UI modal flags on `PlayerController` | `IFocusService` or Input System action maps |
| `PlayerLocator` + `PlayerReference` | single `IPlayerReferenceService` in Core |

---

## Risk

| Risk | Detail |
|------|--------|
| **Dual player stacks** | ECM2 `PlayerController` vs Invector bootstrap — prefab-dependent |
| **UI leakage** | `PlayerController` gates on journal/map/quest/building flags |
| **Component sprawl** | 10+ Invector bridges on one prefab |
| **Scene lookups** | `FullscreenUiNavigator`, `FindAnyObjectByType<UIManager>()` from input adapters |

---

## WorldState fields

Player momentary state stays in **GameState** (`PlayerSnapshot`). WorldState does not duplicate vitals.

Experience telemetry may read `PlayerSnapshot` ratios for **EstimatedStress** / **EstimatedUrgency**.

---

## Dependencies

**Inbound:** Input System, survival, equipment, combat, UI navigators.  
**Outbound:** `SurvivalStats`, `InventorySystem`, `EquipmentController`, `WorldUseController`, Invector managers, `CombatHitResolver`.

**GameState provider:** `PlayerGameStateProvider` → `PlayerLocator`, `SurvivalStats`, `GameSession`.
