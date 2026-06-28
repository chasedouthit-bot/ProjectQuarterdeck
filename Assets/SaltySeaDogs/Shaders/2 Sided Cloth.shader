// Upgrade NOTE: upgraded instancing buffer 'SaltySeaDogs2SidedCloth' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Salty Sea Dogs/2 Sided Cloth "
{
	Properties
	{
		[Header(Translucency)]
		_Translucency("Strength", Range( 0 , 50)) = 1
		_TransNormalDistortion("Normal Distortion", Range( 0 , 1)) = 0.1
		_TransScattering("Scaterring Falloff", Range( 1 , 50)) = 2
		_TransDirect("Direct", Range( 0 , 1)) = 1
		_TransAmbient("Ambient", Range( 0 , 1)) = 0.2
		_TransShadow("Shadow", Range( 0 , 1)) = 0.9
		_ClothTint("Cloth Tint", Color) = (1,1,1,1)
		_Cutoff( "Mask Clip Value", Float ) = -0.37
		_AmbientOcclusion("AmbientOcclusion", 2D) = "white" {}
		_FrontFacesAlbedo("Front Faces Albedo", 2D) = "white" {}
		_FrontLogo("Front Logo", 2D) = "white" {}
		_LogoTranslucency("Logo Translucency", Range( 0 , 1)) = 0.07547475
		_FrontFacesNormal("Front Faces Normal", 2D) = "bump" {}
		_DetailNormal("Detail Normal", 2D) = "bump" {}
		_TranslucencyMask("TranslucencyMask", 2D) = "white" {}
		_NormalIntensity("NormalIntensity", Range( -1 , 1)) = 0
		_OpacityMask("Opacity Mask", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" }
		Cull Off
		Stencil
		{
			Ref 1
			CompFront Always
			PassFront Replace
		}
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#include "UnityPBSLighting.cginc"
		#pragma target 4.6
		#pragma multi_compile_instancing
		#pragma surface surf StandardCustom keepalpha addshadow fullforwardshadows exclude_path:deferred 
		struct Input
		{
			fixed ASEVFace : VFACE;
			half2 uv_texcoord;
		};

		struct SurfaceOutputStandardCustom
		{
			fixed3 Albedo;
			fixed3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			fixed Alpha;
			fixed3 Translucency;
		};

		uniform sampler2D _FrontFacesNormal;
		uniform float4 _FrontFacesNormal_ST;
		uniform sampler2D _DetailNormal;
		uniform float4 _DetailNormal_ST;
		uniform half4 _ClothTint;
		uniform sampler2D _FrontFacesAlbedo;
		uniform float4 _FrontFacesAlbedo_ST;
		uniform sampler2D _FrontLogo;
		uniform float4 _FrontLogo_ST;
		uniform sampler2D _AmbientOcclusion;
		uniform float4 _AmbientOcclusion_ST;
		uniform half _Translucency;
		uniform half _TransNormalDistortion;
		uniform half _TransScattering;
		uniform half _TransDirect;
		uniform half _TransAmbient;
		uniform half _TransShadow;
		uniform sampler2D _TranslucencyMask;
		uniform float4 _TranslucencyMask_ST;
		uniform half _LogoTranslucency;
		uniform sampler2D _OpacityMask;
		uniform float4 _OpacityMask_ST;
		uniform float _Cutoff = -0.37;

		UNITY_INSTANCING_BUFFER_START(SaltySeaDogs2SidedCloth)
			UNITY_DEFINE_INSTANCED_PROP(half, _NormalIntensity)
#define _NormalIntensity_arr SaltySeaDogs2SidedCloth
		UNITY_INSTANCING_BUFFER_END(SaltySeaDogs2SidedCloth)

		inline half4 LightingStandardCustom(SurfaceOutputStandardCustom s, half3 viewDir, UnityGI gi )
		{
			#if !DIRECTIONAL
			float3 lightAtten = gi.light.color;
			#else
			float3 lightAtten = lerp( _LightColor0.rgb, gi.light.color, _TransShadow );
			#endif
			half3 lightDir = gi.light.dir + s.Normal * _TransNormalDistortion;
			half transVdotL = pow( saturate( dot( viewDir, -lightDir ) ), _TransScattering );
			half3 translucency = lightAtten * (transVdotL * _TransDirect + gi.indirect.diffuse * _TransAmbient) * s.Translucency;
			half4 c = half4( s.Albedo * translucency * _Translucency, 0 );

			SurfaceOutputStandard r;
			r.Albedo = s.Albedo;
			r.Normal = s.Normal;
			r.Emission = s.Emission;
			r.Metallic = s.Metallic;
			r.Smoothness = s.Smoothness;
			r.Occlusion = s.Occlusion;
			r.Alpha = s.Alpha;
			return LightingStandard (r, viewDir, gi) + c;
		}

		inline void LightingStandardCustom_GI(SurfaceOutputStandardCustom s, UnityGIInput data, inout UnityGI gi )
		{
			UNITY_GI(gi, s, data);
		}

		void surf( Input i , inout SurfaceOutputStandardCustom o )
		{
			float switchResult72 = (((i.ASEVFace>0)?(1.0):(-1.0)));
			half _NormalIntensity_Instance = UNITY_ACCESS_INSTANCED_PROP(_NormalIntensity_arr, _NormalIntensity);
			float2 uv_FrontFacesNormal = i.uv_texcoord * _FrontFacesNormal_ST.xy + _FrontFacesNormal_ST.zw;
			float2 uv_DetailNormal = i.uv_texcoord * _DetailNormal_ST.xy + _DetailNormal_ST.zw;
			float3 FrontFacesNormal51 = ( ( switchResult72 * _NormalIntensity_Instance ) * BlendNormals( UnpackNormal( tex2D( _FrontFacesNormal, uv_FrontFacesNormal ) ) , UnpackNormal( tex2D( _DetailNormal, uv_DetailNormal ) ) ) );
			o.Normal = FrontFacesNormal51;
			float2 uv_FrontFacesAlbedo = i.uv_texcoord * _FrontFacesAlbedo_ST.xy + _FrontFacesAlbedo_ST.zw;
			float4 FrontFacesAlbedo44 = ( _ClothTint * tex2D( _FrontFacesAlbedo, uv_FrontFacesAlbedo ) );
			float2 uv_FrontLogo = i.uv_texcoord * _FrontLogo_ST.xy + _FrontLogo_ST.zw;
			half4 tex2DNode85 = tex2D( _FrontLogo, uv_FrontLogo );
			half4 temp_cast_0 = (tex2DNode85.a).xxxx;
			float4 switchResult119 = (((i.ASEVFace>0)?(( FrontFacesAlbedo44 * max( tex2DNode85 , ( half4(1,1,1,1) - temp_cast_0 ) ) )):(FrontFacesAlbedo44)));
			float4 Albedo93 = switchResult119;
			o.Albedo = Albedo93.rgb;
			float2 uv_AmbientOcclusion = i.uv_texcoord * _AmbientOcclusion_ST.xy + _AmbientOcclusion_ST.zw;
			o.Occlusion = tex2D( _AmbientOcclusion, uv_AmbientOcclusion ).r;
			float2 uv_TranslucencyMask = i.uv_texcoord * _TranslucencyMask_ST.xy + _TranslucencyMask_ST.zw;
			half4 TranslucencyMask69 = tex2D( _TranslucencyMask, uv_TranslucencyMask );
			half4 temp_cast_3 = (( tex2DNode85.a * _LogoTranslucency )).xxxx;
			float4 Transucency102 = ( TranslucencyMask69 - temp_cast_3 );
			o.Translucency = ( FrontFacesAlbedo44 * Transucency102 ).rgb;
			o.Alpha = 1;
			float2 uv_OpacityMask = i.uv_texcoord * _OpacityMask_ST.xy + _OpacityMask_ST.zw;
			float OpacityMask56 = tex2D( _OpacityMask, uv_OpacityMask ).a;
			clip( OpacityMask56 - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	//CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=13901
-1826;69;1769;942;3464.679;2122.505;3.145899;True;True
Node;AmplifyShaderEditor.CommentaryNode;90;-1825.096,-1951.705;Float;False;1768.425;953.8232;Comment;13;93;102;110;91;112;99;111;85;119;120;124;126;127;Sail Logo;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;52;-1835.402,-868.3826;Float;False;1781.06;824.5439;Comment;14;44;51;43;50;42;28;78;73;74;72;77;75;130;132;Faces;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;42;-1785.402,-622.5721;Float;True;Property;_FrontFacesAlbedo;Front Faces Albedo;9;0;Assets/SaltySeaDogs/sail_clean_new.png;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.CommentaryNode;82;-1834.553,5.387551;Float;False;618.5956;282.5742;Comment;2;69;71;Translucency;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;85;-1777.692,-1902.947;Float;True;Property;_FrontLogo;Front Logo;10;0;Assets/SaltySeaDogs/spanish_coatofarms.png;True;0;False;white;LockedToTexture2D;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.ColorNode;124;-1770.131,-1695.331;Float;False;Constant;_Color0;Color 0;10;0;1,1,1,1;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.ColorNode;28;-1766.894,-818.3826;Float;False;Property;_ClothTint;Cloth Tint;6;0;1,1,1,1;0;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;111;-1767.143,-1517.127;Float;False;Property;_LogoTranslucency;Logo Translucency;11;0;0.07547475;0;1;0;1;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;74;-1438.281,-423.9572;Float;False;Constant;_Float1;Float 1;6;0;-1;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;73;-1435.457,-504.0468;Float;False;Constant;_Float0;Float 0;6;0;1;0;0;0;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;43;-1376.277,-742.7142;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.SimpleSubtractOpNode;126;-1384.332,-1805.431;Float;False;2;0;COLOR;0,0,0,0;False;1;FLOAT;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.SamplerNode;71;-1809.412,56.05954;Float;True;Property;_TranslucencyMask;TranslucencyMask;14;0;Assets/SaltySeaDogs/sail_clean_new_translucency_2.png;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SimpleMaxOpNode;127;-1077.331,-1879.532;Float;False;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.GetLocalVarNode;99;-1761.383,-1295.104;Float;False;69;0;1;COLOR
Node;AmplifyShaderEditor.GetLocalVarNode;91;-1759.08,-1381.717;Float;False;44;0;1;COLOR
Node;AmplifyShaderEditor.SamplerNode;50;-1779.736,-263.7267;Float;True;Property;_FrontFacesNormal;Front Faces Normal;12;0;Assets/SaltySeaDogs/cloth_sails_normal.png;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;FLOAT3;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.SamplerNode;130;-1267.082,-234.77;Float;True;Property;_DetailNormal;Detail Normal;13;0;None;True;0;False;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;FLOAT3;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.RegisterLocalVarNode;69;-1457.261,54.60865;Half;False;TranslucencyMask;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.SwitchByFaceNode;72;-1167.791,-482.3163;Float;False;2;0;FLOAT;0.0;False;1;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.RangedFloatNode;78;-1486.507,-326.653;Half;False;InstancedProperty;_NormalIntensity;NormalIntensity;15;0;0;-1;1;0;1;FLOAT
Node;AmplifyShaderEditor.RegisterLocalVarNode;44;-1138.481,-747.7137;Float;False;FrontFacesAlbedo;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;112;-1258.409,-1420.638;Float;True;2;2;0;FLOAT;0,0,0,0;False;1;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.SimpleSubtractOpNode;110;-787.8523,-1481.551;Float;True;2;0;COLOR;0,0,0,0;False;1;FLOAT;0.0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;120;-928.171,-1899.294;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;77;-895.0546,-487.8436;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.CommentaryNode;84;-3.365044,-862.5934;Float;False;1006.537;811.2684;Comment;8;83;62;58;66;59;70;0;128;Output;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;57;-1148.919,11.3143;Float;False;626.0693;280;Comment;2;56;27;Opacity Mask;1,1,1,1;0;0
Node;AmplifyShaderEditor.BlendNormalsNode;132;-856.024,-360.0251;Float;True;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3
Node;AmplifyShaderEditor.SwitchByFaceNode;119;-603.6271,-1808.846;Float;False;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;75;-566.6791,-383.7284;Float;False;2;2;0;FLOAT;0.0;False;1;FLOAT3;0;False;1;FLOAT3
Node;AmplifyShaderEditor.RegisterLocalVarNode;102;-388.4279,-1488.221;Float;False;Transucency;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.GetLocalVarNode;59;27.67792,-456.14;Float;False;44;0;1;COLOR
Node;AmplifyShaderEditor.SamplerNode;27;-1104.23,61.31394;Float;True;Property;_OpacityMask;Opacity Mask;16;0;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.GetLocalVarNode;70;33.40396,-361.4094;Float;False;102;0;1;COLOR
Node;AmplifyShaderEditor.RegisterLocalVarNode;51;-336.2754,-388.1655;Float;False;FrontFacesNormal;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3
Node;AmplifyShaderEditor.RegisterLocalVarNode;56;-756.8498,149.5312;Float;False;OpacityMask;-1;True;1;0;FLOAT;0.0;False;1;FLOAT
Node;AmplifyShaderEditor.GetLocalVarNode;58;33.64743,-284.7289;Float;False;56;0;1;FLOAT
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;66;357.9245,-474.8246;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.SamplerNode;128;27.21305,-651.6207;Float;True;Property;_AmbientOcclusion;AmbientOcclusion;8;0;Assets/SaltySeaDogs/sail_clean_new_occlusion.png;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;1.0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1.0;False;5;COLOR;FLOAT;FLOAT;FLOAT;FLOAT
Node;AmplifyShaderEditor.GetLocalVarNode;83;20.47438,-809.9031;Float;False;93;0;1;COLOR
Node;AmplifyShaderEditor.RegisterLocalVarNode;93;-389.7302,-1766.854;Float;False;Albedo;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR
Node;AmplifyShaderEditor.GetLocalVarNode;62;22.00537,-731.6902;Float;False;51;0;1;FLOAT3
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;754.181,-766.1504;Half;False;True;6;Half;ASEMaterialInspector;0;0;Standard;Salty Sea Dogs/2 Sided Cloth ;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;1;0;False;0;0;Masked;-0.37;True;True;0;False;TransparentCutout;AlphaTest;ForwardOnly;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;1;255;255;7;3;0;0;0;0;0;0;False;0;4;10;25;True;0.5;True;0;Zero;Zero;4;One;One;Add;Max;0;False;0;1,0.4344827,0,0;VertexScale;False;Cylindrical;False;Relative;0;;7;0;-1;-1;0;0;0;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0.0;False;4;FLOAT;0.0;False;5;FLOAT;0.0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0.0;False;9;FLOAT;0.0;False;10;FLOAT;0.0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;43;0;28;0
WireConnection;43;1;42;0
WireConnection;126;0;124;0
WireConnection;126;1;85;4
WireConnection;127;0;85;0
WireConnection;127;1;126;0
WireConnection;69;0;71;0
WireConnection;72;0;73;0
WireConnection;72;1;74;0
WireConnection;44;0;43;0
WireConnection;112;0;85;4
WireConnection;112;1;111;0
WireConnection;110;0;99;0
WireConnection;110;1;112;0
WireConnection;120;0;91;0
WireConnection;120;1;127;0
WireConnection;77;0;72;0
WireConnection;77;1;78;0
WireConnection;132;0;50;0
WireConnection;132;1;130;0
WireConnection;119;0;120;0
WireConnection;119;1;91;0
WireConnection;75;0;77;0
WireConnection;75;1;132;0
WireConnection;102;0;110;0
WireConnection;51;0;75;0
WireConnection;56;0;27;4
WireConnection;66;0;59;0
WireConnection;66;1;70;0
WireConnection;93;0;119;0
WireConnection;0;0;83;0
WireConnection;0;1;62;0
WireConnection;0;5;128;0
WireConnection;0;7;66;0
WireConnection;0;10;58;0
ASEEND*/
//CHKSM=1035EA18A8CD53FC1AACE53645C78A64CB20182E