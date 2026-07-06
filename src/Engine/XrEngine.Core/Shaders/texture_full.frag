in vec2 fUv;

layout(location=0) out vec4 FragColor;

#ifdef MULTI_VIEW
    layout(binding=0) uniform sampler2DArray uTextures;

    void main()
    {
        FragColor = texture(uTextures, vec3(fUv, gl_ViewID_OVR));
    }

#else

    #ifdef EXTERNAL
        layout(binding=0) uniform samplerExternalOES uTexture;
    #else
        layout(binding=0) uniform sampler2D uTexture;
    #endif

    void main()
    {
        FragColor = texture(uTexture, fUv);
    }
#endif
