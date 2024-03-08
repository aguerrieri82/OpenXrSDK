
layout (location = 0) in vec3 a_position;
layout (location = 1) in vec3 a_color_0;
layout (location = 2) in float a_size;

uniform mat4 uModel;

out vec3 fColor;

#include "position.glsl"

void main()
{
    computePos();
    fColor = a_color_0;
}