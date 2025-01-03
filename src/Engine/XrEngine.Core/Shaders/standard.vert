﻿
#include "Shared/uniforms.glsl"
#include "Shared/position.glsl"

#ifdef PLANAR_REFLECTION
    #include "Shared/planar_reflection.glsl"
    out vec2 fPlanarUv;
#endif

#ifdef USE_CLIP_PLANE 
    uniform vec4 uClipPlane;
#endif

layout (location = 0) in vec3 a_position;
layout (location = 1) in vec3 a_normal;
layout (location = 2) in vec2 a_texcoord_0;

uniform mat4 uModel;
uniform mat4 uNormalMatrix;

#ifdef USE_SHADOW_MAP
    out vec4 fPosLightSpace;
#endif

#ifdef UV_TRANSFORM
    uniform mat3 uUvTransform;
#endif

out vec3 fNormal;
out vec3 fPos;
out vec2 fUv;

void main()
{
    vec4 pos = uModel * vec4(a_position, 1.0);

    fPos = vec3(pos);
    
    computePos(pos);

    fNormal = mat3(uNormalMatrix) * a_normal;
    fUv = a_texcoord_0;
    
    #ifdef UV_TRANSFORM 
	    fUv = (vec3(fUv, 1) * uUvTransform).xy;
    #endif

    #ifdef USE_SHADOW_MAP
        fPosLightSpace = uCamera.lightSpaceMatrix * pos;
    #endif

    #ifdef PLANAR_REFLECTION
        fPlanarUv = planarUV(pos);
    #endif

    #ifdef USE_CLIP_PLANE 
        gl_ClipDistance[0] = -dot(pos, uClipPlane);
    #endif

}