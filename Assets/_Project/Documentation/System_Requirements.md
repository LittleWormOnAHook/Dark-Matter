# Dark Matter: Genesis — System Requirements

Canonical PC system requirements for store pages, FAQ, settings capability detection, and HDRP quality/RT gating. Update when certification targets change.

**Ship title:** Dark Matter: Genesis  
**Platforms:** PC (Windows; recommended row includes macOS), PlayStation 5, Xbox (Series X|S)

---

## System Requirements Overview

| Component | Minimum Specs (1080p, Low RT, 30–60 FPS) | Recommended Specs (1440p+, High RT, 60+ FPS) |
|-----------|------------------------------------------|-----------------------------------------------|
| **GPU (NVIDIA)** | NVIDIA GeForce RTX 2060 (6 GB) / RTX 3050 | NVIDIA GeForce RTX 4070 Ti / RTX 4080 or better |
| **GPU (AMD)** | AMD Radeon RX 6600 XT | AMD Radeon RX 7800 XT / RX 7900 XTX |
| **VRAM** | 6 GB to 8 GB minimum | 12 GB to 16 GB+ for modern gaming |
| **CPU** | Intel Core i5 (8th Gen) / AMD Ryzen 5 (2nd Gen) | Intel Core i7 (12th Gen+) / AMD Ryzen 7 (5000/7000 series+) |
| **System RAM** | 8 GB to 16 GB | 16 GB to 32 GB |
| **Storage** | SSD (Solid State Drive) required | NVMe M.2 SSD |
| **OS** | Windows 10 / 11 (64-bit) | Windows 11 (64-bit) and macOS |

---

## Ray tracing

Ray tracing is an **optional shipped feature** — not required on any quality tier or console.

| Profile | Expectation |
|---------|-------------|
| **Minimum PC** | 1080p, **Low RT** settings, **30–60 FPS** target |
| **Recommended PC** | 1440p+, **High RT** settings, **60+ FPS** target |
| **RT toggle** | Enabled in settings when hardware meets **Minimum** GPU/VRAM thresholds; disabled or hidden below spec |
| **Consoles** | No ray tracing at ship; certify at 60 FPS without RT |

### RT capability gate (runtime / settings UI)

Use **Minimum** column for allowing the RT toggle:

- NVIDIA: GeForce RTX 2060 (6 GB) or RTX 3050 or better
- AMD: Radeon RX 6600 XT or better
- VRAM: at least **6 GB** (warn below 8 GB)
- CPU: Intel Core i5 (8th Gen) or AMD Ryzen 5 (2nd Gen) or better
- System RAM: at least **8 GB** (warn below 16 GB)

Use **Recommended** column for defaulting RT to on or showing “High RT” preset without warning.

---

## Console targets (ship)

| Platform | Target |
|----------|--------|
| PlayStation 5 | 60 FPS on default quality tier |
| Xbox Series X | 60 FPS on default quality tier |
| Xbox Series S | 60 FPS on Performance/Balanced tier |

Console hardware specs are fixed by platform; tune HDRP quality tiers to hit 60 FPS without RT.

---

## Quality tier mapping (PC)

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
- GDD 5.0 — product identity and platform lock (PC + consoles)
