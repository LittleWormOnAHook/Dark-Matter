Shader "Custom/ScannerPostProcess"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _ScanSpeed ("Scan Speed", Float) = 2.0
        _LineThickness ("Line Thickness", Float) = 200.0
        _ScanColor ("Scan Color", Color) = (0, 1, 1, 0.3)
    }

    // URP / legacy OnRenderImage blit
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

    // HDRP Custom Pass / fullscreen blit (same property names)
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="HDRenderPipeline" }
        Pass
        {
            Name "ScannerFullscreen"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

            float _ScanSpeed;
            float _LineThickness;
            float4 _ScanColor;

            float4 Frag(Varyings input) : SV_Target
            {
                // CustomPassCommon Varyings only exposes positionCS; derive UV / pixel coords from it.
                float2 uv = input.positionCS.xy * _ScreenSize.zw;
                uint2 pixelCoords = (uint2)input.positionCS.xy;
                float4 screenColor = float4(CustomPassLoadCameraColor(pixelCoords, 0), 1);
                float scan = frac(uv.y * _LineThickness + _Time.y * _ScanSpeed);
                scan = step(0.95, scan);
                return lerp(screenColor, _ScanColor, scan * _ScanColor.a);
            }
            ENDHLSL
        }

        // Fallback fullscreen using _MainTex when used as a simple blit material
        Pass
        {
            Name "ScannerBlit"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _ScanSpeed;
            float _LineThickness;
            float4 _ScanColor;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
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
