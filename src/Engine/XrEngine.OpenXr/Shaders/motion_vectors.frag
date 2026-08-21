
in vec4 clipPos;
in vec4 prevClipPos;

layout(early_fragment_tests) in;

out highp vec4 outVector;

void main()
{	
	vec3 cur = clipPos.xyz  / clipPos.w;
	vec3 prev = prevClipPos.xyz  / prevClipPos.w;
	outVector.xyz = cur - prev;
	outVector.w = 0.0;
}