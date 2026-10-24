// DM copy of HDRP 17.4 TerrainLit (Dark Matter Genesis, 2026-10-24b). Package source untouched.
#if defined(_NORMALMAP) && defined(SURFACE_GRADIENT)
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
#endif

TEXTURE2D(_Control0);

#define DECLARE_TERRAIN_LAYER_TEXS(n)   \
    TEXTURE2D(_Splat##n);               \
    TEXTURE2D(_Normal##n);              \
    TEXTURE2D(_Mask##n)

DECLARE_TERRAIN_LAYER_TEXS(0);
DECLARE_TERRAIN_LAYER_TEXS(1);
DECLARE_TERRAIN_LAYER_TEXS(2);
DECLARE_TERRAIN_LAYER_TEXS(3);
#ifdef _TERRAIN_8_LAYERS
    DECLARE_TERRAIN_LAYER_TEXS(4);
    DECLARE_TERRAIN_LAYER_TEXS(5);
    DECLARE_TERRAIN_LAYER_TEXS(6);
    DECLARE_TERRAIN_LAYER_TEXS(7);
    TEXTURE2D(_Control1);
#endif

#undef DECLARE_TERRAIN_LAYER_TEXS

SAMPLER(sampler_Splat0);
SAMPLER(sampler_Control0);
float _TerrainBasemapDistance;

// ---------------- DM parallax / POM / anti-tile ----------------
#ifndef DM_TERRAIN_DATA
static float g_DMDepthFrac = 0.0;   // 24s: declared in DM_TerrainParallax_Data.hlsl for raster passes
static float g_DMDepthOffset = 0.0;
#endif
float _DMParallaxHeight, _DMHeightCenter, _DMOffsetBias, _DMParallaxFadeStart, _DMParallaxFadeEnd, _DMGrazingCos;
float _DMPOMSteps, _DMPOMLayers, _DMPOMFadeStart, _DMPOMFadeEnd;
float _DMLayerParallax0, _DMLayerParallax1, _DMLayerParallax2, _DMLayerParallax3;
float _DMLayerParallax4, _DMLayerParallax5, _DMLayerParallax6, _DMLayerParallax7;
float _DMAntiTileScale, _DMAntiTileStrength, _DMAntiTileNoiseScale;
float _DMHeightMapForBlend;
float _DMPOMMinSteps, _DMPOMRefine, _DMHeightMipBias, _DMSlopeFadeStart, _DMSlopeFadeEnd;
float _DMShadowStrength, _DMShadowSharpness, _DMShadowSteps, _DMShadowFadeStart, _DMShadowFadeEnd, _DMShadowLength;
float4 _DMSunDirFallback;
float _DMHeightAOStrength, _DMHeightAOMid;
float _DMLayerHeightAO0, _DMLayerHeightAO1, _DMLayerHeightAO2, _DMLayerHeightAO3, _DMLayerHeightAO4, _DMLayerHeightAO5, _DMLayerHeightAO6, _DMLayerHeightAO7;
TEXTURE2D(_DMHeightMap0); float _DMUseHeightMap0, _DMHeightScale0, _DMHeightOffset0, _DMHeightContrast0;
TEXTURE2D(_DMHeightMap1); float _DMUseHeightMap1, _DMHeightScale1, _DMHeightOffset1, _DMHeightContrast1;
TEXTURE2D(_DMHeightMap2); float _DMUseHeightMap2, _DMHeightScale2, _DMHeightOffset2, _DMHeightContrast2;
TEXTURE2D(_DMHeightMap3); float _DMUseHeightMap3, _DMHeightScale3, _DMHeightOffset3, _DMHeightContrast3;
TEXTURE2D(_DMHeightMap4); float _DMUseHeightMap4, _DMHeightScale4, _DMHeightOffset4, _DMHeightContrast4;
TEXTURE2D(_DMHeightMap5); float _DMUseHeightMap5, _DMHeightScale5, _DMHeightOffset5, _DMHeightContrast5;
TEXTURE2D(_DMHeightMap6); float _DMUseHeightMap6, _DMHeightScale6, _DMHeightOffset6, _DMHeightContrast6;
TEXTURE2D(_DMHeightMap7); float _DMUseHeightMap7, _DMHeightScale7, _DMHeightOffset7, _DMHeightContrast7;

float _DMPeakSoftness0, _DMPeakSoftness1, _DMPeakSoftness2, _DMPeakSoftness3, _DMPeakSoftness4, _DMPeakSoftness5, _DMPeakSoftness6, _DMPeakSoftness7;
float _DMPeakKnee0, _DMPeakKnee1, _DMPeakKnee2, _DMPeakKnee3, _DMPeakKnee4, _DMPeakKnee5, _DMPeakKnee6, _DMPeakKnee7;
float _DMPeakDetail0, _DMPeakDetail1, _DMPeakDetail2, _DMPeakDetail3, _DMPeakDetail4, _DMPeakDetail5, _DMPeakDetail6, _DMPeakDetail7;
float _DMPeakDetailScale0, _DMPeakDetailScale1, _DMPeakDetailScale2, _DMPeakDetailScale3, _DMPeakDetailScale4, _DMPeakDetailScale5, _DMPeakDetailScale6, _DMPeakDetailScale7;
// Current layer peak params (softness, knee, detail, detailScale); set per layer in SampleResults.
static float4 g_DMPeak = float4(0, 1, 0, 1);
float DM_ValueNoise(float2 p);

// Soft-knee shoulder above knee (softness 0 = identity) + procedural relief in the near-white region.
float DM_ApplyPeak(float h, float2 uv)
{
    float soft = g_DMPeak.x;
    float knee = min(g_DMPeak.y, 0.999);
    float det = g_DMPeak.z;
    float r = 1.0 - knee;
    float x = saturate((h - knee) / r);
    float y = (h > knee) ? knee + r * (x - 0.5 * soft * x * x) : h;
    float2 q = uv * g_DMPeak.w;
    float n = 1.0;
    UNITY_BRANCH if (det > 0.0)
        n = DM_ValueNoise(q) * 0.65 + DM_ValueNoise(q * 2.7 + 13.1) * 0.35;
    y -= smoothstep(knee, 1.0, h) * det * (1.0 - n) * max(r, 0.15);
    return saturate(y);
}

// Layer height: DM height map (R) if enabled, else mask B. Shared sampler (sampler_Splat0).
float DM_LayerHeight(TEXTURE2D_PARAM(mTex, s), TEXTURE2D(hTex), float useH, float3 hp, float2 uv, float2 dx, float2 dy)
{
    float h;
    UNITY_BRANCH if (useH > 0.5) h = SAMPLE_TEXTURE2D_GRAD(hTex, s, uv, dx, dy).r;
    else h = SAMPLE_TEXTURE2D_GRAD(mTex, s, uv, dx, dy).b;
    return DM_ApplyPeak(saturate(((h - 0.5) * hp.z + 0.5) * hp.x + hp.y), uv);
}
float4 _DMLayerTint0; float _DMLayerSaturation0, _DMLayerBrightness0;
float4 _DMLayerTint1; float _DMLayerSaturation1, _DMLayerBrightness1;
float4 _DMLayerTint2; float _DMLayerSaturation2, _DMLayerBrightness2;
float4 _DMLayerTint3; float _DMLayerSaturation3, _DMLayerBrightness3;
float4 _DMLayerTint4; float _DMLayerSaturation4, _DMLayerBrightness4;
float4 _DMLayerTint5; float _DMLayerSaturation5, _DMLayerBrightness5;
float4 _DMLayerTint6; float _DMLayerSaturation6, _DMLayerBrightness6;
float4 _DMLayerTint7; float _DMLayerSaturation7, _DMLayerBrightness7;

float3 DM_LayerColor(float3 c, float4 tint, float sat, float bright)
{
    float l = dot(c, float3(0.2126, 0.7152, 0.0722));
    return max(0, lerp(l.xxx, c, sat)) * bright * tint.rgb;
}

#if defined(DM_TERRAIN_DATA) && (defined(_DM_PARALLAX_OFFSET) || defined(_DM_PARALLAX_POM)) && !defined(SHADER_STAGE_RAY_TRACING)
    #define DM_PARALLAX_ACTIVE 1
#else
    #define DM_PARALLAX_ACTIVE 0
#endif
#if defined(DM_TERRAIN_DATA) && defined(_DM_ANTITILE) && !defined(SHADER_STAGE_RAY_TRACING)
    #define DM_ANTITILE_ACTIVE 1
#else
    #define DM_ANTITILE_ACTIVE 0
#endif

// Height = mask map B. Offset parallax with grazing bias.
float2 DM_ParallaxOffset(TEXTURE2D_PARAM(hTex, hSamp), TEXTURE2D(dmH), float useH, float3 hp, float2 uv, float2 dx, float2 dy, float3 vTS, float amp, float center)
{
    float h = DM_LayerHeight(TEXTURE2D_ARGS(hTex, hSamp), dmH, useH, hp, uv, dx, dy);
    g_DMDepthFrac = center - h; // 24s: depth below geometry in units of amp (+ = below)
    float2 p = vTS.xy / (vTS.z + _DMOffsetBias) * amp;
    return uv + p * (h - center);
}

// POM: linear search + binary refinement (_DMPOMRefine 0-8). Matches offset convention at h == center.
float2 DM_ParallaxPOM(TEXTURE2D_PARAM(hTex, hSamp), TEXTURE2D(dmH), float useH, float3 hp, float2 uv, float2 dx, float2 dy, float3 vTS, float amp, int steps, float center)
{
    steps = clamp(steps, 1, 64);
    float2 p = vTS.xy / max(vTS.z, 0.05) * amp;
    float2 uvTop = uv + p * (1.0 - center);
    float diff = DM_LayerHeight(TEXTURE2D_ARGS(hTex, hSamp), dmH, useH, hp, uvTop, dx, dy) - 1.0;
    g_DMDepthFrac = -(1.0 - center); // 24s: hit at the top
    if (diff >= 0.0) return uvTop;
    float stepT = 1.0 / steps;
    float t = 0.0, prevT = 0.0;
    UNITY_LOOP for (int k = 0; k < 64; k++)
    {
        if (k >= steps) break;
        prevT = t;
        t += stepT;
        diff = DM_LayerHeight(TEXTURE2D_ARGS(hTex, hSamp), dmH, useH, hp, uvTop - p * t, dx, dy) - (1.0 - t);
        if (diff >= 0.0) break;
    }
    float lo = prevT, hi = t;
    int refine = clamp((int)_DMPOMRefine, 0, 8);
    UNITY_LOOP for (int j = 0; j < 8; j++)
    {
        if (j >= refine) break;
        float mid = 0.5 * (lo + hi);
        float dm = DM_LayerHeight(TEXTURE2D_ARGS(hTex, hSamp), dmH, useH, hp, uvTop - p * mid, dx, dy) - (1.0 - mid);
        if (dm >= 0.0) hi = mid; else lo = mid;
    }
    g_DMDepthFrac = 0.5 * (lo + hi) - (1.0 - center); // 24s
    return uvTop - p * (0.5 * (lo + hi));
}

// Soft parallax self-shadow toward the light (tangent space). 1 = lit.
float DM_SelfShadow(TEXTURE2D_PARAM(hTex, hSamp), TEXTURE2D(dmH), float useH, float3 hp, float2 uv, float2 dx, float2 dy, float3 lTS, float amp)
{
    if (lTS.z <= 0.0) return 1.0;
    float h0 = DM_LayerHeight(TEXTURE2D_ARGS(hTex, hSamp), dmH, useH, hp, uv, dx, dy);
    int steps = clamp((int)_DMShadowSteps, 1, 32);
    float2 p = lTS.xy / max(lTS.z, 0.05) * amp * _DMShadowLength;
    float occ = 0.0;
    UNITY_LOOP for (int k = 1; k <= 32; k++)
    {
        if (k > steps) break;
        float f = (float)k / steps;
        float t = f * (1.0 - h0);
        float hs = DM_LayerHeight(TEXTURE2D_ARGS(hTex, hSamp), dmH, useH, hp, uv + p * t, dx, dy);
        occ = max(occ, (hs - (h0 + t)) * (1.0 - f));
    }
    return 1.0 - saturate(occ * _DMShadowSharpness);
}

float3 DM_MainLightDir()
{
    float3 dir = normalize(_DMSunDirFallback.xyz + float3(0, 1e-4, 0));
#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)
    if (_DirectionalLightCount > 0)
    {
        int idx = _DirectionalShadowIndex >= 0 ? _DirectionalShadowIndex : 0;
        dir = -_DirectionalLightDatas[idx].forward;
    }
#endif
    return dir;
}

float DM_Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float DM_ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = DM_Hash21(i);
    float b = DM_Hash21(i + float2(1, 0));
    float c = DM_Hash21(i + float2(0, 1));
    float d = DM_Hash21(i + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}
// ----------------------------------------------------------------

float GetSumHeight(float4 heights0, float4 heights1)
{
    float sumHeight = heights0.x;
    sumHeight += heights0.y;
    sumHeight += heights0.z;
    sumHeight += heights0.w;
    #ifdef _TERRAIN_8_LAYERS
        sumHeight += heights1.x;
        sumHeight += heights1.y;
        sumHeight += heights1.z;
        sumHeight += heights1.w;
    #endif
    return sumHeight;
}

#ifdef _NORMALMAP
float3 SampleNormalGrad(TEXTURE2D_PARAM(textureName, samplerName), float2 uv, float2 dxuv, float2 dyuv, float scale)
{
    float4 nrm = SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, uv, dxuv, dyuv);
#ifdef SURFACE_GRADIENT
    #ifdef UNITY_NO_DXT5nm
        return float3(UnpackDerivativeNormalRGB(nrm, scale), 0);
    #else
        return float3(UnpackDerivativeNormalRGorAG(nrm, scale), 0);
    #endif
#else
    #ifdef UNITY_NO_DXT5nm
        return UnpackNormalRGB(nrm, scale);
    #else
        return UnpackNormalMapRGorAG(nrm, scale);
    #endif
#endif
}
#endif

float4 RemapMasks(float4 masks, float blendMask, float4 remapOffset, float4 remapScale)
{
    float4 ret = masks;
    ret.b *= blendMask; // height needs to be weighted before remapping
    ret = ret * remapScale + remapOffset;
    return ret;
}

#ifdef OVERRIDE_SPLAT_SAMPLER_NAME
    #define sampler_Splat0 OVERRIDE_SPLAT_SAMPLER_NAME
    SAMPLER(OVERRIDE_SPLAT_SAMPLER_NAME);
#endif

void TerrainSplatBlend(float2 controlUV, float2 splatBaseUV, inout TerrainLitSurfaceData surfaceData)
{
    // TODO: triplanar
    // DM: parallax offset / POM applied per layer in SampleResults

    float4 albedo[_LAYER_COUNT];
    float3 normal[_LAYER_COUNT];
    float4 masks[_LAYER_COUNT];

#ifdef _NORMALMAP
    #define SampleNormal(i) SampleNormalGrad(_Normal##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv, _NormalScale##i)
#else
    #define SampleNormal(i) float3(0, 0, 0)
#endif

#define DefaultMask(i) float4(_Metallic##i, _MaskMapRemapOffset##i.y + _MaskMapRemapScale##i.y, _MaskMapRemapOffset##i.z + 0.5 * _MaskMapRemapScale##i.z, albedo[i].a * _Smoothness##i)

#ifdef _MASKMAP
    #define MaskModeMasks(i, blendMask) RemapMasks(SAMPLE_TEXTURE2D_GRAD(_Mask##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv), blendMask, _MaskMapRemapOffset##i, _MaskMapRemapScale##i)
#define SampleMasks(i, blendMask) lerp(DefaultMask(i), MaskModeMasks(i, blendMask), _LayerHasMask##i)
    #define NullMask(i)               float4(0, 1, _MaskMapRemapOffset##i.z, 0) // only height matters when weight is zero.
#else
    #define SampleMasks(i, blendMask) DefaultMask(i)
    #define NullMask(i)               float4(0, 1, 0, 0)
#endif

#if DM_PARALLAX_ACTIVE
    #ifdef _DM_SELFSHADOW
        #define DM_SELFSHADOW_LAYER(i, uv, dx, dy, amp) \
            UNITY_BRANCH if (i == dmTop0 && dmShadowFade > 0.0) \
                dmShadow = DM_SelfShadow(TEXTURE2D_ARGS(_Mask##i, sampler_Splat0), _DMHeightMap##i, _DMUseHeightMap##i, DM_HP(i), uv, dx * dmMip, dy * dmMip, dmLightTS, amp);
    #else
        #define DM_SELFSHADOW_LAYER(i, uv, dx, dy, amp)
    #endif
    // 24s: weighted POM depth (meters along view) for SV_Depth; amp in meters = height * layer strength * distance fade.
    #ifdef _DEPTHOFFSET_ON
        #define DM_ACCUM_DEPTH(i) g_DMDepthOffset += dmW[i] * g_DMDepthFrac * (_DMParallaxHeight * _DMLayerParallax##i * dmFade) / max(dmViewTS.z, 0.05);
    #else
        #define DM_ACCUM_DEPTH(i)
    #endif
    #define DM_APPLY_PARALLAX(i, uv, dx, dy) \
        UNITY_BRANCH if (_DMLayerParallax##i > 0.0 && (_LayerHasMask##i > 0.5 || _DMUseHeightMap##i > 0.5)) \
        { \
            float dmAmp0 = _DMParallaxHeight * _DMLayerParallax##i * dmUVPerM * _Splat##i##_ST.x; \
            float dmAmp = dmAmp0 * dmFade; \
            UNITY_BRANCH if (dmAmp > 0.0) \
            { \
                UNITY_BRANCH if (dmPomFade > 0.0 && (i == dmTop0 || i == dmTop1)) \
                    uv = DM_ParallaxPOM(TEXTURE2D_ARGS(_Mask##i, sampler_Splat0), _DMHeightMap##i, _DMUseHeightMap##i, DM_HP(i), uv, dx * dmMip, dy * dmMip, dmViewTS, dmAmp, dmSteps, _DMHeightCenter); \
                else \
                    uv = DM_ParallaxOffset(TEXTURE2D_ARGS(_Mask##i, sampler_Splat0), _DMHeightMap##i, _DMUseHeightMap##i, DM_HP(i), uv, dx * dmMip, dy * dmMip, dmViewTS, dmAmp, _DMHeightCenter); \
                DM_ACCUM_DEPTH(i) \
            } \
            DM_SELFSHADOW_LAYER(i, uv, dx, dy, dmAmp0) \
        }
#else
    #define DM_APPLY_PARALLAX(i, uv, dx, dy)
#endif
#if defined(DM_TERRAIN_DATA) && !defined(SHADER_STAGE_RAY_TRACING)
    #define DM_SET_PEAK(i) g_DMPeak = float4(_DMPeakSoftness##i, _DMPeakKnee##i, _DMPeakDetail##i, _DMPeakDetailScale##i);
#else
    #define DM_SET_PEAK(i)
#endif
#define DM_HP(i) float3(_DMHeightScale##i, _DMHeightOffset##i, _DMHeightContrast##i)
#if defined(DM_TERRAIN_DATA) && defined(_MASKMAP) && defined(_TERRAIN_BLEND_HEIGHT) && !defined(SHADER_STAGE_RAY_TRACING)
    #define DM_APPLY_BLENDHEIGHT(i, mask, uv, dx, dy) \
        UNITY_BRANCH if (_DMHeightMapForBlend > 0.5 && (_DMUseHeightMap##i > 0.5 || (_LayerHasMask##i > 0.5 && (_DMPeakSoftness##i > 0.0 || _DMPeakDetail##i > 0.0)))) \
            masks[i].z = DM_LayerHeight(TEXTURE2D_ARGS(_Mask##i, sampler_Splat0), _DMHeightMap##i, _DMUseHeightMap##i, DM_HP(i), uv, dx, dy) * mask * _MaskMapRemapScale##i.z + _MaskMapRemapOffset##i.z;
#else
    #define DM_APPLY_BLENDHEIGHT(i, mask, uv, dx, dy)
#endif
#if defined(DM_TERRAIN_DATA) && !defined(SHADER_STAGE_RAY_TRACING)
    #define DM_APPLY_HEIGHTAO(i, uv, dx, dy) \
        UNITY_BRANCH if (_DMHeightAOStrength > 0.0 && _DMLayerHeightAO##i > 0.0 && (_LayerHasMask##i > 0.5 || _DMUseHeightMap##i > 0.5)) \
        { \
            float dmHa = DM_LayerHeight(TEXTURE2D_ARGS(_Mask##i, sampler_Splat0), _DMHeightMap##i, _DMUseHeightMap##i, DM_HP(i), uv, dx, dy); \
            masks[i].y *= saturate(1.0 - (1.0 - smoothstep(0.0, max(_DMHeightAOMid, 1e-3), dmHa)) * _DMHeightAOStrength * _DMLayerHeightAO##i); \
        }
#else
    #define DM_APPLY_HEIGHTAO(i, uv, dx, dy)
#endif
#if DM_ANTITILE_ACTIVE
    #define DM_APPLY_ANTITILE(i, alb, uv, dx, dy) \
        { \
            float dmN2 = smoothstep(0.3, 0.7, DM_ValueNoise(dmAbsXZ * _DMAntiTileNoiseScale)) * _DMAntiTileStrength; \
            float4 dmA2 = SAMPLE_TEXTURE2D_GRAD(_Splat##i, sampler_Splat0, uv * _DMAntiTileScale + 0.37, dx * _DMAntiTileScale, dy * _DMAntiTileScale); \
            alb.rgb = lerp(alb.rgb, dmA2.rgb, saturate(dmN2)); \
        }
#else
    #define DM_APPLY_ANTITILE(i, alb, uv, dx, dy)
#endif

#define SampleResults(i, mask)                                                                                  \
    UNITY_BRANCH if (mask > 0)                                                                                  \
    {                                                                                                           \
        float2 splatuv = splatBaseUV * _Splat##i##_ST.xy + _Splat##i##_ST.zw;                                   \
        float2 splatdxuv = dxuv * _Splat##i##_ST.x;                                                             \
        float2 splatdyuv = dyuv * _Splat##i##_ST.y;                                                             \
        DM_SET_PEAK(i)                                                                                          \
        DM_APPLY_PARALLAX(i, splatuv, splatdxuv, splatdyuv)                                                     \
        albedo[i] = SAMPLE_TEXTURE2D_GRAD(_Splat##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv);            \
        DM_APPLY_ANTITILE(i, albedo[i], splatuv, splatdxuv, splatdyuv)                                          \
        albedo[i].rgb *= _DiffuseRemapScale##i.xyz;                                                             \
        albedo[i].rgb = DM_LayerColor(albedo[i].rgb, _DMLayerTint##i, _DMLayerSaturation##i, _DMLayerBrightness##i); \
        normal[i] = SampleNormal(i);                                                                            \
        masks[i] = SampleMasks(i, mask);                                                                        \
        DM_APPLY_BLENDHEIGHT(i, mask, splatuv, splatdxuv, splatdyuv)                                            \
        DM_APPLY_HEIGHTAO(i, splatuv, splatdxuv, splatdyuv)                                                     \
    }                                                                                                           \
    else                                                                                                        \
    {                                                                                                           \
        albedo[i] = float4(0, 0, 0, 0);                                                                         \
        normal[i] = float3(0, 0, 0);                                                                            \
        masks[i] = NullMask(i);                                                                                 \
    }

    // Derivatives are not available for ray tracing for now
#if defined(SHADER_STAGE_RAY_TRACING)
    float2 dxuv = 0;
    float2 dyuv = 0;
#else
    float2 dxuv = ddx(splatBaseUV);
    float2 dyuv = ddy(splatBaseUV);
#endif

    float2 blendUV0 = (controlUV.xy * (_Control0_TexelSize.zw - 1.0f) + 0.5f) * _Control0_TexelSize.xy;
    float4 blendMasks0 = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, blendUV0);
    #ifdef _TERRAIN_8_LAYERS
        float2 blendUV1 = (controlUV.xy * (_Control1_TexelSize.zw - 1.0f) + 0.5f) * _Control1_TexelSize.xy;
        float4 blendMasks1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, blendUV1);
    #else
        float4 blendMasks1 = float4(0, 0, 0, 0);
    #endif

#if DM_ANTITILE_ACTIVE
    float2 dmAbsXZ = GetAbsolutePositionWS(g_DMPosWS).xz;
#endif
#if DM_PARALLAX_ACTIVE
    float3 dmN = normalize(g_DMNormalWS);
    float3 dmT = cross(dmN, float3(0, 0, 1)); dmT *= rsqrt(max(dot(dmT, dmT), 1e-8));
    float3 dmB = cross(dmT, dmN);
    float3 dmV = normalize(g_DMViewWS);
    float3 dmViewTS = float3(dot(dmV, dmT), dot(dmV, dmB), saturate(dot(dmV, dmN)));
    float dmDist = length(g_DMPosWS); // camera-relative
    float dmFade = 1.0 - saturate((dmDist - _DMParallaxFadeStart) / max(_DMParallaxFadeEnd - _DMParallaxFadeStart, 0.01));
    dmFade *= saturate(dmViewTS.z / max(_DMGrazingCos, 1e-3));
    // slope fade: N.y (1 = flat). Off when start <= end.
    UNITY_BRANCH if (_DMSlopeFadeStart > _DMSlopeFadeEnd)
        dmFade *= saturate((dmN.y - _DMSlopeFadeEnd) / max(_DMSlopeFadeStart - _DMSlopeFadeEnd, 1e-3));
    float dmUVPerM = (length(dxuv) + length(dyuv)) / max(length(ddx(g_DMPosWS)) + length(ddy(g_DMPosWS)), 1e-6);
    float dmMip = exp2(_DMHeightMipBias);
    float dmPomFade = 0.0;
    int dmSteps = 1;
    int dmTop0 = -1;
    int dmTop1 = -1;
    float dmW[8] = { blendMasks0.x, blendMasks0.y, blendMasks0.z, blendMasks0.w, blendMasks1.x, blendMasks1.y, blendMasks1.z, blendMasks1.w };
    float dmBest0 = -1.0, dmBest1 = -1.0;
    UNITY_UNROLL for (int dk = 0; dk < _LAYER_COUNT; dk++)
    {
        float w = dmW[dk];
        if (w > dmBest0) { dmBest1 = dmBest0; dmTop1 = dmTop0; dmBest0 = w; dmTop0 = dk; }
        else if (w > dmBest1) { dmBest1 = w; dmTop1 = dk; }
    }
    if (_DMPOMLayers < 1.5) dmTop1 = -1;
    #ifdef _DM_PARALLAX_POM
        dmPomFade = 1.0 - saturate((dmDist - _DMPOMFadeStart) / max(_DMPOMFadeEnd - _DMPOMFadeStart, 0.01));
        // more steps at grazing view, fewer looking straight down
        dmSteps = max(1, (int)round(lerp(_DMPOMSteps, min(_DMPOMMinSteps, _DMPOMSteps), dmViewTS.z) * dmPomFade));
    #endif
    float dmShadow = 1.0;
    float3 dmL = DM_MainLightDir();
    float3 dmLightTS = float3(dot(dmL, dmT), dot(dmL, dmB), dot(dmL, dmN));
    float dmShadowFade = (_DMShadowStrength > 0.0) ? 1.0 - saturate((dmDist - _DMShadowFadeStart) / max(_DMShadowFadeEnd - _DMShadowFadeStart, 0.01)) : 0.0;
#endif

    SampleResults(0, blendMasks0.x);
    SampleResults(1, blendMasks0.y);
    SampleResults(2, blendMasks0.z);
    SampleResults(3, blendMasks0.w);
    #ifdef _TERRAIN_8_LAYERS
        SampleResults(4, blendMasks1.x);
        SampleResults(5, blendMasks1.y);
        SampleResults(6, blendMasks1.z);
        SampleResults(7, blendMasks1.w);
    #endif

#undef SampleNormal
#undef SampleMasks
#undef SampleResults
#undef DM_APPLY_PARALLAX
#undef DM_APPLY_ANTITILE
#undef DM_APPLY_BLENDHEIGHT
#undef DM_APPLY_HEIGHTAO
#undef DM_SET_PEAK
#undef DM_SELFSHADOW_LAYER
#undef DM_ACCUM_DEPTH

    float weights[_LAYER_COUNT];
    ZERO_INITIALIZE_ARRAY(float, weights, _LAYER_COUNT);

    #ifdef _MASKMAP
        #if defined(_TERRAIN_BLEND_HEIGHT)
            // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
            float maxHeight = masks[0].z;
            maxHeight = max(maxHeight, masks[1].z);
            maxHeight = max(maxHeight, masks[2].z);
            maxHeight = max(maxHeight, masks[3].z);
            #ifdef _TERRAIN_8_LAYERS
                maxHeight = max(maxHeight, masks[4].z);
                maxHeight = max(maxHeight, masks[5].z);
                maxHeight = max(maxHeight, masks[6].z);
                maxHeight = max(maxHeight, masks[7].z);
            #endif

            // Make sure that transition is not zero otherwise the next computation will be wrong.
            // The epsilon here also has to be bigger than the epsilon in the next computation.
            float transition = max(_HeightTransition, 1e-5);

            // The goal here is to have all but the highest layer at negative heights, then we add the transition so that if the next highest layer is near transition it will have a positive value.
            // Then we clamp this to zero and normalize everything so that highest layer has a value of 1.
            float4 weightedHeights0 = { masks[0].z, masks[1].z, masks[2].z, masks[3].z };
            weightedHeights0 = weightedHeights0 - maxHeight.xxxx;
            // We need to add an epsilon here for active layers (hence the blendMask again) so that at least a layer shows up if everything's too low.
            weightedHeights0 = (max(0, weightedHeights0 + transition) + 1e-6) * blendMasks0;

            #ifdef _TERRAIN_8_LAYERS
                float4 weightedHeights1 = { masks[4].z, masks[5].z, masks[6].z, masks[7].z };
                weightedHeights1 = weightedHeights1 - maxHeight.xxxx;
                weightedHeights1 = (max(0, weightedHeights1 + transition) + 1e-6) * blendMasks1;
            #else
                float4 weightedHeights1 = { 0, 0, 0, 0 };
            #endif

            // Normalize
            float sumHeight = GetSumHeight(weightedHeights0, weightedHeights1);
            blendMasks0 = weightedHeights0 / sumHeight.xxxx;
            #ifdef _TERRAIN_8_LAYERS
                blendMasks1 = weightedHeights1 / sumHeight.xxxx;
            #endif
        #elif defined(_TERRAIN_BLEND_DENSITY)
            // Denser layers are more visible.
            float4 opacityAsDensity0 = saturate((float4(albedo[0].a, albedo[1].a, albedo[2].a, albedo[3].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks0)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
            opacityAsDensity0 += 0.001f * blendMasks0;      // if all weights are zero, default to what the blend mask says
            float4 useOpacityAsDensityParam0 = { _DiffuseRemapScale0.w, _DiffuseRemapScale1.w, _DiffuseRemapScale2.w, _DiffuseRemapScale3.w }; // 1 is off
            blendMasks0 = lerp(opacityAsDensity0, blendMasks0, useOpacityAsDensityParam0);
            #ifdef _TERRAIN_8_LAYERS
                float4 opacityAsDensity1 = saturate((float4(albedo[4].a, albedo[5].a, albedo[6].a, albedo[7].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks1)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
                opacityAsDensity1 += 0.001f * blendMasks1;  // if all weights are zero, default to what the blend mask says
                float4 useOpacityAsDensityParam1 = { _DiffuseRemapScale4.w, _DiffuseRemapScale5.w, _DiffuseRemapScale6.w, _DiffuseRemapScale7.w };
                blendMasks1 = lerp(opacityAsDensity1, blendMasks1, useOpacityAsDensityParam1);
            #endif

            // Normalize
            float sumHeight = GetSumHeight(blendMasks0, blendMasks1);
            blendMasks0 = blendMasks0 / sumHeight.xxxx;
            #ifdef _TERRAIN_8_LAYERS
                blendMasks1 = blendMasks1 / sumHeight.xxxx;
            #endif
        #endif // if _TERRAIN_BLEND_HEIGHT
    #endif // if _MASKMAP

    weights[0] = blendMasks0.x;
    weights[1] = blendMasks0.y;
    weights[2] = blendMasks0.z;
    weights[3] = blendMasks0.w;
    #ifdef _TERRAIN_8_LAYERS
        weights[4] = blendMasks1.x;
        weights[5] = blendMasks1.y;
        weights[6] = blendMasks1.z;
        weights[7] = blendMasks1.w;
    #endif

    surfaceData.albedo = 0;
    surfaceData.normalData = 0;
    float3 outMasks = 0;
    UNITY_UNROLL for (int i = 0; i < _LAYER_COUNT; ++i)
    {
        surfaceData.albedo += albedo[i].rgb * weights[i];
        surfaceData.normalData += normal[i].rgb * weights[i]; // no need to normalize
        outMasks += masks[i].xyw * weights[i];
    }
    surfaceData.smoothness = outMasks.z;
    surfaceData.metallic = outMasks.x;
    surfaceData.ao = outMasks.y;
#if DM_PARALLAX_ACTIVE && defined(_DM_SELFSHADOW)
    float dmShade = saturate(1.0 - (1.0 - dmShadow) * _DMShadowStrength * dmShadowFade);
    surfaceData.albedo *= dmShade;
    surfaceData.ao *= lerp(1.0, dmShade, 0.5);
#endif
}

void TerrainLitShade(float2 uv, inout TerrainLitSurfaceData surfaceData)
{
    TerrainSplatBlend(uv, uv, surfaceData);
}

void TerrainLitDebug(float2 uv, uint2 screenSpaceCoords, out float3 baseColor)
{
#ifdef DEBUG_DISPLAY
    if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_CONTROL)
    {
        baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv, _Control0);
    }
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER0)
    {
        baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat0_ST.xy + _Splat0_ST.zw, _Splat0);
    }
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER1)
    {
        baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat1_ST.xy + _Splat1_ST.zw, _Splat1);
    }
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER2)
    {
        baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat2_ST.xy + _Splat2_ST.zw, _Splat2);
    }
    else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER3)
    {
        baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat3_ST.xy + _Splat3_ST.zw, _Splat3);
    }
    #ifdef _TERRAIN_8_LAYERS
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER4)
        {
            baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat4_ST.xy + _Splat4_ST.zw, _Splat4);
        }
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER5)
        {
            baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat5_ST.xy + _Splat5_ST.zw, _Splat5);
        }
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER6)
        {
            baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat6_ST.xy + _Splat6_ST.zw, _Splat6);
        }
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER7)
        {
            baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv * _Splat7_ST.xy + _Splat7_ST.zw, _Splat7);
        }
    #else
        else
        {
            // User is trying to debug layer 4/5/6/7 but this terrain only has 4 layers: let's try to display some basic "invalid" debug info...
            baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_NO_TEX(screenSpaceCoords, uv);
        }
    #endif
#endif
}
