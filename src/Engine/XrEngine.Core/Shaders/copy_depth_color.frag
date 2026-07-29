#ifndef CHANNEL
    #define CHANNEL r
#endif

#ifndef PRECISION
    #define PRECISION highp
#endif

#ifdef USE_FETCH

    layout(location = 0) inout PRECISION vec4 outColor;

#else

    #ifdef MULTI_VIEW
        layout(binding=10) uniform PRECISION sampler2DArray uImage;
    #else
        layout(binding=10) uniform PRECISION sampler2D uImage;
    #endif

    in vec2 fUv;

#endif



void main()
{
#ifdef USE_FETCH

    gl_FragDepth = 1.0 - outColor.CHANNEL;

#else

    #ifdef MULTI_VIEW
        gl_FragDepth = 1.0 - texture(uImage, vec3(fUv, float(gl_ViewID_OVR))).CHANNEL;
    #else
        gl_FragDepth = 1.0 - texture(uImage, fUv).CHANNEL;
    #endif

#endif
}