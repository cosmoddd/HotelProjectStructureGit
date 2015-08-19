Shader "Voxeland/Constructor" 
{
	Properties 
	{
		_Color ("Color (RGB)", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap("Normal Map", 2D) = "bump" {}

		_SpecGlossMap("Spec Map (RGB), Smooth Map (A)", 2D) = "white" {}
		_Specular("Spec Value (RGB), Smooth Val (A)", Color) = (0,0,0,0.5)

		_ParallaxMap("Parallax Map", 2D) = "gray" {}
		_Parallax("Parallax Height", Range(0, 1)) = 0.0
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf StandardSpecular fullforwardshadows
		#pragma target 3.0

		#pragma shader_feature _NORMALMAP
		#pragma shader_feature _SPECGLOSSMAP
		#pragma shader_feature _PARALLAXMAP

		
		fixed4 _Color;

		sampler2D _MainTex;
		
		#if _NORMALMAP
		sampler2D _BumpMap;
		#endif

		#if _SPECGLOSSMAP
		sampler2D _SpecGlossMap;
		#endif

		#if _PARALLAXMAP
		sampler2D _ParallaxMap;
		float _Parallax;
		#endif

		fixed4 _Specular;

		/*
		struct SurfaceOutputStandard
		{
		fixed3 Albedo;      // base (diffuse or specular) color
		fixed3 Normal;      // tangent space normal, if written
		half3 Emission;
		half Metallic;      // 0=non-metal, 1=metal
		half Smoothness;    // 0=rough, 1=smooth
		half Occlusion;     // occlusion (default 1)
		fixed Alpha;        // alpha for transparencies
		};
		*/

		/*
		struct SurfaceOutputStandardSpecular
		{
		fixed3 Albedo;      // diffuse color
		fixed3 Specular;    // specular color
		fixed3 Normal;      // tangent space normal, if written
		half3 Emission;
		half Smoothness;    // 0=rough, 1=smooth
		half Occlusion;     // occlusion (default 1)
		fixed Alpha;        // alpha for transparencies
		};
		*/

		/*struct appdata {
		float4 vertex : POSITION;
		float4 tangent : TANGENT;
		float3 normal : NORMAL;
		float4 texcoord : TEXCOORD0;
		float4 texcoord1 : TEXCOORD1;
		fixed4 color : COLOR;
		};*/


		struct Input {
			float2 uv_MainTex; //uv
			float4 color : COLOR; //ambient
			float3 viewDir;
		};

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
		{
			//parallax
			float2 texcoords = IN.uv_MainTex;
			#if _PARALLAXMAP
			half h = tex2D(_ParallaxMap, IN.uv_MainTex).g;
			texcoords += ParallaxOffset1Step(h, _Parallax, IN.viewDir);
			#endif

			//diffuse and alpha
			fixed4 color = tex2D(_MainTex, texcoords);
			o.Albedo = color.rgb * _Color;
			o.Alpha = color.a;

			//specular, metallic and gloss
			#if _SPECGLOSSMAP
			_Specular *= 2;
			_Specular *= tex2D(_SpecGlossMap, texcoords);
			#endif

			o.Specular = _Specular.rgb;
			//o.Metallic = _Specular.r;
			o.Smoothness = _Specular.a;

			//normal
			#if _NORMALMAP
			o.Normal = UnpackNormal(tex2D(_BumpMap, texcoords));
			#endif

			o.Occlusion = IN.color.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
	CustomEditor "VoxelandMaterialInspector"
}
