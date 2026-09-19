# Io scene concept reference

**Canonical set (70):** **`Biomes/B1_SulfurPlains` … `B7_PrecursorRuins`** — **10 HDRP environmental shots per surface biome**. Shot roles: `Io_Biome_Scene_Concept_Master.md`. Kade refs in `References/`.

Legacy POI deep-dives (optional): `Meridian6/`, `AshVeinRest/` (named drill + instance camp). Old 2-style matrix docs kept for reference only.

## Starter trio (original)

Reference images for **blocking scenes, POIs, PPT entries, and art direction** on Io 2160. Not final in-game assets—use for layout, mood, and checklist when building in Unity (`Dark Matter Genesis` scene / biome prefabs).

| File | Style | Scene title | Build targets |
|------|--------|-------------|----------------|
| `DM_SceneConcept_01_InGame_SulfurDrillMeridian6.png` | In-game+ (HDRP-style) | **Meridian-6** sulfur-field drill wreck | Abandoned camp POI, hazard ring, scanner moment, creature telegraph, PPT thing `thing_meridian_log` |
| `DM_SceneConcept_02_Artist_AshVeinRestCamp.png` | Artist key art | **Ash Vein Rest** instance camp | Far-site rest/stash, scrapper NPC, trio camp fire, PPT place `place_ash_vein_rest`, Jupiter horizon landmark |
| `DM_SceneConcept_03_Modern_SignalGeometry.png` | Modern abstract | **Signal Geometry** (Echo/Kairos) | Loading art, Echo UI, marketing; abstract read of relay + drill + creature + lime tracer dot |

## Scene checklist (all three)

- **Lore:** evidence of prior expedition failure; Kade as founder-commander, not superhero.
- **Gameplay:** room for **Tap/Hold E**, scan, thermal/exposure read, optional Echo signal VFX.
- **Palette:** basalt, sulfur ochre, rust steel, steam; **magenta/fuchsia** only on diegetic UI/scan FX in realistic frames—not sky paint.
- **Creatures:** readable silhouette at distance; one primary threat or ambient species per frame.
- **PPT:** name places/things in registry when scene ships (`Assets/_Project/Resources/PPT/`).

## Blockout notes

### 01 Meridian-6 (in-game+)

- Hero prop: bent **drill mast** + collapsed **hab ring**; secondary: cable trays to dead geothermal tap.
- Player line: approach from foreground left; **scanner** readable.
- Creature: ridge line right, **non-agro** stance.
- Distance: caldera glow **low on horizon** (depth only).

### 02 Ash Vein Rest (painterly target)

- Shelf terrain above **lava tube** mouth (walk-in teaser scale).
- **Two shelters** (one lit), geothermal stack, valley **mini rig** for scale.
- **Scrapper + map post** for directions fantasy; **three companion** slots at fire.

### 03 Signal Geometry (abstract)

- Use for **tone** and **poster** layouts; do not literal-match blockout.
- Map **lime dot** → in-game `PositiveGreen` direction tracer; **fuchsia line** → scan/resonance UI only.

## Related docs

- GDD 5.0 — environmental storytelling (collapsed habitats, destroyed rigs).
- `Io_Biome_Exploration_Gameplay_Plan.md` — instance camps, B-biomes.
- `PPT_System_Design.md` — place/thing discovery and Hold E directions.
