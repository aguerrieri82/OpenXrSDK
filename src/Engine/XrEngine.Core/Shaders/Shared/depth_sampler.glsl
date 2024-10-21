
#ifdef MULTISAMPLE

    #ifdef MULTI_VIEW

        precision mediump sampler2DMSArray;

        uniform sampler2DMSArray uDepth;

        float getDepth(vec2 pos) 
        {
            vec2 texSize = vec2(textureSize(uDepth));

            return texelFetch(uDepth, ivec3(pos * texSize, gl_ViewID_OVR), 1).r; 
        }

    #else

        uniform sampler2DMS uDepth;

        float getDepth(vec2 pos) 
        {
            vec2 texSize = vec2(textureSize(uDepth));

            return texelFetch(uDepth, ivec2(pos * texSize), 1).r; 
        }


    #endif

#else

    #ifdef MULTI_VIEW

        precision mediump sampler2DArray;

        uniform sampler2DArray uDepth;

        float getDepth(vec2 pos) 
        {
            return texture(uDepth, vec3(pos, gl_ViewID_OVR)).r; 
        }

    #else

        uniform sampler2D uDepth;

        float getDepth(vec2 pos) 
        {
            return texture(uDepth, pos).r; 
        }

    #endif

#endif
