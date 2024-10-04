
layout (location = 0) in vec3 a_position;
layout (location = 1) in vec3 a_normal;
layout (location = 2) in vec2 a_texcoord_0;

uniform mat4 uModel;
uniform mat4 uNormalMatrix;

#ifdef USE_SHADOW_MAP

uniform mat4 uLightSpaceMatrix;

out vec4 fPosLightSpace;

#endif

out vec3 fNormal;
out vec3 fPos;
out vec2 fUv;

#include "position.glsl"

void main()
{
    vec4 pos4 = vec4(a_position, 1.0);
    vec4 pos = uModel * pos4;
    fPos = vec3(pos);
    
    computePos(pos);

    fNormal = mat3(uNormalMatrix) * a_normal;
    fUv = a_texcoord_0;

    #ifdef USE_SHADOW_MAP
        fPosLightSpace = uLightSpaceMatrix * pos4;
    #endif
}