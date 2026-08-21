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
	float metalness;
	float roughness;
	mat3 texTransform;
	float occlusionStrength;
	vec4 shadowColor;
	float normalScale;
	float alphaCutoff;
	vec4 emissive;
	float planarFactor;
	float planarLevel;
	float alphaSpecularScale;
};

#if !defined(VERTEX_SHADER) || defined(HAS_TEX_TRANSFORM)

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
    float uSpecularTextureLevels; 
    float uIblIntensity;          
    float uIblShadowStrength;    
    vec3 uIblColor;    
    mat3 uIblTransform; 
};