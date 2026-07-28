
uniform mat4 uPrevViewProj[2];

out vec4 fPrevClipPos;
out vec4 fCurClipPos;

void computeMotionVectors(vec3 pos)
{
	fPrevClipPos = uPrevViewProj[ACTIVE_EYE] * (uModel.prevWorldMatrix * vec4(pos, 1.0));
	fCurClipPos = gl_Position;
}