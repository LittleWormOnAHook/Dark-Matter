#ifndef AG_HDRP_UNITY6_COMPAT_INCLUDED
#define AG_HDRP_UNITY6_COMPAT_INCLUDED

// Unity 6 HDRP compatibility for ANGRY MESH Nature Pack Amplify shaders (HDRP 7.x era).

// Current HDRP Lit deliberately uses full precision.  These shaders were
// generated before that safeguard was added, and half precision can corrupt
// camera-relative positions and lighting on large scenes.
#ifndef PREFER_HALF
    #define PREFER_HALF 0
#endif

#if !defined(AREA_SHADOW_LOW) && !defined(AREA_SHADOW_MEDIUM) && !defined(AREA_SHADOW_HIGH)
    #if defined(SHADOW_LOW)
        #define AREA_SHADOW_LOW
    #elif defined(SHADOW_HIGH)
        #define AREA_SHADOW_HIGH
    #else
        #define AREA_SHADOW_MEDIUM
    #endif
#endif

#ifndef UnpackNormalmapRGorAG
float3 UnpackNormalmapRGorAG(float4 packedNormal, float scale)
{
    packedNormal.a *= packedNormal.r;
    float3 normal;
    normal.xy = packedNormal.ag * 2.0 - 1.0;
    normal.z = sqrt(max(1e-16, 1.0 - saturate(dot(normal.xy, normal.xy))));
    normal.xy *= scale;
    return normal;
}
#endif

// The original shaders expect AG Global Settings to initialize the snow-height
// globals. When that component is absent, Unity supplies zero for both values
// and the generated division becomes 0 / 0 at world Y=0.
// Treat an uninitialized/degenerate fade as "no height restriction" while
// preserving the authored height fade whenever a valid range is supplied.
float AGSafeSnowHeightMask(float worldY, float minimumHeight, float fadeHeight)
{
    if (abs(fadeHeight) < 1e-4)
        return 1.0;

    return saturate((worldY - minimumHeight) / abs(fadeHeight));
}

#endif
