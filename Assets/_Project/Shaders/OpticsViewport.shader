Shader "Project/OpticsViewport"
{
    Properties
    {
        [PerRendererData] _MainTex ("Optics Render", 2D) = "white" {}
        _Radius ("Viewport Radius", Range(0.15, 0.48)) = 0.34
        _RectHalfWidth ("Rect Half Width", Range(0.15, 0.5)) = 0.4
        _RectHalfHeight ("Rect Half Height", Range(0.1, 0.35)) = 0.24
        _EdgeSoftness ("Edge Softness", Range(0.005, 0.15)) = 0.025
        _ScannerFuzz ("Scanner Fuzz", Range(0.0, 0.12)) = 0.045
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Mode ("Mode (0=Binoculars 1=Scanner)", Float) = 0
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Radius;
            float _RectHalfWidth;
            float _RectHalfHeight;
            float _EdgeSoftness;
            float _ScannerFuzz;
            float4 _Tint;
            float _Mode;

            float2 AspectCorrect(float2 uv)
            {
                float2 centered = uv - 0.5;
                centered.x *= _ScreenParams.x / max(_ScreenParams.y, 1.0);
                return centered;
            }

            float CircleMask(float2 centered, float radius, float softness)
            {
                float dist = length(centered);
                float inner = radius - softness;
                float outer = radius + softness;
                return 1.0 - smoothstep(inner, outer, dist);
            }

            float RectMask(float2 centered, float halfWidth, float halfHeight, float softness)
            {
                float2 absCoord = abs(centered);
                float edgeX = 1.0 - smoothstep(halfWidth - softness, halfWidth + softness, absCoord.x);
                float edgeY = 1.0 - smoothstep(halfHeight - softness, halfHeight + softness, absCoord.y);
                return saturate(edgeX * edgeY);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                float2 centered = AspectCorrect(i.uv);
                bool scanner = _Mode > 0.5;
                float softness = _EdgeSoftness + (scanner ? _ScannerFuzz : 0.0);

                float mask = scanner
                    ? RectMask(centered, _RectHalfWidth, _RectHalfHeight, softness)
                    : CircleMask(centered, _Radius, softness);

                if (scanner)
                {
                    float scanPulse = 0.92 + 0.08 * sin(_Time.y * 4.0);
                    fixed4 scanTint = fixed4(0.75, 1.0, 0.88, 1.0) * scanPulse;
                    col.rgb = lerp(col.rgb, col.rgb * scanTint.rgb, 0.22);
                    col.rgb *= _Tint.rgb;

                    float2 absCoord = abs(centered);
                    float edgeDist = max(
                        absCoord.x - _RectHalfWidth,
                        absCoord.y - _RectHalfHeight);
                    float fuzz = smoothstep(0.0, _ScannerFuzz * 2.0, edgeDist);
                    col.rgb = lerp(col.rgb, col.rgb * 0.65, fuzz * 0.35);
                }
                else
                {
                    col.rgb *= _Tint.rgb;
                }

                col.a *= mask;
                return col;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
