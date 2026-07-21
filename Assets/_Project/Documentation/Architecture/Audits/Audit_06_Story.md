# Audit_06 — Story

**HLA:** §2.5 Story · §2.9 Aether-9 (Intelligence)  
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
| Aether-9 quest state | `Features/Aether9/` Intelligence layer |
| `CommsQueryService.Aether9AdvisoryUnlocked` | `Aether9WorldStateProvider` |

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
| `MemoryCoresRestored` | Aether-9 provider (future) |
| `Aether9AdvisoryUnlocked` | Comms flag → WorldState |
| Echo rescue count | roster chronicle |

**Providers:** `StoryWorldStateProvider`, `Aether9WorldStateProvider`.

---

## Dependencies

**Inbound:** UI quest HUD, GameState mission provider, achievements, communications context.  
**Outbound:** inventory, progression, crafting, roster, AI spawns (echo guardians).

**Intelligence (planned):** `StoryDirector` reads WorldState; emits quest commands + story beat events.
