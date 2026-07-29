#include "tonemap.glsl"

#ifndef DEPTH_LOCATION
    #define DEPTH_LOCATION 1
#endif

#ifdef COPY_DEPTH
    layout(location=DEPTH_LOCATION) out float outDepth;
#endif

#if defined(COPY_DEPTH_IMG) && !defined(MOTION_VECTORS_DEPTH)

    #ifdef MULTI_VIEW
        layout(r32f, binding=0) uniform writeonly mediump image2DArray uDepthImage;
    #else
        layout(r32f, binding=0) uniform writeonly mediump image2D uDepthImage;
    #endif

    uniform vec2 uDepthImageScale;

#endif

#if defined(MOTION_VECTORS) || defined(MOTION_VECTORS_DEPTH)

    in vec4 fPrevClipPos;
    in vec4 fCurClipPos;

    #ifdef MULTI_VIEW
        layout(rgba16f, binding=1) uniform writeonly highp image2DArray uMotionImage;
    #else
        layout(rgba16f, binding=1) uniform writeonly highp image2D uMotionImage;
    #endif

    uniform vec2 uMotionImageScale;

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
        outDepth = 1.0 - gl_FragCoord.z;
    #endif

    #if defined(COPY_DEPTH_IMG) && !defined(MOTION_VECTORS_DEPTH)

        ivec2 dp = ivec2(gl_FragCoord.xy * uDepthImageScale);

        #ifdef MULTI_VIEW
            imageStore(uDepthImage, ivec3(dp, int(gl_ViewID_OVR)), vec4(1.0 - gl_FragCoord.z));
        #else
            imageStore(uDepthImage, dp, vec4(1.0 - gl_FragCoord.z));
        #endif

    #endif

    #if defined(MOTION_VECTORS) || defined(MOTION_VECTORS_DEPTH)

        vec3 cur = fCurClipPos.xyz / fCurClipPos.w;
        vec3 prev = fPrevClipPos.xyz / fPrevClipPos.w;
        vec2 motion = cur.xy - prev.xy;

        ivec2 mp = ivec2(gl_FragCoord.xy * uMotionImageScale);

        #ifdef MOTION_VECTORS_DEPTH
            vec4 motionDepth = vec4(motion, 1.0 - gl_FragCoord.z, 0.0);
        #else
            vec4 motionDepth = vec4(motion, 0.0, 0.0);
        #endif

        #ifdef MULTI_VIEW
             imageStore(uMotionImage, ivec3(mp, int(gl_ViewID_OVR)), motionDepth);
        #else
             imageStore(uMotionImage, mp, motionDepth);
        #endif

    #endif
}


void doPost(inout vec4 fragColor)
{
    doPostRgb(fragColor.rgb);
}