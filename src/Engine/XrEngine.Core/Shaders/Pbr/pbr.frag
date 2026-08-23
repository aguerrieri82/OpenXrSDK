#include "uniforms.glsl"

#define TM_NONE        0
#define TM_FB_FETCH    1
#define TM_DUAL_SOURCE 2
#define TM_TEXTURE     3

#ifndef TRANSMISSION_MODE
	#define TRANSMISSION_MODE TM_NONE
#endif

#include "../Shared/shadow.glsl"
#include "../Shared/env_depth.glsl"
#include "../Shared/planar_reflection.glsl"
#include "../Shared/consts.glsl"
#include "../Shared/fragment_post.glsl"

#if defined(USE_REFRACTION) || TRANSMISSION_MODE == TM_TEXTURE
	#include "../Shared/position.glsl"
	#include "../Shared/volume.glsl"
#endif


#ifdef USE_IRIDESCENCE
	#include "../Shared/iridescence.glsl"
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

#if defined(USE_NORMAL_MAP) && defined(HAS_TANGENTS)
	in mat3 fTangentBasis;
#endif


#if defined(HAS_UV2) || (ALBEDO_UV_SET == 1)
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

layout(binding=4) uniform samplerCube specularTexture;
layout(binding=5) uniform samplerCube irradianceTexture;
layout(binding=6) uniform sampler2D specularBRDF_LUT;



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

	float metalness;
	float roughness;
	float occlusion;

	vec3 viewDir;

	vec4 emissive;

	float transmission;	
};

FragmentProperties frag;

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


float distributionGGX(float NoH, float roughness)
{
	float r = max(roughness, PBR_MIN_ROUGHNESS);
	float alpha = r * r;
	float alpha2 = alpha * alpha;

	float d = NoH * NoH * (alpha2 - 1.0) + 1.0;
	return alpha2 / max(Epsilon, PI * d * d);
}

float geometrySchlickGGX(float NoX, float roughness)
{
	float r = max(roughness, PBR_MIN_ROUGHNESS) + 1.0;
	float k = (r * r) * 0.125;
	return NoX / max(Epsilon, NoX * (1.0 - k) + k);
}

float geometrySmith(float NoL, float NoV, float roughness)
{
	return geometrySchlickGGX(NoL, roughness) * geometrySchlickGGX(NoV, roughness);
}

vec3 fresnelSchlick(vec3 F0, float cosTheta)
{
	float f = pow(1.0 - saturate(cosTheta), 5.0);
	return F0 + (vec3(1.0) - F0) * f;
}

vec3 fresnelSchlickRoughness(vec3 F0, float cosTheta, float roughness)
{
	float f = pow(1.0 - saturate(cosTheta), 5.0);
	return F0 + (max(vec3(1.0 - roughness), F0) - F0) * f;
}

float pointLightAttenuation(float distance, float range)
{
	float safeRange = max(range, Epsilon);
	float d = distance / safeRange;

	float rangeFalloff = saturate(1.0 - d * d * d * d);
	return (rangeFalloff * rangeFalloff) / max(distance * distance, 0.01);
}

vec3 evaluateDirectLight(
	vec3 albedo,
	float metalness,
	float roughness,
	vec3 N,
	vec3 V,
	vec3 L,
	vec3 radiance)
{
	float NoL = saturate(dot(N, L));
	float NoV = saturate(dot(N, V));

	if (NoL <= 0.0 || NoV <= 0.0)
		return vec3(0.0);

#ifdef SIMPLIFIED
	#if defined(USE_TRANSMISSION)
		return albedo * radiance * NoL * (1.0 - frag.transmission);
	#else
		return albedo * radiance * NoL;
	#endif
#else
	vec3 H = normalize(L + V);

	float NoH = saturate(dot(N, H));
	float VoH = saturate(dot(V, H));

	#ifdef USE_REFRACTION
		float dielectricF0 = square((uVolume.ior - 1.0) / (uVolume.ior + 1.0));
		vec3 F0 = mix(vec3(dielectricF0), albedo, metalness);
	#else
		vec3 F0 = mix(Fdielectric, albedo, metalness);
	#endif

	vec3 F = fresnelSchlick(F0, VoH);

	float D = distributionGGX(NoH, roughness);
	float G = geometrySmith(NoL, NoV, roughness);
	float specularTerm = (D * G) / max(Epsilon, 4.0 * NoL * NoV);
	vec3 kd = (vec3(1.0) - F) * (1.0 - metalness);

	#if PBR_USE_PHYSICAL_DIRECT_DIFFUSE
		vec3 diffuseBRDF = kd * albedo * (1.0 / PI);
	#else
		vec3 diffuseBRDF = kd * albedo;
	#endif

	#if defined(USE_TRANSMISSION)
		diffuseBRDF *= 1.0 - frag.transmission;
	#endif

	vec3 specularBRDF = F * specularTerm;

	#ifdef ALPHA_SPECULAR
		vec3 specularLighting = specularBRDF * radiance * NoL;
		specularStrength += max(specularLighting.r, max(specularLighting.g, specularLighting.b));
	#endif

	vec3 brdf = diffuseBRDF + specularBRDF;

	#ifdef USE_IRIDESCENCE

		#if PBR_USE_PHYSICAL_DIRECT_DIFFUSE
			vec3 iridescenceDiffuseBRDF = albedo * (1.0 / PI);
		#else
			vec3 iridescenceDiffuseBRDF = albedo;
		#endif

		#if defined(USE_TRANSMISSION)
			iridescenceDiffuseBRDF *= 1.0 - frag.transmission;
		#endif

		vec3 iridescenceSpecularBRDF = vec3(specularTerm);
		vec3 iridescenceDielectricBRDF = rgbMix(iridescenceDiffuseBRDF, iridescenceSpecularBRDF, iridescenceFresnelDielectric);
		vec3 iridescenceMetalBRDF = iridescenceSpecularBRDF * iridescenceFresnelMetal;
		vec3 iridescenceBRDF = mix(iridescenceDielectricBRDF, iridescenceMetalBRDF, metalness);
		brdf = mix(brdf, iridescenceBRDF, iridescenceFactor);
	#endif

	return brdf * radiance * NoL;
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

vec3 evaluateDirectTransmission(
	vec3 albedo,
	float metalness,
	float roughness,
	vec3 N,
	vec3 V,
	vec3 L,
	vec3 radiance)
{
	float NoV = saturate(dot(N, V));
	float NoL = dot(N, L);

	if (NoV <= 0.0 || NoL >= 0.0 || frag.transmission <= 0.0)
		return vec3(0.0);

	vec3 Lt = normalize(L - 2.0 * N * dot(N, L));
	float NoLt = saturate(dot(N, Lt));

	vec3 H = normalize(Lt + V);
	float NoH = saturate(dot(N, H));
	float VoH = saturate(dot(V, H));

	float transmissionRoughness = roughness;

	#ifdef USE_REFRACTION
		float roughnessScale = clamp(uVolume.ior * 2.0 - 2.0, 0.0, 1.0);
		transmissionRoughness *= sqrt(roughnessScale);
		float dielectricF0 = square((uVolume.ior - 1.0) / (uVolume.ior + 1.0));
		vec3 F = fresnelSchlick(vec3(dielectricF0), VoH);
	#else
		vec3 F = fresnelSchlick(Fdielectric, VoH);
	#endif

	float D = distributionGGX(NoH, transmissionRoughness);
	float Vis = visibilityGGXTransmission(NoLt, NoV, transmissionRoughness);

	vec3 transmissionWeight =
		(vec3(1.0) - F) *
		(1.0 - metalness) *
		frag.transmission;

	#ifdef USE_IRIDESCENCE
		float iridescenceBaseWeight =
			1.0 - max(iridescenceFresnelDielectric.r,
				max(iridescenceFresnelDielectric.g, iridescenceFresnelDielectric.b));

		transmissionWeight = mix(
			transmissionWeight,
			vec3(iridescenceBaseWeight) * (1.0 - metalness) * frag.transmission,
			iridescenceFactor);
	#endif

	return albedo * transmissionWeight * D * Vis * radiance;
}

#endif


#ifdef USE_LIGHT_FIELD
	#include "../Shared/light_field.glsl"
#endif

vec3 evaluatePunctualLighting(FragmentProperties frag, out vec3 shadowLightDir)
{
	vec3 directLighting = vec3(0.0);
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

			vec3 lightVector = uLights.lights[i].position - frag.position;
			float distanceSq = dot(lightVector, lightVector);

			if (distanceSq >= radius * radius)
				continue;

			float distance = sqrt(distanceSq);
			L = lightVector / max(distance, Epsilon);

			attenuation = pointLightAttenuation(distance, radius);
		}

		// SPOT 2
		else if (type == 2u)
		{
			float radius = uLights.lights[i].radius;

			vec3 lightVector = uLights.lights[i].position - frag.position;
			float distanceSq = dot(lightVector, lightVector);

			if (distanceSq >= radius * radius)
				continue;

			float distance = sqrt(distanceSq);
			L = lightVector / max(distance, Epsilon);

			vec3 lightDir = uLights.lights[i].direction;
			float spotCos = dot(-L, lightDir);

			if (spotCos <= uLights.lights[i].outCone)
				continue;

			float coneAtt = smoothstep(uLights.lights[i].outCone, uLights.lights[i].inCone, spotCos);

			attenuation = pointLightAttenuation(distance, radius) * coneAtt;

			shadowLightDir = L;
		}

		// AREA 3
		else if (type == 3u)
		{
			vec3 position = uLights.lights[i].position;
			vec3 direction = uLights.lights[i].direction;
			float radius = uLights.lights[i].radius;

			vec3 axisX = uLights.lights[i].axisX;
			vec3 axisY = uLights.lights[i].axisY;

			float halfWidth = uLights.lights[i].halfWidth;
			float halfHeight = uLights.lights[i].halfHeight;

			vec3 toFrag = frag.position - position;

			// Coarse reject before closest-point work.
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

			if (distanceSq >= radius * radius)
				continue;

			float distance = sqrt(distanceSq);
			L = lightVector / max(distance, Epsilon);

			float facing = dot(-L, direction);

			if (facing <= 0.0)
				continue;

			attenuation =
				pointLightAttenuation(distance, radius) *
				saturate(facing);
		}

		// DIRECTIONAL 1
		else
		{
			L = -uLights.lights[i].direction;
			shadowLightDir = L;
		}

		if (attenuation <= Epsilon)
			continue;

		float NoL = dot(frag.normal, L);

		if (NoL > 0.0)
		{
			vec3 radiance = uLights.lights[i].radiance * attenuation;
			directLighting += evaluateDirectLight(
				frag.albedo,
				frag.metalness,
				frag.roughness,
				frag.normal,
				frag.viewDir,
				L,
				radiance);
		}
		#ifdef USE_TRANSMISSION

		else 
		{
			vec3 radiance = uLights.lights[i].radiance * attenuation;
			directLighting += evaluateDirectTransmission(
				frag.albedo,
				frag.metalness,
				frag.roughness,
				frag.normal,
				frag.viewDir,
				L,
				radiance);
		}

		#endif
	}
#endif

	return directLighting;
}

vec3 iblDirection(vec3 v)
{
#ifdef USE_IBL_TRANSFORM
	return v * uIblTransform;
#else
	return v;
#endif
}

vec3 evaluateAmbientLighting(FragmentProperties frag, vec3 reflectionDir, float NoV)
{
	vec3 ambientLighting = vec3(0.0);

#ifdef USE_IBL

#ifdef SIMPLIFIED

	vec3 irradiance = texture(irradianceTexture, iblDirection(frag.normal)).rgb * uIblIntensity * uIblColor;
	ambientLighting = frag.albedo * irradiance;

	#if defined(USE_TRANSMISSION)
		ambientLighting *= 1.0 - frag.transmission;
	#endif

#else

	#ifdef USE_REFRACTION
		float dielectricF0 = square((uVolume.ior - 1.0) / (uVolume.ior + 1.0));
		vec3 F0 = mix(vec3(dielectricF0), frag.albedo, frag.metalness);
	#else
		vec3 F0 = mix(Fdielectric, frag.albedo, frag.metalness);
	#endif
		vec3 F = fresnelSchlickRoughness(F0, NoV, frag.roughness);

		vec3 irradiance = texture(irradianceTexture, iblDirection(frag.normal)).rgb * uIblIntensity * uIblColor;
		vec3 kd = (vec3(1.0) - F) * (1.0 - frag.metalness);
		vec3 diffuseIBL = kd * frag.albedo * irradiance;

		#if defined(USE_TRANSMISSION)
			diffuseIBL *= 1.0 - frag.transmission;
		#endif

		vec3 specularVec = iblDirection(reflectionDir);
		vec3 specularIrradiance = textureLod(
			specularTexture,
			specularVec,
			frag.roughness * uSpecularTextureLevels).rgb * uIblIntensity;

		vec2 specularBRDF = texture(specularBRDF_LUT, vec2(NoV, frag.roughness)).rg;
		vec3 specularIBL = (F0 * specularBRDF.x + specularBRDF.y) * specularIrradiance;

		#ifdef ALPHA_SPECULAR
			specularStrength += max(specularIBL.r, max(specularIBL.g, specularIBL.b));
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
	float NoV = saturate(dot(N, V));
	vec3 R = reflect(-V, N);


#ifdef USE_IRIDESCENCE
	iridescenceFactor = getIridescenceFactor(frag.uv0);
	float iridescenceThickness = getIridescenceThickness(frag.uv0);

	if (iridescenceThickness == 0.0)
		iridescenceFactor = 0.0;

	#ifdef USE_REFRACTION
		float iridescenceBaseF0 = square((uVolume.ior - 1.0) / (uVolume.ior + 1.0));
	#else
		float iridescenceBaseF0 = Fdielectric.x;
	#endif

	iridescenceFresnelDielectric = evalIridescence(
		1.0,
		uIridescence.ior,
		NoV,
		iridescenceThickness,
		vec3(iridescenceBaseF0));

	iridescenceFresnelMetal = evalIridescence(
		1.0,
		uIridescence.ior,
		NoV,
		iridescenceThickness,
		frag.albedo);
#endif

	#ifdef ALPHA_SPECULAR
		specularStrength = 0.0;
	#endif

	vec3 shadowLightDir;
	vec3 directLighting = evaluatePunctualLighting(frag, shadowLightDir);
	vec3 ambientLighting = evaluateAmbientLighting(frag, R, NoV);

#ifdef USE_OCCLUSION_MAP
	float ao = mix(1.0, frag.occlusion, uMaterial.occlusionStrength);
	
	#if PBR_OCCLUSION_AFFECTS_DIRECT
		directLighting *= ao;
	#endif

	ambientLighting *= ao;
#endif

#if ALPHA_MODE == ALPHA_OPAQUE
	float a = 1.0;
#else
	float a = frag.baseColor.a;
#endif

#if defined(USE_SHADOW_MAP) && defined(RECEIVE_SHADOWS) && defined(USE_PUNCTUAL)
	float shadow = calculateShadow(fPosLightSpace, N, shadowLightDir);

	#ifdef TRANSPARENT
		vec3 color3 = shadow * uMaterial.shadowColor.rgb;
		a = shadow * uMaterial.shadowColor.a;
	#else
		vec3 shadowFactor = vec3(1.0 - shadow * uMaterial.shadowColor.rgb);

		vec3 color3 =
			directLighting * shadowFactor +
			ambientLighting * mix(vec3(1.0), shadowFactor, uIblShadowStrength);
	#endif

#else
	vec3 color3 = directLighting + ambientLighting;
#endif

#if TRANSMISSION_MODE == TM_TEXTURE
	#ifdef USE_REFRACTION
		float dielectricF0 = square((uVolume.ior - 1.0) / (uVolume.ior + 1.0));
		vec3 F = fresnelSchlick(vec3(dielectricF0), NoV);
	#else
		vec3 F = fresnelSchlick(Fdielectric, NoV);
	#endif

	vec3 transmissionWeight = vec3(1.0) - F;

	#ifdef USE_IRIDESCENCE
		float iridescenceBaseWeight = 1.0 - max(iridescenceFresnelDielectric.r, max(iridescenceFresnelDielectric.g, iridescenceFresnelDielectric.b));
		transmissionWeight = mix(transmissionWeight, vec3(iridescenceBaseWeight), iridescenceFactor);
	#endif

	#ifdef USE_REFRACTION
		vec4 volume = sampleVolume(frag.position, N, V, getViewProj(), frag.uv0, frag.roughness);
	#else
		vec2 volumeUv = computeVolumeUv(frag.position, vec3(0.0), getViewProj());
		float volumeLod = float(textureQueryLevels(volumeForeground) - 1) * frag.roughness;
		vec4 volume = sampleVolumeForeground(volumeUv, volumeLod);
	#endif

	color3 += volume.rgb * volume.a * frag.albedo * transmissionWeight * (1.0 - frag.metalness) * frag.transmission;
#endif

#ifdef PLANAR_REFLECTION
	color3 = planarReflection(color3, frag.position, R, frag.roughness, NoV, uMaterial.planarFactor, uMaterial.planarLevel);
#endif

#ifdef USE_EMISSIVE

    vec3 emissive = uMaterial.emissive.rgb;

	#ifdef USE_EMISSIVE_MAP
		emissive *= frag.emissive.rgb * frag.emissive.a;
	#endif
	
	color3 += emissive;

#endif

	doPostRgb(color3);

#ifdef SRGB_ENCODE
	color3.rgb = linearTosRGB(color3.rgb);
#endif

#ifdef USE_DEPTH_NOISE
	color3 = addNoise(color3);
#endif

#if TRANSMISSION_MODE == TM_FB_FETCH || TRANSMISSION_MODE == TM_DUAL_SOURCE
	a *= 1.0 - frag.transmission;
#endif

#ifdef ALPHA_SPECULAR
	a = mix(a, 1.0, saturate(specularStrength * uMaterial.alphaSpecularScale));
#endif

	vec3 outRgb = color3 * uCamera.exposure;

#if TRANSMISSION_MODE == TM_FB_FETCH
	vec4 dst = color;
	vec3 transmissionColor = frag.albedo * (1.0 - a);
	color = vec4(
		outRgb * a + dst.rgb * transmissionColor,
		a + dst.a * (1.0 - a));
#elif TRANSMISSION_MODE == TM_DUAL_SOURCE
	color = vec4(outRgb, a);
	transmissionBlend = vec4(frag.albedo * (1.0 - a), 0.0);
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
	color = vec4(texture(irradianceTexture, N).rgb * uIblIntensity * uIblColor, 1.0);
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
