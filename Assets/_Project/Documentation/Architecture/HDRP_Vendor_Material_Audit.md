# HDRP Vendor Material Audit

Generated: 2026-08-15 UTC (updated post Phase 6 pink/VFX pass)

## Scope

- Graphics / Quality **High = Genesis_HDRP_High** (Phase 6 applied).
- Counts prioritize materials referenced by `Dark Matter Genesis v1.56` and gameplay prefabs.
- Tools: `Tools/Dark Matter Genesis/HDRP/Audit Vendor Materials`, `Convert Folder URP→HDRP (Dry Run|Apply)`, `Convert Scene-Referenced Particles→HDRP`.

## Custom / _Project shaders (prep status)

| Asset | Choice | Notes |
|-------|--------|-------|
| `Project/EnemyDisintegrate` | Dual SubShader URP+HDRP | Same shader name; `Shader.Find` unchanged. |
| `Project/EnemyDissolveSmoke` | Dual SubShader URP+HDRP | Transparent ForwardOnly on HDRP. |
| `Project/SmokeParticle` | Dual SubShader URP+HDRP | Replaces Legacy Particles on `SmokeParticle.mat`. |
| `Custom/ScannerPostProcess` | Dual SubShader + HDRP Custom Pass | URP: blit / OnRenderImage; HDRP: `ScannerHdrpCustomPass`. |
| `Custom/ScannerPostProcessPBR` | Dual SubShader | Overlay scanline unlit (not full PBR). |
| Needle Plant `glTF-pbrMetallicRoughness` | Package dual-target Shader Graph | Already has UniversalTarget + HDTarget — no local fork. |

## Post Phase 6 pink / VFX pass (2026-08-15)

### Fixed in playable scene

| Category | Count | Notes |
|----------|------:|-------|
| Scene-referenced Legacy/Mobile particle mats → `HDRP/Unlit` | **28** | Invector muzzle/smoke/capsule, Malbers trails/sparks, Magic Spells circles, PolygonNature dust, etc. |
| Scene-referenced Hovl / WFX particle mats → `HDRP/Unlit` | **11** | Muzzle/projectile/trail/energy shield additive mats (center-glow look simplified). |
| Null material slots reassigned | **27** | Mining-tool Fire/Smoke/Capsule particles, grenade Debris, ToxicArea, IO Ancient Cache PS, ShopKeeper. |
| URP Lit leftovers in scene | **0** | Already converted in Phase 6. |
| Error/pink shaders in scene | **0** | No `Hidden/InternalErrorShader` on active/inactive renderers. |

### DMI material pulse scroll (HDRP emission)

- **Root cause:** HDRP Lit exposes both `_EmissiveColor` (real) and legacy `_EmissionColor` (often white). Driver bound `_EmissionColor` first → pulse invisible.
- **Fix:** `DMIMaterialPulseScroll` prefers `_EmissiveColor`, dual-writes secondary channel, uses `_BaseColorMap_ST` / `_EmissiveColorMap_ST`, enables `_EMISSIVE_COLOR`, respects `_UseEmissiveIntensity`.
- Also updated `DMICreatureEmissionDriver` bind order for the same HDRP dual-property trap.

### Remaining artist / tech reauthor (do not mass-convert)

| Asset | Shader | Why left |
|-------|--------|----------|
| `QFX/.../GO_ScannableObject.mat` | `QFX/SFX/Distortion/DistortionCutOut` | Screen distortion; needs HDRP distortion or Custom Pass reauthor. Scan Cone / Holo emitter. |
| `Toon Deserted Temples/.../TFD_Fire_01A.mat` | `Toon/TFD_ToonFire` | Custom toon fire — artist reauthor or HDRP VFX Graph. |
| `Toon Deserted Temples/.../TFD_Water_Ripples_01.mat` | `Toon/TFD_ToonWaterRipples` | Custom toon water ripples. |
| `Procedural Worlds/.../Unity URP Water.mat` | `Shader Graphs/Water` | Gaia Built-in/URP water leftover (lava stand-in). Reauthor on HDRP Water or Lit. |
| Unused Hovl / QFX / JMO / Magic Spells demo catalogs | Custom pack shaders | Not referenced by v1.56 gameplay — skip. |
| TMP Examples | — | Explicitly skipped. |
| SpeedTree / full Gaia biomes | various | Not in current flat playable terrain set; revisit with Io biome bring-up. |

### Converter guardrail fix

- Folder / `_Project` converters no longer force Graphics back to `PC_RPAsset` after Phase 6.
- Scene particle convert menu preserves active HDRP High pipeline.

## Guardrails

- Do **not** blind-convert entire Gaia or Invector trees in one click.
- Prefer dry-run → apply on a pack subfolder that is actually referenced.
- PPT / cinematic HDR / optional RT — still held.
- URP package remains installed (dual-pipeline customs + rollback safety).
