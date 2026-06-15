#define FRAGMENT_SHADER

#include "[XrEngine.Core]Shared/uniforms.glsl"
#include "[XrEngine.Core]Shared/position.glsl"
#include "[XrEngine.Core]Shared/depth_sampler.glsl"


in highp vec2 fUv;

layout(location = 0) out vec4 outColor;

layout(binding = 0) uniform sampler2D uColor;

uniform mat4  uQuadWorld;
uniform float uDepthBias;

// Optional compile-time switches:
//
// #define REVERSED_DEPTH
// #define FLIP_QUAD_Y



void main()
{
    vec2 quadUv = fUv;

#ifdef FLIP_QUAD_Y
    quadUv.y = 1.0 - quadUv.y;
#endif

    // Local quad is centered at origin, size 1x1.
    vec2 localXY = quadUv - vec2(0.5);

    vec4 worldPos = uQuadWorld * vec4(localXY, 0.0, 1.0);
    vec4 clip = getViewProj() * worldPos;

    if (clip.w <= 0.0)
        discard;

    vec3 ndc = clip.xyz / clip.w;

    if (ndc.x < -1.0 || ndc.x > 1.0 ||
        ndc.y < -1.0 || ndc.y > 1.0 ||
        ndc.z < -1.0 || ndc.z > 1.0)
    {
        discard;
    }

    vec2 depthUv = ndc.xy * 0.5 + 0.5;


    float quadDepth = ndc.z * 0.5 + 0.5;
    float sceneDepth = getDepth(depthUv);

#ifdef REVERSED_DEPTH
    bool passDepth = quadDepth >= sceneDepth - uDepthBias;
#else
    bool passDepth = quadDepth <= sceneDepth + uDepthBias;
#endif

    if (passDepth)
        outColor = texture(uColor, quadUv);
    else    
        outColor = vec4(0.0, 0.0, 0.0, 0.0);
}