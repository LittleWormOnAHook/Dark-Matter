# HDRP Settings UI + Runtime Plan — Dark Matter: Genesis

Implementation plan only. Do not treat this as shipped behavior until the phases below land.

**Authority:** Align with `HDRP_Migration_Plan.md`, `System_Requirements.md`, GDD platforms (PC + consoles; WebGL retired), and Phase 6 default **`Genesis_HDRP_High`** (Quality index 3 / `PlatformGraphicsProfile.HighTierIndex`).

**Related code today:**

| Area | Path |
|------|------|
| Settings UI | `Assets/_Project/Scripts/UI/SettingsPanelController.cs` |
| Persist / apply | `Assets/_Project/Scripts/Core/GameSettings.cs` |
| Tier defaults | `Assets/_Project/Scripts/Core/PlatformGraphicsProfile.cs` |
| Boot / LOD overrides | `Assets/_Project/Scripts/Core/PlatformGraphicsBootstrap.cs` |
| Advisories | `Assets/_Project/Scripts/Core/GraphicsCapabilityAdvisor.cs` |
| Post (still URP-shaped) | `Assets/_Project/Scripts/Core/PostProcessingController.cs` |
| HDRP tier assets | `Assets/Settings/HDRP/Genesis_HDRP_{Performance,Balanced,Quality,High,Ultra}.asset` |
| Quality wiring | `ProjectSettings/QualitySettings.asset` (`m_CurrentQuality: 3` = High) |

---

## 1. Current state inventory

### Settings panel (`SettingsPanelController`)

Built at runtime via `MenuUiBuilder` inside a scrollable modal. Sections today:

| Section | Controls | Wired to |
|---------|----------|----------|
| **Audio** | Master / Music / SFX sliders | `GameSettings` + `GameAudioManager` (live) |
| **Graphics** | Quality dropdown | `QualitySettings.names` → `SetQualityLevel` + tier overrides |
| | Resolution dropdown | `Screen.resolutions` → `SetResolutionIndex` |
| | Fullscreen toggle | `GameSettings.Fullscreen` |
| | VSync toggle | `QualitySettings.vSyncCount` |
| | Ray Tracing toggle | **Persists only** — no pipeline / volume apply |
| | Post Processing toggle | Master on/off via `PostProcessingController` |
| | Advisory label | `GameSettings.GetGraphicsAdvisorySummary()` |
| **Gameplay** | Minimap toggle | `MapUI.ApplyMinimapEnabled` |
| Footer | Apply → `GameSettings.Save()` + close | |

**Missing vs requested:** texture quality, shadow quality, anti-aliasing, draw distance, AO, HDR, FOV, per-effect post (bloom / motion blur / DoF), granular raytrace options, DX11 vs DX12.

### `GameSettings` persistence

PlayerPrefs keys already: master/music/sfx volume, postProcessing, fullscreen, vsync, quality, resolutionIndex, minimap, rayTracing.

- Quality: applied on Load with `SetQualityLevel(..., applyExpensiveChanges: true)`.
- Ray tracing: stored + advisory only; **does not** flip HDRP `supportRayTracing` or volume RT overrides.
- No FOV, texture/shadow/AA/AO/HDR/draw-distance, or API preference keys.

### Platform / HDRP alignment already in place

- Five tiers: **Performance → Balanced → Quality → High → Ultra** (`PlatformGraphicsProfile` indices 0–4).
- Default PC/console quality index = **High** (`DefaultQualityIndex` / `PcQualityIndex`).
- Editor play mode forces High via `ForceEditorPlayModePcProfile()`.
- Bootstrap overrides LOD bias, max LOD, and `QualitySettings.shadowDistance` per tier.
- `GraphicsCapabilityAdvisor`: non-blocking warnings for Ultra, RT, RAM/VRAM vs `System_Requirements.md`.
- Quality assets each reference a `Genesis_HDRP_*` pipeline asset. **Only Ultra** currently has `supportRayTracing: 1`; High/others are `0`.

### Gaps / debt

- `PostProcessingController` still imports `UnityEngine.Rendering.Universal` and toggles `UniversalAdditionalCameraData.renderPostProcessing` — **must be HDRP-ported** before fine post controls work.
- Global volume uses URP-era `SampleSceneProfile`; need Genesis HDRP volume profile(s) + runtime override volumes.
- FOV: gameplay cameras use hard-coded / aim-lerp defaults (`PlayerController` `_defaultFov = 60`); no settings binding.
- Compass HUD “FOV” is UI arc only — not camera FOV.
- Windows Player Settings currently list **D3D11 then D3D12** in build target APIs; RT needs DX12 (or Vulkan/Metal where applicable). No in-game API picker yet.
- Resolution list does not dedupe refresh rates; Apply does not re-apply quality/RT after Save beyond volume weight.

---

## 2. Target UX (settings panel sections)

Keep Shift / `DarkMatterGenesisUiPalette` chrome. Expand scroll content; keep Apply + top-right Back. **Never disable** PC/macOS options for hardware — advisories only (`System_Requirements.md`).

### A. Audio *(unchanged)*

Master, Music, SFX.

### B. Display

| Control | Type | Notes |
|---------|------|--------|
| Resolution | Dropdown | Prefer unique `WxH` (highest refresh) or `WxH @ Hz` if we keep multi-rate |
| Fullscreen | Toggle | Existing |
| VSync | Toggle | Existing |
| HDR | Toggle | Display HDR output where supported; advisory if unsupported |
| Graphics API | Dropdown | **Windows PC only:** Auto / DirectX 11 / DirectX 12. Restart required. Hidden on console/macOS builds |

### C. Graphics quality

| Control | Type | Notes |
|---------|------|--------|
| Graphics pipeline (Quality) | Dropdown | Labels = `QualitySettings.names`: Performance, Balanced, Quality, High, Ultra. Default High |
| Texture quality | Dropdown | Full / Half / Quarter / Eighth → mipmap limit |
| Shadow quality | Dropdown | Off / Low / Medium / High / Ultra → maps to HDRP shadow + cascade budget (tier-relative) |
| Anti-aliasing | Dropdown | Off / FXAA / TAA / MSAA 2x/4x (MSAA only where HDRP asset allows; else advisory) |
| Draw distance | Slider or dropdown | Scales camera far clip and/or LOD bias / shadow distance multiplier |
| Ambient occlusion | Toggle or Off/Low/High | Volume `ScreenSpaceAmbientOcclusion` (and RT AO if RT on) |
| Ray tracing | Master toggle | Always available on PC/macOS; default **off**; console hide or force off at ship |
| Ray tracing detail *(optional sub-row)* | Dropdown | Off / Reflections / Soft shadows / Full (subset of HDRP RT features we certify) |

Show `GraphicsCapabilityAdvisor` summary under this block (multi-line, Soft Beige-Gray). Optional one-time confirm when enabling **Ultra + RT** on weak VRAM (still non-blocking).

### D. Camera

| Control | Type | Notes |
|---------|------|--------|
| Field of view | Slider | e.g. 60–90°, default 60. Applies to exploration camera base FOV; aim/optics multiply from this base |

### E. Post-processing

| Control | Type | Notes |
|---------|------|--------|
| Post processing (master) | Toggle | Existing — gates all volume weight |
| Bloom | Toggle | Independent override when master on |
| Motion blur | Toggle | Default off for tactical readability |
| Depth of field | Toggle | Default off or Low; avoid gameplay blur unless cinematic |

### F. Gameplay *(unchanged)*

Minimap.

### Platform visibility

| Platform | Behavior |
|----------|----------|
| Windows PC | Full panel including Graphics API |
| macOS | Full graphics except DX dropdown; Metal/API advisories later |
| Consoles | Quality + safe subset; **no RT** at ship; no DX picker; defaults tuned for 60 FPS |
| WebGL | Retired — no UI |

---

## 3. Data model (extend `GameSettings`)

Add PlayerPrefs-backed properties (defaults match High tier, RT off, post on):

```text
settings.textureQuality      int   // 0=Full … 3=Eighth; default 0
settings.shadowQuality       int   // 0–4; default matches High
settings.antiAliasing        int   // enum; default TAA on High
settings.drawDistance        float // 0.5–1.5 multiplier; default 1
settings.ambientOcclusion    int   // 0=Off,1=Low,2=High; default 1
settings.hdrOutput           int   // 0/1; default 1 if Screen.hdrSupported else 0
settings.fieldOfView         float // default 60
settings.bloom               int   // 0/1; default 1
settings.motionBlur          int   // 0/1; default 0
settings.depthOfField        int   // 0/1; default 0
settings.rayTracingMode      int   // 0=Off master mirrors, or detail enum when master on
settings.graphicsApi         int   // 0=Auto,1=DX11,2=DX12 (Windows); applied via launcher prefs / restart
```

Retain existing keys. `RayTracingEnabled` remains the master bool; `rayTracingMode` refines features when master is on.

**Setters:** update memory + call a single `GraphicsSettingsApplier.ApplyAll()` (new) rather than scattering HDRP calls in the UI.

**Load order:** read prefs → set quality level → apply tier bootstrap → apply fine overrides → apply post/FOV → applier notes pending API restart.

---

## 4. Runtime apply layer

Centralize in a new `Project.Core.GraphicsSettingsApplier` (or expand bootstrap). Map each control to the correct Unity surface:

### A. `QualitySettings` / Screen

| Setting | Apply path |
|---------|------------|
| Graphics pipeline | `QualitySettings.SetQualityLevel(i, true)` → swaps `customRenderPipeline` to `Genesis_HDRP_*` |
| VSync | `QualitySettings.vSyncCount` |
| Resolution / fullscreen | `Screen.SetResolution` / `Screen.fullScreen` |
| Texture quality | `QualitySettings.globalTextureMipmapLimit` |
| Draw distance (coarse) | Multiply `QualitySettings.lodBias` / `shadowDistance` from tier baseline in `PlatformGraphicsBootstrap` |
| HDR (display) | `HDROutputSettings` / `Screen.hdrOutputSettings` where available; else advisory |

### B. `HDRenderPipelineAsset` / HDRP frame settings

Prefer **quality-level assets** as the base (already wired). Fine controls that HDRP stores on the asset should use:

1. **Preferred:** runtime overrides via `HDRenderPipeline.currentPipeline` / frame settings overrides on `HDAdditionalCameraData` where API allows without cloning assets.
2. **Fallback:** maintain lightweight runtime-cloned asset or ScriptableObject override tables per tier (avoid mutating disk assets at runtime).

| Setting | Apply path |
|---------|------------|
| Shadow quality | Shadow resolution / cascade / distance on active HDRP asset or camera frame settings |
| Anti-aliasing | HDRP AA mode (TAA/FXAA/MSAA) on camera / pipeline |
| Ray tracing master | Enable RT frame settings + ensure active pipeline has RT support (High currently `supportRayTracing: 0` — see Phase B asset work) |
| Ray tracing detail | Toggle RT reflections / shadows / AO on volume or frame settings |

**Asset prep (required):** Enable `supportRayTracing` on High (and optionally Quality) pipeline assets used when RT is on, **or** swap to Ultra/RT variant asset when RT enabled while keeping other High settings — product decision in Phase B. Default stay on **Genesis_HDRP_High** when RT is off.

### C. Volume profile overrides

Add a DontDestroyOnLoad **settings override Volume** (high priority) with runtime-generated profile, or clone Genesis HDRP post profile:

| Setting | HDRP override |
|---------|----------------|
| Post master | Volume `weight` / enabled |
| Bloom | `Bloom.active` |
| Motion blur | `MotionBlur.active` |
| Depth of field | `DepthOfField.active` |
| AO | `ScreenSpaceAmbientOcclusion` (+ RT AO if certified) |

Port `PostProcessingController` off URP: use `HDAdditionalCameraData` / HDRP volume stack; drop `UniversalAdditionalCameraData`.

### D. Camera FOV

| Setting | Apply path |
|---------|------------|
| FOV | Set base FOV on player / main exploration camera; `PlayerController` reads `GameSettings.FieldOfView` as `_defaultFov` instead of hard-coded 60; optics/aim multipliers unchanged |

Do **not** change compass HUD FOV constant unless product asks.

### E. Apply timing

- Live where cheap (audio, FOV, post toggles, VSync).
- Expensive (quality tier, shadows, RT): apply on control change **and** on Apply/Save (current pattern).
- Graphics API: save preference + show “Restart required”; do not pretend it hot-swaps.

---

## 5. DX11 / DX12 / RT feasibility

### Facts

- Windows Player Settings currently include **Direct3D11 and Direct3D12**.
- HDRP hardware ray tracing on Windows requires **DX12** (DXR). DX11 → SSR/shadow fallback only.
- Unity **cannot reliably hot-switch** graphics API mid-process; Player Settings / command-line / restart is required.
- macOS: Metal path; no DX control. Consoles: platform API fixed; RT off at ship.
- Policy: keep RT toggle selectable; if API is DX11 or GPU lacks RT, advisor warns and pipeline falls back — **do not lock toggle**.

### Recommended UX

1. **Graphics API** dropdown (Windows): Auto / DirectX 11 / DirectX 12.
2. On change: persist `settings.graphicsApi` + show restart dialog (“Apply on next launch”).
3. Boot: early script or custom launcher args (`-force-d3d12` / `-force-d3d11`) or documented Player Settings dual-list with preferred API written via Editor build script / `PlayerSettings` only at build time — **runtime preference** should use Unity’s supported restart/command-line pattern for standalone.
4. If RT enabled while effective API is DX11: advisory — “Ray tracing needs DirectX 12. Switch API and restart, or keep RT on for next DX12 session.” Still allow selection.
5. Expand `GraphicsCapabilityAdvisor`:
   - RT + DX11
   - RT + `graphicsMemorySize < 6144`
   - RT + Ultra
   - HDR requested but unsupported
   - (Future) HDD + High RT

### Restart requirements matrix

| Change | Restart? |
|--------|----------|
| Quality tier, resolution, VSync, FOV, post toggles | No |
| Texture / shadow / AA / AO / draw distance | Usually no (may hitch) |
| Ray tracing on/off | Prefer no hitch; may need pipeline reload hitch |
| DX11 ↔ DX12 | **Yes** |

---

## 6. Implementation phases (file touch list)

### Phase A — HDRP post foundation *(blocks fine post controls)*

- Rewrite `PostProcessingController.cs` for HDRP volumes / `HDAdditionalCameraData`.
- Add Genesis HDRP post/settings volume assets under `Assets/Settings/HDRP/` (or `_Project/Settings`).
- Wire main menu bootstrap (`MainMenuController.EnsurePostProcessingController`) to HDRP path.
- Smoke: master Post toggle works in `Genesis_HDRP_Test` and main scene after Phase 6.

### Phase B — Pipeline RT readiness

- Decide RT strategy: enable `supportRayTracing` on High (+ Quality?) vs RT-on asset swap.
- Update `Assets/Settings/HDRP/Genesis_HDRP_*.asset` accordingly; keep **default quality High, RT off**.
- Implement `GameSettings.SetRayTracingEnabled` → `GraphicsSettingsApplier` frame settings / volume RT.
- Extend `GraphicsCapabilityAdvisor` for RT + API + VRAM cases.

### Phase C — Data model + applier

- Extend `GameSettings.cs` (keys, load/save, setters).
- Add `GraphicsSettingsApplier.cs` (new).
- Extend `PlatformGraphicsBootstrap.ApplyTierOverrides` to accept draw-distance / shadow multipliers without fighting user overrides.
- Optionally thin `PlatformGraphicsProfile` helpers for enum→value maps.

### Phase D — Settings UI expansion

- Expand `SettingsPanelController.cs` sections (Display / Graphics / Camera / Post).
- Extend `MenuUiBuilder.cs` if enum dropdowns / labeled sliders need helpers.
- Platform `#if` for Windows API row; console RT hide.
- Sync + Apply + advisory refresh for all new controls.

### Phase E — FOV + camera consumers

- `PlayerController.cs` (and any shared camera rig) read `GameSettings.FieldOfView`.
- Ensure hovercraft / optics treat settings FOV as base where appropriate.

### Phase F — Graphics API preference

- Persist preference; restart prompt UI.
- Standalone boot hook or build documentation for `-force-d3d11` / `-force-d3d12`.
- Advisor linkage; verify RT only active under DX12.

### Phase G — Docs + polish

- Update `HDRP_Migration_Plan.md` “Settings integration” from planned → implemented checklist.
- Cross-link `System_Requirements.md`.
- Console default pass notes (60 FPS, no RT).

**Suggested order:** A → B → C → D → E → F → G.

---

## 7. Test plan

### Functional (Editor + Windows standalone)

1. Fresh PlayerPrefs: quality **High**, RT **off**, post **on**, FOV **60**.
2. Cycle all five quality tiers; confirm active RP asset name / visual change; no pink materials.
3. Resolution + fullscreen + VSync round-trip after restart of play mode.
4. Texture / shadow / AA / draw distance / AO each visibly affect frame (use HDRP debug views where useful).
5. Post master off → bloom/MB/DoF ignored; master on → per-toggles work independently.
6. FOV slider moves exploration camera; aim/optics still scale from new base.
7. RT on: advisory appears on low VRAM; no hard disable; fallback lighting if unsupported.
8. DX12 selected + restart → `SystemInfo.graphicsDeviceType == Direct3D12`; RT can engage when hardware allows.
9. DX11 selected + RT on → advisory; no crash.
10. Apply/Save → quit play → enter play → all prefs restored.

### Platform

- macOS: no DX row; tiers + RT advisories; Metal smoke.
- Console (when available): RT hidden/off; default tier hits 60 FPS target guidance.
- Confirm WebGL not offered.

### Regression

- Audio sliders and minimap unchanged.
- Editor play still forces High unless product changes that rule.
- Scanner / custom passes still function with post master on/off.
- No Unity console **Errors** after script edits (wait for compile/domain reload).

---

## 8. Out of scope / risks

### Out of scope (this plan)

- Full cinematic HDR grading / look-dev (Migration Phase 7).
- Console RT certification.
- Re-enabling WebGL.
- URP removal / deleting legacy RP assets (Migration Phase 9).
- Upscaling (DLSS/FSR/XeSS) — defer unless product asks.
- Changing GDD economy/UI palette.
- Auto-detect “recommended settings” that **forces** a tier (advisories only).

### Risks

| Risk | Mitigation |
|------|------------|
| Mutating HDRP assets at runtime dirtying project | Use override volumes / camera frame settings / clones |
| High asset lacks `supportRayTracing` while UI promises RT | Phase B asset or swap strategy before shipping RT apply |
| `PostProcessingController` URP compile/runtime break on HDRP | Phase A first |
| DX API switch unreliable mid-session | Restart-only UX; command-line force flags |
| Too many settings overwhelm modal | Grouped sections + scroll; sensible defaults; advanced RT detail collapsed |
| Draw distance fighting humanoid cull budgets | Apply multiplier through `PlatformGraphicsProfile` helpers |
| Resolution dropdown spam (every refresh rate) | Deduplicate or show `@ Hz` explicitly |
| Editor Force High vs testing other tiers | Temporary editor override or disable force when validating UI |

---

## References

- `Documentation/Architecture/HDRP_Migration_Plan.md`
- `Documentation/System_Requirements.md`
- `ProjectSettings/QualitySettings.asset` (5 tiers → `Genesis_HDRP_*`)
- Phase 6 commit context: project default **HDRP High**
)
