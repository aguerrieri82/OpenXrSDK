
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec2 vUv;

uniform mat4 uModel;

out vec3 fNormal;
out vec3 fPos;
out vec2 fUv;

#include "_position.glsl";

void main()
{
    computePos();

    fPos = vec3(uModel * vec4(vPos, 1.0));

    fNormal = mat3(transpose(inverse(uModel))) * vNormal;
    fUv = vUv;
}