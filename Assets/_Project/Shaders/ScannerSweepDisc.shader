Shader "Project/ScannerSweepDisc"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.83, 0.63, 0.09, 1)
        _GridColor ("Grid Color", Color) = (0.92, 0.78, 0.28, 1)
        _GridSize ("Grid Size (m)", Float) = 0.75
        _GridLineWidth ("Grid Line Width", Range(0.01, 0.2)) = 0.045
        _GridAlpha ("Grid Alpha", Range(0, 1)) = 0.8
    }

    // -------------------------------------------------------------------------
    // URP
    // -------------------------------------------------------------------------
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "UnlitVertexColorGrid"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GridColor;
                float _GridSize;
                float _GridLineWidth;
                float _GridAlpha;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.color = input.color * _BaseColor;
                return output;
            }

            // Thin world-XZ grid lines layered over the soft disc fill.
            half SampleGrid(float3 positionWS)
            {
                float cell = max(0.05, _GridSize);
                float2 uv = positionWS.xz / cell;
                float2 grid = abs(frac(uv - 0.5) - 0.5);
                float2 fw = max(fwidth(uv), float2(1e-5, 1e-5));
                float halfWidth = max(0.01, _GridLineWidth) * 0.5;
                float2 gridLine = 1.0 - smoothstep(halfWidth, halfWidth + fw, grid);
                return saturate(max(gridLine.x, gridLine.y));
            }

            half4 frag(Varyings input) : SV_Target
            {
                half fillA = input.color.a;
                half grid = SampleGrid(input.positionWS);
                half gridA = grid * _GridAlpha;

                half3 rgb = lerp(input.color.rgb, _GridColor.rgb, grid * 0.65);
                // Grid sits on top of the faint fill inside the disc mesh.
                half a = saturate(fillA + gridA);
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    // -------------------------------------------------------------------------
    // HDRP
    // -------------------------------------------------------------------------
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "HDRenderPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "ForwardOnly" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GridColor;
                float _GridSize;
                float _GridLineWidth;
                float _GridAlpha;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.color = input.color * _BaseColor;
                return output;
            }

            half SampleGrid(float3 positionWS)
            {
                float cell = max(0.05, _GridSize);
                float2 uv = positionWS.xz / cell;
                float2 grid = abs(frac(uv - 0.5) - 0.5);
                float2 fw = max(fwidth(uv), float2(1e-5, 1e-5));
                float halfWidth = max(0.01, _GridLineWidth) * 0.5;
                float2 gridLine = 1.0 - smoothstep(halfWidth, halfWidth + fw, grid);
                return saturate(max(gridLine.x, gridLine.y));
            }

            half4 frag(Varyings input) : SV_Target
            {
                half fillA = input.color.a;
                half grid = SampleGrid(input.positionWS);
                half gridA = grid * _GridAlpha;

                half3 rgb = lerp(input.color.rgb, _GridColor.rgb, grid * 0.65);
                half a = saturate(fillA + gridA);
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
