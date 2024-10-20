#include "PbrV2/uniforms.glsl"


// Physically Based Rendering
// Copyright (c) 2017-2018 Michał Siejak

// Physically Based shading model: Vertex program.

layout(location=0) in vec3 position;
layout(location=1) in vec3 normal;
layout(location=4) in vec4 tangent;
//layout(location=3) in vec3 bitangent;
layout(location=2) in vec2 texcoord;


uniform mat4 uModel;
uniform mat4 uNormalMatrix;


layout(location=0) out Vertex
{
	vec3 position;
	vec2 texcoord;
	mat3 tangentBasis;
	vec4 posLightSpace;
	vec3 cameraPos;
} vout;



#ifdef MULTI_VIEW

    #define NUM_VIEWS 2

    layout(num_views=NUM_VIEWS) in;

    layout(std140) uniform SceneMatrices
    {
        uniform mat4 viewProj[NUM_VIEWS];
        uniform vec3 position[NUM_VIEWS];
        float farPlane;
    } uMatrices;

    vec3 getViewPos() 
    {
       return uMatrices.position[gl_ViewID_OVR];   
    }

    mat4 getViewProj() 
    {
       return uMatrices.viewProj[gl_ViewID_OVR];   
    }

#else

    vec3 getViewPos() 
    {
       return uCamera.cameraPosition;   
    }

    mat4 getViewProj() 
    {
       return uCamera.viewProj;   
    }

#endif


void main()
{
	vec4 pos = uModel * vec4(position, 1.0);

	vout.position = pos.xyz; // / pos.w;

	vout.texcoord = texcoord;

	vout.cameraPos = getViewPos();

	#ifdef HAS_TEX_TRANSFORM

	vout.texcoord = (vec3(texcoord.xy, 1) * uMaterial.texTransform).xy;

	#endif

	#ifdef USE_SHADOW_MAP
    
	vout.posLightSpace = uCamera.lightSpaceMatrix * pos;

	#endif


    vec3 N = normalize(vec3(uNormalMatrix * vec4(normal, 0.0)));

    #ifdef HAS_TANGENTS

    vec3 T = normalize(vec3(uModel * vec4(tangent.xyz, 0.0)));
	vec3 B = normalize(cross(T, N) * tangent.w);

    vout.tangentBasis = mat3(T, B, N);

    #else

    vout.tangentBasis = mat3(N, N, N);

    #endif

	gl_Position = getViewProj() * pos;
}
