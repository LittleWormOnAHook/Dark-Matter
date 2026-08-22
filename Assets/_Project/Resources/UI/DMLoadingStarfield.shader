Shader "Project/DMLoadingStarfield"
{
    Properties
    {
        [PerRendererData] _MainTex ("Fallback", 2D) = "white" {}
        _SpaceColor ("Space Color", Color) = (0.01, 0.012, 0.02, 1)
        _StarColor ("Star Color", Color) = (0.93, 0.91, 0.89, 1)
        _AccentColor ("Accent Color", Color) = (0.75, 0.18, 0.48, 1)
        _TimeScale ("Fly Speed", Range(0.05, 1.5)) = 0.32
        _StarDensity ("Star Density", Range(4, 32)) = 10
        _StarBrightness ("Star Brightness", Range(0.1, 2.0)) = 0.55
        _StreakLength ("Streak Length", Range(0.0, 0.2)) = 0.04
        _LensStrength ("Lens Strength", Range(0.0, 0.25)) = 0.03
        _LensRadius ("Lens Radius", Range(0.05, 0.5)) = 0.18
        _HoleOcclusion ("Hole Occlusion", Range(0.0, 0.25)) = 0.05
        _Vignette ("Edge Vignette", Range(0.0, 1.0)) = 0.55
        _Aspect ("Aspect (x/y)", Float) = 1.777
        _UnscaledTime ("Unscaled Time", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "DMLoadingStarfield"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _SpaceColor;
            float4 _StarColor;
            float4 _AccentColor;
            float _TimeScale;
            float _StarDensity;
            float _StarBrightness;
            float _StreakLength;
            float _LensStrength;
            float _LensRadius;
            float _HoleOcclusion;
            float _Vignette;
            float _Aspect;
            float _UnscaledTime;
            float4 _ClipRect;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 Hash22(float2 p)
            {
                float n = Hash21(p);
                return float2(n, Hash21(p + n + 19.19));
            }

            // Soft pull toward the hole — keep weak so it never paints concentric rings.
            float2 ApplySubtleLens(float2 centered)
            {
                float r = length(centered);
                float soft = max(_LensRadius * 0.35, 0.002);
                float influence = 1.0 - smoothstep(0.0, _LensRadius, r);
                float bend = _LensStrength * influence * (_LensRadius / (r + soft));
                return centered * (1.0 + bend * 0.35);
            }

            // Sparse distant dust — large cells + heavy cull so transform stars carry the fly-through.
            float StarLayer(float2 centered, float density, float speed, float time, float streakScale)
            {
                float aspect = max(_Aspect, 0.01);
                float2 worldScale = float2(density * aspect, density);
                float accum = 0.0;

                [unroll]
                for (int iz = 0; iz < 2; iz++)
                {
                    float layerBias = (float)iz * 0.41;
                    float zScroll = frac(time * speed + layerBias);

                    float2 projScale = worldScale / max(0.12 + zScroll * 1.15, 0.12);
                    float2 cellF = centered * projScale;
                    float2 cellId = floor(cellF);
                    float2 cellUV = frac(cellF) - 0.5;

                    [unroll]
                    for (int y = -1; y <= 1; y++)
                    {
                        [unroll]
                        for (int x = -1; x <= 1; x++)
                        {
                            float2 offset = float2(x, y);
                            float2 id = cellId + offset;
                            float2 rnd = Hash22(id + density * 17.0 + (float)iz * 73.0);
                            float2 rndB = Hash22(id * 3.17 + 11.3 + (float)iz);

                            // Keep only a minority of cells so spacing stays irregular / sparse.
                            if (rnd.x < 0.86)
                                continue;

                            // Wide jitter breaks the Cartesian lattice into uneven positions.
                            float2 jitter = (rnd - 0.5) * 1.55 + (rndB - 0.5) * 0.45;
                            float2 local = cellUV - offset - jitter;

                            float depth = saturate(zScroll + (rnd.y - 0.5) * 0.2);
                            float nearness = depth * depth;

                            float2 starScreen = (id + 0.5 + jitter) / projScale;
                            float2 flyDir = starScreen / max(length(starScreen), 1e-4);
                            float along = abs(dot(local, flyDir));
                            float across = abs(dot(local, float2(-flyDir.y, flyDir.x)));
                            float streak = 1.0 + _StreakLength * streakScale * nearness * 22.0;
                            float d = length(float2(across, along / streak));

                            // Slightly different star sizes per cell.
                            float sizeMul = lerp(0.7, 1.45, rndB.x);
                            float size = lerp(0.11, 0.028, nearness) * sizeMul;
                            float spark = smoothstep(size, 0.0, d);
                            float twinkle = 0.7 + 0.3 * sin(time * (1.4 + rnd.y * 3.5) + rnd.x * 37.0);

                            float radialFade = smoothstep(0.02, 0.11, length(starScreen));
                            accum += spark * nearness * twinkle * radialFade;
                        }
                    }
                }

                return accum;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv - 0.5;
                centered.x *= max(_Aspect, 0.01);

                float2 lensed = ApplySubtleLens(centered);
                float t = _UnscaledTime * _TimeScale;

                float stars = 0.0;
                stars += StarLayer(lensed, _StarDensity, 0.16, t, 0.7);
                stars += StarLayer(lensed, _StarDensity * 1.25, 0.28, t * 1.12, 0.95) * 0.55;

                float r = length(centered);
                float holeMask = 1.0 - smoothstep(_HoleOcclusion * 0.35, _HoleOcclusion, r);
                stars *= 1.0 - holeMask;

                float accent = StarLayer(lensed, _StarDensity * 0.55, 0.2, t + 4.2, 0.6) * 0.1;
                float3 starRgb = _StarColor.rgb * stars * _StarBrightness
                               + _AccentColor.rgb * accent * _StarBrightness;

                float vignette = lerp(1.0, smoothstep(1.15, 0.22, r), _Vignette);
                float3 rgb = _SpaceColor.rgb * vignette + starRgb * vignette;

                fixed4 col = fixed4(rgb, 1.0) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
