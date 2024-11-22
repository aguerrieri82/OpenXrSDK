
layout (location = 0) in vec3 a_position;
layout (location = 2) in vec2 a_texcoord_0;

out vec2 fUv;
uniform mat4 uModel;

void main()
{
    gl_Position = uModel * vec4(a_position, 1.0);
    gl_Position.z = -gl_Position.w;

    fUv = a_texcoord_0;
}