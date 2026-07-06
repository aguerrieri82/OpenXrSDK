in vec2 fUv;

#ifdef ARRAY
    layout(binding=1) uniform sampler2DArray uTexture;
    uniform int uIndex;
#else
    layout(binding=1) uniform sampler2D uTexture;
#endif


layout(location=0) out vec4 FragColor;

void main()
{
    #ifdef ARRAY
       FragColor = texture(uTexture, vec3(fUv, uIndex));
    #else
       FragColor = texture(uTexture, fUv);
    #endif
}