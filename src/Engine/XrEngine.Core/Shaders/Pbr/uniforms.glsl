#include "../Shared/uniforms.glsl"	

struct Light {
	uint type;
	vec3 position;	
	vec3 direction;
	vec3 radiance;
	float radius;
	float outCone;
	float inCone;
    vec3 axisX;
	float halfWidth;
	vec3 axisY;
	float halfHeight;
};

layout(std140, binding=1) uniform Lights
{
	uint count;
	Light lights[MAX_LIGHTS];
} uLights;


struct MaterialData
{
	vec4 color;
	vec4 shadowColor;
	vec4 emissive;

	vec3 sheenColor;
	float sheenRoughness;

	vec3 specularColor;
	float specular;

	vec3 attenuationColor;
	float attenuationDistance;

	float metalness;
	float roughness;
	float occlusionStrength;
	float normalScale;
	float alphaCutoff;
	float planarFactor;
	float planarLevel;
	float alphaSpecularScale;
	float transmission;

	float clearCoatFactor;
	float clearCoatRoughnessFactor;
	float clearCoatNormalScale;

	float ior;
	float thickness;

};

#if !defined(VERTEX_SHADER) 

	#ifdef USE_MATERIAL_SSBO

		layout(std140, binding = 2) readonly buffer Material
		{
			MaterialData materialData[];
		};

		uniform int uMaterialIndex;

		#define uMaterial materialData[uMaterialIndex]

	#else

		layout(std140, binding = 2) uniform Material
		{
			MaterialData uMaterial;
		};

	#endif

#endif

layout(std140, binding = 4) uniform Ibl
{
    float specularTexLevels; 
    float intensity;          
    float shadowStrength;    
    vec3 color;    
    mat3 transform; 
} uIbl;

uniform mat3 uTexTransform[5];