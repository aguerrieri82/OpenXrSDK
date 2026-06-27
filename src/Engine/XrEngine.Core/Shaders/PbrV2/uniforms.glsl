#include "../Shared/uniforms.glsl"	

struct Light {
	uint type;
	vec3 position;	
	vec3 direction;
	vec3 radiance;
	float radius;
};

layout(std140, binding=1) uniform Lights
{
	uint count;
	Light lights[MAX_LIGHTS];
} uLights;

layout(std140, binding=2) uniform Material
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
} uMaterial;


layout(std140, binding = 4) uniform Ibl
{
    float uSpecularTextureLevels; 
    float uIblIntensity;          
    float uIblShadowStrength;    
    vec3 uIblColor;    
    mat3 uIblTransform; 
};