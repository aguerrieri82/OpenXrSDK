#include "uniforms.glsl"
#include "../Shared/shadow.glsl"
#include "../Shared/env_depth.glsl"
#include "../Shared/tonemap.glsl"
#include "../Shared/planar_reflection.glsl"

const float PI = 3.141592;
const float Epsilon = 0.00001;

const vec3 Fdielectric = vec3(0.04);

#define DEBUG_UV         1
#define DEBUG_NORMAL     2
#define DEBUG_TANGENT    3
#define DEBUG_BITANGENT  4
#define DEBUG_METALNESS  5
#define DEBUG_ROUGHNESS  6
#define DEBUG_IRRADIANCE 7

// Lighting V2 switches.
// Keep these as compile-time flags so you can A/B against the old shader without changing inputs.
#ifndef PBR_MIN_ROUGHNESS
	#define PBR_MIN_ROUGHNESS 0.045
#endif

#ifndef PBR_USE_PHYSICAL_DIRECT_DIFFUSE
	#define PBR_USE_PHYSICAL_DIRECT_DIFFUSE 1
#endif

#ifndef PBR_OCCLUSION_AFFECTS_DIRECT
	#define PBR_OCCLUSION_AFFECTS_DIRECT 0
#endif

in vec3 fNormal;
in vec3 fPos;
in vec2 fUv;
in vec3 fCameraPos;

#if defined(USE_NORMAL_MAP) && defined(HAS_TANGENTS)
	in mat3 fTangentBasis;
#endif

#ifndef ALBEDO_UV_SET
	#define ALBEDO_UV_SET 0
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

layout(location=0) out vec4 color;

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
};

// Compile-time injected material/fragment loader.
// Replace this include with a generated variant when needed.
#include "fragment_defaults.glsl"

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

vec3 safeNormalize(vec3 v)
{
	float lenSq = dot(v, v);
	if (lenSq <= Epsilon)
		return vec3(0.0, 0.0, 1.0);
	return v * inversesqrt(lenSq);
}

// GGX / Trowbridge-Reitz normal distribution.
// Input roughness is perceptual roughness. Internally converted to alpha = roughness^2.
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

	// Smooth finite-range cutoff. This avoids the hard-looking linear edge.
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
	return albedo * radiance * NoL;
#else
	vec3 H = safeNormalize(L + V);

	float NoH = saturate(dot(N, H));
	float VoH = saturate(dot(V, H));

	vec3 F0 = mix(Fdielectric, albedo, metalness);
	vec3 F = fresnelSchlick(F0, VoH);

	float D = distributionGGX(NoH, roughness);
	float G = geometrySmith(NoL, NoV, roughness);

	vec3 kd = (vec3(1.0) - F) * (1.0 - metalness);

#if PBR_USE_PHYSICAL_DIRECT_DIFFUSE
	vec3 diffuseBRDF = kd * albedo * (1.0 / PI);
#else
	vec3 diffuseBRDF = kd * albedo;
#endif

	vec3 specularBRDF = (F * D * G) / max(Epsilon, 4.0 * NoL * NoV);

	return (diffuseBRDF + specularBRDF) * radiance * NoL;
#endif
}

vec3 evaluatePunctualLighting(FragmentProperties frag, out vec3 shadowLightDir)
{
	vec3 directLighting = vec3(0.0);
	shadowLightDir = vec3(0.0, 1.0, 0.0);

#ifdef USE_PUNCTUAL
	for (uint i = 0u; i < uLights.count; ++i)
	{
		vec3 L;
		float attenuation = 1.0;

		if (uLights.lights[i].type == 0u)
		{
			vec3 lightVector = uLights.lights[i].position - frag.position;
			float distance = length(lightVector);
			L = lightVector / max(distance, Epsilon);

			attenuation = pointLightAttenuation(distance, uLights.lights[i].radius);
		}
		else
		{
			L = safeNormalize(-uLights.lights[i].direction);
			shadowLightDir = L;
		}

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
	vec3 irradiance = texture(irradianceTexture, iblDirection(frag.normal)).rgb * uIblIntensity * uIblColor;

#ifdef SIMPLIFIED
	ambientLighting = frag.albedo * irradiance;
#else
	vec3 F0 = mix(Fdielectric, frag.albedo, frag.metalness);
	vec3 F = fresnelSchlickRoughness(F0, NoV, frag.roughness);
	vec3 kd = (vec3(1.0) - F) * (1.0 - frag.metalness);

	vec3 diffuseIBL = kd * frag.albedo * irradiance;

	vec3 specularVec = iblDirection(reflectionDir);
	vec3 specularIrradiance = textureLod(
		specularTexture,
		specularVec,
		frag.roughness * uSpecularTextureLevels).rgb * uIblIntensity;

	vec2 specularBRDF = texture(specularBRDF_LUT, vec2(NoV, frag.roughness)).rg;
	vec3 specularIBL = (F0 * specularBRDF.x + specularBRDF.y) * specularIrradiance;

	ambientLighting = diffuseIBL + specularIBL;
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

	FragmentProperties frag = LOAD_FRAGMENT_PROPS;

	vec3 N = frag.normal;
	vec3 V = frag.viewDir;
	float NoV = saturate(dot(N, V));
	vec3 R = reflect(-V, N);

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

#if ALPHA_MODE == 1
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

#ifdef PLANAR_REFLECTION
	color3 = planarReflection(color3, frag.position, R, frag.roughness, NoV, uMaterial.planarFactor);
#endif

#ifdef USE_EMISSIVE
    vec3 emissive = uMaterial.emissive.rgb;

    #ifdef USE_EMISSIVE_MAP
        emissive *= frag.emissive.rgb * frag.emissive.a;
    #endif

    color3 += emissive;
#endif


#ifdef TONE_MAP

    #if TONE_MAP == 1
        color3.rgb = toneMap(color3.rgb);
    #endif

    #if TONE_MAP == 2
        color3.rgb = toneMapNeutral(color3.rgb);
    #endif

    #ifdef SRGB
        color3.rgb = linearTosRGB(color3.rgb);
    #endif
#endif

#ifdef USE_DEPTH_NOISE
	color3 = addNoise(color3);
#endif

	color = vec4(color3 * uCamera.exposure, a);

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
#elif DEBUG == DEBUG_IRRADIANCE
	color = vec4(texture(irradianceTexture, N).rgb * uIblIntensity * uIblColor, 1.0);
#endif
}
