// Genesis PCG Rock Creation - Rock Blend layers (included by GenesisRockBlendData.hlsl, game-agnostic).
// Applied to the HDRP Lit SurfaceData after the base (atlas / mesh UV) maps and decals, before built-in (GI) data,
// so indirect lighting uses the blended albedo / normal.
//   - world strata / macro variation  : world-space bands with tilt + warp noise, large-scale tint noise
//   - top layer   (moss/sand/snow/...): vertex color G (up exposure) + world-up normal + noise, height-based edge
//   - base terrain blend              : vertex color A (height above ground), triplanar world projection aligned with the
//                                       terrain layer's UVs
//   - ground fade (optional)          : dithered opaque clip near the ground (all passes), terrain-coloured, contact AO keep
//   - base normal through layers      : the rock's own normal detail shows through the top layer / terrain blend, modulated
//                                       by world noise (smooth patches vs bumpy patches)
//   - per-pixel terrain match         : the terrain under the rock (control maps + layers, TerrainLit's blend) when
//                                       PcgTerrainSplat has bound it to the renderer; else the baked dominant layer
//   - texture arrays                  : base maps from per-kit Texture2DArrays (UV0 + slice / group in UV3)
#ifndef GENESIS_ROCK_BLEND_FUNCTIONS
#define GENESIS_ROCK_BLEND_FUNCTIONS

#if defined(SHADER_STAGE_RAY_TRACING)
#define GRB_SAMPLE(t, s, uv) SAMPLE_TEXTURE2D_LOD(t, s, uv, 0)
#else
#define GRB_SAMPLE(t, s, uv) SAMPLE_TEXTURE2D(t, s, uv)
#endif

float GRB_Lum(float3 c) { return dot(c, float3(0.2126, 0.7152, 0.0722)); }

float GRB_Hash11(float x) { return frac(sin(x * 127.1 + 311.7) * 43758.5453); }

float GRB_Hash31(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

float GRB_ValueNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float n000 = GRB_Hash31(i);
    float n100 = GRB_Hash31(i + float3(1, 0, 0));
    float n010 = GRB_Hash31(i + float3(0, 1, 0));
    float n110 = GRB_Hash31(i + float3(1, 1, 0));
    float n001 = GRB_Hash31(i + float3(0, 0, 1));
    float n101 = GRB_Hash31(i + float3(1, 0, 1));
    float n011 = GRB_Hash31(i + float3(0, 1, 1));
    float n111 = GRB_Hash31(i + float3(1, 1, 1));
    float x00 = lerp(n000, n100, f.x), x10 = lerp(n010, n110, f.x);
    float x01 = lerp(n001, n101, f.x), x11 = lerp(n011, n111, f.x);
    return lerp(lerp(x00, x10, f.y), lerp(x01, x11, f.y), f.z);
}

// ~[0,1], three octaves
float GRB_Fbm(float3 p)
{
    return 0.5 * GRB_ValueNoise(p) + 0.3 * GRB_ValueNoise(p * 2.03 + 17.1) + 0.2 * GRB_ValueNoise(p * 4.01 + 31.7);
}

float3 GRB_TriWeights(float3 n)
{
    float3 w = abs(n);
    w = w * w; w = w * w;
    return w / max(1e-4, w.x + w.y + w.z);
}

// Triplanar albedo; the top (Y) projection uses uvY so it can line up with a terrain layer.
float4 GRB_TriSample(TEXTURE2D_PARAM(tex, samp), float3 p, float2 uvY, float3 w)
{
    return GRB_SAMPLE(tex, samp, p.zy) * w.x + GRB_SAMPLE(tex, samp, uvY) * w.y + GRB_SAMPLE(tex, samp, p.xy) * w.z;
}

// Triplanar normal map, whiteout blend, world space result.
float3 GRB_TriNormal(TEXTURE2D_PARAM(tex, samp), float3 p, float2 uvY, float3 n, float3 w, float scale)
{
    float3 tx = UnpackNormalScale(GRB_SAMPLE(tex, samp, p.zy), scale);
    float3 ty = UnpackNormalScale(GRB_SAMPLE(tex, samp, uvY), scale);
    float3 tz = UnpackNormalScale(GRB_SAMPLE(tex, samp, p.xy), scale);
    tx = float3(tx.xy + n.zy, abs(tx.z) * n.x);
    ty = float3(ty.xy + n.xz, abs(ty.z) * n.y);
    tz = float3(tz.xy + n.xy, abs(tz.z) * n.z);
    return normalize(tx.zyx * w.x + ty.xzy * w.y + tz.xyz * w.z);
}

float3 GRB_Saturate(float3 c, float sat) { float l = GRB_Lum(c); return max(0.0, lerp(l.xxx, c, sat)); }

// How much of the rock's own normal detail shows through a covering layer: world noise -> contrast -> [min, max].
float GRB_Through(float3 pAbs, float scale, float contrast, float mn, float mx, float seed)
{
    float n = GRB_Fbm(pAbs * max(1e-3, scale) + seed);
    n = saturate((n - 0.5) * contrast + 0.5);
    return lerp(mn, mx, n);
}

// Covered surface normal: geometric normal + the layer's own detail + a share of the rock's detail (partial-derivative
// style blend in world space: both details are offsets from the geometric normal, so neither replaces the other).
float3 GRB_CoverNormal(float3 nGeo, float3 nRock, float3 nLayer, float layerStrength, float through)
{
    return normalize(nGeo + (nLayer - nGeo) * layerStrength + (nRock - nGeo) * through);
}

#define GRB_SAMPLE_GRAD(t, uv, dx, dy) SAMPLE_TEXTURE2D_GRAD(t, sampler_GRB_trilinear_repeat, uv, dx, dy)

#if defined(GRB_TEXARRAY_PASS) && !defined(SHADER_STAGE_RAY_TRACING)
// Base maps from the kit texture arrays: UV0 = the material's own UVs (tiling baked in), UV3.x = slice, UV3.y = group
// (0: group A arrays, 1: group B / 4K arrays). Explicit gradients: the group branch may diverge across a quad.
void GRB_ArraySurface(FragInputs input, inout SurfaceData s, inout float3 normalTS)
{
    float2 uv = input.texCoord0.xy;
    float slice = round(input.texCoord3.x);
    float2 dx = ddx(uv), dy = ddy(uv);
    float4 b, n, m;
    UNITY_BRANCH if (input.texCoord3.y > 0.5)
    {
        b = SAMPLE_TEXTURE2D_ARRAY_GRAD(_GRB_BaseArrayB, sampler_GRB_BaseArray, uv, slice, dx, dy);
        n = SAMPLE_TEXTURE2D_ARRAY_GRAD(_GRB_NormalArrayB, sampler_GRB_BaseArray, uv, slice, dx, dy);
        m = SAMPLE_TEXTURE2D_ARRAY_GRAD(_GRB_MaskArrayB, sampler_GRB_BaseArray, uv, slice, dx, dy);
    }
    else
    {
        b = SAMPLE_TEXTURE2D_ARRAY_GRAD(_GRB_BaseArray, sampler_GRB_BaseArray, uv, slice, dx, dy);
        n = SAMPLE_TEXTURE2D_ARRAY_GRAD(_GRB_NormalArray, sampler_GRB_BaseArray, uv, slice, dx, dy);
        m = SAMPLE_TEXTURE2D_ARRAY_GRAD(_GRB_MaskArray, sampler_GRB_BaseArray, uv, slice, dx, dy);
    }
    s.baseColor = b.rgb * _BaseColor.rgb;
    float3 nTS = UnpackNormalMapRGorAG(n, _NormalScale);
    #ifdef SURFACE_GRADIENT
    normalTS = SurfaceGradientFromTangentSpaceNormalAndFromTBN(nTS, input.tangentToWorld[0], input.tangentToWorld[1]);
    #else
    normalTS = nTS;
    #endif
    s.metallic = lerp(_MetallicRemapMin, _MetallicRemapMax, m.r);
    s.ambientOcclusion = lerp(_AORemapMin, _AORemapMax, m.g);
    s.perceptualSmoothness = lerp(_SmoothnessRemapMin, _SmoothnessRemapMax, m.a);
}
#endif

// ---- Per-pixel terrain match (TerrainLit's splat blend, triplanar on the rock) -------------------------------------
// The splat binds 26 textures (2 control maps + 8 layers x albedo / normal / mask). Light-loop passes (Forward,
// ForwardOnly, TransparentBackface) already use most of the 64-texture limit for HDRP's lighting resources, so the splat
// is compiled only into passes without the light loop (GBuffer, META...); forward passes use the baked dominant layer.
#if !defined(SHADER_STAGE_RAY_TRACING) && !defined(HAS_LIGHTLOOP)
#define GRB_TSPLAT 1
#endif
bool GRB_TerrainSplatOn() { return _GRB_TParams.x > 0.5 && _GRB_TerrainPerPixel > 0.5; }

float3 GRB_TriSampleGradRGB(TEXTURE2D_PARAM(tex, samp), float3 p, float2 tpm, float2 uvY, float2 gYx, float2 gYy, float3 dpx, float3 dpy, float3 w)
{
    float3 c = GRB_SAMPLE_GRAD(tex, uvY, gYx, gYy).rgb * w.y;
    c += GRB_SAMPLE_GRAD(tex, p.zy * tpm.yx, dpx.zy * tpm.yx, dpy.zy * tpm.yx).rgb * w.x;
    c += GRB_SAMPLE_GRAD(tex, p.xy * tpm, dpx.xy * tpm, dpy.xy * tpm).rgb * w.z;
    return c;
}

float3 GRB_TriNormalGrad(TEXTURE2D_PARAM(tex, samp), float3 p, float2 tpm, float2 uvY, float2 gYx, float2 gYy, float3 dpx, float3 dpy, float3 n, float3 w, float scale)
{
    float3 tx = UnpackNormalScale(GRB_SAMPLE_GRAD(tex, p.zy * tpm.yx, dpx.zy * tpm.yx, dpy.zy * tpm.yx), scale);
    float3 ty = UnpackNormalScale(GRB_SAMPLE_GRAD(tex, uvY, gYx, gYy), scale);
    float3 tz = UnpackNormalScale(GRB_SAMPLE_GRAD(tex, p.xy * tpm, dpx.xy * tpm, dpy.xy * tpm), scale);
    tx = float3(tx.xy + n.zy, abs(tx.z) * n.x);
    ty = float3(ty.xy + n.xz, abs(ty.z) * n.y);
    tz = float3(tz.xy + n.xy, abs(tz.z) * n.z);
    return normalize(tx.zyx * w.x + ty.xzy * w.y + tz.xyz * w.z);
}

// Layer i: Y projection = the terrain's own splat UV (lines up with the ground), X/Z projections at the same tiling.
#define GRB_TLAYER(i, wgt)                                                                                              \
    UNITY_BRANCH if (wgt > 0.0)                                                                                         \
    {                                                                                                                   \
        float4 st = _GRB_TST##i;                                                                                        \
        float2 uvY = tuv * st.xy + st.zw;                                                                               \
        float2 gYx = dtx * st.xy, gYy = dty * st.xy;                                                                    \
        float2 tpm = st.xy * _GRB_TRect.zw;                                                                             \
        float4 a4 = GRB_SAMPLE_GRAD(_GRB_TAlb##i, uvY, gYx, gYy);                                                       \
        alb[i] = GRB_TriSampleGradRGB(TEXTURE2D_ARGS(_GRB_TAlb##i, sampler_GRB_trilinear_repeat), pAbs, tpm, uvY, gYx, gYy, dpx, dpy, w) * _GRB_TDiff##i.rgb; \
        nrm[i] = GRB_TriNormalGrad(TEXTURE2D_ARGS(_GRB_TNrm##i, sampler_GRB_trilinear_repeat), pAbs, tpm, uvY, gYx, gYy, dpx, dpy, nGeo, w, _GRB_TLayer##i.w); \
        float4 dm = float4(_GRB_TLayer##i.y, _GRB_TMaskOff##i.y + _GRB_TMaskScale##i.y, _GRB_TMaskOff##i.z + 0.5 * _GRB_TMaskScale##i.z, a4.a * _GRB_TLayer##i.z); \
        float4 mm = GRB_SAMPLE_GRAD(_GRB_TMsk##i, uvY, gYx, gYy);                                                      \
        mm.b *= wgt;                                                                                                    \
        mm = mm * _GRB_TMaskScale##i + _GRB_TMaskOff##i;                                                                \
        msk[i] = lerp(dm, mm, _GRB_TLayer##i.x);                                                                        \
    }                                                                                                                   \
    else                                                                                                                \
    {                                                                                                                   \
        alb[i] = 0.0; nrm[i] = 0.0; msk[i] = float4(0.0, 1.0, _GRB_TMaskOff##i.z, 0.0);                                 \
    }

// Albedo / world normal / smoothness / metallic / AO of the terrain at this pixel's world XZ (outside the terrain: edge).
void GRB_TerrainSplat(float3 pAbs, float3 nGeo, float3 w, float3 dpx, float3 dpy,
                      out float3 oAlb, out float3 oN, out float oSmooth, out float oMetal, out float oAO)
{
    float2 tuv = saturate((pAbs.xz - _GRB_TRect.xy) * _GRB_TRect.zw);
    float2 dtx = dpx.xz * _GRB_TRect.zw, dty = dpy.xz * _GRB_TRect.zw;
    float2 cuv = (tuv * (_GRB_TControlTS.zw - 1.0) + 0.5) * _GRB_TControlTS.xy;
    float4 bm0 = SAMPLE_TEXTURE2D_LOD(_GRB_TControl0, sampler_GRB_linear_clamp, cuv, 0);
    float4 bm1 = _GRB_TParams.y > 4.5 ? SAMPLE_TEXTURE2D_LOD(_GRB_TControl1, sampler_GRB_linear_clamp, cuv, 0) : float4(0, 0, 0, 0);
    float3 alb[8]; float3 nrm[8]; float4 msk[8];
    GRB_TLAYER(0, bm0.x) GRB_TLAYER(1, bm0.y) GRB_TLAYER(2, bm0.z) GRB_TLAYER(3, bm0.w)
    GRB_TLAYER(4, bm1.x) GRB_TLAYER(5, bm1.y) GRB_TLAYER(6, bm1.z) GRB_TLAYER(7, bm1.w)
    if (_GRB_TParams.z > 0.5)
    {
        // TerrainLit _TERRAIN_BLEND_HEIGHT: all but the highest layer pushed below zero, + transition, normalized
        float maxH = msk[0].z;
        [unroll] for (int k = 1; k < 8; k++) maxH = max(maxH, msk[k].z);
        float tr = max(_GRB_TParams.w, 1e-5);
        float4 h0 = float4(msk[0].z, msk[1].z, msk[2].z, msk[3].z) - maxH;
        float4 h1 = float4(msk[4].z, msk[5].z, msk[6].z, msk[7].z) - maxH;
        h0 = (max(0.0, h0 + tr) + 1e-6) * bm0;
        h1 = (max(0.0, h1 + tr) + 1e-6) * bm1;
        float sum = dot(h0, float4(1, 1, 1, 1)) + dot(h1, float4(1, 1, 1, 1));
        bm0 = h0 / max(1e-6, sum); bm1 = h1 / max(1e-6, sum);
    }
    float wt[8] = { bm0.x, bm0.y, bm0.z, bm0.w, bm1.x, bm1.y, bm1.z, bm1.w };
    oAlb = 0.0; float3 nn = 0.0; float3 mo = 0.0;
    [unroll] for (int j = 0; j < 8; j++) { oAlb += alb[j] * wt[j]; nn += nrm[j] * wt[j]; mo += msk[j].xyw * wt[j]; }
    oN = dot(nn, nn) > 1e-8 ? normalize(nn) : nGeo;
    oMetal = mo.x; oAO = mo.y; oSmooth = mo.z;
}

// ---- Height above ground ---------------------------------------------------------------------------------------------
// Vertex A = sqrt(height above ground / 4 m) and vertex B = piece size / 8 m are written by PcgSurfaceSnap.ConformMesh with
// the fixed ranges PcgSurfaceSnap.VertexHeightRange / VertexSizeRange; the shader decodes with the same constants (the
// old per-material _GRB_HeightRange / _GRB_SizeRange could drift from the mesh encoding and scale every height band).
#define GRB_VTX_HEIGHT_RANGE 4.0
#define GRB_VTX_SIZE_RANGE 8.0
float GRB_PieceSize(float4 vc) { return vc.b * GRB_VTX_SIZE_RANGE; }

// Height of this pixel above the ground. With a terrain bound (PcgTerrainSplat) it is measured per pixel against the
// terrain heightmap, so the fade / contact band follows the real ground line. The vertex value is only a per-vertex sample
// interpolated across the triangle, and since it is sqrt-encoded, a long triangle running from the ground up the rock
// reads far too low in between (a 0.3 m band became ~1 m on low-poly pieces). It is used off-terrain and in ray tracing.
float GRB_HeightAboveGround(float3 pAbs, float4 vc)
{
    float h = vc.a * vc.a * GRB_VTX_HEIGHT_RANGE;
#if !defined(SHADER_STAGE_RAY_TRACING)
    float2 tuv = (pAbs.xz - _GRB_TRect.xy) * _GRB_TRect.zw;
    bool inside = _GRB_THeightP.z > 0.5 && all(tuv >= 0.0) && all(tuv <= 1.0);
    float2 huv = (saturate(tuv) * (_GRB_THeightTS.zw - 1.0) + 0.5) * _GRB_THeightTS.xy;
    float th = SAMPLE_TEXTURE2D_LOD(_GRB_THeightMap, s_linear_clamp_sampler, huv, 0).r * _GRB_THeightP.y + _GRB_THeightP.x;
    h = inside ? max(0.0, pAbs.y - th) : h;
#endif
    return h;
}

// ---- Ground fade (dithered opaque clip) + contact AO ---------------------------------------------------------------
// Visibility 0..1 of the rock surface near the ground: 0 at the ground .. 1 at _GRB_FadeHeight (capped on small pieces
// like the base blend so debris does not vanish), with world noise on the edge. Uses only vertex A/B + world position,
// so it is identical in every pass (depth, shadow, motion vectors, GBuffer / forward).
float GRB_FadeHeightFor(float pieceSize)
{
    float H = _GRB_FadeHeight;
    if (_GRB_BottomPieceScale > 0.0)
        H = min(H, pieceSize * _GRB_BottomPieceScale);
    return max(1e-3, H);
}

float GRB_FadeVis(float3 pAbs, float hag, float pieceSize)
{
    float H = GRB_FadeHeightFor(pieceSize);
    float n = 0.65 * GRB_ValueNoise(pAbs * _GRB_FadeNoiseScale + 5.3) + 0.35 * GRB_ValueNoise(pAbs * _GRB_FadeNoiseScale * 2.7 + 1.7);
    float h = hag + (n - 0.5) * 2.0 * _GRB_FadeNoiseStrength * H;
    return smoothstep(H * (1.0 - saturate(_GRB_FadeSoftness)), H, h);
}

// Wide, soft contact zone used for the AO fade: 1 at the ground .. 0 at fade height x _GRB_ContactAOWidth.
float GRB_ContactZone(float hag, float pieceSize)
{
    float H = GRB_FadeHeightFor(pieceSize) * max(1.0, _GRB_ContactAOWidth);
    return 1.0 - smoothstep(0.0, H, hag);
}

// 1 = keep the pixel, 0 = clip. Interleaved gradient noise in screen space, re-seeded every frame when TAA is on
// (_TaaFrameInfo.z = TAA frame index, .w = TAA enabled) so TAA resolves it into a soft fade; static pattern otherwise.
float GRB_GroundFadeAlpha(FragInputs input, PositionInputs posInput)
{
#if defined(SHADER_STAGE_RAY_TRACING)
    return 1.0; // no screen-space dither in ray tracing: keep the full rock
#else
    bool taa = _TaaFrameInfo.w > 0.5;
    if (!taa && _GRB_FadeNoTaaMode > 1.5)
        return 1.0; // editor toggle: no ground fade on cameras without TAA (Scene view)
    float3 pAbs = GetAbsolutePositionWS(posInput.positionWS);
    float4 vc = input.color;
    float f = GRB_FadeVis(pAbs, GRB_HeightAboveGround(pAbs, vc), GRB_PieceSize(vc));
    // Without TAA the dither cannot resolve: optional clean edge at the middle of the band instead of the static pattern.
    float d = (!taa && _GRB_FadeNoTaaMode > 0.5) ? 0.5 : InterleavedGradientNoise(posInput.positionSS.xy, taa ? (int)_TaaFrameInfo.z : 0);
    return f > d ? 1.0 : 0.0;
#endif
}

#if defined(HAS_LIGHTLOOP)
// Forward only: pre-compensate HDRP's SSAO in the contact zone so it ends at lerp(SSAO, 1, zone x (1 - keep)).
// HDRP later multiplies direct diffuse by lerp(1, ssao, _AmbientOcclusionParam.w) and baked/indirect diffuse by
// GTAOMultiBounce(min(materialAO, ssao), diffuseColor); both are solved exactly with an albedo scale (direct) and a
// bakeDiffuseLighting scale (indirect, which is multiplied by the albedo again later).
void GRB_ContactSSAO(FragInputs input, PositionInputs posInput, inout SurfaceData s, inout BuiltinData b)
{
    bool bottomOn = _GRB_BottomEnable > 0.5 && _GRB_BottomHeight > 0.001;
    if ((_GRB_FadeEnable < 0.5 && !bottomOn) || _GRB_ContactAOKeep > 0.999)
        return;
    float4 vc = input.color;
    float3 pAbs = GetAbsolutePositionWS(posInput.positionWS);
    float z = GRB_ContactZone(GRB_HeightAboveGround(pAbs, vc), GRB_PieceSize(vc)) * (1.0 - _GRB_ContactAOKeep);
    if (z <= 0.0)
        return;
    float ssao = GetScreenSpaceDiffuseOcclusion(posInput.positionSS.xy);
    float ssaoT = lerp(ssao, 1.0, z);
    float w = _AmbientOcclusionParam.w;
    float a = lerp(1.0, ssaoT, w) / max(0.05, lerp(1.0, ssao, w));
    float3 dc = s.baseColor * (1.0 - s.metallic);
    float3 iaoTarget = GTAOMultiBounce(min(s.ambientOcclusion, ssaoT), dc);
    float3 iaoHdrp = GTAOMultiBounce(min(s.ambientOcclusion, ssao), dc * a);
    s.baseColor *= a;
    b.bakeDiffuseLighting *= iaoTarget / max(0.02, iaoHdrp) / a;
}
#endif

// Vertex colour channels (written by PcgSurfaceSnap.ConformMesh on the live mesh, copied into bakes):
//   R legacy bottom mask, G up exposure, B piece size / 8 m, A sqrt(height above ground / 4 m) (see GRB_HeightAboveGround).
// Meshes without colours read (1,1,1,1): large piece, far above ground -> no base blend.
void GRB_Apply(FragInputs input, PositionInputs posInput, inout SurfaceData s)
{
    float3 pAbs = GetAbsolutePositionWS(posInput.positionWS);
    float3 nGeo = normalize(input.tangentToWorld[2]);
    float4 vc = input.color;
    float3 albedo = s.baseColor;
    float pieceSize = GRB_PieceSize(vc);
    float hag = GRB_HeightAboveGround(pAbs, vc);
    // Base brightness relative to the base map's average (set on bake): ~1 on average, independent of how dark the rock is.
    float detail = GRB_Lum(s.baseColor) / max(0.005, _GRB_BaseLumRef);
    float baseH = saturate(0.5 * detail);

    // Macro variation (large-scale world tint; hides per-piece repetition and seams between pieces).
    if (_GRB_MacroStrength > 0.0)
    {
        float m = GRB_Fbm(pAbs / max(0.1, _GRB_MacroScale));
        albedo *= lerp(1.0, 0.7 + 0.6 * m, _GRB_MacroStrength);
    }

    // World strata: absolute band colours (independent of the base brightness) modulated by the base texture detail,
    // varied band thickness, bedding lines with ledge relief (normal + smoothness), tilt and warp.
    if (_GRB_StrataEnable > 0.5)
    {
        float az = radians(_GRB_StrataAzimuth);
        float2 dir = float2(cos(az), sin(az));
        float sp = max(0.02, _GRB_StrataSpacing);
        float y = pAbs.y + dot(pAbs.xz, dir) * tan(radians(_GRB_StrataTilt))
                + (GRB_Fbm(pAbs * _GRB_StrataNoiseScale) - 0.5) * 2.0 * _GRB_StrataNoiseAmp;
        // thickness variation: a slow 1D warp along the band axis stretches some bands and squeezes others
        y += (GRB_ValueNoise(float3(y / sp * 0.37, 3.7, 1.3)) - 0.5) * 2.0 * _GRB_StrataThickVar * sp;
        float b = y / sp;
        float bi = floor(b);
        float bf = frac(b);
        float h = GRB_Hash11(bi);
        float h2 = GRB_Hash11(bi + 17.3);
        float edge = min(bf, 1.0 - bf) * sp;
        float lw = max(1e-3, _GRB_StrataLineWidth);
        float bedLine = 1.0 - smoothstep(0.0, lw, edge);
        float ledge = 1.0 - smoothstep(0.0, lw * 2.5, edge);
        float3 band = lerp(_GRB_StrataColorA.rgb, _GRB_StrataColorB.rgb, h);
        float3 bandAlb = band * lerp(1.0, clamp(detail, 0.2, 3.0), _GRB_StrataDetail);
        bandAlb *= 1.0 - bedLine * _GRB_StrataLineDarken;
        albedo = lerp(albedo, bandAlb, _GRB_StrataStrength);
        // Ledges: just above a bedding line the face tips up (lit), just below it tips down (shadowed); only on steep faces.
        float side = bf < 0.5 ? 1.0 : -1.0;
        float steep = 1.0 - abs(nGeo.y);
        s.normalWS = normalize(s.normalWS + float3(0.0, side * ledge * _GRB_StrataLedge * steep, 0.0));
        s.perceptualSmoothness = saturate(s.perceptualSmoothness + (h2 - 0.5) * 2.0 * _GRB_StrataSmoothVar * _GRB_StrataStrength
                                          - bedLine * 0.15 * _GRB_StrataLedge);
        s.ambientOcclusion *= 1.0 - bedLine * _GRB_StrataLineDarken * 0.5 * _GRB_StrataStrength;
    }

    // Top layer: slope cut-off on the geometric normal (vertical / blocky faces get none), crevice / ledge collection from the
    // base AO, height + noise breakup, piece-size suppression, tint A/B noise variation, saturation and brightness.
    if (_GRB_TopEnable > 0.5 && _GRB_TopCoverage > 0.001)
    {
        float3 w = GRB_TriWeights(nGeo);
        float3 tp = pAbs * _GRB_TopTiling;
        float tH = GRB_TriSample(TEXTURE2D_ARGS(_GRB_TopHeightMap, sampler_GRB_trilinear_repeat), tp, tp.xz, w).r;
#if !defined(SHADER_STAGE_RAY_TRACING)
        // Parallax (Top Height as a height map): shift the layer's world coordinates along the surface toward the viewer.
        if (_GRB_TopParallax > 0.0)
        {
            float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
            float vn = dot(V, nGeo);
            tp += (V - nGeo * vn) / max(0.25, vn) * ((tH - 0.5) * _GRB_TopParallax * _GRB_TopTiling);
        }
#endif
        float4 tAlb = GRB_TriSample(TEXTURE2D_ARGS(_GRB_TopBaseMap, sampler_GRB_trilinear_repeat), tp, tp.xz, w);
        tAlb.rgb *= _GRB_TopColor.rgb;
        // Optional mask (HDRP packing: R metallic, G AO, A smoothness) and AO map; without them the layer keeps the flat
        // Top Smoothness, metallic 0 and no AO (the auto flags are set by the inspector / layer sets when a map is assigned).
        float tSmooth = _GRB_TopSmoothness, tMetal = 0.0, tAO = 1.0;
        UNITY_BRANCH if (_GRB_TopMaskOn > 0.5)
        {
            float4 tM = GRB_TriSample(TEXTURE2D_ARGS(_GRB_TopMaskMap, sampler_GRB_trilinear_repeat), tp, tp.xz, w);
            tMetal = lerp(_GRB_TopMetallicMin, _GRB_TopMetallicMax, tM.r);
            tSmooth = lerp(_GRB_TopSmoothMin, _GRB_TopSmoothMax, tM.a);
            tAO = tM.g;
        }
        UNITY_BRANCH if (_GRB_TopAOOn > 0.5)
            tAO = GRB_TriSample(TEXTURE2D_ARGS(_GRB_TopAOMap, sampler_GRB_trilinear_repeat), tp, tp.xz, w).r; // AO map wins over mask G
        tAO = lerp(1.0, tAO, _GRB_TopAOStrength);
        float s0 = _GRB_TopSlopeStart, s1 = max(_GRB_TopSlopeStart + 0.01, _GRB_TopSlopeEnd);
        float upN = smoothstep(s0, s1, lerp(nGeo.y, s.normalWS.y, 0.35));
        float upV = smoothstep(s0, s1, vc.g * 2.0 - 1.0);
        float up = lerp(upN, upV, _GRB_TopVertexWeight);
        float n = GRB_Fbm(pAbs * _GRB_TopNoiseScale);
        float crev = saturate(1.0 - s.ambientOcclusion) * _GRB_TopCreviceBias;
        float m = up * (1.0 + crev) + (n - 0.5) * 2.0 * _GRB_TopNoiseStrength;
        float t = m - (1.0 - _GRB_TopCoverage);
        t += ((tH - 0.5) - (baseH - 0.5) * 0.5) * _GRB_TopHeightContrast * 0.5;
        float wt = saturate(t * _GRB_TopSharpness + 0.5) * saturate(_GRB_TopCoverage * 20.0);
        wt *= saturate(up * 4.0);
        if (_GRB_TopMinPieceSize > 0.0)
            wt *= smoothstep(_GRB_TopMinPieceSize, _GRB_TopMinPieceSize * 2.0, pieceSize);
        wt *= _GRB_TopMaxWeight;
        float v = GRB_Fbm(pAbs * _GRB_TopVarScale + 3.1);
        float3 col = tAlb.rgb * lerp(_GRB_TopTint.rgb, _GRB_TopTint2.rgb, saturate(v * 1.6 - 0.3));
        col = GRB_Saturate(col, _GRB_TopSaturation);
        col *= _GRB_TopBrightness * (1.0 + (GRB_Fbm(pAbs * _GRB_TopVarScale * 2.3 + 9.7) - 0.5) * 2.0 * _GRB_TopValueVar);
        float3 tN = GRB_TriNormal(TEXTURE2D_ARGS(_GRB_TopNormalMap, sampler_GRB_trilinear_repeat), tp, tp.xz, nGeo, w, _GRB_TopNormalScale);
        // the rock's own normal detail keeps showing through the layer: noise patches between smooth and bumpy
        float through = GRB_Through(pAbs, _GRB_TopThroughNoiseScale, _GRB_TopThroughContrast, _GRB_TopThroughMin, _GRB_TopThroughMax, 11.3);
        float3 layerN = GRB_CoverNormal(nGeo, s.normalWS, tN, _GRB_TopNormalBlend, through);
        albedo = lerp(albedo, col, wt);
        s.normalWS = normalize(lerp(s.normalWS, layerN, wt));
        s.perceptualSmoothness = lerp(s.perceptualSmoothness, tSmooth, wt);
        s.metallic = lerp(s.metallic, tMetal, wt);
        s.ambientOcclusion = lerp(s.ambientOcclusion, 1.0, wt * 0.5) * lerp(1.0, tAO, wt);
    }

    // Base terrain blend: height above ground (per-pixel terrain height, else vertex A), noisy + jittered contact line, sharpened,
    // capped on small pieces (blend height <= piece size x _GRB_BottomPieceScale) so debris stays readable.
    bool fadeOn = _GRB_FadeEnable > 0.5;
    bool bottomOn = _GRB_BottomEnable > 0.5 && _GRB_BottomHeight > 0.001;
    if (bottomOn || fadeOn)
    {
        float n = GRB_Fbm(pAbs * _GRB_BottomNoiseScale + 7.3);
        float hb = _GRB_BottomHeight * (1.0 + (n - 0.5) * 2.0 * _GRB_BottomNoiseStrength);
        hb += (GRB_ValueNoise(pAbs * _GRB_BottomEdgeNoiseScale + 2.1) - 0.5) * 2.0 * _GRB_BottomEdgeNoise;
        if (_GRB_BottomPieceScale > 0.0)
            hb = min(hb, pieceSize * _GRB_BottomPieceScale);
        float fo = max(0.005, _GRB_BottomFalloff);
        float wb = 1.0 - smoothstep(hb - fo * 0.5, hb + fo * 0.5, hag);
        wb *= lerp(1.0, saturate(s.normalWS.y * 0.5 + 0.75), _GRB_BottomUpBias);
        if (!bottomOn) wb = 0.0;
        // ground fade zone: the dithered pixels that survive take the terrain colour, so the rock dissolves into the ground
        float fadeZone = fadeOn ? (1.0 - GRB_FadeVis(pAbs, hag, pieceSize)) * _GRB_FadeTerrainColor : 0.0;
        float3 w = GRB_TriWeights(nGeo);
        float3 gAlb, gN;
        float gLum, gSmooth = _GRB_TerrainSmoothness, gMetal = 0.0;
#if defined(GRB_TSPLAT)
        if (GRB_TerrainSplatOn())
        {
            // per-pixel: the actual terrain under this pixel (follows the rock when it is moved)
            float3 dpx = ddx(pAbs), dpy = ddy(pAbs);
            gAlb = 0.0; gN = nGeo; float gAO = 1.0;
            UNITY_BRANCH if (wb > 0.0005 || fadeZone > 0.0005)
                GRB_TerrainSplat(pAbs, nGeo, w, dpx, dpy, gAlb, gN, gSmooth, gMetal, gAO);
            gLum = GRB_Lum(gAlb);
        }
        else
#endif
        {
            float3 tp = pAbs * _GRB_TerrainTiling;
            float2 uvY = pAbs.xz * _GRB_TerrainTiling + _GRB_TerrainUVOffset.xy;
            float4 g4 = GRB_TriSample(TEXTURE2D_ARGS(_GRB_TerrainBaseMap, sampler_GRB_trilinear_repeat), tp, uvY, w);
            gLum = GRB_Lum(g4.rgb);
            gAlb = g4.rgb * _GRB_TerrainTint.rgb;
            gN = GRB_TriNormal(TEXTURE2D_ARGS(_GRB_TerrainNormalMap, sampler_GRB_trilinear_repeat), tp, uvY, nGeo, w, _GRB_TerrainNormalScale);
        }
        // height-blend against the ground texture (bright grains fill first), then sharpen the contact
        wb = saturate(wb + (gLum - 0.35) * 0.6 * wb * (1.0 - wb));
        wb = saturate((wb - 0.5) * _GRB_BottomSharpness + 0.5) * step(0.001, wb);
        wb *= _GRB_BottomMaxWeight;
        float wbAO = wb;
        wb = max(wb, fadeZone);
        // terrain normal with a noise-modulated share of the rock's own normal detail
        float throughB = GRB_Through(pAbs, _GRB_BottomThroughNoiseScale, _GRB_BottomThroughContrast, _GRB_BottomThroughMin, _GRB_BottomThroughMax, 23.7);
        float3 groundN = GRB_CoverNormal(nGeo, s.normalWS, gN, 1.0, throughB);
        albedo = lerp(albedo, gAlb, wb);
        s.normalWS = normalize(lerp(s.normalWS, groundN, wb));
        // surviving dither pixels in the fade zone shade like the ground under them (normal toward world up)
        s.normalWS = normalize(lerp(s.normalWS, float3(0.0, 1.0, 0.0), fadeZone * _GRB_FadeNormalUp));
        s.perceptualSmoothness = lerp(s.perceptualSmoothness, gSmooth, wb);
        s.metallic = lerp(s.metallic, gMetal, wb);
        s.ambientOcclusion = lerp(s.ambientOcclusion, 1.0, wbAO * 0.5);
        // Contact AO Keep: fade the material / mask AO toward 1 over a wide, soft zone (0 = none at the ground, 1 = unchanged)
        s.ambientOcclusion = lerp(s.ambientOcclusion, 1.0, GRB_ContactZone(hag, pieceSize) * (1.0 - _GRB_ContactAOKeep));
    }

    s.baseColor = saturate(albedo);
}

#endif
