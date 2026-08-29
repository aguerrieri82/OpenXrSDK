#include "Shared/fragment_post.glsl"

#if defined(USE_VERTEX_COLOR) || defined(COMBINE_VERTEX_COLOR)
	in vec4 fColor;
#endif

uniform vec4 uColor;

#ifndef FRAG_LOCATION
	#define FRAG_LOCATION 0
#endif

layout(location=FRAG_LOCATION) out vec4 FragColor;

void main()
{    
	#if defined(USE_VERTEX_COLOR)
		#if defined(COMBINE_VERTEX_COLOR)
			FragColor = uColor * fColor;
		#else
			FragColor = fColor;	
		#endif
	#else
		FragColor = uColor;
	#endif

	doPost(FragColor);

	fixSrgbColor(FragColor);
}