Shader "KriptoFX/BFX/BFX_Blood"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)

        _boundingMax("Bounding Max", Float) = 1.0
        _boundingMin("Bounding Min", Float) = 1.0
        _numOfFrames("Number Of Frames", int) = 240
        _speed("Speed", Float) = 0.33
        _HeightOffset("_Height Offset", Vector) = (0, 0, 0)
        //[MaterialToggle] _pack_normal("Pack Normal", Float) = 0
        _posTex("Position Map (RGB)", 2D) = "white" {}
        _nTex("Normal Map (RGB)", 2D) = "grey" {}
        _SunPos("Sun Pos", Vector) = (1, 0.5, 1, 0)


    }
    SubShader
    {

         Tags{ "Queue" = "Transparent"}
        //Blend SrcAlpha OneMinusSrcAlpha
        //    Blend DstColor Zero
        Cull Back
        ZWrite On


        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.6

            #pragma multi_compile_instancing
            #define USE_FOG


                    #include "Packages/com.unity.render-pipelines.high-definition-config/Runtime/ShaderConfig.cs.hlsl"
                    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
                    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 tangent : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;

                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD2;
                float4 screenPos : TEXCOORD4;
                float3 viewDir : TEXCOORD5;
                float height : TEXCOORD6;
               // UNITY_FOG_COORDS(8)

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _GrabTexture;
            sampler2D _posTex;
            sampler2D _nTex;
            //uniform float _pack_normal;
            uniform float _boundingMax;
            uniform float _boundingMin;
            uniform float _speed;
            uniform int _numOfFrames;
            half4 _Color;

            float4 _HeightOffset;
            float _HDRFix;
            float4 _SunPos;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _UseCustomTime)
                UNITY_DEFINE_INSTANCED_PROP(float, _TimeInFrames)
                //UNITY_DEFINE_INSTANCED_PROP(float4, _SunPos)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightIntencity)
            UNITY_INSTANCING_BUFFER_END(Props)

            inline float4 UnityObjectToClipPos(in float4 pos)
            {
                return mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(pos.xyz, 1.0)));
            }

            inline half3 LinearToGammaSpace(half3 linRGB)
            {
                linRGB = max(linRGB, half3(0.h, 0.h, 0.h));
                // An almost-perfect approximation from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
                return max(1.055h * pow(linRGB, 0.416666667h) - 0.055h, 0.h);
            }

           inline float4 ComputeGrabScreenPos(float4 pos) {
#if UNITY_UV_STARTS_AT_TOP
                float scale = -1.0;
#else
                float scale = 1.0;
#endif
                float4 o = pos * 0.5f;
                o.xy = float2(o.x, o.y * scale) + o.w;
#ifdef UNITY_SINGLE_PASS_STEREO
                o.xy = TransformStereoScreenSpaceTex(o.xy, pos.w);
#endif
                o.zw = pos.zw;
                return o;
            }

            v2f vert (appdata v)
            {
                v2f o;

              //  UNITY_INITIALIZE_OUTPUT(v2f, o);

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float timeInFrames;
                float currentSpeed = 1.0f / (_numOfFrames / _speed);
                //timeInFrames = ((ceil(frac(-_SunPos.w * currentSpeed) * _numOfFrames)) / _numOfFrames) + (1.0 / _numOfFrames);
                //fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _TimeInFrames);
                timeInFrames = UNITY_ACCESS_INSTANCED_PROP(Props, _UseCustomTime) > 0.5 ? UNITY_ACCESS_INSTANCED_PROP(Props, _TimeInFrames) : 1;

                float4 texturePos = tex2Dlod(_posTex, float4(v.uv.x, (timeInFrames + v.uv.y), 0, 0));
                float3 textureN = tex2Dlod(_nTex, float4(v.uv.x, (timeInFrames + v.uv.y), 0, 0));


#if !UNITY_COLORSPACE_GAMMA
                texturePos.xyz = LinearToGammaSpace(texturePos.xyz);
                textureN = LinearToGammaSpace(textureN);
#endif

                float expand = _boundingMax - _boundingMin;
                texturePos.xyz *= expand;
                texturePos.xyz += _boundingMin;
                texturePos.x *= -1;
                v.vertex.xyz = texturePos.xzy;
                v.vertex.xyz += _HeightOffset.xyz;

                o.worldNormal = textureN.xzy * 2 - 1;
                o.worldNormal.x *= -1;

                //o.normal.y = o.worldNormal.y;
                //o.normal.xz = (mul((float3x3)UNITY_MATRIX_IT_MV, half3(o.worldNormal.x, 0, o.worldNormal.z))).xz;
                //o.normal = o.normal;
                float3 worldPos = mul(UNITY_MATRIX_M, v.vertex).xyz;
                o.viewDir = GetWorldSpaceNormalizeViewDir(GetCameraRelativePositionWS(worldPos));
                //o.pos = v.vertex.xyz;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeGrabScreenPos(o.pos);


               // UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }



            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

           // return 1;
                //i.normal = normalize(i.normal);
                i.worldNormal = normalize(i.worldNormal);
                i.viewDir = normalize(i.viewDir);

                half fresnel = saturate(1 - dot(i.worldNormal, i.viewDir));
               // i.screenPos.xy += lerp(i.worldNormal.xz * 0.5, -i.worldNormal.xz * .5, fresnel);
                //i.screenPos.xy += lerp(i.normal.xz * 2.5, -i.normal.xz * 2.5, fresnel);
                float intencity = UNITY_ACCESS_INSTANCED_PROP(Props, _LightIntencity);


                half3 grabColor = intencity * 0.75;

                float light = max(0.001, dot(normalize(i.worldNormal), normalize(_SunPos.xyz)));

                light = pow(light, 50) * 10 * intencity;
#if !UNITY_COLORSPACE_GAMMA
                _Color.rgb = _Color.rgb * .65;
                fresnel = fresnel * fresnel;
#endif
                grabColor *= _Color.rgb;
                grabColor = lerp(grabColor * 0.15, grabColor, fresnel);
                grabColor = min(grabColor, _Color.rgb * 0.55);

                float3 color = grabColor.xyz + saturate(light);

#if defined(USE_FOG)
                float3 atmosColor;
                float3 atmosOpacity;
                PositionInputs posInput = GetPositionInput(i.pos, _ScreenSize.zw, i.pos.z, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
                EvaluateAtmosphericScattering(posInput, i.viewDir, atmosColor, atmosOpacity);

                color.rgb = color.rgb * (1 - atmosOpacity) + atmosColor;
#endif

                return float4(color, 1);

            }
            ENDHLSL
        }


    }

}
