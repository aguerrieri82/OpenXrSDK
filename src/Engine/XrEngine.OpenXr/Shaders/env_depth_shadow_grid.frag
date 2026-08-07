
#include "[XrEngine.Core]Shared/uniforms.glsl"
#include "[XrEngine.Core]Shared/position.glsl"
#include "[XrEngine.Core]Shared/depth_sampler.glsl"
#include "[XrEngine.Core]Shared/shadow.glsl"

uniform float uEnvDepthBias;
uniform mat4  uLightMatrix;
uniform vec4  uShadowColor;

in vec3 fWorldPos;
in vec2 fUv;
in float fEnvDepth;

out vec4 outColor;

vec2 worldToProjectionUv(vec3 pWorld)
{
    vec4 clip = getViewProj() * vec4(pWorld, 1.0);
    vec2 ndc = clip.xy / clip.w;
    return ndc * 0.5 + 0.5;
}

float worldToProjectionDepth(vec3 pWorld)
{
    vec4 clip = getViewProj() * vec4(pWorld, 1.0);
    float z = clip.z / clip.w;
    return z * 0.5 + 0.5;
}

void main()
{
    //outColor = vec4(fEnvDepth,fEnvDepth,fEnvDepth,1.0);
    //return;


    vec2 renderUv = worldToProjectionUv(fWorldPos);

    float realDepth = worldToProjectionDepth(fWorldPos);
    float virtualDepth = getDepth(renderUv);

    if (virtualDepth < realDepth - uEnvDepthBias)
        discard;

    float shadow = calculateShadow(
        uLightMatrix * vec4(fWorldPos, 1.0),
        vec3(0.0, 1.0, 0.0),
        vec3(0.0, 1.0, 0.0)
    );

    if (shadow <= 0.0)
        discard;

    outColor = vec4(
        uShadowColor.rgb,
        uShadowColor.a * shadow
    );
}