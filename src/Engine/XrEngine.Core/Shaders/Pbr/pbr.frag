#include "uniforms.glsl"

#define TM_NONE        0
#define TM_FB_FETCH    1
#define TM_DUAL_SOURCE 2
#define TM_TEXTURE     3


#if (defined(USE_NORMAL_MAP) || defined(USE_CLEARCOAT_NORMAL_MAP) || defined(USE_ANISOTROPY)) && defined(HAS_TANGENTS) 
	#define HAS_TANGENT_BASIS
	in mat3 fTangentBasis;
#endif


#ifndef TRANSMISSION_MODE
	#define TRANSMISSION_MODE TM_NONE
#endif

#ifdef HAS_ENV_DEPTH
	#include "../Shared/env_depth.glsl"
#endif

#ifdef PLANAR_REFLECTION
	#include "../Shared/planar_reflection.glsl"
#endif

#ifdef USE_SHADOW_MAP
	#include "../Shared/shadow.glsl"
#endif

#include "../Shared/fragment_post.glsl"

#if defined(USE_VOLUME) || TRANSMISSION_MODE == TM_TEXTURE
	#include "../Shared/position.glsl"
	#include "volume.glsl"
#endif

#ifdef USE_IRIDESCENCE
	#include "iridescence.glsl"
#endif

#ifdef USE_SHEEN
	#include "sheen.glsl"
#endif

#ifdef USE_CLEARCOAT
	#include "clearcoat.glsl"
#endif

#ifdef USE_ANISOTROPY
	#include "anisotropy.glsl"
#endif


#if !defined(HAS_CLIP_VOLUME) && !defined(HAS_COLORMAP_PROJ) && ALPHA_MODE != ALPHA_MASK
	layout(early_fragment_tests) in;
#endif

const float PI = 3.141592;
const float Epsilon = 0.00001;

const vec3 Fdielectric = vec3(0.04);

#if ALPHA_MODE != ALPHA_OPAQUE && ALPHA_MODE != ALPHA_MASK && defined(USE_ALPHA_SPECULAR)
	#define ALPHA_SPECULAR
#endif

#ifdef ALPHA_SPECULAR
	float specularStrength;
#endif

#define DEBUG_UV         1
#define DEBUG_NORMAL     2
#define DEBUG_TANGENT    3
#define DEBUG_BITANGENT  4
#define DEBUG_METALNESS  5
#define DEBUG_ROUGHNESS  6
#define DEBUG_IRRADIANCE 7
#define DEBUG_FIELD_DIR  8
#define DEBUG_FIELD_RAD  9
#define DEBUG_TRANSMISSION 10

#ifndef PBR_MIN_ROUGHNESS
	#define PBR_MIN_ROUGHNESS 0.045
#endif

#ifndef PBR_USE_PHYSICAL_DIRECT_DIFFUSE
	#define PBR_USE_PHYSICAL_DIRECT_DIFFUSE 1
#endif

#ifndef PBR_OCCLUSION_AFFECTS_DIRECT
	#define PBR_OCCLUSION_AFFECTS_DIRECT 0
#endif

#ifndef ALBEDO_UV_SET
	#define ALBEDO_UV_SET 0
#endif


in vec3 fNormal;
in vec3 fPos;
in vec2 fUv;
in vec3 fCameraPos;


#if defined(HAS_UV2) 
	in vec2 fUv2;
#endif

#ifdef USE_SHADOW_MAP
	in vec4 fPosLightSpace;
#endif

#ifdef HAS_COLORMAP_PROJ
	in vec4 fProjCoord;
#endif

#if TRANSMISSION_MODE == TM_FB_FETCH
	layout(location=0) inout vec4 color;
#elif TRANSMISSION_MODE == TM_DUAL_SOURCE
	layout(location=0, index=0) out vec4 color;
	layout(location=0, index=1) out vec4 transmissionBlend;
#else
	layout(location=0) out vec4 color;
#endif

layout(binding=IBLGGXENV_SLOT) uniform samplerCube specularEnvTexture;
layout(binding=IBLLAMBERTIANENV_SLOT) uniform samplerCube irradianceTexture;
layout(binding=IBLGGXLUT_SLOT) uniform sampler2D specularBRDF_LUT;


#ifdef HAS_CLIP_VOLUME
	uniform vec3 uClipMin;
	uniform vec3 uClipMax;
#endif

struct FragmentProperties
{
	vec3 position;
	vec2 uv0;
	vec2 uv1;

	vec4 baseColor;
	vec3 albedo;
	vec3 normal;
	vec3 normalGeo;

	float metalness;
	float roughness;
	float occlusion;

	vec3 viewDir;

	vec4 emissive;

	float transmission;	

	vec3 sheenColor;
	float sheenRoughness;

	float clearCoat;
	float clearCoatRoughness;
	vec3 clearCoatNormal;

	float specular;
	vec3 specularColor;

	float thickness;

	float dispersion;

	vec3 anisotropy;
};

struct LightingProperties
{
	float NoV;
	vec3 reflectionDir;
	vec3 dielectricF0;
	vec3 dielectricF0Mixed;
	float transmissionRoughnessScale;
	float clearCoatSurfaceWeight;
	vec3 anisotropicT;
	vec3 anisotropicB;
};

FragmentProperties frag;
LightingProperties lighting;

#ifdef USE_IRIDESCENCE
	float iridescenceFactor;
	vec3 iridescenceFresnelDielectric;
	vec3 iridescenceFresnelMetal;
#endif

float saturate(float v)
{
	return clamp(v, 0.0, 1.0);
}

vec3 saturate(vec3 v)
{
	return clamp(v, vec3(0.0), vec3(1.0));
}

float square(float v)
{
	return v * v;
}

float max3(vec3 v)
{
	return max(v.r, max(v.g, v.b));
}

vec3 getDielectricF0()
{
	#ifdef HAS_IOR
		vec3 F0 = vec3(square((uMaterial.ior - 1.0) / (uMaterial.ior + 1.0)));
	#else
		vec3 F0 = Fdielectric;
	#endif

	#ifdef USE_SPECULAR
		F0 = min(F0 * frag.specularColor, vec3(1.0));
	#endif

	return F0;
}


float distributionGGX(float NoH, float roughness)
{
	float r = max(roughness, PBR_MIN_ROUGHNESS);
	float alpha = r * r;
	float alpha2 = alpha * alpha;

	float d = NoH * NoH * (alpha2 - 1.0) + 1.0;
	return alpha2 / max(Epsilon, PI * d * d);
}

float geometrySmith(float NoL, float NoV, float roughness)
{
	float r = max(roughness, PBR_MIN_ROUGHNESS) + 1.0;
	float k = (r * r) * 0.125;
	float invK = 1.0 - k;

	float GL = NoL / max(Epsilon, NoL * invK + k);
	float GV = NoV / max(Epsilon, NoV * invK + k);

	return GL * GV;
}

vec3 fresnelSchlick(vec3 F0, float cosTheta)
{
	float f = pow5(1.0 - saturate(cosTheta));
	return F0 + (vec3(1.0) - F0) * f;
}

vec3 fresnelSchlickRoughness(vec3 F0, float cosTheta, float roughness)
{
	float f = pow5(1.0 - saturate(cosTheta));
	return F0 + (max(vec3(1.0 - roughness), F0) - F0) * f;
}

float pointLightAttenuation(float distanceSq, float rangeSq)
{
	float d2 = distanceSq / max(rangeSq, Epsilon * Epsilon);
	float rangeFalloff = saturate(1.0 - d2 * d2);
	return (rangeFalloff * rangeFalloff) / max(distanceSq, 0.01);
}

vec3 evaluateDirectLight(vec3 L, vec3 radiance)
{
	float NoL = saturate(dot(frag.normal, L));

	if (NoL <= 0.0)
		return vec3(0.0);

#ifdef SIMPLIFIED
	#if defined(USE_TRANSMISSION)
		return frag.albedo * radiance * NoL * (1.0 - frag.transmission);
	#else
		return frag.albedo * radiance * NoL;
	#endif
#else
	vec3 H = normalize(L + frag.viewDir);

	float NoH = saturate(dot(frag.normal, H));
	float VoH = saturate(dot(frag.viewDir, H));

	#ifdef USE_SPECULAR
		vec3 dielectricF = fresnelSchlick(lighting.dielectricF0, VoH) * frag.specular;
		vec3 metalF = fresnelSchlick(frag.albedo, VoH);
		vec3 F = mix(dielectricF, metalF, frag.metalness);

		vec3 kd = vec3(1.0 - max3(dielectricF)) * (1.0 - frag.metalness);
	#else
		vec3 F = fresnelSchlick(lighting.dielectricF0Mixed, VoH);
		vec3 kd = (vec3(1.0) - F) * (1.0 - frag.metalness);
	#endif

	float specularTerm;

	#ifdef USE_ANISOTROPY

		if (frag.anisotropy.b > 0.0)
		{
			float ToV = dot(lighting.anisotropicT, frag.viewDir);
			float BoV = dot(lighting.anisotropicB, frag.viewDir);
			float ToL = dot(lighting.anisotropicT, L);
			float BoL = dot(lighting.anisotropicB, L);
			float ToH = dot(lighting.anisotropicT, H);
			float BoH = dot(lighting.anisotropicB, H);

			float alphaRoughness = square(max(frag.roughness, PBR_MIN_ROUGHNESS));
			float at = mix(alphaRoughness, 1.0, frag.anisotropy.b * frag.anisotropy.b);
			float ab = alphaRoughness;

			float D = distributionGGXAnisotropic(NoH, ToH, BoH, at, ab);
			float Vis = visibilityGGXAnisotropic(NoL, lighting.NoV, BoV, ToV, ToL, BoL, at, ab);

			specularTerm = D * Vis;
		}
		else
		{
			float D = distributionGGX(NoH, frag.roughness);
			float G = geometrySmith(NoL, lighting.NoV, frag.roughness);
			specularTerm = (D * G) / max(Epsilon, 4.0 * NoL * lighting.NoV);
		}

	#else

		float D = distributionGGX(NoH, frag.roughness);
		float G = geometrySmith(NoL, lighting.NoV, frag.roughness);
		specularTerm = (D * G) / max(Epsilon, 4.0 * NoL * lighting.NoV);

	#endif

	#if PBR_USE_PHYSICAL_DIRECT_DIFFUSE
		vec3 diffuseBRDF = kd * frag.albedo * (1.0 / PI);
	#else
		vec3 diffuseBRDF = kd * frag.albedo;
	#endif

	#if defined(USE_TRANSMISSION)
		diffuseBRDF *= 1.0 - frag.transmission;
	#endif

	vec3 specularBRDF = F * specularTerm;
	vec3 lightRadiance = radiance * NoL;

	#ifdef ALPHA_SPECULAR
		vec3 specularLighting = specularBRDF * lightRadiance;
		specularStrength += max3(specularLighting);
	#endif

	vec3 brdf = diffuseBRDF + specularBRDF;

	#ifdef USE_IRIDESCENCE

		#if PBR_USE_PHYSICAL_DIRECT_DIFFUSE
			vec3 iridescenceDiffuseBRDF = frag.albedo * (1.0 / PI);
		#else
			vec3 iridescenceDiffuseBRDF = frag.albedo;
		#endif

		#if defined(USE_TRANSMISSION)
			iridescenceDiffuseBRDF *= 1.0 - frag.transmission;
		#endif

		vec3 iridescenceSpecularBRDF = vec3(specularTerm);
		vec3 iridescenceDielectricBRDF = rgbMix(iridescenceDiffuseBRDF, iridescenceSpecularBRDF, iridescenceFresnelDielectric);
		vec3 iridescenceMetalBRDF = iridescenceSpecularBRDF * iridescenceFresnelMetal;
		vec3 iridescenceBRDF = mix(iridescenceDielectricBRDF, iridescenceMetalBRDF, frag.metalness);
		brdf = mix(brdf, iridescenceBRDF, iridescenceFactor);
	#endif

	#ifdef USE_SHEEN
		float sheenSpecularStrength;
		float sheenScaling;
		vec3 sheen = evaluateSheenDirect(
			frag.sheenColor,
			frag.sheenRoughness,
			lighting.NoV,
			NoL,
			NoH,
			radiance,
			sheenSpecularStrength,
			sheenScaling);

		brdf = brdf * sheenScaling + sheen;

		#ifdef ALPHA_SPECULAR
			vec3 sheenLighting = sheen * lightRadiance;
			specularStrength += max3(sheenLighting);
		#endif
	#endif

	return brdf * lightRadiance;
#endif
}


#ifdef USE_TRANSMISSION

float visibilityGGXTransmission(float NoL, float NoV, float roughness)
{
	float r = max(roughness, PBR_MIN_ROUGHNESS);
	float alpha = r * r;
	float alpha2 = alpha * alpha;

	float GGXV = NoL * sqrt(NoV * NoV * (1.0 - alpha2) + alpha2);
	float GGXL = NoV * sqrt(NoL * NoL * (1.0 - alpha2) + alpha2);
	float GGX = GGXV + GGXL;

	return GGX > Epsilon ? 0.5 / GGX : 0.0;
}

vec3 evaluateDirectTransmission(vec3 L, vec3 radiance)
{
	float NoL = dot(frag.normal, L);
	float NoLt = abs(NoL);

	if (NoLt <= Epsilon)
		return vec3(0.0);

	vec3 Lt = L - 2.0 * frag.normal * NoL;
	vec3 H = normalize(Lt + frag.viewDir);
	float NoH = saturate(dot(frag.normal, H));
	float VoH = saturate(dot(frag.viewDir, H));

	float transmissionRoughness = frag.roughness;

	#ifdef HAS_IOR
		transmissionRoughness *= lighting.transmissionRoughnessScale;
	#endif

	#ifdef USE_SPECULAR
		vec3 F = fresnelSchlick(lighting.dielectricF0, VoH) * frag.specular;
	#else
		vec3 F = fresnelSchlick(lighting.dielectricF0, VoH);
	#endif

	float D = distributionGGX(NoH, transmissionRoughness);
	float Vis = visibilityGGXTransmission(NoLt, lighting.NoV, transmissionRoughness);

	vec3 transmissionWeight = (vec3(1.0) - F) * (1.0 - frag.metalness) * frag.transmission;

	#ifdef USE_IRIDESCENCE
		float iridescenceBaseWeight = 1.0 - max3(iridescenceFresnelDielectric);

		transmissionWeight = mix(
			transmissionWeight,
			vec3(iridescenceBaseWeight) * (1.0 - frag.metalness) * frag.transmission,
			iridescenceFactor);
	#endif

	return frag.albedo * transmissionWeight * D * Vis * radiance;
}

#endif


#ifdef USE_LIGHT_FIELD
	#include "../Shared/light_field.glsl"
#endif

vec3 evaluatePunctualLighting(out vec3 shadowLightDir, out vec3 clearCoatLighting)
{
	vec3 directLighting = vec3(0.0);
	clearCoatLighting = vec3(0.0);
	shadowLightDir = vec3(0.0, 1.0, 0.0);

#ifdef USE_LIGHT_FIELD

	#ifdef LIGHT_FIELD_FULL
		directLighting += evaluateLightField(
			frag.position,
			frag.albedo,
			frag.metalness,
			frag.roughness,
			frag.normal,
			frag.viewDir);
	#else
		directLighting += evaluateLightFieldSelf(
			frag.position,
			frag.albedo,
			frag.metalness,
			frag.roughness,
			frag.normal,
			frag.viewDir);
	#endif

#endif

#ifdef USE_PUNCTUAL
	for (uint i = 0u; i < uLights.count; ++i)
	{
		vec3 L;
		float attenuation = 1.0;

		uint type = uLights.lights[i].type;

		// POINT 0
		if (type == 0u)
		{
			float radius = uLights.lights[i].radius;
			float radiusSq = radius * radius;

			vec3 lightVector = uLights.lights[i].position - frag.position;
			float distanceSq = dot(lightVector, lightVector);

			if (distanceSq >= radiusSq)
				continue;

			L = lightVector * inversesqrt(max(distanceSq, Epsilon * Epsilon));
			attenuation = pointLightAttenuation(distanceSq, radiusSq);
		}

		// SPOT 2
		else if (type == 2u)
		{
			float radius = uLights.lights[i].radius;
			float radiusSq = radius * radius;

			vec3 lightVector = uLights.lights[i].position - frag.position;
			float distanceSq = dot(lightVector, lightVector);

			if (distanceSq >= radiusSq)
				continue;

			L = lightVector * inversesqrt(max(distanceSq, Epsilon * Epsilon));

			vec3 lightDir = uLights.lights[i].direction;
			float spotCos = dot(-L, lightDir);

			if (spotCos <= uLights.lights[i].outCone)
				continue;

			float coneAtt = smoothstep(uLights.lights[i].outCone, uLights.lights[i].inCone, spotCos);

			attenuation = pointLightAttenuation(distanceSq, radiusSq) * coneAtt;

			shadowLightDir = L;
		}

		// AREA 3
		else if (type == 3u)
		{
			vec3 position = uLights.lights[i].position;
			vec3 direction = uLights.lights[i].direction;
			float radius = uLights.lights[i].radius;
			float radiusSq = radius * radius;

			vec3 axisX = uLights.lights[i].axisX;
			vec3 axisY = uLights.lights[i].axisY;

			float halfWidth = uLights.lights[i].halfWidth;
			float halfHeight = uLights.lights[i].halfHeight;

			vec3 toFrag = frag.position - position;

			// TODO: Precompute coarseRadius on the CPU and pass as a light property:
			// float coarseRadiusSq = square(radius + sqrt(halfWidth * halfWidth + halfHeight * halfHeight));
			float extent = sqrt(halfWidth * halfWidth + halfHeight * halfHeight);
			float coarseRadius = radius + extent;

			if (dot(toFrag, toFrag) >= coarseRadius * coarseRadius)
				continue;

			float localX = dot(toFrag, axisX);
			float localY = dot(toFrag, axisY);

			float clampedX = clamp(localX, -halfWidth, halfWidth);
			float clampedY = clamp(localY, -halfHeight, halfHeight);

			vec3 closest =
				position +
				axisX * clampedX +
				axisY * clampedY;

			vec3 lightVector = closest - frag.position;
			float distanceSq = dot(lightVector, lightVector);

			if (distanceSq >= radiusSq)
				continue;

			L = lightVector * inversesqrt(max(distanceSq, Epsilon * Epsilon));

			float facing = dot(-L, direction);

			if (facing <= 0.0)
				continue;

			attenuation = pointLightAttenuation(distanceSq, radiusSq) * saturate(facing);
		}

		// DIRECTIONAL 1
		else
		{
			L = -uLights.lights[i].direction;
			shadowLightDir = L;
		}

		vec3 radiance = uLights.lights[i].radiance * attenuation;
		float NoLgeo = dot(frag.normalGeo, L);

		if (NoLgeo > 0.0)
		{
			directLighting += evaluateDirectLight(L, radiance);

			#ifdef USE_CLEARCOAT

				vec3 coatLighting = evaluateClearCoatDirect(
					frag.clearCoatRoughness,
					frag.clearCoatNormal,
					frag.viewDir,
					L,
					radiance);

				clearCoatLighting += coatLighting;

				#ifdef ALPHA_SPECULAR
					vec3 clearCoatSpecularLighting = coatLighting * lighting.clearCoatSurfaceWeight;
					specularStrength += max3(clearCoatSpecularLighting);
				#endif

			#endif
		}

		#ifdef USE_TRANSMISSION

		else if (frag.transmission > 0.0 && frag.metalness < 1.0)
		{
			directLighting += evaluateDirectTransmission(L, radiance);
		}

		#endif
	}
#endif

	return directLighting;
}

vec3 iblDirection(vec3 v)
{
#ifdef USE_IBL_TRANSFORM
	return v * uIbl.transform;
#else
	return v;
#endif
}

vec3 evaluateAmbientLighting(out vec3 clearCoatLighting)
{
	vec3 ambientLighting = vec3(0.0);
	clearCoatLighting = vec3(0.0);

#ifdef USE_IBL

#ifdef SIMPLIFIED

	vec3 irradiance = texture(irradianceTexture, iblDirection(frag.normal)).rgb * uIbl.intensity * uIbl.color;
	ambientLighting = frag.albedo * irradiance;

	#if defined(USE_TRANSMISSION)
		ambientLighting *= 1.0 - frag.transmission;
	#endif

#else

	#ifdef USE_SPECULAR
		vec3 dielectricF = fresnelSchlickRoughness(lighting.dielectricF0, lighting.NoV, frag.roughness) * frag.specular;
		vec3 kd = vec3(1.0 - max3(dielectricF)) * (1.0 - frag.metalness);
	#else
		vec3 F = fresnelSchlickRoughness(lighting.dielectricF0Mixed, lighting.NoV, frag.roughness);
		vec3 kd = (vec3(1.0) - F) * (1.0 - frag.metalness);
	#endif

		vec3 irradiance = texture(irradianceTexture, iblDirection(frag.normal)).rgb * uIbl.intensity * uIbl.color;
		vec3 diffuseIBL = kd * frag.albedo * irradiance;

		#if defined(USE_TRANSMISSION)
			diffuseIBL *= 1.0 - frag.transmission;
		#endif

		vec3 reflectionVec = iblDirection(lighting.reflectionDir);
		vec3 specularVec = reflectionVec;

		#ifdef USE_ANISOTROPY
			if (frag.anisotropy.b > 0.0)
			{
				vec3 anisotropicReflection = getAnisotropicReflection(
					frag.normal,
					lighting.anisotropicB,
					frag.viewDir,
					frag.roughness,
					frag.anisotropy.b);

				specularVec = iblDirection(anisotropicReflection);
			}
		#endif

		vec3 specularIrradiance = textureLod(
			specularEnvTexture,
			specularVec,
			frag.roughness * uIbl.specularTexLevels).rgb * uIbl.intensity;

		vec2 specularBRDF = texture(specularBRDF_LUT, vec2(lighting.NoV, frag.roughness)).rg;

		#ifdef USE_SPECULAR
			vec3 dielectricSpecularIBL = (lighting.dielectricF0 * specularBRDF.x + specularBRDF.y) * frag.specular * specularIrradiance;
			vec3 metalSpecularIBL = (frag.albedo * specularBRDF.x + specularBRDF.y) * specularIrradiance;
			vec3 specularIBL = mix(dielectricSpecularIBL, metalSpecularIBL, frag.metalness);
		#else
			vec3 specularIBL = (lighting.dielectricF0Mixed * specularBRDF.x + specularBRDF.y) * specularIrradiance;
		#endif

		#ifdef ALPHA_SPECULAR
			specularStrength += max3(specularIBL);
		#endif

		ambientLighting = diffuseIBL + specularIBL;

		#ifdef USE_IRIDESCENCE
			vec3 iridescenceDiffuseIBL = frag.albedo * irradiance;

			#if defined(USE_TRANSMISSION)
				iridescenceDiffuseIBL *= 1.0 - frag.transmission;
			#endif

			vec3 iridescenceDielectricIBL = rgbMix(iridescenceDiffuseIBL, specularIrradiance, iridescenceFresnelDielectric);
			vec3 iridescenceMetalIBL = specularIrradiance * iridescenceFresnelMetal;
			vec3 iridescenceIBL = mix(iridescenceDielectricIBL, iridescenceMetalIBL, frag.metalness);
			ambientLighting = mix(ambientLighting, iridescenceIBL, iridescenceFactor);
		#endif

		#ifdef USE_SHEEN
			float sheenScaling;
			vec3 sheenIBL = evaluateSheenIBL(
				frag.sheenColor,
				frag.sheenRoughness,
				lighting.NoV,
				reflectionVec,
				uIbl.specularTexLevels,
				uIbl.intensity,
				sheenScaling);

			ambientLighting = ambientLighting * sheenScaling + sheenIBL;

			#ifdef ALPHA_SPECULAR
				specularStrength += max3(sheenIBL);
			#endif
		#endif

		#ifdef USE_CLEARCOAT
			vec3 clearCoatVec = iblDirection(reflect(-frag.viewDir, frag.clearCoatNormal));

			clearCoatLighting = textureLod(
				specularEnvTexture,
				clearCoatVec,
				frag.clearCoatRoughness * uIbl.specularTexLevels).rgb * uIbl.intensity;

			#ifdef ALPHA_SPECULAR
				vec3 clearCoatSpecularLighting = clearCoatLighting * lighting.clearCoatSurfaceWeight;
				specularStrength += max3(clearCoatSpecularLighting);
			#endif
		#endif

	#endif
#endif

	return ambientLighting;
}

float rand(vec2 co)
{
	return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

vec3 addNoise(vec3 color3)
{
	vec2 seed = vec2(fCameraPos.xy + fUv + vec2(gl_FragCoord));
	float noise = rand(seed);
	float linearDepth = (2.0 * uCamera.nearPlane * uCamera.farPlane) /
		(uCamera.farPlane + uCamera.nearPlane - gl_FragCoord.z * (uCamera.farPlane - uCamera.nearPlane));

	color3 += noise * uCamera.depthNoiseFactor * min(linearDepth / uCamera.depthNoiseDistance, 1.0);

	return color3;
}

bool pointInsideVolume(vec3 p, vec3 minV, vec3 maxV)
{
	return all(greaterThanEqual(p, minV)) &&
		   all(lessThanEqual(p, maxV));
}

#slot FS_INCLUDES

void main()
{

#if defined(HAS_ENV_DEPTH) && defined(USE_ENV_DEPTH)
	if (!passEnvDepth(fPos, uint(uCamera.activeEye)))
	{
		color = vec4(0.0);
		return;
	}
#endif

#ifdef HAS_CLIP_VOLUME
	if (!pointInsideVolume(fPos, uClipMin, uClipMax))
		discard;
#endif

	//FragmentProperties frag = [custom_code];
	#slot FRAGMENT_LOADER

	vec3 N = frag.normal;
	vec3 V = frag.viewDir;

	lighting.NoV = max(abs(dot(N, V)), Epsilon);
	lighting.reflectionDir = reflect(-V, N);

#ifdef USE_ANISOTROPY
	vec2 anisotropyDirection = getAnisotropyDirection(frag.anisotropy.rg, uMaterial.anisotropyRotation);
	lighting.anisotropicT = normalize(fTangentBasis * vec3(anisotropyDirection, 0.0));
	lighting.anisotropicB = normalize(cross(frag.normalGeo, lighting.anisotropicT));
#endif

	lighting.dielectricF0 = getDielectricF0();
	
#ifndef USE_SPECULAR
	lighting.dielectricF0Mixed = mix(lighting.dielectricF0, frag.albedo, frag.metalness);
#endif

#ifdef HAS_IOR
	lighting.transmissionRoughnessScale = sqrt(clamp(uMaterial.ior * 2.0 - 2.0, 0.0, 1.0));
#endif

#ifdef USE_CLEARCOAT
	lighting.clearCoatSurfaceWeight = clearCoatWeight(frag.clearCoat, frag.clearCoatNormal, V);
#endif

#ifdef USE_IRIDESCENCE
	iridescenceFactor = getIridescenceFactor(frag.uv0);
	float iridescenceThickness = getIridescenceThickness(frag.uv0);

	if (iridescenceThickness == 0.0)
		iridescenceFactor = 0.0;

	iridescenceFresnelDielectric = evalIridescence(
		1.0,
		uIridescence.ior,
		lighting.NoV,
		iridescenceThickness,
		lighting.dielectricF0);

	iridescenceFresnelMetal = evalIridescence(
		1.0,
		uIridescence.ior,
		lighting.NoV,
		iridescenceThickness,
		frag.albedo);
#endif

	#ifdef ALPHA_SPECULAR
		specularStrength = 0.0;
	#endif

	vec3 shadowLightDir;
	vec3 clearCoatDirectLighting;
	vec3 clearCoatAmbientLighting;

	vec3 directLighting = evaluatePunctualLighting(shadowLightDir, clearCoatDirectLighting);
	vec3 ambientLighting = evaluateAmbientLighting(clearCoatAmbientLighting);

#ifdef USE_OCCLUSION_MAP
	float ao = mix(1.0, frag.occlusion, uMaterial.occlusionStrength);
	
	#if PBR_OCCLUSION_AFFECTS_DIRECT
		directLighting *= ao;

		#ifdef USE_CLEARCOAT
			clearCoatDirectLighting *= ao;
		#endif
	#endif

	ambientLighting *= ao;

	#ifdef USE_CLEARCOAT
		clearCoatAmbientLighting *= ao;
	#endif
#endif

#if ALPHA_MODE == ALPHA_OPAQUE
	float a = 1.0;
#else
	float a = frag.baseColor.a;
#endif

#if defined(USE_SHADOW_MAP) && defined(RECEIVE_SHADOWS) && defined(USE_PUNCTUAL)
	float shadow = calculateShadow(fPosLightSpace, frag.normalGeo, shadowLightDir);

	#ifdef TRANSPARENT
		vec3 color3 = shadow * uMaterial.shadowColor.rgb;
		a = shadow * uMaterial.shadowColor.a;

		#ifdef USE_CLEARCOAT
			vec3 clearCoatLighting = clearCoatDirectLighting + clearCoatAmbientLighting;
		#endif
	#else
		vec3 shadowFactor = vec3(1.0 - shadow * uMaterial.shadowColor.rgb);
		vec3 iblShadowFactor = mix(vec3(1.0), shadowFactor, uIbl.shadowStrength);

		vec3 color3 =
			directLighting * shadowFactor +
			ambientLighting * iblShadowFactor;

		#ifdef USE_CLEARCOAT
			vec3 clearCoatLighting =
				clearCoatDirectLighting * shadowFactor +
				clearCoatAmbientLighting * iblShadowFactor;
		#endif
	#endif

#else
	vec3 color3 = directLighting + ambientLighting;

	#ifdef USE_CLEARCOAT
		vec3 clearCoatLighting = clearCoatDirectLighting + clearCoatAmbientLighting;
	#endif
#endif

#if TRANSMISSION_MODE != TM_NONE
	#ifdef USE_SPECULAR
		vec3 F = fresnelSchlick(lighting.dielectricF0, lighting.NoV) * frag.specular;
	#else
		vec3 F = fresnelSchlick(lighting.dielectricF0, lighting.NoV);
	#endif

	vec3 transmissionWeight = vec3(1.0) - F;

	#ifdef USE_IRIDESCENCE
		float iridescenceBaseWeight = 1.0 - max3(iridescenceFresnelDielectric);
		transmissionWeight = mix(transmissionWeight, vec3(iridescenceBaseWeight), iridescenceFactor);
	#endif

	vec3 transmissionColor = frag.albedo * transmissionWeight * (1.0 - frag.metalness) * frag.transmission;
#endif

#if TRANSMISSION_MODE == TM_TEXTURE

	#ifdef USE_VOLUME
		vec4 volume = sampleVolume(frag.position, N, V, frag.thickness, frag.roughness);
	#else
		vec4 volume = sampleVolume(frag.position, frag.roughness);
	#endif

#endif

#ifdef PLANAR_REFLECTION
	color3 = planarReflection(color3, 
		frag.position, lighting.reflectionDir, lighting.dielectricF0Mixed, 
		frag.roughness, lighting.NoV, uMaterial.planarFactor, uMaterial.planarRoughness);
#endif

#ifdef USE_EMISSIVE

    vec3 emissive = uMaterial.emissive.rgb;

	#ifdef USE_EMISSIVE_MAP
		emissive *= frag.emissive.rgb * frag.emissive.a;
	#endif
	
	color3 += emissive;

#endif

#ifdef USE_CLEARCOAT
	color3 = mix(color3, clearCoatLighting, lighting.clearCoatSurfaceWeight);
#endif

color3 *= uCamera.exposure;

doPostRgb(color3);

#ifdef SRGB_ENCODE
	color3.rgb = linearTosRGB(color3.rgb);
#endif

#ifdef USE_DEPTH_NOISE
	color3 = addNoise(color3);
#endif

#ifdef ALPHA_SPECULAR
	a = mix(a, 1.0, saturate(specularStrength * uMaterial.alphaSpecularScale));
#endif

vec3 outRgb = color3;

#if TRANSMISSION_MODE == TM_TEXTURE
	outRgb += volume.rgb * volume.a * transmissionColor;
#endif

#if TRANSMISSION_MODE == TM_FB_FETCH
	vec4 dst = color;
	color = vec4(
		outRgb + dst.rgb * transmissionColor,
		a + dst.a * (1.0 - a));
#elif TRANSMISSION_MODE == TM_DUAL_SOURCE
	color = vec4(outRgb, a);
	transmissionBlend = vec4(transmissionColor, 0.0);
#else
	color = vec4(outRgb, a);
#endif

#if DEBUG == DEBUG_UV
	color = vec4(fUv.x, fUv.y, 0.0, 1.0);
#elif DEBUG == DEBUG_NORMAL
	color = vec4(N * 0.5 + 0.5, 1.0);
#elif DEBUG == DEBUG_TANGENT
	color = vec4(normalize(fTangentBasis[0]) * 0.5 + 0.5, 1.0);
#elif DEBUG == DEBUG_BITANGENT
	color = vec4(normalize(fTangentBasis[1]) * 0.5 + 0.5, 1.0);
#elif DEBUG == DEBUG_METALNESS
	color = vec4(vec3(frag.metalness), 1.0);
#elif DEBUG == DEBUG_ROUGHNESS
	color = vec4(vec3(frag.roughness), 1.0);
#elif DEBUG == DEBUG_TRANSMISSION
	color = vec4(vec3(frag.transmission), 1.0);
#elif DEBUG == DEBUG_IRRADIANCE
	color = vec4(texture(irradianceTexture, N).rgb * uIbl.intensity * uIbl.color, 1.0);
#elif DEBUG == DEBUG_FIELD_DIR
	color.rgb =	evaluateLightFieldDirection(frag.position, frag.normal);
	color.a = 1.0;

#elif DEBUG == DEBUG_FIELD_RAD
	color.rgb =	evaluateLightFieldRadiance(frag.position, frag.normal) * uMaterial.occlusionStrength;
	color.a = 1.0;
#endif

#if DEBUG != 0 && TRANSMISSION_MODE == TM_DUAL_SOURCE
	transmissionBlend = vec4(0.0);
#endif

}