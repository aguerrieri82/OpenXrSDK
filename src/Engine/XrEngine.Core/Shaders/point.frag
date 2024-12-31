
in vec4 fColor;

layout(location=0) out vec4 FragColor;

void main()
{    
	vec2 coord = gl_PointCoord * 2.0 - 1.0;
	float dist = length(coord);
	if (dist > 1.0)
		discard;

   FragColor = fColor;
}