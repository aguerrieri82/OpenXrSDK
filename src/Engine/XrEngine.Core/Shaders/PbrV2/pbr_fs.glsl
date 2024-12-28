#include "uniforms.glsl"
#include "../Shared/shadow.glsl"
#include "../Shared/env_depth.glsl"
#include "../Shared/tonemap.glsl"
#include "../Shared/planar_reflection.glsl"

const float PI = 3.141592;
const float Epsilon = 0.00001;

const vec3 Fdielectric = vec3(0.04);

#define DEBUG_UV        1
#define DEBUG_NORMAL    2
#define DEBUG_TANGENT   3
#define DEBUG_BITANGENT 4
#define DEBUG_METALNESS 5
#define DEBUG_ROUGHNESS 6

in vec3 fNormal;
in vec3 fPos;
in vec2 fUv;
in vec3 fCameraPos;    
in mat3 fTangentBasis;
in vec3 fDebug;

#ifdef USE_SHADOW_MAP
    in vec4 fPosLightSpace;
#endif


layout(location=0) out vec4 color;


layout(binding=0) uniform sampler2D albedoTexture;
layout(binding=1) uniform sampler2D normalTexture;
layout(binding=2) uniform sampler2D metalroughnessTexture;
layout(binding=3) uniform sampler2D occlusionTexture;

layout(binding=4) uniform samplerCube specularTexture;
layout(binding=5) uniform samplerCube irradianceTexture;
layout(binding=6) uniform sampler2D specularBRDF_LUT;


uniform float uSpecularTextureLevels;
uniform float uIblIntensity;
uniform vec3 uIblColor;


#ifdef USE_IBL_TRANSFORM
uniform mat3 uIblTransform;
#endif


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
	float linearDepth = (2.0 * uCamera.nearPlane * uCamera.farPlane) / (uCamera.farPlane + uCamera.nearPlane - gl_FragCoord.z * (uCamera.farPlane - uCamera.nearPlane));
    
	color += noise * uCamera.depthNoiseFactor * min(linearDepth / uCamera.depthNoiseDistance, 1.0);

	return color;
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

	vec3 shadowLightDir;

	// Sample input textures to get shading model params.
	#ifdef USE_ALBEDO_MAP
		vec4 baseColor = texture(albedoTexture, fUv) * uMaterial.color;
	#else
		vec4 baseColor = uMaterial.color;	
	#endif
	
	//Mask
	#if ALPHA_MODE == 1
		if (baseColor.a < uMaterial.alphaCutoff)
			discard;
	#endif

	vec3 albedo = baseColor.rgb;

	#ifdef USE_METALROUGHNESS_MAP

		vec4 mr = texture(metalroughnessTexture, fUv);
		float metalness = clamp(mr.b * uMaterial.metalness, 0.0, 1.0);
		float roughness = clamp(mr.g * uMaterial.roughness, 0.0, 1.0);

	#else
		float metalness = uMaterial.metalness;
		float roughness = uMaterial.roughness;
	#endif

	// Outgoing light direction (vector from world-space fragment position to the "eye").
	vec3 Lo = normalize(fCameraPos - fPos);

	// Get current fragment's normal and transform to world space.
	#if defined(USE_NORMAL_MAP) && defined(HAS_TANGENTS) 

		vec3 N = normalize(2.0 * texture(normalTexture, fUv).rgb - 1.0);	
		N *= vec3(uMaterial.normalScale, uMaterial.normalScale, 1.0);

		mat3 TBN = fTangentBasis;

		#ifdef DOUBLE_SIDED

		if (!gl_FrontFacing) 
		{
			TBN[0] = -TBN[0]; // Flip tangent
			TBN[1] = -TBN[1]; // Flip bitangent
			TBN[2] = -TBN[2]; // Flip normal
		}
		
		#endif

		N = normalize(TBN * N);

	#else
		vec3 N = normalize(fNormal);
		#ifdef DOUBLE_SIDED
		if (!gl_FrontFacing)
			N = -N;
		#endif
	#endif


	// Angle between surface normal and outgoing light direction.
	float cosLo = max(0.0, dot(N, Lo));
		
	// Specular reflection vector.
	vec3 Lr = reflect(-Lo, N);

	// Fresnel reflectance at normal incidence (for metals use albedo color).
	vec3 F0 = mix(Fdielectric, albedo, metalness);

	// Direct lighting calculation for analytical lights.
	vec3 directLighting = vec3(0);
	for(uint i = 0u; i < uLights.count; ++i)
	{
		vec3 Li;
		float attenuation = 1.0; // Default attenuation

		if(uLights.lights[i].type == 0u)
		{
			// Point light.
			vec3 lightDir = uLights.lights[i].position - fPos;
			float distance = length(lightDir);
			Li = normalize(lightDir);

			float range = uLights.lights[i].radius;
		    float smoothFactor = clamp((range - distance) / range, 0.0, 1.0);
			attenuation = smoothFactor * smoothFactor;
		}
		else
		{
			// Directional light.
			Li = -uLights.lights[i].direction;
			shadowLightDir = Li;
		}

		vec3 Lradiance = uLights.lights[i].radiance * attenuation;

		// Half-vector between Li and Lo.
		vec3 Lh = normalize(Li + Lo);

		// Calculate angles between surface normal and various light vectors.
		float cosLi = max(0.0, dot(N, Li));
		float cosLh = max(0.0, dot(N, Lh));

		// Calculate Fresnel term for direct lighting. 
		float cosTheta = clamp(dot(Lh, Lo), 0.0, 1.0);
		vec3 F  = fresnelSchlickRoughness(F0, cosTheta, roughness);

		// Calculate normal distribution for specular BRDF.
		
		float D = ndfGGX(cosLh, roughness);
		
		// Calculate geometric attenuation for specular BRDF.
		float G = gaSchlickGGX(cosLi, cosLo, roughness);

		// Diffuse scattering happens due to light being refracted multiple times by a dielectric medium.
		// Metals on the other hand either reflect or absorb energy, so diffuse contribution is always zero.
		// To be energy conserving we must scale diffuse BRDF contribution based on Fresnel factor & metalness.
		vec3 kd = mix(vec3(1.0) - F, vec3(0.0), metalness);

		// Lambert diffuse BRDF.
		vec3 diffuseBRDF = kd * albedo;

		// Cook-Torrance specular microfacet BRDF.
		vec3 specularBRDF = (F * D * G) / max(Epsilon, 4.0 * cosLi * cosLo);

		// Total contribution for this light.
		directLighting += (diffuseBRDF + specularBRDF) * Lradiance * cosLi;
	}

	// Ambient lighting (IBL).
	vec3 ambientLighting;

	#ifdef USE_IBL
	{
		// Sample diffuse irradiance at normal direction.

		vec3 irradianceVec = N;
		#ifdef USE_IBL_TRANSFORM
			irradianceVec *= uIblTransform;
		#endif

		vec3 irradiance = texture(irradianceTexture, irradianceVec).rgb * uIblIntensity * uIblColor;

		// Calculate Fresnel term for ambient lighting.
		// Since we use pre-filtered cubemap(s) and irradiance is coming from many directions
		// use cosLo instead of angle with light's half-vector (cosLh above).
		// See: https://seblagarde.wordpress.com/2011/08/17/hello-world/
		vec3 F = fresnelSchlick(F0, cosLo);

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
	}
	#endif

	vec3 color3 = (directLighting + ambientLighting);

	
	#ifdef PLANAR_REFLECTION
		color3 = planarReflection(color3, fPos, Lr, roughness, cosLo);
	#endif


	//Opaque
	#if ALPHA_MODE == 0
		float a = 1.0;	
	#else
		float a = baseColor.a;	
	#endif


	#ifdef USE_OCCLUSION_MAP
    float ao = texture(occlusionTexture, fUv).r;
    color3 *= mix(1.0, ao, uMaterial.occlusionStrength);
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
	
	#ifdef TONEMAP
		color3 = linearTosRGB(toneMap(color3));
	#endif

	//Blend
	#if ALPHA_MODE == 2
		a = max(uMaterial.alphaCutoff, a);
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

#endif

}
