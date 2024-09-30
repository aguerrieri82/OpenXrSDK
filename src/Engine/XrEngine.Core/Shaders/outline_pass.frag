

in vec2 inUV;
out vec4 FragColor;

uniform float uSize;
uniform vec4 uColor;
uniform vec2 uTexSize;


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


void main()
{    

    float value = getDepth(inUV);
    if (value < 1.0)
        discard;

    bool found = false;

    for (float x = -uSize; x <= uSize; x++)
    {
        for (float y = -uSize; y <= uSize; y++)
        {
            vec2 offset = vec2(x, y) * uTexSize;
            value = getDepth(inUV + offset);
            if (value < 1.0)
            {
                found = true;
                break;
            }
        }
        if (found)
            break;
    }

    if (!found)
        discard;

    FragColor = uColor;
}