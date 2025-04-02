uniform vec4 uColor;

#ifdef USE_VERTEX_COLOR
	in vec4 fColor;
#endif

layout(location=0) out vec4 FragColor;

void main()
{    
	FragColor = uColor;

	#ifdef USE_VERTEX_COLOR
		FragColor *= fColor;	
	#endif
}