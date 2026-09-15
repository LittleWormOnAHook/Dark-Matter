# Prologue Intro — Geomagnetic Crash → Coordinates → Horizon Camp

**Project:** Dark Matter: Genesis  
**Status:** Design draft (Sep 2026) — intro slice before full prologue (Kairos shell, BCP, 5000 AC companion pick)  
**Player:** **Kade**  
**Spine alignment:** V2 Colony Horizon default; geomagnetic crash works for V1/V4 with different VO on the coordinate slate  
**Canon refs:** GDD 5.0 A6 (prologue / Kairos), `Player_Identity_Kade.md`, biome order B6 hub first

---

## 1. Premise (30-second pitch)

Kade’s insert shuttle is scheduled to land on a **surveyed horizon site** in **B6 Basalt Highlands** — the future **base camp** ring. Navigation dies in a **geomagnetic shear** (Jupiter–Io flux / precursor field interference). The shuttle **crashes** kilometers off-target.

Kade survives with **no working uplink**, **no ground crew**, and **one hard asset**: a **coordinate set** on a charter slate for the intended landing zone — plotted as a **map search zone** (thin alpha circle), **not** a precise POI. The intro is a **solo walk** from wreck to horizon: explore the ring, **scanner-ping** for short bearing flashes, survive exposure, and pass through **three encounter corrals** that teach core verbs without quest-board hand-holding.

**Prologue proper** begins when Kade reaches the **empty camp pad** (hab scars, dead auto-land beacons) and later finds the **liaison shell** — not during the crash march.

---

## 2. Why geomagnetic (Io-correct)

| Layer | Fiction | Gameplay |
|-------|---------|----------|
| **Jupiter magnetosphere** | Shuttle nav and ground-lock fail during a flux spike | Explains crash without “pilot error”; sets ion lightning / rad pulse tone |
| **Io precursor noise** | Optional late reveal: fields aren’t purely natural | Seeds Kairos / Memory Core mystery without naming Kairos |
| **Schedule truth** | Coordinates are **legitimate** — the pad exists | Player trust in the marker; horror is **emptiness**, not a lying UI |

GDD already ties **ion lightning** and **radiation pulse** to Jupiter shadow / magnetosphere — reuse that language in slate logs and storm barks.

---

## 3. Player start state (crash site)

| Has | Does not have |
|-----|----------------|
| Survival suit, emergency O₂ / thermal baseline | Working shuttle radio (sparks static or ledger ghost lines only) |
| **Crash salvage** (see §3.1) after searching crates | Map reveal of full Io |
| **Journal Coordinates** (from first EEB) → **search zone** on map for **Horizon Pad** | Map center POI for pad until ping/resolve; **no compass ring** (gold bearing dot only) |
| Crash site (implicit anchor) | Companions on the ground (orbit / dead insert / solo charter — pick one; **solo on foot** fits tutorial) |
| | Building Control Panel, Kairos, 5000 AC spend until camp beat |

### 3.1 Crash shuttle salvage (before leaving wreck)

Kade **must search supply crates** in the wreck (interact prompts on labeled cases). **Cannot** exit playable wreck zone until at least **primary crate** looted (tutorial gate — optional soft block with “seal suit first” bark).

| Item | Qty | Notes |
|------|-----|-------|
| **UEA Standard Pistol** | 1 | Lore name; maps to existing ranged pistol pipeline (`sci_fi_pistol` class) until dedicated UEA asset |
| **Pistol ammunition** | **20** | Standard rounds / cell count per ammo bridge |
| **Service Axe** | 1 | Melee tool + corral 2 teach; new `Service Axe` ItemData or reskin of utility axe |
| **Med Pack** | 1 | Maps to `Medpack` consumable |
| **Bio Gel** | 1 | New consumable — exposure/suit patch or heal-over-time (author effect in ItemData) |
| **EEB field log puck** | 1 | Same **primary crate** as pistol; read to collect log + **Horizon Pad coordinates** |

Corral 2–3 assume **ranged + melee** in inventory. Hotbar assignment teach in crash bowl after loot.

**EEB read:** posts **bold gold** coordinate lines to Journal **Coordinates**; plots map search zone (see `Field_Log_Devices.md` §2.1).

### 3.2 Coordinate + search zone (low hand-holding)

- **Source of truth:** first **EEB** transcript (UEA insert / horizon grid) — not a separate slate item.
- **Journal:** Coordinates panel — highlighted **bold**, **~115% size**, **Gold** text.
- **Map / minimap:** **`MapSearchZone`** alpha ring (`Map_Search_Zone_System.md`); **no center POI** until ping/resolve.
- **Compass:** **no ring** — **large gold dot** on bearing with radial alpha falloff (opaque to ~⅔ radius, fade to edge); optional emission glow child.
- Inside map ring: **scanner sweep** → ping + **3 s direction flash** toward true pad; compass dot may pulse once.
- Optional ledger tick when **in zone** + first ping only — not continuous VO.

Scanner sweep taught after first EEB read (open map, see ring) and reinforced in **Corral 1**.

---

## 4. Route structure (crash → horizon)

Approximate **play time 25–40 min** first run; **1.5–3 km** authored path on B6 skirt (greybox OK on flat prototype).

```
[Crash Bowl] ──walk──► [Corral 1: Controls & survival]
                              │
                         [Breather lane] (optional scav, lore)
                              │
                         [Corral 2: First contact combat]
                              │
                         [Corral 3: Stamina / parry or ranged gate]
                              │
                         [Horizon approach ridge]
                              │
                         [Empty Horizon Pad] → INTRO_COMPLETE → Prologue ML-01
```

### 4.1 Crash bowl (5–8 min)

- **Cinematic:** descent → flux alarms → hard landing (skippable).
- **Gameplay:** search **primary crate** (§3.1) → pistol + **EEB** → **read EEB** → Journal coordinates + map ring → equip / hotbar.
- **Tone:** no NPC; distant ruined hab **visible on horizon** optional; emphasize silence.
- **Teach:** interact, loot, EEB read, Journal coordinates styling, map ring + compass gold dot + scanner ping.

**WorldState:** `intro_crashed`, `intro_crash_loot_complete`, `intro_eeb_horizon_read`, `intro_search_zone_plotted`, `intro_journal_coords_horizon_posted`

### 4.2 Open walk 0 → Corral 1 (3–5 min)

- Light **thermal / O₂** pressure (B6 forgiving).
- One **scannable** wreck tag or android loop (“occupancy: zero”).
- No combat.

**Teach:** exposure HUD, sprint stamina if exposed.

### 4.3 Encounter corrals (design pattern)

An **encounter corral** is a **authored choke** with:

- `CombatZoneController` parent + `SurfaceEncounterZone` (or fixed spawn) on enter
- **Invisible or low mesh fences** / cliff rails so threats stay in teach space
- **Single exit** only opens on `encounter_cleared` or survival timer
- **No quest giver** — a **slate prompt** or environmental sign is enough (“**Training grid — ignore if certified**” as irony)

| Corral | Location flavor | Systems taught | Encounter content |
|--------|-----------------|----------------|-------------------|
| **1 — Survey lane** | Basalt cut, broken fence | **Move, jump, scan**, search-zone ping refresh, hotbar | 0 combat or 1 **Beacon Hopper** flee (V1 seed); optional Field Log puck |
| **2 — Contact ring** | Old expedition corral / animal pen mesh | **Lock-on / melee**, damage, **enemy telegraph** | 1–2 **Tube Jackal** or **Cinder Skitter** (B6 ecology); `[SHIPPED]` melee where possible |
| **3 — Hold the gap** | Narrow bridge or tube mouth | **Stamina**, block/parry *or* ranged + **reload**, dodge | 1 **Sulfur Hound** if reachable from B6 skirt *or* jackal pack; use `SurfaceEncounterTable` intro row |

**Corral rules:**

- Fail = respawn at corral entrance (intro-only mercy).
- No loot treadmill — one **field cache** (O₂ patch, bandage) per corral max.
- After clear: **exit gate opens**; no “Return to quest giver.”

**WorldState:** `intro_corral_1_complete` … `intro_corral_3_complete`

### 4.4 Horizon approach (5 min)

- Vista: **pad markings**, crushed landing targets, **empty** supply pallets.
- Optional: shuttle **intended** landing lights still cycling — “nobody’s here.”
- Short walk into pad trigger.

**WorldState:** `intro_reached_horizon_pad` → `intro_complete`

---

## 5. Horizon pad payoff (handoff to prologue)

When Kade hits the pad:

1. **Search zone resolved** — slate chimes “grid match”; ring fades to optional camp marker.
2. Player finds **not** a ready camp — **abandoned prep**: foundations, tether points, dead auto-beacon (scheduled colony that never arrived *on this insert*).
3. **Interact:** deploy **emergency stake** / flag survey claim (fiction) → unlock **camp build footprint** or transition to existing B6 hub greybox.
4. **Do not** spawn Kairos here if ML beats place shell under ash bowl nearby — separate discoverable POI.

**Next beat (out of intro scope):** V2 **ML-01 First Horizon** — first Building Control Overview; V1 **ML-V1-01** — survival relay; companion pick / 5000 AC per chosen package.

---

## 6. Tutorial philosophy (your constraints)

| Do | Don’t |
|----|-------|
| Teach through **corral geometry** and one slate line per corral | Floating modal chains or 12-step coach marks |
| Let **search ring + scanner ping** be the main quest | NPC arrow or precise POI before resolve |
| One **optional** “open map” nudge at crash | Per-meter distance callouts |
| Respect **PC + gamepad** parity for map/compass | Mobile-style forced taps |

---

## 7. Implementation hooks (Unity)

| Piece | Suggestion |
|-------|------------|
| Scene | `Prologue_Intro_B6_CrashToHorizon` (subscene or sector of Genesis scene) |
| Encounters | Reuse `SurfaceEncounterZone` + `CombatZoneController`; intro table `SurfaceEncounterTable_IntroB6` |
| Map | `MapSearchZone` for horizon; `MapMarker` only after `intro_search_zone_resolved` |
| Field Logs | `FieldLogDefinition` + Journal tab; intro EEB + 2–3 optional along route |
| Items | UEA pistol + 20 ammo, Service Axe, Medpack, Bio Gel — crash crate loot table |
| Directors | `GameState` / `WorldState` flags above; `intro_complete` gates main hub systems |
| Comms | Ledger strings only until Communications runtime; label **liaison / probe**, not Kairos |
| Save | New game starts `intro_crashed=false`; mid-intro flags restore at last corral checkpoint |

---

## 8. Package flavor (one VO pass)

| Package | Crash slate line | Empty pad read |
|---------|------------------|----------------|
| **V2** | “Horizon schedule valid. Ground team not detected.” | Hope: first panel still possible |
| **V1** | “Grid matches. Signal traffic does not.” | Wrong-kind beacons nearby |
| **V4** | “Escrow waypoint released. Client assets absent.” | Contract clock starts |

---

## 9. Open decisions

1. **Companions:** Solo insert only vs Reid/Suri in **orbit** (radio after pad). Solo simplifies corral tuning.
2. **Shuttle wreck:** Persistent landmark vs despawn after intro.
3. **Corral 3 enemy:** Strict B6-only fauna vs allow one **Sulfur Hound** as “first real threat” preview before B1.
4. **Distance:** Match 25 min (tight) vs 40 min (more breathers) for campaign pacing.

---

## 10. Related files

- `Map_Search_Zone_System.md` — alpha circle + scanner ping rules  
- `Field_Log_Devices.md` — Io recording collectibles + Journal  
- `Narrative_Package_V2_Colony_Horizon.md` — ML-01+ hub beats  
- `Narrative_Package_V1_Ash_And_Signal.md` — ML-V1-01 prologue beats  
- `Io_Genesis_World_Map_Geography.md` — B6 placement  
- `Assets/_Project/Scripts/Map/MapMarker.cs`, `CompassHudUI.cs`, `ScannerSweepController.cs`, `SurfaceEncounterZone.cs`, `CombatZoneController.cs`
