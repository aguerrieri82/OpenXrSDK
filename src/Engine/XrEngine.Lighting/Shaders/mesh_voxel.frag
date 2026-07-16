

in vec4 vBaseColor;
in vec3 vNormal;
in float vRoughness;
in float vMetallic;

layout(location = 0) out vec4 outColor;

void main()
{
    vec3 normal = normalize(vNormal);
    outColor = vec4(normal * 0.5 + 0.5, 1.0);
}