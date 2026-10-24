// DM copy of HDRP 17.4 TerrainLit (2026-10-24b). Original HDRP/TerrainLit untouched.
Shader "DM/TerrainParallax"
{
    Properties
    {
        [Toggle(_TERRAIN_BLEND_HEIGHT)] _EnableHeightBlend("Height-based Blend", Float) = 1.0
        _HeightTransition("Height Transition", Range(0, 1.0)) = 0.0

        [Header(DM Parallax)]
        [KeywordEnum(Off, Offset, POM)] _DM_Parallax("Parallax Mode", Float) = 1
        _DMParallaxHeight("Parallax Height (m)", Range(0, 0.5)) = 0.05
        _DMHeightCenter("Height Center (mask B)", Range(0, 1)) = 0.5
        _DMOffsetBias("Offset Bias (grazing clamp)", Range(0, 1)) = 0.42
        _DMParallaxFadeStart("Parallax Fade Start (m)", Range(0, 300)) = 20
        _DMParallaxFadeEnd("Parallax Fade End (m)", Range(0, 300)) = 60
        _DMGrazingCos("Grazing Angle Fade (cos)", Range(0, 1)) = 0.1
        _DMSlopeFadeStart("Slope Fade Start (normal.y, full)", Range(0, 1)) = 0
        _DMSlopeFadeEnd("Slope Fade End (normal.y, none)", Range(0, 1)) = 0
        _DMHeightMipBias("Height Mip Bias (anti-fuzz)", Range(-2, 4)) = 0
        [Header(DM POM strongest layers)]
        [IntRange] _DMPOMSteps("POM Max Steps (grazing)", Range(1, 64)) = 12
        [IntRange] _DMPOMMinSteps("POM Min Steps (top-down)", Range(1, 64)) = 4
        [IntRange] _DMPOMRefine("POM Refine Steps (binary)", Range(0, 8)) = 3
        [IntRange] _DMPOMLayers("POM Layers", Range(1, 2)) = 1
        _DMPOMFadeStart("POM Fade Start (m)", Range(0, 300)) = 10
        _DMPOMFadeEnd("POM Fade End (m)", Range(0, 300)) = 30
        [Header(DM Self Shadow main directional light)]
        [Toggle(_DM_SELFSHADOW)] _DMSelfShadow("Parallax Self-Shadow", Float) = 0
        _DMShadowStrength("Shadow Strength", Range(0, 2)) = 0.8
        _DMShadowSharpness("Shadow Sharpness (low = soft)", Range(0.1, 64)) = 8
        [IntRange] _DMShadowSteps("Shadow Steps", Range(1, 32)) = 8
        _DMShadowLength("Shadow Length", Range(0, 8)) = 1
        _DMShadowFadeStart("Shadow Fade Start (m)", Range(0, 300)) = 15
        _DMShadowFadeEnd("Shadow Fade End (m)", Range(0, 300)) = 40
        _DMSunDirFallback("Sun Dir Fallback (no light)", Vector) = (0.3, 0.8, 0.3, 0)
        [Header(DM Height AO)]
        _DMHeightAOStrength("Height AO Strength", Range(0, 2)) = 0
        _DMHeightAOMid("Height AO Crevice Height", Range(0.01, 1)) = 0.4
        [Header(DM Per Layer Parallax)]
        _DMLayerParallax0("Layer 0 Parallax", Range(0, 4)) = 1
        _DMLayerParallax1("Layer 1 Parallax", Range(0, 4)) = 1
        _DMLayerParallax2("Layer 2 Parallax", Range(0, 4)) = 1
        _DMLayerParallax3("Layer 3 Parallax", Range(0, 4)) = 1
        _DMLayerParallax4("Layer 4 Parallax", Range(0, 4)) = 1
        _DMLayerParallax5("Layer 5 Parallax", Range(0, 4)) = 1
        _DMLayerParallax6("Layer 6 Parallax", Range(0, 4)) = 1
        _DMLayerParallax7("Layer 7 Parallax", Range(0, 4)) = 1
        _DMLayerHeightAO0("Layer 0 Height AO", Range(0, 4)) = 1
        _DMLayerHeightAO1("Layer 1 Height AO", Range(0, 4)) = 1
        _DMLayerHeightAO2("Layer 2 Height AO", Range(0, 4)) = 1
        _DMLayerHeightAO3("Layer 3 Height AO", Range(0, 4)) = 1
        _DMLayerHeightAO4("Layer 4 Height AO", Range(0, 4)) = 1
        _DMLayerHeightAO5("Layer 5 Height AO", Range(0, 4)) = 1
        _DMLayerHeightAO6("Layer 6 Height AO", Range(0, 4)) = 1
        _DMLayerHeightAO7("Layer 7 Height AO", Range(0, 4)) = 1
        _DMPeakSoftness0("Layer 0 Peak Softness", Range(0, 1)) = 0
        _DMPeakKnee0("Layer 0 Peak Knee", Range(0.5, 1)) = 0.9
        _DMPeakDetail0("Layer 0 Peak Detail", Range(0, 1)) = 0
        _DMPeakDetailScale0("Layer 0 Peak Detail Scale", Range(0.1, 20)) = 4
        _DMPeakSoftness1("Layer 1 Peak Softness", Range(0, 1)) = 0
        _DMPeakKnee1("Layer 1 Peak Knee", Range(0.5, 1)) = 0.9
        _DMPeakDetail1("Layer 1 Peak Detail", Range(0, 1)) = 0
        _DMPeakDetailScale1("Layer 1 Peak Detail Scale", Range(0.1, 20)) = 4
        _DMPeakSoftness2("Layer 2 Peak Softness", Range(0, 1)) = 0
        _DMPeakKnee2("Layer 2 Peak Knee", Range(0.5, 1)) = 0.9
        _DMPeakDetail2("Layer 2 Peak Detail", Range(0, 1)) = 0
        _DMPeakDetailScale2("Layer 2 Peak Detail Scale", Range(0.1, 20)) = 4
        _DMPeakSoftness3("Layer 3 Peak Softness", Range(0, 1)) = 0
        _DMPeakKnee3("Layer 3 Peak Knee", Range(0.5, 1)) = 0.9
        _DMPeakDetail3("Layer 3 Peak Detail", Range(0, 1)) = 0
        _DMPeakDetailScale3("Layer 3 Peak Detail Scale", Range(0.1, 20)) = 4
        _DMPeakSoftness4("Layer 4 Peak Softness", Range(0, 1)) = 0
        _DMPeakKnee4("Layer 4 Peak Knee", Range(0.5, 1)) = 0.9
        _DMPeakDetail4("Layer 4 Peak Detail", Range(0, 1)) = 0
        _DMPeakDetailScale4("Layer 4 Peak Detail Scale", Range(0.1, 20)) = 4
        _DMPeakSoftness5("Layer 5 Peak Softness", Range(0, 1)) = 0
        _DMPeakKnee5("Layer 5 Peak Knee", Range(0.5, 1)) = 0.9
        _DMPeakDetail5("Layer 5 Peak Detail", Range(0, 1)) = 0
        _DMPeakDetailScale5("Layer 5 Peak Detail Scale", Range(0.1, 20)) = 4
        _DMPeakSoftness6("Layer 6 Peak Softness", Range(0, 1)) = 0
        _DMPeakKnee6("Layer 6 Peak Knee", Range(0.5, 1)) = 0.9
        _DMPeakDetail6("Layer 6 Peak Detail", Range(0, 1)) = 0
        _DMPeakDetailScale6("Layer 6 Peak Detail Scale", Range(0.1, 20)) = 4
        _DMPeakSoftness7("Layer 7 Peak Softness", Range(0, 1)) = 0
        _DMPeakKnee7("Layer 7 Peak Knee", Range(0.5, 1)) = 0.9
        _DMPeakDetail7("Layer 7 Peak Detail", Range(0, 1)) = 0
        _DMPeakDetailScale7("Layer 7 Peak Detail Scale", Range(0.1, 20)) = 4
        [Header(DM Per Layer Height Maps)]
        [ToggleUI] _DMHeightMapForBlend("Height Blend Uses DM Height Maps", Float) = 0
        [NoScaleOffset] _DMHeightMap0("Layer 0 Height Map (R)", 2D) = "gray" {}
        [ToggleUI] _DMUseHeightMap0("Layer 0 Use Height Map", Float) = 0
        _DMHeightScale0("Layer 0 Height Scale", Range(0, 4)) = 1
        _DMHeightOffset0("Layer 0 Height Offset", Range(-1, 1)) = 0
        _DMHeightContrast0("Layer 0 Height Contrast", Range(0, 8)) = 1
        [NoScaleOffset] _DMHeightMap1("Layer 1 Height Map (R)", 2D) = "gray" {}
        [ToggleUI] _DMUseHeightMap1("Layer 1 Use Height Map", Float) = 0
        _DMHeightScale1("Layer 1 Height Scale", Range(0, 4)) = 1
        _DMHeightOffset1("Layer 1 Height Offset", Range(-1, 1)) = 0
        _DMHeightContrast1("Layer 1 Height Contrast", Range(0, 8)) = 1
        [NoScaleOffset] _DMHeightMap2("Layer 2 Height Map (R)", 2D) = "gray" {}
        [ToggleUI] _DMUseHeightMap2("Layer 2 Use Height Map", Float) = 0
        _DMHeightScale2("Layer 2 Height Scale", Range(0, 4)) = 1
        _DMHeightOffset2("Layer 2 Height Offset", Range(-1, 1)) = 0
        _DMHeightContrast2("Layer 2 Height Contrast", Range(0, 8)) = 1
        [NoScaleOffset] _DMHeightMap3("Layer 3 Height Map (R)", 2D) = "gray" {}
        [ToggleUI] _DMUseHeightMap3("Layer 3 Use Height Map", Float) = 0
        _DMHeightScale3("Layer 3 Height Scale", Range(0, 4)) = 1
        _DMHeightOffset3("Layer 3 Height Offset", Range(-1, 1)) = 0
        _DMHeightContrast3("Layer 3 Height Contrast", Range(0, 8)) = 1
        [NoScaleOffset] _DMHeightMap4("Layer 4 Height Map (R)", 2D) = "gray" {}
        [ToggleUI] _DMUseHeightMap4("Layer 4 Use Height Map", Float) = 0
        _DMHeightScale4("Layer 4 Height Scale", Range(0, 4)) = 1
        _DMHeightOffset4("Layer 4 Height Offset", Range(-1, 1)) = 0
        _DMHeightContrast4("Layer 4 Height Contrast", Range(0, 8)) = 1
        [NoScaleOffset] _DMHeightMap5("Layer 5 Height Map (R)", 2D) = "gray" {}
        [ToggleUI] _DMUseHeightMap5("Layer 5 Use Height Map", Float) = 0
        _DMHeightScale5("Layer 5 Height Scale", Range(0, 4)) = 1
        _DMHeightOffset5("Layer 5 Height Offset", Range(-1, 1)) = 0
        _DMHeightContrast5("Layer 5 Height Contrast", Range(0, 8)) = 1
        [NoScaleOffset] _DMHeightMap6("Layer 6 Height Map (R)", 2D) = "gray" {}
        [ToggleUI] _DMUseHeightMap6("Layer 6 Use Height Map", Float) = 0
        _DMHeightScale6("Layer 6 Height Scale", Range(0, 4)) = 1
        _DMHeightOffset6("Layer 6 Height Offset", Range(-1, 1)) = 0
        _DMHeightContrast6("Layer 6 Height Contrast", Range(0, 8)) = 1
        [NoScaleOffset] _DMHeightMap7("Layer 7 Height Map (R)", 2D) = "gray" {}
        [ToggleUI] _DMUseHeightMap7("Layer 7 Use Height Map", Float) = 0
        _DMHeightScale7("Layer 7 Height Scale", Range(0, 4)) = 1
        _DMHeightOffset7("Layer 7 Height Offset", Range(-1, 1)) = 0
        _DMHeightContrast7("Layer 7 Height Contrast", Range(0, 8)) = 1
        [Header(DM Per Layer Color)]
        _DMLayerTint0("Layer 0 Extra Tint", Color) = (1,1,1,1)
        _DMLayerSaturation0("Layer 0 Saturation", Range(0, 3)) = 1
        _DMLayerBrightness0("Layer 0 Brightness", Range(0, 4)) = 1
        _DMLayerTint1("Layer 1 Extra Tint", Color) = (1,1,1,1)
        _DMLayerSaturation1("Layer 1 Saturation", Range(0, 3)) = 1
        _DMLayerBrightness1("Layer 1 Brightness", Range(0, 4)) = 1
        _DMLayerTint2("Layer 2 Extra Tint", Color) = (1,1,1,1)
        _DMLayerSaturation2("Layer 2 Saturation", Range(0, 3)) = 1
        _DMLayerBrightness2("Layer 2 Brightness", Range(0, 4)) = 1
        _DMLayerTint3("Layer 3 Extra Tint", Color) = (1,1,1,1)
        _DMLayerSaturation3("Layer 3 Saturation", Range(0, 3)) = 1
        _DMLayerBrightness3("Layer 3 Brightness", Range(0, 4)) = 1
        _DMLayerTint4("Layer 4 Extra Tint", Color) = (1,1,1,1)
        _DMLayerSaturation4("Layer 4 Saturation", Range(0, 3)) = 1
        _DMLayerBrightness4("Layer 4 Brightness", Range(0, 4)) = 1
        _DMLayerTint5("Layer 5 Extra Tint", Color) = (1,1,1,1)
        _DMLayerSaturation5("Layer 5 Saturation", Range(0, 3)) = 1
        _DMLayerBrightness5("Layer 5 Brightness", Range(0, 4)) = 1
        _DMLayerTint6("Layer 6 Extra Tint", Color) = (1,1,1,1)
        _DMLayerSaturation6("Layer 6 Saturation", Range(0, 3)) = 1
        _DMLayerBrightness6("Layer 6 Brightness", Range(0, 4)) = 1
        _DMLayerTint7("Layer 7 Extra Tint", Color) = (1,1,1,1)
        _DMLayerSaturation7("Layer 7 Saturation", Range(0, 3)) = 1
        _DMLayerBrightness7("Layer 7 Brightness", Range(0, 4)) = 1
        [Header(DM POM Depth Offset (decals land on POM))]
        [Toggle(_DEPTHOFFSET_ON)] _DMDepthOffset("Depth Offset (decals/projections follow POM)", Float) = 0
        _DMDepthOffsetScale("Depth Offset Scale", Range(0, 4)) = 1
        _DMDepthOffsetMax("Depth Offset Max (m)", Range(0.01, 2)) = 0.5
        [Header(DM Anti Tiling)]
        [Toggle(_DM_ANTITILE)] _DMAntiTile("Anti-Tiling", Float) = 0
        _DMAntiTileScale("Anti-Tile UV Scale", Range(0.01, 4)) = 0.31
        _DMAntiTileStrength("Anti-Tile Strength", Range(0, 1)) = 0.5
        _DMAntiTileNoiseScale("Anti-Tile Noise Scale (per m)", Range(0.001, 1)) = 0.05
        [HideInInspector] [Enum(Off, 0, From Ambient Occlusion, 1)]  _SpecularOcclusionMode("Specular Occlusion Mode", Int) = 1

        // TODO: support tri-planar?
        // TODO: support more maps?
        //[HideInInspector] _TexWorldScale0("Tiling", Float) = 1.0
        //[HideInInspector] _TexWorldScale1("Tiling", Float) = 1.0
        //[HideInInspector] _TexWorldScale2("Tiling", Float) = 1.0
        //[HideInInspector] _TexWorldScale3("Tiling", Float) = 1.0

        // Following are builtin properties

        // Stencil state
        // Forward
        [HideInInspector] _StencilRef("_StencilRef", Int) = 0  // StencilUsage.Clear
        [HideInInspector] _StencilWriteMask("_StencilWriteMask", Int) = 3 // StencilUsage.RequiresDeferredLighting | StencilUsage.SubsurfaceScattering
        // GBuffer
        [HideInInspector] _StencilRefGBuffer("_StencilRefGBuffer", Int) = 2 // StencilUsage.RequiresDeferredLighting
        [HideInInspector] _StencilWriteMaskGBuffer("_StencilWriteMaskGBuffer", Int) = 3 // StencilUsage.RequiresDeferredLighting | StencilUsage.SubsurfaceScattering
        // Depth prepass
        [HideInInspector] _StencilRefDepth("_StencilRefDepth", Int) = 0 // Nothing
        [HideInInspector] _StencilWriteMaskDepth("_StencilWriteMaskDepth", Int) = 8 // StencilUsage.TraceReflectionRay

        // Blending state
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector][ToggleUI] _TransparentZWrite("_TransparentZWrite", Float) = 0.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _ZTestDepthEqualForOpaque("_ZTestDepthEqualForOpaque", Int) = 4 // Less equal
        [HideInInspector] _ZTestGBuffer("_ZTestGBuffer", Int) = 4

        [Toggle(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)] _EnableInstancedPerPixelNormal("Instanced per pixel normal", Float) = 1.0

        [HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}

        // Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
        // value that exist to identify if the GI emission need to be enabled.
        // In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
        // TODO: Fix the code in legacy unity so we can customize the behavior for GI
        [HideInInspector] _EmissionColor("Color", Color) = (1, 1, 1)

        // HACK: GI Baking system relies on some properties existing in the shader ("_MainTex", "_Cutoff" and "_Color") for opacity handling, so we need to store our version of those parameters in the hard-coded name the GI baking system recognizes.
        [HideInInspector] _MainTex("Albedo", 2D) = "white" {}
        [HideInInspector] _Color("Color", Color) = (1,1,1,1)

        [HideInInspector] [ToggleUI] _SupportDecals("Support Decals", Float) = 1.0
        [HideInInspector] [ToggleUI] _ReceivesSSR("Receives SSR", Float) = 1.0
        [HideInInspector] [ToggleUI] _AddPrecomputedVelocity("AddPrecomputedVelocity", Float) = 0.0

    }

    HLSLINCLUDE
    #pragma target 4.5

    // Terrain builtin keywords
    #pragma shader_feature_local _TERRAIN_8_LAYERS
    #pragma shader_feature_local _NORMALMAP
    #pragma shader_feature_local _MASKMAP
    #pragma shader_feature_local _SPECULAR_OCCLUSION_NONE

    #pragma shader_feature_local _TERRAIN_BLEND_HEIGHT
    // Sample normal in pixel shader when doing instancing
    #pragma shader_feature_local _TERRAIN_INSTANCED_PERPIXEL_NORMAL
    #pragma shader_feature_local_fragment _DM_PARALLAX_OFF _DM_PARALLAX_OFFSET _DM_PARALLAX_POM
    #pragma shader_feature_local_fragment _ _DM_ANTITILE
    #pragma shader_feature_local_fragment _ _DM_SELFSHADOW
    // 2026-10-24s: POM depth offset (SV_Depth) in DepthOnly/GBuffer/Forward so DBuffer decals project onto the displaced surface.
    #pragma shader_feature_local_fragment _ _DEPTHOFFSET_ON

    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR0 _LAYER_MAPPING_TRIPLANAR0
    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR1 _LAYER_MAPPING_TRIPLANAR1
    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR2 _LAYER_MAPPING_TRIPLANAR2
    //#pragma shader_feature _ _LAYER_MAPPING_PLANAR3 _LAYER_MAPPING_TRIPLANAR3

    #pragma shader_feature_local _DISABLE_DECALS
    #pragma shader_feature_local _ADD_PRECOMPUTED_VELOCITY

    #pragma multi_compile _ _ALPHATEST_ON

    // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
    #define _DEFERRED_CAPABLE_MATERIAL

    // Enable the support of global mip bias in the shader.
    // Only has effect if the global mip bias is enabled in shader config and DRS is enabled.
    #define SUPPORT_GLOBAL_MIP_BIAS

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLit_Splatmap_Includes.hlsl"

    ENDHLSL

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType" = "Opaque"
            "SplatCount" = "8"
            "MaskMapR" = "Metallic"
            "MaskMapG" = "AO"
            "MaskMapB" = "Height"
            "MaskMapA" = "Smoothness"
            "DiffuseA" = "Smoothness (becomes Density when Mask map is assigned)"   // when MaskMap is disabled
            "DiffuseA_MaskMapUsed" = "Density"                                      // when MaskMap is enabled
            "TerrainCompatible" = "True"
        }

        // Caution: The outline selection in the editor use the vertex shader/hull/domain shader of the first pass declare. So it should not bethe  meta pass.
        Pass
        {
            Name "GBuffer"
            Tags { "LightMode" = "GBuffer" } // This will be only for opaque object based on the RenderQueue index

            Cull [_CullMode]
            ZTest [_ZTestGBuffer]

            Stencil
            {
                WriteMask [_StencilWriteMaskGBuffer]
                Ref [_StencilRefGBuffer]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
            // All our shaders use same name for entry point
            #pragma vertex Vert
            #pragma fragment Frag

            //enable GPU instancing support
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            // Setup DECALS_OFF so the shader stripper can remove variants
            #pragma multi_compile_fragment DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile_fragment _ DECAL_SURFACE_GRADIENT
            #pragma multi_compile_fragment _ RENDERING_LAYERS

            #define SHADERPASS SHADERPASS_GBUFFER
            #include "DM_TerrainParallax_Template.hlsl"
            #include "DM_TerrainParallax_Splatmap.hlsl"

            ENDHLSL
        }

        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags{ "LightMode" = "META" }

            Cull Off

            HLSLPROGRAM
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
            // All our shaders use same name for entry point
            #pragma vertex Vert
            #pragma fragment Frag

            //enable GPU instancing support
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            // Lightmap memo
            // DYNAMICLIGHTMAP_ON is used when we have an "enlighten lightmap" ie a lightmap updated at runtime by enlighten.This lightmap contain indirect lighting from realtime lights and realtime emissive material.Offline baked lighting(from baked material / light,
            // both direct and indirect lighting) will hand up in the "regular" lightmap->LIGHTMAP_ON.

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #pragma shader_feature EDITOR_VISUALIZATION
            #include "DM_TerrainParallax_Template.hlsl"
            #include "DM_TerrainParallax_Splatmap.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }

            Cull[_CullMode]

            ZClip [_ZClip]
            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
            // All our shaders use same name for entry point
            #pragma vertex Vert
            #pragma fragment Frag

            //enable GPU instancing support
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #define SHADERPASS SHADERPASS_SHADOWS
            #include "DM_TerrainParallax_Template.hlsl"
            #include "DM_TerrainParallax_Splatmap.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{ "LightMode" = "DepthOnly" }

            Cull[_CullMode]

            // To be able to tag stencil with disableSSR information for forward
            Stencil
            {
                WriteMask [_StencilWriteMaskDepth]
                Ref [_StencilRefDepth]
                Comp Always
                Pass Replace
            }

            ZWrite On

            HLSLPROGRAM
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
            // All our shaders use same name for entry point
            #pragma vertex Vert
            #pragma fragment Frag

            //enable GPU instancing support
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            // In deferred, depth only pass don't output anything.
            // In forward it output the normal buffer
            #pragma multi_compile _ WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_DECAL_BUFFER WRITE_RENDERING_LAYER
            #pragma multi_compile _ WRITE_MSAA_DEPTH

            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "DM_TerrainParallax_Template.hlsl"
            #ifdef WRITE_NORMAL_BUFFER
                #if defined(_NORMALMAP)
                    #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Normal0
                #elif defined(_MASKMAP)
                    #define OVERRIDE_SPLAT_SAMPLER_NAME sampler_Mask0
                #endif
            #endif
            #include "DM_TerrainParallax_Splatmap.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "Forward" } // This will be only for transparent object based on the RenderQueue index

            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            // In case of forward we want to have depth equal for opaque mesh
            ZTest [_ZTestDepthEqualForOpaque]
            ZWrite [_ZWrite]
            Cull [_CullMode]

            HLSLPROGRAM
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
            // All our shaders use same name for entry point
            #pragma vertex Vert
            #pragma fragment Frag

            //enable GPU instancing support
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            #pragma multi_compile_fragment SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON
            // Setup DECALS_OFF so the shader stripper can remove variants
            #pragma multi_compile_fragment DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile_fragment _ DECAL_SURFACE_GRADIENT

            // Supported shadow modes per light type
	        #pragma multi_compile_fragment PUNCTUAL_SHADOW_LOW PUNCTUAL_SHADOW_MEDIUM PUNCTUAL_SHADOW_HIGH
	        #pragma multi_compile_fragment DIRECTIONAL_SHADOW_LOW DIRECTIONAL_SHADOW_MEDIUM DIRECTIONAL_SHADOW_HIGH
            #pragma multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH

            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #define SHADERPASS SHADERPASS_FORWARD
            #include "DM_TerrainParallax_Template.hlsl"
            #include "DM_TerrainParallax_Splatmap.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }

            Cull Off

            HLSLPROGRAM
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch switch2
            // All our shaders use same name for entry point
            #pragma vertex Vert
            #pragma fragment Frag

            //enable GPU instancing support
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #pragma editor_sync_compilation
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENESELECTIONPASS
            #include "DM_TerrainParallax_Template.hlsl"
            #include "DM_TerrainParallax_Splatmap.hlsl"

            ENDHLSL
        }

        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
    }

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType" = "Opaque"
            "SplatCount" = "8"
            "MaskMapR" = "Metallic"
            "MaskMapG" = "AO"
            "MaskMapB" = "Height"
            "MaskMapA" = "Smoothness"
            "DiffuseA" = "Smoothness (becomes Density when Mask map is assigned)"   // when MaskMap is disabled
            "DiffuseA_MaskMapUsed" = "Density"                                      // when MaskMap is enabled
            "TerrainCompatible" = "True"
        }

        Pass
        {
            Name "IndirectDXR"
            Tags{ "LightMode" = "IndirectDXR" }

            HLSLPROGRAM

            #pragma only_renderers d3d11 xboxseries ps5 switch2
            #pragma raytracing surface_shader

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile _ DECAL_SURFACE_GRADIENT

            #define PATH_TRACING_CLUSTERED_DECALS
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

            #define SHADERPASS SHADERPASS_RAYTRACING_INDIRECT
            // multi compile that allows us to strip the recursive code
            #pragma multi_compile _ MULTI_BOUNCE_INDIRECT

            #define SHADOW_LOW
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplateRayTracing.hlsl"
            #include "DM_TerrainParallax_Splatmap.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ForwardDXR"
            Tags{ "LightMode" = "ForwardDXR" }

            HLSLPROGRAM
            #pragma only_renderers d3d11 xboxseries ps5 switch2
            #pragma raytracing surface_shader

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile _ DECAL_SURFACE_GRADIENT

            #define PATH_TRACING_CLUSTERED_DECALS
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

            #define SHADERPASS SHADERPASS_RAYTRACING_FORWARD
            // multi compile that allows us to strip the recursive code

            #define SHADOW_LOW

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplateRayTracing.hlsl"
            #include "DM_TerrainParallax_Splatmap.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "GBufferDXR"
            Tags{ "LightMode" = "GBufferDXR" }

            HLSLPROGRAM

            #pragma only_renderers d3d11 xboxseries ps5 switch2
            #pragma raytracing surface_shader

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile _ DECAL_SURFACE_GRADIENT

            #define PATH_TRACING_CLUSTERED_DECALS
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

            #define SHADERPASS SHADERPASS_RAYTRACING_GBUFFER
            #pragma multi_compile _ MINIMAL_GBUFFER
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplateRayTracing.hlsl"
            #include "DM_TerrainParallax_Splatmap.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingGBuffer.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "VisibilityDXR"
            Tags{ "LightMode" = "VisibilityDXR" }

            HLSLPROGRAM

            #pragma only_renderers d3d11 xboxseries ps5 switch2
            #pragma raytracing surface_shader

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

            #define SHADERPASS SHADERPASS_RAYTRACING_VISIBILITY
            #pragma multi_compile _ TRANSPARENT_COLOR_SHADOW
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplateRayTracing.hlsl"
            #include "DM_TerrainParallax_Splatmap.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DebugDXR"
            Tags{ "LightMode" = "DebugDXR" }

            HLSLPROGRAM
            #pragma only_renderers d3d11 xboxseries ps5 switch2
            #pragma raytracing surface_shader

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #define SHADERPASS SHADERPASS_RAYTRACING_DEBUG

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRayTracingDebug.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "PathTracingDXR"
            Tags{ "LightMode" = "PathTracingDXR" }

            HLSLPROGRAM
            #pragma only_renderers d3d11 xboxseries ps5 switch2
            #pragma raytracing surface_shader

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #define SHADERPASS SHADERPASS_PATH_TRACING

            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
            #pragma multi_compile _ DECAL_SURFACE_GRADIENT

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SENSORSDK_OVERRIDE_REFLECTANCE

            #ifdef SENSORSDK_OVERRIDE_REFLECTANCE
                #define SENSORSDK_ENABLE_LIDAR
            #endif

            // This is just because it needs to be defined, shadow maps are not used.
            #define SHADOW_LOW

            // For all single-sided, refractive materials, we want to force a thin refraction model.
            #if !defined(_DOUBLESIDED_ON) && (defined(_REFRACTION_PLANE) || defined(_REFRACTION_SPHERE))
                #undef  _REFRACTION_PLANE
                #undef  _REFRACTION_SPHERE
                #define _REFRACTION_THIN
            #endif

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/TerrainLit/TerrainLitTemplateRayTracing.hlsl"
            #include "DM_TerrainParallax_Splatmap.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPathTracing.hlsl"
            ENDHLSL
        }
    }

    Dependency "BaseMapShader" = "Hidden/HDRP/TerrainLit_Basemap"
    Dependency "BaseMapGenShader" = "Hidden/HDRP/TerrainLit_BasemapGen"
    FallBack "Hidden/HDRP/FallbackError"
    CustomEditor "Project.EditorTools.Rendering.DMTerrainParallaxGUI"
}
