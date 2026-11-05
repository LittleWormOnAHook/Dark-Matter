# PR #60 — Windows incremental player build handoff (2026-11-05)

**Purpose:** Apply **only** [PR #60](https://github.com/LittleWormOnAHook/Dark-Matter/pull/60) (`dark-branches/door-shader-ui-fixes-4481` → `cursor/wip-clean-20260919`) onto an **already dirty** local Unity tree, then run the **same incremental** standalone build you use today into `A:\Dark Matter Mars`. This document is preparation only — no merge, no clean build, no Project Settings / build-setting changes, no leak/profiling flags.

**Cloud agent status:** Static review + small lifecycle fix on this branch. **No** Unity compile, Play Mode, or standalone build was run in the cloud environment.

---

## Already on base ref (do not redo)

Nov 4 build-only work is on **`cursor/wip-clean-20260919`** tip (`a91a3b299`): scope arming / `ScopeDiag`, BFX blood driver, jetpack interpolation, scene-switch NRE fixes. PR #60 stacks **on top** of that ref. Your eventual player build must still include those fixes — this PR does not replace them.

---

## Exact files changed in PR #60

Apply these paths only (plus this handoff doc after you pull the branch tip):

| Area | Path |
|------|------|
| Doors | `Assets/_Project/Scripts/Environment/Doors/DMDoorDoubleSlideAudioCompat.cs` |
| Doors | `Assets/_Project/Scripts/Environment/Doors/DMSlidingDoorController.cs` |
| Input | `Assets/_Project/Scripts/Player/DMInputSchemeRouter.cs` |
| Input | `Assets/_Project/Scripts/Player/DMInputSchemePromotionRules.cs` |
| Input / UI stack | `Assets/_Project/Scripts/UI/DMSceneStack.cs` |
| Input / UI stack | `Assets/_Project/Scripts/UI/MainMenuController.cs` |
| Input / UI stack | `Assets/_Project/Scripts/UI/DMUiGamepadNavigation.cs` |
| uGUI menus | `Assets/_Project/Scripts/UI/QuestGiverDialogUI.cs`, `QuoraShelterMenuUI.cs`, `PptDirectionsMenuUI.cs`, `BuildingControlPanelUI.Layout.cs` |
| Terrain shader | `Assets/_Project/Rendering/DMShaders/DM_TerrainParallax.shader` |
| Terrain shader | `Assets/_Project/Rendering/DMShaders/DM_TerrainParallax_Splatmap.hlsl` |
| Terrain runtime | `Assets/_Project/Scripts/Rendering/DMTerrainParallaxHeightArrayBinder.cs` |
| Terrain editor | `Assets/_Project/Editor/Rendering/DMTerrainParallaxGUI.cs` |
| UI | `Assets/UI Toolkit/Runtime/DMUiRuntimeAssetRegistry.cs` |
| Sky / RI | `Assets/_Project/Scripts/World/Sky/DMRealtimeIndirect.cs` |
| Tests | `Assets/_Project/Features/Validation/Tests/DMUiRuntimeAssetRegistryTests.cs` |
| Tests | `Assets/_Project/Features/Validation/Tests/DarkMatterStackValidationTests.cs` |
| Docs | `CHANGELOG_20261021.txt`, `Assets/_Project/Documentation/DevManual/02-World.md`, `09-Controls.md`, `10-UI.md`, `13-Rendering.md` |
| Handoff | `Assets/_Project/Documentation/Engineering/PR60_Windows_Incremental_Build_Handoff_2026-11-05.md` |

**`.meta` files already in the PR (do not edit or regenerate):**

- `Assets/_Project/Scripts/Environment/Doors/DMDoorDoubleSlideAudioCompat.cs.meta`
- `Assets/_Project/Scripts/Rendering/DMTerrainParallaxHeightArrayBinder.cs.meta`
- `Assets/_Project/Features/Validation/Tests/DMUiRuntimeAssetRegistryTests.cs.meta`

No `.unity`, `.prefab`, `.asset`, or `.mat` changes in this PR.

---

## Prerequisites (local)

1. **Unity project root:** your live tree (e.g. `A:\Dark Matter Genesis`).
2. **Bring in PR #60 sources** using your normal workflow for a dirty tree (e.g. fetch branch and copy/reconcile the file list above, or merge/cherry-pick **only if** you accept resolving conflicts locally). This handoff intentionally avoids `reset`, `checkout`, `restore`, `stash`, `clean`, `pull`, or `merge` commands over uncommitted work — choose a method that **preserves** local dirty files.
3. **Conflict review:** if any path above overlaps local edits, resolve manually before compile.
4. **Auto Refresh:** if off, refresh scripts (Ctrl+R or MCP `refresh_unity` with `mode: if_dirty`, not `force`).
5. **Compile gate:** wait for domain reload; Unity Console **errors = 0** before building. Warnings alone do not block unless you decide otherwise.
6. **Stop here** if Unity or your build pipeline insists on a **clean build**, bulk reimport, or **Project Settings / Player Settings / Graphics / Quality / Scripting Define** changes to pick up this PR. This slice is code + shader + docs only; it should not require those steps.

---

## Incremental build (unchanged options)

1. Open playable scene: **`Assets/Dark Matter Genesis v1.56.unity`** (or your current numbered Genesis scene if renamed).
2. Use the **same** Build Profile / Build Settings you used for the last Mars drop (platform, scenes list, development vs release, scripting backend, etc.). **Do not** enable memory profiler, leak detection, or extra diagnostic defines for this handoff.
3. Output folder: **`A:\Dark Matter Mars`** — **incremental / overwrite**, not “Clean Build” or delete-output-folder unless your pipeline already requires it (if it does, **stop** per § Prerequisites).
4. Build from the Editor (or your existing batch script). Only a human/local CI run completes deployment.

---

## Focused verification plan (after build)

Run the new exe from `A:\Dark Matter Mars` and spot-check:

| Area | What to check |
|------|----------------|
| **Doors** | Walk through Creepy Cat / legacy `DoorDoubleSlide` triggers — no `NullReferenceException` spam in `Player.log` on enter/exit. |
| **Terrain** | Mars terrain using `DM/TerrainParallax` renders splats/parallax; no “texture limit” / pink shader; height detail sane on layered areas. |
| **UI startup** | Main menu / first UI Toolkit screen loads without “missing script” on registry-owned assets. |
| **EventSystem (Editor)** | After Play, `Editor.log` has **no** repeating “There are 2 event systems in the scene” / “There can be only one active Event System.” (2026-11-05d boot ownership). |
| **Pad at launch** | Controller connected before start → gameplay uses Gamepad scheme (movement/aim per project bindings). |
| **KBM ↔ pad** | Mid-session: press key/mouse button → Keyboard&Mouse scheme; move stick past deadzone (~0.28) → Gamepad scheme; no stuck mute on Invector gate. |
| **Hotplug** | Connect/disconnect pad after main menu and after in-world load. |
| **Mars / IO switch** | Use your world/scene stack path (Mars ↔ IO reload) — scheme still switches; no input dead after reload (`DMSceneStack` refresh path). |

**Still not claimed fixed:** shutdown **ComputeBuffer / GraphicsBuffer / JobTempAlloc / Persistent NativeArray** leaks from Nov 5 logs. Repro those with **stack traces enabled** in a separate profiling pass; this PR only adds runtime `Texture2DArray` cleanup on **`Application.quitting`** and domain reload (`SubsystemRegistration`), not full shutdown allocation audit.

---

## Static review notes (for reviewers)

- **Input pairing:** `InputUser.PerformPairingWithDevice(device, playerInput.user)` before `SwitchCurrentControlScheme` — matches Input System **1.19.0**; failures are caught and logged as warnings.
- **Shader legacy heights:** `_DMHeightMap0..7` removed from shader Properties; materials may still hold orphan assignments — `DMTerrainParallaxHeightArrayBinder` reads them via `GetTexture` and packs `_DMHeightMapArray` at runtime.
- **DMRealtimeIndirect:** edit-mode disable uses deferred `Kill()` to avoid Editor destroy-on-disable asserts.
- **EventSystem boot:** `RefreshEventSystemOwnership()` at play boot + scene loads; menu helpers no longer spawn duplicate EventSystems.

---

## Blocked until you act

| Step | Owner |
|------|--------|
| Apply file set to dirty Windows tree | Local |
| Unity compile + console clean | Local Editor |
| Incremental build → `A:\Dark Matter Mars` | Local (Build button / existing script |
| PR merge | Optional; not required to test from branch |
| Shutdown leak root-cause with stacks | Follow-up profiling session |

**PR:** https://github.com/LittleWormOnAHook/Dark-Matter/pull/60  
**Branch:** `dark-branches/door-shader-ui-fixes-4481`
