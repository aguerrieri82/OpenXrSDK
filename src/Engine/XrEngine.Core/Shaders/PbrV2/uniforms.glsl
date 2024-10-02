struct Light {
	uint type;
	vec3 position;	
	vec3 direction;
	vec3 radiance;
	float radius;
};

layout(std140, binding=0) uniform Camera
{
	mat4 viewProj;
	vec3 cameraPosition;	
	float exposure;
	mat4 lightSpaceMatrix;
} uCamera;

layout(std140, binding=1) uniform LightsUniforms
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
} uMaterial;

