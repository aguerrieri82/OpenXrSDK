
#include "[XrEngine.Core]Shared/skin.glsl"

uniform bool uHasSkin;
uniform uint uActiveEye;

#ifdef MULTI_VIEW

    layout(num_views=2) in;
	#define EYE gl_ViewID_OVR

#else
	#define EYE uActiveEye
#endif


struct FrameMatrices {
	mat4 viewProj[2];
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
	vec3 position = a_position;

	if (uHasSkin)
		skinTransformPos(position);

	clipPos = uMatrices.current.viewProj[EYE] * ( uMatrices.current.model * vec4(position, 1.0 ) );
	prevClipPos = uMatrices.prev.viewProj[EYE] * ( uMatrices.prev.model * vec4(position, 1.0 ) );
	gl_Position = clipPos;
}