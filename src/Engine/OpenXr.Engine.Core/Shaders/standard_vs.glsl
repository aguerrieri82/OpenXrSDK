version 330 core

layout(location = 0) in vec3 aPos;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    // Transform vertex from object space to world space
    vec4 worldPosition = model * vec4(aPos, 1.0);
    
    // Transform vertex from world space to view space
    vec4 viewPosition = view * worldPosition;
    
    // Transform vertex from view space to clip space
    gl_Position = projection * viewPosition;
}