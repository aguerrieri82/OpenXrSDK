

struct FrameMatrices {
	mat4 viewProj;
	mat4 model;
};

struct Matrices {
	FrameMatrices current;
	FrameMatrices prev;
};


uniform Matrices uMatrices;

layout (location = 0) in vec3 a_position;

out vec4 clipPos;
out vec4 prevClipPos;

void main()
{
	clipPos = uMatrices.current.viewProj * ( uMatrices.current.model * vec4( a_position, 1.0 ) );
	prevClipPos = uMatrices.prev.viewProj * ( uMatrices.prev.model * vec4( a_position, 1.0 ) );
	gl_Position = clipPos;
}