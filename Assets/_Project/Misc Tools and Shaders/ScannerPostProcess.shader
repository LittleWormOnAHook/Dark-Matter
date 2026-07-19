Shader "Custom/ScannerPostProcess"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _ScanSpeed ("Scan Speed", Float) = 2.0
        _LineThickness ("Line Thickness", Float) = 200.0
        _ScanColor ("Scan Color", Color) = (0, 1, 1, 0.3)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _ScanSpeed;
            float _LineThickness;
            float4 _ScanColor;

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

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 screenColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float scan = frac(input.uv.y * _LineThickness + _Time.y * _ScanSpeed);
                scan = step(0.95, scan);
                return lerp(screenColor, _ScanColor, scan * _ScanColor.a);
            }
            ENDHLSL
        }
    }
}
