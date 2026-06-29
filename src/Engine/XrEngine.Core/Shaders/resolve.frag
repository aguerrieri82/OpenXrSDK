#include "Shared/tonemap.glsl"

#ifdef MULTI_VIEW
    layout(binding = 0) uniform highp sampler2DArray uImage;
#else
    layout(binding = 0) uniform sampler2D uImage;
#endif

in vec2 fUv;

layout(location=0) out vec4 FragColor;

void main()
{
    vec4 color;

	#ifdef MULTI_VIEW
	   color = texture(uImage, vec3(fUv, gl_ViewID_OVR));
	#else
	   color = texture(uImage, fUv);
	#endif

    /*
    if (color.a > 0.000001)
        color.rgb /= color.a;
    */

    #ifdef TONE_MAP

        #if TONE_MAP == 1
           color.rgb = toneMap(color.rgb);
        #endif

        #if TONE_MAP == 2
           color.rgb = toneMapNeutral(color.rgb);
        #endif

        #ifdef SRGB
            color.rgb = linearTosRGB(color.rgb);
        #endif

    #endif

    FragColor = color;
}