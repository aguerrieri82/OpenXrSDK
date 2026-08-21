
#ifdef USE_FETCH

layout(location = 0) inout highp vec4 outColor;

#else

#ifdef MULTISAMPLE
layout(binding = 0) uniform highp sampler2DMS uSource;
#else
layout(binding = 0) uniform sampler2D uSource;
#endif

#endif

layout(DEST_FORMAT, binding = 1) writeonly uniform highp image2D uDestination;

void main()
{
    ivec2 srcPos = ivec2(gl_FragCoord.xy);

    // Only one source fragment writes each destination texel.

    if (any(notEqual(srcPos % DOWNSAMPLE, ivec2(0))))
        return;

    vec4 color;

#ifdef USE_FETCH
    color = outColor;
#else
#ifdef MULTISAMPLE
    color = vec4(0.0);

    for (int i = 0; i < MULTISAMPLE; i++)
        color += texelFetch(uSource, srcPos, i);

    color /= float(MULTISAMPLE);
#else
    color = texelFetch(uSource, srcPos, 0);
#endif
#endif

    imageStore(
        uDestination,
        srcPos / DOWNSAMPLE,
        color
    );
}