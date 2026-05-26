
#ifdef MULTI_VIEW
	layout(binding=0) uniform highp sampler2DArray srcImage;
#else
    layout(binding=0) uniform sampler2D srcImage;
#endif

uniform int uSize;
uniform vec4 uColor;

out vec4 fragColor;

void main() {
   
    ivec2 coords = ivec2(gl_FragCoord.xy);
    #ifdef MULTI_VIEW
        float color = texelFetch(srcImage, ivec3(coords, gl_ViewID_OVR), 0).r;
    #else
        float color = texelFetch(srcImage, coords, 0).r;
    #endif



    if (color == 0.0) 
    {
        for (int x = -uSize; x <= uSize; ++x) 
        {
            for (int y = -uSize; y <= uSize; ++y) 
            {
                ivec2 offsetCoords = coords + ivec2(x, y);

                #ifdef MULTI_VIEW
                    float neighborColor = texelFetch(srcImage, ivec3(offsetCoords, gl_ViewID_OVR), 0).r;
                #else
                    float neighborColor = texelFetch(srcImage, offsetCoords, 0).r;
                #endif

                if (neighborColor != 0.0) 
                {
                    fragColor = uColor;
                    return;
                }
            }
         }
    }

    discard;  
}