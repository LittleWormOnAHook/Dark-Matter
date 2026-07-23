# World Engine — Disk Status (authoritative for agents)

**Audit date:** July 22, 2026 (updated after Run 1)  
**Repo path surveyed:** `Assets/_Project/`  
**Authority for design:** HLA v1.0 + TDB  
**Authority for progress:** this file + GDD 5.0 Appendix B  
**Reasoning map (M3/M4):** [World_Engine_Reasoning_Map.md](World_Engine_Reasoning_Map.md)

---

## Rule

Do **not** treat Architecture “[Shipped]” rows or ChatGPT session notes as truth without checking this file.  
A Features module is shipped only when its `.cs` / `.asmdef` files exist under `Assets/_Project/Features/<Name>/`.

---

## On disk today

| Item | Status |
|------|--------|
| GDD 5.0 + Architecture markdown | Present (design) |
| `Features/GameState` Runtime + Adapters + Tests | **Present (Run 1)** |
| `Features/WorldState` Runtime + Adapters + Tests | **Present (Run 1)** |
| `Features/Directors` Runtime + Adapters + Tests | **Present (Run 1)** |
| `Features/Validation` Runtime + Tests | **Present (Run 1)** |
| `CompanionSystemsBootstrap` Features chain | **Wired:** GameState → WorldState → Directors |
| Smoke F9 / F10 / F11 | **Present** (`DarkMatterSmokeDriver`) |
| `Features/Communications` Runtime | **Absent** (Run 2) |
| `Features/Experience` / `Generation` | **Absent** |
| `Scripts/Survival/Exposure/` | Present |
| `EchoGenerator` + chronicle + building ops save | Present |
| World seed in `GameSaveData` | **Not yet** (Run 3) |

---

## Near-term build track (LLM deferred)

0. Doc honesty — done  
1. World Engine spine — **done (this pass)**  
2. Internal Communications (rule-based)  
3. Persistent generated world — seed + Generation wrap + save fields  
4. Living-world slice — richer Weather/Simulation director logic  

Safe Mode: see [Unity_Safe_Mode_Recovery.md](Unity_Safe_Mode_Recovery.md).

---

## Deferred

- Communications Phase 8.1 LocalVoiceLLM  
- Communications Phase 9+ LLM / cloud conversation  
- Full Io biomes, Aether-9 story arc
