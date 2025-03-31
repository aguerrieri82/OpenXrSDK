#include "Shared/uniforms.glsl"
#include "Shared/position.glsl"

layout (location = 0) in vec3 a_position;
layout (location = 1) in vec4 a_color_0;
layout (location = 2) in float a_size;

uniform mat4 uWorldMatrix;

out vec4 fColor;

void main()
{
    computePos(uWorldMatrix * vec4(a_position, 1.0));
    fColor = a_color_0;
}