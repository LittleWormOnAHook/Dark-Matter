Shader "Project/EnemyDissolveSmoke"
{
    Properties
    {
        _BaseColor("Color", Color) = (0.38, 0.38, 0.42, 0.55)
        _SmokeAmount("Smoke Amount", Range(0, 1)) = 0
        _RiseOffset("Rise Offset", Float) = 0
        _NoiseScale("Noise Scale", Float) = 4
        _Expand("Expand", Float) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
        }

        Pass
        {
            Name "Smoke"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _SmokeAmount;
                half _RiseOffset;
                half _NoiseScale;
                half _Expand;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
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

                float3 expanded = input.positionOS.xyz + input.normalOS * _Expand;
                expanded.y += _RiseOffset;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(expanded);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float noise = Hash13(input.positionWS * _NoiseScale);
                float puff = smoothstep(0.02, 0.92, noise);
                float alpha = _BaseColor.a * puff * (1.0 - _SmokeAmount);
                return half4(_BaseColor.rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
