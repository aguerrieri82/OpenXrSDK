#include "Shared/fragment_post.glsl"

in vec2 fUv;

#ifdef EXTERNAL
    layout(binding=0) uniform samplerExternalOES uTexture;
#else
    layout(binding=0) uniform sampler2D uTexture;
#endif

#ifdef USE_TRANSFORM
    uniform mat3 uUvTransform;
#endif

#ifdef CHECK_TEXTURE
    uniform uint uHasTexture;
#endif

uniform vec4 uColor;

layout(location=0) out vec4 FragColor;

void main()
{
    vec2 uv = fUv;

    #ifdef USE_TRANSFORM

        vec3 tuv = vec3(uv, 1.0) * uUvTransform;
        uv = tuv.xy / tuv.z;

    #endif
    
    #ifdef CLAMP
        if (any(lessThan(uv, vec2(0.0))) || any(greaterThan(uv, vec2(1.0))))
            discard;
    #endif

    #ifdef CHECK_TEXTURE
        if (uHasTexture == 1u)
            FragColor = texture(uTexture, uv) * uColor;
        else
            FragColor = uColor;
    #else
        FragColor = texture(uTexture, uv) * uColor;
    #endif

    doPost(FragColor);

    fixSrgbTex(FragColor);
}