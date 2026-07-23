#include "Shared/tonemap.glsl"

in vec2 fUv;

#ifdef EXTERNAL
    layout(binding=0) uniform samplerExternalOES uTextureLeft;
    layout(binding=1) uniform samplerExternalOES uTextureRight;
#else
    layout(binding=0) uniform sampler2D uTextureLeft;
    layout(binding=1) uniform sampler2D uTextureRight;
#endif

layout(location=0) out vec4 FragColor;

#ifdef MULTI_VIEW
    #define ACTIVE_EYE gl_ViewID_OVR
#else
    uniform uint uActiveEye;
    #define ACTIVE_EYE uActiveEye
#endif


#ifdef USE_COLOR
    uniform vec4 uColor;
#endif

void main()
{
    #ifdef FIXED_EYE
        if (ACTIVE_EYE != uint(FIXED_EYE))
            discard;
    #endif

    if (ACTIVE_EYE == 0u)
        FragColor = texture(uTextureLeft, fUv);
    else 
        FragColor = texture(uTextureRight, fUv);

    #ifdef USE_COLOR
        FragColor *= uColor;
    #endif

    toneMapTex(FragColor);
}