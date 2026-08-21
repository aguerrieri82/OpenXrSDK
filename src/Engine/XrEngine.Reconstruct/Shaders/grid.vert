#include "[XrEngine.Core]shared/uniforms.glsl"
#include "[XrEngine.Core]shared/position.glsl"

layout(location = 0) in vec3 aPosition;
layout(location = 2) in vec2 aUv;

uniform mat4 uWorldMatrix;

out VS_OUT
{
    vec3 worldPos;
    vec2 uv;
} vs_out;

void main()
{
    vec4 wp = uWorldMatrix * vec4(aPosition, 1.0);

    vs_out.worldPos = wp.xyz;
    vs_out.uv = aUv;

    gl_Position = getViewProj() * wp;
}