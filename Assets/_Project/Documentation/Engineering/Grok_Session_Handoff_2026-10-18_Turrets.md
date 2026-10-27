# Grok bot — Session Handoff (Turrets, Alarm, FPS)

**Generated:** 2026-10-18  
**Project root:** `A:\Dark Matter Genesis`  
**Branch:** `cursor/wip-clean-20260919` (tracks `origin/cursor/wip-clean-20260919`)  
**HEAD (pushed):** `273cf84e2` — *feat(combat): turret authority, Alarm light, tracking persistence, and CPU trims*  
**Playable scene:** `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity`  
**Prior handoff templates:** `Grok_Commit_Push_Handoff.md`, `ChatGPT_Session_Handoff_2026-10-16.md`

---

## 1. Copy-paste Grok system prompt (start here)

```
You are Grok bot continuing Dark Matter: Genesis on Anthony's live Unity folder A:\Dark Matter Genesis.

READ FIRST: .cursor/rules/dark-matter-genesis-core.mdc, unity-agent-workflow.mdc, dark-matter-genesis-player-physics.mdc (no Player_v7 retune), dark-matter-genesis-uitk-lock.mdc.

Branch: cursor/wip-clean-20260919 @ 273cf84e2 (origin up to date for turret commit). Unity 6 HDRP 6000.4.11f1. Play scene: Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity. Auto Refresh OFF — MCP refresh_unity after Assets/ edits; console 0 errors + 0 warnings before done/commit. No git add -A. No Deep Profile (crashes). No NavMesh. FORGE3D lives under Assets/FORGE3D/ (may be untracked locally — do not mass-add vendor tree unless Anthony asks).

SESSION CONTEXT (Oct 18 2026): FORGE3D turrets are wired through DMTurretRuntime as single prefab authority. Tracking Authority + Instance Authority on DM Turret Runtime (custom inspector orange sections). Play-mode hooks re-apply tracking to F3DTurret after exit (DMTurretEditModeTrackingHooks). All 12 combat prefabs under Assets/_Project/Prefabs/Combat/Turrets/ have Alarm sphere + DMTurretAlarmLight (yellow scan, red track, green fire; blink faster when target closer; solid while firing). Bake/wire: Tools → Dark Matter Genesis → Combat → Turrets menus; WireAlarmLight in DMTurretPrefabBakeUtility.

FPS: CPU-bound (~8–14 FPS in Editor Play, GPU ~3ms). Fixes shipped: turret FX cleanup spam, F3D DebugDraw off in play, vision gizmos edit-only, alarm 20Hz blink, turret dormancy when player far, calm enemy AI 6Hz tick, humanoid offscreen skinning policy, motor bridge LOD skip. EditorLoop spikes in Profiler are often editor overhead — validate with Development Build. Remaining cost: skinned humanoids + Invector motor stack.

UNSTAGED WIP on disk (NOT in 273cf84e2): ANGRY MESH rock mat instancing fixes, grenade/building/player/misc _Project files, deleted legacy Pioneer scenes, untracked FORGE3D/, screenshots, terrain auto-upgrade layers. Do not commit noise without explicit paths.

PRIMARY CODE: Assets/_Project/Scripts/Combat/Turrets/*, Editor/Combat/DMTurret*.cs, DMForge3DHitBridge (earlier commits), EnemyAiController.* (turret threat), HumanoidPerformanceController, SceneComponentCache.

NEXT LIKELY TASKS: Play-test Alarm + tracking limits on placed scene turrets (run Ensure alarm on selected/scene turrets if instances predate prefab batch); FPS in build vs editor; optional Genesis Studio registration for alarm colors (profile fields exist on DM_TurretProfile); do NOT regress seated turret, LOS, faction damage, or tracking persistence.

Index: Assets/_Project/Documentation/INDEX.md §2 combat. Combat doc: Assets/_Project/Features/Combat/Documentation/Dark_Matter_Combat_System.md (partial update in 273cf84e2).
```

---

## 2. What shipped in `273cf84e2`

| Area | Summary |
|------|---------|
| **Runtime authority** | `DMTurretTrackingAuthority`, `DMTurretVendorFxAuthority`, expanded `DMTurretInstanceAuthority`; `DMTurretRuntime` pushes to F3DTurret/F3DFXController; brain reads runtime for scan/LOS/rest/aim accept |
| **Inspector** | `DMTurretRuntimeEditor` + `DMTurretInspectorSectionsGui`, driven-component editors for F3D/Brain/Health/Interact |
| **Tracking persistence** | `DMTurretEditModeTrackingHooks` + `PlayModeEditPersistence` callback; wire uses Apply not Capture |
| **Vision cones** | `DMTurretVisionConeGizmo` — edit mode only; F3D-accurate limits |
| **Alarm** | `DMTurretAlarmLight`, `DMTurretAlarmEnsure`, `Alarm.mat`, all combat turret prefabs updated |
| **Perf** | `DMTurretBrain`/`ForgeFxCleanup` gate fix; dormancy; enemy AI/motor/cache/hitch trims |
| **Scene** | `Dark Matter Genesis v1.6.6.unity` + prefabs |

Earlier on branch (already remote): turret UITK menu, FORGE runtime/brain, Hot Cross seated blocks, etc. (`30da2c5d7`, `d7d8588a3`, …).

---

## 3. Menus (Combat → Turrets)

- Bake combat prefab (current template)  
- Sync all FORGE3D templates → Combat/Turrets  
- **Ensure alarm on all combat turret prefabs** (batch — already run once)  
- **Ensure alarm on selected/scene turrets** (for v1.6.6 instances)  
- Wire references on selected runtime (inspector buttons)

---

## 4. Verification checklist (Play)

1. **Tracking:** Edit limits on Ion Turret → override ON → Play → Stop; cones + F3D limits match authority.  
2. **Alarm:** Yellow scan → red track → green fire; blink accelerates with range; solid green while firing.  
3. **Seated:** Enter turret, vendor/game ammo, LOS, no wall pierce, player not aggro target.  
4. **Auto:** Enemy prefers player; reacts to player turret hits.  
5. **FPS:** Stats with no turret selected in Inspector; compare far-from-turret vs combat; optional Development Build.

---

## 5. Commit / push rules (Grok)

See `Grok_Commit_Push_Handoff.md`. After Anthony asks to commit: stage explicit `_Project` paths + `.meta`, run `DmUnityGitSafeguard.ps1 -BeforePush`, push `cursor/wip-clean-20260919`.

---

*End of handoff.*
