# Audit_04 — World

**HLA:** §2.3 World · WoOS layer 1  
**Paths:** `Scripts/Map/`, `Scripts/Survival/`, `Scripts/Survival/Exposure/`  
**Audit date:** July 2026

---

## Excellent / keep

- **`ExposureStatusService`** — single bipolar thermal + multi-hazard aggregates; feeds HUD and `WeatherGameStateProvider`.
- **Zone kind model** — radiation, thermal, sulfur, volcano aligned with GDD.
- **`MapRegistry` / map UI integration** — discovery and compass hooks exist.
- **`PlayerExposureBootstrap`** — player-side exposure wiring via composition root.

---

## Move later

| Current | Target |
|---------|--------|
| Exposure + map | `Features/World/` |
| Flat terrain prototype | biome/streaming submodules (GDD B3) |
| Debug sulfur crisis HUD | `WeatherDirector` + `EnvironmentWorldStateProvider` |

---

## Risk

| Risk | Detail |
|------|--------|
| **Weather = exposure proxy** | `WeatherGameStateProvider` reads exposure, not a live weather scheduler |
| **No sulfur storm scheduler** | GDD B3 not started — crisis via HUD/debug only |
| **No biome pipeline** | flat terrain; `PlanetEvolutionSnapshot` will stub |
| **Map not tied to WorldState** | exploration % not computed |

---

## WorldState fields

| Field | Provider (planned) |
|-------|-------------------|
| `PlanetExplorationPercent` | World adapter (from map discovery) |
| `BiomeUnlockMask` | World adapter (stub until biomes) |
| `SulfurStormActive` / storm phase | `EnvironmentWorldStateProvider` |
| `ThreatLevel` (environment slice) | exposure severity + zone names |
| `DisplayTemperatureF`, hazards | already in `WeatherSnapshot` (GameState) |

---

## Dependencies

**Inbound:** Player position, survival stats, HUD (`Exposure` gauges, crisis mode).  
**Outbound:** `WeatherGameStateProvider` → `ExposureStatusService.Current`.

**Director owner (planned):** `WeatherDirector` schedules storms; Experience modulates intensity.
