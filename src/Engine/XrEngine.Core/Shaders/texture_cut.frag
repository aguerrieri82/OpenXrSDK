#include "Shared/tonemap.glsl"

in vec2 fUv;

#ifdef EXTERNAL
    layout(binding = 0) uniform samplerExternalOES uTexture;
#else
    layout(binding = 0) uniform sampler2D uTexture;
#endif

uniform vec4 uColor;
uniform int uCount;
#define MAIN_QUOD_ID (uint(uCount) - 1u)

#define MODE_MAIN   0u
#define MODE_LAYERS 1u

struct QuadStyle
{
    vec4 BackColor;
    float Opacity;
};

layout(std430, binding = 10) readonly buffer QuadStyleBuffer
{
    QuadStyle uQuads[];
};

layout(location = 0) out vec4 FragColor;



void main()
{
    #ifdef DEPTH_ONLY
        return;
    #endif

    uint quadId = uint(gl_PrimitiveID) / 2u;

    QuadStyle style = uQuads[quadId];

    if (MODE == MODE_MAIN)
    {
        if (quadId == MAIN_QUOD_ID)
        {
            FragColor = texture(uTexture, fUv) * uColor;
        }
        else
        {
            FragColor = style.BackColor * uColor;
        }

        toneMapTex(FragColor);

        return;
    }

    if (MODE == MODE_LAYERS)
    {
        if (quadId == MAIN_QUOD_ID)
            discard;

        FragColor = texture(uTexture, fUv) * uColor;
        FragColor.a *= style.Opacity;

        toneMapTex(FragColor);

        return;
    }

    discard;
}