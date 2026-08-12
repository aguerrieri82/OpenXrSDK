#include "Shared/uniforms.glsl"
#include "Shared/position.glsl"

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec4 aColor;
layout (location = 2) in float aSize;

uniform mat4 uModel;

out vec4 fColor;


void main()
{
    computePos(uModel * vec4(aPosition, 1.0));
    fColor = aColor;
    gl_PointSize = aSize;
}