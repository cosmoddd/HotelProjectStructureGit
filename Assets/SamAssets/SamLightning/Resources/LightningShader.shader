Shader "Unlit/LightningShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_CutoffDist("CutoffDist", Range(-1,1)) = 1
		_Tilt("Tilt", Float) = 0
		_Amplitude("Amplitude", Float) = 50
		_Freq("Frequency", Float) = 10
		_TimeScale("Timescale", Float) = 1
		_EmissionGain("Emission Gain", Range(0, 2)) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque"}
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "fastnoise.cginc"
			#include "noiseSimplex.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			float _CutoffDist;
			float _Tilt;
			float _Amplitude;
			float _Freq;
			float _TimeScale;
			float _EmissionGain;

			v2f vert (appdata v){
				v2f o;

				float4 cutoffVert = mul(UNITY_MATRIX_MVP, float4(v.vertex.x, _CutoffDist, v.vertex.zw));
				v.vertex.x *= 10;

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				float3 time = float3(_Time.x * _TimeScale * 7, _Tilt, v.vertex.y)*_Freq;
				half distmodifier = -pow((2*o.uv.y - 1f),2)+1;
				v.vertex.x += lerp(0, (snoise(time)-.5f) * _Amplitude + _Tilt, distmodifier);
				o.vertex = lerp(mul(UNITY_MATRIX_MVP, v.vertex), cutoffVert, step(_CutoffDist, v.vertex.y));

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target{
				fixed4 col = fixed4(_Color.rgb, tex2D(_MainTex, i.uv)*_Color.a) * (exp(_EmissionGain * 10.0f));
				return col;
			}
			ENDCG
		}
	}
}
