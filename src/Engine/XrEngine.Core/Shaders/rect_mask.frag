in vec2 fUv;

uniform vec2 uOffset;
uniform vec2 uSize;

layout(location=0) out vec4 FragColor;

void main()
{
	if (fUv.x < uOffset.x || fUv.x > uOffset.x + uSize.x || fUv.y < uOffset.y || fUv.y> uOffset.y + uSize.y)
		FragColor = vec4(0.0, 0.0, 0.0, 0.0);
	else
		discard;
}