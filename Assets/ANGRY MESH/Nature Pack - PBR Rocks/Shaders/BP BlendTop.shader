// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "ANGRYMESH/PBR Rocks/BP BlendTop"
{
	Properties
	{
		_BaseSpecular("Base Specular", Range( 0.01 , 1)) = 0.5
		_BaseColor("Base Color", Color) = (1,1,1,0)
		[NoScaleOffset]_BaseDiffuseAGloss("Base Diffuse (A Gloss)", 2D) = "gray" {}
		[NoScaleOffset][Normal]_BaseNormalMap("Base NormalMap", 2D) = "bump" {}
		_TopSpecular("Top Specular", Range( 0.01 , 1)) = 0.5
		_TopUVScale("Top UV Scale", Range( 1 , 30)) = 10
		_TopIntensity("Top Intensity", Range( 0 , 1)) = 1
		_TopOffset("Top Offset", Range( 0 , 1)) = 0.5
		_TopContrast("Top Contrast", Range( 0 , 2)) = 0.5
		_TopNormalIntensity("Top Normal Intensity", Range( 0 , 2)) = 1
		_TopColor("Top Color", Color) = (1,1,1,0)
		[NoScaleOffset]_TopDiffuseAGloss("Top Diffuse (A Gloss)", 2D) = "gray" {}
		[Normal][NoScaleOffset]_TopNormalMap("Top NormalMap", 2D) = "bump" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "UnityCG.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#include "Lighting.cginc"
		#pragma target 2.0
		#pragma multi_compile_instancing
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
			float3 worldNormal;
			INTERNAL_DATA
			float2 uv_texcoord;
			float3 worldPos;
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform half _BaseSpecular;
		uniform half _TopSpecular;
		uniform sampler2D _BaseNormalMap;
		uniform half _TopOffset;
		uniform half _TopContrast;
		uniform half _TopIntensity;
		uniform half _TopNormalIntensity;
		uniform sampler2D _TopNormalMap;
		uniform half _TopUVScale;
		uniform sampler2D _BaseDiffuseAGloss;
		uniform sampler2D _TopDiffuseAGloss;
		uniform half4 _BaseColor;
		uniform half4 _TopColor;

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			#ifdef UNITY_PASS_FORWARDBASE
			float ase_lightAtten = data.atten;
			if( _LightColor0.a == 0)
			ase_lightAtten = 0;
			#else
			float3 ase_lightAttenRGB = gi.light.color / ( ( _LightColor0.rgb ) + 0.000001 );
			float ase_lightAtten = max( max( ase_lightAttenRGB.r, ase_lightAttenRGB.g ), ase_lightAttenRGB.b );
			#endif
			#if defined(HANDLE_SHADOWS_BLENDING_IN_GI)
			half bakedAtten = UnitySampleBakedOcclusion(data.lightmapUV.xy, data.worldPos);
			float zDist = dot(_WorldSpaceCameraPos - data.worldPos, UNITY_MATRIX_V[2].xyz);
			float fadeDist = UnityComputeShadowFadeDistance(data.worldPos, zDist);
			ase_lightAtten = UnityMixRealtimeAndBakedShadows(data.atten, bakedAtten, UnityComputeShadowFade(fadeDist));
			#endif
			float2 uv_BaseNormalMap291 = i.uv_texcoord;
			float3 tex2DNode291 = UnpackNormal( tex2D( _BaseNormalMap, uv_BaseNormalMap291 ) );
			float3 BaseNormalMap166 = tex2DNode291;
			float TopMask168 = saturate( ( pow( ( saturate( (WorldNormalVector( i , BaseNormalMap166 )).y ) + _TopOffset ) , (1.0 + (_TopContrast - 0.0) * (20.0 - 1.0) / (1.0 - 0.0)) ) * _TopIntensity ) );
			float lerpResult299 = lerp( _BaseSpecular , _TopSpecular , TopMask168);
			half Specular301 = lerpResult299;
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld
			float3 ase_worldlightDir = 0;
			#else //aseld
			float3 ase_worldlightDir = normalize( UnityWorldSpaceLightDir( ase_worldPos ) );
			#endif //aseld
			float3 normalizeResult486 = normalize( ( ase_worldViewDir + ase_worldlightDir ) );
			float2 temp_output_195_0 = ( i.uv_texcoord * _TopUVScale );
			float2 TopUVScale197 = temp_output_195_0;
			float3 lerpResult158 = lerp( tex2DNode291 , UnpackScaleNormal( tex2D( _TopNormalMap, TopUVScale197 ), _TopNormalIntensity ) , TopMask168);
			float3 NormalMap387 = lerpResult158;
			float3 normalizeResult473 = normalize( (WorldNormalVector( i , NormalMap387 )) );
			float dotResult465 = dot( normalizeResult486 , normalizeResult473 );
			float2 uv_BaseDiffuseAGloss162 = i.uv_texcoord;
			float4 tex2DNode162 = tex2D( _BaseDiffuseAGloss, uv_BaseDiffuseAGloss162 );
			float DiffuseAlpha212 = ( 1.0 - tex2DNode162.a );
			float4 tex2DNode172 = tex2D( _TopDiffuseAGloss, temp_output_195_0 );
			float TopAlpha220 = ( 1.0 - tex2DNode172.a );
			float lerpResult217 = lerp( DiffuseAlpha212 , TopAlpha220 , TopMask168);
			half Gloss223 = lerpResult217;
			#if defined(LIGHTMAP_ON) && ( UNITY_VERSION < 560 || ( defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN) ) )//aselc
			float4 ase_lightColor = 0;
			#else //aselc
			float4 ase_lightColor = _LightColor0;
			#endif //aselc
			float3 temp_output_470_0 = ( ase_lightColor.rgb * ase_lightAtten );
			float3 LightingSpecular494 = ( Specular301 * pow( max( dotResult465 , 0.0 ) , ( ( 0.01 + Gloss223 ) * 128.0 ) ) * temp_output_470_0 );
			float dotResult458 = dot( normalizeResult473 , ase_worldlightDir );
			UnityGI gi481 = gi;
			float3 diffNorm481 = WorldNormalVector( i , normalizeResult473 );
			gi481 = UnityGI_Base( data, 1, diffNorm481 );
			float3 indirectDiffuse481 = gi481.indirect.diffuse + diffNorm481 * 0.0001;
			float4 lerpResult157 = lerp( ( _BaseColor * tex2DNode162 ) , ( _TopColor * tex2DNode172 ) , TopMask168);
			float4 Diffuse394 = lerpResult157;
			float4 LightingLambert497 = ( float4( ( ( temp_output_470_0 * max( dotResult458 , 0.0 ) ) + indirectDiffuse481 ) , 0.0 ) * Diffuse394 );
			c.rgb = ( float4( LightingSpecular494 , 0.0 ) + LightingLambert497 ).rgb;
			c.a = 1;
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
			o.Normal = float3(0,0,1);
		}

		ENDCG
		CGPROGRAM
		#pragma exclude_renderers d3d9 
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows noambient novertexlights 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
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
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputCustomLightingCustom o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputCustomLightingCustom, o )
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
1927;29;1906;1004;8653.995;3686.449;7.079588;True;False
Node;AmplifyShaderEditor.SamplerNode;291;-2192,384;Float;True;Property;_BaseNormalMap;Base NormalMap;3;2;[NoScaleOffset];[Normal];Create;True;0;0;False;0;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;166;-1680,384;Float;False;BaseNormalMap;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;167;-5248,384;Float;False;166;BaseNormalMap;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldNormalVector;93;-4896,384;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SaturateNode;390;-4560,384;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;107;-4736,704;Half;False;Property;_TopContrast;Top Contrast;8;0;Create;True;0;0;False;0;0.5;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;103;-4736,576;Half;False;Property;_TopOffset;Top Offset;7;0;Create;True;0;0;False;0;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;114;-4368,640;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;4;FLOAT;20;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;111;-4304,384;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;102;-4048,384;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;196;-2688,-832;Half;False;Property;_TopUVScale;Top UV Scale;5;0;Create;True;0;0;False;0;10;0;1;30;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;194;-2640,-1024;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;246;-4096,576;Half;False;Property;_TopIntensity;Top Intensity;6;0;Create;True;0;0;False;0;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;195;-2304,-944;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;393;-3792,384;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;197;-2048,-704;Float;False;TopUVScale;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SaturateNode;143;-3584,384;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;139;-2704,768;Half;False;Property;_TopNormalIntensity;Top Normal Intensity;9;0;Create;True;0;0;False;0;1;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;198;-2624,640;Float;False;197;TopUVScale;1;0;OBJECT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;168;-3424,384;Float;False;TopMask;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;171;-1664,784;Float;False;168;TopMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;119;-2192,640;Float;True;Property;_TopNormalMap;Top NormalMap;12;2;[Normal];[NoScaleOffset];Create;True;0;0;False;0;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;162;-2064,-1584;Float;True;Property;_BaseDiffuseAGloss;Base Diffuse (A Gloss);2;1;[NoScaleOffset];Create;True;0;0;False;0;None;None;True;0;False;gray;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;172;-2048,-1024;Float;True;Property;_TopDiffuseAGloss;Top Diffuse (A Gloss);11;1;[NoScaleOffset];Create;True;0;0;False;0;None;None;True;0;False;gray;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;158;-1296,528;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;401;-1680,-1472;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;325;-1664,-896;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;387;-992,528;Float;False;NormalMap;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;220;-1472,-896;Float;False;TopAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;212;-1488,-1472;Float;False;DiffuseAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;221;-5152,-896;Float;False;220;TopAlpha;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;489;-5168,2560;Float;False;387;NormalMap;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;214;-5168,-1024;Float;False;212;DiffuseAlpha;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;222;-5152,-768;Float;False;168;TopMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldNormalVector;475;-4896,2560;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;484;-5248,1792;Float;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.LerpOp;217;-4864,-912;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;483;-5248,1952;Float;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;485;-4864,1920;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;461;-4944,2752;Float;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NormalizeNode;473;-4608,2560;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;165;-1968,-1840;Half;False;Property;_BaseColor;Base Color;1;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;175;-1968,-1280;Half;False;Property;_TopColor;Top Color;10;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;223;-4608,-912;Half;False;Gloss;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;487;-4608,1808;Float;False;223;Gloss;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;460;-4608,1728;Float;False;Constant;_Add001;Add 0.01;3;0;Create;True;0;0;False;0;0.01;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;458;-4320,2560;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;486;-4608,1920;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LightAttenuation;469;-3840,2304;Float;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.LightColorNode;466;-3840,2176;Float;False;0;3;COLOR;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;170;-1056,-896;Float;False;168;TopMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;173;-1392,-1104;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;290;-5248,-1920;Half;False;Property;_BaseSpecular;Base Specular;0;0;Create;True;0;0;False;0;0.5;0;0.01;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;300;-5152,-1664;Float;False;168;TopMask;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;292;-5248,-1792;Half;False;Property;_TopSpecular;Top Specular;4;0;Create;True;0;0;False;0;0.5;0;0.01;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;163;-1008,-1616;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;470;-3584,2224;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;474;-4320,1792;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DotProductOpNode;465;-4320,1920;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;471;-3584,2560;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;157;-768,-1120;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;299;-4864,-1840;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IndirectDiffuseLighting;481;-4352,2752;Float;False;Tangent;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;457;-4064,1920;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;482;-3312,2224;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;394;-496,-1120;Float;False;Diffuse;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;301;-4496,-1840;Half;False;Specular;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;468;-4080,1792;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;128;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;488;-3744,1776;Float;False;301;Specular;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;463;-3072,2736;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;491;-2976,2944;Float;False;394;Diffuse;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;462;-3712,1872;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;459;-2656,2736;Float;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;479;-3200,1792;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;494;-2432,1792;Float;False;LightingSpecular;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;497;-2432,2736;Float;False;LightingLambert;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;499;303,128;Float;False;494;LightingSpecular;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;500;303,256;Float;False;497;LightingLambert;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;498;671,128;Float;False;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;943,64;Float;False;True;0;Float;;0;0;CustomLighting;ANGRYMESH/PBR Rocks/BP BlendTop;False;False;False;False;True;True;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;-0.09;True;True;0;False;Opaque;;Geometry;All;False;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;8.6;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.CommentaryNode;237;-2560,-2176;Float;False;100;100;;0;// Blend Diffuse ;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;503;-5248,1584;Float;False;100;100;;0;// Lighting Specular;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;230;-5248,-1152;Float;False;100;100;;0;// Gloss;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;241;-5248.225,102.8944;Float;False;100;100;;0;// Top World Mapping;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;303;-5248,-2176;Float;False;100;100;;0;// Specular;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;240;-2688,64;Float;False;100;100;;0;// Blend Normal Maps;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;504;-5248,2432;Float;False;100;100;;0;// Lighting Lambert;1,1,1,1;0;0
WireConnection;166;0;291;0
WireConnection;93;0;167;0
WireConnection;390;0;93;2
WireConnection;114;0;107;0
WireConnection;111;0;390;0
WireConnection;111;1;103;0
WireConnection;102;0;111;0
WireConnection;102;1;114;0
WireConnection;195;0;194;0
WireConnection;195;1;196;0
WireConnection;393;0;102;0
WireConnection;393;1;246;0
WireConnection;197;0;195;0
WireConnection;143;0;393;0
WireConnection;168;0;143;0
WireConnection;119;1;198;0
WireConnection;119;5;139;0
WireConnection;172;1;195;0
WireConnection;158;0;291;0
WireConnection;158;1;119;0
WireConnection;158;2;171;0
WireConnection;401;0;162;4
WireConnection;325;0;172;4
WireConnection;387;0;158;0
WireConnection;220;0;325;0
WireConnection;212;0;401;0
WireConnection;475;0;489;0
WireConnection;217;0;214;0
WireConnection;217;1;221;0
WireConnection;217;2;222;0
WireConnection;485;0;484;0
WireConnection;485;1;483;0
WireConnection;473;0;475;0
WireConnection;223;0;217;0
WireConnection;458;0;473;0
WireConnection;458;1;461;0
WireConnection;486;0;485;0
WireConnection;173;0;175;0
WireConnection;173;1;172;0
WireConnection;163;0;165;0
WireConnection;163;1;162;0
WireConnection;470;0;466;1
WireConnection;470;1;469;0
WireConnection;474;0;460;0
WireConnection;474;1;487;0
WireConnection;465;0;486;0
WireConnection;465;1;473;0
WireConnection;471;0;458;0
WireConnection;157;0;163;0
WireConnection;157;1;173;0
WireConnection;157;2;170;0
WireConnection;299;0;290;0
WireConnection;299;1;292;0
WireConnection;299;2;300;0
WireConnection;481;0;473;0
WireConnection;457;0;465;0
WireConnection;482;0;470;0
WireConnection;482;1;471;0
WireConnection;394;0;157;0
WireConnection;301;0;299;0
WireConnection;468;0;474;0
WireConnection;463;0;482;0
WireConnection;463;1;481;0
WireConnection;462;0;457;0
WireConnection;462;1;468;0
WireConnection;459;0;463;0
WireConnection;459;1;491;0
WireConnection;479;0;488;0
WireConnection;479;1;462;0
WireConnection;479;2;470;0
WireConnection;494;0;479;0
WireConnection;497;0;459;0
WireConnection;498;0;499;0
WireConnection;498;1;500;0
WireConnection;0;13;498;0
ASEEND*/
//CHKSM=105175BF417B29F2B4F7B9A2DD0BAF72C85F1072