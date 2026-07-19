Shader "Custom/RaisedLetters"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _SpecColor ("Specular Color", Color) = (0.8,0.8,0.8,1)
        _Shininess ("Shininess", Range(1, 100)) = 30
        _Height ("Raised Height", Range(0, 0.1)) = 0.02
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            float4 _Color;
            float4 _SpecColor;
            float _Shininess;
            float _Height;

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz + input.normalOS * _Height;
                float3 positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                float ndotl = saturate(dot(normal, float3(0, 1, 0)));
                float ndotv = saturate(dot(normal, viewDir));
                float3 specular = _SpecColor.rgb * pow(ndotv, _Shininess);

                return float4(_Color.rgb * ndotl + specular, 1);
            }
            ENDHLSL
        }
    }
}
