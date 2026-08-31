#include "tonemap.glsl"

#ifndef DEPTH_LOCATION
    #define DEPTH_LOCATION 1
#endif

#if defined(COPY_DEPTH) && !defined(MANUAL_DEPTH_TEST)
    #define WRITE_DEPTH
#endif

#if defined(COPY_DEPTH_IMG) && !defined(MOTION_VECTORS_DEPTH) && !defined(MANUAL_DEPTH_TEST)
    #define WRITE_DEPTH_IMG
#endif

#if (defined(MOTION_VECTORS) || defined(MOTION_VECTORS_DEPTH)) && !defined(MANUAL_DEPTH_TEST)
    #define WRITE_MOTION_IMG
#endif

#if defined(MANUAL_DEPTH_TEST)
    #ifdef MOTION_VECTORS_DEPTH
        #define READ_MOTION_DEPTH
    #else
        #define READ_DEPTH_IMG
    #endif
#endif

#if defined(WRITE_DEPTH)
    layout(location=DEPTH_LOCATION) out float outDepth;
#endif

#if defined(WRITE_DEPTH_IMG) || defined(READ_DEPTH_IMG)

    #ifdef MULTI_VIEW
        #ifdef READ_DEPTH_IMG
            layout(r32f, binding=0) uniform readonly mediump image2DArray uDepthImage;
        #else
            layout(r32f, binding=0) uniform writeonly mediump image2DArray uDepthImage;
        #endif
    #else
        #ifdef READ_DEPTH_IMG
            layout(r32f, binding=0) uniform readonly mediump image2D uDepthImage;
        #else
            layout(r32f, binding=0) uniform writeonly mediump image2D uDepthImage;
        #endif
    #endif

    uniform vec2 uDepthImageScale;

#endif

#if defined(WRITE_MOTION_IMG) || defined(READ_MOTION_DEPTH)

    #ifdef WRITE_MOTION_IMG
        in vec4 fPrevClipPos;
        in vec4 fCurClipPos;
    #endif

    #ifdef MULTI_VIEW
        #ifdef READ_MOTION_DEPTH
            layout(rgba16f, binding=1) uniform readonly highp image2DArray uMotionImage;
        #else
            layout(rgba16f, binding=1) uniform writeonly highp image2DArray uMotionImage;
        #endif
    #else
        #ifdef READ_MOTION_DEPTH
            layout(rgba16f, binding=1) uniform readonly highp image2D uMotionImage;
        #else
            layout(rgba16f, binding=1) uniform writeonly highp image2D uMotionImage;
        #endif
    #endif

    uniform vec2 uMotionImageScale;

#endif

#ifdef MANUAL_DEPTH_TEST

bool manualDepthTest()
{
    #ifdef READ_MOTION_DEPTH

        ivec2 dp = ivec2(gl_FragCoord.xy * uMotionImageScale);

        #ifdef MULTI_VIEW
            float depth = imageLoad(uMotionImage, ivec3(dp, int(gl_ViewID_OVR))).z;
        #else
            float depth = imageLoad(uMotionImage, dp).z;
        #endif

    #else

        ivec2 dp = ivec2(gl_FragCoord.xy * uDepthImageScale);

        #ifdef MULTI_VIEW
            float depth = imageLoad(uDepthImage, ivec3(dp, int(gl_ViewID_OVR))).r;
        #else
            float depth = imageLoad(uDepthImage, dp).r;
        #endif

    #endif

    return 1.0 - gl_FragCoord.z >= depth;
}

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

    #ifdef WRITE_DEPTH
        outDepth = 1.0 - gl_FragCoord.z;
    #endif

    #ifdef WRITE_DEPTH_IMG

        ivec2 dp = ivec2(gl_FragCoord.xy * uDepthImageScale);

        #ifdef MULTI_VIEW
            imageStore(uDepthImage, ivec3(dp, int(gl_ViewID_OVR)), vec4(1.0 - gl_FragCoord.z));
        #else
            imageStore(uDepthImage, dp, vec4(1.0 - gl_FragCoord.z));
        #endif

    #endif

    #ifdef WRITE_MOTION_IMG

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

#undef WRITE_DEPTH
#undef WRITE_DEPTH_IMG
#undef WRITE_MOTION_IMG
#undef READ_DEPTH_IMG
#undef READ_MOTION_DEPTH