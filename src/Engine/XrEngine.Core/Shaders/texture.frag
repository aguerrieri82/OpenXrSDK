in vec2 fUv;

#ifdef EXTERNAL
    uniform samplerExternalOES uTexture;
#else
    uniform sampler2D uTexture;
#endif


uniform vec2 uOffset;
uniform vec2 uScale;

out vec4 FragColor;

void main()
{
    FragColor = texture(uTexture, fUv);
}