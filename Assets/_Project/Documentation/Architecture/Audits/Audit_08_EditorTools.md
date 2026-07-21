# Audit_08 — Editor Tools

**HLA:** §2.11 Editor Framework  
**Path:** `Assets/_Project/Editor/`  
**Audit date:** July 2026

---

## Inventory

**106** `.cs` files across:

| Category | Count | Path | Examples |
|----------|------:|------|----------|
| Root utilities | ~79 | `Editor/` | `EnemyPrefabCreatorWindow`, `CraftingSetup`, `QuestGiverSetup`, `SurvivalPioneerEditorMenus` |
| Invector setup | 9 | `Editor/Invector/` | Pioneer/enemy setup, ragdoll audit |
| UI Layout Studio | 12 | `Editor/UiLayoutEditor/` | `UiStudioWindow`, layout capture |
| Companions | 3 | `Editor/Companions/` | `CompanionPrefabGenerator` |
| Vehicles | 1 | `Editor/Vehicles/` | `HovercraftSetupUtility` |
| DevTools | 2 | `Editor/DevTools/` | `LocalAgentWindow` |
| Communications | 1+ | `Features/Communications/Editor/` | Crew database sync menu |

**Themes:** enemy/AI prefab builders, crafting/recipe creators, quest/map/exposure setup, play-mode edit persistence, combat/progression setup, project structure menus.

---

## Excellent / keep

- **Prefab builder culture** — 100+ editor scripts = framework-developer mindset.
- **Communications crew sync** — `Dark Matter: Genesis → Communications → Sync Crew Database From Companions` aligns Data-first pipeline.
- **UiLayoutEditor studio** — scalable HUD authoring for Solo AAA velocity.
- **Invector setup utilities** — reduce manual prefab wiring errors.
- **`PlayModeEditPersistence`** — designer iteration support.

---

## Move later

| Current | Target |
|---------|--------|
| Scattered `Editor/` root | `Features/<Name>/Editor/` per framework module |
| One-off setup scripts | unified **Validation Tools** runner (HLA §12) |
| No pacing debugger | `Features/Experience/Editor/PacingDebugger` |

---

## Risk

| Risk | Detail |
|------|--------|
| **No unified menu hierarchy** | mixed `Dark Matter: Genesis/` paths |
| **Duplication with runtime** | setup scripts may drift from runtime bridges |
| **No dependency viewer** | planned HLA §12 — not built |
| **No framework visualizer** | docs/manual for WoOS layers |

---

## WorldState / tooling

Planned editor tools reading snapshots:

| Tool | Reads |
|------|-------|
| **Simulation Debugger** | WorldState Simulation + Colony |
| **Pacing Debugger** | ExperienceSnapshot, density meters |
| **Story Timeline** | Story WorldState + quest data |
| **Relationship Viewer** | Simulation relationships (future) |
| **Dependency Viewer** | bootstrap + provider registry |

---

## Dependencies

**Inbound:** all runtime domains for authoring.  
**Outbound:** ScriptableObjects, prefabs, validated scenes.

**Menu convention (target):** `Dark Matter: Genesis / Dark Matter / <Framework> / <Tool>`

**Design Pillars gate:** every new editor tool must cite which pillar(s) it accelerates (usually **Believability** + content pillar).
