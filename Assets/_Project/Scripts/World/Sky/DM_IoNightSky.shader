// 2026-10-24au: DM_IoNightSky - procedural starfield + Milky Way + Io aurora on a camera-centred dome.
// Additive transparent, ZWrite Off, ZTest LEqual, no history/temporal/motion vectors -> cannot ghost.
// Drawn behind Jupiter/terrain (depth tested), on top of the HDRP sky. Driven by DMIoNightSky.cs.
Shader "DM/IoNightSky"
{
    Properties
    {
        _NightVis("Night Visibility", Float) = 1
        _AuroraVis("Aurora Visibility", Float) = 1
        _StarDensity("Star Density", Float) = 180
        _StarBrightness("Star Brightness", Float) = 4
        _StarSize("Star Size", Float) = 1
        _Twinkle("Twinkle", Range(0,1)) = 0.3
        _StarColorVar("Star Colour Variation", Range(0,1)) = 0.6
        _MWIntensity("Milky Way Intensity", Float) = 1.5
        _MWWidth("Milky Way Width", Range(0.02,0.6)) = 0.18
        _MWDust("Milky Way Dust", Range(0,1)) = 0.7
        _MWColorA("Milky Way Core", Color) = (1,0.85,0.7,1)
        _MWColorB("Milky Way Edge", Color) = (0.55,0.65,1,1)
        _AuroraIntensity("Aurora Intensity", Float) = 1.5
        _AuroraColorLow("Aurora Low (Na/S blue-green)", Color) = (0.2,0.9,0.75,1)
        _AuroraColorHigh("Aurora High (O red/violet)", Color) = (0.85,0.2,0.55,1)
        _AuroraSpeed("Aurora Speed", Float) = 0.03
        _AuroraHeight("Aurora Height", Range(0.05,0.8)) = 0.3
        _AuroraCoverage("Aurora Coverage", Range(0,1)) = 0.55
        _AuroraMinElevDeg("Min Elevation (deg)", Float) = 0.5
        _AuroraMaxElevDeg("Max Elevation (deg)", Float) = 18
        _AuroraHeightVariationDeg("Height Variation (deg)", Float) = 6
        _AuroraHeightNoiseScale("Variation Scale", Float) = 3
        _AuroraBottomSoftDeg("Bottom Softness (deg)", Float) = 2
        _AuroraTopSoftDeg("Top Softness (deg)", Float) = 8
        _AuroraAzimuthSpanDeg("Azimuth Coverage (deg)", Float) = 360
        _AuroraAzimuthCenterDeg("Azimuth Centre (deg)", Float) = 0
        _AuroraAzimuthEdgeSoftDeg("Azimuth Edge Softness (deg)", Float) = 30
        _AuroraHeightDriftSpeed("Height Drift Speed", Float) = 0.02
        _AuroraHeightBobDeg("Height Drift Amount (deg)", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Front

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _NightVis, _AuroraVis, _StarDensity, _StarBrightness, _StarSize, _Twinkle, _StarColorVar;
                float _MWIntensity, _MWWidth, _MWDust;
                float4 _MWColorA, _MWColorB;
                float _AuroraIntensity;
                float4 _AuroraColorLow, _AuroraColorHigh;
                float _AuroraSpeed, _AuroraHeight, _AuroraCoverage;
                float _AuroraMinElevDeg, _AuroraMaxElevDeg, _AuroraHeightVariationDeg, _AuroraHeightNoiseScale, _AuroraBottomSoftDeg, _AuroraTopSoftDeg, _AuroraAzimuthSpanDeg, _AuroraAzimuthCenterDeg, _AuroraAzimuthEdgeSoftDeg, _AuroraHeightDriftSpeed, _AuroraHeightBobDeg; // 25p
            CBUFFER_END
            float4x4 _DMNightStarRot;   // star sphere rotation (Io clock)
            float4 _DMNightMWNormal;    // Milky Way plane normal (star space)
            float _DMNightTime;
            float4 _DMSkyOcc[8];        // 25o: sky body discs (xyz camera-relative dir, w angular radius rad)
            float _DMSkyOccCount, _DMSkyOccSoft;

            float SkyBodyVis(float3 dW)
            {
                float v = 1.0;
                int n = (int)_DMSkyOccCount;
                for (int k = 0; k < 8; k++)
                {
                    if (k >= n) break;
                    float a = acos(saturate(dot(dW, _DMSkyOcc[k].xyz)));
                    v *= smoothstep(_DMSkyOcc[k].w, _DMSkyOcc[k].w + _DMSkyOccSoft, a);
                }
                return v;
            }

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dirWS : TEXCOORD0; float3 dirOS : TEXCOORD1; };

            float H1(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float3 H3(float3 p) { return float3(H1(p), H1(p + 17.31), H1(p + 41.73)); }
            float N3(float3 x)
            {
                float3 i = floor(x); float3 f = frac(x); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(H1(i), H1(i + float3(1,0,0)), f.x), lerp(H1(i + float3(0,1,0)), H1(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(H1(i + float3(0,0,1)), H1(i + float3(1,0,1)), f.x), lerp(H1(i + float3(0,1,1)), H1(i + float3(1,1,1)), f.x), f.y), f.z);
            }
            float Fbm(float3 p) { float s = 0, a = 0.5; for (int k = 0; k < 4; k++) { s += a * N3(p); p *= 2.07; a *= 0.5; } return s; }

            Varyings Vert(Attributes a)
            {
                Varyings o;
                float3 rws = TransformObjectToWorld(a.positionOS);
                o.positionCS = TransformWorldToHClip(rws);
                o.dirWS = mul((float3x3)GetObjectToWorldMatrix(), a.positionOS);
                o.dirOS = a.positionOS;
                return o;
            }

            float3 StarLayer(float3 d, float scale, float seed, float px)
            {
                float3 g = d * scale;
                float3 cell = floor(g);
                float3 h = H3(cell + seed);
                if (h.x > 0.35) return 0;                       // ~35% of cells hold a star
                float3 sp = normalize((cell + 0.15 + 0.7 * H3(cell + seed + 7.1)) / scale);
                float ang = length(d - sp) * scale;             // distance in cell units
                float size = (0.04 + 0.10 * h.y * h.y) * _StarSize;
                float aa = max(px * scale, 1e-4);
                float s = 1.0 - smoothstep(size - aa, size + aa, ang);
                float tw = 1.0 - _Twinkle * 0.5 * (1.0 + sin(_DMNightTime * (2.0 + 6.0 * h.z) + h.y * 40.0));
                float t = h.z;                                   // colour temperature
                float3 col = lerp(float3(1,1,1), t < 0.33 ? float3(0.65,0.75,1.0) : (t < 0.66 ? float3(1,0.95,0.85) : float3(1,0.7,0.45)), _StarColorVar);
                return col * s * tw * (0.3 + 1.7 * h.y * h.y * h.y);
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float3 dW = normalize(i.dirWS);
                float px = length(fwidth(dW));
                float3 d = mul((float3x3)_DMNightStarRot, dW);
                float3 col = 0;

                if (_NightVis > 0.001)
                {
                    float3 st = StarLayer(d, _StarDensity, 0.0, px) + 0.6 * StarLayer(d, _StarDensity * 2.3, 13.0, px);
                    col += st * _StarBrightness;
                    float3 n = normalize(_DMNightMWNormal.xyz + 1e-5);
                    float b = dot(d, n);
                    float band = exp(-b * b / (_MWWidth * _MWWidth));
                    float core = exp(-b * b / (_MWWidth * _MWWidth * 0.15));
                    float f = Fbm(d * 6.0);
                    float dust = 1.0 - _MWDust * smoothstep(0.42, 0.68, Fbm(d * 11.0 + 3.1)) * core;
                    float glow = band * (0.45 + 0.8 * f) * dust;
                    col += lerp(_MWColorB.rgb, _MWColorA.rgb, core) * glow * _MWIntensity;
                    col += StarLayer(d, _StarDensity * 4.0, 29.0, px) * band * _StarBrightness * 0.5;   // dense band stars
                    col *= _NightVis * SkyBodyVis(dW); // 25o: stars always behind Jupiter / moons
                }

                if (_AuroraVis > 0.001 && _AuroraIntensity > 0.0)
                {
                    float3 dA = normalize(i.dirOS); // 24aw: dome yaw = Sky Rotation
                    float el = dA.y;
                    float az = atan2(dA.z, dA.x);
                    float t = _DMNightTime * _AuroraSpeed;
                    // 25p: variable height coverage - elevation band with noise-varied edges, azimuth span, soft edges, drift.
                    float elD = degrees(asin(clamp(el, -1.0, 1.0)));
                    float hdg = degrees(atan2(dA.x, dA.z));
                    float da = abs(fmod(hdg - _AuroraAzimuthCenterDeg + 540.0, 360.0) - 180.0);
                    float halfSpan = 0.5 * _AuroraAzimuthSpanDeg;
                    float azMask = _AuroraAzimuthSpanDeg >= 359.9 ? 1.0 : 1.0 - smoothstep(max(halfSpan - _AuroraAzimuthEdgeSoftDeg, 0.0), halfSpan + 1e-3, da);
                    float dt = _DMNightTime * _AuroraHeightDriftSpeed;
                    float2 ring = float2(cos(az), sin(az)) * _AuroraHeightNoiseScale;
                    float nTop = Fbm(float3(ring, dt + 3.7)) * 2.0 - 1.0;
                    float nBot = Fbm(float3(ring * 0.8 + 11.3, dt * 0.8 + 7.1)) * 2.0 - 1.0;
                    float bob = _AuroraHeightBobDeg * sin(dt * 6.2831 + az);
                    float lo = _AuroraMinElevDeg + nBot * _AuroraHeightVariationDeg * 0.5 + bob;
                    float hi = max(_AuroraMaxElevDeg + nTop * _AuroraHeightVariationDeg + bob, lo + 0.5);
                    float prof = azMask * smoothstep(lo, lo + _AuroraBottomSoftDeg, elD) * (1.0 - smoothstep(hi - _AuroraTopSoftDeg, hi, elD));
                    if (prof > 0.0)
                    {
                        float curtain = Fbm(float3(az * 2.2 + t, t * 0.7, 1.3));
                        float cov = smoothstep(1.0 - _AuroraCoverage, 1.0 - _AuroraCoverage + 0.25, curtain);
                        float rays = 0.55 + 0.45 * N3(float3(az * 60.0 + curtain * 6.0, t * 3.0, 5.0));
                        float k = saturate((elD - lo) / max(hi - lo, 1e-3));
                        float3 ac = lerp(_AuroraColorLow.rgb, _AuroraColorHigh.rgb, smoothstep(0.15, 0.85, k));
                        col += ac * prof * cov * rays * _AuroraIntensity * _AuroraVis * SkyBodyVis(dW); // 25p: aurora behind Jupiter / moons too
                    }
                }
                return float4(col * GetCurrentExposureMultiplier(), 0);
            }
            ENDHLSL
        }
    }
}