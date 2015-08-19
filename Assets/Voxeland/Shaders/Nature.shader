Shader "Voxeland/Nature" 
{
	Properties 
	{
		[Enum(Off, 0, Front, 1, Back, 2)] _Culling("Culling", Int) = 1
		_Cutoff("Alpha Ref", Range(0, 1)) = 0.33
		_Color ("Color (RGB)", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap("Normal Map", 2D) = "bump" {}

		_SpecGlossMap("Spec Map (RGB), Smooth Map (A)", 2D) = "white" {}
		_Specular("Spec Value (RGB), Smooth Val (A)", Color) = (0,0,0,0.5)
		_AmbientPower("Ambient Power", Range(0.01, 2)) = 0.25

		_WindTex("Wind(XY)", 2D) = "bump" {}
		_WindSize("Wind Size", Range(0, 300)) = 50
		_WindSpeedX("Wind Speed X", Float) = 0.5
		_WindSpeedZ("Wind Speed Z", Float) = 2
		_WindStrength("Wind Strength", Range(0, 2)) = 0.33
	}

	SubShader 
	{
		Cull[_Culling]
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf StandardSpecular fullforwardshadows alphatest:_Cutoff vertex:vert 
		#pragma target 3.0

		#pragma shader_feature _NORMALMAP
		#pragma shader_feature _SPECGLOSSMAP
		#pragma shader_feature _WIND
		
		fixed4 _Color;

		sampler2D _MainTex;
		
		#if _NORMALMAP
		sampler2D _BumpMap;
		#endif

		#if _SPECGLOSSMAP
		sampler2D _SpecGlossMap;
		#endif

		fixed4 _Specular;
		half _AmbientPower;

		sampler2D _WindTex;
		float _WindSize;
		float _WindSpeedX;
		float _WindSpeedZ;
		float _WindStrength;


		struct Input {
			float2 uv_MainTex; //uv
			float4 color : COLOR; //ambient
		};


		void vert(inout appdata_full v, out Input o)
		{
			#if _WIND
				float3 worldPos = mul(_Object2World, v.vertex).xyz;
				worldPos.x += _WindSpeedX * _Time.y;
				worldPos.z += _WindSpeedZ * _Time.y;

				half4 windColor = tex2Dlod(_WindTex, float4(worldPos.x / _WindSize, worldPos.z / _WindSize, 0, 0));
				float3 windDir = UnpackNormal(windColor);
				float2 vertPosMod = windDir.xy * _WindStrength * v.texcoord1.y;
				v.vertex.xz += vertPosMod;
				v.vertex.y -= (abs(vertPosMod.x) + abs(vertPosMod.y)) / 4;
			#endif

			o.color = v.color;
			o.uv_MainTex = v.texcoord;
		}


		void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
		{
			float2 texcoords = IN.uv_MainTex;

			//diffuse and alpha
			fixed4 color = tex2D(_MainTex, texcoords);
			o.Albedo = color.rgb * _Color * IN.color.rgb;
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

			o.Occlusion = IN.color.a * _AmbientPower;
		}
		ENDCG
	} 
	FallBack "Transparent/Cutout/Diffuse"
	CustomEditor "VoxelandMaterialInspector"
}
