
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
	mat4 viewProjInv;
} uCamera;

struct ModellData 
{
	mat4 worldMatrix;
	mat4 normalMatrix;
	int drawId;
};

#ifdef USE_MODEL_SSBO

	layout(std140, binding = 3) readonly buffer Model
	{
        ModellData modelData[];
	};

	uniform int uModelIndex;

	#define uModel modelData[uModelIndex]

#else

	layout(std140, binding = 3) uniform Model
	{
		ModellData uModel;
	};

#endif