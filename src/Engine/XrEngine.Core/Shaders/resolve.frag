#include "Shared/tonemap.glsl"

#ifdef MULTI_VIEW
    layout(binding = 0) uniform sampler2DArray uImage;
#elif defined(SAMPLE_COUNT) && SAMPLE_COUNT > 1
    layout(binding = 0) uniform sampler2DMS uImage;
#else
    layout(binding = 0) uniform sampler2D uImage;
#endif

in vec2 fUv;

layout(location=0) out vec4 FragColor;

void main()
{
    vec4 color;

#ifdef MULTI_VIEW

    color = texture(uImage, vec3(fUv, gl_ViewID_OVR));

#elif defined(SAMPLE_COUNT) && SAMPLE_COUNT > 1

    ivec2 size = textureSize(uImage);
    ivec2 pos = ivec2(clamp(fUv, vec2(0.0), vec2(0.999999)) * vec2(size));

    color = vec4(0.0);

    for (int i = 0; i < SAMPLE_COUNT; i++)
        color += texelFetch(uImage, pos, i);

    color /= float(SAMPLE_COUNT);

#else

    color = texture(uImage, fUv);

#endif

#ifdef RESOLVE_ALPHA
    if (color.a > 0.000001)
        color.rgb /= color.a;
#endif

#ifdef TONE_MAP

    #if TONE_MAP == 1
        color.rgb = toneMap(color.rgb);
    #endif

    #if TONE_MAP == 2
        color.rgb = toneMapNeutral(color.rgb);
    #endif

#endif

#ifdef SRGB
    color.rgb = linearTosRGB(color.rgb);
#endif

    FragColor = color;
}