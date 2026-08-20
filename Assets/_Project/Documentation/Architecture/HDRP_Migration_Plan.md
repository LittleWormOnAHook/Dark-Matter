# HDRP Migration Plan — Dark Matter: Genesis

Locked product and technical decisions for the URP → HDRP conversion. Update this file when scope changes.

## Product identity

| Item | Value |
|------|--------|
| Ship title | **Dark Matter: Genesis** (Genesis = first shipped game) |
| Retired names | Pioneer, Survival Pioneer |
| Main scene | `Assets/Dark Matter Genesis v1.56.unity` |
| Unity `productName` | Dark Matter: Genesis |

## Scope

- **Full project** conversion in phases (materials, scenes, prefabs, third-party packs, code).
- **WebGL retired** — remove build profiles and Web GL quality tier naming.
- **Visual target:** cinematic HDR (volumetric fog, physical lights, exposure, post).
- **Console:** **60 FPS minimum** on default quality tier per platform.
- **macOS:** ship alongside Windows PC; same quality tiers and optional RT rules.

## Quality tiers (5)

Each tier uses its own `HDRenderPipelineAsset` plus tier volume profile.

| Index | Name | Role |
|-------|------|------|
| 0 | Performance | Series S floor, 60 FPS |
| 1 | Balanced | All consoles 60 FPS fallback |
| 2 | Quality | PS5 / Series X default |
| 3 | High | PC default |
| 4 | Ultra | PC max cinematic settings |

HDRP asset prefix: `Genesis_HDRP_*` under `Assets/Settings/HDRP/`.

## Ray tracing (optional shipped feature)

**Ray tracing is not required** on any PC tier or any console. It is a **shipped, player-toggleable feature** (settings UI alongside other graphics options).

### Runtime behavior

- Default: **RT off** for all platforms and quality tiers.
- **All graphics settings remain selectable** on PC/macOS — no hardware-based disable.
- Below Minimum/Recommended guidance: **non-blocking performance notices** only.
- If RT cannot execute on hardware, **SSR/shadow fallback** at pipeline level; do not remove the toggle.
- Fallback when RT off or unsupported: SSR, shadow maps, and standard HDRP lighting (no RT-only content lock).

### PC hardware guidance

Store/FAQ copy and performance advisories live in **`Documentation/System_Requirements.md`**.

| Profile | Testing / guidance target (not a lock) |
|---------|----------------------------------------|
| **Minimum** | 1080p, Low RT, 30–60 FPS — RTX 2060 / RTX 3050 / RX 6600 XT, 6–8 GB VRAM, Core i5 (8th Gen) / Ryzen 5 (2nd Gen), 8–16 GB RAM; HDD or SSD |
| **Recommended** | 1440p+, High RT, 60+ FPS — RTX 4070 Ti / 4080+ or RX 7800 XT / 7900 XTX, 12–16 GB+ VRAM, Core i7 (12th Gen+) / Ryzen 7 (5000/7000+), 16–32 GB RAM; SSD/NVMe recommended |

Use these rows for **defaults, store text, and advisory messages** — not for disabling settings.

### Console

- **No ray tracing** at ship unless a future platform pass explicitly adds it with 60 FPS proof.
- Do not gate gameplay or certification on RT on consoles.

## Settings integration (planned)

- Graphics quality dropdown → five tiers; **all tiers always selectable** on PC/macOS.
- Ray tracing toggle — **always available** on PC/macOS; advisories when guidance thresholds not met.
- Volumetric fog / shadow quality sub-settings — always available; advisories on weak hardware.
- `GraphicsCapabilityAdvisor` — compares hardware + active settings to Minimum/Recommended; returns warning strings only.
- Existing post-processing, resolution, VSync, fullscreen toggles retained.

### Unity editor steps (after pulling this branch)

1. Open the project and wait for HDRP package import + compile.
2. Run **`Tools/Dark Matter Genesis/HDRP/Phase 0/1 - Create Genesis HDRP Foundation`**.
3. Run **`Tools/Dark Matter Genesis/HDRP/Phase 1 - Create HDRP Test Scene`**.
4. Validate tiers in the test scene; keep **`Dark Matter Genesis v1.56`** on URP until Phase 6.
5. When ready: **`Tools/Dark Matter Genesis/HDRP/Phase 6 - Switch Global Pipeline To HDRP High`**.

## Phase overview

1. **Foundation (started)** — HDRP package, wizard/foundation menu, WebGL retirement, five HDRP assets via `Tools/Dark Matter Genesis/HDRP/Phase 0/1`.
2. Pipeline — quality wiring, bootstrap, settings stubs (**partially landed**).
3. Code — cameras, post, scanner custom pass, dissolve shaders, `Shader.Find` factories. **Custom dissolve/smoke/scanner dual-pipeline shaders landed; `ScannerHdrpCustomPass` stub ready.**
4. Materials — bulk URP → HDRP conversion (~1,600 materials). **`_Project` URP Lit/Unlit batch done; customs dual-targeted.**
5. Third-party audit — per-pack pink-material sign-off. **See `HDRP_Vendor_Material_Audit.md` + folder dry-run/apply menus.**
6. Global switch — `Dark Matter Genesis v1.56` + lighting rebake.
7. Cinematic HDR tuning + optional RT path.
8. 60 FPS certification matrix (console, Windows PC, macOS).
9. URP removal, docs, rule updates.

## Naming (parallel track)

- New HDRP assets and scenes use **Genesis** / **Dark Matter Genesis** naming.
- Mass `Pioneer*` code renames are gradual; avoid blocking HDRP critical path.

## References

- **`Documentation/System_Requirements.md`** — canonical guidance specs and performance-advisory policy (no hard gates)
- GDD 5.0 — platforms PC + consoles, semi-low-poly Io
- `World_Engine_Disk_Status.md` — disk truth before claiming shipped features
- Working scene rule: `.cursor/rules/confirm-before-depot-restore.mdc`
