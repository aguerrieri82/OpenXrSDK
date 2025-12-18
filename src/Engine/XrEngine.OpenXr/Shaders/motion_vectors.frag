
in vec4 clipPos;
in vec4 prevClipPos;

out vec2 outVector;

void main()
{	
	vec3 cur = clipPos.xyz  / clipPos.w;
	vec3 prev = prevClipPos.xyz  / prevClipPos.w;
	outVector = vec4(cur - prev, 0.0).xy;
}