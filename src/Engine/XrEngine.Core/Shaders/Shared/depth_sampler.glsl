#ifndef DEPTH_SAMPLES
    #define DEPTH_SAMPLES 2
#endif

#ifdef TEXTURE_ARRAY
    uniform int uViewIndex;
#endif

#ifdef MULTI_VIEW
    int uViewIndex = int(gl_ViewID_OVR);
#endif

#ifdef MULTISAMPLE

    #if defined(MULTI_VIEW) || defined(TEXTURE_ARRAY)

        layout(binding = 10) uniform highp sampler2DMSArray uDepth;

        float getDepth(vec2 uv) 
        {
            ivec2 size = textureSize(uDepth).xy;
            ivec2 p = ivec2(uv * vec2(size));

            float d = 1.0;

            for (int i = 0; i < DEPTH_SAMPLES; i++)
                d = min(d, texelFetch(uDepth, ivec3(p, uViewIndex), i).r);

            return d;
        }

    #else

        layout(binding = 10) uniform highp sampler2DMS uDepth;

        float getDepth(vec2 uv) 
        {
            ivec2 size = textureSize(uDepth);
            ivec2 p = ivec2(uv * vec2(size));

            float d = 1.0;

            for (int i = 0; i < DEPTH_SAMPLES; i++)
                d = min(d, texelFetch(uDepth, p, i).r);

            return d;
        }


    #endif

#else

    #if defined(MULTI_VIEW) || defined(TEXTURE_ARRAY)

        precision mediump sampler2DArray;

        layout(binding = 10) uniform highp sampler2DArray uDepth;

        float getDepth(vec2 pos) 
        {
            return texture(uDepth, vec3(pos, uViewIndex)).r; 
        }

    #else

        layout(binding = 10) uniform highp sampler2D uDepth;

        float getDepth(vec2 pos) 
        {
            return texture(uDepth, pos).r; 
        }

    #endif

#endif
