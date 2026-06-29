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
};

// This file is the compile-time injected material/fragment loader.
// Swap this include with a generated variant for specialized materials.
#include "fragment_defaults.glsl"


// GGX/Towbridge-Reitz normal distribution function.
// Uses Disney's reparametrization of alpha = roughness^2.
float ndfGGX(float cosLh, float roughness)
{
	float alpha   = roughness * roughness;
	float alphaSq = alpha * alpha;

	float denom = (cosLh * cosLh) * (alphaSq - 1.0) + 1.0;
	return alphaSq / (PI * denom * denom);
}

// Single term for separable Schlick-GGX below.
float gaSchlickG1(float cosTheta, float k)
{
	return cosTheta / (cosTheta * (1.0 - k) + k);
}

// Schlick-GGX approximation of geometric attenuation function using Smith's method.
float gaSchlickGGX(float cosLi, float cosLo, float roughness)
{
	float r = roughness + 1.0;
	float k = (r * r) / 8.0; // Epic suggests using this roughness remapping for analytic lights.
	return gaSchlickG1(cosLi, k) * gaSchlickG1(cosLo, k);
}

// Shlick's approximation of the Fresnel factor.
vec3 fresnelSchlick(vec3 F0, float cosTheta)
{
	return F0 + (vec3(1.0) - F0) * pow(1.0 - cosTheta, 5.0);
}

vec3 fresnelSchlickRoughness(vec3 F0, float cosTheta, float roughness)
{
    return F0 + (max(vec3(1.0 - roughness), F0) - F0) * pow(1.0 - cosTheta, 5.0);
}

float rand(vec2 co) {
    return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);
}

vec3 addNoise(vec3 color)
{
	vec2 seed = vec2(fCameraPos.xy + fUv + vec2(gl_FragCoord));
	
	float noise = rand(seed);
	
	float linearDepth = (2.0 * uCamera.nearPlane * uCamera.farPlane) 
					/ (uCamera.farPlane + uCamera.nearPlane - gl_FragCoord.z * (uCamera.farPlane - uCamera.nearPlane));
    
	color += noise * uCamera.depthNoiseFactor * min(linearDepth / uCamera.depthNoiseDistance, 1.0);

	return color;
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

	vec3 shadowLightDir;

	FragmentProperties frag = LOAD_FRAGMENT_PROPS;

	vec4 baseColor = frag.baseColor;
	vec3 albedo = frag.albedo;
	vec3 N = frag.normal;
	float metalness = frag.metalness;
	float roughness = frag.roughness;
	float occlusion = frag.occlusion;

	// Outgoing light direction (vector from world-space fragment position to the "eye").
	vec3 Lo = frag.viewDir;

	// Angle between surface normal and outgoing light direction.
	float cosLo = clamp(dot(N, Lo), 0.0, 1.0);
		
	// Specular reflection vector.
	vec3 Lr = reflect(-Lo, N);

#ifndef SIMPLIFIED
	// Fresnel reflectance at normal incidence (for metals use albedo color).
	vec3 F0 = mix(Fdielectric, albedo, metalness);
#endif

	// Direct lighting calculation for analytical lights.
	vec3 directLighting = vec3(0);

	#ifdef USE_PUNCTUAL

		for(uint i = 0u; i < uLights.count; ++i)
		{
			vec3 Li;
			float attenuation = 1.0; // Default attenuation

			if (uLights.lights[i].type == 0u)
			{
				// Point light.
				vec3 lightDir = uLights.lights[i].position - fPos;
				float distance = length(lightDir);
				Li = normalize(lightDir);

				float range = uLights.lights[i].radius;

				float falloff = 1.0 / max(distance * distance, 0.01);
				float rangeFalloff = clamp(1.0 - (distance / range), 0.0, 1.0);
				attenuation = falloff * rangeFalloff * rangeFalloff;
			}
			else
			{
				// Directional light.
				Li = -uLights.lights[i].direction;
				shadowLightDir = Li;
			}

			vec3 Lradiance = uLights.lights[i].radiance * attenuation;

			// Calculate angles between surface normal and various light vectors.
			float cosLi = max(0.0, dot(N, Li));

			#ifdef SIMPLIFIED
				directLighting += albedo * Lradiance * cosLi;
			#else

				float r_micro = max(roughness, 0.045);      // microfacet eval roughness floor
				float a       = r_micro * r_micro;          // Disney reparam (alpha)
				float a2      = a * a;

				// Half vector
				vec3  Lh     = normalize(Li + Lo);
				float cosLh  = max(0.0, dot(N, Lh));
				float cosVh  = max(0.0, dot(Lo, Lh));

				vec3 F = fresnelSchlickRoughness(F0, cosVh, roughness);

				// GGX NDF with clamped alpha
				float denom = (cosLh * cosLh) * (a2 - 1.0) + 1.0;
				float D     = a2 / (PI * denom * denom);

				// Smith with Epic k remap, using r_micro
				float r = r_micro + 1.0;
				float k = (r * r) * 0.125;
				float G  = gaSchlickG1(cosLi, k) * gaSchlickG1(cosLo, k);

				vec3  kd        = mix(vec3(1.0) - F, vec3(0.0), metalness);
				vec3  diffuseBRDF  = kd * albedo * (1.0 / PI);

				// Cook-Torrance specular
				vec3  specularBRDF = (F * D * G) / max(Epsilon, 4.0 * cosLi * cosLo);

				// Accumulate
				directLighting += (diffuseBRDF + specularBRDF) * Lradiance * cosLi;
			#endif
		}
	#endif	

	// Ambient lighting (IBL).
	vec3 ambientLighting = vec3(0.0);

	#ifdef USE_IBL
	{
		// Sample diffuse irradiance at normal direction.

		vec3 irradianceVec = N;
		#ifdef USE_IBL_TRANSFORM
			irradianceVec *= uIblTransform;
		#endif

		vec3 irradiance = texture(irradianceTexture, irradianceVec).rgb * uIblIntensity * uIblColor;

		#ifdef SIMPLIFIED

			vec3 diffuseIBL = albedo * irradiance;
			ambientLighting = diffuseIBL;

		#else

			vec3 F = fresnelSchlickRoughness(F0, cosLo, roughness);

			// Get diffuse contribution factor (as with direct lighting).
			vec3 kd = mix(vec3(1.0) - F, vec3(0.0), metalness);

			// Irradiance map contains exitant radiance assuming Lambertian BRDF, no need to scale by 1/PI here either.
			vec3 diffuseIBL = kd * albedo * irradiance;

			// Sample pre-filtered specular reflection environment at correct mipmap level.
			//int specularTextureLevels = textureQueryLevels(specularTexture);
			vec3 specularVec = Lr;
			#ifdef USE_IBL_TRANSFORM
				specularVec *= uIblTransform;
			#endif
			vec3 specularIrradiance = textureLod(specularTexture, specularVec, roughness * uSpecularTextureLevels).rgb * uIblIntensity;

			// Split-sum approximation factors for Cook-Torrance specular BRDF.
			vec2 specularBRDF = texture(specularBRDF_LUT, vec2(cosLo, roughness)).rg;

			// Total specular IBL contribution.
			vec3 specularIBL = (F0 * specularBRDF.x + specularBRDF.y) * specularIrradiance;

			// Total ambient lighting contribution.
			ambientLighting = diffuseIBL + specularIBL;

		#endif 
	}

	#endif

	vec3 color3 = (directLighting + ambientLighting);

	
	#ifdef PLANAR_REFLECTION
		color3 = planarReflection(color3, fPos, Lr, roughness, cosLo, uMaterial.planarFactor);
	#endif


	//Opaque
	#if ALPHA_MODE == 0
		float a = 1.0;	
	#else
		float a = baseColor.a;	
	#endif


	#ifdef USE_OCCLUSION_MAP
		color3 *= mix(1.0, occlusion, uMaterial.occlusionStrength);
	#endif

	#if defined(USE_SHADOW_MAP) && defined(RECEIVE_SHADOWS) && defined(USE_PUNCTUAL)

		float shadow = calculateShadow(fPosLightSpace, N, shadowLightDir);

		#ifdef TRANSPARENT
			color3 = shadow * uMaterial.shadowColor.rgb;
			a = shadow * uMaterial.shadowColor.a;
		#else
			color3 *= vec3(1.0 - shadow * uMaterial.shadowColor.rgb);
		#endif

	#endif

	#ifdef USE_EMISSIVE
		color3 += uMaterial.emissive.rgb * uMaterial.emissive.a * cosLo;
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


	//Blend
	#if ALPHA_MODE == 5
		if (a < uMaterial.alphaCutoff)
			discard;
	#endif

	#ifdef USE_DEPTH_NOISE
		color3 = addNoise(color3);	
	#endif

	// Final fragment color.
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
	
	color = vec4(vec3(metalness), 1.0);

#elif DEBUG == DEBUG_ROUGHNESS
	
	color = vec4(vec3(roughness), 1.0);

#elif DEBUG == DEBUG_IRRADIANCE

	color = vec4(texture(irradianceTexture, N).rgb * uIblIntensity * uIblColor, 1.0);
#endif

}
