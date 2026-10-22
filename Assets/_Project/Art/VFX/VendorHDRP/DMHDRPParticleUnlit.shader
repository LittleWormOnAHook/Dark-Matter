// 2026-10-21f: HDRP replacement for FORGE3D/Additive, Alpha Blended, Billboard Additive (vendor ammo VFX).
// Keeps particle vertex color and tint. Mode 0 = Additive (rgb = tex*tex.a*tint*vc*vc.a*boost), 1 = Alpha (FORGE3D Alpha Blended).
Shader "DarkMatter/HDRP Particle Unlit"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _TintColor("Tint", Color) = (1,1,1,1)
        _Boost("Boost", Float) = 1
        _Mode("Mode (0 Additive, 1 Alpha)", Float) = 0
        _ExposureWeight("Exposure Weight (0 = vendor look, like HDRP Unlit; 1 = scale by camera exposure)", Range(0,1)) = 0
        [HideInInspector] _SrcBlend("Src", Float) = 1
        [HideInInspector] _DstBlend("Dst", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="HDRenderPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST; float4 _TintColor; float _Boost; float _Mode; float _ExposureWeight; float _SrcBlend; float _DstBlend;
            CBUFFER_END

            struct A { float3 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            V vert(A i)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 posWS = TransformObjectToWorld(i.positionOS);
                o.positionCS = TransformWorldToHClip(posWS);
                o.color = i.color;
                o.uv = i.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return o;
            }

            float4 frag(V i) : SV_Target
            {
                float4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float ex = lerp(1.0, GetCurrentExposureMultiplier(), _ExposureWeight);
                if (_Mode < 0.5)
                {
                    float3 c = t.a * t.rgb * _TintColor.rgb * i.color.rgb * i.color.a * _Boost;
                    return float4(c * ex, 1);
                }
                float3 rgb = t.rgb * _TintColor.rgb * i.color.rgb * _Boost;
                float a = saturate(t.a * _TintColor.a * i.color.a);
                return float4(rgb * ex, a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
