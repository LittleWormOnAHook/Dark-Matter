# World Engine — Disk Status (authoritative for agents)

**Audit date:** July 22, 2026  
**Repo path surveyed:** `Assets/_Project/` on git `main` / this workspace  
**Authority for design:** HLA v1.0 + TDB (what to build)  
**Authority for progress:** this file + GDD 5.0 Appendix B (what exists)

---

## Rule

Do **not** treat Architecture “[Shipped]” rows or ChatGPT session notes as truth.
A Features module is shipped only when its `.cs` / `.asmdef` files exist under `Assets/_Project/Features/<Name>/`.

---

## On disk today

| Item | Status |
|------|--------|
| GDD 5.0 + Architecture markdown (HLA, TDB, audits, Phase D doc) | Present (design) |
| `Features/Communications/Documentation/` + Data/Audio READMEs | Present |
| `Features/Communications` Runtime / UI / Adapters / Tests `.cs` | **Absent** |
| `Features/GameState` | **Absent** |
| `Features/WorldState` | **Absent** |
| `Features/Directors` | **Absent** |
| `Features/Validation` | **Absent** |
| `Features/Experience` | **Absent** |
| `Features/Generation` | **Absent** |
| `Scripts/Survival/Exposure/` | Present |
| `Scripts/Pioneers/EchoGenerator.cs`, `EchoChronicleEntry.cs` | Present |
| `Scripts/Building/BuildingOperationRegistry`, `FacilityTaskRunner` | Present |
| `Scripts/UI/EnvironmentalCrisisHudMode.cs` | Present (not WeatherDirector) |
| `GameSaveData` / `GameSaveSystem` (v17) | Present — no world seed / WorldState blob |
| `CompanionSystemsBootstrap` Features chain | **Not wired** |

---

## Near-term build track (LLM deferred)

0. Doc honesty (this file + GDD B4/B5)  
1. World Engine spine — GameState → WorldState → Directors → Validation  
2. Internal Communications (rule-based; skip Phase 8.1 and Phase 9+)  
3. Persistent generated world — seed + Generation wrap + save evolutionary fields  
4. Living-world slice — WeatherDirector / SimulationDirector → crisis HUD + chronicle  

---

## Deferred

- Communications Phase 8.1 LocalVoiceLLM  
- Communications Phase 9+ LLM / cloud conversation  
- Full Io biomes, Aether-9 story arc (until spine + radio exist)
