#ifndef DEPTH_SAMPLES
    #define DEPTH_SAMPLES 2
#endif

#ifdef TEXTURE_ARRAY
    uniform int uViewIndex;

    int getViewIndex()
    {
        return uViewIndex;
    }
#endif

#ifdef MULTI_VIEW
    int getViewIndex()
    {
        return int(gl_ViewID_OVR);
    }
#endif

#ifdef MULTISAMPLE

    #if defined(MULTI_VIEW) || defined(TEXTURE_ARRAY)

        layout(binding = 10) uniform sampler2DMSArray uDepth;

        float getDepth(vec2 uv) 
        {
            ivec2 size = textureSize(uDepth).xy;
            ivec2 p = ivec2(uv * vec2(size));

            float d = 1.0;

            for (int i = 0; i < DEPTH_SAMPLES; i++)
                d = min(d, texelFetch(uDepth, ivec3(p, getViewIndex()), i).r);

            return d;
        }

    #else

        layout(binding = 10) uniform sampler2DMS uDepth;

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

        layout(binding = 10) uniform sampler2DArray uDepth;

        float getDepth(vec2 pos) 
        {
            return texture(uDepth, vec3(pos, getViewIndex())).r; 
        }

    #else

        layout(binding = 10) uniform sampler2D uDepth;

        float getDepth(vec2 pos) 
        {
            return texture(uDepth, pos).r; 
        }

    #endif

#endif
