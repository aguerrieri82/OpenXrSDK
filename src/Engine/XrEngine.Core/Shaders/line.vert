#include "Shared/uniforms.glsl"
#include "Shared/position.glsl"
#include "Shared/vertex_post.glsl"

layout (location = 0) in vec3 aPosition;
layout (location = 7) in vec4 aColor;

uniform mat4 uWorldMatrix;

out vec4 fColor;

#ifdef MOTION_VECTORS
    #include "shared/motion_vectors.glsl"
#endif

void main()
{
    computePos(uWorldMatrix * vec4(aPosition, 1.0));

    fColor = aColor;
    
    #ifdef MOTION_VECTORS
        computeMotionVectors(aPosition);
    #endif

    doPost();
}