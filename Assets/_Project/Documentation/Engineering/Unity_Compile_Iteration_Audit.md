# Unity Compile Iteration Audit

**Project:** Dark Matter: Genesis (`Assets/_Project/`)  
**Editor:** Unity 6 HDRP 6000.4.11f1  
**Date:** 2026-10-05  

This note summarizes assembly layout, compile hot paths, and low-risk iteration habits. It is guidance only — no repo changes are implied beyond documentation.

---

## 1. `.asmdef` map under `Assets/_Project/`

Most gameplay code still lives in the **default Assembly-CSharp** (no asmdef). Only the **Features** slice is split today.


| Assembly                      | Path                           | References                   | Role                            |
| ----------------------------- | ------------------------------ | ---------------------------- | ------------------------------- |
| `Project.Features.GameState`  | `Features/GameState/Runtime/`  | *(none)*                     | Game-state snapshots + service  |
| `Project.Features.WorldState` | `Features/WorldState/Runtime/` | `Project.Features.GameState` | World-state snapshots + service |
| `Project.Features.Directors`  | `Features/Directors/Runtime/`  | `GameState`, `WorldState`    | Director orchestration          |
| `Project.Features.Validation` | `Features/Validation/Runtime/` | *(none)*                     | Bootstrap / smoke validation    |
| `Project.Features.*.Tests`    | matching `Tests/` folders      | parent runtime asm           | Edit Mode tests                 |


**Not asmdef-isolated (default assembly):** `Scripts/` (Combat, Player, AI, Building, UI uGUI legacy, etc.), `Features/Climb`, `Features/Dash`, `Features/Jetpack`, `Features/Locomotion`, `Features/PlayerSystems`, `Editor/` (Editor-only), `UI Toolkit/Runtime/`.

### Vendor / large third-party assemblies (recent combat touch)


| Area                     | Location                               | Compile impact                                                                                  |
| ------------------------ | -------------------------------------- | ----------------------------------------------------------------------------------------------- |
| Invector melee / shooter | `Assets/Invector-3rdPersonController/` | Large; any edit to `vHitBox.cs`, `vMeleeAttackObject.cs` recompiles Invector + all default refs |
| UI Toolkit HUD           | `Assets/UI Toolkit/Runtime/`           | Default assembly; frequent HUD tweaks                                                           |
| Malbers (creatures)      | `Assets/Malbers Animations/`           | Separate `MalbersAnimations.asmdef`                                                             |
| ECM2                     | `Assets/ECM2/`                         | Separate asmdef; climb/dash not on ECM2 for player                                              |


**Recent WIP:** Invector melee patches (`vHitBox`, `vMeleeAttackObject`), Jetpack animator controller, `_Project` combat/player bridges.

---



## 2. Script volume (hot paths, approximate file counts)


| Folder                    | `.cs` files (approx.)                            | Notes                                                                                             |
| ------------------------- | ------------------------------------------------ | ------------------------------------------------------------------------------------------------- |
| `Scripts/Combat/`         | ~60                                              | Melee profiles, hit resolve, VFX — **high churn during Phase 2**                                  |
| `Scripts/Player/`         | ~45                                              | Invector bridges, landing, camera — large files (`PioneerShooterMeleeInput`, `DMLandingDirector`) |
| `Scripts/AI/`             | ~40+                                             | Enemy FSM, Invector bridges                                                                       |
| `Editor/`                 | ~230+                                            | Genesis Studio, building/combat tools — **Editor-only recompile**                                 |
| `Features/Climb/Runtime/` | few files, **very large** `DMClimbController.cs` | Touch sparingly during combat passes                                                              |
| `UI Toolkit/Runtime/`     | separate under `Assets/UI Toolkit/`              | UITK HUD iteration                                                                                |


Use ripgrep for live counts:

```powershell
rg --files -g "*.cs" "Assets/_Project/Scripts/Combat" | Measure-Object
```

---



## 3. Minimal split plan (phased, low risk)

Goal: shorten **Edit → Play** loop without breaking Invector/type references.

### Phase 0 (now)

- Keep status quo; document hot folders (this file).
- Prefer profile + Genesis Studio tuning over editing mega-files when possible.



### Phase 1 — `DM.Combat` runtime asmdef

- **Include:** `Assets/_Project/Scripts/Combat/`** (exclude any `Editor` subfolders if added later).
- **References:** default Unity assemblies only; **no** reference to Editor.
- **Risk:** Low if Combat does not reference Editor types (verify `PioneerMelee`* and profiles).
- **Win:** Combat script edits stop recompiling entire `Scripts/AI`, `Building`, etc. *only after* those areas do not circular-ref Combat.



### Phase 2 — `DM.Player` runtime asmdef

- **Include:** `Scripts/Player/`**, optionally `Features/Climb`, `Dash`, `Jetpack`, `Locomotion` (or split locomotion later).
- **References:** `DM.Combat`, Invector assemblies (implicit via default until Invector is wrapped).
- **Risk:** Medium — Invector types span Player + Combat; may require **asmdef reference to a thin** `DM.InvectorBridge` package folder first.



### Phase 3 — `DM.UI` runtime asmdef

- **Include:** `Assets/UI Toolkit/Runtime/`** (UITK lock surfaces).
- **References:** Combat/Player as needed for HUD bindings.
- **Risk:** Medium — avoid pulling uGUI `Scripts/UI` into same asm until cutover complete.



### Phase 4 — `DM.Editor` asmdef

- **Include:** `Assets/_Project/Editor/`** (already Editor-only).
- **References:** runtime asmdefs above.
- **Risk:** Low; Editor already isolated by `#if UNITY_EDITOR` in many tools.

**Do not** split Invector vendor code — wrap with `_Project` APIs (`dm-naming-no-invector`).

---



## 4. Enter Play Mode options


| Mode                                                                                                     | When to use                                                                                                                        |
| -------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| **Enter Play Mode Options enabled** — disable **Reload Domain** *only* for fast UI/layout/UITK iteration | HUD layout, USS, non-serialized static caches. **Not** for combat/melee/animator/profile work.                                     |
| **Full domain reload (default)**                                                                         | Combat, animator controllers, new `[SerializeField]`, ScriptableObject shape changes, Invector patches, new components on prefabs. |
| **Disable Reload Scene** (optional)                                                                      | Same scene repeated playtests; pair with explicit scene save discipline.                                                           |


**Recommendation for this project:** **Combat-full = domain reload ON.** Use fast mode only on UITK branches with a checklist to re-test one full reload before commit.

---



## 5. Antivirus / refresh habits checklist

- [ ] Exclude Unity project root from real-time scan on `Library/`, `Temp/`, `Logs/`, `obj/` (keep `Assets/_Project` scanned).
- [ ] **Auto Refresh off** in Editor — agent/user runs **Ctrl+R** or MCP `refresh_unity` after batch file writes.
- [ ] Batch script edits; one refresh after last file in a task (`wait_for_ready: true`).
- [ ] Do not commit with Unity console **Errors** (`read_console` / MCP).
- [ ] Avoid `git add -A`; stage scenes/prefabs/materials with script changes.
- [ ] No silent checkout/restore; run `Tools/DmUnityGitSafeguard.ps1` before branch switches.
- [ ] Protect `Documentation/Design/ArtReference/` — never delete Life Sheets without explicit approval.

---



## 6. Combat Studio / Genesis Combat — runtime authority

**Rule:** Any numeric or prefab field exposed in **Combat Studio** or **Genesis Studio → Combat** must live on a **Resources** profile (primary: `Assets/_Project/Resources/Combat/DM_CombatCoreProfile.asset`) and be read through **`DM_CombatCoreProfile.Live`** (or `Resolve`) at **runtime and in shipped builds**. Play-mode slider edits must affect the same in-memory asset instance that `Live` returns; use Genesis **`playModeSave: true`** on combat subtabs so tweaks persist on Play exit.

**Editor-only (by design):** `DMMeleeAnimationSetApplier` / Combat Studio **Build And Apply Melee Animation Set** — assigns **animation clip paths** on animator controllers (`DM_MeleeAnimationSet`), not live combat numbers. Strong charge / Strong B **anim speed** is profile-driven at runtime (`PioneerMeleeDamageWindowTracker`); applier only forces controller state speed **1** so Play does not double-apply.

**Not started (profile hooks only):** `hitstopLightFrames`, `hitstopHeavyFrames` — no consumer yet; documented in `DMG_Combat_Plan_v2.md` §0.

---

## 7. Related docs

- `Assets/_Project/Documentation/Engineering/Unity_Git_Disk_Truth.md`
- `.cursor/rules/unity-agent-workflow.mdc`
- `AGENTS.md` (playable scene, git source of truth)
- `Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md` (combat disk truth)

