#include "uniforms.glsl"
#include "../Shared/position.glsl"

layout(location=0) in vec3 position;
layout(location=1) in vec3 normal;
layout(location=4) in vec4 tangent;
layout(location=2) in vec2 texcoord;

#ifdef USE_CLIP_PLANE 
    uniform vec4 uClipPlane;
#endif

#ifdef USE_INSTANCE

struct ModelInstance {
    mat4 worldMatrix;
    mat4 normalMatrix;
};

layout(std430, binding = 4) buffer InstanceData {
    ModelInstance data[];
};

#endif

#ifdef HAS_UV2
    layout(location=3) in vec2 texcoord2;
    out vec2 fUv2;
#endif

out vec3 fNormal;
out vec3 fPos;
out vec2 fUv;
out vec3 fCameraPos;   

#if defined(USE_NORMAL_MAP) && defined(HAS_TANGENTS) 
    out mat3 fTangentBasis;
#endif

#ifdef USE_SHADOW_MAP
    out vec4 fPosLightSpace;
#endif

#ifdef USE_HEIGHT_MAP
    out vec3 fOrigin;
#endif

void main()
{

    #ifdef USE_INSTANCE
        mat4 worldMatrix = data[gl_InstanceID].worldMatrix;
        mat4 normalMatrix = data[gl_InstanceID].normalMatrix;
    #else
        mat4 worldMatrix = uModel.worldMatrix;
        mat4 normalMatrix = uModel.normalMatrix;
    #endif

    vec4 pos = worldMatrix * vec4(position, 1.0);
    vec3 N = normalize(vec3(normalMatrix * vec4(normal, 0.0)));

	fPos = pos.xyz; 

	fUv = texcoord;

	fCameraPos = getViewPos();

    #ifdef HAS_UV2
        fUv2 = texcoord2;
    #endif

	#ifdef HAS_TEX_TRANSFORM
	    fUv = (vec3(texcoord.xy, 1) * uMaterial.texTransform).xy;
	#endif

	#ifdef USE_SHADOW_MAP
	    fPosLightSpace = uCamera.lightSpaceMatrix * pos;
	#endif

    #if defined(USE_NORMAL_MAP) && defined(HAS_TANGENTS)
        vec3 T = normalize(vec3(normalMatrix * vec4(tangent.xyz, 0.0)));
	    vec3 B = normalize(cross(T, N) * tangent.w);

        fTangentBasis = mat3(T, B, N);

    #else
        fNormal = N;
    #endif

    #ifdef USE_CLIP_PLANE 
        gl_ClipDistance[0] = -dot(pos, uClipPlane);
    #endif

    computePos(pos);
}
