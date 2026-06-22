layout (location = 0) in vec3 a_position;

uniform mat4 uViewProj;
uniform mat4 uWorldMatrix;

void main()
{
    gl_Position = uViewProj * uWorldMatrix * vec4(a_position, 1.0);
}