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

out vec4 FragColor;

void main()
{
    vec2 uv = fUv;

    #ifdef USE_TRANSFORM
        uv = (vec3(uv, 1) * uUvTransform).xy;
    #endif

    #ifdef CHECK_TEXTURE
        if (uHasTexture == 1u)
            FragColor = texture(uTexture, uv) * uColor;
        else
            FragColor = uColor;
    #else
        FragColor = texture(uTexture, uv) * uColor;
    #endif
}