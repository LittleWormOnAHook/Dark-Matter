# Audit_03 — Combat & AI

**HLA:** §2.2 Gameplay · §2.3 World (threat)  
**Paths:** `Scripts/Combat/`, `Scripts/Interaction/`, `Scripts/AI/`  
**Audit date:** July 2026

---

## Excellent / keep

- **`CombatHitResolver`** — shared static resolution for projectile + hitscan.
- **`EnemyAiController` partials** — FSM split (Threat, States, Movement, CombatPositioning) aids maintenance.
- **`EnemySenses` + `EnemyNoiseEvents`** — lightweight detection/event pattern.
- **`EnemyRegistry`** — ScriptableObject catalog via Resources.
- **`CompanionCombatCoordinator`** — trio attack scheduling (sequential/paired/staggered).
- **Invector enemy bridges** — parallel to player pattern; reusable setup utilities in Editor.

---

## Move later

| Current | Target |
|---------|--------|
| `Combat/` + Invector bridges | `Features/Gameplay/Combat/` unified docs |
| `EnemySpawner` reflection setup | Editor-validated prefab pipeline (Generation) |
| Threat/scaling | `ThreatWorldStateProvider` for WorldState |

---

## Risk

| Risk | Detail |
|------|--------|
| **Cross-domain static resolver** | `CombatHitResolver` references AI, Companions, Survival, UI |
| **Split damage ownership** | Invector bridges vs `CombatHitResolver` vs `EnemyHealth` |
| **Legacy controller stubs** | `MeleeCombatController`, `RangedCombatController` shells |
| **Double damage feedback** | Resolver + `EnemyHealth` both may show floating damage |
| **5-file EnemyAiController** | High audit surface for solo maintenance |

---

## WorldState fields

| Field | Source |
|-------|--------|
| `ThreatLevel` | combat density, zone pressure, recent kills |
| `EstimatedStress` inputs | combat duration, low health (via Experience) |
| `DangerBudget` | spawn pressure cap (Experience Director) |

No dedicated AI GameState provider today — gap for future `ThreatSnapshot`.

---

## Dependencies

**Inbound:** Player combat bridges, companion coordinator, pool manager, quest kill relays.  
**Outbound:** `SurvivalStats`, companion/pioneer transforms (threat targeting), loot/disintegration, achievements.

**GameState:** none dedicated; player vitals indirect via `PlayerGameStateProvider`.
