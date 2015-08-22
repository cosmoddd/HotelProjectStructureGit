Shader "Hidden/Internal-DeferredShading_UBER" {
Properties {
	_LightTexture0 ("", any) = "" {}
	_LightTextureB0 ("", 2D) = "" {}
	_ShadowMapTexture ("", any) = "" {}
	_SrcBlend ("", Float) = 1
	_DstBlend ("", Float) = 1
}

// =================================== BEGIN UBER SUPPORT ===================================
CGINCLUDE
	// when using both features check UBER_StandardConfig.cginc to configure Gbuffer channels
	// by default translucency is passed in diffuse (A) gbuffer and self-shadows are passed in normal (A) channel
	//
	// NOTE that you're not supposed to use Standard shader with occlusion data together with UBER translucency in deferred, because Standard Shader writes occlusion velue in GBUFFER0 alpha as the translucency does !
	//
	#define UBER_TRANSLUCENCY_DEFERRED
	#define UBER_POM_SELF_SHADOWS_DEFERRED
	//
	// comment this out when you'd like to have translucency in deferred not influenced by diffuse/base object color
	#define UBER_TRANSLUCENCY_DEFERRED_MULT_DIFFUSE
	//
	// define when you like to control translucency power per light (its color alpha channel)
	// note, that this can interfere with solutions that uses light color.a for different purpose (like Alloy)
	//#define UBER_TRANSLUCENCY_PER_LIGHT_ALPHA	
	//
	// you can gently turn it up (like 0.3, 0.5) if you find front facing geometry overbrighten (esp. for point lights),
	// but suppresion can negate albedo for high translucency values (they can become badly black)
	#define TRANSLUCENCY_SUPPRESS_DIFFUSECOLOR 0.0	
ENDCG
// ==================================== END UBER SUPPORT ====================================

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

#include "UnityCG.cginc"
#include "../Includes/UBER_UnityDeferredLibrary.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityStandardBRDF.cginc"

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;

// =================================== BEGIN UBER SUPPORT ===================================
// UBER - POM self-shadowing (for one realtime light)
#if defined(UBER_POM_SELF_SHADOWS_DEFERRED)
float4		_WorldSpaceLightPosCustom;
#endif

// UBER - Translucency
#if defined(UBER_TRANSLUCENCY_DEFERRED)
sampler2D	_UBERTranslucencyBuffer; // copied by command buffer from emission.a (_CameraGBufferTexture3.a which is not accessible here as it acts as target for lighting pass - we read/write into the same buffer)
half4		_TranslucencyColor;
half4		_TranslucencyColor2;
half4		_TranslucencyColor3;
half4		_TranslucencyColor4;
half		_TranslucencyStrength;
half		_TranslucencyConstant;
half		_TranslucencyNormalOffset;
half		_TranslucencyExponent;
half		_TranslucencyOcclusion;
half		_TranslucencyPointLightDirectionality;
half		_TranslucencySuppressRealtimeShadows;

half Translucency(half3 normalWorld, UnityLight light, half3 eyeVec) {
	#ifdef USING_DIRECTIONAL_LIGHT
		half tLitDot=saturate(dot( (light.dir + normalWorld*_TranslucencyNormalOffset), eyeVec) );
	#else
		float3 lightDirectional=normalize(_LightPos.xyz - _WorldSpaceCameraPos.xyz);
		light.dir=normalize(lerp(light.dir, lightDirectional, _TranslucencyPointLightDirectionality));
	 	half tLitDot=saturate( dot( (light.dir + normalWorld*_TranslucencyNormalOffset), eyeVec) );
	#endif
	tLitDot=exp2(-_TranslucencyExponent*(1-tLitDot))*_TranslucencyStrength;

	half translucencyAtten = (tLitDot+_TranslucencyConstant);
	#if defined(UBER_TRANSLUCENCY_PER_LIGHT_ALPHA)
	translucencyAtten*=_LightColor.a;
	#endif
	
	return translucencyAtten;
}
#endif
// ==================================== END UBER SUPPORT ====================================

half4 CalculateLight (unity_v2f_deferred i)
{
	float3 wpos;
	float2 uv;
	float atten, shadow_atten, fadeDist;
	UnityLight light;
	UNITY_INITIALIZE_OUTPUT(UnityLight, light);
	UnityDeferredCalculateLightParams (i, wpos, uv, light.dir, atten, shadow_atten, fadeDist);

	half4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
	half4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
	half4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);
	
	// =================================== BEGIN UBER SUPPORT ===================================
	// UBER - POM self-shadowing (for one realtime light)
	#if defined(UBER_POM_SELF_SHADOWS_DEFERRED)
		// conditional to attenuate only the selected realtime light
		#if defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE)
			atten = ( abs(dot( (_LightDir.xyz+_WorldSpaceLightPosCustom.xyz), float3(1,1,1) )) < 0.01 ) ? min(atten, gbuffer2.a) : atten;
		#else
			atten = ( abs(dot( (_LightPos.xyz-_WorldSpaceLightPosCustom.xyz), float3(1,1,1) )) < 0.01 ) ? min(atten, gbuffer2.a) : atten;
		#endif
	#endif
	// ==================================== END UBER SUPPORT ====================================
	
	light.color = _LightColor.rgb * atten;
	half3 baseColor = gbuffer0.rgb;
	half3 specColor = gbuffer1.rgb;
	half oneMinusRoughness = gbuffer1.a;
	half3 normalWorld = gbuffer2.rgb * 2 - 1;
	normalWorld = normalize(normalWorld);
	float3 eyeVec = normalize(wpos-_WorldSpaceCameraPos);
	half oneMinusReflectivity = 1 - SpecularStrength(specColor.rgb);
	light.ndotl = LambertTerm (normalWorld, light.dir);

	UnityIndirect ind;
	UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
	ind.diffuse = 0;
	ind.specular = 0;

	// =================================== BEGIN UBER SUPPORT ===================================
	#if defined(UBER_TRANSLUCENCY_DEFERRED)
		half translucency_thickness=1-tex2D(_UBERTranslucencyBuffer, uv).r; // buffer copied from _CameraGBufferTexture3.a in command buffer
		int lightIndex=frac(translucency_thickness*255.9999)*4; // 2 lower bits for light index 0..255 (HDR) (0..63 for LDR)
		half3 TranslucencyColor=_TranslucencyColor.rgb;
		TranslucencyColor = (lightIndex>=1) ? _TranslucencyColor2.rgb : TranslucencyColor;
		TranslucencyColor = (lightIndex>=2) ? _TranslucencyColor3.rgb : TranslucencyColor;
		TranslucencyColor = (lightIndex>=3) ? _TranslucencyColor4.rgb : TranslucencyColor;
		half3 TL=Translucency(normalWorld, light, eyeVec)*TranslucencyColor.rgb;
		#if defined(UBER_TRANSLUCENCY_DEFERRED_MULT_DIFFUSE)
			TL*=baseColor;
		#endif
		// 0..255 (HDR) (0..63 for LDR - TODO)
		TL*=saturate(translucency_thickness-1.0/255); // quickly clean lightindex info here (clamp it down so we are able to damp translucency to zero)
		baseColor*=saturate(1-max(max(TL.r, TL.g), TL.b)*TRANSLUCENCY_SUPPRESS_DIFFUSECOLOR);
		// suppress shadows
		shadow_atten = lerp( shadow_atten, 1, saturate( dot(TL,1)*_TranslucencySuppressRealtimeShadows ) );
	#endif
	// apply shadows here (possibly suppressed by translucency)
	light.color *= shadow_atten;
	// ==================================== END UBER SUPPORT ====================================
	
    half4 res = UNITY_BRDF_PBS (baseColor, specColor, oneMinusReflectivity, oneMinusRoughness, normalWorld, -eyeVec, light, ind);
    
	// =================================== BEGIN UBER SUPPORT ===================================
	#if defined(UBER_TRANSLUCENCY_DEFERRED)
		res.rgb += TL*light.color;
	#endif		
	// ==================================== END UBER SUPPORT ====================================
    
	return res;
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
	ColorMask RGB
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
