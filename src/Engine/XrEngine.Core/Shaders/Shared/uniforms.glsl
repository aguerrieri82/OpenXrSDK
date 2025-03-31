
layout(std140, binding=0) uniform Camera
{
	mat4 viewProj;
	vec3 pos;	
	float exposure;
	mat4 lightSpaceMatrix;
	int activeEye;
	ivec2 viewSize;	
	float nearPlane;
	float farPlane;
	float depthNoiseFactor;
	float depthNoiseDistance;
	vec4 frustumPlanes[6];
	mat4 view;
	mat4 proj;
} uCamera;

layout(std140, binding = 3) uniform Model
{
	mat4 worldMatrix;
	mat4 normalMatrix;
} uModel;
