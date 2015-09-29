Shader "Voxeland/Standard" 
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

		_Color5 ("Color (RGB)", Color) = (1,1,1,1)
		_MainTex5 ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap5 ("Normal Map", 2D) = "bump" {}
		_SpecGlossMap5 ("Spec Map (RGB), Smooth Map (A)", 2D) = "white" {}
		_Specular5 ("Spec Value (RGB), Smooth Val (A)", Color) = (0,0,0,0.5)
		_Tile5 ("Tile", Float) = 0.25

		_Color6 ("Color (RGB)", Color) = (1,1,1,1)
		_MainTex6 ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap6 ("Normal Map", 2D) = "bump" {}
		_SpecGlossMap6 ("Spec Map (RGB), Smooth Map (A)", 2D) = "white" {}
		_Specular6 ("Spec Value (RGB), Smooth Val (A)", Color) = (0,0,0,0.5)
		_Tile6 ("Tile", Float) = 0.25

		_Color7 ("Color (RGB)", Color) = (1,1,1,1)
		_MainTex7 ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap7 ("Normal Map", 2D) = "bump" {}
		_SpecGlossMap7 ("Spec Map (RGB), Smooth Map (A)", 2D) = "white" {}
		_Specular7 ("Spec Value (RGB), Smooth Val (A)", Color) = (0,0,0,0.5)
		_Tile7 ("Tile", Float) = 0.25

		_Color8 ("Color (RGB)", Color) = (1,1,1,1)
		_MainTex8 ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap8 ("Normal Map", 2D) = "bump" {}
		_SpecGlossMap8 ("Spec Map (RGB), Smooth Map (A)", 2D) = "white" {}
		_Specular8 ("Spec Value (RGB), Smooth Val (A)", Color) = (0,0,0,0.5)
		_Tile8 ("Tile", Float) = 0.25

		_Tex1 ("UV1", 2D) = "white" {}
		_Tex2 ("UV2", 2D) = "white" {}
		_Tex3 ("UV3", 2D) = "white" {}
		_Tex4 ("UV4", 2D) = "white" {}
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows vertex:vert
		#pragma target 3.0

		#pragma shader_feature _NORMALMAP
		#pragma shader_feature __ _8CH _SPECGLOSSMAP _PARALLAXMAP
		//#pragma multi_compile 

		/*struct Channel {
			fixed4 _Color;
			sampler2D _MainTex;
			sampler2D _BumpMap;
			fixed4 _Specular;
			half _Tile; };*/
		//vs_4_0 does not allow textures or samplers to be members of compound types


		fixed4 _Color;				fixed4 _Color2;				fixed4 _Color3;				fixed4 _Color4;
		sampler2D _MainTex;			sampler2D _MainTex2;		sampler2D _MainTex3;		sampler2D _MainTex4;
		fixed4 _Specular;			fixed4 _Specular2;			fixed4 _Specular3;			fixed4 _Specular4;
		half _Tile;					half _Tile2;				half _Tile3;				half _Tile4;
		
		#if _8CH
		fixed4 _Color5;				fixed4 _Color6;				fixed4 _Color7;				fixed4 _Color8;
		sampler2D _MainTex5;		sampler2D _MainTex6;		sampler2D _MainTex7;		sampler2D _MainTex8;
		fixed4 _Specular5;			fixed4 _Specular6;			fixed4 _Specular7;			fixed4 _Specular8;
		half _Tile5;				half _Tile6;				half _Tile7;				half _Tile8;
		#endif
		
		#if _NORMALMAP
			sampler2D _BumpMap;			sampler2D _BumpMap2;		sampler2D _BumpMap3;		sampler2D _BumpMap4;
			#if _8CH
			sampler2D _BumpMap5;		sampler2D _BumpMap6;		sampler2D _BumpMap7;		//sampler2D _BumpMap8;
			#endif
		#endif
		
		#if _SPECGLOSSMAP
		sampler2D _SpecGlossMap;	sampler2D _SpecGlossMap2;	sampler2D _SpecGlossMap3;	sampler2D _SpecGlossMap4;
		#endif

		#if _8CH
		sampler2D _SpecGlossMap5;	sampler2D _SpecGlossMap6;	sampler2D _SpecGlossMap7;	sampler2D _SpecGlossMap8;
		#endif
		
		//#if _PARALLAXMAP
		//sampler2D _ParallaxMap;	sampler2D _ParallaxMap2;	sampler2D _ParallaxMap3;	sampler2D _ParallaxMap4;
		//float _Parallax;			float _Parallax2;			float _Parallax3;			float _Parallax4;
		//#if _8CH
		//sampler2D _ParallaxMap5;	sampler2D _ParallaxMap6;	sampler2D _ParallaxMap7;	sampler2D _ParallaxMap8;
		//float _Parallax5;			float _Parallax6;			float _Parallax7;			float _Parallax8;
		//#endif
		//#endif
		


		struct Input {
			float2 uv_Tex1; //type
			float2 uv2_Tex2; //type
			float2 uv3_Tex3; //type
			float2 uv4_Tex4; //type
			float4 color : COLOR; //ambient
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};


		void vert(inout appdata_full v)
		{
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
			
			//tangents
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

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			//getting directions
			float3 worldNormal = WorldNormalVector(IN, fixed3(0,0,1)); //cannot get IN.worldNormal directly because shader writes to o.Normal
			float3 normalPow = abs(pow(worldNormal, 8)) * worldNormal; //las multiply is needed to set + or -
			float3 directions = normalPow / (abs(normalPow.x) + abs(normalPow.y) + abs(normalPow.z));

			//albedo
			fixed4 color = GetTriplanarColor(_MainTex, IN.worldPos*_Tile, directions) * IN.uv_Tex1.x;
			fixed4 color2 = GetTriplanarColor(_MainTex2, IN.worldPos*_Tile2, directions) * IN.uv_Tex1.y;
			fixed4 color3 = GetTriplanarColor(_MainTex3, IN.worldPos*_Tile3, directions) * IN.uv2_Tex2.x;
			fixed4 color4 = GetTriplanarColor(_MainTex4, IN.worldPos*_Tile4, directions) * IN.uv2_Tex2.y;
			fixed4 albedo = color*_Color + color2*_Color2 + color3*_Color3 + color4*_Color4; //getting albedo per-channel to use it's alpha in specular
			
			#if _8CH
			fixed4 color5 = GetTriplanarColor(_MainTex5, IN.worldPos*_Tile5, directions) * IN.uv3_Tex3.x;
			fixed4 color6 = GetTriplanarColor(_MainTex6, IN.worldPos*_Tile6, directions) * IN.uv3_Tex3.y;
			fixed4 color7 = GetTriplanarColor(_MainTex5, IN.worldPos*_Tile7, directions) * IN.uv4_Tex4.x;
			fixed4 color8 = GetTriplanarColor(_MainTex6, IN.worldPos*_Tile8, directions) * IN.uv4_Tex4.y;
			albedo += color5*_Color5 + color6*_Color6 + color7*_Color7 + color8*_Color8;
			#endif

			o.Albedo = albedo.rgb;

			//specular and gloss
			fixed4 specgloss = fixed4(0,0,0,0);
			#if _SPECGLOSSMAP
				specgloss += GetTriplanarColor(_SpecGlossMap, IN.worldPos*_Tile, directions) * IN.uv_Tex1.x * _Specular*2;
				specgloss += GetTriplanarColor(_SpecGlossMap2, IN.worldPos*_Tile2, directions) * IN.uv_Tex1.y * _Specular2*2;
				specgloss += GetTriplanarColor(_SpecGlossMap3, IN.worldPos*_Tile3, directions) * IN.uv2_Tex2.x * _Specular3*2;
				specgloss += GetTriplanarColor(_SpecGlossMap4, IN.worldPos*_Tile4, directions) * IN.uv2_Tex2.y * _Specular4*2;
			#else
				specgloss += color.a*_Specular*2 + color2.a*_Specular2*2 + color3.a*_Specular3*2 + color4.a*_Specular4*2;
				#if _8CH
				specgloss += color5.a*_Specular5*2 + color6.a*_Specular6*2 + color7.a*_Specular7*2 + color8.a*_Specular8*2;
				#endif
			#endif

			//normal map
			#if _NORMALMAP
				fixed4 normal = fixed4(0,0,0,0); //note that it is not vector normal, but a normal map
				normal += GetTriplanarColor(_BumpMap, IN.worldPos*_Tile, directions) * IN.uv_Tex1.x;
				normal += GetTriplanarColor(_BumpMap2, IN.worldPos*_Tile2, directions) * IN.uv_Tex1.y;
				normal += GetTriplanarColor(_BumpMap3, IN.worldPos*_Tile3, directions) * IN.uv2_Tex2.x;
				normal += GetTriplanarColor(_BumpMap4, IN.worldPos*_Tile4, directions) * IN.uv2_Tex2.y;
				
				#if _8CH
				normal += GetTriplanarColor(_BumpMap5, IN.worldPos*_Tile5, directions) * IN.uv3_Tex3.x;
				normal += GetTriplanarColor(_BumpMap6, IN.worldPos*_Tile6, directions) * IN.uv3_Tex3.y;
				normal += GetTriplanarColor(_BumpMap7, IN.worldPos*_Tile7, directions) * IN.uv4_Tex4.x;
				normal += fixed4(0.5,0.5,0.5,0.5) * IN.uv4_Tex4.y; //normal += GetTriplanarColor(_BumpMap8, IN.worldPos*_Tile8, directions) * IN.uv4_Tex4.y;
				#endif
				
				o.Normal = UnpackNormal(normal);
			#else
				o.Normal = float3(0,0,1); //have to write in o.Normal to make shader work
			#endif

			//o.Specular = specgloss.rgb;
			o.Metallic = specgloss.r;
			o.Smoothness = specgloss.a + IN.color.a*0.0001f; //forward rendering will not use occlusion without that

			o.Occlusion = IN.color.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
	CustomEditor "VoxelandMaterialInspector"
}
