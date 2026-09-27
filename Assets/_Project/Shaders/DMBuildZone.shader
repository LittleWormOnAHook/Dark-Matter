Shader "Project/DMBuildZone"
{
    // 0926-build-hub: HDRP unlit alpha-blended build zone box drawn around each Build Hub while build mode is on.
    // World-space grid lines on every face, brighter box edges, a low base alpha and a gentle pulse.
    // Cull Off so the zone also reads from inside. Sizes are in meters (the box is axis aligned, scale = zone size).
    Properties
    {
        [HDR]_Color("Fill Color", Color) = (0.25, 0.7, 1.0, 1)
        [HDR]_LineColor("Grid Line Color", Color) = (0.45, 0.95, 1.4, 1)
        [HDR]_EdgeColor("Edge Color", Color) = (0.7, 1.2, 1.8, 1)
        _BaseAlpha("Base Alpha", Range(0, 1)) = 0.035
        _LineAlpha("Grid Line Alpha", Range(0, 1)) = 0.3
        _EdgeAlpha("Edge Alpha", Range(0, 1)) = 0.55
        _CellSize("Grid Cell (m)", Float) = 2
        _LineWidth("Grid Line Width (m)", Float) = 0.06
        _EdgeWidth("Edge Width (m)", Float) = 0.3
        _PulseSpeed("Pulse Speed (Hz)", Float) = 0.5
        _PulseAmount("Pulse Amount", Range(0, 1)) = 0.25
        _FadeDistance("Grid Fade Distance (m)", Float) = 120
    }

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="HDUnlitShader" "Queue"="Transparent" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
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
                float4 _LineColor;
                float4 _EdgeColor;
                float _BaseAlpha;
                float _LineAlpha;
                float _EdgeAlpha;
                float _CellSize;
                float _LineWidth;
                float _EdgeWidth;
                float _PulseSpeed;
                float _PulseAmount;
                float _FadeDistance;
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
                float3 positionOS : TEXCOORD1;
                float3 normalOS : TEXCOORD2;
                float3 scaleWS : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                float4x4 m = GetObjectToWorldMatrix();
                output.scaleWS = float3(length(m._m00_m10_m20), length(m._m01_m11_m21), length(m._m02_m12_m22));
                return output;
            }

            // Anti-aliased grid lines for a face coordinate in meters. Lines never get thinner than a pixel;
            // they dim instead, and fade out where cells shrink to a few pixels (distance, grazing angles).
            float GridLines(float2 coord)
            {
                float cellSize = max(0.05, _CellSize);
                float2 cell = coord / cellSize;
                float2 dist = abs(frac(cell - 0.5) - 0.5) * cellSize;
                float2 w = max(fwidth(coord), 1e-4);
                float2 drawWidth = max(float2(_LineWidth, _LineWidth), w);
                float2 lines = 1.0 - smoothstep(drawWidth * 0.5 - w * 0.5, drawWidth * 0.5 + w * 0.5, dist);
                lines *= saturate(_LineWidth / drawWidth);
                float2 cellsPerPixel = fwidth(cell);
                float moire = saturate((0.4 - max(cellsPerPixel.x, cellsPerPixel.y)) / 0.3);
                return max(lines.x, lines.y) * moire;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float3 absWS = GetAbsolutePositionWS(input.positionWS);
                float3 n = abs(input.normalOS);

                // The two world axes lying in this face.
                float2 coord = n.x > 0.5 ? absWS.yz : (n.y > 0.5 ? absWS.xz : absWS.xy);
                float grid = GridLines(coord);

                // Distance to the nearest box edge on this face, in meters.
                float3 toEdge = (0.5 - abs(input.positionOS)) * input.scaleWS + n * 100000.0;
                float edgeDist = min(toEdge.x, min(toEdge.y, toEdge.z));
                float ew = max(fwidth(edgeDist), 1e-4);
                float edge = 1.0 - smoothstep(_EdgeWidth - ew, _EdgeWidth + ew, edgeDist);

                // HDRP positionWS is camera relative, so its length is the view distance.
                float far = lerp(0.35, 1.0, saturate(1.0 - length(input.positionWS) / max(1.0, _FadeDistance)));
                float pulse = 1.0 + _PulseAmount * sin(t * _PulseSpeed * 6.28318);

                float alpha = (_BaseAlpha + grid * far * _LineAlpha + edge * _EdgeAlpha) * pulse;
                float3 color = lerp(_Color.rgb, _LineColor.rgb, saturate(grid * far));
                color = lerp(color, _EdgeColor.rgb, edge);
                return float4(color, saturate(alpha) * _Color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
