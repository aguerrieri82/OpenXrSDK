in vec2 fUv;
in vec2 fUv2;

flat in vec4 fConst;
        
#ifdef USE_EXPOSURE

uniform float uExposure[IMG_COUNT];

#endif

layout(binding=0) uniform sampler2DArray uTextureArray;

layout(location = 0) out vec4 color;

vec4 setExposure(vec4 c, float expo)
{
    float y = dot(c.rgb, vec3(0.2126, 0.7152, 0.0722));
    float y2 = y * exp(expo);

    float s = y2 / max(y, 0.003);
    s = clamp(s, 0.25, 4.0);

    return vec4(c.rgb * s, 1.0);
}

void main()
{
    int img0 = int(fConst.x);
    int img1 = int(fConst.y);

    vec4 c0, c1;

    if (img0 >= 0)
    {
        c0 = texture(uTextureArray, vec3(fUv, float(img0)));
        #ifdef USE_EXPOSURE
            c0 = setExposure(c0, uExposure[img0]);
        #endif
    }

    #ifdef MIX_COLORS
  
    if (img1 >= 0)
    {
        c1 = texture(uTextureArray, vec3(fUv2, float(img1)));
        #ifdef USE_EXPOSURE
            c1 = setExposure(c1, uExposure[img1]);
        #endif

        color = mix(c0, c1, 0.5);
    }
    else
    #endif
        color = c0;
}