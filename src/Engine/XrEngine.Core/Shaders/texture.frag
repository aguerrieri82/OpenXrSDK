in vec2 fUv;

#ifdef EXTERNAL
    uniform samplerExternalOES uTexture;
#else
    uniform sampler2D uTexture;
#endif

out vec4 FragColor;

void main()
{
    vec2 uv = fUv;

    #ifdef USE_TRANSFORM

     (vec3(texcoord.xy, 1) * uMaterial.texTransform).xy;
    #endif

    FragColor = texture(uTexture, uv);
}