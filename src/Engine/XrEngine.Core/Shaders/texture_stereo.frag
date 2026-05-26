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

    void main()
    {
        if (gl_ViewID_OVR == 0u)
            FragColor = texture(uTextureLeft, fUv);
        else 
            FragColor = texture(uTextureRight, fUv);
    }

#else
    uniform uint uActiveEye;

    void main()
    {
        if (uActiveEye == 0u)
            FragColor = texture(uTextureLeft, fUv);
        else 
            FragColor = texture(uTextureRight, fUv);
    }
#endif
