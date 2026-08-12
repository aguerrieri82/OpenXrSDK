layout(location=0) in vec3 aPosition;
layout(location=2) in vec2 aUv0;
layout(location=4) in vec4 aTangent;

out vec2 fUv;
out vec2 fUv2;

flat out vec4 fConst;

void main()
{
	fUv = aUv0;
	fConst = aTangent;
	gl_Position = vec4(aPosition, 1.0);
}
