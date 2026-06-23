
in vec3 vWorldPos;
in vec3 vWorldNormal;

layout(binding = 0) uniform highp sampler2DArray uCaptureColor;

#ifdef USE_DEPTH
    layout(binding = 1) uniform highp sampler2D uCaptureDepth;
    uniform float uDepthBias;
#endif

uniform mat4 uCaptureViewProj;
uniform vec3 uCaptureCameraPos;
uniform int uColorIndex;

uniform float uMinFrontness;

layout(location = 0) out vec4 outAccum;

const bool FLIP_COLOR_Y = true;
const bool FLIP_DEPTH_Y = false;

void main()
{
    vec4 clip = uCaptureViewProj * vec4(vWorldPos, 1.0);

    if (clip.w <= 0.0)
        discard;

    vec3 ndc = clip.xyz / clip.w;

    if (ndc.x < -1.0 || ndc.x > 1.0 ||
        ndc.y < -1.0 || ndc.y > 1.0 ||
        ndc.z < -1.0 || ndc.z > 1.0)
    {
        discard;
    }

    vec2 projectedUv = ndc.xy * 0.5 + 0.5;

    vec2 colorUv = projectedUv;
    vec2 depthUv = projectedUv;

    if (FLIP_COLOR_Y)
        colorUv.y = 1.0 - colorUv.y;

    if (FLIP_DEPTH_Y)
        depthUv.y = 1.0 - depthUv.y;

#ifdef USE_DEPTH

    float pointDepth01 = ndc.z * 0.5 + 0.5;
    float sampledDepth01 = texture(uCaptureDepth, depthUv).r;

    if (sampledDepth01 <= 0.0 || sampledDepth01 >= 1.0)
        discard;

    if (pointDepth01 > sampledDepth01 + uDepthBias)
        discard;

#endif

    vec3 toCamera = normalize(uCaptureCameraPos - vWorldPos);
    float frontness = dot(normalize(vWorldNormal), toCamera);

    if (frontness < uMinFrontness)
        discard;

    vec3 color = texture(
        uCaptureColor,
        vec3(colorUv, float(uColorIndex))
    ).rgb;

    float weight = frontness * frontness;

    outAccum = vec4(color * weight, weight);
}