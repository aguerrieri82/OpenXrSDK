#include "Shared/tonemap.glsl"

#if defined(USE_VERTEX_COLOR) || defined(COMBINE_VERTEX_COLOR)
	in vec4 fColor;
#endif

uniform vec4 uColor;

layout(location=0) out vec4 FragColor;

void main()
{    
	#if defined(USE_VERTEX_COLOR)
		FragColor = fColor;	
	#elif defined(COMBINE_VERTEX_COLOR)
		FragColor = uColor * fColor;
	#else
		FragColor = uColor;
	#endif

	toneMapColor(FragColor);
}