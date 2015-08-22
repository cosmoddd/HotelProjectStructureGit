// when using both features check UBER_StandardConfig.cginc to configure Gbuffer channels
// by default translucency is passed in diffuse (A) gbuffer and self-shadows are passed in normal (A) channel
//
// NOTE that you're not supposed to use Standard shader with occlusion data together with UBER translucency in deferred, because Standard Shader writes occlusion velue in GBUFFER0 alpha as the translucency does !
//
#define UBER_TRANSLUCENCY_DEFERRED
#define UBER_POM_SELF_SHADOWS_DEFERRED
//
// you can gently turn it up (like 0.3, 0.5) if you find front facing geometry overbrighten (esp. for point lights),
// but suppresion can negate albedo for high translucency values (they can become badly black)
#define TRANSLUCENCY_SUPPRESS_DIFFUSECOLOR 0.0
//
//
//
//
// ==================================================================================================================
//
// you don't need to modify things below
//
//
// Do NOT select - currently in Alloy3 lightcolor.a is reserved
//#define UBER_TRANSLUCENCY_PER_LIGHT_ALPHA
//
#if defined(UBER_POM_SELF_SHADOWS_DEFERRED)
	float4		_WorldSpaceLightPosCustom;
	#define UBER2ALLOY_SELFSHADOW_MACRO light.shadow = ( abs(dot( (_LightDir.xyz+_WorldSpaceLightPosCustom.xyz), float3(1,1,1) )) < 0.01 ) ? min(light.shadow, gbuffer2.a) : light.shadow;
#else	
	#define UBER2ALLOY_SELFSHADOW_MACRO
#endif

// UBER - Translucency
#if defined(UBER_TRANSLUCENCY_DEFERRED)
sampler2D _UBERTranslucencyBuffer; // copied by command buffer from emission.a (_CameraGBufferTexture3.a which is not accessible here as it acts as target for lighting pass - we read/write into the same buffer)

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

half Translucency(half3 normalWorld, AlloyLightDesc light, half3 eyeVec) {
	#ifdef USING_DIRECTIONAL_LIGHT
		half tLitDot=saturate(dot( (light.directionWorld + normalWorld*_TranslucencyNormalOffset), eyeVec) );
	#else
		float3 lightDirectional=normalize(_LightPos.xyz - _WorldSpaceCameraPos.xyz);
		half3 light_dir=normalize(lerp(light.directionWorld, lightDirectional, _TranslucencyPointLightDirectionality));
	 	half tLitDot=saturate( dot( (light_dir + normalWorld*_TranslucencyNormalOffset), eyeVec) );
	#endif
	tLitDot=exp2(-_TranslucencyExponent*(1-tLitDot))*_TranslucencyStrength;

	half translucencyAtten = (tLitDot+_TranslucencyConstant);
	#if defined(UBER_TRANSLUCENCY_PER_LIGHT_ALPHA)
	translucencyAtten*=_LightColor.a;
	#endif
	
	return translucencyAtten;
}

// 0..255 (HDR) (0..63 for LDR - TODO - change all 255 occurences below to 63)
#define UBER2ALLOY_TRANSLUCENCY_MACRO_INIT half translucency_thickness=1-tex2D(_UBERTranslucencyBuffer, uv).r;\
		int lightIndex=frac(translucency_thickness*255.9999)*4;\
		half3 TranslucencyColor=_TranslucencyColor.rgb;\
		TranslucencyColor = (lightIndex>=1) ? _TranslucencyColor2.rgb : TranslucencyColor;\
		TranslucencyColor = (lightIndex>=2) ? _TranslucencyColor3.rgb : TranslucencyColor;\
		TranslucencyColor = (lightIndex>=3) ? _TranslucencyColor4.rgb : TranslucencyColor;\
		half3 TL=Translucency(s.normalWorld, light, normalize(wpos-_WorldSpaceCameraPos))*TranslucencyColor.rgb;\
		TL*=s.albedo;\
		TL*=saturate(translucency_thickness-1.0/255);\
		s.albedo*=saturate(1-max(max(TL.r, TL.g), TL.b)*TRANSLUCENCY_SUPPRESS_DIFFUSECOLOR);\
		light.shadow = lerp( light.shadow, 1, saturate( dot(TL,1)*_TranslucencySuppressRealtimeShadows ) );

#define UBER2ALLOY_TRANSLUCENCY_MACRO_APPLY color.rgb+=(light.shadow * light.attenuation)*TL*light.color.rgb;

#else
	#define UBER2ALLOY_TRANSLUCENCY_MACRO_INIT
	#define UBER2ALLOY_TRANSLUCENCY_MACRO_APPLY
#endif