
in vec4 clipPos;
in vec4 prevClipPos;

out highp vec4 outVector;

void main()
{	
	vec3 cur = clipPos.xyz  / clipPos.w;
	vec3 prev = prevClipPos.xyz  / prevClipPos.w;
	outVector.xyz = cur - prev;
	outVector.w = 0.0;
}