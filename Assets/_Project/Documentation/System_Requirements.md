# Dark Matter: Genesis — System Requirements

Canonical system requirements for **store pages, FAQ, and performance expectations**. Update when certification targets change.

**Ship title:** Dark Matter: Genesis  
**Ship platforms:** **Windows PC**, **macOS**, PlayStation 5, Xbox (Series X|S)

---

## Policy: recommendations, not locks

All rows in the overview table are **recommendations** — not hard gates.

| Principle | Rule |
|-----------|------|
| **Player freedom** | On PC and macOS, users can select **any** graphics setting (quality tier, ray tracing, resolution, fog, shadows, post-processing, etc.). |
| **No disabled toggles** | Do **not** hide or disable settings based on detected hardware or storage. |
| **Notify, don’t block** | When choices may hurt performance or stability, show a **clear non-blocking notice** (settings helper text, one-time dialog, or banner). User can proceed. |
| **Store / FAQ copy** | Minimum and Recommended describe the experience we **target and certify** — not what we forbid. |

Console platforms still ship with **default quality tiers** tuned for 60 FPS; players may change tier where the platform UI allows, with the same advisory pattern when a choice exceeds cert targets.

---

## System Requirements Overview

Guidance for purchase and expectations — **not enforcement**.

| Component | Minimum (guidance: 1080p, Low RT, 30–60 FPS) | Recommended (guidance: 1440p+, High RT, 60+ FPS) |
|-----------|-----------------------------------------------|-----------------------------------------------------|
| **GPU (NVIDIA)** | NVIDIA GeForce RTX 2060 (6 GB) / RTX 3050 | NVIDIA GeForce RTX 4070 Ti / RTX 4080 or better |
| **GPU (AMD)** | AMD Radeon RX 6600 XT | AMD Radeon RX 7800 XT / RX 7900 XTX |
| **VRAM** | 6 GB to 8 GB | 12 GB to 16 GB+ |
| **CPU** | Intel Core i5 (8th Gen) / AMD Ryzen 5 (2nd Gen) | Intel Core i7 (12th Gen+) / AMD Ryzen 7 (5000/7000 series+) |
| **System RAM** | 8 GB to 16 GB | 16 GB to 32 GB |
| **Storage** | HDD or SSD | SSD or NVMe M.2 (faster loads; smoother High RT) |
| **OS** | Windows 10 / 11 (64-bit) and macOS | Windows 11 (64-bit) and macOS |

### macOS (ship)

Genesis ships on **macOS** alongside Windows. Profile and certify HDRP quality tiers on Apple Silicon; use the same FPS guidance as PC. Document Apple GPU / unified-memory **advisory thresholds** during the macOS certification pass.

---

## Storage

| Guidance | Notes |
|----------|--------|
| **Any storage** | HDD, SSD, or NVMe — all allowed for base game and ray tracing |
| **Recommended** | SSD/NVMe for shorter loads and smoother High RT |

Show an optional performance notice on HDD (longer loads) — **do not** prevent RT or high tiers.

---

## Ray tracing

Ray tracing is an **optional shipped feature** — not required on any quality tier or console.

| Profile | What we target in testing |
|---------|-------------------------|
| **Minimum guidance** | 1080p, Low RT, 30–60 FPS |
| **Recommended guidance** | 1440p+, High RT, 60+ FPS |
| **RT toggle** | **Always available** on PC/macOS; default **off** |
| **Consoles** | No ray tracing at ship; certify at 60 FPS without RT |

### RT and graphics settings (runtime / settings UI)

- **All RT and quality options remain selectable** regardless of GPU, VRAM, CPU, RAM, or storage.
- **Default on first run:** quality tier matched to platform default (e.g. High on PC); RT **off**.
- **Performance advisories** (examples — implement as non-blocking UI):
  - GPU below Minimum guidance → “Performance may be poor at this quality.”
  - VRAM under 6 GB with Ultra or High RT → “High memory use expected.”
  - RT enabled on GPU without hardware RT → “Ray tracing may be unavailable or very slow; SSR fallback may apply.”
  - HDD with High RT or Ultra → “Longer load times possible.”
  - CPU/RAM below guidance with high tiers → “Stuttering or low FPS possible.”
- **Pipeline fallback:** If RT cannot run on hardware, use SSR/shadow fallback **without** locking the toggle off; optionally show that RT is not active.
- **Never** gate gameplay, launch, or progression on specs.

---

## Console targets (ship)

Certification **targets** — not player-facing locks on PC/macOS behavior.

| Platform | Default tier target |
|----------|---------------------|
| PlayStation 5 | 60 FPS on default quality tier |
| Xbox Series X | 60 FPS on default quality tier |
| Xbox Series S | 60 FPS on Performance/Balanced tier |

Tune HDRP assets so defaults hit 60 FPS without RT. If a player selects a higher tier on console, use advisories where platform policy allows.

---

## Quality tier mapping (PC and macOS)

Guidance for defaults and store copy. **All five tiers remain selectable.**

| Tier | Typical hardware (guidance) | Resolution / RT (guidance) | FPS target (testing) |
|------|---------------------------|--------------------------|----------------------|
| Performance | Below Minimum GPU | 1080p, RT off | 30–60 |
| Balanced | Minimum GPU | 1080p, Low RT optional | 30–60 |
| Quality | Mid-range GPU | 1080p–1440p, RT off/low | 60 |
| High | Recommended GPU | 1440p, RT optional | 60+ |
| Ultra | Recommended GPU+ | 1440p+, High RT optional | 60+ |

---

## Implementation notes (`GameSettings` / settings UI)

Planned behavior for HDRP migration:

1. `GraphicsCapabilityAdvisor` (or equivalent) compares active settings + detected hardware to Minimum/Recommended tables.
2. Returns **warning strings only** — never `CanEnable = false`.
3. Settings panel shows warnings inline; optional confirm on first enable of Ultra + RT on weak hardware.
4. Persist user choices in `PlayerPrefs` even when warnings were shown.

---

## References

- `Documentation/Architecture/HDRP_Migration_Plan.md` — HDRP conversion and settings implementation
- GDD 5.0 — product identity; update platform appendix when macOS ship is reflected in GDD body text
