Shader "Project/MoltenGlow"
{
    // Additive glow for molten ammo impacts (DMMoltenImpact): round soft spot from UVs, mottled by noise.
    // Used on the surface glow quad and on drip particles (vertex colour = particle colour over life).
    // _Intensity is screen-relative (written as-is into HDRP's pre-exposed buffer; above 1 blooms) at any exposure.
    Properties
    {
        [HDR] _Color("Color", Color) = (1, 0.45, 0.1, 1)
        _Intensity("Intensity", Float) = 1
        _Softness("Edge Softness", Range(0.05, 1)) = 0.65
        _Noise("Noise", Range(0, 1)) = 0.4
        _Seed("Seed", Float) = 0
        _DepthBias("Depth Bias (m, toward camera)", Float) = 0
    }

    HLSLINCLUDE
    float MoltenHash(float2 p)
    {
        p = frac(p * float2(123.34, 456.21));
        p += dot(p, p + 45.32);
        return frac(p.x * p.y);
    }

    float MoltenValueNoise(float2 p)
    {
        float2 i = floor(p);
        float2 f = frac(p);
        float2 u = f * f * (3.0 - 2.0 * f);
        float a = MoltenHash(i);
        float b = MoltenHash(i + float2(1, 0));
        float c = MoltenHash(i + float2(0, 1));
        float d = MoltenHash(i + float2(1, 1));
        return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
    }

    float MoltenMask(float2 uv, float softness, float noiseAmount, float seed)
    {
        float2 p = uv * 2.0 - 1.0;
        float n = MoltenValueNoise(p * 2.7 + seed) * 0.65 + MoltenValueNoise(p * 6.1 - seed * 1.7) * 0.35;
        float r = length(p) * (1.0 + (n - 0.5) * noiseAmount * 0.8);
        float m = saturate((1.0 - r) / max(softness, 1e-3));
        m *= m;
        return m * lerp(1.0, saturate(0.35 + n * 1.1), noiseAmount);
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "MoltenGlow"
            Tags { "LightMode" = "ForwardOnly" }

            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _Softness;
                float _Noise;
                float _Seed;
                float _DepthBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                // Pull toward the camera so the spot is not buried when the collider surface sits a few cm
                // inside the visible mesh (baked rocks collide on LOD2 but draw LOD0).
                float3 toCam = GetCurrentViewPosition() - positionWS;
                float camDist = length(toCam);
                positionWS += toCam * (min(_DepthBias, camDist * 0.5) / max(camDist, 1e-4));
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float m = MoltenMask(input.uv, _Softness, _Noise, _Seed);
                float3 c = input.color.rgb * _Color.rgb * (input.color.a * _Color.a * _Intensity * m);
                // Screen-relative: written straight into the pre-exposed buffer (1 = white at any exposure).
                // Multiplying by GetCurrentExposureMultiplier() would treat it as physical luminance and make it
                // near-invisible under bright daylight exposure.
                return float4(c, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
