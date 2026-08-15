# Audit_06 — Story

**HLA:** §2.5 Story · §2.9 Kairos (Intelligence)  
**Paths:** `Scripts/Quests/`, `Scripts/Echoes/`  
**Audit date:** July 2026

---

## Excellent / keep

- **`QuestRegistry` + `QuestDefinition`** — ScriptableObject-first quest catalog.
- **`QuestManager`** — progress tracking, inventory-linked collect objectives, events.
- **`QuestGiverNpc` + dialog UI** — authored NPC flow.
- **`EchoWorldEntity` + `EchoSignalRegistry`** — procedural rescue path, static signal list for UI/sense.
- **`EchoGenerator`** (in Pioneers/) — procedural echo generation hook.

---

## Move later

| Current | Target |
|---------|--------|
| `QuestManager` execution | `StoryDirector` + `IQuestCommandService` adapter |
| Quest data | `Features/Story/Data/` |
| `EchoGenerator` | `Features/Generation/` or Simulation |
| Kairos quest state | `Features/Kairos/` Intelligence layer |
| `CommsQueryService.KairosAdvisoryUnlocked` | `KairosWorldStateProvider` |

---

## Risk

| Risk | Detail |
|------|--------|
| **QuestManager = brain today** | violates HLA "quests are outputs" |
| **Inventory polling** | collect objectives bind `InventorySystem` directly |
| **Reward sprawl** | touches progression, crafting, inventory, SimpleGameManager |
| **UI creates QuestManager** | `UIManager.EnsureQuestManager()` |
| **Thin Echoes folder** | chronicle logic in roster; 4 scripts only |
| **Hostile echo → AI prefab** | combat spawn side effect |

---

## WorldState fields

| Field | Source |
|-------|--------|
| `StoryChapterId` | quest graph / active arc |
| `CompletedQuestIds` | QuestManager |
| `ActiveQuestIds` | MissionSnapshot (GameState) |
| `MemoryCoresRestored` | Kairos provider (future) |
| `KairosAdvisoryUnlocked` | Comms flag → WorldState |
| Echo rescue count | roster chronicle |

**Providers:** `StoryWorldStateProvider`, `KairosWorldStateProvider`.

---

## Dependencies

**Inbound:** UI quest HUD, GameState mission provider, achievements, communications context.  
**Outbound:** inventory, progression, crafting, roster, AI spawns (echo guardians).

**Intelligence (planned):** `StoryDirector` reads WorldState; emits quest commands + story beat events.
