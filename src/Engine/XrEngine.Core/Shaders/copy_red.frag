layout(location = 0) out float fragColor;

#ifdef TEXTURE_ARRAY
	uniform int uIndex;
	layout(binding=0) uniform highp sampler2DArray uImage;
#else
	layout(binding=0) uniform sampler2D uImage;
#endif

in vec2 fUv;

void main()
{    
	#ifdef TEXTURE_ARRAY
	   fragColor = texture(uImage, vec3(fUv, uIndex)).r;
	#else
	   fragColor = texture(uImage, fUv).r;
	#endif
}