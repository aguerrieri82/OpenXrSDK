#include "Shared/uniforms.glsl"
#include "Shared/position.glsl"

uniform mat3 uCubeRotation;

layout (location = 0) in vec3 a_position;

out vec3 v_TexCoords;


void main()
{
    v_TexCoords = a_position * uCubeRotation;
    mat4 mat = getViewProj();
    mat[3] = vec4(0.0, 0.0, 0.0, 0.1);
    vec4 pos = mat * vec4(a_position, 1.0);
    gl_Position = pos.xyww;
}
