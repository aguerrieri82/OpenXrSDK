#include "PbrV2/uniforms.glsl"
#include "Pbr/shadow.glsl"

// Physically Based Rendering
// Copyright (c) 2017-2018 Michał Siejak

// Physically Based shading model: Lambetrtian diffuse BRDF + Cook-Torrance microfacet specular BRDF + IBL for ambient.

// This implementation is based on "Real Shading in Unreal Engine 4" SIGGRAPH 2013 course notes by Epic Games.
// See: http://blog.selfshadow.com/publications/s2013-shading-course/karis/s2013_pbs_epic_notes_v2.pdf

const float PI = 3.141592;
const float Epsilon = 0.00001;
const float gamma      = 2.2;
const float inv_gamma  = 1.0 / gamma;
const float pureWhite  = 1.0;

// Constant normal incidence Fresnel factor for all dielectrics.
const vec3 Fdielectric = vec3(0.04);


layout(location=0) in Vertex
{
	vec3 position;
	vec2 texcoord;
	mat3 tangentBasis;
	vec4 posLightSpace;
	vec3 cameraPos;
} vin;

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


vec3 toneMap(vec3 color)
{

	float luminance = dot(color, vec3(0.2126, 0.7152, 0.0722));
	float mappedLuminance = (luminance * (1.0 + luminance/(pureWhite*pureWhite))) / (1.0 + luminance);

	// Scale color by ratio of average luminances.
	vec3 mappedColor = (mappedLuminance / luminance) * color;

	// Gamma correction.
	return pow(mappedColor, vec3(inv_gamma));
}

void main()
{
	vec3 shadowLightDir;

	// Sample input textures to get shading model params.
	#ifdef USE_ALBEDO_MAP
		vec4 baseColor = texture(albedoTexture, vin.texcoord) * uMaterial.color;
	#else
		vec4 baseColor = uMaterial.color;	
	#endif

	vec3 albedo = baseColor.rgb;

	#ifdef USE_METALROUGHNESS_MAP

		vec4 mr = texture(metalroughnessTexture, vin.texcoord);
		float metalness = mr.b * uMaterial.metalness;
		float roughness = mr.g * uMaterial.roughness;

	#else
		float metalness = uMaterial.metalness;
		float roughness = uMaterial.roughness;
	#endif

	// Outgoing light direction (vector from world-space fragment position to the "eye").
	vec3 Lo = normalize(vin.cameraPos - vin.position);

	// Get current fragment's normal and transform to world space.
	#if defined(USE_NORMAL_MAP) && defined(HAS_TANGENTS)

		vec3 N = normalize(2.0 * texture(normalTexture, vin.texcoord).rgb - 1.0);	
		N *= vec3(uMaterial.normalScale, uMaterial.normalScale, 1.0);

		mat3 TBN = vin.tangentBasis;

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
		vec3 N = normalize(vin.tangentBasis[2]);
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
			vec3 lightDir = uLights.lights[i].position - vin.position;
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

		vec3 irradiance = texture(irradianceTexture, irradianceVec).rgb * uIblIntensity;

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
	float a = baseColor.a;	

	#ifdef USE_OCCLUSION_MAP
    float ao = texture(occlusionTexture, vin.texcoord).r;
    color3 *= mix(1.0, ao, uMaterial.occlusionStrength);
	#endif

	#if defined(USE_SHADOW_MAP) && defined(RECEIVE_SHADOWS) && defined(USE_PUNCTUAL)

		float shadow = calculateShadow(vin.posLightSpace, N, shadowLightDir);

		#ifdef TRANSPARENT
			color3 = shadow * uMaterial.shadowColor.rgb;
			a = shadow * uMaterial.shadowColor.a;
		#else
			color3 *= vec3(1.0 - shadow * uMaterial.shadowColor.rgb);
		#endif

	#endif
	
	#ifdef TONEMAP
	color3 = toneMap(color3);
	#endif

	#if ALPHA_MODE == 0
		a = 1.0;
	#endif

	#if ALPHA_MODE == 1
		a = a < uMaterial.alphaCutoff ? 0.0 : 1.0;	
	#endif

	// Final fragment color.
	color = vec4(color3 * uCamera.exposure, a);


}
