
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
	mat4 prevWorldMatrix;
	int drawId;
};


struct ObjectData {
    vec3 bboxMax;
    vec3 bboxMin;
    vec2 extent;
    bool visible;
    bool culled;
};

#ifdef USE_DEPTH_CULL

    layout(std430, binding = 0) buffer Objects
	{
        ObjectData uObjects[];
    };

#endif


#ifdef USE_INSTANCE

    layout(std140, binding = 9) readonly buffer Instances
	{
        ModellData uInstances[];
    };

	#define uModel uInstances[gl_InstanceID]

#else

	#ifdef USE_MODEL_SSBO

		layout(std140, binding = 3) readonly buffer Models
		{
			ModellData uModels[];
		};

		uniform int uModelIndex;

		#define uModel uModels[uModelIndex]

	#else

		layout(std140, binding = 3) uniform Model
		{
			ModellData uModel;
		};

	#endif

#endif