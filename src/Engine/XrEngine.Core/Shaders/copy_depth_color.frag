#ifdef MULTI_VIEW
    layout(binding=10) uniform highp sampler2DArray uImage;
#else
    layout(binding=10) uniform highp sampler2D uImage;
#endif

in vec2 fUv;

void main()
{
#ifdef MULTI_VIEW
    gl_FragDepth = texture(uImage, vec3(fUv, float(gl_ViewID_OVR))).r;
#else
    gl_FragDepth = texture(uImage, fUv).r;
#endif
}