layout (location = 0) in vec3 aPosition;

uniform mat4 uViewProj;
uniform mat4 uWorldMatrix;

void main()
{
    gl_Position = uViewProj * uWorldMatrix * vec4(aPosition, 1.0);
}