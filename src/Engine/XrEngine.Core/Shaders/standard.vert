
layout (location = 0) in vec3 a_position;
layout (location = 1) in vec3 a_normal;
layout (location = 2) in vec2 a_texcoord_0;

uniform mat4 uModel;

out vec3 fNormal;
out vec3 fPos;
out vec2 fUv;

#include "position.glsl"

void main()
{
    computePos();

    fPos = vec3(uModel * vec4(a_position, 1.0));
    fNormal = mat3(transpose(inverse(uModel))) * a_normal;
    fUv = a_texcoord_0;
}