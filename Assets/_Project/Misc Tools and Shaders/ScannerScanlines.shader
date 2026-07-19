Shader "Custom/ScannerPostProcessPBR"
{
    Properties
    {
        _MainTex ("Color Map", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _Occlusion ("Ambient Occlusion", Range(0,1)) = 1.0
        _ScanSpeed ("Scan Speed", Float) = 2.0
        _LineThickness ("Line Thickness", Float) = 200.0
        _ScanColor ("Scan Color", Color) = (0,1,1,0.3)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);

            float _Metallic;
            float _Smoothness;
            float _Occlusion;
            float _ScanSpeed;
            float _LineThickness;
            float4 _ScanColor;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float scan = frac(input.uv.y * _LineThickness + _Time.y * _ScanSpeed);
                scan = step(0.95, scan);

                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float ao = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r * _Occlusion;

                return lerp(color, _ScanColor, scan * _ScanColor.a * ao);
            }
            ENDHLSL
        }
    }
}
