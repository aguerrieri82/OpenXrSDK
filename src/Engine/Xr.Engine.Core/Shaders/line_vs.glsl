
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vColor;
layout (location = 2) in float vSize;

uniform mat4 uModel;

out vec3 fColor;

#include "_position.glsl";

void main()
{
    computePos();
    fColor = vColor;
}