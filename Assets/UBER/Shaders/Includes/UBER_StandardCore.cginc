#ifndef UBER_STANDARD_CORE_INCLUDED
#define UBER_STANDARD_CORE_INCLUDED

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "../UBER_StandardConfig.cginc"
#include "../Includes/UBER_StandardInput.cginc"
#include "../Includes/UBER_PBSLighting.cginc"
#include "../Includes/UBER_StandardUtils.cginc"
#include "../Includes/UBER_StandardUtils2.cginc"
#include "../Includes/UBER_StandardBRDF.cginc"
#include "UnityMetaPass.cginc"

#include "AutoLight.cginc"
// replace AutoLight.cginc LIGHT_ATTENUATION() macros to get independent control over shadows
#include "../Includes/UBER_AutoLightMOD.cginc"

//-------------------------------------------------------------------------------------

// UBER
inline float3 DeferredLightDir( in float3 worldPos )
{
	#ifndef USING_LIGHT_MULTI_COMPILE
		return _WorldSpaceLightPosCustom.xyz - worldPos.xyz * _WorldSpaceLightPosCustom.w;
	#else
		#ifndef USING_DIRECTIONAL_LIGHT
		return _WorldSpaceLightPosCustom.xyz - worldPos.xyz;
		#else
		return _WorldSpaceLightPosCustom.xyz;
		#endif
	#endif
}

//-------------------------------------------------------------------------------------
UnityLight MainLight (half3 normalWorld)
{
	UnityLight l;
	#ifdef LIGHTMAP_OFF
		
		l.color = _LightColor0.rgb;
		l.dir = _WorldSpaceLightPos0.xyz;
		l.ndotl = LambertTerm (normalWorld, l.dir);
	#else
		// no light specified by the engine
		// analytical light might be extracted from Lightmap data later on in the shader depending on the Lightmap type
		l.color = half3(0.f, 0.f, 0.f);
		l.ndotl  = 0.f;
		l.dir = half3(0.f, 0.f, 0.f);
	#endif

	return l;
}

UnityLight AdditiveLight (half3 normalWorld, half3 lightDir, half atten)
{
	UnityLight l;

	l.color = _LightColor0.rgb;
	l.dir = lightDir;
	#ifndef USING_DIRECTIONAL_LIGHT
		l.dir = normalize(l.dir);
	#endif
	l.ndotl = LambertTerm (normalWorld, l.dir);

	// shadow the light
	l.color *= atten;
	return l;
}

UnityLight DummyLight (half3 normalWorld)
{
	UnityLight l;
	l.color = 0;
	l.dir = half3 (0,1,0);
	l.ndotl = LambertTerm (normalWorld, l.dir);
	return l;
}

UnityIndirect ZeroIndirect ()
{
	UnityIndirect ind;
	ind.diffuse = 0;
	ind.specular = 0;
	return ind;
}

//-------------------------------------------------------------------------------------


#if !defined(DISTANCE_MAP) && (defined(_POM_DISTANCE_MAP) || defined(_POM_DISTANCE_MAP_ZWRITE) || defined(_POM_DISTANCE_MAP_SHADOWS))
	#define DISTANCE_MAP
#endif

#if !defined(EXTRUSION_MAP) && (defined(_POM_EXTRUSION_MAP) || defined(_POM_EXTRUSION_MAP_ZWRITE) || defined(_POM_EXTRUSION_MAP_SHADOWS))
	#define EXTRUSION_MAP
#endif

#if !defined(POM) && (defined(_PARALLAX_POM) || defined(_PARALLAX_POM_ZWRITE) || defined(_PARALLAX_POM_SHADOWS))
	#define POM
#endif

#if !defined(TRIPLANAR) && (defined(TRIPLANAR_SELECTIVE))
 	#define TRIPLANAR
#endif

#if (defined(_PARALLAX_POM_ZWRITE) || defined(_POM_DISTANCE_MAP_ZWRITE) || defined(_POM_EXTRUSION_MAP_ZWRITE)) && !defined(ZWRITE)
	#define ZWRITE 1
#endif

// UBER
#if defined(POM) || defined(_TRANSLUCENCY) || defined(_GLITTER) || defined(_SNOW) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
	#define IN_WORLDPOSADD(i) i.posWorld.xyz
#else
	#define IN_WORLDPOSADD(i) half3(0,0,0)
#endif
#if UNITY_SPECCUBE_BOX_PROJECTION || defined(POM) || defined(_TRANSLUCENCY) || defined(_GLITTER) || defined(_SNOW) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
	#define IN_WORLDPOS(i) i.posWorld.xyz
#else
	#define IN_WORLDPOS(i) half3(0,0,0)
#endif

#if ( defined(ZWRITE) || (defined(_SNOW) && (defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP))) ) && !defined(TRIPLANAR_SELECTIVE)
	#if !defined(RAYLENGTH_AVAILABLE)
		#define RAYLENGTH_AVAILABLE
	#endif
#endif

#if !defined(DEPTH_SEMANTIC)
	#if defined(SHADER_API_D3D11) && (SHADER_TARGET>=50) && CONSERVATIVE_DEPTH_WRITE
		#define DEPTH_SEMANTIC SV_DepthGreaterEqual
	#else
		#define DEPTH_SEMANTIC SV_Depth
	#endif
#endif

#define FRAGMENT_SETUP(x) FragmentCommonData x = \
	FragmentSetup(i.tex, eyeVec, worldNormal, i_viewDirForParallax, _TBN, IN_WORLDPOS(i), i.vertex_color, _ddx, _ddy, _ddxDet, _ddyDet, tangentBasisScaled, SclCurv, blendFade, actH); // UBER - additional params added

#define FRAGMENT_SETUP_FWDADD(x) FragmentCommonData x = \
	FragmentSetup(i.tex, eyeVec, worldNormal, i_viewDirForParallax, _TBN, IN_WORLDPOSADD(i), i.vertex_color, _ddx, _ddy, _ddxDet, _ddyDet, tangentBasisScaled, SclCurv, blendFade, actH); // UBER - additional params added

struct FragmentCommonData
{
	half3 diffColor, specColor;
	// Note: oneMinusRoughness & oneMinusReflectivity for optimization purposes, mostly for DX9 SM2.0 level.
	// Most of the math is being done on these (1-x) values, and that saves a few precious ALU slots.
	// (UBER - comment: oneMinusReflectivity is like shader forge "1-specular monochrome")
	half oneMinusReflectivity, oneMinusRoughness;
	half3 normalWorld, eyeVec, posWorld;
	half alpha;
	// UBER
	half3 pureAlbedo; // used for translucency (w/o energy conservation)
	half3 additionalEmission;
	// needed for SS
	#if defined(_PARALLAX_POM_SHADOWS) || defined(_POM_DISTANCE_MAP_SHADOWS) || defined(_POM_EXTRUSION_MAP_SHADOWS)
	float4 rayPos;
	half3x3 tanToWorld;
	#endif
	float2 texture2ObjectRatio;
	#if defined(_WETNESS)
	half Wetness;
	#endif	
	#if defined(_SNOW)
	half snowVal;
	half dissolveMaskValue;
	#endif	
	#if defined(ZWRITE)
	float rayLength;
	#endif
};

#ifndef UNITY_SETUP_BRDF_INPUT
	#define UNITY_SETUP_BRDF_INPUT SpecularSetup
#endif

void SetupUBER_VertexData_TriplanarWorld(half3 normalWorld, inout half4 i_tangentToWorldAndParallax0, inout half4 i_tangentToWorldAndParallax1, inout half4 i_tangentToWorldAndParallax2) {
	i_tangentToWorldAndParallax0.xyz = cross(normalWorld,cross(normalWorld, float3(0,0,1))); // tangents in world space
	i_tangentToWorldAndParallax0.xyz *= normalWorld.x<0 ? -1:1;
	i_tangentToWorldAndParallax1.xyz = cross(normalWorld,cross(normalWorld, float3(1,0,0)));
	i_tangentToWorldAndParallax1.xyz *= normalWorld.y<0 ? -1:1;
	i_tangentToWorldAndParallax2.xyz = cross(normalWorld,cross(normalWorld, float3(0,1,0)));
	i_tangentToWorldAndParallax2.xyz *= normalWorld.z>0 ? 1:-1;
}

void SetupUBER_VertexData_TriplanarLocal(half3 normalObject, inout half4 i_tangentToWorldAndParallax0, inout half4 i_tangentToWorldAndParallax1, inout half4 i_tangentToWorldAndParallax2, out float scaleX, out float scaleY, out float scaleZ) {
	scaleX = length(float3(_Object2World[0][0], _Object2World[1][0], _Object2World[2][0]));
	scaleY = length(float3(_Object2World[0][1], _Object2World[1][1], _Object2World[2][1]));
	scaleZ = length(float3(_Object2World[0][2], _Object2World[1][2], _Object2World[2][2]));

	i_tangentToWorldAndParallax0.xyz = cross(normalObject, cross(normalObject, float3(0,0,1))); // tangents in obj space
	i_tangentToWorldAndParallax0.xyz *= normalObject.x<0 ? -1:1;
	i_tangentToWorldAndParallax1.xyz = cross(normalObject, cross(normalObject, float3(1,0,0)));
	i_tangentToWorldAndParallax1.xyz *= normalObject.y<0 ? -1:1;
	i_tangentToWorldAndParallax2.xyz = cross(normalObject, cross(normalObject, float3(0,1,0)));
	i_tangentToWorldAndParallax2.xyz *= normalObject.z>0 ? 1:-1;
}

void SetupUBER(float4 i_SclCurv, float3 i_eyeVec, float3 i_posWorld, float3 i_posObject, inout float4 i_tex, inout half4 i_tangentToWorldAndParallax0, inout half4 i_tangentToWorldAndParallax1, inout half4 i_tangentToWorldAndParallax2, inout fixed4 vertex_color, out half actH, out float4 SclCurv, out half3 eyeVec, out float3 tangentBasisScaled, out float2 _ddx, out float2 _ddy, out float2 _ddxDet, out float2 _ddyDet, out float blendFade, out half3 i_viewDirForParallax, out half3x3 _TBN, out half3 worldNormal, out float4 texcoordsNoTransform) {
	
	// (out) compiled out when not used
	#if defined(GEOM_BLEND)
		actH=1; // for geom blend - default h is ceil value (but is supposed to be set later in parallax computation or triplanar init setup below)
	#else
		actH=0; // wetness
	#endif

	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			float3 normBlend=i_tex.xyz; // world normal
			float3 posUVZ=i_posWorld.xyz;
			float3 blendVal = abs(normBlend);
		#else
			float3 objScale=float3(i_tangentToWorldAndParallax0.w, i_tangentToWorldAndParallax1.w, i_tangentToWorldAndParallax2.w);
			float3 normObj=i_tex.xyz;
			float3 normBlend=normObj;
			float3 normObjScaled=normalize(normObj/objScale);
			float3 posUVZ=i_posObject.xyz*objScale;
			float3 blendVal = abs(normObjScaled);
		#endif
		float3 uvz = posUVZ.xyz*_MainTex_ST.xxx;
		#if RESOLVE_TRIPLANAR_HEIGHT_SEAMS
			half3 hVal = float3(tex2Dgrad(_ParallaxMap, (normBlend.x>0) ? uvz.zy : float2(-uvz.z,uvz.y) RESOLVE_SEAMS_X).PARALLAX_CHANNEL, tex2Dgrad(_ParallaxMap, (normBlend.y>0) ? uvz.xz : float2(-uvz.x,uvz.z) RESOLVE_SEAMS_Y).PARALLAX_CHANNEL, tex2Dgrad(_ParallaxMap, (normBlend.z>0) ? uvz.yx : float2(-uvz.y,uvz.x) RESOLVE_SEAMS_Z).PARALLAX_CHANNEL);
			#if defined(_TWO_LAYERS)
				float3 uvz2 = posUVZ.xyz*_DetailAlbedoMap_ST.xxx;
				#if defined(_PARALLAX_MAP_2MAPS)
					half3 hVal2 = float3(tex2Dgrad(_ParallaxMap2, (normBlend.x>0) ? uvz2.zy : float2(-uvz2.z,uvz2.y) RESOLVE_SEAMS_X).PARALLAX_CHANNEL, tex2Dgrad(_ParallaxMap2, (normBlend.y>0) ? uvz2.xz : float2(-uvz2.x,uvz2.z) RESOLVE_SEAMS_Y).PARALLAX_CHANNEL, tex2Dgrad(_ParallaxMap2, (normBlend.z>0) ? uvz2.yx : float2(-uvz2.y,uvz2.x) RESOLVE_SEAMS_Z).PARALLAX_CHANNEL);
				#else
					half3 hVal2 = float3(tex2Dgrad(_ParallaxMap2, (normBlend.x>0) ? uvz2.zy : float2(-uvz2.z,uvz2.y) RESOLVE_SEAMS_X).PARALLAX_CHANNEL_2ND_LAYER, tex2Dgrad(_ParallaxMap2, (normBlend.y>0) ? uvz2.xz : float2(-uvz2.x,uvz2.z) RESOLVE_SEAMS_Y).PARALLAX_CHANNEL_2ND_LAYER, tex2Dgrad(_ParallaxMap2, (normBlend.z>0) ? uvz2.yx : float2(-uvz2.y,uvz2.x) RESOLVE_SEAMS_Z).PARALLAX_CHANNEL_2ND_LAYER);
				#endif
				hVal = lerp( hVal2, hVal, __VERTEX_COLOR_CHANNEL_LAYER);
			#endif
		#else
			half3 hVal = float3(tex2D(_ParallaxMap, (normBlend.x>0) ? uvz.zy : float2(-uvz.z,uvz.y) ).PARALLAX_CHANNEL, tex2D(_ParallaxMap, (normBlend.y>0) ? uvz.xz : float2(-uvz.x,uvz.z) ).PARALLAX_CHANNEL, tex2D(_ParallaxMap, (normBlend.z>0) ? uvz.yx : float2(-uvz.y,uvz.x) ).PARALLAX_CHANNEL);
			#if defined(_TWO_LAYERS)
				float3 uvz2 = posUVZ.xyz*_DetailAlbedoMap_ST.xxx;
				#if defined(_PARALLAX_MAP_2MAPS)
					half3 hVal2 = float3(tex2D(_ParallaxMap2, (normBlend.x>0) ? uvz2.zy : float2(-uvz2.z,uvz2.y) ).PARALLAX_CHANNEL, tex2D(_ParallaxMap2, (normBlend.y>0) ? uvz2.xz : float2(-uvz2.x,uvz2.z) ).PARALLAX_CHANNEL, tex2D(_ParallaxMap2, (normBlend.z>0) ? uvz2.yx : float2(-uvz2.y,uvz2.x) ).PARALLAX_CHANNEL);
				#else
					half3 hVal2 = float3(tex2D(_ParallaxMap2, (normBlend.x>0) ? uvz2.zy : float2(-uvz2.z,uvz2.y) ).PARALLAX_CHANNEL_2ND_LAYER, tex2D(_ParallaxMap2, (normBlend.y>0) ? uvz2.xz : float2(-uvz2.x,uvz2.z) ).PARALLAX_CHANNEL_2ND_LAYER, tex2D(_ParallaxMap2, (normBlend.z>0) ? uvz2.yx : float2(-uvz2.y,uvz2.x) ).PARALLAX_CHANNEL_2ND_LAYER);
				#endif
				hVal = lerp( hVal2, hVal, __VERTEX_COLOR_CHANNEL_LAYER);
			#endif
		#endif
		
		blendVal += _TriplanarHeightmapBlendingValue*hVal;
		blendVal /= dot(blendVal,1);
		
		float maxXY = max(blendVal.x,blendVal.y);
		float3 tri_mask = (blendVal.x>blendVal.y) ? float3(1,0,0) : float3(0,1,0);
		tri_mask = (blendVal.z>maxXY) ? float3(0,0,1) : tri_mask;
		
		// inited here, reused in parallax function		
		#if defined(_TWO_LAYERS)
			// need to call GetH to set height blending between layers
			{
			float2 control=float2(__VERTEX_COLOR_CHANNEL_LAYER, 1-__VERTEX_COLOR_CHANNEL_LAYER);
			float2 hgt=float2(dot(hVal2, blendVal), dot(hVal, blendVal));
			control*=hgt+0.01;			// height evaluation
			control*=control; 			// compress
			control/=dot(control,1);	// normalize
			control*=control;			// compress
			control*=control;			// compress
			control*=control;			// compress
			control/=dot(control,1);	// normalize
			
			__VERTEX_COLOR_CHANNEL_LAYER=control.x; // write blending value back into the right vertex_color channel variable
			actH = lerp(hgt.x, hgt.y, __VERTEX_COLOR_CHANNEL_LAYER);
			}
		#else
			actH = dot(hVal, blendVal);
		#endif
		
		blendVal.xy = blendVal.y > blendVal.x ? blendVal.yx : blendVal.xy;
		blendVal.yz = blendVal.z > blendVal.y ? blendVal.zy : blendVal.yz;
		blendVal.xy = blendVal.y > blendVal.x ? blendVal.yx : blendVal.xy;
		// now blendVal.x = max , blendVal.y = mid, blendVal.z = min from initial blendVal.xyz components
		blendFade = saturate( (blendVal.x-blendVal.y)/blendVal.x*_TriplanarBlendSharpness );
		
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			float3 tangent_flip = tri_mask * ((normBlend.xyz<0) ? float3(1,1,1) : float3(-1,-1,-1));
		#else
			float3 tangent_flip = tri_mask * ((normBlend.xyz>0) ? float3(1,1,1) : float3(-1,-1,-1));
		#endif
		i_tex.xy = float2(tangent_flip.x, tri_mask.x)*posUVZ.zy + float2(tangent_flip.y, tri_mask.y)*posUVZ.xz + float2(tangent_flip.z, tri_mask.z)*posUVZ.yx;
		i_tex.zw = i_tex.xy*_DetailAlbedoMap_ST.xx;
		i_tex.xy *= _MainTex_ST.xx;
		texcoordsNoTransform=0; // secondary occlusion, not used - we have no real texcoords
	#else
		blendFade=0; // not used
		texcoordsNoTransform=i_tex;
		i_tex.zw = TRANSFORM_TEX(((_UVSec == 0) ? i_tex.xy : i_tex.zw), _DetailAlbedoMap);
		i_tex.xy = TRANSFORM_TEX(i_tex.xy, _MainTex); // Always source from uv0
	#endif
	
	// UBER
	#if defined(TRIPLANAR_SELECTIVE)
		float3 _ddx3=ddx(uvz);
		_ddx = tri_mask.xx*_ddx3.zy + tri_mask.yy*_ddx3.xz + tri_mask.zz*_ddx3.yx;
		
		float3 _ddy3=ddy(uvz);
		_ddy = tri_mask.xx*_ddy3.zy + tri_mask.yy*_ddy3.xz + tri_mask.zz*_ddy3.yx;
		
		_ddxDet=_ddx/_MainTex_ST.xx*_DetailAlbedoMap_ST.xx;
		_ddyDet=_ddy/_MainTex_ST.xx*_DetailAlbedoMap_ST.xx;
	#elif defined(POM) || defined(_SNOW) || defined(_GLITTER) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		_ddx=ddx(i_tex.xy);
		_ddy=ddy(i_tex.xy);
		_ddxDet=ddx(i_tex.zw);
		_ddyDet=ddy(i_tex.zw);
	#else
		_ddx=0;
		_ddy=0;
		_ddxDet=0;
		_ddyDet=0;
	#endif	

	#if defined(RAYLENGTH_AVAILABLE)
		// we need to go from tangent to world space for zwrite and parallaxed snow (actually when snow is mapped in worldspace)
		tangentBasisScaled=float3(length(i_tangentToWorldAndParallax0.xyz), length(i_tangentToWorldAndParallax1.xyz), length(i_tangentToWorldAndParallax2.xyz));
		i_tangentToWorldAndParallax0.xyz/=tangentBasisScaled.x; // here we can normalize it
		i_tangentToWorldAndParallax1.xyz/=tangentBasisScaled.y;
		i_tangentToWorldAndParallax2.xyz/=tangentBasisScaled.z;
//		tangentBasisScaled=0.4;
	#else
		tangentBasisScaled=1; // not used
	#endif

	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		SclCurv = 0; // not used
		eyeVec = i_posWorld.xyz - _WorldSpaceCameraPos; // will be normalized in FRAGMENT_SETUP()
	#elif defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		eyeVec = i_posWorld.xyz - _WorldSpaceCameraPos; // will be normalized in FRAGMENT_SETUP()
		SclCurv = i_SclCurv;
	#else
		SclCurv = 0; // not used
		eyeVec = i_eyeVec;
	#endif

	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
		
			// TBN in world space
			worldNormal=normBlend; // world normal
			half3 _tangent=tri_mask.xxx*i_tangentToWorldAndParallax0.xyz + tri_mask.yyy*i_tangentToWorldAndParallax1.xyz + tri_mask.zzz*i_tangentToWorldAndParallax2.xyz;
			half3 _binormal=cross(worldNormal, _tangent);
			_TBN = half3x3(_tangent, _binormal, worldNormal);
			
			eyeVec = normalize(i_eyeVec);
			#if defined(_PARALLAX_MAP) || defined(_PARALLAX_MAP_2MAPS)
				i_viewDirForParallax=mul(_TBN, eyeVec);
				i_viewDirForParallax.z=-i_viewDirForParallax.z;
			#else
				i_viewDirForParallax=half3(0,0,0);
			#endif
			
		#else
		
			half3 _normal=normObj;
			half3 _tangent=tri_mask.xxx*i_tangentToWorldAndParallax0.xyz + tri_mask.yyy*i_tangentToWorldAndParallax1.xyz + tri_mask.zzz*i_tangentToWorldAndParallax2.xyz;
			half3 _binormal=cross(_normal, _tangent);
			
			#if defined(_PARALLAX_MAP) || defined(_PARALLAX_MAP_2MAPS)
				// TBN in object space
				half3x3 rotation=half3x3(_tangent, -_binormal, _normal);
				i_viewDirForParallax=normalize( mul(rotation, ObjSpaceViewDir(float4(i_posObject,1)) ) );
			#else
				i_viewDirForParallax=half3(0,0,0);
			#endif
			// TBN in world space
			_normal = UnityObjectToWorldNormal(_normal);
			_binormal = UnityObjectToWorldNormal(_binormal);
			_tangent = cross(_normal, _binormal); // basis is orthonormalized
			
			_TBN = half3x3(_tangent, _binormal, _normal);
			worldNormal=_normal;
			
		#endif	
	#else
		#if defined(_PARALLAX_MAP) || defined(_PARALLAX_MAP_2MAPS) || defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
			i_viewDirForParallax=normalize( half3(i_tangentToWorldAndParallax0.w, i_tangentToWorldAndParallax1.w, i_tangentToWorldAndParallax2.w));
		#else
			i_viewDirForParallax=half3(0,0,0);
		#endif	
		_TBN=ExtractTangentToWorldPerPixel(i_tangentToWorldAndParallax0, i_tangentToWorldAndParallax1, i_tangentToWorldAndParallax2);
		worldNormal=i_tangentToWorldAndParallax2.xyz;
	#endif
}

inline FragmentCommonData SpecularSetup (float4 i_tex, fixed4 vertex_color, float2 _ddx, float2 _ddy, float2 _ddxDet, float2 _ddyDet, half Wetness, half blendFade) // UBER (params added)
{
	half4 specGloss = SpecularGloss(i_tex, vertex_color, _ddx, _ddy, _ddxDet, _ddyDet, Wetness); // UBER - pass 4 components (zw for detail tiling)
	half3 specColor = specGloss.rgb;
	half oneMinusRoughness = specGloss.a;

	half oneMinusReflectivity;

	half3 additionalEmission=0;
	half3 pureAlbedo=Albedo(i_tex, vertex_color, Wetness, _ddx, _ddy, _ddxDet, _ddyDet, /* inout */ additionalEmission, blendFade);
	half3 diffColor = EnergyConservationBetweenDiffuseAndSpecular (pureAlbedo, specColor, /*out*/ oneMinusReflectivity);
	
	FragmentCommonData o = (FragmentCommonData)0;
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.pureAlbedo = pureAlbedo;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.oneMinusRoughness = oneMinusRoughness;
	o.additionalEmission = additionalEmission;
	return o;
}

inline FragmentCommonData MetallicSetup (float4 i_tex, fixed4 vertex_color, float2 _ddx, float2 _ddy, float2 _ddxDet, float2 _ddyDet, half Wetness, half blendFade) // UBER - vertex_color, derivatives
{
	half2 metallicGloss = MetallicGloss(i_tex, vertex_color, _ddx, _ddy, _ddxDet, _ddyDet, Wetness); // UBER - pass 4 components (zw for detail tiling)
	half metallic = metallicGloss.x;
	half oneMinusRoughness = metallicGloss.y;

	half oneMinusReflectivity;
	half3 specColor;

	half3 additionalEmission=0;
	half3 pureAlbedo=Albedo(i_tex, vertex_color, Wetness, _ddx, _ddy, _ddxDet, _ddyDet, /* inout */ additionalEmission, blendFade);
	half3 diffColor = DiffuseAndSpecularFromMetallic (pureAlbedo, metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	FragmentCommonData o = (FragmentCommonData)0;
	o.pureAlbedo = pureAlbedo;
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.oneMinusRoughness = oneMinusRoughness;
	o.additionalEmission = additionalEmission;
	return o;
} 

// UBER - Translucency
#if defined(_TRANSLUCENCY)
half3 Translucency(FragmentCommonData s, UnityLight light, half translucency_thickness, fixed4 vertex_color) {

	half4 TranslucencyColor=_TranslucencyColor;
	#if defined(_TWO_LAYERS)
		TranslucencyColor=lerp(_TranslucencyColor2, _TranslucencyColor, __VERTEX_COLOR_CHANNEL_LAYER);
	#endif

	#if defined(_SNOW)
		half4 translucencyColor=lerp(TranslucencyColor,_SnowTranslucencyColor, s.snowVal);
	#else
		half4 translucencyColor=TranslucencyColor;
	#endif
	#ifdef USING_DIRECTIONAL_LIGHT
		half tLitDot=saturate(dot( (light.dir + s.normalWorld*_TranslucencyNormalOffset), s.eyeVec) );
	#else
		float3 lightDirectional=normalize(_WorldSpaceLightPos0.xyz - _WorldSpaceCameraPos.xyz);
		light.dir=normalize(lerp(light.dir, lightDirectional, _TranslucencyPointLightDirectionality));
	 	half tLitDot=saturate( dot( (light.dir + s.normalWorld*_TranslucencyNormalOffset), s.eyeVec) );
	#endif
	
	tLitDot=exp2(-_TranslucencyExponent*(1-tLitDot))*_TranslucencyStrength;
	half3 pureAlbedo;
	#if defined(_SNOW)
		pureAlbedo=lerp(s.pureAlbedo.rgb, _SnowColorAndCoverage.rgb, s.snowVal);
	#else
		pureAlbedo=s.pureAlbedo.rgb;
	#endif
	half translucencyOcclusion = lerp( 1, translucency_thickness, _TranslucencyOcclusion );
	#if defined(TRANSLUCENCY_VERTEX_COLOR_CHANNEL)
		translucencyOcclusion*=vertex_color.TRANSLUCENCY_VERTEX_COLOR_CHANNEL;
	#endif
	half translucencyAtten = (tLitDot+_TranslucencyConstant)*translucencyOcclusion;
	#if defined(UBER_TRANSLUCENCY_PER_LIGHT_ALPHA)
	translucencyAtten*=_LightColor0.a;
	#endif
	
	return translucencyAtten * pureAlbedo.rgb * translucencyColor.rgb;
}
#endif

// UBER - Glitter
#if defined(_GLITTER) 
void Glitter(inout FragmentCommonData s, float2 _uv, float2 _ddxDet, float2 _ddyDet, float3 posWorld, fixed4 vertex_color, half glitter_thickness) {
	float2 glitterUV_Offset = (_WorldSpaceCameraPos.xz+posWorld.zx+_WorldSpaceCameraPos.yy+posWorld.yy)*GLITTER_ANIMATION_FREQUENCY*_GlitterTiling;
	float MIP_filterVal = _GlitterTiling*exp2(_GlitterFilter);
	float2 _ddxDetBias=_ddxDet*MIP_filterVal;
	float2 _ddyDetBias=_ddyDet*MIP_filterVal;
	half sparkle=tex2Dgrad(_SparkleMap, _uv*_GlitterTiling + glitterUV_Offset, _ddxDetBias, _ddyDetBias).r;
	half sparkle2=tex2Dgrad(_SparkleMap, _uv*_GlitterTiling - glitterUV_Offset, _ddxDetBias, _ddyDetBias).r;
	sparkle*=lerp(sparkle, 1, _GlitterDensity);
	half sparkleDenseVal=sparkle*sparkle2;// depends on density of sparkle mask
	sparkle2*=lerp(sparkle2, 1, _GlitterDensity);
	
	half3 _color=lerp( half3(0.9,0.9,0.9), abs(frac(s.normalWorld*4)*2-1), _GlitterColorization) + half3(0.5,0.5,0.5);
	_color*=_color;
	
	half sparkleStrengh=sparkle2*sparkle*GLITTER_AMPLIFY; // GLITTER_AMPLIFY defined in UBER_StandardConfig.cginc
	#if defined(_SNOW)
		sparkleStrengh*=s.dissolveMaskValue;
	#endif
	#if defined(VERTEX_COLOR_CHANNEL_GLITTER)
		sparkleStrengh*=vertex_color.VERTEX_COLOR_CHANNEL_GLITTER;
	#endif
	
	_color*=sparkleStrengh*glitter_thickness;
	
	half4 GlitterColor=_GlitterColor;
	#if defined(_TWO_LAYERS)
		GlitterColor=lerp(_GlitterColor2, GlitterColor, __VERTEX_COLOR_CHANNEL_LAYER);	
	#endif
	#if defined(_SNOW)
		half4 SnowGlitterColor = _SnowGlitterColorFromGlobal ? _UBER_GlobalSnowGlitterColor : _SnowGlitterColor;
		_color*=lerp(GlitterColor.rgb, SnowGlitterColor.rgb, s.snowVal);
		half GlitterSmoothnessStrength=lerp(GlitterColor.a, SnowGlitterColor.a, s.snowVal);
	#else
		_color*=GlitterColor.rgb;
		half GlitterSmoothnessStrength=GlitterColor.a;
	#endif
	
	#if defined(VERTEX_COLOR_CHANNEL_GLITTER)
		GlitterSmoothnessStrength*=vertex_color.VERTEX_COLOR_CHANNEL_GLITTER;
	#endif
	
	GlitterSmoothnessStrength*=glitter_thickness;
		
	s.oneMinusRoughness+=min(GLITTER_SMOOTHNESS_GAIN_LIMIT, sparkleDenseVal*GLITTER_AMPLIFY*GlitterSmoothnessStrength); // GLITTER_SMOOTHNESS_GAIN_LIMIT defined in UBER_StandardConfig.cginc
	s.oneMinusRoughness=saturate(s.oneMinusRoughness);
	s.specColor+=_color;
}
#endif

// UBER: inout added (parallaxed i_tex is not propagated outside FragmentSetup function thus emission/occlusion calculated was not parallaxed
inline FragmentCommonData FragmentSetup (inout float4 i_tex, half3 i_eyeVec, half3 i_normalWorld, inout half3 i_viewDirForParallax, inout half3x3 i_tanToWorld, half3 i_posWorld, inout fixed4 vertex_color, float2 _ddx, float2 _ddy, float2 _ddxDet, float2 _ddyDet, float3 tangentBasisScaled, float4 SclCurv, float blendFade, half actH) // UBER - additional params added
{
	float4 rayPos=0; // rayPos from POM parallax (the place we hit the surface in tangent space)
	float2 texture2ObjectRatio=0; // computed in Parallax() we need it for self-shadowing too
	float rayLength=0;
	
	// UBER - snow level - set in NormalInTangentSpace() (might be compiled out when not used)
	// (compiled out when not used)
	half _snow_val = 0;
	float _snow_val_nobump = 0;
	half dissolveMaskValue=0;
	#if defined(_SNOW)
		_snow_val = _SnowColorAndCoverage.a*__VERTEX_COLOR_CHANNEL_SNOW;
		_snow_val *= saturate((i_posWorld.y-_SnowHeightThreshold)/_SnowHeightThresholdTransition);
		_snow_val_nobump = saturate( _snow_val - (1-i_normalWorld.y)*_SnowSlopeDamp );
		
		half snowMaskLargeScale=tex2D(_RippleMap, i_posWorld.xz*SNOW_LARGE_MASK_TILING).g*0.3;
		_snow_val_nobump -= lerp(snowMaskLargeScale, 0, _snow_val_nobump);
		_snow_val_nobump = saturate(_snow_val_nobump);
		half _snow_val_nobump_per_material=_snow_val_nobump; // used later for wet coverage with snow (melting snow)
		_snow_val_nobump *= _SnowLevelFromGlobal ? (1-_UBER_GlobalSnowDamp) : 1;
		_snow_val=_snow_val_nobump;
	#endif		
	
	#if defined(_WETNESS_RIPPLES) || defined(_WETNESS_DROPLETS) || defined(_WETNESS_FULL)
		float2 i_tex_wet=i_posWorld.xz;
		//float3 viewDir=normalize(i_posWorld-_WorldSpaceCameraPos);
		//i_tex_wet-=(1-_WetnessLevel/1.25)*viewDir.xz/viewDir.y; // tutaj b. trudno przeliczyć z world do tangent aby mnoznik po lewej sie zgadzal
		float2 wetUV=i_tex_wet.xy;
		float2 wetDDX=ddx(wetUV);
		float2 wetDDY=ddy(wetUV);
	#endif

	#if defined(DISTANCE_MAP)
		half3 _norm=float3(0,0,1); // will be set in ParallaxPOMDistance() function
		i_tex = ParallaxPOMDistance(i_tex, i_viewDirForParallax, i_posWorld, vertex_color, _ddx, _ddy, _snow_val_nobump, /* inout */ actH, /* inout */ rayPos, /* inout */ texture2ObjectRatio, /* inout */ rayLength, tangentBasisScaled, SclCurv, /* inout */ _norm);
	#elif defined(EXTRUSION_MAP)
		half3 _norm=float3(0,0,1); // will be set in ParallaxPOMExtrusion() function
		i_tex = ParallaxPOMExtrusion(i_tex, i_viewDirForParallax, i_posWorld, vertex_color, _ddx, _ddy, _snow_val_nobump, /* inout */ actH, /* inout */ rayPos, /* inout */ texture2ObjectRatio, /* inout */ rayLength, tangentBasisScaled, SclCurv, /* inout */ _norm);
	#else
		// (i_tanToWorld can be modified by silhouette tracing)
		// NOTE: i_viewDirForParallax is object space view dir here when SILHOUETTE_CURVATURE_MAPPED is defined, will be put into tan space inside parallax function
		i_tex = Parallax(i_tex, /* inout */ i_viewDirForParallax, /* inout */ i_tanToWorld, i_posWorld, /* inout */ vertex_color, _ddx, _ddy, _snow_val_nobump, /* inout */ actH, /* inout */ rayPos, /* inout */ texture2ObjectRatio, /* inout */ rayLength, tangentBasisScaled, SclCurv, blendFade); // UBER - i_tanToWorld, i_posWorld, vertex_color, ddx, ddy, actH, ... added
	#endif
	
	// UBER
	#if defined(_SNOW)
		// needed later in case of snow covering wet surface
		float2 uvDet_no_refr=i_tex.zw;
	#else
		float2 uvDet_no_refr=0;
	#endif
	
	// wet ripples normalmap
	#if defined(_WETNESS)
		float3 wetNorm=float3(0,0,1); 
		half rippleMIPsel = 0;
		half Wetness = 0;
		half WetnessConst = 0;
		half deepWetFct = 0;
		half wetMask = 0;
		#if _DETAIL || defined(_DETAIL_SIMPLE)		
			#if defined(_TWO_LAYERS)
				//wetMask=0; // no detail mask available
			#else
				wetMask=0.1-tex2Dp(_DetailMask, i_tex.xy*_WetnessUVMult, _ddx*_WetnessUVMult, _ddy*_WetnessUVMult).r*0.1;
			#endif
		#endif
		#if defined(_SNOW)
			half additionalWetDamp=_WetnessMergeWithSnowPerMaterial ? _snow_val_nobump_per_material : 1;
			wetMask=wetMask*0.7+snowMaskLargeScale*0.3;
		#else
			half additionalWetDamp=1;
		#endif
		Wetness=saturate( (__VERTEX_COLOR_CHANNEL_WETNESS*additionalWetDamp*_WetnessLevel*(_WetnessLevelFromGlobal ? (1-_UBER_GlobalDry) : 1)-actH-wetMask) * 4 );
		WetnessConst=saturate( (__VERTEX_COLOR_CHANNEL_WETNESS*additionalWetDamp*_WetnessConst*(_WetnessConstFromGlobal ? (1-_UBER_GlobalDryConst) : 1)+0.2-wetMask*2) ); 

		
		#if defined(_SNOW)
			// snow override wetness
			// (we skip it - let user controll it via vertex colors or global controller)
			//Wetness*=saturate(1-_snow_val*2);
			half refrSnowDamp=1;//saturate(1-_snow_val*8);
		#endif	
		deepWetFct=saturate((Wetness-0.5)*2);	
		
		UBER_Time = _WetnessFlowGlobalTime ? UBER_Time : _Time;
		
		#if defined(_WETNESS_RIPPLES) || defined(_WETNESS_FULL)
		half RippleStrength = _RippleStrengthFromGlobal ? _UBER_RippleStrength*_RippleStrength : _RippleStrength;
		{
			float2 rippleUV=wetUV*_RippleTiling;
			float2 rippleDDX=wetDDX*_RippleTiling;
			float2 rippleDDY=wetDDY*_RippleTiling;
			
			float animSpeed=_RippleAnimSpeed;
			
			#if !defined(WET_FLOW)
				wetNorm = UnpackScaleNormal(tex2Dgrad(_RippleMap, rippleUV+UBER_Time.y*animSpeed, rippleDDX, rippleDDY) , RippleStrength*__VERTEX_COLOR_CHANNEL_WETNESS_RIPPLES);
				wetNorm = 0.5*wetNorm+0.5*UnpackScaleNormal(tex2Dgrad(_RippleMap, rippleUV-UBER_Time.y*animSpeed, rippleDDX, rippleDDY) , RippleStrength*__VERTEX_COLOR_CHANNEL_WETNESS_RIPPLES);
			#else
				float _Tim=frac(UBER_Time.x*_FlowCycleScale)*2;
				float ft=abs(frac(_Tim)*2 - 1);
				
				#ifdef WATER_FLOW_DIRECTION_FROM_NORMALMAPS
					#if defined(_TWO_LAYERS)
						half FlowNormStrength = _WetnessNormStrength*saturate(1-i_normalWorld.y*0.5)*saturate(actH*2.5-_WetnessLevel);
						half3 mainBumpsInTangentSpace =  UnpackScaleNormal( tex2Dlod(_BumpMap, float4(i_tex.xy,_WetnessNormMIP.xx)) , FlowNormStrength ) ;
						half3 mainBumpsInTangentSpace2 =  UnpackScaleNormal( tex2Dlod(_BumpMap, float4(i_tex.xy,_WetnessNormMIP.xx)) , FlowNormStrength ) ;
						mainBumpsInTangentSpace = lerp( mainBumpsInTangentSpace2, mainBumpsInTangentSpace, __VERTEX_COLOR_CHANNEL_LAYER );
						mainBumpsInTangentSpace = normalize(mainBumpsInTangentSpace);
					#else
						half3 mainBumpsInTangentSpace = normalize( UnpackScaleNormal( tex2Dlod(_BumpMap, float4(i_tex.xy,_WetnessNormMIP.xx)) , _WetnessNormStrength*saturate(1-i_normalWorld.y*0.5)*saturate(actH*2.5-_WetnessLevel) ) );
					#endif
					float2 slopeXZ = mul(mainBumpsInTangentSpace, i_tanToWorld).xz;
				#else
					float2 slopeXZ = i_normalWorld.xz;
				#endif
				
				float2 flowSpeed=clamp((i_normalWorld.y>0 ? -4:4) * slopeXZ+0.04,-1,1)/_FlowCycleScale;
				flowSpeed*=animSpeed*_RippleTiling;
				
				wetNorm = UnpackScaleNormal(tex2Dgrad(_RippleMap, rippleUV+frac(_Tim.xx)*flowSpeed, rippleDDX, rippleDDY) , RippleStrength*__VERTEX_COLOR_CHANNEL_WETNESS_FLOW);
				wetNorm = lerp(wetNorm, UnpackScaleNormal(tex2Dgrad(_RippleMap, rippleUV+frac(_Tim.xx+0.5)*flowSpeed*1.25, rippleDDX, rippleDDY) , RippleStrength*__VERTEX_COLOR_CHANNEL_WETNESS_FLOW), ft);
				wetNorm.xy*=abs(i_normalWorld.y);//1-exp2(-12*abs(i_normalWorld.y));
				//wetNorm=normalize(wetNorm);
			#endif
			#if defined(_SNOW)
//				wetNorm.xy*=refrSnowDamp;
			#endif
						
			rippleDDX*=_RippleMap_TexelSize.zw*16;
			rippleDDY*=_RippleMap_TexelSize.zw*16;
			float rippleD = max( dot( rippleDDX, rippleDDX ), dot( rippleDDY, rippleDDY ) );
			rippleMIPsel = max(0, log2(rippleD)); // uzyte ponizej do filtrowania IBL
			// additional grazing angle filtering
			half _Fresnel=exp2(-8.65*i_viewDirForParallax.z); // (1-x)^5 aprrox.
			rippleMIPsel += _Fresnel*10;
			
			float rippleReduceRef=saturate(1-(rippleMIPsel*_WetnessSpecGloss.a*RippleStrength*_RippleSpecFilter)*0.5);
			float2 ripple_refraction_offset=rippleReduceRef*_RippleRefraction*wetNorm.xy*0.05*deepWetFct;
			 
			i_tex.xy+=ripple_refraction_offset;
			i_tex.zw+=ripple_refraction_offset*_DetailAlbedoMap_ST.xy/_MainTex_ST.xy;
		}
		#endif

		#if defined(_WETNESS_DROPLETS) || defined(_WETNESS_FULL)
		float2 droplets_refraction_offset;	
		{
			half RainIntensity=_RainIntensityFromGlobal ? (1-_UBER_GlobalRainDamp)*_RainIntensity : _RainIntensity;
		
			float2 rippleUV=wetUV*_DropletsTiling;
			float2 rippleDDX=wetDDX*_DropletsTiling;
			float2 rippleDDY=wetDDY*_DropletsTiling;
		
	  	 	fixed4 Ripple = tex2Dp(_DropletsMap, rippleUV, rippleDDX, rippleDDY);
		    Ripple.xy = Ripple.xy * 2 - 1;
		
		    float DropFrac = frac(Ripple.w + _Time.x*_DropletsAnimSpeed);
		    float TimeFrac = DropFrac - 1.0f + Ripple.z;
		    float DropFactor = saturate(RainIntensity - DropFrac);
		    float FinalFactor = DropFactor * Ripple.z * sin( clamp(TimeFrac * 9.0f, 0.0f, 3.0f) * 3.1415);
		  	float2 droplets_refraction_offset = Ripple.xy * FinalFactor;
		  	
		  	rippleUV+=float2(0.25,0.25);
	  	 	Ripple = tex2Dp(_DropletsMap, rippleUV, rippleDDX, rippleDDY);
		    Ripple.xy = Ripple.xy * 2 - 1;
		
		    DropFrac = frac(Ripple.w + _Time.x*_DropletsAnimSpeed);
		    TimeFrac = DropFrac - 1.0f + Ripple.z;
		    DropFactor = saturate(RainIntensity - DropFrac);
		    FinalFactor = DropFactor * Ripple.z * sin( clamp(TimeFrac * 9.0f, 0.0f, 3.0f) * 3.1415);
		  	droplets_refraction_offset += Ripple.xy * FinalFactor;
		  	
		  	droplets_refraction_offset*=__VERTEX_COLOR_CHANNEL_WETNESS_DROPLETS*(deepWetFct*0.9+WetnessConst*0.1) * 0.1f * abs(i_normalWorld.y);
		  	wetNorm.xy+=droplets_refraction_offset*10;
		  	//wetNorm=normalize(wetNorm);
		  	droplets_refraction_offset*=_RippleRefraction;
		  	
			#if defined(_SNOW)
				droplets_refraction_offset*=refrSnowDamp;
			#endif
			i_tex.xy+=droplets_refraction_offset;
			i_tex.zw+=droplets_refraction_offset*_DetailAlbedoMap_ST.xy/_MainTex_ST.xy;
		}
		#endif
		
	#else
		// wetness not used
		half Wetness = 0; // compiled out (not used, even if passed to functions)
		half deepWetFct = 0;
		half3 wetNorm = 0;
		half rippleMIPsel = 0;
	#endif	
			
	half3 eyeVec = i_eyeVec;
	#if defined(TRIPLANAR_SELECTIVE) && defined(_TRIPLANAR_WORLD_MAPPING)
		// already normalized
	#else
		{
		//eyeVec = normalize(eyeVec);
		eyeVec /= max(0.0000001, length(eyeVec)); // get rid of floating point div by zero warning (d3d9 still produces this warning though in U5.1.1)
		}
	#endif			
			
	//
	#if _NORMALMAP
		// _snow_val_nobump, i_tanToWorld - needed for snow in NormalInTangentSpace()
		#if defined(_SNOW) && ENABLE_SNOW_WORLD_MAPPING
			#if defined(RAYLENGTH_AVAILABLE)
				float2 snowUV=rayLength*eyeVec.xz + i_posWorld.xz;
			#else
				float2 snowUV=i_posWorld.xz;
			#endif
			float2 _ddxSnow=ddx(snowUV);
			float2 _ddySnow=ddy(snowUV);
			if (_SnowWorldMapping) {
				uvDet_no_refr=snowUV;
			} else {
				_ddxSnow=_ddxDet;
				_ddySnow=_ddyDet;
			}
		#else
			float2 _ddxSnow=_ddxDet;
			float2 _ddySnow=_ddyDet;
		#endif
	 	#if defined(DISTANCE_MAP)
			half3 normalWorld = mul(BlendNormals(_norm, NormalInTangentSpace(i_tex, uvDet_no_refr, i_viewDirForParallax, _snow_val_nobump, i_tanToWorld, vertex_color, _ddx, _ddy, _ddxDet, _ddyDet, _ddxSnow, _ddySnow, wetNorm, deepWetFct, /* inout */ _snow_val, /* inout */ dissolveMaskValue, blendFade)), i_tanToWorld);
			#if defined(_SNOW)
				_snow_val = saturate( _snow_val - (1-normalWorld.y)*_SnowSlopeDamp );
			#endif
		#elif defined(EXTRUSION_MAP)
			// take main normal from _norm that has been set in specialised parallax function (we needed normal there for texturing sidewalls)
			half3 normalWorld = mul(NormalInTangentSpace(_norm, i_tex, uvDet_no_refr, i_viewDirForParallax, _snow_val_nobump, i_tanToWorld, vertex_color, _ddx, _ddy, _ddxDet, _ddyDet, _ddxSnow, _ddySnow, wetNorm, deepWetFct, /* inout */ _snow_val, /* inout */ dissolveMaskValue, blendFade), i_tanToWorld);
		#else
			#if defined(TRIPLANAR_SELECTIVE)
				half blendFadeWithNormalSharpness = lerp(blendFade, 1-exp2(-9*blendFade), _TriplanarNormalBlendSharpness);
				half3 normInTangentSpace=NormalInTangentSpace(i_tex, uvDet_no_refr, i_viewDirForParallax, _snow_val_nobump, i_tanToWorld, vertex_color, _ddx, _ddy, _ddxDet, _ddyDet, _ddxSnow, _ddySnow, wetNorm, deepWetFct, /* inout */ _snow_val, /* inout */ dissolveMaskValue, blendFade);
				#if defined(_SNOW) 
					// we don't interpolate normals towards (0,0,1) in case of snow coverage in world space - simply we don't have seams there, so no "blendFade" process is needed out there
					blendFadeWithNormalSharpness = _SnowWorldMapping ? lerp(blendFadeWithNormalSharpness, 1, _snow_val) : blendFadeWithNormalSharpness;
				#endif
				normInTangentSpace = lerp(float3(0,0,1), normInTangentSpace, blendFadeWithNormalSharpness );
				half3 normalWorld = mul(normInTangentSpace, i_tanToWorld);
			#else
				half3 normalWorld = mul(NormalInTangentSpace(i_tex, uvDet_no_refr, i_viewDirForParallax, _snow_val_nobump, i_tanToWorld, vertex_color, _ddx, _ddy, _ddxDet, _ddyDet, _ddxSnow, _ddySnow, wetNorm, deepWetFct, /* inout */ _snow_val, /* inout */ dissolveMaskValue, blendFade), i_tanToWorld);
			#endif
		#endif
		normalWorld = normalize(normalWorld);
	#else
		// Should get compiled out, isn't being used in the end.
	 	half3 normalWorld = i_normalWorld;
	 	#if defined(DISTANCE_MAP)
	 		normalWorld=mul(_norm, i_tanToWorld);
	 	#endif
	#endif
	
	
	// alpha
	#if defined(GEOM_BLEND)
		// RTP's geom blend
		half alpha;
		#if defined(BLENDING_HEIGHT)
			float2 globalUV=i_posWorld.xz-_TERRAIN_PosSize.xy;
			globalUV/=_TERRAIN_PosSize.zw;	
			float2 aux=i_posWorld.xz-_TERRAIN_PosSize.xy+_TERRAIN_Tiling.zw;
			aux.xy/=_TERRAIN_Tiling.xy;
			
			float4 terrain_coverage=tex2D(_TERRAIN_Control, globalUV);
			float4 splat_control1=terrain_coverage * tex2D(_TERRAIN_HeightMap, aux.xy) * vertex_color.VERTEX_COLOR_CHANNEL_GEOM_BLEND;
			float4 splat_control2=float4( (actH+0.01) , 0, 0, 0) * (1-vertex_color.VERTEX_COLOR_CHANNEL_GEOM_BLEND);
			
			float blend_coverage=dot(terrain_coverage, 1);
			if (blend_coverage>0.1) {
			
				splat_control1*=splat_control1;
				splat_control1*=splat_control1;
				splat_control2*=splat_control2;
				splat_control2*=splat_control2;
				
				float normalize_sum=dot(splat_control1, float4(1,1,1,1))+dot(splat_control2, float4(1,1,1,1));
				splat_control1 /= normalize_sum;
				splat_control2 /= normalize_sum;		
				
				alpha=dot(splat_control2,1);
				alpha=lerp(1-vertex_color.VERTEX_COLOR_CHANNEL_GEOM_BLEND, alpha, saturate((blend_coverage-0.1)*4) );
			} else {
				alpha=1-vertex_color.VERTEX_COLOR_CHANNEL_GEOM_BLEND;
			}
		#else
			alpha=(1-vertex_color.VERTEX_COLOR_CHANNEL_GEOM_BLEND);
		#endif	
	#else
		// alpha computed regular way (in opposite to geom blend)
		half alpha = Alpha(i_tex, _ddx, _ddy, _ddxDet, _ddyDet, vertex_color); // UBER - ddx, ddy, vertex_color (2 layers)
		#if defined(_SNOW)
			alpha=lerp(alpha, 1, _snow_val);
		#endif	
		#if defined(_WETNESS)
			alpha=lerp(alpha, 1, Wetness*_WetnessColor.a);
		#endif	
	#endif
	
	
	#if defined(_ALPHATEST_ON)
		clip (alpha - _Cutoff);
	#endif
	
	FragmentCommonData o = UNITY_SETUP_BRDF_INPUT (i_tex, vertex_color, _ddx, _ddy, _ddxDet, _ddyDet, Wetness, blendFade); // UBER - params added (vertex colors, derivateves)
	o.normalWorld = normalWorld;
	o.eyeVec = eyeVec;
	o.posWorld = i_posWorld;
	
	#if defined(_ALPHATEST_ON)
		o.additionalEmission+=saturate(_Cutoff - alpha + _CutoffEdgeGlow.a-0.004)*10000*_CutoffEdgeGlow.rgb;
	#endif

//	o.diffColor*=blendFade;
	
	#if defined(_WETNESS)
		half Wetness_with_Const=saturate(Wetness+WetnessConst);
		// wetness specularity (wetness has always "specular" setup)
		o.specColor = lerp(o.specColor, lerp(max(o.specColor, _WetnessSpecGloss.rgb), _WetnessSpecGloss.rgb, _WetnessColor.a), Wetness);
		// wetness darkening (for non emissive water, non opaque water and rough underlying surface)
		o.diffColor *= saturate(1.3-saturate(Wetness_with_Const*_WetnessDarkening*(1-_WetnessEmissiveness)*(1-_WetnessColor.a)*(1-o.oneMinusRoughness)));
		// wetness gloss
		#if defined(_WETNESS_RIPPLES) || defined(_WETNESS_FULL)
			float wetGloss=saturate(_WetnessSpecGloss.a-rippleMIPsel*_WetnessSpecGloss.a*RippleStrength*_RippleSpecFilter);
		#else
			float wetGloss=_WetnessSpecGloss.a;
		#endif
		
		//wetMask=tex2D(_DetailMask, i_tex.xy*_WetnessUVMult*3).g;
		//wetGloss=0.9+wetMask*wetMask*0.3;
		//wetMask=0.1+tex2D(_DetailMask, i_tex.xy*_WetnessUVMult*5).g*0.5;
		//o.specColor=wetMask.xxx*wetMask.xxx;//lerp(o.specColor, wetMask, Wetness);//wetMask.xxx*o.specColor*10;
		//o.normalWorld=i_normalWorld;
		
		o.oneMinusRoughness = lerp(o.oneMinusRoughness, lerp(max(o.oneMinusRoughness,wetGloss), wetGloss, _WetnessColor.a), Wetness_with_Const); // filtrowanie po rippleMIPsel (usuwa artefakty hi-freq z ripli)
		// wetness emissiveness
		#if defined(_WETNESS_RIPPLES) || defined(_WETNESS_FULL)
			float norm_fluid_val=_WetnessEmissivenessWrap ? (saturate(dot(wetNorm.xy*4, wetNorm.xy*4))*4+0.1) : (wetNorm.x+wetNorm.y+1);
		#else
			float norm_fluid_val=1;
		#endif
		o.additionalEmission += norm_fluid_val*_WetnessColor.rgb*_WetnessEmissiveness*Wetness*deepWetFct; // (8x HDR on emission)
		o.Wetness=Wetness;
		
//		o.diffColor=0;
//		o.specColor=0;
//		o.oneMinusRoughness=0;
//		o.oneMinusReflectivity=1;
//		//Wetness=saturate( (__VERTEX_COLOR_CHANNEL_WETNESS*_WetnessLevel*(_WetnessLevelFromGlobal ? (1-_UBER_GlobalDry) : 1)-actH-wetMask) * 2 );
//		o.additionalEmission=saturate(actH*1.35-_WetnessLevel);//Wetness*(1-Wetness)*4;//saturate((Wetness*0.25-0.2))*4;	;//(-deepWetFct+Wetness)*deepWetFct*16;//saturate((_WetnessLevel-actH));
	#endif
	
//	#if defined(_SNOW)
//		o.diffColor=_snow_val_nobump_per_material;
//		o.oneMinusRoughness=0;
//		o.Wetness=0;
//		o.additionalEmission=0;
//		o.normalWorld=i_normalWorld;
//	#endif
	
	#if defined(_TWO_LAYERS)
		half4 DiffuseScatteringColor=lerp(_DiffuseScatteringColor2, _DiffuseScatteringColor, __VERTEX_COLOR_CHANNEL_LAYER);
	#else
		half4 DiffuseScatteringColor=_DiffuseScatteringColor;
	#endif
	#if defined(_SNOW)
		// conserve energy (snow has specular setup)
		half oneMinusReflectivitySnow;

		half4 SnowSpecGloss = _SnowSpecGlossFromGlobal ? _UBER_GlobalSnowSpecGloss : _SnowSpecGloss;
		half3 diffColorSnow = EnergyConservationBetweenDiffuseAndSpecular( _SnowColorAndCoverage.rgb, SnowSpecGloss.rgb, /* out */ oneMinusReflectivitySnow );

		// simple frost - constant color & diffuse scatter
		half Frost=_FrostFromGlobal ? (1-_UBER_Frost)*_Frost : _Frost;
		#if defined(_WETNESS)
			half frost=Frost*Wetness_with_Const;
		#else
			half frost=Frost;
		#endif
		o.diffColor = lerp(o.diffColor, diffColorSnow, frost*0.05);

		o.specColor = lerp(o.specColor, SnowSpecGloss.rgb, _snow_val);
		o.diffColor = lerp(o.diffColor, diffColorSnow, _snow_val);
		o.oneMinusRoughness = lerp(o.oneMinusRoughness, SnowSpecGloss.a, _snow_val);
		o.oneMinusReflectivity=lerp(o.oneMinusReflectivity, oneMinusReflectivitySnow, _snow_val);
		o.snowVal=_snow_val;
		o.dissolveMaskValue=dissolveMaskValue;

		// diffuse scatter
		#if !defined(UNITY_PASS_META)
		half _snow_val_with_frost=max(_snow_val, frost*0.5);
		if (_DiffuseScatter) {
		// uniform branching - close to free on modern hardware (anyway - that's what experts like Aras say)
		//#if defined(_DIFFUSE_SCATTER)
			DiffuseScatteringColor=lerp(DiffuseScatteringColor, _SnowDiffuseScatteringColor, _snow_val_with_frost);
			half scatterNdotV=(dot(normalWorld, eyeVec)+lerp(_DiffuseScatteringOffset, _SnowDiffuseScatteringOffset, _snow_val_with_frost));
			half scatter=exp2(-scatterNdotV*scatterNdotV*lerp(_DiffuseScatteringExponent, _SnowDiffuseScatteringExponent, _snow_val_with_frost));
			scatter*=scatter;
			o.diffColor+=lerp(o.diffColor*DiffuseScatteringColor.rgb, DiffuseScatteringColor.rgb, DiffuseScatteringColor.a)*scatter*4;
		//#else
		} else {
			// currently not used part of code (_DIFFUSE_SCATTER is not a feature)
			DiffuseScatteringColor=_SnowDiffuseScatteringColor*_snow_val_with_frost;
			half scatterNdotV=(dot(normalWorld, eyeVec)+_SnowDiffuseScatteringOffset);
			half scatter=exp2(-scatterNdotV*scatterNdotV*_SnowDiffuseScatteringExponent);
			scatter*=scatter;
			o.diffColor+=lerp(o.diffColor*DiffuseScatteringColor.rgb, DiffuseScatteringColor.rgb, DiffuseScatteringColor.a)*scatter*4;
		}
		//#endif
		#endif
	#else
		// diffuse scatter
		#if !defined(UNITY_PASS_META)
		if (_DiffuseScatter) {
		// uniform branching - close to free on modern hardware (anyway - that's what experts like Aras say)
		//#if defined(_DIFFUSE_SCATTER)
			half scatterNdotV=(dot(normalWorld, eyeVec)+_DiffuseScatteringOffset);
			half scatter=exp2(-scatterNdotV*scatterNdotV*_DiffuseScatteringExponent);
			scatter*=scatter;
			o.diffColor+=lerp(o.diffColor*DiffuseScatteringColor.rgb, DiffuseScatteringColor.rgb, DiffuseScatteringColor.a)*scatter*4;
		}
		//#endif
		#endif
	#endif	
	
	// UBER - SS
	#if defined(_PARALLAX_POM_SHADOWS) || defined(_POM_DISTANCE_MAP_SHADOWS) || defined(_POM_EXTRUSION_MAP_SHADOWS)
	o.rayPos = rayPos;
	o.tanToWorld=i_tanToWorld;
	#endif
	o.texture2ObjectRatio = texture2ObjectRatio;
	#if defined(ZWRITE)
	o.rayLength=rayLength;
	#endif
	
	// NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	o.diffColor = PreMultiplyAlpha (o.diffColor, alpha, o.oneMinusReflectivity, /*out*/ o.alpha);
	
	return o;
}

inline UnityGI FragmentGI (
	float3 posWorld, 
	half occlusion, half4 i_ambientOrLightmapUV, half atten, half oneMinusRoughness, half3 normalWorld, half3 eyeVec,
	UnityLight light)
{
	UnityGIInput d;
	d.light = light;
	d.worldPos = posWorld;
	d.worldViewDir = -eyeVec;
	d.atten = atten;
	#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
		d.ambient = 0;
		d.lightmapUV = i_ambientOrLightmapUV;
	#else
		d.ambient = i_ambientOrLightmapUV.rgb;
		d.lightmapUV = 0;
	#endif
	d.boxMax[0] = unity_SpecCube0_BoxMax;
	d.boxMin[0] = unity_SpecCube0_BoxMin;
	d.probePosition[0] = unity_SpecCube0_ProbePosition;
	d.probeHDR[0] = unity_SpecCube0_HDR;

	d.boxMax[1] = unity_SpecCube1_BoxMax;
	d.boxMin[1] = unity_SpecCube1_BoxMin;
	d.probePosition[1] = unity_SpecCube1_ProbePosition;
	d.probeHDR[1] = unity_SpecCube1_HDR;

	return UnityGlobalIllumination (
		d, occlusion, oneMinusRoughness, normalWorld);
}


//-------------------------------------------------------------------------------------
half4 OutputForward (half4 output, half alphaFromSurface)
{
	#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
		output.a = alphaFromSurface;
	#else
		UNITY_OPAQUE_ALPHA(output.a);
	#endif
	return output;
}

// ------------------------------------------------------------------
//  Base forward pass (directional light, emission, lightmaps, ...)

struct VertexOutputForwardBase
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0; // normal in triplanar (.w means UV1 - u coord for triplanar - needed for secondary occlusion)
	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		float4 posObject				: TEXCOORD1; // .w means UV1 - v coord for triplanar (needed for secondary occlusion)
	#elif defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		float4 SclCurv					: TEXCOORD1;
	#else
		float4 eyeVec 					: TEXCOORD1; // .w means UV1 - v coord for triplanar (needed for secondary occlusion)
	#endif
	half4 tangentToWorldAndParallax0	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax] - note: tangents+obj scale in triplanar (tangents in world space when mapping in world space)
	half4 tangentToWorldAndParallax1	: TEXCOORD3;	// (array fails in GLSL optimizer)
	half4 tangentToWorldAndParallax2	: TEXCOORD4;
	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UV
	fixed4 vertex_color					: COLOR0;		// UBER
	SHADOW_COORDS(6)
	UNITY_FOG_COORDS(7)

	// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
	#if UNITY_SPECCUBE_BOX_PROJECTION || defined(POM) || defined(_TRANSLUCENCY) || defined(_GLITTER) || defined(_SNOW) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP) || defined(TRIPLANAR_SELECTIVE)
		#if defined(ZWRITE)
		float4 posWorld					: TEXCOORD8;
		#else
		float3 posWorld					: TEXCOORD8;
		#endif
	#endif
	#if defined(_REFRACTION)
		float4 screenPos				: TEXCOORD9;
	#endif
};

VertexOutputForwardBase vertForwardBase (VertexInput v)
{
	VertexOutputForwardBase o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase, o);
	
	#if defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		float2 Curv=frac(v.uv3);
		float2 Scl=(v.uv3-Curv)/100; // scale represented with 0.01 resolution (fair enough)
		Scl=Scl*_Tan2ObjectMultOffset.xy+_Tan2ObjectMultOffset.zw;
		//Scl=10;
		#if defined(VERTEX_COLOR_CHANNEL_POMZ)
			v.vertex.xyz+=_POM_ExtrudeVolume ? v.normal.xyz*v.color.VERTEX_COLOR_CHANNEL_POMZ*_Depth*max(Scl.x, Scl.y)/max(_MainTex_ST.x, _MainTex_ST.y) : float3(0,0,0);
			// Curv.x==0 - extruded bottom flag
			v.color.VERTEX_COLOR_CHANNEL_POMZ = Curv.x==0 || (!_POM_ExtrudeVolume) ? v.color.VERTEX_COLOR_CHANNEL_POMZ : 1-v.color.VERTEX_COLOR_CHANNEL_POMZ;
			//Curv=0; // no curvature on extruded volumes (we need bottom flag info in parallax function though - so DON'T zero Curv here !)
			// if we don't handle the volume set the curvature data to desired range
			Curv = _POM_ExtrudeVolume ? Curv : Curv*20-10;
		#else
			Curv=Curv*20-10; // Curv=(Curv-0.5)*10; // we assume curvature won't be higher than +/- 10
		#endif
	#endif

	float4 posWorld = mul(_Object2World, v.vertex);

	#if UNITY_SPECCUBE_BOX_PROJECTION || defined(POM) || defined(_TRANSLUCENCY) || defined(_GLITTER) || defined(_SNOW) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP) || defined(TRIPLANAR_SELECTIVE)
		o.posWorld.xyz = posWorld.xyz;
		#if defined(ZWRITE)
			COMPUTE_EYEDEPTH(o.posWorld.w);
		#endif
	#endif

	o.vertex_color = v.color; // UBER
	o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		//o.posObject set below
	#elif defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		o.SclCurv=float4(float2(1.0,1.0)/Scl, Curv);
	#else
		o.eyeVec.xyz = posWorld.xyz - _WorldSpaceCameraPos;
		#if defined(TRIPLANAR_SELECTIVE)
			// world mapping
			o.eyeVec.w = v.uv1.y;
		#endif
	#endif

	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			float3 normalWorld = UnityObjectToWorldNormal(v.normal);
			SetupUBER_VertexData_TriplanarWorld(normalWorld, /* inout */ o.tangentToWorldAndParallax0, /* inout */ o.tangentToWorldAndParallax1, /* inout */ o.tangentToWorldAndParallax2);
		#else
			float scaleX, scaleY, scaleZ;
			SetupUBER_VertexData_TriplanarLocal(v.normal, /* inout */ o.tangentToWorldAndParallax0, /* inout */ o.tangentToWorldAndParallax1, /* inout */ o.tangentToWorldAndParallax2, /* out */ scaleX, /* out */ scaleY, /* out */ scaleZ);
			float3 normalWorld = UnityObjectToWorldNormal(v.normal);
			o.posObject.xyz = v.vertex.xyz;
			o.posObject.w = v.uv1.y; // pack it here
		#endif		
	#elif defined(_TANGENT_TO_WORLD)
		half3x3 tangentToWorld;

		// we need to go from tangent to world space for zwrite and parallaxed snow (actually when snow is mapped in worldspace)
		#if defined(RAYLENGTH_AVAILABLE)
			float3 normalWorld = mul((float3x3)_Object2World, v.normal.xyz);
			float3 tangentWorld = mul((float3x3)_Object2World, v.tangent.xyz);
			float3 binormalWorld = mul((float3x3)_Object2World, cross(v.normal.xyz, v.tangent.xyz)*v.tangent.w);
			#ifdef SHADER_TARGET_GLSL
			binormalWorld*=0.9999; // dummy op to cheat HLSL2GLSL optimizer to not be so smart (and buggy) here... It probably tries to make some fancy matrix by matrix calculation
			#endif
			// not normalized basis (we need it for texture 2 worldspace ratio calculations)
			tangentToWorld=half3x3(tangentWorld, binormalWorld, normalWorld);
			normalWorld = normalize(normalWorld); // we need it below for lighting
		#else
			float3 normalWorld = UnityObjectToWorldNormal(v.normal);
			float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
			tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, v.tangent.w);
		#endif

		o.tangentToWorldAndParallax0.xyz = tangentToWorld[0];
		o.tangentToWorldAndParallax1.xyz = tangentToWorld[1];
		o.tangentToWorldAndParallax2.xyz = tangentToWorld[2];
	#else
		float3 normalWorld = UnityObjectToWorldNormal(v.normal);
		o.tangentToWorldAndParallax0.xyz = 0;
		o.tangentToWorldAndParallax1.xyz = 0;
		o.tangentToWorldAndParallax2.xyz = normalWorld;
	#endif
	//We need this for shadow receving
	TRANSFER_SHADOW(o);

	// Static lightmaps
	#ifndef LIGHTMAP_OFF
		o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
		o.ambientOrLightmapUV.zw = 0;
	// Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
	#elif UNITY_SHOULD_SAMPLE_SH
		#if UNITY_SAMPLE_FULL_SH_PER_PIXEL
			o.ambientOrLightmapUV.rgb = 0;
		#elif (SHADER_TARGET < 30)
			o.ambientOrLightmapUV.rgb = ShadeSH9(half4(normalWorld, 1.0));
		#else
			// Optimization: L2 per-vertex, L0..L1 per-pixel
			o.ambientOrLightmapUV.rgb = ShadeSH3Order(half4(normalWorld, 1.0));
		#endif
		// Add approximated illumination from non-important point lights
		#ifdef VERTEXLIGHT_ON
			o.ambientOrLightmapUV.rgb += Shade4PointLights (
				unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
				unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
				unity_4LightAtten0, posWorld.xyz, normalWorld);
		#endif
	#endif

	#ifdef DYNAMICLIGHTMAP_ON
		o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif

	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			// .w component not used
		#else
			o.tangentToWorldAndParallax0.w=scaleX;
			o.tangentToWorldAndParallax1.w=scaleY;
			o.tangentToWorldAndParallax2.w=scaleZ;
		#endif
	#elif defined(_PARALLAX_MAP) || defined(_PARALLAX_MAP_2MAPS) || defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		#if ((defined(POM)) && defined(SILHOUETTE_CURVATURE_MAPPED))
			half3 viewDirForParallax = ObjSpaceViewDir(v.vertex);
		#else
			// vertex normal, tangent are not guaranteed to be normalized (!)
			// try - 2 simple planes on the scene using the same material, anchored and parent has decreased scale, Unity makes kind of batch (vertices seems to be transformed to world space) ? Anyway mesh tangents, normals get scaled, too and makes total mess with TBN matrices (view direction...)
			v.normal=normalize(v.normal);
			v.tangent.xyz=normalize(v.tangent.xyz);
			float3 binormal = cross( v.normal, v.tangent.xyz ) * v.tangent.w;
			float3x3 rotation = float3x3( v.tangent.xyz, binormal, v.normal );
			half3 viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
		#endif
		o.tangentToWorldAndParallax0.w = viewDirForParallax.x;
		o.tangentToWorldAndParallax1.w = viewDirForParallax.y;
		o.tangentToWorldAndParallax2.w = viewDirForParallax.z;
	#endif
	#if defined(_REFRACTION)
		o.screenPos = ComputeScreenPos (o.pos);
	#endif
	
	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			o.tex = float4(normalWorld, v.uv1.x); // pack UV1 here
		#else
			o.tex = float4(v.normal, v.uv1.x); // pack UV1 here
		#endif
	#else
		o.tex = TexCoordsNoTransform(v);
	#endif
	
	UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}

void fragForwardBase (VertexOutputForwardBase i, out half4 outCol : SV_Target
#if defined(ZWRITE)
,out float outDepth : DEPTH_SEMANTIC
#endif
)
{
	#if defined(TRIPLANAR_SELECTIVE)
		// unpack UV1
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			float2 secUV=float2(i.tex.w, i.eyeVec.w);
		#else
			float2 secUV=float2(i.tex.w, i.posObject.w);
		#endif		
	#endif

	// ------
	half actH;
	float4 SclCurv;
	half3 eyeVec;
	
	float3 tangentBasisScaled;
	
	float2 _ddx;
	float2 _ddy;
	float2 _ddxDet;
	float2 _ddyDet;
	float blendFade;
	
	half3 i_viewDirForParallax;
	half3x3 _TBN;
	half3 worldNormal;
	
	float4 texcoordsNoTransform;
	
	// void	SetupUBER(float4 i_SclCurv, half3 i_eyeVec, float3 i_posWorld, float3 i_posObject, inout float4 i_tex, inout half4 i_tangentToWorldAndParallax0, inout half4 i_tangentToWorldAndParallax1, inout half4 i_tangentToWorldAndParallax2, inout fixed4 vertex_color, out half actH, out float4 SclCurv, out half3 eyeVec, out float3 tangentBasisScaled, out float2 _ddx, out float2 _ddy, out float2 _ddxDet, out float2 _ddyDet, out float blendFade, out half3 i_viewDirForParallax, out half3x3 _TBN, out half3 worldNormal) {
	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		SetupUBER(float4(0,0,0,0), half3(0,0,0), IN_WORLDPOS(i), i.posObject.xyz, /* inout */ i.tex, /* inout */ i.tangentToWorldAndParallax0, /* inout */ i.tangentToWorldAndParallax1, /* inout */ i.tangentToWorldAndParallax2, /* inout */ i.vertex_color, /* out */ actH, /* out */ SclCurv, /* out */ eyeVec, /* out */ tangentBasisScaled, /* out */ _ddx, /* out */ _ddy, /* out */ _ddxDet, /* out */ _ddyDet, /* out */ blendFade, /* out */ i_viewDirForParallax, /* out */ _TBN, /* out */ worldNormal, /* out */ texcoordsNoTransform);
	#elif defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		SetupUBER(i.SclCurv, half3(0,0,0), IN_WORLDPOS(i), float3(0,0,0), /* inout */ i.tex, /* inout */ i.tangentToWorldAndParallax0, /* inout */ i.tangentToWorldAndParallax1, /* inout */ i.tangentToWorldAndParallax2, /* inout */ i.vertex_color, /* out */ actH, /* out */ SclCurv, /* out */ eyeVec, /* out */ tangentBasisScaled, /* out */ _ddx, /* out */ _ddy, /* out */ _ddxDet, /* out */ _ddyDet, /* out */ blendFade, /* out */ i_viewDirForParallax, /* out */ _TBN, /* out */ worldNormal, /* out */ texcoordsNoTransform);
	#else
		SetupUBER(float4(0,0,0,0), i.eyeVec.xyz, IN_WORLDPOS(i), float3(0,0,0), /* inout */ i.tex, /* inout */ i.tangentToWorldAndParallax0, /* inout */ i.tangentToWorldAndParallax1, /* inout */ i.tangentToWorldAndParallax2, /* inout */ i.vertex_color, /* out */ actH, /* out */ SclCurv, /* out */ eyeVec, /* out */ tangentBasisScaled, /* out */ _ddx, /* out */ _ddy, /* out */ _ddxDet, /* out */ _ddyDet, /* out */ blendFade, /* out */ i_viewDirForParallax, /* out */ _TBN, /* out */ worldNormal, /* out */ texcoordsNoTransform);
	#endif
	// ------	
	
	FRAGMENT_SETUP(s)	
	UnityLight mainLight = MainLight (s.normalWorld);
	half atten = SHADOW_ATTENUATION(i);
	
	half2 occ=Occlusion(i.tex, _ddx, _ddy, _ddxDet, _ddyDet, i.vertex_color); // y - translucency/glitter
	half occlusion = occ.x;
	#if defined(OCCLUSION_VERTEX_COLOR_CHANNEL)
		occlusion*=i.vertex_color.OCCLUSION_VERTEX_COLOR_CHANNEL;
	#endif
	#if defined(_TWO_LAYERS)
		occlusion = LerpOneTo(occlusion, lerp(_OcclusionStrength2, _OcclusionStrength, i.__VERTEX_COLOR_CHANNEL_LAYER));
	#else
		occlusion = LerpOneTo(occlusion, _OcclusionStrength);
	#endif
	// possible 2ndary occlusion
	#if defined(_OCCLUSION_FROM_ALBEDO_ALPHA)
		// primary occlusion from diffuse A, secondary from _OcclusionMap
		#if defined(TRIPLANAR_SELECTIVE)
			// already unpacked secUV
		#else
			#if defined(SECONDARY_OCCLUSION_PARALLAXED)
				float2 secUV=((i.tex.xy-_MainTex_ST.zw)/_MainTex_ST.xy - texcoordsNoTransform.xy) + texcoordsNoTransform.zw;
			#else
				float2 secUV=texcoordsNoTransform.zw; // actually we don't need parallax applied as we assume secondary occlusion is low freq maybe
			#endif
		#endif
		secUV = _UVSecOcclusionLightmapPacked==1 ? (secUV * unity_LightmapST.xy + unity_LightmapST.zw) : secUV;
		half4 occVal = tex2Dp(_OcclusionMap, secUV,  ddx(secUV),  ddy(secUV));
		half2 occ2 = half2(occVal.AMBIENT_OCCLUSION_CHANNEL, occVal.AUX_OCCLUSION_CHANNEL);
		// UV0 / UV1 occlusion switch
		occlusion *= _UVSecOcclusion==0 ? 1 : lerp(1, occ2.x, _SecOcclusionStrength);
		#if defined(_TRANSLUCENCY)
			// translucency mask from UV1
			half translucency_thickness_fromOccMap = occ2.y;
		#endif
	#else
		half translucency_thickness_fromOccMap = 1;
	#endif			

	#if defined(_SNOW)
		occlusion*=lerp(1, s.dissolveMaskValue, s.snowVal*_SnowDissolveMaskOcclusion);
	#endif
	#if defined(TRIPLANAR_SELECTIVE)
		occlusion*=lerp(1, blendFade, _TriplanarBlendAmbientOcclusion);
	#endif
	#if defined(_SNOW)
		occlusion=lerp(occlusion, 1, saturate(s.snowVal*_SnowDeepSmoothen*0.15));
	#endif

	#if defined(_TRANSLUCENCY)
		// UV0 / UV1 occlusion switch	
		half translucency_thickness = _UVSecOcclusion==0 ? occ.y : translucency_thickness_fromOccMap;
	#endif	
	
	// translucency
	#if defined(_TRANSLUCENCY)
		half3 TL=Translucency(s, mainLight, translucency_thickness, i.vertex_color);
		s.diffColor*=saturate(1-max(max(TL.r, TL.g), TL.b)*TRANSLUCENCY_SUPPRESS_DIFFUSECOLOR);
		atten=lerp( atten, 1, saturate( dot(TL,1)*_TranslucencySuppressRealtimeShadows ) );
	#endif	
	
	UnityGI gi = FragmentGI (
		s.posWorld, occlusion, i.ambientOrLightmapUV, atten, s.oneMinusRoughness, s.normalWorld, s.eyeVec, mainLight );

	#if defined(_PARALLAX_POM_SHADOWS) || defined(_POM_DISTANCE_MAP_SHADOWS) || defined(_POM_EXTRUSION_MAP_SHADOWS)
		#if defined(_SNOW) && !defined(_POM_DISTANCE_MAP_SHADOWS) && !defined(_POM_EXTRUSION_MAP_SHADOWS)
			bool SS_flag=gi.light.ndotl>0 && (saturate(s.snowVal*_SnowDeepSmoothen)<0.98);
		#else
			bool SS_flag=gi.light.ndotl>0;
		#endif
		if (SS_flag) {
			float3 lightDirInTanSpace=mul(s.tanToWorld, gi.light.dir);  // named tanToworld but this mul() actually works the opposite (as I swapped params in mul)
			#if defined(_SNOW) && !defined(_POM_DISTANCE_MAP_SHADOWS) && !defined(_POM_EXTRUSION_MAP_SHADOWS)
				gi.light.color *= SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, s.snowVal);
			#else
				#if defined(_POM_DISTANCE_MAP_SHADOWS) || defined(_POM_EXTRUSION_MAP_SHADOWS)
					gi.light.color *= lerp( SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, 0), 1, saturate( distance(i.posWorld, _WorldSpaceCameraPos) / _DepthReductionDistance ) );
				#else
					gi.light.color *= SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, 0);
				#endif
			#endif
		}
	#endif
	
	#if defined(_GLITTER)
		Glitter(/* inout */ s, i.tex.zw, _ddxDet, _ddyDet, i.posWorld.xyz, i.vertex_color, lerp(1, occ.y, _GlitterMask));
	#endif	
	
	half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
	c.rgb += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, occlusion, gi);
	
	#if defined(_TRANSLUCENCY)
		c.rgb += TL*gi.light.color;
	#endif	

	#if defined(_SNOW)
		half snowBlur=_SnowDeepSmoothen*4*s.snowVal; // currently not used
		half3 snowEmissionDamp=LerpWhiteTo(_SnowEmissionTransparency, s.snowVal);
	#else
		half snowBlur=0; // not used
		half3 snowEmissionDamp=1;
	#endif
	c.rgb += Emission(i.tex.xyzw, i.vertex_color, _ddx, _ddy, snowBlur)*snowEmissionDamp; // UBER - 4 components (main uv, detail uv) + vertex color (for masking), under snow blurring
	c.rgb += s.additionalEmission*snowEmissionDamp; // UBER - detail/wet emission

	UNITY_APPLY_FOG(i.fogCoord, c.rgb);

	#if defined(_REFRACTION)
		float3 worldViewDir = s.posWorld - _WorldSpaceCameraPos.xyz;
		worldViewDir=normalize(worldViewDir);
		float2 screenUV = (i.screenPos.xy / i.screenPos.w);
		#if !defined(SHADER_API_OPENGL)
			screenUV.y = _ProjectionParams.x>0 ? 1 - screenUV.y : screenUV.y;
		#endif
    	//UNITY_MATRIX_V[1].xyz // cam up
    	//UNITY_MATRIX_V[0].xyz // cam right
    	float NdotV=dot(_RefractionBumpScale*s.normalWorld+_TBN[2], s.eyeVec);
    	//float2 offset=_Refraction*float2(dot(s.normalWorld, UNITY_MATRIX_V[0].xyz*dot(worldViewDir,UNITY_MATRIX_V[2].xyz)), dot(s.normalWorld, UNITY_MATRIX_V[1].xyz*dot(worldViewDir,UNITY_MATRIX_V[2].xyz)))*(NdotV*NdotV);
    	float2 offset=_Refraction*float2(dot(_TBN[2], UNITY_MATRIX_V[0].xyz*dot(worldViewDir,UNITY_MATRIX_V[2].xyz)), dot(_TBN[2], UNITY_MATRIX_V[1].xyz*dot(_TBN[2],UNITY_MATRIX_V[2].xyz)))*(NdotV*NdotV);
    	half2 dampUV=abs(screenUV*2-1);
    	half borderDamp=saturate(1 - max ( (dampUV.x-0.9)/(1-0.9) , (dampUV.y-0.85)/(1-0.85) ));
    	offset*=borderDamp;
    	float3 centerCol=tex2D(_GrabTexture, screenUV+offset).rgb;
    	#if defined(_CHROMATIC_ABERRATION) 
	    	float abberrationG=1-_RefractionChromaticAberration;
	    	float abberrationB=1+_RefractionChromaticAberration;
	    	float _R=centerCol.r;
			half3 sceneColor = half3(_R, tex2D(_GrabTexture, screenUV+offset*lerp(abberrationG, 1, NdotV)).g, tex2D(_GrabTexture, screenUV+offset*lerp(abberrationB, 1, NdotV)).b);
		#else
			half3 sceneColor = centerCol;
		#endif
		c.rgb+=sceneColor*(1-s.alpha);
		outCol=half4(c.rgb, 1);
	#else
		outCol=OutputForward (c, s.alpha);
	#endif
	
	#if defined(ZWRITE)
		//float depthWithOffset = i.posWorld.w+s.rayLength;
		float depthWithOffset = i.posWorld.w*(1+s.rayLength/distance(i.posWorld.xyz, _WorldSpaceCameraPos)); // Z-DEPTH perspective correction
		outDepth = (1.0 - depthWithOffset * _ZBufferParams.w) / (depthWithOffset * _ZBufferParams.z);
	#endif
	
	
	#if defined(DISTANCE_MAP)
		//outCol.rgb=i.vertex_color.a;//s.rayPos.z;
	#endif
	
//	UNITY_MATRIX_V[3].xyz;//
//Camera position = _WorldSpaceCameraPos = mul(UNITY_MATRIX_V,float4(0,0,0,1)).xyz;
	
//	outCol.rgb=s.normalWorld;

}

// ------------------------------------------------------------------
//  Additive forward pass (one light per pass)
struct VertexOutputForwardAdd
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0; // normal in triplanar (.w means UV1 - u coord for triplanar - needed for secondary occlusion)
	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		float4 posObject				: TEXCOORD1; // .w means UV1 - v coord for triplanar (needed for secondary occlusion)
	#elif defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		float4 SclCurv					: TEXCOORD1;
	#else
		float4 eyeVec 					: TEXCOORD1; // .w means UV1 - v coord for triplanar (needed for secondary occlusion)
	#endif
	half4 tangentToWorldAndParallax0	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax] - note: tangents+obj scale in triplanar (tangents in world space when mapping in world space)
	half4 tangentToWorldAndParallax1	: TEXCOORD3;
	half4 tangentToWorldAndParallax2	: TEXCOORD4;
	fixed4 vertex_color					: COLOR0;
	LIGHTING_COORDS(5,6)
	UNITY_FOG_COORDS(7)

	// next ones would not fit into SM2.0 limits, but they are always for SM3.0+
	#if defined(ZWRITE)
		float4 posWorld					: TEXCOORD8;
	#else
		float3 posWorld					: TEXCOORD8;
	#endif
};

VertexOutputForwardAdd vertForwardAdd (VertexInput v)
{
	VertexOutputForwardAdd o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAdd, o);
	
	#if defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		float2 Curv=frac(v.uv3);
		float2 Scl=(v.uv3-Curv)/100; // scale represented with 0.01 resolution (fair enough)
		Scl=Scl*_Tan2ObjectMultOffset.xy+_Tan2ObjectMultOffset.zw;
		#if defined(VERTEX_COLOR_CHANNEL_POMZ)
			v.vertex.xyz+=_POM_ExtrudeVolume ? v.normal.xyz*v.color.VERTEX_COLOR_CHANNEL_POMZ*_Depth*max(Scl.x, Scl.y)/max(_MainTex_ST.x, _MainTex_ST.y) : float3(0,0,0);
			// Curv.x==0 - extruded bottom flag
			v.color.VERTEX_COLOR_CHANNEL_POMZ = Curv.x==0 || (!_POM_ExtrudeVolume) ? v.color.VERTEX_COLOR_CHANNEL_POMZ : 1-v.color.VERTEX_COLOR_CHANNEL_POMZ;
			//Curv=0; // no curvature on extruded volumes (we need bottom flag info in parallax function though - so DON'T zero Curv here !)
			// if we don't handle the volume set the curvature data to desired range
			Curv = _POM_ExtrudeVolume ? Curv : Curv*20-10;
		#else
			Curv=Curv*20-10; // Curv=(Curv-0.5)*10; // we assume curvature won't be higher than +/- 10
		#endif
	#endif

	float4 posWorld = mul(_Object2World, v.vertex);
	o.posWorld.xyz = posWorld.xyz;
	#if defined(ZWRITE)
		COMPUTE_EYEDEPTH(o.posWorld.w);
	#endif		
	o.vertex_color = v.color;
	o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
	
	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		//o.posObject set below
	#elif defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		o.SclCurv=float4(float2(1.0,1.0)/Scl, Curv);
	#else
		o.eyeVec.xyz = posWorld.xyz - _WorldSpaceCameraPos;
		#if defined(TRIPLANAR_SELECTIVE)
			// world mapping
			o.eyeVec.w = v.uv1.y;
		#endif
	#endif
	
	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			float3 normalWorld = UnityObjectToWorldNormal(v.normal);
			SetupUBER_VertexData_TriplanarWorld(normalWorld, /* inout */ o.tangentToWorldAndParallax0, /* inout */ o.tangentToWorldAndParallax1, /* inout */ o.tangentToWorldAndParallax2);
		#else
			float scaleX, scaleY, scaleZ;
			SetupUBER_VertexData_TriplanarLocal(v.normal, /* inout */ o.tangentToWorldAndParallax0, /* inout */ o.tangentToWorldAndParallax1, /* inout */ o.tangentToWorldAndParallax2, /* out */ scaleX, /* out */ scaleY, /* out */ scaleZ);
			float3 normalWorld = UnityObjectToWorldNormal(v.normal);
			o.posObject.xyz = v.vertex.xyz;
			o.posObject.w = v.uv1.y; // pack it here
		#endif		
	#elif defined(_TANGENT_TO_WORLD)
		half3x3 tangentToWorld;
		
		// we need to go from tangent to world space for zwrite and parallaxed snow (actually when snow is mapped in worldspace)
		#if defined(RAYLENGTH_AVAILABLE)
			float3 normalWorld = mul((float3x3)_Object2World, v.normal.xyz);
			float3 tangentWorld = mul((float3x3)_Object2World, v.tangent.xyz);
			float3 binormalWorld = mul((float3x3)_Object2World, cross(v.normal.xyz, v.tangent.xyz)*v.tangent.w);
			#ifdef SHADER_TARGET_GLSL
			binormalWorld*=0.9999; // dummy op to cheat HLSL2GLSL optimizer to not be so smart (and buggy) here... It probably tries to make some fancy matrix by matrix calculation
			#endif
			// not normalized basis (we need it for texture 2 worldspace ratio calculations)
			tangentToWorld=half3x3(tangentWorld, binormalWorld, normalWorld);
			normalWorld = normalize(normalWorld); // we need it below for lighting
		#else
			float3 normalWorld = UnityObjectToWorldNormal(v.normal);
			float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
			tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, v.tangent.w);
		#endif

		o.tangentToWorldAndParallax0.xyz = tangentToWorld[0];
		o.tangentToWorldAndParallax1.xyz = tangentToWorld[1];
		o.tangentToWorldAndParallax2.xyz = tangentToWorld[2];
	#else
		float3 normalWorld = UnityObjectToWorldNormal(v.normal);
		o.tangentToWorldAndParallax0.xyz = 0;
		o.tangentToWorldAndParallax1.xyz = 0;
		o.tangentToWorldAndParallax2.xyz = normalWorld;
	#endif
	//We need this for shadow receving
	TRANSFER_VERTEX_TO_FRAGMENT(o);

	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			// .w component not used
		#else
			o.tangentToWorldAndParallax0.w=scaleX;
			o.tangentToWorldAndParallax1.w=scaleY;
			o.tangentToWorldAndParallax2.w=scaleZ;
		#endif
	#elif defined(_PARALLAX_MAP) || defined(_PARALLAX_MAP_2MAPS) || defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		#if ((defined(POM)) && defined(SILHOUETTE_CURVATURE_MAPPED)) || defined(TRIPLANAR_SELECTIVE)
			half3 viewDirForParallax = ObjSpaceViewDir(v.vertex);
		#else
			// vertex normal, tangent are not guaranteed to be normalized (!)
			// try - 2 simple planes on the scene using the same material, anchored and parent has decreased scale, Unity makes kind of batch (vertices seems to be transformed to world space) ? Anyway mesh tangents, normals get scaled, too and makes total mess with TBN matrices (view direction...)
			v.normal=normalize(v.normal);
			v.tangent.xyz=normalize(v.tangent.xyz);
			float3 binormal = cross( v.normal, v.tangent.xyz ) * v.tangent.w;
			float3x3 rotation = float3x3( v.tangent.xyz, binormal, v.normal );
			half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
		#endif
		o.tangentToWorldAndParallax0.w = viewDirForParallax.x;
		o.tangentToWorldAndParallax1.w = viewDirForParallax.y;
		o.tangentToWorldAndParallax2.w = viewDirForParallax.z;
	#endif
	
	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			o.tex = float4(normalWorld, v.uv1.x); // pack UV1 here
		#else
			o.tex = float4(v.normal, v.uv1.x); // pack UV1 here
		#endif
	#else
		o.tex = TexCoordsNoTransform(v);
	#endif
	
	UNITY_TRANSFER_FOG(o,o.pos);
	return o;
}

void fragForwardAdd (VertexOutputForwardAdd i, out half4 outCol : SV_Target
#if defined(ZWRITE)
,out float outDepth : DEPTH_SEMANTIC
#endif
)
{
	#if defined(TRIPLANAR_SELECTIVE)
		// unpack UV1
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			float2 secUV=float2(i.tex.w, i.eyeVec.w);
		#else
			float2 secUV=float2(i.tex.w, i.posObject.w);
		#endif		
	#endif
	
	// ------
	half actH;
	float4 SclCurv;
	half3 eyeVec;
	
	float3 tangentBasisScaled;
	
	float2 _ddx;
	float2 _ddy;
	float2 _ddxDet;
	float2 _ddyDet;
	float blendFade;
	
	half3 i_viewDirForParallax;
	half3x3 _TBN;
	half3 worldNormal;
	
	float4 texcoordsNoTransform;
	
	// void	SetupUBER(float4 i_SclCurv, half3 i_eyeVec, float3 i_posWorld, float3 i_posObject, inout float4 i_tex, inout half4 i_tangentToWorldAndParallax0, inout half4 i_tangentToWorldAndParallax1, inout half4 i_tangentToWorldAndParallax2, inout fixed4 vertex_color, out half actH, out float4 SclCurv, out half3 eyeVec, out float3 tangentBasisScaled, out float2 _ddx, out float2 _ddy, out float2 _ddxDet, out float2 _ddyDet, out float blendFade, out half3 i_viewDirForParallax, out half3x3 _TBN, out half3 worldNormal) {
	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		SetupUBER(float4(0,0,0,0), half3(0,0,0), i.posWorld.xyz, i.posObject.xyz, /* inout */ i.tex, /* inout */ i.tangentToWorldAndParallax0, /* inout */ i.tangentToWorldAndParallax1, /* inout */ i.tangentToWorldAndParallax2, /* inout */ i.vertex_color, /* out */ actH, /* out */ SclCurv, /* out */ eyeVec, /* out */ tangentBasisScaled, /* out */ _ddx, /* out */ _ddy, /* out */ _ddxDet, /* out */ _ddyDet, /* out */ blendFade, /* out */ i_viewDirForParallax, /* out */ _TBN, /* out */ worldNormal, /* out */ texcoordsNoTransform);
	#elif defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		SetupUBER(i.SclCurv, half3(0,0,0), i.posWorld.xyz, float3(0,0,0), /* inout */ i.tex, /* inout */ i.tangentToWorldAndParallax0, /* inout */ i.tangentToWorldAndParallax1, /* inout */ i.tangentToWorldAndParallax2, /* inout */ i.vertex_color, /* out */ actH, /* out */ SclCurv, /* out */ eyeVec, /* out */ tangentBasisScaled, /* out */ _ddx, /* out */ _ddy, /* out */ _ddxDet, /* out */ _ddyDet, /* out */ blendFade, /* out */ i_viewDirForParallax, /* out */ _TBN, /* out */ worldNormal, /* out */ texcoordsNoTransform);
	#else
		SetupUBER(float4(0,0,0,0), i.eyeVec.xyz, i.posWorld.xyz, float3(0,0,0), /* inout */ i.tex, /* inout */ i.tangentToWorldAndParallax0, /* inout */ i.tangentToWorldAndParallax1, /* inout */ i.tangentToWorldAndParallax2, /* inout */ i.vertex_color, /* out */ actH, /* out */ SclCurv, /* out */ eyeVec, /* out */ tangentBasisScaled, /* out */ _ddx, /* out */ _ddy, /* out */ _ddxDet, /* out */ _ddyDet, /* out */ blendFade, /* out */ i_viewDirForParallax, /* out */ _TBN, /* out */ worldNormal, /* out */ texcoordsNoTransform);
	#endif
	// ------	
	
	FRAGMENT_SETUP_FWDADD(s)
	
	float3 posWorldDepth = i.posWorld.xyz; // needed for light
	#if defined(ZWRITE)
		posWorldDepth += s.rayLength*s.eyeVec;
	#endif
	
	half2 occ=Occlusion(i.tex, _ddx, _ddy, _ddxDet, _ddyDet, i.vertex_color); // y - translucency/glitter
	#if defined(_TRANSLUCENCY)
		// translucency occlusion might be taken from UV1
		#if defined(TRIPLANAR_SELECTIVE)
			// already unpacked secUV		
		#else
			#if defined(SECONDARY_OCCLUSION_PARALLAXED)
				float2 secUV=((i.tex.xy-_MainTex_ST.zw)/_MainTex_ST.xy - texcoordsNoTransform.xy) + texcoordsNoTransform.zw;
			#else
				float2 secUV=texcoordsNoTransform.zw; // actually we don't need parallax applied as we assume secondary occlusion is low freq maybe
			#endif
		#endif
		secUV = _UVSecOcclusionLightmapPacked==1 ? (secUV * unity_LightmapST.xy + unity_LightmapST.zw) : secUV;
		// translucency mask from UV1
		half translucency_thickness_fromOccMap = tex2Dp(_OcclusionMap, secUV,  ddx(secUV),  ddy(secUV)).AUX_OCCLUSION_CHANNEL;
		// UV0 / UV1 occlusion switch
		half translucency_thickness = _UVSecOcclusion==0 ? occ.y : translucency_thickness_fromOccMap;
	#endif
	
	// push lighting coords
	#if defined(ZWRITE)
		#if defined (SHADOWS_DEPTH) && defined (SPOT)
			// spot lights - more expensive recast World2Shadow matrix...
			i._ShadowCoord = mul (unity_World2Shadow[0], float4(posWorldDepth,1));
		#elif defined (SHADOWS_CUBE)
			// point light - easy stuff - just push it towards viewing vector
			i._ShadowCoord+=s.rayLength*s.eyeVec;
		#endif
		#if defined(SPOT) || defined(POINT)
			// recast coords for light attenuation
			#if defined(POINT)
				i._LightCoord = mul(_LightMatrix0, float4(posWorldDepth,1)).xyz;	
			#else
				i._LightCoord = mul(_LightMatrix0, float4(posWorldDepth,1));	
			#endif
		#endif
	#endif
	 
	
	#if defined(USING_DIRECTIONAL_LIGHT)
		float3 lightDir = _WorldSpaceLightPos0.xyz;
		// NOTE - LIGHT_ATTENUATION(i) doesn't take shadow attenuation now (overriden at UBER_AutoLightMod.cginc)
		UnityLight light = AdditiveLight (s.normalWorld, lightDir, LIGHT_ATTENUATION(i));
	#else
		#if UBER_MATCH_ALLOY_LIGHT_FALLOFF
			//
			// match attenuation falloff of Alloy
			//
			float3 lightDir = (_WorldSpaceLightPos0.xyz - posWorldDepth * _WorldSpaceLightPos0.w);		
//			#ifndef ALLOY_DISABLE_AREA_LIGHTS
//				float3 lightDirArea = lightDir;
//				float3 _R=reflect( (posWorldDepth-_WorldSpaceCameraPos), s.normalWorld);
//				float3 centerToRay = dot(lightDir, _R) * _R - lightDir;
//				float light_size = _LightColor0.a / _LightPositionRange.w; // lightColor.a*range
//				lightDirArea += centerToRay * saturate(light_size / length(centerToRay));
//				lightDirArea = normalize(lightDirArea);
//			#endif
			half light_distSqr = dot(lightDir, lightDir);
			half light_dist = sqrt(light_distSqr);
			lightDir/=light_dist;
			#ifdef SPOT
				half rangeInv = length(i._LightCoord.xyz) / light_dist;
			#else
				half rangeInv=_LightPositionRange.w; // seems that this works _LightPositionRange.w=1/range
			#endif
			half ratio = light_dist * rangeInv;
			half ratio2 = ratio * ratio;
			half num = saturate(1.0h - (ratio2 * ratio2));
			half latten = (num * num) / (light_distSqr + 1.0h);
			#ifdef POINT_COOKIE
				latten *= texCUBE(_LightTexture0, i._LightCoord).w;
			#endif
			#ifdef SPOT
				latten *= (i._LightCoord.z > 0) * UnitySpotCookie(i._LightCoord);
			#endif			
//			#ifndef ALLOY_DISABLE_AREA_LIGHTS
//				// dir for lighting - area lights corrected
//				// (TODO - currently doesn't work - area lights are more than light dir correction only)
//				lightDir = lightDirArea;		
//			#endif
			UnityLight light = AdditiveLight (s.normalWorld, lightDir, latten);
		#else
			//
			// regular Unity's light with default attenuation (w/o shadows though)
			//
			float3 lightDir = normalize(_WorldSpaceLightPos0.xyz - posWorldDepth * _WorldSpaceLightPos0.w);
			// NOTE - LIGHT_ATTENUATION(i) doesn't take shadow attenuation now (overriden at UBER_AutoLightMod.cginc)
			UnityLight light = AdditiveLight (s.normalWorld, lightDir, LIGHT_ATTENUATION(i));
		#endif
	#endif
	half atten = SHADOW_ATTENUATION(i);

	#if (defined(_PARALLAX_POM_SHADOWS) || defined(_POM_DISTANCE_MAP_SHADOWS) || defined(_POM_EXTRUSION_MAP_SHADOWS)) && defined(_UBER_SHADOWS_FORWARDADD)
		#if defined(_SNOW) && !defined(_POM_DISTANCE_MAP_SHADOWS) && !defined(_POM_EXTRUSION_MAP_SHADOWS)
			bool SS_flag=light.ndotl>0 && (saturate(s.snowVal*_SnowDeepSmoothen)<0.98);
		#else
			bool SS_flag=light.ndotl>0;
		#endif
		if (SS_flag) {
			float3 lightDirInTanSpace=mul(light.dir, transpose(s.tanToWorld));
			#if defined(_SNOW) && !defined(_POM_DISTANCE_MAP_SHADOWS) && !defined(_POM_EXTRUSION_MAP_SHADOWS)
				light.color *= SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, s.snowVal);
			#else
				#if defined(_POM_DISTANCE_MAP_SHADOWS) || defined(_POM_EXTRUSION_MAP_SHADOWS)
					light.color *= lerp( SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, 0), 1, saturate( distance(i.posWorld, _WorldSpaceCameraPos) / _DepthReductionDistance ) );
				#else
					light.color *= SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, 0);
				#endif				
			#endif
		}
	#endif
	UnityIndirect noIndirect = ZeroIndirect ();
	
	// translucency
	#if defined(_TRANSLUCENCY)
		half3 TL=Translucency(s, light, translucency_thickness, i.vertex_color);
		s.diffColor*=saturate(1-max(max(TL.r, TL.g), TL.b)*TRANSLUCENCY_SUPPRESS_DIFFUSECOLOR);
		atten=lerp( atten, 1, saturate( dot(TL,0.3)*_TranslucencySuppressRealtimeShadows ) );
	#endif	
	
	// apply shadows here (they can be suppressed by translucency
	light.color*=atten;
	
	#if defined(_GLITTER)
		Glitter(/* inout */ s, i.tex.zw, _ddxDet, _ddyDet, posWorldDepth, i.vertex_color, lerp(1, occ.y, _GlitterMask));
	#endif	
		
	half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, light, noIndirect);
	
	#if defined(_TRANSLUCENCY)
		c.rgb += TL*light.color;
	#endif	
		
	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
	outCol=OutputForward (c, s.alpha);
	
	#if defined(ZWRITE)
		//float depthWithOffset = i.posWorld.w+s.rayLength;
		float depthWithOffset = i.posWorld.w*(1+s.rayLength/distance(i.posWorld.xyz, _WorldSpaceCameraPos)); // Z-DEPTH perspective correction
		outDepth = (1.0 - depthWithOffset * _ZBufferParams.w) / (depthWithOffset * _ZBufferParams.z);
	#endif	
}

// ------------------------------------------------------------------
//  Deferred pass

struct VertexOutputDeferred
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0; // normal in triplanar (.w means UV1 - u coord for triplanar - needed for secondary occlusion)
	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		float4 posObject				: TEXCOORD1; // .w means UV1 - v coord for triplanar (needed for secondary occlusion)
	#elif defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		float4 SclCurv					: TEXCOORD1;
	#else
		float4 eyeVec 					: TEXCOORD1; // .w means UV1 - v coord for triplanar (needed for secondary occlusion)
	#endif
	half4 tangentToWorldAndParallax0	: TEXCOORD2;	// [3x3:tangentToWorld | 1x3:viewDirForParallax] - note: tangents+obj scale in triplanar (tangents in world space when mapping in world space)
	half4 tangentToWorldAndParallax1	: TEXCOORD3;	// (array fails in GLSL optimizer)
	half4 tangentToWorldAndParallax2	: TEXCOORD4;
	half4 ambientOrLightmapUV			: TEXCOORD5;	// SH or Lightmap UVs			
	fixed4 vertex_color					: COLOR0;		// UBER

	#if UNITY_SPECCUBE_BOX_PROJECTION || defined(POM) || defined(_TRANSLUCENCY) || defined(_GLITTER) || defined(_SNOW) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP) || defined(TRIPLANAR_SELECTIVE)
		#if defined(ZWRITE)
			float4 posWorld					: TEXCOORD6;
		#else
			float3 posWorld					: TEXCOORD6;
		#endif
	#endif
};


VertexOutputDeferred vertDeferred (VertexInput v)
{
	VertexOutputDeferred o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputDeferred, o);
	
	#if defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		float2 Curv=frac(v.uv3);
		float2 Scl=(v.uv3-Curv)/100; // scale represented with 0.01 resolution (fair enough)
		Scl=Scl*_Tan2ObjectMultOffset.xy+_Tan2ObjectMultOffset.zw;
		#if defined(VERTEX_COLOR_CHANNEL_POMZ)
			v.vertex.xyz+=_POM_ExtrudeVolume ? v.normal.xyz*v.color.VERTEX_COLOR_CHANNEL_POMZ*_Depth*max(Scl.x, Scl.y)/max(_MainTex_ST.x, _MainTex_ST.y) : float3(0,0,0);
			// Curv.x==0 - extruded bottom flag
			v.color.VERTEX_COLOR_CHANNEL_POMZ = Curv.x==0 || (!_POM_ExtrudeVolume) ? v.color.VERTEX_COLOR_CHANNEL_POMZ : 1-v.color.VERTEX_COLOR_CHANNEL_POMZ;
			//Curv=0; // no curvature on extruded volumes (we need bottom flag info in parallax function though - so DON'T zero Curv here !)
			// if we don't handle the volume set the curvature data to desired range
			Curv = _POM_ExtrudeVolume ? Curv : Curv*20-10;
		#else
			Curv=Curv*20-10; // Curv=(Curv-0.5)*10; // we assume curvature won't be higher than +/- 10
		#endif
	#endif
	
	float4 posWorld = mul(_Object2World, v.vertex);
	#if UNITY_SPECCUBE_BOX_PROJECTION || defined(POM) || defined(_TRANSLUCENCY) || defined(_GLITTER) || defined(_SNOW) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP) || defined(TRIPLANAR_SELECTIVE)
		o.posWorld.xyz = posWorld.xyz; // UBER - there was implicit truncation here
		#if defined(ZWRITE)
			COMPUTE_EYEDEPTH(o.posWorld.w);
		#endif		
	#endif
	o.vertex_color = v.color; // UBER
	o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		//o.posObject set below
	#elif defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		o.SclCurv=float4(float2(1.0,1.0)/Scl, Curv);
	#else
		o.eyeVec.xyz = posWorld.xyz - _WorldSpaceCameraPos;
		#if defined(TRIPLANAR_SELECTIVE)
			// world mapping
			o.eyeVec.w = v.uv1.y;
		#endif
	#endif
	
	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			float3 normalWorld = UnityObjectToWorldNormal(v.normal);
			SetupUBER_VertexData_TriplanarWorld(normalWorld, /* inout */ o.tangentToWorldAndParallax0, /* inout */ o.tangentToWorldAndParallax1, /* inout */ o.tangentToWorldAndParallax2);
		#else
			float scaleX, scaleY, scaleZ;
			SetupUBER_VertexData_TriplanarLocal(v.normal, /* inout */ o.tangentToWorldAndParallax0, /* inout */ o.tangentToWorldAndParallax1, /* inout */ o.tangentToWorldAndParallax2, /* out */ scaleX, /* out */ scaleY, /* out */ scaleZ);
			float3 normalWorld = UnityObjectToWorldNormal(v.normal);
			o.posObject.xyz = v.vertex.xyz;
			o.posObject.w = v.uv1.y; // pack it here
		#endif		
	#elif defined(_TANGENT_TO_WORLD)
		half3x3 tangentToWorld;
		
		// we need to go from tangent to world space for zwrite and parallaxed snow (actually when snow is mapped in worldspace)
		#if defined(RAYLENGTH_AVAILABLE)
			float3 normalWorld = mul((float3x3)_Object2World, v.normal.xyz);
			float3 tangentWorld = mul((float3x3)_Object2World, v.tangent.xyz);
			float3 binormalWorld = mul((float3x3)_Object2World, cross(v.normal.xyz, v.tangent.xyz)*v.tangent.w);
			#ifdef SHADER_TARGET_GLSL
			binormalWorld*=0.9999; // dummy op to cheat HLSL2GLSL optimizer to not be so smart (and buggy) here... It probably tries to make some fancy matrix by matrix calculation
			#endif
			// not normalized basis (we need it for texture 2 worldspace ratio calculations)
			tangentToWorld=half3x3(tangentWorld, binormalWorld, normalWorld);
			normalWorld = normalize(normalWorld); // we need it below for lighting
		#else
			float3 normalWorld = UnityObjectToWorldNormal(v.normal);
			float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
			tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, v.tangent.w);
		#endif


		o.tangentToWorldAndParallax0.xyz = tangentToWorld[0];
		o.tangentToWorldAndParallax1.xyz = tangentToWorld[1];
		o.tangentToWorldAndParallax2.xyz = tangentToWorld[2];
	#else
		float3 normalWorld = UnityObjectToWorldNormal(v.normal);
		o.tangentToWorldAndParallax0.xyz = 0;
		o.tangentToWorldAndParallax1.xyz = 0;
		o.tangentToWorldAndParallax2.xyz = normalWorld;
	#endif

	#ifndef LIGHTMAP_OFF
		o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
		o.ambientOrLightmapUV.zw = 0;
	#elif UNITY_SHOULD_SAMPLE_SH
		#if (SHADER_TARGET < 30)
			o.ambientOrLightmapUV.rgb = ShadeSH9(half4(normalWorld, 1.0));
		#else
			// Optimization: L2 per-vertex, L0..L1 per-pixel
			o.ambientOrLightmapUV.rgb = ShadeSH3Order(half4(normalWorld, 1.0));
		#endif
	#endif
	
	#ifdef DYNAMICLIGHTMAP_ON
		o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif
	
	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			// .w component not used
		#else
			o.tangentToWorldAndParallax0.w=scaleX;
			o.tangentToWorldAndParallax1.w=scaleY;
			o.tangentToWorldAndParallax2.w=scaleZ;
		#endif
	#elif defined(_PARALLAX_MAP) || defined(_PARALLAX_MAP_2MAPS) || defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		#if ((defined(POM)) && defined(SILHOUETTE_CURVATURE_MAPPED)) || defined(TRIPLANAR_SELECTIVE)
			half3 viewDirForParallax = ObjSpaceViewDir(v.vertex);
		#else
			// vertex normal, tangent are not guaranteed to be normalized (!)
			// try - 2 simple planes on the scene using the same material, anchored and parent has decreased scale, Unity makes kind of batch (vertices seems to be transformed to world space) ? Anyway mesh tangents, normals get scaled, too and makes total mess with TBN matrices (view direction...)
			v.normal=normalize(v.normal);
			v.tangent.xyz=normalize(v.tangent.xyz);
			float3 binormal = cross( v.normal, v.tangent.xyz ) * v.tangent.w;
			float3x3 rotation = float3x3( v.tangent.xyz, binormal, v.normal );
			half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
		#endif
		o.tangentToWorldAndParallax0.w = viewDirForParallax.x;
		o.tangentToWorldAndParallax1.w = viewDirForParallax.y;
		o.tangentToWorldAndParallax2.w = viewDirForParallax.z;
	#endif
	
	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			o.tex = float4(normalWorld, v.uv1.x); // pack UV1 here
		#else
			o.tex = float4(v.normal, v.uv1.x);
		#endif
	#else
		o.tex = TexCoordsNoTransform(v);
	#endif
		
	return o;
}

void fragDeferred (
	VertexOutputDeferred i,
	out half4 outDiffuse : SV_Target0,			// RT0: diffuse color (rgb), occlusion (a)
	out half4 outSpecSmoothness : SV_Target1,	// RT1: spec color (rgb), smoothness (a)
	out half4 outNormal : SV_Target2,			// RT2: normal (rgb), POM self-shadowing (a) 
	out half4 outEmission : SV_Target3			// RT3: emission (rgb), translucency encoded (a)
#if defined(ZWRITE)
,out float outDepth : DEPTH_SEMANTIC
#endif	
)
{
	#if (SHADER_TARGET < 30)
		outDiffuse = 1;
		outSpecSmoothness = 1;
		outNormal = 0;
		outEmission = 0;
		return;
	#endif
	
	#if defined(TRIPLANAR_SELECTIVE)
		// unpack UV1
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			float2 secUV=float2(i.tex.w, i.eyeVec.w);
		#else
			float2 secUV=float2(i.tex.w, i.posObject.w);
		#endif		
	#endif
		
	// ------
	half actH;
	float4 SclCurv;
	half3 eyeVec;
	
	float3 tangentBasisScaled;
	
	float2 _ddx;
	float2 _ddy;
	float2 _ddxDet;
	float2 _ddyDet;
	float blendFade;
	
	half3 i_viewDirForParallax;
	half3x3 _TBN;
	half3 worldNormal;
	
	float4 texcoordsNoTransform;
	
	// void	SetupUBER(float4 i_SclCurv, half3 i_eyeVec, float3 i_posWorld, float3 i_posObject, inout float4 i_tex, inout half4 i_tangentToWorldAndParallax0, inout half4 i_tangentToWorldAndParallax1, inout half4 i_tangentToWorldAndParallax2, inout fixed4 vertex_color, out half actH, out float4 SclCurv, out half3 eyeVec, out float3 tangentBasisScaled, out float2 _ddx, out float2 _ddy, out float2 _ddxDet, out float2 _ddyDet, out float blendFade, out half3 i_viewDirForParallax, out half3x3 _TBN, out half3 worldNormal) {
	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		SetupUBER(float4(0,0,0,0), half3(0,0,0), IN_WORLDPOS(i), i.posObject.xyz, /* inout */ i.tex, /* inout */ i.tangentToWorldAndParallax0, /* inout */ i.tangentToWorldAndParallax1, /* inout */ i.tangentToWorldAndParallax2, /* inout */ i.vertex_color, /* out */ actH, /* out */ SclCurv, /* out */ eyeVec, /* out */ tangentBasisScaled, /* out */ _ddx, /* out */ _ddy, /* out */ _ddxDet, /* out */ _ddyDet, /* out */ blendFade, /* out */ i_viewDirForParallax, /* out */ _TBN, /* out */ worldNormal, /* out */ texcoordsNoTransform);
	#elif defined(POM) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP)
		SetupUBER(i.SclCurv, half3(0,0,0), IN_WORLDPOS(i), float3(0,0,0), /* inout */ i.tex, /* inout */ i.tangentToWorldAndParallax0, /* inout */ i.tangentToWorldAndParallax1, /* inout */ i.tangentToWorldAndParallax2, /* inout */ i.vertex_color, /* out */ actH, /* out */ SclCurv, /* out */ eyeVec, /* out */ tangentBasisScaled, /* out */ _ddx, /* out */ _ddy, /* out */ _ddxDet, /* out */ _ddyDet, /* out */ blendFade, /* out */ i_viewDirForParallax, /* out */ _TBN, /* out */ worldNormal, /* out */ texcoordsNoTransform);
	#else
		SetupUBER(float4(0,0,0,0), i.eyeVec.xyz, IN_WORLDPOS(i), float3(0,0,0), /* inout */ i.tex, /* inout */ i.tangentToWorldAndParallax0, /* inout */ i.tangentToWorldAndParallax1, /* inout */ i.tangentToWorldAndParallax2, /* inout */ i.vertex_color, /* out */ actH, /* out */ SclCurv, /* out */ eyeVec, /* out */ tangentBasisScaled, /* out */ _ddx, /* out */ _ddy, /* out */ _ddxDet, /* out */ _ddyDet, /* out */ blendFade, /* out */ i_viewDirForParallax, /* out */ _TBN, /* out */ worldNormal, /* out */ texcoordsNoTransform);
	#endif
	// ------	
	
	FRAGMENT_SETUP(s)

	// no analytic lights in this pass
	UnityLight dummyLight = DummyLight (s.normalWorld);
	half atten = 1;

	// only GI
	half2 occ=Occlusion(i.tex, _ddx, _ddy, _ddxDet, _ddyDet, i.vertex_color); // y - translucency/glitter
	half occlusion = occ.x;
	#if defined(OCCLUSION_VERTEX_COLOR_CHANNEL)
		occlusion*=i.vertex_color.OCCLUSION_VERTEX_COLOR_CHANNEL;
	#endif
	#if defined(_TWO_LAYERS)
		occlusion = LerpOneTo(occlusion, lerp(_OcclusionStrength2, _OcclusionStrength, i.__VERTEX_COLOR_CHANNEL_LAYER));
	#else
		occlusion = LerpOneTo(occlusion, _OcclusionStrength);
	#endif
	// possible 2ndary occlusion
	#if defined(_OCCLUSION_FROM_ALBEDO_ALPHA)
		// primary occlusion from diffuse A, secondary from _OcclusionMap
		#if defined(TRIPLANAR_SELECTIVE)
			// already unpacked secUV			
		#else
			#if defined(SECONDARY_OCCLUSION_PARALLAXED)
				float2 secUV=((i.tex.xy-_MainTex_ST.zw)/_MainTex_ST.xy - texcoordsNoTransform.xy) + texcoordsNoTransform.zw;
			#else
				float2 secUV=texcoordsNoTransform.zw; // actually we don't need parallax applied as we assume secondary occlusion is low freq maybe
			#endif
		#endif
		secUV = _UVSecOcclusionLightmapPacked==1 ? (secUV * unity_LightmapST.xy + unity_LightmapST.zw) : secUV;
		half4 occVal = tex2Dp(_OcclusionMap, secUV,  ddx(secUV),  ddy(secUV));
		half2 occ2 = half2(occVal.AMBIENT_OCCLUSION_CHANNEL, occVal.AUX_OCCLUSION_CHANNEL);
		// UV0 / UV1 occlusion switch		
		occlusion *= _UVSecOcclusion==0 ? 1 : lerp(1, occ2.x, _SecOcclusionStrength);
		#if defined(_TRANSLUCENCY)
			// translucency mask from UV1
			half translucency_thickness_fromOccMap = occ2.y;
		#endif
	#else
		half translucency_thickness_fromOccMap = 1;
	#endif
	
	#if defined(_SNOW)
		occlusion*=lerp(1, s.dissolveMaskValue, s.snowVal*_SnowDissolveMaskOcclusion);
	#endif	
	#if defined(TRIPLANAR_SELECTIVE)
		occlusion*=lerp(1, blendFade, _TriplanarBlendAmbientOcclusion);
	#endif
	#if defined(_SNOW)
		occlusion=lerp(occlusion, 1, saturate(s.snowVal*_SnowDeepSmoothen*0.15));
	#endif	
			
	#if defined(_TRANSLUCENCY)
		// UV0 / UV1 occlusion switch
		half translucency_thickness = _UVSecOcclusion==0 ? occ.y : translucency_thickness_fromOccMap;
		translucency_thickness=lerp(1, translucency_thickness, _TranslucencyOcclusion);
	#endif	
	UnityGI gi = FragmentGI (
		s.posWorld, occlusion, i.ambientOrLightmapUV, atten, s.oneMinusRoughness, s.normalWorld, s.eyeVec, dummyLight);
		
	// baked light POM self-shadows
	bool SS_flag;
	#if defined(_POM_BAKED_SELF_SHADOWS) && (defined(_PARALLAX_POM_SHADOWS) || defined(_POM_DISTANCE_MAP_SHADOWS) || defined(_POM_EXTRUSION_MAP_SHADOWS)) && defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_SEPARATE)
		//float sNdotL=dot(s.tanToWorld[2].xyz, gi.light.dir);
		//if (sNdotL>0) {
		//	float3 lightDirInTanSpace=mul(s.tanToWorld, gi.light.dir);
		//	gi.light.color *= lerp( 1, SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace), saturate(sNdotL*30));
		//}
		#if defined(_SNOW) && !defined(_POM_DISTANCE_MAP_SHADOWS) && !defined(_POM_EXTRUSION_MAP_SHADOWS)
			SS_flag=gi.light.ndotl>0 && (saturate(s.snowVal*_SnowDeepSmoothen)<0.98);
		#else
			SS_flag=gi.light.ndotl>0;
		#endif
		if (SS_flag) {
			float3 lightDirInTanSpace=mul(s.tanToWorld, gi.light.dir); // named tanToworld but this mul() actually works the opposite (as I swapped params in mul)
			#if defined(_SNOW) && !defined(_POM_DISTANCE_MAP_SHADOWS) && !defined(_POM_EXTRUSION_MAP_SHADOWS)
				gi.light.color *= SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, s.snowVal);
			#else
				#if defined(_POM_DISTANCE_MAP_SHADOWS) || defined(_POM_EXTRUSION_MAP_SHADOWS)
					gi.light.color *= lerp( SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, 0), 1, saturate( distance(i.posWorld, _WorldSpaceCameraPos) / _DepthReductionDistance ) );
				#else
					gi.light.color *= SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, 0);
				#endif				
			#endif
		}
	#endif		
	
	#if defined(_GLITTER)
		Glitter(/* inout */ s, i.tex.zw, _ddxDet, _ddyDet, i.posWorld.xyz, i.vertex_color, lerp(1, occ.y, _GlitterMask));
	#endif
		
	half3 color = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;
	color += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, occlusion, gi);

	// emissiveness always available in UBER
	#if defined(_SNOW)
		half snowBlur=_SnowDeepSmoothen*4*s.snowVal; // currently not used
		half3 snowEmissionDamp=LerpWhiteTo(_SnowEmissionTransparency, s.snowVal);
	#else
		half snowBlur=0; // not used
		half3 snowEmissionDamp=1;
	#endif
	color += Emission(i.tex.xyzw, i.vertex_color, _ddx, _ddy, snowBlur)*snowEmissionDamp; // UBER - 4 components (main uv, detail uv) & vertex colors
	color += s.additionalEmission*snowEmissionDamp; // UBER - detail/wet emission
	
	#ifndef UNITY_HDR_ON
		color.rgb = exp2(-color.rgb);
	#endif
	
	
	// translucency
	#if defined(_TRANSLUCENCY)
		half TranslucencyColor_a=_TranslucencyColor.a;
		#if defined(_TWO_LAYERS)
			TranslucencyColor_a=lerp(_TranslucencyColor2.a, _TranslucencyColor.a, i.__VERTEX_COLOR_CHANNEL_LAYER);
		#endif
		#if defined(_SNOW)
			translucency_thickness*=lerp(TranslucencyColor_a,_SnowTranslucencyColor.a, s.snowVal);
		#else
			translucency_thickness*=TranslucencyColor_a;
		#endif	
	#endif	

	outDiffuse = half4(s.diffColor, occlusion); // not used currently in Unity (5.1.1), but Alloy uses this to occlude specular light (direct)
	outSpecSmoothness = half4(s.specColor, s.oneMinusRoughness);
	// realtime light POM self-shadows
	half SS=1;
	#if defined(_POM_REALTIME_SELF_SHADOWS) && (defined(_PARALLAX_POM_SHADOWS) || defined(_POM_DISTANCE_MAP_SHADOWS) || defined(_POM_EXTRUSION_MAP_SHADOWS))
		float3 lightDir=(DeferredLightDir(i.posWorld.xyz));
		#if defined(_SNOW) && !defined(_POM_DISTANCE_MAP_SHADOWS) && !defined(_POM_EXTRUSION_MAP_SHADOWS)
			SS_flag=dot(lightDir, s.normalWorld)>0 && (saturate(s.snowVal*_SnowDeepSmoothen)<0.98);
		#else
			SS_flag=dot(lightDir, s.normalWorld)>0;
		#endif
		if (SS_flag) {
			float3 lightDirInTanSpace=mul(s.tanToWorld, lightDir); // named tanToworld but this mul() actually works the opposite (as I swapped params in mul)
			#if defined(_SNOW) && !defined(_POM_DISTANCE_MAP_SHADOWS) && !defined(_POM_EXTRUSION_MAP_SHADOWS)
				SS = SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, s.snowVal);
			#else
				#if defined(_POM_DISTANCE_MAP_SHADOWS) || defined(_POM_EXTRUSION_MAP_SHADOWS)
					SS = lerp( SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, 0), 1, saturate( distance(i.posWorld, _WorldSpaceCameraPos) / _DepthReductionDistance ) );
				#else
					SS = SelfShadows(s.rayPos, s.texture2ObjectRatio, lightDirInTanSpace, 0);
				#endif				
			#endif
		}
	#endif		
	outNormal = half4(s.normalWorld*0.5+0.5, SS); // UBER (self-shadowing in POM) - _realtime_ light
	
	#if defined(_TRANSLUCENCY)
		float encoded=floor(saturate(translucency_thickness*_TranslucencyStrength)*255); // 0..255 (HDR) (0..63 for LDR - TODO) translucency levels
		encoded=encoded*4+_TranslucencyDeferredLightIndex; // + 0..3 light color index
		encoded/=1023.0; // bring back range from 0..255 (HDR) (0..63 for LDR - TODO) to 0..1
		outEmission = half4(color, 1-encoded);
	#else
		outEmission = half4(color, 1);
	#endif
	
	#if defined(ZWRITE)
		//float depthWithOffset = i.posWorld.w+s.rayLength;
		float depthWithOffset = i.posWorld.w*(1+s.rayLength/distance(i.posWorld.xyz, _WorldSpaceCameraPos)); // Z-DEPTH perspective correction
		outDepth = (1.0 - depthWithOffset * _ZBufferParams.w) / (depthWithOffset * _ZBufferParams.z);
	#endif	
}					


// ------------------------------------------------------------------------------------
//
// tessellation
//
#if defined(_TESSELLATION) && defined(UNITY_CAN_COMPILE_TESSELLATION) && !defined(UNITY_PASS_META)

	struct UnityTessellationFactors {
	    float edge[3] : SV_TessFactor;
	    float inside : SV_InsideTessFactor;
	};

	// tessellation vertex shader
	struct InternalTessInterp_appdata {
	  float4 vertex : INTERNALTESSPOS;
	  float3 normal : NORMAL;
	  float2 uv1 : TEXCOORD1;
	  float2 uv2 : TEXCOORD2;
	  float4 color : COLOR;
	  #if !defined(TRIPLANAR_SELECTIVE)
	  float4 tangent : TANGENT;
	  float2 uv0 : TEXCOORD0;
	  #endif
	};
	InternalTessInterp_appdata tessvert_surf (VertexInput v) {
	  InternalTessInterp_appdata o;
	  o.vertex = v.vertex;
	  o.normal = v.normal;
	  o.uv1 = v.uv1;
	  o.uv2 = v.uv2;
	  o.color = v.color;
	  #if !defined(TRIPLANAR_SELECTIVE)
	  o.tangent = v.tangent;
	  o.uv0 = v.uv0;
	  #endif
	  return o;
	}

	// tessellation hull constant shader
	UnityTessellationFactors hsconst_surf (InputPatch<InternalTessInterp_appdata,3> v) {
	  UnityTessellationFactors o;
	  float4 tf;
	  VertexInput vi[3];
	  vi[0].vertex = v[0].vertex;
	  vi[0].normal = v[0].normal;
	  vi[0].uv1 = v[0].uv1;
	  vi[0].uv2 = v[0].uv2;
	  vi[0].color = v[0].color;
	  vi[1].vertex = v[1].vertex;
	  vi[1].normal = v[1].normal;
	  vi[1].uv1 = v[1].uv1;
	  vi[1].uv2 = v[1].uv2;
	  vi[1].color = v[1].color;
	  vi[2].vertex = v[2].vertex;
	  vi[2].normal = v[2].normal;
	  vi[2].uv1 = v[2].uv1;
	  vi[2].uv2 = v[2].uv2;
	  vi[2].color = v[2].color;
	  #if !defined(TRIPLANAR_SELECTIVE)
	  vi[0].tangent = v[0].tangent;
	  vi[0].uv0 = v[0].uv0;
	  vi[1].tangent = v[1].tangent;
	  vi[1].uv0 = v[1].uv0;
	  vi[2].tangent = v[2].tangent;
	  vi[2].uv0 = v[2].uv0;
	  #endif
	  tf = tessDistanceWithEdgeLimit(vi[0], vi[1], vi[2]);
	  o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
	  return o;
	}

	// tessellation hull shader
	[UNITY_domain("tri")]
	[UNITY_partitioning("fractional_odd")]
	[UNITY_outputtopology("triangle_cw")]
	[UNITY_patchconstantfunc("hsconst_surf")]
	[UNITY_outputcontrolpoints(3)]
	InternalTessInterp_appdata hs_surf (InputPatch<InternalTessInterp_appdata,3> v, uint id : SV_OutputControlPointID) {
	  return v[id];
	}

	// tessellation domain shader
	[UNITY_domain("tri")]
	v2f_struct ds_surf (UnityTessellationFactors tessFactors, const OutputPatch<InternalTessInterp_appdata,3> vi, float3 bary : SV_DomainLocation) {
	  VertexInput v;
	  v.vertex = vi[0].vertex*bary.x + vi[1].vertex*bary.y + vi[2].vertex*bary.z;
	  v.normal = vi[0].normal*bary.x + vi[1].normal*bary.y + vi[2].normal*bary.z;
	  v.uv1 = vi[0].uv1*bary.x + vi[1].uv1*bary.y + vi[2].uv1*bary.z;
	  v.uv2 = vi[0].uv2*bary.x + vi[1].uv2*bary.y + vi[2].uv2*bary.z;
	  v.color = vi[0].color*bary.x + vi[1].color*bary.y + vi[2].color*bary.z;
	  #if !defined(TRIPLANAR_SELECTIVE)
	  v.tangent = vi[0].tangent*bary.x + vi[1].tangent*bary.y + vi[2].tangent*bary.z;
	  v.uv0 = vi[0].uv0*bary.x + vi[1].uv0*bary.y + vi[2].uv0*bary.z;
	  #endif
	  
 	  //
	  // compute displacement
	  //
	  #if defined(_TESSELLATION_DISPLACEMENT)
		fixed4 vertex_color=v.color;
		float3 normalWorld = UnityObjectToWorldNormal(v.normal);
		
		#if defined(_SNOW) || defined(TRIPLANAR_SELECTIVE)
			float4 posWorld = mul(_Object2World, v.vertex);
		#endif
		
		
		#if defined(_SNOW)
			half _snow_val = _SnowColorAndCoverage.a*__VERTEX_COLOR_CHANNEL_SNOW;
			_snow_val *= saturate((posWorld.y-_SnowHeightThreshold)/_SnowHeightThresholdTransition);
			_snow_val = saturate( _snow_val - (1-normalWorld.y)*_SnowSlopeDamp );
			_snow_val *= _SnowLevelFromGlobal ? (1-_UBER_GlobalSnowDamp) : 1;
		#endif
	  
		float d=0; // displacement value
		#if defined(TRIPLANAR_SELECTIVE)
			#if defined(_TRIPLANAR_WORLD_MAPPING)
				float3 normBlend=normalWorld;
				float3 posUVZ=posWorld.xyz;
				float3 blendVal = abs(normBlend);
			#else
				float scaleX = length(float3(_Object2World[0][0], _Object2World[1][0], _Object2World[2][0]));
				float scaleY = length(float3(_Object2World[0][1], _Object2World[1][1], _Object2World[2][1]));
				float scaleZ = length(float3(_Object2World[0][2], _Object2World[1][2], _Object2World[2][2]));
				
				float3 objScale=float3(scaleX, scaleY, scaleZ);
				float3 normObj=v.normal;
				float3 normBlend=normObj;
				float3 normObjScaled=normalize(normObj/objScale);
				float3 posUVZ=v.vertex.xyz*objScale;
				float3 blendVal = abs(normObjScaled);
			#endif	
			
			#if defined(_SNOW)
				float level=_SnowDeepSmoothen*saturate(_snow_val-0.3);
			#else
				float level=0;
			#endif
						
			float3 uvz = posUVZ.xyz*_MainTex_ST.xxx;
			half3 hVal = float3(tex2Dlod(_ParallaxMap, (normBlend.x>0) ? float4(uvz.zy, level.xx) : float4(-uvz.z,uvz.y, level.xx)).PARALLAX_CHANNEL, tex2Dlod(_ParallaxMap, (normBlend.y>0) ? float4(uvz.xz, level.xx) : float4(-uvz.x,uvz.z, level.xx)).PARALLAX_CHANNEL, tex2Dlod(_ParallaxMap, (normBlend.z>0) ? float4(uvz.yx, level.xx) : float4(-uvz.y,uvz.x, level.xx)).PARALLAX_CHANNEL);
			#if defined(_TWO_LAYERS)
				float3 uvz2 = posUVZ.xyz*_DetailAlbedoMap_ST.xxx;
				#if defined(_PARALLAX_MAP_2MAPS)
					half3 hVal2 = float3(tex2Dlod(_ParallaxMap2, (normBlend.x>0) ? float4(uvz2.zy, level.xx) : float4(-uvz2.z,uvz2.y, level.xx)).PARALLAX_CHANNEL, tex2Dlod(_ParallaxMap2, (normBlend.y>0) ? float4(uvz2.xz, level.xx) : float4(-uvz2.x,uvz2.z, level.xx)).PARALLAX_CHANNEL, tex2Dlod(_ParallaxMap2, (normBlend.z>0) ? float4(uvz2.yx, level.xx) : float4(-uvz2.y,uvz2.x, level.xx)).PARALLAX_CHANNEL);
				#else
					half3 hVal2 = float3(tex2Dlod(_ParallaxMap2, (normBlend.x>0) ? float4(uvz2.zy, level.xx) : float4(-uvz2.z,uvz2.y, level.xx)).PARALLAX_CHANNEL_2ND_LAYER, tex2Dlod(_ParallaxMap2, (normBlend.y>0) ? float4(uvz2.xz, level.xx) : float4(-uvz2.x,uvz2.z, level.xx)).PARALLAX_CHANNEL_2ND_LAYER, tex2Dlod(_ParallaxMap2, (normBlend.z>0) ? float4(uvz2.yx, level.xx) : float4(-uvz2.y,uvz2.x, level.xx)).PARALLAX_CHANNEL_2ND_LAYER);
				#endif
				hVal = lerp( hVal2, hVal, __VERTEX_COLOR_CHANNEL_LAYER);
			#endif
			
			blendVal += _TriplanarHeightmapBlendingValue*hVal;
			blendVal /= dot(blendVal,1);
			blendVal*=blendVal;
			blendVal*=blendVal;
			blendVal /= dot(blendVal,1);
			
			#if defined(_TWO_LAYERS)
				// need to call GetH to set height blending between layers
				{
				float2 control=float2(__VERTEX_COLOR_CHANNEL_LAYER, 1-__VERTEX_COLOR_CHANNEL_LAYER);
				float2 hgt=float2(dot(hVal2, blendVal), dot(hVal, blendVal));
				control*=hgt+0.01;			// height evaluation
				control*=control; 			// compress
				control/=dot(control,1);	// normalize
				// no more compression to get smoother cross layer displacement blend
//				control*=control;			// compress
//				control*=control;			// compress
//				control/=dot(control,1);	// normalize
				
				__VERTEX_COLOR_CHANNEL_LAYER=control.x; // write blending value back into the right vertex_color channel variable
				d = lerp(hgt.x, hgt.y, __VERTEX_COLOR_CHANNEL_LAYER);
				}
			#else
				d = dot(hVal, blendVal);
			#endif
			
		#else
			float4 texcoords;
			texcoords.xy=TRANSFORM_TEX(v.uv0, _MainTex);
			texcoords.zw=TRANSFORM_TEX(((_UVSec == 0) ? v.uv0 : v.uv1), _DetailAlbedoMap);
			#if defined(_SNOW)
				d=GetH(vertex_color, texcoords, true, _SnowDeepSmoothen*saturate(_snow_val-0.3)); // true - we're sampling tex2Dlod in per vertex on the _SnowDeepSmoothen*_snow_val level
			#else
				d=GetH(vertex_color, texcoords, true, 0);
			#endif
		#endif
	  
	  #if defined(_SNOW)
			d = saturate( d + saturate(_snow_val-0.7)*0.05*_SnowDeepSmoothen); // TODO - this offset should be calculated a lot more precise (on the surface normal level and in worldspace, because now it's object scale dependent)
	  #endif	  
	  
	  #if defined(DISPLACE_IN_TEXTURE_UNITS)	  
	  	float approxTan2ObjectRatio=distance(vi[1].vertex, vi[0].vertex) / distance(TRANSFORM_TEX(vi[0].uv0, _MainTex), TRANSFORM_TEX(vi[1].uv0, _MainTex));
	  #else
	  	float approxTan2ObjectRatio=1;
	  #endif
	  d = d - _TessOffset;
	  #if defined(GEOM_BLEND)
		d = lerp(d, 0, vertex_color.VERTEX_COLOR_CHANNEL_GEOM_BLEND);
	  #endif
	// displacement
	#endif
	  
	if (_Phong>0) {
		float3 pp[3];
		for (int i = 0; i < 3; ++i)
		pp[i] = v.vertex.xyz - vi[i].normal * (dot(v.vertex.xyz, vi[i].normal) - dot(vi[i].vertex.xyz, vi[i].normal));
		v.vertex.xyz = _Phong * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-_Phong) * v.vertex.xyz;
	}
	  	  	
	#if defined(_TESSELLATION_DISPLACEMENT)
	  v.vertex.xyz += v.normal * d * _TessDepth * approxTan2ObjectRatio;
	#endif
	  

	  v2f_struct o = VERT_SURF(v);
	  return o;
	}

#endif // TESSELLATION

//============================= META ==============================
#if UNITY_PASS_META
struct v2f_meta
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0; // normal in triplanar
	half4 tangentToWorldAndParallax0	: TEXCOORD1;	// [3x3:tangentToWorld | 1x3:viewDirForParallax] - note: tangents+obj scale in triplanar (tangents in world space when mapping in world space)
	half4 tangentToWorldAndParallax1	: TEXCOORD2;	// (array fails in GLSL optimizer)
	half4 tangentToWorldAndParallax2	: TEXCOORD3;
	half3 eyeVec						: TEXCOORD4;
	fixed4 vertex_color					: COLOR0;
	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
	float3 posObject					: TEXCOORD5;
	#endif	
	#if UNITY_SPECCUBE_BOX_PROJECTION || defined(POM) || defined(_TRANSLUCENCY) || defined(_GLITTER) || defined(_SNOW) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP) || defined(TRIPLANAR_SELECTIVE)
		float3 posWorld					: TEXCOORD6;
	#endif	
};

v2f_meta vert_meta (VertexInput v)
{
	v2f_meta o;
	UNITY_INITIALIZE_OUTPUT(v2f_meta, o);
	
	// input v.vertex is actually used for nothing now, but openGL needs such implicit input ?
	o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
	
	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		o.posObject.xyz = v.vertex.xyz;
	#endif
	
	float4 posWorld = mul(_Object2World, v.vertex);
	#if UNITY_SPECCUBE_BOX_PROJECTION || defined(POM) || defined(_TRANSLUCENCY) || defined(_GLITTER) || defined(_SNOW) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP) || defined(TRIPLANAR_SELECTIVE)
		o.posWorld.xyz = posWorld.xyz;
	#endif
	o.vertex_color = v.color;
	o.eyeVec = normalize(posWorld.xyz - _WorldSpaceCameraPos);	
	
	#if !defined(TRIPLANAR_SELECTIVE)
		//float3 normalWorld = UnityObjectToWorldNormal(v.normal); // FIXME - for unknown reason normalWorld isn't actually computed here (either _World2Object, _Object2World or v.vnormal has incorrect values here)
		float3 normalWorld = UnityObjectToWorldDir(v.normal.xyz); // still can't tell if this is any better...
	#endif
	
	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			float3 normalWorld = UnityObjectToWorldDir(v.normal.xyz);
			SetupUBER_VertexData_TriplanarWorld(normalWorld, /* inout */ o.tangentToWorldAndParallax0, /* inout */ o.tangentToWorldAndParallax1, /* inout */ o.tangentToWorldAndParallax2);
		#else
			float scaleX, scaleY, scaleZ;
			SetupUBER_VertexData_TriplanarLocal(v.normal, /* inout */ o.tangentToWorldAndParallax0, /* inout */ o.tangentToWorldAndParallax1, /* inout */ o.tangentToWorldAndParallax2, /* out */ scaleX, /* out */ scaleY, /* out */ scaleZ);
			o.posObject.xyz = v.vertex.xyz;
		#endif		
	#elif defined(_TANGENT_TO_WORLD)	
		float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

		float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
		o.tangentToWorldAndParallax0.xyz = tangentToWorld[0];
		o.tangentToWorldAndParallax1.xyz = tangentToWorld[1];
		o.tangentToWorldAndParallax2.xyz = tangentToWorld[2];
	#else
		o.tangentToWorldAndParallax0.xyz = 0;
		o.tangentToWorldAndParallax1.xyz = 0;
		o.tangentToWorldAndParallax2.xyz = normalWorld;
	#endif	
	
	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			// .w component not used
		#else
			o.tangentToWorldAndParallax0.w=scaleX;
			o.tangentToWorldAndParallax1.w=scaleY;
			o.tangentToWorldAndParallax2.w=scaleZ;
		#endif
	#endif
		
	#if defined(TRIPLANAR_SELECTIVE)
		#if defined(_TRIPLANAR_WORLD_MAPPING)
			o.tex = float4(normalWorld,0);
			// o.tangentToWorldAndParallax[n].w component not used
		#else
			o.tex = float4(v.normal,0);
			o.tangentToWorldAndParallax0.w=scaleX;
			o.tangentToWorldAndParallax1.w=scaleY;
			o.tangentToWorldAndParallax2.w=scaleZ;
		#endif
	#else
		o.tex = TexCoords(v);
	#endif	
	
	return o;
}

// Albedo for lightmapping should basically be diffuse color.
// But rough metals (black diffuse) still scatter quite a lot of light around, so
// we want to take some of that into account too.
half3 UnityLightmappingAlbedo (half3 diffuse, half3 specular, half oneMinusRoughness)
{
	half roughness = 1 - oneMinusRoughness;
	half3 res = diffuse;
	res += specular * roughness * roughness * 0.5;
	return res;
}

float4 frag_meta (v2f_meta i) : SV_Target
{
	#if UNITY_SPECCUBE_BOX_PROJECTION || defined(POM) || defined(_TRANSLUCENCY) || defined(_GLITTER) || defined(_SNOW) || defined(DISTANCE_MAP) || defined(EXTRUSION_MAP) || defined(TRIPLANAR_SELECTIVE)
		float3 posWorld=i.posWorld.xyz;
	#else
		float3 posWorld=0;
	#endif
	
	half actH;
	float4 SclCurv;
	half3 eyeVec;
	
	float3 tangentBasisScaled;
	
	float2 _ddx;
	float2 _ddy;
	float2 _ddxDet;
	float2 _ddyDet;
	float blendFade;
	
	half3 i_viewDirForParallax;
	half3x3 _TBN;
	half3 worldNormal;
	
	float4 texcoordsNoTransform;
	
	// void	SetupUBER(float4 i_SclCurv, half3 i_eyeVec, float3 i_posWorld, float3 i_posObject, inout float4 i_tex, inout half4 i_tangentToWorldAndParallax0, inout half4 i_tangentToWorldAndParallax1, inout half4 i_tangentToWorldAndParallax2, inout fixed4 vertex_color, out half actH, out float4 SclCurv, out half3 eyeVec, out float3 tangentBasisScaled, out float2 _ddx, out float2 _ddy, out float2 _ddxDet, out float2 _ddyDet, out float blendFade, out half3 i_viewDirForParallax, out half3x3 _TBN, out half3 worldNormal) {
	#if defined(TRIPLANAR_SELECTIVE) && !defined(_TRIPLANAR_WORLD_MAPPING)
		SetupUBER(float4(0,0,0,0), half3(0,0,0), posWorld, i.posObject.xyz, /* inout */ i.tex, /* inout */ i.tangentToWorldAndParallax0, /* inout */ i.tangentToWorldAndParallax1, /* inout */ i.tangentToWorldAndParallax2, /* inout */ i.vertex_color, /* out */ actH, /* out */ SclCurv, /* out */ eyeVec, /* out */ tangentBasisScaled, /* out */ _ddx, /* out */ _ddy, /* out */ _ddxDet, /* out */ _ddyDet, /* out */ blendFade, /* out */ i_viewDirForParallax, /* out */ _TBN, /* out */ worldNormal, /* out */ texcoordsNoTransform);
	#else
		SetupUBER(float4(0,0,0,0), half3(0,0,0), posWorld, float3(0,0,0), /* inout */ i.tex, /* inout */ i.tangentToWorldAndParallax0, /* inout */ i.tangentToWorldAndParallax1, /* inout */ i.tangentToWorldAndParallax2, /* inout */ i.vertex_color, /* out */ actH, /* out */ SclCurv, /* out */ eyeVec, /* out */ tangentBasisScaled, /* out */ _ddx, /* out */ _ddy, /* out */ _ddxDet, /* out */ _ddyDet, /* out */ blendFade, /* out */ i_viewDirForParallax, /* out */ _TBN, /* out */ worldNormal, /* out */ texcoordsNoTransform);
	#endif
	// ------	
	
	// inline FragmentCommonData FragmentSetup (inout float4 i_tex, half3 i_eyeVec, half3 i_normalWorld, inout half3 i_viewDirForParallax, inout half3x3 i_tanToWorld, half3 i_posWorld, inout fixed4 vertex_color, float2 _ddx, float2 _ddy, float2 _ddxDet, float2 _ddyDet, float3 tangentBasisScaled, float4 SclCurv, float blendFade, half actH) // UBER - additional params added
//	FragmentSetup(i.tex, eyeVec, worldNormal, i_viewDirForParallax, _TBN, IN_WORLDPOS(i), i.vertex_color, _ddx, _ddy, _ddxDet, _ddyDet, tangentBasisScaled, SclCurv, blendFade, actH); // UBER - additional params added
	FragmentCommonData s = FragmentSetup(i.tex, float3(0,0,0), worldNormal, i_viewDirForParallax, _TBN, posWorld, i.vertex_color, _ddx, _ddy, _ddxDet, _ddyDet, tangentBasisScaled, SclCurv, 1, actH);
	#if defined(_SNOW)
		half snowBlur=_SnowDeepSmoothen*4*s.snowVal; // currently not used
		half3 snowEmissionDamp=LerpWhiteTo(_SnowEmissionTransparency, s.snowVal);
	#else
		half snowBlur=0; // not used
		half3 snowEmissionDamp=1;
	#endif
	
	UnityMetaInput o;
	UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

	o.Albedo = UnityLightmappingAlbedo (s.diffColor, s.specColor, s.oneMinusRoughness);
	o.Emission = Emission(i.tex.xyzw, i.vertex_color, _ddx, _ddy, snowBlur)*snowEmissionDamp; // UBER - 4 components (main uv, detail uv) + vertex color (for masking), under snow blurring
	o.Emission += s.additionalEmission*snowEmissionDamp; // UBER - detail/wet emission
	return UnityMetaFragment(o);
}
#endif // META
//============================= META ==============================

#endif // UBER_STANDARD_CORE_INCLUDED
