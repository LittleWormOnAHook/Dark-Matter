Shader "Project/EnemyDisintegrate"
{
    Properties
    {
        _BaseMap("Albedo", 2D) = "white" {}
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _DissolveAmount("Dissolve Amount", Range(0, 1)) = 0
        _DissolveEdgeWidth("Dissolve Edge Width", Range(0, 0.2)) = 0.045
        _DissolveEdgeColor("Dissolve Edge Color", Color) = (1, 0.45, 0.1, 1)
        _DissolveNoiseScale("Noise Scale", Float) = 5
        _DissolveSpread("Vertex Spread", Float) = 0.35
    }

    // -------------------------------------------------------------------------
    // URP (playable Quality High / Graphics URP)
    // -------------------------------------------------------------------------
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _DissolveAmount;
                half _DissolveEdgeWidth;
                half4 _DissolveEdgeColor;
                half _DissolveNoiseScale;
                half _DissolveSpread;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float noiseSample = Hash13(input.positionOS.xyz * _DissolveNoiseScale);
                float push = _DissolveAmount * _DissolveSpread * (noiseSample * 2.0 - 1.0);
                float3 positionOS = input.positionOS.xyz + input.normalOS * push;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionWS = vertexInput.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float noiseValue = Hash13(input.positionWS * _DissolveNoiseScale);
                clip(noiseValue - _DissolveAmount);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float edge = smoothstep(_DissolveAmount, _DissolveAmount + _DissolveEdgeWidth, noiseValue);
                half3 color = lerp(_DissolveEdgeColor.rgb, albedo.rgb, edge);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    // -------------------------------------------------------------------------
    // HDRP (Quality HDRP tiers / Genesis_HDRP_Test)
    // -------------------------------------------------------------------------
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType" = "HDUnlitShader"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "ForwardOnly" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _DissolveAmount;
                half _DissolveEdgeWidth;
                half4 _DissolveEdgeColor;
                half _DissolveNoiseScale;
                half _DissolveSpread;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float noiseSample = Hash13(input.positionOS.xyz * _DissolveNoiseScale);
                float push = _DissolveAmount * _DissolveSpread * (noiseSample * 2.0 - 1.0);
                float3 positionOS = input.positionOS.xyz + input.normalOS * push;

                float3 positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                output.positionWS = positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float noiseValue = Hash13(input.positionWS * _DissolveNoiseScale);
                clip(noiseValue - _DissolveAmount);

                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float edge = smoothstep(_DissolveAmount, _DissolveAmount + _DissolveEdgeWidth, noiseValue);
                half3 color = lerp(_DissolveEdgeColor.rgb, albedo.rgb, edge);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
