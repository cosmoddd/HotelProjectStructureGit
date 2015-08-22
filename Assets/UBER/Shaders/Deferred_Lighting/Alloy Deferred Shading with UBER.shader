// Alloy Physical Shader Framework
// Copyright 2013-2015 RUST LLC.
// http://www.alloy.rustltd.com/
//
// (included here by permission)
// mod to apply UBER shader features
//
Shader "Hidden/Alloy/Deferred Shading with UBER" {
Properties {
	_LightTexture0 ("", any) = "" {}
	_LightTextureB0 ("", 2D) = "" {}
	_ShadowMapTexture ("", any) = "" {}
	_SrcBlend ("", Float) = 1
	_DstBlend ("", Float) = 1
}
SubShader {

// Pass 1: Lighting pass
//  LDR case - Lighting encoded into a subtractive ARGB8 buffer
//  HDR case - Lighting additively blended into floating point buffer
Pass {
	ZWrite Off
	Blend [_SrcBlend] [_DstBlend]

CGPROGRAM
#pragma target 3.0

#pragma vertex vert_deferred
#pragma fragment frag
#pragma multi_compile_lightpass
#pragma multi_compile ___ UNITY_HDR_ON

#pragma exclude_renderers nomrt

#include "HLSLSupport.cginc"
#include "UnityCG.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityDeferredLibrary.cginc"
#include "Assets/Alloy/Shaders/Lighting/Standard.cginc"
#include "Assets/Alloy/Shaders/Framework/Brdf.cginc"
#include "Assets/Alloy/Shaders/Framework/Light.cginc"
#include "Assets/Alloy/Shaders/Framework/Surface.cginc"
#include "Assets/Alloy/Shaders/Framework/Utility.cginc"

//UBER
#include "Assets/UBER/Shaders/Deferred_Lighting/UBER2AlloyDeferred.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;
		
half4 CalculateLight (unity_v2f_deferred i)
{
	AlloyLightDesc light = AlloyLightDescInit();
	AlloySurfaceDesc s = AlloySurfaceDescInit();

	i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
	float2 uv = i.uv.xy / i.uv.w;
	
	// read depth and reconstruct world position
	float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
	depth = Linear01Depth (depth);
	float4 vpos = float4(i.ray * depth,1);
	float3 wpos = mul(_CameraToWorld, vpos).xyz;

	half4 gbuffer0 = tex2D(_CameraGBufferTexture0, uv);
	half4 gbuffer1 = tex2D(_CameraGBufferTexture1, uv);
	half4 gbuffer2 = tex2D(_CameraGBufferTexture2, uv);

	s.positionWorld = wpos;
	s.albedo = gbuffer0.rgb;
	s.f0 = gbuffer1.rgb;
	s.roughness = 1.0h - gbuffer1.a;
	s.ambientOcclusion = gbuffer0.a;
	s.viewDirWorld = normalize(UnityWorldSpaceViewDir(wpos));
	s.normalWorld = normalize(gbuffer2.rgb * 2.0h - 1.0h);
	s.NdotV = DotClamped(s.normalWorld, s.viewDirWorld);
	AlloySetSpecularData(s);

	float fadeDist = UnityDeferredComputeFadeDistance(wpos, vpos.z);
	
#if defined (SPOT)|| defined (POINT) || defined (POINT_COOKIE)
	float3 tolight = _LightPos.xyz - wpos;
	light.rangeInv = sqrt(_LightPos.w); // _LightPos.w = 1/r*r
	light.range = 1.0h / light.rangeInv;
	light.size = _LightColor.a * light.range;
	AlloySphereLight(light, tolight, s.reflectionVectorWorld);
	
	#if defined (SPOT)
		float4 uvCookie = mul(_LightMatrix0, float4(wpos, 1.0f));
		light.attenuation *= (uvCookie.w < 0.0f) * tex2Dproj(_LightTexture0, UNITY_PROJ_COORD(uvCookie)).w;
		light.shadow = UnityDeferredComputeShadow(wpos, fadeDist, uv);	
		// UBER
		UBER2ALLOY_SELFSHADOW_MACRO		
	#endif //SPOT

	#if defined (POINT) || defined (POINT_COOKIE)
		light.shadow = UnityDeferredComputeShadow(-tolight, fadeDist, uv);
		// UBER
		UBER2ALLOY_SELFSHADOW_MACRO		
		
		#if defined (POINT_COOKIE)
			light.attenuation *= texCUBE(_LightTexture0, mul(_LightMatrix0, float4(wpos, 1.0f)).xyz).w;
		#endif //POINT_COOKIE
	#endif //POINT || POINT_COOKIE
#endif

	// directional light case		
#if defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE)
	light.directionWorld = -_LightDir.xyz;
	light.shadow = UnityDeferredComputeShadow (wpos, fadeDist, uv);
	// UBER
	UBER2ALLOY_SELFSHADOW_MACRO		
	
	#if defined (DIRECTIONAL_COOKIE)
		#ifdef ALLOY_SUPPORT_REDLIGHTS
			light.attenuation = redLightFunctionLegacy(_LightTexture0, wpos, s.normalWorld, s.viewDirWorld, light.directionWorld);
		#else
			light.attenuation = tex2D(_LightTexture0, mul(_LightMatrix0, half4(wpos, 1.0h)).xy).w;
		#endif
	#endif //DIRECTIONAL_COOKIE
#endif //DIRECTIONAL || DIRECTIONAL_COOKIE
	
	light.color = _LightColor.rgb;
		
	half4 color = 0.0h;
	
	//UBER
 	UBER2ALLOY_TRANSLUCENCY_MACRO_INIT	
 	
	color.rgb = AlloyHdrClamp(AlloyDirect(light, s));
	
	//UBER
	UBER2ALLOY_TRANSLUCENCY_MACRO_APPLY	
	
	return color;
}

#ifdef UNITY_HDR_ON
half4
#else
fixed4
#endif
frag (unity_v2f_deferred i) : SV_Target
{
	half4 c = CalculateLight(i);
	#ifdef UNITY_HDR_ON
	return c;
	#else
	return exp2(-c);
	#endif
}

ENDCG
}


// Pass 2: Final decode pass.
// Used only with HDR off, to decode the logarithmic buffer into the main RT
Pass {
	ZTest Always Cull Off ZWrite Off
	Stencil {
		ref [_StencilNonBackground]
		readmask [_StencilNonBackground]
		// Normally just comp would be sufficient, but there's a bug and only front face stencil state is set (case 583207)
		compback equal
		compfront equal
	}

CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma exclude_renderers nomrt

sampler2D _LightBuffer;
struct v2f {
	float4 vertex : SV_POSITION;
	float2 texcoord : TEXCOORD0;
};

v2f vert (float4 vertex : POSITION, float2 texcoord : TEXCOORD0)
{
	v2f o;
	o.vertex = mul(UNITY_MATRIX_MVP, vertex);
	o.texcoord = texcoord.xy;
	return o;
}

fixed4 frag (v2f i) : SV_Target
{
	return -log2(tex2D(_LightBuffer, i.texcoord));
}
ENDCG 
}

}
Fallback Off
}
