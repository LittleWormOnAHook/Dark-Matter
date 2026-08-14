# HDRP Migration Plan — Dark Matter: Genesis

Locked product and technical decisions for the URP → HDRP conversion. Update this file when scope changes.

## Product identity

| Item | Value |
|------|--------|
| Ship title | **Dark Matter: Genesis** (Genesis = first shipped game) |
| Retired names | Pioneer, Survival Pioneer |
| Main scene | `Assets/DarkMatter_Genesis v1.56.unity` |
| Unity `productName` | Dark Matter: Genesis |

## Scope

- **Full project** conversion in phases (materials, scenes, prefabs, third-party packs, code).
- **WebGL retired** — remove build profiles and Web GL quality tier naming.
- **Visual target:** cinematic HDR (volumetric fog, physical lights, exposure, post).
- **Console:** **60 FPS minimum** on default quality tier per platform.
- **macOS:** ship alongside Windows PC; same quality tiers and optional RT rules (SSD required only when RT is enabled).

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
- Player can enable RT in settings when hardware supports it.
- When unsupported or below minimum spec: toggle disabled or auto-off with clear UI feedback.
- Fallback when RT off: SSR, shadow maps, and standard HDRP lighting (no RT-only content lock).

### PC hardware thresholds

Full store/FAQ copy and RT gate thresholds live in **`Documentation/System_Requirements.md`**.

| Profile | Target |
|---------|--------|
| **Minimum** | 1080p, Low RT, 30–60 FPS — RTX 2060 / RTX 3050 / RX 6600 XT, 6–8 GB VRAM, Core i5 (8th Gen) / Ryzen 5 (2nd Gen), 8–16 GB RAM; **SSD not required** unless RT is on |
| **Recommended** | 1440p+, High RT, 60+ FPS — RTX 4070 Ti / 4080+ or RX 7800 XT / 7900 XTX, 12–16 GB+ VRAM, Core i7 (12th Gen+) / Ryzen 7 (5000/7000+), 16–32 GB RAM; **SSD required for RT**; NVMe for High RT |

Use **Minimum** row for RT toggle eligibility; **Recommended** row for “High RT” preset without warnings.

### Console

- **No ray tracing** at ship unless a future platform pass explicitly adds it with 60 FPS proof.
- Do not gate gameplay or certification on RT on consoles.

## Settings integration (planned)

- Graphics quality dropdown → five tiers.
- Ray tracing toggle (PC and macOS; gated on GPU/VRAM/CPU/RAM + **SSD when RT on**).
- Volumetric fog / shadow quality sub-settings within tier bands.
- Existing post-processing, resolution, VSync, fullscreen toggles retained.

## Phase overview

1. Foundation — HDRP package, wizard, WebGL retirement, five HDRP assets.
2. Pipeline — quality wiring, bootstrap, settings stubs.
3. Code — cameras, post, scanner custom pass, dissolve shaders, `Shader.Find` factories.
4. Materials — bulk URP → HDRP conversion (~1,600 materials).
5. Third-party audit — per-pack pink-material sign-off.
6. Global switch — `DarkMatter_Genesis v1.56` + lighting rebake.
7. Cinematic HDR tuning + optional RT path.
8. 60 FPS certification matrix (console, Windows PC, macOS).
9. URP removal, docs, rule updates.

## Naming (parallel track)

- New HDRP assets and scenes use **Genesis** / **DarkMatter_Genesis** naming.
- Mass `Pioneer*` code renames are gradual; avoid blocking HDRP critical path.

## References

- **`Documentation/System_Requirements.md`** — canonical PC minimum/recommended specs and RT gates
- GDD 5.0 — platforms PC + consoles, semi-low-poly Io
- `World_Engine_Disk_Status.md` — disk truth before claiming shipped features
- Working scene rule: `.cursor/rules/confirm-before-depot-restore.mdc`
