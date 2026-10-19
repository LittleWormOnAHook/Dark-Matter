# Dark Matter: Genesis — FPS Baseline (Oct 18, 2026)

> **Purpose:** the reference FPS / frame-time numbers to compare future performance work against.
> **Scene:** `Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity` · **Unity** 6000.4.11f1 HDRP · branch `cursor/wip-clean-20260919` @ `273cf84e2` + uncommitted perf round A–C (backups `*.pre20261018a`).
> **Baseline = the "After" column.** All numbers are **Editor Play mode** (Editor overhead included), not a player build.

## 1. Settings

| Setting | Value |
|---|---|
| Quality tier | **High** (index 3), no tier switching during the run |
| Speed | normal (`timeScale` 1), player idle at spawn, enemies alive but not engaged (no combat) |
| Run In Background | turned on for the session only (Editor was unfocused), restored to off afterwards |
| Profiler | standard Profiler, **no deep profiling**; hierarchy merged by name, depth ≤ 7, last 121 frames |
| Counters | ProfilerRecorder counters sampled ~9×/s for 45 s via MCP for Unity (`manage_profiler get_counters` + `get_frame_timing`) |
| Machine | Anthony's PC ("Teabagger") |

## 2. Before / after (Oct 18, 2026 perf round A–C)

| Metric | Before (22:49–23:19 EDT) | **After = baseline** (23:38–23:42 EDT) |
|---|---|---|
| FPS (mean / median) | ~14–15 | **~41 / ~46** (p95 ≈ 24 FPS) |
| CPU frame time | 66–73 ms mean, 65 median, 84–103 p95, 491 worst | **24.1 mean, 21.5 median, 41 p95, 103 worst** |
| Main thread | 66–72 ms | **24.6 ms** (21.8 median) |
| PlayerLoop | ~56–58 ms | **~20–22 ms** |
| Script Update | ~20 ms | **2.0 ms** |
| Script LateUpdate | 19–21 ms | **5.5–6.8 ms** |
| Script FixedUpdate | 2.3–3.8 ms | 1.8–2.1 ms |
| Render thread / GPU | ~4 / ~3 ms | 4.5 / 3.9 ms |
| GC allocated per frame | 120–230 KB (~1,500–1,800 allocs) | **84 KB (~1,100 allocs)** |
| SetPass / shadow casters / triangles | 165–212 / 143–236 / 0.9–1.6 M | 152 / 388 / 1.8 M (different camera view — render side heavier but still not limiting) |

**What changed (A–C):** `IsGameplayInputLockedByUi()` evaluated once per frame + `SettingsPanelController` registry (no scene search); missing-object scene searches in ActiveQuest / PilotCluster / Minimap / WorldChrome retry at most once a second; `DMWorldMapCalibrationSceneSync` Play snapshot throttled (profile field `editorPlaySnapshotIntervalSeconds`, default 1 s).

### Per-script (ms/frame, Profiler, before → after)

| Script | Before | After |
|---|---|---|
| DMUiToolkitInputHost.Update | 6.6 | 0.08 |
| PioneerShooterMeleeInput (Update + LateUpdate + FixedUpdate) | 7.5 | ~0.4 |
| DMUiToolkitWorldChrome.LateUpdate | 6.1 | 1.26 |
| DMUiToolkitActiveQuest.LateUpdate | 4.0 | 0.16 |
| DMPlayerQuickStep / DMIGrenadeCookController ×4 / DMJetpackInputBridge | ~6.5 combined | < 0.1 each |
| DMUiToolkitPilotCluster.LateUpdate | 4.2 | 2.5 |
| DMUiToolkitMinimap.LateUpdate | 3.2 | 1.8 |

## 3. Top remaining costs (after)

1. **Editor overhead** — EditorLoop ~12 ms + GameView.Render ~8.6 ms (gone in a build; close Hierarchy/Scene views while playing).
2. **HDRP camera render** ~4.5–6.5 ms main thread (HDRP visuals intentionally untouched).
3. **DMUiToolkitPilotCluster** 2.4 ms and **DMUiToolkitMinimap** 1.7 ms (own refresh work).
4. **DMUiToolkitWorldChrome** 1.2 ms (per-frame dot collection; exclusive pickup stem is per-frame by design).
5. **DMSpawnPhysicsStabilizer** 0.9 ms (~10 calls/frame, runs for the enemy's whole life), PlayerController 0.75 ms.
6. Profiler.FlushMemoryCounters ~5.8 ms is a **measurement artifact** (profiler on), not game cost.

Follow-up round (Oct 18 b, `*.pre20261018b`) targeted 3–5 plus the QualitySettings / FX_Snow asset rewrites; it was compiled clean but **not re-measured** — measure against this table next time.

## 4. How to re-measure (same method)

1. Console at 0 errors / 0 warnings; tier High; **never enable deep profiling**.
2. Enter Play in v1.6.6, wait ~20 s for the session to start (GameSession.HasStarted, timeScale 1).
3. If the Editor is unfocused, set `Application.runInBackground = true` for the session only; restore it (and PlayerSettings) to false before exiting Play.
4. Sample the counters above for 30–60 s; then enable the standard Profiler (deepProfiling false) for ~12 s and aggregate the last 121 frames.
5. Exit Play, do not save the scene; check `git status` for ProjectSettings / prefab rewrites.
