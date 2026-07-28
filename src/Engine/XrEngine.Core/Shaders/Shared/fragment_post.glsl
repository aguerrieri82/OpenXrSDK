#include "tonemap.glsl"

#ifndef DEPTH_LOCATION
    #define DEPTH_LOCATION 1
#endif

#ifndef MOTION_VECTORS_LOCATION
    #define MOTION_VECTORS_LOCATION 2
#endif

#ifdef COPY_DEPTH
    layout(location=DEPTH_LOCATION) out float outDepth;
#endif

#ifdef COPY_DEPTH_IMG

    #ifdef MULTI_VIEW
        layout(r32f, binding=0) uniform writeonly mediump image2DArray uDepthImage;
    #else
        layout(r32f, binding=0) uniform writeonly mediump image2D uDepthImage;
    #endif

    uniform vec2 uDepthImageScale;

#endif

#ifdef MOTION_VECTORS

    in vec4 prevClipPos;
    in vec4 curClipPos;

    layout(location=MOTION_VECTORS_LOCATION) out vec4 outVector;

#endif


void doPostRgb(inout vec3 fragColor)
{
    #ifdef TONE_MAP

        #if TONE_MAP == 1
            fragColor = toneMap(fragColor);
        #endif

        #if TONE_MAP == 2
            fragColor = toneMapNeutral(fragColor);
        #endif

    #endif

    #ifdef COPY_DEPTH
        outDepth = gl_FragCoord.z;
    #endif

    #ifdef COPY_DEPTH_IMG

        ivec2 p = ivec2(gl_FragCoord.xy * uDepthImageScale);

        #ifdef MULTI_VIEW
            imageStore(uDepthImage, ivec3(p, int(gl_ViewID_OVR)), vec4(gl_FragCoord.z));
        #else
            imageStore(uDepthImage, p, vec4(gl_FragCoord.z));
        #endif

    #endif

    #ifdef MOTION_VECTORS
        vec3 cur = curClipPos.xyz / curClipPos.w;
        vec3 prev = prevClipPos.xyz / prevClipPos.w;

        outVector.xyz = cur - prev;
        outVector.w = 0.0;
    #endif
}


void doPost(inout vec4 fragColor)
{
    doPostRgb(fragColor.rgb);
}