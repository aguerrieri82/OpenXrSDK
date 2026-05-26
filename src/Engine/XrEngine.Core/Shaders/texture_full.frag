in vec2 fUv;

layout(location=0) out vec4 FragColor;

#ifdef MULTI_VIEW
    layout(binding=0) uniform sampler2D uTextures[2];

    void main()
    {
        FragColor = texture(uTextures[gl_ViewID_OVR], fUv)
    }

#else
    layout(binding=0) uniform sampler2D uTexture;

    void main()
    {
        FragColor = texture(uTexture, fUv);
    }
#endif
