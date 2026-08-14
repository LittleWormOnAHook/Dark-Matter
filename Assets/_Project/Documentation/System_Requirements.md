# Dark Matter: Genesis — System Requirements

Canonical system requirements for store pages, FAQ, settings capability detection, and HDRP quality/RT gating. Update when certification targets change.

**Ship title:** Dark Matter: Genesis  
**Ship platforms:** **Windows PC**, **macOS**, PlayStation 5, Xbox (Series X|S)

---

## System Requirements Overview

| Component | Minimum Specs (1080p, Low RT, 30–60 FPS) | Recommended Specs (1440p+, High RT, 60+ FPS) |
|-----------|------------------------------------------|-----------------------------------------------|
| **GPU (NVIDIA)** | NVIDIA GeForce RTX 2060 (6 GB) / RTX 3050 | NVIDIA GeForce RTX 4070 Ti / RTX 4080 or better |
| **GPU (AMD)** | AMD Radeon RX 6600 XT | AMD Radeon RX 7800 XT / RX 7900 XTX |
| **VRAM** | 6 GB to 8 GB minimum | 12 GB to 16 GB+ for modern gaming |
| **CPU** | Intel Core i5 (8th Gen) / AMD Ryzen 5 (2nd Gen) | Intel Core i7 (12th Gen+) / AMD Ryzen 7 (5000/7000 series+) |
| **System RAM** | 8 GB to 16 GB | 16 GB to 32 GB |
| **Storage** | HDD or SSD acceptable | SSD or NVMe M.2 recommended (faster loads; smoother High RT) |
| **OS** | Windows 10 / 11 (64-bit) and macOS | Windows 11 (64-bit) and macOS |

### macOS (ship)

Genesis ships on **macOS** alongside Windows. Profile and certify HDRP quality tiers on Apple Silicon targets; use the same FPS targets as PC (30–60 at Minimum profile, 60+ at Recommended). Ray tracing on macOS follows the same optional toggle rules as Windows. Document Apple GPU / unified-memory thresholds during the HDRP macOS certification pass.

---

## Storage

| Use case | Requirement |
|----------|-------------|
| **Base game** | HDD or SSD — **no SSD required** |
| **Ray tracing** | HDD or SSD — **users can enable RT on HDD** |
| **Recommended experience** | SSD or NVMe for shorter load times and better High RT performance |

Do **not** gate the RT toggle on storage type. Optional non-blocking UI hint if HDD is detected (e.g. longer loads or lower RT performance expected).

---

## Ray tracing

Ray tracing is an **optional shipped feature** — not required on any quality tier or console.

| Profile | Expectation |
|---------|-------------|
| **Minimum** | 1080p, **Low RT** settings, **30–60 FPS** target |
| **Recommended** | 1440p+, **High RT** settings, **60+ FPS** target |
| **RT toggle** | Enabled when hardware meets **Minimum** GPU/VRAM/CPU/RAM thresholds |
| **Consoles** | No ray tracing at ship; certify at 60 FPS without RT |

### RT capability gate (runtime / settings UI)

Allow the RT toggle when **GPU / CPU / RAM** meet the **Minimum** column:

- NVIDIA: GeForce RTX 2060 (6 GB) or RTX 3050 or better
- AMD: Radeon RX 6600 XT or better
- macOS: Metal-capable GPU meeting HDRP RT minimums (profile during macOS cert)
- VRAM: at least **6 GB** (optional warn below 8 GB)
- CPU: Intel Core i5 (8th Gen) or AMD Ryzen 5 (2nd Gen) or better (macOS: equivalent Apple Silicon tier)
- System RAM: at least **8 GB** (optional warn below 16 GB)

**Storage is not a gate** — RT remains available on HDD. Use **Recommended** column (and SSD/NVMe) only for performance hints, not hard locks.

---

## Console targets (ship)

| Platform | Target |
|----------|--------|
| PlayStation 5 | 60 FPS on default quality tier |
| Xbox Series X | 60 FPS on default quality tier |
| Xbox Series S | 60 FPS on Performance/Balanced tier |

Console hardware specs are fixed by platform; tune HDRP quality tiers to hit 60 FPS without RT.

---

## Quality tier mapping (PC and macOS)

| Tier | Typical hardware | Resolution / RT | FPS target |
|------|------------------|-----------------|------------|
| Performance | Below minimum GPU | 1080p, RT off | 30–60 |
| Balanced | Minimum GPU | 1080p, Low RT optional | 30–60 |
| Quality | Mid-range GPU | 1080p–1440p, RT off/low | 60 |
| High | Recommended GPU | 1440p, RT optional | 60+ |
| Ultra | Recommended GPU+ | 1440p+, High RT optional | 60+ |

---

## References

- `Documentation/Architecture/HDRP_Migration_Plan.md` — HDRP conversion and tier implementation
- GDD 5.0 — product identity; update platform appendix when macOS ship is reflected in GDD body text
