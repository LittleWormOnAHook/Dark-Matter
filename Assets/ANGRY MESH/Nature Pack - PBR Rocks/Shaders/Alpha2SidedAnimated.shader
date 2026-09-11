// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "ANGRYMESH/PBR Rocks/Alpha2SidedAnimated (Legacy)"
{
	Properties
	{
		_SpecColor("Specular Color",Color)=(1,1,1,1)
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Color("Color", Color) = (1,1,1,0)
		_BackColor("Back Color", Color) = (1,1,1,0)
		_BottomColor("Bottom Color", Color) = (1,1,1,0)
		_BottomOffset("Bottom Offset", Range( 0 , 5)) = 5
		_Specular("Specular", Range( 0.01 , 1)) = 0.5
		_Gloss("Gloss", Range( 0.01 , 1)) = 0.5
		[NoScaleOffset]_AlbedoAOpacity("Albedo (A Opacity)", 2D) = "gray" {}
		[NoScaleOffset][Normal]_NormalMap("Normal Map", 2D) = "bump" {}
		_WindSpeed("Wind Speed", Float) = 10
		_WindAmplitude("Wind Amplitude", Float) = 0.5
		_WindCycles("Wind Cycles", Vector) = (4,1,1,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf BlinnPhong keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			half2 uv_texcoord;
			half ASEVFace : VFACE;
		};

		uniform half _WindSpeed;
		uniform half3 _WindCycles;
		uniform half _WindAmplitude;
		uniform sampler2D _NormalMap;
		uniform half4 _BottomColor;
		uniform half4 _Color;
		uniform sampler2D _AlbedoAOpacity;
		uniform half _BottomOffset;
		uniform half4 _BackColor;
		uniform half _Specular;
		uniform half _Gloss;
		uniform float _Cutoff = 0.5;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float3 MOTION53 = ( ( sin( ( ( ase_worldPos.z + ( _Time.x * _WindSpeed ) ) * _WindCycles ) ) * ( _WindAmplitude * 0.1 ) ) * v.color.a );
			v.vertex.xyz += MOTION53;
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_NormalMap23 = i.uv_texcoord;
			o.Normal = UnpackNormal( tex2D( _NormalMap, uv_NormalMap23 ) );
			float2 uv_AlbedoAOpacity1 = i.uv_texcoord;
			half4 tex2DNode1 = tex2D( _AlbedoAOpacity, uv_AlbedoAOpacity1 );
			float4 temp_output_33_0 = ( _Color * tex2DNode1 );
			float4 lerpResult8 = lerp( ( _BottomColor * temp_output_33_0 ) , temp_output_33_0 , saturate( ( i.uv_texcoord.y * _BottomOffset ) ));
			float4 switchResult15 = (((i.ASEVFace>0)?(lerpResult8):(( lerpResult8 * _BackColor ))));
			o.Albedo = switchResult15.rgb;
			o.Specular = _Specular;
			o.Gloss = _Gloss;
			o.Alpha = 1;
			half AlphaOpacity58 = tex2DNode1.a;
			clip( AlphaOpacity58 - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
}
/*ASEBEGIN
Version=16702
1927;29;1906;1004;3365.557;1681.871;3.522762;True;False
Node;AmplifyShaderEditor.RangedFloatNode;39;-1808,1152;Float;False;Property;_WindSpeed;Wind Speed;10;0;Create;True;0;0;False;0;10;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;38;-1840,960;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;-1584,1024;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;40;-1847,768;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;42;-1408,1024;Float;False;Property;_WindCycles;Wind Cycles;12;0;Create;True;0;0;False;0;4,1,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;43;-1408,816;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;32;-1760,-1184;Half;False;Property;_Color;Color;2;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;-1168,816;Float;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;45;-1136,1024;Float;False;Property;_WindAmplitude;Wind Amplitude;11;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;11;-1792,-560;Float;False;Property;_BottomOffset;Bottom Offset;5;0;Create;True;0;0;False;0;5;0;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;5;-1728,-688;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;1;-1808,-960;Float;True;Property;_AlbedoAOpacity;Albedo (A Opacity);8;1;[NoScaleOffset];Create;True;0;0;False;0;None;None;True;0;False;gray;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SinOpNode;47;-928,832;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;-912,1024;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;6;-1760,-1408;Half;False;Property;_BottomColor;Bottom Color;4;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-1408,-608;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;-1408,-1088;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;49;-704,1024;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;48;-720,816;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-1152,-1232;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;13;-1152,-704;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;50;-480,816;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;17;-976,-576;Half;False;Property;_BackColor;Back Color;3;0;Create;True;0;0;False;0;1,1,1,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;8;-896,-1024;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;58;-1400.497,-872.3102;Half;False;AlphaOpacity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;53;-128,832;Float;False;MOTION;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-640,-784;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;57;-448,320;Float;False;58;AlphaOpacity;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;56;-512,192;Half;False;Property;_Gloss;Gloss;7;0;Create;True;0;0;False;0;0.5;0;0.01;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;36;-512,96;Half;False;Property;_Specular;Specular;6;0;Create;True;0;0;False;0;0.5;0;0.01;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;23;-1536,0;Float;True;Property;_NormalMap;Normal Map;9;2;[NoScaleOffset];[Normal];Create;True;0;0;False;0;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;55;-416,416;Float;False;53;MOTION;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SwitchByFaceNode;15;-384,-1024;Float;False;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,-16;Half;False;True;2;Half;;0;0;BlinnPhong;ANGRYMESH/PBR Rocks/Alpha2SidedAnimated (Legacy);False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;1;-1;-1;-1;0;False;0;0;False;-1;0;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.CommentaryNode;54;-1841.611,640;Float;False;100;100;;0;// Motion (Used Vertex Alpha for animation intensity);1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;60;-1792,-1536;Float;False;100;100;;0;// Albedo;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;59;-1536,-128;Float;False;100;100;;0;// Normal Map;1,1,1,1;0;0
WireConnection;41;0;38;1
WireConnection;41;1;39;0
WireConnection;43;0;40;3
WireConnection;43;1;41;0
WireConnection;44;0;43;0
WireConnection;44;1;42;0
WireConnection;47;0;44;0
WireConnection;46;0;45;0
WireConnection;10;0;5;2
WireConnection;10;1;11;0
WireConnection;33;0;32;0
WireConnection;33;1;1;0
WireConnection;48;0;47;0
WireConnection;48;1;46;0
WireConnection;34;0;6;0
WireConnection;34;1;33;0
WireConnection;13;0;10;0
WireConnection;50;0;48;0
WireConnection;50;1;49;4
WireConnection;8;0;34;0
WireConnection;8;1;33;0
WireConnection;8;2;13;0
WireConnection;58;0;1;4
WireConnection;53;0;50;0
WireConnection;19;0;8;0
WireConnection;19;1;17;0
WireConnection;15;0;8;0
WireConnection;15;1;19;0
WireConnection;0;0;15;0
WireConnection;0;1;23;0
WireConnection;0;3;36;0
WireConnection;0;4;56;0
WireConnection;0;10;57;0
WireConnection;0;11;55;0
ASEEND*/
//CHKSM=CA8A09C320E9EB0F9646B386C551A0C0E1CFE1D1