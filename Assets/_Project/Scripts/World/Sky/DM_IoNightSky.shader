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
        _MWVis("Milky Way Visibility", Float) = 1
        _MWSmoothness("Milky Way Smoothness", Range(0,1)) = 0.55
        _AuroraSmoothness("Aurora Smoothness", Range(0,1)) = 0.55
        _DitherEnabled("Dither Enabled", Float) = 1
        _DitherAmount("Dither Amount", Range(0,2)) = 0.55
        _SoftExpPower("Soft Exposure Power", Range(0.1,1)) = 0.28
        _AuroraOverBodies("Aurora Over Bodies", Float) = 1
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
                float _MWIntensity, _MWWidth, _MWDust, _MWVis, _MWSmoothness;
                float4 _MWColorA, _MWColorB;
                float _AuroraIntensity, _AuroraSmoothness;
                float _DitherEnabled, _DitherAmount, _SoftExpPower, _AuroraOverBodies;
                float4 _AuroraColorLow, _AuroraColorHigh;
                float _AuroraSpeed, _AuroraHeight, _AuroraCoverage;
                float _AuroraMinElevDeg, _AuroraMaxElevDeg, _AuroraHeightVariationDeg, _AuroraHeightNoiseScale, _AuroraBottomSoftDeg, _AuroraTopSoftDeg, _AuroraAzimuthSpanDeg, _AuroraAzimuthCenterDeg, _AuroraAzimuthEdgeSoftDeg, _AuroraHeightDriftSpeed, _AuroraHeightBobDeg; // 25p
            CBUFFER_END
            float4x4 _DMNightStarRot;   // star sphere rotation (Io clock)
            float4 _DMNightMWNormal;    // Milky Way plane normal (star space)
            float _DMNightTime;
            float4 _DMSkyOcc[8];        // 25o: sky body discs (xyz camera-relative dir, w angular radius rad)
            float _DMSkyOccCount, _DMSkyOccSoft;

            // 2026-10-26n: tight disc cutout — soft edge straddles the limb (mostly inside, tiny outside) so no dark halo ring.
            float SkyBodyVis(float3 dW)
            {
                float v = 1.0;
                int n = (int)_DMSkyOccCount;
                for (int k = 0; k < 8; k++)
                {
                    if (k >= n) break;
                    float a = acos(saturate(dot(dW, _DMSkyOcc[k].xyz)));
                    float r = max(_DMSkyOcc[k].w, 0.0);
                    float s = max(_DMSkyOccSoft, 1e-5);
                    v *= smoothstep(r - s, r + s * 0.35, a);
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
            // 2026-10-26m: 6-octave rotated FBM + light domain warp (kills large blotchy islands from 4-oct value noise).
            float3 RotY(float3 p, float a) { float s = sin(a), c = cos(a); return float3(c * p.x + s * p.z, p.y, -s * p.x + c * p.z); }
            float3 RotX(float3 p, float a) { float s = sin(a), c = cos(a); return float3(p.x, c * p.y - s * p.z, s * p.y + c * p.z); }
            float Fbm(float3 p)
            {
                float s = 0.0, a = 0.5;
                for (int k = 0; k < 6; k++)
                {
                    s += a * N3(p);
                    p = RotY(RotX(p, 0.71 + 0.17 * k), 1.07 + 0.23 * k) * 2.11;
                    a *= 0.5;
                }
                return s;
            }
            float FbmWarp(float3 p)
            {
                float3 q = float3(Fbm(p + float3(0.0, 1.7, 3.1)), Fbm(p + float3(4.2, 0.3, 1.9)), Fbm(p + float3(2.4, 5.1, 0.7)));
                return Fbm(p + 0.55 * (q * 2.0 - 1.0));
            }
            float TriDither(float2 p)
            {
                // Interleaved gradient noise, triangularized (blue-noise-ish screen dither for R11G11B10 banding).
                float n1 = frac(52.9829189 * frac(dot(p, float2(0.06711056, 0.00583715))));
                float n2 = frac(52.9829189 * frac(dot(p + 197.0, float2(0.06711056, 0.00583715))));
                return n1 + n2 - 1.0; // ~-1..1 triangular
            }

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

                float bodyVis = SkyBodyVis(dW);
                float3 starCol = 0;
                float3 mwCol = 0;
                float3 aurCol = 0;
                if (_NightVis > 0.001 || _MWVis > 0.001)
                {
                    if (_NightVis > 0.001)
                    {
                        float3 st = StarLayer(d, _StarDensity, 0.0, px) + 0.6 * StarLayer(d, _StarDensity * 2.3, 13.0, px);
                        starCol += st * _StarBrightness;
                    }
                    if (_MWVis > 0.001 && _MWIntensity > 0.0)
                    {
                        // 2026-10-26i: smoother dust/band (Smoothness), separate day vis; tempered later so AE does not amplify blotches.
                        float sm = saturate(_MWSmoothness);
                        float3 n = normalize(_DMNightMWNormal.xyz + 1e-5);
                        float b = dot(d, n);
                        float band = exp(-b * b / (_MWWidth * _MWWidth));
                        float core = exp(-b * b / (_MWWidth * _MWWidth * 0.15));
                        float freq = lerp(7.5, 3.2, sm);
                        float f = FbmWarp(d * freq);
                        float f2 = FbmWarp(d * (freq * 1.55) + 3.1);
                        float softLo = lerp(0.42, 0.28, sm);
                        float softHi = lerp(0.68, 0.82, sm);
                        float dust = 1.0 - _MWDust * smoothstep(softLo, softHi, f2) * core;
                        float glow = band * (0.45 + 0.8 * f) * dust;
                        // Soften glow edges with smoothness so bands are not hard islands.
                        glow *= lerp(1.0, smoothstep(0.02, 0.35, band), sm);
                        mwCol += lerp(_MWColorB.rgb, _MWColorA.rgb, core) * glow * _MWIntensity;
                        if (_NightVis > 0.001)
                            starCol += StarLayer(d, _StarDensity * 4.0, 29.0, px) * band * _StarBrightness * 0.5;
                    }
                    starCol *= _NightVis * bodyVis;
                    mwCol *= _MWVis * bodyVis;
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
                        float smA = saturate(_AuroraSmoothness);
                        float azScale = lerp(2.6, 1.2, smA);
                        float curtain = FbmWarp(float3(az * azScale + t, t * 0.7, 1.3));
                        float covW = lerp(0.25, 0.55, smA);
                        float cov = smoothstep(1.0 - _AuroraCoverage, 1.0 - _AuroraCoverage + covW, curtain);
                        float rayFreq = lerp(60.0, 18.0, smA);
                        float rays = 0.55 + 0.45 * N3(float3(az * rayFreq + curtain * 6.0, t * 3.0, 5.0));
                        rays = lerp(rays, 0.55 + 0.45 * curtain, smA); // blend toward soft curtain at high smoothness
                        float k = saturate((elD - lo) / max(hi - lo, 1e-3));
                        float3 ac = lerp(_AuroraColorLow.rgb, _AuroraColorHigh.rgb, smoothstep(0.15, 0.85, k));
                        // 2026-10-26n: aurora is atmospheric — draws over moons/Jupiter when _AuroraOverBodies; stars/MW still use bodyVis.
                        float aurBody = _AuroraOverBodies > 0.5 ? 1.0 : bodyVis;
                        aurCol += ac * prof * cov * rays * _AuroraIntensity * _AuroraVis * aurBody;
                    }
                }
                // 2026-10-26i/m: stars full exposure; MW/aurora use tempered softExp (power < 0.5) so AE does not amplify bands.
                float expMul = GetCurrentExposureMultiplier();
                float softExp = pow(max(expMul, 1e-4), max(_SoftExpPower, 0.1));
                col = starCol * expMul + (mwCol + aurCol) * softExp;
                // 2026-10-26m: triangular/IGN dither after exposure, before write (HDRP R11G11B10 banding).
                if (_DitherEnabled > 0.5)
                {
                    float dn = TriDither(i.positionCS.xy);
                    float luma = max(max(col.r, col.g), max(col.b, 1e-4));
                    float amp = _DitherAmount * (luma * 0.012 + 1.0 / 512.0);
                    col += dn * amp;
                }
                return float4(col, 0);
            }
            ENDHLSL
        }
    }
}