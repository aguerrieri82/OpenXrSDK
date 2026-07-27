
struct FrameMatrices {
	mat4 viewProj[2];
	mat4 model;
};

uniform FrameMatrices uPrevMatrices;

out vec4 prevClipPos;
out vec4 curClipPos;

void computeMotionVectors()
{
	prevClipPos = uPrevMatrices.viewProj[ACTIVE_EYE] * (uPrevMatrices.model * vec4(a_position, 1.0));
	curClipPos = gl_Position;
}