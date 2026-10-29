// 2026-10-24ai: Jupiter + moons in the Io sky. Lit only by the sun direction/lux set by DMIoSkyController
// (_DMSkySunDirWS), so phases are correct day or night. Optional procedural surface for the moons.
// 2026-10-26o: analytic sphere normals + soft limb edge (AA) + Studio rim softness; soft alpha at silhouette.
Shader "DM/IoSkyBody"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Tint / Color A", Color) = (1,1,1,1)
        _ColorB("Color B", Color) = (0.5,0.5,0.5,1)
        _ProcAmount("Procedural", Range(0,1)) = 0
        _NoiseScale("Noise Scale", Float) = 3
        _Lineae("Lineae", Range(0,1)) = 0
        _Spots("Crater Spots", Range(0,1)) = 0
        _Brightness("Brightness", Float) = 1
        _AmbientLux("Ambient Lux", Float) = 0
        _RimColor("Rim Color", Color) = (1,0.7,0.45,1)
        _RimLux("Rim Lux", Float) = 0
        _RimSunBias("Rim Sun Bias", Float) = 3
        _LimbDarkening("Limb Darkening", Range(0,1)) = 0.35
        _RimSoftness("Rim Softness", Range(0.5,16)) = 4
        _EdgeSoft("Edge Soft", Range(0,0.25)) = 0.035
    }

    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }
            Cull Back
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ColorB;
                float _ProcAmount;
                float _NoiseScale;
                float _Lineae;
                float _Spots;
                float _Brightness;
                float _AmbientLux;
                float4 _RimColor;
                float _RimLux;
                float _RimSunBias;
                float _LimbDarkening;
                float _RimSoftness;
                float _EdgeSoft;
            CBUFFER_END

            float4 _DMSkySunDirWS; // xyz = toward sun, w = sun lux

            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 positionRWS : TEXCOORD3;
            };

            float DMHash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float DMNoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(DMHash(i), DMHash(i + float3(1, 0, 0)), f.x), lerp(DMHash(i + float3(0, 1, 0)), DMHash(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(DMHash(i + float3(0, 0, 1)), DMHash(i + float3(1, 0, 1)), f.x), lerp(DMHash(i + float3(0, 1, 1)), DMHash(i + float3(1, 1, 1)), f.x), f.y),
                    f.z);
            }

            float DMFbm(float3 p)
            {
                float s = 0.0;
                float a = 0.5;
                for (int k = 0; k < 5; k++)
                {
                    s += a * DMNoise(p);
                    p *= 2.03;
                    a *= 0.5;
                }
                return s;
            }

            Varyings Vert(Attributes a)
            {
                Varyings o;
                float3 rws = TransformObjectToWorld(a.positionOS);
                o.positionCS = TransformWorldToHClip(rws);
                o.normalWS = TransformObjectToWorldNormal(a.normalOS);
                o.positionOS = a.positionOS;
                o.uv = a.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                o.positionRWS = rws;
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                // 2026-10-26o: analytic unit-sphere normal from OS position (smooth limb even on coarse meshes).
                float3 d = normalize(i.positionOS);
                float3 nA = normalize(TransformObjectToWorldDir(d));
                float3 n = normalize(lerp(normalize(i.normalWS), nA, 0.92));

                float3 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb;
                float f = DMFbm(d * _NoiseScale);
                float3 proc = lerp(_Color.rgb, _ColorB.rgb, smoothstep(0.35, 0.65, f));
                float lin = 1.0 - saturate(abs(DMNoise(d * _NoiseScale * 2.7) - 0.5) * 14.0);
                proc = lerp(proc, _ColorB.rgb * 0.8, lin * _Lineae);
                float sp = smoothstep(0.78, 0.82, DMNoise(d * _NoiseScale * 5.0));
                proc = lerp(proc, float3(0.85, 0.85, 0.82), sp * _Spots);
                float3 albedo = lerp(tex * _Color.rgb, proc, _ProcAmount);

                float3 L = normalize(_DMSkySunDirWS.xyz + 1e-5);
                float ndl = saturate(dot(n, L));
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionRWS);
                float ndv = saturate(dot(n, V));
                float ndvA = saturate(dot(nA, V));
                // 2026-10-26n: floor limb so the rim never goes fully black (was reading as a dark halo on moons).
                float limb = lerp(1.0, max(ndv, 0.28), _LimbDarkening);

                float3 lit = albedo * (ndl * _DMSkySunDirWS.w * _Brightness * limb + _AmbientLux) / PI;
                // Smooth analytic rim; softness = power (higher = tighter/harder glow band).
                float rimPow = max(0.5, _RimSoftness);
                float rim = pow(saturate(1.0 - ndvA), rimPow) * saturate(dot(-V, L) * 0.5 + 0.5) * pow(saturate(dot(nA, L) * 0.5 + 0.5), _RimSunBias);
                lit += _RimColor.rgb * _RimLux * rim;

                // Screen-aware soft silhouette (hides mesh faceting at the limb).
                float edgeW = fwidth(ndvA);
                float edgeSoft = max(_EdgeSoft, edgeW * 1.5);
                float alpha = smoothstep(0.0, edgeSoft, ndvA);

                return float4(lit * GetCurrentExposureMultiplier(), alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
