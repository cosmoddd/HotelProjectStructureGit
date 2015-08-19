Shader "Voxeland/Standard (Specular setup)" 
{
	Properties 
	{
		_Color ("Color (RGB)", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap("Normal Map", 2D) = "bump" {}
		_SpecGlossMap("Spec Map (RGB), Smooth Map (A)", 2D) = "white" {}
		_Specular("Spec Value (RGB), Smooth Val (A)", Color) = (0,0,0,0.5)
		_Tile("Tile", Float) = 0.25

		_Color2 ("Color (RGB)", Color) = (1,1,1,1)
		_MainTex2 ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap2("Normal Map", 2D) = "bump" {}
		_SpecGlossMap2("Spec Map (RGB), Smooth Map (A)", 2D) = "white" {}
		_Specular2("Spec Value (RGB), Smooth Val (A)", Color) = (0,0,0,0.5)
		_Tile2("Tile", Float) = 0.25

		_Color3 ("Color (RGB)", Color) = (1,1,1,1)
		_MainTex3 ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap3("Normal Map", 2D) = "bump" {}
		_SpecGlossMap3("Spec Map (RGB), Smooth Map (A)", 2D) = "white" {}
		_Specular3("Spec Value (RGB), Smooth Val (A)", Color) = (0,0,0,0.5)
		_Tile3("Tile", Float) = 0.25

		_Color4 ("Color (RGB)", Color) = (1,1,1,1)
		_MainTex4 ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap4("Normal Map", 2D) = "bump" {}
		_SpecGlossMap4("Spec Map (RGB), Smooth Map (A)", 2D) = "white" {}
		_Specular4("Spec Value (RGB), Smooth Val (A)", Color) = (0,0,0,0.5)
		_Tile4("Tile", Float) = 0.25
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf StandardSpecular fullforwardshadows vertex:vert
		#pragma target 3.0

		#pragma shader_feature _NORMALMAP
		#pragma shader_feature _SPECGLOSSMAP
		//#pragma shader_feature _PARALLAXMAP
		//#pragma multi_compile _1CH _2CH _3CH _4CH


		/*struct Channel {
			fixed4 _Color;
			sampler2D _MainTex;
			sampler2D _BumpMap;
			fixed4 _Specular;
			half _Tile; };*/
		//vs_4_0 does not allow textures or samplers to be members of compound types


		fixed4 _Color;				fixed4 _Color2;				fixed4 _Color3;				fixed4 _Color4;
		sampler2D _MainTex;			sampler2D _MainTex2;		sampler2D _MainTex3;		sampler2D _MainTex4;
		#if _NORMALMAP
		sampler2D _BumpMap;			sampler2D _BumpMap2;		sampler2D _BumpMap3;		sampler2D _BumpMap4;
		#endif
		#if _SPECGLOSSMAP
		sampler2D _SpecGlossMap;	sampler2D _SpecGlossMap2;	sampler2D _SpecGlossMap3;	sampler2D _SpecGlossMap4;
		#endif
		//#if _PARALLAXMAP
		//sampler2D _ParallaxMap;	sampler2D _ParallaxMap2;	sampler2D _ParallaxMap3;	sampler2D _ParallaxMap4;
		//float _Parallax;			float _Parallax2;			float _Parallax3;			float _Parallax4;
		//#endif
		fixed4 _Specular;			fixed4 _Specular2;			fixed4 _Specular3;			fixed4 _Specular4;
		half _Tile;					half _Tile2;				half _Tile3;				half _Tile4;


		struct Input {
			float2 uv_MainTex; //type
			float2 uv2_BumpMap; //type
			float4 color : COLOR; //ambient
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};


		void vert(inout appdata_full v)
		{
			//tangents
			float4 worldNormal = normalize(mul(_Object2World, float4(v.normal, 0)));
				float3 absWorldNormal = abs(worldNormal);

			if (absWorldNormal.z >= absWorldNormal.x && absWorldNormal.z >= absWorldNormal.y)
			{
				if (worldNormal.z>0) v.tangent = float4(-1, 0, 0, -1);
				else v.tangent = float4(1, 0, 0, -1);
			}

			else if (absWorldNormal.y >= absWorldNormal.x && absWorldNormal.y >= absWorldNormal.z)
			{
				if (worldNormal.y>0) v.tangent = float4(0, 0, -1, -1);
				else v.tangent = float4(0, 0, 1, -1);
			}

			else //if (absWorldNormal.x >= absWorldNormal.x && absWorldNormal.y >= absWorldNormal.z)
			{
				if (worldNormal.x>0) v.tangent = float4(0, 0, 1, -1);
				else v.tangent = float4(0, 0, -1, -1);
			}

			/*float4 worldNormal = normalize(mul(_Object2World, float4(v.normal, 0)));
			float3 absWorldNormal = abs(worldNormal);
			if (absWorldNormal.y >= absWorldNormal.x && absWorldNormal.y >= absWorldNormal.z) v.tangent = float4(0.7, 0.7, 0, 0);
			//if (abs(o.directions.y) >= abs(o.directions.x) && abs(o.directions.y) >= abs(o.directions.z)) v.tangent = float4(0.7,0.7,0,0);
			else v.tangent = float4(0, 1, 0, 0);
			v.tangent = mul(_World2Object, v.tangent);
			v.tangent.xyz = normalize(cross(v.normal, v.tangent.xyz));
			v.tangent.w = -1;*/

			//getting directions - for tesselation
			//float3 normalPow = abs(pow(worldNormal, 8)) * worldNormal; //las multiply is needed to set + or -
			//float3 directions = normalPow / (abs(normalPow.x) + abs(normalPow.y) + abs(normalPow.z));
		}

		inline fixed4 GetTriplanarColor (sampler2D tex, float3 pos, float3 directions)
		{
			fixed4 color = fixed4(0,0,0,0);
			
			float invPos = 0;

			color += tex2D(tex, float2(directions.y>0 ? -pos.z : pos.z, pos.x)) * abs(directions.y);
			color += tex2D(tex, float2(directions.z>0 ? -pos.x : pos.x, pos.y)) * abs(directions.z);
			color += tex2D(tex, float2(directions.x<0 ? -pos.z : pos.z, pos.y)) * abs(directions.x);

			return color;
		}

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) 
		{
			//getting directions
			float3 worldNormal = WorldNormalVector(IN, fixed3(0,0,1)); //cannot get IN.worldNormal directly because shader writes to o.Normal
			float3 normalPow = abs(pow(worldNormal, 8)) * worldNormal; //las multiply is needed to set + or -
			float3 directions = normalPow / (abs(normalPow.x) + abs(normalPow.y) + abs(normalPow.z));

			//albedo
			fixed4 color = GetTriplanarColor(_MainTex, IN.worldPos*_Tile, directions) * IN.uv_MainTex.x;
			fixed4 color2 = GetTriplanarColor(_MainTex2, IN.worldPos*_Tile2, directions) * IN.uv_MainTex.y;
			fixed4 color3 = GetTriplanarColor(_MainTex3, IN.worldPos*_Tile3, directions) * IN.uv2_BumpMap.x;
			fixed4 color4 = GetTriplanarColor(_MainTex4, IN.worldPos*_Tile4, directions) * IN.uv2_BumpMap.y;
			fixed4 albedo = color*_Color + color2*_Color2 + color3*_Color3 + color4*_Color4; //getting albedo per-channel to use it's alpha in specular
			o.Albedo = albedo.rgb;

			//normal map
			#if _NORMALMAP
			fixed4 normal = fixed4(0,0,0,0); //note that it is not vector normal, but a normal map
			normal += GetTriplanarColor(_BumpMap, IN.worldPos*_Tile, directions) * IN.uv_MainTex.x;
			normal += GetTriplanarColor(_BumpMap2, IN.worldPos*_Tile2, directions) * IN.uv_MainTex.y;
			normal += GetTriplanarColor(_BumpMap3, IN.worldPos*_Tile3, directions) * IN.uv2_BumpMap.x;
			normal += GetTriplanarColor(_BumpMap4, IN.worldPos*_Tile4, directions) * IN.uv2_BumpMap.y;
			o.Normal = UnpackNormal(normal);
			#else
			o.Normal = float3(0,0,1); //have to write in o.Normal to make shader work
			#endif

			//specular and gloss
			fixed4 specgloss = fixed4(0,0,0,0);
			#if _SPECGLOSSMAP
			specgloss += GetTriplanarColor(_SpecGlossMap, IN.worldPos*_Tile, directions) * IN.uv_MainTex.x * _Specular*2;
			specgloss += GetTriplanarColor(_SpecGlossMap2, IN.worldPos*_Tile2, directions) * IN.uv_MainTex.y * _Specular2*2;
			specgloss += GetTriplanarColor(_SpecGlossMap3, IN.worldPos*_Tile3, directions) * IN.uv2_BumpMap.x * _Specular3*2;
			specgloss += GetTriplanarColor(_SpecGlossMap4, IN.worldPos*_Tile4, directions) * IN.uv2_BumpMap.y * _Specular4*2;
			#else
			specgloss = color.a*_Specular*2 + color2.a*_Specular2*2 + color3.a*_Specular3*2 + color4.a*_Specular4*2;
			#endif

			o.Specular = specgloss.rgb;
			//o.Metallic = specgloss.r;
			o.Smoothness = specgloss.a + IN.color.a*0.0001f; //forward rendering will not use occlusion without that

			o.Occlusion = IN.color.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
	CustomEditor "VoxelandMaterialInspector"
}
