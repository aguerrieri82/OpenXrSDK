
#ifdef MULTI_VIEW
	layout(binding=0) uniform highp sampler2DArray srcImage;
#else
    layout(binding=0) uniform sampler2D srcImage;
#endif

uniform int uSize;

out vec4 fragColor;

void main() {
   
    ivec2 coords = ivec2(gl_FragCoord.xy);
    #ifdef MULTI_VIEW
        vec4 color = texelFetch(srcImage, ivec3(coords, gl_ViewID_OVR), 0);
    #else
        vec4 color = texelFetch(srcImage, coords, 0);
    #endif



    if (color == vec4(0.0)) 
    {
        for (int x = -uSize; x <= uSize; ++x) 
        {
            for (int y = -uSize; y <= uSize; ++y) 
            {
                ivec2 offsetCoords = coords + ivec2(x, y);

                #ifdef MULTI_VIEW
                    vec4 neighborColor = texelFetch(srcImage, ivec3(offsetCoords, gl_ViewID_OVR), 0);
                #else
                    vec4 neighborColor = texelFetch(srcImage, offsetCoords, 0);
                #endif

                if (neighborColor != vec4(0.0)) 
                {
                    fragColor = neighborColor;
                    return;
                }
            }
         }
    }

    discard;  
}