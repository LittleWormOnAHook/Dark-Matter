# HDRP Vendor Material Audit

Generated: 2026-08-15 UTC

## Scope

- Graphics / Quality **High remain URP** (Phase 6 not run).
- Counts prioritize materials referenced by `Dark Matter Genesis v1.56` and `Assets/_Project` prefabs.
- Referenced `.mat` dependency set size: **341**.
- Tools: `Tools/Dark Matter Genesis/HDRP/Audit Vendor Materials`, `Convert Folder URP→HDRP (Dry Run|Apply)`.

## Custom / _Project shaders (prep status)

| Asset | Choice | Notes |
|-------|--------|-------|
| `Project/EnemyDisintegrate` | Dual SubShader URP+HDRP | Same shader name; `Shader.Find` unchanged. |
| `Project/EnemyDissolveSmoke` | Dual SubShader URP+HDRP | Transparent ForwardOnly on HDRP. |
| `Project/SmokeParticle` | Dual SubShader URP+HDRP | Replaces Legacy Particles on `SmokeParticle.mat`. |
| `Custom/ScannerPostProcess` | Dual SubShader + HDRP Custom Pass | URP: blit / OnRenderImage; HDRP: `ScannerHdrpCustomPass`. |
| `Custom/ScannerPostProcessPBR` | Dual SubShader | Overlay scanline unlit (not full PBR). |
| Needle Plant `glTF-pbrMetallicRoughness` | Package dual-target Shader Graph | Already has UniversalTarget + HDTarget — no local fork. |

## Pack summary

| Pack | Exists | Total .mat | URP convertible | Custom/Built-in | HDRP | Unsupported | Ref total | Ref URP | Ref custom | Ref pink/broken | Severity | Action |
|------|--------|------------|-----------------|-----------------|------|--------------|-----------|---------|------------|-----------------|----------|--------|
| Invector | yes | 301 | 244 | 57 | 0 | 0 | 163 | 120 | 43 | 0 | High | Convert gameplay-referenced URP mats via folder tool; leave unused catalog. |
| Gaia / Procedural Worlds | yes | 247 | 3 | 237 | 7 | 0 | 1 | 0 | 1 | 0 | Defer | Leave until Phase 6. |
| Gaia User Data | yes | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | Defer | Leave until Phase 6. |
| Malbers | yes | 169 | 131 | 38 | 0 | 0 | 34 | 22 | 12 | 0 | Medium | Convert critical referenced URP mats; leave custom/VFX until Phase 6. |
| Hovl Studio | yes | 78 | 3 | 75 | 0 | 0 | 21 | 0 | 21 | 0 | Medium | Convert critical referenced URP mats; leave custom/VFX until Phase 6. |
| PolygonSciFiWorlds | yes | 109 | 76 | 33 | 0 | 0 | 7 | 4 | 3 | 0 | Medium | Convert critical referenced URP mats; leave custom/VFX until Phase 6. |
| PolygonNature | yes | 78 | 62 | 16 | 0 | 0 | 8 | 4 | 4 | 0 | Medium | Convert critical referenced URP mats; leave custom/VFX until Phase 6. |
| PolygonTown | yes | 26 | 26 | 0 | 0 | 0 | 7 | 7 | 0 | 0 | Medium | Convert critical referenced URP mats; leave custom/VFX until Phase 6. |
| QFX | yes | 84 | 27 | 57 | 0 | 0 | 9 | 3 | 6 | 0 | Medium | Convert critical referenced URP mats; leave custom/VFX until Phase 6. |
| Buildings_constructor | yes | 23 | 19 | 4 | 0 | 0 | 12 | 12 | 0 | 0 | Medium | Convert critical referenced URP mats; leave custom/VFX until Phase 6. |
| Shift UI | yes | 10 | 0 | 10 | 0 | 0 | 0 | 0 | 0 | 0 | Defer | Leave until Phase 6. |
| Blink | yes | 3 | 3 | 0 | 0 | 0 | 1 | 1 | 0 | 0 | Medium | Convert critical referenced URP mats; leave custom/VFX until Phase 6. |
| JMO / Cartoon FX | yes | 106 | 10 | 96 | 0 | 0 | 3 | 0 | 3 | 0 | Defer | Leave until Phase 6. |
| Magic Spells & Particles | yes | 172 | 1 | 171 | 0 | 0 | 11 | 1 | 10 | 0 | Medium | Convert critical referenced URP mats; leave custom/VFX until Phase 6. |
| _Project (context) | yes | 48 | 0 | 6 | 42 | 0 | 31 | 0 | 1 | 0 | Defer | Already largely converted; finish custom/Shader Graph leftovers only. |

## Recommended Phase 6 leftovers

1. Bulk-convert remaining vendor URP catalogs (Gaia / Invector) only after playable scene is on HDRP.
2. Replace or reauthor pack-specific custom shaders (Malbers, Hovl, QFX, WarFX) that are not Lit/Unlit.
3. Wire scanner Custom Pass volumes into gameplay cameras; retire `OnRenderImage` path.
4. Rebake lighting / reflection probes on `Dark Matter Genesis v1.56`.
5. PPT / cinematic HDR / optional RT — still held.

## Guardrails

- Do **not** blind-convert entire Gaia or Invector trees in one click.
- Prefer dry-run → apply on a pack subfolder that is actually referenced.
- Keep `PC_RPAsset` on Graphics until Phase 6 menu is explicitly run.

## Conversion decision (this prep pass)

- **Vendor apply deferred** while Quality **High** stays on URP — converting Invector/Malbers/etc. to `HDRP/Lit` now would pink the playable scene.
- Invector folder dry-run: **244** URP Lit/Unlit/Particles convertible of **336** material assets (apply at Phase 6 or via folder menu when switching tiers).
- Critical path for this pass: dual-pipeline `_Project` customs + audit tooling only.

