#ifndef UBER_AUTOLIGHT_MOD_INCLUDED
#define UBER_AUTOLIGHT_MOD_INCLUDED

//
// replace AutoLight.cginc macros to get better independent control over shadows
//
#ifdef POINT
	#if defined(LIGHT_ATTENUATION)
		#undef LIGHT_ATTENUATION
		#define LIGHT_ATTENUATION(a) (tex2D(_LightTexture0, dot(a._LightCoord,a._LightCoord).rr).UNITY_ATTEN_CHANNEL)
	#endif
#endif

#ifdef SPOT
	#if defined(LIGHT_ATTENUATION)
		#undef LIGHT_ATTENUATION
		#define LIGHT_ATTENUATION(a) ( (a._LightCoord.z > 0) * UnitySpotCookie(a._LightCoord) * UnitySpotAttenuate(a._LightCoord.xyz) )
	#endif
#endif

#ifdef DIRECTIONAL
	#if defined(LIGHT_ATTENUATION)
		#undef LIGHT_ATTENUATION
		#define LIGHT_ATTENUATION(a) 1
	#endif
#endif

#ifdef POINT_COOKIE
	#if defined(LIGHT_ATTENUATION)
		#undef LIGHT_ATTENUATION
		#define LIGHT_ATTENUATION(a) (tex2D(_LightTextureB0, dot(a._LightCoord,a._LightCoord).rr).UNITY_ATTEN_CHANNEL * texCUBE(_LightTexture0, a._LightCoord).w )
	#endif
#endif

#ifdef DIRECTIONAL_COOKIE
	#if defined(LIGHT_ATTENUATION)
		#undef LIGHT_ATTENUATION
		#define LIGHT_ATTENUATION(a) (tex2D(_LightTexture0, a._LightCoord).w )
	#endif
#endif

#endif // UBER_AUTOLIGHT_MOD_INCLUDED
