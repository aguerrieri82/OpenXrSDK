layout(location=0) in vec3 a_position;
layout(location=2) in vec2 a_texcoord;
layout(location=4) in vec4 a_tangent;

out vec2 fUv;
flat out vec4 fConst;

void main()
{
	fUv = a_texcoord;
	fConst = a_tangent;
	gl_Position = vec4(a_position, 1.0);
}
