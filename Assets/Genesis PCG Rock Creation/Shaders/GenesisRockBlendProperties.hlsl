// Genesis PCG Rock Creation: copy of HDRP 17.4 LitProperties.hlsl with the Rock Blend parameters added
// (regenerate from the installed HDRP when upgrading HDRP).
// ===========================================================================
//                              WARNING:
// On PS4, texture/sampler declarations need to be outside of CBuffers
// Otherwise those parameters are not bound correctly at runtime.
// ===========================================================================

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

TEXTURE2D(_EmissiveColorMap);
SAMPLER(sampler_EmissiveColorMap);

#ifndef LAYERED_LIT_SHADER

TEXTURE2D(_DiffuseLightingMap);
SAMPLER(sampler_DiffuseLightingMap);

TEXTURE2D(_BaseColorMap);
SAMPLER(sampler_BaseColorMap);

TEXTURE2D(_MaskMap);
SAMPLER(sampler_MaskMap);
TEXTURE2D(_BentNormalMap); // Reuse sampler from normal map
SAMPLER(sampler_BentNormalMap);
TEXTURE2D(_BentNormalMapOS);
SAMPLER(sampler_BentNormalMapOS);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_NormalMapOS);
SAMPLER(sampler_NormalMapOS);

TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);

TEXTURE2D(_HeightMap);
SAMPLER(sampler_HeightMap);

TEXTURE2D(_TangentMap);
SAMPLER(sampler_TangentMap);
TEXTURE2D(_TangentMapOS);
SAMPLER(sampler_TangentMapOS);

TEXTURE2D(_AnisotropyMap);
SAMPLER(sampler_AnisotropyMap);

TEXTURE2D(_SubsurfaceMaskMap);
SAMPLER(sampler_SubsurfaceMaskMap);
TEXTURE2D(_TransmissionMaskMap);
SAMPLER(sampler_TransmissionMaskMap);
TEXTURE2D(_ThicknessMap);
SAMPLER(sampler_ThicknessMap);

TEXTURE2D(_IridescenceThicknessMap);
SAMPLER(sampler_IridescenceThicknessMap);

TEXTURE2D(_IridescenceMaskMap);
SAMPLER(sampler_IridescenceMaskMap);

TEXTURE2D(_SpecularColorMap);
SAMPLER(sampler_SpecularColorMap);

TEXTURE2D(_TransmittanceColorMap);
SAMPLER(sampler_TransmittanceColorMap);

TEXTURE2D(_CoatMaskMap);
SAMPLER(sampler_CoatMaskMap);

#else

// Set of users variables
#define PROP_DECL(type, name) type name##0, name##1, name##2, name##3
// sampler are share by texture type inside a layered material but we need to support that a particualr layer have no texture, so we take the first sampler of available texture as the share one
// mean we must declare all sampler
#define PROP_DECL_TEX2D(name)\
    TEXTURE2D(CALL_MERGE_NAME(name, 0)); \
    SAMPLER(CALL_MERGE_NAME(CALL_MERGE_NAME(sampler, name), 0)); \
    TEXTURE2D(CALL_MERGE_NAME(name, 1)); \
    SAMPLER(CALL_MERGE_NAME(CALL_MERGE_NAME(sampler, name), 1)); \
    TEXTURE2D(CALL_MERGE_NAME(name, 2)); \
    SAMPLER(CALL_MERGE_NAME(CALL_MERGE_NAME(sampler, name), 2)); \
    TEXTURE2D(CALL_MERGE_NAME(name, 3)); \
    SAMPLER(CALL_MERGE_NAME(CALL_MERGE_NAME(sampler, name), 3))


PROP_DECL_TEX2D(_BaseColorMap);
PROP_DECL_TEX2D(_MaskMap);
PROP_DECL_TEX2D(_BentNormalMap);
PROP_DECL_TEX2D(_BentNormalMapOS);
PROP_DECL_TEX2D(_NormalMap);
PROP_DECL_TEX2D(_NormalMapOS);
PROP_DECL_TEX2D(_DetailMap);
PROP_DECL_TEX2D(_HeightMap);

PROP_DECL_TEX2D(_SubsurfaceMaskMap);
PROP_DECL_TEX2D(_TransmissionMaskMap);
PROP_DECL_TEX2D(_ThicknessMap);

TEXTURE2D(_LayerMaskMap);
SAMPLER(sampler_LayerMaskMap);
TEXTURE2D(_LayerInfluenceMaskMap);
SAMPLER(sampler_LayerInfluenceMaskMap);

#endif

// ---- Genesis Rock Blend textures (one inline sampler) ----
// Inline sampler (filter + wrap in the name): stays valid when a pass strips one of the textures.
SAMPLER(sampler_GRB_trilinear_repeat);
TEXTURE2D(_GRB_TopBaseMap);
TEXTURE2D(_GRB_TopNormalMap);
TEXTURE2D(_GRB_TopHeightMap);
TEXTURE2D(_GRB_TopMaskMap);
TEXTURE2D(_GRB_TopAOMap);
TEXTURE2D(_GRB_TerrainBaseMap);
TEXTURE2D(_GRB_TerrainNormalMap);
// Base texture arrays (bake "Texture Array" mode): group A (<= 2K slices) and group B (4K slices); one sampler.
TEXTURE2D_ARRAY(_GRB_BaseArray);
TEXTURE2D_ARRAY(_GRB_NormalArray);
TEXTURE2D_ARRAY(_GRB_MaskArray);
TEXTURE2D_ARRAY(_GRB_BaseArrayB);
TEXTURE2D_ARRAY(_GRB_NormalArrayB);
TEXTURE2D_ARRAY(_GRB_MaskArrayB);
SAMPLER(sampler_GRB_BaseArray);
// Per-pixel terrain match: the terrain under the rock (control maps + up to 8 layers), set per renderer by
// PcgTerrainSplat through a MaterialPropertyBlock (globals, not material properties).
SAMPLER(sampler_GRB_linear_clamp);
TEXTURE2D(_GRB_TControl0); TEXTURE2D(_GRB_TControl1);
TEXTURE2D(_GRB_TAlb0); TEXTURE2D(_GRB_TNrm0); TEXTURE2D(_GRB_TMsk0);
TEXTURE2D(_GRB_TAlb1); TEXTURE2D(_GRB_TNrm1); TEXTURE2D(_GRB_TMsk1);
TEXTURE2D(_GRB_TAlb2); TEXTURE2D(_GRB_TNrm2); TEXTURE2D(_GRB_TMsk2);
TEXTURE2D(_GRB_TAlb3); TEXTURE2D(_GRB_TNrm3); TEXTURE2D(_GRB_TMsk3);
TEXTURE2D(_GRB_TAlb4); TEXTURE2D(_GRB_TNrm4); TEXTURE2D(_GRB_TMsk4);
TEXTURE2D(_GRB_TAlb5); TEXTURE2D(_GRB_TNrm5); TEXTURE2D(_GRB_TMsk5);
TEXTURE2D(_GRB_TAlb6); TEXTURE2D(_GRB_TNrm6); TEXTURE2D(_GRB_TMsk6);
TEXTURE2D(_GRB_TAlb7); TEXTURE2D(_GRB_TNrm7); TEXTURE2D(_GRB_TMsk7);
// (float4 terrain values live in UnityPerMaterial below: PcgTerrainSplat writes them to shared runtime materials in Play.)
// Per-pixel height above the terrain (ground fade / contact AO / base blend), bound with the splat by PcgTerrainSplat.
TEXTURE2D(_GRB_THeightMap);  // TerrainData.heightmapTexture (R16, normalized)
// Global (Shader.SetGlobalFloat, editor toggle): ground fade on cameras WITHOUT TAA. 0 dither, 1 hard edge, 2 off.
float _GRB_FadeNoTaaMode;

CBUFFER_START(UnityPerMaterial)

// shared constant between lit and layered lit
float _AlphaCutoff;
float _UseShadowThreshold;
float _AlphaCutoffShadow;
float _AlphaCutoffPrepass;
float _AlphaCutoffPostpass;
float4 _DoubleSidedConstants;
float _BlendMode;
float _EnableBlendModePreserveSpecularLighting;

float _PPDMaxSamples;
float _PPDMinSamples;
float _PPDLodThreshold;

float3 _EmissiveColor;
float _AlbedoAffectEmissive;
float _EmissiveExposureWeight;

int  _SpecularOcclusionMode;

// Transparency
float3 _TransmittanceColor;
float _Ior;
float _ATDistance;

// Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
// value that exist to identify if the GI emission need to be enabled.
// In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
// TODO: Fix the code in legacy unity so we can customize the beahvior for GI
float3 _EmissionColor;
float4 _EmissiveColorMap_ST;
float _TexWorldScaleEmissive;
float4 _UVMappingMaskEmissive;
float _ObjectSpaceUVMappingEmissive;

float4 _InvPrimScale; // Only XY are used

// Specular AA
float _EnableGeometricSpecularAA;
float _SpecularAAScreenSpaceVariance;
float _SpecularAAThreshold;

// Raytracing
float _RayTracing;

#ifndef LAYERED_LIT_SHADER

// Set of users variables
float4 _BaseColor;
float4 _BaseColorMap_ST;
float4 _BaseColorMap_TexelSize;

float _Metallic;
float _MetallicRemapMin;
float _MetallicRemapMax;
float _Smoothness;
float _SmoothnessRemapMin;
float _SmoothnessRemapMax;
float _AlphaRemapMin;
float _AlphaRemapMax;
float _AORemapMin;
float _AORemapMax;

float _NormalScale;

float4 _DetailMap_ST;
float _DetailAlbedoScale;
float _DetailNormalScale;
float _DetailSmoothnessScale;

float4 _HeightMap_TexelSize; // Unity facility. This will provide the size of the heightmap to the shader

float _HeightAmplitude;
float _HeightCenter;

float _Anisotropy;

float _DiffusionProfileHash;
float _SubsurfaceMask;
float _TransmissionMask;
float _Thickness;
float4 _ThicknessRemap;


float _IridescenceThickness;
float4 _IridescenceThicknessRemap;
float _IridescenceMask;

float _CoatMask;

float4 _SpecularColor;
float _EnergyConservingSpecularColor;

float _TexWorldScale;
float _InvTilingScale;
float4 _UVMappingMask;
float4 _UVDetailsMappingMask;
float _LinkDetailsWithBase;
float _ObjectSpaceUVMapping;

#else // LAYERED_LIT_SHADER

// Set of users variables
PROP_DECL(float4, _BaseColor);
float4 _BaseColorMap0_ST;
float4 _BaseColorMap1_ST;
float4 _BaseColorMap2_ST;
float4 _BaseColorMap3_ST;

float4 _BaseColorMap0_TexelSize;

PROP_DECL(float, _Metallic);
PROP_DECL(float, _MetallicRemapMin);
PROP_DECL(float, _MetallicRemapMax);
PROP_DECL(float, _Smoothness);
PROP_DECL(float, _SmoothnessRemapMin);
PROP_DECL(float, _SmoothnessRemapMax);
PROP_DECL(float, _AlphaRemapMin);
PROP_DECL(float, _AlphaRemapMax);
PROP_DECL(float, _AORemapMin);
PROP_DECL(float, _AORemapMax);

PROP_DECL(float, _NormalScale);
float4 _NormalMap0_TexelSize; // Unity facility. This will provide the size of the base normal to the shader

float4 _HeightMap0_TexelSize;
float4 _HeightMap1_TexelSize;
float4 _HeightMap2_TexelSize;
float4 _HeightMap3_TexelSize;

float4 _DetailMap0_ST;
float4 _DetailMap1_ST;
float4 _DetailMap2_ST;
float4 _DetailMap3_ST;
PROP_DECL(float, _UVDetail);
PROP_DECL(float, _DetailAlbedoScale);
PROP_DECL(float, _DetailNormalScale);
PROP_DECL(float, _DetailSmoothnessScale);

PROP_DECL(float, _HeightAmplitude);
PROP_DECL(float, _HeightCenter);

PROP_DECL(float, _DiffusionProfileHash);
PROP_DECL(float, _SubsurfaceMask);
PROP_DECL(float, _TransmissionMask);
PROP_DECL(float, _Thickness);
PROP_DECL(float4, _ThicknessRemap);

PROP_DECL(float, _OpacityAsDensity);
float _InheritBaseNormal1;
float _InheritBaseNormal2;
float _InheritBaseNormal3;
float _InheritBaseHeight1;
float _InheritBaseHeight2;
float _InheritBaseHeight3;
float _InheritBaseColor1;
float _InheritBaseColor2;
float _InheritBaseColor3;
PROP_DECL(float, _HeightOffset);
float _HeightTransition;

float4 _LayerMaskMap_ST;
float _TexWorldScaleBlendMask;
PROP_DECL(float, _TexWorldScale);
PROP_DECL(float, _InvTilingScale);
float4 _UVMappingMaskBlendMask;
PROP_DECL(float4, _UVMappingMask);
PROP_DECL(float4, _UVDetailsMappingMask);
PROP_DECL(float, _LinkDetailsWithBase);

#endif // LAYERED_LIT_SHADER

// Tessellation specific

#ifdef TESSELLATION_ON
float _TessellationFactor;
float _TessellationFactorMinDistance;
float _TessellationFactorMaxDistance;
float _TessellationFactorTriangleSize;
float _TessellationShapeFactor;
float _TessellationBackFaceCullEpsilon;
#endif



// ---- Genesis Rock Blend (inside UnityPerMaterial: SRP Batcher compatible) ----
float _GRB_HeightRange;
float _GRB_SizeRange;
float _GRB_BaseLumRef;
float _GRB_VColorRange;
float _GRB_TopEnable;
float _GRB_TopFromTerrain;
float4 _GRB_TopTint;
float4 _GRB_TopTint2;
float _GRB_TopVarScale;
float _GRB_TopValueVar;
float _GRB_TopSaturation;
float _GRB_TopBrightness;
float _GRB_TopTiling;
float _GRB_TopNormalScale;
float _GRB_TopSmoothness;
float _GRB_TopCoverage;
float _GRB_TopSharpness;
float _GRB_TopSlopeStart;
float _GRB_TopSlopeEnd;
float _GRB_TopCreviceBias;
float _GRB_TopHeightContrast;
float _GRB_TopVertexWeight;
float _GRB_TopNoiseScale;
float _GRB_TopNoiseStrength;
float _GRB_TopNormalBlend;
float _GRB_TopMaxWeight;
float _GRB_TopMinPieceSize;
float _GRB_BottomEnable;
float _GRB_TerrainAuto;
float4 _GRB_TerrainTint;
float _GRB_TerrainTiling;
float4 _GRB_TerrainUVOffset;
float _GRB_TerrainNormalScale;
float _GRB_TerrainSmoothness;
float _GRB_BottomHeight;
float _GRB_BottomFalloff;
float _GRB_BottomSharpness;
float _GRB_BottomNoiseScale;
float _GRB_BottomNoiseStrength;
float _GRB_BottomEdgeNoiseScale;
float _GRB_BottomEdgeNoise;
float _GRB_BottomUpBias;
float _GRB_BottomMaxWeight;
float _GRB_BottomPieceScale;
float _GRB_StrataEnable;
float4 _GRB_StrataColorA;
float4 _GRB_StrataColorB;
float _GRB_StrataDetail;
float _GRB_StrataStrength;
float _GRB_StrataSpacing;
float _GRB_StrataThickVar;
float _GRB_StrataTilt;
float _GRB_StrataAzimuth;
float _GRB_StrataNoiseScale;
float _GRB_StrataNoiseAmp;
float _GRB_StrataLineDarken;
float _GRB_StrataLineWidth;
float _GRB_StrataLedge;
float _GRB_StrataSmoothVar;
float _GRB_MacroStrength;
float _GRB_MacroScale;
float _GRB_FadeEnable;
float _GRB_FadeClip;
float _GRB_FadeHeight;
float _GRB_FadeSoftness;
float _GRB_FadeNoiseScale;
float _GRB_FadeNoiseStrength;
float _GRB_FadeTerrainColor;
float _GRB_ContactAOKeep;
float _GRB_ContactAOWidth;
float _GRB_ForwardOnly;
float _GRB_FadeNormalUp;
float _GRB_FadeShadowLift;
float _GRB_FadeShadowLiftMargin;
float _GRB_TopThroughMin;
float _GRB_TopThroughMax;
float _GRB_TopThroughNoiseScale;
float _GRB_TopThroughContrast;
float _GRB_BottomThroughMin;
float _GRB_BottomThroughMax;
float _GRB_BottomThroughNoiseScale;
float _GRB_BottomThroughContrast;
float _GRB_TerrainPerPixel;
float _GRB_UseTexArray;
float4 _GRB_TopColor;
float _GRB_TopParallax;
float _GRB_TopMetallicMin;
float _GRB_TopMetallicMax;
float _GRB_TopSmoothMin;
float _GRB_TopSmoothMax;
float _GRB_TopAOStrength;
float _GRB_TopMaskOn;
float _GRB_TopAOOn;
// Per-pixel terrain match (PcgTerrainSplat). In the material constant buffer so shared runtime materials (Play Mode)
// stay SRP Batcher / GPU Resident Drawer compatible; Edit Mode still overrides them per renderer with a property block.
float4 _GRB_TParams;       // x on, y layer count, z height blend, w height transition
float4 _GRB_TRect;         // xy terrain origin (xz), zw 1 / terrain size (xz)
float4 _GRB_TControlTS;    // control map texel size (1/w, 1/h, w, h)
// Per layer N (0..7). No arrays: the SRP Batcher / GPU Resident Drawer need every UnityPerMaterial var declared as a
// shader property. _GRB_TSTN splat UV = terrain UV * xy + zw (TerrainLit's _SplatN_ST), _GRB_TDiffN rgb diffuse remap
// scale (tint), _GRB_TMaskOffN / _GRB_TMaskScaleN mask remap, _GRB_TLayerN x has mask, y metallic, z smoothness, w normal scale.
float4 _GRB_TST0; float4 _GRB_TST1; float4 _GRB_TST2; float4 _GRB_TST3; float4 _GRB_TST4; float4 _GRB_TST5; float4 _GRB_TST6; float4 _GRB_TST7;
float4 _GRB_TDiff0; float4 _GRB_TDiff1; float4 _GRB_TDiff2; float4 _GRB_TDiff3; float4 _GRB_TDiff4; float4 _GRB_TDiff5; float4 _GRB_TDiff6; float4 _GRB_TDiff7;
float4 _GRB_TMaskOff0; float4 _GRB_TMaskOff1; float4 _GRB_TMaskOff2; float4 _GRB_TMaskOff3; float4 _GRB_TMaskOff4; float4 _GRB_TMaskOff5; float4 _GRB_TMaskOff6; float4 _GRB_TMaskOff7;
float4 _GRB_TMaskScale0; float4 _GRB_TMaskScale1; float4 _GRB_TMaskScale2; float4 _GRB_TMaskScale3; float4 _GRB_TMaskScale4; float4 _GRB_TMaskScale5; float4 _GRB_TMaskScale6; float4 _GRB_TMaskScale7;
float4 _GRB_TLayer0; float4 _GRB_TLayer1; float4 _GRB_TLayer2; float4 _GRB_TLayer3; float4 _GRB_TLayer4; float4 _GRB_TLayer5; float4 _GRB_TLayer6; float4 _GRB_TLayer7;
float4 _GRB_THeightP;      // x terrain origin y, y world metres per heightmap unit, z on (1) / off (0)
float4 _GRB_THeightTS;     // heightmap texel size (1/w, 1/h, w, h)

// Mipmap Streaming Debug
UNITY_TEXTURE_STREAMING_DEBUG_VARS;

CBUFFER_END

// Following three variables are feeded by the C++ Editor for Scene selection
// It need to be outside the UnityPerMaterial buffer to have Material compatible with SRP Batcher
int _ObjectId;
int _PassValue;
float4 _SelectionID;

#if defined(UNITY_DOTS_INSTANCING_ENABLED)
#if defined(LAYERED_LIT_SHADER)

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor0)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor1)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor2)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor3)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic0)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic1)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic2)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic3)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMin0)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMin1)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMin2)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMin3)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMax0)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMax1)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMax2)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMax3)
    UNITY_DOTS_INSTANCED_PROP(float3, _EmissiveColor)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaCutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness0)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness1)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness2)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness3)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMin0)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMin1)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMin2)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMin3)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMax0)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMax1)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMax2)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMax3)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMin0)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMin1)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMin2)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMin3)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMax0)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMax1)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMax2)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMax3)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMin0)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMin1)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMin2)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMin3)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMax0)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMax1)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMax2)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMax3)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoScale0)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoScale1)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoScale2)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoScale3)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalScale0)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalScale1)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalScale2)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalScale3)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailSmoothnessScale0)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailSmoothnessScale1)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailSmoothnessScale2)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailSmoothnessScale3)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffusionProfileHash0)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffusionProfileHash1)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffusionProfileHash2)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffusionProfileHash3)
    UNITY_DOTS_INSTANCED_PROP(float , _Thickness0)
    UNITY_DOTS_INSTANCED_PROP(float , _Thickness1)
    UNITY_DOTS_INSTANCED_PROP(float , _Thickness2)
    UNITY_DOTS_INSTANCED_PROP(float , _Thickness3)
    UNITY_DOTS_INSTANCED_PROP(float4, _ThicknessRemap0)
    UNITY_DOTS_INSTANCED_PROP(float4, _ThicknessRemap1)
    UNITY_DOTS_INSTANCED_PROP(float4, _ThicknessRemap2)
    UNITY_DOTS_INSTANCED_PROP(float4, _ThicknessRemap3)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

// Here, we want to avoid overriding a property like e.g. _BaseColor0 with something like this:
// #define _BaseColor0 UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor0)
//
// It would be simpler, but it can cause the compiler to regenerate the property loading code for each use of _BaseColor0.
//
// To avoid this, the property loads are cached in some static values at the beginning of the shader.
// The properties such as _BaseColor0 are then overridden so that it expand directly to the static value like this:
// #define _BaseColor0 unity_DOTS_Sampled_BaseColor0
//
// This simple fix happened to improve GPU performances by ~10% on Meta Quest 2 with URP on some scenes.
static float4 unity_DOTS_Sampled_BaseColor0;
static float4 unity_DOTS_Sampled_BaseColor1;
static float4 unity_DOTS_Sampled_BaseColor2;
static float4 unity_DOTS_Sampled_BaseColor3;
static float  unity_DOTS_Sampled_Metallic0;
static float  unity_DOTS_Sampled_Metallic1;
static float  unity_DOTS_Sampled_Metallic2;
static float  unity_DOTS_Sampled_Metallic3;
static float  unity_DOTS_Sampled_MetallicRemapMin0;
static float  unity_DOTS_Sampled_MetallicRemapMin1;
static float  unity_DOTS_Sampled_MetallicRemapMin2;
static float  unity_DOTS_Sampled_MetallicRemapMin3;
static float  unity_DOTS_Sampled_MetallicRemapMax0;
static float  unity_DOTS_Sampled_MetallicRemapMax1;
static float  unity_DOTS_Sampled_MetallicRemapMax2;
static float  unity_DOTS_Sampled_MetallicRemapMax3;
static float3 unity_DOTS_Sampled_EmissiveColor;
static float  unity_DOTS_Sampled_AlphaCutoff;
static float  unity_DOTS_Sampled_Smoothness0;
static float  unity_DOTS_Sampled_Smoothness1;
static float  unity_DOTS_Sampled_Smoothness2;
static float  unity_DOTS_Sampled_Smoothness3;
static float  unity_DOTS_Sampled_SmoothnessRemapMin0;
static float  unity_DOTS_Sampled_SmoothnessRemapMin1;
static float  unity_DOTS_Sampled_SmoothnessRemapMin2;
static float  unity_DOTS_Sampled_SmoothnessRemapMin3;
static float  unity_DOTS_Sampled_SmoothnessRemapMax0;
static float  unity_DOTS_Sampled_SmoothnessRemapMax1;
static float  unity_DOTS_Sampled_SmoothnessRemapMax2;
static float  unity_DOTS_Sampled_SmoothnessRemapMax3;
static float  unity_DOTS_Sampled_AlphaRemapMin0;
static float  unity_DOTS_Sampled_AlphaRemapMin1;
static float  unity_DOTS_Sampled_AlphaRemapMin2;
static float  unity_DOTS_Sampled_AlphaRemapMin3;
static float  unity_DOTS_Sampled_AlphaRemapMax0;
static float  unity_DOTS_Sampled_AlphaRemapMax1;
static float  unity_DOTS_Sampled_AlphaRemapMax2;
static float  unity_DOTS_Sampled_AlphaRemapMax3;
static float  unity_DOTS_Sampled_AORemapMin0;
static float  unity_DOTS_Sampled_AORemapMin1;
static float  unity_DOTS_Sampled_AORemapMin2;
static float  unity_DOTS_Sampled_AORemapMin3;
static float  unity_DOTS_Sampled_AORemapMax0;
static float  unity_DOTS_Sampled_AORemapMax1;
static float  unity_DOTS_Sampled_AORemapMax2;
static float  unity_DOTS_Sampled_AORemapMax3;
static float  unity_DOTS_Sampled_DetailAlbedoScale0;
static float  unity_DOTS_Sampled_DetailAlbedoScale1;
static float  unity_DOTS_Sampled_DetailAlbedoScale2;
static float  unity_DOTS_Sampled_DetailAlbedoScale3;
static float  unity_DOTS_Sampled_DetailNormalScale0;
static float  unity_DOTS_Sampled_DetailNormalScale1;
static float  unity_DOTS_Sampled_DetailNormalScale2;
static float  unity_DOTS_Sampled_DetailNormalScale3;
static float  unity_DOTS_Sampled_DetailSmoothnessScale0;
static float  unity_DOTS_Sampled_DetailSmoothnessScale1;
static float  unity_DOTS_Sampled_DetailSmoothnessScale2;
static float  unity_DOTS_Sampled_DetailSmoothnessScale3;
static float  unity_DOTS_Sampled_DiffusionProfileHash0;
static float  unity_DOTS_Sampled_DiffusionProfileHash1;
static float  unity_DOTS_Sampled_DiffusionProfileHash2;
static float  unity_DOTS_Sampled_DiffusionProfileHash3;
static float  unity_DOTS_Sampled_Thickness0;
static float  unity_DOTS_Sampled_Thickness1;
static float  unity_DOTS_Sampled_Thickness2;
static float  unity_DOTS_Sampled_Thickness3;
static float4 unity_DOTS_Sampled_ThicknessRemap0;
static float4 unity_DOTS_Sampled_ThicknessRemap1;
static float4 unity_DOTS_Sampled_ThicknessRemap2;
static float4 unity_DOTS_Sampled_ThicknessRemap3;

void SetupDOTSLayeredLitMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor0               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor0);
    unity_DOTS_Sampled_BaseColor1               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor1);
    unity_DOTS_Sampled_BaseColor2               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor2);
    unity_DOTS_Sampled_BaseColor3               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor3);
    unity_DOTS_Sampled_Metallic0                = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic0);
    unity_DOTS_Sampled_Metallic1                = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic1);
    unity_DOTS_Sampled_Metallic2                = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic2);
    unity_DOTS_Sampled_Metallic3                = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic3);
    unity_DOTS_Sampled_MetallicRemapMin0        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMin0);
    unity_DOTS_Sampled_MetallicRemapMin1        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMin1);
    unity_DOTS_Sampled_MetallicRemapMin2        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMin2);
    unity_DOTS_Sampled_MetallicRemapMin3        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMin3);
    unity_DOTS_Sampled_MetallicRemapMax0        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMax0);
    unity_DOTS_Sampled_MetallicRemapMax1        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMax1);
    unity_DOTS_Sampled_MetallicRemapMax2        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMax2);
    unity_DOTS_Sampled_MetallicRemapMax3        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMax3);
    unity_DOTS_Sampled_EmissiveColor            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3, _EmissiveColor);
    unity_DOTS_Sampled_AlphaCutoff              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaCutoff);
    unity_DOTS_Sampled_Smoothness0              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness0);
    unity_DOTS_Sampled_Smoothness1              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness1);
    unity_DOTS_Sampled_Smoothness2              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness2);
    unity_DOTS_Sampled_Smoothness3              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness3);
    unity_DOTS_Sampled_SmoothnessRemapMin0      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMin0);
    unity_DOTS_Sampled_SmoothnessRemapMin1      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMin1);
    unity_DOTS_Sampled_SmoothnessRemapMin2      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMin2);
    unity_DOTS_Sampled_SmoothnessRemapMin3      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMin3);
    unity_DOTS_Sampled_SmoothnessRemapMax0      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMax0);
    unity_DOTS_Sampled_SmoothnessRemapMax1      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMax1);
    unity_DOTS_Sampled_SmoothnessRemapMax2      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMax2);
    unity_DOTS_Sampled_SmoothnessRemapMax3      = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMax3);
    unity_DOTS_Sampled_AlphaRemapMin0           = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMin0);
    unity_DOTS_Sampled_AlphaRemapMin1           = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMin1);
    unity_DOTS_Sampled_AlphaRemapMin2           = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMin2);
    unity_DOTS_Sampled_AlphaRemapMin3           = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMin3);
    unity_DOTS_Sampled_AlphaRemapMax0           = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMax0);
    unity_DOTS_Sampled_AlphaRemapMax1           = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMax1);
    unity_DOTS_Sampled_AlphaRemapMax2           = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMax2);
    unity_DOTS_Sampled_AlphaRemapMax3           = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMax3);
    unity_DOTS_Sampled_AORemapMin0              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMin0);
    unity_DOTS_Sampled_AORemapMin1              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMin1);
    unity_DOTS_Sampled_AORemapMin2              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMin2);
    unity_DOTS_Sampled_AORemapMin3              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMin3);
    unity_DOTS_Sampled_AORemapMax0              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMax0);
    unity_DOTS_Sampled_AORemapMax1              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMax1);
    unity_DOTS_Sampled_AORemapMax2              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMax2);
    unity_DOTS_Sampled_AORemapMax3              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMax3);
    unity_DOTS_Sampled_DetailAlbedoScale0       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoScale0);
    unity_DOTS_Sampled_DetailAlbedoScale1       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoScale1);
    unity_DOTS_Sampled_DetailAlbedoScale2       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoScale2);
    unity_DOTS_Sampled_DetailAlbedoScale3       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoScale3);
    unity_DOTS_Sampled_DetailNormalScale0       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalScale0);
    unity_DOTS_Sampled_DetailNormalScale1       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalScale1);
    unity_DOTS_Sampled_DetailNormalScale2       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalScale2);
    unity_DOTS_Sampled_DetailNormalScale3       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalScale3);
    unity_DOTS_Sampled_DetailSmoothnessScale0   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailSmoothnessScale0);
    unity_DOTS_Sampled_DetailSmoothnessScale1   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailSmoothnessScale1);
    unity_DOTS_Sampled_DetailSmoothnessScale2   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailSmoothnessScale2);
    unity_DOTS_Sampled_DetailSmoothnessScale3   = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailSmoothnessScale3);
    unity_DOTS_Sampled_DiffusionProfileHash0    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffusionProfileHash0);
    unity_DOTS_Sampled_DiffusionProfileHash1    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffusionProfileHash1);
    unity_DOTS_Sampled_DiffusionProfileHash2    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffusionProfileHash2);
    unity_DOTS_Sampled_DiffusionProfileHash3    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffusionProfileHash3);
    unity_DOTS_Sampled_Thickness0               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Thickness0);
    unity_DOTS_Sampled_Thickness1               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Thickness1);
    unity_DOTS_Sampled_Thickness2               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Thickness2);
    unity_DOTS_Sampled_Thickness3               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Thickness3);
    unity_DOTS_Sampled_ThicknessRemap0          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ThicknessRemap0);
    unity_DOTS_Sampled_ThicknessRemap1          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ThicknessRemap1);
    unity_DOTS_Sampled_ThicknessRemap2          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ThicknessRemap2);
    unity_DOTS_Sampled_ThicknessRemap3          = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ThicknessRemap3);
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSLayeredLitMaterialPropertyCaches()

#define _BaseColor0              unity_DOTS_Sampled_BaseColor0
#define _BaseColor1              unity_DOTS_Sampled_BaseColor1
#define _BaseColor2              unity_DOTS_Sampled_BaseColor2
#define _BaseColor3              unity_DOTS_Sampled_BaseColor3
#define _Metallic0               unity_DOTS_Sampled_Metallic0
#define _Metallic1               unity_DOTS_Sampled_Metallic1
#define _Metallic2               unity_DOTS_Sampled_Metallic2
#define _Metallic3               unity_DOTS_Sampled_Metallic3
#define _MetallicRemapMin0       unity_DOTS_Sampled_MetallicRemapMin0
#define _MetallicRemapMin1       unity_DOTS_Sampled_MetallicRemapMin1
#define _MetallicRemapMin2       unity_DOTS_Sampled_MetallicRemapMin2
#define _MetallicRemapMin3       unity_DOTS_Sampled_MetallicRemapMin3
#define _MetallicRemapMax0       unity_DOTS_Sampled_MetallicRemapMax0
#define _MetallicRemapMax1       unity_DOTS_Sampled_MetallicRemapMax1
#define _MetallicRemapMax2       unity_DOTS_Sampled_MetallicRemapMax2
#define _MetallicRemapMax3       unity_DOTS_Sampled_MetallicRemapMax3
#define _EmissiveColor           unity_DOTS_Sampled_EmissiveColor
#define _AlphaCutoff             unity_DOTS_Sampled_AlphaCutoff
#define _Smoothness0             unity_DOTS_Sampled_Smoothness0
#define _Smoothness1             unity_DOTS_Sampled_Smoothness1
#define _Smoothness2             unity_DOTS_Sampled_Smoothness2
#define _Smoothness3             unity_DOTS_Sampled_Smoothness3
#define _SmoothnessRemapMin0     unity_DOTS_Sampled_SmoothnessRemapMin0
#define _SmoothnessRemapMin1     unity_DOTS_Sampled_SmoothnessRemapMin1
#define _SmoothnessRemapMin2     unity_DOTS_Sampled_SmoothnessRemapMin2
#define _SmoothnessRemapMin3     unity_DOTS_Sampled_SmoothnessRemapMin3
#define _SmoothnessRemapMax0     unity_DOTS_Sampled_SmoothnessRemapMax0
#define _SmoothnessRemapMax1     unity_DOTS_Sampled_SmoothnessRemapMax1
#define _SmoothnessRemapMax2     unity_DOTS_Sampled_SmoothnessRemapMax2
#define _SmoothnessRemapMax3     unity_DOTS_Sampled_SmoothnessRemapMax3
#define _AlphaRemapMin0          unity_DOTS_Sampled_AlphaRemapMin0
#define _AlphaRemapMin1          unity_DOTS_Sampled_AlphaRemapMin1
#define _AlphaRemapMin2          unity_DOTS_Sampled_AlphaRemapMin2
#define _AlphaRemapMin3          unity_DOTS_Sampled_AlphaRemapMin3
#define _AlphaRemapMax0          unity_DOTS_Sampled_AlphaRemapMax0
#define _AlphaRemapMax1          unity_DOTS_Sampled_AlphaRemapMax1
#define _AlphaRemapMax2          unity_DOTS_Sampled_AlphaRemapMax2
#define _AlphaRemapMax3          unity_DOTS_Sampled_AlphaRemapMax3
#define _AORemapMin0             unity_DOTS_Sampled_AORemapMin0
#define _AORemapMin1             unity_DOTS_Sampled_AORemapMin1
#define _AORemapMin2             unity_DOTS_Sampled_AORemapMin2
#define _AORemapMin3             unity_DOTS_Sampled_AORemapMin3
#define _AORemapMax0             unity_DOTS_Sampled_AORemapMax0
#define _AORemapMax1             unity_DOTS_Sampled_AORemapMax1
#define _AORemapMax2             unity_DOTS_Sampled_AORemapMax2
#define _AORemapMax3             unity_DOTS_Sampled_AORemapMax3
#define _DetailAlbedoScale0      unity_DOTS_Sampled_DetailAlbedoScale0
#define _DetailAlbedoScale1      unity_DOTS_Sampled_DetailAlbedoScale1
#define _DetailAlbedoScale2      unity_DOTS_Sampled_DetailAlbedoScale2
#define _DetailAlbedoScale3      unity_DOTS_Sampled_DetailAlbedoScale3
#define _DetailNormalScale0      unity_DOTS_Sampled_DetailNormalScale0
#define _DetailNormalScale1      unity_DOTS_Sampled_DetailNormalScale1
#define _DetailNormalScale2      unity_DOTS_Sampled_DetailNormalScale2
#define _DetailNormalScale3      unity_DOTS_Sampled_DetailNormalScale3
#define _DetailSmoothnessScale0  unity_DOTS_Sampled_DetailSmoothnessScale0
#define _DetailSmoothnessScale1  unity_DOTS_Sampled_DetailSmoothnessScale1
#define _DetailSmoothnessScale2  unity_DOTS_Sampled_DetailSmoothnessScale2
#define _DetailSmoothnessScale3  unity_DOTS_Sampled_DetailSmoothnessScale3
#define _DiffusionProfileHash0   unity_DOTS_Sampled_DiffusionProfileHash0
#define _DiffusionProfileHash1   unity_DOTS_Sampled_DiffusionProfileHash1
#define _DiffusionProfileHash2   unity_DOTS_Sampled_DiffusionProfileHash2
#define _DiffusionProfileHash3   unity_DOTS_Sampled_DiffusionProfileHash3
#define _Thickness0              unity_DOTS_Sampled_Thickness0
#define _Thickness1              unity_DOTS_Sampled_Thickness1
#define _Thickness2              unity_DOTS_Sampled_Thickness2
#define _Thickness3              unity_DOTS_Sampled_Thickness3
#define _ThicknessRemap0         unity_DOTS_Sampled_ThicknessRemap0
#define _ThicknessRemap1         unity_DOTS_Sampled_ThicknessRemap1
#define _ThicknessRemap2         unity_DOTS_Sampled_ThicknessRemap2
#define _ThicknessRemap3         unity_DOTS_Sampled_ThicknessRemap3

#else

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMin)
    UNITY_DOTS_INSTANCED_PROP(float , _MetallicRemapMax)
    UNITY_DOTS_INSTANCED_PROP(float3, _EmissiveColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecularColor)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaCutoff);
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMin)
    UNITY_DOTS_INSTANCED_PROP(float , _SmoothnessRemapMax)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMin)
    UNITY_DOTS_INSTANCED_PROP(float , _AlphaRemapMax)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMin)
    UNITY_DOTS_INSTANCED_PROP(float , _AORemapMax)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoScale)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalScale)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailSmoothnessScale)
    UNITY_DOTS_INSTANCED_PROP(float , _DiffusionProfileHash)
    UNITY_DOTS_INSTANCED_PROP(float , _Thickness)
    UNITY_DOTS_INSTANCED_PROP(float4, _ThicknessRemap)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

// Here, we want to avoid overriding a property like e.g. _BaseColor with something like this:
// #define _BaseColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor0)
//
// It would be simpler, but it can cause the compiler to regenerate the property loading code for each use of _BaseColor.
//
// To avoid this, the property loads are cached in some static values at the beginning of the shader.
// The properties such as _BaseColor are then overridden so that it expand directly to the static value like this:
// #define _BaseColor unity_DOTS_Sampled_BaseColor
//
// This simple fix happened to improve GPU performances by ~10% on Meta Quest 2 with URP on some scenes.
static float4 unity_DOTS_Sampled_BaseColor;
static float  unity_DOTS_Sampled_Metallic;
static float  unity_DOTS_Sampled_MetallicRemapMin;
static float  unity_DOTS_Sampled_MetallicRemapMax;
static float3 unity_DOTS_Sampled_EmissiveColor;
static float4 unity_DOTS_Sampled_SpecularColor;
static float  unity_DOTS_Sampled_AlphaCutoff;;
static float  unity_DOTS_Sampled_Smoothness;
static float  unity_DOTS_Sampled_SmoothnessRemapMin;
static float  unity_DOTS_Sampled_SmoothnessRemapMax;
static float  unity_DOTS_Sampled_AlphaRemapMin;
static float  unity_DOTS_Sampled_AlphaRemapMax;
static float  unity_DOTS_Sampled_AORemapMin;
static float  unity_DOTS_Sampled_AORemapMax;
static float  unity_DOTS_Sampled_DetailAlbedoScale;
static float  unity_DOTS_Sampled_DetailNormalScale;
static float  unity_DOTS_Sampled_DetailSmoothnessScale;
static float  unity_DOTS_Sampled_DiffusionProfileHash;
static float  unity_DOTS_Sampled_Thickness;
static float4 unity_DOTS_Sampled_ThicknessRemap;

void SetupDOTSLitMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
    unity_DOTS_Sampled_Metallic = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic);
    unity_DOTS_Sampled_MetallicRemapMin = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMin);
    unity_DOTS_Sampled_MetallicRemapMax = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _MetallicRemapMax);
    unity_DOTS_Sampled_Smoothness = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness);
    unity_DOTS_Sampled_EmissiveColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float3, _EmissiveColor);
    unity_DOTS_Sampled_SpecularColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SpecularColor);
    unity_DOTS_Sampled_AlphaCutoff = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaCutoff);
    unity_DOTS_Sampled_Smoothness = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness);
    unity_DOTS_Sampled_SmoothnessRemapMin = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMin);
    unity_DOTS_Sampled_SmoothnessRemapMax = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _SmoothnessRemapMax);
    unity_DOTS_Sampled_AlphaRemapMin = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMin);
    unity_DOTS_Sampled_AlphaRemapMax = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AlphaRemapMax);
    unity_DOTS_Sampled_AORemapMin = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMin);
    unity_DOTS_Sampled_AORemapMax = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _AORemapMax);
    unity_DOTS_Sampled_DetailAlbedoScale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoScale);
    unity_DOTS_Sampled_DetailNormalScale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalScale);
    unity_DOTS_Sampled_DetailSmoothnessScale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailSmoothnessScale);
    unity_DOTS_Sampled_DiffusionProfileHash = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DiffusionProfileHash);
    unity_DOTS_Sampled_Thickness = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Thickness);
    unity_DOTS_Sampled_ThicknessRemap = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ThicknessRemap);
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSLitMaterialPropertyCaches()

#define _BaseColor             unity_DOTS_Sampled_BaseColor
#define _Metallic              unity_DOTS_Sampled_Metallic
#define _MetallicRemapMin      unity_DOTS_Sampled_MetallicRemapMin
#define _MetallicRemapMax      unity_DOTS_Sampled_MetallicRemapMax
#define _Smoothness            unity_DOTS_Sampled_Smoothness
#define _EmissiveColor         unity_DOTS_Sampled_EmissiveColor
#define _SpecularColor         unity_DOTS_Sampled_SpecularColor
#define _AlphaCutoff           unity_DOTS_Sampled_AlphaCutoff
#define _Smoothness            unity_DOTS_Sampled_Smoothness
#define _SmoothnessRemapMin    unity_DOTS_Sampled_SmoothnessRemapMin
#define _SmoothnessRemapMax    unity_DOTS_Sampled_SmoothnessRemapMax
#define _AlphaRemapMin         unity_DOTS_Sampled_AlphaRemapMin
#define _AlphaRemapMax         unity_DOTS_Sampled_AlphaRemapMax
#define _AORemapMin            unity_DOTS_Sampled_AORemapMin
#define _AORemapMax            unity_DOTS_Sampled_AORemapMax
#define _DetailAlbedoScale     unity_DOTS_Sampled_DetailAlbedoScale
#define _DetailNormalScale     unity_DOTS_Sampled_DetailNormalScale
#define _DetailSmoothnessScale unity_DOTS_Sampled_DetailSmoothnessScale
#define _DiffusionProfileHash  unity_DOTS_Sampled_DiffusionProfileHash
#define _Thickness             unity_DOTS_Sampled_Thickness
#define _ThicknessRemap        unity_DOTS_Sampled_ThicknessRemap

#endif
#endif
