// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "ANGRYMESH/PBR Rocks/PBR BlendTopDetail (Legacy)"
{
	Properties
	{
		_BaseSmoothness("Base Smoothness", Range( 0 , 1)) = 0.5
		_BaseAOIntensity("Base AO Intensity", Range( 0 , 1)) = 0.5
		_BaseColor("Base Color", Color) = (1,1,1,0)
		[NoScaleOffset]_BaseAlbedoASmoothness("Base Albedo (A Smoothness)", 2D) = "gray" {}
		[Normal][NoScaleOffset]_BaseNormalMap("Base NormalMap", 2D) = "bump" {}
		[NoScaleOffset]_BaseAOANoiseMask("Base AO (A NoiseMask)", 2D) = "white" {}
		[Toggle(_TOPNOISE_ON)] _TopNoise("Top Noise", Float) = 0
		_TopNoiseUVScale("Top Noise UV Scale", Range( 0.2 , 10)) = 1
		_TopSmoothness("Top Smoothness", Range( 0 , 1)) = 1
		_TopUVScale("Top UV Scale", Range( 1 , 30)) = 10
		_TopIntensity("Top Intensity", Range( 0 , 1)) = 0
		_TopOffset("Top Offset", Range( 0 , 1)) = 0.5
		_TopContrast("Top Contrast", Range( 0 , 2)) = 1
		_TopNormalIntensity("Top Normal Intensity", Range( 0 , 2)) = 1
		_TopColor("Top Color", Color) = (1,1,1,0)
		[NoScaleOffset]_TopAlbedoASmoothness("Top Albedo (A Smoothness)", 2D) = "gray" {}
		[Normal][NoScaleOffset]_TopNormalMap("Top NormalMap", 2D) = "bump" {}
		_DetailUVScale("Detail UV Scale", Range( 0 , 40)) = 10
		_DetailAlbedoIntensity("Detail Albedo Intensity", Range( 0 , 1)) = 1
		_DetailNormalMapIntensity("Detail NormalMap Intensity", Range( 0 , 2)) = 1
		[NoScaleOffset]_DetailAlbedo("Detail Albedo", 2D) = "gray" {}
		[Normal][NoScaleOffset]_DetailNormalMap("Detail NormalMap", 2D) = "bump" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGINCLUDE
		#include "UnityStandardUtils.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma multi_compile __ _TOPNOISE_ON
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float3 worldNormal;
			INTERNAL_DATA
		};

		uniform half _DetailNormalMapIntensity;
		uniform sampler2D _DetailNormalMap;
		uniform half _DetailUVScale;
		uniform sampler2D _BaseNormalMap;
		uniform half _TopNormalIntensity;
		uniform sampler2D _TopNormalMap;
		uniform half _TopUVScale;
		uniform half _TopOffset;
		uniform half _TopContrast;
		uniform half _TopIntensity;
		uniform sampler2D _BaseAOANoiseMask;
		uniform half _TopNoiseUVScale;
		uniform half4 _BaseColor;
		uniform sampler2D _BaseAlbedoASmoothness;
		uniform sampler2D _DetailAlbedo;
		uniform half _DetailAlbedoIntensity;
		uniform half4 _TopColor;
		uniform sampler2D _TopAlbedoASmoothness;
		uniform half _BaseSmoothness;
		uniform half _TopSmoothness;
		uniform half _BaseAOIntensity;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 temp_output_182_0 = ( i.uv_texcoord * _DetailUVScale );
			float2 DetailUVScale191 = temp_output_182_0;
			float2 uv_BaseNormalMap96 = i.uv_texcoord;
			float3 tex2DNode96 = UnpackNormal( tex2D( _BaseNormalMap, uv_BaseNormalMap96 ) );
			float2 uv_BaseNormalMap288 = i.uv_texcoord;
			float2 temp_output_195_0 = ( i.uv_texcoord * _TopUVScale );
			float2 TopUVScale197 = temp_output_195_0;
			float3 NormalMap166 = tex2DNode96;
			float TopMask168 = saturate( ( pow( ( saturate( (WorldNormalVector( i , NormalMap166 )).y ) + _TopOffset ) , (1.0 + (_TopContrast - 0.0) * (20.0 - 1.0) / (1.0 - 0.0)) ) * _TopIntensity ) );
			float4 temp_cast_0 = (( 1.0 - tex2D( _BaseAOANoiseMask, ( i.uv_texcoord * _TopNoiseUVScale ) ).a )).xxxx;
			#ifdef _TOPNOISE_ON
				float4 staticSwitch270 = temp_cast_0;
			#else
				float4 staticSwitch270 = half4(1,1,1,1);
			#endif
			float4 TopNoise225 = staticSwitch270;
			float3 lerpResult158 = lerp( BlendNormals( UnpackScaleNormal( tex2D( _DetailNormalMap, DetailUVScale191 ), _DetailNormalMapIntensity ) , tex2DNode96 ) , BlendNormals( UnpackScaleNormal( tex2D( _BaseNormalMap, uv_BaseNormalMap288 ), 0.5 ) , UnpackScaleNormal( tex2D( _TopNormalMap, TopUVScale197 ), _TopNormalIntensity ) ) , ( TopMask168 * TopNoise225 ).rgb);
			float3 normalizeResult136 = normalize( lerpResult158 );
			o.Normal = normalizeResult136;
			float2 uv_BaseAlbedoASmoothness162 = i.uv_texcoord;
			float4 tex2DNode162 = tex2D( _BaseAlbedoASmoothness, uv_BaseAlbedoASmoothness162 );
			float4 temp_output_163_0 = ( _BaseColor * tex2DNode162 );
			float4 blendOpSrc178 = ( 2.0 * tex2D( _DetailAlbedo, temp_output_182_0 ) );
			float4 blendOpDest178 = temp_output_163_0;
			float4 lerpResult187 = lerp( temp_output_163_0 , ( saturate( (( blendOpDest178 > 0.5 ) ? ( 1.0 - ( 1.0 - 2.0 * ( blendOpDest178 - 0.5 ) ) * ( 1.0 - blendOpSrc178 ) ) : ( 2.0 * blendOpDest178 * blendOpSrc178 ) ) )) , _DetailAlbedoIntensity);
			float4 tex2DNode172 = tex2D( _TopAlbedoASmoothness, temp_output_195_0 );
			float4 lerpResult157 = lerp( lerpResult187 , ( _TopColor * tex2DNode172 ) , ( TopMask168 * TopNoise225 ));
			o.Albedo = lerpResult157.rgb;
			float AlbedoAlphaSmoothness212 = tex2DNode162.a;
			float TopSmoothness220 = tex2DNode172.a;
			float lerpResult217 = lerp( ( AlbedoAlphaSmoothness212 + (-1.0 + (_BaseSmoothness - 0.0) * (1.0 - -1.0) / (1.0 - 0.0)) ) , ( TopSmoothness220 + (-1.0 + (_TopSmoothness - 0.0) * (1.0 - -1.0) / (1.0 - 0.0)) ) , TopMask168);
			float Smoothness223 = lerpResult217;
			o.Smoothness = Smoothness223;
			float4 temp_cast_3 = (1.0).xxxx;
			float2 uv_BaseAOANoiseMask200 = i.uv_texcoord;
			float4 lerpResult201 = lerp( temp_cast_3 , tex2D( _BaseAOANoiseMask, uv_BaseAOANoiseMask200 ) , _BaseAOIntensity);
			float4 AO207 = lerpResult201;
			float4 temp_cast_4 = (1.0).xxxx;
			float4 lerpResult303 = lerp( AO207 , temp_cast_4 , 0.8);
			float4 lerpResult290 = lerp( AO207 , lerpResult303 , TopMask168);
			o.Occlusion = lerpResult290.r;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}
/*ASEBEGIN
Version=16702
1927;29;1906;1004;2494.436;2352.271;1;True;False
Node;AmplifyShaderEditor.TexturePropertyNode;287;-2624,592;Float;True;Property;_BaseNormalMap;Base NormalMap;4;2;[Normal];[NoScaleOffset];Create;True;0;0;False;0;None;None;True;bump;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.SamplerNode;96;-2176,512;Float;True;Property;_Test;Test;4;0;Create;True;0;0;False;0;None;None;True;0;True;bump;Auto;True;Instance;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;166;-1792,576;Float;False;NormalMap;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;167;-5184,384;Float;False;166;NormalMap;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldNormalVector;93;-4896,384;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;281;-5248,-1600;Half;False;Property;_TopNoiseUVScale;Top Noise UV Scale;7;0;Create;True;0;0;False;0;1;0;0.2;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;279;-5200,-1792;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;107;-4608,704;Half;False;Property;_TopContrast;Top Contrast;12;0;Create;True;0;0;False;0;1;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;103;-4608,576;Half;False;Property;_TopOffset;Top Offset;11;0;Create;True;0;0;False;0;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;314;-4608,384;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;280;-4864,-1728;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;196;-2688,-704;Half;False;Property;_TopUVScale;Top UV Scale;9;0;Create;True;0;0;False;0;10;0;1;30;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;194;-2640,-944;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;114;-4224,640;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;20;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;111;-4256,384;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode;277;-5200,-2048;Float;True;Property;_BaseAOANoiseMask;Base AO (A NoiseMask);5;1;[NoScaleOffset];Create;True;0;0;False;0;None;None;False;white;Auto;Texture2D;0;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RangedFloatNode;246;-4000,640;Half;False;Property;_TopIntensity;Top Intensity;10;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;195;-2304,-816;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;278;-4624,-1824;Float;True;Property;_TextureSample1;Texture Sample 1;5;1;[NoScaleOffset];Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PowerNode;102;-4000,384;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;183;-2640,-2144;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;181;-2688,-1936;Half;False;Property;_DetailUVScale;Detail UV Scale;17;0;Create;True;0;0;False;0;10;0;0;40;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;313;-3712,384;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;172;-2048,-832;Float;True;Property;_TopAlbedoASmoothness;Top Albedo (A Smoothness);15;1;[NoScaleOffset];Create;True;0;0;False;0;None;None;True;0;False;gray;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;162;-2064,-1536;Float;True;Property;_BaseAlbedoASmoothness;Base Albedo (A Smoothness);3;1;[NoScaleOffset];Create;True;0;0;False;0;None;None;True;0;False;gray;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;182;-2288,-2048;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;271;-4224,-1728;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;269;-4528,-1584;Half;False;Constant;_Color0;Color 0;20;0;Create;True;0;0;False;0;1,1,1,1;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;218;-5248,-512;Half;False;Property;_TopSmoothness;Top Smoothness;8;0;Create;True;0;0;False;0;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;231;-4481,-2289;Half;False;Constant;_White1;White1;19;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;197;-2048,-576;Float;False;TopUVScale;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;212;-1664,-1472;Float;False;AlbedoAlphaSmoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;220;-1664,-640;Float;False;TopSmoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;216;-5248,-768;Half;False;Property;_BaseSmoothness;Base Smoothness;0;0;Create;True;0;0;False;0;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;143;-3520,384;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;200;-4624,-2176;Float;True;Property;_Texture;Texture;5;1;[NoScaleOffset];Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Instance;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;191;-2048,-1888;Float;False;DetailUVScale;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch;270;-3968,-1616;Float;False;Property;_TopNoise;Top Noise;6;0;Create;True;0;0;False;0;1;0;0;True;;Toggle;2;Key0;Key1;Create;False;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;203;-4608,-1952;Half;False;Property;_BaseAOIntensity;Base AO Intensity;1;0;Create;True;0;0;False;0;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;221;-5200,-640;Float;False;220;TopSmoothness;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;310;-4864,-768;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-1;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;225;-3712,-1600;Float;False;TopNoise;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;201;-4096,-2176;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.TFHCRemapNode;309;-4864,-512;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-1;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;214;-5256.788,-896;Float;False;212;AlbedoAlphaSmoothness;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;168;-3328,384;Float;False;TopMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;289;-2576,832;Half;False;Constant;_TopInt;Top Int;22;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;193;-2624,256;Float;False;191;DetailUVScale;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;165;-1968,-1792;Half;False;Property;_BaseColor;Base Color;2;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;316;-1936,-2240;Float;False;Constant;_Multiply_X2;Multiply_X2;22;0;Create;True;0;0;False;0;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;139;-2688,1152;Half;False;Property;_TopNormalIntensity;Top Normal Intensity;13;0;Create;True;0;0;False;0;1;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;176;-2064,-2096;Float;True;Property;_DetailAlbedo;Detail Albedo;20;1;[NoScaleOffset];Create;True;0;0;False;0;None;None;True;0;False;gray;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;198;-2608,1024;Float;False;197;TopUVScale;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;189;-2688,384;Half;False;Property;_DetailNormalMapIntensity;Detail NormalMap Intensity;19;0;Create;True;0;0;False;0;1;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;308;-4608,-640;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;222;-4608,-448;Float;False;168;TopMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;311;-4608,-896;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;207;-3712,-2176;Float;False;AO;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;265;-1824,1216;Float;False;225;TopNoise;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;163;-1664,-1664;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;171;-1824,1088;Float;False;168;TopMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;288;-2176,768;Float;True;Property;_TextureSample2;Texture Sample 2;4;0;Create;True;0;0;False;0;None;None;True;0;True;bump;Auto;True;Instance;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;315;-1536,-2048;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;188;-2176,256;Float;True;Property;_DetailNormalMap;Detail NormalMap;21;2;[Normal];[NoScaleOffset];Create;True;0;0;False;0;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;119;-2176,1024;Float;True;Property;_TopNormalMap;Top NormalMap;16;2;[Normal];[NoScaleOffset];Create;True;0;0;False;0;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;217;-4352,-704;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;175;-2048,-1040;Half;False;Property;_TopColor;Top Color;14;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BlendOpsNode;178;-1360,-1760;Float;False;Overlay;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;243;-1568,-384;Float;False;225;TopNoise;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;170;-1568,-512;Float;False;168;TopMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;180;-1296,-1472;Half;False;Property;_DetailAlbedoIntensity;Detail Albedo Intensity;18;0;Create;True;0;0;False;0;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;304;-2688,2304;Half;False;Constant;_AOTopIntensity;AO Top Intensity;22;0;Create;True;0;0;False;0;0.8;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;252;-1584,768;Float;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;306;-2688,2176;Half;False;Constant;_White;White;22;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;258;-1520,1072;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.BlendNormalsNode;190;-1584,384;Float;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;208;-2528,1968;Float;False;207;AO;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;303;-2176,2064;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;223;-4096,-704;Float;False;Smoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;253;-1280,-640;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;187;-1024,-1664;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;173;-1280,-880;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;158;-1280,768;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;291;-2176,2256;Float;False;168;TopMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;157;-768,-896;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;290;-1920,1968;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.NormalizeNode;136;-1024,768;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;224;-304,896;Float;False;223;Smoothness;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;112,752;Float;False;True;2;Float;;0;0;Standard;ANGRYMESH/PBR Rocks/PBR BlendTopDetail (Legacy);False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;-0.09;True;True;0;False;Opaque;;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;8.6;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.CommentaryNode;307;-2688,1696;Float;False;100;100;;0;// Decreased AO intensity for the Top Texture;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;230;-5248,-1120;Float;False;100;100;;0;// Smoothness;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;237;-2688,-2304;Float;False;100;100;;0;// Blend Albedo Detail;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;240;-2688,64;Float;False;100;100;;0;// Blend Normal Maps;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;239;-2688,-1120;Float;False;100;100;;0;// Top Albedo;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;210;-5248,-2304;Float;False;100;100;;0;// Ambient Occlussion;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;241;-5248.225,102.8944;Float;False;100;100;;0;// Top World Mapping;1,1,1,1;0;0
WireConnection;96;0;287;0
WireConnection;166;0;96;0
WireConnection;93;0;167;0
WireConnection;314;0;93;2
WireConnection;280;0;279;0
WireConnection;280;1;281;0
WireConnection;114;0;107;0
WireConnection;111;0;314;0
WireConnection;111;1;103;0
WireConnection;195;0;194;0
WireConnection;195;1;196;0
WireConnection;278;0;277;0
WireConnection;278;1;280;0
WireConnection;102;0;111;0
WireConnection;102;1;114;0
WireConnection;313;0;102;0
WireConnection;313;1;246;0
WireConnection;172;1;195;0
WireConnection;182;0;183;0
WireConnection;182;1;181;0
WireConnection;271;0;278;4
WireConnection;197;0;195;0
WireConnection;212;0;162;4
WireConnection;220;0;172;4
WireConnection;143;0;313;0
WireConnection;200;0;277;0
WireConnection;191;0;182;0
WireConnection;270;1;269;0
WireConnection;270;0;271;0
WireConnection;310;0;216;0
WireConnection;225;0;270;0
WireConnection;201;0;231;0
WireConnection;201;1;200;0
WireConnection;201;2;203;0
WireConnection;309;0;218;0
WireConnection;168;0;143;0
WireConnection;176;1;182;0
WireConnection;308;0;221;0
WireConnection;308;1;309;0
WireConnection;311;0;214;0
WireConnection;311;1;310;0
WireConnection;207;0;201;0
WireConnection;163;0;165;0
WireConnection;163;1;162;0
WireConnection;288;0;287;0
WireConnection;288;5;289;0
WireConnection;315;0;316;0
WireConnection;315;1;176;0
WireConnection;188;1;193;0
WireConnection;188;5;189;0
WireConnection;119;1;198;0
WireConnection;119;5;139;0
WireConnection;217;0;311;0
WireConnection;217;1;308;0
WireConnection;217;2;222;0
WireConnection;178;0;315;0
WireConnection;178;1;163;0
WireConnection;252;0;288;0
WireConnection;252;1;119;0
WireConnection;258;0;171;0
WireConnection;258;1;265;0
WireConnection;190;0;188;0
WireConnection;190;1;96;0
WireConnection;303;0;208;0
WireConnection;303;1;306;0
WireConnection;303;2;304;0
WireConnection;223;0;217;0
WireConnection;253;0;170;0
WireConnection;253;1;243;0
WireConnection;187;0;163;0
WireConnection;187;1;178;0
WireConnection;187;2;180;0
WireConnection;173;0;175;0
WireConnection;173;1;172;0
WireConnection;158;0;190;0
WireConnection;158;1;252;0
WireConnection;158;2;258;0
WireConnection;157;0;187;0
WireConnection;157;1;173;0
WireConnection;157;2;253;0
WireConnection;290;0;208;0
WireConnection;290;1;303;0
WireConnection;290;2;291;0
WireConnection;136;0;158;0
WireConnection;0;0;157;0
WireConnection;0;1;136;0
WireConnection;0;4;224;0
WireConnection;0;5;290;0
ASEEND*/
//CHKSM=3EE9E30451E243234713D7EDE14CD7CEE9228C85