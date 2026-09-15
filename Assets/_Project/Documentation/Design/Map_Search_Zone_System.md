# Map Search Zones (Alpha Circle)

**Project:** Dark Matter: Genesis  
**Status:** Design lock (Sep 2026) — global navigation pattern for intro coordinates, undisclosed POIs, and quest areas  
**UI palette:** thin ring outline — Slate Gray `#4A4A5A` or Warm Off-White `#EDE9E4` at low alpha fill; no filled quest chevron at zone center until resolved  
**Related code (today):** `MapUI.Rendering.cs`, `MapUiSprites.CircleRing`, `CompassHudUI`, `ScannerSweepController`, `MapFogOfWar`

---

## 1. Purpose

Replace **precise map POIs** for unknown or imprecise objectives with a **search zone**: a **hard thin circle** on minimap and full map. The player must **enter the zone** and optionally **use the scanner** to refine position. This applies to:

- Intro **Horizon Pad** coordinates (post-crash)
- Future **undisclosed** POIs and quest areas (exact target hidden until search completes)
- Optional: Echo signals, cache hunts, liaison shell first pass (designer toggle per zone)

**Not** used for: known vendors, active BCP, companions on map, discovered scan markers.

---

## 2. Visual spec

| Element | Rule |
|---------|------|
| **Outline** | 1–2 px equivalent **hard** ring (use `MapUiSprites.CircleRing` or dedicated `SearchZoneRing` sprite — no soft glow-only ring) |
| **Fill** | **Alpha only** — ~8–15% Dark Navy or Slate tint inside ring; must not obscure terrain |
| **Label** | Optional short tag on full map only: e.g. `SEARCH — HORIZON GRID` (Gold when active objective) |
| **Compass** | **No** icon at zone center by default; compass shows zone edge bearing only after slate “plots search area” |
| **Precision POI** | Hidden until zone state ≥ `Pinged`; never show exact dot at game start for intro horizon |

Undisclosed future quests use the same component with `revealLabel = false` until narrative flag unlocks a name.

---

## 3. Zone states

```
Locked → Plotted → InZone → Pinged → Resolved
```

| State | How entered | Map / compass behavior |
|-------|-------------|-------------------------|
| **Locked** | Default for secret quests | No ring (or greyed dossier only in Journal) |
| **Plotted** | Kade reads coordinates / accepts quest | Ring appears; **no center POI** |
| **InZone** | Player position inside circle collider | Ring highlight (Rich Fuchsia edge pulse optional) |
| **Pinged** | InZone + **scanner sweep** (middle mouse / bound scan) | **3 s directional flash** toward true target; optional audio ping |
| **Resolved** | Reach `captureRadius` around true coords | Ring fades; optional permanent `MapMarker` for camp/POI |

**Repeat ping:** While `InZone` and not `Resolved`, each scanner sweep refreshes the **3 s bearing flash** (world-space arrow or screen-edge wedge — not a permanent quest marker).

**Close range:** Inside `captureRadius` (e.g. 25–40 m), flashes stop; environmental cues (beacon stanchions, pad paint) take over.

---

## 4. Scanner interaction (intro + global)

| Condition | Result |
|-----------|--------|
| Outside zone | Scanner works as today (fog, highlights); **no** search ping |
| Inside zone, not resolved | Sweep triggers **ping** + **3 s direction flash** toward `trueTarget` |
| Inside zone, resolved | Normal scanner only |
| Explorer without scanner yet | Can still **resolve** by walking grid inside ring (slower); intro teaches scan in corral 1 |

Direction flash:

- Bearing from player → `trueTarget` (horizontal)
- Duration: **3 seconds** (configurable per zone)
- Cooldown: **5–8 s** between ping flashes to prevent spam
- Optional: brief gold rim on compass strip in bearing direction

---

## 5. Data model (implementation target)

ScriptableObject or scene component **`MapSearchZoneDefinition`**:

| Field | Type | Notes |
|-------|------|-------|
| `zoneId` | string | Save key |
| `center` | Vector3 / zone anchor | Map ring center |
| `radiusMeters` | float | Intro horizon: large (400–800 m); tight caches: 40–80 m |
| `trueTarget` | Vector3 | Actual pad / POI |
| `captureRadius` | float | Resolve distance |
| `initialState` | enum | Intro: `Plotted` after slate read |
| `showOnCompass` | bool | Edge bearing vs none until pinged |
| `label` | string | Optional |
| `linkedQuestFlag` | string | WorldState gate for Locked → Plotted |

Runtime: **`MapSearchZoneRegistry`** syncs with `MapUI` overlay layer and `WorldState`.

---

## 6. Intro horizon (first zone)

| Parameter | Suggested value |
|-----------|-----------------|
| `zoneId` | `intro_horizon_pad` |
| Ring radius | 600 m (tune to corral path length) |
| True target | Empty horizon pad origin |
| Plotted trigger | Kade reads charter slate **after** crate salvage |
| First ping teach | Optional forced hint if player leaves crash bowl without opening map once |

**WorldState flags:** `intro_search_zone_plotted`, `intro_search_zone_pinged`, `intro_search_zone_resolved`

---

## 7. Future content hooks

- Stack **multiple rings** (only one “active” gold; others muted)
- **Nested zones:** outer ring → inner ring after first ping (B3 beacon choir)
- **False rings:** V1 package — hopper mimic ring decoy (one per act, rare)
- Journal **Quest** tab lists zone label + state text, not coordinates string

---

## 8. Do / don’t

| Do | Don’t |
|----|-------|
| Teach “plot search area” once at crash slate | Show golden path polyline |
| Use scanner as **refinement** inside ring | Require ping before allowing any progress (walk-in resolve OK) |
| Match Shift / DMG palette on rings | Cyan sci-fi `#63C6FF` accent |

---

## 9. Related files

- `Prologue_Intro_Crash_To_Horizon.md` — crash march + first zone  
- `Field_Log_Devices.md` — lore pickups often placed **inside** search rings  
- `Assets/_Project/Scripts/UI/MapUiSprites.cs` — ring sprite baseline
