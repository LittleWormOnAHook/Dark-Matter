#ifndef AG_HDRP_UNITY6_COMPAT_INCLUDED
#define AG_HDRP_UNITY6_COMPAT_INCLUDED

// Unity 6 HDRP compatibility for ANGRY MESH Nature Pack Amplify shaders (HDRP 7.x era).

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

#endif
