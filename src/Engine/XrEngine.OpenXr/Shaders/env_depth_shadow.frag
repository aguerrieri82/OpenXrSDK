
#include "[XrEngine.Core]Shared/uniforms.glsl"
#include "[XrEngine.Core]Shared/position.glsl"
#include "[XrEngine.Core]Shared/depth_sampler.glsl"
#include "[XrEngine.Core]Shared/shadow.glsl"
    
layout(binding = 8) uniform sampler2DArray uEnvDepth;

#ifndef MULTI_VIEW
    uniform int uViewIndex;

    int getViewIndex()
    {
        return uViewIndex;
    }
#endif

uniform mat4 uEnvViewProjInv[2];
uniform float uEnvDepthBias;
uniform mat4 uLightMatrix;
uniform vec4 uShadowColor;

in vec2 fUv;
out vec4 outColor;

float getEnvDepth(vec2 uv, int view)
{
    return texture(uEnvDepth, vec3(uv, float(view))).r;
}

vec3 reconstructEnvWorld(vec2 uv, float envDepth, int view)
{
    vec4 clip = vec4(
        uv * 2.0 - 1.0,
        envDepth * 2.0 - 1.0,
        1.0
    );

    vec4 world = uEnvViewProjInv[view] * clip;
    return world.xyz / world.w;
}

float worldToProjectionDepth(vec3 pWorld)
{
    vec4 clip = getViewProj() * vec4(pWorld, 1.0);
    vec3 ndc = clip.xyz / clip.w;
    return ndc.z * 0.5 + 0.5;
}

void main()
{

    int view = getViewIndex();

    float envDepth = getEnvDepth(fUv, view);

    if (envDepth <= 0.0 || envDepth >= 1.0)
        discard;

    vec3 pWorld = reconstructEnvWorld(fUv, envDepth, view);

    float envProjectedDepth = worldToProjectionDepth(pWorld);
    float virtualDepth = getDepth(fUv);

    if (virtualDepth < envProjectedDepth - uEnvDepthBias)
        discard;

    float shadow = calculateShadow(
        uLightMatrix * vec4(pWorld, 1.0),
        vec3(0.0, 1.0, 0.0),
        vec3(0.0, 1.0, 0.0)
    );

    if (shadow <= 0.0)
        discard;

    outColor = vec4(uShadowColor.rgb, uShadowColor.a * shadow);
}