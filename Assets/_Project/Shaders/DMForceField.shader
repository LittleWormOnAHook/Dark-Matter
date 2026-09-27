Shader "Project/DMForceField"
{
    // 0926-force-fields: HDRP unlit additive energy field used by every force field door.
    // Colour, opacity, edge glow, pattern and ripples are driven per renderer by DMForceField (Creation Effects profile).
    Properties
    {
        [HDR]_Color("Color", Color) = (0.25, 0.75, 1.6, 1)
        [HDR]_EdgeColor("Edge Color", Color) = (0.6, 1.2, 2.4, 1)
        _Opacity("Idle Opacity", Range(0, 1)) = 0.25
        _EdgeGlow("Edge Glow", Range(0, 4)) = 1.5
        _EdgeWidth("Edge Width", Range(0.005, 0.5)) = 0.06
        _PatternScale("Pattern Scale", Float) = 1.2
        _ScrollSpeed("Scroll Speed", Float) = 0.35
        _Pulse("Pulse", Range(0, 4)) = 0
        _RippleSpeed("Ripple Speed", Float) = 3
        _RippleWidth("Ripple Width", Float) = 0.35
        _RippleLife("Ripple Life", Float) = 0.6
        _Ripple0("Ripple 0", Vector) = (0, 0, 0, -100)
        _Ripple1("Ripple 1", Vector) = (0, 0, 0, -100)
        _Ripple2("Ripple 2", Vector) = (0, 0, 0, -100)
        _Ripple3("Ripple 3", Vector) = (0, 0, 0, -100)
    }

    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="HDUnlitShader" "Queue"="Transparent" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            Blend SrcAlpha One
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
                float4 _EdgeColor;
                float _Opacity;
                float _EdgeGlow;
                float _EdgeWidth;
                float _PatternScale;
                float _ScrollSpeed;
                float _Pulse;
                float _RippleSpeed;
                float _RippleWidth;
                float _RippleLife;
                float4 _Ripple0;
                float4 _Ripple1;
                float4 _Ripple2;
                float4 _Ripple3;
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
                float3 normalWS : TEXCOORD2;
            };

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float Noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = lerp(Hash13(i), Hash13(i + float3(1, 0, 0)), f.x);
                float b = lerp(Hash13(i + float3(0, 1, 0)), Hash13(i + float3(1, 1, 0)), f.x);
                float c = lerp(Hash13(i + float3(0, 0, 1)), Hash13(i + float3(1, 0, 1)), f.x);
                float d = lerp(Hash13(i + float3(0, 1, 1)), Hash13(i + float3(1, 1, 1)), f.x);
                return lerp(lerp(a, b, f.y), lerp(c, d, f.y), f.z);
            }

            float Ripple(float4 r, float3 pos, float t)
            {
                float age = t - r.w;
                if (age < 0.0 || age > _RippleLife)
                    return 0.0;
                float radius = age * _RippleSpeed;
                float ring = 1.0 - saturate(abs(distance(pos, r.xyz) - radius) / max(0.01, _RippleWidth));
                float fade = 1.0 - age / max(0.01, _RippleLife);
                return ring * ring * fade;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float3 absWS = GetAbsolutePositionWS(input.positionWS);
                float3 p = absWS * _PatternScale;
                float scroll = t * _ScrollSpeed;

                float n1 = Noise3(p + float3(0.0, scroll, scroll * 0.6));
                float n2 = Noise3(p * 2.7 - float3(scroll * 1.3, 0.0, scroll * 0.4));
                float shimmer = n1 * 0.6 + n2 * 0.4;
                float bands = pow(abs(sin((absWS.y * _PatternScale + n1 * 1.5) * 3.14159 + t * 1.5)), 12.0);

                float2 e = min(input.uv, 1.0 - input.uv);
                float edge = 1.0 - saturate(min(e.x, e.y) / max(0.001, _EdgeWidth));

                float3 N = normalize(input.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float fresnel = pow(1.0 - saturate(abs(dot(N, V))), 3.0);

                float rip = Ripple(_Ripple0, absWS, t) + Ripple(_Ripple1, absWS, t)
                          + Ripple(_Ripple2, absWS, t) + Ripple(_Ripple3, absWS, t);

                float alpha = _Opacity * (0.55 + shimmer * 0.9) + bands * 0.15 + edge * _EdgeGlow * 0.5
                            + fresnel * 0.35 + rip * 0.9 + _Pulse * 0.25;
                float3 color = _Color.rgb * (0.6 + shimmer + bands * 0.5 + _Pulse)
                             + _EdgeColor.rgb * (edge * _EdgeGlow + rip * 2.0 + fresnel * 0.5);
                return float4(color, saturate(alpha) * _Color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
