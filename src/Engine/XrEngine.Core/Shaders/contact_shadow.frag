#define FRAGMENT_SHADER

#include "Shared/uniforms.glsl"
#include "Shared/position.glsl"


#ifdef DEPTH_SAMPLES

    #ifdef MULTI_VIEW
        layout(binding = 0) uniform highp sampler2DMSArray uDepthTexture;
    #else
        layout(binding = 0) uniform highp sampler2DMS uDepthTexture;
    #endif

#else

    #ifdef MULTI_VIEW
        layout(binding = 0) uniform highp sampler2DArray uDepthTexture;
    #else
        layout(binding = 0) uniform highp sampler2D uDepthTexture;
    #endif

#endif


layout(std140, binding = 17) uniform ContactShadow
{
    vec4 uLightDirWorld;   // xyz = direction from receiver toward light

    vec2 uViewSize;
    float uMaxDistance;
    float uThickness;      // window-depth thickness, not meters

    float uStrength;
    float uStepCount;
    float uDepthBias;
    float uFadeDistance;
};

layout(location = 0) in vec2 fUv;
layout(location = 0) out float outContactShadow;

#define MAX_CONTACT_STEPS 30


float sampleDepth(vec2 uv)
{
#ifdef DEPTH_SAMPLES

    #ifdef MULTI_VIEW

        ivec3 size = textureSize(uDepthTexture);
        ivec2 p = ivec2(uv * vec2(size.xy));

        if (p.x < 0 || p.y < 0 || p.x >= size.x || p.y >= size.y)
            return 1.0;

        int layer = int(gl_ViewID_OVR);

        float depth = 1.0;

        for (int i = 0; i < DEPTH_SAMPLES; ++i)
            depth = min(depth, texelFetch(uDepthTexture, ivec3(p, layer), i).r);

        return depth;

    #else

        ivec2 size = textureSize(uDepthTexture);
        ivec2 p = ivec2(uv * vec2(size));

        if (p.x < 0 || p.y < 0 || p.x >= size.x || p.y >= size.y)
            return 1.0;

        float depth = 1.0;

        for (int i = 0; i < DEPTH_SAMPLES; ++i)
            depth = min(depth, texelFetch(uDepthTexture, p, i).r);

        return depth;

    #endif

#else

    #ifdef MULTI_VIEW
        return texture(uDepthTexture, vec3(uv, float(gl_ViewID_OVR))).r;
    #else
        return texture(uDepthTexture, uv).r;
    #endif

#endif
}


vec3 reconstructWorld(vec2 uv, float windowDepth)
{
    vec2 ndcXY = uv * 2.0 - 1.0;
    float ndcZ = windowDepth * 2.0 - 1.0;

    vec4 world = getViewProjInv() * vec4(ndcXY, ndcZ, 1.0);
    return world.xyz / world.w;
}


float projectWindowDepth(vec3 worldPos, out vec2 uv)
{
    vec4 clip = getViewProj() * vec4(worldPos, 1.0);
    vec3 ndc = clip.xyz / clip.w;

    uv = ndc.xy * 0.5 + 0.5;

    return ndc.z * 0.5 + 0.5;
}


void main()
{
    float baseDepth = sampleDepth(fUv);

    if (baseDepth >= 1.0)
    {
        outContactShadow = 0.0;
        return;
    }

    vec3 worldPos = reconstructWorld(fUv, baseDepth);
    vec3 rayDir = normalize(uLightDirWorld.xyz);

    float steps = max(uStepCount, 1.0);
    float stepLen = uMaxDistance / steps;

    float contact = 0.0;

    for (int i = 1; i <= MAX_CONTACT_STEPS; ++i)
    {
        if (float(i) > uStepCount)
            break;

        float dist = float(i) * stepLen;
        vec3 sampleWorld = worldPos + rayDir * dist;

        vec2 sampleUv;
        float rayDepth = projectWindowDepth(sampleWorld, sampleUv);

        float sceneDepth = sampleDepth(sampleUv);

        float dz = rayDepth - sceneDepth;

        float hit =
            dz > uDepthBias &&
            dz < uThickness
                ? 1.0
                : 0.0;

        float fade = 1.0 - clamp(dist / uFadeDistance, 0.0, 1.0);

        contact = max(contact, hit * fade);
    }

    outContactShadow = clamp(contact * uStrength, 0.0, 1.0);
}